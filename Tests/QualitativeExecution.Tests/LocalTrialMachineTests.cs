using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeFrieze.Composition;

namespace QualitativeExecution.Tests;

public sealed class LocalTrialMachineTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Positive_blocked_offer_selects_negative_and_requires_a_quiet_grant(bool compiled)
    {
        var (trial, quiet, receiver, other) = Fixture(initial: 2, capacity: 2);
        var selection = trial.Select(trial.Offer(1, false, true, receiver.ExtentPort),
            trial.Offer(-1, true, true, receiver.ExtentPort), trial.Need(true, new Placeholder()), compiled);
        Assert.True(selection.Selected); Assert.Equal(-1, selection.Delta);
        Assert.Equal(2, trial.Allowance); Assert.True(trial.NeedsGrant);
        var fake = new Relation(new Relation(quiet.PublicPort, selection.Request!), new Placeholder());
        Assert.False(trial.Dispatch(quiet, fake).Accepted); Assert.Equal(2, trial.Allowance);
        var grant = quiet.Grant(selection.Request!);
        Assert.True(grant.Accepted); Assert.True(quiet.Quiet);
        var held = quiet.Filter(other.Publication(4), compiled);
        Assert.Equal(2, held.Mask); Assert.Equal(0, other.Step(held.Packet, compiled).AcceptedDelta);
        Assert.Equal(2, quiet.Filter(other.Publication(7), compiled).Mask);
        Assert.Equal(0, quiet.Filter(other.Publication(0), compiled).Mask);
        var dispatch = trial.Dispatch(quiet, grant.Grant!);
        Assert.True(dispatch.Accepted); Assert.Equal(1, trial.Allowance);
        Assert.False(trial.Dispatch(quiet, grant.Grant!).Accepted);
        var step = receiver.Step(dispatch.Packet!, compiled);
        Assert.Equal(-1, step.AcceptedDelta);
        Assert.True(trial.Complete(receiver, step, quiet).Accepted);
        Assert.False(trial.Outstanding); Assert.True(quiet.Quiet);
        Assert.True(quiet.Release(grant.Grant!).Accepted);
        Assert.Equal(4, quiet.Filter(other.Publication(4), compiled).Mask);
    }

    [Theory]
    [InlineData(null, true, true)]
    [InlineData(true, null, true)]
    [InlineData(true, true, null)]
    [InlineData(true, true, false)]
    public void Open_or_unwanted_trials_do_not_spend_allowance(bool? feasible, bool? distinguishes, bool? needed)
    {
        var (trial, _, receiver, _) = Fixture();
        var choice = trial.Select(trial.Offer(1, feasible, distinguishes, receiver.ExtentPort),
            trial.Offer(-1, false, true, receiver.ExtentPort), trial.Need(needed, new Placeholder()));
        Assert.False(choice.Selected); Assert.Null(choice.Request); Assert.Null(choice.Packet);
        Assert.False(trial.Outstanding); Assert.Equal(2, trial.Allowance);
    }

    [Fact]
    public void Fixed_priority_is_visible_in_native_order_and_can_be_reversed()
    {
        var receiver = CoupledExtentMachine.Create(1); var other = CoupledExtentMachine.Create(1);
        var quiet = TrialQuietPort.Create(other);
        var trial = LocalTrialMachine.Create(receiver, quiet.PublicPort, preferPositive: false);
        var selected = Ready(trial);
        Assert.Equal(-1, selected.Delta);
    }

    [Fact]
    public void Repeated_selection_during_delayed_grant_retains_the_same_request_and_packet()
    {
        var (trial, quiet, _, _) = Fixture();
        var first = Ready(trial);
        var second = trial.Select(trial.Offer(1, false, false, new Placeholder()),
            trial.Offer(-1, true, true, new Placeholder()), trial.Need(true, new Placeholder()));
        Assert.Same(first.Request, second.Request); Assert.Same(first.Packet, second.Packet);
        Assert.Equal(1, second.Delta); Assert.Equal(2, trial.Allowance);
        var firstGrant = quiet.Grant(first.Request!).Grant;
        Assert.Same(firstGrant, quiet.Grant(first.Request!).Grant);
    }

    [Fact]
    public void Two_clients_share_one_quiet_slot_and_old_grants_cannot_dispatch_or_release_it()
    {
        var (first, quiet, _, _) = Fixture();
        var anotherReceiver = CoupledExtentMachine.Create(1);
        var second = LocalTrialMachine.Create(anotherReceiver, quiet.PublicPort);
        var a = Ready(first); var b = Ready(second);
        var grantA = quiet.Grant(a.Request!).Grant!;
        Assert.False(quiet.Grant(b.Request!).Accepted);
        Assert.True(quiet.Release(grantA).Accepted);
        Assert.False(first.Dispatch(quiet, grantA).Accepted);
        var grantB = quiet.Grant(b.Request!).Grant!;
        Assert.False(quiet.Release(grantA).Accepted); Assert.True(quiet.Quiet);
        Assert.False(quiet.Release(new Relation(grantB.Left, grantB.Right)).Accepted);
        Assert.True(second.Dispatch(quiet, grantB).Accepted);
        Assert.False(quiet.Grant(a.Request!).Accepted);
        Assert.Equal(2, first.Allowance); Assert.Equal(1, second.Allowance);
    }

    [Fact]
    public void Blocked_actual_attempt_still_spends_one_allowance_without_asserting_motion()
    {
        var (trial, quiet, receiver, _) = Fixture(initial: 2, capacity: 2, allowance: 1);
        var selection = Ready(trial); // deliberately wrong advertised feasibility
        var grant = quiet.Grant(selection.Request!).Grant!;
        var dispatched = trial.Dispatch(quiet, grant);
        var step = receiver.Step(dispatched.Packet!);
        Assert.Equal(0, step.AcceptedDelta); Assert.Equal(1, step.PendingDelta);
        Assert.True(trial.Complete(receiver, step, quiet).Accepted);
        Assert.True(quiet.Release(grant).Accepted);
        Assert.Equal(0, trial.Allowance); Assert.False(Ready(trial).Selected);
        Assert.Equal(1, receiver.Snapshot().Tension);
    }

    [Fact]
    public void Completion_requires_exact_dispatched_packet_and_current_actual_step()
    {
        var (trial, quiet, receiver, _) = Fixture();
        var selection = Ready(trial); var grant = quiet.Grant(selection.Request!).Grant!;
        var dispatch = trial.Dispatch(quiet, grant);
        var paused = receiver.Step(dispatch.Packet!, pulse: false);
        Assert.False(trial.Complete(receiver, paused, quiet).Accepted);
        Assert.False(trial.Complete(receiver, paused with { ConsumedEvent = true }, quiet).Accepted);
        Assert.False(trial.Complete(receiver, receiver.Step(receiver.Publication(4)), quiet).Accepted);
        var actual = receiver.Step(dispatch.Packet!);
        var later = receiver.Step(receiver.Publication(2));
        Assert.False(trial.Complete(receiver, actual, quiet).Accepted);
        Assert.False(trial.Complete(receiver, later, quiet).Accepted);
        Assert.True(trial.Outstanding); Assert.Equal(1, trial.Allowance);
    }

    [Fact]
    public void Release_before_completion_does_not_claim_an_isolated_trial()
    {
        var (trial, quiet, receiver, _) = Fixture();
        var selection = Ready(trial); var grant = quiet.Grant(selection.Request!).Grant!;
        var dispatch = trial.Dispatch(quiet, grant);
        var step = receiver.Step(dispatch.Packet!);
        quiet.Release(grant);
        Assert.False(trial.Complete(receiver, step, quiet).Accepted);
        Assert.True(trial.Outstanding);
    }

    [Fact]
    public void Cancel_only_withdraws_an_undispatched_request_and_does_not_spend()
    {
        var (trial, quiet, _, _) = Fixture();
        var first = Ready(trial);
        Assert.False(trial.Cancel(new Relation(first.Request!.Left, first.Request.Right)).Accepted);
        Assert.True(trial.Cancel(first.Request).Accepted); Assert.Equal(2, trial.Allowance);
        var second = Ready(trial); Assert.NotSame(first.Request, second.Request);
        var grant = quiet.Grant(second.Request!).Grant!; trial.Dispatch(quiet, grant);
        Assert.False(trial.Cancel(second.Request!).Accepted); Assert.Equal(1, trial.Allowance);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Archive_restores_pending_or_dispatched_trial_with_exact_quiet_correspondence(bool dispatched)
    {
        var (trial, quiet, receiver, other) = Fixture();
        var choice = Ready(trial); var grant = quiet.Grant(choice.Request!).Grant!;
        if (dispatched) trial.Dispatch(quiet, grant);
        var roots = GojoNumericFormat.Decode(GojoNumericFormat.Encode(trial.ArchiveSurface(), quiet.ArchiveSurface(),
            receiver.ArchiveSurface(), other.ArchiveSurface()));
        var restored = LocalTrialMachine.Restore((Relation)roots[0]);
        var restoredQuiet = TrialQuietPort.Restore((Relation)roots[1]);
        var restoredReceiver = CoupledExtentMachine.Restore((Relation)roots[2]);
        var restoredOther = CoupledExtentMachine.Restore((Relation)roots[3]);
        Assert.True(restored.Outstanding); Assert.Equal(dispatched, restored.Snapshot().Dispatched);
        Assert.Equal(dispatched ? 1 : 2, restored.Allowance);
        var restoredGrant = restoredQuiet.Snapshot().Grant!;
        var restoredRequest = restored.Snapshot().Request!;
        var packet = dispatched ? restored.Snapshot().Packet! : restored.Dispatch(restoredQuiet, restoredGrant).Packet!;
        Assert.Equal(0, restoredOther.Step(restoredQuiet.Filter(restoredOther.Publication(4)).Packet).AcceptedDelta);
        Assert.True(restored.Complete(restoredReceiver, restoredReceiver.Step(packet), restoredQuiet).Accepted);
        Assert.True(restoredQuiet.Release(restoredGrant).Accepted);
        Assert.False(restoredQuiet.Grant(restoredRequest).Accepted);
        Assert.Equal(1, restored.Allowance); Assert.True(trial.Outstanding); Assert.True(quiet.Quiet);
    }

    [Fact]
    public void Quiet_grant_has_no_receiver_state_and_foreign_packets_cannot_gain_scope()
    {
        var (trial, quiet, receiver, other) = Fixture();
        var choice = Ready(trial); var grant = quiet.Grant(choice.Request!).Grant!;
        var contents = Reachable(grant).ToArray();
        Assert.DoesNotContain(contents, n => ReferenceEquals(n, receiver.ExtentPort));
        Assert.DoesNotContain(contents, n => ReferenceEquals(n, other.ExtentPort));
        Assert.DoesNotContain(contents, n => ReferenceEquals(n, choice.Packet));
        Assert.Equal(0, quiet.Filter(receiver.Publication(4)).Mask);
    }

    [Fact]
    public void Quiet_is_a_routed_command_promise_not_a_global_freeze()
    {
        var other = CoupledExtentMachine.Create(1, policy: "clock");
        var quiet = TrialQuietPort.Create(other);
        var trial = LocalTrialMachine.Create(CoupledExtentMachine.Create(1), quiet.PublicPort);
        quiet.Grant(Ready(trial).Request!);
        var filtered = quiet.Filter(other.Publication(4));
        Assert.Equal(2, filtered.Mask);
        // A clock-driven actuator does not interpret the comparison as its
        // motion command. A caller must retain that actual second change.
        Assert.Equal(1, other.Step(filtered.Packet).AcceptedDelta);
    }

    private static LocalTrialMachine.SelectionRead Ready(LocalTrialMachine trial) => trial.Select(
        trial.Offer(1, true, true, new Placeholder()), trial.Offer(-1, true, true, new Placeholder()),
        trial.Need(true, new Placeholder()));

    private static (LocalTrialMachine Trial, TrialQuietPort Quiet, CoupledExtentMachine Receiver, CoupledExtentMachine Other)
        Fixture(long initial = 1, long capacity = 8, long allowance = 2)
    {
        var receiver = CoupledExtentMachine.Create(initial, capacity); var other = CoupledExtentMachine.Create(1);
        var quiet = TrialQuietPort.Create(other);
        return (LocalTrialMachine.Create(receiver, quiet.PublicPort, allowance), quiet, receiver, other);
    }

    private static IEnumerable<IElement> Reachable(IElement root)
    {
        var seen = new HashSet<IElement>(ReferenceEqualityComparer.Instance); var todo = new Stack<IElement>(); todo.Push(root);
        while (todo.TryPop(out var next))
        {
            if (!seen.Add(next)) continue;
            yield return next;
            if (next is Relation relation) { todo.Push(relation.Left); todo.Push(relation.Right); }
        }
    }
}
