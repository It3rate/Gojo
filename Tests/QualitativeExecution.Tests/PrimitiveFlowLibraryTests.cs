using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeFrieze;

namespace Gojo.Experiments.QualitativeExecution.Tests;

public sealed class PrimitiveFlowLibraryTests
{
    [Theory]
    [InlineData("P01")]
    [InlineData("P02")]
    [InlineData("P03")]
    [InlineData("P04")]
    [InlineData("P05")]
    [InlineData("P06")]
    [InlineData("P07")]
    [InlineData("P08")]
    [InlineData("P09")]
    [InlineData("P10")]
    [InlineData("P11")]
    [InlineData("P12")]
    [InlineData("P13")]
    [InlineData("P14")]
    [InlineData("P15")]
    [InlineData("P16")]
    public void NativeDefinitionsProduceTheirRequestedWholeTraversalsAndAccountForEveryPacket(string id)
    {
        var recipe = PrimitiveFlowLibrary.Create().Single(item => item.Id == id);
        Assert.Empty(E(recipe.Drawing.Strokes)); Assert.Empty(E(recipe.Root.CompletionEvents));
        Assert.True(recipe.Root.IsAtBoundary); Assert.False(recipe.GoalMet); AssertNoNumbers(recipe.Definition);
        var result = recipe.Run(256);
        Assert.True(recipe.GoalMet, id); Assert.True(recipe.Root.IsAtBoundary, id);
        Assert.NotEmpty(E(recipe.Drawing.Strokes)); AssertNoNumbers(result);
        foreach (var node in recipe.Nodes())
        { Assert.Empty(E(node.Pending)); Assert.Empty(E(node.Unresolved)); Assert.True(node.IsAtBoundary); }
        var requested = E(Assert.IsType<Relation>(recipe.Goal.Right)); var completed = E(recipe.Root.CompletionEvents);
        Assert.Equal(requested.Length, completed.Length);
        var matches = E(Assert.IsType<Relation>(Assert.IsType<Relation>(result.Right).Right));
        Assert.Equal(requested.Length, matches.Length);
        foreach (var ((target, actual), match) in requested.Zip(completed).Zip(matches.Cast<Relation>()))
        { Assert.Same(target, match.Left); Assert.Same(actual, match.Right); }
        AssertAccounted(recipe);
        var drawing = recipe.Drawing.State; var completions = recipe.Root.CompletionEvents;
        recipe.Run(1); Assert.Same(drawing, recipe.Drawing.State); Assert.Same(completions, recipe.Root.CompletionEvents);
    }

    [Theory]
    [InlineData("P07", 1)]
    [InlineData("P08", 1)]
    [InlineData("P09", 1)]
    [InlineData("P10", 1)]
    [InlineData("P11", 1)]
    [InlineData("P14", 12)]
    [InlineData("P15", 1)]
    [InlineData("P16", 2)]
    public void ExplicitCompletedLoopJoinsRetainDistinctRawEndpointsAndAdmitASeparateClosedGraph(string id, int loops)
    {
        var recipe = PrimitiveFlowLibrary.Create().Single(item => item.Id == id); recipe.Run(256);
        Assert.True(recipe.GoalMet); var before = recipe.Drawing.State; var rawStrokes = recipe.Drawing.Strokes;
        var closure = recipe.ClosureView();
        Assert.True(closure.TryReadPassed(closure.Evaluate()), id);
        Assert.Equal(loops, E(closure.DeclaredJoins).Length); Assert.Equal(loops, E(closure.Accepted).Length);
        Assert.Empty(E(closure.Blocked)); Assert.Empty(E(closure.Unresolved));
        foreach (var declaration in E(closure.DeclaredJoins).Cast<Relation>())
        {
            var pair = Assert.IsType<Relation>(declaration.Left); Assert.NotSame(pair.Left, pair.Right);
            var a = E(recipe.Drawing.SiteBindings).Cast<Relation>().Single(row => ReferenceEquals(row.Left, pair.Left));
            var b = E(recipe.Drawing.SiteBindings).Cast<Relation>().Single(row => ReferenceEquals(row.Left, pair.Right));
            var equality = recipe.Drawing.Frame.Reduce(Assert.IsType<Relation>(a.Right),
                Inverse(recipe.Drawing.Frame, Assert.IsType<Relation>(b.Right)));
            Assert.True(recipe.Drawing.Frame.TryReadResult(equality, out var remaining, out var cancellations, out var pending));
            Assert.Empty(E(remaining)); Assert.Empty(E(pending));
            Assert.True(E(cancellations).Length > 0 || E(Assert.IsType<Relation>(a.Right)).Length == 0);
            var from = E(closure.SiteMap).Cast<Relation>().Single(row => ReferenceEquals(row.Left, pair.Left));
            var to = E(closure.SiteMap).Cast<Relation>().Single(row => ReferenceEquals(row.Left, pair.Right));
            Assert.Same(from.Right, to.Right);
        }
        Assert.Same(before, recipe.Drawing.State); Assert.Same(rawStrokes, recipe.Drawing.Strokes);
        Assert.Equal(0, closure.Source.Describe(closure.Source.Evaluate()).Contours);
        Assert.Equal(loops, closure.Graph.Describe(closure.Graph.Evaluate()).Contours);
        Assert.Equal(E(rawStrokes).Length, E(closure.Graph.Strokes).Length); AssertNoNumbers(closure.Surface);
    }

    [Fact]
    public void FlatAndHierarchicalMeanderHaveCorrespondingWordsButDifferentCallStructure()
    {
        var recipes = PrimitiveFlowLibrary.Create(); var flat = recipes.Single(item => item.Id == "P12");
        var nested = recipes.Single(item => item.Id == "P13");
        Assert.Same(flat.Drawing.Frame, nested.Drawing.Frame); Assert.IsType<PrimitiveFlowLeaf>(flat.Root);
        var group = Assert.IsType<PrimitiveFlowGroup>(nested.Root); Assert.Equal(3, group.Children.Count);
        flat.Run(128); nested.Run(128); Assert.True(flat.GoalMet); Assert.True(nested.GoalMet);
        SameWords(flat.Drawing.Strokes, nested.Drawing.Strokes);
        Assert.Equal(3, E(flat.Root.CompletionEvents).Length); Assert.Equal(3, E(nested.Root.CompletionEvents).Length);
        Assert.Equal(30, E(flat.Drawing.Strokes).Length); Assert.Equal(30, E(nested.Drawing.Strokes).Length);
        Assert.NotSame(flat.Definition, nested.Definition); AssertAccounted(flat); AssertAccounted(nested);
    }

    [Fact]
    public void DirectAndTransportedDiamondShareNativeOutputWordsAndRetainDifferentDerivations()
    {
        var recipes = PrimitiveFlowLibrary.Create(); var direct = recipes.Single(item => item.Id == "P10");
        var transformed = recipes.Single(item => item.Id == "P11");
        direct.Run(16); transformed.Run(16); Assert.True(direct.GoalMet); Assert.True(transformed.GoalMet);
        SameWords(direct.Drawing.Strokes, transformed.Drawing.Strokes);
        var directEvidence = Assert.IsType<Relation>(Assert.IsType<Relation>(direct.Definition.Right).Left);
        var transportedEvidence = Assert.IsType<Relation>(Assert.IsType<Relation>(transformed.Definition.Right).Left);
        Assert.Empty(E(directEvidence)); Assert.NotSame(directEvidence, transportedEvidence);
        Assert.True(transportedEvidence.Left is Relation); Assert.True(transportedEvidence.Right is Relation);
        var directClosure = direct.ClosureView(); var transportedClosure = transformed.ClosureView();
        Assert.True(directClosure.TryReadPassed(directClosure.Evaluate()));
        Assert.True(transportedClosure.TryReadPassed(transportedClosure.Evaluate()));
        AssertNoNumbers(transportedEvidence);
    }

    [Fact]
    public void StarOutlineAndTwoLoopConstructionRetainDifferentConnectivityAtTheirProjectedCrossings()
    {
        var recipes = PrimitiveFlowLibrary.Create(); var outline = recipes.Single(item => item.Id == "P15");
        var loops = recipes.Single(item => item.Id == "P16"); outline.Run(64); loops.Run(64);
        Assert.True(outline.GoalMet); Assert.True(loops.GoalMet);
        var a = outline.ClosureView(); var b = loops.ClosureView();
        Assert.True(a.TryReadPassed(a.Evaluate())); Assert.True(b.TryReadPassed(b.Evaluate()));
        var outlineRead = a.Graph.Describe(a.Graph.Evaluate()); var loopRead = b.Graph.Describe(b.Graph.Evaluate());
        Assert.Equal(1, outlineRead.Contours); Assert.Equal(2, loopRead.Contours);
        Assert.Equal(1, ActiveComponents(a.Graph)); Assert.Equal(2, ActiveComponents(b.Graph));
        Assert.Equal(0, loopRead.X); Assert.Equal(0, loopRead.Branches);
        Assert.Equal(12, E(a.Graph.Strokes).Length); Assert.Equal(6, E(b.Graph.Strokes).Length);
        AssertNoNumbers(a.Surface); AssertNoNumbers(b.Surface);
    }

    [Fact]
    public void ZeroBudgetAndPartialBudgetDoNotManufactureTheRequestedWholePattern()
    {
        var recipe = PrimitiveFlowLibrary.Create().Single(item => item.Id == "P02");
        recipe.Run(0); Assert.False(recipe.GoalMet); Assert.Empty(E(recipe.Drawing.Strokes));
        recipe.Run(1); Assert.False(recipe.GoalMet); Assert.Single(E(recipe.Drawing.Strokes));
        Assert.Empty(E(recipe.Root.CompletionEvents)); Assert.False(recipe.Root.IsAtBoundary);
        recipe.Run(16); Assert.True(recipe.GoalMet); Assert.Equal(6, E(recipe.Drawing.Strokes).Length); AssertAccounted(recipe);
    }

    [Fact]
    public void WholeProgramCompletionCannotCertifyARequestedClosureWhenItsEndpointsDiffer()
    {
        var drawing = new QualitativeDrawingReceiver(new()); var reach = new Placeholder();
        Relation Draw(IElement direction) => drawing.Draw(S(QualitativeMotionFrame.Move(direction, reach)));
        var leaf = new PrimitiveFlowLeaf("three-sided path", drawing,
            S(Draw(drawing.Frame.Right), Draw(drawing.Frame.Up), Draw(drawing.Frame.Left)));
        var recipe = new PrimitiveFlowRecipe("open", "Open goal", "Completion differs from closure", "",
            leaf, closureNodes: [leaf]);
        recipe.Run(8); Assert.True(recipe.GoalMet); Assert.Single(E(leaf.CompletionEvents));
        var before = drawing.State; var strokes = drawing.Strokes; var closure = recipe.ClosureView();
        Assert.Single(E(closure.DeclaredJoins)); Assert.Empty(E(closure.Accepted)); Assert.Single(E(closure.Blocked));
        Assert.Empty(E(closure.Unresolved)); Assert.False(closure.TryReadPassed(closure.Evaluate()));
        Assert.Equal(0, closure.Graph.Describe(closure.Graph.Evaluate()).Contours);
        Assert.Equal(E(closure.Source.Sites).Length, E(closure.Graph.Sites).Length);
        Assert.Same(before, drawing.State); Assert.Same(strokes, drawing.Strokes); AssertNoNumbers(closure.Surface);
    }

    private static void SameWords(Relation left, Relation right)
    {
        var a = E(left).Cast<Relation>().ToArray(); var b = E(right).Cast<Relation>().ToArray(); Assert.Equal(a.Length, b.Length);
        foreach (var (first, second) in a.Zip(b))
        {
            Assert.True(QualitativeStrokeGraph.TryReadStroke(first, out _, out _, out _, out var wordA));
            Assert.True(QualitativeStrokeGraph.TryReadStroke(second, out _, out _, out _, out var wordB));
            var x = E(wordA).Cast<Relation>().ToArray(); var y = E(wordB).Cast<Relation>().ToArray(); Assert.Equal(x.Length, y.Length);
            foreach (var (m, n) in x.Zip(y)) { Assert.Same(m.Left, n.Left); Assert.Same(m.Right, n.Right); }
        }
    }
    private static void AssertAccounted(PrimitiveFlowRecipe recipe)
    {
        var leaves = recipe.Nodes().OfType<PrimitiveFlowLeaf>().ToArray(); var imported = new List<IElement>(); var current = new List<IElement>();
        foreach (var leaf in leaves)
        {
            var account = leaf.Account(); var header = Assert.IsType<Relation>(account.Left);
            imported.AddRange(E(Assert.IsType<Relation>(header.Right)));
            var active = Assert.IsType<Relation>(account.Right); var retained = Assert.IsType<Relation>(active.Right);
            var released = Assert.IsType<Relation>(retained.Right); Assert.Empty(E(Assert.IsType<Relation>(released.Right)));
            foreach (var scope in new[] { active.Left, retained.Left, released.Left }.Cast<Relation>())
                current.AddRange(E(scope).Cast<Relation>().Select(row => row.Left));
        }
        Assert.NotEmpty(imported); Assert.Equal(imported.Count, imported.Distinct(ReferenceEqualityComparer.Instance).Count());
        Assert.Equal(imported.Count, current.Count); Assert.Equal(current.Count, current.Distinct(ReferenceEqualityComparer.Instance).Count());
        Assert.All(imported, packet => Assert.Contains(current, actual => ReferenceEquals(packet, actual)));
        if (recipe.Root is PrimitiveFlowGroup group) Assert.Equal(leaves.Length, E(group.Account()).Length);
    }
    private static int ActiveComponents(QualitativeStrokeGraph graph)
    {
        var neighbours = new Dictionary<IElement, List<IElement>>(ReferenceEqualityComparer.Instance);
        foreach (var stroke in E(graph.Strokes).Cast<Relation>())
        {
            Assert.True(QualitativeStrokeGraph.TryReadStroke(stroke, out _, out var a, out var b, out _));
            if (!neighbours.ContainsKey(a)) neighbours.Add(a, []); if (!neighbours.ContainsKey(b)) neighbours.Add(b, []);
            neighbours[a].Add(b); neighbours[b].Add(a);
        }
        var seen = new HashSet<IElement>(ReferenceEqualityComparer.Instance); var components = 0;
        foreach (var seed in neighbours.Keys)
        {
            if (!seen.Add(seed)) continue; components++; var pending = new Stack<IElement>(); pending.Push(seed);
            while (pending.TryPop(out var site)) foreach (var next in neighbours[site]) if (seen.Add(next)) pending.Push(next);
        }
        return components;
    }
    private static Relation Inverse(QualitativeMotionFrame frame, Relation word) => S(E(word).Cast<Relation>().Select(move =>
        (IElement)QualitativeMotionFrame.Move(E(frame.Opposites).Cast<Relation>().Single(pair => ReferenceEquals(pair.Left, move.Left)).Right, move.Right)).ToArray());
    private static void AssertNoNumbers(IElement root)
    {
        var seen = new HashSet<IElement>(ReferenceEqualityComparer.Instance); var pending = new Stack<IElement>(); pending.Push(root);
        while (pending.TryPop(out var item))
        {
            if (!seen.Add(item)) continue; Assert.False(item is Carrier or CarrierLocation);
            if (item is Relation relation) { pending.Push(relation.Left); pending.Push(relation.Right); }
        }
    }
    private static Relation S(params IElement[] entries) => StructuralConstraint.Closed(entries);
    private static IElement[] E(Relation scope) => StructuralConstraint.TryReadScope(scope, out var entries, out _, out _) ? entries : [];
}
