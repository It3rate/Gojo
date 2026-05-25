using Gojo.Core;
using Gojo.Structures;

namespace Gojo.Equations;

/// <summary>
/// Construction and single-sample read helpers for relation-shaped equations.
/// A relation-shaped equation is a Gojo surface: frames, operand ports, and
/// output participation remain visible as carrier/relation structure.
/// </summary>
public static class Equation
{
    /// <summary>Creates an affine frame surface from explicit input and output frames.</summary>
    public static Relation Affine(Relation inputFrame, Relation outputFrame) =>
        new(inputFrame ?? Relation.Point(Carrier.Unknown, 0), outputFrame ?? Relation.Point(Carrier.Unknown, 0));

    /// <summary>
    /// Creates an affine frame surface. The start/end values are basis endpoints,
    /// not consecutive samples.
    /// </summary>
    public static Relation Affine(
        Carrier input,
        long inputStart,
        long inputEnd,
        Carrier output,
        long outputStart,
        long outputEnd) =>
        Affine(
            Relation.Segment(RootOrUnknown(input), inputStart, inputEnd),
            Relation.Segment(RootOrUnknown(output), outputStart, outputEnd));

    /// <summary>Creates an operand port that projects one input frame into the equation reader basis.</summary>
    public static Relation WeightedPort(
        Carrier input,
        long inputStart,
        long inputEnd,
        long outputStart,
        long outputEnd) =>
        new(
            Relation.Segment(RootOrUnknown(input), inputStart, inputEnd),
            Relation.Segment(Carrier.Unknown, outputStart, outputEnd));

    /// <summary>Creates a unit operand port for additive or product operation surfaces.</summary>
    public static Relation UnitPort(Carrier input) =>
        WeightedPort(input, 0, 1, 0, 1);

    /// <summary>Creates a point-valued equation surface that leaves supplied input as residue.</summary>
    public static Relation Constant(Carrier output, long ticks) =>
        Relation.Point(RootOrUnknown(output), ticks);

    /// <summary>Creates an additive operation surface over relation-shaped operand input.</summary>
    public static Relation Sum(Carrier output, params Relation[] operandPorts) =>
        new(PortGroup(operandPorts), Relation.Point(RootOrUnknown(output), 1));

    /// <summary>Creates a product operation surface over relation-shaped operand input.</summary>
    public static Relation Product(Carrier output, params Relation[] operandPorts) =>
        new(PortGroup(operandPorts), Relation.Segment(RootOrUnknown(output), 0, 1));

    /// <summary>
    /// Creates an error surface that reads relation-shaped input as observed - predicted.
    /// </summary>
    public static Relation Difference(Carrier predicted, Carrier observed, Carrier error) =>
        Sum(
            error,
            WeightedPort(predicted, 0, 1, 0, -1),
            WeightedPort(observed, 0, 1, 0, 1));

    /// <summary>Projects one input sample through an equation into an output carrier.</summary>
    public static Relation Predict(Relation equation, CarrierLocation input, Carrier output) =>
        Predict(equation, (IElement)input, output);

    /// <summary>Reads one scalar or relation-shaped input sample through an equation.</summary>
    public static Relation Predict(Relation equation, IElement input, Carrier output)
    {
        var supplied = input ?? Relation.Point(Carrier.Unknown, 0);
        var target = RootOrUnknown(output);
        if (equation is null)
        {
            return new Relation(new CarrierLocation(target, 0), supplied);
        }

        if (EquationPower.TryPredict(equation, supplied, target, out var powerRead))
        {
            return powerRead;
        }

        if (EquationSequence.TryReadSteps(equation, out var steps) && steps.Length != 0)
        {
            return EquationSequence.Predict(equation, supplied, target, out _);
        }

        if (EquationFanout.TryReadBranches(equation, out var branches) && branches.Length != 0)
        {
            return EquationFanout.Predict(equation, supplied, out _);
        }

        if (TryReadConstant(equation, target, supplied, out var constantRead))
        {
            return constantRead;
        }

        if (equation.TryReadOperationFrom(supplied, target, out var operationRead))
        {
            return operationRead;
        }

        return supplied is CarrierLocation location
            ? ProjectScalar(equation, location, target)
            : new Relation(new CarrierLocation(target, 0), supplied);
    }

    /// <summary>Projects one input sample only when the equation surface accepts it.</summary>
    public static bool TryPredict(
        Relation equation,
        CarrierLocation input,
        Carrier output,
        out Relation prediction) =>
        TryPredict(equation, (IElement)input, output, out prediction);

    /// <summary>Reads one scalar or relation-shaped input sample only when the equation accepts it.</summary>
    public static bool TryPredict(
        Relation equation,
        IElement input,
        Carrier output,
        out Relation prediction)
    {
        var supplied = input ?? Relation.Point(Carrier.Unknown, 0);
        var target = RootOrUnknown(output);
        if (equation is not null &&
            EquationPower.TryPredict(equation, supplied, target, out prediction))
        {
            return true;
        }

        if (equation is not null &&
            EquationSequence.TryReadSteps(equation, out var steps) &&
            steps.Length != 0)
        {
            prediction = EquationSequence.Predict(equation, supplied, target, out _);
            return true;
        }

        if (equation is not null &&
            EquationFanout.TryReadBranches(equation, out var branches) &&
            branches.Length != 0)
        {
            prediction = EquationFanout.Predict(equation, supplied, out _);
            return true;
        }

        if (equation is not null &&
            (TryReadConstant(equation, target, supplied, out prediction) ||
                equation.TryReadOperationFrom(supplied, target, out prediction) ||
                (supplied is CarrierLocation location && equation.TryProject(location, target, out prediction))))
        {
            return true;
        }

        prediction = new Relation(new CarrierLocation(target, 0), supplied);
        return false;
    }

    /// <summary>Reads a prediction as target output plus residue, preserving relation-shaped residue.</summary>
    public static bool TryReadPrediction(Relation prediction, out CarrierLocation predicted, out IElement residue)
    {
        if (prediction?.Left is CarrierLocation target)
        {
            predicted = target;
            residue = prediction.Right;
            return true;
        }

        predicted = default;
        residue = default!;
        return false;
    }

    /// <summary>
    /// Creates a common read witness for one prediction without changing the
    /// equation's native prediction shape.
    /// </summary>
    public static Relation PredictWitness(
        Carrier root,
        Relation equation,
        IElement input,
        Carrier output)
    {
        var supplied = input ?? Relation.Point(Carrier.Unknown, 0);
        var prediction = Predict(equation, supplied, output);
        return TryCreatePredictionWitness(root, equation, supplied, prediction, out var witness)
            ? witness
            : ReadWitness.Create(
                RootOrNew(root),
                supplied,
                equation ?? Relation.Point(Carrier.Unknown, 0),
                prediction,
                Relation.Point(Carrier.Unknown, 0),
                prediction);
    }

    /// <summary>Packages an existing prediction as selected input, equation surface, output, residue, and raw prediction provenance.</summary>
    public static bool TryCreatePredictionWitness(
        Carrier root,
        Relation equation,
        IElement input,
        Relation prediction,
        out Relation witness)
    {
        if (TryReadPrediction(prediction, out var predicted, out var residue))
        {
            witness = ReadWitness.Create(
                RootOrNew(root),
                input ?? Relation.Point(Carrier.Unknown, 0),
                equation ?? Relation.Point(Carrier.Unknown, 0),
                predicted,
                residue,
                prediction);
            return true;
        }

        witness = null!;
        return false;
    }

    /// <summary>Reads a common prediction witness back into equation-specific parts.</summary>
    public static bool TryReadPredictionWitness(
        Relation witness,
        out IElement input,
        out Relation equation,
        out CarrierLocation predicted,
        out IElement residue,
        out IElement provenance)
    {
        if (ReadWitness.TryRead(
                witness,
                out var selected,
                out var readSurface,
                out var result,
                out var residueOrTension,
                out var witnessProvenance) &&
            readSurface is Relation equationSurface &&
            result is CarrierLocation predictedLocation)
        {
            input = selected;
            equation = equationSurface;
            predicted = predictedLocation;
            residue = residueOrTension;
            provenance = witnessProvenance;
            return true;
        }

        input = default!;
        equation = null!;
        predicted = default;
        residue = default!;
        provenance = default!;
        return false;
    }

    private static IElement PortGroup(IReadOnlyList<Relation> ports)
    {
        if (CountOf(ports) == 0)
        {
            return new Relation(Relation.Point(Carrier.Unknown, 1), Relation.Point(Carrier.Unknown, 1));
        }

        IElement group = ports[0] ?? Relation.Point(Carrier.Unknown, 0);
        for (var index = 1; index < ports.Count; index++)
        {
            group = new Relation(group, ports[index] ?? Relation.Point(Carrier.Unknown, 0));
        }

        return group;
    }

    private static int CountOf<T>(IReadOnlyList<T> values) =>
        values?.Count ?? 0;

    private static Carrier RootOrUnknown(Carrier carrier) =>
        carrier ?? Carrier.Unknown;

    private static Carrier RootOrNew(Carrier carrier) =>
        carrier ?? new Carrier();

    private static Relation ProjectScalar(Relation equation, CarrierLocation input, Carrier output) =>
        equation is null
            ? new Relation(new CarrierLocation(output, 0), input)
            : equation.Project(input, output);

    private static bool TryReadConstant(
        Relation equation,
        Carrier output,
        IElement supplied,
        out Relation prediction)
    {
        if (equation?.Left is CarrierLocation point &&
            equation.Right is Carrier pointCarrier &&
            point.Carrier.SameIdentityAs(pointCarrier) &&
            (output.IsUnknown || output.SameIdentityAs(pointCarrier)))
        {
            prediction = new Relation(new CarrierLocation(pointCarrier, point.Ticks), supplied);
            return true;
        }

        prediction = null!;
        return false;
    }
}
