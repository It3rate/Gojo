using System.Text.Json.Serialization;
using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;
using Curve = Gojo.Experiments.QualitativeExecution.QualitativeCurve;

namespace Gojo.Experiments.QualitativeFrieze.Composition;

/// <summary>Supplied finite compositions of native actions. Source selection,
/// grouping, output-to-later-input dependencies and periodic-cell correspondence
/// remain in the relation graph. These are copied observations, not material
/// fan-out or autonomous discoveries of repeat laws.</summary>
public static class SymmetryCompositionStudy
{
    // application: existing CentrelineAction grammar (action -> source) -> result.
    // group: Closed(actual child scopes) -> flattened section scope.
    // trace: (seed -> Closed(actual applications/groups in order)) -> final group.
    // repeat rule: (input port -> (action -> output port)) -> (output port -> input port).
    // periodic evidence: (rule -> seed binding) -> witnessed first application.
    // periodic sample: (trace -> cyclic permission) -> periodic evidence.
    // permission: (actual final two-child group -> one-slot translation) ->
    //             Closed(cell quotient, slot+slot receipt, period+wrap receipt,
    //                    wrap application, wrap round-trip match).
    // quotient: (actual cell -> continued cell) -> (period action -> wrap action).
    // This explicit quotient is a read permission. Forward feedback by itself
    // neither identifies period-separated cells nor declares backward time.
    // comparison: (actual source -> actual target) ->
    //             Closed(alignment evidence, raw read, aligned read, directed read).
    // No machine-mode bit determines an application. The actual source reference
    // in each application is the dependency, including a previous group/output.

    public sealed record Sample(string Id, string Motif, string Name, string Rule,
        int Steps, int Sections, long[][][] Lines)
    {
        [JsonIgnore] public Curve.Frame Frame { get; init; } = null!;
        [JsonIgnore] public Relation Surface { get; init; } = null!;
        [JsonIgnore] public Relation Output { get; init; } = null!;
        [JsonIgnore] public Relation[] Blocks { get; init; } = [];
    }
    public sealed record Comparison(string Id, string Name, string From, string To,
        bool RawEqual, bool AlignedEqual, bool DirectedEqual, int ShiftSteps,
        string Note, long[][][] AlignedLines)
    {
        [JsonIgnore] public Relation Surface { get; init; } = null!;
    }
    public sealed record Report(Sample[] Samples, Comparison[] Comparisons);
    public sealed record CyclicCellRead(Relation Surface, Relation Cell, Relation[] Slots,
        Relation SlotAction, Relation PeriodAction, Relation WrapAction, Relation Permission);

    public static Report Explore()
    {
        var frame = new Curve.Frame(new QualitativeMotionFrame());
        var wing = Path(frame, (0, 0), (2, 2), (3, 2), (3, 1), (2, 0), (0, 0));
        var corner = Path(frame, (0, 0), (2, 0), (2, 1));
        var zigzag = Path(frame, (0, 0), (1, 1), (2, 0), (3, 1), (4, 0));
        var samples = new List<Sample>(); var comparisons = new List<Comparison>();
        var glide = Action(frame, 4, 6, 0);

        // Same action and three requests; only the actual source dependency is
        // changed. Reusing the original with a fixed placement really stamps
        // the same location again. Its duplicate occurrences remain counted.
        var originalTrace = new Trace(frame, wing); var originalBlocks = new List<Relation> { wing };
        for (var i = 0; i < 3; i++) originalBlocks.Add(originalTrace.Apply(wing, glide));
        var original = originalTrace.Finish("original-fixed", "angular wing", "Reuse the original, fixed placement",
            "Every request reads the original wing through the same reflection-and-advance action; repeated copies overlap.", originalBlocks);
        samples.Add(original);

        var feedbackTrace = new Trace(frame, wing); var feedbackBlocks = new List<Relation> { wing }; var current = wing;
        for (var i = 0; i < 3; i++) { current = feedbackTrace.Apply(current, glide); feedbackBlocks.Add(current); }
        var feedback = feedbackTrace.Finish("feedback", "angular wing", "Reuse the preceding result",
            "Each output becomes the next action's input. Advance accumulates and vertical orientation alternates.", feedbackBlocks);
        samples.Add(feedback);

        var groupTrace = new Trace(frame, wing); current = wing;
        for (var i = 0; i < 3; i++) current = groupTrace.Group(current, groupTrace.Apply(current, glide));
        var group = groupTrace.Finish("whole-group", "angular wing", "Reuse the entire accumulated group",
            "Keep the current group and add its transformed copy. Section participation doubles per round; coincident copies are not merged.", [current]);
        samples.Add(group);

        var clockTrace = new Trace(frame, wing); var clockBlocks = new List<Relation> { wing };
        for (var i = 1; i <= 3; i++) clockBlocks.Add(clockTrace.Apply(wing, Action(frame, 4, i * 6, 0)));
        var clock = clockTrace.Finish("original-clock", "angular wing", "Reuse the original with advancing placement",
            "The original is unchanged; supplied placement steps advance each copy. A fixed reflection does not create alternating orientation.", clockBlocks);
        samples.Add(clock);

        Sample Ordered(string id, string name, Relation first, Relation second, string rule)
        {
            var trace = new Trace(frame, corner);
            var firstResult = trace.Apply(corner, first);
            var secondResult = trace.Apply(firstResult, second);
            var sample = trace.Finish(id, "asymmetric corner", name, rule, [secondResult]); samples.Add(sample); return sample;
        }
        var advance = Action(frame, 0, 6, 0); var verticalMirror = Action(frame, 4, 0, 0); var horizontalMirror = Action(frame, 2, 0, 0);
        var mirrorAdvance = Ordered("mirror-y-advance", "Reflect vertical, then advance", verticalMirror, advance,
            "The horizontal displacement is unchanged by this vertical reflection.");
        var advanceMirror = Ordered("advance-mirror-y", "Advance, then reflect vertical", advance, verticalMirror,
            "The same two native actions in the other order; this selected perpendicular displacement commutes.");
        var horizontalFirst = Ordered("mirror-x-advance", "Reflect horizontal, then advance", horizontalMirror, advance,
            "The mirror opposes horizontal coordinates before the positive displacement is added.");
        var horizontalLast = Ordered("advance-mirror-x", "Advance, then reflect horizontal", advance, horizontalMirror,
            "The final mirror also reverses the displacement; these actions do not commute.");

        var phaseLeft = PhaseCell(frame, wing, false, false); samples.Add(phaseLeft);
        var phaseRight = PhaseCell(frame, wing, true, false); samples.Add(phaseRight);
        var broken = PhaseCell(frame, wing, true, true); samples.Add(broken);
        var zigzagTrace = new Trace(frame, zigzag); var zigzagBlocks = new List<Relation> { zigzag }; current = zigzag;
        for (var i = 0; i < 3; i++) { current = zigzagTrace.Apply(current, glide); zigzagBlocks.Add(current); }
        samples.Add(zigzagTrace.Finish("zigzag-feedback", "zigzag", "The same feedback action on a zigzag",
            "The source shape changes; the connected reflection-and-advance action is reused.", zigzagBlocks));

        comparisons.Add(Compare("source-choice", "Changing which structure is reused", original, feedback,
            "The action is shared, but source dependencies differ. A fixed original accumulates coincident copies, not an advancing strip."));
        comparisons.Add(Compare("group-choice", "Grouping changes participation", feedback, group,
            "The occupied locations can resemble one another, but the growing group has overlapping multiplicity. Ink is not silently unioned."));
        comparisons.Add(Compare("placement-clock", "Placement is separate from feedback", feedback, clock,
            "Both advance across the page. Only output feedback alternates the previous result's orientation."));
        comparisons.Add(Compare("commuting", "These two actions commute in this basis", mirrorAdvance, advanceMirror,
            "Vertical reflection and horizontal translation agree in either order for this connected frame."));
        comparisons.Add(Compare("noncommuting", "The displacement is reflected too", horizontalFirst, horizontalLast,
            "Horizontal reflection and horizontal advance disagree. Relaxing phase is not permission to discard this changed displacement."));

        var shiftTrace = new Trace(frame, phaseRight.Output);
        var shifted = shiftTrace.Apply(phaseRight.Output, advance);
        comparisons.Add(Compare("finite-phase", "A finite translated crop still has different ends", phaseLeft, phaseRight,
            "A one-slot translation aligns the interior cadence but moves the finite cell's end. No crop is silently discarded.", shifted, shiftTrace.Surface(shifted), 1));
        comparisons.Add(CyclicPhase(frame, phaseLeft, phaseRight, "cyclic-phase", "Relax phase under a supplied periodic continuation"));
        comparisons.Add(CyclicPhase(frame, phaseLeft, broken, "broken-phase", "A changed motif cannot be explained by phase alone"));
        return new(samples.ToArray(), comparisons.ToArray());
    }

    private static Sample PhaseCell(Curve.Frame frame, Relation seed, bool reverse, bool mark)
    {
        var trace = new Trace(frame, seed);
        var reflected = trace.Apply(seed, Action(frame, 4, 0, 0));
        if (mark) reflected = trace.Group(reflected, Path(frame, (4, 0), (4, -1)));
        var first = reverse ? reflected : seed;
        var slotAction = Action(frame, 0, 6, 0);
        var second = trace.Apply(reverse ? seed : reflected, slotAction);
        var sample = trace.Finish(mark ? "phase-broken" : reverse ? "phase-right" : "phase-left", "alternating angular wings",
            mark ? "Opposite phase with an added mark" : reverse ? "Mirror / original cell" : "Original / mirror cell",
            "Two ordered motif positions form a supplied cell. Two placement steps supply its period; a separate cyclic read permission allows wrapping.", [first, second]);
        // The continuation rule is explicit rather than inferred from a finite
        // picture: an exposed output feeds the same transformation's next input.
        var slotWord = (Relation)((Relation)slotAction.Right).Left;
        var slotSum = frame.Motion.Reduce(slotWord, slotWord);
        if (!frame.Motion.TryReadResult(slotSum, out var periodWord, out _, out var pending) || E(pending).Length != 0)
            throw new InvalidOperationException("The supplied cell period is unresolved.");
        var period = CentrelineAction.FromBits(frame, 0, periodWord);
        var input = new Placeholder(); var output = new Placeholder();
        var rule = new Relation(new Relation(input, new Relation(period, output)), new Relation(output, input));
        var continuation = CentrelineAction.Apply(frame, period, sample.Output);
        var evidence = new Relation(new Relation(rule, new Relation(input, sample.Output)), continuation);
        if (!CentrelineAction.TryReadApplication(frame, continuation, out var continued) || !continued.Complete)
            throw new InvalidOperationException("The cell continuation is unresolved.");

        var wrapWord = Oppose(frame, periodWord);
        var wrap = CentrelineAction.FromBits(frame, 0, wrapWord);
        var cancellation = frame.Motion.Reduce(periodWord, wrapWord);
        var wrapApplication = CentrelineAction.Apply(frame, wrap, continued.Output);
        var roundtrip = CentrelineAction.Match(frame, wrapApplication, sample.Output);
        var history = E((Relation)((Relation)sample.Surface.Left).Right);
        var group = (Relation)history[^1];
        var quotient = new Relation(new Relation(sample.Output, continued.Output), new Relation(period, wrap));
        var permission = new Relation(new Relation(group, slotAction), S(quotient, slotSum, cancellation, wrapApplication, roundtrip));
        var surface = new Relation(new Relation(sample.Surface, permission), evidence);
        if (!TryReadCyclicCell(frame, surface, out _)) throw new InvalidOperationException("The cyclic-cell permission did not validate.");
        return sample with { Surface = surface };
    }

    private static Comparison CyclicPhase(Curve.Frame frame, Sample target, Sample candidate, string id, string name)
    {
        if (!TryAlignCyclicCell(frame, candidate.Surface, out var aligned, out var alignment))
            throw new InvalidOperationException("The requested cyclic phase lacks a valid permission.");
        // Candidate continuation and the actual per-block applications remain
        // connected. This is a supplied one-period correspondence, not a search
        // over crop boundaries or a claim about arbitrary infinite patterns.
        return Compare(id, name, target, candidate,
            "One connected placement action shifts the selected phase; its two-step period supplies the validated inverse wrap. This needs the explicit cyclic-cell quotient permission, not forward continuation alone, and preserves every section occurrence.",
            aligned, alignment, 1);
    }

    /// <summary>Reads only the declared two-slot translation grammar. Actual
    /// group children, continuation ports, period construction and inverse wrap
    /// are rechecked from the native surface. No report labels or Blocks cache
    /// authorize a cyclic read, and an open inventory stays unresolved.</summary>
    public static bool TryReadCyclicCell(Curve.Frame frame, Relation surface, out CyclicCellRead read)
    {
        read = null!;
        if (surface.Left is not Relation { Left: Relation trace, Right: Relation permission } ||
            trace.Left is not Relation { Right: Relation timeline } || trace.Right is not Relation cell ||
            !Closed(timeline, out var history) || history.Length == 0 ||
            history[^1] is not Relation { Left: Relation children } group || !ReferenceEquals(group.Right, cell) ||
            !Closed(children, out var slots) || slots.Length != 2 || slots.Any(s => s is not Relation) ||
            !Closed(cell, out var output)) return false;
        var all = new List<IElement>();
        foreach (var slot in slots.Cast<Relation>())
        {
            if (!Closed(slot, out var entries) || entries.Length == 0 ||
                entries.Any(e => e is not Relation section || !frame.Read(section).Complete)) return false;
            all.AddRange(entries);
        }
        if (all.Count != output.Length || all.Where((item, i) => !ReferenceEquals(item, output[i])).Any()) return false;
        if (permission.Left is not Relation { Right: Relation slotAction } selected ||
            !ReferenceEquals(selected.Left, group) || permission.Right is not Relation body ||
            !Closed(body, out var facts) || facts.Length != 5 || facts.Any(f => f is not Relation) ||
            !Translation(frame, slotAction, out var slotWord) || !frame.Vector(slotWord, out var slotVector) || slotVector.Zero)
            return false;

        // The selected slot action must be the actual action that placed the
        // second native child, rather than a detached proposed displacement.
        if (!history.OfType<Relation>().Any(item => CentrelineAction.TryReadApplication(frame, item, out var placed) &&
            placed.Complete && ReferenceEquals(placed.Action, slotAction) && ReferenceEquals(placed.Output, slots[1]))) return false;

        if (surface.Right is not Relation { Left: Relation declaration, Right: Relation continuation } ||
            declaration.Left is not Relation { Left: Relation ports, Right: Relation feedback } ||
            ports.Right is not Relation { Left: Relation period } instruction ||
            ReferenceEquals(ports.Left, instruction.Right) ||
            !ReferenceEquals(feedback.Left, instruction.Right) || !ReferenceEquals(feedback.Right, ports.Left) ||
            declaration.Right is not Relation binding || !ReferenceEquals(binding.Left, ports.Left) || !ReferenceEquals(binding.Right, cell) ||
            !Translation(frame, period, out var periodWord) ||
            !CentrelineAction.TryReadApplication(frame, continuation, out var continued) || !continued.Complete ||
            !ReferenceEquals(continued.Action, period) || !ReferenceEquals(continued.Source, cell)) return false;

        if (facts[0] is not Relation { Left: Relation quotientCells, Right: Relation quotientActions } ||
            !ReferenceEquals(quotientCells.Left, cell) || !ReferenceEquals(quotientCells.Right, continued.Output) ||
            !ReferenceEquals(quotientActions.Left, period) || quotientActions.Right is not Relation wrap ||
            !Translation(frame, wrap, out var wrapWord) ||
            !Sum(frame, (Relation)facts[1], slotWord, slotWord, out var doubled) || !ReferenceEquals(doubled, periodWord) ||
            !Sum(frame, (Relation)facts[2], periodWord, wrapWord, out var cancelled) || E(cancelled).Length != 0 ||
            !CentrelineAction.TryReadApplication(frame, (Relation)facts[3], out var returned) || !returned.Complete ||
            !ReferenceEquals(returned.Action, wrap) || !ReferenceEquals(returned.Source, continued.Output) ||
            !CentrelineAction.TryReadMatch(frame, (Relation)facts[4], out var roundtrip) || !roundtrip.Complete ||
            !roundtrip.GeometryExact || !roundtrip.SitePatternExact ||
            !ReferenceEquals(roundtrip.Application, facts[3]) || !ReferenceEquals(roundtrip.Target, cell)) return false;
        read = new(surface, cell, slots.Cast<Relation>().ToArray(), slotAction, period, wrap, permission); return true;
    }

    /// <summary>A permitted one-slot cyclic read. False returns open output,
    /// retaining the invalid/open request as evidence instead of guessing phase.</summary>
    public static bool TryAlignCyclicCell(Curve.Frame frame, Relation surface, out Relation aligned, out Relation evidence)
    {
        aligned = StructuralConstraint.Open(); evidence = new Relation(surface, aligned);
        if (!TryReadCyclicCell(frame, surface, out var cell)) return false;
        var trace = new Trace(frame, cell.Cell);
        var first = trace.Apply(cell.Slots[0], cell.SlotAction);
        var beyond = trace.Apply(cell.Slots[1], cell.SlotAction);
        var wrapped = trace.Apply(beyond, cell.WrapAction);
        aligned = trace.Group(wrapped, first);
        evidence = new Relation(surface, trace.Surface(aligned)); return true;
    }

    private static bool Translation(Curve.Frame frame, Relation action, out Relation displacement)
    {
        displacement = null!;
        if (!CentrelineAction.TryBits(frame, action, out var bits) || bits != 0 ||
            action.Right is not Relation { Left: Relation word } || !frame.Vector(word, out _)) return false;
        displacement = word; return true;
    }

    private static bool Sum(Curve.Frame frame, Relation receipt, Relation before, Relation incoming, out Relation result)
    {
        result = null!;
        return receipt.Left is Relation { Right: Relation sources } && ReferenceEquals(sources.Left, before) &&
            ReferenceEquals(sources.Right, incoming) && frame.Motion.TryReadResult(receipt, out result, out _, out var pending) &&
            Closed(result, out _) && E(pending).Length == 0;
    }

    private static Relation Oppose(Curve.Frame frame, Relation word) => S(E(word).Cast<Relation>().Select(move =>
    {
        if (!frame.Motion.Direction(move, out var axis, out var forward, out var reach)) throw new ArgumentException("Unreadable translation.", nameof(word));
        return (IElement)QualitativeMotionFrame.Move(ReferenceEquals(axis, frame.Motion.Horizontal)
            ? forward ? frame.Motion.Left : frame.Motion.Right : forward ? frame.Motion.Down : frame.Motion.Up, reach);
    }).ToArray());

    public static Comparison Compare(string id, string name, Sample from, Sample to, string note,
        Relation? aligned = null, Relation? alignment = null, int shiftSteps = 0)
    {
        var frame = from.Frame; aligned ??= to.Output; alignment ??= S();
        var raw = BoundaryExchangeRead.Evaluate(frame, from.Output, to.Output);
        var read = BoundaryExchangeRead.Evaluate(frame, from.Output, aligned);
        var directed = BoundaryExchangeRead.Evaluate(frame, from.Output, aligned, directed: true);
        return new(id, name, from.Id, to.Id, raw.Equivalent, read.Equivalent, directed.Equivalent,
            shiftSteps, note, Lines(frame, aligned))
        {
            Surface = new Relation(new Relation(from.Surface, to.Surface), S(alignment, raw.Surface, read.Surface, directed.Surface))
        };
    }

    private sealed class Trace(Curve.Frame frame, Relation seed)
    {
        private readonly List<IElement> history = [];
        private int applications;
        public Relation Apply(Relation source, Relation action)
        {
            var application = CentrelineAction.Apply(frame, action, source);
            if (!CentrelineAction.TryReadApplication(frame, application, out var read) || !read.Complete)
                throw new InvalidOperationException("A supplied finite symmetry application did not read.");
            history.Add(application); applications++; return read.Output;
        }
        public Relation Group(params Relation[] children)
        {
            var result = S(children.SelectMany(E).ToArray());
            history.Add(new Relation(S(children), result)); return result;
        }
        public Relation Surface(Relation output) => new(new Relation(seed, S(history.ToArray())), output);
        public Sample Finish(string id, string motif, string name, string rule, IEnumerable<Relation> blocks)
        {
            var actualBlocks = blocks.ToArray(); var output = Group(actualBlocks);
            return new(id, motif, name, rule, applications, E(output).Length, Lines(frame, output))
            { Frame = frame, Surface = Surface(output), Output = output, Blocks = actualBlocks };
        }
    }

    private static Relation Action(Curve.Frame frame, byte bits, int x, int y) => CentrelineAction.FromBits(frame, bits, frame.Word(x, y));
    private static Relation Path(Curve.Frame frame, params (int X, int Y)[] points)
    {
        var sites = points.Select(_ => (IElement)new Placeholder()).ToArray();
        if (points.Length > 1 && points[0] == points[^1]) sites[^1] = sites[0];
        return S(Enumerable.Range(0, points.Length - 1).Select(i => (IElement)frame.Line(sites[i], sites[i + 1],
            frame.Word(points[i].X, points[i].Y), frame.Word(points[i + 1].X, points[i + 1].Y))).ToArray());
    }
    private static long[][][] Lines(Curve.Frame frame, Relation scope) => E(scope).Cast<Relation>().Select(frame.Read)
        .Select(section => section.Controls.Select(p => new[] { checked((long)p.X), checked((long)p.Y) }).ToArray()).ToArray();
    private static Relation S(params IElement[] entries) => StructuralConstraint.Closed(entries);
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
    private static bool Closed(Relation scope, out IElement[] entries) =>
        StructuralConstraint.TryReadScope(scope, out entries, out var seam, out var tail) && seam.Equals(tail);
}
