using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeFrieze;

namespace Gojo.Experiments.QualitativeExecution.Tests;

public sealed class OrnamentCorpusRefreshTests
{
    [Theory]
    [InlineData("B03")]
    [InlineData("B04")]
    [InlineData("B06")]
    [InlineData("B06.B")]
    [InlineData("B16")]
    [InlineData("B19")]
    [InlineData("B22")]
    [InlineData("B23")]
    [InlineData("B24")]
    [InlineData("B28")]
    [InlineData("B29")]
    public void EnlargedInteriorOrnamentsRemainInsideTheirActualParentCompartment(string id)
    {
        var variant = Fine(id); var design = variant.Design;
        var source = E(design.Enrichment.Source.Paths);
        foreach (var (parent, added) in Attachments(design.Enrichment))
        {
            Assert.Contains(source, path => ReferenceEquals(path, parent));
            Assert.Contains(E(design.Enrichment.Source.ClosedPaths), path => ReferenceEquals(path, parent));
            var boundary = Points(design.Source.Frame, parent);
            foreach (var point in Points(design.Source.Frame, added))
                Assert.True(InsideConvex(boundary, point), $"{id}: {point} escaped its actual source compartment");
        }
    }

    [Fact]
    public void CloudBlossomsFillTheirMainLobesAndRemainSeparatedAcrossTheStaggeredCourses()
    {
        var variant = Fine("B30"); var design = variant.Design;
        var groups = Attachments(design.Enrichment).GroupBy(item => item.Parent, ReferenceEqualityComparer.Instance).ToArray();
        Assert.Equal(4, groups.Length);
        var bodies = new List<(int X, int Y)[]>();
        foreach (var group in groups)
        {
            var parent = Assert.IsType<Relation>(group.Key); var profile = Points(design.Source.Frame, parent);
            var middle = (profile.Min(point => point.X) + profile.Max(point => point.X)) / 2;
            var baseline = profile.Min(point => point.Y);
            var body = Assert.Single(group, item => E((Relation)item.Added.Right).Length == 16);
            var outline = Points(design.Source.Frame, body.Added); bodies.Add(outline);
            Assert.Equal(80, outline.Max(point => point.X) - outline.Min(point => point.X));
            Assert.Equal(56, outline.Max(point => point.Y) - outline.Min(point => point.Y));
            var cap = profile.Where(point => point.X >= middle - 48 && point.X <= middle + 48).ToArray();
            var chamber = new[] { (middle - 48, baseline + 4) }.Concat(cap)
                .Append((middle + 48, baseline + 4)).Append((middle - 48, baseline + 4)).ToArray();
            foreach (var (_, added) in group)
                foreach (var point in Points(design.Source.Frame, added))
                {
                    Assert.InRange(point.X, middle - 40, middle + 40);
                    Assert.InRange(point.Y, baseline + 4, baseline + 60);
                    Assert.True(InsideConvex(chamber, point), $"B30: {point} escaped the actual main-lobe chamber");
                }
        }
        for (var first = 0; first < bodies.Count; first++)
            for (var second = first + 1; second < bodies.Count; second++)
                Assert.True(DisjointBoxes(bodies[first], bodies[second]));
    }

    [Theory]
    [InlineData("B32")]
    [InlineData("B33")]
    [InlineData("B34")]
    public void BroadCourseBlossomsKeepEveryOpenOrCrossingSourceHistory(string id)
    {
        var variant = Fine(id); var enrichment = variant.Design.Enrichment;
        var original = E(enrichment.Source.Paths);
        foreach (var path in original)
            Assert.Contains(E(enrichment.Result.Paths), actual => ReferenceEquals(actual, path));
        Assert.Equal(E(enrichment.Source.Ports).Length,
            E(enrichment.Result.Ports).Count(port => E(enrichment.Source.Ports).Any(actual => ReferenceEquals(actual, port))));
        var body = Assert.Single(E(enrichment.Result.ClosedPaths).Cast<Relation>(), path =>
            !original.Any(actual => ReferenceEquals(path, actual)) && E((Relation)path.Right).Length == 16);
        var points = Points(variant.Design.Source.Frame, body);
        Assert.True(points.Max(point => point.X) - points.Min(point => point.X) >= 36);
        Assert.True(OrnamentCurveSelection.TryRead(variant.Design.Source.Frame, enrichment.ResultSurface,
            Assert.IsType<Relation>(Assert.Single(E(variant.CurveHints))), out var curves));
        Assert.Contains(E(curves.Selected), path => ReferenceEquals(body, path));
        // Display prominence supplies no new topological join at a crossing.
        foreach (var port in E(enrichment.Result.Ports).Cast<Relation>())
            if (port.Right is Relation pick && original.Any(path => ReferenceEquals(pick.Left, path)))
                Assert.Contains(E(enrichment.Source.Ports), actual => ReferenceEquals(actual, port));
    }

    private static BourgoinOrnamentRefinedLibrary.RefinedDesign Fine(string id)
    {
        var sources = new BourgoinPatternLibrary(8).Recipes;
        var index = sources.ToList().FindIndex(source => source.Id == id);
        return Assert.Single(BourgoinOrnamentRefinedLibrary.Create(sources[index], index, ["detail"]));
    }

    private static IEnumerable<(Relation Parent, Relation Added)> Attachments(OrnamentEnrichment enrichment)
    {
        foreach (var layer in E(enrichment.Layers).Cast<Relation>())
        {
            Assert.True(enrichment.TryReadLayer(layer, out _, out _, out _, out _, out var receipts));
            foreach (var receipt in E(receipts).Cast<Relation>())
            {
                var attachment = (Relation)receipt.Left;
                var parent = (Relation)((Relation)((Relation)attachment.Left).Right).Left;
                var added = (Relation)((Relation)attachment.Right).Right;
                yield return (parent, added);
            }
        }
    }

    private static bool InsideConvex((int X, int Y)[] polygon, (int X, int Y) point) =>
        Enumerable.Range(1, polygon.Length - 1).Select(index =>
        {
            var a = polygon[index - 1]; var b = polygon[index];
            return Math.Sign((long)(b.X - a.X) * (point.Y - a.Y) - (long)(b.Y - a.Y) * (point.X - a.X));
        }).Where(sign => sign != 0).Distinct().Count() <= 1;

    private static bool DisjointBoxes((int X, int Y)[] a, (int X, int Y)[] b) =>
        a.Max(point => point.X) < b.Min(point => point.X) || b.Max(point => point.X) < a.Min(point => point.X) ||
        a.Max(point => point.Y) < b.Min(point => point.Y) || b.Max(point => point.Y) < a.Min(point => point.Y);

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
