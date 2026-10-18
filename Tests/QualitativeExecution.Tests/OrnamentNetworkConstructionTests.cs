using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;
using Gojo.Experiments.QualitativeFrieze;

namespace Gojo.Experiments.QualitativeExecution.Tests;

public sealed class OrnamentNetworkConstructionTests
{
    [Fact]
    public void IndependentPassagesRetainCommandsPublicationJoinsAndExactPacketAccounts()
    {
        var source = OrnamentSourcePatch.SquareFans(1, 1);
        var derived = new OrnamentDerivedScaffold(source.Scaffold, new Placeholder());
        var landmarks = new OrnamentWordLandmarks(source.Scaffold, source.Bindings);
        var bindings = S(E(derived.Representatives).Cast<Relation>().Select(row =>
        {
            Assert.True(landmarks.TryRead(row.Left, out var binding, out _));
            return (IElement)new Relation(row.Right, binding.Right);
        }).ToArray());
        var construction = new OrnamentNetworkConstruction(source.Frame, derived, bindings, landmarks.Surface);
        Assert.False(construction.IsComplete); Assert.Empty(construction.Units); construction.Run();
        Assert.True(construction.IsComplete); Assert.Equal(4, construction.Units.Length);
        Assert.Equal(4, E(construction.Commands).Length); Assert.Equal(4, E(construction.DifferenceReads).Length);
        var join = Assert.IsType<QualitativeGraphJoin>(construction.Join);
        Assert.Equal(4, E(join.Graph.Strokes).Length); Assert.Equal(4, E(join.Graph.Sites).Length);
        Assert.Equal(4, E(join.Accepted).Length); Assert.Equal(8, E(construction.RoleSites).Length);
        Assert.Empty(E(construction.Unresolved));
        var packets = 0;
        foreach (var unit in construction.Units)
        {
            Assert.True(unit.PlacementExecution.IsAtBoundary); Assert.True(unit.Current.Execution.IsAtBoundary);
            packets += Released(unit.PlacementExecution.Account()) + Released(unit.Current.Execution.Account());
            Assert.Equal(2, E(unit.Current.Ports).Length);
        }
        Assert.Equal(20, packets);
        var units = construction.Units; var graph = join.Graph; construction.Run();
        Assert.Same(units, construction.Units); Assert.Same(graph, construction.Join!.Graph);
        AssertNoNumbers(construction.Surface);
    }

    [Fact]
    public void CoincidentRolesStayDistinctAndAnIsolatedRoleGetsAPointWithoutInk()
    {
        var frame = new QualitativeMotionFrame(); var a = new Placeholder(); var b = new Placeholder(); var point = new Placeholder();
        var edge = new Placeholder(); var topology = new Relation(S(a, b, point), new Relation(S(new Relation(edge, new Relation(a, b))), S()));
        var bindings = S(new Relation(a, S()), new Relation(b, S()), new Relation(point, S()));
        var construction = new OrnamentNetworkConstruction(frame, topology, bindings, new Placeholder());
        construction.Run(); Assert.True(construction.IsComplete);
        Assert.Single(construction.Units); Assert.Single(construction.IsolatedUnits); Assert.Single(E(construction.IsolatedCommands));
        Assert.Same(edge, ((Relation)Assert.Single(E(construction.Commands))).Left);
        var join = Assert.IsType<QualitativeGraphJoin>(construction.Join);
        Assert.Equal(3, E(join.Graph.Sites).Length); Assert.Single(E(join.Graph.Strokes)); Assert.Empty(E(join.Accepted));
        var isolated = Assert.Single(construction.IsolatedUnits); Assert.Empty(E(isolated.Drawing.Strokes)); Assert.Equal(2, Released(isolated.Account()));
        AssertNoNumbers(construction.Surface);
    }

    [Fact]
    public void AForeignEndpointBlocksTheWholeConstructionBeforeAnyActuatorExists()
    {
        var frame = new QualitativeMotionFrame(); var a = new Placeholder(); var foreign = new Placeholder();
        var topology = new Relation(S(a), new Relation(S(new Relation(new Placeholder(), new Relation(a, foreign))), S()));
        var construction = new OrnamentNetworkConstruction(frame, topology, S(new Relation(a, S())), new Placeholder());
        construction.Run(); Assert.False(construction.IsComplete); Assert.Null(construction.Join);
        Assert.Empty(construction.Units); Assert.Empty(construction.IsolatedUnits); Assert.NotEmpty(E(construction.Unresolved));
    }

    private static int Released(Relation account)
    {
        var imports = E((Relation)((Relation)account.Left).Right); var active = (Relation)account.Right;
        var retained = (Relation)active.Right; var released = (Relation)retained.Right;
        Assert.Empty(E((Relation)active.Left)); Assert.Empty(E((Relation)retained.Left)); Assert.Empty(E((Relation)released.Right));
        var rows = E((Relation)released.Left).Cast<Relation>().ToArray(); Assert.Equal(imports.Length, rows.Length);
        foreach (var packet in imports) Assert.Single(rows, row => ReferenceEquals(row.Left, packet));
        return rows.Length;
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
