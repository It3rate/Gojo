using Gojo.Core;
using Gojo.Encoding;
using Gojo.Structures;

namespace Gojo.Encoding.Tests;

public sealed class RedTraceNearPinGrammarFixtureTests
{
    [Fact]
    public void GrammarLedgerBuildsTinyPhraseWithResidue()
    {
        var ledger = RedTraceNearPinGrammarFixture.CreateLedger(new Carrier());

        Assert.True(ConceptDefinitionSurface.TryReadLedger(ledger, out var definitions));
        var rows = definitions
            .Select(ConceptDefinitionInspection.DefinitionJson)
            .ToDictionary(row => row.Name, StringComparer.Ordinal);

        AssertRow(rows["token-sequence"], "Tokens(token-red,token-trace,token-near,token-pin)", "token-red", "token-trace", "token-near", "token-pin");
        AssertRow(rows["red-lexical-candidate"], "Lex(token-red,red-quality)", "token-red", "red-quality");
        AssertRow(
            rows["near-lexical-candidate"],
            "Lex(token-near,trace-pin-position-read)",
            "token-near",
            "trace-pin-position-read");
        AssertRow(
            rows["red-trace-modifier"],
            "Modify(red-lexical-candidate,trace-lexical-candidate)",
            "red-lexical-candidate",
            "trace-lexical-candidate");
        AssertRow(
            rows["red-trace-near-pin-interpretation"],
            "InterpretationBundle(token-sequence,red-trace-near-pin-selected-read,trace-near-pin-phrase)",
            "token-sequence",
            "red-trace-near-pin-selected-read",
            "red-trace-near-pin-phrase",
            "trace-near-pin-phrase");

        Assert.Equal("exact red basis remains open", rows["red-quality"].ResidueOrTension);
        Assert.Equal("low-resolution position on the pin ground; exact distance remains missing", rows["trace-pin-low-resolution-position"].ResidueOrTension);
        Assert.Equal("near is read from low-resolution position on the pin ground, not from a stored concept tree", rows["trace-pin-position-read"].ResidueOrTension);
        Assert.Equal("alternate attachment remains as residue", rows["red-trace-near-pin-interpretation"].ResidueOrTension);
        Assert.Equal("original order and unselected read remain available", rows["red-trace-near-pin-grammar-trace"].ResidueOrTension);
    }

    [Fact]
    public void GrammarReadDiscoversTokensCandidatesAndAlternatives()
    {
        var ledger = RedTraceNearPinGrammarFixture.CreateLedger(new Carrier());

        Assert.True(GrammarFixtureReadSurface.TryReadGrammar(ledger, out var read));

        Assert.Contains(read.Reads, item =>
            item.Kind == "token-evidence" &&
            item.Definition.Name == "token-red");
        Assert.Contains(read.Reads, item =>
            item.Kind == "token-sequence" &&
            item.Definition.Name == "token-sequence" &&
            item.Ingredients.SequenceEqual(["token-red", "token-trace", "token-near", "token-pin"]));
        Assert.Contains(read.Reads, item =>
            item.Kind == "lexical-candidate" &&
            item.Definition.Name == "near-lexical-candidate" &&
            item.Ingredients.SequenceEqual(["token-near", "trace-pin-position-read"]));
        Assert.Contains(read.Reads, item =>
            item.Kind == "low-resolution-position-read" &&
            item.Definition.Name == "trace-pin-position-read" &&
            item.Ingredients.SequenceEqual(["token-trace", "token-near", "token-pin", "trace-pin-low-resolution-position"]) &&
            item.ResidueOrTension == "exact trace-to-pin position remains unresolved");
        Assert.Contains(read.Reads, item =>
            item.Kind == "modifier-candidate" &&
            item.Definition.Name == "red-trace-modifier" &&
            item.ResidueOrTension == "red modifies trace while exact color basis remains unresolved");
        Assert.Contains(read.Reads, item =>
            item.Kind == "position-phrase-candidate" &&
            item.Definition.Name == "trace-near-pin-phrase" &&
            item.ResidueOrTension == "near can attach to trace before modifier settlement");
        Assert.Contains(read.Reads, item =>
            item.Kind == "position-phrase-candidate" &&
            item.Definition.Name == "red-trace-near-pin-phrase" &&
            item.ResidueOrTension == "near can attach after red-trace phrase settlement");
        Assert.Contains(read.Reads, item =>
            item.Kind == "selected-read" &&
            item.Definition.Name == "red-trace-near-pin-selected-read");
        Assert.Contains(read.Reads, item =>
            item.Kind == "interpretation-bundle" &&
            item.Definition.Name == "red-trace-near-pin-interpretation" &&
            item.ResidueOrTension == "alternate attachment remains as residue");
        Assert.Contains(read.Reads, item =>
            item.Kind == "grammar-trace" &&
            item.Definition.Name == "red-trace-near-pin-grammar-trace" &&
            item.ResidueOrTension == "original order and unselected read remain available");
    }

    [Fact]
    public void NearTokenIsProvenanceForALowResolutionPositionRead()
    {
        var ledger = RedTraceNearPinGrammarFixture.CreateLedger(new Carrier());
        Assert.True(ConceptDefinitionDiscoverySurface.TryReadLedger(ledger, out var graph));

        var node = Assert.Single(graph.Nodes, item => item.Name == "trace-pin-position-read");
        var witness = Assert.IsType<Relation>(node.Read.Body);
        Assert.True(ReadWitness.TryRead(
            witness,
            out var selected,
            out var ground,
            out var result,
            out var residueOrTension,
            out var provenance));

        Assert.Equal("token-trace", ConceptDefinitionInspection.ElementDebug(selected));
        Assert.Equal("token-pin", ConceptDefinitionInspection.ElementDebug(ground));
        Assert.Equal("token-near", ConceptDefinitionInspection.ElementDebug(provenance));
        Assert.True(TryReadPoint(result, out var location));
        Assert.Equal(1, location.Ticks);
        Assert.True(location.Carrier.UnitShape.TryReadResolution(out _, out var sourceTicksPerTick));
        Assert.Equal(10, sourceTicksPerTick);
        Assert.Equal("exact trace-to-pin position remains unresolved", ConceptDefinitionInspection.ElementDebug(residueOrTension));
    }

    private static void AssertRow(
        GojoEncodingJsonFixtureDefinition row,
        string body,
        params string[] dependencies)
    {
        Assert.Equal(body, row.Body);
        Assert.Equal(dependencies, row.DependsOn ?? []);
    }

    private static bool TryReadPoint(IElement element, out CarrierLocation location)
    {
        if (element is Relation point &&
            point.Left is CarrierLocation pointLocation &&
            point.Right is Carrier pointCarrier &&
            pointLocation.Carrier.SameIdentityAs(pointCarrier))
        {
            location = pointLocation;
            return true;
        }

        location = default;
        return false;
    }
}
