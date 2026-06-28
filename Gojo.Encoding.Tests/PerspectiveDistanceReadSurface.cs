using Gojo.Core;
using Gojo.Structures;

namespace Gojo.Encoding.Tests;

/// <summary>
/// Test-only reader for perspective-dependent distance qualifiers. The source
/// element keeps one underlying position; near/far is computed from a read
/// witness result under a perspective surface.
/// </summary>
internal static class PerspectiveDistanceReadSurface
{
    public readonly record struct PerspectiveDistanceRead(
        string DefinitionName,
        string Selected,
        string Perspective,
        long DistanceTicks,
        string Qualifier,
        string ResidueOrTension);

    public readonly record struct PerspectiveDistanceSurfaceRead(PerspectiveDistanceRead[] Reads);

    public static bool TryRead(IElement ledger, out PerspectiveDistanceSurfaceRead read)
    {
        if (!ConceptDefinitionDiscoverySurface.TryReadLedger(ledger, out var graph))
        {
            read = default;
            return false;
        }

        var reads = new List<PerspectiveDistanceRead>();
        foreach (var node in graph.Nodes)
        {
            if (node.Read.Body is not Relation witness ||
                !ReadWitness.TryRead(
                    witness,
                    out var selected,
                    out var perspective,
                    out var result,
                    out var residueOrTension,
                    out _) ||
                !TryReadPoint(result, out var distance) ||
                !distance.Carrier.UnitShape.TryReadResolution(out _, out _) ||
                !HasPerspectiveDependency(graph, node))
            {
                continue;
            }

            reads.Add(new PerspectiveDistanceRead(
                node.Name,
                ConceptDefinitionInspection.ElementDebug(selected),
                ConceptDefinitionInspection.ElementDebug(perspective),
                distance.Ticks,
                Qualify(distance.Ticks),
                ResidueText(residueOrTension)));
        }

        read = new PerspectiveDistanceSurfaceRead([.. reads]);
        return reads.Count > 0;
    }

    private static bool HasPerspectiveDependency(
        ConceptDefinitionDiscoverySurface.DefinitionGraph graph,
        ConceptDefinitionDiscoverySurface.DefinitionNode node) =>
        graph.DependenciesOf(node).Any(dependency =>
            graph.TryReadCompoundBody(dependency, out var body) &&
            body.Lexical == "Perspective");

    private static string Qualify(long ticks) =>
        ticks <= 2
            ? "near"
            : ticks >= 6
                ? "far"
                : "middle";

    private static string ResidueText(IElement element) =>
        IsUnknownPoint(element)
            ? string.Empty
            : EncodedTextSurface.TryRead(element, out var text)
                ? text
                : ConceptDefinitionInspection.ElementDebug(element);

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

    private static bool IsUnknownPoint(IElement element) =>
        element is Relation relation &&
        relation.Left is CarrierLocation location &&
        relation.Right is Carrier carrier &&
        carrier.IsUnknown &&
        location.Carrier.IsUnknown &&
        location.Ticks == 0;
}
