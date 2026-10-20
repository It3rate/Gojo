using System.Diagnostics;
using System.IO.Compression;
using System.Text.Json;
using Gojo.Core;
using Gojo.Encoding.Structures;
using Gojo.Patterns;

namespace Gojo.Experiments.QualitativeFrieze;

/// <summary>Local CLI/report adapter for a small learned acquisition policy.
/// Native training, hypothesis filtering and independent source assessment run
/// before this adapter realizes or draws selected samples.</summary>
public static partial class EvidenceSelectionExperiment
{
    public static int Run(string[] args)
    {
        var output = Path.Combine("experiments", "QualitativeFrieze", "results", "evidence-selection");
        var options = new EvidenceSelectionStudy.Options();
        for (var i = 0; i < args.Length; i++)
        {
            if (args[i] == "--output" && i + 1 < args.Length) output = args[++i];
            else if (args[i] == "--training" && i + 1 < args.Length) options = options with { TrainingEpisodes = int.Parse(args[++i]) };
            else if (args[i] == "--evaluation" && i + 1 < args.Length) options = options with { EvaluationEpisodes = int.Parse(args[++i]) };
            else if (args[i] == "--seed" && i + 1 < args.Length) options = options with { Seed = int.Parse(args[++i]) };
            else if (args[i] == "--requests" && i + 1 < args.Length) options = options with { MaxRequests = int.Parse(args[++i]) };
            else throw new ArgumentException("Usage: --evidence-selection [--output DIRECTORY] [--training N] [--evaluation N] [--seed N] [--requests N]");
        }
        Directory.CreateDirectory(output);
        using var process = Process.GetCurrentProcess();
        var cpu = process.TotalProcessorTime; var allocated = GC.GetTotalAllocatedBytes(true);
        var watch = Stopwatch.StartNew();
        var study = EvidenceSelectionStudy.Build(options);
        watch.Stop(); process.Refresh();
        var costs = new { ElapsedMilliseconds = watch.Elapsed.TotalMilliseconds,
            CpuMilliseconds = (process.TotalProcessorTime - cpu).TotalMilliseconds,
            AllocatedBytes = GC.GetTotalAllocatedBytes(true) - allocated,
            WorkingSetAfterBuildBytes = process.WorkingSet64,
            ProcessPeakWorkingSetAfterBuildBytes = process.PeakWorkingSet64,
            Runtime = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription,
            LogicalProcessors = Environment.ProcessorCount,
#if DEBUG
            Configuration = "Debug"
#else
            Configuration = "Release"
#endif
        };
        var auditSurfaces = new List<IElement>(); var auditOutputs = 0; var incompleteGenericReaders = 0;
        var episodes = study.Episodes.Select(episode =>
        {
            string Role(IElement role)
            {
                var index = Array.FindIndex(episode.Roles, actual => ReferenceEquals(actual, role));
                return index < 0 ? "foreign role" : $"cell {index / 2 + 1} {(index % 2 == 0 ? "horizontal" : "vertical")}";
            }
            string? Draw(ProvisionalEquivalence.Hypothesis? hypothesis, string suffix)
            {
                if (hypothesis is null) return null;
                var form = episode.Forms.Single(form => ReferenceEquals(form.Hypothesis, hypothesis));
                var realized = new EquivalenceRevisionStudy.Realization(new(form.Name, form.Name,
                    form.Program, form.Hypothesis, form.RoleSelectors));
                var comparisons = E(hypothesis.Bindings).OfType<Relation>().Select(binding =>
                {
                    var expected = (Relation)E(hypothesis.ParameterValues).OfType<Relation>()
                        .Single(row => ReferenceEquals(row.Left, binding.Right)).Right;
                    var actual = (Relation)E(realized.SpanValues).OfType<Relation>()
                        .Single(row => ReferenceEquals(row.Left, binding.Left)).Right;
                    return new ProvisionalEquivalence(episode.Domain, S(episode.Reach)).Compare(expected, actual);
                }).ToArray();
                var topology = realized.Graph.Describe(realized.CoarseRead);
                if (topology.Endpoints != 0 || topology.Unresolved != 0 || realized.CycleCount != episode.Roles.Length / 2 ||
                    comparisons.Any(comparison => !comparison.IsEqual))
                    throw new InvalidOperationException($"Fresh execution audit {episode.Id}/{suffix}: coarse closed {realized.CoarseClosed}, cycles {realized.CycleCount}/{episode.Roles.Length / 2}, unequal formal spans {comparisons.Count(read => !read.IsEqual)}.");
                auditSurfaces.Add(new Relation(hypothesis.Surface,
                    new Relation(realized.Surface, S(comparisons.Select(read => (IElement)read.Surface).ToArray()))));
                auditOutputs++;
                if (!realized.Critic.IsComplete) incompleteGenericReaders++;
                var file = episode.Id + "-" + suffix + ".svg";
                File.WriteAllText(Path.Combine(output, file), EquivalenceRevisionExperiment.Svg(
                    realized.Frame, realized.Graph, realized.Bindings, episode.Id + " / " + suffix));
                return file;
            }
            var beforeImage = Draw(episode.Initial.Chosen, "before");
            // An explicit final sample requires one compatible candidate. A
            // retained initial completion can still illustrate an open case.
            var afterImage = Draw(episode.Final.Eligible.Count == 1 ? episode.Final.Eligible[0].Hypothesis : null, "after");
            var metric = episode.Metric;
            var status = metric.Identified ? "One compatible candidate; independent full-source evaluation confirms it."
                : metric.ConflictCount > 0 ? "Conflicting replies remain; no exact interpretation is certified."
                : metric.BlockedCount > 0 && metric.Compatible == 0 ? "Required executable capability is blocked."
                : metric.UniqueCompatible ? "One candidate fits acquired evidence, but full-source evaluation does not certify it."
                : "The evidence budget ended with multiple interpretations still open.";
            return new { episode.Id, Title = episode.Id.StartsWith("held-out") ? "Held-out " + episode.Family : "Control · " + episode.Metric.Split,
                episode.Family, episode.Policy, Cells = episode.Roles.Length / 2,
                InitialCompatible = episode.Initial.Eligible.Count, FinalCompatible = episode.Final.Eligible.Count,
                metric.Requests, metric.Identified, Status = status, BeforeImage = beforeImage, AfterImage = afterImage,
                Evaluation = $"Post-run source evaluator: {episode.Evaluation.Eligible.Count} compatible candidates. This evaluator is separate from policy inputs and acquired evidence.",
                Steps = episode.Steps.Select(step => new { Roles = E(step.Port.Roles).Select(Role).ToArray(),
                    Before = step.Before.Eligible.Count, After = step.After.Eligible.Count,
                    Features = step.FeatureValues, step.Prediction,
                    Conflicts = step.After.Candidates.Select(read => E(read.InScopeConflicts).Length).DefaultIfEmpty().Max(),
                    Words = step.Publications.SelectMany(pub => E(pub.Values)).OfType<Relation>()
                        .Select(row => Role(row.Left) + ": " + Word((Relation)row.Right)).ToArray() }).ToArray() };
        }).ToArray();

        var root = new Relation(study.Surface, S(auditSurfaces.ToArray()));
        var native = Reachable(root).ToArray();
        if (native.Any(item => item is Carrier or CarrierLocation))
            throw new InvalidOperationException("The qualitative world must not acquire measured numeric carriers from the learner backend.");
        var archive = GojoStructureCompactJson.Serialize(GojoStructureCompactJson.FromElement("evidence-selection-v1", root));
        if (!GojoStructureCompactJson.TryToElement(GojoStructureCompactJson.Deserialize(archive), out var restored) ||
            Reachable(restored.Root).Count() != native.Length)
            throw new InvalidOperationException("Retained training and output witnesses must round trip.");
        var restoredStudy = (Relation)((Relation)restored.Root).Left;
        var restoredPolicy = EvidenceSelectionStudy.LearnedPolicy.FromSurface((Relation)restoredStudy.Left);
        if (!restoredPolicy.Coefficients.Zip(study.Policy.Coefficients).All(pair => Math.Abs(pair.First - pair.Second) < 1e-10))
            throw new InvalidOperationException("Restored native outcomes must reconstruct the learned coefficients.");
        using (var file = File.Create(Path.Combine(output, "evidence.gojo.json.gz")))
        using (var zip = new GZipStream(file, CompressionLevel.SmallestSize))
        using (var writer = new StreamWriter(zip)) writer.Write(archive);

        var model = new { Schema = "evidence-selection-v1", Options = options,
            Training = new { Rows = study.Policy.TrainingRows, Coefficients = study.Policy.Coefficients,
                Features = new[] { "Intercept", "Candidate disagreement", "Requested-query role overlap", "Offered role count / 2", "Already published role fraction" } },
            study.Summaries, study.Metrics, Episodes = episodes, Costs = costs,
            NativeElements = native.Length, NativeNumericElements = 0, AuditOutputs = auditOutputs,
            IncompleteGenericReaders = incompleteGenericReaders, ArchiveRoundTrip = true,
            PolicyReconstructedFromArchive = true,
            Limits = new[] {
                "The learned component selects the next published span read; it does not learn native compatibility laws, invent motif programs or recognize an arbitrary ornament.",
                "The train/test split holds out contiguous-block and reflected-pair layouts and larger cell counts, within the same finite four-edge-cycle vocabulary. It is narrow structural transfer, not unrestricted concept generalization.",
                "The fixed-order baseline advertises uninformative vertical services first. Its failure is deliberately easy; the disagreement baseline is the meaningful stronger comparison.",
                "All learned and baseline policies receive the same candidate programs, role interfaces and query context. Concealed source publications enter only through acquisition; post-run full-source evaluation is excluded from policy inputs.",
                "Unique compatibility under acquired evidence is conditional. Correct identification additionally requires agreement with independent full-source evaluation; a false singleton is not counted as correct.",
                "Training utility rewards finite-library reduction and penalizes zero-compatible outcomes. It is a deliberately chosen local objective, not a general confidence, aesthetic or truth law.",
                "Open supplier replies, simultaneous conflicting reports and absent executable capability are separate retained controls. The ranking policy cannot erase those states.",
                "One pair service returns two roles and costs one request, just as advertised. These results measure service calls, not a claim of equal bandwidth or universal optimality.",
                "Policy coefficients are numerical derived reads. The underlying qualitative hypotheses and evidence contain zero measured numeric carriers; coefficients can be recomputed from the transported native training outcomes.",
                "Illustrative right angles and screen lengths do not establish Euclidean square geometry or physical cross-axis calibration. Actual output word participation and coarse closure are checked before drawing.",
                "The earlier small generic motif reader retains its original proposal budget. It can remain incomplete on these larger cell arrays; its unsearched residue is preserved. Output closure checks use a separate actual-incidence read, not an assumed successful motif recognition.",
                "Timing includes one process-first-use training and evaluation pass, with all training outcome witnesses and selected evaluation examples retained. It excludes SDK build, output realization, HTML/SVG and native archive capture; it is not a steady-state GPU benchmark."
            } };
        var json = JsonSerializer.Serialize(model, new JsonSerializerOptions(JsonSerializerDefaults.Web) { WriteIndented = true });
        File.WriteAllText(Path.Combine(output, "diagnostics.json"), json);
        File.WriteAllText(Path.Combine(output, "gallery.html"), Html(json));
        foreach (var summary in study.Summaries)
            Console.WriteLine($"{summary.Policy}: {summary.Identified}/{summary.Episodes} independently confirmed; mean {summary.MeanRequests:0.###} requests.");
        Console.WriteLine($"Training + evaluation {watch.Elapsed.TotalSeconds:0.###} s; {study.Policy.TrainingRows} acquired training outcomes; {native.Length:N0} retained native participants; {auditOutputs} fresh output audits.");
        Console.WriteLine(Path.GetFullPath(output)); return 0;
    }

    private static string Word(Relation word)
    {
        StructuralConstraint.TryReadScope(word, out var entries, out var seam, out var tail);
        return string.Join(" · ", entries.Select(_ => "u")) + (ReferenceEquals(seam, tail) ? entries.Length == 0 ? "empty" : "" : " … open");
    }
    private static Relation S(params IElement[] values) => StructuralConstraint.Closed(values);
    private static IElement[] E(Relation scope) => Gojo.Experiments.QualitativeExecution.FiniteLaw.Entries(scope);
    private static IEnumerable<IElement> Reachable(IElement root)
    {
        var seen = new HashSet<IElement>(ReferenceEqualityComparer.Instance); var pending = new Stack<IElement>(); pending.Push(root);
        while (pending.TryPop(out var item)) if (seen.Add(item))
        { yield return item; if (item is Relation relation) { pending.Push(relation.Left); pending.Push(relation.Right); } }
    }
}
