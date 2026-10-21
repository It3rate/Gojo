using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeFrieze;

namespace QualitativeExecution.Tests;

public sealed class NativeInteriorFeaturesTests
{
    private static readonly Lazy<IReadOnlyList<NativeLatticeSources.SourceCase>> Sources = new(NativeInteriorSources.Cases);

    [Fact]
    public void Elementary_grid_cells_are_discovered_and_classified_without_physical_crossing_joins()
    {
        var source = Case("I01"); var originalSites = E(source.Graph.Sites).Length;
        var topology = new NativeInteriorTopology(source); var features = new NativeInteriorFeatures(topology);
        Assert.Equal(4, features.Reads.Count);
        Assert.All(features.Reads, read =>
        {
            Assert.Equal(4, read.SideCount);
            Assert.Equal("Supported", read.Find("Closed").Status);
            Assert.Equal("Supported", read.Find("Four-sided").Status);
            Assert.Equal("Supported", read.Find("Rectangle").Status);
            Assert.Equal("Supported", read.Find("Square").Status);
            Assert.Equal("Refuted", read.Find("Triangle").Status);
        });
        Assert.Equal(originalSites, E(source.Graph.Sites).Length);
        Assert.DoesNotContain(OrnamentReconstruction.Participants(features.Surface), element => element is Carrier or CarrierLocation);
    }

    [Fact]
    public void Wider_columns_retain_rectangles_and_refute_equal_sides_by_shared_reach_cancellation()
    {
        var features = Read("I02"); Assert.Equal(4, features.Reads.Count);
        Assert.All(features.Reads, read =>
        {
            Assert.Equal("Supported", read.Find("Rectangle").Status);
            Assert.Equal("Refuted", read.Find("Equal-sided").Status);
            Assert.Equal("Refuted", read.Find("Square").Status);
            Assert.NotEmpty(E(read.Find("Equal-sided").Evidence.Refuted));
        });
    }

    [Fact]
    public void An_actual_bisector_selects_two_compartments_instead_of_the_surrounding_outline()
    {
        var features = Read("I16"); Assert.Equal(2, features.Reads.Count);
        Assert.All(features.Reads, read => Assert.Equal(4, read.SideCount));
        Assert.All(features.Reads, read => Assert.Equal("Supported", read.Find("Rectangle").Status));
    }

    [Fact]
    public void An_inset_is_read_as_a_paired_reflex_return_rather_than_a_rectangle_or_star()
    {
        var read = Assert.Single(Read("I17").Reads);
        Assert.Equal(8, read.SideCount);
        Assert.Equal("Supported", read.Find("Contains concavity").Status);
        Assert.Equal("Supported", read.Find("Contains notch").Status);
        Assert.Equal("Refuted", read.Find("Rectangle").Status);
        Assert.Equal("Refuted", read.Find("Star-like").Status);
        Assert.NotEmpty(E(read.Find("Contains notch").Evidence.Supported));
    }

    [Fact]
    public void A_local_sawtooth_is_not_mistaken_for_a_star_around_the_whole_boundary()
    {
        var read = Assert.Single(Read("I18").Reads);
        Assert.Equal("Supported", read.Find("Contains zigzag").Status);
        Assert.Equal("Supported", read.Find("Contains concavity").Status);
        Assert.Equal("Refuted", read.Find("Star-like").Status);
    }

    [Fact]
    public void Same_quadrant_component_order_exposes_star_turns_without_ratios_or_cross_products()
    {
        var read = Assert.Single(Read("I20").Reads);
        Assert.Equal(8, read.SideCount);
        Assert.Equal("Supported", read.Find("Contains concavity").Status);
        Assert.Equal("Supported", read.Find("Contains zigzag").Status);
        Assert.Equal("Supported", read.Find("Star-like").Status);
        Assert.Equal("Refuted", read.Find("Contains notch").Status);
    }

    [Fact]
    public void A_triangle_and_a_rotated_square_retain_separate_local_interpretations()
    {
        var features = Read("I19"); Assert.Equal(2, features.Reads.Count);
        var triangle = Assert.Single(features.Reads, read => read.SideCount == 3);
        var diamond = Assert.Single(features.Reads, read => read.SideCount == 4);
        Assert.Equal("Supported", triangle.Find("Triangle").Status);
        Assert.Equal("Refuted", triangle.Find("Rectangle").Status);
        Assert.Equal("Supported", diamond.Find("Square").Status);
        Assert.Equal("Refuted", diamond.Find("Triangle").Status);
    }

    [Fact]
    public void Withholding_the_geometric_turn_promise_keeps_a_four_sided_cell_open_as_a_rectangle()
    {
        var original = Case("I01"); var source = original with { TurnModel = false };
        var features = new NativeInteriorFeatures(new(source));
        Assert.All(features.Reads, read =>
        {
            Assert.Equal("Supported", read.Find("Four-sided").Status);
            Assert.Equal("Supported", read.Find("Equal-sided").Status);
            Assert.Equal("Open", read.Find("Rectangle").Status);
            Assert.Equal("Open", read.Find("Square").Status);
            Assert.NotEmpty(E(read.Find("Rectangle").Missing));
        });
    }

    [Fact]
    public void Diagonal_sums_do_not_gain_equal_lengths_from_an_unresolved_oblique_basis()
    {
        var original = Case("I19"); var source = original with { TurnModel = false };
        var features = new NativeInteriorFeatures(new(source));
        var diamond = Assert.Single(features.Reads, read => read.SideCount == 4);
        Assert.Equal("Supported", diamond.Find("Four-sided").Status);
        Assert.Equal("Open", diamond.Find("Equal-sided").Status);
        Assert.Equal("Open", diamond.Find("Rectangle").Status);
        Assert.Equal("Open", diamond.Find("Square").Status);
    }

    [Fact]
    public void A_six_run_boundary_does_not_need_a_calibrated_regular_hexagon_claim()
    {
        var features = Read("I05");
        Assert.Contains(features.Reads, read => read.SideCount == 6 && read.Find("Hexagon").Status == "Supported");
        Assert.All(features.Reads.Where(read => read.SideCount == 6), read =>
            Assert.Equal("Refuted", read.Find("Rectangle").Status));
    }

    [Fact]
    public void Collinear_landmarks_remain_in_the_proof_without_becoming_extra_polygon_sides()
    {
        var features = Read("I04");
        Assert.Contains(features.Reads, read => read.Boundary.Darts.Count > read.SideCount && read.SideCount == 4);
        foreach (var read in features.Reads.Where(read => read.SideCount == 4))
        {
            Assert.True(read.SideCountCertain);
            Assert.Equal("Supported", read.Find("Four-sided").Status);
            var participants = OrnamentReconstruction.Participants(read.Sides);
            Assert.All(read.Boundary.Darts, dart => Assert.Contains(dart.Surface, participants));
        }
    }

    [Fact]
    public void A_connected_hypothesis_is_possible_and_keeps_its_missing_passage_visible()
    {
        var topology = new NativeInteriorTopology(Case("I01"));
        var absentPassage = new Relation(new Placeholder(), new Placeholder());
        var assumptions = S(absentPassage); var features = new NativeInteriorFeatures(topology, assumptions);
        Assert.All(features.Reads, read =>
        {
            var claim = read.Find("Square");
            Assert.Equal("Yes", claim.Evidence.Answer);
            Assert.Equal("Possible", claim.Status);
            Assert.Contains(absentPassage, OrnamentReconstruction.Participants(claim.Surface));
        });
    }

    [Fact]
    public void Open_three_sided_route_does_not_become_an_observed_closed_shape()
    {
        var features = Read("I15"); Assert.Empty(features.Reads);
        Assert.NotEmpty(E(features.Topology.Unresolved));
    }

    [Fact]
    public void Every_claim_keeps_the_actual_source_boundary_native_requirement_and_evidence_connected()
    {
        var features = Read("I17"); var read = Assert.Single(features.Reads);
        foreach (var claim in read.Claims)
        {
            var participants = OrnamentReconstruction.Participants(claim.Surface);
            Assert.Contains(read.Boundary.Surface, participants);
            Assert.Contains(features.Topology.Port, participants);
            Assert.Contains(claim.Required, participants);
            Assert.Contains(claim.Evidence.Surface, participants);
            Assert.Same(claim.Predicate, claim.Evidence.Predicate);
        }
        var triangle = read.Find("Triangle"); var square = read.Find("Four-sided");
        Assert.Equal(3, E(triangle.Required).Length); Assert.Equal(4, E(square.Required).Length);
    }

    private static NativeLatticeSources.SourceCase Case(string id) => Sources.Value.Single(source => source.Id == id);
    private static NativeInteriorFeatures Read(string id) => new(new NativeInteriorTopology(Case(id)));
    private static Relation S(params IElement[] entries) => StructuralConstraint.Closed(entries);
    private static IElement[] E(Relation scope) => StructuralConstraint.TryReadScope(scope, out var entries, out _, out _) ? entries : [];
}
