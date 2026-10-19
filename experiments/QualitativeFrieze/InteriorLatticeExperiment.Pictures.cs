using System.Globalization;
using System.Net;
using System.Text;
using Gojo.Core;
using Gojo.Encoding.Structures;
using Gojo.Experiments.QualitativeExecution;
using N = Gojo.Experiments.QualitativeFrieze.FieldNative;

namespace Gojo.Experiments.QualitativeFrieze;

public static partial class InteriorLatticeExperiment
{
    private readonly record struct V(long X, long Y);
    private sealed record EdgePicture(IElement Identity, V From, V To);
    private sealed record CellPicture(V[] Points, int Colour, bool Conflict);
    private sealed record FieldPicture(CellPicture[] Cells, EdgePicture[] Edges, EdgePicture[] Removed,
        CellPlate[] Facts, V[] Sites, BourgoinPatternLibrary.DisplayBasis Basis);
    private static string H(string text) => WebUtility.HtmlEncode(text);
    private static string F(double value) => value.ToString("0.###", CultureInfo.InvariantCulture);

    private static FieldPicture Picture(InteriorCellPartition read, Relation complex, Relation groups, Relation bindings,
        QualitativeMotionFrame frame, BourgoinPatternLibrary.DisplayBasis basis)
    {
        var positions = N.E(bindings).Cast<Relation>().ToDictionary(row => row.Left,
            row => Position((Relation)row.Right, frame), N.Participants.Instance);
        var edges = new Dictionary<IElement, EdgePicture>(N.Participants.Instance);
        var removed = new Dictionary<IElement, EdgePicture>(N.Participants.Instance);
        var cells = new List<CellPicture>(); var facts = new List<CellPlate>();
        var colours = new Dictionary<IElement, HashSet<int>>(N.Participants.Instance);
        foreach (var group in read.GroupReads)
        {
            var darts = N.E(group.Boundary).Cast<Relation>().ToArray();
            var raw = darts.Select(dart => positions[((Relation)dart.Right).Left]).ToArray();
            var points = Simplify(raw); var colour = 0;
            while (darts.Any(dart => colours.TryGetValue(dart.Left, out var used) && used.Contains(colour))) colour++;
            foreach (var dart in darts)
            {
                if (!colours.TryGetValue(dart.Left, out var used)) colours[dart.Left] = used = [];
                used.Add(colour); Add(dart, edges);
            }
            foreach (var edge in N.E(group.RemovedInternalEdges).Cast<Relation>()) Add(edge, removed);
            // Keep every actual seam vertex for fill. Removing a straight
            // neighbour's T landmark before decimal SVG projection could make
            // its long chord miss the other's rounded intermediate point.
            // Straight collapse belongs to the shape read, not this shared
            // boundary rendering contract.
            cells.Add(new(raw, colour, false));
            var atomCount = group.Atoms.Count;
            var area = Math.Abs(TwiceArea(points)) / 2.0;
            var box = points.Length == 0 ? 0 : (double)(points.Max(p => p.X) - points.Min(p => p.X)) * (points.Max(p => p.Y) - points.Min(p => p.Y));
            var scale = points.Length == 0 ? 0 : Math.Max(points.Max(p => p.X) - points.Min(p => p.X), points.Max(p => p.Y) - points.Min(p => p.Y));
            var shortRuns = Enumerable.Range(0, points.Length).Count(i => Distance(points[i], points[(i + 1) % points.Length]) < scale / 6.0);
            var winding = Math.Sign(TwiceArea(points));
            var reflex = Enumerable.Range(0, points.Length).Count(i => Math.Sign(Cross(points[(i + points.Length - 1) % points.Length], points[i], points[(i + 1) % points.Length])) == -winding);
            var fraction = shortRuns / (double)Math.Max(1, points.Length);
            facts.Add(new(facts.Count, atomCount, points.Length, reflex, area, box == 0 ? 0 : area / box,
                fraction, group.Shape is { FineToothReturns: >= 3, FineToothRatio: > .35 }, group.Shape));
        }
        if (!read.IsPartition)
        {
            cells.Clear(); edges.Clear();
            var claims = N.E(groups).OfType<Relation>().ToArray();
            var owners = claims.SelectMany(g => N.E((Relation)g.Right)).GroupBy(f => f, N.Participants.Instance)
                .ToDictionary(g => g.Key, g => g.Count(), N.Participants.Instance);
            for (var i = 0; i < claims.Length; i++)
                foreach (var face in N.E((Relation)claims[i].Right).OfType<Relation>())
                {
                    var darts = N.E((Relation)face.Right).Cast<Relation>().ToArray();
                    if (darts.Any(d => d.Right is not Relation ends || !positions.ContainsKey(ends.Left) || !positions.ContainsKey(ends.Right))) continue;
                    cells.Add(new(Simplify(darts.Select(d => positions[((Relation)d.Right).Left]).ToArray()), i, owners[face] > 1));
                    foreach (var dart in darts) Add(dart, edges);
                }
        }
        return new(cells.ToArray(), edges.Values.ToArray(), removed.Values.ToArray(), facts.ToArray(), positions.Values.ToArray(), basis);
        void Add(Relation row, Dictionary<IElement, EdgePicture> target)
        {
            if (row.Right is Relation ends && positions.TryGetValue(ends.Left, out var a) && positions.TryGetValue(ends.Right, out var b))
                target.TryAdd(row.Left, new(row.Left, a, b));
        }
    }
    private static V Position(Relation word, QualitativeMotionFrame frame)
    {
        long x = 0, y = 0;
        foreach (var term in N.E(word).Cast<Relation>())
        {
            if (!frame.Direction(term, out var axis, out var forward, out _)) throw new InvalidOperationException("A displayed point needs an actual word in its declared frame.");
            if (ReferenceEquals(axis, frame.Horizontal)) x += forward ? 1 : -1; else y += forward ? 1 : -1;
        }
        return new(x, y);
    }
    private static long Cross(V a, V b, V c) => checked((b.X - a.X) * (c.Y - b.Y) - (b.Y - a.Y) * (c.X - b.X));
    private static long TwiceArea(V[] points) => Enumerable.Range(0, points.Length).Sum(i => checked(points[i].X * points[(i + 1) % points.Length].Y - points[i].Y * points[(i + 1) % points.Length].X));
    private static long Distance(V a, V b) => Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y);
    private static V[] Simplify(V[] points)
    {
        var result = points.ToList(); var changed = true;
        while (changed && result.Count > 3)
        {
            changed = false;
            for (var i = result.Count - 1; i >= 0; i--)
            {
                var a = result[(i + result.Count - 1) % result.Count]; var b = result[i]; var c = result[(i + 1) % result.Count];
                if (Cross(a, b, c) != 0 || (b.X - a.X) * (c.X - b.X) + (b.Y - a.Y) * (c.Y - b.Y) <= 0) continue;
                result.RemoveAt(i); changed = true;
            }
        }
        return result.ToArray();
    }
    private static string Svg(string title, FieldPicture picture, string mode)
    {
        (double X, double Y) Project(V p) => (p.X * picture.Basis.RX + p.Y * picture.Basis.UX, p.X * picture.Basis.RY + p.Y * picture.Basis.UY);
        var all = picture.Sites.Select(Project).ToArray();
        var minX = all.Min(p => p.X); var maxX = all.Max(p => p.X); var minY = all.Min(p => p.Y); var maxY = all.Max(p => p.Y);
        var scale = Math.Min(672 / Math.Max(1, maxX - minX), 482 / Math.Max(1, maxY - minY));
        string Point(V p) { var q = Project(p); return F(360 + (q.X - (minX + maxX) / 2) * scale) + "," + F(270 - (q.Y - (minY + maxY) / 2) * scale); }
        string[] palette = ["#DDBB73", "#78A5A0", "#BE7F79", "#8C99BC", "#B6BB83", "#BBA0B5", "#D6A579"];
        var svg = new StringBuilder($"<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 720 540' role='img' aria-label='{H(title)}'><title>{H(title)} · {mode}</title><rect width='720' height='540' fill='#F8F5EB'/>");
        if (mode != "lines")
            foreach (var cell in picture.Cells)
                svg.Append($"<polygon points='{string.Join(' ', cell.Points.Select(Point))}' fill='{(cell.Conflict ? "#D15E62" : palette[cell.Colour % palette.Length])}' fill-opacity='{(cell.Conflict ? ".65" : ".53")}' stroke='none'/>");
        svg.Append("<g fill='none' stroke='#34575C' stroke-width='1.9' stroke-linejoin='round'>");
        foreach (var edge in picture.Edges) svg.Append($"<path d='M {Point(edge.From)} L {Point(edge.To)}'/>");
        svg.Append("</g>");
        if (mode == "removed")
        {
            svg.Append("<g fill='none' stroke='#A35D48' stroke-width='1.05' stroke-dasharray='4 4' opacity='.6'>");
            foreach (var edge in picture.Removed) svg.Append($"<path d='M {Point(edge.From)} L {Point(edge.To)}'/>");
            svg.Append("</g>");
        }
        return svg.Append("</svg>").ToString();
    }
    private static string Fingerprint(FieldPicture picture)
    {
        var minX = picture.Sites.Min(p => p.X); var minY = picture.Sites.Min(p => p.Y);
        var divisor = picture.Sites.SelectMany(p => new[] { Math.Abs(p.X - minX), Math.Abs(p.Y - minY) }).Aggregate(0L, Gcd);
        if (divisor == 0) divisor = 1;
        string Normal(V p) => $"{(p.X - minX) / divisor},{(p.Y - minY) / divisor}";
        string Cycle(CellPicture cell)
        {
            var words = Simplify(cell.Points).Select(Normal).ToArray();
            return Enumerable.Range(0, words.Length).Select(start => string.Join(";", words.Skip(start).Concat(words.Take(start)))).Order(StringComparer.Ordinal).First();
        }
        return Hash(System.Text.Encoding.UTF8.GetBytes(string.Join("|", picture.Cells.Select(Cycle).Order(StringComparer.Ordinal))));
    }
    private static long Gcd(long a, long b) { while (b != 0) (a, b) = (b, a % b); return Math.Abs(a); }

    private static object[] Counterexamples(InteriorLatticeCase sample, string output)
    {
        var stage = sample.Stages[0]; var claims = N.E(stage.Groups).Cast<Relation>().ToArray();
        var firstAtom = (Relation)N.E((Relation)claims[0].Right)[0];
        var duplicate = N.S(claims.Append(new Relation(new Placeholder(), N.S(firstAtom))).Cast<IElement>().ToArray());
        var omitted = N.S(claims.Skip(1).Cast<IElement>().ToArray());
        var far = new Relation(new Placeholder(), N.S(N.E((Relation)claims[0].Right).Concat(N.E((Relation)claims[^1].Right)).ToArray()));
        var disconnected = N.S(claims.Skip(1).SkipLast(1).Append(far).Cast<IElement>().ToArray());
        var examples = new List<object>();
        Check("X01", "One atomic interior claimed twice", stage.Complex, duplicate, stage.Bindings, sample.Frame, sample.Reach);
        Check("X02", "Unowned interior left as a gap", stage.Complex, omitted, stage.Bindings, sample.Frame, sample.Reach);
        Check("X03", "Disconnected pieces called one cell", stage.Complex, disconnected, stage.Bindings, sample.Frame, sample.Reach);
        var author = new LatticeLadderAuthor(); var sites = new List<IElement>(); var edges = new List<IElement>();
        var faces = new List<IElement>(); var bindings = new List<IElement>();
        foreach (var shift in new[] { 0, 1 })
        {
            var vertices = new[] { new V(shift, shift), new V(shift + 2, shift), new V(shift + 2, shift + 2), new V(shift, shift + 2) };
            var ids = vertices.Select(_ => (IElement)new Placeholder()).ToArray(); sites.AddRange(ids);
            for (var i = 0; i < 4; i++) bindings.Add(new Relation(ids[i], author.Word((int)vertices[i].X, (int)vertices[i].Y)));
            var boundary = Enumerable.Range(0, 4).Select(i => InteriorCellPartition.Edge(new Placeholder(), ids[i], ids[(i + 1) % 4])).ToArray();
            edges.AddRange(boundary); faces.Add(InteriorCellPartition.Face(new Placeholder(), boundary.Select(e => InteriorCellPartition.Dart(e, ((Relation)e.Right).Left, ((Relation)e.Right).Right)).ToArray()));
        }
        var complex = InteriorCellPartition.Topology(N.S(sites.ToArray()), N.S(edges.ToArray()), N.S(faces.ToArray()));
        var ownership = N.S(faces.Cast<Relation>().Select(f => (IElement)InteriorCellPartition.Claim(new Placeholder(), f)).ToArray());
        Check("X04", "Separate rectangles with overlapping interiors", complex, ownership, N.S(bindings.ToArray()), author.Frame, author.Reach);
        return examples.ToArray();

        void Check(string id, string name, Relation complex, Relation groups, Relation positions, QualitativeMotionFrame frame, IElement reach)
        {
            var read = InteriorCellPartition.Read(complex, groups, positions, frame.Surface, reach);
            if (read.IsEmbeddedPartition) throw new InvalidOperationException(id + ": a known invalid cell claim must remain rejected.");
            var picture = Picture(read, complex, groups, positions, frame, new(1, 0, 0, 1));
            Save(output, id + ".svg", Svg(name, picture, "cells"));
            Native(output, id + ".gojo.json.gz", GojoStructureCompactJson.Serialize(GojoStructureCompactJson.FromElement("interior-rule-counterexample-v1", read.Surface)));
            examples.Add(new { Id = id, Name = name, read.Summary, Blocked = N.E(read.Blocked).Length + N.E(read.EmbeddingBlocked).Length, Open = N.E(read.Open).Length + N.E(read.EmbeddingOpen).Length });
        }
    }
}
