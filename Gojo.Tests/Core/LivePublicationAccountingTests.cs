using Gojo.Core;
using Gojo.Structures;

namespace Gojo.Tests.Core;

public sealed class LivePublicationAccountingTests
{
    [Fact]
    public void NegativeLiveLevelRetainsOneRequestAcrossRepeatedSettlement()
    {
        var source = new Carrier();
        var target = new Carrier();
        source.AttachLiveOutput(source.ValueSlot,
            new Relation(Relation.Segment(source, 0, 1), Relation.Segment(target, 0, -1)));

        source.Set(4);
        Assert.Equal(0UL, target.Value);
        Assert.Equal(-4, target.Tension);

        Assert.True(SettlementObservation.ObservedQuiet(target.Settle()));
        source.Set(4);
        Assert.Equal(0UL, target.Value);
        Assert.Equal(-4, target.Tension);
    }

    [Fact]
    public void BlockedLiveLevelDoesNotRepeatedlyAppendTheSamePressure()
    {
        var (source, target) = CappedPublication(3);

        source.Set(8);
        Assert.Equal(3UL, target.Value);
        Assert.Equal(5, target.Tension);

        Assert.True(SettlementObservation.ObservedQuiet(source.Settle()));
        target.Add(0);
        source.Set(8);
        Assert.Equal(3UL, target.Value);
        Assert.Equal(5, target.Tension);
    }

    [Fact]
    public void LoweredLiveLevelCancelsOutstandingPressureBeforeLoweringVisibleValue()
    {
        var (source, target) = CappedPublication(3);
        source.Set(8);

        source.Set(6);
        Assert.Equal(3UL, target.Value);
        Assert.Equal(3, target.Tension);

        source.Set(2);
        Assert.Equal(2UL, target.Value);
        Assert.Equal(0, target.Tension);
        Assert.True(SettlementObservation.ObservedQuiet(source.Settle()));
    }

    [Fact]
    public void RaisedLiveLevelAddsOnlyTheNewOutstandingParticipation()
    {
        var (source, target) = CappedPublication(3);
        source.Set(8);

        source.Set(11);
        Assert.Equal(3UL, target.Value);
        Assert.Equal(8, target.Tension);
        Assert.True(SettlementObservation.ObservedQuiet(source.Settle()));
    }

    [Fact]
    public void DownstreamConsumptionCanKeepALivePublicationActive()
    {
        var source = new Carrier();
        var publication = new Carrier();
        var sink = new Carrier();
        source.AttachLiveOutput(source.ValueSlot,
            new Relation(Relation.Segment(source, 0, 1), Relation.Segment(publication, 0, 1)));
        publication.Attach(new Relation(Relation.Point(publication, 1), Relation.Point(sink, 0)));

        // One arrival changes the source level; every settlement pass can then
        // replenish the consumed publication. The source itself is not spent.
        var observation = Carrier.ReceiveTogether(
            Relation.OrderedGroup(new Carrier(), new CarrierLocation(source, 8)), maxPasses: 3);

        Assert.Equal(8UL, source.Value);
        Assert.Equal(24UL, sink.Value);
        Assert.False(SettlementObservation.ObservedQuiet(observation));
        Assert.True(SettlementObservation.TryRead(observation, out _, out var used, out _, out _));
        Assert.Equal(3, used.Ticks);

        var continued = source.Settle(maxPasses: 3);
        Assert.Equal(48UL, sink.Value);
        Assert.False(SettlementObservation.ObservedQuiet(continued));
    }

    [Fact]
    public void EqualFiniteArrivalsRemainDistinctAndDoNotRepeatWhenRead()
    {
        var known = new Carrier();
        var first = new Carrier();
        var second = new Carrier();
        first.Attach(new Relation(Relation.Point(first, 4), Relation.Point(known, 0)));
        second.Attach(new Relation(Relation.Point(second, 4), Relation.Point(known, 0)));

        var firstEvent = Relation.OrderedGroup(new Carrier(), new CarrierLocation(first, 11));
        var secondEvent = Relation.OrderedGroup(new Carrier(), new CarrierLocation(second, 11));
        var firstObservation = Carrier.ReceiveTogether(firstEvent);
        var secondObservation = Carrier.ReceiveTogether(secondEvent);

        Assert.NotSame(firstEvent, secondEvent);
        Assert.True(SettlementObservation.ObservedQuiet(firstObservation));
        Assert.True(SettlementObservation.ObservedQuiet(secondObservation));
        Assert.Equal(4UL, known.Value);
        Assert.Equal(3UL, first.Value);
        Assert.Equal(3UL, second.Value);
        Assert.Equal(0, first.Tension);
        Assert.Equal(0, second.Tension);

        Assert.True(SettlementObservation.ObservedQuiet(known.Settle()));
        Assert.Equal(4UL, known.Value);
        Assert.Equal(3UL, first.Value);
        Assert.Equal(3UL, second.Value);
    }

    [Fact]
    public void ZeroFiniteArrivalRetainsEventAndSourceIdentity()
    {
        var packet = new Carrier();
        var known = new Carrier();
        packet.Attach(new Relation(Relation.Point(packet, 4), Relation.Point(known, 0)));
        var arrival = Relation.OrderedGroup(new Carrier(), new CarrierLocation(packet, 0));

        var observation = Carrier.ReceiveTogether(arrival);

        Assert.True(SettlementObservation.ObservedQuiet(observation));
        Assert.True(ReadWitness.TryRead(observation, out var selected, out _, out _, out _, out var admitted));
        Assert.Same(arrival, selected);
        var frozen = Assert.IsType<Relation>(admitted);
        Assert.True(frozen.TryReadOrderedElementsOn(Assert.IsType<Carrier>(frozen.Right), out var samples));
        var sample = Assert.IsType<CarrierLocation>(Assert.Single(samples));
        Assert.Same(packet, sample.Carrier);
        Assert.Equal(0, sample.Ticks);
        Assert.Equal(0UL, packet.Value);
        Assert.Equal(0UL, known.Value);
    }

    [Fact]
    public void LaterRouteTransfersFiniteRemainderOnlyOnce()
    {
        var packet = new Carrier();
        var known = new Carrier();
        var remainderReceiver = new Carrier();
        packet.Attach(new Relation(Relation.Point(packet, 4), Relation.Point(known, 0)));
        packet.Add(11);
        Assert.Equal(2UL, known.Value);
        Assert.Equal(3UL, packet.Value);

        packet.Attach(new Relation(Relation.Point(packet, 1), Relation.Point(remainderReceiver, 0)));

        Assert.Equal(0UL, packet.Value);
        Assert.Equal(0, packet.Tension);
        Assert.Equal(2UL, known.Value);
        Assert.Equal(3UL, remainderReceiver.Value);
        Assert.True(SettlementObservation.ObservedQuiet(packet.Settle()));
        Assert.Equal(2UL, known.Value);
        Assert.Equal(3UL, remainderReceiver.Value);
    }

    private static (Carrier Source, Carrier Target) CappedPublication(long capacity)
    {
        var source = new Carrier();
        var target = new Carrier();
        target.Attach(new Relation(Relation.Point(target, capacity), Relation.Point(target, capacity)));
        source.AttachLiveOutput(source.ValueSlot,
            new Relation(Relation.Segment(source, 0, 1), Relation.Segment(target, 0, 1)));
        return (source, target);
    }
}
