using System.Diagnostics;
using System.IO.Compression;
using System.Text.Json;
using Gojo.Core;
using Gojo.Patterns;
using Gojo.Encoding.Structures;
using static Gojo.Experiments.QualitativeFrieze.Composition.JointNative;

namespace Gojo.Experiments.QualitativeFrieze.Composition;

public static class JointReconstructionExperiment
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { WriteIndented = true };

    public static int Run(string[] args)
    {
        var output = Path.Combine("experiments", "QualitativeFrieze", "results", "joint-reconstruction");
        string? selection = null;
        for (var i = 0; i < args.Length; i++)
            if (args[i] == "--output" && i + 1 < args.Length) output = args[++i];
            else if (args[i] == "--source" && i + 1 < args.Length) selection = args[++i];
            else throw new ArgumentException("Usage: --joint-reconstruction [--output DIRECTORY] [--source J01,J02,...]");
        Directory.CreateDirectory(output); var watch = Stopwatch.StartNew();
        // All source programs run before any decomposition or receiver proposal.
        var cases = JointReconstructionSources.Cases().Where(item => selection is null || selection.Split(',').Contains(item.Id)).ToArray();
        if (cases.Length == 0) throw new ArgumentException("No source matched.");
        var examples = new List<JointReconstructionGallery.Example>(); var reports = new List<object>();
        var messages = new List<IElement>(); var receiver = new List<IElement>(); var audits = new List<IElement>();
        var exactSelected = 0; var selectedNoViolations = 0; var changedChoices = 0; var assumedClosures = 0;
        foreach (var item in cases)
        {
            var analysis = JointShapeDescription.Analyze(item.Source);
            var packet = GojoStructureCompactJson.Serialize(GojoStructureCompactJson.FromElement("anonymous-joint-message-v1", analysis.Message.Surface));
            if (!GojoStructureCompactJson.TryToElement(GojoStructureCompactJson.Deserialize(packet), out var imported) ||
                imported.Root is not Relation port || !JointShapeDescription.TryRead(port, out var message, out var transport))
                throw new InvalidOperationException(item.Id + ": independent joint message failed to read.");
            var attempts = JointShapeReconstruction.ThreeAttempts(message); var best = JointShapeReconstruction.Best(attempts);
            var bestReference = ReferenceEdges(analysis, message, best);
            if (bestReference.Matched == bestReference.Expected && bestReference.Additional == 0) exactSelected++;
            if (E(best.Assessment.Violations).Length == 0) selectedNoViolations++;
            if (E(best.AssumedStrokes).Length != 0) assumedClosures++;
            if (attempts.Select(attempt => EdgeSignature(message, attempt)).Distinct().Count() > 1) changedChoices++;
            var displayAttempts = attempts.Select((attempt, index) => new JointReconstructionGallery.Attempt(index + 1, attempt.Policy,
                attempt.Source, FeedbackView(attempt, ReferenceEdges(analysis, message, attempt)), attempt.AssumedStrokes, ReferenceEquals(attempt, best))).ToArray();
            var probes = new List<object>(); var probeEpisodes = new List<IElement>();
            foreach (var profile in new[] { "Without symmetry cues", "Without interior-turn cues", "Without axis-order cues" })
            {
                var probe = new JointShapeDescription.Message(message.Frame, message.Basis, message.Roles, message.Joints,
                    profile == "Without axis-order cues" ? new Relation(message.Horizontal.Left, StructuralConstraint.Open()) : message.Horizontal,
                    profile == "Without axis-order cues" ? new Relation(message.Vertical.Left, StructuralConstraint.Open()) : message.Vertical,
                    profile == "Without symmetry cues" ? S() : message.Symmetries, message.Components, message.ClosureDefault,
                    profile == "Without interior-turn cues" ? S() : message.InteriorTurns, message.Obligations);
                if (!JointShapeDescription.TryRead(probe.Surface, out _, out _)) throw new InvalidOperationException("Invalid ablated message.");
                var probeAttempts = JointShapeReconstruction.ThreeAttempts(probe); var probeBest = JointShapeReconstruction.Best(probeAttempts);
                probes.Add(new { Profile = profile, Strokes = E(probeBest.Source.Graph.Strokes).Length,
                    SamePairingsAsFull = EdgeSignature(message, best) == EdgeSignature(probe, probeBest),
                    Score = probeBest.Assessment.Quality, Violated = E(probeBest.Assessment.Violations).Length,
                    Open = E(probeBest.Assessment.Open).Length, Reference = ReferenceEdges(analysis, message, probeBest) });
                probeEpisodes.Add(new Relation(probe.Surface, S(probeAttempts.Select(attempt => (IElement)new Relation(attempt.Surface, attempt.Assessment.Surface)).ToArray())));
            }
            var description = Description(analysis, probes);
            examples.Add(new(item.Id, item.Name, item.Note, item.Source, description, displayAttempts));
            reports.Add(new { item.Id, Joints = message.JointReads.Count, Rays = message.JointReads.Sum(joint => E(joint.Rays).Length),
                MessageBytes = System.Text.Encoding.UTF8.GetByteCount(packet), MessageNativeParticipants = Participants(message.Surface).Count(),
                SourceNativeParticipants = Participants(new Relation(item.Source.Graph.Surface, item.Source.Bindings)).Count(),
                SymmetryOperations = analysis.Symmetry.Operations.Count, ExactSymmetryOperations = analysis.Symmetry.Operations.Count(operation => operation.Exact && operation.NonTrivial),
                SymmetryUnsearched = E(analysis.Symmetry.Unsearched).Length, Selected = Array.IndexOf(attempts.ToArray(), best) + 1,
                MessageAblations = probes, Attempts = attempts.Select((attempt, index) => new { Attempt = index + 1, attempt.Policy,
                    Score = attempt.Assessment.Quality, Supported = E(attempt.Assessment.Matched).Length,
                    Violated = E(attempt.Assessment.Violations).Length, Open = E(attempt.Assessment.Open).Length,
                    Assumptions = E(attempt.AssumedStrokes).Length, Reference = ReferenceEdges(analysis, message, attempt) }).ToArray() });
            messages.Add(message.Surface);
            receiver.Add(new Relation(message.Surface, new Relation(S(attempts.Select(attempt => (IElement)new Relation(attempt.Surface, attempt.Assessment.Surface)).ToArray()), S(probeEpisodes.ToArray()))));
            audits.Add(new Relation(analysis.Audit, new Relation(transport, best.Surface)));
            Console.WriteLine($"{item.Id}: {message.JointReads.Count} joints, {analysis.Symmetry.Operations.Count} action reads; best {best.Assessment.Quality}, {E(best.Assessment.Violations).Length} violations, reference {bestReference.Matched}/{bestReference.Expected}, {E(best.AssumedStrokes).Length} assumed passages.");
        }
        var summary = new { Schema = "joint-reconstruction-v1", Sources = cases.Length, Attempts = cases.Length * 3,
            ExactReferenceConnectivitySelected = exactSelected, SelectedWithoutProvenViolation = selectedNoViolations,
            SourcesWithChangedPairings = changedChoices, SelectedWithAssumedClosure = assumedClosures,
            MessageAblationAttempts = cases.Length * 9, SecondsBeforeArchive = watch.Elapsed.TotalSeconds, Results = reports,
            Scope = "Exploratory authored local policies. No model is trained. Receiver imports only fresh joint/ray roles, ordinal order, direction actions, component promises and interior-turn refinements. Original edge pairings and position words are withheld.",
            Defaults = "Closed-area goal, finite straight passages, shared uncalibrated ordinal gap. Proposed bridges and sample gap sizes are defaults/assumptions, not observed source geometry.",
            Feedback = "Message-only structural critic. Previous violated edge pairs influence later local pairing. A violated complete joint-opposition inventory can trigger wider outer gap defaults, preserving corners. Policy schedule also changes gap/default emphasis. Teacher edge comparison occurs afterward and never chooses the best attempt.",
            Limits = "Two/four-fold action evidence uses explicit native quarter-turn interpretation; arbitrary n equal angular partitions are not inferred. General diagonal crossing, fine within-quadrant angle and curve/tangent inference remain open. Symmetry concerns undirected visible passages, not generating flow." };
        JointReconstructionGallery.WriteText(Path.Combine(output, "summary.json"), JsonSerializer.Serialize(summary, Json));
        Save(Path.Combine(output, "messages.gojo.json.gz"), "anonymous-joint-messages-v1", S(messages.ToArray()));
        Save(Path.Combine(output, "receiver-episodes.gojo.json.gz"), "joint-reconstruction-receiver-v1", S(receiver.ToArray()));
        Save(Path.Combine(output, "audit.gojo.json.gz"), "joint-reconstruction-private-teacher-audit-v1", S(audits.ToArray()));
        JointReconstructionGallery.Write(output, summary, examples);
        Console.WriteLine($"Completed {cases.Length} shapes, {exactSelected} selected exact edge inventories, {selectedNoViolations} selections without a proven violation; {watch.Elapsed.TotalSeconds:0.00}s. {Path.GetFullPath(Path.Combine(output, "gallery.html"))}");
        return 0;
    }

    private static object Description(JointShapeDescription.Analysis analysis, IReadOnlyList<object> probes)
    {
        var message = analysis.Message; var roles = E(message.Roles);
        string Label(IElement role) => "J" + (Array.FindIndex(roles, item => ReferenceEquals(item, role)) + 1);
        object[] BucketRows(Relation order) => E((Relation)order.Right).Cast<Relation>().Select(bucket => (object)E(bucket).Select(Label).ToArray()).ToArray();
        string[] ExceptionLocations(int index, bool missing)
        {
            var clause = (Relation)E(message.Symmetries)[index]; var remainder = (Relation)((Relation)clause.Right).Right;
            var scope = missing ? (Relation)((Relation)remainder.Right).Left : (Relation)remainder.Left;
            return E(scope).OfType<Relation>().Select(row => Label(row.Left)).Distinct().ToArray();
        }
        return new {
            Joints = message.JointReads.Select(joint => new { Id = Label(joint.Role), joint.Kind,
                Rays = E(joint.Rays).Cast<Relation>().Select(ray => HeadingName(message.Frame, (Relation)ray.Right)).ToArray(),
                RightAngles = E(joint.Perpendicular).Length,
                Interior = E(message.InteriorTurns).Cast<Relation>().Where(row => ReferenceEquals(((Relation)row.Left).Left, joint.Role)).Select(row => {
                    var pair = (Relation)row.Right; var difference = (Sector(message.Frame, (Relation)pair.Right) - Sector(message.Frame, (Relation)pair.Left) + 8) % 8;
                    return difference is > 0 and < 4 ? "outward / convex context" : difference is > 4 and < 8 ? "inward / concave context" : "unresolved at this heading resolution";
                }).ToArray() }).ToArray(),
            HorizontalBuckets = BucketRows(message.Horizontal), VerticalBuckets = BucketRows(message.Vertical),
            Symmetries = analysis.Symmetry.Operations.Where(operation => operation.NonTrivial && operation.MatchedEdges > 0).Select((operation, index) => new {
                operation.Name, Status = (operation.Exact ? "Witnessed on this scope" : "Partial correspondence + remainder") +
                    (ExceptionLocations(index, false).Length != 0 ? "; unmatched near " + string.Join(", ", ExceptionLocations(index, false)) : ""),
                Matched = operation.MatchedEdges, Unmatched = E(operation.Unmatched).Length, Missing = E(operation.Missing).Length,
                UnmatchedAt = ExceptionLocations(index, false), MissingAt = ExceptionLocations(index, true) }).ToArray(),
            ClosureObligations = E(message.Obligations).Length, MessageAblations = probes,
            Notes = "Counts of right angles mean proven pairs, not a claim that other pairs are non-right. Concavity belongs to an area/traversal context. Symmetry exceptions coexist with the simpler action, and no absence claim follows from a bounded unsuccessful search."
        };
    }

    private static object FeedbackView(JointShapeReconstruction.Candidate candidate, ReferenceRead reference)
    {
        string Facet(string kind) => $"{candidate.Assessment.Claims.Count(claim => claim.Kind == kind && claim.State == "Supported")} supported / {candidate.Assessment.Claims.Count(claim => claim.Kind == kind && claim.State == "Violated")} violated / {candidate.Assessment.Claims.Count(claim => claim.Kind == kind && claim.State == "Open")} open";
        return new { Quality = candidate.Assessment.Quality, Matched = E(candidate.Assessment.Matched).Length,
            Violations = E(candidate.Assessment.Violations).Length, Open = E(candidate.Assessment.Open).Length,
            Closed = Facet("closed"), JointTypes = Facet("jointTypes"), Positions = Facet("positions"), Crossings = Facet("crossings"), Overlaps = Facet("overlaps"),
            Symmetry = Facet("symmetry"), Components = Facet("components"), Refinement = Facet("refinement"),
            Assumptions = E(candidate.AssumedStrokes).Length, ReferenceEdgeMatches = $"{reference.Matched}/{reference.Expected}; {reference.Additional} other sampled edges (teacher comparison only)" };
    }

    public sealed record ReferenceRead(int Matched, int Expected, int Additional);
    private static ReferenceRead ReferenceEdges(JointShapeDescription.Analysis analysis, JointShapeDescription.Message imported, JointShapeReconstruction.Candidate candidate)
    {
        var teacherSites = E(analysis.Source.Graph.Sites); var roles = E(imported.Roles);
        var index = roles.Select((role, i) => (site: candidate.Sites[role], i)).ToDictionary(pair => pair.site, pair => pair.i, FieldNative.Participants.Instance);
        var teacherIndex = teacherSites.Select((site, i) => (site, i)).ToDictionary(pair => pair.site, pair => pair.i, FieldNative.Participants.Instance);
        HashSet<(int, int)> Edges(NativeLatticeSources.SourceCase source, Dictionary<IElement, int> positions) => E(source.Graph.Strokes).Cast<Relation>().Select(edge => {
            QualitativeStrokeGraph.TryReadStroke(edge, out _, out var a, out var b, out _); return (Math.Min(positions[a], positions[b]), Math.Max(positions[a], positions[b])); }).ToHashSet();
        var expected = Edges(analysis.Source, teacherIndex); var actual = Edges(candidate.Source, index);
        return new(actual.Count(expected.Contains), expected.Count, actual.Count(edge => !expected.Contains(edge)));
    }
    private static string EdgeSignature(JointShapeDescription.Message message, JointShapeReconstruction.Candidate candidate) =>
        string.Join('|', E(candidate.Pairings).Cast<Relation>().Select(pair => {
            var ends = (Relation)pair.Left; var rays = message.JointReads.SelectMany(joint => E(joint.Rays).Cast<Relation>()).Select(row => row.Left).ToArray();
            var a = Array.FindIndex(rays, slot => ReferenceEquals(slot, ends.Left)); var b = Array.FindIndex(rays, slot => ReferenceEquals(slot, ends.Right)); return Math.Min(a, b) + ":" + Math.Max(a, b);
        }).Order(StringComparer.Ordinal));
    private static IEnumerable<IElement> Participants(IElement root)
    {
        var seen = new HashSet<IElement>(ReferenceEqualityComparer.Instance); var pending = new Stack<IElement>(); pending.Push(root);
        while (pending.TryPop(out var item)) { if (!seen.Add(item)) continue; yield return item; if (item is Relation relation) { pending.Push(relation.Left); pending.Push(relation.Right); } }
    }
    private static void Save(string path, string id, Relation surface)
    {
        var data = GojoStructureCompactJson.Serialize(GojoStructureCompactJson.FromElement(id, surface));
        var pending = path + ".pending";
        using (var file = File.Create(pending))
        using (var zip = new GZipStream(file, CompressionLevel.SmallestSize))
        using (var writer = new StreamWriter(zip)) writer.Write(data);
        File.Move(pending, path, overwrite: true);
    }
}
