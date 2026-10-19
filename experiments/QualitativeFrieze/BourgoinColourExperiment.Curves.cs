using System.Text.Json;

namespace Gojo.Experiments.QualitativeFrieze;

public static partial class BourgoinColourExperiment
{
    public static int RunCurves(string[] args)
    {
        var output = Path.Combine("experiments", "QualitativeFrieze", "results", "ornament-curves");
        for (var index = 0; index < args.Length; index++)
            if (args[index] == "--output" && index + 1 < args.Length) output = args[++index];
            else throw new ArgumentException("Usage: --ornament-curves [--output DIRECTORY]");
        Directory.CreateDirectory(output);
        var results = new List<DetailResult>();
        foreach (var example in OrnamentCurveExamples.Create())
        {
            var result = RenderDesign(example.Design, output, 3, 3,
                picture => DetailStyle(picture, example.Design.SourceId), detailViews: true, presentationHints: example.CurveHints);
            results.Add(new(example.Design.Id, "study", example.Notes,
                "Selected native guides have an optional bounded curve display. Pinned public landmarks, starts and ends remain fixed; this does not add a native curvature or clearance law.",
                result with { Plate = result.Plate with { Svg = "" } }));
        }
        var html = DetailHtml(results, output)
            .Replace("Detail that follows the structure", "Curves that follow the guides")
            .Replace("Detail that follows<br>the structure", "Curves that follow<br>the guides")
            .Replace("Ten detail recipes explore how small ornament can have its own internal structure while supporting a larger pattern; four integrations bring that vocabulary back to B02 and B05.",
                "Three limited studies compare angular guides with selected curves. The corrected vine and curl retain their actual branches; the guilloche retains its real interruption.")
            .Replace("<a href='../ornament-designs/gallery.html'>Earlier 108 designs</a>",
                "<a href='../ornament-refined/gallery.html'>The refined 108 designs</a><a href='../ornament-details/gallery.html'>Earlier detail studies</a>")
            .Replace("GojoOrnamentDetailStudy.md", "GojoOrnamentRefinedCollection.md")
            .Replace("ornament-detail-directions.md", "ornament-refined-directions.md");
        foreach (var result in results)
        {
            var id = result.Design.Plate.Id;
            html = html.Replace($"<option value='{id}.svg'>Repeated field</option>",
                $"<option value='{id}-closeup-angular.svg'>One angular unit</option><option value='{id}.svg'>Curved repeated field</option><option value='{id}-angular.svg'>Angular repeated field</option>");
        }
        File.WriteAllText(Path.Combine(output, "gallery.html"), html);
        File.WriteAllText(Path.Combine(output, "overview.svg"), DetailOverview(results, output)
            .Replace("Detail that follows the structure", "Curves that follow the guides"));
        File.WriteAllText(Path.Combine(output, "overview.html"), """
<!doctype html><html lang='en'><head><meta charset='utf-8'><meta name='viewport' content='width=device-width,initial-scale=1'><title>Three curve studies</title><style>body{margin:0;background:#eee8db;font:16px system-ui}nav{padding:22px}a{color:#275d68}img{display:block;width:100%;height:auto}</style></head><body><nav><a href='gallery.html'>Three curve studies</a> · <a href='overview.svg'>SVG</a> · <a href='overview.png'>PNG</a></nav><img src='overview.svg' alt='Vine, returning curl and interrupted guilloche with selected curved presentations'></body></html>
""");
        File.WriteAllText(Path.Combine(output, "diagnostics.json"), JsonSerializer.Serialize(new
        {
            Schema = "gojo-ornament-curves-v1", AllRunsComplete = true, Cases = results,
            Limits = new[] { "Optional bounded curve interpretation over imported native chord guides.",
                "Native selected paths and fixed ports survive packet import by actual reference identity.",
                "Separate gap endpoints stay exact. No hidden continuity or over/under order is invented.",
                "Unpinned fillets provide tangent-direction continuity; pinned corners may remain angular.",
                "The D02 stem and D07 curl were structurally rerouted to avoid their former overlaps before smoothing." }
        }, JsonOptions));
        Console.WriteLine("Wrote three curve studies to " + Path.GetFullPath(output)); return 0;
    }
}
