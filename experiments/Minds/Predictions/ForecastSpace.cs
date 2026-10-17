using System.Numerics;
using Gojo.Core;
using Gojo.Encoding.Syntax;
using Gojo.Patterns;
using Gojo.Structures;
using static Minds.TwoSpace.Graph;

namespace Minds.Predictions;

// A narrow adapter over connected fixture state. The only prediction input is
// the requested time. It has no case/tape, baseline, score or future-value access.
public sealed class ForecastSpace
{
    private readonly IElement[] roots;
    private readonly Relation initial;
    private readonly List<Relation> steps = [];
    private readonly Carrier timelineClock = new();
    private long localEvent;
    public Relation State { get; private set; }
    private IElement[] Stream => F(F(State)[0]);
    public Carrier SourceClock => (Carrier)F(Stream[0])[1];
    public Carrier LocalClock => (Carrier)Stream[3];
    public Carrier Quantity => ((CarrierLocation)R(R(F(Stream[0])[0]).Left).Left).Carrier;
    public int RecordCalls { get; private set; }
    public int FitCalls { get; private set; }
    public int IssueCalls { get; private set; }
    public int AssessmentCalls { get; private set; }
    public Relation Timeline => TimeStepSequence.Create(new Carrier(), timelineClock, initial, steps.ToArray());

    public ForecastSpace()
    {
        roots = GojoSyntax.Parse(PredictionProtocol.Fixture()).Roots.ToArray();
        State = initial = R(roots[0]);
    }

    public ForecastAttempt Predict(long target)
    {
        // Selection is an explicitly authored rule over the local receipt surface.
        var known = F(F(State)[2]).Where(r => Observation(r)[2] is not Placeholder).TakeLast(2).ToArray();
        var times = known.Select(r => ((CarrierLocation)Observation(r)[1]).Ticks).ToArray();
        if (known.Length < 2) return new("insufficient-history", times, null, null, null, false);
        var request = Bind(2, (0,State), (5,known[0]), (6,known[1]), (7,F(roots[1])[0]));
        FitCalls++;
        if (!LocalCalibration.TryFitTemporalFrame(request, out var fit))
        { Retain(State, fit); return new("fit-refused", times, null, null, fit, false); }
        Retain(State, fit);
        var candidate = Bind(3, (8,fit), (9,F(roots[1])[0]));
        var issueTime = localEvent++;
        request = Bind(4, (0,State), (10,candidate), (11,new CarrierLocation(SourceClock,target)),
            (4,new CarrierLocation(LocalClock,issueTime)));
        IssueCalls++;
        var accepted = LocalCalibration.TryIssueForecast(request, out var issue, out var next);
        Retain(next, issue);
        return accepted
            ? new("issued", times, Number(F(F(issue)[2])[4]), issueTime, issue, F(F(issue)[3]).Length != 0)
            : new("issue-refused", times, null, issueTime, issue, false);
    }

    public Relation Record(long sourceTime, long? value)
    {
        IElement sample = value is null ? new Placeholder() : new CarrierLocation(Quantity,value.Value);
        var observation = Bind(0, (1,new CarrierLocation(SourceClock,sourceTime)), (2,sample));
        var request = Bind(1, (0,State), (3,observation), (4,new CarrierLocation(LocalClock,localEvent++)));
        RecordCalls++;
        var ok = LocalCalibration.TryRecordTemporalSample(request, out var receipt, out var next);
        Retain(next, receipt); Require(ok, "record prospective sample"); return receipt;
    }

    public ForecastAssessment Assess(Relation issue, Relation receipt)
    {
        var now = localEvent++;
        var request = Bind(5, (0,State), (12,issue), (13,receipt), (4,new CarrierLocation(LocalClock,now)));
        AssessmentCalls++;
        var ok = LocalCalibration.TryAssessForecast(request, out var assessment, out var next);
        Retain(next, assessment);
        return new(ok, ok ? Number(F(assessment)[2]) : null, now, assessment);
    }

    public static IElement[] Observation(IElement receipt) => F(F(F(receipt)[2])[0]);
    public static long Arrival(IElement receipt) => ((CarrierLocation)F(F(receipt)[2])[1]).Ticks;
    public static string Number(IElement element)
    {
        Require(ProjectionResidue.TryRead(element, out _, out var n, out var d), "exact forecast quantity");
        var gcd = checked((long)BigInteger.GreatestCommonDivisor(n,d)); n /= gcd; d /= gcd;
        return d == 1 ? n.ToString() : $"{n}/{d}";
    }
    public int[] Counts() => [F(F(State)[2]).Length,F(F(State)[3]).Length,F(F(State)[4]).Length];
    private Relation Bind(int pattern, params (int Slot,IElement Value)[] bindings) =>
        R(StructuralPattern.Refine(F(roots[2])[pattern], bindings.Select(b => new Relation(F(roots[3])[b.Slot],b.Value)).ToArray()));
    private void Retain(Relation next, Relation witness)
    {
        // Timeline positions count calls; local availability events stay in witnesses.
        steps.Add(TimeStepSequence.Step(new Carrier(),timelineClock,steps.Count,steps.Count+1,State,witness,next,F(witness)[3]));
        State = next;
    }
}

public sealed record ForecastAttempt(string Status, long[] TrainingTimes, string? Prediction, long? IssueEvent, Relation? Witness, bool HasTimeResidue);
public sealed record ForecastAssessment(bool Accepted, string? Error, long LocalEvent, Relation Witness);
