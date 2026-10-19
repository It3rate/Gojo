using Gojo.Experiments.QualitativeFrieze;

namespace Gojo.Experiments.QualitativeExecution.Tests;

public sealed class OrnamentCurvePresentationTests
{
    [Fact]
    public void QuadraticFilletsStayInTheirGuideTrianglesAndKeepEndpointDirections()
    {
        (double X, double Y)[] points = [(0, 0), (4, 0), (4, 3), (8, 3)];
        var segments = OrnamentCurvePresentation.Build(points, false, .35);
        Assert.Equal(new(0, 0), segments[0].Start); Assert.Equal(new(8, 3), segments[^1].End);
        Assert.Equal(2, segments.Count(segment => segment.IsQuadratic));
        foreach (var segment in segments.Where(segment => segment.IsQuadratic))
            for (var step = 0; step <= 20; step++)
                Assert.True(InTriangle(segment.At(step / 20d), segment.Start, segment.Control!.Value, segment.End));
        for (var index = 0; index < segments.Count - 1; index++)
        {
            var first = segments[index]; var second = segments[index + 1]; Assert.Equal(first.End, second.Start);
            var a = first.Control is { } q ? Difference(first.End, q) : Difference(first.End, first.Start);
            var b = second.Control is { } r ? Difference(r, second.Start) : Difference(second.End, second.Start);
            Assert.Equal(0, Cross(a, b), 10); Assert.True(a.X * b.X + a.Y * b.Y > 0);
        }
        Assert.Equal(0, segments[0].End.Y); Assert.Equal(3, segments[^1].Start.Y);
        Assert.Contains(" Q ", OrnamentCurvePresentation.PathData(points, false));
    }

    [Fact]
    public void FixedBranchAndTipLandmarksClosedStartAndSeparateGapEndsRemainExact()
    {
        (double X, double Y)[] points = [(0, 0), (4, 0), (4, 4), (0, 4), (0, 0)];
        var segments = OrnamentCurvePresentation.Build(points, true, fixedLandmarks: new HashSet<int> { 2 });
        Assert.Equal(new(0, 0), segments[0].Start); Assert.Equal(new(0, 0), segments[^1].End);
        Assert.Contains(segments, segment => segment.End == new OrnamentCurvePresentation.Point(4, 4));
        Assert.DoesNotContain(segments, segment => segment.Control == new OrnamentCurvePresentation.Point(4, 4));
        Assert.EndsWith(" Z", OrnamentCurvePresentation.PathData(points, true, fixedLandmarks: new HashSet<int> { 2 }));
        var before = OrnamentCurvePresentation.Build([(-4, -3), (-2, -3), (0, 0)], false);
        var after = OrnamentCurvePresentation.Build([(2, 3), (4, 3), (6, 0)], false);
        Assert.Equal(new(0, 0), before[^1].End); Assert.Equal(new(2, 3), after[0].Start);
        Assert.NotEqual(before[^1].End, after[0].Start);
    }

    [Fact]
    public void StraightRunsReversalCuspsAndZeroRoundingStayOnTheirChordGuides()
    {
        (double X, double Y)[] cusp = [(0, 0), (2, 0), (0, 0), (0, 2)];
        var segments = OrnamentCurvePresentation.Build(cusp, false);
        Assert.Contains(segments, segment => !segment.IsQuadratic && segment.End == new OrnamentCurvePresentation.Point(2, 0));
        Assert.All(OrnamentCurvePresentation.Build([(0, 0), (2, 0), (4, 0)], false), segment => Assert.False(segment.IsQuadratic));
        Assert.All(OrnamentCurvePresentation.Build(cusp, false, 0), segment => Assert.False(segment.IsQuadratic));
        Assert.Equal("", OrnamentCurvePresentation.PathData([], false));
        Assert.Equal("M 1 2", OrnamentCurvePresentation.PathData([(1, 2)], false));
    }

    [Fact]
    public void InvalidDisplayInputsCannotSilentlyProduceUnboundedCurves()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => OrnamentCurvePresentation.Build([(0, 0), (1, 1)], false, .5));
        Assert.Throws<ArgumentOutOfRangeException>(() => OrnamentCurvePresentation.Build([(0, 0)], false, double.NaN));
        Assert.Throws<ArgumentException>(() => OrnamentCurvePresentation.Build([(double.PositiveInfinity, 0)], false));
        Assert.Throws<ArgumentOutOfRangeException>(() => OrnamentCurvePresentation.Build([(0, 0), (1, 1)], false,
            fixedLandmarks: new HashSet<int> { 2 }));
    }
    private static OrnamentCurvePresentation.Point Difference(OrnamentCurvePresentation.Point a, OrnamentCurvePresentation.Point b) => new(a.X - b.X, a.Y - b.Y);
    private static double Cross(OrnamentCurvePresentation.Point a, OrnamentCurvePresentation.Point b) => a.X * b.Y - a.Y * b.X;
    private static bool InTriangle(OrnamentCurvePresentation.Point point, OrnamentCurvePresentation.Point a,
        OrnamentCurvePresentation.Point b, OrnamentCurvePresentation.Point c)
    {
        var one = Cross(Difference(b, a), Difference(point, a));
        var two = Cross(Difference(c, b), Difference(point, b));
        var three = Cross(Difference(a, c), Difference(point, c));
        return one >= -1e-10 && two >= -1e-10 && three >= -1e-10 || one <= 1e-10 && two <= 1e-10 && three <= 1e-10;
    }
}
