using Gojo.Experiments.OrnamentFlowPilot;
using Gojo.Experiments.OrnamentPathCampaign;
using Gojo.Experiments.PrimitiveMeasure;

namespace Gojo.Tests.OrnamentTension;

public class FlowPilotTests
{
    [Fact]
    public void ReachingBaselineOnFirstDropSkipsAllCorrectionTicksThroughThreeFrameChanges()
    {
        var run = FlowFret.Run(2, 2, true, bays: 4, alternateAtSeam: true);
        Assert.True(run.Completed);
        Assert.Equal(new PathSite[] { new(2, 0), new(4, 0), new(6, 0), new(8, 0) }, run.BayEnds);
        for (int bay = 0; bay < 4; bay++)
        {
            var strokes = run.Trace.Edges.Where(e => e.Cell == bay).ToArray();
            Assert.Equal(5, strokes.Length);
            Assert.Equal((bay % 2 == 0 ? 2 : -2), run.Trace.Sites[strokes[0].To].Y);
        }
    }

    [Fact]
    public void RestAndUnknownPulsesCannotConsumeTheOnlySpot()
    {
        var input = new ParticipationStrip(0, 1);
        var primary = new FlowPen();
        var spot = new SingleSpotPen(new PathSite(3, 3), primary.MarkPort);
        foreach (var gate in new[] { input.Expose(0), input.Expose(1, visible: false), input.Expose(1) })
        {
            Assert.False(spot.Step().DrewSpot);
            primary.Touch(gate, new PathSite(1, 0), () => 1);
            Assert.False(spot.Stopped);
        }
        Assert.True(spot.Step().DrewSpot);
        primary.Touch(input.Expose(1), new PathSite(1, 0), () => 1);
        Assert.False(spot.Step().DrewSpot);
        Assert.True(spot.Stopped);
    }
}
