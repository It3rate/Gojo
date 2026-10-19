using System.Text.Json;
using System.Xml.Linq;

namespace Gojo.Experiments.QualitativeFrieze;

public static partial class BourgoinColourExperiment
{
    private sealed record RefinedResult(DesignResult Design, string ConstructionNotes, int CurvedPlacedPaths);

    public static int RunRefined(string[] args)
    {
        var output = Path.Combine("experiments", "QualitativeFrieze", "results", "ornament-refined");
        var samples = 3; string? only = null; var modes = Roles; var assemble = false; var platesOnly = false; var resume = false; var repaint = false;
        for (var index = 0; index < args.Length; index++)
            if (args[index] == "--output" && index + 1 < args.Length) output = args[++index];
            else if (args[index] == "--samples" && index + 1 < args.Length && int.TryParse(args[++index], out var size) && size is >= 1 and <= 6) samples = size;
            else if (args[index] == "--only" && index + 1 < args.Length) only = args[++index];
            else if (args[index] == "--modes" && index + 1 < args.Length) modes = args[++index].Split(',');
            else if (args[index] == "--assemble") assemble = true;
            else if (args[index] == "--plates-only") platesOnly = true;
            else if (args[index] == "--resume") resume = true;
            else if (args[index] == "--repaint") repaint = true;
            else throw new ArgumentException("Usage: --ornament-refined [--samples 1..6] [--only B02,B05] [--modes field,motif,detail] [--plates-only | --assemble | --repaint] [--resume] [--output DIRECTORY]");
        if (modes.Length == 0 || modes.Distinct(StringComparer.Ordinal).Count() != modes.Length || modes.Any(mode => !Roles.Contains(mode)))
            throw new ArgumentException("Choose distinct design modes from field, motif and detail.");
        Directory.CreateDirectory(output);
        if (repaint)
        {
            if (assemble || resume || platesOnly) throw new ArgumentException("Repainting changes saved SVG presentation only; assemble separately.");
            var selectedIds = only?.Split(',').ToHashSet(StringComparer.Ordinal);
            foreach (var recordPath in Directory.GetFiles(output, "*-refinement.json"))
            {
                var record = JsonSerializer.Deserialize<RefinedResult>(File.ReadAllText(recordPath), JsonOptions) ??
                    throw new InvalidOperationException("A saved refinement record could not be read.");
                if ((selectedIds is not null && !selectedIds.Contains(record.Design.Plate.SourceId)) || !modes.Contains(record.Design.Mode)) continue;
                foreach (var suffix in new[] { ".svg", "-angular.svg", "-construction.svg", "-closeup.svg", "-closeup-angular.svg", "-source.svg" })
                {
                    var file = Path.Combine(output, record.Design.Plate.Id + suffix);
                    File.WriteAllText(file, RestyleRefinedSvg(File.ReadAllText(file)));
                }
                var recoloured = record with { Design = record.Design with { Plate = record.Design.Plate with
                    { Palette = RefinedPaletteLabel(record.Design.Plate.SourceId, record.Design.Mode) } } };
                File.WriteAllText(recordPath, JsonSerializer.Serialize(recoloured, JsonOptions));
                File.WriteAllText(Path.Combine(output, record.Design.Plate.Id + "-record.json"), JsonSerializer.Serialize(recoloured.Design, JsonOptions));
            }
            Console.WriteLine("Repainted retained SVG observations and palette labels; native programs and execution outcomes are unchanged.");
            return 0;
        }
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

    private static string RefinedStyle(string svg) => RestyleRefinedSvg(svg.Replace("data-collection='true'", "data-collection='true' data-refined='true'")
        .Replace("</style>", """
[data-refined][data-focus=detail] .field{opacity:.55}
[data-refined][data-focus=detail] .field .mass{opacity:.16}
[data-refined][data-focus=detail] .motif{opacity:.86}
[data-refined][data-colour-groups=true][data-focus=detail] .motif{opacity:1}
[data-refined][data-colour-groups=true] .field .mass[data-colour-group]{opacity:1}
[data-refined][data-colour-groups=true][data-focus=detail] .field{opacity:.72}
[data-refined] .detail .edge{stroke-width:calc(var(--band) * .2 + 1.6px)}
[data-refined] .detail .keyline{stroke-width:calc(var(--band) * .2 + .7px)}
[data-refined] .detail .pigment{stroke-width:calc(var(--band) * .2)}
</style>
"""));

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
        WriteRefinedComplexity(records, output);
        WriteBalanceComparison(results, output);
        WriteInteriorComparison(results, output);
        File.WriteAllText(Path.Combine(output, "gallery.html"), DesignHtml(results, output, false, refined: true));
        File.WriteAllText(Path.Combine(output, "overview.html"), DesignHtml(results, output, true, refined: true));
        File.WriteAllText(Path.Combine(output, "overview.svg"), DesignOverview(results).Replace("A grammar in colour", "A flowing grammar").Replace("Fine ornament", "Ornamental detail"));
        File.WriteAllText(Path.Combine(output, "preview.svg"), DesignOverview(results.Where(result =>
            new[] { "B08", "B11", "B12", "B13" }.Contains(result.Plate.SourceId)).ToArray()).Replace("A grammar in colour", "A flowing grammar").Replace("Fine ornament", "Ornamental detail"));
        var interiors = results.Where(result => BourgoinOrnamentCompartmentLibrary.Supports(result.Plate.SourceId)).ToArray();
        if (interiors.Length > 0)
        {
            string InteriorPage(bool all) => DesignHtml(interiors, output, all, refined: true)
                .Replace("A flowing grammar", "Ornament fills the field")
                .Replace("href='gallery.html'", "href='interiors-gallery.html'")
                .Replace("href='overview.html'", "href='interiors.html'")
                .Replace("href='overview.svg'", "href='interiors.svg'")
                .Replace("href='overview.png'", "href='interiors.png'")
                .Replace("<a href='interiors-gallery.html'>B08–B14 · filled interiors</a>", "<a href='gallery.html'>All 108 refined designs</a>");
            File.WriteAllText(Path.Combine(output, "interiors-gallery.html"), InteriorPage(false));
            File.WriteAllText(Path.Combine(output, "interiors.html"), InteriorPage(true));
            File.WriteAllText(Path.Combine(output, "interiors.svg"), DesignOverview(interiors)
                .Replace("A grammar in colour", "Ornament fills the field").Replace("Fine ornament", "Ornamental detail")
                .Replace("gallery.html#", "interiors-gallery.html#"));
        }
        File.WriteAllText(Path.Combine(output, "diagnostics.json"), JsonSerializer.Serialize(new
        {
            Schema = "gojo-ornament-refined-v1", AllRunsComplete = true,
            Skeletons = records.Select(record => record.Design.Plate.SourceId).Distinct().Count(), Designs = records.Count,
            Cases = records.Select(record => new { record.Design.Mode, record.ConstructionNotes, record.CurvedPlacedPaths, record.Design.Plate }),
            Limits = new[] {
                "Native guides, prototypes, transforms, layers and landmark attachments remain pre-numeric; smoothing is an explicitly selected presentation.",
                "Version 2 packets retain actual path-linked curve and optional four-colour selections, with fixed public ports in the same native identity graph.",
                "Bounded quadratic fillets preserve open endpoints, gaps and pinned connection landmarks; unpinned corners have tangent-direction continuity.",
                "Curve controls, colour, contour fill and the crop are display interpretations, not native curvature, clearance or occupied-area proofs.",
                "A finite native field is shown closer to its outer frontier than in the earlier collection; no unexecuted repetitions are invented.",
                "Construction and curve display are authored, not autonomous learning or a universal aesthetic score."
            }
        }, JsonOptions));
        Console.WriteLine($"Assembled {records.Count} refined designs at {Path.GetFullPath(output)}");
    }

    private static void WriteInteriorComparison(IReadOnlyList<DesignResult> results, string output)
    {
        var selected = results.Where(result => result.Mode == "detail" &&
            BourgoinOrnamentCompartmentLibrary.Supports(result.Plate.SourceId) &&
            File.Exists(Path.Combine(output, "previous-detail", result.Plate.Id + ".svg"))).ToArray();
        if (selected.Length == 0) return;
        var html = new System.Text.StringBuilder("""
<!doctype html><html lang='en'><meta charset='utf-8'><meta name='viewport' content='width=device-width,initial-scale=1'>
<title>From guide marks to interior ornament · Gojo</title><style>
*{box-sizing:border-box}body{margin:0;background:#eee8db;color:#303e41;font:16px/1.6 system-ui}main{max-width:1800px;margin:auto;padding:32px 24px}h1,h2{font-family:Georgia,serif;font-weight:400}h1{font-size:clamp(32px,4vw,60px);line-height:1.15}a{color:#275d68;text-underline-offset:4px}nav{display:flex;flex-wrap:wrap;gap:24px}article{padding:24px 0;border-bottom:1px solid #cdbfa5}.pair{display:grid;grid-template-columns:1fr 1fr;gap:20px}figure{margin:0;min-width:0}img{display:block;width:100%;height:auto;border:1px solid #bbae96}figcaption{padding:8px 0;font-size:14px}p{max-width:1000px}@media(max-width:700px){.pair{grid-template-columns:1fr}main{padding:24px 14px}}@media print{nav{display:none}article{break-inside:avoid}}
</style><main><h1>From guide marks to interior ornament</h1><p>The earlier detail and its revised reading derive from the same retained skeleton. Larger flowers, shaped colour fields and internal articulation can cover placement lines while retaining the overall course. Each image links to its vector; each native archive retains its own construction.</p><nav><a href='interiors-gallery.html'>The 24 revised designs</a><a href='interiors.html'>Revised sheet</a><a href='gallery.html'>All 108 designs</a></nav>
""");
        foreach (var result in selected)
        {
            var id = H(result.Plate.Id);
            html.Append($"<article id='{H(result.Plate.SourceId)}'><h2>{H(result.Plate.SourceId + " · " + result.SkeletonName)}</h2><div class='pair'>" +
                $"<figure><a href='previous-detail/{id}.svg'><img loading='lazy' src='previous-detail/{id}.svg' width='1200' height='820' alt='Previous detail of {H(result.Plate.SourceId)}'></a><figcaption>Previous detail · checkpoint 919f234 · <a href='previous-detail/{id}.gojo.json.gz'>Native archive</a></figcaption></figure>" +
                $"<figure><a href='{id}.svg'><img loading='lazy' src='{id}.svg' width='1200' height='820' alt='Revised detail of {H(result.Plate.SourceId)}'></a><figcaption>Interior ornament · <a href='{id}.gojo.json.gz'>Native archive</a> · <a href='{id}-construction.svg'>Construction</a></figcaption></figure></div></article>");
        }
        File.WriteAllText(Path.Combine(output, "comparison.html"), html.Append("</main></html>").ToString());
    }

    private static void WriteBalanceComparison(IReadOnlyList<DesignResult> results, string output)
    {
        var selected = results.Where(result => result.Mode == "detail" &&
            File.Exists(Path.Combine(output, "before-balance", result.Plate.Id + ".svg"))).ToArray();
        if (selected.Length == 0) return;
        var html = new System.Text.StringBuilder("""
<!doctype html><html lang='en'><meta charset='utf-8'><meta name='viewport' content='width=device-width,initial-scale=1'>
<title>Silhouette, placement and colour separation · Gojo ornament</title><style>
*{box-sizing:border-box}body{margin:0;background:#eee8db;color:#303e41;font:16px/1.6 system-ui}main{max-width:1900px;margin:auto;padding:32px 24px}h1,h2{font-family:Georgia,serif;font-weight:400}h1{font-size:clamp(32px,4vw,60px);line-height:1.15}a{color:#275d68;text-underline-offset:4px}nav{display:flex;flex-wrap:wrap;gap:20px}article{padding:24px 0;border-bottom:1px solid #cdbfa5;scroll-margin-top:24px}.pair{display:grid;grid-template-columns:1fr 1fr;gap:20px}figure{margin:0;min-width:0}img{display:block;width:100%;height:auto;border:1px solid #bbae96}figcaption{padding:8px 0;font-size:14px}p{max-width:1100px}@media(max-width:700px){.pair{grid-template-columns:1fr}main{padding:24px 14px}}@media print{nav{display:none}article{break-inside:avoid}}
</style><main><h1>Stronger motifs.<br>Clearer neighbours.</h1><p>Twenty-one finer readings gained broader, internally articulated silhouettes. The final edition pares B13/B14 back to four softly coloured interlocking chambers and simple leaves; B32 balances broad diagonal bands against clear space, with small fourfold rosettes at actual opposed crossing landmarks. Its overall reading contains lines only. B10/B11 keep their simpler chamber flowers and continuous parent rails. The other revisions are palette interpretations. These are reviewed authored changes, not a trained aesthetic score.</p><p>Detail describes a role in the whole, rather than a maximum size. A broad flower can replace a tiny mark while preserving its parent course. Compare the earlier checkpoint with the current executed field, then inspect its native construction and proposed conceptual vocabulary.</p><nav><a href='gallery.html'>All 108 designs</a><a href='overview.html'>Whole collection on one page</a><a href='complexity.html'>Conceptual complexity</a><a href='before-balance/manifest.json'>Previous observations · checkpoint</a></nav>
""");
        html.Append("<nav>" + string.Join("", selected.Select(result => $"<a href='#{H(result.Plate.SourceId)}'>{H(result.Plate.SourceId)}</a>")) + "</nav>");
        foreach (var result in selected)
        {
            var id = H(result.Plate.Id); var source = H(result.Plate.SourceId);
            html.Append($"<article id='{source}'><h2>{source} · {H(result.SkeletonName)}</h2><div class='pair'>" +
                $"<figure><a href='before-balance/{id}.svg'><img loading='lazy' src='before-balance/{id}.svg' width='1200' height='820' alt='Previous {source} detail'></a><figcaption>Previous · checkpoint 037527c</figcaption></figure>" +
                $"<figure><a href='{id}.svg'><img loading='lazy' src='{id}.svg' width='1200' height='820' alt='Revised {source} detail'></a><figcaption>Current · <a href='{id}-closeup.svg'>One executed unit</a> · <a href='{id}-construction.svg'>Construction</a> · <a href='complexity.html#{source}'>Conceptual review</a></figcaption></figure></div><p>{H(result.Plate.Description)}</p></article>");
        }
        File.WriteAllText(Path.Combine(output, "balance.html"), html.Append("</main></html>").ToString());
    }
}
