using Gojo.Core;
using Gojo.Structures;

namespace Gojo.Encoding.Tests;

/// <summary>
/// Test-only local projection from an interval band through a context surface
/// into a presentation word. The interval still owns only structural roles;
/// context-specific words live in read witnesses over the selected band.
/// </summary>
internal static class ContextualIntervalProjectionReadSurface
{
    public readonly record struct ContextualIntervalProjectionRead(
        string Context,
        Relation ContextDefinition,
        string IntervalName,
        Relation IntervalDefinition,
        string BandName,
        Relation BandDefinition,
        string Role,
        string Projection,
        string ResidueOrTension);

    public static bool TryProject(
        ConceptDefinitionDiscoverySurface.DefinitionGraph graph,
        Relation contextDefinition,
        Carrier carrier,
        long ticks,
        out ContextualIntervalProjectionRead read)
    {
        if (!graph.TryFind(contextDefinition, out var context) ||
            !CommonIntervalReadSurface.TryLocate(graph, carrier, ticks, out var location))
        {
            read = default;
            return false;
        }

        foreach (var node in graph.Nodes)
        {
            if (node.Read.Body is not Relation witness ||
                !ReadWitness.TryRead(
                    witness,
                    out var selected,
                    out var readSurface,
                    out var result,
                    out var residueOrTension,
                out _) ||
                !ReferenceEquals(selected, location.BandDefinition) ||
                !ReferenceEquals(readSurface, context.Definition) ||
                !EncodedTextSurface.TryRead(result, out var projection))
            {
                continue;
            }

            read = new ContextualIntervalProjectionRead(
                context.Name,
                context.Definition,
                location.IntervalName,
                location.IntervalDefinition,
                location.BandName,
                location.BandDefinition,
                location.Role,
                projection,
                ResidueText(residueOrTension));
            return true;
        }

        read = default;
        return false;
    }

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
}
