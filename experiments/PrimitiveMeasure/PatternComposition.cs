namespace Gojo.Experiments.PrimitiveMeasure;

/// <summary>Demonstration of connected request/completion ports. The parent
/// requests a motif, visits explicit waiting sites, and requests a displacement
/// during the wait. A shared offset is a formal transform, never a coordinate.
/// Each accepted displacement appends [old placement,new placement,offset].
/// Each motif invocation binds to the exposed current placement reference.
/// This is a selected construction rule, not physical pen or ink simulation.</summary>
public sealed class PatternComposition
{
    public PulseCircuit Parent { get; } = new();
    public RepeatedBody Body { get; }
    public PulseCircuit Driver { get; } = new();
    public int Draw { get; }
    public int[] Waiting { get; }
    public int DriverReady { get; }
    public int DriverDone { get; }
    public int Placement { get; private set; } = 1;
    public int Instances { get; private set; }
    private readonly EndpointRows translations = new();
    private readonly EndpointRows frames = new();

    public PatternComposition(int waitBeats = 5)
    {
        if (waitBeats is < 1 or > 16) throw new ArgumentOutOfRangeException(nameof(waitBeats));
        Draw = Parent.State(true);
        Waiting = Enumerable.Range(0, waitBeats).Select(_ => Parent.State()).ToArray();
        Body = new([0, 1], 4, request: Parent.Expose(Draw));
        Body.BindTurns(RepetitionExamples.Contour(4, true, false));
        DriverReady = Driver.State(true);
        DriverDone = Driver.State();
        var request = Driver.Guard();
        Driver.Bind(request, PresencePorts.Any(Waiting.Select(Parent.Expose).ToArray()));
        Driver.Link(Driver.AddTable(PassageLaw.Controlled()), DriverReady, request, DriverDone);
        Driver.Link(Driver.AddTable(PassageLaw.Controlled(false)), DriverDone, request, DriverReady);
        var bodyDone = Parent.Guard();
        var driverDone = Parent.Guard();
        var always = Parent.Guard();
        Parent.Bind(bodyDone, Body.Completion);
        Parent.Bind(driverDone, Driver.Expose(DriverDone));
        var present = Parent.AddTable(PassageLaw.Controlled());
        var forward = Parent.AddTable(PassageLaw.Unconditional());
        Parent.Link(present, Draw, bodyDone, Waiting[0]);
        for (var i = 0; i + 1 < Waiting.Length; i++) Parent.Link(forward, Waiting[i], always, Waiting[i + 1]);
        Parent.Link(present, Waiting[^1], driverDone, Draw);
    }

    /// <summary>The caller supplies one beat. Explicit order is body, driver,
    /// parent; completion/request propagation can therefore cost a later beat.
    /// A finite reference width bounds this retained demonstration history.</summary>
    public PatternStep Pulse()
    {
        // At most one new row of either kind can be created this beat. Refuse
        // before advancing participants if the local retained chart is full.
        if (Placement >= 254 || Instances >= 255) throw new InvalidOperationException("Retained chart is full.");
        var parentBefore = Parent.Active;
        var body = Body.Pulse();
        int? startedAt = null;
        if (body.Before == Body.Idle && body.After == Body.ParticipationSites[0] && body.Before != body.After)
        {
            startedAt = Placement;
            frames.Append(8, Instances, Placement);
            Instances++;
        }
        var driver = Driver.Pulse();
        var moved = driver.Moved && driver.After == DriverDone;
        if (moved)
        {
            translations.Append(8, Placement, Placement + 1, 0);
            Placement++;
        }
        var parent = Parent.Pulse();
        return new(body, startedAt, moved, parentBefore, parent.After);
    }

    public int[] Frame(int instance) => frames.Read(new(2, 8, instance));
    public int[] Translation(int index) => translations.Read(new(3, 8, index));
    public byte[] FrameBytes() => frames.PackedBytes(2, 8);
    public byte[] TranslationBytes() => translations.PackedBytes(3, 8);
}

public readonly record struct PatternStep(BodyStep Body, int? StartedAt, bool PlacementAdvanced,
    int? ParentBefore, int? ParentAfter);
