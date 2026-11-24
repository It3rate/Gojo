namespace Gojo.EncodingC;

/// <summary>Pure structural builders for material, information, threshold and
/// accumulated-quantity tension in <see cref="EncodingC"/>. Everything produced
/// here is ordinary store planes, <see cref="Flipped"/> views and checked
/// <see cref="Law"/> tables; once wired, this class does nothing at runtime and
/// the machine saves and runs identically through <see cref="Transport"/>.
/// <para>Host-side inspection and admissibility reads live separately in
/// <see cref="TensionDiagnostics"/>.</para></summary>
public sealed class Tension
{
    /// <summary>Adds unary pressure (<c>T+</c>) and draw (<c>T−</c>) fields of
    /// <paramref name="capacity"/> units beside <paramref name="value"/> in
    /// <paramref name="store"/>.</summary>
    public Tension(Store store, Field value, int capacity)
        : this(
            store,
            value,
            new Field(Shape.Unary(capacity), [.. Enumerable.Range(0, capacity).Select(_ => store.AddPlane())]),
            new Field(Shape.Unary(capacity), [.. Enumerable.Range(0, capacity).Select(_ => store.AddPlane())]))
    {
    }

    /// <summary>Binds a tension site view over existing unary fields on
    /// <paramref name="store"/>.</summary>
    public Tension(Store store, Field value, Field plus, Field minus)
    {
        ArgumentNullException.ThrowIfNull(store);
        CheckUnary(value, nameof(value));
        CheckUnary(plus, nameof(plus));
        CheckUnary(minus, nameof(minus));
        Store = store;
        Value = value;
        Plus = plus;
        Minus = minus;
    }

    public Store Store { get; }

    /// <summary>Held value at each position.</summary>
    public Field Value { get; }

    /// <summary>Positive material tension (<c>T+</c>): arrived units waiting for room.</summary>
    public Field Plus { get; }

    /// <summary>Negative material tension (<c>T−</c>): unmet draws waiting for supply.</summary>
    public Field Minus { get; }

    /// <summary>Strict Axiom 5 inflow: one unit arrives at each given position
    /// as pressure (<c>T+</c>), never written directly into <see cref="Value"/>.
    /// Blocked units exceed <c>T+</c> capacity and are returned unchanged.</summary>
    public UnaryStep Receive(Plane positions) => Unary.Add(Store, Plus, positions);

    /// <summary>Strict Axiom 5 draw: one unit of demand arrives at each given
    /// position as debt (<c>T−</c>), never bypassing tension. Blocked units
    /// exceed <c>T−</c> capacity and are returned unchanged.</summary>
    public UnaryStep Demand(Plane positions) => Unary.Add(Store, Minus, positions);

    /// <summary>Moves <c>T+</c> into <see cref="Value"/> at
    /// <paramref name="positions"/>, optionally gated.</summary>
    public Wiring Absorb(Plane positions, params Binding[] gates) =>
        Route(Plus.On(Store), Value.On(Store), positions, gates);

    /// <summary>Pays <c>T−</c> from <see cref="Value"/> via
    /// <see cref="Field.Room"/> at <paramref name="positions"/>, optionally gated.</summary>
    public Wiring Draw(Plane positions, params Binding[] gates) =>
        Route(Minus.On(Store), Value.Room(Store), positions, gates);

    /// <summary>Explicitly cancels matching units of <c>T+</c> and <c>T−</c> at
    /// <paramref name="positions"/>. Wired only where both faces represent the
    /// same quantity; nothing nets automatically.</summary>
    public Wiring Cancel(Plane positions, params Binding[] gates) =>
        Route(Plus.On(Store), Minus.Room(Store), positions, gates);

    /// <summary>Fulfils one unit of <c>T−</c> by transferring one unit from
    /// <see cref="Value"/> to <paramref name="outlet"/> while clearing
    /// <c>T−</c> in the same atomic transition (<see cref="Flows.Fulfill(int, int, int)"/>),
    /// conserving both <c>value + outlet</c> and <c>minus + outlet</c>.</summary>
    public Wiring Deliver(Binding outlet, int outletCapacity, Plane positions)
    {
        ArgumentNullException.ThrowIfNull(outlet);
        ArgumentNullException.ThrowIfNull(positions);
        var wiring = new Wiring(
            Flows.Fulfill(Value.Shape.Bits, Minus.Shape.Bits, outletCapacity),
            Value.On(Store),
            Minus.On(Store),
            outlet);
        foreach (var p in positions.Indexes()) wiring.Add(p, p, p);
        return wiring;
    }

    // ---- Static material tension wirings (Flows.Move + zero-copy Field.Room) ----

    /// <summary>Absorbs positive material tension (<c>T+</c>) into
    /// <paramref name="value"/> using <see cref="Flows.Move"/>. Conserves
    /// <c>value + T+</c>. Stops when <paramref name="value"/> is at capacity.</summary>
    public static Wiring Absorb(Surface site, Field plus, Field value)
    {
        CheckUnary(plus, nameof(plus));
        CheckUnary(value, nameof(value));
        return new Wiring(Flows.Move(plus.Shape.Bits, value.Shape.Bits), plus.On(site), value.On(site));
    }

    /// <summary>Pays negative material tension (<c>T−</c>) from
    /// <paramref name="value"/> by moving units from <c>T−</c> into
    /// <see cref="Field.Room"/> of <paramref name="value"/>. Conserves
    /// <c>value − T−</c>. Stops when <paramref name="value"/> reaches zero.</summary>
    public static Wiring Draw(Surface site, Field minus, Field value)
    {
        CheckUnary(minus, nameof(minus));
        CheckUnary(value, nameof(value));
        return new Wiring(Flows.Move(minus.Shape.Bits, value.Shape.Bits), minus.On(site), value.Room(site));
    }

    /// <summary>Explicitly cancels matching units of <c>T+</c> and <c>T−</c>
    /// by moving <c>T+</c> into <see cref="Field.Room"/> of <c>T−</c>, lowering
    /// both by one and conserving <c>T+ − T−</c>. Wired only where the
    /// structure declares both faces are the same quantity.</summary>
    public static Wiring Cancel(Surface site, Field plus, Field minus)
    {
        CheckUnary(plus, nameof(plus));
        CheckUnary(minus, nameof(minus));
        return new Wiring(Flows.Move(plus.Shape.Bits, minus.Shape.Bits), plus.On(site), minus.Room(site));
    }

    /// <summary>A 1:1 move route from <paramref name="from"/> to
    /// <paramref name="to"/> across <paramref name="positions"/>, optionally
    /// waiting on one or more gate bits (<see cref="Flows.When"/>).</summary>
    public static Wiring Route(Binding from, Binding to, Plane positions, params Binding[] gates) =>
        Route(Flows.Move(from.Planes.Count, to.Planes.Count), from, to, positions, gates);

    /// <summary>A two-role transfer law (such as <see cref="Flows.Convert"/>)
    /// from <paramref name="from"/> to <paramref name="to"/> across
    /// <paramref name="positions"/>, optionally waiting on one or more gate
    /// bits (<see cref="Flows.When"/>).</summary>
    public static Wiring Route(TransitionLaw move, Binding from, Binding to, Plane positions, params Binding[] gates)
    {
        ArgumentNullException.ThrowIfNull(move);
        ArgumentNullException.ThrowIfNull(positions);
        ArgumentNullException.ThrowIfNull(gates);
        var wiring = gates.Length == 0
            ? new Wiring(move, from, to)
            : new Wiring(Flows.When(move, gates.Length), [from, to, .. gates]);
        foreach (var p in positions.Indexes()) wiring.Add([.. Enumerable.Repeat(p, 2 + gates.Length)]);
        return wiring;
    }

    // ---- Pure bit-table observer & accumulation wirings ----

    /// <summary>Wires a single-writer combinational fold tree that summarizes
    /// <paramref name="supports"/> of any width (up to 64 values) into
    /// <c>(seen1, seen2)</c> and writes demanded information tension into
    /// <c>(deficit, excess)</c> on <paramref name="statusStore"/>. Because each
    /// plane has a single writer and nothing is cleared and re-set per step,
    /// steady states stay quiet in <see cref="Engine.Settle"/> (even when
    /// tension remains nonzero) and never self-contest under
    /// <see cref="Engine.StepTogether"/>.</summary>
    public static IReadOnlyList<Wiring> ObserveSupports(
        Engine engine,
        Supports supports,
        Surface demandSurface,
        int demandPlane,
        Store statusStore,
        int seen1,
        int seen2,
        int deficit,
        int excess,
        params int[] positions)
    {
        ArgumentNullException.ThrowIfNull(engine);
        ArgumentNullException.ThrowIfNull(supports);
        ArgumentNullException.ThrowIfNull(demandSurface);
        ArgumentNullException.ThrowIfNull(statusStore);
        var targets = positions.Length > 0 ? positions : Enumerable.Range(0, supports.Surface.Count).ToArray();
        var planes = supports.Planes;
        var added = new List<Wiring>();

        if (planes.Count == 1)
        {
            var leaf = new Wiring(TensionLaws.SupportLeaf, new Binding(supports.Surface, planes[0]), new Binding(statusStore, seen1), new Binding(statusStore, seen2));
            foreach (var p in targets) leaf.Add(p, p, p);
            engine.Add(leaf);
            added.Add(leaf);
        }
        else if (planes.Count == 2)
        {
            var pair = new Wiring(TensionLaws.SupportPair, new Binding(supports.Surface, planes[0]), new Binding(supports.Surface, planes[1]), new Binding(statusStore, seen1), new Binding(statusStore, seen2));
            foreach (var p in targets) pair.Add(p, p, p, p);
            engine.Add(pair);
            added.Add(pair);
        }
        else
        {
            var level = new List<(int S1, int S2)>();
            for (var i = 0; i < planes.Count; i += 2)
            {
                var o1 = statusStore.AddPlane();
                var o2 = statusStore.AddPlane();
                Wiring w;
                if (i + 1 < planes.Count)
                {
                    w = new Wiring(TensionLaws.SupportPair, new Binding(supports.Surface, planes[i]), new Binding(supports.Surface, planes[i + 1]), new Binding(statusStore, o1), new Binding(statusStore, o2));
                    foreach (var p in targets) w.Add(p, p, p, p);
                }
                else
                {
                    w = new Wiring(TensionLaws.SupportLeaf, new Binding(supports.Surface, planes[i]), new Binding(statusStore, o1), new Binding(statusStore, o2));
                    foreach (var p in targets) w.Add(p, p, p);
                }
                engine.Add(w);
                added.Add(w);
                level.Add((o1, o2));
            }
            while (level.Count > 1)
            {
                var next = new List<(int S1, int S2)>();
                for (var i = 0; i < level.Count; i += 2)
                {
                    if (i + 1 >= level.Count)
                    {
                        next.Add(level[i]);
                        continue;
                    }
                    var isRoot = level.Count == 2;
                    var o1 = isRoot ? seen1 : statusStore.AddPlane();
                    var o2 = isRoot ? seen2 : statusStore.AddPlane();
                    var merge = new Wiring(
                        TensionLaws.SupportCombine,
                        new Binding(statusStore, level[i].S1),
                        new Binding(statusStore, level[i].S2),
                        new Binding(statusStore, level[i + 1].S1),
                        new Binding(statusStore, level[i + 1].S2),
                        new Binding(statusStore, o1),
                        new Binding(statusStore, o2));
                    foreach (var p in targets) merge.Add(p, p, p, p, p, p);
                    engine.Add(merge);
                    added.Add(merge);
                    next.Add((o1, o2));
                }
                level = next;
            }
        }

        var status = new Wiring(
            TensionLaws.DemandedStatus,
            new Binding(demandSurface, demandPlane),
            new Binding(statusStore, seen1),
            new Binding(statusStore, seen2),
            new Binding(statusStore, deficit),
            new Binding(statusStore, excess));
        foreach (var p in targets) status.Add(p, p, p, p, p);
        engine.Add(status);
        added.Add(status);
        return added;
    }

    /// <summary>Wires a single-writer combinational fold tree comparing two
    /// unary fields (<paramref name="have"/> and <paramref name="want"/>) of
    /// any capacity up to 63 into <c>(needMore, haveExtra)</c> on
    /// <paramref name="outStore"/>. Stays quiet in <see cref="Engine.Settle"/>
    /// even when a mismatch persists, and never self-contests under
    /// <see cref="Engine.StepTogether"/>.</summary>
    public static IReadOnlyList<Wiring> ObserveMismatch(
        Engine engine,
        Surface haveSurface,
        Field have,
        Surface wantSurface,
        Field want,
        Store outStore,
        int needMore,
        int haveExtra,
        params int[] positions)
    {
        ArgumentNullException.ThrowIfNull(engine);
        ArgumentNullException.ThrowIfNull(haveSurface);
        ArgumentNullException.ThrowIfNull(wantSurface);
        ArgumentNullException.ThrowIfNull(outStore);
        CheckUnary(have, nameof(have));
        CheckUnary(want, nameof(want));
        if (have.Shape.Bits != want.Shape.Bits)
            throw new ArgumentException("Both unary fields must have the same capacity.", nameof(want));
        var targets = positions.Length > 0 ? positions : Enumerable.Range(0, haveSurface.Count).ToArray();
        var added = new List<Wiring>();

        if (have.Planes.Count == 1)
        {
            var leaf = new Wiring(
                TensionLaws.MismatchPair,
                new Binding(haveSurface, have.Planes[0]),
                new Binding(wantSurface, want.Planes[0]),
                new Binding(outStore, needMore),
                new Binding(outStore, haveExtra));
            foreach (var p in targets) leaf.Add(p, p, p, p);
            engine.Add(leaf);
            added.Add(leaf);
            return added;
        }

        var level = new List<(int Need, int Extra)>();
        for (var k = 0; k < have.Planes.Count; k++)
        {
            var n = outStore.AddPlane();
            var e = outStore.AddPlane();
            var leaf = new Wiring(
                TensionLaws.MismatchPair,
                new Binding(haveSurface, have.Planes[k]),
                new Binding(wantSurface, want.Planes[k]),
                new Binding(outStore, n),
                new Binding(outStore, e));
            foreach (var p in targets) leaf.Add(p, p, p, p);
            engine.Add(leaf);
            added.Add(leaf);
            level.Add((n, e));
        }
        while (level.Count > 1)
        {
            var next = new List<(int Need, int Extra)>();
            for (var i = 0; i < level.Count; i += 2)
            {
                if (i + 1 >= level.Count)
                {
                    next.Add(level[i]);
                    continue;
                }
                var isRoot = level.Count == 2;
                var n = isRoot ? needMore : outStore.AddPlane();
                var e = isRoot ? haveExtra : outStore.AddPlane();
                var merge = new Wiring(
                    TensionLaws.MismatchCombine,
                    new Binding(outStore, level[i].Need),
                    new Binding(outStore, level[i].Extra),
                    new Binding(outStore, level[i + 1].Need),
                    new Binding(outStore, level[i + 1].Extra),
                    new Binding(outStore, n),
                    new Binding(outStore, e));
                foreach (var p in targets) merge.Add(p, p, p, p, p, p);
                engine.Add(merge);
                added.Add(merge);
                next.Add((n, e));
            }
            level = next;
        }
        return added;
    }

    /// <summary>Data-driven threshold read in planes: compares a unary
    /// <paramref name="magnitude"/> against a unary <paramref name="threshold"/>
    /// stored as ordinary planes (which laws can read, move, save or suppose
    /// about). Writes <paramref name="belowPlane"/> (<c>magnitude &lt; threshold</c>)
    /// and <paramref name="abovePlane"/> (<c>magnitude &gt; threshold</c>).
    /// Reading <paramref name="belowPlane"/> through <see cref="Flipped"/> gives
    /// <c>magnitude &gt;= threshold</c>; reading <paramref name="abovePlane"/>
    /// through <see cref="Flipped"/> gives <c>magnitude &lt;= threshold</c>
    /// (inside a viability bound).</summary>
    public static IReadOnlyList<Wiring> ObserveThreshold(
        Engine engine,
        Surface magnitudeSurface,
        Field magnitude,
        Surface thresholdSurface,
        Field threshold,
        Store outStore,
        int belowPlane,
        int abovePlane,
        params int[] positions) =>
        ObserveMismatch(engine, magnitudeSurface, magnitude, thresholdSurface, threshold, outStore, belowPlane, abovePlane, positions);

    /// <summary>Wires unary accumulation (<c>sum = a + b</c>) as a single-writer
    /// transition law (<see cref="TensionLaws.Sum"/>), emulating coarse numeric
    /// addition in planes so downstream threshold and mismatch circuits read the
    /// combined count directly.</summary>
    public static Wiring Accumulate(
        Engine engine,
        Surface surfaceA,
        Field a,
        Surface surfaceB,
        Field b,
        Surface sumSurface,
        Field sum,
        params int[] positions)
    {
        ArgumentNullException.ThrowIfNull(engine);
        ArgumentNullException.ThrowIfNull(surfaceA);
        ArgumentNullException.ThrowIfNull(surfaceB);
        ArgumentNullException.ThrowIfNull(sumSurface);
        CheckUnary(a, nameof(a));
        CheckUnary(b, nameof(b));
        CheckUnary(sum, nameof(sum));
        if (sum.Shape.Bits != a.Shape.Bits + b.Shape.Bits)
            throw new ArgumentException("Sum field capacity must equal the sum of the two operand capacities.", nameof(sum));
        var targets = positions.Length > 0 ? positions : Enumerable.Range(0, sumSurface.Count).ToArray();
        var wiring = new Wiring(TensionLaws.Sum(a.Shape.Bits, b.Shape.Bits), a.On(surfaceA), b.On(surfaceB), sum.On(sumSurface));
        foreach (var p in targets) wiring.Add(p, p, p);
        engine.Add(wiring);
        return wiring;
    }

    private static void CheckUnary(Field field, string paramName)
    {
        ArgumentNullException.ThrowIfNull(field, paramName);
        if (!field.Shape.IsUnary) throw new ArgumentException("A unary field is needed.", paramName);
    }
}
