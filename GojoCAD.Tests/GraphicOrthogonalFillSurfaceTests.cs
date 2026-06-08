using Gojo.Core;
using GojoCAD.Structures;

namespace GojoCAD.Tests;

public sealed class GraphicOrthogonalFillSurfaceTests
{
    [Fact]
    public void OrthogonalFillParticipantsCreateInnerAndOuterCircleBands()
    {
        var basis = new TestBasis();
        var circle = Circle(basis, 0, 0, 40);
        var witness = GraphicOffsetSurface.BoundarySideWitness(
            new Carrier(),
            circle,
            circle,
            Group(),
            Group());

        var participants = GraphicOrthogonalFillSurface.OrthogonalFillParticipants(witness, 10);

        var areas = GraphicReadGraph.From(Group(participants))
            .ReadsOf<SharedFrameAreaRead>()
            .DistinctBy(area => ReadSourceId(area.Source))
            .ToArray();
        Assert.Equal(8, areas.Count(area => !area.IsAntiArea && area.Boundary.Arcs is { Count: > 0 }));
        Assert.DoesNotContain(areas, area => area.IsAntiArea);
    }

    [Fact]
    public void OrthogonalFillParticipantsCanKeepEndpointCircleOutwardOnly()
    {
        var basis = new TestBasis();
        var circle = Circle(basis, 0, 0, 40);
        var witness = GraphicOffsetSurface.BoundarySideWitness(
            new Carrier(),
            circle,
            circle,
            Group(),
            Group());

        var participants = GraphicOrthogonalFillSurface.OrthogonalFillParticipants(witness, 10, circle);

        var areas = GraphicReadGraph.From(Group(participants))
            .ReadsOf<SharedFrameAreaRead>()
            .DistinctBy(area => ReadSourceId(area.Source))
            .ToArray();
        Assert.Equal(4, areas.Count(area => !area.IsAntiArea && area.Boundary.Arcs is { Count: > 0 }));
        Assert.DoesNotContain(areas, area => area.IsAntiArea);
    }

    [Fact]
    public void OrthogonalFillParticipantsCanSettleCurvedAndYSeedIntoSeparatePositiveLoops()
    {
        var basis = new TestBasis();
        const long offset = 10;
        const long endpointRadius = 18;
        const long curvedOriginX = -70;
        const long yOriginX = 265;
        var curvedCenterStrokes = Group(
            Arc(basis, curvedOriginX, 112, curvedOriginX - 78, 108, curvedOriginX + 78, 108),
            Arc(basis, curvedOriginX, -112, curvedOriginX - 108, -92, curvedOriginX + 108, -92));
        var curvedCenterSquare = Rectangle(basis, curvedOriginX - 32, curvedOriginX + 32, -68, 48);
        var yJoint = basis.P(yOriginX, 38);
        var yBottomJoint = basis.P(yOriginX, -82);
        var yCenterStrokes = Group(
            Segment(basis, yOriginX - 92, 116, yOriginX, 38),
            Segment(basis, yOriginX + 92, 116, yOriginX, 38),
            GraphicStructure.FrameSegment(yJoint, yBottomJoint),
            Segment(basis, yOriginX, -82, yOriginX - 110, -82),
            Segment(basis, yOriginX, -82, yOriginX + 110, -82));
        var yCenterCircle = Circle(basis, yOriginX, 20, 58);
        var endpointCircles = Group(
            Circle(basis, curvedOriginX - 78, 108, endpointRadius),
            Circle(basis, curvedOriginX + 78, 108, endpointRadius),
            Circle(basis, curvedOriginX - 108, -92, endpointRadius),
            Circle(basis, curvedOriginX + 108, -92, endpointRadius),
            Circle(basis, yOriginX - 92, 116, endpointRadius),
            Circle(basis, yOriginX + 92, 116, endpointRadius),
            Circle(basis, yOriginX - 110, -82, endpointRadius),
            Circle(basis, yOriginX + 110, -82, endpointRadius));
        var source = Group(
            Group(curvedCenterStrokes, yCenterStrokes),
            Group(curvedCenterSquare, yCenterCircle),
            endpointCircles);
        var witness = GraphicOffsetSurface.BoundarySideWitness(
            new Carrier(),
            source,
            source,
            Group(),
            Group());

        var participants = GraphicOrthogonalFillSurface.OrthogonalFillParticipants(witness, offset, endpointCircles);
        var participantAreas = GraphicReadGraph.From(Group(participants))
            .ReadsOf<SharedFrameAreaRead>()
            .Where(area => !area.IsAntiArea)
            .DistinctBy(area => ReadSourceId(area.Source))
            .ToArray();
        var participantBounds = BoundsStrings(participantAreas);
        Assert.True(
            participantAreas.Any(area =>
                TryReadBounds(area.Boundary, out var minX, out var maxX, out var minY, out var maxY) &&
                minX <= curvedOriginX - 80 &&
                maxX >= curvedOriginX + 80 &&
                minY >= 20 &&
                maxY >= 100),
            string.Join(", ", participantBounds));
        var areaFrame = GraphicBooleanSurface.AreaFrame(new Carrier(), participants);
        var union = GraphicAreaSettlementSurface.SettledPositiveAreaUnion(new Carrier(), areaFrame);

        Assert.True(GraphicBooleanSurface.TryReadAreaUnion(union, out var settledAreaFrame, out _, out _, out _));
        var positiveAreas = GraphicReadGraph.From(settledAreaFrame)
            .ReadsOf<SharedFrameAreaRead>()
            .Where(area => !area.IsAntiArea)
            .DistinctBy(area => ReadSourceId(area.Source))
            .ToArray();
        var antiAreas = GraphicReadGraph.From(settledAreaFrame)
            .ReadsOf<SharedFrameAreaRead>()
            .Where(area => area.IsAntiArea)
            .DistinctBy(area => ReadSourceId(area.Source))
            .ToArray();
        var positiveBounds = BoundsStrings(positiveAreas);
        var antiBounds = BoundsStrings(antiAreas);
        Assert.True(
            positiveAreas.Any(area =>
                TryReadBounds(area.Boundary, out var minX, out var maxX, out var minY, out var maxY) &&
                minX <= curvedOriginX - 100 &&
                maxX >= curvedOriginX + 100 &&
                minY <= -110 &&
                maxY >= 110),
            $"raw={string.Join(", ", participantBounds)} positive={string.Join(", ", positiveBounds)} anti={string.Join(", ", antiBounds)}");
        Assert.True(
            positiveAreas.Any(area =>
                TryReadBounds(area.Boundary, out var minX, out var maxX, out var minY, out var maxY) &&
                minX <= yOriginX - 130 &&
                maxX >= yOriginX + 130 &&
                minY <= -100 &&
                maxY >= 120),
            string.Join(", ", positiveBounds));
    }

    private static string[] BoundsStrings(IEnumerable<SharedFrameAreaRead> areas) =>
        [.. areas.Select(area =>
        {
            if (!TryReadBounds(area.Boundary, out var minX, out var maxX, out var minY, out var maxY))
            {
                return "unreadable";
            }

            return $"{minX}:{maxX}:{minY}:{maxY}:arcs={area.Boundary.Arcs?.Count ?? 0}";
        })];

    private static Relation Circle(TestBasis basis, long centerX, long centerY, long radius) =>
        Group(
            GraphicStructure.FrameArc(basis.P(centerX, centerY), basis.P(centerX + radius, centerY), basis.P(centerX, centerY + radius)),
            GraphicStructure.FrameArc(basis.P(centerX, centerY), basis.P(centerX, centerY + radius), basis.P(centerX - radius, centerY)),
            GraphicStructure.FrameArc(basis.P(centerX, centerY), basis.P(centerX - radius, centerY), basis.P(centerX, centerY - radius)),
            GraphicStructure.FrameArc(basis.P(centerX, centerY), basis.P(centerX, centerY - radius), basis.P(centerX + radius, centerY)));

    private static Relation Rectangle(TestBasis basis, long minX, long maxX, long minY, long maxY) =>
        Group(
            Segment(basis, minX, minY, maxX, minY),
            Segment(basis, maxX, minY, maxX, maxY),
            Segment(basis, maxX, maxY, minX, maxY),
            Segment(basis, minX, maxY, minX, minY));

    private static Relation Segment(TestBasis basis, long startX, long startY, long endX, long endY) =>
        GraphicStructure.FrameSegment(basis.P(startX, startY), basis.P(endX, endY));

    private static Relation Arc(TestBasis basis, long centerX, long centerY, long startX, long startY, long endX, long endY) =>
        GraphicStructure.FrameArc(basis.P(centerX, centerY), basis.P(startX, startY), basis.P(endX, endY));

    private static bool TryReadBounds(
        ClosedSharedFrameBoundaryRead boundary,
        out long minX,
        out long maxX,
        out long minY,
        out long maxY)
    {
        var readMinX = 0L;
        var readMaxX = 0L;
        var readMinY = 0L;
        var readMaxY = 0L;
        var sawPoint = false;

        void Include(long x, long y)
        {
            if (!sawPoint)
            {
                readMinX = readMaxX = x;
                readMinY = readMaxY = y;
                sawPoint = true;
                return;
            }

            readMinX = Math.Min(readMinX, x);
            readMaxX = Math.Max(readMaxX, x);
            readMinY = Math.Min(readMinY, y);
            readMaxY = Math.Max(readMaxY, y);
        }

        void IncludePoint(SharedFramePointRead point)
        {
            if (point.Coordinates.Count >= 2)
            {
                Include(point.Coordinates[0].Ticks, point.Coordinates[1].Ticks);
            }
        }

        foreach (var segment in boundary.Segments)
        {
            IncludePoint(segment.Start);
            IncludePoint(segment.End);
        }

        foreach (var arc in boundary.Arcs ?? [])
        {
            IncludeArc(arc);
        }

        minX = readMinX;
        maxX = readMaxX;
        minY = readMinY;
        maxY = readMaxY;
        return sawPoint;

        void IncludeArc(SharedFrameArcRead arc)
        {
            if (arc.Center.Coordinates.Count < 2 ||
                arc.Start.Coordinates.Count < 2 ||
                arc.End.Coordinates.Count < 2)
            {
                return;
            }

            var centerX = arc.Center.Coordinates[0].Ticks;
            var centerY = arc.Center.Coordinates[1].Ticks;
            var startX = arc.Start.Coordinates[0].Ticks;
            var startY = arc.Start.Coordinates[1].Ticks;
            var endX = arc.End.Coordinates[0].Ticks;
            var endY = arc.End.Coordinates[1].Ticks;
            var radius = (Math.Sqrt(Math.Pow(startX - centerX, 2) + Math.Pow(startY - centerY, 2)) +
                Math.Sqrt(Math.Pow(endX - centerX, 2) + Math.Pow(endY - centerY, 2))) * 0.5d;
            var startAngle = Math.Atan2(startY - centerY, startX - centerX);
            var sweep = ShortestSweep(startAngle, Math.Atan2(endY - centerY, endX - centerX));
            for (var index = 0; index <= 32; index++)
            {
                var angle = startAngle + sweep * (index / 32d);
                Include(
                    (long)Math.Round(centerX + Math.Cos(angle) * radius),
                    (long)Math.Round(centerY + Math.Sin(angle) * radius));
            }
        }

        static double ShortestSweep(double startAngle, double endAngle)
        {
            var sweep = endAngle - startAngle;
            while (sweep <= -Math.PI)
            {
                sweep += Math.PI * 2d;
            }

            while (sweep > Math.PI)
            {
                sweep -= Math.PI * 2d;
            }

            return sweep;
        }
    }

    private static Relation Group(params IElement[] elements) =>
        Relation.OrderedGroup(new Carrier(), elements ?? []);

    private static ulong ReadSourceId(IElement source) =>
        source is Relation relation ? relation.Id : 0UL;

    private sealed class TestBasis
    {
        private readonly Carrier _frame = new();
        private readonly Carrier _x = new();
        private readonly Carrier _y = new();

        public Relation P(long xTicks, long yTicks) =>
            GraphicStructure.FramePoint(
                _frame,
                new CarrierLocation(_x, xTicks),
                new CarrierLocation(_y, yTicks));
    }
}
