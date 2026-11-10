namespace Gojo.EncodingC;

/// <summary>Admitted tuples of role values. A relation never changes state.
/// Read over supports it narrows what is still possible, which only removes
/// possibilities, so any fair order reaches the same result.</summary>
public sealed class RelationLaw : Law
{
    private int[][]? admitted;

    public RelationLaw(IReadOnlyList<Shape> roles, ReadOnlySpan<byte> bytes)
        : base(roles, Product(roles), bytes)
    {
    }

    public static RelationLaw FromPredicate(IReadOnlyList<Shape> roles, Func<int[], bool> admits)
    {
        ArgumentNullException.ThrowIfNull(admits);
        var count = Product(roles);
        var probe = new RelationLaw(roles, new byte[(count + 7) / 8]);
        var set = Enumerable.Range(0, count).Where(index => admits(probe.Tuple(index))).Select(i => (long)i);
        return new RelationLaw(roles, PackBits(count, set));
    }

    public bool Admits(params int[] values) => BitAt(TupleIndex(values));

    public IReadOnlyList<int[]> AdmittedTuples() =>
        admitted ??= Enumerable.Range(0, TupleCount).Where(BitAt).Select(Tuple).ToArray();

    /// <summary>Narrows supports across lanes in one pass. <paramref name="supports"/>[r][v]
    /// is a lane plane: bit i means "in lane i, role r may still be v". A value
    /// stays possible for a role in a lane only if some admitted tuple with that
    /// value is supported by every other role in the lane. Where
    /// <paramref name="sameReference"/>[r, j] is set, roles r and j read one
    /// position, and only tuples giving them equal values count. A lane with an
    /// empty role support admits nothing.</summary>
    public Plane[][] Narrow(Plane[][] supports, Plane lanes, Plane?[,]? sameReference = null)
    {
        ArgumentNullException.ThrowIfNull(supports);
        ArgumentNullException.ThrowIfNull(lanes);
        if (supports.Length != Roles.Count) throw new ArgumentException("Supply supports for every role.", nameof(supports));
        var next = new Plane[Roles.Count][];
        for (var r = 0; r < Roles.Count; r++)
        {
            if (supports[r].Length != Roles[r].Size) throw new ArgumentException($"Role {r} needs {Roles[r].Size} value planes.", nameof(supports));
            next[r] = new Plane[Roles[r].Size];
            for (var v = 0; v < Roles[r].Size; v++) next[r][v] = new Plane(lanes.Length);
        }
        foreach (var tuple in AdmittedTuples())
            for (var w = 0; w < lanes.WordCount; w++)
            {
                var m = lanes.Word(w);
                for (var j = 0; j < tuple.Length && m != 0; j++) m &= supports[j][tuple[j]].Word(w);
                if (m == 0) continue;
                if (sameReference is not null)
                    for (var r = 0; r < tuple.Length; r++)
                        for (var j = r + 1; j < tuple.Length; j++)
                            if (tuple[r] != tuple[j] && sameReference[r, j] is { } same) m &= ~same.Word(w);
                if (m == 0) continue;
                for (var r = 0; r < tuple.Length; r++) next[r][tuple[r]].OrWord(w, m);
            }
        return next;
    }

    /// <summary>The values role <paramref name="role"/> can take, given a value
    /// mask for every role. <paramref name="references"/>, when given, numbers the
    /// position each role reads; equal numbers are one position read twice and
    /// must take equal values.</summary>
    public ulong Project(int role, ReadOnlySpan<ulong> supports, ReadOnlySpan<int> references = default)
    {
        if (supports.Length != Roles.Count) throw new ArgumentException("Supply a support for every role.", nameof(supports));
        var lanes = new Plane[Roles.Count][];
        for (var r = 0; r < Roles.Count; r++)
        {
            lanes[r] = new Plane[Roles[r].Size];
            for (var v = 0; v < Roles[r].Size; v++)
            {
                lanes[r][v] = new Plane(1);
                lanes[r][v][0] = ((supports[r] >> v) & 1) != 0;
            }
        }
        Plane?[,]? same = null;
        if (!references.IsEmpty)
        {
            if (references.Length != Roles.Count) throw new ArgumentException("Supply a reference for every role.", nameof(references));
            same = new Plane?[Roles.Count, Roles.Count];
            for (var r = 0; r < Roles.Count; r++)
                for (var j = 0; j < Roles.Count; j++)
                    if (references[r] == references[j]) same[r, j] = Plane.Ones(1);
        }
        var narrowed = Narrow(lanes, Plane.Ones(1), same);
        ulong result = 0;
        for (var v = 0; v < Roles[role].Size; v++)
            if (narrowed[role][v][0]) result |= 1UL << v;
        return result;
    }
}
