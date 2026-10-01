using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Gojo.Core;
using Gojo.Reads;
using Gojo.Structures;

namespace Gojo.Encoding;

/// <summary>
/// Compact JSON adapter for a concept-definition ledger. This is an interchange
/// view over ordinary Gojo definition/read-witness structure; it is not the
/// source of concept meaning.
/// </summary>
public static class ConceptDefinitionInterchangeJson
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault
    };

    public static ConceptDefinitionInterchangeDocument FromLedger(string sourceId, Relation ledger)
    {
        if (!ConceptDefinitionDiscoverySurface.TryReadLedger(ledger, out var graph))
        {
            throw new InvalidOperationException("The supplied element is not a concept definition ledger.");
        }

        return new ConceptDefinitionInterchangeDocument
        {
            Version = 2, SourceId = sourceId,
            Structure = GojoStructureCompactJson.FromElement(sourceId, ledger)
        };
    }

    public static Relation ToLedger(ConceptDefinitionInterchangeDocument document)
    {
        return ToLabeledLedger(document).Ledger;
    }

    public static ConceptDefinitionInterchangeLedger ToLabeledLedger(ConceptDefinitionInterchangeDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        if (document.Version == 2 && document.Structure is not null)
        {
            var restored = GojoStructureCompactJson.ToElement(document.Structure);
            if (restored.Root is not Relation ledger || !ConceptDefinitionDiscoverySurface.TryReadLedger(ledger, out var graph))
                throw new InvalidOperationException("Archived structure is not a definition ledger.");
            return new ConceptDefinitionInterchangeLedger(ledger,
                graph.Nodes.GroupBy(node => node.Name).ToDictionary(group => group.Key,
                    group => (IReadOnlyList<Relation>)group.Select(node => node.Definition).ToArray(), StringComparer.Ordinal),
                new Dictionary<Relation, string>(ReferenceEqualityComparer.Instance),
                ReadStructuralCarriers(graph));
        }
        if (document.Version != 0) throw new NotSupportedException($"Unknown definition archive version {document.Version}.");

        var carriers = document.Carriers.ToDictionary(name => name, _ => new Carrier(), StringComparer.Ordinal);
        var nameCarrier = new Carrier();
        var resultCarrier = new Carrier();
        var residueCarrier = new Carrier();
        var unresolvedReferenceCarrier = new Carrier();
        var definitionFrameCarrier = new Carrier();
        var dependencyFrameCarrier = new Carrier();
        var witnessFrameCarrier = new Carrier();
        var resultLabels = new Dictionary<string, Relation>(StringComparer.Ordinal);
        var residueLabels = new Dictionary<string, Relation>(StringComparer.Ordinal);
        var carrierLabels = new Dictionary<Carrier, string>(ReferenceEqualityComparer.Instance);
        var unresolvedReferenceTicks = 0L;
        var definitions = new List<Relation>();
        var byLabel = new Dictionary<string, List<Relation>>(StringComparer.Ordinal);
        var projectionResultLabels = new Dictionary<Relation, string>(ReferenceEqualityComparer.Instance);
        var ledgerFrameCarrier = new Carrier();

        foreach (var (label, carrier) in carriers)
        {
            carrierLabels.Add(carrier, label);
        }

        carrierLabels.Add(ledgerFrameCarrier, "definition-ledger-frame");
        carrierLabels.Add(nameCarrier, "definition-identity-axis");
        carrierLabels.Add(resultCarrier, "projection-result-axis");
        carrierLabels.Add(residueCarrier, "residue-marker-axis");
        carrierLabels.Add(unresolvedReferenceCarrier, "unresolved-reference-axis");
        carrierLabels.Add(definitionFrameCarrier, "definition-frame");
        carrierLabels.Add(dependencyFrameCarrier, "dependency-list-frame");
        carrierLabels.Add(witnessFrameCarrier, "projection-witness-frame");
        carrierLabels.Add(Carrier.Unknown, "open-residue-carrier");

        for (var index = 0; index < document.Definitions.Count; index++)
        {
            var row = document.Definitions[index];
            var body = BodyToElement(
                row.Body,
                carriers,
                byLabel,
                resultCarrier,
                resultLabels,
                residueCarrier,
                residueLabels,
                unresolvedReferenceCarrier,
                ref unresolvedReferenceTicks,
                witnessFrameCarrier,
                out var projectionResultLabel);
            var dependencies = (row.DependsOn ?? [])
                .Select(id => DefinitionOrTensionFor(
                    byLabel,
                    id,
                    unresolvedReferenceCarrier,
                    ref unresolvedReferenceTicks))
                .ToArray();
            var definition = ConceptDefinitionSurface.Create(
                definitionFrameCarrier,
                Relation.Point(nameCarrier, index + 1),
                body,
                dependencies,
                residueOrTension: LabelPointOrUnknown(row.ResidueOrTension, residueCarrier, residueLabels),
                dependencyFrameRoot: dependencyFrameCarrier);
            definitions.Add(definition);

            if (!byLabel.TryGetValue(row.Id, out var labeled))
            {
                labeled = [];
                byLabel.Add(row.Id, labeled);
            }

            labeled.Add(definition);
            if (!string.IsNullOrEmpty(projectionResultLabel))
            {
                projectionResultLabels.Add(definition, projectionResultLabel);
            }
        }

        return new ConceptDefinitionInterchangeLedger(
            ConceptDefinitionSurface.Ledger(ledgerFrameCarrier, definitions),
            byLabel.ToDictionary(
                pair => pair.Key,
                pair => (IReadOnlyList<Relation>)[.. pair.Value],
                StringComparer.Ordinal),
            projectionResultLabels,
            carrierLabels);
    }

    public static string Serialize(ConceptDefinitionInterchangeDocument document) =>
        JsonSerializer.Serialize(document, JsonOptions);

    public static ConceptDefinitionInterchangeDocument Deserialize(string json) =>
        JsonSerializer.Deserialize<ConceptDefinitionInterchangeDocument>(json, JsonOptions) ??
        throw new InvalidOperationException("Could not deserialize concept definition interchange JSON.");

    private static Dictionary<Carrier, string> ReadStructuralCarriers(
        ConceptDefinitionDiscoverySurface.DefinitionGraph graph)
    {
        var carriers = new Dictionary<Carrier, string>(ReferenceEqualityComparer.Instance);
        foreach (var node in graph.Nodes)
        {
            AddStructuralCarriers(node.Read.Body, carriers);
        }

        return carriers;
    }

    private static void AddStructuralCarriers(
        IElement element,
        Dictionary<Carrier, string> carriers)
    {
        if (TryReadPoint(element, out var point))
        {
            Add(CanonicalCarrier(point.Carrier));
            return;
        }

        if (TryReadSegment(element, out var carrier, out _, out _))
        {
            Add(CanonicalCarrier(carrier));
            return;
        }

        if (element is Relation directedSegment &&
            directedSegment.TryReadDirectedSegment(out _, out var positiveEnd))
        {
            Add(positiveEnd.Carrier);
            return;
        }

        if (EncodedTextSurface.TryRead(element, out _))
        {
            return;
        }

        if (element is Relation relation &&
            ReadWitness.TryRead(
                relation,
                out var selected,
                out var readSurface,
                out var result,
                out var residueOrTension,
                out var provenance))
        {
            AddStructuralCarriers(selected, carriers);
            AddStructuralCarriers(readSurface, carriers);
            AddStructuralCarriers(result, carriers);
            AddStructuralCarriers(residueOrTension, carriers);
            AddStructuralCarriers(provenance, carriers);
            return;
        }

        if (element is Relation group &&
            EncodedTextSurface.TryReadRootElements(group, out var elements))
        {
            foreach (var item in elements)
            {
                AddStructuralCarriers(item, carriers);
            }

            return;
        }

        if (element is Relation fallbackRelation)
        {
            AddStructuralCarriers(fallbackRelation.Left, carriers);
            AddStructuralCarriers(fallbackRelation.Right, carriers);
        }

        void Add(Carrier carrier)
        {
            if (carrier.IsUnknown || carriers.ContainsKey(carrier))
            {
                return;
            }

            carriers.Add(carrier, carriers.Count == 0 ? "axis" : $"carrier-{carriers.Count + 1}");
        }
    }

    private static IElement BodyToElement(
        ConceptDefinitionInterchangeBody body,
        Dictionary<string, Carrier> carriers,
        IReadOnlyDictionary<string, List<Relation>> definitions,
        Carrier resultCarrier,
        Dictionary<string, Relation> resultLabels,
        Carrier residueCarrier,
        Dictionary<string, Relation> residueLabels,
        Carrier unresolvedReferenceCarrier,
        ref long unresolvedReferenceTicks,
        Carrier witnessFrameCarrier,
        out string? projectionResultLabel)
    {
        projectionResultLabel = null;
        switch (body.Kind)
        {
            case "definitionRef":
                return DefinitionOrTensionFor(
                    definitions,
                    body.Selected,
                    unresolvedReferenceCarrier,
                    ref unresolvedReferenceTicks);
            case "text":
                return EncodedTextSurface.Create(new Carrier(), body.Text);
            case "point":
                return Relation.Point(CarrierFor(carriers, body.Carrier, body.Face), body.Ticks ?? 0);
            case "segment":
                return Relation.Segment(
                    CarrierFor(carriers, body.Carrier, body.Face),
                    body.Start ?? 0,
                    body.End ?? 0);
            case "directedSegment":
                return Relation.DirectedSegment(
                    CarrierFor(carriers, body.Carrier),
                    body.Start ?? 0,
                    body.End ?? 0);
            case "group":
                var groupElements = new List<IElement>();
                foreach (var element in body.Elements ?? [])
                {
                    groupElements.Add(BodyToElement(
                        element,
                        carriers,
                        definitions,
                        resultCarrier,
                        resultLabels,
                        residueCarrier,
                        residueLabels,
                        unresolvedReferenceCarrier,
                        ref unresolvedReferenceTicks,
                        witnessFrameCarrier,
                        out _));
                }

                return Relation.OrderedGroup(new Carrier(), [.. groupElements]);
            case "relation":
                return new Relation(
                    body.Left is null
                        ? EncodedTextSurface.UnknownPoint()
                        : BodyToElement(
                            body.Left,
                            carriers,
                            definitions,
                            resultCarrier,
                            resultLabels,
                            residueCarrier,
                            residueLabels,
                            unresolvedReferenceCarrier,
                            ref unresolvedReferenceTicks,
                            witnessFrameCarrier,
                            out _),
                    body.Right is null
                        ? EncodedTextSurface.UnknownPoint()
                        : BodyToElement(
                            body.Right,
                            carriers,
                            definitions,
                            resultCarrier,
                            resultLabels,
                            residueCarrier,
                            residueLabels,
                            unresolvedReferenceCarrier,
                            ref unresolvedReferenceTicks,
                            witnessFrameCarrier,
                            out _));
            case "witness":
                var witnessSelected = DefinitionOrTensionFor(
                    definitions,
                    body.Selected,
                    unresolvedReferenceCarrier,
                    ref unresolvedReferenceTicks);
                var witnessReadSurface = body.ReadSurface is null
                    ? Relation.Point(Carrier.Unknown, 0)
                    : BodyToElement(
                        body.ReadSurface,
                        carriers,
                        definitions,
                        resultCarrier,
                        resultLabels,
                        residueCarrier,
                        residueLabels,
                        unresolvedReferenceCarrier,
                        ref unresolvedReferenceTicks,
                        witnessFrameCarrier,
                        out _);
                var witnessResult = body.Result is null
                    ? Relation.Point(Carrier.Unknown, 0)
                    : BodyToElement(
                        body.Result,
                        carriers,
                        definitions,
                        resultCarrier,
                        resultLabels,
                        residueCarrier,
                        residueLabels,
                        unresolvedReferenceCarrier,
                        ref unresolvedReferenceTicks,
                        witnessFrameCarrier,
                        out _);
                return ReadWitness.Create(
                    witnessFrameCarrier,
                    witnessSelected,
                    witnessReadSurface,
                    witnessResult,
                    LabelPointOrUnknown(body.ResidueOrTension, residueCarrier, residueLabels),
                    string.IsNullOrWhiteSpace(body.Provenance)
                        ? witnessSelected
                        : DefinitionOrTensionFor(
                            definitions,
                            body.Provenance,
                            unresolvedReferenceCarrier,
                            ref unresolvedReferenceTicks));
            case "projection":
                projectionResultLabel = body.ResultText;
                var selected = DefinitionOrTensionFor(
                    definitions,
                    body.Selected,
                    unresolvedReferenceCarrier,
                    ref unresolvedReferenceTicks);
                var context = DefinitionOrTensionFor(
                    definitions,
                    body.Context,
                    unresolvedReferenceCarrier,
                    ref unresolvedReferenceTicks);
                return ContextualProjectionReadSurface.Create(
                    witnessFrameCarrier,
                    selected,
                    context,
                    LabelPointOrUnknown(body.ResultText, resultCarrier, resultLabels),
                    LabelPointOrUnknown(body.ResidueOrTension, residueCarrier, residueLabels),
                    string.IsNullOrWhiteSpace(body.Provenance)
                        ? context
                        : DefinitionOrTensionFor(
                            definitions,
                            body.Provenance,
                            unresolvedReferenceCarrier,
                            ref unresolvedReferenceTicks));
            default:
                throw new NotSupportedException($"Unknown compact body kind '{body.Kind}'.");
        }
    }

    private static Carrier CarrierFor(Dictionary<string, Carrier> carriers, string? id, string? face = null)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new InvalidOperationException("Point and segment bodies require a carrier id.");
        }

        if (!carriers.TryGetValue(id, out var carrier))
        {
            carrier = new Carrier();
            carriers.Add(id, carrier);
        }

        return string.Equals(face, "inverse", StringComparison.OrdinalIgnoreCase)
            ? carrier.Inverse()
            : carrier;
    }

    private static Carrier CanonicalCarrier(Carrier carrier) =>
        carrier.IsInverse ? carrier.Inverse() : carrier;

    private static IElement DefinitionOrTensionFor(
        IReadOnlyDictionary<string, List<Relation>> definitions,
        string? id,
        Carrier unresolvedReferenceCarrier,
        ref long unresolvedReferenceTicks)
    {
        if (!string.IsNullOrWhiteSpace(id) &&
            definitions.TryGetValue(id, out var matches) &&
            matches.Count == 1)
        {
            return matches[0];
        }

        return Relation.Point(unresolvedReferenceCarrier, ++unresolvedReferenceTicks);
    }

    private static IElement LabelPointOrUnknown(
        string? label,
        Carrier carrier,
        Dictionary<string, Relation> labels)
    {
        if (string.IsNullOrEmpty(label))
        {
            return Relation.Point(Carrier.Unknown, 0);
        }

        if (!labels.TryGetValue(label, out var point))
        {
            point = Relation.Point(carrier, labels.Count + 1);
            labels.Add(label, point);
        }

        return point;
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
            start = left.Ticks;
            end = right.Ticks;
            return true;
        }

        carrier = Carrier.Unknown;
        start = 0;
        end = 0;
        return false;
    }
}

public sealed record ConceptDefinitionInterchangeDocument
{
    public int Version { get; init; }
    /// <summary>Version 2 preserves the actual graph. Legacy definition rows
    /// remain an authoring/import syntax, not an executable archive.</summary>
    public GojoCompactStructureDocument? Structure { get; init; }
    public string SourceId { get; init; } = string.Empty;

    public List<string> Carriers { get; init; } = [];

    public List<ConceptDefinitionInterchangeDefinition> Definitions { get; init; } = [];
}

public sealed record ConceptDefinitionInterchangeLedger(
    Relation Ledger,
    IReadOnlyDictionary<string, IReadOnlyList<Relation>> DefinitionsByLabel,
    IReadOnlyDictionary<Relation, string> ProjectionResultLabels,
    IReadOnlyDictionary<Carrier, string> CarrierLabels)
{
    public Relation SingleDefinition(string label)
    {
        if (DefinitionsByLabel.TryGetValue(label, out var definitions) && definitions.Count == 1)
        {
            return definitions[0];
        }

        if (definitions is { Count: > 1 })
        {
            throw new InvalidOperationException(
                $"Label '{label}' matched {definitions.Count} definitions. Add more structure before selecting it.");
        }

        throw new InvalidOperationException($"Label '{label}' was not found in the compact interchange ledger.");
    }

    public string ProjectionResultLabel(ContextualProjectionReadSurface.ProjectionRead read) =>
        ProjectionResultLabels.TryGetValue(read.Definition.Definition, out var label)
            ? label
            : read.ResultDebug;
}

public sealed record ConceptDefinitionInterchangeDefinition
{
    public string Id { get; init; } = string.Empty;

    public ConceptDefinitionInterchangeBody Body { get; init; } = new();

    public List<string>? DependsOn { get; init; }

    public string? ResidueOrTension { get; init; }
}

public sealed record ConceptDefinitionInterchangeBody
{
    public string Kind { get; init; } = string.Empty;

    public List<ConceptDefinitionInterchangeBody>? Elements { get; init; }

    public ConceptDefinitionInterchangeBody? Left { get; init; }

    public ConceptDefinitionInterchangeBody? Right { get; init; }

    public string? Carrier { get; init; }

    public string? Face { get; init; }

    public long? Ticks { get; init; }

    public long? Start { get; init; }

    public long? End { get; init; }

    public string? Text { get; init; }

    public string? Selected { get; init; }

    public string? Context { get; init; }

    public ConceptDefinitionInterchangeBody? ReadSurface { get; init; }

    public ConceptDefinitionInterchangeBody? Result { get; init; }

    public string? ResultText { get; init; }

    public string? ResidueOrTension { get; init; }

    public string? Provenance { get; init; }
}
