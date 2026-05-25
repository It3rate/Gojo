using Gojo.ConceptSurface;
using Gojo.Core;

namespace Gojo.Tests.ConceptSurface;

public sealed class BoundaryFlowSurfaceTests
{
    [Theory]
    [InlineData(false, false, "PN")]
    [InlineData(true, false, "P")]
    [InlineData(false, true, "N")]
    [InlineData(true, true, "S")]
    public void BoundarySurfaceReadsSummaryExposure(bool left, bool right, string label)
    {
        var axis = Relation.Segment(new Carrier(), -10, 10);
        var surface = BoundarySurface.Create(
            new Carrier(),
            new BoundaryExposure(left, right),
            axis);

        Assert.True(BoundarySurface.TryRead(
            surface,
            out var exposure,
            out var wholeOrAxis,
            out _,
            out _));
        Assert.Equal(left, exposure.LeftBoundary);
        Assert.Equal(right, exposure.RightBoundary);
        Assert.Equal(label, exposure.Label);
        Assert.Equal(axis, wholeOrAxis);
    }

    [Theory]
    [InlineData(false, false, "None")]
    [InlineData(true, false, "F")]
    [InlineData(false, true, "I")]
    [InlineData(true, true, "FI")]
    public void FlowSurfaceReadsSummaryExposure(bool forward, bool inverse, string label)
    {
        var selected = Relation.Segment(new Carrier(), 0, 1);
        var surface = FlowSurface.Create(
            new Carrier(),
            new FlowExposure(forward, inverse),
            selected);

        Assert.True(FlowSurface.TryRead(
            surface,
            out var exposure,
            out var readSelected,
            out _,
            out _));
        Assert.Equal(forward, exposure.ForwardFlow);
        Assert.Equal(inverse, exposure.InverseFlow);
        Assert.Equal(label, exposure.Label);
        Assert.Equal(selected, readSelected);
    }

    [Theory]
    [MemberData(nameof(AllBoundaryFlowStates))]
    public void BoundaryFlowSurfaceReadsAllSixteenCombinations(
        BoundaryExposure boundary,
        FlowExposure flow)
    {
        var selected = Relation.Segment(new Carrier(), -3, 3);
        var surface = BoundaryFlowSurface.Create(new Carrier(), boundary, flow, selected);

        Assert.True(BoundaryFlowSurface.TryRead(surface, out var exposure));
        Assert.Equal(boundary, exposure.Boundary);
        Assert.Equal(flow, exposure.Flow);
    }

    [Fact]
    public void BoundaryFlowSurfaceCreatePairsBoundarySidesWithPrimitiveFlow()
    {
        var selected = Relation.Segment(new Carrier(), -3, 3);
        var surface = BoundaryFlowSurface.Create(
            new Carrier(),
            BoundaryExposure.FullSigned,
            FlowExposure.Forward,
            selected);

        Assert.True(BoundaryFlowSurface.TryRead(
            surface,
            out var boundary,
            out var flow,
            out var boundarySurface,
            out var flowSurface,
            out _));
        Assert.True(boundary.IsFullSigned);
        Assert.True(flow.IsForwardOnly);
        Assert.True(BoundarySideSurface.TryReadExposure(boundarySurface, out var sideExposure));
        Assert.True(sideExposure.IsFullSigned);
        Assert.Equal(FlowExposure.Forward, FlowFaceSurface.ReadExposure(boundarySurface));
        Assert.Equal(FlowExposure.Forward, FlowFaceSurface.ReadExposure(flowSurface));
    }

    [Fact]
    public void BoundedWholeWithNoFlowCanCarryWholePartition()
    {
        var field = new Carrier();
        var whole = Relation.Segment(field, 0, 100);
        var boundary = BoundarySurface.BoundedWhole(
            new Carrier(),
            whole,
            Relation.Point(field, 0),
            Relation.Point(field, 100));
        var flow = FlowSurface.None(new Carrier(), whole);
        var partitionAxis = new Carrier();
        var partition = ConceptPartition.Create(new Carrier(), whole, partitionAxis, 100);
        var surface = BoundaryFlowSurface.Create(new Carrier(), boundary, flow, partition);

        Assert.True(BoundaryFlowSurface.TryRead(
            surface,
            out var boundaryRead,
            out var flowRead,
            out _,
            out _,
            out var witness));
        Assert.True(boundaryRead.IsBoundedWhole);
        Assert.True(flowRead.IsNone);
        Assert.Equal(partition, witness);
        Assert.True(ConceptPartition.TryRead(
            Assert.IsType<Relation>(witness),
            out var partitionWhole,
            out _,
            out _,
            out var count,
            out _));
        Assert.Equal(whole, partitionWhole);
        Assert.Equal(100, count.Ticks);
    }

    [Fact]
    public void BoundedWholeWithBothFlowsKeepsExplicitFlowSurfaces()
    {
        var bridge = new Carrier();
        var whole = Relation.Segment(bridge, 0, 1);
        var forward = Relation.Segment(bridge, 0, 1);
        var inverse = Relation.Segment(bridge.Inverse(), 0, 1);
        var boundary = BoundarySurface.BoundedWhole(
            new Carrier(),
            whole,
            Relation.Point(bridge, 0),
            Relation.Point(bridge, 1));
        var flow = FlowSurface.Both(new Carrier(), whole, forward, inverse);
        var surface = BoundaryFlowSurface.Create(new Carrier(), boundary, flow);

        Assert.True(BoundaryFlowSurface.TryRead(
            surface,
            out var boundaryRead,
            out var flowRead,
            out _,
            out var flowSurface,
            out _));
        Assert.True(boundaryRead.IsBoundedWhole);
        Assert.True(flowRead.IsBoth);
        Assert.True(FlowSurface.TryRead(
            flowSurface,
            out _,
            out _,
            out var forwardRead,
            out var inverseRead));
        AssertFlowFace(forwardRead, FlowFace.Forward, forward);
        AssertFlowFace(inverseRead, FlowFace.Inverse, inverse);
    }

    [Fact]
    public void FootballFieldIsBoundedWholeWithOpposedCalibrationReads()
    {
        var field = new Carrier();
        var teamAProgress = new Carrier();
        var teamBProgress = new Carrier();
        var fieldWhole = Relation.Segment(field, 0, 100);
        var fieldSurface = BoundaryFlowSurface.Create(
            new Carrier(),
            BoundaryExposure.BoundedWhole,
            FlowExposure.None,
            fieldWhole);
        var teamARead = new Relation(
            Relation.Segment(field, 0, 100),
            Relation.Segment(teamAProgress, 0, 100));
        var teamBRead = new Relation(
            Relation.Segment(field, 100, 0),
            Relation.Segment(teamBProgress, 0, 100));
        var ball = Relation.Point(field, 70);
        var possession = Relation.OrderedGroup(
            new Carrier(),
            teamARead,
            ball);
        var gameRead = ConceptReadFocus.Create(
            new Carrier(),
            fieldSurface,
            teamARead,
            possession);

        Assert.True(BoundaryFlowSurface.TryRead(fieldSurface, out var exposure));
        Assert.True(exposure.Boundary.IsBoundedWhole);
        Assert.True(exposure.Flow.IsNone);
        Assert.True(ConceptReadFocus.TryRead(
            gameRead,
            out var selected,
            out var activeRead,
            out var readState));
        Assert.Equal(fieldSurface, selected);
        Assert.Equal(teamARead, activeRead);
        Assert.Equal(possession, readState);
        Assert.NotEqual(teamARead, teamBRead);
    }

    [Fact]
    public void BoundaryAndFlowExposureCanBeDualedWithoutChangingSelectedCarrier()
    {
        var carrier = new Carrier();
        var surface = CarrierAxisSurface.PositiveUnit(new Carrier(), carrier);

        Assert.True(CarrierAxisSurface.TryDual(surface, out var inverted));
        Assert.True(CarrierAxisSurface.TryRead(
            inverted,
            out var readCarrier,
            out var boundary,
            out var flow,
            out _,
            out _));
        Assert.Same(carrier, readCarrier);
        Assert.True(boundary.IsNegative);
        Assert.True(flow.IsInverseOnly);
    }

    [Fact]
    public void BothFlowExposureDualsToItself()
    {
        var carrier = new Carrier();
        var surface = CarrierAxisSurface.SignedBothFlows(new Carrier(), carrier);

        Assert.True(CarrierAxisSurface.TryDual(surface, out var inverted));
        Assert.True(CarrierAxisSurface.TryRead(
            inverted,
            out _,
            out var boundary,
            out var flow,
            out _,
            out _));
        Assert.True(boundary.IsFullSigned);
        Assert.True(flow.IsBoth);
    }

    [Fact]
    public void NoFlowBoundedWholeDualsToNoFlowBoundedWhole()
    {
        var carrier = new Carrier();
        var surface = CarrierAxisSurface.BoundedWholeNoFlow(new Carrier(), carrier);

        Assert.True(CarrierAxisSurface.TryDual(surface, out var inverted));
        Assert.True(CarrierAxisSurface.TryRead(
            inverted,
            out _,
            out var boundary,
            out var flow,
            out _,
            out _));
        Assert.True(boundary.IsBoundedWhole);
        Assert.True(flow.IsNone);
    }

    [Fact]
    public void CarrierAxisSurfaceSignedForwardPairsBoundarySidesWithFlowFace()
    {
        var carrier = new Carrier();
        var surface = CarrierAxisSurface.SignedForward(new Carrier(), carrier);

        Assert.True(CarrierAxisSurface.TryRead(
            surface,
            out _,
            out var boundary,
            out var flow,
            out var boundarySurface,
            out var flowSurface));
        Assert.True(boundary.IsFullSigned);
        Assert.True(flow.IsForwardOnly);
        Assert.True(BoundarySideSurface.TryReadExposure(boundarySurface, out var sideExposure));
        Assert.True(sideExposure.IsFullSigned);
        Assert.Equal(FlowExposure.Forward, FlowFaceSurface.ReadExposure(boundarySurface));
        Assert.Equal(FlowExposure.Forward, FlowFaceSurface.ReadExposure(flowSurface));
    }

    [Fact]
    public void CarrierAxisSurfaceBoundedWholeNoFlowCarriesWholeAndNoFlowEvidence()
    {
        var carrier = new Carrier();
        var surface = CarrierAxisSurface.BoundedWholeNoFlow(new Carrier(), carrier);

        Assert.True(CarrierAxisSurface.TryRead(
            surface,
            out _,
            out var boundary,
            out var flow,
            out var boundarySurface,
            out var flowSurface));
        Assert.True(boundary.IsBoundedWhole);
        Assert.True(flow.IsNone);
        Assert.True(BoundedWholeSurface.TryRead(boundarySurface, out var whole, out _, out _, out _));
        Assert.Same(carrier, whole);
        Assert.True(FlowSurface.TryRead(flowSurface, out var flowRead, out _, out _, out _));
        Assert.True(flowRead.IsNone);
    }

    [Fact]
    public void CarrierBoundarySurfaceHoldsDomainWithoutFlowExposure()
    {
        var carrier = new Carrier();
        var whole = Relation.Segment(carrier, 0, 10);
        var partition = ConceptPartition.Create(new Carrier(), whole, new Carrier(), 10);
        var surface = CarrierBoundarySurface.BoundedWhole(
            new Carrier(),
            carrier,
            Relation.Point(carrier, 0),
            Relation.Point(carrier, 10),
            partition);

        Assert.True(CarrierBoundarySurface.TryRead(
            surface,
            out var readCarrier,
            out var boundary,
            out _,
            out var readPartition));
        Assert.Same(carrier, readCarrier);
        Assert.True(boundary.IsBoundedWhole);
        Assert.Equal(partition, readPartition);
    }

    [Fact]
    public void CarrierBoundarySurfaceCanPackageExistingBoundaryEvidence()
    {
        var carrier = new Carrier();
        var boundarySurface = BoundedWholeSurface.Create(new Carrier(), carrier);
        var partition = Relation.Point(new Carrier(), 4);
        var surface = CarrierBoundarySurface.Create(
            new Carrier(),
            carrier,
            boundarySurface,
            partition);

        Assert.True(CarrierBoundarySurface.TryRead(
            surface,
            out var readCarrier,
            out var boundary,
            out var readBoundarySurface,
            out var readPartition));
        Assert.Same(carrier, readCarrier);
        Assert.True(boundary.IsBoundedWhole);
        Assert.Equal(boundarySurface, readBoundarySurface);
        Assert.Equal(partition, readPartition);
    }

    [Fact]
    public void RelationFlowSurfaceHoldsTraversalWithoutBoundaryExposure()
    {
        var route = new Carrier();
        var relation = Relation.Segment(route, 0, 1);
        var surface = RelationFlowSurface.Forward(new Carrier(), relation, relation);

        Assert.True(RelationFlowSurface.TryRead(
            surface,
            out var readRelation,
            out var flow,
            out _,
            out var forwardFlow,
            out var inverseFlow,
            out _));
        Assert.Equal(relation, readRelation);
        Assert.True(flow.IsForwardOnly);
        AssertFlowFace(forwardFlow, FlowFace.Forward, relation);
        Assert.NotEqual(relation.Reverse(), inverseFlow);
    }

    [Fact]
    public void RelationFlowSurfaceCanPackageExistingFlowEvidence()
    {
        var route = new Carrier();
        var relation = Relation.Segment(route, 0, 1);
        var flowSurface = FlowFaceSurface.Forward(new Carrier(), relation, relation);
        var gate = Relation.Point(new Carrier(), 1);
        var surface = RelationFlowSurface.Create(new Carrier(), relation, flowSurface, gate);

        Assert.True(RelationFlowSurface.TryRead(
            surface,
            out var readRelation,
            out var flow,
            out var readFlowSurface,
            out var forwardFlow,
            out _,
            out var readGate));
        Assert.Equal(relation, readRelation);
        Assert.True(flow.IsForwardOnly);
        Assert.Equal(flowSurface, readFlowSurface);
        Assert.Equal(flowSurface, forwardFlow);
        Assert.Equal(gate, readGate);
    }

    [Fact]
    public void RelationFlowSurfaceBothKeepsBothTraversalSurfaces()
    {
        var route = new Carrier();
        var forward = Relation.Segment(route, 0, 1);
        var inverse = Relation.Segment(route.Inverse(), 0, 1);
        var surface = RelationFlowSurface.Both(
            new Carrier(),
            forward,
            forward,
            inverse,
            Relation.Point(Carrier.Unknown, 0));

        Assert.True(RelationFlowSurface.TryRead(
            surface,
            out _,
            out var flow,
            out _,
            out var forwardFlow,
            out var inverseFlow,
            out _));
        Assert.True(flow.IsBoth);
        AssertFlowFace(forwardFlow, FlowFace.Forward, forward);
        AssertFlowFace(inverseFlow, FlowFace.Inverse, inverse);
    }

    [Fact]
    public void CarrierAxisSurfaceCanPackageExistingBoundaryAndFlowEvidence()
    {
        var carrier = new Carrier();
        var flowSurface = FlowFaceSurface.Forward(new Carrier(), carrier);
        var positive = BoundarySideSurface.Positive(new Carrier(), flowSurface);
        var negative = BoundarySideSurface.Negative(new Carrier(), flowSurface);
        var boundarySurface = Relation.OrderedGroup(new Carrier(), positive, negative);
        var surface = CarrierAxisSurface.Create(
            new Carrier(),
            carrier,
            boundarySurface,
            flowSurface);

        Assert.True(CarrierAxisSurface.TryRead(
            surface,
            out var readCarrier,
            out var boundary,
            out var flow,
            out var readBoundarySurface,
            out var readFlowSurface));
        Assert.Same(carrier, readCarrier);
        Assert.True(boundary.IsFullSigned);
        Assert.True(flow.IsForwardOnly);
        Assert.Equal(boundarySurface, readBoundarySurface);
        Assert.Equal(flowSurface, readFlowSurface);
    }

    [Fact]
    public void RelationFlowSurfaceDualSwapsFlowExposureAndEvidence()
    {
        var route = new Carrier();
        var forward = Relation.Segment(route, 0, 1);
        var surface = RelationFlowSurface.Forward(new Carrier(), forward, forward);

        Assert.True(RelationFlowSurface.TryDual(surface, out var inverted));
        Assert.True(RelationFlowSurface.TryRead(
            inverted,
            out _,
            out var flow,
            out _,
            out var forwardFlow,
            out var inverseFlow,
            out _));
        Assert.True(flow.IsInverseOnly);
        AssertFlowFace(inverseFlow, FlowFace.Inverse);
        Assert.NotEqual(forward, forwardFlow);
    }

    public static IEnumerable<object[]> AllBoundaryFlowStates()
    {
        var boundaries = new[]
        {
            BoundaryExposure.FullSigned,
            BoundaryExposure.Positive,
            BoundaryExposure.Negative,
            BoundaryExposure.BoundedWhole
        };
        var flows = new[]
        {
            FlowExposure.None,
            FlowExposure.Forward,
            FlowExposure.Inverse,
            FlowExposure.Both
        };

        foreach (var boundary in boundaries)
        {
            foreach (var flow in flows)
            {
                yield return [boundary, flow];
            }
        }
    }

    private static void AssertFlowFace(
        IElement flowSurface,
        FlowFace expectedFace,
        IElement? expectedEvidence = null)
    {
        var surface = Assert.IsType<Relation>(flowSurface);
        Assert.True(FlowFaceSurface.TryRead(
            surface,
            out var face,
            out _,
            out var evidence));
        Assert.Equal(expectedFace, face);
        if (expectedEvidence is not null)
        {
            Assert.Equal(expectedEvidence, evidence);
        }
    }
}
