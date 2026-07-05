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

    private static JsonObject ReadFixture(string fixtureName)
    {
        var path = Path.Combine(
            AppContext.BaseDirectory,
            "Fixtures",
            "Electronics",
            "resistors",
            fixtureName);

        return Assert.IsType<JsonObject>(JsonNode.Parse(File.ReadAllText(path)));
    }
}
