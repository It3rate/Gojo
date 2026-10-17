using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;
using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;

namespace Gojo.Experiments.QualitativeFrieze;

/// <summary>Presentation over actual active motif revisions, their declared
/// seam negotiations and current material owners. Screen coordinates are a
/// selected equal-reach realization of native words, never an input to repair.</summary>
public static class AdaptiveWallpaperExperiment
{
    public static int Run(string[] args)
    {
        var output = Path.Combine("experiments", "QualitativeFrieze", "results", "adaptive-wallpaper");
        if (args.Length == 2 && args[0] == "--output") output = args[1];
        else if (args.Length != 0) throw new ArgumentException("Usage: --adaptive-wallpaper [--output DIRECTORY]");
        Directory.CreateDirectory(output);
        var study = new AdaptiveWallpaperStudy();
        var views = study.Cases.Select(Observe).ToArray();
        var svg = Gallery(views); File.WriteAllText(Path.Combine(output, "gallery.svg"), svg);
        File.WriteAllText(Path.Combine(output, "gallery.html"), Page(svg, views));
        File.WriteAllText(Path.Combine(output, "diagnostics.json"), JsonSerializer.Serialize(new
        {
            Schema = "adaptive-wallpaper-v1",
            Scope = "Finite native motif replacement, explicit non-draining neighbour observations, newly negotiated current-head seams and conserved delayed material passages. No measured energy or trained policy.",
            Cases = views.Select(view => new
            {
                view.Example.Name, view.Example.Description, view.Example.Rounds,
                Nodes = view.Example.Machine.Nodes.Count, Links = view.Example.Machine.Links.Count,
                ImportedPackets = E(view.Example.Machine.Transport.ImportedPackets).Length,
                view.NumericElements, view.Unresolved, view.QuarterTurnPassed, Feasibility = view.Assessment.Summary,
                Frames = view.Frames.Select(frame => new
                {
                    frame.Round, frame.Input, frame.Queued, frame.Received, frame.Accepted, frame.Blocked,
                    frame.Unknown, frame.Revisions, frame.OwnershipPassed, frame.FieldPassed, frame.HistoricalBlockedAttempts,
                    Nodes = frame.NodeStates.Select(node => new { node.Name, node.Variant, node.Revisions, node.Frozen })
                })
            })
        }, new JsonSerializerOptions { WriteIndented = true }));
        foreach (var view in views)
        {
            var last = view.Frames[^1];
            Console.WriteLine($"{view.Example.Name}: {last.Accepted} accepted seams, {last.Blocked} blocked; {last.Revisions} motif replacements; {last.Input} input + {last.Queued} queued + {last.Received} received packets; C4 {view.QuarterTurnPassed}; {view.Unresolved} unresolved; {view.NumericElements} native numeric elements.");
        }
        Console.WriteLine($"Wrote {Path.GetFullPath(output)}.");
        return views.Any(view => view.NumericElements != 0 || view.Unresolved != 0 || view.Frames.Any(frame => !frame.OwnershipPassed || !frame.FieldPassed)) ? 1 : 0;
    }

    private static View Observe(AdaptiveWallpaperStudy.Case example)
    {
        var machine = example.Machine; var frames = new List<Frame>();
        foreach (var seed in E(example.Seeds).Cast<Relation>())
        {
            var link = machine.Links.Single(link => ReferenceEquals(link.Surface, seed.Left));
            machine.Seed(link, seed.Right);
        }
        frames.Add(Capture(0));
        for (var round = 1; round <= example.Rounds; round++) { machine.Round(); frames.Add(Capture(round)); }
        var symmetry = new AdaptiveWallpaperSymmetry(machine); var symmetryRead = symmetry.Evaluate();
        var quarterTurnPassed = symmetry.TryReadPassed(symmetryRead);
        var assessment = new AdaptiveWallpaperAssessment(machine);
        var support = new Relation(example.Support,
            new Relation(machine.Timeline, new Relation(S(frames.Select(frame => (IElement)frame.Native).ToArray()),
                new Relation(new Relation(symmetry.Surface, symmetryRead),
                    new Relation(assessment.Surface, new Relation(machine.Snapshot(), machine.Transport.Account()))))));
        var numeric = Traverse(support).Count(item => item is Carrier or CarrierLocation);
        var unresolved = E(machine.Unresolved).Length + E(machine.DefinitionProblems).Length + E(machine.Transport.Unresolved).Length +
            machine.Nodes.Sum(node => E(node.Unresolved).Length + E(node.Pending).Length);
        return new(example, frames.ToArray(), quarterTurnPassed, unresolved, numeric, assessment, support);

        Frame Capture(int round)
        {
            var field = machine.Field(); var read = field.Evaluate(); var fieldPassed = field.TryReadPassed(read);
            var bindings = Positions(machine.Grammar.Frame, field.Bindings);
            var colours = new Dictionary<Relation, string>(ReferenceEqualityComparer.Instance);
            foreach (var node in machine.Nodes)
                foreach (var stroke in E(node.Current.Graph.Strokes).Cast<Relation>())
                    colours[stroke] = ReferenceEquals(node.Current.Template, machine.Grammar.Plus) ? "#225b63" : "#a85f35";
            var lines = new List<Line>();
            foreach (var output in E(field.Graph.Strokes).Cast<Relation>())
            {
                QualitativeStrokeGraph.TryReadStroke(output, out var source, out var a, out var b, out _);
                lines.Add(new(bindings[a], bindings[b], colours[(Relation)source]));
            }
            var renderedPaths = new List<DrawingPath>();
            var strokeMap = E(field.StrokeMap).Cast<Relation>().ToDictionary<Relation, Relation, Relation>(
                row => (Relation)row.Left, row => (Relation)row.Right, ReferenceEqualityComparer.Instance);
            foreach (var node in machine.Nodes)
            {
                var sourceStrokes = E(node.Current.Graph.Strokes).Cast<Relation>().ToArray(); var offset = 0;
                foreach (var path in E(node.Current.Template.Paths).Cast<Relation>())
                {
                    var count = E((Relation)path.Right).Length; var points = new List<(double X, double Y)>();
                    IElement? start = null; IElement? previous = null;
                    foreach (var source in sourceStrokes.Skip(offset).Take(count))
                    {
                        QualitativeStrokeGraph.TryReadStroke(strokeMap[source], out _, out var a, out var b, out _);
                        if (start is null) { start = a; points.Add(bindings[a]); }
                        else if (!ReferenceEquals(previous, a)) throw new InvalidOperationException("A rendered path must follow actual consecutive sites.");
                        points.Add(bindings[b]); previous = b;
                    }
                    offset += count;
                    renderedPaths.Add(new(points.ToArray(), ReferenceEquals(start, previous), colours[sourceStrokes[offset - 1]]));
                }
            }
            var ghosts = new List<Line>();
            foreach (var node in machine.Nodes)
            {
                var historyBindings = Positions(machine.Grammar.Frame, node.Drawing.SiteBindings);
                foreach (var stroke in E(node.Drawing.Strokes).Cast<Relation>())
                {
                    if (colours.ContainsKey(stroke)) continue;
                    QualitativeStrokeGraph.TryReadStroke(stroke, out _, out var a, out var b, out _);
                    ghosts.Add(new(historyBindings[a], historyBindings[b], "#7f9290"));
                }
            }
            var seams = new List<Seam>(); var accepted = 0; var blocked = 0; var unknown = 0;
            foreach (var link in machine.Links)
            {
                var agreement = E(machine.CurrentSeams).Cast<Relation>().Single(agreement =>
                    agreement.Left is Relation { Left: Relation context } && ReferenceEquals(context.Right, link.Surface));
                var state = machine.TryReadAccepted(agreement, out _) ? "accepted" : KnownMismatch(agreement) ? "blocked" : "unknown";
                if (state == "accepted") accepted++; else if (state == "blocked") blocked++; else unknown++;
                if (!link.From.Current.TryReadPort(link.FromRole, out _, out var a) || !link.To.Current.TryReadPort(link.ToRole, out _, out var b)) continue;
                seams.Add(new(Position(machine.Grammar.Frame, (Relation)a.Right), Position(machine.Grammar.Frame, (Relation)b.Right), state, link));
            }
            machine.Transport.TryReadState(machine.Transport.State, out _, out var placements, out var imported, out _);
            var input = 0; var queued = 0; var received = 0; var markers = new List<Marker>();
            foreach (var placement in E(placements).Cast<Relation>())
            {
                var port = ((Relation)placement.Right).Left;
                var seam = seams.Single(seam => ReferenceEquals(seam.Link.InputPort, port) || ReferenceEquals(seam.Link.QueuePort, port) || ReferenceEquals(seam.Link.TargetPort, port));
                if (ReferenceEquals(port, seam.Link.InputPort)) { input++; markers.Add(new(seam.From, "input")); }
                else if (ReferenceEquals(port, seam.Link.QueuePort))
                { queued++; markers.Add(new(((seam.From.X + seam.To.X) / 2, (seam.From.Y + seam.To.Y) / 2), "queued")); }
                else { received++; markers.Add(new(seam.To, "received")); }
            }
            var account = machine.Transport.Account();
            var accountPending = account.Right is Relation { Right: Relation pending } ? E(pending).Length : 1;
            var ownershipPassed = accountPending == 0 && input + queued + received == E(imported).Length &&
                E(placements).Cast<Relation>().Select(row => row.Left).Distinct(ReferenceEqualityComparer.Instance).Count() == E(imported).Length;
            var nodeStates = machine.Nodes.Select(node => new NodeState(node.Name,
                ReferenceEquals(node.Current.Template, machine.Grammar.Plus) ? "positive handedness" : "reflected handedness",
                E(node.Replacements).Length, E(example.FrozenCells).Any(cell => ReferenceEquals(cell, node.Cell)),
                Position(machine.Grammar.Frame, node.AnchorWord))).ToArray();
            var native = new Relation(machine.Snapshot(), new Relation(machine.Transport.State,
                new Relation(new Relation(field.Surface, read), new Relation(machine.CurrentSeams,
                    new Relation(machine.CurrentUnknown, new Relation(account, machine.Timeline))))));
            return new(round, lines.ToArray(), renderedPaths.ToArray(), ghosts.ToArray(), seams.ToArray(), markers.ToArray(), nodeStates,
                input, queued, received, accepted, blocked, unknown + E(machine.CurrentUnknown).Length,
                nodeStates.Sum(node => node.Revisions), E(machine.Blocked).Length, ownershipPassed, fieldPassed, native);
        }

        bool KnownMismatch(Relation agreement) => agreement.Right is Relation { Right: Relation { Right: Relation equality } } &&
            machine.Grammar.Frame.TryReadResult(equality, out var rest, out _, out var pending) &&
            StructuralConstraint.TryReadScope(rest, out var terms, out var seam, out var tail) && seam.Equals(tail) && terms.Length != 0 && E(pending).Length == 0;
    }

    private static string Gallery(View[] views)
    {
        const int width = 1640; const int cardWidth = 790; const int cardHeight = 785;
        var svg = new StringBuilder($"<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 {width} 1770\" role=\"img\" aria-label=\"Four qualitative wallpaper machines adapting motifs and renegotiating live junctions\"><style>text{{font-family:Segoe UI,Arial,sans-serif}}.head{{font-size:29px;font-weight:650;fill:#213f47}}.label{{font-size:21px;font-weight:600;fill:#213f47}}.note{{font-size:15px;fill:#597078}}.motif{{fill:none;stroke-linecap:butt;stroke-linejoin:miter}}.ghost{{display:none;fill:none;stroke:#788b89;opacity:.22}}.seam.accepted{{display:none;fill:#1e9b88;stroke:#fffdf5;stroke-width:1.2}}.blocked-path{{fill:none;stroke:#cf4540;stroke-width:2;stroke-dasharray:3 3}}.seam.blocked{{fill:#fff8ee;stroke:#cf4540;stroke-width:2}}.seam.unknown{{fill:#f8e4ff;stroke:#8b4bb1;stroke-width:2}}.queued{{fill:#fff4d9;stroke:#be7a1c;stroke-width:2.2}}.input{{fill:#367baf;stroke:#fffdf5;stroke-width:1}}.received{{display:none;fill:#39798e;stroke:#fffdf5;stroke-width:1}}.frozen{{fill:none;stroke:#b19245;stroke-width:1.6;stroke-dasharray:2 2}}.frame.future{{display:none}}</style><rect width=\"100%\" height=\"100%\" fill=\"#f2efe5\"/><text x=\"28\" y=\"45\" class=\"head\">Motifs adapt; junctions negotiate again</text><text x=\"28\" y=\"79\" class=\"note\">Neighbour offer → local question → complete replacement motif → new boundary publications → next-round material acceptance</text><text x=\"28\" y=\"106\" class=\"note\">Shared unmeasured reaches; exact port and packet identities. Red pairs remain blocked. Orange rings retain queued packets.</text>");
        for (var index = 0; index < views.Length; index++)
        {
            var view = views[index]; var left = 20 + index % 2 * 810; var top = 135 + index / 2 * 805;
            var all = view.Frames.SelectMany(frame => frame.Lines).SelectMany(line => new[] { line.From, line.To }).ToArray();
            var minX = all.Min(point => point.X); var maxX = all.Max(point => point.X); var minY = all.Min(point => point.Y); var maxY = all.Max(point => point.Y);
            var scale = Math.Min(32, Math.Min(700 / Math.Max(1, maxX - minX), 565 / Math.Max(1, maxY - minY)));
            var x0 = left + (cardWidth - (maxX - minX) * scale) / 2; var y0 = top + 105 + (565 - (maxY - minY) * scale) / 2;
            (double X, double Y) P((double X, double Y) p) => (x0 + (p.X - minX) * scale, y0 + (maxY - p.Y) * scale);
            string Point((double X, double Y) p) { p = P(p); return $"{F(p.X)} {F(p.Y)}"; }
            svg.Append($"<g data-case=\"{index}\" data-rounds=\"{view.Example.Rounds}\"><rect x=\"{left}\" y=\"{top}\" width=\"{cardWidth}\" height=\"{cardHeight}\" rx=\"14\" fill=\"#fffdf5\" stroke=\"#d7dbd0\"/><text x=\"{left + 22}\" y=\"{top + 35}\" class=\"label\">{H(view.Example.Name)}</text><text x=\"{left + 22}\" y=\"{top + 65}\" class=\"note\">{view.Example.Machine.Nodes.Count} local motifs · {view.Example.Machine.Links.Count} declared passages · {(view.QuarterTurnPassed ? "final field C4 verified" : "paired local repair")}</text>");
            foreach (var frame in view.Frames)
            {
                svg.Append($"<g class=\"frame{(frame.Round == view.Example.Rounds ? "" : " future")}\" data-round=\"{frame.Round}\">");
                foreach (var line in frame.Ghosts) svg.Append($"<path class=\"ghost\" stroke-width=\"{F(scale * .24)}\" d=\"M {Point(line.From)} L {Point(line.To)}\"/>");
                foreach (var path in frame.Paths)
                    svg.Append($"<path class=\"motif\" stroke=\"{path.Colour}\" stroke-width=\"{F(scale * .34)}\" d=\"M {Point(path.Points[0])} {string.Join(" ", path.Points.Skip(1).Select(point => "L " + Point(point)))}{(path.Closed ? " Z" : "")}\"/>");
                foreach (var node in frame.NodeStates.Where(node => node.Frozen))
                { var p = P(node.Anchor); svg.Append($"<circle class=\"frozen\" cx=\"{F(p.X)}\" cy=\"{F(p.Y)}\" r=\"{F(scale * .65)}\"><title>{H(node.Name)}: local policy holds this variant</title></circle>"); }
                foreach (var seam in frame.Seams)
                {
                    if (seam.State == "accepted") Dot(seam.From, "seam accepted", 3, "Current accepted seam between actual port sites");
                    else
                    {
                        svg.Append($"<path class=\"blocked-path\" d=\"M {Point(seam.From)} L {Point(seam.To)}\"><title>Incompatible current boundary offers; no material crossing</title></path>");
                        Dot(seam.From, "seam " + seam.State, 3.5, "Actual source boundary port"); Dot(seam.To, "seam " + seam.State, 3.5, "Separate target boundary port");
                    }
                }
                foreach (var marker in frame.Markers)
                    Dot(marker.Point, marker.Kind, marker.Kind == "queued" ? 5 : 3, "Current conserved packet owner: " + marker.Kind);
                svg.Append($"<text x=\"{left + 22}\" y=\"{top + 704}\" class=\"note\">Round {frame.Round} · {frame.Revisions} accepted motif replacements</text><text x=\"{left + 22}\" y=\"{top + 733}\" class=\"note\">{frame.Accepted} accepted seams · {frame.Blocked} blocked · {frame.Unknown} unresolved</text><text x=\"{left + 22}\" y=\"{top + 762}\" class=\"note\">Packet owners: {frame.Input} input + {frame.Queued} queued + {frame.Received} received</text></g>");
                void Dot((double X, double Y) point, string css, double radius, string title)
                { var p = P(point); svg.Append($"<circle class=\"{css}\" cx=\"{F(p.X)}\" cy=\"{F(p.Y)}\" r=\"{F(radius)}\"><title>{H(title)}</title></circle>"); }
            }
            svg.Append("</g>");
        }
        return svg.Append("</svg>").ToString();
    }

    private static string Page(string svg, View[] views)
    {
        var details = new StringBuilder();
        foreach (var view in views)
        {
            details.Append($"<details><summary>{H(view.Example.Name)} — witnessed rounds</summary><p>{H(view.Example.Description)}</p><p>Final categorical feasibility: {view.Assessment.Summary.Passed} hard criteria passed, {view.Assessment.Summary.Violations} violated, {view.Assessment.Summary.Unknown} unresolved. This is a structural admission read, not a beauty score.</p><table><thead><tr><th>Round</th><th>Replacements</th><th>Accepted / blocked / unresolved seams</th><th>Input / queue / received owners</th><th>Accounting / active field</th></tr></thead><tbody>");
            foreach (var frame in view.Frames) details.Append($"<tr><td>{frame.Round}</td><td>{frame.Revisions}</td><td>{frame.Accepted} / {frame.Blocked} / {frame.Unknown}</td><td>{frame.Input} / {frame.Queued} / {frame.Received}</td><td>{(frame.OwnershipPassed ? "complete ownership" : "open ownership")} / {(frame.FieldPassed ? "exact quotient" : "unresolved quotient")}</td></tr>");
            details.Append("</tbody></table></details>");
        }
        return "<!doctype html><html lang=\"en\"><head><meta charset=\"utf-8\"><meta name=\"viewport\" content=\"width=device-width,initial-scale=1\"><title>Adaptive qualitative wallpaper machines</title><style>body{max-width:1640px;margin:24px auto;padding:0 16px;background:#f2efe5;color:#243e46;font:16px Segoe UI,Arial,sans-serif}svg{width:100%;height:auto}p{line-height:1.6;max-width:1250px}button{padding:10px 14px;margin:0 8px 10px 0;border:1px solid #bfc8bf;border-radius:6px;background:#fffdf5;color:#243e46;cursor:pointer}button[aria-pressed=true]{background:#d9e9e4}input{width:430px;max-width:65vw;vertical-align:middle}output{font-weight:600;padding-left:10px}table{border-collapse:collapse;width:100%;font-size:14px}th,td{padding:9px 12px;text-align:left;border-bottom:1px solid #d6dcd2}details{margin:20px 0;padding:18px;background:#fff8;border-radius:8px}summary{font-weight:600;cursor:pointer}.show-ghosts .ghost{display:inline}.show-accepted .seam.accepted{display:inline}.show-received .received{display:inline}.hide-packets .queued,.hide-packets .input,.hide-packets .received{display:none!important}.mono .motif{stroke:#253f45!important}</style></head><body><p><button id=\"play\">Play rounds</button><button data-toggle=\"show-ghosts\" aria-pressed=\"false\">Superseded motifs</button><button data-toggle=\"show-accepted\" aria-pressed=\"false\">Accepted junctions</button><button data-toggle=\"show-received\" aria-pressed=\"false\">Received packets</button><button data-toggle=\"hide-packets\" aria-pressed=\"false\">Hide packet markers</button><button data-toggle=\"mono\" aria-pressed=\"false\">Single ink colour</button></p><p><label>Event round <input id=\"round\" type=\"range\" min=\"0\" max=\"6\" value=\"6\"><output id=\"counter\">6</output></label></p>" + svg +
            "<p><strong>The motif geometry now changes in response to neighbouring offers.</strong> Each local question selects an actual reusable path program. A replacement is published only after its complete drawing has been accepted. Its old ink remains available as superseded history; the current field contains the active motifs and the junctions justified by their current port receipts.</p><p>Teal and copper distinguish the two mirrored variants. A dashed gold ring marks a locally held policy. Red connectors join two separate incompatible boundary offers; orange rings show their waiting material packets. The last field deliberately retains four blocked central seams. Finite outer boundaries have no promised neighbour and create no missing-connection error.</p><p>Round zero has seeded inputs and no queued output. Each later round first tests the preceding queue against that round’s exact motif heads, then exports available input and reads neighbour advertisements from one shared snapshot. A newly repaired seam can deliver on the next round. Repair observations are explicitly non-draining: these examples demonstrate control signals and conserved packets, without an energetic accounting of drawing work.</p><p>All source paths, repeated reaches, mirrored correspondences, replacement histories, seam proofs and packet owners are pre-numeric relations. This display chooses an equal-step realization. It does not supply measured lengths, energy, elapsed time, autonomous scheduling or learned policies.</p>" + details +
            "<details><summary>Where training can enter next</summary><p>Genetic search can propose path words, observation routes and finite policy rows. A transformer can propose the same structural edits from a question and the native failure witnesses. A market can allocate scarce query or revision capacity through explicit bids, accepted transfers and costs. Each proposal would pass through these existing publication, drawing, seam and ownership checks. None of those learning systems is running in this gallery.</p><p>Keep closure, valid current-head receipts, single ownership and visible blocked participation as hard constraints. Useful objectives include broader seam agreement, symmetry with an actual correspondence proof, limited revision work and interesting motif/field balance. These are separate objectives: four stubborn seams here preserve a symmetric conflict, and visual regularity alone does not settle that conflict.</p></details><p><a href=\"diagnostics.json\">Round-by-round ownership and seam diagnostics</a> · <a href=\"gallery.svg\">Standalone final-round figure</a></p><script>const slider=document.querySelector('#round'),counter=document.querySelector('#counter'),play=document.querySelector('#play');let timer;function show(){counter.textContent=slider.value;document.querySelectorAll('[data-case]').forEach(card=>{const round=Math.min(Number(slider.value),Number(card.dataset.rounds));card.querySelectorAll('.frame').forEach(frame=>frame.classList.toggle('future',Number(frame.dataset.round)!==round))})}function stop(){clearInterval(timer);timer=undefined;play.textContent='Play rounds'}slider.addEventListener('input',()=>{stop();show()});play.addEventListener('click',()=>{if(timer){stop();return}slider.value=0;show();play.textContent='Pause';timer=setInterval(()=>{slider.value=Number(slider.value)+1;show();if(Number(slider.value)===6)stop()},900)});document.querySelectorAll('[data-toggle]').forEach(button=>button.addEventListener('click',()=>{const active=document.body.classList.toggle(button.dataset.toggle);button.setAttribute('aria-pressed',String(active))}));show()</script></body></html>";
    }

    private static Dictionary<IElement, (double X, double Y)> Positions(QualitativeMotionFrame frame, Relation bindings) =>
        E(bindings).Cast<Relation>().ToDictionary<Relation, IElement, (double X, double Y)>(row => row.Left, row => Position(frame, (Relation)row.Right), ReferenceEqualityComparer.Instance);
    private static (double X, double Y) Position(QualitativeMotionFrame frame, Relation word)
    {
        double x = 0, y = 0;
        foreach (var move in E(word).Cast<Relation>())
        {
            if (!frame.Direction(move, out var axis, out var forward, out _)) throw new InvalidOperationException("An unresolved word has no exact display realization.");
            if (ReferenceEquals(axis, frame.Horizontal)) x += forward ? 1 : -1; else y += forward ? 1 : -1;
        }
        return (x, y);
    }
    private static IEnumerable<IElement> Traverse(IElement root)
    {
        var seen = new HashSet<IElement>(ReferenceEqualityComparer.Instance); var stack = new Stack<IElement>(); stack.Push(root);
        while (stack.TryPop(out var item)) { if (!seen.Add(item)) continue; yield return item; if (item is Relation row) { stack.Push(row.Left); stack.Push(row.Right); } }
    }
    private static string H(string text) => WebUtility.HtmlEncode(text);
    private static string F(double value) => value.ToString("0.###", CultureInfo.InvariantCulture);
    private static Relation S(params IElement[] entries) => StructuralConstraint.Closed(entries);
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
    private sealed record Line((double X, double Y) From, (double X, double Y) To, string Colour);
    private sealed record DrawingPath((double X, double Y)[] Points, bool Closed, string Colour);
    private sealed record Seam((double X, double Y) From, (double X, double Y) To, string State, AdaptiveMotifLink Link);
    private sealed record Marker((double X, double Y) Point, string Kind);
    private sealed record NodeState(string Name, string Variant, int Revisions, bool Frozen, (double X, double Y) Anchor);
    private sealed record Frame(int Round, Line[] Lines, DrawingPath[] Paths, Line[] Ghosts, Seam[] Seams, Marker[] Markers, NodeState[] NodeStates,
        int Input, int Queued, int Received, int Accepted, int Blocked, int Unknown, int Revisions, int HistoricalBlockedAttempts,
        bool OwnershipPassed, bool FieldPassed, Relation Native);
    private sealed record View(AdaptiveWallpaperStudy.Case Example, Frame[] Frames, bool QuarterTurnPassed, int Unresolved, int NumericElements,
        AdaptiveWallpaperAssessment Assessment, Relation Native);
}
