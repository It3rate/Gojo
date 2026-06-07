using Gojo.Core;
using GojoCAD.Structures;

namespace GojoCAD.Tests;

public sealed class GraphicAreaSettlementSurfaceTests
{
    [Fact]
    public void SettledAreaFrameSplitsLocalRectangularOpeningAcrossPositiveBar()
    {
        var basis = new TestBasis();
        var frame = Chain(
            GraphicStructure.AreaFill(Rectangle(basis, -50, 50, -50, 50)),
            GraphicStructure.AntiAreaFill(Rectangle(basis, -24, 24, -24, 24)));
        var bar = GraphicStructure.AreaFill(Rectangle(basis, -6, 6, -50, 50));
        var areaFrame = GraphicBooleanSurface.AreaFrame(new Carrier(), frame, bar);

        var settled = GraphicAreaSettlementSurface.SettledAreaFrame(new Carrier(), areaFrame);

        var areas = GraphicReadGraph.From(settled)
            .ReadsOf<SharedFrameAreaRead>()
            .DistinctBy(area => ReadSourceId(area.Source))
            .ToArray();
        var antiAreas = areas.Where(area => area.IsAntiArea).ToArray();
        Assert.True(antiAreas.Length >= 2);
        Assert.DoesNotContain(antiAreas, area =>
            TryReadBounds(area.Boundary, out var minX, out var maxX, out var minY, out var maxY) &&
            minX == -24 && maxX == 24 && minY == -24 && maxY == 24);
    }

    [Fact]
    public void SettledAreaFrameKeepsArcBoundariesWhenBarSplitsCircularOpening()
    {
        var basis = new TestBasis();
        var ring = Chain(
            GraphicStructure.AreaFill(Circle(basis, 0, 0, 60)),
            GraphicStructure.AntiAreaFill(Circle(basis, 0, 0, 34)));
        var bar = GraphicStructure.AreaFill(Rectangle(basis, -7, 7, -60, 60));
        var areaFrame = GraphicBooleanSurface.AreaFrame(new Carrier(), ring, bar);

        var settled = GraphicAreaSettlementSurface.SettledAreaFrame(new Carrier(), areaFrame);

        var antiAreas = GraphicReadGraph.From(settled)
            .ReadsOf<SharedFrameAreaRead>()
            .Where(area => area.IsAntiArea)
            .DistinctBy(area => ReadSourceId(area.Source))
            .ToArray();
        Assert.True(antiAreas.Length >= 2);
        Assert.Contains(antiAreas, area => area.Boundary.Arcs is { Count: > 0 });
    }

    private static Relation Rectangle(TestBasis basis, long minX, long maxX, long minY, long maxY) =>
        Chain(
            GraphicStructure.FrameSegment(basis.P(minX, minY), basis.P(maxX, minY)),
            GraphicStructure.FrameSegment(basis.P(maxX, minY), basis.P(maxX, maxY)),
            GraphicStructure.FrameSegment(basis.P(maxX, maxY), basis.P(minX, maxY)),
            GraphicStructure.FrameSegment(basis.P(minX, maxY), basis.P(minX, minY)));

    private static Relation Circle(TestBasis basis, long centerX, long centerY, long radius) =>
        Chain(
            GraphicStructure.FrameArc(basis.P(centerX, centerY), basis.P(centerX + radius, centerY), basis.P(centerX, centerY + radius)),
            GraphicStructure.FrameArc(basis.P(centerX, centerY), basis.P(centerX, centerY + radius), basis.P(centerX - radius, centerY)),
            GraphicStructure.FrameArc(basis.P(centerX, centerY), basis.P(centerX - radius, centerY), basis.P(centerX, centerY - radius)),
            GraphicStructure.FrameArc(basis.P(centerX, centerY), basis.P(centerX, centerY - radius), basis.P(centerX + radius, centerY)));

    private static Relation Chain(params IElement[] elements)
    {
        IElement structure = elements[^1];
        for (var index = elements.Length - 2; index >= 0; index--)
        {
            structure = new Relation(elements[index], structure);
        }

        return (Relation)structure;
    }

    private static bool TryReadBounds(
        ClosedSharedFrameBoundaryRead boundary,
        out long minX,
        out long maxX,
        out long minY,
        out long maxY)
    {
        var points = boundary.Segments
            .SelectMany(segment => new[] { segment.Start, segment.End })
            .Where(point => point.Coordinates.Count >= 2)
            .Select(point => (X: point.Coordinates[0].Ticks, Y: point.Coordinates[1].Ticks))
            .ToArray();
        if (points.Length == 0)
        {
            minX = 0;
            maxX = 0;
            minY = 0;
            maxY = 0;
            return false;
        }

        minX = points.Min(point => point.X);
        maxX = points.Max(point => point.X);
        minY = points.Min(point => point.Y);
        maxY = points.Max(point => point.Y);
        return true;
    }

    private static ulong ReadSourceId(IElement source) =>
        source is Relation relation ? relation.Id : 0UL;

    private sealed class TestBasis
    {
        private readonly Carrier frame = new();
        private readonly Carrier x = new();
        private readonly Carrier y = new();

        public Relation P(long xTicks, long yTicks) =>
            GraphicStructure.FramePoint(
                frame,
                new CarrierLocation(x, xTicks),
                new CarrierLocation(y, yTicks));
    }
}
