using Gojo.Core;
using Gojo.Equations;
using Gojo.Structures;

namespace Gojo.Tests.Core;

public sealed class CoherentArrivalTests
{
    [Fact]
    public void TwoSensorArrivalsEnterBeforeAnIntermediateDifferenceCanFlow()
    {
        var x = new Carrier();
        var y = new Carrier();
        var difference = new Carrier();
        var receiver = new Carrier();
        var subtract = Equation.Sum(difference, Equation.UnitPort(x), Equation.WeightedPort(y, 0, 1, 0, -1));
        x.AttachLiveOutput(new Relation(x.ValueSlot, y.ValueSlot), subtract);
        difference.Attach(new Relation(Relation.Point(difference, 1), Relation.Point(receiver, 0)));

        var observed = Carrier.ReceiveTogether(Frame(new CarrierLocation(x, 3), new CarrierLocation(y, 3)));

        Assert.Equal(3UL, x.Value);
        Assert.Equal(3UL, y.Value);
        Assert.Equal(0UL, difference.Value);
        Assert.Equal(0UL, receiver.Value);
        Assert.True(SettlementObservation.ObservedQuiet(observed));
    }

    [Fact]
    public void SharedLiveSlotsAreFrozenBeforeAnyArrivalChangesThem()
    {
        var source = new Carrier();
        source.Set(2);
        var frame = Frame(source.ValueSlot, source.ValueSlot);

        var observed = Carrier.ReceiveTogether(frame);

        Assert.Equal(6UL, source.Value);
        Assert.True(ReadWitness.TryRead(observed, out var selected, out _, out _, out _, out var admitted));
        Assert.Same(frame, selected);
        var frozen = Assert.IsType<Relation>(admitted);
        Assert.True(frozen.TryReadOrderedElementsOn(Assert.IsType<Carrier>(frozen.Right), out var samples));
        source.Set(9);
        Assert.All(samples, sample => Assert.Equal(2, Assert.IsType<CarrierLocation>(sample).Ticks));
    }

    [Fact]
    public void AllAffectedWorldsSettleWithoutReplayingSharedWorldInput()
    {
        var a = new Carrier();
        var b = new Carrier();
        var qa = new Carrier();
        var qb = new Carrier();
        a.Attach(new Relation(Relation.Point(a, 4), Relation.Point(qa, 0)));
        b.Attach(new Relation(Relation.Point(b, 3), Relation.Point(qb, 0)));

        var observed = Carrier.ReceiveTogether(Frame(new CarrierLocation(a, 11), new CarrierLocation(b, 22)));

        Assert.Equal(2UL, qa.Value);
        Assert.Equal(3UL, a.Value);
        Assert.Equal(7UL, qb.Value);
        Assert.Equal(1UL, b.Value);
        Assert.True(SettlementObservation.ObservedQuiet(observed));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void UnboundArrivalPreventsPartialAdmission(bool placeholder)
    {
        var source = new Carrier();
        IElement invalid = placeholder ? new Placeholder() : new CarrierLocation(Carrier.Unknown, 4);

        var observed = Carrier.ReceiveTogether(Frame(new CarrierLocation(source, 3), invalid));

        Assert.Equal(0UL, source.Value);
        Assert.False(SettlementObservation.ObservedQuiet(observed));
        Assert.True(SettlementObservation.TryRead(observed, out _, out var used, out _, out _));
        Assert.Equal(0, used.Ticks);
    }

    [Fact]
    public void UnrepresentableLaterArrivalDoesNotPartiallyAdmitEarlierInput()
    {
        var a = new Carrier();
        var b = new Carrier();
        b.Set(long.MaxValue);

        var observed = Carrier.ReceiveTogether(Frame(new CarrierLocation(a, 3), new CarrierLocation(b, 1)));

        Assert.Equal(0UL, a.Value);
        Assert.Equal((ulong)long.MaxValue, b.Value);
        Assert.False(SettlementObservation.ObservedQuiet(observed));
        Assert.True(ReadWitness.TryRead(observed, out _, out _, out _, out _, out var admitted));
        Assert.IsType<Placeholder>(admitted);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SimultaneousOpposedDeltasDoNotDependOnIntermediateHostOverflow(bool reversed)
    {
        var source = new Carrier();
        IElement[] deltas = [new CarrierLocation(source, long.MaxValue),
            new CarrierLocation(source, 1), new CarrierLocation(source, -long.MaxValue)];
        if (reversed) Array.Reverse(deltas);

        var observed = Carrier.ReceiveTogether(Frame(deltas));

        Assert.Equal(1UL, source.Value);
        Assert.True(SettlementObservation.ObservedQuiet(observed));
    }

    [Fact]
    public void PositiveCapacityRouteCannotDiscardUnrepresentedNegativePressure()
    {
        var source = new Carrier();
        var receiver = new Carrier();
        receiver.Attach(new Relation(Relation.Point(receiver, 9), Relation.Point(receiver, 9)));
        source.Attach(new Relation(Relation.Point(source, 1), Relation.Point(receiver, 0)));

        var observed = Carrier.ReceiveTogether(Frame(new CarrierLocation(source, -3)));

        Assert.Equal(-3, source.Tension);
        Assert.Equal(0UL, receiver.Value);
        Assert.True(SettlementObservation.ObservedQuiet(observed));
    }

    [Fact]
    public void RoutedArrivalCanPartlyCancelPressureWithoutAnOldRemainderOverwritingIt()
    {
        var incoming = new Carrier();
        var middle = new Carrier();
        var last = new Carrier();
        middle.Attach(new Relation(Relation.Point(middle, 9), Relation.Point(middle, 9)));
        last.Attach(new Relation(Relation.Point(last, 9), Relation.Point(last, 9)));
        incoming.Attach(new Relation(Relation.Point(incoming, 1), Relation.Point(middle, 0)));
        middle.Attach(new Relation(Relation.Point(middle, 1), Relation.Point(last, 0)));

        var observed = Carrier.ReceiveTogether(Frame(new CarrierLocation(incoming, 2), new CarrierLocation(middle, -3)));

        Assert.Equal(-1, middle.Tension);
        Assert.Equal(0UL, incoming.Value);
        Assert.Equal(0UL, middle.Value);
        Assert.Equal(0UL, last.Value);
        Assert.True(SettlementObservation.ObservedQuiet(observed));
    }

    [Fact]
    public void DifferentlyCalibratedGroupsCannotSpendOnePoolTwice()
    {
        var source = new Carrier();
        var ones = new Carrier();
        var twos = new Carrier();
        ones.Attach(new Relation(Relation.Point(ones, 20), Relation.Point(ones, 20)));
        twos.Attach(new Relation(Relation.Point(twos, 20), Relation.Point(twos, 20)));
        source.Attach(new Relation(Relation.Point(source, 1), Relation.Point(ones, 0)));
        source.Attach(new Relation(Relation.Point(source, 2), Relation.Point(twos, 0)));

        var observed = Carrier.ReceiveTogether(Frame(new CarrierLocation(source, 4)));

        // No preference between these competing calibrations has been supplied.
        // Both proposed full withdrawals stay unresolved at the source.
        Assert.Equal(4, source.Tension);
        Assert.Equal(0UL, ones.Value);
        Assert.Equal(0UL, twos.Value);
        Assert.True(SettlementObservation.ObservedQuiet(observed));
    }

    [Fact]
    public void WildcardWorldDoesNotProduceAnObservedQuietAnswer() =>
        Assert.False(SettlementObservation.ObservedQuiet(Carrier.Unknown.Settle()));

    [Fact]
    public void ZeroBudgetAdmitsTheFrameButDoesNotClaimToHaveObservedSettlement()
    {
        var source = new Carrier();
        var target = new Carrier();
        source.AttachLiveOutput(source.ValueSlot, Equation.Affine(source, 0, 1, target, 0, 1));

        var observed = Carrier.ReceiveTogether(Frame(new CarrierLocation(source, 5)), maxPasses: 0);

        Assert.Equal(5UL, source.Value);
        Assert.Equal(0UL, target.Value);
        Assert.False(SettlementObservation.ObservedQuiet(observed));
        Assert.True(SettlementObservation.TryRead(observed, out _, out var used, out _, out _));
        Assert.Equal(0, used.Ticks);
        Assert.False(SettlementObservation.ObservedQuiet(source.Settle(maxPasses: 1)));
        Assert.Equal(5UL, target.Value);
        Assert.True(SettlementObservation.ObservedQuiet(source.Settle(maxPasses: 1)));
    }

    [Fact]
    public void NegativeBudgetPreservesUnreceivedInput()
    {
        var source = new Carrier();
        var frame = Frame(new CarrierLocation(source, 5));

        var observed = Carrier.ReceiveTogether(frame, maxPasses: -1);

        Assert.Equal(0UL, source.Value);
        Assert.False(SettlementObservation.ObservedQuiet(observed));
        Assert.True(ReadWitness.TryRead(observed, out var selected, out _, out _, out _, out _));
        Assert.Same(frame, selected);
    }

    [Fact]
    public void NegativeMovementAndPositiveParticipationOnInverseFaceStayDistinct()
    {
        var forward = new Carrier();
        var inverse = forward.Inverse();

        var observed = Carrier.ReceiveTogether(Frame(new CarrierLocation(forward, -3), new CarrierLocation(inverse, 3)));

        Assert.Equal(0UL, forward.Value);
        Assert.Equal(-3, forward.Tension);
        Assert.Equal(3UL, inverse.Value);
        Assert.Equal(0, inverse.Tension);
        Assert.True(SettlementObservation.ObservedQuiet(observed));
    }

    [Fact]
    public void QuietCanRetainPressureWithoutCallingItResolved()
    {
        var source = new Carrier();
        source.Attach(new Relation(Relation.Point(source, 3), Relation.Point(source, 3)));
        source.Add(8);

        var observed = source.Settle();

        Assert.True(SettlementObservation.ObservedQuiet(observed));
        Assert.Equal(5, source.Tension);
        Assert.True(ReadWitness.TryRead(observed, out _, out _, out _, out _, out var admitted));
        var noArrival = Assert.IsType<Relation>(admitted);
        Assert.True(noArrival.TryReadOrderedElementsOn(Assert.IsType<Carrier>(noArrival.Right), out var samples));
        Assert.Empty(samples);
    }

    [Fact]
    public void EqualPassEndpointsDoNotHideCompetingLivePublications()
    {
        var source = new Carrier();
        var target = new Carrier();
        source.Set(1);
        var up = source.AttachLiveOutput(source.ValueSlot, Equation.Affine(source, 0, 1, target, 0, 1));
        var zero = new Carrier();
        var down = zero.AttachLiveOutput(zero.ValueSlot, Equation.Affine(zero, 0, 1, target, 0, 1));
        Assert.Equal(0UL, target.Value);

        var observed = source.Settle(maxPasses: 3);

        Assert.Equal(0UL, target.Value);
        Assert.False(SettlementObservation.ObservedQuiet(observed));
        Assert.True(SettlementObservation.TryRead(observed, out _, out var used, out var active, out var continuation));
        Assert.Equal(3, used.Ticks);
        Assert.Same(active, continuation);
        var group = Assert.IsType<Relation>(active);
        Assert.True(group.TryReadOrderedElementsOn(Assert.IsType<Carrier>(group.Right), out var relations));
        Assert.Contains(up, relations);
        Assert.Contains(down, relations);
    }

    private static Relation Frame(params IElement[] arrivals) => Relation.OrderedGroup(new Carrier(), arrivals);
}
