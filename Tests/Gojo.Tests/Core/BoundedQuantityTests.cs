using System.Numerics;
using Gojo.Core;
using Gojo.Patterns;
using Gojo.Structures;

namespace Gojo.Tests.Core;

public sealed class BoundedQuantityTests
{
    [Fact]
    public void SharedUnknownCancelsButTwoInstancesOfOneDescriptionDoNot()
    {
        var axis = new Carrier(); var x = new Placeholder(); var y = new Placeholder();
        var description = G(new Placeholder());
        var shared = Expression(axis, 0, Term(axis, axis, x, 90, 110, 1, 1, description),
            Term(axis, axis, x, 90, 110, -1, 1, description));
        Assert.True(BoundedQuantity.TryObserve(shared, out var cancellation));
        AssertBounds(cancellation, axis, 0, 0);
        Assert.False(StructuralPattern.IsComplete(shared));

        var distinct = Expression(axis, 0, Term(axis, axis, x, 90, 110, 1, 1, description),
            Term(axis, axis, y, 90, 110, -1, 1, description));
        Assert.True(BoundedQuantity.TryObserve(distinct, out var difference));
        AssertBounds(difference, axis, -20, 20);
        Assert.True(ReadWitness.TryRead(cancellation, out var selected, out var normalized, out _, out _, out _));
        Assert.Same(shared, selected);
        Assert.True(BoundedQuantity.TryReadAffine(Assert.IsType<Relation>(normalized), out _, out var terms, out var provenance));
        Assert.Same(shared, provenance);
        Assert.True(terms.TryReadOrderedGroup(out var preserved));
        var term = Assert.IsType<Relation>(Assert.Single(preserved));
        Assert.Equal(Relation.Segment(axis, 0, 0), Assert.IsType<Relation>(term.Right).Right);
        Assert.True(Assert.IsType<Relation>(term.Left).TryReadOrderedGroup(out var sample));
        Assert.Same(x, sample[0]);
    }

    [Fact]
    public void RepeatedUnknownDoublesItsVariation()
    {
        var axis = new Carrier(); var x = new Placeholder();
        var expression = Expression(axis, 0, Term(axis, axis, x, 9, 11), Term(axis, axis, x, 9, 11));
        Assert.True(BoundedQuantity.TryObserve(expression, out var witness));
        AssertBounds(witness, axis, 18, 22);
    }

    [Fact]
    public void ObservationDifferenceCancelsSharedQuantityAndBiasButRetainsEventErrors()
    {
        var axis = new Carrier(); var q = new Placeholder(); var bias = new Placeholder();
        var first = Expression(axis, 100, Term(axis, axis, q, -10, 10),
            Term(axis, axis, bias, -5, 5), Term(axis, axis, new Placeholder(), -1, 1));
        var second = Expression(axis, 100, Term(axis, axis, q, -10, 10),
            Term(axis, axis, bias, -5, 5), Term(axis, axis, new Placeholder(), -1, 1));
        Assert.True(BoundedQuantity.TryCombine(Combination(axis, (first, 1), (second, -1)), out var difference));
        AssertBounds(difference, axis, -2, 2);
        Assert.True(BoundedQuantity.TryCombine(Combination(axis, (first, 1), (first, -1)), out var sameEvent));
        AssertBounds(sameEvent, axis, 0, 0);
        Assert.True(ReadWitness.TryRead(difference, out _, out var normalized, out _, out _, out _));
        Assert.True(BoundedQuantity.TryObserve(Assert.IsType<Relation>(normalized), out var reread));
        AssertBounds(reread, axis, -2, 2);
    }

    [Fact]
    public void OpaqueEvidenceNeitherSuppliesMissingValuesNorCreatesAliases()
    {
        var axis = new Carrier(); var slot = new Placeholder();
        var misleadingPrivateEvidence = G(
            BoundedQuantity.Sample(new Carrier(), slot, Relation.Segment(axis, 1000, 2000), new Placeholder()));
        var expression = Expression(axis, 0,
            Term(axis, axis, slot, 9, 11, 1, 1, misleadingPrivateEvidence),
            Term(axis, axis, new Placeholder(), 9, 11, -1, 1, misleadingPrivateEvidence));
        Assert.True(BoundedQuantity.TryObserve(expression, out var witness));
        AssertBounds(witness, axis, -2, 2);
    }

    [Fact]
    public void IntersectedSupportCanDetermineAValueWithoutBindingTheSlot()
    {
        var axis = new Carrier(); var slot = new Placeholder();
        var expression = Expression(axis, 0, Term(axis, axis, slot, 0, 2), Term(axis, axis, slot, 2, 4));
        Assert.True(BoundedQuantity.TryObserve(expression, out var witness));
        AssertBounds(witness, axis, 4, 4);
        Assert.False(StructuralPattern.IsComplete(expression));
    }

    [Fact]
    public void ContradictionIsPreservedBeforeCancellationOrAZeroGain()
    {
        var axis = new Carrier(); var slot = new Placeholder();
        var contradictory = Expression(axis, 0, Term(axis, axis, slot, 0, 2), Term(axis, axis, slot, 3, 4, -1));
        Assert.False(BoundedQuantity.TryObserve(contradictory, out var witness));
        AssertOpen(witness, contradictory);
        var invalidEvenAtZero = Expression(axis, 0, Term(axis, axis, new CarrierLocation(axis, 5), 0, 2, 0));
        Assert.False(BoundedQuantity.TryObserve(invalidEvenAtZero, out _));

        var a = Expression(axis, 0, Term(axis, axis, slot, 0, 2));
        var b = Expression(axis, 0, Term(axis, axis, slot, 3, 4));
        Assert.False(BoundedQuantity.TryCombine(Combination(axis, (a, 1), (b, -1)), out _));
    }

    [Fact]
    public void BindingRefinesTheReadWithoutRewritingTheOriginalPattern()
    {
        var axis = new Carrier(); var slot = new Placeholder();
        var expression = Expression(axis, 0, Term(axis, axis, slot, 9, 11));
        var bound = Assert.IsType<Relation>(StructuralPattern.Apply(expression, new Relation(slot, new CarrierLocation(axis, 10))));
        Assert.True(StructuralPattern.IsComplete(bound));
        Assert.True(BoundedQuantity.TryObserve(bound, out var witness));
        AssertBounds(witness, axis, 10, 10);
        Assert.False(StructuralPattern.IsComplete(expression));
        var outside = Assert.IsType<Relation>(StructuralPattern.Apply(expression, new Relation(slot, new CarrierLocation(axis, 12))));
        Assert.True(StructuralPattern.IsComplete(outside));
        Assert.False(BoundedQuantity.TryObserve(outside, out _));
    }

    [Fact]
    public void FractionalSampleAndSignedCalibrationPreserveExactEndpointsAndOutwardRounding()
    {
        var source = new Carrier(); var output = new Carrier();
        var sample = BoundedQuantity.Sample(new Carrier(), new Placeholder(), Relation.Segment(source, 4, -3), G());
        var projection = new Relation(Relation.Segment(source, 10, -10), Relation.Segment(output, -5, 5));
        var expression = Expression(output, 0, new Relation(sample, projection));
        Assert.True(BoundedQuantity.TryObserve(expression, out var witness));
        AssertBounds(witness, output, -2, 1, 3, 2);
        Assert.True(BoundedQuantity.TryReadBounds(witness, out _, out _, out _, out var enclosure));
        Assert.Equal(Relation.Segment(output, -2, 2), enclosure);
        Assert.True(ReadWitness.TryRead(witness, out _, out _, out _, out var residue, out _));
        Assert.True(Assert.IsType<Relation>(residue).TryReadOrderedGroup(out var ends));
        AssertScalar(ends[0], output, 0, 1); AssertScalar(ends[1], output, 1, 2);

        var exact = Expression(source, 0, Term(source, source, ProjectionResidue.Create(source, 3, 2), 1, 2));
        Assert.True(BoundedQuantity.TryObserve(exact, out var exactWitness));
        AssertBounds(exactWitness, source, 3, 2, 3, 2);
    }

    [Fact]
    public void NegativeFractionalBoundsRoundAwayFromTheIntervalInterior()
    {
        var axis = new Carrier();
        var expression = Expression(axis, 0, Term(axis, axis, new Placeholder(), -2, -1, 1, 3));
        Assert.True(BoundedQuantity.TryObserve(expression, out var witness));
        AssertBounds(witness, axis, -2, 3, -1, 3);
        Assert.True(BoundedQuantity.TryReadBounds(witness, out _, out _, out _, out var enclosure));
        Assert.Equal(Relation.Segment(axis, -1, 0), enclosure);
    }

    [Fact]
    public void CompatibleUnitsDoNotSupplyMissingSourceIdentityOrCalibration()
    {
        var first = new Carrier(); var other = new Carrier(first.UnitShape); var output = new Carrier();
        var slot = new Placeholder();
        Assert.False(BoundedQuantity.TryObserve(Expression(output, 0,
            Term(first, output, slot, 1, 2), Term(other, output, slot, 1, 2)), out _));
        Assert.True(BoundedQuantity.TryObserve(Expression(output, 0,
            Term(first, output, slot, 1, 2), Term(other, output, new Placeholder(), 1, 2)), out var independent));
        AssertBounds(independent, output, 2, 4);
        var left = Expression(first, 0, Term(first, first, new Placeholder(), 1, 2));
        Assert.False(BoundedQuantity.TryCombine(new Relation(G(new Relation(left,
            new Relation(Relation.Segment(other, 0, 1), Relation.Segment(output, 0, 1)))),
            Relation.Point(output, 1)), out _));
    }

    [Fact]
    public void PartialAndMalformedContributionsDoNotProduceFalseBounds()
    {
        var axis = new Carrier(); var sample = BoundedQuantity.Sample(new Carrier(), new Placeholder(), Relation.Segment(axis, 1, 2), G());
        var calibration = new Relation(Relation.Segment(axis, 0, 1), Relation.Segment(axis, 0, 1));
        var invalid = new[]
        {
            Expression(axis, 0, new Relation(sample, new Placeholder())),
            Expression(axis, 0, new Relation(sample, new Relation(Relation.Segment(axis, 0, 0), Relation.Segment(axis, 0, 1)))),
            Expression(axis, 0, new Relation(G(sample), calibration)),
            Expression(axis, 0, Term(axis, axis, new Placeholder().Dual(), 1, 2)),
            Expression(axis, 0, Term(axis, axis, new CarrierLocation(Carrier.Unknown, 1), 1, 2)),
            Expression(axis.Inverse(), 0),
            G(new CarrierLocation(axis, 0), G(), G(), G()),
            G(new CarrierLocation(axis, 0), G(new Relation(new Placeholder(), calibration)), G())
        };
        foreach (var expression in invalid)
        {
            Assert.False(BoundedQuantity.TryObserve(expression, out var witness));
            AssertOpen(witness, expression);
        }
    }

    [Fact]
    public void WideIntermediatesCanCancelButUnrepresentableStoredResultsCannotSaturate()
    {
        var axis = new Carrier(); var slot = new Placeholder();
        var expression = Expression(axis, 0, Term(axis, axis, slot, long.MaxValue - 1, long.MaxValue, long.MaxValue),
            Term(axis, axis, slot, long.MaxValue - 1, long.MaxValue, -long.MaxValue));
        Assert.True(BoundedQuantity.TryObserve(expression, out var witness));
        AssertBounds(witness, axis, 0, 0);
        var sample = BoundedQuantity.Sample(new Carrier(), slot, Relation.Segment(axis, -5, 5), G());
        var wideIdentity = new Relation(Relation.Segment(axis, long.MinValue, long.MaxValue),
            Relation.Segment(axis, long.MinValue, long.MaxValue));
        Assert.True(BoundedQuantity.TryObserve(Expression(axis, 0, new Relation(sample, wideIdentity)), out var identity));
        AssertBounds(identity, axis, -5, 5);
        var tooLarge = Expression(axis, long.MaxValue, Term(axis, axis, slot, 1, 1));
        Assert.Throws<OverflowException>(() => BoundedQuantity.TryObserve(tooLarge, out _));
        Assert.Equal(0UL, axis.Value); Assert.Equal(0, axis.Tension);
    }

    [Fact]
    public void ProductsKeepUnitsAndDistinguishASquareFromTwoLookalikeSamples()
    {
        var axis = new Carrier(); var output = new Carrier(UnitShape.OrthogonalProduct(axis.UnitShape, axis.UnitShape));
        var x = Expression(axis, 0, Term(axis, axis, new Placeholder(), -2, 3));
        var y = Expression(axis, 0, Term(axis, axis, new Placeholder(), -2, 3));
        Assert.True(BoundedQuantity.TryMultiply(Product(output, x, x), out var square));
        AssertBounds(square, output, 0, 9);
        Assert.True(BoundedQuantity.TryMultiply(Product(output, x, y), out var product));
        AssertBounds(product, output, -6, 9);
        Assert.False(BoundedQuantity.TryMultiply(Product(axis, x, x), out _));
        var w = Expression(axis, 0, Term(axis, axis, new Placeholder(), 4, 6));
        var h = Expression(axis, 0, Term(axis, axis, new Placeholder(), 9, 11));
        Assert.True(BoundedQuantity.TryMultiply(Product(output, w, h), out var area));
        AssertBounds(area, output, 36, 66);
    }

    [Fact]
    public void NonlinearEnclosureRetainsTheRelationWithoutInventingAnIndependentAffineCause()
    {
        var axis = new Carrier(); var slot = new Placeholder();
        var output = new Carrier(UnitShape.OrthogonalProduct(axis.UnitShape, axis.UnitShape));
        var a = Expression(axis, 1, Term(axis, axis, slot, -1, 1));
        var b = Expression(axis, 1, Term(axis, axis, slot, -1, 1, -1));
        var request = Product(output, a, b);
        Assert.True(BoundedQuantity.TryMultiply(request, out var witness));
        // The exact range is [0,1]. This first product read promises a safe
        // enclosure and retains its dependency for a future tighter reader.
        AssertBounds(witness, output, 0, 4);
        Assert.True(ReadWitness.TryRead(witness, out var selected, out var surface, out _, out _, out _));
        Assert.Same(request, selected); Assert.Same(request, surface);
        Assert.False(BoundedQuantity.TryReadAffine(Assert.IsType<Relation>(surface), out _, out _, out _));
    }

    [Fact]
    public void DivisionChecksZeroDomainBeforeSharedQuantityCancellation()
    {
        var axis = new Carrier(); var standard = G(Relation.Point(axis, 1));
        var unitExpression = UnitQuotient.Create(new Carrier(), G(Relation.Point(axis, 1)), G(Relation.Point(axis, 1)), standard);
        Assert.True(UnitQuotient.TryReadUnit(unitExpression, out var unit));
        var output = new Carrier(unit);
        var zeroCrossing = Expression(axis, 0, Term(axis, axis, new Placeholder(), -1, 1));
        var request = G(zeroCrossing, zeroCrossing, output, standard);
        Assert.False(BoundedQuantity.TryDivide(request, out var unresolved));
        AssertOpen(unresolved, request);
        var positive = Expression(axis, 0, Term(axis, axis, new Placeholder(), 1, 2));
        Assert.True(BoundedQuantity.TryDivide(G(positive, positive, output, standard), out var identity));
        AssertBounds(identity, output, 1, 1);
        Assert.False(BoundedQuantity.TryDivide(G(positive, positive, axis, standard), out _));
        var numerator = Expression(axis, 0, Term(axis, axis, new Placeholder(), 5, 7));
        var denominator = Expression(axis, 0, Term(axis, axis, new Placeholder(), 2, 3));
        Assert.True(BoundedQuantity.TryDivide(G(numerator, denominator, output, standard), out var divided));
        AssertBounds(divided, output, 5, 3, 7, 2);
    }

    [Fact]
    public void SharedConstraintsAreCheckedAcrossProductAndQuotientInputs()
    {
        var axis = new Carrier(); var slot = new Placeholder(); var standard = G();
        var a = Expression(axis, 0, Term(axis, axis, slot, 1, 2));
        var b = Expression(axis, 0, Term(axis, axis, slot, 3, 4));
        var product = new Carrier(UnitShape.OrthogonalProduct(axis.UnitShape, axis.UnitShape));
        Assert.False(BoundedQuantity.TryMultiply(Product(product, a, b), out _));
        Assert.True(UnitQuotient.TryReadUnit(UnitQuotient.Create(new Carrier(), G(Relation.Point(axis, 1)),
            G(Relation.Point(axis, 1)), standard), out var unit));
        Assert.False(BoundedQuantity.TryDivide(G(a, b, new Carrier(unit), standard), out _));
    }

    [Theory]
    [InlineData(10, 30, 0, 0)]
    [InlineData(30, 10, 0, 0)]
    [InlineData(15, 25, 0, 5)]
    [InlineData(40, 50, 10, 30)]
    [InlineData(-20, 0, 10, 30)]
    public void ConformanceMeasuresViolationInsteadOfTryingToEliminateDesiredClearance(long a, long b, long lo, long hi)
    {
        var axis = new Carrier();
        var clearance = Expression(axis, 0, Term(axis, axis, new Placeholder(), 1010, 1020),
            Term(axis, axis, new Placeholder(), 990, 1000, -1));
        Assert.True(BoundedQuantity.TryObserve(clearance, out var observation));
        AssertBounds(observation, axis, 10, 30);
        var permitted = Relation.Segment(axis, a, b);
        Assert.True(BoundedQuantity.TryObserveConformance(G(observation, permitted, new Placeholder()), out var witness));
        AssertBounds(witness, axis, lo, hi);
        Assert.Equal(0UL, axis.Value); Assert.Equal(0, axis.Tension);
    }

    [Fact]
    public void AnOpenQuantityOrMalformedBoundsCannotVacuouslyPassConformance()
    {
        var axis = new Carrier(); var expression = Expression(axis, 0, new Placeholder());
        Assert.False(BoundedQuantity.TryObserve(expression, out var observation));
        Assert.False(BoundedQuantity.TryObserveConformance(G(observation, Relation.Segment(axis, -100, 100), G()), out _));
        var malformed = ReadWitness.Create(new Carrier(), expression, expression,
            G(new CarrierLocation(axis, 5), new CarrierLocation(axis, 2), Relation.Segment(axis, 5, 2)), G(), G());
        Assert.False(BoundedQuantity.TryReadBounds(malformed, out _, out _, out _, out _));
        Assert.False(BoundedQuantity.TryObserveConformance(G(malformed, Relation.Segment(axis, -100, 100), G()), out _));
        Assert.True(BoundedQuantity.TryObserve(Expression(axis, 2), out var exact));
        Assert.False(BoundedQuantity.TryObserveConformance(G(exact, Relation.Segment(new Carrier(axis.UnitShape), 1, 3), G()), out _));
    }

    [Fact]
    public void SignedCalibrationMatrixReconstructsItsExtremaWithExactRationalBounds()
    {
        var source = new Carrier(); var output = new Carrier();
        foreach (var a in Enumerable.Range(-2, 5))
        foreach (var b in Enumerable.Range(-2, 5).Where(b => b != a))
        foreach (var c in Enumerable.Range(-2, 5))
        foreach (var d in Enumerable.Range(-2, 5))
        {
            var sample = BoundedQuantity.Sample(new Carrier(), new Placeholder(), Relation.Segment(source, -3, 4), G());
            var expression = Expression(output, 1, new Relation(sample,
                new Relation(Relation.Segment(source, a, b), Relation.Segment(output, c, d))));
            Assert.True(BoundedQuantity.TryObserve(expression, out var witness));
            Assert.True(BoundedQuantity.TryReadBounds(witness, out _, out var lo, out var hi, out _));
            var n0 = (long)(1+c)*(b-a) + (-3-a)*(d-c);
            var n1 = (long)(1+c)*(b-a) + (4-a)*(d-c);
            var den = b-a;
            if (den < 0) { den = -den; n0 = -n0; n1 = -n1; }
            AssertScalar(lo, output, Math.Min(n0,n1), den);
            AssertScalar(hi, output, Math.Max(n0,n1), den);
        }
    }

    [Fact]
    public void CorrelatedProductAndQuotientEnclosuresContainEverySampleInASignedMatrix()
    {
        var axis = new Carrier(); var x = new Placeholder(); var y = new Placeholder();
        var productBasis = new Carrier(UnitShape.OrthogonalProduct(axis.UnitShape, axis.UnitShape));
        var standard = G(Relation.Point(axis, 1));
        Assert.True(UnitQuotient.TryReadUnit(UnitQuotient.Create(new Carrier(), G(Relation.Point(axis, 1)),
            G(Relation.Point(axis, 1)), standard), out var quotientUnit));
        var quotientBasis = new Carrier(quotientUnit);
        foreach (var i in Enumerable.Range(-2, 5))
        foreach (var j in Enumerable.Range(-2, 5))
        {
            var a = Expression(axis, 3, Term(axis, axis, x, -2, 2, i), Term(axis, axis, y, -1, 3));
            var b = Expression(axis, 10, Term(axis, axis, x, -2, 2), Term(axis, axis, y, -1, 3, j));
            Assert.True(BoundedQuantity.TryMultiply(Product(productBasis, a, b), out var product));
            Assert.True(BoundedQuantity.TryDivide(G(a, b, quotientBasis, standard), out var quotient));
            foreach (var sx in Enumerable.Range(-2, 5))
            foreach (var sy in Enumerable.Range(-1, 5))
            {
                var av = 3 + i*sx + sy; var bv = 10 + sx + j*sy;
                AssertContained(product, av*bv, 1);
                AssertContained(quotient, av, bv);
            }
        }
    }

    [Fact]
    public void AnAcceptableKnownErrorRemainsAvailableToAStricterQuestion()
    {
        var axis = new Carrier();
        Assert.True(BoundedQuantity.TryObserve(Expression(axis, 2), out var knownError));
        Assert.True(BoundedQuantity.TryObserveConformance(G(knownError, Relation.Segment(axis, -3, 3), G()), out var accepted));
        AssertBounds(accepted, axis, 0, 0);
        Assert.True(BoundedQuantity.TryObserveConformance(G(knownError, Relation.Segment(axis, 0, 0), G()), out var strict));
        AssertBounds(strict, axis, 2, 2);
        AssertBounds(knownError, axis, 2, 2);
    }

    private static Relation G(params IElement[] fields) => Relation.OrderedGroup(new Carrier(), fields);
    private static Relation Expression(Carrier axis, long offset, params IElement[] terms) =>
        BoundedQuantity.Affine(new Carrier(), new CarrierLocation(axis, offset), G(terms), G());
    private static Relation Term(Carrier source, Carrier output, IElement slot, long lower, long upper,
        long numerator = 1, long denominator = 1, IElement? evidence = null) =>
        new(BoundedQuantity.Sample(new Carrier(), slot, Relation.Segment(source, lower, upper), evidence ?? G()),
            new Relation(Relation.Segment(source, 0, denominator), Relation.Segment(output, 0, numerator)));
    private static Relation Combination(Carrier output, params (Relation Source, long Gain)[] terms) =>
        new(G(terms.Select(term => (IElement)new Relation(term.Source,
            new Relation(Relation.Segment(output, 0, 1), Relation.Segment(output, 0, term.Gain)))).ToArray()),
            Relation.Point(output, 1));
    private static Relation Product(Carrier output, Relation left, Relation right) =>
        new(G(left, right), Relation.Segment(output, 0, 1));

    private static void AssertBounds(Relation witness, Carrier axis, long lower, long upper) =>
        AssertBounds(witness, axis, lower, 1, upper, 1);
    private static void AssertBounds(Relation witness, Carrier axis, long lowerN, long lowerD, long upperN, long upperD)
    {
        Assert.True(BoundedQuantity.TryReadBounds(witness, out var basis, out var lower, out var upper, out _));
        Assert.Same(axis, basis);
        AssertScalar(lower, axis, lowerN, lowerD); AssertScalar(upper, axis, upperN, upperD);
    }
    private static void AssertScalar(IElement element, Carrier axis, long expectedN, long expectedD)
    {
        Assert.True(ProjectionResidue.TryRead(element, out var basis, out var n, out var d));
        Assert.Same(axis, basis);
        Assert.Equal((BigInteger)expectedN * d, (BigInteger)n * expectedD);
    }
    private static void AssertContained(Relation witness, long numerator, long denominator)
    {
        Assert.True(denominator > 0);
        Assert.True(BoundedQuantity.TryReadBounds(witness, out _, out var lower, out var upper, out _));
        Assert.True(ProjectionResidue.TryRead(lower, out _, out var ln, out var ld));
        Assert.True(ProjectionResidue.TryRead(upper, out _, out var un, out var ud));
        Assert.True((BigInteger)numerator * ld >= (BigInteger)ln * denominator);
        Assert.True((BigInteger)numerator * ud <= (BigInteger)un * denominator);
    }
    private static void AssertOpen(Relation witness, IElement request)
    {
        Assert.True(ReadWitness.TryRead(witness, out var selected, out _, out var result, out var residue, out _));
        Assert.Same(request, selected); Assert.Same(request, residue);
        Assert.IsType<Placeholder>(result);
        Assert.False(BoundedQuantity.TryReadBounds(witness, out _, out _, out _, out _));
    }
}
