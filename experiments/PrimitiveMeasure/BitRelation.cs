namespace Gojo.Experiments.PrimitiveMeasure;

/// <summary>A small, explicit relation between one to three ODV-word roles.
/// Tuple (a,b,c) addresses bit a + 8*b + 64*c. A set bit admits that tuple;
/// it is not an operation tag, a stored result, or a material transition.
/// Arity comes from the containing layout; there is no bitmap header.</summary>
public sealed class BitRelation
{
    private readonly byte[] allowed;
    public int Arity { get; }

    public BitRelation(int arity, ReadOnlySpan<byte> allowedBitmap)
    {
        CheckArity(arity);
        var byteCount = (1 << (3 * arity)) / 8;
        if (allowedBitmap.Length != byteCount)
            throw new ArgumentException("Expected exactly one bit per possible tuple.", nameof(allowedBitmap));
        Arity = arity;
        // All bits are meaningful for the supported arities: 1, 8, or 64 bytes.
        allowed = allowedBitmap.ToArray();
    }

    public byte[] PackedBytes() => allowed.ToArray();

    public bool Allows(params byte[] words)
    {
        ArgumentNullException.ThrowIfNull(words);
        return Contains(TupleIndex(words, Arity));
    }

    /// <summary>Projects admitted tuples onto their final role. Each supplied
    /// byte is an eight-bit set of possible ODV words, not an ODV word itself.
    /// Zero means no admitted result; bit 0 means the concrete ODV word 000.
    /// Several set bits preserve alternatives and do not select actual flow.
    /// This overload imposes no shared-word constraint between input roles.</summary>
    public byte Predict(ReadOnlySpan<byte> inputSupports) => PredictCore(inputSupports, [], false);

    /// <summary>Retains explicit shared-input correspondence: roles with the
    /// same local reference must select the same candidate ODV word. Equal
    /// masks alone do not establish that correspondence, and distinct references
    /// do not establish statistical independence or different hidden magnitudes.</summary>
    public byte Predict(ReadOnlySpan<byte> inputSupports, ReadOnlySpan<int> inputReferences) =>
        PredictCore(inputSupports, inputReferences, true);

    private byte PredictCore(ReadOnlySpan<byte> inputSupports, ReadOnlySpan<int> inputReferences, bool constrainReferences)
    {
        if (inputSupports.Length != Arity - 1)
            throw new ArgumentException("Supply one support mask for every input role.", nameof(inputSupports));
        if (constrainReferences)
        {
            if (inputReferences.Length != inputSupports.Length)
                throw new ArgumentException("Supply one local reference for every input role.", nameof(inputReferences));
            foreach (var reference in inputReferences)
                if (reference < 0) throw new ArgumentOutOfRangeException(nameof(inputReferences));
        }
        foreach (var support in inputSupports)
            if (support == 0) return 0;

        byte result = 0;
        for (var tuple = 0; tuple < allowed.Length * 8; tuple++)
        {
            if (!Contains(tuple)) continue;
            var matches = true;
            for (var role = 0; role < inputSupports.Length && matches; role++)
            {
                var word = (tuple >> (3 * role)) & 7;
                if ((inputSupports[role] & (1 << word)) == 0)
                {
                    matches = false;
                    break;
                }
                if (constrainReferences)
                    for (var earlier = 0; earlier < role; earlier++)
                        if (inputReferences[earlier] == inputReferences[role] &&
                            ((tuple >> (3 * earlier)) & 7) != word)
                        {
                            matches = false;
                            break;
                        }
            }
            if (matches) result |= (byte)(1 << ((tuple >> (3 * (Arity - 1))) & 7));
        }
        return result;
    }

    /// <summary>Builds the inspectable bitmap from explicitly admitted tuples.
    /// Duplicate tuples are the same membership bit, not repeated material.</summary>
    public static BitRelation FromTuples(int arity, IEnumerable<byte[]> tuples)
    {
        CheckArity(arity);
        ArgumentNullException.ThrowIfNull(tuples);
        var packed = new byte[(1 << (3 * arity)) / 8];
        foreach (var words in tuples)
        {
            ArgumentNullException.ThrowIfNull(words);
            var bit = TupleIndex(words, arity);
            packed[bit / 8] |= (byte)(1 << (bit % 8));
        }
        return new BitRelation(arity, packed);
    }

    private bool Contains(int tuple) => (allowed[tuple / 8] & (1 << (tuple % 8))) != 0;

    private static int TupleIndex(ReadOnlySpan<byte> words, int arity)
    {
        if (words.Length != arity)
            throw new ArgumentException("Expected one ODV word per role.", nameof(words));
        var index = 0;
        for (var role = 0; role < words.Length; role++)
        {
            if (words[role] > 7) throw new ArgumentOutOfRangeException(nameof(words), "ODV words contain only three bits.");
            index |= words[role] << (3 * role);
        }
        return index;
    }

    private static void CheckArity(int arity)
    {
        if (arity is < 1 or > 3) throw new ArgumentOutOfRangeException(nameof(arity));
    }
}
