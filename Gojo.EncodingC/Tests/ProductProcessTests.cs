using Xunit;

namespace Gojo.EncodingC.Tests;

/// <summary>The 3 × 4 comparison as processes (from ops_sol; ops_claude
/// GojoOperations-2026-11.md, §5.2). Pairing: units of two sides arrive from
/// outside as pressure and are absorbed one a step; the product of the sides is
/// read forward from the product relation, and a sweep that keeps a receipt of
/// the area each unit adds records how the product grew. Packets: a delivery
/// takes whole packets against booked orders into a dock that may be too small,
/// and the books say what is still owed.</summary>
public sealed class ProductProcessTests
{
    private static readonly Plane Here = Plane.Of(1, 0);

    private static Field Count(Store store, int capacity) =>
        new(Shape.Unary(capacity), [.. Enumerable.Range(0, capacity).Select(_ => store.AddPlane())]);

    private static ulong Values(params int[] values) => values.Aggregate(0UL, (m, v) => m | 1UL << v);

    /// <summary>Arrivals by tick: (units of A, units of B). Finishing A first,
    /// arriving together while both have units, and alternating A then B.</summary>
    private static readonly (int A, int B)[] Ell = [(1, 0), (1, 0), (1, 0), (0, 1), (0, 1), (0, 1), (0, 1)];
    private static readonly (int A, int B)[] Diagonal = [(1, 1), (1, 1), (1, 1), (0, 1)];
    private static readonly (int A, int B)[] Serialized = [(1, 0), (0, 1), (1, 0), (0, 1), (1, 0), (0, 1), (0, 1)];

    /// <summary>Runs one schedule. With receipts, each side is absorbed by a
    /// sweep; without, by the plain move. Returns the sides, the product read,
    /// and the two receipts.</summary>
    private static (int A, int B, int Product, int SweptA, int SweptB) Pair((int A, int B)[] schedule, bool receipts)
    {
        var store = new Store(0, 1);
        var a = new Tension(store, Count(store, 3), 3);
        var b = new Tension(store, Count(store, 4), 4);
        Field product = Count(store, 12), sweptA = Count(store, 12), sweptB = Count(store, 12);
        var times = Operations.Product(3, 4);
        var engine = new Engine()
            .Add(Tension.Wire(Operations.Bind(times, times.Roles, [0, 1, -1], [-1, -1, 2]),
                [a.Value.On(store), b.Value.On(store), product.On(store)], Here))
            .Add(receipts
                ? Tension.Wire(Flows.Sweep(3, 3, 4, 12), [a.Plus.On(store), a.Value.On(store), b.Value.On(store), sweptA.On(store)], Here)
                : a.Absorb(Here))
            .Add(receipts
                ? Tension.Wire(Flows.Sweep(4, 4, 3, 12), [b.Plus.On(store), b.Value.On(store), a.Value.On(store), sweptB.On(store)], Here)
                : b.Absorb(Here));

        foreach (var (unitsA, unitsB) in schedule)
        {
            for (var i = 0; i < unitsA; i++) Assert.True(a.Receive(Here).Blocked.None);
            for (var i = 0; i < unitsB; i++) Assert.True(b.Receive(Here).Blocked.None);
            Assert.All(engine.StepTogether().Steps, s => Assert.True(s.Contested!.None));
        }
        Assert.NotNull(engine.Settle(4, together: true));
        return (a.Value.Read(store, 0)!.Value, b.Value.Read(store, 0)!.Value, product.Read(store, 0)!.Value,
            sweptA.Read(store, 0)!.Value, sweptB.Read(store, 0)!.Value);
    }

    /// <summary>Every schedule ends at 3 and 4 with a product of 12. With
    /// receipts the split appears: A's arrivals sweep 0, 3 and 3 under the
    /// three schedules, B's 12, 6 and 9. The corners, units present only where
    /// both sides arrived in one step, are the product's residue after both
    /// receipts, read back through the balance: 0, 3 and 0. Without receipts
    /// the same 12 is all there is, and nothing tells the schedules apart.</summary>
    [Fact]
    public void ReceiptsRecordHowTheProductGrewAndPlainMovesForgetIt()
    {
        var split = Shape.Unary(12);
        var parts = Operations.Balance([split, split, split, split], 1, -1, -1, -1);
        int Corners(int product, int sweptA, int sweptB) =>
            System.Numerics.BitOperations.TrailingZeroCount(parts.Project(3, [Values(product), Values(sweptA), Values(sweptB), (1UL << 13) - 1]));

        var seen = new[] { Ell, Diagonal, Serialized }.Select(s => Pair(s, receipts: true)).ToArray();
        Assert.All(seen, r => Assert.Equal((3, 4, 12), (r.A, r.B, r.Product)));
        Assert.Equal([(0, 12), (3, 6), (3, 9)], seen.Select(r => (r.SweptA, r.SweptB)));
        Assert.Equal([0, 3, 0], seen.Select(r => Corners(r.Product, r.SweptA, r.SweptB)));

        var plain = new[] { Ell, Diagonal, Serialized }.Select(s => Pair(s, receipts: false)).ToArray();
        Assert.All(plain, r => Assert.Equal((3, 4, 12, 0, 0), r));
    }

    /// <summary>A receipt observes and never holds material (ops_sol's probe):
    /// with one unit of pressure, an empty target and the other side at 2, the
    /// unit moves whether the receipt has room or not. A receipt without room
    /// fills to its capacity and stays there; the move is the same as a plain
    /// move's.</summary>
    [Fact]
    public void ARecordNeverHoldsTheMove()
    {
        var sweep = Flows.Sweep(3, 3, 4, 3);
        int[] After(int receipt) => sweep.Tuple(Assert.Single(sweep.Afters(sweep.TupleIndex([1, 0, 2, receipt]))));
        Assert.Equal([0, 1, 2, 2], After(0));
        Assert.Equal([0, 1, 2, 3], After(2));
        Assert.Equal([0, 1, 2, 3], After(3));
        var move = Flows.Move(3, 3);
        Assert.Equal(move.Tuple(Assert.Single(move.Afters(move.TupleIndex([1, 0])))), After(3)[..2]);
    }

    /// <summary>The same schedule run again gives the same receipts: a
    /// recorded split and a replayed one agree.</summary>
    [Fact]
    public void AReplayedScheduleSweepsTheSameReceipts() =>
        Assert.Equal(Pair(Diagonal, receipts: true), Pair(Diagonal, receipts: true));

    /// <summary>Packets (from ops_sol): a supply of 12, four booked orders of
    /// one three-unit packet, and four opportunities. Into a dock of 10 the
    /// fourth packet waits: supply 3, one order open, 9 in the dock. Into a dock
    /// of 12 all four arrive. Supply plus dock and three per order plus dock
    /// stay 12 throughout. The product relation still reads 12; the gap to the
    /// 9 delivered is 3, which the books hold as one open packet.</summary>
    [Fact]
    public void PacketsIntoASmallDockLeaveOneOrderOpen()
    {
        (int Source, int Order, int Dock)[] Run(int dockCapacity)
        {
            var store = new Store(0, 1);
            Field source = Count(store, 12), order = Count(store, 4), dock = Count(store, dockCapacity);
            source.Write(store, 0, 12);
            order.Write(store, 0, 4);
            var engine = new Engine().Add(Tension.Wire(Flows.Fulfill(12, 4, dockCapacity, take: 3, give: 3),
                [source.On(store), order.On(store), dock.On(store)], Here));
            return [.. Enumerable.Range(0, 4).Select(_ =>
            {
                engine.Step();
                return (source.Read(store, 0)!.Value, order.Read(store, 0)!.Value, dock.Read(store, 0)!.Value);
            })];
        }

        var small = Run(10);
        Assert.Equal([(9, 3, 3), (6, 2, 6), (3, 1, 9), (3, 1, 9)], small);
        Assert.Equal((0, 0, 12), Run(12)[^1]);
        Assert.All(small, r => Assert.Equal((12, 12), (r.Source + r.Dock, 3 * r.Order + r.Dock)));

        Assert.Equal(Values(12), Operations.Product(3, 4).Project(2, [Values(3), Values(4), (1UL << 13) - 1]));
        var gap = Counts.Shortfall(10, 12);
        Assert.Equal(3 * small[^1].Order, gap.Tuple(Assert.Single(gap.Afters(gap.TupleIndex([small[^1].Dock, 12, 0]))))[2]);
    }
}
