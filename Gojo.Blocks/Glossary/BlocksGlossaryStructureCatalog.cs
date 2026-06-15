using Gojo.Core;
using GojoCAD;
using SkiaSharp;

namespace Gojo.Blocks;

internal static class BlocksGlossaryStructureCatalog
{
    public static IReadOnlyDictionary<string, BlocksGlossaryStructure> Create()
    {
        var structures = new Dictionary<string, BlocksGlossaryStructure>();
        AddSourceSupport(structures);
        AddDirectionArea(structures);
        AddCurves(structures);
        AddBranches(structures);
        return structures;
    }

    private static void AddSourceSupport(Dictionary<string, BlocksGlossaryStructure> structures)
    {
        var rows = new[]
        {
            ("radial-positive", GlossaryRow.RadialPositive),
            ("radial-inverse", GlossaryRow.RadialInverse),
            ("linear", GlossaryRow.Linear)
        };
        var columns = new[] { "site", "flow", "boundary", "partitioned", "orientation", "direction" };
        foreach (var (rowKey, row) in rows)
        foreach (var column in columns)
        {
            structures[$"source-support-{rowKey}-{column}"] = SourceSupport(row, column);
        }
    }

    private static void AddDirectionArea(Dictionary<string, BlocksGlossaryStructure> structures)
    {
        var rows = new[]
        {
            ("radial-positive", GlossaryRow.RadialPositive),
            ("radial-inverse", GlossaryRow.RadialInverse),
            ("linear", GlossaryRow.Linear)
        };
        var columns = new[]
        {
            ("directed-boundary", "directed-boundary"),
            ("boundary-support", "boundary-support"),
            ("directed-support", "directed-support"),
            ("area", "area"),
            ("directed-area", "directed-area"),
            ("bounded-area", "bounded-area")
        };
        foreach (var (rowKey, row) in rows)
        foreach (var (columnKey, column) in columns)
        {
            structures[$"direction-area-{rowKey}-{columnKey}"] = DirectionArea(row, column);
        }
    }

    private static void AddCurves(Dictionary<string, BlocksGlossaryStructure> structures)
    {
        var rows = new[]
        {
            ("line-line", CurveJoinKind.LineLine),
            ("line-arc", CurveJoinKind.LineArc),
            ("arc-arc", CurveJoinKind.ArcArc)
        };
        var columns = new[]
        {
            ("corner", CurveColumn.Corner),
            ("arc-blend", CurveColumn.ArcBlend),
            ("bezier", CurveColumn.Bezier),
            ("clothoid", CurveColumn.Clothoid),
            ("involute", CurveColumn.Involute),
            ("plotted", CurveColumn.Plotted)
        };
        foreach (var (rowKey, row) in rows)
        foreach (var (columnKey, column) in columns)
        {
            structures[$"curves-{rowKey}-{columnKey}"] = Curve(row, column);
        }
    }

    private static void AddBranches(Dictionary<string, BlocksGlossaryStructure> structures)
    {
        var rows = new[] { "tip", "middle", "structure" };
        var columns = new[] { "branch", "merge", "orthogonal-joint", "other" };
        foreach (var row in rows)
        foreach (var column in columns)
        {
            structures[$"branches-{row}-{column}"] = Branch(row, column);
        }
    }

    private static BlocksGlossaryStructure SourceSupport(GlossaryRow row, string column)
    {
        var inverse = row == GlossaryRow.RadialInverse;
        var linear = row == GlossaryRow.Linear;
        return column switch
        {
            "site" when linear => Spec(
                LinearSites(),
                "P(A) + P(B), with A and B allowed to coincide"),
            "site" => Spec(
                Site(),
                "P(O)"),
            "flow" when linear => Spec(
                LinearFlow(),
                "S(C,0,0) + flow_perp(S(C,0,0))"),
            "flow" => Spec(
                RadialFlow(),
                "P(O) + flow_radial(P(O))",
                inverse ? "The current Gojo outlet structure records an unbounded orthogonal flow from the site. The inward/outward face is still represented as tutorial sense rather than a separate settled radial-flow relation." : null),
            "boundary" when linear => Spec(
                LineSegment(),
                "P(A) + P(B) -> S(A,B)"),
            "boundary" => Spec(
                CircleBoundary(includeRadius: true),
                "P(O) + P(R) -> Circle(O,R)"),
            "partitioned" when linear => Spec(
                PartitionedSegment(),
                "S(A,B) + P(t1) + P(t2) + ..."),
            "partitioned" => Spec(
                PartitionedCircle(),
                "Circle(O,R) + P(theta1) + P(theta2) + ..."),
            "orientation" when linear => Spec(
                DirectedLine(inverse),
                inverse ? "orientation: left is positive; D(S(C,+u,-u))" : "orientation: right is positive; D(S(C,-u,+u))",
                "This is a local stand-in for orientation. A canonical orientation surface should eventually say which direction is positive without requiring a directed segment to carry that whole meaning."),
            "orientation" => Spec(
                DirectedCircle(inverse),
                inverse ? "orientation: clockwise is positive; D(Circle(O,R), +theta -> -theta)" : "orientation: counterclockwise is positive; D(Circle(O,R), -theta -> +theta)",
                "This uses a directed span on a circular carrier as orientation evidence. A dedicated orientation carrier/surface would make this cleaner."),
            "direction" when linear => Spec(
                DirectionOnLine(inverse),
                inverse ? "P(0) + P(-u), read under left-positive orientation" : "P(0) + P(+u), read under right-positive orientation"),
            "direction" => Spec(
                DirectionOnCircle(inverse),
                inverse ? "P(theta0) + P(-theta), read under clockwise orientation" : "P(theta0) + P(+theta), read under counterclockwise orientation"),
            _ => Spec(Site(), "P(O)")
        };
    }

    private static BlocksGlossaryStructure DirectionArea(GlossaryRow row, string column)
    {
        var inverse = row == GlossaryRow.RadialInverse;
        var linear = row == GlossaryRow.Linear;
        return column switch
        {
            "directed-boundary" when linear => Spec(
                DirectedLine(inverse),
                inverse ? "D(S(C,+u,-u))" : "D(S(C,-u,+u))"),
            "directed-boundary" => Spec(
                DirectedCircle(inverse),
                inverse ? "D(Circle(O,R), +theta -> -theta)" : "D(Circle(O,R), -theta -> +theta)"),
            "boundary-support" when linear => Spec(
                LinearFlow(),
                "S(C,0,0) + flow_perp(S(C,0,0))"),
            "boundary-support" => Spec(
                CircleArea(inverse),
                inverse ? "Area*(Circle(O,R))" : "Area(Circle(O,R))"),
            "directed-support" when linear => Spec(
                DirectedLinearArea(inverse),
                "Area(S(A,B) + side support) + D(S(A,B))",
                "Linear side support is represented as filled boundary plus a directed edge. A direct side-face support carrier would be a better eventual encoding."),
            "directed-support" => Spec(
                DirectedCircleArea(inverse),
                "Area(Circle(O,R)) + D(Circle edge)"),
            "area" when linear => Spec(
                RectangleArea(inverse),
                inverse ? "Area*(closed S-boundary)" : "Area(closed S-boundary)"),
            "area" => Spec(
                CircleArea(inverse),
                inverse ? "Area*(Circle(O,R))" : "Area(Circle(O,R))"),
            "directed-area" when linear => Spec(
                DirectedLinearArea(inverse),
                "Area(closed S-boundary) + face D(edge...)",
                "Face circulation is currently expressed by pairing area fill with directed boundary evidence. It is not yet its own settled face-spin surface."),
            "directed-area" => Spec(
                DirectedCircleArea(inverse),
                "Area(Circle(O,R)) + face D(Circle edge)",
                "Face spin is currently represented as an area plus directed boundary evidence. A dedicated directed-area/face-circulation read would be cleaner."),
            "bounded-area" when linear => Spec(
                BoundedRectangleArea(inverse),
                "Area(boundary) + S-boundary + P(tick...)"),
            "bounded-area" => Spec(
                BoundedCircleArea(inverse),
                "Area(Circle(O,R)) + P(O) + S(O,R) + P(theta...)"),
            _ => Spec(Site(), "P(O)")
        };
    }

    private static BlocksGlossaryStructure Curve(CurveJoinKind row, CurveColumn column)
    {
        var frame = new FrameBasis();
        var lineA = GraphicStructure.FrameSegment(frame.Point(-48, 12), frame.Point(-8, 12));
        var lineB = GraphicStructure.FrameSegment(frame.Point(8, -10), frame.Point(48, -10));
        var arcA = GraphicStructure.FrameArc(frame.Point(-18, 8), frame.Point(-48, 8), frame.Point(-10, -24));
        var arcB = GraphicStructure.FrameArc(frame.Point(22, -2), frame.Point(8, 22), frame.Point(50, 0));
        var incoming = row switch
        {
            CurveJoinKind.LineArc => lineA,
            CurveJoinKind.ArcArc => arcA,
            _ => lineA
        };
        var outgoing = row switch
        {
            CurveJoinKind.LineLine => lineB,
            CurveJoinKind.LineArc => arcB,
            _ => arcB
        };

        return column switch
        {
            CurveColumn.Corner => Spec(
                Group(incoming, outgoing, GraphicStructure.Pin(incoming, outgoing)),
                "carrier A + carrier B + shared P(J)"),
            CurveColumn.ArcBlend => Spec(
                Group(incoming, outgoing, GraphicStructure.FrameArc(frame.Point(0, 4), frame.Point(-8, 12), frame.Point(8, -10))),
                "carrier A + carrier B + Arc(P0,P1) as tangent bridge",
                "The current bridge is explicit arc evidence. The future read should derive the smoothing relation from carrier tangency and requested settlement."),
            CurveColumn.Bezier => Spec(
                Group(incoming, outgoing, GraphicStructure.FrameCubicCurve(frame.Point(-8, 12), frame.Point(0, 24), frame.Point(10, -22), frame.Point(26, -10))),
                "carrier A + carrier B + Bezier(P0,H0,H1,P1)"),
            CurveColumn.Clothoid => Spec(
                Group(incoming, outgoing, GraphicStructure.FrameClothoidTransition(GraphicStructure.FrameCurveTransition(incoming, outgoing), GraphicStructure.FrameSegment(frame.Point(-18, -34), frame.Point(28, -34)))),
                "carrier A -> carrier B with curvature ramp over S(L,0,n)",
                "The current clothoid surface is transition plus length/grade evidence; it does not yet solve a true curvature-continuity path from the carrier pair."),
            CurveColumn.Involute => Spec(
                Group(incoming, outgoing, GraphicStructure.FrameInvoluteCurve(arcA, GraphicStructure.FrameSegment(frame.Point(-6, -34), frame.Point(34, -18)))),
                "Arc(base) + unwound tangent S(T,0,n)",
                "Involute generation has a structural foothold, but the exact unwrap relationship still needs richer inspection vocabulary."),
            CurveColumn.Plotted => Spec(
                PlottedPath(frame),
                "P0 + P1 + P2 + ... with S(Pi,Pi+1) samples",
                "This is intentionally a coordinate-sampled contrast case. It is Gojo structure, but it is not a carrier-tangency join."),
            _ => Spec(Group(incoming, outgoing), "carrier A + carrier B")
        };
    }

    private static BlocksGlossaryStructure Branch(string row, string column)
    {
        var frame = new FrameBasis();
        return (row, column) switch
        {
            ("tip", "branch") => Spec(
                Group(
                    GraphicStructure.FrameSegment(frame.Point(-46, 0), frame.Point(0, 0)),
                    GraphicStructure.FrameSegment(frame.Point(0, 0), frame.Point(42, -24)),
                    GraphicStructure.FrameSegment(frame.Point(0, 0), frame.Point(42, 24))),
                "S(A,J) + S(J,B) + S(J,C)"),
            ("tip", "merge") => Spec(
                Group(
                    GraphicStructure.FrameSegment(frame.Point(-42, -24), frame.Point(0, 0)),
                    GraphicStructure.FrameSegment(frame.Point(-42, 24), frame.Point(0, 0)),
                    GraphicStructure.FrameSegment(frame.Point(0, 0), frame.Point(46, 0))),
                "S(A,J) + S(B,J) + S(J,C)"),
            ("tip", "orthogonal-joint") => Spec(
                Group(
                    GraphicStructure.FrameSegment(frame.Point(-42, 0), frame.Point(0, 0)),
                    GraphicStructure.FrameSegment(frame.Point(0, 0), frame.Point(0, -34)),
                    GraphicStructure.FrameSegment(frame.Point(0, 0), frame.Point(38, 26))),
                "S(A,J) + S(J,O) + S(J,C)"),
            ("tip", "other") => Spec(
                Group(
                    GraphicStructure.FrameSegment(frame.Point(-44, 8), frame.Point(-8, 8)),
                    GraphicStructure.FrameArc(frame.Point(0, 2), frame.Point(-8, 8), frame.Point(38, -18))),
                "S(A,J) + Arc(J,B)"),
            ("middle", "branch") => Spec(
                Group(
                    GraphicStructure.FrameSegment(frame.Point(-48, 0), frame.Point(48, 0)),
                    GraphicStructure.FrameSegment(frame.Point(0, 0), frame.Point(0, -36))),
                "S(A,B) + S(M,C), where M lies on S(A,B)"),
            ("middle", "merge") => Spec(
                Group(
                    GraphicStructure.FrameSegment(frame.Point(-48, 0), frame.Point(48, 0)),
                    GraphicStructure.FrameSegment(frame.Point(0, -34), frame.Point(0, 0)),
                    GraphicStructure.FrameSegment(frame.Point(0, 34), frame.Point(0, 0))),
                "S(A,B) + S(C,M) + S(D,M), where M lies on S(A,B)"),
            ("middle", "orthogonal-joint") => Spec(
                Group(
                    GraphicStructure.FrameSegment(frame.Point(-48, 0), frame.Point(48, 0)),
                    GraphicStructure.FrameArc(frame.Point(-16, -18), frame.Point(-30, -36), frame.Point(0, 0))),
                "S(A,B) + Arc(C,M), where M lies on S(A,B)"),
            ("middle", "other") => Spec(
                Group(
                    GraphicStructure.FrameSegment(frame.Point(-48, 0), frame.Point(48, 0)),
                    GraphicStructure.FrameSegment(frame.Point(0, -34), frame.Point(0, 34))),
                "S(A,B) + S(C,D), with no shared P(J)",
                "The crossing is just two carriers in the same visual region. There is no Pin/Junction relation, so the structure does not settle into a join."),
            ("structure", "branch") => Spec(
                MultiBranch(frame),
                "S(J,A) + S(J,B) + S(J,C) + ..."),
            ("structure", "merge") => Spec(
                BranchMergeNetwork(frame),
                "S(A,J) + S(J,B) + S(J,C) + S(B,K) + S(C,K)"),
            ("structure", "orthogonal-joint") => Spec(
                CurvedAreaWithLooseBranch(frame, partial: false),
                "Area(S + S + Arc + S) + loose S(U,V)",
                "A curved closed area is expressible. The unrelated branch line is expressible as separate structure, but the tutorial still uses wording to emphasize non-participation."),
            ("structure", "other") => Spec(
                CurvedAreaWithLooseBranch(frame, partial: true),
                "partial Area(S + S + Arc + ?open) + loose S(U,V)",
                "A poorly bounded U-area is not yet a settled Gojo area. This example keeps the open side as tension-like residue while the renderer sketches the intended partial support."),
            _ => Spec(Site(), "P(O)")
        };
    }

    private static IElement Site()
    {
        var frame = new FrameBasis();
        return frame.Point(0, 0);
    }

    private static IElement LinearSites()
    {
        var frame = new FrameBasis();
        return Group(frame.Point(0, 0), frame.Point(0, 0));
    }

    private static IElement RadialFlow()
    {
        var carrier = new Carrier();
        var point = GraphicStructure.Point(carrier, 0);
        return Group(point, GraphicStructure.RadialFlow(point));
    }

    private static IElement LinearFlow()
    {
        var carrier = new Carrier();
        var segment = GraphicStructure.Segment(carrier, 0, 0);
        return Group(segment, GraphicStructure.RadialFlow(segment));
    }

    private static IElement LineSegment()
    {
        var frame = new FrameBasis();
        return GraphicStructure.FrameSegment(frame.Point(-44, 0), frame.Point(44, 0));
    }

    private static IElement PartitionedSegment()
    {
        var frame = new FrameBasis();
        return Group(
            GraphicStructure.FrameSegment(frame.Point(-46, 0), frame.Point(46, 0)),
            frame.Point(-24, 0),
            frame.Point(0, 0),
            frame.Point(24, 0));
    }

    private static IElement DirectedLine(bool inverse)
    {
        var carrier = new Carrier();
        return Group(
            GraphicStructure.Point(carrier, 0),
            inverse
                ? GraphicStructure.DirectedSegment(carrier, 40, -40)
                : GraphicStructure.DirectedSegment(carrier, -40, 40));
    }

    private static IElement DirectionOnLine(bool inverse)
    {
        var carrier = new Carrier();
        return Group(
            GraphicStructure.Point(carrier, 0),
            GraphicStructure.Point(carrier, inverse ? -32 : 32),
            inverse
                ? GraphicStructure.DirectedSegment(carrier, 32, -32)
                : GraphicStructure.DirectedSegment(carrier, -32, 32));
    }

    private static IElement CircleBoundary(bool includeRadius)
    {
        var frame = new FrameBasis();
        var center = frame.Point(0, 0);
        var radius = frame.Point(34, 0);
        return includeRadius
            ? Group(GraphicStructure.FrameCircle(center, radius), center, radius, GraphicStructure.Pin(center, radius))
            : GraphicStructure.FrameCircle(center, radius);
    }

    private static IElement PartitionedCircle()
    {
        var frame = new FrameBasis();
        var center = frame.Point(0, 0);
        return Group(
            GraphicStructure.FrameCircle(center, frame.Point(34, 0)),
            frame.Point(0, 34),
            frame.Point(-34, 0),
            frame.Point(0, -34),
            frame.Point(24, 24),
            frame.Point(-24, 24),
            frame.Point(-24, -24),
            frame.Point(24, -24));
    }

    private static IElement DirectedCircle(bool inverse)
    {
        var carrier = new Carrier();
        var origin = GraphicStructure.Point(carrier, 0);
        var circle = GraphicStructure.CircularCarrierAt(origin);
        return Group(
            origin,
            GraphicStructure.Point(circle, 0),
            GraphicStructure.Point(circle, 24),
            GraphicStructure.Point(circle, 48),
            inverse
                ? GraphicStructure.DirectedSegment(circle, 44, 4)
                : GraphicStructure.DirectedSegment(circle, 4, 44));
    }

    private static IElement DirectionOnCircle(bool inverse)
    {
        var carrier = new Carrier();
        var origin = GraphicStructure.Point(carrier, 0);
        var circle = GraphicStructure.CircularCarrierAt(origin);
        return Group(
            origin,
            GraphicStructure.Point(circle, 0),
            GraphicStructure.Point(circle, inverse ? -32 : 32),
            inverse
                ? GraphicStructure.DirectedSegment(circle, 32, -32)
                : GraphicStructure.DirectedSegment(circle, -32, 32));
    }

    private static IElement CircleArea(bool inverse)
    {
        var frame = new FrameBasis();
        var circle = GraphicStructure.FrameCircle(frame.Point(0, 0), frame.Point(34, 0));
        var area = inverse
            ? GraphicStructure.AntiAreaFill(circle)
            : GraphicStructure.AreaFill(circle);
        return Group(area, circle);
    }

    private static IElement DirectedCircleArea(bool inverse)
    {
        var frame = new FrameBasis();
        var circle = GraphicStructure.FrameCircle(frame.Point(0, 0), frame.Point(34, 0));
        var area = inverse
            ? GraphicStructure.AntiAreaFill(circle)
            : GraphicStructure.AreaFill(circle);
        return Group(area, circle, DirectedCircle(inverse));
    }

    private static IElement BoundedCircleArea(bool inverse)
    {
        var frame = new FrameBasis();
        var center = frame.Point(0, 0);
        var radius = frame.Point(34, 0);
        var circle = GraphicStructure.FrameCircle(center, radius);
        var area = inverse
            ? GraphicStructure.AntiAreaFill(circle)
            : GraphicStructure.AreaFill(circle);
        return Group(area, circle, center, radius, PartitionedCircle());
    }

    private static IElement RectangleArea(bool inverse)
    {
        var frame = new FrameBasis();
        var boundary = RectangleSegments(frame, -34, -22, 34, 22);
        var area = inverse
            ? GraphicStructure.AntiAreaFill(boundary)
            : GraphicStructure.AreaFill(boundary);
        return Group(area, Group(boundary));
    }

    private static IElement DirectedLinearArea(bool inverse)
    {
        var frame = new FrameBasis();
        var boundary = RectangleSegments(frame, -34, -22, 34, 22);
        return Group(
            inverse ? GraphicStructure.AntiAreaFill(boundary) : GraphicStructure.AreaFill(boundary),
            Group(boundary),
            DirectedLine(inverse));
    }

    private static IElement BoundedRectangleArea(bool inverse)
    {
        var frame = new FrameBasis();
        var boundary = RectangleSegments(frame, -34, -22, 34, 22);
        return Group(
            inverse ? GraphicStructure.AntiAreaFill(boundary) : GraphicStructure.AreaFill(boundary),
            Group(boundary),
            frame.Point(-12, -22),
            frame.Point(12, -22),
            frame.Point(-12, 22),
            frame.Point(12, 22));
    }

    private static IElement PlottedPath(FrameBasis frame)
    {
        var a = frame.Point(-44, 24);
        var b = frame.Point(-24, -6);
        var c = frame.Point(0, 10);
        var d = frame.Point(24, -18);
        var e = frame.Point(46, 16);
        return Group(
            a,
            b,
            c,
            d,
            e,
            GraphicStructure.FrameSegment(a, b),
            GraphicStructure.FrameSegment(b, c),
            GraphicStructure.FrameSegment(c, d),
            GraphicStructure.FrameSegment(d, e));
    }

    private static IElement MultiBranch(FrameBasis frame)
    {
        var hub = frame.Point(0, 0);
        return Group(
            GraphicStructure.FrameSegment(hub, frame.Point(-42, 0)),
            GraphicStructure.FrameSegment(hub, frame.Point(36, -30)),
            GraphicStructure.FrameSegment(hub, frame.Point(44, -8)),
            GraphicStructure.FrameSegment(hub, frame.Point(40, 18)),
            GraphicStructure.FrameSegment(hub, frame.Point(18, 36)));
    }

    private static IElement BranchMergeNetwork(FrameBasis frame)
    {
        var a = frame.Point(-50, 0);
        var b = frame.Point(-18, 0);
        var c = frame.Point(18, -24);
        var d = frame.Point(18, 24);
        var e = frame.Point(50, 0);
        return Group(
            GraphicStructure.FrameSegment(a, b),
            GraphicStructure.FrameSegment(b, c),
            GraphicStructure.FrameSegment(b, d),
            GraphicStructure.FrameSegment(c, e),
            GraphicStructure.FrameSegment(d, e));
    }

    private static IElement CurvedAreaWithLooseBranch(FrameBasis frame, bool partial)
    {
        var a = frame.Point(-34, -24);
        var b = frame.Point(-34, 24);
        var c = frame.Point(20, 24);
        var d = frame.Point(20, -24);
        var arc = GraphicStructure.FrameArc(frame.Point(28, 0), c, d);
        var left = GraphicStructure.FrameSegment(a, b);
        var bottom = GraphicStructure.FrameSegment(b, c);
        var top = GraphicStructure.FrameSegment(d, a);
        var loose = GraphicStructure.FrameSegment(frame.Point(-46, -34), frame.Point(46, -44));
        if (partial)
        {
            var tension = TensionSlot();
            return Group(left, bottom, arc, top, loose, tension);
        }

        var boundary = new IElement[] { left, bottom, arc, top };
        return Group(GraphicStructure.AreaFill(boundary), Group(boundary), loose);
    }

    private static IElement[] RectangleSegments(FrameBasis frame, long left, long top, long right, long bottom)
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

    private static Relation Group(params IElement[] elements) =>
        Relation.OrderedGroup(new Carrier(), elements);

    private static Relation TensionSlot()
    {
        var slot = new Placeholder();
        return new Relation(slot, slot.Dual());
    }

    private static BlocksGlossaryStructure Spec(
        IElement structure,
        string expression,
        string? encodingNote = null,
        SKRect? bounds = null,
        int seed = 0) =>
        new(structure, bounds ?? Bounds(-62, -48, 62, 48), seed, expression, encodingNote);

    private static SKRect Bounds(float left, float top, float right, float bottom) =>
        new(left, top, right, bottom);

    private enum GlossaryRow
    {
        RadialPositive,
        RadialInverse,
        Linear
    }

    private enum CurveJoinKind
    {
        LineLine,
        LineArc,
        ArcArc
    }

    private enum CurveColumn
    {
        Corner,
        ArcBlend,
        Bezier,
        Clothoid,
        Involute,
        Plotted
    }

    private sealed class FrameBasis
    {
        private readonly Carrier frame = new();
        private readonly Carrier x = new();
        private readonly Carrier y = new();

        public Relation Point(long px, long py) =>
            GraphicStructure.FramePoint(frame, new CarrierLocation(x, px), new CarrierLocation(y, py));
    }
}
