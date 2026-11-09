using System.Text.Json;
using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;
using Gojo.Experiments.QualitativeFrieze.Composition;
using Study = Gojo.Experiments.QualitativeFrieze.Composition.SymmetryCompositionStudy;
using Curve = Gojo.Experiments.QualitativeExecution.QualitativeCurve;

namespace QualitativeExecution.Tests;

public sealed class SymmetryCompositionTests
{
    private static readonly Lazy<Study.Report> StudyReport = new(Study.Explore);
    private static Study.Report Report => StudyReport.Value;
    private static Study.Sample Sample(string id) => Assert.Single(Report.Samples, s => s.Id == id);
    private static Study.Comparison Comparison(string id) => Assert.Single(Report.Comparisons, s => s.Id == id);

    [Fact]
    public void Source_choice_is_connected_to_actual_inputs_not_a_machine_mode_label()
    {
        var original = Sample("original-fixed"); var feedback = Sample("feedback");
        var fixedReads = Applications(original); var feedbackReads = Applications(feedback);
        Assert.Equal(3, fixedReads.Length); Assert.Equal(3, feedbackReads.Length);
        var seed = ((Relation)original.Surface.Left).Left;
        Assert.All(fixedReads, r => { Assert.Same(seed, r.Source); Assert.Same(fixedReads[0].Action, r.Action); });
        Assert.Same(seed, feedbackReads[0].Source);
        Assert.Same(fixedReads[0].Action, feedbackReads[0].Action);
        for (var i = 1; i < feedbackReads.Length; i++)
            Assert.Same(feedbackReads[i - 1].Output, feedbackReads[i].Source);
        Assert.False(Comparison("source-choice").AlignedEqual);
    }

    [Fact]
    public void Accumulated_group_is_the_later_input_and_duplicate_occurrences_survive()
    {
        var group = Sample("whole-group"); var timeline = Timeline(group);
        var applications = Applications(group);
        Assert.Equal(new[] { 5, 10, 20 }, applications.Select(a => E(a.Source).Length));
        Assert.Same(((Relation)timeline[1]).Right, applications[1].Source);
        Assert.Same(((Relation)timeline[3]).Right, applications[2].Source);
        Assert.Equal(40, group.Sections); Assert.Equal(20, Sample("feedback").Sections);
        var read = BoundaryExchangeRead.Evaluate(group.Frame, Sample("feedback").Output, group.Output);
        Assert.Empty(E(read.Unresolved)); Assert.Empty(E(read.SourceOnly));
        Assert.NotEmpty(E(read.TargetOnly)); Assert.False(read.Equivalent);
        Assert.False(Comparison("group-choice").AlignedEqual);
    }

    [Fact]
    public void Fixed_placement_keeps_overlapping_copies_and_advancing_clock_is_distinct()
    {
        var original = Sample("original-fixed"); var clock = Sample("original-clock");
        Assert.Equal(20, original.Sections); Assert.Equal(20, clock.Sections);
        var uniqueLines = original.Lines.Select(LineKey).Distinct().Count();
        Assert.Equal(10, uniqueLines);
        Assert.All(Applications(clock), r => Assert.Same(Applications(original)[0].Source, r.Source));
        Assert.False(Comparison("placement-clock").AlignedEqual);
        // Pure visibility would fuse the repeated stamp; native accounting does not.
        var once = StructuralConstraint.Closed(E(original.Blocks[0]).Concat(E(original.Blocks[1])).ToArray());
        var read = BoundaryExchangeRead.Evaluate(original.Frame, once, original.Output);
        Assert.Empty(E(read.SourceOnly)); Assert.NotEmpty(E(read.TargetOnly));
    }

    [Fact]
    public void Action_order_commutes_only_for_the_selected_frame_and_displacement()
    {
        var perpendicular = Comparison("commuting"); var sameAxis = Comparison("noncommuting");
        Assert.True(perpendicular.RawEqual); Assert.True(perpendicular.DirectedEqual);
        Assert.False(sameAxis.RawEqual); Assert.False(sameAxis.DirectedEqual);
        var forward = Applications(Sample("mirror-y-advance"));
        var swapped = Applications(Sample("advance-mirror-y"));
        Assert.Same(forward[0].Action, swapped[1].Action);
        Assert.Same(forward[1].Action, swapped[0].Action);
        Assert.Same(forward[0].Output, forward[1].Source);
        Assert.Same(swapped[0].Output, swapped[1].Source);
    }

    [Fact]
    public void Finite_crop_mismatch_is_retained_while_supplied_cyclic_correspondence_matches()
    {
        var finite = Comparison("finite-phase"); var cyclic = Comparison("cyclic-phase");
        Assert.False(finite.RawEqual); Assert.False(finite.AlignedEqual);
        Assert.False(cyclic.RawEqual); Assert.True(cyclic.AlignedEqual); Assert.True(cyclic.DirectedEqual);
        var finiteFields = E((Relation)finite.Surface.Right);
        Assert.True(BoundaryExchangeRead.TryRead(Sample("phase-left").Frame, (Relation)finiteFields[2], out var finiteRead));
        Assert.NotEmpty(E(finiteRead.SourceOnly)); Assert.NotEmpty(E(finiteRead.TargetOnly));
        var cyclicFields = E((Relation)cyclic.Surface.Right);
        Assert.True(BoundaryExchangeRead.TryRead(Sample("phase-left").Frame, (Relation)cyclicFields[2], out var cyclicRead));
        Assert.Equal(E(cyclicRead.SourceAtoms).Length, E(cyclicRead.TargetAtoms).Length);
        Assert.Equal(E(cyclicRead.SourceAtoms).Length, E(cyclicRead.Matches).Length);
        Assert.Equal(10, cyclic.AlignedLines.Length);
    }

    [Fact]
    public void Periodic_alignment_retains_its_continuation_rule_and_actual_block_routes()
    {
        var candidate = Sample("phase-right"); var evidence = (Relation)candidate.Surface.Right;
        var declaration = (Relation)evidence.Left; var recurrence = (Relation)declaration.Left;
        var ports = (Relation)recurrence.Left; var instruction = (Relation)ports.Right;
        var feedback = (Relation)recurrence.Right; var binding = (Relation)declaration.Right;
        Assert.Same(instruction.Right, feedback.Left); Assert.Same(ports.Left, feedback.Right);
        Assert.Same(ports.Left, binding.Left); Assert.Same(candidate.Output, binding.Right);
        Assert.True(CentrelineAction.TryReadApplication(candidate.Frame, (Relation)evidence.Right, out var continuation));
        Assert.Same(instruction.Left, continuation.Action); Assert.Same(candidate.Output, continuation.Source);

        var fields = E((Relation)Comparison("cyclic-phase").Surface.Right);
        var alignment = (Relation)fields[0]; Assert.Same(candidate.Surface, alignment.Left);
        var trace = (Relation)alignment.Right; var timeline = E((Relation)((Relation)trace.Left).Right);
        var reads = timeline.OfType<Relation>().Select(a =>
            CentrelineAction.TryReadApplication(candidate.Frame, a, out var r) ? r : null).Where(r => r is not null).ToArray();
        Assert.Equal(3, reads.Length);
        Assert.Same(candidate.Blocks[0], reads[0]!.Source); Assert.Same(candidate.Blocks[1], reads[1]!.Source);
        Assert.Same(reads[1]!.Output, reads[2]!.Source);
    }

    [Fact]
    public void Cyclic_permission_binds_actual_native_slots_to_period_and_inverse_wrap()
    {
        var candidate = Sample("phase-right");
        Assert.True(Study.TryReadCyclicCell(candidate.Frame, candidate.Surface, out var cell));
        Assert.Same(candidate.Output, cell.Cell); Assert.Equal(2, cell.Slots.Length);
        Assert.Same(candidate.Blocks[0], cell.Slots[0]); Assert.Same(candidate.Blocks[1], cell.Slots[1]);
        var facts = E((Relation)cell.Permission.Right);
        Assert.Equal(5, facts.Length);
        Assert.True(candidate.Frame.Motion.TryReadResult((Relation)facts[1], out var periodWord, out _, out var pending));
        Assert.Empty(E(pending)); Assert.Same(periodWord, ((Relation)cell.PeriodAction.Right).Left);
        Assert.True(candidate.Frame.Motion.TryReadResult((Relation)facts[2], out var cancellation, out _, out pending));
        Assert.Empty(E(pending)); Assert.Empty(E(cancellation));
        Assert.True(CentrelineAction.TryReadMatch(candidate.Frame, (Relation)facts[4], out var roundtrip));
        Assert.True(roundtrip.Complete); Assert.True(roundtrip.GeometryExact); Assert.True(roundtrip.SitePatternExact);
        // The optional report cache is not the source of the cell's grouping.
        var misleadingCache = candidate with { Blocks = [] };
        Assert.True(Study.TryAlignCyclicCell(misleadingCache.Frame, misleadingCache.Surface, out var aligned, out _));
        Assert.True(BoundaryExchangeRead.Evaluate(candidate.Frame, Sample("phase-left").Output, aligned).Equivalent);
    }

    [Fact]
    public void A_changed_valid_forward_period_does_not_reuse_an_old_cyclic_permission()
    {
        var candidate = Sample("phase-right"); var evidence = (Relation)candidate.Surface.Right;
        var declaration = (Relation)evidence.Left; var recurrence = (Relation)declaration.Left;
        var ports = (Relation)recurrence.Left; var instruction = (Relation)ports.Right;
        var changedPeriod = CentrelineAction.FromBits(candidate.Frame, 0, candidate.Frame.Word(14, 0));
        var changedRule = new Relation(new Relation(ports.Left, new Relation(changedPeriod, instruction.Right)), recurrence.Right);
        var changedApplication = CentrelineAction.Apply(candidate.Frame, changedPeriod, candidate.Output);
        Assert.True(CentrelineAction.TryReadApplication(candidate.Frame, changedApplication, out var valid));
        Assert.True(valid.Complete);
        var changedEvidence = new Relation(new Relation(changedRule, declaration.Right), changedApplication);
        AssertRejected(new Relation(candidate.Surface.Left, changedEvidence), candidate.Frame);
    }

    [Fact]
    public void Forward_continuation_without_an_explicit_cell_quotient_cannot_wrap()
    {
        var candidate = Sample("phase-right"); var owner = (Relation)candidate.Surface.Left;
        var evidence = (Relation)candidate.Surface.Right;
        Assert.True(CentrelineAction.TryReadApplication(candidate.Frame, (Relation)evidence.Right, out var continuation));
        Assert.True(continuation.Complete);
        var noPermission = new Relation(new Relation(owner.Left, StructuralConstraint.Closed()), evidence);
        AssertRejected(noPermission, candidate.Frame);
    }

    [Fact]
    public void Open_native_cell_group_cannot_claim_exhaustive_two_slot_alignment()
    {
        var candidate = Sample("phase-right"); var owner = (Relation)candidate.Surface.Left;
        var trace = (Relation)owner.Left; var header = (Relation)trace.Left;
        var history = E((Relation)header.Right); var group = (Relation)history[^1];
        var openGroup = new Relation(StructuralConstraint.Open(E((Relation)group.Left)), group.Right);
        history[^1] = openGroup;
        var openTrace = new Relation(new Relation(header.Left, StructuralConstraint.Closed(history)), trace.Right);
        var permission = (Relation)owner.Right; var selection = (Relation)permission.Left;
        var changedPermission = new Relation(new Relation(openGroup, selection.Right), permission.Right);
        AssertRejected(new Relation(new Relation(openTrace, changedPermission), candidate.Surface.Right), candidate.Frame);
    }

    [Fact]
    public void Added_half_motif_mark_is_not_discarded_by_phase_alignment()
    {
        var broken = Comparison("broken-phase"); Assert.False(broken.AlignedEqual); Assert.False(broken.DirectedEqual);
        Assert.Equal(11, broken.AlignedLines.Length);
        var fields = E((Relation)broken.Surface.Right);
        Assert.True(BoundaryExchangeRead.TryRead(Sample("phase-left").Frame, (Relation)fields[2], out var read));
        Assert.Empty(E(read.Unresolved)); Assert.Empty(E(read.SourceOnly)); Assert.Single(E(read.TargetOnly));
    }

    [Fact]
    public void Repeated_action_copies_observation_preserving_closure_without_welding_sites()
    {
        var sample = Sample("feedback"); var reads = Applications(sample);
        var seed = E(reads[0].Source).Cast<Relation>().Select(sample.Frame.Read).ToArray();
        Assert.Same(seed[0].Start, seed[^1].End);
        foreach (var application in reads)
        {
            var copy = E(application.Output).Cast<Relation>().Select(sample.Frame.Read).ToArray();
            Assert.Same(copy[0].Start, copy[^1].End);
            Assert.NotSame(seed[0].Start, copy[0].Start);
            Assert.Equal(5, copy.Length);
        }
        Assert.Equal(new[] { 0, 0 }, new[] { (int)seed[0].Controls[0].X, (int)seed[0].Controls[0].Y });
        Assert.Equal(new[] { 3, 1 }, new[] { (int)seed[2].Controls[1].X, (int)seed[2].Controls[1].Y });
        Assert.Equal(16, Sample("zigzag-feedback").Sections);
    }

    [Fact]
    public void Native_archive_restores_comparison_and_evidence_without_the_report_labels()
    {
        var comparison = Comparison("cyclic-phase");
        var root = Assert.IsType<Relation>(Assert.Single(GojoNumericFormat.Decode(GojoNumericFormat.Encode(comparison.Surface))));
        var fields = E((Relation)root.Right); var boundary = (Relation)fields[2];
        var request = (Relation)((Relation)boundary.Left).Left;
        var frame = Curve.Frame.FromSurface((Relation)request.Left);
        Assert.True(BoundaryExchangeRead.TryRead(frame, boundary, out var read)); Assert.True(read.Equivalent);
        Assert.True(BoundaryExchangeRead.TryRead(frame, (Relation)fields[1], out var raw)); Assert.False(raw.Equivalent);
        Assert.True(BoundaryExchangeRead.TryRead(frame, (Relation)fields[3], out var directed)); Assert.True(directed.Equivalent);
        var candidateSurface = (Relation)((Relation)root.Left).Right;
        Assert.True(Study.TryReadCyclicCell(frame, candidateSurface, out _));
        Assert.True(Study.TryAlignCyclicCell(frame, candidateSurface, out var replay, out _));
        var selectedScopes = (Relation)request.Right;
        Assert.True(BoundaryExchangeRead.Evaluate(frame, replay, (Relation)selectedScopes.Right).Equivalent);
    }

    [Fact]
    public void Report_is_a_bounded_projection_with_no_native_graph_in_json()
    {
        Assert.Equal(12, Report.Samples.Length); Assert.Equal(8, Report.Comparisons.Length);
        using var json = JsonDocument.Parse(JsonSerializer.Serialize(Report));
        Assert.False(json.RootElement.GetProperty("Samples")[0].TryGetProperty("Surface", out _));
        Assert.All(Report.Comparisons, c =>
        {
            var fields = E((Relation)c.Surface.Right); var frame = Sample(c.From).Frame;
            for (var i = 1; i <= 3; i++)
            {
                Assert.True(BoundaryExchangeRead.TryRead(frame, (Relation)fields[i], out var read));
                Assert.Empty(E(read.Unresolved));
            }
        });
    }

    private static CentrelineAction.ApplicationRead[] Applications(Study.Sample sample) => Timeline(sample)
        .OfType<Relation>().Select(row => CentrelineAction.TryReadApplication(sample.Frame, row, out var read) ? read : null)
        .Where(read => read is not null).Cast<CentrelineAction.ApplicationRead>().ToArray();
    private static IElement[] Timeline(Study.Sample sample) => E((Relation)((Relation)sample.Surface.Left).Right);
    private static string LineKey(long[][] line) => string.Join("/", line.Select(point => string.Join(",", point)));
    private static void AssertRejected(Relation surface, Curve.Frame frame)
    {
        Assert.False(Study.TryReadCyclicCell(frame, surface, out _));
        Assert.False(Study.TryAlignCyclicCell(frame, surface, out var output, out var evidence));
        Assert.Same(surface, evidence.Left); Assert.Same(output, evidence.Right);
        Assert.True(StructuralConstraint.TryReadScope(output, out _, out var seam, out var tail));
        Assert.NotSame(seam, tail);
    }
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
}
