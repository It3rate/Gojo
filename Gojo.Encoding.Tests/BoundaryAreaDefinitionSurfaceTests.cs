using System.Text.Json.Nodes;
using Gojo.Core;
using Gojo.Encoding;

namespace Gojo.Encoding.Tests;

public sealed class BoundaryAreaDefinitionSurfaceTests
{
    [Fact]
    public void BandVariantLedgerBuildsPositiveAntiAndMixedLaneAreaReads()
    {
        var ledger = BoundaryAreaDefinitionSurface.CreateBandVariantLedger(new Carrier());

        Assert.True(ConceptDefinitionSurface.TryReadLedger(ledger, out var definitions));
        var rows = definitions
            .Select(ConceptDefinitionInspection.DefinitionJson)
            .ToDictionary(row => row.Name, StringComparer.Ordinal);

        Assert.Contains("source-boundary", rows.Keys);
        Assert.Contains("positive-terminal-boundary", rows.Keys);
        Assert.Contains("inverse-terminal-boundary", rows.Keys);

        AssertRow(
            rows["positive-area"],
            "Area+(positive-boundary-pair,positive-lane-evidence)",
            "positive-boundary-pair",
            "positive-lane-evidence");
        AssertRow(
            rows["anti-area"],
            "Area-(inverse-boundary-pair,inverse-lane-evidence)",
            "inverse-boundary-pair",
            "inverse-lane-evidence");
        AssertRow(
            rows["unsettled-area-read"],
            "Area?(positive-boundary-pair,mixed-lane-evidence)",
            "positive-boundary-pair",
            "mixed-lane-evidence");

        Assert.Equal("mixed lane read does not settle as area", rows["unsettled-area-read"].ResidueOrTension);
        Assert.Equal(
            "mixed lane leaves directional residue, not geometric diagonal area",
            rows["mixed-lane-residue"].ResidueOrTension);
        Assert.DoesNotContain(rows.Values, row => row.Body.Contains("Diag", StringComparison.Ordinal));
    }

    [Fact]
    public void BandVariantLedgerMatchesInspectableJsonFixture()
    {
        var generated = BandVariantFixtureEntry();
        var rewritten = GojoStructureEncodingJson.SerializeFixtureEntry(generated);
        var fixture = File.ReadAllText(BandVariantFixturePath());
        var reread = GojoStructureEncodingJson.DeserializeFixtureEntry(fixture);
        var rewrittenAgain = GojoStructureEncodingJson.SerializeFixtureEntry(reread);

        AssertJsonEquivalent(fixture, rewritten);
        AssertJsonEquivalent(rewritten, rewrittenAgain);
    }

    private static GojoEncodingJsonFixtureEntry BandVariantFixtureEntry()
    {
        var ledger = BoundaryAreaDefinitionSurface.CreateBandVariantLedger(new Carrier());
        Assert.True(ConceptDefinitionSurface.TryReadLedger(ledger, out var definitions));

        return new GojoEncodingJsonFixtureEntry
        {
            Represents = "A tiny band sample with source and terminal boundaries present together. Positive lanes settle as area, inverse lanes settle as anti-area, and mixed lanes remain residue instead of becoming a diagonal shape.",
            EntryId = "boundary-area-band-variants",
            Expression = "Bd0(S)+Bd1(Off(S,O(S),d))+Dir(...) -> Area+ | Area- | Area?",
            EncodingNote = "This is a primitive definition sample, not a glossary page. It keeps the band construction local so area and anti-area can crystallize from boundary/lane evidence.",
            Gojo = new GojoEncodingJsonFixtureGojo
            {
                Known =
                [
                    "anti-area-support",
                    "area-support",
                    "boundary",
                    "mixed-lane-residue",
                    "span"
                ],
                Paths =
                [
                    "source-boundary + positive-terminal-boundary + positive lanes --> positive-area",
                    "source-boundary + inverse-terminal-boundary + inverse lanes --> anti-area",
                    "source-boundary + positive-terminal-boundary + mixed lanes --> unresolved area residue"
                ],
                Tension =
                [
                    "mixed lane read does not settle as area",
                    "mixed lane leaves directional residue, not geometric diagonal area"
                ],
                Definitions = [.. definitions.Select(ConceptDefinitionInspection.DefinitionJson)]
            }
        };
    }

    private static void AssertRow(
        GojoEncodingJsonFixtureDefinition row,
        string body,
        params string[] dependencies)
    {
        Assert.Equal(body, row.Body);
        Assert.Equal(dependencies, row.DependsOn ?? []);
    }

    private static string BandVariantFixturePath() =>
        Path.Combine(
            AppContext.BaseDirectory,
            "Fixtures",
            "EncoderJson",
            "concept-definitions",
            "boundary-area-band-variants.json");

    private static void AssertJsonEquivalent(string expected, string actual)
    {
        var expectedNode = JsonNode.Parse(expected);
        var actualNode = JsonNode.Parse(actual);
        Assert.True(JsonNode.DeepEquals(expectedNode, actualNode), actual);
    }
}
