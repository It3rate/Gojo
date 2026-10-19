using System.Text;
using System.Text.Json;
using System.Xml.Linq;

namespace Gojo.Experiments.QualitativeFrieze;

public static partial class BourgoinColourExperiment
{
    private sealed record DetailResult(string Id, string Kind, string ConstructionNotes, string Limits, DesignResult Design);
    private const string InterlaceBandRule = """
[data-detail-study=D09] .motif .edge{stroke-width:calc(var(--band) * 1.5 + 5px)}
[data-detail-study=D09] .motif .keyline{stroke-width:calc(var(--band) * 1.5 + 4px)}
[data-detail-study=D09] .motif .pigment{stroke-width:calc(var(--band) * 1.5 + 3px);stroke:var(--motif)}
""";

    public static int RunDetails(string[] args)
    {
        var output = Path.Combine("experiments", "QualitativeFrieze", "results", "ornament-details");
        string? only = null; var assemble = false; var platesOnly = false;
        for (var index = 0; index < args.Length; index++)
            if (args[index] == "--output" && index + 1 < args.Length) output = args[++index];
            else if (args[index] == "--only" && index + 1 < args.Length) only = args[++index];
            else if (args[index] == "--assemble") assemble = true;
            else if (args[index] == "--plates-only") platesOnly = true;
            else throw new ArgumentException("Usage: --ornament-details [--only D01,D11] [--plates-only | --assemble] [--output DIRECTORY]");
        Directory.CreateDirectory(output);
        if (assemble)
        {
            var restored = Directory.GetFiles(output, "*-study.json").Select(file =>
                JsonSerializer.Deserialize<DetailResult>(File.ReadAllText(file), JsonOptions) ??
                throw new InvalidOperationException("A detail study record could not be read.")).OrderBy(result => result.Id, StringComparer.Ordinal).ToArray();
            WriteDetails(restored, output); return 0;
        }
        var studies = only is null ? OrnamentDetailLibrary.Create() : OrnamentDetailLibrary.Create(only.Split(','));
        var results = new List<DetailResult>();
        foreach (var study in studies)
        {
            var samples = study.Kind == "integration" ? 4 : 3;
            var design = RenderDesign(study.Design, output, samples, samples,
                picture => DetailStyle(picture, study.Id), detailViews: true);
            var result = new DetailResult(study.Id, study.Kind, study.ConstructionNotes, study.Limits,
                design with { Plate = design.Plate with { Svg = "" } });
            File.WriteAllText(Path.Combine(output, study.Id + "-study.json"), JsonSerializer.Serialize(result, JsonOptions));
            results.Add(result);
        }
        if (!platesOnly) WriteDetails(results, output);
        return 0;
    }

    private static string DetailStyle(string svg, string id) => svg
        .Replace("data-collection='true'", "data-collection='true' data-detail-study='" + id + "'")
        .Replace("</style>", """
[data-collection][data-focus=detail] .field{opacity:.48}
[data-collection][data-focus=detail] .field .mass{opacity:.12}
[data-collection][data-focus=detail] .motif{opacity:.85}
[data-collection] .detail .edge{stroke-width:calc(var(--band) * .18 + 1.5px)}
[data-collection] .detail .keyline{stroke-width:calc(var(--band) * .18 + .6px)}
[data-collection] .detail .pigment{stroke-width:calc(var(--band) * .18)}
[data-detail-study=D09] .layer .edge{stroke-width:calc(var(--band) * .65 + 2px)}
[data-detail-study=D09] .layer .keyline{stroke-width:calc(var(--band) * .65 + .9px)}
[data-detail-study=D09] .layer .pigment{stroke-width:calc(var(--band) * .65)}
[data-detail-study=D09] .detail .mass{opacity:.1}
</style>
""").Replace("</style>", InterlaceBandRule + "</style>");

    private static void WriteDetails(IReadOnlyList<DetailResult> results, string output)
    {
        if (results.Count == 0 || results.Select(result => result.Id).Distinct(StringComparer.Ordinal).Count() != results.Count ||
            results.Any(result => result.Design.Plate is not { Blocked: 0, Open: 0, NumericParticipants: 0 } ||
                result.Design.Plate.Units != (result.Kind == "integration" ? 16 : 9) ||
                result.Design.Plate.FocusComplete.Length != 3 || result.Design.Plate.FocusComplete.Any(complete => !complete)))
            throw new InvalidOperationException("Detail collection assembly needs completed, distinct, imported native studies.");
        foreach (var result in results)
            foreach (var suffix in new[] { ".svg", "-construction.svg", "-closeup.svg", "-source.svg", ".gojo.json.gz", "-focus.gojo.json.gz" })
                if (!File.Exists(Path.Combine(output, result.Design.Plate.Id + suffix)))
                    throw new InvalidOperationException("A detail study is missing an exported view or native packet.");
        // A display correction can strengthen the actual interrupted strands
        // without touching their native paths or the retained gap witness.
        foreach (var result in results.Where(result => result.Id == "D09"))
            foreach (var suffix in new[] { ".svg", "-construction.svg", "-closeup.svg", "-source.svg" })
            {
                var file = Path.Combine(output, result.Design.Plate.Id + suffix);
                var picture = File.ReadAllText(file);
                if (!picture.Contains("data-detail-study=", StringComparison.Ordinal)) picture = DetailStyle(picture, result.Id);
                if (!picture.Contains(InterlaceBandRule, StringComparison.Ordinal)) picture = picture.Replace("</style>", InterlaceBandRule + "</style>");
                File.WriteAllText(file, picture);
            }
        File.WriteAllText(Path.Combine(output, "gallery.html"), DetailHtml(results, output));
        File.WriteAllText(Path.Combine(output, "overview.svg"), DetailOverview(results, output));
        File.WriteAllText(Path.Combine(output, "preview.svg"), DetailOverview(results.Where(result =>
            new[] { "D01", "D03", "D05", "D09", "D11", "D13" }.Contains(result.Id)).ToArray(), output));
        File.WriteAllText(Path.Combine(output, "overview.html"), """
<!doctype html><html lang='en'><head><meta charset='utf-8'><meta name='viewport' content='width=device-width,initial-scale=1'><title>Detail that follows the structure · all plates</title><style>body{margin:0;background:#eee8db;color:#303e41;font:16px system-ui}nav{padding:22px;display:flex;flex-wrap:wrap;gap:22px}a{color:#275d68}img{width:100%;height:auto;display:block}</style></head><body><nav><a href='gallery.html'>Browse the detail studies</a><a href='overview.svg'>Zoomable SVG</a><a href='overview.png'>PNG sheet</a></nav><img src='overview.svg' alt='All the native detail studies and their integrations'></body></html>
""");
        File.WriteAllText(Path.Combine(output, "diagnostics.json"), JsonSerializer.Serialize(new
        {
            Schema = "gojo-ornament-details-v1", AllRunsComplete = true,
            Studies = results.Count(result => result.Kind == "study"), Integrations = results.Count(result => result.Kind == "integration"),
            Cases = results.Select(result => new { result.Id, result.Kind, result.ConstructionNotes, result.Limits,
                result.Design.Plate }),
            Limits = new[] { "Authored finite detail programs, not learned aesthetics or automatic fit solving.",
                "The close-up is a read of the first actually executed unit in the same imported field: 3x3 for studies, 4x4 for integrations.",
                "Attachments authenticate actual landmarks and offsets; they do not by themselves prove joins, fit or clearance.",
                "Actual interrupted strands preserve gaps. No hidden continuity or three-dimensional knot order is inferred.",
                "Colour, edging and contour fill are display interpretations; finite chord petals do not prove analytic curvature." }
        }, JsonOptions));
        Console.WriteLine($"Assembled {results.Count} detail plates at {Path.GetFullPath(output)}");
    }

    private static string DetailHtml(IReadOnlyList<DetailResult> results, string output)
    {
        var cards = new StringBuilder();
        foreach (var result in results)
        {
            var plate = result.Design.Plate;
            var image = result.Kind == "study" ? plate.Id + "-closeup.svg" : plate.Id + ".svg";
            var previous = result.Kind == "integration" && plate.SourceId is "B02" or "B05"
                ? Path.GetRelativePath(output, Path.Combine("experiments", "QualitativeFrieze", "results", "ornament-designs", "O-" + plate.SourceId + "-detail.svg")).Replace('\\', '/') : null;
            cards.Append($"<article id='{result.Id}' data-plate='{plate.Id}'><header><span class='eyebrow'>{H(result.Id)} · {H(result.Kind == "study" ? "detail vocabulary" : plate.SourceId + " integration")}</span><h2>{H(plate.Title)}</h2></header><a class='hero' href='{image}'><img loading='lazy' decoding='async' src='{image}' width='1200' height='820' alt='{H(plate.Title)}'></a><div class='controls'><label>View <select class='view'><option value='{plate.Id}-closeup.svg'{(result.Kind == "study" ? " selected" : "")}>One executed unit</option><option value='{plate.Id}.svg'{(result.Kind == "integration" ? " selected" : "")}>Repeated field</option><option value='{plate.Id}-source.svg'>Bare scaffold</option><option value='{plate.Id}-construction.svg'>All paths in one ink</option>" +
                (previous is null ? "" : $"<option value='{previous}'>Previous fine design</option>") +
                $"</select></label><span>{H(plate.Palette)}</span></div><p class='description'>{H(plate.Description)}</p><details><summary>Recipe, native evidence and limits</summary><p>{H(result.ConstructionNotes)}</p><p>{H(result.Limits)}</p><p>{plate.Paths} paths · {plate.Chords} chords per unit · {plate.AddedPaths} added paths / {plate.Attachments} witnessed attachments · {plate.Accepted} accepted seams · {plate.Blocked} blocked / {plate.Open} open.</p><p><a href='{plate.Id}.gojo.json.gz'>Native program · gzip</a> · <a href='{plate.Id}-focus.gojo.json.gz'>Native focus reads</a> · <a href='{plate.Id}-closeup.svg'>Close-up SVG</a> · <a href='{plate.Id}.svg'>Repeated field SVG</a></p>" +
                (result.Id == "D10" && File.Exists(Path.Combine(output, "first-loop", "D10-closeup.svg")) ?
                    "<p><a href='first-loop/D10-closeup.svg'>Earlier loop: axis details cross the diagonal lobes</a> · <a href='first-loop/D10.gojo.json.gz'>Earlier native program</a></p>" : "") + "</details></article>");
        }
        return """
<!doctype html><html lang='en'><head><meta charset='utf-8'><meta name='viewport' content='width=device-width,initial-scale=1'><title>Detail that follows the structure · Gojo</title><style>
*{box-sizing:border-box}body{margin:0;background:#eee8db;color:#303e41;font:16px/1.65 system-ui,sans-serif}main{max-width:1580px;margin:auto;padding:44px 28px 75px}h1,h2{font-family:Georgia,serif;font-weight:400;line-height:1.15}h1{font-size:clamp(40px,5vw,72px);max-width:1000px;margin:15px 0 22px}h2{font-size:28px;margin:8px 0 22px}.eyebrow{font-size:12px;letter-spacing:.15em;text-transform:uppercase;color:#83673e}.intro{max-width:1040px;font-size:19px}a{color:#275d68;text-underline-offset:4px}nav{display:flex;flex-wrap:wrap;gap:15px 25px;margin:25px 0}.index{padding:15px 0;border-block:1px solid #cec1a8}.index a{font-size:13px}.collection{display:grid;grid-template-columns:repeat(2,minmax(0,1fr));gap:20px 38px}article{min-width:0;padding:28px 0;border-bottom:1px solid #cdbfa5;scroll-margin-top:25px}.hero{display:block}.hero img{display:block;width:100%;height:auto;border:1px solid #bbae96}.controls{display:flex;align-items:center;flex-wrap:wrap;gap:12px 22px;margin-top:13px;font-size:12px;color:#756a55}label{display:flex;gap:8px;align-items:center}select{min-width:0;max-width:100%;padding:7px;background:#fff9ee;color:#303e41;border:1px solid #bcae94;font:13px system-ui}.description{font-size:14px}details{font-size:13px}summary{cursor:pointer;color:#395e65}.principles{max-width:1080px;margin-top:40px;border-top:1px solid #cdbfa5;padding-top:22px;font-size:15px}@media(max-width:800px){main{padding:26px 16px 55px}.collection{grid-template-columns:1fr}h2{font-size:26px}}@media print{nav,.controls,details{display:none}article{break-inside:avoid}.collection{gap:15px}}
</style></head><body><main><span class='eyebrow'>Gojo · a focused detail study</span><h1>Detail that follows<br>the structure</h1><p class='intro'>Petals from a centre. Leaves from a stem. Repeated marks along a band. Ten detail recipes explore how small ornament can have its own internal structure while supporting a larger pattern; four integrations bring that vocabulary back to B02 and B05.</p><nav><a href='overview.html'>Every plate on one page</a><a href='overview.svg'>Whole sheet · SVG</a><a href='overview.png'>Whole sheet · PNG</a><a href='../ornament-designs/gallery.html'>Earlier 108 designs</a><a href='#principles'>What this round tests</a></nav>
""" + "<nav class='index'>" + string.Join("", results.Select(result => $"<a href='#{result.Id}'>{H(result.Id)} · {H(result.Design.Plate.Title)}</a>")) + "</nav><div class='collection'>" + cards + """
</div><section class='principles' id='principles'><span class='eyebrow'>Growth · cadence · counterform</span><h2>Attachment is a beginning.</h2><p>A valid offset witness says where an addition comes from. The authored recipe must also give it a coherent direction, parent, repetition and room to breathe. These studies use shared finite prototypes and explicit mirror, translation, rotation or angled substitutions. Each plate executes after native archive import; the close-up and repeated field inspect the same acknowledged paths.</p><p>Scaffold, ornament and finer internal marks remain separate, connected native selections. The woven and knot-like studies preserve actual interrupted strands; a visible gap does not prove unseen continuity or knot topology. All contours are finite chords. Colour, filled silhouettes and outlines remain display choices.</p><p><a href='../../../../Gojo/docs/GojoOrnamentDetailStudy.md'>Findings and reproduction</a> · <a href='../../research/ornament-detail-directions.md'>Historical guidance and art direction</a> · <a href='diagnostics.json'>Execution diagnostics</a></p></section></main><script>for(const card of document.querySelectorAll('article'))card.querySelector('.view').addEventListener('change',e=>{card.querySelector('.hero img').src=e.target.value;card.querySelector('.hero').href=e.target.value;});</script></body></html>
""";
    }

    private static string DetailOverview(IReadOnlyList<DetailResult> results, string output)
    {
        const int width = 2460, tileWidth = 786, tileHeight = 538, rowHeight = 635, header = 135;
        var height = header + (results.Count + 2) / 3 * rowHeight + 25;
        var svg = new StringBuilder($"<svg xmlns='http://www.w3.org/2000/svg' xmlns:xlink='http://www.w3.org/1999/xlink' width='{width}' height='{height}' viewBox='0 0 {width} {height}'><rect width='{width}' height='{height}' fill='#eee8db'/><text x='34' y='57' font-family='Georgia,serif' font-size='40' fill='#303e41'>Detail that follows the structure</text><text x='34' y='94' font-family='sans-serif' font-size='19' fill='#756a55'>Native finite prototypes · explicit mirror, direction and repeat · close-ups and scaffold integrations</text>");
        for (var index = 0; index < results.Count; index++)
        {
            var result = results[index]; var plate = result.Design.Plate;
            var x = 28 + index % 3 * 813; var y = header + index / 3 * rowHeight;
            var file = plate.Id + (result.Kind == "study" ? "-closeup" : "") + ".svg";
            var root = XElement.Parse(File.ReadAllText(Path.Combine(output, file)));
            root.SetAttributeValue("x", x); root.SetAttributeValue("y", y + 44);
            root.SetAttributeValue("width", tileWidth); root.SetAttributeValue("height", tileHeight);
            svg.Append($"<a xlink:href='gallery.html#{result.Id}'><text x='{x + 5}' y='{y + 25}' font-family='Georgia,serif' font-size='24' fill='#303e41'>{H(result.Id + " · " + plate.Title)}</text>{root.ToString(SaveOptions.DisableFormatting)}</a><text x='{x + 5}' y='{y + 610}' font-family='sans-serif' font-size='16' fill='#756a55'>{H(result.Kind == "study" ? "One executed unit · " + plate.Palette : plate.SourceId + " · repeated field · " + plate.Palette)}</text>");
        }
        return svg.Append("</svg>").ToString();
    }
}
