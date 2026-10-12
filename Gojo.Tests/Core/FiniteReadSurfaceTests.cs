using Gojo.Core;
using Gojo.Structures;

namespace Gojo.Tests.Core;

public sealed class FiniteReadSurfaceTests
{
    [Fact]
    public void ExactBoundaryAndClippedBoundaryHaveDifferentSupport()
    {
        var axis = new Carrier();
        var capacity = Capacity(axis, 0, 15);
        var exact = Publish(capacity, axis, 15);
        var clipped = Publish(capacity, axis, 17);

        AssertRead(exact, axis, 15, 15, 15);
        AssertRead(clipped, axis, 15, 15, null);
        Assert.True(FiniteReadSurface.TryGreaterThan(exact, 15, out var exactAnswer));
        Assert.False(exactAnswer);
        Assert.False(FiniteReadSurface.TryGreaterThan(clipped, 15, out _));
        Assert.True(FiniteReadSurface.TryGreaterThan(clipped, 14, out var clippedAnswer));
        Assert.True(clippedAnswer);
    }

    [Fact]
    public void SaturationUncertaintySurvivesSubtractionAndPreservesEvaluationOrder()
    {
        var axis = new Carrier();
        var capacity = Capacity(axis, 0, 15);
        var fourteen = Publish(capacity, axis, 14);
        var three = Publish(capacity, axis, 3);
        var saturated = FiniteReadSurface.Add(new Carrier(), capacity, fourteen, three);
        var afterLoss = FiniteReadSurface.Subtract(new Carrier(), capacity, saturated, three);
        var cancelledFirst = FiniteReadSurface.Subtract(new Carrier(), capacity, three, three);
        var otherOrder = FiniteReadSurface.Add(new Carrier(), capacity, fourteen, cancelledFirst);

        AssertRead(saturated, axis, 15, 15, null);
        AssertRead(afterLoss, axis, 12, 12, null);
        AssertRead(otherOrder, axis, 14, 14, 14);
        Assert.False(FiniteReadSurface.TryGreaterThan(afterLoss, 13, out _));
        Assert.True(FiniteReadSurface.TryGreaterThan(afterLoss, 11, out var proven));
        Assert.True(proven);
    }

    [Fact]
    public void LowerTailRemainsUnknownAfterAddingBackCapacity()
    {
        var axis = new Carrier();
        var capacity = Capacity(axis, -15, 15);
        var clipped = Publish(capacity, axis, -18);
        var restored = FiniteReadSurface.Add(new Carrier(), capacity, clipped, Publish(capacity, axis, 3));

        AssertRead(clipped, axis, -15, null, -15);
        AssertRead(restored, axis, -12, null, -12);
        Assert.True(FiniteReadSurface.TryGreaterThan(restored, -12, out var answer));
        Assert.False(answer);
        Assert.False(FiniteReadSurface.TryGreaterThan(restored, -13, out _));
    }

    [Fact]
    public void SharedObservationCancelsButIndependentClippedPublicationsDoNot()
    {
        var axis = new Carrier();
        var capacity = Capacity(axis, -15, 15);
        var sharedEvidence = new Placeholder();
        var a = Publish(capacity, axis, 19, sharedEvidence);
        var b = Publish(capacity, axis, 19, sharedEvidence);

        AssertRead(FiniteReadSurface.Subtract(new Carrier(), capacity, a, a), axis, 0, 0, 0);
        var independent = FiniteReadSurface.Subtract(new Carrier(), capacity, a, b);
        AssertRead(independent, axis, 0, null, null);
        Assert.False(FiniteReadSurface.TryGreaterThan(independent, 0, out _));
    }

    [Theory]
    [InlineData(10, 8, 8, 11)]
    [InlineData(-10, -8, -11, -8)]
    [InlineData(0, 0, -3, 3)]
    public void DeliberateCoarseningPublishesAWholeCellRatherThanThePrivateRemainder(
        long input, long nominal, long lower, long upper)
    {
        var axis = new Carrier();
        var capacity = Capacity(axis, -15, 15, 4);
        var read = Publish(capacity, axis, input);

        AssertRead(read, axis, nominal, lower, upper);
        Assert.False(FiniteReadSurface.TryGreaterThan(read, lower, out _));
        Assert.True(FiniteReadSurface.TryGreaterThan(read, upper, out var aboveCell));
        Assert.False(aboveCell);
    }

    [Fact]
    public void ClippedBoundaryIsRetainedEvenWhenItIsNotAQuantumMultiple()
    {
        var axis = new Carrier();
        var capacity = Capacity(axis, -15, 15, 4);

        AssertRead(Publish(capacity, axis, 20), axis, 15, 15, null);
        AssertRead(Publish(capacity, axis, -20), axis, -15, null, -15);
    }

    [Fact]
    public void CoarseClosedProductEnclosesEveryPossibleWholeValue()
    {
        var axis = new Carrier();
        var coarse = Capacity(axis, -127, 127, 4);
        var fine = Capacity(axis, -127, 127);
        var a = Publish(coarse, axis, 10);
        var b = Publish(coarse, axis, -10);
        var product = FiniteReadSurface.Multiply(new Carrier(), fine, a, b);

        AssertRead(product, axis, -64, -121, -64);
        for (long av = 8; av <= 11; av++)
        for (long bv = -11; bv <= -8; bv++)
            AssertContains(product, av * bv);
    }

    [Fact]
    public void ExactZeroEliminatesAnOpenFiniteQuantityButDoesNotInventItsValue()
    {
        var axis = new Carrier();
        var capacity = Capacity(axis, -15, 15);
        var unknownHigh = Publish(capacity, axis, 30);
        var zero = Publish(capacity, axis, 0);

        AssertRead(FiniteReadSurface.Multiply(new Carrier(), capacity, unknownHigh, zero), axis, 0, 0, 0);
        AssertRead(FiniteReadSurface.Multiply(new Carrier(), capacity, zero, unknownHigh), axis, 0, 0, 0);
        AssertRead(unknownHigh, axis, 15, 15, null);
    }

    [Fact]
    public void CoarseConsumerUsesClippedProductSupportWithoutRecoveringItsPrivateAnswer()
    {
        var axis = new Carrier();
        var capacity = Capacity(axis, 0, 15);
        var product = FiniteReadSurface.Multiply(new Carrier(), capacity,
            Publish(capacity, axis, 12), Publish(capacity, axis, 14));

        AssertRead(product, axis, 15, 15, null);
        Assert.True(FiniteReadSurface.TryGreaterThan(product, 10, out var sufficient));
        Assert.True(sufficient);
        Assert.False(FiniteReadSurface.TryGreaterThan(product, 20, out _));
        // Native serialization keeps the published loss, not a hidden exact product.
        var restored = (Relation)GojoEncodedSnapshot.CaptureArchive(product).RestoreRoots()[0];
        Assert.True(FiniteReadSurface.TryReadValue(restored, out var value));
        Assert.Equal(15, value.Ticks);
        Assert.True(FiniteReadSurface.TryReadSupport(restored, out var lower, out var upper));
        Assert.Equal(15, lower?.Ticks);
        Assert.Null(upper);
        Assert.False(FiniteReadSurface.TryGreaterThan(restored, 20, out _));
    }

    [Fact]
    public void MaximumQuantumCellCanBePublishedAcrossBothHostExtremes()
    {
        var axis = new Carrier();
        var capacity = Capacity(axis, long.MinValue, long.MaxValue, long.MaxValue);
        AssertRead(Publish(capacity, axis, long.MinValue), axis, -long.MaxValue, long.MinValue, -long.MaxValue);
        AssertRead(Publish(capacity, axis, long.MaxValue), axis, long.MaxValue, long.MaxValue, long.MaxValue);
        AssertRead(Publish(capacity, axis, 1), axis, 0, -long.MaxValue + 1, long.MaxValue - 1);
    }

    [Fact]
    public void AnOpenProductKeepsUncertaintyWhenItsNominalFits()
    {
        var axis = new Carrier();
        var capacity = Capacity(axis, -15, 15);
        var highTail = Publish(capacity, axis, 25);
        var negativeOne = Publish(capacity, axis, -1);
        var product = FiniteReadSurface.Multiply(new Carrier(), capacity, highTail, negativeOne);

        AssertRead(product, axis, -15, null, null);
        Assert.False(FiniteReadSurface.TryGreaterThan(product, -15, out _));
        AssertContains(product, -25);
    }

    [Fact]
    public void FinerReceiverDoesNotRecoverDetailFromPrivatePublicationEvidence()
    {
        var axis = new Carrier();
        var privateInput = new CarrierLocation(axis, 10);
        var coarse = Capacity(axis, -15, 15, 4);
        var fine = Capacity(axis, -15, 15);
        var published = FiniteReadSurface.Publish(new Carrier(), coarse, privateInput, privateInput);
        var received = FiniteReadSurface.Add(new Carrier(), fine, published, Publish(fine, axis, 0));

        AssertRead(published, axis, 8, 8, 11);
        AssertRead(received, axis, 8, 8, 11);
        Assert.False(FiniteReadSurface.TryGreaterThan(received, 9, out _));
        Assert.Equal(10, privateInput.Ticks);
    }

    [Theory]
    [InlineData(long.MaxValue, 1, long.MaxValue, long.MaxValue, null)]
    [InlineData(long.MinValue, -1, long.MinValue, null, long.MinValue)]
    [InlineData(long.MinValue, long.MinValue, long.MinValue, null, long.MinValue)]
    [InlineData(long.MaxValue, long.MaxValue, long.MaxValue, long.MaxValue, null)]
    public void ExtremeAdditionsClipWithoutWrappingOrLosingTheirTail(
        long a, long b, long nominal, long? lower, long? upper)
    {
        var axis = new Carrier();
        var capacity = Capacity(axis, long.MinValue, long.MaxValue);
        AssertRead(FiniteReadSurface.Add(new Carrier(), capacity,
            Publish(capacity, axis, a), Publish(capacity, axis, b)), axis, nominal, lower, upper);
    }

    [Theory]
    [InlineData(long.MinValue, 1, long.MinValue, null, long.MinValue)]
    [InlineData(long.MaxValue, -1, long.MaxValue, long.MaxValue, null)]
    [InlineData(long.MaxValue, long.MinValue, long.MaxValue, long.MaxValue, null)]
    [InlineData(long.MinValue, long.MaxValue, long.MinValue, null, long.MinValue)]
    [InlineData(long.MinValue, long.MinValue, 0L, 0L, 0L)]
    public void ExtremeSubtractionsDoNotNegateOrWidenBeforeClipping(
        long a, long b, long nominal, long? lower, long? upper)
    {
        var axis = new Carrier();
        var capacity = Capacity(axis, long.MinValue, long.MaxValue);
        AssertRead(FiniteReadSurface.Subtract(new Carrier(), capacity,
            Publish(capacity, axis, a), Publish(capacity, axis, b)), axis, nominal, lower, upper);
    }

    [Theory]
    [InlineData(long.MinValue, -1, long.MaxValue, long.MaxValue, null)]
    [InlineData(long.MinValue, long.MinValue, long.MaxValue, long.MaxValue, null)]
    [InlineData(long.MaxValue, long.MaxValue, long.MaxValue, long.MaxValue, null)]
    [InlineData(long.MinValue, 2, long.MinValue, null, long.MinValue)]
    [InlineData(long.MaxValue, -2, long.MinValue, null, long.MinValue)]
    [InlineData(long.MaxValue, -1, -long.MaxValue, -long.MaxValue, -long.MaxValue)]
    [InlineData(long.MinValue, 1, long.MinValue, long.MinValue, long.MinValue)]
    [InlineData(long.MinValue, 0L, 0L, 0L, 0L)]
    [InlineData(0L, long.MaxValue, 0L, 0L, 0L)]
    public void ExtremeProductsPublishFiniteValuesWithoutWideRescue(
        long a, long b, long nominal, long? lower, long? upper)
    {
        var axis = new Carrier();
        var capacity = Capacity(axis, long.MinValue, long.MaxValue);
        AssertRead(FiniteReadSurface.Multiply(new Carrier(), capacity,
            Publish(capacity, axis, a), Publish(capacity, axis, b)), axis, nominal, lower, upper);
    }

    [Fact]
    public void SmallSignedArithmeticClipsCorrectlyAndNeverClaimsAwayTheActualResult()
    {
        var axis = new Carrier();
        var capacity = Capacity(axis, -7, 7);
        var values = Enumerable.Range(-7, 15).Select(n => Publish(capacity, axis, n)).ToArray();
        for (var a = -7; a <= 7; a++)
        for (var b = -7; b <= 7; b++)
        {
            Check(FiniteReadSurface.Add(new Carrier(), capacity, values[a + 7], values[b + 7]), a + b);
            Check(FiniteReadSurface.Subtract(new Carrier(), capacity, values[a + 7], values[b + 7]), a - b);
            Check(FiniteReadSurface.Multiply(new Carrier(), capacity, values[a + 7], values[b + 7]), a * b);
        }

        void Check(Relation read, long actual)
        {
            var nominal = Math.Clamp(actual, -7, 7);
            AssertRead(read, axis, nominal, actual < -7 ? null : nominal, actual > 7 ? null : nominal);
            AssertContains(read, actual);
        }
    }

    [Fact]
    public void PendingSuppliersAreNotEvaluatedWhilePublicationEvidenceIsOpaque()
    {
        var axis = new Carrier();
        var capacity = Capacity(axis, -15, 15);
        var a = Publish(capacity, axis, 3);
        var b = Publish(capacity, axis, 2);
        var pending = QualifiedReadSurface.Add(new Carrier(), a, b);
        Assert.True(QualifiedReadSurface.TryReadValue(pending, out var supplied));
        Assert.Equal(5, supplied.Ticks);
        Assert.False(QualifiedReadSurface.TryReadPublishedValue(pending, out _));

        AssertUnresolved(FiniteReadSurface.Add(new Carrier(), capacity, pending, a));
        AssertUnresolved(FiniteReadSurface.Subtract(new Carrier(), capacity, pending, pending));
        AssertUnresolved(FiniteReadSurface.Multiply(new Carrier(), capacity, pending, Publish(capacity, axis, 0)));

        var materialized = Publish(capacity, axis, 7, pending);
        AssertRead(materialized, axis, 7, 7, 7);
        Assert.True(QualifiedReadSurface.TryReadPublishedValue(materialized, out var retained));
        Assert.Equal(7, retained.Ticks);
        Assert.True(ReadWitness.TryRead(pending, out _, out _, out var unchanged, out _, out _));
        Assert.IsType<Placeholder>(unchanged);
    }

    [Fact]
    public void IncompatibleIdentityOrCapacityDoesNotBecomeASpuriousZero()
    {
        var axis = new Carrier();
        var other = new Carrier(axis.UnitShape);
        var selectedCapacity = Capacity(axis, -7, 7);
        var wideCapacity = Capacity(axis, -15, 15);
        var foreignCapacity = Capacity(other, -7, 7);
        var known = Publish(selectedCapacity, axis, 3);
        var foreign = Publish(foreignCapacity, other, 2);
        var tooWide = Publish(wideCapacity, axis, 10);
        var supportTooWide = Publish(Capacity(axis, -15, 15, 5), axis, 7);

        AssertUnresolved(FiniteReadSurface.Publish(new Carrier(), selectedCapacity,
            new CarrierLocation(other, 2), new Placeholder()));
        AssertUnresolved(FiniteReadSurface.Add(new Carrier(), selectedCapacity, known, foreign));
        AssertUnresolved(FiniteReadSurface.Add(new Carrier(), selectedCapacity, known, tooWide));
        AssertUnresolved(FiniteReadSurface.Add(new Carrier(), selectedCapacity, known, supportTooWide));
    }

    [Theory]
    [InlineData(1, 7, 1)]
    [InlineData(-7, -1, 1)]
    [InlineData(7, -7, 1)]
    [InlineData(-7, 7, 0)]
    [InlineData(-7, 7, -1)]
    public void InvalidCapacityRemainsAnUnresolvedNativeRead(long minimum, long maximum, long quantum)
    {
        var axis = new Carrier();
        var capacity = Capacity(axis, minimum, maximum, quantum);
        AssertUnresolved(Publish(capacity, axis, 0));
    }

    [Fact]
    public void MissingOrUnknownInputBasisRemainsAnUnresolvedNativeRead()
    {
        var axis = new Carrier();
        var capacity = Capacity(axis, -7, 7);
        AssertUnresolved(FiniteReadSurface.Publish(new Carrier(), capacity, default, new Placeholder()));
        AssertUnresolved(FiniteReadSurface.Publish(new Carrier(), capacity,
            new CarrierLocation(Carrier.Unknown, 0), new Placeholder()));
        AssertUnresolved(FiniteReadSurface.Publish(new Carrier(), Capacity(Carrier.Unknown, -7, 7),
            new CarrierLocation(Carrier.Unknown, 0), new Placeholder()));
    }

    [Fact]
    public void MalformedPublicationDoesNotInventABasisOrThrowOnANumericRead()
    {
        var axis = new Carrier();
        var malformed = QualifiedReadSurface.Create(new Carrier(), new Placeholder(), Relation.Segment(axis, 0, 1),
            default(CarrierLocation), Relation.Segment(axis, 0, 0), new Placeholder(), Capacity(axis, -7, 7),
            new Placeholder(), new Placeholder());
        Assert.False(QualifiedReadSurface.TryReadPublishedValue(malformed, out _));
        Assert.False(FiniteReadSurface.TryReadValue(malformed, out _));
        Assert.False(FiniteReadSurface.TryReadSupport(malformed, out _, out _));
        Assert.False(FiniteReadSurface.TryGreaterThan(malformed, 0, out _));
    }

    [Fact]
    public void PublicationAndDerivedResultsAreHistoricalAndDoNotSettleTheSource()
    {
        var axis = new Carrier();
        axis.Set(14);
        var input = axis.ValueSlot;
        var capacity = Capacity(axis, 0, 15);
        var first = FiniteReadSurface.Publish(new Carrier(), capacity, input, input);
        var one = Publish(capacity, axis, 1);
        var firstSum = FiniteReadSurface.Add(new Carrier(), capacity, first, one);

        AssertRead(first, axis, 14, 14, 14);
        AssertRead(firstSum, axis, 15, 15, 15);
        Assert.Equal(14UL, axis.Value);
        Assert.Equal(0, axis.Tension);
        axis.Set(3);
        var next = FiniteReadSurface.Publish(new Carrier(), capacity, input, input);
        var nextSum = FiniteReadSurface.Add(new Carrier(), capacity, next, one);

        AssertRead(first, axis, 14, 14, 14);
        AssertRead(firstSum, axis, 15, 15, 15);
        AssertRead(next, axis, 3, 3, 3);
        AssertRead(nextSum, axis, 4, 4, 4);
        Assert.Equal(3UL, axis.Value);
        Assert.Equal(0, axis.Tension);
        Assert.True(FiniteReadSurface.TryReadValue(first, out var historical));
        Assert.False(historical.SameTickRefAs(input));
    }

    private static Relation Capacity(Carrier axis, long minimum, long maximum, long quantum = 1) =>
        FiniteReadSurface.Capacity(new Carrier(), axis, minimum, maximum, quantum);

    private static Relation Publish(Relation capacity, Carrier axis, long input, IElement? source = null) =>
        FiniteReadSurface.Publish(new Carrier(), capacity, new CarrierLocation(axis, input), source ?? new Placeholder());

    private static void AssertRead(Relation read, Carrier axis, long nominal, long? lower, long? upper)
    {
        Assert.True(ReadWitness.TryRead(read, out _, out _, out _, out _, out _));
        Assert.True(FiniteReadSurface.TryReadValue(read, out var value));
        Assert.True(value.Carrier.SameIdentityAs(axis));
        Assert.Equal(nominal, value.Ticks);
        Assert.True(FiniteReadSurface.TryReadSupport(read, out var low, out var high));
        Assert.Equal(lower, low?.Ticks);
        Assert.Equal(upper, high?.Ticks);
        if (low.HasValue) Assert.True(low.Value.Carrier.SameIdentityAs(axis));
        if (high.HasValue) Assert.True(high.Value.Carrier.SameIdentityAs(axis));
    }

    private static void AssertContains(Relation read, long actual)
    {
        Assert.True(FiniteReadSurface.TryReadSupport(read, out var low, out var high));
        Assert.True(!low.HasValue || actual >= low.Value.Ticks);
        Assert.True(!high.HasValue || actual <= high.Value.Ticks);
    }

    private static void AssertUnresolved(Relation read)
    {
        Assert.True(ReadWitness.TryRead(read, out _, out _, out var result, out _, out _));
        Assert.IsType<Placeholder>(result);
        Assert.False(FiniteReadSurface.TryReadValue(read, out _));
        Assert.False(FiniteReadSurface.TryReadSupport(read, out _, out _));
        Assert.False(FiniteReadSurface.TryGreaterThan(read, 0, out _));
    }
}
