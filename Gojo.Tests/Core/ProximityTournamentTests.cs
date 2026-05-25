using Gojo.Core;
using Gojo.Structures;

namespace Gojo.Tests.Core;

public sealed class ProximityTournamentTests
{
    [Fact]
    public void NearestSelectsSmallestEqualityClaimResidual()
    {
        var value = new Carrier();
        var residual = new Carrier(value.UnitShape);
        var measure = new Carrier(value.UnitShape);
        IElement[] candidates =
        [
            new CarrierLocation(value, 1),
            new CarrierLocation(value, 7),
            new CarrierLocation(value, 4)
        ];

        var proximity = ProximityTournament.Nearest(
            new Carrier(),
            candidates,
            new CarrierLocation(value, 5),
            residual,
            measure,
            out var choices,
            out var rounds);

        Assert.True(ProximityTournament.TryReadChoices(proximity, out var readChoices));
        Assert.Equal(choices, readChoices);
        Assert.Equal(2, rounds.Length);
        Assert.True(ProximityTournament.TryReadTimeline(proximity, out var timeline));
        Assert.True(TimeStepSequence.TryRead(timeline, out var origin, out var initial, out var timedRounds));
        Assert.Equal(rounds, timedRounds);
        Assert.Equal(0, origin.Ticks);
        Assert.Equal(choices, ChoiceValues(Assert.IsType<Relation>(initial)));
        Assert.True(ProximityTournament.TryReadRounds(proximity, out var readRounds));
        Assert.Equal(rounds, readRounds);
        Assert.True(TimeStepSequence.TryReadStep(
            rounds[0],
            out _,
            out var firstStart,
            out var firstEnd,
            out var before,
            out var transition,
            out var after,
            out var residue));
        Assert.Equal(new CarrierLocation(origin.Carrier, 0), firstStart);
        Assert.Equal(new CarrierLocation(origin.Carrier, 1), firstEnd);
        Assert.Equal(choices, ChoiceValues(Assert.IsType<Relation>(before)));
        Assert.Equal(new IElement[] { choices[1], choices[2] }, ChoiceValues(Assert.IsType<Relation>(after)));
        Assert.Equal(new IElement[] { choices[0] }, ChoiceValues(Assert.IsType<Relation>(residue)));
        Assert.True(ExtremaTournament.TryReadRound(Assert.IsType<Relation>(transition), out var roundIndex, out var bouts));
        Assert.Equal(0, roundIndex.Ticks);
        Assert.Single(bouts);
        Assert.True(ProximityTournament.TryReadWinner(proximity, out var winner));
        Assert.True(ProximityTournament.TryReadChoice(
            winner,
            out var winnerValue,
            out _,
            out var equalityClaim,
            out _,
            out var distance,
            out var boundaryViolation,
            out var selectionMeasure));
        Assert.Equal(new CarrierLocation(value, 4), Assert.IsType<CarrierLocation>(winnerValue));
        Assert.Equal(new CarrierLocation(residual, 1), distance);
        Assert.Equal(new CarrierLocation(residual, 0), boundaryViolation);
        Assert.Equal(new CarrierLocation(measure, 1), selectionMeasure);
        Assert.True(OperationClaim.TryRead(equalityClaim, out _, out var residualRead, out _, out _, out _, out _));
        Assert.Equal(new CarrierLocation(residual, 1), Assert.IsType<CarrierLocation>(residualRead));
    }

    [Fact]
    public void NearestAtLeastSelectsClosestCandidateAboveTarget()
    {
        var value = new Carrier();
        var residual = new Carrier(value.UnitShape);
        var measure = new Carrier(value.UnitShape);
        IElement[] candidates =
        [
            new CarrierLocation(value, 4),
            new CarrierLocation(value, 6),
            new CarrierLocation(value, 9)
        ];

        var proximity = ProximityTournament.NearestAtLeast(
            new Carrier(),
            candidates,
            new CarrierLocation(value, 5),
            residual,
            measure,
            out var choices,
            out _);

        Assert.True(ProximityTournament.TryReadWinner(proximity, out var winner));
        Assert.True(ProximityTournament.TryReadChoice(
            winner,
            out var winnerValue,
            out _,
            out _,
            out var winnerBoundary,
            out var winnerDistance,
            out var winnerBoundaryViolation,
            out _));
        Assert.Equal(new CarrierLocation(value, 6), Assert.IsType<CarrierLocation>(winnerValue));
        Assert.Equal(new CarrierLocation(residual, 1), winnerDistance);
        Assert.Equal(new CarrierLocation(residual, 0), winnerBoundaryViolation);
        var winnerBoundaryClaim = Assert.IsType<Relation>(winnerBoundary);
        Assert.True(OperationClaim.TryRead(winnerBoundaryClaim, out _, out _, out _, out _, out var winnerViolationMagnitude, out _));
        Assert.Equal(new CarrierLocation(residual, 0), winnerViolationMagnitude);

        Assert.True(ProximityTournament.TryReadChoice(
            choices[0],
            out _,
            out _,
            out _,
            out var belowBoundary,
            out var belowDistance,
            out var belowBoundaryViolation,
            out var belowSelection));
        Assert.Equal(new CarrierLocation(residual, 1), belowDistance);
        Assert.Equal(new CarrierLocation(residual, 1), belowBoundaryViolation);
        Assert.Equal(6, belowSelection.Ticks);
        var belowBoundaryClaim = Assert.IsType<Relation>(belowBoundary);
        Assert.True(OperationClaim.TryRead(belowBoundaryClaim, out _, out _, out _, out _, out var belowViolationMagnitude, out _));
        Assert.Equal(new CarrierLocation(residual, 1), belowViolationMagnitude);
    }

    [Fact]
    public void NearestAtMostSelectsClosestCandidateBelowTarget()
    {
        var value = new Carrier();
        var residual = new Carrier(value.UnitShape);
        var measure = new Carrier(value.UnitShape);
        IElement[] candidates =
        [
            new CarrierLocation(value, 2),
            new CarrierLocation(value, 4),
            new CarrierLocation(value, 6)
        ];

        var proximity = ProximityTournament.NearestAtMost(
            new Carrier(),
            candidates,
            new CarrierLocation(value, 5),
            residual,
            measure,
            out var choices,
            out _);

        Assert.True(ProximityTournament.TryReadWinner(proximity, out var winner));
        Assert.True(ProximityTournament.TryReadChoice(
            winner,
            out var winnerValue,
            out _,
            out _,
            out _,
            out var winnerDistance,
            out var winnerBoundaryViolation,
            out _));
        Assert.Equal(new CarrierLocation(value, 4), Assert.IsType<CarrierLocation>(winnerValue));
        Assert.Equal(new CarrierLocation(residual, 1), winnerDistance);
        Assert.Equal(new CarrierLocation(residual, 0), winnerBoundaryViolation);

        Assert.True(ProximityTournament.TryReadChoice(
            choices[2],
            out _,
            out _,
            out _,
            out _,
            out _,
            out var aboveBoundaryViolation,
            out var aboveSelection));
        Assert.Equal(new CarrierLocation(residual, 1), aboveBoundaryViolation);
        Assert.Equal(5, aboveSelection.Ticks);
    }

    [Fact]
    public void DirectionalNearestFallsBackToSmallestBoundaryViolationWhenNoCandidateSatisfiesBoundary()
    {
        var value = new Carrier();
        var residual = new Carrier(value.UnitShape);
        var measure = new Carrier(value.UnitShape);
        IElement[] candidates =
        [
            new CarrierLocation(value, 1),
            new CarrierLocation(value, 3)
        ];

        var proximity = ProximityTournament.NearestAtLeast(
            new Carrier(),
            candidates,
            new CarrierLocation(value, 5),
            residual,
            measure,
            out _,
            out _);

        Assert.True(ProximityTournament.TryReadWinner(proximity, out var winner));
        Assert.True(ProximityTournament.TryReadChoice(
            winner,
            out var winnerValue,
            out _,
            out _,
            out _,
            out var distance,
            out var boundaryViolation,
            out var selectionMeasure));
        Assert.Equal(new CarrierLocation(value, 3), Assert.IsType<CarrierLocation>(winnerValue));
        Assert.Equal(new CarrierLocation(residual, 2), distance);
        Assert.Equal(new CarrierLocation(residual, 2), boundaryViolation);
        Assert.Equal(new CarrierLocation(measure, 12), selectionMeasure);
    }

    [Fact]
    public void NearestWithinSelectsClosestCandidateInsideInterval()
    {
        var value = new Carrier();
        var residual = new Carrier(value.UnitShape);
        var measure = new Carrier(value.UnitShape);
        IElement[] candidates =
        [
            new CarrierLocation(value, 2),
            new CarrierLocation(value, 6),
            new CarrierLocation(value, 8)
        ];

        var proximity = ProximityTournament.NearestWithin(
            new Carrier(),
            candidates,
            new CarrierLocation(value, 7),
            new CarrierLocation(value, 3),
            new CarrierLocation(value, 6),
            residual,
            measure,
            out var choices,
            out _);

        Assert.True(ProximityTournament.TryReadWinner(proximity, out var winner));
        Assert.True(ProximityTournament.TryReadChoice(
            winner,
            out var winnerValue,
            out _,
            out _,
            out var winnerBoundary,
            out var winnerDistance,
            out var winnerBoundaryViolation,
            out var winnerSelection));
        Assert.Equal(new CarrierLocation(value, 6), Assert.IsType<CarrierLocation>(winnerValue));
        Assert.Equal(new CarrierLocation(residual, 1), winnerDistance);
        Assert.Equal(new CarrierLocation(residual, 0), winnerBoundaryViolation);
        Assert.Equal(new CarrierLocation(measure, 1), winnerSelection);
        Assert.True(OperationClaim.TryReadBetween(
            Assert.IsType<Relation>(winnerBoundary),
            out _,
            out _,
            out _,
            out _,
            out _,
            out var winnerTotalViolation,
            out _));
        Assert.Equal(new CarrierLocation(residual, 0), winnerTotalViolation);

        Assert.True(ProximityTournament.TryReadChoice(
            choices[2],
            out _,
            out _,
            out _,
            out var outsideBoundary,
            out var outsideDistance,
            out var outsideBoundaryViolation,
            out var outsideSelection));
        Assert.Equal(new CarrierLocation(residual, 1), outsideDistance);
        Assert.Equal(new CarrierLocation(residual, 2), outsideBoundaryViolation);
        Assert.Equal(13, outsideSelection.Ticks);
        Assert.True(OperationClaim.TryReadBetween(
            Assert.IsType<Relation>(outsideBoundary),
            out _,
            out _,
            out _,
            out _,
            out var upperViolation,
            out var totalViolation,
            out _));
        Assert.Equal(new CarrierLocation(residual, 2), upperViolation);
        Assert.Equal(new CarrierLocation(residual, 2), totalViolation);
    }

    private static IElement[] OrderedElements(Relation structure)
    {
        var holder = Assert.IsType<Carrier>(structure.Right);
        Assert.True(structure.TryReadOrderedElementsOn(holder, out var elements));
        return elements;
    }

    private static IElement[] ChoiceValues(Relation entryGroup)
    {
        return [.. OrderedElements(entryGroup).Select(entry =>
        {
            var relation = Assert.IsType<Relation>(entry);
            Assert.True(ExtremaTournament.TryReadEntry(relation, out var value, out _));
            return value;
        })];
    }
}
