using System.Diagnostics;
using System.IO.Compression;
using System.Text.Json;
using Gojo.Core;
using Gojo.Encoding.Structures;
using Gojo.Experiments.QualitativeExecution;

namespace Gojo.Experiments.QualitativeFrieze;

/// <summary>Inspectable realization of native retained-reference reads.
/// Diagrams and diagnostic counts are produced after the actual matching.</summary>
public static partial class RetainedMatchExperiment
{
    public static int Run(string[] args)
    {
        var output = Path.Combine("experiments", "QualitativeFrieze", "results", "retained-match");
        for (var i = 0; i < args.Length; i++)
            if (args[i] == "--output" && i + 1 < args.Length) output = args[++i];
            else throw new ArgumentException("Usage: --retained-match [--output DIRECTORY]");
        Directory.CreateDirectory(output);
        var watch = Stopwatch.StartNew(); var study = RetainedMatchStudy.Build(); watch.Stop();
        object Work(OccurrenceComposition.Assessment read) => new {
            Assignments = E(read.Consumed).Length, Mappings = E(read.CorrespondenceConsumed).Length };
        var cases = study.Episodes.Select(episode =>
        {
            var example = episode.Case;
            var sourceImage = example.Id + "-source.svg"; var focusedImage = example.Id + "-retained.svg";
            File.WriteAllText(Path.Combine(output, sourceImage), StructuralEvidenceExperiment.RenderSample(example.Source, example.Name));
            File.WriteAllText(Path.Combine(output, focusedImage), StructuralEvidenceExperiment.RenderSample(example.Source,
                example.FocusLabel, episode.SelectedSegments));
            return new { example.Id, example.Name, example.Description, example.FocusLabel, SourceImage = sourceImage,
                FocusedImage = focusedImage, InitialQuestion = example.InitialQuestion.Name,
                FollowupQuestion = example.FollowupQuestion.Name, InitialAnswer = episode.Initial.Answer,
                FreshAnswer = episode.Fresh.Answer, RetainedAnswer = episode.Followup.Answer, RecheckAnswer = episode.Recheck.Answer,
                RetainedRoles = E(example.RetainedRoles).Length, SelectedEdges = E(episode.SelectedSegments).Length,
                Search = new { Initial = Work(episode.Initial), Fresh = Work(episode.Fresh), Retained = Work(episode.Followup), Recheck = Work(episode.Recheck) } };
        }).ToArray();
        var participants = Reachable(study.Surface).ToArray();
        if (participants.Any(item => item is Carrier or CarrierLocation))
            throw new InvalidOperationException("Retained structural reads cannot acquire measured numeric participants.");
        var archive = GojoStructureCompactJson.Serialize(GojoStructureCompactJson.FromElement("retained-match-v1", study.Surface));
        if (!GojoStructureCompactJson.TryToElement(GojoStructureCompactJson.Deserialize(archive), out var restored) ||
            Reachable(restored.Root).Count() != participants.Length || restored.Root is not Relation restoredStudy ||
            restoredStudy.Right is not Relation { Left: Relation restoredEpisodes })
            throw new InvalidOperationException("The complete retained-reference study must restore.");
        foreach (var item in E(restoredEpisodes).Cast<Relation>())
        {
            var initial = (Relation)item.Right; var captured = (Relation)initial.Right;
            if (!new RetainedMatch((Relation)captured.Left).IsValid)
                throw new InvalidOperationException("Transport must preserve the certified retained-reference bindings.");
        }
        using (var file = File.Create(Path.Combine(output, "matches.gojo.json.gz")))
        using (var zip = new GZipStream(file, CompressionLevel.SmallestSize))
        using (var writer = new StreamWriter(zip)) writer.Write(archive);
        var model = new { Schema = "retained-match-v1", Cases = cases,
            Controls = study.Controls.Select(control => new { control.Name, control.Purpose, Answer = control.Read.Answer,
                Stale = E(control.Read.Stale).Length, Open = E(control.Read.Open).Length, Blocked = E(control.Read.Blocked).Length,
                Assignments = E(control.Read.Consumed).Length, Correspondences = E(control.Read.CorrespondenceConsumed).Length }).ToArray(),
            NativeElements = participants.Length, NativeNumericElements = 0, ArchiveRoundTrip = true,
            ElapsedMilliseconds = watch.Elapsed.TotalMilliseconds,
            Limits = new[] {
                "These are bounded structural reads over supplied native motif templates. Selecting an actual witness is explicit; the fixture does not learn which interpretation to retain.",
                "Retention fixes the selected roles' actual sites, edge footprint and complete pattern-site correspondence. Unretained roles remain available for another matching partner.",
                "An unrestricted Yes can use another occurrence. A retained No answers about the remembered context; it neither erases that match nor authorizes substitution.",
                "A site between path endpoints is an ordered topological relation. No equal subdivision, length or calibration is required by these questions.",
                "The reference is scoped to one published observation event. A new event remains Open until an explicit correspondence or transfer establishes continuity.",
                "The retained object is a certified interpretation of exposed incidence and headings. It need not recover the author's drawn path grouping when several native walks are valid.",
                "The memory retains its complete proof history. Compact adaptive memory, event tracking, arbitrary pattern discovery and learned selection remain separate work.",
                "Motif matching and correspondence preparation remain eager and bounded. Fewer admissible matches do not by themselves establish a source-side computation saving."
            } };
        var json = JsonSerializer.Serialize(model, new JsonSerializerOptions(JsonSerializerDefaults.Web) { WriteIndented = true });
        File.WriteAllText(Path.Combine(output, "diagnostics.json"), json);
        File.WriteAllText(Path.Combine(output, "gallery.html"), Html(json));
        foreach (var episode in study.Episodes)
            Console.WriteLine($"{episode.Case.Id}: unrestricted {episode.Fresh.Answer}; retained {episode.Followup.Answer}; original condition {episode.Recheck.Answer}.");
        Console.WriteLine($"{study.Episodes.Count} cases; {participants.Length:N0} native participants; zero measured numeric participants; native references restored.");
        Console.WriteLine(Path.GetFullPath(output)); return 0;
    }

    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
    private static IEnumerable<IElement> Reachable(IElement root)
    {
        var seen = new HashSet<IElement>(ReferenceEqualityComparer.Instance); var pending = new Stack<IElement>(); pending.Push(root);
        while (pending.TryPop(out var item)) if (seen.Add(item))
        { yield return item; if (item is Relation relation) { pending.Push(relation.Left); pending.Push(relation.Right); } }
    }
}
