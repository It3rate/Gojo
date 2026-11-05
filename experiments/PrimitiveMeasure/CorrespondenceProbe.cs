namespace Gojo.Experiments.PrimitiveMeasure;

/// <summary>Two exposed observations feed one packed relation row
/// [before, after, answer]. The final role is positive-context false/true;
/// 01 rejects the correspondence, 10 establishes it, 11 leaves it unresolved,
/// and 00 means the connected observations admit no tuple in this law.
/// This is a non-draining read. It neither applies the transformation nor
/// establishes a universal invariant from one successful comparison.</summary>
/// <remarks>The caller supplies a common local read basis. The ODV relation
/// codec can also compare explicitly encoded three-site patterns in the joint
/// apparatus; those role bits do not become ODV quantities. Matching widths
/// alone never authorizes composing different bases. This probe does not
/// discover or serialize a general basis correspondence.</remarks>
public sealed class CorrespondenceProbe : IPrimitiveReadPort
{
    private readonly ReadConnections connections = new();
    private readonly int table;
    private readonly CalculatedPort answer;
    private readonly ConnectionRow row;

    /// <summary>The immutable relation can be reused with other exposed ports.
    /// Its actual tuple bits, rather than a concept name, determine the read.</summary>
    public BitRelation Law { get; }
    public int ReferenceBits { get; }

    public CorrespondenceProbe(BitRelation law, IPrimitiveReadPort before, IPrimitiveReadPort after)
    {
        ArgumentNullException.ThrowIfNull(law);
        ArgumentNullException.ThrowIfNull(before);
        ArgumentNullException.ThrowIfNull(after);
        if (law.Arity != 3) throw new ArgumentException("The law needs before, after and answer roles.", nameof(law));
        for (byte first = 0; first < 8; first++)
        for (byte second = 0; second < 8; second++)
        for (byte output = 2; output < 8; output++)
            if (law.Allows(first, second, output))
                throw new ArgumentException("The answer role must use positive-context false or true.", nameof(law));

        Law = law;
        table = connections.AddTable(law);
        var firstEndpoint = connections.Import(before);
        var secondEndpoint = connections.Import(after);
        ReferenceBits = firstEndpoint == secondEndpoint ? 1 : 2;
        answer = connections.Connect(table, ReferenceBits, firstEndpoint, secondEndpoint);
        row = connections.LocationOf(answer.Endpoint).Row;
    }

    public byte ReadSupport() => answer.ReadSupport();
    public int[] Wiring() => connections.Wiring(table, row);
    public byte[] LawBytes() => connections.LawBytes(table);
    public byte[] RowBytes() => connections.PackedRows(table, ReferenceBits);

    /// <summary>Exact encoded-word equality, not identity, calibrated extent
    /// equality, or agreement of two independently uncertain support masks.</summary>
    public static BitRelation EqualWords() => Through([0, 1, 2, 3, 4, 5, 6, 7]);

    /// <summary>Compile after = map[before] into inspectable tuple bits. The
    /// map has one ODV result for each ODV input. A non-invertible map is legal
    /// here but this helper does not thereby claim that it is a symmetry.</summary>
    public static BitRelation Through(ReadOnlySpan<byte> map)
    {
        if (map.Length != 8) throw new ArgumentException("Supply one mapped word for each of the eight ODV words.", nameof(map));
        foreach (var word in map)
            if (word > 7) throw new ArgumentOutOfRangeException(nameof(map), "Mapped ODV words contain only three bits.");
        var tuples = new List<byte[]>();
        for (byte first = 0; first < 8; first++)
        for (byte second = 0; second < 8; second++)
            tuples.Add([first, second, (byte)(map[first] == second ? 1 : 0)]);
        return BitRelation.FromTuples(3, tuples);
    }
}

/// <summary>An explicit historical observation of an exposed source. Capturing
/// retains its support and source correspondence without consuming material.
/// Separate captures are separate observation events: common source ancestry
/// does not prove that an unknown value was unchanged between those events.</summary>
public sealed class CapturedRead : IPrimitiveReadPort
{
    public IPrimitiveReadPort Source { get; }
    private readonly byte support;

    private CapturedRead(IPrimitiveReadPort source)
    {
        Source = source;
        support = source.ReadSupport();
    }

    public static CapturedRead Capture(IPrimitiveReadPort source)
    {
        ArgumentNullException.ThrowIfNull(source);
        return new(source);
    }

    public byte ReadSupport() => support;
}
