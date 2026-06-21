using Gojo.Core;
using GojoCAD;
using GojoCAD.Structures;
using ScratchMill.KiCad;
using ScratchMill.KiCad.Design;
using ScratchMill.KiCad.Graphics;

namespace ScratchMill.GojoImport;

public static class KiCadGojoImporter
{
    private const double TicksPerMillimeter = 1_000_000d;

    public static Relation ImportFile(string filePath) =>
        ImportPcb(KiCadParser.Parse(filePath));

    public static Relation ImportPcb(KcPcb pcb)
    {
        var context = new ImportContext(pcb);
        var elements = new List<IElement>
        {
            context.Axes(),
            context.BoardBounds()
        };

        elements.AddRange(pcb.Layers.Select(context.Layer));
        elements.AddRange(pcb.Nets.Select(context.Net));
        elements.AddRange(pcb.Segments.Select(context.Segment));
        elements.AddRange(pcb.Arcs.Select(context.Arc));
        elements.AddRange(pcb.Pads.Select(context.Pad));
        elements.AddRange(pcb.Vias.Select(context.Via));
        elements.AddRange(pcb.GrElements.Select(context.Graphic));

        return Relation.OrderedGroup(context.Frame, elements.ToArray());
    }

    private sealed class ImportContext
    {
        private readonly KcPcb pcb;
        private readonly Dictionary<string, int> layerIndexes;

        public ImportContext(KcPcb pcb)
        {
            this.pcb = pcb;
            layerIndexes = pcb.Layers
                .GroupBy(layer => layer.Name)
                .ToDictionary(group => group.Key, group => group.First().Index);
        }

        public Carrier Frame { get; } = new();
        private Carrier X { get; } = new();
        private Carrier Y { get; } = new();
        private Carrier Width { get; } = new();
        private Carrier Height { get; } = new();
        private Carrier Drill { get; } = new();
        private Carrier NetAxis { get; } = new();
        private Carrier LayerAxis { get; } = new();
        private Carrier IndexAxis { get; } = new();
        private Carrier CountAxis { get; } = new();

        public Relation Axes() =>
            Relation.OrderedGroup(new Carrier(), X, Y, Width, Height, Drill, NetAxis, LayerAxis, IndexAxis, CountAxis);

        public Relation BoardBounds()
        {
            var (minimum, maximum) = pcb.Bounds;
            return Relation.OrderedGroup(
                new Carrier(),
                Point(minimum),
                Point(maximum),
                Relation.Point(CountAxis, pcb.Nets.Count),
                Relation.Point(CountAxis, pcb.Layers.Count));
        }

        public Relation Layer(KcLayer layer) =>
            Relation.OrderedGroup(
                new Carrier(),
                Relation.Point(IndexAxis, layer.Index));

        public Relation Net(KcNet net) =>
            EcadNetSurface.Create(new Carrier(), NetAxis, net.Id, net.Name);

        public Relation Segment(KcSegment segment) =>
            WithCopperEvidence(
                GraphicStructure.FrameSegment(Point(segment.At), Point(segment.End)),
                segment.Layer,
                segment.NetId,
                segment.Width);

        public Relation Arc(KcArc arc)
        {
            var center = TryArcCenter(arc.At, arc.Mid, arc.End, out var arcCenter)
                ? Point(arcCenter)
                : Point(arc.Mid);

            return WithCopperEvidence(
                GraphicStructure.FrameArcSection(center, Point(arc.At), Point(arc.Mid), Point(arc.End)),
                arc.Layer,
                arc.NetId,
                arc.Width);
        }

        public Relation Pad(KcPad pad)
        {
            var center = Point(pad.At);
            var halfWidth = Scale((pad.Size?.Width ?? 0) / 2d);
            var halfHeight = Scale((pad.Size?.Height ?? 0) / 2d);
            var body = pad.Shape?.Contains("circle", StringComparison.OrdinalIgnoreCase) == true ||
                Math.Abs(halfWidth - halfHeight) < 1
                    ? GraphicStructure.FrameCircle(center, Point(pad.At.X + (pad.Size?.Width ?? 0) / 2d, pad.At.Y))
                    : RectBody(pad.At, pad.Size ?? KcSize.Empty);

            return Relation.OrderedGroup(
                new Carrier(),
                body,
                center,
                Relation.Point(Width, halfWidth * 2),
                Relation.Point(Height, halfHeight * 2),
                Relation.Point(Drill, Scale(pad.DrillSize)),
                Relation.Point(NetAxis, pad.NetId),
                LayerGroup(pad.Layers));
        }

        public Relation Via(KcVia via)
        {
            var center = Point(via.At);
            return Relation.OrderedGroup(
                new Carrier(),
                GraphicStructure.FrameCircle(center, Point(via.At.X + via.Size.Width / 2d, via.At.Y)),
                center,
                Relation.Point(Width, Scale(via.Size.Width)),
                Relation.Point(Drill, Scale(via.DrillSize)),
                Relation.Point(NetAxis, via.NetId),
                LayerGroup(via.Layers));
        }

        public Relation Graphic(KcGrBase graphic)
        {
            var layer = LayerPoint(graphic.Layer);
            var stroke = Relation.Point(Width, Scale(graphic.Stroke?.Width ?? 0));
            var body = graphic switch
            {
                KcGrRect => RectBody(graphic.Start, SizeBetween(graphic.Start, graphic.End)),
                KcGrCircle => GraphicStructure.FrameCircle(Point(graphic.Start), Point(graphic.End)),
                KcGrArc when graphic.Points.Count >= 3 => GraphicStructure.FrameArcSection(
                    Point(graphic.Points[1]),
                    Point(graphic.Points[0]),
                    Point(graphic.Points[1]),
                    Point(graphic.Points[2])),
                KcGrLine => GraphicStructure.FrameSegment(Point(graphic.Start), Point(graphic.End)),
                KcGrPoly => PolygonBody(graphic.Points),
                _ => Relation.Point(Carrier.Unknown, 0)
            };

            return Relation.OrderedGroup(new Carrier(), body, layer, stroke);
        }

        private Relation WithCopperEvidence(Relation geometry, string layer, int netId, double width) =>
            Relation.OrderedGroup(
                new Carrier(),
                geometry,
                LayerPoint(layer),
                Relation.Point(NetAxis, netId),
                Relation.Point(Width, Scale(width)));

        private Relation RectBody(KcPosition center, KcSize size)
        {
            var halfWidth = size.Width / 2d;
            var halfHeight = size.Height / 2d;
            var topLeft = Point(center.X - halfWidth, center.Y - halfHeight);
            var topRight = Point(center.X + halfWidth, center.Y - halfHeight);
            var bottomRight = Point(center.X + halfWidth, center.Y + halfHeight);
            var bottomLeft = Point(center.X - halfWidth, center.Y + halfHeight);

            var edges = new IElement[]
            {
                GraphicStructure.FrameSegment(topLeft, topRight),
                GraphicStructure.FrameSegment(topRight, bottomRight),
                GraphicStructure.FrameSegment(bottomRight, bottomLeft),
                GraphicStructure.FrameSegment(bottomLeft, topLeft)
            };

            return Relation.OrderedGroup(new Carrier(), edges);
        }

        private Relation PolygonBody(IReadOnlyList<KcPosition> points)
        {
            if (points.Count < 2)
            {
                return Relation.Point(Carrier.Unknown, 0);
            }

            var edges = new List<IElement>();
            for (var index = 0; index < points.Count; index++)
            {
                var next = (index + 1) % points.Count;
                edges.Add(GraphicStructure.FrameSegment(Point(points[index]), Point(points[next])));
            }

            return Relation.OrderedGroup(new Carrier(), edges.ToArray());
        }

        private Relation LayerGroup(IEnumerable<string> layers) =>
            Relation.OrderedGroup(new Carrier(), layers.Select(LayerPoint).Cast<IElement>().ToArray());

        private Relation LayerPoint(string? layerName) =>
            Relation.Point(LayerAxis, layerName is not null && layerIndexes.TryGetValue(layerName, out var index) ? index : -1);

        private Relation Point(KcPosition position) =>
            Point(position.X, position.Y);

        private Relation Point(double x, double y) =>
            GraphicStructure.FramePoint(
                Frame,
                new CarrierLocation(X, Scale(x)),
                new CarrierLocation(Y, Scale(y)));

        private static KcSize SizeBetween(KcPosition start, KcPosition end) =>
            new(Math.Abs(end.X - start.X), Math.Abs(end.Y - start.Y));

        private static long Scale(double millimeters) =>
            checked((long)Math.Round(millimeters * TicksPerMillimeter));

        private static bool TryArcCenter(KcPosition start, KcPosition mid, KcPosition end, out KcPosition center)
        {
            var a = start.X * start.X + start.Y * start.Y;
            var b = mid.X * mid.X + mid.Y * mid.Y;
            var c = end.X * end.X + end.Y * end.Y;

            var determinant = start.X * (mid.Y - end.Y) +
                mid.X * (end.Y - start.Y) +
                end.X * (start.Y - mid.Y);

            if (Math.Abs(determinant) < double.Epsilon)
            {
                center = KcPosition.Empty;
                return false;
            }

            var denominator = 2d * determinant;
            center = new KcPosition(
                ((a * (mid.Y - end.Y)) + (b * (end.Y - start.Y)) + (c * (start.Y - mid.Y))) / denominator,
                ((a * (end.X - mid.X)) + (b * (start.X - end.X)) + (c * (mid.X - start.X))) / denominator);

            return true;
        }
    }
}
