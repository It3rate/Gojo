using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeFrieze;

namespace Gojo.Experiments.QualitativeExecution.Tests;

public sealed class BourgoinQuietSerifTests
{
    [Theory]
    [InlineData("B13", "field", 0)]
    [InlineData("B13", "motif", 4)]
    [InlineData("B13", "detail", 8)]
    [InlineData("B14", "field", 0)]
    [InlineData("B14", "motif", 4)]
    [InlineData("B14", "detail", 8)]
    public void QuietSerifReadingsLeaveCapsAndInterlocksUncovered(string id, string mode, int closedAdditions)
    {
        var source = new BourgoinPatternLibrary(8).Recipes.Single(recipe => recipe.Id == id);
        var variant = Assert.Single(BourgoinOrnamentCompartmentLibrary.Create(source, 0, [mode]));
        var enrichment = variant.Design.Enrichment;
        Assert.True(enrichment.IsComplete);
        var retained = E(enrichment.Source.Paths).Cast<Relation>().ToArray();
        var additions = E(enrichment.Result.Paths).Cast<Relation>()
            .Where(path => !retained.Any(actual => ReferenceEquals(actual, path))).ToArray();
        var closed = E(enrichment.Result.ClosedPaths);
        Assert.Equal(closedAdditions, additions.Count(path => closed.Any(actual => ReferenceEquals(actual, path))));

        // The actual native word sites stay inside the long, narrow crossbar.
        // A cap fan, interlock flower or detached accent cannot pass this read.
        foreach (var path in additions)
        {
            var points = Points(source.Frame, path);
            Assert.Contains(retained, original =>
            {
                var boundary = Points(source.Frame, original);
                var minX = boundary.Min(point => point.X); var maxX = boundary.Max(point => point.X);
                var minY = boundary.Min(point => point.Y); var maxY = boundary.Max(point => point.Y);
                var cx = (minX + maxX) / 2; var cy = (minY + maxY) / 2;
                return maxX - minX > maxY - minY
                    ? points.All(point => Math.Abs(point.Y - cy) <= 2 && point.X >= minX + 9 && point.X <= maxX - 9)
                    : points.All(point => Math.Abs(point.X - cx) <= 2 && point.Y >= minY + 9 && point.Y <= maxY - 9);
            });
        }
        if (mode == "detail")
        {
            var leaves = additions.Where(path => closed.Any(actual => ReferenceEquals(actual, path)))
                .Select(path => Points(source.Frame, path)).GroupBy(points => points[0]).ToArray();
            Assert.Equal(4, leaves.Length);
            foreach (var pair in leaves)
            {
                var actual = pair.ToArray(); Assert.Equal(2, actual.Length);
                var first = actual[0][3]; var second = actual[1][3];
                Assert.Equal(2 * pair.Key.X, first.X + second.X);
                Assert.Equal(2 * pair.Key.Y, first.Y + second.Y);
            }
        }
    }

    private static (int X, int Y)[] Points(QualitativeMotionFrame frame, Relation path)
    {
        var current = Word(frame, (Relation)path.Left);
        var points = new List<(int X, int Y)> { current };
        foreach (var word in E((Relation)path.Right).Cast<Relation>())
        {
            var delta = Word(frame, word); current = (current.X + delta.X, current.Y + delta.Y);
            points.Add(current);
        }
        return points.ToArray();
    }

    private static (int X, int Y) Word(QualitativeMotionFrame frame, Relation word)
    {
        var x = 0; var y = 0;
        foreach (var move in E(word).Cast<Relation>())
        {
            Assert.True(frame.Direction(move, out var axis, out var forward, out _));
            if (ReferenceEquals(axis, frame.Horizontal)) x += forward ? 1 : -1; else y += forward ? 1 : -1;
        }
        return (x, y);
    }

    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
}
