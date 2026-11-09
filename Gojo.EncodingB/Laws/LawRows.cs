namespace Gojo.EncodingB;

/// <summary>Binds a law to rows of sites. Each role reads a plane or field of
/// the grammar; each row names the site every role reads. Row i is lane i when
/// the law runs bit-sliced. The law is shared; the rows are topology.</summary>
public sealed class LawRows
{
    private readonly int[][] rows;
    private readonly IReadOnlyList<string>[] rolePlanes;
    private readonly bool[] writtenRole;

    public LawRows(string name, LawTable law, SiteGrammar grammar, IReadOnlyList<string> roleFields, IReadOnlyList<int[]> rows)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(law);
        ArgumentNullException.ThrowIfNull(grammar);
        ArgumentNullException.ThrowIfNull(roleFields);
        ArgumentNullException.ThrowIfNull(rows);
        if (roleFields.Count != law.Roles.Count) throw new ArgumentException("Bind every role to a plane or field.", nameof(roleFields));
        if (rows.Count is < 1 or > Bits.Width) throw new ArgumentException("Bind 1 to 64 rows.", nameof(rows));
        Name = name;
        Law = law;
        Grammar = grammar;
        RoleFields = roleFields.ToArray();
        rolePlanes = new IReadOnlyList<string>[law.Roles.Count];
        for (var r = 0; r < law.Roles.Count; r++)
        {
            rolePlanes[r] = grammar.PlanesOf(roleFields[r]);
            if (rolePlanes[r].Count != law.Roles[r].Domain.CodeBits)
                throw new ArgumentException($"Role {law.Roles[r].Name} needs {law.Roles[r].Domain.CodeBits} planes; '{roleFields[r]}' has {rolePlanes[r].Count}.");
        }
        var written = law is TransitionLaw transition ? transition.WrittenRoles : [];
        writtenRole = law.Roles.Select(role => written.Contains(role.Name)).ToArray();
        this.rows = rows.Select(row => row.ToArray()).ToArray();
        foreach (var row in this.rows)
        {
            if (row.Length != law.Roles.Count) throw new ArgumentException($"Every row names {law.Roles.Count} sites.", nameof(rows));
            if (row.Any(site => site is < 0 or >= Bits.Width)) throw new ArgumentOutOfRangeException(nameof(rows));
            for (var r = 0; r < row.Length; r++)
                for (var j = 0; j < row.Length; j++)
                    if (r != j && writtenRole[r] && row[r] == row[j] && rolePlanes[r].Intersect(rolePlanes[j]).Any())
                        throw new ArgumentException(
                            $"Row [{string.Join(", ", row)}] uses site {row[r]} for written role {law.Roles[r].Name} and for {law.Roles[j].Name}. " +
                            "One site used twice is not two sites; a transition cannot write it as both.");
        }
    }

    /// <summary>Binds every row of a tile's row set.</summary>
    public static LawRows FromRowSet(string name, LawTable law, Tile tile, string rowSet, IReadOnlyList<string> roleFields)
    {
        var set = tile.Rows(rowSet);
        return new LawRows(name, law, tile.Grammar, roleFields, Enumerable.Range(0, set.Count).Select(set.Row).ToArray());
    }

    public string Name { get; }
    public LawTable Law { get; }
    public SiteGrammar Grammar { get; }
    public IReadOnlyList<string> RoleFields { get; }
    public IReadOnlyList<int[]> Rows => rows;
    public int Count => rows.Length;
    public ulong AllRows => Bits.Low(rows.Length);

    /// <summary>Sites a row reads or writes: the law's accounting scope for that row.</summary>
    public ulong SitesOf(int row) => Bits.Of(rows[row]);

    public ulong SitesOf(ulong rowMask) => Bits.Indexes(rowMask).Aggregate(0UL, (mask, row) => mask | SitesOf(row));

    /// <summary>Sites a row may write.</summary>
    public ulong WrittenSitesOf(int row) =>
        rows[row].Where((_, r) => writtenRole[r]).Aggregate(0UL, (mask, site) => mask | Bits.Of(site));

    public int TupleOf(IPlaneStore store, int row)
    {
        var values = new int[Law.Roles.Count];
        for (var r = 0; r < values.Length; r++) values[r] = Tile.ReadCode(store, RoleFields[r], rows[row][r]);
        return Law.TupleIndex(values);
    }

    /// <summary>Gathers role code planes into lanes: result[r][b] bit i is bit b of
    /// role r's code at row i's site.</summary>
    public ulong[][] GatherCodes(IPlaneStore store, ulong rowMask)
    {
        var codes = new ulong[Law.Roles.Count][];
        for (var r = 0; r < codes.Length; r++)
        {
            codes[r] = new ulong[rolePlanes[r].Count];
            for (var b = 0; b < codes[r].Length; b++)
            {
                var plane = store.Read(rolePlanes[r][b]);
                foreach (var row in Bits.Indexes(rowMask))
                    if (Bits.Get(plane, rows[row][r])) codes[r][b] |= Bits.Of(row);
            }
        }
        return codes;
    }

    /// <summary>Gathers per-site supports into lanes: result[r][v] bit i means role r
    /// may be value v at row i's site.</summary>
    public ulong[][] GatherSupports(Func<string, SupportPlanes> supportFor, ulong rowMask)
    {
        var lanes = new ulong[Law.Roles.Count][];
        for (var r = 0; r < lanes.Length; r++)
        {
            var support = supportFor(RoleFields[r]);
            lanes[r] = new ulong[Law.Roles[r].Domain.Size];
            foreach (var row in Bits.Indexes(rowMask))
            {
                var values = support.Values(rows[row][r]);
                foreach (var v in Bits.Indexes(values)) lanes[r][v] |= Bits.Of(row);
            }
        }
        return lanes;
    }

    /// <summary>Lanes where two roles read the same site: one site used twice.</summary>
    public ulong[][] SameReference()
    {
        var count = Law.Roles.Count;
        var same = new ulong[count][];
        for (var r = 0; r < count; r++)
        {
            same[r] = new ulong[count];
            for (var j = 0; j < count; j++)
                for (var row = 0; row < rows.Length; row++)
                    if (rows[row][r] == rows[row][j]) same[r][j] |= Bits.Of(row);
        }
        return same;
    }

    /// <summary>Rows whose current tuple the law does not admit. For a relation
    /// law this is its residual: how many rows are violated.</summary>
    public ulong Violations(IPlaneStore store)
    {
        if (Law is not RelationLaw relation) throw new InvalidOperationException("Violations are read through relation laws.");
        ulong violated = 0;
        for (var row = 0; row < rows.Length; row++)
            if (!relation.BitAt(TupleOf(store, row))) violated |= Bits.Of(row);
        return violated;
    }

    /// <summary>Groups rows into waves that can run in parallel. Two rows conflict
    /// when one writes a site the other reads. A row runs in the wave after every
    /// earlier row it conflicts with, so row order is the declared schedule
    /// between conflicting rows, and rows that do not conflict share a wave.</summary>
    public IReadOnlyList<ulong> Waves(ulong rowMask)
    {
        var wave = new int[rows.Length];
        var waves = new List<ulong>();
        foreach (var row in Bits.Indexes(rowMask & AllRows))
        {
            var level = 0;
            foreach (var earlier in Bits.Indexes(rowMask & Bits.Low(row)))
                if ((WrittenSitesOf(row) & SitesOf(earlier)) != 0 || (WrittenSitesOf(earlier) & SitesOf(row)) != 0)
                    level = Math.Max(level, wave[earlier] + 1);
            wave[row] = level;
            while (waves.Count <= level) waves.Add(0);
            waves[level] |= Bits.Of(row);
        }
        return waves;
    }

    /// <summary>Runs a transition law over the selected rows, wave by wave, each
    /// wave bit-sliced across its rows. Only written roles are scattered back.</summary>
    public TransitionWitness Apply(IPlaneStore store, ulong rowMask)
    {
        ArgumentNullException.ThrowIfNull(store);
        if (Law is not TransitionLaw transition) throw new InvalidOperationException("Only transition laws change state.");
        if (!ReferenceEquals(store.Grammar, Grammar)) throw new ArgumentException("The store uses a different site grammar.", nameof(store));
        var records = new List<RowTransition>();
        ulong unadmitted = 0, ambiguous = 0;
        var waves = Waves(rowMask);
        foreach (var wave in waves)
        {
            var before = Bits.Indexes(wave).ToDictionary(row => row, row => TupleOf(store, row));
            var step = transition.Step(GatherCodes(store, wave), wave);
            unadmitted |= step.Unadmitted;
            ambiguous |= step.Ambiguous;
            for (var r = 0; r < transition.Roles.Count; r++)
            {
                if (!writtenRole[r]) continue;
                for (var b = 0; b < rolePlanes[r].Count; b++)
                    foreach (var row in Bits.Indexes(wave))
                    {
                        var site = rows[row][r];
                        store.Write(rolePlanes[r][b], Bits.Get(step.After[r][b], row) ? Bits.Of(site) : 0, Bits.Of(site));
                    }
            }
            foreach (var row in Bits.Indexes(wave))
            {
                var after = TupleOf(store, row);
                records.Add(new RowTransition(row, before[row], after, transition.DescribeTuple(before[row]), transition.DescribeTuple(after)));
            }
        }
        return new TransitionWitness(Name, transition.ContentHash, records, unadmitted, ambiguous, waves);
    }
}

/// <summary>What one application of a transition law did, row by row.</summary>
public sealed record TransitionWitness(
    string Rows,
    string LawHash,
    IReadOnlyList<RowTransition> Transitions,
    ulong Unadmitted,
    ulong Ambiguous,
    IReadOnlyList<ulong> Waves)
{
    public bool Changed => Transitions.Any(t => t.Changed);
}

public sealed record RowTransition(int Row, int Before, int After, string BeforeRead, string AfterRead)
{
    public bool Changed => Before != After;
}
