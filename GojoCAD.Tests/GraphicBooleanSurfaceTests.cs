using Gojo.Core;
using GojoCAD.Structures;

namespace GojoCAD.Tests;

public sealed class GraphicBooleanSurfaceTests
{
    [Fact]
    public void AreaUnionCarriesAreasOutlineSettlementAndResidue()
    {
        var frame = new Carrier();
        var x = new Carrier();
        var y = new Carrier();
        Relation Point(long xTicks, long yTicks) =>
            GraphicStructure.FramePoint(frame, new CarrierLocation(x, xTicks), new CarrierLocation(y, yTicks));

        var lowerFill = RectangleFill(Point, 0, 0, 20, 10);
        var upperFill = RectangleFill(Point, 0, 10, 20, 20);
        var outline = RectangleOutline(Point, 0, 0, 20, 20);
        var areaFrame = GraphicBooleanSurface.AreaFrame(new Carrier(), lowerFill, upperFill);
        var union = GraphicBooleanSurface.AreaUnion(new Carrier(), areaFrame, outline);

        Assert.True(GraphicBooleanSurface.TryReadAreaUnion(
            union,
            out var readAreaFrame,
            out var readOutline,
            out var settlement,
            out var residue));
        Assert.Same(areaFrame, readAreaFrame);
        Assert.Same(outline, readOutline);
        Assert.Contains(lowerFill, Walk(union));
        Assert.Contains(upperFill, Walk(union));
        Assert.Contains(outline, Walk(union));
        Assert.Contains(settlement, Walk(union));
        Assert.Contains(residue, Walk(union));

        var readFrame = Assert.IsType<Relation>(readAreaFrame);
        Assert.True(GraphicBooleanSurface.TryReadAreaFrame(readFrame, out var areas));
        Assert.Collection(
            areas,
            area => Assert.Same(lowerFill, area),
            area => Assert.Same(upperFill, area));
    }

    [Fact]
    public void AreaUnionReadRejectsPlainFourSlotFramesWithoutSettlementEvidence()
    {
        var frame = new Carrier();
        var x = new Carrier();
        var y = new Carrier();
        Relation Point(long xTicks, long yTicks) =>
            GraphicStructure.FramePoint(frame, new CarrierLocation(x, xTicks), new CarrierLocation(y, yTicks));

        var fill = RectangleFill(Point, 0, 0, 20, 10);
        var outline = RectangleOutline(Point, 0, 0, 20, 10);
        var areaFrame = GraphicBooleanSurface.AreaFrame(new Carrier(), fill);
        var ordinaryEvidence = GraphicBooleanSurface.Frame(new Carrier(), areaFrame, outline);
        var residue = GraphicBooleanSurface.Frame(new Carrier());
        var plainFourSlotFrame = GraphicBooleanSurface.Frame(
            new Carrier(),
            areaFrame,
            outline,
            ordinaryEvidence,
            residue);

        Assert.False(GraphicBooleanSurface.TryReadAreaUnion(
            plainFourSlotFrame,
            out _,
            out _,
            out _,
            out _));
    }

    [Fact]
    public void BoundarySettlementCarriesDualAreaAndLineCancellationSlots()
    {
        var settlement = GraphicBooleanSurface.BoundarySettlement(new Carrier());

        Assert.True(GraphicBooleanSurface.TryReadBoundarySettlement(
            settlement,
            out var areaBoundary,
            out var inverseAreaBoundary,
            out var lineBoundary,
            out var inverseLineBoundary,
            out var residue));
        var areaRelation = Assert.IsType<Relation>(areaBoundary);
        var lineRelation = Assert.IsType<Relation>(lineBoundary);

        Assert.Equal(areaRelation.Dual(), inverseAreaBoundary);
        Assert.Equal(lineRelation.Dual(), inverseLineBoundary);
        Assert.Contains(residue, Walk(settlement));
    }

    private static Relation RectangleFill(
        Func<long, long, Relation> point,
        long left,
        long bottom,
        long right,
        long top)
    {
        var lowerLeft = point(left, bottom);
        var lowerRight = point(right, bottom);
        var upperRight = point(right, top);
        var upperLeft = point(left, top);
        return GraphicStructure.AreaFill(
            GraphicStructure.FrameSegment(lowerLeft, lowerRight),
            GraphicStructure.FrameSegment(lowerRight, upperRight),
            GraphicStructure.FrameSegment(upperRight, upperLeft),
            GraphicStructure.FrameSegment(upperLeft, lowerLeft));
    }

    private static Relation RectangleOutline(
        Func<long, long, Relation> point,
        long left,
        long bottom,
        long right,
        long top)
    {
        var lowerLeft = point(left, bottom);
        var lowerRight = point(right, bottom);
        var upperRight = point(right, top);
        var upperLeft = point(left, top);
        return Chain(
            GraphicStructure.FrameSegment(lowerLeft, lowerRight),
            GraphicStructure.FrameSegment(lowerRight, upperRight),
            GraphicStructure.FrameSegment(upperRight, upperLeft),
            GraphicStructure.FrameSegment(upperLeft, lowerLeft));
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

    private static IEnumerable<IElement> Walk(IElement root)
    {
        yield return root;

        if (root is not Relation relation)
        {
            yield break;
        }

        foreach (var element in Walk(relation.Left))
        {
            yield return element;
        }

        foreach (var element in Walk(relation.Right))
        {
            yield return element;
        }
    }
}
