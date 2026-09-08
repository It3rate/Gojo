using System.Numerics;
using System.Text.Json;
using System.Text.RegularExpressions;
using Gojo.Core;
using Gojo.Encoding.Syntax;
using Gojo.Patterns;
using Gojo.Structures;

namespace Gojo.Encoding.Tests;

public sealed class GojoSyntaxRotationTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void RadialTurnMovesAnAttachedFrameWithoutAnglesAndPreservesPointIdentity(int restores)
    {
        var f = new Fixture(restores);
        var traversal = f.Walk(f.Surface, 1);
        Assert.True(ContourRotation.TryTurn(traversal, out var turn));
        Number(F(turn)[4], 25);
        var applied = f.Apply(traversal);
        var points = F(Result(applied));
        Vector(points[0], 10, 22); Vector(points[1], 9, 22);
        Vector(points[2], 9, 23); Vector(points[3], 10, 23);
        Assert.Same(points[0], points[4]); Assert.NotSame(points[0], points[5]);
        Vector(F(f.Points)[0], 12, 20);
        Assert.All(f.Surface.Carriers(), c => { Assert.Equal(0UL, c.Value); Assert.Equal(0, c.Tension); });
        var restored = Restore(applied, 2);
        Assert.Same(F(Result(restored))[0], F(Result(restored))[4]);
        var correspondence = F(F(restored)[4]);
        Assert.Same(R(F(correspondence[0])[0]).Right, F(Result(restored))[0]);
        Assert.Same(Result(F(correspondence[1])[0]), F(Result(restored))[0]);
    }

    [Theory]
    [InlineData(-9, 12, -3)]
    [InlineData(-4, 0, -1)]
    [InlineData(-1, 12, -1)]
    [InlineData(0, 0, 0)]
    [InlineData(1, 4, 0)]
    [InlineData(4, 0, 1)]
    [InlineData(9, 4, 2)]
    [InlineData(long.MaxValue, 12, 2305843009213693951)]
    public void SignedTraversalKeepsCrossingsSeparatelyFromTheFinalSite(long steps, long marker, long laps)
    {
        var f = new Fixture(1);
        var walk = f.Walk(f.Surface, steps);
        Number(F(Result(walk))[1], marker); Number(F(Result(walk))[3], laps);
        var turn = f.Apply(walk);
        var expected = marker switch { 0 => (12, 20), 4 => (10, 22), 8 => (8, 20), _ => (10, 18) };
        Vector(F(Result(turn))[0], expected.Item1, expected.Item2);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void UnevenLandmarksDeriveExactRationalTurnsAndRefinementKeepsTheCoarserEndpoint(int restores)
    {
        var f = new Fixture(restores);
        var fine = f.Apply(f.Walk(f.Refined, 1));
        Vector(F(Result(fine))[0], 56, 108, 5);
        Vector(F(Result(fine))[1], 52, 111, 5);
        var twoSteps = f.Apply(f.Walk(f.Refined, 2));
        Vector(F(Result(twoSteps))[0], 10, 22);
        // Four steps in the refined cycle are not a full revolution.
        Vector(F(Result(f.Apply(f.Walk(f.Refined, 4))))[0], 10, 18);
        Vector(F(Result(f.Apply(f.Walk(f.Refined, 5))))[0], 12, 20);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void UnknownGeometryRetainsTraversalAndCanBeRefinedWithoutInventingAnEndpoint(int restores)
    {
        var f = new Fixture(restores);
        var walk = f.Walk(f.Partial, 1);
        Number(F(Result(walk))[1], 4);
        Assert.False(ContourRotation.TryTurn(walk, out var incomplete));
        Assert.IsType<Placeholder>(Result(incomplete));
        Assert.Same(walk, F(incomplete)[0]);
        var captured = F(walk)[4];
        var surface = F(captured)[0];
        var missing = R(R(F(F(surface)[3])[1]).Right).Right;
        var replacement = R(R(F(F(f.Surface)[3])[1]).Right).Right;
        var refined = StructuralPattern.Refine(captured, new Relation(missing, replacement));
        Assert.True(ContourRotation.TryTraverse(R(refined), out var next));
        Vector(F(Result(f.Apply(next)))[0], 10, 22);
        Assert.False(ContourRotation.TryTurn(walk, out _));
    }

    [Fact]
    public void MetricPairingsCanDescribeNonPerpendicularCoordinates()
    {
        var f = new Fixture();
        var fields = F(f.Surface);
        var centre = f.Point(0, 0);
        var metrics = F(fields[2]); var pairings = F(metrics[1]);
        var square = Location(R(pairings[0]).Right).Carrier;
        pairings[1] = new Relation(R(pairings[1]).Left, new CarrierLocation(square, 1));
        pairings[2] = new Relation(R(pairings[2]).Left, new CarrierLocation(square, 2));
        fields[2] = Group(metrics[0], Group(pairings));
        fields[0] = new Relation(R(fields[0]).Left, centre);
        var rimPoints = new[] { f.Point(5, 0), f.Point(-5, 5), f.Point(-5, 0), f.Point(5, -5) };
        fields[3] = Group(F(fields[3]).Select((binding, i) =>
            (IElement)new Relation(R(binding).Left, new Relation(centre, rimPoints[i]))).ToArray());
        var surface = Group(fields);
        var applied = f.Apply(f.Walk(surface, 1), Group(f.Point(2, 0), f.Point(0, 1)));
        Vector(F(Result(applied))[0], -2, 2);
        Vector(F(Result(applied))[1], -2, 1);
    }

    [Fact]
    public void ReversingTheActualCycleReversesTheTurnWithoutChangingCarrierFaces()
    {
        var f = new Fixture();
        var s = F(f.Surface); var bindings = F(s[3]);
        var reverse = new[] { bindings[0], bindings[3], bindings[2], bindings[1] };
        s[3] = Group(reverse); s[4] = Edges(reverse);
        Vector(F(Result(f.Apply(f.Walk(Group(s), 1))))[0], 10, 18);
        Assert.All(Group(s).Carriers(), carrier => Assert.False(carrier.IsInverse));
    }

    [Theory]
    [InlineData("radius")]
    [InlineData("collapsed")]
    [InlineData("metric")]
    [InlineData("metric_role")]
    [InlineData("metric_unit")]
    [InlineData("crossing")]
    [InlineData("repeated_pose")]
    public void FalseGeometricClaimsDoNotEraseTheValidTopologicalRead(string fault)
    {
        var f = new Fixture(); var s = F(f.Surface); var bindings = F(s[3]);
        var metric = F(s[2]); var pairings = F(metric[1]);
        var square = Location(R(pairings[0]).Right).Carrier;
        switch (fault)
        {
            case "radius": bindings[1] = new Relation(R(bindings[1]).Left, new Relation(f.Centre, f.Point(10, 26))); break;
            case "collapsed":
                bindings = bindings.Select(b => (IElement)new Relation(R(b).Left, new Relation(f.Centre, f.Centre))).ToArray(); break;
            case "metric": pairings[1] = new Relation(R(pairings[1]).Left, new CarrierLocation(square, 2)); break;
            case "metric_role": pairings[1] = new Relation(R(pairings[0]).Left, R(pairings[1]).Right); break;
            case "metric_unit": pairings[0] = new Relation(R(pairings[0]).Left, new CarrierLocation(f.X, 1)); break;
            case "crossing": bindings = [bindings[0], bindings[2], bindings[1], bindings[3]]; s[4] = Edges(bindings); break;
            case "repeated_pose": bindings[1] = new Relation(R(bindings[1]).Left, R(bindings[0]).Right); break;
        }
        s[2] = Group(metric[0], Group(pairings)); s[3] = Group(bindings);
        var walk = f.Walk(Group(s), 1);
        Assert.False(ContourRotation.TryTurn(walk, out var failure));
        Assert.IsType<Placeholder>(Result(failure));
        Assert.False(ContourRotation.TryApply(Group(walk, f.Points), out _));
    }

    [Fact]
    public void EqualCoordinatesAndForeignOriginsDoNotSupplyTheMissingConnections()
    {
        var f = new Fixture(); var s = F(f.Surface); var edges = F(s[4]);
        var end = PointLocation(R(edges[0]).Right);
        edges[0] = new Relation(R(edges[0]).Left, Relation.Point(end.Carrier, end.Ticks));
        s[4] = Group(edges);
        Assert.False(ContourRotation.TryTraverse(f.Request(Group(s), 1), out _));
        Assert.False(ContourRotation.TryTraverse(Group(f.Surface,
            Relation.Point(f.Perimeter, 0), Relation.Segment(f.Steps, 0, 1)), out _));
        s = F(f.Surface); s[0] = new Relation(Relation.Point(new Carrier(), 0), f.Centre);
        Assert.False(ContourRotation.TryTraverse(f.Request(Group(s), 1), out _));
    }

    [Fact]
    public void RecordedTurnDoesNotDriftWhenOriginalSitesOrMetricsChange()
    {
        var f = new Fixture(); var walk = f.Walk(f.Surface, 1);
        var applied = f.Apply(walk);
        var rim = R(R(F(F(f.Surface)[3])[1]).Right).Right;
        Location(F(rim)[1]).Tick.Set(26);
        Location(F(f.Centre)[0]).Tick.Set(40);
        var pairing = R(F(F(F(f.Surface)[2])[1])[0]);
        Location(pairing.Right).Tick.Set(9);
        Vector(F(Result(applied))[0], 10, 22);
        Vector(F(Result(f.Apply(walk)))[0], 10, 22);
        Assert.False(ContourRotation.TryTurn(f.Walk(f.Surface, 1), out _));
        Vector(F(Result(Restore(applied, 2)))[1], 9, 22);
    }

    [Fact]
    public void PartialAttachedCoordinatesRemainUsefulAndDoNotBecomeZeros()
    {
        var f = new Fixture();
        var points = Group(Relation.OrderedGroup(f.Space, new Placeholder(), new CarrierLocation(f.Y, 21)));
        Assert.False(ContourRotation.TryApply(Group(f.Walk(f.Surface, 1), points), out var partial));
        var output = F(F(Result(partial))[0]);
        Number(output[0], 9); Assert.IsType<Placeholder>(output[1]);
        Assert.NotEmpty(F(F(partial)[3]));
    }

    [Fact]
    public void TamperedTraversalCannotSupplyAContradictoryEndpointOrCrossingCount()
    {
        var f = new Fixture(); var walk = f.Walk(f.Surface, 1); var w = F(walk); var r = F(w[2]);
        r[1] = r[0]; w[2] = Group(r);
        Assert.False(ContourRotation.TryTurn(Group(w), out _));
        w = F(walk); r = F(w[2]); r[3] = new CarrierLocation(f.Steps, 42); w[2] = Group(r);
        Assert.False(ContourRotation.TryTurn(Group(w), out _));
    }

    [Fact]
    public void IndependentDistanceChecksAndRoundTripsHoldAcrossAllAuthoredTurns()
    {
        var f = new Fixture();
        for (var step = -8; step <= 8; step++)
        {
            var output = F(Result(f.Apply(f.Walk(f.Surface, step))));
            var input = F(f.Points);
            for (var i = 0; i < 4; i++)
            for (var j = 0; j < 4; j++)
                Assert.Equal(SquaredDistance(input[i], input[j]), SquaredDistance(output[i], output[j]));
            var returned = f.Apply(f.Walk(f.Surface, -step), Group(output));
            Vector(F(Result(returned))[0], 12, 20); Vector(F(Result(returned))[2], 13, 21);
        }
    }

    [Fact]
    public void RenamingEveryAuthoredIdentifierLeavesTheReadUnchanged()
    {
        var source = Source();
        var names = Regex.Matches(source, @"[$?][a-zA-Z_][a-zA-Z0-9_]*").Select(m => m.Value).Distinct().ToArray();
        for (var i = 0; i < names.Length; i++) source = Regex.Replace(source,
            Regex.Escape(names[i]) + @"\b", names[i][0] + "renamed_" + i);
        var roots = GojoSyntax.Parse(source).Roots.ToArray();
        Assert.True(ContourRotation.TryTraverse(R(roots[1]), out var walk));
        Assert.True(ContourRotation.TryApply(Group(walk, roots[2]), out var applied));
        Vector(F(Result(applied))[0], 10, 22);
    }

    [Fact]
    public void CallerCanBindTheAuthoredRequestAndAdoptFourSuccessiveFramesWithHistory()
    {
        var roots = GojoSyntax.Parse(Source()).Roots.ToArray();
        var surface = R(roots[0]); var initial = roots[2]; var current = initial;
        var stepAxis = PointLocation(R(F(roots[1])[2]).Left).Carrier;
        var bindings = F(F(surface)[3]);
        var steps = new List<Relation>(); var time = new Carrier();
        for (var i = 0; i < 4; i++)
        {
            var request = Group(surface, R(bindings[i]).Left, Relation.Segment(stepAxis, 0, 1));
            Assert.True(ContourRotation.TryTraverse(request, out var walk));
            var template = i == 0 ? roots[5] : Group(F(roots[5])[0], current);
            var bound = StructuralPattern.Refine(template, new Relation(F(roots[5])[0], walk));
            Assert.True(ContourRotation.TryApply(R(bound), out var applied));
            var next = Result(applied);
            steps.Add(TimeStepSequence.Step(new Carrier(), time, i, i + 1,
                current, applied, next, F(applied)[3]));
            current = next; // Explicitly adopt the read; no source site is assigned a value.
        }
        var history = Restore(TimeStepSequence.Create(new Carrier(), time, initial, [.. steps]), 2);
        Assert.True(TimeStepSequence.TryRead(history, out _, out var recordedInitial, out var recordedSteps));
        Assert.Equal(4, recordedSteps.Length);
        Assert.Same(recordedInitial, F(recordedSteps[0])[1]);
        for (var i = 1; i < 4; i++) Assert.Same(F(recordedSteps[i - 1])[3], F(recordedSteps[i])[1]);
        Vector(F(F(recordedSteps[0])[3])[0], 10, 22);
        Vector(F(F(recordedSteps[1])[3])[0], 8, 20);
        Vector(F(F(recordedSteps[2])[3])[0], 10, 18);
        Vector(F(F(recordedSteps[3])[3])[0], 12, 20);
        Assert.NotSame(recordedInitial, F(recordedSteps[3])[3]);
        var lastApplied = F(recordedSteps[3])[2];
        var lastWalk = F(F(lastApplied)[0])[0];
        Number(F(Result(lastWalk))[3], 1);
        Assert.Same(F(current)[0], F(current)[4]);
        Assert.NotSame(F(current)[0], F(current)[5]);
    }

    [Fact]
    public void OutputBeyondTheCurrentIntegerEncodingRemainsUnresolvedWithoutMutation()
    {
        var f = new Fixture(); var input = Group(f.Point(long.MinValue, 20));
        var request = Group(f.Walk(f.Surface, 2), input);
        Assert.False(ContourRotation.TryApply(request, out var failure));
        Assert.IsType<Placeholder>(Result(failure)); Assert.Same(request, F(F(failure)[3])[0]);
        Vector(F(input)[0], long.MinValue, 20);
        Assert.All(input.Carriers(), c => { Assert.Equal(0UL, c.Value); Assert.Equal(0, c.Tension); });
    }

    [Fact]
    public void AnUnencodableSquaredReachDoesNotEraseTraversalOrWrapItsValue()
    {
        var f = new Fixture(); var fields = F(f.Surface); var centre = f.Point(0, 0);
        fields[0] = new Relation(R(fields[0]).Left, centre);
        var points = new[] { f.Point(4000000000, 0), f.Point(0, 4000000000),
            f.Point(-4000000000, 0), f.Point(0, -4000000000) };
        fields[3] = Group(F(fields[3]).Select((b, i) =>
            (IElement)new Relation(R(b).Left, new Relation(centre, points[i]))).ToArray());
        var walk = f.Walk(Group(fields), 1);
        Assert.False(ContourRotation.TryTurn(walk, out var failure));
        Assert.IsType<Placeholder>(Result(failure)); Number(F(Result(walk))[1], 4);
    }

    [Fact]
    public void ForeignCoordinateCarriersAndOpposedSlotsRequireAnExplicitInterpretation()
    {
        var f = new Fixture(); var walk = f.Walk(f.Surface, 1);
        var foreign = Relation.OrderedGroup(f.Space,
            new CarrierLocation(new Carrier(f.X.UnitShape), 12), new CarrierLocation(f.Y, 20));
        Assert.False(ContourRotation.TryApply(Group(walk, Group(foreign)), out _));
        var opposed = Relation.OrderedGroup(f.Space, new Placeholder().Dual(), new CarrierLocation(f.Y, 20));
        Assert.False(ContourRotation.TryApply(Group(walk, Group(opposed)), out _));
        Assert.False(ContourRotation.TryApply(Group(walk, Group()), out _));
    }

    [Fact]
    public void DemoStatesAndDisplayedSyntaxAreCheckedAgainstTheActualFixture()
    {
        var html = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Gallery", "calibrated-contour-rotation.html"));
        using var trace = JsonDocument.Parse(Regex.Match(html,
            "<script id=\"trace\" type=\"application/json\">([\\s\\S]*?)</script>").Groups[1].Value);
        Assert.Equal(9, trace.RootElement.GetArrayLength());
        foreach (var row in trace.RootElement.EnumerateArray())
        {
            var f = new Fixture(2);
            var kind = row.GetProperty("surface").GetString();
            Relation surface = kind switch { "refined" => f.Refined, "partial" or "resolved" => f.Partial, _ => f.Surface };
            if (kind == "resolved")
            {
                var slot = R(R(F(F(surface)[3])[1]).Right).Right;
                var rim = R(R(F(F(f.Surface)[3])[1]).Right).Right;
                surface = R(StructuralPattern.Refine(surface, new Relation(slot, rim)));
            }
            var walk = f.Walk(surface, row.GetProperty("steps").GetInt64());
            Number(F(Result(walk))[1], row.GetProperty("marker").GetInt64());
            Number(F(Result(walk))[3], row.GetProperty("crossings").GetInt64());
            var success = ContourRotation.TryApply(Group(walk, f.Points), out var applied);
            var expected = row.GetProperty("points");
            Assert.Equal(expected.ValueKind != JsonValueKind.Null, success);
            if (!success) { Assert.Equal(JsonValueKind.Null, row.GetProperty("rim").ValueKind); continue; }
            var actual = F(Result(applied)); var index = 0;
            foreach (var p in expected.EnumerateArray()) Vector(actual[index++], p[0].GetInt64(), p[1].GetInt64(), p[2].GetInt64());
            Assert.Equal(4, index);
            var turn = F(applied)[1]; var finalRadial = R(F(Result(turn))[2]);
            var end = row.GetProperty("rim"); Vector(finalRadial.Right, end[0].GetInt64(), end[1].GetInt64());
        }
        var syntax = System.Net.WebUtility.HtmlDecode(Regex.Match(html,
            "<pre id=\"syntax\">([\\s\\S]*?)</pre>").Groups[1].Value);
        Assert.NotEmpty(syntax);
        foreach (var line in syntax.Split('\n').Where(line => !string.IsNullOrWhiteSpace(line)))
            Assert.Contains(line.Trim(), Source());
    }

    private static BigInteger SquaredDistance(IElement a, IElement b)
    {
        var ax = Location(F(a)[0]).Ticks; var ay = Location(F(a)[1]).Ticks;
        var bx = Location(F(b)[0]).Ticks; var by = Location(F(b)[1]).Ticks;
        var dx = new BigInteger(ax) - bx; var dy = new BigInteger(ay) - by;
        return dx * dx + dy * dy;
    }
    private static Relation Edges(IElement[] bindings) => Group(bindings.Select((b, i) =>
        (IElement)new Relation(R(b).Left, R(bindings[(i + 1) % bindings.Length]).Left)).ToArray());
    private static string Source() => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "Syntax", "contour-rotation.gojo"));
    private static Relation R(IElement e) => Assert.IsType<Relation>(e);
    private static IElement[] F(IElement e) { Assert.True(R(e).TryReadOrderedGroup(out var fields)); return fields; }
    private static IElement Result(IElement e) => F(e)[2];
    private static Relation Group(params IElement[] e) => Relation.OrderedGroup(new Carrier(), e);
    private static CarrierLocation Location(IElement e) => Assert.IsType<CarrierLocation>(e);
    private static CarrierLocation PointLocation(IElement e) => Location(R(e).Left);
    private static void Number(IElement e, long n, long d = 1)
    {
        if (e is Relation { Left: CarrierLocation p, Right: Carrier }) e = p;
        Assert.True(ProjectionResidue.TryRead(e, out _, out var actualN, out var actualD));
        Assert.Equal(new BigInteger(n) * actualD, actualN * d);
    }
    private static void Vector(IElement e, long x, long y, long d = 1) { Number(F(e)[0], x, d); Number(F(e)[1], y, d); }
    private static Relation Restore(IElement e, int times)
    {
        for (var i = 0; i < times; i++) e = Assert.Single(GojoNumericFormat.Decode(GojoNumericFormat.Encode(e)));
        return R(e);
    }
    private sealed class Fixture
    {
        internal readonly Relation Surface, Refined, Partial, Points;
        internal readonly Carrier X, Y, Space, Steps, Perimeter;
        internal IElement Centre => R(F(Surface)[0]).Right;
        internal Fixture(int restores = 0)
        {
            var roots = GojoSyntax.Parse(Source()).Roots.ToArray();
            var captured = F(Restore(Group(roots), restores));
            Surface = R(captured[0]); Points = R(captured[2]); Refined = R(captured[3]); Partial = R(captured[4]);
            X = Location(F(Centre)[0]).Carrier; Y = Location(F(Centre)[1]).Carrier;
            Space = (Carrier)R(Centre).Right; Perimeter = (Carrier)F(Surface)[1];
            Steps = PointLocation(R(F(captured[1])[2]).Left).Carrier;
        }
        internal Relation Point(long x, long y) => Relation.OrderedGroup(Space, new CarrierLocation(X, x), new CarrierLocation(Y, y));
        internal Relation Request(Relation surface, long steps) => Group(surface, R(F(F(surface)[3])[0]).Left, Relation.Segment(Steps, 0, steps));
        internal Relation Walk(Relation surface, long steps)
        { Assert.True(ContourRotation.TryTraverse(Request(surface, steps), out var read)); return read; }
        internal Relation Apply(Relation walk, IElement? points = null)
        { Assert.True(ContourRotation.TryApply(Group(walk, points ?? Points), out var read)); return read; }
    }
}
