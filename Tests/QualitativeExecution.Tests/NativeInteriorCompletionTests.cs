using Gojo.Core;
using Gojo.Experiments.QualitativeFrieze;

namespace QualitativeExecution.Tests;

public sealed class NativeInteriorCompletionTests
{
    private static readonly Lazy<IReadOnlyList<NativeLatticeSources.SourceCase>> Sources = new(NativeInteriorSources.Cases);

    [Fact]
    public void Missing_side_can_be_proposed_without_rewriting_the_open_observation()
    {
        var source = Sources.Value.Single(source => source.Id == "I15");
        var topology = new NativeInteriorTopology(source); var graph = topology.Graph;
        Assert.Empty(topology.Boundaries);
        var completion = new NativeInteriorCompletion(topology);
        var proposal = Assert.Single(completion.Proposals);
        var read = Assert.Single(proposal.Reads);
        Assert.Equal(4, read.SideCount);
        Assert.Equal("Possible", read.Find("Closed").Status);
        Assert.Equal("Possible", read.Find("Square").Status);
        Assert.Single(StructuralPotential.Items(proposal.Assumptions));
        Assert.Empty(topology.Boundaries); Assert.Same(graph, topology.Graph);
        Assert.Equal(3, StructuralPotential.Items(source.Graph.Strokes).Length);
        Assert.DoesNotContain(proposal.Bridge, StructuralPotential.Items(source.Graph.Strokes));
        Assert.Contains(proposal.Context, OrnamentReconstruction.Participants(read.Surface));
    }

    [Fact]
    public void Known_bisection_is_preserved_and_does_not_request_a_closing_edge()
    {
        var topology = new NativeInteriorTopology(Sources.Value.Single(source => source.Id == "I16"));
        Assert.Equal(2, topology.Boundaries.Count);
        var completion = new NativeInteriorCompletion(topology);
        Assert.Empty(completion.Proposals);
        Assert.Equal(2, topology.Boundaries.Count);
    }

    [Fact]
    public void Closure_capacity_keeps_the_unsearched_component_visible()
    {
        var topology = new NativeInteriorTopology(Sources.Value.Single(source => source.Id == "I15"));
        var completion = new NativeInteriorCompletion(topology, proposalCapacity: 0);
        Assert.Empty(completion.Proposals);
        Assert.Single(StructuralPotential.Items(completion.Unsearched));
        Assert.Empty(topology.Boundaries);
    }

    [Fact]
    public void A_closing_hypothesis_cannot_erase_the_producers_unpublished_passages()
    {
        var source = Sources.Value.Single(source => source.Id == "I15");
        var graph = new QualitativeStrokeGraph(source.Graph.Frame, source.Graph.Sites,
            Gojo.Patterns.StructuralConstraint.Open(StructuralPotential.Items(source.Graph.Strokes)));
        var topology = new NativeInteriorTopology(source with { Graph = graph });
        Assert.NotEmpty(StructuralPotential.Items(topology.EmbeddingUnknown));
        var proposal = Assert.Single(new NativeInteriorCompletion(topology).Proposals);
        var read = Assert.Single(proposal.Reads);
        Assert.Equal("Open", read.Find("Elementary").Status);
        Assert.NotEmpty(StructuralPotential.Items(proposal.Topology.EmbeddingUnknown));
        Assert.Equal("Possible", read.Find("Closed").Status);
    }
}
