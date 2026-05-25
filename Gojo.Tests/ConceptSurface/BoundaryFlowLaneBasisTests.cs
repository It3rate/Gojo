using Gojo.ConceptSurface;
using Gojo.Core;
using Gojo.Structures;

namespace Gojo.Tests.ConceptSurface;

public sealed class BoundaryFlowLaneBasisTests
{
    [Theory]
    [MemberData(nameof(SelectedLaneBasisStates))]
    public void BoundaryFlowExposureDefinesLegalLaneBasis(
        BoundaryExposure boundary,
        FlowExposure flow,
        string expectedLabels)
    {
        var lanes = BoundaryFlowLaneBasis.LegalLanes(boundary, flow);

        Assert.Equal(expectedLabels, string.Join("|", lanes.Select(lane => lane.Label)));
    }

    [Fact]
    public void SingleLaneSurfacesCanInferTheOnlyOccupiedLane()
    {
        Assert.True(BoundaryFlowLaneBasis.TryInferSingleLane(
            BoundaryExposure.Positive,
            FlowExposure.Forward,
            out var positiveForward));
        Assert.Equal(BoundaryFlowLane.PositiveForward, positiveForward);

        Assert.True(BoundaryFlowLaneBasis.TryInferSingleLane(
            BoundaryExposure.Negative,
            FlowExposure.Inverse,
            out var negativeInverse));
        Assert.Equal(BoundaryFlowLane.NegativeInverse, negativeInverse);
    }

    [Fact]
    public void MultiLaneSurfaceDoesNotPretendAValueLaneWasChosen()
    {
        Assert.False(BoundaryFlowLaneBasis.TryInferSingleLane(
            BoundaryExposure.FullSigned,
            FlowExposure.Forward,
            out _));
        Assert.False(BoundaryFlowLaneBasis.TryInferSingleLane(
            BoundaryExposure.Negative,
            FlowExposure.Both,
            out _));
    }

    [Fact]
    public void BoundaryAndFlowDualsAreSeparateLaneReads()
    {
        Assert.Equal(BoundaryFlowLane.NegativeForward, BoundaryFlowLane.PositiveForward.DualBoundary());
        Assert.Equal(BoundaryFlowLane.PositiveInverse, BoundaryFlowLane.PositiveForward.DualFlow());
        Assert.Equal(BoundaryFlowLane.NegativeInverse, BoundaryFlowLane.PositiveForward.Dual());
        Assert.Equal(BoundaryFlowLane.PositiveForward, BoundaryFlowLane.NegativeInverse.Dual());
        Assert.Equal(BoundaryFlowLane.WholeNone, BoundaryFlowLane.WholeNone.Dual());
        Assert.Equal(FlowExposure.Inverse, FlowExposure.Forward.Dual());
        Assert.Equal(FlowExposure.Forward, FlowExposure.Inverse.Dual());
        Assert.Equal(FlowExposure.None, FlowExposure.None.Dual());
        Assert.Equal(FlowExposure.Both, FlowExposure.Both.Dual());
    }

    [Fact]
    public void FlowPhaseStepAdvancesAcrossBoundaryAndFlowLanes()
    {
        Assert.True(BoundaryFlowLane.PositiveForward.TryStepFlowPhase(out var first));
        Assert.Equal(BoundaryFlowLane.PositiveInverse, first);
        Assert.True(first.TryStepFlowPhase(out var second));
        Assert.Equal(BoundaryFlowLane.NegativeForward, second);
        Assert.True(second.TryStepFlowPhase(out var third));
        Assert.Equal(BoundaryFlowLane.NegativeInverse, third);
        Assert.True(third.TryStepFlowPhase(out var fourth));
        Assert.Equal(BoundaryFlowLane.PositiveForward, fourth);
        Assert.False(BoundaryFlowLane.WholeNone.TryStepFlowPhase(out _));
    }

    [Fact]
    public void FlowPhaseStepTransitionsAreInspectableStructure()
    {
        var transitions = BoundaryFlowLaneTransitionSurface.FlowPhaseStepTransitions(new Carrier());
        var group = Relation.OrderedGroup(new Carrier(), transitions);

        Assert.Equal(4, transitions.Length);
        Assert.True(BoundaryFlowLaneTransitionSurface.TryFindTransition(
            group,
            BoundaryFlowLane.PositiveForward,
            BoundaryFlowLane.PositiveInverse,
            out _));
        Assert.True(BoundaryFlowLaneTransitionSurface.TryFindTransition(
            group,
            BoundaryFlowLane.PositiveInverse,
            BoundaryFlowLane.NegativeForward,
            out _));
        Assert.True(BoundaryFlowLaneTransitionSurface.TryFindTransition(
            group,
            BoundaryFlowLane.NegativeForward,
            BoundaryFlowLane.NegativeInverse,
            out _));
        Assert.True(BoundaryFlowLaneTransitionSurface.TryFindTransition(
            group,
            BoundaryFlowLane.NegativeInverse,
            BoundaryFlowLane.PositiveForward,
            out _));
    }

    [Fact]
    public void LaneBasisSurfaceKeepsExplicitLegalLaneMarkers()
    {
        var selected = Relation.Segment(new Carrier(), -4, 4);
        var basis = BoundaryFlowLaneBasis.Create(
            new Carrier(),
            BoundaryExposure.FullSigned,
            FlowExposure.Both,
            selected);

        Assert.True(BoundaryFlowLaneBasis.TryRead(
            basis,
            out var exposure,
            out var readSelected,
            out var lanes));
        Assert.True(exposure.Boundary.IsFullSigned);
        Assert.True(exposure.Flow.IsBoth);
        Assert.Equal(selected, readSelected);
        Assert.Equal("PF|PI|NF|NI", string.Join("|", lanes.Select(lane => lane.Label)));
    }

    [Fact]
    public void LaneBasisLegalReadHonorsExplicitLaneEvidence()
    {
        var selected = Relation.Segment(new Carrier(), -4, 4);
        var boundaryFlowSurface = BoundaryFlowSurface.Create(
            new Carrier(),
            BoundaryExposure.FullSigned,
            FlowExposure.Forward,
            selected);
        Assert.True(BoundaryFlowSurface.TryRead(
            boundaryFlowSurface,
            out _,
            out _,
            out var boundaryEvidence,
            out var flowEvidence,
            out _));
        var positiveOnly = BoundaryFlowLaneSurface.Create(
            new Carrier(),
            BoundaryFlowLane.PositiveForward,
            boundaryEvidence,
            flowEvidence);
        var basis = Relation.OrderedGroup(
            new Carrier(),
            boundaryFlowSurface,
            selected,
            positiveOnly);

        Assert.True(BoundaryFlowLaneBasis.TryReadLegalLanes(
            basis,
            out var exposure,
            out var lanes));
        Assert.Equal(new BoundaryFlowExposure(BoundaryExposure.FullSigned, FlowExposure.Forward), exposure);
        Assert.Equal("PF", string.Join("|", lanes.Select(lane => lane.Label)));
        Assert.True(BoundaryFlowLaneBasis.Allows(basis, BoundaryFlowLane.PositiveForward));
        Assert.False(BoundaryFlowLaneBasis.Allows(basis, BoundaryFlowLane.NegativeForward));
    }

    [Fact]
    public void InferredValueUsesExplicitLaneEvidenceBeforeExposureRecipe()
    {
        var selected = Relation.Segment(new Carrier(), -4, 4);
        var boundaryFlowSurface = BoundaryFlowSurface.Create(
            new Carrier(),
            BoundaryExposure.FullSigned,
            FlowExposure.Forward,
            selected);
        Assert.True(BoundaryFlowSurface.TryRead(
            boundaryFlowSurface,
            out _,
            out _,
            out var boundaryEvidence,
            out var flowEvidence,
            out _));
        var positiveOnly = BoundaryFlowLaneSurface.Create(
            new Carrier(),
            BoundaryFlowLane.PositiveForward,
            boundaryEvidence,
            flowEvidence);
        var basis = Relation.OrderedGroup(
            new Carrier(),
            boundaryFlowSurface,
            selected,
            positiveOnly);
        var magnitudeCarrier = new Carrier();
        var magnitude = Relation.Point(magnitudeCarrier, 7);

        Assert.True(BoundaryFlowValueSurface.TryCreateInferred(
            new Carrier(),
            magnitude,
            basis,
            out var value));
        Assert.True(BoundaryFlowValueSurface.TryReadValue(value, basis, out var read));
        Assert.Equal(BoundaryFlowLane.PositiveForward, read.Lane);
        Assert.True(read.IsLegal);
    }

    [Fact]
    public void ValueBasisReadKeepsResolvedBasisAndVisibleLegalLanes()
    {
        var selected = Relation.Segment(new Carrier(), -4, 4);
        var boundaryFlowSurface = BoundaryFlowSurface.Create(
            new Carrier(),
            BoundaryExposure.FullSigned,
            FlowExposure.Forward,
            selected);
        Assert.True(BoundaryFlowSurface.TryRead(
            boundaryFlowSurface,
            out _,
            out _,
            out var boundaryEvidence,
            out var flowEvidence,
            out _));
        var positiveOnly = BoundaryFlowLaneSurface.Create(
            new Carrier(),
            BoundaryFlowLane.PositiveForward,
            boundaryEvidence,
            flowEvidence);
        var basis = Relation.OrderedGroup(
            new Carrier(),
            boundaryFlowSurface,
            selected,
            positiveOnly);
        var magnitudeCarrier = new Carrier();
        var value = BoundaryFlowValueSurface.Create(
            new Carrier(),
            Relation.Point(magnitudeCarrier, 7),
            BoundaryFlowLane.NegativeForward,
            basis,
            Relation.Point(Carrier.Unknown, 0));

        Assert.True(BoundaryFlowValueSurface.TryReadValueWithBasis(value, out var read));
        Assert.Equal(basis, read.LaneBasis);
        Assert.Equal(new BoundaryFlowExposure(BoundaryExposure.FullSigned, FlowExposure.Forward), read.Basis);
        Assert.Equal("PF", string.Join("|", read.LegalLanes.Select(lane => lane.Label)));
        Assert.Equal(BoundaryFlowLane.NegativeForward, read.Lane);
        Assert.False(read.IsLegal);
        Assert.Equal("-7f", read.Notation);
        Assert.Equal(read.ToCompactRead(), new BoundaryFlowValueRead(
            read.Magnitude,
            read.Lane,
            read.Basis,
            read.IsLegal,
            read.ResidueOrTension));
    }

    [Fact]
    public void LaneBasisCanAttachToUnitShapeWithoutCarrierAxis()
    {
        var unitCarrier = new Carrier();
        var selectedTickShape = UnitShape.ResolutionOf(unitCarrier.UnitShape, 4).Surface;
        var basis = BoundaryFlowLaneBasis.Create(
            new Carrier(),
            BoundaryExposure.BoundedWhole,
            FlowExposure.None,
            selectedTickShape);

        Assert.True(BoundaryFlowLaneBasis.TryRead(
            basis,
            out var exposure,
            out var selected,
            out var lanes));
        Assert.Equal(selectedTickShape, selected);
        Assert.True(exposure.Boundary.IsBoundedWhole);
        Assert.True(exposure.Flow.IsNone);
        Assert.Equal("S:None", string.Join("|", lanes.Select(lane => lane.Label)));
    }

    [Fact]
    public void LaneOnlyBasisCanReadWholeNoFlowValueWithoutOwningCarrier()
    {
        var whole = Relation.Segment(new Carrier(), 0, 100);
        var basis = BoundaryFlowLaneBasis.Create(
            new Carrier(),
            BoundaryExposure.BoundedWhole,
            FlowExposure.None,
            whole);
        var magnitudeCarrier = new Carrier();
        var value = BoundaryFlowValueSurface.Create(
            new Carrier(),
            magnitudeCarrier,
            2,
            BoundaryFlowLane.WholeNone,
            basis);

        Assert.True(BoundaryFlowValueSurface.TryReadValue(value, out var read));
        Assert.True(read.IsLegal);
        Assert.True(read.IsWholeBoundary);
        Assert.True(read.IsNoFlow);
        Assert.Equal(new CarrierLocation(magnitudeCarrier, 2), read.Magnitude);
        Assert.Equal("2s", read.Notation);

        Assert.False(CarrierAxisSurface.TryRead(basis, out _, out _, out _, out _, out _));
    }

    [Fact]
    public void UnitShapeDescriptorCanCarryLaneBasisAsTickCharacter()
    {
        var tickSource = Relation.Point(new Carrier(), 1);
        var tickBasis = BoundaryFlowLaneBasis.Create(
            new Carrier(),
            BoundaryExposure.Positive,
            FlowExposure.Forward,
            tickSource);
        var tickShape = UnitShape.DescribedAtom(tickBasis);

        var descriptor = Assert.Single(tickShape.DescriptorSurfaces);
        Assert.True(BoundaryFlowLaneBasis.TryRead(
            Assert.IsType<Relation>(descriptor),
            out var exposure,
            out var selected,
            out var lanes));
        Assert.Equal("P:F", exposure.Label);
        Assert.True(ConceptSurfaceReaderForTests.TryReadPoint(selected, out var selectedTick));
        Assert.Equal(1, selectedTick.Ticks);
        Assert.Equal("PF", string.Join("|", lanes.Select(lane => lane.Label)));
    }

    [Fact]
    public void CarrierUnitShapeCanSupplyPossibleLanesForCompleteValue()
    {
        var tickSource = Relation.Point(new Carrier(), 1);
        var possibleLanes = BoundaryFlowLaneBasis.Create(
            new Carrier(),
            BoundaryExposure.FullSigned,
            FlowExposure.Forward,
            tickSource);
        var signedTickShape = UnitShape.DescribedAtom(possibleLanes);
        var valueCarrier = new Carrier(signedTickShape);

        Assert.True(BoundaryFlowLaneBasis.TryReadUnitShapeBasis(
            valueCarrier.UnitShape,
            out var laneBasis,
            out var exposure,
            out _,
            out var lanes));
        Assert.True(BoundaryFlowLaneBasis.TryRead(laneBasis, out var basisExposure, out _, out _));
        Assert.Equal(exposure, basisExposure);
        Assert.Equal("PN:F", exposure.Label);
        Assert.Equal("PF|NF", string.Join("|", lanes.Select(lane => lane.Label)));

        Assert.True(BoundaryFlowValueSurface.TryCreateFromCarrierUnitShape(
            new Carrier(),
            valueCarrier,
            6,
            BoundaryFlowLane.NegativeForward,
            out var value));
        Assert.True(BoundaryFlowValueSurface.TryRead(
            value,
            out _,
            out _,
            out _,
            out var valueBasisSlot,
            out _));
        Assert.True(ConceptSurfaceReaderForTests.TryReadPoint(valueBasisSlot, out var basisSlot));
        Assert.True(basisSlot.Carrier.IsUnknown);
        Assert.Equal(0, basisSlot.Ticks);
        Assert.True(BoundaryFlowValueSurface.TryReadValue(value, out var read));
        Assert.True(read.IsLegal);
        Assert.Equal(exposure, read.Basis);
        Assert.Equal(BoundaryFlowLane.NegativeForward, read.Lane);
        Assert.Equal(-6, read.SignedTicks);
        Assert.Equal("-6f", read.Notation);
    }

    [Fact]
    public void UnitShapeBasisWinsOverExplicitValueWitness()
    {
        var tickSource = Relation.Point(new Carrier(), 1);
        var possibleLanes = BoundaryFlowLaneBasis.Create(
            new Carrier(),
            BoundaryExposure.FullSigned,
            FlowExposure.Forward,
            tickSource);
        var signedTickShape = UnitShape.DescribedAtom(possibleLanes);
        var valueCarrier = new Carrier(signedTickShape);
        var conflictingWitness = BoundaryFlowLaneBasis.Create(
            new Carrier(),
            BoundaryExposure.Positive,
            FlowExposure.Inverse,
            tickSource);
        var value = BoundaryFlowValueSurface.Create(
            new Carrier(),
            valueCarrier,
            6,
            BoundaryFlowLane.NegativeForward,
            conflictingWitness);

        Assert.True(BoundaryFlowValueSurface.TryReadValueWithBasis(
            value,
            out var read));
        Assert.Equal(possibleLanes, read.LaneBasis);
        Assert.Equal("PN:F", read.Basis.Label);
        Assert.True(read.IsLegal);
        Assert.Equal("-6f", read.Notation);
    }

    [Fact]
    public void ValueReadCanResolveMissingBasisFromCarrierUnitShape()
    {
        var tickSource = Relation.Point(new Carrier(), 1);
        var possibleLanes = BoundaryFlowLaneBasis.Create(
            new Carrier(),
            BoundaryExposure.FullSigned,
            FlowExposure.Forward,
            tickSource);
        var signedTickShape = UnitShape.DescribedAtom(possibleLanes);
        var valueCarrier = new Carrier(signedTickShape);
        var value = BoundaryFlowValueSurface.Create(
            new Carrier(),
            valueCarrier,
            6,
            BoundaryFlowLane.NegativeForward,
            Relation.Point(Carrier.Unknown, 0));

        Assert.True(BoundaryFlowValueSurface.TryReadValueWithBasis(
            value,
            out var read));
        Assert.True(BoundaryFlowLaneBasis.TryRead(read.LaneBasis, out var exposure, out _, out var lanes));
        Assert.Equal("PN:F", exposure.Label);
        Assert.Equal("PF|NF", string.Join("|", lanes.Select(lane => lane.Label)));
        Assert.True(read.IsLegal);
        Assert.Equal(BoundaryFlowLane.NegativeForward, read.Lane);
        Assert.Equal(-6, read.SignedTicks);
        Assert.Equal("-6f", read.Notation);
    }

    [Fact]
    public void ValueReadStillShowsIllegalOccupiedLaneAgainstUnitShapeBasis()
    {
        var tickSource = Relation.Point(new Carrier(), 1);
        var possibleLanes = BoundaryFlowLaneBasis.Create(
            new Carrier(),
            BoundaryExposure.FullSigned,
            FlowExposure.Forward,
            tickSource);
        var signedTickShape = UnitShape.DescribedAtom(possibleLanes);
        var valueCarrier = new Carrier(signedTickShape);
        var value = BoundaryFlowValueSurface.Create(
            new Carrier(),
            valueCarrier,
            6,
            BoundaryFlowLane.NegativeInverse,
            Relation.Point(Carrier.Unknown, 0));

        Assert.True(BoundaryFlowValueSurface.TryReadValue(value, out var read));
        Assert.False(read.IsLegal);
        Assert.Equal(BoundaryFlowLane.NegativeInverse, read.Lane);
        Assert.Equal("-6i", read.Notation);
    }

    [Fact]
    public void PlainCarrierUnitShapeDefaultsToWholeNoFlowBasis()
    {
        var valueCarrier = new Carrier();
        var value = BoundaryFlowValueSurface.Create(
            new Carrier(),
            valueCarrier,
            6,
            BoundaryFlowLane.PositiveForward,
            Relation.Point(Carrier.Unknown, 0));

        Assert.True(BoundaryFlowLaneBasis.TryReadUnitShapeBasis(
            valueCarrier.UnitShape,
            out var basis,
            out var exposure,
            out _,
            out var lanes));
        Assert.True(BoundaryFlowLaneBasis.TryRead(basis, out _, out _, out _));
        Assert.Equal("S:None", exposure.Label);
        Assert.Equal("S:None", string.Join("|", lanes.Select(lane => lane.Label)));

        Assert.True(BoundaryFlowValueSurface.TryReadValueWithBasis(value, out var read));
        Assert.False(read.IsLegal);
        Assert.Equal(BoundaryFlowLane.PositiveForward, read.Lane);
    }

    [Fact]
    public void DefaultWholeNoFlowValueReadsAsLegalOnPlainCarrier()
    {
        var valueCarrier = new Carrier();
        var value = BoundaryFlowValueSurface.CreateOccupied(
            new Carrier(),
            valueCarrier,
            6,
            BoundaryFlowLane.WholeNone);

        Assert.True(BoundaryFlowValueSurface.TryReadValue(value, out var read));
        Assert.True(read.IsLegal);
        Assert.Equal(BoundaryFlowLane.WholeNone, read.Lane);
        Assert.Equal("6s", read.Notation);
    }

    [Fact]
    public void CarrierUnitShapeRejectsOccupiedLaneOutsidePossibleLanes()
    {
        var tickSource = Relation.Point(new Carrier(), 1);
        var possibleLanes = BoundaryFlowLaneBasis.Create(
            new Carrier(),
            BoundaryExposure.FullSigned,
            FlowExposure.Forward,
            tickSource);
        var signedTickShape = UnitShape.DescribedAtom(possibleLanes);
        var valueCarrier = new Carrier(signedTickShape);

        Assert.False(BoundaryFlowValueSurface.TryCreateFromCarrierUnitShape(
            new Carrier(),
            valueCarrier,
            6,
            BoundaryFlowLane.NegativeInverse,
            out _));
    }

    [Fact]
    public void CarrierWithoutLaneDescriptorUsesDefaultWholeNoFlowBasis()
    {
        var valueCarrier = new Carrier();

        Assert.True(BoundaryFlowLaneBasis.TryReadUnitShapeBasis(
            valueCarrier.UnitShape,
            out var basis,
            out var exposure,
            out _,
            out var lanes));
        Assert.True(BoundaryFlowLaneBasis.TryRead(basis, out _, out _, out _));
        Assert.Equal("S:None", exposure.Label);
        Assert.Equal("S:None", string.Join("|", lanes.Select(lane => lane.Label)));
        Assert.False(BoundaryFlowValueSurface.TryCreateFromCarrierUnitShape(
            new Carrier(),
            valueCarrier,
            6,
            BoundaryFlowLane.PositiveForward,
            out _));
        Assert.True(BoundaryFlowValueSurface.TryCreateFromCarrierUnitShape(
            new Carrier(),
            valueCarrier,
            6,
            BoundaryFlowLane.WholeNone,
            out var value));
        Assert.True(BoundaryFlowValueSurface.TryReadValue(value, out var read));
        Assert.True(read.IsLegal);
        Assert.Equal("6s", read.Notation);
    }

    [Fact]
    public void CarrierAxisSurfaceCanExposeLegalLanesWithoutDuplicatingValues()
    {
        var carrier = new Carrier();
        var axis = CarrierAxisSurface.Create(
            new Carrier(),
            carrier,
            BoundaryExposure.Negative,
            FlowExposure.Both);

        Assert.True(CarrierAxisSurface.TryReadLegalLanes(axis, out var lanes));
        Assert.Equal("NF|NI", string.Join("|", lanes.Select(lane => lane.Label)));
    }

    [Fact]
    public void ValueSurfaceKeepsMagnitudeSeparateFromOccupiedLane()
    {
        var carrier = new Carrier();
        var axis = CarrierAxisSurface.Create(
            new Carrier(),
            carrier,
            BoundaryExposure.FullSigned,
            FlowExposure.Both);
        var value = BoundaryFlowValueSurface.Create(
            new Carrier(),
            carrier,
            5,
            BoundaryFlowLane.NegativeInverse,
            axis);

        Assert.True(BoundaryFlowValueSurface.TryRead(
            value,
            out var magnitude,
            out var lane,
            out _,
            out var basis,
            out _));
        Assert.Equal(new CarrierLocation(carrier, 5), magnitude);
        Assert.Equal(BoundaryFlowLane.NegativeInverse, lane);
        Assert.Equal(axis, basis);

        Assert.True(BoundaryFlowValueSurface.TryReadOccupation(
            value,
            axis,
            out _,
            out _,
            out var isLegal));
        Assert.True(isLegal);
    }

    [Fact]
    public void ValueSurfaceCanShowLaneTensionAgainstItsBasis()
    {
        var carrier = new Carrier();
        var positiveForwardAxis = CarrierAxisSurface.PositiveUnit(new Carrier(), carrier);
        var illegalValue = BoundaryFlowValueSurface.Create(
            new Carrier(),
            carrier,
            5,
            BoundaryFlowLane.NegativeInverse,
            positiveForwardAxis);

        Assert.True(BoundaryFlowValueSurface.TryReadOccupation(
            illegalValue,
            positiveForwardAxis,
            out var magnitude,
            out var lane,
            out var isLegal));
        Assert.Equal(new CarrierLocation(carrier, 5), magnitude);
        Assert.Equal(BoundaryFlowLane.NegativeInverse, lane);
        Assert.False(isLegal);
    }

    [Fact]
    public void ValueReadProjectsLaneIntoSignedFlowNotation()
    {
        var carrier = new Carrier();
        var axis = CarrierAxisSurface.SignedBothFlows(new Carrier(), carrier);
        var value = BoundaryFlowValueSurface.Create(
            new Carrier(),
            carrier,
            5,
            BoundaryFlowLane.NegativeInverse,
            axis);

        Assert.True(BoundaryFlowValueSurface.TryReadValue(value, out var read));
        Assert.True(read.IsLegal);
        Assert.True(read.IsInverseFlow);
        Assert.False(read.IsForwardFlow);
        Assert.Equal(-5, read.SignedTicks);
        Assert.Equal("-5i", read.Notation);
    }

    [Fact]
    public void SingleLaneBasisCanCreateInferredValueSurface()
    {
        var carrier = new Carrier();
        var axis = CarrierAxisSurface.PositiveUnit(new Carrier(), carrier);
        var magnitude = Relation.Point(carrier, 7);

        Assert.True(BoundaryFlowValueSurface.TryCreateInferred(
            new Carrier(),
            magnitude,
            axis,
            out var value));
        Assert.True(BoundaryFlowValueSurface.TryReadValue(value, out var read));
        Assert.Equal(BoundaryFlowLane.PositiveForward, read.Lane);
        Assert.Equal("7f", read.Notation);
    }

    [Fact]
    public void MultiLaneBasisDoesNotInferValueLane()
    {
        var carrier = new Carrier();
        var axis = CarrierAxisSurface.SignedForward(new Carrier(), carrier);
        var magnitude = Relation.Point(carrier, 7);

        Assert.False(BoundaryFlowValueSurface.TryCreateInferred(
            new Carrier(),
            magnitude,
            axis,
            out _));
    }

    [Fact]
    public void SignedForwardSurfaceCanSettleAcrossPositiveAndNegativeLanes()
    {
        var carrier = new Carrier();
        var axis = CarrierAxisSurface.SignedForward(new Carrier(), carrier);
        var value = BoundaryFlowValueSurface.Create(
            new Carrier(),
            carrier,
            5,
            BoundaryFlowLane.PositiveForward,
            axis);

        Assert.True(BoundaryFlowValueSettlement.TryApplySignedDelta(
            value,
            axis,
            -8,
            out var settled));
        Assert.True(BoundaryFlowValueSurface.TryReadValue(settled, out var read));
        Assert.True(read.IsLegal);
        Assert.Equal(BoundaryFlowLane.NegativeForward, read.Lane);
        Assert.Equal(3, read.Magnitude.Ticks);
        Assert.Equal(-3, read.SignedTicks);
        Assert.Equal("-3f", read.Notation);
        Assert.True(ConceptSurfaceReaderForTests.TryReadPoint(read.ResidueOrTension, out var residue));
        Assert.Equal(0, residue.Ticks);
    }

    [Fact]
    public void PositiveForwardSurfaceLeavesResidueWhenDeltaWouldNeedNegativeLane()
    {
        var carrier = new Carrier();
        var axis = CarrierAxisSurface.PositiveUnit(new Carrier(), carrier);
        var value = BoundaryFlowValueSurface.Create(
            new Carrier(),
            carrier,
            5,
            BoundaryFlowLane.PositiveForward,
            axis);

        Assert.True(BoundaryFlowValueSettlement.TryApplySignedDelta(
            value,
            axis,
            -8,
            out var settled));
        Assert.True(BoundaryFlowValueSurface.TryReadValue(settled, out var read));
        Assert.True(read.IsLegal);
        Assert.Equal(BoundaryFlowLane.PositiveForward, read.Lane);
        Assert.Equal(0, read.Magnitude.Ticks);
        Assert.True(ConceptSurfaceReaderForTests.TryReadPoint(read.ResidueOrTension, out var residue));
        Assert.Equal(-3, residue.Ticks);
    }

    [Fact]
    public void FullSignedLaneBasisWithoutTransitionLeavesResidueOnCrossing()
    {
        var carrier = new Carrier();
        var basis = BoundaryFlowLaneBasis.Create(
            new Carrier(),
            BoundaryExposure.FullSigned,
            FlowExposure.Forward,
            carrier);
        var value = BoundaryFlowValueSurface.Create(
            new Carrier(),
            carrier,
            5,
            BoundaryFlowLane.PositiveForward,
            basis);

        Assert.True(BoundaryFlowValueSettlement.TryApplySignedDelta(
            value,
            basis,
            -8,
            out var settled));
        Assert.True(BoundaryFlowValueSurface.TryReadValue(settled, out var read));
        Assert.True(read.IsLegal);
        Assert.Equal(BoundaryFlowLane.PositiveForward, read.Lane);
        Assert.Equal(0, read.Magnitude.Ticks);
        Assert.True(ConceptSurfaceReaderForTests.TryReadPoint(read.ResidueOrTension, out var residue));
        Assert.Equal(-3, residue.Ticks);
    }

    [Fact]
    public void LaneTransitionSurfaceIsDirected()
    {
        var transition = BoundaryFlowLaneTransitionSurface.Create(
            new Carrier(),
            BoundaryFlowLane.PositiveForward,
            BoundaryFlowLane.NegativeForward);
        var host = Relation.OrderedGroup(new Carrier(), transition);

        Assert.True(BoundaryFlowLaneTransitionSurface.TryFindTransition(
            host,
            BoundaryFlowLane.PositiveForward,
            BoundaryFlowLane.NegativeForward,
            out var read));
        Assert.Equal(BoundaryFlowLane.PositiveForward, read.Source);
        Assert.Equal(BoundaryFlowLane.NegativeForward, read.Target);
        Assert.False(BoundaryFlowLaneTransitionSurface.TryFindTransition(
            host,
            BoundaryFlowLane.NegativeForward,
            BoundaryFlowLane.PositiveForward,
            out _));
    }

    [Fact]
    public void BoundaryFlowUnitShapeSignedForwardCarriesBasisAndTransitionPair()
    {
        var signedTickShape = BoundaryFlowUnitShape.SignedForward();

        Assert.True(BoundaryFlowLaneBasis.TryReadUnitShapeBasis(
            signedTickShape,
            out _,
            out var exposure,
            out _,
            out var lanes));
        Assert.Equal(new BoundaryFlowExposure(BoundaryExposure.FullSigned, FlowExposure.Forward), exposure);
        Assert.Equal(
            [BoundaryFlowLane.PositiveForward, BoundaryFlowLane.NegativeForward],
            lanes);
        Assert.True(BoundaryFlowLaneTransitionSurface.TryFindUnitShapeTransition(
            signedTickShape,
            BoundaryFlowLane.PositiveForward,
            BoundaryFlowLane.NegativeForward,
            out _));
        Assert.True(BoundaryFlowLaneTransitionSurface.TryFindUnitShapeTransition(
            signedTickShape,
            BoundaryFlowLane.NegativeForward,
            BoundaryFlowLane.PositiveForward,
            out _));
    }

    [Fact]
    public void UnitShapeLaneTransitionsAllowSignedDeltaWithoutValueBasis()
    {
        var signedTickShape = SignedForwardUnitShapeWithTransitions();
        var firstCarrier = new Carrier(signedTickShape);
        var secondCarrier = new Carrier(signedTickShape);
        var firstValue = BoundaryFlowValueSurface.CreateOccupied(
            new Carrier(),
            firstCarrier,
            5,
            BoundaryFlowLane.PositiveForward);
        var secondValue = BoundaryFlowValueSurface.CreateOccupied(
            new Carrier(),
            secondCarrier,
            2,
            BoundaryFlowLane.NegativeForward);

        Assert.True(BoundaryFlowLaneTransitionSurface.TryFindUnitShapeTransition(
            firstCarrier.UnitShape,
            BoundaryFlowLane.PositiveForward,
            BoundaryFlowLane.NegativeForward,
            out _));
        Assert.True(BoundaryFlowValueSettlement.TryApplySignedDelta(
            firstValue,
            -8,
            out var firstSettled));
        Assert.True(BoundaryFlowValueSettlement.TryApplySignedDelta(
            secondValue,
            5,
            out var secondSettled));
        Assert.True(BoundaryFlowValueSurface.TryReadValue(firstSettled, out var firstRead));
        Assert.True(BoundaryFlowValueSurface.TryReadValue(secondSettled, out var secondRead));
        Assert.Equal(BoundaryFlowLane.NegativeForward, firstRead.Lane);
        Assert.Equal(3, firstRead.Magnitude.Ticks);
        Assert.Equal("-3f", firstRead.Notation);
        Assert.Equal(BoundaryFlowLane.PositiveForward, secondRead.Lane);
        Assert.Equal(3, secondRead.Magnitude.Ticks);
        Assert.Equal("3f", secondRead.Notation);
    }

    [Fact]
    public void OneWayUnitShapeLaneTransitionOnlySettlesItsDirection()
    {
        var signedTickShape = SignedForwardUnitShapeWithTransitions(includeReverse: false);
        var firstCarrier = new Carrier(signedTickShape);
        var secondCarrier = new Carrier(signedTickShape);
        var firstValue = BoundaryFlowValueSurface.CreateOccupied(
            new Carrier(),
            firstCarrier,
            5,
            BoundaryFlowLane.PositiveForward);
        var secondValue = BoundaryFlowValueSurface.CreateOccupied(
            new Carrier(),
            secondCarrier,
            2,
            BoundaryFlowLane.NegativeForward);

        Assert.True(BoundaryFlowValueSettlement.TryApplySignedDelta(
            firstValue,
            -8,
            out var firstSettled));
        Assert.True(BoundaryFlowValueSettlement.TryApplySignedDelta(
            secondValue,
            5,
            out var secondSettled));
        Assert.True(BoundaryFlowValueSurface.TryReadValue(firstSettled, out var firstRead));
        Assert.True(BoundaryFlowValueSurface.TryReadValue(secondSettled, out var secondRead));
        Assert.Equal(BoundaryFlowLane.NegativeForward, firstRead.Lane);
        Assert.Equal(3, firstRead.Magnitude.Ticks);
        Assert.True(ConceptSurfaceReaderForTests.TryReadPoint(firstRead.ResidueOrTension, out var firstResidue));
        Assert.Equal(0, firstResidue.Ticks);
        Assert.Equal(BoundaryFlowLane.NegativeForward, secondRead.Lane);
        Assert.Equal(0, secondRead.Magnitude.Ticks);
        Assert.True(ConceptSurfaceReaderForTests.TryReadPoint(secondRead.ResidueOrTension, out var secondResidue));
        Assert.Equal(3, secondResidue.Ticks);
    }

    [Fact]
    public void RouteCanResolveTargetBasisFromCarrierUnitShape()
    {
        var source = new Carrier();
        var target = new Carrier(SignedForwardUnitShapeWithTransitions());
        var sourceAxis = CarrierAxisSurface.SignedForward(new Carrier(), source);
        var sourceValue = BoundaryFlowValueSurface.Create(
            new Carrier(),
            source,
            5,
            BoundaryFlowLane.NegativeForward,
            sourceAxis);
        var route = new Relation(
            Relation.Segment(source, -10, 10),
            Relation.Segment(target, -100, 100));

        Assert.True(BoundaryFlowValueRoute.TryProject(
            sourceValue,
            route,
            target,
            out var routeSurface));
        Assert.True(BoundaryFlowValueRoute.TryRead(
            routeSurface,
            out _,
            out _,
            out var targetBasis,
            out var targetValue,
            out _));
        Assert.True(BoundaryFlowLaneBasis.TryRead(targetBasis, out var exposure, out _, out _));
        Assert.Equal(new BoundaryFlowExposure(BoundaryExposure.FullSigned, FlowExposure.Forward), exposure);
        Assert.True(BoundaryFlowValueSurface.TryReadValue(targetValue, out var targetRead));
        Assert.True(targetRead.IsLegal);
        Assert.Equal(BoundaryFlowLane.NegativeForward, targetRead.Lane);
        Assert.Equal("-50f", targetRead.Notation);
    }

    [Fact]
    public void RouteProjectsSignedForwardValueAcrossCarrierBases()
    {
        var source = new Carrier();
        var target = new Carrier();
        var sourceAxis = CarrierAxisSurface.SignedForward(new Carrier(), source);
        var targetAxis = CarrierAxisSurface.SignedForward(new Carrier(), target);
        var sourceValue = BoundaryFlowValueSurface.Create(
            new Carrier(),
            source,
            5,
            BoundaryFlowLane.NegativeForward,
            sourceAxis);
        var route = new Relation(
            Relation.Segment(source, -10, 10),
            Relation.Segment(target, -100, 100));

        Assert.True(BoundaryFlowValueRoute.TryProject(
            sourceValue,
            route,
            targetAxis,
            target,
            out var routeSurface));
        Assert.True(BoundaryFlowValueRoute.TryRead(
            routeSurface,
            out var readSourceValue,
            out var readRoute,
            out var readTargetAxis,
            out var targetValue,
            out var routeResidue));
        Assert.Equal(sourceValue, readSourceValue);
        Assert.Equal(route, readRoute);
        Assert.Equal(targetAxis, readTargetAxis);
        Assert.True(BoundaryFlowValueSurface.TryReadValue(targetValue, out var targetRead));
        Assert.True(targetRead.IsLegal);
        Assert.Equal(BoundaryFlowLane.NegativeForward, targetRead.Lane);
        Assert.Equal(50, targetRead.Magnitude.Ticks);
        Assert.Equal(-50, targetRead.SignedTicks);
        Assert.Equal("-50f", targetRead.Notation);
        Assert.Equal(targetRead.ResidueOrTension, routeResidue);
        Assert.True(ConceptSurfaceReaderForTests.TryReadPoint(routeResidue, out var residue));
        Assert.Equal(0, residue.Ticks);
    }

    [Fact]
    public void RouteKeepsRejectedInverseFlowVisibleOnForwardOnlyTarget()
    {
        var source = new Carrier();
        var target = new Carrier();
        var sourceAxis = CarrierAxisSurface.SignedBothFlows(new Carrier(), source);
        var targetAxis = CarrierAxisSurface.SignedForward(new Carrier(), target);
        var sourceValue = BoundaryFlowValueSurface.Create(
            new Carrier(),
            source,
            5,
            BoundaryFlowLane.NegativeInverse,
            sourceAxis);
        var route = new Relation(
            Relation.Segment(source, -10, 10),
            Relation.Segment(target, -10, 10));

        Assert.True(BoundaryFlowValueRoute.TryProject(
            sourceValue,
            route,
            targetAxis,
            target,
            out var routeSurface));
        Assert.True(BoundaryFlowValueRoute.TryReadTargetValue(routeSurface, out var targetRead));
        Assert.False(targetRead.IsLegal);
        Assert.Equal(BoundaryFlowLane.NegativeInverse, targetRead.Lane);
        Assert.Equal(5, targetRead.Magnitude.Ticks);
        Assert.Equal("-5i", targetRead.Notation);
        Assert.True(ConceptSurfaceReaderForTests.TryReadPoint(targetRead.ResidueOrTension, out var residue));
        Assert.Equal(-5, residue.Ticks);
    }

    [Fact]
    public void RouteScalesPositiveForwardValueIntoPositiveForwardTarget()
    {
        var source = new Carrier();
        var target = new Carrier();
        var sourceAxis = CarrierAxisSurface.PositiveUnit(new Carrier(), source);
        var targetAxis = CarrierAxisSurface.PositiveUnit(new Carrier(), target);
        var sourceValue = BoundaryFlowValueSurface.Create(
            new Carrier(),
            source,
            5,
            BoundaryFlowLane.PositiveForward,
            sourceAxis);
        var route = new Relation(
            Relation.Segment(source, 0, 10),
            Relation.Segment(target, 0, 100));

        Assert.True(BoundaryFlowValueRoute.TryProject(
            sourceValue,
            route,
            targetAxis,
            target,
            out var routeSurface));
        Assert.True(BoundaryFlowValueRoute.TryReadTargetValue(routeSurface, out var targetRead));
        Assert.True(targetRead.IsLegal);
        Assert.Equal(BoundaryFlowLane.PositiveForward, targetRead.Lane);
        Assert.Equal(50, targetRead.Magnitude.Ticks);
        Assert.Equal("50f", targetRead.Notation);
    }

    [Fact]
    public void TransferFullyAcceptsLegalProjectedLane()
    {
        var source = new Carrier();
        var target = new Carrier();
        var sourceAxis = CarrierAxisSurface.SignedForward(new Carrier(), source);
        var targetAxis = CarrierAxisSurface.SignedForward(new Carrier(), target);
        var sourceValue = BoundaryFlowValueSurface.Create(
            new Carrier(),
            source,
            5,
            BoundaryFlowLane.NegativeForward,
            sourceAxis);
        var route = new Relation(
            Relation.Segment(source, -10, 10),
            Relation.Segment(target, -100, 100));

        Assert.True(BoundaryFlowValueRoute.TryProject(
            sourceValue,
            route,
            targetAxis,
            target,
            out var routeSurface));
        Assert.True(BoundaryFlowValueTransfer.TryAccept(routeSurface, out var transfer));
        Assert.True(BoundaryFlowValueTransfer.TryReadSummary(transfer, out var read));
        Assert.True(read.IsFullyAccepted);
        Assert.False(read.IsPartiallyAccepted);
        Assert.Equal(BoundaryFlowLane.NegativeForward, read.Accepted.Lane);
        Assert.Equal(50, read.Accepted.Magnitude.Ticks);
        Assert.Equal("-50f", read.Accepted.Notation);
        Assert.Equal(0, read.Blocked.Magnitude.Ticks);
    }

    [Fact]
    public void TransferBlocksIllegalProjectedLaneWithoutInventingAcceptedValue()
    {
        var source = new Carrier();
        var target = new Carrier();
        var sourceAxis = CarrierAxisSurface.SignedBothFlows(new Carrier(), source);
        var targetAxis = CarrierAxisSurface.SignedForward(new Carrier(), target);
        var sourceValue = BoundaryFlowValueSurface.Create(
            new Carrier(),
            source,
            5,
            BoundaryFlowLane.NegativeInverse,
            sourceAxis);
        var route = new Relation(
            Relation.Segment(source, -10, 10),
            Relation.Segment(target, -10, 10));

        Assert.True(BoundaryFlowValueRoute.TryProject(
            sourceValue,
            route,
            targetAxis,
            target,
            out var routeSurface));
        Assert.True(BoundaryFlowValueTransfer.TryAccept(routeSurface, out var transfer));
        Assert.False(BoundaryFlowValueTransfer.TryReadAcceptedValue(transfer, out _));
        Assert.True(BoundaryFlowValueTransfer.TryReadSummary(transfer, out var read));
        Assert.True(read.IsFullyBlocked);
        Assert.False(read.HasAccepted);
        Assert.False(read.Blocked.IsLegal);
        Assert.Equal(BoundaryFlowLane.NegativeInverse, read.Blocked.Lane);
        Assert.Equal(5, read.Blocked.Magnitude.Ticks);
        Assert.Equal("-5i", read.Blocked.Notation);
        Assert.True(ConceptSurfaceReaderForTests.TryReadPoint(read.Blocked.ResidueOrTension, out var residue));
        Assert.Equal(-5, residue.Ticks);
    }

    [Fact]
    public void TransferPartiallyAcceptsCapacityLimitedProjection()
    {
        var source = new Carrier();
        var target = new Carrier();
        var sourceAxis = CarrierAxisSurface.PositiveUnit(new Carrier(), source);
        var targetAxis = CarrierAxisSurface.PositiveUnit(new Carrier(), target);
        var sourceValue = BoundaryFlowValueSurface.Create(
            new Carrier(),
            source,
            5,
            BoundaryFlowLane.PositiveForward,
            sourceAxis);
        var route = new Relation(
            Relation.Segment(source, 0, 10),
            Relation.Segment(target, 0, 100));
        var capacity = Relation.Point(target, 30);

        Assert.True(BoundaryFlowValueRoute.TryProject(
            sourceValue,
            route,
            targetAxis,
            target,
            out var routeSurface));
        Assert.True(BoundaryFlowValueTransfer.TryAccept(
            routeSurface,
            capacity,
            out var transfer));
        Assert.True(BoundaryFlowValueTransfer.TryReadSummary(transfer, out var read));
        Assert.True(read.IsPartiallyAccepted);
        Assert.Equal(BoundaryFlowLane.PositiveForward, read.Accepted.Lane);
        Assert.Equal(30, read.Accepted.Magnitude.Ticks);
        Assert.Equal("30f", read.Accepted.Notation);
        Assert.Equal(BoundaryFlowLane.PositiveForward, read.Blocked.Lane);
        Assert.Equal(20, read.Blocked.Magnitude.Ticks);
        Assert.Equal("20f", read.Blocked.Notation);
        Assert.True(ConceptSurfaceReaderForTests.TryReadPoint(read.Blocked.ResidueOrTension, out var residue));
        Assert.Equal(20, residue.Ticks);
    }

    [Fact]
    public void TargetSettlementAccumulatesAcceptedTransferAndWritesTimeline()
    {
        var source = new Carrier();
        var target = new Carrier();
        var sourceAxis = CarrierAxisSurface.PositiveUnit(new Carrier(), source);
        var targetAxis = CarrierAxisSurface.PositiveUnit(new Carrier(), target);
        var sourceValue = BoundaryFlowValueSurface.Create(
            new Carrier(),
            source,
            3,
            BoundaryFlowLane.PositiveForward,
            sourceAxis);
        var targetBefore = BoundaryFlowValueSurface.Create(
            new Carrier(),
            target,
            20,
            BoundaryFlowLane.PositiveForward,
            targetAxis);
        var route = new Relation(
            Relation.Segment(source, 0, 10),
            Relation.Segment(target, 0, 100));

        Assert.True(BoundaryFlowValueRoute.TryProject(
            sourceValue,
            route,
            targetAxis,
            target,
            out var routeSurface));
        Assert.True(BoundaryFlowValueTransfer.TryAccept(routeSurface, out var transfer));
        Assert.True(BoundaryFlowTargetSettlement.TrySettle(
            targetBefore,
            transfer,
            out var settlement));
        Assert.True(BoundaryFlowTargetSettlement.TryRead(
            settlement,
            out var readBefore,
            out var readTransfer,
            out var readAfter,
            out var residue,
            out var timeline));
        Assert.Equal(targetBefore, readBefore);
        Assert.Equal(transfer, readTransfer);
        Assert.True(BoundaryFlowValueSurface.TryReadValue(readAfter, out var afterRead));
        Assert.True(afterRead.IsLegal);
        Assert.Equal(BoundaryFlowLane.PositiveForward, afterRead.Lane);
        Assert.Equal(50, afterRead.Magnitude.Ticks);
        Assert.Equal("50f", afterRead.Notation);
        Assert.True(ConceptSurfaceReaderForTests.TryReadPoint(residue, out var residuePoint));
        Assert.Equal(0, residuePoint.Ticks);

        Assert.True(TimeStepSequence.TryRead(timeline, out _, out var initial, out var steps));
        Assert.Equal(targetBefore, initial);
        Assert.Single(steps);
        Assert.True(TimeStepSequence.TryReadStep(
            steps[0],
            out _,
            out var start,
            out var end,
            out var stepBefore,
            out var transition,
            out var stepAfter,
            out var stepResidue));
        Assert.Equal(0, start.Ticks);
        Assert.Equal(1, end.Ticks);
        Assert.Equal(targetBefore, stepBefore);
        Assert.Equal(transfer, transition);
        Assert.Equal(readAfter, stepAfter);
        Assert.Equal(residue, stepResidue);
    }

    [Fact]
    public void TargetSettlementKeepsBeforeValueWhenTransferIsBlocked()
    {
        var source = new Carrier();
        var target = new Carrier();
        var sourceAxis = CarrierAxisSurface.SignedBothFlows(new Carrier(), source);
        var targetAxis = CarrierAxisSurface.SignedForward(new Carrier(), target);
        var sourceValue = BoundaryFlowValueSurface.Create(
            new Carrier(),
            source,
            5,
            BoundaryFlowLane.NegativeInverse,
            sourceAxis);
        var targetBefore = BoundaryFlowValueSurface.Create(
            new Carrier(),
            target,
            20,
            BoundaryFlowLane.PositiveForward,
            targetAxis);
        var route = new Relation(
            Relation.Segment(source, -10, 10),
            Relation.Segment(target, -10, 10));

        Assert.True(BoundaryFlowValueRoute.TryProject(
            sourceValue,
            route,
            targetAxis,
            target,
            out var routeSurface));
        Assert.True(BoundaryFlowValueTransfer.TryAccept(routeSurface, out var transfer));
        Assert.True(BoundaryFlowTargetSettlement.TrySettle(
            targetBefore,
            transfer,
            out var settlement));
        Assert.True(BoundaryFlowTargetSettlement.TryRead(
            settlement,
            out _,
            out _,
            out var after,
            out var residue,
            out _));
        Assert.Equal(targetBefore, after);
        Assert.True(BoundaryFlowValueSurface.TryReadValue(after, out var afterRead));
        Assert.Equal(20, afterRead.Magnitude.Ticks);
        Assert.Equal("20f", afterRead.Notation);
        Assert.True(BoundaryFlowValueSurface.TryReadValue(Assert.IsType<Relation>(residue), out var blockedRead));
        Assert.False(blockedRead.IsLegal);
        Assert.Equal(BoundaryFlowLane.NegativeInverse, blockedRead.Lane);
        Assert.Equal(5, blockedRead.Magnitude.Ticks);
    }

    [Fact]
    public void TargetSettlementCanCrossSignedLaneWhenAcceptedTransferOpposesBefore()
    {
        var source = new Carrier();
        var target = new Carrier();
        var sourceAxis = CarrierAxisSurface.SignedForward(new Carrier(), source);
        var targetAxis = CarrierAxisSurface.SignedForward(new Carrier(), target);
        var sourceValue = BoundaryFlowValueSurface.Create(
            new Carrier(),
            source,
            5,
            BoundaryFlowLane.NegativeForward,
            sourceAxis);
        var targetBefore = BoundaryFlowValueSurface.Create(
            new Carrier(),
            target,
            20,
            BoundaryFlowLane.PositiveForward,
            targetAxis);
        var route = new Relation(
            Relation.Segment(source, -10, 10),
            Relation.Segment(target, -100, 100));

        Assert.True(BoundaryFlowValueRoute.TryProject(
            sourceValue,
            route,
            targetAxis,
            target,
            out var routeSurface));
        Assert.True(BoundaryFlowValueTransfer.TryAccept(routeSurface, out var transfer));
        Assert.True(BoundaryFlowTargetSettlement.TrySettle(
            targetBefore,
            transfer,
            out var settlement));
        Assert.True(BoundaryFlowTargetSettlement.TryReadAfter(settlement, out var afterRead));
        Assert.True(afterRead.IsLegal);
        Assert.Equal(BoundaryFlowLane.NegativeForward, afterRead.Lane);
        Assert.Equal(30, afterRead.Magnitude.Ticks);
        Assert.Equal(-30, afterRead.SignedTicks);
        Assert.Equal("-30f", afterRead.Notation);
    }

    [Fact]
    public void TargetSettlementUsesUnitShapeLaneTransitionsForOccupiedTarget()
    {
        var source = new Carrier();
        var target = new Carrier(SignedForwardUnitShapeWithTransitions());
        var sourceAxis = CarrierAxisSurface.SignedForward(new Carrier(), source);
        var sourceValue = BoundaryFlowValueSurface.Create(
            new Carrier(),
            source,
            5,
            BoundaryFlowLane.NegativeForward,
            sourceAxis);
        var targetBefore = BoundaryFlowValueSurface.CreateOccupied(
            new Carrier(),
            target,
            20,
            BoundaryFlowLane.PositiveForward);
        var route = new Relation(
            Relation.Segment(source, -10, 10),
            Relation.Segment(target, -100, 100));

        Assert.True(BoundaryFlowValueRoute.TryProject(
            sourceValue,
            route,
            target,
            out var routeSurface));
        Assert.True(BoundaryFlowValueTransfer.TryAccept(routeSurface, out var transfer));
        Assert.True(BoundaryFlowTargetSettlement.TrySettle(
            targetBefore,
            transfer,
            out var settlement));
        Assert.True(BoundaryFlowTargetSettlement.TryReadAfter(settlement, out var afterRead));
        Assert.True(afterRead.IsLegal);
        Assert.Equal(BoundaryFlowLane.NegativeForward, afterRead.Lane);
        Assert.Equal(30, afterRead.Magnitude.Ticks);
        Assert.Equal("-30f", afterRead.Notation);
    }

    [Fact]
    public void SettlementRunChainsAcceptedTransfersIntoOneTimeline()
    {
        var target = new Carrier();
        var targetAxis = CarrierAxisSurface.PositiveUnit(new Carrier(), target);
        var targetBefore = BoundaryFlowValueSurface.Create(
            new Carrier(),
            target,
            10,
            BoundaryFlowLane.PositiveForward,
            targetAxis);
        var firstTransfer = PositiveTransferTo(target, targetAxis, sourceMagnitude: 2);
        var secondTransfer = PositiveTransferTo(target, targetAxis, sourceMagnitude: 1);

        Assert.True(BoundaryFlowTargetSettlementRun.TrySettle(
            targetBefore,
            [firstTransfer, secondTransfer],
            out var run));
        Assert.True(BoundaryFlowTargetSettlementRun.TryReadFinal(run, out var finalRead));
        Assert.True(finalRead.IsLegal);
        Assert.Equal(40, finalRead.Magnitude.Ticks);
        Assert.Equal("40f", finalRead.Notation);
        Assert.True(BoundaryFlowTargetSettlementRun.TryReadTimeline(run, out var timeline));
        Assert.True(TimeStepSequence.TryRead(timeline, out _, out var initial, out var steps));
        Assert.Equal(targetBefore, initial);
        Assert.Equal(2, steps.Length);
        Assert.True(BoundaryFlowTargetSettlementRun.TryReadTotals(run, out var totals));
        Assert.Equal(30, totals.AcceptedMagnitude);
        Assert.Equal(0, totals.BlockedMagnitude);
        Assert.Equal(2, totals.StepCount);
    }

    [Fact]
    public void SettlementRunPreservesBlockedResiduePerStep()
    {
        var target = new Carrier();
        var targetAxis = CarrierAxisSurface.SignedForward(new Carrier(), target);
        var targetBefore = BoundaryFlowValueSurface.Create(
            new Carrier(),
            target,
            10,
            BoundaryFlowLane.PositiveForward,
            targetAxis);
        var accepted = SignedForwardTransferTo(
            target,
            targetAxis,
            sourceMagnitude: 2,
            BoundaryFlowLane.PositiveForward);
        var blocked = SignedBothFlowTransferTo(
            target,
            targetAxis,
            sourceMagnitude: 5,
            BoundaryFlowLane.NegativeInverse);

        Assert.True(BoundaryFlowTargetSettlementRun.TrySettle(
            targetBefore,
            [accepted, blocked],
            out var run));
        Assert.True(BoundaryFlowTargetSettlementRun.TryReadFinal(run, out var finalRead));
        Assert.Equal(30, finalRead.Magnitude.Ticks);
        Assert.Equal("30f", finalRead.Notation);
        Assert.True(BoundaryFlowTargetSettlementRun.TryReadTotals(run, out var totals));
        Assert.Equal(20, totals.AcceptedMagnitude);
        Assert.Equal(5, totals.BlockedMagnitude);
        Assert.Equal(2, totals.StepCount);

        Assert.True(BoundaryFlowTargetSettlementRun.TryReadTimeline(run, out var timeline));
        Assert.True(TimeStepSequence.TryRead(timeline, out _, out _, out var steps));
        Assert.Equal(2, steps.Length);
        Assert.True(TimeStepSequence.TryReadStep(
            steps[1],
            out _,
            out _,
            out _,
            out _,
            out var transition,
            out var after,
            out var residue));
        Assert.True(BoundaryFlowValueTransfer.TryReadSummary(
            Assert.IsType<Relation>(transition),
            out var transferRead));
        Assert.True(transferRead.IsFullyBlocked);
        Assert.True(BoundaryFlowValueSurface.TryReadValue(Assert.IsType<Relation>(residue), out var blockedRead));
        Assert.False(blockedRead.IsLegal);
        Assert.Equal(BoundaryFlowLane.NegativeInverse, blockedRead.Lane);
        Assert.Equal(5, blockedRead.Magnitude.Ticks);
        Assert.True(BoundaryFlowValueSurface.TryReadValue(Assert.IsType<Relation>(after), out var afterRead));
        Assert.Equal(30, afterRead.Magnitude.Ticks);
    }

    [Fact]
    public void SharedCapacitySplitsBetweenCompetingLegalRoutes()
    {
        var target = new Carrier();
        var targetAxis = CarrierAxisSurface.PositiveUnit(new Carrier(), target);
        var firstRoute = PositiveRouteTo(target, targetAxis, sourceMagnitude: 3);
        var secondRoute = PositiveRouteTo(target, targetAxis, sourceMagnitude: 3);
        var capacity = Relation.Point(target, 40);

        Assert.True(BoundaryFlowSharedCapacityTransfer.TryAccept(
            capacity,
            [firstRoute, secondRoute],
            out var shared));
        Assert.True(BoundaryFlowSharedCapacityTransfer.TryReadTotals(shared, out var totals));
        Assert.Equal(40, totals.CapacityMagnitude);
        Assert.Equal(40, totals.AcceptedMagnitude);
        Assert.Equal(20, totals.BlockedMagnitude);
        Assert.Equal(0, totals.UnusedCapacity);
        Assert.Equal(2, totals.TransferCount);
        Assert.True(BoundaryFlowSharedCapacityTransfer.TryReadSummaries(shared, out var summaries));
        Assert.Equal(2, summaries.Length);
        Assert.All(summaries, summary =>
        {
            Assert.True(summary.IsPartiallyAccepted);
            Assert.Equal(20, summary.Accepted.Magnitude.Ticks);
            Assert.Equal(10, summary.Blocked.Magnitude.Ticks);
        });

        Assert.True(BoundaryFlowSharedCapacityTransfer.TryReadTransfers(shared, out var transfers));
        var targetBefore = BoundaryFlowValueSurface.Create(
            new Carrier(),
            target,
            0,
            BoundaryFlowLane.PositiveForward,
            targetAxis);
        Assert.True(BoundaryFlowTargetSettlementRun.TrySettle(
            targetBefore,
            transfers,
            out var run));
        Assert.True(BoundaryFlowTargetSettlementRun.TryReadFinal(run, out var finalRead));
        Assert.Equal(40, finalRead.Magnitude.Ticks);
        Assert.Equal("40f", finalRead.Notation);
    }

    [Fact]
    public void SharedCapacityKeepsIllegalLaneBlockedAndCapacityUnused()
    {
        var target = new Carrier();
        var targetAxis = CarrierAxisSurface.SignedForward(new Carrier(), target);
        var legalRoute = SignedForwardRouteTo(
            target,
            targetAxis,
            sourceMagnitude: 3,
            BoundaryFlowLane.PositiveForward);
        var illegalRoute = SignedBothFlowRouteTo(
            target,
            targetAxis,
            sourceMagnitude: 5,
            BoundaryFlowLane.NegativeInverse);
        var capacity = Relation.Point(target, 40);

        Assert.True(BoundaryFlowSharedCapacityTransfer.TryAccept(
            capacity,
            [legalRoute, illegalRoute],
            out var shared));
        Assert.True(BoundaryFlowSharedCapacityTransfer.TryReadTotals(shared, out var totals));
        Assert.Equal(40, totals.CapacityMagnitude);
        Assert.Equal(30, totals.AcceptedMagnitude);
        Assert.Equal(5, totals.BlockedMagnitude);
        Assert.Equal(10, totals.UnusedCapacity);
        Assert.Equal(2, totals.TransferCount);

        Assert.True(BoundaryFlowSharedCapacityTransfer.TryReadSummaries(shared, out var summaries));
        Assert.True(summaries[0].IsFullyAccepted);
        Assert.Equal(30, summaries[0].Accepted.Magnitude.Ticks);
        Assert.True(summaries[1].IsFullyBlocked);
        Assert.False(summaries[1].Blocked.IsLegal);
        Assert.Equal(BoundaryFlowLane.NegativeInverse, summaries[1].Blocked.Lane);
        Assert.Equal(5, summaries[1].Blocked.Magnitude.Ticks);

        Assert.True(BoundaryFlowSharedCapacityTransfer.TryReadTransfers(shared, out var transfers));
        var targetBefore = BoundaryFlowValueSurface.Create(
            new Carrier(),
            target,
            0,
            BoundaryFlowLane.PositiveForward,
            targetAxis);
        Assert.True(BoundaryFlowTargetSettlementRun.TrySettle(
            targetBefore,
            transfers,
            out var run));
        Assert.True(BoundaryFlowTargetSettlementRun.TryReadFinal(run, out var finalRead));
        Assert.Equal(30, finalRead.Magnitude.Ticks);
        Assert.Equal("30f", finalRead.Notation);
    }

    [Fact]
    public void TargetCapacitySurfaceKeepsCapacityAttachedToTargetBasis()
    {
        var target = new Carrier();
        var targetAxis = CarrierAxisSurface.PositiveUnit(new Carrier(), target);
        var targetBefore = BoundaryFlowValueSurface.Create(
            new Carrier(),
            target,
            0,
            BoundaryFlowLane.PositiveForward,
            targetAxis);

        Assert.True(BoundaryFlowTargetCapacitySurface.TryCreate(
            new Carrier(),
            targetBefore,
            40,
            out var capacity));
        Assert.True(BoundaryFlowTargetCapacitySurface.TryRead(
            capacity,
            out var targetSite,
            out var targetBasis,
            out var capacitySurface,
            out var capacityPoint,
            out _,
            out _));
        Assert.Equal(targetBefore, targetSite);
        Assert.Equal(targetAxis, targetBasis);
        Assert.True(ConceptCapacity.TryRead(
            capacitySurface,
            out var capacityPassage,
            out var wholeCapacity,
            out _,
            out _));
        Assert.Equal(targetBefore, capacityPassage);
        Assert.Equal(capacityPoint, wholeCapacity);

        Assert.True(BoundaryFlowTargetCapacitySurface.TryReadCapacity(capacity, out var read));
        Assert.Equal(40, read.Capacity.Ticks);
        Assert.True(read.TargetCarrier.SameIdentityAs(target));
        Assert.Equal(new BoundaryFlowExposure(BoundaryExposure.Positive, FlowExposure.Forward), read.TargetBasis);
    }

    [Fact]
    public void TargetCapacityCanResolveBasisFromUnitShapeOccupiedTarget()
    {
        var target = new Carrier(SignedForwardUnitShapeWithTransitions());
        var targetBefore = BoundaryFlowValueSurface.CreateOccupied(
            new Carrier(),
            target,
            0,
            BoundaryFlowLane.PositiveForward);

        Assert.True(BoundaryFlowTargetCapacitySurface.TryCreate(
            new Carrier(),
            targetBefore,
            40,
            out var capacity));
        Assert.True(BoundaryFlowTargetCapacitySurface.TryReadCapacity(capacity, out var read));
        Assert.True(read.TargetCarrier.SameIdentityAs(target));
        Assert.Equal(40, read.Capacity.Ticks);
        Assert.Equal(new BoundaryFlowExposure(BoundaryExposure.FullSigned, FlowExposure.Forward), read.TargetBasis);
    }

    [Fact]
    public void TargetCapacitySurfaceCanExposeCoarsePartitions()
    {
        var target = new Carrier();
        var targetAxis = CarrierAxisSurface.PositiveUnit(new Carrier(), target);
        var targetBefore = BoundaryFlowValueSurface.Create(
            new Carrier(),
            target,
            0,
            BoundaryFlowLane.PositiveForward,
            targetAxis);
        var selectedOpening = Relation.Point(new Carrier(), 2);

        Assert.True(BoundaryFlowTargetCapacitySurface.TryCreatePartitioned(
            new Carrier(),
            targetBefore,
            60,
            3,
            selectedOpening,
            out var capacity));
        Assert.True(BoundaryFlowTargetCapacitySurface.TryReadCapacity(capacity, out var read));
        Assert.Equal(60, read.Capacity.Ticks);
        Assert.True(BoundaryFlowTargetCapacitySurface.TryReadPartition(
            capacity,
            out var partition,
            out var partitionCount,
            out var selectedPartition));
        Assert.Equal(3, partitionCount);
        Assert.Equal(selectedOpening, selectedPartition);
        Assert.True(ConceptPartition.TryRead(
            partition,
            out var whole,
            out _,
            out _,
            out _,
            out _));
        Assert.True(ConceptSurfaceReaderForTests.TryReadPoint(Assert.IsType<Relation>(whole), out var wholeRead));
        Assert.Equal(60, wholeRead.Ticks);
    }

    [Fact]
    public void RemainingCapacitySurfaceSubtractsCurrentTargetOccupancy()
    {
        var target = new Carrier();
        var targetAxis = CarrierAxisSurface.PositiveUnit(new Carrier(), target);
        var targetBefore = BoundaryFlowValueSurface.Create(
            new Carrier(),
            target,
            15,
            BoundaryFlowLane.PositiveForward,
            targetAxis);

        Assert.True(BoundaryFlowTargetCapacitySurface.TryCreate(
            new Carrier(),
            targetBefore,
            40,
            out var capacity));
        Assert.True(BoundaryFlowRemainingCapacitySurface.TryCreate(
            new Carrier(),
            capacity,
            targetBefore,
            out var remaining));
        Assert.True(BoundaryFlowRemainingCapacitySurface.TryReadCapacity(remaining, out var read));
        Assert.Equal(40, read.WholeCapacity.Ticks);
        Assert.Equal(15, read.Occupied.Ticks);
        Assert.Equal(25, read.Remaining.Ticks);
        Assert.Equal(0, read.Overflow.Ticks);
        Assert.True(read.TargetCarrier.SameIdentityAs(target));
        Assert.Equal(new BoundaryFlowExposure(BoundaryExposure.Positive, FlowExposure.Forward), read.TargetBasis);
    }

    [Fact]
    public void RemainingCapacitySurfaceUsesSelectedTargetCapacityPartition()
    {
        var target = new Carrier();
        var targetAxis = CarrierAxisSurface.PositiveUnit(new Carrier(), target);
        var targetBefore = BoundaryFlowValueSurface.Create(
            new Carrier(),
            target,
            15,
            BoundaryFlowLane.PositiveForward,
            targetAxis);
        var selectedOpening = Relation.Point(new Carrier(), 2);

        Assert.True(BoundaryFlowTargetCapacitySurface.TryCreatePartitioned(
            new Carrier(),
            targetBefore,
            60,
            3,
            selectedOpening,
            out var capacity));
        Assert.True(BoundaryFlowRemainingCapacitySurface.TryCreate(
            new Carrier(),
            capacity,
            targetBefore,
            out var remaining));
        Assert.True(BoundaryFlowRemainingCapacitySurface.TryReadCapacity(remaining, out var read));
        Assert.Equal(60, read.WholeCapacity.Ticks);
        Assert.Equal(40, read.EffectiveCapacity.Ticks);
        Assert.Equal(15, read.Occupied.Ticks);
        Assert.Equal(25, read.Remaining.Ticks);
        Assert.Equal(0, read.Overflow.Ticks);
        Assert.Equal(0, read.PartitionResidue.Ticks);
        Assert.True(BoundaryFlowRemainingCapacitySurface.TryReadEffectiveCapacity(
            remaining,
            out var effectiveCapacity,
            out var partitionResidue));
        Assert.True(ConceptSurfaceReaderForTests.TryReadPoint(effectiveCapacity, out var effectiveRead));
        Assert.True(ConceptSurfaceReaderForTests.TryReadPoint(partitionResidue, out var residueRead));
        Assert.Equal(40, effectiveRead.Ticks);
        Assert.Equal(0, residueRead.Ticks);
    }

    [Fact]
    public void RemainingCapacitySurfaceLeavesOverflowAgainstSelectedPartition()
    {
        var target = new Carrier();
        var targetAxis = CarrierAxisSurface.PositiveUnit(new Carrier(), target);
        var targetBefore = BoundaryFlowValueSurface.Create(
            new Carrier(),
            target,
            45,
            BoundaryFlowLane.PositiveForward,
            targetAxis);
        var selectedOpening = Relation.Point(new Carrier(), 2);

        Assert.True(BoundaryFlowTargetCapacitySurface.TryCreatePartitioned(
            new Carrier(),
            targetBefore,
            60,
            3,
            selectedOpening,
            out var capacity));
        Assert.True(BoundaryFlowRemainingCapacitySurface.TryCreate(
            new Carrier(),
            capacity,
            targetBefore,
            out var remaining));
        Assert.True(BoundaryFlowRemainingCapacitySurface.TryReadCapacity(remaining, out var read));
        Assert.Equal(60, read.WholeCapacity.Ticks);
        Assert.Equal(40, read.EffectiveCapacity.Ticks);
        Assert.Equal(45, read.Occupied.Ticks);
        Assert.Equal(0, read.Remaining.Ticks);
        Assert.Equal(5, read.Overflow.Ticks);
    }

    [Fact]
    public void RemainingCapacitySurfaceLeavesOverflowWhenTargetAlreadyExceedsCapacity()
    {
        var target = new Carrier();
        var targetAxis = CarrierAxisSurface.PositiveUnit(new Carrier(), target);
        var targetBefore = BoundaryFlowValueSurface.Create(
            new Carrier(),
            target,
            45,
            BoundaryFlowLane.PositiveForward,
            targetAxis);

        Assert.True(BoundaryFlowTargetCapacitySurface.TryCreate(
            new Carrier(),
            targetBefore,
            40,
            out var capacity));
        Assert.True(BoundaryFlowRemainingCapacitySurface.TryCreate(
            new Carrier(),
            capacity,
            targetBefore,
            out var remaining));
        Assert.True(BoundaryFlowRemainingCapacitySurface.TryReadCapacity(remaining, out var read));
        Assert.Equal(40, read.WholeCapacity.Ticks);
        Assert.Equal(40, read.EffectiveCapacity.Ticks);
        Assert.Equal(45, read.Occupied.Ticks);
        Assert.Equal(0, read.Remaining.Ticks);
        Assert.Equal(5, read.Overflow.Ticks);
    }

    [Fact]
    public void SharedCapacityCanUseTargetLocalCapacitySurface()
    {
        var target = new Carrier();
        var targetAxis = CarrierAxisSurface.PositiveUnit(new Carrier(), target);
        var targetBefore = BoundaryFlowValueSurface.Create(
            new Carrier(),
            target,
            0,
            BoundaryFlowLane.PositiveForward,
            targetAxis);
        var firstRoute = PositiveRouteTo(target, targetAxis, sourceMagnitude: 3);
        var secondRoute = PositiveRouteTo(target, targetAxis, sourceMagnitude: 3);

        Assert.True(BoundaryFlowTargetCapacitySurface.TryCreate(
            new Carrier(),
            targetBefore,
            40,
            out var capacity));
        Assert.True(BoundaryFlowSharedCapacityTransfer.TryAccept(
            capacity,
            [firstRoute, secondRoute],
            out var shared));
        Assert.True(BoundaryFlowSharedCapacityTransfer.TryRead(
            shared,
            out var capacityWitness,
            out _,
            out _,
            out _,
            out _));
        Assert.Equal(capacity, capacityWitness);
        Assert.True(BoundaryFlowSharedCapacityTransfer.TryReadTotals(shared, out var totals));
        Assert.Equal(40, totals.CapacityMagnitude);
        Assert.Equal(40, totals.AcceptedMagnitude);
        Assert.Equal(20, totals.BlockedMagnitude);
    }

    [Fact]
    public void SharedCapacityCanUseRemainingTargetCapacity()
    {
        var target = new Carrier();
        var targetAxis = CarrierAxisSurface.PositiveUnit(new Carrier(), target);
        var targetBefore = BoundaryFlowValueSurface.Create(
            new Carrier(),
            target,
            15,
            BoundaryFlowLane.PositiveForward,
            targetAxis);
        var firstRoute = PositiveRouteTo(target, targetAxis, sourceMagnitude: 3);
        var secondRoute = PositiveRouteTo(target, targetAxis, sourceMagnitude: 3);

        Assert.True(BoundaryFlowTargetCapacitySurface.TryCreate(
            new Carrier(),
            targetBefore,
            40,
            out var capacity));
        Assert.True(BoundaryFlowRemainingCapacitySurface.TryCreate(
            new Carrier(),
            capacity,
            targetBefore,
            out var remaining));
        Assert.True(BoundaryFlowSharedCapacityTransfer.TryAccept(
            remaining,
            [firstRoute, secondRoute],
            out var shared));
        Assert.True(BoundaryFlowSharedCapacityTransfer.TryReadTotals(shared, out var totals));
        Assert.Equal(25, totals.CapacityMagnitude);
        Assert.Equal(25, totals.AcceptedMagnitude);
        Assert.Equal(35, totals.BlockedMagnitude);
        Assert.Equal(0, totals.UnusedCapacity);

        Assert.True(BoundaryFlowSharedCapacityTransfer.TryReadSummaries(shared, out var summaries));
        Assert.Equal(13, summaries[0].Accepted.Magnitude.Ticks);
        Assert.Equal(17, summaries[0].Blocked.Magnitude.Ticks);
        Assert.Equal(12, summaries[1].Accepted.Magnitude.Ticks);
        Assert.Equal(18, summaries[1].Blocked.Magnitude.Ticks);

        Assert.True(BoundaryFlowSharedCapacityTransfer.TryReadTransfers(shared, out var transfers));
        Assert.True(BoundaryFlowTargetSettlementRun.TrySettle(
            targetBefore,
            transfers,
            out var run));
        Assert.True(BoundaryFlowTargetSettlementRun.TryReadFinal(run, out var finalRead));
        Assert.Equal(40, finalRead.Magnitude.Ticks);
        Assert.Equal("40f", finalRead.Notation);
    }

    [Fact]
    public void SharedCapacityCanUsePartitionedRemainingTargetCapacity()
    {
        var target = new Carrier();
        var targetAxis = CarrierAxisSurface.PositiveUnit(new Carrier(), target);
        var targetBefore = BoundaryFlowValueSurface.Create(
            new Carrier(),
            target,
            15,
            BoundaryFlowLane.PositiveForward,
            targetAxis);
        var firstRoute = PositiveRouteTo(target, targetAxis, sourceMagnitude: 3);
        var secondRoute = PositiveRouteTo(target, targetAxis, sourceMagnitude: 3);
        var selectedOpening = Relation.Point(new Carrier(), 2);

        Assert.True(BoundaryFlowTargetCapacitySurface.TryCreatePartitioned(
            new Carrier(),
            targetBefore,
            60,
            3,
            selectedOpening,
            out var capacity));
        Assert.True(BoundaryFlowRemainingCapacitySurface.TryCreate(
            new Carrier(),
            capacity,
            targetBefore,
            out var remaining));
        Assert.True(BoundaryFlowSharedCapacityTransfer.TryAccept(
            remaining,
            [firstRoute, secondRoute],
            out var shared));
        Assert.True(BoundaryFlowSharedCapacityTransfer.TryReadTotals(shared, out var totals));
        Assert.Equal(25, totals.CapacityMagnitude);
        Assert.Equal(25, totals.AcceptedMagnitude);
        Assert.Equal(35, totals.BlockedMagnitude);
        Assert.Equal(0, totals.UnusedCapacity);
    }

    [Fact]
    public void TargetLocalCapacityRejectsRoutesForOtherTargetCarriers()
    {
        var capacityTarget = new Carrier();
        var capacityTargetAxis = CarrierAxisSurface.PositiveUnit(new Carrier(), capacityTarget);
        var capacityTargetBefore = BoundaryFlowValueSurface.Create(
            new Carrier(),
            capacityTarget,
            0,
            BoundaryFlowLane.PositiveForward,
            capacityTargetAxis);
        var routeTarget = new Carrier();
        var routeTargetAxis = CarrierAxisSurface.PositiveUnit(new Carrier(), routeTarget);
        var route = PositiveRouteTo(routeTarget, routeTargetAxis, sourceMagnitude: 3);

        Assert.True(BoundaryFlowTargetCapacitySurface.TryCreate(
            new Carrier(),
            capacityTargetBefore,
            40,
            out var capacity));
        Assert.False(BoundaryFlowSharedCapacityTransfer.TryAccept(
            capacity,
            [route],
            out _));
    }

    public static IEnumerable<object[]> SelectedLaneBasisStates()
    {
        yield return [BoundaryExposure.Positive, FlowExposure.Forward, "PF"];
        yield return [BoundaryExposure.Negative, FlowExposure.Inverse, "NI"];
        yield return [BoundaryExposure.Negative, FlowExposure.Both, "NF|NI"];
        yield return [BoundaryExposure.FullSigned, FlowExposure.Forward, "PF|NF"];
        yield return [BoundaryExposure.FullSigned, FlowExposure.Inverse, "PI|NI"];
        yield return [BoundaryExposure.FullSigned, FlowExposure.Both, "PF|PI|NF|NI"];
        yield return [BoundaryExposure.BoundedWhole, FlowExposure.None, "S:None"];
        yield return [BoundaryExposure.BoundedWhole, FlowExposure.Both, "SF|SI"];
    }

    private static UnitShape SignedForwardUnitShapeWithTransitions(bool includeReverse = true)
    {
        if (includeReverse)
        {
            return BoundaryFlowUnitShape.SignedForward();
        }

        var possibleLanes = BoundaryFlowLaneBasis.Create(
            new Carrier(),
            BoundaryExposure.FullSigned,
            FlowExposure.Forward,
            Relation.Point(new Carrier(), 1));
        var positiveToNegative = BoundaryFlowLaneTransitionSurface.Create(
            new Carrier(),
            BoundaryFlowLane.PositiveForward,
            BoundaryFlowLane.NegativeForward);
        return UnitShape
            .DescribedAtom(possibleLanes)
            .WithDescriptors(possibleLanes, positiveToNegative);
    }

    private static Relation PositiveTransferTo(Carrier target, Relation targetAxis, long sourceMagnitude)
    {
        var route = PositiveRouteTo(target, targetAxis, sourceMagnitude);

        Assert.True(BoundaryFlowValueTransfer.TryAccept(route, out var transfer));
        return transfer;
    }

    private static Relation SignedForwardTransferTo(
        Carrier target,
        Relation targetAxis,
        long sourceMagnitude,
        BoundaryFlowLane sourceLane)
    {
        var route = SignedForwardRouteTo(target, targetAxis, sourceMagnitude, sourceLane);

        Assert.True(BoundaryFlowValueTransfer.TryAccept(route, out var transfer));
        return transfer;
    }

    private static Relation SignedBothFlowTransferTo(
        Carrier target,
        Relation targetAxis,
        long sourceMagnitude,
        BoundaryFlowLane sourceLane)
    {
        var route = SignedBothFlowRouteTo(target, targetAxis, sourceMagnitude, sourceLane);

        Assert.True(BoundaryFlowValueTransfer.TryAccept(route, out var transfer));
        return transfer;
    }

    private static Relation PositiveRouteTo(Carrier target, Relation targetAxis, long sourceMagnitude)
    {
        var source = new Carrier();
        var sourceAxis = CarrierAxisSurface.PositiveUnit(new Carrier(), source);
        return ProjectedRouteTo(
            source,
            target,
            sourceAxis,
            targetAxis,
            sourceMagnitude,
            BoundaryFlowLane.PositiveForward,
            Relation.Segment(source, 0, 10),
            Relation.Segment(target, 0, 100));
    }

    private static Relation SignedForwardRouteTo(
        Carrier target,
        Relation targetAxis,
        long sourceMagnitude,
        BoundaryFlowLane sourceLane)
    {
        var source = new Carrier();
        var sourceAxis = CarrierAxisSurface.SignedForward(new Carrier(), source);
        return ProjectedRouteTo(
            source,
            target,
            sourceAxis,
            targetAxis,
            sourceMagnitude,
            sourceLane,
            Relation.Segment(source, -10, 10),
            Relation.Segment(target, -100, 100));
    }

    private static Relation SignedBothFlowRouteTo(
        Carrier target,
        Relation targetAxis,
        long sourceMagnitude,
        BoundaryFlowLane sourceLane)
    {
        var source = new Carrier();
        var sourceAxis = CarrierAxisSurface.SignedBothFlows(new Carrier(), source);
        return ProjectedRouteTo(
            source,
            target,
            sourceAxis,
            targetAxis,
            sourceMagnitude,
            sourceLane,
            Relation.Segment(source, -10, 10),
            Relation.Segment(target, -10, 10));
    }

    private static Relation ProjectedRouteTo(
        Carrier source,
        Carrier target,
        Relation sourceAxis,
        Relation targetAxis,
        long sourceMagnitude,
        BoundaryFlowLane sourceLane,
        Relation sourceFrame,
        Relation targetFrame)
    {
        var sourceValue = BoundaryFlowValueSurface.Create(
            new Carrier(),
            source,
            checked((ulong)sourceMagnitude),
            sourceLane,
            sourceAxis);
        var route = new Relation(sourceFrame, targetFrame);

        Assert.True(BoundaryFlowValueRoute.TryProject(
            sourceValue,
            route,
            targetAxis,
            target,
            out var routeSurface));
        return routeSurface;
    }

}

internal static class ConceptSurfaceReaderForTests
{
    public static bool TryReadPoint(IElement element, out CarrierLocation location)
    {
        if (element is Relation point &&
            point.Left is CarrierLocation pointLocation &&
            point.Right is Carrier pointCarrier &&
            pointLocation.Carrier.SameIdentityAs(pointCarrier))
        {
            location = pointLocation;
            return true;
        }

        location = default;
        return false;
    }
}
