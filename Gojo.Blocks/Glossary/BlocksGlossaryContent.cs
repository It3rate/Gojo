using Gojo.Core;
using GojoCAD;
using GojoCAD.Structures;
using SkiaSharp;
using System.Text.Json;

namespace Gojo.Blocks;

internal static class BlocksGlossaryContent
{
    private const string DefaultLocale = "en";
    private const string DescriptionFileName = "glossary-descriptions.json";

    public static IReadOnlyList<BlocksPage> CreatePages(string locale = DefaultLocale)
    {
        var descriptions = LoadDescriptions(locale);
        var structures = BlocksGlossaryStructureCatalog.Create();
        return
        [
            new BlocksPage(
                "glossary-source-support",
                "Source / Direction Ladder",
                "Source, flow, boundary, partition, orientation, and direction.",
                string.Empty,
                null,
                null,
                0,
                ["site", "flow", "boundary", "partition", "orientation", "direction"],
                GlossarySheet: BlocksGlossarySheetKind.SourceToSupport,
                GlossaryDescriptions: descriptions,
                GlossaryStructures: structures),
            new BlocksPage(
                "glossary-direction-area",
                "Support / Area Ladder",
                "Directed boundary, support, area, directed area, and bounded area.",
                string.Empty,
                null,
                null,
                0,
                ["directed boundary", "boundary support", "directed support", "area", "directed area", "bounded area"],
                GlossarySheet: BlocksGlossarySheetKind.DirectionToArea,
                GlossaryDescriptions: descriptions,
                GlossaryStructures: structures),
            new BlocksPage(
                "glossary-curves",
                "Carrier Curve Joins",
                "Corners, smooth joins, changing curvature, involutes, and plotted contrast.",
                string.Empty,
                null,
                null,
                0,
                ["tangency", "corner", "arc", "Bezier", "clothoid", "involute", "plot"],
                GlossarySheet: BlocksGlossarySheetKind.Curves,
                GlossaryDescriptions: descriptions,
                GlossaryStructures: structures),
            new BlocksPage(
                "glossary-branches",
                "Branch / Merge Structures",
                "Tip joins, middle joins, crossings, enclosures, and larger branch networks.",
                string.Empty,
                null,
                null,
                0,
                ["branch", "merge", "butt joint", "corner", "crossing", "enclosure", "partial area"],
                GlossarySheet: BlocksGlossarySheetKind.Branches,
                GlossaryDescriptions: descriptions,
                GlossaryStructures: structures),
            new BlocksPage(
                "glossary-boolean-participation",
                "Boolean Participation",
                "Occupied support reads for points, segments, and areas.",
                string.Empty,
                null,
                null,
                0,
                ["participants", "touch", "overlap", "union", "intersection", "difference"],
                GlossarySheet: BlocksGlossarySheetKind.BooleanParticipation,
                GlossaryDescriptions: descriptions,
                GlossaryStructures: structures),
            new BlocksPage(
                "glossary-connected-paths",
                "Connected Paths",
                "Shared points, bonded endpoints, orthogonal joins, bridges, and closure tension.",
                string.Empty,
                null,
                null,
                0,
                ["sections", "shared point", "bonded point", "orthogonal join", "bridge", "closure tension"],
                GlossarySheet: BlocksGlossarySheetKind.ConnectedPaths,
                GlossaryDescriptions: descriptions,
                GlossaryStructures: structures),
            new BlocksPage(
                "glossary-path-read-bases",
                "Path Reads / Bases",
                "XY, circular, orthogonal, mixed, composite, and residue-bearing path reads.",
                string.Empty,
                null,
                null,
                0,
                ["XY basis", "circular basis", "orthogonal basis", "mixed basis", "path read", "residue"],
                GlossarySheet: BlocksGlossarySheetKind.PathReadBases,
                GlossaryDescriptions: descriptions,
                GlossaryStructures: structures),
            new BlocksPage(
                "glossary-derived-path-structures",
                "Derived Path Structures",
                "Offsets, insets, fills, and branch/merge area reads from connected paths.",
                string.Empty,
                null,
                null,
                0,
                ["offset", "inset", "outset", "fill", "branch", "merge", "residue"],
                GlossarySheet: BlocksGlossarySheetKind.DerivedPathStructures,
                GlossaryDescriptions: descriptions,
                GlossaryStructures: structures),
            new BlocksPage(
                "glossary-boolean-offset-reads",
                "Boolean Offset Reads",
                "Union, intersection, holes, and rounded-corner ladder steps from offset path reads.",
                string.Empty,
                null,
                null,
                0,
                ["source carriers", "offset reads", "intersections", "split sections", "loops", "holes", "ladder"],
                GlossarySheet: BlocksGlossarySheetKind.BooleanOffsetReads,
                GlossaryDescriptions: descriptions,
                GlossaryStructures: structures),
            new BlocksPage(
                "glossary-drone-clip-offset-ladder",
                "Drone Clip Offset Ladder",
                "A ten-rung progression from a drone arm clip source outline through offsets, intersections, rounding, loops, and filled negative spaces.",
                string.Empty,
                null,
                null,
                0,
                ["source", "path read", "offset", "intersections", "split sections", "outside rounding", "inside rounding", "loop merge", "negative spaces", "settled read"],
                GlossarySheet: BlocksGlossarySheetKind.DroneClipOffsetLadder,
                GlossaryDescriptions: descriptions,
                GlossaryStructures: structures),
            new BlocksPage(
                "glossary-radial-crescent-ladder",
                "Crescent Radial Ladder",
                "A six-rung read of the Sketch Viewer crescent + bent-arms radial ladder through merged areas.",
                string.Empty,
                null,
                null,
                0,
                ["seed arms", "body", "motor caps", "trimmed arms", "offset boundaries", "fill participants", "merged areas"],
                GlossarySheet: BlocksGlossarySheetKind.RadialCrescentLadder,
                GlossaryDescriptions: descriptions,
                GlossaryStructures: structures),
            new BlocksPage(
                "glossary-diamond-cusp-rounding",
                "Diamond Cusp Rounding",
                "Outer and inner Gojo boundaries, two cusp-selection passes, tangent rounding, and a final cutout fill.",
                string.Empty,
                null,
                null,
                0,
                ["shared endpoints", "outer cusps", "inner cusps", "cutout fill"],
                GlossarySheet: BlocksGlossarySheetKind.DiamondCuspRounding,
                GlossaryDescriptions: descriptions,
                GlossaryStructures: structures),
            new BlocksPage(
                "glossary-pcb-data-attachment",
                "PCB Data Attachment",
                "Schematic intent, layout carriers, local attributes, rule reads, and physical board interpretation from one inspectable structure.",
                string.Empty,
                null,
                null,
                0,
                ["schematic", "layout", "net data", "render read", "rule check", "physical read"],
                GlossarySheet: BlocksGlossarySheetKind.PcbDataAttachment,
                GlossaryDescriptions: descriptions,
                GlossaryStructures: structures)
        ];
    }

    private static IReadOnlyDictionary<string, string> LoadDescriptions(string locale)
    {
        var contentRoot = ResolveContentRoot(locale);
        var path = Path.Combine(contentRoot, DescriptionFileName);
        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<Dictionary<string, string>>(
            json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ??
            new Dictionary<string, string>();
    }

    private static string ResolveContentRoot(string locale)
    {
        var relative = Path.Combine("Glossary", "content", locale);
        var candidates = new[]
        {
            Path.Combine(AppContext.BaseDirectory, relative),
            Path.Combine(Environment.CurrentDirectory, "Gojo.Blocks", relative),
            Path.Combine(Environment.CurrentDirectory, relative)
        };

        foreach (var candidate in candidates)
        {
            if (File.Exists(Path.Combine(candidate, DescriptionFileName)))
            {
                return candidate;
            }
        }

        throw new DirectoryNotFoundException(
            $"Could not find glossary content for locale '{locale}'. Looked under: {string.Join("; ", candidates)}");
    }

    private static IReadOnlyList<BlocksGlossaryItem> CreateItems() =>
    [
        Item("point-site", "Point / Site", PointSite(), Bounds(-12, -12, 12, 12)),
        Item("point-pair", "Two Points", TwoPoints(), Bounds(-40, -18, 40, 18)),
        Item("segment", "Segment", Segment(), Bounds(-48, -18, 48, 18)),
        Item("directed-segment", "Directed Segment", DirectedSegment(), Bounds(-55, -24, 55, 24)),
        Item("radial-flow", "Radial Flow", RadialFlow(), Bounds(-36, -36, 36, 36)),
        Item("line-seed", "Line Seed", LineSeed(), Bounds(-50, -36, 50, 36)),
        Item("sheet-seed", "Sheet Seed", SheetSeed(), Bounds(-50, -36, 50, 36)),
        Item("circle", "Circumference", Circle(), Bounds(-48, -48, 48, 48)),
        Item("radius", "Radius Attachment", RadiusAttachment(), Bounds(-52, -42, 52, 42)),
        Item("arc", "Arc", Arc(), Bounds(-48, -48, 48, 48)),
        Item("quadratic", "Quadratic Curve", QuadraticCurve(), Bounds(-52, -34, 52, 40)),
        Item("cubic", "Cubic Curve", CubicCurve(), Bounds(-56, -36, 56, 42)),
        Item("segment-partitions", "Segment Partitions", SegmentPartitions(), Bounds(-54, -20, 54, 20)),
        Item("contour-partitions", "Contour Partitions", ContourPartitions(), Bounds(-50, -50, 50, 50)),
        Item("area-fill", "Area Fill", AreaFill(inverse: false), Bounds(-50, -40, 50, 40)),
        Item("anti-area-fill", "Anti Fill", AreaFill(inverse: true), Bounds(-50, -40, 50, 40)),
        Item("closed-boundary", "Closed Boundary", RectangleBoundary(), Bounds(-50, -40, 50, 40)),
        Item("area-union", "Area Union", AreaUnion(), Bounds(-64, -42, 64, 42)),
        Item("outline-witness", "Outline Witness", OutlineWitness(), Bounds(-64, -42, 64, 42)),
        Item("path-branch", "Path Branch", BranchPath(), Bounds(-54, -44, 54, 44)),
        Item("path-merge", "Path Merge", MergePath(), Bounds(-54, -44, 54, 44)),
        Item("shared-polyline", "Shared Polyline", SharedPolyline(), Bounds(-56, -42, 56, 42)),
        Item("selection", "Selection Evidence", SelectionEvidence(), Bounds(-58, -34, 58, 34))
    ];

    private static BlocksGlossaryItem Item(string id, string title, IElement structure, SKRect? bounds, int seed = 0) =>
        new(id, title, structure, bounds, seed);

    private static SKRect Bounds(float left, float top, float right, float bottom) =>
        new(left, top, right, bottom);

    private static Relation PointSite()
    {
        var frame = new FrameBasis();
        return frame.Point(0, 0);
    }

    private static Relation TwoPoints()
    {
        var frame = new FrameBasis();
        return Group(frame.Point(-28, 0), frame.Point(28, 0));
    }

    private static Relation Segment()
    {
        var frame = new FrameBasis();
        return GraphicStructure.FrameSegment(frame.Point(-40, 0), frame.Point(40, 0));
    }

    private static Relation DirectedSegment()
    {
        var carrier = new Carrier();
        return GraphicStructure.DirectedSegment(carrier, -34, 34);
    }

    private static Relation RadialFlow()
    {
        var carrier = new Carrier();
        return GraphicStructure.RadialFlow(GraphicStructure.Point(carrier, 0));
    }

    private static Relation LineSeed()
    {
        var carrier = new Carrier();
        return GraphicStructure.RadialFlow(GraphicStructure.Segment(carrier, 0, 0));
    }

    private static Relation SheetSeed()
    {
        var carrier = new Carrier();
        return GraphicStructure.RadialFlow(GraphicStructure.Segment(carrier, -28, 28));
    }

    private static Relation Circle()
    {
        var frame = new FrameBasis();
        return GraphicStructure.FrameCircle(frame.Point(0, 0), frame.Point(32, 0));
    }

    private static Relation RadiusAttachment()
    {
        var baseCarrier = new Carrier();
        var origin = GraphicStructure.Point(baseCarrier, 0);
        var circle = GraphicStructure.CircularCarrierAt(origin);
        var marker = GraphicStructure.Point(circle, 72);
        return Group(origin, marker, GraphicStructure.Pin(origin, marker));
    }

    private static Relation Arc()
    {
        var frame = new FrameBasis();
        return GraphicStructure.FrameArc(frame.Point(0, 0), frame.Point(32, 0), frame.Point(0, 32));
    }

    private static Relation QuadraticCurve()
    {
        var frame = new FrameBasis();
        return GraphicStructure.FrameQuadraticCurve(
            frame.Point(-42, 18),
            frame.Point(0, -26),
            frame.Point(42, 18));
    }

    private static Relation CubicCurve()
    {
        var frame = new FrameBasis();
        return GraphicStructure.FrameCubicCurve(
            frame.Point(-46, 20),
            frame.Point(-18, -28),
            frame.Point(18, 38),
            frame.Point(46, -18));
    }

    private static Relation SegmentPartitions()
    {
        var carrier = new Carrier();
        return Group(
            GraphicStructure.Segment(carrier, -45, 45),
            GraphicStructure.Point(carrier, -20),
            GraphicStructure.Point(carrier, 0),
            GraphicStructure.Point(carrier, 20));
    }

    private static Relation ContourPartitions()
    {
        var baseCarrier = new Carrier();
        var origin = GraphicStructure.Point(baseCarrier, 0);
        var circle = GraphicStructure.CircularCarrierAt(origin);
        return Group(
            GraphicStructure.Segment(circle, 0, 120),
            GraphicStructure.Point(circle, 0),
            GraphicStructure.Point(circle, 30),
            GraphicStructure.Point(circle, 60),
            GraphicStructure.Point(circle, 90),
            GraphicStructure.Point(circle, 120));
    }

    private static Relation AreaFill(bool inverse)
    {
        var boundary = RectangleSegments();
        return inverse
            ? GraphicStructure.AntiAreaFill(boundary)
            : GraphicStructure.AreaFill(boundary);
    }

    private static Relation RectangleBoundary() =>
        Group(RectangleSegments());

    private static Relation AreaUnion()
    {
        var left = AreaFill(inverse: false);
        var right = TranslatedAreaFill(18, 0, inverse: false);
        var areas = GraphicAreaParticipationSurface.AreaFrame(new Carrier(), left, right);
        var outline = RectangleBoundary();
        return GraphicAreaParticipationSurface.AreaUnion(new Carrier(), areas, outline);
    }

    private static Relation OutlineWitness()
    {
        var area = AreaFill(inverse: false);
        return GraphicOutlineSurface.FromArea(new Carrier(), area, RectangleBoundary());
    }

    private static Relation BranchPath()
    {
        var frame = new FrameBasis();
        var left = frame.Point(-42, 0);
        var hub = frame.Point(0, 0);
        return Group(
            GraphicStructure.FrameSegment(left, hub),
            GraphicStructure.FrameSegment(hub, frame.Point(42, -26)),
            GraphicStructure.FrameSegment(hub, frame.Point(42, 26)));
    }

    private static Relation MergePath()
    {
        var frame = new FrameBasis();
        var hub = frame.Point(0, 0);
        var right = frame.Point(42, 0);
        return Group(
            GraphicStructure.FrameSegment(frame.Point(-42, -26), hub),
            GraphicStructure.FrameSegment(frame.Point(-42, 26), hub),
            GraphicStructure.FrameSegment(hub, right));
    }

    private static Relation SharedPolyline()
    {
        var frame = new FrameBasis();
        var a = frame.Point(-44, 18);
        var b = frame.Point(-16, -16);
        var c = frame.Point(14, 16);
        var d = frame.Point(44, -18);
        return Group(
            GraphicStructure.FrameSegment(a, b),
            GraphicStructure.FrameSegment(b, c),
            GraphicStructure.FrameSegment(c, d));
    }

    private static Relation SelectionEvidence()
    {
        var source = Segment();
        var match = GraphicSelectionSurface.Match(
            new Carrier(),
            source,
            source,
            GraphicSelectionSurface.Frame(new Carrier()),
            source,
            GraphicStructure.Point(Carrier.Unknown, 0));
        var matches = GraphicSelectionSurface.MatchFrame(new Carrier(), match);
        return GraphicSelectionSurface.Select(
            new Carrier(),
            source,
            source,
            source,
            matches,
            GraphicStructure.Point(Carrier.Unknown, 0));
    }

    private static Relation TranslatedAreaFill(long dx, long dy, bool inverse)
    {
        var boundary = RectangleSegments(dx, dy);
        return inverse
            ? GraphicStructure.AntiAreaFill(boundary)
            : GraphicStructure.AreaFill(boundary);
    }

    private static IElement[] RectangleSegments(long dx = 0, long dy = 0)
    {
        var frame = new FrameBasis();
        var a = frame.Point(-32 + dx, -22 + dy);
        var b = frame.Point(32 + dx, -22 + dy);
        var c = frame.Point(32 + dx, 22 + dy);
        var d = frame.Point(-32 + dx, 22 + dy);
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

    private sealed class FrameBasis
    {
        private readonly Carrier frame = new();
        private readonly Carrier x = new();
        private readonly Carrier y = new();

        public Relation Point(long px, long py) =>
            GraphicStructure.FramePoint(frame, new CarrierLocation(x, px), new CarrierLocation(y, py));
    }
}
