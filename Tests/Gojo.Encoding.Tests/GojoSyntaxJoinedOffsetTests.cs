using System.Net;
using System.Numerics;
using System.Text.Json;
using System.Text.RegularExpressions;
using Gojo.Core;
using Gojo.Encoding.Syntax;
using Gojo.Patterns;
using Gojo.Structures;

namespace Gojo.Encoding.Tests;

public sealed class GojoSyntaxJoinedOffsetTests
{
    [Theory]
    [InlineData(0)] [InlineData(1)] [InlineData(2)]
    public void ThreeOffsetsBecomeOnePathWithSharedConstructedJoints(int restores)
    {
        var f = new Fixture(restores: restores);
        Assert.True(SegmentOffset.TryConstructPath(f.Request, out var read));
        CheckPath(read, 2, true);
        var path = F(Result(read)); var proof = F(F(read)[4]);
        var captured = F(F(read)[1]); var inputs = F(captured[0]); var maps = F(proof[4]);
        for (var i = 0; i < 3; i++)
        {
            Assert.Same(F(F(inputs[i])[0])[0], R(maps[i]).Left);
            Assert.Same(path[i], R(maps[i]).Right);
            Assert.Empty(F(F(F(proof[3])[i])[3]));
        }
        CheckPath(Restore(read, 2), 2, true);
        Assert.All(f.Request.Carriers(), c => { Assert.Equal(0UL, c.Value); Assert.Equal(0, c.Tension); });
    }

    [Theory]
    [InlineData(3, 0)] [InlineData(3, 2)] [InlineData(4, 0)] [InlineData(4, 2)]
    public void ValidLocalCornersDoNotAuthorizeACollapsedOrReversedMiddleSpan(int distance, int restores)
    {
        var f = new Fixture(distance, restores);
        Assert.False(SegmentOffset.TryConstructPath(f.Request, out var read));
        CheckPath(read, distance, false);
        var proof = F(F(read)[4]); var admissions = F(proof[3]);
        Assert.Same(admissions[1], Assert.Single(F(F(read)[3])));
        var middle = F(admissions[1]); Assert.Same(F(Result(read))[1], middle[2]);
        Assert.Equal(2, F(middle[3]).Length);
        // Both exact corner reads succeeded. A later order check retains them.
        Assert.All(F(proof[2]), c => Assert.Empty(F(F(c)[3])));
        if (distance == 3)
        {
            var candidate = Candidate(admissions[1]);
            Assert.NotSame(candidate.Left, candidate.Right); // Equal coordinates are not a weld.
        }
        CheckPath(Restore(read, 2), distance, false);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void ForwardOrderIsRelativeToEachParameterFrame(bool crossed)
    {
        var f = new Fixture(crossed ? 4 : 2); var input = F(f.Request); var supports = F(input[1]);
        for (var i = 0; i < 3; i++)
        {
            var axis = Loc(R(R(supports[i]).Left).Left).Carrier;
            supports[i] = new Relation(Relation.Segment(axis, 100, -20), Relation.Segment(axis, -20, 100));
        }
        Assert.Equal(!crossed, SegmentOffset.TryConstructPath(Group(input[0], Group(supports)), out var read));
        var admissions = F(F(F(read)[4])[3]);
        var traversal = R(F(F(admissions[1])[4])[1]);
        Number(R(traversal.Left).Left, crossed ? 20 : 60);
        Number(R(traversal.Left).Right, crossed ? 60 : 20);
    }

    [Fact]
    public void OpenEndPositionsAlsoHaveToBeAdmittedByTheirSupports()
    {
        var f = new Fixture(); var input = F(f.Request); var supports = F(input[1]);
        var axis = Loc(R(R(supports[0]).Left).Left).Carrier;
        supports[0] = new Relation(Relation.Segment(axis, 0, 10), Relation.Segment(axis, 1, 10));
        Assert.False(SegmentOffset.TryConstructPath(Group(input[0], Group(supports)), out var read));
        var path = F(Result(read)); Assert.IsType<Placeholder>(path[0]); Assert.IsType<Relation>(path[1]); Assert.IsType<Relation>(path[2]);
        var admission = F(F(F(F(read)[4])[3])[0]); var traversal = R(F(admission[4])[1]);
        Number(R(traversal.Left).Left, 0); Number(R(traversal.Left).Right, 15, 2);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void ALocalConstructionSurvivesAShearedOrReflectedChart(bool reflect)
    {
        var f = new Fixture(); var input = F(f.Request); var ps = F(input[0]);
        var oldMetric = F(F(ps[0])[0])[1]; var mf = F(oldMetric); var units = F(mf[0]);
        Assert.True(ProjectionResidue.TryRead(R(F(mf[1])[0]).Right, out var squared, out _, out _));
        Relation Pair(int a, int b, long value) => new(new Relation(units[a], units[b]), new CarrierLocation(squared, value));
        var metric = reflect ? oldMetric : Group(mf[0], Group(Pair(0, 0, 1), Pair(0, 1, -1), Pair(1, 1, 2)));
        long[][] points = [[0,0], [8,0], [8,6], [0,6]], guides = [[0,1], [7,0], [8,5]];
        Relation Map(long[] p) => f.Point(reflect ? -p[0] : p[0] + p[1], p[1]);
        var sites = points.Select(Map).ToArray();
        for (var i = 0; i < 3; i++)
        {
            var p = F(ps[i]); var o = F(p[0]); var edge = new Relation(sites[i], sites[i + 1]);
            ps[i] = Group(Group(edge, metric, new Relation(Relation.OrthogonalOutlet(edge), R(o[2]).Right),
                new Relation(sites[i], Map(guides[i]))), p[1]);
        }
        Assert.True(SegmentOffset.TryConstructPath(R(Restore(Group(Group(ps), input[1]), 2)), out var read));
        var path = F(Result(read));
        long[][] expected = [[0,2], [6,2], [6,4], [0,4]];
        for (var i = 0; i < 3; i++)
        {
            Vector(R(path[i]).Left, reflect ? -expected[i][0] : expected[i][0] + expected[i][1], expected[i][1]);
            Vector(R(path[i]).Right, reflect ? -expected[i+1][0] : expected[i+1][0] + expected[i+1][1], expected[i+1][1]);
        }
        Assert.Same(R(path[0]).Right, R(path[1]).Left); Assert.Same(R(path[1]).Right, R(path[2]).Left);
    }

    [Theory]
    [InlineData(0)] [InlineData(9007199254740993)]
    public void PathsCanContainMoreThanThreeEdgesWithoutLosingNativePrecision(long shift)
    {
        var f = new Fixture(); var ps = F(F(f.Request)[0]); var p = F(ps[0]); var o = F(p[0]);
        long[][] raw = [[0,0], [8,0], [8,6], [0,6], [0,12]];
        var points = raw.Select(v => f.Point(v[0] + shift, v[1])).ToArray();
        long[][] guides = [[0,1], [7,0], [8,5], [-1,6]];
        var preparations = new IElement[4]; var supports = new IElement[4];
        for (var i = 0; i < 4; i++)
        {
            var edge = new Relation(points[i], points[i+1]);
            preparations[i] = Group(Group(edge, o[1], new Relation(Relation.OrthogonalOutlet(edge), R(o[2]).Right),
                new Relation(points[i], f.Point(guides[i][0] + shift, guides[i][1]))), Relation.Segment(new Carrier(), 0, 1));
            var axis = new Carrier(); supports[i] = new Relation(Relation.Segment(axis, 0, 1), Relation.Segment(axis, -1, 2));
        }
        Assert.True(SegmentOffset.TryConstructPath(Group(Group(preparations), Group(supports)), out var read));
        var path = F(Result(read)); Assert.Equal(4, path.Length);
        long[][] expected = [[0,2], [6,2], [6,4], [-2,4], [-2,12]];
        for (var i = 0; i < 4; i++)
        {
            Vector(R(path[i]).Left, shift + expected[i][0], expected[i][1]);
            Vector(R(path[i]).Right, shift + expected[i+1][0], expected[i+1][1]);
            if (i > 0) Assert.Same(R(path[i-1]).Right, R(path[i]).Left);
        }
    }

    [Theory]
    [InlineData("lookalike-corner")] [InlineData("different-metric")]
    [InlineData("different-calibration")] [InlineData("opposite-side")]
    [InlineData("parallel")] [InlineData("open-end")]
    public void AFailedCornerPreservesTheOtherJoinAndOneSharedOpenConnection(string fault)
    {
        var f = new Fixture(); var inputs = F(F(f.Request)[0]); var p = F(inputs[2]); var o = F(p[0]);
        var source = R(o[0]); var start = source.Left; IElement end = source.Right;
        switch (fault)
        {
            case "lookalike-corner": start = f.Point(8, 6); break;
            case "different-metric": o[1] = Group(F(o[1])); break;
            case "different-calibration": o[2] = new Relation(R(o[2]).Left,
                new Relation(R(R(o[2]).Right).Left, Relation.Segment(f.X, 0, 2))); break;
            case "opposite-side": o[3] = new Relation(start, f.Point(8, 7)); break;
            case "parallel": end = f.Point(8, 12); break;
            case "open-end": end = new Placeholder(); break;
        }
        if (fault is "lookalike-corner" or "parallel" or "open-end")
        {
            o[0] = new Relation(start, end); o[2] = new Relation(Relation.OrthogonalOutlet(o[0]), R(o[2]).Right);
            o[3] = new Relation(start, f.Point(7, 6));
            if (fault == "lookalike-corner") o[3] = new Relation(start, f.Point(8, 5));
        }
        inputs[2] = Group(Group(o), p[1]);
        Assert.False(SegmentOffset.TryConstructPath(R(Restore(Group(Group(inputs), F(f.Request)[1]), 2)), out var read));
        var path = F(Result(read)); Assert.IsType<Relation>(path[0]); Assert.IsType<Placeholder>(path[1]); Assert.IsType<Placeholder>(path[2]);
        var proof = F(F(read)[4]); var admissions = F(proof[3]); var corners = F(proof[2]);
        var open = Assert.IsType<Placeholder>(Result(corners[1]));
        Assert.Same(open, Candidate(admissions[1]).Right); Assert.Same(open, Candidate(admissions[2]).Left);
        Vector(R(path[0]).Right, 6, 2); Assert.Equal(2, F(F(read)[3]).Length);
    }

    [Fact]
    public void AnIrrationalOffsetKeepsItsEquationAndCanStillBeObservedAtResolution()
    {
        var f = new Fixture(); var inputs = F(F(f.Request)[0]); var p = F(inputs[2]); var o = F(p[0]);
        var edge = new Relation(R(o[0]).Left, f.Point(0, 7));
        o[0] = edge; o[2] = new Relation(Relation.OrthogonalOutlet(edge), R(o[2]).Right);
        inputs[2] = Group(Group(o), p[1]);
        Assert.False(SegmentOffset.TryConstructPath(Group(Group(inputs), F(f.Request)[1]), out var read));
        read = R(Restore(read, 2));
        var proof = F(F(read)[4]); var preparation = F(proof[0])[2];
        var prepared = F(Result(preparation)); var equation = F(prepared[1]);
        Number(R(equation[2]).Left, 65); Number(R(equation[2]).Right, 4);
        Assert.Same(R(equation[1]).Left, R(equation[1]).Right);
        var whole = F(F(preparation)[1])[1]; var axis = Loc(R(whole).Left).Carrier;
        var fine = new Carrier(UnitShape.SubdivisionOf(axis.UnitShape, 100));
        Assert.True(SegmentOffset.TryObserveAtResolution(Group(preparation,
            new Relation(Relation.Segment(fine, 0, 100), whole)), out var bounded));
        var support = R(F(F(Result(bounded))[0])[1]); Number(support.Left, 24); Number(support.Right, 25);
        var admissions = F(proof[3]);
        Assert.Same(Candidate(admissions[1]).Right, Candidate(admissions[2]).Left);
        Assert.Same(R(prepared[3]).Right, Candidate(admissions[2]).Right);
        Assert.IsType<Relation>(F(Result(read))[0]);
    }

    [Fact]
    public void AChangedCalibrationRequiresANewReadAndDoesNotRewriteTheOldPath()
    {
        var f = new Fixture(); Assert.True(SegmentOffset.TryConstructPath(f.Request, out var before));
        var calibration = R(R(F(F(F(F(f.Request)[0])[0])[0])[2]).Right);
        Loc(R(calibration.Right).Right).Tick.Set(4);
        Assert.False(SegmentOffset.TryConstructPath(f.Request, out var after));
        CheckPath(before, 2, true); CheckPath(after, 4, false);
        Assert.True(SegmentOffset.TryConstructPath(R(Restore(F(before)[1], 2)), out var replay)); CheckPath(replay, 2, true);
    }

    [Theory]
    [InlineData("empty")] [InlineData("one")] [InlineData("missing-support")]
    [InlineData("foreign-support")] [InlineData("collapsed-frame")]
    [InlineData("wrong-scale")] [InlineData("closed")]
    public void MalformedOrOutOfContractPathRequestsStayUnresolved(string fault)
    {
        var f = new Fixture(); var input = F(f.Request); var ps = F(input[0]); var ss = F(input[1]);
        switch (fault)
        {
            case "empty": ps = []; ss = []; break;
            case "one": ps = [ps[0]]; ss = [ss[0]]; break;
            case "missing-support": ss = [ss[0], ss[1]]; break;
            case "foreign-support": ss[0] = new Relation(R(ss[0]).Left, Relation.Segment(new Carrier(), 0, 1)); break;
            case "collapsed-frame": ss[0] = new Relation(Relation.Segment(Loc(R(R(ss[0]).Left).Left).Carrier, 0, 0), R(ss[0]).Right); break;
            case "wrong-scale": ps[0] = Group(F(ps[0])[0], Relation.Segment(new Carrier(), 0, 2)); break;
            case "closed":
                var p = F(ps[2]); var o = F(p[0]); var edge = new Relation(R(o[0]).Left, R(F(F(ps[0])[0])[0]).Left);
                o[0] = edge; o[2] = new Relation(Relation.OrthogonalOutlet(edge), R(o[2]).Right); ps[2] = Group(Group(o), p[1]); break;
        }
        var request = Group(Group(ps), Group(ss));
        Assert.False(SegmentOffset.TryConstructPath(request, out var read));
        Assert.IsType<Placeholder>(Result(read)); Assert.Same(request, Assert.Single(F(F(read)[3])));
    }

    [Fact]
    public void TheWalkthroughUsesTheAuthoredSyntaxAndCheckedKernelTraces()
    {
        var html = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Gallery", "joined-offsets.html"));
        var syntax = Regex.Match(html, "<pre id=\"syntax\">(.*?)</pre>", RegexOptions.Singleline);
        Assert.True(syntax.Success);
        Assert.Equal(Fixture.Text.Replace("\r\n", "\n").Trim(), WebUtility.HtmlDecode(syntax.Groups[1].Value).Replace("\r\n", "\n").Trim());
        var match = Regex.Match(html, "<script id=\"trace\" type=\"application/json\">(.*?)</script>", RegexOptions.Singleline);
        using var trace = JsonDocument.Parse(match.Groups[1].Value);
        foreach (var row in trace.RootElement.GetProperty("reads").EnumerateArray())
        {
            var distance = row.GetProperty("distance").GetInt32(); var f = new Fixture(distance);
            Assert.Equal(row.GetProperty("complete").GetBoolean(), SegmentOffset.TryConstructPath(f.Request, out var read));
            var proof = F(F(read)[4]); var offsets = F(proof[1]); var corners = F(proof[2]); var admissions = F(proof[3]);
            for (var i = 0; i < 3; i++)
            {
                var source = R(F(F(F(F(f.Request)[0])[i])[0])[0]);
                MatchPoint(source.Left, trace.RootElement.GetProperty("source")[i]);
                MatchPoint(source.Right, trace.RootElement.GetProperty("source")[i+1]);
                MatchEdge(Result(offsets[i]), row.GetProperty("offsets")[i]);
                MatchEdge(Candidate(admissions[i]), row.GetProperty("candidates")[i]);
                Assert.Equal(row.GetProperty("admitted")[i].GetBoolean(), Result(admissions[i]) is Relation);
            }
            for (var i = 0; i < 2; i++) MatchPoint(R(F(Result(corners[i]))[0]).Right, row.GetProperty("joints")[i]);
            var parameters = R(R(F(F(admissions[1])[4])[1]).Left);
            var expected = row.GetProperty("middle");
            Number(parameters.Left, expected[0][0].GetInt64(), expected[0][1].GetInt64());
            Number(parameters.Right, expected[1][0].GetInt64(), expected[1][1].GetInt64());
        }
    }

    private static void CheckPath(IElement read, int distance, bool complete)
    {
        var path = F(Result(read)); Assert.Equal(3, path.Length);
        Vector(R(path[0]).Left, 0, distance); Vector(R(path[0]).Right, 8 - distance, distance);
        Vector(R(path[2]).Left, 8 - distance, 6 - distance); Vector(R(path[2]).Right, 0, 6 - distance);
        var admissions = F(F(F(read)[4])[3]); var middle = Candidate(admissions[1]);
        Assert.Same(R(path[0]).Right, middle.Left); Assert.Same(middle.Right, R(path[2]).Left);
        if (complete) Assert.Same(path[1], middle); else Assert.IsType<Placeholder>(path[1]);
        var traversal = R(F(F(admissions[1])[4])[1]);
        Number(R(traversal.Left).Left, distance, 6); Number(R(traversal.Left).Right, 6 - distance, 6);
    }
    private sealed class Fixture
    {
        public static string Text => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "Syntax", "joined-offset-path.gojo"));
        public Relation Request { get; }
        public Carrier X { get; } public Carrier Y { get; }
        private readonly Carrier holder;
        public Fixture(int distance = 2, int restores = 0)
        {
            var roots = GojoSyntax.Parse(Text).Roots.ToArray();
            for (var i = 0; i < restores; i++) roots = GojoNumericFormat.Decode(GojoNumericFormat.Encode(roots));
            Request = R(StructuralPattern.Refine(roots[0], R(roots[distance - 1])));
            var basis = F(F(F(F(F(Request)[0])[0])[0])[1])[0]; var units = F(basis);
            holder = (Carrier)R(basis).Right; X = Loc(R(units[0]).Left).Carrier; Y = Loc(R(units[1]).Left).Carrier;
        }
        public Relation Point(long x, long y) => Relation.OrderedGroup(holder, new CarrierLocation(X, x), new CarrierLocation(Y, y));
    }
    private static Relation Candidate(IElement admission) => R(F(F(admission)[4])[0]);
    private static void MatchPoint(IElement p, JsonElement e) => Vector(p, e[0].GetInt64(), e[1].GetInt64());
    private static void MatchEdge(IElement p, JsonElement e) { MatchPoint(R(p).Left, e[0]); MatchPoint(R(p).Right, e[1]); }
    private static IElement Restore(IElement e, int count)
    { for (var i = 0; i < count; i++) e = GojoNumericFormat.Decode(GojoNumericFormat.Encode(e))[0]; return e; }
    private static Relation R(IElement e) => Assert.IsType<Relation>(e);
    private static IElement[] F(IElement e) { Assert.True(R(e).TryReadOrderedGroup(out var f)); return f; }
    private static IElement Result(IElement e) => F(e)[2];
    private static Relation Group(params IElement[] f) => Relation.OrderedGroup(new Carrier(), f);
    private static CarrierLocation Loc(IElement e) => Assert.IsType<CarrierLocation>(R(e).Left);
    private static void Vector(IElement e, long x, long y) { Number(F(e)[0], x); Number(F(e)[1], y); }
    private static void Number(IElement e, long n, long d = 1)
    {
        if (e is Relation { Left: CarrierLocation p, Right: Carrier }) e = p;
        Assert.True(ProjectionResidue.TryRead(e, out _, out var actual, out var divisor));
        Assert.Equal(new BigInteger(n) * divisor, new BigInteger(actual) * d);
    }
}
