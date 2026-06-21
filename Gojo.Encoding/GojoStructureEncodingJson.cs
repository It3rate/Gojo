using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Gojo.Core;

namespace Gojo.Encoding;

/// <summary>
/// Text adapter for encoded Gojo structure bundles. The JSON document is a
/// readable interchange view over the encoding surfaces, not the source of
/// in-memory Gojo meaning.
/// </summary>
public static class GojoStructureEncodingJson
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault
    };

    public static GojoEncodingJsonBundle BundleFromSurface(IElement bundle)
    {
        if (!GojoStructureEncodingBundleSurface.TryRead(bundle, out var read))
        {
            throw new InvalidOperationException("The supplied element is not a structure encoding bundle.");
        }

        return new GojoEncodingJsonBundle
        {
            SourceId = read.SourceId,
            Entries = [.. read.Entries.Select(EntryFromSurface)],
            ResidueOrTension = DescribeElement(read.ResidueOrTension)
        };
    }

    public static GojoEncodingJsonStructure StructureFromSurface(IElement structureEncoding)
    {
        if (!GojoStructureEncodingSurface.TryRead(structureEncoding, out var read))
        {
            throw new InvalidOperationException("The supplied element is not a structure encoding surface.");
        }

        var structure = new GojoEncodingJsonStructure
        {
            SourceId = read.SourceId,
            Roots = [.. read.Roots.Select(ReadRef)],
            Carriers = [.. read.Carriers.Select(ReadCarrierRow)],
            TickRefs = [.. read.TickRefs.Select(ReadTickRefRow)],
            CarrierLocations = [.. read.CarrierLocations.Select(ReadCarrierLocationRow)],
            Relations = [.. read.Relations.Select(ReadRelationRow)],
            UnitShapes = [.. read.UnitShapes.Select(ReadUnitShapeRow)],
            OpenedStructure = DescribeElement(read.OpenedStructure),
            ResidueOrTension = DescribeElement(read.ResidueOrTension)
        };
        structure.Counts = CountsOf(structure);
        return structure;
    }

    public static string Serialize(GojoEncodingJsonBundle bundle) =>
        JsonSerializer.Serialize(bundle, JsonOptions);

    public static string SerializeEntry(GojoEncodingJsonBundleEntry entry) =>
        JsonSerializer.Serialize(entry, JsonOptions);

    public static string SerializeEntries(IReadOnlyList<GojoEncodingJsonBundleEntry> entries) =>
        JsonSerializer.Serialize(entries, JsonOptions);

    public static GojoEncodingJsonBundle DeserializeBundle(string json) =>
        JsonSerializer.Deserialize<GojoEncodingJsonBundle>(json, JsonOptions) ??
        throw new InvalidOperationException("Could not deserialize Gojo encoding JSON bundle.");

    public static GojoEncodingJsonBundleEntry DeserializeEntry(string json) =>
        JsonSerializer.Deserialize<GojoEncodingJsonBundleEntry>(json, JsonOptions) ??
        throw new InvalidOperationException("Could not deserialize Gojo encoding JSON bundle entry.");

    public static List<GojoEncodingJsonBundleEntry> DeserializeEntries(string json) =>
        JsonSerializer.Deserialize<List<GojoEncodingJsonBundleEntry>>(json, JsonOptions) ??
        throw new InvalidOperationException("Could not deserialize Gojo encoding JSON bundle entries.");

    public static string SerializeStructure(GojoEncodingJsonStructure structure) =>
        JsonSerializer.Serialize(structure, JsonOptions);

    public static GojoEncodingJsonStructure DeserializeStructure(string json) =>
        JsonSerializer.Deserialize<GojoEncodingJsonStructure>(json, JsonOptions) ??
        throw new InvalidOperationException("Could not deserialize Gojo encoding JSON structure.");

    public static GojoEncodingJsonBundleEntry EntryFromSurface(IElement entry)
    {
        if (!GojoStructureEncodingBundleSurface.TryReadEntry(entry, out var read))
        {
            throw new InvalidOperationException("The supplied element is not a structure encoding bundle entry.");
        }

        return new GojoEncodingJsonBundleEntry
        {
            EntryId = read.EntryId,
            Expression = read.Expression,
            EncodingNote = string.IsNullOrEmpty(read.EncodingNote) ? null : read.EncodingNote,
            Structure = StructureFromSurface(read.StructureEncoding),
            ObserverPaths = [.. read.ObserverPaths.Select(ObserverPathFromSurface)],
            ResidueOrTension = DescribeElement(read.ResidueOrTension)
        };
    }

    private static GojoEncodingJsonObserverPath ObserverPathFromSurface(Relation path)
    {
        if (!GojoObserverPathSurface.TryRead(path, out var read))
        {
            throw new InvalidOperationException("The supplied relation is not an observer path.");
        }

        return new GojoEncodingJsonObserverPath
        {
            SelectedSource = DescribeElement(read.SelectedSource),
            ObserverSurface = ObserverSurfaceFromElement(read.ObserverSurface),
            Steps = [.. read.Steps.Select(ObserverStepFromSurface)],
            Result = DescribeElement(read.Result),
            ResidueOrTension = DescribeElement(read.ResidueOrTension),
            Provenance = DescribeElement(read.Provenance)
        };
    }

    private static GojoEncodingJsonObserverStep ObserverStepFromSurface(Relation step)
    {
        if (!GojoObserverPathSurface.TryReadStep(step, out var read))
        {
            throw new InvalidOperationException("The supplied relation is not an observer path step.");
        }

        return new GojoEncodingJsonObserverStep
        {
            From = DescribeElement(read.From),
            Through = DescribeElement(read.Through),
            To = DescribeElement(read.To),
            Evidence = DescribeElement(read.Evidence),
            ResidueOrTension = DescribeElement(read.ResidueOrTension)
        };
    }

    private static GojoEncodingJsonObserverSurface ObserverSurfaceFromElement(IElement surface)
    {
        var observer = new GojoEncodingJsonObserverSurface();

        if (surface is Relation relation &&
            EncodedTextSurface.TryReadRootElements(relation, out var elements) &&
            elements.Length >= 4)
        {
            observer.Basis = DescribeElement(elements[1]);
            observer.Resolution = IsUnknown(elements[2]) ? null : DescribeElement(elements[2]);
            observer.Intent = IsUnknown(elements[3]) ? null : DescribeElement(elements[3]);
        }

        return observer;
    }

    private static GojoEncodingJsonElement DescribeElement(IElement element)
    {
        if (EncodedTextSurface.IsUnknownPoint(element))
        {
            return new GojoEncodingJsonElement { Kind = "unknown-point", IsUnknown = true };
        }

        if (EncodedTextSurface.TryRead(element, out var text))
        {
            return new GojoEncodingJsonElement { Kind = "text", Text = text };
        }

        if (GojoStructureEncodingSurface.TryRead(element, out var structure))
        {
            return new GojoEncodingJsonElement
            {
                Kind = "structure-encoding",
                SourceId = structure.SourceId,
                Counts = new GojoEncodingJsonStructureCounts
                {
                    Roots = structure.Roots.Length,
                    Carriers = structure.Carriers.Length,
                    TickRefs = structure.TickRefs.Length,
                    CarrierLocations = structure.CarrierLocations.Length,
                    Relations = structure.Relations.Length,
                    UnitShapes = structure.UnitShapes.Length
                }
            };
        }

        if (GojoObserverPathSurface.TryRead(element, out var path))
        {
            return new GojoEncodingJsonElement
            {
                Kind = "observer-path",
                Counts = new GojoEncodingJsonStructureCounts { Relations = path.Steps.Length }
            };
        }

        if (GojoStructureEncodingSurface.TryReadRelationRow(element, out var relationRow))
        {
            return new GojoEncodingJsonElement { Kind = "relation-row", RelationRow = ToJson(relationRow) };
        }

        if (GojoStructureEncodingSurface.TryReadCarrierRow(element, out var carrierRow))
        {
            return new GojoEncodingJsonElement { Kind = "carrier-row", CarrierRow = ToJson(carrierRow) };
        }

        if (GojoStructureEncodingSurface.TryReadTickRefRow(element, out var tickRefRow))
        {
            return new GojoEncodingJsonElement { Kind = "tick-ref-row", TickRefRow = ToJson(tickRefRow) };
        }

        if (GojoStructureEncodingSurface.TryReadCarrierLocationRow(element, out var carrierLocationRow))
        {
            return new GojoEncodingJsonElement { Kind = "carrier-location-row", CarrierLocationRow = ToJson(carrierLocationRow) };
        }

        if (GojoStructureEncodingSurface.TryReadUnitShapeRow(element, out var unitShapeRow))
        {
            return new GojoEncodingJsonElement { Kind = "unit-shape-row", UnitShapeRow = ToJson(unitShapeRow) };
        }

        if (GojoStructureEncodingSurface.TryReadRef(element, out var reference))
        {
            return new GojoEncodingJsonElement { Kind = "ref", Ref = ToJson(reference) };
        }

        return element switch
        {
            Carrier carrier => new GojoEncodingJsonElement
            {
                Kind = "carrier",
                IsUnknown = TrueOrNull(carrier.IsUnknown)
            },
            CarrierLocation location => new GojoEncodingJsonElement
            {
                Kind = "carrier-location",
                Ticks = NonZero(location.Ticks),
                IsUnknown = TrueOrNull(location.Carrier.IsUnknown)
            },
            TickRef tick => new GojoEncodingJsonElement
            {
                Kind = "tick-ref",
                Ticks = NonZero(tick.Ticks)
            },
            Relation relation => new GojoEncodingJsonElement
            {
                Kind = "relation"
            },
            _ => new GojoEncodingJsonElement { Kind = "element" }
        };
    }

    private static GojoEncodingJsonElementRef ReadRef(IElement element)
    {
        if (!GojoStructureEncodingSurface.TryReadRef(element, out var reference))
        {
            throw new InvalidOperationException("Could not read encoded element reference.");
        }

        return ToJson(reference);
    }

    private static GojoEncodingJsonCarrierRow ReadCarrierRow(IElement element)
    {
        if (!GojoStructureEncodingSurface.TryReadCarrierRow(element, out var row))
        {
            throw new InvalidOperationException("Could not read encoded carrier row.");
        }

        return ToJson(row);
    }

    private static GojoEncodingJsonTickRefRow ReadTickRefRow(IElement element)
    {
        if (!GojoStructureEncodingSurface.TryReadTickRefRow(element, out var row))
        {
            throw new InvalidOperationException("Could not read encoded tick-ref row.");
        }

        return ToJson(row);
    }

    private static GojoEncodingJsonCarrierLocationRow ReadCarrierLocationRow(IElement element)
    {
        if (!GojoStructureEncodingSurface.TryReadCarrierLocationRow(element, out var row))
        {
            throw new InvalidOperationException("Could not read encoded carrier-location row.");
        }

        return ToJson(row);
    }

    private static GojoEncodingJsonRelationRow ReadRelationRow(IElement element)
    {
        if (!GojoStructureEncodingSurface.TryReadRelationRow(element, out var row))
        {
            throw new InvalidOperationException("Could not read encoded relation row.");
        }

        return ToJson(row);
    }

    private static GojoEncodingJsonUnitShapeRow ReadUnitShapeRow(IElement element)
    {
        if (!GojoStructureEncodingSurface.TryReadUnitShapeRow(element, out var row))
        {
            throw new InvalidOperationException("Could not read encoded unit-shape row.");
        }

        return ToJson(row);
    }

    private static GojoEncodingJsonElementRef ToJson(GojoStructureEncodingSurface.EncodedStructureRef reference) =>
        new()
        {
            Kind = RefKindName(reference.Kind),
            Index = PositiveIndexOrNull(reference.Index)
        };

    private static GojoEncodingJsonCarrierRow ToJson(GojoStructureEncodingSurface.EncodedCarrierRow row) =>
        new()
        {
            Id = LocalId(row.Index),
            IsInverse = row.IsInverse,
            IsUnknown = row.IsUnknown,
            Value = NonZero(row.Value),
            Tension = NonZero(row.Tension),
            UnitShape = PositiveLocalRefOrNull(row.UnitShapeIndex),
            InverseCarrier = PositiveLocalRefOrNull(row.InverseCarrierIndex)
        };

    private static GojoEncodingJsonTickRefRow ToJson(GojoStructureEncodingSurface.EncodedTickRefRow row) =>
        new() { Id = LocalId(row.Index), Ticks = NonZero(row.Ticks) };

    private static GojoEncodingJsonCarrierLocationRow ToJson(GojoStructureEncodingSurface.EncodedCarrierLocationRow row) =>
        new()
        {
            Id = LocalId(row.Index),
            Carrier = PositiveLocalRefOrNull(row.CarrierIndex),
            TickRef = PositiveLocalRefOrNull(row.TickRefIndex)
        };

    private static GojoEncodingJsonRelationRow ToJson(GojoStructureEncodingSurface.EncodedRelationRow row) =>
        new() { Id = LocalId(row.Index), Left = ToJson(row.Left), Right = ToJson(row.Right) };

    private static GojoEncodingJsonUnitShapeRow ToJson(GojoStructureEncodingSurface.EncodedUnitShapeRow row) =>
        new() { Id = LocalId(row.Index), Surface = ToJson(row.Surface) };

    private static GojoEncodingJsonStructureCounts CountsOf(GojoEncodingJsonStructure structure) =>
        new()
        {
            Roots = structure.Roots.Count,
            Carriers = structure.Carriers.Count,
            TickRefs = structure.TickRefs.Count,
            CarrierLocations = structure.CarrierLocations.Count,
            Relations = structure.Relations.Count,
            UnitShapes = structure.UnitShapes.Count
        };

    private static bool? TrueOrNull(bool value) =>
        value ? true : null;

    private static bool IsUnknown(IElement element) =>
        EncodedTextSurface.IsUnknownPoint(element);

    private static long? NonZero(long value) =>
        value == 0 ? null : value;

    private static long? NonZero(ulong value) =>
        value == 0 ? null : checked((long)value);

    private static long LocalId(long index) =>
        index + 1;

    private static long? PositiveLocalRefOrNull(long index) =>
        index < 0 ? null : index + 1;

    private static long? PositiveIndexOrNull(long index) =>
        index <= 0 ? null : index;

    private static string RefKindName(long kind) =>
        kind >= byte.MinValue &&
        kind <= byte.MaxValue &&
        Enum.IsDefined(typeof(GojoEncodedElementKind), (GojoEncodedElementKind)kind)
            ? RefKindName((GojoEncodedElementKind)kind)
            : $"kind-{kind}";

    private static string RefKindName(GojoEncodedElementKind kind) =>
        kind switch
        {
            GojoEncodedElementKind.None => "none",
            GojoEncodedElementKind.Carrier => "carrier",
            GojoEncodedElementKind.TickRef => "tick-ref",
            GojoEncodedElementKind.CarrierLocation => "carrier-location",
            GojoEncodedElementKind.Relation => "relation",
            GojoEncodedElementKind.UnitShape => "unit-shape",
            _ => $"kind-{(long)kind}"
        };
}

public sealed record GojoEncodingJsonBundle
{
    public string SourceId { get; init; } = string.Empty;

    public List<GojoEncodingJsonBundleEntry> Entries { get; init; } = [];

    public GojoEncodingJsonElement ResidueOrTension { get; init; } = new();
}

public sealed record GojoEncodingJsonBundleEntry
{
    public string? Represents { get; init; }

    public string EntryId { get; init; } = string.Empty;

    public string Expression { get; init; } = string.Empty;

    public string? EncodingNote { get; init; }

    public GojoEncodingJsonStructure Structure { get; init; } = new();

    public List<GojoEncodingJsonObserverPath> ObserverPaths { get; init; } = [];

    public GojoEncodingJsonElement ResidueOrTension { get; init; } = new();
}

public sealed record GojoEncodingJsonStructure
{
    public string SourceId { get; init; } = string.Empty;

    public GojoEncodingJsonStructureCounts Counts { get; set; } = new();

    public List<GojoEncodingJsonElementRef> Roots { get; init; } = [];

    public List<GojoEncodingJsonCarrierRow> Carriers { get; init; } = [];

    public List<GojoEncodingJsonTickRefRow> TickRefs { get; init; } = [];

    public List<GojoEncodingJsonCarrierLocationRow> CarrierLocations { get; init; } = [];

    public List<GojoEncodingJsonRelationRow> Relations { get; init; } = [];

    public List<GojoEncodingJsonUnitShapeRow> UnitShapes { get; init; } = [];

    public GojoEncodingJsonElement OpenedStructure { get; init; } = new();

    public GojoEncodingJsonElement ResidueOrTension { get; init; } = new();
}

public sealed record GojoEncodingJsonObserverPath
{
    public GojoEncodingJsonElement SelectedSource { get; init; } = new();

    public GojoEncodingJsonObserverSurface ObserverSurface { get; init; } = new();

    public List<GojoEncodingJsonObserverStep> Steps { get; init; } = [];

    public GojoEncodingJsonElement Result { get; init; } = new();

    public GojoEncodingJsonElement ResidueOrTension { get; init; } = new();

    public GojoEncodingJsonElement Provenance { get; init; } = new();
}

public sealed record GojoEncodingJsonObserverSurface
{
    public GojoEncodingJsonElement? Anchor { get; set; }

    public GojoEncodingJsonElement? Basis { get; set; }

    public GojoEncodingJsonElement? Resolution { get; set; }

    public GojoEncodingJsonElement? Intent { get; set; }
}

public sealed record GojoEncodingJsonObserverStep
{
    public GojoEncodingJsonElement From { get; init; } = new();

    public GojoEncodingJsonElement Through { get; init; } = new();

    public GojoEncodingJsonElement To { get; init; } = new();

    public GojoEncodingJsonElement Evidence { get; init; } = new();

    public GojoEncodingJsonElement ResidueOrTension { get; init; } = new();
}

public sealed record GojoEncodingJsonElement
{
    public string Kind { get; init; } = string.Empty;

    public string? Text { get; init; }

    public string? SourceId { get; init; }

    public bool? IsUnknown { get; init; }

    public long? Ticks { get; init; }

    public GojoEncodingJsonStructureCounts? Counts { get; init; }

    public GojoEncodingJsonElementRef? Ref { get; init; }

    public GojoEncodingJsonCarrierRow? CarrierRow { get; init; }

    public GojoEncodingJsonTickRefRow? TickRefRow { get; init; }

    public GojoEncodingJsonCarrierLocationRow? CarrierLocationRow { get; init; }

    public GojoEncodingJsonRelationRow? RelationRow { get; init; }

    public GojoEncodingJsonUnitShapeRow? UnitShapeRow { get; init; }
}

public sealed record GojoEncodingJsonStructureCounts
{
    public int Roots { get; init; }

    public int Carriers { get; init; }

    public int TickRefs { get; init; }

    public int CarrierLocations { get; init; }

    public int Relations { get; init; }

    public int UnitShapes { get; init; }
}

public sealed record GojoEncodingJsonElementRef
{
    public string Kind { get; init; } = string.Empty;

    public long? Index { get; init; }
}

public sealed record GojoEncodingJsonCarrierRow
{
    public long Id { get; init; }

    public bool IsInverse { get; init; }

    public bool IsUnknown { get; init; }

    public long? Value { get; init; }

    public long? Tension { get; init; }

    public long? UnitShape { get; init; }

    public long? InverseCarrier { get; init; }
}

public sealed record GojoEncodingJsonTickRefRow
{
    public long Id { get; init; }

    public long? Ticks { get; init; }
}

public sealed record GojoEncodingJsonCarrierLocationRow
{
    public long Id { get; init; }

    public long? Carrier { get; init; }

    public long? TickRef { get; init; }
}

public sealed record GojoEncodingJsonRelationRow
{
    public long Id { get; init; }

    public GojoEncodingJsonElementRef Left { get; init; } = new();

    public GojoEncodingJsonElementRef Right { get; init; } = new();
}

public sealed record GojoEncodingJsonUnitShapeRow
{
    public long Id { get; init; }

    public GojoEncodingJsonElementRef Surface { get; init; } = new();
}
