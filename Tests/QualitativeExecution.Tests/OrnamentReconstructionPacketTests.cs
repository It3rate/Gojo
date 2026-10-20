using Gojo.Core;
using Gojo.Encoding.Structures;
using Gojo.Experiments.QualitativeExecution;
using Gojo.Experiments.QualitativeFrieze;
using Gojo.Patterns;

namespace QualitativeExecution.Tests;

public sealed class OrnamentReconstructionPacketTests
{
    [Fact]
    public void The_packet_contains_fresh_public_structure_while_correspondence_and_private_ancestry_remain_outside()
    {
        var source = Sample("X-CROSS"); var packet = new OrnamentReconstructionPacket(source.Observation, includeRelations: true);
        var actual = OrnamentReconstructionPacket.Participants(packet.Surface);
        var sourcePublic = OrnamentReconstructionPacket.Participants(source.Observation.Surface);
        Assert.Empty(actual.Intersect(sourcePublic, ReferenceEqualityComparer.Instance));
        foreach (var privateRoot in new IElement[] { source.Surface, source.Program.Surface, source.Execution.Surface,
            source.Publisher.Surface, source.MechanicalGroups, source.MechanicalEvidence, source.Bindings, packet.Correspondence })
            Assert.DoesNotContain(privateRoot, actual);
        Assert.Contains(source.Observation.Surface, OrnamentReconstructionPacket.Participants(packet.Correspondence));
        Assert.DoesNotContain(actual, item => item is Carrier or CarrierLocation);
        Assert.Equal("Yes", new OrnamentReconstructionGraphRead(source.Observation, packet.Observation).Answer);
    }

    [Fact]
    public void Omitted_relations_are_open_and_only_the_selected_incidence_scopes_have_a_complete_correspondence()
    {
        var source = Sample("X-CROSS"); var packet = new OrnamentReconstructionPacket(source.Observation, includeRelations: false);
        Assert.False(Closed(packet.Observation.Continuations)); Assert.False(Closed(packet.Observation.Crossings));
        Assert.True(Closed(packet.Observation.Sites)); Assert.True(Closed(packet.Observation.Segments));
        Assert.Equal("Yes", new OrnamentReconstructionGraphRead(source.Observation, packet.Observation, includeRelations: false).Answer);
        var strict = new OrnamentReconstructionGraphRead(source.Observation, packet.Observation);
        Assert.Equal("Open", strict.Answer); Assert.Empty(E(strict.Refuted));
    }

    [Fact]
    public void Unknown_heading_and_open_source_scopes_are_preserved_instead_of_filled_by_the_receiver()
    {
        var f = new QualitativeMotionFrame(); var a = new Placeholder(); var b = new Placeholder();
        var edge = new Relation(new Placeholder(), new Relation(new Relation(a, b), StructuralConstraint.Open(f.Right)));
        var source = new QualitativeRecognitionObservation(f, StructuralConstraint.Open(a, b), S(edge),
            StructuralConstraint.Open(), StructuralConstraint.Open());
        var packet = new OrnamentReconstructionPacket(source, includeRelations: true);
        Assert.False(Closed(packet.Observation.Sites)); Assert.False(Closed(packet.Observation.Continuations));
        Assert.False(Closed(packet.Observation.Crossings));
        var mapped = Assert.IsType<Relation>(Assert.Single(E(packet.Observation.Segments)));
        Assert.True(packet.Observation.TryReadSegment(mapped, out _, out _, out _, out var heading));
        Assert.False(Closed(heading)); Assert.Single(E(heading));
        Assert.Equal("Open", new OrnamentReconstructionGraphRead(source, packet.Observation).Answer);
    }

    [Fact]
    public void Equivalent_placeholder_faces_resolve_to_the_same_site_even_with_distinct_clr_wrappers()
    {
        var f = new QualitativeMotionFrame(); var a = new Placeholder(); var b = new Placeholder();
        var aAlias = a.Dual().Dual(); var bAlias = b.Dual().Dual();
        Assert.NotSame(a, aAlias); Assert.Equal(a, aAlias);
        var edge = new Relation(new Placeholder(), new Relation(new Relation(aAlias, bAlias), S(f.Right)));
        var source = new QualitativeRecognitionObservation(f, S(a, b), S(edge));
        Assert.True(source.IsComplete); Assert.True(source.TryReadSegment(edge, out _, out _, out _, out _));
        var packet = new OrnamentReconstructionPacket(source, includeRelations: true);
        Assert.Equal("Yes", new OrnamentReconstructionGraphRead(source, packet.Observation).Answer);
        Assert.Equal(2, E(packet.Observation.Sites).Length);
    }

    [Fact]
    public void Shared_junction_sites_and_crossing_order_survive_copy_and_native_transport()
    {
        var source = Sample("H"); var beforeSites = source.Observation.Sites; var beforeEdges = source.Observation.Segments;
        var packet = new OrnamentReconstructionPacket(source.Observation, includeRelations: true);
        Assert.Same(beforeSites, source.Observation.Sites); Assert.Same(beforeEdges, source.Observation.Segments);
        Assert.Equal("Yes", new OrnamentReconstructionGraphRead(source.Observation, packet.Observation).Answer);
        var json = GojoStructureCompactJson.Serialize(GojoStructureCompactJson.FromElement("source-free graph packet", packet.Surface));
        Assert.True(GojoStructureCompactJson.TryToElement(GojoStructureCompactJson.Deserialize(json), out var restored));
        var original = OrnamentReconstructionPacket.Participants(packet.Surface);
        var imported = OrnamentReconstructionPacket.Participants(restored.Root);
        Assert.Equal(original.Count, imported.Count);
        Assert.Empty(imported.Intersect(original, ReferenceEqualityComparer.Instance));
        Assert.DoesNotContain(source.Observation.Surface, imported);
        Assert.DoesNotContain(imported, item => item is Carrier or CarrierLocation);

        var crossed = Sample("X-CROSS"); var crossedPacket = new OrnamentReconstructionPacket(crossed.Observation, true);
        var correspondence = Assert.IsType<Relation>(crossedPacket.Correspondence.Right);
        var edges = E(Assert.IsType<Relation>(correspondence.Right)).Cast<Relation>().ToArray();
        var sourceRow = Assert.IsType<Relation>(Assert.Single(E(crossed.Observation.Crossings)));
        var copiedRow = Assert.IsType<Relation>(Assert.Single(E(crossedPacket.Observation.Crossings)));
        Assert.Same(edges.Single(row => ReferenceEquals(row.Left, sourceRow.Left)).Right, copiedRow.Left);
        Assert.Same(edges.Single(row => ReferenceEquals(row.Left, sourceRow.Right)).Right, copiedRow.Right);
    }

    private static OrnamentReconstructionCases.Sample Sample(string family) => OrnamentReconstructionCases.Specs.Single(spec =>
        spec.Family == family && spec.Transform == 0 && spec.ReachRecipe == 0 && spec.Repeats == 1).Create();
    private static Relation S(params IElement[] entries) => StructuralConstraint.Closed(entries);
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
    private static bool Closed(Relation scope) => StructuralConstraint.TryReadScope(scope, out _, out var seam, out var tail) && seam.Equals(tail);
}
