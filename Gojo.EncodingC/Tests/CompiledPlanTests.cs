using Xunit;

namespace Gojo.EncodingC.Tests;

/// <summary>Optional, derived <see cref="CompiledPlan"/> verification
/// (Gojo/docs/GojoOperationsSynthesis-2026-11.md, §12): proves that the two
/// family-independent table kernels — general small-table transitions and
/// general word-outer bit-sliced relations — accelerate ladders, portions,
/// products, ODV, sides, value gaps, shared-site diagonals, conversions, and
/// counting laws with exact bit, evidence, tick, and transport parity, while
/// oversized, ineligible, broken, recorded, dynamic-topology, and stale cases
/// fall back cleanly to <see cref="Engine"/>.</summary>
public sealed class CompiledPlanTests
{
    private static Field Allocate(Store store, Shape shape) =>
        new(shape, [.. Enumerable.Range(0, shape.Bits).Select(_ => store.AddPlane())]);

    private static Supports Open(Store store, Shape shape) =>
        Supports.On(store, Allocate(store, shape));

    private static Engine Wired(IEnumerable<Wiring> wirings)
    {
        var engine = new Engine();
        foreach (var w in wirings) engine.Add(w);
        return engine;
    }

    [Fact]
    public void GeneralRelationKernelAcceleratesLaddersClocksAndSharedSiteDiagonals()
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
            a.Measure(100, 3, rung: 1);
            b.Measure(100, 2, rung: 1);
            b.Restrict(101, 3);
            sum.Restrict(101, 8);
            c[^1].Restrict(101, 1UL << 0);
            a.Restrict(102, 4);
            b.Restrict(102, 4);
            sum.Restrict(102, 1);
            return (Wired(w), a, b, sum, c, w);
        }

        var canon = Build(canonStore);
        var fast = Build(fastStore);
        var plan = CompiledPlan.Compile(fast.Eng);
        Assert.True(plan.HasAnyCompiledKernel);
        Assert.True(plan.HasRelationKernel);
        Assert.False(plan.HasTransitionKernel);

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

        // 2. Wide clock ladder [60, 24] (7,200 admitted tuples on minute rung <= 8,192 ceiling) accelerates in place.
        var clkCanon = new Store(0, 2);
        var clkFast = new Store(0, 2);
        Ladder ca1 = new(clkCanon, [60, 24], binary: true), cb1 = new(clkCanon, [60, 24], binary: true), cs1 = new(clkCanon, [60, 24], binary: true);
        Ladder ca2 = new(clkFast, [60, 24], binary: true), cb2 = new(clkFast, [60, 24], binary: true), cs2 = new(clkFast, [60, 24], binary: true);
        var clkEng1 = Wired(Ladder.Add(ca1, cb1, cs1, Plane.Ones(2)).Wirings);
        var clkEng2 = Wired(Ladder.Add(ca2, cb2, cs2, Plane.Ones(2)).Wirings);
        ca1.Restrict(0, 23 * 60 + 59); cb1.Restrict(0, 1); ca1.Restrict(1, 10 * 60 + 45); cb1.Restrict(1, 20);
        ca2.Restrict(0, 23 * 60 + 59); cb2.Restrict(0, 1); ca2.Restrict(1, 10 * 60 + 45); cb2.Restrict(1, 20);

        var clkPlan = CompiledPlan.Compile(clkEng2);
        Assert.True(clkPlan.HasRelationKernel);
        Assert.Equal(clkEng1.Settle(8), clkPlan.Settle(8, out var clkRep));
        Assert.True(clkRep.UsedFastPath);
        Assert.Equal(0L, cs2.Read(0));
        Assert.Equal(11 * 60 + 5L, cs2.Read(1));
        Assert.Equal(Transport.Save(clkEng1), Transport.Save(clkEng2));

        // 3. Shared-site diagonal Ladder.Add(x, x, doubled): general relation kernel filters SameReference tuples and accelerates!
        var diagCanonStore = new Store(0, 4);
        var diagFastStore = new Store(0, 4);
        static (Engine Eng, Ladder X, Ladder Doubled) BuildDiag(Store st)
        {
            var x = new Ladder(st, [2, 8]).AtUnitRung(1);
            var d = new Ladder(st, [2, 8]).AtUnitRung(1);
            var (wirings, carries) = Ladder.Add(x, x, d, Plane.Ones(4));
            var eng = Wired(wirings);
            x.Measure(0, 3, rung: 1);
            d.Restrict(1, 12); // backward halving with no overflow: 2x = 12 -> x = 6
            carries[^1].Restrict(1, 1UL << 0);
            d.Restrict(2, 5);  // odd doubled -> impossible (contradiction)
            return (eng, x, d);
        }
        var dCanon = BuildDiag(diagCanonStore);
        var dFast = BuildDiag(diagFastStore);
        var diagPlan = CompiledPlan.Compile(dFast.Eng);
        Assert.True(diagPlan.HasRelationKernel);
        var dcStep = dCanon.Eng.Step();
        var dfStep = diagPlan.Step(out var diagRep);
        Assert.True(diagRep.UsedFastPath);
        Assert.Equal(0, diagRep.DeoptLanes);
        for (var i = 0; i < dcStep.Steps.Count; i++)
        {
            Assert.Equal(dcStep.Steps[i].Changed, dfStep.Steps[i].Changed);
            Assert.Equal(dcStep.Steps[i].Unadmitted, dfStep.Steps[i].Unadmitted);
            Assert.Equal(dcStep.Steps[i].Ambiguous, dfStep.Steps[i].Ambiguous);
            Assert.Equal(dcStep.Steps[i].Waves, dfStep.Steps[i].Waves);
        }
        Assert.Equal(dCanon.Eng.Settle(8), diagPlan.Settle(8, out var diagSettleRep));
        Assert.True(diagSettleRep.UsedFastPath);
        Assert.Equal([1UL << 0, (1UL << 6) | (1UL << 7)], dFast.Doubled.Open(0));
        Assert.Equal(6L, dFast.X.Read(1));
        Assert.Null(dFast.X.Interval(2));
        Assert.Equal(Transport.Save(dCanon.Eng), Transport.Save(dFast.Eng));
    }

    [Fact]
    public void GeneralRelationKernelCoversPortionsProductsOdvSidesAndValueGap()
    {
        // 1. Portions.AddSameWhole(5) — non-ladder 5-role relation, including single-wiring together: true.
        var pCanon = new Store(0, 6);
        var pFast = new Store(0, 6);
        static Engine BuildPortion(Store st)
        {
            var s = Shape.Unary(5);
            var wShape = Shape.Unary(2);
            Supports n1 = Open(st, s), n2 = Open(st, s), d = Open(st, s), whole = Open(st, wShape), rem = Open(st, s);
            n1.Restrict(0, 1UL << 5); n2.Restrict(0, 1UL << 5); d.Restrict(0, 1UL << 5); // 5/5 + 5/5 = 2 + 0/5
            n1.Restrict(1, 1UL << 3); n2.Restrict(1, 1UL << 4); d.Restrict(1, 1UL << 5); // 3/5 + 4/5 = 1 + 2/5
            n1.Restrict(2, (1UL << 2) | (1UL << 3)); n2.Restrict(2, 1UL << 2); d.Restrict(2, 1UL << 5); // open
            n1.Restrict(3, 1UL << 1); d.Restrict(3, 1UL << 0); // zero denominator -> contradiction
            var wiring = new Wiring(Portions.AddSameWhole(5), n1.Variable, n2.Variable, d.Variable, whole.Variable, rem.Variable);
            for (var i = 0; i < 6; i++) wiring.Add(i, i, i, i, i);
            return new Engine().Add(wiring);
        }
        var pe1 = BuildPortion(pCanon);
        var pe2 = BuildPortion(pFast);
        var pPlan = CompiledPlan.Compile(pe2);
        Assert.True(pPlan.HasRelationKernel);
        var ps1 = pe1.StepTogether();
        var ps2 = pPlan.Step(out var pRep, together: true);
        Assert.True(pRep.UsedFastPath);
        Assert.Equal(6, pRep.FastLanes);
        Assert.Equal(ps1.Steps[0].Changed, ps2.Steps[0].Changed);
        Assert.Equal(ps1.Steps[0].Unadmitted, ps2.Steps[0].Unadmitted);
        Assert.Equal(ps1.Steps[0].Ambiguous, ps2.Steps[0].Ambiguous);
        Assert.Equal(ps1.Steps[0].Contested, ps2.Steps[0].Contested);
        Assert.Equal(Transport.Save(pe1), Transport.Save(pe2));

        // 2. Operations.Product with shared-site diagonal (squaring / square root).
        var sqCanon = new Store(0, 4);
        var sqFast = new Store(0, 4);
        static Engine BuildSquare(Store st)
        {
            var factor = Shape.Unary(4);
            var prod = Shape.Unary(16);
            Supports a = Open(st, factor), p = Open(st, prod);
            p.Restrict(0, 1UL << 9); // x^2 = 9 -> x = 3
            p.Restrict(1, 1UL << 6); // x^2 = 6 -> contradiction
            a.Restrict(2, (1UL << 2) | (1UL << 3)); // x in {2,3} -> p in {4,9}
            var w = new Wiring(Operations.Product(4, 4), a.Variable, a.Variable, p.Variable);
            for (var i = 0; i < 4; i++) w.Add(i, i, i);
            return new Engine().Add(w);
        }
        var sq1 = BuildSquare(sqCanon);
        var sq2 = BuildSquare(sqFast);
        var sqPlan = CompiledPlan.Compile(sq2);
        Assert.True(sqPlan.HasRelationKernel);
        var sqs1 = sq1.Step();
        var sqs2 = sqPlan.Step(out var sqRep);
        Assert.True(sqRep.UsedFastPath);
        Assert.Equal(sqs1.Steps[0].Unadmitted, sqs2.Steps[0].Unadmitted);
        Assert.Equal(sqs1.Steps[0].Ambiguous, sqs2.Steps[0].Ambiguous);
        Assert.Equal(Transport.Save(sq1), Transport.Save(sq2));

        // 3. Odv.Addition with mixed plain (constant O + variable V, D) and open roles.
        var odvCanon = new Store(0, 4);
        var odvFast = new Store(0, 4);
        static Engine BuildOdv(Store st)
        {
            var oPlane = st.AddConstantPlane(true); // forward face O = 1
            var vPlane = st.AddPlane();
            var dPlane = st.AddPlane();
            // Position 0: R+? (111); Position 1: R+0 (110); Position 2: R-? (101); Position 3: R-0 (100)
            st.Write(vPlane, 0, true); st.Write(dPlane, 0, true);
            st.Write(vPlane, 1, false); st.Write(dPlane, 1, true);
            st.Write(vPlane, 2, true); st.Write(dPlane, 2, false);
            st.Write(vPlane, 3, false); st.Write(dPlane, 3, false);
            var bSup = Open(st, Odv.Word);
            var cSup = Open(st, Odv.Word);
            bSup.Restrict(0, 1UL << 7); // R+?
            bSup.Restrict(2, 1UL << 7); // R+?
            var w = new Wiring(Odv.Addition(), new Binding(st, vPlane, dPlane, oPlane), bSup.Variable, cSup.Variable);
            for (var i = 0; i < 4; i++) w.Add(i, i, i);
            return new Engine().Add(w);
        }
        var od1 = BuildOdv(odvCanon);
        var od2 = BuildOdv(odvFast);
        var odPlan = CompiledPlan.Compile(od2);
        Assert.True(odPlan.HasRelationKernel);
        var ods1 = od1.Step();
        var ods2 = odPlan.Step(out var odRep);
        Assert.True(odRep.UsedFastPath);
        Assert.Equal(ods1.Steps[0].Changed, ods2.Steps[0].Changed);
        Assert.Equal(ods1.Steps[0].Ambiguous, ods2.Steps[0].Ambiguous);
        Assert.Equal(Transport.Save(od1), Transport.Save(od2));

        // 4. Sides.Agree and Settling.ValueGap also accelerate through the same general relation kernel.
        var gCanon = new Store(0, 4);
        var gFast = new Store(0, 4);
        static Engine BuildGap(Store st)
        {
            Supports left = Open(st, Shape.Binary(4)), right = Open(st, Shape.Binary(4)), dis = Open(st, Shape.Bit);
            left.Restrict(0, 1UL << 1); right.Restrict(0, 1UL << 2); // disjoint -> dis = 1
            left.Restrict(1, (1UL << 1) | (1UL << 2)); right.Restrict(1, (1UL << 2) | (1UL << 3)); dis.Restrict(1, 1UL << 0); // dis=0 -> intersect to {2}
            var w = new Wiring(Settling.ValueGap(Shape.Binary(4)), left.Variable, right.Variable, dis.Variable);
            for (var i = 0; i < 4; i++) w.Add(i, i, i);
            return new Engine().Add(w);
        }
        var ge1 = BuildGap(gCanon);
        var ge2 = BuildGap(gFast);
        var gPlan = CompiledPlan.Compile(ge2);
        Assert.True(gPlan.HasRelationKernel);
        Assert.Equal(ge1.Settle(4), gPlan.Settle(4, out var gRep));
        Assert.True(gRep.UsedFastPath);
        Assert.Equal(Transport.Save(ge1), Transport.Save(ge2));
    }

    [Fact]
    public void GeneralTransitionKernelAcceleratesSmallConvertShortfallRippleAndMultiplyTables()
    {
        // 1. Small Flows.Convert(2, 3, 9, 9) (100 states <= 512) accelerates via the general transition kernel!
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
        var e1 = new Engine().Add(Tension.Route(convert, src1.On(cStore1), dst1.On(cStore1), Plane.Ones(lanes)));
        var e2 = new Engine().Add(Tension.Route(convert, src2.On(cStore2), dst2.On(cStore2), Plane.Ones(lanes)));
        var plan = CompiledPlan.Compile(e2);
        Assert.True(plan.HasTransitionKernel);
        Assert.False(plan.HasRelationKernel);
        Assert.Equal(e1.Settle(2), plan.Settle(2, out var repPartial));
        Assert.True(repPartial.UsedFastPath);
        Assert.Equal(e1.Tick, e2.Tick);
        Assert.Equal(Transport.Save(e1), Transport.Save(e2));
        Assert.Equal(e1.Settle(10), plan.Settle(10, out var repFull));
        Assert.True(repFull.UsedFastPath);
        Assert.Equal(Transport.Save(e1), Transport.Save(e2));

        // 2. Counts.Shortfall(3, 3) accelerates in both ordered and taken-together modes.
        var sStore1 = new Store(0, 128);
        var sStore2 = new Store(0, 128);
        Field h1 = Allocate(sStore1, Shape.Unary(3)), wt1 = Allocate(sStore1, Shape.Unary(3)), sh1 = Allocate(sStore1, Shape.Unary(3));
        Field h2 = Allocate(sStore2, Shape.Unary(3)), wt2 = Allocate(sStore2, Shape.Unary(3)), sh2 = Allocate(sStore2, Shape.Unary(3));
        for (var i = 0; i < 128; i++)
        {
            h1.Write(sStore1, i, i % 4); wt1.Write(sStore1, i, (i / 4) % 4);
            h2.Write(sStore2, i, i % 4); wt2.Write(sStore2, i, (i / 4) % 4);
        }
        var se1 = new Engine().Add(Tension.Wire(Counts.Shortfall(3, 3), [h1.On(sStore1), wt1.On(sStore1), sh1.On(sStore1)], Plane.Ones(128)));
        var se2 = new Engine().Add(Tension.Wire(Counts.Shortfall(3, 3), [h2.On(sStore2), wt2.On(sStore2), sh2.On(sStore2)], Plane.Ones(128)));
        var sPlan = CompiledPlan.Compile(se2);
        Assert.True(sPlan.HasTransitionKernel);
        var st1 = se1.StepTogether();
        var st2 = sPlan.Step(out var sRep, together: true);
        Assert.True(sRep.UsedFastPath);
        Assert.Equal(128, sRep.FastLanes);
        Assert.Equal(st1.Steps[0].Changed, st2.Steps[0].Changed);
        Assert.Equal(st1.Steps[0].Contested, st2.Steps[0].Contested);
        Assert.Equal(Transport.Save(se1), Transport.Save(se2));

        // 3. Concrete [2, 5] ripple adder (rung 0 has 32 states, rung 1 has 500 states <= 512) runs through the same general transition kernel!
        static Engine BuildRipple(Store store, int count)
        {
            var radices = new[] { 2, 5 };
            var a = radices.Select(r => Allocate(store, Numbers.Digit(r, binary: true))).ToArray();
            var b = radices.Select(r => Allocate(store, Numbers.Digit(r, binary: true))).ToArray();
            var sum = radices.Select(r => Allocate(store, Numbers.Digit(r, binary: true))).ToArray();
            var cin = store.AddPlane();
            var c0 = store.AddPlane();
            var c1 = store.AddPlane();
            for (var x = 0; x < 10; x++)
                for (var y = 0; y < 10; y++)
                {
                    var lane = x * 10 + y;
                    if (lane >= count) break;
                    a[0].Write(store, lane, x % 2); a[1].Write(store, lane, x / 2);
                    b[0].Write(store, lane, y % 2); b[1].Write(store, lane, y / 2);
                }
            var eng = new Engine();
            for (var i = 0; i < 2; i++)
            {
                var d = Numbers.Digit(radices[i], binary: true);
                var law = Operations.Bind(Numbers.DigitSum(radices[i], binary: true).Relation,
                    [d, d, Numbers.Carry, Numbers.Carry, d], [0, 1, 2, -1, -1], [-1, -1, -1, 3, 4]);
                var w = new Wiring(law, a[i].On(store), b[i].On(store),
                    new Binding(store, i == 0 ? cin : c0), new Binding(store, i == 0 ? c0 : c1), sum[i].On(store));
                for (var lane = 0; lane < count; lane++) w.Add(lane, lane, lane, lane, lane);
                eng.Add(w);
            }
            return eng;
        }
        var rCanon = BuildRipple(new Store(0, 100), 100);
        var rFast = BuildRipple(new Store(0, 100), 100);
        var rPlan = CompiledPlan.Compile(rFast);
        Assert.True(rPlan.HasTransitionKernel);
        Assert.Equal(rCanon.Settle(8), rPlan.Settle(8, out var rRep));
        Assert.True(rRep.UsedFastPath);
        Assert.Equal(Transport.Save(rCanon), Transport.Save(rFast));

        // Multi-wiring taken-together falls back cleanly to exact 1-rung-per-tick Engine.StepTogether.
        var rtCanon = BuildRipple(new Store(0, 100), 100);
        var rtFast = BuildRipple(new Store(0, 100), 100);
        var rtPlan = CompiledPlan.Compile(rtFast);
        Assert.Equal(rtCanon.Settle(8, together: true), rtPlan.Settle(8, out var rtRep, together: true));
        Assert.False(rtRep.UsedFastPath);
        Assert.Equal(Transport.Save(rtCanon), Transport.Save(rtFast));

        // 4. Bounded lossless multiplier (480 states <= 512) also runs through the same general transition kernel!
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
        Assert.True(mPlan.HasTransitionKernel);
        Assert.Equal(me1.Settle(4), mPlan.Settle(4, out var mRep));
        Assert.True(mRep.UsedFastPath);
        Assert.Equal(Transport.Save(me1), Transport.Save(me2));
    }

    [Fact]
    public void OversizedRecordedBrokenDynamicAndStaleCasesFallBackCleanlyWithExactParity()
    {
        // 1. Relation exceeding the admitted-tuple ceiling (> 8,192 admitted tuples) falls back cleanly to Engine.
        var bigRel = RelationLaw.FromPredicate(
            [Shape.Binary(16), Shape.Binary(16), Shape.Binary(16), Shape.Binary(4)],
            t => t[3] < 3); // 16 * 16 * 16 * 3 = 12,288 > 8,192 admitted tuples
        var bigStore1 = new Store(0, 2);
        var bigStore2 = new Store(0, 2);
        static Engine BuildBig(Store st, RelationLaw rel)
        {
            Supports a = Open(st, Shape.Binary(16)), b = Open(st, Shape.Binary(16)),
                c = Open(st, Shape.Binary(16)), d = Open(st, Shape.Binary(4));
            a.Restrict(0, 1UL << 5); b.Restrict(0, 1UL << 7); c.Restrict(0, 1UL << 2);
            var w = new Wiring(rel, a.Variable, b.Variable, c.Variable, d.Variable);
            w.Add(0, 0, 0, 0);
            w.Add(1, 1, 1, 1);
            return new Engine().Add(w);
        }
        var big1 = BuildBig(bigStore1, bigRel);
        var big2 = BuildBig(bigStore2, bigRel);
        var bigPlan = CompiledPlan.Compile(big2);
        Assert.False(bigPlan.HasAnyCompiledKernel);
        Assert.Equal(big1.Settle(4), bigPlan.Settle(4, out var bigRep));
        Assert.False(bigRep.UsedFastPath);
        Assert.Equal(Transport.Save(big1), Transport.Save(big2));

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

        // 3. Broken unary code (010) on transition AND on plain relation sample triggers runtime deopt reporting Unread identically.
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
        Assert.True(brkPlan.HasTransitionKernel);
        var bs1 = be1.Step();
        var bs2 = brkPlan.Step(out var brkRep);
        Assert.False(brkRep.UsedFastPath);
        Assert.True(bs2.Steps[0].Unread[0]);
        Assert.Equal(bs1.Steps[0].Unread, bs2.Steps[0].Unread);
        Assert.Equal(bs1.Steps[0].Changed, bs2.Steps[0].Changed);
        Assert.Equal(Transport.Save(be1), Transport.Save(be2));

        var relBrk1 = new Store(0, 2);
        var relBrk2 = new Store(0, 2);
        static Engine BuildRelBroken(Store st)
        {
            var plain = Allocate(st, Shape.Unary(3));
            st.Write(plain.Planes[1], 0, true); // broken 010
            plain.Write(st, 1, 2);
            var open = Open(st, Shape.Unary(3));
            var w = new Wiring(Settling.Same(Shape.Unary(3)), plain.On(st), open.Variable);
            w.Add(0, 0); w.Add(1, 1);
            return new Engine().Add(w);
        }
        var rbe1 = BuildRelBroken(relBrk1);
        var rbe2 = BuildRelBroken(relBrk2);
        var rbePlan = CompiledPlan.Compile(rbe2);
        Assert.True(rbePlan.HasRelationKernel);
        var rbs1 = rbe1.Step();
        var rbs2 = rbePlan.Step(out var rbeRep);
        Assert.False(rbeRep.UsedFastPath);
        Assert.True(rbs2.Steps[0].Unread[0]);
        Assert.Equal(rbs1.Steps[0].Unread, rbs2.Steps[0].Unread);
        Assert.Equal(rbs1.Steps[0].Changed, rbs2.Steps[0].Changed);
        Assert.Equal(Transport.Save(rbe1), Transport.Save(rbe2));

        // 4. Stale invalidation across structural edits: toggling attention, adding/recruiting store positions.
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

        st.Write(fa.Planes[0], 1, true);
        w.Rows.Write(att, 1, false);
        plan = CompiledPlan.Compile(eng);
        Assert.True(plan.IsCurrent);
        plan.Step(out var attRep);
        Assert.True(attRep.UsedFastPath);
        Assert.Equal(1, attRep.FastLanes);
        Assert.Equal(1, fa.Read(st, 1));

        var sparePos = st.AddSpare(1);
        Assert.False(plan.IsCurrent);
        plan = CompiledPlan.Compile(eng);
        Assert.True(plan.IsCurrent);
        st.Recruit(sparePos);
        Assert.False(plan.IsCurrent);

        // 5. Dynamic-topology engine (writing another wiring's Rows) never compiles and falls back cleanly.
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
        Assert.Equal(1, rwA.Read(rwSt, 0));
        Assert.Equal(0, rwB.Read(rwSt, 0));
    }

    [Fact]
    public void PassThreeAndFourAddNoEnumsToEncodingC()
    {
        var enums = typeof(Store).Assembly.GetTypes()
            .Where(t => t.Namespace == typeof(Store).Namespace && t.IsEnum)
            .Select(t => t.FullName!)
            .ToArray();

        // Only master's LearnedRead exists; CompiledPlan, Portions, and Pass 1–4 add zero enums.
        Assert.Equal([typeof(LearnedRead).FullName!], enums);
    }
}
