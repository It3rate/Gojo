using System.Diagnostics;
using System.IO.Compression;
using System.Text.Json;
using Gojo.Core;
using Gojo.Encoding.Structures;
using E = Gojo.Experiments.QualitativeFrieze.StructuralEvidenceRules;
using P = Gojo.Experiments.QualitativeFrieze.StructuralPotential;

namespace Gojo.Experiments.QualitativeFrieze;

public static class StructuralPotentialExperiment
{
    public static int Run(string[] args)
    {
        var output = Path.Combine("experiments", "QualitativeFrieze", "results", "structural-potential");
        for (var i = 0; i < args.Length; i++)
            if (args[i] == "--output" && i + 1 < args.Length) output = args[++i];
            else throw new ArgumentException("Usage: --structural-potential [--output DIRECTORY]");
        Directory.CreateDirectory(output);
        var watch = Stopwatch.StartNew();
        var cases = StructuralPotentialStudy.Cases();
        var archive = new List<IElement>();
        var reads = new List<object>();
        var sourceAnswers = new Dictionary<string, int>();
        var completionAnswers = new Dictionary<string, int>();
        var samples = 0; var transported = 0;
        foreach (var item in cases)
        {
            var frame = item.Frame;
            // Transfer laws, publication and question aliases in one independently
            // restored world. Labels and helper instance identities are absent.
            var world = Restore(new(new Relation(frame.Laws.Surface, item.Source.Surface),
                P.S(frame.Goals.Select(goal => (IElement)goal.Predicate).ToArray())));
            var sources = (Relation)world.Left;
            var laws = new E.Program((Relation)sources.Left);
            var source = new E.Publication((Relation)sources.Right);
            var goals = P.Items((Relation)world.Right).Cast<Relation>().ToArray();
            archive.Add(frame.Surface); archive.Add(item.Source.Surface);
            for (var i = 0; i < frame.Goals.Count; i++)
            {
                var goal = frame.Goals[i]; var read = E.Read(frame.Laws, item.Source, goal.Predicate);
                var imported = E.Read(laws, source, goals[i]);
                if (imported.Answer != read.Answer || P.Items(imported.Missing).Length != P.Items(read.Missing).Length ||
                    P.Items(imported.Visited).Length != P.Items(read.Visited).Length)
                    throw new InvalidOperationException("A source-free native contract read changed after transport.");
                transported++;
                sourceAnswers[read.Answer] = sourceAnswers.GetValueOrDefault(read.Answer) + 1;
                archive.Add(read.Surface);
                string? completionAnswer = null; var assumptions = 0;
                if (i >= 3)
                {
                    var completion = frame.Complete(item.Source, goal.Predicate, item.AllowedAssumptions);
                    completionAnswer = completion.Answer; assumptions = P.Items(completion.AssumedPredicates).Length;
                    completionAnswers[completion.Answer] = completionAnswers.GetValueOrDefault(completion.Answer) + 1;
                    archive.Add(completion.Surface);
                    if (completion.Sample is { } sample)
                    {
                        if (P.Items(sample.CycleWitness).Length != 1 || !sample.Observation.IsComplete || !sample.Motifs.IsComplete)
                            throw new InvalidOperationException("A conditional sample lacks an independently read boundary circuit.");
                        samples++;
                    }
                    if (E.Read(frame.Laws, item.Source, goal.Predicate).Answer != read.Answer)
                        throw new InvalidOperationException("Sampling changed the source evidence.");
                }
                reads.Add(new { Case = item.Id, Question = goal.Name, Source = read.Answer,
                    Visited = P.Items(read.Visited).Length, Missing = P.Items(read.Missing).Length,
                    Unasked = P.Items(read.Unasked).Length, Completion = completionAnswer, Assumptions = assumptions });
            }
            if (P.Items(frame.SourceObservation.Sites).Length != 8 || P.Items(frame.SourceObservation.Segments).Length != 4)
                throw new InvalidOperationException("An observational completion changed material support incidence.");
        }
        StructuralPotentialGallery.Write(output, cases);
        var summary = new { Cases = cases.Count, SourceReads = reads.Count, TransportChecks = transported,
            SourceAnswers = sourceAnswers, CompletionAttempts = completionAnswers.Values.Sum(), CompletionAnswers = completionAnswers,
            BoundarySamples = samples, Seconds = watch.Elapsed.TotalSeconds, Reads = reads };
        File.WriteAllText(Path.Combine(output, "summary.json"), JsonSerializer.Serialize(summary, new JsonSerializerOptions { WriteIndented = true }));
        var packet = GojoStructureCompactJson.Serialize(GojoStructureCompactJson.FromElement("structural-potential-v1", P.S(archive.ToArray())));
        using (var file = File.Create(Path.Combine(output, "evidence.gojo.json.gz")))
        using (var zip = new GZipStream(file, CompressionLevel.SmallestSize))
        using (var writer = new StreamWriter(zip)) writer.Write(packet);
        Console.WriteLine($"Completed {reads.Count} scoped source reads, {transported} transport checks, and {completionAnswers.Values.Sum()} completion attempts; {samples} independently read boundary samples.");
        Console.WriteLine($"Elapsed {watch.Elapsed.TotalSeconds:0.00}s. {Path.GetFullPath(output)}");
        return 0;
    }

    private static Relation Restore(Relation source)
    {
        var text = GojoStructureCompactJson.Serialize(GojoStructureCompactJson.FromElement("potential-laws-publication-questions", source));
        if (!GojoStructureCompactJson.TryToElement(GojoStructureCompactJson.Deserialize(text), out var imported) || imported.Root is not Relation root)
            throw new InvalidOperationException("Native construction evidence failed transport.");
        return root;
    }
}
