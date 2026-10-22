using Gojo.Core;
using Gojo.Experiments.QualitativeExecution;
using Curve = Gojo.Experiments.QualitativeExecution.QualitativeCurve;
using Scenes = Gojo.Experiments.QualitativeFrieze.Composition.CurveEvidenceScene;

namespace Gojo.Experiments.QualitativeFrieze.Composition;

/// <summary>
/// Authored sources precede publication and candidate construction. Integer
/// pairs select repeated participation in a common, unmeasured reach; they
/// are not image coordinates or physical measurements. Names and experiment
/// switches describe the fixture; sections, sites and interpolation controls
/// carry the geometry. No source is claimed to be an exact circular arc.
/// </summary>
public static class CurveRefinementCorpus
{
    public sealed record Case(string Id, string Name, string Note, Scenes.Scene Source,
        bool Profiles = true, bool ProvisionalInterior = false, bool RidgeFocus = false,
        bool OfferBridge = false, bool RequestClosure = false, bool Control = false,
        bool Similarity = true, bool Tangents = true, bool Bends = true,
        bool Traces = true, bool Interior = true, bool FlowingPrior = false, bool OuterFocus = false);

    public static IReadOnlyList<Case> Cases()
    {
        var rows = CurveEvidenceCorpus.Cases().Select(row => new Case(row.Id, row.Name, row.Note, row.Source,
            Profiles: row.Traces, ProvisionalInterior: row.Id == "C018", OfferBridge: row.OfferBridge,
            RequestClosure: row.RequestClosure, Tangents: row.Tangents, Bends: row.Bends,
            Traces: row.Traces, Interior: row.Interior, FlowingPrior: row.FlowingPrior, OuterFocus: row.OuterFocus)).ToList();
        void Add(string name, string note, Scenes.Scene scene, bool profiles = true,
            bool provisional = false, bool ridge = false, bool bridge = false, bool closure = false,
            bool control = false, bool similarity = true, bool tangents = true, bool bends = true,
            bool traces = true, bool interior = true) => rows.Add(new($"C{rows.Count + 1:000}", name, note,
                scene, profiles, provisional, ridge, bridge, closure, control, similarity,
                tangents, bends, traces, interior));

        // C025-C042: fixed endpoint scaffold and endpoint directions, varying
        // relative bow and the balance between its two endpoint handles.
        foreach (var reflected in new[] { false, true })
        foreach (var extent in new[] { 3, 5, 7 })
        foreach (var balance in new[] { 0, -1, 1 })
        {
            var first = extent + balance; var second = extent - balance;
            var recipe = new[] { new[] { (0, 0), (0, first), (9 - second, 9), (9, 9) } };
            if (reflected) recipe = Map(recipe, (x, y) => (x, -y));
            var fullness = extent == 3 ? "Inside the reference-like bow" : extent == 5 ? "Reference-like quarter bow" : "Outside the reference-like bow";
            var equality = balance == 0 ? "balanced" : balance < 0 ? "longer exit handle" : "longer entrance handle";
            Add($"{fullness}: {equality}{(reflected ? ", reflected" : "")}",
                "The two endpoint directions remain perpendicular. Relative extent and endpoint balance change independently; the middle template is only circle-like, not a circle certificate.",
                Make(false, null, recipe));
        }

        // C043-C054: fullness should survive composition into oval boundaries
        // and capsule ends, including two actual sections at each end.
        foreach (var extent in new[] { 3, 5, 7 })
        foreach (var horizontal in new[] { 9, 18 })
            Add($"{(horizontal == 9 ? "Round" : "Elongated")} four-bow boundary, {ExtentName(extent)}",
                "Shared tangent directions and closure are held fixed while relative bow changes. Similarity of the four sections is inspectable without claiming exact circularity.",
                Oval(horizontal, 9, extent));
        foreach (var extent in new[] { 3, 5, 7 })
        foreach (var run in new[] { 4, 10 })
            Add($"Capsule with {(run == 4 ? "short" : "long")} straight runs, {ExtentName(extent)} ends",
                "Each end consists of two quarter-turn polynomial sections. A flat, reference-like or full end is not determined by tangency alone.",
                Capsule(run, 9, extent));

        // C055-C066: signed participation, force, equal-axis tangency and
        // biased tangency are distinct even when the coarse headings agree.
        foreach (var strength in new[] { 2, 5, 9 })
        foreach (var form in new[] { 0, 1, 2, 3 })
        {
            (int, int)[][] recipe = form switch
            {
                0 => [[(-9, 0), (-3, strength), (3, -strength), (9, 0)]],
                1 => [[(-9, 0), (-3, -strength), (3, strength), (9, 0)]],
                2 => [[(-9, 0), (-9 + strength, strength), (9 - strength, -strength), (9, 0)]],
                _ => [[(-9, 0), (-9 + strength, 2 * strength), (9 - strength, -2 * strength), (9, 0)]]
            };
            var direction = form == 0 ? "alternating bow" : form == 1 ? "opposed alternating bow" : form == 2 ? "equal-axis endpoint tangents" : "vertical-biased endpoint tangents";
            Add($"S connection: {StrengthName(strength)}, {direction}",
                "Opposed bending is retained as one span. The equal-axis and biased tangent cases share a quadrant but differ in relative directional participation.",
                Make(false, null, recipe));
        }

        // C067-C078: equal versus unequal handles do not determine whether a
        // join is smooth. Conversely a smooth join does not establish balance.
        foreach (var kind in new[] { 0, 1, 2, 3 })
        foreach (var pair in new[] { (5, 5), (2, 7), (7, 2) })
        {
            var (first, second) = pair;
            (int, int)[] left = [(-9, -9), (-9, -9 + first), (-first, 0), (0, 0)];
            (int, int)[] right = kind switch
            {
                2 => [(0, 0), (0, -second), (9 - second, -9), (9, -9)],
                3 => [(0, 0), (-second, 0), (9, -9 + second), (9, -9)],
                _ => [(0, 0), (second, 0), (9, -9 + second), (9, -9)]
            };
            var recipe = new[] { left, right };
            if (kind == 1) recipe = Map(recipe, (x, y) => (x - y, x + y));
            var joint = kind == 0 ? "horizontal tangent join" : kind == 1 ? "equal-axis tangent join" : kind == 2 ? "finite-angle corner" : "tangent reversal cusp";
            Add($"{joint}: {(first == second ? "balanced neighbours" : first < second ? "exit-heavy neighbours" : "entrance-heavy neighbours")}",
                "Both sections share an actual site. The direction and relative extent at the join are separate reads; a tangent reversal is not merely an unbalanced smooth join.",
                Make(false, null, recipe));
        }

        // C079-C090: retain the open source. Completion and a coarse interior
        // context are separately supplied, revisable assumptions.
        foreach (var gap in new[] { 2, 5, 8 })
        foreach (var arrangement in new[] { 0, 1, 2, 3 })
        {
            var name = arrangement == 0 ? "aligned continuation" : arrangement == 1 ? "descending entrance" : arrangement == 2 ? "ascending entrance" : "opposed exit direction";
            Add($"Open parent boundary: {(gap == 2 ? "small" : gap == 5 ? "middle" : "large")} gap, {name}",
                "The missing span is absent from the source. Tangent arrangement and gap relative to the parent change together only where authored; a closing sample must remain explicitly assumed.",
                GappedOval(gap, arrangement), provisional: true, bridge: true,
                closure: arrangement is 0 or 1, interior: false);
        }

        // C091-C098: identical visible boundaries may be read as enclosure or
        // as the outline of a thick point/line-like object. Focus is not a new
        // geometric fact and must be connected by the receiving experiment.
        foreach (var elongated in new[] { false, true })
        foreach (var rounded in new[] { false, true })
        foreach (var ridge in new[] { false, true })
            Add($"{(rounded ? "Rounded " : "")}{(elongated ? "rectangle" : "square")} as {(ridge ? "thick centre structure" : "enclosed area")}",
                ridge ? "The same boundary is offered with a centre/ridge focus: a compact square supports a point-like centre, an elongated outline supports a line-like centre. This is a contextual read, not a discovered medial axis." :
                    "The closed outline is offered as enclosure evidence. Its paired case has the same section recipe with a different read focus, without changing boundary identity rules.",
                Box(elongated ? 18 : 9, elongated ? 5 : 9, rounded), ridge: ridge);

        // C099-C106: closure does not certify convexity, and a reversed
        // control edge is not sufficient evidence that a curve retraces.
        foreach (var depth in new[] { 2, 5, 8 })
            Add($"Closed outline with {(depth == 2 ? "shallow" : depth == 5 ? "middle" : "deep")} inward bow",
                "The top boundary dips into the selected interior. Its concavity must survive any broader closed-parent or convex-default interpretation.",
                Make(true, 1, [(-9, -6), (9, -6)], [(9, -6), (9, 6)],
                    [(9, 6), (3, 6 - depth), (-3, 6 - depth), (-9, 6)], [(-9, 6), (-9, -6)]), provisional: true);
        Add("Closed outline with an outward bow", "The endpoint scaffold matches the concave family, but the extra participation is outside the selected interior. A prior is not a substitute for signed bend evidence.",
            Make(true, 1, [(-9, -6), (9, -6)], [(9, -6), (9, 6)],
                [(9, 6), (3, 12), (-3, 12), (-9, 6)], [(-9, 6), (-9, -6)]), provisional: true);
        Add("Collinear interior retracing", "The section has straight support yet reverses within the parameter interval. A no-retrace preference needs progress evidence in addition to straightness.",
            Make(false, null, [(-9, 0), (15, 0), (-15, 0), (9, 0)]));
        Add("Reversed control edge, forward trace", "One control-polygon edge points backwards while the polynomial trace still moves forward. Penalizing every reversed control edge would produce a false rejection.",
            Make(false, null, [(-9, 0), (4, 0), (-4, 0), (9, 0)]));
        Add("Vanishing handles, monotone support", "The first and last handles vanish. A limiting tangent remains meaningful; a sufficient regularity test may stay unresolved without proving retracing.",
            Make(false, null, [(-9, 0), (-9, 0), (9, 0), (9, 0)]));
        Add("Collinear overshoots at both ends", "Both endpoint tangents oppose the endpoint chord, so a simple forward line and this authored trace need different progress evidence.",
            Make(false, null, [(-9, 0), (-15, 0), (15, 0), (9, 0)]));

        // C107-C112: source geometry is fixed while public information or a
        // revisable similarity default is withheld. These are explicit
        // controls, rather than new evidence generated by a changed label.
        Add("Control: oval without profile publication", "The source retains the reference-like bow while the receiver has only the earlier trace, heading and bend vocabulary.",
            Oval(9, 9, 5), profiles: false, control: true);
        Add("Control: oval without similarity premise", "Profile publication is requested, but directional balance and excursion bands remain open without the selected similarity premise. Native endpoint topology and affine bend evidence remain available.",
            Oval(9, 9, 5), similarity: false, control: true);
        Add("Control: oval without profiles or similarity", "Both optional channels are withheld from the same underlying oval recipe.",
            Oval(9, 9, 5), profiles: false, similarity: false, control: true);
        Add("Control: profile with tangent evidence withheld", "The profile channel is exposed independently while explicit endpoint-heading and join facts are withheld.",
            Oval(9, 9, 5), tangents: false, control: true);
        Add("Control: endpoints alone", "Trace type, heading, bend, interior, profile and similarity information are all omitted. A shape-looking completion is a receiver hypothesis.",
            Oval(9, 9, 5), profiles: false, similarity: false, tangents: false, bends: false, traces: false, interior: false, control: true);
        Add("Control: profile and tangent, without bend or trace type", "Two optional evidence streams are retained while explicit signed-bend and straight/curved-type channels remain open.",
            Oval(9, 9, 5), bends: false, traces: false, control: true);
        return rows;
    }

    private static string ExtentName(int extent) => extent == 3 ? "under-filled" : extent == 5 ? "reference-like" : "full";
    private static string StrengthName(int strength) => strength == 2 ? "weak" : strength == 5 ? "middle force" : "strong";

    private static Scenes.Scene Oval(int horizontal, int vertical, int extent)
    {
        var hx = extent * horizontal / 9; var hy = extent * vertical / 9;
        return Make(true, 1,
            [(0, -vertical), (hx, -vertical), (horizontal, -hy), (horizontal, 0)],
            [(horizontal, 0), (horizontal, hy), (hx, vertical), (0, vertical)],
            [(0, vertical), (-hx, vertical), (-horizontal, hy), (-horizontal, 0)],
            [(-horizontal, 0), (-horizontal, -hy), (-hx, -vertical), (0, -vertical)]);
    }

    private static Scenes.Scene Capsule(int run, int radius, int extent) => Make(true, 1,
        [(-run, -radius), (run, -radius)],
        [(run, -radius), (run + extent, -radius), (run + radius, -extent), (run + radius, 0)],
        [(run + radius, 0), (run + radius, extent), (run + extent, radius), (run, radius)],
        [(run, radius), (-run, radius)],
        [(-run, radius), (-run - extent, radius), (-run - radius, extent), (-run - radius, 0)],
        [(-run - radius, 0), (-run - radius, -extent), (-run - extent, -radius), (-run, -radius)]);

    private static Scenes.Scene GappedOval(int gap, int arrangement)
    {
        var firstHandle = arrangement == 3 ? -5 : 5;
        var lastHandleHeight = arrangement == 1 ? -7 : arrangement == 2 ? -11 : -9;
        return Make(false, null,
            [(0, -9), (firstHandle, -9), (9, -5), (9, 0)],
            [(9, 0), (9, 5), (5, 9), (0, 9)],
            [(0, 9), (-5, 9), (-9, 5), (-9, 0)],
            [(-9, 0), (-9, -5), (-gap - 2, lastHandleHeight), (-gap, -9)]);
    }

    private static Scenes.Scene Box(int horizontal, int vertical, bool rounded)
    {
        if (!rounded) return Make(true, 1,
            [(-horizontal, -vertical), (horizontal, -vertical)], [(horizontal, -vertical), (horizontal, vertical)],
            [(horizontal, vertical), (-horizontal, vertical)], [(-horizontal, vertical), (-horizontal, -vertical)]);
        const int corner = 3;
        return Make(true, 1,
            [(-horizontal + corner, -vertical), (horizontal - corner, -vertical)],
            [(horizontal - corner, -vertical), (horizontal, -vertical), (horizontal, -vertical + corner)],
            [(horizontal, -vertical + corner), (horizontal, vertical - corner)],
            [(horizontal, vertical - corner), (horizontal, vertical), (horizontal - corner, vertical)],
            [(horizontal - corner, vertical), (-horizontal + corner, vertical)],
            [(-horizontal + corner, vertical), (-horizontal, vertical), (-horizontal, vertical - corner)],
            [(-horizontal, vertical - corner), (-horizontal, -vertical + corner)],
            [(-horizontal, -vertical + corner), (-horizontal, -vertical), (-horizontal + corner, -vertical)]);
    }

    private static (int X, int Y)[][] Map((int X, int Y)[][] sections, Func<int, int, (int X, int Y)> map) =>
        sections.Select(section => section.Select(point => map(point.X, point.Y)).ToArray()).ToArray();

    private static Scenes.Scene Make(bool close, int? side, params (int X, int Y)[][] sections)
    {
        var frame = new Curve.Frame(new QualitativeMotionFrame());
        var start = new Placeholder(); IElement current = start; var output = new List<Relation>();
        for (var i = 0; i < sections.Length; i++)
        {
            var points = sections[i];
            if (i > 0 && points[0] != sections[i - 1][^1])
                throw new ArgumentException("An authored path must share its adjacent positions explicitly.");
            var end = close && i == sections.Length - 1 ? start : new Placeholder();
            if (close && i == sections.Length - 1 && points[^1] != sections[0][0])
                throw new ArgumentException("An authored closed path must return to its original position.");
            var words = points.Select(point => frame.Word(point.X, point.Y)).ToArray();
            output.Add(words.Length switch
            {
                2 => frame.Line(current, end, words[0], words[1]),
                3 => frame.Quadratic(current, end, words[0], words[1], words[2]),
                4 => frame.Cubic(current, end, words[0], words[1], words[2], words[3]),
                _ => throw new ArgumentException("The fixture supports line, quadratic and cubic sections.")
            });
            current = end;
        }
        return new(frame, output, interior: Scenes.Interior(frame, side));
    }
}
