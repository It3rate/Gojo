using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;
using Gojo.Experiments.QualitativeFrieze.Composition;

namespace QualitativeExecution.Tests;

public sealed class ExtentRouteRetentionTests
{
    [Fact]
    public void Two_directional_challenges_retain_actual_feedback_archive_without_score_ranking()
    {
        var report = ExtentRouteRetention.Report();
        Assert.Equal(1, report.RetainedCount);
        var selected = Assert.Single(report.Candidates.Where(c => c.Retained));
        Assert.Equal("feedback", selected.Policy);
        Assert.Equal(new long[] { 5, 1 }, selected.Probes.Select(p => p.Goal));
        Assert.Equal(new byte[] { 4, 1 }, selected.Probes.Select(p => p.BeforeMask));
        Assert.All(selected.Probes, p => { Assert.True(p.Qualifies); Assert.Equal(2, p.AfterMask); });
        Assert.All(report.Candidates.Where(c => !c.Retained), c => Assert.All(c.Probes, p => Assert.False(p.Qualifies)));
        Assert.True(FiniteLaw.TryReadResult(selected.RetentionRead, out var outcome, out var pending, out _));
        Assert.Same(selected.CandidateArchive, Assert.Single(FiniteLaw.Entries(outcome)));
        Assert.Empty(FiniteLaw.Entries(pending));
        Assert.Contains(Reachable(selected.Surface), part => ReferenceEquals(part, selected.CandidateArchive));
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, true)]
    public void Missing_or_ambiguous_references_cannot_earn_retention(bool available, bool ambiguous)
    {
        var candidate = ExtentRouteRetention.Evaluate("feedback", available, ambiguous);
        Assert.False(candidate.Retained);
        Assert.All(candidate.Probes, p => Assert.False(p.Qualifies));
        Assert.True(FiniteLaw.TryReadResult(candidate.RetentionRead, out var result, out _, out _));
        Assert.True(StructuralConstraint.TryReadScope(result, out _, out var seam, out var tail));
        Assert.NotEqual(seam, tail);
        Assert.Throws<ArgumentException>(() => ExtentRouteRetention.Reuse(candidate, 3));
    }

    [Fact]
    public void Frozen_controller_reuses_public_ports_on_fresh_goals_and_retains_capacity_failure()
    {
        var report = ExtentRouteRetention.Report();
        var good = report.HeldOut.Where(r => r.Observation == "resolved" && r.Goal <= 8).ToArray();
        Assert.Equal(new long[] { 0, 3, 6 }, good.Select(r => r.Goal));
        Assert.All(good, r => { Assert.True(r.GoalReached); Assert.Equal(r.Goal, r.Values[^1]); Assert.Equal(0, r.Tensions[^1]); });
        var full = Assert.Single(report.HeldOut.Where(r => r.Goal == 9));
        Assert.False(full.GoalReached); Assert.Equal(8, full.Values[^1]); Assert.Equal(4, full.Tensions[^1]);
        Assert.All(report.HeldOut.Where(r => r.Observation != "resolved"), r =>
        { Assert.False(r.GoalReached); Assert.All(r.Values, value => Assert.Equal(2, value)); });
    }

    [Fact]
    public void Diagnostic_label_or_byte_buffer_cannot_replace_the_retained_native_machine()
    {
        var retained = ExtentRouteRetention.Evaluate("feedback");
        var corruptedDiagnostics = retained with { Policy = "hold", Archive = CoupledExtentMachine.Create(2, policy: "hold").Encode() };
        var reused = ExtentRouteRetention.Reuse(corruptedDiagnostics, 6);
        Assert.True(reused.GoalReached); Assert.Equal(6, reused.Values[^1]);
        // Only the human label is carried into the report. The native selected
        // archive, not either diagnostic field, supplied the executable law.
        Assert.Equal("hold", reused.Policy);
    }

    [Fact]
    public void A_different_native_archive_cannot_be_substituted_for_the_gates_actual_selected_participant()
    {
        var retained = ExtentRouteRetention.Evaluate("feedback");
        var replaced = retained with { CandidateArchive = CoupledExtentMachine.Create(2, policy: "hold").ArchiveSurface() };
        Assert.Throws<ArgumentException>(() => ExtentRouteRetention.Reuse(replaced, 6));
    }

    private static IEnumerable<IElement> Reachable(IElement root)
    {
        var seen = new HashSet<IElement>(ReferenceEqualityComparer.Instance); var stack = new Stack<IElement>(); stack.Push(root);
        while (stack.TryPop(out var element))
        {
            if (!seen.Add(element)) continue; yield return element;
            if (element is Relation relation) { stack.Push(relation.Left); stack.Push(relation.Right); }
        }
    }
}
