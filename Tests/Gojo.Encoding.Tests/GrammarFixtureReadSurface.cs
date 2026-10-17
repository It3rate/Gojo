using Gojo.Core;
using Gojo.Structures;

namespace Gojo.Encoding.Tests;

/// <summary>
/// Local inspection surface for tiny grammar fixtures. It reads grammar evidence
/// from definition shape: token order, lexical candidates, modifier candidates,
/// low-resolution position reads, position phrase candidates, selected reads,
/// interpretation bundles, and traces. The debug kind names are read outputs,
/// not stored grammar ownership.
/// </summary>
internal static class GrammarFixtureReadSurface
{
    public readonly record struct GrammarRead(
        ConceptDefinitionDiscoverySurface.DefinitionNode Definition,
        string Kind,
        string Body,
        string[] Ingredients,
        string ResidueOrTension);

    public readonly record struct GrammarSurfaceRead(GrammarRead[] Reads)
    {
        public GrammarRead[] Of(string kind) =>
            [.. Reads.Where(read => read.Kind == kind)];

        public bool Contains(string kind) =>
            Reads.Any(read => read.Kind == kind);
    }

    public static bool TryReadGrammar(IElement ledger, out GrammarSurfaceRead read)
    {
        if (!ConceptDefinitionDiscoverySurface.TryReadLedger(ledger, out var graph))
        {
            read = default;
            return false;
        }

        return TryReadGrammar(graph, out read);
    }

    public static bool TryReadGrammar(
        ConceptDefinitionDiscoverySurface.DefinitionGraph graph,
        out GrammarSurfaceRead read)
    {
        var reads = new List<GrammarRead>();

        AddCompounds("Token", "token-evidence");
        AddCompounds("Tokens", "token-sequence");
        AddCompounds("Lex", "lexical-candidate");
        AddCompounds("Modify", "modifier-candidate");
        AddLowResolutionPositionReads();
        AddCompounds("PositionPhrase", "position-phrase-candidate");
        AddCompounds("SelectedRead", "selected-read");
        AddCompounds("InterpretationBundle", "interpretation-bundle");
        AddCompounds("ContextFrame", "context-frame");
        AddCompounds("ContextSettlement", "context-settlement");
        AddCompounds("MergedInterpretation", "merged-interpretation");
        AddCompounds("GrammarTrace", "grammar-trace");

        read = new GrammarSurfaceRead([.. reads]);
        return reads.Count > 0;

        void AddCompounds(string lexical, string kind)
        {
            foreach (var node in graph.WithCompoundBody(lexical))
            {
                reads.Add(Read(graph, node, kind));
            }
        }

        void AddLowResolutionPositionReads()
        {
            foreach (var node in graph.Nodes)
            {
                if (node.Read.Body is not Relation witness ||
                    !ReadWitness.TryRead(
                        witness,
                        out var selected,
                        out var ground,
                        out var result,
                        out var residueOrTension,
                        out _))
                {
                    continue;
                }

                if (!TryReadPoint(result, out var location) ||
                    !location.Carrier.UnitShape.TryReadResolution(out _, out _))
                {
                    continue;
                }

                reads.Add(new GrammarRead(
                    node,
                    "low-resolution-position-read",
                    $"Pos({ConceptDefinitionInspection.ElementDebug(selected)},{ConceptDefinitionInspection.ElementDebug(ground)}->{ConceptDefinitionInspection.ElementDebug(result)})",
                    [.. graph.DependenciesOf(node).Select(dependency => dependency.Name)],
                    ResidueText(residueOrTension)));
            }
        }
    }

    private static GrammarRead Read(
        ConceptDefinitionDiscoverySurface.DefinitionGraph graph,
        ConceptDefinitionDiscoverySurface.DefinitionNode node,
        string kind) =>
        new(
            node,
            kind,
            ConceptDefinitionInspection.ElementDebug(node.Read.Body),
            [.. graph.DependenciesOf(node).Select(dependency => dependency.Name)],
            ResidueText(node.Read.ResidueOrTension));

    private static string ResidueText(IElement element) =>
        IsUnknownPoint(element)
            ? string.Empty
            : EncodedTextSurface.TryRead(element, out var text)
                ? text
                : ConceptDefinitionInspection.ElementDebug(element);

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

    private static bool IsUnknownPoint(IElement element) =>
        element is Relation relation &&
        relation.Left is CarrierLocation location &&
        relation.Right is Carrier carrier &&
        carrier.IsUnknown &&
        location.Carrier.IsUnknown &&
        location.Ticks == 0;
}
