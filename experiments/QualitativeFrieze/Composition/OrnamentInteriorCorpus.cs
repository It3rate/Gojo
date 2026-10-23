using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;
using static Gojo.Experiments.QualitativeFrieze.Composition.JointNative;
using Band = Gojo.Experiments.QualitativeExecution.QualitativeBand;
using Curve = Gojo.Experiments.QualitativeExecution.QualitativeCurve;
using Parts = Gojo.Experiments.QualitativeFrieze.Composition.OrnamentBandComposition;
using Vocabulary = Gojo.Experiments.QualitativeFrieze.Composition.OrnamentCompositionCorpus;
using Previous = Gojo.Experiments.QualitativeFrieze.Composition.OrnamentIntegratedCorpus;
using Interior = Gojo.Experiments.QualitativeFrieze.Composition.OrnamentInteriorFrame;
using Attach = Gojo.Experiments.QualitativeFrieze.Composition.OrnamentMotifAttachment;
using V = Gojo.Experiments.QualitativeFrieze.Composition.OrnamentCompositionCorpus.V;

namespace Gojo.Experiments.QualitativeFrieze.Composition;

/// <summary>Compound ornament constructed within selected mesh compartments.
/// Local profiles, guide curves, native placement receipts and the selected
/// boundary/port evidence remain connected. Recipes are authored; neither a
/// general face finder nor a metric containment solver is implied.</summary>
public static class OrnamentInteriorCorpus
{
    // interior application = (interior read -> (anchor selection -> anchor word)) ->
    //                        (local compound -> (actual placement -> guide placement))
    // design = previous -> ((preserved mesh -> carried decoration) ->
    //          (output -> (Closed(interior applications) -> selected colour focus)))
    public sealed record Placement(Interior.Read Interior, Relation Anchor, Relation AnchorRead,
        Parts.Application Application, Parts.Application Guides, Relation[] Roles, Relation? BoundaryBranch, Relation Surface);
    public sealed record Design(string Id, string Name, string Note, Previous.Design Previous,
        Vocabulary.Unit Mesh, Vocabulary.Unit Unit, IReadOnlyList<Placement> Placements,
        Parts.Template Guides, bool GroundFocus, Relation Recessed, Relation Surface);

    public static IReadOnlyList<Design> Cases(int limit = int.MaxValue, string? caseId = null) =>
        Previous.Cases(caseId is null ? limit : int.MaxValue, caseId is null ? null : "I" + caseId[1..])
            .Select(Build).Take(limit).ToArray();

    public static Design Build(Previous.Design previous)
    {
        var b = new Builder(previous); var s = previous.Source; var f = previous.Unit.Template.Frame;
        var cols = s.Lattice.ColumnPhases.Count; var rows = s.Lattice.RowPhases.Count;
        var cellAndProfiles = (Relation)((Relation)((Relation)((Relation)s.Surface.Right).Right).Right).Right;
        var cells = E((Relation)cellAndProfiles.Left).Cast<Relation>().ToArray();
        var number = int.Parse(s.Id.AsSpan(1));
        var names = new[] { "Quatrefoil brocade", "Twill pomegranate chambers", "Diamond compass flowers",
            "Trefoil triangular compartments", "Wave-borne palmettes", "Ogee fleurons",
            "Ribbed scallop fans", "Scroll-and-flower panels", "Counterchanged whorl chambers",
            "Tab-cell garden seals", "Bowed trefoil lace", "Oblique lozenge brocade" };
        var notes = new[] {
            "A four-petal flower, inner jewel and diagonal buds occupy each square pocket; straight spokes organise the curved masses.",
            "Large palmette compounds span a two-by-two twill repeat. Five ribs and side scrolls give the interior its own structure.",
            "Separated, quieter diamond rosettes sit behind the indigo braid. The mesh is the foreground reading, with clear gaps between neighbouring leaves.",
            "A three-lobed flower and its straight tripod are fitted to each triangular face. The opposite face reverses the compound.",
            "A five-rib palmette fills each inter-wave chamber. Paired tangent scrolls turn the quiet space into a second pattern.",
            "Horizontal lens fleurons have long petals, short cross petals, enclosing scrolls and a centre jewel.",
            "Lowered five-lobed fans fill every scallop row, including the top course, while leaving clearance beneath each arch.",
            "A centre blossom is held between two tangent scrolls, with edge-normal side buds. The scrolls are construction for a whole chamber.",
            "A four-part whorl combines straight radial guides, broad curling petals and an inner flower. Alternating chamber tones emphasise the negative spaces.",
            "A diagonally oriented garden seal occupies the upper-left-biased interior focus, answering the tab and recess rather than the lattice centre.",
            "A curved trefoil is biased left and upward within each bowed triangular face. The same world-space bias applies to both orientations.",
            "An oblique lozenge, four broad lobes and internal ribs occupy a left-biased cell focus. Even mesh outlines retain the flat ribbon reading." };

        switch (number)
        {
            case 1: case 2: case 3:
                var step = number == 2 ? 2 : 1;
                for (var r = 0; r < rows - step; r += step) for (var c = 0; c < cols - step; c += step)
                {
                    var inside = b.Scaffold(c, r, c + step, r + step);
                    var kind = number == 2 ? 1 : 0; var seed = b.Seed(kind, number == 2 ? 10 : 7);
                    var map = number == 3 ? Vocabulary.Map(f, new(1, 1), new(1, -1)) : Parts.Identity(f);
                    b.Put(inside, seed, map);
                }
                break;
            case 4: case 11:
                for (var i = 0; i < cells.Length; i++)
                {
                    var inside = Interior.FromCycle(f, cells[i]);
                    // The centroid may be below this common-reach resolution.
                    // Use a declared corner-relative interior focus instead;
                    // retain its sum receipt, never silently round a centroid.
                    var first = (Relation)E(f.Read(inside.Sections[0]).ControlWords)[0];
                    var reversed = i % 2 != 0;
                    // This is an authored optical-focus bias, in the shared
                    // presentation frame, not a computed area centroid. Keep
                    // the same displacement through both motif orientations.
                    var bias = number == 11 ? new V(-2, 2) : new V(0, 0);
                    b.Put(inside, b.Seed(2, 9), reversed ? Vocabulary.Half(f) : Parts.Identity(f),
                        first, (reversed ? new V(0, 13) : new V(12, 7)) + bias);
                }
                break;
            case 5: case 8:
                for (var r = 0; r < rows - 1; r++) for (var c = 0; c < cols - 1; c++)
                {
                    var inside = b.Scaffold(c, r, c + 1, r + 1);
                    b.Put(inside, b.Seed(number == 5 ? 1 : 3, number == 5 ? 7 : 9),
                        (c + r) % 2 == 0 ? Parts.Identity(f) : Vocabulary.Half(f));
                }
                break;
            case 6:
                for (var r = 0; r < rows; r++) for (var c = 0; c < cols - 1; c++)
                {
                    var inside = b.Scaffold(c, r, c + 1, r);
                    b.Put(inside, b.Seed(4, 7), Parts.Identity(f));
                }
                break;
            case 7:
                for (var r = 0; r < rows; r++) for (var c = 0; c < cols - 1; c++)
                {
                    var inside = b.Scaffold(c, r, c + 1, r);
                    b.Put(inside, b.Seed(1, 8), Parts.Identity(f), inside.Centre, new(0, 4));
                }
                break;
            case 9: case 10: case 12:
                foreach (var cell in cells)
                {
                    var inside = Interior.FromCycle(f, cell);
                    // Cells with tab subsegments have more endpoints at the
                    // same repeat boundary. Opposite repeat corners define
                    // the construction centre, rather than averaging tabs.
                    var cornerWords = inside.Sections.SelectMany(e => E(f.Read(e).ControlWords).Take(1)).Cast<Relation>().ToArray();
                    var origin = cornerWords[0];
                    var orientation = number == 10 ? Vocabulary.Map(f, new(1, 1), new(-1, 1)) : Parts.Identity(f);
                    var focus = number == 12 ? new V(18, 12) : number == 10 ? new V(12, 16) : new V(14, 14);
                    b.Put(inside, b.Seed(number == 9 ? 3 : number == 10 ? 0 : 5, number == 9 ? 10 : 9), orientation, origin, focus);
                }
                break;
        }
        return b.Finish(names[number - 1], notes[number - 1], number is 3 or 6 or 9 or 11);
    }

    private sealed record Motif(Vocabulary.Unit Unit, Parts.Template Guides);
    private sealed class Builder(Previous.Design previous)
    {
        private readonly Curve.Frame frame = previous.Unit.Template.Frame;
        private readonly Dictionary<(int, int), Motif> seeds = [];
        private readonly List<Placement> placements = [];
        private readonly List<Vocabulary.Unit> units = [];
        private readonly List<Parts.Template> guides = [];
        private readonly Dictionary<(int, int), Attach.PortRead> ports = [];
        private Attach.PortRead[]? endpoints;

        private Attach.PortRead Port(int c, int r)
        {
            if (ports.TryGetValue((c, r), out var cached)) return cached;
            var node = OrnamentSubstrate.At(previous.Source.Lattice, c, r);
            if (previous.Source.Crosses.Count > 0)
            {
                var crossing = previous.Source.Crosses.First(x => x.Surface.Left is Relation { Right: var n } && ReferenceEquals(n, node.Surface));
                var pair = (Relation)((Relation)crossing.Surface.Right).Left;
                return ports[(c, r)] = Attach.Interior(previous.Source.Lattice, node, (Relation)pair.Left, crossing.Surface);
            }
            endpoints ??= previous.Mesh.Template.Envelopes.SelectMany(e => new[] { Attach.Endpoint(frame, e, true), Attach.Endpoint(frame, e, false) }).Where(p => p.Complete).ToArray();
            frame.Vector(node.Position, out var position);
            return ports[(c, r)] = endpoints.First(p => frame.Vector(p.Position, out var at) && at == position);
        }
        public Interior.Read Scaffold(int c, int r, int cc, int rr)
        {
            var selected = new[] { Port(c, r), Port(cc, r), Port(cc, rr), Port(c, rr) };
            return Interior.FromScaffold(frame, S(selected.Select(p => (IElement)p.Section).Distinct<IElement>(ReferenceEqualityComparer.Instance).ToArray()),
                S(selected.Select(p => (IElement)p.Surface).ToArray()));
        }
        public Motif Seed(int kind, int reach)
        {
            if (seeds.TryGetValue((kind, reach), out var seed)) return seed;
            var a = new Author(frame); var g = new Author(frame);
            switch (kind)
            {
                case 0:
                    foreach (var d in new V[] { new(1, 0), new(0, 1), new(-1, 0), new(0, -1) })
                    { a.Petal(g, d * 2, d * reach, Math.Max(2, reach / 3), 2); a.Line(d * 2, d * (reach - 1), 3); g.Line(new(0, 0), d * (reach + 1), 0); }
                    foreach (var d in new V[] { new(1, 1), new(-1, 1), new(-1, -1), new(1, -1) })
                        a.Petal(g, d * 2, d * (reach / 2), 1, 3);
                    a.Polygon([new(-2, 0), new(0, -2), new(2, 0), new(0, 2)], 3);
                    g.Polygon([new(0, -reach), new(reach, 0), new(0, reach), new(-reach, 0)], 0);
                    break;
                case 1:
                    // The fan is a compound of five broad lobes, ribs and
                    // paired tangent scrolls, not a single tapered mark.
                    var root = new V(0, -reach + 2);
                    foreach (var tip in new V[] { new(-reach + 1, 0), new(-reach / 2, reach - 2), new(0, reach), new(reach / 2, reach - 2), new(reach - 1, 0) })
                    { a.Petal(g, root, tip, 2, 2); a.Line(root, tip + new V(0, -1), 3); g.Line(root, tip, 0); }
                    a.Scroll([new(-1, -reach + 2), new(-reach, -reach + 1), new(-reach, reach / 2), new(-reach + 2, 1)], 1);
                    a.Scroll([new(1, -reach + 2), new(reach, -reach + 1), new(reach, reach / 2), new(reach - 2, 1)], 1);
                    a.Polygon([root + new V(-2, 0), root + new V(0, -2), root + new V(2, 0), root + new V(0, 2)], 3);
                    break;
                case 2:
                    foreach (var tip in new V[] { new(-reach + 1, -3), new(reach - 1, -3), new(0, reach - 1) })
                    { a.Petal(g, new(0, 0), tip, 2, 2); a.Line(new(0, 0), tip, 3); g.Line(new(0, 0), tip + tip, 0); }
                    a.Polygon([new(-2, -1), new(2, -1), new(0, 2)], 3);
                    break;
                case 3:
                    foreach (var d in new V[] { new(1, 0), new(0, 1), new(-1, 0), new(0, -1) })
                    {
                        V Turn(V p) => new(d.X * p.X - d.Y * p.Y, d.Y * p.X + d.X * p.Y);
                        a.Scroll([Turn(new(1, 0)), Turn(new(reach, -reach)), Turn(new(reach, reach)), Turn(new(3, reach - 2))], 1);
                        a.Petal(g, Turn(new(2, 0)), Turn(new(reach - 1, 1)), 2, 2);
                        a.Line(Turn(new(2, 0)), Turn(new(reach - 2, 1)), 3);
                        g.Line(new(0, 0), d * reach, 0);
                    }
                    a.Polygon([new(0, -2), new(2, 0), new(0, 2), new(-2, 0)], 3);
                    break;
                case 4:
                    a.Petal(g, new(-1, 0), new(-reach, 0), 2, 2); a.Petal(g, new(1, 0), new(reach, 0), 2, 2);
                    a.Petal(g, new(0, 0), new(0, 4), 2, 3); a.Petal(g, new(0, 0), new(0, -4), 2, 3);
                    foreach (var d in new V[] { new(1, 1), new(-1, 1), new(-1, -1), new(1, -1) })
                        a.Petal(g, d, new(d.X * 4, d.Y * 3), 1, 2);
                    a.Line(new(-reach + 1, 0), new(reach - 1, 0), 3); a.Line(new(0, -3), new(0, 3), 1);
                    a.Scroll([new(-reach + 1, 0), new(-reach / 2, 6), new(reach / 2, 6), new(reach - 1, 0)], 1);
                    a.Scroll([new(-reach + 1, 0), new(-reach / 2, -6), new(reach / 2, -6), new(reach - 1, 0)], 1);
                    a.Polygon([new(-1, 0), new(0, -1), new(1, 0), new(0, 1)], 3);
                    g.Line(new(-reach, 0), new(reach, 0), 0); g.Line(new(0, -5), new(0, 5), 0); g.Line(new(-reach / 2, -3), new(reach / 2, 3), 0);
                    break;
                case 5:
                    // Broad flat lobes, a lozenge and even ribs. No varying
                    // stroke silhouette is used for this oblique design.
                    foreach (var tip in new V[] { new(8, 0), new(4, 7), new(-8, 0), new(-4, -7) })
                    { a.Petal(g, new(0, 0), tip, 2, 2); a.Line(new(0, 0), tip, 3); g.Line(-tip, tip, 0); }
                    a.Polygon([new(0, -8), new(11, 0), new(0, 8), new(-11, 0)], 1);
                    a.Polygon([new(-2, 0), new(0, -2), new(2, 0), new(0, 2)], 3);
                    break;
            }
            return seeds[(kind, reach)] = new(a.Finish(), g.Finish().Template);
        }

        public void Put(Interior.Read inside, Motif seed, QualitativePatternTransform transform, Relation? anchor = null, V? displacement = null)
        {
            var actual = anchor ?? inside.Centre; IElement receipt = inside.Surface;
            if (displacement is { } offset)
            {
                var sum = frame.Motion.Reduce(actual, frame.Word(offset.X, offset.Y));
                actual = frame.Motion.TryReadResult(sum, out var result, out _, out _) ? result : StructuralConstraint.Open(sum);
                receipt = new Relation(new Relation(inside.Surface, anchor ?? inside.Centre), sum);
            }
            if (!frame.Vector(actual, out _)) throw new InvalidOperationException("Selected interior focus remains open; choose an explicit construction landmark.");
            var app = Parts.Place(seed.Unit.Template, transform, actual); var guide = Parts.Place(seed.Guides, transform, actual);
            Relation Image(Relation original) => (Relation)app.EnvelopePairs.Single(p => ReferenceEquals(p.Left, original)).Right;
            var roles = seed.Unit.Roles.Select(scope => S(E(scope).Cast<Relation>().Select(e => (IElement)Image(e)).ToArray())).ToArray();
            var branch = previous.Source.Id is "S008" or "S009" ? BoundaryBranch(inside, actual) : null;
            var surface = new Relation(new Relation(inside.Surface, new Relation(receipt, actual)), new Relation(seed.Unit.Surface, new Relation(app.Surface, new Relation(guide.Surface, branch?.Surface ?? S()))));
            var unit = Vocabulary.Make(app.Output, roles, [], [app.Surface], [], [], S(app.Output.Envelopes.Cast<IElement>().ToArray()), null, null, surface);
            placements.Add(new(inside, actual, (Relation)receipt, app, guide, roles, branch?.Envelope, surface)); units.Add(unit); guides.Add(guide.Output);
            // A useful attachment construction need not be part of the final
            // decorative read. Retain the actual branch and proof in guides.
            if (branch is not null) guides.Add(branch.Unit.Template);
        }
        private sealed record Branch(Vocabulary.Unit Unit, Relation Envelope, Relation Surface);
        private Branch BoundaryBranch(Interior.Read inside, Relation anchor)
        {
            var env = previous.Mesh.Template.Envelopes.First(e => ReferenceEquals(Band.ReadEnvelope(frame, e).Sweep.Spine.Section, inside.Sections[0]));
            var port = Attach.Endpoint(frame, env, true); var receipts = new List<IElement>();
            Relation Sum(Relation a, Relation b)
            { var sum = frame.Motion.Reduce(a, b); receipts.Add(sum); return frame.Motion.TryReadResult(sum, out var result, out _, out _) ? result : StructuralConstraint.Open(sum); }
            var first = Sum(port.Position, port.Tangent);
            var second = Sum(anchor, frame.Word(-8, -2)); var end = Sum(anchor, frame.Word(-2, -1));
            var section = frame.Cubic(port.Site!, new Placeholder(), port.Position, first, second, end);
            var zero = Enumerable.Range(0, 4).Select(_ => frame.Word(0, 0)).ToArray(); var sweep = Band.Sweep(frame, section, zero, zero); var read = Band.Read(frame, sweep);
            var output = Band.BandEnvelope(sweep, Band.Cap(frame, read, true), Band.Cap(frame, read, false));
            var attachment = Attach.Attach(frame, port.Surface, output);
            var proof = new Relation(new Relation(inside.Surface, anchor), new Relation(port.Surface, new Relation(S(receipts.ToArray()), attachment.Surface)));
            var template = Parts.Create(frame, S(output));
            var unit = Vocabulary.Make(template, [S(), S(output), S(), S()], [], [], [], [], S(output), null, null, proof);
            return new(unit, output, proof);
        }
        public Design Finish(string name, string note, bool groundFocus)
        {
            var ornament = Vocabulary.Merge(units.ToArray()); var output = Vocabulary.Merge(previous.Mesh, ornament);
            var selected = groundFocus ? S(placements.Select(p => (IElement)p.Interior.Surface).ToArray()) : ornament.Highlight;
            var mesh = new HashSet<Relation>(previous.Mesh.Template.Envelopes, ReferenceEqualityComparer.Instance);
            var complement = S(previous.Unit.Template.Envelopes.Where(e => !mesh.Contains(e)).Cast<IElement>().ToArray());
            var recessed = previous.Source.Id == "S003" ? S(ornament.Template.Envelopes.Cast<IElement>().ToArray()) : S();
            var surface = new Relation(previous.Surface, new Relation(new Relation(previous.Mesh.Surface, complement),
                new Relation(output.Surface, new Relation(S(placements.Select(p => (IElement)p.Surface).ToArray()), new Relation(selected, recessed)))));
            return new("F" + previous.Id[1..], name, note, previous, previous.Mesh, output, placements,
                Parts.Compose(frame, guides.ToArray()), groundFocus, recessed, surface);
        }
    }

    private sealed class Author(Curve.Frame frame)
    {
        private readonly List<Relation> threads = []; private readonly List<Relation>[] roles = [[], [], [], []];
        private Relation W(V p) => frame.Word(p.X, p.Y);
        public void Line(V p, V q, int role) => Curve([p, q], [new(0, 0), new(0, 0)], role);
        public void Scroll(V[] points, int role) => Curve(points, points.Select(_ => new V(0, 0)).ToArray(), role);
        public void Polygon(V[] points, int role)
        {
            var site = new Placeholder(); var first = site; var output = new List<Relation>();
            for (var i = 0; i < points.Length; i++)
            {
                var next = i == points.Length - 1 ? first : new Placeholder();
                output.Add(Envelope(frame.Line(site, next, W(points[i]), W(points[(i + 1) % points.Length])), [W(new(0, 0)), W(new(0, 0))], [W(new(0, 0)), W(new(0, 0))], role)); site = next;
            }
            threads.Add(S(output.Cast<IElement>().ToArray()));
        }
        public void Petal(Author guides, V root, V tip, int width, int role)
        {
            var delta = tip + -root; var n = new V(-Math.Sign(delta.Y), Math.Sign(delta.X)) * width;
            var p = root + new V(delta.X / 3, delta.Y / 3); var q = root + new V(2 * delta.X / 3, 2 * delta.Y / 3);
            var actualGuide = guides.Curve([root, p, q, tip], [new(0, 0), new(0, 0), new(0, 0), new(0, 0)], 0);
            var spine = Band.ReadEnvelope(frame, actualGuide).Sweep.Spine.Section;
            var widths = new V[] { new(0, 0), n, n, new(0, 0) };
            var env = Envelope(spine, widths.Select(W).ToArray(), widths.Select(v => W(-v)).ToArray(), role);
            threads.Add(S(env));
        }
        private Relation Curve(V[] controls, V[] offsets, int role)
        {
            var a = new Placeholder(); var b = new Placeholder(); var words = controls.Select(W).ToArray();
            var curve = words.Length == 2 ? frame.Line(a, b, words[0], words[1]) : frame.Cubic(a, b, words[0], words[1], words[2], words[3]);
            var env = Envelope(curve, offsets.Select(W).ToArray(), offsets.Select(v => W(-v)).ToArray(), role); threads.Add(S(env)); return env;
        }
        private Relation Envelope(Relation curve, Relation[] a, Relation[] b, int role)
        {
            var sweep = Band.Sweep(frame, curve, a, b); var read = Band.Read(frame, sweep);
            var env = Band.BandEnvelope(sweep, Band.Cap(frame, read, true), Band.Cap(frame, read, false), role is 0 or 3 && read.Collapsed == true ? null : Band.Occupy(read));
            roles[role].Add(env); return env;
        }
        public Vocabulary.Unit Finish()
        {
            var t = Parts.Create(frame, threads.ToArray());
            return Vocabulary.Make(t, roles.Select(r => S(r.Cast<IElement>().ToArray())).ToArray(), [], [], [], [], S(t.Envelopes.Cast<IElement>().ToArray()), null, null, S());
        }
    }
}
