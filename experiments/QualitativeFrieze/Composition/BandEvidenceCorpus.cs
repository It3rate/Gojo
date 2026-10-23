using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;
using static Gojo.Experiments.QualitativeFrieze.Composition.JointNative;
using Curve = Gojo.Experiments.QualitativeExecution.QualitativeCurve;
using Band = Gojo.Experiments.QualitativeExecution.QualitativeBand;

namespace Gojo.Experiments.QualitativeFrieze.Composition;

/// <summary>Finite authoring recipes, compiled to native curves, side-word
/// correspondences, envelopes and crossing orders before they are read.
/// Integers count a shared, uncalibrated reach; they are not measured lengths.</summary>
public static class BandEvidenceCorpus
{
    public readonly record struct V(int X, int Y)
    {
        public static V operator +(V a, V b) => new(a.X + b.X, a.Y + b.Y);
        public static V operator -(V a) => new(-a.X, -a.Y);
        public static V operator *(V a, int b) => new(a.X * b, a.Y * b);
    }
    public sealed record Thread(Relation Source, Relation Surface);
    public sealed record Layer(Curve.Frame Frame, IReadOnlyList<Thread> Threads, IReadOnlyList<Relation> Crossings,
        IReadOnlyList<Relation> Orders, Relation Surface);
    public sealed record Case(string Id, string Name, string Note, Curve.Frame Frame, Layer Narrow, Layer Broad,
        Relation Source, Relation Surface, bool Decorate = false, bool ReverseFigure = false,
        bool Cropped = false, bool Joined = false, string Origin = "Authored qualitative recipe");

    public static IReadOnlyList<Case> Cases()
    {
        var cases = new List<Case>();
        void Add(Author a, string name, string note, bool decorate = false, bool reverse = false,
            bool cropped = false, bool joined = false, string origin = "Authored qualitative recipe")
        {
            var narrow = a.Build(1); var broad = a.Build(3);
            var correspondence = S(narrow.Threads.Zip(broad.Threads, (x, y) =>
                (IElement)new Relation(x.Surface, y.Surface)).ToArray());
            var authored = a.Source;
            var root = new Relation(authored, new Relation(new Relation(narrow.Surface, broad.Surface), correspondence));
            cases.Add(new("W" + (cases.Count + 1).ToString("000"), name, note, a.Frame, narrow, broad, authored, root,
                decorate, reverse, cropped, joined, origin));
        }

        var straight = new Author(); straight.Line(new(3, 14), new(39, 14), new(0, 1));
        Add(straight, "One passage, two sides", "A supplied centre path grows to paired sides. Occupancy and visible end caps are separate native requests.");

        var taper = new Author();
        taper.Curve([new(3, 6), new(15, 3), new(27, 24), new(39, 20)], [new(0, 0), new(-1, 2), new(-1, 2), new(0, 0)]);
        Add(taper, "Stem: stroke to band to stroke", "The transverse profile collapses at both tips and opens between them. The path is retained when the sides become coincident.", decorate: true);

        var vine = new Author();
        vine.Curve([new(3, 14), new(12, 31), new(30, -3), new(39, 14)], [new(-1, 1), new(0, 2), new(0, 2), new(-1, 1)]);
        Add(vine, "An S-shaped vine", "Authored offset-control words follow the S. These are polynomial side correspondences, not an exact constant-distance offset.", decorate: true);

        var turn = new Author();
        turn.Curve([new(8, 4), new(8, 17), new(20, 26), new(33, 26)], [new(-1, 0), new(-1, 0), new(0, 1), new(0, 1)]);
        Add(turn, "A rounded turn", "Side curves use the same longitudinal interpolation parameter as the centre. End tangents retain their correspondence.");

        var key = new Author(); key.Polyline([new(4, 6), new(4, 24), new(37, 24), new(37, 6), new(17, 6), new(17, 15), new(28, 15)]);
        Add(key, "An angular meander becomes a ribbon", "Shared side sites make actual corner connections. Miter-like offset words are supplied; they are not inferred from paint.");

        var square = new Author(); square.Polyline([new(10, 4), new(32, 4), new(32, 26), new(10, 26)], true);
        Add(square, "Closed route, open central ground", "The route closes onto itself. Its occupied strip leaves an interior hole; filling the strip does not fill the centre.", reverse: true);

        var diamond = new Author(); diamond.Polyline([new(21, 2), new(38, 15), new(21, 28), new(4, 15)], true);
        Add(diamond, "Diamond boundary and diamond passage", "A complete circuit can be read as a route or as the boundary around a compartment. The selected band occupancy is only between its paired sides.", decorate: true);

        var plus = new Author(); var centre = new Placeholder();
        plus.Line(new(21, 15), new(4, 15), new(0, -1), from: centre);
        plus.Line(new(21, 15), new(38, 15), new(0, 1), from: centre);
        plus.Line(new(21, 15), new(21, 2), new(1, 0), from: centre);
        plus.Line(new(21, 15), new(21, 28), new(-1, 0), from: centre);
        Add(plus, "A joined plus needs no over/under", "Four branches share one actual centre site. Their filled supports can meet, with no foreground passage or underpass gap.", joined: true);

        var crossing = new Author();
        crossing.Line(new(3, 15), new(39, 15), new(0, 1));
        crossing.Line(new(21, 1), new(21, 29), new(-1, 0));
        crossing.Cross(0, 0, 1, 0, new(21, 15));
        Add(crossing, "A cross with order left open", "The paths cross in projection but share no endpoint site. Missing depth order stays open; one line is not silently declared foreground.");

        foreach (var horizontalFront in new[] { true, false })
        {
            var weave = new Author();
            weave.Line(new(3, 15), new(39, 15), new(0, 1));
            weave.Line(new(21, 1), new(21, 29), new(-1, 0));
            weave.Cross(0, 0, 1, 0, new(21, 15), horizontalFront ? 0 : 1);
            Add(weave, horizontalFront ? "Horizontal passage over vertical" : "The same crossing, reversed order",
                "Only the local crossing-order relation changes. The underlying underpass remains continuous; a mask is a view selection, not a deletion.");
        }

        var basket = new Author();
        for (var y = 3; y <= 27; y += 8) basket.Line(new(-4, y), new(46, y), new(0, 1), continued: true);
        for (var x = 5; x <= 37; x += 8) basket.Line(new(x, -5), new(x, 35), new(-1, 0), continued: true);
        for (var row = 0; row < 4; row++) for (var col = 0; col < 5; col++)
            basket.Cross(row, 0, 4 + col, 0, new(5 + 8 * col, 3 + 8 * row), (row + col) % 2 == 0 ? row : 4 + col);
        Add(basket, "Basket field: alternating passages", "Alternating crossing order turns continuous horizontal and vertical threads into a weave. The narrow view uses small gaps; the wide view exposes side edges.",
            cropped: true, origin: "B02 basket repertoire: new continuous-thread reading; not inferred depth in the older B02 contours");

        var guilloche = new Author();
        for (var strand = 0; strand < 2; strand++)
        {
            var sign = strand == 0 ? 1 : -1;
            guilloche.Chain(Enumerable.Range(-1, 4).Select(n => {
                var s = n % 2 == 0 ? sign : -sign; var x = 21 * n;
                return (new[] { new V(x, 15), new V(x + 6, 15 + 13 * s), new V(x + 15, 15 + 13 * s), new V(x + 21, 15) },
                    new[] { new V(-s, 1), new V(0, 1), new V(0, 1), new V(s, 1) });
            }).ToArray(), continued: true);
        }
        guilloche.Cross(0, 1, 1, 1, new(21, 15));
        Add(guilloche, "Two-strand guilloche, order unresolved", "Paired curved passages are explicit. Curved crossing certification is intentionally outside this first local reader, so no depth is invented.", cropped: true);

        var wave = new Author(); wave.Polyline([new(-6, 7), new(7, 7), new(7, 23), new(21, 23), new(21, 7), new(35, 7), new(35, 23), new(48, 23)], continued: true);
        Add(wave, "A square-wave course", "The same route survives broadening. Its boundaries use shared corner sites instead of independent decorative strokes.", reverse: true, cropped: true);

        var diagonal = new Author();
        for (var n = -1; n < 5; n++) diagonal.Line(new(-12 + 12 * n, -4), new(24 + 12 * n, 32), new(-1, 1), continued: true);
        Add(diagonal, "Diagonal figure and ground", "Equal-period diagonal passages admit two contrast readings. Reversed paint does not invert path direction or alter side identity.", reverse: true, cropped: true);

        var leaf = new Author();
        leaf.Curve([new(4, 5), new(12, 14), new(25, 17), new(38, 25)], [new(0, 0), new(-2, 3), new(-2, 3), new(0, 0)]);
        Add(leaf, "A leaf as a widened path", "The paired boundary collapses to pointed tips. A broad conceptual detail is allowed; detail need not mean a tiny mark.", decorate: true);

        var open = new Author();
        open.Curve([new(-8, 4), new(9, 2), new(31, 28), new(50, 26)], [new(0, 1), new(-1, 2), new(-1, 2), new(0, 1)], continued: true);
        Add(open, "Bounded sides, unseen ends", "Both ends explicitly continue beyond the observation. An SVG fill closes outside the clip only for paint; the native contour has no visible caps.", cropped: true, decorate: true);

        var paired = new Author();
        paired.Curve([new(4, 8), new(14, 8), new(28, 22), new(38, 22)], [new(0, 2), new(-1, 3), new(-1, 3), new(0, 2)]);
        Add(paired, "Given sides retain their construction axis", "This source already supplies side-profile information. The centre is retained correspondence, not a medial axis rediscovered from an outline.", origin: "Paired-boundary source with known correspondence");

        var zero = new Author(); zero.Line(new(4, 6), new(38, 24), new(0, 0));
        Add(zero, "A truly collapsed band", "Explicit zero side displacements are different from missing width. The occupied transverse construction collapses; it certifies no area.");

        var missing = new Author(); missing.Line(new(4, 6), new(38, 24), new(-1, 1), unknown: true);
        Add(missing, "Width left unknown", "An open offset word prevents a complete band read. The visible centreline remains valid and the renderer does not substitute zero width.");

        var asymmetric = new Author();
        asymmetric.Curve([new(3, 9), new(14, 3), new(29, 24), new(39, 19)], [new(0, 1), new(-1, 2), new(-1, 2), new(0, 1)],
            other: [new(0, -1), new(1, -1), new(1, -1), new(0, -1)]);
        Add(asymmetric, "Asymmetric lateral growth", "Two sides need not be equally distant from the selected spine. Here it is a construction axis, not a claim of exact centring.", decorate: true);

        var rosette = new Author();
        var axes = new[] { new V(0, 1), new V(-1, 1), new V(-1, 0), new V(-1, -1), new V(0, -1), new V(1, -1), new V(1, 0), new V(1, 1) };
        foreach (var v in axes)
        {
            var normal = new V(-v.Y, v.X);
            rosette.Curve([new V(21, 15) + v * 3, new V(21, 15) + v * 8 + normal * 2,
                new V(21, 15) + v * 11 + normal, new V(21, 15) + v * 13], [new(0, 0), normal, normal, new(0, 0)]);
        }
        Add(rosette, "Radial petals: paths and bounded leaves", "Eight cardinal/diagonal branch constructions use one shared reach. Widening turns their paths into leaf-like supports without changing the radial disposition; equal angular partitions are not certified.", decorate: true);
        return cases;
    }

    private sealed record SpanSpec(Relation Spine, Relation[] A, Relation[] B);
    private sealed record ThreadSpec(Relation Source, SpanSpec[] Spans, bool Closed, bool Continued, bool StartContinues);
    private sealed record CrossSpec(int First, int A, int Second, int B, Relation Word, int? Front);
    private sealed class Author
    {
        public Curve.Frame Frame { get; } = new(new QualitativeMotionFrame());
        private readonly List<ThreadSpec> threads = [];
        private readonly List<CrossSpec> crosses = [];
        public Relation Source => new(Frame.Surface, S(threads.Select(t => (IElement)t.Source).ToArray()));
        private Relation Word(V v) => Frame.Word(v.X, v.Y);
        public void Line(V a, V b, V offset, bool continued = false, bool unknown = false, IElement? from = null)
        {
            var spine = Frame.Line(from ?? new Placeholder(), new Placeholder(), Word(a), Word(b));
            var offsets = unknown ? new[] { StructuralConstraint.Open(), StructuralConstraint.Open() } : new[] { Word(offset), Word(offset) };
            Add([new(spine, offsets, [Word(-offset), Word(-offset)])], false, continued, from is not null);
        }
        public void Curve(V[] controls, V[] offsets, bool continued = false, V[]? other = null) =>
            Chain([(controls, offsets)], continued: continued, other: other is null ? null : [other]);
        public void Chain((V[] Controls, V[] Offsets)[] parts, bool closed = false, bool continued = false, V[][]? other = null)
        {
            var first = new Placeholder(); IElement at = first; var spans = new List<SpanSpec>();
            for (var i = 0; i < parts.Length; i++)
            {
                var p = parts[i]; var end = closed && i == parts.Length - 1 ? first : new Placeholder();
                var words = p.Controls.Select(Word).ToArray();
                var spine = words.Length == 2 ? Frame.Line(at, end, words[0], words[1]) :
                    words.Length == 3 ? Frame.Quadratic(at, end, words[0], words[1], words[2]) : Frame.Cubic(at, end, words[0], words[1], words[2], words[3]);
                spans.Add(new(spine, p.Offsets.Select(Word).ToArray(), (other?[i] ?? p.Offsets.Select(v => -v).ToArray()).Select(Word).ToArray())); at = end;
            }
            Add(spans.ToArray(), closed, continued);
        }
        public void Polyline(V[] vertices, bool closed = false, bool continued = false)
        {
            V Normal(V a, V b) => new(-Math.Sign(b.Y - a.Y), Math.Sign(b.X - a.X));
            var offsets = vertices.Select((p, i) => {
                if (!closed && i == 0) return Normal(p, vertices[1]);
                if (!closed && i == vertices.Length - 1) return Normal(vertices[i - 1], p);
                var a = Normal(vertices[(i + vertices.Length - 1) % vertices.Length], p);
                var b = Normal(p, vertices[(i + 1) % vertices.Length]);
                return a == b ? a : a + b;
            }).ToArray();
            Chain(Enumerable.Range(0, closed ? vertices.Length : vertices.Length - 1).Select(i =>
                (new[] { vertices[i], vertices[(i + 1) % vertices.Length] }, new[] { offsets[i], offsets[(i + 1) % vertices.Length] })).ToArray(), closed, continued);
        }
        private void Add(SpanSpec[] spans, bool closed, bool continued, bool startContinues = false)
        {
            var source = new Relation(new Placeholder(), S(spans.Select(span => (IElement)new Relation(span.Spine,
                new Relation(S(span.A), S(span.B)))).ToArray()));
            threads.Add(new(source, spans, closed, continued, startContinues));
        }
        public void Cross(int first, int a, int second, int b, V point, int? front = null) => crosses.Add(new(first, a, second, b, Word(point), front));
        public Layer Build(int scale)
        {
            var built = new List<Thread>(); var constructions = new List<IElement>();
            Relation Scale(Relation word)
            {
                if (!Closed(word)) return word;
                var entries = E(word); var repeated = S(Enumerable.Range(0, scale).SelectMany(_ => entries).ToArray());
                constructions.Add(new Relation(word, new Relation(S(Enumerable.Repeat<IElement>(word, scale).ToArray()), repeated)));
                return repeated;
            }
            foreach (var thread in threads)
            {
                var aSites = Enumerable.Range(0, thread.Spans.Length + 1).Select(_ => (IElement)new Placeholder()).ToArray();
                var bSites = Enumerable.Range(0, thread.Spans.Length + 1).Select(_ => (IElement)new Placeholder()).ToArray();
                if (thread.Closed) { aSites[^1] = aSites[0]; bSites[^1] = bSites[0]; }
                var sweeps = thread.Spans.Select((span, i) => Band.Sweep(Frame, span.Spine, span.A.Select(Scale).ToArray(), span.B.Select(Scale).ToArray(),
                    aSites[i], aSites[i + 1], bSites[i], bSites[i + 1])).ToArray();
                var envelopes = sweeps.Select((sweep, i) => {
                    var read = Band.Read(Frame, sweep);
                    var start = i == 0 && !thread.Closed && !thread.Continued && !thread.StartContinues ? Band.Cap(Frame, read, true) : Band.Continue(Frame, read, true, thread.Source);
                    var end = i == sweeps.Length - 1 && !thread.Closed && !thread.Continued ? Band.Cap(Frame, read, false) : Band.Continue(Frame, read, false, thread.Source);
                    return Band.BandEnvelope(sweep, start, end, Band.Occupy(read));
                }).ToArray();
                built.Add(new(thread.Source, new Relation(thread.Source, S(envelopes.Cast<IElement>().ToArray()))));
            }
            var crossings = crosses.Select(c => Band.Crossing(Frame, threads[c.First].Spans[c.A].Spine, threads[c.Second].Spans[c.B].Spine, c.Word)).ToArray();
            var orders = crosses.Select((c, i) => c.Front is null ? StructuralConstraint.Open(crossings[i].Surface) :
                Band.Over(crossings[i], threads[c.Front.Value].Spans[c.Front == c.First ? c.A : c.B].Spine,
                    threads[c.Front == c.First ? c.Second : c.First].Spans[c.Front == c.First ? c.B : c.A].Spine)).ToArray();
            var root = new Relation(new Relation(Frame.Surface, S(built.Select(t => (IElement)t.Surface).ToArray())),
                new Relation(new Relation(S(crossings.Select(c => (IElement)c.Surface).ToArray()), S(orders)), S(constructions.ToArray())));
            return new(Frame, built, crossings.Select(c => c.Surface).ToArray(), orders, root);
        }
    }
}
