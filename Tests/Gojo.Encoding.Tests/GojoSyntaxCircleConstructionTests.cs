using System.Numerics;
using System.Text.Json;
using System.Text.RegularExpressions;
using Gojo.Core;
using Gojo.Encoding.Syntax;
using Gojo.Patterns;
using Gojo.Structures;

namespace Gojo.Encoding.Tests;

public sealed class GojoSyntaxCircleConstructionTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void CircleIsConstructedFromAnOffsetRadiusAndClosureBeforeItHasLandmarks(int restores)
    {
        var f = new Fixture(restores);
        Assert.True(ContourConstruction.TryReadOffset(R(f.Raw[2]), out var open));
        Assert.IsType<Placeholder>(R(R(Result(open)).Right).Left);
        Assert.Single(F(F(open)[3]));
        Assert.False(ContourConstruction.TryReadCircle(R(f.Raw[4]), out _));
        Assert.True(ContourConstruction.TryReadCircle(f.Circle, out var read));
        var circle = F(Result(read)); var offset = R(circle[0]); var source = R(offset.Left).Left;
        var radial = F(circle[1]); var radius = R(radial[1]); var whole = R(circle[2]); var closure = R(circle[3]);
        Assert.Same(source, R(radial[0]).Left);
        Assert.Same(radius.Left, R(radial[0]).Right); Assert.Same(radius.Right, R(radial[2]).Left);
        Assert.Same(R(offset.Right).Left, radius);
        Assert.Same(whole.Left, R(radial[2]).Right);
        Assert.Same(whole.Right, closure.Left); Assert.Same(whole.Left, closure.Right);
        Assert.NotSame(whole.Left, whole.Right); Number(whole.Left, 0); Number(whole.Right, 1);
        Assert.True(Point(radius.Right).Carrier.TryReadLinearOrigin(out var radialOrigin));
        Assert.True(Point(whole.Left).Carrier.TryReadCircularOrigin(out var contourOrigin));
        Assert.Same(Point(source).Carrier, radialOrigin.Carrier);
        Assert.Same(radialOrigin.Carrier, contourOrigin.Carrier);
    }

    [Theory]
    [InlineData(4, 1, 1, 4, 0)]
    [InlineData(10, 1, 1, 10, 0)]
    [InlineData(360, 90, 1, 4, 0)]
    [InlineData(3600, 900, 1, 4, 0)]
    [InlineData(3600, 637, 637, 3600, 0)]
    [InlineData(4, 4, 0, 1, 1)]
    [InlineData(4, -1, 3, 4, -1)]
    [InlineData(4, -4, 0, 1, -1)]
    [InlineData(long.MaxValue, long.MinValue, long.MaxValue - 1, long.MaxValue, -2)]
    public void WholeSubdivisionCreatesNativeLandmarksAndRadialsWithoutACoordinateGrid(long parts, long tick, long n, long d, long cycles)
    {
        var f = new Fixture(2); var partition = f.Partition(parts);
        Assert.True(ContourConstruction.TryReadPartition(partition, out _));
        var read = f.Locate(partition, tick); var result = F(Result(read));
        Number(result[2], n, d); Number(result[3], cycles);
        var captured = F(F(read)[4])[0]; var capturedCircle = F(F(captured)[0])[0];
        var originalRadial = F(capturedCircle)[1]; var originalRadius = F(originalRadial)[1];
        var newRadial = F(result[1]); var correspondence = R(F(F(read)[4])[1]);
        Assert.Same(originalRadius, correspondence.Left); Assert.Same(newRadial[1], correspondence.Right);
        Assert.Same(result[0], R(newRadial[2]).Right);
        Number(R(newRadial[1]).Left, 0); Number(R(newRadial[1]).Right, 1);
        Assert.All(f.Circle.Carriers(), c => { Assert.Equal(0UL, c.Value); Assert.Equal(0, c.Tension); });
        var restored = F(Result(Restore(read, 2)));
        Assert.Same(restored[0], R(F(restored[1])[2]).Right);
        if (n == 0)
        {
            Assert.Same(R(F(capturedCircle)[2]).Left, result[0]);
            Assert.Same(originalRadial, result[1]);
        }
        else Assert.NotSame(Point(R(originalRadius).Left).Carrier, Point(R(newRadial[1]).Left).Carrier);
    }

    [Theory]
    [InlineData("missing_closure")]
    [InlineData("lookalike_return")]
    [InlineData("unattached_radius")]
    [InlineData("unbound_reach")]
    [InlineData("foreign_source")]
    [InlineData("zero_radius")]
    public void FalseCircleClaimsRetainTheirParticipants(string fault)
    {
        var f = new Fixture(); var fields = F(f.Circle); var radius = R(F(fields[1])[1]);
        switch (fault)
        {
            case "missing_closure": fields[3] = new Placeholder(); break;
            case "lookalike_return": fields[3] = new Relation(R(fields[2]).Right, Relation.Point(f.Contour, 0)); break;
            case "unattached_radius":
                var radial = F(fields[1]); radial[2] = new Relation(radius.Right, Relation.Point(f.Contour, 0)); fields[1] = Group(radial); break;
            case "unbound_reach": fields[0] = f.Raw[2]; break;
            case "foreign_source": fields[0] = new Relation(Relation.OrthogonalOutlet(Relation.Point(new Carrier(), 0)), R(fields[0]).Right); break;
            case "zero_radius": Point(radius.Right).Tick.Set(0); break;
        }
        var candidate = Group(fields);
        Assert.False(ContourConstruction.TryReadCircle(candidate, out var read));
        Assert.IsType<Placeholder>(Result(read)); Assert.Same(candidate, F(F(read)[3])[0]);
    }

    [Fact]
    public void SegmentOffsetAndBareCircularPotentialDoNotSupplyTheCircleConstruction()
    {
        var f = new Fixture();
        Assert.False(ContourConstruction.TryReadCircle(R(f.Raw[1]), out _));
        Assert.False(ContourConstruction.TryReadOffset(new Relation(
            Relation.OrthogonalOutlet(new Relation(f.Roots[0], f.Roots[0])), R(f.Raw[2]).Right), out _));
        Assert.False(ContourConstruction.TryReadCircle(Group(f.Contour), out _));
    }

    [Fact]
    public void SharedTickAliasesRetainAttachmentAndClosureAcrossDistinctPointRelations()
    {
        var f = new Fixture(); var circle = F(f.Circle); var radial = F(circle[1]); var radius = R(radial[1]);
        Relation Alias(IElement e) { var p = Point(e); return new Relation(new CarrierLocation(p.Carrier, p.Tick), p.Carrier); }
        radial[0] = new Relation(Alias(R(radial[0]).Left), Alias(radius.Left));
        radial[2] = new Relation(Alias(radius.Right), Alias(R(radial[2]).Right)); circle[1] = Group(radial);
        var whole = R(circle[2]); circle[2] = new Relation(Alias(whole.Left), Alias(whole.Right));
        circle[3] = new Relation(Alias(whole.Right), Alias(whole.Left));
        Assert.True(ContourConstruction.TryReadCircle(Restore(Group(circle), 2), out var read));
        var actual = F(Result(read)); var start = R(actual[2]).Left; var returned = R(actual[3]).Right;
        Assert.NotSame(start, returned); Assert.True(Point(start).SameTickRefAs(Point(returned)));
    }

    [Fact]
    public void EqualTickCountsAndCompatibleUnitsCannotReplaceAWholeCorrespondence()
    {
        var f = new Fixture(); var p = F(f.Partition(4)); var map = R(p[1]);
        Assert.False(ContourConstruction.TryReadPartition(Group(p[0], new Relation(map.Left, Relation.Segment(f.Contour, 0, 1))), out _));
        var wrongUnits = new Carrier();
        Assert.False(ContourConstruction.TryReadPartition(Group(p[0], new Relation(Relation.Segment(wrongUnits, 0, 4), map.Right)), out _));
        var wrongCount = new Carrier(UnitShape.SubdivisionOf(f.Contour.UnitShape, 10));
        Assert.False(ContourConstruction.TryReadPartition(Group(p[0], new Relation(Relation.Segment(wrongCount, 0, 4), map.Right)), out _));
        var fine = Point(R(map.Left).Left).Carrier;
        Assert.False(ContourConstruction.TryLocate(Group(Group(p), Relation.Point(new Carrier(fine.UnitShape), 1)), out _));
    }

    [Fact]
    public void TheSameCoarseLandmarkCanBeSelectedFromSeveralFinerConstructions()
    {
        var f = new Fixture(2);
        var readings = new[] { (4, 1), (360, 90), (3600, 900) }.Select(p => f.Locate(f.Partition(p.Item1), p.Item2)).ToArray();
        foreach (var read in readings) Number(F(Result(read))[2], 1, 4);
        Assert.NotSame(F(Result(readings[0]))[0], F(Result(readings[1]))[0]);
        // Corresponding contour positions are a valid read, not raw tick-reference identity.
        var fineRequest = F(F(F(readings[2])[4])[0])[1]; Number(fineRequest, 900);
    }

    [Fact]
    public void ARecordedConstructionDoesNotAcquireLaterRadiusOrPartitionEdits()
    {
        var f = new Fixture(); var partition = f.Partition(4); var request = f.Request(partition, 1);
        Assert.True(ContourConstruction.TryLocate(request, out var read));
        Point(R(F(F(f.Circle)[1])[1]).Right).Tick.Set(7);
        Point(F(request)[1]).Tick.Set(3);
        Assert.True(ContourConstruction.TryLocate(request, out var later));
        Number(F(Result(later))[2], 3, 4);
        Number(R(F(F(Result(later))[1])[1]).Right, 7);
        Number(F(Result(read))[2], 1, 4);
        var storedRequest = R(F(F(read)[4])[0]);
        Assert.True(ContourConstruction.TryLocate(storedRequest, out var replay));
        Number(F(Result(replay))[2], 1, 4);
        Number(R(F(F(Result(replay))[1])[1]).Right, 1);
    }

    [Fact]
    public void EveryAuthoredLandmarkRequestSurvivesRenamingAndNumericTransport()
    {
        var source = Regex.Replace(Source(), @"([$?])([a-zA-Z_][a-zA-Z0-9_]*)", "$1renamed_$2");
        var f = new Fixture(2, source);
        foreach (var request in f.Roots.Skip(9)) Assert.True(ContourConstruction.TryLocate(R(request), out _));
    }

    [Fact]
    public void TheDemoShowsTheNativeConstructionAndActualAuthoredPartitionReads()
    {
        var html = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Gallery", "contour-rotation.html"));
        using var trace = JsonDocument.Parse(Regex.Match(html,
            "<script id=\"trace\" type=\"application/json\">([\\s\\S]*?)</script>").Groups[1].Value);
        Assert.Equal(12, trace.RootElement.GetArrayLength());
        var order = new List<string>();
        foreach (var row in trace.RootElement.EnumerateArray())
        {
            var f = new Fixture(2); var kind = row.GetProperty("read").GetString()!; order.Add(kind);
            if (kind == "locate")
            {
                var index = row.GetProperty("parts").GetInt32() switch { 4 => 5, 10 => 6, 360 => 7, 3600 => 8, _ => throw new Exception() };
                var read = f.Locate(R(f.Roots[index]), row.GetProperty("tick").GetInt64());
                Number(F(Result(read))[2], row.GetProperty("n").GetInt64(), row.GetProperty("d").GetInt64());
                Number(F(Result(read))[3], row.GetProperty("cycles").GetInt64());
            }
            else
            {
                Assert.Equal(JsonValueKind.Null, row.GetProperty("n").ValueKind);
                switch (kind)
                {
                    case "point": Number(f.Raw[0], 0); break;
                    case "outlet": Assert.Same(f.Raw[0], R(f.Raw[1]).Left); break;
                    case "offset":
                        Assert.True(ContourConstruction.TryReadOffset(R(f.Raw[2]), out var open));
                        Assert.Single(F(F(open)[3])); break;
                    case "radius":
                        var radial = F(F(f.Circle)[1]); Assert.Same(f.Roots[0], R(radial[0]).Left);
                        Assert.True(Point(R(radial[1]).Right).Carrier.TryReadLinearOrigin(out _)); break;
                    case "circle": Assert.True(ContourConstruction.TryReadCircle(f.Circle, out _)); break;
                    default: throw new Exception("Unknown demo stage");
                }
            }
        }
        Assert.Equal(new[] { "point", "outlet", "offset", "radius", "circle" }, order.Take(5));
        var syntax = System.Net.WebUtility.HtmlDecode(Regex.Match(html,
            "<pre id=\"syntax\">([\\s\\S]*?)</pre>").Groups[1].Value);
        Assert.NotEmpty(syntax);
        foreach (var line in syntax.Split('\n').Where(line => !string.IsNullOrWhiteSpace(line))) Assert.Contains(line.Trim(), Source());
    }

    private static string Source() => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "Syntax", "circle-construction.gojo"));
    private static Relation R(IElement e) => Assert.IsType<Relation>(e);
    private static IElement[] F(IElement e) { Assert.True(R(e).TryReadOrderedGroup(out var fields)); return fields; }
    private static IElement Result(IElement e) => F(e)[2];
    private static Relation Group(params IElement[] elements) => Relation.OrderedGroup(new Carrier(), elements);
    private static CarrierLocation Point(IElement e) => Assert.IsType<CarrierLocation>(R(e).Left);
    private static void Number(IElement e, long n, long d = 1)
    {
        if (e is Relation { Left: CarrierLocation p, Right: Carrier }) e = p;
        Assert.True(ProjectionResidue.TryRead(e, out _, out var actualN, out var actualD));
        Assert.Equal(new BigInteger(n) * actualD, new BigInteger(actualN) * d);
    }
    private static Relation Restore(IElement e, int times)
    { for (var i = 0; i < times; i++) e = Assert.Single(GojoNumericFormat.Decode(GojoNumericFormat.Encode(e))); return R(e); }
    private sealed class Fixture
    {
        internal readonly IElement[] Raw, Roots;
        internal Relation Circle => R(Roots[4]);
        internal Carrier Contour => Point(R(F(Circle)[2]).Left).Carrier;
        internal Fixture(int restores = 0, string? source = null)
        {
            var restored = Restore(Group(GojoSyntax.Parse(source ?? Source()).Roots.ToArray()), restores);
            Raw = F(restored); Roots = F(StructuralPattern.Refine(restored, R(Raw[3])));
        }
        internal Relation Partition(long parts)
        {
            var fine = new Carrier(UnitShape.SubdivisionOf(Contour.UnitShape, parts));
            return Group(Circle, new Relation(Relation.Segment(fine, 0, parts), F(Circle)[2]));
        }
        internal Relation Request(Relation partition, long tick) => Group(partition,
            Relation.Point(Point(R(R(F(partition)[1]).Left).Left).Carrier, tick));
        internal Relation Locate(Relation partition, long tick)
        { Assert.True(ContourConstruction.TryLocate(Request(partition, tick), out var read)); return read; }
    }
}
