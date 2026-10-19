using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;
using Gojo.Experiments.QualitativeFrieze;

namespace Gojo.Experiments.QualitativeExecution.Tests;

public sealed class BourgoinCorrectedNetworkTests
{
    private static readonly Lazy<BourgoinPatternLibrary> Library = new(() => new());

    [Fact]
    public void B02ReusesOneElongatedRectangleSeedForFourActualImagesWithoutDrawingSquareMotifs()
    {
        var recipe = Recipe("B02"); var motif = Motif(recipe);
        var paths = E(motif.Paths).Cast<Relation>().ToArray(); Assert.Equal(4, paths.Length);
        Assert.Equal(4, E(motif.Closed).Length);
        var evidence = E(motif.Evidence).Cast<Relation>().ToArray(); Assert.Equal(4, evidence.Length);
        var copies = evidence.Select(row => Assert.IsType<Relation>(row.Left)).ToArray();
        Assert.All(copies, copy => Assert.Same(copies[0].Left, copy.Left));
        Assert.All(copies, copy => Assert.Contains(paths, path => ReferenceEquals(path, copy.Right)));
        var dimensions = paths.Select(path => Bounds(recipe.Frame, path)).ToArray();
        Assert.Equal(2, dimensions.Count(size => size == (2, 4))); Assert.Equal(2, dimensions.Count(size => size == (4, 2)));
        Assert.All(paths, path => Assert.All(E((Relation)path.Right).Cast<Relation>(), word => Assert.True(AxisOnly(recipe.Frame, word))));
        Assert.Equal((6, 0), Position(recipe.Frame, recipe.ColumnPitch)); Assert.Equal((0, 6), Position(recipe.Frame, recipe.RowPitch));
        var field = Field(recipe); Assert.True(field.IsComplete);
        Assert.True(HasFourEdgeRoute(field.Graph, diagonal: false));
    }

    [Fact]
    public void B06SecondReadHasUprightFourEdgeRoutesWhereTheEarlierReadHasDiagonalOnes()
    {
        var old = Recipe("B06"); var revised = Recipe("B06.B");
        var a = Field(old); var b = Field(revised); Assert.True(a.IsComplete); Assert.True(b.IsComplete);
        Assert.True(HasFourEdgeRoute(a.Graph, diagonal: true));
        Assert.False(HasFourEdgeRoute(a.Graph, diagonal: false));
        Assert.True(HasFourEdgeRoute(b.Graph, diagonal: false));
        Assert.False(HasFourEdgeRoute(b.Graph, diagonal: true));
        Assert.Single(E(Motif(revised).Paths)); Assert.Equal((6, 6), Position(revised.Frame, revised.ColumnPitch));
        Assert.Equal((-6, 6), Position(revised.Frame, revised.RowPitch));
        // These are actual shared-site, zero-displacement route witnesses.
        // They do not certify occupied faces, region interiors or Euclidean squares.
    }

    [Fact]
    public void B08HasTwoStraightPairedRibbonFamiliesRatherThanInternalCrankTurns()
    {
        var recipe = Recipe("B08"); var paths = E(Motif(recipe).Paths).Cast<Relation>().ToArray(); Assert.Equal(4, paths.Length);
        var headings = new List<(int X, int Y)>();
        foreach (var path in paths)
        {
            var chords = E((Relation)path.Right).Cast<Relation>().ToArray(); Assert.Equal(2, chords.Length);
            var first = Position(recipe.Frame, chords[0]); var second = Position(recipe.Frame, chords[1]);
            Assert.True(first is (2, 1) or (-2, 1)); Assert.Equal((first.X * 3, first.Y * 3), second);
            headings.Add(first);
        }
        Assert.Equal(2, headings.Count(heading => heading == (2, 1))); Assert.Equal(2, headings.Count(heading => heading == (-2, 1)));
        Assert.Empty(E(Motif(recipe).Closed)); Assert.NotEmpty(E(Motif(recipe).Evidence));
        var field = Field(recipe); Assert.True(field.IsComplete); Assert.NotEmpty(E(field.Accepted));
        Assert.True(field.Graph.Describe(field.Graph.Evaluate()).X > 0);
    }

    [Fact]
    public void B09HasTwoFreeEndsPerDetachedSidewaysSAndNoBodySeams()
    {
        var recipe = Recipe("B09"); var motif = Motif(recipe); Assert.Empty(E(motif.Closed));
        var path = Assert.IsType<Relation>(Assert.Single(E(motif.Paths))); Assert.Equal(7, E((Relation)path.Right).Length);
        var run = recipe.Program.Generate(recipe.Bindings, SmallWindow(recipe)); var field = new OrnamentProgramField(run, recipe.SeamContract);
        Assert.True(run.IsComplete); Assert.True(field.IsComplete); Assert.Empty(E(field.Accepted)); Assert.Empty(E(field.SeamReads));
        Assert.All(run.Units, unit =>
        {
            Assert.Equal(2, unit.Current.Join.Graph.Describe(unit.Current.Join.Graph.Evaluate()).Endpoints);
            Assert.Empty(E(unit.Current.Join.Accepted));
        });
        Assert.Equal(run.Units.Count * 2, field.Graph.Describe(field.Graph.Evaluate()).Endpoints);
        Assert.Equal((4, 0), Position(recipe.Frame, recipe.ColumnPitch)); Assert.Equal((6, 3), Bounds(recipe.Frame, path));
    }

    [Fact]
    public void B08SecondReadReusesOneBentSeedAndRetainsActualJunctionCorners()
    {
        var recipe = Recipe("B08.B"); var motif = Motif(recipe);
        var evidence = E(motif.Evidence).Cast<Relation>().ToArray(); Assert.Equal(4, evidence.Length);
        var prototype = Assert.IsType<Relation>(evidence[0].Left).Left;
        Assert.All(evidence, row => Assert.Same(prototype, Assert.IsType<Relation>(row.Left).Left));
        Assert.All(E(motif.Paths).Cast<Relation>(), path =>
        {
            var words = E((Relation)path.Right).Cast<Relation>().ToArray(); Assert.Equal(2, words.Length);
            var a = Position(recipe.Frame, words[0]); var b = Position(recipe.Frame, words[1]);
            Assert.Equal((1, 2), (Math.Abs(a.X), a.Y)); Assert.Equal((7, 4), (Math.Abs(b.X), b.Y));
            Assert.NotEqual(a.X * b.Y, a.Y * b.X);
        });
        var field = Field(recipe); Assert.True(field.IsComplete); Assert.NotEmpty(E(field.Accepted));
        Assert.True(HasFourEdgeRoute(field.Graph, diagonal: true));
    }

    private static BourgoinPatternLibrary.Recipe Recipe(string id) => Library.Value.Recipes.Single(recipe => recipe.Id == id);
    private static OrnamentProgramField Field(BourgoinPatternLibrary.Recipe recipe) => new(recipe.Program.Generate(recipe.Bindings, SmallWindow(recipe)), recipe.SeamContract);
    private static Relation SmallWindow(BourgoinPatternLibrary.Recipe recipe)
    {
        var phases = (Relation)recipe.Window.Left;
        return OrnamentProgram.Window(phases.Left, phases.Right, 2, 2);
    }
    private static (Relation Paths, Relation Closed, Relation Evidence) Motif(BourgoinPatternLibrary.Recipe recipe)
    {
        var motif = Assert.IsType<Relation>(Assert.Single(E(recipe.Program.Motifs)));
        var body = Assert.IsType<Relation>(motif.Right); var pathsPorts = Assert.IsType<Relation>(body.Left); var remainder = Assert.IsType<Relation>(body.Right);
        return ((Relation)pathsPorts.Left, (Relation)remainder.Left, (Relation)remainder.Right);
    }
    private static (int Width, int Height) Bounds(QualitativeMotionFrame frame, Relation path)
    {
        var position = Position(frame, (Relation)path.Left); var positions = new List<(int X, int Y)> { position };
        foreach (var word in E((Relation)path.Right).Cast<Relation>())
        { var delta = Position(frame, word); position = (position.X + delta.X, position.Y + delta.Y); positions.Add(position); }
        return (positions.Max(p => p.X) - positions.Min(p => p.X), positions.Max(p => p.Y) - positions.Min(p => p.Y));
    }
    private static (int X, int Y) Position(QualitativeMotionFrame frame, Relation word)
    {
        var read = frame.Reduce(S(), word); Assert.True(frame.TryReadResult(read, out var remaining, out _, out var pending)); Assert.Empty(E(pending));
        Assert.True(StructuralConstraint.TryReadScope(remaining, out var moves, out var seam, out var tail)); Assert.Equal(seam, tail);
        var x = 0; var y = 0;
        foreach (var move in moves.Cast<Relation>())
        {
            Assert.True(frame.Direction(move, out var axis, out var forward, out _));
            if (ReferenceEquals(axis, frame.Horizontal)) x += forward ? 1 : -1; else y += forward ? 1 : -1;
        }
        return (x, y);
    }
    private static bool AxisOnly(QualitativeMotionFrame frame, Relation word)
    { var p = Position(frame, word); return (p.X == 0) != (p.Y == 0); }

    private static bool HasFourEdgeRoute(QualitativeStrokeGraph graph, bool diagonal)
    {
        var edges = E(graph.Strokes).Cast<Relation>().ToArray();
        var rows = edges.Select(edge =>
        {
            Assert.True(QualitativeStrokeGraph.TryReadStroke(edge, out _, out var from, out var to, out var word));
            return (Edge: edge, From: from, To: to, Word: word);
        }).Where(edge =>
        {
            var p = Position(graph.Frame, edge.Word);
            return diagonal ? p.X != 0 && p.Y != 0 : (p.X == 0) != (p.Y == 0);
        }).ToArray();
        var adjacency = new Dictionary<IElement, List<(Relation Edge, IElement To, Relation Word)>>(ReferenceEqualityComparer.Instance);
        foreach (var edge in rows)
        {
            Add(edge.From, edge.To, edge.Word);
            Add(edge.To, edge.From, Inverse(graph.Frame, edge.Word));
            void Add(IElement from, IElement to, Relation word)
            {
                if (!adjacency.TryGetValue(from, out var outgoing)) adjacency.Add(from, outgoing = []);
                outgoing.Add((edge.Edge, to, word));
            }
        }
        foreach (var root in adjacency.Keys)
        {
            var sites = new List<IElement> { root }; var used = new HashSet<Relation>(ReferenceEqualityComparer.Instance); var words = new List<Relation>();
            if (Visit(root)) return true;
            bool Visit(IElement current)
            {
                if (!adjacency.TryGetValue(current, out var outgoing)) return false;
                foreach (var edge in outgoing)
                {
                    if (used.Contains(edge.Edge)) continue;
                    var next = edge.To; var word = edge.Word;
                    if (words.Count == 3)
                    {
                        if (!Same(next, root)) continue;
                        var returnRead = graph.Frame.Reduce(S(), S(words.Append(word).SelectMany(E).ToArray()));
                        if (graph.Frame.TryReadResult(returnRead, out var remainder, out _, out var pending) && E(remainder).Length == 0 && E(pending).Length == 0) return true;
                        continue;
                    }
                    if (sites.Any(site => Same(site, next))) continue;
                    sites.Add(next); words.Add(word); used.Add(edge.Edge);
                    if (Visit(next)) return true;
                    sites.RemoveAt(sites.Count - 1); words.RemoveAt(words.Count - 1); used.Remove(edge.Edge);
                }
                return false;
            }
        }
        return false;
    }
    private static Relation Inverse(QualitativeMotionFrame frame, Relation word) => S(E(word).Reverse().Cast<Relation>().Select(move =>
        (IElement)QualitativeMotionFrame.Move(E(frame.Opposites).Cast<Relation>().Single(pair => Same(pair.Left, move.Left)).Right, move.Right)).ToArray());
    private static bool Same(IElement a, IElement b) => ReferenceEquals(a, b) || a is Placeholder p && b is Placeholder q && p.Equals(q);
    private static Relation S(params IElement[] elements) => StructuralConstraint.Closed(elements);
    private static IElement[] E(Relation scope) => StructuralConstraint.TryReadScope(scope, out var elements, out _, out _) ? elements : [];
}
