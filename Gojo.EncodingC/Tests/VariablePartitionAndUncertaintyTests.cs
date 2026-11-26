using Xunit;

namespace Gojo.EncodingC.Tests;

/// <summary>Round 3 variable-partition numbers, five-star scale distinctions,
/// evidence pooling vs. same-whole addition, coarse-precision uncertainty
/// widening, mechanism-dependent uncertainty (stretch vs. independent sum,
/// same-site cancellation vs. independent difference), Axiom-8 projection
/// residue vs. material conservation, and generalized atomic no-overshoot
/// admission with SaveLocals continuity.</summary>
public sealed class VariablePartitionAndUncertaintyTests
{
    private static readonly Plane Here = Plane.Of(1, 0);

    private static Field Allocate(Store store, Shape shape) =>
        new(shape, [.. Enumerable.Range(0, shape.Bits).Select(_ => store.AddPlane())]);

    [Fact]
    public void FiveStarDomainsAreExactWithoutBinaryInflationAcrossOrdinalIntervalAndRatioScales()
    {
        // 1. Ratio 0..5 (5 intervals, 6 landmarks): Unary(5) allocates 5 planes (1 bit per star) + free Room!
        var unaryFive = Shape.Unary(5);
        Assert.Equal(6, unaryFive.Size);
        Assert.Equal(5, unaryFive.Bits);
        var st = new Store(0, 1);
        var stars = Allocate(st, unaryFive);
        stars.Write(st, 0, 3);
        Assert.Equal(3, stars.Read(st, 0));
        // Room (anti-fill) reads 5 - 3 = 2 stars short of a full 5/5 whole with zero extra planes!
        var roomBinding = stars.Room(st);
        var roomField = new Field(unaryFive, [.. roomBinding.Planes]);
        Assert.Equal(2, roomField.Read(roomBinding.Surface, 0));

        // 2. Binary-packed 0..5 (Shape.Binary(6)) uses 3 planes, yet Size == 6 (NEVER 8!):
        // codes 6 and 7 decode to null, Supports allocates exactly 6 planes, and Law indexes in exact mixed radix.
        var binSix = Shape.Binary(6);
        Assert.Equal(3, binSix.Bits);
        Assert.Equal(6, binSix.Size);
        Assert.Equal(5, binSix.Decode(5));
        Assert.Null(binSix.Decode(6));
        Assert.Null(binSix.Decode(7));
        var binSup = Supports.On(st, Allocate(st, binSix));
        Assert.Equal(6, binSup.Planes.Count);
        var sumTwoFive = RelationLaw.FromPredicate([binSix, binSix, Shape.Binary(11)], t => t[0] + t[1] == t[2]);
        Assert.Equal(6 * 6 * 11, sumTwoFive.BitCount); // 396 bits, NOT 8*8*16 = 1,024!

        // 3. Ordinal 1..5 scale: silence is open {1..5}, zero is outside scope (empty contradiction, distinct from open!).
        var ordStore = new Store(0, 2);
        var rating = Supports.On(ordStore, Allocate(ordStore, binSix));
        var ordinalScope = RelationLaw.FromPredicate([binSix], t => t[0] >= 1 && t[0] <= 5);
        var ordW = new Wiring(ordinalScope, rating.Variable);
        ordW.Add(0);
        ordW.Add(1);
        var ordEng = new Engine().Add(ordW);
        rating.Restrict(1, 1UL << 0); // explicit 0 is outside 1..5 ordinal scope
        Assert.NotNull(ordEng.Settle(4));
        Assert.Equal(0b111110UL, rating.Values(0)); // lane 0: silent -> genuinely open {1,2,3,4,5}
        Assert.Equal(0UL, rating.Values(1));        // lane 1: 0 -> empty contradiction (distinct from silence!)

        // 4. Interval scale (landmark at 1 star: 1..5 has 4 intervals, so 3 stars is 2/4 = 1/2)
        // vs. Ratio scale (0..5 has 5 intervals, so 3 stars is 3/5 > 1/2!).
        var cmpStore = new Store(0, 1);
        var s5 = Shape.Binary(6);
        Supports Sup(Shape sh, int? exact = null)
        {
            var sup = Supports.On(cmpStore, Allocate(cmpStore, sh));
            if (exact is int v) sup.Restrict(0, 1UL << v);
            return sup;
        }
        var cmpOrd = Sup(Shape.Binary(3));
        var cmpW = new Wiring(Portions.Compare(5),
            Sup(s5, 3).Variable, Sup(s5, 5).Variable, // 3/5 (ratio)
            Sup(s5, 2).Variable, Sup(s5, 4).Variable, // (3-1)/(5-1) = 2/4 (interval)
            cmpOrd.Variable);
        cmpW.Add(0, 0, 0, 0, 0);
        Assert.NotNull(new Engine().Add(cmpW).Settle(4));
        Assert.Equal(1UL << 2, cmpOrd.Values(0)); // 3/5 > 2/4 (order == 2)
    }

    [Fact]
    public void MixedPartitionLaddersRefineWithoutDoublingAndCompileWithBitSlicedSwar()
    {
        // 1. Half-stars out of 5: mixed partition [2, 5] anchored at top (UnitRung = 2, UnitScale = 10).
        // Exact domain 0.0..4.5 plus top carry-out whole bit for 5.0 (532 relation bits = 66.5 bytes, zero doubling!).
        var halfStore = new Store(0, 1);
        int[] halfParts = [2, 5];
        Ladder ha = new(halfStore, halfParts, unitRung: 2);
        Ladder hb = new(halfStore, halfParts, unitRung: 2);
        Ladder hs = new(halfStore, halfParts, unitRung: 2);
        Assert.Equal(10L, ha.UnitScale);
        Assert.Equal(9L, ha.Capacity);
        var (hWirings, hCarries) = Ladder.Add(ha, hb, hs, Here);
        var hEng = new Engine();
        foreach (var w in hWirings) hEng.Add(w);
        ha.Restrict(0, 7); // 3.5 stars (3 whole, 1 half)
        hb.Restrict(0, 3); // 1.5 stars (1 whole, 1 half)
        Assert.NotNull(hEng.Settle(8));
        Assert.Equal(0L, hs.Read(0));                    // remainder 0.0
        Assert.Equal(1UL << 1, hCarries[^1].Values(0));  // exact whole carry = 1 (5.0 / 5.0!)

        // Backward subtraction on the exact same [2, 5] wiring: 5.0 - 1.5 = 3.5!
        ha.Reopen(0); hb.Reopen(0); hs.Reopen(0);
        foreach (var c in hCarries) c.Forget(0);
        hCarries[0].Restrict(0, 1UL << 0);
        hb.Restrict(0, 3);
        hs.Restrict(0, 0);
        hCarries[^1].Restrict(0, 1UL << 1);
        Assert.NotNull(hEng.Settle(8));
        Assert.Equal(7L, ha.Read(0));

        // 2. Stars & fifths [5, 6] (Claude Round 14 check): 3 3/5 + 1 4/5 = 5 2/5 (18 + 9 = 27).
        var sfStore = new Store(0, 1);
        Ladder sfa = new(sfStore, [5, 6], unitRung: 1), sfb = new(sfStore, [5, 6], unitRung: 1), sfs = new(sfStore, [5, 6], unitRung: 1);
        var (sfW, sfC) = Ladder.Add(sfa, sfb, sfs, Here);
        var sfEng = new Engine();
        foreach (var w in sfW) sfEng.Add(w);
        sfa.Restrict(0, 3 * 5 + 3);
        sfb.Restrict(0, 1 * 5 + 4);
        Assert.NotNull(sfEng.Settle(8));
        Assert.Equal(5 * 5 + 2, sfs.Read(0));
        Assert.Equal(1UL << 0, sfC[^1].Values(0));

        // 3. Clock mm:ss [10, 6, 10, 6]: 00:59 + 00:01 = 01:00 exact!
        var clkStore = new Store(0, 1);
        int[] clkParts = [10, 6, 10, 6];
        Ladder ca = new(clkStore, clkParts, unary: false), cb = new(clkStore, clkParts, unary: false), cs = new(clkStore, clkParts, unary: false);
        var (clkW, _) = Ladder.Add(ca, cb, cs, Here);
        var clkEng = new Engine();
        foreach (var w in clkW) clkEng.Add(w);
        ca.Restrict(0, 59);
        cb.Restrict(0, 1);
        Assert.NotNull(clkEng.Settle(10));
        Assert.Equal(60L, cs.Read(0));

        // 4. Compiled 64-lane bit-sliced SWAR parity on mixed-partition [2, 5] (ALL 100 half-star pairs) after Transport!
        const int Pairs = 100;
        var st = new Store(0, Pairs);
        var la = new Ladder(st, [2, 5], unitRung: 2, unary: false);
        var lb = new Ladder(st, [2, 5], unitRung: 2, unary: false);
        var lsum = new Ladder(st, [2, 5], unitRung: 2, unary: false);
        var cin = st.AddPlane();
        var cout = st.AddPlane();
        for (var x = 0; x < 10; x++)
            for (var y = 0; y < 10; y++)
            {
                var lane = x * 10 + y;
                la.WriteConcrete(lane, x);
                lb.WriteConcrete(lane, y);
            }
        var eng = new Engine();
        Numbers.WireAdder(eng, la, lb, lsum, cin, cout, Plane.Ones(Pairs));
        var saved = Transport.Save(eng, st);
        var (canonEng, canonSt) = Transport.Load(saved);
        var (fastEng, fastSt) = Transport.Load(saved);

        var plan = CompiledPlan.Compile(fastEng);
        Assert.True(plan.RecognizedRadixAdder);
        var rep = plan.RunStep();
        canonEng.Step();
        Assert.True(rep.UsedFastPath);
        Assert.Equal(Pairs, rep.FastLanes);
        for (var x = 0; x < 10; x++)
            for (var y = 0; y < 10; y++)
            {
                var lane = x * 10 + y;
                var total = x + y;
                Assert.Equal((long)(total % 10), lsum.ReadConcrete(fastSt[0], lane));
                Assert.Equal(total >= 10, fastSt[0].Read(cout, lane));
            }
        for (var p = 0; p < canonSt[0].PlaneCount; p++)
            Assert.Equal(canonSt[0].Read(p), fastSt[0].Read(p));
    }

    [Fact]
    public void EvidencePoolingPreservesCertaintyAndDiffersFromSameWholeAddition()
    {
        // 1. Evidence pooling: 3/5 (+) 2/5 = 5/10 (two five-star reviews pooled: preserves BOTH 5 and 10, never reduces to 1/2!).
        var pSt = new Store(0, 2);
        var s10 = Shape.Binary(11);
        var s16 = Shape.Binary(16);
        Supports P(Shape sh) => Supports.On(pSt, Allocate(pSt, sh));
        var (n1, d1, n2, d2, nOut, dOut) = (P(s10), P(s10), P(s10), P(s10), P(s16), P(s16));
        var (nw, dw) = Portions.WirePool(n1, d1, n2, d2, nOut, dOut, Plane.Ones(2));
        // Lane 0: (3, 5) (+) (2, 5) = (5, 10)
        n1.Restrict(0, 1UL << 3); d1.Restrict(0, 1UL << 5);
        n2.Restrict(0, 1UL << 2); d2.Restrict(0, 1UL << 5);
        // Lane 1: (3, 5) (+) (7, 10) = (10, 15) (three five-star reviews pooled)
        n1.Restrict(1, 1UL << 3); d1.Restrict(1, 1UL << 5);
        n2.Restrict(1, 1UL << 7); d2.Restrict(1, 1UL << 10);

        Assert.NotNull(new Engine().Add(nw).Add(dw).Settle(4));
        Assert.Equal((1UL << 5, 1UL << 10), (nOut.Values(0), dOut.Values(0)));
        Assert.Equal((1UL << 10, 1UL << 15), (nOut.Values(1), dOut.Values(1)));

        // 2. Same-whole addition: 3/5 + 2/5 = 1 whole + 0/5; 4/5 + 3/5 = 1 whole + 2/5.
        var wSt = new Store(0, 3);
        var s6 = Shape.Binary(6);
        Supports W(Shape sh) => Supports.On(wSt, Allocate(wSt, sh));
        var (wa, wb, wd1, wd2, whole, rem) = (W(s6), W(s6), W(s6), W(s6), W(Shape.Bit), W(s6));
        var sameDen = new Wiring(Settling.Same(s6), wd1.Variable, wd2.Variable);
        var sameAdd = new Wiring(Portions.AddSameWhole(5), wa.Variable, wb.Variable, wd1.Variable, whole.Variable, rem.Variable);
        for (var lane = 0; lane < 3; lane++) { sameDen.Add(lane, lane); sameAdd.Add(lane, lane, lane, lane, lane); }

        // Lane 0: 3/5 + 2/5 -> whole=1, rem=0
        wa.Restrict(0, 1UL << 3); wb.Restrict(0, 1UL << 2); wd1.Restrict(0, 1UL << 5); wd2.Restrict(0, 1UL << 5);
        // Lane 1: 4/5 + 3/5 -> whole=1, rem=2
        wa.Restrict(1, 1UL << 4); wb.Restrict(1, 1UL << 3); wd1.Restrict(1, 1UL << 5); wd2.Restrict(1, 1UL << 5);
        // Lane 2: INCOMPATIBLE 3/5 + 2/3 through same-whole wiring -> empty support contradiction (refuses silent /15!)
        wa.Restrict(2, 1UL << 3); wb.Restrict(2, 1UL << 2); wd1.Restrict(2, 1UL << 5); wd2.Restrict(2, 1UL << 3);

        Assert.NotNull(new Engine().Add(sameDen).Add(sameAdd).Settle(8));
        Assert.Equal((1UL << 1, 1UL << 0), (whole.Values(0), rem.Values(0)));
        Assert.Equal((1UL << 1, 1UL << 2), (whole.Values(1), rem.Values(1)));
        Assert.Equal(0UL, wd1.Values(2)); // incompatible denominators visibly contradicted!
        Assert.Equal(0UL, rem.Values(2));
    }

    [Fact]
    public void CoarseFiveStarUncertaintyStaysOpenWidensOnSumAndDependsOnMechanism()
    {
        // 1. Optional-Socket coarse-to-fine publication on [2, 5] half-stars (ops_sol pattern):
        // Supplier holds 3.5 (coarse=3 stars, fine=1 half). Fine port sits behind an unplugged Socket!
        var supplier = new Store(0, 1);
        var supStar = Allocate(supplier, Numbers.Digit(5, unary: false));
        var supHalf = Allocate(supplier, Numbers.Digit(2, unary: false));
        supStar.Write(supplier, 0, 3);
        supHalf.Write(supplier, 0, 1);
        var coarsePort = supplier.Publish([0], [.. supStar.Planes]);
        var finePort = supplier.Publish([0], [.. supHalf.Planes]);
        new Engine().Hold(supplier);

        var receiver = new Store(0, 1);
        var fineSocket = new Socket(1, Numbers.Digit(2, unary: false).Bits);
        var seen = new Ladder(receiver, [2, 5], unitRung: 2, unary: false);
        var obsCoarse = new Wiring(Settling.Same(Numbers.Digit(5, unary: false)),
            new Binding(coarsePort, [.. Enumerable.Range(0, coarsePort.PlaneCount)]), seen.Possible[1].Variable);
        obsCoarse.Add(0, 0);
        var obsFine = new Wiring(Settling.Same(Numbers.Digit(2, unary: false)),
            new Binding(fineSocket, [.. Enumerable.Range(0, fineSocket.PlaneCount)]), seen.Possible[0].Variable);
        obsFine.Add(0, 0);
        var fineEvidence = obsFine.Record();
        var obsEng = new Engine().Add(obsCoarse).Add(obsFine);

        obsEng.Step();
        Assert.True(obsFine.Rows.Read(fineEvidence.Skipped, 0));     // missing fine port visibly Skipped
        Assert.Null(seen.Read(0));                                   // NOT falsely settled to 3.0!
        Assert.Equal([6L, 7L], seen.PossibleValues(0));              // genuinely open {3.0, 3.5}

        // Plugging the fine port narrows the exact same observation to 3.5 (7 half-stars) without draining supplier!
        fineSocket.Plug(finePort);
        obsEng.Step();
        Assert.False(obsFine.Rows.Read(fineEvidence.Skipped, 0));
        Assert.Equal(7L, seen.Read(0));
        Assert.False(coarsePort.IsTaken);
        Assert.False(finePort.IsTaken);

        // Changing supplier to 4.0 (coarse=4, fine=0) requires explicit Reopen before re-narrowing (ops_sol rule)!
        supStar.Write(supplier, 0, 4);
        supHalf.Write(supplier, 0, 0);
        seen.Reopen(0);
        obsEng.Step();
        Assert.Equal(8L, seen.Read(0));

        // 2. Adding TWO independent coarse ratings: 3/5 ({6,7} half-stars) + 2/5 ({4,5} half-stars).
        // Uncertainty widens honestly from width 2 to width 3 ({10, 11, 12} = {5.0, 5.5, 6.0}) across the whole carry!
        var addSt = new Store(0, 1);
        Ladder x = new(addSt, [2, 5], unitRung: 2, unary: false);
        Ladder y = new(addSt, [2, 5], unitRung: 2, unary: false);
        Ladder s = new(addSt, [2, 5], unitRung: 2, unary: false);
        var (addW, addC) = Ladder.Add(x, y, s, Here);
        var addEng = new Engine();
        foreach (var w in addW) addEng.Add(w);
        x.Restrict(0, 1, 1UL << 3); // coarse 3 stars, fine half-star OPEN {0, 1} -> {6, 7}
        y.Restrict(0, 1, 1UL << 2); // coarse 2 stars, fine half-star OPEN {0, 1} -> {4, 5}
        Assert.NotNull(addEng.Settle(8));
        Assert.Equal(1UL << 1, addC[^1].Values(0));          // carry guaranteed 1 (total >= 5.0)
        // Marginally per rung, both s[0] (halves) and s[1] (stars) stay conservatively open ({0, 1} × {0, 1}):
        Assert.Equal([0b11UL, 0b11UL], s.Open(0));
        // Jointly through the coupled carry addC[1], probing each value in a Fork separates the exact
        // surviving joint set {0, 1, 2} ({5.0, 5.5, 6.0}) from the jointly impossible corner 3 (6.5 -> empty carry!):
        var jointSurviving = new List<long>();
        foreach (var cand in s.PossibleValues(0))
        {
            var probe = addEng.Fork();
            var pSurface = probe.Engine.Wirings[0].Bindings[0].Surface;
            var pS0 = s.Possible[0].In(pSurface);
            var pS1 = s.Possible[1].In(pSurface);
            var pC1 = addC[1].In(pSurface);
            pS0.Restrict(0, 1UL << (int)(cand % 2));
            pS1.Restrict(0, 1UL << (int)(cand / 2));
            probe.Engine.Settle(8);
            if (pS0.Values(0) != 0 && pS1.Values(0) != 0 && pC1.Values(0) != 0) jointSurviving.Add(cand);
        }
        Assert.Equal([0L, 1L, 2L], jointSurviving);          // exact joint remainder {0.0, 0.5, 1.0} -> {5.0, 5.5, 6.0}!

        // 3. MECHANISM CHANGES UNCERTAINTY:
        //    (a) Stretching ONE coarse 3/5 sample x in {6, 7} by 2 (2·x via shared-site identity) -> {12, 14} (skips 13!).
        //    (b) Summing TWO independent coarse 3/5 samples x1, x2 in {6, 7} -> {12, 13, 14} (includes 13!).
        var mSt = new Store(0, 2);
        var d16 = Shape.Binary(16);
        Supports M(Shape sh) => Supports.On(mSt, Allocate(mSt, sh));
        var (a1, a2, outSum) = (M(d16), M(d16), M(d16));
        var sumRel = Operations.Balance([d16, d16, d16], 1, 1, -1);
        var wStretch = new Wiring(sumRel, a1.Variable, a1.Variable, outSum.Variable); // SAME site a1 twice = 2·a1!
        wStretch.Add(0, 0, 0);
        var wIndep = new Wiring(sumRel, a1.Variable, a2.Variable, outSum.Variable);   // TWO distinct sites = a1 + a2!
        wIndep.Add(1, 1, 1);

        var coarseThreeHalfStars = (1UL << 6) | (1UL << 7); // {6, 7} half-stars
        a1.Restrict(0, coarseThreeHalfStars);
        a1.Restrict(1, coarseThreeHalfStars);
        a2.Restrict(1, coarseThreeHalfStars);
        Assert.NotNull(new Engine().Add(wStretch).Add(wIndep).Settle(4));
        Assert.Equal((1UL << 12) | (1UL << 14), outSum.Values(0));               // 2·x: ONLY {12, 14}!
        Assert.Equal((1UL << 12) | (1UL << 13) | (1UL << 14), outSum.Values(1)); // x1 + x2: {12, 13, 14}!

        // 4. Same-site cancellation vs. independent difference (Axiom 9):
        //    Shared site x - x settles to exact 0; two independent 3/5 readings x1 - x2 stay open {deficit/excess in 0..1}!
        var gSt = new Store(0, 2);
        var d8 = Shape.Unary(7);
        Supports G(Shape sh) => Supports.On(gSt, Allocate(gSt, sh));
        var (gx1, gx2, def, exc) = (G(d8), G(d8), G(d8), G(d8));
        var gapLaw = Numbers.OpenGap(7);
        var wSelf = new Wiring(gapLaw, gx1.Variable, gx1.Variable, def.Variable, exc.Variable); // same site!
        wSelf.Add(0, 0, 0, 0);
        var wPair = new Wiring(gapLaw, gx1.Variable, gx2.Variable, def.Variable, exc.Variable); // independent!
        wPair.Add(1, 1, 1, 1);
        gx1.Restrict(0, coarseThreeHalfStars);
        gx1.Restrict(1, coarseThreeHalfStars);
        gx2.Restrict(1, coarseThreeHalfStars);
        Assert.NotNull(new Engine().Add(wSelf).Add(wPair).Settle(4));
        Assert.Equal((1UL << 0, 1UL << 0), (def.Values(0), exc.Values(0)));                     // x - x == 0 exact!
        Assert.Equal((0b11UL, 0b11UL), (def.Values(1), exc.Values(1)));                         // x1 - x2 stays open {-1, 0, +1}!
    }

    [Fact]
    public void Axiom8ProjectionPreservesResidueWhileMaterialTransferHoldsWholePackets()
    {
        // 1. Observational Axiom-8 projection: n·d2 = q·d1 + r (0 <= r < d1).
        var st = new Store(0, 4);
        var s10 = Shape.Binary(11);
        Supports S() => Supports.On(st, Allocate(st, s10));
        var (n, d1, d2, q, r) = (S(), S(), S(), S(), S());
        var pw = new Wiring(Portions.Project(10), n.Variable, d1.Variable, d2.Variable, q.Variable, r.Variable);
        for (var lane = 0; lane < 4; lane++) pw.Add(lane, lane, lane, lane, lane);

        // Lane 0: 3/5 -> /10 => exact 6/10, residue 0
        n.Restrict(0, 1UL << 3); d1.Restrict(0, 1UL << 5); d2.Restrict(0, 1UL << 10);
        // Lane 1: 3/5 -> /8  => 4/8 + residue 4 (4/40 = 1/10, exact: 4/8 + 1/10 = 3/5!)
        n.Restrict(1, 1UL << 3); d1.Restrict(1, 1UL << 5); d2.Restrict(1, 1UL << 8);
        // Lane 2: 4/5 -> /3  => 2/3 + residue 2 (2 fifths of a third, visibly retained!)
        n.Restrict(2, 1UL << 4); d1.Restrict(2, 1UL << 5); d2.Restrict(2, 1UL << 3);
        // Lane 3: zero denominator d1 = 0 => empty contradiction (never throws or fakes 0!)
        n.Restrict(3, 1UL << 0); d1.Restrict(3, 1UL << 0); d2.Restrict(3, 1UL << 8);

        Assert.NotNull(new Engine().Add(pw).Settle(4));
        Assert.Equal((1UL << 6, 1UL << 0), (q.Values(0), r.Values(0)));
        Assert.Equal((1UL << 4, 1UL << 4), (q.Values(1), r.Values(1)));
        Assert.Equal((1UL << 2, 1UL << 2), (q.Values(2), r.Values(2)));
        Assert.Equal(0UL, q.Values(3));

        // 2. Material fifths-to-eighths transfer (ops_sol Round 3 contrast):
        // 5 fifths trade for 8 eighths; with only 3 fifths available, 0 eighths move and all 3 fifths wait!
        var mSt = new Store(0, 1);
        var fifths = Allocate(mSt, Shape.Unary(5));
        var eighths = Allocate(mSt, Shape.Unary(8));
        fifths.Write(mSt, 0, 3);
        var matEng = new Engine().Add(Tension.Route(Flows.Convert(5, 8, 5, 8), fifths.On(mSt), eighths.On(mSt), Here));
        Assert.Equal(1, matEng.Settle(4)); // immediately quiet: cannot split a whole packet!
        Assert.Equal((3, 0), (fifths.Read(mSt, 0), eighths.Read(mSt, 0)));

        // 3. Common refinement via conserving moves (Claude Round 14 check): 2/5 + 1/3 -> 6/15 + 5/15 = 11/15!
        var cSt = new Store(0, 1);
        var aFifths = Allocate(cSt, Shape.Unary(5));
        var bThirds = Allocate(cSt, Shape.Unary(3));
        var fifteenths = Allocate(cSt, Shape.Unary(15));
        aFifths.Write(cSt, 0, 2);
        bThirds.Write(cSt, 0, 1);
        var cEng = new Engine()
            .Add(Tension.Route(Flows.Convert(1, 3, 5, 15), aFifths.On(cSt), fifteenths.On(cSt), Here))
            .Add(Tension.Route(Flows.Convert(1, 5, 3, 15), bThirds.On(cSt), fifteenths.On(cSt), Here));
        Assert.NotNull(cEng.Settle(8));
        Assert.Equal((0, 0, 11), (aFifths.Read(cSt, 0), bThirds.Read(cSt, 0), fifteenths.Read(cSt, 0)));
    }

    [Fact]
    public void GeneralizedAtomicAdmissionWorksAtPartitionFiveAndSurvivesSaveLocalsMidCarry()
    {
        // Generalizes ops_sol's RadixFour.Admit to partition 5 (coarse 0..2, fine 0..4 => exact capacity 14).
        const int Partition = 5;
        const int CoarseCap = 2;
        const int TotalCap = (CoarseCap + 1) * Partition - 1; // 14

        static (Engine Source, Tension Incoming, Engine Receiver, Store Rung, Field Coarse, Field Fine) Arrange()
        {
            var srcStore = new Store(0, 1);
            var incoming = new Tension(srcStore, Allocate(srcStore, Shape.Unary(1)), capacity: 1);
            var source = new Engine().Hold(srcStore);
            var outlet = srcStore.Publish([0], [.. incoming.Plus.Planes]);

            var rung = new Store(0, 1);
            var coarse = Allocate(rung, Shape.Unary(CoarseCap));
            var fine = Allocate(rung, Numbers.Digit(Partition, unary: true));
            var admit = Tension.Wire(
                Numbers.Admit(Partition, CoarseCap, pressureCapacity: 1, unary: true),
                [new Binding(outlet, [.. Enumerable.Range(0, outlet.PlaneCount)]), coarse.On(rung), fine.On(rung)],
                Here);
            return (source, incoming, new Engine().Add(admit), rung, coarse, fine);
        }

        // 1. Admit 1..15 units: fine stays strictly in 0..4, carries at 5 and 10, 15th waits as supplier pressure, 16th blocked!
        var f = Arrange();
        for (var offered = 1; offered <= TotalCap + 1; offered++)
        {
            Assert.Equal(Here, f.Incoming.Receive(Here).Taken);
            var step = Assert.Single(f.Receiver.Step().Steps);
            var received = offered <= TotalCap ? offered : TotalCap;
            var pending = f.Incoming.Plus.Read(f.Incoming.Store, 0)!.Value;
            var coarse = f.Coarse.Read(f.Rung, 0)!.Value;
            var fine = f.Fine.Read(f.Rung, 0)!.Value;
            Assert.Equal(offered <= TotalCap, step.Changed[0]);
            Assert.Equal((received / Partition, received % Partition), (coarse, fine));
            Assert.Equal(offered, pending + Partition * coarse + fine);
        }
        Assert.Equal(Here, f.Incoming.Receive(Here).Blocked);

        // 2. Mid-carry SaveLocals / LoadLocals at (0, 4) + 1 pending pressure continues identically to (1, 0)!
        var m = Arrange();
        for (var i = 0; i < 4; i++)
        {
            Assert.Equal(Here, m.Incoming.Receive(Here).Taken);
            m.Receiver.Step();
        }
        Assert.Equal(Here, m.Incoming.Receive(Here).Taken); // 5th unit waiting as pressure to trigger carry!

        var saved = Transport.SaveLocals(m.Source, m.Receiver);
        var restored = Transport.LoadLocals(saved);
        Assert.Equal(saved, Transport.SaveLocals(restored[0].Engine, restored[1].Engine));

        m.Receiver.Step();
        restored[1].Engine.Step();
        Assert.Equal((1, 0), (m.Coarse.Read(m.Rung, 0), m.Fine.Read(m.Rung, 0)));
        Assert.Equal(Transport.SaveLocals(m.Source, m.Receiver), Transport.SaveLocals(restored[0].Engine, restored[1].Engine));
    }
}
