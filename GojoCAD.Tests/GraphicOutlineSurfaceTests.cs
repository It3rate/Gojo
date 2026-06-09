using Gojo.Core;
using GojoCAD.Structures;

namespace GojoCAD.Tests;

public sealed class GraphicOutlineSurfaceTests
{
    [Fact]
    public void FromAreaConnectsAreaOutlineSettlementAndResidue()
    {
        var area = GraphicConceptShape.Area(new Carrier(), GraphicConceptShape.ClosedBoundary(new Carrier(), GraphicConceptShape.Section(new Carrier())));
        var outline = GraphicConceptShape.ClosedBoundary(new Carrier(), GraphicConceptShape.Section(new Carrier()));
        var settlement = GraphicAreaParticipationSurface.BoundaryCancellationEvidence(new Carrier());
        var residue = GraphicConceptShape.TensionSlot();

        var derivation = GraphicOutlineSurface.FromArea(new Carrier(), area, outline, settlement, residue);

        Assert.True(GraphicOutlineSurface.TryReadFromArea(
            derivation,
            out var readArea,
            out var readOutline,
            out var readSettlement,
            out var readResidue));
        Assert.Same(area, readArea);
        Assert.Same(outline, readOutline);
        Assert.Same(settlement, readSettlement);
        Assert.Same(residue, readResidue);
    }

    [Fact]
    public void BoundaryOutlinesFromAreasReadsExistingAreaBoundaries()
    {
        var frame = new Carrier();
        var x = new Carrier();
        var y = new Carrier();
        Relation P(long xTicks, long yTicks) =>
            GraphicStructure.FramePoint(frame, new CarrierLocation(x, xTicks), new CarrierLocation(y, yTicks));
        var square = Chain(
            GraphicStructure.FrameSegment(P(0, 0), P(10, 0)),
            GraphicStructure.FrameSegment(P(10, 0), P(10, 10)),
            GraphicStructure.FrameSegment(P(10, 10), P(0, 10)),
            GraphicStructure.FrameSegment(P(0, 10), P(0, 0)));
        var fill = GraphicStructure.AreaFill(square);
        var areaFrame = GraphicAreaParticipationSurface.AreaFrame(new Carrier(), fill);

        var outlines = GraphicOutlineSurface.BoundaryOutlinesFromAreas(new Carrier(), areaFrame);

        Assert.True(TryReadOrderedFrame(outlines, out var elements));
        var outline = Assert.Single(elements);
        Assert.Single(GraphicReadGraph.From(outline).ReadsOf<ClosedSharedFrameBoundaryRead>());
    }

    private static Relation Chain(params IElement[] elements)
    {
        IElement structure = elements[^1];
        for (var index = elements.Length - 2; index >= 0; index--)
        {
            structure = new Relation(elements[index], structure);
        }

        return (Relation)structure;
    }

    private static bool TryReadOrderedFrame(IElement element, out IElement[] elements)
    {
        if (element is Relation relation &&
            relation.Right is Carrier holder &&
            relation.TryReadOrderedElementsOn(holder, out elements))
        {
            return true;
        }

        elements = [];
        return false;
    }
}
