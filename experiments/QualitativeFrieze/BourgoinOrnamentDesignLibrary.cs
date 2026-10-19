using Gojo.Core;
using Gojo.Patterns;
using N = Gojo.Experiments.QualitativeFrieze.FieldNative;

namespace Gojo.Experiments.QualitativeFrieze;

/// <summary>Three separately authored, native ornament designs for every
/// retained skeleton. Their common source is unchanged. Finite petals,
/// panels, stems, leaves and seeds live in actual layered path programs;
/// names, palette indexes and display weights only describe their presentation.</summary>
public static class BourgoinOrnamentDesignLibrary
{
    public sealed record Design(string Id, string SourceId, string Mode, string Title,
        BourgoinPatternLibrary.Recipe Source, OrnamentEnrichment Enrichment,
        IElement FieldLayer, IElement MotifLayer, IElement DetailLayer, string Description,
        int PaletteIndex, double Weight);

    public static IReadOnlyList<Design> Create() =>
        Create(new BourgoinPatternLibrary(subdivisions: 4).Recipes);

    public static IReadOnlyList<Design> Create(IReadOnlyList<BourgoinPatternLibrary.Recipe> sources) =>
        sources.SelectMany((source, index) => Create(source, index)).ToArray();

    public static IReadOnlyList<Design> Create(BourgoinPatternLibrary.Recipe source, int paletteIndex = 0) =>
        Create(source, paletteIndex, ["field", "motif", "detail"]);

    public static IReadOnlyList<Design> Create(BourgoinPatternLibrary.Recipe source, int paletteIndex, IEnumerable<string> modes)
    {
        var selected = modes.Distinct(StringComparer.Ordinal).ToArray();
        if (selected.Any(mode => mode is not ("field" or "motif" or "detail")))
            throw new ArgumentException("Design modes are field, motif or detail.", nameof(modes));
        return selected.Select(mode => Build(source, mode, paletteIndex)).ToArray();
    }

    private enum Family { Panel, Basket, Medallion, Trellis, Sixfold, Fret, Fan, Cloud, Chevron }
    private static Family ReadFamily(string id) => id switch
    {
        "B01" or "B19" or "B23" => Family.Panel,
        "B02" or "B07" => Family.Basket,
        "B03" or "B06" or "B06.B" or "B18" => Family.Medallion,
        "B08" or "B08.B" or "B15" or "B16" or "B17" or "B21" or "B22" => Family.Trellis,
        "B04" or "B05" or "B20" or "B24" or "B25" or "B26" or "B29" => Family.Sixfold,
        "B27" => Family.Chevron, "B28" => Family.Fan, "B30" => Family.Cloud,
        _ => Family.Fret
    };

    private static Design Build(BourgoinPatternLibrary.Recipe source, string mode, int palette)
    {
        var a = new BourgoinOrnamentAuthor(source); var family = ReadFamily(source.Id);
        var closed = a.Paths.Where(path => path.Closed).ToArray();
        var centres = Centres(a, source.Id);
        var triangular = Math.Abs(source.DisplayBasis.UX) > 0;
        var longPaths = a.Paths.OrderByDescending(path => path.Vertices.Length).Take(4).ToArray();
        string title; string description;

        if (mode == "field")
        {
            title = family switch
            {
                Family.Panel => "Architectural panels", Family.Basket => "Woven compartments",
                Family.Medallion => "Enamelled geometric field", Family.Trellis => "Broad-leaf trellis",
                Family.Sixfold => "Lotus and star field", Family.Fan => "Layered palm fans",
                Family.Cloud => "Cloud brocade", Family.Chevron => "Palmette courses", _ => "Ribbon and window brocade"
            };
            // Large repeats and subordinate subdivisions are actual separate
            // contours; changing focus never manufactures another path.
            foreach (var path in closed.Take(4))
                Inset(a, a.Motif, path, 3, 4);
            foreach (var (centre, radius, path) in centres)
            {
                var anchor = a.NearestPort(centre, path?.Path);
                if (family is Family.Panel or Family.Basket)
                {
                    var vertical = path is null || path.Height >= path.Width;
                    // Overall architecture uses a cartouche; the separate
                    // intermediate design grows a botanical tulip instead.
                    if (source.Id == "B01") vertical = false;
                    Cartouche(a, a.Motif, centre, Math.Max(2, radius), vertical, anchor);
                    Seed(a, a.Detail, centre, 1, anchor);
                }
                else if (family is Family.Fan or Family.Cloud)
                {
                    FanPanel(a, a.Motif, centre, radius, anchor);
                    Seed(a, a.Detail, centre, Math.Max(1, radius / 5), anchor);
                }
                else if (family == Family.Trellis)
                {
                    var map = a.Map((1, -1), (1, 1));
                    Leaf(a, a.Motif, centre, Math.Max(3, radius), Math.Max(1, radius / 3), anchor, map);
                    Leaf(a, a.Motif, centre, Math.Max(3, radius), Math.Max(1, radius / 3), anchor, a.Map((-1, 1), (-1, -1)));
                    Seed(a, a.Detail, centre, 1, anchor);
                }
                else
                {
                    if (family == Family.Sixfold)
                    {
                        var r = Math.Max(2, radius);
                        a.Place(a.Motif, a.Prototype(true, (r, 0), (0, r), (-r, r), (-r, 0),
                            (0, -r), (r, -r), (r, 0)), centre, anchor);
                        Ring(a, a.Detail, centre, Math.Max(1, radius / 3), anchor);
                    }
                    else if (family == Family.Medallion)
                        Ring(a, a.Motif, centre, Math.Max(2, radius), anchor);
                    else
                        a.Place(a.Motif, a.Prototype(true, (0, radius), (radius, 0), (0, -radius),
                            (-radius, 0), (0, radius)), centre, anchor);
                    Seed(a, a.Detail, centre, Math.Max(1, radius / 4), anchor);
                }
            }
            if (closed.Length == 0)
                foreach (var path in longPaths.Take(family == Family.Cloud ? 4 : 2))
                    Companion(a, a.Field, path, family == Family.Cloud ? (0, -2) : (0, 1));
            description = "The large repeat leads: retained construction lines carry broad bands, finite inset compartments and sparse large conventional ornament. Subordinate seeds punctuate the centres. This is a separately authored design, with actual native additions and landmark-offset witnesses, rather than a recolouring of the other two designs.";
        }
        else if (mode == "motif")
        {
            title = family switch
            {
                Family.Panel => "Tulips in the panels", Family.Basket => "Alternating woven leaf pairs",
                Family.Medallion => "Rosette medallions", Family.Trellis => "Leaf and trefoil lattice",
                Family.Sixfold => "Sixfold conventional flowers", Family.Fan => "Lotus fan blossoms",
                Family.Cloud => "Round leaves in the cloud valleys", Family.Chevron => "Sprouting chevron flowers", _ => "Growing fret foliage"
            };
            foreach (var (centre, radius, path) in centres)
            {
                var anchor = a.NearestPort(centre, path?.Path);
                if (family is Family.Panel)
                {
                    Tulip(a, a.Motif, centre, Math.Max(3, radius), path is null || path.Height >= path.Width, anchor);
                    Stem(a, a.Detail, centre, radius, anchor);
                    BudPair(a, a.Detail, (centre.X, centre.Y - Math.Max(2, radius / 2)), Math.Max(2, radius / 3), anchor);
                }
                else if (family == Family.Basket)
                {
                    var vertical = path is null || path.Height >= path.Width;
                    var first = vertical ? a.Map((1, 0), (0, 1)) : a.Map((0, 1), (-1, 0));
                    var second = vertical ? a.Map((-1, 0), (0, -1)) : a.Map((0, -1), (1, 0));
                    Leaf(a, a.Motif, centre, Math.Max(3, radius), Math.Max(1, radius / 3), anchor, first);
                    Leaf(a, a.Motif, centre, Math.Max(3, radius), Math.Max(1, radius / 3), anchor, second);
                    Seed(a, a.Detail, centre, 1, anchor);
                }
                else if (family is Family.Trellis or Family.Chevron)
                {
                    Trefoil(a, a.Motif, centre, Math.Max(3, radius), triangular, anchor);
                    Seed(a, a.Detail, centre, 1, anchor);
                }
                else if (family is Family.Fan or Family.Cloud)
                {
                    Fan(a, a.Motif, centre, Math.Max(3, radius), family == Family.Cloud, anchor);
                    Flower(a, a.Detail, centre, Math.Max(2, radius / 3), family == Family.Cloud ? 4 : 6, triangular, anchor);
                }
                else
                {
                    var petals = family == Family.Sixfold ? 6 : family == Family.Medallion ? 8 : 4;
                    Flower(a, a.Motif, centre, Math.Max(3, radius), petals, triangular, anchor);
                    Ring(a, a.Detail, centre, Math.Max(1, radius / 3), anchor);
                }
            }
            if (family == Family.Fret)
                foreach (var path in longPaths.Take(2)) GrowingLeaf(a, a.Motif, path, 3);
            description = "A repeated intermediate unit leads. Conventional leaves, tulips, trefoils, pinwheels or radial flowers reuse actual finite prototypes in explicit substitution images. Their centres and stems attach through public source landmarks with retained offsets. The original disposition is carried intact as the quieter construction.";
        }
        else
        {
            title = family switch
            {
                Family.Panel => "Border buds and fine stems", Family.Basket => "Woven edge seeds",
                Family.Medallion => "Corner fleurons and pearl centres", Family.Trellis => "Tip buds on the trellis",
                Family.Sixfold => "Small trefoils and six-seed centres", Family.Fan => "Radial buds",
                Family.Cloud => "Pearls on cloud courses", Family.Chevron => "Fine chevron seeds", _ => "Leaflets along the turns"
            };
            if (source.Id is "B15" or "B16" or "B17" or "B18")
            {
                // These small meshes have no room for a whole leaf pair at
                // every corner: six neighbouring images would merge into a
                // large fan. Keep one actual corner accent per macrocell.
                var corner = a.Port(a.Paths[0], 0); var position = a.Position(corner);
                Leaf(a, a.Detail, position, 2, 1, corner);
            }
            else
                foreach (var path in longPaths)
                    GrowingLeaf(a, a.Detail, path, family is Family.Cloud ? 3 : 2);
            foreach (var (centre, radius, path) in centres)
            {
                var anchor = a.NearestPort(centre, path?.Path);
                if (family is Family.Sixfold)
                {
                    var small = Math.Max(2, radius / 3);
                    Flower(a, a.Detail, centre, small, 3, triangular, anchor);
                    Seed(a, a.Motif, centre, 1, anchor);
                }
                else if (family is Family.Medallion or Family.Cloud or Family.Fan)
                    Ring(a, a.Detail, centre, Math.Max(1, radius / 4), anchor);
                else
                    Seed(a, a.Detail, centre, Math.Max(1, radius / 4), anchor);
            }
            description = "Small local enrichments lead. Paired finite leaflets grow at selected published turns and edge landmarks; buds, pearls or small trefoils punctuate the surviving repeat. The large medallions from the intermediate design are absent. The same skeleton remains, but the introduced path set is deliberately different.";
        }
        var enrichment = a.Finish();
        var pitch = Math.Min(DisplayLength(source, source.ColumnPitch), DisplayLength(source, source.RowPitch));
        var weight = Math.Clamp(pitch * (mode == "field" ? .033 : mode == "motif" ? .022 : .013), .055, mode == "field" ? .8 : mode == "motif" ? .55 : .32);
        var sourceNumber = int.Parse(source.Id.AsSpan(1, 2), System.Globalization.CultureInfo.InvariantCulture);
        var paletteChoice = (sourceNumber + palette + (mode == "field" ? 0 : mode == "motif" ? 3 : 7)) % 12;
        return new($"O-{source.Id}-{mode}", source.Id, mode, $"{source.Id} · {title}", source, enrichment,
            a.Field.Identity, a.Motif.Identity, a.Detail.Identity, description, paletteChoice, weight);
    }

    private static ((int X, int Y) Centre, int Radius, BourgoinOrnamentAuthor.PathRead? Path)[] Centres(BourgoinOrnamentAuthor a, string id)
    {
        // These are finite authoring choices, not recognition or a face solver.
        // Every resulting position becomes an anchored native offset word.
        if (id == "B01") return [((8, 4), 3, null), ((0, 12), 3, null)];
        if (id == "B07") return [((16, 16), 5, null)];
        var closed = a.Paths.Where(path => path.Closed && path.Width >= 4 && path.Height >= 4).Take(4).ToArray();
        if (closed.Length != 0) return closed.Select(path =>
        {
            var centre = path.Centre;
            // A triangle's box centre lies on its diagonal. A point average
            // selects its interior, while collinear side partitions are ignored.
            if (id is "B16" or "B24")
            {
                var corners = Corners(path.Vertices);
                centre = ((int)Math.Round(corners.Average(point => point.X)), (int)Math.Round(corners.Average(point => point.Y)));
            }
            var radius = Math.Clamp(Math.Min(path.Width, path.Height) / (id is "B16" or "B24" ? 5 : 3), 2, 12);
            return (centre, radius, (BourgoinOrnamentAuthor.PathRead?)path);
        }).ToArray();
        var selected = id == "B33" ? a.Paths.OrderBy(path => path.Vertices.Length).Take(2) :
            id == "B30" ? a.Paths.AsEnumerable() : a.Paths.OrderByDescending(path => path.Vertices.Length).Take(2);
        return selected.Select(path => (path.Centre, Math.Clamp(Math.Min(Math.Max(4, path.Width), Math.Max(4, path.Height)) / 3, 3, 12),
            (BourgoinOrnamentAuthor.PathRead?)path)).ToArray();
    }

    private static void Inset(BourgoinOrnamentAuthor a, BourgoinOrnamentAuthor.Layer layer,
        BourgoinOrnamentAuthor.PathRead path, int numerator, int denominator)
    {
        var centre = path.Centre;
        var points = Clean(path.Vertices.Select(point => (
            (point.X - centre.X) * numerator / denominator,
            (point.Y - centre.Y) * numerator / denominator)).ToArray());
        if (points.Length >= 4 && points[0] == points[^1])
            a.Place(layer, a.Prototype(true, points), centre, a.NearestPort(centre, path.Path));
    }

    private static void Companion(BourgoinOrnamentAuthor a, BourgoinOrnamentAuthor.Layer layer,
        BourgoinOrnamentAuthor.PathRead path, (int X, int Y) shift)
    {
        var start = path.Vertices[0];
        var points = path.Vertices.Select(point => (point.X - start.X, point.Y - start.Y)).ToArray();
        a.Place(layer, a.Prototype(false, points), (start.X + shift.X, start.Y + shift.Y), a.Port(path, 0));
    }

    private static void Leaf(BourgoinOrnamentAuthor a, BourgoinOrnamentAuthor.Layer layer, (int X, int Y) at,
        int length, int width, Relation anchor, QualitativePatternTransform? transform = null)
    {
        if (length <= 2)
        {
            // A short leaf has no distinct shoulder resolution. Preserve a
            // pointed lower-resolution contour instead of a flat-headed
            // trapezoid whose shoulder would coincide with its tip.
            a.Place(layer, a.Prototype(true, (0, 0), (-1, 1), (0, 2), (1, 1), (0, 0)), at, anchor, transform);
            return;
        }
        var half = Math.Max(1, width / 2);
        var shoulder = Math.Min(length - 1, Math.Max(2, length * 3 / 4));
        var middle = Math.Min(length - 1, Math.Max(2, length / 2));
        var points = Clean([(0, 0), (-half, Math.Max(1, length / 4)), (-width, middle),
            (-half, shoulder), (0, length), (half, shoulder),
            (width, middle), (half, Math.Max(1, length / 4)), (0, 0)]);
        a.Place(layer, a.Prototype(true, points), at, anchor, transform);
    }

    private static void Flower(BourgoinOrnamentAuthor a, BourgoinOrnamentAuthor.Layer layer, (int X, int Y) at,
        int radius, int petals, bool triangular, Relation anchor)
    {
        if (petals == 6 || petals == 3 && triangular)
        {
            (int X, int Y) horizontal = (1, 0), vertical = (0, 1);
            for (var index = 0; index < 6; index++)
            {
                if (petals == 6 || index % 2 == 0)
                    Leaf(a, layer, at, radius, Math.Max(1, radius / 3), anchor, a.Map(horizontal, vertical));
                horizontal = (-horizontal.Y, horizontal.X + horizontal.Y);
                vertical = (-vertical.Y, vertical.X + vertical.Y);
            }
        }
        else
        {
            (int X, int Y)[] directions = petals == 8
                ? [(0, 1), (-1, 1), (-1, 0), (-1, -1), (0, -1), (1, -1), (1, 0), (1, 1)]
                : [(0, 1), (-1, 0), (0, -1), (1, 0)];
            foreach (var vertical in directions)
                Leaf(a, layer, at, radius, Math.Max(1, radius / 3), anchor, a.Map((vertical.Y, -vertical.X), vertical));
        }
    }

    private static void Trefoil(BourgoinOrnamentAuthor a, BourgoinOrnamentAuthor.Layer layer,
        (int X, int Y) centre, int radius, bool triangular, Relation anchor)
    {
        if (triangular) { Flower(a, layer, centre, radius, 3, true, anchor); return; }
        foreach (var direction in new[] { (X: -1, Y: 1), (X: 0, Y: 1), (X: 1, Y: 1) })
            Leaf(a, layer, centre, radius, Math.Max(1, radius / 3), anchor, a.Map((direction.Y, -direction.X), direction));
    }

    private static void Tulip(BourgoinOrnamentAuthor a, BourgoinOrnamentAuthor.Layer layer,
        (int X, int Y) centre, int radius, bool vertical, Relation anchor)
    {
        var width = Math.Max(2, radius * 2 / 3); var half = Math.Max(1, width / 2);
        var points = Clean([(0, -radius), (-width, -radius / 2), (-width, radius / 3),
            (-half, radius), (0, radius * 2 / 3), (half, radius), (width, radius / 3), (width, -radius / 2), (0, -radius)]);
        a.Place(layer, a.Prototype(true, points), centre, anchor, vertical ? null : a.Map((0, 1), (-1, 0)));
    }

    private static void Cartouche(BourgoinOrnamentAuthor a, BourgoinOrnamentAuthor.Layer layer,
        (int X, int Y) centre, int radius, bool vertical, Relation anchor)
    {
        var longReach = radius * 2; var shortReach = Math.Max(1, radius * 2 / 3);
        var corner = Math.Max(1, shortReach / 2);
        var points = Clean([(-longReach + corner, -shortReach), (longReach - corner, -shortReach),
            (longReach, -shortReach + corner), (longReach, shortReach - corner),
            (longReach - corner, shortReach), (-longReach + corner, shortReach),
            (-longReach, shortReach - corner), (-longReach, -shortReach + corner),
            (-longReach + corner, -shortReach)]);
        a.Place(layer, a.Prototype(true, points), centre, anchor, vertical ? a.Map((0, 1), (-1, 0)) : null);
    }

    private static void Fan(BourgoinOrnamentAuthor a, BourgoinOrnamentAuthor.Layer layer,
        (int X, int Y) centre, int radius, bool round, Relation anchor)
    {
        var size = Math.Max(3, radius);
        foreach (var direction in new[] { (X: -1, Y: 1), (X: 0, Y: 1), (X: 1, Y: 1) })
            Leaf(a, layer, centre, size, Math.Max(1, size / (round ? 2 : 4)), anchor, a.Map((direction.Y, -direction.X), direction));
        a.Segment(layer, (centre.X, centre.Y - size / 2), centre, anchor);
    }

    private static void FanPanel(BourgoinOrnamentAuthor a, BourgoinOrnamentAuthor.Layer layer,
        (int X, int Y) centre, int radius, Relation anchor)
    {
        var size = Math.Max(3, radius); var shoulder = Math.Max(1, size / 2);
        var points = Clean([(0, -size), (-shoulder, -shoulder), (-size, 0),
            (-size, shoulder), (-shoulder, size), (0, size), (shoulder, size),
            (size, shoulder), (size, 0), (shoulder, -shoulder), (0, -size)]);
        a.Place(layer, a.Prototype(true, points), centre, anchor);
        a.Segment(layer, (centre.X, centre.Y - size), (centre.X, centre.Y + size), anchor);
    }

    private static void Seed(BourgoinOrnamentAuthor a, BourgoinOrnamentAuthor.Layer layer,
        (int X, int Y) at, int radius, Relation anchor) => a.Place(layer,
        a.Prototype(true, (0, radius), (radius, 0), (0, -radius), (-radius, 0), (0, radius)), at, anchor);

    private static void Ring(BourgoinOrnamentAuthor a, BourgoinOrnamentAuthor.Layer layer,
        (int X, int Y) at, int radius, Relation anchor)
    {
        if (radius < 2) { Seed(a, layer, at, 1, anchor); return; }
        var half = Math.Max(1, radius / 2);
        a.Place(layer, a.Prototype(true, (radius, 0), (radius, half), (half, radius), (0, radius),
            (-half, radius), (-radius, half), (-radius, 0), (-radius, -half), (-half, -radius),
            (0, -radius), (half, -radius), (radius, -half), (radius, 0)), at, anchor);
    }

    private static void Stem(BourgoinOrnamentAuthor a, BourgoinOrnamentAuthor.Layer layer,
        (int X, int Y) centre, int radius, Relation anchor) =>
        a.Segment(layer, (centre.X, centre.Y - radius), (centre.X, centre.Y + radius * 2 / 3), anchor);

    private static void BudPair(BourgoinOrnamentAuthor a, BourgoinOrnamentAuthor.Layer layer,
        (int X, int Y) at, int length, Relation anchor)
    {
        Leaf(a, layer, at, length, 1, anchor, a.Map((1, -1), (1, 1)));
        Leaf(a, layer, at, length, 1, anchor, a.Map((1, 1), (-1, 1)));
    }

    private static void GrowingLeaf(BourgoinOrnamentAuthor a, BourgoinOrnamentAuthor.Layer layer,
        BourgoinOrnamentAuthor.PathRead path, int length)
    {
        // Select actual interior landmarks, with a deterministic coarse cap.
        // No code-created crossing or inferred point is installed as a port.
        var count = path.Vertices.Length - 1;
        foreach (var index in new[] { Math.Max(0, count / 3), Math.Min(count - 1, count * 2 / 3) }.Distinct())
        {
            var port = a.Port(path, index); var at = a.Position(port);
            var next = path.Vertices[index + 1]; var dx = Math.Sign(next.X - at.X); var dy = Math.Sign(next.Y - at.Y);
            if (dx == 0 && dy == 0) continue;
            var horizontal = (X: dx == 0 ? 1 : -dy, Y: dx == 0 ? 0 : dx);
            var direction = (X: dx, Y: dy);
            if (dx == 0) horizontal = (dy, 0);
            Leaf(a, layer, at, length, 1, port, a.Map(horizontal, (direction.X + horizontal.X, direction.Y + horizontal.Y)));
            Leaf(a, layer, at, length, 1, port, a.Map((-horizontal.X, -horizontal.Y), (direction.X - horizontal.X, direction.Y - horizontal.Y)));
            a.Segment(layer, at, (at.X + dx * length, at.Y + dy * length), port);
        }
    }

    private static (int X, int Y)[] Clean((int X, int Y)[] vertices)
    {
        var result = new List<(int X, int Y)>();
        foreach (var point in vertices) if (result.Count == 0 || result[^1] != point) result.Add(point);
        return result.ToArray();
    }

    private static (int X, int Y)[] Corners((int X, int Y)[] vertices) => vertices.Take(vertices.Length - 1)
        .Where((point, index) =>
        {
            var previous = vertices[(index + vertices.Length - 2) % (vertices.Length - 1)];
            var next = vertices[index + 1];
            return (long)(point.X - previous.X) * (next.Y - point.Y) != (long)(point.Y - previous.Y) * (next.X - point.X);
        }).ToArray();

    private static double DisplayLength(BourgoinPatternLibrary.Recipe source, Relation word)
    {
        var x = 0; var y = 0;
        foreach (var move in N.E(word).Cast<Relation>())
        {
            if (!source.Frame.Direction(move, out var axis, out var forward, out _)) continue;
            if (ReferenceEquals(axis, source.Frame.Horizontal)) x += forward ? 1 : -1; else y += forward ? 1 : -1;
        }
        var basis = source.DisplayBasis;
        var dx = x * basis.RX + y * basis.UX; var dy = x * basis.RY + y * basis.UY;
        return Math.Sqrt(dx * dx + dy * dy);
    }
}
