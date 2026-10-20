using System.IO.Compression;
using System.Text;
using System.Text.Json;
using Gojo.Encoding.Structures;

namespace Gojo.Experiments.QualitativeFrieze;

public static partial class OrnamentReconstructionExperiment
{
    public static int Run(string[] args)
    {
        var output = Path.Combine("experiments", "QualitativeFrieze", "results", "ornament-reconstruction");
        if (args.Contains("--dialogue"))
        {
            var at = Array.IndexOf(args, "--output");
            if (at >= 0 && at + 1 < args.Length) output = args[at + 1];
            return RunDialogue(output);
        }
        if (args.Contains("--refresh-report"))
        {
            var at = Array.IndexOf(args, "--output");
            if (at >= 0 && at + 1 < args.Length) output = args[at + 1];
            return Refresh(output);
        }
        var candidates = 0; var targets = 0; var pool = 10; var proposals = 4096; var snapshot = 8;
        for (var index = 0; index < args.Length; index++)
        {
            var argument = args[index];
            if (index + 1 >= args.Length) throw new ArgumentException("Every reconstruction option requires a value.");
            var value = args[++index];
            if (argument == "--output") output = value;
            else if (argument == "--candidates") candidates = int.Parse(value);
            else if (argument == "--targets") targets = int.Parse(value);
            else if (argument == "--pool") pool = int.Parse(value);
            else if (argument == "--proposals") proposals = int.Parse(value);
            else if (argument == "--snapshots") snapshot = int.Parse(value);
            else throw new ArgumentException("Unknown reconstruction option: " + argument);
        }
        if (candidates < 0 || targets < 0 || pool is < 0 or > 12 || proposals < 0 || snapshot < 0)
            throw new ArgumentException("Reconstruction capacities must be nonnegative; pool size is at most twelve.");
        var study = OrnamentReconstructionStudy.Run(new(candidates, targets, pool, proposals, SnapshotCases: snapshot), output);
        var archive = GojoStructureCompactJson.Serialize(GojoStructureCompactJson.FromElement("ornament-reconstruction-v1", study.Snapshot));
        if (!GojoStructureCompactJson.TryToElement(GojoStructureCompactJson.Deserialize(archive), out var restored) ||
            OrnamentReconstructionPacket.Participants(restored.Root).Count != OrnamentReconstructionPacket.Participants(study.Snapshot).Count)
            throw new InvalidOperationException("Reconstruction snapshot must retain native aliases after transport.");
        using (var file = File.Create(Path.Combine(output, "reconstruction.gojo.json.gz")))
        using (var zip = new GZipStream(file, CompressionLevel.SmallestSize))
        using (var writer = new StreamWriter(zip)) writer.Write(archive);
        var groups = study.Trials.GroupBy(row => new { row.Engine, row.Goal, row.Context, row.Strategy })
            .Select(group => new { group.Key.Engine, group.Key.Goal, group.Key.Context, group.Key.Strategy,
                Trials = group.Count(), Robust = group.Count(row => row.Robust), SelectedSuccess = group.Count(row => row.SelectedGoal == true),
                SelectedFailure = group.Count(row => row.SelectedGoal == false), Unresolved = group.Count(row => row.SelectedGoal is null),
                SameIncidence = group.Count(row => row.Incidence == "Yes"), SameRelations = group.Count(row => row.Relations == "Yes"),
                MeanHints = group.Average(row => row.Hints), MeanNativeCost = group.Average(row => row.NativeCost),
                MeanAdmitted = group.Average(row => row.Admitted), MeanPossible = group.Average(row => row.Possible) }).ToArray();
        string[] columns = ["source", "family", "goal", "engine", "context", "strategy", "hints", "nativeCost", "admitted", "possible", "safeAdmitted", "robust",
            "selected", "selectedGoal", "goalUnknown", "outputUnknown", "incidence", "relations", "outsideFocusMismatches", "enumerated", "searchMilliseconds", "queryIndices", "claimAnswers"];
        var rows = study.Trials.Select(row => new object?[] { row.Source, row.Family, row.Goal, row.Engine, row.Context, row.Strategy,
            row.Hints, row.NativeCost, row.Admitted, row.Possible, row.SafeAdmitted, row.Robust, row.Selected, row.SelectedGoal,
            row.GoalUnknown, row.OutputUnknown, row.Incidence, row.Relations, row.OutsideFocusMismatches, row.Enumerated,
            Math.Round(row.SearchMilliseconds, 3), row.QueryIndices, row.ClaimAnswers }).ToArray();
        var model = new { Schema = "ornament-reconstruction-v1", Configuration = study.Configuration,
            Counts = new { Families = study.Candidates.Select(row => row.Family).Distinct().Count(), Candidates = study.Candidates.Count,
                Sources = study.Sources.Count, Queries = study.Vocabulary.Count, Decisions = study.Trials.Count, Subsets = study.Subsets,
                study.NativeReads, study.CachedReads, study.FreshOutputs, SnapshotParticipants = OrnamentReconstructionPacket.Participants(study.Snapshot).Count,
                ArchiveRoundTrip = true },
            Timing = new { study.PreparationMilliseconds, study.EvaluationMilliseconds, study.Cache },
            Queries = study.Vocabulary.Select(entry => new { entry.Id, entry.Label, Channel = entry.Query.Kind,
                NativeParticipants = OrnamentReconstructionPacket.Participants(entry.Query.Surface).Count }).ToArray(),
            Goals = study.Goals.Select(goal => new { goal.Id, goal.Name, goal.QueryIndices }).ToArray(),
            Candidates = study.Candidates, Sources = study.Sources, Summary = groups, TrialColumns = columns, Trials = rows,
            RawTrials = study.RawTrials,
            Limits = new[] {
                "Reconstruction is relative to a supplied executable repertoire and 60 advertised native questions, not arbitrary ornament recognition or a learned model.",
                "A selected output is executed from the actual candidate program, with seams transferred through native public ports and a new observation event. One fresh event per selected program is explicitly reused across non-draining questions and trial decisions.",
                "Robustness requires a nonempty settled admission set and that every still-possible candidate, including candidates with unfinished reads, settle the selected goal. Open candidates are never counted as refuted.",
                "Minimum means exhaustive minimum native participant cost within the selected finite claim pool, receiver repertoire and goal. The sender-side oracle sees held-out judge results; the receiver does not.",
                "Greedy removes possible alternatives per incremental description cost and stops when the remaining known focus profiles agree. It does not see the sender's hidden goal answers.",
                "Native participant cost is a structural reachability count, not bits, bytes or conceptual complexity. Query library, receiver programs, goal focus and proof history are separate shared costs.",
                "Local-form, encounter, joint-occurrence and whole-path questions can agree while the complete incidence graph differs. The separate bounded graph judge reads incidence/headings and optionally ordered crossing/continuation relations.",
                "Whole-path correspondence admits path reversal and cyclic start shifts; encounter questions are unordered. Start markers, over/under, measured aspect ratios, fills, curves and arbitrary recursive discovery are not established.",
                "The native archive retains the exposed candidate world, actual provider programs and a bounded set of full selected-output proofs. The complete trial table is a diagnostic projection reproducible by this executable, not an archive of every rejected search proof."
            } };
        var json = JsonSerializer.Serialize(model, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        using (var bytes = new MemoryStream())
        {
            using (var zip = new GZipStream(bytes, CompressionLevel.SmallestSize, true))
            using (var writer = new StreamWriter(zip)) writer.Write(json);
            var compressed = bytes.ToArray();
            File.WriteAllBytes(Path.Combine(output, "diagnostics.json.gz"), compressed);
            File.WriteAllText(Path.Combine(output, "data.js"), "globalThis.ORNAMENT_RECONSTRUCTION_GZIP=\"" + Convert.ToBase64String(compressed) + "\";");
        }
        File.WriteAllText(Path.Combine(output, "gallery.html"), Html());
        File.WriteAllText(Path.Combine(output, "overview.html"), Overview(study));
        var csv = new StringBuilder("source,family,goal,engine,context,strategy,hints,native_nodes,admitted,possible,robust,selected,goal_survived,same_incidence,same_relations\n");
        foreach (var row in study.Trials) csv.AppendLine(string.Join(',', new object?[] { row.Source, row.Family, row.Goal, row.Engine, row.Context, row.Strategy,
            row.Hints, row.NativeCost, row.Admitted, row.Possible, row.Robust, row.Selected, row.SelectedGoal, row.Incidence, row.Relations }));
        using (var file = File.Create(Path.Combine(output, "trials.csv.gz")))
        using (var zip = new GZipStream(file, CompressionLevel.SmallestSize))
        using (var writer = new StreamWriter(zip)) writer.Write(csv.ToString());
        Console.WriteLine($"Completed {study.Sources.Count} sources, {study.Trials.Count:N0} decisions and {study.Subsets:N0} exhaustive subset evaluations.");
        Console.WriteLine($"Actual native reads: {study.NativeReads:N0}; fresh output events: {study.FreshOutputs}; archived native participants: {model.Counts.SnapshotParticipants:N0}.");
        Console.WriteLine(Path.GetFullPath(output)); return 0;
    }
}
