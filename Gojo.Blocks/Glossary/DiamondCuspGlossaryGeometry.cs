using Gojo.Core;
using GojoCAD;

namespace Gojo.Blocks;

internal static class DiamondCuspGlossaryGeometry
{
    public readonly record struct ModelPoint(long X, long Y);

    private readonly record struct ModelVector(double X, double Y);

    public enum EdgeKind
    {
        Segment,
        Arc,
        Quadratic
    }

    public readonly record struct Edge(EdgeKind Kind, ModelPoint Start, ModelPoint End, ModelPoint Center);

    public sealed record BoundaryRead(
        Relation Outline,
        IReadOnlyList<IElement> Sections,
        IReadOnlyList<Relation> Cusps,
        IReadOnlyList<IElement> Joins,
        IReadOnlyList<IElement> AngleBases);

    public sealed record StageRead(BoundaryRead Outer, BoundaryRead Inner);

    public static ModelPoint Top => new(0, 70);

    public static ModelPoint Right => new(54, 2);

    public static ModelPoint Bottom => new(0, -70);

    public static ModelPoint Left => new(-54, 2);

    public static ModelPoint RightArcCenter => new(75, -70);

    public static ModelPoint LeftArcCenter => new(-75, -70);

    public static ModelPoint InnerLeft => new(-24, 8);

    public static ModelPoint InnerRight => new(24, 8);

    public static ModelPoint InnerArcCenter => new(0, 8);

    public static IReadOnlyList<ModelPoint> Cusps =>
    [
        Top,
        Right,
        Bottom,
        Left
    ];

    public static IReadOnlyList<Edge> SourceEdges =>
    [
        new(EdgeKind.Segment, Top, Right, default),
        new(EdgeKind.Arc, Right, Bottom, RightArcCenter),
        new(EdgeKind.Arc, Bottom, Left, LeftArcCenter),
        new(EdgeKind.Segment, Left, Top, default)
    ];

    public static BoundaryRead CreateSource()
    {
        var frame = new FrameBasis();
        return CreateSource(frame);
    }

    public static BoundaryRead CreateRounded()
    {
        var frame = new FrameBasis();
        return CreateRounded(frame);
    }

    public static BoundaryRead CreateInnerSource()
    {
        var frame = new FrameBasis();
        return CreateInnerSource(frame);
    }

    public static BoundaryRead CreateInnerRounded()
    {
        var frame = new FrameBasis();
        return CreateInnerRounded(frame);
    }

    public static StageRead CreateStage(bool roundOuter, bool roundInner)
    {
        var frame = new FrameBasis();
        var outer = roundOuter ? CreateRounded(frame) : CreateSource(frame);
        var inner = roundInner ? CreateInnerRounded(frame) : CreateInnerSource(frame);
        return new StageRead(outer, inner);
    }

    private static BoundaryRead CreateSource(FrameBasis frame)
    {
        var top = frame.Point(Top);
        var right = frame.Point(Right);
        var bottom = frame.Point(Bottom);
        var left = frame.Point(Left);
        var rightCenter = frame.Point(RightArcCenter);
        var leftCenter = frame.Point(LeftArcCenter);

        var topRight = GraphicStructure.FrameSegment(top, right);
        var rightBottom = GraphicStructure.FrameArc(rightCenter, right, bottom);
        var bottomLeft = GraphicStructure.FrameArc(leftCenter, bottom, left);
        var leftTop = GraphicStructure.FrameSegment(left, top);
        var sections = new IElement[] { topRight, rightBottom, bottomLeft, leftTop };
        var joins = new IElement[]
        {
            JoinAt(top, leftTop, topRight),
            JoinAt(right, topRight, rightBottom),
            JoinAt(bottom, rightBottom, bottomLeft),
            JoinAt(left, bottomLeft, leftTop)
        };
        var angleBases = new IElement[]
        {
            AngleBasis(top, 315, 45),
            AngleBasis(right, 135, 245),
            AngleBasis(bottom, 12, 168),
            AngleBasis(left, 295, 45)
        };

        return new BoundaryRead(Chain(sections), sections, [top, right, bottom, left], joins, angleBases);
    }

    private static BoundaryRead CreateRounded(FrameBasis frame)
    {
        var top = frame.Point(Top);
        var right = frame.Point(Right);
        var bottom = frame.Point(Bottom);
        var left = frame.Point(Left);
        var rightCenter = frame.Point(RightArcCenter);
        var leftCenter = frame.Point(LeftArcCenter);
        var topRightA = frame.Point(Lerp(Top, Right, 0.18f));
        var topRightB = frame.Point(Lerp(Right, Top, 0.20f));
        var rightArcStart = frame.Point(ArcPoint(RightArcCenter, Right, Bottom, 0.16f));
        var bottomRightTrim = frame.Point(ArcPoint(RightArcCenter, Right, Bottom, 0.72f));
        var bottomLeftTrim = frame.Point(ArcPoint(LeftArcCenter, Bottom, Left, 0.28f));
        var leftArcEnd = frame.Point(ArcPoint(LeftArcCenter, Bottom, Left, 0.84f));
        var leftTopA = frame.Point(Lerp(Left, Top, 0.20f));
        var leftTopB = frame.Point(Lerp(Top, Left, 0.18f));
        var rightControl = frame.Point(TangentIntersection(
            Lerp(Top, Right, 0.80f),
            Vector(Top, Right),
            ArcPoint(RightArcCenter, Right, Bottom, 0.16f),
            ArcTangent(RightArcCenter, Right, Bottom, 0.16f)));
        var bottomControl = frame.Point(TangentIntersection(
            ArcPoint(RightArcCenter, Right, Bottom, 0.72f),
            ArcTangent(RightArcCenter, Right, Bottom, 0.72f),
            ArcPoint(LeftArcCenter, Bottom, Left, 0.28f),
            ArcTangent(LeftArcCenter, Bottom, Left, 0.28f)));
        var leftControl = frame.Point(TangentIntersection(
            ArcPoint(LeftArcCenter, Bottom, Left, 0.84f),
            ArcTangent(LeftArcCenter, Bottom, Left, 0.84f),
            Lerp(Left, Top, 0.20f),
            Vector(Left, Top)));
        var topControl = frame.Point(TangentIntersection(
            Lerp(Top, Left, 0.18f),
            Vector(Left, Top),
            Lerp(Top, Right, 0.18f),
            Vector(Top, Right)));

        var topRightSegment = GraphicStructure.FrameSegment(topRightA, topRightB);
        var rightCorner = GraphicStructure.FrameQuadraticCurve(topRightB, rightControl, rightArcStart);
        var rightArc = GraphicStructure.FrameArc(rightCenter, rightArcStart, bottomRightTrim);
        var bottomCorner = GraphicStructure.FrameQuadraticCurve(bottomRightTrim, bottomControl, bottomLeftTrim);
        var leftArc = GraphicStructure.FrameArc(leftCenter, bottomLeftTrim, leftArcEnd);
        var leftCorner = GraphicStructure.FrameQuadraticCurve(leftArcEnd, leftControl, leftTopA);
        var leftTopSegment = GraphicStructure.FrameSegment(leftTopA, leftTopB);
        var topCorner = GraphicStructure.FrameQuadraticCurve(leftTopB, topControl, topRightA);
        var sections = new IElement[]
        {
            topRightSegment,
            rightCorner,
            rightArc,
            bottomCorner,
            leftArc,
            leftCorner,
            leftTopSegment,
            topCorner
        };
        var joins = new IElement[]
        {
            JoinAt(topRightB, topRightSegment, rightCorner),
            JoinAt(rightArcStart, rightCorner, rightArc),
            JoinAt(bottomRightTrim, rightArc, bottomCorner),
            JoinAt(bottomLeftTrim, bottomCorner, leftArc),
            JoinAt(leftArcEnd, leftArc, leftCorner),
            JoinAt(leftTopA, leftCorner, leftTopSegment),
            JoinAt(leftTopB, leftTopSegment, topCorner),
            JoinAt(topRightA, topCorner, topRightSegment)
        };
        var angleBases = new IElement[]
        {
            AngleBasis(top, 315, 45),
            AngleBasis(right, 135, 245),
            AngleBasis(bottom, 12, 168),
            AngleBasis(left, 295, 45)
        };

        return new BoundaryRead(Chain(sections), sections, [top, right, bottom, left], joins, angleBases);
    }

    private static BoundaryRead CreateInnerSource(FrameBasis frame)
    {
        var left = frame.Point(InnerLeft);
        var right = frame.Point(InnerRight);
        var center = frame.Point(InnerArcCenter);
        var arc = GraphicStructure.FrameArc(center, right, left);
        var flat = GraphicStructure.FrameSegment(left, right);
        var sections = new IElement[] { arc, flat };
        var joins = new IElement[]
        {
            JoinAt(left, arc, flat),
            JoinAt(right, flat, arc)
        };
        var angleBases = new IElement[]
        {
            AngleBasis(right, 92, 180),
            AngleBasis(left, 0, 88)
        };

        return new BoundaryRead(Chain(sections), sections, [left, right], joins, angleBases);
    }

    private static BoundaryRead CreateInnerRounded(FrameBasis frame)
    {
        var left = frame.Point(InnerLeft);
        var right = frame.Point(InnerRight);
        var center = frame.Point(InnerArcCenter);
        var rightArcTrimModel = ArcPoint(InnerArcCenter, InnerRight, InnerLeft, 0.13f);
        var leftArcTrimModel = ArcPoint(InnerArcCenter, InnerRight, InnerLeft, 0.87f);
        var leftFlatTrimModel = Lerp(InnerLeft, InnerRight, 0.22f);
        var rightFlatTrimModel = Lerp(InnerRight, InnerLeft, 0.22f);
        var leftArcTrim = frame.Point(leftArcTrimModel);
        var rightArcTrim = frame.Point(rightArcTrimModel);
        var rightFlatTrim = frame.Point(rightFlatTrimModel);
        var leftFlatTrim = frame.Point(leftFlatTrimModel);
        var leftControl = frame.Point(TangentIntersection(
            leftArcTrimModel,
            ArcTangent(InnerArcCenter, InnerRight, InnerLeft, 0.87f),
            leftFlatTrimModel,
            Vector(InnerLeft, InnerRight)));
        var rightControl = frame.Point(TangentIntersection(
            rightFlatTrimModel,
            Vector(InnerLeft, InnerRight),
            rightArcTrimModel,
            ArcTangent(InnerArcCenter, InnerRight, InnerLeft, 0.13f)));

        var retainedArc = GraphicStructure.FrameArc(center, rightArcTrim, leftArcTrim);
        var leftCorner = GraphicStructure.FrameQuadraticCurve(leftArcTrim, leftControl, leftFlatTrim);
        var flat = GraphicStructure.FrameSegment(leftFlatTrim, rightFlatTrim);
        var rightCorner = GraphicStructure.FrameQuadraticCurve(rightFlatTrim, rightControl, rightArcTrim);
        var sections = new IElement[] { retainedArc, leftCorner, flat, rightCorner };
        var joins = new IElement[]
        {
            JoinAt(leftArcTrim, retainedArc, leftCorner),
            JoinAt(leftFlatTrim, leftCorner, flat),
            JoinAt(rightFlatTrim, flat, rightCorner),
            JoinAt(rightArcTrim, rightCorner, retainedArc)
        };
        var angleBases = new IElement[]
        {
            AngleBasis(right, 92, 180),
            AngleBasis(left, 0, 88)
        };

        return new BoundaryRead(Chain(sections), sections, [left, right], joins, angleBases);
    }

    private static ModelPoint Lerp(ModelPoint start, ModelPoint end, float progress) =>
        new(
            (long)Math.Round(start.X + (end.X - start.X) * progress),
            (long)Math.Round(start.Y + (end.Y - start.Y) * progress));

    private static ModelPoint ArcPoint(ModelPoint center, ModelPoint start, ModelPoint end, float progress)
    {
        var startAngle = Math.Atan2(start.Y - center.Y, start.X - center.X);
        var endAngle = Math.Atan2(end.Y - center.Y, end.X - center.X);
        var sweep = ShortestSweep(startAngle, endAngle);
        var radius = Math.Sqrt(Math.Pow(start.X - center.X, 2) + Math.Pow(start.Y - center.Y, 2));
        var angle = startAngle + sweep * progress;
        return new ModelPoint(
            (long)Math.Round(center.X + Math.Cos(angle) * radius),
            (long)Math.Round(center.Y + Math.Sin(angle) * radius));
    }

    private static ModelVector ArcTangent(ModelPoint center, ModelPoint start, ModelPoint end, float progress)
    {
        var startAngle = Math.Atan2(start.Y - center.Y, start.X - center.X);
        var endAngle = Math.Atan2(end.Y - center.Y, end.X - center.X);
        var sweep = ShortestSweep(startAngle, endAngle);
        var angle = startAngle + sweep * progress;
        var direction = sweep < 0 ? -1d : 1d;
        return new ModelVector(-Math.Sin(angle) * direction, Math.Cos(angle) * direction);
    }

    private static ModelVector Vector(ModelPoint start, ModelPoint end) =>
        new(end.X - start.X, end.Y - start.Y);

    private static ModelPoint TangentIntersection(ModelPoint start, ModelVector startTangent, ModelPoint end, ModelVector endTangent)
    {
        var cross = Cross(startTangent, endTangent);
        if (Math.Abs(cross) < 0.0001d)
        {
            return new ModelPoint((start.X + end.X) / 2, (start.Y + end.Y) / 2);
        }

        var delta = new ModelVector(end.X - start.X, end.Y - start.Y);
        var t = Cross(delta, endTangent) / cross;
        return new ModelPoint(
            (long)Math.Round(start.X + startTangent.X * t),
            (long)Math.Round(start.Y + startTangent.Y * t));
    }

    private static double Cross(ModelVector left, ModelVector right) =>
        left.X * right.Y - left.Y * right.X;

    private static double ShortestSweep(double start, double end)
    {
        var sweep = end - start;
        while (sweep > Math.PI)
        {
            sweep -= Math.PI * 2;
        }

        while (sweep < -Math.PI)
        {
            sweep += Math.PI * 2;
        }

        return sweep;
    }

    private static Relation JoinAt(Relation point, IElement incoming, IElement outgoing) =>
        new(point, Group(incoming, outgoing));

    private static Relation AngleBasis(Relation point, long incomingAngleTicks, long outgoingAngleTicks)
    {
        var localPoint = GraphicStructure.Point(new Carrier(UnitShape.DescribedAtom(point)), 0);
        var angle = GraphicStructure.CircularCarrierAt(localPoint);
        return new Relation(
            point,
            Group(
                localPoint,
                GraphicStructure.Segment(angle, 0, 360),
                GraphicStructure.Point(angle, incomingAngleTicks),
                GraphicStructure.Point(angle, outgoingAngleTicks)));
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

    private sealed class FrameBasis
    {
        private readonly Carrier frame = new();
        private readonly Carrier x = new();
        private readonly Carrier y = new();

        public Relation Point(ModelPoint point) =>
            GraphicStructure.FramePoint(frame, new CarrierLocation(x, point.X), new CarrierLocation(y, point.Y));
    }
}
