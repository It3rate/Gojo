namespace Gojo.Experiments.PrimitiveMeasure;

/// <summary>A reusable body assembled from passages. Participation sites refer
/// to shared parameters; alternating turn sites request one phase advance.
/// A check site routes to feedback or completion using a public stop read.
/// No geometric length, angle, counter, or shape opcode is stored.</summary>
public sealed class RepeatedBody
{
    public PulseCircuit Circuit { get; } = new();
    public ReferenceCycle Phase { get; }
    public int[] ParticipationSites { get; }
    public int[] TurnSites { get; }
    public int Check { get; }
    public int Done { get; }
    public int? Idle { get; }
    public int Request { get; }
    public int Stop { get; }
    public int ForwardTable { get; }
    public int PresentTable { get; }
    public int AbsentTable { get; }
    private readonly EndpointRows parameters = new();
    private readonly EndpointRows turnBindings = new();
    private ContourPattern? turnContour;
    public IPrimitiveReadPort Completion => Circuit.Expose(Done);
    public IPrimitiveReadPort Participation => PresencePorts.Any(ParticipationSites.Select(Circuit.Expose).ToArray());

    public RepeatedBody(int[] reachReferences, int phasePositions,
        IPrimitiveReadPort? request = null, IPrimitiveReadPort? stop = null, bool continuous = false)
    {
        ArgumentNullException.ThrowIfNull(reachReferences);
        if (reachReferences.Length is < 1 or > 8 || reachReferences.Any(r => r is < 0 or > 63))
            throw new ArgumentException("Supply one to eight shared local parameter references in 0..63.", nameof(reachReferences));
        if (request is not null) Idle = Circuit.State(true);
        ParticipationSites = new int[reachReferences.Length];
        TurnSites = new int[reachReferences.Length];
        for (var i = 0; i < reachReferences.Length; i++)
        {
            ParticipationSites[i] = Circuit.State(i == 0 && Idle is null);
            TurnSites[i] = Circuit.State();
            // Roles: local activation port, local parameter-bank reference.
            parameters.Append(6, ParticipationSites[i], reachReferences[i]);
        }
        Check = Circuit.State();
        Done = Circuit.State();
        Request = Circuit.Guard(request is null);
        Stop = Circuit.Guard();
        var always = Circuit.Guard();
        var turning = PresencePorts.Any(TurnSites.Select(Circuit.Expose).ToArray());
        Phase = new(phasePositions, turning);
        if (request is not null) Circuit.Bind(Request, request);
        if (stop is not null) Circuit.Bind(Stop, stop);
        else if (!continuous) Circuit.Bind(Stop, Phase.AtStart);
        ForwardTable = Circuit.AddTable(PassageLaw.Unconditional());
        PresentTable = Circuit.AddTable(PassageLaw.Controlled());
        AbsentTable = Circuit.AddTable(PassageLaw.Controlled(false));
        for (var i = 0; i < reachReferences.Length; i++)
        {
            Circuit.Link(ForwardTable, ParticipationSites[i], always, TurnSites[i]);
            Circuit.Link(ForwardTable, TurnSites[i], always,
                i + 1 < reachReferences.Length ? ParticipationSites[i + 1] : Check);
        }
        Circuit.Link(PresentTable, Check, Stop, Done);
        Circuit.Link(AbsentTable, Check, Stop, ParticipationSites[0]);
        if (Idle is int idle)
        {
            Circuit.Link(PresentTable, idle, Request, ParticipationSites[0]);
            Circuit.Link(AbsentTable, Done, Request, idle);
        }
    }

    /// <summary>Explicit order: phase observes the body's turn request, then
    /// body advances. Fixed turn rows are unconditional; uncertainty is tested
    /// at the separate check site. This is not a general cross-owner transaction.</summary>
    public BodyStep Pulse()
    {
        var active = Circuit.Active;
        var phase = Phase.Position;
        int? parameter = null;
        var index = Array.IndexOf(ParticipationSites, active ?? -1);
        if (index >= 0) parameter = parameters.Read(new(2, 6, index))[1];
        var phaseStep = Phase.Circuit.Pulse();
        if (!phaseStep.Committed) return new(null, phase, false, false, active, active);
        var bodyStep = Circuit.Pulse();
        return new(bodyStep.Moved ? parameter : null, phase,
            bodyStep.Moved && bodyStep.After == Done, bodyStep.Committed, bodyStep.Before, bodyStep.After);
    }

    public byte[] ParameterBytes() => parameters.PackedBytes(2, 6);
    public int[] ParameterRow(int index) => parameters.Read(new(2, 6, index));

    /// <summary>Connect the exposed phase sites to successive parts of an
    /// exposed contour traversal. This supplies the additional geometric basis;
    /// a clock cycle alone establishes no angle. Bind once for this fixture.</summary>
    public void BindTurns(ContourPattern contour)
    {
        ArgumentNullException.ThrowIfNull(contour);
        if (turnContour is not null) throw new InvalidOperationException("Turn basis is already bound.");
        if (contour.PartCount != Phase.Sites.Length) throw new ArgumentException("Match every phase to one contour part.");
        var rows = Enumerable.Range(0, contour.PartCount).Select(contour.ArcRow).ToArray();
        if (rows.Any(r => r[0] > 63 || r[1] > 63)) throw new ArgumentException("Turn references exceed this layout.");
        for (var i = 0; i < rows.Length; i++) turnBindings.Append(6, Phase.Sites[i], rows[i][0], rows[i][1]);
        turnContour = contour;
    }

    public bool HasEqualTurnBasis
    {
        get
        {
            if (turnContour is null || turnContour.PartCount != Phase.Sites.Length) return false;
            var read = turnContour.Read();
            return read.Circle && read.EqualParts && Enumerable.Range(0, Phase.Sites.Length).All(i =>
                turnBindings.Read(new(3, 6, i)).SequenceEqual(new[]
                    { Phase.Sites[i], turnContour.ArcRow(i)[0], turnContour.ArcRow(i)[1] }));
        }
    }

    public byte[] TurnBytes() => turnBindings.PackedBytes(3, 6);
}

/// <summary>An optional per-pulse witness. Phase and parameter are references,
/// not measured angles or lengths. Completion is an event after body execution.</summary>
public readonly record struct BodyStep(int? Parameter, int Phase, bool Completed, bool Committed, int? Before, int? After);
