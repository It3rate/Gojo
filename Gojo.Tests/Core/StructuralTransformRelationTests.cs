using Gojo.Core;

namespace Gojo.Tests.Core;

public sealed class StructuralTransformRelationTests
{
    [Fact]
    public void PointIsRelationFromLocationToCarrier()
    {
        var carrier = new Carrier();
        var point = Point(carrier, 7);

        Assert.Equal(new CarrierLocation(carrier, 7), point.Left);
        Assert.Equal(carrier, point.Right);
        Assert.Equal(new[] { new CarrierLocation(carrier, 7) }, point.CarrierLocations());
    }

    [Fact]
    public void RelationsHaveIdentitySeparateFromShapeEquality()
    {
        var carrier = new Carrier();
        var first = Point(carrier, 7);
        var second = Point(carrier, 7);

        Assert.NotSame(first, second);
        Assert.NotEqual(first.Id, second.Id);
        Assert.Equal(first, second);
        Assert.False(first.SameIdentityAs(second));
    }

    [Fact]
    public void SharedPointReferenceMovesWhereItParticipates()
    {
        var carrier = new Carrier();
        var start = Point(carrier, 5);
        var end = Point(carrier, 11);
        var segment = new Relation(start, end);

        start.Perturb(new CarrierLocation(carrier, 3));

        Assert.Same(start, segment.Left);
        Assert.Equal(new CarrierLocation(carrier, 8), start.Left);
        Assert.Equal(new CarrierLocation(carrier, 11), end.Left);
    }

    [Fact]
    public void FreshCoincidentPointDoesNotMoveWithUnsharedSegmentPoint()
    {
        var carrier = new Carrier();
        var start = Point(carrier, 5);
        var end = Point(carrier, 11);
        var segment = new Relation(start, end);
        var coincident = Point(carrier, 5);

        segment.Perturb(new CarrierLocation(carrier, 3));

        Assert.Equal(new CarrierLocation(carrier, 8), start.Left);
        Assert.Equal(new CarrierLocation(carrier, 14), end.Left);
        Assert.Equal(new CarrierLocation(carrier, 5), coincident.Left);
    }

    [Fact]
    public void TargetedPointTranslationChangesOnlyThatPoint()
    {
        var carrier = new Carrier();
        var start = Point(carrier, 5);
        var end = Point(carrier, 11);
        _ = new Relation(start, end);

        end.Perturb(new CarrierLocation(carrier, 3));

        Assert.Equal(new CarrierLocation(carrier, 5), start.Left);
        Assert.Equal(new CarrierLocation(carrier, 14), end.Left);
    }

    [Fact]
    public void TargetedRelationTranslationMovesParticipatingPointsTogether()
    {
        var carrier = new Carrier();
        var start = Point(carrier, 5);
        var end = Point(carrier, 11);
        var segment = new Relation(start, end);

        segment.Perturb(new CarrierLocation(carrier, 3));

        Assert.Equal(new CarrierLocation(carrier, 8), start.Left);
        Assert.Equal(new CarrierLocation(carrier, 14), end.Left);
    }

    [Fact]
    public void RelationScalePerturbationPreservesCrossCarrierRatio()
    {
        var source = new Carrier();
        var target = new Carrier();
        var sourcePoint = Point(source, 2);
        var targetPoint = Point(target, 4);
        var coupling = new Relation(sourcePoint, targetPoint);
        var scale = new Relation(Point(source, 2), Point(source, 3));

        coupling.Perturb(scale);

        Assert.Equal(new CarrierLocation(source, 3), sourcePoint.Left);
        Assert.Equal(new CarrierLocation(target, 6), targetPoint.Left);
    }

    [Fact]
    public void InexactRelationScaleLeavesStructureUnchanged()
    {
        var carrier = new Carrier();
        var start = Point(carrier, 1);
        var end = Point(carrier, 2);
        var segment = new Relation(start, end);
        var scale = new Relation(Point(carrier, 2), Point(carrier, 3));

        Assert.False(segment.Perturb(scale));
        Assert.Equal(new CarrierLocation(carrier, 1), start.Left);
        Assert.Equal(new CarrierLocation(carrier, 2), end.Left);
    }

    [Fact]
    public void CarrierWithoutStructureKeepsInputAsLocalValue()
    {
        var carrier = new Carrier();

        carrier.Add(11);

        Assert.Equal(11UL, carrier.Value);
        Assert.Equal(0, carrier.Tension);
    }

    [Fact]
    public void ForwardCarrierLocationRelationDoesNotCreateTension()
    {
        var carrier = new Carrier();
        carrier.Attach(new Relation(new CarrierLocation(carrier, 0), new CarrierLocation(carrier, 10)));

        carrier.Add(11);

        Assert.Equal(11UL, carrier.Value);
        Assert.Equal(0, carrier.Tension);
    }

    [Fact]
    public void ReverseCarrierLocationRelationWrapsInsteadOfClamping()
    {
        var carrier = new Carrier();
        carrier.Attach(new Relation(new CarrierLocation(carrier, 10), new CarrierLocation(carrier, 0)));

        carrier.Add(13);

        Assert.Equal(3UL, carrier.Value);
        Assert.Equal(10, carrier.Tension);
    }

    [Fact]
    public void SameCarrierResetBoundaryAccountsResidueAndPositiveTension()
    {
        var carrier = new Carrier();
        carrier.Attach(new Relation(Point(carrier, 10), Point(carrier, 0)));

        carrier.Add(23);

        Assert.Equal(3UL, carrier.Value);
        Assert.Equal(20, carrier.Tension);
    }

    [Fact]
    public void SameCarrierClampBoundaryAccountsLimitAndPositiveTension()
    {
        var carrier = new Carrier();
        carrier.Attach(new Relation(Point(carrier, 10), Point(carrier, 10)));

        carrier.Add(23);

        Assert.Equal(10UL, carrier.Value);
        Assert.Equal(13, carrier.Tension);
    }

    [Fact]
    public void CrossCarrierPointRelationRoutesPositiveTension()
    {
        var residue = new Carrier();
        var whole = new Carrier();
        residue.Attach(new Relation(Point(residue, 10), Point(whole, 0)));

        residue.Add(37);

        Assert.Equal(7UL, residue.Value);
        Assert.Equal(0, residue.Tension);
        Assert.Equal(3UL, whole.Value);
    }

    [Fact]
    public void EquivalentCrossCarrierRoutesSharePositiveTension()
    {
        var source = new Carrier();
        var first = new Carrier();
        var second = new Carrier();
        source.Attach(new Relation(Point(source, 1), Point(first, 0)));
        source.Attach(new Relation(Point(source, 1), Point(second, 0)));

        source.Add(20);

        Assert.Equal(0UL, source.Value);
        Assert.Equal(0, source.Tension);
        Assert.Equal(10UL, first.Value);
        Assert.Equal(10UL, second.Value);
    }

    [Fact]
    public void EquivalentBoundaryRoutesShareCarriedQuantaAndPreserveResidue()
    {
        var source = new Carrier();
        var first = new Carrier();
        var second = new Carrier();
        source.Attach(new Relation(Point(source, 10), Point(first, 0)));
        source.Attach(new Relation(Point(source, 10), Point(second, 0)));

        source.Add(47);

        Assert.Equal(7UL, source.Value);
        Assert.Equal(0, source.Tension);
        Assert.Equal(2UL, first.Value);
        Assert.Equal(2UL, second.Value);
    }

    [Fact]
    public void FiniteTargetClampRejectsUnabsorbedDirectRouteTension()
    {
        var source = new Carrier();
        var target = new Carrier();
        target.Attach(new Relation(Point(target, 3), Point(target, 3)));
        source.Attach(new Relation(Point(source, 1), Point(target, 0)));

        source.Add(8);

        Assert.Equal(0UL, source.Value);
        Assert.Equal(5, source.Tension);
        Assert.Equal(3UL, target.Value);
        Assert.Equal(0, target.Tension);
    }

    [Fact]
    public void RouteAllocationRedistributesAroundFiniteTargetClamp()
    {
        var source = new Carrier();
        var limited = new Carrier();
        var open = new Carrier();
        limited.Attach(new Relation(Point(limited, 3), Point(limited, 3)));
        source.Attach(new Relation(Point(source, 1), Point(limited, 0)));
        source.Attach(new Relation(Point(source, 1), Point(open, 0)));

        source.Add(10);

        Assert.Equal(0UL, source.Value);
        Assert.Equal(0, source.Tension);
        Assert.Equal(3UL, limited.Value);
        Assert.Equal(0, limited.Tension);
        Assert.Equal(7UL, open.Value);
    }

    [Fact]
    public void RouteAllocationLeavesRejectedTicksOnSourceWhenTargetsAreFull()
    {
        var source = new Carrier();
        var first = new Carrier();
        var second = new Carrier();
        first.Attach(new Relation(Point(first, 2), Point(first, 2)));
        second.Attach(new Relation(Point(second, 3), Point(second, 3)));
        source.Attach(new Relation(Point(source, 1), Point(first, 0)));
        source.Attach(new Relation(Point(source, 1), Point(second, 0)));

        source.Add(9);

        Assert.Equal(0UL, source.Value);
        Assert.Equal(4, source.Tension);
        Assert.Equal(2UL, first.Value);
        Assert.Equal(0, first.Tension);
        Assert.Equal(3UL, second.Value);
        Assert.Equal(0, second.Tension);
    }

    [Fact]
    public void DuplicateRoutesToSameFiniteTargetShareTargetCapacity()
    {
        var source = new Carrier();
        var target = new Carrier();
        target.Attach(new Relation(Point(target, 3), Point(target, 3)));
        source.Attach(new Relation(Point(source, 1), Point(target, 0)));
        source.Attach(new Relation(Point(source, 1), Point(target, 0)));

        source.Add(10);

        Assert.Equal(0UL, source.Value);
        Assert.Equal(7, source.Tension);
        Assert.Equal(3UL, target.Value);
        Assert.Equal(0, target.Tension);
    }

    [Fact]
    public void MultipleSourcesShareFreedFiniteTargetCapacity()
    {
        var first = new Carrier();
        var second = new Carrier();
        var target = new Carrier();
        target.Attach(new Relation(Point(target, 5), Point(target, 5)));
        first.Attach(new Relation(Point(first, 1), Point(target, 0)));
        second.Attach(new Relation(Point(second, 1), Point(target, 0)));
        target.Add(5);
        first.Add(4);
        second.Add(4);

        target.Remove(4);

        Assert.Equal(0UL, first.Value);
        Assert.Equal(2, first.Tension);
        Assert.Equal(0UL, second.Value);
        Assert.Equal(2, second.Tension);
        Assert.Equal(5UL, target.Value);
        Assert.Equal(0, target.Tension);
    }

    [Fact]
    public void MultipleSourcesRedistributeUnusedTargetAcceptance()
    {
        var small = new Carrier();
        var large = new Carrier();
        var target = new Carrier();
        target.Attach(new Relation(Point(target, 5), Point(target, 5)));
        small.Attach(new Relation(Point(small, 1), Point(target, 0)));
        large.Attach(new Relation(Point(large, 1), Point(target, 0)));
        target.Add(5);
        small.Add(1);
        large.Add(5);

        target.Remove(4);

        Assert.Equal(0UL, small.Value);
        Assert.Equal(0, small.Tension);
        Assert.Equal(0UL, large.Value);
        Assert.Equal(2, large.Tension);
        Assert.Equal(5UL, target.Value);
        Assert.Equal(0, target.Tension);
    }

    [Fact]
    public void CrossCarrierNonzeroPointRelationDoesNotAbsorbAsCarry()
    {
        var source = new Carrier();
        var projection = new Carrier();
        source.Attach(new Relation(Point(source, 2), Point(projection, 3)));

        source.Add(6);

        Assert.Equal(6UL, source.Value);
        Assert.Equal(0, source.Tension);
        Assert.Equal(0UL, projection.Value);
        Assert.Equal(0, projection.Tension);
    }

    [Fact]
    public void CrossCarrierNonzeroPointRelationProjectsExactRatio()
    {
        var source = new Carrier();
        var target = new Carrier();
        var ratio = new Relation(Point(source, 2), Point(target, 3));

        var projected = ratio.Project(new CarrierLocation(source, 6), target);

        Assert.Equal(
            new Relation(
                new CarrierLocation(target, 9),
                new CarrierLocation(source, 0)),
            projected);
    }

    [Fact]
    public void RatioProjectionCanBeReadBackwardsAsDivision()
    {
        var source = new Carrier();
        var target = new Carrier();
        var ratio = new Relation(Point(source, 2), Point(target, 3));

        var projected = ratio.Project(new CarrierLocation(target, 9), source);

        Assert.Equal(
            new Relation(
                new CarrierLocation(source, 6),
                new CarrierLocation(target, 0)),
            projected);
    }

    [Fact]
    public void ContractiveRatioLeavesResidueOnOriginalCarrier()
    {
        var source = new Carrier();
        var target = new Carrier();
        var ratio = new Relation(Point(source, 3), Point(target, 1));

        var projected = ratio.Project(new CarrierLocation(source, 10), target);

        Assert.Equal(
            new Relation(
                new CarrierLocation(target, 3),
                new CarrierLocation(source, 1)),
            projected);
    }

    [Fact]
    public void ExpansiveRatioCanRepresentSubSourceTicksWithoutResidue()
    {
        var source = new Carrier();
        var target = new Carrier();
        var ratio = new Relation(Point(source, 2), Point(target, 4));

        var projected = ratio.Project(new CarrierLocation(source, 1), target);

        Assert.Equal(
            new Relation(
                new CarrierLocation(target, 2),
                new CarrierLocation(source, 0)),
            projected);
    }

    [Fact]
    public void PointToNegativePointProjectsSubtractionShape()
    {
        var source = new Carrier();
        var target = new Carrier();
        var subtract = new Relation(Point(source, 1), Point(target, -1));

        var projected = subtract.Project(new CarrierLocation(source, 7), target);

        Assert.Equal(
            new Relation(
                new CarrierLocation(target, -7),
                new CarrierLocation(source, 0)),
            projected);
    }

    [Fact]
    public void NegativePointToPositivePointAlsoProjectsNegativeRatio()
    {
        var source = new Carrier();
        var target = new Carrier();
        var negativeRatio = new Relation(Point(source, -2), Point(target, 3));

        var projected = negativeRatio.Project(new CarrierLocation(source, 4), target);

        Assert.Equal(
            new Relation(
                new CarrierLocation(target, -6),
                new CarrierLocation(source, 0)),
            projected);
    }

    [Fact]
    public void NegativePointToNegativePointProjectsPositiveRatio()
    {
        var source = new Carrier();
        var target = new Carrier();
        var positiveRatio = new Relation(Point(source, -2), Point(target, -3));

        var projected = positiveRatio.Project(new CarrierLocation(source, 4), target);

        Assert.Equal(
            new Relation(
                new CarrierLocation(target, 6),
                new CarrierLocation(source, 0)),
            projected);
    }

    [Fact]
    public void NegativeRatioCanBeReadBackwardsAsSignedDivision()
    {
        var source = new Carrier();
        var target = new Carrier();
        var negativeRatio = new Relation(Point(source, 2), Point(target, -3));

        var projected = negativeRatio.Project(new CarrierLocation(target, -9), source);

        Assert.Equal(
            new Relation(
                new CarrierLocation(source, 6),
                new CarrierLocation(target, 0)),
            projected);
    }

    [Fact]
    public void SegmentToSegmentRelationProjectsByOrderedBasisLengths()
    {
        var source = new Carrier();
        var target = new Carrier();
        var ratio = new Relation(Segment(source, 0, 2), Segment(target, 0, 3));

        var projected = ratio.Project(new CarrierLocation(source, 6), target);

        Assert.Equal(
            new Relation(
                new CarrierLocation(target, 9),
                new CarrierLocation(source, 0)),
            projected);
    }

    [Fact]
    public void OriginSegmentProjectsValueAsWholeAndResidueThroughUnitRead()
    {
        var source = new Carrier();
        var unit = new Carrier();
        var read = new Relation(Segment(source, 0, 10), Point(unit, 1));

        var projected = read.Project(new CarrierLocation(source, 23), unit);

        Assert.Equal(
            new Relation(
                new CarrierLocation(unit, 2),
                new CarrierLocation(source, 3)),
            projected);
    }

    [Fact]
    public void OffsetSegmentProjectionUsesFirstEndpointAsInspectionOrigin()
    {
        var source = new Carrier();
        var unit = new Carrier();
        var read = new Relation(Segment(source, 5, 15), Point(unit, 1));

        var projected = read.Project(new CarrierLocation(source, 28), unit);

        Assert.Equal(
            new Relation(
                new CarrierLocation(unit, 2),
                new CarrierLocation(source, 3)),
            projected);
    }

    [Fact]
    public void ReversedOffsetSourceSegmentUsesFirstEndpointAsInspectionOrigin()
    {
        var source = new Carrier();
        var target = new Carrier();
        var ratio = new Relation(Segment(source, 2, 0), Segment(target, 0, 3));

        var projected = ratio.Project(new CarrierLocation(source, 4), target);

        Assert.Equal(
            new Relation(
                new CarrierLocation(target, -3),
                new CarrierLocation(source, 0)),
            projected);
    }

    [Fact]
    public void ReversedSegmentProjectionPreservesOrderedInspectionFrame()
    {
        var source = new Carrier();
        var unit = new Carrier();
        var read = new Relation(Segment(source, 10, 0), Point(unit, 1));

        var projected = read.Project(new CarrierLocation(source, 23), unit);

        Assert.Equal(
            new Relation(
                new CarrierLocation(unit, -1),
                new CarrierLocation(source, 3)),
            projected);
    }

    [Fact]
    public void ExplicitBoundaryRelationRoutesPersistedPositiveTensionWhenAddedLater()
    {
        var residue = new Carrier();
        var whole = new Carrier();
        var boundary = residue.Attach(new Relation(Point(residue, 10), Point(residue, 0)));
        residue.Add(23);

        whole.Attach(new Relation(boundary, Point(whole, 0)));

        Assert.Equal(3UL, residue.Value);
        Assert.Equal(0, residue.Tension);
        Assert.Equal(2UL, whole.Value);
    }

    [Fact]
    public void PointRelationsSettleThroughBaseTenCarryChain()
    {
        var ones = new Carrier();
        var tens = new Carrier();
        var hundreds = new Carrier();
        ones.Attach(new Relation(Point(ones, 10), Point(tens, 0)));
        tens.Attach(new Relation(Point(tens, 10), Point(hundreds, 0)));

        ones.Add(375);

        Assert.Equal(5UL, ones.Value);
        Assert.Equal(7UL, tens.Value);
        Assert.Equal(3UL, hundreds.Value);
    }

    [Fact]
    public void NegativeTensionStaysOnCarrierWhenUnderflowIsUnrouted()
    {
        var source = new Carrier();

        source.Remove(23);

        Assert.Equal(-23, source.Tension);
    }

    private static Relation Point(Carrier carrier, long ticks) =>
        new(new CarrierLocation(carrier, ticks), carrier);

    private static Relation Segment(Carrier carrier, long left, long right) =>
        new(Point(carrier, left), Point(carrier, right));
}
