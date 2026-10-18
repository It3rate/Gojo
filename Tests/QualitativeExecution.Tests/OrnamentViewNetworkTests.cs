using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;
using Gojo.Experiments.QualitativeFrieze;

namespace Gojo.Experiments.QualitativeExecution.Tests;

public sealed class OrnamentViewNetworkTests
{
    [Fact]
    public void ClosedEmptyViewProducesKnownEmptyTopologyRatherThanUnknownStructure()
    {
        var source = Source(); var view = new OrnamentView(source, S(), new Placeholder());
        var network = new OrnamentViewNetwork(view, new Placeholder());
        Assert.True(network.IsComplete); Assert.Empty(E(network.Sites)); Assert.Empty(E(network.Contacts)); Assert.Empty(E(network.Overlaps));
        Assert.Empty(E(network.Blocked)); Assert.Empty(E(network.Unresolved)); Assert.Equal(2, E(view.Complement).Length);
        var body = (Relation)network.Topology.Right;
        Assert.Empty(E((Relation)body.Left)); Assert.Empty(E((Relation)body.Right));
    }

    [Fact]
    public void ContactAndOverlapCreateDistinctWitnessedEdgesWithoutMergingRepresentatives()
    {
        var source = Source(); var faces = E(source.Faces).Cast<Relation>().ToArray();
        var view = new OrnamentView(source, S(OrnamentView.Group(new Placeholder(), faces[0]),
            OrnamentView.Group(new Placeholder(), faces[0], faces[1]), OrnamentView.Group(new Placeholder(), faces[1])), new Placeholder());
        var network = new OrnamentViewNetwork(view, new Placeholder());
        Assert.True(network.IsComplete); Assert.Equal(3, E(network.Sites).Length);
        Assert.Equal(3, E(network.Sites).Distinct(ReferenceEqualityComparer.Instance).Count());
        Assert.Single(E(network.Contacts)); Assert.Equal(2, E(network.Overlaps).Length);
        foreach (var row in E(network.Representatives).Cast<Relation>())
        {
            Assert.True(network.TryReadRepresentative(row, out var group, out var role));
            Assert.Contains(group, E(view.Groups)); Assert.Same(row.Right, role); Assert.Contains(role, E(network.Sites));
            Assert.DoesNotContain(role, E(source.Graph.Sites));
        }
        var contact = (Relation)E(network.Contacts)[0];
        Assert.True(network.TryReadContact(contact, out var contactWitness, out var a, out var b));
        Assert.Contains(contactWitness, E(view.Adjacency)); Assert.NotSame(a, b);
        Assert.False(network.TryReadOverlap(contact, out _, out _, out _));
        foreach (var overlap in E(network.Overlaps).Cast<Relation>())
        { Assert.True(network.TryReadOverlap(overlap, out var witness, out _, out _)); Assert.Contains(witness, E(view.Overlaps)); }
        var topologyBody = Assert.IsType<Relation>(network.Topology.Right);
        var normalized = Assert.IsType<Relation>(Assert.Single(E((Relation)topologyBody.Left)));
        Assert.Same(contact, normalized.Left); Assert.Same(contact.Right, normalized.Right);
        Assert.Empty(E((Relation)topologyBody.Right));
        AssertNoNumbers(network.Surface);
    }

    [Fact]
    public void QuestionContextsYieldDifferentConstructionRolesWhileRetainingOneUnchangedSource()
    {
        var source = Source(); var before = source.Evaluate(); var face = (Relation)E(source.Faces)[0];
        var definition = OrnamentView.Group(new Placeholder(), face);
        var leftView = new OrnamentView(source, S(definition), new Placeholder());
        var rightView = new OrnamentView(source, S(definition), new Placeholder());
        var a = new OrnamentViewNetwork(leftView, new Placeholder()); var b = new OrnamentViewNetwork(rightView, new Placeholder());
        Assert.True(a.IsComplete); Assert.True(b.IsComplete);
        Assert.NotSame(E(a.Sites)[0], E(b.Sites)[0]); Assert.NotSame(a.Surface, b.Surface);
        Assert.Same(leftView, a.Source); Assert.Same(rightView, b.Source); Assert.Same(before, source.Evaluate());
        Assert.Single(E(leftView.Complement)); Assert.Single(E(rightView.Complement));
        Assert.Equal(5, E(source.Graph.Strokes).Length); Assert.Equal(4, E(source.Graph.Sites).Length);
        var actual = (Relation)E(a.Representatives)[0];
        Assert.False(a.TryReadRepresentative(new Relation(actual.Left, actual.Right), out _, out _));
        Assert.False(a.TryReadRepresentative((Relation)E(b.Representatives)[0], out _, out _));
    }

    [Fact]
    public void IncompleteViewCannotPublishACompleteConstructionNetwork()
    {
        var source = Source(); var actual = OrnamentView.Group(new Placeholder(), (Relation)E(source.Faces)[0]);
        var view = new OrnamentView(source, StructuralConstraint.Open(actual), new Placeholder());
        var network = new OrnamentViewNetwork(view, new Placeholder());
        Assert.False(network.IsComplete); Assert.Single(E(network.Unresolved));
        Assert.Empty(E(network.Sites)); Assert.Empty(E(network.Contacts)); Assert.Empty(E(network.Overlaps));
    }

    private static ScaffoldQuestionScaffold Source()
    {
        var frame = new QualitativeMotionFrame(); var reach = new Placeholder();
        var a = new Placeholder(); var b = new Placeholder(); var c = new Placeholder(); var d = new Placeholder();
        Relation Word(params IElement[] directions) => S(directions.Select(direction => (IElement)QualitativeMotionFrame.Move(direction, reach)).ToArray());
        var ab = QualitativeStrokeGraph.Stroke(new Placeholder(), a, b, Word(frame.Right));
        var bc = QualitativeStrokeGraph.Stroke(new Placeholder(), b, c, Word(frame.Up));
        var cd = QualitativeStrokeGraph.Stroke(new Placeholder(), c, d, Word(frame.Left));
        var da = QualitativeStrokeGraph.Stroke(new Placeholder(), d, a, Word(frame.Down));
        var ac = QualitativeStrokeGraph.Stroke(new Placeholder(), a, c, Word(frame.Right, frame.Up));
        Relation O(Relation edge, IElement from, IElement to) => ScaffoldQuestionScaffold.Oriented(edge, from, to);
        var first = ScaffoldQuestionScaffold.Boundary(O(ab, a, b), O(bc, b, c), O(ac, c, a));
        var second = ScaffoldQuestionScaffold.Boundary(O(ac, a, c), O(cd, c, d), O(da, d, a));
        var outer = ScaffoldQuestionScaffold.Boundary(O(ab, b, a), O(da, a, d), O(cd, d, c), O(bc, c, b));
        var graph = new QualitativeStrokeGraph(frame, S(a, b, c, d), S(ab, bc, cd, da, ac));
        var source = new ScaffoldQuestionScaffold(graph, S(first, second), S(outer), new Placeholder()); Assert.True(source.IsComplete); return source;
    }
    private static Relation S(params IElement[] entries) => StructuralConstraint.Closed(entries);
    private static IElement[] E(Relation scope) => StructuralConstraint.TryReadScope(scope, out var entries, out _, out _) ? entries : [];
    private static void AssertNoNumbers(IElement root)
    {
        var seen = new HashSet<IElement>(ReferenceEqualityComparer.Instance); var pending = new Stack<IElement>(); pending.Push(root);
        while (pending.TryPop(out var element))
        {
            if (!seen.Add(element)) continue; Assert.False(element is Carrier or CarrierLocation);
            if (element is Relation row) { pending.Push(row.Left); pending.Push(row.Right); }
        }
    }
}
