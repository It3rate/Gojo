using Gojo.Core;
using GojoCAD;

namespace GojoDraw.Visualizer;

internal sealed record VisualizerDocument(
    Carrier Drawing,
    IReadOnlyList<IElement> Elements,
    int ProjectionSeed,
    int? SelectedIndex = null)
{
    public IElement Structure => Chain(Elements);

    public static VisualizerDocument CreateInitial()
    {
        var sample = VisualizerSample.Create();
        return new VisualizerDocument(
            sample.Drawing,
            sample.Elements,
            sample.ProjectionSeed);
    }

    public static VisualizerDocument CreateEmpty() =>
        new(new Carrier(), [], Random.Shared.Next());

    public static VisualizerDocument CreateRandom(int steps = 28)
    {
        var document = CreateEmpty().AddPoint();
        var legend = VisualizerSample.CreateLegend();

        for (var index = 0; index < steps; index++)
        {
            if (document.Elements.Count != 0 && Random.Shared.NextDouble() < 0.22)
            {
                document = document.SelectIndex(Random.Shared.Next(document.Elements.Count));
            }

            var enabled = legend
                .Where(document.CanApply)
                .Where(entry => entry.Label != "point" || Random.Shared.NextDouble() < 0.2)
                .ToArray();
            if (enabled.Length == 0)
            {
                enabled = [.. legend.Where(document.CanApply)];
            }

            if (enabled.Length == 0)
            {
                break;
            }

            document = document.Apply(enabled[Random.Shared.Next(enabled.Length)]);
        }

        return document.Resample();
    }

    public VisualizerDocument Resample() => this with { ProjectionSeed = Random.Shared.Next() };

    public int? SourceIndexOf(IElement? source)
    {
        if (source is not Relation relation)
        {
            return null;
        }

        var indexes = IndexElements(Elements);
        return indexes.TryGetValue(relation.Id, out var index)
            ? index
            : null;
    }

    public VisualizerDocument SelectSource(IElement? source) =>
        SourceIndexOf(source) is int index
            ? SelectIndex(index)
            : this with { SelectedIndex = null };

    public VisualizerDocument SelectIndex(int index) =>
        index >= 0 && index < Elements.Count
            ? this with { SelectedIndex = index }
            : this with { SelectedIndex = null };

    public bool CanApply(VisualizerLegendEntry entry)
    {
        var selection = ReadSelection();
        return entry.Label switch
        {
            "point" => true,
            "radial" => selection.Point is not null,
            "segment" => selection.LineSeed is not null || selection.Point is not null && selection.CircularMarker is null || SelectedIndex is null,
            "partition" => selection.Segment is not null || selection.CircularMarker is not null,
            "directed" => selection.LineSeed is not null || selection.Point is not null && selection.CircularMarker is null || SelectedIndex is null,
            "line seed" => selection.Point is not null,
            "sheet seed" => selection.Segment is not null && !IsCircularCarrier(selection.Segment.Start.Location.Carrier),
            "radius" => selection.CircularMarker is not null || selection.RadialSeed is not null,
            "circumference" => selection.Point is not null,
            "arc/span" => selection.CircularMarker is not null,
            "coordinate path" => selection.Point is not null || SelectedIndex is null,
            "relative path" => selection.Point is not null || SelectedIndex is null,
            "angle path" => selection.Point is not null || SelectedIndex is null,
            _ => false
        };
    }

    public string DisabledReason(VisualizerLegendEntry entry)
    {
        if (CanApply(entry))
        {
            return $"add {entry.Label}";
        }

        return entry.Label switch
        {
            "point" => "add point",
            "radial" => "select a point",
            "segment" => "select a non-circular point or line seed",
            "partition" => "select a segment or circle point",
            "directed" => "select a non-circular point or line seed",
            "line seed" => "select a point",
            "sheet seed" => "select a segment",
            "radius" => "select a circle point or radial",
            "circumference" => "select a point",
            "arc/span" => "select a circle point",
            "coordinate path" => "select a point",
            "relative path" => "select a point",
            "angle path" => "select a point",
            _ => "not available"
        };
    }

    public VisualizerDocument Apply(VisualizerLegendEntry entry)
    {
        if (!CanApply(entry))
        {
            return this;
        }

        return entry.Label switch
        {
            "point" => AddPoint(),
            "radial" => AddRadial(),
            "segment" => AddSegment(),
            "partition" => AddPartition(),
            "directed" => AddDirected(),
            "line seed" => AddLineSeed(),
            "sheet seed" => AddSheetSeed(),
            "radius" => AddRadiusAttachment(),
            "circumference" => AddCircle(includeRadius: false),
            "arc/span" => AddCircularSpan(),
            "coordinate path" => AddFramePath(),
            "relative path" => AddStepPath(),
            "angle path" => AddAngleStep(),
            _ => this
        };
    }

    private VisualizerDocument AddPoint()
    {
        var selection = ReadSelection();
        if (selection.Segment is not null)
        {
            var location = SampleBetween(selection.Segment.Start.Location, selection.Segment.End.Location);
            return Append([GraphicStructure.Point(location.Carrier, location.Tick)], selectedOffset: 0);
        }

        if (selection.CircularMarker is not null)
        {
            var start = selection.CircularMarker.Point.Location;
            var site = SiteNear(start.Ticks, 3, 9);
            return Append([GraphicStructure.Point(start.Carrier, site)], selectedOffset: 0);
        }

        if (selection.Point is not null)
        {
            var start = selection.Point.Location;
            return Append([GraphicStructure.Point(start.Carrier, SiteNear(start.Ticks, 4, 14))], selectedOffset: 0);
        }

        return Append([GraphicStructure.Point(Drawing, GraphicStructure.Site(SampleFreeTick()))], selectedOffset: 0);
    }

    private VisualizerDocument AddRadial()
    {
        var point = ReadSelection().Point;
        return point is null
            ? this
            : Append([GraphicStructure.RadialFlow(PointAt(point.Location))], selectedOffset: 0);
    }

    private VisualizerDocument AddSegment()
    {
        var selection = ReadSelection();
        if (selection.LineSeed is not null)
        {
            return AddLineSeedLength(selection.LineSeed);
        }

        var point = selection.Point;
        if (point is null)
        {
            var start = GraphicStructure.Site(SampleFreeTick());
            var end = SiteNear(start.Ticks, 7, 18);
            var startPoint = GraphicStructure.Point(Drawing, start);
            var freeEndPoint = GraphicStructure.Point(Drawing, end);
            var segment = GraphicStructure.Segment(Drawing, start, end);
            return Append([startPoint, freeEndPoint, segment], selectedOffset: 1);
        }

        var endSite = SiteNear(point.Location.Ticks, 7, 18);
        var attachedEndPoint = GraphicStructure.Point(point.Location.Carrier, endSite);
        var next = GraphicStructure.Segment(point.Location.Carrier, point.Location.Tick, endSite);
        return Append([attachedEndPoint, next], selectedOffset: 0);
    }

    private VisualizerDocument AddPartition()
    {
        var selection = ReadSelection();
        if (selection.CircularSpan is not null)
        {
            return Append(
                PartitionBetween(
                    selection.CircularSpan.Segment.Start.Location,
                    selection.CircularSpan.Segment.End.Location),
                selectedOffset: 0);
        }

        if (selection.Segment is not null)
        {
            return Append(
                PartitionBetween(selection.Segment.Start.Location, selection.Segment.End.Location),
                selectedOffset: 0);
        }

        if (selection.CircularMarker is not null)
        {
            var marker = selection.CircularMarker.Point.Location;
            var period = ReadObservedCircularPeriod(marker.Carrier, marker.Ticks);
            var step = Math.Max(1, period / 4);
            return Append(
                Enumerable.Range(1, 4)
                    .Select(index => (IElement)GraphicStructure.Point(
                        marker.Carrier,
                        GraphicStructure.Site(marker.Ticks + step * index)))
                    .ToArray(),
                selectedOffset: 0);
        }

        return this;
    }

    private VisualizerDocument AddDirected()
    {
        var selection = ReadSelection();
        if (selection.LineSeed is not null)
        {
            return AddLineSeedLength(selection.LineSeed, directed: true);
        }

        var point = selection.Point;
        var carrier = point?.Location.Carrier ?? Drawing;
        var start = point?.Location.Tick ?? GraphicStructure.Site(SampleFreeTick());
        var end = SiteNear(start.Ticks, 6, 14);
        var directed = GraphicStructure.DirectedSegment(carrier, start, end);
        var endPoint = GraphicStructure.Point(carrier, end);
        return Append([endPoint, directed], selectedOffset: 0);
    }

    private VisualizerDocument AddLineSeed()
    {
        var point = ReadSelection().Point;
        if (point is null)
        {
            return this;
        }

        var zero = GraphicStructure.Segment(point.Location.Carrier, point.Location.Tick, point.Location.Tick);
        return Append([Relation.OrthogonalOutlet(zero)], selectedOffset: 0);
    }

    private VisualizerDocument AddSheetSeed()
    {
        var segment = ReadSelection().Segment;
        if (segment is null)
        {
            return this;
        }

        var source = segment.Source as Relation ??
            GraphicStructure.Segment(
                segment.Start.Location.Carrier,
                segment.Start.Location.Tick,
                segment.End.Location.Tick);
        return Append([Relation.OrthogonalOutlet(source)], selectedOffset: 0);
    }

    private VisualizerDocument AddCircle(bool includeRadius)
    {
        var selection = ReadSelection();
        var originLocation = selection.Point?.Location ??
            new CarrierLocation(Drawing, GraphicStructure.Site(SampleFreeTick()));
        var originPoint = PointAt(originLocation);
        var circle = GraphicStructure.CircularCarrierAt(originPoint);
        var marker = GraphicStructure.Point(circle, GraphicStructure.Site(8));
        var additions = new List<IElement>();

        if (selection.Point is null)
        {
            additions.Add(originPoint);
        }

        additions.Add(marker);
        if (includeRadius)
        {
            additions.Add(CreateRadiusAttachment(originLocation, marker));
        }

        return Append(additions, selectedOffset: additions.IndexOf(marker));
    }

    private VisualizerDocument AddRadiusAttachment()
    {
        var selection = ReadSelection();
        if (selection.RadialSeed is not null)
        {
            return AddCircle(includeRadius: true);
        }

        var marker = selection.CircularMarker;
        if (marker is null)
        {
            return this;
        }

        return Append([CreateRadiusAttachment(marker.Origin, PointAt(marker.Point.Location))], selectedOffset: 0);
    }

    private static Relation CreateRadiusAttachment(CarrierLocation origin, Relation marker) =>
        GraphicStructure.Pin(
            GraphicStructure.Point(origin.Carrier, GraphicStructure.Site(origin.Ticks + 10)),
            marker);

    private VisualizerDocument AddCircularSpan()
    {
        var marker = ReadSelection().CircularMarker;
        if (marker is null)
        {
            return this;
        }

        var start = marker.Point.Location;
        var end = SiteNear(start.Ticks, 4, 12);
        var endPoint = GraphicStructure.Point(start.Carrier, end);
        var span = GraphicStructure.Segment(start.Carrier, start.Tick, end);
        return Append([endPoint, span], selectedOffset: 1);
    }

    private VisualizerDocument AddFramePath()
    {
        var selection = ReadSelection();
        var anchor = selection.Point?.Location ??
            new CarrierLocation(Drawing, GraphicStructure.Site(SampleFreeTick()));
        var anchorPoint = PointAt(anchor);
        var frame = new Carrier();
        var x = new Carrier();
        var y = new Carrier();
        var points = new[]
        {
            GraphicStructure.FramePoint(frame, new CarrierLocation(x, 0), new CarrierLocation(y, 0)),
            GraphicStructure.FramePoint(frame, new CarrierLocation(x, SampleOffset(5, 11)), new CarrierLocation(y, SampleOffset(2, 8))),
            GraphicStructure.FramePoint(frame, new CarrierLocation(x, SampleOffset(12, 20)), new CarrierLocation(y, SampleOffset(4, 12)))
        };
        var anchorPin = GraphicStructure.Pin(points[0], anchorPoint);
        var first = GraphicStructure.FrameSegment(points[0], points[1]);
        var second = GraphicStructure.FrameSegment(points[1], points[2]);

        if (selection.Point is null)
        {
            return Append([anchorPoint, anchorPin, first, second], selectedOffset: 3);
        }

        return Append([anchorPin, first, second], selectedOffset: 2);
    }

    private VisualizerDocument AddStepPath()
    {
        var startPoint = ReadSelection().Point;
        var start = startPoint?.Location ?? new CarrierLocation(Drawing, GraphicStructure.Site(SampleFreeTick()));
        var startRelation = PointAt(start);

        var additions = new List<IElement>();
        if (startPoint is null)
        {
            additions.Add(startRelation);
        }

        var origin = startRelation;
        var incomingCarrier = start.Carrier;
        var incomingTicks = start.Ticks;
        for (var index = 0; index < 3; index++)
        {
            var step = CreateRelativeAngleStep(origin, incomingCarrier, incomingTicks);
            additions.AddRange(step.Elements);
            origin = step.EndPoint;
            incomingCarrier = step.Line;
            incomingTicks = step.End.Ticks;
        }

        var selectedOffset = additions.FindIndex(element => ReferenceEquals(element, origin));
        return Append(additions, selectedOffset: selectedOffset >= 0 ? selectedOffset : additions.Count - 1);
    }

    private static RelativeAngleStep CreateRelativeAngleStep(
        Relation origin,
        Carrier incomingCarrier,
        long incomingTicks)
    {
        var start = GraphicStructure.Site(0);
        var end = GraphicStructure.Site(SampleOffset(7, 12));
        var radiusLength = SampleOffset(7, 12);
        var circleAnchor = GraphicStructure.Site(0);
        var circlePeriod = GraphicStructure.Site(24);
        var turn = GraphicStructure.Site(SampleSignedOffset(3, 9));

        var line = GraphicStructure.LinearCarrierAt(origin);
        var circle = GraphicStructure.CircularCarrierAt(origin);
        var lineStart = GraphicStructure.Point(line, start);
        var lineEnd = GraphicStructure.Point(line, end);
        var join = GraphicStructure.Pin(origin, lineStart);
        var segment = GraphicStructure.Segment(line, start, end);
        var radius = GraphicStructure.Pin(
            GraphicStructure.Point(incomingCarrier, GraphicStructure.Site(incomingTicks + radiusLength)),
            GraphicStructure.Point(circle, circleAnchor));
        var partition = GraphicStructure.Point(circle, turn);
        var period = GraphicStructure.Point(circle, circlePeriod);
        var witness = GraphicStructure.Pin(lineEnd, partition);

        return new RelativeAngleStep(line, end, lineEnd, [join, lineEnd, segment, radius, partition, period, witness]);
    }

    private VisualizerDocument AddAngleStep()
    {
        var startPoint = ReadSelection().Point;
        var start = startPoint?.Location ?? new CarrierLocation(Drawing, GraphicStructure.Site(SampleFreeTick()));
        var startRelation = PointAt(start);
        var lineStart = GraphicStructure.Site(0);
        var lineEnd = GraphicStructure.Site(SampleOffset(7, 14));
        var radiusSite = SiteNear(start.Ticks, 9, 18);
        var circleAnchor = GraphicStructure.Site(0);
        var circlePeriod = GraphicStructure.Site(24);
        var circlePartition = GraphicStructure.Site(SampleOffset(4, 10));

        var line = GraphicStructure.LinearCarrierAt(startRelation);
        var circle = GraphicStructure.CircularCarrierAt(startRelation);
        var lineStartPoint = GraphicStructure.Point(line, lineStart);
        var lineEndPoint = GraphicStructure.Point(line, lineEnd);
        var startJoin = GraphicStructure.Pin(startRelation, lineStartPoint);
        var segment = GraphicStructure.Segment(line, lineStart, lineEnd);
        var radius = GraphicStructure.Pin(
            GraphicStructure.Point(start.Carrier, radiusSite),
            GraphicStructure.Point(circle, circleAnchor));
        var partition = GraphicStructure.Point(circle, circlePartition);
        var period = GraphicStructure.Point(circle, circlePeriod);
        var witness = GraphicStructure.Pin(lineEndPoint, partition);

        var additions = new List<IElement>();
        if (startPoint is null)
        {
            additions.Add(startRelation);
        }

        additions.AddRange([
            startJoin,
            lineEndPoint,
            segment,
            radius,
            partition,
            period,
            witness
        ]);

        return Append(additions, selectedOffset: additions.Count - 6);
    }

    private SelectionContext ReadSelection()
    {
        if (SelectedIndex is not int selectedIndex)
        {
            return new SelectionContext();
        }

        var indexes = IndexElements(Elements);
        bool IsSelected(IElement source) =>
            source is Relation relation &&
            indexes.TryGetValue(relation.Id, out var index) &&
            index == selectedIndex;

        var graph = GraphicReadGraph.From(Structure);
        var point = graph.ReadsOf<PointRead>().FirstOrDefault(read => IsSelected(read.Source));
        var radial = graph.ReadsOf<RadialSeedRead>().FirstOrDefault(read => IsSelected(read.Source));
        var lineSeed = graph.ReadsOf<LineSeedRead>().FirstOrDefault(read => IsSelected(read.Source));
        var segment = graph.ReadsOf<PositiveSegmentRead>().FirstOrDefault(read => IsSelected(read.Source));
        var span = graph.ReadsOf<CircularSpanRead>().FirstOrDefault(read => IsSelected(read.Source));
        var marker = point?.CircularMarker;

        point ??= radial?.Point;
        point ??= lineSeed?.Span.Left;
        marker ??= point?.CircularMarker;

        return new SelectionContext(point, segment, marker, span, radial, lineSeed);
    }

    private VisualizerDocument Append(IReadOnlyList<IElement> additions, int selectedOffset)
    {
        var next = Elements.Concat(additions).ToArray();
        var selected = Elements.Count + Math.Clamp(selectedOffset, 0, Math.Max(0, additions.Count - 1));
        return this with
        {
            Elements = next,
            ProjectionSeed = ProjectionSeed,
            SelectedIndex = selected
        };
    }

    private VisualizerDocument AddLineSeedLength(LineSeedRead seed, bool directed = false)
    {
        var origin = seed.Span.Left.Location;
        var line = GraphicStructure.LinearCarrierAt(PointAt(origin));
        var start = GraphicStructure.Site(0);
        var end = GraphicStructure.Site(12);
        var lineStart = GraphicStructure.Point(line, start);
        var lineEnd = GraphicStructure.Point(line, end);
        var join = GraphicStructure.Pin(PointAt(origin), lineStart);
        var segment = directed
            ? GraphicStructure.DirectedSegment(line, start, end)
            : GraphicStructure.Segment(line, start, end);
        return Append([join, lineEnd, segment], selectedOffset: 1);
    }

    private static Relation PointAt(CarrierLocation location) =>
        GraphicStructure.Point(location.Carrier, location.Tick);

    private static CarrierLocation SampleBetween(CarrierLocation start, CarrierLocation end)
    {
        var lower = Math.Min(start.Ticks, end.Ticks);
        var upper = Math.Max(start.Ticks, end.Ticks);
        if (upper - lower <= 1)
        {
            return new CarrierLocation(start.Carrier, SiteNear(start.Ticks, 2, 8));
        }

        return new CarrierLocation(start.Carrier, GraphicStructure.Site(Random.Shared.NextInt64(lower + 1, upper)));
    }

    private static IElement[] PartitionBetween(CarrierLocation start, CarrierLocation end)
    {
        var delta = end.Ticks - start.Ticks;
        if (Math.Abs(delta) < 4)
        {
            var middle = start.Ticks + Math.Sign(delta == 0 ? 1 : delta) * 2;
            return [GraphicStructure.Point(start.Carrier, GraphicStructure.Site(middle))];
        }

        return Enumerable.Range(1, 3)
            .Select(index =>
            {
                var tick = start.Ticks + delta * index / 4;
                return (IElement)GraphicStructure.Point(start.Carrier, GraphicStructure.Site(tick));
            })
            .ToArray();
    }

    private long ReadObservedCircularPeriod(Carrier carrier, long anchorTicks)
    {
        var deltas = GraphicReadGraph.From(Structure)
            .ReadsOf<PointRead>()
            .Where(point => point.Location.Carrier.SameIdentityAs(carrier))
            .Select(point => Math.Abs(point.Location.Ticks - anchorTicks))
            .Where(delta => delta > 0)
            .Distinct()
            .Order()
            .ToArray();
        if (deltas.Length >= 2 && deltas[^1] >= deltas[^2] * 2)
        {
            return deltas[^1];
        }

        return 24;
    }

    private static TickRef SiteNear(long ticks, int min, int max) =>
        GraphicStructure.Site(ticks + SampleSignedOffset(min, max));

    private static long SampleFreeTick() =>
        Random.Shared.NextInt64(-30, 72);

    private static long SampleOffset(int min, int max) =>
        Random.Shared.NextInt64(min, max + 1);

    private static long SampleSignedOffset(int min, int max)
    {
        var offset = SampleOffset(min, max);
        return Random.Shared.Next(2) == 0 ? offset : -offset;
    }

    private static bool IsCircularCarrier(Carrier carrier) =>
        carrier.TryReadCircularOrigin(out _);

    private static IElement Chain(IReadOnlyList<IElement> elements)
    {
        if (elements.Count == 0)
        {
            return Carrier.Unknown;
        }

        var structure = elements[^1];
        for (var index = elements.Count - 2; index >= 0; index--)
        {
            structure = new Relation(elements[index], structure);
        }

        return structure;
    }

    private static IReadOnlyDictionary<ulong, int> IndexElements(IReadOnlyList<IElement> elements)
    {
        var indexes = new Dictionary<ulong, int>();
        for (var index = 0; index < elements.Count; index++)
        {
            if (elements[index] is Relation relation)
            {
                indexes[relation.Id] = index;
            }
        }

        return indexes;
    }

    private sealed record SelectionContext(
        PointRead? Point = null,
        PositiveSegmentRead? Segment = null,
        CircularMarkerRead? CircularMarker = null,
        CircularSpanRead? CircularSpan = null,
        RadialSeedRead? RadialSeed = null,
        LineSeedRead? LineSeed = null);

    private sealed record RelativeAngleStep(
        Carrier Line,
        TickRef End,
        Relation EndPoint,
        IReadOnlyList<IElement> Elements);
}
