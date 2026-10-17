using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;
using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;

namespace Gojo.Experiments.QualitativeFrieze;

/// <summary>An optional measured view of the machine's actual accepted trace.
/// No drawing is tiled or transformed by this renderer.</summary>
public static class WallpaperMachineExperiment
{
    private static readonly string[] Colours = ["#155f76", "#9c4a32", "#6b488b", "#42754b"];

    public static int Run(string[] args)
    {
        var output = Path.Combine("experiments", "QualitativeFrieze", "results", "wallpaper-machine");
        var pulses = 2048;
        for (var i = 0; i < args.Length; i++)
            if (args[i] == "--output" && i + 1 < args.Length) output = args[++i];
            else if (args[i] == "--pulses" && i + 1 < args.Length && int.TryParse(args[++i], out var n) && n is > 0 and <= 8192) pulses = n;
            else throw new ArgumentException("Usage: --wallpaper-machine [--output DIRECTORY] [--pulses 1..8192]");
        Directory.CreateDirectory(output);
        var study = new WallpaperMachineStudy();
        var views = study.Cases.Select(example => Observe(example, pulses)).ToArray();
        var figure = Gallery(views, false); var stretched = Gallery(views, true);
        File.WriteAllText(Path.Combine(output, "gallery.svg"), figure);
        File.WriteAllText(Path.Combine(output, "stretched.svg"), stretched);
        var tables = new StringBuilder();
        foreach (var view in views)
        {
            tables.Append($"<details><summary>{H(view.Example.Name)} — accepted control connections</summary><p>{H(view.Example.Description)}</p><table><tr><th>Connection</th><th>Completed component</th><th>Accepted effects in that cycle</th><th>Next admitted input</th></tr>");
            foreach (var row in view.Events)
                tables.Append($"<tr><td>{row.Index}</td><td>{H(row.Component)}</td><td>{row.Effects}</td><td>{H(row.Next)}</td></tr>");
            tables.Append("</table></details>");
        }
        var maxStep = views.Max(view => view.Steps);
        File.WriteAllText(Path.Combine(output, "gallery.html"), "<!doctype html><html lang=\"en\"><head><meta charset=\"utf-8\"><meta name=\"viewport\" content=\"width=device-width,initial-scale=1\"><title>Acknowledged 2D wallpaper machines</title><style>body{max-width:1420px;margin:24px auto;padding:0 16px;background:#f3f0e6;color:#233d46;font:16px Segoe UI,Arial,sans-serif}p{line-height:1.5}button{padding:9px 14px;margin:0 8px 8px 0}svg{width:100%;height:auto}table{border-collapse:collapse;width:100%;margin:16px 0}th,td{text-align:left;border-bottom:1px solid #c7cec8;padding:7px}details{margin:18px 0;padding:16px;background:#fff9}summary{cursor:pointer;font-weight:600}.navigation .jump,.navigation .anchor{display:inline}.jump,.anchor{display:none}.mono .ink{stroke:#203c45!important}.invert svg .paper{fill:#233d46}.invert .ink{stroke:#f5f0de!important}.invert svg text{fill:#eee}.alternate{display:none}.stretch .regular{display:none}.stretch .alternate{display:block}.future{visibility:hidden}input{width:300px;max-width:65vw}</style></head><body><p><button onclick=\"document.body.classList.toggle('navigation')\">Show / hide placement</button><button onclick=\"document.body.classList.toggle('mono')\">Motif colours</button><button onclick=\"document.body.classList.toggle('invert')\">Invert ink</button><button onclick=\"document.body.classList.toggle('stretch')\">Stretch interpretation</button></p>" +
            $"<p><label>Accepted instruction <input id=\"progress\" type=\"range\" min=\"0\" max=\"{maxStep}\" value=\"{maxStep}\"><output id=\"counter\">{maxStep}</output></label></p><div class=\"regular\">{figure}</div><div class=\"alternate\">{stretched}</div>" +
            "<p><strong>Motif completion → admitted cell passage → accepted placement → next motif.</strong> Each instruction has a local packet threshold and a current question. Its packet and phase remain held until the exact drawing offer is accepted. Completion is a control observation; fresh packets power subsequent instructions.</p><p>A passage accepts reset, restoration of world orientation, non-ink jump, anchor and next orientation separately. A rejected later instruction preserves the accepted prefix and holds the next motif. Placement overlays show those actual actions, including row returns.</p><p>The source has unmeasured shared reaches and discrete events. Equal screen pitches and the stretched view are separate illustrative interpretations. These finite patches do not certify infinite wallpaper groups, calibrated angles, elapsed time or continuous tension flow. There is no post-processing that copies a completed motif.</p>" + tables +
            "<p><a href=\"diagnostics.json\">Native trace diagnostics</a> · <a href=\"gallery.svg\">Standalone figure</a> · <a href=\"stretched.svg\">Stretched interpretation</a></p><script>const slider=document.querySelector('#progress');slider.addEventListener('input',()=>{document.querySelector('#counter').textContent=slider.value;document.querySelectorAll('[data-step]').forEach(e=>e.classList.toggle('future',Number(e.dataset.step)>Number(slider.value)))})</script></body></html>");
        File.WriteAllText(Path.Combine(output, "diagnostics.json"), JsonSerializer.Serialize(new
        {
            Schema = "acknowledged-wallpaper-machine-v1", MaximumPulses = pulses,
            Scope = "Bounded native routes; no numeric source, output tiling, learning or wallpaper group certification.",
            Ownership = "One fresh input packet per accepted instruction; completion notifications are non-draining control observations.",
            Machines = views.Select(view => new { view.Example.Name, view.Example.Description, Complete = view.Example.Machine.IsComplete,
                view.Steps, view.Motifs, view.Passages, Strokes = view.Ink.Count, view.NumericElements, view.NativeElements,
                view.Imported, view.Active, view.Retained, view.Released, view.Pending, view.Milliseconds, Connections = view.Events })
        }, new JsonSerializerOptions { WriteIndented = true }));
        foreach (var view in views)
            Console.WriteLine($"{view.Example.Name}: {view.Motifs} motifs, {view.Passages} accepted passages, {view.Ink.Count} strokes; {view.Imported} packets = {view.Active} active + {view.Retained} retained + {view.Released} released; {view.Pending} pending; {view.NumericElements} native numeric elements; complete {view.Example.Machine.IsComplete}.");
        Console.WriteLine($"Wrote {Path.GetFullPath(output)}.");
        return views.Any(view => !view.Example.Machine.IsComplete || view.Pending != 0 || view.NumericElements != 0) ? 1 : 0;
    }

    private static View Observe(WallpaperMachineStudy.Case example, int pulses)
    {
        var machine = example.Machine; var clock = Stopwatch.StartNew(); var run = machine.Run(pulses); clock.Stop();
        var native = Traverse(run).ToArray();
        var positions = new Dictionary<IElement, (double X, double Y)>(ReferenceEqualityComparer.Instance);
        foreach (var row in E(machine.Drawing.SiteBindings).Cast<Relation>())
            positions.Add(row.Left, Position(machine.Drawing.Frame, (Relation)row.Right));
        var offers = new Dictionary<IElement, int>(ReferenceEqualityComparer.Instance);
        var steps = new Dictionary<IElement, int>(ReferenceEqualityComparer.Instance);
        var ink = new List<Line>(); var jumps = new List<Line>(); var anchors = new List<Marker>();
        var stepIndex = 0;
        foreach (var effect in E(machine.DrawingReceipts).Cast<Relation>())
        {
            stepIndex++; machine.Drawing.TryReadStep(effect, out var before, out var offer, out var after, out _, out _, out _);
            steps[offer] = stepIndex;
            var action = E((Relation)((Relation)((Relation)offer.Right).Right).Left).Single();
            machine.Drawing.TryReadState(before, out _, out var from, out _, out _, out _, out _, out _);
            machine.Drawing.TryReadState(after, out _, out var to, out _, out _, out _, out _, out _);
            if (machine.Drawing.TryReadJump(action, out _) || machine.Drawing.IsReset(action))
                jumps.Add(new(positions[from.Left], positions[to.Left], stepIndex, "#b97930"));
            if (machine.Drawing.IsAnchor(action)) anchors.Add(new(positions[to.Left], stepIndex));
        }
        var motifIndex = 0;
        foreach (var completion in E(machine.CompletedMotifs).Cast<Relation>())
        {
            foreach (var effect in E((Relation)((Relation)completion.Right).Left).Cast<Relation>())
            {
                machine.Drawing.TryReadStep(effect, out _, out var offer, out _, out _, out _, out _); offers[offer] = motifIndex;
            }
            motifIndex++;
        }
        foreach (var stroke in E(machine.Drawing.Strokes).Cast<Relation>())
        {
            QualitativeDrawingReceiver.TryReadStroke(stroke, out var offer, out var from, out var to, out _);
            ink.Add(new(positions[from], positions[to], steps[offer], Colours[offers.GetValueOrDefault(offer) % Colours.Length]));
        }
        var events = new List<Connection>();
        foreach (var dispatch in E(machine.Dispatches).Cast<Relation>())
        {
            var completion = (Relation)((Relation)dispatch.Left).Right;
            var port = ((Relation)completion.Left).Left;
            var isMotif = ReferenceEquals(port, machine.Motif.CompletionPort);
            var question = (Relation)((Relation)dispatch.Right).Right;
            QualitativeQuestion.TryReadAnswer(question, out var answer);
            var input = ((Relation)answer).Right;
            var next = ReferenceEquals(input, machine.TerminalPort) ? "declared terminal" :
                ReferenceEquals(input, machine.Motif.InputPort) ? "next motif" : "cell passage";
            events.Add(new(events.Count + 1, isMotif ? "motif" : "placement", E((Relation)((Relation)completion.Right).Left).Length, next));
        }
        var imported = 0; var active = 0; var retained = 0; var released = 0;
        var pending = E(machine.Unresolved).Length + E(machine.Pending).Length;
        foreach (var component in machine.Components)
        {
            imported += E(component.ImportedPackets).Length;
            var account = (Relation)component.Account().Right; var tail = (Relation)account.Right; var end = (Relation)tail.Right;
            active += E((Relation)account.Left).Length; retained += E((Relation)tail.Left).Length; released += E((Relation)end.Left).Length;
            pending += E((Relation)end.Right).Length + E(component.Unresolved).Length;
        }
        return new(example, ink, jumps, anchors, events, stepIndex, motifIndex, E(machine.AcceptedPassages).Length,
            native.Length, native.Count(item => item is Carrier or CarrierLocation), imported, active, retained, released, pending, clock.ElapsedMilliseconds);
    }

    private static string Gallery(View[] views, bool stretched)
    {
        const int width = 1420, cardHeight = 515; var height = 160 + cardHeight * ((views.Length + 1) / 2);
        var svg = new StringBuilder($"<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 {width} {height}\" role=\"img\" aria-label=\"Four acknowledged two dimensional wallpaper machine traces\"><style>text{{font-family:Segoe UI,Arial,sans-serif}}.title{{font-size:26px;font-weight:650}}.label{{font-size:20px;font-weight:600}}.note{{font-size:14px;fill:#52666b}}.ink{{stroke-width:6;stroke-linecap:square;stroke-linejoin:miter;fill:none}}.jump{{stroke-width:1;stroke-dasharray:5 4;fill:none;opacity:.65}}.anchor{{fill:#c77c2b;stroke:#fff;stroke-width:1}}</style><rect class=\"paper\" width=\"100%\" height=\"100%\" fill=\"#f3f0e6\"/><text x=\"30\" y=\"40\" class=\"title\">One motif machine, two-dimensional accepted placement</text><text x=\"30\" y=\"72\" class=\"note\">Completion → placement request → accepted passage → next motif. Every visible stroke is an executed native action.</text><text x=\"30\" y=\"98\" class=\"note\">{(stretched ? "Stretched interpretation: horizontal pitch ×1.6; shared native state and history." : "Equal-pitch illustrative interpretation. Four columns × three rows, finite native routes; no output tiling.")}</text>");
        svg.Append("<style>.jump,.anchor{display:none}</style>");
        for (var i = 0; i < views.Length; i++)
        {
            var view = views[i]; var left = 20 + i % 2 * 700; var top = 130 + i / 2 * cardHeight;
            svg.Append($"<g><rect class=\"paper\" x=\"{left}\" y=\"{top}\" width=\"680\" height=\"495\" rx=\"12\" fill=\"#fffdf6\" stroke=\"#d4dbd3\"/><text x=\"{left + 20}\" y=\"{top + 33}\" class=\"label\">{H(view.Example.Name)}</text><text x=\"{left + 20}\" y=\"{top + 58}\" class=\"note\">{view.Motifs} motifs · {view.Passages} accepted passages · {view.Ink.Count} strokes</text>");
            var lines = view.Ink.Concat(view.Navigation).ToArray();
            var minX = lines.Min(line => Math.Min(line.From.X, line.To.X)); var maxX = lines.Max(line => Math.Max(line.From.X, line.To.X));
            var minY = lines.Min(line => Math.Min(line.From.Y, line.To.Y)); var maxY = lines.Max(line => Math.Max(line.From.Y, line.To.Y));
            var aspect = stretched ? 1.6 : 1;
            var scale = Math.Min(610 / Math.Max(1, (maxX - minX) * aspect), 325 / Math.Max(1, maxY - minY));
            var x = left + 35 + (610 - (maxX - minX) * aspect * scale) / 2;
            var y = top + 82 + (325 - (maxY - minY) * scale) / 2;
            string P((double X, double Y) point) => $"{F(x + (point.X - minX) * aspect * scale)} {F(y + (maxY - point.Y) * scale)}";
            foreach (var line in view.Navigation) Add(line, "jump");
            foreach (var line in view.Ink) Add(line, "ink");
            foreach (var anchor in view.Anchors)
            {
                var parts = P(anchor.Point).Split(' ');
                svg.Append($"<circle class=\"anchor\" data-step=\"{anchor.Step}\" cx=\"{parts[0]}\" cy=\"{parts[1]}\" r=\"4\"><title>Accepted anchor, instruction {anchor.Step}</title></circle>");
            }
            svg.Append($"<text x=\"{left + 20}\" y=\"{top + 450}\" class=\"note\">{view.Steps} acknowledged instructions · {view.Imported} packets released once</text><text x=\"{left + 20}\" y=\"{top + 475}\" class=\"note\">{view.Pending} pending · {view.NumericElements} numeric source elements · finite route {(view.Example.Machine.IsComplete ? "complete" : "held")}</text></g>");
            void Add(Line line, string kind) => svg.Append($"<path class=\"{kind}\" data-step=\"{line.Step}\" stroke=\"{line.Colour}\" d=\"M {P(line.From)} L {P(line.To)}\"><title>Accepted instruction {line.Step}</title></path>");
        }
        return svg.Append("</svg>").ToString();
    }

    private static (double X, double Y) Position(QualitativeMotionFrame frame, Relation word)
    {
        double x = 0, y = 0;
        foreach (var move in E(word).Cast<Relation>())
        {
            if (!frame.Direction(move, out var axis, out var forward, out _)) throw new InvalidOperationException("Unresolved display direction.");
            if (ReferenceEquals(axis, frame.Horizontal)) x += forward ? 1 : -1; else y += forward ? 1 : -1;
        }
        return (x, y);
    }
    private static IEnumerable<IElement> Traverse(IElement root)
    {
        var seen = new HashSet<IElement>(ReferenceEqualityComparer.Instance); var stack = new Stack<IElement>(); stack.Push(root);
        while (stack.TryPop(out var item))
        { if (!seen.Add(item)) continue; yield return item; if (item is Relation row) { stack.Push(row.Left); stack.Push(row.Right); } }
    }
    private static string H(string value) => WebUtility.HtmlEncode(value);
    private static string F(double value) => value.ToString("0.###", CultureInfo.InvariantCulture);
    private static Relation S(params IElement[] items) => StructuralConstraint.Closed(items);
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
    private sealed record Line((double X, double Y) From, (double X, double Y) To, int Step, string Colour);
    private sealed record Marker((double X, double Y) Point, int Step);
    private sealed record Connection(int Index, string Component, int Effects, string Next);
    private sealed record View(WallpaperMachineStudy.Case Example, List<Line> Ink, List<Line> Navigation,
        List<Marker> Anchors, List<Connection> Events, int Steps, int Motifs, int Passages, int NativeElements,
        int NumericElements, int Imported, int Active, int Retained, int Released, int Pending, long Milliseconds);
}
