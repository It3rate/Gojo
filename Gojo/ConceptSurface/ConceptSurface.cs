using Gojo.Core;
using Gojo.Structures;

namespace Gojo.ConceptSurface;

/// <summary>
/// Builds and reads a concept-surface inspection record. The record is an
/// ordinary ordered Gojo group; this helper does not own the concept's meaning.
/// </summary>
public static class ConceptSurface
{
    /// <summary>
    /// Creates a concept surface:
    /// selected, topology, exposed read/focus, flow, parameters, constraints,
    /// settlement witness, and refinement timeline.
    /// </summary>
    public static Relation Create(
        Carrier root,
        IElement selected,
        IElement topology,
        IElement focus,
        IElement flow,
        IElement parameters,
        IElement constraints,
        IElement settlementWitness,
        IElement refinementTimeline)
    {
        return Relation.OrderedGroup(
            RootOrNew(root),
            selected ?? UnknownPoint(),
            topology ?? UnknownPoint(),
            focus ?? UnknownPoint(),
            flow ?? UnknownPoint(),
            parameters ?? UnknownPoint(),
            constraints ?? UnknownPoint(),
            settlementWitness ?? UnknownPoint(),
            refinementTimeline ?? UnknownPoint());
    }

    /// <summary>Creates a concept surface without a witnessed timeline yet.</summary>
    public static Relation Create(
        Carrier root,
        IElement selected,
        IElement topology,
        IElement focus,
        IElement flow,
        IElement parameters,
        IElement constraints,
        IElement settlementWitness) =>
        Create(
            root,
            selected,
            topology,
            focus,
            flow,
            parameters,
            constraints,
            settlementWitness,
            UnknownPoint());

    /// <summary>Creates a minimal surface around selected structure and topology.</summary>
    public static Relation Create(
        Carrier root,
        IElement selected,
        IElement topology) =>
        Create(
            root,
            selected,
            topology,
            UnknownPoint(),
            UnknownPoint(),
            UnknownPoint(),
            UnknownPoint(),
            UnknownPoint(),
            UnknownPoint());

    /// <summary>Reads a concept surface created by <see cref="Create(Carrier,IElement,IElement,IElement,IElement,IElement,IElement,IElement,IElement)"/>.</summary>
    public static bool TryRead(
        Relation surface,
        out IElement selected,
        out IElement topology,
        out IElement focus,
        out IElement flow,
        out IElement parameters,
        out IElement constraints,
        out IElement settlementWitness,
        out IElement refinementTimeline)
    {
        if (ConceptSurfaceReader.TryReadRootElements(surface, out var elements) &&
            elements.Length >= 8)
        {
            selected = elements[0];
            topology = elements[1];
            focus = elements[2];
            flow = elements[3];
            parameters = elements[4];
            constraints = elements[5];
            settlementWitness = elements[6];
            refinementTimeline = elements[7];
            return true;
        }

        selected = default!;
        topology = default!;
        focus = default!;
        flow = default!;
        parameters = default!;
        constraints = default!;
        settlementWitness = default!;
        refinementTimeline = default!;
        return false;
    }

    /// <summary>Reads the refinement timeline when the surface has a witnessed time-step sequence.</summary>
    public static bool TryReadTimeline(Relation surface, out Relation timeline)
    {
        if (TryRead(
                surface,
                out _,
                out _,
                out _,
                out _,
                out _,
                out _,
                out _,
                out var timelineElement) &&
            timelineElement is Relation candidate &&
            TimeStepSequence.TryRead(candidate, out _, out _, out _))
        {
            timeline = candidate;
            return true;
        }

        timeline = null!;
        return false;
    }

    internal static Relation UnknownPoint() => Relation.Point(Carrier.Unknown, 0);

    internal static Carrier RootOrNew(Carrier carrier) => carrier ?? new Carrier();
}

/// <summary>
/// Unoriented endpoint-pair surfaces. They are represented as ordered Gojo
/// groups today, but the read treats the endpoints as an extent whose
/// traversal has not been chosen yet.
/// </summary>
public static class ConceptExtent
{
    public static Relation Create(Carrier root, Relation endpointA, Relation endpointB) =>
        Relation.OrderedGroup(
            ConceptSurface.RootOrNew(root),
            endpointA ?? ConceptSurface.UnknownPoint(),
            endpointB ?? ConceptSurface.UnknownPoint());

    public static Relation FromLocations(Carrier root, CarrierLocation first, CarrierLocation second) =>
        Create(root, Point(first), Point(second));

    public static bool TryRead(
        Relation extent,
        out Relation endpointA,
        out Relation endpointB)
    {
        if (ConceptSurfaceReader.TryReadRootElements(extent, out var elements) &&
            elements.Length >= 2 &&
            elements[0] is Relation first &&
            elements[1] is Relation second &&
            ConceptSurfaceReader.TryReadPoint(first, out _) &&
            ConceptSurfaceReader.TryReadPoint(second, out _))
        {
            endpointA = first;
            endpointB = second;
            return true;
        }

        endpointA = null!;
        endpointB = null!;
        return false;
    }

    public static bool TryReadLocations(
        Relation extent,
        out CarrierLocation endpointA,
        out CarrierLocation endpointB)
    {
        if (TryRead(extent, out var first, out var second) &&
            ConceptSurfaceReader.TryReadPoint(first, out var firstLocation) &&
            ConceptSurfaceReader.TryReadPoint(second, out var secondLocation))
        {
            endpointA = firstLocation;
            endpointB = secondLocation;
            return true;
        }

        endpointA = default;
        endpointB = default;
        return false;
    }

    /// <summary>Reads whether two extents expose the same endpoint points, ignoring stored order.</summary>
    public static bool SameUnorientedEndpoints(Relation left, Relation right)
    {
        if (!TryRead(left, out var leftA, out var leftB) ||
            !TryRead(right, out var rightA, out var rightB))
        {
            return false;
        }

        return Equals(leftA, rightA) && Equals(leftB, rightB) ||
            Equals(leftA, rightB) && Equals(leftB, rightA);
    }

    /// <summary>Chooses a traversal order for an extent without mutating the source extent.</summary>
    public static bool TryOrient(Relation extent, out Relation directed, bool reverse = false)
    {
        if (!TryRead(extent, out var first, out var second))
        {
            directed = null!;
            return false;
        }

        directed = reverse ? new Relation(second, first) : new Relation(first, second);
        return true;
    }

    private static Relation Point(CarrierLocation location) =>
        new(location, location.Carrier);
}

/// <summary>
/// Traversal surfaces built from ordinary forward and reverse relations.
/// Reversible traversal is stronger than an agnostic connection because both
/// directions are explicitly present.
/// </summary>
public static class ConceptTraversal
{
    public static Relation Reversible(Carrier root, Relation forward, Relation reverse) =>
        Relation.OrderedGroup(
            ConceptSurface.RootOrNew(root),
            forward ?? ConceptSurface.UnknownPoint(),
            reverse ?? forward?.Reverse() ?? ConceptSurface.UnknownPoint());

    public static Relation ReversibleSegment(Carrier root, Carrier carrier, long firstTicks, long secondTicks)
    {
        var forward = Relation.Segment(carrier ?? Carrier.Unknown, firstTicks, secondTicks);
        return Reversible(root, forward, forward.Reverse());
    }

    public static bool TryReadReversible(
        Relation traversal,
        out Relation forward,
        out Relation reverse)
    {
        if (ConceptSurfaceReader.TryReadRootElements(traversal, out var elements) &&
            elements.Length >= 2 &&
            elements[0] is Relation forwardRelation &&
            elements[1] is Relation reverseRelation)
        {
            forward = forwardRelation;
            reverse = reverseRelation;
            return true;
        }

        forward = null!;
        reverse = null!;
        return false;
    }
}

/// <summary>
/// Open attachment surfaces distinguish an omitted structure from a visible
/// site that expects a future receiver or source.
/// </summary>
public static class ConceptAttachment
{
    public static Relation OpenOutlet(
        Carrier root,
        IElement site,
        IElement expectedFlow,
        IElement receiver,
        IElement unresolvedWitness) =>
        Relation.OrderedGroup(
            ConceptSurface.RootOrNew(root),
            site ?? ConceptSurface.UnknownPoint(),
            expectedFlow ?? ConceptSurface.UnknownPoint(),
            receiver ?? new Placeholder(),
            unresolvedWitness ?? ConceptSurface.UnknownPoint());

    public static Relation OpenOutlet(Carrier root, IElement site, IElement expectedFlow) =>
        OpenOutlet(root, site, expectedFlow, new Placeholder(), ConceptSurface.UnknownPoint());

    public static Relation OpenInlet(
        Carrier root,
        IElement site,
        IElement expectedFlow,
        IElement source,
        IElement unresolvedWitness) =>
        OpenOutlet(root, site, expectedFlow, source, unresolvedWitness);

    public static Relation OpenInlet(Carrier root, IElement site, IElement expectedFlow) =>
        OpenInlet(root, site, expectedFlow, new Placeholder(), ConceptSurface.UnknownPoint());

    public static bool TryRead(
        Relation attachment,
        out IElement site,
        out IElement expectedFlow,
        out IElement openElement,
        out IElement unresolvedWitness)
    {
        if (ConceptSurfaceReader.TryReadRootElements(attachment, out var elements) &&
            elements.Length >= 4)
        {
            site = elements[0];
            expectedFlow = elements[1];
            openElement = elements[2];
            unresolvedWitness = elements[3];
            return true;
        }

        site = default!;
        expectedFlow = default!;
        openElement = default!;
        unresolvedWitness = default!;
        return false;
    }
}

/// <summary>Explicit bounded absence or anti-participation surface.</summary>
public static class ConceptVoid
{
    public static Relation Create(
        Carrier root,
        IElement boundary,
        IElement antiParticipation,
        IElement passThroughAxis) =>
        Relation.OrderedGroup(
            ConceptSurface.RootOrNew(root),
            boundary ?? ConceptSurface.UnknownPoint(),
            antiParticipation ?? ConceptSurface.UnknownPoint(),
            passThroughAxis ?? ConceptSurface.UnknownPoint());

    public static Relation Create(Carrier root, IElement boundary, IElement antiParticipation) =>
        Create(root, boundary, antiParticipation, ConceptSurface.UnknownPoint());

    public static bool TryRead(
        Relation voidSurface,
        out IElement boundary,
        out IElement antiParticipation,
        out IElement passThroughAxis)
    {
        if (ConceptSurfaceReader.TryReadRootElements(voidSurface, out var elements) &&
            elements.Length >= 3)
        {
            boundary = elements[0];
            antiParticipation = elements[1];
            passThroughAxis = elements[2];
            return true;
        }

        boundary = default!;
        antiParticipation = default!;
        passThroughAxis = default!;
        return false;
    }
}

/// <summary>
/// Represents partition of a whole surface, distinct from a raw count of unit
/// ticks. A partition says a whole has been divided into addressable parts.
/// </summary>
public static class ConceptPartition
{
    public static Relation Create(
        Carrier root,
        IElement whole,
        Carrier axis,
        long partitionCount,
        IElement selectedPartition)
    {
        var partitionAxis = axis ?? new Carrier();
        return Relation.OrderedGroup(
            ConceptSurface.RootOrNew(root),
            whole ?? ConceptSurface.UnknownPoint(),
            RepeatSurface.Count(partitionAxis, Math.Max(0, partitionCount)),
            selectedPartition ?? ConceptSurface.UnknownPoint());
    }

    public static Relation Create(Carrier root, IElement whole, Carrier axis, long partitionCount) =>
        Create(root, whole, axis, partitionCount, ConceptSurface.UnknownPoint());

    public static bool TryRead(
        Relation partition,
        out IElement whole,
        out Relation controller,
        out CarrierLocation origin,
        out CarrierLocation count,
        out IElement selectedPartition)
    {
        if (ConceptSurfaceReader.TryReadRootElements(partition, out var elements) &&
            elements.Length >= 3 &&
            elements[1] is Relation controllerRelation &&
            RepeatSurface.TryReadSpan(controllerRelation, out var controllerOrigin, out var controllerCount))
        {
            whole = elements[0];
            controller = controllerRelation;
            origin = controllerOrigin;
            count = controllerCount;
            selectedPartition = elements[2];
            return true;
        }

        whole = default!;
        controller = null!;
        origin = default;
        count = default;
        selectedPartition = default!;
        return false;
    }
}

/// <summary>
/// Stores the fact that a selected structure is being inspected through a
/// particular read. This is a focus surface, not external focus metadata.
/// </summary>
public static class ConceptReadFocus
{
    public static Relation Create(
        Carrier root,
        IElement selected,
        Relation readSurface,
        IElement resultOrResidue) =>
        Relation.OrderedGroup(
            ConceptSurface.RootOrNew(root),
            selected ?? ConceptSurface.UnknownPoint(),
            readSurface ?? ConceptSurface.UnknownPoint(),
            resultOrResidue ?? ConceptSurface.UnknownPoint());

    public static Relation Create(Carrier root, IElement selected, Relation readSurface) =>
        Create(root, selected, readSurface, ConceptSurface.UnknownPoint());

    public static bool TryRead(
        Relation focus,
        out IElement selected,
        out Relation readSurface,
        out IElement resultOrResidue)
    {
        if (ConceptSurfaceReader.TryReadRootElements(focus, out var elements) &&
            elements.Length >= 3 &&
            elements[1] is Relation read)
        {
            selected = elements[0];
            readSurface = read;
            resultOrResidue = elements[2];
            return true;
        }

        selected = default!;
        readSurface = null!;
        resultOrResidue = default!;
        return false;
    }
}

/// <summary>
/// Passage capacity surface. The capacity itself is ordinary structure: a
/// whole capacity surface, optional partition, and overflow/residue witness.
/// </summary>
public static class ConceptCapacity
{
    public static Relation Create(
        Carrier root,
        IElement passage,
        IElement wholeCapacity,
        IElement partition,
        IElement overflowWitness) =>
        Relation.OrderedGroup(
            ConceptSurface.RootOrNew(root),
            passage ?? ConceptSurface.UnknownPoint(),
            wholeCapacity ?? ConceptSurface.UnknownPoint(),
            partition ?? ConceptSurface.UnknownPoint(),
            overflowWitness ?? ConceptSurface.UnknownPoint());

    public static Relation Create(Carrier root, IElement passage, IElement wholeCapacity) =>
        Create(root, passage, wholeCapacity, ConceptSurface.UnknownPoint(), ConceptSurface.UnknownPoint());

    public static bool TryRead(
        Relation capacity,
        out IElement passage,
        out IElement wholeCapacity,
        out IElement partition,
        out IElement overflowWitness)
    {
        if (ConceptSurfaceReader.TryReadRootElements(capacity, out var elements) &&
            elements.Length >= 4)
        {
            passage = elements[0];
            wholeCapacity = elements[1];
            partition = elements[2];
            overflowWitness = elements[3];
            return true;
        }

        passage = default!;
        wholeCapacity = default!;
        partition = default!;
        overflowWitness = default!;
        return false;
    }
}

/// <summary>Potential settlement site before a perturbation has used it.</summary>
public static class ConceptStablePotential
{
    public static Relation Create(
        Carrier root,
        IElement site,
        IElement approachSurface,
        IElement holdingSurface,
        IElement thresholdOrResidue) =>
        Relation.OrderedGroup(
            ConceptSurface.RootOrNew(root),
            site ?? ConceptSurface.UnknownPoint(),
            approachSurface ?? ConceptSurface.UnknownPoint(),
            holdingSurface ?? ConceptSurface.UnknownPoint(),
            thresholdOrResidue ?? ConceptSurface.UnknownPoint());

    public static bool TryRead(
        Relation potential,
        out IElement site,
        out IElement approachSurface,
        out IElement holdingSurface,
        out IElement thresholdOrResidue)
    {
        if (ConceptSurfaceReader.TryReadRootElements(potential, out var elements) &&
            elements.Length >= 4)
        {
            site = elements[0];
            approachSurface = elements[1];
            holdingSurface = elements[2];
            thresholdOrResidue = elements[3];
            return true;
        }

        site = default!;
        approachSurface = default!;
        holdingSurface = default!;
        thresholdOrResidue = default!;
        return false;
    }
}

/// <summary>
/// Structural gate surface. The controlling parameter is relation structure;
/// the helper does not decide boolean pass/fail in code.
/// </summary>
public static class ConceptGate
{
    public static Relation Create(
        Carrier root,
        IElement candidateRoute,
        IElement controllingParameter,
        IElement passWitness,
        IElement blockedWitness) =>
        Relation.OrderedGroup(
            ConceptSurface.RootOrNew(root),
            candidateRoute ?? ConceptSurface.UnknownPoint(),
            controllingParameter ?? ConceptSurface.UnknownPoint(),
            passWitness ?? ConceptSurface.UnknownPoint(),
            blockedWitness ?? ConceptSurface.UnknownPoint());

    public static bool TryRead(
        Relation gate,
        out IElement candidateRoute,
        out IElement controllingParameter,
        out IElement passWitness,
        out IElement blockedWitness)
    {
        if (ConceptSurfaceReader.TryReadRootElements(gate, out var elements) &&
            elements.Length >= 4)
        {
            candidateRoute = elements[0];
            controllingParameter = elements[1];
            passWitness = elements[2];
            blockedWitness = elements[3];
            return true;
        }

        candidateRoute = default!;
        controllingParameter = default!;
        passWitness = default!;
        blockedWitness = default!;
        return false;
    }
}

/// <summary>Local read helpers shared by concept-surface builders.</summary>
internal static class ConceptSurfaceReader
{
    public static bool TryReadRootElements(Relation structure, out IElement[] elements)
    {
        if (structure?.Right is Carrier holder &&
            structure.TryReadOrderedElementsOn(holder, out elements))
        {
            return true;
        }

        elements = [];
        return false;
    }

    public static bool TryReadPoint(IElement element, out CarrierLocation location)
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
}
