using Gojo.Core;

namespace Gojo.Encoding.Structures;

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
        string ResidueOrTension,
        bool IsInferred,
        string Evidence);

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

            if (TryReadFacet(
                    element,
                    outerResidueOrTension,
                    allowInference: true,
                    out var facet,
                    out var body,
                    out var residueOrTension,
                    out var isInferred,
                    out var evidence) &&
                visitedFacetBodies.Add(FacetBodyOf(element)))
            {
                facets.Add(new SourceFacetRead(
                    facets.Count,
                    path,
                    element,
                    facet,
                    body,
                    residueOrTension,
                    isInferred,
                    evidence));
                return;
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
        out string residueOrTension) =>
        TryReadFacet(
            element,
            outerResidueOrTension,
            allowInference: false,
            out facet,
            out body,
            out residueOrTension,
            out _,
            out _);

    private static bool TryReadFacet(
        IElement element,
        IElement outerResidueOrTension,
        bool allowInference,
        out string facet,
        out string body,
        out string residueOrTension,
        out bool isInferred,
        out string evidence)
    {
        var facetBody = FacetBodyOf(element);
        if (IsPrimitiveSurface(facetBody))
        {
            facet = string.Empty;
            body = string.Empty;
            residueOrTension = string.Empty;
            isInferred = false;
            evidence = string.Empty;
            return false;
        }

        if (ResolutionFacetStructureSurface.TryReadPartition(element, out var partition))
        {
            return Matched("partition", element, outerResidueOrTension, partition.ResidueOrTension, out facet, out body, out residueOrTension, out isInferred, out evidence);
        }

        if (ResolutionFacetStructureSurface.TryReadUnitResolution(element, out var unitResolution))
        {
            return Matched("unit", element, outerResidueOrTension, unitResolution.ResidueOrTension, out facet, out body, out residueOrTension, out isInferred, out evidence);
        }

        if (ResolutionFacetStructureSurface.TryReadObserverResolution(element, out var observerResolution))
        {
            return Matched("observer", element, outerResidueOrTension, observerResolution.ResidueOrTension, out facet, out body, out residueOrTension, out isInferred, out evidence);
        }

        if (ResolutionFacetStructureSurface.TryReadSlot(element, out _))
        {
            return Matched("omission", element, outerResidueOrTension, EncodedTextSurface.UnknownPoint(), out facet, out body, out residueOrTension, out isInferred, out evidence);
        }

        if (ResolutionFacetStructureSurface.TryReadApproximation(element, out var approximation))
        {
            return Matched("approximation", element, outerResidueOrTension, approximation.ResidueOrTension, out facet, out body, out residueOrTension, out isInferred, out evidence);
        }

        if (ResolutionFacetStructureSurface.TryReadAmbiguousResolution(element, out var ambiguousResolution))
        {
            return Matched("ambiguity", element, outerResidueOrTension, ambiguousResolution.ResidueOrTension, out facet, out body, out residueOrTension, out isInferred, out evidence);
        }

        if (ResolutionFacetStructureSurface.TryReadLadderResolution(element, out var ladderResolution))
        {
            return Matched("ladder", element, outerResidueOrTension, ladderResolution.ResidueOrTension, out facet, out body, out residueOrTension, out isInferred, out evidence);
        }

        if (ResolutionFacetStructureSurface.TryReadRuntimeResolution(element, out var runtimeResolution))
        {
            return Matched("runtime", element, outerResidueOrTension, runtimeResolution.ResidueOrTension, out facet, out body, out residueOrTension, out isInferred, out evidence);
        }

        if (allowInference &&
            TryInferFacet(element, outerResidueOrTension, out facet, out body, out residueOrTension, out evidence))
        {
            isInferred = true;
            return true;
        }

        facet = string.Empty;
        body = string.Empty;
        residueOrTension = string.Empty;
        isInferred = false;
        evidence = string.Empty;
        return false;
    }

    private static bool Matched(
        string facetName,
        IElement element,
        IElement outerResidueOrTension,
        IElement innerResidueOrTension,
        out string facet,
        out string body,
        out string residueOrTension,
        out bool isInferred,
        out string evidence)
    {
        facet = facetName;
        body = ConceptDefinitionInspection.ElementDebug(FacetBodyOf(element));
        residueOrTension = ResidueText(
            EncodedTextSurface.IsUnknownPoint(outerResidueOrTension)
                ? innerResidueOrTension
                : outerResidueOrTension);
        isInferred = false;
        evidence = "native resolution facet surface";
        return true;
    }

    private static bool TryInferFacet(
        IElement element,
        IElement outerResidueOrTension,
        out string facet,
        out string body,
        out string residueOrTension,
        out string evidence)
    {
        var facetBody = FacetBodyOf(element);

        if (TryInferAmbiguousBasis(facetBody, outerResidueOrTension, out facet, out body, out residueOrTension, out evidence))
        {
            return true;
        }

        if (TryReadGroup(facetBody, out var elements))
        {
            if (TryInferPartition(elements, outerResidueOrTension, out facet, out body, out residueOrTension, out evidence) ||
                TryInferOmission(elements, outerResidueOrTension, out facet, out body, out residueOrTension, out evidence))
            {
                return true;
            }
        }

        if (ResolutionFacetStructureSurface.TryReadUnitComparison(facetBody, out var unitComparison))
        {
            facet = "unit";
            body = $"U?({ConceptDefinitionInspection.ElementDebug(facetBody)})";
            evidence = "inferred from unit comparison witness without a canonical unit-resolution wrapper";
            residueOrTension = InferredResidue(outerResidueOrTension, unitComparison.ResidueOrTension, evidence);
            return true;
        }

        if (TryInferReadWitness(facetBody, outerResidueOrTension, out facet, out body, out residueOrTension, out evidence) ||
            TryInferRefinement(facetBody, outerResidueOrTension, out facet, out body, out residueOrTension, out evidence) ||
            TryInferRuntimeStep(facetBody, outerResidueOrTension, out facet, out body, out residueOrTension, out evidence))
        {
            return true;
        }

        facet = string.Empty;
        body = string.Empty;
        residueOrTension = string.Empty;
        evidence = string.Empty;
        return false;
    }

    private static bool TryInferPartition(
        IElement[] elements,
        IElement outerResidueOrTension,
        out string facet,
        out string body,
        out string residueOrTension,
        out string evidence)
    {
        if (elements.Length >= 3 &&
            TryReadSegment(elements[0], out var start, out _) &&
            elements[2] is Relation siteFrame &&
            EncodedTextSurface.TryReadRootElements(siteFrame, out var sites) &&
            sites.Length > 0 &&
            sites.All(site =>
                site is Relation point &&
                EncodedTextSurface.TryReadPoint(point, out var siteLocation) &&
                siteLocation.Carrier.SameIdentityAs(start.Carrier)))
        {
            facet = "partition";
            body = $"Part?({ConceptDefinitionInspection.ElementDebug(elements[0])},{ConceptDefinitionInspection.ElementDebug(elements[1])})";
            evidence = "inferred from source, count, and local point-site frame without the canonical residue slot";
            residueOrTension = InferredResidue(
                outerResidueOrTension,
                elements.Length > 3 ? elements[3] : EncodedTextSurface.UnknownPoint(),
                evidence);
            return true;
        }

        facet = string.Empty;
        body = string.Empty;
        residueOrTension = string.Empty;
        evidence = string.Empty;
        return false;
    }

    private static bool TryInferOmission(
        IElement[] elements,
        IElement outerResidueOrTension,
        out string facet,
        out string body,
        out string residueOrTension,
        out string evidence)
    {
        if (elements.Length >= 2 &&
            elements[0] is not Placeholder &&
            elements.Skip(1).Any(element => element is Placeholder))
        {
            facet = "omission";
            body = $"Slot?({ConceptDefinitionInspection.ElementDebug(elements[0])},?)";
            evidence = "inferred from a role followed by an open placeholder slot";
            residueOrTension = InferredResidue(
                outerResidueOrTension,
                elements.Length > 2 ? elements[^1] : EncodedTextSurface.UnknownPoint(),
                evidence);
            return true;
        }

        facet = string.Empty;
        body = string.Empty;
        residueOrTension = string.Empty;
        evidence = string.Empty;
        return false;
    }

    private static bool TryInferReadWitness(
        IElement element,
        IElement outerResidueOrTension,
        out string facet,
        out string body,
        out string residueOrTension,
        out string evidence)
    {
        if (element is Relation witness &&
            Gojo.Structures.ReadWitness.TryRead(
                witness,
                out var source,
                out var readSurface,
                out var result,
                out var residue,
                out _) &&
            ResolutionFacetStructureSurface.TryReadResolutionRung(result, out _))
        {
            if (ContainsPlaceholder(readSurface))
            {
                facet = "approximation";
                body = $"Approx?(Comp({ConceptDefinitionInspection.ElementDebug(source)},{ConceptDefinitionInspection.ElementDebug(readSurface)},{ConceptDefinitionInspection.ElementDebug(result)}),{ConceptDefinitionInspection.ElementDebug(result)})";
                evidence = "inferred from read witness whose target/read surface contains an open placeholder at a resolution rung";
                residueOrTension = InferredResidue(outerResidueOrTension, residue, evidence);
                return true;
            }

            facet = "observer";
            body = $"Read?({ConceptDefinitionInspection.ElementDebug(source)},{ConceptDefinitionInspection.ElementDebug(readSurface)},{ConceptDefinitionInspection.ElementDebug(result)})";
            evidence = "inferred from read witness ending at a resolution rung without canonical observer-basis structure";
            residueOrTension = InferredResidue(outerResidueOrTension, residue, evidence);
            return true;
        }

        facet = string.Empty;
        body = string.Empty;
        residueOrTension = string.Empty;
        evidence = string.Empty;
        return false;
    }

    private static bool TryInferAmbiguousBasis(
        IElement element,
        IElement outerResidueOrTension,
        out string facet,
        out string body,
        out string residueOrTension,
        out string evidence)
    {
        if (ResolutionFacetStructureSurface.TryReadAmbiguousBasis(element, out var ambiguousBasis))
        {
            facet = "ambiguity";
            body = $"Read?({ConceptDefinitionInspection.ElementDebug(ambiguousBasis.Source)},B?)";
            evidence = "inferred from ambiguous basis surface without a wrapping observer read";
            residueOrTension = InferredResidue(outerResidueOrTension, ambiguousBasis.ResidueOrTension, evidence);
            return true;
        }

        facet = string.Empty;
        body = string.Empty;
        residueOrTension = string.Empty;
        evidence = string.Empty;
        return false;
    }

    private static bool TryInferRefinement(
        IElement element,
        IElement outerResidueOrTension,
        out string facet,
        out string body,
        out string residueOrTension,
        out string evidence)
    {
        if (element is Relation refinement &&
            Gojo.Structures.RefinementRead.TryRead(
                refinement,
                out var parameter,
                out var binding,
                out var witness,
                out var residue,
                out var timelineOrProvenance) &&
            Gojo.Structures.ReadWitness.TryRead(
                witness,
                out var selected,
                out _,
                out var result,
                out _,
                out _) &&
            ReferenceEquals(parameter, selected) &&
            ReferenceEquals(binding, result))
        {
            facet = "ladder";
            body = $"Refine?({ConceptDefinitionInspection.ElementDebug(parameter)},{ConceptDefinitionInspection.ElementDebug(binding)},{ConceptDefinitionInspection.ElementDebug(timelineOrProvenance)})";
            evidence = "inferred from compatible refinement read without canonical rung/correspondence bodies";
            residueOrTension = InferredResidue(outerResidueOrTension, residue, evidence);
            return true;
        }

        facet = string.Empty;
        body = string.Empty;
        residueOrTension = string.Empty;
        evidence = string.Empty;
        return false;
    }

    private static bool TryInferRuntimeStep(
        IElement element,
        IElement outerResidueOrTension,
        out string facet,
        out string body,
        out string residueOrTension,
        out string evidence)
    {
        if (element is Relation step &&
            Gojo.Structures.TimeStepSequence.TryReadStep(
                step,
                out _,
                out _,
                out _,
                out var before,
                out var transition,
                out var after,
                out var residue) &&
            ResolutionFacetStructureSurface.TryReadResolutionRung(after, out _))
        {
            facet = "runtime";
            body = $"Run?({ConceptDefinitionInspection.ElementDebug(before)},{ConceptDefinitionInspection.ElementDebug(transition)},t?,{ConceptDefinitionInspection.ElementDebug(after)})";
            evidence = "inferred from a time-step witness whose after-state is a resolution rung";
            residueOrTension = InferredResidue(outerResidueOrTension, residue, evidence);
            return true;
        }

        facet = string.Empty;
        body = string.Empty;
        residueOrTension = string.Empty;
        evidence = string.Empty;
        return false;
    }

    private static bool TryReadGroup(IElement element, out IElement[] elements)
    {
        if (element is Relation relation &&
            EncodedTextSurface.TryReadRootElements(relation, out elements))
        {
            return true;
        }

        elements = [];
        return false;
    }

    private static bool ContainsPlaceholder(IElement element)
    {
        if (element is Placeholder)
        {
            return true;
        }

        return element is Relation relation &&
            (ContainsPlaceholder(relation.Left) || ContainsPlaceholder(relation.Right));
    }

    private static bool TryReadSegment(
        IElement element,
        out CarrierLocation start,
        out CarrierLocation end)
    {
        if (element is Relation segment &&
            segment.Left is Relation leftPoint &&
            segment.Right is Relation rightPoint &&
            EncodedTextSurface.TryReadPoint(leftPoint, out start) &&
            EncodedTextSurface.TryReadPoint(rightPoint, out end) &&
            start.Carrier.SameIdentityAs(end.Carrier))
        {
            return true;
        }

        start = default;
        end = default;
        return false;
    }

    private static string InferredResidue(
        IElement outerResidueOrTension,
        IElement innerResidueOrTension,
        string evidence)
    {
        var residue = ResidueText(
            EncodedTextSurface.IsUnknownPoint(outerResidueOrTension)
                ? innerResidueOrTension
                : outerResidueOrTension);
        return string.IsNullOrEmpty(residue)
            ? evidence
            : $"{residue}; {evidence}";
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
