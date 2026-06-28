using Gojo.Core;

namespace Gojo.Encoding.Tests;

/// <summary>
/// Test-only reader for generic one-dimensional interval utilities. It reads
/// start, center, end, and adjacent bands from carrier points/segments and
/// dependency structure, without depending on the readable names of the bands.
/// </summary>
internal static class CommonIntervalReadSurface
{
    public readonly record struct IntervalBandRead(
        string DefinitionName,
        long StartTicks,
        long EndTicks,
        string Role,
        string Qualifier);

    public readonly record struct IntervalRead(
        string DefinitionName,
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
        long Ticks,
        string BandName,
        string Role,
        string Qualifier);

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
            if (!TryFindLandmark(graph, node, carrier, start, out var startLandmark) ||
                !TryFindLandmark(graph, node, carrier, center, out var centerLandmark) ||
                !TryFindLandmark(graph, node, carrier, end, out var endLandmark))
            {
                continue;
            }

            var bands = ReadBands(graph, node, carrier, start, center, end);
            if (bands.Length == 0)
            {
                continue;
            }

            reads.Add(new IntervalRead(
                node.Name,
                carrier,
                start,
                center,
                end,
                startLandmark.Name,
                centerLandmark.Name,
                endLandmark.Name,
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
                        ticks,
                        band.DefinitionName,
                        band.Role,
                        band.Qualifier);
                    return true;
                }
            }
        }

        location = default;
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
            if (node.Index == interval.Index ||
                !graph.DependsOn(node, interval) ||
                !TryReadSegment(node.Read.Body, out var bandCarrier, out var start, out var end) ||
                !bandCarrier.SameIdentityAs(carrier))
            {
                continue;
            }

            var role = BandRole(start, end, intervalStart, intervalCenter, intervalEnd);
            bands.Add(new IntervalBandRead(
                node.Name,
                start,
                end,
                role,
                QualifierFor(role)));
        }

        return [.. bands];
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

    private static string QualifierFor(string role) =>
        role switch
        {
            "start" => "near",
            "center" => "middle",
            "end" => "far",
            _ => "unsettled"
        };

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
