using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;
using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;

namespace Gojo.Experiments.QualitativeFrieze;

/// <summary>Presentation over accepted native drawing and sampling histories.
/// Host coordinates serve only this optional view; they do not drive a recipe,
/// discharge a completion goal, merge sites or establish metric geometry.</summary>
public static class PrimitiveFlowExperiment
{
    private static readonly string[] Palette = ["#176f76", "#bc633e", "#785993", "#517643", "#a27724"];

    public static int Run(string[] args)
    {
        var output = Path.Combine("experiments", "QualitativeFrieze", "results", "primitive-flows");
        for (var i = 0; i < args.Length; i++)
            if (args[i] == "--output" && i + 1 < args.Length) output = args[++i];
            else throw new ArgumentException("Usage: --primitive-flows [--output DIRECTORY]");
        Directory.CreateDirectory(output);
        var views = PrimitiveFlowLibrary.Create().Select(Observe).ToList();
        var study = new PrimitiveSamplingStudy(); var capture = study.Capture(12);
        views.Add(ObserveSampling("P17", "Zigzag / joint observation", "One root sample combines the actual forward and alternating transverse applications. Compare P06: a diagonal-turn recipe reaches the same family by another route.", capture.Combined));
        views.Add(ObserveSampling("P18", "Square wave / separate observation", "The exact same network, emissions, starting phases and reaches as P17. Sampling each component exposes its intermediate corner; source events are unchanged.", capture.Separated));
        views.Add(ObserveSampling("P19", "Terraces / slower transverse recurrence", "A different root phase graph fires the transverse drive on only one of its two phases. The intervening horizontal passage is known, not an unknown emission interpreted as a pause.", capture.Slower));
        var cards = string.Join("\n", views.Select(Card));
        File.WriteAllText(Path.Combine(output, "gallery.html"), Html(cards));
        File.WriteAllText(Path.Combine(output, "gallery.svg"), Sheet(views));
        File.WriteAllText(Path.Combine(output, "diagnostics.json"), JsonSerializer.Serialize(new
        {
            Schema = "gojo-primitive-flows-v1",
            Scope = "Accepted native actuator histories and finite phase-network observations; optional display pitches only.",
            SamplingSharesExactNetwork = ReferenceEquals(study.Combined.Network, study.Separated.Network),
            SamplingSharesExactOutputLaw = ReferenceEquals(study.Combined.OutputLaw, study.Separated.OutputLaw),
            SamplingSharesExactStarts = ReferenceEquals(study.Combined.Starts, study.Separated.Starts),
            SamplingSharesExactReaches = ReferenceEquals(study.Combined.Reaches, study.Separated.Reaches),
            Recipes = views.Select(view => new
            {
                view.Id, view.Name, view.Kind, view.Basis, view.GoalMet, view.Requested, view.Completed,
                Strokes = view.Lines.Count, Navigation = view.Navigation.Count, view.Pending,
                view.NativeElements, view.NativeNumbers, view.ClosureExpected, view.ClosureAccepted, view.ClosureBlocked, view.ClosureUnresolved,
                view.Packets, view.OmittedSegments, view.LayerCount,
                NativeSource = "Retained in executable relation surfaces; element IDs below are local display references only."
            }),
            Limits = new[]
            {
                "Finite packet participation is not continuous signed carrier tension.",
                "Formal displacement equality does not create shared-site identity; declared contour joins have separate witnesses.",
                "Equal-axis, triangular and stretched coordinates are optional render interpretations, not native measurements.",
                "Atlas drawings are reference targets; these images derive actual accepted output histories.",
                "A reached completion goal verifies the requested operational traversal, not a universal aesthetic score or automatic recognition."
            }
        }, new JsonSerializerOptions { WriteIndented = true }));
        foreach (var view in views)
            Console.WriteLine($"{view.Id} {view.Name}: {view.Lines.Count} strokes; completion {view.Completed}/{view.Requested}; {view.Pending} pending; {view.NativeNumbers} native numeric elements; joins {view.ClosureAccepted} accepted / {view.ClosureBlocked} blocked.");
        Console.WriteLine($"Wrote {Path.GetFullPath(output)}.");
        return views.Any(view => !view.GoalMet || view.Pending != 0 || view.NativeNumbers != 0 ||
            view.ClosureBlocked != 0 || view.ClosureUnresolved != 0 || view.ClosureExpected && view.ClosureAccepted == 0 || !view.Packets.Accounted) ? 1 : 0;
    }

    private static View Observe(PrimitiveFlowRecipe recipe)
    {
        var run = recipe.Run(); var closure = recipe.ClosureView();
        var source = new Relation(run, closure.Surface); var native = Traverse(source).ToArray();
        var ids = new NativeIds(native); var nodes = recipe.Nodes().ToArray();
        var leaves = nodes.OfType<PrimitiveFlowLeaf>().ToArray();
        var owners = new Dictionary<IElement, int>(ReferenceEqualityComparer.Instance);
        var navigation = new List<Line>();
        foreach (var (leaf, index) in leaves.Select((leaf, index) => (leaf, index)))
        foreach (var acknowledgment in E(leaf.Timeline).OfType<Relation>())
        {
            if (acknowledgment.Left is not Relation { Right: Relation receipt } ||
                !recipe.Drawing.TryReadStep(receipt, out var before, out var offer, out var after, out _, out _, out _)) continue;
            owners.TryAdd(offer, index);
            if (!recipe.Drawing.TryReadState(before, out _, out var from, out _, out _, out _, out _, out _) ||
                !recipe.Drawing.TryReadState(after, out _, out var to, out _, out _, out _, out _, out _) ||
                ReferenceEquals(from.Left, to.Left)) continue;
            if (acknowledgment.Left is not Relation { Left: Relation { Right: Relation request } } ||
                !leaf.Sequence.TryReadRequest(request, out var action, out _, out _) ||
                !recipe.Drawing.TryReadJump(action, out _) && !recipe.Drawing.IsReset(action)) continue;
            if (TryWord(recipe.Drawing.Frame, (Relation)from.Right, out var first) &&
                TryWord(recipe.Drawing.Frame, (Relation)to.Right, out var second))
                navigation.Add(new(first.X, first.Y, second.X, second.Y, "#b88947", $"navigation from {ids.Of(from.Left)} to {ids.Of(to.Left)}"));
        }
        var positions = new Dictionary<IElement, Point>(ReferenceEqualityComparer.Instance);
        var omitted = 0;
        foreach (var binding in E(recipe.Drawing.SiteBindings).OfType<Relation>())
            if (recipe.Drawing.TryReadBinding(binding, out var site, out var word) &&
                TryWord(recipe.Drawing.Frame, word, out var position)) positions.Add(site, position);
        var lines = new List<Line>();
        foreach (var stroke in E(recipe.Drawing.Strokes).OfType<Relation>())
        {
            if (!QualitativeStrokeGraph.TryReadStroke(stroke, out var cause, out var from, out var to, out var chord) ||
                !positions.TryGetValue(from, out var a) || !positions.TryGetValue(to, out var b)) { omitted++; continue; }
            var colour = owners.TryGetValue(cause, out var owner) ? Palette[owner % Palette.Length] : Palette[0];
            lines.Add(new(a.X, a.Y, b.X, b.Y, colour,
                $"stroke {ids.Of(stroke)} · sites {ids.Of(from)} → {ids.Of(to)} · {DescribeWord(recipe.Drawing.Frame, chord, ids)}"));
        }
        var packetRows = leaves.Select(leaf => Account(leaf, ids)).ToArray();
        var packets = Sum(packetRows);
        var pending = nodes.Sum(node => E(node.Pending).Length + E(node.Unresolved).Length) + packets.Unresolved + omitted;
        var details = new StringBuilder("<p>Each call row selects exposed child input and completion ports. A parent advances only after an authenticated child completion. Reused children keep one state and packet account.</p><div class=layers>");
        foreach (var node in nodes)
        {
            var nodeId = ids.Of(node.Surface);
            details.Append($"<section class=layer id=\"{recipe.Id}-{nodeId}\"><h4><code>{nodeId}</code> {H(node.Name)} <span>{(node is PrimitiveFlowGroup ? "group" : "actuator")}</span></h4><p class=small>Input <code>{ids.Of(node.InputPort)}</code> · done <code>{ids.Of(node.CompletionPort)}</code> · {E(node.CompletionEvents).Length} accepted completions</p>");
            if (node is PrimitiveFlowGroup group)
            {
                var rows = E(group.CallRows).OfType<Relation>().ToArray();
                details.Append("<ol>");
                for (var index = 0; index < group.Children.Count; index++)
                {
                    var child = group.Children[index]; var row = rows[index];
                    details.Append($"<li><a href=\"#{recipe.Id}-{ids.Of(child.Surface)}\">{H(child.Name)} <code>{ids.Of(child.Surface)}</code></a><br><span class=small>call <code>{ids.Of(row)}</code> · phase <code>{ids.Of(row.Left)}</code> · <code>{ids.Of(child.InputPort)}</code> → <code>{ids.Of(child.CompletionPort)}</code></span></li>");
                }
                details.Append("</ol>");
            }
            else if (node is PrimitiveFlowLeaf leaf)
            {
                details.Append("<ol>");
                foreach (var row in E(leaf.Sequence.ActionRows).OfType<Relation>())
                    details.Append($"<li><code>{ids.Of(row)}</code> {H(DescribeAction(recipe.Drawing, row.Right, ids))}</li>");
                details.Append("</ol>");
                var account = packetRows.Single(row => ReferenceEquals(row.Leaf, leaf));
                details.Append($"<p class=small>Packets: {account.Imported} imported = {account.Active} held + {account.Retained} retained + {account.Released} released; {account.Unresolved} unresolved ownership reads.</p>");
            }
            details.Append("</section>");
        }
        details.Append("</div>");
        return new(recipe.Id, recipe.Name, recipe.Read, recipe.Atlas, recipe.DisplayBasis, "acknowledged call/return",
            lines, navigation, recipe.GoalMet, E((Relation)recipe.Goal.Right).Length, E(recipe.Root.CompletionEvents).Length,
            pending, native.Length, native.Count(element => element is Carrier or CarrierLocation),
            recipe.ClosureNodes.Count != 0, E(closure.Accepted).Length, E(closure.Blocked).Length, E(closure.Unresolved).Length,
            packets, omitted, nodes.Length, details.ToString());
    }

    private static View ObserveSampling(string id, string name, string read, PrimitiveSamplingRun run)
    {
        var native = Traverse(run.NativeSource).ToArray(); var ids = new NativeIds(native);
        var lines = new List<Line>(); var omitted = 0;
        if (!run.Fixture.TryReadTrace(run.Trace, out _, out _, out var steps, out _, out var samples, out _))
            throw new InvalidOperationException("A native sampling trace lost its observation contract.");
        foreach (var sample in E(samples).OfType<Relation>())
        {
            if (!FriezeFixture.TryReadSample(sample, out _, out _, out var before, out var after, out var apps, out _) ||
                !TryWord(run.Fixture.Frame, before, out var a) || !TryWord(run.Fixture.Frame, after, out var b)) { omitted++; continue; }
            var colour = E(apps).Length > 1 ? Palette[2] : E(apps).OfType<Relation>().Any(app =>
                app.Right is Relation move && run.Fixture.Frame.Direction(move, out var axis, out _, out _) &&
                ReferenceEquals(axis, run.Fixture.Frame.Vertical)) ? Palette[1] : Palette[0];
            lines.Add(new(a.X, a.Y, b.X, b.Y, colour,
                $"sample {ids.Of(sample)} · {E(apps).Length} actual applications · fresh occurrence history"));
        }
        var details = new StringBuilder($"<p>Source network <code>{ids.Of(run.Fixture.Network.Surface)}</code> · output law <code>{ids.Of(run.Fixture.OutputLaw)}</code> · start bindings <code>{ids.Of(run.Fixture.Starts)}</code>.</p><p>P17 and P18 share those exact source references. Their fresh execution histories remain independently identified. This observer uses no material packet imports.</p><ol>");
        foreach (var route in E(run.Fixture.Sampling).OfType<Relation>())
            details.Append($"<li>Sample {(ReferenceEquals(route.Left, run.Fixture.Network.Surface) ? "completed root" : "component port")} <code>{ids.Of(route.Left)}</code> → receiver <code>{ids.Of(route.Right)}</code></li>");
        details.Append("</ol><p class=small>Sample chords in actual commit order:</p><div class=word-list>");
        foreach (var word in E(run.SampleWords).OfType<Relation>())
            details.Append($"<code>{H(DescribeWord(run.Fixture.Frame, word, ids))}</code> ");
        details.Append("</div>");
        var pending = E(run.Unresolved).Length + omitted;
        return new(id, name, read, "S03", "axes", "phase-network observation", lines, [],
            pending == 0 && E(steps).Length == 12, 12, E(steps).Length,
            pending, native.Length, native.Count(element => element is Carrier or CarrierLocation),
            false, 0, 0, 0, new(0, 0, 0, 0, 0, true), omitted, run.Fixture.Components.Count, details.ToString());
    }

    private static PacketRow Account(PrimitiveFlowLeaf leaf, NativeIds ids)
    {
        var account = leaf.Account();
        if (account.Left is not Relation { Right: Relation imported } ||
            account.Right is not Relation { Left: Relation active, Right: Relation { Left: Relation retained,
                Right: Relation { Left: Relation released, Right: Relation unresolved } } })
            throw new InvalidOperationException($"Actuator {ids.Of(leaf.Surface)} lost its ownership account.");
        return new(leaf, E(imported).Length, E(active).Length, E(retained).Length, E(released).Length, E(unresolved).Length);
    }

    private static PacketTotals Sum(IEnumerable<PacketRow> rows)
    {
        var values = rows.ToArray(); var imported = values.Sum(row => row.Imported);
        var active = values.Sum(row => row.Active); var retained = values.Sum(row => row.Retained);
        var released = values.Sum(row => row.Released); var unresolved = values.Sum(row => row.Unresolved);
        return new(imported, active, retained, released, unresolved, imported == active + retained + released && unresolved == 0);
    }

    private static string Card(View view) => $"""
        <article class="card" id="{view.Id}">
          <div class="card-head"><span class="id">{view.Id}</span><span class="status {(view.GoalMet && view.Pending == 0 ? "passed" : "open")}">{(view.GoalMet && view.Pending == 0 ? "requested run complete" : "open / pending")}</span></div>
          <h2>{H(view.Name)}</h2>
          {Picture(view)}
          <p class="read">{H(view.Read)}</p>
          <div class="facts"><span><strong>{view.Lines.Count}</strong> strokes</span><span><strong>{view.LayerCount}</strong> reusable components</span><span><strong>{view.Completed}/{view.Requested}</strong> completions</span><span><strong>{view.Pending}</strong> pending</span></div>
          <p class="small">{(view.Packets.Imported == 0 ? "Observed phase events; no packet ownership transfer." : $"Packets: {view.Packets.Imported} imported = {view.Packets.Active} held + {view.Packets.Retained} retained + {view.Packets.Released} released.")} · {view.NativeNumbers} native numeric elements.</p>
          <p class="small">{(!view.ClosureExpected ? "No declared contour-closure view." : $"Closure view: {view.ClosureAccepted} accepted joins · {view.ClosureBlocked} blocked · {view.ClosureUnresolved} unresolved.")} {AtlasLinks(view.Atlas)}</p>
          <details><summary>Inspect recipe layers and causal reads</summary>{view.Details}</details>
        </article>
        """;

    private static string AtlasLinks(string references) => string.Join(" · ", references.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
        .Select(id => $"<a href=\"../skeleton-atlas/{(id.StartsWith('G') ? "goals" : "glossary")}.html#{H(id)}\">Atlas {H(id)}</a>"));

    private static string Picture(View view) => $"<svg class=\"picture\" xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 640 230\" role=\"img\" aria-label=\"{H(view.Name)}: actual accepted strokes\"><rect width=\"640\" height=\"230\" rx=\"8\" fill=\"#f5f2e9\"/><g class=\"equal\">{Paths(view, false, 640, 230)}</g><g class=\"stretch\">{Paths(view, true, 640, 230)}</g></svg>";

    private static string Paths(View view, bool stretch, double width, double height)
    {
        Point Project(double x, double y)
        {
            var point = view.Basis switch
            {
                "triangular" => new Point(x + .5 * y, .8660254037844386 * y),
                "diagonal" => new Point((x - y) * .7071067811865476, (x + y) * .7071067811865476),
                _ => new Point(x, y)
            };
            return stretch ? new(point.X * 1.6, point.Y * .72) : point;
        }
        var all = view.Lines.Concat(view.Navigation).ToArray();
        if (all.Length == 0) return "<text x=\"28\" y=\"115\" fill=\"#7c725f\">No accepted strokes at this boundary.</text>";
        var points = all.SelectMany(line => new[] { Project(line.X1, line.Y1), Project(line.X2, line.Y2) }).ToArray();
        var x0 = points.Min(point => point.X); var x1 = points.Max(point => point.X);
        var y0 = points.Min(point => point.Y); var y1 = points.Max(point => point.Y);
        var scale = Math.Min((width - 72) / Math.Max(1, x1 - x0), (height - 54) / Math.Max(1, y1 - y0));
        var dx = (width - (x1 - x0) * scale) / 2 - x0 * scale;
        var dy = (height - (y1 - y0) * scale) / 2 + y1 * scale;
        var body = new StringBuilder();
        foreach (var line in view.Navigation) Add(line, true);
        foreach (var line in view.Lines) Add(line, false);
        return body.ToString();
        void Add(Line line, bool navigation)
        {
            var from = Project(line.X1, line.Y1); var to = Project(line.X2, line.Y2);
            body.Append($"<path {(navigation ? "class=\"nav-line\" " : "")}d=\"M {F(dx + from.X * scale)} {F(dy - from.Y * scale)} L {F(dx + to.X * scale)} {F(dy - to.Y * scale)}\" fill=\"none\" stroke=\"{line.Colour}\" stroke-width=\"{(navigation ? "1.3" : "3.5")}\" stroke-linecap=\"square\" stroke-linejoin=\"miter\"{(navigation ? " stroke-dasharray=\"4 5\" opacity=\".65\"" : "")}><title>{H(line.Cause)}</title></path>");
        }
    }

    private static string Sheet(IReadOnlyList<View> views)
    {
        var height = 155 + ((views.Count + 1) / 2) * 330;
        var svg = new StringBuilder($"<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"1400\" height=\"{height}\" viewBox=\"0 0 1400 {height}\"><style>text{{font-family:Segoe UI,Arial,sans-serif}}.title{{font-family:Georgia,serif;font-size:36px}}.label{{font-size:20px;font-weight:650}}.note{{font-size:14px;fill:#6d756b}}</style><rect width=\"100%\" height=\"100%\" fill=\"#f3f0e7\"/><text x=\"40\" y=\"57\" class=\"title\">Primitive flows · executable recipe catalogue</text><text x=\"40\" y=\"88\" class=\"note\">Accepted native outputs · independent layer identities · optional display pitches</text>");
        for (var i = 0; i < views.Count; i++)
        {
            var view = views[i]; var x = 30 + (i % 2) * 690; var y = 125 + (i / 2) * 330;
            svg.Append($"<g transform=\"translate({x} {y})\"><rect width=\"650\" height=\"305\" rx=\"8\" fill=\"#fffdf7\"/><text x=\"20\" y=\"32\" class=\"label\">{H(view.Id + " · " + view.Name)}</text><g transform=\"translate(5 42)\">{Paths(view, false, 640, 220)}</g><text x=\"20\" y=\"286\" class=\"note\">{view.Lines.Count} strokes · {view.Completed}/{view.Requested} completions · {view.Pending} pending · {view.NativeNumbers} native numeric elements</text></g>");
        }
        return svg.Append("</svg>").ToString();
    }

    private static string Html(string cards) => """
        <!doctype html><html lang="en"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>Primitive flows · Gojo</title><style>
        :root{--paper:#f3f0e7;--card:#fffdf7;--ink:#203e42;--muted:#657269;--line:#d9dfd1;--rust:#a65e3c}*{box-sizing:border-box}html{scroll-behavior:smooth}body{margin:0;background:var(--paper);color:var(--ink);font:16px/1.6 'Segoe UI',Arial,sans-serif}header,main,footer{max-width:1430px;margin:auto;padding:0 30px}header{padding-top:38px}h1{font:normal clamp(35px,5vw,62px)/1.1 Georgia,serif;letter-spacing:-.025em;margin:12px 0 22px}h2{font:normal 25px/1.2 Georgia,serif;margin:10px 0 18px}h3{font:normal 26px/1.3 Georgia,serif}h4{margin:0 0 4px;font-size:15px}p{margin:0 0 16px}.eyebrow{font-size:12px;letter-spacing:.16em;font-weight:700;text-transform:uppercase;color:var(--muted)}.lede{max-width:980px;font-size:19px;color:#53665f}.toolbar{display:flex;align-items:center;gap:15px;flex-wrap:wrap;padding:15px 0 23px;border-bottom:1px solid var(--line)}a{color:inherit;text-underline-offset:3px}a:hover{color:var(--rust)}button{font:inherit;cursor:pointer;background:var(--card);color:var(--ink);border:1px solid #acbdb1;border-radius:5px;padding:9px 15px}button[aria-pressed=true]{background:#dfe9df}.small{font-size:13px;color:var(--muted)}.compare{display:grid;grid-template-columns:1fr 1fr;gap:30px;padding:27px 0 12px}.compare p{font-size:14px;color:#53665f}.compare strong{color:var(--ink)}.grid{display:grid;grid-template-columns:repeat(2,minmax(0,1fr));gap:25px}.card{min-width:0;background:var(--card);border:1px solid var(--line);border-radius:9px;padding:23px;scroll-margin-top:20px}.card-head{display:flex;justify-content:space-between;align-items:center;gap:10px}.id{font-weight:750;color:var(--rust);font-size:13px;letter-spacing:.08em}.status{font-size:11px;padding:3px 9px;border-radius:20px;background:#e4eee1;color:#3d6540}.status.open{background:#f7e2cb;color:#8d5c22}.picture{display:block;width:100%;height:auto;margin:0 0 18px}.read{min-height:76px;font-size:15px;line-height:1.6}.facts{display:flex;flex-wrap:wrap;gap:8px 16px;border-top:1px solid var(--line);padding:13px 0;font-size:12px;color:var(--muted)}.facts strong{font-size:16px;color:var(--ink)}details{border-top:1px solid var(--line);padding-top:12px;margin-top:17px;font-size:14px}summary{cursor:pointer;font-weight:650}details>p{margin-top:17px}code{font:12px/1.5 Consolas,monospace;color:#745a4b;background:#f0eee7;border-radius:3px;padding:1px 4px}.layer{border-left:2px solid #c9d7c9;padding:10px 0 10px 15px;margin-top:15px;scroll-margin-top:20px}.layer h4 span{font-weight:400;font-size:12px;color:var(--muted)}.layer p{margin:4px 0}.layer ol{padding-left:21px;margin:8px 0}.layer li{margin:7px 0}.word-list{display:flex;flex-wrap:wrap;gap:7px}.stretch{display:none}.stretched .equal{display:none}.stretched .stretch{display:inline}.hide-navigation .nav-line{display:none}.limits{margin:32px 0;padding:25px;background:#e7ebe0;border-radius:8px}.limits p{max-width:1100px;font-size:14px}footer{padding-top:18px;padding-bottom:35px;color:var(--muted);font-size:13px}@media(max-width:850px){.grid,.compare{grid-template-columns:1fr}header,main,footer{padding-left:18px;padding-right:18px}.card{padding:18px}.read{min-height:0}}@media print{.toolbar,details{display:none}.grid{gap:12px}.card{break-inside:avoid;padding:13px}.read{min-height:0}}
        </style></head><body><header><p class=eyebrow>Gojo / pre-numeric execution / primitive library</p><h1>Small structures.<br>Different ways to draw.</h1><p class=lede>Steps, turns, contours and repeated components produce actual drawing histories. The same small component can recur inside a larger one; completion travels back through exposed ports before the next call begins.</p><nav class=toolbar><button id=basis aria-pressed=false>Stretch the display</button><button id=navigation aria-pressed=false>Hide navigation</button><a href="../skeleton-atlas/index.html">Reference atlas</a><a href="gallery.svg">Full contact sheet</a><a href="diagnostics.json">Native diagnostics</a></nav></header><main><div class=compare><p><strong>One family, different mechanisms.</strong> Compare <a href=#P06>P06</a> diagonal turns with <a href=#P17>P17</a> combined drives, then <a href=#P18>P18</a> where the same drives expose separate corners. Compare <a href=#P12>P12</a> and <a href=#P13>P13</a> for matching meanders with different completion boundaries.</p><p><strong>Read the structure at several levels.</strong> Each card opens to its actual reusable components, ordered call occurrences, ports and packet account. Colour distinguishes emitting actuators. Dashed lines are navigation without ink. The atlas links are authored reference targets; these diagrams are accepted executable outputs.</p></div><div class=grid>
        """ + cards + """
        </div><aside class=limits><h3>What these pictures establish</h3><p>Sources contain no numeric carriers or locations. Displacements retain repeated shared-reach terms. The display supplies equal pitches, an oblique triangular basis for sixfold examples, or a diagonal presentation for herringbone; stretching changes only that presentation. A formally returning endpoint remains a fresh site unless an explicit closure declaration is accepted in the separate graph view.</p><p>Packet participation and readiness questions support these discrete call/return experiments. Continuous signed carrier tension, learned recipe discovery, metric angle claims and automatic shape recognition remain separate refinements. A completed run establishes the requested native traversal; it does not certify aesthetic quality or correspondence to every feature of an atlas target.</p></aside></main><footer>Stable conversation references: P01–P19. Definition IDs inside a card are local inspection references, not persisted Gojo identities. Generated from native relations and accepted histories.</footer><script>
        document.getElementById('basis').addEventListener('click',function(){const active=document.body.classList.toggle('stretched');this.setAttribute('aria-pressed',String(active));this.textContent=active?'Use equal display pitches':'Stretch the display';});
        document.getElementById('navigation').addEventListener('click',function(){const active=document.body.classList.toggle('hide-navigation');this.setAttribute('aria-pressed',String(active));this.textContent=active?'Show navigation':'Hide navigation';});
        </script></body></html>
        """;

    private static bool TryWord(QualitativeMotionFrame frame, Relation word, out Point point)
    {
        point = default;
        if (!StructuralConstraint.TryReadScope(word, out var entries, out var seam, out var tail) || !seam.Equals(tail)) return false;
        double x = 0, y = 0;
        foreach (var item in entries)
        {
            if (item is not Relation move || !frame.Direction(move, out var axis, out var forward, out _)) return false;
            if (ReferenceEquals(axis, frame.Horizontal)) x += forward ? 1 : -1; else y += forward ? 1 : -1;
        }
        point = new(x, y); return true;
    }

    private static string DescribeAction(QualitativeDrawingReceiver drawing, IElement action, NativeIds ids) =>
        drawing.TryReadDraw(action, out var word) ? "draw " + DescribeWord(drawing.Frame, word, ids) :
        drawing.TryReadJump(action, out word) ? "navigate " + DescribeWord(drawing.Frame, word, ids) :
        drawing.TryReadDirectionAction(action, out var map) ? $"compose direction correspondence {ids.Of(map)}" :
        drawing.TryReadMirror(action, out map) ? $"compose mirror correspondence {ids.Of(map)}" :
        drawing.IsReset(action) ? "reset to actual anchor site" : drawing.IsAnchor(action) ? "retain current anchor site" : "unresolved action";

    private static string DescribeWord(QualitativeMotionFrame frame, Relation word, NativeIds ids)
    {
        if (!StructuralConstraint.TryReadScope(word, out var entries, out var seam, out var tail)) return "unreadable word";
        var terms = entries.Select(item => item is Relation move && frame.Direction(move, out var axis, out var forward, out var reach)
            ? (ReferenceEquals(axis, frame.Horizontal) ? forward ? "R" : "L" : forward ? "U" : "D") + "[" + ids.Of(reach) + "]" : "?");
        return (entries.Length == 0 ? "empty word" : string.Join(" + ", terms)) + (seam.Equals(tail) ? "" : " + open participation");
    }

    private static string F(double value) => value.ToString("0.###", CultureInfo.InvariantCulture);
    private static string H(string value) => WebUtility.HtmlEncode(value);
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
    private static IEnumerable<IElement> Traverse(IElement root)
    {
        var seen = new HashSet<IElement>(ReferenceEqualityComparer.Instance); var stack = new Stack<IElement>(); stack.Push(root);
        while (stack.TryPop(out var item))
        {
            if (!seen.Add(item)) continue; yield return item;
            if (item is Relation row) { stack.Push(row.Right); stack.Push(row.Left); }
        }
    }
    private sealed class NativeIds
    {
        private readonly Dictionary<IElement, int> ids = new(ReferenceEqualityComparer.Instance);
        public NativeIds(IEnumerable<IElement> elements)
        {
            foreach (var element in elements) if (!ids.ContainsKey(element)) ids.Add(element, ids.Count + 1);
        }
        public string Of(IElement element)
        {
            if (!ids.TryGetValue(element, out var id)) { id = ids.Count + 1; ids.Add(element, id); }
            return "r" + id.ToString(CultureInfo.InvariantCulture);
        }
    }
    private readonly record struct Point(double X, double Y);
    private sealed record Line(double X1, double Y1, double X2, double Y2, string Colour, string Cause);
    private sealed record PacketRow(PrimitiveFlowLeaf Leaf, int Imported, int Active, int Retained, int Released, int Unresolved);
    private sealed record PacketTotals(int Imported, int Active, int Retained, int Released, int Unresolved, bool Accounted);
    private sealed record View(string Id, string Name, string Read, string Atlas, string Basis, string Kind,
        List<Line> Lines, List<Line> Navigation, bool GoalMet, int Requested, int Completed, int Pending,
        int NativeElements, int NativeNumbers, bool ClosureExpected, int ClosureAccepted, int ClosureBlocked, int ClosureUnresolved,
        PacketTotals Packets, int OmittedSegments, int LayerCount, string Details);
}
