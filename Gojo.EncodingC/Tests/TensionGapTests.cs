using Xunit;

namespace Gojo.EncodingC.Tests;

/// <summary>Two-faced directional mismatch: exact small-width
/// <see cref="TensionLaws.Gap"/> relations and scalable bit-level
/// <see cref="TensionLaws.MismatchFold"/> observers.</summary>
public sealed class TensionGapTests
{
    /// <summary>The two-faced gap relation keeps deficit and excess as valid
    /// unary counts (never both nonzero), narrows open supports in every
    /// direction, and swaps deficit with excess when have and want swap.</summary>
    [Fact]
    public void TwoFacedGapSeparatesDeficitAndExcessAndNarrowsBothWays()
    {
        var gap = TensionLaws.Gap(4);
        const ulong all = 0b11111;

        // have=1, want=3 => deficit=2, excess=0.
        Assert.Equal(1UL << 2, gap.Project(2, [1UL << 1, 1UL << 3, all, all]));
        Assert.Equal(1UL << 0, gap.Project(3, [1UL << 1, 1UL << 3, all, all]));

        // Swapping have and want (reorienting the comparison) swaps deficit and excess.
        Assert.Equal(1UL << 0, gap.Project(2, [1UL << 3, 1UL << 1, all, all]));
        Assert.Equal(1UL << 2, gap.Project(3, [1UL << 3, 1UL << 1, all, all]));

        // Backward narrowing: want=3 and deficit=2 settles have=1.
        Assert.Equal(1UL << 1, gap.Project(0, [all, 1UL << 3, 1UL << 2, 1UL << 0]));

        // Open have in {1, 2} against want=3 leaves deficit in {1, 2} and excess=0.
        Assert.Equal((1UL << 1) | (1UL << 2), gap.Project(2, [(1UL << 1) | (1UL << 2), 1UL << 3, all, all]));
        Assert.Equal(1UL << 0, gap.Project(3, [(1UL << 1) | (1UL << 2), 1UL << 3, all, all]));

        // Zero gap (deficit=0, excess=0) forces have == want.
        Assert.Equal(1UL << 4, gap.Project(0, [all, 1UL << 4, 1UL << 0, 1UL << 0]));
    }

    /// <summary>A plant with capacity 10 (too wide for a single 4-role gap
    /// table) folds <c>(have, want)</c> bit by bit into <c>(needMore, haveExtra)</c>
    /// and wires those two bits straight into the inlet and outlet valves. No
    /// steering law ever reads the wide counts directly.</summary>
    [Fact]
    public void MismatchFoldSteersAWidePlantDirectlyFromTwoDirectionBits()
    {
        const int Cap = 10;
        // Store layout: planes 0..9 count, plane 10 valveIn (needMore), plane 11 valveOut (haveExtra).
        // Positions: 0 reservoir, 1 tank, 2 drain.
        var plant = new Store(Cap + 2, 3);
        var count = new Field(Shape.Unary(Cap), Enumerable.Range(0, Cap).ToArray());
        count.Write(plant, 0, 8);
        count.Write(plant, 1, 2);

        var goal = new Store(Cap, 3);
        count.Write(goal, 1, 6);

        var engine = new Engine();
        Tension.ObserveMismatch(engine, plant, count, goal, count, plant, needMore: Cap, haveExtra: Cap + 1, positions: 1);

        var flow = new Wiring(
            Flows.GatedMove(Cap, Cap),
            new Binding(plant, [.. count.Planes]),
            new Binding(plant, Cap),
            new Binding(plant, [.. count.Planes]));
        flow.Add(0, 1, 1); // reservoir -> tank while needMore (plane Cap) is 1
        var drain = new Wiring(
            Flows.GatedMove(Cap, Cap),
            new Binding(plant, [.. count.Planes]),
            new Binding(plant, Cap + 1),
            new Binding(plant, [.. count.Planes]));
        drain.Add(1, 1, 2); // tank -> drain while haveExtra (plane Cap + 1) is 1
        engine.Add(flow).Add(drain);

        var saved = Transport.Save(engine, plant, goal);
        var (restored, copies) = Transport.Load(saved);

        engine.Settle(16);
        Assert.Equal((4, 6, 0), (count.Read(plant, 0), count.Read(plant, 1), count.Read(plant, 2)));
        Assert.False(plant.Read(Cap, 1));
        Assert.False(plant.Read(Cap + 1, 1));

        // Lower the goal to 3: haveExtra opens the drain valve until tank == 3.
        count.Write(goal, 1, 3);
        engine.Settle(16);
        Assert.Equal((4, 3, 3), (count.Read(plant, 0), count.Read(plant, 1), count.Read(plant, 2)));

        // Restored machine behaves identically.
        restored.Settle(16);
        Assert.Equal((4, 6, 0), (count.Read(copies[0], 0), count.Read(copies[0], 1), count.Read(copies[0], 2)));
    }

    /// <summary>For valid unary counts, <c>needMore</c> and <c>haveExtra</c>
    /// are never both 1. A broken unary pattern (such as <c>010</c> against
    /// <c>001</c>) sets both bits, exposing the noise instead of faking a
    /// direction.</summary>
    [Fact]
    public void MismatchFoldDetectsABrokenUnaryCountAsBothDirectionsSet()
    {
        var store = new Store(8, 1);
        var have = new Field(Shape.Unary(3), 0, 1, 2);
        var want = new Field(Shape.Unary(3), 3, 4, 5);
        store.Write(1, 0, true); // have = 010 (broken)
        want.Write(store, 0, 1); // want = 001 (valid 1)

        var engine = new Engine();
        Tension.ObserveMismatch(engine, store, have, store, want, store, needMore: 6, haveExtra: 7, positions: 0);
        engine.Step();

        Assert.True(store.Read(6, 0));
        Assert.True(store.Read(7, 0));
    }
}
