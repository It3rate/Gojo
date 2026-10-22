using System.Diagnostics;
using System.IO.Compression;
using System.Text.Json;
using Gojo.Core;
using Gojo.Patterns;
using Gojo.Encoding.Structures;
using static Gojo.Experiments.QualitativeFrieze.Composition.JointNative;

namespace Gojo.Experiments.QualitativeFrieze.Composition;

public static class RelationalShapeExperiment
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { WriteIndented = true };
    public sealed record Packet(JointShapeDescription.Message Message, RelationalSpatialEvidence.Read Spatial,
        JointEquivalenceEvidence.Read Equivalence, StructuralEvidenceStreams.Bundle Streams, Relation Surface, Relation Transport);

    public static int Run(string[] args)
    {
        var output = Path.Combine("experiments", "QualitativeFrieze", "results", "relational-shapes");
        var budget = 64; string? selection = null; var extentRepair = false;
        for (var i = 0; i < args.Length; i++)
            if (args[i] == "--output" && i + 1 < args.Length) output = args[++i];
            else if (args[i] == "--budget" && i + 1 < args.Length) budget = int.Parse(args[++i]);
            else if (args[i] == "--source" && i + 1 < args.Length) selection = args[++i];
            else if (args[i] == "--extent-repair") extentRepair = true;
            else throw new ArgumentException("Usage: --relational-shapes [--budget N] [--source R001,...] [--extent-repair] [--output DIRECTORY]");
        if (budget < 3) throw new ArgumentOutOfRangeException(nameof(budget));
        Directory.CreateDirectory(output); var watch = Stopwatch.StartNew();
        // All 100 teacher sources exist before any decomposition takes place.
        var sources = RelationalShapeCorpus.Cases().Where(item => selection is null || selection.Split(',').Contains(item.Id)).ToArray();
        if (sources.Length == 0) throw new ArgumentException("No sources matched.");
        var examples = new List<RelationalShapeGallery.Example>(); var reports = new List<object>();
        var messages = new List<IElement>(); var episodes = new List<IElement>(); var audits = new List<IElement>();
        var requested = 0; var evaluated = 0; var blocked = 0; var zeroConflicts = 0; var faithfulExact = 0;
        var rejectedDefaults = 0; var completionAssumptions = 0; var equivalenceChanged = 0; var sparseExact = 0;
        foreach (var source in sources)
        {
            var analysis = JointShapeDescription.Analyze(source.Source);
            var packet = Publish(analysis); var m = packet.Message;
            var defaultContext = StructuralEvidenceStreams.SelectClosure(m, packet.Streams);
            var repairStreams = RepairGoal(m, packet.Streams);
            var repairContext = StructuralEvidenceStreams.SelectClosure(m, repairStreams);
            var full = RelationalSpatialEvidence.Select(packet.Spatial, m, "observed");
            var sparse = RelationalSpatialEvidence.Select(packet.Spatial, m, "sparse");
            RelationalShapeSearch.Focus[] focuses = [
                new("evidence", "Evidence first", full, packet.Equivalence, defaultContext),
                new("completion", "Requested completion", full, packet.Equivalence, repairContext),
                new("sparse", "Sparse relative locations", sparse, packet.Equivalence, defaultContext),
                new("no-equivalence", "Equivalence withheld", full, null, defaultContext)
            ];
            var runs = focuses.Select(focus => RelationalShapeSearch.Run(m, focus, budget, extentRepair)).ToArray();
            requested += runs.Sum(run => run.Requested); evaluated += runs.Sum(run => run.Evaluated); blocked += runs.Sum(run => run.Blocked);
            if (runs[0].Best.HardConflicts == 0 && runs[0].Best.UnpairedRays == 0) zeroConflicts++;
            var reference = Reference(analysis, m, runs[0].Best.Candidate);
            if (reference.Exact) faithfulExact++;
            if (Reference(analysis, m, runs[2].Best.Candidate).Exact) sparseExact++;
            if (!defaultContext.ShouldComplete && defaultContext.PreserveObservedGap) rejectedDefaults++;
            completionAssumptions += runs[1].Best.Assumptions;
            if (RelationalShapeSearch.Signature(m, runs[0].Best.Candidate) != RelationalShapeSearch.Signature(m, runs[3].Best.Candidate)) equivalenceChanged++;
            var rows = runs.Select(run => new RelationalShapeGallery.Result(run.Focus.Id, run.Focus.Name,
                run.Best.Candidate.Source, run.Best.Candidate.AssumedStrokes, Feedback(run, run.Best, Reference(analysis, m, run.Best.Candidate)),
                run.Retained.Select((trial, index) => new RelationalShapeGallery.Attempt(index + 1,
                    $"Search proposal {trial.Index}", trial.Candidate.Source, trial.Candidate.AssumedStrokes,
                    Feedback(run, trial, Reference(analysis, m, trial.Candidate)), ReferenceEquals(trial, run.Best) &&
                        index == Array.FindIndex(run.Retained.ToArray(), item => ReferenceEquals(item, run.Best)))).ToArray())).ToArray();
            examples.Add(new(source.Id, source.Name, source.Note, source.Source, Evidence(packet, defaultContext), rows));
            reports.Add(new { source.Id, TeacherFamily = source.Control.Family, JointRoles = m.JointReads.Count,
                ExactLengthGroups = E(packet.Equivalence.ExactLengths).Length, ExactAngleGroups = E(packet.Equivalence.ExactAngles).Length,
                CoarseAngleGroups = E(packet.Equivalence.SimilarAngles).Length, ObservedGap = defaultContext.PreserveObservedGap,
                DefaultClosurePermitted = defaultContext.ShouldComplete, Results = runs.Select(run => new {
                    Profile = run.Focus.Id, run.Requested, run.Evaluated, run.Blocked, run.Improved, run.DistinctSamples,
                    SelectedSearchIndex = run.Best.Index, run.Best.HardConflicts, run.Best.UnpairedRays, run.Best.GoalBlocked,
                    run.Best.DefaultConflicts, run.Best.Assumptions, run.Best.Open, run.Best.Supported,
                    Reference = Reference(analysis, m, run.Best.Candidate) }).ToArray() });
            messages.Add(packet.Surface);
            // Selected concrete states can be replayed through the retained
            // native messages. Discarded trial histories are not promised.
            episodes.Add(new Relation(packet.Surface, S(runs.Select(run => (IElement)new Relation(run.Focus.Surface,
                new Relation(run.Best.Snapshot, new Relation(run.Best.Candidate.Pairings, run.Best.Candidate.AssumedStrokes)))).ToArray())));
            var originalRoles = E(analysis.Message.Roles); var importedRoles = E(m.Roles);
            var roleTransport = S(originalRoles.Select((role, index) => (IElement)new Relation(role, importedRoles[index])).ToArray());
            audits.Add(new Relation(new Relation(source.Source.Graph.Surface, source.Source.Bindings),
                new Relation(new Relation(analysis.SourceMap, roleTransport), new Relation(source.Control.Surface,
                    new Relation(packet.Surface, S(runs.Select(run => (IElement)run.Best.Snapshot).ToArray()))))));
            JointReconstructionGallery.WriteText(Path.Combine(output, "progress.json"), JsonSerializer.Serialize(new {
                Completed = reports.Count, Total = sources.Length, Requested = requested, Evaluated = evaluated, Blocked = blocked,
                Seconds = watch.Elapsed.TotalSeconds, Last = source.Id }, Json));
            Console.WriteLine($"{source.Id} {source.Control.Family}: {m.JointReads.Count} joints; evidence {reference.Matched}/{reference.Expected}, hard {runs[0].Best.HardConflicts}, unpaired {runs[0].Best.UnpairedRays}; sparse {runs[2].Best.HardConflicts}/{runs[2].Best.UnpairedRays}; {evaluated} reads, {watch.Elapsed.TotalSeconds:0.0}s.");
        }
        var summary = new { Schema = "relational-shape-study-v1", Sources = sources.Length, Profiles = 4, BudgetPerProfile = budget, ExtentRepair = extentRepair,
            RequestedProposals = requested, EvaluatedProposals = evaluated, BlockedOrdinalHypotheses = blocked,
            EvidenceSelectionsWithoutHardConflictOrMissingRays = zeroConflicts, ExactObservedConnectivity = faithfulExact,
            SparseExactObservedConnectivity = sparseExact, ClosureDefaultsDefeated = rejectedDefaults,
            CompletionAssumedPassages = completionAssumptions, EquivalenceChangesSelectedSample = equivalenceChanged,
            SecondsBeforeArchive = watch.Elapsed.TotalSeconds,
            Scope = "Native unordered joints, independent axis projection/order facts, exact unmeasured equivalences, coarse hypotheses, ordinal support/doubt and separate goals. No original endpoint partner, source word, teacher family or completion template enters search.",
            Search = "Bounded authored proposal and criticism, not model training. Guessed pairings, ordinal embeddings and gap repetitions are explicit hypotheses. Rank prioritizes hard evidence conflicts, missing rays, blocked goals, default conflicts and sampling cost.",
            Confidence = "Four connected ordinal rungs, no calibrated probabilities. Support and opposition remain separate. Duplicated streams do not add certainty. Goals do not establish observation.",
            Retention = "Three inspection samples and one selected native replay state per profile; other search outcomes are aggregated. Snapshots deliberately omit earlier policy histories. No exact replay promise for discarded attempts.",
            Limits = "Finite straight passages and existing native D4 metric interpretation. General near-length tolerance, arbitrary angular partitions, curves, global truth-maintenance and learned proposal policies remain open. Default crossing/overlap penalties are not universal evidence of invalidity.",
            Results = reports };
        JointReconstructionGallery.WriteText(Path.Combine(output, "summary.json"), JsonSerializer.Serialize(summary, Json));
        Save(Path.Combine(output, "messages.gojo.json.gz"), "relational-shape-messages-v1", S(messages.ToArray()));
        Save(Path.Combine(output, "selected-episodes.gojo.json.gz"), "relational-shape-selected-v1", S(episodes.ToArray()));
        Save(Path.Combine(output, "teacher-audit.gojo.json.gz"), "relational-shape-teacher-v1", S(audits.ToArray()));
        RelationalShapeGallery.Write(output, summary, examples);
        Console.WriteLine($"Complete: {sources.Length} sources, {evaluated} assessed proposals, {blocked} blocked layouts, {faithfulExact} exact observed connection inventories; {watch.Elapsed.TotalSeconds:0.00}s. {Path.GetFullPath(output)}");
        return 0;
    }

    public static Packet Publish(JointShapeDescription.Analysis analysis)
    {
        var original = analysis.Message;
        var spatial = RelationalSpatialEvidence.Analyze(original); var equivalence = JointEquivalenceEvidence.Analyze(analysis);
        // No constructor/path order or global axis bucket list is supplied.
        var message = new JointShapeDescription.Message(original.Frame, original.Basis, original.Roles, original.Joints,
            new(original.Frame.Horizontal, StructuralConstraint.Open()), new(original.Frame.Vertical, StructuralConstraint.Open()),
            original.Symmetries, original.Components, original.ClosureDefault, original.InteriorTurns, original.Obligations);
        var closure = StructuralEvidenceStreams.ClosureEvidence(message); var ladder = closure.Ladder;
        var observations = E(closure.Observations).Concat(new[] { message.Joints, spatial.Surface, equivalence.ExactLengths, equivalence.ExactAngles }
            .Select(claim => (IElement)new StructuralEvidenceStreams.Stream(claim, ladder.Strong, ladder.None,
                message.Roles, S(message.Roles), claim).Surface)).ToArray();
        var hypotheses = S(new StructuralEvidenceStreams.Stream(equivalence.SimilarAngles, ladder.Weak, ladder.None,
            message.Roles, S(message.Joints), equivalence.SimilarAngles).Surface);
        var streams = new StructuralEvidenceStreams.Bundle(ladder, S(observations), hypotheses, closure.Defaults, closure.Goals);
        return Import(new Relation(message.Surface, new Relation(spatial.Surface, new Relation(equivalence.Surface, streams.Surface))));
    }

    public static Packet Import(Relation surface)
    {
        var data = GojoStructureCompactJson.Serialize(GojoStructureCompactJson.FromElement("relational-shape-packet-v1", surface));
        if (!GojoStructureCompactJson.TryToElement(GojoStructureCompactJson.Deserialize(data), out var decoded) || decoded.Root is not Relation root ||
            root.Left is not Relation jointPort || !JointShapeDescription.TryRead(jointPort, out var message, out var transport))
            throw new ArgumentException("The joint message did not survive standalone transport.");
        // Use the exact transport's node correspondence for all companion
        // streams, including relation-valued claims such as the closure goal.
        var rebind = (Relation)((Relation)transport.Right).Left;
        var map = (Relation)((Relation)((Relation)rebind.Right).Right).Left;
        var replacements = E(map).Cast<Relation>().ToDictionary(row => row.Left, row => row.Right, FieldNative.Participants.Instance);
        var companion = (Relation)Copy(root.Right);
        if (companion.Left is not Relation rawSpatial || companion.Right is not Relation { Left: Relation rawEquivalence, Right: Relation rawStreams } ||
            !RelationalSpatialEvidence.TryRead(rawSpatial, message, out var spatial, out _) ||
            !JointEquivalenceEvidence.TryRead(rawEquivalence, message, out var equivalence, out _) ||
            !StructuralEvidenceStreams.Bundle.TryRead(rawStreams, out var streams))
            throw new ArgumentException("The companion evidence streams failed their exact readers.");
        var actual = new Relation(message.Surface, new Relation(spatial.Surface, new Relation(equivalence.Surface, streams.Surface)));
        return new(message, spatial, equivalence, streams, actual, transport);

        IElement Copy(IElement item)
        {
            if (replacements.TryGetValue(item, out var replacement)) return replacement;
            var result = item is Relation relation ? new Relation(Copy(relation.Left), Copy(relation.Right)) : item;
            replacements[item] = result; return result;
        }
    }

    private static StructuralEvidenceStreams.Bundle RepairGoal(JointShapeDescription.Message message, StructuralEvidenceStreams.Bundle source)
    {
        var goal = new StructuralEvidenceStreams.Stream(message.ClosureDefault, source.Ladder.Strong, source.Ladder.None,
            message.ClosureDefault, S(), new Relation(message.Roles, message.ClosureDefault));
        return new(source.Ladder, source.Observations, source.Hypotheses, source.Defaults, S(goal.Surface));
    }

    private static object Evidence(Packet packet, StructuralEvidenceStreams.ClosureSelection context)
    {
        var m = packet.Message; var roles = E(m.Roles);
        var names = m.JointReads.SelectMany((joint, index) => E(joint.Rays).Cast<Relation>().Select((ray, at) => (ray.Left, Name: $"J{index + 1}.r{at + 1}")))
            .ToDictionary(pair => pair.Left, pair => pair.Name, FieldNative.Participants.Instance);
        string Member(IElement item) => names.TryGetValue(item, out var label) ? label : item is Relation pair ? Member(pair.Left) + " ∠ " + Member(pair.Right) : "open role";
        object[] Groups(Relation groups) => E(groups).Cast<Relation>().Select(group => (object)JointEquivalenceEvidence.Members(group).Select(Member).ToArray()).ToArray();
        var streams = new[] { ("observation", packet.Streams.Observations), ("hypothesis", packet.Streams.Hypotheses), ("default", packet.Streams.Defaults), ("goal", packet.Streams.Goals) }
            .SelectMany(pair => packet.Streams.Read(pair.Item2).Select(stream => new { Context = pair.Item1,
                Claim = ReferenceEquals(stream.Claim, m.ClosureDefault) ? "Current closure / proposed completion" :
                    ReferenceEquals(stream.Claim, m.Joints) ? "Observed local joints" : ReferenceEquals(stream.Claim, packet.Equivalence.ExactLengths) ? "Exact unmeasured length groups" :
                    ReferenceEquals(stream.Claim, packet.Equivalence.ExactAngles) ? "Exact angle groups" : ReferenceEquals(stream.Claim, packet.Equivalence.SimilarAngles) ? "Coarse angle hypotheses" : "Relative position facts",
                Support = packet.Streams.Ladder.Name(stream.Support), Opposition = packet.Streams.Ladder.Name(stream.Opposition) })).ToArray();
        return new { Joints = m.JointReads.Select((joint, index) => new { Id = "J" + (index + 1), joint.Kind,
                Rays = E(joint.Rays).Cast<Relation>().Select(ray => HeadingName(m.Frame, (Relation)ray.Right)).ToArray() }).ToArray(),
            SpatialSummary = "Unordered projection-equivalence groups and directed above/below/left/right facts. These do not prescribe an edge sequence. Sparse profiles withhold some comparisons; the receiver explicitly proposes missing orders.",
            LengthGroups = Groups(packet.Equivalence.ExactLengths), AngleGroups = Groups(packet.Equivalence.ExactAngles),
            Streams = streams, Symmetries = $"{E(m.Symmetries).Length} partial or exact native action correspondences; arbitrary absent actions are not ruled out.",
            Closure = new { Selected = context.ShouldComplete ? "Completion is permitted" : "Preserve observed opening",
                Support = packet.Streams.Ladder.Name(context.Read.Support), Doubt = packet.Streams.Ladder.Name(context.Read.Opposition),
                Alternatives = "Closed parent with damage; genuinely open path; optional requested repair" },
            Notes = "Exact comparison promises are conditional on the native shared-reach/turn basis. Coarse similarity is a weak hypothesis. Confidence is ordinal support and opposition, not a probability; two streams can disagree without being erased." };
    }

    private static object Feedback(RelationalShapeSearch.Result run, RelationalShapeSearch.Trial trial, ReferenceRead reference) => new {
        SearchProposal = trial.Index, run.Requested, run.Evaluated, run.Blocked, run.DistinctSamples,
        trial.HardConflicts, trial.UnpairedRays, trial.GoalBlocked, trial.DefaultConflicts, trial.Assumptions, trial.Open,
        ExactLengthConflicts = trial.Equivalence?.Claims.Count(claim => claim.Kind == "lengthEquivalence" && claim.State == "Contradicted") ?? 0,
        ExactAngleConflicts = trial.Equivalence?.Claims.Count(claim => claim.Kind == "angleEquivalence" && claim.State == "Contradicted") ?? 0,
        SourceComparison = $"{reference.Matched}/{reference.Expected} observed edges; {reference.Additional} other passages (audit only)",
        Interpretation = run.Focus.Context.PreserveObservedGap ? run.Focus.Context.ShouldComplete ? "Repair requested; observed gap retained separately" : "Opening retained; closed-parent interpretation remains tentative" : "No exact one-ray opening observed"
    };

    public sealed record ReferenceRead(int Matched, int Expected, int Additional)
    { public bool Exact => Matched == Expected && Additional == 0; }
    private static ReferenceRead Reference(JointShapeDescription.Analysis analysis, JointShapeDescription.Message message, JointShapeReconstruction.Candidate candidate)
    {
        var sourceSites = E(analysis.Source.Graph.Sites); var roles = E(message.Roles);
        var actual = roles.Select((role, index) => (site: candidate.Sites[role], index)).ToDictionary(pair => pair.site, pair => pair.index, FieldNative.Participants.Instance);
        var expected = sourceSites.Select((site, index) => (site, index)).ToDictionary(pair => pair.site, pair => pair.index, FieldNative.Participants.Instance);
        HashSet<(int, int)> Edges(NativeLatticeSources.SourceCase source, Dictionary<IElement, int> order) => E(source.Graph.Strokes).Cast<Relation>().Select(edge => {
            QualitativeStrokeGraph.TryReadStroke(edge, out _, out var a, out var b, out _); return (Math.Min(order[a], order[b]), Math.Max(order[a], order[b])); }).ToHashSet();
        var a = Edges(candidate.Source, actual); var b = Edges(analysis.Source, expected);
        return new(a.Count(b.Contains), b.Count, a.Count(edge => !b.Contains(edge)));
    }
    private static void Save(string path, string id, Relation root)
    {
        var text = GojoStructureCompactJson.Serialize(GojoStructureCompactJson.FromElement(id, root)); var pending = path + ".pending";
        using (var file = File.Create(pending)) using (var zip = new GZipStream(file, CompressionLevel.SmallestSize)) using (var writer = new StreamWriter(zip)) writer.Write(text);
        File.Move(pending, path, true);
    }
}
