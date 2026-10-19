using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;
using N = Gojo.Experiments.QualitativeFrieze.FieldNative;

namespace Gojo.Experiments.QualitativeFrieze;

/// <summary>Authored native detail studies. Actual guide paths, reusable
/// silhouettes, vein paths, maps and anchor correspondences survive export.
/// Colour, line width, finite-contour fill and presentation zoom are separate.</summary>
public static class OrnamentDetailLibrary
{
    public sealed record Study(string Id, string Kind, BourgoinOrnamentDesignLibrary.Design Design,
        string ConstructionNotes, string Limits, Relation CrossingEvidence)
    {
        public string Title => Design.Title;
    }

    public static IReadOnlyList<Study> Create() => Create(Enumerable.Range(1, 14).Select(index => $"D{index:00}"));
    public static IReadOnlyList<Study> Create(IEnumerable<string> ids)
    {
        var selected = ids.Distinct(StringComparer.Ordinal).ToArray();
        var result = new List<Study>(); BourgoinPatternLibrary? sources = null;
        foreach (var id in selected)
        {
            if (id is "D11" or "D12" or "D13" or "D14")
            {
                sources ??= new BourgoinPatternLibrary();
                var sourceId = id is "D11" or "D12" ? "B02" : "B05";
                var source = new BourgoinPatternRefinement(sources.Recipes.Single(recipe => recipe.Id == sourceId), 8).Result;
                result.Add(Integration(id, source));
            }
            else result.Add(Standalone(id));
        }
        return result;
    }

    private static Study Standalone(string id)
    {
        var scaffold = new Scaffold(); string title; string notes; Relation gap = N.S();
        switch (id)
        {
            case "D01":
                title = "Mirrored veined leaf sprig";
                scaffold.Path(false, (0, -12), (0, -4), (0, 4), (0, 10));
                scaffold.Path(false, (0, -4), (3, -1), (7, 3));
                scaffold.Path(false, (0, -4), (-3, -1), (-7, 3));
                notes = "Two mirrored images of one lens leaf enclose the actual paired branch guides. Their veins converge toward the parent stem; a small terminal bud remains subordinate.";
                break;
            case "D02":
                title = "Three-leaf running vine";
                scaffold.Path(false, (0, -12), (0, -8), (1, -4), (0, 0), (-1, 4), (0, 8), (0, 14));
                foreach (var (y, side) in new[] { (-8, 1), (0, -1), (8, 1) })
                    scaffold.Path(false, (0, y), (side * 3, y + 3), (side * 6, y + 6));
                notes = "One curving finite stem supplies three successive public branch landmarks. Reused leaves alternate their face while continuing the same upward growth.";
                break;
            case "D03":
                title = "Eight-petal compass rose";
                foreach (var vector in EightDirections) scaffold.Path(false, (0, 0), Scale(vector, 2), Scale(vector, 4), Scale(vector, 10));
                notes = "Eight actual radial guides support eight images of one pointed petal. Each petal begins at an exposed two-reach neck and ends at its actual ten-reach guide tip, keeping the bases distinct beyond the central seed. Repeated vein forks provide smaller enrichment inside the same compass disposition.";
                break;
            case "D04":
                title = "Six-petal lotus wheel";
                scaffold.Triangular = true;
                foreach (var vector in SixDirections) scaffold.Path(false, (0, 0), Scale(vector, 4), Scale(vector, 10));
                notes = "Six substitution-related petals follow six actual shared-origin radial guides. The triangular presentation basis is explicit; no angular calibration or smooth-circle law is inferred.";
                break;
            case "D05":
                title = "Five-lobed anthemion";
                scaffold.Path(false, (0, -14), (0, -6), (0, 0), (0, 14));
                scaffold.Path(false, (0, -6), (-4, -2), (-11, 12));
                scaffold.Path(false, (0, -6), (4, -2), (11, 12));
                scaffold.Path(false, (0, -6), (-8, -6), (-16, 2));
                scaffold.Path(false, (0, -6), (8, -6), (16, 2));
                notes = "A parent stalk divides into five separate branch guides. The central lance, taller inner lobes and lower outer leaves are deliberately graded; they retain small clear intervals rather than forming an accidental solid fan.";
                break;
            case "D06":
                title = "Open lotus fan";
                scaffold.Path(false, (0, -14), (0, -10), (0, -6), (0, 12));
                scaffold.Path(false, (0, -10), (-6, -6), (-14, 10));
                scaffold.Path(false, (0, -10), (6, -6), (14, 10));
                notes = "Three leaves grow from an exposed basal fork. Long outer petals turn away from the central upright petal, leaving visible channels of background between them.";
                break;
            case "D07":
                title = "Corner acanthus and returning curl";
                scaffold.Path(false, (0, -12), (0, -6), (1, -1), (4, 0), (6, 0), (12, 0));
                scaffold.Path(false, (0, -6), (-3, -3), (-6, 0));
                scaffold.Path(false, (6, 0), (9, 3), (12, 6));
                scaffold.Path(false, (1, -1), (4, -1), (6, 1), (5, 4), (2, 5), (0, 3));
                notes = "A turning parent path carries two outward leaves and one returning open curl. The branch guides are joined through actual coincident public source roles; the curl's free end stays open.";
                break;
            case "D08":
                title = "Pearl-and-leaf linked chain";
                scaffold.Path(false, (-16, 0), (-12, 0), (-8, 0), (-4, 0), (0, 0), (4, 0), (8, 0), (12, 0), (16, 0));
                foreach (var (x, side) in new[] { (-8, 1), (0, -1), (8, 1) })
                    scaffold.Path(false, (x, 0), (x, side * 3), (x, side * 6));
                notes = "The actual longitudinal strand carries three alternating leaf branches and four pearl stations. Leaves grow from the line instead of being placed in unrelated corners.";
                break;
            case "D09":
                title = "Interrupted two-strand guilloche";
                scaffold.Path(false, (-12, 0), (-10, 3), (-8, 5), (-4, 6), (0, 0), (4, -6), (8, -5), (10, -3), (12, 0));
                var left = scaffold.Path(false, (-12, 0), (-10, -3), (-8, -5), (-4, -6), (-2, -3));
                var right = scaffold.Path(false, (2, 3), (4, 6), (8, 5), (10, 3), (12, 0));
                var bare = scaffold.Unit();
                var interruption = new OrnamentStrandGap(bare, scaffold.Terminal(left), scaffold.Start(right));
                if (!interruption.IsComplete) throw new InvalidOperationException("The interrupted strand must keep its two actual open ends.");
                gap = interruption.Surface; scaffold.Retain(gap);
                notes = "One continuous wave passes through a real gap between two other native paths. The selected terminal and restart ports retain a nonzero displacement witness. This decorative crossing does not claim a hidden continuous under-strand or native over/under order.";
                break;
            case "D10":
                title = "Quatrefoil loop and inner cord";
                scaffold.Path(true, (0, 8), (3, 11), (7, 11), (11, 7), (11, 3), (8, 0),
                    (11, -3), (11, -7), (7, -11), (3, -11), (0, -8), (-3, -11),
                    (-7, -11), (-11, -7), (-11, -3), (-8, 0), (-11, 3), (-11, 7), (-7, 11), (-3, 11), (0, 8));
                scaffold.Path(false, (0, -8), (0, -4), (0, 0), (0, 4), (0, 8));
                foreach (var vector in EightDirections.Where(vector => vector.X != 0 && vector.Y != 0))
                    scaffold.Path(false, (0, 0), Scale(vector, 2), Scale(vector, 8));
                notes = "A single closed faceted quatrefoil supplies the outer loop. Four smaller pointed compartments follow actual diagonal branch guides inside its four lobes, while an inner cord follows the upright guide. It is an ornamental loop study, not a topological knot assertion.";
                break;
            default: throw new ArgumentException($"Unknown detail study {id}.", nameof(id));
        }
        var source = scaffold.Recipe(id, title); var a = new BourgoinOrnamentAuthor(source);
        Decorate(a, id);
        return Finish(id, "study", title, source, a, notes, gap);
    }

    private static void Decorate(BourgoinOrnamentAuthor a, string id)
    {
        // The builder is an author. Its finite choices become actual path
        // programs and local attachment witnesses before rendering begins.
        switch (id)
        {
            case "D01":
                VeinedLeaf(a, (0, -4), 7, 2, a.Map((1, -1), (1, 1)));
                VeinedLeaf(a, (0, -4), 7, 2, a.Map((-1, -1), (-1, 1)));
                Pearl(a, (0, 9), 2);
                break;
            case "D02":
                foreach (var (y, side) in new[] { (-8, 1), (0, -1), (8, 1) })
                    VeinedLeaf(a, (0, y), 6, 2, a.Map((side, -1), (side, 1)));
                break;
            case "D03":
                foreach (var vector in EightDirections)
                    VeinedLeaf(a, Scale(vector, 2), 8, 2, a.Map((vector.Y, -vector.X), vector));
                Pearl(a, (0, 0), 2);
                break;
            case "D04":
                foreach (var map in SixMaps(a)) VeinedLeaf(a, (0, 0), 10, 2, map);
                Pearl(a, (0, 0), 2);
                break;
            case "D05":
                VeinedLeaf(a, (0, 0), 14, 3);
                VeinedLeaf(a, (-4, -2), 7, 1, a.Map((2, 1), (-1, 2)));
                VeinedLeaf(a, (4, -2), 7, 1, a.Map((-2, 1), (1, 2)));
                VeinedLeaf(a, (-8, -6), 8, 1, a.Map((1, 1), (-1, 1)));
                VeinedLeaf(a, (8, -6), 8, 1, a.Map((-1, 1), (1, 1)));
                Pearl(a, (0, -6), 2);
                break;
            case "D06":
                VeinedLeaf(a, (0, -6), 18, 4);
                VeinedLeaf(a, (-6, -6), 8, 1, a.Map((2, 1), (-1, 2)));
                VeinedLeaf(a, (6, -6), 8, 1, a.Map((-2, 1), (1, 2)));
                break;
            case "D07":
                VeinedLeaf(a, (0, -6), 6, 2, a.Map((1, 1), (-1, 1)));
                VeinedLeaf(a, (6, 0), 6, 2, a.Map((1, -1), (1, 1)));
                a.Motif.Select(a.Paths[^1].Path);
                Pearl(a, (3, 2), 1);
                break;
            case "D08":
                foreach (var (x, side) in new[] { (-8, 1), (0, -1), (8, 1) })
                    VeinedLeaf(a, (x, 0), 6, 1, a.Map((1, 0), (0, side)));
                foreach (var x in new[] { -12, -4, 4, 12 }) Pearl(a, (x, 0), 2);
                break;
            case "D09":
                a.Motif.Select(a.Paths[0].Path); a.Detail.Select(a.Paths[1].Path, a.Paths[2].Path);
                Pearl(a, (-6, 0), 2); Pearl(a, (6, 0), 2);
                break;
            case "D10":
                foreach (var vector in EightDirections.Where(vector => vector.X != 0 && vector.Y != 0))
                {
                    var petal = a.Prototype(true, (0, 2), (-1, 3), (-2, 5), (0, 8), (2, 5), (1, 3), (0, 2));
                    a.Place(a.Motif, petal, (0, 0), transform: a.Map((vector.Y, -vector.X), vector));
                }
                Pearl(a, (0, 0), 2);
                a.Detail.Select(a.Paths[1].Path);
                break;
        }
    }

    private static Study Integration(string id, BourgoinPatternLibrary.Recipe source)
    {
        var a = new BourgoinOrnamentAuthor(source); string title; string notes;
        if (id is "D11" or "D12")
        {
            title = id == "D11" ? "Willow sprigs in the woven strips" : "Feather-veined basket leaves";
            foreach (var path in a.Paths.Where(path => path.Closed))
            {
                var vertical = path.Height > path.Width; var centre = path.Centre;
                var map = vertical ? a.Map((1, 0), (0, 1)) : a.Map((0, 1), (-1, 0));
                var parentPort = a.NearestPort(centre, path.Path);
                if (id == "D11")
                {
                    PlaceLine(a, a.Motif, centre, [(0, -12), (0, -8), (0, -2), (0, 4), (0, 12)], map, parentPort);
                    foreach (var (y, side) in new[] { (-8, 1), (-2, -1), (4, 1) })
                    {
                        var origin = Apply(vertical, centre, (0, y));
                        var leafMap = vertical ? a.Map((side, -1), (side, 1)) : a.Map((1, side), (-1, side));
                        VeinedLeaf(a, origin, 4, 1, leafMap, forks: false, anchor: parentPort);
                    }
                    Pearl(a, Apply(vertical, centre, (0, 11)), 1, parentPort);
                }
                else
                {
                    a.Place(a.Motif, a.Prototype(true, (0, -12), (-2, -8), (-4, -3), (-4, 2), (-2, 8),
                        (0, 12), (2, 8), (4, 2), (4, -3), (2, -8), (0, -12)), centre, parentPort, map);
                    PlaceLine(a, a.Detail, centre, [(0, -12), (0, 12)], map, parentPort);
                    foreach (var y in new[] { -5, 0, 5 })
                        PlaceLine(a, a.Detail, centre, [(-2, y + 2), (0, y), (2, y + 2)], map, parentPort);
                }
            }
            notes = "The four actual B02 rectangles retain their alternating native quarter images. Every addition stays inside its own strip envelope (four fine reaches sideways, twelve lengthwise); square gaps stay undecorated. Ornament follows the strip's long axis rather than the macrocell's corners.";
        }
        else if (id == "D13")
        {
            title = "Six-petal lotus inside the hexagons";
            foreach (var map in SixMaps(a)) VeinedLeaf(a, (0, 0), 10, 2, map);
            Pearl(a, (0, 0), 2);
            notes = "One six-petal flower occupies the actual B05 hexagon centre through a boundary-port offset. Its ten-reach radial tips stop short of the sixteen-reach source boundary. Reused vein forks remain inside their own petals; the triangular interstices remain quiet.";
        }
        else
        {
            title = "Inward buds around an open hexagonal heart";
            var maps = SixMaps(a);
            for (var index = 0; index < SixDirections.Length; index++)
            {
                var vector = SixDirections[index];
                var root = Scale(vector, 12); var boundary = Scale(vector, 16);
                a.Segment(a.Motif, boundary, root);
                VeinedLeaf(a, root, 8, 1, maps[(index + 2) % 6], forks: false);
            }
            notes = "Six actual boundary vertices feed short inward stems, each ending in a separate pointed bud. Buds turn toward the centre but stop four reaches away; that central hexagonal negative space is deliberately left open. This reverses the outward flower reading without changing B05's source lattice.";
        }
        return Finish(id, "integration", title, source, a, notes, N.S());
    }

    private static Study Finish(string id, string kind, string title, BourgoinPatternLibrary.Recipe source,
        BourgoinOrnamentAuthor a, string notes, Relation gaps)
    {
        var enrichment = a.Finish();
        var design = new BourgoinOrnamentDesignLibrary.Design(id, source.Id, "detail", title, source, enrichment,
            a.Field.Identity, a.Motif.Identity, a.Detail.Identity, notes, (int.Parse(id.AsSpan(1)) + 2) % 12,
            kind == "study" ? .11 : .075);
        const string limits = "Finite native chords, shared prototypes and actual landmark-offset witnesses; no analytic curvature, occupied-face clearance, automatic physical join from coordinate equality, learned beauty, or hidden over/under continuity is claimed.";
        return new(id, kind, design, notes, limits, gaps);
    }

    private static readonly (int X, int Y)[] EightDirections = [(0, 1), (-1, 1), (-1, 0), (-1, -1), (0, -1), (1, -1), (1, 0), (1, 1)];
    private static readonly (int X, int Y)[] SixDirections = [(1, 0), (0, 1), (-1, 1), (-1, 0), (0, -1), (1, -1)];
    private static (int X, int Y) Scale((int X, int Y) point, int count) => (point.X * count, point.Y * count);
    private static (int X, int Y) Apply(bool vertical, (int X, int Y) origin, (int X, int Y) local) =>
        vertical ? (origin.X + local.X, origin.Y + local.Y) : (origin.X - local.Y, origin.Y + local.X);
    private static QualitativePatternTransform[] SixMaps(BourgoinOrnamentAuthor a)
    {
        (int X, int Y) horizontal = (1, 0), vertical = (0, 1); var result = new List<QualitativePatternTransform>();
        for (var index = 0; index < 6; index++)
        {
            result.Add(a.Map(horizontal, vertical));
            horizontal = (-horizontal.Y, horizontal.X + horizontal.Y);
            vertical = (-vertical.Y, vertical.X + vertical.Y);
        }
        return result.ToArray();
    }

    private static void VeinedLeaf(BourgoinOrnamentAuthor a, (int X, int Y) root, int length, int width,
        QualitativePatternTransform? map = null, bool forks = true, Relation? anchor = null)
    {
        var q = Math.Max(1, length / 5); var middle = Math.Max(2, length / 2); var shoulder = Math.Max(middle + 1, length * 4 / 5);
        var vertices = new (int X, int Y)[] { (0, 0), (-1, q), (-width, middle), (-width, shoulder - 1),
            (-1, shoulder), (0, length), (1, shoulder), (width, shoulder - 1), (width, middle), (1, q), (0, 0) };
        var leaf = a.Prototype(true, vertices.Where((point, index) => index == 0 || point != vertices[index - 1]).ToArray());
        a.Place(a.Motif, leaf, root, anchor, map);
        PlaceLine(a, a.Detail, root, [(0, 0), (0, length)], map, anchor);
        if (forks && width > 1)
            PlaceLine(a, a.Detail, root, [(-width + 1, middle + 1), (0, middle - 1), (width - 1, middle + 1)], map, anchor);
    }

    private static void PlaceLine(BourgoinOrnamentAuthor a, BourgoinOrnamentAuthor.Layer layer,
        (int X, int Y) origin, (int X, int Y)[] vertices, QualitativePatternTransform? map = null, Relation? anchor = null) =>
        a.Place(layer, a.Prototype(false, vertices), origin, anchor, map);

    private static void Pearl(BourgoinOrnamentAuthor a, (int X, int Y) centre, int radius, Relation? anchor = null)
    {
        if (radius == 1)
        {
            a.Place(a.Detail, a.Prototype(true, (1, 0), (0, 1), (-1, 0), (0, -1), (1, 0)), centre, anchor);
            return;
        }
        var half = Math.Max(1, radius / 2);
        a.Place(a.Detail, a.Prototype(true, (radius, 0), (radius, half), (half, radius), (0, radius),
            (-half, radius), (-radius, half), (-radius, 0), (-radius, -half), (-half, -radius),
            (0, -radius), (half, -radius), (radius, -half), (radius, 0)), centre, anchor);
    }

    private sealed class Scaffold
    {
        private readonly QualitativeMotionFrame frame = new(); private readonly IElement reach = new Placeholder();
        private readonly List<Relation> paths = [], ports = [], closed = [], retained = [];
        private readonly Dictionary<Relation, (int X, int Y)> positions = new(ReferenceEqualityComparer.Instance);
        public bool Triangular { get; set; }
        public Relation Path(bool closes, params (int X, int Y)[] vertices)
        {
            var words = Enumerable.Range(0, vertices.Length - 1).Select(index => Word(
                (vertices[index + 1].X - vertices[index].X, vertices[index + 1].Y - vertices[index].Y))).ToArray();
            var path = new Relation(Word(vertices[0]), N.S(words)); paths.Add(path); if (closes) closed.Add(path);
            for (var index = 0; index < vertices.Length; index++)
            {
                var port = new Relation(new Placeholder(), new Relation(path, index == words.Length ? path : words[index]));
                ports.Add(port); positions.Add(port, vertices[index]);
            }
            return path;
        }
        public Relation Start(Relation path) => ports.Single(port => port.Right is Relation pick &&
            ReferenceEquals(pick.Left, path) && ReferenceEquals(pick.Right, N.E((Relation)path.Right)[0]));
        public Relation Terminal(Relation path) => ports.Single(port => port.Right is Relation pick &&
            ReferenceEquals(pick.Left, path) && ReferenceEquals(pick.Right, path));
        public void Retain(Relation evidence) => retained.Add(evidence);
        public UnitArrangementProgram Unit() => new(frame, "actual detail scaffold", N.S(paths.ToArray()), N.S(ports.ToArray()), N.S(closed.ToArray()), N.S(retained.ToArray()));
        public BourgoinPatternLibrary.Recipe Recipe(string id, string title)
        {
            var unit = Unit(); var identity = Map();
            var motif = new Placeholder(); var transform = new Placeholder(); var cu = new Placeholder(); var rv = new Placeholder();
            var motifs = N.S(unit.Surface); var transforms = N.S(identity.Surface);
            var column = Word((40, 0)); var row = Word((0, 40)); IElement u = new Placeholder(), v = new Placeholder();
            var uc = new Relation(N.S(u), N.S(new Relation(u, u))); var vc = new Relation(N.S(v), N.S(new Relation(v, v)));
            var layout = new Relation(new Relation(uc, vc), new Relation(new Relation(cu, rv), N.S(new Relation(new Relation(u, v), new Relation(motif, transform)))));
            var parameters = N.S(new Relation(motif, motifs), new Relation(transform, transforms), new Relation(cu, N.S(column)), new Relation(rv, N.S(row)));
            var program = new OrnamentProgram(frame, motifs, transforms, parameters, layout);
            var bindings = N.S(new Relation(motif, unit.Surface), new Relation(transform, identity.Surface), new Relation(cu, column), new Relation(rv, row));
            var joins = new List<Relation>();
            for (var first = 0; first < ports.Count; first++)
                for (var second = first + 1; second < ports.Count; second++)
                    if (positions[ports[first]] == positions[ports[second]])
                        joins.Add(OrnamentProgramField.Rule(unit.Surface, identity.Surface, unit.Surface, identity.Surface,
                            OrnamentProgramField.Offset(program, 0, 0), ports[first].Left, ports[second].Left));
            return new(id, title, 0, 0, "Authored native detail scaffold", "Actual guide paths, public branch roles and admitted local coincident-site seams.",
                "An authored conventional ornament study; it is not a source engraving reconstruction.", program, bindings,
                OrnamentProgram.Window(u, v, 3, 3), column, row, OrnamentProgramField.Contract(program, joins.ToArray()),
                Triangular ? new(1, 0, .5, Math.Sqrt(3) / 2) : new(1, 0, 0, 1));
        }
        private QualitativePatternTransform Map()
        {
            Relation Row(IElement direction) => new(QualitativeMotionFrame.Move(direction, reach), N.S(QualitativeMotionFrame.Move(direction, reach)));
            return new(frame, frame, N.S(Row(frame.Right), Row(frame.Left), Row(frame.Up), Row(frame.Down)));
        }
        private Relation Word((int X, int Y) point) => N.S(
            Enumerable.Range(0, Math.Abs(point.X)).Select(_ => (IElement)QualitativeMotionFrame.Move(point.X < 0 ? frame.Left : frame.Right, reach))
                .Concat(Enumerable.Range(0, Math.Abs(point.Y)).Select(_ => (IElement)QualitativeMotionFrame.Move(point.Y < 0 ? frame.Down : frame.Up, reach))).ToArray());
    }
}
