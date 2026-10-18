using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;
using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;
using N = Gojo.Experiments.QualitativeFrieze.FieldNative;

namespace Gojo.Experiments.QualitativeFrieze;

/// <summary>Gallery and standalone native-archive replay. Screen coordinates,
/// line thickness and colours are a separate interpretation of accepted native
/// strokes. They never choose rules, parameters, phases or shared sites.</summary>
public static class OrnamentProgramExperiment
{
    private static readonly string[] Colours = ["#205f69", "#ae6545", "#675380"];
    private sealed record Point(double X, double Y);
    private sealed record Stroke(IElement From, IElement To, Point A, Point B, string Colour);
    private sealed record Counts(int Nodes, int Relations, int Participants, int NumericElements);

    public static int Run(string[] args)
    {
        var output = Path.Combine("experiments", "QualitativeFrieze", "results", "ornament-program");
        string? input = null;
        for (var i = 0; i < args.Length; i++)
            if (args[i] == "--output" && i + 1 < args.Length) output = args[++i];
            else if (args[i] == "--input" && i + 1 < args.Length) input = args[++i];
            else throw new ArgumentException("Usage: --ornament-program [--input NATIVE-SAMPLE.json] [--output DIRECTORY]");
        Directory.CreateDirectory(output);

        string archive;
        IReadOnlyList<OrnamentProgramStudy.Variant> variants;
        Relation correspondence;
        if (input is null)
        {
            var study = new OrnamentProgramStudy();
            archive = study.Archive; variants = study.Variants; correspondence = study.LoadedSample.Correspondence;
        }
        else
        {
            archive = File.ReadAllText(input);
            if (!OrnamentProgram.TryLoad(archive, out var sample, out var unresolved))
                throw new InvalidDataException($"The supplied native sample cannot be read ({N.E(unresolved).Length} unresolved obligations).");
            var run = sample.Program.Generate(sample.Bindings, sample.Window);
            variants = [new("P01", "Replay a saved native sample", "This process loaded the program, parameter domains, selected bindings and phase/window roles from the archive. No source motif factory was invoked.", sample.Program, sample.Bindings, sample.Window, run, true)];
            correspondence = sample.Correspondence;
        }

        var cards = new StringBuilder();
        foreach (var variant in variants)
        {
            var svg = Svg(variant);
            File.WriteAllText(Path.Combine(output, variant.Id + ".svg"), svg);
            var declaration = Count(variant.Program.Surface);
            var sampled = Count(new Relation(variant.Program.Surface, new Relation(variant.Bindings, variant.Window)));
            var expanded = Count(variant.Run.Surface);
            var instructions = variant.Run.Units.Sum(unit => N.E(unit.Current.Graph.Strokes).Length);
            var phase = (Relation)variant.Window.Left;
            var sampleScope = (Relation)variant.Window.Right;
            var columns = N.E((Relation)sampleScope.Left).Length;
            var rows = N.E((Relation)sampleScope.Right).Length;
            var label = variant.LoadedFromArchive ? "LOADED NATIVE DEFINITION" : "ORIGINAL NATIVE DEFINITION";
            cards.Append($"<article id='{variant.Id}'><header><p class='eyebrow'>{variant.Id} · {label}</p><h2>{H(variant.Name)}</h2><p>{H(variant.Description)}</p></header><figure>{svg}</figure>");
            cards.Append($"<div class='facts'><span><b>{columns} × {rows}</b> sampling slots</span><span><b>{variant.Run.Units.Count}</b> accepted units</span><span><b>{instructions}</b> native strokes</span><span><b>{N.E(variant.Run.Blocked).Length} / {N.E(variant.Run.Unresolved).Length}</b> blocked / unresolved</span></div>");
            cards.Append($"<details><summary>Inspect this sample and its representation</summary><p>{H(Choices(variant))}</p><p>Native phase origins: column {PhaseName(variant.Program, phase.Left, true)}, row {PhaseName(variant.Program, phase.Right, false)}. These select actual cycle roles; a renderer coordinate is not used as a phase.</p><table><thead><tr><th>Reachable relation-graph object count</th><th>Distinct objects</th></tr></thead><tbody><tr><td>Reusable program definition</td><td>{declaration.Nodes:N0}</td></tr><tr><td>Definition + selected bindings + sampled window</td><td>{sampled.Nodes:N0}</td></tr><tr><td>Expanded execution and retained audit</td><td>{expanded.Nodes:N0}</td></tr></tbody></table><p class='note'>Counts include shared objects once through relation links. They are diagnostic graph sizes, not minimum description lengths, byte packing claims or a matched-work computation benchmark. The private execution audit intentionally retains receipts, phases and correspondence.</p><p><a href='{variant.Id}.svg'>Open this SVG</a></p></details></article>");
        }

        File.WriteAllText(Path.Combine(output, "program.gojo.json"), archive);
        File.WriteAllText(Path.Combine(output, "encoding.txt"), Explanation() + "\n\n" + Describe(variants[0]));
        var diagnostics = new
        {
            Schema = "gojo-ornament-program-study-v1", Input = input is null ? "authored-native-sample" : "native-archive-only",
            ArchiveBytes = System.Text.Encoding.UTF8.GetByteCount(archive), ArchiveCorrespondenceNodes = Count(correspondence).Nodes,
            AllRunsComplete = variants.All(variant => variant.Run.IsComplete),
            Cases = variants.Select(variant => new
            {
                variant.Id, variant.Name, variant.LoadedFromArchive, Units = variant.Run.Units.Count,
                Strokes = variant.Run.Units.Sum(unit => N.E(unit.Current.Graph.Strokes).Length),
                Applications = N.E(variant.Run.Applications).Length, Blocked = N.E(variant.Run.Blocked).Length,
                Unresolved = N.E(variant.Run.Unresolved).Length, Program = Count(variant.Program.Surface),
                Sample = Count(new Relation(variant.Program.Surface, new Relation(variant.Bindings, variant.Window))),
                Expanded = Count(variant.Run.Surface)
            }).ToArray(),
            Limits = new[]
            {
                "Finite authored motif/transform domains and layout rules; not learned construction synthesis.",
                "A relative phase origin and a finite sampling window, not a calibrated clock or measured spatial frequency.",
                "No new seam negotiation, endpoint fusion, rooted descriptor bins or metric regularity inference.",
                "Optional screen interpretation assigns every symbolic move reach size one; archive execution does not require that interpretation."
            }
        };
        var options = new JsonSerializerOptions { WriteIndented = true };
        File.WriteAllText(Path.Combine(output, "diagnostics.json"), JsonSerializer.Serialize(diagnostics, options));
        File.WriteAllText(Path.Combine(output, "gallery.html"), Html(cards.ToString(), variants, System.Text.Encoding.UTF8.GetByteCount(archive), input is not null));
        Console.WriteLine(JsonSerializer.Serialize(diagnostics, options));
        Console.WriteLine($"Wrote {Path.GetFullPath(output)}");
        return variants.Any(variant => !variant.Run.IsComplete || Count(variant.Program.Surface).NumericElements != 0) ? 1 : 0;
    }

    private static string Choices(OrnamentProgramStudy.Variant variant)
    {
        var names = new[] { "motif A", "motif B", "transform A", "transform B", "column step", "row step" };
        var motifs = N.E(variant.Program.Motifs);
        var transforms = N.E(variant.Program.Transforms);
        var parameters = N.E(variant.Program.Parameters).Cast<Relation>().ToArray();
        return string.Join(" · ", parameters.Select((parameter, index) =>
        {
            var binding = N.E(variant.Bindings).Cast<Relation>().Single(row => N.Same(row.Left, parameter.Left));
            var choice = Array.FindIndex(N.E((Relation)parameter.Right), actual => N.Same(actual, binding.Right)) + 1;
            var domain = motifs.Any(actual => N.Same(actual, binding.Right)) ? "motif " :
                transforms.Any(actual => N.Same(actual, binding.Right)) ? "transform " : "word ";
            return $"{(index < names.Length ? names[index] : "parameter " + (index + 1))} = {domain}{choice}";
        }));
    }

    private static string PhaseName(OrnamentProgram program, IElement phase, bool horizontal)
    {
        var cycles = (Relation)program.Layout.Left;
        var cycle = (Relation)(horizontal ? cycles.Left : cycles.Right);
        return (Array.FindIndex(N.E((Relation)cycle.Left), actual => N.Same(actual, phase)) + 1).ToString(CultureInfo.InvariantCulture);
    }

    private static string Svg(OrnamentProgramStudy.Variant variant)
    {
        const double width = 960, height = 460, padding = 28;
        var strokes = new List<Stroke>();
        foreach (var unit in variant.Run.Units)
        {
            var application = N.E(variant.Run.Applications).Cast<Relation>().Single(row => row.Right is
                Relation { Right: Relation placement } && ReferenceEquals(placement.Right, unit.Current.Surface));
            var choices = (Relation)((Relation)application.Right).Left;
            var motifIndex = Array.FindIndex(N.E(variant.Program.Motifs), motif => N.Same(motif, choices.Left));
            if (motifIndex < 0) throw new InvalidOperationException("A rendered occurrence needs its actual selected motif application.");
            var graph = unit.Current.Join.Graph;
            var positions = N.E(unit.Current.Join.Bindings).Cast<Relation>().ToDictionary(binding => binding.Left,
                binding => Word(graph.Frame, (Relation)binding.Right), N.Participants.Instance);
            foreach (var row in N.E(graph.Strokes).Cast<Relation>())
            {
                if (!QualitativeStrokeGraph.TryReadStroke(row, out _, out var from, out var to, out _) ||
                    !positions.TryGetValue(from, out var a) || !positions.TryGetValue(to, out var b))
                    throw new InvalidOperationException("Every rendered stroke needs its actual accepted endpoint bindings.");
                strokes.Add(new(from, to, a, b, Colours[motifIndex % Colours.Length]));
            }
        }
        var points = strokes.SelectMany(stroke => new[] { stroke.A, stroke.B }).ToArray();
        var minX = points.Length == 0 ? 0 : points.Min(point => point.X);
        var maxX = points.Length == 0 ? 1 : points.Max(point => point.X);
        var minY = points.Length == 0 ? 0 : points.Min(point => point.Y);
        var maxY = points.Length == 0 ? 1 : points.Max(point => point.Y);
        var scale = Math.Min((width - 2 * padding) / Math.Max(1, maxX - minX), (height - 2 * padding) / Math.Max(1, maxY - minY));
        Point Display(Point point) => new(width / 2 + (point.X - (minX + maxX) / 2) * scale,
            height / 2 - (point.Y - (minY + maxY) / 2) * scale);
        string Line(Point a, Point b, string colour, double thickness)
        {
            var first = Display(a); var last = Display(b);
            return $"<line x1='{F(first.X)}' y1='{F(first.Y)}' x2='{F(last.X)}' y2='{F(last.Y)}' stroke='{colour}' stroke-width='{F(thickness)}' stroke-linecap='square'/>";
        }
        var svg = new StringBuilder($"<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 {width} {height}' role='img' aria-label='{H(variant.Name)}'><title>{H(variant.Name)}</title><rect width='100%' height='100%' fill='#f9f6ed'/>");
        foreach (var stroke in strokes) svg.Append(Line(stroke.A, stroke.B, stroke.Colour, 4.5));
        // Projected crossings are displayed as overpasses. This visual test
        // neither introduces a shared site nor alters native execution.
        for (var i = 0; i < strokes.Count; i++)
            for (var j = i + 1; j < strokes.Count; j++)
            {
                var lower = strokes[i]; var upper = strokes[j];
                if (N.Same(lower.From, upper.From) || N.Same(lower.From, upper.To) || N.Same(lower.To, upper.From) || N.Same(lower.To, upper.To) ||
                    !Crossing(lower, upper, out var point)) continue;
                var at = Display(point); var a = Display(upper.A); var b = Display(upper.B);
                var length = Math.Sqrt((b.X - a.X) * (b.X - a.X) + (b.Y - a.Y) * (b.Y - a.Y));
                if (length == 0) continue;
                var dx = (b.X - a.X) / length * 6; var dy = (b.Y - a.Y) / length * 6;
                svg.Append($"<g><title>Projected crossing; independent native sites</title><circle cx='{F(at.X)}' cy='{F(at.Y)}' r='5.8' fill='#f9f6ed'/><line x1='{F(at.X - dx)}' y1='{F(at.Y - dy)}' x2='{F(at.X + dx)}' y2='{F(at.Y + dy)}' stroke='{upper.Colour}' stroke-width='4.5'/></g>");
            }
        var endpoints = strokes.SelectMany(stroke => new[] { (Site: stroke.From, Point: stroke.A), (Site: stroke.To, Point: stroke.B) });
        foreach (var group in endpoints.GroupBy(endpoint => endpoint.Point).Where(group =>
            group.Select(endpoint => endpoint.Site).Distinct(N.Participants.Instance).Count() > 1))
        {
            var at = Display(group.Key);
            svg.Append($"<circle cx='{F(at.X)}' cy='{F(at.Y)}' r='3.1' fill='#f9f6ed' stroke='#8c887b' stroke-width='1'><title>Equal screen position; distinct native sites, without a seam join</title></circle>");
        }
        if (strokes.Count == 0) svg.Append("<text x='480' y='230' text-anchor='middle' fill='#82504a'>No accepted unit strokes; inspect blocked and unresolved evidence.</text>");
        return svg.Append("</svg>").ToString();
    }

    private static Point Word(QualitativeMotionFrame frame, Relation word)
    {
        if (!N.Read(word, out var entries)) throw new InvalidOperationException("The renderer cannot guess an open position word.");
        double x = 0, y = 0;
        foreach (var entry in entries)
        {
            if (entry is not Relation move || !frame.Direction(move, out var axis, out var forward, out _))
                throw new InvalidOperationException("A rendered move must belong to its declared frame.");
            if (ReferenceEquals(axis, frame.Horizontal)) x += forward ? 1 : -1;
            else y += forward ? 1 : -1;
        }
        return new(x, y);
    }

    private static bool Crossing(Stroke a, Stroke b, out Point point)
    {
        point = null!;
        var ux = a.B.X - a.A.X; var uy = a.B.Y - a.A.Y;
        var vx = b.B.X - b.A.X; var vy = b.B.Y - b.A.Y;
        var denominator = ux * vy - uy * vx;
        if (Math.Abs(denominator) < 1e-8) return false;
        var dx = b.A.X - a.A.X; var dy = b.A.Y - a.A.Y;
        var t = (dx * vy - dy * vx) / denominator; var u = (dx * uy - dy * ux) / denominator;
        if (t <= 1e-6 || t >= 1 - 1e-6 || u <= 1e-6 || u >= 1 - 1e-6) return false;
        point = new(a.A.X + t * ux, a.A.Y + t * uy); return true;
    }

    private static Counts Count(IElement root)
    {
        var pending = new Stack<IElement>(); var visited = new HashSet<IElement>(ReferenceEqualityComparer.Instance);
        pending.Push(root); var relations = 0; var numeric = 0;
        while (pending.TryPop(out var element))
        {
            if (!visited.Add(element)) continue;
            if (element is Relation relation) { relations++; pending.Push(relation.Left); pending.Push(relation.Right); }
            if (element is Carrier or CarrierLocation) numeric++;
        }
        return new(visited.Count, relations, visited.Count - relations, numeric);
    }

    /// <summary>Presentation aliases are derived from actual native scopes.
    /// This report is neither an executable language nor an alternate parser.</summary>
    private static string Describe(OrnamentProgramStudy.Variant sample)
    {
        var program = sample.Program;
        var text = new StringBuilder("ACTUAL STORED SAMPLE — GRAPH-DERIVED INSPECTION\n\n");
        text.AppendLine("Aliases below identify actual native participants in this sample. They are local presentation names, not executable tags, a canonical encoding, or a parsable replacement for program.gojo.json.");
        text.AppendLine("R/L and U/D name the two declared ordered axes and their reverse directions. [ ... ] is an ordered closed scope; Open[ ... ] retains an unknown tail. Every move occurrence is shown separately. Each chord word is one drawing action; its internal move terms do not introduce intermediate line corners.");
        var aliases = new Dictionary<IElement, string>(N.Participants.Instance);
        aliases[program.Frame.Origin] = "O";
        text.AppendLine("O is the declared native frame-origin marker. Start and placement words use that reference; [] means empty displacement and does not by itself identify two physical sites.");
        var reaches = new List<IElement>(); var unknown = new List<IElement>();
        var motifs = N.E(program.Motifs); var transforms = N.E(program.Transforms);
        var parameters = N.E(program.Parameters).Cast<Relation>().ToArray();
        for (var i = 0; i < motifs.Length; i++) aliases[motifs[i]] = "M" + (i + 1);
        for (var i = 0; i < transforms.Length; i++) aliases[transforms[i]] = "T" + (i + 1);
        for (var i = 0; i < parameters.Length; i++) aliases[parameters[i].Left] = "P" + (i + 1);
        var cycles = (Relation)program.Layout.Left;
        foreach (var (cycle, prefix) in new[] { ((Relation)cycles.Left, "U"), ((Relation)cycles.Right, "V") })
            foreach (var (phase, index) in N.E((Relation)cycle.Left).Select((phase, index) => (phase, index))) aliases[phase] = prefix + index;

        text.AppendLine("\nMOTIF DEFINITIONS");
        foreach (var motif in motifs.Cast<Relation>())
        {
            var definition = (Relation)motif.Right; var pathAndPorts = (Relation)definition.Left;
            var paths = N.E((Relation)pathAndPorts.Left).Cast<Relation>().ToArray();
            var closedPaths = (Relation)((Relation)definition.Right).Left;
            text.AppendLine(A(motif) + ":");
            for (var i = 0; i < paths.Length; i++)
            {
                var path = paths[i]; var name = A(motif) + ".path" + (i + 1);
                var chords = N.E((Relation)path.Right);
                text.AppendLine("  " + name + " starts at " + Word((Relation)path.Left));
                for (var j = 0; j < chords.Length; j++)
                {
                    var chord = name + ".chord" + (j + 1);
                    text.AppendLine("    " + chord + " = " + Word((Relation)chords[j]));
                }
                text.AppendLine("    closure = " + (N.E(closedPaths).Any(actual => N.Same(actual, path))
                    ? "explicitly declared for this actual path" : N.Read(closedPaths, out _) ? "not declared" : "unresolved open inventory"));
            }
            foreach (var (port, index) in N.E((Relation)pathAndPorts.Right).Cast<Relation>().Select((port, index) => (port, index)))
            {
                var selection = (Relation)port.Right;
                var pathIndex = Array.FindIndex(paths, actual => N.Same(actual, selection.Left));
                var selector = A(motif) + ".path" + (pathIndex + 1) + (N.Same(selection.Left, selection.Right)
                    ? ".end" : ".chord" + (Array.FindIndex(N.E((Relation)paths[pathIndex].Right), actual => N.Same(actual, selection.Right)) + 1));
                text.AppendLine("  " + A(motif) + ".port" + (index + 1) + " selects " + selector +
                    (N.Same(selection.Left, selection.Right) ? " (terminal cursor)" : " (cursor before this chord)"));
            }
        }

        text.AppendLine("\nTRANSFORM SUBSTITUTIONS");
        foreach (var transform in transforms.Cast<Relation>())
        {
            text.AppendLine(A(transform) + " (declared frame -> same declared frame):");
            var rows = (Relation)((Relation)transform.Right).Left;
            foreach (var row in N.E(rows).Cast<Relation>()) text.AppendLine("  " + Move((Relation)row.Left) + " -> " + Word((Relation)row.Right));
            if (!N.Read(rows, out _)) text.AppendLine("  ... unresolved substitution tail");
        }

        text.AppendLine("\nPARAMETER DOMAINS AND SELECTED BINDINGS");
        foreach (var parameter in parameters)
        {
            var domain = (Relation)parameter.Right;
            var bindings = N.E(sample.Bindings).Cast<Relation>().Where(row => N.Same(row.Left, parameter.Left)).ToArray();
            text.AppendLine(A(parameter.Left) + " allows " + Scope(domain, Choice) + "; selected " +
                (bindings.Length == 1 ? Choice(bindings[0].Right) : bindings.Length == 0 ? "unresolved" : "conflicting multiple bindings"));
        }
        text.AppendLine("\nLAYOUT PHASES AND RULES");
        foreach (var (cycle, label) in new[] { ((Relation)cycles.Left, "Column"), ((Relation)cycles.Right, "Row") })
        {
            text.AppendLine(label + " cycle roles = " + Scope((Relation)cycle.Left, A));
            foreach (var row in N.E((Relation)cycle.Right).Cast<Relation>()) text.AppendLine("  " + A(row.Left) + " -> " + A(row.Right));
        }
        var layout = (Relation)program.Layout.Right; var steps = (Relation)layout.Left;
        text.AppendLine("Column displacement reads " + A(steps.Left) + "; row displacement reads " + A(steps.Right) + ".");
        foreach (var rule in N.E((Relation)layout.Right).Cast<Relation>())
        {
            var phase = (Relation)rule.Left; var slots = (Relation)rule.Right;
            text.AppendLine("  (" + A(phase.Left) + ", " + A(phase.Right) + ") -> (motif " + A(slots.Left) + ", transform " + A(slots.Right) + ")");
        }
        var start = (Relation)sample.Window.Left; var window = (Relation)sample.Window.Right;
        text.AppendLine("\nSAMPLED WINDOW");
        text.AppendLine("Start roles = (" + A(start.Left) + ", " + A(start.Right) + ").");
        text.AppendLine("Column occurrence slots = " + Scope((Relation)window.Left, item => "c" + Array.FindIndex(N.E((Relation)window.Left), actual => N.Same(actual, item))));
        text.AppendLine("Row occurrence slots = " + Scope((Relation)window.Right, item => "r" + Array.FindIndex(N.E((Relation)window.Right), actual => N.Same(actual, item))));
        text.AppendLine("These finite scopes sample " + sample.Run.Units.Count + " accepted unit occurrences. Slot counts and aliases are diagnostic reads, not calibrated lengths or clocks.");
        text.AppendLine("\nSHARED REACHES");
        for (var i = 0; i < reaches.Count; i++) text.AppendLine(ReachName(i) + " = one actual shared, unmeasured reach identity. Reuse means shared participation; no numerical size is supplied.");
        return text.ToString();

        string A(IElement item)
        {
            if (aliases.TryGetValue(item, out var name)) return name;
            var index = unknown.FindIndex(actual => N.Same(actual, item));
            if (index < 0) { index = unknown.Count; unknown.Add(item); }
            return "native" + (index + 1);
        }
        string ReachName(int index) => index == 0 ? "u" : "u" + (index + 1);
        string Move(Relation move)
        {
            if (!program.Frame.Direction(move, out var axis, out var forward, out var reach)) return A(move);
            var index = reaches.FindIndex(actual => N.Same(actual, reach));
            if (index < 0) { index = reaches.Count; reaches.Add(reach); }
            return (ReferenceEquals(axis, program.Frame.Horizontal) ? forward ? "R" : "L" : forward ? "U" : "D") + "(" + ReachName(index) + ")";
        }
        string Word(Relation word) => Scope(word, item => item is Relation move ? Move(move) : A(item));
        string Choice(IElement choice) => aliases.ContainsKey(choice) ? A(choice) : choice is Relation word ? Word(word) : A(choice);
        string Scope(Relation scope, Func<IElement, string> show) => (N.Read(scope, out _) ? "[" : "Open[") + string.Join(" ", N.E(scope).Select(show)) + "]";
    }

    private static string Explanation() => """
GOJO ORNAMENT PROGRAM — FINITE NATIVE FORMAT, VERSION 0

C(...) means an ordered closed native scope; -> means an ordered relation.
Names below describe connected slots. Names are not executable tags.

PROGRAM = frame -> (C(motif surfaces) -> (C(transform surfaces) ->
          (C(parameter-slot -> C(actual allowed choices)) -> layout)))

MOTIF = frame -> ((C(start-word -> C(chord words)) -> C(public ports)) ->
        (C(explicit closed paths) -> evidence))
MOVE = actual ordered direction -> shared symbolic reach
WORD = C(actual move occurrences)
TRANSFORM = (source frame -> target frame) ->
            (C(actual move pattern -> replacement word) -> evidence)

CYCLE = C(actual phase identities) -> C(phase -> successor)
LAYOUT = (column cycle -> row cycle) ->
         ((column-step parameter -> row-step parameter) ->
          C((column phase -> row phase) -> (motif parameter -> transform parameter)))
BINDINGS = C(actual parameter slot -> actual member of its allowed choices)
WINDOW = (actual starting column phase -> actual starting row phase) ->
         (C(column occurrence slots) -> C(row occurrence slots))

A sample retains the complete native program, actual bindings and actual window.
The JSON archive transports that connected native graph with shared identity.
Loading rebinds the declared frame and preserves native correspondence; it does
not call a named motif factory. Parameter changes select existing archived
choices. Every generated unit executes through the existing acknowledged unit
actuator, retaining local requests, receipts, transform reads and blocked work.

Program definition, chosen sample and expanded execution audit are distinct.
This finite template expresses possible patterns; it is not a proof that any
chosen pattern was discovered, optimal, Euclidean-regular or aesthetically good.
There is no new rooted-neighbourhood binning or endpoint negotiation here.

The optional renderer reads accepted endpoint words. It assigns each move reach
display size one. This measured presentation is not part of native execution.
Projected crossings and coincident endpoint positions never create shared sites.

The graph-derived sample inspection below is explanatory text, with local human
aliases. It is not a parsable or canonical semantic file. The executable archive
is program.gojo.json; no label in this report selects execution.
""";

    private static string Html(string cards, IReadOnlyList<OrnamentProgramStudy.Variant> variants, int bytes, bool replay) => """
<!doctype html><html lang='en'><head><meta charset='utf-8'><meta name='viewport' content='width=device-width,initial-scale=1'><title>Gojo · One ornament program, several reads</title><style>
:root{color-scheme:light;--ink:#203a3c;--muted:#65716d;--paper:#efeee7;--line:#d9dcd1;--accent:#205f69}*{box-sizing:border-box}body{margin:0;background:var(--paper);color:var(--ink);font:16px/1.6 system-ui,-apple-system,Segoe UI,sans-serif}main{max-width:1180px;margin:auto;padding:38px 24px 70px}h1{font:500 clamp(32px,5vw,54px)/1.12 Georgia,serif;letter-spacing:-.025em;max-width:850px;margin:12px 0 22px}h2{font:500 28px/1.25 Georgia,serif;margin:4px 0 13px}p{max-width:970px}.eyebrow{font-size:12px;letter-spacing:.12em;color:var(--accent);font-weight:700}.intro{margin:0 0 28px}.intro p{font-size:17px;color:#52645f}.pipeline{display:flex;gap:9px;align-items:center;flex-wrap:wrap;margin:22px 0}.pipeline span{background:#f9f8f2;border:1px solid var(--line);padding:11px 17px;border-radius:7px;font-size:14px}.pipeline b{font-weight:500;color:#86938b}nav{display:flex;flex-wrap:wrap;gap:9px;margin:20px 0 32px}a{color:var(--accent);text-underline-offset:3px}nav a{font-size:13px;background:#e0e7df;padding:7px 13px;border-radius:20px;text-decoration:none}article{margin:28px 0;padding:26px;background:#fffdf7;border:1px solid var(--line);border-radius:10px;scroll-margin-top:15px}header p:not(.eyebrow){color:var(--muted);margin-bottom:20px}figure{margin:0;border:1px solid #e6e2d6;overflow:hidden;border-radius:5px;background:#f9f6ed}figure svg{display:block;width:100%;height:auto}.facts{display:flex;flex-wrap:wrap;gap:24px;padding:18px 0;color:var(--muted);font-size:13px}.facts b{color:var(--ink);font-size:18px;font-weight:600}details{border-top:1px solid var(--line);padding-top:14px;font-size:14px}summary{cursor:pointer;color:var(--accent)}details p{margin-bottom:12px}table{border-collapse:collapse;width:100%;font-size:13px;margin:16px 0}th,td{text-align:left;padding:9px;border-bottom:1px solid var(--line)}th:last-child,td:last-child{text-align:right}.note,footer{color:var(--muted);font-size:13px}.download{padding:16px 20px;border:1px solid var(--line);background:#e4ebe2;border-radius:7px}.download a{margin-right:20px}code{font-size:.9em;background:#e5e9e0;padding:2px 5px;border-radius:3px}@media(max-width:600px){main{padding:22px 12px}article{padding:16px}.facts{gap:12px}.pipeline{gap:5px}.pipeline span{padding:7px 10px}}
</style></head><body><main><div class='intro'><p class='eyebrow'>GOJO · REUSABLE PRE-NUMERIC CONSTRUCTIONS</p><h1>One ornament program,<br>several realizations.</h1><p>A motif vocabulary, explicit word substitutions, finite phase cycles and narrow parameter bindings describe a repeating field. The archived definition stays separate from the window we sample and the much larger history of accepted execution.</p><div class='pipeline'><span>Native definition</span><b>→</b><span>Actual parameter bindings</span><b>→</b><span>Phase origin + window</span><b>→</b><span>Acknowledged unit flows</span></div></div>
""" + $"<div class='download'><a href='program.gojo.json' download>Download native sample · {bytes:N0} bytes</a><a href='encoding.txt'>Inspect actual motifs, rules and bindings</a><a href='diagnostics.json'>Inspect diagnostics</a></div><p class='note'>{(replay ? "Standalone replay: this gallery loaded its complete sample from an external archive." : "P02–P06 reuse the loaded archive. Their bindings select actual choices already inside that archive; no source factory regenerates the motifs. Stored motif vocabulary: 1 diamond (teal), 2 stepped hook (copper), 3 zigzag (violet). Transforms: 1 identity, 2 left/right mirror, 3 quarter-turn.")} Hollow endpoint rings mark distinct sites with the same display position. Overpass gaps mark projected crossings.</p><nav>{string.Join("", variants.Select(variant => $"<a href='#{variant.Id}'>{variant.Id} · {H(variant.Name)}</a>"))}</nav>" + cards + """
<footer><p>This is a finite authored generation format and a native archive demonstration. It does not yet synthesize executable machines from discovered motif graphs, learn a construction policy, negotiate neighbouring seams or introduce rooted descriptor bins. The optional renderer chooses lengths, colour and thickness; the program uses shared symbolic reaches, actual identities, relations and accepted flow.</p><p>Replay with <code>--ornament-program --input program.gojo.json --output DIRECTORY</code>.</p></footer></main></body></html>
""";

    private static string H(string text) => WebUtility.HtmlEncode(text);
    private static string F(double number) => number.ToString("0.###", CultureInfo.InvariantCulture);
}
