using System.Text.Json;

namespace Gojo.Experiments.QualitativeFrieze.Composition;

public static class CentrelinePatternExperiment
{
    public static int Run(string[] args)
    {
        var output = Path.Combine("artifacts", "centreline-patterns");
        for (var i = 0; i < args.Length; i++)
            if (args[i] == "--output" && i + 1 < args.Length) output = args[++i];
            else throw new ArgumentException("Usage: --centreline-patterns [--output DIRECTORY]");
        Directory.CreateDirectory(output);
        var destination = Path.Combine(output, "study-data.json");
        File.WriteAllText(destination, JsonSerializer.Serialize(CentrelinePatternStudy.Report(),
            new JsonSerializerOptions(JsonSerializerDefaults.Web) { WriteIndented = true }));
        Console.WriteLine("Centre-line pattern readings recorded at " + Path.GetFullPath(destination));
        return 0;
    }
}
