using System.IO.Compression;
using System.Text.Json;
using Gojo.Core;
using Gojo.Encoding;
using Gojo.Structures;
using static Gojo.Experiments.ConstructionMachines.MachineExpressions;

namespace Gojo.Experiments.ConstructionMachines;

public static class EnsembleExperiment
{
    public sealed record Judgment(string name, int support, double? value, double? lower, double? upper,
        double? demand, double? change, double[]? proposals, int[][] participants);
    public sealed record Candidate(string name, double? after, int? openPaths, bool eligible);
    public sealed record Review(double? baseline, Candidate[] candidates);
    public sealed record Frame(int index, int pass, string phase, string construction, double[][][] geometry,
        double cost, int unknown, double pressure, ulong persistence, ulong attempts, double movement, double withheld,
        Judgment[] judgments, Review? review);
    public sealed record Case(string id, string title, string explanation, int method, string condition, int seed,
        long[] reference, List<Frame> frames, string archive);
    public sealed record Trial(int seed, string condition, int method, double initial, double final,
        double meanDemand, int firstChange, int unresolved, string archive);
    private sealed record Setup(string Id, string Title, string Explanation, int Method = 3, string Condition = "ordinary", long Reviews = 2);

    public static void Write(string outputFolder)
    {
        var folder = Path.GetFullPath(outputFolder); Directory.CreateDirectory(folder);
        Directory.CreateDirectory(Path.Combine(folder, "native")); Directory.CreateDirectory(Path.Combine(folder, "benchmark"));
        var setups = new List<Setup>();
        for (var method = 0; method < EnsembleRecipes.MethodNames.Length; method++) setups.Add(new($"ordinary-{method}", $"Cooperating parts · {EnsembleRecipes.MethodNames[method]}",
            "The wall, roof and opening receive several local opinions. The roof can change construction only after a bounded comparison against continuing the current one.", method));
        setups.Add(new("rigid", "Keep the rigid roof", "The same blend runs with no construction-review budget. Remaining eave/ridge disagreement stays visible.", Reviews: 0));
        setups.Add(new("quiet", "A permitted eave stays put", "Small eaves already fit every permitted region. Zero violation does not require exact wall/roof coincidence.", Condition: "quiet"));
        for (var method = 0; method < EnsembleRecipes.MethodNames.Length; method++) setups.Add(new($"outlier-{method}", $"Conflicting opening opinion · {EnsembleRecipes.MethodNames[method]}",
            "An extra opinion places the opening far outside the wall. Compare containment, source fit and the final compromise; none is hidden from the audit.", method, "outlier"));
        setups.Add(new("pooled", "Five copies share one support", "Five explicitly corresponding copies of the same opinion contribute once. Their native structures remain available.", Condition: "pooled-copies"));
        setups.Add(new("unpooled", "Let the copies amplify their requests", "The same five copies each send a request in this control. The diagnostic still counts their common support once, so it cannot reward the amplification.", Condition: "unpooled-copies"));
        setups.Add(new("blocked", "Fixed participants keep their disagreement", "Every receiver has zero movement capacity. Requests and violations remain; absence of motion is not successful agreement.", Condition: "blocked"));
        setups.Add(new("unknown", "An unknown condition stays unknown", "One participant has an unbound permitted region. The known-demand total is partial and construction review cannot treat this as zero error.", Condition: "unknown"));
        setups.Add(new("unsuitable", "No admissible structural alternative", "The library offers the opposed deformation or removal of the required support. Numerical benefit cannot hide a broken path.", Condition: "unsuitable"));
        setups.Add(new("fixed-wall", "The wall has no movement capacity", "The roof and opening can respond while the wall remains fixed. The wall's source observations still disagree and remain in the audit.", Condition: "fixed-wall"));
        var cases = new List<Case>(); var trials = new List<Trial>(); const int passes = 40;
        foreach (var setup in setups)
        {
            var scene = EnsembleRecipes.Create(setup.Method, reviewBudget: setup.Reviews, condition: setup.Condition);
            var model = scene.Model; var frames = new List<Frame>(); var demandSum = 0d; var changed = -1;
            Add(0, "Initial state", model.LastRead, null, null);
            for (var pass = 1; pass <= passes; pass++)
            {
                var step = model.Step(); demandSum += Number(EnsembleSettlement.Cost(step.AfterMotion));
                Add(pass, "Local responses", step.AfterMotion, step, null);
                var review = ReadReview(model, step.Review);
                if (review is not null)
                {
                    if (step.Rewritten) changed = pass;
                    Add(pass, step.Rewritten ? "Construction accepted" : "Review declined", step.AfterDecision, step, review);
                }
            }
            var archive = $"native/{setup.Id}.gojo.bin.gz";
            Save(Path.Combine(folder, archive), G(model.Surface, scene.Initial, Relation.Segment(new Carrier(), 0, passes)));
            var restored = EnsembleSettlement.Restore((Relation)F(Load(Path.Combine(folder, archive)))[0]);
            var next = restored.Step();
            Console.WriteLine($"{setup.Id}: {frames[0].cost:0.##} -> {frames[^1].cost:0.##}; change {changed}; native continuation {Number(EnsembleSettlement.Cost(next.AfterDecision)):0.##}");
            cases.Add(new(setup.Id, setup.Title, setup.Explanation, setup.Method, setup.Condition, 0, scene.Reference, frames, archive));
            if ((setup.Condition is "ordinary" or "outlier") && setup.Reviews == 2)
                trials.Add(new(0, setup.Condition, setup.Method, frames[0].cost, frames[^1].cost, demandSum / passes, changed, frames[^1].unknown, archive));

            void Add(int pass, string phase, Relation read, EnsembleSettlement.Transition? step, Review? review)
            {
                var assessments = EnsembleSettlement.Assessments(read); var support = new List<IElement>();
                var effects = step is null ? [] : F(step.Effects).Cast<Relation>().ToArray();
                var plans = step is null ? [] : F(step.Plans).Cast<Relation>().ToArray();
                var judgments = model.Active.Select((factor, i) =>
                {
                    var supportIndex = support.FindIndex(s => ReferenceEquals(s, factor.Support)); if (supportIndex < 0) { supportIndex = support.Count; support.Add(factor.Support); }
                    var assessment = assessments[i]; var resolved = EvidenceObligation.Resolved(assessment);
                    var permitted = TimeStepSequence.TryReadSpan(factor.Allowed, out var lo, out var hi);
                    double? change = effects.Length > i && OperationClaim.TryReadIntervalResponse(effects[i], out _, out _, out _, out _, out _, out var delta) ? delta.Ticks : null;
                    var participants = factor.Terms.Select(term =>
                    {
                        var port = F(term)[0];
                        for (var m = 0; m < model.Machines.Length; m++)
                        { var index = Array.FindIndex(model.Machines[m].Ports.ToArray(), p => ReferenceEquals(p, port)); if (index >= 0) return new[] { m, index / 2 }; }
                        throw new InvalidDataException("An obligation escaped its declared public participants.");
                    }).ToArray();
                    double[]? contributions = plans.Length > i && F(plans[i])[2] is Relation planResult && planResult.TryReadOrderedGroup(out var pf) && pf.Length == 3
                        ? F(pf[2]).Where((_, part) => part % 2 == 0).Select(c => Number(F(c)[2])).ToArray() : null;
                    return new Judgment(scene.ObligationNames[i], supportIndex, resolved ? Number(EvidenceObligation.Value(assessment)) : null,
                        permitted ? lo.Ticks : null, permitted ? hi.Ticks : null, resolved ? Number(EvidenceObligation.Demand(assessment)) : null, change, contributions, participants);
                }).ToArray();
                var movement = step is null ? 0 : F(step.Motion).Cast<Relation>().SelectMany(NegotiatingMachine.Outcomes)
                    .Sum(o => Math.Abs(Number(o.Accepted)));
                var attempts = phase == "Local responses" && step?.Review is Relation decision && ReadReview(model, decision) is not null ? model.Reviews.Value - 1 : model.Reviews.Value;
                var withheld = step is null ? 0 : F(F(step.Negotiation)[4]).Sum(entry => Math.Abs(Number(F(F(entry)[0])[3])));
                frames.Add(new(frames.Count, pass, phase, phase == "Local responses" && step?.Rewritten == true ? "Rigid shared-height joint" : model.Selected is Placeholder ? "Rigid shared-height joint" : "Independent eaves with required support",
                    model.Machines.Select(m => m.Points.Select(Program.XY).ToArray()).ToArray(), Number(EnsembleSettlement.Cost(read)), EnsembleSettlement.Unknown(read),
                    model.LastPressure is Relation r ? Number(F(r)[2]) : 0, model.Persistence.Value, attempts, movement, withheld, judgments, review));
            }
        }
        foreach (var condition in new[] { "ordinary", "outlier" })
            for (var seed = 1; seed < 12; seed++)
                for (var method = 0; method < EnsembleRecipes.MethodNames.Length; method++)
                {
                    var scene = EnsembleRecipes.Create(method, seed, condition: condition); var model = scene.Model;
                    var initial = Number(EnsembleSettlement.Cost(model.LastRead)); var total = 0d; var changed = -1;
                    for (var pass = 1; pass <= passes; pass++)
                    {
                        var step = model.Step(); total += Number(EnsembleSettlement.Cost(model.LastRead)); if (step.Rewritten) changed = pass;
                    }
                    var archive = $"benchmark/{condition}-{seed:D2}-{method}.gojo.bin.gz";
                    // Initial executable machine + final witnessed read + round
                    // budget: deliberately not a complete benchmark history.
                    Save(Path.Combine(folder, archive), G(scene.Initial, model.LastRead, Relation.Segment(new Carrier(), 0, passes)));
                    trials.Add(new(seed, condition, method, initial, Number(EnsembleSettlement.Cost(model.LastRead)), total / passes, changed,
                        EnsembleSettlement.Unknown(model.LastRead), archive));
                    Console.WriteLine($"grid {condition}/{seed}/{method}: {trials[^1].final:0.##}");
                }
        var report = new { methods = EnsembleRecipes.MethodNames, shares = EnsembleRecipes.Shares, passes, cases, trials,
            scope = new[] { "All rules, response shares, observation intervals and construction alternatives are authored. No neural model is trained or called.",
                "The three response rules share one influence budget. Their outputs are proposals about the same evidence, not independent evidence streams.",
                "The negotiated blend adds a receiver-side passage: large requests are capped at 12 when both signs arrive at the same public port. This is an authored feedback law, not a learned confidence score.",
                "The common audit sums declared support correspondence once. It is a selected diagnostic, not probability, physical energy or pending carrier tension.",
                "Each structural candidate is compared with eight more local rounds of the current construction under the same frozen public neighbours. Required paths are checked separately.",
                "Saved visual worlds can be restored and continued. Benchmark files retain initial executable worlds and final witnessed scores, not every intermediate state.",
                "This finite synthetic grid tests mechanisms and failure cases. It does not establish general visual recognition or equivalence to a neural network." } };
        var json = JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(Path.Combine(folder, "observations.json"), json); EnsembleReview.Write(Path.Combine(folder, "review.html"), json);
        foreach (var condition in new[] { "ordinary", "outlier" })
            Console.WriteLine($"{condition} mean final: " + string.Join(", ", Enumerable.Range(0, EnsembleRecipes.MethodNames.Length).Select(method => $"{EnsembleRecipes.MethodNames[method]}={trials.Where(t => t.condition == condition && t.method == method).Average(t => t.final):0.##}")));
        Console.WriteLine($"{cases.Count} visual worlds / {cases.Sum(c => c.frames.Count)} observations; {trials.Count} controlled trajectories; {folder}");
    }
    private static Review? ReadReview(EnsembleSettlement model, IElement review)
    {
        if (review is not Relation read || F(read)[2] is not Relation result || !result.TryReadOrderedGroup(out var fields) || fields.Length != 3) return null;
        var candidates = F(fields[2]).Cast<Relation>().Select(r =>
        {
            var index = Array.FindIndex(F(model.Proposals), p => ReferenceEquals(p, F(r)[1]));
            var name = F(model.Proposals).Length == 2 ? new[] { "Opposed deformation", "Remove support" }[index]
                : new[] { "Release eaves", "Opposed deformation", "Remove support" }[index];
            return new Candidate(name, EnsembleSettlement.ProbeResolved(r) ? Number(F(F(r)[2])[1]) : null,
                EnsembleSettlement.ProbeResolved(r) ? (int)Number(F(F(r)[2])[2]) : null, EnsembleSettlement.Eligible(r));
        }).ToArray();
        return new(EnsembleSettlement.ProbeResolved((Relation)fields[1]) ? Number(F(F((Relation)fields[1])[2])[1]) : null, candidates);
    }
    private static void Save(string path, IElement root)
    {
        var binary = GojoNumericFormat.Encode(root);
        using var file = File.Create(path); using var stream = new GZipStream(file, CompressionLevel.Fastest); stream.Write(binary);
    }
    private static IElement Load(string path)
    { using var file = File.OpenRead(path); using var stream = new GZipStream(file, CompressionMode.Decompress); using var buffer = new MemoryStream(); stream.CopyTo(buffer); return GojoNumericFormat.Decode(buffer.ToArray())[0]; }
    private static double Number(IElement value) => CoupledSettlement.Number(value);
}
