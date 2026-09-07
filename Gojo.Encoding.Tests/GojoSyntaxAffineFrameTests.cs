using System.Numerics;
using Gojo.Core;
using Gojo.Encoding.Syntax;
using Gojo.Patterns;
using Gojo.Structures;

namespace Gojo.Encoding.Tests;

public class GojoSyntaxAffineFrameTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void ExactAndPartialInterpolationUseExplicitSupportAndOrderedFrames(int restores)
    {
        var roots = Parse("affine-frame.gojo", restores);
        Assert.True(AffineFrame.TryInterpolate(R(roots[0]), out var exact));
        Vector(Result(exact), (0, 1), (7, 3));
        Assert.False(AffineFrame.TryInterpolate(R(roots[1]), out var partial));
        Scalar(F(Result(partial))[0], 0, 1);
        Assert.IsType<Placeholder>(F(Result(partial))[1]);
        Assert.Single(F(F(partial)[3]));
        Assert.False(AffineFrame.TryInterpolate(R(roots[2]), out var outside));
        Assert.Same(roots[2], F(outside)[0]);
        Assert.True(AffineFrame.TryInterpolate(R(roots[3]), out var extrapolated));
        Vector(Result(extrapolated), (3, 1), (10, 3));
        Assert.True(AffineFrame.TryInterpolate(R(roots[4]), out var reversed));
        Vector(Result(reversed), (0, 1), (7, 3));
        var restored = Restore(Group(exact, partial), 2);
        Vector(Result(F(restored)[0]), (0, 1), (7, 3));
        Assert.IsType<Placeholder>(F(Result(F(restored)[1]))[1]);
        Assert.Same(Result(F(restored)[0]), R(F(F(restored)[0])[4]).Right);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void DeCasteljauEdgesAgreeWithIndependentBernsteinPolynomialWithoutIntermediateRounding(int restores)
    {
        var roots = Parse("bezier-construction.gojo", restores);
        var f = F(roots[1]);
        var controls = F(roots[0]).Skip(2).ToArray();
        foreach (var tick in new long[] { 0, 1, 137, 250, 333, 500, 750, 999, 1000 })
        {
            var binds = F(f[2]).Select(R).ToList();
            var sampleBind = binds[^1];
            var parameter = ((CarrierLocation)sampleBind.Right).Carrier;
            binds[^1] = new Relation(sampleBind.Left, new CarrierLocation(parameter, tick));
            var (point, reads) = Construction(f[0], binds);
            var expectedX = Bernstein([0, 4, 8, 12], tick, 1000);
            var expectedY = Bernstein([0, 8, 0, 4], tick, 1000);
            Scalar(F(point)[0], expectedX.N, expectedX.D);
            Scalar(F(point)[1], expectedY.N, expectedY.D);
            Assert.Equal(6, reads.Count);
            Assert.All(reads, read => Assert.Empty(F(F(read)[3])));
            // The final read's inputs reference the actual preceding results.
            var finalInputs = F(R(F(reads[^1])[0]).Left);
            Assert.Same(Result(reads[3]), finalInputs[0]);
            Assert.Same(Result(reads[4]), finalInputs[1]);
            var packet = Restore(Group(Group([.. reads]), point), 2);
            var restoredReads = F(F(packet)[0]);
            Assert.Same(F(packet)[1], Result(restoredReads[^1]));
            Assert.Same(Result(restoredReads[3]), F(R(F(restoredReads[^1])[0]).Left)[0]);
            Scalar(F(F(packet)[1])[1], expectedY.N, expectedY.D);
        }
        Assert.All(controls.SelectMany(F).Cast<CarrierLocation>(), location => Assert.Equal(0UL, location.Carrier.Value));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void ClosedRepeatedReversedAndMissingControlsKeepTheirConstructionMeaning(int restores)
    {
        var roots = Parse("bezier-construction.gojo", restores);
        var normal = F(roots[1]); var degenerate = F(roots[2]); var reverse = F(roots[3]);
        var sample = R(F(normal[2])[^1]);
        var closed = F(degenerate[3]).Select(R).Append(sample).ToList();
        var (closedPoint, _) = Construction(degenerate[2], closed);
        Vector(closedPoint, (9, 2), (3, 1));
        var repeated = F(degenerate[4]).Select(R).Append(sample).ToList();
        var (repeatedPoint, _) = Construction(degenerate[2], repeated);
        Vector(repeatedPoint, (0, 1), (0, 1));
        var t = ((CarrierLocation)sample.Right).Carrier;
        var forward = F(normal[2]).Select(R).SkipLast(1)
            .Append(new Relation(sample.Left, new CarrierLocation(t, 137))).ToList();
        var reversed = F(reverse[1]).Select(R)
            .Append(new Relation(sample.Left, new CarrierLocation(t, 863))).ToList();
        var (a, _) = Construction(normal[0], forward);
        var (b, _) = Construction(reverse[0], reversed);
        SameCoordinates(a, b);
        // All control bindings except the second remain, with a grounded parameter.
        var partial = F(normal[1]).Select(R).Append(sample).ToList();
        var (open, openReads) = Construction(normal[0], partial, complete: false);
        Assert.All(F(open), value => Assert.IsType<Placeholder>(value));
        Assert.Empty(F(F(openReads[0])[3])); // the first edge is already knowable
        Assert.NotEmpty(F(F(openReads[^1])[3]));
        partial[^1] = new Relation(sample.Left, new CarrierLocation(t, 0));
        var (endpoint, _) = Construction(normal[0], partial, complete: false);
        Vector(endpoint, (0, 1), (0, 1));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void MotifCorrespondencePreservesTopologySharedTicksAndIndependentLookalikes(int restores)
    {
        var request = Parse("affine-frame.gojo", restores)[5];
        Assert.True(AffineFrame.TryMapExposed(R(request), out var read));
        var source = F(F(request)[0]); var mapped = F(Result(read));
        Assert.Same(mapped[0], mapped[1]);
        Assert.Same(R(mapped[0]).Right, R(mapped[2]).Left);
        Assert.Same(source[3], mapped[3]);
        var p = R(mapped[0]).Left; var q = R(mapped[0]).Right; var lookalike = R(mapped[2]).Right;
        Vector(p, (11, 3), (-1, 1)); Vector(q, (11, 3), (-3, 1));
        SameCoordinates(p, lookalike);
        Assert.NotSame(p, lookalike);
        Assert.Same(R(F(p)[0]).Left, R(F(q)[0]).Left);
        Assert.NotSame(R(F(p)[0]).Left, R(F(lookalike)[0]).Left);
        var restored = Restore(read, 2);
        var restoredEdges = F(Result(restored));
        Assert.Same(restoredEdges[0], restoredEdges[1]);
        var restoredP = R(restoredEdges[0]).Left; var restoredQ = R(restoredEdges[0]).Right;
        Assert.Same(((CarrierLocation)R(F(restoredP)[0]).Left).Tick,
            ((CarrierLocation)R(F(restoredQ)[0]).Left).Tick);
        var clauses = F(F(F(restored)[4])[0]);
        Assert.Same(restoredP, R(clauses[0]).Right);
        Assert.Same(F(F(F(restored)[0])[1])[0], R(clauses[0]).Left);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void SelectionDoesNotSilentlyTransformUnexposedStructure(int restores)
    {
        var request = Parse("affine-frame.gojo", restores)[6];
        var source = F(F(request)[0]);
        Assert.True(AffineFrame.TryMapExposed(R(request), out var read));
        var result = F(Result(read));
        Assert.NotSame(R(source[0]).Left, R(result[0]).Left);
        Assert.Same(R(source[0]).Right, R(result[0]).Right);
        Assert.Same(source[2], result[2]);
        Assert.Single(F(F(F(read)[4])[0]));
        // An equal coordinate group outside the selected graph has no source identity here.
        var outside = Group([.. F(R(source[0]).Left)]);
        var fields = F(request);
        Assert.False(AffineFrame.TryMapExposed(Group(fields[0], Group(outside), fields[2], fields[3]), out _));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void MappingAPartialMotifRetainsKnownCoordinatesAndSharedUnknownDependency(int restores)
    {
        var request = Parse("affine-frame.gojo", restores)[7];
        Assert.False(AffineFrame.TryMapExposed(R(request), out var read));
        var segment = R(Result(read));
        Scalar(F(segment.Left)[0], 5, 3);
        Scalar(F(segment.Right)[0], 7, 3);
        Assert.IsType<Placeholder>(F(segment.Left)[1]);
        Assert.Same(F(segment.Left)[1], F(segment.Right)[1]);
        Assert.Equal(2, F(F(read)[3]).Length);
        var restored = Restore(read, 2);
        var after = R(Result(restored));
        Assert.Same(F(after.Left)[1], F(after.Right)[1]);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void GlideCompositionAndInverseActOnTheActualExposedMotif(int restores)
    {
        var roots = Parse("frieze-patterns.gojo", restores);
        var f = F(roots[2]); var glide = F(f[0]);
        Assert.True(AffineFrame.TryMapExposed(R(f[0]), out var once));
        var nextExposure = Group([.. F(F(F(once)[4])[0]).Select(clause => R(clause).Right)]);
        Assert.True(AffineFrame.TryMapExposed(Group(Result(once), nextExposure, glide[2], glide[3]), out var twice));
        Assert.True(AffineFrame.TryMapExposed(R(f[1]), out var translated));
        var twicePoints = F(F(F(twice)[4])[0]).Select(clause => R(clause).Right).ToArray();
        var translatedPoints = F(F(F(translated)[4])[0]).Select(clause => R(clause).Right).ToArray();
        for (var i = 0; i < twicePoints.Length; i++) SameCoordinates(twicePoints[i], translatedPoints[i]);
        Assert.True(AffineFrame.TryMapExposed(Group(Result(once), nextExposure, f[2], glide[3]), out var inverse));
        var originalPoints = F(glide[1]);
        var returned = F(F(F(inverse)[4])[0]).Select(clause => R(clause).Right).ToArray();
        for (var i = 0; i < returned.Length; i++)
        {
            SameCoordinates(originalPoints[i], returned[i]);
            Assert.NotSame(originalPoints[i], returned[i]); // inverse read is not reference revival
        }
        // Fill the actual exposed correspondence obligation using a witnessed read.
        var operation = F(F(f[3])[1]); var slot = operation[2];
        var bound = StructuralPattern.Apply(f[3], new Relation(slot, once));
        Assert.Same(once, F(F(bound)[1])[2]);
        Assert.IsType<Placeholder>(F(f[6])[1]); // repeat support still open
        Assert.Same(F(f[6])[0], ((CarrierLocation)R(R(f[5]).Left).Left).Carrier);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void EndpointFirstAndSecondDerivativeClaimsHaveIndependentWitnesses(int restores)
    {
        var roots = Parse("bridge-continuity.gojo", restores); var f = F(roots[0]);
        var binds = new List<Relation>(); var witnesses = new List<IElement>();
        var steps = F(f[6]);
        for (var i = 0; i < steps.Length; i++) ApplyStep(steps[i], i < 2, binds, witnesses);
        var endpointRequest = R(R(f[4]).Left);
        Assert.True(AffineFrame.TryDifference(endpointRequest, out var endpoint));
        Vector(Result(endpoint), (0, 1), (0, 1));
        Assert.Same(F(endpointRequest)[0], F(endpointRequest)[1]);
        var tangentRequest = R(StructuralPattern.Apply(R(f[5]).Left, binds));
        Assert.True(AffineFrame.TryDifference(tangentRequest, out var tangent));
        Vector(Result(tangent), (0, 1), (0, 1));
        Vector(binds[^1].Right, (3, 1), (0, 1));
        var second = F(roots[3]); var secondSteps = F(second[0]);
        for (var i = 0; i < secondSteps.Length; i++) ApplyStep(secondSteps[i], i < 4, binds, witnesses);
        var accelerationRequest = R(StructuralPattern.Apply(R(second[1]).Left, binds));
        Assert.True(AffineFrame.TryDifference(accelerationRequest, out var acceleration));
        Vector(Result(acceleration), (0, 1), (6, 1)); // C1 holds; C2 does not
        Vector(R(second[1]).Right, (0, 1), (0, 1)); // desired result is still attached
        Assert.IsType<Placeholder>(f[9]); // no curvature verdict inferred
        var contact = F(roots[2]);
        Assert.NotSame(contact[1], contact[2]);
        Assert.True(AffineFrame.TryDifference(R(contact[0]), out var equal));
        Vector(Result(equal), (0, 1), (0, 1));
        var packet = Restore(Group(Group([.. witnesses]), endpoint, tangent, acceleration), 2);
        Vector(Result(F(packet)[3]), (0, 1), (6, 1));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void PositiveTangentRescalingAndZeroDerivativeRemainDifferentCases(int restores)
    {
        var f = F(Parse("bridge-continuity.gojo", restores)[1]);
        Assert.True(AffineFrame.TryDifference(R(f[0]), out var leg));
        Assert.True(AffineFrame.TryProject(Group(Result(leg), f[2], f[4]), out var faster));
        Vector(Result(faster), (6, 1), (0, 1));
        Assert.True(AffineFrame.TryProject(Group(Result(faster), f[3], f[4]), out var rescaled));
        Vector(Result(rescaled), (3, 1), (0, 1));
        Assert.True(AffineFrame.TryDifference(R(f[1]), out var collapsed));
        Assert.True(AffineFrame.TryProject(Group(Result(collapsed), f[2], f[4]), out var zero));
        Vector(Result(zero), (0, 1), (0, 1));
    }

    private static void ApplyStep(IElement clause, bool difference, List<Relation> bindings, List<IElement> witnesses)
    {
        var request = R(StructuralPattern.Apply(R(clause).Left, bindings));
        Relation read;
        Assert.True(difference ? AffineFrame.TryDifference(request, out read) : AffineFrame.TryProject(request, out read));
        witnesses.Add(read); bindings.Add(new Relation(R(clause).Right, Result(read)));
    }

    private static (IElement Point, List<IElement> Reads) Construction(IElement action, List<Relation> bindings, bool complete = true)
    {
        // Each sample is a fresh application. Intermediate bindings from an
        // earlier sample are observations of that sample, not live formulas.
        bindings = [.. bindings];
        var reads = new List<IElement>(); var steps = F(F(action)[1]);
        foreach (var step in steps)
        {
            var request = R(StructuralPattern.Apply(step, bindings));
            var success = AffineFrame.TryInterpolate(request, out var read);
            if (complete) Assert.True(success);
            reads.Add(read);
            bindings.Add(new Relation(R(step).Right, Result(read)));
        }
        return (Result(reads[^1]), reads);
    }
    private static (long N, long D) Bernstein(long[] controls, long t, long scale)
    {
        BigInteger u = t; BigInteger v = scale - t;
        var numerator = controls[0] * v * v * v + 3 * controls[1] * v * v * u +
            3 * controls[2] * v * u * u + controls[3] * u * u * u;
        var denominator = BigInteger.Pow(scale, 3);
        var gcd = BigInteger.GreatestCommonDivisor(numerator, denominator);
        return ((long)(numerator / gcd), (long)(denominator / gcd));
    }
    private static IElement[] Parse(string fixture, int restores)
    {
        var roots = GojoSyntax.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "Syntax", fixture))).Roots.ToArray();
        for (var i = 0; i < restores; i++) roots = GojoNumericFormat.Decode(GojoNumericFormat.Encode(roots));
        return roots;
    }
    private static IElement Restore(IElement root, int count)
    {
        for (var i = 0; i < count; i++) root = GojoNumericFormat.Decode(GojoNumericFormat.Encode(root))[0];
        return root;
    }
    private static Relation R(IElement element) => Assert.IsType<Relation>(element);
    private static Relation Group(params IElement[] fields) => Relation.OrderedGroup(new Carrier(), fields);
    private static IElement[] F(IElement element)
    { Assert.True(R(element).TryReadOrderedGroup(out var fields)); return fields; }
    private static IElement Result(IElement witness) => F(witness)[2];
    private static void Scalar(IElement value, long n, long d)
    {
        Assert.True(ProjectionResidue.TryRead(value, out _, out var numerator, out var denominator));
        Assert.Equal(n, numerator); Assert.Equal(d, denominator);
    }
    private static void Vector(IElement point, params (long N, long D)[] expected)
    {
        var actual = F(point); Assert.Equal(expected.Length, actual.Length);
        for (var i = 0; i < actual.Length; i++) Scalar(actual[i], expected[i].N, expected[i].D);
    }
    private static void SameCoordinates(IElement first, IElement second)
    {
        var a = F(first); var b = F(second); Assert.Equal(a.Length, b.Length);
        for (var i = 0; i < a.Length; i++)
        {
            Assert.True(ProjectionResidue.TryRead(a[i], out var basis, out var n, out var d));
            Assert.True(ProjectionResidue.TryRead(b[i], out var other, out _, out _));
            Assert.Same(basis, other); Scalar(b[i], n, d);
        }
    }
}
