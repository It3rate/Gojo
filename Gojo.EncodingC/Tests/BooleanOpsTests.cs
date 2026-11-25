using Xunit;

namespace Gojo.EncodingC.Tests;

/// <summary>Phase 1 of GojoOperations-2026-11.md (X13): Boolean operations
/// across the bit rung, unary thermometer planes, 3+ contributors, spatial
/// scopes with four occupancy regions, and the four distinct zero-like states.</summary>
public sealed class BooleanOpsTests
{
    private static readonly Plane Here = Plane.Of(1, 0);

    private static Supports OpenBit(Store store) => Supports.On(store, new Field(Shape.Bit, store.AddPlane()));

    /// <summary>All 16 two-input Boolean relations are one byte each, read in
    /// every direction, matching the canonical byte signatures AND=0x87,
    /// OR=0xE1, XOR=0x69; at the bit rung, AND is carry/product, OR is clamped
    /// sum, XOR is wrapped sum, and HalfAdder joins wrap + carry.</summary>
    [Fact]
    public void SixteenOneByteGatesAndBitRungArithmeticHoldInEveryDirection()
    {
        Assert.Equal([0x87], Booleans.And.Bytes());
        Assert.Equal([0xE1], Booleans.Or.Bytes());
        Assert.Equal([0x69], Booleans.Xor.Bytes());

        for (var mask = 0; mask < 16; mask++)
        {
            var gate = Booleans.Gate(mask);
            Assert.Single(gate.Bytes());
            for (var a = 0; a <= 1; a++)
                for (var b = 0; b <= 1; b++)
                {
                    var expected = (mask >> (a + 2 * b)) & 1;
                    Assert.Equal(1UL << expected, gate.Project(2, [1UL << a, 1UL << b, 0b11]));
                }
        }

        // Backward read on AND: out = 1 forces both a = 1 and b = 1; out = 0 with a = 1 forces b = 0.
        Assert.Equal(1UL << 1, Booleans.And.Project(0, [0b11, 0b11, 1UL << 1]));
        Assert.Equal(1UL << 1, Booleans.And.Project(1, [0b11, 0b11, 1UL << 1]));
        Assert.Equal(1UL << 0, Booleans.And.Project(1, [1UL << 1, 0b11, 1UL << 0]));

        // Bit-rung arithmetic identities: AND == carry/product, OR == clamped sum, XOR == wrapped sum.
        for (var a = 0; a <= 1; a++)
            for (var b = 0; b <= 1; b++)
            {
                Assert.True(Booleans.And.Admits(a, b, (a + b) / 2));
                Assert.True(Booleans.And.Admits(a, b, a * b));
                Assert.True(Booleans.Or.Admits(a, b, a + b > 1 ? 1 : a + b));
                Assert.True(Booleans.Xor.Admits(a, b, (a + b) % 2));
                Assert.True(Booleans.HalfAdder.Admits(a, b, (a + b) & 1, (a + b) >> 1));
            }
    }

    /// <summary>On unary thermometer planes, planewise AND is min, OR is max,
    /// and XOR is the span [min, max) — a valid occupancy segment of length
    /// |a - b| that is NOT a valid unary prefix whenever min > 0.</summary>
    [Fact]
    public void UnaryThermometerBitwiseActsGiveMinMaxAndSpanNotProduct()
    {
        const int Cap = 6;
        var store = new Store(Cap * 5, 1);
        Field UnaryAt(int start) => new(Shape.Unary(Cap), [.. Enumerable.Range(start, Cap)]);
        var (fa, fb, fAnd, fOr, fXor) = (UnaryAt(0), UnaryAt(Cap), UnaryAt(2 * Cap), UnaryAt(3 * Cap), UnaryAt(4 * Cap));

        for (var a = 0; a <= Cap; a++)
            for (var b = 0; b <= Cap; b++)
            {
                fa.Write(store, 0, a);
                fb.Write(store, 0, b);
                var spanCount = 0;
                for (var p = 0; p < Cap; p++)
                {
                    var ba = store.Read(fa.Planes[p], 0);
                    var bb = store.Read(fb.Planes[p], 0);
                    store.Write(fAnd.Planes[p], 0, ba && bb);
                    store.Write(fOr.Planes[p], 0, ba || bb);
                    var bx = ba ^ bb;
                    store.Write(fXor.Planes[p], 0, bx);
                    if (bx) spanCount++;
                }

                var min = a < b ? a : b;
                var max = a > b ? a : b;
                Assert.Equal(min, fAnd.Read(store, 0));
                Assert.Equal(max, fOr.Read(store, 0));
                Assert.Equal(max - min, spanCount);

                // Whenever min(a, b) > 0 and a != b, XOR leaves lower planes 0 and higher planes 1: invalid unary code!
                if (min > 0 && a != b)
                    Assert.Null(fXor.Read(store, 0));
                else
                    Assert.Equal(max - min, fXor.Read(store, 0));
            }
    }

    /// <summary>Spatial Boolean over a declared scope [0..12) with spans [1..7)
    /// and [5..10): partitions into four occupancy regions (neither, A only,
    /// B only, both), routes participation conservatively with residue kept,
    /// and uses Flipped for scope-relative complement.</summary>
    [Fact]
    public void SpatialScopePartitionsIntoFourRegionsAndRoutesConservatively()
    {
        const int Scope = 12;
        var store = new Store(10, Scope);
        // Plane 0: span A [1..7), Plane 1: span B [5..10), Planes 2..5: neither, aOnly, bOnly, both
        // Planes 6..8: unit in, selected (XOR), residue (XNOR)
        for (var i = 1; i < 7; i++) store.Write(0, i, true);
        for (var i = 5; i < 10; i++) store.Write(1, i, true);
        for (var i = 0; i < Scope; i++) store.Write(6, i, true);

        // Open supports for the four regions narrowed by Booleans.FourRegions
        var rNeither = Supports.On(store, new Field(Shape.Bit, 2));
        var rAOnly = Supports.On(store, new Field(Shape.Bit, 3));
        var rBOnly = Supports.On(store, new Field(Shape.Bit, 4));
        var rBoth = Supports.On(store, new Field(Shape.Bit, 5));
        var part = new Wiring(Booleans.FourRegions,
            new Binding(store, 0), new Binding(store, 1),
            rNeither.Variable, rAOnly.Variable, rBOnly.Variable, rBoth.Variable);
        for (var i = 0; i < Scope; i++) part.Add(i, i, i, i, i, i);

        // Conserving XOR router across all 12 scope positions: selected + residue stays 1 everywhere.
        var xorRouter = Tension.Wire(Booleans.ConservingSelect(0b0110),
            [new Binding(store, 0), new Binding(store, 1), new Binding(store, 6), new Binding(store, 7), new Binding(store, 8)],
            Plane.Ones(Scope));

        var engine = new Engine().Add(part).Add(xorRouter);
        Assert.NotNull(engine.Settle(8));

        // Scope [0..12) regions: neither={0,10,11}, A only={1,2,3,4}, B only={7,8,9}, both={5,6}.
        for (var i = 0; i < Scope; i++)
        {
            Assert.Equal(i is 0 or 10 or 11 ? 1UL << 1 : 1UL << 0, rNeither.Values(i));
            Assert.Equal(i is >= 1 and <= 4 ? 1UL << 1 : 1UL << 0, rAOnly.Values(i));
            Assert.Equal(i is >= 7 and <= 9 ? 1UL << 1 : 1UL << 0, rBOnly.Values(i));
            Assert.Equal(i is 5 or 6 ? 1UL << 1 : 1UL << 0, rBoth.Values(i));

            var inXor = i is (>= 1 and <= 4) or (>= 7 and <= 9);
            Assert.False(store.Read(6, i));
            Assert.Equal(inXor, store.Read(7, i));
            Assert.Equal(!inXor, store.Read(8, i));
        }

        // Scope-relative complement via Flipped (no copy): NOT(A) over Live scope [0..12).
        var notA = new Flipped(store, 0);
        var notAPlane = Plane.Of(Scope, Enumerable.Range(0, Scope).Where(i => notA.Read(0, i)));
        Assert.Equal(Plane.Of(Scope, 0, 7, 8, 9, 10, 11), notAPlane);

        // Fredkin gate is both conservative and reversible (self-inverse).
        Assert.True(Booleans.Fredkin.IsReversible);
        Assert.True(Booleans.Fredkin.Accounting.Reversible);
        Assert.True(Booleans.Fredkin.Accounting.Conserves(1));
        Assert.True(Booleans.Fredkin.SameTable(Booleans.Fredkin.Inverse()));
    }

    /// <summary>Three or more Boolean contributors separate all, at-least-two,
    /// exactly-one, and odd parity using master's Counts.Tally plus OddParity,
    /// and keep the four zero-like states (ODV V=0 undetectable, unselected,
    /// empty support contradiction, Boolean 0 absence) distinct.</summary>
    [Fact]
    public void ThreePlusContributorsAndFourZeroStatesStayDistinct()
    {
        var store = new Store(0, 8);
        var b0 = store.AddPlane();
        var b1 = store.AddPlane();
        var b2 = store.AddPlane();
        var tally = new Field(Shape.Unary(3), store.AddPlane(), store.AddPlane(), store.AddPlane());
        var two = new Field(Shape.Unary(3), store.AddPlane(), store.AddPlane(), store.AddPlane());
        var atLeastTwo = store.AddPlane();
        var odd = OpenBit(store);

        // Encode all 8 combinations of (b0, b1, b2) across positions 0..7.
        for (var i = 0; i < 8; i++)
        {
            store.Write(b0, i, (i & 1) != 0);
            store.Write(b1, i, (i & 2) != 0);
            store.Write(b2, i, (i & 4) != 0);
            two.Write(store, i, 2);
        }

        var all8 = Plane.Ones(8);
        var parityWiring = new Wiring(Booleans.OddParity(3), tally.On(store), odd.Variable);
        for (var i = 0; i < 8; i++) parityWiring.Add(i, i);

        var engine = new Engine()
            .Add(Tension.Wire(Counts.Tally(3), [new Binding(store, b0), new Binding(store, b1), new Binding(store, b2), tally.On(store)], all8))
            .Add(Tension.Wire(Counts.AtLeast(3, 3), [tally.On(store), two.On(store), new Binding(store, atLeastTwo)], all8))
            .Add(parityWiring);
        Assert.NotNull(engine.Settle(8));

        for (var i = 0; i < 8; i++)
        {
            var pop = System.Numerics.BitOperations.PopCount((uint)i);
            Assert.Equal(pop, tally.Read(store, i));
            Assert.Equal(pop == 3, store.Read(tally.Planes[2], i));               // all
            Assert.Equal(pop >= 2, store.Read(atLeastTwo, i));                    // at least two
            Assert.Equal(pop == 1, store.Read(tally.Planes[0], i) && !store.Read(tally.Planes[1], i)); // exactly one
            Assert.Equal(1UL << (pop & 1), odd.Values(i));                        // odd parity
        }

        // Four zero-like states stay structurally distinct:
        // 1) ODV undetectable (V=0, positive): two undetectable positives can still combine into detectable +?
        var odvSum = Odv.Add(OdvWord.Of(true, false, false), OdvWord.Of(true, false, false), Odv.Detection);
        Assert.NotEqual(0UL, odvSum & (1UL << OdvWord.Of(true, false, true).Code));
        // 2) Boolean 0 (absent presence): 0 OR 0 stays strictly 0.
        Assert.Equal(1UL << 0, Booleans.Or.Project(2, [1UL << 0, 1UL << 0, 0b11]));
        // 3) Empty support (contradiction): mask 0, distinct from settled 0 (mask 0b01).
        Assert.Equal(0UL, Booleans.And.Project(0, [1UL << 0, 0b11, 1UL << 1]));
        Assert.NotEqual(0UL, 1UL << 0);
        // 4) Unselected lane (Attention = 0): row does not act and leaves target untouched.
        var attStore = new Store(3, 2);
        attStore.Write(0, 0, true);
        attStore.Write(0, 1, true);
        var gated = new Wiring(Flows.Move(1, 1), new Binding(attStore, 0), new Binding(attStore, 1));
        gated.Add(0, 0);
        gated.Add(1, 1);
        var att = gated.Attend();
        gated.Rows.Write(att, 1, false); // position 1 unselected (attention = 0)
        new Engine().Add(gated).Step();
        Assert.Equal((false, true), (attStore.Read(0, 0), attStore.Read(1, 0)));
        Assert.Equal((true, false), (attStore.Read(0, 1), attStore.Read(1, 1)));
    }
}
