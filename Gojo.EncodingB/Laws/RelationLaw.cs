namespace Gojo.EncodingB;

/// <summary>Admitted tuples over role domains. A relation only observes: it
/// never changes state. Read over supports it narrows possibilities, which is
/// monotone and therefore order-free (C1).</summary>
public sealed class RelationLaw : LawTable
{
    private int[][]? admitted;

    public RelationLaw(string name, IReadOnlyList<LawRole> roles, ReadOnlySpan<byte> bytes, string provenance = "authored")
        : base(name, roles, Product(roles), bytes, provenance)
    {
    }

    protected override string Kind => "relation";

    public ConfluenceClass Confluence => ConfluenceClass.MonotoneNarrowing;

    public static RelationLaw FromPredicate(string name, IReadOnlyList<LawRole> roles, Func<int[], bool> admits, string provenance = "authored")
    {
        ArgumentNullException.ThrowIfNull(admits);
        var count = Product(roles);
        var probe = new RelationLaw(name, roles, new byte[(count + 7) / 8], provenance);
        var set = Enumerable.Range(0, count).Where(index => admits(probe.Tuple(index)));
        return new RelationLaw(name, roles, PackBits(count, set), provenance);
    }

    public bool Admits(params int[] values) => BitAt(TupleIndex(values));

    /// <summary>Every admitted tuple, in index order.</summary>
    public IReadOnlyList<int[]> AdmittedTuples() =>
        admitted ??= Enumerable.Range(0, TupleCount).Where(BitAt).Select(Tuple).ToArray();

    /// <summary>Bit-sliced narrowing. <paramref name="supports"/>[r][v] is a lane
    /// plane: bit i means "in lane i, role r may still be v". Returns the narrowed
    /// planes: v stays possible for role r in a lane only if some admitted tuple
    /// with that value is supported by every other role in that lane.
    /// <paramref name="sameReference"/>[r][j], when given, marks lanes where roles r
    /// and j reference the same site; there only tuples with equal values count.
    /// A lane with any empty role support admits nothing.</summary>
    public ulong[][] Narrow(ulong[][] supports, ulong lanes, ulong[][]? sameReference = null)
    {
        ArgumentNullException.ThrowIfNull(supports);
        if (supports.Length != Roles.Count) throw new ArgumentException("Supply supports for every role.", nameof(supports));
        var next = new ulong[Roles.Count][];
        for (var r = 0; r < Roles.Count; r++)
        {
            if (supports[r].Length != Roles[r].Domain.Size)
                throw new ArgumentException($"Role {Roles[r].Name} needs {Roles[r].Domain.Size} value planes.", nameof(supports));
            next[r] = new ulong[Roles[r].Domain.Size];
        }
        foreach (var tuple in AdmittedTuples())
        {
            var lanesForTuple = lanes;
            for (var j = 0; j < tuple.Length && lanesForTuple != 0; j++) lanesForTuple &= supports[j][tuple[j]];
            if (lanesForTuple == 0) continue;
            if (sameReference is not null)
                for (var r = 0; r < tuple.Length; r++)
                    for (var j = r + 1; j < tuple.Length; j++)
                        if (tuple[r] != tuple[j]) lanesForTuple &= ~sameReference[r][j];
            if (lanesForTuple == 0) continue;
            for (var r = 0; r < tuple.Length; r++) next[r][tuple[r]] |= lanesForTuple;
        }
        return next;
    }

    /// <summary>A forward read for one instance: the values role
    /// <paramref name="role"/> can take given value masks for every role.
    /// <paramref name="references"/>, when given, names the site each role reads;
    /// equal references must take equal values (one site used twice).</summary>
    public ulong Project(int role, ReadOnlySpan<ulong> supports, ReadOnlySpan<int> references = default)
    {
        if (supports.Length != Roles.Count) throw new ArgumentException("Supply a support for every role.", nameof(supports));
        var lanes = new ulong[Roles.Count][];
        for (var r = 0; r < Roles.Count; r++)
        {
            lanes[r] = new ulong[Roles[r].Domain.Size];
            for (var v = 0; v < Roles[r].Domain.Size; v++)
                if (Bits.Get(supports[r], v)) lanes[r][v] = 1;
        }
        ulong[][]? same = null;
        if (!references.IsEmpty)
        {
            if (references.Length != Roles.Count) throw new ArgumentException("Supply a reference for every role.", nameof(references));
            same = new ulong[Roles.Count][];
            for (var r = 0; r < Roles.Count; r++)
            {
                same[r] = new ulong[Roles.Count];
                for (var j = 0; j < Roles.Count; j++) same[r][j] = references[r] == references[j] ? 1UL : 0;
            }
        }
        var narrowed = Narrow(lanes, 1, same);
        ulong result = 0;
        for (var v = 0; v < Roles[role].Domain.Size; v++)
            if (narrowed[role][v] != 0) result |= 1UL << v;
        return result;
    }
}
