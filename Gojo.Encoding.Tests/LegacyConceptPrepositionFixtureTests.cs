using System.Text.Json.Nodes;
using Gojo.Core;
using Gojo.Encoding;

namespace Gojo.Encoding.Tests;

public sealed class LegacyConceptPrepositionFixtureTests
{
    [Fact]
    public void PrepositionLedgerBuildsParameterizedFocusWords()
    {
        var ledger = LegacyConceptPrepositionDefinitionSurface.CreatePrepositionLedger(new Carrier());

        Assert.True(ConceptDefinitionSurface.TryReadLedger(ledger, out var definitions));
        var rows = definitions
            .Select(ConceptDefinitionInspection.DefinitionJson)
            .ToDictionary(row => row.Name, StringComparer.Ordinal);

        AssertRow(
            rows["preposition-role-frame"],
            "RoleFrame(figure-slot,ground-slot,basis-slot,resolution-slot)",
            "figure-slot",
            "ground-slot",
            "basis-slot",
            "resolution-slot");
        AssertRow(
            rows["comparison-template"],
            "Comp(figure-slot,ground-slot,resolution-slot)",
            "figure-slot",
            "ground-slot",
            "basis-slot",
            "resolution-slot");
        AssertRow(rows["near-focus"], "Focus(comparison-scale,near-zone)", "comparison-scale", "near-zone");
        AssertRow(rows["far-focus"], "Focus(comparison-scale,far-zone)", "comparison-scale", "far-zone");
        AssertRow(rows["halfway-focus"], "Focus(comparison-scale,halfway-zone)", "comparison-scale", "halfway-zone");
        AssertRow(rows["arrived-focus"], "Focus(comparison-scale,arrived-zone)", "comparison-scale", "arrived-zone");
        AssertRow(rows["near"], "Cr(near-focus;context=preposition-role-frame)", "near-focus", "preposition-role-frame");

        Assert.Equal("near is a crystallized focus read, not the whole comparison scale", rows["near"].ResidueOrTension);
        Assert.Equal("basis and resolution remain open until binding", rows["comparison-template"].ResidueOrTension);
    }

    [Fact]
    public void PrepositionReadDiscoversFocusWordsAndAppliedBindings()
    {
        var ledger = LegacyConceptPrepositionDefinitionSurface.CreatePrepositionLedger(new Carrier());

        Assert.True(LegacyConceptPrepositionReadSurface.TryReadPrepositions(ledger, out var read));

        Assert.Contains(read.Reads, item =>
            item.Kind == "role-frame" &&
            item.Definition.Name == "preposition-role-frame");
        Assert.Contains(read.Reads, item =>
            item.Kind == "comparison-template" &&
            item.Definition.Name == "comparison-template");
        Assert.Contains(read.Reads, item =>
            item.Kind == "parameter-scale" &&
            item.Definition.Name == "comparison-scale");
        Assert.Contains(read.Reads, item =>
            item.Kind == "focus-zone" &&
            item.Definition.Name == "near-zone");
        Assert.Contains(read.Reads, item =>
            item.Kind == "focus-read" &&
            item.Definition.Name == "near-focus" &&
            item.Ingredients.SequenceEqual(["comparison-scale", "near-zone"]));
        Assert.Contains(read.Reads, item =>
            item.Kind == "focus-read" &&
            item.Definition.Name == "far-focus" &&
            item.Ingredients.SequenceEqual(["comparison-scale", "far-zone"]));
        Assert.Contains(read.Reads, item =>
            item.Kind == "crystallized-focus-read" &&
            item.Definition.Name == "near" &&
            item.Ingredients.SequenceEqual(["near-focus", "preposition-role-frame"]));
        Assert.Contains(read.Reads, item =>
            item.Kind == "role-binding" &&
            item.Definition.Name == "store-house-near-binding");
        Assert.Contains(read.Reads, item =>
            item.Kind == "bound-comparison" &&
            item.Definition.Name == "store-house-comparison");
        Assert.Contains(read.Reads, item =>
            item.Kind == "applied-preposition-read" &&
            item.Definition.Name == "store-house-near-read" &&
            item.ResidueOrTension == "store and house bind the open roles while the distance value remains low resolution");
        Assert.Contains(read.Reads, item =>
            item.Kind == "applied-preposition-read" &&
            item.Definition.Name == "trace-pad-near-read" &&
            item.ResidueOrTension == "same focus shape, but the CAD basis changes the meaning of near");
    }

    [Fact]
    public void PrepositionReadUsesShapeInsteadOfSpecificPrepositionWord()
    {
        var ledger = RenamedPrepositionLedger();

        Assert.True(LegacyConceptPrepositionReadSurface.TryReadPrepositions(ledger, out var read));

        Assert.Contains(read.Reads, item =>
            item.Kind == "focus-read" &&
            item.Definition.Name == "small gap focus");
        Assert.Contains(read.Reads, item =>
            item.Kind == "crystallized-focus-read" &&
            item.Definition.Name == "close-ish");
        Assert.Contains(read.Reads, item =>
            item.Kind == "applied-preposition-read" &&
            item.Definition.Name == "shop home close read");
        Assert.DoesNotContain(read.Reads.Select(item => item.Definition.Name), name => name == "near");
    }

    [Fact]
    public void PrepositionLedgerMatchesInspectableJsonFixture()
    {
        var generated = PrepositionFixtureEntry();
        var rewritten = GojoStructureEncodingJson.SerializeFixtureEntry(generated);
        var fixture = File.ReadAllText(PrepositionFixturePath());
        var reread = GojoStructureEncodingJson.DeserializeFixtureEntry(fixture);
        var rewrittenAgain = GojoStructureEncodingJson.SerializeFixtureEntry(reread);

        AssertJsonEquivalent(fixture, rewritten);
        AssertJsonEquivalent(rewritten, rewrittenAgain);
    }

    private static GojoEncodingJsonFixtureEntry PrepositionFixtureEntry()
    {
        var ledger = LegacyConceptPrepositionDefinitionSurface.CreatePrepositionLedger(new Carrier());
        Assert.True(ConceptDefinitionSurface.TryReadLedger(ledger, out var definitions));

        return new GojoEncodingJsonFixtureEntry
        {
            Represents = "A legacy contrast sample where near, far, halfway, and arrived are crystallized focus reads over one role-bearing comparison scale, then near is applied to store/house and trace/pad contexts.",
            EntryId = "legacy-preposition-parameterized-focus",
            Expression = "RoleFrame + Comp + Scale + Zone + Focus + Cr + BindRoles + Apply",
            EncodingNote = "This preserves the older role-frame approach as a contrast fixture. It is intentionally not the active grammar direction.",
            Gojo = new GojoEncodingJsonFixtureGojo
            {
                Known =
                [
                    "applied-preposition-read",
                    "comparison-template",
                    "crystallized-focus-read",
                    "focus-read",
                    "parameter-scale",
                    "role-frame"
                ],
                Paths =
                [
                    "figure/ground/basis/resolution slots --> preposition role frame",
                    "figure slot + ground slot + resolution slot --> comparison template",
                    "comparison template --> scale with salient regions",
                    "comparison scale + near zone --> near focus",
                    "near focus + role frame --> crystallized near concept",
                    "store + house + ordinary distance basis --> bound near read",
                    "trace + pad + board clearance basis --> bound near read with CAD residue"
                ],
                Tension =
                [
                    "basis and resolution remain open until binding",
                    "scale landmarks are salient regions, not necessarily equal partitions",
                    "place context supplies the rough distance basis; exact distance remains absent",
                    "near can be descriptive or a clearance-rule warning in this context"
                ],
                Definitions = [.. definitions.Select(ConceptDefinitionInspection.DefinitionJson)]
            }
        };
    }

    private static Relation RenamedPrepositionLedger()
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

        var mover = Add("mover slot", ResolutionFacetStructureSurface.Slot(new Carrier(), Atom("mover")));
        var anchor = Add("anchor slot", ResolutionFacetStructureSurface.Slot(new Carrier(), Atom("anchor")));
        var metric = Add("metric slot", ResolutionFacetStructureSurface.Slot(new Carrier(), Atom("metric")));
        var roughness = Add("roughness slot", ResolutionFacetStructureSurface.Slot(new Carrier(), Atom("roughness")));
        var roles = Add("relation shape", Address("RoleFrame", Atom("mover slot"), Atom("anchor slot"), Atom("metric slot"), Atom("roughness slot")), [mover, anchor, metric, roughness]);
        var comparison = Add(
            "gap compare",
            ResolutionFacetStructureSurface.Comparison(new Carrier(), mover, anchor, roughness),
            [mover, anchor, metric, roughness]);
        var scale = Add("gap scale", Address("Scale", Atom("gap compare"), Atom("landmarks")), [comparison]);
        var zone = Add("small gap region", Address("Zone", Atom("gap scale"), Atom("low-separation")), [scale]);
        var focus = Add("small gap focus", Address("Focus", Atom("gap scale"), Atom("small gap region")), [scale, zone]);
        var close = Add("close-ish", ConceptDefinitionSurface.Reference(new Carrier(), focus, roles), [focus, roles]);
        var shop = Add("shop", Atom("shop"));
        var home = Add("home", Atom("home"));
        var basis = Add("walkable gap", Address("Basis", Atom("walking-distance")));
        var resolution = Add("casual roughness", Address("Res", Atom("casual-distance")));
        var binding = Add("shop home roles", Address("BindRoles", Atom("relation shape"), Atom("shop"), Atom("home"), Atom("walkable gap"), Atom("casual roughness")), [roles, shop, home, basis, resolution]);
        var boundComparison = Add("shop home compare", ResolutionFacetStructureSurface.Comparison(new Carrier(), shop, home, resolution), [shop, home, basis, resolution]);
        Add("shop home close read", Address("Apply", Atom("close-ish"), Atom("shop home roles"), Atom("shop home compare")), [close, binding, boundComparison]);

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

    private static string PrepositionFixturePath() =>
        Path.Combine(
            AppContext.BaseDirectory,
            "Fixtures",
            "EncoderJson",
            "concept-definitions",
            "legacy-preposition-parameterized-focus.json");

    private static Relation Address(string lexical, params IElement[] parts) =>
        ConceptAddressSurface.Compound(new Carrier(), lexical, parts);

    private static Relation Atom(string name) =>
        ConceptAddressSurface.Atom(new Carrier(), name);

    private static void AssertJsonEquivalent(string expected, string actual)
    {
        var expectedNode = JsonNode.Parse(expected);
        var actualNode = JsonNode.Parse(actual);
        Assert.True(JsonNode.DeepEquals(expectedNode, actualNode), actual);
    }
}
