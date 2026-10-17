using Gojo.Core;
using Gojo.Equations;
using Gojo.Structures;

namespace Gojo.Tests.Core;

public sealed class OperationClaimIntervalResponseTests
{
    [Fact]
    public void SameNativeContributionHelpsOneConsumerAndHurtsAnother()
    {
        var input = new Carrier();
        var producer = new Carrier(input.UnitShape);
        var equation = Equation.Sum(input, Equation.UnitPort(input), Equation.UnitPort(producer));
        var before = new CarrierLocation(input, 3);
        var contribution = new CarrierLocation(producer, 4);
        Assert.True(Equation.TryPredict(equation, new Relation(before, contribution), input, out var applied));
        Assert.True(Equation.TryReadPrediction(applied, out var after, out _));
        var transition = Relation.OrderedGroup(new Carrier(), equation, contribution, applied);

        var first = OperationClaim.IntervalResponse(new Carrier(), Relation.Segment(input, 6, 8), before, after,
            new Carrier(input.UnitShape), transition);
        var second = OperationClaim.IntervalResponse(new Carrier(), Relation.Segment(input, 2, 4), before, after,
            new Carrier(input.UnitShape), transition);

        AssertResponse(first, 3, 0, 3);
        AssertResponse(second, 0, 3, -3);
        Assert.True(ReadWitness.TryRead(first, out _, out _, out _, out _, out var provenance));
        Assert.True(Assert.IsType<Relation>(provenance).TryReadOrderedGroup(out var evidence));
        Assert.Same(transition, evidence[0]);
        Assert.Equal(0UL, input.Value);
        Assert.Equal(0L, input.Tension);
    }

    [Fact]
    public void RetargetingReusableBoundsAndSamplesChangesNewReadButNotEarlierSample()
    {
        var basis = new Carrier();
        var lower = new TickRef(5);
        var upper = new TickRef(8);
        var previous = new TickRef(2);
        var current = new TickRef(6);
        var interval = new Relation(new Relation(new CarrierLocation(basis, lower), basis),
            new Relation(new CarrierLocation(basis, upper), basis));
        Relation Observe() => OperationClaim.IntervalResponse(new Carrier(), interval,
            new CarrierLocation(basis, previous), new CarrierLocation(basis, current), new Carrier(basis.UnitShape));

        var earlier = Observe();
        AssertResponse(earlier, 3, 0, 3);
        lower.Set(0); upper.Set(3); previous.Set(1); current.Set(5);
        var later = Observe();
        AssertResponse(later, 0, 2, -2);
        AssertResponse(earlier, 3, 0, 3);
        Assert.True(OperationClaim.TryReadIntervalResponse(earlier, out var captured, out _, out _, out _, out _, out _));
        Assert.True(TimeStepSequence.TryReadSpan(captured, out var capturedLower, out var capturedUpper));
        Assert.Equal(5, capturedLower.Ticks);
        Assert.Equal(8, capturedUpper.Ticks);
        Assert.NotSame(lower, capturedLower.Tick);
    }

    [Fact]
    public void ZeroDemandIsDifferentFromUnchangedUnmetDemand()
    {
        var basis = new Carrier();
        var interval = Relation.Segment(basis, 4, 8);
        var quiet = OperationClaim.IntervalResponse(new Carrier(), interval, new CarrierLocation(basis, 5),
            new CarrierLocation(basis, 6), new Carrier(basis.UnitShape));
        var stalled = OperationClaim.IntervalResponse(new Carrier(), interval, new CarrierLocation(basis, 1),
            new CarrierLocation(basis, 1), new Carrier(basis.UnitShape));
        AssertResponse(quiet, 0, 0, 0);
        AssertResponse(stalled, 3, 3, 0);
    }

    [Fact]
    public void OpenInputRemainsUnresolvedInsteadOfZeroUtility()
    {
        var basis = new Carrier();
        var open = new Placeholder();
        var witness = OperationClaim.IntervalResponse(new Carrier(), Relation.Segment(basis, 1, 3),
            open, new CarrierLocation(basis, 2), new Carrier(basis.UnitShape));
        AssertUnresolved(witness);
        Assert.True(ReadWitness.TryRead(witness, out var selected, out _, out _, out _, out _));
        Assert.True(Assert.IsType<Relation>(selected).TryReadOrderedGroup(out var samples));
        Assert.Same(open, samples[0]);
    }

    [Fact]
    public void UnalignedInputAndOpposedFaceCannotSupplyUtility()
    {
        var basis = new Carrier();
        foreach (var other in new[] { new Carrier(basis.UnitShape), basis.Inverse(), Carrier.Unknown })
        {
            var witness = OperationClaim.IntervalResponse(new Carrier(), Relation.Segment(basis, 1, 3),
                new CarrierLocation(other, 0), new CarrierLocation(basis, 2), new Carrier(basis.UnitShape));
            AssertUnresolved(witness);
        }
    }

    [Fact]
    public void ReversedAndOpenBoundsAreRetainedWithoutSortingOrScalarVerdict()
    {
        var basis = new Carrier();
        var reversed = OperationClaim.IntervalResponse(new Carrier(), Relation.Segment(basis, 8, 2),
            new CarrierLocation(basis, 1), new CarrierLocation(basis, 5), new Carrier(basis.UnitShape));
        AssertUnresolved(reversed);
        Assert.True(ReadWitness.TryRead(reversed, out _, out var interval, out _, out _, out _));
        Assert.True(TimeStepSequence.TryReadSpan(Assert.IsType<Relation>(interval), out var start, out var end));
        Assert.Equal(8, start.Ticks); Assert.Equal(2, end.Ticks);

        var open = OperationClaim.IntervalResponse(new Carrier(), new Relation(Relation.Point(basis, 1), new Placeholder()),
            new CarrierLocation(basis, 1), new CarrierLocation(basis, 5), new Carrier(basis.UnitShape));
        AssertUnresolved(open);
    }

    [Fact]
    public void QualifiedBetweenReaderRejectsQuietButUnalignedAndReversedClaims()
    {
        var basis = new Carrier();
        var residual = new Carrier(basis.UnitShape);
        var resolved = OperationClaim.Between(new Carrier(), new CarrierLocation(basis, 0),
            new CarrierLocation(basis, 2), new CarrierLocation(basis, 5), residual);
        Assert.True(OperationClaim.TryReadResolvedBetweenViolation(resolved, out var demand));
        Assert.Equal(2, demand.Ticks);
        var unaligned = OperationClaim.Between(new Carrier(), new CarrierLocation(new Carrier(basis.UnitShape), 3),
            new CarrierLocation(basis, 2), new CarrierLocation(basis, 5), residual);
        Assert.False(OperationClaim.TryReadResolvedBetweenViolation(unaligned, out _));
        var reversed = OperationClaim.Between(new Carrier(), new CarrierLocation(basis, 3),
            new CarrierLocation(basis, 5), new CarrierLocation(basis, 2), residual);
        Assert.False(OperationClaim.TryReadResolvedBetweenViolation(reversed, out _));
    }

    [Fact]
    public void QualifiedBetweenReaderRejectsCachedViolationAfterLiveValueOrBoundsMove()
    {
        var basis = new Carrier();
        var value = new TickRef(2);
        var lower = new TickRef(5);
        var residual = new Carrier(basis.UnitShape);
        Relation Read() => OperationClaim.Between(new Carrier(), new CarrierLocation(basis, value),
            new CarrierLocation(basis, lower), new CarrierLocation(basis, 8), residual);
        var initial = Read();
        Assert.True(OperationClaim.TryReadResolvedBetweenViolation(initial, out var initialDemand));
        Assert.Equal(3, initialDemand.Ticks);

        value.Set(6);
        Assert.False(OperationClaim.TryReadResolvedBetweenViolation(initial, out _));
        var inside = Read();
        Assert.True(OperationClaim.TryReadResolvedBetweenViolation(inside, out var insideDemand));
        Assert.Equal(0, insideDemand.Ticks);

        // Remaining inside still changes the cached signed residual: require a
        // new observation rather than accepting a coincidentally unchanged zero.
        lower.Set(4);
        Assert.False(OperationClaim.TryReadResolvedBetweenViolation(inside, out _));
        Assert.True(OperationClaim.TryReadResolvedBetweenViolation(Read(), out var refreshed));
        Assert.Equal(0, refreshed.Ticks);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void IntervalResponseDoesNotIgnoreOpenOrNonzeroResidue(bool alterPrediction, bool open)
    {
        var basis = new Carrier();
        var response = OperationClaim.IntervalResponse(new Carrier(), Relation.Segment(basis, 5, 8),
            new CarrierLocation(basis, 2), new CarrierLocation(basis, 6), new Carrier(basis.UnitShape));
        AssertResponse(response, 3, 0, 3);
        Assert.True(ReadWitness.TryRead(response, out var selected, out var surface, out var result,
            out var residue, out var provenance));
        Assert.True(Assert.IsType<Relation>(result).TryReadOrderedGroup(out var fields));
        Assert.True(Equation.TryReadPrediction(Assert.IsType<Relation>(fields[3]), out var reduction, out var remainder));
        var pair = Assert.IsType<Relation>(remainder);
        var originalLeft = Assert.IsType<CarrierLocation>(pair.Left);
        IElement altered = new Relation(open ? new Placeholder() : new CarrierLocation(originalLeft.Carrier, 1), pair.Right);
        if (alterPrediction)
        {
            fields[3] = new Relation(reduction, altered);
            result = Relation.OrderedGroup(new Carrier(), fields);
        }
        else residue = altered;
        var contaminated = ReadWitness.Create(new Carrier(), selected, surface, result, residue, provenance);
        Assert.False(OperationClaim.TryReadIntervalResponse(contaminated, out _, out _, out _, out _, out _, out _));
    }

    private static void AssertResponse(Relation witness, long before, long after, long reduction)
    {
        Assert.True(OperationClaim.TryReadIntervalResponse(witness, out _, out _, out _,
            out var beforeDemand, out var afterDemand, out var actual));
        Assert.Equal(before, beforeDemand.Ticks);
        Assert.Equal(after, afterDemand.Ticks);
        Assert.Equal(reduction, actual.Ticks);
    }

    private static void AssertUnresolved(Relation witness)
    {
        Assert.False(OperationClaim.TryReadIntervalResponse(witness, out _, out _, out _, out _, out _, out _));
        Assert.True(ReadWitness.TryRead(witness, out _, out _, out var result, out var residue, out _));
        Assert.IsType<Relation>(residue);
        Assert.True(Assert.IsType<Relation>(result).TryReadOrderedGroup(out var fields));
        Assert.False(Equation.TryReadPrediction(Assert.IsType<Relation>(fields[3]), out _, out _));
    }
}
