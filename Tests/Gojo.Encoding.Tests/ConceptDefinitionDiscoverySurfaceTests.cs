using Gojo.Core;
using Gojo.Encoding;

namespace Gojo.Encoding.Tests;

public sealed class ConceptDefinitionDiscoverySurfaceTests
{
    [Fact]
    public void DefinitionGraphReadsCompoundsDependenciesGroupsAndReferences()
    {
        var distance = ConceptDefinitionSurface.Create(
            new Carrier(),
            "distance",
            Relation.Segment(new Carrier(), 0, 4));
        var time = ConceptDefinitionSurface.Create(
            new Carrier(),
            "time",
            Relation.Segment(new Carrier(), 0, 2));
        var rate = ConceptDefinitionSurface.Create(
            new Carrier(),
            "rate",
            ConceptAddressSurface.Compound(
                new Carrier(),
                "Rate",
                [
                    ConceptAddressSurface.Atom(new Carrier(), "distance"),
                    ConceptAddressSurface.Atom(new Carrier(), "time")
                ]),
            [distance, time]);
        var reusableRate = ConceptDefinitionSurface.Create(
            new Carrier(),
            "speed-read",
            ConceptDefinitionSurface.Reference(new Carrier(), rate, ConceptAddressSurface.Atom(new Carrier(), "motion-context")),
            [rate]);
        var comparison = ConceptDefinitionSurface.Create(
            new Carrier(),
            "comparison",
            Relation.OrderedGroup(new Carrier(), distance, rate),
            [distance, rate]);
        var ledger = ConceptDefinitionSurface.Ledger(new Carrier(), [distance, time, rate, reusableRate, comparison]);

        Assert.True(ConceptDefinitionDiscoverySurface.TryReadLedger(ledger, out var graph));
        Assert.True(graph.TryFindByName("rate", out var rateNode));
        Assert.True(graph.TryReadCompoundBody(rateNode, out var rateBody));
        Assert.Equal("Rate", rateBody.Lexical);
        Assert.Equal(["distance", "time"], rateBody.PartNames);
        Assert.Equal(["distance", "time"], graph.DependenciesOf(rateNode).Select(node => node.Name));

        Assert.True(graph.TryFindByName("speed-read", out var referenceNode));
        Assert.True(graph.TryReadReferenceBody(referenceNode, out var reference));
        Assert.Same(rate, reference.DefinitionOrAddress);

        Assert.True(graph.TryFindByName("comparison", out var comparisonNode));
        Assert.True(graph.TryReadDefinitionGroupBody(comparisonNode, out var participants));
        Assert.Equal(["distance", "rate"], participants.Select(node => node.Name));
    }
}
