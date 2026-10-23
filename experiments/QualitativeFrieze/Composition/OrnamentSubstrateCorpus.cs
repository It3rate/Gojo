using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;
using static Gojo.Experiments.QualitativeFrieze.Composition.JointNative;
using Band = Gojo.Experiments.QualitativeExecution.QualitativeBand;
using Curve = Gojo.Experiments.QualitativeExecution.QualitativeCurve;
using Parts = Gojo.Experiments.QualitativeFrieze.Composition.OrnamentBandComposition;
using Vocabulary = Gojo.Experiments.QualitativeFrieze.Composition.OrnamentCompositionCorpus;
using Substrate = Gojo.Experiments.QualitativeFrieze.Composition.OrnamentSubstrate;
using V = Gojo.Experiments.QualitativeFrieze.Composition.OrnamentCompositionCorpus.V;

namespace Gojo.Experiments.QualitativeFrieze.Composition;

/// <summary>Continuous routes and shared cell boundaries over an independent
/// repeat substrate. Host coordinates author finite common-reach words; all
/// repeat landmarks, encounters and edge correspondences are native structure.</summary>
public static class OrnamentSubstrateCorpus
{
    // A cell is a CLOSED scope of ordered actual curve sections, not a polygon
    // name. The successive section end/start identities must close a cycle.
    // design = unit -> (lattice -> (Closed(actual thread -> event scope) ->
    //          (Closed(boundary pair reads) ->
    //           (Closed(actual cell cycles) -> Closed(profile applications)))))
    // profile = frame -> (Closed(local controls) -> (Closed(side A offsets) -> Closed(side B offsets)))
    // application = (actual profile -> (anchor selection -> actual anchor word)) ->
    //               (Closed(actual sum receipts) -> actual generated curve)
    public sealed record Design(string Id, string Name, string Note, string Repertoire,
        Vocabulary.Design Ornament, Substrate.Lattice Lattice,
        IReadOnlyList<Substrate.CrossRead> Crosses, IReadOnlyList<Substrate.VisitRead> Visits,
        IReadOnlyList<Substrate.BoundaryPairRead> Pairs, IReadOnlyList<Relation> Cells, IReadOnlyList<Relation> Generations,
        Relation Focus, Relation Surface);

    public static IReadOnlyList<Design> Cases(int limit = int.MaxValue)
    {
        var output = new List<Design>();
        void Add(Func<int, Design> make) { if (output.Count < limit) output.Add(make(output.Count + 1)); }
        Add(n => Weave(n, "Unbroken plain weave", new(18, 0), new(0, 18), 5, 5, 1));
        Add(n => Weave(n, "Two-over two-under twill", new(14, 0), new(0, 14), 7, 7, 2));
        Add(n => Weave(n, "Diagonal braid field", new(16, 12), new(16, -12), 5, 5, 1));
        Add(n => Triangles(n, false));
        Add(n => Waves(n, false));
        Add(n => Waves(n, true));
        Add(Scales);
        Add(Vines);
        Add(n => Cells(n, false, false));
        Add(n => Cells(n, true, false));
        Add(n => Triangles(n, true));
        Add(n => Cells(n, true, true));
        return output;
    }

    private static Design Weave(int number, string name, V u, V v, int columns, int rows, int run)
    {
        var b = new Builder(u, v, columns, rows); var horizontals = new Relation[rows]; var verticals = new Relation[columns];
        for (var r = 0; r < rows; r++)
        {
            var p = b.Point(0, r); var q = b.Point(columns - 1, r);
            horizontals[r] = b.Chain(1, [(new[] { p + -u, q + u }, new[] { b.Normal(u) * 2, b.Normal(u) * 2 })])[0];
        }
        for (var c = 0; c < columns; c++)
        {
            var p = b.Point(c, 0); var q = b.Point(c, rows - 1);
            verticals[c] = b.Chain(2, [(new[] { p + -v, q + v }, new[] { b.Normal(v) * 2, b.Normal(v) * 2 })])[0];
        }
        // Two native phase cycles, not a colour-based or SVG depth inference.
        var phases = Enumerable.Range(0, run * 2).Select(_ => (IElement)new Placeholder()).ToArray();
        var cycle = new Relation(S(phases), S(phases.Select((p, i) => (IElement)new Relation(p, phases[(i + 1) % phases.Length])).ToArray()));
        var frontPhases = S(phases.Take(run).ToArray()); var decisions = new List<IElement>();
        for (var r = 0; r < rows; r++) for (var c = 0; c < columns; c++)
        {
            var phase = phases[(c + r) % phases.Length]; var first = E(frontPhases).Any(p => ReferenceEquals(p, phase));
            var h = horizontals[r]; var vv = verticals[c]; var front = first ? h : vv;
            var point = Substrate.At(b.Lattice, c, r); var crossing = Substrate.CrossEvent(b.Lattice, point, h, vv); b.Crosses.Add(crossing);
            var hs = Band.ReadEnvelope(b.Frame, h).Sweep.Spine.Section; var vs = Band.ReadEnvelope(b.Frame, vv).Sweep.Spine.Section;
            var order = Band.Over(Band.Crossing(b.Frame, hs, vs, point.Position), first ? hs : vs, first ? vs : hs);
            b.Orders.Add(new(h, vv, point.Position, front, order));
            decisions.Add(new Relation(new Relation(point.Surface, new Relation(cycle, new Relation(frontPhases, phase))), order));
        }
        b.Provenance.Add(new Relation(cycle, S(decisions.ToArray())));
        b.Focus = S(horizontals[rows / 2]);
        b.Pair(horizontals[0], horizontals[1], v); b.Pair(verticals[0], verticals[1], u);
        return b.Finish(number, name,
            "Each coloured strand is one full-length native line. Substrate encounters repeat along it; only the ordered crossing phase changes.",
            "Day, Pattern Design, interlacing and continuous pattern; Jones, Moresque interlacement");
    }

    private static Design Waves(int number, bool opposed)
    {
        var b = new Builder(new(18, 0), new(0, opposed ? 20 : 18), 7, 5);
        for (var r = 0; r < b.Rows; r++)
        {
            var signs = opposed ? new[] { 1, -1 } : new[] { 1 };
            foreach (var sign in signs)
            {
                var parts = new List<(V[], V[])>();
                for (var c = 0; c < b.Columns - 1; c++)
                {
                    var p = b.Point(c, r); var q = b.Point(c + 1, r);
                    if (opposed)
                    {
                        var direction = c % 2 == 0 ? sign : -sign;
                        parts.Add(([p, p + new V(9, 13 * direction), q], [new V(0, 1), new V(0, 1), new V(0, 1)]));
                    }
                    else parts.Add(([p, p + new V(6, 9), p + new V(12, -9), q], [new V(0, 1), new V(0, 1), new V(0, 1), new V(0, 1)]));
                }
                var chain = b.Chain(1, parts.ToArray(), sites: opposed ? Enumerable.Range(0, b.Columns).Select(c => Substrate.At(b.Lattice, c, r).Site).ToArray() : null);
                for (var c = 0; c < chain.Length; c++) b.Visit(c, r, chain[c]);
                b.Visit(b.Columns - 1, r, chain[^1]);
                b.Pair(chain[0], chain[opposed ? 2 : 1], b.U * (opposed ? 2 : 1));
                if (r == 2 && sign == 1) b.Focus = S(chain.Cast<IElement>().ToArray());
            }
        }
        if (opposed)
        {
            // The leaf is placed in each lens, not used to define its period.
            var leaf = Vocabulary.Leaf(b.Frame, 7);
            for (var r = 0; r < b.Rows; r++) for (var c = 0; c < b.Columns - 1; c++)
            {
                var p = b.Point(c, r); var upward = c % 2 == 0;
                b.Decorate(leaf, upward ? Parts.Identity(b.Frame) : Vocabulary.Half(b.Frame), p + new V(9, upward ? -3 : 3));
            }
        }
        return b.Finish(number, opposed ? "Opposed waves and ogee lenses" : "Zigzag softened into continuous waves",
            opposed ? "Opposed continuous chains share the substrate sites. Lens-shaped spaces emerge between them; the green infill follows those compartments." :
            "A single strand passes through every column marker. Its periodic cubic deviation changes the line without changing the repeat substrate.",
            "Day, The Anatomy of Pattern, plates 11 and 15: wave, ogee and scale construction");
    }

    private static Design Scales(int number)
    {
        var b = new Builder(new(24, 0), new(12, 15), 5, 5);
        var flower = Vocabulary.Flower(b.Frame, 4);
        for (var r = 0; r < b.Rows; r++)
        {
            var parts = new List<(V[], V[])>();
            for (var c = 0; c < b.Columns - 1; c++)
            {
                var p = b.Point(c, r); var q = b.Point(c + 1, r);
                parts.Add(([p, p + new V(2, 18), q + new V(-2, 18), q], [new V(0, 1), new V(-1, 1), new V(1, 1), new V(0, 1)]));
                b.Decorate(flower, Parts.Identity(b.Frame), p + new V(12, 8));
            }
            var chain = b.Chain(1, parts.ToArray());
            for (var c = 0; c < chain.Length; c++) b.Visit(c, r, chain[c]);
            b.Visit(b.Columns - 1, r, chain[^1]); b.Pair(chain[0], chain[1], b.U);
            if (r == 2) b.Focus = S(chain.Cast<IElement>().ToArray());
        }
        return b.Finish(number, "Scalloped half-drop courses", "One scalloped thread crosses an entire course. The second lattice generator supplies the row drop; flowers occupy the resulting scales.",
            "Day, Anatomy of Pattern, plate 15; Pattern Design, dropped repeats");
    }

    private static Design Vines(int number)
    {
        var b = new Builder(new(24, 0), new(0, 24), 5, 5); var leaf = Vocabulary.Leaf(b.Frame, 9);
        for (var c = 0; c < b.Columns; c++)
        {
            var parts = new List<(V[], V[])>();
            for (var r = 0; r < b.Rows - 1; r++)
            {
                var p = b.Point(c, r); var q = b.Point(c, r + 1);
                parts.Add(([p, p + new V(10, 8), p + new V(-10, 16), q], [new V(-1, 0), new V(-1, 0), new V(-1, 0), new V(-1, 0)]));
                b.Decorate(leaf, (r + c) % 2 == 0 ? Vocabulary.Map(b.Frame, new(0, 1), new(1, 1)) : Vocabulary.Map(b.Frame, new(0, -1), new(-1, 1)),
                    p + new V(0, 12));
            }
            var chain = b.Chain(1, parts.ToArray());
            for (var r = 0; r < chain.Length; r++) b.Visit(c, r, chain[r]);
            b.Visit(c, b.Rows - 1, chain[^1]); b.Pair(chain[0], chain[1], b.V);
            if (c == 2) b.Focus = S(chain.Cast<IElement>().ToArray());
        }
        return b.Finish(number, "Sinuous foliate cables", "The stem is one shared-site curved chain per column. Leaves are a separate reusable compound positioned from substrate landmarks, not the units from which the stem is assembled.",
            "Jones, conventional tangential foliage; Day, continuous line pattern");
    }

    private static Design Triangles(int number, bool curved)
    {
        var b = new Builder(new(24, 0), new(12, 20), 5, 5);
        var edges = new Dictionary<(int C, int R, int D), Relation>();
        Relation Edge(int c, int r, int dc, int dr, int d)
        {
            var p = b.Point(c, r); var q = b.Point(c + dc, r + dr); var normal = b.Normal(q + -p);
            var controls = curved ? new[] { p, new V((p.X + q.X) / 2, (p.Y + q.Y) / 2) + normal * 4, q } : [p, q];
            var offsets = controls.Select(_ => normal).ToArray();
            var env = b.Chain(1, [(controls, offsets)], sites: [Substrate.At(b.Lattice, c, r).Site, Substrate.At(b.Lattice, c + dc, r + dr).Site])[0];
            b.Visit(c, r, env); b.Visit(c + dc, r + dr, env); edges[(c, r, d)] = env; return env;
        }
        for (var r = 0; r < b.Rows; r++) for (var c = 0; c < b.Columns - 1; c++) Edge(c, r, 1, 0, 0);
        for (var c = 0; c < b.Columns; c++) for (var r = 0; r < b.Rows - 1; r++) Edge(c, r, 0, 1, 1);
        for (var r = 0; r < b.Rows - 1; r++) for (var c = 1; c < b.Columns; c++) Edge(c, r, -1, 1, 2);
        for (var r = 0; r < b.Rows - 1; r++) for (var c = 0; c < b.Columns - 1; c++)
        {
            b.Cell(edges[(c, r, 0)], edges[(c + 1, r, 2)], edges[(c, r, 1)]);
            b.OtherTriangle(edges[(c, r + 1, 0)], edges[(c + 1, r, 1)], edges[(c + 1, r, 2)]);
            if (curved && (c + r) % 2 == 0) b.Decorate(Vocabulary.Flower(b.Frame, 4), Parts.Identity(b.Frame), b.Point(c, r) + new V(12, 7));
        }
        foreach (var d in new[] { 0, 1, 2 })
        {
            var first = edges.First(k => k.Key.D == d); var neighbour = edges.First(k => k.Key.D == d && k.Key.C == first.Key.C + 1 && k.Key.R == first.Key.R);
            b.Pair(first.Value, neighbour.Value, b.U);
        }
        b.Focus = S(edges.Where(p => p.Key.R == 2 && p.Key.D == 0).Select(p => (IElement)p.Value).ToArray());
        return b.Finish(number, curved ? "Bowed triangular counterchange" : "Three-family triangular net",
            curved ? "Each triangular edge is bowed, with the same curve used on both sides of an adjacent compartment. The repeat lattice remains triangular while the visible faces become curved." :
            "Three families share actual junction sites. The triangular faces are read from selected edge cycles, rather than drawn as independent overlapping triangle motifs.",
            "Day, triangular networks and alternative construction recipes; Bourgoin triangular mesh");
    }

    private static Design Cells(int number, bool tabs, bool oblique)
    {
        var b = new Builder(new(28, 0), oblique ? new(14, 24) : new(0, 28), 5, 4);
        var h = new Dictionary<(int, int), Relation[]>(); var v = new Dictionary<(int, int), Relation[]>();
        (V[], V[])[] Profile(V p, V delta, bool horizontal)
        {
            var normal = b.Normal(delta); var n = normal * (tabs ? 7 : 6);
            V Fraction(int numerator, int denominator) => new(delta.X * numerator / denominator, delta.Y * numerator / denominator);
            if (!tabs) return [([p, p + Fraction(1, 3) + n, p + Fraction(2, 3) + -n, p + delta], [normal, normal, normal, normal])];
            var a = p + Fraction(1, 3); var z = p + Fraction(2, 3);
            return [([p, a], [normal, normal]), ([a, a + n, z + n, z], [normal, normal, normal, normal]), ([z, p + delta], [normal, normal])];
        }
        for (var r = 0; r < b.Rows; r++)
        {
            var all = new List<(V[], V[])>();
            for (var c = 0; c < b.Columns - 1; c++) all.AddRange(Profile(b.Point(c, r), b.U, true));
            var pieces = tabs ? 3 : 1; var sites = new IElement[all.Count + 1];
            for (var i = 0; i < sites.Length; i++) sites[i] = i % pieces == 0 ? Substrate.At(b.Lattice, i / pieces, r).Site : new Placeholder();
            var chain = b.Chain(1, all.ToArray(), sites: sites);
            for (var c = 0; c < b.Columns - 1; c++) { h[(c, r)] = chain.Skip(c * pieces).Take(pieces).ToArray(); b.Visit(c, r, chain[c * pieces]); }
            b.Visit(b.Columns - 1, r, chain[^1]); if (r == 1) b.Focus = S(chain.Cast<IElement>().ToArray());
        }
        for (var c = 0; c < b.Columns; c++)
        {
            var all = new List<(V[], V[])>();
            for (var r = 0; r < b.Rows - 1; r++) all.AddRange(Profile(b.Point(c, r), b.V, false));
            var pieces = tabs ? 3 : 1; var sites = new IElement[all.Count + 1];
            for (var i = 0; i < sites.Length; i++) sites[i] = i % pieces == 0 ? Substrate.At(b.Lattice, c, i / pieces).Site : new Placeholder();
            var chain = b.Chain(1, all.ToArray(), sites: sites);
            for (var r = 0; r < b.Rows - 1; r++) { v[(c, r)] = chain.Skip(r * pieces).Take(pieces).ToArray(); b.Visit(c, r, chain[r * pieces]); }
            b.Visit(c, b.Rows - 1, chain[^1]);
        }
        for (var r = 0; r < b.Rows - 1; r++) for (var c = 0; c < b.Columns - 1; c++)
        {
            b.CellCycle(h[(c, r)], v[(c + 1, r)], h[(c, r + 1)], v[(c, r)]);
            for (var k = 0; k < h[(c, r)].Length; k++) b.Pair(h[(c, r)][k], h[(c, r + 1)][k], b.V);
            for (var k = 0; k < v[(c, r)].Length; k++) b.Pair(v[(c, r)][k], v[(c + 1, r)][k], b.U);
            var centre = b.Point(c, r) + new V((b.U.X + b.V.X) / 2, (b.U.Y + b.V.Y) / 2);
            if (!tabs) b.Decorate(Vocabulary.Flower(b.Frame, 6), Parts.Identity(b.Frame), centre);
            else b.Decorate(Vocabulary.Leaf(b.Frame, 10), (r + c) % 2 == 0 ? Vocabulary.Quarter(b.Frame) : Vocabulary.Half(b.Frame), centre + new V(0, -3));
        }
        return b.Finish(number, oblique ? "Interlocking oblique feather cells" : tabs ? "Paired tab-and-recess compartments" : "S-shaped counterchange cells",
            "Each neighbour selects the same actual boundary, in opposite traversal order. Translated opposite edges retain all control correspondences: an excursion into one cell is the matching recess of its neighbour.",
            "Day, Pattern Design, figs 126–129: counterchange and compensated repeat boundaries");
    }

    private sealed class Builder
    {
        public Curve.Frame Frame { get; } = new(new QualitativeMotionFrame());
        public V U { get; } public V V { get; } public int Columns { get; } public int Rows { get; }
        public Substrate.Lattice Lattice { get; }
        public List<Substrate.CrossRead> Crosses { get; } = []; public List<Substrate.VisitRead> Visits { get; } = [];
        public List<Substrate.BoundaryPairRead> Pairs { get; } = []; public List<Relation> Cells { get; } = [];
        public List<Vocabulary.Crossing> Orders { get; } = []; public List<IElement> Provenance { get; } = [];
        public List<Relation> Generations { get; } = [];
        public Relation? Focus { get; set; }
        private readonly List<Relation> threads = []; private readonly List<Relation>[] roles = [[], [], [], []];
        private readonly List<Vocabulary.Unit> decorations = [];
        private sealed record Profile(V[] Deltas, V[] Offsets, Relation Controls, Relation SideA, Relation SideB, Relation Surface);
        private readonly List<Profile> profiles = [];
        public Builder(V u, V v, int columns, int rows)
        { U = u; V = v; Columns = columns; Rows = rows; Lattice = Substrate.Create(Frame, Word(u), Word(v), Word(new(12, 12)), columns, rows); }
        private Relation Word(V p) => Frame.Word(p.X, p.Y);
        public V Point(int c, int r)
        { Frame.Vector(Substrate.At(Lattice, c, r).Position, out var p); return new((int)p.X, (int)p.Y); }
        public V Normal(V d) => new(-Math.Sign(d.Y), Math.Sign(d.X));
        public Relation[] Chain(int role, (V[] Controls, V[] Offsets)[] parts, IElement[]? sites = null)
        {
            sites ??= Enumerable.Range(0, parts.Length + 1).Select(_ => (IElement)new Placeholder()).ToArray();
            var a = Enumerable.Range(0, parts.Length + 1).Select(_ => (IElement)new Placeholder()).ToArray();
            var b = Enumerable.Range(0, parts.Length + 1).Select(_ => (IElement)new Placeholder()).ToArray();
            var output = new List<Relation>();
            for (var i = 0; i < parts.Length; i++)
            {
                var p = parts[i]; var origin = p.Controls[0]; var deltas = p.Controls.Select(c => c + -origin).ToArray();
                // Matching authoring arrays only chooses reuse. Native profile
                // words, consumed position receipts and the resulting section
                // carry the reusable geometry after this cache disappears.
                var profile = profiles.FirstOrDefault(s => s.Deltas.SequenceEqual(deltas) && s.Offsets.SequenceEqual(p.Offsets));
                if (profile is null)
                {
                    var controls = S(deltas.Select(d => (IElement)Word(d)).ToArray());
                    var aa = S(p.Offsets.Select(o => (IElement)Word(o)).ToArray()); var bb = S(p.Offsets.Select(o => (IElement)Word(-o)).ToArray());
                    profile = new(deltas, p.Offsets, controls, aa, bb, new Relation(Frame.Surface, new Relation(controls, new Relation(aa, bb))));
                    profiles.Add(profile);
                }
                var landmark = Lattice.Landmarks.FirstOrDefault(l => Frame.Vector(l.Position, out var at) && at == new Curve.Vector(origin.X, origin.Y));
                var anchor = landmark?.Position ?? Word(origin);
                IElement anchorSelection = landmark is null ? anchor : new Relation(Lattice.Surface, landmark.Surface);
                var receipts = E(profile.Controls).Cast<Relation>().Select(local => Frame.Motion.Reduce(anchor, local)).ToArray();
                var words = receipts.Select(receipt => Frame.Motion.TryReadResult(receipt, out var result, out _, out _) ? result : StructuralConstraint.Open(receipt)).ToArray();
                var curve = words.Length == 2 ? Frame.Line(sites[i], sites[i + 1], words[0], words[1]) : words.Length == 3 ?
                    Frame.Quadratic(sites[i], sites[i + 1], words[0], words[1], words[2]) : Frame.Cubic(sites[i], sites[i + 1], words[0], words[1], words[2], words[3]);
                var generation = new Relation(new Relation(profile.Surface, new Relation(anchorSelection, anchor)),
                    new Relation(S(receipts.Cast<IElement>().ToArray()), curve));
                Generations.Add(generation); Provenance.Add(generation);
                var sweep = Band.Sweep(Frame, curve, E(profile.SideA).Cast<Relation>().ToArray(), E(profile.SideB).Cast<Relation>().ToArray(), a[i], a[i + 1], b[i], b[i + 1]);
                var read = Band.Read(Frame, sweep);
                var envelope = Band.BandEnvelope(sweep, Band.Continue(Frame, read, true), Band.Continue(Frame, read, false), role is 0 or 3 ? null : Band.Occupy(read));
                output.Add(envelope); roles[role].Add(envelope);
            }
            threads.Add(S(output.Cast<IElement>().ToArray())); return output.ToArray();
        }
        public void Visit(int c, int r, Relation e) => Visits.Add(Substrate.Visit(Lattice, Substrate.At(Lattice, c, r), e));
        public void Pair(Relation a, Relation b, V translation) => Pairs.Add(Substrate.PairBoundary(Frame,
            Band.ReadEnvelope(Frame, a).Sweep.Spine.Section, Band.ReadEnvelope(Frame, b).Sweep.Spine.Section, Parts.Identity(Frame), Word(translation)));
        public void Decorate(Vocabulary.Unit source, QualitativePatternTransform map, V position)
        { var actual = Vocabulary.Place(source, map, position); decorations.Add(actual); }
        public void Cell(Relation bottom, Relation diagonal, Relation left)
        { Cells.Add(S(Spine(bottom), Spine(diagonal), Reversed(Spine(left)))); }
        public void OtherTriangle(Relation top, Relation right, Relation diagonal)
        { Cells.Add(S(Spine(right), Reversed(Spine(top)), Reversed(Spine(diagonal)))); }
        public void CellCycle(Relation[] bottom, Relation[] right, Relation[] top, Relation[] left)
        { Cells.Add(S(bottom.Concat(right).Select(e => (IElement)Spine(e)).Concat(top.Reverse().Concat(left.Reverse()).Select(e => (IElement)Reversed(Spine(e)))).ToArray())); }
        private Relation Reversed(Relation source)
        { var reversed = Frame.Reverse(source); Provenance.Add(new Relation(source, reversed)); return reversed; }
        private Relation Spine(Relation e) => Band.ReadEnvelope(Frame, e).Sweep.Spine.Section;
        public Design Finish(int number, string name, string note, string repertoire)
        {
            var template = Parts.Create(Frame, threads.ToArray()); Focus ??= threads[0];
            var unit = Vocabulary.Make(template, roles.Select(r => S(r.Cast<IElement>().ToArray())).ToArray(), Orders,
                [], [], [], Focus, null, null, S(Provenance.ToArray()));
            if (decorations.Count > 0) unit = Vocabulary.Merge([unit, .. decorations]);
            var routes = S(threads.Select(thread => (IElement)new Relation(thread,
                new Relation(Lattice.Surface, S(Visits.Select(v => (IElement)v.Surface).Concat(Crosses.Select(c => (IElement)c.Surface)).Where(e => ReferencesThread(thread, e)).ToArray())))).ToArray());
            // Route membership is exact endpoint/envelope identity; this helper
            // only selects already-built local event surfaces, not geometry.
            bool ReferencesThread(Relation thread, IElement e)
            { return E(thread).Any(env => Visits.Any(v => ReferenceEquals(v.Surface, e) && ReferenceEquals(((Relation)v.Surface.Left).Right, env)) ||
                Crosses.Any(c => ReferenceEquals(c.Surface, e) && c.Surface.Right is Relation body && ContainsPair(body.Left, env))); }
            static bool ContainsPair(IElement pair, IElement env) => pair is Relation p &&
                (ReferenceEquals(p.Left, env) || ReferenceEquals(p.Right, env) || p.Right is Relation q && (ReferenceEquals(q.Left, env) || ReferenceEquals(q.Right, env)));
            var surface = new Relation(unit.Surface, new Relation(Lattice.Surface, new Relation(routes,
                new Relation(S(Pairs.Select(p => (IElement)p.Surface).ToArray()),
                    new Relation(S(Cells.Cast<IElement>().ToArray()), S(Generations.Cast<IElement>().ToArray()))))));
            var controls = unit.Template.Envelopes.SelectMany(e => Band.ReadEnvelope(Frame, e).Sweep.Spine.Controls).ToArray();
            var maxX = (int)controls.Max(p => p.X) + 8; var maxY = (int)controls.Max(p => p.Y) + 8;
            var id = "S" + number.ToString("000");
            var ornament = new Vocabulary.Design(id, name, note, repertoire, unit, maxX, maxY,
                "Substrate landmarks -> continuous band threads -> encounter/endpoint reads -> paired full boundary controls -> selected cell cycles",
                "Finite authored common-reach realization. No infinite tessellation proof, exact curve offset, automatic curved crossing discovery or learned aesthetic judgement.");
            return new(id, name, note, repertoire, ornament, Lattice, Crosses, Visits, Pairs, Cells, Generations, Focus, surface);
        }
    }
}
