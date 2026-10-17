using Gojo.Core;
using Gojo.Structures;
using Minds.Predictions;

namespace Minds.Tests;

public sealed class PredictionTests
{
    public static IEnumerable<object[]> DevelopmentCases => PredictionProtocol.Cases.Where(c=>c.Split=="development").Select(c=>new object[]{c.Id});

    [Theory]
    [MemberData(nameof(DevelopmentCases))]
    public void DevelopmentTracesMatchIndependentExactReference(string id)
    {
        var c=PredictionProtocol.Cases.Single(c=>c.Id==id);
        var run=PredictionStudy.Run(c.Samples);
        Assert.Empty(PredictionEvaluation.Compare(run.Rows,PredictionEvaluation.Reference(c.Samples)));
        Assert.True(TimeStepSequence.TryRead(run.Timeline,out _,out _,out var steps));
        var calls=run.Rows[^1].Calls;
        Assert.Equal(calls.Record+calls.Fit+calls.Issue+calls.Assess,steps.Length);
        foreach(var r in run.Rows.Where(r=>r.Prediction is not null))
        {
            Assert.True(r.IssueEvent<r.ArrivalEvent);
            Assert.True(r.ArrivalEvent<r.AssessmentEvent);
            Assert.All(r.TrainingTimes,t=>Assert.True(t<r.Target));
        }
    }

    [Fact]
    public void ChangingFutureValuesCannotChangeAnIssuedPredictionOrAnEarlierPrefix()
    {
        Sample[] a=[new(0,10),new(2,14),new(5,20),new(7,24)];
        Sample[] b=[new(0,10),new(2,14),new(5,-100),new(7,1000)];
        var first=PredictionStudy.Run(a).Rows; var second=PredictionStudy.Run(b).Rows;
        Assert.Equal(first.Take(2).Select(r=>System.Text.Json.JsonSerializer.Serialize(r)),second.Take(2).Select(r=>System.Text.Json.JsonSerializer.Serialize(r)));
        Assert.Equal("20",first[2].Prediction); Assert.Equal(first[2].Prediction,second[2].Prediction);
        Assert.NotEqual(first[2].Error,second[2].Error);
    }

    [Fact]
    public void ForecastUsesElapsedSourceTimeAndKeepsAnExactFraction()
    {
        var run=PredictionStudy.Run([new(1,2),new(4,3),new(6,5)]);
        Assert.Equal("11/3",run.Rows[2].Prediction);
        Assert.Equal("4/3",run.Rows[2].Error);
    }

    [Fact]
    public void MissingOutcomesStayOpenAndDoNotTrainTheNextLine()
    {
        var run=PredictionStudy.Run([new(0,2),new(1,5),new(2,null),new(3,11)]);
        Assert.False(run.Rows[2].AssessmentAccepted); Assert.Null(run.Rows[2].Error);
        Assert.Equal(new long[]{0,1},run.Rows[3].TrainingTimes);
        Assert.Equal("11",run.Rows[3].Prediction); Assert.Equal(2,run.Rows[3].SampleAge);
        Assert.Equal(new[]{4,2,1},run.Rows[3].Retained);
        var receipts=Fields(Fields(run.State)[2]);
        Assert.IsType<Placeholder>(ForecastSpace.Observation(receipts[2])[2]);
        Assert.Single(Fields(Fields(run.State)[4]));
    }

    [Fact]
    public void FlatFramesKeepTimeResidueEvenWhenThePredictionIsCorrect()
    {
        var run=PredictionStudy.Run([new(0,-7),new(1,-7),new(2,-7)]);
        var row=run.Rows[^1]; Assert.Equal("-7",row.Prediction); Assert.Equal("0",row.Error); Assert.True(row.TimeResidue);
        var issue=Fields(Fields(run.State)[3])[0];
        Assert.NotEmpty(Fields(Fields(issue)[3]));
        Assert.IsType<Placeholder>(Fields(Fields(issue)[2])[5]);
    }

    [Fact]
    public void ANewFitAndItsSuccessCannotRewriteAnEarlierMiss()
    {
        var space=new ForecastSpace(); space.Record(0,1); space.Record(1,4);
        var old=space.Predict(2); Assert.Equal("7",old.Prediction);
        var before=space.State;
        var receipt=space.Record(2,5); var assessment=space.Assess(old.Witness!,receipt);
        Assert.Equal("-2",assessment.Error);
        var revised=space.Predict(3); Assert.Equal("6",revised.Prediction);
        Assert.Equal("7",ForecastSpace.Number(Fields(Fields(old.Witness!)[2])[4]));
        Assert.Empty(Fields(Fields(before)[4]));
        Assert.Single(Fields(Fields(space.State)[4]));
        Assert.Equal(2,Fields(Fields(space.State)[3]).Length);
    }

    [Fact]
    public void MissingHistoryNeverBecomesAZeroPrediction()
    {
        Sample[] tape=[new(0,null),new(1,0),new(2,null)];
        var run=PredictionStudy.Run(tape);
        Assert.All(run.Rows,r=>Assert.Null(r.Prediction));
        var reference=PredictionEvaluation.Reference(tape);
        Assert.Null(reference[1].HoldLast); Assert.Equal("0",reference[2].HoldLast);
        var summary=PredictionEvaluation.Summarize(run.Rows,reference);
        Assert.Equal(0,summary.CommonTargets); Assert.Null(summary.AffineCommon.MeanAbsolute);
        Assert.Null(summary.NoPredictionError); Assert.Equal(0,summary.NoPredictionIssued);
    }

    [Fact]
    public void AbsoluteErrorsDoNotCancelAndEmptyScoresAreNotPerfect()
    {
        var score=PredictionEvaluation.Score(["-3","3"]);
        Assert.Equal("3",score.MeanAbsolute); Assert.Equal(0,score.ExactHits);
        Assert.Null(PredictionEvaluation.Score([]).MeanAbsolute);
    }

    [Fact]
    public void HoldLastComparisonUsesCommonTargetsWhileRetainingExtraCoverage()
    {
        Sample[] tape=[new(0,2),new(1,4),new(2,6),new(3,null)];
        var run=PredictionStudy.Run(tape); var reference=PredictionEvaluation.Reference(tape);
        var summary=PredictionEvaluation.Summarize(run.Rows,reference);
        Assert.Equal(2,summary.Issued); Assert.Equal(3,summary.HoldIssued);
        Assert.Equal(1,summary.CommonTargets); Assert.Equal(2,summary.HoldScored);
        Assert.Equal(1,summary.AwaitingOutcome);
        Assert.Equal("0",summary.AffineCommon.MeanAbsolute); Assert.Equal("2",summary.HoldCommon.MeanAbsolute);
    }

    [Fact]
    public void DiagnosticSinkSeesIssuedValueBeforeOutcomeAndCannotChangeTheRun()
    {
        Sample[] tape=[new(0,1),new(1,2),new(2,4)];
        var log=new List<string>();
        var first=PredictionStudy.Run(tape,e=>log.Add(System.Text.Json.JsonSerializer.Serialize(e)));
        var second=PredictionStudy.Run(tape);
        Assert.Equal(System.Text.Json.JsonSerializer.Serialize(first.Rows),System.Text.Json.JsonSerializer.Serialize(second.Rows));
        Assert.Equal(6,log.Count); Assert.Contains("before-observation",log[4]);
        Assert.DoesNotContain("Outcome",log[4]); Assert.Contains("after-observation",log[5]);
    }

    [Fact]
    public void ComparisonDetectsDeliberatelyCorruptedEvidence()
    {
        Sample[] tape=[new(0,1),new(1,2),new(2,3)];
        var rows=PredictionStudy.Run(tape).Rows.ToArray(); rows[2]=rows[2] with { Prediction="999" };
        var diff=Assert.Single(PredictionEvaluation.Compare(rows,PredictionEvaluation.Reference(tape)));
        Assert.Equal("Prediction",diff.Field); Assert.Equal(2,diff.Index);
    }

    private static IElement[] Fields(IElement element)
    { Assert.True(Assert.IsType<Relation>(element).TryReadOrderedGroup(out var f)); return f; }
}
