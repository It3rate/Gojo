using System.Numerics;
using Gojo.Core;
using Gojo.Patterns;
using static Gojo.Experiments.QualitativeFrieze.Composition.JointNative;
using Curve = Gojo.Experiments.QualitativeExecution.QualitativeCurve;
using Shapes = Gojo.Experiments.QualitativeExecution.CurveShapeQualities;
using Scenes = Gojo.Experiments.QualitativeFrieze.Composition.CurveEvidenceScene;

namespace Gojo.Experiments.QualitativeFrieze.Composition;

/// <summary>
/// Provisional context read over the public endpoint scaffold. A chord-loop
/// orientation is a parent hypothesis, not a proof about the private curved
/// boundary. A constructed centre is a focus, not a medial-axis theorem.
/// Gap-angle and cross-axis extent comparisons require a selected native
/// similarity premise; their affine input remains available without it.
/// </summary>
public static class CurveContextRead
{
    public sealed record GapRead(bool? Relatable, BigInteger GapSquared, BigInteger SupportSquared,
        Relation Surface, IElement? From = null, IElement? To = null,
        Scenes.Span? Incoming = null, Scenes.Span? Outgoing = null);

    public sealed record Context(Relation Surface, int? ProposedInteriorSide, Scenes.Scene Coarse,
        Relation InteriorPremise, GapRead Gap);

    // gap = (message -> selected supporting spans) ->
    //       ((endpoint words -> tangent headings) ->
    //        (proposed passage -> positive/negative/open relatability scope)).
    // context = (message -> selected focus witness) ->
    //           (coarse scene -> (provisional interior premise -> gap witness)).
    // An assumed bridge remains in the coarse scene's assumed-section scope.
    // The observed message and its interior field are never replaced.
    public static Context Read(Scenes.Message message, bool proposeInterior, bool ridgeFocus, bool offerBridge,
        Relation? similarity = null)
    {
        var frame = message.Frame;
        var selectedSimilarity = similarity is not null && Shapes.IsSimilarityBasis(frame, similarity);
        var gap = ReadGap(message, similarity);
        var ordered = TryOrder(message, out var chain, out var closed);
        var sections = (ordered ? chain : message.Spans).Select(span =>
            frame.Line(span.From, span.To, span.Start, span.End)).ToList();
        var assumed = new List<IElement>();
        var mayClose = ordered && !closed && offerBridge && gap.Incoming is not null && gap.Outgoing is not null;
        if (mayClose)
        {
            var bridge = frame.Line(gap.From!, gap.To!, gap.Incoming!.End, gap.Outgoing!.Start);
            sections.Add(bridge); assumed.Add(bridge);
        }
        var chordOutline = new Scenes.Scene(frame, sections, S(assumed.ToArray()), message.Interior);
        int? proposed = null;
        var premise = StructuralConstraint.Open(message.Surface);
        if (proposeInterior && ordered && (closed || mayClose) &&
            TryPolygonOrientation(message, chain, out var side))
        {
            proposed = side;
            var orientation = Scenes.Interior(frame, side);
            // The open complement makes explicit that this is a chord-parent
            // interpretation. Private bulges and crossing curves are not read.
            premise = new(new Relation(message.Surface, chordOutline.Surface),
                new Relation(orientation, StructuralConstraint.Open(chordOutline.Surface)));
            chordOutline = new(frame, sections, S(assumed.ToArray()), orientation);
        }

        var coarse = chordOutline;
        Relation focus = new(message.Surface, S(sections.Cast<IElement>().ToArray()));
        if (ridgeFocus && selectedSimilarity && TryCentre(message, out var centre, out var support))
        {
            // Keep the support outline, and add one explicitly constructed
            // point-like or line-like centre at this read's existing reach.
            var all = sections.Append(centre).ToArray();
            coarse = new(frame, all, S(assumed.Append(centre).ToArray()), chordOutline.Interior);
            focus = new(new Relation(message.Surface, new Relation(similarity!, support)),
                new Relation(centre, StructuralConstraint.Open(chordOutline.Surface)));
        }
        else if (ridgeFocus)
        {
            // Missing similarity or a half-reach centre is not silently
            // supplied by a new calibration. The focus remains unresolved.
            focus = new(message.Surface, StructuralConstraint.Open(chordOutline.Surface));
        }
        var surface = new Relation(new Relation(message.Surface, focus),
            new Relation(coarse.Surface, new Relation(premise, gap.Surface)));
        return new(surface, proposed, coarse, premise, gap);
    }

    private static GapRead ReadGap(Scenes.Message message, Relation? similarity)
    {
        var basis = similarity ?? StructuralConstraint.Open(message.Frame.Surface);
        var context = new Relation(message.Surface, basis);
        if (!TryOrder(message, out var chain, out var closed) || closed || chain.Length == 0)
            return new(null, 0, 0, StructuralConstraint.Open(context));
        var incoming = chain[^1]; var outgoing = chain[0]; var frame = message.Frame;
        if (!frame.Vector(incoming.Start, out var before) || !frame.Vector(incoming.End, out var from) ||
            !frame.Vector(outgoing.Start, out var to) || !frame.Vector(outgoing.End, out var after))
            return new(null, 0, 0, StructuralConstraint.Open(context));
        var d = to - from; var a = from - before; var b = after - to;
        var gapSquared = Curve.Vector.Dot(d, d);
        var supportSquared = Curve.Vector.Dot(a, a) + Curve.Vector.Dot(b, b);
        var enter = Scenes.Direction(incoming.EndHeading, frame);
        var leave = Scenes.Direction(outgoing.StartHeading, frame);
        bool? relatable = null;
        // Squared-coordinate diagnostics can be calculated in the selected
        // affine frame. Interpreting their ratio as a scale-free gap measure,
        // or dot >= 0 as a turn bound, additionally needs this explicit
        // equal-reach/right-angle similarity premise.
        if (similarity is not null && Shapes.IsSimilarityBasis(frame, similarity) &&
            Closed(incoming.EndHeading) && Closed(outgoing.StartHeading) && !enter.Zero && !leave.Zero && !d.Zero && supportSquared > 0)
        {
            var dot = Curve.Vector.Dot(enter, leave);
            var determinant = Curve.Vector.Cross(enter, leave);
            if (dot < 0) relatable = false;
            else if (determinant.IsZero)
            {
                if (Curve.Vector.Cross(d, enter).IsZero)
                    relatable = Curve.Vector.Dot(d, enter) > 0;
                // Parallel but offset tangents can admit an S completion.
                // Failure of the ray-intersection seed leaves that case open.
            }
            else
            {
                // a + s*enter = b - u*leave. A positive s and u locates
                // intersection on the forward entering and reverse exit rays.
                var first = Curve.Vector.Cross(d, leave);
                var second = Curve.Vector.Cross(enter, d);
                relatable = first * determinant >= 0 && second * determinant >= 0;
            }
        }
        var passage = new Relation(incoming.To, outgoing.From);
        var supports = new Relation(incoming.Surface, outgoing.Surface);
        var selected = new Relation(new Relation(incoming.End, outgoing.Start),
            new Relation(incoming.EndHeading, outgoing.StartHeading));
        var receipt = new Relation(supports, selected);
        Relation verdict = relatable is null ? StructuralConstraint.Open(receipt) : relatable.Value ? S(receipt) : S();
        var surface = new Relation(new Relation(context, supports),
            new Relation(selected, new Relation(passage, verdict)));
        return new(relatable, gapSquared, supportSquared, surface, incoming.To, outgoing.From, incoming, outgoing);
    }

    private static bool TryOrder(Scenes.Message message, out Scenes.Span[] ordered, out bool closed)
    {
        ordered = []; closed = false;
        if (message.Spans.Count == 0) return false;
        var comparer = FieldNative.Participants.Instance;
        var starts = new Dictionary<IElement, Scenes.Span>(comparer);
        var ends = new Dictionary<IElement, Scenes.Span>(comparer);
        var words = new Dictionary<IElement, Curve.Vector>(comparer);
        foreach (var span in message.Spans)
        {
            if (!message.Frame.Vector(span.Start, out var a) || !message.Frame.Vector(span.End, out var b) ||
                !starts.TryAdd(span.From, span) || !ends.TryAdd(span.To, span)) return false;
            if (FieldNative.Same(span.From, span.To) && a != b) return false;
            if (words.TryGetValue(span.From, out var oldStart) && oldStart != a ||
                words.TryGetValue(span.To, out var oldEnd) && oldEnd != b) return false;
            words[span.From] = a; words[span.To] = b;
        }
        var beginnings = starts.Keys.Where(site => !ends.ContainsKey(site)).ToArray();
        var endings = ends.Keys.Where(site => !starts.ContainsKey(site)).ToArray();
        closed = beginnings.Length == 0 && endings.Length == 0;
        if (!closed && (beginnings.Length != 1 || endings.Length != 1)) return false;
        var start = closed ? message.Spans[0].From : beginnings[0];
        var current = start; var output = new List<Scenes.Span>(); var seen = new HashSet<IElement>(comparer);
        while (starts.TryGetValue(current, out var next) && seen.Add(next.Role))
        { output.Add(next); current = next.To; }
        if (output.Count != message.Spans.Count || (closed ? !FieldNative.Same(current, start) : !FieldNative.Same(current, endings[0])))
            return false;
        ordered = output.ToArray(); return true;
    }

    private static bool TryPolygonOrientation(Scenes.Message message, IReadOnlyList<Scenes.Span> chain, out int side)
    {
        side = 0;
        var points = new List<Curve.Vector>();
        foreach (var span in chain)
        { if (!message.Frame.Vector(span.Start, out var point)) return false; points.Add(point); }
        if (!FieldNative.Same(chain[^1].To, chain[0].From))
        { if (!message.Frame.Vector(chain[^1].End, out var last)) return false; points.Add(last); }
        if (points.Count < 3) return false;
        for (var i = 0; i < points.Count; i++)
        {
            var previous = points[(i + points.Count - 1) % points.Count]; var point = points[i]; var next = points[(i + 1) % points.Count];
            var incoming = point - previous; var outgoing = next - point;
            if (incoming.Zero || outgoing.Zero || Curve.Vector.Cross(incoming, outgoing).IsZero && Curve.Vector.Dot(incoming, outgoing) <= 0)
                return false;
            for (var j = i + 1; j < points.Count; j++)
            {
                if (j == i + 1 || i == 0 && j == points.Count - 1) continue;
                if (Intersect(point, next, points[j], points[(j + 1) % points.Count])) return false;
            }
        }
        var area = points.Select((point, i) => Curve.Vector.Cross(point, points[(i + 1) % points.Count]))
            .Aggregate(BigInteger.Zero, (sum, term) => sum + term);
        if (area.IsZero) return false;
        side = area.Sign; return true;
    }

    private static bool Intersect(Curve.Vector a, Curve.Vector b, Curve.Vector c, Curve.Vector d)
    {
        var ac = Curve.Vector.Cross(b - a, c - a); var ad = Curve.Vector.Cross(b - a, d - a);
        var ca = Curve.Vector.Cross(d - c, a - c); var cb = Curve.Vector.Cross(d - c, b - c);
        if (ac.Sign * ad.Sign < 0 && ca.Sign * cb.Sign < 0) return true;
        return ac.IsZero && Between(a, b, c) || ad.IsZero && Between(a, b, d) ||
            ca.IsZero && Between(c, d, a) || cb.IsZero && Between(c, d, b);
    }

    private static bool Between(Curve.Vector a, Curve.Vector b, Curve.Vector p) =>
        p.X >= BigInteger.Min(a.X, b.X) && p.X <= BigInteger.Max(a.X, b.X) &&
        p.Y >= BigInteger.Min(a.Y, b.Y) && p.Y <= BigInteger.Max(a.Y, b.Y);

    private static bool TryCentre(Scenes.Message message, out Relation centre, out Relation support)
    {
        centre = StructuralConstraint.Open(message.Surface); support = StructuralConstraint.Open(message.Surface);
        var landmarks = new List<(IElement Site, Relation Word, Curve.Vector Value)>();
        foreach (var span in message.Spans)
        {
            if (!message.Frame.Vector(span.Start, out var a) || !message.Frame.Vector(span.End, out var b)) return false;
            landmarks.Add((span.From, span.Start, a)); landmarks.Add((span.To, span.End, b));
        }
        if (landmarks.Count == 0) return false;
        var left = landmarks.MinBy(item => item.Value.X); var right = landmarks.MaxBy(item => item.Value.X);
        var bottom = landmarks.MinBy(item => item.Value.Y); var top = landmarks.MaxBy(item => item.Value.Y);
        var width = right.Value.X - left.Value.X; var height = top.Value.Y - bottom.Value.Y;
        var sx = right.Value.X + left.Value.X; var sy = top.Value.Y + bottom.Value.Y;
        if (width <= 0 || height <= 0 || !sx.IsEven || !sy.IsEven) return false;
        var cx = sx / 2; var cy = sy / 2;
        var from = new Curve.Vector(cx, cy); var to = from;
        if (width >= 2 * height) { from = new(left.Value.X, cy); to = new(right.Value.X, cy); }
        else if (height >= 2 * width) { from = new(cx, bottom.Value.Y); to = new(cx, top.Value.Y); }
        if (from.X < int.MinValue || from.X > int.MaxValue || from.Y < int.MinValue || from.Y > int.MaxValue ||
            to.X < int.MinValue || to.X > int.MaxValue || to.Y < int.MinValue || to.Y > int.MaxValue) return false;
        var f = message.Frame;
        centre = f.Line(new Placeholder(), new Placeholder(), f.Word((int)from.X, (int)from.Y), f.Word((int)to.X, (int)to.Y));
        support = S(new[] { left, right, bottom, top }.Select(item =>
            (IElement)new Relation(item.Site, item.Word)).ToArray());
        return true;
    }
}
