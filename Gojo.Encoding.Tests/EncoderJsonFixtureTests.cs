using System.Text.Json.Nodes;
using Gojo.Blocks;
using Gojo.Encoding;

namespace Gojo.Encoding.Tests;

public sealed class EncoderJsonFixtureTests
{
    [Fact]
    public void GlossaryFixtureFilesAreStrictJsonAndRoundTripThroughEncodingDtos()
    {
        var files = FixturePaths();
        var expected = ExpectedExports();

        Assert.Equal(expected.Length, files.Length);
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
    public void GlossaryFixtureFilesMatchLiveGlossaryExport()
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
    public void GlossaryFixtureFilesMatchGeneratedLiveGojoProjection()
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
    public void GlossaryFixtureIdsAreUniqueAndComplete()
    {
        var entries = ReadFixtureEntries();
        var expected = ExpectedExports();

        Assert.Equal(expected.Length, entries.Select(entry => entry.EntryId).Distinct(StringComparer.Ordinal).Count());
        Assert.Equal(expected.Select(item => item.Id), entries.Select(entry => entry.EntryId));
    }

    [Fact]
    public void GlossaryFixtureFilesKeepUnresolvedInformationVisibleWherePresent()
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
        Assert.Equal(["flow", "position"], entry.Gojo.Known ?? []);
        Assert.Contains("position --open flow--> flow", entry.Gojo.Paths ?? []);
        Assert.DoesNotContain("radial-flow", entry.Gojo.Paths ?? []);
        Assert.Contains("no preferred direction", entry.Gojo.Tension ?? []);
        Assert.Contains("flow shape depends on available dimension", entry.Gojo.Tension ?? []);
    }

    [Fact]
    public void LinearPotentialFixtureKeepsSharedReferenceSeparateFromSpan()
    {
        var file = Path.Combine(FixtureDirectory(), "009-site-flow-boundary-linear-potential.json");
        var json = File.ReadAllText(file);
        var entry = GojoStructureEncodingJson.DeserializeFixtureEntry(json);

        Assert.Equal(["flow", "position", "shared-reference"], entry.Gojo.Known ?? []);
        Assert.DoesNotContain("span", entry.Gojo.Known ?? []);
        Assert.DoesNotContain("coincident-span", entry.Gojo.Known ?? []);
        Assert.Contains("shared-reference --open basis read--> unresolved carrier potential", entry.Gojo.Paths ?? []);
        Assert.Contains("unresolved carrier potential --open flow--> flow", entry.Gojo.Paths ?? []);
        Assert.Contains("basis orientation unresolved", entry.Gojo.Tension ?? []);
        Assert.Contains("length unresolved", entry.Gojo.Tension ?? []);
        Assert.Contains("opposed lane not asserted", entry.Gojo.Tension ?? []);
    }

    [Fact]
    public void MeasuredBoundaryFixtureKeepsRadiusEvidenceSeparateFromContourReads()
    {
        var file = Path.Combine(FixtureDirectory(), "010-site-flow-boundary-measured-boundary.json");
        var json = File.ReadAllText(file);
        var entry = GojoStructureEncodingJson.DeserializeFixtureEntry(json);

        Assert.Equal(["circle-boundary", "position", "radius-evidence"], entry.Gojo.Known ?? []);
        Assert.Contains("position + radius-evidence --> circle-boundary", entry.Gojo.Paths ?? []);
        Assert.DoesNotContain("position + radius-position --> circle-boundary", entry.Gojo.Paths ?? []);
        Assert.Contains("contour traversal not selected", entry.Gojo.Tension ?? []);
        Assert.Contains("inside/outside support not selected", entry.Gojo.Tension ?? []);
    }

    [Fact]
    public void FlowShapeIsNotEncodedAsAPrimitivePathMarker()
    {
        foreach (var entry in ReadFixtureEntries())
        {
            var paths = entry.Gojo.Paths ?? [];

            Assert.DoesNotContain(paths, path => path.Contains("radial-flow", StringComparison.Ordinal));
            Assert.DoesNotContain(paths, path => path.Contains("linear-potential", StringComparison.Ordinal));
        }
    }

    [Fact]
    public void FlowDirectionPathsRequireLocalFlowKnowledge()
    {
        foreach (var entry in ReadFixtureEntries())
        {
            var paths = entry.Gojo.Paths ?? [];
            var hasFlowDirectionPath = paths.Any(path =>
                path.StartsWith("flow --direction read-->", StringComparison.Ordinal));

            if (!hasFlowDirectionPath)
            {
                continue;
            }

            Assert.Contains("flow", entry.Gojo.Known ?? []);
        }
    }

    private static GojoEncodingJsonFixtureEntry[] ReadFixtureEntries() =>
        [.. FixturePaths()
            .Select(file => GojoStructureEncodingJson.DeserializeFixtureEntry(File.ReadAllText(file)))];

    private static string[] FixturePaths() =>
        [.. Directory
            .GetFiles(FixtureDirectory(), "*.json")
            .Order(StringComparer.Ordinal)];

    private static string FixtureDirectory() =>
        Path.Combine(AppContext.BaseDirectory, "Fixtures", "EncoderJson", "glossary-objects");

    private static BlocksGlossaryStructureExport[] ExpectedExports() =>
        [.. BlocksGlossaryExport.Structures()];

    private static void AssertJsonEquivalent(string expected, string actual)
    {
        var expectedNode = JsonNode.Parse(expected);
        var actualNode = JsonNode.Parse(actual);
        Assert.True(JsonNode.DeepEquals(expectedNode, actualNode), actual);
    }
}
