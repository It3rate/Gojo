using Gojo.Experiments.OrnamentPathCampaign;
using Gojo.Experiments.PrimitiveMeasure;

namespace Gojo.Experiments.OrnamentFlowPilot;

// One beat's read at an ink contact. Extent is sampled only for a
// participating contact that has a bound extent source.
internal readonly record struct PenContact(byte Participation, int? Extent, PathSite From, PathSite To)
{
    public bool DrewSpot => Participation == 2 && Extent is null;
    public bool DrewStroke => Participation == 2 && Extent > 0;
}

internal sealed class FlowPen
{
    private readonly PortStructure ink = new();
    private readonly int deposited;

    public FlowPen(PathSite start = default)
    {
        Cursor = start;
        deposited = ink.Create(0, 0, 1, 0);
        MarkPort = ink.Connect(ink.Publish(deposited));
    }

    public PathSite Cursor { get; private set; }
    public IPrimitiveReadPort MarkPort { get; }

    // A neighbor can bind one comparison at this baseline. Reading the port
    // samples the pen at that instant and exposes only the predicate bit.
    public IPrimitiveReadPort ReadHeight(FretFrame basis, bool above)
    {
        basis.Check();
        return new HeightPort(this, basis, above);
    }

    private sealed class HeightPort(FlowPen pen, FretFrame basis, bool above) : IPrimitiveReadPort
    {
        public byte ReadSupport()
        {
            int height = basis.Height(pen.Cursor);
            bool present = above ? height > 0 : height == 0;
            return present ? (byte)2 : (byte)1;
        }
    }

    // This outlet explicitly deposits ink. With no extent connection
    // it touches its site; a measured zero-length stroke has no ink.
    // Unit is a direction in the supplied drawing frame.
    public PenContact Touch(IPrimitiveReadPort participation, PathSite unit, Func<int>? readExtent = null)
    {
        var support = participation.ReadSupport();
        if (support > 3)
            throw new ArgumentException("A contact requires a positive-context participation port.", nameof(participation));

        var start = Cursor;
        int? extent = null;
        if (support == 2 && readExtent is not null)
        {
            extent = readExtent();
            if (extent < 0)
                throw new InvalidOperationException("The drawing extent must be an unsigned tick count.");
            if (extent > 0)
            {
                if (unit.X is < -1 or > 1 || unit.Y is < -1 or > 1 || unit == default)
                    throw new ArgumentOutOfRangeException(nameof(unit), "A nonzero reach needs a unit direction.");
                Cursor = new PathSite(checked(start.X + unit.X * extent.Value), checked(start.Y + unit.Y * extent.Value));
            }
        }

        var result = new PenContact(support, extent, start, Cursor);
        // Publish whether ink actually landed this beat. The separate input
        // witness still distinguishes missing (0), off (1), on (2), open (3).
        ink.StoreObservation(deposited, false, false, result.DrewSpot || result.DrewStroke ? 1UL : 0UL);
        return result;
    }
}

internal sealed class SingleSpotPen
{
    private readonly int stopped;
    private readonly IPrimitiveReadPort contact;

    public SingleSpotPen(PathSite start, IPrimitiveReadPort previousMark)
    {
        Pen = new FlowPen(start);
        var waiting = Circuit.State(true);
        var stamping = Circuit.State();
        stopped = Circuit.State();
        var release = Circuit.Guard();
        Circuit.Bind(release, previousMark);
        Circuit.Link(Circuit.AddTable(PassageLaw.Controlled()), waiting, release, stamping);
        Circuit.Link(Circuit.AddTable(PassageLaw.Unconditional()), stamping, release, stopped);
        contact = Circuit.Expose(stamping);
    }

    public FlowPen Pen { get; }
    public PulseCircuit Circuit { get; } = new(referenceBits: 2);
    public bool Stopped => Circuit.Active == stopped;

    // Step before the primary pen publishes its new mark on the shared beat.
    public PenContact Step()
    {
        Circuit.Pulse();
        return Pen.Touch(contact, default);
    }
}
