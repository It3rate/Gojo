namespace Gojo.Experiments.PrimitiveMeasure;

/// <summary>Releases one reserved activation to open B's passage. The goal is an
/// input to this device only when an owner explicitly binds it here.</summary>
public sealed class RaceAssistance
{
    private const int ReferenceBits = 4;
    private readonly PulseCircuit release = new(2);
    private readonly ReadConnections reads = new();
    private readonly int unless, both;
    private readonly int passage;

    public IPrimitiveReadPort Released { get; }
    public IPrimitiveReadPort EffectiveAccess { get; }

    public RaceAssistance(IPrimitiveReadPort goalReached, IPrimitiveReadPort runningB,
        IPrimitiveReadPort requestB, IPrimitiveReadPort baseAccessB)
    {
        var goal = reads.Import(goalReached);
        var running = reads.Import(runningB);
        var request = reads.Import(requestB);
        var access = reads.Import(baseAccessB);

        // In the positive Boolean basis, keep the first input when the
        // second is clear. An unbound second role has no admitted tuple.
        unless = reads.AddTable(ConnectionLaws.FirstWithoutSecond);
        both = reads.AddTable(RaceExperiments.And);
        var stillRunning = reads.Connect(unless, ReferenceBits, running, goal);
        var refused = reads.Connect(unless, ReferenceBits, request, access);
        var eligible = reads.Connect(both, ReferenceBits, stillRunning.Endpoint, refused.Endpoint);

        var reserve = release.State(true);
        var opened = release.State();
        var guard = release.Guard();
        release.Bind(guard, eligible);
        passage = release.AddTable(PassageLaw.Controlled());
        release.Link(passage, reserve, guard, opened);
        Released = release.Expose(opened);
        var either = reads.AddTable(ConnectionLaws.Presence());
        EffectiveAccess = reads.Connect(either, ReferenceBits, access, reads.Import(Released));
    }

    public PulseStep Step() => release.Pulse();
    public byte[] RefusalAndGoalRows() => reads.PackedRows(unless, ReferenceBits);
    public byte[] EligibilityRow() => reads.PackedRows(both, ReferenceBits);
    public byte[] ReleaseRow() => release.PackedRows(passage);
}
