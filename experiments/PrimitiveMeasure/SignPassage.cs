namespace Gojo.Experiments.PrimitiveMeasure;

/// <summary>Three openings in stored order: negative, zero, positive. Each bit
/// says whether that passage is present. This is connection shape, not another
/// measure word. Selection observes a settled word; it does not move flow.</summary>
public static class SignPassage
{
    public const byte Negative = 0b001;
    public const byte Zero = 0b010;
    public const byte Positive = 0b100;
    public const byte All = Negative | Zero | Positive;

    /// <summary>Return the open position selected by an actual observation, or
    /// no position when blocked. Both direction-marked zeros select position 1.
    /// All positions use the observed word's orientation.</summary>
    public static int? Select(byte observed, byte openings)
    {
        if (observed > 7) throw new ArgumentOutOfRangeException(nameof(observed));
        if (openings > 7) throw new ArgumentOutOfRangeException(nameof(openings));
        var position = (observed & ThreeBitMeasure.Value) == 0 ? 1
            : (observed & ThreeBitMeasure.Direction) != 0 ? 0 : 2;
        return (openings & (1 << position)) != 0 ? position : null;
    }
}
