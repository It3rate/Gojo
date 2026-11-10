namespace Gojo.EncodingB;

/// <summary>The three-bit word O D V, high to low.
/// O: 0 positive-right, 1 positive-left (which way this reader calls positive).
/// D: 0 positive, 1 negative, within that orientation.
/// V: 0 no magnitude detectable at this read's resolution, 1 detectable.
/// V = 1 is not the quantity one. Whether a word takes part in a computation is
/// not stored here: participation is selection, held by a workspace.</summary>
public readonly record struct OdvWord
{
    public const byte OrientationBit = 0b100;
    public const byte DirectionBit = 0b010;
    public const byte ValueBit = 0b001;

    private static readonly string[] Names = ["R+0", "R+?", "R-0", "R-?", "L+0", "L+?", "L-0", "L-?"];

    public OdvWord(byte code)
    {
        if (code > 7) throw new ArgumentOutOfRangeException(nameof(code), "An ODV word has three bits.");
        Code = code;
    }

    public byte Code { get; }

    /// <summary>The eight words as a domain; code k is word k.</summary>
    public static Domain Domain { get; } = new("odv", Names);

    public bool PositiveLeft => (Code & OrientationBit) != 0;
    public bool Negative => (Code & DirectionBit) != 0;
    public bool Detectable => (Code & ValueBit) != 0;

    /// <summary>"R+?" = positive-right frame, positive, detectable.</summary>
    public string Name => Names[Code];

    public static OdvWord Of(bool positiveLeft, bool negative, bool detectable) =>
        new((byte)((positiveLeft ? OrientationBit : 0) | (negative ? DirectionBit : 0) | (detectable ? ValueBit : 0)));

    public static OdvWord Parse(string name)
    {
        var code = Array.IndexOf(Names, name);
        return code >= 0 ? new OdvWord((byte)code) : throw new ArgumentException($"'{name}' is not an ODV word.", nameof(name));
    }

    /// <summary>The thing now points the other way: flip D. 001 → 011.</summary>
    public OdvWord Reverse() => new((byte)(Code ^ DirectionBit));

    /// <summary>The reader turns around; the thing does not move: flip O and D
    /// together. 001 → 111. V is kept, including at zero.</summary>
    public OdvWord Reorient() => new((byte)(Code ^ (OrientationBit | DirectionBit)));

    /// <summary>negative detectable &lt; zero &lt; positive detectable, within one
    /// orientation. Equal here does not assert equal hidden amounts. Different
    /// orientations have no selected comparison.</summary>
    public static int? Compare(OdvWord a, OdvWord b)
    {
        if (a.PositiveLeft != b.PositiveLeft) return null;
        return Coarse(a).CompareTo(Coarse(b));
    }

    public override string ToString() => Name;

    private static int Coarse(OdvWord word) => !word.Detectable ? 0 : word.Negative ? -1 : 1;
}

/// <summary>ODV storage and arithmetic as substrate laws.</summary>
public static class Odv
{
    private static readonly Dictionary<int, RelationLaw> AdditionLaws = [];

    /// <summary>Planes V and D stored per site; O supplied by the tile basis, so
    /// it is never stored. Field "word" reads them together as V + 2D + 4O.</summary>
    public static SiteGrammar Grammar { get; } = SiteGrammar.Create("odv")
        .Plane("V", "value", "no magnitude detectable at this resolution", "magnitude detectable (not the quantity one)", material: true)
        .Plane("D", "value", "positive within the tile orientation", "negative within the tile orientation", material: true)
        .BasisPlane("O", "basis", "positive-right orientation", "positive-left orientation")
        .Code("word", OdvWord.Domain, "V", "D", "O")
        .Build();

    /// <summary>Packs three bits per word at offsets 0, 3, 6 …, low bit first. No
    /// header; position supplies order. All eight words pack as 88 C6 FA.</summary>
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

    /// <summary>Reads a word by position. The caller knows the word count; trailing
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
    /// <paramref name="threshold"/>. Threshold 1 is the exact-zero contract: the
    /// historical table. Threshold 2 or more is a detection contract, under which
    /// V = 0 is still an operand: two undetectable positives can sum to a
    /// detectable one. A zero result keeps the operands' direction when they
    /// agree and admits both zero words when they do not. Across orientations
    /// nothing is admitted.</summary>
    public static RelationLaw Addition(int threshold = 1)
    {
        if (threshold < 1) throw new ArgumentOutOfRangeException(nameof(threshold));
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
            var roles = new[] { new LawRole("a", OdvWord.Domain), new LawRole("b", OdvWord.Domain), new LawRole("r", OdvWord.Domain) };
            var contract = threshold == 1 ? "exact-zero contract" : $"detection contract, threshold {threshold}";
            var law = RelationLaw.FromPredicate($"odv addition ({contract})", roles,
                t => admitted.Contains((t[0], t[1], t[2])), $"derived by enumerating a threshold-{threshold} reader");
            AdditionLaws[threshold] = law;
            return law;
        }
    }

    /// <summary>Possible result words, as a support mask over the eight words.
    /// A prediction, not a merge.</summary>
    public static ulong Add(OdvWord a, OdvWord b, int threshold = 1) =>
        Addition(threshold).Project(2, [Bits.Of(a.Code), Bits.Of(b.Code), OdvWord.Domain.Full]);

    /// <summary>Adds a word to itself through one reference: only diagonal tuples count.</summary>
    public static ulong AddSameReference(OdvWord x, int threshold = 1) =>
        Addition(threshold).Project(2, [Bits.Of(x.Code), Bits.Of(x.Code), OdvWord.Domain.Full], [0, 0, 1]);

    /// <summary>Reverses the second operand and adds. Equal words are not
    /// evidence for exact cancellation.</summary>
    public static ulong Subtract(OdvWord a, OdvWord b, int threshold = 1) => Add(a, b.Reverse(), threshold);

    /// <summary>The basis an ODV port publishes: the eight words, what they measure,
    /// and the addition law their words obey. The law fixes the zero contract, so
    /// an exact-zero port and a detection port have different bases even though
    /// their words have the same names.</summary>
    public static PortBasis Basis(int threshold, string unit) => new(OdvWord.Domain, unit, null, Addition(threshold));

    /// <summary>How a word read by a threshold-<paramref name="fromThreshold"/> reader
    /// may read for a threshold-<paramref name="toThreshold"/> reader, derived by
    /// enumerating hidden values. A zero keeps its direction, and the orientation
    /// is kept. From detection to exact, an undetectable word becomes competing
    /// candidates (zero, or detectable with the same sign), never an exact zero.</summary>
    public static RelationLaw ThresholdCorrespondence(int fromThreshold, int toThreshold)
    {
        if (fromThreshold < 1) throw new ArgumentOutOfRangeException(nameof(fromThreshold));
        if (toThreshold < 1) throw new ArgumentOutOfRangeException(nameof(toThreshold));
        var admitted = new HashSet<(int, int)>();
        var reach = 3 * Math.Max(fromThreshold, toThreshold) + 1;
        for (var orientation = 0; orientation <= 1; orientation++)
            for (var x = -reach; x <= reach; x++)
                foreach (var from in WordsOf(x, fromThreshold))
                    foreach (var to in x == 0 ? new[] { from & 0b10 } : WordsOf(x, toThreshold))
                        admitted.Add((from | orientation << 2, to | orientation << 2));
        var roles = new[] { new LawRole("from", OdvWord.Domain), new LawRole("to", OdvWord.Domain) };
        return RelationLaw.FromPredicate($"odv read at threshold {fromThreshold} as threshold {toThreshold}", roles,
            t => admitted.Contains((t[0], t[1])),
            $"derived by enumerating hidden values read at thresholds {fromThreshold} and {toThreshold}");
    }

    /// <summary>Reads every word in one orientation. A word from the other
    /// orientation is the same thing seen by a reader facing the other way, so it
    /// is reoriented (O and D flip together); the thing does not move.</summary>
    public static RelationLaw ReadIn(bool positiveLeft)
    {
        var roles = new[] { new LawRole("from", OdvWord.Domain), new LawRole("to", OdvWord.Domain) };
        return RelationLaw.FromPredicate($"odv read positive-{(positiveLeft ? "left" : "right")}", roles,
            t =>
            {
                var word = new OdvWord((byte)t[0]);
                return t[1] == (word.PositiveLeft == positiveLeft ? word : word.Reorient()).Code;
            },
            "a reader turning around flips O and D together");
    }

    /// <summary>Reorients a whole tile: flip O in the basis and XOR D with all
    /// allocated sites. Two plane operations for every site at once.</summary>
    public static void ReorientTile(Tile tile)
    {
        ArgumentNullException.ThrowIfNull(tile);
        tile.FlipBasis("O");
        tile.WriteState("D", tile.Read("D") ^ tile.Allocated, tile.Allocated);
    }

    /// <summary>Reverses the selected sites: D ^= m.</summary>
    public static void Reverse(Tile tile, ulong sites)
    {
        ArgumentNullException.ThrowIfNull(tile);
        tile.WriteState("D", tile.Read("D") ^ sites, sites);
    }

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
