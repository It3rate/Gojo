using Gojo.Experiments.OrnamentPathCampaign;

namespace Gojo.Experiments.OrnamentPathCampaign.Tests;

public sealed class PathJudgeTests
{
    [Fact]
    public void EveryIndependentChallengeMeetsItsDeclaredRelation()
    {
        var challenges=JudgeChallenge.Run();
        Assert.True(challenges.Length>=16);
        Assert.All(challenges,c=>Assert.True(c.Pass,$"{c.Example} / {c.Criterion}: {c.A} vs {c.B}"));
        var overlap=Assert.Single(challenges,c=>c.Criterion=="no-overdraw" && c.Expectation=="A > B");
        Assert.Equal(5.0/6,overlap.B!.Value,11);
    }

    [Fact]
    public void EmptyDrawingsHaveNoObservations()
    {
        var scores=PathJudge.Measure(new([],[],[],4,2,1));
        Assert.Equal(PathJudge.Criteria.Length,scores.Length);
        Assert.All(scores,x=>Assert.True(double.IsNaN(x)));
        Assert.Equal(PathJudge.Criteria.Length, PathJudge.Criteria.DistinctBy(x=>x.Key).Count());
    }
}
