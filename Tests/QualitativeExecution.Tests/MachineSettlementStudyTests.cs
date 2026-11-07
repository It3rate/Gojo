using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;
using Gojo.Experiments.QualitativeFrieze.Composition;

namespace QualitativeExecution.Tests;

public sealed class MachineSettlementStudyTests
{
    [Fact]
    public void A_coarse_feedback_link_reaches_new_extents_without_a_target_picture()
    {
        foreach (var scenario in new[] { "stretch", "diamond", "taller", "both", "reverse", "missing", "ambiguous" })
        {
            var r = MachineSettlementStudy.Execute("chevron", "feedback", scenario, 0);
            Assert.True(r.GoalReached); Assert.True(r.PairAgrees); Assert.True(r.JointGoalsMet);
            Assert.True(r.Quiet); Assert.Equal(0, r.FinalTension);
            Assert.NotNull(r.NativeSurface);
        }
    }

    [Fact]
    public void Unobserved_or_ambiguous_comparisons_do_not_import_hidden_target_values()
    {
        foreach (var scenario in new[] { "missing", "ambiguous", "hidden" })
        {
            var r = MachineSettlementStudy.Execute("chevron", "feedback", scenario, 0);
            foreach (var frame in r.Frames.Where(f => f.Beat is >= 4 and <= 9))
            {
                Assert.Null(frame.Requests[1]); Assert.Null(frame.Requests[3]);
                Assert.Equal(0, frame.Accepted[1]); Assert.Equal(0, frame.Accepted[3]);
                Assert.False(frame.Resolved[1]); Assert.False(frame.Resolved[3]);
                Assert.Equal(scenario == "ambiguous" ? 7 : 0, frame.Supports[1]);
            }
            if (scenario == "hidden") { Assert.False(r.PairAgrees); Assert.True(r.GoalReached); }
        }
    }

    [Fact]
    public void Capacity_failure_and_opposed_requests_preserve_existing_debt()
    {
        var r = MachineSettlementStudy.Execute("chevron", "feedback", "capacity", 0);
        Assert.True(r.GoalReached); Assert.False(r.PairAgrees); Assert.False(r.JointGoalsMet);
        Assert.True(r.Blocked > 0); Assert.True(r.FinalTension > 0);
        Assert.Equal(3, r.Frames[^1].Owners[1].Value);
        var release = MachineSettlementStudy.Execute("chevron", "feedback", "release", 0);
        Assert.True(release.Frames.Any(f => f.Requests[1] == -1 && f.Accepted[1] == 0 && f.Owners[1].Tension > 0));
    }

    [Fact]
    public void Three_drawing_families_rebind_one_control_trace_but_have_different_native_instructions()
    {
        var runs = MachineSettlementStudy.Families.Select(f => MachineSettlementStudy.Execute(f, "feedback", "diamond", 0)).ToArray();
        Assert.All(runs.Skip(1), r => Assert.Equal(runs[0].Frames.Select(f => f.Owners[0].Value), r.Frames.Select(f => f.Owners[0].Value)));
        var chevron = MachineSettlementStudy.WithDrawings(runs[0]);
        var inset = MachineSettlementStudy.WithDrawings(runs[2]);
        Assert.Equal(4, chevron.Frames[^1].Drawing!.Upper.Length);
        Assert.Equal(8, inset.Frames[^1].Drawing!.Upper.Length);
        Assert.Equal(2, chevron.Frames[^1].Drawing!.ZeroSections);
        Assert.Equal(2, chevron.Frames[^1].Drawing!.AcceptedSeams);
        Assert.NotNull(chevron.Frames[^1].PublicSamples);
    }

    [Fact]
    public void Restoring_connected_owners_and_sensor_links_preserves_blocked_continuation()
    {
        var leader = CoupledExtentMachine.Create(5, 8);
        var follower = CoupledExtentMachine.Create(2, 3, unit: leader.StepUnit.UnitShape);
        var link = CoupledComparisonLink.Create(leader.ExtentPort, follower);
        for (var i = 0; i < 4; i++) follower.Step(link.Observe().Packet);
        var roots = GojoNumericFormat.Decode(GojoNumericFormat.Encode(leader.ArchiveSurface(), follower.ArchiveSurface(), link.Surface));
        var a = CoupledExtentMachine.Restore((Relation)roots[0]);
        var b = CoupledExtentMachine.Restore((Relation)roots[1]);
        var restoredLink = CoupledComparisonLink.FromSurface((Relation)roots[2]);
        Assert.Equal(follower.Snapshot().Tension, b.Snapshot().Tension);
        for (var i = 0; i < 3; i++)
        {
            var before = follower.Step(link.Observe().Packet);
            var after = b.Step(restoredLink.Observe().Packet);
            Assert.Equal(before.RequestedDelta, after.RequestedDelta);
            Assert.Equal(before.After.Value, after.After.Value);
            Assert.Equal(before.After.Tension, after.After.Tension);
        }
        Assert.Equal(5, a.Snapshot().Value);
        Assert.NotSame(leader.Owner, a.Owner);
    }

    [Fact]
    public void Comparison_packets_do_not_reach_the_source_machine_but_sensor_witnesses_do()
    {
        var a = CoupledExtentMachine.Create(6); var b = CoupledExtentMachine.Create(1, unit: a.StepUnit.UnitShape);
        var link = CoupledComparisonLink.Create(a.ExtentPort, b); var read = link.Observe();
        Assert.DoesNotContain(Reachable(read.Packet), item => ReferenceEquals(item, a.ExtentPort));
        Assert.Contains(Reachable(read.Witness), item => ReferenceEquals(item, a.ExtentPort));
        Assert.Contains(Reachable(link.Surface), item => ReferenceEquals(item, b.ExtentPort));
    }

    private static IEnumerable<IElement> Reachable(IElement source)
    {
        var seen = new HashSet<IElement>(ReferenceEqualityComparer.Instance); var todo = new Stack<IElement>(); todo.Push(source);
        while (todo.TryPop(out var next)) { if (!seen.Add(next)) continue; yield return next;
            if (next is Relation r) { todo.Push(r.Left); todo.Push(r.Right); } }
    }
}
