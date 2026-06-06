using Gojo.Core;

namespace GojoCAD.Structures;

/// <summary>
/// Relation-shaped offset evidence for drawable sections and boundaries. The
/// surface records source, distance, side, direction, closure, and residue as
/// ordinary Gojo structure; Cartesian offset samples are a render-boundary read.
/// </summary>
public static class GraphicOffsetSurface
{
    /// <summary>Creates an ordered frame for offset evidence.</summary>
    public static Relation Frame(Carrier root, params IElement[] elements) =>
        Relation.OrderedGroup(RootOrNew(root), elements ?? []);

    /// <summary>
    /// Creates an offset evidence surface:
    /// normal basis, distance/radius evidence, side pair, direction, closure,
    /// residue.
    /// </summary>
    public static Relation Evidence(Carrier root) =>
        Evidence(root, TensionSlot(), SidePair(new Carrier()), Direction(new Carrier()), Closure(new Carrier()));

    /// <summary>Creates offset evidence with an explicit distance/radius surface.</summary>
    public static Relation Evidence(Carrier root, IElement distance) =>
        Evidence(root, distance, SidePair(new Carrier()), Direction(new Carrier()), Closure(new Carrier()));

    /// <summary>Creates offset evidence from explicit offset sub-surfaces.</summary>
    public static Relation Evidence(
        Carrier root,
        IElement distance,
        IElement sidePair,
        IElement direction,
        IElement closure) =>
        Frame(
            root,
            OrthogonalSlot(),
            distance ?? TensionSlot(),
            sidePair ?? SidePair(new Carrier()),
            direction ?? Direction(new Carrier()),
            closure ?? Closure(new Carrier()),
            TensionSlot());

    /// <summary>Creates a standalone offset request over a source surface.</summary>
    public static Relation Request(Carrier root, IElement source) =>
        RequestWithEvidence(root, source, Evidence(new Carrier()));

    /// <summary>Creates a standalone offset request with explicit distance evidence.</summary>
    public static Relation RequestWithDistance(Carrier root, IElement source, IElement distance) =>
        RequestWithEvidence(root, source, Evidence(new Carrier(), distance));

    /// <summary>Creates a standalone offset request with explicit offset evidence.</summary>
    public static Relation RequestWithEvidence(Carrier root, IElement source, IElement evidence) =>
        Frame(
            root,
            source ?? TensionSlot(),
            evidence ?? Evidence(new Carrier()),
            TensionSlot());

    /// <summary>
    /// Creates local interior/exterior side evidence. The paired sites are
    /// opposed faces of the same unresolved side witness.
    /// </summary>
    public static Relation SidePair(Carrier root)
    {
        var interior = Site();
        var exterior = interior.Dual();
        return Frame(root, interior, exterior, TensionSlot());
    }

    /// <summary>Creates a positive-flow direction surface for an offset read.</summary>
    public static Relation Direction(Carrier root) =>
        Frame(root, FlowSlot(), TensionSlot());

    /// <summary>Creates unresolved closure evidence for open or partial offsets.</summary>
    public static Relation Closure(Carrier root)
    {
        var closureSite = Site();
        return Frame(root, new Relation(closureSite, closureSite), TensionSlot());
    }

    /// <summary>Reads an offset evidence surface.</summary>
    public static bool TryReadEvidence(
        Relation evidence,
        out IElement normalBasis,
        out IElement distance,
        out IElement sidePair,
        out IElement direction,
        out IElement closure,
        out IElement residueOrTension)
    {
        if (TryReadFrameElements(evidence, out var elements) &&
            elements.Length == 6)
        {
            normalBasis = elements[0];
            distance = elements[1];
            sidePair = elements[2];
            direction = elements[3];
            closure = elements[4];
            residueOrTension = elements[5];
            return true;
        }

        normalBasis = default!;
        distance = default!;
        sidePair = default!;
        direction = default!;
        closure = default!;
        residueOrTension = default!;
        return false;
    }

    /// <summary>Reads a standalone offset request.</summary>
    public static bool TryReadRequest(
        Relation request,
        out IElement source,
        out Relation evidence,
        out IElement residueOrTension)
    {
        if (TryReadFrameElements(request, out var elements) &&
            elements.Length == 3 &&
            elements[1] is Relation evidenceRelation &&
            TryReadEvidence(
                evidenceRelation,
                out _,
                out _,
                out _,
                out _,
                out _,
                out _))
        {
            source = elements[0];
            evidence = evidenceRelation;
            residueOrTension = elements[2];
            return true;
        }

        source = default!;
        evidence = default!;
        residueOrTension = default!;
        return false;
    }

    /// <summary>Reads the interior/exterior side pair.</summary>
    public static bool TryReadSidePair(
        Relation sidePair,
        out IElement interior,
        out IElement exterior,
        out IElement residueOrTension)
    {
        if (TryReadFrameElements(sidePair, out var elements) &&
            elements.Length == 3)
        {
            interior = elements[0];
            exterior = elements[1];
            residueOrTension = elements[2];
            return true;
        }

        interior = default!;
        exterior = default!;
        residueOrTension = default!;
        return false;
    }

    /// <summary>Reads the direction surface.</summary>
    public static bool TryReadDirection(
        Relation direction,
        out IElement flow,
        out IElement residueOrTension)
    {
        if (TryReadFrameElements(direction, out var elements) &&
            elements.Length == 2)
        {
            flow = elements[0];
            residueOrTension = elements[1];
            return true;
        }

        flow = default!;
        residueOrTension = default!;
        return false;
    }

    private static Relation TensionSlot()
    {
        var slot = Site();
        return new Relation(slot, slot.Dual());
    }

    private static Placeholder Site() => new();

    private static Relation OrthogonalSlot() =>
        Relation.OrthogonalOutlet(Site());

    private static Relation FlowSlot()
    {
        var flow = Site();
        return new Relation(flow.Dual(), flow);
    }

    private static bool TryReadFrameElements(Relation frame, out IElement[] elements)
    {
        if (frame?.Right is Carrier holder &&
            frame.TryReadOrderedElementsOn(holder, out elements))
        {
            return true;
        }

        elements = [];
        return false;
    }

    private static Carrier RootOrNew(Carrier root) => root ?? new Carrier();
}
