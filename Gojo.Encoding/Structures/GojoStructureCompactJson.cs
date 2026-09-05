using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Gojo.Core;

namespace Gojo.Encoding.Structures;

/// <summary>
/// Folded JSON view of a Gojo element graph. Unlike the diagnostic row table,
/// this keeps common structural forms compact: ordered groups carry a single
/// frame plus element refs, and points/segments are single nodes.
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
        var writer = new Writer(sourceId, carrierLabels);
        return writer.Write(root);
    }

    public static RehydratedCompactStructure ToElement(GojoCompactStructureDocument document)
    {
        var reader = new Reader(document);
        return reader.Read();
    }

    public static string Serialize(GojoCompactStructureDocument document) =>
        JsonSerializer.Serialize(document, JsonOptions);

    public static GojoCompactStructureDocument Deserialize(string json) =>
        JsonSerializer.Deserialize<GojoCompactStructureDocument>(json, JsonOptions) ??
        throw new InvalidOperationException("Could not deserialize compact Gojo structure JSON.");

    private sealed class Writer
    {
        private readonly string sourceId;
        private readonly IReadOnlyDictionary<Carrier, string> carrierLabels;
        private readonly Dictionary<Carrier, string> carriers = new(ReferenceEqualityComparer.Instance);
        private readonly Dictionary<Relation, string> relations = new(ReferenceEqualityComparer.Instance);
        private readonly HashSet<string> usedCarrierIds = new(StringComparer.Ordinal);
        private readonly List<GojoCompactCarrierNode> carrierNodes = [];
        private readonly List<GojoCompactStructureNode> nodes = [];
        private int nextNodeIndex;

        public Writer(string sourceId, IReadOnlyDictionary<Carrier, string>? carrierLabels)
        {
            this.sourceId = sourceId;
            this.carrierLabels = carrierLabels ??
                new Dictionary<Carrier, string>(ReferenceEqualityComparer.Instance);
        }

        public GojoCompactStructureDocument Write(IElement root)
        {
            var rootRef = WriteElement(root);
            var document = new GojoCompactStructureDocument
            {
                SourceId = sourceId,
                Root = rootRef,
                Carriers = carrierNodes,
                Nodes = nodes
            };
            document.Counts = CountsOf(document);
            return document;
        }

        private string WriteElement(IElement element) =>
            element switch
            {
                Carrier carrier => WriteCarrier(carrier),
                CarrierLocation location => WriteLocation(location),
                Relation relation => WriteRelation(relation),
                _ => throw new NotSupportedException(
                    $"Compact Gojo structure JSON does not support element {element.GetType().Name}.")
            };

        private string WriteCarrier(Carrier carrier)
        {
            if (carriers.TryGetValue(carrier, out var existing))
            {
                return existing;
            }

            var id = UniqueCarrierId(carrierLabels.TryGetValue(carrier, out var label)
                ? SlugOf(label)
                : $"carrier-{carrierNodes.Count + 1}");
            carriers.Add(carrier, id);
            carrierNodes.Add(new GojoCompactCarrierNode
            {
                Id = id,
                IsUnknown = carrier.IsUnknown,
                IsInverse = carrier.IsInverse,
                Value = NonZero(carrier.Value),
                Tension = NonZero(carrier.Tension)
            });
            return id;
        }

        private string WriteLocation(CarrierLocation location)
        {
            var id = NextNodeId();
            nodes.Add(new GojoCompactStructureNode
            {
                Id = id,
                Kind = "location",
                Carrier = WriteCarrier(location.Carrier),
                Ticks = location.Ticks
            });
            return id;
        }

        private string WriteRelation(Relation relation)
        {
            if (relations.TryGetValue(relation, out var existing))
            {
                return existing;
            }

            var id = NextNodeId();
            relations.Add(relation, id);

            if (TryReadPoint(relation, out var point))
            {
                nodes.Add(new GojoCompactStructureNode
                {
                    Id = id,
                    Kind = "point",
                    Carrier = WriteCarrier(point.Carrier),
                    Ticks = point.Ticks
                });
                return id;
            }

            if (TryReadSegment(relation, out var carrier, out var start, out var end))
            {
                nodes.Add(new GojoCompactStructureNode
                {
                    Id = id,
                    Kind = "segment",
                    Carrier = WriteCarrier(carrier),
                    Start = start,
                    End = end
                });
                return id;
            }

            if (relation.Right is Carrier holder &&
                relation.TryReadOrderedElementsOn(holder, out var elements))
            {
                nodes.Add(new GojoCompactStructureNode
                {
                    Id = id,
                    Kind = "group",
                    Carrier = WriteCarrier(holder),
                    Elements = [.. elements.Select(WriteElement)]
                });
                return id;
            }

            nodes.Add(new GojoCompactStructureNode
            {
                Id = id,
                Kind = "relation",
                Left = WriteElement(relation.Left),
                Right = WriteElement(relation.Right)
            });
            return id;
        }

        private string UniqueCarrierId(string requested)
        {
            if (usedCarrierIds.Add(requested))
            {
                return requested;
            }

            for (var index = 2; ; index++)
            {
                var candidate = $"{requested}-{index}";
                if (usedCarrierIds.Add(candidate))
                {
                    return candidate;
                }
            }
        }

        private static string SlugOf(string label)
        {
            var builder = new StringBuilder(label.Length);
            var pendingSeparator = false;
            foreach (var character in label.Trim())
            {
                if (char.IsLetterOrDigit(character))
                {
                    if (pendingSeparator && builder.Length > 0)
                    {
                        builder.Append('-');
                    }

                    builder.Append(char.ToLowerInvariant(character));
                    pendingSeparator = false;
                }
                else if (character is '-' or '_' || char.IsWhiteSpace(character))
                {
                    pendingSeparator = builder.Length > 0;
                }
            }

            return builder.Length == 0 ? "carrier" : builder.ToString();
        }

        private string NextNodeId() => $"node-{++nextNodeIndex}";
    }

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
            foreach (var carrier in document.Carriers)
            {
                carriers.Add(carrier.Id, carrier.IsUnknown ? Carrier.Unknown : new Carrier(carrier.IsInverse));
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

    private static bool TryReadPoint(IElement element, out CarrierLocation location)
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

    private static bool TryReadSegment(IElement element, out Carrier carrier, out long start, out long end)
    {
        if (element is Relation segment &&
            TryReadPoint(segment.Left, out var left) &&
            TryReadPoint(segment.Right, out var right) &&
            left.Carrier.SameIdentityAs(right.Carrier))
        {
            carrier = left.Carrier;
            start = Math.Min(left.Ticks, right.Ticks);
            end = Math.Max(left.Ticks, right.Ticks);
            return true;
        }

        carrier = Carrier.Unknown;
        start = 0;
        end = 0;
        return false;
    }

    private static GojoCompactStructureCounts CountsOf(GojoCompactStructureDocument document) =>
        new()
        {
            Carriers = document.Carriers.Count,
            Nodes = document.Nodes.Count,
            Points = document.Nodes.Count(node => node.Kind == "point"),
            Segments = document.Nodes.Count(node => node.Kind == "segment"),
            Groups = document.Nodes.Count(node => node.Kind == "group"),
            Relations = document.Nodes.Count(node => node.Kind == "relation"),
            Locations = document.Nodes.Count(node => node.Kind == "location")
        };

    private static long? NonZero(long value) => value == 0 ? null : value;

    private static long? NonZero(ulong value) => value == 0 ? null : checked((long)value);
}

public sealed record RehydratedCompactStructure(string SourceId, IElement Root);

public sealed record GojoCompactStructureDocument
{
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
    public string Id { get; init; } = string.Empty;

    public string Kind { get; init; } = string.Empty;

    public string? Carrier { get; init; }

    public long? Ticks { get; init; }

    public long? Start { get; init; }

    public long? End { get; init; }

    public string? Left { get; init; }

    public string? Right { get; init; }

    public List<string>? Elements { get; init; }
}
