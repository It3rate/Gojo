using System.Numerics;
using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using Gojo.Core;
using Gojo.Encoding.Syntax;
using Gojo.Structures;

namespace Gojo.Encoding.Tests;

public sealed class GojoSyntaxMetricConstructionTests
{
    [Theory]
    [InlineData(0)] [InlineData(1)] [InlineData(2)]
    public void IntrinsicQuarterLandmarksFeedTheExistingRotationAndKeepNativeIdentity(int restores)
    {
        var roots = Fixture("constructed-circle-rotation", restores);
        var request = R(roots[0]);
        Assert.True(ContourConstruction.TryCalibrateQuarters(request, out var read));
        var surface = F(Result(read)); var partition = F(surface[1]); var circle = F(partition[0]);
        var bindings = F(surface[3]); var cycle = F(surface[4]);
        Assert.Same(R(circle[2]).Left, R(bindings[0]).Left);
        long[][] rims = [[13,24], [6,23], [7,16], [14,17]];
        var nativeReads = F(F(read)[4]); var locations = F(nativeReads[0]); var correspondences = F(nativeReads[1]);
        for (var i = 0; i < 4; i++)
        {
            Vector(R(R(bindings[i]).Right).Right, rims[i][0], rims[i][1]);
            Assert.Same(R(bindings[i]).Left, R(cycle[i]).Left);
            Assert.Same(R(bindings[(i + 1) % 4]).Left, R(cycle[i]).Right);
            Assert.Same(R(bindings[i]).Left, F(Result(locations[i]))[0]);
            Assert.Same(F(Result(locations[i]))[1], R(correspondences[i]).Left);
            Assert.Same(R(bindings[i]).Right, R(correspondences[i]).Right);
            // Independent integer oracle for all four metric radii.
            var dx = rims[i][0] - 10; var dy = rims[i][1] - 20;
            Assert.Equal(25, dx * dx + dy * dy);
        }
        Assert.True(ContourRotation.TryTraverse(Group(Result(read), R(bindings[0]).Left,
            Relation.Segment(new Carrier(), 0, 1)), out var walk));
        Assert.True(ContourRotation.TryTurn(walk, out var turn)); Number(F(turn)[4], 25);
        Assert.True(ContourRotation.TryApply(Group(walk, roots[1]), out var applied));
        var points = F(Result(applied)); Vector(points[0], 10, 22); Vector(points[1], 9, 22);
        Assert.Same(points[0], points[2]); Vector(F(roots[1])[0], 12, 20);
        // Transport the actual constructed result and traversal, not just inputs.
        var restored = F(Restore(Group(read, walk, applied), 2));
        Assert.True(ContourRotation.TryTurn(R(restored[1]), out _));
        Assert.Same(F(Result(restored[2]))[0], F(Result(restored[2]))[2]);
        Assert.All(request.Carriers(), c => { Assert.Equal(0UL, c.Value); Assert.Equal(0, c.Tension); });
    }

    [Theory]
    [InlineData(4)] [InlineData(8)] [InlineData(360)] [InlineData(3600)]
    public void AQuarterUsesTheExistingWholePartitionRatherThanAuthoredRimCoordinates(int parts)
    {
        var request = F(Fixture("constructed-circle-rotation")[0]);
        var partition = F(request[0]); var circle = F(partition[0]);
        var contour = Location(R(circle[2]).Left).Carrier;
        var fine = new Carrier(UnitShape.SubdivisionOf(contour.UnitShape, parts));
        request[0] = Group(partition[0], new Relation(Relation.Segment(fine, 0, parts), circle[2]));
        Assert.True(ContourConstruction.TryCalibrateQuarters(Group(request), out var read));
        var bindings = F(F(Result(read))[3]);
        Number(R(bindings[1]).Left, parts / 4); Number(R(bindings[3]).Left, 3 * parts / 4);
        Vector(R(R(bindings[1]).Right).Right, 6, 23);
    }

    [Theory]
    [InlineData(-1, 10, 18)] [InlineData(0, 12, 20)] [InlineData(1, 10, 22)]
    [InlineData(2, 8, 20)] [InlineData(4, 12, 20)] [InlineData(9, 10, 22)]
    public void ConstructedTurnsKeepSignedTraversalAndFullCyclesSeparate(long steps, long x, long y)
    {
        var roots = Fixture("constructed-circle-rotation", 1);
        Assert.True(ContourConstruction.TryCalibrateQuarters(R(roots[0]), out var read));
        var marker = R(F(F(Result(read))[3])[0]).Left;
        Assert.True(ContourRotation.TryTraverse(Group(Result(read), marker, Relation.Segment(new Carrier(), 0, steps)), out var walk));
        Assert.True(ContourRotation.TryApply(Group(walk, roots[1]), out var applied));
        Vector(F(Result(applied))[0], x, y);
    }

    [Theory]
    [InlineData("open-centre")] [InlineData("open-side")] [InlineData("tangent-side")]
    [InlineData("collapsed-ray")] [InlineData("irrational-ray")] [InlineData("negative-length")]
    [InlineData("unrelated-reach")] [InlineData("unrelated-origin")] [InlineData("unrelated-length-unit")]
    [InlineData("ten-parts")] [InlineData("indefinite-metric")]
    public void CircleCalibrationKeepsMissingOrUnsupportedPremisesUnresolved(string fault)
    {
        var f = F(Fixture("constructed-circle-rotation")[0]); var chart = new Chart(f[1]); var centre = R(f[2]).Right;
        switch (fault)
        {
            case "open-centre": f[2] = new Relation(R(f[2]).Left, new Placeholder()); break;
            case "open-side": f[5] = new Relation(centre, new Placeholder()); break;
            case "tangent-side": f[5] = f[4]; break;
            case "collapsed-ray": f[4] = new Relation(centre, centre); break;
            case "irrational-ray": f[4] = new Relation(centre, chart.Point(11, 21)); break;
            case "negative-length": f[3] = new Relation(R(f[3]).Left, Relation.Segment(chart.X, 0, -5)); break;
            case "unrelated-reach": f[3] = new Relation(Relation.Segment(new Carrier(), 0, 1), R(f[3]).Right); break;
            case "unrelated-origin": f[2] = new Relation(Relation.Point(Location(R(f[2]).Left).Carrier, 0), centre); break;
            case "unrelated-length-unit": f[3] = new Relation(R(f[3]).Left, Relation.Segment(new Carrier(), 0, 5)); break;
            case "ten-parts":
                var p = F(f[0]); var circle = F(p[0]); var fine = new Carrier(UnitShape.SubdivisionOf(Location(R(circle[2]).Left).Carrier.UnitShape, 10));
                f[0] = Group(p[0], new Relation(Relation.Segment(fine, 0, 10), circle[2])); break;
            case "indefinite-metric": f[1] = chart.Metric(1, 2, 1); break;
        }
        Assert.False(ContourConstruction.TryCalibrateQuarters(R(Restore(Group(f), 2)), out var read));
        Assert.IsType<Placeholder>(Result(read)); Assert.NotEmpty(F(F(read)[3]));
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void QuarterConstructionWorksInAShearedChartAndWithEitherRelativeSide(bool opposite)
    {
        var roots = Fixture("constructed-circle-rotation"); var f = F(roots[0]); var chart = new Chart(f[1]);
        // Re-express x'=x+y, y'=y. Pullback metric has pairings (1,-1,2).
        f[1] = chart.Metric(1, -1, 2);
        var centre = chart.Point(30, 20);
        f[2] = new Relation(R(f[2]).Left, centre);
        f[4] = new Relation(centre, chart.Point(44, 28));
        f[5] = new Relation(centre, chart.Point(opposite ? 29 : 31, opposite ? 19 : 21));
        Assert.True(ContourConstruction.TryCalibrateQuarters(Group(f), out var read));
        var binding = F(F(Result(read))[3]);
        Vector(R(R(binding[0]).Right).Right, 37, 24);
        Vector(R(R(binding[1]).Right).Right, opposite ? 31 : 29, opposite ? 17 : 23);
        Assert.True(ContourRotation.TryTraverse(Group(Result(read), R(binding[0]).Left, Relation.Segment(new Carrier(), 0, 1)), out var walk));
        Assert.True(ContourRotation.TryApply(Group(walk, Group(chart.Point(32, 20))), out var applied));
        Vector(F(Result(applied))[0], opposite ? 28 : 32, opposite ? 18 : 22);
    }

    [Fact]
    public void NativeTraversalRejectsLookalikeSeamsAndScrambledPartitionOrder()
    {
        Assert.True(ContourConstruction.TryCalibrateQuarters(R(Fixture("constructed-circle-rotation")[0]), out var read));
        var surface = F(Result(read)); var bindings = F(surface[3]);
        Relation Request(IElement[] bs)
        {
            var edges = Enumerable.Range(0, bs.Length).Select(i => (IElement)new Relation(R(bs[i]).Left, R(bs[(i + 1) % bs.Length]).Left)).ToArray();
            return Group(Group(surface[0], surface[1], surface[2], Group(bs), Group(edges)), R(bs[0]).Left, Relation.Segment(new Carrier(), 0, 1));
        }
        Assert.False(ContourRotation.TryTraverse(Request([bindings[0], bindings[2], bindings[1], bindings[3]]), out _));
        var site = Location(R(bindings[0]).Left);
        bindings[0] = new Relation(Relation.Point(site.Carrier, site.Ticks), R(bindings[0]).Right);
        Assert.False(ContourRotation.TryTraverse(Request(bindings), out _));
    }

    [Theory]
    [InlineData(0)] [InlineData(1)] [InlineData(2)]
    public void OffsetsAreCalculatedBeforeTheTwoSupportsMeetAtOneConstructedCorner(int restores)
    {
        var roots = Fixture("measured-segment-offset", restores);
        Assert.True(SegmentOffset.TryConstruct(R(roots[0]), out var first));
        Assert.True(SegmentOffset.TryConstruct(R(roots[1]), out var second));
        Vector(R(Result(first)).Left, 0, 2); Vector(R(Result(first)).Right, 6, 2);
        Vector(R(Result(second)).Left, 4, 0); Vector(R(Result(second)).Right, 4, 6);
        Assert.True(SegmentOffset.TryJoin(R(roots[2]), out var joined));
        var path = F(Result(joined)); var joint = R(path[0]).Right;
        Assert.Same(joint, R(path[1]).Left); Vector(joint, 4, 2);
        Vector(R(path[0]).Left, 0, 2); Vector(R(path[1]).Right, 4, 6);
        var candidate = F(F(joined)[4]); var parameters = F(F(candidate[1])[1]);
        Number(parameters[0], 2, 3); Number(parameters[1], 1, 3);
        var history = F(Restore(joined, 2)); var restoredPath = F(history[2]);
        Assert.Same(R(restoredPath[0]).Right, R(restoredPath[1]).Left);
        Assert.Same(F(F(history[4])[1])[0], R(restoredPath[0]).Right);
        Assert.All(R(roots[2]).Carriers(), c => { Assert.Equal(0UL, c.Value); Assert.Equal(0, c.Tension); });
    }

    [Theory]
    [InlineData(3, 4, 2, -8, 6, 5)]
    [InlineData(-3, -4, 2, -8, 6, 5)] // Same physical guide, reversed frame: same selected physical side.
    [InlineData(0, 6, 2, -2, 0, 1)]
    [InlineData(6, 0, 0, 0, 0, 1)]
    [InlineData(5, 12, 3, -36, 15, 13)]
    public void AMeasuredOffsetHasExactDistanceAndPreservesOrderedDisplacement(long dx, long dy, long distance, long nx, long ny, long denominator)
    {
        var f = F(Fixture("measured-segment-offset")[0]); var chart = new Chart(f[1]);
        var a = chart.Point(10, 20); var b = chart.Point(10 + dx, 20 + dy);
        // Guide to the selected side, with deliberately irrelevant tangent content.
        var guide = chart.Point(10 - Math.Abs(dy) + 3 * dx, 20 + Math.Abs(dx) + 3 * dy);
        if (dx == 0) guide = chart.Point(9, 20);
        var edge = new Relation(a, b); var calibration = new Relation(R(R(f[2]).Right).Left, Relation.Segment(chart.X, 0, distance));
        var request = Group(edge, f[1], new Relation(Relation.OrthogonalOutlet(edge), calibration), new Relation(a, guide));
        Assert.True(SegmentOffset.TryConstruct(request, out var read));
        var result = R(Result(read));
        Vector(result.Left, 10 * denominator + nx, 20 * denominator + ny, denominator);
        Vector(result.Right, (10 + dx) * denominator + nx, (20 + dy) * denominator + ny, denominator);
        var proof = F(F(read)[4]); Number(proof[2], 0); Number(proof[3], distance * distance);
        // Independent integer checks do not reuse the kernel's normal formula.
        Assert.Equal(0, dx * nx + dy * ny);
        Assert.Equal(distance * distance * denominator * denominator, nx * nx + ny * ny);
        var capturedEdge = R(F(F(read)[1])[0]);
        Assert.Same(capturedEdge.Left, R(F(proof[0])[0]).Left);
        Assert.Same(result.Right, R(F(proof[0])[1]).Right);
        if (distance == 0) Assert.Same(capturedEdge.Left, result.Left);
    }

    [Theory]
    [InlineData(-1)] [InlineData(1)]
    public void AMetricNormalInAShearedChartIsNotTheScreensNinetyDegreeGuess(int sideSign)
    {
        var f = F(Fixture("measured-segment-offset")[0]); var chart = new Chart(f[1]);
        var a = chart.Point(10, 20); var edge = new Relation(a, chart.Point(17, 24));
        var request = Group(edge, chart.Metric(1, -1, 2), new Relation(Relation.OrthogonalOutlet(edge), R(f[2]).Right),
            new Relation(a, chart.Point(10 + sideSign, 20 + sideSign)));
        Assert.True(SegmentOffset.TryConstruct(request, out var read));
        Vector(R(Result(read)).Left, 50 - sideSign * 2, 100 + sideSign * 6, 5);
        var proof = F(F(read)[4]); Number(proof[2], 0); Number(proof[3], 4);
    }

    [Theory]
    [InlineData("collapsed")] [InlineData("irrational")] [InlineData("open-end")]
    [InlineData("tangent-guide")] [InlineData("different-guide-origin")]
    [InlineData("foreign-coordinate")] [InlineData("foreign-holder")]
    [InlineData("wrong-outlet")] [InlineData("no-outlet")] [InlineData("negative-reach")]
    public void OffsetFailurePreservesTheUnresolvedRequestInsteadOfSupplyingCandidateGeometry(string fault)
    {
        var f = F(Fixture("measured-segment-offset")[0]); var chart = new Chart(f[1]); var edge = R(f[0]);
        var a = edge.Left; var b = edge.Right;
        switch (fault)
        {
            case "collapsed": b = a; break;
            case "irrational": b = chart.Point(1, 1); break;
            case "open-end": b = new Placeholder(); break;
            case "tangent-guide": f[3] = new Relation(a, edge.Right); break;
            case "different-guide-origin": f[3] = new Relation(chart.Point(0, 0), R(f[3]).Right); break;
            case "foreign-coordinate": b = Relation.OrderedGroup(chart.Holder, new CarrierLocation(new Carrier(chart.X.UnitShape), 6), new CarrierLocation(chart.Y, 0)); break;
            case "foreign-holder": b = Group(new CarrierLocation(chart.X, 6), new CarrierLocation(chart.Y, 0)); break;
            case "negative-reach": f[2] = new Relation(R(f[2]).Left, new Relation(R(R(f[2]).Right).Left, Relation.Segment(chart.X, 0, -2))); break;
        }
        f[0] = new Relation(a, b);
        f[2] = new Relation(fault == "wrong-outlet" ? Relation.OrthogonalOutlet(edge) :
            fault == "no-outlet" ? new Relation(f[0], f[0]) : Relation.OrthogonalOutlet(f[0]), R(f[2]).Right);
        Assert.False(SegmentOffset.TryConstruct(R(Restore(Group(f), 2)), out var read));
        Assert.IsType<Placeholder>(Result(read)); Assert.NotEmpty(F(F(read)[3]));
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void AnOuterCornerRequiresExplicitPermissionToExtendBothSourceSupports(bool extend)
    {
        var roots = Fixture("measured-segment-offset"); var join = F(roots[extend ? 3 : 2]);
        var first = F(join[0]); var second = F(join[1]); var chart = new Chart(first[1]);
        first[3] = new Relation(R(first[0]).Left, chart.Point(0, -1));
        second[3] = new Relation(R(second[0]).Left, chart.Point(7, 0));
        join[0] = Group(first); join[1] = Group(second);
        Assert.Equal(extend, SegmentOffset.TryJoin(Group(join), out var read));
        if (extend)
        {
            Vector(R(F(Result(read))[0]).Right, 8, -2);
            var parameters = F(F(F(F(read)[4])[1])[1]); Number(parameters[0], 4, 3); Number(parameters[1], -1, 3);
        }
        else
        {
            Assert.IsType<Placeholder>(Result(read)); Assert.NotEmpty(F(F(read)[3]));
            var evidence = F(F(read)[4]); Assert.Equal(2, F(evidence[0]).Length);
            Vector(F(evidence[1])[0], 8, -2); // Candidate is evidence, not an accepted join.
        }
    }

    [Theory]
    [InlineData("lookalike-joint")] [InlineData("different-metric")] [InlineData("different-calibration")]
    [InlineData("opposite-side")] [InlineData("parallel")] [InlineData("open-end")]
    [InlineData("foreign-support")]
    public void ACornerDoesNotInferContactOrAJoinPolicyFromCoordinateEquality(string fault)
    {
        var join = F(Fixture("measured-segment-offset")[2]); var first = F(join[0]); var second = F(join[1]);
        var chart = new Chart(first[1]);
        switch (fault)
        {
            case "lookalike-joint":
                var start = chart.Point(6, 0); second[0] = new Relation(start, R(second[0]).Right);
                second[2] = new Relation(Relation.OrthogonalOutlet(second[0]), R(first[2]).Right);
                second[3] = new Relation(start, chart.Point(5, 0)); break;
            case "different-metric": second[1] = chart.Metric(1, 0, 1); break;
            case "different-calibration": second[2] = new Relation(R(second[2]).Left, new Relation(R(R(first[2]).Right).Left, Relation.Segment(chart.X, 0, 2))); break;
            case "opposite-side": second[3] = new Relation(R(second[0]).Left, chart.Point(7, 0)); break;
            case "parallel":
                second[0] = new Relation(R(second[0]).Left, chart.Point(12, 0));
                second[2] = new Relation(Relation.OrthogonalOutlet(second[0]), R(first[2]).Right);
                second[3] = new Relation(R(second[0]).Left, chart.Point(6, 1)); break;
            case "open-end":
                second[0] = new Relation(R(second[0]).Left, new Placeholder());
                second[2] = new Relation(Relation.OrthogonalOutlet(second[0]), R(first[2]).Right); break;
            case "foreign-support":
                var supports = F(join[2]); supports[0] = new Relation(R(supports[0]).Left, Relation.Segment(new Carrier(), 0, 1)); join[2] = Group(supports); break;
        }
        join[0] = Group(first); join[1] = Group(second);
        Assert.False(SegmentOffset.TryJoin(R(Restore(Group(join), 2)), out var read));
        Assert.IsType<Placeholder>(Result(read)); Assert.NotEmpty(F(F(read)[3]));
    }

    [Fact]
    public void ConstructionHistoryCapturesInputsAndAChangedCalibrationRequiresANewRead()
    {
        var roots = Fixture("measured-segment-offset"); var request = R(roots[2]);
        Assert.True(SegmentOffset.TryJoin(request, out var before));
        var calibration = R(R(F(F(request)[0])[2]).Right); Location(R(calibration.Right).Right).Tick.Set(3);
        Vector(R(F(Result(before))[0]).Right, 4, 2);
        Assert.True(SegmentOffset.TryJoin(request, out var after)); Vector(R(F(Result(after))[0]).Right, 3, 3);
        var captured = R(F(before)[1]);
        Assert.True(SegmentOffset.TryJoin(R(Restore(captured, 2)), out var replay)); Vector(R(F(Result(replay))[0]).Right, 4, 2);
    }

    [Fact]
    public void ExactNormalizationDoesNotLoseIntegersBeyondFloatingPointPrecision()
    {
        var f = F(Fixture("measured-segment-offset")[0]); var chart = new Chart(f[1]);
        const long large = 9007199254740993;
        var a = chart.Point(large, 0); var edge = new Relation(a, chart.Point(large + 6, 0));
        var request = Group(edge, f[1], new Relation(Relation.OrthogonalOutlet(edge), R(f[2]).Right),
            new Relation(a, chart.Point(large, 1)));
        Assert.True(SegmentOffset.TryConstruct(R(Restore(request, 2)), out var read));
        Vector(R(Result(read)).Left, large, 2); Vector(R(Result(read)).Right, large + 6, 2);
    }

    [Fact]
    public void UnrepresentableCoordinateGrowthRemainsUnresolved()
    {
        var f = F(Fixture("measured-segment-offset")[0]); var chart = new Chart(f[1]);
        var a = chart.Point(long.MaxValue, 0); var edge = new Relation(a, chart.Point(long.MaxValue, 6));
        var request = Group(edge, f[1], new Relation(Relation.OrthogonalOutlet(edge), R(f[2]).Right),
            new Relation(a, chart.Point(long.MaxValue - 1, 0)));
        Assert.True(SegmentOffset.TryConstruct(request, out var first));
        Vector(R(Result(first)).Left, long.MaxValue - 2, 0);
        // Re-express at the other representable edge; a draw past the storage
        // limit must not wrap, clamp or silently use a floating-point result.
        a = chart.Point(long.MinValue, 0); edge = new Relation(a, chart.Point(long.MinValue, 6));
        request = Group(edge, f[1], new Relation(Relation.OrthogonalOutlet(edge), R(f[2]).Right),
            new Relation(a, chart.Point(long.MinValue + 1, 0)));
        Assert.True(SegmentOffset.TryConstruct(request, out _));
        a = chart.Point(long.MaxValue - 1, 0); edge = new Relation(a, chart.Point(long.MaxValue - 1, 6));
        request = Group(edge, f[1], new Relation(Relation.OrthogonalOutlet(edge), R(f[2]).Right),
            new Relation(a, chart.Point(long.MaxValue, 0)));
        Assert.False(SegmentOffset.TryConstruct(request, out var rejected));
        Assert.IsType<Placeholder>(Result(rejected)); Assert.NotEmpty(F(F(rejected)[3]));
    }

    [Fact]
    public void TheWalkthroughDisplaysTheActualFixtureSyntaxAndCalculatedKernelTraces()
    {
        var html = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Gallery", "metric-construction.html"));
        foreach (var (id, fixture) in new[] { ("circle-syntax", "constructed-circle-rotation"), ("offset-syntax", "measured-segment-offset") })
        {
            var match = Regex.Match(html, $"<pre id=\"{id}\">(.*?)</pre>", RegexOptions.Singleline);
            Assert.True(match.Success);
            var syntax = WebUtility.HtmlDecode(match.Groups[1].Value).Replace("\r\n", "\n").Trim();
            var authored = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "Syntax", fixture + ".gojo"));
            Assert.Equal(authored.Replace("\r\n", "\n").Trim(), syntax);
            Assert.NotEmpty(GojoSyntax.Parse(syntax).Roots);
        }
        var json = Regex.Match(html, "<script id=\"trace\" type=\"application/json\">(.*?)</script>", RegexOptions.Singleline);
        using var trace = JsonDocument.Parse(json.Groups[1].Value);
        var c = trace.RootElement.GetProperty("circle"); var o = trace.RootElement.GetProperty("offset");
        void MatchPoint(IElement point, JsonElement expected) => Vector(point, expected[0].GetInt64(), expected[1].GetInt64());
        void MatchEdge(IElement edge, JsonElement expected) { MatchPoint(R(edge).Left, expected[0]); MatchPoint(R(edge).Right, expected[1]); }
        var circleRoots = Fixture("constructed-circle-rotation"); var cf = F(circleRoots[0]);
        MatchPoint(R(cf[2]).Right, c.GetProperty("centre")); MatchPoint(R(cf[4]).Right, c.GetProperty("direction")); MatchPoint(R(cf[5]).Right, c.GetProperty("side"));
        Assert.True(ContourConstruction.TryCalibrateQuarters(R(circleRoots[0]), out var circle));
        var bindings = F(F(Result(circle))[3]);
        for (var i = 0; i < bindings.Length; i++) MatchPoint(R(R(bindings[i]).Right).Right, c.GetProperty("rims")[i]);
        Number(F(F(circle)[4])[2], c.GetProperty("squaredRadius").GetInt64());
        Assert.True(ContourRotation.TryTraverse(Group(Result(circle), R(bindings[0]).Left, Relation.Segment(new Carrier(), 0, 1)), out var walk));
        Assert.True(ContourRotation.TryApply(Group(walk, circleRoots[1]), out var applied));
        for (var i = 0; i < 2; i++)
        {
            MatchPoint(F(circleRoots[1])[i], c.GetProperty("sourcePoints")[i]);
            MatchPoint(F(Result(applied))[i], c.GetProperty("turnedPoints")[i]);
        }
        var offsetRoots = Fixture("measured-segment-offset"); var first = F(offsetRoots[0]); var second = F(offsetRoots[1]);
        MatchPoint(R(first[0]).Left, o.GetProperty("source")[0]); MatchPoint(R(first[0]).Right, o.GetProperty("source")[1]); MatchPoint(R(second[0]).Right, o.GetProperty("source")[2]);
        Assert.True(SegmentOffset.TryConstruct(R(offsetRoots[0]), out var a)); MatchEdge(Result(a), o.GetProperty("first"));
        Assert.True(SegmentOffset.TryConstruct(R(offsetRoots[1]), out var b)); MatchEdge(Result(b), o.GetProperty("second"));
        Number(F(F(a)[4])[3], o.GetProperty("squaredDistance").GetInt64());
        Assert.True(SegmentOffset.TryJoin(R(offsetRoots[2]), out var join)); MatchPoint(R(F(Result(join))[0]).Right, o.GetProperty("joint"));
        var chart = new Chart(first[1]);
        first[3] = new Relation(R(first[0]).Left, chart.Point(0, -1)); second[3] = new Relation(R(second[0]).Left, chart.Point(7, 0));
        Assert.True(SegmentOffset.TryConstruct(Group(first), out var outerA)); MatchEdge(Result(outerA), o.GetProperty("outerFirst"));
        Assert.True(SegmentOffset.TryConstruct(Group(second), out var outerB)); MatchEdge(Result(outerB), o.GetProperty("outerSecond"));
        foreach (var (root, property) in new[] { (2, "outerWholeAccepted"), (3, "outerExtendedAccepted") })
        {
            var request = Group(Group(first), Group(second), F(offsetRoots[root])[2]);
            Assert.Equal(o.GetProperty(property).GetBoolean(), SegmentOffset.TryJoin(request, out var outer));
            MatchPoint(F(F(F(outer)[4])[1])[0], o.GetProperty("outerJoint"));
            var actualParameters = F(F(F(F(outer)[4])[1])[1]);
            for (var i = 0; i < 2; i++) Number(actualParameters[i], o.GetProperty("outerParameters")[i][0].GetInt64(), o.GetProperty("outerParameters")[i][1].GetInt64());
        }
        var parameters = F(F(F(F(join)[4])[1])[1]);
        for (var i = 0; i < 2; i++) Number(parameters[i], o.GetProperty("parameters")[i][0].GetInt64(), o.GetProperty("parameters")[i][1].GetInt64());
    }

    private sealed class Chart
    {
        private readonly IElement basis; private readonly IElement[] units; private readonly Carrier square;
        public Carrier Holder { get; } public Carrier X { get; } public Carrier Y { get; }
        public Chart(IElement metric)
        {
            var f = F(metric); basis = f[0]; Holder = (Carrier)R(basis).Right; units = F(basis);
            X = Location(R(units[0]).Left).Carrier; Y = Location(R(units[1]).Left).Carrier;
            Assert.True(ProjectionResidue.TryRead(R(F(f[1])[0]).Right, out square!, out _, out _));
        }
        public Relation Point(long x, long y) => Relation.OrderedGroup(Holder, new CarrierLocation(X, x), new CarrierLocation(Y, y));
        public Relation Metric(long xx, long xy, long yy) => Group(basis, Group(
            new Relation(new Relation(units[0], units[0]), new CarrierLocation(square, xx)),
            new Relation(new Relation(units[0], units[1]), new CarrierLocation(square, xy)),
            new Relation(new Relation(units[1], units[1]), new CarrierLocation(square, yy))));
    }
    private static IElement[] Fixture(string name, int restores = 0)
    {
        var roots = GojoSyntax.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "Syntax", name + ".gojo"))).Roots.ToArray();
        for (var i = 0; i < restores; i++) roots = GojoNumericFormat.Decode(GojoNumericFormat.Encode(roots));
        return roots;
    }
    private static IElement Restore(IElement e, int times)
    { for (var i = 0; i < times; i++) e = GojoNumericFormat.Decode(GojoNumericFormat.Encode(e))[0]; return e; }
    private static Relation R(IElement e) => Assert.IsType<Relation>(e);
    private static IElement[] F(IElement e) { Assert.True(R(e).TryReadOrderedGroup(out var f)); return f; }
    private static IElement Result(IElement w) => F(w)[2];
    private static Relation Group(params IElement[] f) => Relation.OrderedGroup(new Carrier(), f);
    private static CarrierLocation Location(IElement e) => Assert.IsType<CarrierLocation>(R(e).Left);
    private static void Number(IElement e, long n, long d = 1)
    {
        if (e is Relation { Left: CarrierLocation location, Right: Carrier }) e = location;
        Assert.True(ProjectionResidue.TryRead(e, out _, out var numerator, out var denominator));
        Assert.Equal(new BigInteger(n) * denominator, new BigInteger(numerator) * d);
    }
    private static void Vector(IElement e, long x, long y, long d = 1)
    { var f = F(e); Number(f[0], x, d); Number(f[1], y, d); }
}
