using System.Numerics;
using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;
using static Gojo.Experiments.QualitativeFrieze.Composition.JointNative;
using Curve = Gojo.Experiments.QualitativeExecution.QualitativeCurve;

namespace Gojo.Experiments.QualitativeFrieze.Composition;

/// <summary>Native polynomial scenes and an intentionally sparse public
/// scaffold. Endpoint locations are supplied at an unmeasured repeated-reach
/// resolution. Private handles and their interpolation histories are omitted.
/// This tests curve completion on a given scaffold, not blind reconstruction.</summary>
public static class CurveEvidenceScene
{
    // scene = (curve frame -> Closed(actual sections)) -> (assumed -> interior)
    // interior = Closed(ordered-axis orientation); open leaves it unselected.
    // span = (role -> endpoint pair) -> ((start word -> end word) ->
    //        ((start heading -> end heading) -> (trace template -> bend word)))
    // message = (frame -> (line template -> curve template)) ->
    //           (Closed(spans) -> (Closed(join facts) -> interior))
    // join fact = (incoming role -> outgoing role) ->
    //             (shared site -> (smooth -> (cusp -> corner)))
    // Every asserted join scope contains its actual public shared-site pair.
    public sealed class Scene
    {
        public Curve.Frame Frame { get; }
        public IReadOnlyList<Relation> Sections { get; }
        public Relation Assumed { get; }
        public Relation Interior { get; }
        public Relation Surface { get; }
        public IReadOnlyList<Curve.Read> Reads => Sections.Select(Frame.Read).ToArray();
        public int? InteriorSide => Side(Frame, Interior);
        public bool Closed => Sections.Count != 0 && Reads.SelectMany(read => new[] { read.Start, read.End })
            .Distinct(FieldNative.Participants.Instance).All(site => Reads.Sum(read =>
                (FieldNative.Same(read.Start, site) ? 1 : 0) + (FieldNative.Same(read.End, site) ? 1 : 0)) == 2);
        public Scene(Curve.Frame frame, IEnumerable<Relation> sections, Relation? assumed = null, Relation? interior = null)
        {
            Frame = frame; Sections = sections.ToArray(); Assumed = assumed ?? S(); Interior = interior ?? StructuralConstraint.Open();
            Surface = new(new Relation(frame.Surface, S(Sections.Cast<IElement>().ToArray())), new Relation(Assumed, Interior));
        }
    }
    public sealed record Span(IElement Role, IElement From, IElement To, Relation Start, Relation End,
        Relation StartHeading, Relation EndHeading, IElement Trace, Relation Bends, Relation Surface);
    public sealed record Join(Span Incoming, Span Outgoing, Relation Smooth, Relation Cusp, Relation Corner, Relation Surface);
    public sealed record Message(Curve.Frame Frame, Relation LineTemplate, Relation CurveTemplate,
        IReadOnlyList<Span> Spans, IReadOnlyList<Join> Joins, Relation Interior, Relation Surface)
    { public int? InteriorSide => Side(Frame, Interior); }

    public static Relation Interior(Curve.Frame frame, int? side) => side is 1 ? S(new Relation(frame.Motion.Horizontal, frame.Motion.Vertical)) :
        side is -1 ? S(new Relation(frame.Motion.Vertical, frame.Motion.Horizontal)) : StructuralConstraint.Open();
    public static int? Side(Curve.Frame frame, Relation surface)
    {
        if (!Closed(surface) || E(surface) is not [Relation order]) return null;
        return ReferenceEquals(order.Left, frame.Motion.Horizontal) && ReferenceEquals(order.Right, frame.Motion.Vertical) ? 1 :
            ReferenceEquals(order.Left, frame.Motion.Vertical) && ReferenceEquals(order.Right, frame.Motion.Horizontal) ? -1 : null;
    }

    public static Message Publish(Scene source, bool tangents = true, bool bends = true, bool traces = true, bool interior = true)
    {
        var frame = new Curve.Frame(new QualitativeMotionFrame());
        // Trace-type correspondence uses readable native examples. An open
        // cubic grammar alone would not assert curvature: its controls could
        // also be collinear. Only their straightness read is communicated as
        // equivalent, not their exact directions, extent or control recipe.
        var line = frame.Line(new Placeholder(), new Placeholder(), frame.Word(0, 0), frame.Word(1, 0));
        var curve = frame.Cubic(new Placeholder(), new Placeholder(), frame.Word(0, 0), frame.Word(0, 1), frame.Word(1, 1), frame.Word(1, 0));
        var sites = source.Reads.SelectMany(read => new[] { read.Start, read.End }).Distinct(FieldNative.Participants.Instance)
            .ToDictionary(site => site, _ => (IElement)new Placeholder(), FieldNative.Participants.Instance);
        Relation Word(Curve.Vector v) => frame.Word((int)v.X, (int)v.Y);
        Relation Head(Curve.Vector v) => v.Zero || !tangents ? StructuralConstraint.Open() :
            S((v.X.Sign == 0 ? Array.Empty<IElement>() : new IElement[] { v.X.Sign > 0 ? frame.Motion.Right : frame.Motion.Left })
                .Concat(v.Y.Sign == 0 ? [] : new IElement[] { v.Y.Sign > 0 ? frame.Motion.Up : frame.Motion.Down }).ToArray());
        var orientation = new Relation(frame.Motion.Horizontal, frame.Motion.Vertical);
        var spans = source.Reads.Select(read => {
            var role = new Placeholder(); var from = sites[read.Start]; var to = sites[read.End];
            var a = Word(read.Controls[0]); var b = Word(read.Controls[^1]);
            var start = Head(read.StartVector); var end = Head(read.EndVector);
            IElement trace = !traces || read.Straight is null ? StructuralConstraint.Open() : read.Straight.Value ? line : curve;
            var turn = !bends ? StructuralConstraint.Open() : read.Inflected ? S(orientation, orientation.Reverse()) :
                read.BendSign == 1 ? S(orientation) : read.BendSign == -1 ? S(orientation.Reverse()) : read.BendSign == 0 ? S() : StructuralConstraint.Open();
            var surface = new Relation(new Relation(role, new Relation(from, to)), new Relation(new Relation(a, b),
                new Relation(new Relation(start, end), new Relation(trace, turn))));
            return new Span(role, from, to, a, b, start, end, trace, turn, surface);
        }).ToArray();
        var joins = new List<Join>(); var original = source.Reads;
        for (var i = 0; i < original.Count; i++) for (var j = 0; j < original.Count; j++)
        {
            if (i == j || !FieldNative.Same(original[i].End, original[j].Start)) continue;
            var read = Curve.Join(original[i], original[j]);
            var shared = new Relation(spans[i].To, spans[j].From);
            Relation Fact(bool? value) => value is null || !tangents ? StructuralConstraint.Open() : value.Value ? S(shared) : S();
            var smooth = Fact(read.Smooth); var cusp = Fact(read.Cusp); var corner = Fact(read.Corner);
            var surface = new Relation(new Relation(spans[i].Role, spans[j].Role), new Relation(shared,
                new Relation(smooth, new Relation(cusp, corner))));
            joins.Add(new(spans[i], spans[j], smooth, cusp, corner, surface));
        }
        var inside = Interior(frame, interior ? source.InteriorSide : null);
        var root = new Relation(new Relation(frame.Surface, new Relation(line, curve)), new Relation(S(spans.Select(item => (IElement)item.Surface).ToArray()),
            new Relation(S(joins.Select(item => (IElement)item.Surface).ToArray()), inside)));
        return new(frame, line, curve, spans, joins, inside, root);
    }

    public static Scene Propose(Message message, int mode, bool bridge = false)
    {
        var f = message.Frame; var sections = new List<Relation>();
        foreach (var span in message.Spans)
        {
            f.Vector(span.Start, out var start); f.Vector(span.End, out var end);
            var trace = TraceStraight(message, span);
            var curved = trace == false;
            if (mode == 0 || mode == 1 && trace == true)
            { sections.Add(f.Line(span.From, span.To, span.Start, span.End)); continue; }
            var d0 = Direction(span.StartHeading, f); var d1 = Direction(span.EndHeading, f);
            var chord = end - start; var unitChord = new Curve.Vector(chord.X.Sign, chord.Y.Sign);
            if (d0.Zero) d0 = unitChord; if (d1.Zero) d1 = unitChord;
            var h = mode == 1 ? 2 : 4;
            var a = new Curve.Vector(start.X + h * d0.X, start.Y + h * d0.Y);
            var b = new Curve.Vector(end.X - h * d1.X, end.Y - h * d1.Y);
            if (!curved && mode == 2 || curved && !Closed(span.StartHeading) && !Closed(span.EndHeading))
            {
                // An explicit bowed alternative to the straight interpretation.
                var normal = new Curve.Vector(-chord.Y.Sign, chord.X.Sign);
                a += new Curve.Vector(h * normal.X, h * normal.Y); b += new Curve.Vector(h * normal.X, h * normal.Y);
            }
            sections.Add(f.Cubic(span.From, span.To, span.Start, f.Word((int)a.X, (int)a.Y), f.Word((int)b.X, (int)b.Y), span.End));
        }
        var assumed = new List<IElement>();
        if (bridge && message.Spans.Count != 0)
        {
            var incoming = message.Spans[^1]; var outgoing = message.Spans[0];
            if (!FieldNative.Same(incoming.To, outgoing.From))
            {
                f.Vector(incoming.End, out var a); f.Vector(outgoing.Start, out var b);
                var d0 = Direction(incoming.EndHeading, f); var d1 = Direction(outgoing.StartHeading, f);
                Relation Word(Curve.Vector v) => f.Word((int)v.X, (int)v.Y);
                var added = f.Cubic(incoming.To, outgoing.From, incoming.End,
                    Word(a + new Curve.Vector(2 * d0.X, 2 * d0.Y)), Word(b - new Curve.Vector(2 * d1.X, 2 * d1.Y)), outgoing.Start);
                sections.Add(added); assumed.Add(added);
            }
        }
        return new(f, sections, S(assumed.ToArray()), message.Interior);
    }

    public static Curve.Vector Direction(Relation heading, Curve.Frame frame) => new(
        E(heading).Contains(frame.Motion.Right, FieldNative.Participants.Instance) ? 1 : E(heading).Contains(frame.Motion.Left, FieldNative.Participants.Instance) ? -1 : 0,
        E(heading).Contains(frame.Motion.Up, FieldNative.Participants.Instance) ? 1 : E(heading).Contains(frame.Motion.Down, FieldNative.Participants.Instance) ? -1 : 0);

    public static bool? TraceStraight(Message message, Span span) =>
        span.Trace is Relation example ? message.Frame.Read(example).Straight : null;

    public sealed record Envelope(Scene Scene, Relation Evidence, int Notches, bool Complete);
    public static Envelope OuterRead(Message message)
    {
        var vertices = message.Spans.SelectMany(span => new[] { (Site: span.From, Word: span.Start), (Site: span.To, Word: span.End) })
            .DistinctBy(item => item.Site, FieldNative.Participants.Instance).Select(item => {
                message.Frame.Vector(item.Word, out var v); return (item.Site, item.Word, V: v);
            }).ToArray();
        var f = message.Frame;
        if (vertices.Length < 4) return new(new(f, []), StructuralConstraint.Open(message.Surface), 0, false);
        var top = vertices.OrderByDescending(item => item.V.Y).ThenBy(item => item.V.X).First();
        var right = vertices.OrderByDescending(item => item.V.X).ThenBy(item => item.V.Y).First();
        var bottom = vertices.OrderBy(item => item.V.Y).ThenByDescending(item => item.V.X).First();
        var left = vertices.OrderBy(item => item.V.X).ThenByDescending(item => item.V.Y).First();
        var tips = new[] { top, right, bottom, left };
        if (tips.Select(item => item.Site).Distinct(FieldNative.Participants.Instance).Count() != 4)
            return new(new(f, []), StructuralConstraint.Open(message.Surface), 0, false);
        var sections = tips.Select((tip, i) => f.Line(tip.Site, tips[(i + 1) % 4].Site, tip.Word, tips[(i + 1) % 4].Word)).ToArray();
        var envelope = new Scene(f, sections, S(sections.Cast<IElement>().ToArray()));
        var notches = message.Joins.Count(join => {
            var a = Direction(join.Incoming.EndHeading, f); var b = Direction(join.Outgoing.StartHeading, f);
            return message.InteriorSide is int side && Curve.Vector.Cross(a, b).Sign * side < 0;
        });
        var evidence = new Relation(message.Surface, new Relation(S(tips.Select(item => (IElement)new Relation(item.Site, item.Word)).ToArray()),
            new Relation(envelope.Surface, StructuralConstraint.Open())));
        return new(envelope, evidence, notches, true);
    }

    public static CurveEvidenceGallery.Drawing Drawing(Scene scene, string note = "")
    {
        CurveEvidenceGallery.Point Point(Curve.Vector v) => new((double)v.X, (double)v.Y);
        var reads = scene.Reads;
        var strokes = reads.Select(read => new CurveEvidenceGallery.Stroke(Point(read.Controls[0]), Point(read.Controls[^1]),
            read.Controls.Length > 2 ? Point(read.Controls[1]) : null,
            read.Controls.Length == 4 ? Point(read.Controls[2]) : null,
            E(scene.Assumed).Contains(read.Section, FieldNative.Participants.Instance))).ToArray();
        var joints = new List<CurveEvidenceGallery.Joint>();
        foreach (var site in reads.SelectMany(read => new[] { read.Start, read.End }).Distinct(FieldNative.Participants.Instance))
        {
            var incoming = reads.Where(read => FieldNative.Same(read.End, site)).ToArray();
            var outgoing = reads.Where(read => FieldNative.Same(read.Start, site)).ToArray();
            var position = incoming.Length != 0 ? incoming[0].Controls[^1] : outgoing[0].Controls[0];
            var mark = CurveEvidenceGallery.JoinMark.Open; var text = "Observed endpoint; continuation is not established.";
            if (incoming.Length == 1 && outgoing.Length == 1)
            {
                var join = Curve.Join(incoming[0], outgoing[0]);
                mark = join.Smooth == true ? CurveEvidenceGallery.JoinMark.Tangent : join.Cusp == true ? CurveEvidenceGallery.JoinMark.Cusp :
                    join.Corner == true ? CurveEvidenceGallery.JoinMark.Corner : CurveEvidenceGallery.JoinMark.Unresolved;
                text = "Native forward tangent read: " + mark;
            }
            else if (incoming.Length + outgoing.Length > 2)
            { mark = CurveEvidenceGallery.JoinMark.Unresolved; text = "Several incident sections; one continuation is not selected."; }
            joints.Add(new(Point(position), mark, text, incoming.Length == 1 ? Point(incoming[0].EndVector) : null,
                outgoing.Length == 1 ? Point(outgoing[0].StartVector) : null));
        }
        return new(strokes, joints, note, false); // no fill/simple-interior inference from closure
    }
}
