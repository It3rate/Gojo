using System.Globalization;
using System.IO.Compression;
using System.Net;
using System.Text;
using System.Text.Json;
using Gojo.Core;
using Gojo.Encoding.Structures;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;

namespace Gojo.Experiments.QualitativeFrieze;

/// <summary>Presentation of witnessed construction-context experiments. Only
/// this boundary chooses lengths, screen axes, colour and line thickness.</summary>
public static partial class ConstructionContextExperiment
{
    private sealed record Point(double X, double Y);
    private sealed record Line(Point A, Point B, int Colour = 0, bool Guide = false, IElement? From = null, IElement? To = null);
    private sealed record Picture(Line[] Lines, Point[] Starts, Point[][]? Faces = null, int[][]? Memberships = null, Point[]? Sites = null);
    private sealed record StepModel(string Id, string Name, string Description, string Plain, string Construction,
        int Paths, int Chords, int ClosedClaims, int Ports, int Rungs, int Blocked, int Open,
        int Receipts, int NativeElements, int NativeNumericElements, int AcceptedActions);
    private sealed record VariantModel(string Id, string Name, string Description, StepModel[] Steps);
    private sealed record CaseModel(string Id, string Name, string Description, VariantModel[] Variants);
    private sealed record ViewModel(string Id, string Name, string Description, string Source, string Groups,
        string Result, string Ornament, int GroupCount, int SelectedFaces, int Complement,
        int Contacts, int Overlaps, int Actuators, int Decorations);
    private static readonly string[] Colours = ["#246a70", "#b96d48", "#65568f", "#89954f", "#b78d35", "#527aa0"];
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };

    public static int Run(string[] args)
    {
        var output = Path.Combine("experiments", "QualitativeFrieze", "results", "construction-contexts");
        for (var i = 0; i < args.Length; i++)
            if (args[i] == "--output" && i + 1 < args.Length) output = args[++i];
            else throw new ArgumentException("Usage: --construction-contexts [--output DIRECTORY]");
        Directory.CreateDirectory(output);
        var study = ConstructionContextStudy.Build();
        var native = new List<IElement> { study.Surface };
        var cases = new List<CaseModel>();
        foreach (var item in study.Cases)
        {
            var commonWindow = item.Variants.SelectMany(variant => variant.Steps)
                .Where(step => step.RepeatRun is null).SelectMany(step => ProgramWindow(step.Program)).ToArray();
            var variants = new List<VariantModel>();
            foreach (var variant in item.Variants)
            {
                var steps = new List<StepModel>();
                foreach (var step in variant.Steps)
                {
                    var program = step.Program;
                    var actuator = new UnitArrangementNode(step.Name, [program], program, S());
                    var execution = actuator.Current;
                    if (!execution.Join.TryReadPassed(execution.Join.Evaluate()))
                        throw new InvalidOperationException("A displayed program must have acknowledged native execution.");
                    var root = new Relation(step.Surface, new Relation(S(step.Receipts.Cast<IElement>().ToArray()),
                        new Relation(actuator.Surface, execution.Surface)));
                    native.Add(root);
                    var participants = Reachable(root).ToArray();
                    var numeric = participants.Count(value => value is Carrier or CarrierLocation);
                    if (numeric != 0) throw new InvalidOperationException("Construction contexts must remain pre-numeric.");
                    var id = step.Id;
                    var picture = Graph(program.Frame, execution.Join.Graph, execution.Join.Bindings, program);
                    if (step.RepeatRun is {} repeat)
                    {
                        var actualRepeatLines = repeat.Units.SelectMany((unit, index) => Graph(program.Frame,
                            unit.Current.Join.Graph, unit.Current.Join.Bindings).Lines.Select(line => line with { Colour = index })).ToArray();
                        picture = new(actualRepeatLines, repeat.Units.Select(unit => Word(program.Frame, unit.PlacementWord)).ToArray());
                    }
                    var plain = id + ".svg"; var construction = id + "-construction.svg";
                    var window = step.RepeatRun is null ? commonWindow : null;
                    File.WriteAllText(Path.Combine(output, plain), Svg(picture, step.Name, false, window));
                    File.WriteAllText(Path.Combine(output, construction), Svg(picture, step.Name, true, window));
                    steps.Add(new(step.Id, step.Name, step.Description, plain, construction,
                        E(program.Paths).Length, E(program.Paths).Cast<Relation>().Sum(path => E((Relation)path.Right).Length),
                        E(program.ClosedPaths).Length, E(program.Ports).Length, step.Ladder.Rungs.Count,
                        E(step.Ladder.Current.Blocked).Length, E(step.Ladder.Current.Open).Length,
                        step.Receipts.Count, participants.Length, numeric,
                        step.RepeatRun?.Units.Sum(unit => E(unit.Current.Receipts).Length) ?? E(execution.Receipts).Length));
                }
                variants.Add(new(variant.Id, variant.Name, variant.Description, steps.ToArray()));
            }
            cases.Add(new(item.Id, item.Name, item.Description, variants.ToArray()));
        }

        Console.WriteLine("Constructing shared field and alternative views...");
        var views = new ConstructionViewStudy(); native.Add(views.Surface);
        var viewModels = new List<ViewModel>();
        var sourcePositions = Positions(views.Source.Frame, views.Source.Bindings);
        var sourcePicture = Graph(views.Source.Frame, views.Source.Scaffold.Graph, views.Source.Bindings);
        File.WriteAllText(Path.Combine(output, "field-source.svg"), Svg(sourcePicture, "One shared source field", false));
        foreach (var variant in views.Variants)
        {
            var actualGroups = E(variant.View.Groups).Cast<Relation>().Select(group =>
            { variant.View.TryReadGroup(group, out var parts); return E(parts.Faces); }).ToArray();
            var actualFaces = E(views.Source.Scaffold.Faces).Cast<Relation>().ToArray();
            var polygons = actualFaces.Select(face =>
            {
                views.Source.Scaffold.TryReadFace(face, out _, out var vertices);
                return E(vertices).Select(site => sourcePositions[site]).ToArray();
            }).ToArray();
            var memberships = actualFaces.Select(face => actualGroups.Select((members, index) => (members, index))
                .Where(group => group.members.Any(actual => ReferenceEquals(actual, face))).Select(group => group.index).ToArray()).ToArray();
            var grouped = sourcePicture with { Faces = polygons, Memberships = memberships };
            var outputGraph = Graph(views.Source.Frame, variant.Execution.Join!.Graph, variant.Execution.Bindings);
            var result = outputGraph with { Lines = [..sourcePicture.Lines.Select(line => line with { Guide = true }), ..outputGraph.Lines],
                Sites = Positions(views.Source.Frame, variant.Execution.Bindings).Values.ToArray() };
            var ornaments = variant.Decorations.SelectMany(node => Graph(views.Source.Frame,
                node.Current.Join.Graph, node.Current.Join.Bindings).Lines).Select(line => line with { Colour = 1 }).ToArray();
            var embellished = result with { Lines = [..result.Lines, ..ornaments] };
            var groupFile = variant.Id + "-groups.svg"; var resultFile = variant.Id + "-result.svg";
            var ornamentFile = variant.Id + "-ornament.svg";
            // Keep a common display window: choosing a view must not rescale its result invisibly.
            var bounds = sourcePositions.Values.ToArray();
            File.WriteAllText(Path.Combine(output, groupFile), Svg(grouped, variant.Name + " / selected groups", true, bounds));
            File.WriteAllText(Path.Combine(output, resultFile), Svg(result, variant.Name + " / accepted construction", false, bounds));
            File.WriteAllText(Path.Combine(output, ornamentFile), Svg(embellished, variant.Name + " / shared ornament unit", true, bounds));
            viewModels.Add(new(variant.Id, variant.Name, variant.Description, "field-source.svg", groupFile, resultFile, ornamentFile,
                E(variant.View.Groups).Length, E(variant.View.SelectedFaces).Length, E(variant.View.Complement).Length,
                E(variant.Network.Contacts).Length, E(variant.Network.Overlaps).Length, variant.Execution.Units.Length,
                variant.Decorations.Length));
        }

        var complete = S(native.ToArray());
        var all = Reachable(complete).ToArray();
        var numericTotal = all.Count(value => value is Carrier or CarrierLocation);
        if (numericTotal != 0) throw new InvalidOperationException("Native observation and construction must remain unmeasured.");
        var archive = GojoStructureCompactJson.FromElement("construction-context-experiments-v1", complete);
        var json = GojoStructureCompactJson.Serialize(archive);
        if (!GojoStructureCompactJson.TryToElement(GojoStructureCompactJson.Deserialize(json), out var restored) ||
            Reachable(restored.Root).Count() != all.Length)
            throw new InvalidOperationException("The native archive must retain all connected participation.");
        using (var file = File.Create(Path.Combine(output, "contexts.gojo.json.gz")))
        using (var zip = new GZipStream(file, CompressionLevel.SmallestSize))
        using (var writer = new StreamWriter(zip)) writer.Write(json);
        var model = new
        {
            Schema = "gojo-construction-contexts-v1", Cases = cases, Views = viewModels,
            NativeElements = all.Length, NativeNumericElements = numericTotal,
            ArchiveRoundTrip = true,
            Limits = new[] {
                "Authored constructions and grouping proposals; automatic Gestalt discovery is not claimed.",
                "Formal displacement equality does not establish contact, material joining, straightness or Euclidean regularity.",
                "The display selects an equal unit length for each open reach; colour and scale are presentation choices.",
                "Blocked/open edit scopes preserve the last admitted program; a historical ancestor is not a current-shape certificate.",
                "Overlap is non-draining shared observation. Only actual boundary contact creates construction passages.",
                "Counts describe actual structural participation in these supplied contracts; they are not general search complexity or perceptual salience scores.",
                "No model training, learned ranking, image recognition, free-angle polygon geometry or global aesthetic optimum is demonstrated."
            }
        };
        File.WriteAllText(Path.Combine(output, "diagnostics.json"), JsonSerializer.Serialize(model, JsonOptions));
        File.WriteAllText(Path.Combine(output, "gallery.html"), Html(JsonSerializer.Serialize(model, JsonOptions)));
        File.WriteAllText(Path.Combine(output, "overview.html"), Overview(cases, viewModels));
        Console.WriteLine($"Wrote {cases.Count} construction cases / {cases.Sum(item => item.Variants.Sum(variant => variant.Steps.Length))} steps and {viewModels.Count} shared-field designs.");
        Console.WriteLine($"Native elements {all.Length}; numeric elements {numericTotal}; archive round-trip verified.");
        Console.WriteLine(Path.GetFullPath(output));
        return 0;
    }

    private static Picture Graph(QualitativeMotionFrame frame, QualitativeStrokeGraph graph, Relation bindings,
        UnitArrangementProgram? program = null)
    {
        var points = Positions(frame, bindings);
        var groups = program is null ? [] : E(program.Paths).Cast<Relation>().SelectMany((path, index) =>
            E((Relation)path.Right).Select(_ => index)).ToArray();
        var lines = E(graph.Strokes).Cast<Relation>().Select((stroke, index) =>
        {
            if (!QualitativeStrokeGraph.TryReadStroke(stroke, out _, out var from, out var to, out _))
                throw new InvalidOperationException("A render stroke needs an actual graph read.");
            return new Line(points[from], points[to], index < groups.Length ? groups[index] : 0, From: from, To: to);
        }).ToArray();
        var starts = program is null ? [] : E(program.Paths).Cast<Relation>().Select(path => Word(frame, (Relation)path.Left)).ToArray();
        return new(lines, starts);
    }
    private static Dictionary<IElement, Point> Positions(QualitativeMotionFrame frame, Relation bindings) =>
        E(bindings).Cast<Relation>().ToDictionary(row => row.Left, row => Word(frame, (Relation)row.Right),
            (IEqualityComparer<IElement>)ReferenceEqualityComparer.Instance);
    private static IEnumerable<Point> ProgramWindow(UnitArrangementProgram program)
    {
        foreach (var path in E(program.Paths).Cast<Relation>())
        {
            var point = Word(program.Frame, (Relation)path.Left); yield return point;
            foreach (var chord in E((Relation)path.Right).Cast<Relation>())
            {
                var delta = Word(program.Frame, chord); point = new(point.X + delta.X, point.Y + delta.Y);
                yield return point;
            }
        }
    }
    private static Point Word(QualitativeMotionFrame frame, Relation word)
    {
        var x = 0d; var y = 0d;
        foreach (var term in E(word).Cast<Relation>())
        {
            if (!frame.Direction(term, out var axis, out var forward, out _))
                throw new InvalidOperationException("An illustrative word requires an exposed direction.");
            if (ReferenceEquals(axis, frame.Horizontal)) x += forward ? 1 : -1;
            else if (ReferenceEquals(axis, frame.Vertical)) y += forward ? 1 : -1;
            else throw new InvalidOperationException("This display has no interpretation for the native axis.");
        }
        return new(x, y);
    }
    private static string Svg(Picture picture, string title, bool construction, Point[]? window = null)
    {
        const double width = 680, height = 340, padding = 30;
        var bounds = window ?? picture.Lines.SelectMany(line => new[] { line.A, line.B }).Concat(picture.Starts).ToArray();
        if (bounds.Length == 0) bounds = [new(0, 0), new(1, 1)];
        var minX = bounds.Min(point => point.X); var maxX = bounds.Max(point => point.X);
        var minY = bounds.Min(point => point.Y); var maxY = bounds.Max(point => point.Y);
        var scale = Math.Min((width - 2 * padding) / Math.Max(1, maxX - minX), (height - 2 * padding) / Math.Max(1, maxY - minY));
        Point P(Point point) => new(width / 2 + (point.X - (minX + maxX) / 2) * scale,
            height / 2 - (point.Y - (minY + maxY) / 2) * scale);
        var svg = new StringBuilder($"<svg xmlns='http://www.w3.org/2000/svg' width='680' height='340' viewBox='0 0 680 340' role='img' aria-label='{H(title)}'><title>{H(title)}</title><rect width='100%' height='100%' fill='#faf8f1'/>");
        if (picture.Faces is not null && picture.Memberships is not null)
        {
            svg.Append("<defs>");
            for (var i = 0; i < picture.Faces.Length; i++)
            {
                var members = picture.Memberships[i];
                if (members.Length < 2) continue;
                svg.Append($"<pattern id='shared-{i}' width='12' height='12' patternUnits='userSpaceOnUse' patternTransform='rotate(45)'><rect width='12' height='12' fill='{Colours[members[0] % Colours.Length]}' opacity='.32'/><rect width='6' height='12' fill='{Colours[members[1] % Colours.Length]}' opacity='.45'/></pattern>");
            }
            svg.Append("</defs>");
            for (var i = 0; i < picture.Faces.Length; i++)
            {
                var members = picture.Memberships[i];
                var fill = members.Length == 0 ? "#e4e3da" : members.Length == 1 ? Colours[members[0] % Colours.Length] : $"url(#shared-{i})";
                svg.Append($"<polygon points='{string.Join(" ", picture.Faces[i].Select(point => { var p = P(point); return F(p.X) + "," + F(p.Y); }))}' fill='{fill}' fill-opacity='{(members.Length > 1 ? "1" : ".29")}'/>");
            }
        }
        foreach (var line in picture.Lines)
        {
            var a = P(line.A); var b = P(line.B);
            var colour = line.Guide ? "#d9d7cc" : construction ? Colours[line.Colour % Colours.Length] : "#28595e";
            svg.Append($"<line x1='{F(a.X)}' y1='{F(a.Y)}' x2='{F(b.X)}' y2='{F(b.Y)}' stroke='{colour}' stroke-width='{(line.Guide ? ".7" : picture.Faces is null ? "3" : "1.1")}' stroke-linecap='round'/>");
        }
        if (picture.Sites is not null)
        {
            // This is presentation of native incidence, never an intersection
            // inference that feeds back into topology. Only interior projected
            // crossings of edges with no actual shared endpoint get a bridge.
            var ink = picture.Lines.Where(line => !line.Guide).ToArray();
            var crossings = new HashSet<string>();
            for (var i = 0; i < ink.Length; i++) for (var j = i + 1; j < ink.Length; j++)
            {
                var first = ink[i]; var upper = ink[j];
                if (first.From is null || upper.From is null || ReferenceEquals(first.From, upper.From) ||
                    ReferenceEquals(first.From, upper.To) || ReferenceEquals(first.To, upper.From) || ReferenceEquals(first.To, upper.To)) continue;
                var a = P(first.A); var b = P(first.B); var c = P(upper.A); var d = P(upper.B);
                var ux = b.X - a.X; var uy = b.Y - a.Y; var vx = d.X - c.X; var vy = d.Y - c.Y;
                var determinant = ux * vy - uy * vx; if (Math.Abs(determinant) < 1e-9) continue;
                var t = ((c.X - a.X) * vy - (c.Y - a.Y) * vx) / determinant;
                var q = ((c.X - a.X) * uy - (c.Y - a.Y) * ux) / determinant;
                if (t <= 1e-6 || t >= 1 - 1e-6 || q <= 1e-6 || q >= 1 - 1e-6) continue;
                var x = a.X + t * ux; var y = a.Y + t * uy;
                if (!crossings.Add(F(x) + "," + F(y))) continue;
                var norm = Math.Sqrt(vx * vx + vy * vy); var dx = vx / norm * 5; var dy = vy / norm * 5;
                var colour = construction ? Colours[upper.Colour % Colours.Length] : "#28595e";
                svg.Append($"<circle class='bridge' cx='{F(x)}' cy='{F(y)}' r='4.8' fill='#faf8f1'><title>Projected crossing without an actual joined site</title></circle><path d='M{F(x-dx)} {F(y-dy)}L{F(x+dx)} {F(y+dy)}' stroke='{colour}' stroke-width='3' fill='none'/>");
            }
            foreach (var site in picture.Sites)
            {
                var point = P(site);
                svg.Append($"<circle class='site' cx='{F(point.X)}' cy='{F(point.Y)}' r='3.5' fill='#28595e'><title>Actual constructed endpoint role</title></circle>");
            }
        }
        if (construction)
            for (var i = 0; i < picture.Starts.Length; i++)
            {
                var p = P(picture.Starts[i]);
                svg.Append($"<circle cx='{F(p.X)}' cy='{F(p.Y)}' r='4.2' fill='#faf8f1' stroke='{Colours[i % Colours.Length]}' stroke-width='2'><title>Actual path {i + 1} start/reset</title></circle>");
            }
        return svg.Append("</svg>").ToString();
    }
    private static IEnumerable<IElement> Reachable(IElement root)
    {
        var seen = new HashSet<IElement>(ReferenceEqualityComparer.Instance); var pending = new Stack<IElement>(); pending.Push(root);
        while (pending.TryPop(out var value))
        { if (!seen.Add(value)) continue; yield return value; if (value is Relation relation) { pending.Push(relation.Right); pending.Push(relation.Left); } }
    }
    private static Relation S(params IElement[] values) => StructuralConstraint.Closed(values);
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
    private static string H(string value) => WebUtility.HtmlEncode(value);
    private static string F(double value) => value.ToString("0.###", CultureInfo.InvariantCulture);
}
