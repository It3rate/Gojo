using System.Text.Json;
using Gojo.Core;
using static Gojo.Experiments.ConstructionMachines.MachineExpressions;

namespace Gojo.Experiments.ConstructionMachines;

/// <summary>A post-hoc two-case probe of a known training-count mismatch.
/// It changes the declared query domain, preserving all fitted coefficients.</summary>
public static class ResponseCoverageExperiment
{
    public static LocalResponseProgram RestrictCount(LocalResponseProgram program, int minimum, int maximum, IElement source)
    {
        var fields = F(program.Surface); var cells = program.Cells.Select(cell =>
        {
            var c = F(cell); var domains = F(c[0]); domains[5] = Relation.Segment(program.Inputs[5],
                minimum * LocalResponseProgram.InputScale / 8, maximum * LocalResponseProgram.InputScale / 8);
            return (IElement)G(G(domains), c[1], c[2], G(c[3], source));
        }).ToArray();
        return new(G(fields[0], fields[1], G(cells), fields[3], G(program.Surface, source)));
    }

    public static void Write(string folder)
    {
        using var input = JsonDocument.Parse(File.ReadAllText(Path.Combine(folder, "observations.json")));
        var originals = JsonSerializer.Deserialize<EnsembleExperiment.Case[]>(input.RootElement.GetProperty("cases").GetRawText())!;
        var passes = input.RootElement.GetProperty("passes").GetInt32();
        var allCounts = new List<int>();
        foreach (var path in ResponseLearningVerification.DataFiles(folder, "training"))
            foreach (var patch in F(F(ResponseLearningExperiment.Load(path))[1])) allCounts.Add(F(F(patch)[1]).Length);
        var minimum = allCounts.Min(); var maximum = allCounts.Max(); var countAxis = new Carrier();
        var source = G(new CarrierLocation(countAxis, allCounts.Count), Relation.Segment(countAxis, minimum, maximum),
            G(allCounts.Select(n => (IElement)new CarrierLocation(countAxis, n)).ToArray()));
        var neural = new LocalResponseProgram((Relation)ResponseLearningExperiment.Load(Path.Combine(folder, "models", "neural.gojo.bin.gz")));
        var guarded = RestrictCount(neural, minimum, maximum, source);
        ResponseLearningExperiment.Save(Path.Combine(folder, "models", "neural-covered.gojo.bin.gz"), guarded.Surface);
        var cases = new List<EnsembleExperiment.Case>(); var trials = new List<EnsembleExperiment.Trial>();
        var summaries = new List<object>();
        foreach (var condition in new[] { "ordinary", "outlier" })
        {
            var fixedCase = originals.Single(c => c.condition == condition && c.method == 1);
            var rawCase = originals.Single(c => c.condition == condition && c.method == 4);
            cases.Add(fixedCase with { id = condition + "-0", method = 0 }); cases.Add(rawCase with { id = condition + "-1", method = 1 });
            var scene = EnsembleRecipes.Create(method: 4, condition: condition, learner: guarded); var model = scene.Model;
            var frames = new List<EnsembleExperiment.Frame> { EnsembleExperiment.ObserveFrame(scene, 0, 0, "Initial state", model.LastRead) };
            for (var pass = 1; pass <= passes; pass++)
            {
                var step = model.Step(); frames.Add(EnsembleExperiment.ObserveFrame(scene, frames.Count, pass, "Local responses", step.AfterMotion, step));
                var review = EnsembleExperiment.ReadReview(model, step.Review);
                if (review is not null) frames.Add(EnsembleExperiment.ObserveFrame(scene, frames.Count, pass,
                    step.Rewritten ? "Construction accepted" : "Review declined", step.AfterDecision, step, review));
            }
            var archive = $"native/{condition}-covered-neural.gojo.bin.gz";
            ResponseLearningExperiment.Save(Path.Combine(folder, archive), G(model.Surface, scene.Initial, Relation.Segment(new Carrier(), 0, passes)));
            var restored = EnsembleSettlement.Restore((Relation)F(ResponseLearningExperiment.Load(Path.Combine(folder, archive)))[0]);
            model.Step(); restored.Step();
            if (!model.Machines.SelectMany(m => m.Points.SelectMany(Program.XY)).SequenceEqual(restored.Machines.SelectMany(m => m.Points.SelectMany(Program.XY))))
                throw new InvalidOperationException("The count-qualified world's continuation changed after restoration.");
            cases.Add(new(condition + "-2", (condition == "ordinary" ? "Cooperating parts" : "Conflicting opinion") + " · Neural with count qualification",
                "Post-hoc probe: the fitted function is queried only for request counts observed in training. Otherwise the original twelve-tick response remains, with the unavailable model read retained.",
                2, condition, 0, scene.Reference, frames, archive, true));
            foreach (var c in cases.Where(c => c.condition == condition)) trials.Add(new(0, condition, c.method, c.frames[0].cost, c.frames[^1].cost,
                c.frames.Where(f => f.phase == "Local responses").Average(f => f.cost), c.frames.FirstOrDefault(f => f.phase == "Construction accepted")?.pass ?? -1,
                c.frames[^1].unknown, c.archive));
            var contexts = rawCase.frames.Where(f => f.phase == "Local responses").SelectMany(f => f.decisions ?? []).ToArray();
            var outside = contexts.Count(d => d.requests.Length < minimum || d.requests.Length > maximum);
            summaries.Add(new { condition, fixedFinal = fixedCase.frames[^1].cost, rawFinal = rawCase.frames[^1].cost, guardedFinal = frames[^1].cost,
                contexts = contexts.Length, outsideCount = outside, outsideAmplitude = contexts.Count(d => d.requests.Any(x => Math.Abs(x) > 128)),
                guardedFallbacks = frames.Where(f => f.phase == "Local responses").SelectMany(f => f.decisions ?? []).Count(d => d.cap is null) });
            Console.WriteLine($"Count coverage {condition}: raw={rawCase.frames[^1].cost}, qualified={frames[^1].cost}, fixed={fixedCase.frames[^1].cost}; {outside}/{contexts.Length} raw contexts outside observed counts");
        }
        var report = new { title = "Post-hoc request-count coverage probe", learning = true, defaultCase = 2, features = RequestContext.Names,
            benchmarkNote = "Two previously inspected input worlds, with three response policies. This post-hoc probe qualifies the model's query domain; it is not a fresh held-out test.",
            caps = LocalResponseProgram.Caps, minimum, maximum, summaries, cases, trials, passes,
            methods = new[] { "Fixed local negotiation", "Small neural model", "Neural with count qualification" },
            shares = new long[][] { [2, 2, 2], [2, 2, 2], [2, 2, 2] },
            scope = new[] { "This is a post-hoc diagnostic on two previously inspected input worlds. It is not a new held-out performance claim.",
                "The minimum and maximum request counts come only from the saved training problems. Fitted neural coefficients are unchanged.",
                "The count-qualified program returns an unresolved read outside that selected domain. The existing twelve-tick proposal is used as a separate fallback.",
                "The read's unknown state remains in native provenance. The evidence obligations themselves are not relabelled unknown or erased.",
                "Count qualification does not detect every form of distribution shift. Request amplitudes, shared actuators and the teaching objective can still differ." } };
        var json = JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(Path.Combine(folder, "coverage.json"), json); EnsembleReview.Write(Path.Combine(folder, "coverage.html"), json);
    }
}
