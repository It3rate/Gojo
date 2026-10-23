using System.Numerics;
using Gojo.Core;
using Gojo.Patterns;
using Gojo.Structures;
using Curve = Gojo.Experiments.QualitativeExecution.QualitativeCurve;

namespace Gojo.Experiments.QualitativeExecution;

/// <summary>A finite, unmeasured ruled-band construction. A spine and two
/// corresponding boundary interpolations share their actual longitudinal
/// parameter; a second bounded parameter traverses between the boundaries.
/// Offsets are relative motion words, not measured radii or exact normal
/// offsets. Bounded support, selected occupancy, visible caps and depth order
/// remain separate relation surfaces.</summary>
public static class QualitativeBand
{
    // sweep = ((frame -> spine) -> (sideA -> sideB)) ->
    //         (Closed(control offset/sum correspondences) -> ruled support)
    // correspondence = (spine control -> (offsetA -> offsetB)) -> (sumA -> sumB)
    // support = (longitudinal interval -> transverse interval) ->
    //           (Interpolate(sideA output, sideB output, u, output) -> output)
    // envelope = sweep -> ((start condition -> end condition) -> occupancy)
    // cap = endpoint cross-section -> actual cross-section line
    // continuation = endpoint cross-section -> (spine endpoint -> outside slot)
    // occupancy = ruled support -> Closed(sideA -> sideB)

    public sealed record SweepRead(Curve.Frame Frame, Curve.Read Spine,
        Curve.Read SideA, Curve.Read SideB, bool Complete, bool? Collapsed,
        Relation Support, Relation Surface);
    public sealed record EndRead(bool? Capped, bool? Continuing, Relation Surface);
    public sealed record EnvelopeRead(SweepRead Sweep, EndRead Start, EndRead End,
        bool? Occupied, bool? VisibleContourClosed, Relation Surface);
    public sealed record CrossingRead(Relation First, Relation Second, Relation Locus,
        bool? Crosses, Relation Surface);
    public sealed record OrderRead(Relation? Front, Relation? Back, bool Complete,
        Relation Surface);

    /// <summary>Construct corresponding boundary controls by native additive
    /// displacement, retaining each cancellation witness. Boundary endpoints
    /// remain independently identified unless explicit sites are supplied.</summary>
    public static Relation Sweep(Curve.Frame frame, Relation spine,
        IReadOnlyList<Relation> sideAOffsets, IReadOnlyList<Relation> sideBOffsets,
        IElement? sideAStart = null, IElement? sideAEnd = null,
        IElement? sideBStart = null, IElement? sideBEnd = null)
    {
        var source = frame.Read(spine);
        if (!source.Complete || !Entries(source.ControlWords, out var controls) ||
            controls.Length != sideAOffsets.Count || controls.Length != sideBOffsets.Count)
            return new(new Relation(frame.Surface, spine),
                StructuralConstraint.Open(S(sideAOffsets.Cast<IElement>().ToArray()), S(sideBOffsets.Cast<IElement>().ToArray())));
        var first = new Relation[controls.Length]; var second = new Relation[controls.Length];
        var rows = new IElement[controls.Length];
        for (var i = 0; i < controls.Length; i++)
        {
            var a = frame.Motion.Reduce((Relation)controls[i], sideAOffsets[i]);
            var b = frame.Motion.Reduce((Relation)controls[i], sideBOffsets[i]);
            first[i] = frame.Motion.TryReadResult(a, out var ar, out _, out _) ? ar : StructuralConstraint.Open(a);
            second[i] = frame.Motion.TryReadResult(b, out var br, out _, out _) ? br : StructuralConstraint.Open(b);
            rows[i] = new Relation(new Relation(controls[i], new Relation(sideAOffsets[i], sideBOffsets[i])), new Relation(a, b));
        }
        var interval = IntervalOf(spine);
        var sideA = Section(frame, sideAStart ?? new Placeholder(), sideAEnd ?? new Placeholder(), interval, first);
        var sideB = Section(frame, sideBStart ?? new Placeholder(), sideBEnd ?? new Placeholder(), interval, second);
        var transverse = BoundedInterval(); var parameter = Parameter(transverse);
        var output = new Placeholder();
        var operation = TransformSurface.Interpolate(Output(sideA), Output(sideB), parameter, output);
        var support = new Relation(new Relation(interval, transverse), new Relation(operation, output));
        return new(new Relation(new Relation(frame.Surface, spine), new Relation(sideA, sideB)), new Relation(S(rows), support));
    }

    /// <summary>Reads only this exact construction grammar. Incomplete or
    /// foreign displacements retain an open read; they are never zero width.</summary>
    public static SweepRead Read(Curve.Frame frame, Relation surface)
    {
        SweepRead Open(Relation? spine = null, Relation? sideA = null, Relation? sideB = null) =>
            new(frame, frame.Read(spine ?? StructuralConstraint.Open()), frame.Read(sideA ?? StructuralConstraint.Open()),
                frame.Read(sideB ?? StructuralConstraint.Open()), false, null, StructuralConstraint.Open(surface), surface);
        if (surface is not { Left: Relation { Left: Relation { Left: var selectedFrame, Right: Relation spine },
            Right: Relation { Left: Relation a, Right: Relation b } },
            Right: Relation { Left: Relation correspondences, Right: Relation support } } || !Same(selectedFrame, frame.Surface)) return Open();
        var source = frame.Read(spine); var first = frame.Read(a); var second = frame.Read(b);
        if (!source.Complete || !first.Complete || !second.Complete ||
            !Entries(source.ControlWords, out var controls) || !Entries(first.ControlWords, out var ac) ||
            !Entries(second.ControlWords, out var bc) || !Entries(correspondences, out var rows) ||
            rows.Length != controls.Length || ac.Length != controls.Length || bc.Length != controls.Length ||
            !Same(IntervalOf(spine), IntervalOf(a)) || !Same(IntervalOf(spine), IntervalOf(b))) return Open(spine, a, b);
        for (var i = 0; i < rows.Length; i++)
        {
            if (rows[i] is not Relation { Left: Relation { Left: var control, Right: Relation { Left: Relation oa, Right: Relation ob } },
                Right: Relation { Left: Relation sumA, Right: Relation sumB } } || !Same(control, controls[i]) ||
                !Sum(frame, sumA, (Relation)controls[i], oa, ac[i]) || !Sum(frame, sumB, (Relation)controls[i], ob, bc[i])) return Open(spine, a, b);
        }
        if (support is not { Left: Relation { Left: var longitudinal, Right: Relation transverse },
            Right: Relation { Left: Relation operation, Right: Placeholder result } } ||
            !Same(longitudinal, IntervalOf(spine)) || !TryInterval(transverse, out var parameter) ||
            Same(parameter, Parameter(IntervalOf(spine))) ||
            !TransformSurface.TryReadInterpolate(operation, out var ao, out var bo, out var u, out var output) ||
            !Same(ao, Output(a)) || !Same(bo, Output(b)) || !Same(u, parameter) || !Same(output, result) ||
            Same(result, parameter) || Same(result, ao) || Same(result, bo)) return Open(spine, a, b);
        return new(frame, source, first, second, true,
            first.Controls.Zip(second.Controls).All(pair => pair.First == pair.Second), support, surface);
    }

    /// <summary>Selects the bounded ruled participation for occupancy. This is
    /// an authored support, not a proof of nonintersection or positive area.</summary>
    public static Relation Occupy(SweepRead read) => read.Complete
        ? new(read.Support, S(new Relation(read.SideA.Section, read.SideB.Section)))
        : new(read.Support, StructuralConstraint.Open(read.Surface));

    public static Relation BandEnvelope(Relation sweep, Relation start, Relation end, Relation? occupancy = null) =>
        new(sweep, new Relation(new Relation(start, end), occupancy ?? StructuralConstraint.Open()));

    /// <summary>A visible end cap is an actual cross-section line connected to
    /// both boundary endpoint sites. It does not follow from selecting fill.</summary>
    public static Relation Cap(Curve.Frame frame, SweepRead read, bool start)
    {
        var crossSection = EndCrossSection(read, start);
        if (!read.Complete || !Same(frame.Surface, read.Frame.Surface)) return new(crossSection, StructuralConstraint.Open());
        var aSite = start ? read.SideA.Start : read.SideA.End; var bSite = start ? read.SideB.Start : read.SideB.End;
        var aw = EndpointWord(read.SideA, start); var bw = EndpointWord(read.SideB, start);
        return new(crossSection, start ? frame.Line(bSite, aSite, bw, aw) : frame.Line(aSite, bSite, aw, bw));
    }

    /// <summary>Retains continuation beyond the selected view without adding
    /// a cap or inventing its unexposed geometry.</summary>
    public static Relation Continue(Curve.Frame frame, SweepRead read, bool start, IElement? outside = null) =>
        new(EndCrossSection(read, start), read.Complete && Same(frame.Surface, read.Frame.Surface)
            ? new Relation(start ? read.Spine.Start : read.Spine.End, outside ?? new Placeholder())
            : StructuralConstraint.Open(read.Surface));

    public static Relation UnknownEnd(SweepRead read, bool start) => new(EndCrossSection(read, start), StructuralConstraint.Open());

    public static EnvelopeRead ReadEnvelope(Curve.Frame frame, Relation surface)
    {
        var empty = Read(frame, StructuralConstraint.Open());
        if (surface is not { Left: Relation sweep, Right: Relation { Left: Relation { Left: Relation start, Right: Relation end },
            Right: Relation occupied } }) return new(empty, new(null, null, surface), new(null, null, surface), null, null, surface);
        var read = Read(frame, sweep); var a = ReadEnd(read, start, true); var b = ReadEnd(read, end, false);
        bool? occupancy = null;
        if (read.Complete && Same(occupied.Left, read.Support) && occupied.Right is Relation selection && Entries(selection, out var entries) &&
            entries is [Relation pair] && Same(pair.Left, read.SideA.Section) && Same(pair.Right, read.SideB.Section)) occupancy = true;
        bool? closed = a.Capped == true && b.Capped == true ? true : a.Continuing == true || b.Continuing == true ? false : null;
        return new(read, a, b, occupancy, closed, surface);
    }

    private static EndRead ReadEnd(SweepRead read, Relation end, bool start)
    {
        if (!read.Complete || end.Left is not Relation cross || !SameCrossSection(cross, read, start)) return new(null, null, end);
        if (end.Right is Relation continuation && Same(continuation.Left, start ? read.Spine.Start : read.Spine.End) &&
            !Same(continuation.Right, continuation.Left)) return new(false, true, end);
        if (end.Right is not Relation section) return new(null, null, end);
        var cap = read.Frame.Read(section);
        if (!cap.Complete || cap.Controls.Length != 2) return new(null, null, end);
        var from = start ? read.SideB : read.SideA; var to = start ? read.SideA : read.SideB;
        if (!Same(cap.Start, start ? from.Start : from.End) || !Same(cap.End, start ? to.Start : to.End) ||
            cap.Controls[0] != (start ? from.Controls[0] : from.Controls[^1]) ||
            cap.Controls[1] != (start ? to.Controls[0] : to.Controls[^1])) return new(null, null, end);
        return new(true, false, end);
    }

    /// <summary>A strict interior crossing of two straight sections, anchored
    /// to a public position word. Curved intersection is deliberately open;
    /// coordinate contact neither welds sites nor supplies a depth order.</summary>
    public static CrossingRead Crossing(Curve.Frame frame, Relation first, Relation second, Relation locusWord)
    {
        var surface = new Relation(new Relation(frame.Surface, new Relation(first, second)), locusWord);
        return ReadCrossing(frame, surface);
    }

    public static CrossingRead ReadCrossing(Curve.Frame frame, Relation surface)
    {
        if (surface is not { Left: Relation { Left: var selected, Right: Relation { Left: Relation first, Right: Relation second } },
            Right: Relation locus } || !Same(selected, frame.Surface))
            return new(surface, surface, StructuralConstraint.Open(), null, surface);
        var a = frame.Read(first); var b = frame.Read(second);
        if (!a.Complete || !b.Complete || !frame.Vector(locus, out var at) ||
            a.Controls.Length != 2 || b.Controls.Length != 2 || a.Controls[0] == a.Controls[^1] || b.Controls[0] == b.Controls[^1])
            return new(first, second, locus, null, surface);
        var av = a.Controls[^1] - a.Controls[0]; var bv = b.Controls[^1] - b.Controls[0];
        var crosses = !Curve.Vector.Cross(av, bv).IsZero && Interior(at, a.Controls[0], a.Controls[^1]) && Interior(at, b.Controls[0], b.Controls[^1]);
        return new(first, second, locus, crosses, surface);
    }

    /// <summary>First is visibly in front of second under this crossing read.
    /// The two actual source sections are retained, not a hidden order tag.</summary>
    public static Relation Over(CrossingRead crossing, Relation front, Relation back) => new(crossing.Surface, new Relation(front, back));

    public static OrderRead ReadOrder(Curve.Frame frame, Relation surface)
    {
        if (surface is not { Left: Relation cross, Right: Relation { Left: Relation front, Right: Relation back } }) return new(null, null, false, surface);
        var read = ReadCrossing(frame, cross);
        var complete = read.Crosses == true && !Same(front, back) &&
            (Same(front, read.First) && Same(back, read.Second) || Same(front, read.Second) && Same(back, read.First));
        return new(front, back, complete, surface);
    }

    private static bool Interior(Curve.Vector at, Curve.Vector from, Curve.Vector to)
    {
        var direction = to - from; var delta = at - from;
        var progress = Curve.Vector.Dot(delta, direction);
        return Curve.Vector.Cross(delta, direction).IsZero && progress.Sign > 0 && progress < Curve.Vector.Dot(direction, direction);
    }

    private static bool Sum(Curve.Frame frame, Relation witness, Relation control, Relation offset, IElement result) =>
        witness.Left is Relation { Right: Relation inputs } && Same(inputs.Left, control) && Same(inputs.Right, offset) &&
        frame.Vector(offset, out _) && frame.Motion.TryReadResult(witness, out var read, out _, out _) && Same(read, result);

    private static Relation EndCrossSection(SweepRead read, bool start) => new(
        new Relation(start ? read.SideA.Start : read.SideA.End, EndpointWord(read.SideA, start)),
        new Relation(start ? read.SideB.Start : read.SideB.End, EndpointWord(read.SideB, start)));
    private static bool SameCrossSection(Relation cross, SweepRead read, bool start) =>
        cross is { Left: Relation a, Right: Relation b } &&
        Same(a.Left, start ? read.SideA.Start : read.SideA.End) && Same(b.Left, start ? read.SideB.Start : read.SideB.End) &&
        Same(a.Right, EndpointWord(read.SideA, start)) && Same(b.Right, EndpointWord(read.SideB, start));
    private static Relation EndpointWord(Curve.Read read, bool start) =>
        Entries(read.ControlWords, out var controls) && controls.Length != 0 && controls[start ? 0 : controls.Length - 1] is Relation word
            ? word : StructuralConstraint.Open(read.Section);

    private static Relation Section(Curve.Frame frame, IElement from, IElement to, Relation interval, Relation[] controls)
    {
        var parameter = Parameter(interval); var operations = new List<IElement>(); IElement[] level = controls;
        while (level.Length > 1)
        {
            var next = new IElement[level.Length - 1];
            for (var i = 0; i < next.Length; i++)
            {
                next[i] = new Placeholder();
                operations.Add(TransformSurface.Interpolate(level[i], level[i + 1], parameter, next[i]));
            }
            level = next;
        }
        return new(new Relation(from, to), new Relation(S(controls),
            new Relation(new Relation(frame.Surface, interval), new Relation(S(operations.ToArray()), level[0]))));
    }

    private static Relation BoundedInterval()
    {
        var start = new Placeholder(); var end = new Placeholder(); var parameter = new Placeholder();
        return new(new Relation(start, end), new Relation(parameter, S(new Relation(start, parameter), new Relation(parameter, end))));
    }
    private static bool TryInterval(Relation interval, out IElement parameter)
    {
        parameter = null!;
        if (interval is not { Left: Relation { Left: Placeholder start, Right: Placeholder end },
            Right: Relation { Left: Placeholder at, Right: Relation order } } ||
            start.IsInverse || end.IsInverse || at.IsInverse || Same(start, end) || Same(start, at) || Same(end, at) ||
            !Entries(order, out var pairs) || pairs is not [Relation a, Relation b] ||
            !Same(a.Left, start) || !Same(a.Right, at) || !Same(b.Left, at) || !Same(b.Right, end)) return false;
        parameter = at; return true;
    }
    private static Relation IntervalOf(Relation section) => (Relation)((Relation)((Relation)((Relation)section.Right).Right).Left).Right;
    private static IElement Parameter(Relation interval) => ((Relation)interval.Right).Left;
    private static IElement Output(Relation section) => ((Relation)((Relation)((Relation)section.Right).Right).Right).Right;
    private static bool Same(IElement a, IElement b) => ReferenceEquals(a, b) || a is Placeholder x && b is Placeholder y && x.Equals(y);
    private static bool Entries(Relation scope, out IElement[] entries) => StructuralConstraint.TryReadScope(scope, out entries, out var seam, out var tail) && seam.Equals(tail);
    private static Relation S(params IElement[] entries) => StructuralConstraint.Closed(entries);
}
