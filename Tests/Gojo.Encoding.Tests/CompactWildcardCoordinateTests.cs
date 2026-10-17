using Gojo.Core;

namespace Gojo.Encoding.Tests;

public sealed class CompactWildcardCoordinateTests
{
    [Fact]
    public void CompleteCompactCaptureRetainsCanonicalWildcardSlotsAndSeparateObservation()
    {
        var root = Relation.OrderedGroup(new Carrier(), Carrier.Unknown,
            Carrier.Unknown.ValueSlot, Carrier.Unknown.LiveTensionSlot, new CarrierLocation(Carrier.Unknown, new TickRef(0)));
        var document = RoundTrip(GojoStructureCompactJson.FromElement("wildcard-coordinates", root));

        var wildcard = Assert.Single(document.Carriers, carrier => carrier.IsUnknown);
        Assert.NotNull(wildcard.ValueTick);
        Assert.NotNull(wildcard.TensionTick);
        Assert.NotEqual(wildcard.ValueTick, wildcard.TensionTick);
        Assert.True(GojoStructureCompactJson.TryToElement(document, out var restored));
        var group = Assert.IsType<Relation>(restored.Root);
        Assert.True(group.TryReadOrderedElementsOn(Assert.IsType<Carrier>(group.Right), out var parts));
        Assert.Same(Carrier.Unknown, parts[0]);
        Assert.True(Assert.IsType<CarrierLocation>(parts[1]).SameTickRefAs(Carrier.Unknown.ValueSlot));
        Assert.True(Assert.IsType<CarrierLocation>(parts[2]).SameTickRefAs(Carrier.Unknown.LiveTensionSlot));
        var observation = Assert.IsType<CarrierLocation>(parts[3]);
        Assert.False(observation.SameTickRefAs(Carrier.Unknown.ValueSlot));
        Assert.False(observation.SameTickRefAs(Carrier.Unknown.LiveTensionSlot));
        Assert.Equal(0, Carrier.Unknown.LiveTensionSlot.Ticks);
        Assert.Equal(0, Carrier.Unknown.ValueSlot.Ticks);
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("wrong-kind")]
    [InlineData("nonzero")]
    [InlineData("alias-wildcard-value")]
    [InlineData("alias-mutable-value")]
    public void ContradictoryWildcardCoordinatesRemainUnresolvedWithoutChangingGlobalSlots(string defect)
    {
        var root = Relation.OrderedGroup(new Carrier(), Carrier.Unknown);
        var complete = GojoStructureCompactJson.FromElement("invalid-wildcard-coordinate", root);
        var wildcard = Assert.Single(complete.Carriers, carrier => carrier.IsUnknown);
        var mutable = complete.Carriers.First(carrier => !carrier.IsUnknown);
        var malformed = complete with
        {
            Carriers = complete.Carriers.Select(carrier => !carrier.IsUnknown ? carrier : defect switch
            {
                "missing" => carrier with { TensionTick = "missing-tick" },
                "wrong-kind" => carrier with { TensionTick = carrier.Id },
                "alias-wildcard-value" => carrier with { TensionTick = carrier.ValueTick },
                "alias-mutable-value" => carrier with { TensionTick = mutable.ValueTick },
                _ => carrier
            }).ToList(),
            Nodes = complete.Nodes.Select(node => defect == "nonzero" && node.Id == wildcard.TensionTick
                ? node with { Ticks = 1 } : node).ToList()
        };

        Assert.False(GojoStructureCompactJson.TryToElement(RoundTrip(malformed), out var unresolved));
        Assert.NotEqual(Relation.Point(Carrier.Unknown, 0), unresolved.ResidueOrTension);
        Assert.Equal(0, Carrier.Unknown.ValueSlot.Ticks);
        Assert.Equal(0, Carrier.Unknown.LiveTensionSlot.Ticks);
    }

    private static GojoCompactStructureDocument RoundTrip(GojoCompactStructureDocument document) =>
        GojoStructureCompactJson.Deserialize(GojoStructureCompactJson.Serialize(document));
}
