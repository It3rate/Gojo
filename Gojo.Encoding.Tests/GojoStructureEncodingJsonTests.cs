using System.Text.Json.Nodes;
using Gojo.Blocks;
using Gojo.Core;
using Gojo.Encoding;

namespace Gojo.Encoding.Tests;

public sealed class GojoStructureEncodingJsonTests
{
    [Fact]
    public void StructureEncodingJsonRoundTripsThroughText()
    {
        var x = new Carrier();
        var y = new Carrier();
        var sharedTick = new TickRef(7);
        var structure = new Relation(new CarrierLocation(x, sharedTick), new CarrierLocation(y, sharedTick));
        var encoded = GojoStructureEncodingSurface.Create(new Carrier(), "json/shared-tick", structure);

        var document = GojoStructureEncodingJson.StructureFromSurface(encoded);
        var json = GojoStructureEncodingJson.SerializeStructure(document);
        var deserialized = GojoStructureEncodingJson.DeserializeStructure(json);
        var rewritten = GojoStructureEncodingJson.SerializeStructure(deserialized);

        AssertJsonEquivalent(json, rewritten);
        Assert.Equal("json/shared-tick", deserialized.SourceId);
        Assert.Single(deserialized.TickRefs, row => row.Ticks == 7);
        Assert.True(deserialized.CarrierLocations.Count >= 2);
        Assert.Contains("\"tickRefs\"", json);
        Assert.Contains("\"kindName\": \"Relation\"", json);
    }

    [Fact]
    public void GlossaryBundleJsonRoundTripsThroughText()
    {
        var bundle = CreateResolutionTensionBundle();
        var document = GojoStructureEncodingJson.BundleFromSurface(bundle);
        var json = GojoStructureEncodingJson.Serialize(document);
        var deserialized = GojoStructureEncodingJson.DeserializeBundle(json);
        var rewritten = GojoStructureEncodingJson.Serialize(deserialized);

        AssertJsonEquivalent(json, rewritten);
        Assert.Equal("gojo.blocks/glossary/resolution-tension", deserialized.SourceId);
        Assert.Equal(6, deserialized.Entries.Count);
        Assert.All(deserialized.Entries, entry =>
        {
            Assert.Equal(entry.EntryId, entry.Structure.SourceId);
            Assert.Equal(2, entry.ObserverPaths.Count);
            Assert.NotEmpty(entry.Structure.Relations);
        });
        Assert.Contains("\"observerPaths\"", json);
        Assert.Contains("resolution-tension-closure-tension", json);
        Assert.Contains("relation table scan", json);
        var closure = Assert.Single(
            deserialized.Entries,
            entry => entry.EntryId == "resolution-tension-closure-tension");
        Assert.Equal("S(A,B)+S(B,C)+S(C,A') + close(A',A) -> Tension(delta)", closure.Expression);
    }

    [Fact]
    public void DeserializedGlossaryJsonRetainsReadableObserverPathShape()
    {
        var bundle = CreateResolutionTensionBundle();
        var document = GojoStructureEncodingJson.BundleFromSurface(bundle);
        var json = GojoStructureEncodingJson.Serialize(document);
        var deserialized = GojoStructureEncodingJson.DeserializeBundle(json);

        var closure = Assert.Single(
            deserialized.Entries,
            entry => entry.EntryId == "resolution-tension-closure-tension");
        var openedPath = closure.ObserverPaths[0];
        var rowPath = closure.ObserverPaths[1];

        Assert.Equal("text", openedPath.ObserverSurface.Basis?.Kind);
        Assert.Equal("opened glossary block", openedPath.ObserverSurface.Basis?.Text);
        Assert.Equal("text", openedPath.Steps[0].Through.Kind);
        Assert.Equal("opened structure", openedPath.Steps[0].Through.Text);
        Assert.Equal("text", rowPath.ObserverSurface.Basis?.Kind);
        Assert.Equal("relation table scan", rowPath.ObserverSurface.Basis?.Text);
        Assert.Equal("relation-row", rowPath.Result.Kind);
    }

    private static Relation CreateResolutionTensionBundle()
    {
        var entries = BlocksGlossaryExport.ResolutionTension()
            .Select(EncodeEntry)
            .Cast<IElement>()
            .ToArray();

        return GojoStructureEncodingBundleSurface.Create(
            new Carrier(),
            "gojo.blocks/glossary/resolution-tension",
            entries);
    }

    private static Relation EncodeEntry(BlocksGlossaryStructureExport export)
    {
        var raw = GojoStructureEncodingSurface.Create(new Carrier(), export.Id, export.Structure);
        Assert.True(GojoStructureEncodingSurface.TryRead(raw, out var rawRead));

        return GojoStructureEncodingBundleSurface.Entry(
            new Carrier(),
            export.Id,
            export.Expression,
            export.EncodingNote,
            raw,
            [
                OpenedStructurePath(export, raw, rawRead),
                FirstRelationRowPath(export, raw, rawRead)
            ]);
    }

    private static Relation OpenedStructurePath(
        BlocksGlossaryStructureExport export,
        Relation rawEncoding,
        GojoStructureEncodingSurface.StructureRead rawRead) =>
        GojoObserverPathSurface.Create(
            new Carrier(),
            rawEncoding,
            GojoObserverPathSurface.ObserverSurface(
                new Carrier(),
                rawEncoding,
                Label("opened glossary block")),
            GojoObserverPathSurface.StepFrame(
                new Carrier(),
                GojoObserverPathSurface.Step(
                    new Carrier(),
                    rawEncoding,
                    Label("opened structure"),
                    rawRead.OpenedStructure,
                    export.Structure)),
            rawRead.OpenedStructure,
            provenance: Label(export.Expression));

    private static Relation FirstRelationRowPath(
        BlocksGlossaryStructureExport export,
        Relation rawEncoding,
        GojoStructureEncodingSurface.StructureRead rawRead)
    {
        var row = rawRead.Relations.Length == 0
            ? rawRead.OpenedStructure
            : rawRead.Relations[0];

        return GojoObserverPathSurface.Create(
            new Carrier(),
            rawEncoding,
            GojoObserverPathSurface.ObserverSurface(
                new Carrier(),
                rawEncoding,
                Label("relation table scan")),
            GojoObserverPathSurface.StepFrame(
                new Carrier(),
                GojoObserverPathSurface.Step(
                    new Carrier(),
                    rawEncoding,
                    Label("first relation row"),
                    row,
                    rawRead.OpenedStructure)),
            row,
            provenance: Label(export.Id));
    }

    private static Relation Label(string text) =>
        EncodedTextSurface.Create(new Carrier(), text);

    private static void AssertJsonEquivalent(string expected, string actual)
    {
        var expectedNode = JsonNode.Parse(expected);
        var actualNode = JsonNode.Parse(actual);
        Assert.True(JsonNode.DeepEquals(expectedNode, actualNode), actual);
    }
}
