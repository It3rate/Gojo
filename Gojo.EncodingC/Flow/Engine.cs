namespace Gojo.EncodingC;

/// <summary>One local structure, and the order it settles in. An engine holds
/// its stores and runs its wirings, one step at a time, in their declared
/// order: a transition law changes state, a relation law rules out what is no
/// longer possible. Nothing else happens in a step, so a machine that
/// observes, narrows, decides, acts and forgets does all of it as wirings. A
/// wiring that touches a position another writes runs in a later wave and
/// reads what the earlier one left; the rest could run at the same moment on a
/// parallel substrate and give the same result. Running them one after
/// another in declared order is therefore exactly the waved run. The order is
/// structure: it is the schedule, and two takers of one unit cannot both have
/// it.
/// <para><b>Inside and outside.</b> A store belongs to the engine that holds
/// it or first binds it directly, and to no other. Inside, wirings bind its
/// stores freely: their positions, whether those are in use, and workspaces
/// over them. Another local structure is reached only through a port, or a
/// socket a port fills. Adding a wiring that would bind another's store
/// directly is refused, so the boundary is kept by the structure, not by
/// whoever wires. Membership is by connection: no name, flag or index marks
/// it, and a saved machine keeps it as the grouping of its stores.</para>
/// <para><b>Several local structures</b> each step on their own. Whoever runs
/// them chooses the order; a peer that is not stepped holds still, and one
/// that is absent leaves a socket empty. A port has one taker (see
/// <see cref="Port"/>), so no unit is contested between structures, and
/// stepping them in any order, or at the same moment, moves the same units.
/// Partial structures can <see cref="Fuse"/> into one.</para></summary>
public sealed class Engine
{
    private readonly List<Wiring> wirings = [];
    private readonly List<Store> stores = [];

    public IReadOnlyList<Wiring> Wirings => wirings.AsReadOnly();

    /// <summary>The stores that belong to this local structure, in the order
    /// they joined it.</summary>
    public IReadOnlyList<Store> Stores => stores.AsReadOnly();

    /// <summary>The sockets its wirings bind, in the order first bound: what
    /// this structure expects from others.</summary>
    public IReadOnlyList<Socket> Sockets =>
        wirings.SelectMany(wiring => wiring.Bindings).Select(binding => binding.Surface).OfType<Socket>().Distinct().ToArray();

    /// <summary>Steps run so far.</summary>
    public long Tick { get; private set; }

    /// <summary>The structure this one is a fork of, or null. A fork is the
    /// same local intelligence supposing: it reads its parent's stores through
    /// workspaces and never binds them directly.</summary>
    public Engine? Parent { get; private init; }

    /// <summary>A fork of this local structure: its state read by reference
    /// through copy-on-write workspaces, its topology copied so it can be
    /// rewired, its laws shared. See <see cref="EncodingC.Fork"/>.</summary>
    public Fork Fork() => EncodingC.Fork.Of(this);

    internal static Engine ForkOf(Engine parent) => new() { Parent = parent };

    /// <summary>Takes stores into this local structure, such as one that only
    /// exposes ports, which no wiring here binds directly.</summary>
    public Engine Hold(params Store[] held)
    {
        ArgumentNullException.ThrowIfNull(held);
        Claim(held);
        return this;
    }

    /// <summary>Adds a wiring. Every store it binds directly, and the store of
    /// its rows, joins this local structure; one that belongs to another is
    /// refused.</summary>
    public Engine Add(Wiring wiring)
    {
        ArgumentNullException.ThrowIfNull(wiring);
        Claim(wiring.Bindings.Where(binding => !Supposes(binding.Surface)).Select(binding => binding.Surface.Home).OfType<Store>().Append(wiring.Rows));
        wirings.Add(wiring);
        return this;
    }

    /// <summary>A surface this structure supposes about rather than owns: a
    /// workspace, or a view of one, over a store of a structure this one is a
    /// fork of.</summary>
    private bool Supposes(Surface surface)
    {
        var view = surface is Flipped flipped ? flipped.Source : surface;
        if (view is not Workspace || surface.Home?.Local is not { } owner) return false;
        for (var ancestor = Parent; ancestor is not null; ancestor = ancestor.Parent)
            if (ancestor == owner) return true;
        return false;
    }

    /// <summary>Fuses another local structure into this one. Its stores
    /// join this one, and its wirings run after this one's, in their order:
    /// the two settle as one from now on. Ports and sockets between them keep
    /// working, now as connections inside one structure, and its wirings may
    /// bind the other's stores directly. The other is left empty.</summary>
    public Engine Fuse(Engine other)
    {
        ArgumentNullException.ThrowIfNull(other);
        if (ReferenceEquals(other, this)) throw new ArgumentException("A structure does not fuse with itself.", nameof(other));
        foreach (var store in other.stores) store.Local = this;
        stores.AddRange(other.stores);
        wirings.AddRange(other.wirings);
        other.stores.Clear();
        other.wirings.Clear();
        return this;
    }

    private void Claim(IEnumerable<Store> claimed)
    {
        var all = claimed.ToArray();
        foreach (var store in all)
        {
            ArgumentNullException.ThrowIfNull(store);
            if (store.Local is { } other && other != this)
                throw new ArgumentException(
                    "A store of another local structure is bound directly. Between local structures the only connection is a port.",
                    nameof(claimed));
        }
        foreach (var store in all)
        {
            if (store.Local is not null) continue;
            store.Local = this;
            stores.Add(store);
        }
    }

    /// <summary>Wirings grouped into waves, by index.</summary>
    public IReadOnlyList<int[]> Waves()
    {
        var lastWrite = new Dictionary<(Surface, int), int>();
        var lastTouch = new Dictionary<(Surface, int), int>();
        var waves = new List<List<int>>();
        for (var i = 0; i < wirings.Count; i++)
        {
            var rows = wirings[i].Runnable().Indexes().ToArray();
            var touched = rows.SelectMany(wirings[i].TouchedKeys).Concat(wirings[i].WatchedKeys()).Distinct().ToArray();
            var written = rows.SelectMany(wirings[i].WrittenKeys).Distinct().ToArray();
            var level = 0;
            foreach (var site in touched)
                if (lastWrite.TryGetValue(site, out var w) && w >= level) level = w + 1;
            foreach (var site in written)
                if (lastTouch.TryGetValue(site, out var t) && t >= level) level = t + 1;
            foreach (var site in touched)
                if (lastTouch.GetValueOrDefault(site, -1) < level) lastTouch[site] = level;
            foreach (var site in written)
                if (lastWrite.GetValueOrDefault(site, -1) < level) lastWrite[site] = level;
            while (waves.Count <= level) waves.Add([]);
            waves[level].Add(i);
        }
        return waves.Select(wave => wave.ToArray()).ToArray();
    }

    public EngineStep Step()
    {
        var waves = Waves();
        var steps = wirings.Select(wiring => wiring.Apply()).ToArray();
        Tick++;
        return new EngineStep(Tick, steps, waves);
    }

    public IReadOnlyList<EngineStep> Run(int steps) => Enumerable.Range(0, steps).Select(_ => Step()).ToArray();
}

/// <summary>One engine step: what each wiring did, and which wirings could have
/// run together.</summary>
public sealed record EngineStep(long Tick, IReadOnlyList<WiringStep> Steps, IReadOnlyList<int[]> Waves);
