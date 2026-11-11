namespace Gojo.EncodingC;

/// <summary>A workspace: a live view of a structure, or of another rung, with a
/// layer on top that holds only what was changed here.
/// <para><b>Reference first.</b> A rung copies nothing when it is made. Every
/// position it has not written reads the source as the source is now, so a
/// rung sees the world keep moving. Writing a position copies the whole
/// position in, every plane but the constants, so a value spread over several
/// planes is never half the rung's and half the source's. The rung keeps the
/// new bits and the source bits they replaced. A rung therefore costs what it
/// changes, not what it sees, and many can be open at once. Laws run on it
/// exactly as on a structure; a law run here writes only here.</para>
/// <para><b>Selection, coarse to fine.</b> What a rung attends to is a short
/// list of layers over the source's positions, each kept or ignored, later
/// layers over earlier ones: "all of it", "but not that area", "except this
/// detail". The caller never lists positions one by one. "All" follows the
/// source as it grows.</para>
/// <para><b>Selection by use.</b> A row that touches a selected position and
/// some unselected ones brings those in when it runs, one step further from
/// what was asked for. Each position keeps how many steps are left (a unary
/// count, at most <see cref="Reach"/>). A row that would reach past that, or
/// into an area the rung ignores, does not run and is reported, so attention
/// spreads only so far from a request.</para>
/// <para><b>Leaving.</b> A rung that is dropped changed nothing. One that is
/// compared with its source after the source moves is a prediction. One can
/// be reverted in part, or committed in part or in whole. A commit writes
/// only if nothing it would overwrite has moved since it was copied, writes
/// nothing at all otherwise, and refuses to split changes one row made
/// together, so a move is never committed as half a move. Introduced
/// positions are candidates with no source; a commit adopts them.</para>
/// <para>Constants are read by reference and cannot be changed here: every
/// position of a structure shares them.</para></summary>
public sealed class Rung : Surface
{
    private readonly Surface source;
    private readonly List<Plane?> held = [];
    private readonly List<Plane?> bits = [];
    private readonly List<Plane?> basis = [];
    private readonly PackedInts beyond = new();
    private readonly List<(Plane? Area, bool Keep)> layers = [];
    private readonly List<Plane> left = [];
    private Plane used = new(0);
    private PackedInts? together;
    private int span = -1;

    /// <summary>A rung over a structure.</summary>
    public Rung(Structure source, int reach = 3)
        : this((Surface)source, reach)
    {
    }

    /// <summary>A rung over another rung: it sees that rung's changes live, and
    /// its own commits go there, not to the structure underneath.</summary>
    public Rung(Rung source, int reach = 3)
        : this((Surface)source, reach)
    {
    }

    private Rung(Surface source, int reach)
    {
        ArgumentNullException.ThrowIfNull(source);
        if (reach is < 0 or > 63) throw new ArgumentOutOfRangeException(nameof(reach));
        this.source = source;
        Reach = reach;
        for (var k = 0; k < reach; k++) left.Add(new Plane(0));
    }

    /// <summary>How many steps from a requested position use can still bring
    /// positions in.</summary>
    public int Reach { get; }

    /// <summary>Source positions, then introduced ones. Until something is
    /// introduced, this follows the source as it grows.</summary>
    public override int Count => Span + beyond.Count;

    public override int PlaneCount => source.PlaneCount;

    internal override bool ThroughPort => false;

    private int Span => span < 0 ? source.Count : span;

    /// <summary>The source position behind a rung position, or null for one
    /// introduced here.</summary>
    public int? SourceOf(int index)
    {
        CheckIndex(index);
        if (index < Span) return index;
        var mapped = beyond[index - Span];
        return mapped > 0 ? mapped - 1 : null;
    }

    public override bool IsLive(int index) =>
        (uint)index < (uint)Count && (SourceOf(index) is not int s || source.IsLive(s));

    public override bool Read(int plane, int index)
    {
        CheckPlane(plane);
        CheckIndex(index);
        if (source.IsConstant(plane)) return ConstantBit(plane);
        if (Holds(plane, index)) return Fit(bits, plane)[index];
        return SourceOf(index) is int s && source.Read(plane, s);
    }

    /// <summary>Supposes a bit here: copies it in and changes it. The source is
    /// untouched.</summary>
    public void Write(int plane, int index, bool value) => Set(plane, index, value);

    /// <summary>Adds a position with no source: a candidate, such as a proposed
    /// closing site. It is always selected, never read as evidence about the
    /// source, and adopted by a commit. Once something is introduced, source
    /// positions added later are not seen until <see cref="Follow"/>.</summary>
    public int Introduce()
    {
        if (span < 0) span = source.Count;
        beyond.Add(0);
        return Count - 1;
    }

    /// <summary>Brings source positions added since the last look into view.
    /// Returns how many.</summary>
    public int Follow()
    {
        if (span < 0) return 0;
        var seen = Enumerable.Range(Span, beyond.Count).Select(i => SourceOf(i)).OfType<int>().ToHashSet();
        var added = 0;
        for (var s = span; s < source.Count; s++)
            if (!seen.Contains(s))
            {
                beyond.Add(s + 1);
                added++;
            }
        return added;
    }

    /// <summary>Introduced positions not yet adopted.</summary>
    public Plane Introduced => Plane.Of(Count, Enumerable.Range(0, Count).Where(i => SourceOf(i) is null));

    // ---- Selection ----

    /// <summary>Attends to everything in the source, as it grows. A coarse layer.</summary>
    public Rung SelectAll()
    {
        layers.Add((null, true));
        return this;
    }

    /// <summary>Attends to an area of source positions.</summary>
    public Rung Select(Plane area)
    {
        ArgumentNullException.ThrowIfNull(area);
        layers.Add((area.Clone(), true));
        return this;
    }

    /// <summary>Ignores an area of source positions, even if an earlier layer
    /// selected it. A later layer can select within it again. Use never brings
    /// an ignored position in.</summary>
    public Rung Ignore(Plane area)
    {
        ArgumentNullException.ThrowIfNull(area);
        layers.Add((area.Clone(), false));
        return this;
    }

    /// <summary>Positions the layers ask for, and introduced ones.</summary>
    public Plane Requested => Layers().Requested;

    /// <summary>Positions the layers set aside.</summary>
    public Plane Ignored => Layers().Ignored;

    /// <summary>Positions brought in by use.</summary>
    public Plane Used => Fit(ref used).Clone();

    /// <summary>What the rung attends to: requested, plus what use brought in
    /// that no layer ignores.</summary>
    public Plane Selected
    {
        get
        {
            var (requested, ignored) = Layers();
            return requested.Or(Fit(ref used).AndNot(ignored));
        }
    }

    /// <summary>Steps of use left at a position: <see cref="Reach"/> where it
    /// was asked for, fewer where use brought it in, 0 elsewhere.</summary>
    public int ReachLeft(int index)
    {
        CheckIndex(index);
        if (Requested[index]) return Reach;
        return Left(index);
    }

    // ---- Running ----

    /// <summary>Runs a transition wiring here. Rows whose rung positions are all
    /// selected run. A row that also touches unselected positions brings them
    /// in, one step further from the request, if one of its selected positions
    /// has steps left and none of the others is ignored; otherwise it does not
    /// run and is returned in <see cref="RungStep.Beyond"/>. Rows that touch
    /// nothing selected are not this rung's business. Rows that changed several
    /// of this rung's positions are remembered as changed together; that is done
    /// by the wiring, so it holds however the wiring is run.</summary>
    public RungStep Apply(Wiring wiring)
    {
        ArgumentNullException.ThrowIfNull(wiring);
        if (wiring.Law is not TransitionLaw) throw new ArgumentException("Only transition laws change state.", nameof(wiring));
        var bound = Enumerable.Range(0, wiring.Bindings.Count).Where(r => ReferenceEquals(wiring.Bindings[r].Surface, this)).ToArray();
        if (bound.Length == 0) throw new ArgumentException("Bind at least one role to this rung.", nameof(wiring));
        for (var r = 0; r < wiring.Bindings.Count; r++)
            if (wiring.Writes(r) && !ReferenceEquals(wiring.Bindings[r].Surface, this))
                throw new ArgumentException($"Role {r} would write outside this rung. A rung writes only itself; its commit decides what reaches anything else.", nameof(wiring));

        var (requested, ignored) = Layers();
        var selected = requested.Or(Fit(ref used).AndNot(ignored));
        var run = new Plane(wiring.Count);
        var beyondRows = new Plane(wiring.Count);
        var brought = new Plane(Count);
        foreach (var row in wiring.Live.Indexes())
        {
            var mine = wiring.IndexesOn(row, this).Distinct().ToArray();
            var inside = mine.Where(i => selected[i]).ToArray();
            if (inside.Length == 0) continue;
            var outside = mine.Where(i => !selected[i]).ToArray();
            if (outside.Length == 0)
            {
                run[row] = true;
                continue;
            }
            var steps = inside.Max(i => requested[i] ? Reach : Left(i));
            if (steps == 0 || outside.Any(i => ignored[i]))
            {
                beyondRows[row] = true;
                continue;
            }
            foreach (var i in outside)
            {
                BringIn(i, steps - 1);
                selected[i] = true;
                brought[i] = true;
            }
            run[row] = true;
        }

        return new RungStep(wiring.Apply(run), beyondRows, brought);
    }

    // ---- Reading what happened here ----

    /// <summary>Positions holding a bit copied in, and introduced positions.</summary>
    public Plane Held
    {
        get
        {
            var all = Introduced;
            foreach (var plane in held)
                if (plane is not null) all = all.Or(Fit(plane));
            return all;
        }
    }

    /// <summary>Positions where this rung reads differently from its source now,
    /// and introduced positions: what a commit would change. Checked after the
    /// source has moved on, this is how far a prediction missed.</summary>
    public Plane Changes()
    {
        var changes = Introduced;
        for (var p = 0; p < held.Count; p++)
        {
            if (held[p] is not { } h) continue;
            var b = Fit(bits, p);
            foreach (var i in Fit(h).Indexes())
                if (SourceOf(i) is int s && source.Read(p, s) != b[i]) changes[i] = true;
        }
        return changes;
    }

    /// <summary>Positions with a copied bit whose source has moved since it was
    /// copied, or that the source has retired. Committing them would
    /// overwrite something newer.</summary>
    public Plane Stale()
    {
        var stale = new Plane(Count);
        for (var p = 0; p < held.Count; p++)
        {
            if (held[p] is not { } h) continue;
            var was = Fit(basis, p);
            foreach (var i in Fit(h).Indexes())
                if (SourceOf(i) is int s && (!source.IsLive(s) || source.Read(p, s) != was[i])) stale[i] = true;
        }
        return stale;
    }

    /// <summary>Positions a sibling rung over the same source reads differently
    /// from this one: two suppositions from one start, compared.</summary>
    public Plane Differs(Rung other)
    {
        ArgumentNullException.ThrowIfNull(other);
        if (!ReferenceEquals(other.source, source)) throw new ArgumentException("Compare rungs over the same source.", nameof(other));
        var differs = new Plane(Count);
        var candidates = Held;
        foreach (var i in other.Held.Indexes())
            if (i < Count) candidates[i] = true;
        foreach (var i in candidates.Indexes())
        {
            if (i >= other.Count || SourceOf(i) is not int s || other.SourceOf(i) != s) continue;
            for (var p = 0; p < PlaneCount; p++)
                if (!source.IsConstant(p) && Read(p, i) != other.Read(p, i))
                {
                    differs[i] = true;
                    break;
                }
        }
        return differs;
    }

    // ---- Leaving ----

    /// <summary>Forgets what was written at the given positions (all held
    /// positions when null), so they read the source live again. Changes made
    /// together are forgotten together: a unit moved into an introduced
    /// position goes back to where it came from, and the introduced position
    /// stays, empty, as a candidate.</summary>
    public Plane Revert(Plane? positions = null)
    {
        var set = Groups(Restrict(positions));
        foreach (var i in set.Indexes()) Release(i);
        return set;
    }

    /// <summary>Writes this rung's changes at the given positions (all held
    /// positions when null) into its source. Refused, writing nothing, when the
    /// positions would split changes one row made together, or when anything
    /// they would overwrite has moved in the source since it was copied.
    /// Introduced positions are adopted as new source positions. Committed into
    /// a parent rung, changes made together stay together there.</summary>
    public RungCommit Commit(Plane? positions = null)
    {
        var set = Restrict(positions);
        var split = Groups(set).AndNot(set);
        var none = new Plane(Count);
        if (split.Any) return new RungCommit(false, none, split, none, []);
        var stale = Stale().And(set);
        if (stale.Any) return new RungCommit(false, stale, none, none, []);

        var written = new Plane(Count);
        for (var p = 0; p < held.Count; p++)
        {
            if (held[p] is not { } h) continue;
            var b = Fit(bits, p);
            foreach (var i in Fit(h).And(set).Indexes())
                if (SourceOf(i) is int s && source.Read(p, s) != b[i])
                {
                    source.Set(p, s, b[i]);
                    written[i] = true;
                }
        }
        var adopted = new List<int>();
        foreach (var i in set.Indexes())
        {
            if (SourceOf(i) is not null) continue;
            var s = source switch
            {
                Structure structure => structure.Add(),
                Rung parent => parent.Introduce(),
                _ => throw new InvalidOperationException("Only a structure or a rung adopts positions."),
            };
            for (var p = 0; p < held.Count; p++)
                if (held[p] is not null && Fit(held, p)[i]) source.Set(p, s, Fit(bits, p)[i]);
            beyond[i - Span] = s + 1;
            adopted.Add(s);
            written[i] = true;
        }
        foreach (var group in written.Indexes().GroupBy(Find))
        {
            var there = group.Select(i => SourceOf(i)!.Value).Distinct().ToArray();
            if (there.Length > 1) source.ChangedTogether(there);
        }
        foreach (var i in set.Indexes()) Release(i);
        return new RungCommit(true, none, none, written, adopted);
    }

    // ---- Surface ----

    internal override bool IsConstant(int plane) => source.IsConstant(plane);

    internal override void Set(int plane, int index, bool value)
    {
        CheckPlane(plane);
        if (source.IsConstant(plane))
            throw new InvalidOperationException($"Plane {plane} is a constant every position of the source shares. A rung reads it but cannot change it.");
        if (!IsLive(index)) throw new ArgumentOutOfRangeException(nameof(index), $"Position {index} is not live.");
        if (!Holds(plane, index)) CopyIn(index);
        Fit(bits, plane)[index] = value;
    }

    internal override (Surface Home, int Index, int Plane) Bit(int index, int plane) => (this, index, plane);

    internal override void ReadKeys(int index, List<(Surface, int)> keys)
    {
        keys.Add((this, index));
        if (SourceOf(index) is int s) source.ReadKeys(s, keys);
    }

    internal override (Surface, int) WriteKey(int index) => (this, index);

    internal override void ChangedTogether(IReadOnlyList<int> indexes)
    {
        for (var k = 1; k < indexes.Count; k++) Join(indexes[0], indexes[k]);
    }

    // ---- Internals ----

    private bool ConstantBit(int plane) => source switch
    {
        Structure structure => structure.ConstantValue(plane),
        Rung parent => parent.ConstantBit(plane),
        _ => throw new InvalidOperationException("A rung sits on a structure or a rung."),
    };

    private bool Holds(int plane, int index) => plane < held.Count && held[plane] is { } h && index < h.Length && h[index];

    /// <summary>Copies a whole position in: every plane it has that is not a
    /// constant. A value spread over several planes is then the rung's as a
    /// whole, never half its own and half the source's.</summary>
    private void CopyIn(int index)
    {
        var s = SourceOf(index);
        for (var p = 0; p < PlaneCount; p++)
        {
            if (source.IsConstant(p) || Holds(p, index)) continue;
            Fit(held, p)[index] = true;
            var bit = s is int at && source.Read(p, at);
            Fit(basis, p)[index] = bit;
            Fit(bits, p)[index] = bit;
        }
    }

    private (Plane Requested, Plane Ignored) Layers()
    {
        var requested = new Plane(Count);
        var ignored = new Plane(Count);
        foreach (var (area, keep) in layers)
        {
            var mask = ToRung(area);
            if (keep)
            {
                requested = requested.Or(mask);
                ignored = ignored.AndNot(mask);
            }
            else
            {
                requested = requested.AndNot(mask);
                ignored = ignored.Or(mask);
            }
        }
        return (requested.Or(Introduced), ignored.AndNot(Introduced));
    }

    /// <summary>An area over source positions, as a plane over rung positions.
    /// No area means every live source position.</summary>
    private Plane ToRung(Plane? area)
    {
        var mask = new Plane(Count);
        for (var i = 0; i < Count; i++)
            if (SourceOf(i) is int s && (area is null ? source.IsLive(s) : s < area.Length && area[s])) mask[i] = true;
        return mask;
    }

    private int Left(int index)
    {
        var steps = 0;
        while (steps < left.Count && index < left[steps].Length && left[steps][index]) steps++;
        return steps;
    }

    private void BringIn(int index, int steps)
    {
        Fit(ref used)[index] = true;
        for (var k = 0; k < steps; k++)
        {
            if (left[k].Length < Count) left[k].Grow(Count);
            left[k][index] = true;
        }
    }

    private Plane Restrict(Plane? positions)
    {
        var heldNow = Held;
        if (positions is null) return heldNow;
        return Plane.Of(Count, positions.Indexes().Where(i => i < Count)).And(heldNow);
    }

    /// <summary>The positions, and every position changed together with one of them.</summary>
    private Plane Groups(Plane set)
    {
        var result = set.Clone();
        if (together is null) return result;
        var roots = set.Indexes().Select(Find).ToHashSet();
        foreach (var i in Held.Indexes())
            if (roots.Contains(Find(i))) result[i] = true;
        return result;
    }

    private void Release(int index)
    {
        foreach (var plane in held)
            if (plane is not null && index < plane.Length) plane[index] = false;
        if (together is not null && index < together.Count) together[index] = 0;
    }

    private int Find(int index)
    {
        if (together is null) return index;
        while (index < together.Count && together[index] > 0 && together[index] - 1 != index) index = together[index] - 1;
        return index;
    }

    private void Join(int a, int b)
    {
        together ??= new PackedInts();
        while (together.Count < Count) together.Add(0);
        var (ra, rb) = (Find(a), Find(b));
        if (ra != rb) together[rb] = ra + 1;
    }

    private Plane Fit(List<Plane?> planes, int plane)
    {
        while (planes.Count <= plane) planes.Add(null);
        var p = planes[plane] ??= new Plane(Count);
        if (p.Length < Count) p.Grow(Count);
        return p;
    }

    private Plane Fit(Plane plane)
    {
        if (plane.Length < Count) plane.Grow(Count);
        return plane;
    }

    private Plane Fit(ref Plane plane)
    {
        if (plane.Length < Count) plane.Grow(Count);
        return plane;
    }

    private void CheckIndex(int index)
    {
        if ((uint)index >= (uint)Count) throw new ArgumentOutOfRangeException(nameof(index));
    }

    private void CheckPlane(int plane)
    {
        if ((uint)plane >= (uint)PlaneCount) throw new ArgumentOutOfRangeException(nameof(plane));
    }
}

/// <summary>What running a wiring on a rung did. <see cref="Beyond"/> marks rows
/// that would have reached past the rung's reach or into an ignored area;
/// <see cref="Brought"/> marks the positions use brought in.</summary>
public sealed record RungStep(WiringStep Step, Plane Beyond, Plane Brought);

/// <summary>A commit's result. When refused, nothing was written:
/// <see cref="Stale"/> marks positions whose source moved since they were
/// copied, and <see cref="Split"/> marks positions changed together with the
/// ones asked for but left out. <see cref="Written"/> marks rung positions
/// whose changes reached the source; <see cref="Adopted"/> lists the source
/// positions made for introduced ones.</summary>
public sealed record RungCommit(bool Committed, Plane Stale, Plane Split, Plane Written, IReadOnlyList<int> Adopted);
