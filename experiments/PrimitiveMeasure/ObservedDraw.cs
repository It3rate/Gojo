namespace Gojo.Experiments.PrimitiveMeasure;

/// <summary>One serial request samples permission into the receiver gate,
/// then pulses the collecting stem's material seams. A missing or ambiguous
/// sample defers that request, even if the last sample opened the gate.</summary>
public sealed class ObservedDraw
{
    private readonly PortStructure receiver = new();
    private readonly ReadConnections reads = new();
    private readonly int blocked, law;
    private readonly ConnectionRow capture, park;
    private readonly PassageConnections local;
    private readonly PortSeams draw;

    public ObservedDraw(GlyphJunction collector, IPrimitiveReadPort permission)
    {
        blocked = receiver.Create(0, 0, 1, 1);
        int gate = receiver.Publish(blocked);
        int inbox = receiver.Publish(receiver.Create(0, 0, 1, 0));
        int shelf = receiver.Publish(receiver.Create(0, 0, 1, 0));
        law = reads.AddTable(BitRelation.FromTuples(2, Enumerable.Range(0, 8)
            .Select(w => new byte[] { (byte)w, (byte)(w ^ 1) })));
        capture = reads.Wire(law, 2, reads.Import(permission), reads.Import(receiver.Connect(gate)));
        draw = collector.ConnectDraw(receiver.ExportIntake(gate, inbox));
        local = new(receiver, PassageLaw.Unconditional());
        park = local.Connect(2, inbox, gate, shelf);
    }

    public byte Stored => receiver.PackedBytes(0, 0, 1)[0];
    public SeamStep[]? Request() => reads.TryCapture(law, capture, receiver, blocked) ? draw.Pulse() : null;
    public void Park() => local.TryStep(park, local.Observe(park), out _);
}
