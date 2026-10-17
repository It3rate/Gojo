using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;
using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;

namespace Gojo.Experiments.QualitativeFrieze;

public static class SquareCellExperiment
{
    public sealed record Case(string Name, QualitativeTilePatch Patch, QualitativeSquareStudy.Deformation Deformation,
        IReadOnlyDictionary<IElement, long> Calibration);
    public sealed record Metric(long XX, long XY, long YY, long Determinant, bool QuarterTurnIsEuclideanIsometry);
    public sealed record Examples(QualitativeSquareStudy Study, IReadOnlyList<Case> Cases);
    private sealed record Line(long X1, long Y1, long X2, long Y2, string Colour);
    private sealed record View(Case Source, Metric Metric, List<Line> Lines, Relation Projection, int NativeElements, int NumericElements,
        bool MapPassed, QualitativeSquareStudy.Symmetry NativeQuarter, QualitativeSquareStudy.Symmetry NativeMirror);

    public static Examples Create()
    {
        var study = new QualitativeSquareStudy(); var r = study.DirectionTurn; var m = study.DirectionMirror; var v = study.OtherDirectionMirror;
        var cyclic = study.Generate("Four-turn hook", S(), S(r), S(r, r), S(r, r, r));
        var mirror = study.Generate("Turn + mirror hook", S(), S(r), S(r, r), S(r, r, r), S(m), S(r, m), S(r, r, m), S(r, r, r, m));
        // V is derived from the exposed R/M generators. Preserve the actual
        // execution word rather than inventing an unrelated site-map dispatch.
        var flips = study.Generate("Two independent flips", S(), S(m), S(r, r, m), S(m, r, r, m));
        var cPatch = new QualitativeTilePatch(study, cyclic, 6, 2);
        var dPatch = new QualitativeTilePatch(study, mirror, 6, 2);
        var fPatch = new QualitativeTilePatch(study, flips, 6, 2);
        var cases = new List<Case>();
        Add("Four-turn field / square view", cyclic, cPatch, study.Reach, study.Reach, null, true, 3, 3, 0);
        Add("Turn + mirror field / square view", mirror, dPatch, study.Reach, study.Reach, null, true, 3, 3, 0);
        Add("Two-flip field / square view", flips, fPatch, study.Reach, study.Reach, null, true, 3, 3, 0);
        Add("Explicit horizontal stretch", cyclic, cPatch, new Placeholder(), study.Reach, null, true, 6, 3, 0);
        var q = new Placeholder();
        var lean = Add("Right lean / first realization", cyclic, cPatch, study.Reach, study.Reach, q, true, 3, 3, 1);
        cases.Add(new("Same unmeasured lean / second realization", cPatch, lean.Deformation,
            new Dictionary<IElement, long>(ReferenceEqualityComparer.Instance) { [study.Reach] = 3, [q] = 2 }));
        Add("Left lean / separate direction promise", cyclic, cPatch, study.Reach, study.Reach, q, false, 3, 3, 1);
        Add("Turn + mirror field / right lean", mirror, dPatch, study.Reach, study.Reach, q, true, 3, 3, 1);
        return new(study, cases.AsReadOnly());
        Case Add(string name, QualitativeSquareStudy.Pattern pattern, QualitativeTilePatch patch, IElement horizontal, IElement vertical,
            IElement? shear, bool forward, long h, long vv, long qq)
        {
            var target = new QualitativeMotionFrame();
            Relation T(IElement direction, IElement reach) => QualitativeMotionFrame.Move(direction, reach);
            Relation Up() => shear is null ? S(T(target.Up, vertical)) : S(T(forward ? target.Right : target.Left, shear), T(target.Up, vertical));
            Relation Down() => shear is null ? S(T(target.Down, vertical)) : S(T(forward ? target.Left : target.Right, shear), T(target.Down, vertical));
            var transform = new QualitativePatternTransform(study.Frame, target, S(
                new Relation(T(study.Frame.Right, study.Reach), S(T(target.Right, horizontal))),
                new Relation(T(study.Frame.Left, study.Reach), S(T(target.Left, horizontal))),
                new Relation(T(study.Frame.Up, study.Reach), Up()), new Relation(T(study.Frame.Down, study.Reach), Down())));
            var calibration = new Dictionary<IElement, long>(ReferenceEqualityComparer.Instance) { [horizontal] = h, [vertical] = vv };
            if (shear is not null) calibration[shear] = qq;
            var item = new Case(name, patch, study.Deform(pattern, transform), calibration); cases.Add(item); return item;
        }
    }

    /// <summary>Optional ordinary Euclidean view of the transported basis.
    /// This exact integer diagnostic is downstream of the native construction.</summary>
    public static Metric ObserveMetric(QualitativeSquareStudy study, Case example)
    {
        var transform = example.Deformation.Transform;
        var aRead = transform.Apply(S(QualitativeMotionFrame.Move(study.Frame.Right, study.Reach)));
        var bRead = transform.Apply(S(QualitativeMotionFrame.Move(study.Frame.Up, study.Reach)));
        if (!transform.TryReadComplete(aRead, out var aWord) || !transform.TryReadComplete(bRead, out var bWord)) throw new InvalidOperationException("Unknown basis projection.");
        var a = Position(transform.Target, aWord, example.Calibration); var b = Position(transform.Target, bWord, example.Calibration);
        checked { var xx = a.X * a.X + a.Y * a.Y; var xy = a.X * b.X + a.Y * b.Y; var yy = b.X * b.X + b.Y * b.Y;
            return new(xx, xy, yy, xx * yy - xy * xy, xx == yy && xy == 0 && xx > 0); }
    }

    public static int Run(string[] args)
    {
        var output = Path.Combine("experiments", "QualitativeFrieze", "results", "square-cell");
        if (args.Length == 2 && args[0] == "--output") output = args[1];
        else if (args.Length != 0) throw new ArgumentException("Usage: --square-cell [--output DIRECTORY]");
        Directory.CreateDirectory(output);
        var examples = Create(); var study = examples.Study;
        var views = examples.Cases.Select(example => Observe(study, example)).ToArray();
        var height = 175 + 290 * ((views.Length + 1) / 2);
        var svg = new StringBuilder($"<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"1460\" height=\"{height}\" viewBox=\"0 0 1460 {height}\"><style>text{{font-family:Segoe UI,Arial,sans-serif}}.title{{font-size:27px;font-weight:650}}.name{{font-size:18px;font-weight:600}}.note{{font-size:14px;fill:#52616a}}.ink{{fill:none;stroke-width:4;stroke-linecap:square;stroke-linejoin:miter}}.seam{{fill:none;stroke:#8d9b9d;stroke-width:1;stroke-dasharray:4 5}}</style><rect width=\"100%\" height=\"100%\" fill=\"#f6f2e9\"/><text x=\"36\" y=\"43\" class=\"title\">One qualitative cell, two periodic actions</text><text x=\"36\" y=\"76\" class=\"note\">Shared reaches · contour / axis correspondence · actual seam joins · independent two-direction repetition</text><text x=\"36\" y=\"102\" class=\"note\">Eight optional views of three native fields. Dashed cell guides are observations, not additional ink.</text><text x=\"36\" y=\"128\" class=\"note\">Cyclic turns and commuting flips differ. Affine projection preserves correspondence; Euclidean symmetry is a stronger read.</text>");
        for (var i = 0; i < views.Length; i++) svg.Append(Panel(study, views[i], i % 2 * 730, 155 + i / 2 * 290));
        svg.Append("</svg>"); File.WriteAllText(Path.Combine(output, "gallery.svg"), svg.ToString());
        var mechanics = Mechanics(study); File.WriteAllText(Path.Combine(output, "mechanics.svg"), mechanics);
        var table = string.Join("", study.Laws.Select(law => $"<tr><td>{H(law.Name)}</td><td>{(law.Passed is null ? "unresolved" : law.Passed.Value ? "equal" : "different")}</td></tr>"));
        var casesTable = string.Join("", views.Select(view => $"<tr><td>{H(view.Source.Name)}</td><td>{view.MapPassed}</td><td>{view.NativeQuarter.Preserves}</td><td>{view.NativeMirror.Preserves}</td><td>{view.Metric.QuarterTurnIsEuclideanIsometry}</td><td>{view.Metric.XX}, {view.Metric.XY}, {view.Metric.YY}</td></tr>"));
        File.WriteAllText(Path.Combine(output, "gallery.html"), "<!doctype html><html><head><meta charset=\"utf-8\"><title>Gojo qualitative square cell</title><style>body{max-width:1460px;margin:22px auto;background:#f6f2e9;color:#243640;font:16px Segoe UI,Arial,sans-serif}svg{width:100%;height:auto}button{padding:9px 15px;margin-right:8px}table{border-collapse:collapse;width:100%;margin:20px 0}td,th{padding:8px;border-bottom:1px solid #bbc5c3;text-align:left}.hide-seams .seam{display:none}.mono .ink{stroke:#17313e!important}.invert svg>rect{fill:#243640}.invert svg text{fill:#eee}.invert .ink{stroke:#f6f2e9!important}</style></head><body><p><button onclick=\"document.body.classList.toggle('hide-seams')\">Cell guides</button><button onclick=\"document.body.classList.toggle('mono')\">Orbit phase colours</button><button onclick=\"document.body.classList.toggle('invert')\">Invert ink</button></p>" + svg +
            "<p>Each field is an actual 6 × 2 native patch. Boundary participants are joined using the exposed seam rows before any numeric coordinate is read. Colour marks the orbit phase; mirror partners share colours. Coincident orbit copies retain distinct provenance.</p><table><tr><th>Finite action question</th><th>Native result</th></tr>" + table + "</table><table><tr><th>View</th><th>Cell structural map</th><th>Native motif quarter-turn</th><th>Native motif mirror</th><th>Basis quarter-turn is a Euclidean isometry</th><th>Basis Gram entries xx, xy, yy</th></tr>" + casesTable +
            "</table><p>Native motif invariance is checked separately from the basis metric. For example the two-flip field has an ordinary square view, but its directed motif is not invariant under the source quarter-turn.</p><details><summary>Contour, axis and seam mechanics</summary>" + mechanics + "</details><p>A marked contour without an equal-turn promise still closes and has a successor correspondence. Its equal-angle question remains unresolved. Shared radial reach is a declared construction; spontaneous radial tension and a continuous circle are not demonstrated here.</p><p>Stretch and shear are explicit transformations, rather than refinements that preserve every square metric promise. The conjugated four-step action retains its abstract order. A compatible transported metric can preserve its intrinsic geometry, but that metric is not supplied automatically.</p><p>The finite seam patch and local action relations do not certify every infinite wallpaper-group embedding. No all-affine solver or general wallpaper classifier is claimed.</p></body></html>");
        File.WriteAllText(Path.Combine(output, "diagnostics.json"), JsonSerializer.Serialize(new {
            Scope = "Carrier-free native construction; selected equal-turn promises; finite cell and patch certificates; explicit affine projection; optional integer Euclidean view.",
            Laws = study.Laws.Select(law => new { law.Name, Equal = law.Passed }),
            ClosedContour = study.Contour.TryReadClosedContour(study.Contour.Evaluate(), out _),
            EqualTurns = study.Contour.TryReadEqualTurns(study.Contour.Evaluate(), out _),
            RawContourEqualTurns = study.UnequalOrUnknownTurns.TryReadEqualTurns(study.UnequalOrUnknownTurns.Evaluate(), out _),
            CellPassed = study.Cell.TryReadPassed(study.CellRead),
            Views = views.Select(view => new { view.Source.Name, NativeCellStrokeCopies = E(view.Source.Deformation.Graph.Strokes).Length,
                PatchStrokeCopies = view.Lines.Count, SharedPatchSites = E(view.Source.Patch.Pattern.Graph.Sites).Length,
                view.NativeElements, view.NumericElements, view.MapPassed, NativeQuarter = view.NativeQuarter.Preserves, NativeMirror = view.NativeMirror.Preserves, view.Metric,
                Pending = E(view.Source.Deformation.Unresolved).Length })
        }, new JsonSerializerOptions { WriteIndented = true }));
        foreach (var view in views) Console.WriteLine($"{view.Source.Name}: {view.Lines.Count} stroke copies; cell map {view.MapPassed}; Euclidean quarter-turn {view.Metric.QuarterTurnIsEuclideanIsometry}; {view.NumericElements} native numeric elements.");
        Console.WriteLine($"Wrote {Path.GetFullPath(output)}.");
        return views.All(view => view.MapPassed && view.NumericElements == 0 && E(view.Source.Deformation.Unresolved).Length == 0) ? 0 : 1;
    }

    private static View Observe(QualitativeSquareStudy study, Case example)
    {
        var transform = example.Deformation.Transform; var points = new Dictionary<IElement, (long X, long Y)>(ReferenceEqualityComparer.Instance);
        var reads = new List<IElement>(); var lines = new List<Line>();
        foreach (var binding in E(example.Patch.Pattern.Bindings).Cast<Relation>())
        {
            var read = transform.Apply((Relation)binding.Right); if (!transform.TryReadComplete(read, out var output)) throw new InvalidOperationException("Unknown projected patch binding.");
            points.Add(binding.Left, Position(transform.Target, output, example.Calibration)); reads.Add(new Relation(binding, read));
        }
        var index = 0;
        foreach (var stroke in E(example.Patch.Pattern.Graph.Strokes).Cast<Relation>())
        {
            QualitativeStrokeGraph.TryReadStroke(stroke, out _, out var from, out var to, out var word);
            var read = transform.Apply(word); if (!transform.TryReadComplete(read, out var mapped)) throw new InvalidOperationException("Unknown projected patch chord.");
            var a = points[from]; var b = points[to]; var delta = Position(transform.Target, mapped, example.Calibration);
            if (checked(a.X + delta.X) != b.X || checked(a.Y + delta.Y) != b.Y) throw new InvalidOperationException("A projection contradicted its actual seam endpoint binding.");
            var colour = new[] { "#176b7c", "#b75a2a", "#795294", "#52793e" }[(index++ / 2) % 4];
            lines.Add(new(a.X, a.Y, b.X, b.Y, colour)); reads.Add(new Relation(stroke, read));
        }
        var nativeQuarter = study.ReadSymmetry(example.Deformation.Source, study.DirectionTurn, study.SiteTurn);
        var nativeMirror = study.ReadSymmetry(example.Deformation.Source, study.DirectionMirror, study.SiteMirror);
        var projection = new Relation(new Relation(example.Patch.Surface, transform.Surface),
            new Relation(S(reads.ToArray()), new Relation(nativeQuarter.Witness, nativeMirror.Witness)));
        var native = Traverse(new Relation(projection, example.Deformation.Surface)).ToArray();
        return new(example, ObserveMetric(study, example), lines, projection, native.Length, native.Count(item => item is Carrier or CarrierLocation),
            example.Deformation.Source.Graph.TryReadMapPassed(example.Deformation.Graph, example.Deformation.MapRead), nativeQuarter, nativeMirror);
    }

    private static string Panel(QualitativeSquareStudy study, View view, int left, int top)
    {
        var example = view.Source; var transform = example.Deformation.Transform;
        (long X, long Y) Project(Relation word)
        { var read = transform.Apply(word); transform.TryReadComplete(read, out var result); return Position(transform.Target, result, example.Calibration); }
        var x = Project(study.Cell.XWord); var y = Project(study.Cell.YWord);
        var corner = Project((Relation)E(study.SiteBindings).Cast<Relation>().Single(binding => ReferenceEquals(binding.Left, E((Relation)study.Cell.Bottom.Left)[0])).Right);
        var guides = new List<Line>();
        for (var i = 0; i < 6; i++) for (var j = 0; j < 2; j++)
        {
            var a = (X: corner.X + i * x.X + j * y.X, Y: corner.Y + i * x.Y + j * y.Y);
            guides.Add(new(a.X, a.Y, a.X + x.X, a.Y + x.Y, "")); guides.Add(new(a.X, a.Y, a.X + y.X, a.Y + y.Y, ""));
            guides.Add(new(a.X + y.X, a.Y + y.Y, a.X + x.X + y.X, a.Y + x.Y + y.Y, "")); guides.Add(new(a.X + x.X, a.Y + x.Y, a.X + x.X + y.X, a.Y + x.Y + y.Y, ""));
        }
        var all = view.Lines.Concat(guides).ToArray(); var minX = all.Min(line => Math.Min(line.X1, line.X2)); var maxX = all.Max(line => Math.Max(line.X1, line.X2));
        var minY = all.Min(line => Math.Min(line.Y1, line.Y2)); var maxY = all.Max(line => Math.Max(line.Y1, line.Y2));
        var scale = Math.Min(650d / Math.Max(1, maxX - minX), 170d / Math.Max(1, maxY - minY)); var dx = left + 38 - minX * scale; var dy = top + 65 + maxY * scale;
        var visibleQuarter = view.NativeQuarter.Preserves == true && view.Metric.QuarterTurnIsEuclideanIsometry;
        var b = new StringBuilder($"<g><text x=\"{left + 36}\" y=\"{top + 10}\" class=\"name\">{H(example.Name)}</text><text x=\"{left + 36}\" y=\"{top + 36}\" class=\"note\">Structural map: {(view.MapPassed ? "accepted" : "pending")} · Quarter-turn symmetry: {(visibleQuarter ? "preserved" : "not certified in this view")}</text>");
        foreach (var line in guides) Add(line, "seam"); foreach (var line in view.Lines) Add(line, "ink");
        b.Append($"<text x=\"{left + 36}\" y=\"{top + 264}\" class=\"note\">{view.Lines.Count} actual stroke copies · {E(example.Patch.Pattern.Graph.Sites).Length} shared landmarks · no numeric source values</text></g>");
        return b.ToString();
        void Add(Line line, string css) => b.Append($"<path class=\"{css}\" stroke=\"{line.Colour}\" d=\"M {F(dx + line.X1 * scale)} {F(dy - line.Y1 * scale)} L {F(dx + line.X2 * scale)} {F(dy - line.Y2 * scale)}\"/>");
    }
    private static string Mechanics(QualitativeSquareStudy study)
    {
        var closed = study.Contour.TryReadClosedContour(study.Contour.Evaluate(), out _);
        var equal = study.Contour.TryReadEqualTurns(study.Contour.Evaluate(), out _);
        var cell = study.Cell.TryReadPassed(study.CellRead);
        return $"""
        <svg xmlns="http://www.w3.org/2000/svg" width="1460" height="470" viewBox="0 0 1460 470">
        <rect width="1460" height="470" fill="#f6f2e9"/>
        <g font-family="Segoe UI,Arial,sans-serif" fill="#243640">
        <text x="36" y="42" font-size="25" font-weight="650">Three connected constructions, separate proof obligations</text>
        <text x="36" y="82" font-size="18" font-weight="600">Marked whole traversal</text>
        <circle cx="220" cy="215" r="92" fill="none" stroke="#176b7c" stroke-width="3"/>
        <path d="M220 215L312 215 M220 215L220 123 M220 215L128 215 M220 215L220 307" stroke="#8d9b9d" fill="none"/>
        <g fill="#176b7c"><circle cx="220" cy="215" r="5"/><circle cx="312" cy="215" r="6"/><circle cx="220" cy="123" r="6"/><circle cx="128" cy="215" r="6"/><circle cx="220" cy="307" r="6"/></g>
        <circle cx="312" cy="215" r="11" fill="none" stroke="#b75a2a" stroke-width="2"/>
        <text x="326" y="220" font-size="15">start / return</text>
        <text x="36" y="353" font-size="15">Shared center and unmeasured radial reach.</text>
        <text x="36" y="380" font-size="15">Closed: {closed} · equal-turn promise: {equal}</text>
        <text x="36" y="407" font-size="15">Without that promise, equal turns remain open.</text>
        <text x="515" y="82" font-size="18" font-weight="600">Axis / sign action</text>
        <path d="M585 215H845 M715 105V325" stroke="#8d9b9d" fill="none"/>
        <g fill="#176b7c"><circle cx="815" cy="215" r="7"/><circle cx="715" cy="115" r="7"/><circle cx="615" cy="215" r="7"/><circle cx="715" cy="315" r="7"/></g>
        <g font-size="16"><text x="833" y="220">+X</text><text x="701" y="101">+Y</text><text x="578" y="220">−X</text><text x="701" y="342">−Y</text></g>
        <path d="M805 195Q795 135 738 125 M695 125Q635 135 625 193 M625 237Q635 295 694 305 M738 305Q795 295 805 237" fill="none" stroke="#b75a2a" stroke-width="2"/>
        <text x="515" y="380" font-size="15">+X → +Y → −X → −Y → +X</text>
        <text x="515" y="407" font-size="15">Four-step turn; two independent flips differ.</text>
        <text x="1000" y="82" font-size="18" font-weight="600">Two periodic seam actions</text>
        <path d="M1090 307V123H1274V307Z M1090 307L1274 307L1274 123 M1090 307L1090 123L1274 123" fill="none" stroke="#176b7c" stroke-width="3"/>
        <g fill="#b75a2a"><circle cx="1090" cy="307" r="7"/><circle cx="1274" cy="307" r="7"/><circle cx="1090" cy="123" r="7"/><circle cx="1274" cy="123" r="7"/></g>
        <g font-size="16"><text x="1175" y="335">X</text><text x="1290" y="220">Y</text><text x="1030" y="220">Y</text><text x="1175" y="108">X</text></g>
        <text x="1000" y="353" font-size="15">Same corner, two retained routes: {cell}</text>
        <text x="1000" y="380" font-size="15">Nonzero periods survive quotient endpoint return.</text>
        <text x="1000" y="407" font-size="15">Basis directions and seam actions stay distinct.</text>
        <text x="36" y="450" font-size="14" fill="#52616a">Chosen illustrations. The proofs are native participant maps; the drawing does not establish a physical circle or metric.</text>
        </g></svg>
        """;
    }
    private static (long X, long Y) Position(QualitativeMotionFrame frame, Relation word, IReadOnlyDictionary<IElement, long> calibration)
    {
        long x = 0, y = 0;
        foreach (var item in E(word).Cast<Relation>())
        {
            if (!frame.Direction(item, out var axis, out var forward, out var reach) || !calibration.TryGetValue(reach, out var magnitude) || magnitude <= 0)
                throw new InvalidOperationException("No positive calibration was supplied for an actual reach.");
            checked { if (ReferenceEquals(axis, frame.Horizontal)) x += forward ? magnitude : -magnitude; else y += forward ? magnitude : -magnitude; }
        }
        return (x, y);
    }
    private static IEnumerable<IElement> Traverse(IElement root)
    {
        var seen = new HashSet<IElement>(ReferenceEqualityComparer.Instance); var stack = new Stack<IElement>(); stack.Push(root);
        while (stack.TryPop(out var item)) { if (!seen.Add(item)) continue; yield return item; if (item is Relation row) { stack.Push(row.Left); stack.Push(row.Right); } }
    }
    private static string F(double value) => value.ToString("0.###", CultureInfo.InvariantCulture);
    private static string H(string value) => WebUtility.HtmlEncode(value);
    private static Relation S(params IElement[] values) => StructuralConstraint.Closed(values);
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
}
