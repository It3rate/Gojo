using Xunit;

namespace Gojo.EncodingC.Tests;

/// <summary>Optional, derived <see cref="CompiledPlan"/> verification
/// (Gojo/docs/GojoOperationsSynthesis-2026-11.md, §7 items 2–3): proves
/// eligible graphs genuinely execute compiled kernels with exact bit, tick,
/// quiet, and transport parity, while every ineligible or stale case falls
/// back cleanly to <see cref="Engine"/>.</summary>
public sealed class CompiledPlanTests
{
    private static Field Allocate(Store store, Shape shape) =>
        new(shape, [.. Enumerable.Range(0, shape.Bits).Select(_ => store.AddPlane())]);

    private static Engine Wired(IEnumerable<Wiring> wirings)
    {
        var engine = new Engine();
        foreach (var w in wirings) engine.Add(w);
        return engine;
    }

    [Fact]
    public void PassOneRelationalLaddersTakeTheCompiledFastPathAcrossMixedRadicesAndReobservation()
    {
        // 1. Mixed-partition [2, 5] half-stars across all 100 exact pairs + open/backward/contradictory lanes.
        const int lanes = 104;
        var canonStore = new Store(0, lanes);
        var fastStore = new Store(0, lanes);

        (Engine Eng, Ladder A, Ladder B, Ladder Sum, IReadOnlyList<Supports> Carries, IReadOnlyList<Wiring> Wirings) Build(Store st)
        {
            Ladder a = new(st, [2, 5], binary: true), b = new(st, [2, 5], binary: true), sum = new(st, [2, 5], binary: true);
            var (w, c) = Ladder.Add(a, b, sum, Plane.Ones(lanes));
            for (var x = 0; x < 10; x++)
                for (var y = 0; y < 10; y++)
                {
                    var lane = x * 10 + y;
                    a.Restrict(lane, x);
                    b.Restrict(lane, y);
                }
            // Lane 100: coarse measurement (open fine half-star)
            a.Measure(100, 3, rung: 1);
            b.Measure(100, 2, rung: 1);
            // Lane 101: backward subtraction 8 - 3 = 5
            b.Restrict(101, 3);
            sum.Restrict(101, 8);
            c[^1].Restrict(101, 1UL << 0);
            // Lane 102: impossible sum (contradiction)
            a.Restrict(102, 4);
            b.Restrict(102, 4);
            sum.Restrict(102, 1);
            // Lane 103: completely open
            return (Wired(w), a, b, sum, c, w);
        }

        var canon = Build(canonStore);
        var fast = Build(fastStore);
        var plan = CompiledPlan.Compile(fast.Eng);
        Assert.True(plan.RecognizedRelationalLadderAdder);
        Assert.True(plan.HasAnyCompiledKernel);

        var canonTicks = canon.Eng.Settle(10);
        var fastTicks = plan.Settle(10, out var report);
        Assert.True(report.UsedFastPath);
        Assert.False(report.Stale);
        Assert.True(report.FastLanes > 0);
        Assert.Equal(0, report.DeoptLanes);
        Assert.Equal(canonTicks, fastTicks);
        Assert.Equal(canon.Eng.Tick, fast.Eng.Tick);
        Assert.Equal(Transport.Save(canon.Eng), Transport.Save(fast.Eng));
        Assert.Equal(5L, fast.A.Read(101));
        Assert.Null(fast.Sum.Interval(102));

        // Re-observe lane 0 via Ladder.Reopen: plan stays current and runs compiled again.
        Ladder.Reopen(canon.Wirings, 0);
        Ladder.Reopen(fast.Wirings, 0);
        canon.A.Restrict(0, 7); canon.B.Restrict(0, 6);
        fast.A.Restrict(0, 7); fast.B.Restrict(0, 6);
        Assert.True(plan.IsCurrent);
        Assert.Equal(canon.Eng.Settle(10), plan.Settle(10, out var reobs));
        Assert.True(reobs.UsedFastPath);
        Assert.Equal(3L, fast.Sum.Read(0));
        Assert.Equal(1UL << 1, fast.Carries[^1].Values(0));
        Assert.Equal(Transport.Save(canon.Eng), Transport.Save(fast.Eng));

        // 2. Wide non-SWAR4 clock ladder [60, 24]: still takes the compiled exact factorized relational path!
        var clkCanon = new Store(0, 2);
        var clkFast = new Store(0, 2);
        Ladder ca1 = new(clkCanon, [60, 24], binary: true), cb1 = new(clkCanon, [60, 24], binary: true), cs1 = new(clkCanon, [60, 24], binary: true);
        Ladder ca2 = new(clkFast, [60, 24], binary: true), cb2 = new(clkFast, [60, 24], binary: true), cs2 = new(clkFast, [60, 24], binary: true);
        var clkEng1 = Wired(Ladder.Add(ca1, cb1, cs1, Plane.Ones(2)).Wirings);
        var clkEng2 = Wired(Ladder.Add(ca2, cb2, cs2, Plane.Ones(2)).Wirings);
        ca1.Restrict(0, 23 * 60 + 59); cb1.Restrict(0, 1); ca1.Restrict(1, 10 * 60 + 45); cb1.Restrict(1, 20);
        ca2.Restrict(0, 23 * 60 + 59); cb2.Restrict(0, 1); ca2.Restrict(1, 10 * 60 + 45); cb2.Restrict(1, 20);

        var clkPlan = CompiledPlan.Compile(clkEng2);
        Assert.True(clkPlan.RecognizedRelationalLadderAdder);
        Assert.Equal(clkEng1.Settle(8), clkPlan.Settle(8, out var clkRep));
        Assert.True(clkRep.UsedFastPath);
        Assert.Equal(0L, cs2.Read(0));
        Assert.Equal(11 * 60 + 5L, cs2.Read(1));
        Assert.Equal(Transport.Save(clkEng1), Transport.Save(clkEng2));

        // 3. Differing top-rung shapes: 1-rung [6] + [6] -> [11] (3/5 + 2/5 = 5/10) and 2-rung [5, 2] + [5, 2] -> [5, 3].
        var topStore = new Store(0, 1);
        Ladder tA = new(topStore, [6]), tB = new(topStore, [6]), tSum = new(topStore, [11]);
        var tEng = Wired(Ladder.Add(tA, tB, tSum, Plane.Ones(1)).Wirings);
        tA.Restrict(0, 3); tB.Restrict(0, 2);
        var tPlan = CompiledPlan.Compile(tEng);
        Assert.True(tPlan.RecognizedRelationalColumn);
        Assert.NotNull(tPlan.Settle(6, out var tRep));
        Assert.True(tRep.UsedFastPath);
        Assert.Equal(5L, tSum.Read(0));

        var uStore = new Store(0, 2);
        Ladder uA = new Ladder(uStore, [5, 2]).AtUnitRung(1), uB = new Ladder(uStore, [5, 2]).AtUnitRung(1), uS = new Ladder(uStore, [5, 3]).AtUnitRung(1);
        var uEng = Wired(Ladder.Add(uA, uB, uS, Plane.Ones(2)).Wirings);
        uA.Restrict(0, 3); uB.Restrict(0, 2);
        uA.Restrict(1, 5); uB.Restrict(1, 5);
        var uPlan = CompiledPlan.Compile(uEng);
        Assert.True(uPlan.RecognizedRelationalLadderAdder);
        Assert.NotNull(uPlan.Settle(8, out var uRep));
        Assert.True(uRep.UsedFastPath);
        Assert.Equal(5L, uS.Read(0));
        Assert.Equal(10L, uS.Read(1));
    }

    [Fact]
    public void ConcreteMixedPartitionAndDecimalRippleAddersTakeTheCompiledSwarPath()
    {
        // Build both a combined [2, 5] adder and a split [10, 10, 10] adder directly from Pass-1 Numbers.DigitSum.
        static (Engine Engine, Store Store, Field[] A, Field[] B, Field[] Sum, int Cout) BuildAdder(int[] radices, int lanes, bool split)
        {
            var store = new Store(0, lanes);
            var a = radices.Select(r => Allocate(store, Numbers.Digit(r, binary: true))).ToArray();
            var b = radices.Select(r => Allocate(store, Numbers.Digit(r, binary: true))).ToArray();
            var sum = radices.Select(r => Allocate(store, Numbers.Digit(r, binary: true))).ToArray();
            var cin = store.AddPlane();
            var carries = new int[radices.Length];
            for (var i = 0; i < radices.Length; i++) carries[i] = store.AddPlane();

            var engine = new Engine();
            for (var i = 0; i < radices.Length; i++)
            {
                var p = radices[i];
                var d = Numbers.Digit(p, binary: true);
                var col = Numbers.DigitSum(p, binary: true);
                var cIn = i == 0 ? cin : carries[i - 1];
                var cOut = carries[i];
                if (!split)
                {
                    var addLaw = Operations.Bind(col.Relation, [d, d, Numbers.Carry, Numbers.Carry, d],
                        [0, 1, 2, -1, -1], [-1, -1, -1, 3, 4]);
                    var w = new Wiring(addLaw, a[i].On(store), b[i].On(store),
                        new Binding(store, cIn), new Binding(store, cOut), sum[i].On(store));
                    for (var lane = 0; lane < lanes; lane++) w.Add(lane, lane, lane, lane, lane);
                    engine.Add(w);
                }
                else
                {
                    var sumLaw = Operations.Bind(col.Relation, [d, d, Numbers.Carry, d],
                        [0, 1, 2, -1, -1], [-1, -1, -1, -1, 3]);
                    var carLaw = Operations.Bind(col.Relation, [d, d, Numbers.Carry, Numbers.Carry],
                        [0, 1, 2, -1, -1], [-1, -1, -1, 3, -1]);
                    var ws = new Wiring(sumLaw, a[i].On(store), b[i].On(store), new Binding(store, cIn), sum[i].On(store));
                    var wc = new Wiring(carLaw, a[i].On(store), b[i].On(store), new Binding(store, cIn), new Binding(store, cOut));
                    for (var lane = 0; lane < lanes; lane++) { ws.Add(lane, lane, lane, lane); wc.Add(lane, lane, lane, lane); }
                    engine.Add(ws).Add(wc);
                }
            }
            return (engine, store, a, b, sum, carries[^1]);
        }

        // 1. Mixed [2, 5] combined adder: all 100 pairs survive Transport.Load and take compiled SWAR path.
        var mixed = BuildAdder([2, 5], 100, split: false);
        for (var x = 0; x < 10; x++)
            for (var y = 0; y < 10; y++)
            {
                var lane = x * 10 + y;
                mixed.A[0].Write(mixed.Store, lane, x % 2); mixed.A[1].Write(mixed.Store, lane, x / 2);
                mixed.B[0].Write(mixed.Store, lane, y % 2); mixed.B[1].Write(mixed.Store, lane, y / 2);
            }
        var saved = Transport.Save(mixed.Engine);
        var (canonEng, _) = Transport.Load(saved);
        var (fastEng, _) = Transport.Load(saved);

        var plan = CompiledPlan.Compile(fastEng);
        Assert.True(plan.RecognizedConcreteLadderAdder);
        var step = plan.Step(out var rep);
        var canonStep = canonEng.Step();
        Assert.True(rep.UsedFastPath);
        Assert.Equal(100, rep.FastLanes);
        Assert.Equal(0, rep.DeoptLanes);
        for (var i = 0; i < canonStep.Steps.Count; i++)
        {
            Assert.Equal(canonStep.Steps[i].Changed, step.Steps[i].Changed);
            Assert.Equal(canonStep.Steps[i].Before, step.Steps[i].Before);
            Assert.Equal(canonStep.Steps[i].After, step.Steps[i].After);
        }
        Assert.Equal(Transport.Save(canonEng), Transport.Save(fastEng));

        // Under together: true, multi-wiring ripple must NOT fuse across rungs — falls back cleanly to exact 1-rung-per-tick!
        var (tCanon, _) = Transport.Load(saved);
        var (tFast, _) = Transport.Load(saved);
        var tPlan = CompiledPlan.Compile(tFast);
        Assert.Equal(tCanon.Settle(8, together: true), tPlan.Settle(8, out var tRep, together: true));
        Assert.False(tRep.UsedFastPath);
        Assert.Equal(Transport.Save(tCanon), Transport.Save(tFast));

        // 2. Split 3-digit base-10 [10, 10, 10] adder across 256 lanes.
        var decCanon = BuildAdder([10, 10, 10], 256, split: true);
        var decFast = BuildAdder([10, 10, 10], 256, split: true);
        uint rng = 123456789;
        for (var lane = 0; lane < 256; lane++)
        {
            rng ^= rng << 13; rng ^= rng >> 17; rng ^= rng << 5;
            var x = (int)(rng % 1000);
            var y = (int)((rng >> 10) % 1000);
            for (var r = 0; r < 3; r++)
            {
                decCanon.A[r].Write(decCanon.Store, lane, x % 10); decCanon.B[r].Write(decCanon.Store, lane, y % 10);
                decFast.A[r].Write(decFast.Store, lane, x % 10); decFast.B[r].Write(decFast.Store, lane, y % 10);
                x /= 10; y /= 10;
            }
        }
        var decPlan = CompiledPlan.Compile(decFast.Engine);
        Assert.True(decPlan.RecognizedConcreteLadderAdder);
        Assert.Equal(decCanon.Engine.Settle(8), decPlan.Settle(8, out var decRep));
        Assert.True(decRep.UsedFastPath);
        Assert.Equal(Transport.Save(decCanon.Engine), Transport.Save(decFast.Engine));
    }

    [Fact]
    public void PacketConvertBoundedMultiplyAndSmallTablesTakeCompiledFastPaths()
    {
        // 1. PacketConvert: identity AVX2 shift + exact bounded multi-step settle.
        const int lanes = 300;
        var cStore1 = new Store(0, lanes);
        var cStore2 = new Store(0, lanes);
        Field src1 = Allocate(cStore1, Shape.Unary(9)), dst1 = Allocate(cStore1, Shape.Unary(9));
        Field src2 = Allocate(cStore2, Shape.Unary(9)), dst2 = Allocate(cStore2, Shape.Unary(9));
        for (var i = 0; i < lanes; i++)
        {
            var s = i % 10;
            var d = (i * 3) % 10;
            src1.Write(cStore1, i, s); dst1.Write(cStore1, i, d);
            src2.Write(cStore2, i, s); dst2.Write(cStore2, i, d);
        }
        var convert = Flows.Convert(2, 3, 9, 9);
        var w1 = Tension.Route(convert, src1.On(cStore1), dst1.On(cStore1), Plane.Ones(lanes));
        var w2 = Tension.Route(convert, src2.On(cStore2), dst2.On(cStore2), Plane.Ones(lanes));
        var e1 = new Engine().Add(w1);
        var e2 = new Engine().Add(w2);
        var plan = CompiledPlan.Compile(e2);
        Assert.True(plan.RecognizedPacketConvert);

        // Partial budget limit = 2 (does not finish settling -> null, tick = 2).
        Assert.Equal(e1.Settle(2), plan.Settle(2, out var repPartial));
        Assert.True(repPartial.UsedFastPath);
        Assert.Equal(e1.Tick, e2.Tick);
        Assert.Equal(Transport.Save(e1), Transport.Save(e2));

        // Remaining settle finishes identically.
        Assert.Equal(e1.Settle(10), plan.Settle(10, out var repFull));
        Assert.True(repFull.UsedFastPath);
        Assert.Equal(e1.Tick, e2.Tick);
        Assert.Equal(Transport.Save(e1), Transport.Save(e2));

        // 2. Small functional transition (Counts.Shortfall) takes SimdTransitionKernel (including together: true!).
        var sStore1 = new Store(0, 128);
        var sStore2 = new Store(0, 128);
        Field h1 = Allocate(sStore1, Shape.Unary(3)), wt1 = Allocate(sStore1, Shape.Unary(3)), sh1 = Allocate(sStore1, Shape.Unary(3));
        Field h2 = Allocate(sStore2, Shape.Unary(3)), wt2 = Allocate(sStore2, Shape.Unary(3)), sh2 = Allocate(sStore2, Shape.Unary(3));
        for (var i = 0; i < 128; i++)
        {
            h1.Write(sStore1, i, i % 4); wt1.Write(sStore1, i, (i / 4) % 4);
            h2.Write(sStore2, i, i % 4); wt2.Write(sStore2, i, (i / 4) % 4);
        }
        var sw1 = Tension.Wire(Counts.Shortfall(3, 3), [h1.On(sStore1), wt1.On(sStore1), sh1.On(sStore1)], Plane.Ones(128));
        var sw2 = Tension.Wire(Counts.Shortfall(3, 3), [h2.On(sStore2), wt2.On(sStore2), sh2.On(sStore2)], Plane.Ones(128));
        var se1 = new Engine().Add(sw1);
        var se2 = new Engine().Add(sw2);
        var sPlan = CompiledPlan.Compile(se2);
        Assert.True(sPlan.RecognizedSimdTransition);
        var st1 = se1.StepTogether();
        var st2 = sPlan.Step(out var sRep, together: true);
        Assert.True(sRep.UsedFastPath);
        Assert.Equal(128, sRep.FastLanes);
        Assert.Equal(st1.Steps[0].Changed, st2.Steps[0].Changed);
        Assert.Equal(st1.Steps[0].Contested, st2.Steps[0].Contested);
        Assert.Equal(Transport.Save(se1), Transport.Save(se2));

        // 3. Bounded lossless multiplier takes BoundedMultiplyKernel.
        var mulLaw = TransitionLaw.FromFunction(
            [Shape.Binary(4), Shape.Binary(4), Shape.Binary(6), Shape.Binary(5)],
            t => { var p = t[0] * t[1]; return [t[0], t[1], p <= 5 ? p : 5, p <= 5 ? 0 : p - 5]; },
            new Accounting(true, new Unchanged(0), new Unchanged(1)));
        var mStore1 = new Store(0, 16);
        var mStore2 = new Store(0, 16);
        Field ma1 = Allocate(mStore1, Shape.Binary(4)), mb1 = Allocate(mStore1, Shape.Binary(4)),
            mv1 = Allocate(mStore1, Shape.Binary(6)), mo1 = Allocate(mStore1, Shape.Binary(5));
        Field ma2 = Allocate(mStore2, Shape.Binary(4)), mb2 = Allocate(mStore2, Shape.Binary(4)),
            mv2 = Allocate(mStore2, Shape.Binary(6)), mo2 = Allocate(mStore2, Shape.Binary(5));
        for (var a = 0; a < 4; a++)
            for (var b = 0; b < 4; b++)
            {
                var lane = a * 4 + b;
                ma1.Write(mStore1, lane, a); mb1.Write(mStore1, lane, b);
                ma2.Write(mStore2, lane, a); mb2.Write(mStore2, lane, b);
            }
        var me1 = new Engine().Add(Tension.Wire(mulLaw, [ma1.On(mStore1), mb1.On(mStore1), mv1.On(mStore1), mo1.On(mStore1)], Plane.Ones(16)));
        var me2 = new Engine().Add(Tension.Wire(mulLaw, [ma2.On(mStore2), mb2.On(mStore2), mv2.On(mStore2), mo2.On(mStore2)], Plane.Ones(16)));
        var mPlan = CompiledPlan.Compile(me2);
        Assert.True(mPlan.RecognizedBoundedMultiply);
        Assert.Equal(me1.Settle(4), mPlan.Settle(4, out var mRep));
        Assert.True(mRep.UsedFastPath);
        Assert.Equal(Transport.Save(me1), Transport.Save(me2));
    }

    [Fact]
    public void IneligibleBrokenRecordedSharedSiteAndStaleCasesFallBackCleanlyWithExactParity()
    {
        // 1. Shared-site relational Ladder.Add(x, x, doubled): must NOT use independent-role kernel, preserves diagonal!
        var shareStore = new Store(0, 1);
        var x = new Ladder(shareStore, [2, 8]).AtUnitRung(1);
        var doubled = new Ladder(shareStore, [2, 8]).AtUnitRung(1);
        var shareEng = Wired(Ladder.Add(x, x, doubled, Plane.Ones(1)).Wirings);
        x.Measure(0, 3, rung: 1);
        var sharePlan = CompiledPlan.Compile(shareEng);
        Assert.False(sharePlan.RecognizedRelationalLadderAdder);
        Assert.NotNull(sharePlan.Settle(8, out var shareRep));
        Assert.False(shareRep.UsedFastPath);
        Assert.Equal([1UL << 0, (1UL << 6) | (1UL << 7)], doubled.Open(0));

        // 2. Recorded wiring falls back cleanly and populates all 7 evidence planes identically.
        var recStore1 = new Store(0, 2);
        var recStore2 = new Store(0, 2);
        Field ra1 = Allocate(recStore1, Shape.Unary(3)), rb1 = Allocate(recStore1, Shape.Unary(3));
        Field ra2 = Allocate(recStore2, Shape.Unary(3)), rb2 = Allocate(recStore2, Shape.Unary(3));
        ra1.Write(recStore1, 0, 2); ra2.Write(recStore2, 0, 2);
        var rw1 = Tension.Route(Flows.Convert(1, 1, 3, 3), ra1.On(recStore1), rb1.On(recStore1), Plane.Ones(2));
        var rw2 = Tension.Route(Flows.Convert(1, 1, 3, 3), ra2.On(recStore2), rb2.On(recStore2), Plane.Ones(2));
        rw1.Record(); rw2.Record();
        var re1 = new Engine().Add(rw1);
        var re2 = new Engine().Add(rw2);
        var recPlan = CompiledPlan.Compile(re2);
        Assert.False(recPlan.HasAnyCompiledKernel);
        Assert.Equal(re1.Settle(6), recPlan.Settle(6, out var recRep));
        Assert.False(recRep.UsedFastPath);
        Assert.Equal(Transport.Save(re1), Transport.Save(re2));

        // 3. Broken unary code (010) triggers runtime deopt and reports Unread identically without corrupting bits.
        var brkStore1 = new Store(0, 2);
        var brkStore2 = new Store(0, 2);
        Field ba1 = Allocate(brkStore1, Shape.Unary(3)), bb1 = Allocate(brkStore1, Shape.Unary(3));
        Field ba2 = Allocate(brkStore2, Shape.Unary(3)), bb2 = Allocate(brkStore2, Shape.Unary(3));
        brkStore1.Write(ba1.Planes[1], 0, true); // broken 010 on lane 0
        brkStore2.Write(ba2.Planes[1], 0, true);
        ba1.Write(brkStore1, 1, 2); ba2.Write(brkStore2, 1, 2);
        var be1 = new Engine().Add(Tension.Route(Flows.Convert(1, 1, 3, 3), ba1.On(brkStore1), bb1.On(brkStore1), Plane.Ones(2)));
        var be2 = new Engine().Add(Tension.Route(Flows.Convert(1, 1, 3, 3), ba2.On(brkStore2), bb2.On(brkStore2), Plane.Ones(2)));
        var brkPlan = CompiledPlan.Compile(be2);
        Assert.True(brkPlan.RecognizedPacketConvert);
        var bs1 = be1.Step();
        var bs2 = brkPlan.Step(out var brkRep);
        Assert.False(brkRep.UsedFastPath);
        Assert.True(bs2.Steps[0].Unread[0]);
        Assert.Equal(bs1.Steps[0].Unread, bs2.Steps[0].Unread);
        Assert.Equal(bs1.Steps[0].Changed, bs2.Steps[0].Changed);
        Assert.Equal(Transport.Save(be1), Transport.Save(be2));

        // 4. Stale invalidation across structural edits: adding a row, toggling attention, enabling record, adding store positions.
        var st = new Store(0, 2);
        Field fa = Allocate(st, Shape.Unary(3)), fb = Allocate(st, Shape.Unary(3));
        fa.Write(st, 0, 2);
        var w = Tension.Route(Flows.Convert(1, 1, 3, 3), fa.On(st), fb.On(st), Plane.Ones(2));
        var eng = new Engine().Add(w);
        var plan = CompiledPlan.Compile(eng);
        Assert.True(plan.IsCurrent);

        var att = w.Attend();
        Assert.False(plan.IsCurrent);
        plan.Step(out var staleRep);
        Assert.True(staleRep.Stale);
        Assert.False(staleRep.UsedFastPath);

        // Recompiling after Attention recognizes the attended mask and accelerates again.
        st.Write(fa.Planes[0], 1, true);
        w.Rows.Write(att, 1, false); // suppress row 1 via Attention
        plan = CompiledPlan.Compile(eng);
        Assert.True(plan.IsCurrent);
        plan.Step(out var attRep);
        Assert.True(attRep.UsedFastPath);
        Assert.Equal(1, attRep.FastLanes);
        Assert.Equal(1, fa.Read(st, 1)); // unattended row 1 did NOT fire!

        // Growing or recruiting/retiring on a bound store invalidates the plan cleanly.
        var sparePos = st.AddSpare(1);
        Assert.False(plan.IsCurrent);
        plan = CompiledPlan.Compile(eng);
        Assert.True(plan.IsCurrent);
        st.Recruit(sparePos);
        Assert.False(plan.IsCurrent);

        // 5. Self-rewiring engine (a wiring writing another wiring's Rows store) never compiles and falls back cleanly.
        var rwSt = new Store(0, 2);
        var rwA = Allocate(rwSt, Shape.Unary(1));
        var rwB = Allocate(rwSt, Shape.Unary(1));
        rwA.Write(rwSt, 0, 1);
        var targetW = Tension.Route(Flows.Convert(1, 1, 1, 1), rwA.On(rwSt), rwB.On(rwSt), Plane.Of(2, 0));
        var targetAtt = targetW.Attend();
        var clearAtt = TransitionLaw.FromFunction([Shape.Bit], _ => [0], new Accounting(true));
        var mutator = new Wiring(clearAtt, new Binding(targetW.Rows, targetAtt));
        mutator.Add(0);
        var selfEng = new Engine().Add(mutator).Add(targetW);
        var selfPlan = CompiledPlan.Compile(selfEng);
        Assert.False(selfPlan.HasAnyCompiledKernel);
        selfPlan.Step(out var selfRep);
        Assert.False(selfRep.UsedFastPath);
        Assert.Equal(1, rwA.Read(rwSt, 0)); // mutator cleared attention mid-step, so targetW did NOT move the unit!
        Assert.Equal(0, rwB.Read(rwSt, 0));
    }

    [Fact]
    public void PassThreeAddsNoEnumsToEncodingC()
    {
        var enums = typeof(Store).Assembly.GetTypes()
            .Where(t => t.Namespace == typeof(Store).Namespace && t.IsEnum)
            .Select(t => t.FullName!)
            .ToArray();

        // Only master's LearnedRead exists; CompiledPlan, Portions, and Pass 1–3 add zero enums.
        Assert.Equal([typeof(LearnedRead).FullName!], enums);
    }
}
