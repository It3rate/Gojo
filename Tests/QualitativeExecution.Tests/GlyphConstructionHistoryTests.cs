using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;
using Gojo.Experiments.QualitativeFrieze.Composition;

namespace QualitativeExecution.Tests;

public sealed class GlyphConstructionHistoryTests
{
    private static GlyphConstructionCorpus.Fixture Glyph(string glyph) =>
        GlyphConstructionCorpus.Create().Single(f => f.Glyph == glyph && f.VariantId == "baseline");

    [Fact]
    public void A_continuous_L_turn_and_a_later_F_endpoint_attachment_have_different_histories()
    {
        var l = GlyphConstructionHistory.Read(Glyph("L").Surface);
        Assert.Contains(l.Strokes[0].Events, e => e.Kind == "continuous-turn");
        Assert.DoesNotContain(l.Strokes.SelectMany(s => s.Events), e => e.Kind == "endpoint-attachment");
        var f = GlyphConstructionHistory.Read(Glyph("F").Surface);
        Assert.Equal("return-endpoint", f.Strokes[1].StartKind);
        Assert.Contains(f.Strokes[1].Events, e => e.Kind == "endpoint-attachment" && e.BeforeDegree == 1);
        Assert.Equal(new[] { 0 }, f.Strokes[1].StartSourceStrokes);
    }

    [Fact]
    public void F_middle_bar_leaves_an_earlier_interior_landmark_as_a_secondary_route()
    {
        var f = GlyphConstructionHistory.Read(Glyph("F").Surface);
        Assert.True(f.Complete);
        Assert.Equal("return-interior", f.Strokes[2].StartKind);
        Assert.Equal("free-stop", f.Strokes[2].EndKind);
        Assert.Contains(f.Strokes[2].Events, e => e.Kind == "outward-branch" && e.BeforeDegree == 2);
        Assert.Equal(new[] { 0 }, f.Strokes[2].StartSourceStrokes);
    }

    [Fact]
    public void Reversing_that_bar_changes_departure_into_arrival_and_stop_without_changing_the_ink()
    {
        var source = Glyph("F"); var strokes = E(source.History).Cast<Relation>().ToArray();
        var visit = Assert.IsType<Relation>(Assert.Single(E(strokes[^1])));
        strokes[^1] = S(GlyphConstructionCorpus.Visit(source.Frame, (Relation)visit.Left, reverse: true));
        var read = GlyphConstructionHistory.Read(new Relation(source.Surface.Left, S(strokes)));
        Assert.True(read.Complete);
        Assert.Equal("new-start", read.Strokes[^1].StartKind);
        Assert.Equal("stop-at-interior", read.Strokes[^1].EndKind);
        Assert.Contains(read.Strokes[^1].Events, e => e.Kind == "incoming-stop" && e.BeforeDegree == 2);
        Assert.DoesNotContain(read.Strokes[^1].Events, e => e.Kind == "outward-branch");
    }

    [Fact]
    public void H_bridge_connects_two_landmarks_that_were_both_available_before_the_cross_stroke()
    {
        var h = GlyphConstructionHistory.Read(Glyph("H").Surface);
        Assert.Equal("return-interior", h.Strokes[2].StartKind);
        Assert.Equal("stop-at-interior", h.Strokes[2].EndKind);
        Assert.Equal(new[] { 0 }, h.Strokes[2].StartSourceStrokes);
        Assert.Equal(new[] { 1 }, h.Strokes[2].EndSourceStrokes);
        Assert.Contains(h.Strokes[2].Events, e => e.Kind == "landmark-to-landmark-join");
        Assert.Equal("new-start", h.Strokes[1].StartKind); // Geometry alone does not supply an offset instruction.
    }

    [Fact]
    public void A_supplied_H_offset_connects_the_new_stem_to_an_earlier_stem_without_welding_them()
    {
        var h = Glyph("H"); var strokes = E(h.History).Cast<Relation>().ToArray();
        var sourceStart = Start(strokes[0]); var targetStart = Start(strokes[1]);
        var plan = GlyphConstructionHistory.Placement(strokes[1], strokes[0], sourceStart, targetStart, h.Frame.Word(8, 0));
        var read = GlyphConstructionHistory.Read(h.Surface, S(plan));
        Assert.True(read.Complete);
        Assert.Equal("offset-start", read.Strokes[1].StartKind);
        Assert.Equal(0, read.Strokes[1].PlacementSourceStroke);
        Assert.Equal(new long[] { 8, 0 }, read.Strokes[1].PlacementOffset);
        Assert.True(read.Strokes[1].ParallelToSource);
        Assert.NotEqual(read.Strokes[0].StartSite, read.Strokes[1].StartSite);
    }

    [Fact]
    public void Placement_can_reference_an_earlier_end_landmark_not_only_a_stroke_start()
    {
        var h = Glyph("H"); var strokes = E(h.History).Cast<Relation>().ToArray();
        var sourceEnd = End(strokes[0]); var targetStart = Start(strokes[1]);
        var a = Position(h, sourceEnd); var b = Position(h, targetStart);
        var plan = GlyphConstructionHistory.Placement(strokes[1], strokes[0], sourceEnd, targetStart,
            h.Frame.Word((int)(b.X - a.X), (int)(b.Y - a.Y)));
        var read = GlyphConstructionHistory.Read(h.Surface, S(plan));
        Assert.True(read.Complete);
        Assert.Equal("offset-start", read.Strokes[1].StartKind);
        Assert.Equal(read.Strokes[0].EndSite, read.Strokes[1].PlacementSourceSite);
    }

    [Fact]
    public void Placement_cannot_reference_a_stroke_that_has_not_happened_yet()
    {
        var h = Glyph("H"); var strokes = E(h.History).Cast<Relation>().ToArray();
        var plan = GlyphConstructionHistory.Placement(strokes[0], strokes[1], Start(strokes[1]), Start(strokes[0]), h.Frame.Word(-8, 0));
        var read = GlyphConstructionHistory.Read(h.Surface, S(plan));
        Assert.False(read.Complete);
        Assert.Equal("unresolved-placement", read.Strokes[0].StartKind);
        Assert.Null(read.Strokes[0].PlacementSourceStroke);
    }

    [Fact]
    public void Placement_requires_the_actual_earlier_site_and_a_matching_offset()
    {
        var h = Glyph("H"); var strokes = E(h.History).Cast<Relation>().ToArray();
        var foreign = GlyphConstructionHistory.Placement(strokes[1], strokes[0], new Placeholder(), Start(strokes[1]), h.Frame.Word(8, 0));
        var wrongOffset = GlyphConstructionHistory.Placement(strokes[1], strokes[0], Start(strokes[0]), Start(strokes[1]), h.Frame.Word(9, 0));
        Assert.False(GlyphConstructionHistory.Read(h.Surface, S(foreign)).Complete);
        Assert.False(GlyphConstructionHistory.Read(h.Surface, S(wrongOffset)).Complete);
    }

    [Fact]
    public void Final_T_geometry_does_not_leak_into_the_earlier_stem_drawing()
    {
        var f = Glyph("F"); var read = GlyphConstructionHistory.Read(f.Surface);
        Assert.Contains(f.Sites, s => s.Joint == "T-like");
        Assert.All(read.Strokes[0].Events, e => Assert.Equal(0, e.BeforeDegree));
        Assert.DoesNotContain(read.Strokes[0].Events, e => e.Kind == "outward-branch" || e.Kind == "meet-landmark-and-continue");
    }

    [Fact]
    public void B_bowls_can_meet_an_earlier_midline_and_continue_instead_of_stopping()
    {
        var b = GlyphConstructionHistory.Read(Glyph("B").Surface);
        Assert.Contains(b.Strokes[1].Events, e => e.Kind == "meet-landmark-and-continue" && e.BeforeDegree == 2);
        var meeting = b.Strokes[1].Events.Single(e => e.Kind == "meet-landmark-and-continue");
        Assert.DoesNotContain(b.Strokes[1].Events, e => e.Site == meeting.Site && e.Kind == "incoming-stop");
    }

    [Fact]
    public void Six_stops_at_a_landmark_created_earlier_in_the_same_stroke()
    {
        var six = GlyphConstructionHistory.Read(Glyph("6").Surface);
        Assert.True(six.Complete);
        Assert.Equal("stop-at-earlier-landmark", six.Strokes[0].EndKind);
        Assert.Equal(new[] { 0 }, six.Strokes[0].EndSourceStrokes);
        var stop = Assert.Single(six.Strokes[0].Events, e => e.Kind == "earlier-landmark-stop");
        Assert.Equal(2, stop.BeforeDegree); // The just-arriving third incidence is not prior evidence.
        Assert.DoesNotContain(six.Strokes[0].Events, e => e.Kind == "landmark-to-landmark-join");
    }

    [Fact]
    public void Eight_revisits_its_waist_during_one_stroke_but_does_not_borrow_that_evidence_early()
    {
        var eight = GlyphConstructionHistory.Read(Glyph("8").Surface);
        Assert.True(eight.Complete);
        var again = Assert.Single(eight.Strokes[0].Events, e => e.Kind == "revisit-and-continue");
        Assert.Equal(new[] { 0 }, again.SourceStrokes); Assert.Equal(2, again.BeforeDegree);
        var first = eight.Strokes[0].Events.First(e => e.Site == again.Site);
        Assert.Equal(0, first.BeforeDegree); Assert.Empty(first.SourceStrokes);
        Assert.Equal("closed-return", eight.Strokes[0].EndKind);
    }

    [Fact]
    public void A_simple_O_has_a_return_stop_but_does_not_become_a_join_between_earlier_strokes()
    {
        var o = GlyphConstructionHistory.Read(Glyph("O").Surface);
        Assert.Equal("closed-return", o.Strokes[0].EndKind);
        Assert.Contains(o.Strokes[0].Events, e => e.Kind == "closure-stop");
        Assert.DoesNotContain(o.Strokes[0].Events, e => e.Kind == "landmark-to-landmark-join");
        Assert.All(o.Strokes[0].Events.Where(e => e.Kind == "continuous-passage"), e => Assert.Equal(0, e.BeforeDegree));
    }

    [Fact]
    public void Repeated_stroke_holders_need_an_occurrence_correspondence_instead_of_an_ambiguous_placement()
    {
        var l = Glyph("L"); var stroke = Assert.IsType<Relation>(Assert.Single(E(l.History)));
        var read = GlyphConstructionHistory.Read(new Relation(l.Surface.Left, S(stroke, stroke)));
        Assert.False(read.Complete); Assert.Single(read.Strokes);
    }

    [Fact]
    public void Distinct_coincident_crossing_sites_are_not_previous_landmarks()
    {
        var x = GlyphConstructionHistory.Read(Glyph("X").Surface);
        Assert.True(x.Complete);
        Assert.All(x.Strokes[1].Events, e => Assert.Equal(0, e.BeforeDegree));
        Assert.DoesNotContain(x.Strokes[1].Events, e => e.Kind == "meet-landmark-and-continue");
    }

    [Fact]
    public void A_disconnected_visit_sequence_cannot_be_read_as_continuous_construction()
    {
        var f = Glyph("F"); var strokes = E(f.History).Cast<Relation>().ToArray();
        var falseStroke = S(E(strokes[0])[0], E(strokes[1])[0]);
        var read = GlyphConstructionHistory.Read(new Relation(f.Surface.Left, S(falseStroke)));
        Assert.False(read.Complete); Assert.Empty(read.Strokes);
    }

    [Fact]
    public void Native_construction_read_recomputes_after_roundtrip_without_a_glyph_name()
    {
        var f = GlyphConstructionHistory.Read(Glyph("F").Surface);
        var archive = Assert.IsType<Relation>(Assert.Single(GojoNumericFormat.Decode(GojoNumericFormat.Encode(f.Surface))));
        var input = Assert.IsType<Relation>(archive.Left);
        var restored = GlyphConstructionHistory.Read(Assert.IsType<Relation>(input.Left), Assert.IsType<Relation>(input.Right));
        Assert.True(restored.Complete);
        Assert.Equal(f.Strokes.Select(s => (s.StartKind, s.EndKind)), restored.Strokes.Select(s => (s.StartKind, s.EndKind)));
        Assert.Equal(f.Sites.Select(s => string.Join("/", s.Kinds)), restored.Sites.Select(s => string.Join("/", s.Kinds)));
    }

    private static IElement Start(Relation stroke) => ((Relation)((Relation)((Relation)E(stroke)[0]).Right).Left).Left;
    private static IElement End(Relation stroke) => ((Relation)((Relation)((Relation)E(stroke)[^1]).Right).Left).Right;
    private static QualitativeCurve.Vector Position(GlyphConstructionCorpus.Fixture fixture, IElement site)
    {
        foreach (var section in E(fixture.Sections).Cast<Relation>())
        {
            var read = fixture.Frame.Read(section);
            if (ReferenceEquals(read.Start, site)) return read.Controls[0];
            if (ReferenceEquals(read.End, site)) return read.Controls[^1];
        }
        throw new InvalidOperationException("Expected an actual site.");
    }
    private static Relation S(params IElement[] entries) => StructuralConstraint.Closed(entries);
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
}
