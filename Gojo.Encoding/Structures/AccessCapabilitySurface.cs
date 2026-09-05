using Gojo.Core;
using Gojo.Structures;

namespace Gojo.Encoding.Structures;

/// <summary>
/// Relates an ambient basis to the smaller lane and action surfaces exposed by
/// one local tool. The ambient structure records known possibility; the
/// outward ports record access. Omitted capabilities are inaccessible through
/// this surface, but their omission is not proof that the corresponding
/// mathematics does not exist.
/// </summary>
public static class AccessCapabilitySurface
{
    /// <summary>
    /// Creates an access surface:
    /// ambient-to-exposed relation, action capability ports, blocked-attempt
    /// ports, residue/tension, and provenance.
    /// </summary>
    public static Relation Create(
        Carrier root,
        IElement ambientBasis,
        IElement exposedPorts,
        IElement actionCapabilityPorts,
        IElement blockedAttemptPorts,
        IElement residueOrTension,
        IElement provenance)
    {
        var ambient = ambientBasis ?? UnknownPoint();
        var exposed = exposedPorts ?? TransformSurface.Ports(new Carrier());
        return Relation.OrderedGroup(
            RootOrNew(root),
            new Relation(ambient, exposed),
            actionCapabilityPorts ?? TransformSurface.Ports(new Carrier()),
            blockedAttemptPorts ?? TransformSurface.Ports(new Carrier()),
            residueOrTension ?? UnknownPoint(),
            provenance ?? UnknownPoint());
    }

    /// <summary>Reads only the exact access/capability surface shape.</summary>
    public static bool TryRead(
        Relation surface,
        out IElement ambientBasis,
        out IElement exposedPorts,
        out IElement actionCapabilityPorts,
        out IElement blockedAttemptPorts,
        out IElement residueOrTension,
        out IElement provenance)
    {
        if (TryReadRootElements(surface, out var elements) &&
            elements.Length == 5 &&
            elements[0] is Relation exposure &&
            TransformSurface.TryReadPorts(exposure.Right, out _) &&
            TransformSurface.TryReadPorts(elements[1], out _) &&
            TransformSurface.TryReadPorts(elements[2], out _))
        {
            ambientBasis = exposure.Left;
            exposedPorts = exposure.Right;
            actionCapabilityPorts = elements[1];
            blockedAttemptPorts = elements[2];
            residueOrTension = elements[3];
            provenance = elements[4];
            return true;
        }

        ambientBasis = default!;
        exposedPorts = default!;
        actionCapabilityPorts = default!;
        blockedAttemptPorts = default!;
        residueOrTension = default!;
        provenance = default!;
        return false;
    }

    /// <summary>
    /// Returns only the outward access surfaces. A tool using this read does
    /// not receive the ambient basis merely because its owner retains it.
    /// </summary>
    public static bool TryReadOutwardPorts(
        Relation surface,
        out IElement exposedPorts,
        out IElement actionCapabilityPorts)
    {
        if (TryRead(
                surface,
                out _,
                out var exposed,
                out var capabilities,
                out _,
                out _,
                out _))
        {
            exposedPorts = exposed;
            actionCapabilityPorts = capabilities;
            return true;
        }

        exposedPorts = default!;
        actionCapabilityPorts = default!;
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
