using Gojo.Core;
using Gojo.Experiments.ConstructionMachines;
using static Gojo.Experiments.ConstructionMachines.MachineExpressions;

namespace Gojo.ConstructionMachines.Tests;

public sealed class OpenConstructionTests
{
    [Fact]
    public void UnmeasuredSpanRolesCanBeBoundWithoutChangingTheRecipe()
    {
        var c = new ConstructionRecipes(); var origin = new Placeholder();
        var width = new Placeholder(); var height = new Placeholder();
        var w = Program.Parameter(12); var h = Program.Parameter(8);
        var recipe = c.Offsets(origin, width, w, height, h);
        var facade = new ShapeInterface();
        Assert.False(facade.Publish(MachineRunner.Run(recipe)));
        Assert.Empty(facade.CurrentBindings);
        var binding = new[] { new Relation(origin, c.Point(10, 20)), new Relation(width, w.ValueSlot), new Relation(height, h.ValueSlot) };
        Assert.True(facade.Publish(MachineRunner.Run(recipe, binding)));
        Assert.Same(recipe, facade.Selected);
        Assert.Equal(new[] { 22.0, 28.0 }, Program.XY(facade.CurrentBindings[2].Right));
        w.Add(4);
        Assert.True(facade.Publish(MachineRunner.Run(recipe, binding)));
        Assert.Equal(new[] { 26.0, 28.0 }, Program.XY(facade.CurrentBindings[2].Right));
    }

    [Fact]
    public void SameWidthChangeHasDifferentAnchoringInDifferentConstructions()
    {
        var c = new ConstructionRecipes(); var w = Program.Parameter(8); var h = Program.Parameter(4);
        var halfW = Program.Parameter(4); var halfH = Program.Parameter(2);
        var path = c.Path(c.Zero, c.Point(1, 0), [w, h], 2);
        var centre = c.OppositePairs(c.Point(4, 2), halfW, halfH);
        w.Add(4); halfW.Add(2);
        var p = F(F(MachineRunner.Run(path))[2]); var q = F(F(MachineRunner.Run(centre))[2]);
        Assert.Equal(new[] { 0.0, 0.0 }, Program.XY(p[0]));
        Assert.Equal(new[] { 12.0, 0.0 }, Program.XY(p[1]));
        Assert.Equal(new[] { -2.0, 0.0 }, Program.XY(q[0]));
        Assert.Equal(new[] { 10.0, 0.0 }, Program.XY(q[1]));
    }

    [Fact]
    public void FiveLengthBeatsWithUnresolvedTurnDoNotInventAPentagon()
    {
        var c = new ConstructionRecipes(); var turn = new Placeholder();
        var recipe = c.Path(c.Zero, c.Point(1, 0), [Program.Parameter(4), Program.Parameter(8),
            Program.Parameter(4), Program.Parameter(4), Program.Parameter(8)], 1, turn);
        var read = MachineRunner.Run(recipe);
        Assert.NotEmpty(F(F(read)[4]));
        Assert.False(new ShapeInterface().Publish(read));
        Assert.False(MachineRunner.TryNumbers(F(F(read)[2])[^1], out _));
    }
}
