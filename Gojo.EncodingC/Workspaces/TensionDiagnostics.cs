namespace Gojo.EncodingC;

/// <summary>Host-side inspection and admissibility reads over a local
/// structure's tension planes and evidence. Nothing inside a running machine
/// calls these methods: they sit strictly outside the encoding, alongside
/// <see cref="Workspace"/> and <see cref="Closure"/>, to measure tiers, detect
/// duplicate rows or co-moving positions, and evaluate strict vs. Ashby
/// viability-threshold admissibility on structural trials.</summary>
public static class TensionDiagnostics
{
    /// <summary>Pairs of live rows within the same wiring that bind identical
    /// underlying positions across every role: structural redundancy.</summary>
    public static IReadOnlyList<DuplicateRow> Duplicates(Engine engine)
    {
        ArgumentNullException.ThrowIfNull(engine);
        var duplicates = new List<DuplicateRow>();
        foreach (var wiring in engine.Wirings.Distinct())
        {
            var live = wiring.Live.Indexes().ToArray();
            var resolved = new (Surface Home, int Index)[live.Length][];
            for (var i = 0; i < live.Length; i++)
            {
                var row = wiring.Row(live[i]);
                resolved[i] = new (Surface, int)[row.Length];
                for (var r = 0; r < row.Length; r++)
                    resolved[i][r] = Identity(wiring.Bindings[r].Surface, row[r]);
            }
            for (var i = 0; i < live.Length; i++)
                for (var j = i + 1; j < live.Length; j++)
                    if (SameTuple(resolved[i], resolved[j]))
                        duplicates.Add(new DuplicateRow(wiring, live[i], live[j]));
        }
        return duplicates;
    }

    /// <summary>Captures every nonconstant plane of <paramref name="store"/>
    /// at the current moment, for tracking co-moving positions over a window.</summary>
    public static Plane[] Snapshot(Store store)
    {
        ArgumentNullException.ThrowIfNull(store);
        var planes = new List<Plane>();
        for (var p = 0; p < store.PlaneCount; p++)
            if (!store.IsConstantPlane(p)) planes.Add(store.Read(p));
        return [.. planes];
    }

    /// <summary>Pairs of distinct live positions in <paramref name="store"/>
    /// whose nonconstant bits never differed across a recorded window of
    /// <paramref name="snapshots"/> (at least two snapshots required):
    /// co-moving redundancy cue.</summary>
    public static IReadOnlyList<(int First, int Second)> Joined(Store store, IReadOnlyList<Plane[]> snapshots)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(snapshots);
        if (snapshots.Count < 2) return [];
        var live = store.Live.Indexes().ToArray();
        var joined = new List<(int, int)>();
        for (var a = 0; a < live.Length; a++)
            for (var b = a + 1; b < live.Length; b++)
            {
                var (i, j) = (live[a], live[b]);
                var same = true;
                for (var s = 0; s < snapshots.Count && same; s++)
                    foreach (var plane in snapshots[s])
                        if (i < plane.Length && j < plane.Length && plane[i] != plane[j])
                        {
                            same = false;
                            break;
                        }
                if (same) joined.Add((i, j));
            }
        return joined;
    }

    /// <summary>Reads all tension tiers of <paramref name="engine"/> from its
    /// planes and evidence without storing hidden state outside the encoding.</summary>
    public static TensionProfile Measure(
        Engine engine,
        IReadOnlyList<TensionSite>? sites = null,
        IReadOnlyList<TensionDemand>? demands = null,
        IReadOnlyList<TensionGoal>? goals = null,
        IReadOnlyList<StreakWatch>? persistent = null,
        IReadOnlyList<StreakWatch>? idle = null,
        IReadOnlyList<(Store Store, IReadOnlyList<Plane[]> History)>? histories = null)
    {
        ArgumentNullException.ThrowIfNull(engine);
        var hard = 0;
        var obligation = 0;
        var materialPlus = 0;
        var materialMinus = 0;
        var goal = 0;
        var contention = 0;
        var structuralDeficit = 0;
        var structuralExcess = 0;

        foreach (var wiring in engine.Wirings.Distinct())
        {
            if (wiring.Recorded is { } rec)
            {
                var live = wiring.Live;
                hard += wiring.Rows.Read(rec.Aliased).And(live).Count;
                hard += wiring.Rows.Read(rec.Unread).And(live).Count;
                obligation += wiring.Rows.Read(rec.Unadmitted).And(live).Count;
                contention += wiring.Rows.Read(rec.Contested).And(live).Count;
            }
            else
            {
                hard += wiring.Aliased().Count;
                if (wiring.Law is RelationLaw relation)
                {
                    var (unreadRows, unadmittedRows) = ReadRelation(wiring, relation);
                    hard += unreadRows;
                    obligation += unadmittedRows;
                }
            }
        }

        if (sites is not null)
            foreach (var site in sites)
            {
                if (site.Value is { } v) hard += Unary.Broken(site.Surface, v).Count;
                if (site.Plus is { } plus)
                {
                    hard += Unary.Broken(site.Surface, plus).Count;
                    materialPlus += SumUnary(site.Surface, plus);
                }
                if (site.Minus is { } minus)
                {
                    hard += Unary.Broken(site.Surface, minus).Count;
                    materialMinus += SumUnary(site.Surface, minus);
                }
            }

        if (demands is not null)
            foreach (var item in demands)
            {
                obligation += item.Supports.Empty.And(item.Demand).Count;
                goal += item.Supports.Open.And(item.Demand).Count;
            }

        if (goals is not null)
            foreach (var g in goals)
            {
                if (g.Deficit is { } d)
                {
                    hard += Unary.Broken(g.Surface, d).Count;
                    goal += SumUnary(g.Surface, d);
                }
                if (g.Excess is { } e)
                {
                    hard += Unary.Broken(g.Surface, e).Count;
                    goal += SumUnary(g.Surface, e);
                }
            }

        if (persistent is not null)
            foreach (var watch in persistent)
                structuralDeficit += CountAtLeast(watch.Surface, watch.Streak, watch.Threshold);

        if (idle is not null)
            foreach (var watch in idle)
                structuralExcess += CountAtLeast(watch.Surface, watch.Streak, watch.Threshold);

        structuralExcess += Duplicates(engine).Count;
        if (histories is not null)
            foreach (var (store, history) in histories)
                structuralExcess += Joined(store, history).Count;

        var descriptionBits = Savable(engine) ? Transport.Measure(engine).Bits : ActiveBits(engine);
        return new TensionProfile(hard, obligation, materialPlus, materialMinus, goal, contention, structuralDeficit, structuralExcess, descriptionBits);
    }

    /// <summary>Strict lexicographic admissibility (Optimality Theory style,
    /// GojoTension §5): <c>Hard</c> must be 0, no higher tier may rise, and at
    /// least one tier drops unless <paramref name="allowNeutral"/> is true.</summary>
    public static Admissibility AdmissibleStrict(TensionProfile before, TensionProfile after, bool allowNeutral = false)
    {
        if (after.Hard > 0) return new Admissibility(false, false, 0);
        long[] b = [before.Hard, before.Obligation, before.MaterialTotal, before.Goal, before.Contention, before.StructuralDeficit, before.StructuralExcess, before.DescriptionBits];
        long[] a = [after.Hard, after.Obligation, after.MaterialTotal, after.Goal, after.Contention, after.StructuralDeficit, after.StructuralExcess, after.DescriptionBits];
        for (var i = 0; i < b.Length; i++)
        {
            if (a[i] < b[i]) return new Admissibility(true, true, 0);
            if (a[i] > b[i]) return new Admissibility(false, false, 0);
        }
        return new Admissibility(allowNeutral, false, 0);
    }

    /// <summary>Viability-threshold admissibility (Ashby essential variables):
    /// <c>Hard == 0</c> remains strictly dominant. Below <c>Hard</c>, each tier
    /// has an explicit viability bound; a change inside bounds (such as lossy
    /// crystallization that trims redundancy while staying within coarse
    /// tolerance) is admissible when total tension or description size drops.
    /// Any rise above a viability bound requires an explicit
    /// <paramref name="loanBudget"/>, recorded in <see cref="Admissibility.LoanUsed"/>.</summary>
    public static Admissibility AdmissibleViable(
        TensionProfile before,
        TensionProfile after,
        TensionBounds bounds,
        int loanBudget = 0,
        bool allowNeutral = false)
    {
        if (loanBudget < 0) throw new ArgumentOutOfRangeException(nameof(loanBudget));
        if (after.Hard > 0) return new Admissibility(false, false, 0);

        (int Before, int After, int Bound)[] tiers =
        [
            (before.Obligation, after.Obligation, bounds.Obligation),
            (before.MaterialPlus, after.MaterialPlus, bounds.MaterialPlus),
            (before.MaterialMinus, after.MaterialMinus, bounds.MaterialMinus),
            (before.Goal, after.Goal, bounds.Goal),
            (before.Contention, after.Contention, bounds.Contention),
            (before.StructuralDeficit, after.StructuralDeficit, bounds.StructuralDeficit),
            (before.StructuralExcess, after.StructuralExcess, bounds.StructuralExcess),
        ];

        var beforeOver = 0;
        var afterOver = 0;
        var loanUsed = 0;
        foreach (var (b, a, limit) in tiers)
        {
            var ob = b > limit ? b - limit : 0;
            var oa = a > limit ? a - limit : 0;
            beforeOver += ob;
            afterOver += oa;
            if (oa > ob) loanUsed += oa - ob;
        }
        if (loanUsed > loanBudget) return new Admissibility(false, false, loanUsed);

        if (afterOver < beforeOver) return new Admissibility(true, true, loanUsed);
        if (beforeOver > 0 && afterOver == beforeOver && loanUsed == 0)
        {
            var lowered = StrictlyBetterWithin(before, after);
            return new Admissibility(lowered || allowNeutral, lowered, 0);
        }
        if (beforeOver == 0 && afterOver == 0)
        {
            var lowered = StrictlyBetterWithin(before, after);
            return new Admissibility(lowered || allowNeutral, lowered, 0);
        }
        return new Admissibility(loanUsed > 0, false, loanUsed);
    }

    /// <summary>Live active bits of <paramref name="engine"/>: live positions
    /// times nonconstant planes across its owned stores plus distinct law bits.
    /// Used by <see cref="Measure"/> on forks (where <see cref="Transport.Measure"/>
    /// cannot serialize workspace bindings) so trials compare on one scale.</summary>
    public static long ActiveBits(Engine engine)
    {
        ArgumentNullException.ThrowIfNull(engine);
        long bits = 0;
        foreach (var store in engine.Stores)
        {
            var nonConstant = 0;
            for (var p = 0; p < store.PlaneCount; p++)
                if (!store.IsConstantPlane(p)) nonConstant++;
            bits += (long)store.Live.Count * nonConstant;
        }
        foreach (var law in engine.Wirings.Select(w => w.Law).Distinct()) bits += law.BitCount;
        return bits;
    }

    private static bool StrictlyBetterWithin(TensionProfile before, TensionProfile after) =>
        after.Obligation < before.Obligation ||
        after.MaterialPlus < before.MaterialPlus ||
        after.MaterialMinus < before.MaterialMinus ||
        after.Goal < before.Goal ||
        after.Contention < before.Contention ||
        after.StructuralDeficit < before.StructuralDeficit ||
        after.StructuralExcess < before.StructuralExcess ||
        after.DescriptionBits < before.DescriptionBits;

    private static (int Unread, int Unadmitted) ReadRelation(Wiring wiring, RelationLaw relation)
    {
        var runnable = wiring.Runnable();
        if (runnable.None) return (0, 0);
        var bindings = wiring.Bindings;
        var supports = new Plane[bindings.Count][];
        for (var r = 0; r < supports.Length; r++)
            supports[r] = Enumerable.Range(0, relation.Roles[r].Size).Select(_ => new Plane(wiring.Count)).ToArray();
        var readable = runnable.Clone();
        var unread = 0;
        foreach (var row in runnable.Indexes())
        {
            var indexes = wiring.Row(row);
            for (var r = 0; r < bindings.Count; r++)
            {
                var binding = bindings[r];
                var index = indexes[r];
                if (binding.IsOpen)
                {
                    for (var v = 0; v < supports[r].Length; v++)
                        if (binding.Surface.Read(binding.Plane(v), index)) supports[r][v][row] = true;
                }
                else if (wiring.Value(row, r) is int value) supports[r][value][row] = true;
                else
                {
                    unread++;
                    readable[row] = false;
                    break;
                }
            }
        }
        if (readable.None) return (unread, 0);
        Plane?[,]? same = null;
        for (var r = 0; r < bindings.Count; r++)
            for (var j = r + 1; j < bindings.Count; j++)
            {
                if (!bindings[r].IsOpen || !bindings[j].IsOpen || bindings[r].Planes.Count != bindings[j].Planes.Count) continue;
                foreach (var row in readable.Indexes())
                {
                    var indexes = wiring.Row(row);
                    var equal = true;
                    for (var p = 0; p < bindings[r].Planes.Count && equal; p++)
                        equal = bindings[r].Surface.Bit(indexes[r], bindings[r].Plane(p)) == bindings[j].Surface.Bit(indexes[j], bindings[j].Plane(p));
                    if (!equal) continue;
                    same ??= new Plane?[bindings.Count, bindings.Count];
                    (same[r, j] ??= new Plane(wiring.Count))[row] = true;
                }
            }
        var next = relation.Narrow(supports, readable, same);
        var unadmitted = 0;
        foreach (var row in readable.Indexes())
        {
            var empty = false;
            for (var r = 0; r < bindings.Count && !empty; r++)
            {
                var any = false;
                for (var v = 0; v < next[r].Length && !any; v++) any = next[r][v][row];
                if (!any) empty = true;
            }
            if (empty) unadmitted++;
        }
        return (unread, unadmitted);
    }

    private static int SumUnary(Surface surface, Field field)
    {
        var total = 0;
        for (var pos = 0; pos < surface.Count; pos++)
            if (surface.IsLive(pos) && field.Read(surface, pos) is int count) total += count;
        return total;
    }

    private static int CountAtLeast(Surface surface, Field streak, int threshold)
    {
        var count = 0;
        for (var pos = 0; pos < surface.Count; pos++)
            if (surface.IsLive(pos) && streak.Read(surface, pos) is int value && value >= threshold) count++;
        return count;
    }

    private static bool Savable(Engine engine) =>
        engine.Parent is null &&
        engine.Wirings.All(w => w.Bindings.All(b => b.Surface.Underlying is not Workspace and not Gap));

    private static (Surface Home, int Index) Identity(Surface surface, int index)
    {
        var resolved = surface.Underlying;
        return resolved switch
        {
            Port port when port.IsLive(index) => (port.Owner, port.Lane(index)),
            Socket { Plugged: { } port } when port.IsLive(index) => (port.Owner, port.Lane(index)),
            _ => (resolved, index),
        };
    }

    private static bool SameTuple((Surface Home, int Index)[] a, (Surface Home, int Index)[] b)
    {
        if (a.Length != b.Length) return false;
        for (var i = 0; i < a.Length; i++)
            if (!ReferenceEquals(a[i].Home, b[i].Home) || a[i].Index != b[i].Index) return false;
        return true;
    }
}

/// <summary>Host descriptor for a surface (store or fork workspace) holding
/// material value and/or tension fields (<c>T+</c>, <c>T−</c>).</summary>
public sealed record TensionSite(Surface Surface, Field? Value = null, Field? Plus = null, Field? Minus = null);

/// <summary>Host descriptor pairing an open value's supports with the demand
/// plane that requires them to settle.</summary>
public sealed record TensionDemand(Supports Supports, Plane Demand);

/// <summary>Host descriptor for a two-faced goal mismatch (unary deficit and
/// excess fields) on a surface.</summary>
public sealed record TensionGoal(Surface Surface, Field? Deficit = null, Field? Excess = null);

/// <summary>Host descriptor for a slow-clock unary streak counter (persistent
/// tension or idle row count) and the threshold at which it fires.</summary>
public sealed record StreakWatch(Surface Surface, Field Streak, int Threshold);

/// <summary>Two live rows of one wiring binding identical underlying positions.</summary>
public sealed record DuplicateRow(Wiring Wiring, int FirstRow, int DuplicateRowIndex);

/// <summary>A host-read snapshot of tension across every tier of a local
/// structure. <see cref="MaterialPlus"/> and <see cref="MaterialMinus"/> are
/// kept apart and never netted.</summary>
public sealed record TensionProfile(
    int Hard,
    int Obligation,
    int MaterialPlus,
    int MaterialMinus,
    int Goal,
    int Contention,
    int StructuralDeficit,
    int StructuralExcess,
    long DescriptionBits)
{
    public int MaterialTotal => MaterialPlus + MaterialMinus;

    public bool IsQuiet =>
        Hard == 0 && Obligation == 0 && MaterialPlus == 0 && MaterialMinus == 0 &&
        Goal == 0 && Contention == 0 && StructuralDeficit == 0 && StructuralExcess == 0;
}

/// <summary>Ashby viability bounds per tension tier. Values at or below their
/// bound stay inside tolerance; crossing above escalates the tier.</summary>
public sealed record TensionBounds(
    int Obligation = 0,
    int MaterialPlus = 0,
    int MaterialMinus = 0,
    int Goal = 0,
    int Contention = 0,
    int StructuralDeficit = 0,
    int StructuralExcess = 0);

/// <summary>Result of checking whether a step or structural trial is admissible.</summary>
public sealed record Admissibility(bool Admitted, bool StrictlyLowered, int LoanUsed);
