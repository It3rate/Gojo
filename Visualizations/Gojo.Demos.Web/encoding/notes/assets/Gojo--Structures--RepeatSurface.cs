using Gojo.Core;
using Gojo.Patterns;

namespace Gojo.Structures;

/// <summary>
/// Structural foothold for first-class repetition. A repeat surface connects a
/// selected structure to a controller axis, transition surface, feedback frame,
/// residue frame, and optional witnessed timeline. The helper only builds and
/// reads ordinary Gojo relation structure.
/// </summary>
public static class RepeatSurface
{
    /// <summary>Creates an ordered port set. Ports are ordinary slot-shaped elements.</summary>
    public static Relation Ports(Carrier root, params IElement[] ports) =>
        Relation.OrderedGroup(RootOrNew(root), ports ?? []);

    /// <summary>Creates an ordered frame of slot bindings.</summary>
    public static Relation Frame(Carrier root, params Relation[] bindings) =>
        Relation.OrderedGroup(RootOrNew(root), bindings ?? []);

    /// <summary>
    /// Creates a repeatable template from input ports, selected structure, and
    /// output ports. The ports and selected structure remain ordinary Gojo
    /// relation structure.
    /// </summary>
    public static Relation Template(
        Carrier root,
        IElement inputPorts,
        IElement selected,
        IElement outputPorts) =>
        Relation.OrderedGroup(
            RootOrNew(root),
            inputPorts ?? Ports(new Carrier()),
            selected ?? UnknownPoint(),
            outputPorts ?? Ports(new Carrier()));

    /// <summary>
    /// Creates feedback evidence that the output port feeds the next input
    /// port. The relation is interpreted in feedback context as
    /// output -> next input.
    /// </summary>
    public static Relation Feed(IElement outputPort, IElement nextInputPort) =>
        new(outputPort ?? UnknownPoint(), nextInputPort ?? UnknownPoint());

    /// <summary>Reads a repeatable template created by <see cref="Template"/>.</summary>
    public static bool TryReadTemplate(
        Relation template,
        out IElement inputPorts,
        out IElement selected,
        out IElement outputPorts)
    {
        if (TryReadRootElements(template, out var elements) &&
            elements.Length >= 3)
        {
            inputPorts = elements[0];
            selected = elements[1];
            outputPorts = elements[2];
            return true;
        }

        inputPorts = default!;
        selected = default!;
        outputPorts = default!;
        return false;
    }

    /// <summary>Reads an ordered port set.</summary>
    public static bool TryReadPorts(IElement ports, out IElement[] portElements)
    {
        if (ports is Relation portsRelation &&
            TryReadRootElements(portsRelation, out var elements))
        {
            portElements = elements;
            return true;
        }

        portElements = [];
        return false;
    }

    /// <summary>Reads an ordered frame of relation-shaped bindings.</summary>
    public static bool TryReadFrame(IElement frame, out Relation[] bindings)
    {
        if (frame is Relation frameRelation &&
            TryReadRootElements(frameRelation, out var elements))
        {
            bindings = [.. elements.OfType<Relation>()];
            return true;
        }

        bindings = [];
        return false;
    }

    /// <summary>Reads a feedback relation as output port -> next input port.</summary>
    public static bool TryReadFeed(Relation feed, out IElement outputPort, out IElement nextInputPort)
    {
        if (feed is not null)
        {
            outputPort = feed.Left;
            nextInputPort = feed.Right;
            return true;
        }

        outputPort = default!;
        nextInputPort = default!;
        return false;
    }

    /// <summary>Applies transition bindings to a frame, producing the next visible frame.</summary>
    public static Relation[] ApplyTransitionFrame(
        IReadOnlyList<Relation> frame,
        IReadOnlyList<Relation> transitionBindings) =>
        StructuralPattern.Advance(frame ?? [], transitionBindings ?? []);

    /// <summary>Applies transition bindings to a frame, producing the next visible frame.</summary>
    public static Relation[] ApplyTransitionFrame(
        IReadOnlyList<Relation> frame,
        params Relation[] transitionBindings) =>
        ApplyTransitionFrame(frame, (IReadOnlyList<Relation>)transitionBindings);

    /// <summary>
    /// Builds the next input frame from output-to-input feedback evidence and
    /// optional pass-through or external data bindings.
    /// </summary>
    public static Relation[] ApplyFeedbackFrame(
        IReadOnlyList<Relation> frame,
        IReadOnlyList<Relation> feedback,
        IReadOnlyList<Relation> dataBindings)
    {
        var transitions = new List<Relation>();
        foreach (var feed in feedback ?? [])
        {
            if (TryReadFeed(feed, out var outputPort, out var nextInputPort))
            {
                transitions.Add(new Relation(nextInputPort, outputPort));
            }
        }

        if (dataBindings is not null)
        {
            transitions.AddRange(dataBindings);
        }

        return ApplyTransitionFrame(frame ?? [], transitions);
    }

    /// <summary>
    /// Builds the next input frame from output-to-input feedback evidence and
    /// optional pass-through or external data bindings.
    /// </summary>
    public static Relation[] ApplyFeedbackFrame(
        IReadOnlyList<Relation> frame,
        IReadOnlyList<Relation> feedback,
        params Relation[] dataBindings) =>
        ApplyFeedbackFrame(frame, feedback, (IReadOnlyList<Relation>)dataBindings);

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
