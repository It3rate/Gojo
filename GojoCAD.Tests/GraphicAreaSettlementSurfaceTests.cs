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

    [Fact]
    public void SettledPositiveAreaFrameKeepsOpeningsAsOutlinesWithoutAntiAreaFills()
    {
        var basis = new TestBasis();
        var frame = Chain(
            GraphicStructure.AreaFill(Rectangle(basis, -50, 50, -50, 50)),
            GraphicStructure.AntiAreaFill(Rectangle(basis, -24, 24, -24, 24)));
        var bar = GraphicStructure.AreaFill(Rectangle(basis, -6, 6, -50, 50));
        var areaFrame = GraphicBooleanSurface.AreaFrame(new Carrier(), frame, bar);

        var settledPositive = GraphicAreaSettlementSurface.SettledPositiveAreaFrame(new Carrier(), areaFrame);
        var openingOutlines = GraphicAreaSettlementSurface.SettledAntiBoundaryOutlinesFromAreas(new Carrier(), areaFrame);

        var areas = GraphicReadGraph.From(settledPositive)
            .ReadsOf<SharedFrameAreaRead>()
            .DistinctBy(area => ReadSourceId(area.Source))
            .ToArray();
        var openings = GraphicReadGraph.From(openingOutlines)
            .ReadsOf<ClosedSharedFrameBoundaryRead>()
            .DistinctBy(boundary => ReadSourceId(boundary.Source))
            .ToArray();
        Assert.NotEmpty(areas);
        Assert.DoesNotContain(areas, area => area.IsAntiArea);
        Assert.True(openings.Length >= 2);
    }

    [Fact]
    public void SettledAreaFrameKeepsCurvedArmCompositeAsPositiveStructure()
    {
        var basis = new TestBasis();
        const long offset = 10;
        const long endpointOuter = 28;
        const long endpointInner = 8;
        var centerFrame = Chain(
            GraphicStructure.AreaFill(Rectangle(basis, -32, 32, -68, 48)),
            GraphicStructure.AntiAreaFill(Rectangle(basis, -24, 24, -52, 32)));
        var areaFrame = GraphicBooleanSurface.AreaFrame(
            new Carrier(),
            GraphicStructure.AreaFill(ArcBand(basis, 0, 112, -78, 108, 78, 108, offset)),
            GraphicStructure.AreaFill(ArcBand(basis, 0, -112, -108, -92, 108, -92, offset)),
            centerFrame,
            RingFill(basis, -78, 108, endpointOuter, endpointInner),
            RingFill(basis, 78, 108, endpointOuter, endpointInner),
            RingFill(basis, -108, -92, endpointOuter, endpointInner),
            RingFill(basis, 108, -92, endpointOuter, endpointInner));

        var settled = GraphicAreaSettlementSurface.SettledAreaFrame(new Carrier(), areaFrame);

        var areas = GraphicReadGraph.From(settled)
            .ReadsOf<SharedFrameAreaRead>()
            .DistinctBy(area => ReadSourceId(area.Source))
            .ToArray();
        var positiveAreas = areas.Where(area => !area.IsAntiArea).ToArray();
        var antiAreas = areas.Where(area => area.IsAntiArea).ToArray();
        Assert.Contains(positiveAreas, area =>
            TryReadBounds(area.Boundary, out var minX, out var maxX, out var minY, out var maxY) &&
            minX <= -110 &&
            maxX >= 110 &&
            minY <= -110 &&
            maxY >= 110);
        Assert.True(antiAreas.Length >= 2);
        Assert.Contains(antiAreas, area => area.Boundary.Arcs is { Count: > 0 });
    }

    [Fact]
    public void SettledAreaFrameKeepsSeparatedCurvedAndYComposites()
    {
        var basis = new TestBasis();
        const long offset = 10;
        const long endpointOuter = 28;
        const long endpointInner = 8;
        const long curvedOriginX = -70;
        const long yOriginX = 265;
        var curvedCenterFrame = Chain(
            GraphicStructure.AreaFill(Rectangle(basis, curvedOriginX - 32, curvedOriginX + 32, -68, 48)),
            GraphicStructure.AntiAreaFill(Rectangle(basis, curvedOriginX - 24, curvedOriginX + 24, -52, 32)));
        var yStem = GraphicStructure.AreaFill(Chain(
            Segment(basis, yOriginX - 130, -102, yOriginX + 130, -102),
            Segment(basis, yOriginX + 130, -102, yOriginX + 130, -62),
            Segment(basis, yOriginX + 130, -62, yOriginX + 18, -62),
            Segment(basis, yOriginX + 18, -62, yOriginX + 18, 20),
            Segment(basis, yOriginX + 18, 20, yOriginX + 104, 104),
            Segment(basis, yOriginX + 104, 104, yOriginX + 78, 130),
            Segment(basis, yOriginX + 78, 130, yOriginX, 52),
            Segment(basis, yOriginX, 52, yOriginX - 78, 130),
            Segment(basis, yOriginX - 78, 130, yOriginX - 104, 104),
            Segment(basis, yOriginX - 104, 104, yOriginX - 18, 20),
            Segment(basis, yOriginX - 18, 20, yOriginX - 18, -62),
            Segment(basis, yOriginX - 18, -62, yOriginX - 130, -62),
            Segment(basis, yOriginX - 130, -62, yOriginX - 130, -102)));
        var areaFrame = GraphicBooleanSurface.AreaFrame(
            new Carrier(),
            GraphicStructure.AreaFill(ArcBand(
                basis,
                curvedOriginX,
                112,
                curvedOriginX - 78,
                108,
                curvedOriginX + 78,
                108,
                offset)),
            GraphicStructure.AreaFill(ArcBand(
                basis,
                curvedOriginX,
                -112,
                curvedOriginX - 108,
                -92,
                curvedOriginX + 108,
                -92,
                offset)),
            curvedCenterFrame,
            RingFill(basis, curvedOriginX - 78, 108, endpointOuter, endpointInner),
            RingFill(basis, curvedOriginX + 78, 108, endpointOuter, endpointInner),
            RingFill(basis, curvedOriginX - 108, -92, endpointOuter, endpointInner),
            RingFill(basis, curvedOriginX + 108, -92, endpointOuter, endpointInner),
            yStem,
            RingFill(basis, yOriginX, 20, 58, 32),
            RingFill(basis, yOriginX - 92, 116, endpointOuter, endpointInner),
            RingFill(basis, yOriginX + 92, 116, endpointOuter, endpointInner),
            RingFill(basis, yOriginX - 110, -82, endpointOuter, endpointInner),
            RingFill(basis, yOriginX + 110, -82, endpointOuter, endpointInner));

        var settled = GraphicAreaSettlementSurface.SettledAreaFrame(new Carrier(), areaFrame);

        var positiveAreas = GraphicReadGraph.From(settled)
            .ReadsOf<SharedFrameAreaRead>()
            .Where(area => !area.IsAntiArea)
            .DistinctBy(area => ReadSourceId(area.Source))
            .ToArray();
        var antiAreas = GraphicReadGraph.From(settled)
            .ReadsOf<SharedFrameAreaRead>()
            .Where(area => area.IsAntiArea)
            .DistinctBy(area => ReadSourceId(area.Source))
            .ToArray();
        Assert.Contains(positiveAreas, area =>
            TryReadBounds(area.Boundary, out var minX, out var maxX, out var minY, out var maxY) &&
            minX <= curvedOriginX - 100 &&
            maxX >= curvedOriginX + 100 &&
            minY <= -110 &&
            maxY >= 110);
        Assert.Contains(positiveAreas, area =>
            TryReadBounds(area.Boundary, out var minX, out var maxX, out var minY, out var maxY) &&
            minX <= yOriginX - 130 &&
            maxX >= yOriginX + 130 &&
            minY <= -100 &&
            maxY >= 120);
        Assert.True(antiAreas.Count(area =>
            TryReadBounds(area.Boundary, out var minX, out var maxX, out var minY, out var maxY) &&
            minX == curvedOriginX - 24 &&
            maxX == curvedOriginX + 24 &&
            minY >= -52 &&
            maxY <= 32) >= 2);
    }

    [Fact]
    public void SettledPositiveAreaUnionKeepsSeparatedCurvedAndYCompositesAsResolvedLoops()
    {
        var basis = new TestBasis();
        const long offset = 10;
        const long endpointOuter = 28;
        const long endpointInner = 8;
        const long curvedOriginX = -70;
        const long yOriginX = 265;
        var curvedCenterFrame = Chain(
            GraphicStructure.AreaFill(Rectangle(basis, curvedOriginX - 32, curvedOriginX + 32, -68, 48)),
            GraphicStructure.AntiAreaFill(Rectangle(basis, curvedOriginX - 24, curvedOriginX + 24, -52, 32)));
        var yStem = GraphicStructure.AreaFill(Chain(
            Segment(basis, yOriginX - 130, -102, yOriginX + 130, -102),
            Segment(basis, yOriginX + 130, -102, yOriginX + 130, -62),
            Segment(basis, yOriginX + 130, -62, yOriginX + 18, -62),
            Segment(basis, yOriginX + 18, -62, yOriginX + 18, 20),
            Segment(basis, yOriginX + 18, 20, yOriginX + 104, 104),
            Segment(basis, yOriginX + 104, 104, yOriginX + 78, 130),
            Segment(basis, yOriginX + 78, 130, yOriginX, 52),
            Segment(basis, yOriginX, 52, yOriginX - 78, 130),
            Segment(basis, yOriginX - 78, 130, yOriginX - 104, 104),
            Segment(basis, yOriginX - 104, 104, yOriginX - 18, 20),
            Segment(basis, yOriginX - 18, 20, yOriginX - 18, -62),
            Segment(basis, yOriginX - 18, -62, yOriginX - 130, -62),
            Segment(basis, yOriginX - 130, -62, yOriginX - 130, -102)));
        var areaFrame = GraphicBooleanSurface.AreaFrame(
            new Carrier(),
            GraphicStructure.AreaFill(ArcBand(
                basis,
                curvedOriginX,
                112,
                curvedOriginX - 78,
                108,
                curvedOriginX + 78,
                108,
                offset)),
            GraphicStructure.AreaFill(ArcBand(
                basis,
                curvedOriginX,
                -112,
                curvedOriginX - 108,
                -92,
                curvedOriginX + 108,
                -92,
                offset)),
            curvedCenterFrame,
            RingFill(basis, curvedOriginX - 78, 108, endpointOuter, endpointInner),
            RingFill(basis, curvedOriginX + 78, 108, endpointOuter, endpointInner),
            RingFill(basis, curvedOriginX - 108, -92, endpointOuter, endpointInner),
            RingFill(basis, curvedOriginX + 108, -92, endpointOuter, endpointInner),
            yStem,
            RingFill(basis, yOriginX, 20, 58, 32),
            RingFill(basis, yOriginX - 92, 116, endpointOuter, endpointInner),
            RingFill(basis, yOriginX + 92, 116, endpointOuter, endpointInner),
            RingFill(basis, yOriginX - 110, -82, endpointOuter, endpointInner),
            RingFill(basis, yOriginX + 110, -82, endpointOuter, endpointInner));

        var union = GraphicAreaSettlementSurface.SettledPositiveAreaUnion(new Carrier(), areaFrame);

        Assert.True(GraphicBooleanSurface.TryReadAreaUnion(union, out var settledAreaFrame, out var outlineWitness, out _, out _));
        var positiveAreas = GraphicReadGraph.From(settledAreaFrame)
            .ReadsOf<SharedFrameAreaRead>()
            .Where(area => !area.IsAntiArea)
            .DistinctBy(area => ReadSourceId(area.Source))
            .ToArray();
        var outlines = GraphicReadGraph.From(outlineWitness)
            .ReadsOf<ClosedSharedFrameBoundaryRead>()
            .DistinctBy(boundary => ReadSourceId(boundary.Source))
            .ToArray();
        Assert.Contains(positiveAreas, area =>
            TryReadBounds(area.Boundary, out var minX, out var maxX, out var minY, out var maxY) &&
            minX <= curvedOriginX - 100 &&
            maxX >= curvedOriginX + 100 &&
            minY <= -110 &&
            maxY >= 110);
        Assert.Contains(positiveAreas, area =>
            TryReadBounds(area.Boundary, out var minX, out var maxX, out var minY, out var maxY) &&
            minX <= yOriginX - 130 &&
            maxX >= yOriginX + 130 &&
            minY <= -100 &&
            maxY >= 120);
        Assert.True(outlines.Count(boundary =>
            TryReadBounds(boundary, out var minX, out var maxX, out _, out _) &&
            minX >= curvedOriginX - 24 &&
            maxX <= curvedOriginX + 24) >= 2);
    }

    [Fact]
    public void SettledPositiveAreaUnionCarriesRingOpeningAsOutline()
    {
        var basis = new TestBasis();
        var ring = Chain(
            GraphicStructure.AreaFill(Circle(basis, 0, 0, 58)),
            GraphicStructure.AntiAreaFill(Circle(basis, 0, 0, 32)));
        var areaFrame = GraphicBooleanSurface.AreaFrame(new Carrier(), ring);

        var union = GraphicAreaSettlementSurface.SettledPositiveAreaUnion(new Carrier(), areaFrame);

        Assert.True(GraphicBooleanSurface.TryReadAreaUnion(union, out _, out var outlineWitness, out _, out _));
        var outlines = GraphicReadGraph.From(outlineWitness)
            .ReadsOf<ClosedSharedFrameBoundaryRead>()
            .DistinctBy(boundary => ReadSourceId(boundary.Source))
            .ToArray();
        Assert.True(outlines.Length >= 2);
        Assert.Contains(outlines, outline =>
            TryReadBounds(outline, out var minX, out var maxX, out var minY, out var maxY) &&
            minX >= -32 &&
            maxX <= 32 &&
            minY >= -32 &&
            maxY <= 32);
    }

    [Fact]
    public void SettledPositiveAreaUnionCarriesCrescentCenterOpeningAsOutline()
    {
        var basis = new TestBasis();
        const long offset = 10;
        var centerRing = RingFill(basis, 0, 0, 68, 48);
        var areaFrame = GraphicBooleanSurface.AreaFrame(
            new Carrier(),
            centerRing,
            GraphicStructure.AreaFill(ArcBand(basis, -170, 0, -95, 108, -95, -108, offset)),
            GraphicStructure.AreaFill(ArcBand(basis, 170, 0, 95, -108, 95, 108, offset)));

        var union = GraphicAreaSettlementSurface.SettledPositiveAreaUnion(new Carrier(), areaFrame);

        Assert.True(GraphicBooleanSurface.TryReadAreaUnion(union, out _, out var outlineWitness, out _, out _));
        var outlines = GraphicReadGraph.From(outlineWitness)
            .ReadsOf<ClosedSharedFrameBoundaryRead>()
            .DistinctBy(boundary => ReadSourceId(boundary.Source))
            .ToArray();
        Assert.Contains(outlines, outline =>
            TryReadBounds(outline, out var minX, out var maxX, out var minY, out var maxY) &&
            minX >= -50 &&
            maxX <= 50 &&
            minY <= -40 &&
            maxY >= 40);
    }

    [Fact]
    public void SettledAreaFrameSynthesizesCompositeLoopAcrossColinearOverlap()
    {
        var basis = new TestBasis();
        var areaFrame = GraphicBooleanSurface.AreaFrame(
            new Carrier(),
            GraphicStructure.AreaFill(Rectangle(basis, -50, 20, -10, 10)),
            GraphicStructure.AreaFill(Rectangle(basis, -20, 50, -10, 10)));

        var settled = GraphicAreaSettlementSurface.SettledAreaFrame(new Carrier(), areaFrame);

        var areas = GraphicReadGraph.From(settled)
            .ReadsOf<SharedFrameAreaRead>()
            .DistinctBy(area => ReadSourceId(area.Source))
            .ToArray();
        var positiveAreas = areas.Where(area => !area.IsAntiArea).ToArray();
        Assert.Contains(positiveAreas, area =>
            TryReadBounds(area.Boundary, out var minX, out var maxX, out var minY, out var maxY) &&
            minX == -50 &&
            maxX == 50 &&
            minY == -10 &&
            maxY == 10);
        Assert.DoesNotContain(areas, area => area.IsAntiArea);
    }

    [Fact]
    public void SettledAreaFrameSynthesizesCompositeLoopAcrossCoincidentArcOverlap()
    {
        var basis = new TestBasis();
        const long radius = 60;
        const long offset = 8;
        var firstStart = PointOnCircle(radius, 180);
        var firstEnd = PointOnCircle(radius, 45);
        var secondStart = PointOnCircle(radius, 135);
        var secondEnd = PointOnCircle(radius, 0);
        var areaFrame = GraphicBooleanSurface.AreaFrame(
            new Carrier(),
            GraphicStructure.AreaFill(ArcBand(basis, 0, 0, firstStart.X, firstStart.Y, firstEnd.X, firstEnd.Y, offset)),
            GraphicStructure.AreaFill(ArcBand(basis, 0, 0, secondStart.X, secondStart.Y, secondEnd.X, secondEnd.Y, offset)));

        var settled = GraphicAreaSettlementSurface.SettledAreaFrame(new Carrier(), areaFrame);

        var positiveAreas = GraphicReadGraph.From(settled)
            .ReadsOf<SharedFrameAreaRead>()
            .Where(area => !area.IsAntiArea)
            .DistinctBy(area => ReadSourceId(area.Source))
            .ToArray();
        Assert.Contains(positiveAreas, area =>
            area.Boundary.Arcs is { Count: > 0 } &&
            TryReadBounds(area.Boundary, out var minX, out var maxX, out _, out var maxY) &&
            minX <= -radius - offset + 1 &&
            maxX >= radius + offset - 1 &&
            maxY >= 48);
    }

    [Fact]
    public void SettledAreaFrameKeepsRoundedHExteriorWithInteriorCuts()
    {
        var basis = new TestBasis();
        const long reach = 108;
        const long columnX = 72;
        const long offset = 10;
        const long crossbarHalfWidth = 10;
        const long centerHalfX = 44;
        const long centerHalfY = 68;
        const long capRadius = 28;
        const long holeRadius = 8;
        var outline = HExteriorSharpOutline(
            basis,
            reach,
            columnX,
            offset,
            crossbarHalfWidth,
            centerHalfX,
            centerHalfY,
            capRadius);
        var pattern = GraphicBoundaryConceptSurface.CuspWitnessPattern(new Carrier(), new Placeholder());
        var domain = GraphicBoundaryConceptSurface.CuspCandidateDomain(new Carrier(), outline);
        var selection = GraphicSelectionSurface.Search(new Carrier(), outline, domain, pattern);
        var action = GraphicBoundaryConceptSurface.RoundedBoundaryFromCuspSelection(
            new Carrier(),
            selection,
            outline,
            radiusTicks: 4);
        Assert.True(GraphicBoundaryConceptSurface.TryReadRoundedBoundaryFromCuspSelection(
            action,
            out _,
            out _,
            out _,
            out var roundedFrame,
            out _));
        var areaFrame = GraphicBooleanSurface.AreaFrame(
            new Carrier(),
            [.. AreaFillsFromOutlines(roundedFrame),
            GraphicStructure.AntiAreaFill(Rectangle(basis, -34, 34, crossbarHalfWidth, 58)),
            GraphicStructure.AntiAreaFill(Rectangle(basis, -34, 34, -58, -crossbarHalfWidth)),
            GraphicStructure.AntiAreaFill(Circle(basis, -columnX, -reach, holeRadius)),
            GraphicStructure.AntiAreaFill(Circle(basis, -columnX, reach, holeRadius)),
            GraphicStructure.AntiAreaFill(Circle(basis, columnX, -reach, holeRadius)),
            GraphicStructure.AntiAreaFill(Circle(basis, columnX, reach, holeRadius))]);

        var settled = GraphicAreaSettlementSurface.SettledAreaFrame(new Carrier(), areaFrame);

        var areas = GraphicReadGraph.From(settled)
            .ReadsOf<SharedFrameAreaRead>()
            .DistinctBy(area => ReadSourceId(area.Source))
            .ToArray();
        Assert.Contains(areas.Where(area => !area.IsAntiArea), area =>
            TryReadBounds(area.Boundary, out var minX, out var maxX, out var minY, out var maxY) &&
            minX <= -90 &&
            maxX >= 90 &&
            minY <= -120 &&
            maxY >= 120);
        Assert.True(areas.Count(area => area.IsAntiArea) >= 6);
    }

    [Fact]
    public void SettledAreaFrameKeepsHPositiveWhenInteriorCutsAreRounded()
    {
        var basis = new TestBasis();
        const long reach = 108;
        const long columnX = 72;
        const long offset = 10;
        const long crossbarHalfWidth = 10;
        const long centerHalfX = 44;
        const long centerHalfY = 68;
        const long capRadius = 28;
        const long holeRadius = 8;
        var outline = HExteriorSharpOutline(
            basis,
            reach,
            columnX,
            offset,
            crossbarHalfWidth,
            centerHalfX,
            centerHalfY,
            capRadius);
        var originalHoles = GraphicBooleanSurface.Frame(
            new Carrier(),
            Rectangle(basis, -34, 34, crossbarHalfWidth, 58),
            Rectangle(basis, -34, 34, -58, -crossbarHalfWidth),
            Circle(basis, -columnX, -reach, holeRadius),
            Circle(basis, -columnX, reach, holeRadius),
            Circle(basis, columnX, -reach, holeRadius),
            Circle(basis, columnX, reach, holeRadius));
        var sharpAreaFrame = GraphicBooleanSurface.AreaFrame(
            new Carrier(),
            [GraphicStructure.AreaFill(outline), .. AntiAreaFillsFromOutlines(originalHoles)]);
        var sharpSettled = GraphicAreaSettlementSurface.SettledAreaFrame(new Carrier(), sharpAreaFrame);
        Assert.NotEmpty(PositiveAreaFillsFromAreaFrame(sharpSettled));
        var pattern = GraphicBoundaryConceptSurface.CuspWitnessPattern(new Carrier(), new Placeholder());
        var domain = GraphicBoundaryConceptSurface.CuspCandidateDomain(new Carrier(), originalHoles);
        var selection = GraphicSelectionSurface.Search(new Carrier(), sharpSettled, domain, pattern);
        var action = GraphicBoundaryConceptSurface.RoundedBoundaryFromCuspSelection(
            new Carrier(),
            selection,
            originalHoles,
            radiusTicks: 4);
        Assert.True(GraphicBoundaryConceptSurface.TryReadRoundedBoundaryFromCuspSelection(
            action,
            out _,
            out _,
            out _,
            out var roundedHoles,
            out _));
        var positiveOnlyAreaFrame = GraphicBooleanSurface.AreaFrame(new Carrier(), PositiveAreaFillsFromAreaFrame(sharpSettled));
        Assert.NotEmpty(GraphicReadGraph.From(positiveOnlyAreaFrame).ReadsOf<SharedFrameAreaRead>());
        var positiveOnlySettled = GraphicAreaSettlementSurface.SettledAreaFrame(new Carrier(), positiveOnlyAreaFrame);
        Assert.NotEmpty(PositiveAreaFillsFromAreaFrame(positiveOnlySettled));
        var roundedSourceAreaFrame = GraphicBooleanSurface.AreaFrame(
            new Carrier(),
            [.. PositiveAreaFillsFromAreaFrame(sharpSettled), .. AntiAreaFillsFromOutlines(roundedHoles)]);

        var roundedSettled = GraphicAreaSettlementSurface.SettledAreaFrame(new Carrier(), roundedSourceAreaFrame);

        var positiveAreas = GraphicReadGraph.From(roundedSettled)
            .ReadsOf<SharedFrameAreaRead>()
            .Where(area => !area.IsAntiArea)
            .DistinctBy(area => ReadSourceId(area.Source))
            .ToArray();
        Assert.Contains(positiveAreas, area =>
            TryReadBounds(area.Boundary, out var minX, out var maxX, out var minY, out var maxY) &&
            minX <= -90 &&
            maxX >= 90 &&
            minY <= -120 &&
            maxY >= 120);
    }

    private static Relation Rectangle(TestBasis basis, long minX, long maxX, long minY, long maxY) =>
        Chain(
            Segment(basis, minX, minY, maxX, minY),
            Segment(basis, maxX, minY, maxX, maxY),
            Segment(basis, maxX, maxY, minX, maxY),
            Segment(basis, minX, maxY, minX, minY));

    private static Relation Segment(TestBasis basis, long startX, long startY, long endX, long endY) =>
        GraphicStructure.FrameSegment(basis.P(startX, startY), basis.P(endX, endY));

    private static Relation Circle(TestBasis basis, long centerX, long centerY, long radius) =>
        Chain(
            GraphicStructure.FrameArc(basis.P(centerX, centerY), basis.P(centerX + radius, centerY), basis.P(centerX, centerY + radius)),
            GraphicStructure.FrameArc(basis.P(centerX, centerY), basis.P(centerX, centerY + radius), basis.P(centerX - radius, centerY)),
            GraphicStructure.FrameArc(basis.P(centerX, centerY), basis.P(centerX - radius, centerY), basis.P(centerX, centerY - radius)),
            GraphicStructure.FrameArc(basis.P(centerX, centerY), basis.P(centerX, centerY - radius), basis.P(centerX + radius, centerY)));

    private static Relation ArcBand(
        TestBasis basis,
        long centerX,
        long centerY,
        long startX,
        long startY,
        long endX,
        long endY,
        long distanceTicks)
    {
        var distance = Math.Abs(distanceTicks);
        var startRadius = Math.Sqrt(
            Math.Pow(startX - centerX, 2) +
            Math.Pow(startY - centerY, 2));
        var endRadius = Math.Sqrt(
            Math.Pow(endX - centerX, 2) +
            Math.Pow(endY - centerY, 2));
        var radius = Math.Min(startRadius, endRadius);
        (long X, long Y) OffsetPoint(long pointX, long pointY, double offsetRadius)
        {
            var dx = pointX - centerX;
            var dy = pointY - centerY;
            var length = Math.Sqrt(dx * dx + dy * dy);
            return length <= 0
                ? (pointX, pointY)
                : (
                    centerX + (long)Math.Round(dx / length * offsetRadius),
                    centerY + (long)Math.Round(dy / length * offsetRadius));
        }

        var outerStart = OffsetPoint(startX, startY, radius + distance);
        var outerEnd = OffsetPoint(endX, endY, radius + distance);
        var innerStart = OffsetPoint(startX, startY, radius - distance);
        var innerEnd = OffsetPoint(endX, endY, radius - distance);
        return Chain(
            GraphicStructure.FrameArc(basis.P(centerX, centerY), basis.P(outerStart.X, outerStart.Y), basis.P(outerEnd.X, outerEnd.Y)),
            GraphicStructure.FrameSegment(basis.P(outerEnd.X, outerEnd.Y), basis.P(innerEnd.X, innerEnd.Y)),
            GraphicStructure.FrameArc(basis.P(centerX, centerY), basis.P(innerEnd.X, innerEnd.Y), basis.P(innerStart.X, innerStart.Y)),
            GraphicStructure.FrameSegment(basis.P(innerStart.X, innerStart.Y), basis.P(outerStart.X, outerStart.Y)));
    }

    private static Relation RingFill(TestBasis basis, long centerX, long centerY, long outerRadius, long innerRadius) =>
        Chain(
            GraphicStructure.AreaFill(Circle(basis, centerX, centerY, outerRadius)),
            GraphicStructure.AntiAreaFill(Circle(basis, centerX, centerY, innerRadius)));

    private static (long X, long Y) PointOnCircle(long radius, double degrees)
    {
        var radians = degrees * Math.PI / 180d;
        return (
            (long)Math.Round(Math.Cos(radians) * radius),
            (long)Math.Round(Math.Sin(radians) * radius));
    }

    private static IElement[] AreaFillsFromOutlines(IElement outlines)
    {
        if (outlines is Relation outlineFrame &&
            outlineFrame.Right is Carrier holder &&
            outlineFrame.TryReadOrderedElementsOn(holder, out var outlineElements))
        {
            return [.. outlineElements.Select(outline => GraphicStructure.AreaFill(outline))];
        }

        return [GraphicStructure.AreaFill(outlines)];
    }

    private static IElement[] AntiAreaFillsFromOutlines(IElement outlines)
    {
        if (outlines is Relation outlineFrame &&
            outlineFrame.Right is Carrier holder &&
            outlineFrame.TryReadOrderedElementsOn(holder, out var outlineElements))
        {
            return [.. outlineElements.Select(outline => GraphicStructure.AntiAreaFill(outline))];
        }

        return [GraphicStructure.AntiAreaFill(outlines)];
    }

    private static IElement[] PositiveAreaFillsFromAreaFrame(IElement sourceAreaFrame) =>
        [.. GraphicReadGraph.From(sourceAreaFrame)
            .ReadsOf<SharedFrameAreaRead>()
            .Where(area => !area.IsAntiArea)
            .DistinctBy(area => ReadSourceId(area.Source))
            .Select(area => area.Source)];

    private static Relation HExteriorSharpOutline(
        TestBasis basis,
        long reachTicks,
        long columnX,
        long columnHalfWidth,
        long crossbarHalfWidth,
        long centerHalfX,
        long centerHalfY,
        long capRadius)
    {
        var leftColumnX = -columnX;
        var rightColumnX = columnX;
        var leftColumnInnerX = leftColumnX + columnHalfWidth;
        var rightColumnInnerX = rightColumnX - columnHalfWidth;
        var centerLeftX = -centerHalfX;
        var centerRightX = centerHalfX;
        var tangentOffsetY = (long)Math.Round(Math.Sqrt(capRadius * capRadius - columnHalfWidth * columnHalfWidth));

        (long X, long Y) TopCapLeft(long centerX) => (centerX - columnHalfWidth, reachTicks - tangentOffsetY);
        (long X, long Y) TopCapRight(long centerX) => (centerX + columnHalfWidth, reachTicks - tangentOffsetY);
        (long X, long Y) BottomCapLeft(long centerX) => (centerX - columnHalfWidth, -reachTicks + tangentOffsetY);
        (long X, long Y) BottomCapRight(long centerX) => (centerX + columnHalfWidth, -reachTicks + tangentOffsetY);

        IElement[] TopCapArcs(long centerX) =>
        [
            Arc(basis, centerX, reachTicks, centerX - columnHalfWidth, reachTicks - tangentOffsetY, centerX - capRadius, reachTicks),
            Arc(basis, centerX, reachTicks, centerX - capRadius, reachTicks, centerX, reachTicks + capRadius),
            Arc(basis, centerX, reachTicks, centerX, reachTicks + capRadius, centerX + capRadius, reachTicks),
            Arc(basis, centerX, reachTicks, centerX + capRadius, reachTicks, centerX + columnHalfWidth, reachTicks - tangentOffsetY)
        ];

        IElement[] BottomCapArcs(long centerX) =>
        [
            Arc(basis, centerX, -reachTicks, centerX + columnHalfWidth, -reachTicks + tangentOffsetY, centerX + capRadius, -reachTicks),
            Arc(basis, centerX, -reachTicks, centerX + capRadius, -reachTicks, centerX, -reachTicks - capRadius),
            Arc(basis, centerX, -reachTicks, centerX, -reachTicks - capRadius, centerX - capRadius, -reachTicks),
            Arc(basis, centerX, -reachTicks, centerX - capRadius, -reachTicks, centerX - columnHalfWidth, -reachTicks + tangentOffsetY)
        ];

        return Chain(
            TopCapArcs(leftColumnX)
                .Append(SegmentBetween(basis, TopCapRight(leftColumnX), (leftColumnInnerX, crossbarHalfWidth)))
                .Append(Segment(basis, leftColumnInnerX, crossbarHalfWidth, centerLeftX, crossbarHalfWidth))
                .Append(Segment(basis, centerLeftX, crossbarHalfWidth, centerLeftX, centerHalfY))
                .Append(Segment(basis, centerLeftX, centerHalfY, centerRightX, centerHalfY))
                .Append(Segment(basis, centerRightX, centerHalfY, centerRightX, crossbarHalfWidth))
                .Append(Segment(basis, centerRightX, crossbarHalfWidth, rightColumnInnerX, crossbarHalfWidth))
                .Append(SegmentBetween(basis, (rightColumnInnerX, crossbarHalfWidth), TopCapLeft(rightColumnX)))
                .Concat(TopCapArcs(rightColumnX))
                .Append(SegmentBetween(basis, TopCapRight(rightColumnX), BottomCapRight(rightColumnX)))
                .Concat(BottomCapArcs(rightColumnX))
                .Append(SegmentBetween(basis, BottomCapLeft(rightColumnX), (rightColumnInnerX, -crossbarHalfWidth)))
                .Append(Segment(basis, rightColumnInnerX, -crossbarHalfWidth, centerRightX, -crossbarHalfWidth))
                .Append(Segment(basis, centerRightX, -crossbarHalfWidth, centerRightX, -centerHalfY))
                .Append(Segment(basis, centerRightX, -centerHalfY, centerLeftX, -centerHalfY))
                .Append(Segment(basis, centerLeftX, -centerHalfY, centerLeftX, -crossbarHalfWidth))
                .Append(Segment(basis, centerLeftX, -crossbarHalfWidth, leftColumnInnerX, -crossbarHalfWidth))
                .Append(SegmentBetween(basis, (leftColumnInnerX, -crossbarHalfWidth), BottomCapRight(leftColumnX)))
                .Concat(BottomCapArcs(leftColumnX))
                .Append(SegmentBetween(basis, BottomCapLeft(leftColumnX), TopCapLeft(leftColumnX)))
                .ToArray());
    }

    private static Relation SegmentBetween(TestBasis basis, (long X, long Y) start, (long X, long Y) end) =>
        Segment(basis, start.X, start.Y, end.X, end.Y);

    private static Relation Arc(
        TestBasis basis,
        long centerX,
        long centerY,
        long startX,
        long startY,
        long endX,
        long endY) =>
        GraphicStructure.FrameArc(basis.P(centerX, centerY), basis.P(startX, startY), basis.P(endX, endY));

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
