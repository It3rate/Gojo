using System.Numerics;
using Gojo.Core;
using N = Gojo.Experiments.QualitativeFrieze.FieldNative;

namespace Gojo.Experiments.QualitativeFrieze;

public sealed partial class InteriorCellPartition
{
    /// <summary>Derived formal positions only. No screen coordinates create a
    /// join, and no coordinate read mutates the supplier's actual topology.</summary>
    private EmbeddingResult ReadEmbedding(IElement[] sites, Atom[] atoms, Dictionary<IElement, Relation> edges,
        List<InteriorCellGroupRead> groups, Relation? bindings, Relation? frame, IElement? reach)
    {
        var result = new EmbeddingResult();
        if (bindings is null || frame is null || reach is null)
        { result.Open.Add(Embedding); return result; }
        if (frame is not { Left: Relation { Right: Relation { Left: Relation horizontal, Right: Relation vertical } },
            Right: Relation { Left: Relation opposites } } || N.Same(horizontal, vertical) ||
            !N.Read(opposites, out var oppositeRows))
        { result.Open.Add(new Relation(Embedding, frame)); return result; }
        IElement? Reverse(Relation axis)
        {
            var selected = oppositeRows.OfType<Relation>().Where(r => ReferenceEquals(r.Left, axis)).Select(r => r.Right).ToArray();
            return selected.Length == 1 ? selected[0] : null;
        }
        var negativeHorizontal = Reverse(horizontal); var negativeVertical = Reverse(vertical);
        if (negativeHorizontal is not Relation inverseH || negativeVertical is not Relation inverseV ||
            !N.Same(inverseH.Left, horizontal.Right) || !N.Same(inverseH.Right, horizontal.Left) ||
            !N.Same(inverseV.Left, vertical.Right) || !N.Same(inverseV.Right, vertical.Left))
        { result.Open.Add(new Relation(Embedding, opposites)); return result; }
        if (!Closed(bindings, result.Blocked, result.Open, out var bindingRows)) return result;
        var positions = new Dictionary<IElement, FormalPoint>(N.Participants.Instance);
        var actualBindings = new Dictionary<IElement, Relation>(N.Participants.Instance);
        var siteSet = sites.ToHashSet(N.Participants.Instance);
        foreach (var item in bindingRows)
        {
            if (item is not Relation { Right: Relation word } binding || !siteSet.Contains(binding.Left) || actualBindings.ContainsKey(binding.Left))
            { result.Blocked.Add(new Relation(item, bindings)); continue; }
            actualBindings.Add(binding.Left, binding);
            if (!Closed(word, result.Blocked, result.Open, out var terms)) continue;
            long x = 0, y = 0; var complete = true;
            foreach (var term in terms)
            {
                if (term is not Relation move || !N.Same(move.Right, reach))
                { result.Open.Add(new Relation(binding, term)); complete = false; continue; }
                if (ReferenceEquals(move.Left, horizontal)) x = checked(x + 1);
                else if (ReferenceEquals(move.Left, negativeHorizontal)) x = checked(x - 1);
                else if (ReferenceEquals(move.Left, vertical)) y = checked(y + 1);
                else if (ReferenceEquals(move.Left, negativeVertical)) y = checked(y - 1);
                else { result.Open.Add(new Relation(binding, move)); complete = false; }
            }
            if (complete) positions.Add(binding.Left, new(x, y));
        }
        foreach (var site in sites.Where(site => !positions.ContainsKey(site))) result.Open.Add(new Relation(site, bindings));
        if (result.Open.Count != 0 || result.Blocked.Count != 0) return result;
        // The premise deliberately supports convex atomic triangles/quads (and
        // other convex polygons), not arbitrary filled-path inference.
        var polygons = new List<EmbeddedAtom>();
        foreach (var atom in atoms)
        {
            var points = atom.Darts.Select(d => positions[((Relation)d.Right).Left]).ToArray();
            var area2 = Area2(points); var turns = Enumerable.Range(0, points.Length)
                .Select(i => Cross(points[i], points[(i + 1) % points.Length], points[(i + 2) % points.Length])).ToArray();
            var valid = area2 != 0 && points.Distinct().Count() == points.Length &&
                turns.All(turn => turn == 0 || turn.Sign == area2.Sign);
            for (var i = 0; i < points.Length && valid; i++)
                for (var j = i + 1; j < points.Length && valid; j++)
                    if (j != i + 1 && !(i == 0 && j == points.Length - 1) &&
                        SegmentsMeet(points[i], points[(i + 1) % points.Length], points[j], points[(j + 1) % points.Length])) valid = false;
            if (!valid)
            { result.Blocked.Add(new Relation(atom.Row, N.S(atom.Darts.SelectMany(d => new[] { actualBindings[((Relation)d.Right).Left], actualBindings[((Relation)d.Right).Right] }).Cast<IElement>().ToArray()))); continue; }
            polygons.Add(new(atom, points, area2));
        }
        if (polygons.Count != atoms.Length) return result;
        var ordered = polygons.OrderBy(p => p.MinX).ToArray();
        for (var i = 0; i < ordered.Length; i++)
        {
            var a = ordered[i];
            for (var j = i + 1; j < ordered.Length && ordered[j].MinX <= a.MaxX; j++)
            {
                var b = ordered[j]; if (b.MinY > a.MaxY || b.MaxY < a.MinY) continue;
                result.ComparedPairs++;
                var pair = new Relation(a.Atom.Row, b.Atom.Row);
                if (PositiveInteriorOverlap(a.Points, b.Points))
                { result.OverlappingPairs++; result.Blocked.Add(new Relation(pair, Embedding)); continue; }
                var invalidContact = false;
                for (var ai = 0; ai < a.Points.Length; ai++)
                    for (var bi = 0; bi < b.Points.Length; bi++)
                        if (PositiveCollinearContact(a.Points[ai], a.Points[(ai + 1) % a.Points.Length], b.Points[bi], b.Points[(bi + 1) % b.Points.Length]) &&
                            !N.Same(a.Atom.Darts[ai].Left, b.Atom.Darts[bi].Left))
                        {
                            invalidContact = true;
                            result.Blocked.Add(new Relation(new Relation(a.Atom.Darts[ai], b.Atom.Darts[bi]), Embedding));
                        }
                if (!invalidContact) result.Accepted.Add(pair);
            }
        }
        var atomIndex = polygons.ToDictionary(p => p.Atom.Row, p => p);
        foreach (var group in groups)
        {
            var vertices = group.BoundaryDarts.Select(d => positions[((Relation)d.Right).Left]).ToArray();
            var area2 = Area2(vertices);
            var atomArea2 = group.Atoms.Aggregate(BigInteger.Zero, (sum, atom) => sum + BigInteger.Abs(atomIndex[atom].Area2));
            var crossing = false;
            for (var i = 0; i < vertices.Length; i++)
                for (var j = i + 1; j < vertices.Length; j++)
                    if (j != i + 1 && !(i == 0 && j == vertices.Length - 1) &&
                        SegmentsMeet(vertices[i], vertices[(i + 1) % vertices.Length], vertices[j], vertices[(j + 1) % vertices.Length])) crossing = true;
            if (area2 == 0 || BigInteger.Abs(area2) != atomArea2 || crossing)
            { result.Blocked.Add(new Relation(group.Surface, Embedding)); continue; }
            var shape = ShapeOf(group, vertices, positions, actualBindings);
            group.Shape = shape.Summary; group.ShapeWitness = shape.Witness;
            result.Accepted.Add(new Relation(group.Surface, shape.Witness));
        }
        return result;
    }

    private static bool PositiveInteriorOverlap(FormalPoint[] a, FormalPoint[] b)
    {
        foreach (var polygon in new[] { a, b })
            for (var i = 0; i < polygon.Length; i++)
            {
                var p = polygon[i]; var q = polygon[(i + 1) % polygon.Length];
                var axisX = (BigInteger)p.Y - q.Y; var axisY = (BigInteger)q.X - p.X;
                var projectionA = a.Select(v => axisX * v.X + axisY * v.Y).ToArray();
                var projectionB = b.Select(v => axisX * v.X + axisY * v.Y).ToArray();
                if (projectionA.Max() <= projectionB.Min() || projectionB.Max() <= projectionA.Min()) return false;
            }
        return true;
    }
    private static bool PositiveCollinearContact(FormalPoint a, FormalPoint b, FormalPoint c, FormalPoint d)
    {
        if (Cross(a, b, c) != 0 || Cross(a, b, d) != 0) return false;
        return a.X != b.X ? Math.Max(Math.Min(a.X, b.X), Math.Min(c.X, d.X)) < Math.Min(Math.Max(a.X, b.X), Math.Max(c.X, d.X)) :
            Math.Max(Math.Min(a.Y, b.Y), Math.Min(c.Y, d.Y)) < Math.Min(Math.Max(a.Y, b.Y), Math.Max(c.Y, d.Y));
    }
    private static bool SegmentsMeet(FormalPoint a, FormalPoint b, FormalPoint c, FormalPoint d)
    {
        if (Math.Max(a.X, b.X) < Math.Min(c.X, d.X) || Math.Max(c.X, d.X) < Math.Min(a.X, b.X) ||
            Math.Max(a.Y, b.Y) < Math.Min(c.Y, d.Y) || Math.Max(c.Y, d.Y) < Math.Min(a.Y, b.Y)) return false;
        var abc = Cross(a, b, c); var abd = Cross(a, b, d); var cda = Cross(c, d, a); var cdb = Cross(c, d, b);
        return abc.Sign * abd.Sign <= 0 && cda.Sign * cdb.Sign <= 0;
    }
    private static BigInteger Cross(FormalPoint a, FormalPoint b, FormalPoint c) => ((BigInteger)b.X - a.X) * ((BigInteger)c.Y - a.Y) - ((BigInteger)b.Y - a.Y) * ((BigInteger)c.X - a.X);
    private static BigInteger Area2(IReadOnlyList<FormalPoint> points) => Enumerable.Range(0, points.Count).Aggregate(BigInteger.Zero,
        (sum, i) => sum + (BigInteger)points[i].X * points[(i + 1) % points.Count].Y - (BigInteger)points[(i + 1) % points.Count].X * points[i].Y);
    private sealed record FormalPoint(long X, long Y);
    private sealed class EmbeddedAtom(Atom atom, FormalPoint[] points, BigInteger area2)
    {
        public Atom Atom { get; } = atom;
        public FormalPoint[] Points { get; } = points;
        public BigInteger Area2 { get; } = area2;
        public long MinX { get; } = points.Min(p => p.X);
        public long MaxX { get; } = points.Max(p => p.X);
        public long MinY { get; } = points.Min(p => p.Y);
        public long MaxY { get; } = points.Max(p => p.Y);
    }
    private sealed class EmbeddingResult
    {
        public List<IElement> Accepted { get; } = [];
        public List<IElement> Blocked { get; } = [];
        public List<IElement> Open { get; } = [];
        public int ComparedPairs { get; set; }
        public int OverlappingPairs { get; set; }
    }
}
