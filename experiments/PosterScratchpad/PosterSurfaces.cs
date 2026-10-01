using Gojo.Core;
using Gojo.Encoding;
using GojoCAD;

namespace PosterScratchpad;

// Experimental construction grammar. Every returned artifact is an ordinary
// Gojo graph. The static fields are shared bases, not per-poster state/metadata.
internal static class PosterSurfaces
{
    internal static readonly Carrier Frame = new();
    internal static readonly Carrier X = new(UnitShape.LinearAt(Relation.Point(new Carrier(), 0)));
    internal static readonly Carrier Y = new(UnitShape.LinearAt(Relation.Point(new Carrier(), 0)));
    internal static readonly Carrier Red = new();
    internal static readonly Carrier Green = new();
    internal static readonly Carrier Blue = new();
    internal static readonly Carrier TypeSize = new(X.UnitShape);
    internal static readonly Carrier FontWeight = new();

    internal static Relation Group(params IElement[] parts) => Relation.OrderedGroup(new Carrier(), parts);
    internal static IElement[] Parts(Relation group)
    {
        if (group.Right is Carrier holder && group.TryReadOrderedElementsOn(holder, out var parts))
            return parts;
        throw new InvalidOperationException("Expected a local ordered surface.");
    }
    internal static Relation Point(long x, long y) => GraphicStructure.FramePoint(Frame,
        new CarrierLocation(X, x), new CarrierLocation(Y, y));
    internal static Relation Color(long r, long g, long b) => Group(
        new CarrierLocation(Red, r), new CarrierLocation(Green, g), new CarrierLocation(Blue, b));
    internal static Relation Ink(Relation geometry, Relation color) => new(geometry, color);
    internal static Relation Polygon(params (long X, long Y)[] vertices)
    {
        var points = vertices.Select(v => Point(v.X, v.Y)).ToArray();
        return Group(points.Select((point, index) => (IElement)GraphicStructure.FrameSegment(
            point, points[(index + 1) % points.Length])).ToArray());
    }
    internal static Relation Rectangle(long x, long y, long width, long height) =>
        Polygon((x, y), (x + width, y), (x + width, y + height), (x, y + height));
    internal static Relation Circle(long x, long y, long radius) =>
        GraphicStructure.FrameCircle(Point(x, y), Point(x + radius, y));
    internal static Relation Text(string text, long x, long baseline, long size, long maxWidth,
        string family, long weight = 400) => Group(
            EncodedTextSurface.Create(new Carrier(), text),
            GraphicStructure.FrameSegment(Point(x, baseline), Point(x + maxWidth, baseline)),
            new CarrierLocation(TypeSize, size), EncodedTextSurface.Create(new Carrier(), family),
            new CarrierLocation(FontWeight, weight));
    internal static Relation Document(params IElement[] layers) => Group(
        GraphicStructure.FrameSegment(Point(0, 0), Point(1080, 1440)), Group(layers));

    internal static Relation A()
    {
        var paper = Color(255, 242, 215);
        var plum = Color(73, 29, 67);
        var coral = Color(244, 83, 58);
        var gold = Color(246, 190, 52);
        // The logo is a connected group of four circular lobes and a paper-colored
        // center. It contains no encoded text. Repeated points share the page basis.
        var logo = Group(
            Ink(Circle(448, 370, 151), coral), Ink(Circle(632, 370, 151), coral),
            Ink(Circle(448, 554, 151), coral), Ink(Circle(632, 554, 151), coral),
            Ink(Circle(540, 462, 105), paper), Ink(Circle(540, 462, 38), plum));
        return Document(
            Ink(Rectangle(0, 0, 1080, 1440), paper),
            Ink(Text("AN OPEN INVITATION", 66, 106, 24, 730, "Arial", 700), plum),
            Ink(Rectangle(66, 134, 948, 3), plum),
            Ink(Circle(935, 260, 38), gold),
            logo,
            Ink(Circle(131, 706, 18), plum),
            Ink(Text("gather", 58, 983, 244, 968, "Georgia", 700), plum),
            Ink(Rectangle(66, 1061, 640, 3), plum),
            Ink(Text("SEPT 29, 2006", 66, 1140, 44, 635, "Arial", 700), plum),
            Ink(Text("2:00PM", 66, 1226, 70, 580, "Georgia", 400), plum),
            Ink(Circle(880, 1191, 130), plum),
            Ink(Text("$10", 789, 1204, 98, 189, "Arial", 700), paper),
            Ink(Text("ENTRANCE FEE", 793, 1255, 22, 182, "Arial", 700), paper),
            Ink(Rectangle(66, 1345, 948, 3), plum));
    }

    internal static Relation B()
    {
        var ink = Color(18, 24, 28);
        var paper = Color(239, 242, 227);
        var lime = Color(211, 247, 79);
        var quiet = Color(110, 125, 124);
        // Two opposed folded contours form a split diamond. The void is
        // uncovered paper, rather than an icon/operation enum or a letter glyph.
        var logo = Group(
            Ink(Polygon((158, 458), (411, 205), (547, 205), (294, 458), (547, 711), (411, 711)), lime),
            Ink(Polygon((533, 205), (669, 205), (922, 458), (669, 711), (533, 711), (786, 458)), lime));
        return Document(
            Ink(Rectangle(0, 0, 1080, 1440), ink),
            Ink(Text("AN OPEN INVITATION", 66, 106, 24, 730, "Bahnschrift", 400), paper),
            Ink(Rectangle(66, 134, 948, 2), quiet),
            logo,
            Ink(Text("GATHER", 60, 984, 235, 963, "Impact", 400), paper),
            Ink(Rectangle(66, 1061, 948, 3), paper),
            Ink(Text("SEPT 29, 2006", 66, 1140, 44, 625, "Bahnschrift", 400), paper),
            Ink(Text("2:00PM", 66, 1228, 70, 580, "Bahnschrift", 400), lime),
            Ink(Rectangle(722, 1101, 2, 173), quiet),
            Ink(Text("$10", 785, 1210, 103, 220, "Bahnschrift", 700), paper),
            Ink(Text("ENTRANCE FEE", 787, 1260, 23, 220, "Bahnschrift", 400), paper),
            Ink(Rectangle(66, 1345, 948, 2), quiet));
    }
}
