using System.Security.Cryptography;
using System.Text.Json;
using Minds.FeedbackLimits;

namespace Minds.Presentation;

public sealed record CompletedStudy(StudyCase Case, int Repetition, StudyTrace Gojo,
    StudyTrace Reference, IReadOnlyList<StudyMismatch> Mismatches, string TraceHash);
public sealed record FailedStudy(string Case, int Repetition, string Error);

public static class FeedbackLimitsReport
{
    private static readonly JsonSerializerOptions Json = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    public static void Run(string directory)
    {
        if (Directory.Exists(directory) && Directory.EnumerateFileSystemEntries(directory).Any())
            throw new IOException("Use a new output directory to retain earlier experiment records: " + directory);
        Directory.CreateDirectory(directory); Directory.CreateDirectory(Path.Combine(directory, "runs"));
        Directory.CreateDirectory(Path.Combine(directory, "fixtures"));
        WriteJson("cases.json", new { version = StudyProtocol.Version, repetitions = StudyProtocol.Repetitions, cases = StudyProtocol.Cases });
        File.Copy(Path.Combine(AppContext.BaseDirectory, "docs", "FeedbackLimitsProtocol.md"), Path.Combine(directory, "Protocol.md"));
        var completed = new List<CompletedStudy>(); var failures = new List<FailedStudy>();
        foreach (var c in StudyProtocol.Cases)
        {
            var tape = StudyProtocol.Tape(c);
            File.WriteAllText(Path.Combine(directory, "fixtures", c.Id + ".gojo"), StudyProtocol.Fixture(c));
            WriteJson("runs/" + c.Id + "-tape.json", tape);
            for (var repetition = 1; repetition <= StudyProtocol.Repetitions; repetition++)
            {
                try
                {
                    var actual = GojoStudy.Run(c, tape);
                    var reference = ConventionalReference.Run(c, tape);
                    var comparison = StudyComparison.Compare(actual.Trace, reference);
                    var bytes = JsonSerializer.SerializeToUtf8Bytes(actual.Trace, Json);
                    var run = new CompletedStudy(c, repetition, actual.Trace, reference, comparison, Convert.ToHexString(SHA256.HashData(bytes)));
                    completed.Add(run); WriteJson($"runs/{c.Id}-{repetition}.json", run);
                    Console.WriteLine($"{c.Id} repeat {repetition}: A={actual.Trace.Rounds[^1].Values["a"]}, B={actual.Trace.Rounds[^1].Values["b"]}, mismatches={comparison.Count}");
                }
                catch (Exception error) when (error is not OutOfMemoryException)
                {
                    failures.Add(new FailedStudy(c.Id, repetition, error.ToString()));
                    WriteJson("failures.json", failures);
                    Console.WriteLine($"APPARATUS FAILURE {c.Id} repeat {repetition}: {error.Message}");
                }
            }
        }
        var summaries = StudyProtocol.Cases.Select(c => {
            var matching = completed.Where(r => r.Case.Id == c.Id).ToArray();
            return new { c.Id, c.Name, c.Factor, completed = matching.Length,
                distinctTraces = matching.Select(r => r.TraceHash).Distinct().Count(),
                mismatches = matching.Sum(r => r.Mismatches.Count),
                failures = failures.Count(r => r.Case == c.Id),
                final = matching.FirstOrDefault()?.Gojo.Rounds[^1].Values };
        }).ToArray();
        WriteJson("summary.json", new { protocol = StudyProtocol.Version, summaries, failures });
        // One copy of each distinct trajectory is enough for the viewer. Every
        // repetition and the independent reference are still downloadable above.
        var views = completed.GroupBy(r => r.Case.Id).SelectMany(g => g.GroupBy(r => r.TraceHash).Select(v => v.First()))
            .Select(r => new { r.Case, r.Repetition, r.Gojo, mismatchCount = r.Mismatches.Count }).ToArray();
        var payload = JsonSerializer.Serialize(new { protocol = StudyProtocol.Version, summaries, views, failures }, Json);
        var template = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Presentation", "feedback-limits.html"));
        File.WriteAllText(Path.Combine(directory, "index.html"), template.Replace("__STUDY_JSON__", payload));
        var rows = summaries.Select(s => s.final is null ? $"| {s.Name} | unavailable | | | | {s.failures} apparatus failures |" :
            $"| {s.Name} | {s.final["a"]} | {s.final["b"]} | {s.final["gap"]} | {s.final["movement"]} | {s.completed} runs; {s.distinctTraces} distinct; {s.mismatches} mismatches |");
        File.WriteAllText(Path.Combine(directory, "Results.md"),
            $"# Experiment 02: feedback limits\n\nProtocol `{StudyProtocol.Version}`. {completed.Count} completed runs, {failures.Count} apparatus failures. " +
            $"{completed.Sum(r => r.Mismatches.Count)} event/round field disagreements with the independent exact reference.\n\n" +
            "All values below use A units; movement is the sum of absolute changes since the preceding paired observation. It is not physical energy. " +
            "Each row shows the first completed run. Distinct-trace counts compare recorded observations, not complete restorable graphs.\n\n" +
            "| Condition | Final A | Final B | B minus A | Last-round movement | Comparison |\n| --- | ---: | ---: | ---: | ---: | --- |\n" + string.Join("\n", rows) +
            "\n\nTwelve rounds end observation. Equal values, quiet estimates, unresolved requests and controller limits are separate findings. " +
            "Ordinary exact arithmetic retains the same fractional requests, copied messages and counters. A match establishes selected contract agreement, not a Gojo performance advantage.\n\n" +
            "[Viewer](index.html) · [Selected rules](Protocol.md) · [All conditions and parameters](cases.json) · [Summary and failures](summary.json). " +
            "Individual run files retain both implementations; the viewer includes every distinct recorded trajectory.\n");
        Console.WriteLine($"Completed {completed.Count}; failures {failures.Count}; mismatches {completed.Sum(r => r.Mismatches.Count)}. Viewer: {Path.Combine(directory, "index.html")}");
        void WriteJson(string file, object value) => File.WriteAllText(Path.Combine(directory, file), JsonSerializer.Serialize(value, Json));
    }
}
