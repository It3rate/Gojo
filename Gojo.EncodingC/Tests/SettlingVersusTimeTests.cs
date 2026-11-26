using Xunit;

namespace Gojo.EncodingC.Tests;

/// <summary>Settling is not advancing time (from ops_sol;
/// Gojo/docs/GojoOperationsSynthesis-2026-11.md, §3.1). "Double B" is one balance,
/// 2·B = B', read four ways. As a relation over one position read twice it
/// admits only zero. Written over B once a step, it doubles once a tick. Run by
/// Settle, which steps until nothing changes, it doubles until the count has no
/// room and then holds: quiet, but not met. Written into a new count from B as
/// it stands, it settles in one step with the relation met. So quiet means
/// nothing more can change, not that what was asked holds; feedback needs its
/// time boundary stated, by a step, a captured before-state or a new count.</summary>
public sealed class SettlingVersusTimeTests
{
    private const int Capacity = 40;

    private static readonly Plane Here = Plane.Of(1, 0);

    private static readonly Shape Count = Shape.Unary(Capacity);

    /// <summary>2·B = B'.</summary>
    private static readonly RelationLaw Doubled = Operations.Balance([Count, Count], 2, -1);

    private static ulong Values(params int[] values) => values.Aggregate(0UL, (m, v) => m | 1UL << v);

    private static (Store Store, Field B, Field D) Site(int b)
    {
        var store = new Store(0, 1);
        Field Add(int value)
        {
            var field = new Field(Count, [.. Enumerable.Range(0, Capacity).Select(_ => store.AddPlane())]);
            field.Write(store, 0, value);
            return field;
        }
        return (store, Add(b), Add(0));
    }

    /// <summary>Read on two positions, the balance says 10 is double 5. Read
    /// on one position twice, it admits only 0, and with 0 ruled out nothing.</summary>
    [Fact]
    public void AsARelationOnOnePositionOnlyZeroIsDoubleItself()
    {
        var any = (1UL << Count.Size) - 1;
        Assert.Equal(Values(5), Doubled.Project(0, [any, Values(10)]));
        Assert.Equal(Values(0), Doubled.Project(0, [any, any], [0, 0]));
        Assert.Equal(0UL, Doubled.Project(0, [any & ~1UL, any], [0, 0]));
    }

    /// <summary>Written over B, once a step: 5, 10, 20, 40, and then the count
    /// has no room for 80 and the step holds.</summary>
    [Fact]
    public void WrittenOverItselfOnceAStepItDoublesOnceATick()
    {
        var (store, b, _) = Site(5);
        var engine = new Engine().Add(Tension.Wire(Operations.Bind(Doubled, [Count], [0, -1], [-1, 0]), [b.On(store)], Here));
        var seen = new List<int> { b.Read(store, 0)!.Value };
        for (var i = 0; i < 4; i++)
        {
            engine.Step();
            seen.Add(b.Read(store, 0)!.Value);
        }
        Assert.Equal([5, 10, 20, 40, 40], seen);
    }

    /// <summary>Under Settle the same law runs until nothing changes. From 5
    /// that is 40, where 80 has no room: the structure is quiet and the balance
    /// on one position is unmet. Only from 0 is quiet also met. A blocked bit
    /// written by the binding tells the two apart in the structure itself: 1
    /// where the last step held, 0 where it wrote a completion.</summary>
    [Fact]
    public void UnderSettleItGoesQuietOnlyWhereItIsBlocked()
    {
        var inPlace = Operations.Bind(Doubled, [Count, Shape.Bit], [0, -1], [-1, 0], blocked: 1);
        foreach (var (start, end, held) in new[] { (5, 40, 1), (3, 24, 1), (0, 0, 0) })
        {
            var (store, b, _) = Site(start);
            var bit = store.AddPlane();
            var engine = new Engine().Add(Tension.Wire(inPlace, [b.On(store), new Binding(store, bit)], Here));
            Assert.NotNull(engine.Settle(10));
            var settled = b.Read(store, 0)!.Value;
            Assert.Equal(end, settled);
            Assert.Equal(settled == 0, Doubled.Admits(settled, settled));
            Assert.Equal(held == 1, store.Read(bit, 0));
        }
    }

    /// <summary>Written into a new count from B as it stands, double settles in
    /// one step and the balance is met: the before-state is captured by giving
    /// the result its own place. Where the new count has no room, the step
    /// holds and the balance between the two is unmet.</summary>
    [Fact]
    public void WrittenIntoANewCountItSettlesAtOnce()
    {
        var forward = Operations.Bind(Doubled, [Count, Count], [0, -1], [-1, 1]);
        foreach (var (start, end) in new[] { (5, 10), (30, 0) })
        {
            var (store, b, d) = Site(start);
            var engine = new Engine().Add(Tension.Wire(forward, [b.On(store), d.On(store)], Here));
            engine.Step();
            Assert.Equal((start, end), (b.Read(store, 0)!.Value, d.Read(store, 0)!.Value));
            Assert.Equal(start <= Capacity / 2, Doubled.Admits(start, end));
            Assert.NotNull(engine.Settle(1));
        }
    }
}
