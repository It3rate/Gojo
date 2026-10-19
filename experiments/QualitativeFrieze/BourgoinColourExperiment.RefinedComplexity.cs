using System.IO.Compression;
using System.Text;
using System.Text.Json;
using Gojo.Core;
using N = Gojo.Experiments.QualitativeFrieze.FieldNative;

namespace Gojo.Experiments.QualitativeFrieze;

public static partial class BourgoinColourExperiment
{
    // A deliberately small display projection: keeping Report.Evidence here
    // would retain all 108 imported native worlds until the table is written.
    private sealed record RefinedComplexityAssessment(string SourceId, string Mode,
        OrnamentMotifComplexity.ReviewedEstimate? Estimate, OrnamentMotifComplexity.EncodingCounts Encoding);
    private sealed record RefinedComplexityRow(string Id, string SkeletonName, string Title,
        RefinedComplexityAssessment Assessment);

    /// <summary>Reopens the actual exported native archives. No authoring
    /// factory, SVG analysis, or whole-field execution supplies these reads.</summary>
    private static void WriteRefinedComplexity(IReadOnlyList<RefinedResult> records, string output)
    {
        var rows = new List<RefinedComplexityRow>();
        foreach (var record in records)
        {
            var plate = record.Design.Plate;
            using var compressed = new GZipStream(File.OpenRead(Path.Combine(output, plate.Id + ".gojo.json.gz")), CompressionMode.Decompress);
            using var reader = new StreamReader(compressed);
            if (!OrnamentFieldPacket.TryLoad(reader.ReadToEnd(), out var packet, out var unresolved) ||
                N.E(unresolved).Length != 0 || N.E(packet.Program.Motifs) is not [Relation motif] ||
                !OrnamentEnrichment.TryReadEnrichment(packet.Program.Frame, motif, out var enrichment))
                throw new InvalidOperationException($"Complexity review requires {plate.Id}'s actual complete exported enrichment.");
            var assessment = OrnamentMotifComplexity.Review(plate.SourceId, record.Design.Mode, enrichment);
            var counts = assessment.Encoding;
            if (counts.SourcePaths + counts.AddedPaths != plate.Paths ||
                counts.SourceChords + counts.AddedChords != plate.Chords || counts.AddedPaths != plate.AddedPaths)
                throw new InvalidOperationException($"Complexity evidence disagrees with {plate.Id}'s completed native export counts.");
            rows.Add(new(plate.Id, record.Design.SkeletonName, plate.Title,
                new(assessment.SourceId, assessment.Mode, assessment.Estimate, assessment.Encoding)));
        }
        File.WriteAllText(Path.Combine(output, "complexity.json"), JsonSerializer.Serialize(new
        {
            Schema = "gojo-ornament-complexity-review-v1",
            Designs = rows.Count,
            ReviewedDetailVariants = rows.Count(row => row.Assessment.Estimate is not null),
            AllEncodingCountsAgreeWithNativeExports = true,
            SuggestedReviewTrigger = OrnamentMotifComplexity.ReviewTrigger,
            EstimateGranularity = OrnamentMotifComplexity.Granularity,
            Limits = new[]
            {
                "Conceptual estimates are authored construction-review ranges, not measured human cognitive load or autonomous discovery.",
                "Only detail variants have reviewed estimates; other variants retain null and native statistics.",
                "The estimate counts a repeating unit's reusable ornamental vocabulary, not its repeated instances. The underlying lattice, palette and display fillets are outside this denominator.",
                "Native path/chord counts and retained prototype identities are separately recovered encoding statistics; neither is used as the conceptual count.",
                "Twelve is a suggested review trigger. A range crossing it asks for a closer look, not automatic rejection.",
                "No minimum-description-length score, learned chunker, cognitive-load predictor or SUBDUE search is implemented."
            },
            Cases = rows
        }, JsonOptions));
        File.WriteAllText(Path.Combine(output, "complexity.html"), RefinedComplexityHtml(rows));
        Console.WriteLine($"Reviewed {rows.Count} native complexity records; {rows.Count(row => row.Assessment.Estimate is not null)} authored detail ranges.");
    }

    private static string RefinedComplexityHtml(IReadOnlyList<RefinedComplexityRow> rows)
    {
        var reviewed = rows.Where(row => row.Assessment.Estimate is not null).ToArray();
        var html = new StringBuilder("""
<!doctype html><html lang='en'><meta charset='utf-8'><meta name='viewport' content='width=device-width,initial-scale=1'>
<title>Ornament complexity · construction vocabulary and native sharing</title>
<style>
*{box-sizing:border-box}body{margin:0;background:#eee8db;color:#303e41;font:16px/1.55 system-ui}main{max-width:1800px;margin:auto;padding:34px 26px}h1,h2{font-family:Georgia,serif;font-weight:400}h1{font-size:clamp(32px,4vw,58px);line-height:1.12;max-width:1100px}p{max-width:1100px}a{color:#275d68;text-underline-offset:4px}nav{display:flex;flex-wrap:wrap;gap:20px}.read{background:#f8f2e5;border:1px solid #cbbd9f;padding:18px 22px;margin:22px 0;max-width:1250px}.table{overflow-x:auto;border:1px solid #cdbfa5}table{border-collapse:collapse;width:100%;min-width:1050px;background:#f8f3e8}th,td{text-align:left;vertical-align:top;padding:16px;border-bottom:1px solid #d6cbb5}th{font-size:13px;text-transform:uppercase;letter-spacing:.07em;background:#e2dbc9}tbody tr:target{background:#fff7dc;outline:2px solid #b17a32;outline-offset:-2px}.identity{width:200px}.identity strong{font:26px Georgia,serif;display:block}.identity small{display:block;max-width:200px}.identity img{display:block;margin-top:12px;width:180px;height:120px;object-fit:contain;background:#eee7d7;border:1px solid #c6baa4}.estimate{width:145px}.range{font:30px Georgia,serif;white-space:nowrap}.flag{display:block;color:#8a4e1f;font-size:13px;margin-top:8px}.recipe{max-width:590px}.native{font-variant-numeric:tabular-nums;min-width:210px;font-size:14px}.native span{display:block}.quiet{font-size:14px;color:#59635f}.legend{font-size:14px;max-width:1100px}footer{margin-top:28px}@media(max-width:650px){main{padding:24px 14px}th,td{padding:12px}.read{padding:14px}}@media print{nav{display:none}.table{overflow:visible}table{min-width:0}th,td{padding:9px}.identity img{width:120px;height:80px}tr{break-inside:avoid}}
</style><main><h1>How many ideas make a motif?</h1>
<p>A construction review of the ornamental vocabulary in one repeating unit. Reuse matters: eight copies of a petal can be one shape plus an arrangement rule. A single outline can also contain several meaningful substructures. The ranges below keep that ambiguity visible.</p>
<nav><a href='gallery.html'>Refined gallery</a><a href='overview.html'>All designs</a><a href='complexity.json'>All native records · JSON</a><a href='../../../../Gojo/docs/GojoOrnamentMotifComplexity.md'>Method and limits</a></nav>
<div class='read'><strong>Denominator: one unit's reusable ornamental construction vocabulary.</strong> The count includes proposed shape families, internal articulation, symmetry/repetition and attachment rules. Repeated instances do not each add a concept. The underlying lattice, palette and renderer's fillets are outside this motif estimate. These are authored ranges, not measured cognitive load. Field and intermediate variants have native statistics in the JSON, with their conceptual estimates left unreviewed.</div>
<p><strong>12 is a review trigger.</strong> A range extending above 12 asks whether a simpler grouping or a more coherent hierarchy would help. It is neither a mathematical limit nor an automatic aesthetic failure. A larger motif can remain simple; a small vocabulary can still look crowded if placement, colour or layering is poor.</p>
<p class='legend'>The separate encoding column comes from reopening each exported native packet and reading its actual enrichment/prototype correspondences. Counts are per unit and agree with the saved execution records. “Prototype identities” means retained identity sharing, not inferred similarity or discovered human concepts. Unknown ancestry remains unattributed.</p>
<div class='table'><table><thead><tr><th class='identity'>Detail reading</th><th class='estimate'>Reviewed range</th><th>Proposed construction reading</th><th>Recovered encoding · per unit</th></tr></thead><tbody>
""");
        foreach (var row in reviewed)
        {
            var report = row.Assessment; var estimate = report.Estimate!; var counts = report.Encoding;
            var id = H(row.Id); var source = H(report.SourceId);
            var flag = estimate.EntireRangeExceedsReviewTrigger ? "Above the suggested 12: review" :
                estimate.MayExceedReviewTrigger ? "Range crosses 12: review the grouping" : "";
            html.Append($"<tr id='{source}'><td class='identity'><strong><a href='gallery.html#{source}'>{source}</a></strong><small>{H(row.SkeletonName)}</small>" +
                $"<a href='{id}.svg'><img loading='lazy' src='{id}-closeup.svg' width='180' height='120' alt='One native unit of {source} ornamental detail'></a></td>" +
                $"<td class='estimate'><span class='range'>{estimate.Minimum}–{estimate.Maximum}</span>" +
                (flag.Length == 0 ? "" : $"<span class='flag'>{H(flag)}</span>") + "</td>" +
                $"<td class='recipe'>{H(string.Join(" · ", estimate.ConceptualRecipe))}<p class='quiet'>A proposed decomposition; nearby alternative groupings account for the range.</p></td>" +
                $"<td class='native'><span>{counts.SourcePaths} source paths + {counts.AddedPaths} added</span>" +
                $"<span>{counts.SourceChords} source chords + {counts.AddedChords} added</span>" +
                $"<span>{counts.RetainedPrototypeIdentities} prototype identities</span><span>{counts.WitnessedPrototypePlacements} witnessed path placements</span>" +
                $"<span>{counts.ActualSourceAnchors} actual source anchors</span><span>{counts.UnattributedAddedPaths} unattributed added paths</span>" +
                $"<a href='{id}.gojo.json.gz'>Native archive</a> · <a href='{id}-construction.svg'>Construction</a></td></tr>");
        }
        html.Append("""
</tbody></table></div><footer><h2>From declared groupings to discovered substructure</h2>
<p>SUBDUE offers a relevant future direction: compare the description cost of a reusable substructure with the cost of describing the remaining graph using it. Repeated replacement supports a hierarchy, and bounded inexact matching supports related instances. That is a potential next experiment, separate from this authored review. No discovery search, learned chunking or minimum-description-length score is implemented here. <a href='https://arxiv.org/abs/cs/9402102'>Cook and Holder, <em>Substructure Discovery Using Minimum Description Length and Background Knowledge</em> (1994)</a>.</p>
""");
        html.Append($"<p class='quiet'>{rows.Count} exported native designs inspected; {reviewed.Length} detail estimates reviewed. All reported path/chord counts agree with the completed native export records.</p></footer></main></html>");
        return html.ToString();
    }
}
