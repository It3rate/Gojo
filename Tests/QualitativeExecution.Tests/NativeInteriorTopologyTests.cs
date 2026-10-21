using Gojo.Core;
using Gojo.Patterns;
using Gojo.Encoding.Structures;
using Gojo.Experiments.QualitativeFrieze;

namespace QualitativeExecution.Tests;

public sealed class NativeInteriorTopologyTests
{
    [Fact]
    public void Unjoined_axis_strokes_yield_only_elementary_observational_boundaries()
    {
        var source = NativeLatticeSources.Cases().Single(item => item.Id == "N02");
        var read = new NativeInteriorTopology(source);
        Assert.Equal(4, read.Boundaries.Count);
        Assert.All(read.Boundaries, boundary => Assert.Equal(4, boundary.Darts.Count));
        Assert.All(read.Boundaries, boundary => Assert.Same(boundary.Darts[0].From, boundary.Darts[^1].To));
        Assert.NotEmpty(E(read.OpenWalks)); Assert.Empty(E(read.Unsearched));
        Assert.Equal(6, E(source.Graph.Strokes).Length);
        Assert.Equal(12, E(source.Graph.Sites).Length);
        Assert.Contains(read.Nodes, node => E(node.SourceSites).Length == 0 && E(node.Encounters).Length != 0);
        // A new observational intersection does not acquire any source site.
        Assert.All(read.Boundaries.SelectMany(boundary => boundary.Darts), dart =>
            Assert.Contains(read.Edges, edge => ReferenceEquals(edge, dart.Edge)));
    }

    [Fact]
    public void A_shared_bisector_yields_two_regions_and_never_the_larger_cycle()
    {
        var source = Fixture([(0, 0), (1, 0), (2, 0), (2, 2), (1, 2), (0, 2)],
            [(0, 1), (1, 2), (2, 3), (3, 4), (4, 5), (5, 0), (1, 4)]);
        var read = new NativeInteriorTopology(source);
        Assert.Equal(2, read.Boundaries.Count);
        Assert.All(read.Boundaries, boundary => Assert.Equal(4, boundary.Darts.Count));
        Assert.DoesNotContain(read.Boundaries, boundary => boundary.Darts.Count == 6);
    }

    [Fact]
    public void An_interior_endpoint_on_an_axis_stroke_is_discovered_without_source_join()
    {
        // Both ends of the bisector merely coincide with interiors of long
        // source strokes. Its physical endpoint sites remain independent.
        var source = Fixture([(0, 0), (2, 0), (2, 2), (0, 2), (1, 0), (1, 2)],
            [(0, 1), (1, 2), (2, 3), (3, 0), (4, 5)]);
        var read = new NativeInteriorTopology(source);
        Assert.Equal(2, read.Boundaries.Count); Assert.Equal(7, read.Edges.Count);
        Assert.Equal(5, E(source.Graph.Strokes).Length);
        Assert.All(read.Boundaries, boundary => Assert.Equal(4, boundary.Darts.Count));
    }

    [Fact]
    public void Open_three_sided_path_does_not_silently_acquire_a_closure()
    {
        var read = new NativeInteriorTopology(Fixture([(0, 0), (2, 0), (2, 2), (0, 2)], [(0, 1), (1, 2), (2, 3)]));
        Assert.Empty(read.Boundaries); Assert.NotEmpty(E(read.Unresolved));
        Assert.Equal(2, read.Nodes.Count(node => read.Edges.Count(edge => ReferenceEquals(edge.From, node) || ReferenceEquals(edge.To, node)) == 1));
    }

    [Fact]
    public void Triangle_diamond_and_notch_walks_use_the_same_native_cyclic_basis()
    {
        var triangle = new NativeInteriorTopology(Fixture([(0, 0), (2, 0), (1, 2)], [(0, 1), (1, 2), (2, 0)]));
        Assert.Equal(3, Assert.Single(triangle.Boundaries).Darts.Count);
        var diamond = new NativeInteriorTopology(Fixture([(0, 1), (1, 0), (2, 1), (1, 2)], [(0, 1), (1, 2), (2, 3), (3, 0)]));
        Assert.Equal(4, Assert.Single(diamond.Boundaries).Darts.Count);
        var notch = new NativeInteriorTopology(Fixture([(0, 0), (4, 0), (4, 4), (3, 4), (3, 3), (1, 3), (1, 4), (0, 4)],
            [(0, 1), (1, 2), (2, 3), (3, 4), (4, 5), (5, 6), (6, 7), (7, 0)]));
        Assert.Equal(8, Assert.Single(notch.Boundaries).Darts.Count);
        Assert.All(new[] { triangle, diamond, notch }, read => Assert.Equal(8, E(Assert.IsType<Relation>(read.Basis.Right)).Length));
    }

    [Fact]
    public void Unestablished_diagonal_crossing_stays_visible_as_unresolved_geometry()
    {
        var source = Fixture([(0, 0), (2, 0), (2, 2), (0, 2)],
            [(0, 1), (1, 2), (2, 3), (3, 0), (0, 2), (1, 3)]);
        var read = new NativeInteriorTopology(source);
        Assert.NotEmpty(E(read.Unresolved));
        Assert.DoesNotContain(read.Nodes, node => node.Word is not null && E(node.SourceSites).Length == 0);
        Assert.Equal(6, read.Edges.Count);
    }

    [Fact]
    public void Coincident_duplicate_strokes_retain_occurrences_without_duplicate_faces()
    {
        var source = Fixture([(0, 0), (2, 0), (2, 2), (0, 2)], [(0, 1), (1, 2), (2, 3), (3, 0), (1, 0)]);
        var read = new NativeInteriorTopology(source);
        Assert.Equal(4, read.Edges.Count); Assert.Single(read.Boundaries);
        Assert.Contains(read.Edges, edge => E(edge.SourceUses).Length == 2);
        Assert.Equal(5, E(source.Graph.Strokes).Length);
    }

    [Fact]
    public void Face_capacity_retains_omitted_closed_walks_separately_from_exterior()
    {
        var source = NativeLatticeSources.Cases().Single(item => item.Id == "N02");
        var read = new NativeInteriorTopology(source, faceCapacity: 1);
        Assert.Single(read.Boundaries); Assert.Equal(3, E(read.Unsearched).Length); Assert.NotEmpty(E(read.OpenWalks));
    }

    [Fact]
    public void Withheld_straight_interpolation_is_not_recovered_from_private_program_causes()
    {
        var source = NativeLatticeSources.Cases().Single(item => item.Id == "N07");
        var read = new NativeInteriorTopology(source);
        Assert.Empty(read.Edges); Assert.Empty(read.Boundaries); Assert.NotEmpty(E(read.Unresolved));
        Assert.NotSame(source.Graph, read.Source.Graph);
    }

    [Fact]
    public void Disconnected_inner_loop_qualifies_the_larger_boundary_as_obstructed()
    {
        var read = new NativeInteriorTopology(Fixture(
            [(0, 0), (6, 0), (6, 6), (0, 6), (2, 2), (4, 2), (4, 4), (2, 4)],
            [(0, 1), (1, 2), (2, 3), (3, 0), (4, 5), (5, 6), (6, 7), (7, 4)]));
        Assert.Equal(2, read.Boundaries.Count);
        Assert.Single(read.Boundaries, boundary => E(boundary.InteriorObstructions).Length == 4);
        Assert.Single(read.Boundaries, boundary => E(boundary.InteriorObstructions).Length == 0);
        Assert.All(read.Boundaries, boundary => Assert.Empty(E(boundary.InteriorUnknown)));
    }

    [Fact]
    public void Known_open_tails_do_not_make_the_planar_embedding_unknown()
    {
        var read = new NativeInteriorTopology(NativeLatticeSources.Cases().Single(item => item.Id == "N02"));
        Assert.NotEmpty(E(read.OpenWalks)); Assert.Empty(E(read.EmbeddingUnknown));
        Assert.All(read.Boundaries, boundary => Assert.Empty(E(boundary.InteriorObstructions)));
    }

    [Fact]
    public void An_open_stroke_inventory_does_not_certify_that_a_known_boundary_is_unbisected()
    {
        var source = Fixture([(0, 0), (2, 0), (2, 2), (0, 2)], [(0, 1), (1, 2), (2, 3), (3, 0)]);
        var graph = new QualitativeStrokeGraph(source.Graph.Frame, source.Graph.Sites, StructuralConstraint.Open(E(source.Graph.Strokes)));
        var read = new NativeInteriorTopology(source with { Graph = graph });
        Assert.Single(read.Boundaries); Assert.NotEmpty(E(read.EmbeddingUnknown));
    }

    [Theory]
    [InlineData("I01")]
    [InlineData("I17")]
    public void Native_port_round_trip_preserves_discovery_without_private_history(string id)
    {
        var source = NativeInteriorSources.Cases().Single(item => item.Id == id);
        var original = new NativeInteriorTopology(source);
        var packet = GojoStructureCompactJson.FromElement("public-interior", original.Port);
        Assert.True(GojoStructureCompactJson.TryToElement(GojoStructureCompactJson.Deserialize(
            GojoStructureCompactJson.Serialize(packet)), out var restored));
        Assert.True(NativeInteriorTopology.TryReadPort(Assert.IsType<Relation>(restored.Root), out var imported, out var witness));
        Assert.NotNull(witness); Assert.Equal("imported", imported.Source.Id);
        Assert.Equal(original.Nodes.Count, imported.Nodes.Count); Assert.Equal(original.Edges.Count, imported.Edges.Count);
        Assert.Equal(original.Boundaries.Select(boundary => boundary.Darts.Count), imported.Boundaries.Select(boundary => boundary.Darts.Count));
        Assert.Equal(E(original.EmbeddingUnknown).Length, E(imported.EmbeddingUnknown).Length);
        var before = original.GeometryBasis.ReadRightAngle(new Relation(new Placeholder(), new Placeholder()));
        var after = imported.GeometryBasis.ReadRightAngle(new Relation(new Placeholder(), new Placeholder()));
        Assert.Equal(before.Answer, after.Answer);
        Assert.DoesNotContain(OrnamentReconstruction.Participants(original.Port), participant => ReferenceEquals(participant, source.Provenance));
    }

    private static NativeLatticeSources.SourceCase Fixture((int X, int Y)[] positions, (int From, int To)[] links)
    {
        var author = new LatticeLadderAuthor();
        var sites = positions.Select(_ => (IElement)new Placeholder()).ToArray();
        var strokes = links.Select(link => (IElement)QualitativeStrokeGraph.Stroke(new Placeholder(), sites[link.From], sites[link.To],
            author.Word(positions[link.To].X - positions[link.From].X, positions[link.To].Y - positions[link.From].Y))).ToArray();
        var graph = new QualitativeStrokeGraph(author.Frame, S(sites), S(strokes));
        return new("fixture", "fixture", "native test source", graph,
            S(positions.Select((point, index) => (IElement)new Relation(sites[index], author.Word(point.X, point.Y))).ToArray()),
            graph.Strokes, S(), true);
    }
    private static Relation S(params IElement[] entries) => StructuralConstraint.Closed(entries);
    private static IElement[] E(Relation scope) => StructuralConstraint.TryReadScope(scope, out var entries, out _, out _) ? entries : [];
}
