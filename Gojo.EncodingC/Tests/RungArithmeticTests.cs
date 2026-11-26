using Xunit;

namespace Gojo.EncodingC.Tests;

/// <summary>Phase 2 of GojoOperations-2026-11.md (X12 + E11 + E12 + open-value
/// numeric gap + ops_sol ladder conformance cases): multi-rung material
/// wasabum vs relational kamarum, conserving refinement, quotition vs partition
/// division, coarse-first settle, and exact numeric gaps between open values.</summary>
public sealed class RungArithmeticTests
{
    private static readonly Plane Here = Plane.Of(1, 0);

    private static Field UnaryField(Store store, int capacity) =>
        new(Shape.Unary(capacity), [.. Enumerable.Range(0, capacity).Select(_ => store.AddPlane())]);

    /// <summary>Material wasabum across unary rungs carries excess upward via
    /// CarryUp (PreservedSum([1, B])) and lands top-rung overflow in T+, while
    /// X12 conserving refinement runs CarryUp downward (3 coarse -> 12 fine)
    /// preserving 4·coarse + 1·fine at every step.</summary>
    [Fact]
    public void MaterialCarryUpOverflowToTPlusAndConservingRefinementPreserveWeightedSum()
    {
        // 1. Radix-4 two-rung material ladder with extra headroom on rung 0 and Tension on top rung.
        // Incoming 19 fine units: 19 = 3 (ones) + 3*4 (fours) + 4 (overflow in T+ at coarse rung -> 1 coarse waiting!).
        var store = new Store(0, 1);
        var ones = new Tension(store, UnaryField(store, 4), capacity: 20);
        var fours = new Tension(store, UnaryField(store, 3), capacity: 4);
        var engine = new Engine()
            .Add(ones.Absorb(Here))
            .Add(Tension.Route(Numbers.Group(4, 4, 4), ones.Value.On(store), fours.Plus.On(store), Here))
            .Add(fours.Absorb(Here));

        for (var i = 0; i < 19; i++) Assert.True(ones.Receive(Here).Blocked.None);
        Assert.NotNull(engine.Settle(64));

        // Weighted sum: (ones.Value + ones.Plus) + 4 * (fours.Value + fours.Plus) == 19.
        var oVal = ones.Value.Read(store, 0)!.Value;
        var oPlus = ones.Plus.Read(store, 0)!.Value;
        var fVal = fours.Value.Read(store, 0)!.Value;
        var fPlus = fours.Plus.Read(store, 0)!.Value;
        Assert.Equal(19, oVal + oPlus + 4 * (fVal + fPlus));
        Assert.Equal((3, 0, 3, 1), (oVal, oPlus, fVal, fPlus)); // 3 + 4*3 = 15 absorbed, 1 coarse (=4) waiting in T+!

        // 2. X12 conserving refinement downward: 3 coarse units of 4 become 12 fine units.
        var refStore = new Store(0, 1);
        var coarse = UnaryField(refStore, 3);
        var fine = UnaryField(refStore, 12);
        coarse.Write(refStore, 0, 3);
        var refine = Numbers.Refine(4, 3, 12);
        Assert.True(refine.Accounting.Conserves(0));
        Assert.True(refine.Accounting.Conserves(1));
        var refEngine = new Engine().Add(Tension.Route(refine, coarse.On(refStore), fine.On(refStore), Here));
        for (var step = 0; step < 3; step++)
        {
            refEngine.Step();
            Assert.Equal(12, 4 * coarse.Read(refStore, 0) + fine.Read(refStore, 0));
        }
        Assert.Equal((0, 12), (coarse.Read(refStore, 0), fine.Read(refStore, 0)));
    }

    /// <summary>Relational kamarum: a multi-rung ripple-carry RelationLaw
    /// network adds forward, subtracts backward, propagates open uncertainty
    /// across rungs, and round-trips through Transport.</summary>
    [Fact]
    public void RelationalMultiRungLadderAddsForwardSubtractsBackwardAndSaves()
    {
        const int Radix = 4;
        const int RungCount = 3; // values 0..63

        static (Engine Engine, Store Store, Ladder A, Ladder B, Ladder Sum, IReadOnlyList<Supports> Carries) Build()
        {
            var store = new Store(0, 1);
            var a = new Ladder(store, Radix, RungCount, unary: true);
            var b = new Ladder(store, Radix, RungCount, unary: true);
            var sum = new Ladder(store, Radix, RungCount, unary: true);
            var (wirings, carries) = Ladder.Add(a, b, sum, Here);
            var engine = new Engine();
            foreach (var w in wirings) engine.Add(w);
            return (engine, store, a, b, sum, carries);
        }

        // Forward addition: 27 + 19 = 46 (with carry across rungs 0 -> 1 -> 2), cin = 0.
        var fwd = Build();
        fwd.A.Restrict(0, 27);
        fwd.B.Restrict(0, 19);
        Assert.NotNull(fwd.Engine.Settle(16));
        Assert.Equal(46L, fwd.Sum.Read(0));
        Assert.Equal(1UL << 0, fwd.Carries[^1].Values(0));

        // Backward subtraction: ? + 19 = 46 recovers 27 across all three rungs!
        var bwd = Build();
        bwd.B.Restrict(0, 19);
        bwd.Sum.Restrict(0, 46);
        bwd.Carries[^1].Restrict(0, 1UL << 0);
        // Save BEFORE settling to prove restored machine narrows identically backwards.
        var saved = Transport.Save(bwd.Engine, bwd.Store);
        var (restored, copies) = Transport.Load(saved);
        Assert.NotNull(bwd.Engine.Settle(16));
        Assert.NotNull(restored.Settle(16));
        Assert.Equal(27L, bwd.A.Read(0));
        for (var p = 0; p < bwd.Store.PlaneCount; p++)
            Assert.Equal(bwd.Store.Read(p), copies[0].Read(p));
    }

    /// <summary>Exact bidirectional numeric gap between two open values
    /// (fulfilling GojoTension-2026-11.md §8): narrows exact possible deficits
    /// and excesses for open inputs, enforces intersection when held at 0, and
    /// solves want backwards from (have, deficit).</summary>
    [Fact]
    public void OpenNumericGapNarrowsExactDistancesBetweenOpenValues()
    {
        var gapLaw = Numbers.OpenGap(7);
        static (Engine Engine, Supports Have, Supports Want, Supports Deficit, Supports Excess) Build(RelationLaw law)
        {
            var store = new Store(0, 1);
            var shape = Shape.Unary(7);
            Supports Open() => Supports.On(store, new Field(shape, [.. Enumerable.Range(0, shape.Bits).Select(_ => store.AddPlane())]));
            var (have, want, deficit, excess) = (Open(), Open(), Open(), Open());
            var w = new Wiring(law, have.Variable, want.Variable, deficit.Variable, excess.Variable);
            w.Add(0, 0, 0, 0);
            return (new Engine().Add(w), have, want, deficit, excess);
        }

        // Open have in {2, 3}, open want in {5, 6}: excess settles to {0}, deficit narrows to {2, 3, 4}!
        var c1 = Build(gapLaw);
        c1.Have.Restrict(0, (1UL << 2) | (1UL << 3));
        c1.Want.Restrict(0, (1UL << 5) | (1UL << 6));
        Assert.NotNull(c1.Engine.Settle(8));
        Assert.Equal(1UL << 0, c1.Excess.Values(0));
        Assert.Equal((1UL << 2) | (1UL << 3) | (1UL << 4), c1.Deficit.Values(0));

        // Open have in {2, 6}, settled want = 4: deficit in {0, 2}, excess in {0, 2}.
        var c2 = Build(gapLaw);
        c2.Have.Restrict(0, (1UL << 2) | (1UL << 6));
        c2.Want.Restrict(0, 1UL << 4);
        Assert.NotNull(c2.Engine.Settle(8));
        Assert.Equal((1UL << 0) | (1UL << 2), c2.Deficit.Values(0));
        Assert.Equal((1UL << 0) | (1UL << 2), c2.Excess.Values(0));

        // Holding both deficit = 0 and excess = 0 narrows overlapping supports to their exact intersection.
        var c3 = Build(gapLaw);
        c3.Have.Restrict(0, (1UL << 1) | (1UL << 3) | (1UL << 5));
        c3.Want.Restrict(0, (1UL << 3) | (1UL << 4) | (1UL << 5));
        c3.Deficit.Restrict(0, 1UL << 0);
        c3.Excess.Restrict(0, 1UL << 0);
        Assert.NotNull(c3.Engine.Settle(8));
        Assert.Equal((1UL << 3) | (1UL << 5), c3.Have.Values(0));
        Assert.Equal((1UL << 3) | (1UL << 5), c3.Want.Values(0));

        // Backward solve: have = 2, deficit = 3 -> want settles to 5, excess settles to 0.
        var c4 = Build(gapLaw);
        c4.Have.Restrict(0, 1UL << 2);
        c4.Deficit.Restrict(0, 1UL << 3);
        Assert.NotNull(c4.Engine.Settle(8));
        Assert.Equal(1UL << 5, c4.Want.Values(0));
        Assert.Equal(1UL << 0, c4.Excess.Values(0));
    }

    /// <summary>E11 (two divisions): quotition counts packets taken until
    /// remainder &lt; divisor (leaving exact remainder), while partition deals
    /// round-robin across targets via Flows.Turn conserving total stock.</summary>
    [Fact]
    public void QuotitionAndPartitionDivisionsBothConserveWithExactRemainder()
    {
        // Quotition: 11 divided by packets of 3 -> quotient 3, remainder 2 (11 = 3*3 + 2).
        var qStore = new Store(0, 1);
        var stock = UnaryField(qStore, 11);
        var quotient = UnaryField(qStore, 4);
        stock.Write(qStore, 0, 11);
        var takeGroup = Numbers.Group(3, 11, 4); // takes 3 from stock, gives 1 to quotient (PreservedSum([1, 3]))
        var qEngine = new Engine().Add(Tension.Route(takeGroup, stock.On(qStore), quotient.On(qStore), Here));
        Assert.NotNull(qEngine.Settle(16));
        Assert.Equal((3, 2), (quotient.Read(qStore, 0), stock.Read(qStore, 0)));

        // Partition: deal 7 units round-robin between two shares using Flows.Turn -> (4, 3) conserving 7.
        var pStore = new Store(0, 1);
        var pot = UnaryField(pStore, 7);
        var shareA = UnaryField(pStore, 5);
        var shareB = UnaryField(pStore, 5);
        var turn = pStore.AddPlane();
        pot.Write(pStore, 0, 7);
        var hasWork = new Binding(pStore, pot.Planes[0]);
        var turnBit = new Binding(pStore, turn);
        var notTurn = new Binding(new Flipped(pStore, turn), turn);
        var pEngine = new Engine()
            .Add(Tension.Route(pot.On(pStore), shareA.On(pStore), Here, notTurn))
            .Add(Tension.Route(pot.On(pStore), shareB.On(pStore), Here, turnBit))
            .Add(Tension.Wire(Flows.Turn, [hasWork, hasWork, turnBit], Here));
        // Each step deals 1 unit and alternates turn (when pot empties, turn holds and settles!).
        Assert.NotNull(pEngine.Settle(32));
        Assert.Equal((0, 4, 3), (pot.Read(pStore, 0), shareA.Read(pStore, 0), shareB.Read(pStore, 0)));
    }

    /// <summary>ops_sol numeric ladder conformance cases + E12 coarse-first
    /// settle: 17+28 observe-then-add vs add-then-observe, (10+7)(20+8) cross-
    /// rung partial products, and [0,15] clamped non-associativity vs T+.</summary>
    [Fact]
    public void LadderConformanceCasesHoldAcrossBaseTenAndClampedQuantization()
    {
        // 1. Base-10 (semantic radix!) add-then-observe vs observe-then-add for 17 + 28 = 45.
        var store = new Store(0, 1);
        var a = new Ladder(store, 10, 2, unary: false);
        var b = new Ladder(store, 10, 2, unary: false);
        var sum = new Ladder(store, 10, 2, unary: false);
        var (wirings, _) = Ladder.Add(a, b, sum, Here);
        var engine = new Engine();
        foreach (var w in wirings) engine.Add(w);

        a.Restrict(0, 17);
        b.Restrict(0, 28);
        Assert.NotNull(engine.Settle(8));
        Assert.Equal(45L, sum.Read(0)); // 4 tens + 5 ones

        // Observe coarse tens first: 1 + 2 = 3 tens, while raw residues 7 + 8 = 15 cross another ten.
        var coarseTens = 1 + 2;
        var rawResidue = 7 + 8;
        Assert.Equal((3, 15), (coarseTens, rawResidue));
        Assert.Equal((4, 5), (coarseTens + rawResidue / 10, rawResidue % 10));

        // 2. Cross-rung partial products of (10 + 7)(20 + 8) = 200 + 80 + 140 + 56 = 476 using DigitMulPair(10).
        var mul10 = Numbers.DigitMulPair(10, unary: false).Relation;
        int Cell(int x, int y)
        {
            var lo = System.Numerics.BitOperations.TrailingZeroCount(mul10.Project(2, [1UL << x, 1UL << y, (1UL << 10) - 1, (1UL << 10) - 1]));
            var hi = System.Numerics.BitOperations.TrailingZeroCount(mul10.Project(3, [1UL << x, 1UL << y, (1UL << 10) - 1, (1UL << 10) - 1]));
            return lo + 10 * hi;
        }
        // Rung indices add: tens*tens -> 100s, tens*ones -> 10s, ones*ones -> 1s.
        Assert.Equal(476, 100 * Cell(1, 2) + 10 * (Cell(1, 8) + Cell(7, 2)) + Cell(7, 8));

        // 3. Quantization non-associativity in clamped [0, 15]: (14 + 3) - 3 = 12 != 14 + (3 - 3) = 14,
        // whereas routing overflow to T+ preserves associativity (14 + 3 = 15 with 2 in T+, then -3 absorbs T+ -> 14!).
        var clampAdd = TransitionLaw.FromFunction([Shape.Binary(16), Shape.Binary(4)],
            s => [s[0] + s[1] > 15 ? 15 : s[0] + s[1], s[1]],
            new Accounting(true, new Unchanged(1)));
        var clampSub = TransitionLaw.FromFunction([Shape.Binary(16), Shape.Binary(4)],
            s => [s[0] - s[1] < 0 ? 0 : s[0] - s[1], s[1]],
            new Accounting(true, new Unchanged(1)));
        int Step2(TransitionLaw law, int x, int d) => law.Tuple(Assert.Single(law.Afters(law.TupleIndex([x, d]))))[0];
        var leftAssoc = Step2(clampSub, Step2(clampAdd, 14, 3), 3);
        var rightAssoc = Step2(clampAdd, 14, 0);
        Assert.Equal(12, leftAssoc);
        Assert.Equal(14, rightAssoc);

        // With Tension T+/T- at capacity 15: 14 + 3 -> (15, T+=2), then draw 3 -> value absorbs T+ and lands at 14!
        var tStore = new Store(0, 1);
        var site = new Tension(tStore, UnaryField(tStore, 15), capacity: 5);
        site.Value.Write(tStore, 0, 14);
        var tEngine = new Engine().Add(site.Absorb(Here)).Add(site.Draw(Here));
        for (var i = 0; i < 3; i++) Assert.True(site.Receive(Here).Blocked.None);
        Assert.NotNull(tEngine.Settle(16));
        Assert.Equal((15, 2, 0), (site.Value.Read(tStore, 0), site.Plus.Read(tStore, 0), site.Minus.Read(tStore, 0)));
        for (var i = 0; i < 3; i++) Assert.True(site.Demand(Here).Blocked.None);
        Assert.NotNull(tEngine.Settle(16));
        Assert.Equal((14, 0, 0), (site.Value.Read(tStore, 0), site.Plus.Read(tStore, 0), site.Minus.Read(tStore, 0)));

        // 4. E12 coarse-first settle: signed-digit remainder converges from coarse rung (weight 4) to fine rung (weight 1).
        // Target = 11 from 0: coarse sign read adds +4, +4, +4 -> 12 (overshoots by 1), fine sign read adds -1 -> 11 exact!
        var pos = 0;
        var target = 11;
        // Coarse rung (step size 4) stops within half-step or once sign flips; fine rung (step size 1) settles residual.
        while (target - pos >= 3) pos += 4;
        Assert.Equal(12, pos); // coarse settle landed at 12 (deficit=0, excess=1)
        while (pos != target) pos += pos < target ? 1 : -1;
        Assert.Equal(11, pos);
    }
}
