using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;
using Gojo.Experiments.QualitativeFrieze.Composition;
using Curve = Gojo.Experiments.QualitativeExecution.QualitativeCurve;
using Study = Gojo.Experiments.QualitativeFrieze.Composition.WeaveContinuationStudy;

namespace QualitativeExecution.Tests;

public sealed class WeaveContinuationStudyTests
{
    private static readonly Lazy<Study.Case[]> Cases = new(Study.Cases);
    private static Study.Case Sample(string id) => Assert.Single(Cases.Value, item => item.Id == id);

    [Fact]
    public void An_aligned_hidden_gap_yields_an_open_request_without_welding_or_claiming_closure()
    {
        var sample = Sample("aligned"); var read = sample.Read;
        Assert.True(read.Complete); Assert.Equal(1, read.CompatibleCandidates);
        var candidate = Assert.Single(read.Candidates);
        Assert.True(candidate.StraightAligned); Assert.True(candidate.OccluderSupported); Assert.True(candidate.Eligible);
        Assert.False(candidate.NativeJoined); Assert.False(read.ActualClosure); Assert.False(read.ConditionalClosure);
        Assert.NotEqual(read.Fragments[0].EndSite, read.Fragments[1].StartSite);
        var witnessBody = Assert.IsType<Relation>(candidate.Surface.Right);
        var proposed = Assert.IsType<Relation>(witnessBody.Right);
        Assert.True(StructuralConstraint.TryReadScope(proposed, out var entries, out var seam, out var tail));
        Assert.NotEqual(seam, tail);
        var bridge = Assert.IsType<Relation>(Assert.Single(entries));
        var openSpan = Assert.IsType<Relation>(bridge.Right);
        Assert.True(StructuralConstraint.TryReadScope(openSpan, out var detail, out seam, out tail));
        Assert.Empty(detail); Assert.NotEqual(seam, tail);
        var again = Study.Read(sample.Frame, sample.Surface);
        Assert.Equal(read.Fragments.Select(f => (f.Index, f.StartSite, f.EndSite)), again.Fragments.Select(f => (f.Index, f.StartSite, f.EndSite)));
        Assert.Equal(read.Fragments.SelectMany(f => f.Controls), again.Fragments.SelectMany(f => f.Controls));
    }

    [Fact]
    public void Missing_occlusion_and_incompatible_direction_remain_different_failed_reads()
    {
        var uncovered = Assert.Single(Sample("unoccluded").Read.Candidates);
        Assert.True(uncovered.StraightAligned); Assert.False(uncovered.OccluderSupported); Assert.False(uncovered.Eligible);
        var incompatible = Assert.Single(Sample("misaligned").Read.Candidates);
        Assert.False(incompatible.TangentCompatible); Assert.True(incompatible.OccluderSupported); Assert.False(incompatible.Eligible);
    }

    [Fact]
    public void Coincident_target_positions_and_tangents_do_not_choose_a_target_identity()
    {
        var read = Sample("ambiguous").Read;
        Assert.Equal(2, read.CompatibleCandidates); Assert.Equal(2, read.Candidates.Length);
        Assert.All(read.Candidates, c => Assert.True(c.Eligible));
        Assert.Equal(read.Candidates[0].To, read.Candidates[1].To);
        Assert.NotEqual(read.Fragments[1].StartSite, read.Fragments[2].StartSite);
        Assert.False(read.ActualClosure); Assert.False(read.ConditionalClosure);
    }

    [Fact]
    public void Forward_tangents_admit_a_curved_hypothesis_without_manufacturing_controls()
    {
        var read = Sample("tangent").Read; var candidate = Assert.Single(read.Candidates);
        Assert.True(candidate.TangentCompatible); Assert.False(candidate.StraightAligned); Assert.True(candidate.Eligible);
        Assert.All(read.Fragments, f => Assert.Equal(2, f.Controls.Length));
        Assert.False(candidate.NativeJoined);
    }

    [Fact]
    public void Conditional_closure_and_an_authored_closed_route_have_different_evidence()
    {
        var proposed = Sample("conditional-closure").Read;
        Assert.True(proposed.ConditionalClosure); Assert.False(proposed.ActualClosure);
        Assert.True(Assert.Single(proposed.Candidates).WouldClose);
        var authored = Sample("authored-closure").Read;
        Assert.True(authored.ActualClosure); Assert.False(authored.ConditionalClosure); Assert.Empty(authored.Candidates);
        Assert.Equal(proposed.Fragments.Length + 1, authored.Fragments.Length);
    }

    [Fact]
    public void Coordinate_closure_without_native_endpoint_identity_stays_open()
    {
        var read = Sample("coincident-open").Read;
        Assert.Equal(read.Fragments[0].Controls[0], read.Fragments[^1].Controls[^1]);
        Assert.NotEqual(read.Fragments[0].StartSite, read.Fragments[^1].EndSite);
        Assert.False(read.ActualClosure); Assert.False(read.ConditionalClosure);
        Assert.False(Assert.Single(read.Candidates).NativeJoined); Assert.Equal(0, read.CompatibleCandidates);
    }

    [Fact]
    public void A_port_to_an_unexposed_fragment_and_a_foreign_frame_cannot_enter_the_read()
    {
        var f = new Curve.Frame(new QualitativeMotionFrame());
        var a = f.Line(new Placeholder(), new Placeholder(), f.Word(0, 0), f.Word(1, 0));
        var hidden = f.Line(new Placeholder(), new Placeholder(), f.Word(2, 0), f.Word(3, 0));
        var query = Study.Request(f, [a], Study.Expose(f, a, false), [Study.Expose(f, hidden, true)], []);
        Assert.False(Study.Read(f, query).Complete);
        var visible = Study.Request(f, [a, hidden], Study.Expose(f, a, false), [Study.Expose(f, hidden, true)], []);
        Assert.True(Study.Read(f, visible).Complete);
        Assert.False(Study.Read(new Curve.Frame(new QualitativeMotionFrame()), visible).Complete);
        Assert.False(Study.Read(f, Study.Request(f, [a, hidden], Study.Expose(f, a, false),
            [Study.Expose(f, hidden, true)], [a])).Complete);
    }

    [Fact]
    public void A_forged_tangent_or_return_route_does_not_gain_evidence_from_matching_coordinates()
    {
        var f = new Curve.Frame(new QualitativeMotionFrame());
        var a = f.Line(new Placeholder(), new Placeholder(), f.Word(-3, 0), f.Word(-1, 0));
        var b = f.Line(new Placeholder(), new Placeholder(), f.Word(1, 0), f.Word(3, 0));
        var port = Study.Expose(f, a, false);
        var end = Assert.IsType<Relation>(port.Right); var position = Assert.IsType<Relation>(end.Right);
        var forged = new Relation(port.Left, new Relation(end.Left, new Relation(position.Left, f.Word(0, 1))));
        Assert.False(Study.Read(f, Study.Request(f, [a, b], forged, [Study.Expose(f, b, true)], [])).Complete);
        var foreign = f.Line(new Placeholder(), new Placeholder(), f.Word(3, 0), f.Word(-3, 0));
        Assert.False(Study.Read(f, Study.Request(f, [a, b], port, [Study.Expose(f, b, true)], [], [b, foreign, a])).Complete);
    }

    [Fact]
    public void An_unrelated_closed_route_or_inconsistent_shared_site_is_not_a_completion_witness()
    {
        var f = new Curve.Frame(new QualitativeMotionFrame());
        var shared = new Placeholder();
        var source = f.Line(new Placeholder(), shared, f.Word(-3, 0), f.Word(-1, 0));
        var inconsistent = f.Line(shared, new Placeholder(), f.Word(1, 0), f.Word(3, 0));
        Assert.False(Study.Read(f, Study.Request(f, [source, inconsistent], Study.Expose(f, source, false),
            [Study.Expose(f, inconsistent, true)], [])).Complete);
        var a = new Placeholder(); var b = new Placeholder();
        var outward = f.Line(a, b, f.Word(8, 0), f.Word(9, 0));
        var homeward = f.Line(b, a, f.Word(9, 0), f.Word(8, 0));
        Assert.False(Study.Read(f, Study.Request(f, [source, outward, homeward], Study.Expose(f, source, false),
            [], [], [outward, homeward])).Complete);
    }
}
