using Xunit;

namespace Gojo.EncodingC.Tests;

/// <summary>A step taken together: every row reads the state before the step
/// and the writes land afterwards, so declared order no longer decides what
/// moves, and two rows that would write the same bit are contested rather than
/// ranked.</summary>
public sealed class RuntimeTests
{
    private const int Sites = 5;
    private static readonly Field Direction = new(Sides.Word, 0, 1);

    private static (Engine Engine, Store Path) Road((int From, int To)[] rows, params int[] units)
    {
        var path = new Store(1, Sites);
        foreach (var unit in units) path.Write(0, unit, true);
        var control = new Store(2, 1);
        Direction.Write(control, 0, Sides.With);
        var forward = new Wiring(Paths.Advance(Sides.With), new Binding(path, 0), new Binding(control, 0, 1), new Binding(path, 0));
        foreach (var (from, to) in rows) forward.Add(from, 0, to);
        return (new Engine().Add(forward).Hold(path, control), path);
    }

    private static int[] Units(Store path) => [.. path.Read(0).Indexes()];

    private static readonly (int, int)[] NearFirst = [(0, 1), (1, 2), (2, 3), (3, 4)];
    private static readonly (int, int)[] FarFirst = [(3, 4), (2, 3), (1, 2), (0, 1)];

    /// <summary>Rows declared from the near end chain a unit along the whole
    /// road in one step taken in order, since each row reads what the one
    /// before it wrote. Taken together, a unit moves one site a step whichever
    /// way the rows are declared.</summary>
    [Fact]
    public void TakenTogetherAUnitMovesOneSiteAStepWhateverTheOrder()
    {
        var (inOrder, inOrderPath) = Road(NearFirst, 0);
        inOrder.Step();
        Assert.Equal([4], Units(inOrderPath));

        foreach (var rows in new[] { NearFirst, FarFirst })
        {
            var (engine, path) = Road(rows, 0);
            var trail = new List<int>();
            for (var i = 0; i < 4; i++)
            {
                engine.StepTogether();
                trail.Add(Assert.Single(Units(path)));
            }
            Assert.Equal([1, 2, 3, 4], trail);
        }
    }

    /// <summary>Two units in a line. The one ahead moves into free room; the
    /// one behind finds the site ahead taken, as it was before the step, and
    /// waits a step. Nothing is made or lost.</summary>
    [Fact]
    public void UnitsInALineEachReadTheRoadAsItWas()
    {
        var (engine, path) = Road(NearFirst, 0, 1);

        engine.StepTogether();
        Assert.Equal([0, 2], Units(path));
        engine.StepTogether();
        Assert.Equal([1, 3], Units(path));
    }

    /// <summary>At a fork two rows would take the same unit. Taken in order,
    /// the first declared wins. Taken together, both would clear the same
    /// source bit, so both are contested: the unit stays where it is, the step
    /// says why, and nothing is chosen by order.
    /// <para>The contest is left where laws can read it: a seventh evidence
    /// plane on the rows, set for both rows, and cleared by a step in order,
    /// where declared order chose.</para></summary>
    [Fact]
    public void TwoRowsRacingForOneUnitAreBothContested()
    {
        (int From, int To)[] fork = [(1, 2), (1, 3)];
        var (inOrder, inOrderPath) = Road(fork, 1);
        inOrder.Step();
        Assert.Equal([2], Units(inOrderPath));

        var (engine, path) = Road(fork, 1);
        var moves = engine.Wirings[0];
        var evidence = moves.Record();
        var step = engine.StepTogether();

        Assert.Equal([1], Units(path));
        Assert.Equal(Plane.Of(2, 0, 1), step.Steps[0].Contested);
        Assert.True(step.Steps[0].Changed.None);
        Assert.Equal(Plane.Of(2, 0, 1), moves.Rows.Read(evidence.Contested));

        engine.Step();
        Assert.True(moves.Rows.Read(evidence.Contested).None);
    }

    /// <summary>Narrowing taken together reaches what narrowing in order does,
    /// one position further each step since each row reads the state before
    /// it. Two rows ruling the same value out at one position agree and are not
    /// contested. Settling stops at the first step that changes nothing.</summary>
    [Fact]
    public void NarrowingTakenTogetherSettlesToTheSameValues()
    {
        (Engine, Supports) Chain()
        {
            var store = new Store(1, Sites);
            var bits = Supports.On(store, Field.Bit(0));
            var same = new Wiring(Settling.Same(Shape.Bit), bits.Variable, bits.Variable);
            for (var i = 0; i + 1 < Sites; i++) same.Add(i, i + 1);
            var agree = new Wiring(Settling.Same(Shape.Bit), bits.Variable, bits.Variable);
            agree.Add(0, 1);
            bits.Restrict(0, 0b10);
            return (new Engine().Add(same).Add(agree), bits);
        }

        var (together, settledTogether) = Chain();
        var (inOrder, settledInOrder) = Chain();

        var first = together.StepTogether();
        Assert.True(first.Steps.All(s => s.Contested!.None));
        Assert.True(first.Steps.All(s => s.Changed[0]));
        Assert.Equal(0b10UL, settledTogether.Values(1));
        Assert.Equal(0b11UL, settledTogether.Values(2));

        Assert.Equal(4, together.Settle(16, together: true));
        Assert.Equal(2, inOrder.Settle(16));
        Assert.All(Enumerable.Range(0, Sites), i => Assert.Equal(settledInOrder.Values(i), settledTogether.Values(i)));
        Assert.All(Enumerable.Range(0, Sites), i => Assert.Equal(0b10UL, settledTogether.Values(i)));
    }
}
