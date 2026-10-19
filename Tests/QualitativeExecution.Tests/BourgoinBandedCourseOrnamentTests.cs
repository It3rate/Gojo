using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeFrieze;

namespace Gojo.Experiments.QualitativeExecution.Tests;

public sealed class BourgoinBandedCourseOrnamentTests
{
    private static readonly Lazy<IReadOnlyList<BourgoinOrnamentRefinedLibrary.RefinedDesign>> Variants = new(() =>
    {
        var sources = new BourgoinPatternLibrary(8).Recipes;
        var index = sources.ToList().FindIndex(source => source.Id == "B32");
        return BourgoinOrnamentRefinedLibrary.Create(sources[index], index, ["field", "motif", "detail"]);
    });

    [Theory]
    [InlineData("field")]
    [InlineData("motif")]
    [InlineData("detail")]
    public void EveryScaleKeepsTheActualOpenCoursesInOneRetainedLayer(string mode)
    {
        var variant = Variants.Value.Single(item => item.Design.Mode == mode);
        var design = variant.Design; var enrichment = design.Enrichment;
        Assert.True(enrichment.IsComplete);
        Assert.Empty(E(enrichment.Source.ClosedPaths));
        var source = E(enrichment.Source.Paths);
        Assert.Equal(4, source.Length);
        foreach (var path in source)
            Assert.Contains(E(enrichment.Result.Paths), actual => ReferenceEquals(actual, path));
        foreach (var port in E(enrichment.Source.Ports))
            Assert.Contains(E(enrichment.Result.Ports), actual => ReferenceEquals(actual, port));
        foreach (var layer in E(enrichment.Layers).Cast<Relation>())
        {
            Assert.True(enrichment.TryReadLayer(layer, out var definition, out _, out _, out _, out var receipts));
            var selected = E((Relation)((Relation)definition.Right).Left);
            foreach (var path in source)
                Assert.Equal(ReferenceEquals(definition.Left, design.FieldLayer), selected.Any(actual => ReferenceEquals(actual, path)));
            foreach (var receipt in E(receipts).Cast<Relation>())
            {
                var attachment = Assert.IsType<Relation>(receipt.Left);
                var port = Assert.IsType<Relation>(attachment.Left);
                Assert.Contains(E(enrichment.Source.Ports), actual => ReferenceEquals(actual, port));
            }
        }
        // Thickening this read cannot authenticate any new source join.
        foreach (var port in E(enrichment.Result.Ports).Cast<Relation>())
            if (port.Right is Relation pick && source.Any(path => ReferenceEquals(pick.Left, path)))
                Assert.Contains(E(enrichment.Source.Ports), actual => ReferenceEquals(actual, port));
    }

    [Theory]
    [InlineData("field", 1)]
    [InlineData("motif", 2)]
    [InlineData("detail", 3)]
    public void TheRestrainedInlayHasGenuineFourfoldDiamondSymmetryWithoutCurvedFloralPaths(string mode, int outlines)
    {
        var variant = Variants.Value.Single(item => item.Design.Mode == mode);
        var design = variant.Design; var enrichment = design.Enrichment;
        var original = E(enrichment.Source.Paths);
        var added = E(enrichment.Result.Paths).Cast<Relation>()
            .Where(path => !original.Any(source => ReferenceEquals(source, path))).ToArray();
        Assert.Equal(outlines, added.Length);
        var courseDirections = original.Cast<Relation>().SelectMany(path =>
        {
            var points = Points(design.Source.Frame, path);
            return points.Zip(points.Skip(1), (a, b) => (X: b.X - a.X, Y: b.Y - a.Y));
        }).ToArray();
        (int X, int Y)? commonCentre = null;
        foreach (var path in added)
        {
            Assert.Contains(E(enrichment.Result.ClosedPaths), actual => ReferenceEquals(actual, path));
            var points = Points(design.Source.Frame, path);
            Assert.Equal(points[0], points[^1]); Assert.Equal(5, points.Length);
            var centre = ((points.Min(point => point.X) + points.Max(point => point.X)) / 2,
                (points.Min(point => point.Y) + points.Max(point => point.Y)) / 2);
            commonCentre ??= centre; Assert.Equal(commonCentre.Value, centre);
            var local = points.Take(4).Select(point => (X: point.X - centre.Item1, Y: point.Y - centre.Item2)).ToHashSet();
            Assert.Equal(4, local.Count);
            foreach (var point in local)
            {
                Assert.True(point.X == 0 || point.Y == 0);
                Assert.Contains((-point.Y, point.X), local);
            }
            foreach (var (first, second) in points.Zip(points.Skip(1)))
            {
                var edge = (X: second.X - first.X, Y: second.Y - first.Y);
                Assert.Contains(courseDirections, direction => (long)direction.X * edge.Y == (long)direction.Y * edge.X);
            }
            Assert.InRange(points.Max(point => point.X) - points.Min(point => point.X), 1, 16);
        }
        var hint = Assert.IsType<Relation>(Assert.Single(E(variant.CurveHints)));
        Assert.True(OrnamentCurveSelection.TryRead(design.Source.Frame, enrichment.ResultSurface, hint, out var curves));
        Assert.Empty(E(curves.Selected));
        Assert.Equal(E(enrichment.Result.Paths).Length, E(curves.Complement).Length);
    }

    private static (int X, int Y)[] Points(QualitativeMotionFrame frame, Relation path)
    {
        var cursor = Word((Relation)path.Left); var result = new List<(int X, int Y)> { cursor };
        foreach (var chord in E((Relation)path.Right).Cast<Relation>())
        { var step = Word(chord); cursor = (cursor.X + step.X, cursor.Y + step.Y); result.Add(cursor); }
        return result.ToArray();

        (int X, int Y) Word(Relation word)
        {
            var x = 0; var y = 0;
            foreach (var move in E(word).Cast<Relation>())
            {
                Assert.True(frame.Direction(move, out var axis, out var forward, out _));
                if (ReferenceEquals(axis, frame.Horizontal)) x += forward ? 1 : -1; else y += forward ? 1 : -1;
            }
            return (x, y);
        }
    }

    private static IElement[] E(Relation surface) => FiniteLaw.Entries(surface);
}
