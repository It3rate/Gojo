using Minds.Presentation;
using Minds.TwoSpace;

if (args.Length > 0 && args[0] == "--experiment3")
{
    var studyDirectory = Path.GetFullPath(args.Length > 1 ? args[1] :
        Path.Combine("Minds", "results", "predictions", DateTime.UtcNow.ToString("yyyyMMdd-HHmmss")));
    PredictionsReport.Run(studyDirectory);
    return;
}

if (args.Length > 0 && args[0] == "--experiment2")
{
    var studyDirectory = Path.GetFullPath(args.Length > 1 ? args[1] :
        Path.Combine("Minds", "results", "feedback-limits", DateTime.UtcNow.ToString("yyyyMMdd-HHmmss")));
    FeedbackLimitsReport.Run(studyDirectory);
    return;
}

var directory = Path.GetFullPath(args.Length == 0 ? Path.Combine("Minds", "results", "two-space") : args[0]);
var runs = FeedbackExperiment.Cases.Select(condition => FeedbackExperiment.Run(condition)).ToArray();
FeedbackReport.Write(directory, runs);
Console.WriteLine(File.ReadAllText(Path.Combine(directory, "Results.md")));
Console.WriteLine($"Step viewer: {Path.Combine(directory, "index.html")}");
