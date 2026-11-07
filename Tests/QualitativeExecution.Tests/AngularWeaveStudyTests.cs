using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;
using Gojo.Experiments.QualitativeFrieze;
using Gojo.Experiments.QualitativeFrieze.Composition;
using Curve = Gojo.Experiments.QualitativeExecution.QualitativeCurve;
using Study = Gojo.Experiments.QualitativeFrieze.Composition.AngularWeaveStudy;

namespace QualitativeExecution.Tests;

public sealed class AngularWeaveStudyTests
{
    [Fact]
    public void Four_continuous_routes_cross_without_joining_or_closing()
    {
        var f = Study.Build(); var read = Study.Read(f);
        Assert.Equal(42, read.Segments.Length); Assert.Equal(24, read.Crossings.Length);
        Assert.Equal(4, read.Summary.MaterialComponents); Assert.Equal(8, read.Summary.MaterialEnds);
        Assert.Equal(0, read.Summary.ClosedStrands); Assert.Equal(0, read.Summary.ProjectedEndpointContacts);
        Assert.All(read.Strands, s => { Assert.True(s.Continuous); Assert.True(s.RouteContinuous); Assert.False(s.Closed); Assert.False(s.RouteClosed); });
        Assert.All(read.Crossings, c => { Assert.True(c.Projected); Assert.True(c.Material); Assert.True(c.OrderKnown); Assert.True(c.DepthAgrees); });
        Assert.Equal(44, read.Summary.Alternations); Assert.Equal(44, read.Summary.AdjacentVisits);
        Assert.All(read.Strands, s => Assert.Equal(s.PredictionChecks, s.PredictionMatches));
    }

    [Fact]
    public void Reusable_words_phase_laws_and_generated_sections_remain_connected()
    {
        var f = Study.Build(); var fields = E(f.Surface); var generation = E((Relation)fields[5]).Cast<Relation>().ToArray();
        Assert.Equal(12, generation.Length);
        Assert.Equal(2, generation.Select(g => ((Relation)g.Left).Right).Distinct(ReferenceEqualityComparer.Instance).Count());
        var actualSections = new List<IElement>();
        foreach (var g in generation)
        {
            var source = (Relation)g.Left; var occurrence = (Relation)source.Left; var transition = (Relation)occurrence.Right;
            var targetAndRead = (Relation)transition.Right; var lawRead = (Relation)targetAndRead.Right;
            Assert.True(FiniteLaw.TryReadResult(lawRead, out var result, out var pending, out _));
            Assert.Same(targetAndRead.Left, Assert.Single(E(result))); Assert.Empty(E(pending));
            Assert.NotSame(transition.Left, targetAndRead.Left);
            var generated = (Relation)g.Right;
            Assert.NotEmpty(E((Relation)generated.Left)); actualSections.AddRange(E((Relation)generated.Right));
        }
        var routeSections = E((Relation)fields[3]).Cast<Relation>().SelectMany(s => E((Relation)E(s)[0])).ToArray();
        Assert.Equal(42, actualSections.Count); Assert.Equal(routeSections, actualSections);
    }

    [Fact]
    public void Crossing_depth_axes_are_local_and_samples_reference_actual_sections()
    {
        var f = Study.Build(); var rows = E((Relation)E(f.Surface)[4]).Cast<Relation>().ToArray();
        var axes = new List<IElement>();
        foreach (var row in rows)
        {
            var body = (Relation)row.Right; var order = (Relation)body.Left; var depth = (Relation)body.Right; var axis = (Relation)depth.Left;
            axes.Add(axis); Assert.NotSame(axis.Left, axis.Right); Assert.Same(row.Left, order.Left);
            var pair = (Relation)order.Right; var samples = E((Relation)depth.Right).Cast<Relation>().ToArray();
            Assert.Equal(2, samples.Length);
            Assert.Contains(samples, s => ReferenceEquals(s.Left, pair.Left) && ReferenceEquals(s.Right, axis.Right));
            Assert.Contains(samples, s => ReferenceEquals(s.Left, pair.Right) && ReferenceEquals(s.Right, axis.Left));
        }
        Assert.Equal(24, axes.Distinct(ReferenceEqualityComparer.Instance).Count());
    }

    [Fact]
    public void One_bay_exchanges_strands_and_two_bays_return_each_named_strand()
    {
        var read = Study.Read(Study.Build()); var one = Study.PeriodRead(read, 1); var two = Study.PeriodRead(read, 2);
        Assert.Equal(new[] { 1, 0, 3, 2 }, one.StrandMap); Assert.Equal(new[] { 0, 1, 2, 3 }, two.StrandMap);
        foreach (var p in new[] { one, two })
        {
            Assert.True(p.SegmentsChecked > 0); Assert.Equal(p.SegmentsChecked, p.SegmentsMatched);
            Assert.True(p.CrossingsChecked > 0); Assert.Equal(p.CrossingsChecked, p.CrossingsMatched);
            Assert.Equal(p.CrossingsChecked, p.OrdersMatched); Assert.False(p.ClosesStrands);
        }
    }

    [Fact]
    public void A_single_order_edit_keeps_geometry_and_breaks_encounter_alternation()
    {
        var before = Study.Read(Study.Build()); var after = Study.Read(Study.Build(flipCrossing: 3));
        Assert.Equal(before.Segments, after.Segments);
        Assert.Single(before.Crossings.Zip(after.Crossings).Where(p => p.First.FrontSegment != p.Second.FrontSegment));
        Assert.Equal(before.Summary.MaterialComponents, after.Summary.MaterialComponents);
        Assert.True(after.Summary.Alternations < after.Summary.AdjacentVisits);
        Assert.Equal(2, after.Strands.Sum(s => s.PredictionChecks - s.PredictionMatches));
        Assert.All(after.Crossings, c => Assert.True(c.DepthAgrees));
    }

    [Fact]
    public void Global_depth_exchange_preserves_alternation_but_reverses_every_local_read()
    {
        var before = Study.Read(Study.Build()); var after = Study.Read(Study.Build(invertDepth: true));
        Assert.Equal(before.Segments, after.Segments);
        foreach (var pair in before.Crossings.Zip(after.Crossings))
        {
            Assert.Equal(pair.First.FrontSegment, pair.Second.BackSegment);
            Assert.Equal(-pair.First.FirstZ, pair.Second.FirstZ); Assert.Equal(-pair.First.SecondZ, pair.Second.SecondZ);
        }
        Assert.Equal(after.Summary.AdjacentVisits, after.Summary.Alternations);
    }

    [Fact]
    public void Planar_reflection_preserves_local_orders_without_inverting_depth()
    {
        var before = Study.Read(Study.Build()); var after = Study.Read(Study.Build(mirror: true));
        foreach (var pair in before.Segments.Zip(after.Segments))
        {
            Assert.Equal(new Study.Pt(pair.First.From.X, 12 - pair.First.From.Y), pair.Second.From);
            Assert.Equal(new Study.Pt(pair.First.To.X, 12 - pair.First.To.Y), pair.Second.To);
            Assert.Equal(pair.First.FromSite, pair.Second.FromSite); Assert.Equal(pair.First.ToSite, pair.Second.ToSite);
        }
        foreach (var cross in before.Crossings)
        {
            var target = Assert.Single(after.Crossings, c => c.FirstSegment == cross.FirstSegment && c.SecondSegment == cross.SecondSegment);
            Assert.Equal(new Study.Pt(cross.At.X, 12 - cross.At.Y), target.At);
            Assert.Equal(cross.FrontSegment, target.FrontSegment); Assert.Equal(cross.FirstZ, target.FirstZ);
        }
    }

    [Fact]
    public void Actual_absent_material_splits_a_strand_while_occlusion_keeps_it_whole()
    {
        var before = Study.Read(Study.Build()); var after = Study.Read(Study.Build(missingSegment: 15));
        Assert.Equal(4, before.Summary.MaterialComponents); Assert.Equal(5, after.Summary.MaterialComponents);
        Assert.Equal(10, after.Summary.MaterialEnds); Assert.False(after.Segments[15].Present);
        Assert.All(after.Strands, s => Assert.True(s.RouteContinuous));
        Assert.Single(after.Strands.Where(s => !s.Continuous));
        Assert.Equal(before.Segments.Select(s => (s.From, s.To, s.FromSite, s.ToSite)), after.Segments.Select(s => (s.From, s.To, s.FromSite, s.ToSite)));
        Assert.Equal(2, after.Crossings.Count(c => !c.Material));
        Assert.All(after.Strands.SelectMany(s => s.Visits).Where(v => !after.Crossings[v.Crossing].Material), v => { Assert.Null(v.Over); Assert.Null(v.Z); });
    }

    [Fact]
    public void A_missing_section_between_crossings_can_preserve_every_rhythm_check()
    {
        var read = Study.Read(Study.Build(missingSegment: 14));
        Assert.Equal(5, read.Summary.MaterialComponents); Assert.Equal(10, read.Summary.MaterialEnds);
        Assert.Equal(24, read.Summary.KnownOrders); Assert.Equal(44, read.Summary.Alternations);
        Assert.Equal(44, read.Summary.AdjacentVisits); Assert.False(read.Strands[2].Continuous);
    }

    [Fact]
    public void Unknown_order_is_not_zero_and_adjacent_visits_do_not_skip_across_it()
    {
        var before = Study.Read(Study.Build()); var after = Study.Read(Study.Build(unknownCrossing: 3));
        Assert.Equal(before.Segments, after.Segments); Assert.Equal(23, after.Summary.KnownOrders);
        var cross = after.Crossings[3]; Assert.False(cross.OrderKnown); Assert.Null(cross.FirstZ); Assert.Null(cross.SecondZ);
        foreach (var strand in after.Strands)
        {
            var pairs = strand.Visits.Zip(strand.Visits.Skip(1)).Count(p => p.First.Over.HasValue && p.Second.Over.HasValue);
            Assert.Equal(pairs, strand.AdjacentVisits);
        }
        Assert.Equal(41, after.Summary.AdjacentVisits); Assert.Equal(41, after.Summary.Alternations);
    }

    [Fact]
    public void Shifted_inner_course_recomputes_crossings_and_can_keep_the_same_rhythm()
    {
        var before = Study.Read(Study.Build()); var after = Study.Read(Study.Build(shiftInner: 2));
        Assert.Contains(before.Crossings.Zip(after.Crossings), p => p.First.At != p.Second.At);
        Assert.Equal(24, after.Summary.KnownOrders); Assert.Equal(44, after.Summary.Alternations);
        Assert.All(before.Segments.Zip(after.Segments), p =>
        {
            var dx = p.First.Strand == 2 ? 2 : 0;
            Assert.Equal(new Study.Pt(p.First.From.X + dx, p.First.From.Y), p.Second.From);
        });
    }

    [Fact]
    public void Foreign_order_cannot_supply_a_current_crossing_order()
    {
        var f = Study.Build(); var fields = E(f.Surface); var rows = E((Relation)fields[4]);
        var current = (Relation)rows[0]; var other = (Relation)rows[1];
        var currentBody = (Relation)current.Right; var otherBody = (Relation)other.Right;
        rows[0] = new Relation(current.Left, new Relation(otherBody.Left, currentBody.Right));
        fields[4] = S(rows); var read = Study.Read(f with { Surface = S(fields) });
        Assert.False(read.Crossings[0].OrderKnown); Assert.Null(read.Crossings[0].FrontSegment);
        Assert.Equal(23, read.Summary.KnownOrders);
    }

    [Fact]
    public void A_collapsed_depth_axis_cannot_claim_two_depth_levels()
    {
        var f = Study.Build(); var fields = E(f.Surface); var rows = E((Relation)fields[4]);
        var current = (Relation)rows[0]; var body = (Relation)current.Right; var depth = (Relation)body.Right; var axis = (Relation)depth.Left;
        rows[0] = new Relation(current.Left, new Relation(body.Left, new Relation(new Relation(axis.Left, axis.Left), depth.Right)));
        fields[4] = S(rows); var read = Study.Read(f with { Surface = S(fields) });
        Assert.True(read.Crossings[0].OrderKnown); Assert.False(read.Crossings[0].DepthAgrees);
        Assert.Null(read.Crossings[0].FirstZ); Assert.Null(read.Crossings[0].SecondZ);
    }

    [Fact]
    public void Shared_site_with_contradictory_positions_does_not_claim_continuous_geometry()
    {
        var f = SmallPath(shared: true, contradictory: true); var read = Study.Read(f);
        Assert.False(read.Strands[0].Continuous); Assert.False(read.Strands[0].RouteContinuous);
        Assert.Equal(1, read.Summary.MaterialComponents);
    }

    [Fact]
    public void Coordinate_coincidence_without_site_identity_does_not_join_sections()
    {
        var f = SmallPath(shared: false, contradictory: false); var read = Study.Read(f);
        Assert.False(read.Strands[0].Continuous); Assert.Equal(2, read.Summary.MaterialComponents);
        Assert.Equal(1, read.Summary.ProjectedEndpointContacts);
    }

    private static Study.Fixture SmallPath(bool shared, bool contradictory)
    {
        var frame = new Curve.Frame(new QualitativeMotionFrame()); var join = new Placeholder();
        var a = frame.Line(new Placeholder(), join, frame.Word(0, 0), frame.Word(2, 0));
        var b = frame.Line(shared ? join : new Placeholder(), new Placeholder(), frame.Word(contradictory ? 3 : 2, 0), frame.Word(4, 0));
        var phases = new Relation(new Placeholder(), new Placeholder()); var law = QualitativeWeave.CreateExchangeLaw(phases);
        return new(frame, S(frame.Surface, frame.Word(4, 0), new Relation(phases, law), S(S(S(a, b), S(a, b), phases.Left, S())), S(), S()));
    }
    private static Relation S(params IElement[] entries) => StructuralConstraint.Closed(entries);
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
}
