using System.Numerics;
using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;
using static Gojo.Experiments.QualitativeFrieze.Composition.JointNative;
using Curve = Gojo.Experiments.QualitativeExecution.QualitativeCurve;
using Scenes = Gojo.Experiments.QualitativeFrieze.Composition.CurveEvidenceScene;

namespace Gojo.Experiments.QualitativeFrieze.Composition;

/// <summary>Sources exist before publication or proposal. Recipe labels stay
/// outside receiver messages. Curves are polynomial constructions; no claim
/// of exact circles or arbitrary imported sketch recovery is made.</summary>
public static class CurveEvidenceCorpus
{
    public sealed record Case(string Id, string Name, string Note, Scenes.Scene Source,
        bool Tangents = true, bool Bends = true, bool Traces = true, bool Interior = true,
        bool OfferBridge = false, bool RequestClosure = false, bool FlowingPrior = false, bool OuterFocus = false);
    public static IReadOnlyList<Case> Cases()
    {
        var rows = new List<Case>();
        void Add(string name, string note, Scenes.Scene scene, bool tangents = true, bool bends = true,
            bool traces = true, bool interior = true, bool bridge = false, bool closeGoal = false, bool flow = false, bool outer = false) =>
            rows.Add(new($"C{rows.Count + 1:000}", name, note, scene, tangents, bends, traces, interior, bridge, closeGoal, flow, outer));
        Add("Straight despite a flowing context", "A weak preference for flowing forms is revised by explicit straightness evidence; the object label never supplies geometry.",
            Make(false, null, [(-6, 0), (6, 0)]), flow: true);
        Add("A bow without a measured bulge", "Curved is communicated, but tangent directions and bend side are withheld. Different bows remain allowed.",
            Make(false, null, [(-6, 0), (-6, 5), (6, 5), (6, 0)]), tangents: false, bends: false);
        Add("Endpoints alone leave line and curve open", "The source is bowed, but the receiver gets endpoints without trace or tangent evidence. Its sampled curve is a hypothesis.",
            Make(false, null, [(-6, 0), (-6, 5), (6, 5), (6, 0)]), tangents: false, bends: false, traces: false);
        Add("An inward curved notch", "The top section bends opposite to the selected interior orientation. Closure is not a convexity rule.",
            Make(true, 1, [(-6, -4), (6, -4)], [(6, -4), (6, 4)], [(6, 4), (3, -1), (-3, -1), (-6, 4)], [(-6, 4), (-6, -4)]));
        Add("Four smooth bows make an oval", "Four polynomial sections share tangent joins and a closed site cycle. Their circular appearance does not establish an exact circle.", Oval());
        Add("Capsule: straight runs and curved ends", "The line/curve transitions can be tangent without equal handle lengths or curvature continuity.",
            Make(true, 1, [(-5, -3), (5, -3)], [(5, -3), (9, -3), (9, 3), (5, 3)], [(5, 3), (-5, 3)], [(-5, 3), (-9, 3), (-9, -3), (-5, -3)]));
        Add("Rounded rectangle", "Eight explicit sections keep straight intervals separate from curved corners; a smooth join does not erase the section identities.",
            Make(true, 1, [(-4, -5), (4, -5)], [(4, -5), (6, -5), (6, -3)], [(6, -3), (6, 3)], [(6, 3), (6, 5), (4, 5)],
                [(4, 5), (-4, 5)], [(-4, 5), (-6, 5), (-6, 3)], [(-6, 3), (-6, -3)], [(-6, -3), (-6, -5), (-4, -5)]));
        Add("Pointed lens: corners at curved tips", "Two curved pieces close with finite-angle corners. A point is not automatically a tangent-reversal cusp.",
            Make(true, -1, [(-6, 0), (0, 6), (6, 0)], [(6, 0), (0, -6), (-6, 0)]));
        Add("A true tangent-reversal cusp", "At the right tip, the outgoing forward tangent reverses the incoming one. This differs from both smooth continuation and a finite-angle corner.",
            Make(true, 1, [(0, 0), (3, 0), (5, 4), (8, 4)], [(8, 4), (5, 4), (3, 8), (0, 8)], [(0, 8), (0, 0)]));
        Add("S connection with an inflection", "One span has opposed bend signs and a regular derivative certificate. A single convex/concave label would discard its important structure.",
            Make(false, null, [(-6, 0), (-2, 6), (2, -6), (6, 0)]));
        Add("Arch meeting a straight base", "The closed contour mixes a bow with a straight section; its two joins are corners, not accidental smoothing instructions.",
            Make(true, -1, [(-6, 0), (-6, 6), (6, 6), (6, 0)], [(6, 0), (-6, 0)]));
        Add("Two curves share a forward tangent", "The middle join is smooth even though curvature and interpolation speed are not promised to agree.",
            Make(false, null, [(-6, 0), (-3, 4), (0, 4)], [(0, 4), (3, 4), (6, 0)]));
        Add("Same middle site, different tangent", "The next curve leaves downwards instead of continuing rightwards, producing a corner at the shared landmark.",
            Make(false, null, [(-6, 0), (-3, 4), (0, 4)], [(0, 4), (0, 0), (6, 0)]));
        Add("An observed gap remains open", "A closing continuation is offered as an explicitly assumed passage. The source still has an observed gap and is never silently rewritten.", Gap(), bridge: true);
        Add("A requested closed-parent reading", "The same kind of open evidence is read with a separate completion goal. A preferred bridge is an action proposal, not newly observed truth.",
            Gap(), bridge: true, closeGoal: true);
        Add("Coincident endpoints are separate sites", "The two endpoints occupy the same word position but retain distinct identities. Coordinate coincidence does not establish closure.",
            Make(false, null, [(-6, 0), (-6, 6), (6, 6), (6, 0)], [(6, 0), (-6, 0)]));
        var crossFrame = new Curve.Frame(new QualitativeMotionFrame());
        var a = Path(crossFrame, false, [(-6, -4), (6, 4)]); var b = Path(crossFrame, false, [(-6, 4), (6, -4)]);
        Add("Crossing paths without a joint", "Two independent line paths cross in the drawing. No shared midpoint site or branch is invented.", new(crossFrame, a.Concat(b)));
        Add("Closed, with interior orientation withheld", "The oval closes, but convex/concave readings remain unbound because its interior-side premise is not communicated.", Oval(), interior: false);
        Add("A vanishing first handle", "A tangent-limit read is possible, while the current sufficient regularity certificate remains open. Unknown is not singularity proof.",
            Make(false, null, [(-6, 0), (-6, 0), (6, 4), (6, 0)]));
        Add("Collinear support can still retrace", "The controls lie on one line, but the trace can reverse within the parameter interval. Straight support alone is too coarse to promise no backtracking.",
            Make(false, null, [(-4, 0), (6, 0), (-6, 0), (4, 0)]));
        var mirrored = Oval();
        Add("Mirrored bows and interior side", "Reflection reverses signed turning and the oriented interior together, retaining the same physical convexity read.",
            new(mirrored.Frame, mirrored.Sections.Select(section => mirrored.Frame.Reflect(section)), interior: Scenes.Interior(mirrored.Frame, -1)));
        var reversed = Oval();
        Add("Reversed traversal, same oval boundary", "Section order, endpoint roles and tangent directions reverse; the interior side also reverses, preserving local convexity.",
            new(reversed.Frame, reversed.Sections.Reverse().Select(section => reversed.Frame.Reverse(section)), interior: Scenes.Interior(reversed.Frame, -1)));
        Add("R095: diamond envelope with stepped detail", "Four outer tip landmarks support a weak diamond-like envelope. The original stepped/notched boundary remains a separate detailed read, not a failed diamond.",
            EarlierMedallion(), outer: true);
        Add("Mixed flowing stem and angular lobe", "A small ornamental scaffold combines a curved stem with an angular branch. Its multiway join leaves the continuation choice explicit.", Branch());
        return rows;
    }

    private static Scenes.Scene Oval() => Make(true, 1,
        [(0, -6), (4, -6), (6, -4), (6, 0)], [(6, 0), (6, 4), (4, 6), (0, 6)],
        [(0, 6), (-4, 6), (-6, 4), (-6, 0)], [(-6, 0), (-6, -4), (-4, -6), (0, -6)]);
    private static Scenes.Scene Gap() => Make(false, null,
        [(0, -6), (4, -6), (6, -4), (6, 0)], [(6, 0), (6, 4), (4, 6), (0, 6)],
        [(0, 6), (-4, 6), (-6, 4), (-6, 0)], [(-6, 0), (-6, -3), (-5, -5), (-3, -5)]);
    private static Scenes.Scene Make(bool close, int? side, params (int X, int Y)[][] sections)
    { var f = new Curve.Frame(new QualitativeMotionFrame()); return new(f, Path(f, close, sections), interior: Scenes.Interior(f, side)); }
    private static Relation[] Path(Curve.Frame frame, bool close, params (int X, int Y)[][] sections)
    {
        var start = new Placeholder(); IElement current = start; var output = new List<Relation>();
        for (var i = 0; i < sections.Length; i++)
        {
            var points = sections[i];
            if (i > 0 && points[0] != sections[i - 1][^1]) throw new ArgumentException("An authored path must share its adjacent positions explicitly.");
            var end = close && i == sections.Length - 1 ? start : new Placeholder();
            if (close && i == sections.Length - 1 && points[^1] != sections[0][0]) throw new ArgumentException("An authored closed path must return to its original position.");
            var words = points.Select(point => frame.Word(point.X, point.Y)).ToArray();
            output.Add(words.Length == 2 ? frame.Line(current, end, words[0], words[1]) : words.Length == 3 ?
                frame.Quadratic(current, end, words[0], words[1], words[2]) : frame.Cubic(current, end, words[0], words[1], words[2], words[3]));
            current = end;
        }
        return output.ToArray();
    }
    private static Scenes.Scene Branch()
    {
        var f = new Curve.Frame(new QualitativeMotionFrame()); var a = new Placeholder(); var joint = new Placeholder();
        var b = new Placeholder(); var c = new Placeholder(); var tip = new Placeholder();
        Relation W(int x, int y) => f.Word(x, y);
        return new(f, [f.Cubic(a, joint, W(-6, -5), W(-6, 0), W(-3, 0), W(0, 0)),
            f.Cubic(joint, b, W(0, 0), W(3, 0), W(6, 0), W(6, 5)),
            f.Line(joint, c, W(0, 0), W(0, 4)), f.Line(c, tip, W(0, 4), W(3, 6))]);
    }
    private static Scenes.Scene EarlierMedallion()
    {
        var source = RelationalShapeCorpus.Cases().Single(item => item.Id == "R095").Source;
        var bindings = E(source.Bindings).Cast<Relation>().ToDictionary(row => row.Left, row => (Relation)row.Right, FieldNative.Participants.Instance);
        var reach = bindings.Values.SelectMany(E).OfType<Relation>().Select(move => move.Right).First();
        var frame = new Curve.Frame(source.Graph.Frame, reach); var sections = new List<Relation>();
        foreach (var edge in E(source.Graph.Strokes).Cast<Relation>())
        {
            QualitativeStrokeGraph.TryReadStroke(edge, out _, out var from, out var to, out _);
            if (!frame.Vector(bindings[from], out _) || !frame.Vector(bindings[to], out _))
                throw new InvalidOperationException("The R095 source needs an explicit common-reach correspondence before curve interpretation.");
            sections.Add(frame.Line(from, to, bindings[from], bindings[to]));
        }
        var area = sections.Select(frame.Read).Aggregate(BigInteger.Zero, (sum, read) => sum + Curve.Vector.Cross(read.Controls[0], read.Controls[^1]));
        return new(frame, sections, interior: Scenes.Interior(frame, area.Sign == 0 ? null : area.Sign));
    }
}
