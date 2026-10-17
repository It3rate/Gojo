using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeFrieze;

namespace Gojo.Experiments.QualitativeExecution.Tests;

public sealed class QualitativeGraphJoinTests
{
    [Fact]
    public void EqualFormalPositionsRemainSeparateWithoutADeclaration()
    {
        var fixture = Fixture(); var join = new QualitativeGraphJoin(fixture.Graph, fixture.Drawing.SiteBindings, S());
        Assert.True(join.TryReadPassed(join.Evaluate())); Assert.Equal(4, E(join.Graph.Sites).Length);
        Assert.Same(fixture.Out, Target(join, fixture.Out)); Assert.Same(fixture.In, Target(join, fixture.In));
        Assert.NotSame(Target(join, fixture.Out), Target(join, fixture.In)); Assert.Empty(E(join.Accepted));
        Assert.Equal(4, join.Graph.Describe(join.Graph.Evaluate()).Endpoints);
    }

    [Fact]
    public void ExplicitEqualPositionSitesJoinThroughANativeClassAndRetainAllSourceIdentities()
    {
        var fixture = Fixture(); var declaration = Declare(fixture.Out, fixture.In, fixture.Passage);
        var before = fixture.Drawing.State; var originalSites = fixture.Graph.Sites; var originalStrokes = fixture.Graph.Strokes;
        var join = new QualitativeGraphJoin(fixture.Graph, fixture.Drawing.SiteBindings, S(declaration));
        Assert.True(join.TryReadPassed(join.Evaluate())); Assert.Same(declaration, Assert.Single(E(join.Accepted)));
        Assert.Same(Target(join, fixture.Out), Target(join, fixture.In)); Assert.NotSame(fixture.Out, Target(join, fixture.Out));
        Assert.Equal(3, E(join.Graph.Sites).Length); Assert.Equal(4, E(join.SiteMap).Length);
        var partition = Assert.Single(E(join.Classes).Cast<Relation>(), row => E(Assert.IsType<Relation>(Assert.IsType<Relation>(row.Left).Left)).Length == 2);
        var evidence = Assert.IsType<Relation>(partition.Right);
        Assert.Same(declaration, Assert.Single(E(Assert.IsType<Relation>(evidence.Left))));
        Assert.Single(E(Assert.IsType<Relation>(evidence.Right))); Assert.Single(E(join.BindingChecks));
        var summary = join.Graph.Describe(join.Graph.Evaluate()); Assert.Equal(2, summary.Endpoints); Assert.Equal(1, summary.L);
        Assert.Same(before, fixture.Drawing.State); Assert.Same(originalSites, fixture.Graph.Sites); Assert.Same(originalStrokes, fixture.Graph.Strokes);
        Assert.Contains(Traverse(join.Surface), item => ReferenceEquals(item, fixture.Out));
        Assert.Contains(Traverse(join.Surface), item => ReferenceEquals(item, fixture.In));
        Assert.Contains(Traverse(join.Surface), item => ReferenceEquals(item, fixture.Passage));
        Assert.DoesNotContain(Traverse(join.Surface), item => item is Carrier or CarrierLocation);
    }

    [Fact]
    public void KnownDifferentPositionsBlockTheDeclarationWithoutMergingSites()
    {
        var fixture = Fixture(); var start = E(fixture.Graph.Sites)[0];
        var declaration = Declare(start, fixture.In);
        var join = new QualitativeGraphJoin(fixture.Graph, fixture.Drawing.SiteBindings, S(declaration));
        Assert.False(join.TryReadPassed(join.Evaluate())); Assert.Empty(E(join.Accepted)); Assert.Single(E(join.Blocked));
        Assert.Empty(E(join.Unresolved)); Assert.Equal(4, E(join.Graph.Sites).Length);
        Assert.NotSame(Target(join, start), Target(join, fixture.In));
        Assert.Same(declaration, Assert.IsType<Relation>(Assert.Single(E(join.Blocked))).Left);
    }

    [Fact]
    public void ForeignAndLookalikeParticipantsCannotBorrowAnActualSiteBinding()
    {
        var fixture = Fixture(); var foreign = new Placeholder();
        var bindings = S([.. E(fixture.Drawing.SiteBindings), new Relation(foreign, Binding(fixture, fixture.Out).Right)]);
        var declaration = Declare(fixture.Out, foreign);
        var join = new QualitativeGraphJoin(fixture.Graph, bindings, S(declaration));
        Assert.False(join.TryReadPassed(join.Evaluate())); Assert.Empty(E(join.Accepted)); Assert.Single(E(join.Blocked));
        Assert.NotEmpty(E(join.Unresolved)); Assert.Equal(4, E(join.Graph.Sites).Length);
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("duplicate")]
    [InlineData("open-scope")]
    [InlineData("open-word")]
    public void MissingAmbiguousAndOpenBindingsLeaveTheProposedJoinUnresolved(string mode)
    {
        var fixture = Fixture(); var entries = E(fixture.Drawing.SiteBindings);
        Relation bindings = mode switch {
            "missing" => S(entries.Where(item => !ReferenceEquals(item, Binding(fixture, fixture.In))).ToArray()),
            "duplicate" => S([.. entries, new Relation(fixture.In, Binding(fixture, fixture.In).Right)]),
            "open-scope" => StructuralConstraint.Open(entries),
            _ => S(entries.Select(item => item is Relation row && ReferenceEquals(row.Left, fixture.In)
                ? (IElement)new Relation(row.Left, StructuralConstraint.Open(E(Assert.IsType<Relation>(row.Right)))) : item).ToArray())
        };
        var join = new QualitativeGraphJoin(fixture.Graph, bindings, S(Declare(fixture.Out, fixture.In)));
        Assert.False(join.TryReadPassed(join.Evaluate())); Assert.Empty(E(join.Accepted)); Assert.NotEmpty(E(join.Unresolved));
        Assert.NotSame(Target(join, fixture.Out), Target(join, fixture.In));
        Assert.False(Closed(join.Bindings)); Assert.False(Closed(join.Graph.Strokes));
    }

    [Fact]
    public void OpenDeclarationScopeKeepsItsUnaccountedTailVisible()
    {
        var fixture = Fixture(); var declaration = Declare(fixture.Out, fixture.In);
        var join = new QualitativeGraphJoin(fixture.Graph, fixture.Drawing.SiteBindings, StructuralConstraint.Open(declaration));
        Assert.False(join.TryReadPassed(join.Evaluate())); Assert.Contains(E(join.Accepted), row => ReferenceEquals(row, declaration));
        Assert.Contains(E(join.Unresolved), item => ReferenceEquals(item, join.DeclaredJoins));
        Assert.False(Closed(join.Graph.Strokes));
    }

    [Fact]
    public void EverySourceStrokeOccurrenceIsCopiedOnceWithItsActualCauseAndChord()
    {
        var fixture = Fixture(); var repeated = E(fixture.Graph.Strokes)[0];
        var source = new QualitativeStrokeGraph(fixture.Drawing.Frame, fixture.Graph.Sites, S([.. E(fixture.Graph.Strokes), repeated]));
        var join = new QualitativeGraphJoin(source, fixture.Drawing.SiteBindings, S(Declare(fixture.Out, fixture.In)));
        Assert.True(join.TryReadPassed(join.Evaluate())); Assert.Equal(3, E(join.Graph.Strokes).Length); Assert.Equal(3, E(join.StrokeMap).Length);
        var original = E(source.Strokes); var mapped = E(join.Graph.Strokes);
        for (var index = 0; index < original.Length; index++)
        {
            var sourceStroke = Assert.IsType<Relation>(original[index]); var output = Assert.IsType<Relation>(mapped[index]);
            Assert.True(QualitativeStrokeGraph.TryReadStroke(sourceStroke, out _, out var a, out var b, out var word));
            Assert.True(QualitativeStrokeGraph.TryReadStroke(output, out var cause, out var x, out var y, out var outputWord));
            Assert.Same(sourceStroke, cause); Assert.Same(word, outputWord); Assert.Same(Target(join, a), x); Assert.Same(Target(join, b), y);
            var row = Assert.IsType<Relation>(E(join.StrokeMap)[index]); Assert.Same(sourceStroke, row.Left); Assert.Same(output, row.Right);
        }
        Assert.NotSame(mapped[0], mapped[2]);
    }

    [Fact]
    public void TransitiveClassesContainOnlySitesConnectedByDeclaredAcceptedPairs()
    {
        var frame = new QualitativeMotionFrame(); var sites = new IElement[] { new Placeholder(), new Placeholder(), new Placeholder(), new Placeholder() };
        var source = new QualitativeStrokeGraph(frame, S(sites), S()); var bindings = S(sites.Select(site => (IElement)new Relation(site, S())).ToArray());
        var first = Declare(sites[0], sites[1]); var second = Declare(sites[1], sites[2]);
        var join = new QualitativeGraphJoin(source, bindings, S(first, second));
        Assert.True(join.TryReadPassed(join.Evaluate())); Assert.Equal(2, E(join.Classes).Length);
        Assert.Same(Target(join, sites[0]), Target(join, sites[2])); Assert.NotSame(Target(join, sites[0]), Target(join, sites[3]));
        Assert.Equal(2, E(join.Accepted).Length); Assert.Equal(4, E(join.SiteMap).Length);
    }

    [Fact]
    public void FabricatedReadGraphAndSiteMapCannotReplaceTheCapturedNativeResult()
    {
        var fixture = Fixture(); var join = new QualitativeGraphJoin(fixture.Graph, fixture.Drawing.SiteBindings, S(Declare(fixture.Out, fixture.In)));
        var read = join.Evaluate(); Assert.True(join.TryReadPassed(new Relation(read.Left, read.Right)));
        Assert.False(join.TryReadPassed(new Relation(join.Surface, new Relation(S(), new Relation(S(), S())))));
        var other = new QualitativeGraphJoin(fixture.Graph, fixture.Drawing.SiteBindings, S());
        Assert.False(join.TryReadPassed(other.Evaluate()));
        var forgedGraph = new QualitativeStrokeGraph(join.Source.Frame, S(new Placeholder()), join.Graph.Strokes);
        Assert.False(join.TryReadPassed(new Relation(join.Surface, new Relation(forgedGraph.Surface, read.Right))));
        var forgedMap = S(E(join.SiteMap).Cast<Relation>().Select(row => (IElement)new Relation(row.Left, new Placeholder())).ToArray());
        Assert.False(join.TryReadPassed(new Relation(join.Surface, new Relation(forgedMap, read.Right))));
    }

    private sealed record Example(QualitativeDrawingReceiver Drawing, QualitativeStrokeGraph Graph, IElement Out, IElement In, Relation Passage);
    private static Example Fixture()
    {
        var frame = new QualitativeMotionFrame(); var drawing = new QualitativeDrawingReceiver(frame); var reach = new Placeholder();
        drawing.Apply(new Placeholder(), S(drawing.Draw(S(QualitativeMotionFrame.Move(frame.Right, reach)))), new Placeholder());
        var outgoing = drawing.CursorSite;
        var passage = drawing.Apply(new Placeholder(), S(drawing.Jump(S())), new Placeholder()); var incoming = drawing.CursorSite;
        drawing.Apply(new Placeholder(), S(drawing.Draw(S(QualitativeMotionFrame.Move(frame.Up, reach)))), new Placeholder());
        var graph = new QualitativeStrokeGraph(frame, S(E(drawing.SiteBindings).Cast<Relation>().Select(row => row.Left).ToArray()), drawing.Strokes);
        return new(drawing, graph, outgoing, incoming, passage);
    }
    private static Relation Binding(Example fixture, IElement site) => E(fixture.Drawing.SiteBindings).Cast<Relation>().Single(row => ReferenceEquals(row.Left, site));
    private static Relation Declare(IElement first, IElement second, IElement? cause = null) => new(new Relation(first, second), cause ?? new Placeholder());
    private static IElement Target(QualitativeGraphJoin join, IElement source) => E(join.SiteMap).Cast<Relation>().Single(row => ReferenceEquals(row.Left, source)).Right;
    private static IEnumerable<IElement> Traverse(IElement root)
    {
        var seen = new HashSet<IElement>(ReferenceEqualityComparer.Instance); var stack = new Stack<IElement>(); stack.Push(root);
        while (stack.TryPop(out var element))
        { if (!seen.Add(element)) continue; yield return element; if (element is Relation row) { stack.Push(row.Left); stack.Push(row.Right); } }
    }
    private static Relation S(params IElement[] values) => StructuralConstraint.Closed(values);
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
    private static bool Closed(Relation scope) => StructuralConstraint.TryReadScope(scope, out _, out var seam, out var tail) && seam.Equals(tail);
}
