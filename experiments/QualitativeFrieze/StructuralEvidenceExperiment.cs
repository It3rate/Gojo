using System.Diagnostics;
using System.IO.Compression;
using System.Text.Json;
using Gojo.Core;
using Gojo.Encoding.Structures;

namespace Gojo.Experiments.QualitativeFrieze;

/// <summary>Explanatory views of finite native structural inquiries. Drawings
/// are emitted after acquisition and private evaluation; they feed neither.</summary>
public static partial class StructuralEvidenceExperiment
{
    public static int Run(string[] args)
    {
        var output = Path.Combine("experiments", "QualitativeFrieze", "results", "structural-evidence");
        for (var i = 0; i < args.Length; i++)
            if (args[i] == "--output" && i + 1 < args.Length) output = args[++i];
            else throw new ArgumentException("Usage: --structural-evidence [--output DIRECTORY]");
        Directory.CreateDirectory(output);
        var watch = Stopwatch.StartNew(); var study = StructuralEvidenceStudy.Build(); watch.Stop();
        var sampleModels = study.Cases.Cases.Select(example =>
        {
            var sourceImage = example.Id + "-source.svg";
            File.WriteAllText(Path.Combine(output, sourceImage), Svg(example.Source, example.Name + " / private source"));
            var candidates = example.Candidates.Select((sample, index) =>
            {
                var image = example.Id + "-candidate-" + index + ".svg";
                File.WriteAllText(Path.Combine(output, image), Svg(sample, sample.Name));
                return new { Index = index, sample.Name, Image = image, Sites = E(sample.Graph.Sites).Length,
                    Edges = E(sample.Graph.Strokes).Length,
                    DeclaredCrossings = E(sample.Observation.Crossings).Length };
            }).ToArray();
            return new { example.Id, example.Name, example.Description, example.QuestionPurpose,
                HumanReading = example.CoarseHint, example.ExpectedAmbiguity, SourceImage = sourceImage,
                SourceName = example.Source.Name, Candidates = candidates };
        }).ToArray();
        var episodes = study.Episodes.Select(episode =>
        {
            int Index(StructuralEvidenceInquiry.Candidate candidate) => episode.Receiver.Candidates.ToList()
                .FindIndex(actual => ReferenceEquals(actual, candidate));
            object Stage(StructuralEvidenceInquiry.Assessment assessment, string name, StructuralEvidenceInquiry.Query? question = null,
                string? answer = null, double[]? features = null, double? prediction = null) => new {
                    Name = name, Question = question is null ? null : QuestionLabel(question.Name), Answer = answer, Features = features, Prediction = prediction,
                    Eligible = assessment.Eligible.Select(read => Index(read.Candidate)).ToArray(),
                    Conflicts = E(assessment.Conflicts).Length,
                    Reads = assessment.Candidates.Select(read => new { Index = Index(read.Candidate),
                        AnswerToQuestion = question is null ? null : PredictionAnswer(episode.Receiver.Predict(read.Candidate, question)),
                        Supported = E(read.Supported).Length, Refuted = E(read.Refuted).Length,
                        Open = E(read.Open).Length, Blocked = E(read.Blocked).Length }).ToArray() };
            var stages = new List<object> { Stage(episode.Initial, "0 · Start: no source facts yet") };
            stages.AddRange(episode.Steps.Select((step, index) => Stage(step.After, (index + 1) + " · " + Answer(step.Response),
                step.Query, Answer(step.Response), step.Features.Values, step.Prediction)));
            var expected = episode.Case.ExpectedAmbiguity;
            var status = episode.CorrectAtVocabulary ? "One candidate remains and independently agrees with the source at the tested motif vocabulary."
                : episode.CorrectAmbiguity ? "Both recipes remain possible. The exposed output does not reveal how it was constructed."
                : episode.HasConflict ? "Contradictory same-event replies remain visible; no candidate is certified."
                : episode.HasBlocked ? "The receiver has no admitted output capability for this request."
                : "The available questions did not resolve the alternatives. Unknown evidence stays open.";
            return new { episode.Id, CaseId = episode.Case.Id, episode.Policy, episode.Control,
                Requests = episode.Steps.Count, episode.CorrectAtVocabulary, episode.CorrectAmbiguity,
                HasConflict = episode.HasConflict, HasBlocked = episode.HasBlocked, Status = status,
                ExpectedAmbiguity = expected, Stages = stages,
                Evaluations = episode.Evaluations.Select((evaluation, index) => new { Index = index,
                    Compatible = evaluation.IsCompatibleAtVocabulary, Supported = E(evaluation.Supported).Length,
                    Refuted = E(evaluation.Refuted).Length, Open = E(evaluation.Open).Length,
                    Blocked = E(evaluation.Blocked).Length }).ToArray() };
        }).ToArray();
        var native = Reachable(study.Surface).ToArray();
        if (native.Any(item => item is Carrier or CarrierLocation))
            throw new InvalidOperationException("Structural inquiries must not acquire numeric measurements through their statistical policy.");
        var archive = GojoStructureCompactJson.Serialize(GojoStructureCompactJson.FromElement("structural-evidence-v1", study.Surface));
        if (!GojoStructureCompactJson.TryToElement(GojoStructureCompactJson.Deserialize(archive), out var restored) ||
            Reachable(restored.Root).Count() != native.Length)
            throw new InvalidOperationException("Native inquiry and audit worlds must restore completely.");
        var restoredPolicy = EvidenceSelectionStudy.LearnedPolicy.FromSurface((Relation)((Relation)restored.Root).Left);
        if (!restoredPolicy.Coefficients.Zip(study.Policy.Coefficients).All(pair => Math.Abs(pair.First - pair.Second) < 1e-10))
            throw new InvalidOperationException("Transported native training evidence must retain the unchanged ranking read.");
        using (var file = File.Create(Path.Combine(output, "inquiries.gojo.json.gz")))
        using (var zip = new GZipStream(file, CompressionLevel.SmallestSize))
        using (var writer = new StreamWriter(zip)) writer.Write(archive);
        var model = new { Schema = "structural-evidence-v1", Cases = sampleModels, Episodes = episodes,
            study.Summaries, Policy = new { Origin = "Earlier span-acquisition training only", TrainingRows = study.Policy.TrainingRows,
                Coefficients = study.Policy.Coefficients, StructuralTrainingEpisodes = 0 },
            NativeElements = native.Length, NativeNumericElements = 0, ArchiveRoundTrip = true,
            ElapsedStudyMilliseconds = watch.Elapsed.TotalMilliseconds,
            Limits = new[] {
                "The candidates are supplied executable constructions. This is selective inquiry among known possibilities, not open-ended shape discovery or pixel recognition.",
                "The earlier five ranking coefficients are reused unchanged. No new topology, join or heading examples train them in this run.",
                "The source, supplier audit and post-run vocabulary comparison are separate from receiver inputs. The caption suggesting a coarse human reading is explanatory text, not a transmitted native hint.",
                "Queries ask whether actual bounded incidence/heading patterns exist. Negative answers require completed search within the declared grammar. Incomplete or unsupported searches return open.",
                "A valid broader question is placed first for every policy in the five distinguishing cases. It cannot separate those candidate libraries; its purpose is to check that the ranking reads actual discrimination rather than list order.",
                "The source supplier eagerly induces its bounded motif inventory before answering. Fewer requests demonstrate selective communication; this does not yet demonstrate avoided source-side search computation.",
                "Independent validation compares both bounded existential motif vocabularies. Agreement does not prove graph isomorphism, motif counts, translation cadence, global arrangement or construction ancestry.",
                "The crossing case rejects a shared junction among the supplied alternatives. It does not discover the crossing position or choose over/under. The source separately carries an explicit crossing declaration.",
                "The square recipe case intentionally keeps two alternatives. A provenance service would be needed to distinguish their generating constructions.",
                "One existential answer publishes one matching occurrence. Other source components and occurrences stay private; the complete proof is retained only on the supplier audit surface.",
                "Screen coordinates and angles are illustrative realizations. Solid dots indicate actual shared sites; paired hollow markers indicate different actual sites at one projected location. Rendering creates no native connections."
            } };
        var json = JsonSerializer.Serialize(model, new JsonSerializerOptions(JsonSerializerDefaults.Web) { WriteIndented = true });
        File.WriteAllText(Path.Combine(output, "diagnostics.json"), json);
        File.WriteAllText(Path.Combine(output, "gallery.html"), Html(json));
        foreach (var summary in study.Summaries)
            Console.WriteLine($"{summary.Policy}: {summary.Resolved}/{summary.Episodes} uniquely resolved; {summary.Open} open; {summary.MeanRequests:0.###} mean requests.");
        Console.WriteLine($"{study.Episodes.Count} episodes; {native.Length:N0} native participants; archive restored; {watch.Elapsed.TotalSeconds:0.###} s study build.");
        Console.WriteLine(Path.GetFullPath(output)); return 0;
    }
    private static string Answer(StructuralEvidenceInquiry.Response response) => E(response.Supported).Length > 0 ? "Yes"
        : E(response.Refuted).Length > 0 ? "No" : "Open";
    private static string PredictionAnswer(CommunicationAssessment prediction) => prediction.SupportedCount > 0 ? "Yes"
        : prediction.RefutedCount > 0 ? "No" : "Open";
    private static string QuestionLabel(string question) => question
        .Replace("three-edge cycle", "closed loop with exactly three edges", StringComparison.Ordinal)
        .Replace("four-edge cycle", "closed loop with exactly four edges", StringComparison.Ordinal)
        .Replace("four-edge closed contour", "closed loop with exactly four edges", StringComparison.Ordinal);
    private static IEnumerable<IElement> Reachable(IElement root)
    {
        var seen = new HashSet<IElement>(ReferenceEqualityComparer.Instance); var pending = new Stack<IElement>(); pending.Push(root);
        while (pending.TryPop(out var item)) if (seen.Add(item))
        { yield return item; if (item is Relation relation) { pending.Push(relation.Left); pending.Push(relation.Right); } }
    }
}
