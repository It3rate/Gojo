using System.IO.Compression;
using System.Text;
using System.Text.Json;
using Gojo.Core;
using Gojo.Encoding.Structures;
using Gojo.Patterns;
using N = Gojo.Experiments.QualitativeFrieze.FieldNative;

namespace Gojo.Experiments.QualitativeFrieze;

public static partial class BourgoinColourExperiment
{
    private sealed record DesignResult(string Mode, string SkeletonName, SceneResult Plate);
    private const string FineMassRule = "[data-collection][data-focus=detail] .field .mass{opacity:.32}";

    // Roles deliberately travel through unusual warm/cool combinations.
    // These are presentation interpretations, not encoded pigment promises.
    private static readonly Palette[] DesignPalettes =
    [
        new("Peacock, jade & coral", "#102F35", "#28766B", "#D36C51", "#E2C16E", "#D8AE58", "#F4E8CA", "#122B30", .12),
        new("Aubergine, peach & parchment", "#24273C", "#6A3D65", "#D99C88", "#F4E5CB", "#C0A363", "#F4E5CB", "#211E31", .12),
        new("Ivory, oxblood & sage", "#F2E7D0", "#7B2935", "#8A9E7E", "#C48144", "#C99B45", "#FAF2DF", "#352E35", .12),
        new("Charcoal, burnt orange & olive", "#1E2727", "#89914D", "#C46734", "#E9E1C7", "#C6A458", "#F2E9D1", "#182323", .12),
        new("Blush, petrol & mulberry", "#F0DEDA", "#245B68", "#8B4059", "#D8A63F", "#B9924A", "#FFF2DB", "#2A3D42", .12),
        new("Ultramarine, sky & vermilion", "#203A76", "#829EB4", "#C64C40", "#F2E7C9", "#D4B065", "#FFF2DC", "#192846", .12),
        new("Saffron, ink & porcelain", "#D3A84C", "#244954", "#6F3B46", "#F6EACD", "#D8BD74", "#F6EACD", "#203B40", .12),
        new("Chalk, ink-blue & terracotta", "#EBE6DC", "#314F73", "#A65D46", "#B9974D", "#C5A25A", "#FFF6E2", "#263B4A", .12),
        new("Celadon, plum & apricot", "#D1DECA", "#506D64", "#703F60", "#E1A45E", "#BF9854", "#F7EED7", "#303C3E", .12),
        new("Mulberry, malachite & citron", "#4B2542", "#3E7970", "#A89143", "#E3B99F", "#D7BB6E", "#F5E6CE", "#2C2336", .12),
        new("Oxblood, lapis & shell", "#652B33", "#345C78", "#C49565", "#EFE3CD", "#D6B369", "#F7E8CF", "#342931", .12),
        new("Night garden, lilac & copper", "#202E37", "#718878", "#9585AA", "#D39762", "#C7AD72", "#EFE6D2", "#172730", .12)
    ];

    public static int RunDesigns(string[] args)
    {
        var output = Path.Combine("experiments", "QualitativeFrieze", "results", "ornament-designs");
        var columns = 5; var rows = 5; string? only = null; var assemble = false; var platesOnly = false;
        var resume = false; var modes = Roles;
        for (var index = 0; index < args.Length; index++)
            if (args[index] == "--output" && index + 1 < args.Length) output = args[++index];
            else if (args[index] == "--columns" && index + 1 < args.Length && int.TryParse(args[++index], out var cc) && cc is >= 2 and <= 8) columns = cc;
            else if (args[index] == "--rows" && index + 1 < args.Length && int.TryParse(args[++index], out var rr) && rr is >= 2 and <= 8) rows = rr;
            else if (args[index] == "--only" && index + 1 < args.Length) only = args[++index];
            else if (args[index] == "--assemble") assemble = true;
            else if (args[index] == "--plates-only") platesOnly = true;
            else if (args[index] == "--resume") resume = true;
            else if (args[index] == "--modes" && index + 1 < args.Length) modes = args[++index].Split(',');
            else throw new ArgumentException("Usage: --ornament-designs [--columns 2..8] [--rows 2..8] [--only B01,B26] [--modes field,motif,detail] [--resume] [--plates-only | --assemble] [--output DIRECTORY]");
        if (modes.Length == 0 || modes.Distinct().Count() != modes.Length || modes.Any(mode => !Roles.Contains(mode)))
            throw new ArgumentException("Choose distinct design modes from field, motif and detail.");
        Directory.CreateDirectory(output);
        if (assemble)
        {
            var restored = Directory.GetFiles(output, "*-record.json").Select(file =>
                JsonSerializer.Deserialize<DesignResult>(File.ReadAllText(file), JsonOptions) ??
                throw new InvalidOperationException("An exported design record could not be read."))
                .OrderBy(result => int.Parse(result.Plate.SourceId.AsSpan(1, 2)))
                .ThenBy(result => result.Plate.SourceId, StringComparer.Ordinal)
                .ThenBy(result => Array.IndexOf(Roles, result.Mode))
                .Select(result => result with { Plate = result.Plate with { Svg = ReadDesignSvg(output, result.Plate.Id) } }).ToArray();
            if (restored.Length == 0) throw new ArgumentException("No completed design records exist in the requested output.");
            WriteDesignCollection(restored, output);
            return 0;
        }
        var selected = only?.Split(',').ToHashSet(StringComparer.Ordinal);
        var sources = new BourgoinPatternLibrary().Recipes.Select((source, index) => (Source: source, Index: index))
            .Where(pair => selected is null || selected.Contains(pair.Source.Id)).ToArray();
        if (sources.Length == 0) throw new ArgumentException("No source skeleton matched --only.");
        var designs = sources.SelectMany(pair =>
        {
            var unfinished = modes.Where(mode => !resume || !HasSavedDesign(output, "O-" + pair.Source.Id + "-" + mode, columns, rows)).ToArray();
            return unfinished.Length == 0 ? [] :
                BourgoinOrnamentDesignLibrary.Create(new BourgoinPatternRefinement(pair.Source, 4).Result, pair.Index, unfinished);
        });
        var results = new List<DesignResult>();
        foreach (var design in designs)
        {
            results.Add(RenderDesign(design, output, columns, rows));
        }
        if (!platesOnly && !resume && modes.Length == 3) WriteDesignCollection(results, output);
        else if (!platesOnly) Console.WriteLine("Partial/resumed plates saved. Use --assemble after the requested triples are complete.");
        Console.WriteLine($"Wrote {results.Count} designs to {Path.GetFullPath(output)}");
        return 0;
    }

    private static DesignResult RenderDesign(BourgoinOrnamentDesignLibrary.Design design, string output,
        int columns, int rows, Func<string, string>? presentation = null, bool detailViews = false,
        Relation? presentationHints = null, bool widerWindow = false)
    {
        var source = design.Source;
        var installed = new OrnamentEnrichmentProgram(source.Program, source.Bindings, source.SeamContract, design.Enrichment);
        var initial = (Relation)source.Window.Left;
        var archive = installed.Save(OrnamentProgram.Window(initial.Left, initial.Right, columns, rows), presentationHints);
        var archivePath = Path.Combine(output, design.Id + ".gojo.json.gz");
        WriteNative(archivePath, archive);
        using var compressed = new GZipStream(File.OpenRead(archivePath), CompressionMode.Decompress);
        using var reader = new StreamReader(compressed);
        if (!OrnamentFieldPacket.TryLoad(reader.ReadToEnd(), out var packet, out _) ||
            N.E(packet.Program.Motifs) is not [Relation motif] ||
            !OrnamentEnrichment.TryReadEnrichment(packet.Program.Frame, motif, out var enrichment))
            throw new InvalidOperationException("Every design must recover its native layers from the exported packet.");
        var layerRows = N.E(enrichment.Layers).Cast<Relation>().ToArray();
        if (layerRows.Length != 3) throw new InvalidOperationException("A design requires three connected native levels.");
        var selections = new HashSet<Relation>[3]; var focuses = new List<IElement>(); var added = 0; var anchors = 0;
        for (var layer = 0; layer < 3; layer++)
        {
            if (!enrichment.TryReadLayer(layerRows[layer], out var definition, out var chosen, out _, out var additions, out var attachments))
                throw new InvalidOperationException("A native design layer did not expose its selections and attachments.");
            selections[layer] = new(N.E(chosen).Concat(N.E(additions)).Cast<Relation>(), ReferenceEqualityComparer.Instance);
            anchors += N.E(attachments).Length; added += N.E(additions).Length;
            var focus = enrichment.Focus(definition.Left);
            if (!focus.IsComplete) throw new InvalidOperationException("All three native focus reads must carry their siblings.");
            focuses.Add(focus.Surface);
        }
        WriteNative(Path.Combine(output, design.Id + "-focus.gojo.json.gz"), JsonSerializer.Serialize(
            GojoStructureCompactJson.FromElement("ornament-focus-reads-v1", N.S(focuses.ToArray())),
            new JsonSerializerOptions(JsonSerializerDefaults.Web)));
        var run = packet.Program.Generate(packet.Bindings, packet.Window);
        var field = new OrnamentProgramField(run, packet.SeamContract);
        if (!run.IsComplete || !field.IsComplete) throw new InvalidOperationException("Ornament must preserve the source's accepted finite seams.");
        var paths = ReadPaths(run, source.DisplayBasis);
        var curves = ReadCurvePaths(packet);
        var palette = DesignPalettes[design.PaletteIndex % DesignPalettes.Length] with { Band = design.Weight };
        string Style(string picture) => picture
            .Replace("data-focus='motif'", "data-collection='true' data-focus='" + design.Mode + "'")
            .Replace("</style>", """
[data-collection] .detail .edge{stroke-width:calc(var(--band) * .24 + 2px)}
[data-collection] .detail .keyline{stroke-width:calc(var(--band) * .24 + .9px)}
[data-collection] .detail .pigment{stroke-width:calc(var(--band) * .24)}
[data-collection][data-focus=field] .motif{opacity:.62}
[data-collection][data-focus=field] .detail{opacity:.62}
[data-collection][data-focus=detail] .field{opacity:.68}
[data-collection][data-focus=detail] .motif{opacity:.68}
</style>
""");
        string Present(string picture) => presentation?.Invoke(RefreshDesignStyles(Style(picture))) ?? RefreshDesignStyles(Style(picture));
        var svg = Present(Svg(design.Id, design.Title, paths, selections, packet, source.DisplayBasis, palette, columns, rows,
            curves: curves, widerWindow: widerWindow));
        File.WriteAllText(Path.Combine(output, design.Id + ".svg"), svg);
        if (presentationHints is not null)
            File.WriteAllText(Path.Combine(output, design.Id + "-angular.svg"), Present(Svg(design.Id + "-angular",
                design.Title + " · chord interpretation", paths, selections, packet, source.DisplayBasis, palette, columns, rows,
                widerWindow: widerWindow)));
        var skeleton = svg.Replace("data-view='ornament'", "data-view='skeleton'");
        File.WriteAllText(Path.Combine(output, design.Id + "-construction.svg"), skeleton);
        if (detailViews)
        {
            var unit = paths.Where(path => path.Unit == 0).ToArray();
            File.WriteAllText(Path.Combine(output, design.Id + "-closeup.svg"), Present(Svg(design.Id + "-closeup",
                design.Title + " · one executed unit", unit, selections, packet, source.DisplayBasis, palette, columns, rows,
                fitAll: true, curves: curves)));
            if (presentationHints is not null)
                File.WriteAllText(Path.Combine(output, design.Id + "-closeup-angular.svg"), Present(Svg(design.Id + "-closeup-angular",
                    design.Title + " · one executed chord unit", unit, selections, packet, source.DisplayBasis, palette, columns, rows, fitAll: true)));
            var retained = N.E(enrichment.Source.Paths).Cast<Relation>().ToHashSet(ReferenceEqualityComparer.Instance);
            var sourcePaths = paths.Where(path => retained.Contains(path.Source)).ToArray();
            File.WriteAllText(Path.Combine(output, design.Id + "-source.svg"), Present(Svg(design.Id + "-source",
                design.Title + " · retained scaffold", sourcePaths, selections, packet, source.DisplayBasis, palette, columns, rows)));
        }
        var nativeRoot = new Relation(packet.Program.Surface, packet.SeamContract);
        var participants = Reachable(packet.PresentationHints is null ? nativeRoot : new Relation(nativeRoot, packet.PresentationHints)).ToArray();
        var numeric = participants.Count(element => element is Carrier or CarrierLocation);
        if (numeric != 0) throw new InvalidOperationException("The native ornament design must remain pre-numeric.");
        var plate = new SceneResult(design.Id, design.Title, design.SourceId, design.Description, svg, run.Units.Count,
            N.E(enrichment.Result.Paths).Length,
            N.E(enrichment.Result.Paths).Cast<Relation>().Sum(path => N.E((Relation)path.Right).Length), added, anchors,
            N.E(field.Graph.Strokes).Length, N.E(field.Accepted).Length, N.E(field.Blocked).Length,
            N.E(field.Unresolved).Length, N.E(field.Frontier).Length, participants.Length, numeric,
            selections.Select(layer => layer.Count).ToArray(), [true, true, true], palette.Name);
        var result = new DesignResult(design.Mode, source.Name, plate);
        File.WriteAllText(Path.Combine(output, design.Id + "-record.json"),
            JsonSerializer.Serialize(result with { Plate = plate with { Svg = "" } }, JsonOptions));
        Console.WriteLine($"{design.Id}: {plate.Chords} chords/unit, {added} additions, {plate.Accepted} accepted seams, {plate.Blocked} blocked / {plate.Open} open.");
        return result;
    }

    private static IReadOnlyDictionary<Relation, IReadOnlySet<int>> ReadCurvePaths(OrnamentFieldPacket packet)
    {
        var result = new Dictionary<Relation, IReadOnlySet<int>>(ReferenceEqualityComparer.Instance);
        if (packet.PresentationHints is null) return result;
        if (!N.Read(packet.PresentationHints, out var hints)) throw new InvalidOperationException("Curve hints must be a closed native selection scope.");
        var motifs = N.E(packet.Program.Motifs).Cast<Relation>().ToArray();
        foreach (var hint in hints)
        {
            OrnamentCurveSelection? selected = null;
            foreach (var actual in motifs)
                if (hint is Relation surface && OrnamentCurveSelection.TryRead(packet.Program.Frame, actual, surface, out var read))
                { selected = read; break; }
            if (selected is null) throw new InvalidOperationException("A curved presentation must select actual imported motif paths and fixed ports.");
            foreach (var path in N.E(selected.Selected).Cast<Relation>())
                if (!result.TryAdd(path, selected.FixedLandmarks(path)))
                    throw new InvalidOperationException("A path must have one unambiguous curve presentation read.");
        }
        return result;
    }

    private static void WriteDesignCollection(IReadOnlyList<DesignResult> results, string output)
    {
        if (results.Any(result => result.Plate.Blocked != 0 || result.Plate.Open != 0 || result.Plate.NumericParticipants != 0 ||
            result.Plate.FocusComplete.Length != 3 || result.Plate.FocusComplete.Any(complete => !complete)) ||
            results.GroupBy(result => result.Plate.SourceId).Any(group => !Roles.All(role => group.Count(result => result.Mode == role) == 1)))
            throw new InvalidOperationException("Collection assembly requires three completed native designs per source.");
        File.WriteAllText(Path.Combine(output, "diagnostics.json"), JsonSerializer.Serialize(new
        {
            Schema = "gojo-ornament-designs-v1", AllRunsComplete = true,
            Skeletons = results.Select(result => result.Plate.SourceId).Distinct().Count(), Designs = results.Count,
            Cases = results.Select(result => new { result.Mode, result.Plate.Id, result.Plate.SourceId, result.Plate.Title,
                result.Plate.Units, result.Plate.Paths, result.Plate.Chords, result.Plate.AddedPaths, result.Plate.Attachments,
                result.Plate.Strokes, result.Plate.Accepted, result.Plate.Blocked, result.Plate.Open, result.Plate.Frontier,
                result.Plate.NativeObjects, result.Plate.NumericParticipants, result.Plate.LayerPaths, result.Plate.FocusComplete, result.Plate.Palette }),
            Limits = new[] {
                "Three separately authored enrichment programs per retained skeleton, not an autonomous learned style.",
                "Added finite contours and actual landmark attachments are native; colour, width, edging, fill and crop are display interpretations.",
                "Closed contour fill does not establish native occupied faces, clearance, smooth curvature or knot order.",
                "Jones's design preferences guide authorship; no universal beauty or historical pigment theorem is claimed."
            }
        }, JsonOptions));
        File.WriteAllText(Path.Combine(output, "gallery.html"), DesignHtml(results, output, false));
        File.WriteAllText(Path.Combine(output, "overview.html"), DesignHtml(results, output, true));
        File.WriteAllText(Path.Combine(output, "overview.svg"), DesignOverview(results));
        File.WriteAllText(Path.Combine(output, "preview.svg"), DesignOverview(results.Where(result =>
            new[] { "B06.B", "B26", "B28", "B31" }.Contains(result.Plate.SourceId)).ToArray()));
        Console.WriteLine($"Assembled {results.Count} designs at {Path.GetFullPath(output)}");
    }

    private static void WriteNative(string file, string json)
    {
        using var zip = new GZipStream(File.Create(file), CompressionLevel.SmallestSize);
        using var writer = new StreamWriter(zip, new UTF8Encoding(false));
        writer.Write(json);
    }

    private static string RefreshDesignStyles(string svg) => svg.Contains(FineMassRule, StringComparison.Ordinal)
        ? svg : svg.Replace("</style>", FineMassRule + "</style>", StringComparison.Ordinal);

    // Presentation can be revised after native execution. This changes only
    // the explicitly selected closed-mass contrast, retaining every path and
    // all of the independently saved native programs and focus witnesses.
    private static string ReadDesignSvg(string output, string id)
    {
        var file = Path.Combine(output, id + ".svg");
        var svg = RefreshDesignStyles(File.ReadAllText(file)); File.WriteAllText(file, svg);
        var construction = Path.Combine(output, id + "-construction.svg");
        if (File.Exists(construction)) File.WriteAllText(construction, RefreshDesignStyles(File.ReadAllText(construction)));
        return svg;
    }

    private static bool HasSavedDesign(string output, string id, int columns, int rows)
    {
        var record = Path.Combine(output, id + "-record.json");
        if (!File.Exists(record)) return false;
        var result = JsonSerializer.Deserialize<DesignResult>(File.ReadAllText(record), JsonOptions);
        if (!(result?.Plate is { Blocked: 0, Open: 0, NumericParticipants: 0 } plate && plate.Units == columns * rows &&
            plate.FocusComplete.Length == 3 && plate.FocusComplete.All(complete => complete) &&
            new[] { ".svg", "-construction.svg", ".gojo.json.gz", "-focus.gojo.json.gz" }
                .All(suffix => File.Exists(Path.Combine(output, id + suffix))))) return false;
        using var compressed = new GZipStream(File.OpenRead(Path.Combine(output, id + ".gojo.json.gz")), CompressionMode.Decompress);
        using var reader = new StreamReader(compressed);
        return OrnamentFieldPacket.TryLoad(reader.ReadToEnd(), out var packet, out _) &&
            packet.Window.Right is Relation { Left: Relation actualColumns, Right: Relation actualRows } &&
            N.E(actualColumns).Length == columns && N.E(actualRows).Length == rows;
    }
}
