using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;
using Gojo.Experiments.QualitativeFrieze;

namespace Gojo.Experiments.QualitativeExecution.Tests;

public sealed class OrnamentViewCompositionTests
{
    [Fact]
    public void ClosedEmptyCompositionIsKnownEmptyAndKeepsTheCompleteSourceComplement()
    {
        var source = Source(); var lower = new OrnamentView(source,
            S(OrnamentView.Group(new Placeholder(), (Relation)E(source.Faces)[0])), new Placeholder());
        var operation = new OrnamentViewComposition(lower, S(), new Placeholder());
        Assert.True(operation.IsComplete); Assert.Empty(E(operation.View.Groups)); Assert.Empty(E(operation.Correspondence));
        Assert.Empty(E(operation.SharedMembership)); Assert.Equal(2, E(operation.View.Complement).Length);
        Assert.Empty(E(operation.Blocked)); Assert.Empty(E(operation.Unresolved));
    }

    [Fact]
    public void ParentUnionRetainsSharedSourceOccurrencesThroughDifferentChildren()
    {
        var source = Source(); var faces = E(source.Faces).Cast<Relation>().ToArray();
        var lower = new OrnamentView(source, S(OrnamentView.Group(new Placeholder(), faces[0]),
            OrnamentView.Group(new Placeholder(), faces[0], faces[1]), OrnamentView.Group(new Placeholder(), faces[1])), new Placeholder());
        var child = E(lower.Groups).Cast<Relation>().ToArray(); var parentIdentity = new Placeholder();
        var parent = new Relation(parentIdentity, S(child[0], child[1]));
        var composition = new OrnamentViewComposition(lower, S(parent), new Placeholder());
        Assert.True(composition.IsComplete); Assert.Single(E(composition.View.Groups)); Assert.Same(source, composition.View.Source);
        Assert.Equal(2, E(composition.View.SelectedFaces).Length);
        var row = Assert.IsType<Relation>(Assert.Single(E(composition.Correspondence)));
        var origin = Assert.IsType<Relation>(row.Left); Assert.Same(parent, origin.Left);
        var expanded = Assert.IsType<Relation>(origin.Right); Assert.Same(parentIdentity, expanded.Left); Assert.Equal(2, E((Relation)expanded.Right).Length);
        var maps = Assert.IsType<Relation>(row.Right); Assert.Equal(2, E((Relation)maps.Left).Length);
        var faceMaps = E((Relation)maps.Right).Cast<Relation>().ToArray(); Assert.Equal(2, faceMaps.Length);
        Assert.Equal(2, E((Relation)faceMaps[0].Right).Length); Assert.Single(E((Relation)faceMaps[1].Right));
        var shared = Assert.IsType<Relation>(Assert.Single(E(composition.SharedMembership))); Assert.Same(parent, shared.Left);
        var sharedFace = Assert.IsType<Relation>(Assert.Single(E((Relation)shared.Right))); Assert.Same(faces[0], sharedFace.Left);
        Assert.Equal(2, E((Relation)sharedFace.Right).Length);
        foreach (var occurrence in E((Relation)sharedFace.Right).Cast<Relation>())
        { Assert.Contains(occurrence.Left, child); Assert.Same(faces[0], occurrence.Right); }
        AssertNoNumbers(composition.Evaluate());
    }

    [Fact]
    public void SeveralParentsMayShareChildrenAndFurtherLevelsRetainBothPerspectives()
    {
        var source = Source(); var faces = E(source.Faces).Cast<Relation>().ToArray();
        var lower = new OrnamentView(source, S(OrnamentView.Group(new Placeholder(), faces[0]),
            OrnamentView.Group(new Placeholder(), faces[1])), new Placeholder());
        var children = E(lower.Groups); var firstIdentity = new Placeholder(); var secondIdentity = new Placeholder();
        var middle = new OrnamentViewComposition(lower, S(new Relation(firstIdentity, S(children)),
            new Relation(secondIdentity, S(children))), new Placeholder());
        Assert.True(middle.IsComplete); Assert.Equal(2, E(middle.View.Groups).Length); Assert.Single(E(middle.View.Overlaps));
        Assert.Equal(2, E(middle.Correspondence).Length); Assert.Empty(E(middle.SharedMembership));
        var top = new OrnamentViewComposition(middle.View, S(new Relation(new Placeholder(), S(E(middle.View.Groups)))), new Placeholder());
        Assert.True(top.IsComplete); Assert.Single(E(top.View.Groups)); Assert.Equal(2, E(top.View.SelectedFaces).Length);
        var topRow = Assert.IsType<Relation>(Assert.Single(E(top.Correspondence)));
        var childMaps = E((Relation)((Relation)topRow.Right).Left).Cast<Relation>().ToArray();
        Assert.Equal(2, childMaps.Length);
        foreach (var map in childMaps) Assert.Contains(map.Left, E(middle.View.Groups));
        var shared = Assert.IsType<Relation>(Assert.Single(E(top.SharedMembership)));
        Assert.Equal(2, E((Relation)shared.Right).Length);
        Assert.Same(lower.Source, top.View.Source); AssertNoNumbers(top.Evaluate());
    }

    [Fact]
    public void ForeignCopiedOrDefinitionRowsCannotStandInForActualChildReads()
    {
        var source = Source(); var face = (Relation)E(source.Faces)[0];
        var definition = OrnamentView.Group(new Placeholder(), face);
        var lower = new OrnamentView(source, S(definition), new Placeholder()); var actual = (Relation)E(lower.Groups)[0];
        var otherContext = new OrnamentView(source, S(definition), new Placeholder());
        foreach (var falseChild in new IElement[] { definition, new Relation(actual.Left, actual.Right), E(otherContext.Groups)[0] })
        {
            var badParent = new Relation(new Placeholder(), S(actual, falseChild));
            var goodParent = new Relation(new Placeholder(), S(actual));
            var composed = new OrnamentViewComposition(lower, S(badParent, goodParent), new Placeholder());
            Assert.False(composed.IsComplete); Assert.Single(E(composed.Blocked));
            Assert.Single(E(composed.View.Groups)); Assert.Single(E(composed.Correspondence));
            var row = (Relation)E(composed.Correspondence)[0]; Assert.Same(goodParent, ((Relation)row.Left).Left);
        }
    }

    [Fact]
    public void DuplicateChildReferencesCannotFabricateMultiplicity()
    {
        var source = Source(); var lower = new OrnamentView(source,
            S(OrnamentView.Group(new Placeholder(), (Relation)E(source.Faces)[0])), new Placeholder());
        var child = E(lower.Groups)[0]; var operation = new OrnamentViewComposition(lower,
            S(new Relation(new Placeholder(), S(child, child))), new Placeholder());
        Assert.False(operation.IsComplete); Assert.Single(E(operation.Blocked)); Assert.Empty(E(operation.View.Groups));
        Assert.Empty(E(operation.SharedMembership));
    }

    [Fact]
    public void OpenChildrenAndOpenParentScopeKeepExactCompositionUnresolved()
    {
        var source = Source(); var lower = new OrnamentView(source,
            S(OrnamentView.Group(new Placeholder(), (Relation)E(source.Faces)[0])), new Placeholder()); var child = E(lower.Groups)[0];
        var openChildren = new OrnamentViewComposition(lower, S(new Relation(new Placeholder(), StructuralConstraint.Open(child))), new Placeholder());
        var openScope = new OrnamentViewComposition(lower, StructuralConstraint.Open(new Relation(new Placeholder(), S(child))), new Placeholder());
        foreach (var operation in new[] { openChildren, openScope })
        { Assert.False(operation.IsComplete); Assert.NotEmpty(E(operation.Unresolved)); Assert.Empty(E(operation.View.Groups)); }
    }

    [Fact]
    public void DuplicateParentIdentitiesCannotSelectOneMeaningAndSourceIsUnchanged()
    {
        var source = Source(); var sourceRead = source.Evaluate(); var faces = E(source.Faces).Cast<Relation>().ToArray();
        var lower = new OrnamentView(source, S(OrnamentView.Group(new Placeholder(), faces[0]), OrnamentView.Group(new Placeholder(), faces[1])), new Placeholder());
        var children = E(lower.Groups); var identity = new Placeholder();
        var operation = new OrnamentViewComposition(lower, S(new Relation(identity, S(children[0])), new Relation(identity, S(children[1]))), new Placeholder());
        Assert.False(operation.IsComplete); Assert.NotEmpty(E(operation.Blocked)); Assert.Empty(E(operation.View.Groups));
        Assert.Same(sourceRead, source.Evaluate()); Assert.Equal(5, E(source.Graph.Strokes).Length); Assert.Equal(2, E(source.Faces).Length);
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
        var source = new ScaffoldQuestionScaffold(graph, S(first, second), S(outer), new Placeholder());
        Assert.True(source.IsComplete); return source;
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
