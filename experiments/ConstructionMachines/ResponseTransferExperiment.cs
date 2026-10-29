using System.Text.Json;
using Gojo.Core;
using static Gojo.Experiments.ConstructionMachines.MachineExpressions;

namespace Gojo.Experiments.ConstructionMachines;

/// <summary>Frozen-model transfer from scalar patches to coupled constructions.
/// Neither these worlds nor their outcomes participate in fitting or selection.</summary>
public static class ResponseTransferExperiment
{
    public static readonly string[] Names = ["Direct projection", "Fixed local negotiation", "Context table", "Small regression", "Small neural model"];
    public static void Write(string folder, bool quick = false)
    {
        folder = Path.GetFullPath(folder); var nativeFolder = Path.Combine(folder, "native"); var benchmarkFolder = Path.Combine(folder, "benchmark");
        Directory.CreateDirectory(nativeFolder); Directory.CreateDirectory(benchmarkFolder);
        var programs = new[] { "table", "regression", "neural" }.Select(name => new LocalResponseProgram(
            (Relation)ResponseLearningExperiment.Load(Path.Combine(folder, "models", name + ".gojo.bin.gz")))).ToArray();
        var cases = new List<EnsembleExperiment.Case>(); var trials = new List<EnsembleExperiment.Trial>();
        var passes = quick ? 12 : 40; var seeds = quick ? 2 : 12;
        foreach (var condition in new[] { "ordinary", "outlier" })
            for (var seed = 0; seed < seeds; seed++) for (var method = 0; method < Names.Length; method++) Run(condition, seed, method, seed == 0);
        foreach (var condition in new[] { "quiet", "blocked", "unknown", "fixed-wall" }) Run(condition, 0, 4, true);
        var report = new { title = "Learned local responses in coupled constructions", learning = true, features = RequestContext.Names,
            benchmarkNote = $"{seeds} deterministic placements and proportions, with and without a conflicting opinion. {passes} numerical rounds each. All functions are frozen before this transfer evaluation.",
            caps = LocalResponseProgram.Caps, methods = Names, shares = new long[][] { [6, 0, 0], [2, 2, 2], [2, 2, 2], [2, 2, 2], [2, 2, 2] },
            passes, cases, trials, scope = new[] {
                "The context table, regression and neural function were fitted only to independent local scalar problems. These coupled worlds are a transfer evaluation.",
                "Each learned function sees six reads of the requests at its actual public port. It predicts comparative local relief for the same six available bounds; those scores are not probabilities.",
                "All learned functions execute as finite native affine layers and declared clipping intervals. A shared reader executes the table, regression and neural forms.",
                "The three authored response components still share one influence budget. The learned function changes only the receiver-side request bound when both signs arrive.",
                "A fixed twelve-tick passage is the matched baseline. Direct projection is an additional control with a different response-component mix.",
                "Admission, receiver capacity, construction review, the continuing baseline and required paths use the existing machinery. A learned proposal cannot directly move private parameters.",
                "A missing model read retains its uncertainty and uses the original twelve-tick fallback. Unknown evidence permissions stay unresolved.",
                "The synthetic scalar teaching objective is not the full coupled diagnostic. Good patch predictions need not transfer to sustained multi-part negotiation." } };
        var json = JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(Path.Combine(folder, "observations.json"), json); EnsembleReview.Write(Path.Combine(folder, "transfer.html"), json);
        foreach (var condition in new[] { "ordinary", "outlier" })
            Console.WriteLine($"Transfer {condition}: " + string.Join(", ", Enumerable.Range(0, Names.Length).Select(method =>
                $"{Names[method]}={trials.Where(t => t.condition == condition && t.method == method).Average(t => t.final):0.###}")));
        Console.WriteLine($"{cases.Count} visual transfer worlds / {cases.Sum(c => c.frames.Count)} observations / {trials.Count} trajectories");

        void Run(string condition, int seed, int method, bool visual)
        {
            var scene = EnsembleRecipes.Create(method == 0 ? 0 : 4, seed, condition: condition, learner: method < 2 ? null : programs[method - 2]);
            var model = scene.Model; var initial = CoupledSettlement.Number(EnsembleSettlement.Cost(model.LastRead));
            var frames = new List<EnsembleExperiment.Frame>(); var total = 0d; var changed = -1;
            if (visual) frames.Add(EnsembleExperiment.ObserveFrame(scene, 0, 0, "Initial state", model.LastRead));
            for (var pass = 1; pass <= passes; pass++)
            {
                var step = model.Step(); total += CoupledSettlement.Number(EnsembleSettlement.Cost(model.LastRead));
                if (visual)
                {
                    frames.Add(EnsembleExperiment.ObserveFrame(scene, frames.Count, pass, "Local responses", step.AfterMotion, step));
                    var review = EnsembleExperiment.ReadReview(model, step.Review);
                    if (review is not null) frames.Add(EnsembleExperiment.ObserveFrame(scene, frames.Count, pass,
                        step.Rewritten ? "Construction accepted" : "Review declined", step.AfterDecision, step, review));
                }
                if (step.Rewritten) changed = pass;
            }
            var archive = visual ? $"native/{condition}-{method}.gojo.bin.gz" : $"benchmark/{condition}-{seed:D2}-{method}.gojo.bin.gz";
            ResponseLearningExperiment.Save(Path.Combine(folder, archive), visual
                ? G(model.Surface, scene.Initial, Relation.Segment(new Carrier(), 0, passes))
                : G(scene.Initial, model.LastRead, Relation.Segment(new Carrier(), 0, passes)));
            if (visual)
            {
                var restored = EnsembleSettlement.Restore((Relation)F(ResponseLearningExperiment.Load(Path.Combine(folder, archive)))[0]);
                var next = restored.Step();
                // Compare the continuation with the same extra live round,
                // while reporting the forty-round observation separately.
                var expected = model.Step();
                if (!restored.Machines.SelectMany(m => m.Points.SelectMany(Program.XY)).SequenceEqual(model.Machines.SelectMany(m => m.Points.SelectMany(Program.XY))) ||
                    CoupledSettlement.Number(EnsembleSettlement.Cost(next.AfterDecision)) != CoupledSettlement.Number(EnsembleSettlement.Cost(expected.AfterDecision)))
                    throw new InvalidOperationException("A saved learned world changed its continuation.");
                var title = condition switch { "ordinary" => "Cooperating parts", "outlier" => "Conflicting opening opinion",
                    "quiet" => "Permitted eaves", "blocked" => "Fixed receivers", "unknown" => "Unknown permission", _ => "Fixed wall" };
                cases.Add(new($"{condition}-{method}", title + " · " + Names[method],
                    "A frozen local response function participates through the existing public ports. Its training task had one scalar receiver; this scene couples three constructions.",
                    method, condition, seed, scene.Reference, frames, archive, method != 0));
            }
            // The visual continuation above is verification only. Its reported
            // final read and trial budget remain the recorded last observation.
            var final = visual ? frames[^1].cost : CoupledSettlement.Number(EnsembleSettlement.Cost(model.LastRead));
            var unknown = visual ? frames[^1].unknown : EnsembleSettlement.Unknown(model.LastRead);
            if (condition is "ordinary" or "outlier") trials.Add(new(seed, condition, method, initial, final, total / passes, changed, unknown, archive));
            Console.WriteLine($"Transfer {condition}/{seed}/{method}: {initial:0} -> {final:0}; structural change {changed}");
        }
    }
}
