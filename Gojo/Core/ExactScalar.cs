using System.Numerics;

namespace Gojo.Core;

// Derived arithmetic shared by selected readers. Durable values remain grounded
// rational participation in ordinary Gojo relations, never entries in a side table.
internal readonly record struct ExactScalar : IComparable<ExactScalar>
{
    internal ExactScalar(BigInteger numerator) : this(numerator, BigInteger.One) { }

    internal ExactScalar(BigInteger numerator, BigInteger denominator)
    {
        if (denominator.IsZero) throw new DivideByZeroException();
        if (denominator.Sign < 0) { numerator = -numerator; denominator = -denominator; }
        var gcd = BigInteger.GreatestCommonDivisor(numerator, denominator);
        Numerator = numerator / gcd; Denominator = denominator / gcd;
    }

    internal BigInteger Numerator { get; }
    internal BigInteger Denominator { get; }
    internal static ExactScalar Zero => new(0);
    internal static ExactScalar One => new(1);
    internal bool IsZero => Numerator.IsZero;
    internal BigInteger Floor() => Numerator.Sign < 0
        ? -((-Numerator + Denominator - 1) / Denominator) : Numerator / Denominator;
    internal BigInteger Ceiling() => -(-this).Floor();
    internal IElement Encode(Carrier basis) => ProjectionResidue.Create(basis, Numerator, Denominator);
    // Refuse irrational roots. A rational read must not silently invent a
    // floating-point tolerance or resolution for an unsupported exact value.
    internal bool TrySquareRoot(out ExactScalar root)
    {
        root = Zero;
        if (Numerator.Sign < 0) return false;
        static BigInteger FloorRoot(BigInteger value)
        {
            if (value <= 1) return value;
            var estimate = BigInteger.One << checked((int)((value.GetBitLength() + 1) / 2));
            while (true)
            {
                var next = (estimate + value / estimate) / 2;
                if (next >= estimate) return estimate;
                estimate = next;
            }
        }
        var n = FloorRoot(Numerator); var d = FloorRoot(Denominator);
        if (n * n != Numerator || d * d != Denominator) return false;
        root = new ExactScalar(n, d); return true;
    }
    internal static ExactScalar Min(ExactScalar a, ExactScalar b) => a <= b ? a : b;
    internal static ExactScalar Max(ExactScalar a, ExactScalar b) => a >= b ? a : b;
    public int CompareTo(ExactScalar other) => (Numerator * other.Denominator).CompareTo(other.Numerator * Denominator);
    public static ExactScalar operator +(ExactScalar a, ExactScalar b) =>
        new(a.Numerator * b.Denominator + b.Numerator * a.Denominator, a.Denominator * b.Denominator);
    public static ExactScalar operator -(ExactScalar a, ExactScalar b) => a + -b;
    public static ExactScalar operator -(ExactScalar a) => new(-a.Numerator, a.Denominator);
    public static ExactScalar operator *(ExactScalar a, ExactScalar b) => new(a.Numerator * b.Numerator, a.Denominator * b.Denominator);
    public static ExactScalar operator /(ExactScalar a, ExactScalar b) => new(a.Numerator * b.Denominator, a.Denominator * b.Numerator);
    public static bool operator <(ExactScalar a, ExactScalar b) => a.CompareTo(b) < 0;
    public static bool operator >(ExactScalar a, ExactScalar b) => a.CompareTo(b) > 0;
    public static bool operator <=(ExactScalar a, ExactScalar b) => a.CompareTo(b) <= 0;
    public static bool operator >=(ExactScalar a, ExactScalar b) => a.CompareTo(b) >= 0;
}
