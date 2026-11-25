using Xunit;

namespace Gojo.EncodingC.Tests;

/// <summary>Phase 0 of GojoOperations-2026-11.md: saved engine step kind,
/// checked shear reversibility and inverse laws, the 3×4 common comparison
/// (X1), and the 5-row aliasing / Step-vs-Settle contract (X7).</summary>
public sealed class OperationsPhase0Tests
{
    private static readonly Plane Here = Plane.Of(1, 0);

    private static Field UnaryField(Store store, int capacity) =>
        new(Shape.Unary(capacity), [.. Enumerable.Range(0, capacity).Select(_ => store.AddPlane())]);

    private static Supports OpenField(Store store, Shape shape) =>
        Supports.On(store, new Field(shape, [.. Enumerable.Range(0, shape.Bits).Select(_ => store.AddPlane())]));

    /// <summary>A shear y' = (y + x) mod 4 is functional and reversible without
    /// conserving any positive sum. Its inverse runs cleanly back to the exact
    /// original state, while a lossy law like Passage is refused when claiming
    /// reversibility. Both Reversible and Inverse round-trip through Transport.</summary>
    [Fact]
    public void ReversibleAccountingAcceptsShearsRefusesLossyMergesAndRoundTrips()
    {
        var mod4 = Shape.Binary(4);
        var shear = TransitionLaw.FromFunction(
            [mod4, mod4],
            s => [s[0], (s[1] + s[0]) & 3],
            new Accounting(functional: true, reversible: true, new Unchanged(0)));

        Assert.True(shear.IsFunctional);
        Assert.True(shear.IsReversible);
        Assert.True(shear.Accounting.Reversible);
        Assert.False(Flows.Passage.IsReversible);

        // Claiming Reversible on a non-injective table is refused with Invariant == -2.
        Assert.Throws<ArgumentException>(() =>
            TransitionLaw.FromFunction([Shape.Bit, Shape.Bit], s => [s[0], s[0] | s[1]],
                new Accounting(functional: true, reversible: true, new Unchanged(0))));

        // Partial bijection (out-of-range unadmitted) is still Reversible (!Functional).
        var partialShear = TransitionLaw.FromFunction(
            [Shape.Unary(3), Shape.Unary(3)],
            s => s[1] + s[0] <= 3 ? [s[0], s[1] + s[0]] : null,
            new Accounting(functional: false, reversible: true, new Unchanged(0)));
        Assert.True(partialShear.IsReversible);
        Assert.False(partialShear.IsFunctional);

        // Inverse() recovers every (x, y) exactly.
        var unShear = shear.Inverse();
        Assert.True(unShear.IsFunctional);
        Assert.True(unShear.IsReversible);
        for (var x = 0; x < 4; x++)
            for (var y = 0; y < 4; y++)
            {
                var next = shear.Tuple(Assert.Single(shear.Afters(shear.TupleIndex([x, y]))));
                var back = unShear.Tuple(Assert.Single(unShear.Afters(unShear.TupleIndex(next))));
                Assert.Equal([x, y], back);
            }

        // Transport saves and restores Reversible.
        var store = new Store(4, 1);
        var (fx, fy) = (new Field(mod4, 0, 1), new Field(mod4, 2, 3));
        fx.Write(store, 0, 3);
        fy.Write(store, 0, 2);
        var wiring = new Wiring(shear, fx.On(store), fy.On(store));
        wiring.Add(0, 0);
        var engine = new Engine().Add(wiring);
        var saved = Transport.Save(engine);
        var (restored, stores) = Transport.Load(saved);

        var restoredLaw = Assert.IsType<TransitionLaw>(restored.Wirings[0].Law);
        Assert.True(restoredLaw.Accounting.Reversible);
        Assert.True(restoredLaw.IsReversible);
        restored.Advance();
        Assert.Equal((3, 1), (fx.Read(stores[0], 0), fy.Read(stores[0], 0)));
    }

    /// <summary>An engine's declared step kind (in-order vs together) is saved
    /// in Transport, propagated onto forks, enforced on Fuse, and used by
    /// Advance() / Run() / Settle(limit).</summary>
    [Fact]
    public void SavedStepKindRoundTripsThroughTransportAndFork()
    {
        static (Engine Engine, Store Store) Chain(bool together)
        {
            var store = new Store(3, 1);
            store.Write(0, 0, true);
            var w1 = new Wiring(Flows.Move(1, 1), new Binding(store, 0), new Binding(store, 1));
            var w2 = new Wiring(Flows.Move(1, 1), new Binding(store, 1), new Binding(store, 2));
            w1.Add(0, 0);
            w2.Add(0, 0);
            return (new Engine { Together = together }.Add(w1).Add(w2), store);
        }

        var (ordered, _) = Chain(together: false);
        var (together, _) = Chain(together: true);

        Assert.Throws<ArgumentException>(() => ordered.Fuse(together));

        var (restoredOrdered, ordStores) = Transport.Load(Transport.Save(ordered));
        var (restoredTogether, togStores) = Transport.Load(Transport.Save(together));
        Assert.False(restoredOrdered.Together);
        Assert.True(restoredTogether.Together);
        Assert.True(restoredTogether.Fork().Engine.Together);

        // One Advance(): in-order chains bit 0 -> 1 -> 2 in one step; together moves only 0 -> 1.
        restoredOrdered.Advance();
        restoredTogether.Advance();
        Assert.Equal((false, false, true), (ordStores[0].Read(0, 0), ordStores[0].Read(1, 0), ordStores[0].Read(2, 0)));
        Assert.Equal((false, true, false), (togStores[0].Read(0, 0), togStores[0].Read(1, 0), togStores[0].Read(2, 0)));
    }

    /// <summary>X1 (3 × 4 common comparison): one numeral pair exposes three
    /// distinct readings — a 425-bit bounded relation (including same-sample
    /// diagonal squares via SameReference), a shared-partition AND vs paired
    /// product, and an accountable Flows.Fulfill packet process (indivisible
    /// 3/tick packets into capacity 10 giving 0->3->6->9->9 vs partial).</summary>
    [Fact]
    public void ThreeTimesFourDistinguishesRelationSharedAndAndSuppliedPackets()
    {
        // 1. Bounded relation over a,b in 0..4 and c in 0..16: 5*5*17 = 425 bits (54 bytes), 25 tuples.
        var factor = Shape.Unary(4);
        var prod = Shape.Unary(16);
        var times = RelationLaw.FromPredicate([factor, factor, prod], t => t[0] * t[1] == t[2]);
        Assert.Equal(425, times.BitCount);
        Assert.Equal(54, times.Bytes().Length);
        Assert.Equal(25, times.AdmittedCount);

        ulong All(Shape s) => (1UL << s.Size) - 1;
        // Forward: (3, 4) -> 12
        Assert.Equal(1UL << 12, times.Project(2, [1UL << 3, 1UL << 4, All(prod)]));
        // Backward: (3, ?) -> 12 gives 4
        Assert.Equal(1UL << 4, times.Project(1, [1UL << 3, All(factor), 1UL << 12]));
        // Factor alternatives: c = 12 leaves marginals {3, 4} for both roles, while (3, 3) & (4, 4) remain excluded.
        Assert.Equal((1UL << 3) | (1UL << 4), times.Project(0, [All(factor), All(factor), 1UL << 12]));
        Assert.False(times.Admits(3, 3, 12));
        Assert.False(times.Admits(4, 4, 12));
        // Zero factor: a = 0, c = 0 leaves all 5 values of b open.
        Assert.Equal(All(factor), times.Project(1, [1UL << 0, All(factor), 1UL << 0]));
        // Contradiction inside declared domain: a = 2, c = 5 -> empty support.
        Assert.Equal(0UL, times.Project(1, [1UL << 2, All(factor), 1UL << 5]));

        // Same-sample binding via Wiring.SameReference: binding both inputs to one open field
        // admits only diagonal squares {0, 1, 4, 9, 16}, contradicting c = 12.
        var relStore = new Store(0, 1);
        var x = OpenField(relStore, factor);
        var p = OpenField(relStore, prod);
        var diagWiring = new Wiring(times, x.Variable, x.Variable, p.Variable);
        diagWiring.Add(0, 0, 0);
        var diagEngine = new Engine().Add(diagWiring);
        Assert.NotNull(diagEngine.Settle(8));
        Assert.Equal((1UL << 0) | (1UL << 1) | (1UL << 4) | (1UL << 9) | (1UL << 16), p.Values(0));
        p.Restrict(0, 1UL << 12);
        Assert.NotNull(diagEngine.Settle(8));
        Assert.Equal(0UL, x.Values(0));
        Assert.Equal(0UL, p.Values(0));

        // 2. Shared-axis AND (occupancy counts 3 and 4 on 4 sites overlap by 3) vs independent pairing (12).
        var axisA = Plane.Of(4, 0, 1, 2);
        var axisB = Plane.Of(4, 0, 1, 2, 3);
        Assert.Equal(3, axisA.And(axisB).Count);
        Assert.Equal(12, axisA.Count * axisB.Count);

        // 3. Supplied packet process: 3/tick from supply=12 against 4 orders into receiver capacity=10.
        // Note: putting a 4th tally role directly onto Fulfill(12,4,10,3,3,4) yields 3,575^2 = 12.78M bits (> Law.MaxBits),
        // hitting the table wall; keeping the receipt counter as a separate small law reading Changed keeps every table small!
        Assert.Throws<ArgumentOutOfRangeException>(() => Flows.Fulfill(12, 4, 10, take: 3, give: 3, tallyCapacity: 4));
        var pktStore = new Store(0, 1);
        var supply = UnaryField(pktStore, 12);
        var orders = UnaryField(pktStore, 4);
        var receiver = UnaryField(pktStore, 10);
        var receipts = UnaryField(pktStore, 4);
        supply.Write(pktStore, 0, 12);
        orders.Write(pktStore, 0, 4);

        var fulfill = Flows.Fulfill(12, 4, 10, take: 3, give: 3);
        var delivery = Tension.Wire(fulfill, [supply.On(pktStore), orders.On(pktStore), receiver.On(pktStore)], Here);
        var changed = delivery.Record();
        var countReceipts = Tension.Wire(Counts.Accumulate(4), [new Binding(delivery.Rows, changed.Changed), receipts.On(pktStore)], Here);
        var pktEngine = new Engine().Add(delivery).Add(countReceipts);
        var trace = new List<int> { receiver.Read(pktStore, 0)!.Value };
        for (var i = 0; i < 4; i++)
        {
            pktEngine.Step();
            trace.Add(receiver.Read(pktStore, 0)!.Value);
            // Total stock conserved every tick: supply + receiver == 12.
            Assert.Equal(12, supply.Read(pktStore, 0) + receiver.Read(pktStore, 0));
        }
        Assert.Equal([0, 3, 6, 9, 9], trace);
        Assert.Equal((3, 1, 9, 3), (supply.Read(pktStore, 0), orders.Read(pktStore, 0), receiver.Read(pktStore, 0), receipts.Read(pktStore, 0)));

        // Contrast with partial-unit delivery (1/step capped by 12 orders): reaches capacity 10 leaving 2 at supply.
        var partStore = new Store(0, 1);
        var pSupply = UnaryField(partStore, 12);
        var pOrders = UnaryField(partStore, 12);
        var pRecv = UnaryField(partStore, 10);
        pSupply.Write(partStore, 0, 12);
        pOrders.Write(partStore, 0, 12);
        var partEngine = new Engine().Add(Tension.Wire(Flows.Fulfill(12, 12, 10, take: 1, give: 1), [pSupply.On(partStore), pOrders.On(partStore), pRecv.On(partStore)], Here));
        Assert.NotNull(partEngine.Settle(32));
        Assert.Equal((2, 2, 10), (pSupply.Read(partStore, 0), pOrders.Read(partStore, 0), pRecv.Read(partStore, 0)));
    }

    /// <summary>X7 (5-row aliasing & Step-vs-Settle contract for doubling B):
    /// read keeps B intact, publish targets a fresh store, apply-once consumes
    /// a 1-shot token (and records shortfall at capacity), feedback doubles per
    /// Step() but saturates under Settle(), and simultaneous B = 2B narrows to
    /// 0 or contradicts B = 5 without ever erasing B.</summary>
    [Fact]
    public void DoublingBDistinguishesReadPublishApplyOnceFeedbackSettleAndSimultaneousConstraint()
    {
        const int Cap = 40;
        var val = Shape.Binary(Cap + 1); // 6 bits, values 0..40

        // Row 1 & 2: Read / Publish — functional law (b [Unchanged], out [written]) -> min(2*b, Cap).
        var readDouble = TransitionLaw.FromFunction(
            [val, val],
            s => [s[0], s[0] * 2 <= Cap ? s[0] * 2 : Cap],
            new Accounting(true, new Unchanged(0)));

        var pubStore = new Store(0, 1);
        var bField = new Field(val, [.. Enumerable.Range(0, val.Bits).Select(_ => pubStore.AddPlane())]);
        var outField = new Field(val, [.. Enumerable.Range(0, val.Bits).Select(_ => pubStore.AddPlane())]);
        bField.Write(pubStore, 0, 5);
        var pubEngine = new Engine().Add(Tension.Wire(readDouble, [bField.On(pubStore), outField.On(pubStore)], Here));
        Assert.NotNull(pubEngine.Settle(8));
        Assert.Equal((5, 10), (bField.Read(pubStore, 0), outField.Read(pubStore, 0)));

        // Row 3: Apply once to B — explicit 1-shot request token consumed as it fires,
        // and explicit receiver capacity (8) leaving shortfall 2 visible.
        var cap8 = Shape.Unary(8);
        var applyOnceCap8 = TransitionLaw.FromFunction(
            [cap8, Shape.Bit, Shape.Unary(8)], // (b, pending, shortfall)
            s => s[1] == 0 ? s : [s[0] * 2 <= 8 ? s[0] * 2 : 8, 0, s[0] * 2 > 8 ? s[0] * 2 - 8 : 0],
            new Accounting(true, new NeverIncreases(1)));
        var onceStore = new Store(0, 1);
        var onceB = UnaryField(onceStore, 8);
        var pending = onceStore.AddPlane();
        var shortfall = UnaryField(onceStore, 8);
        onceB.Write(onceStore, 0, 5);
        onceStore.Write(pending, 0, true);
        var onceEngine = new Engine().Add(Tension.Wire(applyOnceCap8, [onceB.On(onceStore), new Binding(onceStore, pending), shortfall.On(onceStore)], Here));
        Assert.NotNull(onceEngine.Settle(8));
        Assert.Equal((8, false, 2), (onceB.Read(onceStore, 0), onceStore.Read(pending, 0), shortfall.Read(onceStore, 0)));

        // Row 4: Feedback recurrence — Step() produces 5 -> 10 -> 20 -> 40; Settle() saturates straight to 40!
        var doubleInPlace = TransitionLaw.FromFunction(
            [val],
            s => [s[0] * 2 <= Cap ? s[0] * 2 : Cap],
            new Accounting(true, new NeverDecreases(0)));

        var stepStore = new Store(0, 1);
        var stepB = new Field(val, [.. Enumerable.Range(0, val.Bits).Select(_ => stepStore.AddPlane())]);
        stepB.Write(stepStore, 0, 5);
        var stepEngine = new Engine().Add(Tension.Wire(doubleInPlace, [stepB.On(stepStore)], Here));
        var snapshots = new List<int> { stepB.Read(stepStore, 0)!.Value };
        for (var i = 0; i < 3; i++)
        {
            stepEngine.Step();
            snapshots.Add(stepB.Read(stepStore, 0)!.Value);
        }
        Assert.Equal([5, 10, 20, 40], snapshots);

        var settleStore = new Store(0, 1);
        var settleB = new Field(val, [.. Enumerable.Range(0, val.Bits).Select(_ => settleStore.AddPlane())]);
        settleB.Write(settleStore, 0, 5);
        var settleEngine = new Engine().Add(Tension.Wire(doubleInPlace, [settleB.On(settleStore)], Here));
        Assert.NotNull(settleEngine.Settle(16));
        Assert.Equal(40, settleB.Read(settleStore, 0));

        // Row 5: Simultaneous constraint B = 2B at one instant via SameReference.
        var small = Shape.Unary(10);
        var isDouble = RelationLaw.FromPredicate([small, small], t => t[1] == 2 * t[0]);

        var cStore = new Store(0, 1);
        var openB = OpenField(cStore, small);
        var cWiring = new Wiring(isDouble, openB.Variable, openB.Variable);
        cWiring.Add(0, 0);
        var cEngine = new Engine().Add(cWiring);
        Assert.NotNull(cEngine.Settle(8));
        Assert.Equal(1UL << 0, openB.Values(0)); // only solution to B = 2B is 0

        // With B known to be 5, concrete sample B stays 5 while the constraint narrows open support to empty (contradiction).
        var cStore2 = new Store(0, 1);
        var concreteB = UnaryField(cStore2, 10);
        concreteB.Write(cStore2, 0, 5);
        var openB2 = OpenField(cStore2, small);
        openB2.Restrict(0, 1UL << 5);
        var cWiring2 = new Wiring(isDouble, openB2.Variable, openB2.Variable);
        cWiring2.Add(0, 0);
        Assert.NotNull(new Engine().Add(cWiring2).Settle(8));
        Assert.Equal(0UL, openB2.Values(0));
        Assert.Equal(5, concreteB.Read(cStore2, 0)); // contradiction never erases concrete B
    }
}
