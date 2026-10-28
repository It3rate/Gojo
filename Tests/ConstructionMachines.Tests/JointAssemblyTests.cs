using Gojo.Core;
using Gojo.Encoding;
using Gojo.Experiments.ConstructionMachines;
using Gojo.Structures;
using static Gojo.Experiments.ConstructionMachines.MachineExpressions;

namespace Gojo.ConstructionMachines.Tests;

public sealed class JointAssemblyTests
{
    [Fact]
    public void TheRigidJointUsesASharedReachAndConstructedTurnRatherThanIndependentPointSamples()
    {
        var r = new JointRecipes(); var definition = r.Tee();
        var before = MachineRunner.Run(JointGraph.Machine(definition));
        Point(F(F(before)[2])[0], 20, 100);
        r.Reach.Add(20);
        var after = MachineRunner.Run(JointGraph.Machine(definition));
        Point(F(F(after)[2])[0], 0, 100);
        Point(F(F(after)[2])[1], 200, 100);
        Point(F(F(after)[2])[2], 100, 0);
        Point(F(F(after)[2])[3], 100, 0);
        Assert.Equal(0L, r.Reach.Tension);
    }
    [Fact]
    public void AFixedConstructionRetainsDiagonalDemandWithoutMovingOrReclassifying()
    {
        var r = new JointRecipes(); var joint = new JointAssembly(r.Tee(), G());
        var target = new NegotiatingMachine(JointGraph.Machine(r.Fan(Program.Parameter(40))));
        var world = World(joint, target);
        var definition = joint.Definition; var publication = joint.Mechanical.Publish(joint.Mechanical.Port(0, 1));

        for (var i = 0; i < 3; i++) joint.Step(world);

        Point(joint.Mechanical.Point(0), 20, 100);
        Point(joint.Mechanical.Point(1), 180, 100);
        Assert.Same(definition, joint.Definition);
        Assert.Equal(3, JointGraph.Edges(joint.Definition).Length);
        Assert.Equal(80, Gap(world));
        Assert.NotSame(publication, joint.Mechanical.Publish(joint.Mechanical.Port(0, 1)));
    }

    [Fact]
    public void RelaxationPreservesTopologyAndPublicSitesButAllowsNewCoordinatedMotion()
    {
        var r = new JointRecipes(); var joint = new JointAssembly(r.Tee(), G()); var rise = Program.Parameter(0);
        var target = new NegotiatingMachine(JointGraph.Machine(r.Fan(Program.Parameter(40)))); var world = World(joint, target);
        var points = joint.Mechanical.Points.ToArray(); var ports = joint.Mechanical.Ports.ToArray(); var edges = JointGraph.Edges(joint.Definition);

        Assert.True(joint.TryRewrite(r.Fan(rise), JointRecipes.Carry(JointGraph.Nodes(joint.Definition)), JointRecipes.Carry(edges), out _));

        Assert.Equal(points, joint.Mechanical.Points);
        Assert.Equal(ports, joint.Mechanical.Ports);
        Assert.Equal(edges, JointGraph.Edges(joint.Definition));
        Point(joint.Mechanical.Point(0), 20, 100);
        joint.Mechanical.AddResponse(joint.Mechanical.Port(0, 1), rise, min: 0, max: 100, responseD: 4);
        joint.Mechanical.AddResponse(joint.Mechanical.Port(1, 1), rise, min: 0, max: 100, responseD: 4);
        joint.Step(world);

        Assert.Equal(20UL, rise.Value);
        Assert.Equal(0L, rise.Tension);
        Point(joint.Mechanical.Point(0), 20, 120);
        Point(joint.Mechanical.Point(1), 180, 120);
        Assert.Equal(40, Gap(world));
    }

    [Fact]
    public void AnExplicitSplitBreaksConnectivityBeforeTheCoincidentSitesMoveApart()
    {
        var r = new JointRecipes(); var query = new Relation(r.External[2], r.External[3]);
        var joint = new JointAssembly(r.Tee(), G(query)); var spread = Program.Parameter(0);
        var target = new NegotiatingMachine(JointGraph.Machine(r.Corners(Program.Parameter(40)))); var world = World(joint, target);
        var bottomPorts = new[] { joint.Mechanical.Port(2, 0), joint.Mechanical.Port(3, 0) };
        Assert.IsType<Relation>(F(JointGraph.ReadPath(joint.Definition, query))[2]);

        Assert.True(joint.TryRewrite(r.Corners(spread), r.TeeToCornersNodes(), r.TeeToCornersEdges(), out var rewrite));

        Assert.IsType<Placeholder>(F(JointGraph.ReadPath(joint.Definition, query))[2]);
        Point(joint.Mechanical.Point(2), 100, 20);
        Point(joint.Mechanical.Point(3), 100, 20);
        Assert.Same(bottomPorts[0], joint.Mechanical.Port(2, 0));
        Assert.Same(bottomPorts[1], joint.Mechanical.Port(3, 0));
        Assert.Equal(6, JointGraph.Nodes(joint.Definition).Length);
        Assert.Equal(4, JointGraph.Edges(joint.Definition).Length);
        Assert.NotEmpty(F(F(rewrite)[3])); // The requested inter-foot path remains unresolved.
        AddSpread(joint, spread);
        joint.Step(world);
        Point(joint.Mechanical.Point(2), 80, 20);
        Point(joint.Mechanical.Point(3), 120, 20);
        Assert.Equal(20UL, spread.Value);
    }

    [Fact]
    public void FoldingCoincidentSitesCreatesAnActualSharedFootWithoutGeometricMovement()
    {
        var r = new JointRecipes(); var query = new Relation(r.External[2], r.External[3]);
        var joint = new JointAssembly(r.Corners(Program.Parameter(0)), G(query));
        var points = joint.Mechanical.Points.ToArray(); var coordinates = points.SelectMany(Program.XY).ToArray();
        Assert.IsType<Placeholder>(F(JointGraph.ReadPath(joint.Definition, query))[2]);

        Assert.True(joint.TryRewrite(r.Tee(), r.CornersToTeeNodes(), r.CornersToTeeEdges(), out _));

        Assert.Equal(points, joint.Mechanical.Points);
        Assert.Equal(coordinates, joint.Mechanical.Points.SelectMany(Program.XY));
        var exposures = JointGraph.Exposures(joint.Definition);
        Assert.Same(exposures[2].Right, exposures[3].Right);
        var route = Assert.IsType<Relation>(F(JointGraph.ReadPath(joint.Definition, query))[2]);
        Assert.Empty(F(route)); // Same actual site, hence a valid zero-edge path.
        Assert.Empty(F(F(joint.CurrentRead)[3]));
    }

    [Fact]
    public void RemovingAndRestoringAnEdgeKeepsTheRememberedSiteAndExternalPathObligation()
    {
        var r = new JointRecipes(); var query = new Relation(r.External[3], r.External[0]);
        var joint = new JointAssembly(r.Cross(), G(query));
        var oldUpper = joint.Mechanical.Point(3); var oldPort = joint.Mechanical.Port(3, 1); var upperEdge = JointGraph.Edges(joint.Definition)[3];
        Assert.IsType<Relation>(F(JointGraph.ReadPath(joint.Definition, query))[2]);

        Assert.True(joint.TryRewrite(r.Cross(false), JointRecipes.Carry(JointGraph.Nodes(joint.Definition)), r.CrossEdgeChange(false), out _));

        Assert.Contains(r.Upper, JointGraph.Nodes(joint.Definition));
        Assert.DoesNotContain(upperEdge, JointGraph.Edges(joint.Definition));
        Assert.Same(oldUpper, joint.Mechanical.Point(3)); Assert.Same(oldPort, joint.Mechanical.Port(3, 1));
        Assert.IsType<Placeholder>(F(JointGraph.ReadPath(joint.Definition, query))[2]);
        Assert.Single(F(F(joint.CurrentRead)[3]));

        Assert.True(joint.TryRewrite(r.Cross(), JointRecipes.Carry(JointGraph.Nodes(joint.Definition)), r.CrossEdgeChange(true), out _));

        Assert.Contains(upperEdge, JointGraph.Edges(joint.Definition));
        Assert.IsType<Relation>(F(JointGraph.ReadPath(joint.Definition, query))[2]);
        Assert.Empty(F(F(joint.CurrentRead)[3]));
    }

    [Fact]
    public void APathRetainsActualEdgeOrderAndItsSelectedReverseTraversal()
    {
        var r = new JointRecipes(); var definition = r.Tee(); var leftArm = JointGraph.Edges(definition)[0];
        var path = F(F(JointGraph.ReadPath(definition, new Relation(r.External[0], r.External[1])))[2]);
        var first = Assert.IsType<Relation>(path[0]); var traversal = Assert.IsType<Relation>(first.Right);
        Assert.Same(leftArm, first.Left);
        Assert.Same(r.Hub, leftArm.Left);
        Assert.Same(r.Left, traversal.Left);
        Assert.Same(r.Hub, traversal.Right);
    }

    [Fact]
    public void AnIncompleteCorrespondenceIsRejectedWithoutChangingTheLiveConstruction()
    {
        var r = new JointRecipes(); var joint = new JointAssembly(r.Tee(), G());
        var original = joint.Definition; var publication = joint.Mechanical.CurrentPublication;

        Assert.False(joint.TryRewrite(r.Corners(Program.Parameter(0)), G(), r.TeeToCornersEdges(), out var failed));

        Assert.Same(original, joint.Definition);
        Assert.Same(publication, joint.Mechanical.CurrentPublication);
        Assert.IsType<Placeholder>(F(failed)[2]);
        Assert.NotEmpty(F(F(failed)[3]));
    }

    [Fact]
    public void ADisplacedRewriteCannotBypassTheStablePublicInterface()
    {
        var r = new JointRecipes(); var joint = new JointAssembly(r.Tee(), G());
        var original = joint.Definition; var publication = joint.Mechanical.CurrentPublication;

        Assert.False(joint.TryRewrite(r.Fan(Program.Parameter(5)), JointRecipes.Carry(JointGraph.Nodes(original)),
            JointRecipes.Carry(JointGraph.Edges(original)), out _));

        Assert.Same(original, joint.Definition);
        Assert.Same(publication, joint.Mechanical.CurrentPublication);
        Point(joint.Mechanical.Point(0), 20, 100);
    }

    [Fact]
    public void EqualCoordinateExpressionsCannotReplaceTheActualExposedNodeCorrespondence()
    {
        var r = new JointRecipes(); var candidate = r.Corners(Program.Parameter(0)); var f = F(candidate);
        var exposures = JointGraph.Exposures(candidate);
        exposures[3] = new Relation(exposures[3].Left, exposures[2].Right);
        f[3] = G(exposures);

        Assert.False(JointGraph.Valid(G(f)));
        // The two feet coincide, but their producing expressions are distinct.
        Assert.True(JointGraph.Valid(candidate));
    }

    [Fact]
    public void ATopologyChangeInvalidatesRequestsFromThePreviousInterpretation()
    {
        var r = new JointRecipes(); var joint = new JointAssembly(r.Tee(), G()); var rise = Program.Parameter(0);
        var target = new NegotiatingMachine(JointGraph.Machine(r.Fan(Program.Parameter(40))));
        var port = joint.Mechanical.Port(0, 1); var link = new NegotiationLink(port, target.Port(0, 1));
        var oldRequest = NegotiationLink.Requests(link.Plan(joint.Mechanical.Publish(port), target.Publish(target.Port(0, 1))))[0];
        Assert.True(joint.TryRewrite(r.Fan(rise), JointRecipes.Carry(JointGraph.Nodes(joint.Definition)), JointRecipes.Carry(JointGraph.Edges(joint.Definition)), out _));
        joint.Mechanical.AddResponse(port, rise, min: 0, max: 100);

        Assert.False(joint.Mechanical.Apply([oldRequest], out _));
        Assert.Equal(0UL, rise.Value);
    }

    [Fact]
    public void EarlierTopologyReadsKeepTheirNumericCoordinatesAfterLaterSettlement()
    {
        var r = new JointRecipes(); var rise = Program.Parameter(0); var joint = new JointAssembly(r.Fan(rise), G());
        joint.Mechanical.AddResponse(joint.Mechanical.Port(0, 1), rise, min: 0, max: 100, responseD: 4);
        joint.Mechanical.AddResponse(joint.Mechanical.Port(1, 1), rise, min: 0, max: 100, responseD: 4);
        var oldRead = joint.CurrentRead;
        joint.Step(World(joint, new NegotiatingMachine(JointGraph.Machine(r.Fan(Program.Parameter(40))))));

        var oldLeft = F(F(F(oldRead)[2])[0]).Cast<Relation>().Single(b => ReferenceEquals(b.Left, r.Left)).Right;
        Point(oldLeft, 20, 100);
        Point(joint.Mechanical.Point(0), 20, 120);
    }

    [Fact]
    public void NativeRestorationPreservesTheSplitAndContinuesThroughTheSameNeighbourPorts()
    {
        var r = new JointRecipes(); var joint = new JointAssembly(r.Tee(), G()); var spread = Program.Parameter(0);
        var target = new NegotiatingMachine(JointGraph.Machine(r.Corners(Program.Parameter(40)))); var world = World(joint, target);
        Assert.True(joint.TryRewrite(r.Corners(spread), r.TeeToCornersNodes(), r.TeeToCornersEdges(), out _)); AddSpread(joint, spread);
        joint.Step(world);
        var archive = GojoNumericFormat.Encode(G(joint.Surface, world.Surface));
        var restored = F(Assert.Single(GojoNumericFormat.Decode(archive)));
        var loadedWorld = NegotiationWorld.Restore((Relation)restored[1]);
        var loaded = JointAssembly.Restore((Relation)restored[0], loadedWorld.Machines[0]);

        loaded.Step(loadedWorld);

        Assert.Same(loadedWorld.Machines[0], loaded.Mechanical);
        Assert.Same(loaded.Mechanical.Port(2, 0), loadedWorld.Links[4].Left);
        Point(loaded.Mechanical.Point(2), 70, 20);
        Point(loaded.Mechanical.Point(3), 130, 20);
        Point(joint.Mechanical.Point(2), 80, 20);
        Assert.Equal(6, JointGraph.Nodes(loaded.Definition).Length);
        Assert.True(TimeStepSequence.TryRead(loaded.Timeline, out _, out _, out var retained));
        Assert.Equal(3, retained.Length); // Split, first response, restored response.
    }

    private static NegotiationWorld World(JointAssembly joint, NegotiatingMachine target) => new([joint.Mechanical, target],
        Enumerable.Range(0, 4).SelectMany(i => Enumerable.Range(0, 2).Select(axis => new NegotiationLink(joint.Mechanical.Port(i, axis), target.Port(i, axis)))).ToArray());
    private static double Gap(NegotiationWorld world) => world.Compare().Sum(p => { ProjectionResidue.TryRead(NegotiationLink.Residual(p), out _, out var n, out var d); return Math.Abs((double)n / (double)d); });
    private static void AddSpread(JointAssembly joint, Carrier spread)
    {
        joint.Mechanical.AddResponse(joint.Mechanical.Port(2, 0), spread, gainN: -1, min: 0, max: 60, responseD: 4);
        joint.Mechanical.AddResponse(joint.Mechanical.Port(3, 0), spread, min: 0, max: 60, responseD: 4);
    }
    private static void Point(IElement value, double x, double y) => Assert.Equal(new[] { x, y }, Program.XY(value));

    [Fact]
    public void RestoringAnInstanceRequiresItsCompleteActualBoundaryPortCorrespondence()
    {
        var r = new JointRecipes(); var joint = new JointAssembly(r.Tee(), G());
        var fields = F(joint.Surface); fields[1] = G();
        Assert.Throws<ArgumentException>(() => JointAssembly.Restore(G(fields)));
        fields = F(joint.Surface);
        var rows = F(fields[1]); rows[0] = new Relation(((Relation)rows[0]).Left, G()); fields[1] = G(rows);
        Assert.Throws<ArgumentException>(() => JointAssembly.Restore(G(fields)));
    }
}
