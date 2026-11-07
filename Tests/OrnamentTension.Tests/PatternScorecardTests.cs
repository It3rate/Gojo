using Gojo.Experiments.OrnamentFlowPilot;
using Gojo.Experiments.OrnamentPathCampaign;
using Gojo.Experiments.PrimitiveMeasure;

namespace Gojo.Tests.OrnamentTension;

public class PatternScorecardTests
{
    [Fact]
    public void MissingMaterialKeepsTheSourceSitesAndDoesNotBridgeItsGap()
    {
        var course = FretSymmetrySource.Glide();
        var complete = PatternScorecard.ReadCourse(course);
        byte[] bits = Enumerable.Repeat((byte)1, course.Length).ToArray();
        bits[4] = 0;
        var partial = PatternScorecard.ReadCourse(course, new ParticipationStrip(bits));
        Assert.Equal(complete.Sites, partial.Sites);
        Assert.Equal(16, partial.StrokeSlots);
        Assert.Equal(15, partial.DepositedStrokes);
        Assert.Equal(complete.Strokes[4] with { Participates = false }, partial.Strokes[4]);
        Assert.Equal(complete.Strokes[5], partial.Strokes[5]);
        Assert.Equal(2, partial.NativeComponents);
        Assert.Equal(4, partial.NativeEnds);
        Assert.Equal(new PatternCount(13, 15), partial.Handoffs);
        Assert.True(partial.PeriodicSeam);
        Assert.Null(partial.Windows);
        Assert.Null(partial.Gaps);
        Assert.NotNull(partial.DescriptorLimit);
        var tests = PatternScorecard.Evaluate(partial, "participation", "handoffs", "glide");
        Assert.Equal(1, tests[0].Misses);
        Assert.Equal(2, tests[1].Misses);
        Assert.Null(tests[2].Passed);
        Assert.Equal(0, tests[2].Checked);
    }

    [Fact]
    public void AbsentEndMaterialFailsTheSeamWithoutRemovingTimingSlots()
    {
        var course = FretSymmetrySource.Glide();
        byte[] bits = Enumerable.Repeat((byte)1, course.Length).ToArray();
        bits[^1] = 0;
        var partial = PatternScorecard.ReadCourse(course, new ParticipationStrip(bits));
        Assert.False(partial.PeriodicSeam);
        Assert.Equal(15, partial.Strokes[^1].Beat);
        Assert.Equal(16, partial.Strokes[^1].To);
        Assert.False(Assert.Single(PatternScorecard.Evaluate(partial, "seam")).Passed);
    }

    [Fact]
    public void AGeometricCrossingDoesNotBecomeNativeContactOrOverdraw()
    {
        var trace = new PathTrace([new(-1, -1), new(1, 1), new(-1, 1), new(1, -1)],
            [new(0, 1, 0, 0, 0, 0, 1, 0), new(2, 3, 1, 0, 0, 0, 7, 0)], [], 2, 1, 2);
        var cross = PatternScorecard.Read(trace);
        Assert.Equal(1, cross.InteriorCrossings);
        Assert.Equal(0, cross.CollinearOverlaps);
        Assert.Equal(0, cross.UnjoinedContacts);
        Assert.Equal(2, cross.NativeComponents);
        Assert.True(Assert.Single(PatternScorecard.Evaluate(cross, "no-overdraw")).Passed);
        Assert.Null(cross.PeriodicSeam);
        Assert.Null(Assert.Single(PatternScorecard.Evaluate(cross, "seam")).Passed);
    }

    [Fact]
    public void ExactCoincidenceDoesNotSubstituteForANativeJoin()
    {
        var unjoined = new PathTrace([new(0, 0), new(2, 0), new(2, 0), new(2, 2)],
            [new(0, 1, 0, 0, 0, 0, 0, 0), new(2, 3, 0, 1, 0, 1, 2, 0)], [], 2, 1, 1);
        var a = PatternScorecard.Read(unjoined);
        var b = PatternScorecard.Read(unjoined with { Edges = [unjoined.Edges[0], unjoined.Edges[1] with { From = 1 }] });
        Assert.Equal(1, a.UnjoinedContacts);
        Assert.Equal(2, a.NativeComponents);
        Assert.Equal(0, a.Handoffs.Met);
        Assert.Equal(0, b.UnjoinedContacts);
        Assert.Equal(1, b.NativeComponents);
        Assert.Equal(1, b.Handoffs.Met);
    }

    [Fact]
    public void DuplicateSupportIsMeasuredEvenWhenItUsesTheSameNativeSites()
    {
        var trace = new PathTrace([new(0, 0), new(4, 0)],
            [new(0, 1, 0, 0, 0, 0, 0, 0), new(0, 1, 1, 0, 0, 0, 0, 0)], [], 4, 1, 2);
        var read = PatternScorecard.Read(trace);
        Assert.Equal(1, read.CollinearOverlaps);
        Assert.Equal(0, read.InteriorCrossings);
        Assert.Equal(1, read.NativeComponents);
        var obligation = Assert.Single(PatternScorecard.Evaluate(read, "no-overdraw"));
        Assert.Equal(1, obligation.Checked);
        Assert.Equal(1, obligation.Misses);
    }

    [Fact]
    public void AStretchedReachKeepsTurnEvidenceWhileChangingWholeCourseAndWhiteSpace()
    {
        var original = PatternScorecard.ReadCourse(FretSymmetrySource.Glide());
        var changed = PatternScorecard.ReadCourse(FretSymmetrySource.Direct(2));
        Assert.Equal(original.Course!.TurnWord, changed.Course!.TurnWord);
        Assert.Equal(2, original.Gaps!.Regions.Length);
        Assert.Equal(4, changed.Gaps!.Regions.Length);
        var first = PatternScorecard.Evaluate(original, "glide", "common-nearest-gap", "no-sealed-regions");
        var second = PatternScorecard.Evaluate(changed, "glide", "common-nearest-gap", "no-sealed-regions");
        Assert.All(first, check => Assert.True(check.Passed));
        Assert.False(second[0].Passed);
        Assert.Equal(4, second[1].Misses);
        Assert.Equal(2, second[2].Misses);
    }

    [Fact]
    public void ReflectionPreservesTheGapObligationsAndReversesTurningHand()
    {
        var source = FretSymmetrySource.Glide();
        var original = PatternScorecard.ReadCourse(source);
        var reflected = PatternScorecard.ReadCourse(source.Select(p => new PathSite(p.X, -p.Y)).ToArray());
        Assert.Equal(original.NativeComponents, reflected.NativeComponents);
        Assert.Equal(original.Gaps!.Regions.Select(r => r.Area).Order(), reflected.Gaps!.Regions.Select(r => r.Area).Order());
        Assert.Equal(new string(original.Course!.TurnWord.Select(c => c == 'L' ? 'R' : 'L').ToArray()), reflected.Course!.TurnWord);
        Assert.All(PatternScorecard.Evaluate(reflected, "seam", "common-nearest-gap", "alternating-mouths", "glide"), c => Assert.True(c.Passed));
    }

    [Fact]
    public void BacktrackingHasAValidSupportReadWithoutInventingCorners()
    {
        var read = PatternScorecard.ReadCourse([new(2, 0), new(-2, 0), new(2, 0)]);
        Assert.Equal(3, read.CollinearOverlaps);
        Assert.Null(read.Course);
        Assert.NotNull(read.DescriptorLimit);
        Assert.NotNull(read.Windows);
        Assert.NotNull(read.Gaps);
    }

    [Fact]
    public void NeighborCopyCanOverlapEvenWhenTheSingleBayDoesNot()
    {
        PathSite[] course = [new(3, 0), new(0, 1), new(-2, 0), new(0, -1)];
        var single = PatternScorecard.ReadCourse(course);
        var repeated = PatternScorecard.ReadRepeatedCourse(course);
        Assert.Equal(0, single.CollinearOverlaps);
        Assert.True(repeated.CollinearOverlaps > 0);
        Assert.Equal(8, repeated.StrokeSlots);
        Assert.Equal(7, repeated.Handoffs.Met);
        Assert.Equal(repeated.Strokes[3].To, repeated.Strokes[4].From);
        Assert.Equal(new PathSite(1, 0), repeated.Sites[repeated.Strokes[4].From]);
        Assert.Null(repeated.PeriodicSeam);
        Assert.Null(repeated.Windows);
        Assert.Null(repeated.Gaps);
        Assert.False(Assert.Single(PatternScorecard.Evaluate(repeated, "no-overdraw")).Passed);
    }

    [Fact]
    public void MaskingTheNeighborSeamPreservesItsPositionsAndTheFollowingCopy()
    {
        PathSite[] course = [new(2, 0), new(0, 1), new(2, 0), new(0, -1)];
        var all = PatternScorecard.ReadRepeatedCourse(course);
        var mask = new ParticipationStrip(1, 1, 1, 0, 1, 1, 1, 1);
        var gap = PatternScorecard.ReadRepeatedCourse(course, deposit: mask);
        Assert.Equal(all.Sites, gap.Sites);
        Assert.Equal(all.Strokes[4], gap.Strokes[4]);
        Assert.Equal(5, gap.Handoffs.Met);
        Assert.Equal(2, gap.NativeComponents);
        Assert.Equal(0, gap.CollinearOverlaps);
    }
}
