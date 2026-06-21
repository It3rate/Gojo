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
            var entry = GojoStructureEncodingJson.DeserializeEntry(json);
            var rewritten = GojoStructureEncodingJson.SerializeEntry(entry);
            var reread = GojoStructureEncodingJson.DeserializeEntry(rewritten);
            var rewrittenAgain = GojoStructureEncodingJson.SerializeEntry(reread);

            Assert.NotNull(originalNode);
            Assert.IsType<JsonObject>(originalNode);
            AssertJsonEquivalent(rewritten, rewrittenAgain);
        }

        var entries = ReadFixtureEntries();
        Assert.All(entries, entry =>
        {
            Assert.False(string.IsNullOrWhiteSpace(entry.Represents));
            Assert.Equal(entry.EntryId, entry.Structure.SourceId);
            Assert.NotEmpty(entry.ObserverPaths);
            Assert.NotEmpty(entry.ReadPaths);
            Assert.Contains(
                entry.ObserverPaths,
                path => !string.IsNullOrWhiteSpace(path.ObserverSurface.Basis?.Text));
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

            Assert.Equal(generated.Structure.Counts, fixture.Structure.Counts);
            Assert.Equal(
                generated.ObserverPaths.Select(path => path.ObserverSurface.Basis?.Text).ToArray(),
                fixture.ObserverPaths.Select(path => path.ObserverSurface.Basis?.Text).ToArray());
            Assert.Equal(
                generated.ObserverPaths.Select(path => path.Steps.Single().Through.Text).ToArray(),
                fixture.ObserverPaths.Select(path => path.Steps.Single().Through.Text).ToArray());
            Assert.Equal(
                generated.ObserverPaths.Select(path => path.Result.Kind).ToArray(),
                fixture.ObserverPaths.Select(path => path.Result.Kind).ToArray());
            Assert.Equal(
                generated.ReadPaths.Select(path => path.Path).ToArray(),
                fixture.ReadPaths.Select(path => path.Path).ToArray());
        }
    }

    [Fact]
    public void GeneratedFixtureProjectionKeepsRealGojoSurfaceReadable()
    {
        foreach (var export in ExpectedExports())
        {
            var surface = GojoStructureEncodingFixtureProjection.EntrySurfaceFromStructure(
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
            Assert.True(root.ContainsKey("structure"), file);
            Assert.False(root.ContainsKey("entries"), file);

            var entryId = root["entryId"]?.GetValue<string>();
            var structureId = root["structure"]?["sourceId"]?.GetValue<string>();
            Assert.False(string.IsNullOrWhiteSpace(entryId), file);
            Assert.Equal(entryId, structureId);
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
    public void FirstGlossaryFixtureFilesKeepTensionVisibleInEveryEntry()
    {
        var entries = ReadFixtureEntries();

        Assert.All(entries, entry =>
        {
            Assert.Equal("unknown-point", entry.ResidueOrTension.Kind);
            Assert.True(entry.ResidueOrTension.IsUnknown);
            Assert.Equal("unknown-point", entry.Structure.ResidueOrTension.Kind);
            Assert.All(entry.ObserverPaths, path =>
            {
                Assert.Equal("unknown-point", path.ResidueOrTension.Kind);
                Assert.True(path.ResidueOrTension.IsUnknown);
            });
        });
    }

    [Fact]
    public void FixtureRelationDescriptionsDoNotExposeRecursiveCarrierCounts()
    {
        foreach (var file in FixturePaths())
        {
            var root = JsonNode.Parse(File.ReadAllText(file));
            Assert.NotNull(root);

            foreach (var relation in RelationDescriptions(root))
            {
                Assert.False(relation.ContainsKey("counts"), file);
            }
        }
    }

    [Fact]
    public void FixtureFilesOmitDefaultAndRawStorageNoise()
    {
        foreach (var file in FixturePaths())
        {
            var json = File.ReadAllText(file);

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
    public void FixtureFilesCarryCompactReadPathProjection()
    {
        foreach (var entry in ReadFixtureEntries())
        {
            Assert.Equal(entry.ObserverPaths.Count, entry.ReadPaths.Count);
            Assert.Contains(entry.ReadPaths, path =>
                path.Basis == "opened glossary block" &&
                path.Path == $"structure({entry.EntryId}) --opened structure--> relation" &&
                path.Result == "relation" &&
                path.ResidueOrTension == "unknown");
            Assert.Contains(entry.ReadPaths, path =>
                path.Basis == "relation table scan" &&
                path.Path.StartsWith($"structure({entry.EntryId}) --first relation row--> relation#1(", StringComparison.Ordinal));
        }
    }

    private static IEnumerable<JsonObject> RelationDescriptions(JsonNode? node)
    {
        if (node is JsonObject obj)
        {
            if (obj.TryGetPropertyValue("kind", out var kind) &&
                kind?.ToJsonString() == "\"relation\"")
            {
                yield return obj;
            }

            foreach (var child in obj.Select(pair => pair.Value))
            {
                foreach (var relation in RelationDescriptions(child))
                {
                    yield return relation;
                }
            }
        }
        else if (node is JsonArray array)
        {
            foreach (var child in array)
            {
                foreach (var relation in RelationDescriptions(child))
                {
                    yield return relation;
                }
            }
        }
    }

    private static GojoEncodingJsonBundleEntry[] ReadFixtureEntries() =>
        [.. FixturePaths()
            .Select(file => GojoStructureEncodingJson.DeserializeEntry(File.ReadAllText(file)))];

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
