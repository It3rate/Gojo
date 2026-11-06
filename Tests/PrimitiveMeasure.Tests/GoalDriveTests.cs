using Gojo.Experiments.PrimitiveMeasure;

namespace Gojo.Experiments.PrimitiveMeasure.Tests;

public sealed class GoalDriveTests
{
    private const byte Off = 1, On = 2, Unknown = 3, Missing = 0;

    [Fact]
    public void FinishingThroughASecondPassageWithdrawsDemandBeforeParkedWorkCanBeSpent()
    {
        var trace = GoalDriveExperiments.Run(9).Timeline;
        var crossing = Assert.Single(trace, frame => frame.ExternalRequest == On);
        Assert.Equal((On, On, On, Off, Off), (crossing.Target, crossing.Finished, crossing.Pending, crossing.Spent, crossing.Gap));
        var reopened = trace[^1];
        Assert.Equal((On, Off, On, Off), (reopened.Access, reopened.Gap, reopened.Pending, reopened.Spent));
    }

    [Fact]
    public void The_same_observed_gap_requires_response_rows_to_move_available_work()
    {
        var passive = GoalDriveExperiments.Run(0);
        var local = GoalDriveExperiments.Run(2);
        Assert.Equal(passive.Timeline[1], local.Timeline[1]);
        Assert.Equal(On, local.Timeline[1].Gap);
        Assert.Equal(On, local.Timeline[1].Fuel);
        Assert.Empty(passive.WorkRows);
        Assert.NotEmpty(local.WorkRows);
        Assert.Equal(passive.BodyRows, local.BodyRows);
        Assert.Equal((On, Off, Off, Off), WorkAndFinish(passive.Timeline[^1]));
        Assert.Equal((Off, Off, On, On), WorkAndFinish(local.Timeline[^1]));
    }

    [Fact]
    public void A_funded_request_waits_across_blocked_beats_and_retires_on_arrival()
    {
        var trace = GoalDriveExperiments.Run(2).Timeline;
        Assert.All(trace, frame => Assert.Equal(Off, frame.ExternalRequest));
        Assert.Equal((Off, On, Off, Off), WorkAndFinish(trace[2]));
        Assert.Equal(WorkAndFinish(trace[2]), WorkAndFinish(trace[3]));
        Assert.Equal(On, trace[3].Gap);
        Assert.Equal((Off, Off, On, On), WorkAndFinish(trace[4]));
        Assert.Equal(Off, trace[4].Gap);
        Assert.Equal(WorkAndFinish(trace[4]), WorkAndFinish(trace[5]));
    }

    [Fact]
    public void A_released_helper_permits_but_cannot_initiate_the_runners_next_attempt()
    {
        var trace = GoalDriveExperiments.Run(1).Timeline;
        Assert.Equal((On, Off, Off), (trace[2].ExternalRequest, trace[2].HelpReleased, trace[2].Finished));
        Assert.Equal((Off, On, Off), (trace[3].ExternalRequest, trace[3].HelpReleased, trace[3].Finished));
        Assert.Equal((Off, On, Off), (trace[4].ExternalRequest, trace[4].HelpReleased, trace[4].Finished));
        Assert.Equal((On, On, On), (trace[5].ExternalRequest, trace[5].HelpReleased, trace[5].Finished));
    }

    [Fact]
    public void A_withdrawn_target_holds_parked_work_through_an_open_opportunity()
    {
        var trace = GoalDriveExperiments.Run(7).Timeline;
        var parked = trace.Single(frame => frame.Access == On && frame.Target == Off);
        Assert.Equal((Off, On, Off, Off), WorkAndFinish(parked));
        Assert.Equal(Off, parked.Gap);
        Assert.Equal(On, trace[^1].Target);
        Assert.Equal((Off, Off, On, On), WorkAndFinish(trace[^1]));
        Assert.Equal(Off, trace[^1].Gap);
    }

    [Fact]
    public void Closed_access_and_empty_stock_hold_different_local_states()
    {
        var obstructed = GoalDriveExperiments.Run(3).Timeline[^1];
        var unfunded = GoalDriveExperiments.Run(4).Timeline[^1];
        Assert.Equal((On, Off, On), (obstructed.Gap, obstructed.Access, obstructed.Pending));
        Assert.Equal((On, On, Off), (unfunded.Gap, unfunded.Access, unfunded.Pending));
        Assert.Equal((Off, Off, Off, Off), WorkAndFinish(unfunded));
        Assert.Equal(On, unfunded.OnCourse);
    }

    [Theory]
    [InlineData(5, Missing)]
    [InlineData(6, Unknown)]
    public void Withholding_actual_finish_evidence_keeps_an_unused_reserve(int scenario, byte reported)
    {
        var last = GoalDriveExperiments.Run(scenario).Timeline[^1];
        Assert.Equal(On, last.Target);
        Assert.Equal(Off, last.Finished);
        Assert.Equal(reported, last.FinishRead);
        Assert.Equal(reported, last.Gap);
        Assert.Equal((On, Off, Off, Off), WorkAndFinish(last));
    }

    [Fact]
    public void Already_satisfied_target_never_demands_work()
    {
        Assert.All(GoalDriveExperiments.Run(8).Timeline, frame =>
        {
            Assert.Equal(Off, frame.Gap);
            Assert.Equal((On, Off, Off, On), WorkAndFinish(frame));
        });
    }

    [Fact]
    public void Body_and_effort_are_separately_accounted_on_every_beat()
    {
        for (var scenario = 0; scenario < GoalDriveExperiments.Names.Length; scenario++)
        foreach (var frame in GoalDriveExperiments.Run(scenario).Timeline)
        {
            Assert.Equal(1, new[] { frame.OnCourse, frame.Finished }.Count(value => value == On));
            var stock = new[] { frame.Fuel, frame.Pending, frame.Spent };
            Assert.Equal(scenario is 1 or 4 ? 0 : 1, stock.Count(value => value == On));
            if (scenario == 1) Assert.All(stock, value => Assert.Equal(Missing, value));
        }
    }

    private static (byte, byte, byte, byte) WorkAndFinish(GoalDriveBeat beat) =>
        (beat.Fuel, beat.Pending, beat.Spent, beat.Finished);
}
