using Gojo.Core;

namespace Gojo.Structures;

/// <summary>
/// An explicit dimensional read over ordered numerator and denominator factor
/// groups, under a retained standard. Factors are one-tick points on unit-bearing
/// carriers; open or unsupported factors remain visible. This is not numeric
/// division, a material operation, or an implicit rewrite of UnitShape compatibility.
/// </summary>
public static class UnitQuotient
{
    /// <summary>Connects ordered numerator factors, denominator factors, and the selected standard.</summary>
    public static Relation Create(Carrier root, Relation numerator, Relation denominator, IElement standard) =>
        Relation.OrderedGroup(root, numerator, denominator, standard);

    /// <summary>Reads exactly the three fields of a selected unit quotient.</summary>
    public static bool TryRead(Relation expression, out Relation numerator, out Relation denominator, out IElement standard)
    {
        if (TryReadGroup(expression, out var fields) && fields.Length == 3 &&
            fields[0] is Relation n && TryReadGroup(n, out _) &&
            fields[1] is Relation d && TryReadGroup(d, out _))
        {
            numerator = n;
            denominator = d;
            standard = fields[2];
            return true;
        }

        numerator = null!;
        denominator = null!;
        standard = null!;
        return false;
    }

    /// <summary>
    /// Cancels matching occurrences only for this explicitly selected dimensional
    /// read. The witness retains the original expression, paired one-tick bases,
    /// remaining ordered factors, unreadable participation, and the standard.
    /// </summary>
    public static bool TryCancel(Relation expression, out Relation witness)
    {
        if (!TryRead(expression, out var numerator, out var denominator, out var standard))
        {
            witness = null!;
            return false;
        }

        TryReadGroup(numerator, out var n);
        TryReadGroup(denominator, out var d);
        var usedNumerator = new bool[n.Length];
        var usedDenominator = new bool[d.Length];
        var pairs = new List<IElement>();
        for (var di = 0; di < d.Length; di++)
        {
            if (!TryReadFactor(d[di], out var denominatorUnit)) continue;
            for (var ni = 0; ni < n.Length; ni++)
            {
                if (usedNumerator[ni] || !TryReadFactor(n[ni], out var numeratorUnit) ||
                    !numeratorUnit.Equals(denominatorUnit)) continue;

                // A pair is an explicit one-tick to one-tick basis correspondence.
                // Each incidence participates at most once; equal ticks are not identity.
                pairs.Add(new Relation(n[ni], d[di]));
                usedNumerator[ni] = true;
                usedDenominator[di] = true;
                break;
            }
        }

        var result = Create(new Carrier(),
            Group(n.Where((_, i) => !usedNumerator[i])),
            Group(d.Where((_, i) => !usedDenominator[i])), standard);
        var unresolved = new Relation(
            Group(n.Where(factor => !TryReadFactor(factor, out _))),
            Group(d.Where(factor => !TryReadFactor(factor, out _))));
        witness = ReadWitness.Create(new Carrier(), expression, Group(pairs), result, unresolved, standard);
        return true;
    }

    /// <summary>
    /// Reads surviving numerator units once no denominator remains. An empty
    /// numerator is a dimensionless read, not Carrier.Unknown's wildcard unit.
    /// </summary>
    public static bool TryReadNumeratorUnit(Relation expression, out UnitShape unit)
    {
        if (TryRead(expression, out var numerator, out var denominator, out _) &&
            TryReadGroup(denominator, out var d) && d.Length == 0 &&
            TryReadGroup(numerator, out var n) && n.Length != 0)
        {
            var units = new List<UnitShape>();
            foreach (var factor in n)
            {
                if (!TryReadFactor(factor, out var shape))
                {
                    unit = null!;
                    return false;
                }
                units.Add(shape);
            }
            unit = UnitShape.Product(units);
            return true;
        }

        unit = null!;
        return false;
    }

    /// <summary>Reads an empty numerator and denominator as no remaining dimensional factors.</summary>
    public static bool IsDimensionless(Relation expression) =>
        TryRead(expression, out var numerator, out var denominator, out _) &&
        TryReadGroup(numerator, out var n) && n.Length == 0 &&
        TryReadGroup(denominator, out var d) && d.Length == 0;

    private static bool TryReadFactor(IElement factor, out UnitShape unit)
    {
        if (factor is Relation { Left: CarrierLocation amount, Right: Carrier basis } &&
            !basis.IsUnknown && amount.Carrier.SameIdentityAs(basis) && amount.Ticks == 1 &&
            !basis.UnitShape.AlgebraicallyCompatibleWith(Carrier.Unknown.UnitShape))
        {
            unit = basis.UnitShape;
            return true;
        }

        unit = null!;
        return false;
    }

    private static Relation Group(IEnumerable<IElement> factors) => Relation.OrderedGroup(new Carrier(), [.. factors]);

    private static bool TryReadGroup(Relation group, out IElement[] elements)
    {
        if (group.Right is Carrier holder)
        {
            if (IsPointOn(group.Left, holder, 1))
            {
                elements = [];
                return true;
            }

            var collected = new List<IElement>();
            if (TryCollectAttachments(group.Left, holder, collected))
            {
                elements = [.. collected];
                return true;
            }
        }

        elements = [];
        return false;
    }

    // Unlike a discovery traversal, every branch must belong to the group.
    // A bare point or a skipped malformed branch is not an empty factor list.
    private static bool TryCollectAttachments(IElement element, Carrier holder, List<IElement> elements)
    {
        if (element is not Relation relation) return false;
        if (IsPointOn(relation.Left, holder, 0))
        {
            elements.Add(relation.Right);
            return true;
        }

        return TryCollectAttachments(relation.Left, holder, elements) &&
            TryCollectAttachments(relation.Right, holder, elements);
    }

    private static bool IsPointOn(IElement element, Carrier holder, long ticks) =>
        element is Relation { Left: CarrierLocation location, Right: Carrier carrier } &&
        carrier.SameIdentityAs(holder) && location.Carrier.SameIdentityAs(holder) && location.Ticks == ticks;
}
