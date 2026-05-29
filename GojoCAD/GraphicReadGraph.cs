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
        IReadOnlyList<GraphicRead> reads,
        GraphicReadIndex index)
    {
        Root = root;
        Nodes = nodes;
        Reads = reads;
        this.index = index;
    }

    private readonly GraphicReadIndex index;

    public GraphicReadNode Root { get; }

    public IReadOnlyList<GraphicReadNode> Nodes { get; }

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
        return [.. Reads.Where(read =>
            read.Source is Relation relation &&
            affectedSources.Contains(relation.Id))];
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
            var reads = nodes.SelectMany(node => node.LocalReads).ToList();
            reads.AddRange(DerivePartitions(reads).ToArray());
            reads.AddRange(DeriveRadiusAttachments(reads).ToArray());
            return new GraphicReadGraph(root, [.. nodes], reads, GraphicReadIndex.From(nodes));
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

        private static IEnumerable<GraphicRead> DeriveRadiusAttachments(IReadOnlyList<GraphicRead> reads)
        {
            foreach (var pair in reads.OfType<PointPairRead>())
            {
                if (pair.Left.CircularMarker is not null && !IsCircularCarrier(pair.Right.Location.Carrier))
                {
                    yield return new RadiusAttachmentRead(pair.Source, pair.Right, pair.Left.CircularMarker);
                }

                if (pair.Right.CircularMarker is not null && !IsCircularCarrier(pair.Left.Location.Carrier))
                {
                    yield return new RadiusAttachmentRead(pair.Source, pair.Left, pair.Right.CircularMarker);
                }
            }
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

        private static bool ReadCircularOrigin(Carrier carrier, out CarrierLocation origin) =>
            carrier.TryReadCircularOrigin(out origin);
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

public abstract record GraphicRead(IElement Source);

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
    CircularMarkerRead Marker) : GraphicRead(Source);

public sealed record SegmentPartitionRead(
    IElement Source,
    PositiveSegmentRead Segment,
    PointRead Point) : GraphicRead(Source);

public sealed record ContourPartitionRead(
    IElement Source,
    CircularSpanRead Span,
    CircularMarkerRead Marker) : GraphicRead(Source);
