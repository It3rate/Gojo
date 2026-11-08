using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;
using Gojo.Experiments.QualitativeFrieze.Composition;

namespace QualitativeExecution.Tests;

public sealed class AcknowledgedComparisonChannelTests
{
    [Fact]
    public void One_slot_holds_its_actual_request_and_does_not_resample_until_acknowledged()
    {
        var (goal, receiver, link, channel) = Fixture();
        var request = channel.Request(link);
        Assert.True(request.Accepted); Assert.Equal(4, request.Observation!.Mask);
        goal.Remove(5);
        var blocked = channel.Request(link);
        Assert.False(blocked.Accepted); Assert.Null(blocked.Observation);
        Assert.Same(request.Observation.Packet, channel.Snapshot().Packet);
        Assert.Equal(1, channel.Snapshot().Occupancy);
        var dispatched = channel.Dispatch();
        Assert.True(dispatched.Accepted); Assert.Same(request.Observation.Packet, dispatched.Packet);
        Assert.False(channel.Dispatch().Accepted);
        Assert.True(channel.Snapshot().Waiting);
        var response = receiver.Step(dispatched.Packet!);
        var receipt = channel.Receipt(receiver, response);
        Assert.True(receipt.Accepted); Assert.True(channel.Snapshot().Waiting);
        Assert.True(channel.Acknowledge(receipt.Receipt!).Accepted);
        Assert.False(channel.Snapshot().Waiting); Assert.Equal(0, channel.Snapshot().Occupancy);
        Assert.Equal(1, channel.Request(link).Observation!.Mask);
    }

    [Fact]
    public void Foreign_contract_and_blocked_offer_never_replace_the_reserved_packet()
    {
        var (_, _, link, channel) = Fixture();
        var offered = channel.Request(link).Observation!.Packet;
        var other = CoupledExtentMachine.Create();
        Assert.False(channel.Offer(other.Publication(4)).Accepted);
        Assert.Same(offered, channel.Snapshot().Packet);
        var (_, _, _, empty) = Fixture();
        Assert.False(empty.Offer(offered).Accepted);
        Assert.False(empty.Snapshot().Waiting);
    }

    [Fact]
    public void Receipt_acknowledges_processing_even_when_capacity_keeps_tension()
    {
        var (_, receiver, link, channel) = Fixture(initial: 2, capacity: 2);
        channel.Request(link); var packet = channel.Dispatch().Packet!;
        var response = receiver.Step(packet);
        Assert.True(response.ConsumedEvent); Assert.Equal(0, response.AcceptedDelta);
        Assert.Equal(1, response.After.Tension);
        var receipt = channel.Receipt(receiver, response);
        Assert.True(receipt.Accepted); Assert.True(channel.Acknowledge(receipt.Receipt!).Accepted);
        Assert.Equal(1, receiver.Snapshot().Tension);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, true)]
    public void Missing_or_ambiguous_processing_can_be_acknowledged_without_asserting_settlement(bool available, bool ambiguous)
    {
        var (_, receiver, link, channel) = Fixture();
        channel.Request(link, available, ambiguous);
        var response = receiver.Step(channel.Dispatch().Packet!);
        Assert.True(response.ConsumedEvent); Assert.False(response.Resolved);
        var receipt = channel.Receipt(receiver, response);
        Assert.True(receipt.Accepted); Assert.True(channel.Acknowledge(receipt.Receipt!).Accepted);
        Assert.Equal(1, receiver.Snapshot().Value);
    }

    [Fact]
    public void Paused_replayed_foreign_and_wrong_event_responses_do_not_release_the_request()
    {
        var (_, receiver, link, channel) = Fixture();
        channel.Request(link); var packet = channel.Dispatch().Packet!;
        Assert.False(channel.Receipt(receiver, receiver.Step(packet, pulse: false)).Accepted);
        Assert.False(channel.Receipt(receiver, receiver.Step(receiver.Publication(4))).Accepted);
        var foreign = CoupledExtentMachine.Create();
        Assert.False(channel.Receipt(foreign, foreign.Step(foreign.Publication(4))).Accepted);
        var actual = receiver.Step(packet);
        Assert.False(channel.Receipt(receiver, receiver.Step(packet)).Accepted);
        Assert.True(channel.Snapshot().Waiting);
        Assert.True(channel.Receipt(receiver, actual).Accepted);
    }

    [Fact]
    public void Gate_may_change_support_while_preserving_the_consumed_occurrence_and_contract()
    {
        var (_, receiver, link, channel) = Fixture();
        channel.Request(link); var packet = channel.Dispatch().Packet!;
        Assert.True(QualitativePublication.TryRead(packet, out _, out _, out var occurrence, out _, out _, out _, out _));
        var gated = receiver.Publication(2, occurrence, new Placeholder());
        var response = receiver.Step(gated);
        Assert.Equal(0, response.AcceptedDelta);
        var receipt = channel.Receipt(receiver, response);
        Assert.True(receipt.Accepted);
        Assert.True(channel.Acknowledge(receipt.Receipt!).Accepted);
    }

    [Fact]
    public void Receipt_is_opaque_and_wrong_copied_or_old_receipts_cannot_release_a_new_request()
    {
        var (_, receiver, link, channel) = Fixture();
        channel.Request(link);
        var response = receiver.Step(channel.Dispatch().Packet!);
        var receipt = channel.Receipt(receiver, response).Receipt!;
        var reachable = Reachable(receipt).ToArray();
        Assert.DoesNotContain(reachable, item => ReferenceEquals(item, receiver.Surface));
        Assert.DoesNotContain(reachable, item => ReferenceEquals(item, response.Surface));
        Assert.DoesNotContain(reachable, item => ReferenceEquals(item, receiver.ExtentPort));
        Assert.False(channel.Acknowledge(new Relation(receipt.Left, receipt.Right)).Accepted);
        Assert.True(channel.Acknowledge(receipt).Accepted);
        Assert.False(channel.Acknowledge(receipt).Accepted);
        channel.Request(link);
        Assert.False(channel.Acknowledge(receipt).Accepted);
        Assert.True(channel.Snapshot().Waiting);
    }

    [Fact]
    public void Reoffering_a_consumed_occurrence_does_not_create_a_new_observation()
    {
        var (_, receiver, link, channel) = Fixture();
        channel.Request(link); var packet = channel.Dispatch().Packet!;
        var receipt = channel.Receipt(receiver, receiver.Step(packet)).Receipt!;
        channel.Acknowledge(receipt);
        Assert.False(channel.Offer(packet).Accepted);
        Assert.False(channel.Snapshot().Waiting);
    }

    [Fact]
    public void Archive_restores_an_outstanding_delivery_and_shared_receiver_contract()
    {
        var (_, receiver, link, channel) = Fixture();
        channel.Request(link); channel.Dispatch();
        var roots = GojoNumericFormat.Decode(GojoNumericFormat.Encode(channel.ArchiveSurface(), receiver.ArchiveSurface()));
        var restoredChannel = AcknowledgedComparisonChannel.Restore((Relation)roots[0]);
        var restoredReceiver = CoupledExtentMachine.Restore((Relation)roots[1]);
        var restored = restoredChannel.Snapshot();
        Assert.True(restored.Waiting); Assert.True(restored.Dispatched); Assert.False(restored.ReceiptReady);
        var response = restoredReceiver.Step(restored.Packet!);
        var receipt = restoredChannel.Receipt(restoredReceiver, response);
        Assert.True(receipt.Accepted); Assert.Equal(2, restoredReceiver.Snapshot().Value);
        var receivedArchive = AcknowledgedComparisonChannel.Decode(restoredChannel.Encode());
        Assert.True(receivedArchive.Snapshot().ReceiptReady);
        Assert.True(receivedArchive.Acknowledge(receivedArchive.Snapshot().Receipt!).Accepted);
        Assert.False(receivedArchive.Snapshot().Waiting);
        Assert.True(channel.Snapshot().Waiting); Assert.Equal(1, receiver.Snapshot().Value);
    }

    [Fact]
    public void Stable_goal_with_delayed_delivery_and_acknowledgement_converges_without_stale_pipeline_overshoot()
    {
        var (goal, receiver, link, channel) = Fixture();
        goal.Set(4);
        var dispatchAt = -1; var ackAt = -1; Relation? receipt = null;
        for (var beat = 0; beat < 40; beat++)
        {
            if (beat == ackAt) { Assert.True(channel.Acknowledge(receipt!).Accepted); receipt = null; }
            if (!channel.Snapshot().Waiting) { Assert.True(channel.Request(link).Accepted); dispatchAt = beat + 3; }
            if (beat == dispatchAt)
            {
                var delivered = channel.Dispatch();
                receipt = channel.Receipt(receiver, receiver.Step(delivered.Packet!)).Receipt;
                ackAt = beat + 3;
            }
            Assert.InRange(receiver.Snapshot().Value, 1, 4);
            Assert.InRange(channel.Snapshot().Occupancy, 0, 1);
        }
        Assert.Equal(4, receiver.Snapshot().Value); Assert.Equal(0, receiver.Snapshot().Tension);
    }

    [Fact]
    public void Lost_acknowledgement_keeps_the_slot_held_even_after_the_receiver_moves()
    {
        var (_, receiver, link, channel) = Fixture();
        channel.Request(link);
        channel.Receipt(receiver, receiver.Step(channel.Dispatch().Packet!));
        for (var i = 0; i < 5; i++) Assert.False(channel.Request(link).Accepted);
        Assert.True(channel.Snapshot().Waiting); Assert.True(channel.Snapshot().ReceiptReady);
        Assert.Equal(2, receiver.Snapshot().Value);
    }

    private static (Carrier Goal, CoupledExtentMachine Receiver, CoupledComparisonLink Link,
        AcknowledgedComparisonChannel Channel) Fixture(long initial = 1, long capacity = 8)
    {
        var goal = new Carrier(); goal.Add(5);
        var receiver = CoupledExtentMachine.Create(initial, capacity, unit: goal.UnitShape);
        var link = CoupledComparisonLink.Create(new Relation(goal.ValueSlot, goal), receiver);
        return (goal, receiver, link, AcknowledgedComparisonChannel.Create(receiver));
    }

    private static IEnumerable<IElement> Reachable(IElement root)
    {
        var seen = new HashSet<IElement>(ReferenceEqualityComparer.Instance);
        var todo = new Stack<IElement>(); todo.Push(root);
        while (todo.TryPop(out var next))
        {
            if (!seen.Add(next)) continue;
            yield return next;
            if (next is Relation relation) { todo.Push(relation.Left); todo.Push(relation.Right); }
        }
    }
}
