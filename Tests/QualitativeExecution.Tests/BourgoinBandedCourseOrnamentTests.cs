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
    [InlineData("field", 0, 0)]
    [InlineData("motif", 10, 0)]
    [InlineData("detail", 18, 8)]
    public void OnlyOrnamentalReadingsAddSmallFourfoldRosettesAtTheActualOpposedCrossingPorts(
        string mode, int addedPaths, int midribs)
    {
        var variant = Variants.Value.Single(item => item.Design.Mode == mode);
        var design = variant.Design; var enrichment = design.Enrichment; var frame = design.Source.Frame;
        var source = E(enrichment.Source.Paths).Cast<Relation>().ToArray();
        var added = E(enrichment.Result.Paths).Cast<Relation>()
            .Where(path => !source.Any(original => ReferenceEquals(original, path))).ToArray();
        Assert.Equal(addedPaths, added.Length);
        var hooks = source.Where(hook => source.Any(backbone => !ReferenceEquals(backbone, hook) &&
            Points(frame, backbone).Skip(1).SkipLast(1).Contains(Points(frame, hook)[0]))).ToArray();
        Assert.Equal(2, hooks.Length);
        var ports = hooks.Select(hook => E(enrichment.Source.Ports).Cast<Relation>().Single(port =>
            port.Right is Relation pick && ReferenceEquals(pick.Left, hook) &&
            ReferenceEquals(pick.Right, E((Relation)hook.Right)[0]))).ToArray();
        var rooted = new Dictionary<Relation, List<Relation>>(ReferenceEqualityComparer.Instance);
        foreach (var port in ports) rooted.Add(port, []);
        foreach (var layer in E(enrichment.Layers).Cast<Relation>())
        {
            Assert.True(enrichment.TryReadLayer(layer, out _, out _, out _, out _, out var receipts));
            foreach (var receipt in E(receipts).Cast<Relation>())
            {
                var attachment = Assert.IsType<Relation>(receipt.Left);
                var port = Assert.IsType<Relation>(attachment.Left);
                Assert.Contains(ports, actual => ReferenceEquals(actual, port));
                var path = Assert.IsType<Relation>(((Relation)attachment.Right).Right);
                rooted[port].Add(path);
                var root = Points(frame, (Relation)((Relation)port.Right).Left)[0];
                Assert.All(Points(frame, path), point =>
                {
                    Assert.InRange(point.X - root.X, -3, 3);
                    Assert.InRange(point.Y - root.Y, -3, 3);
                });
            }
        }
        foreach (var (port, paths) in rooted)
        {
            Assert.Equal(addedPaths / 2, paths.Count);
            var centre = Points(frame, (Relation)((Relation)port.Right).Left)[0];
            var petals = paths.Where(path => E((Relation)path.Right).Length == 4 &&
                Points(frame, path)[0] == centre).ToArray();
            Assert.Equal(mode == "field" ? 0 : 4, petals.Length);
            var signatures = petals.Select(path => Points(frame, path).Take(4)
                .Select(point => (X: point.X - centre.X, Y: point.Y - centre.Y)).ToHashSet()).ToArray();
            foreach (var points in signatures)
            {
                Assert.Contains((0, 0), points);
                Assert.All(points, point => { Assert.InRange(point.X, -3, 3); Assert.InRange(point.Y, -3, 3); });
                var quarter = points.Select(point => (-point.Y, point.X)).ToHashSet();
                Assert.Contains(signatures, other => other.SetEquals(quarter));
            }
        }
        Assert.Equal(midribs, added.Count(path => !E(enrichment.Result.ClosedPaths).Any(closed => ReferenceEquals(closed, path))));
        var hint = Assert.IsType<Relation>(Assert.Single(E(variant.CurveHints)));
        Assert.True(OrnamentCurveSelection.TryRead(frame, enrichment.ResultSurface, hint, out var curves));
        Assert.Equal(mode == "field" ? 0 : 8, E(curves.Selected).Length);
        foreach (var petal in E(curves.Selected).Cast<Relation>())
        {
            Assert.Contains(0, curves.FixedLandmarks(petal));
            Assert.Contains(2, curves.FixedLandmarks(petal));
        }
        Assert.Equal(E(enrichment.Result.Paths).Length, E(curves.Selected).Length + E(curves.Complement).Length);
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
