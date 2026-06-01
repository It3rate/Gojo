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
            AddBand(bands, reads, order: 4, DeriveBoundaryControls(reads));

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
            if (RecognizeSharedFramePoint(relation, out var framePoint))
            {
                reads.Add(framePoint);
            }

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

            foreach (var leftFramePoint in left.LocalReads.OfType<SharedFramePointRead>())
            foreach (var rightFramePoint in right.LocalReads.OfType<SharedFramePointRead>())
            {
                if (HaveSameFrameBasis(leftFramePoint, rightFramePoint) &&
                    !SameFramePoint(leftFramePoint, rightFramePoint))
                {
                    reads.Add(new SharedFrameSegmentRead(relation, leftFramePoint, rightFramePoint));
                }
            }

            foreach (var leftFramePoint in left.LocalReads.OfType<SharedFramePointRead>())
            foreach (var rightPoint in right.LocalReads.OfType<PointRead>())
            {
                reads.Add(new SharedFramePointAttachmentRead(relation, leftFramePoint, rightPoint));
            }

            foreach (var leftPoint in left.LocalReads.OfType<PointRead>())
            foreach (var rightFramePoint in right.LocalReads.OfType<SharedFramePointRead>())
            {
                reads.Add(new SharedFramePointAttachmentRead(relation, rightFramePoint, leftPoint));
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
                reads.Add(new PathSegmentRead(pair.Source, segment, pair.Left, pair.Right));
                if (ReadCircularOrigin(left.Carrier, out var origin))
                {
                    reads.Add(new CircularSpanRead(pair.Source, segment, origin));
                }
            }
            else if (left.Carrier.SameIdentityAs(right.Carrier) &&
                !left.Carrier.IsUnknown &&
                left.Carrier.IsInverse &&
                right.Carrier.IsInverse)
            {
                var segment = new InverseSegmentRead(pair.Source, pair.Left, pair.Right);
                reads.Add(segment);
                reads.Add(new PathSegmentRead(pair.Source, segment, pair.Left, pair.Right));
            }

            if (IsDirectedPair(left, right))
            {
                var segment = new DirectedSegmentRead(pair.Source, pair.Left, pair.Right, IsSourceOrderReversed: false);
                reads.Add(segment);
                reads.Add(new PathSegmentRead(pair.Source, segment, pair.Left, pair.Right));
            }
            else if (IsDirectedPair(right, left))
            {
                var segment = new DirectedSegmentRead(pair.Source, pair.Right, pair.Left, IsSourceOrderReversed: true);
                reads.Add(segment);
                reads.Add(new PathSegmentRead(pair.Source, segment, pair.Right, pair.Left));
            }
        }

        private static IEnumerable<GraphicRead> DeriveIncidenceReads(IReadOnlyList<GraphicRead> reads)
        {
            var working = reads.ToList();

            var units = DeriveCarrierUnitReads(working).ToArray();
            foreach (var unit in units)
            {
                yield return unit;
            }

            working.AddRange(units);

            var attachments = DerivePointAttachments(working).ToArray();
            foreach (var attachment in attachments)
            {
                yield return attachment;
            }

            working.AddRange(attachments);

            var sites = DeriveSharedSites(working).ToArray();
            foreach (var site in sites)
            {
                yield return site;
            }

            working.AddRange(sites);

            var partitions = DerivePartitions(working).ToArray();
            foreach (var partition in partitions)
            {
                yield return partition;
            }

            working.AddRange(partitions);

            foreach (var connection in DeriveSegmentConnections(working))
            {
                yield return connection;
            }
        }

        private static IEnumerable<GraphicRead> DeriveCarrierUnitReads(IReadOnlyList<GraphicRead> reads)
        {
            foreach (var point in reads
                .OfType<PointRead>()
                .Where(point => !point.Location.Carrier.IsUnknown)
                .GroupBy(point => point.Location.Carrier.Id)
                .Select(group => group.First()))
            {
                var carrier = point.Location.Carrier;
                var unitShape = carrier.UnitShape;
                var hasOpposedFace = unitShape.TryReadOpposedFace(out var opposedSource);
                var hasOrthogonalSource = unitShape.TryReadOrthogonalSource(out var orthogonalSource);
                var hasResolution = unitShape.TryReadResolution(
                    out var resolutionSource,
                    out var resolutionSourceTicksPerTick);
                yield return new CarrierUnitRead(
                    point.Source,
                    carrier,
                    unitShape,
                    unitShape.Surface,
                    unitShape.DescriptorSurfaces,
                    hasOpposedFace,
                    hasOpposedFace ? opposedSource : null,
                    hasOrthogonalSource,
                    hasOrthogonalSource ? orthogonalSource : null,
                    hasResolution,
                    hasResolution ? resolutionSource : null,
                    hasResolution ? resolutionSourceTicksPerTick : 0);
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
            foreach (var group in ReadAllPointGroups(points, reads))
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

            foreach (var group in ReadAllSegmentEndpointGroups(endpoints, reads))
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

            var joins = DeriveSegmentJoins(reads).ToArray();
            foreach (var join in joins)
            {
                yield return join;
            }

            var joinFlows = DeriveSegmentJoinFlows(joins).ToArray();
            foreach (var flow in joinFlows)
            {
                yield return flow;
            }

            foreach (var compatibility in DeriveSegmentJoinUnitCompatibilities(reads, joinFlows))
            {
                yield return compatibility;
            }

            foreach (var junction in DeriveSegmentJunctions(reads))
            {
                yield return junction;
            }

            foreach (var alignment in DeriveLinearBasisAlignments(reads))
            {
                yield return alignment;
            }
        }

        private static IEnumerable<SegmentJoinRead> DeriveSegmentJoins(IReadOnlyList<GraphicRead> reads)
        {
            foreach (var site in reads.OfType<SharedSiteRead>())
            {
                var endpoints = ReadSegmentEndpointsAt(site, reads);
                if (endpoints.Length == 2)
                {
                    yield return new SegmentJoinRead(site.Source, site, endpoints[0], endpoints[1]);
                }
            }
        }

        private static IEnumerable<SegmentJoinFlowRead> DeriveSegmentJoinFlows(IReadOnlyList<SegmentJoinRead> joins)
        {
            foreach (var join in joins)
            {
                yield return new SegmentJoinFlowRead(
                    join.Source,
                    join,
                    ReadSegmentEndpointFlow(join.First),
                    ReadSegmentEndpointFlow(join.Second));
            }
        }

        private static IEnumerable<GraphicRead> DeriveSegmentJoinUnitCompatibilities(
            IReadOnlyList<GraphicRead> reads,
            IReadOnlyList<SegmentJoinFlowRead> flows)
        {
            var unitsByCarrier = reads
                .OfType<CarrierUnitRead>()
                .GroupBy(unit => unit.Carrier.Id)
                .ToDictionary(group => group.Key, group => group.First());

            foreach (var flow in flows)
            {
                if (unitsByCarrier.TryGetValue(flow.First.Carrier.Id, out var first) &&
                    unitsByCarrier.TryGetValue(flow.Second.Carrier.Id, out var second))
                {
                    yield return new SegmentJoinUnitCompatibilityRead(
                        flow.Source,
                        flow,
                        first,
                        second);
                }
            }
        }

        private static IEnumerable<GraphicRead> DeriveSegmentJunctions(IReadOnlyList<GraphicRead> reads)
        {
            foreach (var site in reads.OfType<SharedSiteRead>())
            {
                var endpoints = ReadSegmentEndpointsAt(site, reads);
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

        private static IEnumerable<GraphicRead> DeriveLinearBasisAlignments(IReadOnlyList<GraphicRead> reads)
        {
            foreach (var attachment in reads.OfType<PointAttachmentRead>())
            {
                if (TryReadLinearBasisPoint(attachment.Left, out var left) &&
                    TryReadLinearBasisPoint(attachment.Right, out var right) &&
                    !left.Carrier.SameIdentityAs(right.Carrier))
                {
                    yield return new LinearBasisAlignmentRead(
                        attachment.Source,
                        left,
                        right,
                        attachment);
                }
            }
        }

        private static IEnumerable<GraphicRead> DeriveBoundaryControls(IReadOnlyList<GraphicRead> reads)
        {
            var boundaries = DeriveClosedSegmentBoundaries(reads).ToArray();
            foreach (var boundary in boundaries)
            {
                yield return boundary;
            }

            var boundaryJoins = DeriveBoundaryJoins(reads, boundaries).ToArray();
            foreach (var join in boundaryJoins)
            {
                yield return join;
            }

            foreach (var flow in DeriveBoundaryJoinFlows(reads, boundaryJoins))
            {
                yield return flow;
            }

            foreach (var compatibility in DeriveBoundaryJoinUnitCompatibilities(reads, boundaryJoins))
            {
                yield return compatibility;
            }

            foreach (var path in DeriveOpenSegmentPaths(reads))
            {
                yield return path;
            }

            foreach (var path in DeriveSharedFramePolylines(reads))
            {
                yield return path;
            }
        }

        private static IEnumerable<ClosedSegmentBoundaryRead> DeriveClosedSegmentBoundaries(IReadOnlyList<GraphicRead> reads)
        {
            var segments = reads
                .OfType<PositiveSegmentRead>()
                .Where(segment => !IsCircularCarrier(segment.Start.Location.Carrier))
                .ToArray();
            if (segments.Length < 3)
            {
                yield break;
            }

            var endpoints = segments
                .SelectMany(segment => new[]
                {
                    new SegmentEndpointRead(segment, segment.Start, IsStart: true),
                    new SegmentEndpointRead(segment, segment.End, IsStart: false)
                })
                .ToArray();
            var endpointGroups = ReadAllSegmentEndpointGroups(endpoints, reads).ToArray();
            var groupByEndpoint = IndexEndpointGroups(endpointGroups);
            var adjacent = BuildSegmentAdjacency(segments, endpointGroups);
            var visited = ReadReferenceSet<PositiveSegmentRead>();

            foreach (var seed in segments)
            {
                if (!visited.Add(seed))
                {
                    continue;
                }

                var component = ReadSegmentComponent(seed, adjacent, visited);
                if (component.Count < 3)
                {
                    continue;
                }

                var componentEndpoints = component
                    .SelectMany(segment => new[]
                    {
                        new SegmentEndpointRead(segment, segment.Start, IsStart: true),
                        new SegmentEndpointRead(segment, segment.End, IsStart: false)
                    })
                    .ToArray();
                var boundarySites = new List<IReadOnlyList<SegmentEndpointRead>>();
                var closed = true;
                foreach (var endpoint in componentEndpoints)
                {
                    if (!groupByEndpoint.TryGetValue(endpoint, out var group))
                    {
                        closed = false;
                        break;
                    }

                    var participating = group
                        .Where(candidate => component.Any(segment => ReferenceEquals(segment, candidate.Segment)))
                        .GroupBy(endpoint => endpoint.Segment, ReadReferenceComparer<PositiveSegmentRead>())
                        .Select(endpointGroup => endpointGroup.First())
                        .ToArray();
                    if (participating.Length != 2)
                    {
                        closed = false;
                        break;
                    }

                    if (!boundarySites.Any(site => EndpointGroupSignature(site) == EndpointGroupSignature(participating)))
                    {
                        boundarySites.Add(participating);
                    }
                }

                if (closed && boundarySites.Count == component.Count)
                {
                    yield return new ClosedSegmentBoundaryRead(component[0].Source, component, boundarySites);
                }
            }
        }

        private static IEnumerable<BoundaryJoinRead> DeriveBoundaryJoins(
            IReadOnlyList<GraphicRead> reads,
            IReadOnlyList<ClosedSegmentBoundaryRead> boundaries)
        {
            var joins = reads.OfType<SegmentJoinRead>().ToArray();
            foreach (var boundary in boundaries)
            {
                foreach (var site in boundary.Sites)
                {
                    var siteSignature = EndpointGroupSignature(site);
                    foreach (var join in joins.Where(join =>
                        EndpointGroupSignature([join.First, join.Second]) == siteSignature))
                    {
                        yield return new BoundaryJoinRead(join.Source, boundary, join);
                    }
                }
            }
        }

        private static IEnumerable<GraphicRead> DeriveBoundaryJoinFlows(
            IReadOnlyList<GraphicRead> reads,
            IReadOnlyList<BoundaryJoinRead> boundaryJoins)
        {
            var flows = reads.OfType<SegmentJoinFlowRead>().ToArray();
            foreach (var boundaryJoin in boundaryJoins)
            {
                var joinSignature = EndpointGroupSignature([boundaryJoin.Join.First, boundaryJoin.Join.Second]);
                foreach (var flow in flows.Where(flow =>
                    ReferenceEquals(flow.Join, boundaryJoin.Join) ||
                    EndpointGroupSignature([flow.Join.First, flow.Join.Second]) == joinSignature))
                {
                    yield return new BoundaryJoinFlowRead(boundaryJoin.Source, boundaryJoin, flow);
                }
            }
        }

        private static IEnumerable<GraphicRead> DeriveBoundaryJoinUnitCompatibilities(
            IReadOnlyList<GraphicRead> reads,
            IReadOnlyList<BoundaryJoinRead> boundaryJoins)
        {
            var compatibilities = reads.OfType<SegmentJoinUnitCompatibilityRead>().ToArray();
            foreach (var boundaryJoin in boundaryJoins)
            {
                var joinSignature = EndpointGroupSignature([boundaryJoin.Join.First, boundaryJoin.Join.Second]);
                foreach (var compatibility in compatibilities.Where(compatibility =>
                    ReferenceEquals(compatibility.Flow.Join, boundaryJoin.Join) ||
                    EndpointGroupSignature([compatibility.Flow.Join.First, compatibility.Flow.Join.Second]) == joinSignature))
                {
                    yield return new BoundaryJoinUnitCompatibilityRead(
                        boundaryJoin.Source,
                        boundaryJoin,
                        compatibility);
                }
            }
        }

        private static IEnumerable<GraphicRead> DeriveOpenSegmentPaths(IReadOnlyList<GraphicRead> reads)
        {
            var segments = reads
                .OfType<PathSegmentRead>()
                .Where(segment => !IsCircularCarrier(NormalizeCarrier(segment.Start.Location.Carrier)))
                .ToArray();
            if (segments.Length < 3)
            {
                yield break;
            }

            var endpoints = ReadPathSegmentEndpoints(segments);
            var endpointGroups = ReadAllPathEndpointGroups(endpoints, reads);
            var adjacent = BuildPathSegmentAdjacency(segments, endpointGroups);
            var visited = ReadReferenceSet<PathSegmentRead>();

            foreach (var seed in segments)
            {
                if (!visited.Add(seed))
                {
                    continue;
                }

                var component = ReadSegmentComponent(seed, adjacent, visited);
                if (component.Count >= 3 &&
                    TryReadOpenSegmentPath(component, endpointGroups, reads, out var path))
                {
                    yield return path;
                }
            }
        }

        private static IEnumerable<GraphicRead> DeriveSharedFramePolylines(IReadOnlyList<GraphicRead> reads)
        {
            var segments = reads.OfType<SharedFrameSegmentRead>().ToArray();
            if (segments.Length < 2)
            {
                yield break;
            }

            var endpointGroups = ReadSharedFrameEndpointGroups(segments);
            var adjacent = BuildSharedFrameSegmentAdjacency(segments, endpointGroups);
            var visited = ReadReferenceSet<SharedFrameSegmentRead>();

            foreach (var seed in segments)
            {
                if (!visited.Add(seed))
                {
                    continue;
                }

                var component = ReadSharedFrameSegmentComponent(seed, adjacent, visited);
                if (component.Count >= 2 &&
                    TryReadSharedFramePolyline(component, endpointGroups, out var path))
                {
                    yield return path;
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

        private static bool TryReadLinearBasisPoint(PointRead point, out LinearBasisPointRead basisPoint)
        {
            var location = Normalize(point.Location);
            var carrier = NormalizeCarrier(location.Carrier);
            if (!carrier.TryReadLinearOrigin(out var sourceOrigin) ||
                location.Ticks == LinearCarrierOriginTicks)
            {
                basisPoint = default!;
                return false;
            }

            basisPoint = new LinearBasisPointRead(
                point.Source,
                carrier,
                LinearCarrierOriginTicks,
                sourceOrigin,
                location,
                point);
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

        private static bool RecognizeSharedFramePoint(Relation relation, out SharedFramePointRead point)
        {
            if (relation.Right is Carrier holder &&
                relation.TryReadOrderedElementsOn(holder, out var elements) &&
                elements.Length >= 2 &&
                elements.All(element => element is CarrierLocation))
            {
                point = new SharedFramePointRead(
                    relation,
                    holder,
                    [.. elements.Cast<CarrierLocation>()]);
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

        private static SegmentEndpointRead[] ReadSegmentEndpointsAt(
            SharedSiteRead site,
            IReadOnlyList<GraphicRead> reads) =>
            [.. reads
                .OfType<SegmentConnectionRead>()
                .SelectMany(connection => new[] { connection.Left, connection.Right })
                .Where(endpoint => site.Points.Any(point =>
                    point.Location.SameTickRefAs(endpoint.Point.Location) ||
                    SameNormalizedLocation(point.Location, endpoint.Point.Location)))
                .GroupBy(endpoint => endpoint.Segment.Source)
                .Select(group => group.First())
                .OrderBy(endpoint => endpoint.Segment.Source is Relation relation ? relation.Id : 0UL)
                .ThenBy(endpoint => endpoint.IsStart)];

        private static SegmentEndpointFlow ReadSegmentEndpointFlow(SegmentEndpointRead endpoint)
        {
            var site = Normalize(endpoint.Point.Location);
            var otherPoint = endpoint.IsStart
                ? endpoint.Segment.End
                : endpoint.Segment.Start;
            var other = Normalize(otherPoint.Location);
            return new SegmentEndpointFlow(
                endpoint,
                endpoint.Point.Location.Carrier,
                NormalizeCarrier(site.Carrier),
                site.Ticks,
                other.Ticks);
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

        private static bool HaveSameFrameBasis(SharedFramePointRead left, SharedFramePointRead right) =>
            left.Coordinates.Count == right.Coordinates.Count &&
            left.Coordinates.Zip(right.Coordinates).All(pair =>
                NormalizeCarrier(pair.First.Carrier).SameIdentityAs(NormalizeCarrier(pair.Second.Carrier)));

        private static bool SameFramePoint(SharedFramePointRead left, SharedFramePointRead right) =>
            HaveSameFrameBasis(left, right) &&
            left.Coordinates.Zip(right.Coordinates).All(pair =>
                pair.First.SameTickRefAs(pair.Second) ||
                SameNormalizedLocation(pair.First, pair.Second));

        private static IReadOnlyList<IReadOnlyList<PointRead>> ReadAllPointGroups(
            IReadOnlyList<PointRead> points,
            IReadOnlyList<GraphicRead> reads)
        {
            var parent = Enumerable.Range(0, points.Count).ToArray();

            int Find(int index)
            {
                while (parent[index] != index)
                {
                    parent[index] = parent[parent[index]];
                    index = parent[index];
                }

                return index;
            }

            void Union(int left, int right)
            {
                var leftRoot = Find(left);
                var rightRoot = Find(right);
                if (leftRoot != rightRoot)
                {
                    parent[rightRoot] = leftRoot;
                }
            }

            void UnionGroup(IEnumerable<int> indexes)
            {
                var groupIndexes = indexes.ToArray();
                if (groupIndexes.Length < 2)
                {
                    return;
                }

                for (var index = 1; index < groupIndexes.Length; index++)
                {
                    Union(groupIndexes[0], groupIndexes[index]);
                }
            }

            foreach (var group in points
                .Select((point, index) => (point, index))
                .GroupBy(entry => LocationGroupKey.Of(entry.point.Location)))
            {
                UnionGroup(group.Select(entry => entry.index));
            }

            foreach (var group in points
                .Select((point, index) => (point, index))
                .Where(entry => entry.point.Location.Tick is not null)
                .GroupBy(entry => entry.point.Location.Tick!.Id))
            {
                UnionGroup(group.Select(entry => entry.index));
            }

            foreach (var attachment in reads.OfType<PointAttachmentRead>())
            {
                var leftIndexes = ReadPointIndexesAt(points, attachment.Left.Location);
                var rightIndexes = ReadPointIndexesAt(points, attachment.Right.Location);
                if (leftIndexes.Length == 0 || rightIndexes.Length == 0)
                {
                    continue;
                }

                foreach (var leftIndex in leftIndexes)
                foreach (var rightIndex in rightIndexes)
                {
                    Union(leftIndex, rightIndex);
                }
            }

            return [.. points
                .Select((point, index) => (point, root: Find(index)))
                .GroupBy(entry => entry.root)
                .Select(group => (IReadOnlyList<PointRead>)[.. group.Select(entry => entry.point)])
                .Where(group => group.Count > 1)];
        }

        private static SegmentEndpointRead[] ReadSegmentEndpoints(IReadOnlyList<PositiveSegmentRead> segments) =>
            [.. segments.SelectMany(segment => new[]
            {
                new SegmentEndpointRead(segment, segment.Start, IsStart: true),
                new SegmentEndpointRead(segment, segment.End, IsStart: false)
            })];

        private static IReadOnlyList<IReadOnlyList<SegmentEndpointRead>> ReadAllSegmentEndpointGroups(
            IReadOnlyList<SegmentEndpointRead> endpoints,
            IReadOnlyList<GraphicRead> reads)
        {
            var parent = Enumerable.Range(0, endpoints.Count).ToArray();

            int Find(int index)
            {
                while (parent[index] != index)
                {
                    parent[index] = parent[parent[index]];
                    index = parent[index];
                }

                return index;
            }

            void Union(int left, int right)
            {
                var leftRoot = Find(left);
                var rightRoot = Find(right);
                if (leftRoot != rightRoot)
                {
                    parent[rightRoot] = leftRoot;
                }
            }

            void UnionGroup(IEnumerable<int> indexes)
            {
                var groupIndexes = indexes.ToArray();
                if (groupIndexes.Length < 2)
                {
                    return;
                }

                for (var index = 1; index < groupIndexes.Length; index++)
                {
                    Union(groupIndexes[0], groupIndexes[index]);
                }
            }

            foreach (var group in endpoints
                .Select((endpoint, index) => (endpoint, index))
                .GroupBy(entry => LocationGroupKey.Of(entry.endpoint.Point.Location)))
            {
                UnionGroup(group.Select(entry => entry.index));
            }

            foreach (var group in endpoints
                .Select((endpoint, index) => (endpoint, index))
                .Where(entry => entry.endpoint.Point.Location.Tick is not null)
                .GroupBy(entry => entry.endpoint.Point.Location.Tick!.Id))
            {
                UnionGroup(group.Select(entry => entry.index));
            }

            foreach (var attachment in reads.OfType<PointAttachmentRead>())
            {
                var leftIndexes = ReadEndpointIndexesAt(endpoints, attachment.Left.Location);
                var rightIndexes = ReadEndpointIndexesAt(endpoints, attachment.Right.Location);
                if (leftIndexes.Length == 0 || rightIndexes.Length == 0)
                {
                    continue;
                }

                foreach (var leftIndex in leftIndexes)
                foreach (var rightIndex in rightIndexes)
                {
                    Union(leftIndex, rightIndex);
                }
            }

            return [.. endpoints
                .Select((endpoint, index) => (endpoint, root: Find(index)))
                .GroupBy(entry => entry.root)
                .Select(group => (IReadOnlyList<SegmentEndpointRead>)[.. group.Select(entry => entry.endpoint)])];
        }

        private static PathSegmentEndpointRead[] ReadPathSegmentEndpoints(IReadOnlyList<PathSegmentRead> segments) =>
            [.. segments.SelectMany(segment => new[]
            {
                new PathSegmentEndpointRead(segment, segment.Start, IsStart: true),
                new PathSegmentEndpointRead(segment, segment.End, IsStart: false)
            })];

        private static IReadOnlyList<IReadOnlyList<PathSegmentEndpointRead>> ReadAllPathEndpointGroups(
            IReadOnlyList<PathSegmentEndpointRead> endpoints,
            IReadOnlyList<GraphicRead> reads)
        {
            var parent = Enumerable.Range(0, endpoints.Count).ToArray();

            int Find(int index)
            {
                while (parent[index] != index)
                {
                    parent[index] = parent[parent[index]];
                    index = parent[index];
                }

                return index;
            }

            void Union(int left, int right)
            {
                var leftRoot = Find(left);
                var rightRoot = Find(right);
                if (leftRoot != rightRoot)
                {
                    parent[rightRoot] = leftRoot;
                }
            }

            void UnionGroup(IEnumerable<int> indexes)
            {
                var groupIndexes = indexes.ToArray();
                if (groupIndexes.Length < 2)
                {
                    return;
                }

                for (var index = 1; index < groupIndexes.Length; index++)
                {
                    Union(groupIndexes[0], groupIndexes[index]);
                }
            }

            foreach (var group in endpoints
                .Select((endpoint, index) => (endpoint, index))
                .GroupBy(entry => LocationGroupKey.Of(entry.endpoint.Point.Location)))
            {
                UnionGroup(group.Select(entry => entry.index));
            }

            foreach (var group in endpoints
                .Select((endpoint, index) => (endpoint, index))
                .Where(entry => entry.endpoint.Point.Location.Tick is not null)
                .GroupBy(entry => entry.endpoint.Point.Location.Tick!.Id))
            {
                UnionGroup(group.Select(entry => entry.index));
            }

            foreach (var attachment in reads.OfType<PointAttachmentRead>())
            {
                var leftIndexes = ReadEndpointIndexesAt(endpoints, attachment.Left.Location);
                var rightIndexes = ReadEndpointIndexesAt(endpoints, attachment.Right.Location);
                if (leftIndexes.Length == 0 || rightIndexes.Length == 0)
                {
                    continue;
                }

                foreach (var leftIndex in leftIndexes)
                foreach (var rightIndex in rightIndexes)
                {
                    Union(leftIndex, rightIndex);
                }
            }

            return [.. endpoints
                .Select((endpoint, index) => (endpoint, root: Find(index)))
                .GroupBy(entry => entry.root)
                .Select(group => (IReadOnlyList<PathSegmentEndpointRead>)[.. group.Select(entry => entry.endpoint)])];
        }

        private static int[] ReadEndpointIndexesAt(
            IReadOnlyList<SegmentEndpointRead> endpoints,
            CarrierLocation location) =>
            [.. endpoints
                .Select((endpoint, index) => (endpoint, index))
                .Where(entry =>
                    entry.endpoint.Point.Location.SameTickRefAs(location) ||
                    SameNormalizedLocation(entry.endpoint.Point.Location, location))
                .Select(entry => entry.index)];

        private static int[] ReadPointIndexesAt(
            IReadOnlyList<PointRead> points,
            CarrierLocation location) =>
            [.. points
                .Select((point, index) => (point, index))
                .Where(entry =>
                    entry.point.Location.SameTickRefAs(location) ||
                    SameNormalizedLocation(entry.point.Location, location))
                .Select(entry => entry.index)];

        private static int[] ReadEndpointIndexesAt(
            IReadOnlyList<PathSegmentEndpointRead> endpoints,
            CarrierLocation location) =>
            [.. endpoints
                .Select((endpoint, index) => (endpoint, index))
                .Where(entry =>
                    entry.endpoint.Point.Location.SameTickRefAs(location) ||
                    SameNormalizedLocation(entry.endpoint.Point.Location, location))
                .Select(entry => entry.index)];

        private static string EndpointGroupSignature(IEnumerable<SegmentEndpointRead> endpoints) =>
            string.Join(",", endpoints
                .Select(endpoint =>
                    $"{(endpoint.Segment.Source is Relation relation ? relation.Id : 0UL)}:{endpoint.IsStart}")
                .Order());

        private static string PathEndpointGroupSignature(IEnumerable<PathSegmentEndpointRead> endpoints) =>
            string.Join(",", endpoints
                .Select(endpoint =>
                    $"{(endpoint.Segment.Source is Relation relation ? relation.Id : 0UL)}:{endpoint.IsStart}")
                .Order());

        private static Dictionary<SegmentEndpointRead, IReadOnlyList<SegmentEndpointRead>> IndexEndpointGroups(
            IEnumerable<IReadOnlyList<SegmentEndpointRead>> endpointGroups)
        {
            var index = new Dictionary<SegmentEndpointRead, IReadOnlyList<SegmentEndpointRead>>();
            foreach (var group in endpointGroups)
            {
                foreach (var endpoint in group)
                {
                    index.TryAdd(endpoint, group);
                }
            }

            return index;
        }

        private static Dictionary<PathSegmentEndpointRead, IReadOnlyList<PathSegmentEndpointRead>> IndexPathEndpointGroups(
            IEnumerable<IReadOnlyList<PathSegmentEndpointRead>> endpointGroups)
        {
            var index = new Dictionary<PathSegmentEndpointRead, IReadOnlyList<PathSegmentEndpointRead>>();
            foreach (var group in endpointGroups)
            {
                foreach (var endpoint in group)
                {
                    index.TryAdd(endpoint, group);
                }
            }

            return index;
        }

        private static Dictionary<PositiveSegmentRead, List<PositiveSegmentRead>> BuildSegmentAdjacency(
            IReadOnlyList<PositiveSegmentRead> segments,
            IReadOnlyList<IReadOnlyList<SegmentEndpointRead>> endpointGroups)
        {
            var adjacent = ReadReferenceDictionary<PositiveSegmentRead, List<PositiveSegmentRead>>();
            foreach (var segment in segments)
            {
                adjacent.TryAdd(segment, []);
            }

            foreach (var group in endpointGroups)
            {
                var groupSegments = group
                    .Select(endpoint => endpoint.Segment)
                    .Distinct(ReadReferenceComparer<PositiveSegmentRead>())
                    .ToArray();
                for (var leftIndex = 0; leftIndex < groupSegments.Length; leftIndex++)
                for (var rightIndex = leftIndex + 1; rightIndex < groupSegments.Length; rightIndex++)
                {
                    adjacent[groupSegments[leftIndex]].Add(groupSegments[rightIndex]);
                    adjacent[groupSegments[rightIndex]].Add(groupSegments[leftIndex]);
                }
            }

            return adjacent;
        }

        private static Dictionary<PathSegmentRead, List<PathSegmentRead>> BuildPathSegmentAdjacency(
            IReadOnlyList<PathSegmentRead> segments,
            IReadOnlyList<IReadOnlyList<PathSegmentEndpointRead>> endpointGroups)
        {
            var adjacent = ReadReferenceDictionary<PathSegmentRead, List<PathSegmentRead>>();
            foreach (var segment in segments)
            {
                adjacent.TryAdd(segment, []);
            }

            foreach (var group in endpointGroups)
            {
                var groupSegments = group
                    .Select(endpoint => endpoint.Segment)
                    .Distinct(ReadReferenceComparer<PathSegmentRead>())
                    .ToArray();
                for (var leftIndex = 0; leftIndex < groupSegments.Length; leftIndex++)
                for (var rightIndex = leftIndex + 1; rightIndex < groupSegments.Length; rightIndex++)
                {
                    adjacent[groupSegments[leftIndex]].Add(groupSegments[rightIndex]);
                    adjacent[groupSegments[rightIndex]].Add(groupSegments[leftIndex]);
                }
            }

            return adjacent;
        }

        private static IReadOnlyList<PositiveSegmentRead> ReadSegmentComponent(
            PositiveSegmentRead seed,
            IReadOnlyDictionary<PositiveSegmentRead, List<PositiveSegmentRead>> adjacent,
            HashSet<PositiveSegmentRead> visited)
        {
            var component = new List<PositiveSegmentRead> { seed };
            var pending = new Queue<PositiveSegmentRead>();
            pending.Enqueue(seed);
            while (pending.Count != 0)
            {
                var current = pending.Dequeue();
                foreach (var next in adjacent[current])
                {
                    if (visited.Add(next))
                    {
                        component.Add(next);
                        pending.Enqueue(next);
                    }
                }
            }

            return component;
        }

        private static IReadOnlyList<PathSegmentRead> ReadSegmentComponent(
            PathSegmentRead seed,
            IReadOnlyDictionary<PathSegmentRead, List<PathSegmentRead>> adjacent,
            HashSet<PathSegmentRead> visited)
        {
            var component = new List<PathSegmentRead> { seed };
            var pending = new Queue<PathSegmentRead>();
            pending.Enqueue(seed);
            while (pending.Count != 0)
            {
                var current = pending.Dequeue();
                foreach (var next in adjacent[current])
                {
                    if (visited.Add(next))
                    {
                        component.Add(next);
                        pending.Enqueue(next);
                    }
                }
            }

            return component;
        }

        private static bool TryReadOpenSegmentPath(
            IReadOnlyList<PathSegmentRead> component,
            IReadOnlyList<IReadOnlyList<PathSegmentEndpointRead>> endpointGroups,
            IReadOnlyList<GraphicRead> reads,
            out OpenSegmentPathRead path)
        {
            var componentSet = ReadReferenceSet<PathSegmentRead>(component);
            var groups = endpointGroups
                .Select(group => group.Where(endpoint => componentSet.Contains(endpoint.Segment)).ToArray())
                .Where(group => group.Length != 0)
                .GroupBy(PathEndpointGroupSignature)
                .Select(group => group.First())
                .ToArray();

            if (groups.Any(group => group.Length > 2))
            {
                path = default!;
                return false;
            }

            var terminals = groups.Where(group => group.Length == 1).ToArray();
            var joinGroups = groups.Where(group => group.Length == 2).ToArray();
            if (terminals.Length != 2 || joinGroups.Length != component.Count - 1)
            {
                path = default!;
                return false;
            }

            var groupByEndpoint = IndexPathEndpointGroups(groups);
            if (!TryOrderSegmentPath(terminals[0][0], component.Count, groupByEndpoint, out var orderedSegments))
            {
                path = default!;
                return false;
            }

            var orderedSet = ReadReferenceSet<PathSegmentRead>(orderedSegments);
            if (orderedSet.Count != component.Count || !component.All(orderedSet.Contains))
            {
                path = default!;
                return false;
            }

            var joinsBySignature = reads
                .OfType<SegmentJoinRead>()
                .GroupBy(join => EndpointGroupSignature([join.First, join.Second]))
                .ToDictionary(group => group.Key, group => group.First());
            var flowsBySignature = reads
                .OfType<SegmentJoinFlowRead>()
                .GroupBy(flow => EndpointGroupSignature([flow.Join.First, flow.Join.Second]))
                .ToDictionary(group => group.Key, group => group.First());
            var compatibilitiesBySignature = reads
                .OfType<SegmentJoinUnitCompatibilityRead>()
                .GroupBy(compatibility => EndpointGroupSignature(
                    [compatibility.Flow.Join.First, compatibility.Flow.Join.Second]))
                .ToDictionary(group => group.Key, group => group.First());

            var joins = new List<SegmentJoinRead>();
            var flows = new List<SegmentJoinFlowRead>();
            var compatibilities = new List<SegmentJoinUnitCompatibilityRead>();
            foreach (var group in joinGroups)
            {
                var signature = PathEndpointGroupSignature(group);
                if (joinsBySignature.TryGetValue(signature, out var join))
                {
                    joins.Add(join);
                }

                if (flowsBySignature.TryGetValue(signature, out var flow))
                {
                    flows.Add(flow);
                }

                if (compatibilitiesBySignature.TryGetValue(signature, out var compatibility))
                {
                    compatibilities.Add(compatibility);
                }
            }

            path = new OpenSegmentPathRead(
                orderedSegments[0].Source,
                orderedSegments,
                joins,
                flows,
                compatibilities);
            return true;
        }

        private static bool TryOrderSegmentPath(
            PathSegmentEndpointRead startEndpoint,
            int expectedCount,
            IReadOnlyDictionary<PathSegmentEndpointRead, IReadOnlyList<PathSegmentEndpointRead>> groupByEndpoint,
            out IReadOnlyList<PathSegmentRead> orderedSegments)
        {
            var ordered = new List<PathSegmentRead>();
            var visited = ReadReferenceSet<PathSegmentRead>();
            var incoming = startEndpoint;

            while (visited.Add(incoming.Segment))
            {
                ordered.Add(incoming.Segment);
                var outgoing = OtherEndpoint(incoming);
                if (!groupByEndpoint.TryGetValue(outgoing, out var group) ||
                    group.Count == 1)
                {
                    break;
                }

                var next = group.FirstOrDefault(endpoint => !ReferenceEquals(endpoint.Segment, incoming.Segment));
                if (next is null)
                {
                    break;
                }

                incoming = next;
            }

            orderedSegments = ordered;
            return ordered.Count == expectedCount;
        }

        private static PathSegmentEndpointRead OtherEndpoint(PathSegmentEndpointRead endpoint) =>
            endpoint.IsStart
                ? new PathSegmentEndpointRead(endpoint.Segment, endpoint.Segment.End, IsStart: false)
                : new PathSegmentEndpointRead(endpoint.Segment, endpoint.Segment.Start, IsStart: true);

        private static IReadOnlyList<IReadOnlyList<SharedFrameSegmentEndpointRead>> ReadSharedFrameEndpointGroups(
            IReadOnlyList<SharedFrameSegmentRead> segments)
        {
            var endpoints = segments
                .SelectMany(segment => new[]
                {
                    new SharedFrameSegmentEndpointRead(segment, segment.Start, IsStart: true),
                    new SharedFrameSegmentEndpointRead(segment, segment.End, IsStart: false)
                })
                .ToArray();
            return [.. endpoints
                .GroupBy(endpoint => FramePointSignature(endpoint.Point))
                .Select(group => (IReadOnlyList<SharedFrameSegmentEndpointRead>)[.. group])];
        }

        private static Dictionary<SharedFrameSegmentRead, List<SharedFrameSegmentRead>> BuildSharedFrameSegmentAdjacency(
            IReadOnlyList<SharedFrameSegmentRead> segments,
            IEnumerable<IReadOnlyList<SharedFrameSegmentEndpointRead>> endpointGroups)
        {
            var adjacent = ReadReferenceDictionary<SharedFrameSegmentRead, List<SharedFrameSegmentRead>>();
            foreach (var segment in segments)
            {
                adjacent.TryAdd(segment, []);
            }

            foreach (var group in endpointGroups.Where(group => group.Count > 1))
            {
                var groupSegments = group
                    .Select(endpoint => endpoint.Segment)
                    .Distinct(ReadReferenceComparer<SharedFrameSegmentRead>())
                    .ToArray();
                for (var leftIndex = 0; leftIndex < groupSegments.Length; leftIndex++)
                for (var rightIndex = leftIndex + 1; rightIndex < groupSegments.Length; rightIndex++)
                {
                    adjacent[groupSegments[leftIndex]].Add(groupSegments[rightIndex]);
                    adjacent[groupSegments[rightIndex]].Add(groupSegments[leftIndex]);
                }
            }

            return adjacent;
        }

        private static IReadOnlyList<SharedFrameSegmentRead> ReadSharedFrameSegmentComponent(
            SharedFrameSegmentRead seed,
            IReadOnlyDictionary<SharedFrameSegmentRead, List<SharedFrameSegmentRead>> adjacent,
            HashSet<SharedFrameSegmentRead> visited)
        {
            var component = new List<SharedFrameSegmentRead> { seed };
            var pending = new Queue<SharedFrameSegmentRead>();
            pending.Enqueue(seed);
            while (pending.Count != 0)
            {
                var current = pending.Dequeue();
                foreach (var next in adjacent[current])
                {
                    if (visited.Add(next))
                    {
                        component.Add(next);
                        pending.Enqueue(next);
                    }
                }
            }

            return component;
        }

        private static bool TryReadSharedFramePolyline(
            IReadOnlyList<SharedFrameSegmentRead> component,
            IReadOnlyList<IReadOnlyList<SharedFrameSegmentEndpointRead>> endpointGroups,
            out SharedFramePolylineRead path)
        {
            var componentSet = ReadReferenceSet<SharedFrameSegmentRead>(component);
            var groups = endpointGroups
                .Select(group => group.Where(endpoint => componentSet.Contains(endpoint.Segment)).ToArray())
                .Where(group => group.Length != 0)
                .GroupBy(FrameEndpointGroupSignature)
                .Select(group => group.First())
                .ToArray();

            if (groups.Any(group => group.Length > 2))
            {
                path = default!;
                return false;
            }

            var terminals = groups.Where(group => group.Length == 1).ToArray();
            var joins = groups.Where(group => group.Length == 2).ToArray();
            if (terminals.Length != 2 || joins.Length != component.Count - 1)
            {
                path = default!;
                return false;
            }

            var groupByEndpoint = new Dictionary<SharedFrameSegmentEndpointRead, IReadOnlyList<SharedFrameSegmentEndpointRead>>();
            foreach (var group in groups)
            {
                foreach (var endpoint in group)
                {
                    groupByEndpoint[endpoint] = group;
                }
            }

            if (!TryOrderSharedFramePath(terminals[0][0], component.Count, groupByEndpoint, out var ordered))
            {
                path = default!;
                return false;
            }

            var orderedSet = ReadReferenceSet<SharedFrameSegmentRead>(ordered);
            if (orderedSet.Count != component.Count || !component.All(orderedSet.Contains))
            {
                path = default!;
                return false;
            }

            path = new SharedFramePolylineRead(ordered[0].Source, ordered, [.. joins]);
            return true;
        }

        private static bool TryOrderSharedFramePath(
            SharedFrameSegmentEndpointRead startEndpoint,
            int expectedCount,
            IReadOnlyDictionary<SharedFrameSegmentEndpointRead, IReadOnlyList<SharedFrameSegmentEndpointRead>> groupByEndpoint,
            out IReadOnlyList<SharedFrameSegmentRead> orderedSegments)
        {
            var ordered = new List<SharedFrameSegmentRead>();
            var visited = ReadReferenceSet<SharedFrameSegmentRead>();
            var incoming = startEndpoint;

            while (visited.Add(incoming.Segment))
            {
                ordered.Add(incoming.Segment);
                var outgoing = incoming.IsStart
                    ? new SharedFrameSegmentEndpointRead(incoming.Segment, incoming.Segment.End, IsStart: false)
                    : new SharedFrameSegmentEndpointRead(incoming.Segment, incoming.Segment.Start, IsStart: true);
                if (!groupByEndpoint.TryGetValue(outgoing, out var group) ||
                    group.Count == 1)
                {
                    break;
                }

                var next = group.FirstOrDefault(endpoint => !ReferenceEquals(endpoint.Segment, incoming.Segment));
                if (next is null)
                {
                    break;
                }

                incoming = next;
            }

            orderedSegments = ordered;
            return ordered.Count == expectedCount;
        }

        private static string FramePointSignature(SharedFramePointRead point) =>
            string.Join("|", point.Coordinates.Select(location =>
            {
                var normalized = Normalize(location);
                return $"{NormalizeCarrier(normalized.Carrier).Id}:{normalized.Tick?.Id ?? 0}:{normalized.Ticks}";
            }));

        private static string FrameEndpointGroupSignature(IEnumerable<SharedFrameSegmentEndpointRead> endpoints) =>
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

        private static Dictionary<TKey, TValue> ReadReferenceDictionary<TKey, TValue>()
            where TKey : class =>
            new(ReadReferenceComparer<TKey>());

        private static HashSet<T> ReadReferenceSet<T>()
            where T : class =>
            new(ReadReferenceComparer<T>());

        private static HashSet<T> ReadReferenceSet<T>(IEnumerable<T> values)
            where T : class =>
            new(values, ReadReferenceComparer<T>());

        private static IEqualityComparer<T> ReadReferenceComparer<T>()
            where T : class =>
            ReferenceReadComparer<T>.Instance;

        private const long LinearCarrierOriginTicks = 0;

        private readonly record struct LocationGroupKey(ulong CarrierId, long Ticks)
        {
            public static LocationGroupKey Of(CarrierLocation location)
            {
                var normalized = Normalize(location);
                return new LocationGroupKey(NormalizeCarrier(normalized.Carrier).Id, normalized.Ticks);
            }
        }

        private sealed class ReferenceReadComparer<T> : IEqualityComparer<T>
            where T : class
        {
            public static ReferenceReadComparer<T> Instance { get; } = new();

            public bool Equals(T? x, T? y) => ReferenceEquals(x, y);

            public int GetHashCode(T obj) =>
                System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
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

public sealed record InverseSegmentRead(IElement Source, PointRead Start, PointRead End) : GraphicRead(Source);

public sealed record SharedFramePointRead(
    IElement Source,
    Carrier Holder,
    IReadOnlyList<CarrierLocation> Coordinates) : GraphicRead(Source)
{
    public override IEnumerable<IElement> Sources => [Source, Holder, .. Coordinates.Cast<IElement>()];
}

public sealed record SharedFrameSegmentRead(
    IElement Source,
    SharedFramePointRead Start,
    SharedFramePointRead End) : GraphicRead(Source)
{
    public override IEnumerable<IElement> Sources => [Source, Start.Source, End.Source];
}

public sealed record SharedFramePointAttachmentRead(
    IElement Source,
    SharedFramePointRead FramePoint,
    PointRead Point) : GraphicRead(Source)
{
    public override IEnumerable<IElement> Sources => [Source, FramePoint.Source, Point.Source];
}

public sealed record ZeroSpanRead(IElement Source, PointRead Left, PointRead Right) : GraphicRead(Source);

public sealed record DirectedSegmentRead(
    IElement Source,
    PointRead InverseStart,
    PointRead PositiveEnd,
    bool IsSourceOrderReversed) : GraphicRead(Source);

public sealed record PathSegmentRead(
    IElement Source,
    GraphicRead Segment,
    PointRead Start,
    PointRead End) : GraphicRead(Source)
{
    public bool IsPositive => Segment is PositiveSegmentRead;

    public bool IsInverse => Segment is InverseSegmentRead;

    public bool IsDirected => Segment is DirectedSegmentRead;

    public override IEnumerable<IElement> Sources =>
        Segment.Sources.Concat([Start.Source, End.Source]);
}

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

public sealed record CarrierUnitRead(
    IElement Source,
    Carrier Carrier,
    UnitShape UnitShape,
    IElement Surface,
    IReadOnlyList<IElement> Descriptors,
    bool HasOpposedFace,
    UnitShape? OpposedSource,
    bool HasOrthogonalSource,
    IElement? OrthogonalSource,
    bool HasResolution,
    UnitShape? ResolutionSource,
    long ResolutionSourceTicksPerTick) : GraphicRead(Source)
{
    public bool HasDescriptors => Descriptors.Count != 0;

    public int DescriptorCount => Descriptors.Count;

    public override IEnumerable<IElement> Sources => [Source, Carrier];
}

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

public sealed record LinearBasisPointRead(
    IElement Source,
    Carrier Carrier,
    long OriginTicks,
    CarrierLocation SourceOrigin,
    CarrierLocation Location,
    PointRead Point) : GraphicRead(Source)
{
    public long DeltaTicks => Location.Ticks - OriginTicks;

    public override IEnumerable<IElement> Sources => [Source, Point.Source];
}

public sealed record LinearBasisAlignmentRead(
    IElement Source,
    LinearBasisPointRead Left,
    LinearBasisPointRead Right,
    PointAttachmentRead Attachment) : GraphicRead(Source)
{
    public bool HasSameFlow => Math.Sign(Left.DeltaTicks) == Math.Sign(Right.DeltaTicks);

    public bool HasOpposedFlow => Math.Sign(Left.DeltaTicks) == -Math.Sign(Right.DeltaTicks);

    public override IEnumerable<IElement> Sources =>
        Attachment.Sources.Concat(Left.Sources).Concat(Right.Sources);
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

public sealed record PathSegmentEndpointRead(
    PathSegmentRead Segment,
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

public sealed record SegmentJoinRead(
    IElement Source,
    SharedSiteRead Site,
    SegmentEndpointRead First,
    SegmentEndpointRead Second) : GraphicRead(Source)
{
    public override IEnumerable<IElement> Sources =>
        Site.Sources.Concat(new[]
        {
            First.Segment.Source,
            First.Point.Source,
            Second.Segment.Source,
            Second.Point.Source
        });
}

public sealed record SegmentEndpointFlow(
    SegmentEndpointRead Endpoint,
    Carrier Carrier,
    Carrier NormalizedCarrier,
    long SiteTicks,
    long OtherTicks)
{
    public long AwayTicks => OtherTicks - SiteTicks;

    public int AwaySign => Math.Sign(AwayTicks);

    public bool HasResolvedFlow => AwayTicks != 0;
}

public sealed record SegmentJoinFlowRead(
    IElement Source,
    SegmentJoinRead Join,
    SegmentEndpointFlow First,
    SegmentEndpointFlow Second) : GraphicRead(Source)
{
    public bool SharesCarrier => First.NormalizedCarrier.SameIdentityAs(Second.NormalizedCarrier);

    public bool UsesSameFace => First.Carrier.SameIdentityAs(Second.Carrier);

    public bool UsesOpposedFaces => SharesCarrier && !UsesSameFace;

    public bool IsCarrierTransition => !SharesCarrier;

    public bool HasResolvedFlow => First.HasResolvedFlow && Second.HasResolvedFlow;

    public bool HasOpposedLocalFlow =>
        SharesCarrier &&
        HasResolvedFlow &&
        First.AwaySign == -Second.AwaySign;

    public bool HasSameLocalFlow =>
        SharesCarrier &&
        HasResolvedFlow &&
        First.AwaySign == Second.AwaySign;

    public override IEnumerable<IElement> Sources => Join.Sources;
}

public sealed record SegmentJoinUnitCompatibilityRead(
    IElement Source,
    SegmentJoinFlowRead Flow,
    CarrierUnitRead First,
    CarrierUnitRead Second) : GraphicRead(Source)
{
    public bool SharesUnitShapeIdentity => ReferenceEquals(First.UnitShape, Second.UnitShape);

    public bool AlgebraicallyCompatible =>
        First.UnitShape.AlgebraicallyCompatibleWith(Second.UnitShape);

    public bool CompatibleWithDescriptors =>
        First.UnitShape.CompatibleWith(Second.UnitShape);

    public bool HasSharedTemplateProvenance =>
        CompatibleWithDescriptors && !SharesUnitShapeIdentity;

    public bool HasDifferentDescriptors =>
        AlgebraicallyCompatible && !CompatibleWithDescriptors;

    public bool HasDifferentAlgebra => !AlgebraicallyCompatible;

    public override IEnumerable<IElement> Sources =>
        Flow.Sources.Concat(First.Sources).Concat(Second.Sources);
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

public sealed record ClosedSegmentBoundaryRead(
    IElement Source,
    IReadOnlyList<PositiveSegmentRead> Segments,
    IReadOnlyList<IReadOnlyList<SegmentEndpointRead>> Sites) : GraphicRead(Source)
{
    public override IEnumerable<IElement> Sources =>
        Segments.Select(segment => segment.Source)
            .Concat(Sites.SelectMany(site => site.Select(endpoint => endpoint.Point.Source)));
}

public sealed record BoundaryJoinRead(
    IElement Source,
    ClosedSegmentBoundaryRead Boundary,
    SegmentJoinRead Join) : GraphicRead(Source)
{
    public override IEnumerable<IElement> Sources => Boundary.Sources.Concat(Join.Sources);
}

public sealed record BoundaryJoinFlowRead(
    IElement Source,
    BoundaryJoinRead BoundaryJoin,
    SegmentJoinFlowRead Flow) : GraphicRead(Source)
{
    public bool IsCarrierTransition => Flow.IsCarrierTransition;

    public bool HasOpposedLocalFlow => Flow.HasOpposedLocalFlow;

    public bool HasSameLocalFlow => Flow.HasSameLocalFlow;

    public override IEnumerable<IElement> Sources => BoundaryJoin.Sources.Concat(Flow.Sources);
}

public sealed record BoundaryJoinUnitCompatibilityRead(
    IElement Source,
    BoundaryJoinRead BoundaryJoin,
    SegmentJoinUnitCompatibilityRead Compatibility) : GraphicRead(Source)
{
    public bool AlgebraicallyCompatible => Compatibility.AlgebraicallyCompatible;

    public bool CompatibleWithDescriptors => Compatibility.CompatibleWithDescriptors;

    public bool HasSharedTemplateProvenance => Compatibility.HasSharedTemplateProvenance;

    public bool HasDifferentDescriptors => Compatibility.HasDifferentDescriptors;

    public bool HasDifferentAlgebra => Compatibility.HasDifferentAlgebra;

    public override IEnumerable<IElement> Sources => BoundaryJoin.Sources.Concat(Compatibility.Sources);
}

public sealed record OpenSegmentPathRead(
    IElement Source,
    IReadOnlyList<PathSegmentRead> Segments,
    IReadOnlyList<SegmentJoinRead> Joins,
    IReadOnlyList<SegmentJoinFlowRead> JoinFlows,
    IReadOnlyList<SegmentJoinUnitCompatibilityRead> UnitCompatibilities) : GraphicRead(Source)
{
    public bool HasSingleCarrier =>
        Segments
            .Select(segment => segment.Start.Location.Carrier.IsInverse
                ? segment.Start.Location.Carrier.Inverse()
                : segment.Start.Location.Carrier)
            .DistinctBy(carrier => carrier.Id)
            .Count() == 1;

    public bool IsAllPositive => Segments.All(segment => segment.IsPositive);

    public bool IsAllInverse => Segments.All(segment => segment.IsInverse);

    public bool IsAllDirected => Segments.All(segment => segment.IsDirected);

    public bool IsMixedSegmentForm => !IsAllPositive && !IsAllInverse && !IsAllDirected;

    public bool HasCarrierTransitions => JoinFlows.Any(flow => flow.IsCarrierTransition);

    public bool HasCompatibleUnitProvenance =>
        Joins.Count != 0 &&
        UnitCompatibilities.Count == Joins.Count &&
        UnitCompatibilities.All(compatibility => compatibility.CompatibleWithDescriptors);

    public bool HasSharedTemplateProvenance =>
        HasCompatibleUnitProvenance &&
        UnitCompatibilities.Any(compatibility => compatibility.HasSharedTemplateProvenance);

    public override IEnumerable<IElement> Sources =>
        Segments.Select(segment => segment.Source)
            .Concat(Joins.SelectMany(join => join.Sources))
            .Concat(JoinFlows.SelectMany(flow => flow.Sources))
            .Concat(UnitCompatibilities.SelectMany(compatibility => compatibility.Sources));
}

public sealed record SharedFrameSegmentEndpointRead(
    SharedFrameSegmentRead Segment,
    SharedFramePointRead Point,
    bool IsStart);

public sealed record SharedFramePolylineRead(
    IElement Source,
    IReadOnlyList<SharedFrameSegmentRead> Segments,
    IReadOnlyList<IReadOnlyList<SharedFrameSegmentEndpointRead>> Sites) : GraphicRead(Source)
{
    public int CoordinateCount => Segments.Count == 0 ? 0 : Segments[0].Start.Coordinates.Count;

    public override IEnumerable<IElement> Sources =>
        Segments.Select(segment => segment.Source)
            .Concat(Sites.SelectMany(site => site.Select(endpoint => endpoint.Point.Source)));
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
