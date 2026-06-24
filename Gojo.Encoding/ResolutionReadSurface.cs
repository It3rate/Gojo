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
            var facet = FacetOf(node);
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

    private static string? FacetOf(ConceptDefinitionDiscoverySurface.DefinitionNode node)
    {
        if (ResolutionFacetStructureSurface.TryReadPartition(node.Read.Body, out _))
        {
            return "partition";
        }

        if (ResolutionFacetStructureSurface.TryReadUnitResolution(node.Read.Body, out _))
        {
            return "unit";
        }

        if (ResolutionFacetStructureSurface.TryReadObserverResolution(node.Read.Body, out _))
        {
            return "observer";
        }

        if (ResolutionFacetStructureSurface.TryReadSlot(node.Read.Body, out _))
        {
            return "omission";
        }

        if (ResolutionFacetStructureSurface.TryReadApproximation(node.Read.Body, out _))
        {
            return "approximation";
        }

        if (ResolutionFacetStructureSurface.TryReadAmbiguousResolution(node.Read.Body, out _))
        {
            return "ambiguity";
        }

        if (ResolutionFacetStructureSurface.TryReadLadderResolution(node.Read.Body, out _))
        {
            return "ladder";
        }

        if (ResolutionFacetStructureSurface.TryReadRuntimeResolution(node.Read.Body, out _))
        {
            return "runtime";
        }

        return null;
    }

    private static string ResidueText(IElement element) =>
        EncodedTextSurface.IsUnknownPoint(element)
            ? string.Empty
            : EncodedTextSurface.TryRead(element, out var text)
                ? text
                : ConceptDefinitionInspection.ElementDebug(element);
}
