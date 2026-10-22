using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;
using Gojo.Experiments.QualitativeFrieze.Composition;
using Curve = Gojo.Experiments.QualitativeExecution.QualitativeCurve;
using Scenes = Gojo.Experiments.QualitativeFrieze.Composition.CurveEvidenceScene;
using Stats = Gojo.Experiments.QualitativeExecution.StatisticalEvidence;

namespace QualitativeExecution.Tests;

public sealed class CurveEvidenceIntegrationTests
{
    private static readonly Lazy<IReadOnlyList<CurveEvidenceCorpus.Case>> Corpus = new(CurveEvidenceCorpus.Cases);

    [Fact]
    public void Publication_copies_endpoint_scaffold_into_new_roles_and_withholds_private_handles_and_schemes()
    {
        var source = Case("C005").Source; var message = Scenes.Publish(source);
        Assert.NotSame(source.Frame, message.Frame); Assert.NotSame(source.Frame.Reach, message.Frame.Reach);
        var exposed = Reachable(message.Surface).ToHashSet(ReferenceEqualityComparer.Instance);
        Assert.DoesNotContain(source.Surface, exposed); Assert.DoesNotContain(source.Frame.Surface, exposed);
        for (var i = 0; i < source.Reads.Count; i++)
        {
            var read = source.Reads[i]; var span = message.Spans[i];
            Assert.DoesNotContain(read.Section, exposed);
            Assert.DoesNotContain(read.Start, exposed); Assert.DoesNotContain(read.End, exposed);
            Assert.DoesNotContain(read.ControlWords, exposed);
            Assert.All(Entries(read.ControlWords), control => Assert.DoesNotContain(control, exposed));
            Assert.True(message.Frame.Vector(span.Start, out var start)); Assert.Equal(read.Controls[0], start);
            Assert.True(message.Frame.Vector(span.End, out var end)); Assert.Equal(read.Controls[^1], end);
        }
        // Shared source topology survives through new public identities.
        for (var i = 0; i < message.Spans.Count; i++)
            Assert.Same(message.Spans[i].To, message.Spans[(i + 1) % message.Spans.Count].From);
    }

    [Fact]
    public void Distinct_private_bulges_can_publish_the_same_coarse_curve_description()
    {
        static Scenes.Scene Bow(int bulge)
        {
            var f = new Curve.Frame(new QualitativeMotionFrame());
            return new(f, [f.Cubic(new Placeholder(), new Placeholder(), f.Word(0, 0),
                f.Word(0, bulge), f.Word(8, bulge), f.Word(8, 0))]);
        }
        var low = Bow(3); var high = Bow(9);
        var a = Scenes.Publish(low); var b = Scenes.Publish(high);
        Assert.NotEqual(low.Reads[0].Controls[1], high.Reads[0].Controls[1]);
        var x = Assert.Single(a.Spans); var y = Assert.Single(b.Spans);
        Assert.True(a.Frame.Vector(x.Start, out var aStart)); Assert.True(b.Frame.Vector(y.Start, out var bStart));
        Assert.True(a.Frame.Vector(x.End, out var aEnd)); Assert.True(b.Frame.Vector(y.End, out var bEnd));
        Assert.Equal(aStart, bStart); Assert.Equal(aEnd, bEnd);
        Assert.Equal(Scenes.Direction(x.StartHeading, a.Frame), Scenes.Direction(y.StartHeading, b.Frame));
        Assert.Equal(Scenes.Direction(x.EndHeading, a.Frame), Scenes.Direction(y.EndHeading, b.Frame));
        Assert.Same(a.CurveTemplate, x.Trace); Assert.Same(b.CurveTemplate, y.Trace);
        Assert.Equal(Entries(x.Bends).Length, Entries(y.Bends).Length);
    }

    [Fact]
    public void Public_trace_exemplars_demonstrate_their_geometry_without_source_handle_access()
    {
        var message = Scenes.Publish(Case("C002").Source);
        var line = message.Frame.Read(message.LineTemplate); var curved = message.Frame.Read(message.CurveTemplate);
        Assert.True(line.Complete); Assert.True(line.Straight); Assert.True(line.Regular);
        Assert.True(curved.Complete); Assert.False(curved.Straight); Assert.True(curved.Regular);
        Assert.Equal(2, line.Controls.Length); Assert.Equal(4, curved.Controls.Length);
        Assert.NotSame(Case("C002").Source.Reads[0].Section, message.CurveTemplate);
    }

    [Fact]
    public void Endpoints_without_trace_or_tangent_reports_leave_priors_unchanged()
    {
        var run = CurveEvidenceExperiment.Calculate(Case("C003"));
        Assert.False(run.Admitted); Assert.Empty(run.Posterior.Events);
        Assert.Equal(run.Prior.Rows.Select(row => row.Probability), run.Posterior.Rows.Select(row => row.Probability));
        Assert.All(run.Assessments, read => { Assert.Equal(0, read.Matches); Assert.Equal(0, read.Conflicts); Assert.True(read.Unresolved > 0); });
        Assert.Equal(3, run.Ties);
        Assert.Contains(run.Candidates, candidate => candidate.Reads.Any(read => read.Straight == false));
        Assert.Contains(run.Candidates, candidate => candidate.Reads.All(read => read.Straight == true));
    }

    [Fact]
    public void Explicit_straightness_revises_a_flowing_prior_without_becoming_a_hard_exclusion()
    {
        var run = CurveEvidenceExperiment.Calculate(Case("C001"));
        Assert.True(run.Admitted); Assert.Single(run.Posterior.Events);
        Assert.Equal(new Stats.Ratio(1, 5), Weight(run.Prior, run.Candidates[0]));
        Assert.Equal(new Stats.Ratio(2, 5), Weight(run.Prior, run.Candidates[2]));
        Assert.True(run.Candidates[run.Selected].Reads.All(read => read.Straight == true));
        Assert.True(Greater(Weight(run.Posterior, run.Candidates[1]), Weight(run.Posterior, run.Candidates[2])));
        Assert.True(Weight(run.Posterior, run.Candidates[2]).Numerator > 0);
        Assert.True(run.Assessments[2].Conflicts > 0);
    }

    [Fact]
    public void An_explicit_curve_report_changes_the_line_curve_choice_with_one_pooled_event()
    {
        var run = CurveEvidenceExperiment.Calculate(Case("C002"));
        Assert.True(run.Admitted); Assert.Single(run.Posterior.Events);
        Assert.True(run.Candidates[0].Reads.All(read => read.Straight == true));
        Assert.True(run.Candidates[run.Selected].Reads.Any(read => read.Straight == false));
        Assert.True(Greater(Weight(run.Prior, run.Candidates[0]), Weight(run.Posterior, run.Candidates[0])));
        Assert.True(run.Assessments[0].Conflicts > 0);
        Assert.All(run.Message.Spans, span => { Assert.False(Closed(span.StartHeading)); Assert.False(Closed(span.Bends)); });
    }

    [Theory]
    [InlineData("C014")]
    [InlineData("C015")]
    public void Closing_a_gap_is_an_assumed_addition_and_does_not_rewrite_the_source(string id)
    {
        var item = Case(id); var source = item.Source; var before = source.Sections.ToArray(); var surface = source.Surface;
        Assert.False(source.Closed); Assert.Empty(Entries(source.Assumed));
        var run = CurveEvidenceExperiment.Calculate(item); var completed = run.Candidates[2];
        Assert.True(completed.Closed); Assert.Equal(source.Sections.Count + 1, completed.Sections.Count);
        var added = Assert.Single(Entries(completed.Assumed)); Assert.Same(completed.Sections[^1], added);
        Assert.False(source.Closed); Assert.Same(surface, source.Surface);
        Assert.Equal(before, source.Sections); Assert.Empty(Entries(source.Assumed));
        Assert.NotSame(run.Message.Spans[^1].To, run.Message.Spans[0].From);
        var bridge = completed.Reads[^1];
        Assert.Same(run.Message.Spans[^1].To, bridge.Start); Assert.Same(run.Message.Spans[0].From, bridge.End);
    }

    [Fact]
    public void Closed_scaffold_does_not_supply_an_unpublished_interior_orientation()
    {
        var run = CurveEvidenceExperiment.Calculate(Case("C018"));
        Assert.True(Case("C018").Source.Closed); Assert.Null(run.Message.InteriorSide);
        Assert.All(run.Candidates, candidate =>
        {
            Assert.Null(candidate.InteriorSide);
            Assert.All(candidate.Reads, read => Assert.Null(Curve.Convexity(read, candidate.InteriorSide)));
        });
        Assert.Contains(run.Candidates.SelectMany(candidate => candidate.Reads), read => read.Regular && read.BendSign is not null and not 0);
    }

    [Fact]
    public void R095_tip_envelope_preserves_the_detailed_boundary_and_does_not_add_independent_evidence()
    {
        var item = Case("C023"); Assert.True(item.OuterFocus); var before = item.Source.Sections.ToArray();
        var run = CurveEvidenceExperiment.Calculate(item); var envelope = Scenes.OuterRead(run.Message);
        Assert.True(envelope.Complete); Assert.Equal(4, envelope.Scene.Sections.Count); Assert.True(envelope.Scene.Closed);
        Assert.Equal(4, Entries(envelope.Scene.Assumed).Length); Assert.True(envelope.Notches > 0);
        Assert.True(item.Source.Sections.Count > envelope.Scene.Sections.Count);
        Assert.Equal(before, item.Source.Sections);
        Assert.Equal(item.Source.Sections.Count, run.Message.Spans.Count);
        Assert.Single(run.Posterior.Events);
        Assert.Contains(Reachable(envelope.Evidence), participant => ReferenceEquals(participant, run.Message.Surface));
        Assert.All(envelope.Scene.Reads, read =>
        {
            Assert.Contains(run.Message.Spans, span => ReferenceEquals(span.From, read.Start) || ReferenceEquals(span.To, read.Start));
            Assert.Contains(run.Message.Spans, span => ReferenceEquals(span.From, read.End) || ReferenceEquals(span.To, read.End));
        });
    }

    [Fact]
    public void Foreign_frame_and_missing_scaffold_cannot_accidentally_agree_with_public_evidence()
    {
        var source = Case("C001").Source; var message = Scenes.Publish(source);
        var other = new Curve.Frame(new QualitativeMotionFrame()); var span = Assert.Single(message.Spans);
        message.Frame.Vector(span.Start, out var start); message.Frame.Vector(span.End, out var end);
        var foreign = new Scenes.Scene(other, [other.Line(span.From, span.To,
            other.Word((int)start.X, (int)start.Y), other.Word((int)end.X, (int)end.Y))]);
        var foreignRead = CurveEvidenceExperiment.Assess(message, foreign);
        Assert.Equal(0, foreignRead.Matches); Assert.True(foreignRead.Unresolved > 0);
        var missing = CurveEvidenceExperiment.Assess(message, new Scenes.Scene(message.Frame, []));
        Assert.Equal(0, missing.Matches); Assert.True(missing.Unresolved > 0);
    }

    [Fact]
    public void Reversed_endpoint_roles_need_a_correspondence_before_span_assessment()
    {
        var message = Scenes.Publish(Case("C001").Source); var span = Assert.Single(message.Spans); var f = message.Frame;
        var reversed = new Scenes.Scene(f, [f.Line(span.To, span.From, span.End, span.Start)]);
        var assessment = CurveEvidenceExperiment.Assess(message, reversed);
        Assert.Equal(0, assessment.Matches); Assert.True(assessment.Unresolved > 0);
    }

    [Fact]
    public void Public_endpoint_correspondence_preserves_assessment_when_candidate_sections_are_reordered()
    {
        var message = Scenes.Publish(Case("C005").Source); var candidate = Scenes.Propose(message, 1);
        var reordered = new Scenes.Scene(candidate.Frame, candidate.Sections.Reverse(), candidate.Assumed, candidate.Interior);
        var originalRead = CurveEvidenceExperiment.Assess(message, candidate);
        var reorderedRead = CurveEvidenceExperiment.Assess(message, reordered);
        Assert.Equal(originalRead.Matches, reorderedRead.Matches);
        Assert.Equal(originalRead.Conflicts, reorderedRead.Conflicts);
        Assert.Equal(originalRead.Unresolved, reorderedRead.Unresolved);
        Assert.Equal(originalRead.Groups.Select(group => (group.Name, group.Matches, group.Conflicts, group.Unresolved)),
            reorderedRead.Groups.Select(group => (group.Name, group.Matches, group.Conflicts, group.Unresolved)));
        Assert.True(originalRead.Matches > 0);
    }

    [Fact]
    public void Duplicate_candidate_spans_leave_correspondence_open_instead_of_selecting_an_arbitrary_copy()
    {
        var message = Scenes.Publish(Case("C001").Source); var candidate = Scenes.Propose(message, 0);
        var section = Assert.Single(candidate.Sections);
        var duplicated = new Scenes.Scene(candidate.Frame, [section, section]);
        var assessment = CurveEvidenceExperiment.Assess(message, duplicated);
        Assert.Equal(0, assessment.Matches); Assert.Equal(0, assessment.Conflicts);
        Assert.True(assessment.Unresolved > 0);
    }

    private static CurveEvidenceCorpus.Case Case(string id) => Corpus.Value.Single(item => item.Id == id);
    private static Stats.Ratio Weight(Stats.Distribution read, Scenes.Scene candidate) =>
        read.Rows.Single(row => ReferenceEquals(row.Model, candidate.Surface)).Probability!.Value;
    private static bool Greater(Stats.Ratio a, Stats.Ratio b) => a.Numerator * b.Denominator > b.Numerator * a.Denominator;
    private static bool Closed(Relation scope) => StructuralConstraint.TryReadScope(scope, out _, out var seam, out var tail) && seam.Equals(tail);
    private static IElement[] Entries(Relation scope)
    { Assert.True(StructuralConstraint.TryReadScope(scope, out var entries, out var seam, out var tail)); Assert.Equal(seam, tail); return entries; }
    private static IElement[] Reachable(IElement root)
    {
        var seen = new HashSet<IElement>(ReferenceEqualityComparer.Instance);
        void Visit(IElement item) { if (!seen.Add(item)) return; if (item is Relation relation) { Visit(relation.Left); Visit(relation.Right); } }
        Visit(root); return seen.ToArray();
    }
}
