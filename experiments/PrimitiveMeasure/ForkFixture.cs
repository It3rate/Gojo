namespace Gojo.Experiments.PrimitiveMeasure;

/// <summary>One waiting whole pulse and two exports. The exit owner's published
/// vacancy observations drive the junction's two guards. A latched offer is a
/// material site. Both seams are pulsed in fixed order on every clock.</summary>
public sealed class ForkFixture
{
    private readonly PortStructure exits = new();
    private readonly PulseCircuit fork = new(3), receipt = new(2);
    private readonly ReadConnections reads = new();
    private readonly PassageConnections local;
    private readonly ConnectionRow close, release, deliverLeft, deliverRight;
    private readonly PortSeams seams;
    private readonly int routeLaw, vacancyLaw, fallbackLaw;
    private int beat;

    public ForkFixture(bool leftBlocked = false, bool rightBlocked = false, bool connectLeft = true,
        bool connectReceipt = true, bool advertiseLeft = true)
    {
        int Site(bool occupied = false) => exits.Publish(exits.Create(0, 0, 1, occupied ? 1UL : 0UL));
        var leftGate = Site(leftBlocked); var rightGate = Site(rightBlocked);
        var leftBox = Site(); var rightBox = Site();
        var leftEnd = Site(); var rightEnd = Site();
        var reserve = Site(!leftBlocked); var clock = Site(true);
        local = new PassageConnections(exits, PassageLaw.Unconditional());
        close = local.Connect(3, reserve, clock, leftGate);
        release = local.Connect(3, leftGate, clock, reserve);
        deliverLeft = local.Connect(3, leftBox, clock, leftEnd);
        deliverRight = local.Connect(3, rightBox, clock, rightEnd);

        // Compile construction choices once. ReadConnections evaluates the
        // stored tuple bits and connected references at runtime.
        vacancyLaw = reads.AddTable(TruthTable((gate, box) => !gate && !box));
        var leftFree = reads.Connect(vacancyLaw, 3, reads.Import(exits.Connect(leftGate)), reads.Import(exits.Connect(leftBox)));
        var rightFree = reads.Connect(vacancyLaw, 3, reads.Import(exits.Connect(rightGate)), reads.Import(exits.Connect(rightBox)));
        fallbackLaw = reads.AddTable(TruthTable((left, right) => !left && right));
        var chooseRight = reads.Connect(fallbackLaw, 3, leftFree.Endpoint, rightFree.Endpoint);
        int waiting = fork.State(true), offerLeft = fork.State(), offerRight = fork.State();
        var leftGuard = fork.Guard(); var rightGuard = fork.Guard();
        if (advertiseLeft) fork.Bind(leftGuard, leftFree);
        fork.Bind(rightGuard, chooseRight);
        routeLaw = fork.AddTable(PassageLaw.Controlled());
        fork.Link(routeLaw, waiting, leftGuard, offerLeft);
        fork.Link(routeLaw, waiting, rightGuard, offerRight);

        seams = new([fork.ExportOutlet(offerLeft), fork.ExportOutlet(offerRight)],
            [exits.ExportIntake(leftGate, leftBox), exits.ExportIntake(rightGate, rightBox)],
            PassageLaw.Controlled(whenPresent: false));
        if (connectLeft) seams.Link(0, 0);
        seams.Link(1, 1);

        var waitingReceipt = receipt.State(true);
        var seen = receipt.State();
        var arrivalGuard = receipt.Guard();
        var arrived = PresencePorts.Any(exits.Connect(leftEnd), exits.Connect(rightEnd));
        if (connectReceipt) receipt.Bind(arrivalGuard, arrived);
        receipt.Link(receipt.AddTable(PassageLaw.Controlled()), waitingReceipt, arrivalGuard, seen);
    }

    private static BitRelation TruthTable(Func<bool, bool, bool> f) => BitRelation.FromTuples(3,
        from a in Enumerable.Range(0, 2) from b in Enumerable.Range(0, 2)
        select new byte[] { (byte)a, (byte)b, (byte)(f(a != 0, b != 0) ? 1 : 0) });

    private void PulseLocal(ConnectionRow row)
    {
        var before = local.Observe(row);
        if (!local.TryStep(row, before, out _)) throw new InvalidOperationException("Unresolved fixed fixture row.");
    }

    // External gate stimuli move a separate blocker between reserve and gate.
    public void CloseLeft() => PulseLocal(close);
    public void ReleaseLeft() => PulseLocal(release);

    // The clock supplies order. Every connected row gets the same opportunities
    // even after failure. No observer chooses a route, retry or successful run.
    private SeamStep[] Clock()
    {
        PulseLocal(deliverLeft);
        PulseLocal(deliverRight);
        var transfers = seams.Pulse();
        fork.Pulse();
        receipt.Pulse();
        beat++;
        return transfers;
    }

    public void Tick() => Clock();
    public ForkFrame Advance() => Frame(Clock());
    public ForkFrame Snapshot() => Frame([]);
    private ForkFrame Frame(SeamStep[] transfers) => new(beat, fork.ValueBytes()[0],
        exits.PackedBytes(0, 0, 1)[0], receipt.ValueBytes()[0], transfers);
    public byte[] RouteRows() => fork.PackedRows(routeLaw);
    public byte[] SeamRows() => seams.RowBytes();
    public byte[] VacancyRows() => reads.PackedRows(vacancyLaw, 3);
    public byte[] FallbackRows() => reads.PackedRows(fallbackLaw, 3);
    public byte[] VacancyLaw() => reads.LawBytes(vacancyLaw);
    public byte[] FallbackLaw() => reads.LawBytes(fallbackLaw);
    public byte[] RouteLaw() => fork.LawBytes(routeLaw);
    public byte[] SeamLaw() => seams.LawBytes();
    public ForkCounter AttachRefusalCounter()
    {
        // Four diagonal cells have S=1 both before and after settlement.
        byte[] held = [0, 2, 0, 8, 0, 32, 0, 128];
        var ports = Enumerable.Range(0, seams.Count).Select(i => seams.ObserveTransition(i, held)).ToArray();
        return new ForkCounter(PresencePorts.Any(ports));
    }
}

/// <summary>Clamped tally of beats with a resolved offered-but-held transfer.
/// Only the caller's measuring clock pulses this instrument once after each
/// sampled beat. Its states have no connection back to either material owner.</summary>
public sealed class ForkCounter
{
    private readonly PulseCircuit circuit = new(2);
    internal ForkCounter(IPrimitiveReadPort held)
    {
        var zero = circuit.State(true); var one = circuit.State(); var two = circuit.State();
        var guard = circuit.Guard();
        circuit.Bind(guard, held);
        var table = circuit.AddTable(PassageLaw.Controlled());
        circuit.Link(table, zero, guard, one);
        circuit.Link(table, one, guard, two);
    }
    public void Sample() => circuit.Pulse();
    public int Reading => circuit.Active!.Value;
    public byte[] Bytes() => circuit.ValueBytes();
}

/// <summary>Exogenous inputs fixed before execution. This apparatus can touch
/// only the obstacle. Its input contains no selected route or observed result.</summary>
public readonly record struct ForkTape(bool LeftBusy, bool RightBusy, int CloseOn = 0, int ReleaseOn = 0)
{
    public void Apply(ForkFixture machine, int beat)
    {
        if (beat == CloseOn) machine.CloseLeft();
        if (beat == ReleaseOn) machine.ReleaseLeft();
    }
}

// Output copies. Counters are consumers of these readouts after the run.
public sealed record ForkFrame(int Beat, byte Fork, byte Exits, byte Receipt, SeamStep[] Transfers)
{
    public int PulseSites => System.Numerics.BitOperations.PopCount((uint)(Fork & 7)) +
        System.Numerics.BitOperations.PopCount((uint)(Exits & 0x3C));
}

public readonly record struct ForkCounts(int Offers, int Refusals, int Accepted, int Arrivals)
{
    public static ForkCounts From(IReadOnlyList<ForkFrame> frames)
    {
        var offered = 0; var refused = 0; var accepted = 0; var arrived = 0;
        foreach (var frame in frames)
            foreach (var step in frame.Transfers)
                if (step.Transfer.Offered)
                {
                    offered++;
                    if (step.Transfer.Moved) accepted++; else refused++;
                }
        for (var i = 1; i < frames.Count; i++)
            arrived += System.Numerics.BitOperations.PopCount((uint)(frames[i].Exits & ~frames[i - 1].Exits & 0x30));
        return new(offered, refused, accepted, arrived);
    }
}
