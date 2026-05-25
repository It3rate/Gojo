using Gojo.Core;

namespace Gojo.Structures;

/// <summary>
/// Common relation-shaped read for basis-bearing structure. The selected
/// source, axis/face evidence, lane basis, metric evidence, residue/tension,
/// and provenance are all ordinary Gojo elements.
/// </summary>
public static class BasisReadSurface
{
    /// <summary>
    /// Creates a basis read:
    /// selected source, axis/face surface, lane basis, metric evidence,
    /// residue/tension, provenance.
    /// </summary>
    public static Relation Create(
        Carrier root,
        IElement selected,
        IElement axisOrFaceSurface,
        IElement laneBasis,
        IElement metricEvidence,
        IElement residueOrTension,
        IElement provenance)
    {
        return Relation.OrderedGroup(
            RootOrNew(root),
            selected ?? UnknownPoint(),
            axisOrFaceSurface ?? UnknownPoint(),
            laneBasis ?? UnknownPoint(),
            metricEvidence ?? UnknownPoint(),
            residueOrTension ?? UnknownPoint(),
            provenance ?? UnknownPoint());
    }

    /// <summary>Creates a basis read with unresolved metric evidence and residue.</summary>
    public static Relation Create(
        Carrier root,
        IElement selected,
        IElement axisOrFaceSurface,
        IElement laneBasis,
        IElement provenance) =>
        Create(
            root,
            selected,
            axisOrFaceSurface,
            laneBasis,
            UnknownPoint(),
            UnknownPoint(),
            provenance);

    public static bool TryRead(
        Relation basisRead,
        out IElement selected,
        out IElement axisOrFaceSurface,
        out IElement laneBasis,
        out IElement metricEvidence,
        out IElement residueOrTension,
        out IElement provenance)
    {
        if (TryReadRootElements(basisRead, out var elements) &&
            elements.Length >= 6)
        {
            selected = elements[0];
            axisOrFaceSurface = elements[1];
            laneBasis = elements[2];
            metricEvidence = elements[3];
            residueOrTension = elements[4];
            provenance = elements[5];
            return true;
        }

        selected = default!;
        axisOrFaceSurface = default!;
        laneBasis = default!;
        metricEvidence = default!;
        residueOrTension = default!;
        provenance = default!;
        return false;
    }

    /// <summary>Projects a basis read into the common read-witness skeleton.</summary>
    public static bool TryReadWitness(
        Relation basisRead,
        Carrier root,
        out Relation witness)
    {
        if (TryRead(
                basisRead,
                out var selected,
                out var axisOrFaceSurface,
                out var laneBasis,
                out _,
                out var residueOrTension,
                out var provenance))
        {
            witness = ReadWitness.Create(
                RootOrNew(root),
                selected,
                axisOrFaceSurface,
                laneBasis,
                residueOrTension,
                provenance);
            return true;
        }

        witness = null!;
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
