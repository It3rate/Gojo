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
