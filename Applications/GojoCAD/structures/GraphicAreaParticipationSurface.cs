using Gojo.ConceptSurface;
using Gojo.Core;

namespace GojoCAD.Structures;

/// <summary>
/// Relation-shaped area participation and settlement evidence. These helpers
/// preserve area participants, outline/result evidence, settlement evidence,
/// and residue as ordinary Gojo structure; they do not decide set truth in
/// code.
/// </summary>
public static class GraphicAreaParticipationSurface
{
    /// <summary>Creates an ordered evidence frame.</summary>
    public static Relation Frame(Carrier root, params IElement[] elements) =>
        Relation.OrderedGroup(RootOrNew(root), elements ?? []);

    /// <summary>Creates an ordered frame of area or fill participants.</summary>
    public static Relation AreaFrame(Carrier root, params IElement[] areas) =>
        Frame(root, areas ?? []);

    /// <summary>
    /// Creates shared-boundary cancellation evidence. Positive area-to-area
    /// union can replace this slot with richer participation settlement
    /// evidence when occupied flow has been read.
    /// </summary>
    public static Relation BoundaryCancellationEvidence(Carrier root) =>
        BoundaryCancellationEvidence(root, AreaBoundarySlot(), LineBoundarySlot(), TensionSlot());

    /// <summary>Creates shared-boundary cancellation evidence with explicit slots.</summary>
    public static Relation BoundaryCancellationEvidence(
        Carrier root,
        Relation areaBoundary,
        Relation lineBoundary,
        IElement residueOrTension) =>
        Frame(
            root,
            areaBoundary,
            areaBoundary.Dual(),
            lineBoundary,
            lineBoundary.Dual(),
            residueOrTension ?? TensionSlot());

    /// <summary>
    /// Creates an area union surface:
    /// input areas/fills, requested/result outline, settlement evidence,
    /// residue.
    /// </summary>
    public static Relation AreaUnion(Carrier root, IElement areaFrame, IElement outline) =>
        AreaUnion(root, areaFrame, outline, BoundaryCancellationEvidence(new Carrier()), TensionSlot());

    /// <summary>Creates an area union surface with explicit settlement evidence.</summary>
    public static Relation AreaUnion(
        Carrier root,
        IElement areaFrame,
        IElement outline,
        IElement settlementEvidence,
        IElement residueOrTension) =>
        Frame(
            root,
            areaFrame ?? AreaFrame(new Carrier()),
            outline ?? TensionSlot(),
            settlementEvidence ?? BoundaryCancellationEvidence(new Carrier()),
            residueOrTension ?? TensionSlot());

    /// <summary>Reads an area union surface.</summary>
    public static bool TryReadAreaUnion(
        Relation union,
        out IElement areaFrame,
        out IElement outline,
        out IElement settlementEvidence,
        out IElement residueOrTension)
    {
        if (TryReadFrameElements(union, out var elements) &&
            elements.Length == 4 &&
            elements[0] is Relation areas &&
            TryReadAreaParticipantFrame(areas, out _) &&
            CarriesSettlementEvidence(elements[2]))
        {
            areaFrame = elements[0];
            outline = elements[1];
            settlementEvidence = elements[2];
            residueOrTension = elements[3];
            return true;
        }

        areaFrame = default!;
        outline = default!;
        settlementEvidence = default!;
        residueOrTension = default!;
        return false;
    }

    /// <summary>
    /// Reads whether a slot carries settlement evidence usable by area and
    /// outline participation surfaces.
    /// </summary>
    public static bool CarriesSettlementEvidence(IElement evidence) =>
        evidence is Relation relation &&
        (TryReadBoundaryCancellationEvidence(
                relation,
                out _,
                out _,
                out _,
                out _,
                out _) ||
            ParticipationSettlementSurface.TryRead(relation, out _));

    /// <summary>Reads an area frame created by <see cref="AreaFrame"/>.</summary>
    public static bool TryReadAreaFrame(Relation areaFrame, out IElement[] areas) =>
        TryReadFrameElements(areaFrame, out areas);

    private static bool TryReadAreaParticipantFrame(Relation areaFrame, out IElement[] areas)
    {
        if (!TryReadFrameElements(areaFrame, out areas))
        {
            return false;
        }

        return areas.Length == 0 ||
            areas.All(DirectlyReadsAreaParticipant);
    }

    private static bool DirectlyReadsAreaParticipant(IElement element)
    {
        var graph = GraphicReadGraph.From(element);
        return graph.ReadsOf<SharedFrameAreaRead>().Any() ||
            graph.ReadsOf<AreaRead>().Any();
    }

    /// <summary>Reads shared-boundary cancellation evidence.</summary>
    public static bool TryReadBoundaryCancellationEvidence(
        Relation settlement,
        out IElement areaBoundary,
        out IElement inverseAreaBoundary,
        out IElement lineBoundary,
        out IElement inverseLineBoundary,
        out IElement residueOrTension)
    {
        if (TryReadFrameElements(settlement, out var elements) &&
            elements.Length == 5)
        {
            areaBoundary = elements[0];
            inverseAreaBoundary = elements[1];
            lineBoundary = elements[2];
            inverseLineBoundary = elements[3];
            residueOrTension = elements[4];
            return true;
        }

        areaBoundary = default!;
        inverseAreaBoundary = default!;
        lineBoundary = default!;
        inverseLineBoundary = default!;
        residueOrTension = default!;
        return false;
    }

    private static Relation AreaBoundarySlot() =>
        Relation.OrthogonalOutlet(new Placeholder());

    private static Relation LineBoundarySlot()
    {
        var site = new Placeholder();
        return new Relation(site, site);
    }

    private static Relation TensionSlot()
    {
        var slot = new Placeholder();
        return new Relation(slot, slot.Dual());
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
