using Gojo.Core;

namespace Gojo.Encoding;

/// <summary>
/// Small debug/fixture read surface for concept definitions. The order of reads
/// is intentional because several Gojo surfaces can share the same broad
/// relation shape.
/// </summary>
public static class ConceptDefinitionInspection
{
    public static GojoEncodingJsonFixtureDefinition DefinitionJson(Relation definition)
    {
        if (!ConceptDefinitionSurface.TryRead(definition, out var read))
        {
            throw new InvalidOperationException("Could not read generated concept definition.");
        }

        var name = ElementDebug(read.Name);
        var body = ElementDebug(read.Body);
        var provenance = ElementDebug(read.Provenance);
        var dependencies = read.Dependencies.Select(ElementDebug).ToList();
        var residue = EncodedTextSurface.IsUnknownPoint(read.ResidueOrTension)
            ? null
            : ElementDebug(read.ResidueOrTension);
        var dependencyText = dependencies.Count == 0
            ? string.Empty
            : $"; deps={string.Join(",", dependencies)}";
        var residueText = residue is null
            ? string.Empty
            : $"; residue={residue}";

        return new GojoEncodingJsonFixtureDefinition
        {
            Name = name,
            Body = body,
            DependsOn = dependencies.Count == 0 ? null : dependencies,
            Provenance = provenance == body ? null : provenance,
            ResidueOrTension = residue,
            Encoding = $"Def({name},{body}{dependencyText})+Prov({provenance}){residueText}"
        };
    }

    public static Relation Address(string lexical, params IElement[] parts) =>
        ConceptAddressSurface.Compound(new Carrier(), lexical, parts);

    public static Relation Atom(string name) =>
        ConceptAddressSurface.Atom(new Carrier(), name);

    public static Relation Text(string text) =>
        EncodedTextSurface.Create(new Carrier(), text);

    public static string ElementDebug(IElement element)
    {
        if (EncodedTextSurface.IsUnknownPoint(element))
        {
            return "?";
        }

        if (ConceptAddressSurface.TryReadAtom(element, out _, out _) ||
            ConceptAddressSurface.TryReadCompound(element, out _, out _, out _, out _))
        {
            return ConceptAddressSurface.ToDebugString(element);
        }

        if (ConceptDefinitionSurface.TryRead(element, out var definition))
        {
            return ElementDebug(definition.Name);
        }

        if (TryReadPoint(element, out _))
        {
            return "P(X)";
        }

        if (TryReadSegment(element, out var start, out var end))
        {
            return start.Ticks == end.Ticks
                ? "S0(P(A@X),P(B@X))"
                : "S(P(A@X),P(B@Y))";
        }

        if (EncodedTextSurface.TryRead(element, out var text))
        {
            return text;
        }

        if (ResolutionFacetStructureSurface.TryReadPartition(element, out var partition))
        {
            return $"Part({ElementDebug(partition.Source)},{ElementDebug(partition.Count)})";
        }

        if (ResolutionFacetStructureSurface.TryReadUnitResolution(element, out var unitResolution))
        {
            return $"U({ElementDebug(unitResolution.Comparison)})";
        }

        if (ResolutionFacetStructureSurface.TryReadObserverResolution(element, out var observerResolution))
        {
            return $"Read({ElementDebug(observerResolution.Source)},{ElementDebug(observerResolution.Basis)},{ElementDebug(observerResolution.Resolution)})";
        }

        if (ResolutionFacetStructureSurface.TryReadAmbiguousResolution(element, out var ambiguousResolution))
        {
            return $"Read({ElementDebug(ambiguousResolution.Source)},{ElementDebug(ambiguousResolution.Basis)})";
        }

        if (ResolutionFacetStructureSurface.TryReadSlot(element, out var slot))
        {
            return $"Slot({ElementDebug(slot.Role)},?)";
        }

        if (ResolutionFacetStructureSurface.TryReadApproximation(element, out var approximation))
        {
            return $"Approx({ElementDebug(approximation.Comparison)},{ElementDebug(approximation.Resolution)})";
        }

        if (ResolutionFacetStructureSurface.TryReadAmbiguousBasis(element, out var ambiguousBasis))
        {
            return $"B?({ElementDebug(ambiguousBasis.Source)})";
        }

        if (ResolutionFacetStructureSurface.TryReadObserverBasis(element, out var observerBasis))
        {
            return $"B({ElementDebug(observerBasis.Source)})";
        }

        if (ResolutionFacetStructureSurface.TryReadLadderResolution(element, out var ladder))
        {
            return $"Refine({ElementDebug(ladder.RoughRung)},{ElementDebug(ladder.FineRung)},{ElementDebug(ladder.Correspondence)})";
        }

        if (ResolutionFacetStructureSurface.TryReadRung(element, out var rung))
        {
            return $"Rung({ElementDebug(rung.Source)},{ElementDebug(rung.Resolution)})";
        }

        if (ResolutionFacetStructureSurface.TryReadCorrespondence(element, out var correspondence) &&
            ResolutionFacetStructureSurface.TryReadRung(correspondence.Source, out _) &&
            ResolutionFacetStructureSurface.TryReadRung(correspondence.Target, out _))
        {
            return $"Corr({ElementDebug(correspondence.Source)},{ElementDebug(correspondence.Target)})";
        }

        if (ResolutionFacetStructureSurface.TryReadRuntimeResolution(element, out var runtimeResolution))
        {
            return $"Run({ElementDebug(runtimeResolution.Source)},{ElementDebug(runtimeResolution.Input)},{ElementDebug(runtimeResolution.Step)},{ElementDebug(runtimeResolution.Resolution)})";
        }

        if (ResolutionFacetStructureSurface.TryReadRuntimeInput(element, out _))
        {
            return "Input(?)";
        }

        if (ResolutionFacetStructureSurface.TryReadRuntimeStep(element, out var runtimeStep))
        {
            return $"t({runtimeStep.Label})";
        }

        if (ResolutionFacetStructureSurface.TryReadTarget(element, out _))
        {
            return "Target(?)";
        }

        if (ResolutionFacetStructureSurface.TryReadResolutionRung(element, out var resolutionRung))
        {
            return $"Res({resolutionRung.Label})";
        }

        if (ResolutionFacetStructureSurface.TryReadComparison(element, out var comparison))
        {
            var resolution = comparison.Resolution is null
                ? string.Empty
                : $",{ElementDebug(comparison.Resolution)}";
            return $"Comp({ElementDebug(comparison.Source)},{ElementDebug(comparison.Target)}{resolution})";
        }

        if (ConceptDefinitionSurface.TryReadReference(element, out var reference))
        {
            var context = EncodedTextSurface.IsUnknownPoint(reference.Context)
                ? string.Empty
                : $";context={ElementDebug(reference.Context)}";
            var residue = EncodedTextSurface.IsUnknownPoint(reference.ResidueOrTension)
                ? string.Empty
                : $";residue={ElementDebug(reference.ResidueOrTension)}";

            return $"Cr({ElementDebug(reference.DefinitionOrAddress)}{context}{residue})";
        }

        if (UnitShape.TryReadOrthogonalSource(element, out var orthogonalSource))
        {
            return $"F(O({ElementDebug(orthogonalSource)}))";
        }

        if (TryReadGroup(element, out var participants))
        {
            return string.Join(" + ", participants.Select(ElementDebug));
        }

        return "E?";
    }

    private static bool TryReadGroup(IElement element, out IElement[] participants)
    {
        if (element is Relation relation &&
            relation.Right is Carrier holder &&
            relation.TryReadOrderedElementsOn(holder, out participants))
        {
            return true;
        }

        participants = [];
        return false;
    }

    private static bool TryReadPoint(IElement element, out CarrierLocation location)
    {
        if (element is Relation point &&
            point.Left is CarrierLocation pointLocation &&
            point.Right is Carrier carrier &&
            pointLocation.Carrier.SameIdentityAs(carrier))
        {
            location = pointLocation;
            return true;
        }

        location = default;
        return false;
    }

    private static bool TryReadSegment(
        IElement element,
        out CarrierLocation start,
        out CarrierLocation end)
    {
        if (element is Relation segment &&
            TryReadPoint(segment.Left, out start) &&
            TryReadPoint(segment.Right, out end) &&
            start.Carrier.SameIdentityAs(end.Carrier))
        {
            return true;
        }

        start = default;
        end = default;
        return false;
    }
}
