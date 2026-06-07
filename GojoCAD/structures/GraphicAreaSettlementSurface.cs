using Gojo.Core;

namespace GojoCAD.Structures;

/// <summary>
/// Local area-boundary settlement for shared-frame segment and circular-arc
/// participants. The numeric work here is a read: the result is emitted as
/// ordinary Gojo area and outline structure.
/// </summary>
public static class GraphicAreaSettlementSurface
{
    private const double Epsilon = 0.01;
    private const double SampleDistance = 0.55;

    /// <summary>
    /// Settles an area frame into positive and anti area loops by splitting
    /// segment/arc boundaries at intersections and keeping only fragments that
    /// separate filled from unfilled space.
    /// </summary>
    public static Relation SettledAreaFrame(Carrier root, IElement areaFrame)
    {
        var settlement = CreateSettlement(areaFrame);
        return GraphicBooleanSurface.AreaFrame(RootOrNew(root), [.. settlement.AreaFills]);
    }

    /// <summary>Reads the settled boundary loops from an area frame.</summary>
    public static Relation SettledBoundaryOutlinesFromAreas(Carrier root, IElement areaFrame)
    {
        var settlement = CreateSettlement(areaFrame);
        return GraphicOutlineSurface.OutlineFrame(RootOrNew(root), [.. settlement.Outlines]);
    }

    private static SettlementRead CreateSettlement(IElement areaFrame)
    {
        if (areaFrame is not Relation frame ||
            !GraphicBooleanSurface.TryReadAreaFrame(frame, out var participantElements) ||
            participantElements.Length == 0)
        {
            return new SettlementRead([], []);
        }

        var participants = participantElements
            .Select(ReadParticipant)
            .Where(participant => participant.Positive.Count != 0 || participant.Negative.Count != 0)
            .ToArray();
        if (participants.Length == 0)
        {
            return new SettlementRead([], []);
        }

        var edges = participants
            .SelectMany(participant => participant.Positive.Concat(participant.Negative))
            .SelectMany(boundary => boundary.Edges)
            .ToArray();
        if (edges.Length == 0)
        {
            return new SettlementRead([], []);
        }

        var splitParameters = edges.ToDictionary(edge => edge, _ => new List<double> { 0d, 1d });
        for (var leftIndex = 0; leftIndex < edges.Length; leftIndex++)
        for (var rightIndex = leftIndex + 1; rightIndex < edges.Length; rightIndex++)
        {
            foreach (var intersection in Intersections(edges[leftIndex], edges[rightIndex]))
            {
                if (edges[leftIndex].TryParameterOf(intersection, out var leftT))
                {
                    splitParameters[edges[leftIndex]].Add(leftT);
                }

                if (edges[rightIndex].TryParameterOf(intersection, out var rightT))
                {
                    splitParameters[edges[rightIndex]].Add(rightT);
                }
            }
        }

        var fragments = new List<FragmentRead>();
        foreach (var edge in edges)
        {
            var parameters = splitParameters[edge]
                .Where(t => t >= -Epsilon && t <= 1d + Epsilon)
                .Select(t => Math.Clamp(t, 0d, 1d))
                .DistinctBy(t => Math.Round(t, 6))
                .Order()
                .ToArray();
            for (var index = 0; index + 1 < parameters.Length; index++)
            {
                var startT = parameters[index];
                var endT = parameters[index + 1];
                if (endT - startT < Epsilon)
                {
                    continue;
                }

                var fragment = edge.Fragment(startT, endT);
                var leftInside = Contains(participants, fragment.LeftSample());
                var rightInside = Contains(participants, fragment.RightSample());
                if (leftInside == rightInside)
                {
                    continue;
                }

                fragments.Add(fragment);
            }
        }

        var loops = BuildLoops(fragments);
        var outlines = new List<IElement>();
        var fills = new List<IElement>();
        foreach (var loop in loops)
        {
            var elements = loop.Select(fragment => fragment.ToElement()).ToArray();
            if (elements.Length < 3)
            {
                continue;
            }

            var outline = Chain(elements);
            outlines.Add(outline);
            var interiorSample = InteriorSample(loop);
            var isPositive = Contains(participants, interiorSample);
            fills.Add(isPositive
                ? GraphicStructure.AreaFill(outline)
                : GraphicStructure.AntiAreaFill(outline));
        }

        return new SettlementRead([.. fills], [.. outlines]);
    }

    private static ParticipantRead ReadParticipant(IElement participant)
    {
        var graph = GraphicReadGraph.From(participant);
        var positive = new List<BoundaryRead>();
        var negative = new List<BoundaryRead>();
        foreach (var area in graph.ReadsOf<SharedFrameAreaRead>()
            .DistinctBy(area => SourceId(area.Source)))
        {
            if (!TryReadBoundary(area.Boundary, area.IsAntiArea, out var boundary))
            {
                continue;
            }

            if (area.IsAntiArea)
            {
                negative.Add(boundary);
            }
            else
            {
                positive.Add(boundary);
            }
        }

        return new ParticipantRead([.. positive], [.. negative]);
    }

    private static bool TryReadBoundary(
        ClosedSharedFrameBoundaryRead boundary,
        bool isAnti,
        out BoundaryRead read)
    {
        read = default!;
        if (!TryReadOrientedBoundaryEdges(boundary, out var oriented))
        {
            return false;
        }

        var arcBySpanSource = (boundary.Arcs ?? [])
            .GroupBy(arc => SourceId(arc.Span.Source))
            .ToDictionary(group => group.Key, group => group.First());
        var edges = new List<BoundaryEdge>();
        foreach (var edge in oriented)
        {
            if (arcBySpanSource.TryGetValue(SourceId(edge.Segment.Source), out var arc))
            {
                if (!TryCreateArcEdge(edge, arc, out var arcEdge))
                {
                    continue;
                }

                edges.Add(arcEdge);
            }
            else
            {
                if (!TryCreateLineEdge(edge, out var lineEdge))
                {
                    continue;
                }

                edges.Add(lineEdge);
            }
        }

        if (edges.Count < 3)
        {
            return false;
        }

        read = new BoundaryRead(isAnti, [.. edges]);
        return true;
    }

    private static bool TryCreateLineEdge(OrientedBoundaryEdge source, out BoundaryEdge edge)
    {
        edge = default!;
        if (!TryReadPoint(source.StartEndpoint.Point, out var start) ||
            !TryReadPoint(source.EndEndpoint.Point, out var end) ||
            NearlySame(start.Point, end.Point))
        {
            return false;
        }

        edge = BoundaryEdge.Line(start.Template, start.Point, end.Point);
        return true;
    }

    private static bool TryCreateArcEdge(
        OrientedBoundaryEdge source,
        SharedFrameArcRead arc,
        out BoundaryEdge edge)
    {
        edge = default!;
        if (!TryReadPoint(source.StartEndpoint.Point, out var start) ||
            !TryReadPoint(source.EndEndpoint.Point, out var end) ||
            !TryReadPoint(arc.Center, out var center) ||
            NearlySame(start.Point, end.Point))
        {
            return false;
        }

        edge = BoundaryEdge.Arc(start.Template, center.Point, start.Point, end.Point);
        return true;
    }

    private static IEnumerable<Point2> Intersections(BoundaryEdge left, BoundaryEdge right)
    {
        if (!left.IsArc && !right.IsArc)
        {
            return LineLineIntersections(left, right);
        }

        if (!left.IsArc && right.IsArc)
        {
            return LineArcIntersections(left, right);
        }

        if (left.IsArc && !right.IsArc)
        {
            return LineArcIntersections(right, left);
        }

        return ArcArcIntersections(left, right);
    }

    private static IEnumerable<Point2> LineLineIntersections(BoundaryEdge left, BoundaryEdge right)
    {
        var p = left.Start;
        var r = left.End - left.Start;
        var q = right.Start;
        var s = right.End - right.Start;
        var cross = Cross(r, s);
        if (Math.Abs(cross) < Epsilon)
        {
            yield break;
        }

        var t = Cross(q - p, s) / cross;
        var u = Cross(q - p, r) / cross;
        if (WithinUnit(t) && WithinUnit(u))
        {
            yield return p + r * t;
        }
    }

    private static IEnumerable<Point2> LineArcIntersections(BoundaryEdge line, BoundaryEdge arc)
    {
        var d = line.End - line.Start;
        var f = line.Start - arc.Center;
        var a = Dot(d, d);
        var b = 2d * Dot(f, d);
        var c = Dot(f, f) - arc.Radius * arc.Radius;
        var discriminant = b * b - 4d * a * c;
        if (discriminant < -Epsilon ||
            Math.Abs(a) < Epsilon)
        {
            yield break;
        }

        var root = Math.Sqrt(Math.Max(0d, discriminant));
        foreach (var t in new[] { (-b - root) / (2d * a), (-b + root) / (2d * a) })
        {
            var point = line.Start + d * t;
            if (WithinUnit(t) &&
                arc.TryParameterOf(point, out _))
            {
                yield return point;
            }
        }
    }

    private static IEnumerable<Point2> ArcArcIntersections(BoundaryEdge left, BoundaryEdge right)
    {
        var delta = right.Center - left.Center;
        var distance = Length(delta);
        if (distance < Epsilon ||
            distance > left.Radius + right.Radius + Epsilon ||
            distance < Math.Abs(left.Radius - right.Radius) - Epsilon)
        {
            yield break;
        }

        var a = (left.Radius * left.Radius - right.Radius * right.Radius + distance * distance) / (2d * distance);
        var hSquared = left.Radius * left.Radius - a * a;
        if (hSquared < -Epsilon)
        {
            yield break;
        }

        var h = Math.Sqrt(Math.Max(0d, hSquared));
        var direction = delta / distance;
        var basePoint = left.Center + direction * a;
        var normal = new Point2(-direction.Y, direction.X);
        foreach (var point in new[] { basePoint + normal * h, basePoint - normal * h })
        {
            if (left.TryParameterOf(point, out _) &&
                right.TryParameterOf(point, out _))
            {
                yield return point;
            }
        }
    }

    private static IReadOnlyList<IReadOnlyList<FragmentRead>> BuildLoops(IReadOnlyList<FragmentRead> fragments)
    {
        var unused = new HashSet<int>(Enumerable.Range(0, fragments.Count));
        var loops = new List<IReadOnlyList<FragmentRead>>();
        while (unused.Count > 0)
        {
            var seedIndex = unused.First();
            unused.Remove(seedIndex);
            var seed = fragments[seedIndex];
            var loop = new List<FragmentRead> { seed };
            var startKey = PointKey(seed.Start);
            var currentKey = PointKey(seed.End);
            var guard = 0;
            while (currentKey != startKey && guard++ < fragments.Count + 2)
            {
                if (!TryChooseNextFragment(fragments, unused, loop[^1], currentKey, out var nextIndex, out var next))
                {
                    break;
                }

                unused.Remove(nextIndex);
                loop.Add(next);
                currentKey = PointKey(next.End);
            }

            if (currentKey == startKey && loop.Count >= 3)
            {
                loops.Add(loop);
            }
        }

        return loops;
    }

    private static bool TryChooseNextFragment(
        IReadOnlyList<FragmentRead> fragments,
        IReadOnlySet<int> unused,
        FragmentRead current,
        string currentKey,
        out int nextIndex,
        out FragmentRead next)
    {
        var incoming = current.TangentAtEnd();
        var candidates = unused
            .Select(index =>
            {
                var candidate = fragments[index];
                if (PointKey(candidate.Start) == currentKey)
                {
                    return (index, fragment: candidate, score: Dot(Unit(incoming), Unit(candidate.TangentAtStart())));
                }

                if (PointKey(candidate.End) == currentKey)
                {
                    var reversed = candidate.Reversed();
                    return (index, fragment: reversed, score: Dot(Unit(incoming), Unit(reversed.TangentAtStart())));
                }

                return (index: -1, fragment: candidate, score: double.NegativeInfinity);
            })
            .Where(candidate => candidate.index >= 0)
            .OrderByDescending(candidate => candidate.score)
            .ToArray();
        if (candidates.Length == 0)
        {
            nextIndex = -1;
            next = default!;
            return false;
        }

        nextIndex = candidates[0].index;
        next = candidates[0].fragment;
        return true;
    }

    private static Point2 InteriorSample(IReadOnlyList<FragmentRead> loop)
    {
        var area = SignedArea(loop);
        return area >= 0d
            ? loop[0].LeftSample()
            : loop[0].RightSample();
    }

    private static double SignedArea(IReadOnlyList<FragmentRead> loop)
    {
        var points = new List<Point2>();
        foreach (var fragment in loop)
        {
            points.AddRange(fragment.Flatten());
        }

        if (points.Count < 3)
        {
            return 0d;
        }

        var area = 0d;
        for (var index = 0; index < points.Count; index++)
        {
            var current = points[index];
            var next = points[(index + 1) % points.Count];
            area += current.X * next.Y - next.X * current.Y;
        }

        return area * 0.5d;
    }

    private static bool Contains(IReadOnlyList<ParticipantRead> participants, Point2 point)
    {
        var positive = false;
        var globalCut = false;
        foreach (var participant in participants)
        {
            if (participant.Positive.Count == 0)
            {
                if (participant.Negative.Any(boundary => boundary.Contains(point)))
                {
                    globalCut = true;
                }

                continue;
            }

            if (participant.Positive.Any(boundary => boundary.Contains(point)) &&
                !participant.Negative.Any(boundary => boundary.Contains(point)))
            {
                positive = true;
            }
        }

        return positive && !globalCut;
    }

    private static bool TryReadOrientedBoundaryEdges(
        ClosedSharedFrameBoundaryRead boundary,
        out IReadOnlyList<OrientedBoundaryEdge> edges)
    {
        var siteByEndpoint = boundary.Sites
            .SelectMany(site => site.Select(endpoint => (endpoint, site)))
            .ToDictionary(pair => EndpointSignature(pair.endpoint), pair => pair.site);
        var first = boundary.Segments[0];
        var incoming = new SharedFrameSegmentEndpointRead(first, first.Start, IsStart: true);
        var visited = new HashSet<ulong>();
        var ordered = new List<OrientedBoundaryEdge>();
        while (visited.Add(SourceId(incoming.Segment.Source)))
        {
            var outgoing = OtherEndpoint(incoming);
            ordered.Add(new OrientedBoundaryEdge(incoming.Segment, incoming, outgoing));
            if (!siteByEndpoint.TryGetValue(EndpointSignature(outgoing), out var site))
            {
                break;
            }

            var next = site.FirstOrDefault(endpoint =>
                SourceId(endpoint.Segment.Source) != SourceId(incoming.Segment.Source) &&
                !visited.Contains(SourceId(endpoint.Segment.Source)));
            if (next is null)
            {
                break;
            }

            incoming = next;
        }

        edges = ordered;
        return ordered.Count == boundary.Segments.Count;
    }

    private static bool TryReadPoint(SharedFramePointRead point, out FramePointTicks ticks)
    {
        if (point.Coordinates.Count >= 2)
        {
            ticks = new FramePointTicks(
                point,
                new Point2(point.Coordinates[0].Ticks, point.Coordinates[1].Ticks));
            return true;
        }

        ticks = default;
        return false;
    }

    private static Relation CreateFramePoint(SharedFramePointRead template, Point2 point)
    {
        var roundedX = Rounded(point.X);
        var roundedY = Rounded(point.Y);
        var coordinates = template.Coordinates
            .Select((coordinate, index) => new CarrierLocation(
                coordinate.Carrier,
                index == 0 ? roundedX : index == 1 ? roundedY : coordinate.Ticks))
            .ToArray();
        return GraphicStructure.FramePoint(template.Holder, coordinates);
    }

    private static Relation Chain(params IElement[] elements)
    {
        if (elements.Length == 0)
        {
            var slot = new Placeholder();
            return new Relation(slot, slot.Dual());
        }

        IElement structure = elements[^1];
        for (var index = elements.Length - 2; index >= 0; index--)
        {
            structure = new Relation(elements[index], structure);
        }

        return structure as Relation ?? new Relation(structure, structure);
    }

    private static SharedFrameSegmentEndpointRead OtherEndpoint(SharedFrameSegmentEndpointRead endpoint) =>
        endpoint.IsStart
            ? new SharedFrameSegmentEndpointRead(endpoint.Segment, endpoint.Segment.End, IsStart: false)
            : new SharedFrameSegmentEndpointRead(endpoint.Segment, endpoint.Segment.Start, IsStart: true);

    private static string EndpointSignature(SharedFrameSegmentEndpointRead endpoint) =>
        $"{SourceId(endpoint.Segment.Source)}:{(endpoint.IsStart ? "s" : "e")}";

    private static string PointKey(Point2 point) =>
        $"{Rounded(point.X)}:{Rounded(point.Y)}";

    private static long Rounded(double value) =>
        (long)Math.Round(value, MidpointRounding.AwayFromZero);

    private static bool NearlySame(Point2 left, Point2 right) =>
        Length(left - right) < Epsilon;

    private static bool WithinUnit(double value) =>
        value >= -Epsilon && value <= 1d + Epsilon;

    private static double Cross(Point2 left, Point2 right) =>
        left.X * right.Y - left.Y * right.X;

    private static double Dot(Point2 left, Point2 right) =>
        left.X * right.X + left.Y * right.Y;

    private static double Length(Point2 point) =>
        Math.Sqrt(point.X * point.X + point.Y * point.Y);

    private static Point2 Unit(Point2 point)
    {
        var length = Length(point);
        return length < Epsilon
            ? new Point2(0d, 0d)
            : point / length;
    }

    private static double Angle(Point2 center, Point2 point) =>
        Math.Atan2(point.Y - center.Y, point.X - center.X);

    private static double ShortestSweep(double startAngle, double endAngle)
    {
        var sweep = endAngle - startAngle;
        while (sweep <= -Math.PI)
        {
            sweep += Math.PI * 2d;
        }

        while (sweep > Math.PI)
        {
            sweep -= Math.PI * 2d;
        }

        return sweep;
    }

    private static Carrier RootOrNew(Carrier root) => root ?? new Carrier();

    private sealed record SettlementRead(IReadOnlyList<IElement> AreaFills, IReadOnlyList<IElement> Outlines);

    private sealed record ParticipantRead(
        IReadOnlyList<BoundaryRead> Positive,
        IReadOnlyList<BoundaryRead> Negative);

    private sealed record BoundaryRead(bool IsAnti, IReadOnlyList<BoundaryEdge> Edges)
    {
        public bool Contains(Point2 point)
        {
            var flattened = Edges.SelectMany(edge => edge.Flatten()).ToArray();
            if (flattened.Length < 3)
            {
                return false;
            }

            var inside = false;
            for (int i = 0, j = flattened.Length - 1; i < flattened.Length; j = i++)
            {
                var current = flattened[i];
                var previous = flattened[j];
                if ((current.Y > point.Y) != (previous.Y > point.Y) &&
                    point.X < (previous.X - current.X) * (point.Y - current.Y) / (previous.Y - current.Y + double.Epsilon) + current.X)
                {
                    inside = !inside;
                }
            }

            return inside;
        }
    }

    private sealed record BoundaryEdge(
        SharedFramePointRead Template,
        bool IsArc,
        Point2 Start,
        Point2 End,
        Point2 Center,
        double Radius,
        double StartAngle,
        double Sweep)
    {
        public static BoundaryEdge Line(SharedFramePointRead template, Point2 start, Point2 end) =>
            new(template, IsArc: false, start, end, default, 0d, 0d, 0d);

        public static BoundaryEdge Arc(SharedFramePointRead template, Point2 center, Point2 start, Point2 end)
        {
            var startAngle = Angle(center, start);
            return new(
                template,
                IsArc: true,
                start,
                end,
                center,
                (Length(start - center) + Length(end - center)) * 0.5d,
                startAngle,
                ShortestSweep(startAngle, Angle(center, end)));
        }

        public bool TryParameterOf(Point2 point, out double t)
        {
            if (!IsArc)
            {
                var delta = End - Start;
                var lengthSquared = Dot(delta, delta);
                if (lengthSquared < Epsilon)
                {
                    t = 0d;
                    return false;
                }

                t = Dot(point - Start, delta) / lengthSquared;
                var projected = Start + delta * t;
                return WithinUnit(t) && Length(projected - point) <= 0.75d;
            }

            if (Math.Abs(Radius) < Epsilon ||
                Math.Abs(Length(point - Center) - Radius) > 0.85d)
            {
                t = 0d;
                return false;
            }

            var angle = Angle(Center, point);
            var deltaAngle = ShortestSweep(StartAngle, angle);
            t = Math.Abs(Sweep) < Epsilon ? 0d : deltaAngle / Sweep;
            return WithinUnit(t);
        }

        public FragmentRead Fragment(double startT, double endT) =>
            new(Template, IsArc, PointAt(startT), PointAt(endT), Center, Radius, StartAngle + Sweep * startT, Sweep * (endT - startT));

        public Point2 PointAt(double t)
        {
            if (!IsArc)
            {
                return Start + (End - Start) * t;
            }

            var angle = StartAngle + Sweep * t;
            return new Point2(
                Center.X + Math.Cos(angle) * Radius,
                Center.Y + Math.Sin(angle) * Radius);
        }

        public IEnumerable<Point2> Flatten()
        {
            if (!IsArc)
            {
                yield return Start;
                yield return End;
                yield break;
            }

            var steps = Math.Max(2, (int)Math.Ceiling(Math.Abs(Sweep) / (Math.PI / 16d)));
            for (var index = 0; index <= steps; index++)
            {
                yield return PointAt(index / (double)steps);
            }
        }

    }

    private sealed record FragmentRead(
        SharedFramePointRead Template,
        bool IsArc,
        Point2 Start,
        Point2 End,
        Point2 Center,
        double Radius,
        double StartAngle,
        double Sweep)
    {
        public FragmentRead Reversed() =>
            new(Template, IsArc, End, Start, Center, Radius, StartAngle + Sweep, -Sweep);

        public Point2 MidPoint() =>
            IsArc
                ? PointAt(0.5d)
                : Start + (End - Start) * 0.5d;

        public Point2 LeftSample()
        {
            var normal = LeftNormal();
            return MidPoint() + normal * SampleDistance;
        }

        public Point2 RightSample()
        {
            var normal = LeftNormal();
            return MidPoint() - normal * SampleDistance;
        }

        public IElement ToElement()
        {
            var start = CreateFramePoint(Template, Start);
            var end = CreateFramePoint(Template, End);
            if (!IsArc)
            {
                return GraphicStructure.FrameSegment(start, end);
            }

            return GraphicStructure.FrameArc(CreateFramePoint(Template, Center), start, end);
        }

        public IEnumerable<Point2> Flatten()
        {
            if (!IsArc)
            {
                yield return Start;
                yield return End;
                yield break;
            }

            var steps = Math.Max(2, (int)Math.Ceiling(Math.Abs(Sweep) / (Math.PI / 16d)));
            for (var index = 0; index <= steps; index++)
            {
                yield return PointAt(index / (double)steps);
            }
        }

        public Point2 TangentAtStart()
        {
            if (!IsArc)
            {
                return End - Start;
            }

            var sign = Math.Sign(Sweep == 0d ? 1d : Sweep);
            return new Point2(-Math.Sin(StartAngle) * sign, Math.Cos(StartAngle) * sign);
        }

        public Point2 TangentAtEnd()
        {
            if (!IsArc)
            {
                return End - Start;
            }

            var sign = Math.Sign(Sweep == 0d ? 1d : Sweep);
            var endAngle = StartAngle + Sweep;
            return new Point2(-Math.Sin(endAngle) * sign, Math.Cos(endAngle) * sign);
        }

        private Point2 PointAt(double t)
        {
            if (!IsArc)
            {
                return Start + (End - Start) * t;
            }

            var angle = StartAngle + Sweep * t;
            return new Point2(
                Center.X + Math.Cos(angle) * Radius,
                Center.Y + Math.Sin(angle) * Radius);
        }

        private Point2 LeftNormal()
        {
            var tangent = IsArc
                ? new Point2(
                    -Math.Sin(StartAngle + Sweep * 0.5d) * Math.Sign(Sweep == 0d ? 1d : Sweep),
                    Math.Cos(StartAngle + Sweep * 0.5d) * Math.Sign(Sweep == 0d ? 1d : Sweep))
                : End - Start;
            var length = Length(tangent);
            if (length < Epsilon)
            {
                return new Point2(0d, 0d);
            }

            tangent /= length;
            return new Point2(-tangent.Y, tangent.X);
        }
    }

    private readonly record struct FramePointTicks(SharedFramePointRead Template, Point2 Point);

    private readonly record struct OrientedBoundaryEdge(
        SharedFrameSegmentRead Segment,
        SharedFrameSegmentEndpointRead StartEndpoint,
        SharedFrameSegmentEndpointRead EndEndpoint);

    private readonly record struct Point2(double X, double Y)
    {
        public static Point2 operator +(Point2 left, Point2 right) =>
            new(left.X + right.X, left.Y + right.Y);

        public static Point2 operator -(Point2 left, Point2 right) =>
            new(left.X - right.X, left.Y - right.Y);

        public static Point2 operator *(Point2 point, double scale) =>
            new(point.X * scale, point.Y * scale);

        public static Point2 operator /(Point2 point, double scale) =>
            new(point.X / scale, point.Y / scale);
    }

    private static ulong SourceId(IElement source) =>
        source is Relation relation ? relation.Id : 0UL;
}
