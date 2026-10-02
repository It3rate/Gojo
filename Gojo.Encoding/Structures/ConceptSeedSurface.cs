using Gojo.Core;

namespace Gojo.Encoding.Structures;

/// <summary>
/// A small concept seed surface: readable text, compact hint signature,
/// optional opened Gojo structure, and residue/tension. The surface is read by
/// field shape rather than by a symbolic marker.
/// </summary>
public static class ConceptSeedSurface
{
    public readonly record struct SeedRead(
        string Name,
        IElement[] Hints,
        IElement OpenedStructure,
        IElement ResidueOrTension);

    public static Relation Create(
        Carrier root,
        string name,
        IReadOnlyList<IElement> hints,
        IElement? openedStructure = null,
        IElement? residueOrTension = null) =>
        Relation.OrderedGroup(
            EncodedTextSurface.RootOrNew(root),
            EncodedTextSurface.Create(new Carrier(), name),
            ConceptHintSurface.Signature(new Carrier(), [.. hints]),
            openedStructure ?? EncodedTextSurface.UnknownPoint(),
            residueOrTension ?? EncodedTextSurface.UnknownPoint());

    public static bool TryRead(IElement element, out SeedRead read)
    {
        if (element is Relation seed &&
            EncodedTextSurface.TryReadRootElements(seed, out var elements) &&
            elements.Length >= 4 &&
            EncodedTextSurface.TryRead(elements[0], out var name) &&
            ConceptHintSurface.TryReadSignature(elements[1], out var hints))
        {
            read = new SeedRead(name, hints, elements[2], elements[3]);
            return true;
        }

        read = default;
        return false;
    }

    public static bool TryPackQuerySignature(IElement seed, out uint[] tokens)
    {
        if (!TryRead(seed, out var read))
        {
            tokens = [];
            return false;
        }

        var packed = new List<uint>(read.Hints.Length);
        foreach (var hint in read.Hints)
        {
            if (!ConceptHintSurface.TryPackQueryToken(hint, out var token))
            {
                tokens = [];
                return false;
            }

            packed.Add(token);
        }

        tokens = [.. packed];
        return true;
    }

    public static bool SharesAnyHintAxis(IElement left, IElement right)
    {
        if (!TryRead(left, out var leftRead) ||
            !TryRead(right, out var rightRead))
        {
            return false;
        }

        return leftRead.Hints.Any(leftHint =>
            rightRead.Hints.Any(rightHint => ConceptHintSurface.SharesAxis(leftHint, rightHint)));
    }
}
