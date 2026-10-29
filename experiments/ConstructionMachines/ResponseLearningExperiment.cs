using System.Diagnostics;
using System.IO.Compression;
using System.Text.Json;
using Gojo.Core;
using Gojo.Encoding;
using static Gojo.Experiments.ConstructionMachines.MachineExpressions;

namespace Gojo.Experiments.ConstructionMachines;

public static class ResponseLearningExperiment
{
    public sealed record ModelResult(string name, int parameters, int seed, int bestEpoch, double trainingMse, double validationMse,
        ResponseTraining.Metrics test, ResponseTraining.Metrics stress, double? maximumExportDifference,
        ResponseTraining.Epoch[] history, string archive, double fitSeconds);
    public sealed record Example(int id, string split, long[] requests, double[] features, double[] rewards, long[] moves,
        double[][] predictions, int[] choices);

    public static void Write(string outputFolder, bool quick = false)
    {
        var folder = Path.GetFullPath(outputFolder); Directory.CreateDirectory(folder); Directory.CreateDirectory(Path.Combine(folder, "models"));
        Directory.CreateDirectory(Path.Combine(folder, "data"));
        Console.WriteLine("Generating independent local training, validation, test and wider-request problems...");
        var data = LocalResponseData.Generate(quick);
        var index = new Dictionary<string, List<string>>();
        foreach (var (name, patches) in new[] { ("training", data.Train), ("validation", data.Validation), ("test", data.Test), ("stress", data.Stress) })
        {
            index[name] = [];
            for (var start = 0; start < patches.Length; start += 64)
            {
                var file = $"{name}-{start / 64:D3}.gojo.bin.gz";
                Save(Path.Combine(folder, "data", file),
                    G(data.Receipt, G(patches.Skip(start).Take(64).Select(p => (IElement)p.Surface).ToArray())));
                index[name].Add(file);
            }
            Console.WriteLine($"{name}: {patches.Length} whole local problems");
        }
        File.WriteAllText(Path.Combine(folder, "data", "index.json"), JsonSerializer.Serialize(index, new JsonSerializerOptions { WriteIndented = true }));
        var fits = new List<ResponseTraining.Fit>(); var seconds = new List<double>();
        void Fit(Func<ResponseTraining.Fit> build)
        {
            var watch = Stopwatch.StartNew(); var fit = build(); fits.Add(fit); seconds.Add(watch.Elapsed.TotalSeconds);
            Console.WriteLine($"{fit.Name}, seed {fit.Seed}: validation MSE {fit.ValidationMse:G5}; fitted in {watch.Elapsed.TotalSeconds:0.0}s");
        }
        Fit(() => ResponseTraining.Table(data)); Fit(() => ResponseTraining.Linear(data));
        foreach (var seed in new[] { 11, 29, 47 }) Fit(() => ResponseTraining.Neural(data, seed, quick ? 80 : 600));
        var validationChoices = fits.Skip(2).ToDictionary(f => f.Seed, f => ResponseTraining.Evaluate(f.Program, data.Validation));
        var neural = fits.Skip(2).OrderBy(f => validationChoices[f.Seed].meanRegret).ThenBy(f => f.ValidationMse).First();
        var selected = new[] { fits[0], fits[1], neural }; var results = new List<ModelResult>();
        for (var i = 0; i < selected.Length; i++)
        {
            var fit = selected[i]; var archive = $"models/{new[] { "table", "regression", "neural" }[i]}.gojo.bin.gz";
            Save(Path.Combine(folder, archive), fit.Program.Surface);
            var restored = new LocalResponseProgram((Relation)Load(Path.Combine(folder, archive)));
            var difference = 0d;
            foreach (var patch in data.Validation.Take(32))
            {
                var first = Predict(fit.Program, patch); var copy = Predict(restored, patch);
                if (!first.SequenceEqual(copy)) throw new InvalidOperationException("Native response restoration changed predictions.");
                if (fit.Weights.Length > 0)
                    difference = Math.Max(difference, first.Zip(ResponseTraining.PredictBackend(fit.Weights, patch.X), (a, b) => Math.Abs(a - b)).Max());
            }
            var test = ResponseTraining.Evaluate(restored, data.Test); var stress = ResponseTraining.Evaluate(restored, data.Stress);
            results.Add(new(fit.Name, fit.Parameters, fit.Seed, fit.BestEpoch, fit.TrainingMse, fit.ValidationMse,
                test, stress, fit.Weights.Length == 0 ? null : difference, fit.History, archive, seconds[fits.IndexOf(fit)]));
            Console.WriteLine($"{fit.Name}: test regret {test.meanRegret:G5}, best-action rate {test.bestActionRate:P1}; stress regret {stress.meanRegret:G5}");
        }
        var examples = data.Test.Take(16).Concat(data.Stress.Take(8)).Select(p =>
        {
            var predictions = selected.Select(f => Predict(f.Program, p)).ToArray();
            return new Example(p.Id, p.Split, p.Requests, p.X, p.Rewards, p.Moves, predictions,
                predictions.Select(scores => Enumerable.Range(0, scores.Length).MaxBy(i => scores[i])).ToArray());
        }).ToArray();
        var report = new { quick, task = "Choose a local bound for competing requests", features = RequestContext.Names,
            caps = LocalResponseProgram.Caps, counts = new { training = data.Train.Length, validation = data.Validation.Length, test = data.Test.Length, stress = data.Stress.Length },
            models = results, neuralSeeds = fits.Skip(2).Select(f => new { seed = f.Seed, bestEpoch = f.BestEpoch, validationMse = f.ValidationMse,
                validationRegret = validationChoices[f.Seed].meanRegret, selected = f.Seed == neural.Seed, history = f.History }), examples };
        var json = JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(Path.Combine(folder, "learning.json"), json);
        Console.WriteLine("Local training and held-out evaluation complete.");
    }

    public static double[] Predict(LocalResponseProgram program, LocalResponseData.Patch patch)
    {
        var context = RequestContext.Rebind((Relation)F(patch.Surface)[2], program.Inputs);
        return F(F(program.Predict(context))[2]).Select(x => CoupledSettlement.Number(x) / LocalResponseProgram.OutputScale).ToArray();
    }
    public static void Save(string path, IElement root)
    {
        var binary = GojoNumericFormat.Encode(root);
        using var file = File.Create(path); using var stream = new GZipStream(file, CompressionLevel.Fastest); stream.Write(binary);
    }
    public static IElement Load(string path)
    { using var file = File.OpenRead(path); using var stream = new GZipStream(file, CompressionMode.Decompress); using var buffer = new MemoryStream(); stream.CopyTo(buffer); return GojoNumericFormat.Decode(buffer.ToArray())[0]; }
}
