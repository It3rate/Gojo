using Xunit;

namespace Gojo.EncodingC.Tests;

/// <summary>A coasting multiplier with a material account (ops_claude
/// GojoOperations-2026-11.md, §5.1 and §4; experiment X5). A clock of ticks
/// counts down, one tick a step, into an elapsed count. While it has a tick
/// left, a booking writes the rate over the supply's debt, heap bound in place:
/// rate times time, held as an obligation. A delivery pays that debt one unit
/// a step from the supply's stock into the target. The product is formed by the
/// clock and settled by material: at every step the target plus the debt is
/// the rate times the ticks elapsed, and once settled the target holds what
/// the supply could cover and the rest stays as debt.</summary>
public sealed class CoastingMultiplierTests
{
    private const int RateCapacity = 3, Ticks = 3, Owed = RateCapacity * Ticks, Stock = 10;

    private static readonly Plane Here = Plane.Of(1, 0);

    private static Field Count(Store store, int capacity, int value = 0)
    {
        var field = new Field(Shape.Unary(capacity), [.. Enumerable.Range(0, capacity).Select(_ => store.AddPlane())]);
        field.Write(store, 0, value);
        return field;
    }

    private sealed record Coast(Store Store, Field Rate, Field Left, Field Elapsed, Tension Supply, Field Target, Engine Engine)
    {
        public (int Left, int Elapsed, int Target, int Debt, int Stock) Read() =>
            (Left.Read(Store, 0)!.Value, Elapsed.Read(Store, 0)!.Value, Target.Read(Store, 0)!.Value,
             Supply.Minus.Read(Store, 0)!.Value, Supply.Value.Read(Store, 0)!.Value);
    }

    /// <summary>Booking, then the clock, then delivery, in declared order.</summary>
    private static Coast Build(int rate, int stock)
    {
        var store = new Store(0, 1);
        Field rateCount = Count(store, RateCapacity, rate), left = Count(store, Ticks, Ticks), elapsed = Count(store, Ticks);
        var supply = new Tension(store, Count(store, Stock, stock), Owed);
        var target = Count(store, Owed);

        // debt + rate = debt', written over the debt.
        Shape debt = Shape.Unary(Owed), per = Shape.Unary(RateCapacity);
        var book = Operations.Bind(Operations.Balance([debt, per, debt], 1, 1, -1), [per, debt], [1, 0, -1], [-1, -1, 1]);
        var engine = new Engine()
            .Add(Tension.Wire(book, [rateCount.On(store), supply.Minus.On(store)], Here, new Binding(store, left.Planes[0])))
            .Add(Tension.Route(left.On(store), elapsed.On(store), Here))
            .Add(supply.Deliver(target.On(store), Here));
        return new Coast(store, rateCount, left, elapsed, supply, target, engine);
    }

    private static List<(int Left, int Elapsed, int Target, int Debt, int Stock)> Run(Coast coast)
    {
        var seen = new List<(int, int, int, int, int)> { coast.Read() };
        for (var i = 0; i < 12; i++)
        {
            coast.Engine.Step();
            seen.Add(coast.Read());
        }
        Assert.NotNull(coast.Engine.Settle(2));
        return seen;
    }

    /// <summary>Three a tick for three ticks, from a stock of 10: the clock
    /// runs out after three steps with 9 booked; delivery catches up a unit a
    /// step, leaving 9 in the target, 1 in stock and nothing owed. Time is
    /// conserved by the clock, and at every step target plus debt is the rate
    /// times the ticks elapsed.</summary>
    [Fact]
    public void TheClockBooksTheProductAndDeliverySettlesIt()
    {
        var seen = Run(Build(rate: 3, stock: 10));
        Assert.Equal((3, 0, 0, 0, 10), seen[0]);
        Assert.Equal((0, 3, 3, 6, 7), seen[3]);
        Assert.Equal((0, 3, 9, 0, 1), seen[^1]);
        Assert.All(seen, s => Assert.Equal(Ticks, s.Left + s.Elapsed));
        Assert.All(seen, s => Assert.Equal(3 * s.Elapsed, s.Target + s.Debt));
        Assert.All(seen, s => Assert.Equal(Stock, s.Stock + s.Target));
    }

    /// <summary>From a stock of 7 the supply runs dry with 2 still owed: the
    /// shortfall is debt on the supply, evidence of what could not be
    /// delivered, not a smaller product.</summary>
    [Fact]
    public void AShortSupplyLeavesTheRestAsDebt()
    {
        var seen = Run(Build(rate: 3, stock: 7));
        Assert.Equal((0, 3, 7, 2, 0), seen[^1]);
        Assert.All(seen, s => Assert.Equal(3 * s.Elapsed, s.Target + s.Debt));
    }

    /// <summary>The rate is a count the booking reads, so a structure can move
    /// it: at 2 a tick the same clock books and delivers 6.</summary>
    [Fact]
    public void TheRateIsACountThatCanBeMoved()
    {
        Assert.Equal((0, 3, 6, 0, 4), Run(Build(rate: 2, stock: 10))[^1]);
        Assert.Equal((0, 3, 0, 0, 10), Run(Build(rate: 0, stock: 10))[^1]);
    }
}
