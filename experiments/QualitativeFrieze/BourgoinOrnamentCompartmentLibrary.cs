using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;
using N = Gojo.Experiments.QualitativeFrieze.FieldNative;

namespace Gojo.Experiments.QualitativeFrieze;

/// <summary>Room-filling interpretations of eight retained skeletons. The
/// authoring coordinates select finite words; final paths, actual source
/// attachments, prototype maps and palette/curve selections are native.
/// Painted covers are presentation, not a claim that native faces were cut.</summary>
public static class BourgoinOrnamentCompartmentLibrary
{
    public static readonly string[] SourceIds = ["B08", "B08.B", "B09", "B10", "B11", "B12", "B13", "B14"];
    public static bool Supports(string sourceId) => SourceIds.Contains(sourceId, StringComparer.Ordinal);

    public static IReadOnlyList<BourgoinOrnamentRefinedLibrary.RefinedDesign> Create(
        BourgoinPatternLibrary.Recipe source, int paletteIndex, IEnumerable<string> modes)
    {
        if (!Supports(source.Id)) throw new ArgumentException("This study selects B08–B14.", nameof(source));
        var selected = modes.Distinct(StringComparer.Ordinal).ToArray();
        if (selected.Any(mode => mode is not ("field" or "motif" or "detail")))
            throw new ArgumentException("Modes are field, motif or detail.", nameof(modes));
        return selected.Select(mode => Build(source, paletteIndex, mode)).ToArray();
    }

    private static BourgoinOrnamentRefinedLibrary.RefinedDesign Build(BourgoinPatternLibrary.Recipe source, int palette, string mode)
    {
        var p = new Painter(source); string title; string notes;
        switch (source.Id)
        {
            case "B08": case "B08.B":
                Diamonds(p, mode); title = "Flower-filled ribbon diamonds";
                notes = "The broad diamond between actual oblique ribbon families carries a full medallion rather than leaves on its rails. Alternating long and short petals, inner veins and a contrasting collar occupy the room; a second smaller flower marks the ribbon crossing. The source guides remain behind the ornament.";
                break;
            case "B09":
                ReturnCups(p, mode); title = "Palmettes in opposed S returns";
                notes = "Two opposed return cups of the detached S receive broad, articulated palmettes. Their separate placement and opposed maps retain the S's free ends; no new connector pretends that the source is a continuous meander.";
                break;
            case "B10": case "B11":
                PointedRooms(p, mode); title = source.Id == "B11" ? "Counterturned interior gardens" : "Pointed chambers and inward fans";
                notes = "Each actual pointed chamber supplies its own finite interior placement read and source port. Wide opposed sprays or a radial blossom occupy the interior. The detail has four dominant diagonal petals, four short axial petals, one centre and quiet veins; its uninterrupted parent rails remain legible. Alternate chamber families take different colours.";
                break;
            case "B12":
                ArmRosettes(p, mode); title = "Rotating eight-petal chamber flowers";
                notes = "Large eight-petal flowers cover the middle of the stepped-arm field. Four broad long petals carry the dominant turning read; four shorter interleaved petals and a double collar provide a second scale. The native closed arm contours and diagonal disposition are retained underneath the intentionally occluding ornamental reading.";
                break;
            default:
                SerifGardens(p, mode); title = "Four-colour interlocking gardens";
                notes = "The four actual quarter-related serif-I images are four stable colour families. Ornament follows each I's long crossbar and broad end caps, with visible inner veins and curled cap fans. Larger bright flowers punctuate selected interlocks; the complete four-image source remains the placement construction.";
                break;
        }
        var enriched = p.Author.Finish();
        var curves = new OrnamentCurveSelection(enriched.Result, N.S(p.Curves.ToArray()), N.S(p.Fixed.ToArray()));
        var colour = new OrnamentColourSelection(enriched.Result, N.S(p.Colours.Select(group => (IElement)N.S(group.ToArray())).ToArray()));
        if (!curves.IsComplete || !colour.IsComplete) throw new InvalidOperationException("Presentation must select actual final paths.");
        var number = int.Parse(source.Id.AsSpan(1, 2));
        var weight = mode == "field" ? .25 : mode == "motif" ? .17 : .10;
        var design = new BourgoinOrnamentDesignLibrary.Design($"R-{source.Id}-{mode}", source.Id, mode,
            $"{source.Id} · {title}", source, enriched, p.Author.Field.Identity, p.Author.Motif.Identity,
            p.Author.Detail.Identity, notes, (number * 5 + palette + (mode == "field" ? 0 : mode == "motif" ? 3 : 7)) % 12, weight);
        return new(design, N.S(curves.Surface, colour.Surface), notes +
            " Curved rendering rounds selected flowing contours only; pointed tips, roots and native source seam identities remain fixed. Four colour groups are exact disjoint path selections, not screen-location bins.");
    }

    private static void Diamonds(Painter p, string mode)
    {
        var c = p.Source.Id == "B08" ? (X: 0, Y: 40) : (X: 0, Y: 48);
        var ry = p.Source.Id == "B08" ? 19 : 23; var rx = p.Source.Id == "B08" ? 35 : 38;
        if (mode == "field")
        {
            p.Contour(p.Author.Motif, c, [(0, ry), (-rx / 2, ry / 2), (-rx, 0), (-rx / 2, -ry / 2),
                (0, -ry), (rx / 2, -ry / 2), (rx, 0), (rx / 2, ry / 2), (0, ry)], 0, curve: true, fixedSites: [0, 2, 4, 6]);
            p.Flower(c, 4, 11, 5, 1, detailed: false, stretchX: 2);
            p.Collar(c, 3, 3);
        }
        else if (mode == "motif")
        {
            p.Flower(c, 4, ry - 2, 6, 1, detailed: true, stretchX: 1);
            p.Leaf((c.X - 3, c.Y), 27, 7, p.Author.Map((0, 1), (-1, 0)), 0, detailed: true);
            p.Leaf((c.X + 3, c.Y), 27, 7, p.Author.Map((0, -1), (1, 0)), 2, detailed: true);
            p.Collar(c, 4, 3);
        }
        else
        {
            p.Flower(c, 8, ry - 1, 6, 0, detailed: true, stretchX: 1, secondaryLength: ry / 2);
            // The long horizontal pair fills the shallow diamond rather than
            // mechanically shrinking all eight petals to a circular rosette.
            p.Leaf((c.X - 3, c.Y), rx - 4, 6, p.Author.Map((0, 1), (-1, 0)), 2, true);
            p.Leaf((c.X + 3, c.Y), rx - 4, 6, p.Author.Map((0, -1), (1, 0)), 0, true);
            p.Collar(c, 4, 3); p.Collar(c, 2, 1);
        }
        p.Flower((0, 0), 4, mode == "detail" ? 5 : 4, 2, 3, detailed: mode == "detail");
    }

    private static void ReturnCups(Painter p, string mode)
    {
        foreach (var (c, turn, family) in new[] { ((X: 12, Y: 8), 0, 0), ((X: 36, Y: 16), 2, 2) })
        {
            var map = p.Turn(turn);
            if (mode == "field")
            {
                p.Contour(p.Author.Motif, c, [(-8, -4), (-8, 1), (-5, 5), (0, 6), (5, 5), (8, 1), (8, -4),
                    (4, -2), (0, 0), (-4, -2), (-8, -4)], family, map, true, [0, 6]);
                p.Line(p.Author.Detail, c, [(-6, -2), (0, 2), (6, -2)], map);
            }
            else
            {
                p.Palmette(c, 8, 6, map, family, mode == "detail");
                p.Collar(c, mode == "detail" ? 2 : 1, 3);
            }
        }
    }

    private static void PointedRooms(Painter p, string mode)
    {
        var cells = p.Author.Paths.Where(path => path.Closed).ToArray();
        for (var i = 0; i < cells.Length; i++)
        {
            var cell = cells[i]; var (c, radius) = Interior(cell); var r = Math.Max(6, radius - 1);
            var anchor = p.Author.NearestPort(c, cell.Path); var family = i % 4;
            if (mode == "field")
            {
                p.Contour(p.Author.Motif, c, [(0, r), (-r + 1, 1), (-r, 0), (-r + 1, -1), (0, -r),
                    (r - 1, -1), (r, 0), (r - 1, 1), (0, r)], family, curve: true, fixedSites: [0, 2, 4, 6], anchor: anchor);
                p.Flower(c, 4, Math.Max(4, r * 2 / 3), Math.Max(2, r / 4), (family + 1) % 4, false, anchor: anchor);
            }
            else if (mode == "motif")
            {
                p.Palmette(c, r, Math.Max(5, r - 1), p.Turn(i % 4), family, true, anchor);
                p.Collar(c, 2, 3, anchor);
            }
            else
            {
                var axial = Math.Max(3, r / 3);
                p.Flower(c, 8, axial, Math.Max(3, r / 3), family, false,
                    secondaryLength: axial * 2, anchor: anchor);
                p.Collar(c, 2, 3, anchor);
                // The long diagonal petals fit the chamber's pointed read.
                // One centre and one vein per petal preserve that silhouette
                // without the previous nested crossing fan of detail.
            }
        }
    }

    private static void ArmRosettes(Painter p, string mode)
    {
        var cells = p.Author.Paths.Where(path => path.Closed).ToArray();
        for (var index = 0; index < cells.Length; index++)
        {
            var cell = cells[index]; var c = cell.Centre; var family = index % 4;
            var anchor = p.Author.NearestPort(c, cell.Path);
            if (mode == "field")
            {
                p.Contour(p.Author.Motif, c, [(0, 23), (-16, 16), (-23, 0), (-16, -16), (0, -23),
                    (16, -16), (23, 0), (16, 16), (0, 23)], family, curve: true, anchor: anchor);
                p.Flower(c, 4, 15, 5, (family + 1) % 4, false, anchor: anchor);
            }
            else if (mode == "motif")
            {
                p.Flower(c, 8, 21, 5, family, false, secondaryLength: 11, anchor: anchor);
                p.Collar(c, 4, 3, anchor);
            }
            else
            {
                // Four equal diagonal maps are actual word correspondences.
                // Their wider/longer petals dominate the four shorter axial
                // petals; this gives the flower a clear turning read.
                p.Flower(c, 8, 17, 6, family, true, secondaryLength: 25, anchor: anchor);
                p.Collar(c, 6, 3, anchor); p.Collar(c, 3, (family + 2) % 4, anchor);
            }
        }
    }

    private static void SerifGardens(Painter p, string mode)
    {
        var cells = p.Author.Paths.Where(path => path.Closed).ToArray();
        for (var index = 0; index < cells.Length; index++)
        {
            var cell = cells[index]; p.Colours[index % 4].Add(cell.Path);
            var horizontal = cell.Width > cell.Height; var c = cell.Centre; var family = (index + 1) % 4;
            var anchor = p.Author.NearestPort(c, cell.Path);
            var map = horizontal ? p.Turn(1) : p.Turn(0);
            var half = (horizontal ? cell.Width : cell.Height) / 2 - 9;
            if (mode == "field")
            {
                p.Leaf(horizontal ? (c.X - half, c.Y) : (c.X, c.Y - half), half * 2, 2,
                    map, family, false, anchor);
                p.Collar(c, 3, 3, anchor);
            }
            else
            {
                p.Line(p.Author.Detail, c, [(-half, 0), (-half / 2, 1), (0, 0), (half / 2, -1), (half, 0)],
                    horizontal ? null : p.Turn(1), curve: true, fixedSites: [0, 2, 4], anchor: anchor);
                var ends = horizontal ? new[] { (X: c.X - half - 2, Y: c.Y), (X: c.X + half + 2, Y: c.Y) } :
                    new[] { (X: c.X, Y: c.Y - half - 2), (X: c.X, Y: c.Y + half + 2) };
                for (var end = 0; end < ends.Length; end++)
                    p.Palmette(ends[end], 7, 6, p.Turn((horizontal ? 1 : 0) + end * 2), family, mode == "detail", anchor);
                if (mode == "detail")
                    p.Leaf(horizontal ? (c.X - 17, c.Y) : (c.X, c.Y - 17), 34, 2, map, (family + 2) % 4, true, anchor);
            }
        }
        // A shared interlock, not four identical tiny ornaments placed along
        // the contour. The large flower is intentionally allowed to cover
        // the fine internal fret detail of the retained construction.
        if (mode != "field")
        {
            p.Flower((56, -48), mode == "detail" ? 8 : 4, mode == "detail" ? 11 : 9, 3, 3,
                mode == "detail", secondaryLength: 7);
            p.Collar((56, -48), 3, 1);
        }
    }

    // Authoring-only interior placement: exact integer points are tested
    // against the retained polygon. The resulting placement is then installed
    // through an actual source port and native offset/read witness. This is
    // neither a runtime numeric supplier nor a discovery/beauty claim.
    private static ((int X, int Y) Centre, int Radius) Interior(BourgoinOrnamentAuthor.PathRead cell)
    {
        var candidates = InteriorSites(cell, 1).Select(point => (Point: point, Distance: EdgeDistance(cell, point)))
            .OrderByDescending(site => site.Distance).ThenBy(site =>
                Math.Abs(site.Point.X - cell.Centre.X) + Math.Abs(site.Point.Y - cell.Centre.Y)).ToArray();
        if (candidates.Length == 0) return (cell.Centre, 4);
        return (candidates[0].Point, Math.Max(3, (int)Math.Floor(candidates[0].Distance)));
    }

    private static IEnumerable<(int X, int Y)> InteriorSites(BourgoinOrnamentAuthor.PathRead cell, int clearance)
    {
        for (var x = cell.MinX + clearance; x <= cell.MinX + cell.Width - clearance; x += 2)
            for (var y = cell.MinY + clearance; y <= cell.MinY + cell.Height - clearance; y += 2)
                if (Inside(cell.Vertices, (x, y)) && EdgeDistance(cell, (x, y)) >= clearance) yield return (x, y);
    }

    private static bool Inside((int X, int Y)[] polygon, (int X, int Y) point)
    {
        var inside = false;
        for (var i = 0; i < polygon.Length - 1; i++)
        {
            var a = polygon[i]; var b = polygon[i + 1];
            if ((a.Y > point.Y) != (b.Y > point.Y) &&
                point.X < (b.X - a.X) * (double)(point.Y - a.Y) / (b.Y - a.Y) + a.X) inside = !inside;
        }
        return inside;
    }

    private static double EdgeDistance(BourgoinOrnamentAuthor.PathRead cell, (int X, int Y) point) =>
        Enumerable.Range(0, cell.Vertices.Length - 1).Min(index =>
        {
            var a = cell.Vertices[index]; var b = cell.Vertices[index + 1];
            var dx = b.X - a.X; var dy = b.Y - a.Y;
            var t = Math.Clamp(((point.X - a.X) * dx + (point.Y - a.Y) * dy) / (double)(dx * dx + dy * dy), 0, 1);
            var ex = point.X - a.X - t * dx; var ey = point.Y - a.Y - t * dy;
            return Math.Sqrt(ex * ex + ey * ey);
        });

    private sealed class Painter
    {
        public BourgoinPatternLibrary.Recipe Source { get; }
        public BourgoinOrnamentAuthor Author { get; }
        public List<Relation> Curves { get; } = [];
        public List<Relation> Fixed { get; } = [];
        public List<Relation>[] Colours { get; } = [[], [], [], []];
        public Painter(BourgoinPatternLibrary.Recipe source)
        {
            Source = source; Author = new(source);
            Author.Motif.SelectOnly(); Author.Detail.SelectOnly();
        }
        public QualitativePatternTransform Turn(int quarter) => (((quarter % 4) + 4) % 4) switch
        {
            0 => Author.Map((1, 0), (0, 1)), 1 => Author.Map((0, -1), (1, 0)),
            2 => Author.Map((-1, 0), (0, -1)), _ => Author.Map((0, 1), (-1, 0))
        };

        public Relation Contour(BourgoinOrnamentAuthor.Layer layer, (int X, int Y) at, (int X, int Y)[] points,
            int colour, QualitativePatternTransform? map = null, bool curve = false, int[]? fixedSites = null, Relation? anchor = null) =>
            Draw(layer, at, points, true, colour, map, curve, fixedSites, anchor);
        public Relation Line(BourgoinOrnamentAuthor.Layer layer, (int X, int Y) at, (int X, int Y)[] points,
            QualitativePatternTransform? map = null, bool curve = false, int[]? fixedSites = null, Relation? anchor = null) =>
            Draw(layer, at, points, false, -1, map, curve, fixedSites, anchor);

        private Relation Draw(BourgoinOrnamentAuthor.Layer layer, (int X, int Y) at, (int X, int Y)[] points,
            bool closed, int colour, QualitativePatternTransform? map, bool curve, int[]? fixedSites, Relation? anchor)
        {
            var clean = points.Where((point, index) => index == 0 || point != points[index - 1]).ToArray();
            Author.Place(layer, Author.Prototype(closed, clean), at, anchor, map);
            var additions = (Relation)((Relation)((Relation)layer.Definition.Right).Right).Left;
            var actual = (Relation)N.E(additions).Last();
            var declaration = (Relation)actual.Right; var pathsPorts = (Relation)declaration.Left;
            var path = (Relation)N.E((Relation)pathsPorts.Left).Single();
            if (colour >= 0) Colours[colour % 4].Add(path);
            if (curve)
            {
                Curves.Add(path); var words = N.E((Relation)path.Right);
                foreach (var index in (fixedSites ?? []).Distinct())
                {
                    var pick = index == words.Length ? path : words[index];
                    Fixed.Add(N.E((Relation)pathsPorts.Right).Cast<Relation>().Single(port =>
                        port.Right is Relation selector && ReferenceEquals(selector.Left, path) && ReferenceEquals(selector.Right, pick)));
                }
            }
            return path;
        }

        public void Leaf((int X, int Y) root, int length, int width, QualitativePatternTransform? map,
            int colour, bool detailed, Relation? anchor = null)
        {
            var a = Math.Max(1, length / 3); var b = Math.Max(a + 1, length * 2 / 3);
            Contour(Author.Motif, root, [(0, 0), (-width, a), (-width, b), (0, length), (width, b), (width, a), (0, 0)],
                colour, map, true, [0, 3], anchor);
            Line(Author.Detail, root, [(0, 1), (0, length - 1)], map, anchor: anchor);
            if (detailed)
            {
                Line(Author.Detail, root, [(-Math.Max(1, width - 1), b), (0, a), (Math.Max(1, width - 1), b)], map, anchor: anchor);
                if (width >= 4) Line(Author.Detail, root, [(-width + 1, a + 1), (0, length / 5), (width - 1, a + 1)], map, anchor: anchor);
            }
        }

        public void Flower((int X, int Y) centre, int petals, int length, int width, int colour, bool detailed,
            int stretchX = 1, int? secondaryLength = null, Relation? anchor = null)
        {
            for (var i = 0; i < petals; i++)
            {
                var diagonal = petals == 8 && i % 2 != 0; var quarter = petals == 8 ? i / 2 : i;
                var n = diagonal ? secondaryLength ?? Math.Max(4, length * 2 / 3) : length;
                (int X, int Y) v = quarter switch { 0 => (0, 1), 1 => (1, 0), 2 => (0, -1), _ => (-1, 0) };
                if (diagonal) v = (v.X + v.Y, v.Y - v.X);
                var transverse = (X: v.Y * stretchX, Y: -v.X);
                var map = Author.Map(transverse, (v.X * stretchX, v.Y));
                var root = (centre.X + v.X * 2 * stretchX, centre.Y + v.Y * 2);
                Leaf(root, n, diagonal ? Math.Max(2, width - 2) : width, map,
                    (colour + (diagonal ? 1 : quarter % 2 * 2)) % 4, detailed, anchor);
            }
        }

        public void Palmette((int X, int Y) centre, int width, int height, QualitativePatternTransform map,
            int colour, bool detailed, Relation? anchor = null)
        {
            var w = Math.Max(4, width); var h = Math.Max(4, height);
            Contour(Author.Motif, centre, [(0, -h / 2), (-w, -h / 2), (-w, h / 3), (-w / 2, h),
                (0, h * 2 / 3), (w / 2, h), (w, h / 3), (w, -h / 2), (0, -h / 2)],
                colour, map, true, [0, 2, 3, 5, 6], anchor);
            Line(Author.Detail, centre, [(0, -h / 2), (0, h * 2 / 3)], map, anchor: anchor);
            Line(Author.Detail, centre, [(-w + 2, h / 3), (0, -h / 2), (w - 2, h / 3)], map, curve: true, anchor: anchor);
            if (detailed)
            {
                Line(Author.Detail, centre, [(-w / 2, h - 2), (-w / 3, h / 4), (0, -h / 2),
                    (w / 3, h / 4), (w / 2, h - 2)], map, curve: true, fixedSites: [2], anchor: anchor);
                Contour(Author.Detail, centre, [(0, -h / 4), (-2, h / 3), (0, h / 2), (2, h / 3), (0, -h / 4)],
                    (colour + 2) % 4, map, true, [0, 2], anchor);
            }
        }

        public void Collar((int X, int Y) c, int r, int colour, Relation? anchor = null) =>
            Contour(Author.Detail, c, [(r, 0), (r, r / 2), (r / 2, r), (0, r), (-r / 2, r), (-r, r / 2),
                (-r, 0), (-r, -r / 2), (-r / 2, -r), (0, -r), (r / 2, -r), (r, -r / 2), (r, 0)],
                colour, curve: true, anchor: anchor);
    }
}
