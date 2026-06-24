using Gojo.Core;

namespace Gojo.Encoding;

/// <summary>
/// Local discovery over resolution definition ledgers and arbitrary source
/// structure. Names are debug handles when present; the facet meaning is read
/// from native relation bodies.
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

    public readonly record struct SourceFacetRead(
        int Index,
        string Path,
        IElement Surface,
        string Facet,
        string Body,
        string ResidueOrTension);

    public readonly record struct ResolutionSourceRead(SourceFacetRead[] Facets)
    {
        public SourceFacetRead[] Of(string facet) =>
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
            if (!TryReadFacet(
                    node.Read.Body,
                    node.Read.ResidueOrTension,
                    out var facet,
                    out var body,
                    out var residueOrTension))
            {
                continue;
            }

            facets.Add(new FacetRead(
                node,
                facet,
                body,
                residueOrTension));
        }

        read = new ResolutionFacetRead([.. facets]);
        return facets.Count > 0;
    }

    public static bool TryDiscoverResolutionFacets(IElement source, out ResolutionSourceRead read)
    {
        var facets = new List<SourceFacetRead>();
        var visitedElements = new HashSet<IElement>(ReferenceEqualityComparer.Instance);
        var visitedFacetBodies = new HashSet<IElement>(ReferenceEqualityComparer.Instance);

        Visit(source, "root");

        read = new ResolutionSourceRead([.. facets]);
        return facets.Count > 0;

        void Visit(IElement element, string path)
        {
            if (element is null || !visitedElements.Add(element))
            {
                return;
            }

            var outerResidueOrTension = element is Relation relationElement &&
                ConceptDefinitionSurface.TryRead(relationElement, out var definitionElement)
                    ? definitionElement.ResidueOrTension
                    : EncodedTextSurface.UnknownPoint();

            if (TryReadFacet(element, outerResidueOrTension, out var facet, out var body, out var residueOrTension) &&
                visitedFacetBodies.Add(FacetBodyOf(element)))
            {
                facets.Add(new SourceFacetRead(
                    facets.Count,
                    path,
                    element,
                    facet,
                    body,
                    residueOrTension));
            }

            if (element is Relation relation)
            {
                if (ConceptDefinitionSurface.TryRead(relation, out var definition))
                {
                    Visit(definition.Name, $"{path}.name");
                    Visit(definition.Body, $"{path}.body");
                    Visit(definition.Provenance, $"{path}.provenance");
                    Visit(definition.ResidueOrTension, $"{path}.residue");
                    for (var index = 0; index < definition.Dependencies.Length; index++)
                    {
                        Visit(definition.Dependencies[index], $"{path}.dep{index}");
                    }
                }

                Visit(relation.Left, $"{path}.left");
                Visit(relation.Right, $"{path}.right");
            }
        }
    }

    private static bool TryReadFacet(
        IElement element,
        IElement outerResidueOrTension,
        out string facet,
        out string body,
        out string residueOrTension)
    {
        var facetBody = FacetBodyOf(element);
        if (IsPrimitiveSurface(facetBody))
        {
            facet = string.Empty;
            body = string.Empty;
            residueOrTension = string.Empty;
            return false;
        }

        if (ResolutionFacetStructureSurface.TryReadPartition(element, out var partition))
        {
            return Matched("partition", element, outerResidueOrTension, partition.ResidueOrTension, out facet, out body, out residueOrTension);
        }

        if (ResolutionFacetStructureSurface.TryReadUnitResolution(element, out var unitResolution))
        {
            return Matched("unit", element, outerResidueOrTension, unitResolution.ResidueOrTension, out facet, out body, out residueOrTension);
        }

        if (ResolutionFacetStructureSurface.TryReadObserverResolution(element, out var observerResolution))
        {
            return Matched("observer", element, outerResidueOrTension, observerResolution.ResidueOrTension, out facet, out body, out residueOrTension);
        }

        if (ResolutionFacetStructureSurface.TryReadSlot(element, out _))
        {
            return Matched("omission", element, outerResidueOrTension, EncodedTextSurface.UnknownPoint(), out facet, out body, out residueOrTension);
        }

        if (ResolutionFacetStructureSurface.TryReadApproximation(element, out var approximation))
        {
            return Matched("approximation", element, outerResidueOrTension, approximation.ResidueOrTension, out facet, out body, out residueOrTension);
        }

        if (ResolutionFacetStructureSurface.TryReadAmbiguousResolution(element, out var ambiguousResolution))
        {
            return Matched("ambiguity", element, outerResidueOrTension, ambiguousResolution.ResidueOrTension, out facet, out body, out residueOrTension);
        }

        if (ResolutionFacetStructureSurface.TryReadLadderResolution(element, out var ladderResolution))
        {
            return Matched("ladder", element, outerResidueOrTension, ladderResolution.ResidueOrTension, out facet, out body, out residueOrTension);
        }

        if (ResolutionFacetStructureSurface.TryReadRuntimeResolution(element, out var runtimeResolution))
        {
            return Matched("runtime", element, outerResidueOrTension, runtimeResolution.ResidueOrTension, out facet, out body, out residueOrTension);
        }

        facet = string.Empty;
        body = string.Empty;
        residueOrTension = string.Empty;
        return false;
    }

    private static bool Matched(
        string facetName,
        IElement element,
        IElement outerResidueOrTension,
        IElement innerResidueOrTension,
        out string facet,
        out string body,
        out string residueOrTension)
    {
        facet = facetName;
        body = ConceptDefinitionInspection.ElementDebug(FacetBodyOf(element));
        residueOrTension = ResidueText(
            EncodedTextSurface.IsUnknownPoint(outerResidueOrTension)
                ? innerResidueOrTension
                : outerResidueOrTension);
        return true;
    }

    private static IElement FacetBodyOf(IElement element) =>
        ConceptDefinitionSurface.TryRead(element, out var definition)
            ? definition.Body
            : element;

    private static bool IsPrimitiveSurface(IElement element) =>
        ConceptAddressSurface.TryReadAtom(element, out _, out _) ||
        ConceptAddressSurface.TryReadCompound(element, out _, out _, out _, out _) ||
        EncodedTextSurface.TryRead(element, out _);

    private static string ResidueText(IElement element) =>
        EncodedTextSurface.IsUnknownPoint(element)
            ? string.Empty
            : EncodedTextSurface.TryRead(element, out var text)
                ? text
                : ConceptDefinitionInspection.ElementDebug(element);
}
