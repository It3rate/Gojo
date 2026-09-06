using Gojo.Core;

namespace Gojo.Structures;

public static partial class UnitQuotient
{
    /// <summary>
    /// Explicitly exposes nested quotient and product participation. Each
    /// decomposition retains its source factor, ordered child roles and local
    /// standard. Calibration, opposed faces and additive factors remain atomic.
    /// </summary>
    public static bool TryExpand(Relation expression, out Relation witness)
    {
        if (!TryRead(expression, out var numerator, out var denominator, out var standard))
        {
            witness = null!;
            return false;
        }

        TryReadGroup(numerator, out var n);
        TryReadGroup(denominator, out var d);
        var expandedN = new List<IElement>();
        var expandedD = new List<IElement>();
        var decompositions = new List<IElement>();
        foreach (var factor in n) Expand(factor, false);
        foreach (var factor in d) Expand(factor, true);
        var result = Create(new Carrier(), Group(expandedN), Group(expandedD), standard);
        witness = ReadWitness.Create(new Carrier(), expression, Group(decompositions), result,
            new Relation(Group(expandedN.Where(f => !TryReadFactor(f, out _))),
                Group(expandedD.Where(f => !TryReadFactor(f, out _)))), standard);
        return true;

        void Expand(IElement factor, bool inDenominator)
        {
            if (TryReadFactor(factor, out var unit) && !unit.TryReadOpposedFace(out _))
            {
                if (unit.TryReadQuotient(out var innerN, out var innerD, out var innerStandard))
                {
                    var childN = Factors(innerN);
                    var childD = Factors(innerD);
                    decompositions.Add(new Relation(factor,
                        Create(new Carrier(), Group(childN), Group(childD), innerStandard)));
                    foreach (var child in childN) Expand(child, inDenominator);
                    foreach (var child in childD) Expand(child, !inDenominator);
                    return;
                }
                if (unit.TryReadOrthogonalProductOperands(out var factors))
                {
                    var children = Factors(factors);
                    decompositions.Add(new Relation(factor, Group(children)));
                    foreach (var child in children) Expand(child, inDenominator);
                    return;
                }
            }
            (inDenominator ? expandedD : expandedN).Add(factor);
        }
    }

    /// <summary>Composes explicit expansion and cancellation, retaining both witnesses.</summary>
    public static bool TryNormalize(Relation expression, out Relation witness)
    {
        if (TryExpand(expression, out var expansion) &&
            ReadWitness.TryRead(expansion, out _, out _, out var expanded, out _, out _) &&
            expanded is Relation expandedExpression && TryCancel(expandedExpression, out var cancellation) &&
            ReadWitness.TryRead(cancellation, out _, out _, out var result, out var residue, out _))
        {
            witness = ReadWitness.Create(new Carrier(), expression,
                Group([expansion, cancellation]), result, residue,
                Group([expansion, cancellation]));
            return true;
        }
        witness = null!;
        return false;
    }

    /// <summary>
    /// Observes a quantity through a separately selected dimensional expansion
    /// and cancellation. Only exact one-tick equivalence is admitted; no scale
    /// conversion or material transfer is inferred from a standard.
    /// </summary>
    public static bool TryProjectEquivalent(CarrierLocation value, Carrier target,
        IElement standard, out Relation witness)
    {
        var expression = Create(new Carrier(), Group([Relation.Point(value.Carrier, 1)]), Group([]), standard);
        if (TryNormalize(expression, out var normalization) &&
            ReadWitness.TryRead(normalization, out _, out _, out var result, out _, out _) &&
            result is Relation reduced && MatchesUnit(reduced, target.UnitShape))
        {
            var projection = new Relation(Relation.Point(value.Carrier, 1), Relation.Point(target, 1));
            if (projection.TryProject(value, target, out var read) && read.TryReadProjection(out var projected, out var residue))
            {
                witness = ReadWitness.Create(new Carrier(), value, projection, projected, residue, normalization);
                return true;
            }
        }
        witness = ReadWitness.Create(new Carrier(), value, expression, new Placeholder(), value, standard);
        return false;
    }

    private static bool MatchesUnit(Relation expression, UnitShape target) =>
        (TryReadUnit(expression, out var quotient) && quotient.Equals(target)) ||
        (TryReadNumeratorUnit(expression, out var numerator) && numerator.Equals(target));

    private static IElement[] Factors(IEnumerable<UnitShape> units) =>
        [.. units.Select(unit => Relation.Point(new Carrier(unit), 1))];
}
