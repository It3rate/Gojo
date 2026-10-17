using Gojo.Core;
using GojoCAD;

namespace Gojo.Blocks;

internal static class PcbDataGlossaryGeometry
{
    public readonly record struct ModelPoint(long X, long Y);

    public sealed record TraceRead(
        string Key,
        string Net,
        string Layer,
        long WidthTicks,
        long CostTicks,
        Relation Path,
        IReadOnlyList<IElement> Sections,
        Relation DataAttachment,
        Relation RenderRead,
        Relation RuleRead);

    public sealed record PadRead(
        string Key,
        string Net,
        Relation Center,
        Relation Circle);

    public sealed record StageRead(
        Relation Schematic,
        Relation BoardOutline,
        Relation Footprints,
        Relation LayoutCarriers,
        Relation DataAttachments,
        Relation RenderRead,
        Relation RuleCheck,
        Relation PhysicalRead,
        IReadOnlyList<TraceRead> Traces,
        IReadOnlyList<PadRead> Pads);

    public static StageRead CreateStage()
    {
        var frame = new FrameBasis();
        var netAxis = new Carrier();
        var layerAxis = new Carrier();
        var widthAxis = new Carrier();
        var costAxis = new Carrier();
        var ruleAxis = new Carrier();
        var physicalAxis = new Carrier();

        var board = BoardOutline(frame);
        var footprints = Footprints(frame);
        var pads = Pads(frame);
        var traces = new[]
        {
            Trace(
                frame,
                "vcc",
                "VCC",
                "TOP",
                8,
                2,
                netAxis,
                layerAxis,
                widthAxis,
                costAxis,
                ruleAxis,
                NetTicks.Vcc,
                LayerTicks.Top,
                [new ModelPoint(-92, 42), new ModelPoint(-55, 42), new ModelPoint(-22, 22)],
                [new ModelPoint(-38, 38)]),
            Trace(
                frame,
                "sig",
                "SIG",
                "TOP",
                5,
                4,
                netAxis,
                layerAxis,
                widthAxis,
                costAxis,
                ruleAxis,
                NetTicks.Signal,
                LayerTicks.Top,
                [new ModelPoint(-92, 0), new ModelPoint(-54, 0), new ModelPoint(-22, 0)],
                [new ModelPoint(-40, -18)]),
            Trace(
                frame,
                "gnd",
                "GND",
                "BOT",
                7,
                3,
                netAxis,
                layerAxis,
                widthAxis,
                costAxis,
                ruleAxis,
                NetTicks.Ground,
                LayerTicks.Bottom,
                [new ModelPoint(-92, -42), new ModelPoint(-58, -42), new ModelPoint(-22, -22)],
                [new ModelPoint(-38, -44)]),
            Trace(
                frame,
                "led",
                "LED",
                "TOP",
                5,
                3,
                netAxis,
                layerAxis,
                widthAxis,
                costAxis,
                ruleAxis,
                NetTicks.Led,
                LayerTicks.Top,
                [new ModelPoint(42, 22), new ModelPoint(78, 22)],
                []),
            Trace(
                frame,
                "return",
                "GND",
                "BOT",
                6,
                5,
                netAxis,
                layerAxis,
                widthAxis,
                costAxis,
                ruleAxis,
                NetTicks.Ground,
                LayerTicks.Bottom,
                [new ModelPoint(42, -22), new ModelPoint(58, -42), new ModelPoint(78, -22)],
                [new ModelPoint(66, -44)])
        };

        var schematic = Schematic(
            netAxis,
            traces,
            Group(pads.Select(pad => pad.Center).Cast<IElement>().ToArray()));
        var traceGroup = Group(traces.Select(trace => trace.Path).Cast<IElement>().ToArray());
        var padGroup = Group(pads.Select(pad => pad.Circle).Cast<IElement>().ToArray());
        var layout = Group(board, footprints, padGroup, traceGroup);
        var data = Group(traces.Select(trace => trace.DataAttachment).Cast<IElement>().ToArray());
        var renderBasis = Group(
            Relation.Point(layerAxis, LayerTicks.Top),
            Relation.Point(layerAxis, LayerTicks.Bottom),
            Relation.Segment(widthAxis, 0, 10));
        var renderRead = new Relation(layout, Group(data, renderBasis, Group(traces.Select(trace => trace.RenderRead).Cast<IElement>().ToArray())));
        var ruleCheck = new Relation(
            layout,
            Group(
                data,
                Relation.Segment(ruleAxis, 0, 3),
                Group(traces.Select(trace => trace.RuleRead).Cast<IElement>().ToArray()),
                TensionSlot()));
        var physicalRead = new Relation(
            renderRead,
            Group(
                Relation.Point(physicalAxis, 1),
                board,
                padGroup,
                traceGroup,
                data));

        return new StageRead(
            schematic,
            board,
            footprints,
            layout,
            data,
            renderRead,
            ruleCheck,
            physicalRead,
            traces,
            pads);
    }

    private static TraceRead Trace(
        FrameBasis frame,
        string key,
        string net,
        string layer,
        long widthTicks,
        long costTicks,
        Carrier netAxis,
        Carrier layerAxis,
        Carrier widthAxis,
        Carrier costAxis,
        Carrier ruleAxis,
        long netTicks,
        long layerTicks,
        IReadOnlyList<ModelPoint> routePoints,
        IReadOnlyList<ModelPoint> controls)
    {
        var points = routePoints.Select(frame.Point).ToArray();
        var sections = new List<IElement>();
        if (points.Length == 2)
        {
            sections.Add(GraphicStructure.FrameSegment(points[0], points[1]));
        }
        else if (points.Length >= 3)
        {
            sections.Add(GraphicStructure.FrameSegment(points[0], points[1]));
            sections.Add(GraphicStructure.FrameQuadraticCurve(points[1], frame.Point(controls[0]), points[2]));
        }

        var path = Chain(sections);
        var traceHolder = new Carrier(UnitShape.DescribedAtom(path));
        var netPoint = Relation.Point(netAxis, netTicks);
        var layerPoint = Relation.Point(layerAxis, layerTicks);
        var width = Relation.Segment(widthAxis, 0, widthTicks);
        var cost = Relation.Point(costAxis, costTicks);
        var data = Relation.OrderedGroup(
            traceHolder,
            path,
            new Relation(path, netPoint),
            new Relation(path, layerPoint),
            new Relation(path, width),
            new Relation(path, cost));
        var render = new Relation(data, Relation.OrderedGroup(new Carrier(), path, layerPoint, width));
        var rule = new Relation(data, Relation.OrderedGroup(new Carrier(), Relation.Point(ruleAxis, 1), Relation.Point(ruleAxis, widthTicks >= 5 ? 1 : 0)));
        return new TraceRead(key, net, layer, widthTicks, costTicks, path, sections, data, render, rule);
    }

    private static Relation Schematic(Carrier netAxis, IReadOnlyList<TraceRead> traces, IElement pads)
    {
        var j1 = new Carrier();
        var u1 = new Carrier();
        var d1 = new Carrier();
        var ports = Group(
            Relation.Point(j1, 1),
            Relation.Point(j1, 2),
            Relation.Point(j1, 3),
            Relation.Point(u1, 1),
            Relation.Point(u1, 2),
            Relation.Point(u1, 3),
            Relation.Point(d1, 1),
            Relation.Point(d1, 2));
        var nets = Group(
            new Relation(Relation.Point(netAxis, NetTicks.Vcc), Group(Relation.Point(j1, 1), Relation.Point(u1, 1))),
            new Relation(Relation.Point(netAxis, NetTicks.Signal), Group(Relation.Point(j1, 2), Relation.Point(u1, 2), Relation.Point(d1, 1))),
            new Relation(Relation.Point(netAxis, NetTicks.Ground), Group(Relation.Point(j1, 3), Relation.Point(u1, 3), Relation.Point(d1, 2))));
        return Group(ports, nets, pads, Group(traces.Select(trace => trace.DataAttachment).Cast<IElement>().ToArray()));
    }

    private static Relation BoardOutline(FrameBasis frame)
    {
        var a = frame.Point(-108, 68);
        var b = frame.Point(108, 68);
        var c = frame.Point(108, -68);
        var d = frame.Point(-108, -68);
        return Chain(
        [
            GraphicStructure.FrameSegment(a, b),
            GraphicStructure.FrameSegment(b, c),
            GraphicStructure.FrameSegment(c, d),
            GraphicStructure.FrameSegment(d, a)
        ]);
    }

    private static Relation Footprints(FrameBasis frame) =>
        Group(
            Chain(Rectangle(frame, -22, 32, 42, -32)),
            Chain(Rectangle(frame, -102, 54, -82, -54)),
            GraphicStructure.FrameCircle(frame.Point(78, 22), frame.Point(88, 22)),
            GraphicStructure.FrameCircle(frame.Point(78, -22), frame.Point(88, -22)));

    private static IReadOnlyList<PadRead> Pads(FrameBasis frame)
    {
        static PadRead Pad(FrameBasis frame, string key, string net, ModelPoint center, long radius)
        {
            var c = frame.Point(center);
            return new PadRead(key, net, c, GraphicStructure.FrameCircle(c, frame.Point(new ModelPoint(center.X + radius, center.Y))));
        }

        return
        [
            Pad(frame, "j1-vcc", "VCC", new ModelPoint(-92, 42), 8),
            Pad(frame, "j1-sig", "SIG", new ModelPoint(-92, 0), 8),
            Pad(frame, "j1-gnd", "GND", new ModelPoint(-92, -42), 8),
            Pad(frame, "u1-vcc", "VCC", new ModelPoint(-22, 22), 6),
            Pad(frame, "u1-sig", "SIG", new ModelPoint(-22, 0), 6),
            Pad(frame, "u1-gnd", "GND", new ModelPoint(-22, -22), 6),
            Pad(frame, "u1-led", "LED", new ModelPoint(42, 22), 6),
            Pad(frame, "u1-ret", "GND", new ModelPoint(42, -22), 6),
            Pad(frame, "d1-led", "LED", new ModelPoint(78, 22), 8),
            Pad(frame, "d1-gnd", "GND", new ModelPoint(78, -22), 8)
        ];
    }

    private static IElement[] Rectangle(FrameBasis frame, long left, long top, long right, long bottom)
    {
        var a = frame.Point(left, top);
        var b = frame.Point(right, top);
        var c = frame.Point(right, bottom);
        var d = frame.Point(left, bottom);
        return
        [
            GraphicStructure.FrameSegment(a, b),
            GraphicStructure.FrameSegment(b, c),
            GraphicStructure.FrameSegment(c, d),
            GraphicStructure.FrameSegment(d, a)
        ];
    }

    private static Relation Chain(IReadOnlyList<IElement> elements)
    {
        if (elements.Count == 0)
        {
            return Relation.Point(Carrier.Unknown, 0);
        }

        IElement structure = elements[^1];
        for (var index = elements.Count - 2; index >= 0; index--)
        {
            structure = new Relation(elements[index], structure);
        }

        return structure as Relation ?? new Relation(structure, structure);
    }

    private static Relation Group(params IElement[] elements) =>
        Relation.OrderedGroup(new Carrier(), elements);

    private static Relation TensionSlot()
    {
        var slot = new Placeholder();
        return new Relation(slot, slot.Dual());
    }

    private static class NetTicks
    {
        public const long Vcc = 1;
        public const long Signal = 2;
        public const long Ground = 3;
        public const long Led = 4;
    }

    private static class LayerTicks
    {
        public const long Top = 1;
        public const long Bottom = -1;
    }

    private sealed class FrameBasis
    {
        private readonly Carrier frame = new();
        private readonly Carrier x = new();
        private readonly Carrier y = new();

        public Relation Point(long pointX, long pointY) =>
            Point(new ModelPoint(pointX, pointY));

        public Relation Point(ModelPoint point) =>
            GraphicStructure.FramePoint(frame, new CarrierLocation(x, point.X), new CarrierLocation(y, point.Y));
    }
}
