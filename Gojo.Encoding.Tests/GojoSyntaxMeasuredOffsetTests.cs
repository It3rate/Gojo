using System.Numerics;
using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using Gojo.Core;
using Gojo.Encoding.Syntax;
using Gojo.Patterns;
using Gojo.Structures;

namespace Gojo.Encoding.Tests;

public sealed class GojoSyntaxMeasuredOffsetTests
{
    [Theory]
    [InlineData(0)] [InlineData(1)] [InlineData(2)]
    public void PreparationRetainsTheNormalEquationAndBothDependentEndpointRecipes(int restores)
    {
        var f = new Fixture(restores); var result = F(Result(f.Prepared)); var equation = F(result[1]);
        Vector(result[0], -1, 1); Number(R(equation[2]).Left, 2); Number(R(equation[2]).Right, 4);
        var scale = Assert.IsType<Placeholder>(R(equation[1]).Left); Assert.Same(scale, R(equation[1]).Right);
        var first = R(F(result[2])[0]); var second = R(F(result[2])[1]);
        var firstInputs = F(first.Left); var secondInputs = F(second.Left);
        Assert.Same(firstInputs[2], secondInputs[2]); Assert.Same(scale, F(firstInputs[2])[0]);
        Assert.Same(f.Whole, F(firstInputs[2])[1]);
        var support = R(F(firstInputs[2])[2]); Number(support.Left, 0); Number(support.Right, 2);
        Assert.Same(first.Right, R(result[3]).Left); Assert.Same(second.Right, R(result[3]).Right);
        Vector(firstInputs[1], -1, 1); Vector(secondInputs[1], 0, 2);
        Assert.False(SegmentOffset.TryConstruct(R(F(f.Roots[0])[0]), out _));
        // These are existing affine requests. Bind one exploratory scale and
        // support without claiming it satisfies the still-attached square law.
        var axis = Location(R(f.Whole).Left).Carrier;
        var bindings = new[] { new Relation(scale, new CarrierLocation(axis, 1)) };
        Assert.True(AffineFrame.TryInterpolate(R(StructuralPattern.Refine(first, bindings)), out var firstRead));
        Assert.True(AffineFrame.TryInterpolate(R(StructuralPattern.Refine(second, bindings)), out var secondRead));
        Vector(Result(firstRead), -1, 1); Vector(Result(secondRead), 0, 2);
        Assert.False(AffineFrame.TryInterpolate(R(StructuralPattern.Refine(first,
            new Relation(scale, new CarrierLocation(axis, -1)))), out _));
        // Preparation and its bound observation restore with the same aliases.
        var restored = Restore(f.Prepared, 2); var re = F(Result(restored));
        Assert.Same(R(F(re[1])[1]).Left, F(F(R(F(re[2])[0]).Left)[2])[0]);
        Assert.All(f.Prepared.Carriers(), c => { Assert.Equal(0UL, c.Value); Assert.Equal(0, c.Tension); });
    }

    [Theory]
    [InlineData(1, 1, 2)] [InlineData(10, 14, 15)] [InlineData(100, 141, 142)] [InlineData(1000, 1414, 1415)]
    public void ExplicitResolutionEnclosesTheRootAndSharedOffsetCancelsExactly(long parts, long lo, long hi)
    {
        var f = new Fixture(1); var read = f.Observe(parts); var result = F(Result(read));
        var sample = F(result[0]); Assert.IsType<Placeholder>(sample[0]);
        Number(R(sample[1]).Left, lo); Number(R(sample[1]).Right, hi);
        // Independent integer inequalities for sqrt(2), not the reader's formula.
        Assert.True(new BigInteger(lo) * lo < 2 * new BigInteger(parts) * parts);
        Assert.True(new BigInteger(hi) * hi > 2 * new BigInteger(parts) * parts);
        var bounds = R(result[2]);
        Bounds(F(bounds.Left)[0], -hi, -lo, parts); Bounds(F(bounds.Left)[1], lo, hi, parts);
        Bounds(F(bounds.Right)[0], parts - hi, parts - lo, parts);
        Bounds(F(bounds.Right)[1], parts + lo, parts + hi, parts);
        AssertDisplacement(read, 1, 1);
        var restored = Restore(read, 2); AssertDisplacement(restored, 1, 1);
        var expressions = R(F(Result(restored))[1]); var shared = F(Result(restored))[0];
        foreach (var point in new[] { expressions.Left, expressions.Right })
            foreach (var lane in F(point)) Assert.Same(shared, R(F(F(lane)[1])[0]).Left);
        // The mapped sample remains linked to the original constrained scale.
        var provenance = F(F(restored)[4]); var equation = F(provenance[2]);
        Assert.Same(R(equation[1]).Left, R(R(provenance[1]).Right).Right);
        var outputSlots = R(F(Result(provenance[0]))[3]); var outputReads = F(provenance[3]);
        Assert.Same(outputSlots.Left, R(outputReads[0]).Left); Assert.Same(outputSlots.Right, R(outputReads[1]).Left);
        Assert.Same(expressions.Left, R(outputReads[0]).Right); Assert.Same(expressions.Right, R(outputReads[1]).Right);
    }

    [Fact]
    public void FinerEnclosuresRefineTheSameCapturedConstructionWithoutRewritingTheCoarseRead()
    {
        var f = new Fixture(); var coarse = f.Observe(10); var fine = f.Observe(100);
        Bounds(F(R(F(Result(coarse))[2]).Left)[1], 14, 15, 10);
        Bounds(F(R(F(Result(fine))[2]).Left)[1], 141, 142, 100);
        // Live source changes require another preparation; both previous reads
        // and further reads of the old preparation preserve its captured law.
        var offset = F(F(f.Roots[0])[0]); var calibration = R(R(offset[2]).Right);
        Location(R(calibration.Right).Right).Tick.Set(3);
        Bounds(F(R(F(Result(f.Observe(10)))[2]).Left)[1], 14, 15, 10);
        Assert.True(SegmentOffset.TryPrepare(R(f.Roots[0]), out var changed));
        var changedRead = Observe(changed, 10);
        Bounds(F(R(F(Result(changedRead))[2]).Left)[1], 21, 22, 10);
        Bounds(F(R(F(Result(coarse))[2]).Left)[1], 14, 15, 10);
    }

    [Theory]
    [InlineData(0, 5, 0, 0)] [InlineData(2, 5, 2, 2)] [InlineData(2, 1, 0, 1)]
    public void ExactGridSitesCollapseWhileACoarseObservationCanKeepDetailOpen(long distance, long parts, long lo, long hi)
    {
        var f = new Fixture(); var setup = F(f.Roots[0]); var offset = F(setup[0]); var chart = new Chart(offset[1]);
        var a = R(offset[0]).Left; var edge = new Relation(a, chart.Point(3, 4));
        offset[0] = edge; offset[2] = new Relation(Relation.OrthogonalOutlet(edge),
            new Relation(R(R(offset[2]).Right).Left, Relation.Segment(chart.X, 0, distance)));
        Assert.True(SegmentOffset.TryPrepare(Group(Group(offset), setup[1]), out var prepared));
        var read = Observe(prepared, parts); var sample = F(F(Result(read))[0]);
        Number(R(sample[1]).Left, lo); Number(R(sample[1]).Right, hi);
        if (lo == hi)
        {
            Number(sample[0], lo);
            Bounds(F(R(F(Result(read))[2]).Left)[0], -4 * distance, -4 * distance, 5);
            Bounds(F(R(F(Result(read))[2]).Left)[1], 3 * distance, 3 * distance, 5);
        }
        else Assert.IsType<Placeholder>(sample[0]);
        AssertDisplacement(read, 3, 4);
    }

    [Theory]
    [InlineData(1)] [InlineData(-1)]
    public void AZeroComponentStaysExactInASkewChartWithEitherPhysicalSide(int side)
    {
        var f = new Fixture(); var setup = F(f.Roots[0]); var offset = F(setup[0]); var chart = new Chart(offset[1]);
        // x'=x+y, y'=y sends (-1,1) to (0,1). Pairing becomes (1,-1,2).
        var a = R(offset[0]).Left; var edge = new Relation(a, chart.Point(2, 1));
        offset[0] = edge; offset[1] = chart.Metric(1, -1, 2);
        offset[2] = new Relation(Relation.OrthogonalOutlet(edge), R(offset[2]).Right);
        offset[3] = new Relation(a, chart.Point(-side, 0));
        Assert.True(SegmentOffset.TryPrepare(Group(Group(offset), setup[1]), out var prepared));
        Vector(F(Result(prepared))[0], 0, side);
        var read = Observe(prepared, 100); var bounds = R(F(Result(read))[2]);
        Bounds(F(bounds.Left)[0], 0, 0); Bounds(F(bounds.Right)[0], 2, 2);
        Bounds(F(bounds.Left)[1], side > 0 ? 141 : -142, side > 0 ? 142 : -141, 100);
        AssertDisplacement(read, 2, 1);
    }

    [Fact]
    public void SeparateReadSamplesDoNotAcquireExactDependencyByHavingSimilarBounds()
    {
        var f = new Fixture(); var a = f.Observe(10); var b = f.Observe(10);
        var first = F(R(F(Result(a))[1]).Left)[0]; var second = F(R(F(Result(b))[1]).Right)[0];
        var difference = Difference(first, second);
        Bounds(difference, 9, 11, 10);
        // Within either read, the same actual cause still cancels exactly.
        AssertDisplacement(a, 1, 1); AssertDisplacement(b, 1, 1);
    }

    [Theory]
    [InlineData("normal")] [InlineData("square")] [InlineData("factor-identity")]
    [InlineData("probe")] [InlineData("grade")] [InlineData("output-identity")]
    [InlineData("whole-lookalike")] [InlineData("wrong-unit")]
    [InlineData("wrong-fine-span")] [InlineData("wrong-holder")]
    [InlineData("negative-support")] [InlineData("too-small-support")]
    public void ASerializedPreparationIsCheckedBeforeItsClaimedResultsAreConsumed(string fault)
    {
        var f = new Fixture(); var w = F(f.Prepared); var result = F(w[2]); var equation = F(result[1]);
        var axis = Location(R(f.Whole).Left).Carrier; var chart = new Chart(F(F(w[1])[0])[1]);
        var map = new Relation(Relation.Segment(new Carrier(UnitShape.SubdivisionOf(axis.UnitShape, 10)), 0, 10), f.Whole);
        switch (fault)
        {
            case "normal": result[0] = chart.Point(1, 1); break;
            case "square": equation[2] = new Relation(R(equation[2]).Left, new CarrierLocation(chart.Square, 9)); result[1] = Group(equation); break;
            case "factor-identity": equation[1] = new Relation(R(equation[1]).Left, new Placeholder()); result[1] = Group(equation); break;
            case "probe":
                var rs = F(result[2]); var recipe = R(rs[0]); var inputs = F(recipe.Left); inputs[1] = chart.Point(-2, 1);
                rs[0] = new Relation(Group(inputs), recipe.Right); result[2] = Group(rs); break;
            case "grade":
                var recipes = F(result[2]); var one = R(recipes[0]); var ports = F(one.Left); var grade = F(ports[2]);
                grade[0] = new Placeholder(); ports[2] = Group(grade); recipes[0] = new Relation(Group(ports), one.Right); result[2] = Group(recipes); break;
            case "output-identity": result[3] = new Relation(new Placeholder(), R(result[3]).Right); break;
            case "whole-lookalike": map = new Relation(map.Left, Relation.Segment(axis, 0, 1)); break;
            case "wrong-unit": map = new Relation(Relation.Segment(new Carrier(), 0, 10), f.Whole); break;
            case "wrong-fine-span": map = new Relation(Relation.Segment(Location(R(map.Left).Left).Carrier, 0, 9), f.Whole); break;
            case "wrong-holder": result[0] = Group(F(result[0])); break;
            case "negative-support":
            case "too-small-support":
                var both = F(result[2]); var sharedGrade = F(F(R(both[0]).Left)[2]);
                sharedGrade[2] = Relation.Segment(axis, fault == "negative-support" ? -2 : 0, fault == "negative-support" ? 2 : 1);
                var newGrade = Group(sharedGrade);
                for (var i = 0; i < 2; i++)
                {
                    var old = R(both[i]); var args = F(old.Left); args[2] = newGrade;
                    both[i] = new Relation(Group(args), old.Right);
                }
                result[2] = Group(both); break;
        }
        w[2] = Group(result); var request = R(Restore(Group(Group(w), map), 2));
        Assert.False(SegmentOffset.TryObserveAtResolution(request, out var rejected));
        Assert.IsType<Placeholder>(Result(rejected)); Assert.NotEmpty(F(F(rejected)[3]));
    }

    [Fact]
    public void RefinementStorageLimitsStayUnresolvedWithoutRoundingOrWrapping()
    {
        var f = new Fixture(); var axis = Location(R(f.Whole).Left).Carrier;
        var fine = new Carrier(UnitShape.SubdivisionOf(axis.UnitShape, long.MaxValue));
        var request = Group(f.Prepared, new Relation(Relation.Segment(fine, 0, long.MaxValue), f.Whole));
        Assert.False(SegmentOffset.TryObserveAtResolution(request, out var read));
        Assert.IsType<Placeholder>(Result(read)); Assert.NotEmpty(F(F(read)[3]));
        Assert.IsType<Relation>(F(read)[4]); // Prepared equation remains available.
    }

    [Fact]
    public void WalkthroughSyntaxAndEveryDisplayedBoundMatchTheExecutableConstruction()
    {
        var html = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Gallery", "measured-offsets.html"));
        var syntaxMatch = Regex.Match(html, "<pre id=\"syntax\">(.*?)</pre>", RegexOptions.Singleline);
        Assert.True(syntaxMatch.Success);
        var syntax = WebUtility.HtmlDecode(syntaxMatch.Groups[1].Value).Replace("\r\n", "\n").Trim();
        var authored = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "Syntax", "measured-offset-refinement.gojo"));
        Assert.Equal(authored.Replace("\r\n", "\n").Trim(), syntax);
        Assert.Equal(5, GojoSyntax.Parse(syntax).Roots.Count);
        var traceMatch = Regex.Match(html, "<script id=\"trace\" type=\"application/json\">(.*?)</script>", RegexOptions.Singleline);
        using var trace = JsonDocument.Parse(traceMatch.Groups[1].Value);
        var f = new Fixture(); var json = trace.RootElement; var prepared = F(Result(f.Prepared));
        var edge = R(F(F(f.Roots[0])[0])[0]);
        Vector(edge.Left, json.GetProperty("source")[0][0].GetInt64(), json.GetProperty("source")[0][1].GetInt64());
        Vector(edge.Right, json.GetProperty("source")[1][0].GetInt64(), json.GetProperty("source")[1][1].GetInt64());
        Vector(prepared[0], json.GetProperty("normal")[0].GetInt64(), json.GetProperty("normal")[1].GetInt64());
        Number(R(F(prepared[1])[2]).Left, json.GetProperty("normalSquared").GetInt64());
        Number(R(F(prepared[1])[2]).Right, json.GetProperty("distanceSquared").GetInt64());
        foreach (var row in json.GetProperty("reads").EnumerateArray())
        {
            var read = f.Observe(row.GetProperty("parts").GetInt64()); var result = F(Result(read));
            var support = R(F(result[0])[1]); Number(support.Left, row.GetProperty("count")[0].GetInt64()); Number(support.Right, row.GetProperty("count")[1].GetInt64());
            var actual = R(result[2]);
            foreach (var (point, key) in new[] { (actual.Left, "a"), (actual.Right, "b") })
                for (var axis = 0; axis < 2; axis++)
                {
                    Assert.True(BoundedQuantity.TryReadBounds(R(F(point)[axis]), out _, out var lower, out var upper, out _));
                    var expected = row.GetProperty(key)[axis];
                    Number(lower, expected[0][0].GetInt64(), expected[0][1].GetInt64());
                    Number(upper, expected[1][0].GetInt64(), expected[1][1].GetInt64());
                }
            AssertDisplacement(read, row.GetProperty("difference")[0].GetInt64(), row.GetProperty("difference")[1].GetInt64());
        }
    }

    private sealed class Fixture
    {
        public IElement[] Roots { get; }
        public Relation Prepared { get; }
        public IElement Whole => F(F(Prepared)[1])[1];
        public Fixture(int restores = 0)
        {
            Roots = GojoSyntax.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "Syntax", "measured-offset-refinement.gojo"))).Roots.ToArray();
            for (var i = 0; i < restores; i++) Roots = GojoNumericFormat.Decode(GojoNumericFormat.Encode(Roots));
            Assert.True(SegmentOffset.TryPrepare(R(Roots[0]), out var prepared)); Prepared = prepared;
        }
        public Relation Observe(long parts)
        {
            var root = parts switch { 1 => 1, 10 => 2, 100 => 3, 1000 => 4, _ => throw new ArgumentException() };
            var f = F(Roots[root]);
            var request = R(StructuralPattern.Refine(Roots[root], new Relation(f[0], Prepared), new Relation(R(f[1]).Right, Whole)));
            Assert.True(SegmentOffset.TryObserveAtResolution(request, out var read)); return read;
        }
    }
    private sealed class Chart
    {
        public Carrier X { get; } public Carrier Y { get; } public Carrier Square { get; }
        private readonly IElement basis; private readonly IElement[] units; private readonly Carrier holder;
        public Chart(IElement metric)
        {
            var f = F(metric); basis = f[0]; units = F(basis); holder = (Carrier)R(basis).Right;
            X = Location(R(units[0]).Left).Carrier; Y = Location(R(units[1]).Left).Carrier;
            Assert.True(ProjectionResidue.TryRead(R(F(f[1])[0]).Right, out var square, out _, out _)); Square = square;
        }
        public Relation Point(long x, long y) => Relation.OrderedGroup(holder, new CarrierLocation(X, x), new CarrierLocation(Y, y));
        public Relation Metric(long xx, long xy, long yy) => Group(basis, Group(
            new Relation(new Relation(units[0], units[0]), new CarrierLocation(Square, xx)),
            new Relation(new Relation(units[0], units[1]), new CarrierLocation(Square, xy)),
            new Relation(new Relation(units[1], units[1]), new CarrierLocation(Square, yy))));
    }
    private static Relation Observe(Relation prepared, long parts)
    {
        var whole = F(F(prepared)[1])[1]; var axis = Location(R(whole).Left).Carrier;
        var fine = new Carrier(UnitShape.SubdivisionOf(axis.UnitShape, parts));
        Assert.True(SegmentOffset.TryObserveAtResolution(Group(prepared, new Relation(Relation.Segment(fine, 0, parts), whole)), out var read));
        return read;
    }
    private static Relation Difference(IElement first, IElement second)
    {
        Assert.True(ProjectionResidue.TryRead(F(first)[0], out var axis, out _, out _));
        var negative = new Relation(Relation.Segment(axis, 0, 1), Relation.Segment(axis, 0, -1));
        var positive = new Relation(Relation.Segment(axis, 0, 1), Relation.Segment(axis, 0, 1));
        Assert.True(BoundedQuantity.TryCombine(new Relation(Group(new Relation(first, negative), new Relation(second, positive)), Relation.Point(axis, 1)), out var read));
        return read;
    }
    private static void AssertDisplacement(IElement witness, long x, long y)
    {
        var edge = R(F(Result(witness))[1]);
        Bounds(Difference(F(edge.Left)[0], F(edge.Right)[0]), x, x);
        Bounds(Difference(F(edge.Left)[1], F(edge.Right)[1]), y, y);
    }
    private static void Bounds(IElement read, long lo, long hi, long d = 1)
    {
        Assert.True(BoundedQuantity.TryReadBounds(R(read), out _, out var lower, out var upper, out _));
        Number(lower, lo, d); Number(upper, hi, d);
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
        if (e is Relation { Left: CarrierLocation p, Right: Carrier }) e = p;
        Assert.True(ProjectionResidue.TryRead(e, out _, out var actual, out var divisor));
        Assert.Equal(new BigInteger(n) * divisor, new BigInteger(actual) * d);
    }
    private static void Vector(IElement e, long x, long y) { Number(F(e)[0], x); Number(F(e)[1], y); }
}
