namespace Gojo.Experiments.PrimitiveMeasure;

/// <summary>A published set of possible ODV words. Bit w admits word w;
/// this is a read result, not another material amount on the primitive.</summary>
public interface IPrimitiveReadPort
{
    byte ReadSupport();
}

public static class ReadSupport
{
    public static byte Of(byte word)
    {
        if (word > 7) throw new ArgumentOutOfRangeException(nameof(word));
        return (byte)(1 << word);
    }

    public static bool TrySingle(byte support, out byte word)
    {
        word = 0;
        if (support == 0 || (support & (support - 1)) != 0) return false;
        word = (byte)System.Numerics.BitOperations.TrailingZeroCount((uint)support);
        return true;
    }

    public static byte[] Words(byte support) => Enumerable.Range(0, 8)
        .Where(word => (support & (1 << word)) != 0).Select(word => (byte)word).ToArray();
}
