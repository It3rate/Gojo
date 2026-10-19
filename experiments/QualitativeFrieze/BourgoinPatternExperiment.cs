using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;
using Gojo.Core;
using Gojo.Experiments.QualitativeExecution;
using N = Gojo.Experiments.QualitativeFrieze.FieldNative;

namespace Gojo.Experiments.QualitativeFrieze;

/// <summary>Source-qualified geometric ornament reconstructions. Only accepted
/// native graph output reaches this renderer. Source scans, display metrics,
/// colours and line weights are interpretations, not generation instructions.</summary>
public static class BourgoinPatternExperiment
{
    private const string SourceRoot = "https://archive.org/details/thoriedelorneme01bourgoog/page/n";
    private static readonly string[] Colours = ["#1b6373", "#a74d36", "#82652c", "#695c89", "#40816b", "#944c77"];
    private sealed record Point(double X, double Y);
    private sealed record Stroke(IElement From, IElement To, Point A, Point B, int Unit);
    private sealed record Census(int Objects, int Relations, int NumericElements);
    private sealed record Facts(string Id, string Name, int Units, int Sites, int Strokes,
        int Components, int CycleRank, int Accepted, int Blocked, int Unresolved, int Frontier,
        int DuplicateEndpointPairs, Census Definition, Census Packet, int ArchiveBytes);
    private sealed record Entry(string Id, string Name, int Leaf, string Reference, string Construction,
        string Fidelity, OrnamentFieldPacket Packet, string Archive, double RX, double RY, double UX, double UY);

    public static int Run(string[] args)
    {
        var output = Path.Combine("experiments", "QualitativeFrieze", "results", "bourgoin-patterns");
        string? input = null; int? columns = null, rows = null; var triangular = false;
        for (var i = 0; i < args.Length; i++)
            if (args[i] == "--output" && i + 1 < args.Length) output = args[++i];
            else if (args[i] == "--input" && i + 1 < args.Length) input = args[++i];
            else if (args[i] == "--columns" && i + 1 < args.Length && int.TryParse(args[++i], out var cc) && cc is > 0 and <= 12) columns = cc;
            else if (args[i] == "--rows" && i + 1 < args.Length && int.TryParse(args[++i], out var rr) && rr is > 0 and <= 12) rows = rr;
            else if (args[i] == "--triangular-display") triangular = true;
            else throw new ArgumentException("Usage: --bourgoin-patterns [--input PACKET.gojo.json] [--columns 1..12] [--rows 1..12] [--triangular-display] [--output DIRECTORY]");
        Directory.CreateDirectory(output);
        var entries = Entries(input, triangular).ToArray();
        var defaultSources = Path.Combine("experiments", "QualitativeFrieze", "results", "bourgoin-patterns");
        foreach (var entry in entries.Where(entry => entry.Leaf != 0))
        {
            var source = Path.Combine(defaultSources, "source-" + entry.Id + ".jpg");
            var target = Path.Combine(output, "source-" + entry.Id + ".jpg");
            if (File.Exists(source) && !string.Equals(Path.GetFullPath(source), Path.GetFullPath(target), StringComparison.OrdinalIgnoreCase)) File.Copy(source, target, true);
        }
        File.WriteAllText(Path.Combine(output, "catalog.json"), JsonSerializer.Serialize(new
        {
            Schema = "gojo-bourgoin-source-catalog-v1",
            Entries = entries.Select(entry => new { entry.Id, entry.Name, entry.Leaf, entry.Reference,
                entry.Construction, entry.Fidelity, Source = entry.Leaf == 0 ? null : SourceRoot + entry.Leaf + "/mode/1up",
                DisplayBasis = new { entry.RX, entry.RY, entry.UX, entry.UY } })
        }, new JsonSerializerOptions { WriteIndented = true }));
        var cards = new StringBuilder(); var diagnostics = new List<Facts>(); var failed = false;
        foreach (var entry in entries)
        {
            var packet = entry.Packet;
            var window = columns.HasValue || rows.HasValue ? Resize(packet.Window, columns, rows) : packet.Window;
            var run = packet.Program.Generate(packet.Bindings, window);
            var field = new OrnamentProgramField(run, packet.SeamContract);
            var saved = OrnamentFieldPacket.Save(packet.Program, packet.Bindings, window, packet.SeamContract);
            var facts = Inspect(entry, run, field, window, saved);
            diagnostics.Add(facts);
            failed |= !run.IsComplete || facts.Blocked != 0 || facts.Unresolved != 0 || facts.Definition.NumericElements != 0 || facts.Packet.NumericElements != 0;
            var svg = Svg(entry, run, field);
            File.WriteAllText(Path.Combine(output, entry.Id + ".svg"), svg);
            File.WriteAllText(Path.Combine(output, entry.Id + ".gojo.json"), saved);
            var inspection = OrnamentProgramExperiment.Describe(new(entry.Id, entry.Name, entry.Construction,
                packet.Program, packet.Bindings, window, run, true));
            File.WriteAllText(Path.Combine(output, entry.Id + "-encoding.txt"), inspection +
                $"\n\nEXPLICIT FIELD READ\nAccepted seams: {facts.Accepted}; blocked: {facts.Blocked}; unresolved: {facts.Unresolved}; declared finite frontier: {facts.Frontier}.\n" +
                "Seam declarations select actual motif/transform identities and public roles. Finite offsets walk the sampled occurrence order. Equal display positions do not propose new joins. Parallel stroke participations are retained.\n");
            cards.Append(Card(entry, svg, facts, output));
            Console.WriteLine($"{entry.Id}: {facts.Units} units, {facts.Strokes} strokes, {facts.Accepted} accepted seams; {facts.Blocked} blocked, {facts.Unresolved} unresolved; {facts.Components} ink components.");
        }
        var report = new
        {
            Schema = "gojo-bourgoin-pattern-study-v1", Source = "Jules Bourgoin, Theorie de l'ornement, 1883 scan",
            Input = input is null ? "authored declarations, archived then imported before execution" : "native field packet only",
            AllRunsComplete = !failed, Cases = diagnostics,
            Limits = new[] { "Source-derived structural adaptations, not pixel matching or a learned generator.",
                "Every saved native packet contains only relations/placeholders; screen metrics and source metadata are separate.",
                "Declared seams join sites; they do not fold duplicate coincident edges.",
                "A graph cycle is not a certified filled face. Crossings do not become junctions automatically.",
                "Triangular display is an optional metric realization, not a native assertion of sixty-degree angles." }
        };
        File.WriteAllText(Path.Combine(output, "diagnostics.json"), JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }));
        File.WriteAllText(Path.Combine(output, "gallery.html"), Html(cards.ToString(), diagnostics, input is not null));
        Console.WriteLine($"Wrote {Path.GetFullPath(output)}");
        return failed ? 1 : 0;
    }

    private static IEnumerable<Entry> Entries(string? input, bool triangular)
    {
        if (input is not null)
        {
            var archive = File.ReadAllText(input);
            if (!OrnamentFieldPacket.TryLoad(archive, out var packet, out _)) throw new InvalidDataException("This file is not a complete native ornament field packet.");
            yield return new("REPLAY", "Replay an imported field", 0, "Native packet",
                "The source motif programs, finite phase cycles, bindings and seam rules come entirely from the imported graph. A new window may sample the same definition.",
                "No authoring library invoked. Display basis is an explicit renderer choice.", packet, archive, 1, 0, triangular ? .5 : 0, triangular ? Math.Sqrt(3) / 2 : 1);
            yield break;
        }
        foreach (var recipe in new BourgoinPatternLibrary().Recipes)
        {
            var archive = OrnamentFieldPacket.Save(recipe.Program, recipe.Bindings, recipe.Window, recipe.SeamContract);
            if (!OrnamentFieldPacket.TryLoad(archive, out var packet, out _)) throw new InvalidDataException("A source declaration failed to round-trip: " + recipe.Id);
            var basis = recipe.DisplayBasis;
            yield return new(recipe.Id, recipe.Name, recipe.Leaf, recipe.Reference, recipe.Construction,
                recipe.Fidelity, packet, archive, basis.RX, basis.RY, basis.UX, basis.UY);
        }
    }

    private static Relation Resize(Relation window, int? columns, int? rows)
    {
        var initial = (Relation)window.Left; var scopes = (Relation)window.Right;
        return OrnamentProgram.Window(initial.Left, initial.Right,
            columns ?? N.E((Relation)scopes.Left).Length, rows ?? N.E((Relation)scopes.Right).Length);
    }

    private static Facts Inspect(Entry entry, OrnamentProgramRun run, OrnamentProgramField field, Relation window, string saved)
    {
        var sites = N.E(field.Graph.Sites); var edges = N.E(field.Graph.Strokes).Cast<Relation>().ToArray();
        var indices = sites.Select((site, index) => (site, index)).ToDictionary(pair => pair.site, pair => pair.index, N.Participants.Instance);
        var parents = Enumerable.Range(0, sites.Length).ToArray();
        int Root(int i) { while (parents[i] != i) { parents[i] = parents[parents[i]]; i = parents[i]; } return i; }
        var pairs = new HashSet<(int, int)>(); var duplicatePairs = 0;
        foreach (var edge in edges)
        {
            if (!QualitativeStrokeGraph.TryReadStroke(edge, out _, out var from, out var to, out _) ||
                !indices.TryGetValue(from, out var a) || !indices.TryGetValue(to, out var b)) throw new InvalidOperationException("A field stroke lost its native endpoint identity.");
            if (!pairs.Add(a < b ? (a, b) : (b, a))) duplicatePairs++;
            parents[Root(a)] = Root(b);
        }
        var components = Enumerable.Range(0, sites.Length).Select(Root).Distinct().Count();
        return new(entry.Id, entry.Name, run.Units.Count, sites.Length, edges.Length,
            components, edges.Length - sites.Length + components, N.E(field.Accepted).Length,
            N.E(run.Blocked).Length + N.E(field.Blocked).Length,
            N.E(run.Unresolved).Length + N.E(field.Unresolved).Length, N.E(field.Frontier).Length, duplicatePairs,
            Count(entry.Packet.Program.Surface), Count(new Relation(new Relation(entry.Packet.Program.Surface,
                new Relation(entry.Packet.Bindings, window)), entry.Packet.SeamContract)), System.Text.Encoding.UTF8.GetByteCount(saved));
    }

    private static Census Count(IElement root)
    {
        var seen = new HashSet<IElement>(N.Participants.Instance); var pending = new Stack<IElement>(); pending.Push(root);
        var relations = 0; var numeric = 0;
        while (pending.TryPop(out var element))
        {
            if (!seen.Add(element)) continue;
            if (element is Relation relation) { relations++; pending.Push(relation.Left); pending.Push(relation.Right); }
            if (element is Carrier or CarrierLocation) numeric++;
        }
        return new(seen.Count, relations, numeric);
    }

    private static string Svg(Entry entry, OrnamentProgramRun run, OrnamentProgramField field)
    {
        const double width = 1000, height = 600, margin = 28;
        var graph = field.Graph;
        var positions = N.E(field.Bindings).Cast<Relation>().ToDictionary(row => row.Left,
            row => Word(graph.Frame, (Relation)row.Right, entry), N.Participants.Instance);
        var owners = new Dictionary<IElement, int>(N.Participants.Instance);
        for (var i = 0; i < run.Units.Count; i++)
            foreach (var edge in N.E(run.Units[i].Current.Join.Graph.Strokes)) owners.TryAdd(edge, i);
        var strokes = new List<Stroke>();
        foreach (var edge in N.E(graph.Strokes).Cast<Relation>())
        {
            if (!QualitativeStrokeGraph.TryReadStroke(edge, out var cause, out var from, out var to, out _) ||
                !positions.TryGetValue(from, out var a) || !positions.TryGetValue(to, out var b)) throw new InvalidOperationException("The renderer requires actual accepted endpoint bindings.");
            strokes.Add(new(from, to, a, b, owners.GetValueOrDefault(cause)));
        }
        var points = strokes.SelectMany(stroke => new[] { stroke.A, stroke.B }).ToArray();
        var minX = points.Length == 0 ? 0 : points.Min(point => point.X); var maxX = points.Length == 0 ? 1 : points.Max(point => point.X);
        var minY = points.Length == 0 ? 0 : points.Min(point => point.Y); var maxY = points.Length == 0 ? 1 : points.Max(point => point.Y);
        var scale = Math.Min((width - 2 * margin) / Math.Max(1, maxX - minX), (height - 2 * margin) / Math.Max(1, maxY - minY));
        Point Display(Point p) => new(width / 2 + (p.X - (minX + maxX) / 2) * scale, height / 2 - (p.Y - (minY + maxY) / 2) * scale);
        var svg = new StringBuilder($"<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 {width} {height}' role='img' aria-label='{H(entry.Name)}'><title>{H(entry.Name)}</title><style>.ink{{stroke:#263b3a;stroke-width:var(--weight,2.8);stroke-linecap:round;stroke-linejoin:round;fill:none}}.ports{{display:none}}.guides{{display:none}}[data-view=units] .ink{{stroke:var(--unit)}}[data-view=ports] .ports{{display:block}}[data-view=ports] .ink{{stroke:#698685;opacity:.7}}[data-view=construction] .guides{{display:block}}[data-view=construction] .ink{{stroke:#395b5b}}</style><rect width='100%' height='100%' fill='#faf7ee'/>");
        svg.Append("<g class='guides' stroke='#c6a970' stroke-width='1' stroke-dasharray='4 5' fill='none'>");
        foreach (var unit in run.Units)
        {
            var at = Display(Word(graph.Frame, unit.PlacementWord, entry));
            svg.Append($"<path d='M {F(at.X - 5)} {F(at.Y)} h 10 M {F(at.X)} {F(at.Y - 5)} v 10'/>");
        }
        svg.Append("</g><g>");
        foreach (var stroke in strokes)
        {
            var a = Display(stroke.A); var b = Display(stroke.B);
            svg.Append($"<path class='ink' style='--unit:{Colours[stroke.Unit % Colours.Length]}' d='M {F(a.X)} {F(a.Y)} L {F(b.X)} {F(b.Y)}'><title>Accepted stroke; occurrence {stroke.Unit + 1}</title></path>");
        }
        svg.Append("</g>");
        // A projected interior crossing is only a display crossing. An actual
        // junction requires split edges and an admitted shared-site declaration.
        for (var i = 0; i < strokes.Count; i++) for (var j = i + 1; j < strokes.Count; j++)
        {
            var a = strokes[i]; var b = strokes[j];
            if (N.Same(a.From, b.From) || N.Same(a.From, b.To) || N.Same(a.To, b.From) || N.Same(a.To, b.To) || !Crossing(a, b, out var p)) continue;
            var at = Display(p); var start = Display(b.A); var finish = Display(b.B);
            var dx = finish.X - start.X; var dy = finish.Y - start.Y; var length = Math.Sqrt(dx * dx + dy * dy);
            dx = dx / length * 4; dy = dy / length * 4;
            svg.Append($"<g><title>Independent crossing; no shared native site</title><circle cx='{F(at.X)}' cy='{F(at.Y)}' r='3.5' fill='#faf7ee'/><path class='ink' style='--unit:{Colours[b.Unit % Colours.Length]}' d='M {F(at.X - dx)} {F(at.Y - dy)} L {F(at.X + dx)} {F(at.Y + dy)}'/></g>");
        }
        svg.Append("<g class='ports'>");
        foreach (var group in strokes.SelectMany(stroke => new[] { stroke.From, stroke.To }).GroupBy(site => site, N.Participants.Instance))
        {
            var at = Display(positions[group.Key]); var degree = group.Count();
            svg.Append($"<circle cx='{F(at.X)}' cy='{F(at.Y)}' r='{(degree > 2 ? "3.4" : "2.1")}' fill='{(degree > 2 ? "#b54b38" : "#faf7ee")}' stroke='#386368' stroke-width='1'><title>Actual site with {degree} stroke participations</title></circle>");
        }
        return svg.Append("</g></svg>").ToString();
    }

    private static Point Word(QualitativeMotionFrame frame, Relation word, Entry entry)
    {
        if (!N.Read(word, out var moves)) throw new InvalidOperationException("An open position cannot be displayed as known.");
        double x = 0, y = 0;
        foreach (var term in moves)
        {
            if (term is not Relation move || !frame.Direction(move, out var axis, out var forward, out _)) throw new InvalidOperationException("A display move must belong to the actual native frame.");
            var sign = forward ? 1 : -1;
            x += sign * (ReferenceEquals(axis, frame.Horizontal) ? entry.RX : entry.UX);
            y += sign * (ReferenceEquals(axis, frame.Horizontal) ? entry.RY : entry.UY);
        }
        return new(x, y);
    }

    private static bool Crossing(Stroke a, Stroke b, out Point point)
    {
        point = null!; var ux = a.B.X - a.A.X; var uy = a.B.Y - a.A.Y; var vx = b.B.X - b.A.X; var vy = b.B.Y - b.A.Y;
        var det = ux * vy - uy * vx; if (Math.Abs(det) < 1e-8) return false;
        var dx = b.A.X - a.A.X; var dy = b.A.Y - a.A.Y; var t = (dx * vy - dy * vx) / det; var u = (dx * uy - dy * ux) / det;
        if (t <= 1e-6 || t >= 1 - 1e-6 || u <= 1e-6 || u >= 1 - 1e-6) return false;
        point = new(a.A.X + t * ux, a.A.Y + t * uy); return true;
    }

    private static string Card(Entry entry, string svg, Facts facts, string output)
    {
        var sourceImage = "source-" + entry.Id + ".jpg";
        var source = entry.Leaf == 0 ? "<p class='note'>Standalone replay. Source attribution and display style live outside the executable packet.</p>" :
            $"<a href='{SourceRoot}{entry.Leaf}/mode/1up' target='_blank' rel='noreferrer'><img loading='lazy' src='{sourceImage}' alt='Source excerpt: {H(entry.Reference)}'/></a><p>{H(entry.Reference)} · scan leaf {entry.Leaf}. <a href='{SourceRoot}{entry.Leaf}/mode/1up'>Open the source page</a>.</p>";
        return $"<article id='{entry.Id}'><header><span class='eyebrow'>{H(entry.Id)} · imported native field</span><h2>{H(entry.Name)}</h2><p>{H(entry.Construction)}</p></header><div class='comparison'><figure class='result'>{svg}<figcaption>Accepted native output · one optional screen realization</figcaption></figure><figure class='source'>{source}</figure></div><p class='fidelity'>{H(entry.Fidelity)}</p><div class='facts'><span><b>{facts.Units}</b> occurrences</span><span><b>{facts.Strokes}</b> strokes</span><span><b>{facts.Accepted}</b> admitted seams</span><span><b>{facts.Blocked} / {facts.Unresolved}</b> blocked / open</span></div><details><summary>Construction, identities and limits</summary><p>{facts.Definition.Objects:N0} distinct native objects in the reusable program; {facts.Packet.Objects:N0} including sample and seam law. {facts.ArchiveBytes:N0} JSON bytes. All are relation/placeholder objects; there are {facts.Packet.NumericElements} numeric participants.</p><p>The joined ink graph has {facts.Components} connected components and cycle rank {facts.CycleRank}. These are graph diagnostics, not face counts or an aesthetic score. The finite patch has {facts.Frontier} out-of-window seam obligations. {facts.DuplicateEndpointPairs} repeated endpoint-pair edge participations remain explicit.</p><p>Source fidelity is an authored judgement. No image recognition or pixel comparison chose this recipe. The selected display basis is R=({F(entry.RX)},{F(entry.RY)}), U=({F(entry.UX)},{F(entry.UY)}); these coordinates are absent from the executable graph.</p><p><a href='{entry.Id}.gojo.json'>Native packet</a> · <a href='{entry.Id}-encoding.txt'>Graph-derived program inspection</a> · <a href='{entry.Id}.svg'>SVG</a></p></details></article>";
    }

    private static string Html(string cards, IReadOnlyList<Facts> facts, bool replay) => """
<!doctype html><html lang="en"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>Bourgoin · native ornament constructions</title><style>
*{box-sizing:border-box}body{margin:0;background:#eeece2;color:#263b3a;font:16px/1.6 system-ui,sans-serif}main{max-width:1380px;margin:auto;padding:36px 26px 80px}h1,h2{font-family:Georgia,serif;font-weight:500;line-height:1.12}h1{font-size:clamp(36px,5vw,64px);max-width:1050px;margin:12px 0 22px}h2{font-size:31px;margin:10px 0}p{max-width:1050px}a{color:#276d78;text-underline-offset:3px}.eyebrow{font-size:12px;letter-spacing:.14em;text-transform:uppercase;color:#7c6440}.intro{font-size:19px}.controls{position:sticky;top:0;z-index:2;background:#faf7eef5;backdrop-filter:blur(8px);border:1px solid #d6d3c5;padding:12px 16px;display:flex;align-items:center;gap:24px;flex-wrap:wrap;margin:28px 0}.controls label{display:flex;align-items:center;gap:10px}select{font:inherit;padding:5px;border:1px solid #afb7aa;background:#fffdf7;color:inherit}input{accent-color:#36737a}nav{display:flex;gap:10px;flex-wrap:wrap;margin:20px 0 32px}nav a{padding:4px 9px;background:#e4e2d5;text-decoration:none}article{margin:30px 0 46px;padding:25px;background:#fffdf7;border:1px solid #d5d4c6;scroll-margin-top:95px}header p{margin:14px 0 18px}.comparison{display:grid;grid-template-columns:minmax(0,1fr) 240px;gap:24px;align-items:center}.comparison figure{margin:0;min-width:0}.result svg{display:block;width:100%;height:auto;max-height:650px;background:#faf7ee}.source img{display:block;width:100%;height:auto;max-height:350px;object-fit:contain;filter:sepia(.1)}figcaption,.source p,.note{font-size:13px;color:#72766a}.source p{margin:8px 0}.fidelity{border-left:3px solid #c1a371;padding-left:14px;color:#675b46;font-size:14px}.facts{display:flex;gap:16px 30px;flex-wrap:wrap;margin:20px 0;font-size:14px}.facts b{display:block;font-size:20px;color:#2b666e}details{border-top:1px solid #e3e0d5;padding-top:14px;font-size:14px}summary{cursor:pointer;color:#2f6972}footer{border-top:1px solid #c7c8ba;padding-top:24px}.note strong{color:#4a665f}@media(max-width:750px){main{padding:24px 14px}article{padding:18px 12px}.comparison{grid-template-columns:1fr}.source{display:flex;align-items:center;gap:14px}.source>a{width:42%;flex-shrink:0}.source img{max-height:180px}.controls{gap:12px}h2{font-size:27px}.facts{gap:12px 22px}}
</style></head><body data-view="ink"><main><span class="eyebrow">Source study · Jules Bourgoin · 1883 scan</span><h1>One construction, a coherent field</h1><p class="intro">Geometric ornament recipes built from shared unmeasured reaches, ordered path words, finite phase cycles and explicit landmark seams. Every preview executes an imported native packet. The renderer supplies only a screen interpretation.</p><p>These are structural reconstructions and adaptations of selected diagrams in <a href="https://archive.org/details/thoriedelorneme01bourgoog">Théorie de l’ornement</a>. Compare the source excerpt beside each field. Faithfulness, simplifications and open metric assumptions are stated individually.</p><p class="note"><strong>A common plan is useful; one connected ink component is optional.</strong> Bourgoin distinguishes detached, contiguous, crossed and articulated motifs. Guides and source descriptions often explain the composition better than the final outline alone.</p><div class="controls"><label>Read <select id="view"><option value="ink">Unified ornament</option><option value="units">Individual occurrences</option><option value="ports">Actual sites and joins</option><option value="construction">Placement landmarks</option></select></label><label>Line weight <input id="weight" type="range" min="1" max="7" step=".2" value="2.8"></label></div>
""" + "<nav>" + string.Join("", facts.Select(item => $"<a href='#{item.Id}'>{item.Id}</a>")) + "</nav>" + cards + """
<footer><p>These are authored structural programmes, not learned imitations. Source citations and crops are reference material. A display crossing does not create a junction; a finite boundary is not an interior failure; shared ink edges are not silently deduplicated.</p><p><a href="../../../../Gojo/docs/GojoBourgoinEncodingStudy.md">Findings and reproduction commands</a> · <a href="diagnostics.json">Diagnostics</a> · <a href="../ornament-program/gallery.html">Earlier ornament program study</a></p></footer></main><script>document.querySelector('#view').addEventListener('change',e=>document.body.dataset.view=e.target.value);document.querySelector('#weight').addEventListener('input',e=>document.documentElement.style.setProperty('--weight',e.target.value));</script></body></html>
""";

    private static string H(string value) => WebUtility.HtmlEncode(value);
    private static string F(double value) => value.ToString("0.###", CultureInfo.InvariantCulture);
}
