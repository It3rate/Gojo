namespace Gojo.EncodingC;

/// <summary>The three-bit word O D V, high to low.
/// O: 0 positive-right, 1 positive-left (which way this reader calls positive).
/// D: 0 positive, 1 negative, within that orientation.
/// V: 0 no magnitude detectable at this read's resolution, 1 detectable.
/// V = 1 is not the quantity one. Whether a word takes part in a computation is
/// not stored here: that is selection, which a workspace holds.</summary>
public readonly record struct OdvWord
{
    public const byte OrientationBit = 0b100;
    public const byte DirectionBit = 0b010;
    public const byte ValueBit = 0b001;

    public OdvWord(byte code)
    {
        if (code > 7) throw new ArgumentOutOfRangeException(nameof(code), "An ODV word has three bits.");
        Code = code;
    }

    public byte Code { get; }

    public bool PositiveLeft => (Code & OrientationBit) != 0;

    public bool Negative => (Code & DirectionBit) != 0;

    public bool Detectable => (Code & ValueBit) != 0;

    public static OdvWord Of(bool positiveLeft, bool negative, bool detectable) =>
        new((byte)((positiveLeft ? OrientationBit : 0) | (negative ? DirectionBit : 0) | (detectable ? ValueBit : 0)));

    /// <summary>The thing now points the other way: flip D. 001 → 011.</summary>
    public OdvWord Reverse() => new((byte)(Code ^ DirectionBit));

    /// <summary>The reader turns around and the thing does not move: flip O and D
    /// together. 001 → 111. V is kept, including at zero.</summary>
    public OdvWord Reorient() => new((byte)(Code ^ (OrientationBit | DirectionBit)));

    /// <summary>negative detectable &lt; undetectable &lt; positive detectable, within
    /// one orientation. Equal here does not assert equal hidden amounts. Words of
    /// different orientations have no comparison.</summary>
    public static int? Compare(OdvWord a, OdvWord b)
    {
        if (a.PositiveLeft != b.PositiveLeft) return null;
        return Coarse(a).CompareTo(Coarse(b));
    }

    public override string ToString() => OdvNotation.Name(this);

    private static int Coarse(OdvWord word) => !word.Detectable ? 0 : word.Negative ? -1 : 1;
}

/// <summary>ODV on a structure, and its arithmetic as laws.
/// V and D are planes 0 and 1, one bit per position. O is plane 2, a constant
/// plane: one bit for the whole structure, never stored per position. Reading
/// planes 0, 1, 2 together gives the word V + 2D + 4O.</summary>
public static class Odv
{
    public const int V = 0;
    public const int D = 1;
    public const int O = 2;

    /// <summary>The detection contract's threshold. Every threshold from 2 up
    /// gives the same addition table.</summary>
    public const int Detection = 2;

    /// <summary>The historical exact-zero contract: undetectable means a hidden
    /// zero. Never a default.</summary>
    public const int ExactZero = 1;

    private static readonly Dictionary<int, RelationLaw> AdditionLaws = [];

    /// <summary>Eight values in three bits.</summary>
    public static Shape Word { get; } = Shape.Binary(8);

    /// <summary>The word read from planes V, D and O.</summary>
    public static Field WordField { get; } = new(Word, V, D, O);

    public static Structure Create(int positions, bool positiveLeft = false)
    {
        var structure = new Structure(2, positions);
        structure.AddConstantPlane(positiveLeft);
        return structure;
    }

    public static OdvWord Read(Structure structure, int position) =>
        new((byte)(WordField.Read(structure, position) ?? throw new InvalidOperationException("Every three-bit pattern is a word.")));

    /// <summary>Writes V and D. The word's O must match the structure's.</summary>
    public static void Write(Structure structure, int position, OdvWord word) => WordField.Write(structure, position, word.Code);

    /// <summary>Reorients the whole structure: one constant bit and one XOR of D
    /// over every live position. Every word in it shares the orientation, so
    /// they all turn together.</summary>
    public static void Reorient(Structure structure)
    {
        ArgumentNullException.ThrowIfNull(structure);
        structure.SetConstant(O, !structure.ConstantValue(O));
        var live = structure.Live;
        structure.Write(D, structure.Read(D).Xor(live), live);
    }

    /// <summary>Reverses the given positions: D ^= positions.</summary>
    public static void Reverse(Structure structure, Plane positions)
    {
        ArgumentNullException.ThrowIfNull(structure);
        structure.Write(D, structure.Read(D).Xor(positions), positions);
    }

    /// <summary>Packs three bits per word at bit offsets 0, 3, 6 …, low bit
    /// first. No header; position supplies order. All eight words pack as
    /// 88 C6 FA.</summary>
    public static byte[] Pack(ReadOnlySpan<OdvWord> words)
    {
        var packed = new byte[(words.Length * 3 + 7) / 8];
        for (var index = 0; index < words.Length; index++)
        {
            var bit = index * 3;
            packed[bit / 8] |= (byte)(words[index].Code << (bit % 8));
            if (bit % 8 > 5) packed[bit / 8 + 1] |= (byte)(words[index].Code >> (8 - bit % 8));
        }
        return packed;
    }

    /// <summary>Reads a word by position. The reader knows the count; trailing
    /// padding is not another zero word.</summary>
    public static OdvWord At(ReadOnlySpan<byte> packed, int index)
    {
        var bit = index * 3L;
        if (index < 0 || bit + 3 > packed.Length * 8L) throw new ArgumentOutOfRangeException(nameof(index));
        var bits = (int)packed[(int)(bit / 8)];
        if (bit % 8 > 5) bits |= packed[(int)(bit / 8) + 1] << 8;
        return new OdvWord((byte)((bits >> (int)(bit % 8)) & 7));
    }

    /// <summary>Addition as a relation over (a, b, r), derived from a threshold
    /// reader: a hidden integer reads as detectable when its magnitude reaches
    /// <paramref name="threshold"/>. The default is the detection contract, under
    /// which V = 0 is still an operand: two undetectable positives can sum to a
    /// detectable one. Every threshold from 2 up gives this same table, so
    /// detection is one contract, not a family. Threshold 1 is the historical
    /// exact-zero contract, which treats an undetectable word as a hidden zero;
    /// it is there only for a reader that asks for it by name.
    /// <para>A zero read is a sample, not nothing: the word keeps its orientation
    /// and its place in the computation. Nothing is what is not selected. A zero
    /// result keeps the operands' direction when they agree and admits both zero
    /// words when they do not. Across orientations nothing is admitted.</para></summary>
    public static RelationLaw Addition(int threshold = Detection)
    {
        if (threshold < 1) throw new ArgumentOutOfRangeException(nameof(threshold));
        threshold = Math.Min(threshold, Detection);
        lock (AdditionLaws)
        {
            if (AdditionLaws.TryGetValue(threshold, out var cached)) return cached;
            var admitted = new HashSet<(int, int, int)>();
            var reach = 3 * threshold + 1;
            for (var orientation = 0; orientation <= 1; orientation++)
                for (var x = -reach; x <= reach; x++)
                    for (var y = -reach; y <= reach; y++)
                        foreach (var a in WordsOf(x, threshold))
                            foreach (var b in WordsOf(y, threshold))
                                foreach (var r in ResultWords(x + y, threshold, a, b))
                                    admitted.Add((a | orientation << 2, b | orientation << 2, r | orientation << 2));
            var law = RelationLaw.FromPredicate([Word, Word, Word], t => admitted.Contains((t[0], t[1], t[2])));
            AdditionLaws[threshold] = law;
            return law;
        }
    }

    /// <summary>Possible result words, as a mask over the eight words. A
    /// prediction, not a merge.</summary>
    public static ulong Add(OdvWord a, OdvWord b, int threshold = Detection) =>
        Addition(threshold).Project(2, [1UL << a.Code, 1UL << b.Code, 0xFF]);

    /// <summary>Adds a word to itself through one position: only diagonal tuples count.</summary>
    public static ulong AddSameReference(OdvWord x, int threshold = Detection) =>
        Addition(threshold).Project(2, [1UL << x.Code, 1UL << x.Code, 0xFF], [0, 0, 1]);

    /// <summary>Reverses the second operand and adds. Equal words are not
    /// evidence of exact cancellation.</summary>
    public static ulong Subtract(OdvWord a, OdvWord b, int threshold = Detection) => Add(a, b.Reverse(), threshold);

    /// <summary>A reader's calibration, if it wants one: how a word read at
    /// threshold <paramref name="fromThreshold"/> may read at threshold
    /// <paramref name="toThreshold"/>, derived by enumerating hidden values. A zero
    /// keeps its direction and the orientation is kept. From detection to exact,
    /// an undetectable word becomes competing candidates, never an exact zero.
    /// Only the order of the two thresholds, and whether either is exact, shape
    /// the table, so large thresholds read as small ones in the same order.</summary>
    public static RelationLaw ThresholdCorrespondence(int fromThreshold, int toThreshold)
    {
        if (fromThreshold < 1) throw new ArgumentOutOfRangeException(nameof(fromThreshold));
        if (toThreshold < 1) throw new ArgumentOutOfRangeException(nameof(toThreshold));
        (fromThreshold, toThreshold) = (Math.Min(fromThreshold, Detection) + (fromThreshold > toThreshold && toThreshold >= Detection ? 1 : 0),
            Math.Min(toThreshold, Detection) + (toThreshold > fromThreshold && fromThreshold >= Detection ? 1 : 0));
        var admitted = new HashSet<(int, int)>();
        var reach = 3 * Math.Max(fromThreshold, toThreshold) + 1;
        for (var orientation = 0; orientation <= 1; orientation++)
            for (var x = -reach; x <= reach; x++)
                foreach (var from in WordsOf(x, fromThreshold))
                    foreach (var to in x == 0 ? new[] { from & 0b10 } : WordsOf(x, toThreshold))
                        admitted.Add((from | orientation << 2, to | orientation << 2));
        return RelationLaw.FromPredicate([Word, Word], t => admitted.Contains((t[0], t[1])));
    }

    /// <summary>A reader's calibration for orientation: reads every word in one
    /// orientation. A word from the other orientation is the same thing seen by
    /// a reader facing the other way, so O and D flip together.</summary>
    public static RelationLaw ReadIn(bool positiveLeft) =>
        RelationLaw.FromPredicate([Word, Word], t =>
        {
            var word = new OdvWord((byte)t[0]);
            return t[1] == (word.PositiveLeft == positiveLeft ? word : word.Reorient()).Code;
        });

    private static IEnumerable<int> WordsOf(int hidden, int threshold)
    {
        if (hidden >= threshold) yield return 0b01;
        else if (hidden <= -threshold) yield return 0b11;
        else if (hidden > 0) yield return 0b00;
        else if (hidden < 0) yield return 0b10;
        else
        {
            yield return 0b00;
            yield return 0b10;
        }
    }

    private static IEnumerable<int> ResultWords(int sum, int threshold, int a, int b)
    {
        if (sum >= threshold) yield return 0b01;
        else if (sum <= -threshold) yield return 0b11;
        else if (sum > 0) yield return 0b00;
        else if (sum < 0) yield return 0b10;
        else if ((a & 0b10) == (b & 0b10)) yield return a & 0b10;
        else
        {
            yield return 0b00;
            yield return 0b10;
        }
    }
}
