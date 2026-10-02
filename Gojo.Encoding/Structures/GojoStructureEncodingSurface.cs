using Gojo.Core;

namespace Gojo.Encoding.Structures;

/// <summary>
/// Encodes an observed Gojo element tree as relation-shaped integer rows for
/// diagnostics and executable transport experiments. This is not a canonical
/// object encoding: rows describe what this snapshot reached, not everything
/// the object may be or all relations it may have.
/// </summary>
public static class GojoStructureEncodingSurface
{
    private static readonly Carrier NumberCarrier = new();

    public readonly record struct StructureRead(
        string SourceId,
        IElement[] Roots,
        IElement[] Carriers,
        IElement[] TickRefs,
        IElement[] CarrierLocations,
        IElement[] Relations,
        IElement[] UnitShapes,
        IElement OpenedStructure,
        IElement ResidueOrTension)
    {
        public IElement[] Placeholders { get; init; } = [];
    }

    public readonly record struct EncodedStructureRef(long Kind, long Index)
    {
        public bool IsNone => Kind == 0 || Index < 0;
    }

    public readonly record struct EncodedCarrierRow(
        long Index,
        long Id,
        bool IsInverse,
        bool IsUnknown,
        long Value,
        long Tension,
        long UnitShapeIndex,
        long InverseCarrierIndex);

    public readonly record struct EncodedTickRefRow(long Index, long Id, long Ticks);

    public readonly record struct EncodedCarrierLocationRow(
        long Index,
        long CarrierIndex,
        long TickRefIndex);

    public readonly record struct EncodedRelationRow(
        long Index,
        long Id,
        EncodedStructureRef Left,
        EncodedStructureRef Right);

    public readonly record struct EncodedUnitShapeRow(
        long Index,
        long Id,
        EncodedStructureRef Surface)
    {
        public IElement[] Descriptors { get; init; } = [];
    }

    public readonly record struct EncodedPlaceholderRow(long Index, long SlotIndex, bool IsInverse);

    public static Relation CreateDiagnosticSnapshot(
        Carrier root,
        string sourceId,
        IElement structure,
        IElement? residueOrTension = null)
    {
        var openedStructure = structure ?? EncodedTextSurface.UnknownPoint();
        return CreateFromSnapshot(
            root,
            sourceId,
            GojoEncodedSnapshot.Capture(openedStructure),
            openedStructure,
            residueOrTension);
    }

    public static Relation CreateDiagnosticWorldSnapshot(
        Carrier root,
        string sourceId,
        Carrier anchor,
        IElement? residueOrTension = null)
    {
        var openedStructure = anchor ?? Carrier.Unknown;
        return CreateFromSnapshot(
            root,
            sourceId,
            GojoEncodedSnapshot.CaptureWorld(openedStructure),
            openedStructure,
            residueOrTension);
    }

    [Obsolete("Creates a diagnostic snapshot, not a canonical object encoding. Use CreateDiagnosticSnapshot to make that intent explicit.")]
    public static Relation Create(
        Carrier root,
        string sourceId,
        IElement structure,
        IElement? residueOrTension = null) =>
        CreateDiagnosticSnapshot(root, sourceId, structure, residueOrTension);

    [Obsolete("Creates a diagnostic world snapshot, not a canonical object encoding. Use CreateDiagnosticWorldSnapshot to make that intent explicit.")]
    public static Relation CreateWorld(
        Carrier root,
        string sourceId,
        Carrier anchor,
        IElement? residueOrTension = null) =>
        CreateDiagnosticWorldSnapshot(root, sourceId, anchor, residueOrTension);

    public static bool TryRead(IElement element, out StructureRead read)
    {
        if (element is Relation surface &&
            EncodedTextSurface.TryReadRootElements(surface, out var elements) &&
            elements.Length >= 9 &&
            EncodedTextSurface.TryRead(elements[0], out var sourceId) &&
            TryReadFrame(elements[1], out var roots) &&
            TryReadFrame(elements[2], out var carriers) &&
            TryReadFrame(elements[3], out var tickRefs) &&
            TryReadFrame(elements[4], out var carrierLocations) &&
            TryReadFrame(elements[5], out var relations) &&
            TryReadFrame(elements[6], out var unitShapes))
        {
            read = new StructureRead(
                sourceId,
                roots,
                carriers,
                tickRefs,
                carrierLocations,
                relations,
                unitShapes,
                elements[7],
                elements[8]);
            if (elements.Length > 9)
            {
                if (!TryReadFrame(elements[9], out var placeholders)) return false;
                read = read with { Placeholders = placeholders };
            }
            return true;
        }

        read = default;
        return false;
    }

    public static bool TryReadRef(IElement element, out EncodedStructureRef reference)
    {
        if (TryReadRow(element, out var row) &&
            row.Length >= 2 &&
            TryReadNumber(row[0], out var kind) &&
            TryReadNumber(row[1], out var index))
        {
            reference = new EncodedStructureRef(kind, index);
            return true;
        }

        reference = default;
        return false;
    }

    public static bool TryReadCarrierRow(IElement element, out EncodedCarrierRow carrier)
    {
        if (TryReadRow(element, out var row) &&
            row.Length >= 8 &&
            TryReadNumber(row[0], out var index) &&
            TryReadNumber(row[1], out var id) &&
            TryReadNumber(row[2], out var isInverse) &&
            TryReadNumber(row[3], out var isUnknown) &&
            TryReadNumber(row[4], out var value) &&
            TryReadNumber(row[5], out var tension) &&
            TryReadNumber(row[6], out var unitShapeIndex) &&
            TryReadNumber(row[7], out var inverseCarrierIndex))
        {
            carrier = new EncodedCarrierRow(
                index,
                id,
                isInverse != 0,
                isUnknown != 0,
                value,
                tension,
                unitShapeIndex,
                inverseCarrierIndex);
            return true;
        }

        carrier = default;
        return false;
    }

    public static bool TryReadTickRefRow(IElement element, out EncodedTickRefRow tickRef)
    {
        if (TryReadRow(element, out var row) &&
            row.Length >= 3 &&
            TryReadNumber(row[0], out var index) &&
            TryReadNumber(row[1], out var id) &&
            TryReadNumber(row[2], out var ticks))
        {
            tickRef = new EncodedTickRefRow(index, id, ticks);
            return true;
        }

        tickRef = default;
        return false;
    }

    public static bool TryReadCarrierLocationRow(
        IElement element,
        out EncodedCarrierLocationRow carrierLocation)
    {
        if (TryReadRow(element, out var row) &&
            row.Length >= 3 &&
            TryReadNumber(row[0], out var index) &&
            TryReadNumber(row[1], out var carrierIndex) &&
            TryReadNumber(row[2], out var tickRefIndex))
        {
            carrierLocation = new EncodedCarrierLocationRow(index, carrierIndex, tickRefIndex);
            return true;
        }

        carrierLocation = default;
        return false;
    }

    public static bool TryReadRelationRow(IElement element, out EncodedRelationRow relation)
    {
        if (TryReadRow(element, out var row) &&
            row.Length >= 4 &&
            TryReadNumber(row[0], out var index) &&
            TryReadNumber(row[1], out var id) &&
            TryReadRef(row[2], out var left) &&
            TryReadRef(row[3], out var right))
        {
            relation = new EncodedRelationRow(index, id, left, right);
            return true;
        }

        relation = default;
        return false;
    }

    public static bool TryReadUnitShapeRow(IElement element, out EncodedUnitShapeRow unitShape)
    {
        if (TryReadRow(element, out var row) &&
            row.Length >= 3 &&
            TryReadNumber(row[0], out var index) &&
            TryReadNumber(row[1], out var id) &&
            TryReadRef(row[2], out var surface))
        {
            unitShape = new EncodedUnitShapeRow(index, id, surface);
            if (row.Length > 3)
            {
                if (!TryReadFrame(row[3], out var descriptors)) return false;
                unitShape = unitShape with { Descriptors = descriptors };
            }
            return true;
        }

        unitShape = default;
        return false;
    }

    public static bool TryReadPlaceholderRow(IElement element, out EncodedPlaceholderRow placeholder)
    {
        if (TryReadRow(element, out var row) && row.Length == 3 &&
            TryReadNumber(row[0], out var index) && TryReadNumber(row[1], out var slot) &&
            TryReadNumber(row[2], out var face) && face is 0 or 1)
        {
            placeholder = new(index, slot, face != 0);
            return true;
        }
        placeholder = default;
        return false;
    }

    private static Relation CreateFromSnapshot(
        Carrier root,
        string sourceId,
        GojoEncodedSnapshot snapshot,
        IElement openedStructure,
        IElement? residueOrTension) =>
        Relation.OrderedGroup(
            EncodedTextSurface.RootOrNew(root),
            EncodedTextSurface.Create(new Carrier(), sourceId),
            Frame(snapshot.Roots.Select(EncodeRef)),
            Frame(snapshot.Carriers.Select(EncodeCarrierRow)),
            Frame(snapshot.TickRefs.Select(EncodeTickRefRow)),
            Frame(snapshot.CarrierLocations.Select(EncodeCarrierLocationRow)),
            Frame(snapshot.Relations.Select(EncodeRelationRow)),
            Frame(snapshot.UnitShapes.Select(EncodeUnitShapeRow)),
            openedStructure,
            residueOrTension ?? EncodedTextSurface.UnknownPoint(),
            Frame(snapshot.Placeholders.Select(slot => Row(Number(slot.Index), Number(slot.SlotIndex),
                Number(slot.IsInverse ? 1 : 0)))));

    private static Relation EncodeRef(GojoEncodedElementRef reference) =>
        Row(Number((long)reference.Kind), Number(reference.Index));

    private static Relation EncodeCarrierRow(GojoEncodedCarrier carrier) =>
        Row(
            Number(carrier.Index),
            Number(ToSigned(carrier.Id)),
            Number(carrier.IsInverse ? 1 : 0),
            Number(carrier.IsUnknown ? 1 : 0),
            Number(ToSigned(carrier.Value)),
            Number(carrier.Tension),
            Number(carrier.UnitShapeIndex),
            Number(carrier.InverseCarrierIndex));

    private static Relation EncodeTickRefRow(GojoEncodedTickRef tickRef) =>
        Row(
            Number(tickRef.Index),
            Number(ToSigned(tickRef.Id)),
            Number(tickRef.Ticks));

    private static Relation EncodeCarrierLocationRow(GojoEncodedCarrierLocation carrierLocation) =>
        Row(
            Number(carrierLocation.Index),
            Number(carrierLocation.CarrierIndex),
            Number(carrierLocation.TickRefIndex));

    private static Relation EncodeRelationRow(GojoEncodedRelation relation) =>
        Row(
            Number(relation.Index),
            Number(ToSigned(relation.Id)),
            EncodeRef(relation.Left),
            EncodeRef(relation.Right));

    private static Relation EncodeUnitShapeRow(GojoEncodedUnitShape unitShape) =>
        Row(
            Number(unitShape.Index),
            Number(ToSigned(unitShape.Id)),
            EncodeRef(unitShape.Surface),
            Frame(unitShape.Descriptors.Select(EncodeRef)));

    private static Relation Frame(IEnumerable<IElement> elements) =>
        Relation.OrderedGroup(new Carrier(), [.. elements]);

    private static Relation Row(params IElement[] elements) =>
        Relation.OrderedGroup(new Carrier(), elements);

    private static Relation Number(long value) =>
        Relation.Point(NumberCarrier, value);

    private static bool TryReadFrame(IElement element, out IElement[] rows) =>
        TryReadRow(element, out rows);

    private static bool TryReadRow(IElement element, out IElement[] row)
    {
        if (element is Relation relation &&
            EncodedTextSurface.TryReadRootElements(relation, out row))
        {
            return true;
        }

        row = [];
        return false;
    }

    private static bool TryReadNumber(IElement element, out long value)
    {
        if (EncodedTextSurface.TryReadPoint(element, out var location))
        {
            value = location.Ticks;
            return true;
        }

        value = 0;
        return false;
    }

    private static long ToSigned(ulong value) =>
        checked((long)value);
}
