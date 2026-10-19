using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;
using Gojo.Core;
using Gojo.Encoding.Structures;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;
using N = Gojo.Experiments.QualitativeFrieze.FieldNative;

namespace Gojo.Experiments.QualitativeFrieze;

/// <summary>Display interpretations over imported native enrichment programs.
/// Colour, edging, clipping and contour fill introduce no physical contact,
/// filled-face or perceptual beauty law.</summary>
public static partial class BourgoinColourExperiment
{
    private sealed record Palette(string Name, string Ground, string Field, string Motif,
        string Detail, string Gold, string Pale, string Ink, double Band);
    private static readonly Palette[] Palettes =
    [
        new("Lapis, madder & old gold", "#182F40", "#356170", "#B85847", "#DCB76B", "#C89E55", "#EFE1C0", "#132832", .15),
        new("Ivory, verdigris & coral", "#E7DEC7", "#6E8D80", "#2D536E", "#B45343", "#B99253", "#FAF0D8", "#263F43", .17),
        new("Indigo stems & vermilion flowers", "#EAE0C8", "#2B5673", "#57775E", "#B84F40", "#BD9650", "#F8EED7", "#273C47", .9),
        new("Copper, slate & champagne", "#292E40", "#70858A", "#AC6651", "#D8B673", "#C3A061", "#EFE1C5", "#202A36", .38)
    ];
    private static readonly string[] Roles = ["field", "motif", "detail"];
    private static readonly string[] Labels = ["Overall disposition", "Intermediate motif", "Fine ornament"];
    private sealed record Point(double X, double Y);
    private sealed record DrawPath(string Id, Relation Source, Point[] Points, bool Closed, int Unit);
    private sealed record SceneResult(string Id, string Title, string SourceId, string Description,
        string Svg, int Units, int Paths, int Chords, int AddedPaths, int Attachments,
        int Strokes, int Accepted, int Blocked, int Open, int Frontier, int NativeObjects,
        int NumericParticipants, int[] LayerPaths, bool[] FocusComplete, string Palette);
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };

    public static int Run(string[] args)
    {
        var output = Path.Combine("experiments", "QualitativeFrieze", "results", "coloured-ornament");
        var columns = 5; var rows = 5;
        for (var index = 0; index < args.Length; index++)
            if (args[index] == "--output" && index + 1 < args.Length) output = args[++index];
            else if (args[index] == "--columns" && index + 1 < args.Length && int.TryParse(args[++index], out var cc) && cc is >= 2 and <= 8) columns = cc;
            else if (args[index] == "--rows" && index + 1 < args.Length && int.TryParse(args[++index], out var rr) && rr is >= 2 and <= 8) rows = rr;
            else throw new ArgumentException("Usage: --bourgoin-colour [--columns 2..8] [--rows 2..8] [--output DIRECTORY]");
        Directory.CreateDirectory(output);
        var results = new List<SceneResult>(); var scenes = BourgoinColourLibrary.Create();
        for (var index = 0; index < scenes.Count; index++)
        {
            var scene = scenes[index];
            var installed = new OrnamentEnrichmentProgram(scene.Source.Program, scene.Source.Bindings,
                scene.Source.SeamContract, scene.Enrichment);
            var initial = (Relation)scene.Source.Window.Left;
            var window = OrnamentProgram.Window(initial.Left, initial.Right, columns, rows);
            var archive = installed.Save(window);
            File.WriteAllText(Path.Combine(output, scene.Id + ".gojo.json"), archive);
            if (!OrnamentFieldPacket.TryLoad(archive, out var packet, out _) ||
                N.E(packet.Program.Motifs) is not [Relation motif] ||
                !OrnamentEnrichment.TryReadEnrichment(packet.Program.Frame, motif, out var enrichment))
                throw new InvalidOperationException("A coloured scene must replay its native layers without its authoring factory.");
            var layerRows = N.E(enrichment.Layers).Cast<Relation>().ToArray();
            if (layerRows.Length != 3) throw new InvalidOperationException("This display needs three explicit native layer questions.");
            var selections = new HashSet<Relation>[3]; var focuses = new List<IElement>(); var anchors = 0; var added = 0;
            for (var layer = 0; layer < layerRows.Length; layer++)
            {
                if (!enrichment.TryReadLayer(layerRows[layer], out var definition, out var selected, out _, out var additions, out var attachments))
                    throw new InvalidOperationException("A retained layer must expose its actual native participation.");
                selections[layer] = new HashSet<Relation>(N.E(selected).Concat(N.E(additions)).Cast<Relation>(), ReferenceEqualityComparer.Instance);
                anchors += N.E(attachments).Length; added += N.E(additions).Length;
                focuses.Add(enrichment.Focus(definition.Left).Surface);
            }
            File.WriteAllText(Path.Combine(output, scene.Id + "-focus.gojo.json"), JsonSerializer.Serialize(
                GojoStructureCompactJson.FromElement("ornament-focus-reads-v1", N.S(focuses.ToArray())),
                new JsonSerializerOptions(JsonSerializerDefaults.Web)));
            var run = packet.Program.Generate(packet.Bindings, packet.Window);
            var field = new OrnamentProgramField(run, packet.SeamContract);
            if (!run.IsComplete || !field.IsComplete) throw new InvalidOperationException("Decoration must preserve a complete native field and its source seams.");
            var paths = ReadPaths(run, scene.Source.DisplayBasis);
            var palette = Palettes[index % Palettes.Length];
            var svg = Svg(scene.Id, scene.Title, paths, selections, packet, scene.Source.DisplayBasis, palette, columns, rows);
            File.WriteAllText(Path.Combine(output, scene.Id + ".svg"), svg);
            foreach (var role in Roles) File.WriteAllText(Path.Combine(output, scene.Id + "-" + role + ".svg"),
                svg.Replace("data-focus='motif'", "data-focus='" + role + "'"));
            var participants = Reachable(new Relation(packet.Program.Surface, packet.SeamContract)).ToArray();
            var numeric = participants.Count(element => element is Carrier or CarrierLocation);
            var focusComplete = layerRows.Select(row => enrichment.Focus(((Relation)row.Left).Left).IsComplete).ToArray();
            var result = new SceneResult(scene.Id, scene.Title, scene.SourceId, scene.Description, svg,
                run.Units.Count, N.E(enrichment.Result.Paths).Length,
                N.E(enrichment.Result.Paths).Cast<Relation>().Sum(path => N.E((Relation)path.Right).Length), added, anchors,
                N.E(field.Graph.Strokes).Length, N.E(field.Accepted).Length, N.E(field.Blocked).Length,
                N.E(field.Unresolved).Length, N.E(field.Frontier).Length, participants.Length, numeric,
                selections.Select(layer => layer.Count).ToArray(), focusComplete, palette.Name);
            if (numeric != 0 || focusComplete.Any(complete => !complete)) throw new InvalidOperationException("A scene lost its native pre-numeric focus contract.");
            results.Add(result);
            Console.WriteLine($"{scene.Id}: {result.Units} units, {result.Chords} chords/unit, {result.AddedPaths} added paths, {result.Attachments} witnessed anchors; {result.Accepted} seams, 0 blocked/open.");
        }
        File.WriteAllText(Path.Combine(output, "diagnostics.json"), JsonSerializer.Serialize(new
        {
            Schema = "gojo-coloured-ornament-v1", AllRunsComplete = true,
            Cases = results.Select(result => new { result.Id, result.Title, result.SourceId, result.Units,
                result.Paths, result.Chords, result.AddedPaths, result.Attachments, result.Strokes, result.Accepted,
                result.Blocked, result.Open, result.Frontier, result.NativeObjects, result.NumericParticipants,
                result.LayerPaths, result.FocusComplete, result.Palette }),
            Limits = new[] { "Authored ornament and selected historical style; no learned aesthetics or image recognition.",
                "SVG colour, contour fill, edging and clipping are presentation interpretations, not native occupied faces or crossing order.",
                "Focus controls select precomputed native reads and retain siblings; opacity is visual contrast, not material change.",
                "Finite chord ornaments do not establish analytic circles, curves or tangency.",
                "The rectangular display crops the accepted finite field. It does not prove an infinite wallpaper or colour-area theorem." }
        }, JsonOptions));
        File.WriteAllText(Path.Combine(output, "gallery.html"), Html(results, output));
        File.WriteAllText(Path.Combine(output, "overview.svg"), Overview(results));
        File.WriteAllText(Path.Combine(output, "overview.html"), "<!doctype html><html lang='en'><meta charset='utf-8'><meta name='viewport' content='width=device-width,initial-scale=1'><title>Four ornament readings</title><style>body{margin:0;background:#eae3d3;font:16px system-ui}nav{padding:20px}a{color:#24545d}img{width:100%;display:block}</style><nav><a href='gallery.html'>Interactive collection</a> · <a href='overview.svg'>Standalone SVG</a></nav><img src='overview.svg' alt='Four coloured ornamental fields'>");
        Console.WriteLine("Wrote " + Path.GetFullPath(output)); return 0;
    }

    // Read geometry from acknowledged receipts, keeping the actual native
    // path correspondence of the placed program. Names never choose geometry.
    private static List<DrawPath> ReadPaths(OrnamentProgramRun run, BourgoinPatternLibrary.DisplayBasis basis)
    {
        var result = new List<DrawPath>(); var id = 0;
        for (var unitIndex = 0; unitIndex < run.Units.Count; unitIndex++)
        {
            var unit = run.Units[unitIndex]; var revision = unit.Current;
            var positions = N.E(revision.Bindings).Cast<Relation>().ToDictionary(row => row.Left,
                row => Position(run.Program.Frame, (Relation)row.Right, basis), N.Participants.Instance);
            var evidence = (Relation)revision.ExecutedProgram.Evidence;
            var transformation = (Relation)evidence.Right; var application = (Relation)transformation.Right;
            var correspondences = (Relation)application.Left;
            var pathRows = N.E((Relation)correspondences.Left).Cast<Relation>().ToArray();
            var receipts = N.E(revision.Receipts).Cast<Relation>().Where(row => unit.Drawing.TryReadDraw(row.Left, out _)).ToArray();
            var byWord = new Dictionary<IElement, Queue<Relation>>(N.Participants.Instance);
            foreach (var receipt in receipts)
            {
                unit.Drawing.TryReadDraw(receipt.Left, out var actual);
                if (!byWord.TryGetValue(actual, out var queue)) byWord.Add(actual, queue = new());
                queue.Enqueue(receipt);
            }
            var used = new HashSet<Relation>(ReferenceEqualityComparer.Instance);
            foreach (var pathRow in pathRows)
            {
                var sourcePath = (Relation)pathRow.Left; var placedPath = (Relation)pathRow.Right;
                var points = new List<Point>();
                foreach (var word in N.E((Relation)placedPath.Right).Cast<Relation>())
                {
                    if (!byWord.TryGetValue(word, out var queue) || !queue.TryDequeue(out var row) ||
                        !unit.Drawing.TryReadAccepted((Relation)row.Right, out _, out var added) || N.E(added) is not [Relation stroke] ||
                        !QualitativeStrokeGraph.TryReadStroke(stroke, out _, out var from, out var to, out _))
                        throw new InvalidOperationException("A presented chord must have one actual acknowledged stroke.");
                    used.Add(row); if (points.Count == 0) points.Add(positions[from]); points.Add(positions[to]);
                }
                result.Add(new("p" + id++, sourcePath, points.ToArray(),
                    N.E(revision.Program.ClosedPaths).Any(path => ReferenceEquals(path, sourcePath)), unitIndex));
            }
            if (used.Count != receipts.Length) throw new InvalidOperationException("Every accepted drawing chord must retain a path correspondence.");
        }
        return result;
    }

    private static Point Position(QualitativeMotionFrame frame, Relation word, BourgoinPatternLibrary.DisplayBasis basis)
    {
        double x = 0, y = 0;
        foreach (var entry in N.E(word).Cast<Relation>())
        {
            if (!frame.Direction(entry, out var axis, out var positive, out _)) throw new InvalidOperationException("A rendered word must read in its exposed frame.");
            var sign = positive ? 1 : -1;
            if (ReferenceEquals(axis, frame.Horizontal)) { x += sign * basis.RX; y += sign * basis.RY; }
            else { x += sign * basis.UX; y += sign * basis.UY; }
        }
        return new(x, y);
    }
    private static IEnumerable<IElement> Reachable(IElement root)
    {
        var seen = new HashSet<IElement>(N.Participants.Instance); var stack = new Stack<IElement>(); stack.Push(root);
        while (stack.TryPop(out var actual))
        { if (!seen.Add(actual)) continue; yield return actual; if (actual is Relation relation) { stack.Push(relation.Left); stack.Push(relation.Right); } }
    }
    private static string F(double value) => value.ToString("0.###", CultureInfo.InvariantCulture);
    private static string H(string value) => WebUtility.HtmlEncode(value);
}
