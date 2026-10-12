using System.Diagnostics;
using System.Globalization;
using System.Text.Json;

/// <summary>A paired sensor-resolution trial over existing founders and ecology.</summary>
internal static class VisionTrials
{
    internal static async Task Run(string[] args)
    {
        var seconds = args.Length > 1 ? int.Parse(args[1]) : 60;
        var seeds = args.Length > 2 ? args[2] : "7,19";
        var output = Path.GetFullPath(args.Length > 3 ? args[3] :
            "artifacts/phototaxis/vision/" + DateTime.Now.ToString("yyyyMMdd-HHmmss"));
        Directory.CreateDirectory(output);
        var timer = Stopwatch.StartNew();
        foreach (var levels in new[] { 16, 256 })
        {
            Console.WriteLine($"\nExisting founders · {levels} eye levels · {seconds}s per seed · Jev off · fixed resource observations");
            await CognitiveTrials.Run(["cognitive", seconds.ToString(CultureInfo.InvariantCulture), seeds,
                Path.Combine(output, "levels-" + levels), "--eye-levels", levels.ToString(CultureInfo.InvariantCulture),
                "--wall-seconds", "120", "--no-archives"]);
        }
        using var coarse = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(output, "levels-16", "summary.json")));
        using var fine = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(output, "levels-256", "summary.json")));
        var a = coarse.RootElement.GetProperty("results").EnumerateArray().ToArray();
        var b = fine.RootElement.GetProperty("results").EnumerateArray().ToArray();
        var pairs = a.Zip(b).Select(pair => new { seed = pair.First.GetProperty("seed").GetInt32(),
            completed = pair.First.GetProperty("completed").GetBoolean() && pair.Second.GetProperty("completed").GetBoolean(),
            levels16 = pair.First, levels256 = pair.Second }).ToArray();
        object Summary(JsonElement[] rows) => new { lifetime = rows.Average(r => r.GetProperty("lifetime").GetDouble()),
            food = rows.Average(r => r.GetProperty("food").GetInt64()),
            births = rows.Sum(r => r.GetProperty("births").GetInt32()),
            alive = rows.Sum(r => r.GetProperty("alive").GetInt32()),
            stationary = rows.Average(r => r.GetProperty("stationary").GetDouble()),
            equalEyes = rows.Average(r => r.GetProperty("equalEyes").GetDouble()),
            simulationWallSeconds = rows.Sum(r => r.GetProperty("wallSeconds").GetDouble()),
            setupSeconds = rows.Sum(r => r.GetProperty("setupSeconds").GetDouble()) };
        var summary = new { seconds, seeds, completed = pairs.All(p => p.completed),
            wallSeconds = timer.Elapsed.TotalSeconds, levels16 = Summary(a), levels256 = Summary(b) };
        await File.WriteAllTextAsync(Path.Combine(output, "comparison.json"),
            JsonSerializer.Serialize(new { summary, pairs }, new JsonSerializerOptions { WriteIndented = true }));
        Console.WriteLine("\n" + JsonSerializer.Serialize(summary));
        Console.WriteLine("Comparison saved to " + output);
    }
}
