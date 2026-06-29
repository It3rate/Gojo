using Gojo.Core;
using Gojo.Structures;

namespace Gojo.Encoding;

/// <summary>
/// Reads a simple one-dimensional interval basis from local definition
/// structure. Start, center, end, and coarse bands are read outcomes over a
/// carrier segment plus optional landmark/band evidence or derived read
/// witnesses; the labels are not stored grammar concepts.
/// </summary>
public static class IntervalBasisReadSurface
{
    public readonly record struct IntervalBandRead(
        string DefinitionName,
        Relation Definition,
        long StartTicks,
        long EndTicks,
        string Role);

    public readonly record struct IntervalRead(
        string DefinitionName,
        Relation Definition,
        Carrier Carrier,
        long StartTicks,
        long CenterTicks,
        long EndTicks,
        string StartLandmark,
        string CenterLandmark,
        string EndLandmark,
        IntervalBandRead[] Bands);

    public readonly record struct IntervalLocationRead(
        string IntervalName,
        Relation IntervalDefinition,
        long Ticks,
        string BandName,
        Relation BandDefinition,
        string Role);

    public static bool TryRead(
        ConceptDefinitionDiscoverySurface.DefinitionGraph graph,
        out IntervalRead[] intervals)
    {
        var reads = new List<IntervalRead>();
        foreach (var node in graph.Nodes)
        {
            if (!TryReadSegment(node.Read.Body, out var carrier, out var start, out var end) ||
                start == end)
            {
                continue;
            }

            var center = start + ((end - start) / 2);
            var startLandmarkName = TryFindLandmark(graph, node, carrier, start, out var startLandmark)
                ? startLandmark.Name
                : "derived-startpoint";
            var centerLandmarkName = TryFindLandmark(graph, node, carrier, center, out var centerLandmark)
                ? centerLandmark.Name
                : "derived-centerpoint";
            var endLandmarkName = TryFindLandmark(graph, node, carrier, end, out var endLandmark)
                ? endLandmark.Name
                : "derived-endpoint";

            var bands = ReadBands(graph, node, carrier, start, center, end);
            if (bands.Length == 0)
            {
                continue;
            }

            reads.Add(new IntervalRead(
                node.Name,
                node.Definition,
                carrier,
                start,
                center,
                end,
                startLandmarkName,
                centerLandmarkName,
                endLandmarkName,
                bands));
        }

        intervals = [.. reads];
        return intervals.Length > 0;
    }

    public static bool TryLocate(
        ConceptDefinitionDiscoverySurface.DefinitionGraph graph,
        Carrier carrier,
        long ticks,
        out IntervalLocationRead location)
    {
        if (TryRead(graph, out var intervals))
        {
            foreach (var interval in intervals)
            {
                if (!interval.Carrier.SameIdentityAs(carrier) ||
                    ticks < interval.StartTicks ||
                    ticks > interval.EndTicks)
                {
                    continue;
                }

                foreach (var band in interval.Bands)
                {
                    if (ticks < band.StartTicks || ticks > band.EndTicks)
                    {
                        continue;
                    }

                    location = new IntervalLocationRead(
                        interval.DefinitionName,
                        interval.Definition,
                        ticks,
                        band.DefinitionName,
                        band.Definition,
                        band.Role);
                    return true;
                }
            }
        }

        location = default;
        return false;
    }

    public static bool TryReadInterior(
        IElement intervalOrBand,
        Carrier carrier,
        long ticks,
        out bool inside)
    {
        if (TryReadSegment(intervalOrBand, out var segmentCarrier, out var start, out var end) &&
            segmentCarrier.SameIdentityAs(carrier))
        {
            inside = ticks > start && ticks < end;
            return true;
        }

        inside = false;
        return false;
    }

    private static IntervalBandRead[] ReadBands(
        ConceptDefinitionDiscoverySurface.DefinitionGraph graph,
        ConceptDefinitionDiscoverySurface.DefinitionNode interval,
        Carrier carrier,
        long intervalStart,
        long intervalCenter,
        long intervalEnd)
    {
        var bands = new List<IntervalBandRead>();
        foreach (var node in graph.Nodes)
        {
            if (node.Index == interval.Index)
            {
                continue;
            }

            if (TryReadDerivedBand(
                node,
                interval,
                carrier,
                intervalStart,
                intervalCenter,
                intervalEnd,
                out var derivedBand))
            {
                bands.Add(derivedBand);
                continue;
            }

            if (!graph.DependsOn(node, interval) ||
                !TryReadSegment(node.Read.Body, out var bandCarrier, out var start, out var end) ||
                !bandCarrier.SameIdentityAs(carrier))
            {
                continue;
            }

            var role = BandRole(start, end, intervalStart, intervalCenter, intervalEnd);
            bands.Add(new IntervalBandRead(
                node.Name,
                node.Definition,
                start,
                end,
                role));
        }

        return [.. bands];
    }

    private static bool TryReadDerivedBand(
        ConceptDefinitionDiscoverySurface.DefinitionNode node,
        ConceptDefinitionDiscoverySurface.DefinitionNode interval,
        Carrier carrier,
        long intervalStart,
        long intervalCenter,
        long intervalEnd,
        out IntervalBandRead band)
    {
        if (node.Read.Body is Relation witness &&
            ReadWitness.TryRead(
                witness,
                out var selected,
                out _,
                out var result,
                out _,
                out _) &&
            ReferenceEquals(selected, interval.Definition) &&
            TryReadSegment(result, out var bandCarrier, out var start, out var end) &&
            bandCarrier.SameIdentityAs(carrier))
        {
            band = new IntervalBandRead(
                node.Name,
                node.Definition,
                start,
                end,
                BandRole(start, end, intervalStart, intervalCenter, intervalEnd));
            return true;
        }

        band = default;
        return false;
    }

    private static string BandRole(
        long start,
        long end,
        long intervalStart,
        long intervalCenter,
        long intervalEnd)
    {
        if (start <= intervalStart && end < intervalCenter)
        {
            return "start";
        }

        if (start > intervalCenter && end >= intervalEnd)
        {
            return "end";
        }

        if (start <= intervalCenter && end >= intervalCenter)
        {
            return "center";
        }

        return "interior";
    }

    private static bool TryFindLandmark(
        ConceptDefinitionDiscoverySurface.DefinitionGraph graph,
        ConceptDefinitionDiscoverySurface.DefinitionNode interval,
        Carrier carrier,
        long ticks,
        out ConceptDefinitionDiscoverySurface.DefinitionNode landmark)
    {
        foreach (var node in graph.Nodes)
        {
            if (graph.DependsOn(node, interval) &&
                TryReadPoint(node.Read.Body, out var point) &&
                point.Carrier.SameIdentityAs(carrier) &&
                point.Ticks == ticks)
            {
                landmark = node;
                return true;
            }
        }

        landmark = default;
        return false;
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
}
