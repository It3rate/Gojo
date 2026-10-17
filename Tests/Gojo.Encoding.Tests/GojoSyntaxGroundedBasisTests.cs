using System.Numerics;
using System.Text.Json;
using System.Text.RegularExpressions;
using Gojo.Core;
using Gojo.Encoding.Syntax;
using Gojo.Patterns;
using Gojo.Structures;

namespace Gojo.Encoding.Tests;

public sealed class GojoSyntaxGroundedBasisTests
{
    [Fact]
    public void WalkthroughTraceMatchesKernelReadsForBothConventionsAndPartialControls()
    {
        var html = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Gallery", "grounded-affine-basis.html"));
        var match = Regex.Match(html, "<script id=\"trace\" type=\"application/json\">(.*?)</script>", RegexOptions.Singleline);
        Assert.True(match.Success);
        using var trace = JsonDocument.Parse(match.Groups[1].Value);
        var f = new Fixture(2);
        var samples = trace.RootElement.GetProperty("samples").EnumerateArray().ToArray();
        Assert.Equal(18, samples.Length);
        Assert.Equal(18, samples.Select(r => (r.GetProperty("native").GetInt64(), r.GetProperty("opposite").GetBoolean())).Distinct().Count());
        foreach (var row in samples)
        {
            var basis = row.GetProperty("opposite").GetBoolean() ? f.Opposite : f.Basis;
            var input = Group(new CarrierLocation(f.Line, 3), new CarrierLocation(f.Cross, row.GetProperty("native").GetInt64()));
            Assert.True(AffineBasisConstruction.TryLocate(Group(basis, input), out var read));
            AssertTracePoint(Result(read), row.GetProperty("point"));
        }
        var curves = trace.RootElement.GetProperty("curves").EnumerateArray().ToArray();
        Assert.Equal(30, curves.Length);
        Assert.Equal(30, curves.Select(r => (r.GetProperty("tick").GetInt64(), r.GetProperty("opposite").GetBoolean(), r.GetProperty("mode").GetString())).Distinct().Count());
        foreach (var row in curves)
        {
            var basis = row.GetProperty("opposite").GetBoolean() ? f.Opposite : f.Basis;
            var mode = row.GetProperty("mode").GetString();
            Assert.Contains(mode, new[] { "complete", "partial", "missing" });
            var (_, reads) = f.Curve(row.GetProperty("tick").GetInt64(), basis, mode == "partial", mode == "missing");
            var nodes = row.GetProperty("nodes").EnumerateArray().ToArray();
            Assert.Equal(reads.Count, nodes.Length);
            for (var i = 0; i < nodes.Length; i++) AssertTracePoint(Result(reads[i]), nodes[i]);
        }
    }

    private static void AssertTracePoint(IElement point, JsonElement encoded)
    {
        var coordinates = F(point); var expected = encoded.EnumerateArray().ToArray();
        Assert.Equal(coordinates.Length, expected.Length);
        for (var i = 0; i < coordinates.Length; i++)
        {
            if (expected[i].ValueKind == JsonValueKind.Null) Assert.IsType<Placeholder>(coordinates[i]);
            else { Assert.Equal(2, expected[i].GetArrayLength()); Number(coordinates[i], expected[i][0].GetInt64(), expected[i][1].GetInt64()); }
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void ConnectedLegsCrystallizeIntoWholeBasedCoordinatesWithRetainedCalibration(int restores)
    {
        var f = new Fixture(restores);
        var grounded = F(f.Basis); var foundation = F(grounded[0]); var units = F(grounded[1]); var maps = F(grounded[2]);
        Assert.Same(foundation[0], R(maps[0]).Left);
        Assert.Same(R(foundation[1]).Right, R(maps[1]).Left);
        Assert.Same(units[0], R(maps[0]).Right); Assert.Same(units[1], R(maps[1]).Right);
        Assert.NotSame(f.Foundation, grounded[0]);
        Number(R(R(maps[0]).Left).Right, 6);
        Number(R(R(maps[1]).Left).Right, 4);
        Vector(Result(f.Locate(2)), (0,1), (0,1));
        Vector(Result(f.Locate(3)), (1,1), (0,1));
        Vector(Result(f.Locate(4)), (0,1), (1,1));
        Vector(Result(f.Locate(5)), (1,1), (1,1));
        Vector(Result(f.Locate(6)), (1,2), (1,2));
        Assert.True(AffineFrame.TryDifference(Group(Result(f.Locate(4)), Result(f.Locate(5)), f.Basis), out var translated));
        Vector(Result(translated), (1,1), (0,1));

        // The complete basis retains cross-space origin correspondence after transport.
        var restored = F(Restore(f.Basis, 2)); var source = F(restored[0]);
        Assert.Same(source[0], R(R(source[1]).Left).Left);
        Assert.True(Location(R(source[0]).Left).SameTickRefAs(Location(R(source[2]).Left)));
        Assert.True(Location(R(R(source[1]).Right).Left).SameTickRefAs(Location(R(source[2]).Right)));
        Assert.All(f.Foundation.Carriers(), c => { Assert.Equal(0UL, c.Value); Assert.Equal(0, c.Tension); });
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void SixEdgeBezierReusesJustifiedBasisAndAgreesWithIndependentPolynomial(int restores)
    {
        var f = new Fixture(restores);
        foreach (var tick in new long[] { 0, 1, 137, 250, 500, 863, 999, 1000 })
        foreach (var opposite in new[] { false, true })
        {
            var basis = opposite ? f.Opposite : f.Basis;
            var (point, reads) = f.Curve(tick, basis);
            var p = F(point);
            Polynomial(p[0], [0,0,1,1], tick);
            Polynomial(p[1], opposite ? [0,-1,-1,0] : [0,1,1,0], tick);
            Assert.Equal(6, reads.Count);
            Assert.All(reads, read => Assert.Empty(F(F(read)[3])));
            Assert.Same(Result(reads[3]), F(R(F(reads[^1])[0]).Left)[0]);
            Assert.Same(basis, F(F(R(F(reads[^1])[0]).Left)[2])[3]);
            var packet = F(Restore(Group(point, Group([.. reads])), 2));
            Assert.Same(packet[0], Result(F(packet[1])[^1]));
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void OrthogonalParticipationAndOppositeConventionDoNotChangeTheFirstRead(int restores)
    {
        var f = new Fixture(restores);
        foreach (var cross in new long[] { -8, -4, 0, 2, 4, 8 })
        {
            var sample = Group(new CarrierLocation(f.Line, 3), new CarrierLocation(f.Cross, cross));
            Assert.True(AffineBasisConstruction.TryLocate(Group(f.Basis, sample), out var positive));
            Assert.True(AffineBasisConstruction.TryLocate(Group(f.Opposite, sample), out var negative));
            Number(F(Result(positive))[0], 1, 2); Number(F(Result(negative))[0], 1, 2);
            Rational(F(Result(positive))[1], cross, 4); Rational(F(Result(negative))[1], -cross, 4);

            // Read either convention back through its actual inverse calibration.
            // Equal native coordinates establish correspondence, not shared point identity.
            foreach (var pair in new[] { (f.Basis, positive), (f.Opposite, negative) })
            {
                var inverse = Group([.. F(F(pair.Item1)[2]).Select(m => (IElement)new Relation(R(m).Right, R(m).Left))]);
                var nativeBasis = Group(Relation.Segment(f.Line, 0, 1), Relation.Segment(f.Cross, 0, 1));
                Assert.True(AffineFrame.TryProject(Group(Result(pair.Item2), inverse, nativeBasis), out var native));
                Vector(Result(native), (3,1), (cross,1));
                Assert.NotSame(sample, Result(native));
            }
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void PartialAndOmittedPointsKeepKnownCoordinatesAndCurveEndpoints(int restores)
    {
        var f = new Fixture(restores);
        var partial = f.Locate(7, complete: false);
        Number(F(Result(partial))[0], 1, 2); Assert.IsType<Placeholder>(F(Result(partial))[1]);
        Assert.Single(F(F(partial)[3]));
        var absent = f.Locate(8, complete: false);
        var coordinates = F(Result(absent));
        Assert.All(coordinates, c => Assert.IsType<Placeholder>(c));
        Assert.NotSame(coordinates[0], coordinates[1]); // omission is not u=v evidence
        Assert.Equal(2, F(F(absent)[3]).Length);
        var (partialPoint, partialReads) = f.Curve(500, f.Basis, partialControl: true);
        Number(F(partialPoint)[0], 1, 2);
        Assert.IsType<Placeholder>(F(partialPoint)[1]);
        Assert.NotEmpty(F(F(partialReads[^1])[3]));
        var (missingPoint, _) = f.Curve(500, f.Basis, missingControl: true);
        Assert.All(F(missingPoint), c => Assert.IsType<Placeholder>(c));
        var (start, _) = f.Curve(0, f.Basis, missingControl: true);
        Vector(start, (0,1), (0,1));
        Assert.False(AffineBasisConstruction.TryCrystallize(R(f.Roots[1]), out var open));
        Assert.Same(f.Roots[1], Assert.Single(F(F(open)[3])));
    }

    [Theory]
    [InlineData("zero first")]
    [InlineData("zero second")]
    [InlineData("open second")]
    [InlineData("lookalike source")]
    [InlineData("lookalike attachment")]
    [InlineData("foreign line origin")]
    [InlineData("same axis")]
    [InlineData("opposed carrier")]
    [InlineData("extra field")]
    public void UnsupportedFoundationsCannotProduceACompleteCoordinateBasis(string fault)
    {
        var f = new Fixture(); var fields = F(f.Foundation); var first = R(fields[0]); var selection = R(fields[1]);
        var second = R(selection.Right);
        switch (fault)
        {
            case "zero first": Location(first.Right).Tick.Set(0); break;
            case "zero second": Location(second.Right).Tick.Set(0); break;
            case "open second": fields[1] = new Relation(selection.Left, new Placeholder()); break;
            case "lookalike source": fields[1] = new Relation(Relation.OrthogonalOutlet(new Relation(first.Left, first.Right)), second); break;
            case "lookalike attachment": fields[2] = new Relation(Relation.Point(f.Line, 0), second.Left); break;
            case "foreign line origin":
                var foreign = new Carrier(UnitShape.LinearAt(Relation.Point(new Carrier(), 0)));
                var wrong = Relation.Segment(foreign, 0, 4);
                fields[1] = new Relation(selection.Left, wrong); fields[2] = new Relation(first.Left, wrong.Left); break;
            case "same axis": fields[1] = new Relation(selection.Left, first); fields[2] = new Relation(first.Left, first.Left); break;
            case "opposed carrier":
                var opposed = Relation.Segment(f.Cross.Inverse(), 0, 4);
                fields[1] = new Relation(selection.Left, opposed); fields[2] = new Relation(first.Left, opposed.Left); break;
            case "extra field": fields = [.. fields, new Placeholder()]; break;
        }
        var request = Group(fields);
        Assert.False(AffineBasisConstruction.TryCrystallize(request, out var read));
        Assert.IsType<Placeholder>(Result(read)); Assert.Same(request, Assert.Single(F(F(read)[3])));
    }

    [Theory]
    [InlineData("detached source")]
    [InlineData("detached target")]
    [InlineData("wrong unit promise")]
    [InlineData("changed frozen extent")]
    public void AUnitLabelOrUnconnectedCalibrationCannotReplaceTheGroundedBasis(string fault)
    {
        var f = new Fixture(); var point = Result(f.Locate(2));
        var grounded = F(f.Basis); var maps = F(grounded[2]); var first = R(maps[0]);
        switch (fault)
        {
            case "detached source": maps[0] = new Relation(new Relation(R(first.Left).Left, R(first.Left).Right), first.Right); break;
            case "detached target": maps[0] = new Relation(first.Left, new Relation(R(first.Right).Left, R(first.Right).Right)); break;
            case "changed frozen extent": Location(R(first.Left).Right).Tick.Set(12); break;
            case "wrong unit promise":
                var units = F(grounded[1]); units[0] = Relation.Segment(new Carrier(), 0, 1);
                maps[0] = new Relation(first.Left, units[0]); grounded[1] = Group(units); break;
        }
        grounded[2] = Group(maps); var invalid = Group(grounded);
        Assert.False(AffineBasisConstruction.TryLocate(Group(invalid, F(f.Roots[6])[1]), out _));
        var t = new Carrier();
        var grade = Group(new CarrierLocation(t, 0), Relation.Segment(t, 0, 1), Relation.Segment(t, 0, 1), invalid);
        Assert.False(AffineFrame.TryInterpolate(new Relation(Group(point, point, grade), new Placeholder()), out _));
    }

    [Fact]
    public void NativeFractionsAndOutsideUnitCoordinatesUseCalibrationWithoutImplicitClamping()
    {
        var f = new Fixture();
        var fraction = new Relation(new CarrierLocation(f.Line, 3), Relation.Point(f.Line, 2));
        var input = Group(fraction, new CarrierLocation(f.Cross, 6));
        Assert.True(AffineBasisConstruction.TryLocate(Group(f.Basis, input), out var read));
        Vector(Result(read), (1,4), (3,2));
        Assert.Empty(F(F(read)[3]));
        // Compatible units do not authorize an implicit conversion from another axis.
        var foreign = new Carrier(f.Line.UnitShape);
        Assert.False(AffineBasisConstruction.TryLocate(Group(f.Basis,
            Group(new CarrierLocation(foreign, 3), new CarrierLocation(f.Cross, 2))), out var rejected));
        Assert.IsType<Placeholder>(Result(rejected)); Assert.NotEmpty(F(F(rejected)[3]));
    }

    [Fact]
    public void CrystallizedSpansAndObservationHistoryDoNotDriftWithLiveInputs()
    {
        var f = new Fixture(); var sample = Group(new CarrierLocation(f.Line, 6), new CarrierLocation(f.Cross, 2));
        Assert.True(AffineBasisConstruction.TryLocate(Group(f.Basis, sample), out var before));
        Location(R(F(f.Foundation)[0]).Right).Tick.Set(12);
        Assert.True(AffineBasisConstruction.TryCrystallize(f.Foundation, out var later));
        Assert.True(AffineBasisConstruction.TryLocate(Group(Result(later), sample), out var changedBasis));
        Vector(Result(before), (1,1), (1,2)); Vector(Result(changedBasis), (1,2), (1,2));
        Assert.True(AffineBasisConstruction.TryLocate(Group(f.Basis, sample), out var oldBasis));
        Vector(Result(oldBasis), (1,1), (1,2));
        Assert.IsType<CarrierLocation>(F(sample)[0]).Tick.Set(3);
        Vector(Result(before), (1,1), (1,2));
        var captured = F(F(before)[4])[0]; Number(F(F(captured)[1])[0], 6);
        Assert.True(AffineBasisConstruction.TryLocate(R(Restore(Group(f.Basis, sample), 2)), out var after));
        Vector(Result(after), (1,2), (1,2));
    }

    [Fact]
    public void SharedSiteWrappersAndReversedNativeFramesHaveExplicitReads()
    {
        var f = new Fixture(); var first = R(F(f.Foundation)[0]);
        var reversed = new Relation(first.Right, first.Left);
        var cross = new Carrier(UnitShape.LinearAt(R(reversed.Left)));
        var second = Relation.Segment(cross, 0, -4);
        Relation Alias(IElement e) { var p = Location(e); return new Relation(new CarrierLocation(p.Carrier, p.Tick), p.Carrier); }
        var foundation = Group(reversed, new Relation(Relation.OrthogonalOutlet(reversed), second),
            new Relation(Alias(reversed.Left), Alias(second.Left)));
        Assert.True(AffineBasisConstruction.TryCrystallize(R(Restore(foundation, 2)), out var read));
        // Use the restored native carriers, preserving the independent-world boundary.
        var basis = Result(read); var legs = F(F(basis)[0]);
        var x = Location(R(legs[0]).Left).Carrier; var y = Location(R(R(legs[1]).Right).Left).Carrier;
        Assert.True(AffineBasisConstruction.TryLocate(Group(basis,
            Group(new CarrierLocation(x, 0), new CarrierLocation(y, 2))), out var point));
        Vector(Result(point), (1,1), (-1,2));
    }

    private sealed class Fixture
    {
        public IElement[] Roots { get; }
        public Relation Foundation => R(F(Roots[0])[1]);
        public Relation Basis { get; }
        public Relation Opposite { get; }
        public Carrier Line => Location(R(F(Foundation)[0]).Left).Carrier;
        public Carrier Cross => Location(R(R(F(Foundation)[1]).Right).Left).Carrier;
        public Fixture(int restores = 0)
        {
            Roots = GojoSyntax.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory,
                "Fixtures", "Syntax", "grounded-affine-basis.gojo"))).Roots.ToArray();
            for (var i = 0; i < restores; i++) Roots = GojoNumericFormat.Decode(GojoNumericFormat.Encode(Roots));
            Assert.True(AffineBasisConstruction.TryCrystallize(Foundation, out var read)); Basis = R(Result(read));
            Assert.True(AffineBasisConstruction.TryCrystallize(R(Roots[9]), out var other)); Opposite = R(Result(other));
        }
        public Relation Locate(int root, Relation? basis = null, bool complete = true)
        {
            var request = R(Roots[root]);
            request = R(StructuralPattern.Apply(request, new Relation(F(request)[0], basis ?? Basis)));
            var success = AffineBasisConstruction.TryLocate(request, out var witness);
            Assert.Equal(complete, success); return witness;
        }
        public (IElement Point, List<IElement> Reads) Curve(long tick, Relation basis,
            bool partialControl = false, bool missingControl = false)
        {
            var setup = F(Roots[10]); var slots = F(setup[2]);
            var points = new IElement[] { Result(Locate(2, basis)), Result(Locate(4, basis)), Result(Locate(5, basis)), Result(Locate(3, basis)) };
            if (partialControl) points[2] = Group(F(points[2])[0], new Placeholder());
            var bindings = new List<Relation> { new(slots[0], basis) };
            for (var i = 0; i < 4; i++) if (!(missingControl && i == 2)) bindings.Add(new Relation(slots[i + 1], points[i]));
            var sample = R(F(setup[1])[0]); var parameter = Assert.IsType<CarrierLocation>(sample.Right).Carrier;
            bindings.Add(new Relation(sample.Left, new CarrierLocation(parameter, tick)));
            var reads = new List<IElement>();
            foreach (var step in F(F(setup[0])[1]))
            {
                var request = R(StructuralPattern.Apply(step, bindings));
                var complete = AffineFrame.TryInterpolate(request, out var read);
                if (!partialControl && !missingControl) Assert.True(complete);
                reads.Add(read); bindings.Add(new Relation(R(step).Right, Result(read)));
            }
            return (Result(reads[^1]), reads);
        }
    }
    private static void Polynomial(IElement value, long[] p, long tick)
    {
        BigInteger t = tick; BigInteger s = 1000 - tick;
        var n = p[0]*s*s*s + 3*p[1]*s*s*t + 3*p[2]*s*t*t + p[3]*t*t*t;
        var d = BigInteger.Pow(1000, 3); var gcd = BigInteger.GreatestCommonDivisor(n, d);
        Number(value, (long)(n / gcd), (long)(d / gcd));
    }
    private static void Rational(IElement value, long n, long d)
    { var gcd = (long)BigInteger.GreatestCommonDivisor(n, d); Number(value, n / gcd, d / gcd); }
    private static void Number(IElement value, long n, long d = 1)
    {
        if (value is Relation { Left: CarrierLocation p, Right: Carrier }) value = p;
        Assert.True(ProjectionResidue.TryRead(value, out _, out var actual, out var divisor));
        Assert.Equal(n, actual); Assert.Equal(d, divisor);
    }
    private static void Vector(IElement point, params (long N, long D)[] values)
    { var f = F(point); Assert.Equal(values.Length, f.Length); for (var i = 0; i < f.Length; i++) Number(f[i], values[i].N, values[i].D); }
    private static CarrierLocation Location(IElement p) => Assert.IsType<CarrierLocation>(R(p).Left);
    private static IElement Restore(IElement e, int count)
    { for (var i = 0; i < count; i++) e = GojoNumericFormat.Decode(GojoNumericFormat.Encode(e))[0]; return e; }
    private static Relation R(IElement e) => Assert.IsType<Relation>(e);
    private static Relation Group(params IElement[] f) => Relation.OrderedGroup(new Carrier(), f);
    private static IElement[] F(IElement e) { Assert.True(R(e).TryReadOrderedGroup(out var f)); return f; }
    private static IElement Result(IElement witness) => F(witness)[2];
}
