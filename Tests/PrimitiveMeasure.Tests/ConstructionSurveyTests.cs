using Gojo.Experiments.PrimitiveMeasure;

namespace Gojo.Experiments.PrimitiveMeasure.Tests;

public sealed class ConstructionSurveyTests
{
    private static readonly ConstructionObservation[] Observations = ConstructionSurvey.Run();

    [Fact]
    public void EveryConstructionIsObservedInEveryVisibilityAndViewAndRetainsItself()
    {
        Assert.Equal(310, Observations.Length);
        for (var source = 0; source < ConstructionAtlas.All.Count; source++)
            foreach (var reverse in new[] { false, true })
            {
                var rows = Observations.Where(row => row.Source == source && row.Reverse == reverse).ToArray();
                Assert.Equal(Enum.GetValues<ConstructionVisibility>(), rows.Select(row => row.Visibility));
                Assert.All(rows, row => Assert.True(row.RetainsSource));
            }
    }

    [Fact]
    public void HidingCluesCannotRemoveCandidatesAndConnectionsOnlyKeepsTheBroadestSet()
    {
        for (var source = 0; source < ConstructionAtlas.All.Count; source++)
            foreach (var reverse in new[] { false, true })
            {
                var rows = Observations.Where(row => row.Source == source && row.Reverse == reverse).ToArray();
                var full = rows.Single(row => row.Visibility == ConstructionVisibility.All);
                var topology = rows.Single(row => row.Visibility == ConstructionVisibility.ConnectionsOnly);
                Assert.Equal(5, topology.KnownCount);
                foreach (var row in rows)
                {
                    Assert.True(full.Candidates.All(row.Candidates.Contains));
                    Assert.True(row.Candidates.All(topology.Candidates.Contains));
                    Assert.InRange(row.KnownCount, topology.KnownCount, full.KnownCount);
                }
            }
    }

    [Fact]
    public void RenamingAndMovingTheDrawingsDoesNotChangeAnySurveyCandidates()
    {
        var renamed = ConstructionAtlas.All.Select((example, i) => example with
        {
            Pattern = "Other caption " + i,
            Method = "Different story",
            Description = "No construction instruction",
            Points = example.Points.Select(p => new PenPoint(p.X * -17 + i, p.Y + 1000)).ToArray()
        }).ToArray();
        var reread = ConstructionSurvey.Run(renamed);
        for (var i = 0; i < Observations.Length; i++)
        {
            Assert.Equal(Observations[i].Clues.Values, reread[i].Clues.Values);
            Assert.Equal(Observations[i].Candidates, reread[i].Candidates);
        }
    }
}
