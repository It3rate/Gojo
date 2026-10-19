using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;
using N = Gojo.Experiments.QualitativeFrieze.FieldNative;

namespace Gojo.Experiments.QualitativeFrieze;

/// <summary>A second authored catalogue. The retained source, additions,
/// attachment witnesses and presentation selections are all native. This
/// library does not replace the earlier catalogue or discover a beauty law.</summary>
public static class BourgoinOrnamentRefinedLibrary
{
    public sealed record RefinedDesign(BourgoinOrnamentDesignLibrary.Design Design,
        Relation CurveHints, string ConstructionNotes);

    public static IReadOnlyList<RefinedDesign> Create() => new BourgoinPatternLibrary(8).Recipes
        .SelectMany((source, index) => Create(source, index, ["field", "motif", "detail"])).ToArray();

    public static IReadOnlyList<RefinedDesign> Create(BourgoinPatternLibrary.Recipe source, int paletteIndex,
        IEnumerable<string> modes)
    {
        var selected = modes.Distinct(StringComparer.Ordinal).ToArray();
        if (selected.Any(mode => mode is not ("field" or "motif" or "detail")))
            throw new ArgumentException("Modes are field, motif or detail.", nameof(modes));
        return selected.Select(mode => Build(source, paletteIndex, mode)).ToArray();
    }

    private static RefinedDesign Build(BourgoinPatternLibrary.Recipe source, int palette, string mode)
    {
        var p = new Painter(source); string title; string notes;
        switch (source.Id)
        {
            case "B01":
                Brick(p, mode); title = "Brick panels and growing sprays";
                notes = "The two offset brick interiors carry horizontal cartouches or narrow rooted sprays. Leaf growth stays within the course; the running-bond joints remain readable.";
                break;
            case "B02":
                Basket(p, mode); title = "Woven strips with leaf and vein";
                notes = "Each actual rectangle owns its stem, leaves and veins through its own public port. Its quarter-related long axis determines the ornament; emergent square gaps stay quiet.";
                break;
            case "B05":
                HexFlower(p, mode); title = "Hexagonal lotus and inward buds";
                notes = "The same six-side source supports inset enamel, an inward budwheel or an outward veined lotus. All additions stay inside the source hexagon and leave triangular interstices undecorated.";
                break;
            case "B07":
                Bands(p, mode); title = "Orthogonal bands and running foliage";
                notes = "Narrow horizontal and vertical strips carry separate running stems, with the central crossing left uncluttered. A broad intermediate leaf occupies the larger existing compartment.";
                break;
            case "B08":
            case "B08.B":
                Ribbon(p, mode); title = "Diagonal ribbon sprays";
                notes = "Actual source direction words supply two oblique strip bases. Foliage follows those bases with restrained transverse reach; the crossing and larger diamond gaps remain visible.";
                break;
            case "B15":
            case "B16":
            case "B17":
            case "B18":
                Mesh(p, mode); title = "Fine lattice grain and guided leaves";
                notes = "The small mesh receives one contained leaf or narrow leaf images along actual half-diagonal guides. No fan of corner leaves accumulates at a shared lattice vertex.";
                break;
            case "B28":
                Fans(p, mode); title = "Graded lotus fans";
                notes = "Each actual shell owns a broad inset fan, a graded five-lobed lotus, or fine rib-bound foliage. The common fan base and its spreading construction remain visible.";
                break;
            case "B30":
                Clouds(p, mode); title = "Cloud valleys and round foliage";
                notes = "Angular cloud companions lead the overall design. Paired round leaves and a rooted pearl occupy each existing valley in the intermediate design; fine leaves follow the actual arch courses.";
                break;
            case "B27":
                Chevrons(p, mode); title = "Sprouting chevron courses";
                notes = "The paired ridges carry companion bands, an apex-rooted three-leaf spray, or smaller edge buds. The intervening diamond remains a subordinate window.";
                break;
            case "B31":
                Arms(p, mode); title = "Outward arms with wrist foliage";
                notes = "Growth follows actual arm chords, while the round source marker remains an unfilled window. The larger overall construction uses exact angular companion arms.";
                break;
            default:
                General(p, mode); title = GeneralTitle(source.Id, mode);
                notes = "The leading scale changes the introduced structure: broad inset construction, repeated botanical units, or finer leaves and veins following actual source courses. Old source paths, ports, periods and seams remain intact.";
                break;
        }
        var enrichment = p.Author.Finish();
        var hint = new OrnamentCurveSelection(enrichment.Result, N.S(p.Curves.ToArray()), N.S(p.Fixed.ToArray()));
        if (!hint.IsComplete) throw new InvalidOperationException("A curve hint must select actual final paths and public landmarks.");
        var factor = mode == "field" ? .028 : mode == "motif" ? .019 : .011;
        var pitch = Math.Min(Length(source, source.ColumnPitch), Length(source, source.RowPitch));
        var weight = Math.Clamp(pitch * factor, .055, mode == "field" ? .7 : mode == "motif" ? .48 : .28);
        var number = int.Parse(source.Id.AsSpan(1, 2));
        var design = new BourgoinOrnamentDesignLibrary.Design($"R-{source.Id}-{mode}", source.Id, mode,
            $"{source.Id} · {title}", source, enrichment, p.Author.Field.Identity, p.Author.Motif.Identity,
            p.Author.Detail.Identity, notes, (number * 5 + palette + (mode == "field" ? 0 : mode == "motif" ? 3 : 7)) % 12, weight);
        return new(design, N.S(hint.Surface), notes +
            " Optional curved display selects only intended flowing paths and retains actual roots, tips and seam landmarks; native execution remains finite chords.");
    }

    private static void Brick(Painter p, string mode)
    {
        foreach (var centre in new[] { (X: 16, Y: 8), (X: 0, Y: 24) })
        {
            if (mode == "field")
                p.Contour(p.Author.Motif, centre, [(-12, -4), (12, -4), (14, -2), (14, 2), (12, 4), (-12, 4), (-14, 2), (-14, -2), (-12, -4)]);
            else if (mode == "motif")
            {
                p.Leaf((centre.X - 9, centre.Y), 18, 3, p.Author.Map((0, 1), (1, 0)), forks: true);
                p.Pearl(centre, 1);
            }
            else
            {
                var at = (centre.X - 16, centre.Y);
                p.Line(p.Author.Motif, at, [(0, 0), (6, 0), (12, 1), (20, 0), (28, 0)], curve: true, fixedVertices: [1, 3]);
                p.Leaf((at.Item1 + 6, at.Y), 4, 1, forks: false);
                p.Leaf((at.Item1 + 20, at.Y), 4, 1, p.Author.Map((1, 0), (0, -1)), forks: false);
            }
        }
    }

    private static void Basket(Painter p, string mode)
    {
        foreach (var path in p.Author.Paths.Where(path => path.Closed))
        {
            var vertical = path.Height > path.Width; var c = path.Centre;
            var map = vertical ? p.Author.Map((1, 0), (0, 1)) : p.Author.Map((0, 1), (-1, 0));
            var anchor = p.Author.NearestPort(c, path.Path);
            if (mode == "field")
            {
                p.Contour(p.Author.Motif, c, [(-4, -12), (4, -12), (5, -10), (5, 10), (4, 12), (-4, 12), (-5, 10), (-5, -10), (-4, -12)], map, anchor);
                p.Pearl(c, 1, anchor);
            }
            else if (mode == "motif")
            {
                p.Leaf(Place(vertical, c, (0, -12)), 24, 4, map, true, anchor);
                foreach (var y in new[] { -5, 0, 5 })
                    p.Line(p.Author.Detail, c, [(-2, y + 2), (0, y), (2, y + 2)], map, anchor);
            }
            else
            {
                p.Line(p.Author.Motif, c, [(0, -12), (0, -8), (0, -2), (0, 4), (0, 12)], map, anchor, true, [1, 2, 3]);
                foreach (var (y, side) in new[] { (-8, 1), (-2, -1), (4, 1) })
                {
                    var leafMap = vertical ? p.Author.Map((side, -1), (side, 1)) : p.Author.Map((1, side), (-1, side));
                    p.Leaf(Place(vertical, c, (0, y)), 4, 1, leafMap, false, anchor);
                }
                p.Pearl(Place(vertical, c, (0, 11)), 1, anchor);
            }
        }
    }

    private static void HexFlower(Painter p, string mode)
    {
        if (mode == "field")
        {
            p.Inset(p.Author.Paths.Single(path => path.Closed), 3, 4);
            p.Pearl((0, 0), 4);
        }
        else if (mode == "motif")
        {
            var maps = p.SixMaps();
            for (var index = 0; index < Six.Length; index++)
            {
                var vector = Six[index]; var root = Scale(vector, 12);
                p.Line(p.Author.Motif, Scale(vector, 16), [(0, 0), Scale(vector, -4)]);
                p.Leaf(root, 8, 1, maps[(index + 2) % 6], forks: false);
            }
        }
        else
        {
            foreach (var map in p.SixMaps()) p.Leaf((0, 0), 10, 2, map, forks: true);
            p.Pearl((0, 0), 2);
        }
    }

    private static void Bands(Painter p, string mode)
    {
        if (mode == "field")
            p.Contour(p.Author.Motif, (32, 32), [(-10, -8), (10, -8), (12, -6), (12, 6), (10, 8), (-10, 8), (-12, 6), (-12, -6), (-10, -8)]);
        else if (mode == "motif")
        {
            p.Line(p.Author.Motif, (16, 32), [(0, 0), (8, -8), (16, -8)]);
            p.Leaf((32, 24), 16, 4, forks: true);
        }
        else
        {
            p.Line(p.Author.Motif, (16, 8), [(0, 0), (8, 1), (16, 0), (24, -1), (32, 0)], curve: true, fixedVertices: [1, 3]);
            p.Line(p.Author.Motif, (8, 16), [(0, 0), (-1, 8), (0, 16), (1, 24), (0, 32)], curve: true, fixedVertices: [1, 3]);
            p.Leaf((24, 9), 4, 1, forks: false);
            p.Leaf((40, 7), 4, 1, p.Author.Map((1, 0), (0, -1)), forks: false);
            p.Leaf((7, 24), 4, 1, p.Author.Map((0, -1), (-1, 0)), forks: false);
            p.Leaf((9, 40), 4, 1, p.Author.Map((0, 1), (1, 0)), forks: false);
        }
    }

    private static void Ribbon(Painter p, string mode)
    {
        static (int X, int Y) At(int u, int v) => (2 * u - 2 * v, u + v);
        var a = p.Author;
        if (mode == "field")
        {
            p.Contour(a.Motif, At(20, 20), [(-10, 0), (0, 5), (10, 0), (0, -5), (-10, 0)]);
            p.Pearl(At(20, 20), 2);
        }
        else if (mode == "motif")
        {
            p.Leaf(At(20, 20), 10, 2, a.Map((-2, 1), (2, 1)), forks: true);
            p.Leaf(At(20, 20), 10, 2, a.Map((2, -1), (-2, -1)), forks: true);
            p.Pearl(At(20, 20), 1);
        }
        else
        {
            p.Line(a.Motif, At(0, 4), [(0, 0), (16, 8), (32, 16), (48, 24), (64, 32)], curve: true, fixedVertices: [1, 3]);
            p.Line(a.Motif, At(4, 0), [(0, 0), (-16, 8), (-32, 16), (-48, 24), (-64, 32)], curve: true, fixedVertices: [1, 3]);
            foreach (var u in new[] { 12, 24 })
            {
                p.Leaf(At(u, 4), 3, 1, a.Map((2, 1), (-2, 1)), forks: false);
                p.Leaf(At(4, u), 3, 1, a.Map((-2, 1), (2, 1)), forks: false);
            }
        }
    }

    private static void Mesh(Painter p, string mode)
    {
        var id = p.Source.Id;
        if (mode == "field")
            foreach (var closed in p.Author.Paths.Where(path => path.Closed)) p.Inset(closed, 3, 4);
        else if (mode == "detail" && id is "B17" or "B18")
        {
            foreach (var guide in p.Author.Paths.Where(path => !path.Closed))
                p.Follow(guide, maxLeaves: 2, maximumLength: 4);
        }
        else
        {
            foreach (var cell in p.Author.Paths.Where(path => path.Closed))
            {
                var c = Centre(cell, triangularCell: id == "B16");
                if (id == "B15")
                    p.Leaf((c.X - 6, c.Y), 12, mode == "detail" ? 1 : 2, p.Author.Map((-1, 2), (1, 0)), forks: true, anchor: p.Author.NearestPort(c, cell.Path));
                else if (id == "B16")
                {
                    if (mode == "motif") p.Rosette(c, 3, 3, 1, cell.Path);
                    else p.Leaf((c.X, c.Y - 2), 4, 1, forks: false, anchor: p.Author.NearestPort(c, cell.Path));
                }
                else
                    p.Rosette(c, 4, 5, 1, cell.Path);
            }
        }
    }

    private static void General(Painter p, string mode)
    {
        var source = p.Source; var closed = p.Author.Paths.Where(path => path.Closed).ToArray();
        var open = p.Author.Paths.Where(path => !path.Closed).ToArray();
        var sixfold = source.Id is "B04" or "B20" or "B24" or "B25" or "B26" or "B29";
        var fret = source.Id is "B09" or "B10" or "B11" or "B12" or "B13" or "B14" or "B31" or "B32" or "B33" or "B34";
        if (mode == "field")
        {
            var cells = closed.Take(4).ToArray();
            var sourceChords = p.Author.Paths.Sum(path => path.Vertices.Length - 1);
            var insetChords = cells.Sum(path => path.Vertices.Length - 1);
            // Keep the complete sibling set symmetric; complex fret panels get
            // quiet seeds instead of several additional full fret contours.
            if (sourceChords + insetChords + cells.Length * 12 <= 200)
                foreach (var boundary in cells) p.Inset(boundary, 3, 4);
            if (closed.Length == 0)
                foreach (var course in open.OrderByDescending(path => path.Vertices.Length).Take(2))
                {
                    p.Companion(course, (0, 3));
                    var c = course.Centre;
                    var r = Math.Clamp(Math.Min(course.Width, course.Height) / 6, 4, 16);
                    p.Contour(p.Author.Motif, c, [(0, -r), (r, 0), (0, r), (-r, 0), (0, -r)],
                        anchor: p.Author.NearestPort(c, course.Path));
                    p.Pearl(c, 1, p.Author.NearestPort(c, course.Path));
                }
            else
                foreach (var cell in closed.Take(4)) p.Pearl(Centre(cell, source.Id == "B24"), 2);
        }
        else if (mode == "motif")
        {
            if (closed.Length == 0)
            {
                foreach (var course in open.OrderByDescending(path => path.Vertices.Length).Take(2))
                    p.FlowerSpray(course);
                return;
            }
            foreach (var cell in closed.Take(4))
            {
                var centre = Centre(cell, source.Id == "B24");
                var size = Math.Clamp(Math.Min(cell.Width, cell.Height) / 4, 4, 14);
                if (source.Id == "B03")
                    p.Rosette(centre, cell.Width > 16 ? 8 : 4, cell.Width > 16 ? 8 : 4, 1, cell.Path, spines: cell.Width > 16);
                else if (fret)
                {
                    p.Fleuron(centre, Math.Clamp(Math.Min(cell.Width, cell.Height) / 6, 4, 10), cell.Path);
                    p.Follow(cell, 1, 7);
                }
                else if (source.Id is "B19" or "B21" or "B22" or "B23")
                    p.CellLeaf(cell, size * 2, mode == "motif" ? 2 : 1);
                else
                    p.Rosette(centre, sixfold ? closed.Length > 1 ? 3 : 6 : 4, size, 1, cell.Path);
            }
        }
        else if (source.Id == "B26")
        {
            foreach (var ray in open.Where(path => Greatest(Math.Abs(path.Vertices[^1].X - path.Vertices[0].X),
                Math.Abs(path.Vertices[^1].Y - path.Vertices[0].Y)) >= 16))
                p.Follow(ray, 1, 10);
            p.Pearl((0, 0), 2);
        }
        else if (open.Length != 0)
        {
            foreach (var course in open.Take(6)) p.Follow(course, source.Id == "B30" ? 1 : 2, 6);
            if (source.Id == "B31") p.Pearl((0, 0), 3);
        }
        else if (fret)
        {
            foreach (var course in closed.Take(4)) p.Follow(course, 2, 5);
        }
        else
        {
            foreach (var cell in closed.Take(4))
            {
                if (source.Id is "B03" or "B06" or "B06.B")
                    p.Rosette(Centre(cell, false), 4, Math.Clamp(Math.Min(cell.Width, cell.Height) / 5, 3, 10), 1, cell.Path);
                else p.CellLeaf(cell, Math.Clamp(Math.Max(cell.Width, cell.Height) / 2, 4, 16), 1);
            }
        }
    }

    private static void Fans(Painter p, string mode)
    {
        foreach (var shell in p.Author.Paths.Where(path => path.Closed))
        {
            var root = shell.Vertices.OrderBy(point => point.Y).First();
            var anchor = p.Author.NearestPort(root, shell.Path);
            if (mode == "field")
            {
                p.Inset(shell, 3, 4);
                p.Contour(p.Author.Motif, root, [(0, 3), (-20, 29), (-13, 33), (0, 35), (13, 33), (20, 29), (0, 3)], anchor: anchor);
                p.Pearl((root.X, root.Y + 23), 3, anchor);
            }
            else if (mode == "motif")
            {
                p.Leaf((root.X, root.Y + 3), 28, 3, anchor: anchor);
                foreach (var side in new[] { -1, 1 })
                {
                    p.Leaf((root.X, root.Y + 3), 16, 2, p.Author.Map((1, 0), (side, 2)), false, anchor);
                    p.Leaf((root.X, root.Y + 3), 16, 2, p.Author.Map((1, 0), (side, 1)), false, anchor);
                }
                p.Pearl((root.X, root.Y + 3), 2, anchor);
            }
            else
            {
                foreach (var rib in p.Author.Paths.Where(path => !path.Closed && path.Vertices[0] == root))
                    p.Follow(rib, 1, 7);
                p.Pearl((root.X, root.Y + 26), 2, anchor);
            }
        }
    }

    private static void Clouds(Painter p, string mode)
    {
        foreach (var course in p.Author.Paths)
        {
            if (mode == "field") { p.Companion(course, (0, -3)); continue; }
            if (mode == "detail") { p.Follow(course, 1, 7); continue; }
            var root = course.Vertices.OrderBy(point => point.Y).First();
            var anchor = p.Author.NearestPort(root, course.Path);
            var at = (root.X, root.Y + 3);
            p.Line(p.Author.Motif, root, [(0, 0), (0, 3)], anchor: anchor);
            foreach (var side in new[] { -1, 1 })
                p.Leaf(at, 20, 4, p.Author.Map((side, -1), (side, 1)), false, anchor);
            p.Pearl(at, 2, anchor);
        }
    }

    private static void Chevrons(Painter p, string mode)
    {
        var ridges = p.Author.Paths.Where(path => !path.Closed).Take(2).ToArray();
        foreach (var ridge in ridges)
        {
            if (mode == "field") p.Companion(ridge, (0, -2));
            else if (mode == "detail") p.Follow(ridge, 1, 5);
        }
        if (mode == "motif")
        {
            var ridge = ridges[0]; var apex = ridge.Vertices[1]; var anchor = p.Author.Port(ridge, 1);
            p.Leaf(apex, 10, 2, forks: true, anchor: anchor);
            foreach (var side in new[] { -1, 1 })
                p.Leaf(apex, 6, 1, p.Author.Map((1, 0), (side, 1)), false, anchor);
            p.Pearl(apex, 1, anchor);
        }
        else
            foreach (var cell in p.Author.Paths.Where(path => path.Closed)) p.Pearl(cell.Centre, 1);
    }

    private static void Arms(Painter p, string mode)
    {
        foreach (var arm in p.Author.Paths.Where(path => !path.Closed).Take(4))
        {
            if (mode == "field") p.Companion(arm, (1, 1));
            else p.Follow(arm, mode == "motif" ? 1 : 2, mode == "motif" ? 12 : 5, mode == "motif" ? 3 : 1);
        }
    }

    private static string GeneralTitle(string id, string mode) => mode switch
    {
        "field" => "Inset fields and quiet construction",
        "motif" => id is "B04" or "B20" or "B24" or "B25" or "B26" or "B29" ? "Repeated lotus and trefoil units" : "Conventional leaves and medallions",
        _ => "Veins and foliage following the scaffold"
    };

    private static readonly (int X, int Y)[] Six = [(1, 0), (0, 1), (-1, 1), (-1, 0), (0, -1), (1, -1)];
    private static (int X, int Y) Scale((int X, int Y) value, int count) => (value.X * count, value.Y * count);
    private static (int X, int Y) Place(bool vertical, (int X, int Y) c, (int X, int Y) value) =>
        vertical ? (c.X + value.X, c.Y + value.Y) : (c.X - value.Y, c.Y + value.X);
    private static int Greatest(int a, int b) { while (b != 0) { var r = a % b; a = b; b = r; } return a; }
    private static (int X, int Y) Centre(BourgoinOrnamentAuthor.PathRead path, bool triangularCell)
    {
        if (!triangularCell) return path.Centre;
        var vertices = path.Vertices.Take(path.Vertices.Length - 1).Where((point, index) =>
        {
            var before = path.Vertices[(index + path.Vertices.Length - 2) % (path.Vertices.Length - 1)];
            var after = path.Vertices[index + 1];
            return (long)(point.X - before.X) * (after.Y - point.Y) != (long)(point.Y - before.Y) * (after.X - point.X);
        }).ToArray();
        return ((int)Math.Round(vertices.Average(point => point.X)), (int)Math.Round(vertices.Average(point => point.Y)));
    }

    private sealed class Painter
    {
        public BourgoinPatternLibrary.Recipe Source { get; }
        public BourgoinOrnamentAuthor Author { get; }
        public List<Relation> Curves { get; } = [];
        public List<Relation> Fixed { get; } = [];
        public Painter(BourgoinPatternLibrary.Recipe source) { Source = source; Author = new(source); }

        public void Contour(BourgoinOrnamentAuthor.Layer layer, (int X, int Y) at, (int X, int Y)[] vertices,
            QualitativePatternTransform? map = null, Relation? anchor = null, bool curve = false, int[]? fixedVertices = null) =>
            Draw(layer, Author.Prototype(true, Clean(vertices)), at, map, anchor, curve, fixedVertices ?? []);
        public void Line(BourgoinOrnamentAuthor.Layer layer, (int X, int Y) at, (int X, int Y)[] vertices,
            QualitativePatternTransform? map = null, Relation? anchor = null, bool curve = false, int[]? fixedVertices = null) =>
            Draw(layer, Author.Prototype(false, Clean(vertices)), at, map, anchor, curve, fixedVertices ?? []);

        private void Draw(BourgoinOrnamentAuthor.Layer layer, UnitArrangementProgram prototype, (int X, int Y) at,
            QualitativePatternTransform? map, Relation? anchor, bool curve, int[] fixedVertices)
        {
            Author.Place(layer, prototype, at, anchor, map);
            if (!curve) return;
            var additions = (Relation)((Relation)((Relation)layer.Definition.Right).Right).Left;
            var actual = (Relation)N.E(additions).Last();
            var declaration = (Relation)actual.Right; var pathsPorts = (Relation)declaration.Left;
            var path = (Relation)N.E((Relation)pathsPorts.Left).Single();
            var ports = N.E((Relation)pathsPorts.Right).Cast<Relation>().ToArray();
            Curves.Add(path); var words = N.E((Relation)path.Right);
            foreach (var index in fixedVertices.Distinct())
            {
                var selected = index == words.Length ? path : words[index];
                var port = ports.Single(port => port.Right is Relation pick && ReferenceEquals(pick.Left, path) && ReferenceEquals(pick.Right, selected));
                Fixed.Add(port);
            }
        }

        public void Leaf((int X, int Y) at, int length, int width, QualitativePatternTransform? map = null,
            bool forks = true, Relation? anchor = null, bool spine = true)
        {
            length = Math.Max(3, length);
            var q = Math.Max(1, length / 3); var high = Math.Max(q + 1, length * 2 / 3);
            Contour(Author.Motif, at, [(0, 0), (-width, q), (-width, high), (0, length), (width, high), (width, q), (0, 0)],
                map, anchor, curve: true, fixedVertices: [0, 3]);
            if (spine) Line(Author.Detail, at, [(0, 0), (0, length)], map, anchor);
            if (forks && width > 1)
                Line(Author.Detail, at, [(-width + 1, high), (0, q), (width - 1, high)], map, anchor);
        }

        public void Pearl((int X, int Y) at, int radius, Relation? anchor = null)
        {
            if (radius == 1)
            { Contour(Author.Detail, at, [(1, 0), (0, 1), (-1, 0), (0, -1), (1, 0)], anchor: anchor); return; }
            var h = Math.Max(1, radius / 2);
            Contour(Author.Detail, at, [(radius, 0), (radius, h), (h, radius), (0, radius), (-h, radius), (-radius, h),
                (-radius, 0), (-radius, -h), (-h, -radius), (0, -radius), (h, -radius), (radius, -h), (radius, 0)], anchor: anchor, curve: true);
        }

        public void Inset(BourgoinOrnamentAuthor.PathRead path, int numerator, int denominator)
        {
            var c = path.Centre;
            var points = path.Vertices.Select(point => ((point.X - c.X) * numerator / denominator, (point.Y - c.Y) * numerator / denominator)).ToArray();
            Contour(Author.Motif, c, points, anchor: Author.NearestPort(c, path.Path));
        }

        public void CellLeaf(BourgoinOrnamentAuthor.PathRead path, int length, int width)
        {
            var c = Centre(path, Source.Id is "B24" or "B16"); var vertical = path.Height >= path.Width;
            length = Math.Min(length, Math.Max(4, (vertical ? path.Height : path.Width) * 2 / 3));
            var root = vertical ? (c.X, c.Y - length / 2) : (c.X - length / 2, c.Y);
            Leaf(root, length, width, vertical ? null : Author.Map((0, 1), (1, 0)),
                anchor: Author.NearestPort(c, path.Path));
        }

        public void Rosette((int X, int Y) centre, int petals, int length, int width, Relation? parent = null, bool spines = true)
        {
            var anchor = Author.NearestPort(centre, parent);
            var maps = petals is 3 or 6 ? SixMaps().Where((_, index) => petals == 6 || index % 2 == 0).ToArray() : petals == 8 ?
                new[] { Author.Map((1, 0), (0, 1)), Author.Map((1, 1), (-1, 1)), Author.Map((0, 1), (-1, 0)),
                    Author.Map((-1, 1), (-1, -1)), Author.Map((-1, 0), (0, -1)), Author.Map((-1, -1), (1, -1)),
                    Author.Map((0, -1), (1, 0)), Author.Map((1, -1), (1, 1)) } :
                new[] { Author.Map((1, 0), (0, 1)), Author.Map((0, 1), (-1, 0)),
                    Author.Map((-1, 0), (0, -1)), Author.Map((0, -1), (1, 0)) };
            for (var i = 0; i < maps.Length; i++)
                Leaf(centre, petals == 8 && i % 2 != 0 ? Math.Max(3, length * 2 / 3) : length,
                    width, maps[i], forks: false, anchor: anchor, spine: spines);
            Pearl(centre, 1, anchor);
        }

        public void Companion(BourgoinOrnamentAuthor.PathRead path, (int X, int Y) offset)
        {
            var start = path.Vertices[0];
            Line(Author.Field, (start.X + offset.X, start.Y + offset.Y),
                path.Vertices.Select(point => (point.X - start.X, point.Y - start.Y)).ToArray(),
                anchor: Author.Port(path, 0));
        }

        public void FlowerSpray(BourgoinOrnamentAuthor.PathRead path)
        {
            var c = path.Centre; var anchor = Author.NearestPort(c, path.Path);
            var radius = Math.Clamp(Math.Min(path.Width, path.Height) / 5, 5, 16);
            var foot = (c.X - radius, c.Y - radius);
            Line(Author.Motif, foot, [(0, 0), (radius / 2, radius / 3), (radius, radius)],
                anchor: anchor, curve: true, fixedVertices: [1]);
            Rosette(c, 4, radius, Math.Max(2, radius / 4), path.Path);
            Leaf(foot, Math.Max(5, radius * 2 / 3), 2, Author.Map((1, 0), (-1, 1)), false, anchor);
            Leaf(foot, Math.Max(5, radius * 2 / 3), 2, Author.Map((-1, 0), (1, -1)), false, anchor);
        }

        public void Fleuron((int X, int Y) centre, int radius, Relation parent)
        {
            var anchor = Author.NearestPort(centre, parent);
            Contour(Author.Motif, centre, [(0, radius), (-1, 1), (-radius, 0), (-1, -1),
                (0, -radius), (1, -1), (radius, 0), (1, 1), (0, radius)],
                anchor: anchor, curve: true, fixedVertices: [0, 2, 4, 6]);
            Line(Author.Detail, centre, [(-radius + 1, 0), (radius - 1, 0)], anchor: anchor);
            Line(Author.Detail, centre, [(0, -radius + 1), (0, radius - 1)], anchor: anchor);
            Pearl(centre, 1, anchor);
        }

        public QualitativePatternTransform[] SixMaps()
        {
            (int X, int Y) h = (1, 0), v = (0, 1); var maps = new List<QualitativePatternTransform>();
            for (var index = 0; index < 6; index++)
            { maps.Add(Author.Map(h, v)); h = (-h.Y, h.X + h.Y); v = (-v.Y, v.X + v.Y); }
            return maps.ToArray();
        }

        public void Follow(BourgoinOrnamentAuthor.PathRead path, int maxLeaves, int maximumLength, int width = 1)
        {
            var edges = Enumerable.Range(0, path.Vertices.Length - 1).Select(index =>
            {
                var first = path.Vertices[index]; var last = path.Vertices[index + 1];
                var dx = last.X - first.X; var dy = last.Y - first.Y;
                var count = Greatest(Math.Abs(dx), Math.Abs(dy));
                return (Index: index, Start: first, Count: count, V: count == 0 ? (X: 0, Y: 0) : (X: dx / count, Y: dy / count));
            }).Where(edge => edge.Count >= 7).OrderByDescending(edge => edge.Count).Take(maxLeaves);
            foreach (var edge in edges)
            {
                var inset = Math.Max(2, edge.Count / 5); var length = Math.Min(maximumLength, edge.Count - 2 * inset);
                if (length < 3) continue;
                var root = (edge.Start.X + edge.V.X * inset, edge.Start.Y + edge.V.Y * inset);
                var transverse = Math.Abs(edge.V.Y) > Math.Abs(edge.V.X) ? (1, 0) : (0, 1);
                Leaf(root, length, width, Author.Map(transverse, edge.V), forks: width > 1,
                    anchor: Author.Port(path, edge.Index), spine: false);
            }
        }

        private static (int X, int Y)[] Clean((int X, int Y)[] points) =>
            points.Where((point, index) => index == 0 || point != points[index - 1]).ToArray();
    }

    private static double Length(BourgoinPatternLibrary.Recipe source, Relation word)
    {
        var x = 0; var y = 0;
        foreach (var move in N.E(word).Cast<Relation>())
        {
            if (!source.Frame.Direction(move, out var axis, out var forward, out _)) continue;
            if (ReferenceEquals(axis, source.Frame.Horizontal)) x += forward ? 1 : -1; else y += forward ? 1 : -1;
        }
        var b = source.DisplayBasis; var dx = x * b.RX + y * b.UX; var dy = x * b.RY + y * b.UY;
        return Math.Sqrt(dx * dx + dy * dy);
    }
}
