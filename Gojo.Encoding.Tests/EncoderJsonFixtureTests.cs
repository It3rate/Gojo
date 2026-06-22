using System.Text.Json.Nodes;
using Gojo.Blocks;
using Gojo.Encoding;

namespace Gojo.Encoding.Tests;

public sealed class EncoderJsonFixtureTests
{
    [Fact]
    public void FirstGlossaryFixtureFilesAreStrictJsonAndRoundTripThroughEncodingDtos()
    {
        var files = FixturePaths();

        Assert.Equal(10, files.Length);
        foreach (var file in files)
        {
            var json = File.ReadAllText(file);
            var originalNode = JsonNode.Parse(json);
            var entry = GojoStructureEncodingJson.DeserializeFixtureEntry(json);
            var rewritten = GojoStructureEncodingJson.SerializeFixtureEntry(entry);
            var reread = GojoStructureEncodingJson.DeserializeFixtureEntry(rewritten);
            var rewrittenAgain = GojoStructureEncodingJson.SerializeFixtureEntry(reread);

            Assert.NotNull(originalNode);
            Assert.IsType<JsonObject>(originalNode);
            AssertJsonEquivalent(rewritten, rewrittenAgain);
        }

        var entries = ReadFixtureEntries();
        Assert.All(entries, entry =>
        {
            Assert.False(string.IsNullOrWhiteSpace(entry.Represents));
            Assert.NotEmpty(entry.Gojo.Known ?? []);
        });
    }

    [Fact]
    public void FirstGlossaryFixtureFilesMatchLiveGlossaryExport()
    {
        var entries = ReadFixtureEntries();
        var expected = ExpectedExports();

        Assert.Equal(expected.Select(item => item.Id).ToArray(), entries.Select(item => item.EntryId).ToArray());

        for (var index = 0; index < expected.Length; index++)
        {
            Assert.Equal(expected[index].Expression, entries[index].Expression);
            Assert.Equal(expected[index].EncodingNote ?? string.Empty, entries[index].EncodingNote ?? string.Empty);
        }
    }

    [Fact]
    public void FirstGlossaryFixtureFilesMatchGeneratedLiveGojoProjection()
    {
        var fixtures = ReadFixtureEntries();
        var expected = ExpectedExports().ToDictionary(item => item.Id, StringComparer.Ordinal);

        foreach (var fixture in fixtures)
        {
            var export = expected[fixture.EntryId];
            var generated = GojoStructureEncodingFixtureProjection.EntryFromStructure(
                new GojoStructureEncodingFixtureProjection.Source(
                    export.Id,
                    fixture.Represents ?? string.Empty,
                    export.Expression,
                    export.EncodingNote,
                    export.Structure));

            Assert.Equal(generated.Gojo.Known ?? [], fixture.Gojo.Known ?? []);
            Assert.Equal(generated.Gojo.Paths ?? [], fixture.Gojo.Paths ?? []);
            Assert.Equal(generated.Gojo.Tension ?? [], fixture.Gojo.Tension ?? []);
        }
    }

    [Fact]
    public void GeneratedFixtureProjectionKeepsRealGojoSurfaceReadable()
    {
        foreach (var export in ExpectedExports())
        {
            var surface = GojoStructureEncodingFixtureProjection.DiagnosticEntrySurfaceFromStructure(
                new GojoStructureEncodingFixtureProjection.Source(
                    export.Id,
                    string.Empty,
                    export.Expression,
                    export.EncodingNote,
                    export.Structure));

            Assert.True(GojoStructureEncodingBundleSurface.TryReadEntry(surface, out var entryRead));
            Assert.True(GojoStructureEncodingSurface.TryRead(entryRead.StructureEncoding, out var structureRead));
            Assert.Same(export.Structure, structureRead.OpenedStructure);
            Assert.Equal(export.Id, entryRead.EntryId);
            Assert.Equal(2, entryRead.ObserverPaths.Length);
        }
    }

    [Fact]
    public void FixtureFilesAreSingleEntryObjectsNotPageBundles()
    {
        foreach (var file in FixturePaths())
        {
            var root = Assert.IsType<JsonObject>(JsonNode.Parse(File.ReadAllText(file)));
            Assert.True(root.ContainsKey("entryId"), file);
            Assert.True(root.ContainsKey("gojo"), file);
            Assert.False(root.ContainsKey("structure"), file);
            Assert.False(root.ContainsKey("observerPaths"), file);
            Assert.False(root.ContainsKey("readPaths"), file);
            Assert.False(root.ContainsKey("entries"), file);

            var entryId = root["entryId"]?.GetValue<string>();
            Assert.False(string.IsNullOrWhiteSpace(entryId), file);
        }
    }

    [Fact]
    public void FirstGlossaryExportsUseDistinctSourceStructures()
    {
        var exports = ExpectedExports();

        for (var left = 0; left < exports.Length; left++)
        {
            for (var right = left + 1; right < exports.Length; right++)
            {
                Assert.NotSame(exports[left].Structure, exports[right].Structure);
            }
        }
    }

    [Fact]
    public void FirstGlossaryFixtureFilesKeepUnresolvedInformationVisibleWherePresent()
    {
        var entries = ReadFixtureEntries();

        Assert.Contains(entries, entry =>
            entry.EntryId == "resolution-tension-unspecified-site" &&
            (entry.Gojo.Tension ?? []).Contains("unresolved change remains inspectable"));
        Assert.Contains(entries, entry =>
            entry.EntryId == "resolution-tension-failed-read" &&
            (entry.Gojo.Tension ?? []).Contains("read residue preserved"));
        Assert.Contains(entries, entry =>
            entry.EntryId == "site-flow-boundary-radial-flow" &&
            (entry.Gojo.Tension ?? []).Contains("no preferred direction"));
    }

    [Fact]
    public void FixtureFilesDoNotExposeStorageInventory()
    {
        foreach (var file in FixturePaths())
        {
            var json = File.ReadAllText(file);

            Assert.DoesNotContain("\"observerPaths\"", json);
            Assert.DoesNotContain("\"readPaths\"", json);
            Assert.DoesNotContain("\"counts\"", json);
            Assert.DoesNotContain("\"roots\"", json);
            Assert.DoesNotContain("\"carriers\"", json);
            Assert.DoesNotContain("\"tickRefs\"", json);
            Assert.DoesNotContain("\"carrierLocations\"", json);
            Assert.DoesNotContain("\"relations\"", json);
            Assert.DoesNotContain("\"unitShapes\"", json);
            Assert.DoesNotContain("\"relationRow\"", json);
            Assert.DoesNotContain("\"carrierRow\"", json);
            Assert.DoesNotContain("\"kindName\"", json);
            Assert.DoesNotContain("\"type\"", json);
            Assert.DoesNotContain("\"index\": 0", json);
            Assert.DoesNotContain("\"isInverse\": false", json);
            Assert.DoesNotContain("\"isUnknown\": false", json);
            Assert.DoesNotContain("\"value\": 0", json);
            Assert.DoesNotContain("\"tension\": 0", json);
            Assert.DoesNotContain("\"inverseCarrierIndex\"", json);
            Assert.DoesNotContain("\"unitShapeIndex\"", json);
            Assert.DoesNotContain("\"carrierIndex\"", json);
            Assert.DoesNotContain("\"tickRefIndex\"", json);
        }
    }

    [Fact]
    public void RadialFlowFixtureStaysSparseAndConceptual()
    {
        var file = Path.Combine(FixtureDirectory(), "008-site-flow-boundary-radial-flow.json");
        var json = File.ReadAllText(file);
        var entry = GojoStructureEncodingJson.DeserializeFixtureEntry(json);

        Assert.True(File.ReadAllLines(file).Length <= 24, json);
        Assert.Equal(["position"], entry.Gojo.Known ?? []);
        Assert.Contains("position --open normal--> radial-flow", entry.Gojo.Paths ?? []);
        Assert.Contains("no preferred direction", entry.Gojo.Tension ?? []);
    }

    private static GojoEncodingJsonFixtureEntry[] ReadFixtureEntries() =>
        [.. FixturePaths()
            .Select(file => GojoStructureEncodingJson.DeserializeFixtureEntry(File.ReadAllText(file)))];

    private static string[] FixturePaths() =>
        [.. Directory
            .GetFiles(FixtureDirectory(), "*.json")
            .Order(StringComparer.Ordinal)];

    private static string FixtureDirectory() =>
        Path.Combine(AppContext.BaseDirectory, "Fixtures", "EncoderJson", "first-10-glossary-objects");

    private static BlocksGlossaryStructureExport[] ExpectedExports() =>
        [.. BlocksGlossaryExport.ResolutionTension()
            .Concat(BlocksGlossaryExport.Structures("site-flow-boundary").Take(4))];

    private static void AssertJsonEquivalent(string expected, string actual)
    {
        var expectedNode = JsonNode.Parse(expected);
        var actualNode = JsonNode.Parse(actual);
        Assert.True(JsonNode.DeepEquals(expectedNode, actualNode), actual);
    }
}
