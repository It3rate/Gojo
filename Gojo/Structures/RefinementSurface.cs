using Gojo.Core;

namespace Gojo.Structures;

/// <summary>
/// Common relation-shaped refinement record. It connects a parameter surface,
/// a binding or candidate choice, a read witness, visible residue/tension, and
/// optional timeline/provenance. It does not own refinement meaning; it only
/// makes the refinement attempt traversable.
/// </summary>
public static class RefinementSurface
{
    /// <summary>
    /// Creates a refinement surface:
    /// parameter, binding, witness, residue/tension, timeline/provenance.
    /// </summary>
    public static Relation Create(
        Carrier root,
        IElement parameter,
        IElement binding,
        Relation witness,
        IElement residueOrTension,
        IElement timelineOrProvenance)
    {
        return Relation.OrderedGroup(
            RootOrNew(root),
            parameter ?? UnknownPoint(),
            binding ?? UnknownPoint(),
            witness ?? UnknownPoint(),
            residueOrTension ?? UnknownPoint(),
            timelineOrProvenance ?? UnknownPoint());
    }

    /// <summary>Creates a refinement surface using the witness residue and no extra timeline.</summary>
    public static Relation Create(
        Carrier root,
        IElement parameter,
        IElement binding,
        Relation witness)
    {
        var residue = ReadWitness.TryRead(
            witness,
            out _,
            out _,
            out _,
            out var witnessResidue,
            out _)
            ? witnessResidue
            : UnknownPoint();

        return Create(root, parameter, binding, witness, residue, UnknownPoint());
    }

    /// <summary>Reads a refinement surface created by <see cref="Create(Carrier,IElement,IElement,Relation,IElement,IElement)"/>.</summary>
    public static bool TryRead(
        Relation refinement,
        out IElement parameter,
        out IElement binding,
        out Relation witness,
        out IElement residueOrTension,
        out IElement timelineOrProvenance)
    {
        if (TryReadRootElements(refinement, out var elements) &&
            elements.Length >= 5 &&
            elements[2] is Relation readWitness &&
            ReadWitness.TryRead(readWitness, out _, out _, out _, out _, out _))
        {
            parameter = elements[0];
            binding = elements[1];
            witness = readWitness;
            residueOrTension = elements[3];
            timelineOrProvenance = elements[4];
            return true;
        }

        parameter = default!;
        binding = default!;
        witness = null!;
        residueOrTension = default!;
        timelineOrProvenance = default!;
        return false;
    }

    /// <summary>Reads the witness embedded in a refinement surface.</summary>
    public static bool TryReadWitness(Relation refinement, out Relation witness)
    {
        if (TryRead(refinement, out _, out _, out witness, out _, out _))
        {
            return true;
        }

        witness = null!;
        return false;
    }

    /// <summary>Reads a timeline when the refinement provenance is a time-step sequence.</summary>
    public static bool TryReadTimeline(Relation refinement, out Relation timeline)
    {
        if (TryRead(
                refinement,
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
