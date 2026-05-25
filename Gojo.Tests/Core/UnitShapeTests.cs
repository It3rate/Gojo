using Gojo.Core;

namespace Gojo.Tests.Core;

public sealed class UnitShapeTests
{
    [Fact]
    public void DefaultCarrierHasFreshLinearUnitShape()
    {
        var carrier = new Carrier();
        var other = new Carrier();

        Assert.True(carrier.UnitShape.CompatibleWith(UnitShape.Atom(carrier)));
        Assert.False(carrier.UnitShape.CompatibleWith(other.UnitShape));
    }

    [Fact]
    public void CarriersCanShareUnitShapeWithoutSharingIdentity()
    {
        var unitShape = new Carrier().UnitShape;
        var first = new Carrier(unitShape);
        var second = new Carrier(unitShape);

        Assert.True(first.UnitShape.CompatibleWith(second.UnitShape));
        Assert.False(first.SameIdentityAs(second));
    }

    [Fact]
    public void InverseCarrierHasInspectableOpposedUnitFaceCompatibleWithForwardCarrier()
    {
        var carrier = new Carrier();

        var inverse = carrier.Inverse();

        Assert.False(inverse.UnitShape.Equals(carrier.UnitShape));
        Assert.True(inverse.UnitShape.CompatibleWith(carrier.UnitShape));
        Assert.True(inverse.UnitShape.TryReadOpposedFace(out var source));
        Assert.True(source.CompatibleWith(carrier.UnitShape));
    }

    [Fact]
    public void SameCarrierBasisReadsCarrierUnitShape()
    {
        var carrier = new Carrier();
        var basis = Segment(carrier, 0, 10);

        Assert.True(basis.TryReadUnitShape(out var unitShape));
        Assert.True(unitShape.CompatibleWith(carrier.UnitShape));
    }

    [Fact]
    public void PointOrthogonalCanCreatePartitionedCircleCarrierWithoutRadiusCalibration()
    {
        var source = new Carrier();
        var circleShape = UnitShape.OrthogonalOf(Point(source, 0));
        var perimeter = new Carrier(circleShape);
        var cycles = new Carrier();
        perimeter.Attach(new Relation(Point(perimeter, 12), Point(cycles, 0)));

        perimeter.Add(27);

        Assert.True(perimeter.UnitShape.CompatibleWith(circleShape));
        Assert.Equal(3UL, perimeter.Value);
        Assert.Equal(2UL, cycles.Value);
        Assert.Equal(0, perimeter.Tension);
    }

    [Fact]
    public void OrthogonalShapeReadsSourceAsSnapshot()
    {
        var carrier = new Carrier();
        var segment = Segment(carrier, 0, 10);
        var sheetShape = UnitShape.OrthogonalOf(segment);

        Assert.True(sheetShape.TryReadOrthogonalSource(out var source));
        Assert.Equal(segment, source);

        Assert.True(((Relation)source).Perturb(new CarrierLocation(carrier, 5)));
        Assert.True(sheetShape.TryReadOrthogonalSource(out var reread));
        Assert.Equal(segment, reread);
    }

    [Fact]
    public void ZeroLengthSegmentOrthogonalIsRayLikeAndDistinctFromPointOrthogonal()
    {
        var carrier = new Carrier();
        var pointOrthogonal = UnitShape.OrthogonalOf(Point(carrier, 0));
        var rayOrthogonal = UnitShape.OrthogonalOf(Segment(carrier, 0, 0));
        var sameRayShape = UnitShape.OrthogonalOf(Segment(carrier, 0, 0));

        Assert.False(rayOrthogonal.CompatibleWith(pointOrthogonal));
        Assert.True(rayOrthogonal.CompatibleWith(sameRayShape));
    }

    [Fact]
    public void CoincidentPointsCreateRayLikeOrthogonalWithoutParentSegment()
    {
        var carrier = new Carrier();
        var coincidentPoints = new Relation(Point(carrier, 5), Point(carrier, 5));
        var rayShape = UnitShape.OrthogonalOf(coincidentPoints);

        Assert.True(rayShape.TryReadOrthogonalSource(out var source));
        Assert.Equal(coincidentPoints, source);
        Assert.False(rayShape.CompatibleWith(UnitShape.OrthogonalOf(Point(carrier, 5))));
    }

    [Fact]
    public void SegmentOrthogonalCanCreateRectangularShapeCarrier()
    {
        var carrier = new Carrier();
        var basis = Segment(carrier, 0, 10);
        var rectangleShape = UnitShape.OrthogonalOf(basis);
        var rectangleCarrier = new Carrier(rectangleShape);

        rectangleCarrier.Set(4);

        Assert.True(rectangleCarrier.UnitShape.CompatibleWith(rectangleShape));
        Assert.Equal(4UL, rectangleCarrier.Value);
        Assert.Equal(0, rectangleCarrier.Tension);
    }

    [Fact]
    public void OrthogonalShapeRemainsAtomicInsideProductParticipationReads()
    {
        var carrier = new Carrier();
        var rayOrthogonal = UnitShape.OrthogonalOf(Segment(carrier, 0, 0));
        var repeatedRay = UnitShape.Product(rayOrthogonal, rayOrthogonal);

        Assert.Equal(2, repeatedRay.ProductParticipationOf(rayOrthogonal));
    }

    [Fact]
    public void OrthogonalRelationSurfaceReadsPointShapeUnit()
    {
        var source = new Carrier();
        var point = Point(source, 0);
        var surface = new Relation(point, Segment(Carrier.Unknown, 0, 0));

        Assert.True(surface.TryReadUnitShape(out var unitShape));

        Assert.True(unitShape.CompatibleWith(UnitShape.OrthogonalOf(point)));
        Assert.False(unitShape.CompatibleWith(source.UnitShape));
    }

    [Fact]
    public void OrthogonalRelationSurfaceCanCrystallizePartitionedCarrier()
    {
        var source = new Carrier();
        var surface = new Relation(Point(source, 0), Segment(Carrier.Unknown, 0, 0));
        var cycles = new Carrier();

        Assert.True(surface.TryCrystallize(out var perimeter));
        perimeter.Attach(new Relation(Point(perimeter, 12), Point(cycles, 0)));

        perimeter.Add(27);

        Assert.True(perimeter.UnitShape.CompatibleWith(UnitShape.OrthogonalOf(Point(source, 0))));
        Assert.Equal(3UL, perimeter.Value);
        Assert.Equal(2UL, cycles.Value);
        Assert.Equal(0, perimeter.Tension);
    }

    [Fact]
    public void OrthogonalRelationSurfaceWinsOverZeroExtentPortRead()
    {
        var source = new Carrier();
        var zeroExtentPortLikeSurface = new Relation(Segment(source, 0, 10), Segment(Carrier.Unknown, 0, 0));

        Assert.True(zeroExtentPortLikeSurface.TryReadUnitShape(out var unitShape));

        Assert.True(unitShape.CompatibleWith(UnitShape.OrthogonalOf(Segment(source, 0, 10))));
        Assert.False(unitShape.CompatibleWith(source.UnitShape));
    }

    [Fact]
    public void OrthogonalRelationSurfaceReadsSegmentAndCoincidentPointSources()
    {
        var carrier = new Carrier();
        var segment = Segment(carrier, 0, 10);
        var coincidentPoints = new Relation(Point(carrier, 5), Point(carrier, 5));
        var segmentSurface = new Relation(segment, Segment(Carrier.Unknown, 0, 0));
        var raySurface = new Relation(coincidentPoints, Segment(Carrier.Unknown, 0, 0));

        Assert.True(segmentSurface.TryReadUnitShape(out var segmentShape));
        Assert.True(raySurface.TryReadUnitShape(out var rayShape));

        Assert.True(segmentShape.CompatibleWith(UnitShape.OrthogonalOf(segment)));
        Assert.True(rayShape.CompatibleWith(UnitShape.OrthogonalOf(coincidentPoints)));
        Assert.False(rayShape.CompatibleWith(UnitShape.OrthogonalOf(Point(carrier, 5))));
    }

    [Fact]
    public void OfficialCircularShapeComesFromPointOrthogonalOutlet()
    {
        var center = new Carrier();
        var point = Relation.Point(center, 0);
        var surface = Relation.OrthogonalOutlet(point);

        Assert.True(surface.TryReadUnitShape(out var readShape));

        Assert.True(readShape.CompatibleWith(UnitShape.CircularFrom(point)));
        Assert.True(readShape.TryReadOrthogonalSource(out var source));
        Assert.Equal(point, source);
    }

    [Fact]
    public void OfficialLinearShapeComesFromZeroLengthSegmentOrthogonalOutlet()
    {
        var carrier = new Carrier();
        var zeroSegment = Relation.Segment(carrier, 0, 0);
        var point = Relation.Point(carrier, 0);

        var linear = UnitShape.LinearFrom(zeroSegment);
        var circular = UnitShape.CircularFrom(point);

        Assert.True(Relation.OrthogonalOutlet(zeroSegment).TryReadUnitShape(out var readShape));
        Assert.True(readShape.CompatibleWith(linear));
        Assert.False(linear.CompatibleWith(circular));
    }

    [Fact]
    public void OfficialSheetShapeComesFromNonzeroSegmentOrthogonalOutlet()
    {
        var carrier = new Carrier();
        var segment = Relation.Segment(carrier, 0, 10);
        var zeroSegment = Relation.Segment(carrier, 0, 0);

        var sheet = UnitShape.SheetFrom(segment);

        Assert.True(Relation.OrthogonalOutlet(segment).TryReadUnitShape(out var readShape));
        Assert.True(readShape.CompatibleWith(sheet));
        Assert.False(sheet.CompatibleWith(UnitShape.LinearFrom(zeroSegment)));
    }

    [Fact]
    public void CircularPerimeterCarrierPartitionsGearTeethWithOrdinaryTicks()
    {
        var center = new Carrier();
        var perimeter = new Carrier(UnitShape.CircularFrom(Relation.Point(center, 0)));
        var rotations = new Carrier();
        perimeter.Attach(new Relation(Relation.Point(perimeter, 12), Relation.Point(rotations, 0)));

        perimeter.Add(27);

        Assert.Equal(3UL, perimeter.Value);
        Assert.Equal(2UL, rotations.Value);
        Assert.Equal(0, perimeter.Tension);
    }

    [Fact]
    public void OpposedGearToothRatioCanBeReadThroughCircularPerimeters()
    {
        var driverCenter = new Carrier();
        var drivenCenter = new Carrier();
        var driver = new Carrier(UnitShape.CircularFrom(Relation.Point(driverCenter, 0)));
        var driven = new Carrier(UnitShape.CircularFrom(Relation.Point(drivenCenter, 0)));
        var mesh = new Relation(
            Relation.Segment(driver, 0, 20),
            Relation.Segment(driven, 0, -10));
        driver.Set(4);

        var read = mesh.Project(driver.ValueSlot, driven);

        Assert.Equal(new Relation(new CarrierLocation(driven, -2), new CarrierLocation(driver, 0)), read);
    }

    [Fact]
    public void TransferPortUsesConcreteTargetUnitShape()
    {
        var light = new Carrier();
        var heat = new Carrier();
        var port = new Relation(Point(light, 1), Point(heat, 1));

        Assert.True(port.TryReadUnitShape(out var unitShape));

        Assert.True(unitShape.CompatibleWith(heat.UnitShape));
        Assert.False(unitShape.CompatibleWith(light.UnitShape));
    }

    [Fact]
    public void UnknownOutputPortUsesSourceUnitShape()
    {
        var source = new Carrier();
        var port = new Relation(Segment(source, 0, 10), Point(Carrier.Unknown, 1));

        Assert.True(port.TryReadUnitShape(out var unitShape));

        Assert.True(unitShape.CompatibleWith(source.UnitShape));
    }

    [Fact]
    public void PointOutputOperationReadsAlignedCompatibleProductAsSharedUnitShape()
    {
        var lengthShape = new Carrier().UnitShape;
        var x = new Carrier(lengthShape);
        var y = new Carrier(lengthShape);
        var result = new Carrier();
        var xPort = new Relation(Point(x, 1), Point(Carrier.Unknown, 1));
        var yPort = new Relation(Point(y, 1), Point(Carrier.Unknown, 1));
        var operation = new Relation(new Relation(xPort, yPort), Point(result, 1));

        Assert.True(operation.TryReadUnitShape(out var unitShape));

        Assert.True(unitShape.CompatibleWith(lengthShape));
    }

    [Fact]
    public void PointOutputOperationReadsAlignedIncompatibleProductAsUnreducedParticipation()
    {
        var length = new Carrier();
        var heat = new Carrier();
        var result = new Carrier();
        var lengthPort = new Relation(Point(length, 1), Point(Carrier.Unknown, 1));
        var heatPort = new Relation(Point(heat, 1), Point(Carrier.Unknown, 1));
        var operation = new Relation(new Relation(lengthPort, heatPort), Point(result, 1));
        var expected = UnitShape.Additive(length.UnitShape, heat.UnitShape);

        Assert.True(operation.TryReadUnitShape(out var unitShape));

        Assert.True(unitShape.CompatibleWith(expected));
        Assert.False(unitShape.CompatibleWith(length.UnitShape));
        Assert.False(unitShape.CompatibleWith(heat.UnitShape));
    }

    [Fact]
    public void CompatibleAdditiveUnitShapeCollapsesWithoutGroupOperands()
    {
        var fruitShape = new Carrier().UnitShape;

        var unitShape = UnitShape.Additive(fruitShape, fruitShape);

        Assert.True(unitShape.CompatibleWith(fruitShape));
        Assert.False(unitShape.TryReadAdditiveOperands(out _));
    }

    [Fact]
    public void AdditiveUnitShapeCombinesCompatibleOperandsBeforeGrouping()
    {
        var appleShape = new Carrier().UnitShape;
        var orangeShape = new Carrier().UnitShape;
        var bananaShape = new Carrier().UnitShape;

        var groupShape = UnitShape.Additive(appleShape, orangeShape, appleShape, bananaShape, orangeShape);

        Assert.True(groupShape.TryReadAdditiveOperands(out var operands));
        Assert.Equal(3, operands.Length);
        Assert.True(operands[0].CompatibleWith(appleShape));
        Assert.True(operands[1].CompatibleWith(orangeShape));
        Assert.True(operands[2].CompatibleWith(bananaShape));
    }

    [Fact]
    public void AdditiveOperandReadPreservesProductOperandsAsOneParticipant()
    {
        var lengthShape = new Carrier().UnitShape;
        var timeShape = new Carrier().UnitShape;
        var areaShape = UnitShape.Product(lengthShape, lengthShape);

        var groupShape = UnitShape.Additive(areaShape, timeShape);

        Assert.True(groupShape.TryReadAdditiveOperands(out var operands));
        Assert.Equal(2, operands.Length);
        Assert.True(operands[0].CompatibleWith(areaShape));
        Assert.True(operands[1].CompatibleWith(timeShape));
    }

    [Fact]
    public void SegmentOutputOperationReadsOrthogonalProductUnitShape()
    {
        var lengthShape = new Carrier().UnitShape;
        var width = new Carrier(lengthShape);
        var height = new Carrier(lengthShape);
        var result = new Carrier();
        var widthPort = new Relation(Point(width, 1), Point(Carrier.Unknown, 1));
        var heightPort = new Relation(Point(height, 1), Point(Carrier.Unknown, 1));
        var operation = new Relation(new Relation(widthPort, heightPort), Segment(result, 0, 1));
        var expected = UnitShape.Product(lengthShape, lengthShape);

        Assert.True(operation.TryReadUnitShape(out var unitShape));

        Assert.True(unitShape.CompatibleWith(expected));
        Assert.Equal(2, unitShape.ProductParticipationOf(lengthShape));
    }

    [Fact]
    public void AlignedProductOfCompatibleUnitsKeepsSharedType()
    {
        var lengthShape = new Carrier().UnitShape;

        var aligned = UnitShape.AlignedProduct(lengthShape, lengthShape);

        Assert.True(aligned.CompatibleWith(lengthShape));
        Assert.False(aligned.TryReadOrthogonalProductOperands(out _));
    }

    [Fact]
    public void OrthogonalProductOfCompatibleUnitsCreatesProductType()
    {
        var lengthShape = new Carrier().UnitShape;

        var orthogonal = UnitShape.OrthogonalProduct(lengthShape, lengthShape);

        Assert.False(orthogonal.CompatibleWith(lengthShape));
        Assert.Equal(2, orthogonal.ProductParticipationOf(lengthShape));
        Assert.True(orthogonal.TryReadOrthogonalProductOperands(out var operands));
        Assert.Equal(2, operands.Length);
        Assert.True(operands[0].CompatibleWith(lengthShape));
        Assert.True(operands[1].CompatibleWith(lengthShape));
    }

    [Fact]
    public void AlignedProductOfDifferentUnitsStaysAdditiveParticipation()
    {
        var lengthShape = new Carrier().UnitShape;
        var heatShape = new Carrier().UnitShape;

        var aligned = UnitShape.AlignedProduct(lengthShape, heatShape);

        Assert.True(aligned.TryReadAdditiveOperands(out var operands));
        Assert.Equal(2, operands.Length);
        Assert.True(operands[0].CompatibleWith(lengthShape));
        Assert.True(operands[1].CompatibleWith(heatShape));
    }

    [Fact]
    public void ResolutionShapeCrystallizesCoarseTickOverSourceUnit()
    {
        var lengthShape = new Carrier().UnitShape;

        var hundreds = UnitShape.ResolutionOf(lengthShape, 100);

        Assert.False(hundreds.CompatibleWith(lengthShape));
        Assert.True(hundreds.TryReadResolution(out var source, out var sourceTicksPerTick));
        Assert.True(source.CompatibleWith(lengthShape));
        Assert.Equal(100, sourceTicksPerTick);
        Assert.Equal(1, hundreds.ProductParticipationOf(lengthShape));
    }

    [Fact]
    public void ResolutionShapesRemainCompatibleOnlyAtSameCoarseSpan()
    {
        var lengthShape = new Carrier().UnitShape;

        var firstHundreds = UnitShape.ResolutionOf(lengthShape, 100);
        var secondHundreds = UnitShape.ResolutionOf(lengthShape, 100);
        var tens = UnitShape.ResolutionOf(lengthShape, 10);

        Assert.True(firstHundreds.CompatibleWith(secondHundreds));
        Assert.False(firstHundreds.CompatibleWith(tens));
    }

    [Fact]
    public void UnitShapeDescriptorCanGroundAlgebraicAtomProvenance()
    {
        var temperature = new Carrier();

        var shape = UnitShape.DescribedAtom(temperature);

        Assert.Equal(1, shape.DescriptorCount);
        Assert.True(shape.TryReadDescriptor(0, out var descriptor));
        Assert.Same(temperature, descriptor);
        Assert.False(shape.TryReadDescriptor(1, out _));
    }

    [Fact]
    public void UnitShapeBareAlgebraDisconnectsOptionalDescriptors()
    {
        var temperature = UnitShape.DescribedAtom(new Carrier());

        var bare = temperature.Bare();

        Assert.Equal(0, bare.DescriptorCount);
        Assert.True(temperature.AlgebraicallyCompatibleWith(bare));
        Assert.False(temperature.CompatibleWith(bare));
        Assert.True(bare.WithDescriptors(temperature.DescriptorSurfaces).CompatibleWith(temperature));
    }

    [Fact]
    public void UnitShapeDescriptorsDistinguishDirectCompatibilityFromAlgebraicCompatibility()
    {
        var temperature = UnitShape.DescribedAtom(new Carrier());
        var distance = UnitShape.DescribedAtom(new Carrier());

        Assert.True(temperature.AlgebraicallyCompatibleWith(distance));
        Assert.False(temperature.CompatibleWith(distance));
    }

    [Fact]
    public void UnitShapeCompositeReusesDescriptorSlotsAlgebraically()
    {
        var aDescriptor = new Carrier();
        var bDescriptor = new Carrier();
        var a = UnitShape.DescribedAtom(aDescriptor);
        var b = UnitShape.DescribedAtom(bDescriptor);

        var formula = UnitShape.Additive(
            UnitShape.Product(a, a),
            UnitShape.Product(b, b));

        Assert.Equal(2, formula.DescriptorCount);
        Assert.True(formula.TryReadDescriptor(0, out var first));
        Assert.True(formula.TryReadDescriptor(1, out var second));
        Assert.Same(aDescriptor, first);
        Assert.Same(bDescriptor, second);

        var bare = formula.Bare();
        Assert.True(bare.AlgebraicallyCompatibleWith(formula));
        Assert.False(bare.CompatibleWith(formula));
        Assert.True(bare.WithDescriptors(aDescriptor, bDescriptor).CompatibleWith(formula));

        Assert.True(formula.TryReadAdditiveOperands(out var terms));
        Assert.Equal(2, terms.Length);
        Assert.Equal(1, terms[0].DescriptorCount);
        Assert.Equal(1, terms[1].DescriptorCount);
        Assert.True(terms[0].CompatibleWith(UnitShape.Product(a, a)));
        Assert.True(terms[1].CompatibleWith(UnitShape.Product(b, b)));
    }

    [Fact]
    public void NestedResolutionShapesComposeIntoOneSourceSpan()
    {
        var lengthShape = new Carrier().UnitShape;
        var tens = UnitShape.ResolutionOf(lengthShape, 10);

        var thousands = UnitShape.ResolutionOf(tens, 100);

        Assert.True(thousands.TryReadResolution(out var source, out var sourceTicksPerTick));
        Assert.True(source.CompatibleWith(lengthShape));
        Assert.Equal(1000, sourceTicksPerTick);
    }

    [Fact]
    public void AdditiveOperandReadPreservesResolutionOperandsAsWholeStructure()
    {
        var lengthShape = new Carrier().UnitShape;
        var heatShape = new Carrier().UnitShape;
        var hundreds = UnitShape.ResolutionOf(lengthShape, 100);

        var grouped = UnitShape.Additive(hundreds, heatShape);

        Assert.True(grouped.TryReadAdditiveOperands(out var operands));
        Assert.Equal(2, operands.Length);
        Assert.True(operands[0].CompatibleWith(hundreds));
        Assert.True(operands[1].CompatibleWith(heatShape));
    }

    [Fact]
    public void CrystallizedCarrierUsesDerivedUnitShapeWithoutProjectingValue()
    {
        var lengthShape = new Carrier().UnitShape;
        var width = new Carrier(lengthShape);
        var height = new Carrier(lengthShape);
        var result = new Carrier();
        var widthPort = new Relation(Point(width, 1), Point(Carrier.Unknown, 1));
        var heightPort = new Relation(Point(height, 1), Point(Carrier.Unknown, 1));
        var operation = new Relation(new Relation(widthPort, heightPort), Segment(result, 0, 1));

        Assert.True(operation.TryCrystallize(out var crystal));

        Assert.True(crystal.UnitShape.CompatibleWith(UnitShape.Product(lengthShape, lengthShape)));
        Assert.Equal(0UL, crystal.Value);
        Assert.Equal(0, crystal.Tension);
    }

    [Fact]
    public void OrthogonalProductCanCrystallizeResultBasisWithoutMultiplyingValues()
    {
        var lengthShape = new Carrier().UnitShape;
        var width = new Carrier(lengthShape);
        var height = new Carrier(lengthShape);
        var result = new Carrier();
        var widthPort = new Relation(Point(width, 1), Point(Carrier.Unknown, 1));
        var heightPort = new Relation(Point(height, 1), Point(Carrier.Unknown, 1));
        var operation = new Relation(new Relation(widthPort, heightPort), Segment(result, 0, 1));
        width.Set(5);
        height.Set(2);

        Assert.True(operation.TryCrystallize(out var crystal));

        Assert.True(crystal.UnitShape.CompatibleWith(UnitShape.OrthogonalProduct(lengthShape, lengthShape)));
        Assert.Equal(0UL, crystal.Value);
        Assert.Equal(5UL, width.Value);
        Assert.Equal(2UL, height.Value);
    }

    [Fact]
    public void UnitShapeSurfaceReadIsSnapshotNotMutableBasis()
    {
        var lengthShape = new Carrier().UnitShape;
        var areaShape = UnitShape.Product(lengthShape, lengthShape);
        var surface = (Relation)areaShape.Surface;

        Assert.True(surface.Perturb(new CarrierLocation(Carrier.Unknown, 10)));

        Assert.Equal(2, areaShape.ProductParticipationOf(lengthShape));
    }

    [Fact]
    public void CrystallizedCarrierKeepsBasisWhenReadSurfaceIsPerturbed()
    {
        var lengthShape = new Carrier().UnitShape;
        var areaShape = UnitShape.Product(lengthShape, lengthShape);
        var crystal = new Carrier(areaShape);
        var surface = (Relation)crystal.UnitShape.Surface;

        Assert.True(surface.Perturb(new CarrierLocation(Carrier.Unknown, 10)));

        Assert.True(crystal.UnitShape.CompatibleWith(areaShape));
        Assert.Equal(2, crystal.UnitShape.ProductParticipationOf(lengthShape));
    }

    [Fact]
    public void CrystallizedCarriersSharingUnitShapeKeepIndependentValues()
    {
        var lengthShape = new Carrier().UnitShape;
        var areaShape = UnitShape.Product(lengthShape, lengthShape);
        var first = new Carrier(areaShape);
        var second = new Carrier(areaShape);

        first.Set(12);
        second.Set(3);
        first.Remove(20);

        Assert.True(first.UnitShape.CompatibleWith(second.UnitShape));
        Assert.False(first.SameIdentityAs(second));
        Assert.Equal(0UL, first.Value);
        Assert.Equal(-8, first.Tension);
        Assert.Equal(3UL, second.Value);
        Assert.Equal(0, second.Tension);
    }

    [Fact]
    public void CrystallizeFromProjectsOperationValueOntoNewCarrier()
    {
        var lengthShape = new Carrier().UnitShape;
        var width = new Carrier(lengthShape);
        var height = new Carrier(lengthShape);
        var result = new Carrier();
        var widthPort = new Relation(Point(width, 1), Point(Carrier.Unknown, 1));
        var heightPort = new Relation(Point(height, 1), Point(Carrier.Unknown, 1));
        var operation = new Relation(new Relation(widthPort, heightPort), Segment(result, 0, 1));
        width.Set(3);
        height.Set(4);

        Assert.True(operation.TryCrystallize(
            new Relation(width.ValueSlot, height.ValueSlot),
            out var crystal,
            out var read));

        Assert.True(crystal.UnitShape.CompatibleWith(UnitShape.Product(lengthShape, lengthShape)));
        Assert.Equal(12UL, crystal.Value);
        Assert.Equal(0, crystal.Tension);
        Assert.Equal(
            new Relation(
                new CarrierLocation(crystal, 12),
                new Relation(new CarrierLocation(width, 0), new CarrierLocation(height, 0))),
            read);
    }

    [Fact]
    public void CrystallizeFromReturnsOperationBasisReboundToNewCarrier()
    {
        var lengthShape = new Carrier().UnitShape;
        var width = new Carrier(lengthShape);
        var height = new Carrier(lengthShape);
        var result = new Carrier();
        var widthPort = new Relation(Point(width, 1), Point(Carrier.Unknown, 1));
        var heightPort = new Relation(Point(height, 1), Point(Carrier.Unknown, 1));
        var operation = new Relation(new Relation(widthPort, heightPort), Segment(result, 0, 10));
        width.Set(3);
        height.Set(4);

        Assert.True(operation.TryCrystallize(
            new Relation(width.ValueSlot, height.ValueSlot),
            out var crystal,
            out var basis,
            out var read));

        Assert.Equal(Segment(crystal, 0, 10), basis);
        Assert.Equal(120UL, crystal.Value);
        Assert.Equal(
            new Relation(
                new CarrierLocation(crystal, 120),
                new Relation(new CarrierLocation(width, 0), new CarrierLocation(height, 0))),
            read);
    }

    [Fact]
    public void UnknownProductOutputDerivesCoarsestParticipatingBasis()
    {
        var lengthShape = new Carrier().UnitShape;
        var width = new Carrier(lengthShape);
        var height = new Carrier(lengthShape);
        var widthPort = new Relation(Segment(width, 0, 12), Point(Carrier.Unknown, 1));
        var heightPort = new Relation(Segment(height, 0, 4), Point(Carrier.Unknown, 1));
        var operation = new Relation(new Relation(widthPort, heightPort), Segment(Carrier.Unknown, 0, 1));
        width.Set(24);
        height.Set(8);

        Assert.True(operation.TryCrystallize(
            new Relation(width.ValueSlot, height.ValueSlot),
            out var crystal,
            out var basis,
            out var read));

        Assert.Equal(Segment(crystal, 0, 4), basis);
        Assert.Equal(16UL, crystal.Value);
        Assert.Equal(
            new Relation(
                new CarrierLocation(crystal, 16),
                new Relation(new CarrierLocation(width, 0), new CarrierLocation(height, 0))),
            read);
    }

    [Fact]
    public void ExplicitProductOutputBasisOverridesCoarsestParticipatingBasis()
    {
        var lengthShape = new Carrier().UnitShape;
        var width = new Carrier(lengthShape);
        var height = new Carrier(lengthShape);
        var result = new Carrier();
        var widthPort = new Relation(Segment(width, 0, 12), Point(Carrier.Unknown, 1));
        var heightPort = new Relation(Segment(height, 0, 4), Point(Carrier.Unknown, 1));
        var operation = new Relation(new Relation(widthPort, heightPort), Segment(result, 0, 10));
        width.Set(24);
        height.Set(8);

        Assert.True(operation.TryCrystallize(
            new Relation(width.ValueSlot, height.ValueSlot),
            out var crystal,
            out var basis,
            out _));

        Assert.Equal(Segment(crystal, 0, 10), basis);
        Assert.Equal(40UL, crystal.Value);
    }

    [Fact]
    public void ConcreteOperandTargetActsAsDominantProductBasis()
    {
        var rate = new Carrier();
        var miles = new Carrier();
        var hours = new Carrier();
        var ratePort = new Relation(Point(rate, 1), Segment(miles, 0, 12));
        var hoursPort = new Relation(Segment(hours, 0, 4), Point(Carrier.Unknown, 1));
        var operation = new Relation(new Relation(ratePort, hoursPort), Segment(Carrier.Unknown, 0, 1));
        rate.Set(2);
        hours.Set(8);

        Assert.True(operation.TryCrystallize(
            new Relation(rate.ValueSlot, hours.ValueSlot),
            out var crystal,
            out var basis,
            out var read));

        Assert.Equal(Segment(crystal, 0, 12), basis);
        Assert.Equal(48UL, crystal.Value);
        Assert.Equal(
            new Relation(
                new CarrierLocation(crystal, 48),
                new Relation(new CarrierLocation(rate, 0), new CarrierLocation(hours, 0))),
            read);
    }

    [Fact]
    public void UnknownAdditiveOutputDerivesCoarsestParticipatingBasis()
    {
        var lengthShape = new Carrier().UnitShape;
        var x = new Carrier(lengthShape);
        var y = new Carrier(lengthShape);
        var xPort = new Relation(Segment(x, 0, 10), Point(Carrier.Unknown, 1));
        var yPort = new Relation(Segment(y, 0, 4), Point(Carrier.Unknown, 1));
        var operation = new Relation(new Relation(xPort, yPort), Point(Carrier.Unknown, 1));
        x.Set(20);
        y.Set(8);

        Assert.True(operation.TryCrystallize(
            new Relation(x.ValueSlot, y.ValueSlot),
            out var crystal,
            out var basis,
            out var read));

        Assert.Equal(Point(crystal, 4), basis);
        Assert.Equal(16UL, crystal.Value);
        Assert.Equal(
            new Relation(
                new CarrierLocation(crystal, 16),
                new Relation(new CarrierLocation(x, 0), new CarrierLocation(y, 0))),
            read);
    }

    [Fact]
    public void AdditiveCrystallizeReturnsPointBasisReboundToNewCarrier()
    {
        var lengthShape = new Carrier().UnitShape;
        var x = new Carrier(lengthShape);
        var y = new Carrier(lengthShape);
        var result = new Carrier();
        var xPort = new Relation(Point(x, 1), Point(Carrier.Unknown, 1));
        var yPort = new Relation(Point(y, 1), Point(Carrier.Unknown, 1));
        var operation = new Relation(new Relation(xPort, yPort), Point(result, 1));
        x.Set(3);
        y.Set(4);

        Assert.True(operation.TryCrystallize(
            new Relation(x.ValueSlot, y.ValueSlot),
            out var crystal,
            out var basis,
            out _));

        Assert.Equal(Point(crystal, 1), basis);
        Assert.Equal(7UL, crystal.Value);
        Assert.True(crystal.UnitShape.CompatibleWith(lengthShape));
    }

    [Fact]
    public void ReturnedOperationBasisIsExternalRelationNotCarrierDefault()
    {
        var lengthShape = new Carrier().UnitShape;
        var width = new Carrier(lengthShape);
        var height = new Carrier(lengthShape);
        var result = new Carrier();
        var widthPort = new Relation(Point(width, 1), Point(Carrier.Unknown, 1));
        var heightPort = new Relation(Point(height, 1), Point(Carrier.Unknown, 1));
        var operation = new Relation(new Relation(widthPort, heightPort), Segment(result, 0, 10));
        width.Set(3);
        height.Set(4);

        Assert.True(operation.TryCrystallize(
            new Relation(width.ValueSlot, height.ValueSlot),
            out var crystal,
            out var basis,
            out _));

        Assert.True(basis.Perturb(new CarrierLocation(crystal, 5)));

        Assert.Equal(120UL, crystal.Value);
        Assert.Equal(Segment(crystal, 5, 15), basis);
    }

    [Fact]
    public void CrystallizedProjectionPreservesOperationOutputBasis()
    {
        var lengthShape = new Carrier().UnitShape;
        var width = new Carrier(lengthShape);
        var height = new Carrier(lengthShape);
        var result = new Carrier();
        var widthPort = new Relation(Point(width, 1), Point(Carrier.Unknown, 1));
        var heightPort = new Relation(Point(height, 1), Point(Carrier.Unknown, 1));
        var operation = new Relation(new Relation(widthPort, heightPort), Segment(result, 0, 10));
        width.Set(3);
        height.Set(4);

        Assert.True(operation.TryCrystallize(
            new Relation(width.ValueSlot, height.ValueSlot),
            out var crystal,
            out var read));

        Assert.Equal(120UL, crystal.Value);
        Assert.Equal(
            new Relation(
                new CarrierLocation(crystal, 120),
                new Relation(new CarrierLocation(width, 0), new CarrierLocation(height, 0))),
            read);
    }

    [Fact]
    public void CrystallizedProjectionKeepsOperandResidueVisibleWithoutMutatingSources()
    {
        var lengthShape = new Carrier().UnitShape;
        var x = new Carrier(lengthShape);
        var y = new Carrier(lengthShape);
        var result = new Carrier();
        var xPort = new Relation(Segment(x, 0, 10), Point(Carrier.Unknown, 1));
        var yPort = new Relation(Segment(y, 0, 10), Point(Carrier.Unknown, 1));
        var operation = new Relation(new Relation(xPort, yPort), Point(result, 1));
        x.Set(17);
        y.Set(28);

        Assert.True(operation.TryCrystallize(
            new Relation(x.ValueSlot, y.ValueSlot),
            out var crystal,
            out var read));

        Assert.Equal(3UL, crystal.Value);
        Assert.Equal(0, x.Tension);
        Assert.Equal(0, y.Tension);
        Assert.Equal(
            new Relation(
                new CarrierLocation(crystal, 3),
                new Relation(new CarrierLocation(x, 7), new CarrierLocation(y, 8))),
            read);
    }

    [Fact]
    public void ExistingCrystalProjectionUsesDeltaSettlement()
    {
        var lengthShape = new Carrier().UnitShape;
        var width = new Carrier(lengthShape);
        var height = new Carrier(lengthShape);
        var result = new Carrier();
        var widthPort = new Relation(Point(width, 1), Point(Carrier.Unknown, 1));
        var heightPort = new Relation(Point(height, 1), Point(Carrier.Unknown, 1));
        var operation = new Relation(new Relation(widthPort, heightPort), Segment(result, 0, 1));
        Assert.True(operation.TryCrystallize(out var crystal));
        width.Set(3);
        height.Set(4);
        Assert.True(operation.TryProjectCrystallizedValueFrom(
            new Relation(width.ValueSlot, height.ValueSlot),
            crystal,
            out _));
        width.Set(5);

        Assert.True(operation.TryProjectCrystallizedValueFrom(
            new Relation(width.ValueSlot, height.ValueSlot),
            crystal,
            out var read));

        Assert.Equal(20UL, crystal.Value);
        Assert.Equal(
            new Relation(
                new CarrierLocation(crystal, 20),
                new Relation(new CarrierLocation(width, 0), new CarrierLocation(height, 0))),
            read);
    }

    [Fact]
    public void IncompatibleAdditiveCrystalDoesNotProjectAsSingleScalarValue()
    {
        var length = new Carrier();
        var heat = new Carrier();
        var result = new Carrier();
        var lengthPort = new Relation(Point(length, 1), Point(Carrier.Unknown, 1));
        var heatPort = new Relation(Point(heat, 1), Point(Carrier.Unknown, 1));
        var operation = new Relation(new Relation(lengthPort, heatPort), Point(result, 1));
        length.Set(3);
        heat.Set(4);

        Assert.True(operation.TryCrystallize(out var shapeOnlyCrystal));
        Assert.False(operation.TryCrystallize(
            new Relation(length.ValueSlot, heat.ValueSlot),
            out _,
            out _));
        Assert.False(operation.TryProjectCrystallizedValueFrom(
            new Relation(length.ValueSlot, heat.ValueSlot),
            shapeOnlyCrystal,
            out _));
        Assert.Equal(0UL, shapeOnlyCrystal.Value);
    }

    private static Relation Point(Carrier carrier, long ticks) =>
        Relation.Point(carrier, ticks);

    private static Relation Segment(Carrier carrier, long left, long right) =>
        Relation.Segment(carrier, left, right);
}
