using Gojo.Core;
using GojoCAD.Structures;

namespace GojoCAD.Tests;

public sealed class GraphicOrthogonalFillSurfaceTests
{
    [Fact]
    public void OrthogonalFillParticipantsCreateCircleFrameWithInnerOpening()
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
        Assert.Single(areas, area => !area.IsAntiArea && area.Boundary.Circles is { Count: > 0 });
        Assert.Single(areas, area => area.IsAntiArea && area.Boundary.Circles is { Count: > 0 });
    }

    [Fact]
    public void OrthogonalFillParticipantsDoesNotTreatNativeCircleRadiusAsOpenLine()
    {
        var basis = new TestBasis();
        var circle = GraphicStructure.FrameCircle(basis.P(0, 0), basis.P(40, 0));
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
        Assert.Equal(2, areas.Length);
        Assert.All(areas, area =>
        {
            Assert.Empty(area.Boundary.Segments);
            Assert.Empty(area.Boundary.Arcs ?? []);
            Assert.Single(area.Boundary.Circles ?? []);
        });
    }

    [Fact]
    public void OrthogonalFillParticipantsUsesSameCircleFrameForEndpointCircles()
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
        Assert.Single(areas, area => !area.IsAntiArea && area.Boundary.Circles is { Count: > 0 });
        Assert.Single(areas, area => area.IsAntiArea && area.Boundary.Circles is { Count: > 0 });
    }

    [Fact]
    public void OrthogonalFillParticipantsCreateRectangleFrameWithInnerOpening()
    {
        var basis = new TestBasis();
        var rectangle = Rectangle(basis, -20, 20, -40, 40);
        var witness = GraphicOffsetSurface.BoundarySideWitness(
            new Carrier(),
            rectangle,
            rectangle,
            Group(),
            Group());

        var participants = GraphicOrthogonalFillSurface.OrthogonalFillParticipants(witness, 10);

        var areas = GraphicReadGraph.From(Group(participants))
            .ReadsOf<SharedFrameAreaRead>()
            .DistinctBy(area => ReadSourceId(area.Source))
            .ToArray();
        Assert.Single(areas, area => !area.IsAntiArea && area.Boundary.Arcs is not { Count: > 0 });
        Assert.Single(areas, area => area.IsAntiArea && area.Boundary.Arcs is not { Count: > 0 });
        Assert.Contains(areas, area =>
            !area.IsAntiArea &&
            TryReadBounds(area.Boundary, out var minX, out var maxX, out var minY, out var maxY) &&
            minX == -30 &&
            maxX == 30 &&
            minY == -50 &&
            maxY == 50);
        Assert.Contains(areas, area =>
            area.IsAntiArea &&
            TryReadBounds(area.Boundary, out var minX, out var maxX, out var minY, out var maxY) &&
            minX == -10 &&
            maxX == 10 &&
            minY == -30 &&
            maxY == 30);
    }

    [Fact]
    public void OrthogonalFillParticipantsCreateClosedSegmentLoopFrameWithInnerOpening()
    {
        var basis = new TestBasis();
        var diamond = Group(
            Segment(basis, 0, 50, 40, 0),
            Segment(basis, 40, 0, 0, -50),
            Segment(basis, 0, -50, -40, 0),
            Segment(basis, -40, 0, 0, 50));
        var witness = GraphicOffsetSurface.BoundarySideWitness(
            new Carrier(),
            diamond,
            diamond,
            Group(),
            Group());

        var participants = GraphicOrthogonalFillSurface.OrthogonalFillParticipants(witness, 8);

        var areas = GraphicReadGraph.From(Group(participants))
            .ReadsOf<SharedFrameAreaRead>()
            .DistinctBy(area => ReadSourceId(area.Source))
            .ToArray();
        Assert.Single(areas, area => !area.IsAntiArea && area.Boundary.Segments.Count == 4);
        Assert.Single(areas, area => area.IsAntiArea && area.Boundary.Segments.Count == 4);
        Assert.Contains(areas, area =>
            !area.IsAntiArea &&
            TryReadBounds(area.Boundary, out var minX, out var maxX, out var minY, out var maxY) &&
            minX < -40 &&
            maxX > 40 &&
            minY < -50 &&
            maxY > 50);
        Assert.Contains(areas, area =>
            area.IsAntiArea &&
            TryReadBounds(area.Boundary, out var minX, out var maxX, out var minY, out var maxY) &&
            minX > -40 &&
            maxX < 40 &&
            minY > -50 &&
            maxY < 50);
    }

    [Fact]
    public void OrthogonalFillParticipantsTreatSharedOpenSegmentChainAsOneJoinedBand()
    {
        var basis = new TestBasis();
        var start = basis.P(-48, 72);
        var knee = basis.P(-10, -6);
        var end = basis.P(-70, -72);
        var bentStroke = Group(
            GraphicStructure.FrameSegment(start, knee),
            GraphicStructure.FrameSegment(knee, end));
        var witness = GraphicOffsetSurface.BoundarySideWitness(
            new Carrier(),
            bentStroke,
            bentStroke,
            Group(),
            Group());

        var participants = GraphicOrthogonalFillSurface.OrthogonalFillParticipants(witness, 10);

        Assert.Single(participants);
        var area = Assert.Single(GraphicReadGraph.From(Group(participants))
            .ReadsOf<SharedFrameAreaRead>()
            .Where(area => !area.IsAntiArea)
            .DistinctBy(area => ReadSourceId(area.Source)));
        Assert.True(area.Boundary.Segments.Count >= 6);
        Assert.True(TryReadBounds(area.Boundary, out var minX, out var maxX, out var minY, out var maxY));
        Assert.True(minX < -70);
        Assert.True(maxX > -10);
        Assert.True(minY < -72);
        Assert.True(maxY > 72);
    }

    [Fact]
    public void OrthogonalFillParticipantsCanSettleCurvedAndYSeedIntoSeparatePositiveLoops()
    {
        var basis = new TestBasis();
        const long offset = 10;
        const long endpointRadius = 18;
        const long curvedOriginX = -70;
        const long yOriginX = 265;
        const long yCircleCenterY = 46;
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
        var yCenterCircle = Circle(basis, yOriginX, yCircleCenterY, 58);
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
        var areaFrame = GraphicAreaParticipationSurface.AreaFrame(new Carrier(), participants);
        var union = GraphicAreaSettlementSurface.SettledPositiveAreaUnion(new Carrier(), areaFrame);

        Assert.True(GraphicAreaParticipationSurface.TryReadAreaUnion(union, out var settledAreaFrame, out var outlineWitness, out _, out _));
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
            BoundsCover(positiveAreas, curvedOriginX - 100, curvedOriginX + 100, -110, 110),
            $"raw={string.Join(", ", participantBounds)} positive={string.Join(", ", positiveBounds)} anti={string.Join(", ", antiBounds)}");
        Assert.True(
            BoundsCover(positiveAreas, yOriginX - 80, yOriginX + 130, -100, 110),
            string.Join(", ", positiveBounds));
        var outlines = GraphicReadGraph.From(outlineWitness)
            .ReadsOf<ClosedSharedFrameBoundaryRead>()
            .DistinctBy(boundary => ReadSourceId(boundary.Source))
            .ToArray();
        Assert.True(
            outlines.Count(outline =>
                TryReadBounds(outline, out var minX, out var maxX, out var minY, out var maxY) &&
                minX >= yOriginX - 55 &&
                maxX <= yOriginX + 55 &&
                minY >= yCircleCenterY - 55 &&
                maxY <= yCircleCenterY + 55) >= 3,
            string.Join(", ", BoundsStrings(outlines)));
    }

    [Fact]
    public void OrthogonalFillParticipantsPreserveCrescentCenterOpeningAfterSettlement()
    {
        var basis = new TestBasis();
        const long offset = 10;
        const long endpointRadius = 18;
        var crescentCenterStrokes = Group(
            TrimmedArc(basis, -170, 0, -95, 108, -95, -108, endpointRadius, endpointRadius),
            TrimmedArc(basis, 170, 0, 95, -108, 95, 108, endpointRadius, endpointRadius));
        var crescentCenterCircle = Circle(basis, 0, 0, 78);
        var endpointCircles = Group(
            Circle(basis, -95, 108, endpointRadius),
            Circle(basis, -95, -108, endpointRadius),
            Circle(basis, 95, -108, endpointRadius),
            Circle(basis, 95, 108, endpointRadius));
        var source = Group(crescentCenterStrokes, crescentCenterCircle, endpointCircles);
        var witness = GraphicOffsetSurface.BoundarySideWitness(
            new Carrier(),
            source,
            source,
            Group(),
            Group());

        var participants = GraphicOrthogonalFillSurface.OrthogonalFillParticipants(witness, offset, endpointCircles);
        var areaFrame = GraphicAreaParticipationSurface.AreaFrame(new Carrier(), participants);
        var union = GraphicAreaSettlementSurface.SettledPositiveAreaUnion(new Carrier(), areaFrame);

        Assert.True(GraphicAreaParticipationSurface.TryReadAreaUnion(union, out var unionAreaFrame, out var outlineWitness, out _, out _));
        var unionAreas = GraphicReadGraph.From(unionAreaFrame)
            .ReadsOf<SharedFrameAreaRead>()
            .DistinctBy(area => ReadSourceId(area.Source))
            .ToArray();
        Assert.True(unionAreas.Any(area =>
            area.IsAntiArea &&
            TryReadBounds(area.Boundary, out var minX, out var maxX, out var minY, out var maxY) &&
            minX <= -35 &&
            maxX >= 35 &&
            minY <= -50 &&
            maxY >= 50),
            string.Join(", ", BoundsStrings(unionAreas)));
        var outlines = GraphicReadGraph.From(outlineWitness)
            .ReadsOf<ClosedSharedFrameBoundaryRead>()
            .DistinctBy(boundary => ReadSourceId(boundary.Source))
            .ToArray();
        Assert.True(outlines.Any(outline =>
            TryReadBounds(outline, out var minX, out var maxX, out var minY, out var maxY) &&
            minX >= -70 &&
            maxX <= 70 &&
            minY <= -60 &&
            maxY >= 60),
            string.Join(", ", BoundsStrings(outlines)));
    }

    [Fact]
    public void OrthogonalFillParticipantsPreserveCrescentOpeningWithBentComparisonShape()
    {
        var basis = new TestBasis();
        const long offset = 10;
        const long endpointRadius = 18;
        const long crescentOriginX = -70;
        const long bentOriginX = 265;
        var crescentCenterStrokes = Group(
            TrimmedArc(basis, crescentOriginX - 170, 0, crescentOriginX - 95, 108, crescentOriginX - 95, -108, endpointRadius, endpointRadius),
            TrimmedArc(basis, crescentOriginX + 170, 0, crescentOriginX + 95, -108, crescentOriginX + 95, 108, endpointRadius, endpointRadius));
        var crescentCenterCircle = Circle(basis, crescentOriginX, 0, 78);
        var bentLeftTop = basis.P(bentOriginX - 92, 122);
        var bentLeftKnee = basis.P(bentOriginX - 18, -6);
        var bentLeftLower = basis.P(bentOriginX - 132, -88);
        var bentRightTop = basis.P(bentOriginX + 92, 122);
        var bentRightKnee = basis.P(bentOriginX + 18, -6);
        var bentRightLower = basis.P(bentOriginX + 132, -88);
        var bentCenterStrokes = Group(
            TrimmedSegment(basis, bentOriginX - 92, 122, bentOriginX - 18, -6, endpointRadius, 0),
            TrimmedSegment(basis, bentOriginX - 18, -6, bentOriginX - 132, -88, 0, endpointRadius),
            TrimmedSegment(basis, bentOriginX + 92, 122, bentOriginX + 18, -6, endpointRadius, 0),
            TrimmedSegment(basis, bentOriginX + 18, -6, bentOriginX + 132, -88, 0, endpointRadius));
        var bentDiamond = Diamond(basis, bentOriginX, 0, 76, 88);
        var endpointCircles = Group(
            Circle(basis, crescentOriginX - 95, 108, endpointRadius),
            Circle(basis, crescentOriginX - 95, -108, endpointRadius),
            Circle(basis, crescentOriginX + 95, -108, endpointRadius),
            Circle(basis, crescentOriginX + 95, 108, endpointRadius),
            Circle(basis, bentOriginX - 92, 122, endpointRadius),
            Circle(basis, bentOriginX - 132, -88, endpointRadius),
            Circle(basis, bentOriginX + 92, 122, endpointRadius),
            Circle(basis, bentOriginX + 132, -88, endpointRadius));
        var source = Group(
            Group(crescentCenterStrokes, bentCenterStrokes),
            Group(crescentCenterCircle, bentDiamond),
            endpointCircles);
        var witness = GraphicOffsetSurface.BoundarySideWitness(
            new Carrier(),
            source,
            source,
            Group(),
            Group());

        var participants = GraphicOrthogonalFillSurface.OrthogonalFillParticipants(witness, offset, endpointCircles);
        var areaFrame = GraphicAreaParticipationSurface.AreaFrame(new Carrier(), participants);
        var union = GraphicAreaSettlementSurface.SettledPositiveAreaUnion(new Carrier(), areaFrame);

        Assert.True(GraphicAreaParticipationSurface.TryReadAreaUnion(union, out var unionAreaFrame, out var outlineWitness, out _, out _));
        var unionAreas = GraphicReadGraph.From(unionAreaFrame)
            .ReadsOf<SharedFrameAreaRead>()
            .DistinctBy(area => ReadSourceId(area.Source))
            .ToArray();
        var hasSettledCenterAntiArea = unionAreas.Any(area =>
            area.IsAntiArea &&
            TryReadBounds(area.Boundary, out var minX, out var maxX, out var minY, out var maxY) &&
            minX <= crescentOriginX - 35 &&
            maxX >= crescentOriginX + 35 &&
            minY <= -50 &&
            maxY >= 50);
        var outlines = GraphicReadGraph.From(outlineWitness)
            .ReadsOf<ClosedSharedFrameBoundaryRead>()
            .DistinctBy(boundary => ReadSourceId(boundary.Source))
            .ToArray();
        var outlineBounds = BoundsStrings(outlines);
        Assert.True(
            hasSettledCenterAntiArea &&
            outlines.Any(outline =>
                TryReadBounds(outline, out var minX, out var maxX, out var minY, out var maxY) &&
                minX >= crescentOriginX - 70 &&
                maxX <= crescentOriginX + 70 &&
                minY <= -60 &&
                maxY >= 60),
            $"areas={string.Join(", ", BoundsStrings(unionAreas))}; outlines={string.Join(", ", outlineBounds)}");
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

    private static string[] BoundsStrings(IEnumerable<ClosedSharedFrameBoundaryRead> boundaries) =>
        [.. boundaries.Select(boundary =>
        {
            if (!TryReadBounds(boundary, out var minX, out var maxX, out var minY, out var maxY))
            {
                return "unreadable";
            }

            return $"{minX}:{maxX}:{minY}:{maxY}:arcs={boundary.Arcs?.Count ?? 0}";
        })];

    private static bool BoundsCover(
        IEnumerable<SharedFrameAreaRead> areas,
        long minX,
        long maxX,
        long minY,
        long maxY)
    {
        var bounds = areas
            .Select(area => TryReadBounds(area.Boundary, out var readMinX, out var readMaxX, out var readMinY, out var readMaxY)
                ? new BoundsBox(readMinX, readMaxX, readMinY, readMaxY)
                : (BoundsBox?)null)
            .Where(box => box is not null)
            .Select(box => box!.Value)
            .Where(box =>
                box.MinX <= maxX &&
                box.MaxX >= minX &&
                box.MinY <= maxY &&
                box.MaxY >= minY)
            .ToArray();
        return bounds.Length > 0 &&
            bounds.Min(box => box.MinX) <= minX &&
            bounds.Max(box => box.MaxX) >= maxX &&
            bounds.Min(box => box.MinY) <= minY &&
            bounds.Max(box => box.MaxY) >= maxY;
    }

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

    private static Relation Diamond(TestBasis basis, long centerX, long centerY, long halfWidth, long halfHeight) =>
        Group(
            Segment(basis, centerX, centerY + halfHeight, centerX + halfWidth, centerY),
            Segment(basis, centerX + halfWidth, centerY, centerX, centerY - halfHeight),
            Segment(basis, centerX, centerY - halfHeight, centerX - halfWidth, centerY),
            Segment(basis, centerX - halfWidth, centerY, centerX, centerY + halfHeight));

    private static Relation TrimmedSegment(
        TestBasis basis,
        long startX,
        long startY,
        long endX,
        long endY,
        long startTrim,
        long endTrim)
    {
        var deltaX = endX - startX;
        var deltaY = endY - startY;
        var length = Math.Sqrt(deltaX * deltaX + deltaY * deltaY);
        var unitX = deltaX / length;
        var unitY = deltaY / length;
        return Segment(
            basis,
            startX + (long)Math.Round(unitX * startTrim),
            startY + (long)Math.Round(unitY * startTrim),
            endX - (long)Math.Round(unitX * endTrim),
            endY - (long)Math.Round(unitY * endTrim));
    }

    private static Relation TrimmedArc(
        TestBasis basis,
        long centerX,
        long centerY,
        long startX,
        long startY,
        long endX,
        long endY,
        long startTrim,
        long endTrim)
    {
        var radius = Math.Sqrt(
            Math.Pow(startX - centerX, 2) +
            Math.Pow(startY - centerY, 2));
        var startAngle = Math.Atan2(startY - centerY, startX - centerX);
        var sweep = ShortestSweep(startAngle, Math.Atan2(endY - centerY, endX - centerX));
        var direction = Math.Sign(sweep == 0d ? 1d : sweep);
        var trimmedStartAngle = startAngle + direction * startTrim / radius;
        var trimmedEndAngle = startAngle + sweep - direction * endTrim / radius;
        (long X, long Y) PointAt(double angle) => (
            centerX + (long)Math.Round(Math.Cos(angle) * radius),
            centerY + (long)Math.Round(Math.Sin(angle) * radius));
        var trimmedStart = PointAt(trimmedStartAngle);
        var trimmedEnd = PointAt(trimmedEndAngle);
        return Arc(basis, centerX, centerY, trimmedStart.X, trimmedStart.Y, trimmedEnd.X, trimmedEnd.Y);
    }

    private static Relation Segment(TestBasis basis, long startX, long startY, long endX, long endY) =>
        GraphicStructure.FrameSegment(basis.P(startX, startY), basis.P(endX, endY));

    private static Relation Arc(TestBasis basis, long centerX, long centerY, long startX, long startY, long endX, long endY) =>
        GraphicStructure.FrameArc(basis.P(centerX, centerY), basis.P(startX, startY), basis.P(endX, endY));

    private static double ShortestSweep(double startAngle, double endAngle)
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

        foreach (var circle in boundary.Circles ?? [])
        {
            IncludeCircle(circle);
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

        void IncludeCircle(SharedFrameCircleRead circle)
        {
            if (circle.Center.Coordinates.Count < 2 ||
                circle.RadiusPoint.Coordinates.Count < 2)
            {
                return;
            }

            var centerX = circle.Center.Coordinates[0].Ticks;
            var centerY = circle.Center.Coordinates[1].Ticks;
            var radiusX = circle.RadiusPoint.Coordinates[0].Ticks;
            var radiusY = circle.RadiusPoint.Coordinates[1].Ticks;
            var radius = Math.Sqrt(Math.Pow(radiusX - centerX, 2) + Math.Pow(radiusY - centerY, 2));
            Include((long)Math.Round(centerX - radius), (long)Math.Round(centerY - radius));
            Include((long)Math.Round(centerX + radius), (long)Math.Round(centerY + radius));
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

    private readonly record struct BoundsBox(long MinX, long MaxX, long MinY, long MaxY);

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
