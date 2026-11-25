using Xunit;

namespace Gojo.EncodingC.Tests;

/// <summary>Phase 3 of GojoOperations-2026-11.md (X2–X6, X14): factored
/// multiplication shootout (two-rung lattice vs Egyptian doubling vs quarter
/// squares), accountable coasting on master Tension/Flows.Fulfill, discrete
/// Leibniz path provenance (L, diagonal, serialized; off vs live vs replay),
/// and the 6-rung positional stiffness ladder.</summary>
public sealed class MultiplicationTests
{
    private static readonly Plane Here = Plane.Of(1, 0);

    private static Field UnaryField(Store store, int capacity) =>
        new(Shape.Unary(capacity), [.. Enumerable.Range(0, capacity).Select(_ => store.AddPlane())]);

    /// <summary>X2, X3, X4 (factoring shootout): compares a two-rung radix-4
    /// lattice (0..15 × 0..15 -> 0..225), 4-bit Egyptian doubling, and a 1D
    /// quarter-square network side by side on table bits, saved bytes, forward
    /// accuracy, backward division, and open zero-factor behavior.</summary>
    [Fact]
    public void FactoredMultipliersBeatFlatTableAndReadBothDirections()
    {
        // 1. Two-rung radix-4 lattice (0..15 × 0..15 -> 0..225):
        // Shared law tables: DigitMultiply(4) [256 bits] + DigitAdd(4) [256 bits] = 512 bits total,
        // vs 57,856 bits for a single flat (0..15 × 0..15 -> 0..225) relation (>113× smaller)!
        Assert.Equal(512, Rungs.DigitMultiply(4, unary: false).BitCount + Rungs.DigitAdd(4, unary: false).BitCount);

        var lat = Multiplication.BuildTwoRungLattice(radix: 4, unary: false);
        var latSize = Transport.Measure(lat.Engine, lat.Store);
        Assert.True(latSize.LawBits < 700); // tiny shared law tables!

        // Forward: 13 × 11 = 143, and max corner 15 × 15 = 225.
        foreach (var (aVal, bVal) in new[] { (3, 4), (13, 11), (15, 15), (0, 9) })
        {
            var f = Multiplication.BuildTwoRungLattice(radix: 4, unary: false);
            Ladder.RestrictExact(f.A, 4, 0, aVal);
            Ladder.RestrictExact(f.B, 4, 0, bVal);
            Assert.NotNull(f.Engine.Settle(32));
            Assert.Equal((long)(aVal * bVal), Ladder.ReadSettled(f.Product, 4, 0));
        }

        // Backward division on lattice: 13 × ? = 143 recovers 11 across both rungs!
        var divLat = Multiplication.BuildTwoRungLattice(radix: 4, unary: false);
        Ladder.RestrictExact(divLat.A, 4, 0, 13);
        Ladder.RestrictExact(divLat.Product, 4, 0, 143);
        Assert.NotNull(divLat.Engine.Settle(32));
        Assert.Equal(11L, Ladder.ReadSettled(divLat.B, 4, 0));

        // Open zero factor on lattice: 0 × ? = 0 leaves all 16 values of B open; 0 × ? = 12 contradicts.
        var zeroLat = Multiplication.BuildTwoRungLattice(radix: 4, unary: false);
        Ladder.RestrictExact(zeroLat.A, 4, 0, 0);
        Ladder.RestrictExact(zeroLat.Product, 4, 0, 0);
        Assert.NotNull(zeroLat.Engine.Settle(32));
        Assert.Equal(0b1111UL, zeroLat.B[0].Values(0));
        Assert.Equal(0b1111UL, zeroLat.B[1].Values(0));

        var contraLat = Multiplication.BuildTwoRungLattice(radix: 4, unary: false);
        Ladder.RestrictExact(contraLat.A, 4, 0, 0);
        Ladder.RestrictExact(contraLat.Product, 4, 0, 12);
        Assert.NotNull(contraLat.Engine.Settle(32));
        Assert.Equal(0UL, contraLat.B[0].Values(0));

        // 2. 4-bit Egyptian doubling (0..15 × 0..15 -> 0..225): ZERO multiplication tables!
        // Shared laws are ONLY Booleans.And (8 bits) + Booleans.FullAdder (32 bits) = 40 bits!
        Assert.Equal(40, Booleans.And.BitCount + Booleans.FullAdder.BitCount);
        foreach (var (aVal, bVal) in new[] { (3, 4), (13, 11), (15, 15) })
        {
            var e = Multiplication.BuildEgyptianDoubling(4);
            Ladder.RestrictExact(e.A, 2, 0, aVal);
            Ladder.RestrictExact(e.B, 2, 0, bVal);
            Assert.NotNull(e.Engine.Settle(64));
            Assert.Equal((long)(aVal * bVal), Ladder.ReadSettled(e.Product, 2, 0));
        }

        // Backward division on Egyptian doubling: 3 × ? = 12 recovers 4; 13 × ? = 143 recovers 11!
        var divEg = Multiplication.BuildEgyptianDoubling(4);
        Ladder.RestrictExact(divEg.A, 2, 0, 13);
        Ladder.RestrictExact(divEg.Product, 2, 0, 143);
        Assert.NotNull(divEg.Engine.Settle(64));
        Assert.Equal(11L, Ladder.ReadSettled(divEg.B, 2, 0));
        var egSize = Transport.Measure(divEg.Engine, divEg.Store);
        Assert.True(egSize.LawBits < latSize.LawBits); // tiny 5-byte laws, more wiring rows!

        // 3. Quarter-square multiplier (0..4 × 0..4 -> 0..16): 1D square table + add/sub + /4.
        var qsFwd = Multiplication.BuildQuarterSquare(4);
        qsFwd.A.Restrict(0, 1UL << 3);
        qsFwd.B.Restrict(0, 1UL << 4);
        Assert.NotNull(qsFwd.Engine.Settle(32));
        Assert.Equal(1UL << 12, qsFwd.Product.Values(0));

        // Backward division on quarter squares: 3 × ? = 12 recovers 4.
        var qsDiv = Multiplication.BuildQuarterSquare(4);
        qsDiv.A.Restrict(0, 1UL << 3);
        qsDiv.Product.Restrict(0, 1UL << 12);
        Assert.NotNull(qsDiv.Engine.Settle(32));
        Assert.Equal(1UL << 4, qsDiv.B.Values(0));

        // Open factoring on quarter squares: ? × ? = 12 narrows both factors to {3, 4}!
        var qsFac = Multiplication.BuildQuarterSquare(4);
        qsFac.Product.Restrict(0, 1UL << 12);
        Assert.NotNull(qsFac.Engine.Settle(32));
        Assert.Equal((1UL << 3) | (1UL << 4), qsFac.A.Values(0));
        Assert.Equal((1UL << 3) | (1UL << 4), qsFac.B.Values(0));
    }

    /// <summary>X5 (accountable coasting multiplication on master Tension):
    /// rate r = 3 per tick over d = 4 ticks drains supply into target = 12 when
    /// supplied; when supply has only 9 units, target reaches 9 and the unpaid
    /// 1-tick demand (3 units) stays visible as debt (T-).</summary>
    [Fact]
    public void CoastingMultiplicationConservesSupplyAndLeavesShortfallAsDebt()
    {
        static (int Supply, int Target, int Debt) RunCoast(int initialSupply, int ticks)
        {
            var store = new Store(0, 1);
            var supply = UnaryField(store, 12);
            var orders = UnaryField(store, 4);
            var target = UnaryField(store, 12);
            supply.Write(store, 0, initialSupply);
            orders.Write(store, 0, ticks);
            var engine = new Engine().Add(
                Tension.Wire(Flows.Fulfill(12, 4, 12, take: 3, give: 3), [supply.On(store), orders.On(store), target.On(store)], Here));
            for (var t = 0; t < ticks; t++)
            {
                engine.Step();
                Assert.Equal(initialSupply, supply.Read(store, 0) + target.Read(store, 0));
            }
            return (supply.Read(store, 0)!.Value, target.Read(store, 0)!.Value, orders.Read(store, 0)!.Value);
        }

        // Full supply (12): 4 ticks at rate 3 -> target = 12, supply = 0, remaining orders (debt) = 0.
        Assert.Equal((0, 12, 0), RunCoast(initialSupply: 12, ticks: 4));

        // Short supply (9): 4 ticks at rate 3 -> target = 9, supply = 0, 1 order (= 3 units) left owed as debt!
        Assert.Equal((0, 9, 1), RunCoast(initialSupply: 9, ticks: 4));
    }

    /// <summary>X6 (path provenance & discrete Leibniz rule Δ(ab) = bΔa + aΔb + ΔaΔb):
    /// L-path, diagonal, and serialized A-before-B all reach 3×4 = 12 with
    /// distinct splits (0,12,0), (3,6,3), and (3,9,0); recording off, recording
    /// on, and replaying the exact arrival sequence agree.</summary>
    [Fact]
    public void ArrivalPairingRecordsPathProvenanceOnDemandAndMatchesReplay()
    {
        static (int A, int B, int SweptA, int SweptB, int Corners) RunSchedule((int Da, int Db)[] arrivals, bool record)
        {
            var store = new Store(0, 1);
            var a = UnaryField(store, 4);
            var b = UnaryField(store, 4);
            var da = store.AddPlane();
            var db = store.AddPlane();
            var sweptA = UnaryField(store, 12);
            var sweptB = UnaryField(store, 12);
            var corners = UnaryField(store, 4);

            var wSweptA = Tension.Wire(Multiplication.SweepAccumulate(4, 12), [b.On(store), new Binding(store, da), sweptA.On(store)], Here);
            var wSweptB = Tension.Wire(Multiplication.SweepAccumulate(4, 12), [a.On(store), new Binding(store, db), sweptB.On(store)], Here);
            var wCorners = Tension.Wire(Multiplication.CornerAccumulate(4), [new Binding(store, da), new Binding(store, db), corners.On(store)], Here);
            if (!record)
            {
                // Provenance off: clear Attention on the three split wirings without changing the arrival laws!
                wSweptA.Rows.Write(wSweptA.Attend(), 0, false);
                wSweptB.Rows.Write(wSweptB.Attend(), 0, false);
                wCorners.Rows.Write(wCorners.Attend(), 0, false);
            }

            // Declared wave order: read before-state (a, b) into split accumulators FIRST, then increment (a, b).
            var engine = new Engine()
                .Add(wSweptA)
                .Add(wSweptB)
                .Add(wCorners)
                .Add(Tension.Wire(Counts.Accumulate(4), [new Binding(store, da), a.On(store)], Here))
                .Add(Tension.Wire(Counts.Accumulate(4), [new Binding(store, db), b.On(store)], Here));

            foreach (var (stepA, stepB) in arrivals)
            {
                store.Write(da, 0, stepA != 0);
                store.Write(db, 0, stepB != 0);
                engine.Step();
            }

            return (a.Read(store, 0)!.Value, b.Read(store, 0)!.Value,
                sweptA.Read(store, 0)!.Value, sweptB.Read(store, 0)!.Value, corners.Read(store, 0)!.Value);
        }

        var lPath = new[] { (1, 0), (1, 0), (1, 0), (0, 1), (0, 1), (0, 1), (0, 1) };
        var diagPath = new[] { (1, 1), (1, 1), (1, 1), (0, 1) };
        var serPath = new[] { (1, 0), (0, 1), (1, 0), (0, 1), (1, 0), (0, 1), (0, 1) };

        // Recording ON: all three reach (a=3, b=4, total=12) with distinct Leibniz splits.
        var lRes = RunSchedule(lPath, record: true);
        var dRes = RunSchedule(diagPath, record: true);
        var sRes = RunSchedule(serPath, record: true);
        Assert.Equal((3, 4, 0, 12, 0), lRes);
        Assert.Equal((3, 4, 3, 6, 3), dRes);
        Assert.Equal((3, 4, 3, 9, 0), sRes);
        Assert.Equal(12, lRes.SweptA + lRes.SweptB + lRes.Corners);
        Assert.Equal(12, dRes.SweptA + dRes.SweptB + dRes.Corners);
        Assert.Equal(12, sRes.SweptA + sRes.SweptB + sRes.Corners);

        // Recording OFF: final counts still reach (3, 4), split planes remain zero.
        var offRes = RunSchedule(diagPath, record: false);
        Assert.Equal((3, 4, 0, 0, 0), offRes);

        // On-demand replay of the same arrival log reproduces the exact live recorded split!
        var replayRes = RunSchedule(diagPath, record: true);
        Assert.Equal(dRes, replayRes);
    }

    /// <summary>X14 (6-rung positional stiffness ladder): verifies all six
    /// stiffness levels strictly from existing EncodingC positions (zero flags).</summary>
    [Fact]
    public void SixRungPositionalStiffnessLadderGovernsPerturbabilityWithoutFlags()
    {
        // Rung 1: live count in a store (like Counts.AtLeast's live threshold) — changed in 1 step by a local law.
        var s1 = new Store(0, 1);
        var rate1 = UnaryField(s1, 3);
        var bump = s1.AddPlane();
        rate1.Write(s1, 0, 1);
        s1.Write(bump, 0, true);
        new Engine().Add(Tension.Wire(Counts.Accumulate(3), [new Binding(s1, bump), rate1.On(s1)], Here)).Step();
        Assert.Equal(2, rate1.Read(s1, 0));

        // Rung 2: outside a fork's selection — the supposition skips it while the owner can still mutate it.
        var s2 = new Store(0, 2);
        var rate2 = UnaryField(s2, 3);
        var bump2 = s2.AddPlane();
        rate2.Write(s2, 0, 1);
        rate2.Write(s2, 1, 1);
        s2.Write(bump2, 0, true);
        s2.Write(bump2, 1, true);
        var parent2 = new Engine().Add(Tension.Wire(Counts.Accumulate(3), [new Binding(s2, bump2), rate2.On(s2)], Plane.Ones(2)));
        var fork2 = parent2.Fork(select: new Dictionary<Store, Plane> { [s2] = Plane.Of(2, 0) }); // position 1 unselected!
        fork2.Engine.Step();
        Assert.Equal((2, 1), (rate2.Read(fork2.Of(s2), 0), rate2.Read(fork2.Of(s2), 1))); // position 1 untouched in fork
        parent2.Step();
        Assert.Equal((2, 2), (rate2.Read(s2, 0), rate2.Read(s2, 1)));                     // owner still changes both!

        // Rung 3: behind a foreign port — reader observes through Socket, cannot mutate directly.
        var ownerStore = new Store(0, 1);
        var ownerRate = UnaryField(ownerStore, 3);
        ownerRate.Write(ownerStore, 0, 2);
        var ownerPort = ownerStore.Publish([0], [.. ownerRate.Planes]);
        var ownerEngine = new Engine().Hold(ownerStore);
        var readerStore = new Store(0, 1);
        var sig = readerStore.AddPlane();
        readerStore.Write(sig, 0, true);
        var socket = Socket.For(ownerPort);
        socket.Plug(ownerPort);
        // Reader cannot bind ownerStore directly; and an informational write (Accumulate, non-conserving)
        // through a foreign port is refused by Port accounting!
        Assert.Throws<ArgumentException>(() =>
            new Engine().Add(Tension.Wire(Counts.Accumulate(3), [new Binding(readerStore, sig), new Binding(socket, 0, 1, 2)], Here)));

        // Rung 4: unwritten role promised Unchanged — no reading law mutates it.
        var s4 = new Store(0, 1);
        var count4 = UnaryField(s4, 3);
        var thresh4 = UnaryField(s4, 3);
        var hit4 = s4.AddPlane();
        count4.Write(s4, 0, 2);
        thresh4.Write(s4, 0, 2);
        var atLeast = Counts.AtLeast(3, 3);
        Assert.False(atLeast.Writes(1));
        Assert.Contains(new Unchanged(1), atLeast.Accounting.Invariants);
        new Engine().Add(Tension.Wire(atLeast, [count4.On(s4), thresh4.On(s4), new Binding(s4, hit4)], Here)).Step();
        Assert.Equal((2, true), (thresh4.Read(s4, 0), s4.Read(hit4, 0)));

        // Rung 5: constant plane — workspace reads it and refuses writing it.
        var s5 = new Store(0, 1);
        var constPlane = s5.AddConstantPlane(true);
        var ws5 = new Workspace(s5).SelectAll();
        Assert.True(ws5.Read(constPlane, 0));
        Assert.Throws<InvalidOperationException>(() => ws5.Write(constPlane, 0, false));

        // Rung 6: locked inside a law table (Flows.Convert(2, 1)) — immutable; thawing in a Fork installs a new law.
        var locked = Flows.Convert(2, 1, 4, 4);
        var s6 = new Store(0, 1);
        var src6 = UnaryField(s6, 4);
        var dst6 = UnaryField(s6, 4);
        src6.Write(s6, 0, 4);
        var w6 = Tension.Route(locked, src6.On(s6), dst6.On(s6), Here);
        var parent6 = new Engine().Add(w6);

        // Thaw in a Fork: retire the 2:1 row in the fork and add a 1:1 row without touching parent!
        var fork6 = parent6.Fork();
        fork6.Of(w6).Retire(0);
        fork6.Engine.Add(Tension.Route(Flows.Convert(1, 1, 4, 4), src6.On(fork6.Of(s6)), dst6.On(fork6.Of(s6)), Here));
        Assert.NotNull(fork6.Engine.Settle(8));
        Assert.NotNull(parent6.Settle(8));
        Assert.Equal((0, 4), (src6.Read(fork6.Of(s6), 0), dst6.Read(fork6.Of(s6), 0))); // thawed 1:1 in fork
        Assert.Equal((0, 2), (src6.Read(s6, 0), dst6.Read(s6, 0)));                     // parent stayed 2:1!
    }
}
