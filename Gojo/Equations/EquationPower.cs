using Gojo.Core;
using Gojo.Structures;

namespace Gojo.Equations;

/// <summary>
/// Repeated-product equation foothold built as a repeat surface. Literal count
/// controllers can be read as powers today; non-literal controllers remain
/// inspectable repeat structure.
/// </summary>
public static class EquationPower
{
    /// <summary>Creates x^exponent as a repeat surface over a unit input port.</summary>
    public static Relation Create(
        Carrier root,
        Carrier input,
        Carrier output,
        int exponent) =>
        Create(root, input, output, RepeatSurface.Count(RepeatSurface.CreateAxis(new Carrier()), exponent));

    /// <summary>Creates a power-like repeat surface with an explicit controller.</summary>
    public static Relation Create(
        Carrier root,
        Carrier input,
        Carrier output,
        IElement controller)
    {
        var inputCarrier = RootOrUnknown(input);
        var outputCarrier = RootOrUnknown(output);
        var selected = Equation.UnitPort(inputCarrier);
        var transition = ProductTransition(inputCarrier, outputCarrier, ReadResolvedExponent(controller));
        return RepeatSurface.Create(
            RootOrNew(root),
            selected,
            controller ?? Relation.Point(Carrier.Unknown, 0),
            transition,
            Relation.Point(outputCarrier, 0),
            Relation.Point(Carrier.Unknown, 0));
    }

    /// <summary>Reads one input through a power repeat surface.</summary>
    public static Relation Predict(
        Relation power,
        CarrierLocation input,
        Carrier output,
        out Relation repeatRead)
    {
        if (!TryReadPowerSurface(
                power,
                out var selected,
                out var controller,
                out _,
                out _,
                out _,
                out _,
                out _,
                out var surfaceOutput))
        {
            repeatRead = power ?? Relation.Point(Carrier.Unknown, 0);
            return new Relation(new CarrierLocation(RootOrUnknown(output), 0), input);
        }

        var outputCarrier = surfaceOutput.IsUnknown ? RootOrUnknown(output) : surfaceOutput;
        if (!TryReadResolvedExponent(controller, input, out var exponent, out var controllerEvidence))
        {
            repeatRead = power;
            return new Relation(new CarrierLocation(outputCarrier, 0), input);
        }

        var inputCarrier = input.Carrier;
        var readTransition = ProductTransition(inputCarrier, outputCarrier, exponent);
        var factorCarriers = CreateFactorCarriers(inputCarrier.UnitShape, exponent);
        var factorBranches = new Relation[exponent];
        var productPorts = new Relation[exponent];
        var stepReads = new Relation[exponent];
        var timelineSteps = new Relation[exponent];
        IElement previous = Relation.Point(Carrier.Unknown, 1);

        for (var index = 0; index < exponent; index++)
        {
            factorBranches[index] = Equation.Affine(inputCarrier, 0, 1, factorCarriers[index], 0, 1);
            productPorts[index] = Equation.UnitPort(factorCarriers[index]);
            var factorRead = Equation.Predict(factorBranches[index], input, factorCarriers[index]);
            stepReads[index] = factorRead;
            var partial = PartialProductLocation(outputCarrier, input.Ticks, index + 1);
            timelineSteps[index] = TimeStepSequence.Step(
                new Carrier(),
                inputCarrier,
                index,
                index + 1,
                previous,
                factorBranches[index],
                partial,
                factorRead.Right);
            previous = partial;
        }

        var prediction = exponent == 0
            ? new Relation(new CarrierLocation(outputCarrier, 1), input)
            : Equation.Predict(Equation.Product(outputCarrier, productPorts), ProductInput(stepReads), outputCarrier);
        var feedback = exponent == 0
            ? Relation.OrderedGroup(new Carrier(), Relation.Point(outputCarrier, 1))
            : Relation.OrderedGroup(new Carrier(), [.. stepReads.Select(read => read.Left)]);
        var timeline = TimeStepSequence.Create(
            new Carrier(),
            inputCarrier,
            PowerTimelineInitial(outputCarrier, controllerEvidence),
            timelineSteps);

        repeatRead = RepeatSurface.Create(
            new Carrier(),
            selected,
            controller,
            readTransition,
            feedback,
            prediction.Right,
            timeline);
        return prediction;
    }

    /// <summary>Attempts to read and predict a power surface while keeping the repeat read available.</summary>
    public static bool TryPredict(
        Relation power,
        IElement input,
        Carrier output,
        out Relation prediction,
        out Relation repeatRead)
    {
        if (!TryReadPowerSurface(
                power,
                out _,
                out _,
                out _,
                out _,
                out _,
                out _,
                out _,
                out var surfaceOutput))
        {
            prediction = null!;
            repeatRead = null!;
            return false;
        }

        if (input is CarrierLocation location)
        {
            prediction = Predict(power, location, output, out repeatRead);
            return true;
        }

        var target = surfaceOutput.IsUnknown ? RootOrUnknown(output) : surfaceOutput;
        repeatRead = power;
        prediction = new Relation(new CarrierLocation(target, 0), input ?? Relation.Point(Carrier.Unknown, 0));
        return true;
    }

    /// <summary>Attempts to read and predict a power surface.</summary>
    public static bool TryPredict(
        Relation power,
        IElement input,
        Carrier output,
        out Relation prediction) =>
        TryPredict(power, input, output, out prediction, out _);

    /// <summary>Reads the repeat surface fields for inspection.</summary>
    public static bool TryRead(
        Relation power,
        out IElement selected,
        out IElement controller,
        out IElement transition,
        out IElement feedback,
        out IElement residue,
        out IElement timeline) =>
        RepeatSurface.TryRead(power, out selected, out controller, out transition, out feedback, out residue, out timeline);

    /// <summary>Reads the resolved controller evidence attached to a power repeat read.</summary>
    public static bool TryReadControllerEvidence(Relation powerRead, out Relation evidence)
    {
        if (TryRead(powerRead, out _, out _, out _, out _, out _, out var timeline) &&
            timeline is Relation timelineRelation &&
            TimeStepSequence.TryRead(timelineRelation, out _, out var initial, out _) &&
            TryReadPowerTimelineInitial(initial, out _, out var controllerEvidence))
        {
            evidence = controllerEvidence;
            return true;
        }

        evidence = null!;
        return false;
    }

    /// <summary>Reads the resolved controller evidence fields from a power repeat read.</summary>
    public static bool TryReadControllerEvidence(
        Relation powerRead,
        out IElement controller,
        out CarrierLocation count,
        out Relation span,
        out IElement reason,
        out IElement residue)
    {
        if (TryReadControllerEvidence(powerRead, out var evidence) &&
            TryReadControllerEvidenceFields(evidence, out controller, out count, out span, out reason, out residue))
        {
            return true;
        }

        controller = default!;
        count = default;
        span = null!;
        reason = default!;
        residue = default!;
        return false;
    }

    /// <summary>Reads the product identity and controller evidence from a power timeline initial frame.</summary>
    public static bool TryReadPowerTimelineInitial(
        IElement initial,
        out IElement productIdentity,
        out Relation controllerEvidence)
    {
        if (initial is Relation structure &&
            TryReadRootElements(structure, out var elements) &&
            elements.Length >= 2 &&
            elements[1] is Relation evidence &&
            TryReadControllerEvidenceFields(evidence, out _, out _, out _, out _, out _))
        {
            productIdentity = elements[0];
            controllerEvidence = evidence;
            return true;
        }

        productIdentity = default!;
        controllerEvidence = null!;
        return false;
    }

    /// <summary>Reads controller evidence as controller, resolved count, resolved span, evidence, and residue.</summary>
    public static bool TryReadControllerEvidenceFields(
        Relation evidence,
        out IElement controller,
        out CarrierLocation count,
        out Relation span,
        out IElement reason,
        out IElement residue)
    {
        if (TryReadRootElements(evidence, out var elements) &&
            elements.Length >= 5 &&
            elements[1] is CarrierLocation countLocation &&
            elements[2] is Relation spanRelation)
        {
            controller = elements[0];
            count = countLocation;
            span = spanRelation;
            reason = elements[3];
            residue = elements[4];
            return true;
        }

        controller = default!;
        count = default;
        span = null!;
        reason = default!;
        residue = default!;
        return false;
    }

    /// <summary>Reads a repeat surface only when its structure is power-like.</summary>
    public static bool TryReadPowerSurface(
        Relation power,
        out IElement selected,
        out IElement controller,
        out IElement transition,
        out IElement feedback,
        out IElement residue,
        out IElement timeline,
        out Carrier input,
        out Carrier output)
    {
        if (TryRead(power, out selected, out controller, out transition, out feedback, out residue, out timeline) &&
            TryReadUnitPort(selected, out input) &&
            TryReadTransitionOutput(transition, controller, out output))
        {
            return true;
        }

        selected = default!;
        controller = default!;
        transition = default!;
        feedback = default!;
        residue = default!;
        timeline = default!;
        input = null!;
        output = null!;
        return false;
    }

    /// <summary>
    /// Builds a controller whose count is read from another equation surface.
    /// The controller keeps the source equation, supplied input, prediction,
    /// count span, and read residue visible as ordinary relation structure.
    /// </summary>
    public static Relation CountFromEquation(
        Carrier root,
        Carrier axis,
        Relation equation,
        IElement input,
        Carrier countOutput)
    {
        var countAxis = RootOrNew(axis);
        var countCarrier = RootOrUnknown(countOutput);
        var supplied = input ?? Relation.Point(Carrier.Unknown, 0);
        var countEquation = equation ?? Relation.Point(Carrier.Unknown, 0);
        var prediction = IsUnknownPoint(supplied)
            ? Relation.Point(Carrier.Unknown, 0)
            : Equation.Predict(countEquation, supplied, countCarrier);
        var span = !IsUnknownPoint(supplied) && TryReadPredictionCount(prediction, out var ticks)
            ? RepeatSurface.Count(countAxis, ticks)
            : Relation.Point(Carrier.Unknown, 0);

        return Relation.OrderedGroup(
            RootOrNew(root),
            Relation.Point(countAxis, 0),
            countEquation,
            supplied,
            countCarrier,
            prediction,
            span);
    }

    /// <summary>Builds an equation-derived controller whose input is supplied by the eventual power read.</summary>
    public static Relation CountFromEquation(
        Carrier root,
        Carrier axis,
        Relation equation,
        Carrier countOutput) =>
        CountFromEquation(root, axis, equation, Relation.Point(Carrier.Unknown, 0), countOutput);

    /// <summary>
    /// Builds a controller whose count is the selected input from a resolution
    /// search. The selected sample remains visible inside the controller.
    /// </summary>
    public static Relation CountFromResolutionSearch(
        Carrier root,
        Carrier axis,
        Relation search)
    {
        var countAxis = RootOrNew(axis);
        var searchElement = search ?? Relation.Point(Carrier.Unknown, 0);
        var selected = TryReadLastResolutionSample(searchElement, out var selectedSample)
            ? selectedSample
            : Relation.Point(Carrier.Unknown, 0);
        var span = TryReadResolutionSampleCount(selected, out var ticks)
            ? RepeatSurface.Count(countAxis, ticks)
            : Relation.Point(Carrier.Unknown, 0);

        return Relation.OrderedGroup(
            RootOrNew(root),
            Relation.Point(countAxis, 0),
            searchElement,
            selected,
            span);
    }

    /// <summary>Reads a literal exponent from a controller span.</summary>
    public static bool TryReadLiteralExponent(IElement controller, out int exponent)
    {
        if (RepeatSurface.TryReadSpan(controller, out var start, out var end) &&
            start.Ticks == 0 &&
            end.Ticks >= 0 &&
            end.Ticks <= int.MaxValue)
        {
            exponent = checked((int)end.Ticks);
            return true;
        }

        exponent = 0;
        return false;
    }

    /// <summary>Reads a nonnegative exponent from any currently supported controller shape.</summary>
    public static bool TryReadResolvedExponent(IElement controller, out int exponent) =>
        TryReadResolvedExponent(controller, suppliedInput: null, out exponent, out _);

    private static bool TryReadResolvedExponent(
        IElement controller,
        IElement? suppliedInput,
        out int exponent,
        out Relation evidence)
    {
        if (TryReadLiteralExponent(controller, out exponent))
        {
            evidence = BuildControllerEvidence(controller, controller, exponent);
            return true;
        }

        if (TryReadCountLocation(controller, out exponent, out var rawEvidence) ||
            TryReadEquationController(controller, suppliedInput, out exponent, out rawEvidence) ||
            TryReadResolutionSearchController(controller, out exponent, out rawEvidence) ||
            TryReadClaimController(controller, out exponent, out rawEvidence))
        {
            evidence = BuildControllerEvidence(controller, rawEvidence, exponent);
            return true;
        }

        exponent = 0;
        evidence = null!;
        return false;
    }

    private static int ReadResolvedExponent(IElement controller) =>
        TryReadResolvedExponent(controller, out var exponent) ? exponent : 0;

    private static bool TryReadTransitionOutput(IElement transition, IElement controller, out Carrier output)
    {
        if (TryReadResolvedExponent(controller, out var exponent))
        {
            return exponent <= 0
                ? TryReadConstantOutput(transition, 1, out output)
                : TryReadProductTransition(transition, exponent, out output);
        }

        return TryReadConstantOutput(transition, 1, out output) ||
            TryReadProductTransition(transition, expectedCount: null, out output);
    }

    private static bool TryReadConstantOutput(IElement transition, long value, out Carrier output)
    {
        if (transition is Relation point &&
            point.Left is CarrierLocation location &&
            point.Right is Carrier carrier &&
            location.Carrier.SameIdentityAs(carrier) &&
            location.Ticks == value)
        {
            output = carrier;
            return true;
        }

        output = null!;
        return false;
    }

    private static bool TryReadProductTransition(IElement transition, int? expectedCount, out Carrier output)
    {
        if (transition is Relation product &&
            product.Right is Relation outputFrame &&
            TimeStepSequence.TryReadSpan(outputFrame, out var outputStart, out var outputEnd) &&
            outputStart.Ticks == 0 &&
            outputEnd.Ticks == 1 &&
            TryReadPortGroup(product.Left, expectedCount, out var ports) &&
            ports.Length > 0)
        {
            output = outputEnd.Carrier;
            return true;
        }

        output = null!;
        return false;
    }

    private static bool TryReadPortGroup(IElement element, int? expectedCount, out Relation[] ports)
    {
        if (expectedCount is { } count)
        {
            var collected = new Relation[count];
            if (TryReadPortGroup(element, count, collected, 0))
            {
                ports = collected;
                return true;
            }

            ports = [];
            return false;
        }

        var dynamicPorts = new List<Relation>();
        if (TryCollectLeftAssociatedPorts(element, dynamicPorts) && dynamicPorts.Count > 0)
        {
            ports = [.. dynamicPorts];
            return true;
        }

        ports = [];
        return false;
    }

    private static bool TryReadPortGroup(IElement element, int count, Relation[] ports, int offset)
    {
        if (count == 1)
        {
            if (element is Relation port && TryReadUnitPort(port, out _))
            {
                ports[offset] = port;
                return true;
            }

            return false;
        }

        return element is Relation group &&
            TryReadPortGroup(group.Left, count - 1, ports, offset) &&
            TryReadPortGroup(group.Right, 1, ports, offset + count - 1);
    }

    private static bool TryCollectLeftAssociatedPorts(IElement element, List<Relation> ports)
    {
        if (element is not Relation relation)
        {
            return false;
        }

        if (TryReadUnitPort(relation, out _))
        {
            ports.Add(relation);
            return true;
        }

        return TryCollectLeftAssociatedPorts(relation.Left, ports) &&
            TryCollectLeftAssociatedPorts(relation.Right, ports);
    }

    private static bool TryReadUnitPort(IElement element, out Carrier input)
    {
        if (element is Relation port &&
            port.Left is Relation inputFrame &&
            port.Right is Relation outputFrame &&
            TimeStepSequence.TryReadSpan(inputFrame, out var inputStart, out var inputEnd) &&
            TimeStepSequence.TryReadSpan(outputFrame, out var outputStart, out var outputEnd) &&
            inputStart.Ticks == 0 &&
            inputEnd.Ticks == 1 &&
            outputStart.Ticks == 0 &&
            outputEnd.Ticks == 1)
        {
            input = inputEnd.Carrier;
            return true;
        }

        input = null!;
        return false;
    }

    private static bool TryReadCountLocation(IElement controller, out int exponent, out IElement evidence)
    {
        if (controller is CarrierLocation location &&
            TryTicksToExponent(location.Ticks, out exponent))
        {
            evidence = location;
            return true;
        }

        if (TryReadPoint(controller, out var point) &&
            TryTicksToExponent(point.Ticks, out exponent))
        {
            evidence = controller;
            return true;
        }

        if (controller is Relation prediction &&
            Equation.TryReadPrediction(prediction, out var predicted, out _) &&
            TryTicksToExponent(predicted.Ticks, out exponent))
        {
            evidence = prediction;
            return true;
        }

        exponent = 0;
        evidence = Relation.Point(Carrier.Unknown, 0);
        return false;
    }

    private static bool TryReadEquationController(
        IElement controller,
        IElement? suppliedInput,
        out int exponent,
        out IElement evidence)
    {
        if (controller is Relation structure &&
            TryReadRootElements(structure, out var elements) &&
            elements.Length >= 6 &&
            TryReadPoint(elements[0], out _) &&
            elements[1] is Relation equation &&
            elements[3] is Carrier countOutput)
        {
            var hasOpenInput = IsUnknownPoint(elements[2]);
            if (hasOpenInput && suppliedInput is null)
            {
                exponent = 0;
                evidence = Relation.Point(Carrier.Unknown, 0);
                return false;
            }

            IElement controllerInput = hasOpenInput
                ? suppliedInput!
                : elements[2];
            var prediction = Equation.Predict(equation, controllerInput, countOutput);
            if (TryReadPredictionCount(prediction, out var ticks) &&
                TryTicksToExponent(ticks, out exponent))
            {
                evidence = Relation.OrderedGroup(
                    new Carrier(),
                    controller,
                    controllerInput,
                    prediction,
                    TryReadRootPointCarrier(elements[0], out var axis)
                        ? RepeatSurface.Count(axis, ticks)
                        : Relation.Point(Carrier.Unknown, 0));
                return true;
            }
        }

        exponent = 0;
        evidence = Relation.Point(Carrier.Unknown, 0);
        return false;
    }

    private static bool TryReadResolutionSearchController(IElement controller, out int exponent, out IElement evidence)
    {
        if (controller is Relation structure &&
            TryReadRootElements(structure, out var elements) &&
            elements.Length >= 4 &&
            TryReadPoint(elements[0], out _) &&
            elements[1] is Relation search &&
            TryReadLastResolutionSample(search, out var selected) &&
            TryReadResolutionSampleCount(selected, out var ticks) &&
            TryTicksToExponent(ticks, out exponent))
        {
            evidence = selected;
            return true;
        }

        if (controller is Relation rawSearch &&
            TryReadLastResolutionSample(rawSearch, out var rawSelected) &&
            TryReadResolutionSampleCount(rawSelected, out var rawTicks) &&
            TryTicksToExponent(rawTicks, out exponent))
        {
            evidence = rawSelected;
            return true;
        }

        exponent = 0;
        evidence = Relation.Point(Carrier.Unknown, 0);
        return false;
    }

    private static bool TryReadClaimController(IElement controller, out int exponent, out IElement evidence)
    {
        if (controller is Relation claim &&
            OperationClaim.TryRead(
                claim,
                out _,
                out _,
                out _,
                out var residualMagnitude,
                out var violationMagnitude,
                out _) &&
            TryTicksToExponent(violationMagnitude.Ticks != 0 ? violationMagnitude.Ticks : residualMagnitude.Ticks, out exponent))
        {
            evidence = claim;
            return true;
        }

        exponent = 0;
        evidence = Relation.Point(Carrier.Unknown, 0);
        return false;
    }

    private static bool TryReadLastResolutionSample(Relation search, out Relation sample)
    {
        if (EquationResolutionSearch.TryReadRounds(search, out var rounds))
        {
            for (var index = rounds.Length - 1; index >= 0; index--)
            {
                if (EquationResolutionSearch.TryReadSelectedSample(rounds[index], out sample))
                {
                    return true;
                }
            }
        }

        sample = null!;
        return false;
    }

    private static bool TryReadResolutionSampleCount(Relation sample, out long ticks)
    {
        if (EquationResolutionSearch.TryReadCandidateSample(
                sample,
                out _,
                out var input,
                out _,
                out _,
                out _,
                out _,
                out _))
        {
            ticks = input.Ticks;
            return true;
        }

        ticks = 0;
        return false;
    }

    private static bool TryReadPredictionCount(Relation prediction, out long ticks)
    {
        if (Equation.TryReadPrediction(prediction, out var predicted, out _))
        {
            ticks = predicted.Ticks;
            return true;
        }

        ticks = 0;
        return false;
    }

    private static bool TryReadRootElements(Relation structure, out IElement[] elements)
    {
        if (structure?.Right is Carrier holder &&
            structure.TryReadOrderedElementsOn(holder, out elements))
        {
            return true;
        }

        elements = [];
        return false;
    }

    private static bool TryReadPoint(IElement element, out CarrierLocation location)
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

    private static bool TryReadRootPointCarrier(IElement element, out Carrier carrier)
    {
        if (TryReadPoint(element, out var location))
        {
            carrier = location.Carrier;
            return true;
        }

        carrier = null!;
        return false;
    }

    private static bool IsUnknownPoint(IElement element) =>
        TryReadPoint(element, out var location) && location.Carrier.IsUnknown && location.Ticks == 0;

    private static bool TryTicksToExponent(long ticks, out int exponent)
    {
        if (ticks >= 0 && ticks <= int.MaxValue)
        {
            exponent = checked((int)ticks);
            return true;
        }

        exponent = 0;
        return false;
    }

    private static Relation PowerTimelineInitial(Carrier output, Relation controllerEvidence) =>
        Relation.OrderedGroup(
            new Carrier(),
            Relation.Point(output, 1),
            controllerEvidence ?? BuildControllerEvidence(
                Relation.Point(Carrier.Unknown, 0),
                Relation.Point(Carrier.Unknown, 0),
                0));

    private static Relation BuildControllerEvidence(IElement controller, IElement reason, int exponent)
    {
        var safeReason = reason ?? Relation.Point(Carrier.Unknown, 0);
        var countCarrier = TryReadControllerEvidenceCarrier(controller, safeReason, out var carrier)
            ? carrier
            : new Carrier();
        var count = new CarrierLocation(countCarrier, exponent);
        var span = TryReadControllerSpan(controller, exponent, out var controllerSpan)
            ? controllerSpan
            : RepeatSurface.Count(countCarrier, exponent);

        return Relation.OrderedGroup(
            new Carrier(),
            controller ?? Relation.Point(Carrier.Unknown, 0),
            count,
            span,
            safeReason,
            EvidenceResidue(safeReason));
    }

    private static bool TryReadControllerSpan(IElement controller, int exponent, out Relation span)
    {
        if (controller is Relation relation &&
            RepeatSurface.TryReadSpan(relation, out var start, out var end) &&
            start.Ticks == 0 &&
            end.Ticks == exponent)
        {
            span = relation;
            return true;
        }

        if (controller is Relation structure &&
            TryReadRootElements(structure, out var elements))
        {
            foreach (var candidate in elements.OfType<Relation>())
            {
                if (RepeatSurface.TryReadSpan(candidate, out var candidateStart, out var candidateEnd) &&
                    candidateStart.Ticks == 0 &&
                    candidateEnd.Ticks == exponent)
                {
                    span = candidate;
                    return true;
                }
            }
        }

        span = null!;
        return false;
    }

    private static bool TryReadControllerEvidenceCarrier(IElement controller, IElement reason, out Carrier carrier)
    {
        if (controller is CarrierLocation location)
        {
            carrier = location.Carrier;
            return true;
        }

        if (TryReadPoint(controller, out var point))
        {
            carrier = point.Carrier;
            return true;
        }

        if (controller is Relation span &&
            RepeatSurface.TryReadSpan(span, out _, out var end))
        {
            carrier = end.Carrier;
            return true;
        }

        if (TryReadEvidenceLocation(reason, out var evidenceLocation))
        {
            carrier = evidenceLocation.Carrier;
            return true;
        }

        carrier = null!;
        return false;
    }

    private static IElement EvidenceResidue(IElement reason)
    {
        if (reason is Relation relation)
        {
            if (Equation.TryReadPrediction(relation, out _, out var predictionResidue))
            {
                return predictionResidue;
            }

            if (OperationClaim.TryRead(relation, out _, out _, out _, out _, out _, out var claimResidue))
            {
                return claimResidue;
            }

            if (EquationResolutionSearch.TryReadCandidateSample(
                    relation,
                    out _,
                    out _,
                    out _,
                    out _,
                    out _,
                    out _,
                    out var sampleResidue))
            {
                return sampleResidue;
            }

            if (TryReadRootElements(relation, out var elements))
            {
                foreach (var element in elements)
                {
                    var residue = EvidenceResidue(element);
                    if (!IsZeroUnknownLocation(residue))
                    {
                        return residue;
                    }
                }
            }
        }

        return Relation.Point(Carrier.Unknown, 0);
    }

    private static bool TryReadEvidenceLocation(IElement reason, out CarrierLocation location)
    {
        if (reason is CarrierLocation direct)
        {
            location = direct;
            return true;
        }

        if (TryReadPoint(reason, out var point))
        {
            location = point;
            return true;
        }

        if (reason is Relation relation)
        {
            if (Equation.TryReadPrediction(relation, out var predicted, out _))
            {
                location = predicted;
                return true;
            }

            if (EquationResolutionSearch.TryReadCandidateSample(
                    relation,
                    out _,
                    out var input,
                    out _,
                    out _,
                    out _,
                    out _,
                    out _))
            {
                location = input;
                return true;
            }

            if (OperationClaim.TryRead(
                    relation,
                    out _,
                    out _,
                    out _,
                    out var residualMagnitude,
                    out var violationMagnitude,
                    out _))
            {
                location = violationMagnitude.Ticks != 0 ? violationMagnitude : residualMagnitude;
                return true;
            }

            if (TryReadRootElements(relation, out var elements))
            {
                foreach (var element in elements)
                {
                    if (TryReadEvidenceLocation(element, out location))
                    {
                        return true;
                    }
                }
            }
        }

        location = default;
        return false;
    }

    private static bool IsZeroUnknownLocation(IElement element) =>
        element is CarrierLocation { Carrier.IsUnknown: true, Ticks: 0 } ||
        TryReadPoint(element, out var location) && location.Carrier.IsUnknown && location.Ticks == 0;

    private static Relation ProductTransition(Carrier input, Carrier output, int exponent)
    {
        var count = Math.Max(0, exponent);
        if (count == 0)
        {
            return Equation.Constant(output, 1);
        }

        var carriers = CreateFactorCarriers(input.UnitShape, count);
        var ports = new Relation[count];
        for (var index = 0; index < count; index++)
        {
            ports[index] = Equation.UnitPort(carriers[index]);
        }

        return Equation.Product(output, ports);
    }

    private static Carrier[] CreateFactorCarriers(UnitShape unitShape, int count)
    {
        var carriers = new Carrier[Math.Max(0, count)];
        for (var index = 0; index < carriers.Length; index++)
        {
            carriers[index] = new Carrier(unitShape);
        }

        return carriers;
    }

    private static IElement ProductInput(IReadOnlyList<Relation> factorReads)
    {
        if (factorReads.Count == 0)
        {
            return Relation.Point(Carrier.Unknown, 1);
        }

        IElement input = factorReads[0].Left;
        for (var index = 1; index < factorReads.Count; index++)
        {
            input = new Relation(input, factorReads[index].Left);
        }

        return input;
    }

    private static CarrierLocation PartialProductLocation(Carrier output, long inputTicks, int exponent)
    {
        var value = 1L;
        for (var index = 0; index < exponent; index++)
        {
            value = checked(value * inputTicks);
        }

        return new CarrierLocation(output, value);
    }

    private static Carrier RootOrUnknown(Carrier carrier) =>
        carrier ?? Carrier.Unknown;

    private static Carrier RootOrNew(Carrier carrier) =>
        carrier ?? new Carrier();
}
