using Gojo.Core;
using Gojo.Structures;

namespace Gojo.Tests.Core;

public sealed class UnitQuotientTests
{
    [Fact]
    public void DistancePerTimeTimesTimeExposesDistanceAndKeepsItsCancellationWitness()
    {
        var distance = new Carrier();
        var time = new Carrier();
        var anotherClock = new Carrier(time.UnitShape);
        distance.Set(23);
        time.Set(7);
        var lengthFactor = Relation.Point(distance, 1);
        var elapsedFactor = Relation.Point(anotherClock, 1);
        var perFactor = Relation.Point(time, 1);
        var standard = new Relation(Relation.Point(distance, 1), Relation.Point(time, 1));
        var expression = UnitQuotient.Create(new Carrier(), Group(lengthFactor, elapsedFactor), Group(perFactor), standard);
        var (result, pairs, unresolved) = Cancel(expression, standard);
        Assert.True(UnitQuotient.TryReadNumeratorUnit(result, out var unit));
        Assert.Same(distance.UnitShape, unit);
        var pair = Assert.IsType<Relation>(Assert.Single(pairs));
        Assert.Same(elapsedFactor, pair.Left);
        Assert.Same(perFactor, pair.Right);
        Assert.Empty(Elements(Assert.IsType<Relation>(unresolved.Left)));
        Assert.Empty(Elements(Assert.IsType<Relation>(unresolved.Right)));
        Assert.True(UnitQuotient.TryRead(expression, out var n, out var d, out _));
        Assert.Equal(2, Elements(n).Length);
        Assert.Single(Elements(d));
        Assert.Equal(23UL, distance.Value);
        Assert.Equal(7UL, time.Value);
        Assert.Equal(0, distance.Tension);
        Assert.Equal(0, time.Tension);
    }

    [Fact]
    public void ReciprocalAndUnmatchedDenominatorRemainExpressibleWithoutInventingAUnit()
    {
        var time = new Carrier();
        var standard = Relation.Point(time, 1);
        var reciprocal = UnitQuotient.Create(new Carrier(), Group(), Group(standard), standard);
        var (result, pairs, _) = Cancel(reciprocal, standard);
        Assert.Empty(pairs);
        Assert.False(UnitQuotient.IsDimensionless(result));
        Assert.False(UnitQuotient.TryReadNumeratorUnit(result, out _));
        Assert.True(UnitQuotient.TryRead(result, out var n, out var d, out _));
        Assert.Empty(Elements(n));
        Assert.Same(standard, Assert.Single(Elements(d)));
    }

    [Fact]
    public void ExplicitCommutativeReadPreservesOrderAndMultiplicityOfSurvivingFactors()
    {
        var u = new Carrier();
        var v = new Carrier();
        var w = new Carrier();
        var u1 = Relation.Point(u, 1);
        var u2 = Relation.Point(u, 1);
        var v1 = Relation.Point(v, 1);
        var w1 = Relation.Point(w, 1);
        var denominator = Relation.Point(u, 1);
        var standard = new Relation(u, v);
        var expression = UnitQuotient.Create(new Carrier(), Group(v1, u1, w1, u2), Group(denominator), standard);
        var (result, pairs, _) = Cancel(expression, standard);
        Assert.Single(pairs);
        Assert.True(UnitQuotient.TryRead(result, out var n, out _, out _));
        Assert.Equal(new IElement[] { v1, w1, u2 }, Elements(n));
        Assert.True(UnitQuotient.TryReadNumeratorUnit(result, out var unit));
        Assert.True(unit.TryReadOrthogonalProductOperands(out var factors));
        Assert.True(factors[0].Equals(v.UnitShape));
        Assert.True(factors[1].Equals(w.UnitShape));
        Assert.True(factors[2].Equals(u.UnitShape));
        Assert.False(UnitShape.Product(u.UnitShape, v.UnitShape).CompatibleWith(UnitShape.Product(v.UnitShape, u.UnitShape)));
    }

    [Fact]
    public void OneNumeratorOccurrenceCannotCancelTwoDenominatorOccurrences()
    {
        var u = new Carrier();
        var standard = Relation.Point(u, 1);
        // Reusing one reference in two positions still records two participations.
        var expression = UnitQuotient.Create(new Carrier(), Group(standard), Group(standard, standard), standard);
        var (result, pairs, _) = Cancel(expression, standard);
        Assert.Single(pairs);
        Assert.True(UnitQuotient.TryRead(result, out var n, out var d, out _));
        Assert.Empty(Elements(n));
        Assert.Single(Elements(d));
        Assert.False(UnitQuotient.IsDimensionless(result));
    }

    [Fact]
    public void CompleteUnitCancellationDoesNotClaimNumericDivisionIsDefined()
    {
        var u = new Carrier(); // Its value is zero; a unit read is not division by that value.
        var standard = Relation.Point(u, 1);
        var expression = UnitQuotient.Create(new Carrier(), Group(standard), Group(standard), standard);
        var (result, pairs, _) = Cancel(expression, standard);
        Assert.Single(pairs);
        Assert.True(UnitQuotient.IsDimensionless(result));
        Assert.False(UnitQuotient.TryReadNumeratorUnit(result, out _));
        Assert.Equal(0UL, u.Value);
    }

    [Fact]
    public void CalibrationDifferencesNeedAnAdapterBeforeNativeTicksCancel()
    {
        var km = new Carrier();
        var hundredKm = new Carrier(UnitShape.ResolutionOf(km.UnitShape, 100));
        var litre = new Carrier();
        var standard = new Relation(Relation.Point(litre, 1), Relation.Point(km, 100));
        var expression = UnitQuotient.Create(new Carrier(), Group(Relation.Point(litre, 1), Relation.Point(km, 1)), Group(Relation.Point(hundredKm, 1)), standard);
        var (result, pairs, _) = Cancel(expression, standard);
        Assert.Empty(pairs);
        Assert.False(UnitQuotient.TryReadNumeratorUnit(result, out _));
        Assert.True(UnitQuotient.TryRead(result, out _, out _, out var retained));
        Assert.Same(standard, retained);
    }

    [Fact]
    public void DescriptorsAndOpposedFacesDoNotDisappearThroughDimensionalCancellation()
    {
        var first = new Carrier(UnitShape.DescribedAtom(new Carrier()));
        var second = new Carrier(UnitShape.DescribedAtom(new Carrier()));
        Assert.True(first.UnitShape.AlgebraicallyCompatibleWith(second.UnitShape));
        var standard = Relation.Point(first, 1);
        var expression = UnitQuotient.Create(new Carrier(), Group(standard, Relation.Point(first.Inverse(), 1)), Group(Relation.Point(second, 1), standard), standard);
        var (result, pairs, _) = Cancel(expression, standard);
        Assert.Single(pairs); // Only the exact forward first-unit match.
        Assert.True(UnitQuotient.TryRead(result, out var n, out var d, out _));
        Assert.Single(Elements(n));
        Assert.Single(Elements(d));
    }

    [Fact]
    public void OpenOrUnsupportedFactorsRemainInTheirOriginalRoles()
    {
        var u = new Carrier();
        var open = new Placeholder();
        var unknown = Relation.Point(Carrier.Unknown, 1);
        var nonunit = Relation.Point(u, 0);
        var standard = new Relation(u, open);
        var expression = UnitQuotient.Create(new Carrier(), Group(open, unknown, nonunit), Group(open, unknown), standard);
        var (result, pairs, unresolved) = Cancel(expression, standard);
        Assert.Empty(pairs);
        Assert.False(UnitQuotient.IsDimensionless(result));
        Assert.True(UnitQuotient.TryRead(result, out var n, out var d, out _));
        Assert.Equal(new IElement[] { open, unknown, nonunit }, Elements(n));
        Assert.Equal(new IElement[] { open, unknown }, Elements(d));
        Assert.Equal(Elements(n), Elements(Assert.IsType<Relation>(unresolved.Left)));
        Assert.Equal(Elements(d), Elements(Assert.IsType<Relation>(unresolved.Right)));
    }

    [Fact]
    public void FactorCountsFollowIntegerExponentSubtractionAcrossAnExplicitRead()
    {
        var u = new Carrier();
        var v = new Carrier();
        var standard = new Relation(u, v);
        for (var un = 0; un <= 3; un++)
        for (var ud = 0; ud <= 3; ud++)
        for (var vn = 0; vn <= 3; vn++)
        for (var vd = 0; vd <= 3; vd++)
        {
            var n = Enumerable.Range(0, un).Select(_ => (IElement)Relation.Point(u, 1))
                .Concat(Enumerable.Range(0, vn).Select(_ => (IElement)Relation.Point(v, 1))).ToArray();
            var d = Enumerable.Range(0, vd).Select(_ => (IElement)Relation.Point(v, 1))
                .Concat(Enumerable.Range(0, ud).Select(_ => (IElement)Relation.Point(u, 1))).ToArray();
            var expression = UnitQuotient.Create(new Carrier(), Group(n), Group(d), standard);
            var (result, pairs, _) = Cancel(expression, standard);
            Assert.Equal(Math.Min(un, ud) + Math.Min(vn, vd), pairs.Length);
            Assert.True(UnitQuotient.TryRead(result, out var rn, out var rd, out _));
            Assert.Equal(Math.Max(0, un - ud) + Math.Max(0, vn - vd), Elements(rn).Length);
            Assert.Equal(Math.Max(0, ud - un) + Math.Max(0, vd - vn), Elements(rd).Length);
            Assert.Equal(un - ud, Count(rn, u) - Count(rd, u));
            Assert.Equal(vn - vd, Count(rn, v) - Count(rd, v));
            var (_, furtherPairs, _) = Cancel(result, standard);
            Assert.Empty(furtherPairs);
        }

        static int Count(Relation group, Carrier carrier) => Elements(group).Count(factor =>
            Assert.IsType<Carrier>(Assert.IsType<Relation>(factor).Right).SameIdentityAs(carrier));
    }

    [Fact]
    public void ExtraFieldsAndNestedLookalikesDoNotReadAsASelectedQuotient()
    {
        var u = new Carrier();
        var factors = Group(Relation.Point(u, 1));
        Assert.False(UnitQuotient.TryCancel(Group(factors, factors, u, u), out _));
        var expression = UnitQuotient.Create(new Carrier(), factors, factors, u);
        Assert.False(UnitQuotient.TryCancel(Group(expression), out _));
        Assert.False(UnitQuotient.TryCancel(Group(Relation.Point(u, 1), factors, u), out _));
        var holder = new Carrier();
        var brokenGroup = new Relation(new Relation(Relation.AdditiveAttachment(holder, u), new Placeholder()), holder);
        Assert.False(UnitQuotient.TryCancel(Group(brokenGroup, factors, u), out _));
    }

    private static (Relation Result, IElement[] Pairs, Relation Unresolved) Cancel(Relation expression, IElement standard)
    {
        Assert.True(UnitQuotient.TryCancel(expression, out var witness));
        Assert.True(ReadWitness.TryRead(witness, out var selected, out var read, out var result, out var unresolved, out var provenance));
        Assert.Same(expression, selected);
        Assert.Same(standard, provenance);
        return (Assert.IsType<Relation>(result), Elements(Assert.IsType<Relation>(read)), Assert.IsType<Relation>(unresolved));
    }

    private static Relation Group(params IElement[] factors) => Relation.OrderedGroup(new Carrier(), factors);
    private static IElement[] Elements(Relation group)
    {
        Assert.True(group.TryReadOrderedElementsOn(Assert.IsType<Carrier>(group.Right), out var elements));
        return elements;
    }
}
