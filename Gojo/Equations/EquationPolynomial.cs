using Gojo.Core;
using Gojo.Patterns;

namespace Gojo.Equations;

/// <summary>
/// Polynomial surface helpers built from existing Gojo equation structure:
/// input fanout generates term carriers, then a weighted sum reads those terms.
/// </summary>
public static class EquationPolynomial
{
    /// <summary>Creates a concrete polynomial sequence from constant-first coefficients.</summary>
    public static Relation Create(
        Carrier root,
        Carrier input,
        Carrier output,
        params long[] coefficients) =>
        Create(root, input, output, (IReadOnlyList<long>)coefficients);

    /// <summary>Creates a concrete polynomial sequence from constant-first coefficients.</summary>
    public static Relation Create(
        Carrier root,
        Carrier input,
        Carrier output,
        IReadOnlyList<long> coefficients)
    {
        var termCount = Math.Max(1, CountOf(coefficients));
        var terms = CreateTermCarriers(termCount);
        var termFanout = TermFanout(new Carrier(), input, terms);
        var ports = new Relation[termCount];

        for (var degree = 0; degree < termCount; degree++)
        {
            var coefficient = degree < CountOf(coefficients) ? coefficients[degree] : 0;
            ports[degree] = Equation.WeightedPort(terms[degree], 0, 1, 0, coefficient);
        }

        return EquationSequence.Create(
            RootOrNew(root),
            termFanout,
            Equation.Sum(output, ports));
    }

    /// <summary>
    /// Creates a polynomial sequence whose coefficient target frames are slots.
    /// Binding a slot to <see cref="CoefficientTarget"/> completes that term.
    /// </summary>
    public static Relation Pattern(
        Carrier root,
        Carrier input,
        Carrier output,
        int degree,
        out Carrier[] terms,
        out Placeholder[] coefficientSlots)
    {
        var termCount = Math.Max(1, degree + 1);
        terms = CreateTermCarriers(termCount);
        coefficientSlots = new Placeholder[termCount];
        var ports = new Relation[termCount];

        for (var termDegree = 0; termDegree < termCount; termDegree++)
        {
            coefficientSlots[termDegree] = new Placeholder();
            ports[termDegree] = new Relation(
                Relation.Segment(terms[termDegree], 0, 1),
                coefficientSlots[termDegree]);
        }

        return EquationSequence.Create(
            RootOrNew(root),
            TermFanout(new Carrier(), input, terms),
            Equation.Sum(output, ports));
    }

    /// <summary>Creates the target frame used to bind one coefficient slot.</summary>
    public static Relation CoefficientTarget(long coefficient) =>
        Relation.Segment(Carrier.Unknown, 0, coefficient);

    /// <summary>Creates a relation-shaped binding for one coefficient target slot.</summary>
    public static Relation BindCoefficient(Placeholder coefficientSlot, long coefficient) =>
        StructuralPattern.Bind(coefficientSlot, CoefficientTarget(coefficient));

    /// <summary>Reads one input through a polynomial sequence.</summary>
    public static Relation Predict(
        Relation polynomial,
        CarrierLocation input,
        Carrier output,
        out Relation[] stepReads) =>
        EquationSequence.Predict(polynomial, input, output, out stepReads);

    /// <summary>Applies coefficient-slot bindings to a polynomial pattern, then predicts.</summary>
    public static bool TryPredict(
        Relation polynomialPattern,
        IElement input,
        Carrier output,
        IReadOnlyList<Relation> bindings,
        out Relation prediction,
        out IElement[] unboundSlots,
        out Relation[] stepReads)
    {
        var complete = StructuralPattern.TryApplyComplete(
            polynomialPattern,
            bindings,
            out var applied,
            out unboundSlots);

        if (applied is Relation polynomial)
        {
            prediction = EquationSequence.Predict(polynomial, input, output, out stepReads);
            return complete;
        }

        stepReads = [];
        prediction = new Relation(
            new CarrierLocation(output ?? Carrier.Unknown, 0),
            input ?? Relation.Point(Carrier.Unknown, 0));
        return false;
    }

    /// <summary>Builds the fanout that generates 1, x, x^2, x^3, and higher repeated products.</summary>
    public static Relation TermFanout(Carrier root, Carrier input, IReadOnlyList<Carrier> terms)
    {
        var branches = new Relation[CountOf(terms)];
        for (var degree = 0; degree < branches.Length; degree++)
        {
            branches[degree] = PowerTerm(input, terms[degree], degree);
        }

        return EquationFanout.Create(RootOrNew(root), branches);
    }

    /// <summary>Reads the generated term fanout from a polynomial sequence.</summary>
    public static bool TryReadTermFanout(Relation polynomial, out Relation termFanout)
    {
        if (EquationSequence.TryReadSteps(polynomial, out var steps) &&
            steps.Length >= 1 &&
            EquationFanout.TryReadBranches(steps[0], out _))
        {
            termFanout = steps[0];
            return true;
        }

        termFanout = null!;
        return false;
    }

    /// <summary>Reads the weighted-sum surface from a polynomial sequence.</summary>
    public static bool TryReadWeightedSum(Relation polynomial, out Relation weightedSum)
    {
        if (EquationSequence.TryReadSteps(polynomial, out var steps) && steps.Length >= 2)
        {
            weightedSum = steps[1];
            return true;
        }

        weightedSum = null!;
        return false;
    }

    private static Relation PowerTerm(Carrier input, Carrier term, int degree)
    {
        return EquationPower.Create(new Carrier(), input, term, degree);
    }

    private static Carrier[] CreateTermCarriers(int count)
    {
        var carriers = new Carrier[Math.Max(0, count)];
        for (var index = 0; index < carriers.Length; index++)
        {
            carriers[index] = new Carrier();
        }

        return carriers;
    }

    private static int CountOf<T>(IReadOnlyList<T> values) =>
        values?.Count ?? 0;

    private static Carrier RootOrNew(Carrier carrier) =>
        carrier ?? new Carrier();
}
