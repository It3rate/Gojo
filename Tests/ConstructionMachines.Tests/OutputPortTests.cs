using Gojo.Core;
using Gojo.Experiments.ConstructionMachines;
using static Gojo.Experiments.ConstructionMachines.MachineExpressions;

namespace Gojo.ConstructionMachines.Tests;

public sealed class OutputPortTests
{
    [Fact]
    public void DirectMachineOutputPortsConnectResultsAndPublicRoles()
    {
        var c = new ConstructionRecipes();
        var machine = c.Offsets(c.Zero, Program.Parameter(12), Program.Parameter(8));
        var application = MachineRunner.Run(machine);
        var ports = F(F(machine)[2]); var results = F(F(application)[2]); var bindings = F(F(application)[5]);
        for (var i = 0; i < 4; i++)
        {
            var binding = Assert.IsType<Relation>(bindings[i]);
            Assert.Same(ports[i], binding.Left);
            Assert.Same(results[i], binding.Right);
        }
        var shape = new ShapeInterface(); Assert.True(shape.Publish(application));
        var patch = F(shape.LastAttempt!)[4]; var correspondence = F(F(patch)[6]);
        var portMap = F(correspondence[0]);
        for (var i = 0; i < 4; i++)
        {
            var map = Assert.IsType<Relation>(portMap[i]);
            Assert.Same(ports[i], map.Left);
            Assert.Same(shape.Corner(i), map.Right);
        }
    }

    [Fact]
    public void RepeatedPathOutputExposesOrderedRouteIncludingItsReturnRole()
    {
        var c = new ConstructionRecipes(); var machine = c.Path(c.Zero, c.Point(1, 0),
            [Program.Parameter(12), Program.Parameter(8)], 2);
        var application = MachineRunner.Run(machine);
        var output = Assert.IsType<Relation>(Assert.Single(F(F(application)[5])));
        Assert.Same(Assert.Single(F(F(machine)[2])), output.Left);
        Assert.Same(F(application)[2], output.Right);
        Assert.Equal(5, F(output.Right).Length);
        var shape = new ShapeInterface(); Assert.True(shape.Publish(application));
        var map = Assert.IsType<Relation>(Assert.Single(F(F(F(F(shape.LastAttempt!)[4])[6])[0])));
        Assert.Same(output.Left, map.Left);
        var roles = F(map.Right); Assert.Equal(5, roles.Length);
        Assert.Same(shape.Corner(0), roles[0]); Assert.Same(roles[0], roles[4]);
    }

    [Fact]
    public void AnUnconnectedNumericResultCannotMasqueradeAsAMachineOutput()
    {
        var c = new ConstructionRecipes(); var machine = c.Offsets(c.Zero, Program.Parameter(12), Program.Parameter(8));
        var application = F(MachineRunner.Run(machine)); var outputBindings = F(application[5]);
        var old = Assert.IsType<Relation>(outputBindings[0]);
        outputBindings[0] = new Relation(old.Left, c.Point(0, 0)); // same values, no output identity
        application[5] = G(outputBindings);
        var shape = new ShapeInterface(); Assert.False(shape.Publish(G(application)));
        Assert.Empty(shape.CurrentBindings);
    }

    [Fact]
    public void HistoricalCaseCapturesBothAttachedEndpointsAndCalculatedRidge()
    {
        var c = new ConstructionRecipes(); var w = Program.Parameter(12); var h = Program.Parameter(8);
        var machine = c.Offsets(c.Zero, w, h); var shape = new ShapeInterface();
        Assert.True(shape.Publish(MachineRunner.Run(machine)));
        var roof = Attachments.Roof(c, shape.Side(2), Program.Parameter(4));
        var captured = F(Program.CaptureObservation(G(machine, shape.Surface, Attachments.Read(roof, shape.CurrentBindings))));
        w.Add(8); Assert.True(shape.Publish(MachineRunner.Run(machine)));
        var edges = F(F(captured[2])[2]).Cast<Relation>().ToArray();
        Assert.Equal(new[] { 12.0, 8.0 }, Program.XY(edges[0].Left));
        Assert.Equal(new[] { 6.0, 12.0 }, Program.XY(edges[0].Right));
        Assert.Same(edges[0].Right, edges[1].Left);
        var rerun = MachineRunner.Run(Assert.IsType<Relation>(captured[0]));
        Assert.Equal(new[] { 12.0, 8.0 }, Program.XY(F(F(rerun)[2])[2]));
    }
}
