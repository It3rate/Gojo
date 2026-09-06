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
            var generated = GojoStructureEncodingFixtureProjection.EntryFromTeachingExample(
                new GojoStructureEncodingFixtureProjection.Source(
                    export.Id,
                    fixture.Represents ?? string.Empty,
                    export.Expression,
                    export.EncodingNote,
                    export.Structure));

            Assert.Equal(generated.Gojo.Known ?? [], fixture.Gojo.Known ?? []);
            Assert.Equal(generated.Gojo.Paths ?? [], fixture.Gojo.Paths ?? []);
            Assert.Equal(generated.Gojo.Tension ?? [], fixture.Gojo.Tension ?? []);
            Assert.Equal(DefinitionRows(generated.Gojo), DefinitionRows(fixture.Gojo));
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

        Assert.True(File.ReadAllLines(file).Length <= 42, json);
        Assert.Equal(["flow", "position"], entry.Gojo.Known ?? []);
        Assert.Contains("position --orthogonal outlet--> flow", entry.Gojo.Paths ?? []);
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
        Assert.DoesNotContain("collapsed-basis", entry.Gojo.Known ?? []);
        Assert.DoesNotContain("span", entry.Gojo.Known ?? []);
        Assert.DoesNotContain("coincident-span", entry.Gojo.Known ?? []);
        Assert.Contains("shared-reference --open basis read--> unresolved carrier potential", entry.Gojo.Paths ?? []);
        Assert.Contains("unresolved carrier potential --orthogonal outlet--> flow", entry.Gojo.Paths ?? []);
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

        Assert.Equal(["boundary", "circle-boundary", "position", "radius-evidence"], entry.Gojo.Known ?? []);
        Assert.Contains("circle-boundary --self boundary read--> BdSelf(C)", entry.Gojo.Paths ?? []);
        Assert.Contains("position + orthogonal outlet + radius-evidence --> radial terminal boundary", entry.Gojo.Paths ?? []);
        Assert.Contains("position + radius-evidence --> circle-boundary", entry.Gojo.Paths ?? []);
        Assert.DoesNotContain("position + radius-position --> circle-boundary", entry.Gojo.Paths ?? []);
        Assert.Contains("contour traversal not selected", entry.Gojo.Tension ?? []);
        Assert.Contains("inside/outside support not selected", entry.Gojo.Tension ?? []);
    }

    [Fact]
    public void ContextualBoundaryAreaAndOffsetFormsArePhysicallyRepresented()
    {
        var offset = ReadFixtureEntry("146-boolean-offset-offset-reads.json");

        Assert.Contains("span --source boundary read--> Bd0(S)", offset.Gojo.Paths ?? []);
        Assert.Contains("source + orthogonal outlet + extent --> offset-read", offset.Gojo.Paths ?? []);
        Assert.Contains("offset-read --terminal boundary read--> Bd1(Off)", offset.Gojo.Paths ?? []);
        Assert.Contains("paired offset reads --lane compatibility read--> area or tension", offset.Gojo.Paths ?? []);
        Assert.Contains("offset lane compatibility unresolved", offset.Gojo.Tension ?? []);

        var circleSupport = ReadFixtureEntry("012-site-flow-boundary-boundary-support.json");
        var circleDefinitions = DefinitionNames(circleSupport.Gojo);

        Assert.Contains("self-boundary", circleDefinitions);
        Assert.Contains("boundary-side", circleDefinitions);
        Assert.Contains("positive-area", circleDefinitions);

        var unionHole = ReadFixtureEntry("148-boolean-offset-union-hole.json");
        var unionDefinitions = DefinitionNames(unionHole.Gojo);

        Assert.Contains("compatible positive boundaries --> Area+", unionHole.Gojo.Paths ?? []);
        Assert.Contains("compatible inverse boundaries --> Area-", unionHole.Gojo.Paths ?? []);
        Assert.Contains("positive area + anti-area --> support with holes", unionHole.Gojo.Paths ?? []);
        Assert.Contains("closed path --boundary read--> Bd(Path)", unionHole.Gojo.Paths ?? []);
        Assert.Contains("selection --local focus read--> selected structure + residue", unionHole.Gojo.Paths ?? []);
        Assert.Contains("source-boundary", unionDefinitions);
        Assert.Contains("closed-boundary", unionDefinitions);
        Assert.Contains("positive-lane-evidence", unionDefinitions);
        Assert.Contains("positive-area", unionDefinitions);
        Assert.Contains("inverse-boundary", unionDefinitions);
        Assert.Contains("inverse-lane-evidence", unionDefinitions);
        Assert.Contains("anti-area", unionDefinitions);
        Assert.Contains("support-with-hole", unionDefinitions);
    }

    [Fact]
    public void DefinitionSamplesSupportBlindShapeReconstruction()
    {
        var cases = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["001-resolution-tension-resolution-read.json"] = "resolution read over a span with lower detail open",
            ["006-resolution-tension-settled-read.json"] = "settled read from source through basis and resolution",
            ["007-site-flow-boundary-site.json"] = "single position participation",
            ["008-site-flow-boundary-radial-flow.json"] = "point-sourced flow with unresolved dimension or direction",
            ["009-site-flow-boundary-linear-potential.json"] = "shared-reference carrier potential with unresolved flow",
            ["012-site-flow-boundary-boundary-support.json"] = "circle boundary with area support",
            ["019-units-ticks-internal-partition.json"] = "partitioned span producing readable sites",
            ["020-units-ticks-unit-comparison.json"] = "unit comparison read with preserved residue",
            ["146-boolean-offset-offset-reads.json"] = "paired offset boundary read with unresolved lane compatibility",
            ["148-boolean-offset-union-hole.json"] = "positive area with anti-area hole support"
        };

        foreach (var (fixtureName, expected) in cases)
        {
            var entry = ReadFixtureEntry(fixtureName);
            var read = BlindShapeFrom(entry.Gojo);

            Assert.Equal(expected, read.Primary);
            Assert.NotEmpty(read.Evidence);
        }
    }

    [Fact]
    public void CurveSelectionCrystallizationAndRuntimeGapsStayVisible()
    {
        var curve = ReadFixtureEntry("080-curves-line-line-arc-blend.json");

        Assert.Contains("arc evidence --curve read--> constant curvature placeholder", curve.Gojo.Paths ?? []);
        Assert.Contains("curve --acceleration footing--> unresolved", curve.Gojo.Paths ?? []);
        Assert.Contains("tangency evidence --settlement read--> smoothing bridge", curve.Gojo.Paths ?? []);
        Assert.Contains("curve acceleration footing unresolved", curve.Gojo.Tension ?? []);

        var crystallized = ReadFixtureEntry("153-crystallization-provenance-crystallized-element.json");

        Assert.Contains("read --crystallize--> reusable structure", crystallized.Gojo.Paths ?? []);
        Assert.Contains("crystallization provenance must remain inspectable", crystallized.Gojo.Tension ?? []);

        var runtime = ReadFixtureEntry("199-repetition-timeline-feedback.json");

        Assert.Contains("runtime pass --records--> residue + tension", runtime.Gojo.Paths ?? []);
        Assert.Contains("runtime trace --crystallize--> behavior witness", runtime.Gojo.Paths ?? []);
        Assert.Contains("runtime/static distinction must remain explicit", runtime.Gojo.Tension ?? []);
    }

    [Fact]
    public void MixedLaneDiagonalRequiresExplicitMixedLaneEvidence()
    {
        var diagonalGeometry = ReadFixtureEntry("173-radial-crescent-outline-experiment-full.json");

        Assert.DoesNotContain("mixed lane boundaries --> diagonal tension", diagonalGeometry.Gojo.Paths ?? []);
        Assert.DoesNotContain("area mixed-lane read does not settle as area", diagonalGeometry.Gojo.Tension ?? []);
    }

    [Fact]
    public void FixtureVocabularyDoesNotKeepOutdatedDebugTerms()
    {
        foreach (var file in FixturePaths())
        {
            var json = File.ReadAllText(file);

            Assert.DoesNotContain("coincident-span", json);
            Assert.DoesNotContain("flow_radial", json);
            Assert.DoesNotContain("flow_perp", json);
            Assert.DoesNotContain("radius-position", json);
            Assert.DoesNotContain("--open flow-->", json);
        }
    }

    [Theory]
    [InlineData("028-basis-frames-orthogonal-basis.json")]
    [InlineData("044-source-support-linear-flow.json")]
    [InlineData("053-grades-dimensions-segment-normal.json")]
    [InlineData("054-grades-dimensions-point-pair-normal.json")]
    [InlineData("068-direction-area-linear-boundary-support.json")]
    [InlineData("074-face-normal-segment-sides.json")]
    public void CollapsedBasisNormalFixturesUseOrthogonalOutletRead(string fixtureName)
    {
        var file = Path.Combine(FixtureDirectory(), fixtureName);
        var entry = GojoStructureEncodingJson.DeserializeFixtureEntry(File.ReadAllText(file));

        Assert.Contains("collapsed-basis", entry.Gojo.Known ?? []);
        Assert.Contains("flow", entry.Gojo.Known ?? []);
        Assert.Contains("collapsed-basis --orthogonal outlet--> flow", entry.Gojo.Paths ?? []);
        Assert.Contains("length unresolved", entry.Gojo.Tension ?? []);
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

    private static GojoEncodingJsonFixtureEntry ReadFixtureEntry(string fixtureName) =>
        GojoStructureEncodingJson.DeserializeFixtureEntry(File.ReadAllText(Path.Combine(FixtureDirectory(), fixtureName)));

    private static string[] DefinitionRows(GojoEncodingJsonFixtureGojo gojo) =>
        [.. (gojo.Definitions ?? [])
            .Select(definition =>
                string.Join(
                    "|",
                    definition.Name,
                    definition.Encoding,
                    definition.Body,
                    string.Join(",", definition.DependsOn ?? []),
                    definition.Provenance ?? string.Empty,
                    definition.ResidueOrTension ?? string.Empty))];

    private static HashSet<string> DefinitionNames(GojoEncodingJsonFixtureGojo gojo) =>
        (gojo.Definitions ?? [])
            .Select(definition => definition.Name)
            .ToHashSet(StringComparer.Ordinal);

    private readonly record struct BlindShapeRead(string Primary, string[] Evidence);

    private static BlindShapeRead BlindShapeFrom(GojoEncodingJsonFixtureGojo gojo)
    {
        var definitions = (gojo.Definitions ?? [])
            .Select(definition => definition.Name)
            .ToHashSet(StringComparer.Ordinal);
        var paths = (gojo.Paths ?? []).ToHashSet(StringComparer.Ordinal);
        var tension = (gojo.Tension ?? []).ToHashSet(StringComparer.Ordinal);

        if (definitions.Contains("anti-area-support"))
        {
            return Shape(
                "positive area with anti-area hole support",
                definitions,
                "source-boundary",
                "closed-boundary",
                "boundary",
                "selection",
                "positive-lane-evidence",
                "positive-area",
                "area-support",
                "inverse-boundary",
                "inverse-lane-evidence",
                "anti-area",
                "anti-area-support",
                "support-with-hole");
        }

        if (definitions.Contains("unit-comparison"))
        {
            return Shape("unit comparison read with preserved residue", definitions, "span", "unit-span", "comparison", "unit", "unit-comparison");
        }

        if (definitions.Contains("offset-read") &&
            tension.Contains("offset lane compatibility unresolved"))
        {
            return Shape("paired offset boundary read with unresolved lane compatibility", definitions, "span", "boundary", "offset-read");
        }

        if (definitions.Contains("circle-boundary") &&
            definitions.Contains("area-support"))
        {
            return Shape(
                "circle boundary with area support",
                definitions,
                "position",
                "radius-evidence",
                "circle-boundary",
                "self-boundary",
                "boundary",
                "boundary-side",
                "positive-area",
                "area-support");
        }

        if (definitions.Contains("partition-site"))
        {
            return Shape("partitioned span producing readable sites", definitions, "span", "partition-count", "partition", "partition-site");
        }

        if (definitions.Contains("unresolved-carrier-potential"))
        {
            return Shape("shared-reference carrier potential with unresolved flow", definitions, "position", "shared-reference", "unresolved-carrier-potential", "flow");
        }

        if (definitions.Contains("settled-read"))
        {
            return Shape("settled read from source through basis and resolution", definitions, "source", "basis", "resolution", "settled-read");
        }

        if (definitions.Contains("resolution-read"))
        {
            return Shape("resolution read over a span with lower detail open", definitions, "span", "resolution", "resolution-read");
        }

        if (definitions.Contains("flow") &&
            definitions.Contains("position") &&
            paths.Contains("position --orthogonal outlet--> flow"))
        {
            return Shape("point-sourced flow with unresolved dimension or direction", definitions, "position", "flow");
        }

        if (definitions.SetEquals(["position"]))
        {
            return Shape("single position participation", definitions, "position");
        }

        return Shape("unreconstructed", definitions);
    }

    private static BlindShapeRead Shape(
        string primary,
        IReadOnlySet<string> availableDefinitions,
        params string[] expectedEvidence)
    {
        var evidence = expectedEvidence.Length == 0
            ? availableDefinitions.Order(StringComparer.Ordinal).ToArray()
            : [.. expectedEvidence.Where(availableDefinitions.Contains)];

        return new BlindShapeRead(primary, evidence);
    }

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
