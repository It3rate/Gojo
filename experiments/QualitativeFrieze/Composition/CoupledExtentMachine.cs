using Gojo.Core;
using Gojo.Patterns;
using Gojo.Structures;
using Gojo.Experiments.QualitativeExecution;

namespace Gojo.Experiments.QualitativeFrieze.Composition;

/// <summary>
/// A bounded local actuator. Its public input is a ternary comparison publication;
/// it never reads a supplier's target, private extent or geometry. A native finite
/// law selects a signed step, an explicit outlet admits that step, and the ordinary
/// carrier clamp retains any unabsorbed tension. The host supplies clock arrivals.
/// This is an authored mechanism, not an inferred controller or a geometric solver.
/// </summary>
public sealed class CoupledExtentMachine
{
    // Surface = Closed(owner, sensor, extent-port, attached-clamp, comparison-basis,
    //   comparison-domain, phase-port, time-port, finite-law, effect-outlets,
    //   step-unit, timeline-holder)
    // value-port = actual live value location -> owning carrier
    // effect-outlet = signed-step-location -> (extent-port -> unit-correspondence)
    // archive = Closed(Surface, Closed(consumed event identities), Closed(steps))
    // Comparison coordinates -1/0/+1 mean decrease/equal/increase respectively.
    // They are published ordinal relations, not the supplier's numeric magnitude.
    // All policies differ in actual finite-law rows. Execution never dispatches
    // on a policy name, a shape name, or a requested final picture.

    private readonly Carrier extent;
    private readonly Carrier phase;
    private readonly Carrier time;
    private readonly Carrier journal;
    private readonly Relation effects;
    private readonly List<IElement> events;
    private readonly List<Relation> history;
    private readonly IElement[] comparisonStates;
    private readonly IElement[] phaseStates;
    private readonly FiniteLawPlan plan;

    public Relation Surface { get; }
    public Relation ExtentPort { get; }
    /// <summary>An explicitly advertised signed pending-adjustment surface.
    /// Observing it does not withdraw debt or promise eventual settlement.</summary>
    public Relation PendingPort => new(extent.LiveTensionSlot, extent);
    public IElement Owner { get; }
    public IElement Sensor { get; }
    public Carrier ComparisonBasis { get; }
    public Relation ComparisonDomain { get; }
    public Relation Law { get; }
    public Carrier StepUnit { get; }
    public IReadOnlyList<Relation> History => history.AsReadOnly();

    public sealed record SnapshotRead(long Value, long Tension, long Beat, long Phase, Relation Surface);
    public sealed record StepRead(SnapshotRead Before, SnapshotRead After, long? RequestedDelta,
        long IntroducedDelta, long AcceptedDelta, long PendingDelta, long UnroutedDelta,
        bool Changed, bool Resolved, bool Blocked, bool ConsumedEvent, Relation Surface, Relation LawRead);

    private CoupledExtentMachine(Relation surface, List<IElement> events, List<Relation> history)
    {
        var rows = Closed(surface);
        if (rows.Length != 12 || rows[2] is not Relation extentPort || !ReadPort(extentPort, out extent) ||
            rows[3] is not Relation boundary || !extent.IsAttached(boundary) ||
            !ReadClamp(boundary, extent) || rows[4] is not Carrier comparisonBasis ||
            rows[5] is not Relation domain || rows[6] is not Relation phasePort || !ReadPort(phasePort, out phase) ||
            rows[7] is not Relation timePort || !ReadPort(timePort, out time) ||
            rows[8] is not Relation law || rows[9] is not Relation outlets || rows[10] is not Carrier unit ||
            rows[11] is not Carrier holder || !FiniteLaw.TryRead(law, out var left, out var right, out var output, out _) ||
            !ReferenceEquals(left, domain) || !unit.UnitShape.Equals(extent.UnitShape))
            throw new ArgumentException("Expected the exact local extent-machine surface.", nameof(surface));
        comparisonStates = Closed(domain); phaseStates = Closed(right);
        var outputs = Closed(output);
        if (!Coordinates(comparisonStates, comparisonBasis, [-1, 0, 1]) ||
            !Coordinates(phaseStates, phase, [0, 1]) || !Coordinates(outputs, unit, [-1, 0, 1]) ||
            !phase.AttachedRelations.Any(r => ReadWrap(r, phase)) ||
            Closed(outlets).Any(r => r is not Relation { Left: CarrierLocation, Right: Relation }))
            throw new ArgumentException("The machine's comparison, phase or step contract is incomplete.", nameof(surface));
        Surface = surface; Owner = rows[0]; Sensor = rows[1]; ExtentPort = extentPort;
        ComparisonBasis = comparisonBasis; ComparisonDomain = domain; Law = law; effects = outlets;
        StepUnit = unit; journal = holder; this.events = events; this.history = history;
        plan = FiniteLawPlan.Capture(law) ?? throw new ArgumentException("The finite law cannot be compiled.", nameof(surface));
    }

    /// <summary>Policy names select authored row sets only; no name survives as
    /// the source of runtime meaning. A shared unit establishes comparable steps,
    /// not a live binding between independently created extent carriers.</summary>
    public static CoupledExtentMachine Create(long initial = 0, long capacity = 8,
        string policy = "feedback", bool allowDecrease = true, bool allowIncrease = true,
        UnitShape? unit = null)
    {
        if (initial < 0 || capacity < 0 || initial > capacity)
            throw new ArgumentOutOfRangeException(nameof(initial), "Initial extent must fit the nonnegative local capacity.");
        if (policy is not ("feedback" or "hold" or "clock"))
            throw new ArgumentException("Select feedback, hold or clock for this authored fixture.", nameof(policy));
        var extent = unit is null ? new Carrier() : new Carrier(unit);
        var clamp = new Relation(Relation.Point(extent, capacity), Relation.Point(extent, capacity));
        extent.Attach(clamp); extent.Add((ulong)initial);
        var extentPort = new Relation(extent.ValueSlot, extent);
        var phase = new Carrier(); phase.Attach(new Relation(Relation.Point(phase, 2), Relation.Point(phase, 0)));
        var time = new Carrier(); var comparison = new Carrier(); var step = new Carrier(extent.UnitShape);
        var signs = new long[] { -1, 0, 1 };
        var comparisons = signs.Select(n => (IElement)new CarrierLocation(comparison, n)).ToArray();
        var phases = new IElement[] { new CarrierLocation(phase, 0), new CarrierLocation(phase, 1) };
        var deltas = signs.Select(n => (IElement)new CarrierLocation(step, n)).ToArray();
        var left = S(comparisons); var right = S(phases); var output = S(deltas);
        var rows = new List<Relation>();
        for (var a = 0; a < 3; a++)
            for (var b = 0; b < 2; b++)
            {
                var delta = policy == "feedback" ? a : policy == "hold" ? 1 : b == 0 ? 2 : 0;
                rows.Add(FiniteLaw.Row(comparisons[a], phases[b], S(deltas[delta])));
            }
        var law = FiniteLaw.Create(left, right, output, rows.ToArray());
        var calibration = new Relation(Relation.Segment(step, 0, 1), Relation.Segment(extent, 0, 1));
        var outlets = deltas.Cast<CarrierLocation>().Where(d => d.Ticks == 0 || d.Ticks < 0 && allowDecrease || d.Ticks > 0 && allowIncrease)
            .Select(d => (IElement)new Relation(d, new Relation(extentPort, calibration))).ToArray();
        return new CoupledExtentMachine(S(new Placeholder(), new Placeholder(), extentPort, clamp,
            comparison, left, new Relation(phase.ValueSlot, phase), new Relation(time.ValueSlot, time),
            law, S(outlets), step, new Carrier()), [], []);
    }

    /// <summary>Publishes only the comparison support, scoped to this sensor and
    /// receiver. Bits 0/1/2 address decrease/equal/increase. Zero means a missing
    /// observation (open support); seven supplies all three known alternatives.</summary>
    public Relation Publication(byte comparisonMask, IElement? eventIdentity = null, IElement? evidence = null)
    {
        if (comparisonMask > 7) throw new ArgumentOutOfRangeException(nameof(comparisonMask));
        var support = comparisonMask == 0 ? StructuralConstraint.Open() :
            S(comparisonStates.Where((_, i) => (comparisonMask & 1 << i) != 0).ToArray());
        return QualitativePublication.Create(Sensor, Owner, eventIdentity ?? new Placeholder(),
            ComparisonBasis, ComparisonDomain, support, evidence);
    }

    public SnapshotRead Snapshot() => new(checked((long)extent.Value), extent.Tension,
        checked((long)time.Value), checked((long)phase.Value), S(
            new CarrierLocation(extent, checked((long)extent.Value)), new CarrierLocation(extent, extent.Tension),
            new CarrierLocation(time, checked((long)time.Value)), new CarrierLocation(phase, checked((long)phase.Value))));

    /// <summary>A supplied pulse attempts one local transition. No pulse means
    /// no event consumption. Missing/competing input advances observed time but
    /// does not invent a command. Each admitted signed step is a new tension
    /// arrival; a full actuator may therefore accumulate explicitly counted debt.</summary>
    public StepRead Step(Relation publication, bool compiled = false, bool pulse = true)
    {
        var before = Snapshot();
        var valid = QualitativePublication.TryRead(publication, out var source, out var subject,
            out var occurrence, out var basis, out var domain, out var support, out _) &&
            Same(source, Sensor) && Same(subject, Owner) && Same(basis, ComparisonBasis) && ReferenceEquals(domain, ComparisonDomain);
        var replay = valid && events.Any(e => Same(e, occurrence));
        var phaseSupport = S(phaseStates.Single(p => ((CarrierLocation)p).Ticks == before.Phase));
        var input = valid ? support : StructuralConstraint.Open(publication);
        var lawRead = compiled ? plan.Evaluate(input, phaseSupport) : FiniteLaw.Evaluate(Law, input, phaseSupport);
        var outputs = Array.Empty<IElement>(); var pending = Array.Empty<IElement>();
        var readable = FiniteLaw.TryReadResult(lawRead, out var answer, out var residue, out _);
        var resolved = valid && !replay && readable && TryClosed(answer, out outputs) &&
            TryClosed(residue, out pending) && pending.Length == 0 && outputs.Length == 1 &&
            outputs[0] is CarrierLocation delta && ReferenceEquals(delta.Carrier, StepUnit);
        long? requested = resolved ? ((CarrierLocation)outputs[0]).Ticks : null;
        var admitted = 0L; var unrouted = 0L; IElement routeEvidence = S();
        var consumed = pulse && valid && !replay;
        if (consumed)
        {
            if (resolved)
            {
                var routes = Closed(effects).Cast<Relation>().Where(r => Same(r.Left, outputs[0])).ToArray();
                if (routes.Length == 1 && ValidOutlet(routes[0]))
                {
                    admitted = requested!.Value; routeEvidence = routes[0];
                    if (admitted > 0) extent.Add((ulong)admitted);
                    else if (admitted < 0) extent.Remove((ulong)-admitted);
                }
                else { unrouted = requested!.Value; routeEvidence = StructuralConstraint.Open(outputs[0]); }
            }
            events.Add(occurrence); time.Add(1); phase.Add(1);
        }
        else if (requested is not null) unrouted = requested.Value;
        var after = Snapshot();
        var accepted = after.Value - before.Value; var held = after.Tension - before.Tension;
        if (admitted != accepted + held) throw new InvalidOperationException("The local clamp lost signed participation.");
        var requestEvidence = requested is null ? StructuralConstraint.Open(lawRead) :
            S(new CarrierLocation(StepUnit, requested.Value));
        var accounting = S(new CarrierLocation(StepUnit, admitted), new CarrierLocation(StepUnit, accepted),
            new CarrierLocation(StepUnit, held), new CarrierLocation(StepUnit, unrouted));
        var transition = new Relation(new Relation(publication, lawRead), new Relation(routeEvidence, requestEvidence));
        var remainder = S(accounting, resolved ? S() : StructuralConstraint.Open(publication),
            replay ? S(occurrence) : S(), pulse ? S() : StructuralConstraint.Open(new Relation(publication, time)));
        var witness = TimeStepSequence.Step(journal, time, before.Beat, after.Beat,
            before.Surface, transition, after.Surface, remainder);
        history.Add(witness);
        return new(before, after, requested, admitted, accepted, held, unrouted,
            accepted != 0 || held != 0, resolved, !consumed || !resolved || unrouted != 0 || held != 0,
            consumed, witness, lawRead);
    }

    /// <summary>The native graph archive includes retained history. Its byte
    /// count is a transport cost, not a claim about minimal controller bits.</summary>
    public Relation ArchiveSurface() => S(Surface, S(events.ToArray()), S(history.Cast<IElement>().ToArray()));

    public byte[] Encode() => GojoNumericFormat.Encode(ArchiveSurface());

    public static CoupledExtentMachine Decode(ReadOnlySpan<byte> bytes)
    {
        var roots = GojoNumericFormat.Decode(bytes);
        if (roots is not [Relation archive]) throw new ArgumentException("Expected one local-machine archive root.", nameof(bytes));
        return Restore(archive);
    }

    /// <summary>Rebinds one already restored local machine. A composed world can
    /// archive several machines and their connections in one numeric graph, then
    /// call this reader on each root without breaking shared unit or port aliases.</summary>
    public static CoupledExtentMachine Restore(Relation archive)
    {
        var rows = Closed(archive);
        if (rows is not [Relation surface, Relation eventScope, Relation historyScope])
            throw new ArgumentException("Expected surface, event history and transition history.", nameof(archive));
        var events = Closed(eventScope); var steps = Closed(historyScope);
        if (steps.Any(step => step is not Relation) || events.Distinct(ReferenceEqualityComparer.Instance).Count() != events.Length)
            throw new ArgumentException("Malformed retained machine history.", nameof(archive));
        return new(surface, events.ToList(), steps.Cast<Relation>().ToList());
    }

    /// <summary>Diagnostic conformance check over all finite input supports and
    /// both phases. Open input is checked separately by the tests.</summary>
    public bool CompactLawAgrees()
    {
        for (byte a = 0; a < 8; a++)
            for (var b = 0; b < 2; b++)
            {
                var support = S(comparisonStates.Where((_, i) => (a & 1 << i) != 0).ToArray());
                var phaseSupport = S(phaseStates[b]);
                var native = FiniteLaw.Evaluate(Law, support, phaseSupport);
                if (!FiniteLaw.TryReadResult(native, out var output, out _, out _) ||
                    !plan.TryEncodeLeft(support, out var left) || !plan.TryEncodeRight(phaseSupport, out var right) ||
                    !plan.TryEncodeOutput(output, out var expected) || !plan.TryEvaluateMasks(left, right, out var actual) || expected != actual)
                    return false;
            }
        return true;
    }

    private bool ValidOutlet(Relation route) => route.Right is Relation linked && ReferenceEquals(linked.Left, ExtentPort) &&
        linked.Right is Relation calibration && calibration.Left is Relation source && calibration.Right is Relation target &&
        Span(source, StepUnit, 0, 1) && Span(target, extent, 0, 1);
    private static bool ReadPort(Relation port, out Carrier carrier)
    {
        carrier = null!;
        if (port is not { Left: CarrierLocation location, Right: Carrier owner } ||
            !ReferenceEquals(location.Carrier, owner) || !location.SameTickRefAs(owner.ValueSlot)) return false;
        carrier = owner; return true;
    }
    private static bool ReadClamp(Relation r, Carrier c) => r.Left is Relation a && r.Right is Relation b &&
        a.Left is CarrierLocation x && b.Left is CarrierLocation y && ReferenceEquals(a.Right, c) &&
        ReferenceEquals(b.Right, c) && ReferenceEquals(x.Carrier, c) && ReferenceEquals(y.Carrier, c) &&
        x.Ticks == y.Ticks && x.Ticks >= 0;
    private static bool ReadWrap(Relation r, Carrier c) => r.Left is Relation a && r.Right is Relation b &&
        Point(a, c, 2) && Point(b, c, 0);
    private static bool Span(Relation r, Carrier c, long a, long b) =>
        r.Left is Relation from && r.Right is Relation to && Point(from, c, a) && Point(to, c, b);
    private static bool Point(Relation r, Carrier c, long n) => r.Left is CarrierLocation p &&
        ReferenceEquals(r.Right, c) && ReferenceEquals(p.Carrier, c) && p.Ticks == n;
    private static bool Coordinates(IElement[] values, Carrier carrier, long[] coordinates) =>
        values.Length == coordinates.Length && values.Select((value, i) => value is CarrierLocation p &&
            ReferenceEquals(p.Carrier, carrier) && p.Ticks == coordinates[i]).All(same => same);
    private static bool Same(IElement a, IElement b) => ReferenceEquals(a, b) || a is Placeholder x && b is Placeholder y && x.Equals(y) ||
        a is CarrierLocation u && b is CarrierLocation v && ReferenceEquals(u.Carrier, v.Carrier) && u.SameTickRefAs(v);
    private static Relation S(params IElement[] entries) => StructuralConstraint.Closed(entries);
    private static IElement[] Closed(Relation scope) => TryClosed(scope, out var entries) ? entries :
        throw new ArgumentException("Expected a closed native scope.");
    private static bool TryClosed(Relation scope, out IElement[] entries) =>
        StructuralConstraint.TryReadScope(scope, out entries, out var seam, out var tail) && seam.Equals(tail);
}
