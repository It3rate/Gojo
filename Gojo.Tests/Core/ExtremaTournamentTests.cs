using Gojo.Core;
using Gojo.Structures;

namespace Gojo.Tests.Core;

public sealed class ExtremaTournamentTests
{
    [Fact]
    public void MinimumTournamentSelectsLowestMeasureThroughPairwiseBouts()
    {
        var measure = new Carrier();
        var first = Relation.Point(new Carrier(), 0);
        var second = Relation.Point(new Carrier(), 1);
        var third = Relation.Point(new Carrier(), 2);
        var entries = new[]
        {
            ExtremaTournament.Entry(first, new CarrierLocation(measure, 7)),
            ExtremaTournament.Entry(second, new CarrierLocation(measure, 3)),
            ExtremaTournament.Entry(third, new CarrierLocation(measure, 5))
        };

        var tournament = ExtremaTournament.Minimum(new Carrier(), entries, out var rounds);

        Assert.True(ExtremaTournament.TryReadWinner(tournament, out var winner));
        Assert.True(ExtremaTournament.TryReadEntry(winner, out var value, out var winnerMeasure));
        Assert.Equal(second, value);
        Assert.Equal(new CarrierLocation(measure, 3), winnerMeasure);
        Assert.Equal(2, rounds.Length);
        Assert.True(ExtremaTournament.TryReadTimeline(tournament, out var timeline));
        Assert.True(TimeStepSequence.TryRead(timeline, out var origin, out var initial, out var timedRounds));
        Assert.Equal(rounds, timedRounds);
        Assert.Equal(0, origin.Ticks);
        var initialEntries = OrderedElements(Assert.IsType<Relation>(initial));
        Assert.Equal(new IElement[] { entries[0], entries[1], entries[2] }, initialEntries);
        Assert.True(TimeStepSequence.TryReadStep(
            rounds[0],
            out _,
            out var timeStart,
            out var timeEnd,
            out var before,
            out var transition,
            out var after,
            out var residue));
        Assert.True(origin.Carrier.SameIdentityAs(timeStart.Carrier));
        Assert.Equal(new CarrierLocation(origin.Carrier, 0), timeStart);
        Assert.Equal(new CarrierLocation(origin.Carrier, 1), timeEnd);
        Assert.Equal(new IElement[] { entries[0], entries[1], entries[2] }, OrderedElements(Assert.IsType<Relation>(before)));
        Assert.Equal(new IElement[] { entries[1], entries[2] }, OrderedElements(Assert.IsType<Relation>(after)));
        Assert.Equal(new IElement[] { entries[0] }, OrderedElements(Assert.IsType<Relation>(residue)));

        Assert.True(ExtremaTournament.TryReadRound(rounds[0], out var firstRoundIndex, out var firstRoundBouts));
        Assert.Equal(timeStart, firstRoundIndex);
        Assert.True(ExtremaTournament.TryReadRound(Assert.IsType<Relation>(transition), out var evidenceRoundIndex, out var evidenceBouts));
        Assert.Equal(0, evidenceRoundIndex.Ticks);
        Assert.Equal(evidenceBouts, firstRoundBouts);
        Assert.Single(firstRoundBouts);
        Assert.True(ExtremaTournament.TryReadBout(
            firstRoundBouts[0],
            out _,
            out var firstWinner,
            out var firstRemainder,
            out var firstMargin));
        Assert.True(ExtremaTournament.TryReadEntry(firstWinner, out var firstWinnerValue, out _));
        Assert.True(ExtremaTournament.TryReadEntry(firstRemainder, out var firstRemainderValue, out _));
        Assert.Equal(second, firstWinnerValue);
        Assert.Equal(first, firstRemainderValue);
        Assert.Equal(new CarrierLocation(measure, 4), firstMargin);
        Assert.True(ExtremaTournament.TryReadBoutClaim(firstRoundBouts[0], out var firstClaim));
        Assert.True(OperationClaim.TryRead(
            firstClaim,
            out _,
            out var firstResidual,
            out var firstViolation,
            out _,
            out var firstViolationMagnitude,
            out _));
        Assert.Equal(new CarrierLocation(measure, 4), Assert.IsType<CarrierLocation>(firstResidual));
        Assert.Equal(new CarrierLocation(measure, 4), Assert.IsType<CarrierLocation>(firstViolation));
        Assert.Equal(new CarrierLocation(measure, 4), firstViolationMagnitude);
    }

    [Fact]
    public void MaximumTournamentSelectsHighestMeasure()
    {
        var measure = new Carrier();
        var low = Relation.Point(new Carrier(), 0);
        var high = Relation.Point(new Carrier(), 1);
        var entries = new[]
        {
            ExtremaTournament.Entry(low, new CarrierLocation(measure, 2)),
            ExtremaTournament.Entry(high, new CarrierLocation(measure, 9))
        };

        var tournament = ExtremaTournament.Maximum(new Carrier(), entries, out var rounds);

        Assert.True(ExtremaTournament.TryReadWinner(tournament, out var winner));
        Assert.True(ExtremaTournament.TryReadEntry(winner, out var value, out var winnerMeasure));
        Assert.Equal(high, value);
        Assert.Equal(new CarrierLocation(measure, 9), winnerMeasure);
        Assert.Single(rounds);
        Assert.True(ExtremaTournament.TryReadRound(rounds[0], out _, out var bouts));
        Assert.True(ExtremaTournament.TryReadBout(bouts[0], out _, out _, out _, out var margin));
        Assert.Equal(new CarrierLocation(measure, 7), margin);
        Assert.True(ExtremaTournament.TryReadBoutClaim(bouts[0], out var claim));
        Assert.True(OperationClaim.TryRead(claim, out _, out _, out _, out _, out var violationMagnitude, out _));
        Assert.Equal(new CarrierLocation(measure, 7), violationMagnitude);
    }

    [Fact]
    public void TiedTournamentPreservesZeroMarginAndKeepsFirstEntry()
    {
        var measure = new Carrier();
        var first = Relation.Point(new Carrier(), 0);
        var second = Relation.Point(new Carrier(), 1);
        var entries = new[]
        {
            ExtremaTournament.Entry(first, new CarrierLocation(measure, 4)),
            ExtremaTournament.Entry(second, new CarrierLocation(measure, 4))
        };

        var tournament = ExtremaTournament.Minimum(new Carrier(), entries, out var rounds);

        Assert.True(ExtremaTournament.TryReadWinner(tournament, out var winner));
        Assert.True(ExtremaTournament.TryReadEntry(winner, out var value, out _));
        Assert.Equal(first, value);
        Assert.True(ExtremaTournament.TryReadRound(rounds[0], out _, out var bouts));
        Assert.True(ExtremaTournament.TryReadBout(bouts[0], out _, out _, out var remainder, out var margin));
        Assert.True(ExtremaTournament.TryReadEntry(remainder, out var remainderValue, out _));
        Assert.Equal(second, remainderValue);
        Assert.Equal(new CarrierLocation(measure, 0), margin);
        Assert.True(ExtremaTournament.TryReadBoutClaim(bouts[0], out var claim));
        Assert.True(OperationClaim.TryRead(claim, out _, out _, out _, out _, out var violationMagnitude, out _));
        Assert.Equal(new CarrierLocation(measure, 0), violationMagnitude);
    }

    private static IElement[] OrderedElements(Relation structure)
    {
        var holder = Assert.IsType<Carrier>(structure.Right);
        Assert.True(structure.TryReadOrderedElementsOn(holder, out var elements));
        return elements;
    }
}
