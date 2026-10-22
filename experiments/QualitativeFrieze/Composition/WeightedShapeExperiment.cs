using System.Diagnostics;
using System.IO.Compression;
using System.Text.Json;
using Gojo.Core;
using Gojo.Patterns;
using Gojo.Encoding.Structures;
using static Gojo.Experiments.QualitativeFrieze.Composition.JointNative;
using Weighting = Gojo.Experiments.QualitativeFrieze.Composition.ShapeEvidenceWeighting;
using Stats = Gojo.Experiments.QualitativeExecution.StatisticalEvidence;

namespace Gojo.Experiments.QualitativeFrieze.Composition;

/// <summary>Same-pool controls for authored statistical evidence estimates.
/// Teacher connectivity is audited only after every selection has completed.</summary>
public static class WeightedShapeExperiment
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { WriteIndented = true };
    public sealed record Audit(int Matched, int Expected, int Additional)
    { public bool Exact => Matched == Expected && Additional == 0; }

    public static int Run(string[] args)
    {
        var output = Path.Combine("experiments", "QualitativeFrieze", "results", "weighted-shapes");
        var budget = 64; string? selection = null;
        for (var i = 0; i < args.Length; i++)
            if (args[i] == "--output" && i + 1 < args.Length) output = args[++i];
            else if (args[i] == "--budget" && i + 1 < args.Length) budget = int.Parse(args[++i]);
            else if (args[i] == "--source" && i + 1 < args.Length) selection = args[++i];
            else throw new ArgumentException("Usage: --weighted-shapes [--budget N] [--source R001,...] [--output DIRECTORY]");
        if (budget < 3) throw new ArgumentOutOfRangeException(nameof(budget));
        Directory.CreateDirectory(output);
        var sources = RelationalShapeCorpus.Cases().Where(item => selection is null || selection.Split(',').Contains(item.Id)).ToArray();
        if (sources.Length == 0) throw new ArgumentException("No sources matched.");
        var watch = Stopwatch.StartNew(); var examples = new List<WeightedShapeGallery.Example>(); var records = new List<CaseResult>();
        var evaluated = 0; var blocked = 0; var distinct = 0; var nativeChecks = 0; long nativeBytes = 0;
        foreach (var source in sources)
        {
            var analysis = JointShapeDescription.Analyze(source.Source);
            var packet = RelationalShapeExperiment.Publish(analysis); var m = packet.Message;
            var closure = StructuralEvidenceStreams.SelectClosure(m, packet.Streams);
            // Sparse comes first on the overview: full evidence was already
            // close to saturated in the preceding reconstruction experiment.
            RelationalShapeSearch.Focus[] focuses = [
                new("sparse", "Sparse relative locations", RelationalSpatialEvidence.Select(packet.Spatial, m, "sparse"), packet.Equivalence, closure),
                new("full", "Full relative locations", RelationalSpatialEvidence.Select(packet.Spatial, m, "observed"), packet.Equivalence, closure)
            ];
            var views = new List<WeightedShapeGallery.Result>(); var archive = new List<IElement>();
            foreach (var focus in focuses)
            {
                var trials = new List<RelationalShapeSearch.Trial>();
                var search = RelationalShapeSearch.Run(m, focus, budget, observeTrial: trials.Add);
                var pool = Weighting.Freeze(m, search, trials);
                evaluated += search.Evaluated; blocked += search.Blocked; distinct += pool.Alternatives.Count;
                // All readings finish before any teacher correspondence is
                // consulted. None can feed the adaptive proposal policy.
                var readings = Weighting.Plans.Select(plan => Weighting.Read(pool, plan)).ToArray();
                var audits = pool.Alternatives.ToDictionary(item => item.Id, item => Reference(analysis, m, item.Trial.Candidate));
                var baselineAudit = audits[pool.Baseline.Id];
                var sourceInPool = audits.Values.Any(read => read.Exact);
                foreach (var reading in readings)
                {
                    var audit = audits[reading.Selected.Id];
                    var rank = pool.Alternatives.OrderByDescending(item => Weighting.Weight(reading.Posterior, item), Weighting.RatioComparer.Instance)
                        .ThenBy(item => item.Trial.Rank).ThenBy(item => item.Trial.Index).ToArray();
                    var display = new[] { reading.Baseline, reading.Selected }.Concat(rank).DistinctBy(item => item.Id).Take(6).ToArray();
                    var evidence = reading.Steps.Select(step => {
                        var group = reading.Assessments[reading.Selected.Id].Single(item => item.Family == step.Family);
                        return new WeightedShapeGallery.Evidence(step.Family.Name,
                            $"{group.Supported} supported · {group.Contradicted} contradicted · {group.Unresolved} unresolved",
                            step.Family.Id, step.Likelihoods[reading.Selected.Id].ToString(),
                            $"Baseline channel likelihood {step.Likelihoods[reading.Baseline.Id]}. One summary report, not {group.Supported + group.Contradicted + group.Unresolved} independent samples.");
                    }).ToArray();
                    var changed = reading.Selected.Id != reading.Baseline.Id;
                    var record = new CaseResult(source.Id, focus.Id, reading.Plan.Id, pool.Alternatives.Count,
                        search.Evaluated, search.Blocked, sourceInPool, baselineAudit.Exact, audit.Exact, changed,
                        reading.Tied.Count, reading.Steps.Count, reading.Selected.Trial.HardConflicts,
                        reading.Baseline.Trial.HardConflicts, reading.Selected.Trial.UnpairedRays, reading.Baseline.Trial.UnpairedRays,
                        reading.Selected.Id, reading.Baseline.Id, Weighting.Weight(reading.Posterior, reading.Selected).ToString(),
                        sourceInPool ? rank.TakeWhile(item => !audits[item.Id].Exact).Count() + 1 : null);
                    records.Add(record);
                    views.Add(new(focus.Id + "-" + reading.Plan.Id, focus.Name + " · " + reading.Plan.Name,
                        reading.Plan.Premise, reading.Baseline.Id, reading.Selected.Id,
                        display.Select(item => new WeightedShapeGallery.Candidate(item.Id, $"Proposal {item.Trial.Index} · {item.Routes.Count} route(s)",
                            item.Trial.Candidate.Source, item.Trial.Candidate.AssumedStrokes,
                            Weighting.Weight(reading.Prior, item).ToString(), Weighting.Weight(reading.Posterior, item).ToString(),
                            new { SourceConnectivity = audits[item.Id], item.Trial.HardConflicts, item.Trial.UnpairedRays,
                                item.Trial.DefaultConflicts, item.Trial.Assumptions, item.Trial.Open, item.Trial.Supported,
                                CompactPriorCost = Weighting.CompactCost(item.Trial), ProposedRoutes = item.Routes.Count })).ToArray(),
                        evidence, new { DistinctNormalizedSamples = pool.Alternatives.Count, OriginalProposals = trials.Count,
                            SourceConnectivityPresentInPool = sourceInPool, Baseline = baselineAudit, Weighted = audit,
                            SelectionChanged = changed, MaximumWeightTies = reading.Tied.Count, TiedIds = reading.Tied.Select(item => item.Id).ToArray(),
                            TieBreak = "Original ordinal rank, then proposal index. Equal weights do not establish identity.",
                            record.FirstFaithfulRank, EstimatedFamilyReports = reading.Steps.Count,
                            DisplayedCandidates = display.Length, Evaluated = search.Evaluated, Blocked = search.Blocked,
                            Limits = "Conditional receiver-choice shares within this frozen pool. Supplied channel estimates and approximate independence; no calibrated probability of teacher intent." }));
                    archive.Add(reading.Surface);
                }
                // The same native states must give the same fractions after
                // an independently restored compact transport, without teacher.
                var root = S(readings.Select(item => (IElement)item.State).ToArray());
                var wire = GojoStructureCompactJson.Serialize(GojoStructureCompactJson.FromElement("weighted-shape-check", root));
                if (!GojoStructureCompactJson.TryToElement(GojoStructureCompactJson.Deserialize(wire), out var restored) || restored.Root is not Relation imported)
                    throw new InvalidOperationException("Native statistical states failed standalone transport.");
                var importedStates = E(imported).Cast<Relation>().ToArray();
                for (var i = 0; i < readings.Length; i++)
                {
                    var replay = Stats.Read(importedStates[i]);
                    if (!replay.Complete || !replay.Rows.Select(row => row.Probability).SequenceEqual(readings[i].Posterior.Rows.Select(row => row.Probability)))
                        throw new InvalidOperationException("Restored native evidence changed a posterior.");
                    nativeChecks++;
                }
                var neutral = readings.Single(item => item.Plan.Id == "uniform-flat");
                if (neutral.Selected != pool.Baseline || !neutral.Prior.Rows.Select(row => row.Probability).SequenceEqual(neutral.Posterior.Rows.Select(row => row.Probability)))
                    throw new InvalidOperationException("Neutral likelihood control changed preference or weights.");
            }
            examples.Add(new(source.Id, source.Name, source.Note, source.Source,
                new { JointRoles = m.JointReads.Count, SourceBasis = "Anonymous joints and ray slots, relative projection/order, unmeasured equivalence, symmetry and interior-turn contexts.",
                    SourcePrivacy = "Teacher shape, family, private source words and edge correspondence withheld from proposal weighting.",
                    Defaults = "Existing closure policy controls the common proposal pool. Closure, crossing and overlap preferences are not reimported as observed facts.",
                    Model = "One estimated mixture report per family. Unknown predicts 1/2. This is not the joint likelihood of every low-level check." }, views));
            nativeBytes += Save(Path.Combine(output, source.Id.ToLowerInvariant() + ".gojo.json.gz"), source.Id, S(archive.ToArray()));
            JointReconstructionGallery.WriteText(Path.Combine(output, "progress.json"), JsonSerializer.Serialize(new {
                Completed = examples.Count, Total = sources.Length, Evaluated = evaluated, Blocked = blocked, Seconds = watch.Elapsed.TotalSeconds, Last = source.Id }, Json));
            Console.WriteLine($"{source.Id}: {evaluated} assessed proposals, {distinct} distinct pool samples; {watch.Elapsed.TotalSeconds:0.0}s.");
        }
        var aggregates = records.GroupBy(row => (row.Focus, row.Plan)).Select(group => new {
            Profile = group.Key.Focus, Plan = group.Key.Plan, Cases = group.Count(),
            BaselineExactConnectivity = group.Count(row => row.BaselineExact), WeightedExactConnectivity = group.Count(row => row.SelectedExact),
            ExactConnectivityAvailableInPool = group.Count(row => row.SourceInPool),
            SelectionChanges = group.Count(row => row.Changed), ImprovedConnectivity = group.Count(row => !row.BaselineExact && row.SelectedExact),
            LostExactConnectivity = group.Count(row => row.BaselineExact && !row.SelectedExact),
            LowerHardConflictCount = group.Count(row => row.SelectedHardConflicts < row.BaselineHardConflicts),
            HigherHardConflictCount = group.Count(row => row.SelectedHardConflicts > row.BaselineHardConflicts),
            MoreUnpairedRays = group.Count(row => row.SelectedUnpaired > row.BaselineUnpaired),
            TiedPreferences = group.Count(row => row.Ties > 1),
            MeanDistinctSamples = group.Average(row => row.Pool), MeanTopWeight = group.Average(row => Parse(row.TopWeight)),
            MeanFirstFaithfulRankWherePresent = group.Where(row => row.FirstFaithfulRank is not null).Select(row => (double)row.FirstFaithfulRank!.Value).DefaultIfEmpty().Average()
        }).ToArray();
        var summary = new { Schema = "weighted-shape-study-v1", Sources = sources.Length, Profiles = 2, Plans = Weighting.Plans.Count,
            BudgetPerProfile = budget, EvaluatedProposals = evaluated, BlockedProposals = blocked, DistinctNormalizedSamples = distinct,
            NativeRoundTrips = nativeChecks, NativeArchiveBytes = nativeBytes, SecondsBeforeGallery = watch.Elapsed.TotalSeconds,
            Scope = "Fixed-pool Bayesian compatibility experiment, no learned/calibrated probabilities. Teacher connectivity audited only after all choices.",
            Dependence = "Within-family averaging avoids per-check multiplication. Approximate conditional independence between family summary channels is an explicit supplied assumption.",
            Deduplication = "Identical connectivity and role placement up to a common positive repeat scale share one model. Repeated proposal routes gain no prior mass.",
            Training = "No fitting in this round. Future generated examples require recipe/family-held-out train/validation/test splits, likelihood calibration and separate proposal-coverage evaluation.",
            Comparisons = aggregates, Cases = records };
        JointReconstructionGallery.WriteText(Path.Combine(output, "summary.json"), JsonSerializer.Serialize(summary, Json));
        WeightedShapeGallery.Write(output, summary, examples);
        Console.WriteLine($"Complete: {sources.Length} sources, {evaluated} proposals, {nativeChecks} exact native round trips; {watch.Elapsed.TotalSeconds:0.00}s. {Path.GetFullPath(output)}");
        return 0;
    }

    public sealed record CaseResult(string Source, string Focus, string Plan, int Pool, int Evaluated, int Blocked,
        bool SourceInPool, bool BaselineExact, bool SelectedExact, bool Changed, int Ties, int Reports,
        int SelectedHardConflicts, int BaselineHardConflicts, int SelectedUnpaired, int BaselineUnpaired,
        string SelectedId, string BaselineId, string TopWeight, int? FirstFaithfulRank);

    private static Audit Reference(JointShapeDescription.Analysis analysis, JointShapeDescription.Message message, JointShapeReconstruction.Candidate candidate)
    {
        var sourceSites = E(analysis.Source.Graph.Sites); var roles = E(message.Roles);
        var actual = roles.Select((role, index) => (site: candidate.Sites[role], index)).ToDictionary(pair => pair.site, pair => pair.index, FieldNative.Participants.Instance);
        var expected = sourceSites.Select((site, index) => (site, index)).ToDictionary(pair => pair.site, pair => pair.index, FieldNative.Participants.Instance);
        HashSet<(int, int)> Edges(NativeLatticeSources.SourceCase source, Dictionary<IElement, int> order) => E(source.Graph.Strokes).Cast<Relation>().Select(edge => {
            QualitativeStrokeGraph.TryReadStroke(edge, out _, out var a, out var b, out _); return (Math.Min(order[a], order[b]), Math.Max(order[a], order[b])); }).ToHashSet();
        var a = Edges(candidate.Source, actual); var b = Edges(analysis.Source, expected);
        return new(a.Count(b.Contains), b.Count, a.Count(edge => !b.Contains(edge)));
    }
    private static long Save(string path, string id, Relation root)
    {
        var text = GojoStructureCompactJson.Serialize(GojoStructureCompactJson.FromElement(id, root));
        using (var file = File.Create(path)) using (var zip = new GZipStream(file, CompressionLevel.SmallestSize))
        using (var writer = new StreamWriter(zip)) writer.Write(text);
        return new FileInfo(path).Length;
    }
    private static double Parse(string ratio)
    { var parts = ratio.Split('/'); return double.Parse(parts[0], System.Globalization.CultureInfo.InvariantCulture) / double.Parse(parts[1], System.Globalization.CultureInfo.InvariantCulture); }
}
