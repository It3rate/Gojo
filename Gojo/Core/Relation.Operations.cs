namespace Gojo.Core;

public sealed partial class Relation
{
    #region Operation Surfaces and Crystallization

    /// <summary>
    /// Reads an operation surface over port input without mutating carriers.
    /// This is the current executable foothold for a broader signal-through-
    /// structure pass: input is projected through local ports into operation
    /// signals, signals retain face, residue, and provenance, then the output
    /// surface decides whether to add, distribute, preserve faces, or collapse.
    /// Future resolution, fill, settlement, and live-routing work should move
    /// toward this same signal flow instead of adding independent classifiers.
    /// </summary>
    public bool TryReadOperationFrom(IElement input, Carrier reader, out Relation read) =>
        TryReadOperationFrom(input, reader, bindOutputToReader: false, out read);

    /// <summary>Reads an operation surface through a concrete output basis.</summary>
    private bool TryReadOperationFrom(IElement input, Carrier reader, Relation outputBasis, out Relation read)
    {
        if (!TryReadFrame(outputBasis, out var targetFrame) ||
            !targetFrame.Carrier.SameIdentityAs(reader))
        {
            read = default!;
            return false;
        }

        var scaleByOutputBasis = true;
        if (TryReadFrame(Right, out var operationFrame) &&
            operationFrame.Carrier.IsUnknown &&
            TryReadExpectedOperationBasisExtent(operationFrame.Extent, out _, out var hasDominantBasis) &&
            hasDominantBasis)
        {
            scaleByOutputBasis = false;
        }

        return TryReadOperationFrom(input, reader, targetFrame, scaleByOutputBasis, out read);
    }

    /// <summary>Reads an operation surface, optionally binding output to the reader.</summary>
    private bool TryReadOperationFrom(
        IElement input,
        Carrier reader,
        bool bindOutputToReader,
        out Relation read)
    {
        if (TryReadPointFrame(Right, out var pointFrame) &&
            TryResolveOperationTargetFrame(pointFrame, reader, bindOutputToReader, out var pointTargetFrame))
        {
            return TryReadOperationSignalFlow(
                input,
                reader,
                pointTargetFrame,
                scaleByTargetFrame: true,
                out read);
        }

        if (TryReadFrame(Right, out var segmentFrame) &&
            TryResolveOperationTargetFrame(segmentFrame, reader, bindOutputToReader, out var segmentTargetFrame))
        {
            return TryReadOperationSignalFlow(
                input,
                reader,
                segmentTargetFrame,
                scaleByTargetFrame: true,
                out read);
        }

        read = default!;
        return false;
    }

    /// <summary>Reads an operation surface using an already resolved output frame.</summary>
    private bool TryReadOperationFrom(
        IElement input,
        Carrier reader,
        CarrierFrame targetFrame,
        bool scaleByOutputBasis,
        out Relation read) =>
        TryReadOperationSignalFlow(input, reader, targetFrame, scaleByOutputBasis, out read);

    /// <summary>Runs one collected operation-signal flow into the output surface.</summary>
    private bool TryReadOperationSignalFlow(
        IElement input,
        Carrier reader,
        CarrierFrame targetFrame,
        bool scaleByTargetFrame,
        out Relation read)
    {
        if (targetFrame.Extent == 0 ||
            !TryCollectOperationSignalFlow(Left, input, reader, out var flow))
        {
            read = default!;
            return false;
        }

        if (TryReadPointFrame(Right, out _))
        {
            var signals = OperationComponentsOf(flow);
            read = OperationSumResult(targetFrame, signals, scaleByTargetFrame);
            return true;
        }

        if (TryReadFrame(Right, out _))
        {
            if (flow.Factors.Any(factor => factor.Components.Length > 1))
            {
                read = OperationProductExpansion(targetFrame, flow, scaleByTargetFrame);
                return true;
            }

            var signals = OperationSignalsOf(flow);
            var result = ProductValue(signals);
            read = OperationResult(targetFrame, result, signals, scaleByTargetFrame, ProductInvertsFlow(signals));
            return true;
        }

        read = default!;
        return false;
    }

    /// <summary>Calculates the unit shape exposed by this relation surface.</summary>
    public bool TryReadUnitShape(out UnitShape unitShape)
    {
        if (TryReadOperationUnitShape(out unitShape))
        {
            return true;
        }

        if (TryReadOrthogonalUnitShape(out unitShape))
        {
            return true;
        }

        if (TryReadPortSurface(this, out var port) &&
            TryReadPortUnitShape(port, out unitShape))
        {
            return true;
        }

        if (TryReadFrame(this, out var frame))
        {
            unitShape = frame.Carrier.UnitShape;
            return true;
        }

        unitShape = null!;
        return false;
    }

    /// <summary>Reads a source-to-zero-segment surface as an orthogonal shape unit.</summary>
    private bool TryReadOrthogonalUnitShape(out UnitShape unitShape)
    {
        if (UnitShape.TryReadOrthogonalSource(this, out var source))
        {
            unitShape = UnitShape.OrthogonalOf(source);
            return true;
        }

        unitShape = null!;
        return false;
    }

    /// <summary>Creates a carrier with this relation's derived unit shape only.</summary>
    public bool TryCrystallize(out Carrier carrier)
    {
        if (!TryReadUnitShape(out var unitShape))
        {
            carrier = null!;
            return false;
        }

        carrier = new Carrier(unitShape);
        return true;
    }

    /// <summary>
    /// Crystallizes a bounded non-point frame as a filled interval carrier.
    /// The carrier records magnitude and face, not construction endpoints.
    /// </summary>
    public bool TryCrystallizeFill(out Carrier carrier)
    {
        if (!TryReadFillFrame(out var frame))
        {
            carrier = null!;
            return false;
        }

        var fill = new Carrier(frame.Carrier.UnitShape);
        if (!TryReadFillMeasureOn(fill, out var measure))
        {
            carrier = null!;
            return false;
        }

        measure.Carrier.Set(MagnitudeOf(measure.Ticks));
        carrier = measure.Carrier;
        return true;
    }

    /// <summary>Reads the current filled interval measure without crystallizing it.</summary>
    public bool TryReadFillMeasureOn(Carrier reader, out CarrierLocation measure)
    {
        if (!TryReadFillFrame(out var frame) ||
            !reader.UnitShape.CompatibleWith(frame.Carrier.UnitShape))
        {
            measure = default;
            return false;
        }

        var target = frame.Extent < 0 ? reader.Inverse() : reader;
        measure = new CarrierLocation(target, MagnitudeOf(frame.Extent));
        return true;
    }

    /// <summary>Creates a fill basis carrier without measuring the source structure.</summary>
    private Carrier CreateFillBasis(bool inverse)
    {
        var fill = new Carrier(UnitShape.OrthogonalOf(this));
        var face = inverse ? fill.Inverse() : fill;
        face.Set(1);
        return face;
    }

    /// <summary>Creates a crystal carrier and projects compatible current value into it.</summary>
    public bool TryCrystallize(IElement input, out Carrier carrier, out Relation read)
    {
        if (!TryCrystallize(input, out carrier, out _, out read))
        {
            return false;
        }

        return true;
    }

    /// <summary>Creates a crystal carrier, its operation basis, and projects compatible current value into it.</summary>
    public bool TryCrystallize(IElement input, out Carrier carrier, out Relation basis, out Relation read)
    {
        if (!TryReadUnitShape(out var unitShape))
        {
            carrier = null!;
            basis = null!;
            read = null!;
            return false;
        }

        var crystal = new Carrier(unitShape);
        if (!TryReadOperationBasisOn(crystal, out basis) ||
            !TryProjectCrystallizedValueFrom(input, crystal, out read))
        {
            carrier = null!;
            basis = null!;
            read = null!;
            return false;
        }

        carrier = crystal;
        return true;
    }

    /// <summary>Projects compatible current operation value onto an existing crystal.</summary>
    public bool TryProjectCrystallizedValueFrom(IElement input, Carrier crystal, out Relation read)
    {
        if (!TryReadUnitShape(out var unitShape) ||
            !crystal.UnitShape.CompatibleWith(unitShape) ||
            !CanProjectOperationValueAsSingleCrystal() ||
            !TryReadOperationBasisOn(crystal, out var basis) ||
            !TryReadOperationFrom(input, crystal, basis, out read) ||
            read.Left is not CarrierLocation target ||
            !target.Carrier.SameIdentityAs(crystal))
        {
            read = null!;
            return false;
        }

        crystal.Set(target.Ticks);
        return true;
    }

    /// <summary>Reads this operation's output basis rebound onto a concrete result carrier.</summary>
    public bool TryReadOperationBasisOn(Carrier carrier, out Relation basis)
    {
        var operands = new List<UnitShape>();
        if (!TryCollectOperationUnitShapes(Left, operands) ||
            operands.Count == 0)
        {
            basis = null!;
            return false;
        }

        if (TryReadPointFrame(Right, out var pointFrame) &&
            pointFrame.Extent != 0)
        {
            var extent = pointFrame.Carrier.IsUnknown &&
                TryReadExpectedOperationBasisExtent(pointFrame.Extent, out var expectedExtent)
                    ? expectedExtent
                    : pointFrame.Extent;
            basis = Point(carrier, extent);
            return true;
        }

        if (TryReadFrame(Right, out var segmentFrame) &&
            segmentFrame.Extent != 0)
        {
            var extent = segmentFrame.Carrier.IsUnknown &&
                TryReadExpectedOperationBasisExtent(segmentFrame.Extent, out var expectedExtent)
                    ? expectedExtent
                    : segmentFrame.Extent;
            basis = Segment(carrier, segmentFrame.Start, checked(segmentFrame.Start + extent));
            return true;
        }

        basis = null!;
        return false;
    }

    #endregion

    #region Operation and Unit Shape Helpers

    /// <summary>Collects one normalized signal flow from input through the operation ports.</summary>
    private static bool TryCollectOperationSignalFlow(
        IElement portSurface,
        IElement input,
        Carrier reader,
        out OperationSignalFlow flow)
    {
        var factors = new List<OperationSignalFactor>();
        if (!TryCollectOperationSignalFactors(portSurface, input, reader, factors) ||
            factors.Count == 0)
        {
            flow = default;
            return false;
        }

        flow = new OperationSignalFlow([.. factors]);
        return true;
    }

    /// <summary>Collects signal factors, expanding fused carrier-face inputs locally.</summary>
    private static bool TryCollectOperationSignalFactors(
        IElement portSurface,
        IElement input,
        Carrier reader,
        List<OperationSignalFactor> factors)
    {
        if (TryReadPortSurface(portSurface, out var port))
        {
            return TryCollectOperationSignalFactor(port, input, reader, factors);
        }

        if (input is Relation inputGroup &&
            portSurface is Relation portGroup)
        {
            return TryCollectOperationSignalFactors(portGroup.Left, inputGroup.Left, reader, factors) &&
                TryCollectOperationSignalFactors(portGroup.Right, inputGroup.Right, reader, factors);
        }

        return false;
    }

    /// <summary>Collects one operation factor from a port and input element.</summary>
    private static bool TryCollectOperationSignalFactor(
        Relation port,
        IElement input,
        Carrier reader,
        List<OperationSignalFactor> factors)
    {
        if (TryReadFusedCarrierFaces(input, out var inverseFace, out var positiveFace))
        {
            if (!TryReadOperationSignal(port, inverseFace, reader, out var inverseSignal) ||
                !TryReadOperationSignal(port, positiveFace, reader, out var positiveSignal))
            {
                return false;
            }

            factors.Add(new OperationSignalFactor([inverseSignal, positiveSignal]));
            return true;
        }

        var signals = new List<OperationSignal>();
        if (!TryCollectOperationSignal(port, input, reader, signals) ||
            signals.Count == 0)
        {
            return false;
        }

        factors.Add(new OperationSignalFactor([.. signals]));
        return true;
    }

    /// <summary>Collects one operation signal from a port and input element.</summary>
    private static bool TryCollectOperationSignal(
        Relation port,
        IElement input,
        Carrier reader,
        List<OperationSignal> signals)
    {
        if (input is CarrierLocation location)
        {
            return TryCollectOperationSignal(port, location, reader, signals);
        }

        if (input is Relation &&
            TryReadAtomicFrameInput(input, port.Left, out var inputExtent))
        {
            return TryCollectOperationSignal(port, inputExtent, reader, signals);
        }

        return false;
    }

    /// <summary>Projects a concrete location through one operation signal port.</summary>
    private static bool TryCollectOperationSignal(
        Relation port,
        CarrierLocation input,
        Carrier reader,
        List<OperationSignal> signals)
    {
        if (!TryReadOperationSignal(port, input, reader, out var signal))
        {
            return false;
        }

        signals.Add(signal);
        return true;
    }

    /// <summary>Projects a concrete input location into one operation signal.</summary>
    private static bool TryReadOperationSignal(
        Relation port,
        CarrierLocation input,
        Carrier reader,
        out OperationSignal signal)
    {
        if (!TryProjectOperationSignalThrough(port.Left, port.Right, input, reader, out var projection))
        {
            signal = default;
            return false;
        }

        signal = new OperationSignal(
            projection.Target.Ticks,
            projection.Residue,
            OperationSignalInvertsFlow(port.Left, input.Carrier),
            input);
        return true;
    }

    /// <summary>Reads a relation as one positive and one opposed face of the same carrier.</summary>
    internal static bool TryReadFusedCarrierFaces(
        IElement fused,
        out CarrierLocation inverseFace,
        out CarrierLocation positiveFace)
    {
        if (fused is Relation relation &&
            TryReadFaceComponent(relation.Left, out var left) &&
            TryReadFaceComponent(relation.Right, out var right) &&
            TryReadOpposedFaces(left.Carrier, right.Carrier, out var inverseCarrier, out var positiveCarrier))
        {
            inverseFace = left.Carrier.SameIdentityAs(inverseCarrier) ? left : right;
            positiveFace = left.Carrier.SameIdentityAs(positiveCarrier) ? left : right;
            return true;
        }

        inverseFace = default;
        positiveFace = default;
        return false;
    }

    /// <summary>Reads a face component from a direct location or grounded point relation.</summary>
    private static bool TryReadFaceComponent(IElement element, out CarrierLocation location)
    {
        if (element is CarrierLocation direct)
        {
            location = direct;
            return true;
        }

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

    /// <summary>Finds the positive and opposed carrier faces when both are present.</summary>
    private static bool TryReadOpposedFaces(
        Carrier first,
        Carrier second,
        out Carrier inverse,
        out Carrier positive)
    {
        if (first.IsInverse == second.IsInverse)
        {
            inverse = null!;
            positive = null!;
            return false;
        }

        inverse = first.IsInverse ? first : second;
        positive = first.IsInverse ? second : first;
        return inverse.ExistingInverse is { } inversePositive && inversePositive.SameIdentityAs(positive) ||
            positive.ExistingInverse is { } positiveInverse && positiveInverse.SameIdentityAs(inverse);
    }

    /// <summary>Projects an operation signal through its exposed port basis into the operation reader.</summary>
    private static bool TryProjectOperationSignalThrough(
        IElement sourceSurface,
        IElement targetSurface,
        CarrierLocation input,
        Carrier reader,
        out SurfaceProjection projection)
    {
        if (TryProjectThrough(sourceSurface, targetSurface, input, reader, out projection))
        {
            return true;
        }

        if (!TryReadFrame(sourceSurface, out var sourceFrame) ||
            !TryReadFrame(targetSurface, out var targetFrame))
        {
            projection = default;
            return false;
        }

        if (FrameAcceptsProjectionSource(sourceFrame, input.Carrier))
        {
            projection = ProjectOperationSignalBetween(
                input,
                reader,
                ResolveProjectionSourceFrame(sourceFrame, input.Carrier),
                targetFrame);
            return true;
        }

        if (!targetFrame.Carrier.IsUnknown &&
            FrameAcceptsProjectionSource(targetFrame, input.Carrier))
        {
            projection = ProjectOperationSignalBetween(
                input,
                reader,
                ResolveProjectionSourceFrame(targetFrame, input.Carrier),
                sourceFrame);
            return true;
        }

        projection = default;
        return false;
    }

    /// <summary>Projects a signal into the operation reader while preserving the exposed target basis extent.</summary>
    private static SurfaceProjection ProjectOperationSignalBetween(
        CarrierLocation input,
        Carrier reader,
        CarrierFrame source,
        CarrierFrame target)
    {
        var projected = ProjectBetween(
            input,
            reader,
            source,
            new CarrierFrame(reader, target.Start, target.End));
        return projected.TryGetCarrierLocations(out var targetLocation, out var residue)
            ? new SurfaceProjection(targetLocation, residue)
            : default;
    }

    /// <summary>Projects input to a concrete target surface carrier.</summary>
    private static bool TryProjectToTargetSurface(
        IElement sourceSurface,
        IElement targetSurface,
        CarrierLocation input,
        out SurfaceProjection projection)
    {
        if (!TryReadFrame(targetSurface, out var targetFrame) ||
            targetFrame.Carrier.IsUnknown)
        {
            projection = default;
            return false;
        }

        return TryProjectThrough(sourceSurface, targetSurface, input, targetFrame.Carrier, out projection);
    }

    /// <summary>Projects input through surfaces and returns target plus residue slots.</summary>
    private static bool TryProjectThrough(
        IElement sourceSurface,
        IElement targetSurface,
        CarrierLocation input,
        Carrier reader,
        out SurfaceProjection projection)
    {
        if (!TryProjectBetweenSurfaces(sourceSurface, targetSurface, input, reader, out var projected) ||
            !projected.TryGetCarrierLocations(out var target, out var residue) ||
            !target.Carrier.SameIdentityAs(reader))
        {
            projection = default;
            return false;
        }

        projection = new SurfaceProjection(target, residue);
        return true;
    }

    /// <summary>Reads relation-shaped input as an atomic extent relative to a source frame.</summary>
    private static bool TryReadAtomicFrameInput(
        IElement input,
        IElement sourceSurface,
        out CarrierLocation inputExtent)
    {
        if (!TryReadFrame(input, out var inputFrame) ||
            !TryReadFrame(sourceSurface, out var sourceFrame))
        {
            inputExtent = default;
            return false;
        }

        inputExtent = new CarrierLocation(inputFrame.Carrier, checked(sourceFrame.Start + inputFrame.Extent));
        return true;
    }

    /// <summary>Reads an exposed port surface as source frame to target frame.</summary>
    private static bool TryReadPortSurface(IElement element, out Relation port)
    {
        if (element is Relation relation &&
            !TryReadFrame(relation, out _) &&
            TryReadFrame(relation.Left, out _) &&
            TryReadFrame(relation.Right, out _))
        {
            port = relation;
            return true;
        }

        port = null!;
        return false;
    }

    /// <summary>Reads whether this signal participates through an inverse-flow face.</summary>
    private static bool OperationSignalInvertsFlow(IElement sourceSurface, Carrier inputCarrier) =>
        inputCarrier.IsInverse ||
        (TryReadFrame(sourceSurface, out var sourceFrame) && sourceFrame.Carrier.IsInverse);

    /// <summary>Calculates operation-result unit shape from operand port shapes.</summary>
    private bool TryReadOperationUnitShape(out UnitShape unitShape)
    {
        var operands = new List<UnitShape>();
        if (!TryCollectOperationUnitShapes(Left, operands) ||
            operands.Count == 0)
        {
            unitShape = null!;
            return false;
        }

        if (TryReadPointFrame(Right, out var pointFrame) &&
            pointFrame.Extent != 0)
        {
            unitShape = UnitShape.AlignedProduct(operands);
            return true;
        }

        if (TryReadFrame(Right, out var segmentFrame) &&
            segmentFrame.Extent != 0)
        {
            unitShape = UnitShape.OrthogonalProduct(operands);
            return true;
        }

        unitShape = null!;
        return false;
    }

    /// <summary>Collects unit shapes from a grouped operation port surface.</summary>
    private static bool TryCollectOperationUnitShapes(
        IElement portSurface,
        List<UnitShape> unitShapes)
    {
        if (TryReadPortSurface(portSurface, out var port))
        {
            if (!TryReadPortUnitShape(port, out var unitShape))
            {
                return false;
            }

            unitShapes.Add(unitShape);
            return true;
        }

        if (portSurface is Relation portGroup)
        {
            return TryCollectOperationUnitShapes(portGroup.Left, unitShapes) &&
                TryCollectOperationUnitShapes(portGroup.Right, unitShapes);
        }

        return false;
    }

    /// <summary>Reads unit shape exposed by a port, with concrete target winning.</summary>
    private static bool TryReadPortUnitShape(Relation port, out UnitShape unitShape)
    {
        if (!TryReadFrame(port.Left, out var sourceFrame) ||
            !TryReadFrame(port.Right, out var targetFrame))
        {
            unitShape = null!;
            return false;
        }

        unitShape = targetFrame.Carrier.IsUnknown
            ? sourceFrame.Carrier.UnitShape
            : targetFrame.Carrier.UnitShape;
        return true;
    }

    /// <summary>Reads the expected output basis extent from exposed operation ports.</summary>
    private bool TryReadExpectedOperationBasisExtent(long outputExtent, out long extent) =>
        TryReadExpectedOperationBasisExtent(outputExtent, out extent, out _);

    /// <summary>Reads the expected output basis extent and whether a concrete operand target supplied it.</summary>
    private bool TryReadExpectedOperationBasisExtent(long outputExtent, out long extent, out bool hasDominantBasis)
    {
        var bases = new List<OperationBasis>();
        if (!TryCollectOperationBases(Left, bases) ||
            bases.Count == 0)
        {
            extent = 0;
            hasDominantBasis = false;
            return false;
        }

        hasDominantBasis = bases.Any(candidate => candidate.IsDominant);
        var candidates = hasDominantBasis
            ? bases.Where(candidate => candidate.IsDominant)
            : bases;
        var magnitude = candidates
            .Select(candidate => MagnitudeOf(candidate.Extent))
            .Where(value => value != 0)
            .DefaultIfEmpty(MagnitudeOf(outputExtent))
            .Min();
        var sign = Math.Sign(outputExtent);
        extent = SignedTicks(magnitude, sign == 0 ? 1 : sign);
        return true;
    }

    /// <summary>Collects basis extents exposed by an operation port surface.</summary>
    private static bool TryCollectOperationBases(
        IElement portSurface,
        List<OperationBasis> bases)
    {
        if (TryReadPortSurface(portSurface, out var port))
        {
            if (!TryReadPortBasis(port, out var basis))
            {
                return false;
            }

            bases.Add(basis);
            return true;
        }

        if (portSurface is Relation portGroup)
        {
            return TryCollectOperationBases(portGroup.Left, bases) &&
                TryCollectOperationBases(portGroup.Right, bases);
        }

        return false;
    }

    /// <summary>Reads the exposed basis of a port, with concrete target frames acting as dominant bases.</summary>
    private static bool TryReadPortBasis(Relation port, out OperationBasis basis)
    {
        if (!TryReadFrame(port.Left, out var sourceFrame) ||
            !TryReadFrame(port.Right, out var targetFrame))
        {
            basis = default;
            return false;
        }

        basis = targetFrame.Carrier.IsUnknown
            ? new OperationBasis(sourceFrame.Extent, IsDominant: false)
            : new OperationBasis(targetFrame.Extent, IsDominant: true);
        return true;
    }

    /// <summary>Builds operation read output with target value and residue chain.</summary>
    private static Relation OperationResult(
        CarrierFrame targetFrame,
        long value,
        List<OperationSignal> signals,
        bool scaleByTargetFrame = true,
        bool invertTargetFace = false)
    {
        var targetCarrier = invertTargetFace
            ? targetFrame.Carrier.Inverse()
            : targetFrame.Carrier;
        return new Relation(
            new CarrierLocation(
                targetCarrier,
                checked(targetFrame.Start + (scaleByTargetFrame ? checked(value * targetFrame.Extent) : value))),
            OperationResidue(signals));
    }

    /// <summary>Adds aligned signals while preserving opposed-face participation.</summary>
    private static Relation OperationSumResult(
        CarrierFrame targetFrame,
        IReadOnlyList<OperationSignal> signals,
        bool scaleByTargetFrame)
    {
        if (!signals.Any(signal => signal.InvertsFlow))
        {
            return OperationResult(
                targetFrame,
                signals.Aggregate(0L, (ticks, signal) => checked(ticks + signal.Ticks)),
                [.. signals],
                scaleByTargetFrame);
        }

        var positive = signals
            .Where(signal => !signal.InvertsFlow)
            .Aggregate(0L, (ticks, signal) => checked(ticks + signal.Ticks));
        var inverse = signals
            .Where(signal => signal.InvertsFlow)
            .Aggregate(0L, (ticks, signal) => checked(ticks + signal.Ticks));

        IElement target = positive != 0
            ? new Relation(
                OperationFaceLocation(targetFrame, inverse, invertTargetFace: true, scaleByTargetFrame),
                OperationFaceLocation(targetFrame, positive, invertTargetFace: false, scaleByTargetFrame))
            : OperationFaceLocation(targetFrame, inverse, invertTargetFace: true, scaleByTargetFrame);

        return new Relation(target, OperationResidue(signals));
    }

    /// <summary>Projects one face of an aligned operation result into the target frame.</summary>
    private static CarrierLocation OperationFaceLocation(
        CarrierFrame targetFrame,
        long value,
        bool invertTargetFace,
        bool scaleByTargetFrame)
    {
        var targetCarrier = invertTargetFace && !targetFrame.Carrier.IsUnknown
            ? targetFrame.Carrier.Inverse()
            : targetFrame.Carrier;
        return new CarrierLocation(
            targetCarrier,
            checked(targetFrame.Start + (scaleByTargetFrame ? checked(value * targetFrame.Extent) : value)));
    }

    /// <summary>Builds visible product terms for fused carrier-face factors.</summary>
    private static Relation OperationProductExpansion(
        CarrierFrame targetFrame,
        OperationSignalFlow flow,
        bool scaleByTargetFrame)
    {
        var terms = new List<IElement>();
        var residues = new List<IElement>();
        var combination = new OperationSignal[flow.Factors.Length];
        CollectProductExpansionTerms(
            targetFrame,
            flow.Factors,
            scaleByTargetFrame,
            factorIndex: 0,
            combination,
            terms,
            residues);

        return new Relation(
            OrderedGroup(new Carrier(), [.. terms]),
            OrderedGroup(new Carrier(), [.. residues]));
    }

    /// <summary>Recursively distributes product factors into product-term structure.</summary>
    private static void CollectProductExpansionTerms(
        CarrierFrame targetFrame,
        IReadOnlyList<OperationSignalFactor> factors,
        bool scaleByTargetFrame,
        int factorIndex,
        OperationSignal[] combination,
        List<IElement> terms,
        List<IElement> residues)
    {
        if (factorIndex == factors.Count)
        {
            var termSignals = combination.ToArray();
            var value = termSignals.Aggregate(1L, (ticks, signal) => checked(ticks * signal.Ticks));
            var targetCarrier = ProductTermInvertsFlow(termSignals) && !targetFrame.Carrier.IsUnknown
                ? targetFrame.Carrier.Inverse()
                : targetFrame.Carrier;
            var targetTicks = checked(targetFrame.Start + (scaleByTargetFrame ? checked(value * targetFrame.Extent) : value));
            terms.Add(new Relation(
                new CarrierLocation(targetCarrier, targetTicks),
                OperationSource(termSignals)));
            residues.Add(OperationResidue(termSignals));
            return;
        }

        foreach (var component in factors[factorIndex].Components)
        {
            combination[factorIndex] = component;
            CollectProductExpansionTerms(
                targetFrame,
                factors,
                scaleByTargetFrame,
                factorIndex + 1,
                combination,
                terms,
                residues);
        }
    }

    /// <summary>Product terms preserve opposed-face participation before scalar fold.</summary>
    private static bool ProductTermInvertsFlow(IReadOnlyList<OperationSignal> signals) =>
        signals.Any(signal => signal.InvertsFlow);

    /// <summary>Flattens every collected factor component for aligned addition.</summary>
    private static List<OperationSignal> OperationComponentsOf(OperationSignalFlow flow) =>
        [.. flow.Factors.SelectMany(factor => factor.Components)];

    /// <summary>Flattens single-component product factors back to ordinary scalar signals.</summary>
    private static List<OperationSignal> OperationSignalsOf(OperationSignalFlow flow) =>
        [.. flow.Factors.Select(factor => factor.Components[0])];

    /// <summary>Product output flips face when an odd number of signals are inverse-flow.</summary>
    private static bool ProductInvertsFlow(IReadOnlyList<OperationSignal> signals) =>
        signals.Count(signal => signal.InvertsFlow) % 2 != 0;

    /// <summary>Multiplies signal values while preserving inverse-flow pair negation.</summary>
    private static long ProductValue(IReadOnlyList<OperationSignal> signals)
    {
        var result = signals.Aggregate(1L, (ticks, signal) => checked(ticks * signal.Ticks));
        var inversePairs = signals.Count(signal => signal.InvertsFlow) / 2;
        return inversePairs % 2 == 0 ? result : checked(-result);
    }

    /// <summary>Chains signal source locations in operation signal order.</summary>
    private static IElement OperationSource(IReadOnlyList<OperationSignal> signals)
    {
        IElement source = signals[^1].Source;
        for (var index = signals.Count - 2; index >= 0; index--)
        {
            source = new Relation(signals[index].Source, source);
        }

        return source;
    }

    /// <summary>Chains signal residues in operation signal order.</summary>
    private static IElement OperationResidue(IReadOnlyList<OperationSignal> signals)
    {
        IElement residue = signals[^1].Residue;
        for (var index = signals.Count - 2; index >= 0; index--)
        {
            residue = new Relation(signals[index].Residue, residue);
        }

        return residue;
    }

    /// <summary>Finds the concrete structural coordinate retargeted by a surface.</summary>
    private static bool TryReadStructuralTarget(IElement element, out CarrierLocation targetSite)
    {
        if (element is CarrierLocation location)
        {
            targetSite = location;
            return true;
        }

        if (element is not Relation relation)
        {
            targetSite = default;
            return false;
        }

        if (relation.IsPoint(out var pointLocation, out _))
        {
            targetSite = pointLocation;
            return true;
        }

        if (relation.TryGetCarrierLocations(out _, out var right))
        {
            targetSite = right;
            return true;
        }

        if (relation.Left is Relation leftPoint &&
            relation.Right is Relation rightPoint &&
            leftPoint.IsPoint(out var leftPointLocation, out _) &&
            rightPoint.IsPoint(out var rightPointLocation, out _) &&
            leftPointLocation.Carrier.SameIdentityAs(rightPointLocation.Carrier))
        {
            targetSite = rightPointLocation;
            return true;
        }

        targetSite = default;
        return false;
    }

    /// <summary>Returns true when a frame accepts the requested concrete carrier.</summary>
    private static bool FrameAccepts(CarrierFrame frame, Carrier carrier) =>
        frame.Carrier.IsUnknown || carrier.SameIdentityAs(frame.Carrier);

    /// <summary>Returns true when an origin basis can read a same-unit source carrier.</summary>
    private static bool FrameAcceptsProjectionSource(CarrierFrame frame, Carrier carrier) =>
        FrameAccepts(frame, carrier) ||
        (frame.Start == 0 &&
            !frame.Carrier.IsUnknown &&
            frame.Carrier.UnitShape.CompatibleWith(carrier.UnitShape));

    /// <summary>Replaces Unknown frame carrier with the concrete carrier for a read.</summary>
    private static CarrierFrame ResolveFrameCarrier(CarrierFrame frame, Carrier carrier) =>
        frame.Carrier.IsUnknown
            ? new CarrierFrame(carrier, frame.Start, frame.End)
            : frame;

    /// <summary>Rebases a projection source frame onto a compatible concrete carrier.</summary>
    private static CarrierFrame ResolveProjectionSourceFrame(CarrierFrame frame, Carrier carrier) =>
        frame.Carrier.IsUnknown || !carrier.SameIdentityAs(frame.Carrier)
            ? new CarrierFrame(carrier, frame.Start, frame.End)
            : frame;

    /// <summary>Resolves operation output frame, optionally binding it to the reader.</summary>
    private static bool TryResolveOperationTargetFrame(
        CarrierFrame frame,
        Carrier reader,
        bool bindOutputToReader,
        out CarrierFrame targetFrame)
    {
        if (bindOutputToReader)
        {
            targetFrame = new CarrierFrame(reader, frame.Start, frame.End);
            return true;
        }

        if (!FrameAccepts(frame, reader))
        {
            targetFrame = default;
            return false;
        }

        targetFrame = ResolveFrameCarrier(frame, reader);
        return true;
    }

    /// <summary>Allows scalar crystal value projection only for currently reducible shapes.</summary>
    private bool CanProjectOperationValueAsSingleCrystal()
    {
        var operands = new List<UnitShape>();
        if (!TryCollectOperationUnitShapes(Left, operands) ||
            operands.Count == 0)
        {
            return false;
        }

        if (TryReadPointFrame(Right, out var pointFrame) &&
            pointFrame.Extent != 0)
        {
            var first = operands[0];
            return operands.All(first.CompatibleWith);
        }

        return TryReadFrame(Right, out var segmentFrame) &&
            segmentFrame.Extent != 0;
    }

    #endregion
}
