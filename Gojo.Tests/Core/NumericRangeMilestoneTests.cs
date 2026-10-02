using Gojo.Core;
using Gojo.Structures;
using Gojo.Equations;

namespace Gojo.Tests.Core;

public sealed class NumericRangeMilestoneTests
{
    [Fact]
    public void ProximityScoresDoNotCollapseDifferentRanksThroughSaturation()
    {
        var value = new Carrier(); var residual = new Carrier(); var measure = new Carrier();
        IElement[] choices = [new CarrierLocation(value, long.MaxValue), new CarrierLocation(value, 0)];
        var nearest = ProximityTournament.Nearest(new Carrier(), choices, new CarrierLocation(value, 0),
            residual, measure, out _, out _);
        Assert.True(ProximityTournament.TryReadWinner(nearest, out _));
        Assert.Throws<OverflowException>(() => ProximityTournament.NearestAtMost(new Carrier(), choices,
            new CarrierLocation(value, 0), residual, measure, out _, out _));
    }

    [Fact]
    public void ExactClaimsFollowAndFitDoNotReportSaturatedAnswers()
    {
        var source = new Carrier(); var error = new Carrier();
        Assert.Throws<OverflowException>(() => OperationClaim.Equality(new Carrier(),
            new CarrierLocation(source, 0), new CarrierLocation(source, long.MinValue), error));
        Assert.Throws<OverflowException>(() => OperationFollow.Step(new Carrier(),
            new CarrierLocation(source, long.MaxValue - 1), new CarrierLocation(source, long.MaxValue), error, 2, 1));
        Assert.Throws<OverflowException>(() => EquationInspection.SummarizeFit(new Carrier(),
            [new CarrierLocation(error, long.MaxValue), new CarrierLocation(error, -1)], new Carrier(), new Carrier(), out _));
        Assert.Equal(0UL, source.Value);
        Assert.Equal(0, source.Tension);
    }

    [Fact]
    public void IncomparableSamplesRetainTheirResidualInsteadOfReportingZeroError()
    {
        var a = new Carrier(); var b = new Carrier(); var error = new Carrier();
        EquationInspection.Compare(new Carrier(), [new CarrierLocation(a, 7)], [new CarrierLocation(b, 7)], error, out var samples);
        var sample = Assert.Single(samples);
        Assert.False(EquationInspection.TryReadComparisonSample(sample, out _, out _, out _, out _));
        Assert.True(EquationInspection.TryReadComparisonClaim(sample, out var claim));
        Assert.True(OperationClaim.TryRead(claim, out _, out var residual, out _, out _, out _, out var residue));
        Assert.IsType<Relation>(residual);
        Assert.Equal(new Relation(new CarrierLocation(a, 7), new CarrierLocation(b, 7)), residue);
    }

    [Fact]
    public void WideCandidateInterpolationDoesNotOverflowBeforeDivision()
    {
        var input = new Carrier(); var output = new Carrier();
        var equation = Equation.Affine(input, 0, 1, output, 0, 1);
        EquationResolutionSearch.Search(new Carrier(), equation, input, output, 0, 0, long.MaxValue,
            candidateCount: 5, roundCount: 1, out var rounds);
        Assert.True(EquationResolutionSearch.TryReadSelectedSample(Assert.Single(rounds), out var sample));
        Assert.True(EquationResolutionSearch.TryReadCandidateSample(sample, out _, out var selected,
            out _, out _, out _, out _, out _));
        Assert.Equal(0, selected.Ticks);
    }

    [Fact]
    public void MinimumDrawIsRepresentableAndRejectedOverflowLeavesBothStateFieldsUntouched()
    {
        var carrier = new Carrier();
        carrier.Set(long.MinValue);
        Assert.Equal(long.MinValue, carrier.Tension);
        carrier.Add((ulong)long.MaxValue);
        Assert.Equal(-1, carrier.Tension);
        carrier.Add(1);
        Assert.Equal(0, carrier.Tension);
        carrier.Set(long.MaxValue);
        Assert.Throws<OverflowException>(() => carrier.Add(1));
        Assert.Equal((ulong)long.MaxValue, carrier.Value);
        Assert.Equal(0, carrier.Tension);
        carrier.Remove(ulong.MaxValue);
        Assert.Equal(0UL, carrier.Value);
        Assert.Equal(long.MinValue, carrier.Tension);
    }

    [Fact]
    public void ExactLadderReadsDoNotSaturateAndWideContributionsCanCancel()
    {
        var levels = ResolutionLadder.CreateLevels(new Carrier(), 2);
        levels[0].Set(long.MaxValue); levels[1].Set(long.MaxValue);
        Assert.Equal(0, ResolutionLadder.ReadComposite(levels, [2, -2]));
        Assert.Throws<OverflowException>(() => ResolutionLadder.ReadComposite(levels, [1, 1]));
        Assert.Throws<OverflowException>(() => ResolutionLadder.SpanFromRadix(2, long.MaxValue));
        Assert.Equal(new[] { 1L, long.MaxValue }, ResolutionLadder.RepeatedRadixScales(2, long.MaxValue));
        Assert.Throws<OverflowException>(() => ResolutionLadder.RepeatedRadixScales(3, long.MaxValue));
        var empty = new Carrier();
        Assert.Throws<OverflowException>(() => ResolutionLadder.AddToLevel([empty], 0, ulong.MaxValue));
        Assert.Equal(0UL, empty.Value);
        Assert.Equal(0, empty.Tension);
    }
}
