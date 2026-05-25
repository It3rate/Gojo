using Gojo.Core;
using Gojo.Structures;

namespace Gojo.Equations;

/// <summary>
/// Inspection helpers for applying equation surfaces to ordered samples,
/// comparing outputs, and recording fit evidence as visible Gojo structure.
/// </summary>
public static class EquationInspection
{
    /// <summary>
    /// Applies one equation to an ordered group of input samples. The returned
    /// structure records the map span, equation, and one sample group per input.
    /// </summary>
    public static Relation Map(
        Carrier root,
        Relation equation,
        IReadOnlyList<CarrierLocation> inputs,
        Carrier output,
        out Relation[] samples)
    {
        var holder = RootOrNew(root);
        var index = new Carrier();
        var count = CountOf(inputs);
        samples = new Relation[count];

        for (var sampleIndex = 0; sampleIndex < count; sampleIndex++)
        {
            var prediction = Equation.Predict(equation, inputs[sampleIndex], output);
            samples[sampleIndex] = PredictionSample(index, sampleIndex, inputs[sampleIndex], prediction);
        }

        return Relation.OrderedGroup(
            holder,
            [Relation.Segment(index, 0, count), equation ?? Relation.Point(Carrier.Unknown, 0), .. samples]);
    }

    /// <summary>
    /// Applies one equation to an ordered group of scalar or relation-shaped
    /// input samples.
    /// </summary>
    public static Relation Map(
        Carrier root,
        Relation equation,
        IReadOnlyList<IElement> inputs,
        Carrier output,
        out Relation[] samples)
    {
        var holder = RootOrNew(root);
        var index = new Carrier();
        var count = CountOf(inputs);
        samples = new Relation[count];

        for (var sampleIndex = 0; sampleIndex < count; sampleIndex++)
        {
            var input = inputs[sampleIndex] ?? Relation.Point(Carrier.Unknown, 0);
            var prediction = Equation.Predict(equation, input, output);
            samples[sampleIndex] = PredictionSample(index, sampleIndex, input, prediction);
        }

        return Relation.OrderedGroup(
            holder,
            [Relation.Segment(index, 0, count), equation ?? Relation.Point(Carrier.Unknown, 0), .. samples]);
    }

    /// <summary>
    /// Compares predicted and observed samples by shared index and creates
    /// per-sample error locations. Extra samples on either side are left out.
    /// </summary>
    public static Relation Compare(
        Carrier root,
        IReadOnlyList<CarrierLocation> predictions,
        IReadOnlyList<CarrierLocation> observations,
        Carrier error,
        out Relation[] samples)
    {
        var holder = RootOrNew(root);
        var errorCarrier = RootOrUnknown(error);
        var index = new Carrier();
        var count = Math.Min(CountOf(predictions), CountOf(observations));
        samples = new Relation[count];

        for (var sampleIndex = 0; sampleIndex < count; sampleIndex++)
        {
            var claim = OperationClaim.Equality(
                new Carrier(),
                predictions[sampleIndex],
                observations[sampleIndex],
                errorCarrier);
            OperationClaim.TryRead(
                claim,
                out _,
                out var residual,
                out _,
                out _,
                out _,
                out _);
            var errorLocation = residual is CarrierLocation residualLocation
                ? residualLocation
                : new CarrierLocation(errorCarrier, 0);
            samples[sampleIndex] = ComparisonSample(
                index,
                sampleIndex,
                predictions[sampleIndex],
                observations[sampleIndex],
                errorLocation,
                claim);
        }

        return Relation.OrderedGroup(holder, [Relation.Segment(index, 0, count), .. samples]);
    }

    /// <summary>
    /// Summarizes a stream of signed errors as inspectable signed and magnitude
    /// totals, preserving one witness group per error sample.
    /// </summary>
    public static Relation SummarizeFit(
        Carrier root,
        IReadOnlyList<CarrierLocation> errors,
        Carrier signedError,
        Carrier errorMagnitude,
        out Relation[] samples)
    {
        var holder = RootOrNew(root);
        var signedCarrier = RootOrUnknown(signedError);
        var magnitudeCarrier = RootOrUnknown(errorMagnitude);
        var index = new Carrier();
        var count = CountOf(errors);
        var totalSigned = 0L;
        var totalMagnitude = 0L;
        samples = new Relation[count];

        for (var sampleIndex = 0; sampleIndex < count; sampleIndex++)
        {
            var error = errors[sampleIndex];
            var magnitude = MagnitudeOf(error.Ticks);
            var magnitudeLocation = new CarrierLocation(magnitudeCarrier, magnitude);
            samples[sampleIndex] = FitSample(index, sampleIndex, error, magnitudeLocation);
            totalSigned = SaturatingAdd(totalSigned, error.Ticks);
            totalMagnitude = SaturatingAdd(totalMagnitude, magnitude);
        }

        return Relation.OrderedGroup(
            holder,
            [
                Relation.Segment(index, 0, count),
                new CarrierLocation(signedCarrier, totalSigned),
                new CarrierLocation(magnitudeCarrier, totalMagnitude),
                .. samples
            ]);
    }

    /// <summary>Reads mapped sample groups from a structure returned by Map.</summary>
    public static bool TryReadMapSamples(Relation map, out Relation[] samples)
    {
        if (!TryReadRootElements(map, out var elements) || elements.Length < 2)
        {
            samples = [];
            return false;
        }

        samples = [.. elements.Skip(2).OfType<Relation>()];
        return true;
    }

    /// <summary>Reads comparison sample groups from a structure returned by Compare.</summary>
    public static bool TryReadComparisonSamples(Relation comparison, out Relation[] samples)
    {
        if (!TryReadRootElements(comparison, out var elements) || elements.Length < 1)
        {
            samples = [];
            return false;
        }

        samples = [.. elements.Skip(1).OfType<Relation>()];
        return true;
    }

    /// <summary>Reads a fit summary returned by SummarizeFit.</summary>
    public static bool TryReadFitSummary(
        Relation fit,
        out CarrierLocation totalSigned,
        out CarrierLocation totalMagnitude,
        out Relation[] samples)
    {
        if (TryReadRootElements(fit, out var elements) &&
            elements.Length >= 3 &&
            elements[1] is CarrierLocation signed &&
            elements[2] is CarrierLocation magnitude)
        {
            totalSigned = signed;
            totalMagnitude = magnitude;
            samples = [.. elements.Skip(3).OfType<Relation>()];
            return true;
        }

        totalSigned = default;
        totalMagnitude = default;
        samples = [];
        return false;
    }

    /// <summary>Reads one prediction sample group.</summary>
    public static bool TryReadPredictionSample(
        Relation sample,
        out CarrierLocation index,
        out CarrierLocation input,
        out CarrierLocation predicted,
        out CarrierLocation residue)
    {
        if (TryReadSampleLocations(sample, expectedLocations: 4, out var locations))
        {
            index = locations[0];
            input = locations[1];
            predicted = locations[2];
            residue = locations[3];
            return true;
        }

        index = default;
        input = default;
        predicted = default;
        residue = default;
        return false;
    }

    /// <summary>Reads one prediction sample while preserving relation-shaped input and residue.</summary>
    public static bool TryReadPredictionSampleElement(
        Relation sample,
        out CarrierLocation index,
        out IElement input,
        out CarrierLocation predicted,
        out IElement residue)
    {
        if (TryReadRootElements(sample, out var elements) &&
            elements.Length == 4 &&
            elements[0] is Relation indexPoint &&
            indexPoint.Left is CarrierLocation indexLocation &&
            elements[2] is CarrierLocation predictedLocation)
        {
            index = indexLocation;
            input = elements[1];
            predicted = predictedLocation;
            residue = elements[3];
            return true;
        }

        index = default;
        input = default!;
        predicted = default;
        residue = default!;
        return false;
    }

    /// <summary>Reads one comparison sample group.</summary>
    public static bool TryReadComparisonSample(
        Relation sample,
        out CarrierLocation index,
        out CarrierLocation predicted,
        out CarrierLocation observed,
        out CarrierLocation error)
    {
        if (TryReadRootElements(sample, out var elements) &&
            elements.Length >= 4 &&
            elements[0] is Relation indexPoint &&
            indexPoint.Left is CarrierLocation indexLocation &&
            elements[1] is CarrierLocation predictedLocation &&
            elements[2] is CarrierLocation observedLocation &&
            elements[3] is CarrierLocation errorLocation)
        {
            index = indexLocation;
            predicted = predictedLocation;
            observed = observedLocation;
            error = errorLocation;
            return true;
        }

        index = default;
        predicted = default;
        observed = default;
        error = default;
        return false;
    }

    /// <summary>Reads the equality claim tested by one comparison sample.</summary>
    public static bool TryReadComparisonClaim(Relation sample, out Relation claim)
    {
        if (TryReadRootElements(sample, out var elements) &&
            elements.Length >= 5 &&
            elements[4] is Relation comparisonClaim &&
            OperationClaim.TryRead(comparisonClaim, out _, out _, out _, out _, out _, out _))
        {
            claim = comparisonClaim;
            return true;
        }

        claim = null!;
        return false;
    }

    /// <summary>Reads one fit sample group.</summary>
    public static bool TryReadFitSample(
        Relation sample,
        out CarrierLocation index,
        out CarrierLocation error,
        out CarrierLocation magnitude)
    {
        if (TryReadSampleLocations(sample, expectedLocations: 3, out var locations))
        {
            index = locations[0];
            error = locations[1];
            magnitude = locations[2];
            return true;
        }

        index = default;
        error = default;
        magnitude = default;
        return false;
    }

    private static Relation PredictionSample(
        Carrier index,
        int sampleIndex,
        CarrierLocation input,
        Relation prediction) =>
        PredictionSample(index, sampleIndex, (IElement)input, prediction);

    private static Relation PredictionSample(
        Carrier index,
        int sampleIndex,
        IElement input,
        Relation prediction)
    {
        if (prediction.Left is CarrierLocation predicted)
        {
            return Relation.OrderedGroup(
                new Carrier(),
                Relation.Point(index, sampleIndex),
                input,
                predicted,
                prediction.Right);
        }

        return Relation.OrderedGroup(new Carrier(), Relation.Point(index, sampleIndex), input, prediction);
    }

    private static Relation ComparisonSample(
        Carrier index,
        int sampleIndex,
        CarrierLocation predicted,
        CarrierLocation observed,
        CarrierLocation error,
        Relation claim) =>
        Relation.OrderedGroup(
            new Carrier(),
            Relation.Point(index, sampleIndex),
            predicted,
            observed,
            error,
            claim);

    private static Relation FitSample(
        Carrier index,
        int sampleIndex,
        CarrierLocation error,
        CarrierLocation magnitude) =>
        Relation.OrderedGroup(
            new Carrier(),
            Relation.Point(index, sampleIndex),
            error,
            magnitude);

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

    private static bool TryReadSampleLocations(
        Relation sample,
        int expectedLocations,
        out CarrierLocation[] locations)
    {
        if (TryReadRootElements(sample, out var elements) &&
            elements.Length == expectedLocations &&
            elements[0] is Relation indexPoint &&
            indexPoint.Left is CarrierLocation index &&
            elements.Skip(1).All(element => element is CarrierLocation))
        {
            locations = [index, .. elements.Skip(1).Cast<CarrierLocation>()];
            return true;
        }

        locations = [];
        return false;
    }

    private static int CountOf<T>(IReadOnlyList<T> values) =>
        values?.Count ?? 0;

    private static Carrier RootOrUnknown(Carrier carrier) =>
        carrier ?? Carrier.Unknown;

    private static Carrier RootOrNew(Carrier carrier) =>
        carrier ?? new Carrier();

    private static long SaturatingAdd(long left, long right)
    {
        var sum = (Int128)left + right;
        if (sum > long.MaxValue)
        {
            return long.MaxValue;
        }

        return sum < long.MinValue ? long.MinValue : (long)sum;
    }

    private static long MagnitudeOf(long ticks)
    {
        var magnitude = ticks < 0 ? -(Int128)ticks : (Int128)ticks;
        return magnitude > long.MaxValue ? long.MaxValue : (long)magnitude;
    }
}
