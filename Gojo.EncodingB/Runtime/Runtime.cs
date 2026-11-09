namespace Gojo.EncodingB;

/// <summary>What arrives in one tick: signed change at sites, and supplied
/// observations of non-material planes (such as gate readings).</summary>
public sealed record TickInput(IReadOnlyList<(int Site, int Delta)> Ingest, IReadOnlyList<(string Plane, int Site, bool Value)> Observations)
{
    public static TickInput Empty { get; } = new([], []);

    public static TickInput Change(params (int Site, int Delta)[] ingest) => new(ingest, []);
}

/// <summary>Offer, accept and return for one tick. The identities
/// <c>pending = offered + remainder</c> and <c>offered = accepted + blocked</c> are
/// checked every tick; a violation is a hard-energy bug, not a trade-off.</summary>
public sealed record TransferRecord(
    IReadOnlyList<(int Route, int From, int To)> Offered,
    IReadOnlyList<(int Route, int From, int To)> Accepted,
    IReadOnlyList<(int Route, int From, int To)> Blocked,
    IReadOnlyList<int> Remainder)
{
    public static TransferRecord None { get; } = new([], [], [], []);
}

/// <summary>Everything one tick did, phase by phase.</summary>
public sealed record TickWitness(
    long Tick,
    IReadOnlyList<IngestRecord> Ingested,
    TransferRecord Transfers,
    IReadOnlyList<TransitionWitness> Transitions,
    (int Drawn, int Filled) Absorbed,
    GapSettlement? Gaps,
    Energy Before,
    Energy After,
    IReadOnlyList<string> HardViolations);

/// <summary>State needed to replay exactly: tile, overflow residue, route
/// strengths, schedule pointer and tick count.</summary>
public sealed record Checkpoint(long Tick, TileSnapshot Tile, int[] Overflow, byte[]? Routes, int GapPointer);

/// <summary>The tick contract on one tile:
/// ingest → observe → offer → accept → return → settle laws → absorb → account → adapt.
/// Offers are read from committed state and receivers write only their own
/// sites, so the material phases are double-buffered. Laws run in declared
/// waves. Every input is logged, so any tick can be replayed from a checkpoint
/// instead of storing a witness for every read.</summary>
public sealed class Runtime
{
    private readonly List<TickInput> log = [];
    private readonly IReadOnlyList<LawRows> transitions;
    private readonly IReadOnlyList<LawRows> obligations;

    public Runtime(
        TensionField tension,
        RouteTable? routes = null,
        GapBalance? gaps = null,
        IReadOnlyList<LawRows>? transitions = null,
        IReadOnlyList<LawRows>? obligations = null,
        int adaptEvery = 1)
    {
        ArgumentNullException.ThrowIfNull(tension);
        if (adaptEvery < 1) throw new ArgumentOutOfRangeException(nameof(adaptEvery));
        Tension = tension;
        Routes = routes;
        Gaps = gaps;
        this.transitions = transitions ?? [];
        this.obligations = obligations ?? [];
        AdaptEvery = adaptEvery;
        foreach (var rows in this.transitions.Concat(this.obligations))
            if (!ReferenceEquals(rows.Grammar, Tile.Grammar)) throw new ArgumentException($"{rows.Name} uses a different grammar.");
    }

    public Tile Tile => Tension.Tile;
    public TensionField Tension { get; }
    public RouteTable? Routes { get; }
    public GapBalance? Gaps { get; }
    public int AdaptEvery { get; }
    public long TickIndex { get; private set; }
    public IReadOnlyList<TickInput> Log => log;

    public Energy Energy(IReadOnlyList<string>? hardViolations = null)
    {
        var hard = (hardViolations?.Count ?? 0) + Tile.Validate().Count;
        var obligation = obligations.Sum(rows => (long)Bits.Count(rows.Violations(Tile)));
        var goal = Tension.TotalTension + (Gaps?.Spread ?? 0);
        return new Energy(hard, obligation, goal, 0);
    }

    public TickWitness Tick(TickInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        log.Add(input);
        var before = Energy();
        var hard = new List<string>();

        // 0 ingest: external change enters as tension.
        var ingested = input.Ingest.Select(change => Tension.Ingest(change.Site, change.Delta)).ToArray();
        foreach (var record in ingested)
            if (record.Accepted + record.Overflow != Math.Abs(record.Delta))
                hard.Add($"ingest at {record.Site} lost units");

        // 1 observe: supplied evidence on non-material planes.
        foreach (var (plane, site, value) in input.Observations) Tile.Supply(plane, site, value);

        // 2 absorb what each site can hold (self-local, C2).
        var absorbedFirst = Tension.Absorb(Tile.Allocated);

        // 3-5 offer, accept, return.
        var transfers = Routes is null ? TransferRecord.None : Transfer(hard);

        // settle: declared transition laws, then gap descent.
        var witnesses = transitions.Select(rows => rows.Apply(Tile, rows.AllRows)).ToArray();
        var gaps = Gaps?.Settle();

        // absorb what arrived.
        var absorbedSecond = Tension.Absorb(Tile.Allocated);

        // 7 adapt, on the medium clock.
        TickIndex++;
        if (Routes is not null && TickIndex % AdaptEvery == 0)
        {
            foreach (var (route, _, _) in transfers.Accepted) Routes.Revise(route, relieved: true);
            foreach (var (route, _, _) in transfers.Blocked) Routes.Revise(route, relieved: false);
        }

        // 6 account.
        var after = Energy(hard);
        return new TickWitness(TickIndex, ingested, transfers, witnesses,
            (absorbedFirst.Drawn + absorbedSecond.Drawn, absorbedFirst.Filled + absorbedSecond.Filled), gaps, before, after, hard);
    }

    public Checkpoint Checkpoint() =>
        new(TickIndex, Tile.Snapshot(), Tension.OverflowSnapshot(), Routes?.Snapshot(), Gaps?.Pointer ?? 0);

    public void Restore(Checkpoint checkpoint)
    {
        ArgumentNullException.ThrowIfNull(checkpoint);
        Tile.Restore(checkpoint.Tile);
        Tension.RestoreOverflow(checkpoint.Overflow);
        if (checkpoint.Routes is not null) Routes?.Restore(checkpoint.Routes);
        if (Gaps is not null) Gaps.Pointer = checkpoint.GapPointer;
        TickIndex = checkpoint.Tick;
        log.RemoveRange(0, log.Count);
    }

    /// <summary>Restores a checkpoint and replays inputs: the witness lane. Any
    /// tick's witness can be rematerialized rather than stored.</summary>
    public IReadOnlyList<TickWitness> Replay(Checkpoint checkpoint, IEnumerable<TickInput> inputs)
    {
        Restore(checkpoint);
        return inputs.Select(Tick).ToArray();
    }

    private TransferRecord Transfer(List<string> hard)
    {
        var routes = Routes!;
        // Offer buffer: read only from committed state.
        var pending = Enumerable.Range(0, Tile.SiteCount).Where(site => Tension.Positive(site) > 0).ToArray();
        var offered = new List<(int, int, int)>();
        var remainder = new List<int>();
        foreach (var site in pending)
        {
            if (routes.Strongest(site) is int route) offered.Add((route, site, routes.To(route)));
            else remainder.Add(site);
        }
        // Accept: receivers decide in route order, writing only their own room.
        var room = Enumerable.Range(0, Tile.SiteCount).Select(Tension.Room).ToArray();
        var accepted = new List<(int, int, int)>();
        var blocked = new List<(int, int, int)>();
        foreach (var offer in offered.OrderBy(o => o.Item1))
        {
            if (room[offer.Item3] > 0)
            {
                room[offer.Item3]--;
                accepted.Add(offer);
            }
            else blocked.Add(offer);
        }
        // Return: blocked units stay with their senders; accepted units arrive as tension.
        foreach (var (_, from, to) in accepted)
            if (!Tension.MoveUnit(from, to)) hard.Add($"accepted unit {from}->{to} could not move");
        if (pending.Length != offered.Count + remainder.Count) hard.Add("pending != offered + remainder");
        if (offered.Count != accepted.Count + blocked.Count) hard.Add("offered != accepted + blocked");
        return new TransferRecord(offered, accepted, blocked, remainder);
    }
}
