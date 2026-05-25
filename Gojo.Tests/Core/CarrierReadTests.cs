using Gojo.Core;
using Gojo.Diagnostics;
using Gojo.Interpretation;
using Gojo.Reads;

namespace Gojo.Tests.Core;

public sealed class CarrierReadTests
{
    [Fact]
    public void CarrierKeepsPhysicalStateOnItsOwnInstance()
    {
        var carrier = new Carrier();

        carrier.Add(7);
        carrier.Remove(10);

        Assert.Equal(0UL, carrier.Value);
        Assert.Equal(-3, carrier.Tension);
    }

    [Fact]
    public void CarrierIdsAreAutomaticAndUnknownIsZero()
    {
        var first = new Carrier();
        var second = new Carrier();

        Assert.Equal(0UL, Carrier.Unknown.Id);
        Assert.Same(Carrier.Unknown, Carrier.Unbound);
        Assert.NotEqual(first.Id, second.Id);
        Assert.False(first.SameIdentityAs(second));
    }

    [Fact]
    public void SameCarrierReadSeesPositiveTicks()
    {
        var x = new Carrier();
        var location = new CarrierLocation(x, 5);
        var read = location.ReadFrom(x);

        Assert.Equal(5, location.Ticks);
        Assert.True(read.IsVisible);
        Assert.Equal(1, read.Direction);
        Assert.Equal(5, read.ProjectedTicks);
    }

    [Fact]
    public void SameCarrierNegativeLocationReadsAsNegativeTicks()
    {
        var x = new Carrier();
        var location = new CarrierLocation(x, -5);
        var read = location.ReadFrom(x);

        Assert.Equal(-5, location.Ticks);
        Assert.True(read.IsVisible);
        Assert.Equal(-1, read.Direction);
        Assert.Equal(5UL, read.Magnitude);
        Assert.Equal(-5, read.ProjectedTicks);
    }

    [Fact]
    public void OrthogonalCarrierReadReturnsZero()
    {
        var x = new Carrier();
        var y = new Carrier();
        var location = new CarrierLocation(y, 5);
        var read = location.ReadFrom(x);

        Assert.Equal(0, read.Direction);
        Assert.False(read.IsVisible);
        Assert.Equal(0, read.ProjectedTicks);
    }

    [Fact]
    public void InverseCarrierReadIsOrthogonalToScalarCarrierRead()
    {
        var forward = new Carrier();
        var inverse = forward.Inverse();
        var location = new CarrierLocation(inverse, 5);

        var readFromForward = location.ReadFrom(forward);
        var readFromInverse = location.ReadFrom(inverse);

        Assert.Equal(5, location.Ticks);
        Assert.Equal(0, readFromForward.Direction);
        Assert.False(readFromForward.IsVisible);
        Assert.Equal(0, readFromForward.ProjectedTicks);
        Assert.Equal(1, readFromInverse.Direction);
        Assert.Equal(5, readFromInverse.ProjectedTicks);
    }

    [Fact]
    public void CarrierLocationCarriesTransformWhenReadFromTransformSlot()
    {
        var transformReader = Carrier.Unknown;
        var expansive = new CarrierLocation(transformReader, 3);
        var contractive = new CarrierLocation(transformReader, -10);
        var additive = new CarrierLocation(transformReader, 0);

        Assert.Equal(1, expansive.ReadFrom(transformReader).Direction);
        Assert.Equal(3, expansive.Ticks);
        Assert.Equal(-1, contractive.ReadFrom(transformReader).Direction);
        Assert.Equal(-10, contractive.Ticks);
        Assert.False(additive.ReadFrom(transformReader).IsVisible);
        Assert.Equal(0, additive.Ticks);
    }

    [Fact]
    public void BoundaryRefillDrawBeyondCounterLeavesOpposedRemainderVisible()
    {
        var counter = new Carrier();
        var counterState = new Relation(
            new CarrierLocation(counter, 2),
            new CarrierLocation(counter, -5));

        Assert.Equal(
            new Relation(
                new CarrierLocation(counter, -2),
                new CarrierLocation(counter, 2)),
            counterState.TensionOn(counter));
        Assert.Equal(
            new Relation(
                new CarrierLocation(counter, -3),
                new CarrierLocation(counter, 0)),
            counterState.ExclusiveOn(counter));
    }

    [Fact]
    public void BoundarySideAndTravelCanBePlainCarrierLocations()
    {
        var side = new Carrier();
        var travel = new Carrier();

        var positiveOverflow = Alignment(new CarrierLocation(side, 1), new CarrierLocation(travel, 1));
        var positiveRefill = Alignment(new CarrierLocation(side, 1), new CarrierLocation(travel, -1));
        var negativeRefill = Alignment(new CarrierLocation(side, -1), new CarrierLocation(travel, 1));
        var negativeOverflow = Alignment(new CarrierLocation(side, -1), new CarrierLocation(travel, -1));

        Assert.Equal(1, positiveOverflow);
        Assert.Equal(-1, positiveRefill);
        Assert.Equal(-1, negativeRefill);
        Assert.Equal(1, negativeOverflow);

        long Alignment(CarrierLocation sideLocation, CarrierLocation travelLocation) =>
            Math.Sign(sideLocation.ReadFrom(side).Direction * travelLocation.ReadFrom(travel).Direction);
    }

    [Fact]
    public void NewTensionAppearsOnlyPastAccountedCarrierRegion()
    {
        var carrier = new Carrier();
        var accounted = new CarrierLocation(carrier, 20);

        var inside = new CarrierLocation(carrier, 11).NewTensionAfter(accounted, carrier);
        var boundary = new CarrierLocation(carrier, 20).NewTensionAfter(accounted, carrier);
        var beyond = new CarrierLocation(carrier, 21).NewTensionAfter(accounted, carrier);

        Assert.Equal(new CarrierLocation(carrier, 0), inside);
        Assert.Equal(new CarrierLocation(carrier, 0), boundary);
        Assert.Equal(new CarrierLocation(carrier, 1), beyond);
    }

    [Fact]
    public void UnaccountedVisibleParticipationIsAllNewTension()
    {
        var carrier = new Carrier();
        var incoming = new CarrierLocation(carrier, 9);

        var read = incoming.AccountedBy(carrier);

        Assert.Equal(
            new Relation(
                new CarrierLocation(carrier, 0),
                incoming),
            read);
    }

    [Fact]
    public void ZeroAccountingSurfaceDoesNotConsumeVisibleTension()
    {
        var carrier = new Carrier();
        var incoming = new CarrierLocation(carrier, 9);

        var read = incoming.AccountedBy(carrier, new CarrierLocation(carrier, 0));

        Assert.Equal(
            new Relation(
                new CarrierLocation(carrier, 0),
                incoming),
            read);
    }

    [Fact]
    public void MultipleCompatibleAccountingSurfacesAccumulateBeforeNewTensionAppears()
    {
        var carrier = new Carrier();
        var incoming = new CarrierLocation(carrier, 15);

        var partial = incoming.NewTensionAfter(
            carrier,
            new CarrierLocation(carrier, 7),
            new CarrierLocation(carrier, 6));

        var covered = incoming.NewTensionAfter(
            carrier,
            new CarrierLocation(carrier, 7),
            new CarrierLocation(carrier, 8));

        Assert.Equal(new CarrierLocation(carrier, 2), partial);
        Assert.Equal(new CarrierLocation(carrier, 0), covered);
    }

    [Fact]
    public void AccountingReadReturnsAccountedParticipationAndNewTension()
    {
        var carrier = new Carrier();
        var incoming = new CarrierLocation(carrier, 15);

        var read = incoming.AccountedBy(
            carrier,
            new CarrierLocation(carrier, 7),
            new CarrierLocation(carrier, 6));

        Assert.Equal(
            new Relation(
                new CarrierLocation(carrier, 13),
                new CarrierLocation(carrier, 2)),
            read);
    }

    [Fact]
    public void ClampReadIsAccountingWithoutContinuationRoute()
    {
        var carrier = new Carrier();
        var incoming = new CarrierLocation(carrier, 12);
        var clampFace = new CarrierLocation(carrier, 5);

        var clamp = incoming.AccountedBy(carrier, clampFace);

        Assert.Equal(
            new Relation(
                new CarrierLocation(carrier, 5),
                new CarrierLocation(carrier, 7)),
            clamp);
    }

    [Fact]
    public void ClampReadCanUseSeveralCompatibleFacesBeforeTensionAppears()
    {
        var carrier = new Carrier();
        var incoming = new CarrierLocation(carrier, 12);
        var firstFace = new CarrierLocation(carrier, 5);
        var secondFace = new CarrierLocation(carrier, 4);

        var clamp = incoming.AccountedBy(carrier, firstFace, secondFace);

        Assert.Equal(
            new Relation(
                new CarrierLocation(carrier, 9),
                new CarrierLocation(carrier, 3)),
            clamp);
    }

    [Fact]
    public void AccountingReadReturnsNoParticipationWhenReaderCannotSeeIncomingCarrier()
    {
        var x = new Carrier();
        var y = new Carrier();
        var incoming = new CarrierLocation(y, 15);

        var read = incoming.AccountedBy(x, new CarrierLocation(y, 15));

        Assert.Equal(
            new Relation(
                new CarrierLocation(y, 0),
                new CarrierLocation(y, 0)),
            read);
    }

    [Fact]
    public void RelationCanActAsTwoAccountingSurfacesInReaderContext()
    {
        var carrier = new Carrier();
        var incoming = new CarrierLocation(carrier, 15);
        var routeCapacity = new Relation(
            new CarrierLocation(carrier, 7),
            new CarrierLocation(carrier, 6));

        var tension = incoming.NewTensionAfter(carrier, routeCapacity);
        var read = incoming.AccountedBy(carrier, routeCapacity);

        Assert.Equal(new CarrierLocation(carrier, 2), tension);
        Assert.Equal(
            new Relation(
                new CarrierLocation(carrier, 13),
                new CarrierLocation(carrier, 2)),
            read);
    }

    [Fact]
    public void MultipleRelationsCanActAsAccountingSurfacesInReaderContext()
    {
        var carrier = new Carrier();
        var incoming = new CarrierLocation(carrier, 15);
        var first = new Relation(
            new CarrierLocation(carrier, 4),
            new CarrierLocation(carrier, 3));
        var second = new Relation(
            new CarrierLocation(carrier, 2),
            new CarrierLocation(carrier, 5));

        var tension = incoming.NewTensionAfter(carrier, first, second);

        Assert.Equal(new CarrierLocation(carrier, 1), tension);
    }

    [Fact]
    public void RelationAccountingStillIgnoresIncompatibleSlots()
    {
        var x = new Carrier();
        var y = new Carrier();
        var incoming = new CarrierLocation(x, 15);
        var capacity = new Relation(
            new CarrierLocation(x, 4),
            new CarrierLocation(y, 100));

        var read = incoming.AccountedBy(x, capacity);

        Assert.Equal(
            new Relation(
                new CarrierLocation(x, 4),
                new CarrierLocation(x, 11)),
            read);
    }

    [Fact]
    public void RelationCarrierLocationsTraverseNestedRelationSurface()
    {
        var x = new Carrier();
        var y = new Carrier();
        var relation = new Relation(
            new Relation(
                new CarrierLocation(x, 1),
                new CarrierLocation(x, 2)),
            new Relation(
                new CarrierLocation(y, 3),
                new CarrierLocation(y, 4)));

        Assert.Equal(
            new[]
            {
                new CarrierLocation(x, 1),
                new CarrierLocation(x, 2),
                new CarrierLocation(y, 3),
                new CarrierLocation(y, 4)
            },
            relation.CarrierLocations());
        Assert.Equal(
            new[]
            {
                new CarrierLocation(x, 1),
                new CarrierLocation(x, 2)
            },
            relation.LeftCarrierLocations());
        Assert.Equal(
            new[]
            {
                new CarrierLocation(y, 3),
                new CarrierLocation(y, 4)
            },
            relation.RightCarrierLocations());
    }

    [Fact]
    public void RelationCanBeReadAsOrderedBoundariesInCarrierContext()
    {
        var carrier = new Carrier();
        var segment = new Relation(
            new CarrierLocation(carrier, 8),
            new CarrierLocation(carrier, 2));

        var boundaries = segment.BoundariesOn(carrier);
        var span = segment.SpanOn(carrier);

        Assert.Equal(
            new Relation(
                new CarrierLocation(carrier, 2),
                new CarrierLocation(carrier, 8)),
            boundaries);
        Assert.Equal(new CarrierLocation(carrier, 6), span);
    }

    [Fact]
    public void RelationSegmentConstructorCanBeReadAsBoundariesAndFlow()
    {
        var carrier = new Carrier();
        var segment = Relation.Segment(carrier, 6, 48);

        var boundaries = segment.BoundariesOn(carrier);
        var span = segment.SpanOn(carrier);
        var flow = segment.FlowOn(carrier);

        Assert.Equal(
            new Relation(
                new CarrierLocation(carrier, 6),
                new CarrierLocation(carrier, 48)),
            boundaries);
        Assert.Equal(new CarrierLocation(carrier, 42), span);
        Assert.Equal(new CarrierLocation(carrier, 42), flow);
    }

    [Fact]
    public void RelationBoundaryReadReturnsZeroWhenReaderCannotSeeBothEndpoints()
    {
        var x = new Carrier();
        var y = new Carrier();
        var segment = new Relation(
            new CarrierLocation(x, 2),
            new CarrierLocation(y, 8));

        Assert.Equal(
            new Relation(
                new CarrierLocation(x, 0),
                new CarrierLocation(x, 0)),
            segment.BoundariesOn(x));
        Assert.Equal(new CarrierLocation(x, 0), segment.SpanOn(x));
    }

    [Fact]
    public void RelationBoundaryReadDoesNotMergeIndependentCarrier()
    {
        var source = new Carrier();
        var independent = new Carrier();
        var segment = new Relation(
            new CarrierLocation(source, 2),
            new CarrierLocation(independent, 8));

        var boundaries = segment.BoundariesOn(independent);

        Assert.Equal(
            new Relation(
                new CarrierLocation(independent, 0),
                new CarrierLocation(independent, 0)),
            boundaries);
    }

    [Fact]
    public void RelationBoundaryReadReturnsZeroForInverseCarrierEndpoints()
    {
        var forward = new Carrier();
        var inverse = forward.Inverse();
        var segment = new Relation(
            new CarrierLocation(inverse, 8),
            new CarrierLocation(inverse, 2));

        var boundaries = segment.BoundariesOn(forward);
        var span = segment.SpanOn(forward);

        Assert.Equal(
            new Relation(
                new CarrierLocation(forward, 0),
                new CarrierLocation(forward, 0)),
            boundaries);
        Assert.Equal(new CarrierLocation(forward, 0), span);
    }

    [Fact]
    public void RelationFlowReadPreservesTravelDirectionAsCarrierParticipation()
    {
        var carrier = new Carrier();

        var forward = new Relation(
            new CarrierLocation(carrier, 2),
            new CarrierLocation(carrier, 8));
        var reverse = new Relation(
            new CarrierLocation(carrier, 8),
            new CarrierLocation(carrier, 2));

        Assert.Equal(new CarrierLocation(carrier, 6), forward.FlowOn(carrier));
        Assert.Equal(new CarrierLocation(carrier, -6), reverse.FlowOn(carrier));
    }

    [Fact]
    public void RelationBoundaryAndFlowReadsSeeSignedLocationsThroughOrigin()
    {
        var carrier = new Carrier();
        var reverseFromOrigin = new Relation(
            new CarrierLocation(carrier, 0),
            new CarrierLocation(carrier, -8));

        Assert.Equal(
            new Relation(
                new CarrierLocation(carrier, -8),
                new CarrierLocation(carrier, 0)),
            reverseFromOrigin.BoundariesOn(carrier));
        Assert.Equal(new CarrierLocation(carrier, 8), reverseFromOrigin.SpanOn(carrier));
        Assert.Equal(new CarrierLocation(carrier, -8), reverseFromOrigin.FlowOn(carrier));
    }

    [Fact]
    public void RelationFlowReadReturnsZeroForInverseCarrierEndpoints()
    {
        var forward = new Carrier();
        var inverse = forward.Inverse();
        var segment = new Relation(
            new CarrierLocation(inverse, 2),
            new CarrierLocation(inverse, 8));

        Assert.Equal(new CarrierLocation(forward, 0), segment.FlowOn(forward));
    }

    [Fact]
    public void RelationBoundaryPointsSplitWhereCarrierParticipationCanChange()
    {
        var carrier = new Carrier();
        var outer = new Relation(
            new CarrierLocation(carrier, 0),
            new CarrierLocation(carrier, 8));
        var inner = new Relation(
            new CarrierLocation(carrier, 2),
            new CarrierLocation(carrier, 5));

        var points = RelationReads.BoundaryPointsOn(carrier, outer, inner);

        Assert.Equal(
            new[]
            {
                new CarrierLocation(carrier, 0),
                new CarrierLocation(carrier, 2),
                new CarrierLocation(carrier, 5),
                new CarrierLocation(carrier, 8)
            },
            points);
    }

    [Fact]
    public void RelationBoundaryPointsIgnoreInvisibleAndZeroSpans()
    {
        var x = new Carrier();
        var y = new Carrier();
        var visible = new Relation(
            new CarrierLocation(x, 2),
            new CarrierLocation(x, 5));
        var invisible = new Relation(
            new CarrierLocation(x, 7),
            new CarrierLocation(y, 10));
        var zero = new Relation(
            new CarrierLocation(x, 3),
            new CarrierLocation(x, 3));

        var points = RelationReads.BoundaryPointsOn(x, visible, invisible, zero);

        Assert.Equal(
            new[]
            {
                new CarrierLocation(x, 2),
                new CarrierLocation(x, 5)
            },
            points);
    }

    [Fact]
    public void RelationBoundaryPointsIgnoreInverseCarrierSurfaceFromScalarReader()
    {
        var forward = new Carrier();
        var inverse = forward.Inverse();
        var segment = new Relation(
            new CarrierLocation(inverse, 0),
            new CarrierLocation(inverse, 8));

        var points = RelationReads.BoundaryPointsOn(forward, segment);

        Assert.Empty(points);
    }

    [Fact]
    public void RelationTraversedBoundaryPointsReturnSitesInTravelOrder()
    {
        var carrier = new Carrier();
        var travel = new Relation(
            new CarrierLocation(carrier, 0),
            new CarrierLocation(carrier, 10));
        var first = new Relation(
            new CarrierLocation(carrier, 2),
            new CarrierLocation(carrier, 4));
        var second = new Relation(
            new CarrierLocation(carrier, 7),
            new CarrierLocation(carrier, 9));

        var points = RelationReads.TraversedBoundaryPointsOn(carrier, travel, first, second);

        Assert.Equal(
            new[]
            {
                new CarrierLocation(carrier, 2),
                new CarrierLocation(carrier, 4),
                new CarrierLocation(carrier, 7),
                new CarrierLocation(carrier, 9)
            },
            points);
    }

    [Fact]
    public void RelationTraversedBoundaryPointsExcludeTravelStartAndIncludeTravelEnd()
    {
        var carrier = new Carrier();
        var travel = new Relation(
            new CarrierLocation(carrier, 2),
            new CarrierLocation(carrier, 8));
        var boundaries = new Relation(
            new CarrierLocation(carrier, 2),
            new CarrierLocation(carrier, 8));

        var points = RelationReads.TraversedBoundaryPointsOn(carrier, travel, boundaries);

        Assert.Equal(
            new[]
            {
                new CarrierLocation(carrier, 8)
            },
            points);
    }

    [Fact]
    public void RelationTraversedBoundaryPointsFollowReverseTravelOrder()
    {
        var carrier = new Carrier();
        var travel = new Relation(
            new CarrierLocation(carrier, 10),
            new CarrierLocation(carrier, 0));
        var first = new Relation(
            new CarrierLocation(carrier, 2),
            new CarrierLocation(carrier, 4));
        var second = new Relation(
            new CarrierLocation(carrier, 7),
            new CarrierLocation(carrier, 9));

        var points = RelationReads.TraversedBoundaryPointsOn(carrier, travel, first, second);

        Assert.Equal(
            new[]
            {
                new CarrierLocation(carrier, 9),
                new CarrierLocation(carrier, 7),
                new CarrierLocation(carrier, 4),
                new CarrierLocation(carrier, 2)
            },
            points);
    }

    [Fact]
    public void RelationTraversedBoundaryPointsIgnoreInverseCarrierSurfaceFromScalarReader()
    {
        var forward = new Carrier();
        var inverse = forward.Inverse();
        var travel = new Relation(
            new CarrierLocation(inverse, 0),
            new CarrierLocation(inverse, 10));
        var boundary = new Relation(
            new CarrierLocation(inverse, 2),
            new CarrierLocation(inverse, 4));

        var points = RelationReads.TraversedBoundaryPointsOn(forward, travel, boundary);

        Assert.Empty(points);
    }

    [Fact]
    public void RelationTraversedBoundaryPointsIgnoreInvisibleSites()
    {
        var x = new Carrier();
        var y = new Carrier();
        var travel = new Relation(
            new CarrierLocation(x, 0),
            new CarrierLocation(x, 10));
        var visible = new Relation(
            new CarrierLocation(x, 2),
            new CarrierLocation(x, 4));
        var invisible = new Relation(
            new CarrierLocation(y, 6),
            new CarrierLocation(y, 8));

        var points = RelationReads.TraversedBoundaryPointsOn(x, travel, visible, invisible);

        Assert.Equal(
            new[]
            {
                new CarrierLocation(x, 2),
                new CarrierLocation(x, 4)
            },
            points);
    }

    [Fact]
    public void RelationTraversalSitesExposeStartAsOccupiedAndEndAsNewFlow()
    {
        var carrier = new Carrier();
        var travel = new Relation(
            new CarrierLocation(carrier, 2),
            new CarrierLocation(carrier, 8));

        var sites = travel.TraversalSitesOn(carrier);

        Assert.Equal(
            new Relation(
                new Relation(
                    new CarrierLocation(carrier, 2),
                    new CarrierLocation(carrier, 0)),
                new Relation(
                    new CarrierLocation(carrier, 8),
                    new CarrierLocation(carrier, 6))),
            sites);
    }

    [Fact]
    public void RelationTraversalSitesExposeReverseFlowAsNegativeLocation()
    {
        var carrier = new Carrier();
        var travel = new Relation(
            new CarrierLocation(carrier, 8),
            new CarrierLocation(carrier, 2));

        var sites = travel.TraversalSitesOn(carrier);

        Assert.Equal(
            new Relation(
                new Relation(
                    new CarrierLocation(carrier, 8),
                    new CarrierLocation(carrier, 0)),
                new Relation(
                    new CarrierLocation(carrier, 2),
                    new CarrierLocation(carrier, -6))),
            sites);
    }

    [Fact]
    public void RelationTraversedBoundaryStatesPairEachSiteWithTravelFlow()
    {
        var carrier = new Carrier();
        var travel = new Relation(
            new CarrierLocation(carrier, 0),
            new CarrierLocation(carrier, 10));
        var boundary = new Relation(
            new CarrierLocation(carrier, 2),
            new CarrierLocation(carrier, 4));

        var states = RelationReads.TraversedBoundaryStatesOn(carrier, travel, boundary);

        Assert.Equal(
            new[]
            {
                new Relation(
                    new CarrierLocation(carrier, 2),
                    new CarrierLocation(carrier, 10)),
                new Relation(
                    new CarrierLocation(carrier, 4),
                    new CarrierLocation(carrier, 10))
            },
            states);
    }

    [Fact]
    public void RelationTraversedBoundaryStatesCarryReverseFlow()
    {
        var carrier = new Carrier();
        var travel = new Relation(
            new CarrierLocation(carrier, 10),
            new CarrierLocation(carrier, 0));
        var boundary = new Relation(
            new CarrierLocation(carrier, 2),
            new CarrierLocation(carrier, 4));

        var states = RelationReads.TraversedBoundaryStatesOn(carrier, travel, boundary);

        Assert.Equal(
            new[]
            {
                new Relation(
                    new CarrierLocation(carrier, 4),
                    new CarrierLocation(carrier, -10)),
                new Relation(
                    new CarrierLocation(carrier, 2),
                    new CarrierLocation(carrier, -10))
            },
            states);
    }

    [Fact]
    public void RelationTraversedSpansSplitTravelAtBoundaryPointsInTravelOrder()
    {
        var carrier = new Carrier();
        var travel = new Relation(
            new CarrierLocation(carrier, 0),
            new CarrierLocation(carrier, 10));
        var first = new Relation(
            new CarrierLocation(carrier, 2),
            new CarrierLocation(carrier, 4));
        var second = new Relation(
            new CarrierLocation(carrier, 7),
            new CarrierLocation(carrier, 9));

        var spans = RelationReads.TraversedSpansOn(carrier, travel, first, second);

        Assert.Equal(
            new[]
            {
                new Relation(new CarrierLocation(carrier, 0), new CarrierLocation(carrier, 2)),
                new Relation(new CarrierLocation(carrier, 2), new CarrierLocation(carrier, 4)),
                new Relation(new CarrierLocation(carrier, 4), new CarrierLocation(carrier, 7)),
                new Relation(new CarrierLocation(carrier, 7), new CarrierLocation(carrier, 9)),
                new Relation(new CarrierLocation(carrier, 9), new CarrierLocation(carrier, 10))
            },
            spans);
    }

    [Fact]
    public void RelationTraversedSpansFollowReverseTravelOrder()
    {
        var carrier = new Carrier();
        var travel = new Relation(
            new CarrierLocation(carrier, 10),
            new CarrierLocation(carrier, 0));
        var first = new Relation(
            new CarrierLocation(carrier, 2),
            new CarrierLocation(carrier, 4));
        var second = new Relation(
            new CarrierLocation(carrier, 7),
            new CarrierLocation(carrier, 9));

        var spans = RelationReads.TraversedSpansOn(carrier, travel, first, second);

        Assert.Equal(
            new[]
            {
                new Relation(new CarrierLocation(carrier, 10), new CarrierLocation(carrier, 9)),
                new Relation(new CarrierLocation(carrier, 9), new CarrierLocation(carrier, 7)),
                new Relation(new CarrierLocation(carrier, 7), new CarrierLocation(carrier, 4)),
                new Relation(new CarrierLocation(carrier, 4), new CarrierLocation(carrier, 2)),
                new Relation(new CarrierLocation(carrier, 2), new CarrierLocation(carrier, 0))
            },
            spans);
    }

    [Fact]
    public void RelationTraversedSpansReturnWholeTravelWhenNoBoundaryIsCrossed()
    {
        var carrier = new Carrier();
        var travel = new Relation(
            new CarrierLocation(carrier, 0),
            new CarrierLocation(carrier, 10));

        var spans = RelationReads.TraversedSpansOn(carrier, travel);

        Assert.Equal(
            new[]
            {
                travel
            },
            spans);
    }

    [Fact]
    public void RelationTraversedSpansIgnoreInverseCarrierSurfaceFromScalarReader()
    {
        var forward = new Carrier();
        var inverse = forward.Inverse();
        var travel = new Relation(
            new CarrierLocation(inverse, 0),
            new CarrierLocation(inverse, 10));
        var boundary = new Relation(
            new CarrierLocation(inverse, 2),
            new CarrierLocation(inverse, 4));

        var spans = RelationReads.TraversedSpansOn(forward, travel, boundary);

        Assert.Empty(spans);
    }

    [Fact]
    public void RelationTraversedSpanStatesPairEachSpanWithLocalFlow()
    {
        var carrier = new Carrier();
        var travel = new Relation(
            new CarrierLocation(carrier, 0),
            new CarrierLocation(carrier, 10));
        var boundary = new Relation(
            new CarrierLocation(carrier, 2),
            new CarrierLocation(carrier, 4));

        var states = RelationReads.TraversedSpanStatesOn(carrier, travel, boundary);

        Assert.Equal(
            new[]
            {
                new Relation(
                    new Relation(new CarrierLocation(carrier, 0), new CarrierLocation(carrier, 2)),
                    new CarrierLocation(carrier, 2)),
                new Relation(
                    new Relation(new CarrierLocation(carrier, 2), new CarrierLocation(carrier, 4)),
                    new CarrierLocation(carrier, 2)),
                new Relation(
                    new Relation(new CarrierLocation(carrier, 4), new CarrierLocation(carrier, 10)),
                    new CarrierLocation(carrier, 6))
            },
            states);
    }

    [Fact]
    public void RelationTraversedSpanStatesKeepReverseSitesAndFlowOnSignedCarrier()
    {
        var carrier = new Carrier();
        var travel = new Relation(
            new CarrierLocation(carrier, 10),
            new CarrierLocation(carrier, 0));
        var boundary = new Relation(
            new CarrierLocation(carrier, 2),
            new CarrierLocation(carrier, 4));

        var states = RelationReads.TraversedSpanStatesOn(carrier, travel, boundary);

        Assert.Equal(
            new[]
            {
                new Relation(
                    new Relation(new CarrierLocation(carrier, 10), new CarrierLocation(carrier, 4)),
                    new CarrierLocation(carrier, -6)),
                new Relation(
                    new Relation(new CarrierLocation(carrier, 4), new CarrierLocation(carrier, 2)),
                    new CarrierLocation(carrier, -2)),
                new Relation(
                    new Relation(new CarrierLocation(carrier, 2), new CarrierLocation(carrier, 0)),
                    new CarrierLocation(carrier, -2))
            },
            states);
    }

    [Fact]
    public void RelationTraversedSupportStatesPreserveUnsupportedPathSpans()
    {
        var carrier = new Carrier();
        var travel = new Relation(
            new CarrierLocation(carrier, 0),
            new CarrierLocation(carrier, 10));
        var support = new Relation(
            new CarrierLocation(carrier, 2),
            new CarrierLocation(carrier, 5));

        var states = RelationReads.TraversedSupportStatesOn(carrier, travel, support);

        Assert.Equal(
            new[]
            {
                new Relation(
                    new Relation(new CarrierLocation(carrier, 0), new CarrierLocation(carrier, 2)),
                    new Relation(new CarrierLocation(carrier, 0), new CarrierLocation(carrier, 0))),
                new Relation(
                    new Relation(new CarrierLocation(carrier, 2), new CarrierLocation(carrier, 5)),
                    new Relation(new CarrierLocation(carrier, 0), new CarrierLocation(carrier, 1))),
                new Relation(
                    new Relation(new CarrierLocation(carrier, 5), new CarrierLocation(carrier, 10)),
                    new Relation(new CarrierLocation(carrier, 0), new CarrierLocation(carrier, 0)))
            },
            states);
    }

    [Fact]
    public void RelationTraversedSupportProjectionReturnsOnlySupportedPathSpans()
    {
        var carrier = new Carrier();
        var travel = new Relation(
            new CarrierLocation(carrier, 0),
            new CarrierLocation(carrier, 10));
        var support = new Relation(
            new CarrierLocation(carrier, 2),
            new CarrierLocation(carrier, 5));

        var passed = RelationReads.TraversedSupportOn(carrier, travel, support);
        var blocked = RelationReads.TraversedBlockOn(carrier, travel, support);

        Assert.Equal(
            new[]
            {
                new Relation(
                    new Relation(new CarrierLocation(carrier, 2), new CarrierLocation(carrier, 5)),
                    new Relation(new CarrierLocation(carrier, 0), new CarrierLocation(carrier, 1)))
            },
            passed);
        Assert.Equal(
            new[]
            {
                new Relation(new CarrierLocation(carrier, 0), new CarrierLocation(carrier, 2)),
                new Relation(new CarrierLocation(carrier, 5), new CarrierLocation(carrier, 10))
            },
            blocked);
    }

    [Fact]
    public void TraversalReadGroupsTheNaturalTraversalSequence()
    {
        var carrier = new Carrier();
        var travel = new Relation(
            new CarrierLocation(carrier, 0),
            new CarrierLocation(carrier, 10));
        var support = new Relation(
            new CarrierLocation(carrier, 2),
            new CarrierLocation(carrier, 5));

        var read = TraversalDiagnostics.Traverse(carrier, travel, support);

        Assert.Equal(
            new[]
            {
                new CarrierLocation(carrier, 2),
                new CarrierLocation(carrier, 5)
            },
            read.BoundaryPoints);
        Assert.Equal(
            new[]
            {
                new Relation(new CarrierLocation(carrier, 0), new CarrierLocation(carrier, 2)),
                new Relation(new CarrierLocation(carrier, 2), new CarrierLocation(carrier, 5)),
                new Relation(new CarrierLocation(carrier, 5), new CarrierLocation(carrier, 10))
            },
            read.Spans);
        Assert.Equal(
            new[]
            {
                new Relation(
                    new Relation(new CarrierLocation(carrier, 2), new CarrierLocation(carrier, 5)),
                    new Relation(new CarrierLocation(carrier, 0), new CarrierLocation(carrier, 1)))
            },
            read.Pass);
        Assert.Equal(
            new[]
            {
                new Relation(new CarrierLocation(carrier, 0), new CarrierLocation(carrier, 2)),
                new Relation(new CarrierLocation(carrier, 5), new CarrierLocation(carrier, 10))
            },
            read.Block);
    }

    [Fact]
    public void TraversalReadMirrorsIndividualTraversalReads()
    {
        var carrier = new Carrier();
        var travel = new Relation(
            new CarrierLocation(carrier, 0),
            new CarrierLocation(carrier, 10));
        var forward = new Relation(
            new CarrierLocation(carrier, 2),
            new CarrierLocation(carrier, 8));
        var reverse = new Relation(
            new CarrierLocation(carrier, 6),
            new CarrierLocation(carrier, 4));

        var read = TraversalDiagnostics.Traverse(carrier, travel, forward, reverse);

        Assert.Equal(RelationReads.TraversedBoundaryPointsOn(carrier, travel, forward, reverse), read.BoundaryPoints);
        Assert.Equal(RelationReads.TraversedBoundaryStatesOn(carrier, travel, forward, reverse), read.BoundaryStates);
        Assert.Equal(RelationReads.TraversedSpansOn(carrier, travel, forward, reverse), read.Spans);
        Assert.Equal(RelationReads.TraversedSpanStatesOn(carrier, travel, forward, reverse), read.SpanStates);
        Assert.Equal(RelationReads.TraversedSupportStatesOn(carrier, travel, forward, reverse), read.SupportStates);
        Assert.Equal(RelationReads.TraversedSupportOn(carrier, travel, forward, reverse), read.Support);
        Assert.Equal(RelationReads.TraversedTensionOn(carrier, travel, forward, reverse), read.Tension);
        Assert.Equal(RelationReads.TraversedExclusiveOn(carrier, travel, forward, reverse), read.Exclusive);
        Assert.Equal(RelationReads.TraversedPassOn(carrier, travel, forward, reverse), read.Pass);
        Assert.Equal(RelationReads.TraversedBlockOn(carrier, travel, forward, reverse), read.Block);
    }

    [Fact]
    public void RelationTraversedSupportStatesReadReversePathSpansByCoveredInterval()
    {
        var carrier = new Carrier();
        var travel = new Relation(
            new CarrierLocation(carrier, 10),
            new CarrierLocation(carrier, 0));
        var support = new Relation(
            new CarrierLocation(carrier, 2),
            new CarrierLocation(carrier, 5));

        var states = RelationReads.TraversedSupportStatesOn(carrier, travel, support);

        Assert.Equal(
            new[]
            {
                new Relation(
                    new Relation(new CarrierLocation(carrier, 10), new CarrierLocation(carrier, 5)),
                    new Relation(new CarrierLocation(carrier, 0), new CarrierLocation(carrier, 0))),
                new Relation(
                    new Relation(new CarrierLocation(carrier, 5), new CarrierLocation(carrier, 2)),
                    new Relation(new CarrierLocation(carrier, 0), new CarrierLocation(carrier, 1))),
                new Relation(
                    new Relation(new CarrierLocation(carrier, 2), new CarrierLocation(carrier, 0)),
                    new Relation(new CarrierLocation(carrier, 0), new CarrierLocation(carrier, 0)))
            },
            states);
    }

    [Fact]
    public void RelationTraversedTensionAndExclusiveReadPathOpposition()
    {
        var carrier = new Carrier();
        var travel = new Relation(
            new CarrierLocation(carrier, 0),
            new CarrierLocation(carrier, 10));
        var forward = new Relation(
            new CarrierLocation(carrier, 2),
            new CarrierLocation(carrier, 8));
        var reverse = new Relation(
            new CarrierLocation(carrier, 6),
            new CarrierLocation(carrier, 4));

        var tension = RelationReads.TraversedTensionOn(carrier, travel, forward, reverse);
        var exclusive = RelationReads.TraversedExclusiveOn(carrier, travel, forward, reverse);

        Assert.Equal(
            new[]
            {
                new Relation(
                    new Relation(new CarrierLocation(carrier, 4), new CarrierLocation(carrier, 6)),
                    new Relation(new CarrierLocation(carrier, -1), new CarrierLocation(carrier, 1)))
            },
            tension);
        Assert.Equal(
            new[]
            {
                new Relation(
                    new Relation(new CarrierLocation(carrier, 2), new CarrierLocation(carrier, 4)),
                    new Relation(new CarrierLocation(carrier, 0), new CarrierLocation(carrier, 1))),
                new Relation(
                    new Relation(new CarrierLocation(carrier, 6), new CarrierLocation(carrier, 8)),
                    new Relation(new CarrierLocation(carrier, 0), new CarrierLocation(carrier, 1)))
            },
            exclusive);
    }

    [Fact]
    public void RelationCoveredSpansSplitWhereParticipationCanChange()
    {
        var carrier = new Carrier();
        var outer = new Relation(
            new CarrierLocation(carrier, 0),
            new CarrierLocation(carrier, 8));
        var inner = new Relation(
            new CarrierLocation(carrier, 2),
            new CarrierLocation(carrier, 5));

        var spans = RelationReads.CoveredSpansOn(carrier, outer, inner);

        Assert.Equal(
            new[]
            {
                new Relation(new CarrierLocation(carrier, 0), new CarrierLocation(carrier, 2)),
                new Relation(new CarrierLocation(carrier, 2), new CarrierLocation(carrier, 5)),
                new Relation(new CarrierLocation(carrier, 5), new CarrierLocation(carrier, 8))
            },
            spans);
    }

    [Fact]
    public void RelationCoveredSpansIgnoreInverseCarrierSurfaceFromScalarReader()
    {
        var forward = new Carrier();
        var inverse = forward.Inverse();
        var segment = new Relation(
            new CarrierLocation(inverse, 0),
            new CarrierLocation(inverse, 8));

        var spans = RelationReads.CoveredSpansOn(forward, segment);

        Assert.Empty(spans);
    }

    [Fact]
    public void RelationCoveredSpansSkipUncoveredGaps()
    {
        var carrier = new Carrier();
        var first = new Relation(
            new CarrierLocation(carrier, 0),
            new CarrierLocation(carrier, 2));
        var second = new Relation(
            new CarrierLocation(carrier, 5),
            new CarrierLocation(carrier, 8));

        var spans = RelationReads.CoveredSpansOn(carrier, first, second);

        Assert.Equal(
            new[]
            {
                new Relation(new CarrierLocation(carrier, 0), new CarrierLocation(carrier, 2)),
                new Relation(new CarrierLocation(carrier, 5), new CarrierLocation(carrier, 8))
            },
            spans);
    }

    [Fact]
    public void RelationCoveredSpansIgnoreInvisibleSegments()
    {
        var x = new Carrier();
        var y = new Carrier();
        var invisible = new Relation(
            new CarrierLocation(y, 0),
            new CarrierLocation(y, 8));

        Assert.Empty(RelationReads.CoveredSpansOn(x, invisible));
    }

    [Fact]
    public void RelationSupportStatesReadReverseAndForwardParticipationBySpan()
    {
        var carrier = new Carrier();
        var forward = new Relation(
            new CarrierLocation(carrier, 0),
            new CarrierLocation(carrier, 8));
        var reverse = new Relation(
            new CarrierLocation(carrier, 2),
            new CarrierLocation(carrier, 0));

        var states = RelationReads.SupportStatesOn(carrier, forward, reverse);

        Assert.Equal(
            new[]
            {
                new Relation(
                    new Relation(new CarrierLocation(carrier, 0), new CarrierLocation(carrier, 2)),
                    new Relation(new CarrierLocation(carrier, -1), new CarrierLocation(carrier, 1))),
                new Relation(
                    new Relation(new CarrierLocation(carrier, 2), new CarrierLocation(carrier, 8)),
                    new Relation(new CarrierLocation(carrier, 0), new CarrierLocation(carrier, 1)))
            },
            states);
    }

    [Fact]
    public void RelationSupportStatesSkipUncoveredGaps()
    {
        var carrier = new Carrier();
        var first = new Relation(
            new CarrierLocation(carrier, 0),
            new CarrierLocation(carrier, 2));
        var second = new Relation(
            new CarrierLocation(carrier, 5),
            new CarrierLocation(carrier, 8));

        var states = RelationReads.SupportStatesOn(carrier, first, second);

        Assert.Equal(
            new[]
            {
                new Relation(
                    new Relation(new CarrierLocation(carrier, 0), new CarrierLocation(carrier, 2)),
                    new Relation(new CarrierLocation(carrier, 0), new CarrierLocation(carrier, 1))),
                new Relation(
                    new Relation(new CarrierLocation(carrier, 5), new CarrierLocation(carrier, 8)),
                    new Relation(new CarrierLocation(carrier, 0), new CarrierLocation(carrier, 1)))
            },
            states);
    }

    [Fact]
    public void RelationSupportStatesIgnoreInverseCarrierFlowFromScalarReader()
    {
        var forward = new Carrier();
        var inverse = forward.Inverse();
        var opposed = new Relation(
            new CarrierLocation(inverse, 0),
            new CarrierLocation(inverse, 8));

        var states = RelationReads.SupportStatesOn(forward, opposed);

        Assert.Empty(states);
    }

    [Fact]
    public void RelationSupportStatesMergeMultipleCompatibleParticipations()
    {
        var carrier = new Carrier();
        var firstForward = new Relation(
            new CarrierLocation(carrier, 0),
            new CarrierLocation(carrier, 8));
        var secondForward = new Relation(
            new CarrierLocation(carrier, 1),
            new CarrierLocation(carrier, 9));
        var reverse = new Relation(
            new CarrierLocation(carrier, 10),
            new CarrierLocation(carrier, 2));

        var states = RelationReads.SupportStatesOn(carrier, firstForward, secondForward, reverse);

        Assert.Contains(
            new Relation(
                new Relation(new CarrierLocation(carrier, 2), new CarrierLocation(carrier, 8)),
                new Relation(new CarrierLocation(carrier, -1), new CarrierLocation(carrier, 2))),
            states);
    }

    [Fact]
    public void RelationSupportStatesCanBeScopedToCarrierInterval()
    {
        var carrier = new Carrier();
        var scope = new Relation(
            new CarrierLocation(carrier, 2),
            new CarrierLocation(carrier, 6));
        var forward = new Relation(
            new CarrierLocation(carrier, 0),
            new CarrierLocation(carrier, 8));
        var reverse = new Relation(
            new CarrierLocation(carrier, 10),
            new CarrierLocation(carrier, 4));

        var states = RelationReads.SupportStatesWithinOn(carrier, scope, forward, reverse);

        Assert.Equal(
            new[]
            {
                new Relation(
                    new Relation(new CarrierLocation(carrier, 2), new CarrierLocation(carrier, 4)),
                    new Relation(new CarrierLocation(carrier, 0), new CarrierLocation(carrier, 1))),
                new Relation(
                    new Relation(new CarrierLocation(carrier, 4), new CarrierLocation(carrier, 6)),
                    new Relation(new CarrierLocation(carrier, -1), new CarrierLocation(carrier, 1)))
            },
            states);
    }

    [Fact]
    public void RelationScopedSupportProjectionsPreserveOnlyScopedParticipation()
    {
        var carrier = new Carrier();
        var scope = new Relation(
            new CarrierLocation(carrier, 2),
            new CarrierLocation(carrier, 6));
        var forward = new Relation(
            new CarrierLocation(carrier, 0),
            new CarrierLocation(carrier, 8));
        var reverse = new Relation(
            new CarrierLocation(carrier, 10),
            new CarrierLocation(carrier, 4));

        var support = RelationReads.SupportWithinOn(carrier, scope, forward, reverse);
        var tension = RelationReads.TensionWithinOn(carrier, scope, forward, reverse);
        var exclusive = RelationReads.ExclusiveWithinOn(carrier, scope, forward, reverse);

        Assert.Equal(
            new[]
            {
                new Relation(
                    new Relation(new CarrierLocation(carrier, 2), new CarrierLocation(carrier, 4)),
                    new Relation(new CarrierLocation(carrier, 0), new CarrierLocation(carrier, 1))),
                new Relation(
                    new Relation(new CarrierLocation(carrier, 4), new CarrierLocation(carrier, 6)),
                    new Relation(new CarrierLocation(carrier, -1), new CarrierLocation(carrier, 1)))
            },
            support);
        Assert.Equal(
            new[]
            {
                new Relation(
                    new Relation(new CarrierLocation(carrier, 4), new CarrierLocation(carrier, 6)),
                    new Relation(new CarrierLocation(carrier, -1), new CarrierLocation(carrier, 1)))
            },
            tension);
        Assert.Equal(
            new[]
            {
                new Relation(
                    new Relation(new CarrierLocation(carrier, 2), new CarrierLocation(carrier, 4)),
                    new Relation(new CarrierLocation(carrier, 0), new CarrierLocation(carrier, 1)))
            },
            exclusive);
    }

    [Fact]
    public void RelationSupportProjectionPreservesReverseAndForwardSlots()
    {
        var carrier = new Carrier();
        var state = new Relation(
            new CarrierLocation(carrier, -2),
            new CarrierLocation(carrier, 3));

        Assert.Equal(
            new Relation(
                new CarrierLocation(carrier, -2),
                new CarrierLocation(carrier, 3)),
            state.SupportOn(carrier));
    }

    [Fact]
    public void RelationTensionProjectionReadsSharedReverseAndForwardParticipation()
    {
        var carrier = new Carrier();
        var state = new Relation(
            new CarrierLocation(carrier, -2),
            new CarrierLocation(carrier, 3));

        Assert.Equal(
            new Relation(
                new CarrierLocation(carrier, -2),
                new CarrierLocation(carrier, 2)),
            state.TensionOn(carrier));
    }

    [Fact]
    public void RelationExclusiveProjectionKeepsOnlyLargerFlowSide()
    {
        var carrier = new Carrier();
        var forwardLarger = new Relation(
            new CarrierLocation(carrier, -2),
            new CarrierLocation(carrier, 5));
        var reverseLarger = new Relation(
            new CarrierLocation(carrier, -5),
            new CarrierLocation(carrier, 2));

        Assert.Equal(
            new Relation(
                new CarrierLocation(carrier, 0),
                new CarrierLocation(carrier, 3)),
            forwardLarger.ExclusiveOn(carrier));
        Assert.Equal(
            new Relation(
                new CarrierLocation(carrier, -3),
                new CarrierLocation(carrier, 0)),
            reverseLarger.ExclusiveOn(carrier));
    }

    [Fact]
    public void RelationSupportProjectionsCanReadNestedSupportState()
    {
        var carrier = new Carrier();
        var state = new Relation(
            new Relation(new CarrierLocation(carrier, 0), new CarrierLocation(carrier, 2)),
            new Relation(new CarrierLocation(carrier, -1), new CarrierLocation(carrier, 1)));

        Assert.Equal(
            new Relation(
                new CarrierLocation(carrier, -1),
                new CarrierLocation(carrier, 1)),
            state.SupportOn(carrier));
        Assert.Equal(
            new Relation(
                new CarrierLocation(carrier, -1),
                new CarrierLocation(carrier, 1)),
            state.TensionOn(carrier));
        Assert.Equal(
            new Relation(
                new CarrierLocation(carrier, 0),
                new CarrierLocation(carrier, 0)),
            state.ExclusiveOn(carrier));
    }

    [Fact]
    public void RelationSupportSpanProjectionPreservesIntervalsWithAnyParticipation()
    {
        var carrier = new Carrier();
        var forward = new Relation(
            new CarrierLocation(carrier, 0),
            new CarrierLocation(carrier, 8));
        var reverse = new Relation(
            new CarrierLocation(carrier, 2),
            new CarrierLocation(carrier, 0));

        var support = RelationReads.SupportOn(carrier, forward, reverse);

        Assert.Equal(
            new[]
            {
                new Relation(
                    new Relation(new CarrierLocation(carrier, 0), new CarrierLocation(carrier, 2)),
                    new Relation(new CarrierLocation(carrier, -1), new CarrierLocation(carrier, 1))),
                new Relation(
                    new Relation(new CarrierLocation(carrier, 2), new CarrierLocation(carrier, 8)),
                    new Relation(new CarrierLocation(carrier, 0), new CarrierLocation(carrier, 1)))
            },
            support);
    }

    [Fact]
    public void RelationTensionSpanProjectionPreservesOnlyIntervalsWithSharedOpposedParticipation()
    {
        var carrier = new Carrier();
        var forward = new Relation(
            new CarrierLocation(carrier, 0),
            new CarrierLocation(carrier, 8));
        var reverse = new Relation(
            new CarrierLocation(carrier, 2),
            new CarrierLocation(carrier, 0));

        var tension = RelationReads.TensionOn(carrier, forward, reverse);

        Assert.Equal(
            new[]
            {
                new Relation(
                    new Relation(new CarrierLocation(carrier, 0), new CarrierLocation(carrier, 2)),
                    new Relation(new CarrierLocation(carrier, -1), new CarrierLocation(carrier, 1)))
            },
            tension);
    }

    [Fact]
    public void RelationExclusiveSpanProjectionPreservesOnlyUnopposedIntervals()
    {
        var carrier = new Carrier();
        var forward = new Relation(
            new CarrierLocation(carrier, 0),
            new CarrierLocation(carrier, 8));
        var reverse = new Relation(
            new CarrierLocation(carrier, 2),
            new CarrierLocation(carrier, 0));

        var exclusive = RelationReads.ExclusiveOn(carrier, forward, reverse);

        Assert.Equal(
            new[]
            {
                new Relation(
                    new Relation(new CarrierLocation(carrier, 2), new CarrierLocation(carrier, 8)),
                    new Relation(new CarrierLocation(carrier, 0), new CarrierLocation(carrier, 1)))
            },
            exclusive);
    }

    [Fact]
    public void RelationTensionAndExclusiveSpanProjectionsReturnNoZeroStates()
    {
        var carrier = new Carrier();
        var forward = new Relation(
            new CarrierLocation(carrier, 0),
            new CarrierLocation(carrier, 8));

        Assert.Empty(RelationReads.TensionOn(carrier, forward));
        Assert.Equal(RelationReads.SupportOn(carrier, forward), RelationReads.ExclusiveOn(carrier, forward));
    }

    [Fact]
    public void BooleanStyleSpanReadsAreSupportProjections()
    {
        var carrier = new Carrier();
        var forward = new Relation(
            new CarrierLocation(carrier, 0),
            new CarrierLocation(carrier, 8));
        var reverse = new Relation(
            new CarrierLocation(carrier, 2),
            new CarrierLocation(carrier, 0));

        Assert.Equal(RelationReads.SupportOn(carrier, forward, reverse), BooleanStyleReads.OrOn(carrier, forward, reverse));
        Assert.Equal(RelationReads.TensionOn(carrier, forward, reverse), BooleanStyleReads.AndOn(carrier, forward, reverse));
        Assert.Equal(RelationReads.ExclusiveOn(carrier, forward, reverse), BooleanStyleReads.XorOn(carrier, forward, reverse));
    }

    [Fact]
    public void RelationUncoveredSpansReadScopedAbsenceBetweenCoveredIntervals()
    {
        var carrier = new Carrier();
        var scope = new Relation(
            new CarrierLocation(carrier, 0),
            new CarrierLocation(carrier, 10));
        var blocked = new Relation(
            new CarrierLocation(carrier, 2),
            new CarrierLocation(carrier, 5));

        var uncovered = RelationReads.UncoveredSpansOn(carrier, scope, blocked);

        Assert.Equal(
            new[]
            {
                new Relation(new CarrierLocation(carrier, 0), new CarrierLocation(carrier, 2)),
                new Relation(new CarrierLocation(carrier, 5), new CarrierLocation(carrier, 10))
            },
            uncovered);
    }

    [Fact]
    public void RelationUncoveredSpansReturnEmptyWhenScopeIsFullyCovered()
    {
        var carrier = new Carrier();
        var scope = new Relation(
            new CarrierLocation(carrier, 2),
            new CarrierLocation(carrier, 8));
        var blocked = new Relation(
            new CarrierLocation(carrier, 0),
            new CarrierLocation(carrier, 10));

        var uncovered = RelationReads.UncoveredSpansOn(carrier, scope, blocked);

        Assert.Empty(uncovered);
    }

    [Fact]
    public void RelationUncoveredSpansIgnoreInvisibleCoverage()
    {
        var carrier = new Carrier();
        var other = new Carrier();
        var scope = new Relation(
            new CarrierLocation(carrier, 0),
            new CarrierLocation(carrier, 10));
        var invisible = new Relation(
            new CarrierLocation(other, 2),
            new CarrierLocation(other, 5));

        var uncovered = RelationReads.UncoveredSpansOn(carrier, scope, invisible);

        Assert.Equal(
            new[]
            {
                scope
            },
            uncovered);
    }

    [Fact]
    public void RelationUncoveredSpansClipCoverageToScope()
    {
        var carrier = new Carrier();
        var scope = new Relation(
            new CarrierLocation(carrier, 2),
            new CarrierLocation(carrier, 8));
        var leftBlocked = new Relation(
            new CarrierLocation(carrier, 0),
            new CarrierLocation(carrier, 5));
        var rightBlocked = new Relation(
            new CarrierLocation(carrier, 7),
            new CarrierLocation(carrier, 10));

        var uncovered = RelationReads.UncoveredSpansOn(carrier, scope, leftBlocked, rightBlocked);

        Assert.Equal(
            new[]
            {
                new Relation(new CarrierLocation(carrier, 5), new CarrierLocation(carrier, 7))
            },
            uncovered);
    }

    [Fact]
    public void RelationCoveredSpansCanReadCoverageInsideScope()
    {
        var carrier = new Carrier();
        var scope = new Relation(
            new CarrierLocation(carrier, 2),
            new CarrierLocation(carrier, 8));
        var leftSupported = new Relation(
            new CarrierLocation(carrier, 0),
            new CarrierLocation(carrier, 5));
        var rightSupported = new Relation(
            new CarrierLocation(carrier, 7),
            new CarrierLocation(carrier, 10));

        var covered = RelationReads.CoveredSpansWithinOn(carrier, scope, leftSupported, rightSupported);

        Assert.Equal(
            new[]
            {
                new Relation(new CarrierLocation(carrier, 2), new CarrierLocation(carrier, 5)),
                new Relation(new CarrierLocation(carrier, 7), new CarrierLocation(carrier, 8))
            },
            covered);
    }

    [Fact]
    public void RelationPassAndBlockAreScopedFilterProjections()
    {
        var carrier = new Carrier();
        var scope = new Relation(
            new CarrierLocation(carrier, 0),
            new CarrierLocation(carrier, 10));
        var supported = new Relation(
            new CarrierLocation(carrier, 2),
            new CarrierLocation(carrier, 5));

        Assert.Equal(
            RelationReads.CoveredSpansWithinOn(carrier, scope, supported),
            BooleanStyleReads.PassOn(carrier, scope, supported));
        Assert.Equal(
            RelationReads.UncoveredSpansOn(carrier, scope, supported),
            BooleanStyleReads.BlockOn(carrier, scope, supported));
    }

    [Fact]
    public void RelationNotIsScopedAbsenceProjection()
    {
        var carrier = new Carrier();
        var scope = new Relation(
            new CarrierLocation(carrier, 0),
            new CarrierLocation(carrier, 10));
        var blocked = new Relation(
            new CarrierLocation(carrier, 2),
            new CarrierLocation(carrier, 5));

        Assert.Equal(
            RelationReads.UncoveredSpansOn(carrier, scope, blocked),
            BooleanStyleReads.NotOn(carrier, scope, blocked));
    }

    [Fact]
    public void SupportFilterCanSplitIncomingExtentIntoPassedAndBlockedSpans()
    {
        var carrier = new Carrier();
        var incomingScope = new Relation(
            new CarrierLocation(carrier, 0),
            new CarrierLocation(carrier, 10));
        var support = new Relation(
            new CarrierLocation(carrier, 2),
            new CarrierLocation(carrier, 5));

        var passed = BooleanStyleReads.PassOn(carrier, incomingScope, support);
        var blocked = BooleanStyleReads.BlockOn(carrier, incomingScope, support);

        Assert.Equal(
            new[]
            {
                new Relation(new CarrierLocation(carrier, 2), new CarrierLocation(carrier, 5))
            },
            passed);
        Assert.Equal(
            new[]
            {
                new Relation(new CarrierLocation(carrier, 0), new CarrierLocation(carrier, 2)),
                new Relation(new CarrierLocation(carrier, 5), new CarrierLocation(carrier, 10))
            },
            blocked);
    }

    [Fact]
    public void MultipleAccountingSurfacesIgnoreOrthogonalAndOpposedParticipation()
    {
        var x = new Carrier();
        var y = new Carrier();
        var incoming = new CarrierLocation(x, 10);

        var tension = incoming.NewTensionAfter(
            x,
            new CarrierLocation(x, 3),
            new CarrierLocation(y, 100),
            new CarrierLocation(x, -100),
            new CarrierLocation(x, 4));

        Assert.Equal(new CarrierLocation(x, 3), tension);
    }

    [Fact]
    public void IndependentCarrierAccountingDoesNotAbsorbWithoutRelation()
    {
        var source = new Carrier();
        var independent = new Carrier();
        var incoming = new CarrierLocation(independent, 10);

        var tension = incoming.NewTensionAfter(
            independent,
            new CarrierLocation(source, 6));

        Assert.Equal(incoming, tension);
    }

    [Fact]
    public void IndependentCarrierAccountingDoesNotAbsorbCoincidentTicks()
    {
        var source = new Carrier();
        var independent = new Carrier();
        var incoming = new CarrierLocation(source, 10);

        var tension = incoming.NewTensionAfter(
            source,
            new CarrierLocation(independent, 10));

        Assert.Equal(incoming, tension);
    }

    [Fact]
    public void InverseCarrierCanBeAccountedWithinItsOwnFlow()
    {
        var forward = new Carrier();
        var inverse = forward.Inverse();
        var incoming = new CarrierLocation(inverse, 10);

        var tension = incoming.NewTensionAfter(
            inverse,
            new CarrierLocation(inverse, 6));

        Assert.Equal(new CarrierLocation(inverse, 4), tension);
    }

    [Fact]
    public void OrthogonalAccountingDoesNotAbsorbCarrierTension()
    {
        var x = new Carrier();
        var y = new Carrier();
        var incoming = new CarrierLocation(x, 5);
        var orthogonalAccounted = new CarrierLocation(y, 100);

        var tension = incoming.NewTensionAfter(orthogonalAccounted, x);

        Assert.Equal(incoming, tension);
    }

    [Fact]
    public void OpposedAccountingDoesNotAbsorbCarrierTensionWithoutRoute()
    {
        var forward = new Carrier();
        var incoming = new CarrierLocation(forward, 5);
        var opposedAccounted = new CarrierLocation(forward, -100);

        var tension = incoming.NewTensionAfter(opposedAccounted, forward);

        Assert.Equal(incoming, tension);
    }

    [Fact]
    public void InvisibleCarrierDoesNotCreateTensionInReaderFrame()
    {
        var x = new Carrier();
        var y = new Carrier();
        var incoming = new CarrierLocation(y, 5);
        var accounted = new CarrierLocation(y, 0);

        var tension = incoming.NewTensionAfter(accounted, x);

        Assert.Equal(new CarrierLocation(y, 0), tension);
    }
}

