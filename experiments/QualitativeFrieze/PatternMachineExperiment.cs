using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;
using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;

namespace Gojo.Experiments.QualitativeFrieze;

public static class PatternMachineExperiment
{
    private static readonly string[] Palette = ["#126c83", "#d07621", "#884ba1", "#417c40"];
    public static int Run(string[] args, IReadOnlyList<PatternMachine>? definitions = null,
        string? outputDirectory = null, int defaultDrives = 48)
    {
        var output = outputDirectory ?? Path.Combine("experiments", "QualitativeFrieze", "results", "machines"); var drives = defaultDrives;
        for (var i = 0; i < args.Length; i++)
            if (args[i] == "--output" && i + 1 < args.Length) output = args[++i];
            else if (args[i] == "--drives" && i + 1 < args.Length && int.TryParse(args[++i], out var n) && n is > 0 and <= 128) drives = n;
            else throw new ArgumentException("Usage: --machines [--output DIRECTORY] [--drives 1..128]");
        Directory.CreateDirectory(output);
        var views = (definitions ?? PatternMachineLibrary.Create()).Select(machine => Observe(machine, drives)).ToArray();
        var figure = new StringBuilder($"<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"1420\" height=\"{190 + views.Length * 250}\" viewBox=\"0 0 1420 {190 + views.Length * 250}\"><style>text{{font-family:Segoe UI,Arial,sans-serif}}.head{{font-size:26px;font-weight:650}}.label{{font-size:18px;font-weight:600}}.note{{font-size:15px;fill:#52616a}}.ink{{stroke-width:7;stroke-linejoin:round;stroke-linecap:square;fill:none}}.jump{{stroke:#c87a28;stroke-width:1.2;stroke-dasharray:4 4;fill:none}}</style><rect width=\"100%\" height=\"100%\" fill=\"#f5f2e8\"/><text x=\"40\" y=\"45\" class=\"head\">Operating qualitative pattern machines</text><text x=\"40\" y=\"77\" class=\"note\">{drives} explicit drives · finite packet occupancy → threshold question → release → muscle → cursor</text><text x=\"40\" y=\"103\" class=\"note\">No numeric samples or prebuilt output sites. Equal display pitches are an optional calibration.</text><text x=\"40\" y=\"129\" class=\"note\">Blue: one muscle · purple: simultaneous contributions · dashed orange: navigation without ink.</text>");
        for (var i = 0; i < views.Length; i++) figure.Append(Panel(views[i], 165 + 250 * i));
        figure.Append("</svg>");
        File.WriteAllText(Path.Combine(output, "gallery.svg"), figure.ToString());
        var cards = new StringBuilder();
        foreach (var view in views)
        {
            cards.Append($"<details><summary>{H(view.Machine.Name)} — causal events</summary><p>Each row is a local reservoir question. Waiting is definite; release requires a current affirmative question. Control actions occur after the drive’s drawing sample.</p><table><thead><tr><th>Drive</th><th>Muscle</th><th>Intake</th><th>Occupied before release</th><th>Released</th><th>Response</th></tr></thead><tbody>");
            foreach (var row in view.Events.Take(96)) cards.Append($"<tr><td>{row.Drive}</td><td>{H(row.Muscle)}</td><td>{row.Intake}</td><td>{row.Occupied}</td><td>{row.Released}</td><td>{H(row.Response)}</td></tr>");
            cards.Append("</tbody></table></details>");
        }
        File.WriteAllText(Path.Combine(output, "gallery.html"), "<!doctype html><html><head><meta charset=\"utf-8\"><title>Gojo pattern machines</title><style>body{max-width:1420px;margin:24px auto;background:#f5f2e8;color:#243640;font:16px Segoe UI,Arial,sans-serif}button{padding:9px 16px;margin-right:10px}svg{width:100%;height:auto}table{border-collapse:collapse;width:100%;margin:16px 0}th,td{text-align:left;border-bottom:1px solid #c6c9c3;padding:7px}details{margin:20px;padding:15px;background:#fff9}summary{cursor:pointer;font-weight:600}.hide-jumps .jump{display:none}.mono .ink{stroke:#17313e!important}.invert>svg>rect{fill:#243640}.invert .ink{stroke:#f5f2e8!important}.invert svg text{fill:#eee}</style></head><body><p><button onclick=\"document.body.classList.toggle('hide-jumps')\">Show / hide navigation</button><button onclick=\"document.body.classList.toggle('mono')\">Contribution colours</button><button onclick=\"document.body.classList.toggle('invert')\">Invert ink</button></p>" + figure + "<p>The machine is the definition: reservoirs, phase transitions, exposed ports, laws and initial states. Its run adds fresh packet identities, questions, transfers, actions and sites. This is an uncalibrated discrete execution layer; continuous signed carrier tension remains a separate refinement.</p><p>Navigation is drawn as an explanatory overlay only. A geometric return does not merge site identities. No automatic learning, elapsed-time calibration, packet recycling or energy expenditure is claimed.</p>" + cards + "</body></html>");
        File.WriteAllText(Path.Combine(output, "diagnostics.json"), JsonSerializer.Serialize(new {
            Drives = drives, Scope = "Operating uncalibrated packet machines; optional equal-pitch display; no measured input or continuous carrier tension.",
            Schedule = "One explicit drive imports packets; modules settle in exposed feed-forward order; movement responses combine into one chord; ordered controls act after that sample.",
            Machines = views.Select(view => new { view.Machine.Name, DefinitionElements = Traverse(view.Machine.Surface).Count(), view.NativeElements, NativeCarriers = view.Carriers, view.Strokes, view.Jumps,
                view.Imported, view.Active, view.Retained, view.Released, view.Pending, view.Events })
        }, new JsonSerializerOptions { WriteIndented = true }));
        foreach (var view in views) Console.WriteLine($"{view.Machine.Name}: {view.Strokes} strokes, {view.Jumps} navigation actions; packets {view.Imported} = {view.Active} active + {view.Retained} retained + {view.Released} released; {view.Pending} pending; {view.Carriers} native numeric elements.");
        Console.WriteLine($"Wrote {Path.GetFullPath(output)}.");
        return views.Any(view => view.Pending != 0 || view.Carriers != 0) ? 1 : 0;
    }

    private static View Observe(PatternMachine machine, int drives)
    {
        var run = machine.Run(drives); var elements = Traverse(run).ToArray();
        var positions = new Dictionary<IElement, (double X, double Y)>(ReferenceEqualityComparer.Instance);
        foreach (var binding in E(machine.Drawing.SiteBindings).Cast<Relation>())
        {
            if (!machine.Drawing.TryReadBinding(binding, out var site, out var position)) throw new InvalidOperationException("Unknown site binding.");
            positions.Add(site, ReadWord(machine.Drawing.Frame, position));
        }
        var events = new List<Event>(); var lines = new List<Line>(); var jumps = new List<Line>();
        var tickOwners = new Dictionary<IElement, (int Drive, int Owner)>(ReferenceEqualityComparer.Instance);
        var drive = 0;
        foreach (var receipt in E(machine.Timeline).Cast<Relation>())
        {
            drive++;
            var ticks = (Relation)((Relation)receipt.Right).Left;
            for (var owner = 0; owner < machine.Muscles.Count; owner++)
            {
                var tick = (Relation)E(ticks)[owner]; var muscle = machine.Muscles[owner]; tickOwners.Add(tick, (drive, owner));
                muscle.TryReadTick(tick, out var actions, out var drained, out _, out _);
                var receive = (Relation)((Relation)((Relation)tick.Right).Left).Left;
                muscle.Accumulator.TryReadReceipt(receive, out _, out _, out var charged, out var accepted, out _, out _, out _, out _);
                muscle.Accumulator.TryReadState(charged, out _, out var occupied, out _, out _);
                events.Add(new(drive, muscle.Name, E(accepted).Length, E(occupied).Length, E(drained).Length,
                    E(drained).Length == 0 ? "waiting" : string.Join(" → ", E(actions).Select(action => Describe(machine.Drawing, action)))));
            }
            var effects = (Relation)((Relation)((Relation)((Relation)receipt.Right).Right).Right).Left;
            foreach (var effect in E(effects).Cast<Relation>())
            {
                machine.Drawing.TryReadStep(effect, out var before, out var offer, out var after, out _, out _, out _);
                var action = E((Relation)((Relation)((Relation)offer.Right).Right).Left).Single();
                if (!machine.Drawing.TryReadJump(action, out _) && !machine.Drawing.IsReset(action)) continue;
                machine.Drawing.TryReadState(before, out _, out var from, out _, out _, out _, out _, out _);
                machine.Drawing.TryReadState(after, out _, out var to, out _, out _, out _, out _, out _);
                var a = positions[from.Left]; var b = positions[to.Left]; jumps.Add(new(a.X, a.Y, b.X, b.Y, drive, "#c87a28"));
            }
        }
        foreach (var stroke in E(machine.Drawing.Strokes).Cast<Relation>())
        {
            QualitativeStrokeGraph.TryReadStroke(stroke, out var cause, out var from, out var to, out _);
            var contributions = (Relation)((Relation)((Relation)((Relation)cause).Right).Right).Right;
            var owners = E(contributions).Cast<Relation>().Select(row => tickOwners[row.Left]).ToArray();
            var a = positions[from]; var b = positions[to]; var colour = owners.Length == 1 ? Palette[owners[0].Owner % Palette.Length] : "#884ba1";
            lines.Add(new(a.X, a.Y, b.X, b.Y, owners[0].Drive, colour));
        }
        var account = (Relation)machine.Account().Right; var tail = (Relation)account.Right; var end = (Relation)tail.Right;
        return new(machine, lines, jumps, events, elements.Length, elements.Count(element => element is Carrier or CarrierLocation),
            E(machine.ImportedPackets).Length, E((Relation)account.Left).Length, E((Relation)tail.Left).Length,
            E((Relation)end.Left).Length, E((Relation)end.Right).Length + E(machine.Unresolved).Length);
    }

    private static string Panel(View view, int top)
    {
        var lines = view.Lines.Concat(view.Navigation).ToArray(); var x0 = lines.Min(line => Math.Min(line.X1, line.X2));
        var x1 = lines.Max(line => Math.Max(line.X1, line.X2)); var y0 = lines.Min(line => Math.Min(line.Y1, line.Y2)); var y1 = lines.Max(line => Math.Max(line.Y1, line.Y2));
        var scale = Math.Min(1280 / Math.Max(1, x1 - x0), 110 / Math.Max(1, y1 - y0));
        var x = 55 - x0 * scale; var y = top + 90 + y1 * scale;
        var svg = new StringBuilder($"<g><text x=\"40\" y=\"{top + 12}\" class=\"label\">{H(view.Machine.Name)}</text><text x=\"40\" y=\"{top + 40}\" class=\"note\">{view.Strokes} strokes · {view.Jumps} navigation actions · {view.Imported} packets = {view.Active} stored + {view.Retained} retained + {view.Released} released · {view.Pending} pending</text>");
        foreach (var line in view.Navigation) Add(line, "jump");
        foreach (var line in view.Lines) Add(line, "ink");
        var inputs = view.Machine.Muscles.Where(muscle => E(view.Machine.ExternalPorts).Any(port => ReferenceEquals(port, muscle.Accumulator.InputPort)))
            .Select(muscle => $"{muscle.Name} [{E(muscle.Accumulator.ThresholdSites).Length} sites]");
        var routes = E(view.Machine.Connections).Cast<Relation>().Select(row =>
        {
            var from = view.Machine.Muscles.Single(muscle => ReferenceEquals(muscle.OutputPort, row.Left));
            var to = view.Machine.Muscles.Single(muscle => ReferenceEquals(muscle.Accumulator.InputPort, row.Right));
            return $"{from.Name} → {to.Name} [{E(to.Accumulator.ThresholdSites).Length} sites]";
        }).ToArray();
        var caption = "Inputs: " + string.Join(" + ", inputs) + (routes.Length == 0 ? "" : " · Transfer: " + string.Join("; ", routes));
        svg.Append($"<text x=\"40\" y=\"{top + 230}\" class=\"note\">{H(caption)}</text></g>");
        return svg.ToString();
        void Add(Line line, string cls) => svg.Append($"<path class=\"{cls}\" stroke=\"{line.Colour}\" d=\"M {F(x + line.X1 * scale)} {F(y - line.Y1 * scale)} L {F(x + line.X2 * scale)} {F(y - line.Y2 * scale)}\"><title>Drive {line.Drive}</title></path>");
    }
    private static (double X, double Y) ReadWord(QualitativeMotionFrame frame, Relation word)
    {
        double x = 0, y = 0;
        foreach (var move in E(word).Cast<Relation>())
        {
            if (!frame.Direction(move, out var axis, out var forward, out _)) throw new InvalidOperationException("The chosen view cannot interpret a move.");
            if (ReferenceEquals(axis, frame.Horizontal)) x += forward ? 20 : -20; else y += forward ? 20 : -20;
        }
        return (x, y);
    }
    private static string Describe(QualitativeDrawingReceiver drawing, IElement action) => drawing.TryReadDraw(action, out _) ? "draw" : drawing.TryReadJump(action, out _) ? "jump" : drawing.IsReset(action) ? "reset anchor" : drawing.IsAnchor(action) ? "set anchor" : drawing.TryReadMirror(action, out _) ? "mirror future output" : drawing.TryReadDirectionAction(action, out _) ? "apply direction correspondence" : "unknown";
    private static string F(double value) => value.ToString("0.###", CultureInfo.InvariantCulture);
    private static string H(string value) => WebUtility.HtmlEncode(value);
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
    private static IEnumerable<IElement> Traverse(IElement root)
    {
        var seen = new HashSet<IElement>(ReferenceEqualityComparer.Instance); var stack = new Stack<IElement>(); stack.Push(root);
        while (stack.TryPop(out var item)) { if (!seen.Add(item)) continue; yield return item; if (item is Relation row) { stack.Push(row.Left); stack.Push(row.Right); } }
    }
    private sealed record Line(double X1, double Y1, double X2, double Y2, int Drive, string Colour);
    private sealed record Event(int Drive, string Muscle, int Intake, int Occupied, int Released, string Response);
    private sealed record View(PatternMachine Machine, List<Line> Lines, List<Line> Navigation, List<Event> Events,
        int NativeElements, int Carriers, int Imported, int Active, int Retained, int Released, int Pending)
    { public int Strokes => Lines.Count; public int Jumps => Navigation.Count; }
}
