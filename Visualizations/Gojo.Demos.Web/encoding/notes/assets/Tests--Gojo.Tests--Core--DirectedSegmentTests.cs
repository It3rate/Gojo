using Gojo.Core;
using Gojo.Reads;

namespace Gojo.Tests.Core;

public sealed class DirectedSegmentTests
{
    [Fact]
    public void DirectedSegmentUsesOpposedStartAndPositiveEndpoint()
    {
        var axis = new Carrier();

        var segment = Relation.DirectedSegment(axis, inverseStart: 2, positiveEnd: 3);

        Assert.True(segment.TryReadDirectedSegment(out var start, out var end));
        Assert.True(start.Carrier.SameIdentityAs(axis.Inverse()));
        Assert.Equal(2, start.Ticks);
        Assert.True(end.Carrier.SameIdentityAs(axis));
        Assert.Equal(3, end.Ticks);
    }

    [Fact]
    public void FusedCarrierFaceReadAcceptsEitherRelationOrder()
    {
        var axis = new Carrier();
        var forwardFirst = new Relation(Relation.Point(axis, 3), Relation.Point(axis.Inverse(), 2));
        var inverseFirst = new Relation(Relation.Point(axis.Inverse(), 2), Relation.Point(axis, 3));

        Assert.True(forwardFirst.TryReadFusedCarrierFaces(out var firstInverse, out var firstPositive));
        Assert.True(inverseFirst.TryReadFusedCarrierFaces(out var secondInverse, out var secondPositive));
        Assert.Equal(new CarrierLocation(axis.Inverse(), 2), firstInverse);
        Assert.Equal(new CarrierLocation(axis, 3), firstPositive);
        Assert.Equal(firstInverse, secondInverse);
        Assert.Equal(firstPositive, secondPositive);
    }

    [Fact]
    public void DirectedSegmentProductDistributesEndpointParticipation()
    {
        var lengthShape = new Carrier().UnitShape;
        var x = new Carrier(lengthShape);
        var y = new Carrier(lengthShape);
        var area = new Carrier(UnitShape.Product(lengthShape, lengthShape));
        var xSegment = Relation.DirectedSegment(x, inverseStart: 2, positiveEnd: 3);
        var ySegment = Relation.DirectedSegment(y, inverseStart: 4, positiveEnd: 5);

        var product = DirectedSegmentReads.DirectedSegmentProduct(xSegment, ySegment, area, out var terms);

        Assert.True(product.TryReadOrderedElementsOn((Carrier)product.Right, out var elements));
        Assert.Equal(terms, elements);
        AssertTerm(terms[0], area.Inverse(), 8);
        AssertTerm(terms[1], area.Inverse(), 10);
        AssertTerm(terms[2], area.Inverse(), 12);
        AssertTerm(terms[3], area, 15);

        static void AssertTerm(Relation term, Carrier expectedCarrier, long expectedTicks)
        {
            var target = Assert.IsType<CarrierLocation>(term.Left);
            Assert.True(target.Carrier.SameIdentityAs(expectedCarrier));
            Assert.Equal(expectedTicks, target.Ticks);
            Assert.IsType<Relation>(term.Right);
        }
    }

    [Fact]
    public void FusedCarrierFaceProductIsOrderIndependent()
    {
        var lengthShape = new Carrier().UnitShape;
        var x = new Carrier(lengthShape);
        var y = new Carrier(lengthShape);
        var area = new Carrier(UnitShape.Product(lengthShape, lengthShape));
        var xForwardFirst = new Relation(Relation.Point(x, 3), Relation.Point(x.Inverse(), 2));
        var yInverseFirst = Relation.DirectedSegment(y, inverseStart: 4, positiveEnd: 5);

        var product = FusedCarrierFaceReads.FusedCarrierFaceProduct(xForwardFirst, yInverseFirst, area, out var terms);

        Assert.True(product.TryReadOrderedElementsOn((Carrier)product.Right, out var elements));
        Assert.Equal(terms, elements);
        AssertTerm(terms[0], area.Inverse(), 8);
        AssertTerm(terms[1], area.Inverse(), 10);
        AssertTerm(terms[2], area.Inverse(), 12);
        AssertTerm(terms[3], area, 15);

        static void AssertTerm(Relation term, Carrier expectedCarrier, long expectedTicks)
        {
            var target = Assert.IsType<CarrierLocation>(term.Left);
            Assert.True(target.Carrier.SameIdentityAs(expectedCarrier));
            Assert.Equal(expectedTicks, target.Ticks);
        }
    }

    [Fact]
    public void FusedCarrierFaceFoldCancelsPositiveAndOpposedFaces()
    {
        var carrier = new Carrier();
        var foldedPositive = FusedCarrierFaceReads.FoldCarrierFaces(
            carrier,
            new CarrierLocation(carrier, 15),
            new CarrierLocation(carrier.Inverse(), 8));
        var foldedInverse = FusedCarrierFaceReads.FoldCarrierFaces(
            carrier,
            new CarrierLocation(carrier, 3),
            new CarrierLocation(carrier.Inverse(), 10));

        Assert.Equal(new CarrierLocation(carrier, 7), foldedPositive);
        Assert.Equal(new CarrierLocation(carrier.Inverse(), 7), foldedInverse);
    }
}
