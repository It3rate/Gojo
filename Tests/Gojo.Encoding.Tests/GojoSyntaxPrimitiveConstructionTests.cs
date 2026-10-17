using System.Numerics;
using Gojo.Core;
using Gojo.Encoding.Syntax;
using Gojo.Patterns;
using Gojo.Structures;

namespace Gojo.Encoding.Tests;

public class GojoSyntaxPrimitiveConstructionTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void ConstructedParallelFeedsItsActualCornerIntoBezierAndSurvivesTransport(int restores)
    {
        var f = Fixture(restores);
        var bindings = F(f[1]).Select(R).ToList();
        var (output, reads) = Construct(f, bindings);
        var edges = F(output);
        Coordinates(R(edges[1]).Right, 7, 13);
        AssertBernstein(F(edges[3])[0], [-3,-1,7,5], 500); // 5/2
        AssertBernstein(F(edges[3])[1], [2,8,13,7], 500); // 9
        Assert.Equal(8, reads.Count);
        Assert.All(reads, read => Assert.Empty(F(F(read)[3])));
        Assert.Same(Result(reads[0]), F(R(F(reads[1])[0]).Left)[1]);
        Assert.Same(Result(reads[1]), R(edges[1]).Right);
        Assert.Same(Result(reads[1]), F(R(F(reads[3])[0]).Left)[1]);
        Assert.Same(Result(reads[1]), F(R(F(reads[4])[0]).Left)[0]);
        Assert.Same(R(edges[0]).Left, R(R(edges[2]).Left).Left);
        Assert.Same(R(edges[1]).Right, R(R(edges[2]).Right).Right);

        // Both ordered edges have the same displacement, calculated by the kernel.
        Assert.True(AffineFrame.TryDifference(Group(R(edges[0]).Left, R(edges[0]).Right, f[3]), out var first));
        Assert.True(AffineFrame.TryDifference(Group(R(edges[1]).Left, R(edges[1]).Right, f[3]), out var second));
        Coordinates(Result(first), 8, 5);
        Coordinates(Result(second), 8, 5);
        // Its displacement (2,6) is deliberately not a normal under an identity metric:
        // (8,5) dot (2,6) = 46. Parallel does not imply a perpendicular construction.

        var packet = F(Restore(Group(output, Group([.. reads])), 2));
        var restoredReads = F(packet[1]);
        Assert.Same(Result(restoredReads[1]), R(F(packet[0])[1]).Right);
        Assert.Same(Result(restoredReads[1]), F(R(F(restoredReads[3])[0]).Left)[1]);
        Assert.Same(Result(restoredReads[7]), F(packet[0])[3]);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void ConstructionAgreesWithIndependentAffineAndBernsteinLawsInDifferentCharts(int restores)
    {
        // Input observations are re-expressed by these test-only affine maps.
        // The kernel must construct all subsequent geometry from those observations.
        var maps = new (long XX, long XY, long YX, long YY, long X, long Y)[]
        {
            (1,0,0,1,0,0), (1,2,0,1,-11,7), (-2,0,0,3,4,-9), (0,-1,1,0,3,5)
        };
        foreach (var map in maps)
        foreach (var tick in new long[] { 0, 137, 500, 999, 1000 })
        {
            var f = Fixture(restores);
            var bindings = F(f[1]).Select(R).ToList();
            var source = new (long X, long Y)[] { (-3,2), (5,7), (-1,8) };
            var mapped = source.Select(p =>
                (X: map.XX * p.X + map.XY * p.Y + map.X, Y: map.YX * p.X + map.YY * p.Y + map.Y)).ToArray();
            for (var i = 0; i < 3; i++)
                bindings[i] = new Relation(bindings[i].Left, PointLike(bindings[i].Right, mapped[i].X, mapped[i].Y));
            var sample = Assert.IsType<CarrierLocation>(bindings[4].Right);
            bindings[4] = new Relation(bindings[4].Left, new CarrierLocation(sample.Carrier, tick));
            var (output, _) = Construct(f, bindings);
            var cornerX = mapped[1].X + mapped[2].X - mapped[0].X;
            var cornerY = mapped[1].Y + mapped[2].Y - mapped[0].Y;
            Coordinates(R(F(output)[1]).Right, cornerX, cornerY);
            var point = F(F(output)[3]);
            AssertBernstein(point[0], [mapped[0].X, mapped[2].X, cornerX, mapped[1].X], tick);
            AssertBernstein(point[1], [mapped[0].Y, mapped[2].Y, cornerY, mapped[1].Y], tick);
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void MissingGuideOrDisallowedExtrapolationLeavesConstructionOpen(int restores)
    {
        foreach (var missingGuide in new[] { true, false })
        {
            var f = Fixture(restores);
            var bindings = F(f[1]).Select(R).ToList();
            if (missingGuide) bindings.RemoveAt(2);
            else
            {
                var grade = F(bindings[3].Right);
                bindings[3] = new Relation(bindings[3].Left, Group(grade[0], grade[1], grade[1], grade[3]));
            }
            var (output, reads) = Construct(f, bindings, complete: false);
            Assert.NotEmpty(F(F(reads[1])[3]));
            Assert.NotEmpty(F(F(reads[^1])[3]));
            Assert.All(F(F(output)[3]), coordinate => Assert.IsType<Placeholder>(coordinate));
            Assert.Same(bindings[0].Right, R(F(output)[0]).Left);
        }
        // An unknown downstream corner does not hide the known start endpoint.
        var original = Fixture(restores);
        var endpointBindings = F(original[1]).Select(R).ToList();
        endpointBindings.RemoveAt(2);
        var sample = Assert.IsType<CarrierLocation>(endpointBindings[^1].Right);
        endpointBindings[^1] = new Relation(endpointBindings[^1].Left, new CarrierLocation(sample.Carrier, 0));
        var (endpoint, _) = Construct(original, endpointBindings, complete: false);
        Coordinates(F(endpoint)[3], -3, 2);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void CollapsedSourceRetainsAValidDegenerateConstructionWithoutMergingSites(int restores)
    {
        var f = Fixture(restores);
        var bindings = F(f[1]).Select(R).ToList();
        bindings[1] = new Relation(bindings[1].Left, bindings[0].Right);
        var (output, _) = Construct(f, bindings);
        var edges = F(output);
        Assert.Same(R(edges[0]).Left, R(edges[0]).Right);
        Coordinates(R(edges[1]).Right, -1, 8);
        Assert.NotSame(R(edges[1]).Left, R(edges[1]).Right);
        AssertBernstein(F(edges[3])[0], [-3,-1,-1,-3], 500);
        AssertBernstein(F(edges[3])[1], [2,8,8,2], 500);
        // Equal zero displacements establish a degenerate affine completion,
        // not a regular line direction or a geometric parallelism verdict.
    }

    private static (IElement Output, List<IElement> Reads) Construct(IElement[] f, List<Relation> initial, bool complete = true)
    {
        var bindings = new List<Relation>(initial);
        var reads = new List<IElement>();
        foreach (var step in F(F(f[0])[1]))
        {
            var request = R(StructuralPattern.Apply(step, bindings));
            var success = AffineFrame.TryInterpolate(request, out var witness);
            if (complete) Assert.True(success);
            reads.Add(witness);
            bindings.Add(new Relation(R(step).Right, Result(witness)));
        }
        return (StructuralPattern.Apply(f[2], bindings), reads);
    }

    private static IElement[] Fixture(int restores)
    {
        var root = GojoSyntax.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory,
            "Fixtures", "Syntax", "constructed-parallels.gojo"))).Roots.Single();
        return F(Restore(root, restores));
    }
    private static IElement Restore(IElement root, int count)
    {
        for (var i = 0; i < count; i++) root = GojoNumericFormat.Decode(GojoNumericFormat.Encode(root))[0];
        return root;
    }
    private static Relation PointLike(IElement point, long x, long y)
    {
        var coordinates = F(point).Cast<CarrierLocation>().ToArray();
        return Group(new CarrierLocation(coordinates[0].Carrier, x), new CarrierLocation(coordinates[1].Carrier, y));
    }
    private static void AssertBernstein(IElement actual, long[] p, long tick)
    {
        BigInteger u = tick; BigInteger v = 1000 - tick;
        var n = p[0] * v * v * v + 3 * p[1] * v * v * u + 3 * p[2] * v * u * u + p[3] * u * u * u;
        BigInteger d = 1000000000;
        var gcd = BigInteger.GreatestCommonDivisor(n, d);
        Assert.True(ProjectionResidue.TryRead(actual, out _, out var actualN, out var actualD));
        Assert.Equal(n / gcd, new BigInteger(actualN));
        Assert.Equal(d / gcd, new BigInteger(actualD));
    }
    private static void Coordinates(IElement point, long x, long y)
    {
        var fields = F(point);
        for (var i = 0; i < 2; i++)
        {
            Assert.True(ProjectionResidue.TryRead(fields[i], out _, out var n, out var d));
            Assert.Equal(i == 0 ? x : y, n); Assert.Equal(1, d);
        }
    }
    private static Relation R(IElement element) => Assert.IsType<Relation>(element);
    private static Relation Group(params IElement[] fields) => Relation.OrderedGroup(new Carrier(), fields);
    private static IElement[] F(IElement element)
    { Assert.True(R(element).TryReadOrderedGroup(out var fields)); return fields; }
    private static IElement Result(IElement witness) => F(witness)[2];
}
