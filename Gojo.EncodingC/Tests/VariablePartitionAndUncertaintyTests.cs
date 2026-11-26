using Xunit;

namespace Gojo.EncodingC.Tests;

/// <summary>Variable-partition scales, mechanism-dependent uncertainty,
/// live-denominator portion guards, and cross-structure atomic admission
/// (GojoOperationsSynthesis-2026-11.md, §7 item 5).</summary>
public sealed class VariablePartitionAndUncertaintyTests
{
    private static readonly Plane Here = Plane.Ones(1);

    private static Field Allocate(Store store, Shape shape) =>
        new(shape, [.. Enumerable.Range(0, shape.Bits).Select(_ => store.AddPlane())]);

    private static ulong Mask(params int[] values) => values.Aggregate(0UL, (m, v) => m | 1UL << v);

    private static Engine Wired(IEnumerable<Wiring> wirings)
    {
        var engine = new Engine();
        foreach (var w in wirings) engine.Add(w);
        return engine;
    }

    [Theory]
    [MemberData(nameof(ExecutionConformancePath.All), MemberType = typeof(ExecutionConformancePath), DisableDiscoveryEnumeration = true)]
    public void MixedPartitionHalfStarsAddSubtractAndNarrowJointlyInAFork(ExecutionConformancePath path)
    {
        // Half-stars out of 5: mixed partition [2, 5] with 1 star = 2 half-stars.
        var store = new Store(0, 1);
        var a = new Ladder(store, [2, 5]).AtUnitRung(1);
        var b = new Ladder(store, [2, 5]).AtUnitRung(1);
        var sum = new Ladder(store, [2, 5]).AtUnitRung(1);
        Assert.Equal(2L, a.TicksPerUnit);
        Assert.Equal(9L, a.Capacity);
        var (wirings, carries) = Ladder.Add(a, b, sum, Here);
        var engine = Wired(wirings);

        // Forward: 3.5 + 1.5 = 5.0 (remainder 0.0, overflow carry 1 whole 5-star block).
        a.Restrict(0, 7);
        b.Restrict(0, 3);
        Assert.NotNull(path.Settle(engine, 8));
        Assert.Equal(0L, sum.Read(0));
        Assert.Equal(Mask(1), carries[^1].Values(0));

        // Backward subtraction on the exact same wiring after whole-network reopen: 5.0 - 1.5 = 3.5.
        Ladder.Reopen(wirings, 0);
        b.Restrict(0, 3);
        sum.Restrict(0, 0);
        carries[^1].Restrict(0, Mask(1));
        Assert.NotNull(path.Settle(engine, 8));
        Assert.Equal(7L, a.Read(0));

        // Coarse half-star sum: 3 stars measured ({6, 7} half-stars) + 2 stars measured ({4, 5} half-stars).
        // Per-rung marginal supports keep {0, 1} x {0, 1} (5.0..6.5), while joint probing in a Fork
        // eliminates the impossible corner 6.5 (3 half-stars above 5.0) and retains exact {5.0, 5.5, 6.0}.
        Ladder.Reopen(wirings, 0);
        a.Measure(0, 3, rung: 1);
        b.Measure(0, 2, rung: 1);
        Assert.NotNull(path.Settle(engine, 8));
        Assert.Equal(Mask(1), carries[^1].Values(0));
        Assert.Equal([Mask(0, 1), Mask(0, 1)], sum.Open(0));
        Assert.Equal((0L, 3L), sum.Interval(0));

        var surviving = new List<int>();
        for (var cand = 0; cand < 4; cand++)
        {
            var probe = engine.Fork();
            var surface = probe.Engine.Wirings[0].Bindings[0].Surface;
            var pS0 = sum.Possible[0].In(surface);
            var pS1 = sum.Possible[1].In(surface);
            var pC0 = carries[0].In(surface);
            pS0.Restrict(0, Mask(cand % 2));
            pS1.Restrict(0, Mask(cand / 2));
            path.Settle(probe.Engine, 8);
            if (pS0.Values(0) != 0 && pS1.Values(0) != 0 && pC0.Values(0) != 0) surviving.Add(cand);
        }
        Assert.Equal([0, 1, 2], surviving);
    }

    [Theory]
    [MemberData(nameof(ExecutionConformancePath.All), MemberType = typeof(ExecutionConformancePath), DisableDiscoveryEnumeration = true)]
    public void DoublingOneCoarseSampleDiffersFromSummingTwoIndependentSamples(ExecutionConformancePath path)
    {
        // 1. On Pass-1/2 Ladder.Add ([2, 8] half-stars, unit = star):
        //    Doubling ONE coarse 3-star sample x in {6, 7} wires x to BOTH operands; shared-site identity
        //    settles the half-star digit to exact 0 ({12, 14} = {6.0, 7.0}, skipping 6.5!).
        //    Summing TWO independent coarse 3-star samples x and y leaves the half-star digit open ({12, 13, 14}).
        var store = new Store(0, 1);
        var x = new Ladder(store, [2, 8]).AtUnitRung(1);
        var y = new Ladder(store, [2, 8]).AtUnitRung(1);
        var doubled = new Ladder(store, [2, 8]).AtUnitRung(1);
        var summed = new Ladder(store, [2, 8]).AtUnitRung(1);
        var engine = new Engine();
        foreach (var w in Ladder.Add(x, x, doubled, Here).Wirings) engine.Add(w);
        foreach (var w in Ladder.Add(x, y, summed, Here).Wirings) engine.Add(w);

        x.Measure(0, 3, rung: 1);
        y.Measure(0, 3, rung: 1);
        Assert.NotNull(path.Settle(engine, 8));
        Assert.Equal([Mask(0), Mask(6, 7)], doubled.Open(0));
        Assert.Equal([Mask(0, 1), Mask(6, 7)], summed.Open(0));

        // 2. Single-rung balance contrast: shared site a1 + a1 -> {12, 14}; distinct a1 + a2 -> {12, 13, 14}.
        var flat = new Store(0, 2);
        var d16 = Shape.Binary(16);
        Supports S() => Supports.On(flat, Allocate(flat, d16));
        var (a1, a2, outSum) = (S(), S(), S());
        var sumRel = Operations.Balance([d16, d16, d16], 1, 1, -1);
        var wStretch = new Wiring(sumRel, a1.Variable, a1.Variable, outSum.Variable);
        wStretch.Add(0, 0, 0);
        var wIndep = new Wiring(sumRel, a1.Variable, a2.Variable, outSum.Variable);
        wIndep.Add(1, 1, 1);

        var coarse = Mask(6, 7);
        a1.Restrict(0, coarse);
        a1.Restrict(1, coarse);
        a2.Restrict(1, coarse);
        Assert.NotNull(path.Settle(new Engine().Add(wStretch).Add(wIndep), 4));
        Assert.Equal(Mask(12, 14), outSum.Values(0));
        Assert.Equal(Mask(12, 13, 14), outSum.Values(1));
    }

    [Theory]
    [MemberData(nameof(ExecutionConformancePath.All), MemberType = typeof(ExecutionConformancePath), DisableDiscoveryEnumeration = true)]
    public void SharedSiteDifferenceCancelsToZeroWhileIndependentDifferenceStaysOpen(ExecutionConformancePath path)
    {
        // 1. On Pass-1/2 Ladder.Add: diff + x = x with shared coarse x in {6, 7} settles diff to exact 0.
        var store = new Store(0, 1);
        var x = new Ladder(store, [2, 8]).AtUnitRung(1);
        var diff = new Ladder(store, [2, 8]).AtUnitRung(1);
        var (wirings, carries) = Ladder.Add(diff, x, x, Here);
        var ladderEngine = Wired(wirings);
        x.Measure(0, 3, rung: 1);
        carries[^1].Restrict(0, Mask(0));
        Assert.NotNull(path.Settle(ladderEngine, 8));
        Assert.Equal(0L, diff.Read(0));

        // 2. Open gap balance (a + deficit = b + excess, deficit = 0 or excess = 0):
        //    Shared site x - x cancels to exact (0, 0); independent x1 - x2 stays open ({0, 1}, {0, 1}).
        var gStore = new Store(0, 2);
        var d8 = Shape.Unary(7);
        Supports G() => Supports.On(gStore, Allocate(gStore, d8));
        var (gx1, gx2, deficit, excess) = (G(), G(), G(), G());
        var gapLaw = RelationLaw.FromPredicate([d8, d8, d8, d8],
            t => (t[2] == 0 || t[3] == 0) && t[0] + t[2] == t[1] + t[3]);
        var wSelf = new Wiring(gapLaw, gx1.Variable, gx1.Variable, deficit.Variable, excess.Variable);
        wSelf.Add(0, 0, 0, 0);
        var wPair = new Wiring(gapLaw, gx1.Variable, gx2.Variable, deficit.Variable, excess.Variable);
        wPair.Add(1, 1, 1, 1);

        var coarse = Mask(6, 7);
        gx1.Restrict(0, coarse);
        gx1.Restrict(1, coarse);
        gx2.Restrict(1, coarse);
        Assert.NotNull(path.Settle(new Engine().Add(wSelf).Add(wPair), 4));
        Assert.Equal((Mask(0), Mask(0)), (deficit.Values(0), excess.Values(0)));
        Assert.Equal((Mask(0, 1), Mask(0, 1)), (deficit.Values(1), excess.Values(1)));
    }

    [Theory]
    [MemberData(nameof(ExecutionConformancePath.All), MemberType = typeof(ExecutionConformancePath), DisableDiscoveryEnumeration = true)]
    public void OrdinalIntervalAndRatioReadsStayDistinctOnFiveStarScales(ExecutionConformancePath path)
    {
        // Ordinal 1..5 scope: silent lane stays open {1..5}; explicit 0 contradicts (distinct from silence).
        var ordStore = new Store(0, 2);
        var six = Shape.Binary(6);
        var rating = Supports.On(ordStore, Allocate(ordStore, six));
        var ordinalScope = RelationLaw.FromPredicate([six], t => t[0] >= 1 && t[0] <= 5);
        var ordW = new Wiring(ordinalScope, rating.Variable);
        ordW.Add(0);
        ordW.Add(1);
        rating.Restrict(1, Mask(0));
        Assert.NotNull(path.Settle(new Engine().Add(ordW), 4));
        Assert.Equal(Mask(1, 2, 3, 4, 5), rating.Values(0));
        Assert.Equal(0UL, rating.Values(1));

        // Ratio 3/5 vs Interval (3-1)/(5-1) = 2/4 compared without floats: 3/5 > 2/4 (order = 2).
        var cmpStore = new Store(0, 1);
        Supports Sup(Shape shape, int? exact = null)
        {
            var sup = Supports.On(cmpStore, Allocate(cmpStore, shape));
            if (exact is int v) sup.Restrict(0, Mask(v));
            return sup;
        }
        var order = Sup(Shape.Binary(3));
        var cmpW = new Wiring(Portions.Compare(5, binary: true),
            Sup(six, 3).Variable, Sup(six, 5).Variable,
            Sup(six, 2).Variable, Sup(six, 4).Variable,
            order.Variable);
        cmpW.Add(0, 0, 0, 0, 0);
        Assert.NotNull(path.Settle(new Engine().Add(cmpW), 4));
        Assert.Equal(Mask(2), order.Values(0));
    }

    [Theory]
    [MemberData(nameof(ExecutionConformancePath.All), MemberType = typeof(ExecutionConformancePath), DisableDiscoveryEnumeration = true)]
    public void LiveDenominatorPortionsRejectZeroDenominatorAndKeepTwoWholesForFullPortions(ExecutionConformancePath path)
    {
        // 1. Guarded pooling: (3,5) (+) (2,5) = (5,10), (3,5) (+) (7,10) = (10,15),
        //    while invalid operands (1,0) and (6,5) contradict (Sol defect #1 fixed).
        var pStore = new Store(0, 4);
        var s10 = Shape.Binary(11);
        var s16 = Shape.Binary(16);
        Supports P(Shape shape) => Supports.On(pStore, Allocate(pStore, shape));
        var (n1, d1, n2, d2, nOut, dOut) = (P(s10), P(s10), P(s10), P(s10), P(s16), P(s16));
        var poolEngine = Wired(Portions.WirePool(n1, d1, n2, d2, nOut, dOut, Plane.Ones(4)));

        // Lane 0: (3, 5) (+) (2, 5) = (5, 10)
        n1.Restrict(0, Mask(3)); d1.Restrict(0, Mask(5)); n2.Restrict(0, Mask(2)); d2.Restrict(0, Mask(5));
        // Lane 1: (3, 5) (+) (7, 10) = (10, 15)
        n1.Restrict(1, Mask(3)); d1.Restrict(1, Mask(5)); n2.Restrict(1, Mask(7)); d2.Restrict(1, Mask(10));
        // Lane 2: (1, 0) (+) (2, 5) -> zero denominator rejected as contradiction!
        n1.Restrict(2, Mask(1)); d1.Restrict(2, Mask(0)); n2.Restrict(2, Mask(2)); d2.Restrict(2, Mask(5));
        // Lane 3: (6, 5) (+) (2, 5) -> n > d rejected as contradiction!
        n1.Restrict(3, Mask(6)); d1.Restrict(3, Mask(5)); n2.Restrict(3, Mask(2)); d2.Restrict(3, Mask(5));

        Assert.NotNull(path.Settle(poolEngine, 8));
        Assert.Equal((Mask(5), Mask(10)), (nOut.Values(0), dOut.Values(0)));
        Assert.Equal((Mask(10), Mask(15)), (nOut.Values(1), dOut.Values(1)));
        Assert.Equal((0UL, 0UL), (nOut.Values(2), dOut.Values(2)));
        Assert.Equal((0UL, 0UL), (nOut.Values(3), dOut.Values(3)));

        // 2. Same-whole addition over a live denominator:
        //    3/5 + 2/5 -> (1, 0); 4/5 + 3/5 -> (1, 2); 5/5 + 5/5 -> (2, 0) (Sol defect #2 fixed!);
        //    3/5 + 2/3 -> empty contradiction.
        var wStore = new Store(0, 4);
        var s6 = Shape.Binary(6);
        var wholeShape = Shape.Binary(3);
        Supports W(Shape shape) => Supports.On(wStore, Allocate(wStore, shape));
        var (wa, wb, wd1, wd2, whole, rem) = (W(s6), W(s6), W(s6), W(s6), W(wholeShape), W(s6));
        var sameDen = new Wiring(Settling.Same(s6), wd1.Variable, wd2.Variable);
        var sameAdd = new Wiring(Portions.AddSameWhole(5, binary: true),
            wa.Variable, wb.Variable, wd1.Variable, whole.Variable, rem.Variable);
        for (var lane = 0; lane < 4; lane++) { sameDen.Add(lane, lane); sameAdd.Add(lane, lane, lane, lane, lane); }

        wa.Restrict(0, Mask(3)); wb.Restrict(0, Mask(2)); wd1.Restrict(0, Mask(5)); wd2.Restrict(0, Mask(5));
        wa.Restrict(1, Mask(4)); wb.Restrict(1, Mask(3)); wd1.Restrict(1, Mask(5)); wd2.Restrict(1, Mask(5));
        wa.Restrict(2, Mask(5)); wb.Restrict(2, Mask(5)); wd1.Restrict(2, Mask(5)); wd2.Restrict(2, Mask(5));
        wa.Restrict(3, Mask(3)); wb.Restrict(3, Mask(2)); wd1.Restrict(3, Mask(5)); wd2.Restrict(3, Mask(3));

        Assert.NotNull(path.Settle(new Engine().Add(sameDen).Add(sameAdd), 8));
        Assert.Equal((Mask(1), Mask(0)), (whole.Values(0), rem.Values(0)));
        Assert.Equal((Mask(1), Mask(2)), (whole.Values(1), rem.Values(1)));
        Assert.Equal((Mask(2), Mask(0)), (whole.Values(2), rem.Values(2)));
        Assert.Equal((0UL, 0UL), (wd1.Values(3), rem.Values(3)));

        // 3. Live-denominator projection keeps exact source-basis residue and rejects d1 = 0.
        var rStore = new Store(0, 4);
        Supports R() => Supports.On(rStore, Allocate(rStore, s10));
        var (pn, pd1, pd2, pq, pr) = (R(), R(), R(), R(), R());
        var pw = new Wiring(Portions.Project(10, binary: true),
            pn.Variable, pd1.Variable, pd2.Variable, pq.Variable, pr.Variable);
        for (var lane = 0; lane < 4; lane++) pw.Add(lane, lane, lane, lane, lane);

        pn.Restrict(0, Mask(3)); pd1.Restrict(0, Mask(5)); pd2.Restrict(0, Mask(10));
        pn.Restrict(1, Mask(3)); pd1.Restrict(1, Mask(5)); pd2.Restrict(1, Mask(8));
        pn.Restrict(2, Mask(4)); pd1.Restrict(2, Mask(5)); pd2.Restrict(2, Mask(3));
        pn.Restrict(3, Mask(0)); pd1.Restrict(3, Mask(0)); pd2.Restrict(3, Mask(8));

        Assert.NotNull(path.Settle(new Engine().Add(pw), 4));
        Assert.Equal((Mask(6), Mask(0)), (pq.Values(0), pr.Values(0)));
        Assert.Equal((Mask(4), Mask(4)), (pq.Values(1), pr.Values(1)));
        Assert.Equal((Mask(2), Mask(2)), (pq.Values(2), pr.Values(2)));
        Assert.Equal(0UL, pq.Values(3));
    }

    [Theory]
    [MemberData(nameof(ExecutionConformancePath.All), MemberType = typeof(ExecutionConformancePath), DisableDiscoveryEnumeration = true)]
    public void MaterialPacketsDoNotSplitAndCommonRefinementMovesWholePackets(ExecutionConformancePath path)
    {
        // Material fifths-to-eighths: 5 fifths trade for 8 eighths; with only 3 fifths, 0 eighths move and all 3 wait.
        var mStore = new Store(0, 1);
        var fifths = Allocate(mStore, Shape.Unary(5));
        var eighths = Allocate(mStore, Shape.Unary(8));
        fifths.Write(mStore, 0, 3);
        var matEng = new Engine().Add(Tension.Route(Flows.Convert(5, 8, 5, 8), fifths.On(mStore), eighths.On(mStore), Here));
        Assert.Equal(1, path.Settle(matEng, 4));
        Assert.Equal((3, 0), (fifths.Read(mStore, 0), eighths.Read(mStore, 0)));

        // Common refinement via conserving moves: 2/5 + 1/3 -> 6/15 + 5/15 = 11/15.
        var cStore = new Store(0, 1);
        var aFifths = Allocate(cStore, Shape.Unary(5));
        var bThirds = Allocate(cStore, Shape.Unary(3));
        var fifteenths = Allocate(cStore, Shape.Unary(15));
        aFifths.Write(cStore, 0, 2);
        bThirds.Write(cStore, 0, 1);
        var cEng = new Engine()
            .Add(Tension.Route(Flows.Convert(1, 3, 5, 15), aFifths.On(cStore), fifteenths.On(cStore), Here))
            .Add(Tension.Route(Flows.Convert(1, 5, 3, 15), bThirds.On(cStore), fifteenths.On(cStore), Here));
        Assert.NotNull(path.Settle(cEng, 8));
        Assert.Equal((0, 0, 11), (aFifths.Read(cStore, 0), bThirds.Read(cStore, 0), fifteenths.Read(cStore, 0)));
    }

    [Theory]
    [MemberData(nameof(ExecutionConformancePath.All), MemberType = typeof(ExecutionConformancePath), DisableDiscoveryEnumeration = true)]
    public void AtomicAdmissionAtPartitionFiveSurvivesSaveLocalsMidCarry(ExecutionConformancePath path)
    {
        const int partition = 5;
        const int coarseCap = 2;
        const int totalCap = (coarseCap + 1) * partition - 1; // 14

        static (Engine Source, Tension Incoming, Engine Receiver, Store Rung, Field Coarse, Field Fine) Arrange()
        {
            var srcStore = new Store(0, 1);
            var incoming = new Tension(srcStore, Allocate(srcStore, Shape.Unary(1)), capacity: 1);
            var source = new Engine().Hold(srcStore);
            var outlet = srcStore.Publish([0], [.. incoming.Plus.Planes]);

            var rung = new Store(0, 1);
            var coarse = Allocate(rung, Shape.Unary(coarseCap));
            var fine = Allocate(rung, Numbers.Digit(partition));
            var admit = Tension.Wire(
                Numbers.Admit(partition, pressureCapacity: 1, coarseCap),
                [new Binding(outlet, [.. Enumerable.Range(0, outlet.PlaneCount)]), coarse.On(rung), fine.On(rung)],
                Here);
            return (source, incoming, new Engine().Add(admit), rung, coarse, fine);
        }

        // Admit 1..15 units: fine stays in 0..4, carries atomically at 5 and 10, 15th waits as pressure.
        var f = Arrange();
        for (var offered = 1; offered <= totalCap + 1; offered++)
        {
            Assert.Equal(Here, f.Incoming.Receive(Here).Taken);
            var step = Assert.Single(path.Step(f.Receiver).Steps);
            var received = offered <= totalCap ? offered : totalCap;
            var pending = f.Incoming.Plus.Read(f.Incoming.Store, 0)!.Value;
            var coarse = f.Coarse.Read(f.Rung, 0)!.Value;
            var fine = f.Fine.Read(f.Rung, 0)!.Value;
            Assert.Equal(offered <= totalCap, step.Changed[0]);
            Assert.Equal((received / partition, received % partition), (coarse, fine));
            Assert.Equal(offered, pending + partition * coarse + fine);
        }
        Assert.Equal(Here, f.Incoming.Receive(Here).Blocked);

        // Mid-carry SaveLocals / LoadLocals at (0, 4) + 1 pending continues identically to (1, 0).
        var m = Arrange();
        for (var i = 0; i < 4; i++)
        {
            Assert.Equal(Here, m.Incoming.Receive(Here).Taken);
            path.Step(m.Receiver);
        }
        Assert.Equal(Here, m.Incoming.Receive(Here).Taken);

        var saved = Transport.SaveLocals(m.Source, m.Receiver);
        var restored = Transport.LoadLocals(saved);
        Assert.Equal(saved, Transport.SaveLocals(restored[0].Engine, restored[1].Engine));

        path.Step(m.Receiver);
        path.Step(restored[1].Engine);
        Assert.Equal((1, 0), (m.Coarse.Read(m.Rung, 0), m.Fine.Read(m.Rung, 0)));
        Assert.Equal(Transport.SaveLocals(m.Source, m.Receiver), Transport.SaveLocals(restored[0].Engine, restored[1].Engine));
    }
}
