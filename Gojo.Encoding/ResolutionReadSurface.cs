using Gojo.Core;

namespace Gojo.Encoding;

/// <summary>
/// Local discovery over resolution definition ledgers. This reads facet shapes
/// from dependency and compound-body structure; names are debug handles, not
/// the source of the facet meaning.
/// </summary>
public static class ResolutionReadSurface
{
    public readonly record struct FacetRead(
        ConceptDefinitionDiscoverySurface.DefinitionNode Definition,
        string Facet,
        string Body,
        string ResidueOrTension);

    public readonly record struct ResolutionFacetRead(FacetRead[] Facets)
    {
        public FacetRead[] Of(string facet) =>
            [.. Facets.Where(read => read.Facet == facet)];

        public bool Has(string facet) =>
            Facets.Any(read => read.Facet == facet);
    }

    public static bool TryReadResolutionFacets(IElement ledger, out ResolutionFacetRead read)
    {
        if (!ConceptDefinitionDiscoverySurface.TryReadLedger(ledger, out var graph))
        {
            read = default;
            return false;
        }

        return TryReadResolutionFacets(graph, out read);
    }

    public static bool TryReadResolutionFacets(
        ConceptDefinitionDiscoverySurface.DefinitionGraph graph,
        out ResolutionFacetRead read)
    {
        var facets = new List<FacetRead>();

        foreach (var node in graph.Nodes)
        {
            if (!graph.TryReadCompoundBody(node, out var body))
            {
                continue;
            }

            var facet = FacetOf(graph, node, body);
            if (facet is null)
            {
                continue;
            }

            facets.Add(new FacetRead(
                node,
                facet,
                ConceptDefinitionInspection.ElementDebug(node.Read.Body),
                ResidueText(node.Read.ResidueOrTension)));
        }

        read = new ResolutionFacetRead([.. facets]);
        return facets.Count > 0;
    }

    private static string? FacetOf(
        ConceptDefinitionDiscoverySurface.DefinitionGraph graph,
        ConceptDefinitionDiscoverySurface.DefinitionNode node,
        ConceptDefinitionDiscoverySurface.CompoundBodyRead body)
    {
        if (body.Lexical == "Part")
        {
            return "partition";
        }

        if (body.Lexical == "U")
        {
            return "unit";
        }

        if (body.Lexical == "Slot" || body.Lexical == "Open")
        {
            return "omission";
        }

        if (body.Lexical == "Approx")
        {
            return "approximation";
        }

        if (body.Lexical == "Refine")
        {
            return "ladder";
        }

        if (body.Lexical == "Run")
        {
            return "runtime";
        }

        if (body.Lexical == "Read")
        {
            if (HasDependencyCompound(graph, node, "B?"))
            {
                return "ambiguity";
            }

            if (HasDependencyCompound(graph, node, "B") &&
                HasDependencyCompound(graph, node, "Res"))
            {
                return "observer";
            }
        }

        return null;
    }

    private static bool HasDependencyCompound(
        ConceptDefinitionDiscoverySurface.DefinitionGraph graph,
        ConceptDefinitionDiscoverySurface.DefinitionNode node,
        string lexical) =>
        graph.DependenciesOf(node).Any(dependency =>
            graph.TryReadCompoundBody(dependency, out var body) &&
            body.Lexical == lexical);

    private static string ResidueText(IElement element) =>
        EncodedTextSurface.IsUnknownPoint(element)
            ? string.Empty
            : EncodedTextSurface.TryRead(element, out var text)
                ? text
                : ConceptDefinitionInspection.ElementDebug(element);
}
