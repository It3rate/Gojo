using Gojo.Core;

namespace GojoCAD;

/// <summary>
/// Derived inspection graph for GojoCAD relation structure. The graph is a
/// disposable read surface; graphic meaning remains in the source relations.
/// </summary>
public sealed class GraphicReadGraph
{
    private GraphicReadGraph(
        GraphicReadNode root,
        IReadOnlyList<GraphicReadNode> nodes,
        IReadOnlyList<GraphicReadBand> bands,
        IReadOnlyList<GraphicRead> reads,
        GraphicReadIndex index)
    {
        Root = root;
        Nodes = nodes;
        Bands = bands;
        Reads = reads;
        this.index = index;
    }

    private readonly GraphicReadIndex index;

    public GraphicReadNode Root { get; }

    public IReadOnlyList<GraphicReadNode> Nodes { get; }

    public IReadOnlyList<GraphicReadBand> Bands { get; }

    public IReadOnlyList<GraphicRead> Reads { get; }

    public IEnumerable<TRead> ReadsOf<TRead>()
        where TRead : GraphicRead =>
        Reads.OfType<TRead>();

    public IReadOnlyList<GraphicReadNode> NavigateFrom(IElement changed, int radius = 2)
    {
        if (changed is null)
        {
            return [];
        }

        var boundedRadius = Math.Max(0, radius);
        var found = new HashSet<GraphicReadNode>();
        var pending = new Queue<(GraphicReadNode Node, int Depth)>();
        foreach (var seed in index.SeedsFor(changed))
        {
            if (found.Add(seed))
            {
                pending.Enqueue((seed, 0));
            }
        }

        while (pending.Count != 0)
        {
            var (node, depth) = pending.Dequeue();
            if (depth >= boundedRadius)
            {
                continue;
            }

            foreach (var next in index.NeighborsOf(node))
            {
                if (found.Add(next))
                {
                    pending.Enqueue((next, depth + 1));
                }
            }
        }

        return [.. found.OrderBy(node => node.Order)];
    }

    public IReadOnlyList<GraphicRead> AffectedReadsFrom(IElement changed, int radius = 2)
    {
        var affectedSources = NavigateFrom(changed, radius)
            .Select(node => node.Source)
            .OfType<Relation>()
            .Select(relation => relation.Id)
            .ToHashSet();
        return [.. Reads.Where(read => read.Sources
            .OfType<Relation>()
            .Any(relation => affectedSources.Contains(relation.Id)))];
    }

    public GraphicReadRefresh RefreshFrom(IElement changed, int radius = 2)
    {
        var previousNodes = NavigateFrom(changed, radius);
        var previousReads = AffectedReadsFrom(changed, radius);
        var next = From(Root.Source);
        return new GraphicReadRefresh(
            next,
            previousNodes,
            previousReads,
            next.NavigateFrom(changed, radius),
            next.AffectedReadsFrom(changed, radius));
    }

    public static GraphicReadGraph From(IElement source) =>
        new Reader().Read(source ?? throw new ArgumentNullException(nameof(source)));

    private sealed class Reader
    {
        private readonly List<GraphicReadNode> nodes = [];

        public GraphicReadGraph Read(IElement source)
        {
            var root = Visit(source);
            var bands = new List<GraphicReadBand>();
            var reads = new List<GraphicRead>();

            AddBand(bands, reads, order: 0, nodes.SelectMany(node => node.LocalReads));
            AddBand(bands, reads, order: 1, DeriveIncidenceReads(reads));
            AddBand(bands, reads, order: 2, DeriveSecondOrderControls(reads));

            return new GraphicReadGraph(root, [.. nodes], [.. bands], [.. reads], GraphicReadIndex.From(nodes));
        }

        private GraphicReadNode Visit(IElement source)
        {
            if (source is not Relation relation)
            {
                var atom = new GraphicReadNode(source, left: null, right: null, []);
                nodes.Add(atom);
                return atom;
            }

            var left = Visit(relation.Left);
            var right = Visit(relation.Right);
            var localReads = Compose(relation, left, right);
            var node = new GraphicReadNode(relation, left, right, localReads);
            left.AddParent(node);
            right.AddParent(node);
            nodes.Add(node);
            return node;
        }

        private static IReadOnlyList<GraphicRead> Compose(
            Relation relation,
            GraphicReadNode left,
            GraphicReadNode right)
        {
            var reads = new List<GraphicRead>();
            if (RecognizePoint(relation, out var point))
            {
                AddPointReads(reads, point);
                return reads;
            }

            foreach (var leftPoint in left.LocalReads.OfType<PointRead>())
            foreach (var rightPoint in right.LocalReads.OfType<PointRead>())
            {
                var pair = new PointPairRead(relation, leftPoint, rightPoint);
                reads.Add(pair);
                AddPointPairReads(reads, pair);
            }

            foreach (var outlet in right.LocalReads.OfType<ZeroSpanRead>().Where(IsUnknownZeroSpan))
            {
                foreach (var sourcePoint in left.LocalReads.OfType<PointRead>())
                {
                    var orthogonal = new OrthogonalOutletRead(relation, sourcePoint, outlet);
                    reads.Add(orthogonal);
                    reads.Add(new RadialSeedRead(relation, sourcePoint, orthogonal));
                }

                foreach (var zeroSpan in left.LocalReads.OfType<ZeroSpanRead>())
                {
                    var orthogonal = new OrthogonalOutletRead(relation, zeroSpan, outlet);
                    reads.Add(orthogonal);
                    reads.Add(new LineSeedRead(relation, zeroSpan, orthogonal));
                }

                foreach (var segment in left.LocalReads.OfType<PositiveSegmentRead>())
                {
                    var orthogonal = new OrthogonalOutletRead(relation, segment, outlet);
                    reads.Add(orthogonal);
                    reads.Add(new SheetSeedRead(relation, segment, orthogonal));
                }
            }

            return reads;
        }

        private static void AddPointPairReads(List<GraphicRead> reads, PointPairRead pair)
        {
            var left = pair.Left.Location;
            var right = pair.Right.Location;
            if (left.Carrier.SameIdentityAs(right.Carrier) &&
                left.Ticks == right.Ticks)
            {
                reads.Add(new ZeroSpanRead(pair.Source, pair.Left, pair.Right));
                return;
            }

            if (left.Carrier.SameIdentityAs(right.Carrier) &&
                !left.Carrier.IsUnknown &&
                !left.Carrier.IsInverse &&
                !right.Carrier.IsInverse)
            {
                var segment = new PositiveSegmentRead(pair.Source, pair.Left, pair.Right);
                reads.Add(segment);
                if (ReadCircularOrigin(left.Carrier, out var origin))
                {
                    reads.Add(new CircularSpanRead(pair.Source, segment, origin));
                }
            }

            if (IsDirectedPair(left, right))
            {
                reads.Add(new DirectedSegmentRead(pair.Source, pair.Left, pair.Right, IsSourceOrderReversed: false));
            }
            else if (IsDirectedPair(right, left))
            {
                reads.Add(new DirectedSegmentRead(pair.Source, pair.Right, pair.Left, IsSourceOrderReversed: true));
            }
        }

        private static IEnumerable<GraphicRead> DeriveIncidenceReads(IReadOnlyList<GraphicRead> reads)
        {
            foreach (var attachment in DerivePointAttachments(reads))
            {
                yield return attachment;
            }

            foreach (var site in DeriveSharedSites(reads))
            {
                yield return site;
            }

            foreach (var partition in DerivePartitions(reads))
            {
                yield return partition;
            }

            foreach (var connection in DeriveSegmentConnections(reads))
            {
                yield return connection;
            }
        }

        private static IEnumerable<GraphicRead> DerivePointAttachments(IReadOnlyList<GraphicRead> reads)
        {
            foreach (var pair in reads.OfType<PointPairRead>())
            {
                if (!NormalizeCarrier(pair.Left.Location.Carrier).SameIdentityAs(
                    NormalizeCarrier(pair.Right.Location.Carrier)))
                {
                    yield return new PointAttachmentRead(pair.Source, pair.Left, pair.Right, pair);
                }
            }
        }

        private static IEnumerable<GraphicRead> DeriveSharedSites(IReadOnlyList<GraphicRead> reads)
        {
            var points = reads.OfType<PointRead>().ToArray();
            foreach (var group in ReadSharedPointGroups(points))
            {
                yield return new SharedSiteRead(group[0].Source, group);
            }
        }

        private static IEnumerable<GraphicRead> DerivePartitions(IReadOnlyList<GraphicRead> reads)
        {
            var points = reads.OfType<PointRead>().ToArray();
            foreach (var segment in reads.OfType<PositiveSegmentRead>())
            {
                foreach (var point in points.Where(point => IsInteriorPointOnSegment(point.Location, segment)))
                {
                    yield return new SegmentPartitionRead(point.Source, segment, point);
                }
            }

            foreach (var span in reads.OfType<CircularSpanRead>())
            {
                foreach (var marker in reads.OfType<CircularMarkerRead>()
                    .Where(marker => IsInteriorPointOnCircularSpan(marker.Point.Location, span)))
                {
                    yield return new ContourPartitionRead(marker.Source, span, marker);
                }
            }
        }

        private static IEnumerable<GraphicRead> DeriveSegmentConnections(IReadOnlyList<GraphicRead> reads)
        {
            var endpoints = reads
                .OfType<PositiveSegmentRead>()
                .SelectMany(segment => new[]
                {
                    new SegmentEndpointRead(segment, segment.Start, IsStart: true),
                    new SegmentEndpointRead(segment, segment.End, IsStart: false)
                })
                .ToArray();

            foreach (var group in ReadSharedEndpointGroups(endpoints))
            {
                for (var leftIndex = 0; leftIndex < group.Count; leftIndex++)
                for (var rightIndex = leftIndex + 1; rightIndex < group.Count; rightIndex++)
                {
                    var left = group[leftIndex];
                    var right = group[rightIndex];
                    if (!ReferenceEquals(left.Segment, right.Segment))
                    {
                        yield return new SegmentConnectionRead(
                            left.Segment.Source,
                            left,
                            right);
                    }
                }
            }
        }

        private static IEnumerable<GraphicRead> DeriveRadiusAttachments(IReadOnlyList<GraphicRead> reads)
        {
            foreach (var pair in reads.OfType<PointPairRead>())
            {
                if (pair.Left.CircularMarker is not null &&
                    IsRadiusBasePoint(pair.Right.Location, pair.Left.CircularMarker))
                {
                    yield return new RadiusAttachmentRead(pair.Source, pair.Right, pair.Left.CircularMarker);
                }

                if (pair.Right.CircularMarker is not null &&
                    IsRadiusBasePoint(pair.Left.Location, pair.Right.CircularMarker))
                {
                    yield return new RadiusAttachmentRead(pair.Source, pair.Left, pair.Right.CircularMarker);
                }
            }
        }

        private static IEnumerable<GraphicRead> DeriveSecondOrderControls(IReadOnlyList<GraphicRead> reads)
        {
            foreach (var radius in DeriveRadiusAttachments(reads))
            {
                yield return radius;
            }

            foreach (var witness in DeriveLinearBasisWitnesses(reads))
            {
                yield return witness;
            }

            foreach (var junction in DeriveSegmentJunctions(reads))
            {
                yield return junction;
            }
        }

        private static IEnumerable<GraphicRead> DeriveSegmentJunctions(IReadOnlyList<GraphicRead> reads)
        {
            foreach (var site in reads.OfType<SharedSiteRead>())
            {
                var endpoints = reads
                    .OfType<SegmentConnectionRead>()
                    .SelectMany(connection => new[] { connection.Left, connection.Right })
                    .Where(endpoint => site.Points.Any(point =>
                        point.Location.SameTickRefAs(endpoint.Point.Location) ||
                        SameNormalizedLocation(point.Location, endpoint.Point.Location)))
                    .GroupBy(endpoint => endpoint.Segment.Source)
                    .Select(group => group.First())
                    .ToArray();
                if (endpoints.Length >= 3)
                {
                    yield return new SegmentJunctionRead(site.Source, site, endpoints);
                }
            }
        }

        private static IEnumerable<GraphicRead> DeriveLinearBasisWitnesses(IReadOnlyList<GraphicRead> reads)
        {
            foreach (var pair in reads.OfType<PointPairRead>())
            {
                if (TryReadLinearBasisWitness(pair, pair.Left, pair.Right, out var leftWitness))
                {
                    yield return leftWitness;
                }

                if (TryReadLinearBasisWitness(pair, pair.Right, pair.Left, out var rightWitness))
                {
                    yield return rightWitness;
                }
            }
        }

        private static bool TryReadLinearBasisWitness(
            PointPairRead pair,
            PointRead linearPoint,
            PointRead witnessPoint,
            out LinearBasisWitnessRead witness)
        {
            var linearLocation = Normalize(linearPoint.Location);
            var carrier = NormalizeCarrier(linearLocation.Carrier);
            if (!carrier.TryReadLinearOrigin(out var sourceOrigin) ||
                linearLocation.Ticks == LinearCarrierOriginTicks ||
                witnessPoint.Location.Carrier.SameIdentityAs(linearPoint.Location.Carrier))
            {
                witness = default!;
                return false;
            }

            witness = new LinearBasisWitnessRead(
                pair.Source,
                carrier,
                LinearCarrierOriginTicks,
                sourceOrigin,
                linearLocation,
                linearPoint,
                witnessPoint,
                pair);
            return true;
        }

        private static void AddPointReads(List<GraphicRead> reads, PointRead point)
        {
            reads.Add(point);
            if (ReadCircularOrigin(point.Location.Carrier, out var origin))
            {
                point.CircularMarker = new CircularMarkerRead(point.Source, point, origin);
                reads.Add(point.CircularMarker);
            }
        }

        private static bool RecognizePoint(Relation relation, out PointRead point)
        {
            if (relation.Left is CarrierLocation location &&
                relation.Right is Carrier carrier &&
                location.Carrier.SameIdentityAs(carrier))
            {
                point = new PointRead(relation, location);
                return true;
            }

            point = default!;
            return false;
        }

        private static bool IsUnknownZeroSpan(ZeroSpanRead span) =>
            span.Left.Location.Carrier.IsUnknown &&
            span.Right.Location.Carrier.IsUnknown &&
            span.Left.Location.Ticks == 0 &&
            span.Right.Location.Ticks == 0;

        private static bool IsDirectedPair(CarrierLocation inverse, CarrierLocation positive) =>
            inverse.Carrier.IsInverse &&
            !positive.Carrier.IsInverse &&
            inverse.Carrier.Inverse().SameIdentityAs(positive.Carrier);

        private static bool IsInteriorPointOnSegment(CarrierLocation point, PositiveSegmentRead segment)
        {
            if (!point.Carrier.SameIdentityAs(segment.Start.Location.Carrier))
            {
                return false;
            }

            var start = segment.Start.Location.Ticks;
            var end = segment.End.Location.Ticks;
            var min = Math.Min(start, end);
            var max = Math.Max(start, end);
            return point.Ticks > min && point.Ticks < max;
        }

        private static bool IsInteriorPointOnCircularSpan(CarrierLocation point, CircularSpanRead span)
        {
            if (!point.Carrier.SameIdentityAs(span.Segment.Start.Location.Carrier))
            {
                return false;
            }

            var start = span.Segment.Start.Location.Ticks;
            var end = span.Segment.End.Location.Ticks;
            var min = Math.Min(start, end);
            var max = Math.Max(start, end);
            return point.Ticks > min && point.Ticks < max;
        }

        private static bool IsCircularCarrier(Carrier carrier) => ReadCircularOrigin(carrier, out _);

        private static bool IsRadiusBasePoint(CarrierLocation location, CircularMarkerRead marker) =>
            !IsCircularCarrier(location.Carrier) &&
            location.Carrier.SameIdentityAs(marker.Origin.Carrier);

        private static bool ReadCircularOrigin(Carrier carrier, out CarrierLocation origin) =>
            carrier.TryReadCircularOrigin(out origin);

        private static CarrierLocation Normalize(CarrierLocation location) =>
            location.Carrier.IsInverse
                ? new CarrierLocation(location.Carrier.Inverse(), checked(-location.Ticks))
                : location;

        private static Carrier NormalizeCarrier(Carrier carrier) => carrier.IsInverse ? carrier.Inverse() : carrier;

        private static bool SameNormalizedLocation(CarrierLocation left, CarrierLocation right)
        {
            var normalizedLeft = Normalize(left);
            var normalizedRight = Normalize(right);
            return normalizedLeft.Carrier.SameIdentityAs(normalizedRight.Carrier) &&
                normalizedLeft.Ticks == normalizedRight.Ticks;
        }

        private static IEnumerable<IReadOnlyList<PointRead>> ReadSharedPointGroups(IReadOnlyList<PointRead> points)
        {
            var emitted = new HashSet<string>();
            foreach (var group in points
                .GroupBy(point => LocationGroupKey.Of(point.Location))
                .Where(group => group.Count() > 1)
                .Select(group => group.ToArray()))
            {
                if (emitted.Add(PointGroupSignature(group)))
                {
                    yield return group;
                }
            }

            foreach (var group in points
                .Where(point => point.Location.Tick is not null)
                .GroupBy(point => point.Location.Tick.Id)
                .Where(group => group.Count() > 1)
                .Select(group => group.ToArray()))
            {
                if (emitted.Add(PointGroupSignature(group)))
                {
                    yield return group;
                }
            }
        }

        private static IEnumerable<IReadOnlyList<SegmentEndpointRead>> ReadSharedEndpointGroups(
            IReadOnlyList<SegmentEndpointRead> endpoints)
        {
            var emitted = new HashSet<string>();
            foreach (var group in endpoints
                .GroupBy(endpoint => LocationGroupKey.Of(endpoint.Point.Location))
                .Where(group => group.Count() > 1)
                .Select(group => group.ToArray()))
            {
                if (emitted.Add(EndpointGroupSignature(group)))
                {
                    yield return group;
                }
            }

            foreach (var group in endpoints
                .Where(endpoint => endpoint.Point.Location.Tick is not null)
                .GroupBy(endpoint => endpoint.Point.Location.Tick.Id)
                .Where(group => group.Count() > 1)
                .Select(group => group.ToArray()))
            {
                if (emitted.Add(EndpointGroupSignature(group)))
                {
                    yield return group;
                }
            }
        }

        private static string PointGroupSignature(IEnumerable<PointRead> points) =>
            string.Join(",", points
                .Select(point => point.Source is Relation relation ? relation.Id : 0UL)
                .Order());

        private static string EndpointGroupSignature(IEnumerable<SegmentEndpointRead> endpoints) =>
            string.Join(",", endpoints
                .Select(endpoint =>
                    $"{(endpoint.Segment.Source is Relation relation ? relation.Id : 0UL)}:{endpoint.IsStart}")
                .Order());

        private static void AddBand(
            List<GraphicReadBand> bands,
            List<GraphicRead> reads,
            int order,
            IEnumerable<GraphicRead> nextReads)
        {
            var bandReads = nextReads.ToArray();
            if (bandReads.Length == 0)
            {
                return;
            }

            bands.Add(new GraphicReadBand(order, bandReads));
            reads.AddRange(bandReads);
        }

        private const long LinearCarrierOriginTicks = 0;

        private readonly record struct LocationGroupKey(ulong CarrierId, long Ticks)
        {
            public static LocationGroupKey Of(CarrierLocation location)
            {
                var normalized = Normalize(location);
                return new LocationGroupKey(NormalizeCarrier(normalized.Carrier).Id, normalized.Ticks);
            }
        }
    }
}

internal sealed class GraphicReadIndex
{
    private readonly Dictionary<ulong, List<GraphicReadNode>> relationNodes = [];
    private readonly Dictionary<ulong, List<GraphicReadNode>> carrierNodes = [];
    private readonly Dictionary<ulong, List<GraphicReadNode>> tickRefNodes = [];

    private GraphicReadIndex()
    {
    }

    public static GraphicReadIndex From(IEnumerable<GraphicReadNode> nodes)
    {
        var index = new GraphicReadIndex();
        foreach (var node in nodes)
        {
            index.AddNode(node);
        }

        return index;
    }

    public IReadOnlyList<GraphicReadNode> SeedsFor(IElement changed)
    {
        var seeds = new List<GraphicReadNode>();
        switch (changed)
        {
            case Relation relation:
                AddFrom(relationNodes, relation.Id, seeds);
                break;
            case Carrier carrier:
                AddFrom(carrierNodes, carrier.Id, seeds);
                break;
            case CarrierLocation location:
                AddLocationSeeds(location, seeds);
                break;
            case TickRef tick:
                AddFrom(tickRefNodes, tick.Id, seeds);
                break;
        }

        return [.. seeds.Distinct()];
    }

    public IEnumerable<GraphicReadNode> NeighborsOf(GraphicReadNode node)
    {
        if (node.Left is not null)
        {
            yield return node.Left;
        }

        if (node.Right is not null)
        {
            yield return node.Right;
        }

        foreach (var parent in node.Parents)
        {
            yield return parent;
        }

        foreach (var location in DirectLocationsOf(node.Source))
        {
            foreach (var shared in NodesForLocation(location))
            {
                if (!ReferenceEquals(shared, node))
                {
                    yield return shared;
                }
            }
        }

        foreach (var carrier in DirectCarriersOf(node.Source))
        {
            foreach (var shared in NodesForCarrier(carrier))
            {
                if (!ReferenceEquals(shared, node))
                {
                    yield return shared;
                }
            }
        }
    }

    private void AddNode(GraphicReadNode node)
    {
        if (node.Source is Relation relation)
        {
            AddTo(relationNodes, relation.Id, node);
        }

        foreach (var carrier in DirectCarriersOf(node.Source))
        {
            AddTo(carrierNodes, carrier.Id, node);
        }

        foreach (var location in DirectLocationsOf(node.Source))
        {
            if (location.Tick is not null)
            {
                AddTo(tickRefNodes, location.Tick.Id, node);
            }
        }
    }

    private void AddLocationSeeds(CarrierLocation location, List<GraphicReadNode> seeds)
    {
        if (location.Tick is not null)
        {
            AddFrom(tickRefNodes, location.Tick.Id, seeds);
        }

        AddFrom(carrierNodes, location.Carrier.Id, seeds);
    }

    private IEnumerable<GraphicReadNode> NodesForLocation(CarrierLocation location)
    {
        if (location.Tick is not null &&
            tickRefNodes.TryGetValue(location.Tick.Id, out var byTick))
        {
            foreach (var node in byTick)
            {
                yield return node;
            }
        }
    }

    private IEnumerable<GraphicReadNode> NodesForCarrier(Carrier carrier) =>
        carrierNodes.TryGetValue(carrier.Id, out var nodes)
            ? nodes
            : [];

    private static CarrierLocation[] DirectLocationsOf(IElement element) => element switch
    {
        CarrierLocation location => [location],
        Relation { Left: CarrierLocation left, Right: CarrierLocation right } => [left, right],
        Relation { Left: CarrierLocation left } => [left],
        Relation { Right: CarrierLocation right } => [right],
        _ => []
    };

    private static Carrier[] DirectCarriersOf(IElement element) => element switch
    {
        Carrier carrier => [carrier],
        CarrierLocation location => [location.Carrier],
        Relation { Left: Carrier left, Right: Carrier right } => [left, right],
        Relation { Left: Carrier left, Right: CarrierLocation right } => [left, right.Carrier],
        Relation { Left: CarrierLocation left, Right: Carrier right } => [left.Carrier, right],
        Relation { Left: Carrier left } => [left],
        Relation { Right: Carrier right } => [right],
        Relation { Left: CarrierLocation left } => [left.Carrier],
        Relation { Right: CarrierLocation right } => [right.Carrier],
        _ => []
    };

    private static void AddTo(
        Dictionary<ulong, List<GraphicReadNode>> index,
        ulong key,
        GraphicReadNode node)
    {
        if (!index.TryGetValue(key, out var nodes))
        {
            nodes = [];
            index[key] = nodes;
        }

        if (!nodes.Contains(node))
        {
            nodes.Add(node);
        }
    }

    private static void AddFrom(
        Dictionary<ulong, List<GraphicReadNode>> index,
        ulong key,
        List<GraphicReadNode> target)
    {
        if (index.TryGetValue(key, out var nodes))
        {
            target.AddRange(nodes);
        }
    }
}

public sealed class GraphicReadNode
{
    private static ulong orderCounter;
    private readonly List<GraphicReadNode> parents = [];

    internal GraphicReadNode(
        IElement source,
        GraphicReadNode? left,
        GraphicReadNode? right,
        IReadOnlyList<GraphicRead> localReads)
    {
        Order = NextOrder();
        Source = source;
        Left = left;
        Right = right;
        LocalReads = localReads;
    }

    public ulong Order { get; }

    public IElement Source { get; }

    public GraphicReadNode? Left { get; }

    public GraphicReadNode? Right { get; }

    public IReadOnlyList<GraphicReadNode> Parents => parents;

    public IReadOnlyList<GraphicRead> LocalReads { get; }

    internal void AddParent(GraphicReadNode parent)
    {
        if (!parents.Contains(parent))
        {
            parents.Add(parent);
        }
    }

    private static ulong NextOrder() => orderCounter++;
}

public sealed record GraphicReadRefresh(
    GraphicReadGraph CurrentGraph,
    IReadOnlyList<GraphicReadNode> PreviousNodes,
    IReadOnlyList<GraphicRead> PreviousReads,
    IReadOnlyList<GraphicReadNode> CurrentNodes,
    IReadOnlyList<GraphicRead> CurrentReads);

public sealed record GraphicReadBand(int Order, IReadOnlyList<GraphicRead> Reads);

public abstract record GraphicRead(IElement Source)
{
    public virtual IEnumerable<IElement> Sources => [Source];
}

public sealed record PointRead(IElement Source, CarrierLocation Location) : GraphicRead(Source)
{
    public CircularMarkerRead? CircularMarker { get; internal set; }
}

public sealed record PointPairRead(IElement Source, PointRead Left, PointRead Right) : GraphicRead(Source);

public sealed record PositiveSegmentRead(IElement Source, PointRead Start, PointRead End) : GraphicRead(Source);

public sealed record ZeroSpanRead(IElement Source, PointRead Left, PointRead Right) : GraphicRead(Source);

public sealed record DirectedSegmentRead(
    IElement Source,
    PointRead InverseStart,
    PointRead PositiveEnd,
    bool IsSourceOrderReversed) : GraphicRead(Source);

public sealed record OrthogonalOutletRead(
    IElement Source,
    GraphicRead SourceRead,
    ZeroSpanRead OutletSpan) : GraphicRead(Source);

public sealed record RadialSeedRead(
    IElement Source,
    PointRead Point,
    OrthogonalOutletRead Outlet) : GraphicRead(Source);

public sealed record LineSeedRead(
    IElement Source,
    ZeroSpanRead Span,
    OrthogonalOutletRead Outlet) : GraphicRead(Source);

public sealed record SheetSeedRead(
    IElement Source,
    PositiveSegmentRead Span,
    OrthogonalOutletRead Outlet) : GraphicRead(Source);

public sealed record CircularMarkerRead(
    IElement Source,
    PointRead Point,
    CarrierLocation Origin) : GraphicRead(Source);

public sealed record CircularSpanRead(
    IElement Source,
    PositiveSegmentRead Segment,
    CarrierLocation Origin) : GraphicRead(Source);

public sealed record RadiusAttachmentRead(
    IElement Source,
    PointRead BasePoint,
    CircularMarkerRead Marker) : GraphicRead(Source)
{
    public override IEnumerable<IElement> Sources => [Source, BasePoint.Source, Marker.Point.Source];
}

public sealed record LinearBasisWitnessRead(
    IElement Source,
    Carrier LinearCarrier,
    long LinearOriginTicks,
    CarrierLocation SourceOrigin,
    CarrierLocation LinearLocation,
    PointRead LinearPoint,
    PointRead WitnessPoint,
    PointPairRead Attachment) : GraphicRead(Source)
{
    public override IEnumerable<IElement> Sources => [Source, LinearPoint.Source, WitnessPoint.Source];
}

public sealed record PointAttachmentRead(
    IElement Source,
    PointRead Left,
    PointRead Right,
    PointPairRead Pair) : GraphicRead(Source)
{
    public override IEnumerable<IElement> Sources => [Source, Left.Source, Right.Source];
}

public sealed record SharedSiteRead(
    IElement Source,
    IReadOnlyList<PointRead> Points) : GraphicRead(Source)
{
    public override IEnumerable<IElement> Sources => Points.Select(point => point.Source).Prepend(Source);
}

public sealed record SegmentEndpointRead(
    PositiveSegmentRead Segment,
    PointRead Point,
    bool IsStart);

public sealed record SegmentConnectionRead(
    IElement Source,
    SegmentEndpointRead Left,
    SegmentEndpointRead Right) : GraphicRead(Source)
{
    public override IEnumerable<IElement> Sources =>
    [
        Source,
        Left.Segment.Source,
        Left.Point.Source,
        Right.Segment.Source,
        Right.Point.Source
    ];
}

public sealed record SegmentJunctionRead(
    IElement Source,
    SharedSiteRead Site,
    IReadOnlyList<SegmentEndpointRead> Endpoints) : GraphicRead(Source)
{
    public override IEnumerable<IElement> Sources =>
        Site.Sources.Concat(Endpoints.SelectMany(endpoint => new[]
        {
            endpoint.Segment.Source,
            endpoint.Point.Source
        }));
}

public sealed record SegmentPartitionRead(
    IElement Source,
    PositiveSegmentRead Segment,
    PointRead Point) : GraphicRead(Source)
{
    public override IEnumerable<IElement> Sources => [Source, Segment.Source, Point.Source];
}

public sealed record ContourPartitionRead(
    IElement Source,
    CircularSpanRead Span,
    CircularMarkerRead Marker) : GraphicRead(Source)
{
    public override IEnumerable<IElement> Sources => [Source, Span.Source, Marker.Point.Source];
}
