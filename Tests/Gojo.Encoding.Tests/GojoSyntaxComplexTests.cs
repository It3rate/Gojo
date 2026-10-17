using System.Numerics;
using Gojo.Core;
using Gojo.Encoding.Syntax;
using Gojo.Reads;
using Gojo.Structures;

namespace Gojo.Encoding.Tests;

public sealed class GojoSyntaxComplexTests
{
    [Theory]
    [InlineData(0)] [InlineData(1)] [InlineData(2)]
    public void OpposedInterpretationKeepsBothPositiveComponentsAndItsFourProductTerms(int restores)
    {
        var f = Fixture(restores); Assert.True(ComplexFrame.TryReadBasis(f[0]));
        Assert.True(ComplexFrame.TryMultiply(R(f[3]), out var read));
        Pair(Result(read), 7, 22);
        var terms = F(F(F(read)[4])[1]); Assert.Equal(4, terms.Length);
        Pair(R(terms[0]).Right, 15, 0); Pair(R(terms[1]).Right, 0, 12);
        Pair(R(terms[2]).Right, 0, 10); Pair(R(terms[3]).Right, -8, 0);
        Assert.True(Axes(f[0])[1].SameIdentityAs(Axes(f[0])[0].Inverse()));
        Pair(f[1], 3, 2); Pair(f[2], 5, 4);
        var stored = Restore(read, 2); Pair(Result(stored), 7, 22);
        Assert.True(ComplexFrame.TryMultiply(R(F(F(stored)[4])[0]), out var replay)); Pair(Result(replay), 7, 22);
        Assert.All(R(f[3]).Carriers(), c => { Assert.Equal(0UL, c.Value); Assert.Equal(0L, c.Tension); });
    }

    [Fact]
    public void GeneratorNegationConjugationAndFaceDualHaveDifferentActions()
    {
        var f = Fixture(); var basis = f[0]; var axis = Axes(basis)[0];
        Assert.Same(axis, axis.Inverse().Inverse());
        var current = State(basis, 0);
        for (var step = 1; step <= 4; step++)
        {
            Assert.True(ComplexFrame.TryApplyGenerator(Group(basis, current), out var read));
            SameNumbers(Result(read), State(basis, step % 4)); current = Result(read);
        }
        Assert.True(ComplexFrame.TryApplyGenerator(Group(basis, f[1]), out var applied)); Pair(Result(applied), -2, 3);
        Assert.True(ComplexFrame.TryConjugate(Group(basis, f[1]), out var conjugate)); Pair(Result(conjugate), 3, -2);
        Assert.True(ComplexFrame.TrySubtract(Group(basis, Value(basis, 0, 0), f[1]), out var negated)); Pair(Result(negated), -3, -2);
        var rightCalibration = F(R(F(F(F(negated)[4])[1])[1]).Right);
        Assert.All(rightCalibration, map => Assert.Equal(-1, Assert.IsType<CarrierLocation>(R(R(R(map).Right).Right).Left).Ticks));
        // The pre-existing scalar face fold has a different purpose and result.
        var folded = FusedCarrierFaceReads.FoldCarrierFaces(axis, new CarrierLocation(axis, 3), new CarrierLocation(axis.Inverse(), 2));
        Assert.Equal(1, folded.Ticks); Pair(f[1], 3, 2);
    }

    [Fact]
    public void RationalComplexFieldConformsToIndependentIntegerIdentities()
    {
        var basis = ComplexFrame.Opposed(new Carrier());
        for (var a = -2; a <= 2; a++) for (var b = -2; b <= 2; b++)
        for (var c = -2; c <= 2; c++) for (var d = -2; d <= 2; d++)
        {
            var x = Value(basis, a, b); var y = Value(basis, c, d);
            Assert.True(ComplexFrame.TryMultiply(Group(basis, x, y), out var product)); Pair(Result(product), a * c - b * d, a * d + b * c);
            Assert.True(ComplexFrame.TryAdd(Group(basis, x, y), out var sum)); Pair(Result(sum), a + c, b + d);
            Assert.True(ComplexFrame.TrySubtract(Group(basis, x, y), out var difference)); Pair(Result(difference), a - c, b - d);
            Assert.True(ComplexFrame.TryMultiply(Group(basis, y, x), out var reversed)); SameNumbers(Result(product), Result(reversed));
            if (c == 0 && d == 0) { Assert.False(ComplexFrame.TryDivide(Group(basis, x, y), out _)); continue; }
            Assert.True(ComplexFrame.TryDivide(Group(basis, x, y), out var quotient));
            Pair(Result(quotient), a * c + b * d, b * c - a * d, c * c + d * d);
            Assert.True(ComplexFrame.TryMultiply(Group(basis, Result(quotient), y), out var recovered)); Pair(Result(recovered), a, b);
        }
        // Associativity and distributivity check a different composition path.
        foreach (var x in new[] { Value(basis, 2, 3), Value(basis, -1, 4), Value(basis, 0, 0) })
        foreach (var y in new[] { Value(basis, 1, -2), Value(basis, 3, 0) })
        foreach (var z in new[] { Value(basis, -2, 1), Value(basis, 1, 1) })
        {
            IElement Mul(IElement a, IElement b) { Assert.True(ComplexFrame.TryMultiply(Group(basis, a, b), out var w)); return Result(w); }
            IElement Add(IElement a, IElement b) { Assert.True(ComplexFrame.TryAdd(Group(basis, a, b), out var w)); return Result(w); }
            SameNumbers(Mul(Mul(x, y), z), Mul(x, Mul(y, z)));
            SameNumbers(Mul(x, Add(y, z)), Add(Mul(x, y), Mul(x, z)));
        }
    }

    [Theory]
    [InlineData("collapsed-role")] [InlineData("swapped-action")] [InlineData("two-step-dual")]
    [InlineData("lookalike-cycle")] [InlineData("open-edge")] [InlineData("wrong-unit-span")]
    public void APairOrAnInvolutionDoesNotFalselyReadAsAComplexBasis(string fault)
    {
        var f = Fixture(); var basis = F(f[0]); var coords = F(basis[0]); var edges = F(basis[1]);
        switch (fault)
        {
            case "collapsed-role": coords[1] = coords[0]; basis[0] = Relation.OrderedGroup((Carrier)R(basis[0]).Right, coords); break;
            case "swapped-action": (edges[1], edges[2]) = (edges[2], edges[1]); break;
            case "two-step-dual": edges[1] = new Relation(R(edges[1]).Left, R(edges[0]).Left); break;
            case "lookalike-cycle": edges[0] = new Relation(R(edges[0]).Left, Value(f[0], 0, 1)); break;
            case "open-edge": edges[2] = new Placeholder(); break;
            case "wrong-unit-span": coords[0] = Relation.Segment(Axes(f[0])[0], 0, 2); basis[0] = Relation.OrderedGroup((Carrier)R(basis[0]).Right, coords); break;
        }
        basis[1] = Group(edges); var invalid = Restore(Group(basis), 1);
        Assert.False(ComplexFrame.TryReadBasis(invalid));
        Assert.False(ComplexFrame.TryMultiply(Group(invalid, f[1], f[2]), out var read));
        Assert.IsType<Placeholder>(Result(read)); Assert.NotEmpty(F(F(read)[3]));
    }

    [Fact]
    public void OpenComponentsPreserveKnownPartsAndSharedCancellationWithoutUnknownDivision()
    {
        var f = Fixture(); var basis = f[0];
        Assert.True(ComplexFrame.TrySubtract(Group(basis, f[10], f[11]), out var difference)); Pair(Result(difference), -4, 0);
        Assert.False(ComplexFrame.TryMultiply(Group(basis, f[10], Value(basis, 2, 0)), out var product));
        Number(F(Result(product))[0], 6); Assert.IsType<Placeholder>(F(Result(product))[1]);
        Assert.True(ComplexFrame.TryMultiply(Group(basis, f[10], Value(basis, 0, 0)), out var zero)); Pair(Result(zero), 0, 0);
        Assert.False(ComplexFrame.TryDivide(Group(basis, f[10], f[10]), out var quotient)); Assert.IsType<Placeholder>(Result(quotient));
        var whole = new Placeholder();
        Assert.True(ComplexFrame.TrySubtract(Group(basis, whole, whole), out var same)); Pair(Result(same), 0, 0);
        Assert.False(ComplexFrame.TryAdd(Group(basis, whole, f[1]), out var sum)); Assert.All(F(Result(sum)), e => Assert.IsType<Placeholder>(e));
        Assert.True(ComplexFrame.TryMultiply(Group(basis, whole, Value(basis, 0, 0)), out var allZero)); Pair(Result(allZero), 0, 0);
        Assert.False(ComplexFrame.TrySubtract(Group(basis, f[10], Value(basis, 7, new Placeholder())), out var independent));
        Number(F(Result(independent))[0], -4); Assert.IsType<Placeholder>(F(Result(independent))[1]);
    }

    [Theory]
    [InlineData(0)] [InlineData(1)] [InlineData(2)]
    public void FractionalPowersPreserveAlternativeRootsAndSeparateReferenceSelection(int restores)
    {
        var f = Fixture(restores);
        Assert.True(ComplexFrame.TryFractionalPower(R(f[4]), out var minusOne)); Candidates(minusOne, (0, 1), (0, -1));
        Assert.True(ComplexFrame.TryFractionalPower(R(f[5]), out var threeFour)); Candidates(threeFour, (2, 1), (-2, -1));
        Assert.True(ComplexFrame.TrySelectNearest(R(f[12]), out var nearest)); Pair(F(F(Result(nearest))[2]).Single(), 0, -1);
        Assert.False(ComplexFrame.TrySelectNearest(Group(f[4], Value(f[0], 0, 0)), out var tie)); Assert.Equal(2, F(F(Result(tie))[2]).Length);
        Assert.False(ComplexFrame.TrySelectNearest(Group(f[4], new Placeholder()), out var undecided));
        Candidates(F(Result(undecided))[0], (0, 1), (0, -1)); Assert.Equal(2, F(F(Result(undecided))[2]).Length);
        Assert.All(F(F(Result(undecided))[1]), d => Assert.IsType<Placeholder>(R(d).Right));
        var rootConstraint = R(F(Result(threeFour))[0]); var power = F(rootConstraint.Left);
        Assert.Equal(2, F(power[1]).Length); Assert.All(F(power[1]), e => Assert.IsType<Placeholder>(e));
        foreach (var candidate in F(F(Result(threeFour))[1]))
        {
            var check = F(candidate); var equation = R(check[1]);
            Assert.Same(check[0], F(equation.Left)[1]);
            var binding = R(F(check[4])[0]); Assert.Same(power[1], binding.Left); Assert.Same(check[0], binding.Right);
            Assert.True(ComplexFrame.TryPower(R(equation.Left), out var repeated)); SameNumbers(Result(repeated), equation.Right);
        }
        var stored = Restore(threeFour, 2); Candidates(stored, (2, 1), (-2, -1));
    }

    [Theory]
    [InlineData(9, 0, 3, 0)] [InlineData(-9, 0, 0, 3)] [InlineData(3, -4, 2, -1)]
    [InlineData(-7, 24, 3, 4)] [InlineData(0, 0, 0, 0)]
    public void SquareRootBranchesReproduceTheirSources(long a, long b, long x, long y)
    {
        var basis = Fixture()[0];
        Assert.True(ComplexFrame.TryFractionalPower(Fraction(basis, Value(basis, a, b), 1, 2), out var read));
        if (a == 0 && b == 0) Candidates(read, (0, 0)); else Candidates(read, (x, y), (-x, -y));
    }

    [Fact]
    public void RationalRootsAndNegativeExponentsDoNotRequireFloatingPointOrLoseTheDomain()
    {
        var basis = Fixture()[0];
        Assert.True(ComplexFrame.TryFractionalPower(Fraction(basis, Value(basis, 4, 0, 9), 1, 2), out var fraction));
        Pair(F(F(F(Result(fraction))[1])[0])[0], 2, 0, 3);
        Assert.True(ComplexFrame.TryFractionalPower(Fraction(basis, Value(basis, -1, 0), -1, 2), out var inverseRoots)); Candidates(inverseRoots, (0, 1), (0, -1));
        Assert.True(ComplexFrame.TryFractionalPower(Fraction(basis, Value(basis, 9, 0), 3, 2), out var powered)); Candidates(powered, (27, 0), (-27, 0));
        Assert.True(ComplexFrame.TryFractionalPower(Fraction(basis, Value(basis, 3, 4), 2, 2), out var reduced)); Candidates(reduced, (3, 4));
        Assert.True(ComplexFrame.TryFractionalPower(Fraction(basis, Value(basis, 0, 0), 1, 17), out var zero)); Candidates(zero, (0, 0));
        Assert.False(ComplexFrame.TryFractionalPower(Fraction(basis, Value(basis, 0, 0), -1, 2), out _));
        Assert.False(ComplexFrame.TryPower(Group(basis, Value(basis, 0, 0), Relation.Segment(new Carrier(), 0, 0)), out _));
        Assert.True(ComplexFrame.TryPower(Group(basis, Value(basis, 0, 1), Relation.Segment(new Carrier(), 0, long.MinValue)), out var large)); Pair(Result(large), 1, 0);
    }

    [Fact]
    public void ExactGaussianSquaresRecoverBothOriginalSignsAcrossAllQuadrants()
    {
        var basis = ComplexFrame.Opposed(new Carrier());
        for (var a = -3; a <= 3; a++) for (var b = -3; b <= 3; b++)
        {
            // Independent square oracle, not a call to the product under test.
            var source = Value(basis, a * a - b * b, 2 * a * b);
            Assert.True(ComplexFrame.TryFractionalPower(Fraction(basis, source, 1, 2), out var roots));
            var candidates = F(F(Result(roots))[1]).Select(c => F(c)[0]).ToArray();
            Assert.Equal(a == 0 && b == 0 ? 1 : 2, candidates.Length);
            var recovered = candidates.Select(c => F(c).Select(e =>
            { Assert.True(ProjectionResidue.TryRead(e, out _, out var n, out var d)); Assert.Equal(1, d); return n; }).ToArray()).ToArray();
            Assert.Contains(recovered, c => c[0] == a && c[1] == b);
            Assert.Contains(recovered, c => c[0] == -a && c[1] == -b);
        }
    }

    [Fact]
    public void IrrationalAndHigherRootsRemainEquationsWithPartialCandidates()
    {
        var f = Fixture();
        Assert.False(ComplexFrame.TryFractionalPower(R(f[6]), out var sqrtTwo));
        var constraint = R(F(Result(sqrtTwo))[0]); var candidate = F(F(constraint.Left)[1]);
        Assert.IsType<Placeholder>(candidate[0]); Number(candidate[1], 0); Pair(constraint.Right, 2, 0);
        Assert.Empty(F(F(Result(sqrtTwo))[1])); Assert.NotEmpty(F(F(sqrtTwo)[3]));
        Assert.False(ComplexFrame.TryFractionalPower(Fraction(f[0], Value(f[0], 0, 1), 1, 2), out var sqrtI));
        Assert.All(F(F(R(F(Result(sqrtI))[0]).Left)[1]), e => Assert.IsType<Placeholder>(e));
        Assert.False(ComplexFrame.TryFractionalPower(Fraction(f[0], Value(f[0], 8, 0), 1, 3), out var cube));
        // Exact 2 is one root, but returning only it would omit two complex roots.
        Assert.Empty(F(F(Result(cube))[1])); Pair(R(F(Result(cube))[0]).Right, 8, 0);
        var stored = Restore(sqrtTwo, 2); Assert.False(ComplexFrame.TryFractionalPower(R(F(F(stored)[4])[0]), out var reread));
        Number(F(F(R(F(Result(reread))[0]).Left)[1])[1], 0);
    }

    [Fact]
    public void BasisCorrespondenceCannotBeInferredFromEqualComponents()
    {
        var a = ComplexFrame.Opposed(new Carrier()); var b = ComplexFrame.Opposed(new Carrier());
        var x = Value(a, 3, 2); var y = Value(b, 3, 2);
        Assert.False(ComplexFrame.TryMultiply(Group(a, x, y), out _));
        var maps = Group(new Relation(State(a, 0), State(b, 0)), new Relation(State(a, 1), State(b, 1)));
        Assert.True(ComplexFrame.TryReexpress(Group(a, b, maps, x), out var mapped)); Pair(Result(mapped), 3, 2);
        Assert.Same(Axes(b)[1], Assert.IsType<CarrierLocation>(F(Result(mapped))[1]).Carrier);
        var lookalike = Group(new Relation(Value(a, 1, 0), State(b, 0)), new Relation(State(a, 1), State(b, 1)));
        Assert.False(ComplexFrame.TryReexpress(Group(a, b, lookalike, x), out _));
    }

    [Theory]
    [InlineData(0, false, false)] [InlineData(1, false, false)] [InlineData(2, false, false)]
    [InlineData(1, true, false)] [InlineData(1, false, true)] [InlineData(1, true, true)]
    public void ConstructedCircularQuartersAndOpposedFacesShareTheAlgebraThroughExplicitMaps(int restores, bool opposite, bool sheared)
    {
        var authored = GojoSyntax.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "Syntax", "constructed-circle-rotation.gojo"))).Roots;
        var request = F(authored[0]); var metric = F(request[1]); var units = F(metric[0]);
        var x = Assert.IsType<CarrierLocation>(R(R(units[0]).Left).Left).Carrier;
        var y = Assert.IsType<CarrierLocation>(R(R(units[1]).Left).Left).Carrier;
        var holder = (Carrier)R(metric[0]).Right;
        Relation Point(long px, long py) => Relation.OrderedGroup(holder, new CarrierLocation(x, px), new CarrierLocation(y, py));
        var centre = Point(sheared ? 30 : 10, 20);
        request[2] = new Relation(R(request[2]).Left, centre);
        request[4] = new Relation(centre, Point(sheared ? 44 : 16, 28));
        request[5] = new Relation(centre, Point(sheared ? opposite ? 29 : 31 : 10, opposite ? 19 : 21));
        if (sheared)
        {
            var squared = Assert.IsType<CarrierLocation>(R(F(metric[1])[0]).Right).Carrier;
            request[1] = Group(metric[0], Group(new Relation(new Relation(units[0], units[0]), new CarrierLocation(squared, 1)),
                new Relation(new Relation(units[0], units[1]), new CarrierLocation(squared, -1)),
                new Relation(new Relation(units[1], units[1]), new CarrierLocation(squared, 2))));
        }
        var fixture = Fixture(restores);
        Assert.True(ContourConstruction.TryCalibrateQuarters(R(Restore(Group(request), restores)), out var quarters));
        // A restored world has independent carrier identities. Construct its
        // probe on the exposed restored chart, not on the pre-restoration axes.
        var restoredCoordinates = F(F(Result(quarters))[2])[0]; var restoredUnits = F(restoredCoordinates);
        x = Assert.IsType<CarrierLocation>(R(R(restoredUnits[0]).Left).Left).Carrier;
        y = Assert.IsType<CarrierLocation>(R(R(restoredUnits[1]).Left).Left).Carrier;
        holder = (Carrier)R(restoredCoordinates).Right;
        Assert.True(ComplexFrame.TryFromContour(Group(Result(quarters), fixture[8]), out var circular));
        var basis = Result(circular); Assert.True(ComplexFrame.TryReadBasis(basis));
        Assert.False(Axes(basis)[0].IsInverse); Assert.False(Axes(basis)[1].IsInverse);
        var links = F(F(circular)[4]); var bindings = F(F(F(F(circular)[1])[0])[3]);
        for (var i = 0; i < 4; i++)
        { Assert.Same(R(bindings[i]).Left, R(links[i]).Left); Assert.Same(State(basis, i), R(links[i]).Right); }
        var map = Group(new Relation(State(fixture[0], 0), State(basis, 0)), new Relation(State(fixture[0], 1), State(basis, 1)));
        Assert.True(ComplexFrame.TryReexpress(Group(fixture[0], basis, map, fixture[1]), out var a));
        Assert.True(ComplexFrame.TryReexpress(Group(fixture[0], basis, map, fixture[2]), out var b));
        Assert.True(ComplexFrame.TryMultiply(Group(basis, Result(a), Result(b)), out var product)); Pair(Result(product), 7, 22);
        Assert.True(ComplexFrame.TryFractionalPower(Fraction(basis, Value(basis, -1, 0), 1, 2), out var roots)); Candidates(roots, (0, 1), (0, -1));
        Assert.True(ComplexFrame.TryApplyGenerator(Group(basis, Result(a)), out var generated)); Pair(Result(generated), -2, 3);

        // Independently apply the existing geometry reader to the same radial
        // combination: centre + 3*firstRay + 2*secondRay. Shear is x'=x+y.
        var sign = opposite ? -1 : 1;
        var px = 10 + 9 - 8 * sign; var py = 20 + 12 + 6 * sign;
        var qx = 10 - 6 - 12 * sign; var qy = 20 - 8 + 9 * sign;
        var surface = Result(quarters); var firstMarker = R(F(F(surface)[3])[0]).Left;
        Assert.True(ContourRotation.TryTraverse(Group(surface, firstMarker, Relation.Segment(new Carrier(), 0, 1)), out var walk));
        Assert.True(ContourRotation.TryApply(Group(walk, Group(Point(sheared ? px + py : px, py))), out var geometry));
        Pair(F(Result(geometry))[0], sheared ? qx + qy : qx, qy);
        Assert.True(ComplexFrame.TryReadBasis(Restore(basis, 2)));
    }

    [Theory]
    [InlineData("non-quarter")] [InlineData("missing-metric")] [InlineData("unrelated-seam")] [InlineData("missing-ray")]
    public void AClosedFourMarkerContourMustActuallyJustifyTheComplexAction(string fault)
    {
        var authored = GojoSyntax.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "Syntax", "constructed-circle-rotation.gojo"))).Roots;
        Assert.True(ContourConstruction.TryCalibrateQuarters(R(authored[0]), out var quarters));
        var surface = F(Result(quarters)); var bindings = F(surface[3]);
        switch (fault)
        {
            case "missing-metric": surface[2] = new Placeholder(); break;
            case "missing-ray": bindings[2] = new Relation(R(bindings[2]).Left, new Placeholder()); break;
            case "unrelated-seam":
                var origin = Assert.IsType<CarrierLocation>(R(R(surface[0]).Left).Left);
                surface[0] = new Relation(Relation.Point(origin.Carrier, origin.Ticks), R(surface[0]).Right); break;
            case "non-quarter":
                // Same radius and convex cyclic order, but the second ray is
                // not perpendicular to the first. Merely having four is weak.
                var radial = R(R(bindings[1]).Right); var rim = F(radial.Right);
                var x = Assert.IsType<CarrierLocation>(rim[0]).Carrier; var y = Assert.IsType<CarrierLocation>(rim[1]).Carrier;
                var altered = Relation.OrderedGroup((Carrier)R(radial.Right).Right, new CarrierLocation(x, 5), new CarrierLocation(y, 20));
                bindings[1] = new Relation(R(bindings[1]).Left, new Relation(radial.Left, altered)); break;
        }
        surface[3] = Group(bindings);
        if (fault == "non-quarter")
        {
            Assert.True(ContourRotation.TryTraverse(Group(Group(surface), R(bindings[0]).Left, Relation.Segment(new Carrier(), 0, 1)), out var walk));
            Assert.True(ContourRotation.TryTurn(walk, out _));
        }
        Assert.False(ComplexFrame.TryFromContour(Group(Restore(Group(surface), 1), Fixture()[8]), out var read));
        Assert.IsType<Placeholder>(Result(read)); Assert.NotEmpty(F(F(read)[3]));
    }

    [Fact]
    public void CapturedArithmeticDoesNotDriftAndRangeFailureDoesNotWrap()
    {
        var f = Fixture(); var basis = f[0];
        Assert.True(ComplexFrame.TryMultiply(R(f[3]), out var old));
        Assert.IsType<CarrierLocation>(F(f[1])[0]).Tick.Set(4);
        Assert.True(ComplexFrame.TryMultiply(R(f[3]), out var current)); Pair(Result(current), 12, 26); Pair(Result(old), 7, 22);
        Assert.True(ComplexFrame.TryMultiply(R(F(F(old)[4])[0]), out var replay)); Pair(Result(replay), 7, 22);
        const long large = 9007199254740993;
        Assert.True(ComplexFrame.TryMultiply(Group(basis, Value(basis, large, 1), Value(basis, 0, 1)), out var precise)); Pair(Result(precise), -1, large);
        Assert.False(ComplexFrame.TryMultiply(Group(basis, Value(basis, long.MaxValue, 0), Value(basis, 2, 0)), out var overflow));
        Assert.IsType<Placeholder>(Result(overflow)); Assert.NotEmpty(F(F(overflow)[3]));
    }

    private static IElement[] Fixture(int times = 0) => F(Restore(Group(GojoSyntax.Parse(File.ReadAllText(
        Path.Combine(AppContext.BaseDirectory, "Fixtures", "Syntax", "complex-interpretations.gojo"))).Roots.ToArray()), times));
    private static Relation Fraction(IElement basis, IElement value, long p, long q)
    { var count = new Carrier(); return Group(basis, value, Relation.Segment(count, 0, p), Relation.Segment(count, 0, q)); }
    private static Relation Value(IElement basis, long x, long y, long d = 1) => Relation.OrderedGroup((Carrier)R(F(basis)[0]).Right,
        ProjectionResidue.Create(Axes(basis)[0], x, d), ProjectionResidue.Create(Axes(basis)[1], y, d));
    private static Relation Value(IElement basis, long x, IElement y) => Relation.OrderedGroup((Carrier)R(F(basis)[0]).Right,
        new CarrierLocation(Axes(basis)[0], x), y);
    private static Carrier[] Axes(IElement basis) => F(F(basis)[0]).Select(e => Assert.IsType<CarrierLocation>(R(R(e).Left).Left).Carrier).ToArray();
    private static IElement State(IElement basis, int i) => R(F(F(basis)[1])[i]).Left;
    private static Relation Group(params IElement[] f) => Relation.OrderedGroup(new Carrier(), f);
    private static Relation R(IElement e) => Assert.IsType<Relation>(e);
    private static IElement[] F(IElement e) { Assert.True(R(e).TryReadOrderedGroup(out var f)); return f; }
    private static IElement Result(IElement e) => F(e)[2];
    private static IElement Restore(IElement e, int times)
    { for (var i = 0; i < times; i++) e = GojoNumericFormat.Decode(GojoNumericFormat.Encode(e))[0]; return e; }
    private static void Number(IElement e, long n, long d = 1)
    {
        Assert.True(ProjectionResidue.TryRead(e, out _, out var a, out var b));
        Assert.Equal(new BigInteger(n) * b, new BigInteger(a) * d);
    }
    private static void Pair(IElement e, long x, long y, long d = 1) { var f = F(e); Number(f[0], x, d); Number(f[1], y, d); }
    private static void SameNumbers(IElement a, IElement b)
    {
        var x = F(a); var y = F(b);
        for (var i = 0; i < 2; i++)
        { Assert.True(ProjectionResidue.TryRead(y[i], out _, out var n, out var d)); Number(x[i], n, d); }
    }
    private static void Candidates(IElement witness, params (long x, long y)[] expected)
    {
        var candidates = F(F(Result(witness))[1]); Assert.Equal(expected.Length, candidates.Length);
        for (var i = 0; i < expected.Length; i++) Pair(F(candidates[i])[0], expected[i].x, expected[i].y);
    }
}
