using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;
using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;

namespace Gojo.Experiments.QualitativeFrieze;

/// <summary>Optional calibrated views of connected, actually executed fields.
/// The renderer neither tiles motifs nor discovers joins from screen positions.</summary>
public static class ConnectedWallpaperExperiment
{
    private static readonly string[] Palette = ["#195f72", "#985030", "#684482", "#4f7546", "#986a24"];
    public sealed record Examples(ConnectedWallpaperStudy Study, ConnectedWallpaperStudy.Case[] Cases,
        ConnectedWallpaperStudy.Case Sixfold, ConnectedWallpaperStudy.Case Stars, QualitativeSixfoldWords Words, Relation Surface);
    public sealed record Metric(long XX, long XY, long YY, long Determinant, bool PreservesSixfoldAction);

    /// <summary>A measured Gram read of the actual substituted basis words.
    /// Entries use one common positive scale, so no angle approximation is
    /// needed to test whether this particular view preserves the action.</summary>
    public static Metric ReadSixfoldMetric(QualitativeSixfoldWords words, bool triangular)
    {
        long xx = triangular ? 2 : 1, xy = triangular ? 1 : 0, yy = triangular ? 2 : 1;
        var a = Position(words.Frame, words.TurnWord(words.U), words.Reach);
        var b = Position(words.Frame, words.TurnWord(words.V), words.Reach);
        long Dot((double X, double Y) u, (double X, double Y) v) => checked(
            xx * (long)u.X * (long)v.X + xy * ((long)u.X * (long)v.Y + (long)u.Y * (long)v.X) + yy * (long)u.Y * (long)v.Y);
        return new(xx, xy, yy, checked(xx * yy - xy * xy), Dot(a, a) == xx && Dot(a, b) == xy && Dot(b, b) == yy);
    }

    public static Examples Create()
    {
        var study = new ConnectedWallpaperStudy(); var basis = study.Basis.Basis;
        var words = new QualitativeSixfoldWords(basis.Frame, basis.Reach);
        var hex = SixfoldRings(study, words); var stars = SixfoldStars(study, words);
        var cases = study.Cases.Concat([hex, stars]).ToArray();
        return new(study, cases, hex, stars, words, new Relation(study.Surface,
            new Relation(words.Surface, S(cases.Select(item => (IElement)item.Machine.Surface).ToArray()))));
    }

    /// <summary>Compiles an order-six native word construction to existing
    /// acknowledged Draw actions. It does not add a sixfold direction actuator.</summary>
    public static ConnectedWallpaperStudy.Case SixfoldRings(ConnectedWallpaperStudy study,
        QualitativeSixfoldWords words, int columns = 5, int rows = 4) => SixfoldCells(study, words, false, columns, rows);

    public static ConnectedWallpaperStudy.Case SixfoldStars(ConnectedWallpaperStudy study,
        QualitativeSixfoldWords words, int columns = 5, int rows = 4) => SixfoldCells(study, words, true, columns, rows);

    private static ConnectedWallpaperStudy.Case SixfoldCells(ConnectedWallpaperStudy study,
        QualitativeSixfoldWords words, bool star, int columns, int rows)
    {
        var basis = study.Basis.Basis; var drawing = new QualitativeDrawingReceiver(basis.Frame, initialSite: basis.Frame.Origin);
        Relation Repeat(Relation word, int count) => S(Enumerable.Range(0, count).SelectMany(_ => E(word)).ToArray());
        var jump = drawing.Jump(Repeat(words.U, star ? 4 : 2)); var actions = new List<IElement> { jump };
        var firstTurn = words.TurnWord(words.U, out var firstDerivation);
        var edge = words.TurnWord(firstTurn, out var secondDerivation);
        var derivations = new List<IElement> { firstDerivation, secondDerivation }; var draws = new List<Relation>();
        // With outer radii 4u and 4v, the shared inward landmark u+v gives
        // two sides v-3u and 3v-u. Repeated participation defines the ratio;
        // no measured angle or scalar radius is stored in the source.
        var firstSide = S([.. E(words.V), .. E(Repeat(basis.Inverse(words.U), 3))]);
        var secondSide = S([.. E(Repeat(words.V, 3)), .. E(basis.Inverse(words.U))]);
        for (var turn = 0; turn < 6; turn++)
        {
            if (star)
            {
                var a = drawing.Draw(firstSide); var b = drawing.Draw(secondSide);
                actions.Add(a); actions.Add(b); draws.Add(b);
                var nextA = words.TurnWord(firstSide, out var readA); var nextB = words.TurnWord(secondSide, out var readB);
                derivations.Add(new Relation(new Relation(firstSide, a), readA));
                derivations.Add(new Relation(new Relation(secondSide, b), readB));
                firstSide = nextA; secondSide = nextB;
            }
            else
            {
                var draw = drawing.Draw(Repeat(edge, 2)); draws.Add(draw); actions.Add(draw);
                var next = words.TurnWord(edge, out var read);
                derivations.Add(new Relation(new Relation(edge, draw), read)); edge = next;
            }
        }
        actions.Add(drawing.Reset());
        var slots = Enumerable.Range(0, 7).Select(_ => (IElement)new Placeholder()).ToArray();
        // First port is the actual initial u vertex; the last is its fresh
        // returned occurrence. Their closure is an explicit local declaration.
        var ports = S([new Relation(slots[0], new Relation(jump, drawing.CursorPort)),
            .. draws.Select((draw, i) => (IElement)new Relation(slots[i + 1], new Relation(draw, drawing.CursorPort)))]);
        var x = Repeat(words.U, star ? 8 : 4); var y = Repeat(words.V, star ? 8 : 4);
        var diagonal = S([.. E(basis.Inverse(x)), .. E(y)]);
        var pairings = S(new Relation(x, S(new Relation(slots[0], slots[3]))),
            new Relation(y, S(new Relation(slots[1], slots[4]))),
            new Relation(diagonal, S(new Relation(slots[2], slots[5]))));
        var construction = new Relation(words.Surface, S(derivations.ToArray()));
        return study.Build(star ? "Six-turn star corridors" : "Six-turn corridor rings",
            star ? "Paired sides through shared inward landmarks create a twelve-edge rosette. Six outer tips connect neighbouring stars through the same three lattice directions."
                 : "A native six-turn word orbit compiles to six live ring edges. Three neighbour directions join opposite radial ports; the regular hexagonal view is a separate equal-reach interpretation.",
            drawing, S(actions.ToArray()), x, y, ports, pairings,
            S(new Relation(slots[6], slots[0])), columns, rows, construction);
    }

    public static int Run(string[] args)
    {
        var output = Path.Combine("experiments", "QualitativeFrieze", "results", "wallpaper-field"); var pulses = 4096;
        for (var i = 0; i < args.Length; i++)
            if (args[i] == "--output" && i + 1 < args.Length) output = args[++i];
            else if (args[i] == "--pulses" && i + 1 < args.Length && int.TryParse(args[++i], out var count) && count is > 0 and <= 8192) pulses = count;
            else throw new ArgumentException("Usage: --wallpaper-field [--output DIRECTORY] [--pulses 1..8192]");
        Directory.CreateDirectory(output); var examples = Create();
        var views = examples.Cases.Select(example => Observe(examples, example, pulses)).ToArray();
        var normal = Gallery(views, false); var alternate = Gallery(views, true);
        File.WriteAllText(Path.Combine(output, "gallery.svg"), normal);
        File.WriteAllText(Path.Combine(output, "alternate.svg"), alternate);
        var table = new StringBuilder("<table><thead><tr><th>Machine</th><th>Motifs</th><th>Declared joins</th><th>Actual sites → joined sites</th><th>Closed cycles</th><th>Four-way sites</th><th>Open frontier surfaces</th></tr></thead><tbody>");
        foreach (var view in views)
            table.Append($"<tr><td>{H(view.Example.Name)}</td><td>{view.Motifs}</td><td>{view.Joined}</td><td>{view.SourceSites} → {view.Sites}</td><td>{view.Topology.Contours}</td><td>{view.Topology.X}</td><td>{view.Frontier}</td></tr>");
        table.Append("</tbody></table>");
        var maxStep = views.Max(view => view.Steps);
        File.WriteAllText(Path.Combine(output, "gallery.html"), "<!doctype html><html lang=\"en\"><head><meta charset=\"utf-8\"><meta name=\"viewport\" content=\"width=device-width,initial-scale=1\"><title>Connected qualitative wallpaper fields</title><style>body{max-width:1420px;margin:22px auto;padding:0 16px;background:#f3f0e6;color:#233d46;font:16px Segoe UI,Arial,sans-serif}p{line-height:1.5}button{padding:10px 14px;margin:0 8px 8px 0}svg{width:100%;height:auto}table{border-collapse:collapse;width:100%;font-size:14px}td,th{text-align:left;border-bottom:1px solid #c9cec8;padding:9px}.alternate{display:none}.other-basis .normal{display:none}.other-basis .alternate{display:block}.colours .ink{stroke:var(--owner)!important}.joins .joint{display:inline}.invert .paper{fill:#233d46}.invert .ink{stroke:#f5f0de}.invert text{fill:#eee}.future{visibility:hidden}input{width:300px;max-width:60vw}.reference{font-size:14px}details{padding:16px;margin:20px 0;background:#fff9}</style></head><body><p><button onclick=\"document.body.classList.toggle('joins')\">Declared junctions</button><button onclick=\"document.body.classList.toggle('colours')\">Motif colours</button><button onclick=\"document.body.classList.toggle('other-basis')\">Change basis interpretation</button><button onclick=\"document.body.classList.toggle('invert')\">Invert ink</button></p>" +
            $"<p><label>Accepted instruction <input id=\"progress\" type=\"range\" min=\"0\" max=\"{maxStep}\" value=\"{maxStep}\"><output id=\"counter\">{maxStep}</output></label></p><div class=\"normal\">{normal}</div><div class=\"alternate\">{alternate}</div>" +
            "<p><strong>The field is connected through declared motif ports.</strong> Accepted motif receipts publish actual boundary sites. A declared neighbour period must agree with the actual anchor movement, and its paired ports must agree in formal displacement. Only those accepted pairs create shared junctions in a derived graph. The original drawing sites and histories remain distinct and inspectable.</p><p>The diagonal weave and junction mesh have matching line geometry but different topology: crossing pixels alone do not create a shared site. Orange markers show declared multi-site junctions. Boundary roles beyond the finite patch remain an explicit frontier.</p>" + table +
            "<details><summary>Angles and the six-turn construction</summary><p>The source uses independent unmeasured directions and simultaneous move words. A diagonal is combined participation, rather than a stored angle. The six-turn construction maps u to v and v to v−u; native cancellation verifies its third power reverses direction and its sixth power restores it. Its transformed words compile into existing acknowledged drawing instructions.</p><p>The ring panel initially supplies equal screen reaches separated by 60°. The alternative supplies a square basis. Both show the same source topology; the square interpretation does not make the six-turn action a Euclidean rotation. This experiment does not add a general sixfold orientation actuator or certify all symmetries of an infinite wallpaper.</p></details>" +
            "<p class=\"reference\">The direction follows the paper’s separation of a template, symmetry construction and replication network, and its use of connecting corridors: <a href=\"https://journals.sagepub.com/doi/10.1177/14780771241279347\">Naserabad &amp; Ghanbaran, Computational approach in presentation a parametric method to construct hybrid girihs</a>. These are small structural experiments, not a reproduction of its full algorithm.</p><p><a href=\"diagnostics.json\">Native diagnostics</a> · <a href=\"gallery.svg\">Standalone figure</a> · <a href=\"alternate.svg\">Alternative interpretations</a></p><script>const input=document.querySelector('#progress');input.addEventListener('input',()=>{document.querySelector('#counter').textContent=input.value;document.querySelectorAll('[data-step]').forEach(e=>e.classList.toggle('future',Number(e.dataset.step)>Number(input.value)))})</script></body></html>");
        File.WriteAllText(Path.Combine(output, "diagnostics.json"), JsonSerializer.Serialize(new
        {
            Schema = "connected-wallpaper-field-v1", MaximumPulses = pulses,
            Scope = "Executed finite motif routes; explicit conditional seam quotient; native word action distinct from measured geometry.",
            Sixfold = new { HalfTurn = Zero(examples.Words.Frame, examples.Words.HalfTurnRead), FullTurn = Zero(examples.Words.Frame, examples.Words.FullTurnRead),
                SquareBasis = ReadSixfoldMetric(examples.Words, false), EqualReachSixtyDegreeBasis = ReadSixfoldMetric(examples.Words, true) },
            Machines = views.Select(view => new { view.Example.Name, Complete = view.Example.Machine.IsComplete, view.Motifs, view.Steps,
                Strokes = view.Lines.Length, view.SourceSites, JoinedSites = view.Sites, DeclaredJoins = view.Joined, FrontierSurfaces = view.Frontier,
                view.Topology, view.Pending, view.NumericElements, view.NativeElements, view.Imported, view.Active, view.Retained, view.Released })
        }, new JsonSerializerOptions { WriteIndented = true }));
        foreach (var view in views)
            Console.WriteLine($"{view.Example.Name}: {view.Motifs} motifs, {view.Lines.Length} strokes; {view.Joined} declared joins; {view.SourceSites} -> {view.Sites} sites; {view.Topology.Contours} cycles, {view.Topology.X} four-way sites; {view.Pending} pending; {view.NumericElements} numeric native elements.");
        Console.WriteLine($"Wrote {Path.GetFullPath(output)}.");
        return views.Any(view => !view.Example.Machine.IsComplete || view.Pending != 0 || view.NumericElements != 0) ? 1 : 0;
    }

    private static View Observe(Examples examples, ConnectedWallpaperStudy.Case example, int pulses)
    {
        var machine = example.Machine; var run = machine.Run(pulses); var field = examples.Study.Capture(example);
        var elements = Traverse(new Relation(examples.Surface, new Relation(run, field.Surface))).ToArray();
        var positions = E(field.SourceBindings).Cast<Relation>().ToDictionary<Relation, IElement, (double X, double Y)>(
            row => row.Left, row => Position(machine.Drawing.Frame, (Relation)row.Right, examples.Study.Basis.Basis.Reach), ReferenceEqualityComparer.Instance);
        var steps = new Dictionary<IElement, int>(ReferenceEqualityComparer.Instance); var owners = new Dictionary<IElement, int>(ReferenceEqualityComparer.Instance);
        var index = 0;
        foreach (var receipt in E(machine.DrawingReceipts).Cast<Relation>())
        { machine.Drawing.TryReadStep(receipt, out _, out var offer, out _, out _, out _, out _); steps[offer] = ++index; }
        var owner = 0;
        foreach (var completion in E(machine.CompletedMotifs).Cast<Relation>())
        {
            foreach (var effect in E((Relation)((Relation)completion.Right).Left).Cast<Relation>())
            { machine.Drawing.TryReadStep(effect, out _, out var offer, out _, out _, out _, out _); owners[offer] = owner; }
            owner++;
        }
        var lines = E(field.SourceGraph.Strokes).Cast<Relation>().Select(stroke =>
        {
            QualitativeStrokeGraph.TryReadStroke(stroke, out var cause, out var from, out var to, out _);
            return new Line(positions[from], positions[to], steps[cause], Palette[owners.GetValueOrDefault(cause) % Palette.Length]);
        }).ToArray();
        var joinedPositions = new Dictionary<IElement, (double X, double Y)>(ReferenceEqualityComparer.Instance);
        foreach (var binding in E(field.Bindings).Cast<Relation>()) joinedPositions[binding.Left] = Position(machine.Drawing.Frame, (Relation)binding.Right, examples.Study.Basis.Basis.Reach);
        var markers = E(field.Join.Classes).Cast<Relation>().Select(row => (Relation)row.Left)
            .Where(row => E((Relation)row.Left).Length > 1).Select(row => joinedPositions[row.Right]).ToArray();
        var topology = field.Graph.Describe(field.Graph.Evaluate());
        var imported = 0; var active = 0; var retained = 0; var released = 0;
        var pending = E(field.Unresolved).Length + E(field.BlockedSeams).Length + E(field.Join.Unresolved).Length + E(field.Join.Blocked).Length + E(machine.Pending).Length + E(machine.Unresolved).Length;
        if (!field.Join.TryReadPassed(field.Join.Evaluate())) pending++;
        foreach (var component in machine.Components)
        {
            imported += E(component.ImportedPackets).Length;
            var account = (Relation)component.Account().Right; var tail = (Relation)account.Right; var end = (Relation)tail.Right;
            active += E((Relation)account.Left).Length; retained += E((Relation)tail.Left).Length; released += E((Relation)end.Left).Length;
            pending += E((Relation)end.Right).Length + E(component.Unresolved).Length;
        }
        return new(example, lines, markers, topology, E(machine.CompletedMotifs).Length, index, E(field.SourceGraph.Sites).Length,
            E(field.Graph.Sites).Length, E(field.Join.Accepted).Length, E(field.Frontier).Length, pending, elements.Length,
            elements.Count(item => item is Carrier or CarrierLocation), imported, active, retained, released,
            ReferenceEquals(example, examples.Sixfold) || ReferenceEquals(example, examples.Stars));
    }

    private static string Gallery(View[] views, bool alternate)
    {
        var height = 145 + 490 * ((views.Length + 1) / 2);
        var caption = alternate ? "Alternative affine interpretations of exactly the same native sites, strokes and connections." :
            $"{views.Length} live 5 × 4 fields. Simultaneous chords, continuous seams, and a six-turn word construction.";
        var svg = new StringBuilder($"<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 1420 {height}\" role=\"img\" aria-label=\"Connected qualitative wallpaper machine fields\"><style>text{{font-family:Segoe UI,Arial,sans-serif}}.title{{font-size:26px;font-weight:650}}.label{{font-size:19px;font-weight:600}}.note{{font-size:14px;fill:#52676d}}.ink{{fill:none;stroke:#233e46;stroke-width:5;stroke-linecap:square;stroke-linejoin:miter}}.joint{{display:none;fill:#cc812b;stroke:#fff;stroke-width:1}}</style><rect class=\"paper\" width=\"100%\" height=\"100%\" fill=\"#f3f0e6\"/><text x=\"28\" y=\"40\" class=\"title\">From repeated motifs to connected wallpaper fields</text><text x=\"28\" y=\"72\" class=\"note\">Actual acknowledged drawing → published motif ports → checked neighbour periods → explicit junction correspondences</text><text x=\"28\" y=\"99\" class=\"note\">{caption}</text>");
        for (var i = 0; i < views.Length; i++)
        {
            var view = views[i]; var left = 20 + i % 2 * 700; var top = 125 + i / 2 * 490;
            var oblique = view.Sixfold != alternate;
            (double X, double Y) Project((double X, double Y) p) => oblique ? (p.X + .5 * p.Y, Math.Sqrt(.75) * p.Y) : p;
            var points = view.Lines.SelectMany(line => new[] { Project(line.From), Project(line.To) }).ToArray();
            var minX = points.Min(p => p.X); var maxX = points.Max(p => p.X); var minY = points.Min(p => p.Y); var maxY = points.Max(p => p.Y);
            var scale = Math.Min(615 / Math.Max(1, maxX - minX), 305 / Math.Max(1, maxY - minY));
            var x = left + 32 + (615 - (maxX - minX) * scale) / 2; var y = top + 82 + (305 - (maxY - minY) * scale) / 2;
            string P((double X, double Y) p) { p = Project(p); return $"{F(x + (p.X - minX) * scale)} {F(y + (maxY - p.Y) * scale)}"; }
            svg.Append($"<g><rect class=\"paper\" x=\"{left}\" y=\"{top}\" width=\"680\" height=\"470\" rx=\"12\" fill=\"#fffdf5\" stroke=\"#d4dbd3\"/><text x=\"{left + 18}\" y=\"{top + 32}\" class=\"label\">{H(view.Example.Name)}</text><text x=\"{left + 18}\" y=\"{top + 57}\" class=\"note\">{view.Joined} declared joins · {view.Topology.Contours} cycles · {view.Topology.X} four-way sites</text>");
            foreach (var line in view.Lines) svg.Append($"<path class=\"ink\" data-step=\"{line.Step}\" style=\"--owner:{line.Colour}\" d=\"M {P(line.From)} L {P(line.To)}\"><title>Actual accepted instruction {line.Step}</title></path>");
            foreach (var marker in view.Markers)
            { var p = P(marker).Split(' '); svg.Append($"<circle class=\"joint\" cx=\"{p[0]}\" cy=\"{p[1]}\" r=\"4\"/>"); }
            svg.Append($"<text x=\"{left + 18}\" y=\"{top + 421}\" class=\"note\">{view.SourceSites} actual sites → {view.Sites} joined sites · {view.Frontier} frontier surfaces</text><text x=\"{left + 18}\" y=\"{top + 448}\" class=\"note\">{view.Pending} pending · {(oblique ? "equal-reach 60° basis interpretation" : "square basis interpretation")}</text></g>");
        }
        return svg.Append("</svg>").ToString();
    }

    private static (double X, double Y) Position(QualitativeMotionFrame frame, Relation word, IElement reach)
    {
        double x = 0, y = 0;
        foreach (var entry in E(word))
        {
            if (entry is not Relation move || !frame.Direction(move, out var axis, out var forward, out var actualReach) || !ReferenceEquals(reach, actualReach))
                throw new InvalidOperationException("The display calibration does not cover this actual move.");
            if (ReferenceEquals(axis, frame.Horizontal)) x += forward ? 1 : -1; else y += forward ? 1 : -1;
        }
        return (x, y);
    }
    private static bool Zero(QualitativeMotionFrame frame, Relation read) => frame.TryReadResult(read, out var rest, out _, out var pending) && E(rest).Length == 0 && E(pending).Length == 0;
    private static IEnumerable<IElement> Traverse(IElement root)
    {
        var seen = new HashSet<IElement>(ReferenceEqualityComparer.Instance); var stack = new Stack<IElement>(); stack.Push(root);
        while (stack.TryPop(out var item)) { if (!seen.Add(item)) continue; yield return item; if (item is Relation row) { stack.Push(row.Left); stack.Push(row.Right); } }
    }
    private static string H(string value) => WebUtility.HtmlEncode(value);
    private static string F(double value) => value.ToString("0.###", CultureInfo.InvariantCulture);
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
    private static Relation S(params IElement[] values) => StructuralConstraint.Closed(values);
    private sealed record Line((double X, double Y) From, (double X, double Y) To, int Step, string Colour);
    private sealed record View(ConnectedWallpaperStudy.Case Example, Line[] Lines, (double X, double Y)[] Markers,
        QualitativeStrokeGraph.Summary Topology, int Motifs, int Steps, int SourceSites, int Sites, int Joined, int Frontier,
        int Pending, int NativeElements, int NumericElements, int Imported, int Active, int Retained, int Released, bool Sixfold);
}
