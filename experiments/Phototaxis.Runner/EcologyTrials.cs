using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using Phototaxis;

/// <summary>Compare world settings and a spatial-vision ablation over existing founders.</summary>
internal static class EcologyTrials
{
    internal static async Task Run(string[] args)
    {
        var seconds = args.Length > 1 ? int.Parse(args[1]) : 60;
        var seeds = args.Length > 2 ? args[2] : "7,19";
        var output = Path.GetFullPath(args.Length > 3 ? args[3] :
            "artifacts/phototaxis/ecology/" + DateTime.Now.ToString("yyyyMMdd-HHmmss"));
        Directory.CreateDirectory(output);
        var timer = Stopwatch.StartNew();
        var groups = new List<object>();
        var alternate = args[0] == "roving" ? ("roving", EcologySettings.Roving) : ("focused", EcologySettings.Focused);
        foreach (var (name, settings) in new[] { ("original", EcologySettings.Original), alternate })
        {
            var treatments = new List<(string Name, JsonElement[] Rows)>();
            foreach (var binocular in new[] { true, false })
            {
                var eyes = binocular ? "binocular" : "averaged";
                var destination = Path.Combine(output, name, eyes);
                Console.WriteLine($"\n{name} world · {eyes} eyes · {seconds}s per seed · existing founders · Jev off");
                var trial = new List<string> { "cognitive", seconds.ToString(CultureInfo.InvariantCulture), seeds,
                    destination, "--no-archives", "--light-radius", settings.LightRadius.ToString(CultureInfo.InvariantCulture),
                    "--light-on-scale", settings.LightOnScale.ToString(CultureInfo.InvariantCulture),
                    "--patch-food", settings.PatchHarvestPerPulse.ToString(CultureInfo.InvariantCulture),
                    "--light-drift", settings.LightDriftSpeed.ToString(CultureInfo.InvariantCulture) };
                if (!binocular) trial.Add("--averaged-eyes");
                await CognitiveTrials.Run(trial.ToArray());
                using var doc = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(destination, "summary.json")));
                treatments.Add((eyes, doc.RootElement.GetProperty("results").EnumerateArray().Select(r => r.Clone()).ToArray()));
            }
            object Aggregate(JsonElement[] rows)
            {
                var life = rows.Sum(r => r.GetProperty("lifetime").GetDouble());
                var food = rows.Sum(r => r.GetProperty("food").GetInt64());
                var gradientSamples = rows.Sum(r => r.GetProperty("hungryGradientSamples").GetInt64());
                return new { completed = rows.All(r => r.GetProperty("completed").GetBoolean()),
                    meanLifetime = life / rows.Length, meanFood = food / (double)rows.Length,
                    foodPerCreatureSecond = life == 0 ? (double?)null : food / life,
                    births = rows.Sum(r => r.GetProperty("births").GetInt32()),
                    alive = rows.Sum(r => r.GetProperty("alive").GetInt32()),
                    stationary = rows.Average(r => r.GetProperty("stationary").GetDouble()),
                    hungryToward = rows.Average(r => r.GetProperty("hungryToward").ValueKind == JsonValueKind.Null ?
                        (double?)null : r.GetProperty("hungryToward").GetDouble()),
                    hungryGradientAlignment = gradientSamples == 0 ? (double?)null :
                        rows.Sum(r => r.GetProperty("hungryGradientAlignment").ValueKind == JsonValueKind.Null ? 0 :
                            r.GetProperty("hungryGradientAlignment").GetDouble() * r.GetProperty("hungryGradientSamples").GetInt64()) / gradientSamples,
                    hungryGradientSamples = gradientSamples,
                    localFoodShortfall = rows.Sum(r => r.GetProperty("localFoodShortfall").GetInt64()),
                    simulationWallSeconds = rows.Sum(r => r.GetProperty("wallSeconds").GetDouble()) };
            }
            var pairs = treatments[0].Rows.Zip(treatments[1].Rows).Select(pair => new {
                seed = pair.First.GetProperty("seed").GetInt32(), binocular = pair.First, averaged = pair.Second }).ToArray();
            var group = new { world = name, settings, binocular = Aggregate(treatments[0].Rows), averaged = Aggregate(treatments[1].Rows), pairs };
            groups.Add(group); Console.WriteLine(JsonSerializer.Serialize(new {
                world = name, binocular = group.binocular, averaged = group.averaged }));
        }
        await File.WriteAllTextAsync(Path.Combine(output, "comparison.json"), JsonSerializer.Serialize(new {
            seconds, seeds, wallSeconds = timer.Elapsed.TotalSeconds, groups }, new JsonSerializerOptions { WriteIndented = true }));
        Console.WriteLine("Comparison saved to " + output);
    }
}
