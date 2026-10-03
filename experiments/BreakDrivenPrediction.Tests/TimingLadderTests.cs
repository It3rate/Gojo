using Gojo.Core;
using Gojo.Structures;
using C = BreakDrivenPrediction.Continuation;
using L = BreakDrivenPrediction.TimingLadder;
using S = BreakDrivenPrediction.TimingSource;

namespace BreakDrivenPrediction.Tests;

public sealed class TimingLadderTests
{
    [Theory]
    [InlineData(15, false, true, true, -1)]
    [InlineData(16, true, true, true, 0)]
    [InlineData(17, false, true, true, 1)]
    [InlineData(18, false, false, true, 2)]
    [InlineData(24, false, false, false, 8)]
    public void CoarseAgreementPreservesFineMismatchAndHalfOpenBoundaries(long at,
        bool fine, bool middle, bool coarse, long error)
    {
        var clock = new Carrier();
        var issue = L.Issue(L.Create(clock, 1, 3, 12), new CarrierLocation(clock, 12),
            new CarrierLocation(clock, 16), C.G());
        var read = L.Assess(issue, new CarrierLocation(clock, at));
        var rows = C.Parts((Relation)C.Parts(read)[2]).Cast<Relation>().ToArray();
        Assert.Equal(new[] { fine, middle, coarse }, rows.Select(L.Matches));
        Assert.All(rows, row => Assert.Equal(error, L.Difference(row).Ticks));
    }

    [Fact]
    public void ACoarseCellContainingThePastCannotEarnRetrospectiveCredit()
    {
        var clock = new Carrier();
        var issue = L.Issue(L.Create(clock, 12), new CarrierLocation(clock, 16),
            new CarrierLocation(clock, 17), C.G());
        Assert.Equal((12L, 24L), Cell(L.Rungs(issue)[0]));
        var read = L.Assess(issue, new CarrierLocation(clock, 15));
        Assert.False(L.Matches((Relation)C.Parts((Relation)C.Parts(read)[2])[0]));
    }

    [Fact]
    public void NonnestedThirteenAndTwentyOneTickFramesPreserveTheirOwnBoundaries()
    {
        var clock = new Carrier();
        var ladder = L.Create(clock, 13, 21);
        var issue = L.Issue(ladder, new CarrierLocation(clock, 24), new CarrierLocation(clock, 40), C.G());
        var rows = L.Rungs(issue);
        Assert.Equal((39L, 52L), Cell(rows[0]));
        Assert.Equal((21L, 42L), Cell(rows[1]));
        // Neither cell contains the other; no nesting is invented from rung order.
        var read = L.Assess(issue, new CarrierLocation(clock, 43));
        var assessments = C.Parts((Relation)C.Parts(read)[2]).Cast<Relation>().ToArray();
        Assert.True(L.Matches(assessments[0]));
        Assert.False(L.Matches(assessments[1]));
    }

    [Fact]
    public void RetargetingAnAuthoredMapCannotRewriteAnIssuedCell()
    {
        var clock = new Carrier();
        var ladder = L.Create(clock, 3);
        var issue = L.Issue(ladder, new CarrierLocation(clock, 12), new CarrierLocation(clock, 16), C.G());
        var authored = L.Maps(ladder)[0];
        Assert.True(TimeStepSequence.TryReadSpan((Relation)authored.Left, out _, out var width));
        width.Tick.Set(12);
        Assert.Equal((15L, 18L), Cell(L.Rungs(issue)[0]));
        var read = L.Assess(issue, new CarrierLocation(clock, 19));
        Assert.False(L.Matches((Relation)C.Parts((Relation)C.Parts(read)[2])[0]));
    }

    [Fact]
    public void UnrepresentableCellAndForeignClockRemainUnresolved()
    {
        var clock = new Carrier();
        var ladder = L.Create(clock, 1, 3, 12);
        Assert.Empty(L.Rungs(L.Issue(ladder, new CarrierLocation(clock, long.MaxValue - 1),
            new CarrierLocation(clock, long.MaxValue), C.G())));
        Assert.Empty(L.Rungs(L.Issue(ladder, new CarrierLocation(new Carrier(), 12),
            new CarrierLocation(clock, 16), C.G())));
        Assert.Empty(L.Maps(L.Create(clock, 0)));
    }

    [Fact]
    public void ReportBoundaryPreventsSilentlyDroppingAccumulatedEvents()
    {
        var clock = new Carrier();
        var source = S.Create(100, clock, 1);
        for (var t = 1; t <= 12; t++)
        {
            Assert.True(S.TryObserve(source, new CarrierLocation(clock, t),
                new CarrierLocation(S.Sensor(source), 100 + t % 2), out var next));
            source = next;
        }
        var before = GojoNumericFormat.Encode(source);
        Assert.False(S.TryObserve(source, new CarrierLocation(clock, 13),
            new CarrierLocation(S.Sensor(source), 101), out _));
        Assert.Equal(before, GojoNumericFormat.Encode(source));
        Assert.True(S.Publish(source, new CarrierLocation(clock, 12), out var published, out var packet));
        Assert.Equal(11, C.Parts((Relation)C.Parts(packet)[3]).Length);
        Assert.True(S.TryObserve(published, new CarrierLocation(clock, 13),
            new CarrierLocation(S.Sensor(published), 101), out _));
    }

    private static (long Start, long End) Cell(Relation row)
    {
        Assert.True(TimeStepSequence.TryReadSpan((Relation)C.Parts(row)[1], out var start, out var end));
        return (start.Ticks, end.Ticks);
    }
}
