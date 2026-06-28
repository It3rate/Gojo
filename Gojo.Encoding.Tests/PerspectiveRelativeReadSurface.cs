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
        string IntervalName,
        string IntervalBand,
        string IntervalRole,
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
                    out var focusSurface,
                    out _,
                    out var residueOrTension,
                    out _) ||
                !graph.TryFind(selected, out var selectedNode) ||
                focusSurface is not Relation focus ||
                !TryReadFocusPerspective(focus, selectedNode, out var selectedSite, out var origin, out var coarseCarrier, out var sourceTicksPerReadTick))
            {
                continue;
            }

            var sourceDeltaTicks = Math.Abs(selectedSite.Ticks - origin.Ticks);
            var relativeTicks = CeilingDivide(sourceDeltaTicks, sourceTicksPerReadTick);
            var intervalLocation = CommonIntervalReadSurface.TryLocate(
                    graph,
                    coarseCarrier,
                    relativeTicks,
                    out var located)
                ? located
                : default;

            reads.Add(new PerspectiveRelativeRead(
                node.Name,
                ConceptDefinitionInspection.ElementDebug(selected),
                BodyNameOrDebug(graph, focus),
                sourceDeltaTicks,
                relativeTicks,
                intervalLocation.IntervalName,
                intervalLocation.BandName,
                intervalLocation.Role,
                string.IsNullOrEmpty(intervalLocation.Qualifier) ? "unsettled" : intervalLocation.Qualifier,
                ResidueText(residueOrTension)));
        }

        read = new PerspectiveRelativeSurfaceRead([.. reads]);
        return reads.Count > 0;
    }

    private static bool TryReadFocusPerspective(
        Relation focus,
        ConceptDefinitionDiscoverySurface.DefinitionNode selected,
        out CarrierLocation selectedSite,
        out CarrierLocation origin,
        out Carrier coarseCarrier,
        out long sourceTicksPerReadTick)
    {
        if (!FocusSurface.TryRead(
                focus,
                out var source,
                out var focusBasis,
                out var participation,
                out _,
                out _,
                out _,
                out _,
                out _) ||
            !ReferenceEquals(source, selected.Definition) ||
            !TryReadPoint(participation, out selectedSite) ||
            !FocusSurface.TryReadFrame(focusBasis, out var focusBasisElements))
        {
            selectedSite = default;
            origin = default;
            coarseCarrier = Carrier.Unknown;
            sourceTicksPerReadTick = 0;
            return false;
        }

        origin = default;
        coarseCarrier = Carrier.Unknown;
        sourceTicksPerReadTick = 0;
        foreach (var element in focusBasisElements)
        {
            if (!TryReadPoint(element, out var point))
            {
                continue;
            }

            if (point.Carrier.UnitShape.TryReadResolution(out _, out var sourceTicks) &&
                sourceTicks > 0)
            {
                coarseCarrier = point.Carrier;
                sourceTicksPerReadTick = sourceTicks;
            }
            else
            {
                origin = point;
            }
        }

        return sourceTicksPerReadTick > 0 &&
            !origin.Carrier.IsUnknown &&
            !coarseCarrier.IsUnknown &&
            selectedSite.Carrier.SameIdentityAs(origin.Carrier);
    }

    private static long CeilingDivide(long numerator, long denominator) =>
        numerator == 0
            ? 0
            : checked(((numerator - 1) / denominator) + 1);

    private static string BodyNameOrDebug(
        ConceptDefinitionDiscoverySurface.DefinitionGraph graph,
        IElement body)
    {
        foreach (var node in graph.Nodes)
        {
            if (ReferenceEquals(node.Read.Body, body))
            {
                return node.Name;
            }
        }

        return ConceptDefinitionInspection.ElementDebug(body);
    }

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
