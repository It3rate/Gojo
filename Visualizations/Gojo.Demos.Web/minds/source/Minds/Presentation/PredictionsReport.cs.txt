using System.Security.Cryptography;
using System.Text.Json;
using Minds.Predictions;

namespace Minds.Presentation;

public sealed record CompletedPrediction(PredictionCase Case, IReadOnlyList<PredictionRow> Rows,
    IReadOnlyList<ReferenceRow> Reference, PredictionSummary Summary, IReadOnlyList<PredictionDifference> Differences);

public static class PredictionsReport
{
    private static readonly JsonSerializerOptions Json = new() { PropertyNamingPolicy=JsonNamingPolicy.CamelCase };
    public static void Run(string directory)
    {
        if(Directory.Exists(directory) && Directory.EnumerateFileSystemEntries(directory).Any())
            throw new IOException("Use a new output directory to retain earlier results: "+directory);
        Directory.CreateDirectory(directory); Directory.CreateDirectory(Path.Combine(directory,"runs"));
        var protocol=File.ReadAllText(Path.Combine(AppContext.BaseDirectory,"docs","PredictionProtocol.md"));
        File.WriteAllText(Path.Combine(directory,"Protocol.md"),protocol);
        File.WriteAllText(Path.Combine(directory,"fixture.gojo"),PredictionProtocol.Fixture());
        Write("cases.json",PredictionProtocol.Cases);
        Write("protocol-record.json",new { version=PredictionProtocol.Version, capturedBeforeRuns=DateTime.UtcNow,
            protocolSha256=Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(protocol))) });
        var completed=new List<CompletedPrediction>(); var failures=new List<object>();
        foreach(var c in PredictionProtocol.Cases)
        {
            using var journal=new StreamWriter(Path.Combine(directory,"runs",c.Id+"-events.jsonl")){AutoFlush=true};
            try
            {
                var run=PredictionStudy.Run(c.Samples,e=>journal.WriteLine(JsonSerializer.Serialize(e,Json)));
                var reference=PredictionEvaluation.Reference(c.Samples);
                var result=new CompletedPrediction(c,run.Rows,reference,PredictionEvaluation.Summarize(run.Rows,reference),
                    PredictionEvaluation.Compare(run.Rows,reference));
                completed.Add(result); Write("runs/"+c.Id+".json",result);
                Console.WriteLine($"{c.Id}: common targets {result.Summary.CommonTargets}, line MAE {result.Summary.AffineCommon.MeanAbsolute??"unavailable"}, hold MAE {result.Summary.HoldCommon.MeanAbsolute??"unavailable"}, differences {result.Differences.Count}");
            }
            catch(Exception error) when(error is not OutOfMemoryException)
            {
                failures.Add(new { c.Id, error=error.ToString() }); Write("failures.json",failures);
                Console.WriteLine($"APPARATUS FAILURE {c.Id}: {error.Message}");
            }
        }
        var payload=new { protocol=PredictionProtocol.Version, completed, failures };
        Write("summary.json",new { protocol=PredictionProtocol.Version, cases=completed.Select(r=>new { r.Case.Id,r.Case.Name,r.Case.Split,r.Summary,differences=r.Differences.Count }),failures });
        var template=File.ReadAllText(Path.Combine(AppContext.BaseDirectory,"Presentation","predictions.html"));
        File.WriteAllText(Path.Combine(directory,"index.html"),template.Replace("__PREDICTIONS_JSON__",JsonSerializer.Serialize(payload,Json)));
        var rows=completed.Select(r=>$"| {r.Case.Name} | {r.Case.Split} | {r.Summary.Issued}/{r.Summary.Targets} | {r.Summary.CommonTargets} | {r.Summary.AffineCommon.MeanAbsolute??"unavailable"} | {r.Summary.HoldCommon.MeanAbsolute??"unavailable"} | {r.Summary.AwaitingOutcome} | {r.Differences.Count} |");
        File.WriteAllText(Path.Combine(directory,"Results.md"),
            $"# Experiment 03: predictions before observations\n\nProtocol `{PredictionProtocol.Version}`. {completed.Count} completed cases; {failures.Count} apparatus failures; {completed.Sum(r=>r.Differences.Count)} field differences from the independent exact affine reference.\n\n"+
            "Mean absolute error (MAE) is compared only on shared targets with an observed outcome. Missing targets are not zeros. The external hold-last baseline also has wider coverage after just one known sample; complete counts and errors are in each trace.\n\n"+
            "| Case | Split | Line issued / targets | Common scored | Line MAE | Hold-last MAE | Line awaiting outcome | Exact-model differences |\n| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: |\n"+string.Join("\n",rows)+
            "\n\nNo-prediction coverage is zero in every case; its error is unavailable. Counts measure selected calls and retained records, not CPU or energy. These hand-authored examples do not establish a population-wide winner. No feedback changes the selected model family.\n\n[Viewer](index.html) · [Selected rules](Protocol.md) · [All tapes](cases.json) · [Complete summary](summary.json). Each run includes a before-observation/after-observation event journal.\n");
        Console.WriteLine($"Viewer: {Path.Combine(directory,"index.html")}");
        if(failures.Count!=0 || completed.Any(r=>r.Differences.Count!=0))Environment.ExitCode=1;
        void Write(string file,object value)=>File.WriteAllText(Path.Combine(directory,file),JsonSerializer.Serialize(value,Json));
    }
}
