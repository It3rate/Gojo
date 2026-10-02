using Gojo.Core;
using Gojo.Structures;

namespace Gojo.Encoding.Structures;

/// <summary>
/// Reads context-local projections from ordinary read witnesses. The selected
/// structure and context surface remain the source evidence; words or addresses
/// in the result are read products, not stored low-level roles.
/// </summary>
public static class ContextualProjectionReadSurface
{
    public readonly record struct ProjectionRead(
        ConceptDefinitionDiscoverySurface.DefinitionNode Definition,
        ConceptDefinitionDiscoverySurface.DefinitionNode Selected,
        ConceptDefinitionDiscoverySurface.DefinitionNode Context,
        IElement Result,
        string ResultDebug,
        IElement ResidueOrTension,
        string ResidueOrTensionDebug,
        IElement Provenance);

    public readonly record struct IntervalProjectionRead(
        IntervalBasisReadSurface.IntervalLocationRead Location,
        ProjectionRead Projection);

    public static Relation Create(
        Carrier root,
        IElement selected,
        IElement context,
        IElement result,
        IElement? residueOrTension = null,
        IElement? provenance = null) =>
        ReadWitness.Create(
            root ?? new Carrier(),
            selected,
            context,
            result,
            residueOrTension ?? UnknownPoint(),
            provenance ?? UnknownPoint());

    public static bool TryRead(
        ConceptDefinitionDiscoverySurface.DefinitionGraph graph,
        Relation selectedDefinition,
        Relation contextDefinition,
        out ProjectionRead read)
    {
        if (TryReadAll(graph, selectedDefinition, contextDefinition, out var reads))
        {
            read = reads[0];
            return true;
        }

        read = default;
        return false;
    }

    public static bool TryReadAll(
        ConceptDefinitionDiscoverySurface.DefinitionGraph graph,
        Relation selectedDefinition,
        Relation contextDefinition,
        out ProjectionRead[] reads)
    {
        if (!graph.TryFind(selectedDefinition, out var selectedNode) ||
            !graph.TryFind(contextDefinition, out var contextNode))
        {
            reads = [];
            return false;
        }

        var found = new List<ProjectionRead>();
        foreach (var node in graph.Nodes)
        {
            if (!TryReadWitnessProjection(node, selectedNode, contextNode, out var projection))
            {
                continue;
            }

            found.Add(projection);
        }

        reads = [.. found];
        return reads.Length > 0;
    }

    public static bool TryReadAt(
        ConceptDefinitionDiscoverySurface.DefinitionGraph graph,
        Relation contextDefinition,
        Carrier carrier,
        long ticks,
        out IntervalProjectionRead read)
    {
        if (!IntervalBasisReadSurface.TryLocate(graph, carrier, ticks, out var location) ||
            !TryRead(graph, location.BandDefinition, contextDefinition, out var projection))
        {
            read = default;
            return false;
        }

        read = new IntervalProjectionRead(location, projection);
        return true;
    }

    public static bool TryReadAtAll(
        ConceptDefinitionDiscoverySurface.DefinitionGraph graph,
        Relation contextDefinition,
        Carrier carrier,
        long ticks,
        out IntervalProjectionRead[] reads)
    {
        if (!IntervalBasisReadSurface.TryLocate(graph, carrier, ticks, out var location) ||
            !TryReadAll(graph, location.BandDefinition, contextDefinition, out var projections))
        {
            reads = [];
            return false;
        }

        reads = [.. projections.Select(projection => new IntervalProjectionRead(location, projection))];
        return reads.Length > 0;
    }

    private static bool TryReadWitnessProjection(
        ConceptDefinitionDiscoverySurface.DefinitionNode node,
        ConceptDefinitionDiscoverySurface.DefinitionNode selectedNode,
        ConceptDefinitionDiscoverySurface.DefinitionNode contextNode,
        out ProjectionRead read)
    {
        if (node.Read.Body is Relation witness &&
            ReadWitness.TryRead(
                witness,
                out var selected,
                out var readSurface,
                out var result,
                out var residueOrTension,
                out var provenance) &&
            ReferenceEquals(selected, selectedNode.Definition) &&
            ReferenceEquals(readSurface, contextNode.Definition))
        {
            read = new ProjectionRead(
                node,
                selectedNode,
                contextNode,
                result,
                ResultText(result),
                residueOrTension,
                ResidueText(residueOrTension),
                provenance);
            return true;
        }

        read = default;
        return false;
    }

    private static string ResultText(IElement element) =>
        EncodedTextSurface.TryRead(element, out var text)
            ? text
            : ConceptAddressSurface.TryReadAtom(element, out _, out _) ||
                ConceptAddressSurface.TryReadCompound(element, out _, out _, out _, out _)
                    ? ConceptAddressSurface.ToDebugString(element)
                    : ConceptDefinitionInspection.ElementDebug(element);

    private static string ResidueText(IElement element) =>
        IsUnknownPoint(element)
            ? string.Empty
            : EncodedTextSurface.TryRead(element, out var text)
                ? text
                : ConceptDefinitionInspection.ElementDebug(element);

    private static bool IsUnknownPoint(IElement element) =>
        element is Relation relation &&
        relation.Left is CarrierLocation location &&
        relation.Right is Carrier carrier &&
        carrier.IsUnknown &&
        location.Carrier.IsUnknown &&
        location.Ticks == 0;

    private static Relation UnknownPoint() =>
        Relation.Point(Carrier.Unknown, 0);
}
