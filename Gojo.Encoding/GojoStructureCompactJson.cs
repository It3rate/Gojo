using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Gojo.Core;

namespace Gojo.Encoding;

/// <summary>
/// Versioned reference graph transport. Version 2 preserves ordered relations,
/// shared coordinates, unit descriptors, inverse pairing and live attachments.
/// The legacy folded syntax is readable with explicit missing-evidence residue.
/// </summary>
public static class GojoStructureCompactJson
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault
    };

    public static GojoCompactStructureDocument FromElement(
        string sourceId,
        IElement root,
        IReadOnlyDictionary<Carrier, string>? carrierLabels = null)
    {
        return GojoCompactArchive.Capture(sourceId, root, carrierLabels);
    }

    public static RehydratedCompactStructure ToElement(GojoCompactStructureDocument document)
    {
        if (document.Version == 2) return GojoCompactArchive.Restore(document);
        if (document.Version != 0) throw new NotSupportedException($"Unknown compact graph version {document.Version}.");
        return new Reader(document).Read() with { ResidueOrTension = UnitShape.DescriptorSlot(0) };
    }

    /// <summary>Returns true only for a complete executable archive. A failed
    /// version-2 import returns an unresolved witness containing the supplied
    /// document and its gaps, never an executable carrier with default state.</summary>
    public static bool TryToElement(GojoCompactStructureDocument document, out RehydratedCompactStructure restored)
    {
        if (document.Version == 2) return GojoCompactArchive.TryRestore(document, out restored);
        restored = ToElement(document);
        return false; // Legacy syntax does not contain complete archive evidence.
    }

    public static string Serialize(GojoCompactStructureDocument document) =>
        JsonSerializer.Serialize(document, JsonOptions);

    public static GojoCompactStructureDocument Deserialize(string json) =>
        JsonSerializer.Deserialize<GojoCompactStructureDocument>(json, JsonOptions) ??
        throw new InvalidOperationException("Could not deserialize compact Gojo structure JSON.");

    private sealed class Reader
    {
        private readonly GojoCompactStructureDocument document;
        private readonly Dictionary<string, Carrier> carriers = new(StringComparer.Ordinal);
        private readonly Dictionary<string, GojoCompactStructureNode> nodes;
        private readonly Dictionary<string, IElement> read = new(StringComparer.Ordinal);

        public Reader(GojoCompactStructureDocument document)
        {
            this.document = document;
            nodes = document.Nodes.ToDictionary(node => node.Id, StringComparer.Ordinal);
        }

        public RehydratedCompactStructure Read()
        {
            // Legacy files contain state but no inverse links, unit shapes or
            // shared-coordinate evidence. Restore only what is present.
            var rows = document.Carriers.Select((c, i) => new GojoEncodedCarrier(i, 0,
                c.IsInverse, c.IsUnknown, checked((ulong)(c.Value ?? 0)), c.Tension ?? 0, -1, -1)).ToArray();
            var roots = rows.Select(row => new GojoEncodedElementRef(GojoEncodedElementKind.Carrier, row.Index)).ToArray();
            var restored = new GojoEncodedSnapshot(roots, rows, [], [], [], []).RestoreRoots();
            for (var index = 0; index < document.Carriers.Count; index++)
            {
                carriers.Add(document.Carriers[index].Id, (Carrier)restored[index]);
            }

            return new RehydratedCompactStructure(
                document.SourceId,
                ReadRef(document.Root));
        }

        private IElement ReadRef(string id)
        {
            if (carriers.TryGetValue(id, out var carrier))
            {
                return carrier;
            }

            if (read.TryGetValue(id, out var existing))
            {
                return existing;
            }

            if (!nodes.TryGetValue(id, out var node))
            {
                throw new InvalidOperationException($"Compact Gojo structure reference '{id}' was not found.");
            }

            var element = ReadNode(node);
            read.Add(id, element);
            return element;
        }

        private IElement ReadNode(GojoCompactStructureNode node)
        {
            switch (node.Kind)
            {
                case "location":
                    return new CarrierLocation(ReadCarrier(node.Carrier), node.Ticks ?? 0);
                case "point":
                    return Relation.Point(ReadCarrier(node.Carrier), node.Ticks ?? 0);
                case "segment":
                    return Relation.Segment(ReadCarrier(node.Carrier), node.Start ?? 0, node.End ?? 0);
                case "group":
                    return Relation.OrderedGroup(
                        ReadCarrier(node.Carrier),
                        [.. (node.Elements ?? []).Select(ReadRef)]);
                case "relation":
                    return new Relation(ReadRef(node.Left ?? string.Empty), ReadRef(node.Right ?? string.Empty));
                default:
                    throw new NotSupportedException($"Unknown compact Gojo structure node kind '{node.Kind}'.");
            }
        }

        private Carrier ReadCarrier(string? id)
        {
            if (!string.IsNullOrWhiteSpace(id) && carriers.TryGetValue(id, out var carrier))
            {
                return carrier;
            }

            throw new InvalidOperationException($"Compact Gojo structure carrier '{id}' was not found.");
        }
    }

}

public sealed record RehydratedCompactStructure(string SourceId, IElement Root)
{
    /// <summary>Legacy files cannot establish the identities and qualifications
    /// discarded by their writer. That missing evidence remains an open slot.</summary>
    public IElement ResidueOrTension { get; init; } = Relation.Point(Carrier.Unknown, 0);
}

public sealed record GojoCompactStructureDocument
{
    public int Version { get; init; }
    public string SourceId { get; init; } = string.Empty;

    public GojoCompactStructureCounts Counts { get; set; } = new();

    public string Root { get; init; } = string.Empty;

    public List<GojoCompactCarrierNode> Carriers { get; init; } = [];

    public List<GojoCompactStructureNode> Nodes { get; init; } = [];
}

public sealed record GojoCompactCarrierNode
{
    public string Id { get; init; } = string.Empty;

    public bool IsUnknown { get; init; }

    public bool IsInverse { get; init; }

    public long? Value { get; init; }

    public long? Tension { get; init; }

    public string? UnitShape { get; init; }
    public string? Inverse { get; init; }
    public string? ValueTick { get; init; }
    public List<string>? Attachments { get; init; }
}

public sealed record GojoCompactStructureCounts
{
    public int Carriers { get; init; }

    public int Nodes { get; init; }

    public int Points { get; init; }

    public int Segments { get; init; }

    public int Groups { get; init; }

    public int Relations { get; init; }

    public int Locations { get; init; }
}

public sealed record GojoCompactStructureNode
{
    public bool IsInverse { get; init; }
    public string Id { get; init; } = string.Empty;

    public string Kind { get; init; } = string.Empty;

    public string? Carrier { get; init; }

    public long? Ticks { get; init; }

    public long? Start { get; init; }

    public long? End { get; init; }

    public string? Left { get; init; }

    public string? Right { get; init; }

    public List<string>? Elements { get; init; }

    public string? TickRef { get; init; }
    public string? Surface { get; init; }
    public List<string>? Descriptors { get; init; }
}
