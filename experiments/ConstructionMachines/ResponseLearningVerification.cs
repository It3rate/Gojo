using System.Text.Json;
using Gojo.Core;
using static Gojo.Experiments.ConstructionMachines.MachineExpressions;

namespace Gojo.Experiments.ConstructionMachines;

/// <summary>Recomputes reported local results from the saved native data and
/// programs, independently of the fitter's in-memory feature/label arrays.</summary>
public static class ResponseLearningVerification
{
    public static void Verify(string folder)
    {
        using var report = JsonDocument.Parse(File.ReadAllText(Path.Combine(folder, "learning.json")));
        var quick = report.RootElement.GetProperty("quick").GetBoolean();
        var models = new[] { "table", "regression", "neural" }.Select(name => new LocalResponseProgram(
            (Relation)ResponseLearningExperiment.Load(Path.Combine(folder, "models", name + ".gojo.bin.gz")))).ToArray();
        var totals = new Dictionary<(string Split, int Model), Accumulator>(); var keys = new HashSet<string>(StringComparer.Ordinal);
        var counts = new Dictionary<string, int>(); var files = 0;
        foreach (var split in new[] { "training", "validation", "test", "stress" })
        {
            counts[split] = 0;
            foreach (var path in DataFiles(folder, split))
            {
                files++; var root = F(ResponseLearningExperiment.Load(path));
                foreach (var patch in F(root[1]))
                {
                    var p = F(patch); if (p.Length != 5) throw new InvalidDataException("An exact native teaching patch is required.");
                    var requests = F(p[1]).Select(r => checked((long)CoupledSettlement.Number(F(r)[3]))).ToArray();
                    if (!keys.Add(LocalResponseData.Key(requests))) throw new InvalidDataException("A whole problem or reflection crosses a native split.");
                    var before = requests.Sum(x => Math.Abs(x)); var context = (Relation)p[2]; var branches = F(p[4]); var rewards = new double[branches.Length];
                    for (var i = 0; i < branches.Length; i++)
                    {
                        var b = F(branches[i]); var values = F(b[2]); var movement = (long)CoupledSettlement.Number(values[0]);
                        var cap = LocalResponseProgram.Caps[i]; var sum = requests.Sum(x => Math.Clamp(x, -cap, cap));
                        if (movement != (long)Math.Clamp(sum / 4d, -8, 8)) throw new InvalidDataException("A teacher movement differs from its declared response law.");
                        var gain = before - requests.Sum(x => Math.Abs(x - movement)); var reward = F(values[2]);
                        rewards[i] = CoupledSettlement.Number(reward[2]) / LocalResponseProgram.OutputScale;
                        var sourceRemainder = CoupledSettlement.Number(reward[3]);
                        if (Math.Abs(rewards[i] * before + sourceRemainder - gain) > 1e-8) throw new InvalidDataException("A finite teacher read lost its source-basis remainder.");
                    }
                    for (var m = 0; m < models.Length; m++)
                    {
                        var prediction = models[m].Predict(RequestContext.Rebind(context, models[m].Inputs));
                        var selection = models[m].Select(prediction); var choice = LocalResponseProgram.Choice(selection);
                        if (choice < 0) throw new InvalidDataException("A native model cannot read a saved teaching patch.");
                        var predicted = F(F(prediction)[2]).Select(x => CoupledSettlement.Number(x) / LocalResponseProgram.OutputScale).ToArray();
                        var key = (split, m); if (!totals.TryGetValue(key, out var total)) totals[key] = total = new();
                        total.Count++; total.Mse += predicted.Select((x, i) => Math.Pow(x - rewards[i], 2)).Average();
                        total.Regret += rewards.Max() - rewards[choice]; total.Relief += rewards[choice]; total.Fixed += rewards[0];
                        if (rewards.Max() - rewards[choice] <= 1e-12) total.Best++;
                    }
                    counts[split]++;
                }
            }
        }
        if (!quick)
        {
            var development = LocalResponseData.Generate(true);
            if (development.Train.Concat(development.Validation).Concat(development.Test).Concat(development.Stress)
                .Any(p => keys.Contains(LocalResponseData.Key(p.Requests)))) throw new InvalidDataException("A development problem leaked into the full data.");
        }
        var declared = report.RootElement.GetProperty("counts");
        foreach (var split in counts.Keys) if (counts[split] != declared.GetProperty(split).GetInt32()) throw new InvalidDataException("A saved split has the wrong count.");
        var results = new List<object>(); var declarations = report.RootElement.GetProperty("models").EnumerateArray().ToArray();
        for (var m = 0; m < models.Length; m++) foreach (var split in new[] { "training", "validation", "test", "stress" })
        {
            var total = totals[(split, m)]; var mse = total.Mse / total.Count; var regret = total.Regret / total.Count;
            var d = declarations[m]; var expected = split is "training" or "validation" ? d.GetProperty(split + "Mse").GetDouble() : d.GetProperty(split).GetProperty("mse").GetDouble();
            if (Math.Abs(mse - expected) > 1e-12) throw new InvalidDataException("Native-only prediction error differs from the report.");
            if (split is "test" or "stress")
            {
                var metrics = d.GetProperty(split);
                if (Math.Abs(regret - metrics.GetProperty("meanRegret").GetDouble()) > 1e-12 ||
                    Math.Abs(total.Relief / total.Count - metrics.GetProperty("meanRelief").GetDouble()) > 1e-12 ||
                    Math.Abs((double)total.Best / total.Count - metrics.GetProperty("bestActionRate").GetDouble()) > 1e-12)
                    throw new InvalidDataException("A native-only choice metric differs from the report.");
            }
            results.Add(new { model = d.GetProperty("name").GetString(), split, count = total.Count, mse, regret });
        }
        File.WriteAllText(Path.Combine(folder, "native-verification.json"), JsonSerializer.Serialize(new { passed = true, files, counts,
            distinctReflectionClasses = keys.Count, recomputed = results }, new JsonSerializerOptions { WriteIndented = true }));
        Console.WriteLine($"Verified {keys.Count} native teaching problems from {files} archives; all three programs reproduce their reported metrics.");
    }
    public static string[] DataFiles(string folder, string split)
    {
        var root = Path.GetFullPath(Path.Combine(folder, "data")); var manifest = Path.Combine(root, "index.json");
        if (!File.Exists(manifest)) return Directory.GetFiles(root, split + "-*.gojo.bin.gz").Order().ToArray();
        using var index = JsonDocument.Parse(File.ReadAllText(manifest));
        return index.RootElement.GetProperty(split).EnumerateArray().Select(file =>
        {
            var path = Path.GetFullPath(Path.Combine(root, file.GetString()!));
            if (!path.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("A dataset file is outside its declared data folder.");
            return path;
        }).ToArray();
    }
    private sealed class Accumulator
    { public int Count, Best; public double Mse, Regret, Relief, Fixed; }
}
