using Gojo.Core;

namespace GojoCAD.Tests;

public sealed class GraphicStructureTests
{
    [Fact]
    public void PolylinePreservesOrderedDirectedSpans()
    {
        var holder = new Carrier();
        var axis = new Carrier();
        var a = GraphicStructure.Site(0);
        var b = GraphicStructure.Site(5);
        var c = GraphicStructure.Site(9);
        var first = GraphicStructure.DirectedSpan(axis, a, b);
        var second = GraphicStructure.DirectedSpan(axis, b, c);

        var polyline = GraphicStructure.Polyline(holder, first, second);

        Assert.True(GraphicStructure.TryReadPolyline(polyline, holder, out var spans));
        Assert.Equal([first, second], spans);
        Assert.True(GraphicStructure.TryReadDirectedSpan(spans[0], out _, out var firstEnd));
        Assert.True(GraphicStructure.TryReadDirectedSpan(spans[1], out var secondStart, out _));
        Assert.True(GraphicStructure.ShareGroundedSite(firstEnd, secondStart));
    }

    [Fact]
    public void ClosedPolylineReadsClosureFromSharedEndpointSite()
    {
        var holder = new Carrier();
        var contour = new Carrier();
        var a = GraphicStructure.Site(0);
        var b = GraphicStructure.Site(4);
        var c = GraphicStructure.Site(8);
        var first = GraphicStructure.DirectedSpan(contour, a, b);
        var second = GraphicStructure.DirectedSpan(contour, b, c);
        var third = GraphicStructure.DirectedSpan(contour, c, a);
        var polyline = GraphicStructure.Polyline(holder, first, second, third);

        Assert.True(GraphicStructure.TryReadClosedPolyline(polyline, holder, out var spans));
        Assert.Equal([first, second, third], spans);
    }

    [Fact]
    public void BranchReadsSpansConnectedToJunction()
    {
        var holder = new Carrier();
        var axis = new Carrier();
        var junctionSite = GraphicStructure.Site(10);
        var junction = GraphicStructure.Point(axis, junctionSite);
        var left = GraphicStructure.DirectedSpan(axis, GraphicStructure.Site(0), junctionSite);
        var right = GraphicStructure.DirectedSpan(axis, junctionSite, GraphicStructure.Site(20));
        var upward = GraphicStructure.DirectedSpan(axis, junctionSite, GraphicStructure.Site(30));

        var branch = GraphicStructure.Branch(holder, junction, left, right, upward);

        Assert.True(GraphicStructure.TryReadBranch(branch, holder, out var readJunction, out var spans));
        Assert.Equal(new CarrierLocation(axis, junctionSite), readJunction);
        Assert.Equal([left, right, upward], spans);
    }

    [Fact]
    public void ClothoidLikeTransitionKeepsLengthToCurvatureEvidenceUnsolved()
    {
        var holder = new Carrier();
        var pathAxis = new Carrier();
        var length = new Carrier();
        var curvature = new Carrier();
        var selectedSpan = GraphicStructure.DirectedSpan(pathAxis, 0, 100);

        var transition = GraphicStructure.ClothoidLikeTransition(
            holder,
            selectedSpan,
            length,
            curvature,
            lengthTicks: 100,
            curvatureTicks: 12);

        Assert.True(transition.TryReadOrderedElementsOn(holder, out var elements));
        Assert.Equal(2, elements.Length);
        Assert.Equal(selectedSpan, elements[0]);
        var lengthToCurvature = Assert.IsType<Relation>(elements[1]);
        Assert.True(GraphicStructure.TryReadDirectedSpan(
            Assert.IsType<Relation>(lengthToCurvature.Left),
            out var lengthStart,
            out var lengthEnd));
        Assert.True(GraphicStructure.TryReadDirectedSpan(
            Assert.IsType<Relation>(lengthToCurvature.Right),
            out var curvatureStart,
            out var curvatureEnd));
        Assert.Same(length, lengthStart.Carrier);
        Assert.Same(length, lengthEnd.Carrier);
        Assert.Same(curvature, curvatureStart.Carrier);
        Assert.Same(curvature, curvatureEnd.Carrier);
        Assert.Equal(100, lengthEnd.Ticks);
        Assert.Equal(12, curvatureEnd.Ticks);
    }
}
