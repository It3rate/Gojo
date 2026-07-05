using Gojo.Core;
using Gojo.Structures;
using GojoCAD.Electronics.Components.Resistors;
using System.Text.Json.Nodes;

namespace GojoCAD.Tests;

public sealed class ResistanceValueSurfaceTests
{
    [Fact]
    public void E6NominalStoresStepCountAndGojoCarryRead()
    {
        var resistanceParameter = Relation.Segment(new Carrier(), 0, 1);

        var nominal = ResistanceValueSurface.E6Nominal(
            new Carrier(),
            resistanceParameter,
            e6Steps: 22);

        Assert.True(ResistanceValueSurface.TryReadE6Nominal(
            nominal,
            out var readParameter,
            out _,
            out var repeat,
            out var carryRead,
            out _,
            out _));
        Assert.Same(resistanceParameter, readParameter);
        Assert.True(ResistanceValueSurface.TryReadE6StepCount(repeat, out var stepCount));
        Assert.Equal(22, stepCount.Ticks);

        Assert.True(ResistanceValueSurface.TryReadE6CarryRead(carryRead, out var levels));
        Assert.True(levels.Length >= 2);
        Assert.Equal(4UL, levels[0].Value);
        Assert.Equal(3UL, levels[1].Value);
        Assert.Equal(22, ResolutionLadder.ReadCompositeFromRadix(levels, 6));

        Assert.True(ResistanceValueSurface.TryReadE6Position(
            nominal,
            out var steps,
            out var phase,
            out var decadeCarry));
        Assert.Equal(22, steps);
        Assert.Equal(4, phase);
        Assert.Equal(3, decadeCarry);
        Assert.True(ResistanceValueSurface.TryReadE6PhaseLabel(phase, out var label));
        Assert.Equal("4.7", label);
    }

    [Fact]
    public void E6NominalReadsTenKAsPhaseZeroAtNextCarry()
    {
        var resistanceParameter = Relation.Segment(new Carrier(), 0, 1);

        var nominal = ResistanceValueSurface.E6Nominal(
            new Carrier(),
            resistanceParameter,
            e6Steps: 24);

        Assert.True(ResistanceValueSurface.TryReadE6Position(
            nominal,
            out var steps,
            out var phase,
            out var decadeCarry));
        Assert.Equal(24, steps);
        Assert.Equal(0, phase);
        Assert.Equal(4, decadeCarry);
        Assert.True(ResistanceValueSurface.TryReadE6PhaseLabel(phase, out var label));
        Assert.Equal("1.0", label);
    }

    [Fact]
    public void RelativeToleranceStoresResolutionWithoutEndpointValues()
    {
        var nominal = ResistanceValueSurface.E6Nominal(
            new Carrier(),
            Relation.Segment(new Carrier(), 0, 1),
            e6Steps: 24);

        var tolerance = ResistanceValueSurface.RelativeTolerance(
            new Carrier(),
            nominal,
            partsPerNominal: 20);

        Assert.True(ResistanceValueSurface.TryReadRelativeTolerance(
            tolerance,
            out var selectedNominal,
            out var relativeResolution,
            out var selectedGrain,
            out _,
            out _));
        Assert.Same(nominal, selectedNominal);
        Assert.Equal(1, selectedGrain.Ticks);
        var start = Assert.IsType<Relation>(relativeResolution.Left).LeftLocation;
        var end = Assert.IsType<Relation>(relativeResolution.Right).LeftLocation;
        Assert.True(start.Carrier.SameIdentityAs(end.Carrier));
        Assert.Equal(0, start.Ticks);
        Assert.Equal(20, end.Ticks);
    }

    [Fact]
    public void ZeroOhmBridgeIsSeparateFromPositiveE6Ladder()
    {
        var terminalA = Relation.Point(new Carrier(), 1);
        var terminalB = Relation.Point(new Carrier(), 2);

        var zeroOhm = ResistanceValueSurface.ZeroOhmBridge(
            new Carrier(),
            terminalA,
            terminalB);

        Assert.True(ResistanceValueSurface.TryReadZeroOhmBridge(
            zeroOhm,
            out var readA,
            out var readB,
            out var bridge,
            out var positiveLadderEvidence,
            out _,
            out _));
        Assert.Same(terminalA, readA);
        Assert.Same(terminalB, readB);
        Assert.Same(terminalA, bridge.Left);
        Assert.Same(terminalB, bridge.Right);
        Assert.IsType<Relation>(positiveLadderEvidence);
    }

    [Fact]
    public void ResistorValueJsonFixtureLoadsIntoE6AndToleranceSurfaces()
    {
        var fixture = ReadFixture("4k7-5-percent.resistor-value.json");

        Assert.Equal("gojocad.electronics.resistor-value.v0", fixture["schema"]?.GetValue<string>());
        Assert.Equal("R1", fixture["id"]?.GetValue<string>());
        Assert.Equal("resistor", fixture["component"]?["kind"]?.GetValue<string>());
        Assert.False(fixture.ToJsonString().Contains("lowerOhms", StringComparison.OrdinalIgnoreCase));
        Assert.False(fixture.ToJsonString().Contains("upperOhms", StringComparison.OrdinalIgnoreCase));

        var terminalAxis = new Carrier();
        var terminalA = Relation.Point(terminalAxis, 0);
        var terminalB = Relation.Point(terminalAxis, 1);
        var resistanceParameter = new Relation(terminalA, terminalB);
        var nominalSpec = fixture["encoded"]?["nominalResistance"];
        var toleranceSpec = fixture["encoded"]?["tolerance"];

        var nominal = ResistanceValueSurface.E6Nominal(
            new Carrier(),
            resistanceParameter,
            nominalSpec?["stepsFromReference"]?.GetValue<long>() ?? 0,
            provenance: Relation.Point(new Carrier(), 1));

        Assert.True(ResistanceValueSurface.TryReadE6Position(
            nominal,
            out var steps,
            out var phase,
            out var decadeCarry));
        Assert.Equal(22, steps);
        Assert.Equal(fixture["readHints"]?["nominalRead"]?["phase"]?.GetValue<long>(), phase);
        Assert.Equal(fixture["readHints"]?["nominalRead"]?["decadeCarry"]?.GetValue<long>(), decadeCarry);
        Assert.True(ResistanceValueSurface.TryReadE6PhaseLabel(phase, out var label));
        Assert.Equal(fixture["readHints"]?["nominalRead"]?["phaseLabel"]?.GetValue<string>(), label);

        var tolerance = ResistanceValueSurface.RelativeTolerance(
            new Carrier(),
            nominal,
            toleranceSpec?["partsPerNominal"]?.GetValue<long>() ?? 0);

        Assert.True(ResistanceValueSurface.TryReadRelativeTolerance(
            tolerance,
            out var selectedNominal,
            out var relativeResolution,
            out var selectedGrain,
            out _,
            out _));
        Assert.Same(nominal, selectedNominal);
        Assert.Equal(toleranceSpec?["selectedGrain"]?.GetValue<long>(), selectedGrain.Ticks);
        var start = Assert.IsType<Relation>(relativeResolution.Left).LeftLocation;
        var end = Assert.IsType<Relation>(relativeResolution.Right).LeftLocation;
        Assert.Equal(0, start.Ticks);
        Assert.Equal(toleranceSpec?["partsPerNominal"]?.GetValue<long>(), end.Ticks);
        Assert.Equal("unresolved", fixture["encoded"]?["actualResistance"]?["state"]?.GetValue<string>());
    }

    [Fact]
    public void PhysicalResistorValueJsonFixtureReusesBasisSurfacesAndLoadsIntoGojo()
    {
        var bases = ReadElectronicsFixture("bases", "resistor-value-bases.gojo.json");
        var fixture = ReadElectronicsFixture("resistors", "4k7-5-percent.resistor-value.gojo.json");
        var e6Basis = FindSurface(bases, 6006);
        var toleranceBasis = FindSurface(bases, 5005);

        var e6Ordered = RequiredArray(e6Basis["ordered"]);
        var oneStep = RequiredArray(RequiredObject(e6Ordered[0])["segment"]);
        Assert.Equal(2, oneStep.Count);
        Assert.Equal(1, ReadSegmentEnd(oneStep));
        var carryRelation = RequiredArray(RequiredObject(e6Ordered[1])["relation"]);
        var carrySource = RequiredArray(RequiredObject(carryRelation[0])["segment"]);
        var carryTarget = RequiredArray(RequiredObject(carryRelation[1])["point"]);
        Assert.Equal(2, carrySource.Count);
        Assert.Single(carryTarget);
        Assert.Equal(6, ReadSegmentEnd(carrySource));
        Assert.Equal(1, ReadPointTicks(carryTarget));
        Assert.Equal(1, RequiredObject(e6Ordered[3])["residue"]!.GetValue<long>());

        var toleranceOrdered = RequiredArray(toleranceBasis["ordered"]);
        var toleranceSpan = RequiredArray(RequiredObject(toleranceOrdered[0])["segment"]);
        var toleranceGrain = RequiredArray(RequiredObject(toleranceOrdered[1])["point"]);
        Assert.Equal(2, toleranceSpan.Count);
        Assert.Single(toleranceGrain);
        Assert.Equal(20, ReadSegmentEnd(toleranceSpan));
        Assert.Equal(1, ReadPointTicks(toleranceGrain));
        Assert.Equal(1, RequiredObject(toleranceOrdered[2])["residue"]!.GetValue<long>());

        var ordered = RequiredArray(fixture["gojo"]?["ordered"]);
        var terminalRelation = RequiredArray(RequiredObject(ordered[0])["relation"]);
        var terminalA = RequiredArray(RequiredObject(terminalRelation[0])["point"]);
        var terminalB = RequiredArray(RequiredObject(terminalRelation[1])["point"]);
        Assert.Single(terminalA);
        Assert.Single(terminalB);
        var terminalAxis = new Carrier();
        var terminalAPoint = Relation.Point(terminalAxis, ReadPointTicks(terminalA));
        var terminalBPoint = Relation.Point(terminalAxis, ReadPointTicks(terminalB));
        var resistanceParameter = new Relation(terminalAPoint, terminalBPoint);

        var nominalRepeat = RequiredObject(RequiredObject(ordered[1])["repeat"]);
        Assert.Equal(6006, nominalRepeat["surface"]!.GetValue<long>());
        var countSpan = RequiredArray(RequiredObject(nominalRepeat["count"])["span"]);
        Assert.Equal(2, countSpan.Count);
        var e6Steps = ReadSegmentEnd(countSpan);

        var nominal = ResistanceValueSurface.E6Nominal(
            new Carrier(),
            resistanceParameter,
            e6Steps);

        Assert.True(ResistanceValueSurface.TryReadE6Position(
            nominal,
            out var steps,
            out var phase,
            out var decadeCarry));
        Assert.Equal(22, steps);
        Assert.Equal(fixture["inspect"]?["nominalRead"]?["phase"]?.GetValue<long>(), phase);
        Assert.Equal(fixture["inspect"]?["nominalRead"]?["decadeCarry"]?.GetValue<long>(), decadeCarry);

        var toleranceUse = RequiredObject(RequiredObject(ordered[2])["use"]);
        Assert.Equal(5005, toleranceUse["surface"]!.GetValue<long>());
        var tolerance = ResistanceValueSurface.RelativeTolerance(
            new Carrier(),
            nominal,
            ReadSegmentEnd(toleranceSpan));

        Assert.True(ResistanceValueSurface.TryReadRelativeTolerance(
            tolerance,
            out var selectedNominal,
            out var relativeResolution,
            out var selectedGrain,
            out _,
            out _));
        Assert.Same(nominal, selectedNominal);
        Assert.Equal(ReadPointTicks(toleranceGrain), selectedGrain.Ticks);
        Assert.Equal(20, Assert.IsType<Relation>(relativeResolution.Right).LeftLocation.Ticks);

        Assert.Equal(1, RequiredObject(ordered[3])["residue"]!.GetValue<long>());
        Assert.False(fixture["gojo"]!.ToJsonString().Contains("4.7", StringComparison.OrdinalIgnoreCase));
        Assert.False(fixture["gojo"]!.ToJsonString().Contains("5%", StringComparison.OrdinalIgnoreCase));
    }

    private static JsonObject ReadFixture(string fixtureName)
    {
        return ReadElectronicsFixture("resistors", fixtureName);
    }

    private static JsonObject ReadElectronicsFixture(params string[] pathParts)
    {
        var path = Path.Combine([AppContext.BaseDirectory, "Fixtures", "Electronics", .. pathParts]);
        return Assert.IsType<JsonObject>(JsonNode.Parse(File.ReadAllText(path)));
    }

    private static JsonObject FindSurface(JsonObject document, long surfaceId)
    {
        var surfaces = RequiredArray(document["gojo"]?["surfaces"]);
        foreach (var surfaceNode in surfaces)
        {
            var surface = RequiredObject(surfaceNode);
            if (surface["id"]?.GetValue<long>() == surfaceId)
            {
                return surface;
            }
        }

        throw new InvalidOperationException($"Surface {surfaceId} was not found.");
    }

    private static JsonObject RequiredObject(JsonNode? node) =>
        Assert.IsType<JsonObject>(node);

    private static JsonArray RequiredArray(JsonNode? node) =>
        Assert.IsType<JsonArray>(node);

    private static long ReadPointTicks(JsonArray point) =>
        point[point.Count - 1]!.GetValue<long>();

    private static long ReadSegmentEnd(JsonArray segment) =>
        segment[segment.Count - 1]!.GetValue<long>();
}
