using System.Text.Json;

namespace Gojo.Experiments.QualitativeFrieze.Composition;

/// <summary>Records native reads for the small authored weave and the independent
/// exposed-fragment continuation fixtures. The website consumes this report.</summary>
public static class AngularWeaveExperiment
{
    public static int Run(string[] args)
    {
        var output = Path.Combine("artifacts", "angular-weave");
        for (var i = 0; i < args.Length; i++)
            if (args[i] == "--output" && i + 1 < args.Length) output = args[++i];
            else throw new ArgumentException("Usage: --angular-weave [--output DIRECTORY]");

        var report = new
        {
            Schema = "gojo.angular-weave.v1",
            Scope = "Authored finite native strand and crossing structures, followed by selected local reads. " +
                "Hidden-continuation candidates use exposed evidence and remain hypotheses. " +
                "Display depth is a local ordered realization, not a measured three-dimensional reconstruction.",
            Weave = AngularWeaveStudy.Report(),
            Continuations = WeaveContinuationStudy.Cases(),
            Junctions = WeaveJunctionStudy.Cases()
        };
        Directory.CreateDirectory(output);
        var destination = Path.Combine(output, "study-data.json");
        File.WriteAllText(destination, JsonSerializer.Serialize(report,
            new JsonSerializerOptions(JsonSerializerDefaults.Web) { WriteIndented = true }));
        Console.WriteLine("Angular weave recorded at " + Path.GetFullPath(destination));
        return 0;
    }
}
