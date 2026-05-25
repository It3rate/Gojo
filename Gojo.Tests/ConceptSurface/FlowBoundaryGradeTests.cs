using Gojo.ConceptSurface;
using Gojo.Core;
using Gojo.Structures;

namespace Gojo.Tests.ConceptSurface;

public sealed class FlowBoundaryGradeTests
{
    [Fact]
    public void FlowFaceSurfaceReadsPrimitiveFaces()
    {
        var selected = Relation.Point(new Carrier(), 9);
        var evidence = Relation.Segment(new Carrier(), 0, 3);
        var forward = FlowFaceSurface.Forward(new Carrier(), selected, evidence);
        var inverse = FlowFaceSurface.Inverse(new Carrier(), selected, evidence);

        Assert.True(FlowFaceSurface.TryRead(forward, out var forwardFace, out var forwardSelected, out var forwardEvidence));
        Assert.Equal(FlowFace.Forward, forwardFace);
        Assert.Equal(selected, forwardSelected);
        Assert.Equal(evidence, forwardEvidence);

        Assert.True(FlowFaceSurface.TryRead(inverse, out var inverseFace, out var inverseSelected, out var inverseEvidence));
        Assert.Equal(FlowFace.Inverse, inverseFace);
        Assert.Equal(selected, inverseSelected);
        Assert.Equal(evidence, inverseEvidence);
        Assert.Equal(FlowFace.Forward, inverseFace.Dual());
    }

    [Fact]
    public void FlowExposureReadsAbsentAndComposedFaces()
    {
        var selected = Relation.Point(new Carrier(), 1);
        var empty = Relation.OrderedGroup(new Carrier());
        var forward = FlowFaceSurface.Forward(new Carrier(), selected);
        var inverse = FlowFaceSurface.Inverse(new Carrier(), selected);
        var both = Relation.OrderedGroup(new Carrier(), forward, inverse);

        Assert.Equal(FlowExposure.None, FlowFaceSurface.ReadExposure(empty));
        Assert.Equal(FlowExposure.Forward, FlowFaceSurface.ReadExposure(forward));
        Assert.Equal(FlowExposure.Inverse, FlowFaceSurface.ReadExposure(inverse));
        Assert.Equal(FlowExposure.Both, FlowFaceSurface.ReadExposure(both));
    }

    [Fact]
    public void FlowSurfaceReadsPrimitiveFaceEvidence()
    {
        var selected = Relation.Point(new Carrier(), 1);
        var forward = FlowFaceSurface.Forward(new Carrier(), selected);
        var inverse = FlowFaceSurface.Inverse(new Carrier(), selected);
        var primitiveGroup = Relation.OrderedGroup(new Carrier(), forward, inverse);

        Assert.True(FlowSurface.TryRead(
            primitiveGroup,
            out var exposure,
            out var readSelected,
            out var forwardRead,
            out var inverseRead));
        Assert.Equal(FlowExposure.Both, exposure);
        Assert.Equal(selected, readSelected);
        Assert.Equal(forward, forwardRead);
        Assert.Equal(inverse, inverseRead);

        var directGroup = Relation.OrderedGroup(new Carrier(), forward, inverse);
        Assert.True(FlowSurface.TryRead(directGroup, out var directExposure, out _, out _, out _));
        Assert.Equal(FlowExposure.Both, directExposure);
    }

    [Fact]
    public void BoundarySideSurfaceRequiresPrimitiveFlowFace()
    {
        var selected = Relation.Point(new Carrier(), 2);
        var flowFace = FlowFaceSurface.Forward(new Carrier(), selected);
        var cutEvidence = Relation.Point(new Carrier(), 0);
        var side = BoundarySideSurface.Positive(new Carrier(), flowFace, cutEvidence);

        Assert.True(BoundarySideSurface.TryRead(side, out var readSide, out var readFlowFace, out var readCutEvidence));
        Assert.Equal(BoundarySide.Positive, readSide);
        Assert.Equal(flowFace, readFlowFace);
        Assert.Equal(cutEvidence, readCutEvidence);

        var nonFlowRelation = Relation.Point(new Carrier(), 0);
        var invalidSide = BoundarySideSurface.Positive(new Carrier(), nonFlowRelation);

        Assert.False(BoundarySideSurface.TryRead(invalidSide, out _, out _, out _));
    }

    [Fact]
    public void FlowFaceSurfaceDoesNotReadBoundarySideAsFace()
    {
        var selected = Relation.Point(new Carrier(), 2);
        var flowFace = FlowFaceSurface.Forward(new Carrier(), selected);
        var negativeSide = BoundarySideSurface.Negative(new Carrier(), flowFace);

        Assert.False(FlowFaceSurface.TryRead(negativeSide, out _, out _, out _));
        Assert.Equal(FlowExposure.Forward, FlowFaceSurface.ReadExposure(negativeSide));
    }

    [Fact]
    public void BoundaryExposureReadsComposedSidesOverFlow()
    {
        var selected = Relation.Point(new Carrier(), 4);
        var flowFace = FlowFaceSurface.Forward(new Carrier(), selected);
        var positive = BoundarySideSurface.Positive(new Carrier(), flowFace);
        var negative = BoundarySideSurface.Negative(new Carrier(), flowFace);
        var both = Relation.OrderedGroup(new Carrier(), positive, negative);

        Assert.True(BoundarySideSurface.TryReadExposure(positive, out var positiveExposure));
        Assert.Equal(BoundaryExposure.Positive, positiveExposure);

        Assert.True(BoundarySideSurface.TryReadExposure(negative, out var negativeExposure));
        Assert.Equal(BoundaryExposure.Negative, negativeExposure);

        Assert.True(BoundarySideSurface.TryReadExposure(both, out var fullExposure));
        Assert.Equal(BoundaryExposure.FullSigned, fullExposure);

        var empty = Relation.OrderedGroup(new Carrier());
        Assert.False(BoundarySideSurface.TryReadExposure(empty, out _));
    }

    [Fact]
    public void BoundarySurfaceReadsPrimitiveSideEvidence()
    {
        var selected = Relation.Point(new Carrier(), 4);
        var flowFace = FlowFaceSurface.Forward(new Carrier(), selected);
        var positive = BoundarySideSurface.Positive(new Carrier(), flowFace);
        var negative = BoundarySideSurface.Negative(new Carrier(), flowFace);
        var primitiveGroup = Relation.OrderedGroup(new Carrier(), positive, negative);

        Assert.True(BoundarySurface.TryRead(
            primitiveGroup,
            out var exposure,
            out var readSelected,
            out var positiveRead,
            out var negativeRead));
        Assert.Equal(BoundaryExposure.FullSigned, exposure);
        Assert.Equal(selected, readSelected);
        Assert.Equal(positive, positiveRead);
        Assert.Equal(negative, negativeRead);

        var directGroup = Relation.OrderedGroup(new Carrier(), positive, negative);
        Assert.True(BoundarySurface.TryRead(directGroup, out var directExposure, out _, out _, out _));
        Assert.Equal(BoundaryExposure.FullSigned, directExposure);
    }

    [Fact]
    public void BoundedWholeSurfaceCarriesWholeBoundaryPairAndPartition()
    {
        var wholeCarrier = new Carrier();
        var whole = Relation.Segment(wholeCarrier, 0, 100);
        var left = Relation.Point(wholeCarrier, 0);
        var right = Relation.Point(wholeCarrier, 100);
        var partition = ConceptPartition.Create(new Carrier(), whole, new Carrier(), 10);
        var surface = BoundedWholeSurface.Create(new Carrier(), whole, left, right, partition);

        Assert.True(BoundedWholeSurface.TryRead(
            surface,
            out var readWhole,
            out var readLeft,
            out var readRight,
            out var readPartition));
        Assert.Equal(whole, readWhole);
        Assert.Equal(left, readLeft);
        Assert.Equal(right, readRight);
        Assert.Equal(partition, readPartition);

        Assert.True(BoundarySurface.TryRead(
            surface,
            out var exposure,
            out var selected,
            out var selectedLeft,
            out var selectedRight));
        Assert.Equal(BoundaryExposure.BoundedWhole, exposure);
        Assert.Equal(whole, selected);
        Assert.Equal(left, selectedLeft);
        Assert.Equal(right, selectedRight);
    }

    [Fact]
    public void FlowBoundaryGradeLaneSurfaceMapsBornSidesToOccupiedLanes()
    {
        var selected = Relation.Point(new Carrier(), 5);
        var positiveForward = FlowBoundaryGradeLaneSurface.Create(
            new Carrier(),
            FlowFace.Forward,
            BoundarySide.Positive,
            selected);
        var negativeInverse = FlowBoundaryGradeLaneSurface.Create(
            new Carrier(),
            FlowFace.Inverse,
            BoundarySide.Negative,
            selected);

        Assert.True(FlowBoundaryGradeLaneSurface.TryRead(
            positiveForward,
            out var firstLane,
            out var firstFlowFace,
            out var firstBoundarySide,
            out var firstSelected));
        Assert.Equal(BoundaryFlowLane.PositiveForward, firstLane);
        Assert.True(FlowFaceSurface.TryRead(firstFlowFace, out var firstFlow, out _, out _));
        Assert.Equal(FlowFace.Forward, firstFlow);
        Assert.True(BoundarySideSurface.TryRead(firstBoundarySide, out var firstSide, out _, out _));
        Assert.Equal(BoundarySide.Positive, firstSide);
        Assert.Equal(selected, firstSelected);

        Assert.True(FlowBoundaryGradeLaneSurface.TryRead(
            negativeInverse,
            out var secondLane,
            out _,
            out _,
            out _));
        Assert.Equal(BoundaryFlowLane.NegativeInverse, secondLane);
    }

    [Fact]
    public void OccupiedLaneSurfaceUsesPrimitiveBoundaryAndFlowEvidence()
    {
        var selected = Relation.Point(new Carrier(), 5);
        var laneSurface = BoundaryFlowLaneSurface.Create(
            new Carrier(),
            BoundaryFlowLane.PositiveForward,
            selected,
            selected);

        Assert.True(BoundaryFlowLaneSurface.TryRead(
            laneSurface,
            out var lane,
            out var boundarySurface,
            out var flowSurface));
        Assert.Equal(BoundaryFlowLane.PositiveForward, lane);
        Assert.True(FlowFaceSurface.TryRead(flowSurface, out var flowFace, out _, out _));
        Assert.Equal(FlowFace.Forward, flowFace);
        Assert.True(BoundarySideSurface.TryRead(
            boundarySurface,
            out var boundarySide,
            out var sideFlow,
            out _));
        Assert.Equal(BoundarySide.Positive, boundarySide);
        Assert.Equal(flowSurface, sideFlow);
    }

    [Fact]
    public void WholeNoFlowLaneSurfaceUsesBoundedWholeAndNoFlowEvidence()
    {
        var selected = Relation.Point(new Carrier(), 5);
        var laneSurface = BoundaryFlowLaneSurface.Create(
            new Carrier(),
            BoundaryFlowLane.WholeNone,
            selected,
            selected);

        Assert.True(BoundaryFlowLaneSurface.TryRead(
            laneSurface,
            out var lane,
            out var boundarySurface,
            out var flowSurface));
        Assert.Equal(BoundaryFlowLane.WholeNone, lane);
        Assert.True(BoundedWholeSurface.TryRead(
            boundarySurface,
            out var whole,
            out _,
            out _,
            out _));
        Assert.Equal(selected, whole);
        Assert.True(FlowSurface.TryRead(flowSurface, out var flow, out _, out _, out _));
        Assert.Equal(FlowExposure.None, flow);
    }

    [Fact]
    public void PositiveNoFlowLaneSurfaceUsesStrippedBoundarySideEvidence()
    {
        var selected = Relation.Point(new Carrier(), 5);
        var laneSurface = BoundaryFlowLaneSurface.Create(
            new Carrier(),
            BoundaryFlowLane.PositiveNone,
            selected,
            selected);

        Assert.True(BoundaryFlowLaneSurface.TryRead(
            laneSurface,
            out var lane,
            out var boundarySurface,
            out var flowSurface));
        Assert.Equal(BoundaryFlowLane.PositiveNone, lane);
        Assert.True(StrippedBoundarySideSurface.TryRead(
            boundarySurface,
            out var side,
            out var sideFlow,
            out var strippedSelected,
            out _));
        Assert.Equal(BoundarySide.Positive, side);
        Assert.Equal(selected, strippedSelected);
        Assert.True(FlowSurface.TryRead(sideFlow, out var sideFlowRead, out _, out _, out _));
        Assert.Equal(FlowExposure.None, sideFlowRead);
        Assert.True(FlowSurface.TryRead(flowSurface, out var flow, out _, out _, out _));
        Assert.Equal(FlowExposure.None, flow);
    }

    [Fact]
    public void BoundaryFlowSurfaceCanPairPrimitiveBoundaryAndFlowEvidence()
    {
        var selected = Relation.Point(new Carrier(), 8);
        var forward = FlowFaceSurface.Forward(new Carrier(), selected);
        var inverse = FlowFaceSurface.Inverse(new Carrier(), selected);
        var positive = BoundarySideSurface.Positive(new Carrier(), forward);
        var negative = BoundarySideSurface.Negative(new Carrier(), forward);
        var boundaryEvidence = Relation.OrderedGroup(new Carrier(), positive, negative);
        var flowEvidence = Relation.OrderedGroup(new Carrier(), forward, inverse);
        var surface = BoundaryFlowSurface.Create(new Carrier(), boundaryEvidence, flowEvidence);

        Assert.True(BoundaryFlowSurface.TryRead(
            surface,
            out var boundary,
            out var flow,
            out var boundaryRead,
            out var flowRead,
            out _));
        Assert.Equal(BoundaryExposure.FullSigned, boundary);
        Assert.Equal(FlowExposure.Both, flow);
        Assert.Equal(boundaryEvidence, boundaryRead);
        Assert.Equal(flowEvidence, flowRead);
    }

    [Fact]
    public void BoundaryFlowUnitShapeSignedForwardCarriesPrimitiveBasisEvidence()
    {
        var signedTickShape = BoundaryFlowUnitShape.SignedForward();

        Assert.True(BoundaryFlowLaneBasis.TryReadUnitShapeBasis(
            signedTickShape,
            out var basis,
            out var exposure,
            out _,
            out var lanes));
        Assert.Equal(new BoundaryFlowExposure(BoundaryExposure.FullSigned, FlowExposure.Forward), exposure);
        Assert.Equal("PF|NF", string.Join("|", lanes.Select(lane => lane.Label)));
        Assert.True(BoundarySideSurface.TryReadExposure(basis, out var sideExposure));
        Assert.Equal(BoundaryExposure.FullSigned, sideExposure);
        Assert.Equal(FlowExposure.Forward, FlowFaceSurface.ReadExposure(basis));
    }

    [Fact]
    public void BoundaryFlowUnitShapeSignedBothFlowsCarriesBothPrimitiveFaces()
    {
        var signedTickShape = BoundaryFlowUnitShape.SignedBothFlows();

        Assert.True(BoundaryFlowLaneBasis.TryReadUnitShapeBasis(
            signedTickShape,
            out var basis,
            out var exposure,
            out _,
            out var lanes));
        Assert.Equal(new BoundaryFlowExposure(BoundaryExposure.FullSigned, FlowExposure.Both), exposure);
        Assert.Equal("PF|PI|NF|NI", string.Join("|", lanes.Select(lane => lane.Label)));
        Assert.True(BoundarySideSurface.TryReadExposure(basis, out var sideExposure));
        Assert.Equal(BoundaryExposure.FullSigned, sideExposure);
        Assert.Equal(FlowExposure.Both, FlowFaceSurface.ReadExposure(basis));
    }

    [Fact]
    public void BoundaryFlowUnitShapeBoundedWholeNoFlowCarriesWholeEvidence()
    {
        var selected = Relation.Point(new Carrier(), 1);

        Assert.True(BoundaryFlowUnitShape.TryCreatePrimitiveBasis(
            new Carrier(),
            BoundaryExposure.BoundedWhole,
            FlowExposure.None,
            selected,
            out var primitiveBasis));
        Assert.True(BoundedWholeSurface.TryFind(
            primitiveBasis,
            out _,
            out var primitiveWhole,
            out _,
            out _,
            out _));
        Assert.Equal(selected, primitiveWhole);

        var wholeTickShape = BoundaryFlowUnitShape.BoundedWholeNoFlow(selected);
        Assert.True(BoundaryFlowLaneBasis.TryReadUnitShapeBasis(
            wholeTickShape,
            out var basis,
            out var exposure,
            out _,
            out var lanes));
        Assert.Equal(new BoundaryFlowExposure(BoundaryExposure.BoundedWhole, FlowExposure.None), exposure);
        Assert.Equal("S:None", string.Join("|", lanes.Select(lane => lane.Label)));
        Assert.False(BoundarySideSurface.TryReadExposure(basis, out _));
        Assert.Equal(FlowExposure.None, FlowFaceSurface.ReadExposure(basis));
        Assert.True(BoundedWholeSurface.TryFind(basis, out _, out var whole, out _, out _, out _));
        Assert.Equal(selected, whole);
    }

    [Fact]
    public void BoundaryFlowUnitShapeBoundedWholeForwardCarriesWholeAndFlowEvidence()
    {
        var selected = Relation.Point(new Carrier(), 1);
        var wholeForwardShape = BoundaryFlowUnitShape.DescribedAtom(
            BoundaryExposure.BoundedWhole,
            FlowExposure.Forward,
            selected);

        Assert.True(BoundaryFlowLaneBasis.TryReadUnitShapeBasis(
            wholeForwardShape,
            out var basis,
            out var exposure,
            out _,
            out var lanes));
        Assert.Equal(new BoundaryFlowExposure(BoundaryExposure.BoundedWhole, FlowExposure.Forward), exposure);
        Assert.Equal("SF", string.Join("|", lanes.Select(lane => lane.Label)));
        Assert.True(BoundedWholeSurface.TryFind(basis, out _, out var whole, out _, out _, out _));
        Assert.Equal(selected, whole);
        Assert.Equal(FlowExposure.Forward, FlowFaceSurface.ReadExposure(basis));
    }
}
