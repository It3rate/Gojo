namespace Gojo.EncodingC;

/// <summary>Where one role of a law reads and writes: a surface, and planes
/// there. A plain binding names the planes that hold the role's bits, low bit
/// first: a code, one value. An <see cref="Open"/> binding names one plane per
/// value, set where the value is ruled out: what the role may still be.</summary>
public sealed class Binding
{
    private readonly int[] planes;

    public Binding(Surface surface, params int[] planes)
        : this(surface, planes, false)
    {
    }

    private Binding(Surface surface, int[] planes, bool open)
    {
        ArgumentNullException.ThrowIfNull(surface);
        ArgumentNullException.ThrowIfNull(planes);
        foreach (var plane in planes)
            if ((uint)plane >= (uint)surface.PlaneCount) throw new ArgumentOutOfRangeException(nameof(planes));
        if (planes.Distinct().Count() != planes.Length) throw new ArgumentException("A plane holds one bit of a role.", nameof(planes));
        Surface = surface;
        IsOpen = open;
        this.planes = planes.ToArray();
    }

    /// <summary>Binds a role to a value that is not settled: one plane per
    /// value of the role, set where that value is ruled out. Relation laws
    /// narrow such roles.</summary>
    public static Binding Open(Surface surface, params int[] planes) => new(surface, planes, true);

    public Surface Surface { get; }

    /// <summary>The planes are one per value, not the bits of a code.</summary>
    public bool IsOpen { get; }

    public IReadOnlyList<int> Planes => Array.AsReadOnly(planes);

    internal int Plane(int bit) => planes[bit];
}

/// <summary>A law bound to rows: every row names, for each role, the position
/// (or port lane) the role reads there. Row i is lane i when the law runs. The
/// law is shared; the rows are topology.
/// <para><b>Rows are positions.</b> A wiring's rows are the positions of a
/// structure of its own, <see cref="Rows"/>, and a row's references are values
/// in that structure's planes, at the width the surface needs. Topology is
/// state like any other: a law bound to those planes rewires a row by writing
/// a reference, a saved machine keeps it as it keeps any structure, and a
/// retired row is a retired position. A reference a law has written to
/// nowhere is not an error; the row does not run and is reported.</para>
/// <para><b>One rule per kind of law.</b> A transition law changes state: each
/// row takes the next state its table gives. A relation law changes what is
/// known: each row rules out, for roles bound open, the values no admitted
/// tuple supports. Plain roles of a relation are samples and are only read.
/// Both run across all rows at once from the table alone.</para>
/// <para><b>Copying and moving are not flags.</b> A role the law never changes
/// is read: bound to a port, that is a reference, and any number of readers can
/// hold one. A role the law changes is written. Writing a structure's own
/// positions is that structure's business. Writing through a port reaches into
/// another structure, so it is allowed only when the law's accounting puts the
/// role in a preserved sum of positive amounts: what leaves there arrives
/// somewhere the law also writes. That is a move, and nothing can be made or
/// destroyed in another structure. Narrowing never writes through a port: a
/// reader may see what another structure considers possible, and cannot rule
/// anything out there. What is supposed never rules anything out in what is
/// known: when any role of a relation reads a rung, only roles on rungs are
/// narrowed.</para>
/// <para><b>One bit is one position.</b> References are live, so a port's owner
/// can move a lane, or a law a reference, after a row was wired. A row whose
/// roles now meet at a bit one of them writes would let the law count one
/// thing twice. It does not run, and it is reported as aliased until
/// something moves again.</para>
/// <para><b>Schedule.</b> Rows that write a position another row touches run in
/// a later wave and read what the earlier one wrote; row order decides which is
/// first. The rest run together. Declared order is structure, not a hidden
/// priority. Each row's before and after are taken around its own wave, so the
/// evidence says what that row did.</para>
/// <para><b>What a row did.</b> After <see cref="Record"/>, every run leaves
/// its result in planes of <see cref="Rows"/>, one bit per row and kind, where
/// laws and selections can read it: a count of what flowed through a row, or
/// attention drawn to what did not resolve.</para></summary>
public sealed class Wiring
{
    private readonly Binding[] bindings;
    private readonly List<int>[] references;
    private readonly ulong[] changing;
    private readonly bool[] writes;
    private Evidence? evidence;

    public Wiring(Law law, params Binding[] roles)
        : this(law, roles, null, null, null)
    {
    }

    /// <summary>A wiring over rows that already exist, as a saved machine has them.</summary>
    internal Wiring(Law law, Binding[] roles, Structure? rows, int[][]? referencePlanes, int? evidenceFirst)
    {
        ArgumentNullException.ThrowIfNull(law);
        ArgumentNullException.ThrowIfNull(roles);
        if (roles.Length != law.Roles.Count) throw new ArgumentException($"Bind all {law.Roles.Count} roles.", nameof(roles));
        Law = law;
        bindings = roles.ToArray();
        changing = new ulong[roles.Length];
        writes = new bool[roles.Length];
        var transition = law as TransitionLaw;
        var supposed = roles.Any(role => role.Surface is Rung);
        for (var r = 0; r < roles.Length; r++)
        {
            var surface = roles[r].Surface;
            if (roles[r].IsOpen)
            {
                if (transition is not null)
                    throw new ArgumentException($"Role {r} is bound open. A transition law runs on codes; relation laws narrow what is open.", nameof(roles));
                if (roles[r].Planes.Count != law.Roles[r].Size)
                    throw new ArgumentException($"Role {r} has {law.Roles[r].Size} values; an open binding names one plane for each.", nameof(roles));
                if (roles[r].Planes.Any(surface.IsConstant))
                    throw new ArgumentException($"Role {r} is bound open on a constant plane, which holds one bit for every position.", nameof(roles));
                writes[r] = !surface.ThroughPort && !surface.ReadOnly && (!supposed || surface is Rung);
                continue;
            }
            if (roles[r].Planes.Count != law.Roles[r].Bits)
                throw new ArgumentException($"Role {r} has {law.Roles[r].Bits} bits; its binding names {roles[r].Planes.Count} planes.", nameof(roles));
            if (transition is null) continue;
            changing[r] = transition.ChangingBits(r);
            writes[r] = changing[r] != 0;
            if (!writes[r]) continue;
            for (var b = 0; b < roles[r].Planes.Count; b++)
                if (((changing[r] >> b) & 1) != 0 && surface.IsConstant(roles[r].Planes[b]))
                    throw new ArgumentException($"Role {r} would write a constant plane, which holds one bit for every position.", nameof(roles));
            if (surface.ReadOnly)
                throw new ArgumentException($"Role {r} would write a surface that is a read of other things.", nameof(roles));
            if (surface.ThroughPort && !transition.Accounting.Conserves(r))
                throw new ArgumentException(
                    $"Role {r} writes through a port but is in no preserved sum of positive amounts. Writing another structure's bits is allowed only as a move, where what leaves arrives elsewhere.",
                    nameof(roles));
        }
        if (transition is not null)
        {
            var rung = Enumerable.Range(0, roles.Length).Where(r => writes[r]).Select(r => roles[r].Surface).OfType<Rung>().FirstOrDefault();
            if (rung is not null && Enumerable.Range(0, roles.Length).Any(r => writes[r] && !ReferenceEquals(roles[r].Surface, rung)))
                throw new ArgumentException(
                    "A law that writes a rung writes only that rung. A supposition and what is are never changed in one step; the rung's commit decides what reaches anything else.",
                    nameof(roles));
        }

        Rows = rows ?? new Structure(0);
        references = new List<int>[roles.Length];
        for (var r = 0; r < roles.Length; r++)
        {
            if (referencePlanes is not null)
            {
                if (referencePlanes[r].Length is < 1 or > 31 || referencePlanes[r].Any(plane => (uint)plane >= (uint)Rows.PlaneCount || Rows.IsConstantPlane(plane)))
                    throw new ArgumentException($"Role {r}'s references do not sit in the rows' planes.", nameof(referencePlanes));
                references[r] = [.. referencePlanes[r]];
                continue;
            }
            references[r] = [];
            Widen(r, roles[r].Surface.Count > 0 ? roles[r].Surface.Count - 1 : 0);
        }
        if (evidenceFirst is int first)
        {
            if (first < 0 || first + Evidence.Kinds > Rows.PlaneCount) throw new ArgumentException("The evidence planes are not among the rows' planes.", nameof(evidenceFirst));
            evidence = Evidence.From(first);
        }
    }

    public Law Law { get; }

    public IReadOnlyList<Binding> Bindings => Array.AsReadOnly(bindings);

    /// <summary>The rows as a structure: one position per row, a reference per
    /// role in its planes, and the evidence planes once recording.</summary>
    public Structure Rows { get; }

    /// <summary>Rows ever added, retired ones included.</summary>
    public int Count => Rows.Count;

    public Plane Live => Rows.Live;

    /// <summary>Whether running the law writes this role: a transition changes
    /// it, or a relation narrows it. Otherwise the role is only read.</summary>
    public bool Writes(int role) => writes[role];

    /// <summary>The planes of <see cref="Rows"/> that hold a role's reference,
    /// low bit first. A law bound to them reads or rewires the rows.</summary>
    public IReadOnlyList<int> ReferencePlanes(int role) => references[role].AsReadOnly();

    /// <summary>The evidence planes, or null when the wiring is not recording.</summary>
    public Evidence? Recorded => evidence;

    /// <summary>From now on, leaves what each run did in six planes of
    /// <see cref="Rows"/>, one bit per row: changed, unread, unadmitted,
    /// ambiguous, skipped, aliased. Returns the planes.</summary>
    public Evidence Record()
    {
        if (evidence is not null) return evidence;
        var first = Rows.PlaneCount;
        for (var kind = 0; kind < Evidence.Kinds; kind++) Rows.AddPlane();
        return evidence = Evidence.From(first);
    }

    /// <summary>Adds a row: one surface index per role. Returns the row.</summary>
    public int Add(params int[] indexes)
    {
        CheckRow(indexes);
        for (var r = 0; r < indexes.Length; r++) Widen(r, indexes[r]);
        var row = Rows.Add();
        for (var r = 0; r < indexes.Length; r++) Point(row, r, indexes[r]);
        return row;
    }

    /// <summary>Adds a row that is wired and not yet in use: a connection that
    /// could exist. It does not run until something recruits it, which a law
    /// does by setting the row's bit in <c>Rows.Life</c>. Returns the row.</summary>
    public int AddSpare(params int[] indexes)
    {
        CheckRow(indexes);
        for (var r = 0; r < indexes.Length; r++) Widen(r, indexes[r]);
        var row = Rows.AddSpare();
        for (var r = 0; r < indexes.Length; r++) Point(row, r, indexes[r]);
        return row;
    }

    /// <summary>The index each role holds in a row, as the planes have it now.</summary>
    public int[] Row(int row)
    {
        CheckRowIndex(row);
        return Indexes(row);
    }

    /// <summary>Points one role of a row at another index: a structural edit.
    /// A law does the same by writing the reference planes.</summary>
    public void Rewire(int row, int role, int index)
    {
        var current = Row(row);
        current[role] = index;
        CheckRow(current);
        Widen(role, index);
        Point(row, role, index);
    }

    /// <summary>Takes a row out of use. Its number is never reused.</summary>
    public void Retire(int row)
    {
        CheckRowIndex(row);
        if (Rows.IsLive(row)) Rows.Retire(row);
    }

    /// <summary>Live rows whose references reach live positions and whose roles
    /// do not meet at a written bit.</summary>
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
                var index = Reference(row, r);
                for (var b = 0; b < codes[r].Length; b++)
                    if (bindings[r].Surface.Read(bindings[r].Plane(b), index)) codes[r][b][row] = true;
            }
        return codes;
    }

    /// <summary>The value a role reads in a row: its code, or for an open role
    /// the one value left. Null when the bits are not a code, the role is not
    /// settled, or the reference reaches nowhere.</summary>
    public int? Value(int row, int role)
    {
        var binding = bindings[role];
        var index = Reference(row, role);
        if ((uint)index >= (uint)binding.Surface.Count) return null;
        if (binding.IsOpen)
        {
            int? only = null;
            for (var v = 0; v < binding.Planes.Count; v++)
            {
                if (binding.Surface.Read(binding.Plane(v), index)) continue;
                if (only is not null) return null;
                only = v;
            }
            return only;
        }
        ulong pattern = 0;
        for (var b = 0; b < binding.Planes.Count; b++)
            if (binding.Surface.Read(binding.Plane(b), index)) pattern |= 1UL << b;
        return Law.Roles[role].Decode(pattern);
    }

    /// <summary>The row's whole state as a tuple index, or null when a role has no one value.</summary>
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
                if (lastWrite.TryGetValue(site, out var w) && w >= level) level = w + 1;
            foreach (var site in written)
                if (lastTouch.TryGetValue(site, out var t) && t >= level) level = t + 1;
            foreach (var site in touched)
                if (lastTouch.GetValueOrDefault(site, -1) < level) lastTouch[site] = level;
            foreach (var site in written)
                if (lastWrite.GetValueOrDefault(site, -1) < level) lastWrite[site] = level;
            while (waves.Count <= level) waves.Add(new Plane(Count));
            waves[level][row] = true;
        }
        return waves;
    }

    /// <summary>Runs every runnable row.</summary>
    public WiringStep Apply() => Apply(Live);

    /// <summary>Runs the law over the given rows, wave by wave, each wave across
    /// all its rows at once. A transition law changes state; a relation law
    /// rules out what no admitted tuple supports. Only bits that change are
    /// written.</summary>
    public WiringStep Apply(Plane rowMask)
    {
        ArgumentNullException.ThrowIfNull(rowMask);
        var step = Law switch
        {
            TransitionLaw transition => Run(rowMask, transition),
            RelationLaw relation => Narrow(rowMask, relation),
            _ => throw new InvalidOperationException("A law is a transition or a relation."),
        };
        if (evidence is not null)
        {
            var mask = rowMask.And(Rows.Live);
            Rows.Write(evidence.Changed, step.Changed, mask);
            Rows.Write(evidence.Unread, step.Unread, mask);
            Rows.Write(evidence.Unadmitted, step.Unadmitted, mask);
            Rows.Write(evidence.Ambiguous, step.Ambiguous, mask);
            Rows.Write(evidence.Skipped, step.Skipped, mask);
            Rows.Write(evidence.Aliased, step.Aliased, mask);
        }
        return step;
    }

    private WiringStep Run(Plane rowMask, TransitionLaw transition)
    {
        var (runnable, aliasedRows) = Classify();
        var lanes = rowMask.And(runnable);
        var aliased = rowMask.And(aliasedRows);
        var skipped = rowMask.AndNot(runnable).AndNot(aliased);
        var before = Enumerable.Repeat(-1, Count).ToArray();
        var after = Enumerable.Repeat(-1, Count).ToArray();
        var changed = new Plane(Count);
        var unread = new Plane(Count);
        var unadmitted = new Plane(Count);
        var ambiguous = new Plane(Count);
        var waves = Waves(lanes);
        foreach (var wave in waves)
        {
            foreach (var row in wave.Indexes()) before[row] = TupleOf(row) ?? -1;
            var step = transition.Step(Gather(wave), wave);
            unread = unread.Or(step.Unread);
            unadmitted = unadmitted.Or(step.Unadmitted);
            ambiguous = ambiguous.Or(step.Ambiguous);
            foreach (var row in wave.Indexes())
                for (var r = 0; r < bindings.Length; r++)
                {
                    if (!writes[r]) continue;
                    var surface = bindings[r].Surface;
                    var index = Reference(row, r);
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
                if (after[row] == before[row]) continue;
                changed[row] = true;
                foreach (var surface in bindings.Where((_, r) => writes[r]).Select(b => b.Surface).Distinct())
                {
                    var written = IndexesOn(row, surface, writtenOnly: true).Distinct().ToArray();
                    if (written.Length > 1) surface.ChangedTogether(written);
                }
            }
        }
        return new WiringStep(changed, unread, unadmitted, ambiguous, skipped, aliased, before, after, waves);
    }

    /// <summary>The relation's step. Each role's support in a row is what its
    /// planes leave possible (an open role) or the one value its bits read (a
    /// sample). A value stays possible only if some admitted tuple with it is
    /// supported by every other role. The same vocabulary reports it: a row is
    /// changed when something was ruled out, unread when a sample is not a
    /// code, unadmitted when no tuple fits at all (a contradiction, left in the
    /// planes it could write), and ambiguous while an open role still has
    /// several values.</summary>
    private WiringStep Narrow(Plane rowMask, RelationLaw relation)
    {
        var (runnable, _) = Classify();
        var lanes = rowMask.And(runnable);
        var skipped = rowMask.AndNot(runnable);
        var changed = new Plane(Count);
        var unread = new Plane(Count);
        var unadmitted = new Plane(Count);
        var ambiguous = new Plane(Count);
        var none = Enumerable.Repeat(-1, Count).ToArray();
        var waves = Waves(lanes);
        foreach (var wave in waves)
        {
            var supports = new Plane[bindings.Length][];
            for (var r = 0; r < supports.Length; r++)
                supports[r] = Enumerable.Range(0, relation.Roles[r].Size).Select(_ => new Plane(Count)).ToArray();
            var readable = wave.Clone();
            foreach (var row in wave.Indexes())
                for (var r = 0; r < bindings.Length; r++)
                {
                    var binding = bindings[r];
                    var index = Reference(row, r);
                    if (binding.IsOpen)
                    {
                        for (var v = 0; v < supports[r].Length; v++)
                            if (!binding.Surface.Read(binding.Plane(v), index)) supports[r][v][row] = true;
                    }
                    else if (Value(row, r) is int value) supports[r][value][row] = true;
                    else
                    {
                        unread[row] = true;
                        readable[row] = false;
                    }
                }
            var next = relation.Narrow(supports, readable, SameReference(readable));
            foreach (var row in readable.Indexes())
            {
                var empty = false;
                var open = false;
                for (var r = 0; r < bindings.Length; r++)
                {
                    var binding = bindings[r];
                    var index = Reference(row, r);
                    var left = 0;
                    for (var v = 0; v < next[r].Length; v++)
                    {
                        if (next[r][v][row])
                        {
                            left++;
                            continue;
                        }
                        if (!writes[r] || !supports[r][v][row]) continue;
                        binding.Surface.Set(binding.Plane(v), index, true);
                        changed[row] = true;
                    }
                    empty |= left == 0;
                    open |= binding.IsOpen && left > 1;
                }
                if (empty) unadmitted[row] = true;
                else if (open) ambiguous[row] = true;
            }
        }
        return new WiringStep(changed, unread, unadmitted, ambiguous, skipped, new Plane(Count), none, (int[])none.Clone(), waves);
    }

    /// <summary>The indexes of a row's roles that are bound to <paramref name="surface"/>.</summary>
    internal IEnumerable<int> IndexesOn(int row, Surface surface, bool writtenOnly = false)
    {
        for (var r = 0; r < bindings.Length; r++)
            if (ReferenceEquals(bindings[r].Surface, surface) && (!writtenOnly || writes[r]))
                yield return Reference(row, r);
    }

    internal IEnumerable<(Surface, int)> TouchedKeys(int row)
    {
        var keys = new List<(Surface, int)> { (Rows, row) };
        for (var r = 0; r < bindings.Length; r++)
        {
            var index = Reference(row, r);
            if ((uint)index >= (uint)bindings[r].Surface.Count) continue;
            bindings[r].Surface.ReadKeys(index, keys);
            if (writes[r]) keys.Add(bindings[r].Surface.WriteKey(index));
        }
        return keys;
    }

    internal IEnumerable<(Surface, int)> WrittenKeys(int row)
    {
        if (evidence is not null) yield return (Rows, row);
        for (var r = 0; r < bindings.Length; r++)
        {
            if (!writes[r]) continue;
            var index = Reference(row, r);
            if ((uint)index < (uint)bindings[r].Surface.Count) yield return bindings[r].Surface.WriteKey(index);
        }
    }

    /// <summary>For scheduling: every row that is in use or could be recruited.
    /// The wiring reads whether each is in use every time it runs.</summary>
    internal IEnumerable<(Surface, int)> WatchedKeys()
    {
        for (var row = 0; row < Count; row++)
            if (!Rows.IsRetired(row)) yield return (Rows, row);
    }

    private int Reference(int row, int role)
    {
        var index = 0;
        var planes = references[role];
        for (var k = 0; k < planes.Count; k++)
            if (Rows.Read(planes[k], row)) index |= 1 << k;
        return index;
    }

    private int[] Indexes(int row)
    {
        var indexes = new int[bindings.Length];
        for (var r = 0; r < indexes.Length; r++) indexes[r] = Reference(row, r);
        return indexes;
    }

    private void Point(int row, int role, int index)
    {
        var planes = references[role];
        for (var k = 0; k < planes.Count; k++) Rows.Write(planes[k], row, ((index >> k) & 1) != 0);
    }

    /// <summary>Gives a role's reference enough planes to hold an index.</summary>
    private void Widen(int role, int index)
    {
        while (references[role].Count < PackedInts.WidthFor(index)) references[role].Add(Rows.AddPlane());
    }

    private (Plane Runnable, Plane Aliased) Classify()
    {
        var runnable = new Plane(Count);
        var aliased = new Plane(Count);
        foreach (var row in Rows.Live.Indexes())
        {
            var indexes = Indexes(row);
            var reached = true;
            for (var r = 0; r < indexes.Length && reached; r++) reached = bindings[r].Surface.IsLive(indexes[r]);
            if (!reached) continue;
            if (MeetAtAWrittenBit(indexes) is not null) aliased[row] = true;
            else runnable[row] = true;
        }
        return (runnable, aliased);
    }

    /// <summary>For each pair of open roles, the rows where both reach the same
    /// bits: one value read twice, which must agree with itself.</summary>
    private Plane?[,]? SameReference(Plane rows)
    {
        Plane?[,]? same = null;
        for (var r = 0; r < bindings.Length; r++)
            for (var j = r + 1; j < bindings.Length; j++)
            {
                if (!bindings[r].IsOpen || !bindings[j].IsOpen || bindings[r].Planes.Count != bindings[j].Planes.Count) continue;
                foreach (var row in rows.Indexes())
                {
                    if (bindings[r].Surface.Bit(Reference(row, r), bindings[r].Plane(0)) != bindings[j].Surface.Bit(Reference(row, j), bindings[j].Plane(0))) continue;
                    same ??= new Plane?[bindings.Length, bindings.Length];
                    (same[r, j] ??= new Plane(Count))[row] = true;
                }
            }
        return same;
    }

    /// <summary>The first pair of roles that would hold one bit that one of
    /// them writes, or null. A role can meet itself, when two of its bits reach
    /// one bit of the structure underneath.</summary>
    private (int Writer, int Other)? MeetAtAWrittenBit(int[] indexes)
    {
        for (var r = 0; r < indexes.Length; r++)
            for (var b = 0; b < bindings[r].Planes.Count; b++)
            {
                if (((changing[r] >> b) & 1) == 0) continue;
                var written = bindings[r].Surface.Bit(indexes[r], bindings[r].Plane(b));
                for (var j = 0; j < indexes.Length; j++)
                    for (var c = 0; c < bindings[j].Planes.Count; c++)
                        if ((j != r || c != b) && bindings[j].Surface.Bit(indexes[j], bindings[j].Plane(c)) == written) return (r, j);
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

/// <summary>The planes of a wiring's rows where each run leaves what it did,
/// one bit per row. They are plane numbers, in a fixed order.</summary>
public sealed record Evidence(int Changed, int Unread, int Unadmitted, int Ambiguous, int Skipped, int Aliased)
{
    internal const int Kinds = 6;

    internal static Evidence From(int first) => new(first, first + 1, first + 2, first + 3, first + 4, first + 5);
}

/// <summary>What one application of a wiring did, row by row.
/// <see cref="Before"/> and <see cref="After"/> hold tuple indexes (−1 where a
/// role was not a code or the row did not run, and for relations), read around
/// the row's own wave. Rows that did not change say why, each kind apart:
/// unread rows held bits that are not codes; unadmitted rows were in a state
/// the law admits nothing for, which for a relation is a contradiction;
/// ambiguous rows had several admitted possibilities and nothing chose;
/// skipped rows are retired or reach a retired position or nowhere; aliased
/// rows had roles meeting at a written bit.</summary>
public sealed record WiringStep(
    Plane Changed,
    Plane Unread,
    Plane Unadmitted,
    Plane Ambiguous,
    Plane Skipped,
    Plane Aliased,
    int[] Before,
    int[] After,
    IReadOnlyList<Plane> Waves);
