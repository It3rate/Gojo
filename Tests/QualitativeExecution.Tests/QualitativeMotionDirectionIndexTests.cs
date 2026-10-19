using Gojo.Core;
using Gojo.Patterns;

namespace Gojo.Experiments.QualitativeExecution.Tests;

public sealed class QualitativeMotionDirectionIndexTests
{
    [Fact]
    public void LongOneFaceWordsKeepTheirActualOrderedOccurrencesAndRejectLostParticipation()
    {
        var frame = new QualitativeMotionFrame(); var reach = new Placeholder();
        var before = Enumerable.Range(0, 400).Select(_ => (IElement)QualitativeMotionFrame.Move(frame.Right, reach)).ToArray();
        var incoming = Enumerable.Range(0, 300).Select(_ => (IElement)QualitativeMotionFrame.Move(frame.Up, reach)).ToArray();
        var first = S(before); var next = S(incoming); var read = frame.Reduce(first, next);
        Assert.True(frame.TryReadResult(read, out var result, out var pairs, out var pending));
        Assert.Equal(before.Concat(incoming), E(result)); Assert.Empty(E(pairs)); Assert.Empty(E(pending));
        var header = Assert.IsType<Relation>(Assert.IsType<Relation>(read.Left).Right);
        Assert.Same(first, header.Left); Assert.Same(next, header.Right);
        var missing = new Relation(read.Left, new Relation(S(E(result).Skip(1).ToArray()), new Relation(pairs, pending)));
        Assert.False(frame.TryReadResult(missing, out _, out _, out _));
    }

    [Fact]
    public void DirectionOccupancyUpdatesAsOppositesAppearAndDisappearWithoutChangingPairOrder()
    {
        var frame = new QualitativeMotionFrame(); var a = new Placeholder(); var b = new Placeholder();
        var ra = QualitativeMotionFrame.Move(frame.Right, a); var rb = QualitativeMotionFrame.Move(frame.Right, b);
        var la = QualitativeMotionFrame.Move(frame.Left, a); var lb = QualitativeMotionFrame.Move(frame.Left, b);
        var up = QualitativeMotionFrame.Move(frame.Up, a); var down = QualitativeMotionFrame.Move(frame.Down, a);
        var last = QualitativeMotionFrame.Move(frame.Right, a);
        var read = frame.Reduce(S(ra, rb), S(la, up, lb, down, last));
        Assert.True(frame.TryReadResult(read, out var result, out var pairs, out var pending));
        Assert.Same(last, Assert.Single(E(result))); Assert.Empty(E(pending));
        var actual = E(pairs).Cast<Relation>().ToArray(); Assert.Equal(3, actual.Length);
        Assert.Same(ra, actual[0].Left); Assert.Same(la, actual[0].Right);
        Assert.Same(rb, actual[1].Left); Assert.Same(lb, actual[1].Right);
        Assert.Same(up, actual[2].Left); Assert.Same(down, actual[2].Right);
    }

    [Fact]
    public void BothFacesWithDifferentReachesKeepEveryNativeComparisonInItsOriginalOrder()
    {
        var frame = new QualitativeMotionFrame(); var a = new Placeholder(); var b = new Placeholder(); var c = new Placeholder();
        var ra = QualitativeMotionFrame.Move(frame.Right, a); var rb = QualitativeMotionFrame.Move(frame.Right, b);
        var up = QualitativeMotionFrame.Move(frame.Up, a); var lc = QualitativeMotionFrame.Move(frame.Left, c);
        var down = QualitativeMotionFrame.Move(frame.Down, b);
        var read = frame.Reduce(S(ra, rb, up), S(lc, down));
        Assert.True(frame.TryReadResult(read, out var result, out var pairs, out var pending));
        Assert.Equal(new IElement[] { ra, rb, up, lc, down }, E(result)); Assert.Empty(E(pairs));
        var actual = E(pending).Cast<Relation>().ToArray(); Assert.Equal(3, actual.Length);
        Assert.Same(ra, actual[0].Left); Assert.Same(lc, actual[0].Right);
        Assert.Same(rb, actual[1].Left); Assert.Same(lc, actual[1].Right);
        Assert.Same(up, actual[2].Left); Assert.Same(down, actual[2].Right);
        var hidden = new Relation(read.Left, new Relation(result, new Relation(pairs, S())));
        Assert.False(frame.TryReadResult(hidden, out _, out _, out _));
    }

    [Fact]
    public void UnknownAndForeignParticipationRemainOpenBesideOneFaceKnownMoves()
    {
        var frame = new QualitativeMotionFrame(); var foreignFrame = new QualitativeMotionFrame(); var reach = new Placeholder();
        var known = QualitativeMotionFrame.Move(frame.Right, reach);
        var foreign = QualitativeMotionFrame.Move(foreignFrame.Left, reach); var unknown = new Placeholder();
        var read = frame.Reduce(S(known), S(foreign, unknown));
        Assert.True(frame.TryReadResult(read, out var result, out var pairs, out var pending));
        Assert.Equal(new IElement[] { known, foreign, unknown }, E(result)); Assert.Empty(E(pairs));
        Assert.Equal(2, E(pending).Length);
        Assert.True(StructuralConstraint.TryReadScope(result, out _, out var seam, out var tail)); Assert.NotEqual(seam, tail);
    }

    [Fact]
    public void OccurrenceCountsPreserveRepeatedAliasesAndRejectMissingExcessOrCopiedOccurrences()
    {
        var frame = new QualitativeMotionFrame(); var reach = new Placeholder();
        var right = QualitativeMotionFrame.Move(frame.Right, reach);
        var left = QualitativeMotionFrame.Move(frame.Left, reach);
        var read = frame.Reduce(S(right, right, right), S(left));
        Assert.True(frame.TryReadResult(read, out var result, out var pairs, out var pending));
        Assert.Equal(new IElement[] { right, right }, E(result)); Assert.Single(E(pairs)); Assert.Empty(E(pending));
        Assert.False(ReadWith(S(right)));
        Assert.False(ReadWith(S(right, right, right)));
        Assert.False(ReadWith(S(right, QualitativeMotionFrame.Move(frame.Right, reach))));

        bool ReadWith(Relation survivors) => frame.TryReadResult(
            new Relation(read.Left, new Relation(survivors, new Relation(pairs, pending))), out _, out _, out _);
    }

    private static Relation S(params IElement[] entries) => StructuralConstraint.Closed(entries);
    private static IElement[] E(Relation source) => FiniteLaw.Entries(source);
}
