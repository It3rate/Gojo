namespace Gojo.EncodingC;

/// <summary>Stacked physical dynamics as EncodingC laws: the un-deferred
/// order-4 orientation generator J (GojoOrientationDefinitionAndUse-2026-11.md
/// §5), the rung-0 8-state Sides oscillator and 4-state Gray projection, and
/// the unified 2×3 order-and-sign family (first-order Napier decay/hold/growth
/// and second-order symplectic kick-drift for sin/cos, coasting multiply, and
/// cosh/sinh). All laws are integer bit-tables with zero System.Math calls.</summary>
public static class Dynamics
{
    private static readonly Dictionary<(int, int, int, bool), TransitionLaw> Shears = [];
    private static readonly Dictionary<(int, int, int), TransitionLaw> FirstOrders = [];
    private static readonly Dictionary<(int, int), TransitionLaw> SignKicks = [];

    /// <summary>The order-4 orientation generator J over (face, negative), one
    /// bit each (face: 0=F, 1=I; negative: 0=+, 1=−):
    /// <c>J(F, x) = (I, x)</c> (face flip), <c>J(I, x) = (F, −x)</c> (reorient).
    /// Functional and reversible (exact bijection): J² = Negate, J⁴ = id.</summary>
    public static TransitionLaw GeneratorJ { get; } = TransitionLaw.FromFunction(
        [Shape.Bit, Shape.Bit],
        s => s[0] == 0 ? [1, s[1]] : [0, 1 - s[1]],
        new Accounting(functional: true, reversible: true));

    /// <summary>Rung-0 unresolved-at Gray cycle over (xSign, vSign) where 1=+,
    /// 0=−: <c>11 → 10 → 00 → 01 → 11</c>. Two steps negate both signs; four
    /// return. Functional and reversible.</summary>
    public static TransitionLaw Rung0GrayCycle { get; } = TransitionLaw.FromFunction(
        [Shape.Bit, Shape.Bit],
        s => [s[1], 1 - s[0]],
        new Accounting(functional: true, reversible: true));

    /// <summary>Rung-0 8-state Sides oscillator over (xSide, vSide), each a
    /// 3-value signed side (0 = −, 1 = at/0, 2 = +):
    /// <c>(+1,0) → (+1,−1) → (0,−1) → (−1,−1) → (−1,0) → (−1,+1) → (0,+1) → (+1,+1) → (+1,0)</c>
    /// with fixed point (0,0). Its four axis states trace J's cycle
    /// <c>(F,+) → (I,−) → (F,−) → (I,+)</c>. Exact 9-state bijection.</summary>
    public static TransitionLaw Rung0Oscillator { get; } = TransitionLaw.FromFunction(
        [Shape.Binary(3), Shape.Binary(3)],
        s => (s[0] - 1, s[1] - 1) switch
        {
            (1, 0) => [2, 0],   // (+1, 0)  -> (+1, -1)  turned back
            (1, -1) => [1, 0],  // (+1, -1) -> (0, -1)
            (0, -1) => [0, 0],  // (0, -1)  -> (-1, -1)  crossed
            (-1, -1) => [0, 1], // (-1, -1) -> (-1, 0)
            (-1, 0) => [0, 2],  // (-1, 0)  -> (-1, +1)  turned back
            (-1, 1) => [1, 2],  // (-1, +1) -> (0, +1)
            (0, 1) => [2, 2],   // (0, +1)  -> (+1, +1)  crossed
            (1, 1) => [2, 1],   // (+1, +1) -> (+1, 0)
            _ => [1, 1],        // (0, 0)   -> (0, 0)    rest at landmark
        },
        new Accounting(functional: true, reversible: true));

    /// <summary>Integer coarse read through <paramref name="shift"/> binary
    /// rungs (truncating toward zero).</summary>
    public static int Coarse(int value, int shift) =>
        value < 0 ? -((-value) >> shift) : value >> shift;

    /// <summary>Offset-binary shape representing signed integers
    /// <c>[-maxAbs, +maxAbs]</c> via code <c>value + maxAbs</c>.</summary>
    public static Shape SignedShape(int maxAbs)
    {
        if (maxAbs < 1) throw new ArgumentOutOfRangeException(nameof(maxAbs));
        return Shape.Binary(2 * maxAbs + 1);
    }

    public static int EncodeSigned(int value, int maxAbs)
    {
        if (value < -maxAbs || value > maxAbs) throw new ArgumentOutOfRangeException(nameof(value));
        return value + maxAbs;
    }

    public static int DecodeSigned(int code, int maxAbs) => code - maxAbs;

    /// <summary>Signed shear step <c>target ← target + sign · coarse(source, shift)</c>
    /// over offset-binary signed roles <c>[-maxAbs, +maxAbs]</c>.
    /// When <paramref name="admitOutOfRange"/> is false (default), out-of-range
    /// transitions are unadmitted so the law is a checked <see cref="Accounting.Reversible"/>
    /// partial bijection (no information lost, boundary hits surface as Unadmitted);
    /// when true, out-of-range values clamp to ±maxAbs (functional).</summary>
    public static TransitionLaw SignedShear(int maxAbs, int shift, int sign, bool admitOutOfRange = false)
    {
        if (shift < 0) throw new ArgumentOutOfRangeException(nameof(shift));
        if (sign is < -1 or > 1) throw new ArgumentOutOfRangeException(nameof(sign), "Use feedback sign -1, 0, or +1.");
        var key = (maxAbs, shift, sign, admitOutOfRange);
        lock (Shears)
        {
            if (Shears.TryGetValue(key, out var cached)) return cached;
            var shape = SignedShape(maxAbs);
            return Shears[key] = TransitionLaw.FromFunction(
                [shape, shape],
                s =>
                {
                    var src = s[0] - maxAbs;
                    var dst = s[1] - maxAbs;
                    var next = dst + sign * Coarse(src, shift);
                    if (next < -maxAbs || next > maxAbs)
                    {
                        if (!admitOutOfRange) return null;
                        next = next < -maxAbs ? -maxAbs : maxAbs;
                    }
                    return [s[0], next + maxAbs];
                },
                new Accounting(functional: admitOutOfRange, reversible: !admitOutOfRange, new Unchanged(0)));
        }
    }

    /// <summary>First-order landmark step <c>x ← x − m · coarse(x, shift)</c>
    /// over nonnegative values 0..maxVal: <c>m = +1</c> gives Napier relaxation
    /// (logarithm: equal ratios in equal steps), <c>m = 0</c> holds, and
    /// <c>m = −1</c> compounds exponentially.</summary>
    public static TransitionLaw FirstOrderStep(int maxVal, int shift, int feedbackSign)
    {
        if (maxVal < 1) throw new ArgumentOutOfRangeException(nameof(maxVal));
        if (shift < 0) throw new ArgumentOutOfRangeException(nameof(shift));
        if (feedbackSign is < -1 or > 1) throw new ArgumentOutOfRangeException(nameof(feedbackSign));
        var key = (maxVal, shift, feedbackSign);
        lock (FirstOrders)
        {
            if (FirstOrders.TryGetValue(key, out var cached)) return cached;
            var shape = Shape.Binary(maxVal + 1);
            return FirstOrders[key] = TransitionLaw.FromFunction(
                [shape],
                s =>
                {
                    var next = s[0] - feedbackSign * (s[0] >> shift);
                    if (next < 0) next = 0;
                    if (next > maxVal) next = maxVal;
                    return [next];
                },
                new Accounting(true));
        }
    }

    /// <summary>Coarse-read constant-sign restoring kick: reads ONLY the sign
    /// of x (before/at/after landmark) and changes v by ±step toward the
    /// landmark, producing parabolic arcs (triangle velocity) instead of a
    /// sine wave. Roles: x (Unchanged), v (written).</summary>
    public static TransitionLaw ConstantSignKick(int maxAbs, int step = 1)
    {
        if (step < 1) throw new ArgumentOutOfRangeException(nameof(step));
        var key = (maxAbs, step);
        lock (SignKicks)
        {
            if (SignKicks.TryGetValue(key, out var cached)) return cached;
            var shape = SignedShape(maxAbs);
            return SignKicks[key] = TransitionLaw.FromFunction(
                [shape, shape],
                s =>
                {
                    var x = s[0] - maxAbs;
                    var v = s[1] - maxAbs;
                    var kick = x > 0 ? -step : x < 0 ? step : 0;
                    var next = v + kick;
                    return next < -maxAbs || next > maxAbs ? null : [s[0], next + maxAbs];
                },
                new Accounting(functional: false, reversible: true, new Unchanged(0)));
        }
    }
}
