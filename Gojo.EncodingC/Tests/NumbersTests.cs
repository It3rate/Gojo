using Xunit;

namespace Gojo.EncodingC.Tests;

/// <summary>Minimal multi-rung numbers layered above pre-numeric EncodingC:
/// Operations.Balance/Bind core, radix 2/4/8/10 add/subtract/multiply up to
/// 3-digit base-10 (0..999) far beyond Law.MaxBits, pre-numeric Rung 0
/// coexistence, two-rung coasting (B13), coupled relational narrowing + Fork
/// splitting (B2), unsigned live-landmark oscillators (B5), and compiled ALU /
/// oscillator parity.</summary>
public sealed class NumbersTests
{
    private static readonly Plane Here = Plane.Of(1, 0);

    [Fact]
    public void OperationsBalanceAndBindReproduceCountsWithExplicitFreeRolePolicyAndBlockedBit()
    {
        // Master's Counts laws match Operations.Bind(Operations.Balance(...)) bit-for-bit!
        Shape s3 = Shape.Unary(3), s6 = Shape.Unary(6), s9 = Shape.Unary(9);
        Assert.True(Counts.Sum(3, 3, weight: 2).SameTable(
            Operations.Bind(Operations.Balance([s3, s3, s9], 1, 2, -1), [s3, s3, s9], [0, 1, -1], [-1, -1, 2])));

        Assert.True(Counts.Shortfall(3, 3).SameTable(
            Operations.Bind(Operations.Balance([s3, s3, s3, s3], 1, -1, 1, -1), [s3, s3, s3], [0, 1, -1, -1], [-1, -1, 2, -1])));

        Assert.True(Counts.Accumulate(3).SameTable(
            Operations.Bind(Operations.Balance([s3, Shape.Bit, s3, Shape.Bit], 1, 1, -1, -1), [Shape.Bit, s3], [1, 0, -1, -1], [-1, -1, 1, -1])));

        // Explicit FreeRolePolicy (ops_sol catch): LeastTotal settles shortfall functionally;
        // AllAdmitted leaves the extra free-role choices open!
        var gap = Operations.Balance([s3, s3, s3, s3], 1, -1, 1, -1);
        var settled = Operations.Bind(gap, [s3, s3, s3], [0, 1, -1, -1], [-1, -1, 2, -1], policy: FreeRolePolicy.LeastTotal);
        var open = Operations.Bind(gap, [s3, s3, s3], [0, 1, -1, -1], [-1, -1, 2, -1], policy: FreeRolePolicy.AllAdmitted);
        Assert.True(settled.IsFunctional);
        Assert.False(open.IsFunctional);

        // Optional blocked bit tells quiet-because-met (0 -> 0) from quiet-because-blocked (5 -> 40 -> held)!
        var c40 = Shape.Unary(40);
        var doubled = Operations.Bind(Operations.Balance([c40, c40], 2, -1), [c40, Shape.Bit], [0, -1], [-1, 0], blocked: 1);
        foreach (var (start, expected, expectBlocked) in new[] { (5, 40, true), (0, 0, false) })
        {
            var st = new Store(0, 1);
            var b = new Field(c40, [.. Enumerable.Range(0, 40).Select(_ => st.AddPlane())]);
            var blk = st.AddPlane();
            b.Write(st, 0, start);
            var eng = new Engine().Add(Tension.Wire(doubled, [b.On(st), new Binding(st, blk)], Here));
            Assert.NotNull(eng.Settle(10));
            Assert.Equal(expected, b.Read(st, 0));
            Assert.Equal(expectBlocked, st.Read(blk, 0));
        }
    }

    [Fact]
    public void ThreeDigitBase10AdderAndSubtractorReuseOneBalanceRelationAndCompile()
    {
        // One single DigitAddRelation(10) (4,000 bits = 500 bytes!) powers BOTH 3-digit
        // base-10 addition (0..999 + 0..999) AND 3-digit base-10 subtraction!
        Assert.Equal(4000, Numbers.DigitAddRelation(10).BitCount);

        const int Lanes = 4;
        (int A, int B)[] addCases = [(378, 465), (750, 480), (999, 1), (0, 0)];

        static (Engine Engine, Store Store, RungNumber A, RungNumber B, RungNumber Sum, int Cout) BuildBase10Adder((int A, int B)[] cases)
        {
            var st = new Store(0, cases.Length);
            var a = new RungNumber(st, 10, 3);
            var b = new RungNumber(st, 10, 3);
            var sum = new RungNumber(st, 10, 3);
            var cin = st.AddPlane();
            var cout = st.AddPlane();
            for (var i = 0; i < cases.Length; i++)
            {
                a.Write(i, cases[i].A);
                b.Write(i, cases[i].B);
            }
            var eng = new Engine();
            Numbers.WireAdder(eng, a, b, sum, cin, cout, Plane.Ones(cases.Length));
            return (eng, st, a, b, sum, cout);
        }

        // Save via Transport and restore so compilation proves content-based recognition of split base-10 adders!
        var built = BuildBase10Adder(addCases);
        var saved = Transport.Save(built.Engine, built.Store);
        var (canonEng, canonStores) = Transport.Load(saved);
        var (fastEng, fastStores) = Transport.Load(saved);

        var plan = CompiledPlan.Compile(fastEng);
        Assert.True(plan.RecognizedRadixAdder);

        var rep = plan.RunStep();
        canonEng.Step();
        Assert.True(rep.UsedFastPath);
        Assert.Equal(Lanes, rep.FastLanes);
        Assert.Equal(canonEng.Tick, fastEng.Tick);

        for (var i = 0; i < Lanes; i++)
        {
            var total = addCases[i].A + addCases[i].B;
            Assert.Equal((long)(total % 1000), built.Sum.Read(fastStores[0], i));
            Assert.Equal(total >= 1000, fastStores[0].Read(built.Cout, i));
        }
        for (var p = 0; p < canonStores[0].PlaneCount; p++)
            Assert.Equal(canonStores[0].Read(p), fastStores[0].Read(p));

        // Reverse direction of the EXACT SAME DigitAddRelation(10): 3-digit base-10 subtraction!
        var subStore = new Store(0, 3);
        var min = new RungNumber(subStore, 10, 3);
        var sub = new RungNumber(subStore, 10, 3);
        var diff = new RungNumber(subStore, 10, 3);
        var bin = subStore.AddPlane();
        var bout = subStore.AddPlane();
        min.Write(0, 843); sub.Write(0, 465); // 378, borrow 0
        min.Write(1, 500); sub.Write(1, 1);   // 499, borrow 0
        min.Write(2, 120); sub.Write(2, 350); // 770 (mod 1000), borrow 1 (negative!)

        var subEng = new Engine();
        Numbers.WireSubtractor(subEng, min, sub, diff, bin, bout, Plane.Ones(3));
        subEng.Step();

        Assert.Equal((378L, false), (diff.Read(0), subStore.Read(bout, 0)));
        Assert.Equal((499L, false), (diff.Read(1), subStore.Read(bout, 1)));
        Assert.Equal((770L, true), (diff.Read(2), subStore.Read(bout, 2)));
    }

    [Fact]
    public void MultiDigitBase10MultiplierComputesProductsFarBeyondDenseLawMaxBits()
    {
        // 2-digit base-10 × 2-digit base-10 -> 4-digit base-10 (0..9,801)
        // A dense table for 0..99 × 0..99 -> 0..9801 would need 98,020,000 bits (> 23× Law.MaxBits);
        // the rung lattice uses tiny shared DigitMulLo/Hi + DigitSum/Carry tables!
        (int A, int B)[] pairs = [(27, 36), (17, 28), (99, 99), (0, 73), (12, 10)];
        var st = new Store(0, pairs.Length);
        var a = new RungNumber(st, 10, 2);
        var b = new RungNumber(st, 10, 2);
        var prod = new RungNumber(st, 10, 4);
        for (var i = 0; i < pairs.Length; i++)
        {
            a.Write(i, pairs[i].A);
            b.Write(i, pairs[i].B);
        }

        var eng = new Engine();
        Numbers.WireMultiplier(eng, a, b, prod, Plane.Ones(pairs.Length));
        eng.Step();

        for (var i = 0; i < pairs.Length; i++)
            Assert.Equal((long)pairs[i].A * pairs[i].B, prod.Read(i));
    }

    [Fact]
    public void PreNumericRung0WorksUnchangedBesideMultiRungNumbersAndTwoRungCoasting()
    {
        // 1. Pre-numeric Rung 0: a 1-rung unary RungNumber IS an ordinary unary Field compatible with Tension & Flows!
        var preStore = new Store(0, 1);
        var preNum = new RungNumber(preStore, radix: 5, rungs: 1, unary: true);
        var site = new Tension(preStore, preNum.Digits[0], 4);
        var preEng = new Engine().Add(site.Absorb(Here));
        for (var i = 0; i < 3; i++) Assert.True(site.Receive(Here).Blocked.None);
        Assert.NotNull(preEng.Settle(6));
        Assert.Equal(3L, preNum.Read(0));

        // 2. Two-rung coasting (ops_claude B13): rate=3, ticks=4 => 12 delivered via radix-4 rungs (ones + fours).
        // Each tick books 3 into ones-rung; conserving Flows.Convert(4, 1) carries groups of 4 into the high rung
        // so bulk delivery transfers whole 4-unit packets in 3 steps instead of 12 unary steps!
        var st = new Store(0, 1);
        Field U(int cap, int val = 0)
        {
            var f = new Field(Shape.Unary(cap), [.. Enumerable.Range(0, cap).Select(_ => st.AddPlane())]);
            f.Write(st, 0, val);
            return f;
        }
        var ticksLeft = U(4, 4);
        var elapsed = U(4, 0);
        var rate = U(3, 3);
        var loOwed = U(7, 0); // ones rung (carries 4 -> 1 into hiOwed)
        var hiOwed = U(4, 0); // fours rung (each unit = 4!)
        var stockPacks = U(4, 4); // 4 packs of 4 = 16 units stock
        var deliveredPacks = U(4, 0);

        // Book rate into loOwed while ticksLeft > 0; carry 4->1 from loOwed into hiOwed; fulfill packs from stockPacks!
        var bookLo = Operations.Bind(
            Operations.Balance([Shape.Unary(7), Shape.Unary(3), Shape.Unary(7)], 1, 1, -1),
            [Shape.Unary(3), Shape.Unary(7)], [1, 0, -1], [-1, -1, 1]);
        var eng = new Engine()
            .Add(Tension.Wire(bookLo, [rate.On(st), loOwed.On(st)], Here, new Binding(st, ticksLeft.Planes[0])))
            .Add(Tension.Route(ticksLeft.On(st), elapsed.On(st), Here))
            .Add(Tension.Route(Flows.Convert(4, 1, 7, 4), loOwed.On(st), hiOwed.On(st), Here))
            .Add(Tension.Wire(Flows.Fulfill(4, 4, 4, take: 1, give: 1), [stockPacks.On(st), hiOwed.On(st), deliveredPacks.On(st)], Here));

        var steps = eng.Settle(10);
        Assert.NotNull(steps);
        Assert.True(steps <= 6); // Settled 12 units in <= 6 steps (vs 13+ steps unit-by-unit)!
        Assert.Equal(4, elapsed.Read(st, 0));
        Assert.Equal(0, loOwed.Read(st, 0));
        Assert.Equal(0, hiOwed.Read(st, 0));
        Assert.Equal(3, deliveredPacks.Read(st, 0)); // 3 × 4 = 12 units delivered!
        Assert.Equal(1, stockPacks.Read(st, 0));     // 1 × 4 = 4 remaining!
    }

    [Fact]
    public void CoupledRelationsNarrowMarginalsAndForkSuppositionsRecoverExactPairs()
    {
        // ops_claude B2: x + y = 5 and x * y = 6 over 0..5.
        // Narrowing keeps marginals {2, 3} for both x and y without saying which pairs with which;
        // Forking and supposing x = 2 settles (2, 3) and supposing x = 3 settles (3, 2)!
        var s5 = Shape.Binary(6);
        var s25 = Shape.Binary(26);
        var sumRel = Operations.Balance([s5, s5, s5], 1, 1, -1);
        var prodRel = Operations.Product(s5, s5, s25);

        var st = new Store(0, 1);
        Supports Open(Shape s) => Supports.On(st, new Field(s, [.. Enumerable.Range(0, s.Bits).Select(_ => st.AddPlane())]));
        var x = Open(s5);
        var y = Open(s5);
        var sum = Open(s5);
        var prod = Open(s25);
        sum.Restrict(0, 1UL << 5);
        prod.Restrict(0, 1UL << 6);

        var wSum = new Wiring(sumRel, x.Variable, y.Variable, sum.Variable); wSum.Add(0, 0, 0);
        var wProd = new Wiring(prodRel, x.Variable, y.Variable, prod.Variable); wProd.Add(0, 0, 0);
        var eng = new Engine().Add(wSum).Add(wProd);
        Assert.NotNull(eng.Settle(6));

        var twoOrThree = (1UL << 2) | (1UL << 3);
        Assert.Equal(twoOrThree, x.Values(0));
        Assert.Equal(twoOrThree, y.Values(0));

        // Fork 1: suppose x = 2 -> y settles to 3!
        var f1 = eng.Fork();
        var f1Store = f1.Of(st);
        var fx1 = new Supports(f1Store, x.Field, [.. x.Planes]);
        var fy1 = new Supports(f1Store, y.Field, [.. y.Planes]);
        fx1.Restrict(0, 1UL << 2);
        Assert.NotNull(f1.Engine.Settle(6));
        Assert.Equal(1UL << 3, fy1.Values(0));

        // Fork 2: suppose x = 3 -> y settles to 2!
        var f2 = eng.Fork();
        var f2Store = f2.Of(st);
        var fx2 = new Supports(f2Store, x.Field, [.. x.Planes]);
        var fy2 = new Supports(f2Store, y.Field, [.. y.Planes]);
        fx2.Restrict(0, 1UL << 3);
        Assert.NotNull(f2.Engine.Settle(6));
        Assert.Equal(1UL << 2, fy2.Values(0));

        // Original store remains untouched at {2, 3}.
        Assert.Equal(twoOrThree, x.Values(0));
    }

    [Fact]
    public void UnsignedLandmarkOscillatorDeduplicatesTablePerturbsCenterMidRunAndCompiles()
    {
        const int Track = 8;
        const int Mark = 4;

        // Kick and drift with weight 1 share the EXACT SAME table!
        var kick = Numbers.LandmarkShear(Track, weight: 1, withBlocked: true);
        var drift = Numbers.LandmarkShear(Track, weight: 1, withBlocked: true);
        Assert.True(kick.SameTable(drift));

        static (Engine Engine, Store Store, Field X, Field V, Field L, Field R, int Kb, int Db) Build(int x0, int v0, int lm, int rest, int k = 1)
        {
            var st = new Store(0, 1);
            Field U(int val)
            {
                var f = new Field(Shape.Unary(Track), [.. Enumerable.Range(0, Track).Select(_ => st.AddPlane())]);
                f.Write(st, 0, val);
                return f;
            }
            var fx = U(x0);
            var fv = U(v0);
            var fl = U(lm);
            var fr = U(rest);
            var kb = st.AddPlane();
            var db = st.AddPlane();
            var eng = new Engine()
                .Add(Tension.Wire(Numbers.LandmarkShear(Track, k, withBlocked: true), [fv.On(st), fl.On(st), fx.On(st), new Binding(st, kb)], Here))
                .Add(Tension.Wire(Numbers.LandmarkShear(Track, 1, withBlocked: true), [fx.On(st), fv.On(st), fr.On(st), new Binding(st, db)], Here));
            return (eng, st, fx, fv, fl, fr, kb, db);
        }

        // Verify compact wire size via Transport.Save (~3.7 KB with blocked bit, ~1.9 KB without — far below 97 KB signed!)
        var compact = Build(Mark + 2, Mark, Mark, Mark);
        var bytes = Transport.Save(compact.Engine, compact.Store);
        Assert.True(bytes.Length < 4500);

        // Compile the RESTORED unsigned oscillator and verify 18 steps (3 exact 6-step hexagon cycles) + mid-run landmark perturbation (B5)!
        var (canonEng, canonStores) = Transport.Load(bytes);
        var (fastEng, fastStores) = Transport.Load(bytes);
        var plan = CompiledPlan.Compile(fastEng);
        Assert.True(plan.RecognizedUnsignedLandmarkOscillator);

        for (var t = 0; t < 18; t++)
        {
            var rep = plan.RunStep();
            canonEng.Step();
            Assert.True(rep.UsedFastPath);
            Assert.Equal(canonEng.Tick, fastEng.Tick);
            for (var p = 0; p < canonStores[0].PlaneCount; p++)
                Assert.Equal(canonStores[0].Read(p), fastStores[0].Read(p));
        }
        // Closed exact 6-step cycle back to (Mark + 2, Mark)!
        Assert.Equal((Mark + 2, Mark), (compact.X.Read(fastStores[0], 0), compact.V.Read(fastStores[0], 0)));

        // Perturb the LIVE landmark count mid-run (B5): moving L from 4 -> 5 re-centers the orbit immediately!
        compact.L.Write(canonStores[0], 0, 5);
        compact.L.Write(fastStores[0], 0, 5);
        for (var t = 0; t < 6; t++)
        {
            var rep = plan.RunStep();
            canonEng.Step();
            Assert.True(rep.UsedFastPath);
            Assert.Equal(canonEng.Tick, fastEng.Tick);
            for (var p = 0; p < canonStores[0].PlaneCount; p++)
                Assert.Equal(canonStores[0].Read(p), fastStores[0].Read(p));
        }
        // Orbit around new center L=5 returned cleanly to starting point (6, 4) after 6 steps!
        Assert.Equal((6, 4), (compact.X.Read(fastStores[0], 0), compact.V.Read(fastStores[0], 0)));
    }
}
