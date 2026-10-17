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
    public void BandAreaReadDiscoversSettledAndUnsettledAreaFromLocalDefinitions()
    {
        var ledger = BoundaryAreaDefinitionSurface.CreateBandVariantLedger(new Carrier());

        Assert.True(BoundaryAreaReadSurface.TryReadBandArea(ledger, out var read));
        Assert.Contains(read.Boundaries, boundary => boundary.Role == "source");
        Assert.Contains(read.Boundaries, boundary => boundary.Role == "terminal" && boundary.Extent == "+d");
        Assert.Contains(read.Boundaries, boundary => boundary.Role == "terminal" && boundary.Extent == "-d");
        Assert.Equal(2, read.BoundaryPairs.Length);
        Assert.Equal(3, read.LaneEvidence.Length);
        Assert.Contains(read.Areas, area => area.Settlement == "positive" && area.LaneEvidence.Lanes.All(lane => lane.Mark == "+f"));
        Assert.Contains(read.Areas, area => area.Settlement == "anti" && area.LaneEvidence.Lanes.All(lane => lane.Mark == "+i"));
        Assert.Contains(read.Areas, area => area.Settlement == "unsettled" && area.LaneEvidence.Lanes.Select(lane => lane.Mark).SequenceEqual(["+f", "-i"]));
        Assert.Contains(read.Residues, residue =>
            residue.Source.Settlement == "unsettled" &&
            residue.ResidueOrTension == "mixed lane leaves directional residue, not geometric diagonal area");
    }

    [Fact]
    public void BandAreaReadUsesStructureInsteadOfSpecificDefinitionNames()
    {
        var ledger = RenamedBandLedger();

        Assert.True(BoundaryAreaReadSurface.TryReadBandArea(ledger, out var read));
        Assert.Contains(read.Areas, area => area.Settlement == "positive");
        Assert.Contains(read.Areas, area => area.Settlement == "anti");
        Assert.Contains(read.Areas, area => area.Settlement == "unsettled");
        Assert.DoesNotContain(read.Areas.Select(area => area.Definition.Name), name => name is "positive-area" or "anti-area" or "unsettled-area-read");
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

    private static Relation RenamedBandLedger()
    {
        var definitions = new List<Relation>();

        Relation Add(
            string name,
            IElement body,
            IReadOnlyList<IElement>? dependencies = null,
            IElement? residueOrTension = null)
        {
            var definition = ConceptDefinitionSurface.Create(
                new Carrier(),
                name,
                body,
                dependencies,
                residueOrTension: residueOrTension);
            definitions.Add(definition);
            return definition;
        }

        var span = Add("seed", Relation.Segment(new Carrier(), 0, 1));
        var start = Add("first edge", Address("Bd0", Atom("seed")), [span]);
        var outlet = Add("escape", Relation.OrthogonalOutlet(Relation.Segment(new Carrier(), 0, 1)), [span]);

        var outward = Add("outward amount", Atom("+d"));
        var outwardOffset = Add("outward read", Address("Off", Atom("seed"), Atom("escape"), Atom("outward amount")), [span, outlet, outward]);
        var outwardEdge = Add("outer edge", Address("Bd1", Atom("outward read")), [outwardOffset]);
        var outwardPair = Add("outer pair", Relation.OrderedGroup(new Carrier(), start, outwardEdge), [start, outwardEdge]);
        var startForward = Add("start forward", Address("Dir", Atom("first edge"), Atom("+f")), [start]);
        var outerForward = Add("outer forward", Address("Dir", Atom("outer edge"), Atom("+f")), [outwardEdge]);
        var forwardEvidence = Add("forward evidence", Relation.OrderedGroup(new Carrier(), startForward, outerForward), [startForward, outerForward]);
        Add("filled result", Address("Area+", Atom("outer pair"), Atom("forward evidence")), [outwardPair, forwardEvidence]);

        var inward = Add("inward amount", Atom("-d"));
        var inwardOffset = Add("inward read", Address("Off", Atom("seed"), Atom("escape"), Atom("inward amount")), [span, outlet, inward]);
        var inwardEdge = Add("inner edge", Address("Bd1", Atom("inward read")), [inwardOffset]);
        var inwardPair = Add("inner pair", Relation.OrderedGroup(new Carrier(), start, inwardEdge), [start, inwardEdge]);
        var startInverse = Add("start inverse", Address("Dir", Atom("first edge"), Atom("+i")), [start]);
        var innerInverse = Add("inner inverse", Address("Dir", Atom("inner edge"), Atom("+i")), [inwardEdge]);
        var inverseEvidence = Add("inverse evidence", Relation.OrderedGroup(new Carrier(), startInverse, innerInverse), [startInverse, innerInverse]);
        Add("void result", Address("Area-", Atom("inner pair"), Atom("inverse evidence")), [inwardPair, inverseEvidence]);

        var outerAgainst = Add("outer against", Address("Dir", Atom("outer edge"), Atom("-i")), [outwardEdge]);
        var mixedEvidence = Add("cross evidence", Relation.OrderedGroup(new Carrier(), startForward, outerAgainst), [startForward, outerAgainst]);
        var unsettled = Add(
            "not area result",
            Address("Area?", Atom("outer pair"), Atom("cross evidence")),
            [outwardPair, mixedEvidence],
            Text("mixed lane read does not settle as area"));
        Add(
            "leftover read",
            Address("Residue", Atom("not area result")),
            [unsettled],
            Text("mixed lane leaves directional residue, not geometric diagonal area"));

        return ConceptDefinitionSurface.Ledger(new Carrier(), definitions);
    }

    private static Relation Address(string lexical, params IElement[] parts) =>
        ConceptAddressSurface.Compound(new Carrier(), lexical, parts);

    private static Relation Atom(string name) =>
        ConceptAddressSurface.Atom(new Carrier(), name);

    private static Relation Text(string text) =>
        EncodedTextSurface.Create(new Carrier(), text);

    private static void AssertJsonEquivalent(string expected, string actual)
    {
        var expectedNode = JsonNode.Parse(expected);
        var actualNode = JsonNode.Parse(actual);
        Assert.True(JsonNode.DeepEquals(expectedNode, actualNode), actual);
    }
}
