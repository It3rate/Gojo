using System.Text.Json.Nodes;
using Gojo.Core;
using Gojo.Encoding;

namespace Gojo.Encoding.Tests;

public sealed class ConceptCompositionDefinitionSurfaceTests
{
    [Fact]
    public void CompositionLedgerBuildsCommonNounsFromEarlierPieces()
    {
        var ledger = ConceptCompositionDefinitionSurface.CreateNounCompositionLedger(new Carrier());

        Assert.True(ConceptDefinitionSurface.TryReadLedger(ledger, out var definitions));
        var rows = definitions
            .Select(ConceptDefinitionInspection.DefinitionJson)
            .ToDictionary(row => row.Name, StringComparer.Ordinal);

        AssertRow(
            rows["margin-area"],
            "Area+(margin-band,margin-lane-evidence)",
            "margin-band",
            "margin-lane-evidence");
        AssertRow(
            rows["annulus-support"],
            "Support(ring-area,circle-hole)",
            "ring-area",
            "circle-hole");
        AssertRow(
            rows["clearance-rule"],
            "Clearance(segment-source,margin-terminal-boundary,margin-distance,margin-resolution)",
            "segment-source",
            "margin-terminal-boundary",
            "margin-distance",
            "margin-resolution");
        AssertRow(
            rows["clearance-keepout"],
            "Area-(margin-band,clearance-rule)",
            "margin-band",
            "clearance-rule");

        Assert.Equal("inner boundary participates as a hole, not an eraser", rows["annulus-support"].ResidueOrTension);
        Assert.Equal("clearance is a constraint read over an offset boundary", rows["clearance-rule"].ResidueOrTension);
        Assert.Equal("keepout anti-area is a rule surface, not a visual deletion", rows["clearance-keepout"].ResidueOrTension);
    }

    [Fact]
    public void CompositionReadDiscoversCommonNounsFromLocalStructure()
    {
        var ledger = ConceptCompositionDefinitionSurface.CreateNounCompositionLedger(new Carrier());

        Assert.True(ConceptCompositionReadSurface.TryReadCompositions(ledger, out var read));

        Assert.Contains(read.Reads, item =>
            item.Kind == "offset-band" &&
            item.Definition.Name == "margin-band" &&
            item.Ingredients.SequenceEqual(["segment-source-boundary", "margin-terminal-boundary"]));
        Assert.Contains(read.Reads, item =>
            item.Kind == "band-area" &&
            item.Definition.Name == "margin-area" &&
            item.Ingredients.SequenceEqual(["margin-band", "margin-lane-evidence"]));
        Assert.Contains(read.Reads, item =>
            item.Kind == "support-with-hole" &&
            item.Definition.Name == "annulus-support" &&
            item.ResidueOrTension == "inner boundary participates as a hole, not an eraser");
        Assert.Contains(read.Reads, item =>
            item.Kind == "clearance-rule" &&
            item.Definition.Name == "clearance-rule" &&
            item.ResidueOrTension == "clearance is a constraint read over an offset boundary");
        Assert.Contains(read.Reads, item =>
            item.Kind == "keepout-area" &&
            item.Definition.Name == "clearance-keepout" &&
            item.ResidueOrTension == "keepout anti-area is a rule surface, not a visual deletion");
        Assert.Contains(read.Reads, item =>
            item.Kind == "crystallized-band-area" &&
            item.Definition.Name == "margin");
        Assert.Contains(read.Reads, item =>
            item.Kind == "crystallized-support-with-hole" &&
            item.Definition.Name == "ring");
        Assert.Contains(read.Reads, item =>
            item.Kind == "crystallized-clearance-rule" &&
            item.Definition.Name == "clearance");
    }

    [Fact]
    public void CompositionReadUsesShapeInsteadOfSpecificDefinitionNames()
    {
        var ledger = RenamedCompositionLedger();

        Assert.True(ConceptCompositionReadSurface.TryReadCompositions(ledger, out var read));

        Assert.Contains(read.Reads, item =>
            item.Kind == "offset-band" &&
            item.Definition.Name == "thickened rail");
        Assert.Contains(read.Reads, item =>
            item.Kind == "band-area" &&
            item.Definition.Name == "filled strip");
        Assert.Contains(read.Reads, item =>
            item.Kind == "clearance-rule" &&
            item.Definition.Name == "spacing promise");
        Assert.Contains(read.Reads, item =>
            item.Kind == "crystallized-band-area" &&
            item.Definition.Name == "usable strip");
        Assert.DoesNotContain(read.Reads.Select(item => item.Definition.Name), name => name is "margin-area" or "clearance-rule");
    }

    [Fact]
    public void CompositionLedgerMatchesInspectableJsonFixture()
    {
        var generated = CompositionFixtureEntry();
        var rewritten = GojoStructureEncodingJson.SerializeFixtureEntry(generated);
        var fixture = File.ReadAllText(CompositionFixturePath());
        var reread = GojoStructureEncodingJson.DeserializeFixtureEntry(fixture);
        var rewrittenAgain = GojoStructureEncodingJson.SerializeFixtureEntry(reread);

        AssertJsonEquivalent(fixture, rewritten);
        AssertJsonEquivalent(rewritten, rewrittenAgain);
    }

    private static GojoEncodingJsonFixtureEntry CompositionFixtureEntry()
    {
        var ledger = ConceptCompositionDefinitionSurface.CreateNounCompositionLedger(new Carrier());
        Assert.True(ConceptDefinitionSurface.TryReadLedger(ledger, out var definitions));

        return new GojoEncodingJsonFixtureEntry
        {
            Represents = "A tiny composition sample where common nouns crystallize from earlier structures: margin from offset band and area, ring from circumference offset and hole support, and clearance from offset boundary, resolution, and keepout residue.",
            EntryId = "noun-composition-cad-seeds",
            Expression = "Band + Area + Support + Clearance + Cr",
            EncodingNote = "This is not a permutation catalog. It checks that useful CAD/language nouns can emerge from reusable local structures and become crystallized only where the combined read is useful.",
            Gojo = new GojoEncodingJsonFixtureGojo
            {
                Known =
                [
                    "annulus-support",
                    "clearance",
                    "keepout",
                    "margin",
                    "offset-band",
                    "ring"
                ],
                Paths =
                [
                    "segment boundary + offset boundary --> margin band",
                    "margin band + lane evidence --> margin area",
                    "circumference + exterior offset boundary --> ring band",
                    "ring area + circle anti-area --> annulus support",
                    "offset boundary + distance + resolution --> clearance rule",
                    "clearance rule + band --> keepout anti-area",
                    "useful composed reads --> crystallized reusable nouns"
                ],
                Tension =
                [
                    "inner boundary participates as a hole, not an eraser",
                    "clearance is a constraint read over an offset boundary",
                    "keepout anti-area is a rule surface, not a visual deletion"
                ],
                Definitions = [.. definitions.Select(ConceptDefinitionInspection.DefinitionJson)]
            }
        };
    }

    private static Relation RenamedCompositionLedger()
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

        var source = Add("rail", Relation.Segment(new Carrier(), 0, 1));
        var sourceBoundary = Add("rail edge", Address("Bd0", Atom("rail")), [source]);
        var shifted = Add("shifted read", Address("Off", Atom("rail"), Atom("normal"), Atom("left"), Atom("+d"), Atom("coarse")), [source]);
        var shiftedBoundary = Add("shifted edge", Address("Bd1", Atom("shifted read")), [shifted]);
        var band = Add("thickened rail", Address("Band", Atom("rail edge"), Atom("shifted edge")), [sourceBoundary, shiftedBoundary]);
        var laneA = Add("lane a", Address("Dir", Atom("rail edge"), Atom("+f")), [sourceBoundary]);
        var laneB = Add("lane b", Address("Dir", Atom("shifted edge"), Atom("+f")), [shiftedBoundary]);
        var evidence = Add("lane agreement", Relation.OrderedGroup(new Carrier(), laneA, laneB), [laneA, laneB]);
        var area = Add("filled strip", Address("Area+", Atom("thickened rail"), Atom("lane agreement")), [band, evidence]);
        var rule = Add(
            "spacing promise",
            Address("Clearance", Atom("rail"), Atom("shifted edge"), Atom("+d"), Atom("coarse")),
            [source, shiftedBoundary],
            Text("clearance is a constraint read over an offset boundary"));
        Add("usable strip", ConceptDefinitionSurface.Reference(new Carrier(), area, band), [area, band]);
        Add("usable spacing", ConceptDefinitionSurface.Reference(new Carrier(), rule, band), [rule, band]);

        return ConceptDefinitionSurface.Ledger(new Carrier(), definitions);
    }

    private static void AssertRow(
        GojoEncodingJsonFixtureDefinition row,
        string body,
        params string[] dependencies)
    {
        Assert.Equal(body, row.Body);
        Assert.Equal(dependencies, row.DependsOn ?? []);
    }

    private static string CompositionFixturePath() =>
        Path.Combine(
            AppContext.BaseDirectory,
            "Fixtures",
            "EncoderJson",
            "concept-definitions",
            "noun-composition-cad-seeds.json");

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
