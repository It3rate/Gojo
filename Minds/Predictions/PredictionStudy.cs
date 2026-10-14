using Gojo.Core;

namespace Minds.Predictions;

public sealed record WorkCounts(int Record, int Fit, int Issue, int Assess);
public sealed record PredictionRow(long Target, long? Outcome, string Status, long[] TrainingTimes,
    string? Prediction, long? IssueEvent, long ArrivalEvent, long? AssessmentEvent,
    bool? AssessmentAccepted, string? Error, bool TimeResidue, long? SampleAge,
    int[] Retained, WorkCounts Calls);
public sealed record PredictionRun(IReadOnlyList<PredictionRow> Rows, Relation State, Relation Timeline);

public static class PredictionStudy
{
    public static PredictionRun Run(IReadOnlyList<Sample> tape, Action<object>? record = null)
    {
        var space = new ForecastSpace(); var rows = new List<PredictionRow>();
        foreach (var sample in tape)
        {
            var attempt = space.Predict(sample.Time);
            // Written before Record admits this target. The diagnostic sink never
            // returns participant input and receives no live structural objects.
            record?.Invoke(new { phase = "before-observation", target = sample.Time, attempt.Status,
                attempt.TrainingTimes, attempt.Prediction, attempt.IssueEvent, attempt.HasTimeResidue });
            var receipt = space.Record(sample.Time,sample.Value);
            var assessment = attempt.Status == "issued" ? space.Assess(attempt.Witness!,receipt) : null;
            var row = new PredictionRow(sample.Time,sample.Value,attempt.Status,attempt.TrainingTimes,
                attempt.Prediction,attempt.IssueEvent,ForecastSpace.Arrival(receipt),assessment?.LocalEvent,
                assessment?.Accepted,assessment?.Error,attempt.HasTimeResidue,
                attempt.TrainingTimes.Length == 0 ? null : sample.Time-attempt.TrainingTimes[^1],
                space.Counts(),new(space.RecordCalls,space.FitCalls,space.IssueCalls,space.AssessmentCalls));
            rows.Add(row); record?.Invoke(new { phase = "after-observation", row });
        }
        return new(rows,space.State,space.Timeline);
    }
}
