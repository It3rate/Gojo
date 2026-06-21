using System.Text;
using Gojo.Core;
using Gojo.Structures;

namespace GojoCAD.Structures;

/// <summary>
/// Small relation-shaped text surface for documentary ECAD evidence. Text is
/// stored as ordered Unicode scalar values on a local carrier so source names
/// can remain Gojo structure instead of external string metadata.
/// </summary>
public static class EcadTextSurface
{
    public static Relation Create(Carrier root, string? text)
    {
        var codePointAxis = new Carrier();
        var elements = (text ?? string.Empty)
            .EnumerateRunes()
            .Select(rune => (IElement)Relation.Point(codePointAxis, rune.Value))
            .ToArray();

        return Relation.OrderedGroup(RootOrNew(root), elements);
    }

    public static bool TryRead(Relation textSurface, out string text)
    {
        if (!TryReadRootElements(textSurface, out var elements))
        {
            text = string.Empty;
            return false;
        }

        var builder = new StringBuilder();
        foreach (var element in elements)
        {
            if (!TryReadPoint(element, out var location) ||
                location.Ticks < 0 ||
                location.Ticks > int.MaxValue ||
                !Rune.TryCreate((int)location.Ticks, out var rune))
            {
                text = string.Empty;
                return false;
            }

            builder.Append(rune.ToString());
        }

        text = builder.ToString();
        return true;
    }

    internal static Relation UnknownPoint() => Relation.Point(Carrier.Unknown, 0);

    internal static Carrier RootOrNew(Carrier carrier) =>
        carrier is null || carrier.IsUnknown ? new Carrier() : carrier;

    internal static bool TryReadRootElements(Relation structure, out IElement[] elements)
    {
        if (structure?.Right is Carrier holder &&
            structure.TryReadOrderedElementsOn(holder, out elements))
        {
            return true;
        }

        elements = [];
        return false;
    }

    internal static bool TryReadPoint(IElement element, out CarrierLocation location)
    {
        if (element is Relation point &&
            point.Left is CarrierLocation pointLocation &&
            point.Right is Carrier pointCarrier &&
            pointLocation.Carrier.SameIdentityAs(pointCarrier))
        {
            location = pointLocation;
            return true;
        }

        location = default;
        return false;
    }
}

/// <summary>
/// First small ECAD documentary surface: a parsed net-list entry. The helper
/// creates ordinary relation structure for source record, dialect read, net
/// identity, unresolved participants, and residue.
/// </summary>
public static class EcadNetSurface
{
    public static Relation Create(
        Carrier root,
        Carrier netAxis,
        int id,
        string? name,
        IElement? sourceProvenance = null,
        IElement? participants = null,
        IElement? residueOrTension = null)
    {
        var axis = EcadTextSurface.RootOrNew(netAxis);
        var identity = Identity(new Carrier(), axis, id, name);
        var source = SourceRecord(new Carrier(), axis, id, name);
        var dialect = DialectReadSurface(new Carrier());
        var residue = residueOrTension ?? EcadTextSurface.UnknownPoint();
        var witness = ReadWitness.Create(
            new Carrier(),
            source,
            dialect,
            identity,
            residue,
            sourceProvenance ?? EcadTextSurface.UnknownPoint());

        return Relation.OrderedGroup(
            EcadTextSurface.RootOrNew(root),
            identity,
            witness,
            participants ?? EcadTextSurface.UnknownPoint(),
            residue);
    }

    public static Relation Identity(Carrier root, Carrier netAxis, int id, string? name)
    {
        var axis = EcadTextSurface.RootOrNew(netAxis);
        return Relation.OrderedGroup(
            EcadTextSurface.RootOrNew(root),
            Relation.Point(axis, id),
            EcadTextSurface.Create(new Carrier(), name));
    }

    public static Relation SourceRecord(Carrier root, Carrier netAxis, int id, string? name)
    {
        var axis = EcadTextSurface.RootOrNew(netAxis);
        return Relation.OrderedGroup(
            EcadTextSurface.RootOrNew(root),
            EcadTextSurface.Create(new Carrier(), "kicad-pcb"),
            EcadTextSurface.Create(new Carrier(), "net"),
            Relation.Point(axis, id),
            EcadTextSurface.Create(new Carrier(), name));
    }

    public static Relation DialectReadSurface(Carrier root) =>
        Relation.OrderedGroup(
            EcadTextSurface.RootOrNew(root),
            EcadTextSurface.Create(new Carrier(), "kicad-pcb"),
            EcadTextSurface.Create(new Carrier(), "net-list-entry"));

    public static bool TryRead(
        Relation netSurface,
        out CarrierLocation netId,
        out string name,
        out Relation readWitness,
        out IElement participants,
        out IElement residueOrTension)
    {
        if (EcadTextSurface.TryReadRootElements(netSurface, out var elements) &&
            elements.Length >= 4 &&
            elements[0] is Relation identity &&
            TryReadIdentity(identity, out netId, out name) &&
            ReadWitness.TryRead(elements[1], out readWitness))
        {
            participants = elements[2];
            residueOrTension = elements[3];
            return true;
        }

        netId = default;
        name = string.Empty;
        readWitness = null!;
        participants = default!;
        residueOrTension = default!;
        return false;
    }

    public static bool TryReadIdentity(Relation identity, out CarrierLocation netId, out string name)
    {
        if (EcadTextSurface.TryReadRootElements(identity, out var elements) &&
            elements.Length >= 2 &&
            EcadTextSurface.TryReadPoint(elements[0], out netId) &&
            elements[1] is Relation nameSurface &&
            EcadTextSurface.TryRead(nameSurface, out name))
        {
            return true;
        }

        netId = default;
        name = string.Empty;
        return false;
    }

    public static bool TryReadSourceRecord(
        IElement source,
        out string format,
        out string recordKind,
        out CarrierLocation netId,
        out string name)
    {
        if (source is Relation sourceRecord &&
            EcadTextSurface.TryReadRootElements(sourceRecord, out var elements) &&
            elements.Length >= 4 &&
            elements[0] is Relation formatText &&
            elements[1] is Relation kindText &&
            EcadTextSurface.TryRead(formatText, out format) &&
            EcadTextSurface.TryRead(kindText, out recordKind) &&
            EcadTextSurface.TryReadPoint(elements[2], out netId) &&
            elements[3] is Relation nameText &&
            EcadTextSurface.TryRead(nameText, out name))
        {
            return true;
        }

        format = string.Empty;
        recordKind = string.Empty;
        netId = default;
        name = string.Empty;
        return false;
    }

    public static bool TryReadDialectReadSurface(
        IElement readSurface,
        out string format,
        out string recordKind)
    {
        if (readSurface is Relation dialect &&
            EcadTextSurface.TryReadRootElements(dialect, out var elements) &&
            elements.Length >= 2 &&
            elements[0] is Relation formatText &&
            elements[1] is Relation kindText &&
            EcadTextSurface.TryRead(formatText, out format) &&
            EcadTextSurface.TryRead(kindText, out recordKind))
        {
            return true;
        }

        format = string.Empty;
        recordKind = string.Empty;
        return false;
    }
}
