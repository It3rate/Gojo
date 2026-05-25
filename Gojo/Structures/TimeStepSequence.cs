using Gojo.Core;

namespace Gojo.Structures;

/// <summary>
/// Shared structural foothold for ordered time steps. A step is a span on a
/// time carrier plus ordinary Gojo evidence for before, transition, after, and
/// residue. The helper only builds and reads relation structure.
/// </summary>
public static class TimeStepSequence
{
    /// <summary>Creates a clock carrier sharing the supplied time basis unit shape.</summary>
    public static Carrier CreateClock(Carrier timeBasis)
    {
        var basis = timeBasis ?? new Carrier();
        return new Carrier(basis.UnitShape);
    }

    /// <summary>Creates a step span on a time carrier.</summary>
    public static Relation Span(Carrier time, long startTicks, long endTicks) =>
        Relation.Segment(RootOrNew(time), startTicks, endTicks);

    /// <summary>
    /// Creates one timed transition record:
    /// time span, before evidence, transition surface, after evidence, residue.
    /// </summary>
    public static Relation Step(
        Carrier root,
        Carrier time,
        long startTicks,
        long endTicks,
        IElement before,
        IElement transition,
        IElement after,
        IElement residue)
    {
        return Relation.OrderedGroup(
            RootOrNew(root),
            Span(time, startTicks, endTicks),
            before ?? Relation.Point(Carrier.Unknown, 0),
            transition ?? Relation.Point(Carrier.Unknown, 0),
            after ?? Relation.Point(Carrier.Unknown, 0),
            residue ?? Relation.Point(Carrier.Unknown, 0));
    }

    /// <summary>Creates an ordered timeline rooted in a clock origin and initial evidence.</summary>
    public static Relation Create(
        Carrier root,
        Carrier time,
        IElement initial,
        params Relation[] steps)
    {
        var clock = RootOrNew(time);
        var cleanSteps = CleanSteps(steps);
        var elements = new IElement[cleanSteps.Length + 2];
        elements[0] = Relation.Point(clock, 0);
        elements[1] = initial ?? Relation.Point(Carrier.Unknown, 0);
        for (var index = 0; index < cleanSteps.Length; index++)
        {
            elements[index + 2] = cleanSteps[index];
        }

        return Relation.OrderedGroup(RootOrNew(root), elements);
    }

    /// <summary>Reads a timeline created by <see cref="Create"/>.</summary>
    public static bool TryRead(
        Relation sequence,
        out CarrierLocation origin,
        out IElement initial,
        out Relation[] steps)
    {
        if (TryReadRootElements(sequence, out var elements) &&
            elements.Length >= 2 &&
            TryReadPoint(elements[0], out var originLocation))
        {
            origin = originLocation;
            initial = elements[1];
            steps = [.. elements.Skip(2).OfType<Relation>()];
            return true;
        }

        origin = default;
        initial = default!;
        steps = [];
        return false;
    }

    /// <summary>Reads one timed transition record.</summary>
    public static bool TryReadStep(
        Relation step,
        out Relation span,
        out CarrierLocation start,
        out CarrierLocation end,
        out IElement before,
        out IElement transition,
        out IElement after,
        out IElement residue)
    {
        if (TryReadRootElements(step, out var elements) &&
            elements.Length >= 5 &&
            elements[0] is Relation spanRelation &&
            TryReadSpan(spanRelation, out var startLocation, out var endLocation))
        {
            span = spanRelation;
            start = startLocation;
            end = endLocation;
            before = elements[1];
            transition = elements[2];
            after = elements[3];
            residue = elements[4];
            return true;
        }

        span = null!;
        start = default;
        end = default;
        before = default!;
        transition = default!;
        after = default!;
        residue = default!;
        return false;
    }

    /// <summary>Reads a time span when both endpoints are grounded on the same carrier.</summary>
    public static bool TryReadSpan(Relation span, out CarrierLocation start, out CarrierLocation end)
    {
        if (span?.Left is Relation leftPoint &&
            span.Right is Relation rightPoint &&
            TryReadPoint(leftPoint, out var startLocation) &&
            TryReadPoint(rightPoint, out var endLocation) &&
            startLocation.Carrier.SameIdentityAs(endLocation.Carrier))
        {
            start = startLocation;
            end = endLocation;
            return true;
        }

        start = default;
        end = default;
        return false;
    }

    private static Relation[] CleanSteps(IReadOnlyList<Relation> steps)
    {
        var count = CountOf(steps);
        if (count == 0)
        {
            return [];
        }

        var clean = new Relation[count];
        for (var index = 0; index < count; index++)
        {
            clean[index] = steps[index] ?? Relation.OrderedGroup(new Carrier(), Relation.Point(Carrier.Unknown, 0));
        }

        return clean;
    }

    private static bool TryReadRootElements(Relation structure, out IElement[] elements)
    {
        if (structure?.Right is Carrier holder &&
            structure.TryReadOrderedElementsOn(holder, out elements))
        {
            return true;
        }

        elements = [];
        return false;
    }

    private static bool TryReadPoint(IElement element, out CarrierLocation location)
    {
        if (element is Relation point &&
            point.Left is CarrierLocation pointLocation &&
            point.Right is Carrier pointCarrier &&
            pointLocation.Carrier.SameIdentityAs(pointCarrier))
        {
            location = pointLocation;
            return true;
        }

        location = default;
        return false;
    }

    private static int CountOf<T>(IReadOnlyList<T> values) =>
        values?.Count ?? 0;

    private static Carrier RootOrNew(Carrier carrier) =>
        carrier ?? new Carrier();
}
