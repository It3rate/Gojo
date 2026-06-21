using Gojo.Core;

namespace Gojo.Encoding;

/// <summary>
/// Variable-length semantic address surface. Addresses are ordinary Gojo
/// relation structure: atoms can be composed into larger concepts with context
/// and residue while keeping a debug-readable lexical surface.
/// </summary>
public static class ConceptAddressSurface
{
    private const string AtomMarker = "gojo.address.atom";
    private const string CompoundMarker = "gojo.address.compound";

    public static Relation Atom(Carrier root, string name, IElement? residueOrTension = null) =>
        Relation.OrderedGroup(
            EncodedTextSurface.RootOrNew(root),
            EncodedTextSurface.Create(new Carrier(), AtomMarker),
            EncodedTextSurface.Create(new Carrier(), name),
            residueOrTension ?? EncodedTextSurface.UnknownPoint());

    public static Relation Compound(
        Carrier root,
        string lexical,
        IReadOnlyList<IElement> parts,
        IReadOnlyList<IElement>? contexts = null,
        IElement? residueOrTension = null) =>
        Relation.OrderedGroup(
            EncodedTextSurface.RootOrNew(root),
            EncodedTextSurface.Create(new Carrier(), CompoundMarker),
            EncodedTextSurface.Create(new Carrier(), lexical),
            Frame(new Carrier(), parts ?? []),
            Frame(new Carrier(), contexts ?? []),
            residueOrTension ?? EncodedTextSurface.UnknownPoint());

    public static Relation Frame(Carrier root, IReadOnlyList<IElement> elements) =>
        Relation.OrderedGroup(EncodedTextSurface.RootOrNew(root), [.. elements]);

    public static bool TryReadAtom(
        IElement element,
        out string name,
        out IElement residueOrTension)
    {
        if (element is Relation atom &&
            EncodedTextSurface.TryReadRootElements(atom, out var elements) &&
            elements.Length >= 3 &&
            IsMarker(elements[0], AtomMarker) &&
            EncodedTextSurface.TryRead(elements[1], out name))
        {
            residueOrTension = elements[2];
            return true;
        }

        name = string.Empty;
        residueOrTension = default!;
        return false;
    }

    public static bool TryReadCompound(
        IElement element,
        out string lexical,
        out IElement[] parts,
        out IElement[] contexts,
        out IElement residueOrTension)
    {
        if (element is Relation compound &&
            EncodedTextSurface.TryReadRootElements(compound, out var elements) &&
            elements.Length >= 5 &&
            IsMarker(elements[0], CompoundMarker) &&
            EncodedTextSurface.TryRead(elements[1], out lexical) &&
            elements[2] is Relation partFrame &&
            elements[3] is Relation contextFrame &&
            EncodedTextSurface.TryReadRootElements(partFrame, out parts) &&
            EncodedTextSurface.TryReadRootElements(contextFrame, out contexts))
        {
            residueOrTension = elements[4];
            return true;
        }

        lexical = string.Empty;
        parts = [];
        contexts = [];
        residueOrTension = default!;
        return false;
    }

    public static string ToDebugString(IElement address)
    {
        if (TryReadAtom(address, out var atomName, out var atomResidue))
        {
            return EncodedTextSurface.IsUnknownPoint(atomResidue)
                ? atomName
                : $"{atomName}{{residue:{ResidueDebug(atomResidue)}}}";
        }

        if (TryReadCompound(
                address,
                out var lexical,
                out var parts,
                out var contexts,
                out var residue))
        {
            var partText = parts.Length == 0
                ? string.Empty
                : string.Join(",", parts.Select(ToDebugString));
            var contextText = contexts.Length == 0
                ? string.Empty
                : $";context={string.Join(",", contexts.Select(ToDebugString))}";
            var residueText = EncodedTextSurface.IsUnknownPoint(residue)
                ? string.Empty
                : $";residue={ResidueDebug(residue)}";

            return $"{lexical}({partText}{contextText}{residueText})";
        }

        return "?unread-address";
    }

    private static bool IsMarker(IElement element, string expected) =>
        EncodedTextSurface.TryRead(element, out var marker) &&
        marker == expected;

    private static string ResidueDebug(IElement residue) =>
        TryReadAtom(residue, out var atom, out _)
            ? atom
            : TryReadCompound(residue, out _, out _, out _, out _)
                ? ToDebugString(residue)
                : "?";
}
