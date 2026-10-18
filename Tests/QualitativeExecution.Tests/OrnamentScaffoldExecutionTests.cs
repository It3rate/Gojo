using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeFrieze;

namespace Gojo.Experiments.QualitativeExecution.Tests;

public sealed class OrnamentScaffoldExecutionTests
{
    [Fact]
    public void AReadBecomesAnAcknowledgedConstructionWithExactRoleAndMaterialCorrespondence()
    {
        var source = OrnamentSourcePatch.SquareFans(1, 1);
        var derived = new OrnamentDerivedScaffold(source.Scaffold, new Placeholder());
        var words = new OrnamentWordLandmarks(source.Scaffold, source.Bindings);
        var bindings = S(E(derived.Representatives).Cast<Relation>().Select(row =>
        {
            Assert.True(words.TryRead(row.Left, out var binding, out _));
            return (IElement)new Relation(row.Right, binding.Right);
        }).ToArray());
        var execution = new OrnamentScaffoldExecution(source.Frame, derived, bindings, words.Surface);
        Assert.Null(execution.Join); Assert.False(execution.IsComplete);
        execution.Run(2); Assert.Null(execution.Join); Assert.False(execution.IsComplete);
        var root = Assert.IsType<PrimitiveFlowGroup>(execution.Root); Assert.Empty(E(root.CompletionEvents));
        execution.Run();
        Assert.True(execution.IsComplete); Assert.Single(E(root.CompletionEvents)); Assert.True(root.IsAtBoundary);
        var join = Assert.IsType<QualitativeGraphJoin>(execution.Join);
        Assert.Equal(4, E(join.Graph.Strokes).Length); Assert.Equal(4, E(join.Graph.Sites).Length);
        Assert.Equal(8, E(execution.RoleSites).Length); Assert.Equal(4, E(execution.Commands).Length);
        Assert.Equal(4, E(execution.DifferenceReads).Length); Assert.Equal(4, E(join.Accepted).Length);
        Assert.Empty(E(execution.Unresolved)); Assert.Empty(E(root.Unresolved));
        var packetCount = 0;
        foreach (var account in E(root.Account()).Cast<Relation>())
        {
            var incoming = E((Relation)((Relation)account.Left).Right);
            var active = (Relation)account.Right; var retained = (Relation)active.Right; var released = (Relation)retained.Right;
            Assert.Empty(E((Relation)active.Left)); Assert.Empty(E((Relation)retained.Left)); Assert.Empty(E((Relation)released.Right));
            var exits = E((Relation)released.Left).Cast<Relation>().ToArray(); Assert.Equal(incoming.Length, exits.Length);
            foreach (var packet in incoming) Assert.Single(exits, row => ReferenceEquals(row.Left, packet));
            packetCount += incoming.Length;
        }
        Assert.Equal(12, packetCount);
        var state = execution.Drawing.State; execution.Run(); Assert.Same(state, execution.Drawing.State);
        AssertNoNumbers(execution.Surface);
    }

    [Fact]
    public void EqualPositionRolesRemainDifferentSitesUnlessTheDerivedTopologyIdentifiesThem()
    {
        var source = OrnamentSourcePatch.SquareFans(1, 1);
        var derived = new OrnamentDerivedScaffold(source.Scaffold, new Placeholder());
        // This coarse interpretation deliberately collapses every projection,
        // while retaining the four independent topological representative roles.
        var bindings = S(E(derived.Sites).Select(role => (IElement)new Relation(role, S())).ToArray());
        var execution = new OrnamentScaffoldExecution(source.Frame, derived, bindings, new Placeholder());
        execution.Run(); Assert.True(execution.IsComplete);
        var graph = Assert.IsType<QualitativeGraphJoin>(execution.Join).Graph;
        Assert.Equal(4, E(graph.Sites).Length); Assert.Equal(4, E(graph.Strokes).Length);
        Assert.All(E(execution.OutputBindings).Cast<Relation>(), binding => Assert.Empty(E((Relation)binding.Right)));
        AssertNoNumbers(execution.Surface);
    }

    [Fact]
    public void MissingRepresentativeBindingPreservesTheUnresolvedConstructionWithoutEmittingPartialMaterial()
    {
        var source = OrnamentSourcePatch.SquareFans(1, 1);
        var derived = new OrnamentDerivedScaffold(source.Scaffold, new Placeholder());
        var bindings = S(E(derived.Sites).Skip(1).Select(role => (IElement)new Relation(role, S())).ToArray());
        var execution = new OrnamentScaffoldExecution(source.Frame, derived, bindings, new Placeholder());
        var before = execution.Drawing.State;
        execution.Run(); Assert.False(execution.IsComplete); Assert.Null(execution.Join);
        Assert.Same(before, execution.Drawing.State); Assert.Empty(E(execution.Drawing.Strokes));
        Assert.NotEmpty(E(execution.Unresolved)); AssertNoNumbers(execution.Surface);
    }

    [Fact]
    public void APublishedContactTopologyConstructsItsEdgesAndKeepsAnIsolatedRepresentative()
    {
        var frame = new Gojo.Experiments.QualitativeExecution.QualitativeMotionFrame(); var reach = new Placeholder();
        var sites = Enumerable.Range(0, 7).Select(_ => (IElement)new Placeholder()).ToArray();
        var edges = Enumerable.Range(0, 5).Select(i => (IElement)new Relation(new Placeholder(), new Relation(sites[i], sites[i + 1]))).ToArray();
        var topology = new Relation(S(sites), new Relation(S(edges), S()));
        var bindings = S(sites.Select((site, i) => (IElement)new Relation(site,
            S(Enumerable.Repeat((IElement)Gojo.Experiments.QualitativeExecution.QualitativeMotionFrame.Move(frame.Right, reach), i).ToArray()))).ToArray());
        var execution = new OrnamentScaffoldExecution(frame, topology, bindings, new Placeholder());
        var root = Assert.IsType<PrimitiveFlowGroup>(execution.Root);
        Assert.All(root.Children, child => Assert.IsType<PrimitiveFlowGroup>(child));
        Assert.Same(topology, execution.SourceTopology); Assert.Same(topology, execution.Source);
        execution.Run(); Assert.True(execution.IsComplete);
        var join = Assert.IsType<QualitativeGraphJoin>(execution.Join);
        Assert.Equal(5, E(join.Graph.Strokes).Length); Assert.Equal(7, E(join.Graph.Sites).Length);
        Assert.Equal(5, E(execution.Commands).Length); Assert.Single(E(execution.IsolatedCommands));
        var pointCommand = Assert.IsType<Relation>(Assert.Single(E(execution.IsolatedCommands)));
        Assert.Same(sites[^1], pointCommand.Left);
        Assert.Single(E(execution.RoleSites).Cast<Relation>(), row => ReferenceEquals(((Relation)row.Left).Left, sites[^1]));
        Assert.Equal(6, E(root.Account()).Length); AssertNoNumbers(execution.Surface);
    }

    [Fact]
    public void GenericTopologyRejectsForeignEndpointsBeforeAnyPacketIsImported()
    {
        var frame = new Gojo.Experiments.QualitativeExecution.QualitativeMotionFrame();
        var a = new Placeholder(); var b = new Placeholder(); var foreign = new Placeholder();
        var topology = new Relation(S(a, b), new Relation(S(new Relation(new Placeholder(), new Relation(a, foreign))), S()));
        var execution = new OrnamentScaffoldExecution(frame, topology, S(new Relation(a, S()), new Relation(b, S())), new Placeholder());
        var before = execution.Drawing.State; execution.Run();
        Assert.False(execution.IsComplete); Assert.Null(execution.Join); Assert.NotEmpty(E(execution.Unresolved));
        Assert.Same(before, execution.Drawing.State); Assert.Empty(E(execution.Drawing.Strokes));
    }

    private static void AssertNoNumbers(IElement root)
    {
        var seen = new HashSet<IElement>(ReferenceEqualityComparer.Instance); var todo = new Stack<IElement>(); todo.Push(root);
        while (todo.TryPop(out var item))
        {
            if (!seen.Add(item)) continue; Assert.IsNotType<Carrier>(item); Assert.IsNotType<CarrierLocation>(item);
            if (item is Relation relation) { todo.Push(relation.Left); todo.Push(relation.Right); }
        }
    }
    private static Relation S(params IElement[] values) => StructuralConstraint.Closed(values);
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
}
