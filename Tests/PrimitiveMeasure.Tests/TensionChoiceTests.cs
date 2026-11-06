using Gojo.Experiments.PrimitiveMeasure;

namespace Gojo.Experiments.PrimitiveMeasure.Tests;

public sealed class TensionChoiceTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void PreferenceFollowsTheOutletThatActuallyDeliversMorePerOpportunity(int fastAction)
    {
        int[][] effects = [[1,0,0], [1,0,0], [1,0,0]];
        var machine = new TensionChoiceMachine([48,14,25], effects, fastAction);
        var turns = Enumerable.Range(0, 15).Select(_ => machine.Step(0)).ToArray();

        var firstAttempts = turns.Take(3).ToArray();
        Assert.Equal(new[] { 0, 1, 2 }, firstAttempts.Select(s => s.Action).Order().ToArray());
        Assert.All(firstAttempts, turn =>
        {
            Assert.Null(turn.Prediction[0]);
            Assert.Equal(1, turn.Observed[0]);
        });
        var learned = Array.FindIndex(turns, t => t.Action == fastAction && t.Observed[0] == 2);
        Assert.InRange(learned, 3, turns.Length - 4);
        Assert.All(turns.Skip(learned + 1), turn =>
        {
            Assert.Equal(fastAction, turn.Action);
            Assert.Equal(2, turn.Prediction[0]);
            Assert.Equal(2, turn.Before[0] - turn.After[0]);
        });
        Assert.All(turns, turn =>
        {
            Assert.Equal(1 << (turn.Action + 1), turn.OpportunityBits);
            Assert.Equal(new[] { 14, 25 }, turn.After[1..]);
        });
    }

    [Fact]
    public void ClosingTheOnlyAvailableRouteHoldsTheOpportunityAndCreatesNoEvidence()
    {
        var machine = new TensionChoiceMachine([30,12,24], [[1,0,0], [1,0,0], [1,0,0]]);
        var closed = machine.Step(0, accessMask: 0);
        Assert.Equal(-1, closed.Action);
        Assert.Equal(1, closed.OpportunityBits);
        Assert.Equal(closed.Before, closed.After);
        Assert.All(closed.Samples.SelectMany(row => row), sample => Assert.Null(sample));
        var open = machine.Step(0, accessMask: 0b100);
        Assert.Equal(2, open.Action);
        Assert.Equal(0b1000, open.OpportunityBits);
        Assert.Equal(new[] { 29, 12, 24 }, open.After);
    }

    [Fact]
    public void EquallyEffectiveServicesContinueSharingOpportunitiesAcrossTies()
    {
        var machine = new TensionChoiceMachine([48,5,6], [[1,0,0], [1,0,0], [1,0,0]]);
        var turns = Enumerable.Range(0, 12).Select(_ => machine.Step(0)).ToArray();
        Assert.All(turns.Skip(3), turn => Assert.Equal(0b111, turn.SelectionMask));
        foreach (int action in Enumerable.Range(0, 3))
            Assert.Equal(4, turns.Count(t => t.Action == action));
    }

    [Fact]
    public void ClosingAKnownBestOutletLetsAnotherRouteActWithoutErasingTheBestPrediction()
    {
        var machine = new TensionChoiceMachine([48,11,12], [[1,0,0], [2,0,0], [1,0,0]]);
        foreach (var mask in new byte[] { 1, 2, 4 }) machine.Step(0, mask);
        Assert.Equal(1, machine.Step(0).Action);
        var closed = machine.Step(0, accessMask: 0b101);
        Assert.Contains(closed.Action, new[] { 0, 2 });
        Assert.Equal(2, closed.Samples[1][0]);
        Assert.Equal(1, machine.Step(0).Action);
    }

    [Fact]
    public void AnOptimisticBetCanMeetAStandingNeedWhileMakingTheCashNeedWorse()
    {
        int[][] effects = [[1,0,0], [1,0,0], [-1,1,0]];
        int[,] priors = {{1,0,0}, {1,0,0}, {2,1,0}};
        var machine = new TensionChoiceMachine([20,15,13], effects, priors: priors);
        var bet = machine.Step(0);
        Assert.Equal(2, bet.Action);
        Assert.Equal(2, bet.Prediction[0]);
        Assert.Equal(new int?[] { -1, 1, 0 }, bet.Observed);
        Assert.Equal(new[] { 21, 14, 13 }, bet.After);
        Assert.Equal(0b1000, bet.OpportunityBits);
        Assert.NotEqual(2, machine.Step(0).Action);
        Assert.Equal(2, machine.Step(1).Action);
    }

    [Fact]
    public void ConcealedCashOutcomeCannotUpdateAnOptimisticForecastFromPrivateState()
    {
        int[][] effects = [[1,0,0], [1,0,0], [-1,1,0]];
        int[,] priors = {{1,0,0}, {1,0,0}, {2,0,0}};
        var machine = new TensionChoiceMachine([20,15,13], effects, priors: priors);
        var concealed = machine.Step(0, feedbackMask: 0b110);
        Assert.Equal(2, concealed.Action);
        Assert.Equal(new[] { 21, 14, 13 }, concealed.After);
        Assert.Null(concealed.Observed[0]);
        Assert.Equal(2, concealed.Samples[2][0]);
        Assert.Equal(1, concealed.Samples[2][1]);
        var witnessed = machine.Step(0);
        Assert.Equal(2, witnessed.Action);
        Assert.Equal(2, witnessed.Prediction[0]);
        Assert.Equal(-1, witnessed.Observed[0]);
        Assert.NotEqual(2, machine.Step(0).Action);
    }

    [Fact]
    public void ARouteLearnsOnlyAsMuchReliefAsTheReceiverCanActuallyAccept()
    {
        int[,] priors = {{2,0,0}, {1,0,0}, {1,0,0}};
        var machine = new TensionChoiceMachine([1,12,9], [[2,0,0], [1,0,0], [1,0,0]], priors: priors);
        var clipped = machine.Step(0);
        Assert.Equal(0, clipped.Action);
        Assert.Equal(2, clipped.Prediction[0]);
        Assert.Equal(1, clipped.Observed[0]);
        Assert.Equal(1, clipped.BlockedEffect[0]);
        Assert.Equal(1, clipped.Samples[0][0]);
        var satisfied = machine.Step(0);
        Assert.Equal(-1, satisfied.Action);
        Assert.Equal(new[] { 0, 12, 9 }, satisfied.After);
    }

    [Fact]
    public void DebitAtTheNeedCeilingHasABlockedEffectAndTheRecipientCanStillGainStanding()
    {
        int[,] priors = {{1,0,0}, {2,0,0}, {1,0,0}};
        var machine = new TensionChoiceMachine([48,12,6], [[1,0,0], [-1,1,0], [1,0,0]], priors: priors);
        var bet = machine.Step(0);
        Assert.Equal(1, bet.Action);
        Assert.Equal(new int?[] { 0, 1, 0 }, bet.Observed);
        Assert.Equal(new[] { -1, 0, 0 }, bet.BlockedEffect);
        Assert.Equal(new[] { 48, 11, 6 }, bet.After);
        Assert.Equal(0, bet.Samples[1][0]);
    }

    [Fact]
    public void TheOccupiedOutletCreditsOnlyItsConnectedMemoryFields()
    {
        var machine = new TensionChoiceMachine([20,15,13], [[1,0,0], [2,-1,0], [1,0,0]]);
        var turn = machine.Step(0, accessMask: 0b010);
        var routes = machine.FeedbackRows();
        var written = new HashSet<int>();
        int Role(int field) => (routes[field / 2] >> (4 * (field % 2))) & 15;
        for (var row = 0; row < 9; row++)
        {
            var outlet = Role(3 * row);
            var basis = Role(3 * row + 1);
            var destination = Role(3 * row + 2);
            int bit = 3 * destination;
            int packed = turn.MemoryBytes[bit / 8];
            if (bit / 8 + 1 < turn.MemoryBytes.Length) packed |= turn.MemoryBytes[bit / 8 + 1] << 8;
            int code = (packed >> (bit % 8)) & 7;
            if ((turn.OpportunityBits & (1 << outlet)) == 0) Assert.Equal(0, code);
            else
            {
                Assert.Equal(2 + turn.Observed[basis], code);
                Assert.True(written.Add(destination));
            }
        }
        Assert.Equal(3, written.Count);
    }

    [Fact]
    public void UnrelatedClaimsDoNotSteerAReaderFocusingOnOneBasis()
    {
        int[][] effects = [[1,0,0], [2,-1,0], [1,1,0]];
        var low = new TensionChoiceMachine([40,2,0], effects);
        var high = new TensionChoiceMachine([40,17,48], effects);
        for (var i = 0; i < 10; i++)
        {
            var a = low.Step(0);
            var b = high.Step(0);
            Assert.Equal(a.Action, b.Action);
            Assert.Equal(a.After[0], b.After[0]);
            Assert.Equal(48, b.After[2]);
        }
    }

    [Fact]
    public void AnOutstandingPickupNeedCanHaveThreeOpenActionsAndNoPredictedRelief()
    {
        var machine = new TensionChoiceMachine([30,10,19], [[1,0,0], [2,1,0], [1,1,0]],
            offerMasks: [7,7,7]);
        // Each experience also exposes its effect at pickup's own read surface.
        foreach (var mask in new byte[] { 1, 2, 4 }) machine.Step(0, mask);
        var stranded = machine.Step(2);
        Assert.Equal(new int?[] { 0, 0, 0 }, stranded.Forecasts);
        Assert.Equal(-1, stranded.Action);
        Assert.Equal(1, stranded.OpportunityBits);
        Assert.Equal(stranded.Before, stranded.After);
        Assert.Equal(19, stranded.After[2]);
    }

    [Fact]
    public void HighNeedWithoutAnOfferedRoutePreservesUnknownPredictions()
    {
        var machine = new TensionChoiceMachine([12,8,48], [[1,1,0], [2,0,0], [0,2,0]]);
        var stranded = machine.Step(2);
        Assert.All(stranded.Forecasts, sample => Assert.Null(sample));
        Assert.Equal(0, machine.OfferedActions(2));
        Assert.Equal(-1, stranded.Action);
        Assert.Equal(48, stranded.After[2]);
    }

    [Fact]
    public void ANewlyBetterOutletPresentBehindANegativePriorRequiresAPathToRetest()
    {
        // The apparatus starts with stale memories. No external investigator
        // can reveal the hidden effect of an action the selector never takes.
        int[,] priors = {{1,0,0}, {-1,0,0}, {0,0,0}};
        var machine = new TensionChoiceMachine([30,0,0], [[1,0,0], [2,0,0], [0,0,0]], priors: priors);
        for (var i = 0; i < 6; i++)
        {
            var turn = machine.Step(0);
            Assert.Equal(0, turn.Action);
            Assert.Equal(-1, turn.Samples[1][0]);
        }
    }
}
