namespace Gojo.EncodingC;

/// <summary>Where one role of a law reads and writes: a surface, and the planes
/// there that hold the role's bits, low bit first.</summary>
public sealed class Binding
{
    private readonly int[] planes;

    public Binding(Surface surface, params int[] planes)
    {
        ArgumentNullException.ThrowIfNull(surface);
        ArgumentNullException.ThrowIfNull(planes);
        foreach (var plane in planes)
            if ((uint)plane >= (uint)surface.PlaneCount) throw new ArgumentOutOfRangeException(nameof(planes));
        Surface = surface;
        this.planes = planes.ToArray();
    }

    public Surface Surface { get; }

    public IReadOnlyList<int> Planes => Array.AsReadOnly(planes);

    internal int Plane(int bit) => planes[bit];
}

/// <summary>A law bound to rows: every row names, for each role, the position
/// (or port lane) the role reads there. Row i is lane i when the law runs. The
/// law is shared; the rows are topology, stored as packed references at the
/// width the surface needs.
/// <para><b>Copying and moving are not flags.</b> A role the law never changes
/// is read: bound to a port, that is a reference, and any number of readers can
/// hold one. A role the law changes is written. Writing a structure's own
/// positions is that structure's business. Writing through a port reaches into
/// another structure, so it is allowed only when the law's accounting puts the
/// role in a preserved sum of positive amounts: what leaves there arrives
/// somewhere the law also writes. That is a move, and nothing can be made or
/// destroyed in another structure.</para>
/// <para><b>One bit is one position.</b> References are live, so a port's owner
/// can move a lane after a row was wired. A row whose roles now meet at a bit
/// one of them writes would let the law count one thing twice. It does not run,
/// and it is reported as aliased until the lane moves again.</para>
/// <para><b>Schedule.</b> Rows that write a position another row touches run in
/// a later wave and read what the earlier one wrote; row order decides which is
/// first. The rest run together. Declared order is structure, not a hidden
/// priority. Each row's before and after are taken around its own wave, so the
/// evidence says what that row did.</para></summary>
public sealed class Wiring
{
    private readonly Binding[] bindings;
    private readonly PackedInts[] rows;
    private readonly ulong[] changing;
    private Plane live = new(0);

    public Wiring(Law law, params Binding[] roles)
    {
        ArgumentNullException.ThrowIfNull(law);
        ArgumentNullException.ThrowIfNull(roles);
        if (roles.Length != law.Roles.Count) throw new ArgumentException($"Bind all {law.Roles.Count} roles.", nameof(roles));
        Law = law;
        bindings = roles.ToArray();
        changing = new ulong[roles.Length];
        rows = new PackedInts[roles.Length];
        for (var r = 0; r < roles.Length; r++)
        {
            if (roles[r].Planes.Count != law.Roles[r].Bits)
                throw new ArgumentException($"Role {r} has {law.Roles[r].Bits} bits; its binding names {roles[r].Planes.Count} planes.", nameof(roles));
            rows[r] = new PackedInts();
            if (law is not TransitionLaw transition) continue;
            changing[r] = transition.ChangingBits(r);
            if (changing[r] == 0) continue;
            for (var b = 0; b < roles[r].Planes.Count; b++)
                if (((changing[r] >> b) & 1) != 0 && roles[r].Surface.IsConstant(roles[r].Planes[b]))
                    throw new ArgumentException($"Role {r} would write a constant plane, which holds one bit for every position.", nameof(roles));
            if (roles[r].Surface.ThroughPort && !transition.Accounting.Conserves(r))
                throw new ArgumentException(
                    $"Role {r} writes through a port but is in no preserved sum of positive amounts. Writing another structure's bits is allowed only as a move, where what leaves arrives elsewhere.",
                    nameof(roles));
        }
    }

    public Law Law { get; }

    public IReadOnlyList<Binding> Bindings => Array.AsReadOnly(bindings);

    /// <summary>Rows ever added, retired ones included.</summary>
    public int Count => live.Length;

    public Plane Live => live.Clone();

    /// <summary>Advances on every row edit.</summary>
    public long Epoch { get; private set; }

    /// <summary>Whether the law changes this role (otherwise the role is only read).</summary>
    public bool Writes(int role) => changing[role] != 0;

    /// <summary>Adds a row: one surface index per role. Returns the row.</summary>
    public int Add(params int[] indexes)
    {
        CheckRow(indexes);
        for (var r = 0; r < rows.Length; r++) rows[r].Add(indexes[r]);
        live.Grow(live.Length + 1);
        live[live.Length - 1] = true;
        Epoch++;
        return live.Length - 1;
    }

    public int[] Row(int row)
    {
        CheckRowIndex(row);
        return Indexes(row);
    }

    /// <summary>Points one role of a row at another index: a structural edit.</summary>
    public void Rewire(int row, int role, int index)
    {
        var current = Row(row);
        current[role] = index;
        CheckRow(current);
        rows[role][row] = index;
        Epoch++;
    }

    /// <summary>Takes a row out of use. Its number is never reused.</summary>
    public void Retire(int row)
    {
        CheckRowIndex(row);
        live[row] = false;
        Epoch++;
    }

    /// <summary>Live rows whose positions are all live and whose roles do not
    /// meet at a written bit.</summary>
    public Plane Runnable() => Classify().Runnable;

    /// <summary>Live rows whose roles now meet at a bit one of them writes.
    /// They do not run.</summary>
    public Plane Aliased() => Classify().Aliased;

    /// <summary>Gathers role bits into lanes: result[r][b] bit i is bit b of role r in row i.</summary>
    public Plane[][] Gather(Plane lanes)
    {
        ArgumentNullException.ThrowIfNull(lanes);
        var codes = new Plane[bindings.Length][];
        for (var r = 0; r < codes.Length; r++)
        {
            codes[r] = new Plane[bindings[r].Planes.Count];
            for (var b = 0; b < codes[r].Length; b++) codes[r][b] = new Plane(Count);
        }
        foreach (var row in lanes.Indexes())
            for (var r = 0; r < codes.Length; r++)
            {
                var index = rows[r][row];
                for (var b = 0; b < codes[r].Length; b++)
                    if (bindings[r].Surface.Read(bindings[r].Plane(b), index)) codes[r][b][row] = true;
            }
        return codes;
    }

    /// <summary>The value a role reads in a row, or null when its bits are not a code.</summary>
    public int? Value(int row, int role)
    {
        ulong pattern = 0;
        var index = rows[role][row];
        for (var b = 0; b < bindings[role].Planes.Count; b++)
            if (bindings[role].Surface.Read(bindings[role].Plane(b), index)) pattern |= 1UL << b;
        return Law.Roles[role].Decode(pattern);
    }

    /// <summary>The row's whole state as a tuple index, or null when a role is not a code.</summary>
    public int? TupleOf(int row)
    {
        var values = new int[bindings.Length];
        for (var r = 0; r < values.Length; r++)
        {
            if (Value(row, r) is not int value) return null;
            values[r] = value;
        }
        return Law.TupleIndex(values);
    }

    /// <summary>Runnable rows whose state the relation does not admit: its residual.</summary>
    public Plane Violations()
    {
        if (Law is not RelationLaw relation) throw new InvalidOperationException("Violations are read through relation laws.");
        var violated = new Plane(Count);
        foreach (var row in Runnable().Indexes())
            if (TupleOf(row) is not int tuple || !relation.BitAt(tuple)) violated[row] = true;
        return violated;
    }

    /// <summary>Groups rows into waves. A row runs after every earlier row that
    /// writes a position it touches, or touches a position it writes.</summary>
    public IReadOnlyList<Plane> Waves(Plane rowMask)
    {
        ArgumentNullException.ThrowIfNull(rowMask);
        var lastWrite = new Dictionary<(Surface, int), int>();
        var lastTouch = new Dictionary<(Surface, int), int>();
        var waves = new List<Plane>();
        foreach (var row in rowMask.Indexes())
        {
            var touched = TouchedKeys(row).Distinct().ToArray();
            var written = WrittenKeys(row).Distinct().ToArray();
            var level = 0;
            foreach (var site in touched)
                if (lastWrite.TryGetValue(site, out var w)) level = Math.Max(level, w + 1);
            foreach (var site in written)
                if (lastTouch.TryGetValue(site, out var t)) level = Math.Max(level, t + 1);
            foreach (var site in touched) lastTouch[site] = Math.Max(lastTouch.GetValueOrDefault(site, -1), level);
            foreach (var site in written) lastWrite[site] = Math.Max(lastWrite.GetValueOrDefault(site, -1), level);
            while (waves.Count <= level) waves.Add(new Plane(Count));
            waves[level][row] = true;
        }
        return waves;
    }

    /// <summary>Runs every runnable row.</summary>
    public WiringStep Apply() => Apply(Live);

    /// <summary>Runs a transition law over the given rows, wave by wave, each
    /// wave across all its rows at once. Only bits that change are written. A
    /// row's before and after are read around its own wave.</summary>
    public WiringStep Apply(Plane rowMask)
    {
        ArgumentNullException.ThrowIfNull(rowMask);
        if (Law is not TransitionLaw transition) throw new InvalidOperationException("Only transition laws change state.");
        var (runnable, aliasedRows) = Classify();
        var lanes = rowMask.And(runnable);
        var aliased = rowMask.And(aliasedRows);
        var skipped = rowMask.AndNot(runnable).AndNot(aliased);
        var before = Enumerable.Repeat(-1, Count).ToArray();
        var after = Enumerable.Repeat(-1, Count).ToArray();
        var changed = new Plane(Count);
        var unadmitted = new Plane(Count);
        var ambiguous = new Plane(Count);
        var waves = Waves(lanes);
        foreach (var wave in waves)
        {
            foreach (var row in wave.Indexes()) before[row] = TupleOf(row) ?? -1;
            var step = transition.Step(Gather(wave), wave);
            unadmitted = unadmitted.Or(step.Unadmitted);
            ambiguous = ambiguous.Or(step.Ambiguous);
            foreach (var row in wave.Indexes())
                for (var r = 0; r < bindings.Length; r++)
                {
                    if (changing[r] == 0) continue;
                    var surface = bindings[r].Surface;
                    var index = rows[r][row];
                    for (var b = 0; b < bindings[r].Planes.Count; b++)
                    {
                        if (((changing[r] >> b) & 1) == 0) continue;
                        var plane = bindings[r].Plane(b);
                        var value = step.After[r][b][row];
                        if (surface.Read(plane, index) != value) surface.Set(plane, index, value);
                    }
                }
            foreach (var row in wave.Indexes())
            {
                after[row] = TupleOf(row) ?? -1;
                if (after[row] != before[row]) changed[row] = true;
            }
        }
        return new WiringStep(changed, unadmitted, ambiguous, skipped, aliased, before, after, waves);
    }

    /// <summary>The indexes of a row's roles that are bound to <paramref name="surface"/>.</summary>
    internal IEnumerable<int> IndexesOn(int row, Surface surface, bool writtenOnly = false)
    {
        for (var r = 0; r < bindings.Length; r++)
            if (ReferenceEquals(bindings[r].Surface, surface) && (!writtenOnly || changing[r] != 0))
                yield return rows[r][row];
    }

    internal IEnumerable<(Surface, int)> TouchedKeys(int row)
    {
        var keys = new List<(Surface, int)>();
        for (var r = 0; r < bindings.Length; r++)
        {
            var index = rows[r][row];
            bindings[r].Surface.ReadKeys(index, keys);
            if (changing[r] != 0) keys.Add(bindings[r].Surface.WriteKey(index));
        }
        return keys;
    }

    internal IEnumerable<(Surface, int)> WrittenKeys(int row)
    {
        for (var r = 0; r < bindings.Length; r++)
            if (changing[r] != 0) yield return bindings[r].Surface.WriteKey(rows[r][row]);
    }

    private int[] Indexes(int row) => rows.Select(r => r[row]).ToArray();

    private (Plane Runnable, Plane Aliased) Classify()
    {
        var runnable = new Plane(Count);
        var aliased = new Plane(Count);
        foreach (var row in live.Indexes())
        {
            var indexes = Indexes(row);
            var allLive = true;
            for (var r = 0; r < indexes.Length && allLive; r++) allLive = bindings[r].Surface.IsLive(indexes[r]);
            if (!allLive) continue;
            if (MeetAtAWrittenBit(indexes) is not null) aliased[row] = true;
            else runnable[row] = true;
        }
        return (runnable, aliased);
    }

    /// <summary>The first pair of roles that would hold one bit that one of
    /// them writes, or null.</summary>
    private (int Writer, int Other)? MeetAtAWrittenBit(int[] indexes)
    {
        for (var r = 0; r < indexes.Length; r++)
            for (var b = 0; b < bindings[r].Planes.Count; b++)
            {
                if (((changing[r] >> b) & 1) == 0) continue;
                var written = bindings[r].Surface.Bit(indexes[r], bindings[r].Plane(b));
                for (var j = 0; j < indexes.Length; j++)
                {
                    if (j == r) continue;
                    for (var c = 0; c < bindings[j].Planes.Count; c++)
                        if (bindings[j].Surface.Bit(indexes[j], bindings[j].Plane(c)) == written) return (r, j);
                }
            }
        return null;
    }

    private void CheckRow(int[] indexes)
    {
        ArgumentNullException.ThrowIfNull(indexes);
        if (indexes.Length != bindings.Length) throw new ArgumentException($"A row names {bindings.Length} indexes, one per role.", nameof(indexes));
        for (var r = 0; r < indexes.Length; r++)
            if ((uint)indexes[r] >= (uint)bindings[r].Surface.Count) throw new ArgumentOutOfRangeException(nameof(indexes));
        if (MeetAtAWrittenBit(indexes) is var (writer, other))
            throw new ArgumentException(
                $"Roles {writer} and {other} would hold the same bit, and role {writer} writes it. One position used twice is not two positions.",
                nameof(indexes));
    }

    private void CheckRowIndex(int row)
    {
        if ((uint)row >= (uint)Count) throw new ArgumentOutOfRangeException(nameof(row));
    }
}

/// <summary>What one application of a wiring did, row by row.
/// <see cref="Before"/> and <see cref="After"/> hold tuple indexes (−1 where a
/// role was not a code or the row did not run), read around the row's own
/// wave. Skipped rows touch a retired position or are retired. Aliased rows
/// had roles meeting at a written bit and did not run.</summary>
public sealed record WiringStep(
    Plane Changed,
    Plane Unadmitted,
    Plane Ambiguous,
    Plane Skipped,
    Plane Aliased,
    int[] Before,
    int[] After,
    IReadOnlyList<Plane> Waves);
