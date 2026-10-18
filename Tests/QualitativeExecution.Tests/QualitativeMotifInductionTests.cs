using Gojo.Core;
using Gojo.Experiments.QualitativeExecution;
using Gojo.Experiments.QualitativeFrieze;
using Gojo.Patterns;

namespace QualitativeExecution.Tests;

public sealed class QualitativeMotifInductionTests
{
    [Fact]
    public void Two_unlabelled_squares_induce_a_reusable_cycle_with_actual_bijections()
    {
        var fixture = new Fixture(); fixture.Cycle(1, 4, 2, 8); fixture.Cycle(1, 4, 2, 8);
        var read = fixture.Read();
        var family = Assert.Single(read.Families, family => family.Kind == "cycle");
        Assert.Equal(2, family.Occurrences.Count); Assert.Equal(4, family.SegmentCount);
        Assert.True(read.TryReadFamily(family.Read, out var decoded)); Assert.Same(family, decoded);
        Assert.True(read.TryReadPattern(family.Definition, out var kind, out var signature));
        Assert.Equal(family.Kind, kind); Assert.Equal(family.Signature, signature);
        foreach (var occurrence in family.Occurrences)
        {
            Assert.True(read.TryReadOccurrence(family.Read, occurrence.Read, out var actual)); Assert.Same(actual, occurrence);
            Assert.Equal(4, Entries(occurrence.SiteBindings).Length);
            Assert.All(Entries(occurrence.SegmentBindings).Cast<Relation>(), binding => Assert.Contains(fixture.Edges, edge => ReferenceEquals(edge, binding.Right)));
            Assert.Equal(4, Entries(occurrence.Route).Length);
            Assert.Same(family.Definition, ((Relation)occurrence.Read.Left).Left);
        }
    }

    [Fact]
    public void One_cycle_is_not_counted_again_for_each_start_or_traversal_direction()
    {
        var fixture = new Fixture(); fixture.Cycle(1, 4, 2, 8);
        var read = fixture.Read(); var family = Assert.Single(read.Families, family => family.Kind == "cycle");
        Assert.Single(family.Occurrences);
    }

    [Fact]
    public void Chorded_quadrilateral_retains_triangle_and_four_edge_interpretations()
    {
        var fixture = new Fixture(); var square = fixture.Cycle(1, 4, 2, 8);
        fixture.Edge(square[0], square[2], 5);
        var read = fixture.Read(); var cycles = read.Families.Where(family => family.Kind == "cycle").ToArray();
        Assert.Equal(2, cycles.Where(family => family.SegmentCount == 3).Sum(family => family.Occurrences.Count));
        Assert.Single(cycles, family => family.SegmentCount == 4);
        Assert.True(read.IsComplete);
    }

    [Fact]
    public void Heading_participation_distinguishes_square_and_slanted_cycles_without_measuring_angles()
    {
        var fixture = new Fixture(); fixture.Cycle(1, 4, 2, 8); fixture.Cycle(1, 5, 2, 10);
        var read = fixture.Read(); var cycles = read.Families.Where(family => family.Kind == "cycle").ToArray();
        Assert.Equal(2, cycles.Length); Assert.NotEqual(cycles[0].Signature, cycles[1].Signature);
        Assert.All(cycles, family => Assert.Single(family.Occurrences));
    }

    [Fact]
    public void Six_and_eight_edge_cycles_are_discovered_without_authored_faces()
    {
        var fixture = new Fixture(); fixture.Cycle(1, 5, 4, 2, 10, 8); fixture.Cycle(1, 5, 4, 6, 2, 10, 8, 9);
        var read = fixture.Read();
        Assert.Contains(read.Families, family => family.Kind == "cycle" && family.SegmentCount == 6);
        Assert.Contains(read.Families, family => family.Kind == "cycle" && family.SegmentCount == 8);
        var lengths = (Relation)((Relation)read.Budget.Right).Right;
        Assert.Equal(8, Entries((Relation)lengths.Left).Length);
    }

    [Fact]
    public void A_selected_cycle_length_bound_is_visible_and_does_not_invent_larger_matches()
    {
        var fixture = new Fixture(); fixture.Cycle(1, 5, 4, 2, 10, 8);
        var read = fixture.Read(maxCycleEdges: 4);
        Assert.DoesNotContain(read.Families, family => family.Kind == "cycle");
        Assert.True(read.IsComplete);
    }

    [Fact]
    public void Zero_proposal_capacity_retains_unsearched_native_scope_instead_of_claiming_absence()
    {
        var fixture = new Fixture(); fixture.Cycle(1, 4, 2, 8);
        var read = fixture.Read(proposalCapacity: 0);
        Assert.Empty(read.Families); Assert.False(read.IsComplete); Assert.NotEmpty(Entries(read.Unsearched));
        Assert.Empty(Entries(read.Consumed)); Assert.Equal(4, Entries(read.Remainder).Length);
    }

    [Fact]
    public void Retention_capacity_has_distinct_explicit_unsearched_witnesses()
    {
        var fixture = new Fixture(); fixture.Cycle(1, 4, 2, 8);
        var read = fixture.Read(retainedCapacity: 1);
        Assert.Single(read.Families.SelectMany(family => family.Occurrences));
        Assert.NotEmpty(Entries(read.Unsearched)); Assert.False(read.IsComplete);
    }

    [Fact]
    public void Role_identity_not_equal_heading_creates_a_join()
    {
        var fixture = new Fixture();
        fixture.Edge(fixture.Site(), fixture.Site(), 1); fixture.Edge(fixture.Site(), fixture.Site(), 4);
        var read = fixture.Read();
        Assert.DoesNotContain(read.Families, family => family.Kind is "walk" or "cycle");
        Assert.All(read.Families, family => Assert.Equal(1, family.SegmentCount));
    }

    [Fact]
    public void Short_walks_are_canonical_under_reversed_use_but_preserve_actual_order()
    {
        var fixture = new Fixture(); var a = fixture.Site(); var b = fixture.Site(); var c = fixture.Site();
        fixture.Edge(a, b, 1); fixture.Edge(b, c, 4);
        var read = fixture.Read(); var walk = Assert.Single(read.Families, family => family.Kind == "walk");
        Assert.Single(walk.Occurrences); Assert.Equal(2, walk.SegmentCount); Assert.Equal(3, walk.SiteCount);
        var route = Entries(walk.Occurrences[0].Route).Cast<Relation>().ToArray();
        Assert.Same(((Relation)route[0].Right).Right, ((Relation)route[1].Right).Left);
    }

    [Fact]
    public void Equal_branch_readings_repeat_without_merging_actual_centres()
    {
        var fixture = new Fixture();
        for (var i = 0; i < 2; i++)
        {
            var centre = fixture.Site(); foreach (var heading in new[] { 1, 4, 8 }) fixture.Edge(centre, fixture.Site(), heading);
        }
        var read = fixture.Read(); var family = Assert.Single(read.Families, family => family.Kind == "junction" && family.SegmentCount == 3);
        Assert.Equal(2, family.Occurrences.Count);
        var centres = family.Occurrences.Select(occurrence => ((Relation)Entries(occurrence.SiteBindings)[0]).Right).ToArray();
        Assert.NotSame(centres[0], centres[1]);
    }

    [Fact]
    public void Repeated_overlapping_motifs_do_not_duplicate_coverage()
    {
        var fixture = new Fixture(); var a = fixture.Site(); var b = fixture.Site(); var c = fixture.Site();
        var d = fixture.Site(); var e = fixture.Site(); var f = fixture.Site();
        fixture.Edge(a, b, 1); fixture.Edge(b, c, 4); fixture.Edge(c, d, 2); fixture.Edge(d, a, 8);
        fixture.Edge(b, e, 1); fixture.Edge(e, f, 4); fixture.Edge(f, c, 2);
        var read = fixture.Read();
        var squares = read.Families.Where(family => family.Kind == "cycle" && family.SegmentCount == 4).ToArray();
        Assert.Equal(2, Assert.Single(squares).Occurrences.Count);
        Assert.Equal(7, Entries(read.Covered).Length); Assert.Empty(Entries(read.Remainder));
    }

    [Fact]
    public void Singleton_shape_remains_available_even_without_repeat_support()
    {
        var fixture = new Fixture(); fixture.Cycle(1, 5, 8);
        var read = fixture.Read(); var cycle = Assert.Single(read.Families, family => family.Kind == "cycle");
        Assert.Single(cycle.Occurrences); Assert.Contains(Entries(read.Candidates), row => ReferenceEquals(row, cycle.Read));
    }

    [Fact]
    public void Readers_require_actual_family_and_occurrence_identity()
    {
        var fixture = new Fixture(); fixture.Cycle(1, 4, 2, 8); var read = fixture.Read();
        var family = read.Families[0];
        Assert.False(read.TryReadFamily(new Relation(family.Read.Left, family.Read.Right), out _));
        var occurrence = family.Occurrences[0].Read;
        Assert.False(read.TryReadOccurrence(family.Read, new Relation(occurrence.Left, occurrence.Right), out _));
    }

    [Fact]
    public void Unknown_heading_does_not_turn_a_full_junction_into_a_smaller_exact_star()
    {
        var fixture = new Fixture(); var centre = fixture.Site();
        fixture.Edge(centre, fixture.Site(), 1); fixture.Edge(centre, fixture.Site(), 4);
        var endpoint = fixture.Site(); fixture.Edges.Add(new(new Placeholder(), new Relation(new Relation(centre, endpoint), StructuralConstraint.Open())));
        var read = fixture.Read();
        Assert.False(read.IsComplete);
        Assert.DoesNotContain(read.Families.Where(family => family.Kind == "junction").SelectMany(family => family.Occurrences),
            occurrence => Entries(occurrence.SiteBindings).Cast<Relation>().First().Right == centre);
        Assert.Contains(read.Families, family => family.Kind == "walk" && family.SegmentCount == 2);
    }

    [Fact]
    public void Shared_endpoint_aliases_are_part_of_the_induced_junction_definition()
    {
        var fixture = new Fixture(); var first = fixture.Site(); var second = fixture.Site(); var shared = fixture.Site();
        fixture.Edge(first, fixture.Site(), 1); fixture.Edge(first, fixture.Site(), 1); fixture.Edge(first, fixture.Site(), 4);
        fixture.Edge(second, shared, 1); fixture.Edge(second, shared, 1); fixture.Edge(second, fixture.Site(), 4);
        var read = fixture.Read(); var stars = read.Families.Where(family => family.Kind == "junction" && family.SegmentCount == 3).ToArray();
        Assert.Equal(2, stars.Length); Assert.All(stars, family => Assert.Single(family.Occurrences));
        Assert.Equal(new[] { 3, 4 }, stars.Select(family => family.SiteCount).Order().ToArray());
        Assert.All(stars, family => Assert.True(read.TryReadPattern(family.Definition, out _, out _)));
    }

    [Fact]
    public void An_open_incidence_scope_preserves_walks_but_cannot_publish_exhaustive_stars()
    {
        var fixture = new Fixture(); var a = fixture.Site(); var b = fixture.Site(); var c = fixture.Site();
        fixture.Edge(a, b, 1); fixture.Edge(b, c, 4);
        var observation = new QualitativeRecognitionObservation(fixture.Frame, StructuralConstraint.Closed(fixture.Sites.ToArray()),
            StructuralConstraint.Open(fixture.Edges.ToArray()));
        var read = new QualitativeMotifInduction(observation);
        Assert.DoesNotContain(read.Families, family => family.Kind == "junction");
        Assert.Contains(read.Families, family => family.Kind == "walk"); Assert.False(read.IsComplete);
    }

    [Fact]
    public void Duplicate_segment_identity_cannot_supply_independent_motif_support()
    {
        var fixture = new Fixture(); var a = fixture.Site(); var b = fixture.Site(); var c = fixture.Site(); var d = fixture.Site();
        var first = fixture.Edge(a, b, 1); fixture.Edge(b, c, 4);
        var fourth = fixture.Edge(c, d, 2);
        fixture.Edges.Add(new(first.Left, fourth.Right));
        var read = fixture.Read();
        Assert.False(read.IsComplete); Assert.NotEmpty(Entries(read.Unresolved));
        Assert.DoesNotContain(read.Families.SelectMany(family => family.Occurrences).SelectMany(occurrence => Entries(occurrence.Segments)),
            segment => segment is Relation row && ReferenceEquals(row.Left, first.Left));
    }

    [Fact]
    public void Duplicate_site_alias_does_not_duplicate_junction_or_walk_occurrences()
    {
        var fixture = new Fixture(); var a = fixture.Site(); var b = fixture.Site(); var c = fixture.Site();
        fixture.Edge(a, b, 1); fixture.Edge(b, c, 4); fixture.Sites.Add(a);
        var read = fixture.Read();
        Assert.False(read.IsComplete); Assert.DoesNotContain(read.Families, family => family.Kind == "junction");
        Assert.Single(Assert.Single(read.Families, family => family.Kind == "walk").Occurrences);
    }

    [Fact]
    public void Invalid_declared_crossing_stays_unresolved_even_when_local_walk_is_readable()
    {
        var fixture = new Fixture(); var a = fixture.Site(); var b = fixture.Site(); var c = fixture.Site();
        var first = fixture.Edge(a, b, 1); var second = fixture.Edge(b, c, 4);
        var observation = new QualitativeRecognitionObservation(fixture.Frame, StructuralConstraint.Closed(fixture.Sites.ToArray()),
            StructuralConstraint.Closed(fixture.Edges.ToArray()), crossings: StructuralConstraint.Closed(new Relation(first, second)));
        var read = new QualitativeMotifInduction(observation);
        Assert.False(read.IsComplete); Assert.NotEmpty(Entries(read.Unresolved));
        Assert.Contains(read.Families, family => family.Kind == "walk");
    }

    private static IElement[] Entries(Relation scope) => StructuralConstraint.TryReadScope(scope, out var entries, out _, out _) ? entries.ToArray() : [];
    private sealed class Fixture
    {
        public QualitativeMotionFrame Frame { get; } = new();
        public List<IElement> Sites { get; } = [];
        public List<Relation> Edges { get; } = [];
        public IElement Site() { var site = new Placeholder(); Sites.Add(site); return site; }
        public Relation Edge(IElement from, IElement to, int mask)
        {
            var heading = StructuralConstraint.Closed(new[] { Frame.Right, Frame.Left, Frame.Up, Frame.Down }
                .Where((_, index) => (mask & (1 << index)) != 0).Cast<IElement>().ToArray());
            var row = new Relation(new Placeholder(), new Relation(new Relation(from, to), heading)); Edges.Add(row); return row;
        }
        public IElement[] Cycle(params int[] headings)
        {
            var sites = headings.Select(_ => Site()).ToArray();
            for (var i = 0; i < headings.Length; i++) Edge(sites[i], sites[(i + 1) % headings.Length], headings[i]); return sites;
        }
        public QualitativeMotifInduction Read(int maxCycleEdges = 8, int proposalCapacity = 20000, int retainedCapacity = 3000) =>
            new(new QualitativeRecognitionObservation(Frame, StructuralConstraint.Closed(Sites.ToArray()), StructuralConstraint.Closed(Edges.ToArray())),
                maxCycleEdges, proposalCapacity, 3, retainedCapacity);
    }
}
