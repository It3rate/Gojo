using System.Numerics;
using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;
using static Gojo.Experiments.QualitativeFrieze.Composition.JointNative;
using Band = Gojo.Experiments.QualitativeExecution.QualitativeBand;
using Curve = Gojo.Experiments.QualitativeExecution.QualitativeCurve;
using Parts = Gojo.Experiments.QualitativeFrieze.Composition.OrnamentBandComposition;
using Vocabulary = Gojo.Experiments.QualitativeFrieze.Composition.OrnamentCompositionCorpus;
using Source = Gojo.Experiments.QualitativeFrieze.Composition.OrnamentSubstrateCorpus;
using Attach = Gojo.Experiments.QualitativeFrieze.Composition.OrnamentMotifAttachment;
using V = Gojo.Experiments.QualitativeFrieze.Composition.OrnamentCompositionCorpus.V;

namespace Gojo.Experiments.QualitativeFrieze.Composition;

/// <summary>A replacement detail layer over the exact previous mesh. Motifs
/// consume exposed mesh positions and tangent ports; previous decorations are
/// a retained complement. Host recipes choose designs, native relations retain
/// anchors, generated controls, shared roots, incidence and flow evidence.</summary>
public static class OrnamentIntegratedCorpus
{
    // integrated = source -> ((mesh selection -> carried old decoration) ->
    //                (output unit -> (Closed(attachments) ->
    //                  (Closed(actual motif groups) -> Closed(profile applications)))))
    public sealed record Design(string Id, string Name, string Note, Source.Design Source,
        Vocabulary.Unit Mesh, Vocabulary.Unit Unit, IReadOnlyList<Attach.AttachmentRead> Attachments,
        IReadOnlyList<Relation> Motifs, IReadOnlyList<Relation> Generations, Relation Removed, Relation Surface);

    public static IReadOnlyList<Design> Cases(int limit = int.MaxValue, string? caseId = null) =>
        Source.Cases(caseId is null ? limit : int.MaxValue).Where(s => caseId is null || "I" + s.Id[1..] == caseId).Select(Integrate).Take(limit).ToArray();

    public static Design Integrate(Source.Design source)
    {
        var b = new Builder(source); var cols = source.Lattice.ColumnPhases.Count; var rows = source.Lattice.RowPhases.Count;
        switch (source.Id)
        {
            case "S001": case "S003":
                for (var r = 0; r < rows - 1; r++) for (var c = 0; c < cols - 1; c++) if ((c + r) % 2 == 0)
                {
                    var p = b.Interior(c, r, horizontal: true); var q = b.Interior(c + 1, r + 1, horizontal: true);
                    if (source.Id == "S001") { b.Leaf(p, new(5, 0), new(12, 1), new(13, 10), new(0, 3)); b.Leaf(q, new(-5, 0), new(-12, -1), new(-13, -10), new(0, -3)); }
                    else { b.Leaf(p, new(4, 3), new(12, 5), new(17, 0), new(-2, 2)); b.Leaf(q, new(-4, -3), new(-12, -5), new(-17, 0), new(2, -2)); }
                }
                b.CrossingFlorets(4); break;
            case "S002":
                for (var r = 1; r < rows - 1; r += 2) for (var c = 1; c < cols - 1; c += 2)
                {
                    var port = b.Interior(c, r, horizontal: (c + r) % 4 < 2);
                    var horizontal = b.Tangent(port).Y == 0;
                    b.Leaf(port, horizontal ? new(6, 0) : new(0, 6), horizontal ? new(15, 0) : new(0, 15),
                        horizontal ? new(20, 6) : new(6, 20), horizontal ? new(0, 3) : new(3, 0));
                    b.Floret(port, 3, false);
                }
                break;
            case "S004": case "S011":
                for (var r = 0; r < rows - 1; r++) for (var c = 0; c < cols - 1; c++)
                {
                    var east = b.Endpoint(c, r, preferHorizontal: true); var rising = b.Endpoint(c, r, preferHorizontal: false);
                    if (source.Id == "S004") { b.Leaf(east, new(7, 0), new(14, 2), new(15, 7), new(0, 3)); b.Leaf(rising, new(3, 5), new(3, 12), new(8, 15), new(-2, 1)); }
                    else { b.Leaf(east, new(6, 2), new(14, 2), new(15, 8), new(0, 3)); b.Leaf(rising, new(1, 7), new(4, 13), new(9, 15), new(-2, 1)); }
                }
                for (var r = 1; r < rows - 1; r += 2) for (var c = 1; c < cols - 1; c += 2) b.Floret(b.Endpoint(c, r, true), 6, true);
                break;
            case "S005":
                for (var r = 0; r < rows; r++) for (var c = 1; c < cols - 1; c += 2)
                {
                    var port = b.Endpoint(c, r, true);
                    b.Leaf(port, new(4, 6), new(11, 8), new(14, 7), new(-2, 1));
                    b.Leaf(port, new(-4, -6), new(-11, -8), new(-14, -7), new(2, -1));
                    if (c == 3 && r % 2 == 1) b.Floret(port, 4, false);
                }
                break;
            case "S006":
                foreach (var env in b.Mesh.Template.Envelopes)
                {
                    var read = Band.ReadEnvelope(b.Frame, env).Sweep.Spine;
                    var sign = Math.Sign((int)(read.Controls[1].Y - read.Controls[0].Y));
                    b.Hug(env, [new(0, 0), new(0, -sign * 10), new(0, 0)]);
                }
                for (var r = 1; r < rows; r += 2) for (var c = 2; c < cols - 1; c += 2) b.Floret(b.Endpoint(c, r, true), 3, false);
                break;
            case "S007":
                foreach (var env in b.Mesh.Template.Envelopes)
                {
                    var first = Attach.Endpoint(b.Frame, env, start: true); var last = Attach.Endpoint(b.Frame, env, start: false);
                    b.Leaf(first, new(1, 9), new(7, 12), new(12, 7), new(-2, 1));
                    b.Leaf(last, new(-1, 9), new(-7, 12), new(-12, 7), new(2, 1));
                }
                for (var r = 1; r < rows; r += 2) b.Floret(b.Endpoint(2, r, true), 4, false);
                break;
            case "S008":
                foreach (var env in b.Mesh.Template.Envelopes)
                {
                    b.Leaf(Attach.Endpoint(b.Frame, env, start: true), new(5, 4), new(11, 6), new(10, 16), new(-2, 2));
                    b.Leaf(Attach.Endpoint(b.Frame, env, start: false), new(-5, -4), new(-11, -6), new(-10, -16), new(2, -2));
                }
                for (var c = 1; c < cols; c += 2) for (var r = 1; r < rows; r += 2) b.Floret(b.Endpoint(c, r, false), 4, false);
                break;
            case "S009":
                for (var r = 0; r < rows - 1; r++) for (var c = 0; c < cols - 1; c++)
                {
                    var h = b.Endpoint(c, r, true); var v = b.Endpoint(c, r, false);
                    b.Hug(h.Envelope, [new(0, 0), new(0, 9), new(0, 9), new(0, 0)]);
                    b.Hug(v.Envelope, [new(0, 0), new(9, 0), new(9, 0), new(0, 0)]);
                }
                for (var r = 1; r < rows - 1; r++) for (var c = 1; c < cols - 1; c++) b.Floret(b.Endpoint(c, r, true), 5, false);
                break;
            case "S010": case "S012":
                var slanted = source.Id == "S012";
                for (var r = 0; r < rows - 1; r++) for (var c = 0; c < cols - 1; c++)
                {
                    var p = b.Point(c, r); var shoulder = b.FindEndpoint(p + (slanted ? new V(4, 8) : new V(0, 9)), true, curved: true);
                    b.Leaf(shoulder, slanted ? new(3, -3) : new(7, 0), slanted ? new(14, 11) : new(16, 0), slanted ? new(22, 12) : new(20, 11), slanted ? new(-2, 4) : new(0, 5));
                }
                for (var r = 1; r < rows - 1; r++) for (var c = 1; c < cols - 1; c += 2) b.Floret(b.Endpoint(c, r, true), 4, false);
                break;
        }
        return b.Finish();
    }

    private sealed class Builder
    {
        public Source.Design Source { get; } public Curve.Frame Frame { get; } public Vocabulary.Unit Mesh { get; }
        private readonly List<Relation> threads = []; private readonly List<Relation>[] roles = [[], [], [], []];
        private readonly List<Attach.AttachmentRead> attachments = []; private readonly List<Relation> groups = []; private readonly List<Relation> generations = [];
        private readonly List<(V[] Controls, V[] A, V[] B, Relation Surface)> profiles = [];
        private readonly Relation removed;
        private Attach.PortRead[]? endpointPorts;
        public Builder(Source.Design source)
        {
            Source = source; Frame = source.Ornament.Unit.Template.Frame;
            var original = Parts.ReadTemplate(Frame, source.Ornament.Unit.Template.Surface);
            // Select the source generator outputs from the native design root,
            // not the diagnostic Generations array on the record.
            var generationScope = (Relation)((Relation)((Relation)((Relation)((Relation)source.Surface.Right).Right).Right).Right).Right;
            var curves = new HashSet<IElement>(E(generationScope).Cast<Relation>().Select(g => ((Relation)g.Right).Right), ReferenceEqualityComparer.Instance);
            var selected = original.Envelopes.Where(e => curves.Contains(Band.ReadEnvelope(Frame, e).Sweep.Spine.Section)).ToArray();
            var meshThreads = original.Threads.Select(t => E(t).Where(e => selected.Any(s => ReferenceEquals(s, e))).ToArray()).Where(es => es.Length > 0).Select(S).ToArray();
            var template = Parts.Create(Frame, meshThreads); var sourceRoles = OrnamentCompositionExperiment.Roles(source.Ornament.Unit);
            var meshRoles = sourceRoles.Select(scope => S(E(scope).Where(e => selected.Any(s => ReferenceEquals(s, e))).ToArray())).ToArray();
            removed = S(original.Envelopes.Where(e => !selected.Any(s => ReferenceEquals(s, e))).Cast<IElement>().ToArray());
            Mesh = Vocabulary.Make(template, meshRoles, source.Ornament.Unit.Crossings, [], [], [], source.Focus, null, null,
                new Relation(source.Surface, new Relation(S(selected.Cast<IElement>().ToArray()), removed)));
        }
        public V Point(int c, int r)
        { Frame.Vector(OrnamentSubstrate.At(Source.Lattice, c, r).Position, out var p); return new((int)p.X, (int)p.Y); }
        public V Tangent(Attach.PortRead port)
        { Frame.Vector(port.Tangent, out var p); return new((int)p.X, (int)p.Y); }
        public Attach.PortRead Interior(int c, int r, bool horizontal)
        {
            var node = OrnamentSubstrate.At(Source.Lattice, c, r);
            var cross = Source.Crosses.First(x => x.Surface.Left is Relation p && ReferenceEquals(p.Right, node.Surface));
            var pair = (Relation)((Relation)cross.Surface.Right).Left;
            return Attach.Interior(Source.Lattice, node, (Relation)(horizontal ? pair.Left : pair.Right), cross.Surface);
        }
        public Attach.PortRead Endpoint(int c, int r, bool preferHorizontal) => FindEndpoint(Point(c, r), preferHorizontal);
        public Attach.PortRead FindEndpoint(V at, bool preferHorizontal, bool curved = false)
        {
            endpointPorts ??= Mesh.Template.Envelopes.SelectMany(e => new[] { Attach.Endpoint(Frame, e, true), Attach.Endpoint(Frame, e, false) }).ToArray();
            var options = endpointPorts
                .Where(p => p.Complete && Frame.Vector(p.Position, out var position) && position == new Curve.Vector(at.X, at.Y))
                .Where(p => !curved || Frame.Read(p.Section).Straight == false)
                .OrderByDescending(p => p.AtStart == true)
                .ThenBy(p => { var t = Tangent(p); return preferHorizontal ? Math.Abs(t.Y) * 100 / Math.Max(1, Math.Abs(t.X)) : Math.Abs(t.X) * 100 / Math.Max(1, Math.Abs(t.Y)); }).ToArray();
            return options.First();
        }
        public void CrossingFlorets(int reach)
        {
            for (var r = 1; r < Source.Lattice.RowPhases.Count - 1; r += 2) for (var c = 1; c < Source.Lattice.ColumnPhases.Count - 1; c += 2)
            {
                var node = OrnamentSubstrate.At(Source.Lattice, c, r); var cross = Source.Ornament.Unit.Crossings.First(x => ReferenceEquals(x.Locus, node.Position));
                Floret(Attach.Interior(Source.Lattice, node, cross.Front), reach, false);
            }
        }
        private Relation Word(V p) => Frame.Word(p.X, p.Y);
        private Relation Sum(Relation a, Relation b, List<IElement> receipts)
        { var receipt = Frame.Motion.Reduce(a, b); receipts.Add(receipt); return Frame.Motion.TryReadResult(receipt, out var word, out _, out _) ? word : StructuralConstraint.Open(receipt); }
        public Relation Leaf(Attach.PortRead port, V first, V second, V tip, V width)
        {
            var locals = new[] { new V(0, 0), first, second, tip }; var widths = new[] { new V(0, 0), width, width, new V(0, 0) };
            var env = Profile(port, locals, widths, widths.Select(w => -w).ToArray());
            attachments.Add(Attach.Attach(Frame, port.Surface, env)); groups.Add(S(env)); return env;
        }
        private Relation Profile(Attach.PortRead port, V[] locals, V[] a, V[] b, IElement? root = null)
        {
            var profile = profiles.FirstOrDefault(p => p.Controls.SequenceEqual(locals) && p.A.SequenceEqual(a) && p.B.SequenceEqual(b));
            if (profile.Surface is null)
            {
                profile = (locals, a, b, new Relation(Frame.Surface, new Relation(S(locals.Select(v => (IElement)Word(v)).ToArray()),
                    new Relation(S(a.Select(v => (IElement)Word(v)).ToArray()), S(b.Select(v => (IElement)Word(v)).ToArray())))));
                profiles.Add(profile);
            }
            var detail = (Relation)profile.Surface.Right; var offsets = (Relation)detail.Right;
            var receipts = new List<IElement>(); var words = E((Relation)detail.Left).Cast<Relation>().Select(local => Sum(port.Position, local, receipts)).ToArray();
            var site = root ?? port.Site ?? new Placeholder(); var end = new Placeholder();
            var spine = words.Length == 3 ? Frame.Quadratic(site, end, words[0], words[1], words[2]) : Frame.Cubic(site, end, words[0], words[1], words[2], words[3]);
            var env = Envelope(spine, E((Relation)offsets.Left).Cast<Relation>().ToArray(), E((Relation)offsets.Right).Cast<Relation>().ToArray(), 2);
            generations.Add(new Relation(new Relation(profile.Surface, port.Surface), new Relation(S(receipts.ToArray()), env)));
            Vein(spine); return env;
        }
        private Relation Envelope(Relation spine, Relation[] a, Relation[] b, int role)
        {
            var sweep = Band.Sweep(Frame, spine, a, b); var read = Band.Read(Frame, sweep);
            var env = Band.BandEnvelope(sweep, Band.Cap(Frame, read, true), Band.Cap(Frame, read, false), role == 3 ? null : Band.Occupy(read));
            threads.Add(S(env)); roles[role].Add(env); return env;
        }
        private void Vein(Relation spine)
        { var read = Frame.Read(spine); var zero = read.Controls.Select(_ => Word(new(0, 0))).ToArray(); Envelope(spine, zero, zero, 3); }
        public void Hug(Relation source, V[] inset)
        {
            var read = Band.ReadEnvelope(Frame, source).Sweep.Spine; var zero = inset.Select(_ => Word(new(0, 0))).ToArray();
            var env = Envelope(read.Section, zero, inset.Select(Word).ToArray(), 2);
            var port = Attach.Endpoint(Frame, source, true); attachments.Add(Attach.Attach(Frame, port.Surface, env)); groups.Add(S(env));
            var band = Band.ReadEnvelope(Frame, env).Sweep;
            // The inner boundary is itself a derived native curve and supplies
            // a quieter rib, without adding a second unrelated ornament.
            Vein(band.SideB.Section); generations.Add(new Relation(new Relation(source, port.Surface), env));
        }
        public void Floret(Attach.PortRead port, int reach, bool sixfold)
        {
            var root = port.Site ?? new Placeholder(); var envs = new List<IElement>();
            V[] tips = sixfold ? [new(reach, 0), new(reach / 2, reach * 5 / 6), new(-reach / 2, reach * 5 / 6), new(-reach, 0), new(-reach / 2, -reach * 5 / 6), new(reach / 2, -reach * 5 / 6)] :
                [new(reach, 0), new(0, reach), new(-reach, 0), new(0, -reach)];
            if (Source.Id == "S003") tips = [new(reach * 2, reach * 3 / 2), new(-reach * 2, reach * 3 / 2), new(-reach * 2, -reach * 3 / 2), new(reach * 2, -reach * 3 / 2)];
            if (Source.Id == "S012") tips = [new(reach, 0), new(reach / 2, reach), new(-reach, 0), new(-reach / 2, -reach)];
            Relation? selected = null;
            foreach (var tip in tips)
            {
                var normal = new V(-Math.Sign(tip.Y), Math.Sign(tip.X)); var w = reach >= 5 ? normal * 2 : normal;
                V Portion(int n) => Source.Id is "S003" or "S012" ? new(tip.X / 2, tip.Y / 2) : new(tip.X * n / 3, tip.Y * n / 3);
                var env = Profile(port, [new(0, 0), Portion(1), Portion(2), tip], [new(0, 0), w, w, new(0, 0)], [new(0, 0), -w, -w, new(0, 0)], root);
                envs.Add(env); var at = Attach.Attach(Frame, port.Surface, env);
                if (selected is null || at.Smooth == true) selected = env;
            }
            if (selected is not null) attachments.Add(Attach.Attach(Frame, port.Surface, selected));
            groups.Add(S(envs.ToArray()));
        }
        public Design Finish()
        {
            var template = Parts.Create(Frame, threads.ToArray()); var ornament = Vocabulary.Make(template,
                roles.Select(r => S(r.Cast<IElement>().ToArray())).ToArray(), [], [], [], [], S(template.Envelopes.Cast<IElement>().ToArray()), null, null, S(generations.Cast<IElement>().ToArray()));
            var output = Vocabulary.Merge(Mesh, ornament);
            var surface = new Relation(Source.Surface, new Relation(new Relation(Mesh.Surface, removed), new Relation(output.Surface,
                new Relation(S(attachments.Select(a => (IElement)a.Surface).ToArray()), new Relation(S(groups.Cast<IElement>().ToArray()), S(generations.Cast<IElement>().ToArray()))))));
            var id = "I" + Source.Id[1..];
            return new(id, Source.Name + " · rooted ornament", "Larger motifs consume mesh endpoint or crossing-landmark ports. Shared roots, tangent agreement and curve-following profiles remain inspectable alongside the preserved mesh.", Source, Mesh, output, attachments, groups, generations, removed, surface);
        }
    }
}
