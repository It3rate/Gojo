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

    public IReadOnlyList<int> Planes => planes;
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
/// role in a preserved sum: what leaves there arrives somewhere the law also
/// writes. That is a move, and nothing can be made or destroyed in another
/// structure.</para>
/// <para><b>Schedule.</b> Rows that write a position another row touches run in
/// a later wave and read what the earlier one wrote; row order decides which is
/// first. The rest run together. Declared order is structure, not a hidden
/// priority.</para></summary>
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
                    $"Role {r} writes through a port but is in no preserved sum. Writing another structure's bits is allowed only as a move, where what leaves arrives elsewhere.",
                    nameof(roles));
        }
    }

    public Law Law { get; }

    public IReadOnlyList<Binding> Bindings => bindings;

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
        return rows.Select(r => r[row]).ToArray();
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

    /// <summary>Live rows whose positions are all live.</summary>
    public Plane Runnable()
    {
        var runnable = live.Clone();
        foreach (var row in live.Indexes())
            if (!Sites(row).All(site => site.Structure.IsLive(site.Position))) runnable[row] = false;
        return runnable;
    }

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
                for (var b = 0; b < codes[r].Length; b++)
                {
                    var (structure, plane, position) = Site(row, r, b);
                    if (structure.Read(plane, position)) codes[r][b][row] = true;
                }
        return codes;
    }

    /// <summary>The value a role reads in a row, or null when its bits are not a code.</summary>
    public int? Value(int row, int role)
    {
        ulong pattern = 0;
        for (var b = 0; b < bindings[role].Planes.Count; b++)
        {
            var (structure, plane, position) = Site(row, role, b);
            if (structure.Read(plane, position)) pattern |= 1UL << b;
        }
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
        var lastWrite = new Dictionary<(Structure, int), int>();
        var lastTouch = new Dictionary<(Structure, int), int>();
        var waves = new List<Plane>();
        foreach (var row in rowMask.Indexes())
        {
            var touched = Sites(row).Select(s => (s.Structure, s.Position)).Distinct().ToArray();
            var written = WrittenSites(row).Distinct().ToArray();
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
    /// wave across all its rows at once. Only bits the law can change are
    /// written back.</summary>
    public WiringStep Apply(Plane rowMask)
    {
        ArgumentNullException.ThrowIfNull(rowMask);
        if (Law is not TransitionLaw transition) throw new InvalidOperationException("Only transition laws change state.");
        var runnable = Runnable();
        var lanes = rowMask.And(runnable);
        var skipped = rowMask.AndNot(runnable);
        var before = Enumerable.Repeat(-1, Count).ToArray();
        var after = Enumerable.Repeat(-1, Count).ToArray();
        foreach (var row in lanes.Indexes()) before[row] = TupleOf(row) ?? -1;
        var unadmitted = new Plane(Count);
        var ambiguous = new Plane(Count);
        var waves = Waves(lanes);
        foreach (var wave in waves)
        {
            var step = transition.Step(Gather(wave), wave);
            unadmitted = unadmitted.Or(step.Unadmitted);
            ambiguous = ambiguous.Or(step.Ambiguous);
            for (var r = 0; r < bindings.Length; r++)
                for (var b = 0; b < bindings[r].Planes.Count; b++)
                {
                    if (((changing[r] >> b) & 1) == 0) continue;
                    foreach (var row in wave.Indexes())
                    {
                        var (structure, plane, position) = Site(row, r, b);
                        structure.Write(plane, position, step.After[r][b][row]);
                    }
                }
        }
        var changed = new Plane(Count);
        foreach (var row in lanes.Indexes())
        {
            after[row] = TupleOf(row) ?? -1;
            if (after[row] != before[row]) changed[row] = true;
        }
        return new WiringStep(changed, unadmitted, ambiguous, skipped, before, after, waves);
    }

    internal (Structure Structure, int Plane, int Position) Site(int row, int role, int bit) =>
        bindings[role].Surface.Resolve(rows[role][row], bindings[role].Planes[bit]);

    internal IEnumerable<(Structure Structure, int Plane, int Position)> Sites(int row)
    {
        for (var r = 0; r < bindings.Length; r++)
            for (var b = 0; b < bindings[r].Planes.Count; b++)
                yield return Site(row, r, b);
    }

    internal IEnumerable<(Structure Structure, int Position)> WrittenSites(int row)
    {
        for (var r = 0; r < bindings.Length; r++)
            for (var b = 0; b < bindings[r].Planes.Count; b++)
                if (((changing[r] >> b) & 1) != 0)
                {
                    var site = Site(row, r, b);
                    yield return (site.Structure, site.Position);
                }
    }

    private void CheckRow(int[] indexes)
    {
        ArgumentNullException.ThrowIfNull(indexes);
        if (indexes.Length != bindings.Length) throw new ArgumentException($"A row names {bindings.Length} indexes, one per role.", nameof(indexes));
        for (var r = 0; r < indexes.Length; r++)
            if ((uint)indexes[r] >= (uint)bindings[r].Surface.Count) throw new ArgumentOutOfRangeException(nameof(indexes));
        for (var r = 0; r < indexes.Length; r++)
            for (var b = 0; b < bindings[r].Planes.Count; b++)
            {
                if (((changing[r] >> b) & 1) == 0) continue;
                var written = bindings[r].Surface.Resolve(indexes[r], bindings[r].Planes[b]);
                for (var j = 0; j < indexes.Length; j++)
                {
                    if (j == r) continue;
                    for (var c = 0; c < bindings[j].Planes.Count; c++)
                        if (bindings[j].Surface.Resolve(indexes[j], bindings[j].Planes[c]) == written)
                            throw new ArgumentException(
                                $"Roles {r} and {j} would hold the same bit, and role {r} writes it. One position used twice is not two positions.",
                                nameof(indexes));
                }
            }
    }

    private void CheckRowIndex(int row)
    {
        if ((uint)row >= (uint)Count) throw new ArgumentOutOfRangeException(nameof(row));
    }
}

/// <summary>What one application of a wiring did, row by row.
/// <see cref="Before"/> and <see cref="After"/> hold tuple indexes (−1 where a
/// role was not a code or the row did not run). Skipped rows touch a retired
/// position or are retired.</summary>
public sealed record WiringStep(
    Plane Changed,
    Plane Unadmitted,
    Plane Ambiguous,
    Plane Skipped,
    int[] Before,
    int[] After,
    IReadOnlyList<Plane> Waves);
