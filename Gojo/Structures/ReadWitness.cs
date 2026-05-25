using Gojo.Core;

namespace Gojo.Structures;

/// <summary>
/// Common relation-shaped witness for a local read. The helper only builds and
/// reads ordinary Gojo structure; the selected source, read surface, result,
/// residue/tension, and provenance remain inspectable elements.
/// </summary>
public static class ReadWitness
{
    /// <summary>
    /// Creates a read witness:
    /// selected source, read surface, result, residue/tension, provenance.
    /// </summary>
    public static Relation Create(
        Carrier root,
        IElement selected,
        IElement readSurface,
        IElement result,
        IElement residueOrTension,
        IElement provenance)
    {
        return Relation.OrderedGroup(
            RootOrNew(root),
            selected ?? UnknownPoint(),
            readSurface ?? UnknownPoint(),
            result ?? UnknownPoint(),
            residueOrTension ?? UnknownPoint(),
            provenance ?? UnknownPoint());
    }

    /// <summary>Creates a read witness without extra provenance yet.</summary>
    public static Relation Create(
        Carrier root,
        IElement selected,
        IElement readSurface,
        IElement result,
        IElement residueOrTension) =>
        Create(root, selected, readSurface, result, residueOrTension, UnknownPoint());

    /// <summary>Reads a common read witness created by <see cref="Create(Carrier,IElement,IElement,IElement,IElement,IElement)"/>.</summary>
    public static bool TryRead(
        Relation witness,
        out IElement selected,
        out IElement readSurface,
        out IElement result,
        out IElement residueOrTension,
        out IElement provenance)
    {
        if (TryReadRootElements(witness, out var elements) &&
            elements.Length >= 5)
        {
            selected = elements[0];
            readSurface = elements[1];
            result = elements[2];
            residueOrTension = elements[3];
            provenance = elements[4];
            return true;
        }

        selected = default!;
        readSurface = default!;
        result = default!;
        residueOrTension = default!;
        provenance = default!;
        return false;
    }

    /// <summary>Reads an element only when it is a valid common read witness relation.</summary>
    public static bool TryRead(IElement element, out Relation witness)
    {
        if (element is Relation candidate &&
            TryRead(candidate, out _, out _, out _, out _, out _))
        {
            witness = candidate;
            return true;
        }

        witness = null!;
        return false;
    }

    /// <summary>Reads a witness embedded at an ordered-group slot.</summary>
    public static bool TryReadEmbedded(Relation structure, int index, out Relation witness)
    {
        if (index >= 0 &&
            TryReadRootElements(structure, out var elements) &&
            elements.Length > index)
        {
            return TryRead(elements[index], out witness);
        }

        witness = null!;
        return false;
    }

    /// <summary>Reads a witnessed timeline when provenance is a time-step sequence.</summary>
    public static bool TryReadTimeline(Relation witness, out Relation timeline)
    {
        if (TryRead(
                witness,
                out _,
                out _,
                out _,
                out _,
                out var provenance) &&
            provenance is Relation candidate &&
            TimeStepSequence.TryRead(candidate, out _, out _, out _))
        {
            timeline = candidate;
            return true;
        }

        timeline = null!;
        return false;
    }

    private static bool TryReadRootElements(Relation structure, out IElement[] elements)
    {
        if (structure?.Right is Carrier holder &&
            structure.TryReadOrderedElementsOn(holder, out elements))
        {
            return true;
        }

        elements = [];
        return false;
    }

    private static Relation UnknownPoint() =>
        Relation.Point(Carrier.Unknown, 0);

    private static Carrier RootOrNew(Carrier carrier) =>
        carrier ?? new Carrier();
}
