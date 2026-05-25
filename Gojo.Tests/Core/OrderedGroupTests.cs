using Gojo.Core;

namespace Gojo.Tests.Core;

public sealed class OrderedGroupTests
{
    [Fact]
    public void EmptyOrderedGroupReadsNoElements()
    {
        var holder = new Carrier();

        var group = Relation.OrderedGroup(holder);

        Assert.True(group.TryReadOrderedElementsOn(holder, out var elements));
        Assert.Empty(elements);
    }

    [Fact]
    public void OrderedGroupCanRepresentComponentGroundedPositionBeforeBasisAttachment()
    {
        var position = new Carrier();
        var horizontal = new Carrier();
        var vertical = new Carrier();
        var x = new CarrierLocation(horizontal, 3);
        var y = new CarrierLocation(vertical, 2);

        var point = Relation.OrderedGroup(position, x, y);

        Assert.True(point.TryReadOrderedElementsOn(position, out var elements));
        Assert.Equal(2, elements.Length);
        Assert.Equal(x, elements[0]);
        Assert.Equal(y, elements[1]);
    }

    [Fact]
    public void SameOrderedPositionGroupCanAttachToMultipleBases()
    {
        var position = new Carrier();
        var horizontal = new Carrier();
        var vertical = new Carrier();
        var basisA = new Carrier();
        var basisB = new Carrier();
        var point = Relation.OrderedGroup(
            position,
            new CarrierLocation(horizontal, 3),
            new CarrierLocation(vertical, 2));
        var attachedToA = Relation.AdditiveAttachment(basisA, point);
        var attachedToB = Relation.AdditiveAttachment(basisB, point);

        Assert.True(attachedToA.TryReadOrderedElementsOn(basisA, out var aElements));
        Assert.True(attachedToB.TryReadOrderedElementsOn(basisB, out var bElements));

        Assert.Single(aElements);
        Assert.Single(bElements);
        Assert.Same(point, aElements[0]);
        Assert.Same(point, bElements[0]);
    }

    [Fact]
    public void PolylineCanBeOrderedGroupOfComponentGroundedPositions()
    {
        var path = new Carrier();
        var xAxis = new Carrier();
        var yAxis = new Carrier();
        var p0Holder = new Carrier();
        var p1Holder = new Carrier();
        var p2Holder = new Carrier();
        var p0 = Relation.OrderedGroup(p0Holder, new CarrierLocation(xAxis, 0), new CarrierLocation(yAxis, 0));
        var p1 = Relation.OrderedGroup(p1Holder, new CarrierLocation(xAxis, 3), new CarrierLocation(yAxis, 0));
        var p2 = Relation.OrderedGroup(p2Holder, new CarrierLocation(xAxis, 3), new CarrierLocation(yAxis, 2));

        var polyline = Relation.OrderedGroup(path, p0, p1, p2);

        Assert.True(polyline.TryReadOrderedElementsOn(path, out var points));
        Assert.Equal(3, points.Length);
        Assert.Same(p0, points[0]);
        Assert.Same(p1, points[1]);
        Assert.Same(p2, points[2]);
        Assert.True(((Relation)points[1]).TryReadOrderedElementsOn(p1Holder, out var p1Components));
        Assert.Equal(new CarrierLocation(xAxis, 3), p1Components[0]);
        Assert.Equal(new CarrierLocation(yAxis, 0), p1Components[1]);
    }

    [Fact]
    public void ClosedPolylineCanShareEndpointTickReferences()
    {
        var path = new Carrier();
        var xAxis = new Carrier();
        var yAxis = new Carrier();
        var originX = new TickRef(0);
        var originY = new TickRef(0);
        var p0Holder = new Carrier();
        var p1Holder = new Carrier();
        var p2Holder = new Carrier();
        var p3Holder = new Carrier();
        var p0 = Relation.OrderedGroup(
            p0Holder,
            new CarrierLocation(xAxis, originX),
            new CarrierLocation(yAxis, originY));
        var p1 = Relation.OrderedGroup(p1Holder, new CarrierLocation(xAxis, 3), new CarrierLocation(yAxis, 0));
        var p2 = Relation.OrderedGroup(p2Holder, new CarrierLocation(xAxis, 3), new CarrierLocation(yAxis, 2));
        var p3 = Relation.OrderedGroup(
            p3Holder,
            new CarrierLocation(xAxis, originX),
            new CarrierLocation(yAxis, originY));
        var contour = Relation.OrderedGroup(path, p0, p1, p2, p3);

        Assert.True(contour.TryReadOrderedElementsOn(path, out var points));
        Assert.True(((Relation)points[0]).TryReadOrderedElementsOn(p0Holder, out var first));
        Assert.True(((Relation)points[3]).TryReadOrderedElementsOn(p3Holder, out var last));

        Assert.True(((CarrierLocation)first[0]).SameTickRefAs((CarrierLocation)last[0]));
        Assert.True(((CarrierLocation)first[1]).SameTickRefAs((CarrierLocation)last[1]));
        Assert.True(contour.TryReadOrderedClosureOn(path, out var closure));
        Assert.Equal(new Relation(points[0], points[3]), closure);
    }

    [Fact]
    public void SameValuePolylineEndpointsDoNotCloseWithoutSharedTickReferences()
    {
        var path = new Carrier();
        var xAxis = new Carrier();
        var yAxis = new Carrier();
        var p0Holder = new Carrier();
        var p1Holder = new Carrier();
        var p2Holder = new Carrier();
        var p0 = Relation.OrderedGroup(
            p0Holder,
            new CarrierLocation(xAxis, 0),
            new CarrierLocation(yAxis, 0));
        var p1 = Relation.OrderedGroup(p1Holder, new CarrierLocation(xAxis, 3), new CarrierLocation(yAxis, 0));
        var p2 = Relation.OrderedGroup(
            p2Holder,
            new CarrierLocation(xAxis, 0),
            new CarrierLocation(yAxis, 0));
        var polyline = Relation.OrderedGroup(path, p0, p1, p2);

        Assert.False(polyline.TryReadOrderedClosureOn(path, out _));
    }

    [Fact]
    public void ClosedContourCanCreateFillBasisFromOrderedStructure()
    {
        var path = new Carrier();
        var xAxis = new Carrier();
        var yAxis = new Carrier();
        var originX = new TickRef(0);
        var originY = new TickRef(0);
        var p0Holder = new Carrier();
        var p1Holder = new Carrier();
        var p2Holder = new Carrier();
        var p3Holder = new Carrier();
        var p0 = Relation.OrderedGroup(
            p0Holder,
            new CarrierLocation(xAxis, originX),
            new CarrierLocation(yAxis, originY));
        var p1 = Relation.OrderedGroup(p1Holder, new CarrierLocation(xAxis, 3), new CarrierLocation(yAxis, 0));
        var p2 = Relation.OrderedGroup(p2Holder, new CarrierLocation(xAxis, 3), new CarrierLocation(yAxis, 2));
        var p3 = Relation.OrderedGroup(
            p3Holder,
            new CarrierLocation(xAxis, originX),
            new CarrierLocation(yAxis, originY));
        var contour = Relation.OrderedGroup(path, p0, p1, p2, p3);

        Assert.True(contour.TryCreateContourFillBasisOn(path, out var fill));

        Assert.Equal(1UL, fill.Value);
        Assert.True(fill.UnitShape.TryReadOrthogonalSource(out var source));
        Assert.Equal(contour, source);
    }

    [Fact]
    public void OpenPolylineDoesNotCreateContourFillBasis()
    {
        var path = new Carrier();
        var xAxis = new Carrier();
        var yAxis = new Carrier();
        var p0Holder = new Carrier();
        var p1Holder = new Carrier();
        var p0 = Relation.OrderedGroup(p0Holder, new CarrierLocation(xAxis, 0), new CarrierLocation(yAxis, 0));
        var p1 = Relation.OrderedGroup(p1Holder, new CarrierLocation(xAxis, 3), new CarrierLocation(yAxis, 2));
        var polyline = Relation.OrderedGroup(path, p0, p1);

        Assert.False(polyline.TryCreateContourFillBasisOn(path, out _));
    }

    [Fact]
    public void OrderedPolylineGroupCanAttachToThumbnailBasisWithoutChangingPoints()
    {
        var path = new Carrier();
        var xAxis = new Carrier();
        var yAxis = new Carrier();
        var thumbnailBasis = new Carrier();
        var p0Holder = new Carrier();
        var p1Holder = new Carrier();
        var p0 = Relation.OrderedGroup(p0Holder, new CarrierLocation(xAxis, 0), new CarrierLocation(yAxis, 0));
        var p1 = Relation.OrderedGroup(p1Holder, new CarrierLocation(xAxis, 30), new CarrierLocation(yAxis, 20));
        var polyline = Relation.OrderedGroup(path, p0, p1);

        var thumbnailAttachment = Relation.AdditiveAttachment(thumbnailBasis, polyline);

        Assert.True(thumbnailAttachment.TryReadOrderedElementsOn(thumbnailBasis, out var basisElements));
        Assert.Single(basisElements);
        Assert.Same(polyline, basisElements[0]);
        Assert.True(polyline.TryReadOrderedElementsOn(path, out var points));
        Assert.Same(p0, points[0]);
        Assert.Same(p1, points[1]);
    }
}
