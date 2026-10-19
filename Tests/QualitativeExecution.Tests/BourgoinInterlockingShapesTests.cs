using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;
using Gojo.Experiments.QualitativeFrieze;

namespace Gojo.Experiments.QualitativeExecution.Tests;

public sealed class BourgoinInterlockingShapesTests : IClassFixture<BourgoinInterlockingShapesTests.Catalog>
{
    public sealed class Catalog
    {
        public BourgoinPatternLibrary Library { get; } = new();
    }

    private readonly Catalog catalog;
    public BourgoinInterlockingShapesTests(Catalog catalog) => this.catalog = catalog;

    [Theory]
    [InlineData("B12", 16, 196, 14, -28)]
    [InlineData("B13", 28, 50, 10, -10)]
    [InlineData("B14", 20, 50, 10, -10)]
    public void ActualExecutableContoursAreClosedAndRetainTheTracedCornerOrder(string id, int corners, int doubleArea, int pitchX, int pitchY)
    {
        var recipe = Recipe(id); var motif = Motif(recipe);
        var paths = E(motif.Paths).Cast<Relation>().ToArray();
        Assert.Equal(4, paths.Length); Assert.Equal(4, E(motif.Closed).Length);
        foreach (var path in paths)
        {
            Assert.Contains(E(motif.Closed), actual => ReferenceEquals(actual, path));
            var points = Points(recipe.Frame, path);
            Assert.Equal(points[0], points[^1]);
            Assert.Equal(corners, CornerCount(points));
            Assert.Equal(doubleArea, Math.Abs(DoubleArea(points)));
            Assert.Equal(E((Relation)path.Right).Length + 1,
                E(motif.Ports).Cast<Relation>().Count(port => ReferenceEquals(((Relation)port.Right).Left, path)));
        }
        Assert.Equal((pitchX, 0), Position(recipe.Frame, recipe.ColumnPitch));
        Assert.Equal((0, pitchY), Position(recipe.Frame, recipe.RowPitch));
    }

    [Theory]
    [InlineData("B12", 16, 2)]
    [InlineData("B13", 28, 1)]
    [InlineData("B14", 20, 1)]
    public void NativeCornerCorrespondencesSelectActualSourceChordsAndActualExecutablePorts(string id, int corners, int prototypes)
    {
        var recipe = Recipe(id); var motif = Motif(recipe);
        var evidence = E(motif.Evidence).Cast<Relation>().ToArray(); Assert.Equal(8, evidence.Length);
        var mappings = evidence.Where((_, index) => index % 2 == 1).ToArray();
        var sources = new HashSet<IElement>(ReferenceEqualityComparer.Instance);
        foreach (var mapping in mappings)
        {
            var pair = Assert.IsType<Relation>(mapping.Left);
            var source = Assert.IsType<Relation>(pair.Left); sources.Add(source);
            var sourcePath = Assert.IsType<Relation>(source.Left);
            var output = Assert.IsType<Relation>(pair.Right);
            Assert.Contains(E(motif.Paths), path => ReferenceEquals(path, output));
            var ordered = Assert.IsType<Relation>(mapping.Right);
            var rows = E(Assert.IsType<Relation>(ordered.Left)).Cast<Relation>().ToArray();
            var successors = E(Assert.IsType<Relation>(ordered.Right)).Cast<Relation>().ToArray();
            Assert.Equal(corners, rows.Length); Assert.Equal(corners, successors.Length);
            foreach (var row in rows)
            {
                var sourcePick = Assert.IsType<Relation>(row.Left);
                Assert.Same(sourcePath, sourcePick.Left);
                Assert.Contains(E((Relation)sourcePath.Right), chord => ReferenceEquals(chord, sourcePick.Right));
                var actualPort = Assert.IsType<Relation>(row.Right);
                Assert.Contains(E(motif.Ports), port => ReferenceEquals(port, actualPort));
                Assert.Same(output, Assert.IsType<Relation>(actualPort.Right).Left);
            }
            for (var index = 0; index < rows.Length; index++)
            {
                Assert.Same(rows[index], successors[index].Left);
                Assert.Same(rows[(index + 1) % rows.Length], successors[index].Right);
            }
        }
        Assert.Equal(prototypes, sources.Count);
    }

    [Theory]
    [InlineData("B12", 14, 28)]
    [InlineData("B13", 10, 10)]
    [InlineData("B14", 10, 10)]
    public void ClosedChambersTileTheSelectedFormalDisplayWithoutHolesOrOverlaps(string id, int width, int height)
    {
        var recipe = Recipe(id); var motif = Motif(recipe);
        var contours = E(motif.Paths).Cast<Relation>().Select(path => Points(recipe.Frame, path)).ToArray();
        // This is an independent geometric audit in the chosen formal basis,
        // not a runtime face-inference or Euclidean-regularity claim. Unequal
        // offsets avoid sampling directly on the diagonal boundary strokes.
        for (var x = 0; x < width * 2; x++) for (var y = 0; y < height; y++)
        {
            var covering = 0;
            foreach (var contour in contours)
                for (var c = -2; c <= 3; c++) for (var r = -2; r <= 2; r++)
                    if (Inside(contour, x + .213 - c * width, -y - .391 + r * height)) covering++;
            Assert.Equal(1, covering);
        }
    }

    [Theory]
    [InlineData("B12")]
    [InlineData("B13")]
    [InlineData("B14")]
    public void NeighbouringContourCornersNeverRequireAnUnpublishedInteriorStrokeJoin(string id)
    {
        var recipe = Recipe(id); var motif = Motif(recipe);
        var contours = E(motif.Paths).Cast<Relation>().Select(path => Points(recipe.Frame, path)).ToArray();
        var column = Position(recipe.Frame, recipe.ColumnPitch); var row = Position(recipe.Frame, recipe.RowPitch);
        // This is the source authoring seam stencil, including its reverses.
        // The test reads actual executable vertices and period words; it does
        // not infer shared identity from coincident display coordinates.
        var offsets = new (int C, int R)[] { (0, 0), (1, 0), (0, 1), (1, 1), (-1, 1), (-1, 0), (0, -1), (-1, -1), (1, -1) };
        foreach (var contour in contours)
            for (var edge = 0; edge < contour.Length - 1; edge++)
            {
                var from = contour[edge]; var to = contour[edge + 1];
                var dx = to.X - from.X; var dy = to.Y - from.Y;
                foreach (var neighbour in contours) foreach (var offset in offsets)
                    foreach (var vertex in neighbour.Take(neighbour.Length - 1))
                    {
                        var px = vertex.X + offset.C * column.X + offset.R * row.X - from.X;
                        var py = vertex.Y + offset.C * column.Y + offset.R * row.Y - from.Y;
                        var projection = px * dx + py * dy;
                        Assert.False(px * dy == py * dx && projection > 0 && projection < dx * dx + dy * dy,
                            $"{id}: a neighbouring source corner meets an unexposed stroke interior.");
                    }
            }
    }

    private BourgoinPatternLibrary.Recipe Recipe(string id) => catalog.Library.Recipes.Single(recipe => recipe.Id == id);
    private sealed record NativeMotif(Relation Paths, Relation Ports, Relation Closed, Relation Evidence);
    private static NativeMotif Motif(BourgoinPatternLibrary.Recipe recipe)
    {
        var surface = Assert.IsType<Relation>(Assert.Single(E(recipe.Motifs)));
        var body = Assert.IsType<Relation>(surface.Right); var pathsPorts = Assert.IsType<Relation>(body.Left);
        var closure = Assert.IsType<Relation>(body.Right);
        return new((Relation)pathsPorts.Left, (Relation)pathsPorts.Right, (Relation)closure.Left, (Relation)closure.Right);
    }
    private static (int X, int Y)[] Points(QualitativeMotionFrame frame, Relation path)
    {
        var cursor = Position(frame, (Relation)path.Left); var points = new List<(int X, int Y)> { cursor };
        foreach (var word in E((Relation)path.Right).Cast<Relation>())
        {
            var delta = Position(frame, word); cursor = (cursor.X + delta.X, cursor.Y + delta.Y); points.Add(cursor);
        }
        return points.ToArray();
    }
    private static (int X, int Y) Position(QualitativeMotionFrame frame, Relation word)
    {
        var x = 0; var y = 0;
        foreach (var move in E(word).Cast<Relation>())
        {
            Assert.True(frame.Direction(move, out var axis, out var forward, out _));
            if (ReferenceEquals(axis, frame.Horizontal)) x += forward ? 1 : -1; else y += forward ? 1 : -1;
        }
        return (x, y);
    }
    private static int CornerCount((int X, int Y)[] points)
    {
        var count = 0; var length = points.Length - 1;
        for (var index = 0; index < length; index++)
        {
            var before = points[(index + length - 1) % length]; var current = points[index]; var after = points[(index + 1) % length];
            var a = (X: current.X - before.X, Y: current.Y - before.Y); var b = (X: after.X - current.X, Y: after.Y - current.Y);
            if (a.X * b.Y != a.Y * b.X || a.X * b.X + a.Y * b.Y <= 0) count++;
        }
        return count;
    }
    private static int DoubleArea((int X, int Y)[] points) => Enumerable.Range(0, points.Length - 1)
        .Sum(index => points[index].X * points[index + 1].Y - points[index + 1].X * points[index].Y);
    private static bool Inside((int X, int Y)[] points, double x, double y)
    {
        var result = false;
        for (var index = 0; index < points.Length - 1; index++)
        {
            var a = points[index]; var b = points[index + 1];
            if ((a.Y > y) != (b.Y > y) && x < (b.X - a.X) * (y - a.Y) / (b.Y - a.Y) + a.X) result = !result;
        }
        return result;
    }
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
}
