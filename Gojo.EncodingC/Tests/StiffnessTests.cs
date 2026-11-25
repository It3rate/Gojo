using Xunit;

namespace Gojo.EncodingC.Tests;

/// <summary>How hard a rate is to change follows from where it sits
/// (ops_claude/GojoOperations-2026-11.md, §10; experiment X14). The same
/// coasting booking, rate times three ticks written over a debt, reads its
/// rate from three places. A count in the store moves with any write, and a
/// workspace can suppose another value without touching the store. Constant
/// planes, one bit shared by every position, refuse an ordinary write and a
/// workspace's supposition alike, and move only when the store itself changes
/// the constant, for every position at once. A weight in the law's table has no
/// plane at all: only another law, with another table, books at another rate.</summary>
public sealed class StiffnessTests
{
    private const int Ticks = 3, Owed = 9;

    private static readonly Plane Here = Plane.Of(1, 0);

    private static readonly Shape Debt = Shape.Unary(Owed), Rate = Shape.Unary(3);

    /// <summary>debt + rate = debt', written over the debt: a rate read from a count.</summary>
    private static readonly TransitionLaw ReadRate = Operations.Bind(Operations.Balance([Debt, Rate, Debt], 1, 1, -1), [Rate, Debt], [1, 0, -1], [-1, -1, 1]);

    /// <summary>debt + weight · one = debt': a rate held as a weight in the table,
    /// read against a constant one.</summary>
    private static TransitionLaw Weighted(int weight) =>
        Operations.Bind(Operations.Balance([Debt, Shape.Bit, Debt], 1, weight, -1), [Shape.Bit, Debt], [1, 0, -1], [-1, -1, 1]);

    private static Field Count(Store store, int capacity, int value = 0)
    {
        var field = new Field(Shape.Unary(capacity), [.. Enumerable.Range(0, capacity).Select(_ => store.AddPlane())]);
        field.Write(store, 0, value);
        return field;
    }

    /// <summary>A booking reading <paramref name="rate"/> under a clock of
    /// three ticks. Each call winds the clock back, clears the debt, runs it
    /// to quiet and returns the debt booked.</summary>
    private static Func<int> Coasting(Store store, TransitionLaw book, Binding rate)
    {
        Field left = Count(store, Ticks), elapsed = Count(store, Ticks), debt = Count(store, Owed);
        var engine = new Engine()
            .Add(Tension.Wire(book, [rate, debt.On(store)], Here, new Binding(store, left.Planes[0])))
            .Add(Tension.Route(left.On(store), elapsed.On(store), Here));
        return () =>
        {
            left.Write(store, 0, Ticks);
            elapsed.Write(store, 0, 0);
            debt.Write(store, 0, 0);
            Assert.NotNull(engine.Settle(Ticks + 2));
            return debt.Read(store, 0)!.Value;
        };
    }

    [Fact]
    public void ARateInACountMovesWithAnyWrite()
    {
        var store = new Store(0, 1);
        var rate = Count(store, 3, 3);
        var coast = Coasting(store, ReadRate, rate.On(store));
        Assert.Equal(9, coast());

        var supposed = new Workspace(store);
        supposed.Write(rate.Planes[2], 0, false);
        Assert.Equal(2, rate.Read(supposed, 0));
        Assert.Equal(3, rate.Read(store, 0));

        rate.Write(store, 0, 2);
        Assert.Equal(6, coast());
    }

    [Fact]
    public void ARateOnConstantPlanesMovesOnlyWhenTheStoreChangesIt()
    {
        var store = new Store(0, 1);
        var rate = new Field(Rate, [.. Enumerable.Range(0, 3).Select(_ => store.AddConstantPlane(true))]);
        var coast = Coasting(store, ReadRate, rate.On(store));
        Assert.Equal(9, coast());

        Assert.Throws<ArgumentException>(() => rate.Write(store, 0, 2));
        var supposed = new Workspace(store);
        Assert.Throws<InvalidOperationException>(() => supposed.Write(rate.Planes[2], 0, false));
        Assert.Equal(3, rate.Read(supposed, 0));

        store.SetConstant(rate.Planes[2], false);
        Assert.Equal(6, coast());
    }

    [Fact]
    public void ARateInATableMovesOnlyWithAnotherLaw()
    {
        int CoastAt(int weight)
        {
            var store = new Store(0, 1);
            return Coasting(store, Weighted(weight), new Binding(store, store.AddConstantPlane(true)))();
        }
        Assert.Equal(9, CoastAt(3));
        Assert.Equal([Shape.Bit, Debt], Weighted(3).Roles);
        Assert.Equal(6, CoastAt(2));
        Assert.False(Weighted(3).SameTable(Weighted(2)));
    }
}
