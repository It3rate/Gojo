using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;
using Gojo.Experiments.QualitativeFrieze;

namespace Gojo.Experiments.QualitativeExecution.Tests;

public sealed class BourgoinRibbonCourseCorrectionTests
{
    private static readonly Lazy<BourgoinPatternLibrary> Library = new(() => new());

    [Theory]
    [InlineData("B32", 4, 14, 2)]
    [InlineData("B33", 4, 14, 2)]
    public void OpenCourseDefinitionsAndActualSourceImagesReplaceTheClosedStripReading(string id, int paths, int chords, int prototypes)
    {
        var recipe = Recipe(id); var read = new BourgoinConstructionRead(recipe.Frame, (Relation)Assert.Single(E(recipe.Motifs)));
        Assert.True(read.IsReadable); Assert.Equal(paths, read.PathCount); Assert.Equal(chords, read.ChordCount);
        Assert.Equal(0, read.ClosedPathCount); Assert.Equal(paths, read.Images.Count); Assert.Equal(prototypes, E(read.Prototypes).Length);
        Assert.Empty(E(read.Blocked)); Assert.Empty(E(read.Unresolved));
        foreach (var path in E(read.Paths).Cast<Relation>()) foreach (var chord in E((Relation)path.Right).Cast<Relation>())
        {
            var delta = Position(recipe.Frame, chord);
            Assert.NotEqual(0, delta.X); Assert.Equal(Math.Abs(delta.X), Math.Abs(delta.Y));
        }
    }

    [Fact]
    public void B32RetainsTwoDistinctCentralFreeEndsInsteadOfOneConnectedSpiral()
    {
        var recipe = Recipe("B32"); var field = Field(recipe, 2, 2);
        Assert.True(field.IsComplete); var neighbours = Neighbours(field.Graph);
        var centralEnds = new[] { (X: -1, Y: -5), (X: 0, Y: -6) };
        foreach (var column in new[] { 0, 10 }) foreach (var row in new[] { 0, -10 })
        {
            var sites = centralEnds.Select(point => SiteAt(field, (point.X + column, point.Y + row))).ToArray();
            Assert.NotSame(sites[0], sites[1]);
            Assert.All(sites, site => Assert.Single(neighbours[site]));
            Assert.DoesNotContain(sites[1], neighbours[sites[0]]);
        }
        Assert.Contains(neighbours.Values, adjacent => adjacent.Count == 3);
    }

    [Theory]
    [InlineData("B32")]
    [InlineData("B33")]
    public void RemovingNeighbourSeamsBreaksTheRepeatedCoursesWithoutDeletingInk(string id)
    {
        var recipe = Recipe(id); var run = Run(recipe, 3, 2);
        var complete = new OrnamentProgramField(run, recipe.SeamContract);
        var local = E((Relation)recipe.SeamContract.Right).Cast<Relation>()
            .Where(rule => E((Relation)((Relation)rule.Right).Left).Length == 0).ToArray();
        var isolated = new OrnamentProgramField(run, OrnamentProgramField.Contract(recipe.Program, local));
        Assert.True(complete.IsComplete); Assert.True(isolated.IsComplete);
        Assert.Equal(E(complete.Graph.Strokes).Length, E(isolated.Graph.Strokes).Length);
        Assert.True(Components(complete.Graph) < Components(isolated.Graph));
        Assert.True(E(complete.Accepted).Length > E(isolated.Accepted).Length);
    }

    [Fact]
    public void B33OpenCourseAndAttachmentPairsCreateLoopsThroughActualTJoins()
    {
        var recipe = Recipe("B33"); var field = Field(recipe, 3, 2);
        Assert.True(field.IsComplete); var neighbours = Neighbours(field.Graph);
        Assert.Equal(4, Components(field.Graph));
        Assert.Equal(8, neighbours.Values.Count(adjacent => adjacent.Count == 1));
        Assert.Equal(24, neighbours.Values.Count(adjacent => adjacent.Count == 3));
        Assert.Equal(12, E(field.Graph.Strokes).Length - E(field.Graph.Sites).Length + Components(field.Graph));
        // Graph loops are actual shared-site circuits here. They do not alone
        // identify shaded/white faces or establish a filled-region algebra.
    }

    [Fact]
    public void B33FiniteChosenBasisReadFindsTheNarrowPassageAndKeepsTheDiamondPocketSeparate()
    {
        var field = Field(Recipe("B33"), 3, 3);
        var positions = E(field.Bindings).Cast<Relation>().ToDictionary(binding => binding.Left,
            binding => Position(field.Graph.Frame, (Relation)binding.Right), new ParticipantComparer());
        var segments = E(field.Graph.Strokes).Cast<Relation>().Select(stroke =>
        {
            Assert.True(QualitativeStrokeGraph.TryReadStroke(stroke, out _, out var from, out var to, out _));
            return (From: positions[from], To: positions[to]);
        }).ToArray();
        // Independent finite geometric diagnostic, NOT a native fill/face law.
        // Quarter-reach links with .325 clearance at both endpoints retain at
        // least .2 clearance along their whole length: every link point is
        // within .125 of an endpoint, and distance to ink is 1-Lipschitz.
        // This distinguishes the passage from a raster/ink-width accident.
        const int left = 8, right = 80, bottom = -88, top = -16;
        var available = new HashSet<(int X, int Y)>();
        for (var x = left; x <= right; x++) for (var y = bottom; y <= top; y++)
            if (segments.All(segment => DistanceSquared(x / 4d, y / 4d, segment.From, segment.To) > .325 * .325)) available.Add((x, y));
        var start = (X: 30, Y: -38); Assert.Contains(start, available);
        var reached = new HashSet<(int X, int Y)> { start }; var queue = new Queue<(int X, int Y)>(); queue.Enqueue(start);
        while (queue.TryDequeue(out var at))
            foreach (var step in new[] { (X: 1, Y: 0), (X: -1, Y: 0), (X: 0, Y: 1), (X: 0, Y: -1) })
            {
                var next = (X: at.X + step.X, Y: at.Y + step.Y);
                if (available.Contains(next) && reached.Add(next)) queue.Enqueue(next);
            }
        Assert.Contains((26, -46), reached); // Another arm of the same passage.
        Assert.Contains(reached, point => point.X == left);
        Assert.Contains(reached, point => point.X == right);
        Assert.Contains((36, -44), available); // Actual closed diamond's interior.
        Assert.DoesNotContain((36, -44), reached);
    }

    private static BourgoinPatternLibrary.Recipe Recipe(string id) => Library.Value.Recipes.Single(recipe => recipe.Id == id);
    private static OrnamentProgramRun Run(BourgoinPatternLibrary.Recipe recipe, int columns, int rows)
    {
        var origin = (Relation)recipe.Window.Left;
        var run = recipe.Program.Generate(recipe.Bindings, OrnamentProgram.Window(origin.Left, origin.Right, columns, rows));
        Assert.True(run.IsComplete); return run;
    }
    private static OrnamentProgramField Field(BourgoinPatternLibrary.Recipe recipe, int columns, int rows) => new(Run(recipe, columns, rows), recipe.SeamContract);
    private static IElement SiteAt(OrnamentProgramField field, (int X, int Y) point) => Assert.Single(E(field.Bindings).Cast<Relation>(),
        binding => Position(field.Graph.Frame, (Relation)binding.Right) == point).Left;
    private static Dictionary<IElement, HashSet<IElement>> Neighbours(QualitativeStrokeGraph graph)
    {
        var neighbours = new Dictionary<IElement, HashSet<IElement>>(ReferenceEqualityComparer.Instance);
        foreach (var site in E(graph.Sites)) neighbours.Add(site, new(ReferenceEqualityComparer.Instance));
        foreach (var edge in E(graph.Strokes).Cast<Relation>())
        {
            Assert.True(QualitativeStrokeGraph.TryReadStroke(edge, out _, out var from, out var to, out _));
            neighbours[from].Add(to); neighbours[to].Add(from);
        }
        return neighbours;
    }
    private static int Components(QualitativeStrokeGraph graph)
    {
        var neighbours = Neighbours(graph); var remaining = new HashSet<IElement>(neighbours.Keys, ReferenceEqualityComparer.Instance); var count = 0;
        while (remaining.Count != 0)
        {
            count++; var queue = new Queue<IElement>(); var first = remaining.First(); remaining.Remove(first); queue.Enqueue(first);
            while (queue.TryDequeue(out var site)) foreach (var next in neighbours[site]) if (remaining.Remove(next)) queue.Enqueue(next);
        }
        return count;
    }
    private static double DistanceSquared(double x, double y, (int X, int Y) from, (int X, int Y) to)
    {
        var dx = to.X - from.X; var dy = to.Y - from.Y;
        var t = Math.Clamp(((x - from.X) * dx + (y - from.Y) * dy) / (dx * dx + dy * dy), 0, 1);
        var a = x - from.X - t * dx; var b = y - from.Y - t * dy; return a * a + b * b;
    }
    private sealed class ParticipantComparer : IEqualityComparer<IElement>
    {
        public bool Equals(IElement? first, IElement? second) => ReferenceEquals(first, second);
        public int GetHashCode(IElement value) => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(value);
    }
    private static (int X, int Y) Position(QualitativeMotionFrame frame, Relation word)
    {
        var x = 0; var y = 0;
        foreach (var item in E(word).Cast<Relation>())
        {
            Assert.True(frame.Direction(item, out var axis, out var forward, out _));
            if (ReferenceEquals(axis, frame.Horizontal)) x += forward ? 1 : -1; else y += forward ? 1 : -1;
        }
        return (x, y);
    }
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
}
