using Gojo.Core;
using Gojo.Structures;

namespace Gojo.Encoding.Tests;

/// <summary>
/// Test-only reader for perspective-dependent relative qualifiers. The selected
/// element keeps one underlying carrier site; near/far is computed from that
/// site, a read origin, and a coarse basis under a perspective surface.
/// </summary>
internal static class PerspectiveRelativeReadSurface
{
    public readonly record struct PerspectiveRelativeRead(
        string DefinitionName,
        string Selected,
        string Perspective,
        long SourceDeltaTicks,
        long RelativeTicks,
        string Qualifier,
        string ResidueOrTension);

    public readonly record struct PerspectiveRelativeSurfaceRead(PerspectiveRelativeRead[] Reads);

    public static bool TryRead(IElement ledger, out PerspectiveRelativeSurfaceRead read)
    {
        if (!ConceptDefinitionDiscoverySurface.TryReadLedger(ledger, out var graph))
        {
            read = default;
            return false;
        }

        var reads = new List<PerspectiveRelativeRead>();
        foreach (var node in graph.Nodes)
        {
            if (node.Read.Body is not Relation witness ||
                !ReadWitness.TryRead(
                    witness,
                    out var selected,
                    out var perspective,
                    out _,
                    out var residueOrTension,
                    out _) ||
                !graph.TryFind(selected, out var selectedNode) ||
                !graph.TryFind(perspective, out var perspectiveNode) ||
                !TryReadPerspective(graph, perspectiveNode, out var origin, out var sourceTicksPerReadTick) ||
                !TryReadSelectedSite(graph, node, selectedNode, origin.Carrier, out var selectedSite))
            {
                continue;
            }

            var sourceDeltaTicks = Math.Abs(selectedSite.Ticks - origin.Ticks);
            var relativeTicks = CeilingDivide(sourceDeltaTicks, sourceTicksPerReadTick);

            reads.Add(new PerspectiveRelativeRead(
                node.Name,
                ConceptDefinitionInspection.ElementDebug(selected),
                ConceptDefinitionInspection.ElementDebug(perspective),
                sourceDeltaTicks,
                relativeTicks,
                Qualify(relativeTicks),
                ResidueText(residueOrTension)));
        }

        read = new PerspectiveRelativeSurfaceRead([.. reads]);
        return reads.Count > 0;
    }

    private static bool TryReadPerspective(
        ConceptDefinitionDiscoverySurface.DefinitionGraph graph,
        ConceptDefinitionDiscoverySurface.DefinitionNode perspective,
        out CarrierLocation origin,
        out long sourceTicksPerReadTick)
    {
        if (!graph.TryReadCompoundBody(perspective, out var body) ||
            body.Lexical != "Perspective")
        {
            origin = default;
            sourceTicksPerReadTick = 0;
            return false;
        }

        origin = default;
        sourceTicksPerReadTick = 0;
        foreach (var dependency in graph.DependenciesOf(perspective))
        {
            if (!TryReadPoint(dependency.Read.Body, out var point))
            {
                continue;
            }

            if (point.Carrier.UnitShape.TryReadResolution(out _, out var sourceTicks) &&
                sourceTicks > 0)
            {
                sourceTicksPerReadTick = sourceTicks;
            }
            else
            {
                origin = point;
            }
        }

        return sourceTicksPerReadTick > 0 && !origin.Carrier.IsUnknown;
    }

    private static bool TryReadSelectedSite(
        ConceptDefinitionDiscoverySurface.DefinitionGraph graph,
        ConceptDefinitionDiscoverySurface.DefinitionNode read,
        ConceptDefinitionDiscoverySurface.DefinitionNode selected,
        Carrier sourceCarrier,
        out CarrierLocation site)
    {
        foreach (var dependency in graph.DependenciesOf(read))
        {
            if (graph.DependsOn(dependency, selected) &&
                TryReadPoint(dependency.Read.Body, out var point) &&
                point.Carrier.SameIdentityAs(sourceCarrier))
            {
                site = point;
                return true;
            }
        }

        site = default;
        return false;
    }

    private static string Qualify(long ticks) =>
        ticks <= 1
            ? "near"
            : ticks >= 5
                ? "far"
                : "middle";

    private static long CeilingDivide(long numerator, long denominator) =>
        numerator == 0
            ? 0
            : checked(((numerator - 1) / denominator) + 1);

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
