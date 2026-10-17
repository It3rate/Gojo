using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;
using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;

namespace Gojo.Experiments.QualitativeFrieze;

/// <summary>Measured observation of actual delayed feedback histories. Every
/// coloured response is an accepted local drawing, not a rendered prediction.</summary>
public static class JunctionFeedbackExperiment
{
    private static readonly string[] Colours = ["#18758b", "#9d5833", "#754891", "#427d4c"];
    public static int Run(string[] args)
    {
        var output = Path.Combine("experiments", "QualitativeFrieze", "results", "junction-feedback");
        if (args.Length == 2 && args[0] == "--output") output = args[1];
        else if (args.Length != 0) throw new ArgumentException("Usage: --junction-feedback [--output DIRECTORY]");
        Directory.CreateDirectory(output); var study = new JunctionFeedbackStudy();
        var views = study.Cases.Select(example => Observe(study, example)).ToArray();
        var svg = Gallery(views); File.WriteAllText(Path.Combine(output, "gallery.svg"), svg);
        var details = new StringBuilder();
        foreach (var view in views)
        {
            details.Append($"<details><summary>{H(view.Example.Name)} — local conditional history</summary><p>{H(view.Example.Description)}</p><table><tr><th>Round</th><th>Node</th><th>Question</th><th>New accepted drawing</th><th>Current intake packets</th></tr>");
            foreach (var decision in view.Decisions)
                details.Append($"<tr><td>{decision.Round}</td><td>{decision.Node}</td><td>{H(decision.Answer)}</td><td>{decision.Accepted}</td><td>{decision.Primary} primary / {decision.Auxiliary} auxiliary</td></tr>");
            details.Append("</table></details>");
        }
        File.WriteAllText(Path.Combine(output, "gallery.html"), "<!doctype html><html lang=\"en\"><head><meta charset=\"utf-8\"><meta name=\"viewport\" content=\"width=device-width,initial-scale=1\"><title>Live qualitative junction feedback</title><style>body{max-width:1420px;margin:24px auto;padding:0 16px;background:#f3f0e6;color:#243c44;font:16px Segoe UI,Arial,sans-serif}svg{width:100%;height:auto}p{line-height:1.5}button{padding:9px 14px;margin:0 8px 8px 0}input{width:330px;max-width:60vw}table{border-collapse:collapse;width:100%;font-size:14px}td,th{padding:8px;border-bottom:1px solid #c8cec8;text-align:left}details{padding:14px;margin:18px 0;background:#fff9}summary{cursor:pointer;font-weight:600}.hide-substrate .substrate{display:none}.show-routes .route{display:inline}.mono .response{stroke:#203e47!important}.frame.future{display:none}</style></head><body><p><button id=\"play\">Play rounds</button><button onclick=\"document.body.classList.toggle('hide-substrate')\">Fixed substrate</button><button onclick=\"document.body.classList.toggle('show-routes')\">Feedback routes</button><button onclick=\"document.body.classList.toggle('mono')\">Response colours</button></p><p><label>Event round <input id=\"round\" type=\"range\" min=\"0\" max=\"12\" value=\"12\"><output id=\"counter\">12</output></label></p>" + svg +
            "<p><strong>Accepted junctions now carry conserved neighbour packets into live local conditionals.</strong> Orange rings are packets waiting in a next-round channel. Blue dots are held intake packets; grey dots belong to explicit sinks. Colours identify a node’s actual accepted local response. The substrate and its junction sites remain fixed.</p><p>Every round first delivers only the previous queue, then asks each local law from a shared input snapshot. A definite accepted drawing advances that node’s phase and moves the selected packet identities. New output waits until the next round. The circulating case reuses one actual packet, including when it returns to a previous node.</p><p>Coincidence requires separate neighbour intakes; repeated arrivals on one side cannot fill the other. The reflection pair keeps both seed roles but cuts the auxiliary seam in the control. Its target consequently selects an upward response instead of a downward one. Refractory targets preserve later input instead of silently consuming it.</p><p>This is uncalibrated packet pressure and discrete event scheduling, rather than continuous signed carrier tension or measured elapsed time. Local response traces do not create new contacts automatically. Changing substrate boundary geometry would require a new seam contract.</p>" + details +
            "<p><a href=\"diagnostics.json\">Ownership and causal diagnostics</a> · <a href=\"gallery.svg\">Standalone final-round figure</a></p><script>const slider=document.querySelector('#round'),counter=document.querySelector('#counter');let timer;function show(){counter.textContent=slider.value;document.querySelectorAll('[data-case]').forEach(card=>{const round=Math.min(Number(slider.value),Number(card.dataset.rounds));card.querySelectorAll('.frame').forEach(frame=>frame.classList.toggle('future',Number(frame.dataset.round)!==round))})}slider.addEventListener('input',show);document.querySelector('#play').addEventListener('click',()=>{if(timer){clearInterval(timer);timer=undefined;return}slider.value=0;show();timer=setInterval(()=>{slider.value=Number(slider.value)+1;show();if(Number(slider.value)===12){clearInterval(timer);timer=undefined}},650)});show()</script></body></html>");
        File.WriteAllText(Path.Combine(output, "diagnostics.json"), JsonSerializer.Serialize(new
        {
            Schema = "live-junction-feedback-v1", Scope = "Captured accepted junction substrate; live conserved discrete packets, delayed queues, finite conditional laws and acknowledged local outputs.",
            Cases = views.Select(view => new { view.Example.Name, view.Example.Description, Rounds = view.Example.Rounds,
                ImportedPackets = E(view.Example.Flow.Transport.ImportedPackets).Length, AcceptedResponses = E(view.Example.Flow.Emissions).Length,
                NativeNumericElements = view.NumericElements, view.Pending, Frames = view.Frames.Select(frame => new { frame.Round, frame.Input, frame.Queued, frame.Sink, frame.Responses }), view.Decisions })
        }, new JsonSerializerOptions { WriteIndented = true }));
        foreach (var view in views) Console.WriteLine($"{view.Example.Name}: {E(view.Example.Flow.Transport.ImportedPackets).Length} conserved packets, {E(view.Example.Flow.Emissions).Length} accepted responses; {view.Frames[^1].Input} held + {view.Frames[^1].Queued} queued + {view.Frames[^1].Sink} sink; {view.Pending} pending; {view.NumericElements} native numeric elements.");
        Console.WriteLine($"Wrote {Path.GetFullPath(output)}.");
        return views.Any(view => view.Pending != 0 || view.NumericElements != 0) ? 1 : 0;
    }

    private static View Observe(JunctionFeedbackStudy study, JunctionFeedbackStudy.Case example)
    {
        var flow = example.Flow; var frames = new List<Frame>(); var decisions = new List<Decision>();
        foreach (var row in E(example.Seeds).Cast<Relation>()) flow.Seed(new Placeholder(), (Relation)row.Left, (Relation)row.Right);
        var sites = E(example.Field.SourceBindings).Cast<Relation>().ToDictionary<Relation, IElement, (double X, double Y)>(
            row => row.Left, row => Position(example.Field.SourceGraph.Frame, (Relation)row.Right), ReferenceEqualityComparer.Instance);
        var substrate = E(example.Field.SourceGraph.Strokes).Cast<Relation>().Select(stroke =>
        { QualitativeStrokeGraph.TryReadStroke(stroke, out _, out var a, out var b, out _); return new Line(sites[a], sites[b], "#b5beb9"); }).ToArray();
        var anchors = flow.Nodes.Select(node => Position(example.Field.SourceGraph.Frame, (Relation)node.Anchor.Right)).ToArray();
        var channels = E(flow.Channels).Cast<Relation>().Select(channel =>
        {
            var context = (Relation)channel.Left; var edge = (Relation)context.Left; var ends = (Relation)edge.Left;
            var a = flow.Nodes.Select((node, index) => (node, index)).Single(pair => ReferenceEquals(pair.node.Cell, ends.Left)).index;
            var b = flow.Nodes.Select((node, index) => (node, index)).Single(pair => ReferenceEquals(pair.node.Cell, ends.Right)).index;
            var actualSeam = (Relation)((Relation)edge.Right).Left; var seamSite = ((Relation)actualSeam.Left).Left;
            return new Channel((Relation)channel.Right, a, b, sites[seamSite]);
        }).ToArray();
        frames.Add(Capture(0));
        for (var round = 1; round <= example.Rounds; round++)
        {
            var before = flow.Nodes.Select(node => E(node.Activations).Length).ToArray(); flow.Round(); frames.Add(Capture(round));
            for (var index = 0; index < flow.Nodes.Count; index++)
            {
                var node = flow.Nodes[index]; var question = E(node.Conditions).LastOrDefault() as Relation;
                var answer = question is not null && QualitativeQuestion.TryReadAnswer(question, out var selected)
                    ? ReferenceEquals(selected, node.Hold) ? "hold" : ReferenceEquals(selected, node.Up) ? "upward response" : "downward response" : "unresolved";
                decisions.Add(new(round, ((char)('A' + index)).ToString(), answer, E(node.Activations).Length - before[index],
                    E(flow.Transport.PortPackets(node.InputPort)).Length, E(flow.Transport.PortPackets(node.AuxiliaryPort)).Length));
            }
        }
        var run = flow.Run(0); var native = Traverse(new Relation(study.Surface, new Relation(example.Support, run))).ToArray();
        var account = flow.Transport.Account();
        var accountingPending = account.Right is Relation body && body.Right is Relation pending ? E(pending).Length : 1;
        var unresolved = E(flow.Unresolved).Length + E(flow.Pending).Length + E(flow.DefinitionProblems).Length + E(flow.Transport.Unresolved).Length + accountingPending;
        return new(example, substrate, anchors, channels, frames.ToArray(), decisions.ToArray(), unresolved, native.Count(item => item is Carrier or CarrierLocation));

        Frame Capture(int round)
        {
            var lines = new List<Line>();
            foreach (var (node, index) in flow.Nodes.Select((node, index) => (node, index)))
            {
                var bindings = new Dictionary<IElement, (double X, double Y)>(ReferenceEqualityComparer.Instance);
                foreach (var binding in E(node.Drawing.SiteBindings).Cast<Relation>())
                {
                    var projection = node.Projection.Apply((Relation)binding.Right);
                    if (!node.Projection.TryReadComplete(projection, out var word)) throw new InvalidOperationException("Local trace projection remains unresolved.");
                    var local = Position(example.Field.SourceGraph.Frame, word); var anchor = anchors[index];
                    bindings[binding.Left] = (anchor.X + local.X, anchor.Y + local.Y);
                }
                foreach (var stroke in E(node.Drawing.Strokes).Cast<Relation>())
                { QualitativeStrokeGraph.TryReadStroke(stroke, out _, out var a, out var b, out _); lines.Add(new(bindings[a], bindings[b], Colours[index % Colours.Length])); }
            }
            flow.Transport.TryReadState(flow.Transport.State, out _, out var placements, out _, out _);
            var markers = new List<Marker>(); var input = 0; var queued = 0; var sink = 0;
            foreach (var placement in E(placements).Cast<Relation>())
            {
                var port = ((Relation)placement.Right).Left; var channel = channels.SingleOrDefault(candidate => ReferenceEquals(candidate.Port, port));
                if (channel is not null) { queued++; markers.Add(new(channel.Point, "queued", queued)); continue; }
                var found = flow.Nodes.Select((node, index) => (node, index)).Single(pair => pair.node.Inputs.Any(candidate => ReferenceEquals(candidate, port)) || ReferenceEquals(pair.node.SinkPort, port));
                if (ReferenceEquals(found.node.SinkPort, port)) { sink++; markers.Add(new(anchors[found.index], "sink", sink)); }
                else { input++; markers.Add(new(anchors[found.index], "input", input)); }
            }
            var nativeRead = new Relation(flow.ClockState, new Relation(flow.Transport.State, flow.NodeStates()));
            return new(round, lines.ToArray(), markers.ToArray(), input, queued, sink, E(flow.Emissions).Length, nativeRead);
        }
    }

    private static string Gallery(View[] views)
    {
        var height = 140 + ((views.Length + 1) / 2) * 440;
        var svg = new StringBuilder($"<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 1420 {height}\" role=\"img\" aria-label=\"Three paired live junction feedback experiments\"><style>text{{font-family:Segoe UI,Arial,sans-serif}}.head{{font-size:26px;font-weight:650}}.label{{font-size:17px;font-weight:600}}.note{{font-size:14px;fill:#536b71}}.substrate{{stroke:#b5beb9;stroke-width:2;fill:none}}.response{{stroke-width:6;stroke-linecap:round;fill:none}}.route{{display:none;stroke:#8b7a53;stroke-width:1.3;stroke-dasharray:4 4;fill:none}}.queued{{fill:#fff9e7;stroke:#c57b23;stroke-width:3}}.input{{fill:#157c9c;stroke:#fff;stroke-width:1.5}}.sink{{fill:#737f80;stroke:#fff;stroke-width:1.5}}.node{{fill:#fffdf6;stroke:#557078;stroke-width:1.2}}</style><rect width=\"100%\" height=\"100%\" fill=\"#f3f0e6\"/><text x=\"28\" y=\"40\" class=\"head\">Junctions feeding back into live local flow</text><text x=\"28\" y=\"72\" class=\"note\">Accepted seam → delayed conserved packet → local threshold / question → acknowledged response → next queue</text><text x=\"28\" y=\"99\" class=\"note\">Connected experiment on the left; changed seam or seed on the right. No measured samples or numeric carrier state.</text>");
        for (var index = 0; index < views.Length; index++)
        {
            var view = views[index]; var left = 20 + index % 2 * 700; var top = 125 + index / 2 * 440;
            var points = view.Substrate.Concat(view.Frames.SelectMany(frame => frame.Lines)).SelectMany(line => new[] { line.From, line.To }).Concat(view.Anchors).ToArray();
            var x0 = points.Min(p => p.X); var x1 = points.Max(p => p.X); var y0 = points.Min(p => p.Y); var y1 = points.Max(p => p.Y);
            var scale = Math.Min(560 / Math.Max(1, x1 - x0), 240 / Math.Max(1, y1 - y0));
            var x = left + 60 + (560 - (x1 - x0) * scale) / 2; var y = top + 80 + (240 - (y1 - y0) * scale) / 2;
            (double X, double Y) P((double X, double Y) p) => (x + (p.X - x0) * scale, y + (y1 - p.Y) * scale);
            string Point((double X, double Y) p) { p = P(p); return $"{F(p.X)} {F(p.Y)}"; }
            svg.Append($"<g data-case=\"{index}\" data-rounds=\"{view.Example.Rounds}\"><rect x=\"{left}\" y=\"{top}\" width=\"680\" height=\"420\" rx=\"12\" fill=\"#fffdf5\" stroke=\"#d6dcd3\"/><text x=\"{left + 18}\" y=\"{top + 31}\" class=\"label\">{H(view.Example.Name)}</text><text x=\"{left + 18}\" y=\"{top + 56}\" class=\"note\">{E(view.Example.Flow.Transport.ImportedPackets).Length} packet identities · {view.Example.Rounds} explicit rounds · {view.Pending} pending</text>");
            foreach (var line in view.Substrate) svg.Append($"<path class=\"substrate\" d=\"M {Point(line.From)} L {Point(line.To)}\"/>");
            foreach (var route in view.Channels) svg.Append($"<path class=\"route\" d=\"M {Point(view.Anchors[route.From])} L {Point(route.Point)} L {Point(view.Anchors[route.To])}\"/>");
            for (var node = 0; node < view.Anchors.Length; node++)
            { var p = P(view.Anchors[node]); svg.Append($"<circle class=\"node\" cx=\"{F(p.X)}\" cy=\"{F(p.Y)}\" r=\"8\"/><text x=\"{F(p.X - 18)}\" y=\"{F(p.Y - 12)}\" class=\"note\">{(char)('A' + node)}</text>"); }
            foreach (var frame in view.Frames)
            {
                svg.Append($"<g class=\"frame{(frame.Round == view.Example.Rounds ? "" : " future")}\" data-round=\"{frame.Round}\">");
                foreach (var line in frame.Lines) svg.Append($"<path class=\"response\" stroke=\"{line.Colour}\" d=\"M {Point(line.From)} L {Point(line.To)}\"/>");
                foreach (var marker in frame.Markers)
                {
                    var p = P(marker.Point); var offset = marker.Kind == "sink" ? 12 : 0;
                    svg.Append($"<circle class=\"{marker.Kind}\" cx=\"{F(p.X + offset)}\" cy=\"{F(p.Y + marker.Index % 2 * 7)}\" r=\"5\"><title>Current packet owner: {marker.Kind}</title></circle>");
                }
                svg.Append($"<text x=\"{left + 18}\" y=\"{top + 365}\" class=\"note\">Round {frame.Round}: {frame.Responses} accepted responses</text><text x=\"{left + 18}\" y=\"{top + 393}\" class=\"note\">{frame.Input} held at inputs + {frame.Queued} in delayed queues + {frame.Sink} at sinks</text></g>");
            }
            svg.Append("</g>");
        }
        // Class removal, rather than replacement drawing, selects a witnessed
        // historical frame. The same CSS supports the standalone final view.
        return svg.Append("<style>.frame.future{display:none}</style></svg>").ToString();
    }
    private static (double X, double Y) Position(QualitativeMotionFrame frame, Relation word)
    {
        double x = 0, y = 0;
        foreach (var item in E(word))
        { if (item is not Relation move || !frame.Direction(move, out var axis, out var forward, out _)) throw new InvalidOperationException("Unresolved display word."); if (ReferenceEquals(axis, frame.Horizontal)) x += forward ? 1 : -1; else y += forward ? 1 : -1; }
        return (x, y);
    }
    private static IEnumerable<IElement> Traverse(IElement root)
    {
        var seen = new HashSet<IElement>(ReferenceEqualityComparer.Instance); var stack = new Stack<IElement>(); stack.Push(root);
        while (stack.TryPop(out var item)) { if (!seen.Add(item)) continue; yield return item; if (item is Relation row) { stack.Push(row.Left); stack.Push(row.Right); } }
    }
    private static string H(string value) => WebUtility.HtmlEncode(value);
    private static string F(double value) => value.ToString("0.###", CultureInfo.InvariantCulture);
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
    private sealed record Line((double X, double Y) From, (double X, double Y) To, string Colour);
    private sealed record Channel(Relation Port, int From, int To, (double X, double Y) Point);
    private sealed record Marker((double X, double Y) Point, string Kind, int Index);
    private sealed record Frame(int Round, Line[] Lines, Marker[] Markers, int Input, int Queued, int Sink, int Responses, Relation Native);
    private sealed record Decision(int Round, string Node, string Answer, int Accepted, int Primary, int Auxiliary);
    private sealed record View(JunctionFeedbackStudy.Case Example, Line[] Substrate, (double X, double Y)[] Anchors,
        Channel[] Channels, Frame[] Frames, Decision[] Decisions, int Pending, int NumericElements);
}
