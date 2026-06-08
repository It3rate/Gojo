using Gojo.Core;
using GojoCAD.Structures;

namespace GojoCAD.Tests;

public sealed class GraphicBoundaryConceptSurfaceTests
{
    [Fact]
    public void CuspCandidateDomainFindsCornersOnClosedSharedFrameOutline()
    {
        var frame = new Carrier();
        var x = new Carrier();
        var y = new Carrier();
        Relation P(long xTicks, long yTicks) =>
            GraphicStructure.FramePoint(frame, new CarrierLocation(x, xTicks), new CarrierLocation(y, yTicks));
        var outline = Chain(
            GraphicStructure.FrameSegment(P(0, 0), P(20, 0)),
            GraphicStructure.FrameSegment(P(20, 0), P(20, 12)),
            GraphicStructure.FrameSegment(P(20, 12), P(0, 12)),
            GraphicStructure.FrameSegment(P(0, 12), P(0, 0)));
        var witnessSlot = new Placeholder();
        var pattern = GraphicBoundaryConceptSurface.CuspWitnessPattern(new Carrier(), witnessSlot);

        var domain = GraphicBoundaryConceptSurface.CuspCandidateDomain(new Carrier(), outline);
        var selection = GraphicSelectionSurface.Search(new Carrier(), outline, domain, pattern);

        Assert.True(GraphicSelectionSurface.TryReadSelection(
            selection,
            out _,
            out _,
            out _,
            out var readMatches,
            out _));
        Assert.True(GraphicSelectionSurface.TryReadMatchFrame(
            Assert.IsType<Relation>(readMatches),
            out var matches));
        Assert.Equal(4, matches.Length);
        Assert.All(matches, match =>
        {
            Assert.True(GraphicSelectionSurface.TryReadMatch(
                Assert.IsType<Relation>(match),
                out _,
                out _,
                out var bindings,
                out _,
                out _));
            Assert.True(Gojo.Structures.FocusSurface.TryReadBindingFrame(bindings, out var clauses));
            Assert.Contains(clauses, clause => ReferenceEquals(clause.Left, witnessSlot));
        });
    }

    [Fact]
    public void CuspCandidateDomainSkipsSameCenterArcContinuations()
    {
        var frame = new Carrier();
        var x = new Carrier();
        var y = new Carrier();
        Relation P(long xTicks, long yTicks) =>
            GraphicStructure.FramePoint(frame, new CarrierLocation(x, xTicks), new CarrierLocation(y, yTicks));
        var center = P(0, 0);
        var right = P(10, 0);
        var top = P(0, 10);
        var left = P(-10, 0);
        var bottom = P(0, -10);
        var circle = Chain(
            GraphicStructure.FrameArc(center, right, top),
            GraphicStructure.FrameArc(center, top, left),
            GraphicStructure.FrameArc(center, left, bottom),
            GraphicStructure.FrameArc(center, bottom, right));

        var domain = GraphicBoundaryConceptSurface.CuspCandidateDomain(new Carrier(), circle);

        Assert.True(TryReadOrderedFrame(domain, out var candidates));
        Assert.Empty(candidates);
    }

    [Fact]
    public void RoundedBoundaryFromCuspSelectionReplacesSelectedLineCornersWithArcs()
    {
        var frame = new Carrier();
        var x = new Carrier();
        var y = new Carrier();
        Relation P(long xTicks, long yTicks) =>
            GraphicStructure.FramePoint(frame, new CarrierLocation(x, xTicks), new CarrierLocation(y, yTicks));
        var outline = Chain(
            GraphicStructure.FrameSegment(P(0, 0), P(20, 0)),
            GraphicStructure.FrameSegment(P(20, 0), P(20, 12)),
            GraphicStructure.FrameSegment(P(20, 12), P(0, 12)),
            GraphicStructure.FrameSegment(P(0, 12), P(0, 0)));
        var pattern = GraphicBoundaryConceptSurface.CuspWitnessPattern(new Carrier(), new Placeholder());
        var domain = GraphicBoundaryConceptSurface.CuspCandidateDomain(new Carrier(), outline);
        var selection = GraphicSelectionSurface.Search(new Carrier(), outline, domain, pattern);

        var action = GraphicBoundaryConceptSurface.RoundedBoundaryFromCuspSelection(
            new Carrier(),
            selection,
            outline,
            radiusTicks: 3);

        Assert.True(GraphicBoundaryConceptSurface.TryReadRoundedBoundaryFromCuspSelection(
            action,
            out var readSelection,
            out var roundedEvidence,
            out _,
            out var roundedFrame,
            out _));
        Assert.Same(selection, readSelection);
        Assert.True(GraphicConceptShape.TryReadRoundedCuspJoinEvidence(
            Assert.IsType<Relation>(roundedEvidence),
            out _,
            out _,
            out _,
            out _));
        var roundedBoundary = Assert.Single(GraphicReadGraph.From(roundedFrame).ReadsOf<ClosedSharedFrameBoundaryRead>());
        Assert.Equal(4, roundedBoundary.Arcs?.Count ?? 0);
    }

    [Fact]
    public void RoundedBoundaryFromCuspSelectionReplacesSelectedLineArcCornersWithArcs()
    {
        var frame = new Carrier();
        var x = new Carrier();
        var y = new Carrier();
        Relation P(long xTicks, long yTicks) =>
            GraphicStructure.FramePoint(frame, new CarrierLocation(x, xTicks), new CarrierLocation(y, yTicks));
        var outline = Chain(
            GraphicStructure.FrameSegment(P(0, 0), P(20, 0)),
            GraphicStructure.FrameArc(P(30, 10), P(20, 0), P(40, 10)),
            GraphicStructure.FrameSegment(P(40, 10), P(0, 20)),
            GraphicStructure.FrameSegment(P(0, 20), P(0, 0)));
        var pattern = GraphicBoundaryConceptSurface.CuspWitnessPattern(new Carrier(), new Placeholder());
        var domain = GraphicBoundaryConceptSurface.CuspCandidateDomain(new Carrier(), outline);
        var selection = GraphicSelectionSurface.Search(new Carrier(), outline, domain, pattern);

        var action = GraphicBoundaryConceptSurface.RoundedBoundaryFromCuspSelection(
            new Carrier(),
            selection,
            outline,
            radiusTicks: 6);

        Assert.True(GraphicBoundaryConceptSurface.TryReadRoundedBoundaryFromCuspSelection(
            action,
            out _,
            out _,
            out _,
            out var roundedFrame,
            out var residue));
        var roundedBoundary = Assert.Single(GraphicReadGraph.From(roundedFrame).ReadsOf<ClosedSharedFrameBoundaryRead>());
        var arcCount = roundedBoundary.Arcs?.Count ?? 0;
        Assert.True(arcCount >= 5, $"arcCount={arcCount}");
    }

    [Fact]
    public void LineTipCandidateDomainFindsOpenSharedFrameTerminals()
    {
        var frame = new Carrier();
        var x = new Carrier();
        var y = new Carrier();
        Relation P(long xTicks, long yTicks) =>
            GraphicStructure.FramePoint(frame, new CarrierLocation(x, xTicks), new CarrierLocation(y, yTicks));
        var plus = Chain(
            GraphicStructure.FrameSegment(P(-20, 0), P(20, 0)),
            GraphicStructure.FrameSegment(P(0, -20), P(0, 20)));
        var witnessSlot = new Placeholder();
        var pattern = GraphicBoundaryConceptSurface.LineTipWitnessPattern(new Carrier(), witnessSlot);

        var domain = GraphicBoundaryConceptSurface.LineTipCandidateDomain(new Carrier(), plus);
        var selection = GraphicSelectionSurface.Search(new Carrier(), plus, domain, pattern);

        Assert.True(GraphicSelectionSurface.TryReadSelection(
            selection,
            out _,
            out _,
            out _,
            out var readMatches,
            out _));
        Assert.True(GraphicSelectionSurface.TryReadMatchFrame(
            Assert.IsType<Relation>(readMatches),
            out var matches));
        Assert.Equal(4, matches.Length);
    }

    [Fact]
    public void LineTipCandidateDomainFindsOpenSharedFrameArcTerminals()
    {
        var frame = new Carrier();
        var x = new Carrier();
        var y = new Carrier();
        Relation P(long xTicks, long yTicks) =>
            GraphicStructure.FramePoint(frame, new CarrierLocation(x, xTicks), new CarrierLocation(y, yTicks));
        var arc = GraphicStructure.FrameArc(P(0, 50), P(-30, 0), P(30, 0));
        var pattern = GraphicBoundaryConceptSurface.LineTipWitnessPattern(new Carrier(), new Placeholder());

        var domain = GraphicBoundaryConceptSurface.LineTipCandidateDomain(new Carrier(), arc);
        var selection = GraphicSelectionSurface.Search(new Carrier(), arc, domain, pattern);

        Assert.True(GraphicSelectionSurface.TryReadSelection(
            selection,
            out _,
            out _,
            out _,
            out var readMatches,
            out _));
        Assert.True(GraphicSelectionSurface.TryReadMatchFrame(
            Assert.IsType<Relation>(readMatches),
            out var matches));
        Assert.Equal(2, matches.Length);
    }

    [Fact]
    public void CircleCapsFromLineTipSelectionCreatesCircleOutlinesAtSelectedTips()
    {
        var frame = new Carrier();
        var x = new Carrier();
        var y = new Carrier();
        Relation P(long xTicks, long yTicks) =>
            GraphicStructure.FramePoint(frame, new CarrierLocation(x, xTicks), new CarrierLocation(y, yTicks));
        var plus = Chain(
            GraphicStructure.FrameSegment(P(-20, 0), P(20, 0)),
            GraphicStructure.FrameSegment(P(0, -20), P(0, 20)));
        var pattern = GraphicBoundaryConceptSurface.LineTipWitnessPattern(new Carrier(), new Placeholder());
        var domain = GraphicBoundaryConceptSurface.LineTipCandidateDomain(new Carrier(), plus);
        var selection = GraphicSelectionSurface.Search(new Carrier(), plus, domain, pattern);

        var action = GraphicBoundaryConceptSurface.CircleCapsFromLineTipSelection(new Carrier(), selection, radiusTicks: 6);

        Assert.True(GraphicBoundaryConceptSurface.TryReadCircleCapsFromLineTipSelection(
            action,
            out var readSelection,
            out var capEvidence,
            out _,
            out var circleFrame,
            out _));
        Assert.Same(selection, readSelection);
        Assert.True(GraphicConceptShape.TryReadCircularTipCapEvidence(
            Assert.IsType<Relation>(capEvidence),
            out _,
            out _,
            out _,
            out _));
        Assert.True(TryReadOrderedFrame(circleFrame, out var circles));
        Assert.Equal(4, circles.Length);
        Assert.Equal(4, GraphicReadGraph.From(circleFrame).ReadsOf<ClosedSharedFrameBoundaryRead>().Count());
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
