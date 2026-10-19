using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;
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
    private static readonly HashSet<string> ContactCorrections = ["B24", "B26", "B29", "B31", "B32", "B33"];
    private sealed record Point(double X, double Y);
    private sealed record Stroke(IElement From, IElement To, Point A, Point B, int Unit);
    private sealed record Census(int Objects, int Relations, int NumericElements);
    private sealed record ConstructionFacts(int Motifs, int Paths, int Chords, int Ports,
        int ClosedPaths, int Prototypes, int Images, int Refinements, int CornerMaps,
        int OtherEvidence, int BlockedReads, int OpenReads);
    // A manually observed engraving registration, outside the executable
    // packet. R/U are image pixels per displayed unmeasured reach.
    private sealed record SourceRegistration(double Width, double Height, double X, double Y,
        double RX, double RY, double UX, double UY)
    {
        public SourceRegistration(double width, double height, double x, double y, double r, double u)
            : this(width, height, x, y, r, 0, 0, -u) { }
    }
    private static readonly IReadOnlyDictionary<string, SourceRegistration> SourceRegistrations =
        new Dictionary<string, SourceRegistration>
        {
            ["B02"] = new(233, 283, 32, 183, 14.333333, 14),
            ["B06.B"] = new(659, 416, 112, 326, 12.166667, 11.5),
            ["B08.B"] = new(733, 471, 165, 288, 7.625, 8.667),
            ["B09"] = new(172, 71, 17, 48, 7, 7),
            ["B10"] = new(233, 321, 33.666667, 305.333347, 6.416667, 6.416667),
            ["B11"] = new(233, 321, 35, 307.333347, 6.416667, 6.416667),
            ["B12"] = new(245, 293, 91.2, 60.8, 4.284, 4.284),
            ["B13"] = new(354, 321, 71, 273.42, 0, -10.38, -10.38, 0),
            ["B14"] = new(354, 293, 17, 32, 8.7, 8.7),
            ["B24"] = new(233, 264, 7.08, 179.58, 14.58, 0, .08, -13.95),
            ["B26"] = new(336, 302, 91, 85, 5.06625, 2.925, 2.925, -5.06625),
            ["B29"] = new(244, 290, 78.984, 220.187, 4.46251, -6.58737, -6.57653, -4.21778),
            ["B31"] = new(354, 287, 278.5, 234.5, -2.916667, 0, 0, -2.916667),
            ["B32"] = new(367, 321, 56.81, 79.69, 5.44, 5.44),
            ["B33"] = new(367, 303, 62.125, 52.125, 6.09375, 6.09375)
        };
    private sealed record Facts(string Id, string Name, int Units, int Sites, int Strokes,
        int Components, int CycleRank, int Accepted, int Blocked, int Unresolved, int Frontier,
        int DuplicateEndpointPairs, Census Definition, Census Packet, int ArchiveBytes,
        ConstructionFacts Structure);
    private sealed record Entry(string Id, string Name, int Leaf, string Reference, string Construction,
        string Fidelity, OrnamentFieldPacket Packet, string Archive, double RX, double RY, double UX, double UY);

    public static int Run(string[] args)
    {
        var output = Path.Combine("experiments", "QualitativeFrieze", "results", "bourgoin-patterns");
        string? input = null, selectedCases = null; int? columns = null, rows = null; var triangular = false;
        for (var i = 0; i < args.Length; i++)
            if (args[i] == "--output" && i + 1 < args.Length) output = args[++i];
            else if (args[i] == "--input" && i + 1 < args.Length) input = args[++i];
            else if (args[i] == "--cases" && i + 1 < args.Length) selectedCases = args[++i];
            else if (args[i] == "--columns" && i + 1 < args.Length && int.TryParse(args[++i], out var cc) && cc is > 0 and <= 12) columns = cc;
            else if (args[i] == "--rows" && i + 1 < args.Length && int.TryParse(args[++i], out var rr) && rr is > 0 and <= 12) rows = rr;
            else if (args[i] == "--triangular-display") triangular = true;
            else throw new ArgumentException("Usage: --bourgoin-patterns [--input PACKET.gojo.json] [--cases B02,B06.B,...] [--columns 1..12] [--rows 1..12] [--triangular-display] [--output DIRECTORY]");
        Directory.CreateDirectory(output);
        var requested = selectedCases?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var entries = Entries(input, triangular, requested).Where(entry => requested is null || requested.Contains(entry.Id, StringComparer.OrdinalIgnoreCase)).ToArray();
        if (entries.Length == 0 || requested is not null && requested.Any(id => !entries.Any(entry => string.Equals(entry.Id, id, StringComparison.OrdinalIgnoreCase))))
            throw new ArgumentException("Every selected case must name an available native source recipe.", nameof(args));
        var defaultSources = Path.Combine("experiments", "QualitativeFrieze", "results", "bourgoin-patterns");
        foreach (var entry in entries.Where(entry => entry.Leaf != 0))
        {
            var source = Path.Combine(defaultSources, "source-" + entry.Id + ".jpg");
            var target = Path.Combine(output, "source-" + entry.Id + ".jpg");
            if (File.Exists(source) && !string.Equals(Path.GetFullPath(source), Path.GetFullPath(target), StringComparison.OrdinalIgnoreCase)) File.Copy(source, target, true);
            var prior = Previous(entry.Id);
            var before = Path.Combine(defaultSources, prior.Folder, prior.Id + ".svg");
            var after = Path.Combine(output, prior.Folder, prior.Id + ".svg");
            if (File.Exists(before) && !string.Equals(Path.GetFullPath(before), Path.GetFullPath(after), StringComparison.OrdinalIgnoreCase))
            { Directory.CreateDirectory(Path.GetDirectoryName(after)!); File.Copy(before, after, true); }
        }
        File.WriteAllText(Path.Combine(output, "catalog.json"), JsonSerializer.Serialize(new
        {
            Schema = "gojo-bourgoin-source-catalog-v1",
            Entries = entries.Select(entry => new { entry.Id, entry.Name, entry.Leaf, entry.Reference,
                entry.Construction, entry.Fidelity, Source = entry.Leaf == 0 ? null : SourceRoot + entry.Leaf + "/mode/1up",
                SourceRegistration = SourceRegistrations.GetValueOrDefault(entry.Id),
                DisplayBasis = new { entry.RX, entry.RY, entry.UX, entry.UY } })
        }, new JsonSerializerOptions { WriteIndented = true }));
        var cards = new StringBuilder(); var diagnostics = new List<Facts>(); var failed = false;
        var overview = new List<(string Id, string Name, string Svg)>();
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
            overview.Add((entry.Id, entry.Name, svg));
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
        var html = Html(cards.ToString(), diagnostics, input is not null)
            .Replace("../../../../Gojo/docs/GojoBourgoinRefinementStudy.md", H(Path.GetRelativePath(output, "Gojo/docs/GojoBourgoinContactAndCourseStudy.md").Replace('\\', '/')))
            .Replace("../ornament-program/gallery.html", H(Path.GetRelativePath(output, "experiments/QualitativeFrieze/results/ornament-program/gallery.html").Replace('\\', '/')));
        html = html.Replace("<nav>", "<p><a href='overview.html'>All constructions on one page</a> · <a href='overview.svg'>Standalone labelled SVG</a></p><nav>");
        File.WriteAllText(Path.Combine(output, "gallery.html"), html);
        File.WriteAllText(Path.Combine(output, "overview.svg"), Overview(overview));
        File.WriteAllText(Path.Combine(output, "overview.html"), "<!doctype html><html lang='en'><meta charset='utf-8'><meta name='viewport' content='width=device-width,initial-scale=1'><title>Bourgoin · all constructions</title><style>body{margin:0;background:#eeece2;color:#263b3a;font:16px system-ui}nav{padding:18px 24px}a{color:#276d78;margin-right:20px}img{display:block;width:100%;height:auto}</style><nav><a href='gallery.html'>Detailed comparisons</a><a href='overview.svg'>Standalone SVG · zoom or save</a></nav><img src='overview.svg' alt='Labelled overview of all native Bourgoin constructions'></html>");
        Console.WriteLine($"Wrote {Path.GetFullPath(output)}");
        return failed ? 1 : 0;
    }

    private static IEnumerable<Entry> Entries(string? input, bool triangular, string[]? requested)
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
        foreach (var recipe in new BourgoinPatternLibrary().Recipes.Where(recipe =>
                     requested is null || requested.Contains(recipe.Id, StringComparer.OrdinalIgnoreCase)))
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
                new Relation(entry.Packet.Bindings, window)), entry.Packet.SeamContract)), System.Text.Encoding.UTF8.GetByteCount(saved),
            Construction(entry.Packet.Program));
    }

    private static ConstructionFacts Construction(OrnamentProgram program)
    {
        var reads = N.E(program.Motifs).Cast<Relation>().Select(motif => new BourgoinConstructionRead(program.Frame, motif)).ToArray();
        var prototypes = reads.SelectMany(read => N.E(read.Prototypes)).Distinct(N.Participants.Instance).Count();
        return new(reads.Length, reads.Sum(read => read.PathCount), reads.Sum(read => read.ChordCount),
            reads.Sum(read => read.PortCount), reads.Sum(read => read.ClosedPathCount), prototypes,
            reads.Sum(read => read.Images.Count), reads.Sum(read => read.Refinements.Count),
            reads.Sum(read => read.Corners.Count), reads.Sum(read => N.E(read.Unrecognized).Length),
            reads.Sum(read => N.E(read.Blocked).Length), reads.Sum(read => N.E(read.Unresolved).Length));
    }

    private static string Overview(IReadOnlyList<(string Id, string Name, string Svg)> items)
    {
        const int columns = 6, tileWidth = 410, tileHeight = 302, inset = 18, heading = 112;
        var rows = (items.Count + columns - 1) / columns;
        var svg = new StringBuilder($"<svg xmlns='http://www.w3.org/2000/svg' xmlns:xlink='http://www.w3.org/1999/xlink' width='{columns * tileWidth + inset * 2}' height='{rows * tileHeight + heading + inset}' viewBox='0 0 {columns * tileWidth + inset * 2} {rows * tileHeight + heading + inset}' role='img' aria-label='All Bourgoin native constructions'><style>text{{fill:#263b3a;font-family:Georgia,serif}}.ink{{--weight:4}}</style><rect width='100%' height='100%' fill='#eeece2'/><text x='{inset}' y='44' font-size='32'>Bourgoin · native construction atlas</text><text x='{inset}' y='78' font-size='18'>{items.Count} executable skeletons · shared motifs, explicit transforms and landmark seams · selected screen proportions</text>");
        for (var i = 0; i < items.Count; i++)
        {
            var item = items[i]; var x = inset + i % columns * tileWidth; var y = heading + i / columns * tileHeight;
            var root = XDocument.Parse(item.Svg).Root!;
            root.SetAttributeValue("x", x + 8); root.SetAttributeValue("y", y + 53);
            root.SetAttributeValue("width", tileWidth - 24); root.SetAttributeValue("height", tileHeight - 69);
            var words = item.Name.Split(' '); var lines = new List<string>(); var line = item.Id + " ·";
            foreach (var word in words)
            {
                if (line.Length + word.Length > 39) { lines.Add(line); line = word; }
                else line += " " + word;
            }
            lines.Add(line);
            svg.Append($"<a xlink:href='gallery.html#{H(item.Id)}'><rect x='{x}' y='{y}' width='{tileWidth - 10}' height='{tileHeight - 10}' fill='#fffdf7' stroke='#d5d4c6'/>");
            for (var j = 0; j < lines.Count; j++) svg.Append($"<text x='{x + 12}' y='{y + 22 + j * 19}' font-size='16'>{H(lines[j])}</text>");
            svg.Append(root.ToString(SaveOptions.DisableFormatting)).Append("</a>");
        }
        return svg.Append("</svg>").ToString();
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
        var svg = new StringBuilder($"<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 {width} {height}' role='img' aria-label='{H(entry.Name)}' data-world-x='{F((minX + maxX) / 2)}' data-world-y='{F((minY + maxY) / 2)}' data-screen-scale='{scale.ToString("R", CultureInfo.InvariantCulture)}'><title>{H(entry.Name)}</title><style>.ink{{stroke:#263b3a;stroke-width:var(--weight,2.8);stroke-linecap:round;stroke-linejoin:round;fill:none}}.ports{{display:none}}.guides{{display:none}}[data-view=units] .ink{{stroke:var(--unit)}}[data-view=ports] .ports{{display:block}}[data-view=ports] .ink{{stroke:#698685;opacity:.7}}[data-view=construction] .guides{{display:block}}[data-view=construction] .ink{{stroke:#395b5b}}</style><rect width='100%' height='100%' fill='#faf7ee'/>");
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
        return $"<article id='{entry.Id}'><header><span class='eyebrow'>{H(entry.Id)} · imported native field</span><h2>{H(entry.Name)}</h2><p class='note'>Authored recipe · description of the generating construction</p><p>{H(entry.Construction)}</p></header><div class='comparison'><figure class='result'>{svg}<figcaption>Accepted native output · one optional screen realization</figcaption></figure><figure class='source'>{source}</figure></div><p class='fidelity'><strong>Authored source assessment.</strong> {H(entry.Fidelity)}</p><div class='facts'><span><b>{facts.Units}</b> occurrences</span><span><b>{facts.Strokes}</b> strokes</span><span><b>{facts.Accepted}</b> admitted seams</span><span><b>{facts.Blocked} / {facts.Unresolved}</b> blocked / open</span></div>" + Comparison(entry, svg, output) + ConstructionPanel(facts) + $"<p class='note'>The selected display basis is R=({F(entry.RX)},{F(entry.RY)}), U=({F(entry.UX)},{F(entry.UY)}); screen coordinates are absent from the executable graph. <a href='{entry.Id}.gojo.json'>Native packet</a> · <a href='{entry.Id}-encoding.txt'>Graph-derived program inspection</a> · <a href='{entry.Id}.svg'>SVG</a></p></article>";
    }

    private static string ConstructionPanel(Facts facts)
    {
        var c = facts.Structure;
        return $"<details class='structure-read'><summary>Computed structure, identities and limits</summary><h3>Read from the imported native graph</h3><p>The reusable program declares {c.Motifs} motif definition(s), containing {c.Paths} executable paths, {c.Chords} chords, {c.Ports} landmark ports and {c.ClosedPaths} closed-path claims. The sampled field executes {facts.Units} occurrences; occurrences share the selected definition while retaining their own cursors, sites and applications.</p><p>The exact construction reader admits {c.Images} seed-image correspondences from {c.Prototypes} distinct retained contour prototypes, {c.Refinements} coarse/fine path refinements and {c.CornerMaps} ordered corner correspondences. It leaves {c.OtherEvidence} other evidence entries available without classifying them; {c.BlockedReads} rejected construction reads and {c.OpenReads} open evidence scopes remain visible. This read may admit no prototypes because evidence is absent, unfamiliar, rejected or open; that is not a verdict about the path.</p><p>{facts.Definition.Objects:N0} distinct native objects belong to the reusable program; {facts.Packet.Objects:N0} include the sample and seam law. The packet has {facts.ArchiveBytes:N0} JSON bytes and {facts.Packet.NumericElements} numeric participants. Object and byte counts measure this inspectable representation, not a minimum encoding.</p><h3>Read from execution</h3><p>The joined ink graph has {facts.Components} connected components and cycle rank {facts.CycleRank}. These are graph diagnostics, not face counts or an aesthetic score. The finite patch has {facts.Frontier} out-of-window seam obligations. {facts.DuplicateEndpointPairs} repeated endpoint-pair edge participations remain explicit.</p><h3>Scope of this construction</h3><p>Placement supplies the motif, transform and period words. Local path flow publishes landmark sites; the seam layer checks those exposed ports and admits or blocks shared-site relations. These source recipes do not currently renegotiate their motif in response to neighbouring tension. Reusable seed history and active feedback are different claims. Source fidelity and interpretation remain authored judgements.</p></details>";
    }

    private static (string Folder, string Id, string Label) Previous(string id) => id == "B32"
        ? ("contact-pass", id, "Before the diagonal half-drop")
        : ContactCorrections.Contains(id)
            ? ("vocabulary-pass", id, "Previous vocabulary pass")
            : ("first-pass", id is "B06.B" or "B08.B" ? id[..3] : id, "First pass");

    private static string Comparison(Entry entry, string svg, string output)
    {
        if (entry.Leaf == 0) return "";
        var prior = Previous(entry.Id);
        if (!File.Exists(Path.Combine(output, prior.Folder, prior.Id + ".svg"))) return "";
        var revised = ContactCorrections.Contains(entry.Id) || new[] { "B02", "B06.B", "B08", "B08.B", "B09", "B10", "B11", "B12", "B13", "B14" }.Contains(entry.Id);
        if (!revised) return "";
        XNamespace ns = "http://www.w3.org/2000/svg";
        var root = XDocument.Parse(svg).Root!;
        double Read(string name) => double.Parse((string)root.Attribute(name)!, CultureInfo.InvariantCulture);
        var registration = SourceRegistrations.GetValueOrDefault(entry.Id);
        var registrationMatrix = "matrix(1 0 0 1 0 0)";
        if (registration is not null)
        {
            var imageScale = Math.Min(1000 / registration.Width, 600 / registration.Height);
            var a = imageScale * registration.RX / Read("data-screen-scale");
            var b = imageScale * registration.RY / Read("data-screen-scale");
            var c = -imageScale * registration.UX / Read("data-screen-scale");
            var d = -imageScale * registration.UY / Read("data-screen-scale");
            var x = (1000 - registration.Width * imageScale) / 2 + imageScale * (registration.X + registration.RX * Read("data-world-x") + registration.UX * Read("data-world-y")) - a * 500 - c * 300;
            var y = (600 - registration.Height * imageScale) / 2 + imageScale * (registration.Y + registration.RY * Read("data-world-x") + registration.UY * Read("data-world-y")) - b * 500 - d * 300;
            registrationMatrix = $"matrix({F(a)} {F(b)} {F(c)} {F(d)} {F(x)} {F(y)})";
        }
        var ink = root.Descendants(ns + "path")
            .Where(path => (string?)path.Attribute("class") == "ink")
            .Select(path => new XElement(ns + "path", new XAttribute("d", (string)path.Attribute("d")!)));
        var paths = $"<g transform='{registrationMatrix}'>" + string.Concat(ink.Select(path => path.ToString(SaveOptions.DisableFormatting))) + "</g>";
        var controls = string.Join("", new[] { ("x", "Across", "-1000", "1000", "0", "1"),
            ("y", "Down", "-1000", "1000", "0", "1"), ("sx", "Width", ".05", "4", "1", ".001"),
            ("sy", "Height", ".05", "4", "1", ".001"), ("alpha", "Ink", "0", "1", ".65", ".01") }
            .Select(item => $"<label>{item.Item2}<input data-register='{item.Item1}' type='range' min='{item.Item3}' max='{item.Item4}' value='{item.Item5}' step='{item.Item6}'></label>"));
        return $"<details class='comparison-workbench'><summary>Previous construction and source overlay</summary><div class='before-after'><figure><img loading='lazy' src='{prior.Folder}/{prior.Id}.svg' alt='Previous reconstruction'/><figcaption>{prior.Label}</figcaption></figure><figure><img loading='lazy' src='{entry.Id}.svg' alt='Revised construction'/><figcaption>Revised native output</figcaption></figure></div><p class='note'>The red overlay is the accepted output above. Adjust its screen registration against the scan to inspect repeated edges and compartments. This is a visual comparison; registration does not change native geometry or certify a match.</p><div class='registration-controls'>{controls}<button type='button' class='reset-registration'>Reset</button></div><svg class='registration' xmlns='http://www.w3.org/2000/svg' viewBox='0 0 1000 600' role='img' aria-label='Manually registered source comparison'><rect width='1000' height='600' fill='#fff'/><image href='source-{entry.Id}.jpg' width='1000' height='600' preserveAspectRatio='xMidYMid meet'/><g class='registered-ink' fill='none' stroke='#c33724' stroke-width='2.1' stroke-linecap='round' stroke-linejoin='round' opacity='.65'>{paths}</g></svg></details>";
    }

    private static string Html(string cards, IReadOnlyList<Facts> facts, bool replay) => """
<!doctype html><html lang="en"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>Bourgoin · native ornament constructions</title><style>
*{box-sizing:border-box}body{margin:0;background:#eeece2;color:#263b3a;font:16px/1.6 system-ui,sans-serif}main{max-width:1380px;margin:auto;padding:36px 26px 80px}h1,h2{font-family:Georgia,serif;font-weight:500;line-height:1.12}h1{font-size:clamp(36px,5vw,64px);max-width:1050px;margin:12px 0 22px}h2{font-size:31px;margin:10px 0}p{max-width:1050px}a{color:#276d78;text-underline-offset:3px}.eyebrow{font-size:12px;letter-spacing:.14em;text-transform:uppercase;color:#7c6440}.intro{font-size:19px}.controls{position:sticky;top:0;z-index:2;background:#faf7eef5;backdrop-filter:blur(8px);border:1px solid #d6d3c5;padding:12px 16px;display:flex;align-items:center;gap:24px;flex-wrap:wrap;margin:28px 0}.controls label{display:flex;align-items:center;gap:10px}select{font:inherit;padding:5px;border:1px solid #afb7aa;background:#fffdf7;color:inherit}input{accent-color:#36737a}nav{display:flex;gap:10px;flex-wrap:wrap;margin:20px 0 32px}nav a{padding:4px 9px;background:#e4e2d5;text-decoration:none}article{margin:30px 0 46px;padding:25px;background:#fffdf7;border:1px solid #d5d4c6;scroll-margin-top:95px}header p{margin:14px 0 18px}.comparison{display:grid;grid-template-columns:minmax(0,1fr) 240px;gap:24px;align-items:center}.comparison figure{margin:0;min-width:0}.result svg{display:block;width:100%;height:auto;max-height:650px;background:#faf7ee}.source img{display:block;width:100%;height:auto;max-height:350px;object-fit:contain;filter:sepia(.1)}figcaption,.source p,.note{font-size:13px;color:#72766a}.source p{margin:8px 0}.fidelity{border-left:3px solid #c1a371;padding-left:14px;color:#675b46;font-size:14px}.facts{display:flex;gap:16px 30px;flex-wrap:wrap;margin:20px 0;font-size:14px}.facts b{display:block;font-size:20px;color:#2b666e}details{border-top:1px solid #e3e0d5;padding-top:14px;font-size:14px}summary{cursor:pointer;color:#2f6972}footer{border-top:1px solid #c7c8ba;padding-top:24px}.note strong{color:#4a665f}@media(max-width:750px){main{padding:24px 14px}article{padding:18px 12px}.comparison{grid-template-columns:1fr}.source{display:flex;align-items:center;gap:14px}.source>a{width:42%;flex-shrink:0}.source img{max-height:180px}.controls{gap:12px}h2{font-size:27px}.facts{gap:12px 22px}}
</style></head><body data-view="ink"><main><span class="eyebrow">Source study · Jules Bourgoin · 1883 scan</span><h1>One construction, a coherent field</h1><p class="intro">Geometric ornament recipes built from shared unmeasured reaches, ordered path words, finite phase cycles and explicit landmark seams. Every preview executes an imported native packet. The renderer supplies only a screen interpretation.</p><p>These are structural reconstructions and adaptations of selected diagrams in <a href="https://archive.org/details/thoriedelorneme01bourgoog">Théorie de l’ornement</a>. Compare the source excerpt beside each field. Faithfulness, simplifications and open metric assumptions are stated individually.</p><p class="note"><strong>A common plan is useful; one connected ink component is optional.</strong> Bourgoin distinguishes detached, contiguous, crossed and articulated motifs. Guides and source descriptions often explain the composition better than the final outline alone.</p><div class="controls"><label>Read <select id="view"><option value="ink">Unified ornament</option><option value="units">Individual occurrences</option><option value="ports">Actual sites and joins</option><option value="construction">Placement landmarks</option></select></label><label>Line weight <input id="weight" type="range" min="1" max="7" step=".2" value="2.8"></label></div>
""" + "<nav>" + string.Join("", facts.Select(item => $"<a href='#{item.Id}'>{item.Id}</a>")) + "</nav>" + cards + """
<footer><p>These are authored structural programmes, not learned imitations. Source citations and crops are reference material. A display crossing does not create a junction; a finite boundary is not an interior failure; shared ink edges are not silently deduplicated.</p><p><a href="../../../../Gojo/docs/GojoBourgoinRefinementStudy.md">Findings and reproduction commands</a> · <a href="diagnostics.json">Diagnostics</a> · <a href="../ornament-program/gallery.html">Earlier ornament program study</a></p></footer></main><script>document.querySelector('#view').addEventListener('change',e=>document.body.dataset.view=e.target.value);document.querySelector('#weight').addEventListener('input',e=>document.documentElement.style.setProperty('--weight',e.target.value));for(const panel of document.querySelectorAll('.comparison-workbench')){const sliders=[...panel.querySelectorAll('[data-register]')],ink=panel.querySelector('.registered-ink');function update(){const v=Object.fromEntries(sliders.map(s=>[s.dataset.register,Number(s.value)]));ink.setAttribute('transform',`translate(${v.x} ${v.y}) translate(500 300) scale(${v.sx} ${v.sy}) translate(-500 -300)`);ink.setAttribute('opacity',v.alpha);}for(const s of sliders)s.addEventListener('input',update);panel.querySelector('.reset-registration').addEventListener('click',()=>{for(const s of sliders)s.value=s.defaultValue;update();});update();}</script><style>.before-after{display:grid;grid-template-columns:1fr 1fr;gap:16px;margin:20px 0}.before-after figure{margin:0;min-width:0}.before-after img{width:100%;height:auto;display:block}.registration{width:100%;height:auto;display:block;border:1px solid #ddd6c6;margin-bottom:20px}.registration-controls{display:flex;flex-wrap:wrap;gap:12px;margin:16px 0}.registration-controls label{display:flex;gap:8px;align-items:center;font-size:13px}.registration-controls input{max-width:120px}.comparison-workbench{margin:16px 0}.comparison-workbench button{background:#f5efdf;border:1px solid #cec5b1;color:#315d61;padding:5px 12px;cursor:pointer}@media(max-width:600px){.before-after{grid-template-columns:1fr}}</style></body></html>
""";

    private static string H(string value) => WebUtility.HtmlEncode(value);
    private static string F(double value) => value.ToString("0.###", CultureInfo.InvariantCulture);
}
