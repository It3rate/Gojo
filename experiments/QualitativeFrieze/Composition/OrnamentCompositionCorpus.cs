using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;
using static Gojo.Experiments.QualitativeFrieze.Composition.JointNative;
using Band = Gojo.Experiments.QualitativeExecution.QualitativeBand;
using Curve = Gojo.Experiments.QualitativeExecution.QualitativeCurve;
using Parts = Gojo.Experiments.QualitativeFrieze.Composition.OrnamentBandComposition;

namespace Gojo.Experiments.QualitativeFrieze.Composition;

/// <summary>Authored ornamental vocabulary compiled to reusable native band
/// templates. Whole compounds, not SVG groups, are mirrored and repeated.</summary>
public static class OrnamentCompositionCorpus
{
    public readonly record struct V(int X, int Y)
    {
        public static V operator +(V a, V b) => new(a.X + b.X, a.Y + b.Y);
        public static V operator -(V a) => new(-a.X, -a.Y);
        public static V operator *(V a, int b) => new(a.X * b, a.Y * b);
    }
    public sealed record Crossing(Relation A, Relation B, Relation Locus, Relation Front, Relation Surface);
    public sealed record Unit(Parts.Template Template, Relation[] Roles, IReadOnlyList<Crossing> Crossings,
        IReadOnlyList<Relation> Applications, IReadOnlyList<Relation> Repetitions, IReadOnlyList<Relation> Seams,
        Relation Highlight, Relation? Entry, Relation? Exit, Relation Surface);
    public sealed record Design(string Id, string Name, string Note, string Repertoire, Unit Unit, int Width, int Height,
        string Construction, string Limit);

    public static IReadOnlyList<Design> Cases(int limit = int.MaxValue)
    {
        var output = new List<Design>();
        void Add(string name, string note, string repertoire, Func<Curve.Frame, Unit> factory,
            int columns = 3, int rows = 2, int pitchX = 30, int pitchY = 30, bool alternate = false, bool drop = false, bool alternateWeave = false)
        {
            if (output.Count >= limit) return;
            var f = new Curve.Frame(new QualitativeMotionFrame()); var authored = factory(f);
            var tile = Make(authored.Template, authored.Roles, authored.Crossings, authored.Applications, authored.Repetitions,
                authored.Seams, S(authored.Template.Envelopes.Cast<IElement>().ToArray()), authored.Entry, authored.Exit, authored.Surface);
            var at = Place(tile, Parts.Identity(f), new(pitchX / 2, pitchY / 2));
            Unit row;
            // Reflection is about the current frame origin. Alternating a row
            // therefore needs explicit translated placements, rather than a
            // false fixed-pitch recurrence through already translated points.
            if (alternate)
            {
                var plan = Enumerable.Range(0, columns).Select(i => Parts.Plan(i % 2 == 0 ? Parts.Identity(f) : MirrorX(f),
                    f.Word(pitchX / 2 + i * pitchX, pitchY / 2))).ToArray();
                row = Arrange(tile, plan);
            }
            else row = Repeat(at, Parts.Identity(f), new(pitchX, 0), columns);
            Unit field;
            if (drop)
            {
                var plan = Enumerable.Range(0, rows).Select(j => Parts.Plan(Parts.Identity(f),
                    f.Word(j % 2 == 0 ? 0 : pitchX / 2, j * pitchY))).ToArray();
                field = Arrange(row, plan);
            }
            else field = Repeat(row, Parts.Identity(f), new(0, pitchY), rows);
            if (alternateWeave) field = AlternateOrders(field, columns);
            output.Add(new("O" + (output.Count + 1).ToString("000"), name, note, repertoire, field,
                columns * pitchX + (drop ? pitchX / 2 : 0), rows * pitchY,
                $"{columns} column placements × {rows} row placements; " + (alternate ? "alternate reflected compound units" : "native output-to-input translation recurrence") +
                (drop ? "; alternate half-drop rows" : ""),
                "Authored repertoire adaptation; no facsimile, global no-overlap proof or learned aesthetic verdict."));
        }

        Add("Mirrored flowering vine", "A two-part curved stem carries broad leaves and tiny spine veins. The entire sprig is mirrored between columns.",
            "Jones conventional foliage; previous vine-band experiment", f => {
                var a = new Author(f); a.FrameBox(30, 30);
                a.Chain(1, [([new V(0, -15), new V(-9, -7), new V(9, 7), new V(0, 15)], [new V(-1, 0), new V(-1, 1), new V(-1, 1), new V(-1, 0)])], continued: true, ports: true);
                var stem = a.Finish(); var leaf = Leaf(f, 10);
                var left = Place(leaf, Parts.Map(f, f.Word(0, 1), f.Word(0, -1), f.Word(-1, 1), f.Word(1, -1)), new(0, 0));
                var right = Place(leaf, Parts.Map(f, f.Word(0, 1), f.Word(0, -1), f.Word(1, 1), f.Word(-1, -1)), new(1, 0));
                return Merge(stem, left, right, Place(Flower(f, 3), Parts.Identity(f), new(0, 11)));
            }, alternate: true);

        Add("Ogee damask", "Mirrored curved half-arches define an ogee compartment, enclosing a separate radial flower and paired leaves.",
            "Moresque diaper and ogival foliage", f => {
                var a = new Author(f); a.FrameBox(32, 32);
                a.Chain(1, [([new V(0, -16), new V(-13, -9), new V(-13, 7), new V(0, 16)], [new V(-1, 0), new V(-1, 0), new V(-1, 0), new V(-1, 0)])], continued: true);
                var half = a.Finish(); var other = Place(half, MirrorX(f), new(0, 0));
                return Merge(half, other, Flower(f, 7));
            }, pitchX: 32, pitchY: 32, columns: 2, rows: 2, drop: true);

        Add("Lotus on a lozenge scaffold", "One pointed leaf and its mirror become a lotus. Its complete compound repeats over a diamond construction frame.",
            "Conventional lotus and diamond diaper", f => {
                var a = new Author(f); a.Path(0, [new(0, -15), new(15, 0), new(0, 15), new(-15, 0)], 0, closed: true);
                a.Chain(1, [([new V(-9, -6), new V(-7, -15), new V(7, -15), new V(9, -6)], [new V(-1, 0), new V(0, -1), new V(0, -1), new V(1, 0)])]);
                var side = Place(Leaf(f, 10), Map(f, new(1, 0), new(1, 1)), new(-2, -6));
                return Merge(a.Finish(), Place(Leaf(f, 13), Parts.Identity(f), new(0, -7)), side, Place(side, MirrorX(f), new(0, 0)));
            }, drop: true);

        Add("Quatrefoil and pearl diaper", "Four real quarter-turn applications form a four-lobed ribbon arrangement; small flowers occupy the intermediate compartments. Coincident copies remain separate sites.",
            "Fourfold Gothic and Moresque compartment grammar", f => {
                var a = new Author(f); a.FrameBox(30, 30);
                a.Chain(1, [([new V(6, 6), new V(6, 18), new V(-6, 18), new V(-6, 6)], [new V(-1, -1), new V(-1, 1), new V(1, 1), new V(1, -1)])], continued: true, ports: true);
                var orbit = Repeat(a.Finish(), Quarter(f), new(0, 0), 4);
                // The parent exposes the compound, not a through-route port
                // connecting one medallion to its next neighbour.
                orbit = Make(orbit.Template, orbit.Roles, orbit.Crossings, orbit.Applications, orbit.Repetitions,
                    orbit.Seams, orbit.Highlight, null, null, orbit.Surface);
                return Merge(orbit, Flower(f, 5));
            }, columns: 2, rows: 2);

        Add("Palmettes and opposed fans", "A compound fan of broad curved leaves is reflected as a unit, over a quiet square course.",
            "Greek palmette and anthemion border", f => {
                var a = new Author(f); a.FrameBox(30, 30); a.Path(1, [new(-11, -9), new(11, -9)], 1);
                var leaves = new List<Unit> { a.Finish(), Place(Leaf(f, 17), Parts.Identity(f), new(0, -9)) };
                foreach (var x in new[] { -1, 1 }) foreach (var y in new[] { 1, 2 })
                    leaves.Add(Place(Leaf(f, 6), Map(f, new(y, -x), new(x * 2, y)), new(0, -8)));
                return Merge(leaves.ToArray());
            }, alternate: true);

        Add("Octagonal flower compartments", "Chamfered frame corners, curved leaves and a fourfold centre share one compound unit. Small interstitial squares belong to the disposition.",
            "B06.B octagon/square skeleton", f => {
                var a = new Author(f); a.Path(1, [new(-7, -13), new(7, -13), new(13, -7), new(13, 7), new(7, 13), new(-7, 13), new(-13, 7), new(-13, -7)], 1, closed: true);
                a.FrameBox(30, 30); return Merge(a.Finish(), Flower(f, 10));
            });

        Add("Scalloped leaf courses", "A rounded arch and three broad leaves form a shell-like unit. Alternating row drop changes the larger grouping.",
            "B28/B30 shell and cloud-scale courses", f => {
                var a = new Author(f); a.FrameBox(30, 24);
                a.Chain(1, [([new V(-15, -8), new V(-15, 13), new V(15, 13), new V(15, -8)], [new V(-1, 0), new V(-1, 1), new V(1, 1), new V(1, 0)])], continued: true);
                var u = a.Finish(); var leaf = Place(Leaf(f, 11), Parts.Identity(f), new(0, -7));
                return Merge(u, leaf, Place(Leaf(f, 6), Map(f, new(1, 1), new(-1, 1)), new(0, -7)), Place(Leaf(f, 6), Map(f, new(1, -1), new(1, 1)), new(0, -7)));
            }, pitchY: 24, drop: true);

        Add("Fret ribbons and interior leaves", "Angular meanders remain actual occupied bands. Curved leaves occupy the compartment rather than being scattered along the line.",
            "B09 detached S/fret vocabulary", f => {
                var a = new Author(f); a.FrameBox(30, 30);
                a.Path(1, [new(-15, -12), new(12, -12), new(12, 12), new(-7, 12), new(-7, -3), new(4, -3), new(4, 5)], 1, continued: true);
                return Merge(a.Finish(), Place(Leaf(f, 8), Map(f, new(0, -1), new(1, 0)), new(-5, 5)));
            }, alternate: true);

        Add("Basket and four-petal accents", "Independent continuous ribbons carry explicit straight-crossing order. The flower is a sibling unit anchored to that same repeating frame.",
            "B02 basket and W012 weave", f => {
                var a = new Author(f); a.FrameBox(30, 30);
                var h = a.Path(1, [new(-15, 0), new(15, 0)], 2, continued: true);
                var v = a.Path(2, [new(0, -15), new(0, 15)], 2, continued: true);
                a.Cross(h[0], v[0], new(0, 0), h[0]);
                return Merge(a.Finish(), Place(Flower(f, 5), Parts.Identity(f), new(8, 8)));
            }, alternateWeave: true);

        Add("Diagonal vine and star lattice", "A diamond ribbon and a curled sprig are repeated together. The broad leaves follow the diagonal frame instead of obscuring its disposition.",
            "B08 intersecting ribbons, stylised foliate diaper", f => {
                var a = new Author(f); a.Path(0, [new(0, -15), new(15, 0), new(0, 15), new(-15, 0)], 0, closed: true);
                a.Path(1, [new(-15, 0), new(0, 15), new(15, 0), new(0, -15)], 1, closed: true);
                return Merge(a.Finish(), Flower(f, 9));
            }, columns: 2, rows: 2, drop: true);

        Add("Pomegranate and mirrored tendrils", "A large pointed body is a tapered band with explicit occupied support. Paired curved tendrils and a small crown articulate its interior.",
            "Conventional pomegranate and damask repertoire", f => {
                var a = new Author(f); a.FrameBox(32, 36);
                a.Chain(2, [([new V(0, -13), new V(0, -7), new V(0, 3), new V(0, 10)], [new V(0, 0), new V(-10, 0), new V(-11, 0), new V(0, 0)])]);
                a.Path(3, [new(-3, 10), new(-3, 13), new(0, 12), new(3, 13), new(3, 10)], 0);
                var b = new Author(f); b.Chain(1, [([new V(0, -13), new V(-15, -7), new V(-16, 9), new V(-5, 12)], [new V(-1, 0), new V(-1, 1), new V(-1, 1), new V(0, 1)])]);
                var half = b.Finish(); return Merge(a.Finish(), half, Place(half, MirrorX(f), new(0, 0)), Place(Flower(f, 4), Parts.Identity(f), new(0, -1)));
            }, columns: 2, rows: 2, pitchX: 32, pitchY: 36, alternate: true);

        Add("Braided diagonal passages", "The diagonal crossing is certified on straight source sections; curved buds decorate its compartments without supplying the crossing evidence.",
            "Interlaced diagonal bands with foliate infill", f => {
                var a = new Author(f); a.FrameBox(30, 30);
                var p = a.Path(1, [new(-15, -15), new(15, 15)], 1, continued: true);
                var q = a.Path(2, [new(-15, 15), new(15, -15)], 1, continued: true);
                a.Cross(p[0], q[0], new(0, 0), p[0]);
                return Merge(a.Finish(), Place(Leaf(f, 7), Parts.Identity(f), new(0, 5)), Place(Leaf(f, 7), Half(f), new(0, -5)));
            }, columns: 2, rows: 2, alternate: true);

        Add("Eight-leaf medallion lattice", "Two distinct petal constructions form a wedge; the entire wedge repeats by quarter turns before the medallion repeats on the field.",
            "Radial floral medallions and geometric diaper", f => {
                var a = new Author(f); a.FrameBox(32, 32);
                var diag = Place(Leaf(f, 7), Map(f, new(1, -1), new(1, 1)), new(0, 0));
                return Merge(a.Finish(), Repeat(Merge(Leaf(f, 13), diag), Quarter(f), new(0, 0), 4), Flower(f, 3));
            }, columns: 2, rows: 2, pitchX: 32, pitchY: 32);

        Add("Herringbone fronds", "A large two-leaf sprig is mirrored and shifted as a compound. Its diagonal limbs share a narrow parent stem.",
            "Herringbone and conventional branching foliage", f => {
                var a = new Author(f); a.FrameBox(26, 28); a.Path(1, [new(0, -14), new(0, 14)], 1, continued: true);
                var half = Place(Leaf(f, 7), Map(f, new(0, 1), new(1, 1)), new(0, -5));
                return Merge(a.Finish(), half, Place(half, MirrorX(f), new(0, 0)));
            }, pitchX: 26, pitchY: 28, alternate: true, drop: true);

        Add("Cloud arches with pendant drops", "A lobed arch is a compound of three curved segments. Drop repeats create a second reading of the same course.",
            "B30 faceted cloud-scale arches, now cubic", f => {
                var a = new Author(f); a.FrameBox(30, 22);
                a.Chain(1, [
                    ([new V(-15, -5), new V(-15, 5), new V(-10, 8), new V(-7, 4)], [new V(-1, 0), new V(-1, 1), new V(0, 1), new V(0, 1)]),
                    ([new V(-7, 4), new V(-4, 13), new V(4, 13), new V(7, 4)], [new V(0, 1), new V(-1, 1), new V(1, 1), new V(0, 1)]),
                    ([new V(7, 4), new V(10, 8), new V(15, 5), new V(15, -5)], [new V(0, 1), new V(0, 1), new V(1, 1), new V(1, 0)])], continued: true);
                return Merge(a.Finish(), Place(Leaf(f, 8), Half(f), new(0, 3)));
            }, pitchY: 22, drop: true);

        Add("Paired S-scroll compartments", "An S-scroll and its reflected partner make one reusable compartment. A fourfold flower and narrow inner veins complete the intermediate layer.",
            "B31 outward arms reinterpreted as continuous curves", f => {
                var a = new Author(f); a.FrameBox(32, 32);
                a.Chain(1, [([new V(-16, -12), new V(13, -20), new V(-13, 20), new V(16, 12)], [new V(0, 1), new V(1, 1), new V(1, 1), new V(0, 1)])], continued: true);
                var half = a.Finish(); return Merge(half, Place(half, MirrorX(f), new(0, 0)), Flower(f, 6));
            }, columns: 2, rows: 2, pitchX: 32, pitchY: 32);
        return output;
    }

    public static QualitativePatternTransform Map(Curve.Frame f, V right, V up) => Parts.Map(f,
        f.Word(right.X, right.Y), f.Word(-right.X, -right.Y), f.Word(up.X, up.Y), f.Word(-up.X, -up.Y));
    public static QualitativePatternTransform Quarter(Curve.Frame f) => Parts.Map(f, f.Word(0, 1), f.Word(0, -1),
        f.Word(-1, 0), f.Word(1, 0), CurveShapeQualities.SimilarityBasis(f));
    public static QualitativePatternTransform Half(Curve.Frame f) => Map(f, new(-1, 0), new(0, -1));
    public static QualitativePatternTransform MirrorX(Curve.Frame f) => Map(f, new(-1, 0), new(0, 1));

    private static Unit Leaf(Curve.Frame f, int length)
    {
        var a = new Author(f);
        a.Chain(2, [([new V(0, 0), new V(2, length / 3), new V(2, 2 * length / 3), new V(0, length)],
            [new V(0, 0), new V(-2, 0), new V(-2, 0), new V(0, 0)])]);
        a.Chain(3, [([new V(0, 0), new V(2, length / 3), new V(2, 2 * length / 3), new V(0, length - 1)],
            [new V(0, 0), new V(0, 0), new V(0, 0), new V(0, 0)])]);
        return a.Finish();
    }
    private static Unit Flower(Curve.Frame f, int reach)
    {
        var a = new Author(f); var width = reach < 6 ? 1 : 2;
        a.Chain(2, [([new V(0, 0), new V(0, reach / 3), new V(0, 2 * reach / 3), new V(0, reach)],
            [new V(0, 0), new V(-width, 0), new V(-width, 0), new V(0, 0)])]);
        a.Path(3, [new(0, 0), new(0, reach - 1)], 0);
        return Repeat(a.Finish(), Quarter(f), new(0, 0), 4);
    }

    public static Unit Place(Unit source, QualitativePatternTransform transform, V shift) => MapApplication(source,
        Parts.Place(source.Template, transform, source.Template.Frame.Word(shift.X, shift.Y)));
    private static Unit MapApplication(Unit source, Parts.Application application)
    {
        Relation Image(Relation original) => (Relation)application.EnvelopePairs.Single(p => ReferenceEquals(p.Left, original)).Right;
        var roles = source.Roles.Select(r => S(E(r).Cast<Relation>().Select(e => (IElement)Image(e)).ToArray())).ToArray();
        var proofs = new List<IElement>();
        var seams = source.Seams.Select(seam => {
            if (seam.Left is not Relation { Left: Relation incoming, Right: Relation outgoing }) return StructuralConstraint.Open(seam);
            var a = application.EnvelopePairs.FirstOrDefault(pair => ReferenceEquals(pair.Left, incoming));
            var b = application.EnvelopePairs.FirstOrDefault(pair => ReferenceEquals(pair.Left, outgoing));
            if (a?.Right is not Relation aa || b?.Right is not Relation bb) return StructuralConstraint.Open(seam);
            var current = Parts.Seam(source.Template.Frame, aa, bb).Surface;
            proofs.Add(new Relation(seam, current)); return current;
        }).ToArray();
        var crosses = source.Crossings.Select(c => {
            var action = application.Transform.Apply(c.Locus); application.Transform.TryReadComplete(action, out var mapped);
            var sum = source.Template.Frame.Motion.Reduce(mapped, application.Translation);
            source.Template.Frame.Motion.TryReadResult(sum, out var word, out _, out _);
            var a = Image(c.A); var b = Image(c.B); var front = Image(c.Front);
            var ar = Band.ReadEnvelope(source.Template.Frame, a); var br = Band.ReadEnvelope(source.Template.Frame, b);
            var crossing = Band.Crossing(source.Template.Frame, ar.Sweep.Spine.Section, br.Sweep.Spine.Section, word);
            var order = Band.Over(crossing, ReferenceEquals(front, a) ? ar.Sweep.Spine.Section : br.Sweep.Spine.Section,
                ReferenceEquals(front, a) ? br.Sweep.Spine.Section : ar.Sweep.Spine.Section);
            var surface = new Relation(c.Surface, new Relation(new Relation(action, sum), order)); proofs.Add(surface);
            return new Crossing(a, b, word, front, order);
        }).ToArray();
        return Make(application.Output, roles, crosses, source.Applications.Append(application.Surface).ToArray(), source.Repetitions,
            seams, S(E(source.Highlight).Cast<Relation>().Select(e => (IElement)Image(e)).ToArray()),
            source.Entry is null ? null : Image(source.Entry), source.Exit is null ? null : Image(source.Exit),
            new Relation(source.Surface, new Relation(application.Surface, S(proofs.ToArray()))));
    }
    private static Unit Repeat(Unit seed, QualitativePatternTransform map, V shift, int count)
    {
        var run = Parts.Repeat(seed.Template, map, seed.Template.Frame.Word(shift.X, shift.Y), count);
        var current = seed; var units = new List<Unit>(); var seams = new List<Relation>();
        foreach (var application in run.Applications)
        {
            var next = MapApplication(current, application);
            if (units.Count > 0 && units[^1].Exit is not null && next.Entry is not null)
                seams.Add(Parts.Seam(seed.Template.Frame, units[^1].Exit!, next.Entry).Surface);
            units.Add(next); current = next;
        }
        var merged = Merge(units.ToArray());
        return Make(run.Output, merged.Roles, merged.Crossings, merged.Applications,
            merged.Repetitions.Append(run.Surface).Distinct<Relation>(ReferenceEqualityComparer.Instance).ToArray(),
            merged.Seams.Concat(seams).ToArray(), units[0].Highlight, units[0].Entry, units[^1].Exit, new Relation(merged.Surface, run.Surface));
    }
    private static Unit Merge(params Unit[] children)
    {
        var frame = children[0].Template.Frame; var template = Parts.Compose(frame, children.Select(c => c.Template).ToArray());
        return Make(template, Enumerable.Range(0, 4).Select(r => S(children.SelectMany(c => E(c.Roles[r])).ToArray())).ToArray(),
            children.SelectMany(c => c.Crossings).ToArray(), children.SelectMany(c => c.Applications).Distinct<Relation>(ReferenceEqualityComparer.Instance).ToArray(),
            children.SelectMany(c => c.Repetitions).Distinct<Relation>(ReferenceEqualityComparer.Instance).ToArray(),
            children.SelectMany(c => c.Seams).ToArray(), children[0].Highlight, children[0].Entry, children[0].Exit,
            S(children.Select(c => (IElement)c.Surface).ToArray()));
    }
    private static Unit Arrange(Unit source, params Parts.PlacementInstruction[] instructions)
    {
        var plan = Parts.Arrange(source.Template, instructions);
        var units = plan.Applications.Select(app => MapApplication(source, app)).ToArray();
        var merged = Merge(units);
        return Make(plan.Output, merged.Roles, merged.Crossings, merged.Applications,
            merged.Repetitions.Append(plan.Surface).Distinct<Relation>(ReferenceEqualityComparer.Instance).ToArray(),
            merged.Seams, units[0].Highlight, merged.Entry, merged.Exit, new Relation(merged.Surface, plan.Surface));
    }
    private static Unit AlternateOrders(Unit source, int columns)
    {
        var first = new Placeholder(); var second = new Placeholder();
        var cycle = new Relation(S(first, second), S(new Relation(first, second), new Relation(second, first)));
        var columnPhases = Enumerable.Range(0, columns).Select(i => (IElement)(i % 2 == 0 ? first : second)).ToArray();
        var rowPhases = Enumerable.Range(0, (source.Crossings.Count + columns - 1) / columns)
            .Select(i => (IElement)(i % 2 == 0 ? first : second)).ToArray();
        var controller = new Relation(cycle, new Relation(S(columnPhases), S(rowPhases)));
        var witnesses = new List<IElement>(); var frame = source.Template.Frame;
        var crossings = source.Crossings.Select((c, i) => {
            var opposed = !ReferenceEquals(columnPhases[i % columns], rowPhases[i / columns]);
            var front = opposed ? ReferenceEquals(c.Front, c.A) ? c.B : c.A : c.Front;
            var a = Band.ReadEnvelope(frame, c.A).Sweep.Spine.Section; var b = Band.ReadEnvelope(frame, c.B).Sweep.Spine.Section;
            var order = Band.Over(Band.Crossing(frame, a, b, c.Locus), ReferenceEquals(front, c.A) ? a : b, ReferenceEquals(front, c.A) ? b : a);
            witnesses.Add(new Relation(new Relation(c.Surface, new Relation(columnPhases[i % columns], rowPhases[i / columns])), order));
            return c with { Front = front, Surface = order };
        }).ToArray();
        return Make(source.Template, source.Roles, crossings, source.Applications, source.Repetitions, source.Seams,
            source.Highlight, source.Entry, source.Exit, new Relation(source.Surface, new Relation(controller, S(witnesses.ToArray()))));
    }
    private static Unit Make(Parts.Template template, Relation[] roles, IReadOnlyList<Crossing> crosses,
        IReadOnlyList<Relation> applications, IReadOnlyList<Relation> repeats, IReadOnlyList<Relation> seams,
        Relation highlight, Relation? entry, Relation? exit, IElement provenance)
    {
        Relation Port(Relation? envelope, bool start)
        {
            if (envelope is null) return StructuralConstraint.Open();
            var read = Band.ReadEnvelope(template.Frame, envelope).Sweep;
            return S(new Relation(envelope, new Relation(start ? read.Spine.Start : read.Spine.End,
                new Relation(start ? read.SideA.Start : read.SideA.End, start ? read.SideB.Start : read.SideB.End))));
        }
        var root = new Relation(template.Surface, new Relation(S(roles), new Relation(S(crosses.Select(c => (IElement)c.Surface).ToArray()),
            new Relation(new Relation(S(applications.Cast<IElement>().ToArray()), S(repeats.Cast<IElement>().ToArray())),
                new Relation(new Relation(S(seams.Cast<IElement>().ToArray()), highlight),
                    new Relation(new Relation(Port(entry, true), Port(exit, false)), provenance))))));
        return new(template, roles, crosses, applications, repeats, seams, highlight, entry, exit, root);
    }

    private sealed class Author(Curve.Frame frame)
    {
        private readonly List<Relation> threads = [];
        private readonly List<Relation>[] roles = [[], [], [], []];
        private readonly List<Crossing> crosses = [];
        private Relation? entry, exit;
        public void FrameBox(int width, int height) => Path(0, [new(-width / 2, -height / 2), new(width / 2, -height / 2), new(width / 2, height / 2), new(-width / 2, height / 2)], 0, closed: true);
        public Relation[] Path(int role, V[] points, int width, bool closed = false, bool continued = false)
        {
            V Normal(V a, V b) => new(-Math.Sign(b.Y - a.Y), Math.Sign(b.X - a.X));
            var offsets = points.Select((p, i) => {
                if (!closed && i == 0) return Normal(p, points[1]) * width;
                if (!closed && i == points.Length - 1) return Normal(points[i - 1], p) * width;
                var a = Normal(points[(i + points.Length - 1) % points.Length], p); var b = Normal(p, points[(i + 1) % points.Length]);
                return (a == b ? a : a + b) * width;
            }).ToArray();
            return Chain(role, Enumerable.Range(0, closed ? points.Length : points.Length - 1).Select(i =>
                (new[] { points[i], points[(i + 1) % points.Length] }, new[] { offsets[i], offsets[(i + 1) % points.Length] })).ToArray(), closed, continued);
        }
        public Relation[] Chain(int role, (V[] Controls, V[] Offsets)[] parts, bool closed = false, bool continued = false, bool ports = false)
        {
            var sites = Enumerable.Range(0, parts.Length + 1).Select(_ => (IElement)new Placeholder()).ToArray();
            var sideA = Enumerable.Range(0, parts.Length + 1).Select(_ => (IElement)new Placeholder()).ToArray();
            var sideB = Enumerable.Range(0, parts.Length + 1).Select(_ => (IElement)new Placeholder()).ToArray();
            if (closed) { sites[^1] = sites[0]; sideA[^1] = sideA[0]; sideB[^1] = sideB[0]; }
            Relation Word(V v) => frame.Word(v.X, v.Y);
            var output = new List<Relation>();
            foreach (var (part, i) in parts.Select((part, i) => (part, i)))
            {
                var c = part.Controls.Select(Word).ToArray();
                var spine = c.Length == 2 ? frame.Line(sites[i], sites[i + 1], c[0], c[1]) : c.Length == 3 ? frame.Quadratic(sites[i], sites[i + 1], c[0], c[1], c[2]) : frame.Cubic(sites[i], sites[i + 1], c[0], c[1], c[2], c[3]);
                var sweep = Band.Sweep(frame, spine, part.Offsets.Select(Word).ToArray(), part.Offsets.Select(v => Word(-v)).ToArray(), sideA[i], sideA[i + 1], sideB[i], sideB[i + 1]);
                var read = Band.Read(frame, sweep);
                var start = i == 0 && !continued && !closed ? Band.Cap(frame, read, true) : Band.Continue(frame, read, true);
                var end = i == parts.Length - 1 && !continued && !closed ? Band.Cap(frame, read, false) : Band.Continue(frame, read, false);
                var env = Band.BandEnvelope(sweep, start, end, role == 0 || role == 3 ? null : Band.Occupy(read));
                output.Add(env); roles[role].Add(env);
            }
            threads.Add(S(output.Cast<IElement>().ToArray())); if (ports) { entry = output[0]; exit = output[^1]; }
            return output.ToArray();
        }
        public void Cross(Relation a, Relation b, V locus, Relation front)
        {
            var ar = Band.ReadEnvelope(frame, a); var br = Band.ReadEnvelope(frame, b);
            var word = frame.Word(locus.X, locus.Y);
            var crossing = Band.Crossing(frame, ar.Sweep.Spine.Section, br.Sweep.Spine.Section, word);
            var order = Band.Over(crossing, ReferenceEquals(front, a) ? ar.Sweep.Spine.Section : br.Sweep.Spine.Section,
                ReferenceEquals(front, a) ? br.Sweep.Spine.Section : ar.Sweep.Spine.Section);
            crosses.Add(new(a, b, word, front, order));
        }
        public Unit Finish()
        {
            var template = Parts.Create(frame, threads.ToArray());
            return Make(template, roles.Select(r => S(r.Cast<IElement>().ToArray())).ToArray(), crosses, [], [], [],
                S(template.Envelopes.Cast<IElement>().ToArray()), entry, exit, S());
        }
    }
}
