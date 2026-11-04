namespace Gojo.Experiments.PrimitiveMeasure;

/// <summary>Waiting -> offer under a published guard, then offer -> inbox
/// across a seam. The signal is observed and the sender supplies the pulse.</summary>
public sealed class GuardedOffer
{
    private readonly PulseCircuit sender = new(2);
    private readonly PassageFixture receiver = new(3, 0);
    private readonly PortSeams seam;

    public GuardedOffer(IPrimitiveReadPort permission)
    {
        var waiting = sender.State(true);
        var offer = sender.State();
        var guard = sender.Guard();
        sender.Bind(guard, permission);
        sender.Link(sender.AddTable(PassageLaw.Controlled()), waiting, guard, offer);
        var intake = receiver.Owner.ExportIntake(receiver.Ports[0], receiver.Ports[1]);
        seam = new([sender.ExportOutlet(offer)], [intake], PassageLaw.Controlled(false));
        seam.Link(0, 0);
    }

    public byte Sender => sender.ValueBytes()[0];
    public byte Receiver => receiver.Stored;

    public HandoffRead Request()
    {
        sender.Pulse();
        return seam.Pulse()[0].Transfer;
    }
}
