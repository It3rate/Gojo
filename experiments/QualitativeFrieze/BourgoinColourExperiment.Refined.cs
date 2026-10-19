using System.Text.Json;
using System.Xml.Linq;

namespace Gojo.Experiments.QualitativeFrieze;

public static partial class BourgoinColourExperiment
{
    private sealed record RefinedResult(DesignResult Design, string ConstructionNotes, int CurvedPlacedPaths);

    public static int RunRefined(string[] args)
    {
        var output = Path.Combine("experiments", "QualitativeFrieze", "results", "ornament-refined");
        var samples = 3; string? only = null; var modes = Roles; var assemble = false; var platesOnly = false; var resume = false;
        for (var index = 0; index < args.Length; index++)
            if (args[index] == "--output" && index + 1 < args.Length) output = args[++index];
            else if (args[index] == "--samples" && index + 1 < args.Length && int.TryParse(args[++index], out var size) && size is >= 3 and <= 6) samples = size;
            else if (args[index] == "--only" && index + 1 < args.Length) only = args[++index];
            else if (args[index] == "--modes" && index + 1 < args.Length) modes = args[++index].Split(',');
            else if (args[index] == "--assemble") assemble = true;
            else if (args[index] == "--plates-only") platesOnly = true;
            else if (args[index] == "--resume") resume = true;
            else throw new ArgumentException("Usage: --ornament-refined [--samples 3..6] [--only B02,B05] [--modes field,motif,detail] [--plates-only | --assemble] [--resume] [--output DIRECTORY]");
        if (modes.Length == 0 || modes.Distinct(StringComparer.Ordinal).Count() != modes.Length || modes.Any(mode => !Roles.Contains(mode)))
            throw new ArgumentException("Choose distinct design modes from field, motif and detail.");
        Directory.CreateDirectory(output);
        if (assemble)
        {
            var restored = Directory.GetFiles(output, "*-refinement.json").Select(file =>
                JsonSerializer.Deserialize<RefinedResult>(File.ReadAllText(file), JsonOptions) ??
                throw new InvalidOperationException("A refinement display record could not be read."))
                .OrderBy(result => int.Parse(result.Design.Plate.SourceId.AsSpan(1, 2)))
                .ThenBy(result => result.Design.Plate.SourceId, StringComparer.Ordinal)
                .ThenBy(result => Array.IndexOf(Roles, result.Design.Mode)).ToArray();
            WriteRefined(restored, output); return 0;
        }
        var selected = only?.Split(',').ToHashSet(StringComparer.Ordinal);
        var sources = new BourgoinPatternLibrary().Recipes.Select((source, index) => (Source: source, Index: index))
            .Where(pair => selected is null || selected.Contains(pair.Source.Id)).ToArray();
        if (sources.Length == 0) throw new ArgumentException("No source skeleton matched --only.");
        var results = new List<RefinedResult>();
        foreach (var pair in sources)
        {
            var unfinished = modes.Where(mode => !resume || !HasSavedRefined(output, $"R-{pair.Source.Id}-{mode}", samples)).ToArray();
            if (unfinished.Length == 0) continue;
            var source = new BourgoinPatternRefinement(pair.Source, 8).Result;
            foreach (var refined in BourgoinOrnamentRefinedLibrary.Create(source, pair.Index, unfinished))
            {
                var design = RenderDesign(refined.Design, output, samples, samples, RefinedStyle,
                    detailViews: true, presentationHints: refined.CurveHints, widerWindow: true);
                var curved = XDocument.Parse(design.Plate.Svg).Descendants().Count(element =>
                    element.Name.LocalName == "path" && (string?)element.Attribute("data-curve") == "true");
                var result = new RefinedResult(design with { Plate = design.Plate with { Svg = "" } }, refined.ConstructionNotes, curved);
                File.WriteAllText(Path.Combine(output, refined.Design.Id + "-refinement.json"), JsonSerializer.Serialize(result, JsonOptions));
                results.Add(result);
            }
        }
        if (!platesOnly && !resume && modes.Length == 3) WriteRefined(results, output);
        else if (!platesOnly) Console.WriteLine("Plates retained. Use --assemble when the requested triples are complete.");
        return 0;
    }

    private static string RefinedStyle(string svg) => svg.Replace("data-collection='true'", "data-collection='true' data-refined='true'")
        .Replace("</style>", """
[data-refined][data-focus=detail] .field{opacity:.55}
[data-refined][data-focus=detail] .field .mass{opacity:.16}
[data-refined][data-focus=detail] .motif{opacity:.86}
[data-refined] .detail .edge{stroke-width:calc(var(--band) * .2 + 1.6px)}
[data-refined] .detail .keyline{stroke-width:calc(var(--band) * .2 + .7px)}
[data-refined] .detail .pigment{stroke-width:calc(var(--band) * .2)}
</style>
""");

    private static bool HasSavedRefined(string output, string id, int samples)
    {
        if (!HasSavedDesign(output, id, samples, samples) ||
            !new[] { "-refinement.json", "-angular.svg", "-closeup.svg", "-closeup-angular.svg", "-source.svg" }
                .All(suffix => File.Exists(Path.Combine(output, id + suffix)))) return false;
        var refined = JsonSerializer.Deserialize<RefinedResult>(File.ReadAllText(Path.Combine(output, id + "-refinement.json")), JsonOptions);
        return refined?.Design.Plate.Id == id && JsonSerializer.Serialize(refined.Design, JsonOptions) ==
            File.ReadAllText(Path.Combine(output, id + "-record.json"));
    }

    private static void WriteRefined(IReadOnlyList<RefinedResult> records, string output)
    {
        if (records.Count == 0 || records.Select(record => record.Design.Plate.Id).Distinct(StringComparer.Ordinal).Count() != records.Count ||
            records.Any(record => record.Design.Plate is not { Blocked: 0, Open: 0, NumericParticipants: 0 } ||
                record.Design.Plate.FocusComplete.Length != 3 || record.Design.Plate.FocusComplete.Any(complete => !complete)) ||
            records.GroupBy(record => record.Design.Plate.SourceId).Any(group => !Roles.All(role => group.Count(record => record.Design.Mode == role) == 1)))
            throw new InvalidOperationException("Refined collection assembly requires three completed native designs per skeleton.");
        foreach (var record in records)
        {
            var samples = (int)Math.Sqrt(record.Design.Plate.Units);
            if (samples * samples != record.Design.Plate.Units || !HasSavedRefined(output, record.Design.Plate.Id, samples))
                throw new InvalidOperationException("Refined display records must agree with their completed native exports.");
        }
        var results = records.Select(record => record.Design with
        { Plate = record.Design.Plate with { Svg = File.ReadAllText(Path.Combine(output, record.Design.Plate.Id + ".svg")) } }).ToArray();
        File.WriteAllText(Path.Combine(output, "gallery.html"), DesignHtml(results, output, false, refined: true));
        File.WriteAllText(Path.Combine(output, "overview.html"), DesignHtml(results, output, true, refined: true));
        File.WriteAllText(Path.Combine(output, "overview.svg"), DesignOverview(results).Replace("A grammar in colour", "A flowing grammar").Replace("Fine ornament", "Ornamental detail"));
        File.WriteAllText(Path.Combine(output, "preview.svg"), DesignOverview(results.Where(result =>
            new[] { "B02", "B05", "B32" }.Contains(result.Plate.SourceId)).ToArray()).Replace("A grammar in colour", "A flowing grammar").Replace("Fine ornament", "Ornamental detail"));
        File.WriteAllText(Path.Combine(output, "diagnostics.json"), JsonSerializer.Serialize(new
        {
            Schema = "gojo-ornament-refined-v1", AllRunsComplete = true,
            Skeletons = records.Select(record => record.Design.Plate.SourceId).Distinct().Count(), Designs = records.Count,
            Cases = records.Select(record => new { record.Design.Mode, record.ConstructionNotes, record.CurvedPlacedPaths, record.Design.Plate }),
            Limits = new[] {
                "Native guides, prototypes, transforms, layers and landmark attachments remain pre-numeric; smoothing is an explicitly selected presentation.",
                "Version 2 packets retain actual path-linked curve selections and fixed public ports in the same native identity graph.",
                "Bounded quadratic fillets preserve open endpoints, gaps and pinned connection landmarks; unpinned corners have tangent-direction continuity.",
                "Curve controls, colour, contour fill and the crop are display interpretations, not native curvature, clearance or occupied-area proofs.",
                "A finite native field is shown closer to its outer frontier than in the earlier collection; no unexecuted repetitions are invented.",
                "Construction and curve display are authored, not autonomous learning or a universal aesthetic score."
            }
        }, JsonOptions));
        Console.WriteLine($"Assembled {records.Count} refined designs at {Path.GetFullPath(output)}");
    }
}
