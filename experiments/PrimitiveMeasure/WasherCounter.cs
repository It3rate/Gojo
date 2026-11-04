using System.Numerics;

namespace Gojo.Experiments.PrimitiveMeasure;

/// <summary>One outstanding order, two stock sites and an initially empty tray.
/// Request and receipt wires are observations. Only the two seam rows move stock.</summary>
public sealed class WasherCounter
{
    // Truth bit L + 2*R. L = arrived request, R = retained successful delivery.
    public const byte RememberDelivery = 0b0010, ServeEveryRequest = 0b1010;
    private const byte Or = 0b1110, And = 0b1000, LeftOnly = 0b0010;

    private readonly PortStructure desk = new(), counter = new(), tray = new(), wires = new();
    private readonly int done, served, gate, forward, back;
    private readonly PortSeams feed;
    private readonly PassageConnections park;
    private readonly ConnectionRow parkRow;
    private readonly IPrimitiveReadPort request, admit, dropped, receipt, returned, nextServed, nextDone, lamp;

    public WasherCounter(byte admission, int initialStock = 2)
    {
        if (admission is not (RememberDelivery or ServeEveryRequest))
            throw new ArgumentOutOfRangeException(nameof(admission));
        if (initialStock is < 0 or > 2) throw new ArgumentOutOfRangeException(nameof(initialStock));
        done = desk.Create(0,0,1,0);
        served = counter.Create(0,0,1,0);
        int first = counter.Publish(counter.Create(0,0,1,initialStock >= 1 ? 1u : 0u));
        int second = counter.Publish(counter.Create(0,0,1,initialStock == 2 ? 1u : 0u));
        gate = tray.Create(0,0,1,0);
        int gatePort = tray.Publish(gate);
        int inbox = tray.Publish(tray.Create(0,0,1,0));
        int shelf = tray.Publish(tray.Create(0,0,1,0));
        forward = wires.Create(0,0,1,0);
        back = wires.Create(0,0,1,0);

        var confirmed = desk.Connect(desk.Publish(done));
        var remembered = counter.Connect(counter.Publish(served));
        request = Read(wires.Connect(wires.Publish(forward)), confirmed, LeftOnly);
        admit = Read(request, remembered, admission);
        var replay = Read(request, admit, LeftOnly);
        feed = new([counter.ExportOutlet(first), counter.ExportOutlet(second)],
            [tray.ExportIntake(gatePort, inbox)], PassageLaw.Controlled());
        feed.Link(0,0);
        feed.Link(1,0);
        // Only 011 -> 110 witnesses a whole moving through this open gate.
        byte[] arrived = [0,0,0,0,0,0,1 << 3,0];
        dropped = Read(feed.ObserveTransition(0,arrived), feed.ObserveTransition(1,arrived), Or);
        receipt = Read(dropped, replay, Or);
        returned = Read(receipt, wires.Connect(wires.Publish(back)), And);
        nextServed = Read(remembered, dropped, Or);
        nextDone = Read(confirmed, returned, Or);
        park = new(tray, PassageLaw.Unconditional());
        parkRow = park.Connect(2,inbox,gatePort,shelf);
        lamp = Read(tray.Connect(inbox), tray.Connect(shelf), Or);
    }

    private static IPrimitiveReadPort Read(IPrimitiveReadPort left, IPrimitiveReadPort right, byte truth) =>
        PresencePairPort.Bind(15,left,right).Test(truth);

    private static bool On(byte support) => support switch
    {
        1 => false, 2 => true, _ => throw new InvalidOperationException("Expected a resolved wire on this serial clock.")
    };

    public byte[] FeedRows => feed.RowBytes();
    public byte[] FeedLaw => feed.LawBytes();
    public byte StockBits => (byte)(counter.PackedBytes(0,0,1)[0] >> 1);
    public byte TrayBits => (byte)(tray.PackedBytes(0,0,1)[0] >> 1);
    public bool TrayLamp => On(lamp.ReadSupport());

    public WasherBeat Step(bool forwardOpen, bool returnOpen)
    {
        wires.StoreObservation(forward,false,false,forwardOpen ? 1u : 0u);
        wires.StoreObservation(back,false,false,returnOpen ? 1u : 0u);
        park.TryStep(parkRow,park.Observe(parkRow),out _);
        bool arrived = On(request.ReadSupport());
        if (!tray.TryCaptureObservation(gate,admit.ReadSupport()))
            throw new InvalidOperationException("Unresolved tray intake.");
        feed.Pulse();
        // Sample all feedback before committing either latch. Replay belongs to
        // the request as it arrived, even if a washer has just changed hands.
        bool sent = On(receipt.ReadSupport()), heard = On(returned.ReadSupport());
        byte holdServed = nextServed.ReadSupport(), holdDone = nextDone.ReadSupport();
        if (!counter.TryCaptureObservation(served,holdServed) || !desk.TryCaptureObservation(done,holdDone))
            throw new InvalidOperationException("Unresolved feedback on the counter clock.");
        return new(arrived,On(dropped.ReadSupport()),sent,heard,On(holdServed),On(holdDone),StockBits,TrayBits);
    }
}

public readonly record struct WasherBeat(bool RequestArrived, bool Dropped, bool ReceiptSent, bool Heard,
    bool Served, bool Done, byte StockBits, byte TrayBits);

/// <summary>Bit i is the event on beat i. The counts are an external audit of
/// occupied sites or release pulses. No material site holds a count.</summary>
public readonly record struct WasherHistory(byte Drops, byte Heard)
{
    public int Parts => BitOperations.PopCount((uint)Drops);

    // Tray contents persist. Occupancy changes on the earliest release only.
    public byte LampWord(int beats)
    {
        int mask = WasherSurvey.Mask(beats);
        int inWindow = Drops & mask;
        return (byte)(-(inWindow & -inWindow) & mask);
    }
}
