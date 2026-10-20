using System.Diagnostics;
using System.IO.Compression;
using System.Text.Json;
using Gojo.Core;
using Gojo.Encoding.Structures;
using Gojo.Experiments.QualitativeExecution;
using Gojo.Patterns;

namespace Gojo.Experiments.QualitativeFrieze;

/// <summary>Human inspection artifacts derived after the native reads. SVGs
/// and descriptive labels do not participate in matching or comparison.</summary>
public static partial class OccurrenceCompositionExperiment
{
    public static int Run(string[] args)
    {
        var output = Path.Combine("experiments", "QualitativeFrieze", "results", "occurrence-composition");
        for (var i = 0; i < args.Length; i++)
            if (args[i] == "--output" && i + 1 < args.Length) output = args[++i];
            else throw new ArgumentException("Usage: --occurrence-composition [--output DIRECTORY]");
        Directory.CreateDirectory(output);
        var watch = Stopwatch.StartNew(); var study = OccurrenceCompositionStudy.Build(); watch.Stop();
        var episodes = study.Episodes.Select(episode =>
        {
            var example = episode.Case;
            string Draw(StructuralEvidenceCases.Sample sample, string suffix)
            {
                var name = example.Id + "-" + suffix + ".svg";
                File.WriteAllText(Path.Combine(output, name), StructuralEvidenceExperiment.RenderSample(sample, sample.Name));
                return name;
            }
            return new { example.Id, example.Name, example.Description, example.QuestionPurpose, example.ExpectedAmbiguity,
                Source = new { example.Source.Name, Image = Draw(example.Source, "source") },
                Candidates = example.Candidates.Select((candidate, index) => new { Index = index, candidate.Name,
                    Image = Draw(candidate, "candidate-" + index),
                    Sites = E(candidate.Observation.Sites).Length, Edges = E(candidate.Observation.Segments).Length,
                    EarlierVocabulary = episode.EarlierVocabulary[index].IsCompatibleAtVocabulary ? "Agrees" :
                        E(episode.EarlierVocabulary[index].Open).Length != 0 ? "Open" : "Differs" }).ToArray(),
                Stages = episode.Stages.Select((stage, index) => new {
                    Index = index,
                    Question = index == 0 ? null : episode.Reads[index - 1].Question.Name,
                    SourceAnswer = index == 0 ? null : Answer(episode.Reads[index - 1].Source),
                    Attempts = index == 0 ? 0 : E(episode.Reads[index - 1].Source.Consumed).Length,
                    Compatible = stage.Compatible.Select(candidate => candidate.Index).ToArray(),
                    Reads = stage.Candidates.Select(read => new { read.Index, read.IsCompatible, read.IsSettled,
                        Answer = index == 0 ? null : Answer(episode.Reads[index - 1].Candidates[read.Index]),
                        Agreements = E(read.Agreement).Length, Oppositions = E(read.Opposition).Length,
                        Open = E(read.Open).Length, Blocked = E(read.Blocked).Length }).ToArray()
                }).ToArray()
            };
        }).ToArray();
        var native = Reachable(study.Surface).ToArray();
        if (native.Any(element => element is Carrier or CarrierLocation))
            throw new InvalidOperationException("Qualitative composition cannot acquire measured numeric participants.");
        var archive = GojoStructureCompactJson.Serialize(GojoStructureCompactJson.FromElement("occurrence-composition-v1", study.Surface));
        if (!GojoStructureCompactJson.TryToElement(GojoStructureCompactJson.Deserialize(archive), out var restored) ||
            Reachable(restored.Root).Count() != native.Length)
            throw new InvalidOperationException("The native composition study must restore completely.");
        using (var file = File.Create(Path.Combine(output, "composition.gojo.json.gz")))
        using (var zip = new GZipStream(file, CompressionLevel.SmallestSize))
        using (var writer = new StreamWriter(zip)) writer.Write(archive);
        var model = new { Schema = "occurrence-composition-v1", Cases = episodes,
            Controls = study.Controls.Select(control => new { control.Name, control.Purpose, Answer = Answer(control.Read),
                Attempts = E(control.Read.Consumed).Length, Open = E(control.Read.Open).Length, Blocked = E(control.Read.Blocked).Length }).ToArray(),
            study.Scaling, NativeElements = native.Length, NativeNumericElements = 0, ArchiveRoundTrip = true,
            BuildMilliseconds = watch.Elapsed.TotalMilliseconds,
            Limits = new[] {
                "Questions are composed from supplied native motif definitions. This run does not learn new motifs, select questions statistically, or recognize pixels.",
                "The reader observes an explicitly exposed local inventory. It does not implement a new private supplier/reply protocol; complete source assessments belong to this local analysis and audit fixture.",
                "Distinct means different actual edge footprints. Multiple traversal starts or reverse readings of the same loop are not additional copies.",
                "Two/three roles establish an existential lower bound, not an exact global count. A disjoint pair does not prove that the entire graph is disconnected.",
                "Sharing uses actual site or edge identity. Coincident illustrative positions do not join, merge or duplicate participants.",
                "A negative answer requires complete motif search within its supported grammar and exhausted compound assignments. An exhausted budget remains Open.",
                "Connectivity and qualitative headings do not determine spacing, calibrated shape, an infinite repetition law, empty interiors or construction ancestry.",
                "The assignment budget bounds attempted role extensions. Inventory induction, option construction, matching and allocation still incur work outside that budget. Timings are small local diagnostics, not throughput forecasts.",
                "This is one layer of composition over motif occurrences, not arbitrary recursive concept discovery. No new kernel primitive or numeric geometric measurement is introduced."
            } };
        var json = JsonSerializer.Serialize(model, new JsonSerializerOptions(JsonSerializerDefaults.Web) { WriteIndented = true });
        File.WriteAllText(Path.Combine(output, "diagnostics.json"), json);
        File.WriteAllText(Path.Combine(output, "gallery.html"), Html(json));
        foreach (var episode in study.Episodes)
            Console.WriteLine($"{episode.Case.Id}: {episode.Stages[^1].Compatible.Count} compatible; " +
                string.Join(", ", episode.Reads.Select(read => read.Question.Name + " = " + Answer(read.Source))));
        Console.WriteLine($"{study.Episodes.Count} cases; {native.Length:N0} native participants; zero measured numeric participants; native archive restored.");
        Console.WriteLine(Path.GetFullPath(output)); return 0;
    }

    private static string Answer(OccurrenceComposition.Assessment read) => OccurrenceCompositionStudy.Answer(read);
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
    private static IEnumerable<IElement> Reachable(IElement root)
    {
        var seen = new HashSet<IElement>(ReferenceEqualityComparer.Instance); var pending = new Stack<IElement>(); pending.Push(root);
        while (pending.TryPop(out var element)) if (seen.Add(element))
        { yield return element; if (element is Relation relation) { pending.Push(relation.Left); pending.Push(relation.Right); } }
    }
}
