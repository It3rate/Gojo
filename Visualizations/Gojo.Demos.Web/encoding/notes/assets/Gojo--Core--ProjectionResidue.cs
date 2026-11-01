using System.Numerics;

namespace Gojo.Core;

/// <summary>
/// Exact source participation left by an integer projection. Whole residue is
/// L(source,n). Fractional residue is L(source,n) -> P(source,d): n counts in
/// a d-part subdivision of one source tick, not n ordinary source ticks.
/// The relation supplies the scale; no fractional carrier state is implied.
/// </summary>
public static class ProjectionResidue
{
    /// <summary>Builds a reduced source residue, retaining a positive subdivision.</summary>
    public static IElement Create(Carrier source, long numerator, long denominator) =>
        Create(source, (BigInteger)numerator, denominator);

    internal static IElement Create(Carrier source, BigInteger numerator, BigInteger denominator)
    {
        ArgumentNullException.ThrowIfNull(source);
        if (denominator.IsZero)
        {
            throw new ArgumentOutOfRangeException(nameof(denominator), "A residue subdivision cannot be zero.");
        }

        if (denominator.Sign < 0)
        {
            numerator = -numerator;
            denominator = -denominator;
        }

        var divisor = BigInteger.GreatestCommonDivisor(numerator, denominator);
        numerator /= divisor;
        denominator /= divisor;
        var amount = new CarrierLocation(source, checked((long)numerator));
        return denominator.IsOne
            ? amount
            : new Relation(amount, Relation.Point(source, checked((long)denominator)));
    }

    /// <summary>
    /// Reads whole residue or an exact reduced subdivision. This is a contextual
    /// residue read, not a classifier for arbitrary relation semantics.
    /// </summary>
    public static bool TryRead(IElement residue, out Carrier source, out long numerator, out long denominator)
    {
        if (residue is CarrierLocation whole)
        {
            source = whole.Carrier;
            numerator = whole.Ticks;
            denominator = 1;
            return true;
        }

        if (residue is Relation
            {
                Left: CarrierLocation amount,
                Right: Relation { Left: CarrierLocation scale, Right: Carrier basis }
            } &&
            amount.Carrier.SameIdentityAs(basis) &&
            scale.Carrier.SameIdentityAs(basis) &&
            scale.Ticks > 1 && amount.Ticks != 0 &&
            BigInteger.GreatestCommonDivisor(amount.Ticks, scale.Ticks).IsOne)
        {
            source = basis;
            numerator = amount.Ticks;
            denominator = scale.Ticks;
            return true;
        }

        source = null!;
        numerator = 0;
        denominator = 0;
        return false;
    }
}
