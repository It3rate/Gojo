using Gojo.Core;

namespace Gojo.Structures;

/// <summary>
/// Structural foothold for first-class repetition. A repeat surface connects a
/// selected structure to a controller axis, transition surface, feedback frame,
/// residue frame, and optional witnessed timeline. The helper only builds and
/// reads ordinary Gojo relation structure.
/// </summary>
public static class RepeatSurface
{
    /// <summary>Creates a repeat axis carrier sharing the supplied basis unit shape.</summary>
    public static Carrier CreateAxis(Carrier basis)
    {
        var source = basis ?? new Carrier();
        return new Carrier(source.UnitShape);
    }

    /// <summary>Creates a simple controller span on a repeat axis.</summary>
    public static Relation Span(Carrier axis, long startTicks, long endTicks) =>
        Relation.Segment(RootOrNew(axis), startTicks, endTicks);

    /// <summary>Creates a count-style controller from origin to count on a repeat axis.</summary>
    public static Relation Count(Carrier axis, long countTicks) =>
        Span(axis, 0, countTicks);

    /// <summary>
    /// Creates a repeat surface:
    /// selected, controller, transition, feedback, residue, timeline.
    /// </summary>
    public static Relation Create(
        Carrier root,
        IElement selected,
        IElement controller,
        IElement transition,
        IElement feedback,
        IElement residue,
        IElement timeline)
    {
        return Relation.OrderedGroup(
            RootOrNew(root),
            selected ?? UnknownPoint(),
            controller ?? UnknownPoint(),
            transition ?? UnknownPoint(),
            feedback ?? UnknownPoint(),
            residue ?? UnknownPoint(),
            timeline ?? UnknownPoint());
    }

    /// <summary>Creates a repeat surface without a witnessed timeline yet.</summary>
    public static Relation Create(
        Carrier root,
        IElement selected,
        IElement controller,
        IElement transition,
        IElement feedback,
        IElement residue) =>
        Create(root, selected, controller, transition, feedback, residue, UnknownPoint());

    /// <summary>Reads a repeat surface created by <see cref="Create(Carrier,IElement,IElement,IElement,IElement,IElement,IElement)"/>.</summary>
    public static bool TryRead(
        Relation repeat,
        out IElement selected,
        out IElement controller,
        out IElement transition,
        out IElement feedback,
        out IElement residue,
        out IElement timeline)
    {
        if (TryReadRootElements(repeat, out var elements) &&
            elements.Length >= 6)
        {
            selected = elements[0];
            controller = elements[1];
            transition = elements[2];
            feedback = elements[3];
            residue = elements[4];
            timeline = elements[5];
            return true;
        }

        selected = default!;
        controller = default!;
        transition = default!;
        feedback = default!;
        residue = default!;
        timeline = default!;
        return false;
    }

    /// <summary>Reads a literal repeat span when the controller is a simple axis segment.</summary>
    public static bool TryReadSpan(IElement controller, out CarrierLocation start, out CarrierLocation end)
    {
        if (controller is Relation relation &&
            TimeStepSequence.TryReadSpan(relation, out var startLocation, out var endLocation))
        {
            start = startLocation;
            end = endLocation;
            return true;
        }

        start = default;
        end = default;
        return false;
    }

    /// <summary>Reads a witnessed timeline from a repeat surface when one is attached.</summary>
    public static bool TryReadTimeline(Relation repeat, out Relation timeline)
    {
        if (TryRead(
                repeat,
                out _,
                out _,
                out _,
                out _,
                out _,
                out var timelineElement) &&
            timelineElement is Relation candidateTimeline &&
            TimeStepSequence.TryRead(candidateTimeline, out _, out _, out _))
        {
            timeline = candidateTimeline;
            return true;
        }

        timeline = null!;
        return false;
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

    private static Relation UnknownPoint() =>
        Relation.Point(Carrier.Unknown, 0);

    private static Carrier RootOrNew(Carrier carrier) =>
        carrier ?? new Carrier();
}
