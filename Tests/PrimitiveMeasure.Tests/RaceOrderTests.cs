using Gojo.Experiments.PrimitiveMeasure;

namespace Gojo.Experiments.PrimitiveMeasure.Tests;

public sealed class RaceOrderTests
{
    // The order ports admit A first, B first, or a shared finish event.
    private const byte AFirst = 1;
    private const byte BFirst = 2;
    private const byte SharedFinish = 4;

    [Fact]
    public void ExchangingTheReadRolesReversesTheFinishButPreservesWhetherTheFavoriteWon()
    {
        var ordinary = RaceExperiments.Run(0);
        var exchanged = RaceExperiments.Run(2);

        Assert.True(ordinary.FinishOrder is AFirst or BFirst);
        Assert.Equal(ordinary.LandmarkOrder, ordinary.FinishOrder);
        Assert.Equal(Exchange(ordinary.FinishOrder), exchanged.FinishOrder);
        Assert.Equal(Exchange(ordinary.LandmarkOrder), exchanged.LandmarkOrder);
        Assert.Equal(Exchange(ordinary.FavoriteOrder), exchanged.FavoriteOrder);
        Assert.Equal((byte)2, ordinary.FavoriteWon);
        Assert.Equal(ordinary.FavoriteWon, exchanged.FavoriteWon);
        Assert.Equal(ordinary.ConsumerLawBytes, exchanged.ConsumerLawBytes);
    }

    [Fact]
    public void ForecastAndFinishCanDisagreeWhileTheContestGoalIsReached()
    {
        var upset = RaceExperiments.Run(1);
        Assert.True(upset.FinishOrder is AFirst or BFirst);
        Assert.Equal(Exchange(upset.FinishOrder), upset.FavoriteOrder);
        Assert.Equal((byte)1, upset.FavoriteWon);
        Assert.Equal((byte)2, upset.GoalReached);
    }

    [Fact]
    public void WithheldPrecedenceAndAnObservedSharedFinishHaveDifferentSupports()
    {
        var hidden = RaceExperiments.Run(4);
        var shared = RaceExperiments.Run(5);
        Assert.Equal((byte)2, hidden.GoalReached);
        Assert.Equal((byte)2, shared.GoalReached);
        Assert.Equal((byte)(AFirst | BFirst | SharedFinish), hidden.FinishOrder);
        Assert.Equal((byte)3, hidden.FavoriteWon);
        Assert.Equal(SharedFinish, shared.FinishOrder);
        Assert.Equal((byte)1, shared.FavoriteWon); // The consumer asks about sole victory.
    }

    [Theory]
    [InlineData(8)] // Another contest.
    [InlineData(9)] // Another entrant.
    public void ForeignPublishedEvidenceLeavesTheResultVisibleAndTheBlendWithoutABasis(int scenario)
    {
        var foreign = RaceExperiments.Run(scenario);
        Assert.Equal((byte)2, foreign.GoalReached);
        Assert.True(foreign.FinishOrder is AFirst or BFirst);
        Assert.Equal((byte)0, foreign.FavoriteWon);
    }

    [Fact]
    public void RefiningThePrivateTrackDoesNotIncreaseTheConsumersWorkOrChangeTheRaceRead()
    {
        var small = RaceExperiments.Run(0, 16);
        var large = RaceExperiments.Run(0, 16_384);

        Assert.Equal(small.FinishOrder, large.FinishOrder);
        Assert.Equal(small.FavoriteWon, large.FavoriteWon);
        Assert.Equal(small.Timeline.Select(b => (b.AState, b.BState)),
            large.Timeline.Select(b => (b.AState, b.BState)));
        Assert.True(small.HostReads > 0);
        Assert.Equal(small.HostReads, large.HostReads);
        Assert.True(large.SupplierInspections > small.SupplierInspections);

        var reshaped = RaceExperiments.Run(3, 16);
        Assert.NotEqual(small.CoursePrefix, reshaped.CoursePrefix);
        Assert.Equal(small.FinishOrder, reshaped.FinishOrder);
        Assert.Equal(small.FinishBytes, reshaped.FinishBytes);
    }

    [Fact]
    public void APartialCourseRecheckWithdrawsOnlyTheCurrentPlacingUntilTheOwnerCompletesItsWork()
    {
        var trial = RaceExperiments.Run(10, 16);
        Assert.All(trial.CourseEdit, edit => Assert.True(edit.Applied));
        Assert.Equal(2, trial.CourseEdit.Length);
        var earlier = trial.Timeline[^4];
        var stale = trial.Timeline[^3];
        var partial = trial.Timeline[^2];
        var refreshed = trial.Timeline[^1];

        Assert.Equal(AFirst, earlier.FinishOrder);
        Assert.Equal((byte)2, earlier.FavoriteWon);
        Assert.Equal((byte)7, stale.FinishOrder);
        Assert.Equal((byte)3, stale.FavoriteWon);
        Assert.Equal(stale.FinishOrder, partial.FinishOrder);
        Assert.Equal(stale.FavoriteWon, partial.FavoriteWon);
        Assert.True(partial.SupplierInspections > stale.SupplierInspections);
        Assert.True(refreshed.SupplierInspections > partial.SupplierInspections);
        Assert.Equal(earlier.FinishOrder, refreshed.FinishOrder);
        Assert.Equal(earlier.FavoriteWon, refreshed.FavoriteWon);
        Assert.All(new[] { earlier, stale, partial, refreshed }, beat =>
        {
            Assert.Equal((2, 2), (beat.AState, beat.BState));
            Assert.Equal((byte)2, beat.GoalReached);
            Assert.Equal(earlier.Elapsed, beat.Elapsed);
        });
    }

    [Fact]
    public void OneOrdinalReaderDistinguishesEarlierLaterSharedUnorderedAndOutside()
    {
        var chain = new LandmarkRun(2, [2, 0, 1]);
        var incomplete = new LandmarkRun(2, [2, 0, 1], []);
        Assert.Equal(AFirst, new RaceOrderRead(chain, 2, 1).ReadSupport());
        Assert.Equal(BFirst, new RaceOrderRead(chain, 1, 2).ReadSupport());
        Assert.Equal(SharedFinish, new RaceOrderRead(incomplete, 0, 0).ReadSupport());
        Assert.Equal((byte)7, new RaceOrderRead(incomplete, 2, 1).ReadSupport());
        Assert.Equal((byte)0, new RaceOrderRead(chain, 3, 1).ReadSupport());
    }

    [Fact]
    public void ReversedLocalRolesAreMappedBeforeTheFavoriteIsCompared()
    {
        var owner = new PortStructure();
        var contest = owner.Publish(owner.Create(0, 0, 1, 1));
        var a = owner.Publish(owner.Create(0, 0, 1, 1));
        var b = owner.Publish(owner.Create(0, 0, 1, 1));
        // Distinct connection objects expose the same public sites.
        var board = new RaceFrame(owner.Connect(contest), owner.Connect(a), owner.Connect(b));
        var finish = new RaceFrame(owner.Connect(contest), owner.Connect(b), owner.Connect(a));
        var forecast = new Reading(AFirst);
        var finishOrder = new Reading(BFirst);
        var decider = new RaceDecision(new RaceOrderOffer(board, forecast), new RaceOrderOffer(finish, finishOrder));

        foreach (var favored in new byte[] { AFirst, BFirst })
        for (byte admittedFinish = 0; admittedFinish < 8; admittedFinish++)
        {
            forecast.Support = favored;
            finishOrder.Support = admittedFinish;
            byte expected = 0;
            var favoredInFinishFrame = Exchange(favored);
            if ((admittedFinish & favoredInFinishFrame) != 0) expected |= 2;
            if ((admittedFinish & ~favoredInFinishFrame) != 0) expected |= 1;
            Assert.Equal(expected, decider.ReadSupport());
        }
        Assert.True(finishOrder.ReadCount > 0);
        Assert.True(forecast.ReadCount > 0);
    }

    [Fact]
    public void TwoBindingsOfTheSameEntrantCannotEstablishATiedTwoEntrantContest()
    {
        var owner = new PortStructure();
        var entrant = owner.Publish(owner.Create(0, 0, 1, 1));
        var frame = new RaceFrame(NewSite(owner), owner.Connect(entrant), owner.Connect(entrant));
        var decision = new RaceDecision(new RaceOrderOffer(frame, new Reading(AFirst)),
            new RaceOrderOffer(frame, new Reading(SharedFinish)));
        Assert.Equal((byte)0, decision.ReadSupport());
    }

    [Fact]
    public void ReusingOneObservationPreservesItsCorrelationWhileTwoCapturesDoNot()
    {
        var owner = new PortStructure();
        var frame = new RaceFrame(NewSite(owner), NewSite(owner), NewSite(owner));
        var uncertain = new Reading(AFirst | BFirst);
        var offer = new RaceOrderOffer(frame, uncertain);
        var sameObservation = new RaceDecision(offer, offer);
        Assert.Equal((byte)2, sameObservation.ReadSupport());
        Assert.Equal(1, uncertain.ReadCount);
        Assert.Equal(1, sameObservation.HostReads);

        var oldRead = CapturedRead.Capture(uncertain);
        var newRead = CapturedRead.Capture(uncertain);
        var separateObservations = new RaceDecision(new RaceOrderOffer(frame, oldRead),
            new RaceOrderOffer(frame, newRead));
        Assert.Equal((byte)3, separateObservations.ReadSupport());
        Assert.Equal(2, separateObservations.HostReads);
    }

    [Fact]
    public void AnIncompleteFinishCanGuardAnotherOwnerWithoutSelectingAResult()
    {
        var owner = new PortStructure();
        var frame = new RaceFrame(NewSite(owner), NewSite(owner), NewSite(owner));
        var finishOrder = new Reading(7);
        var decision = new RaceDecision(new RaceOrderOffer(frame, new Reading(AFirst)),
            new RaceOrderOffer(frame, finishOrder));
        var continuation = new PulseCircuit(2);
        var waiting = continuation.State(true);
        var after = continuation.State();
        var guard = continuation.Guard();
        continuation.Bind(guard, decision);
        continuation.Link(continuation.AddTable(PassageLaw.Controlled()), waiting, guard, after);

        var unresolved = continuation.Pulse();
        Assert.False(unresolved.Committed);
        Assert.Equal(waiting, continuation.Active);
        finishOrder.Support = BFirst;
        Assert.False(continuation.Pulse().Moved);
        Assert.Equal(waiting, continuation.Active);
        finishOrder.Support = AFirst;
        Assert.True(continuation.Pulse().Moved);
        Assert.Equal(after, continuation.Active);
    }

    [Fact]
    public void DisconnectingTheObserverGoalLeavesIdenticalRunnerAndFinishWitnesses()
    {
        var observed = RaceExperiments.Run(0);
        var disconnected = RaceExperiments.Run(11);
        Assert.Equal((byte)2, observed.GoalReached);
        Assert.Equal((byte)0, disconnected.GoalReached);
        Assert.Equal(observed.FinishOrder, disconnected.FinishOrder);
        Assert.Equal(observed.FavoriteWon, disconnected.FavoriteWon);
        Assert.Equal(observed.Timeline.Select(b => (b.AState, b.BState)),
            disconnected.Timeline.Select(b => (b.AState, b.BState)));
    }

    [Fact]
    public void WaitingAtTheSharedStartIsDistinctFromAnObstacleAfterRelease()
    {
        var waiting = RaceExperiments.Run(6);
        var obstacle = RaceExperiments.Run(7);

        Assert.True(waiting.Timeline.Length > 1);
        Assert.All(waiting.Timeline, beat =>
        {
            Assert.Equal(0, beat.AState);
            Assert.Equal(0, beat.BState);
            Assert.Equal((byte)1, beat.GoalReached);
        });
        Assert.Contains(obstacle.Timeline, beat => beat.BState == 1 && beat.BAccess == 1);
        Assert.All(obstacle.Timeline, beat => Assert.Equal((byte)0, beat.Assistance));
        Assert.Equal(2, obstacle.Timeline[^1].BState);
        Assert.Equal((byte)2, obstacle.GoalReached);
    }

    [Fact]
    public void AssistanceNeedsAnUnsatisfiedGoalAndOpensThePassageBeforeTheRunnerCanFinish()
    {
        var helped = RaceExperiments.Run(12);
        var disconnected = RaceExperiments.Run(13);

        Assert.Contains(helped.Timeline, beat => beat.BState == 1 && beat.BAccess == 1
            && beat.Assistance == 1 && beat.GoalReached == 1);
        int release = Array.FindIndex(helped.Timeline, beat => beat.Assistance == 2);
        int finish = Array.FindIndex(helped.Timeline, beat => beat.BState == 2);
        Assert.True(release >= 0 && finish > release);
        Assert.Equal(1, helped.Timeline[release].BState);
        Assert.Equal((byte)2, helped.GoalReached);
        Assert.Equal((byte)1, helped.Timeline[finish].BAccess);
        Assert.Equal((byte)2, helped.Timeline[finish].BEffectiveAccess);

        Assert.All(disconnected.Timeline, beat => Assert.Equal((byte)1, beat.Assistance));
        Assert.Equal(1, disconnected.Timeline[^1].BState);
        Assert.Equal((byte)0, disconnected.GoalReached);
    }

    private static PortConnection NewSite(PortStructure owner) =>
        owner.Connect(owner.Publish(owner.Create(0, 0, 1, 1)));

    private sealed class Reading(byte support) : IPrimitiveReadPort
    {
        public byte Support { get; set; } = support;
        public int ReadCount { get; private set; }

        public byte ReadSupport()
        {
            ReadCount++;
            return Support;
        }
    }

    private static byte Exchange(byte support) =>
        (byte)(((support & AFirst) << 1) | ((support & BFirst) >> 1) | (support & SharedFinish));
}
