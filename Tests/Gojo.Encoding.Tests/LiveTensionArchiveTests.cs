using Gojo.Core;
using Gojo.Equations;

namespace Gojo.Encoding.Tests;

public sealed class LiveTensionArchiveTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ArchivePreservesLivePressureIdentityHistoricalObservationAndPublication(bool inverse)
    {
        var forward = new Carrier();
        var source = inverse ? forward.Inverse() : forward;
        var output = new Carrier();
        source.Attach(new Relation(Relation.Point(source, 3), Relation.Point(source, 3)));
        source.AttachLiveOutput(source.LiveTensionSlot, Equation.Affine(source, -10, 10, output, 0, 20));
        source.Add(8);
        var root = Relation.OrderedGroup(new Carrier(), source, output, source.LiveTensionSlot, source.TensionSlot);
        var document = RoundTrip(GojoStructureCompactJson.FromElement("live-pressure", root));

        Assert.Equal(3, document.Version);
        Assert.True(GojoStructureCompactJson.TryToElement(document, out var restored));
        Assert.Equal(Relation.Point(Carrier.Unknown, 0), restored.ResidueOrTension);
        var parts = Parts(restored.Root);
        var loaded = Assert.IsType<Carrier>(parts[0]);
        var loadedOutput = Assert.IsType<Carrier>(parts[1]);
        var live = Assert.IsType<CarrierLocation>(parts[2]);
        var observation = Assert.IsType<CarrierLocation>(parts[3]);
        Assert.True(live.SameTickRefAs(loaded.LiveTensionSlot));
        Assert.False(observation.SameTickRefAs(loaded.LiveTensionSlot));
        Assert.Equal(inverse, loaded.IsInverse);
        Assert.Equal(5, live.Ticks);
        Assert.Equal(15UL, loadedOutput.Value);

        loaded.Remove(10);
        Assert.Equal(-2, live.Ticks);
        Assert.Equal(8UL, loadedOutput.Value);
        Assert.Equal(5, observation.Ticks);
        loaded.Add(2);
        Assert.Equal(0, live.Ticks);
        Assert.Equal(10UL, loadedOutput.Value);
        Assert.Equal(5, observation.Ticks);
        Assert.Equal(5, source.Tension);
        Assert.Equal(15UL, output.Value);
    }

    [Fact]
    public void OlderVersionTwoWithoutLivePressureReferenceKeepsHistoricalCoordinatesDetached()
    {
        var source = new Carrier();
        source.Remove(3);
        var root = Relation.OrderedGroup(new Carrier(), source, source.TensionSlot);
        var current = GojoStructureCompactJson.FromElement("old-pressure-observation", root);
        var pressureTicks = current.Carriers.Where(c => c.TensionTick is not null).Select(c => c.TensionTick!).ToHashSet();
        var legacy = current with
        {
            Version = 2,
            Carriers = current.Carriers.Select(c => c with { TensionTick = null }).ToList(),
            Nodes = current.Nodes.Where(n => !pressureTicks.Contains(n.Id)).ToList()
        };

        Assert.Equal(2, legacy.Version);
        Assert.True(GojoStructureCompactJson.TryToElement(RoundTrip(legacy), out var restored));
        var parts = Parts(restored.Root);
        var loaded = Assert.IsType<Carrier>(parts[0]);
        var observed = Assert.IsType<CarrierLocation>(parts[1]);
        Assert.Equal(-3, loaded.LiveTensionSlot.Ticks);
        Assert.False(loaded.LiveTensionSlot.SameTickRefAs(observed));
        loaded.Add(3);
        Assert.Equal(0, loaded.LiveTensionSlot.Ticks);
        Assert.Equal(-3, observed.Ticks);

        var upgraded = RoundTrip(GojoStructureCompactJson.FromElement("upgraded-pressure", restored.Root));
        Assert.Equal(3, upgraded.Version);
        Assert.True(GojoStructureCompactJson.TryToElement(upgraded, out var upgradedRestore));
        var upgradedParts = Parts(upgradedRestore.Root);
        var upgradedCarrier = Assert.IsType<Carrier>(upgradedParts[0]);
        var upgradedObservation = Assert.IsType<CarrierLocation>(upgradedParts[1]);
        Assert.False(upgradedObservation.SameTickRefAs(upgradedCarrier.LiveTensionSlot));
        Assert.Equal(0, upgradedCarrier.LiveTensionSlot.Ticks);
        Assert.Equal(-3, upgradedObservation.Ticks);
    }

    [Theory]
    [InlineData("absent-field")]
    [InlineData("missing")]
    [InlineData("wrong-kind")]
    [InlineData("contradiction")]
    [InlineData("alias-value")]
    public void InvalidLivePressureReferenceDoesNotPublishGuessedAccountingState(string defect)
    {
        var source = new Carrier();
        var complete = GojoStructureCompactJson.FromElement("invalid-pressure", source);
        var carrier = complete.Carriers.Single(c => c.Id == complete.Root);
        var malformed = complete with
        {
            Carriers = complete.Carriers.Select(c => c.Id != complete.Root ? c : defect switch
            {
                "absent-field" => c with { TensionTick = null },
                "missing" => c with { TensionTick = "absent-tick" },
                "wrong-kind" => c with { TensionTick = c.Id },
                "alias-value" => c with { TensionTick = c.ValueTick },
                _ => c
            }).ToList(),
            Nodes = complete.Nodes.Select(n => defect == "contradiction" && n.Id == carrier.TensionTick ? n with { Ticks = 1 } : n).ToList()
        };

        Assert.False(GojoStructureCompactJson.TryToElement(RoundTrip(malformed), out var unresolved));
        Assert.NotEqual(Relation.Point(Carrier.Unknown, 0), unresolved.ResidueOrTension);
        Assert.IsNotType<Carrier>(unresolved.Root);
        Assert.Equal(0, source.Tension);
    }

    private static GojoCompactStructureDocument RoundTrip(GojoCompactStructureDocument document) =>
        GojoStructureCompactJson.Deserialize(GojoStructureCompactJson.Serialize(document));

    private static IElement[] Parts(IElement root)
    {
        var group = Assert.IsType<Relation>(root);
        Assert.True(group.TryReadOrderedElementsOn(Assert.IsType<Carrier>(group.Right), out var parts));
        return parts;
    }
}
