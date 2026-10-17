using Gojo.Core;
using Gojo.Patterns;
using Xunit;

namespace Gojo.Experiments.QualitativeExecution.Tests;

public sealed class DetectorTests
{
    [Fact]
    public void DetectorLawPublicationAndReadContainNoNumericParticipants()
    {
        var fixture = new ThresholdFixture();
        var view = fixture.Forward;
        var source = new Placeholder();
        var observation = view.Observe(source, new Placeholder(),
            StructuralConstraint.Closed(view.Above), StructuralConstraint.Closed(view.Falling));
        var native = view.ReadNative(observation);
        var compiled = view.ReadCompiled(observation);

        AssertNoNumbers(fixture.Surface);
        AssertNoNumbers(observation);
        AssertNoNumbers(native);
        AssertNoNumbers(compiled);
        AssertSupport(native, view.Approaching);
        AssertSupport(compiled, view.Approaching);
        Assert.True(Contains(native, observation));
        Assert.True(Contains(compiled, observation));
        Assert.True(Contains(fixture.Surface, fixture.Forward.Law));
        Assert.True(Contains(fixture.Surface, fixture.Reverse.CategoryChangeLaw));
    }

    [Fact]
    public void NineSideAndTrendPairsExpressFiveDistinctLocalOutcomes()
    {
        var view = new ThresholdFixture().Forward;
        var sides = new[] { view.Below, view.WithinBand, view.Above };
        var trends = new[] { view.Falling, view.Steady, view.Rising };
        Relation[,] expected =
        {
            { view.Departing, view.OutsideSteady, view.Approaching },
            { view.WithinChanging, view.WithinSteady, view.WithinChanging },
            { view.Approaching, view.OutsideSteady, view.Departing }
        };
        for (var i = 0; i < sides.Length; i++)
            for (var j = 0; j < trends.Length; j++)
            {
                var observation = view.Observe(new Placeholder(), new Placeholder(),
                    StructuralConstraint.Closed(sides[i]), StructuralConstraint.Closed(trends[j]));
                AssertSupport(view.ReadNative(observation), expected[i, j]);
                AssertSupport(view.ReadCompiled(observation), expected[i, j]);
            }
    }

    [Fact]
    public void ThresholdBandIsNotAnExactZeroAndChangingInsideItIsNotDeparting()
    {
        var view = new ThresholdFixture().Forward;
        Assert.NotSame(view.Boundary.Left, view.Boundary.Right);
        Assert.Same(view.Boundary, view.WithinBand);
        Assert.NotSame(view.WithinBand, view.Steady);
        Assert.Same(view.Steady.Left, view.Steady.Right);
        var fallingInside = view.Observe(new Placeholder(), new Placeholder(),
            StructuralConstraint.Closed(view.WithinBand), StructuralConstraint.Closed(view.Falling));

        AssertSupport(view.ReadNative(fallingInside), view.WithinChanging);
        Assert.NotSame(view.WithinChanging, view.Departing);
        Assert.NotSame(view.WithinChanging, view.WithinSteady);
    }

    [Fact]
    public void SameCategoryDoesNotEstablishStableOrAnyOtherSingleTrend()
    {
        var view = new ThresholdFixture().Forward;
        var source = new Placeholder();
        foreach (var category in new[] { view.Below, view.WithinBand, view.Above })
        {
            var earlier = view.Observe(source, new Placeholder(),
                StructuralConstraint.Closed(category), StructuralConstraint.Closed(view.Falling));
            var later = view.Observe(source, new Placeholder(),
                StructuralConstraint.Closed(category), StructuralConstraint.Closed(view.Rising));
            AssertSupport(view.CompareCategories(earlier, later), view.Falling, view.Steady, view.Rising);
        }
    }

    [Fact]
    public void AboveAboveThenWithinBandNeedsNoStoredSampleAndGivesOnlyCoarseCrossingEvidence()
    {
        var view = new ThresholdFixture().Forward;
        var source = new Placeholder();
        var first = view.Observe(source, new Placeholder(), StructuralConstraint.Closed(view.Above), view.TrendDomain);
        var second = view.Observe(source, new Placeholder(), StructuralConstraint.Closed(view.Above), view.TrendDomain);
        var third = view.Observe(source, new Placeholder(), StructuralConstraint.Closed(view.WithinBand), view.TrendDomain);
        var withinBin = view.CompareCategories(first, second);
        var crossing = view.CompareCategories(second, third);

        AssertSupport(withinBin, view.Falling, view.Steady, view.Rising);
        AssertSupport(crossing, view.Falling);
        Assert.True(Contains(crossing, second));
        Assert.True(Contains(crossing, third));
        AssertNoNumbers(new Relation(withinBin, crossing));
        // The later publication still has no instantaneous trend measurement.
        Assert.True(DetectorObservation.TryRead(third, out _, out _, out _, out _, out var trend, out _));
        Assert.Same(view.TrendDomain, trend);
    }

    [Fact]
    public void CategoryComparisonNeedsTheSameSourceFrameAndThresholdContract()
    {
        var fixture = new ThresholdFixture();
        var view = fixture.Forward;
        var source = new Placeholder();
        var earlier = view.Observe(source, new Placeholder(), StructuralConstraint.Closed(view.Above), view.TrendDomain);
        var foreignSource = view.Observe(new Placeholder(), new Placeholder(),
            StructuralConstraint.Closed(view.WithinBand), view.TrendDomain);
        var foreignFrame = fixture.Reverse.Observe(source, new Placeholder(),
            StructuralConstraint.Closed(fixture.Reverse.WithinBand), fixture.Reverse.TrendDomain);
        var movedThreshold = DetectorObservation.Create(source, new Placeholder(), view.Surface,
            new Relation(new Placeholder(), new Placeholder()), view.Resolution,
            StructuralConstraint.Closed(view.WithinBand), view.TrendDomain, StructuralConstraint.Closed());

        AssertUnresolved(view.CompareCategories(earlier, foreignSource));
        AssertUnresolved(view.CompareCategories(earlier, foreignFrame));
        AssertUnresolved(view.CompareCategories(earlier, movedThreshold));
    }

    [Fact]
    public void ReversingBothReadFramesPreservesPhysicalOutcomeAndObservationIdentity()
    {
        var fixture = new ThresholdFixture();
        var forward = fixture.Forward;
        var reverse = fixture.Reverse;
        Assert.Same(forward.Boundary, reverse.Boundary);
        Assert.Same(forward.Resolution, reverse.Resolution);
        Assert.Same(forward.Outcomes, reverse.Outcomes);
        Assert.Same(forward.Order.Left, reverse.Order.Right);
        Assert.Same(forward.Order.Right, reverse.Order.Left);
        foreach (var side in new[] { forward.Below, forward.WithinBand, forward.Above })
            foreach (var trend in new[] { forward.Falling, forward.Steady, forward.Rising })
            {
                var source = new Placeholder();
                var eventIdentity = new Placeholder();
                var observation = forward.Observe(source, eventIdentity,
                    StructuralConstraint.Closed(side), StructuralConstraint.Closed(trend));
                var reversed = fixture.ReframeToReverse(observation);
                Assert.True(DetectorObservation.TryRead(reversed, out var observedSource,
                    out var observedEvent, out var observedView, out _, out _, out var evidence));
                Assert.Same(source, observedSource);
                Assert.Same(eventIdentity, observedEvent);
                Assert.Same(reverse.Surface, observedView);
                Assert.True(Contains(evidence, observation));
                Assert.True(Contains(evidence, fixture.Correspondence));
                Assert.Equal(ResultEntries(forward.ReadNative(observation)), ResultEntries(reverse.ReadNative(reversed)));
                Assert.Equal(ResultEntries(forward.ReadNative(observation)), ResultEntries(reverse.ReadCompiled(reversed)));
            }
    }

    [Fact]
    public void UnmappedViewAndSingleFieldReversalCannotImpersonateAReframedObservation()
    {
        var fixture = new ThresholdFixture();
        var forward = fixture.Forward;
        var reverse = fixture.Reverse;
        var observation = forward.Observe(new Placeholder(), new Placeholder(),
            StructuralConstraint.Closed(forward.Above), StructuralConstraint.Closed(forward.Falling));
        AssertUnresolved(reverse.ReadNative(observation));
        AssertUnresolved(reverse.ReadCompiled(observation));
        var mixedFrames = reverse.Observe(new Placeholder(), new Placeholder(),
            StructuralConstraint.Closed(reverse.Below), StructuralConstraint.Closed(forward.Falling));
        AssertUnresolved(reverse.ReadNative(mixedFrames));
        AssertUnresolved(reverse.ReadCompiled(mixedFrames));
    }

    [Fact]
    public void LookalikeCategoriesAreNotTheSameDetectorEvidence()
    {
        var view = new ThresholdFixture().Forward;
        var lookalikeAbove = new Relation(view.Above.Left, view.Above.Right);
        Assert.Equal(view.Above, lookalikeAbove);
        Assert.NotSame(view.Above, lookalikeAbove);
        var observation = view.Observe(new Placeholder(), new Placeholder(),
            StructuralConstraint.Closed(lookalikeAbove), StructuralConstraint.Closed(view.Falling));
        AssertUnresolved(view.ReadNative(observation));
        AssertUnresolved(view.ReadCompiled(observation));
    }

    [Fact]
    public void CombiningSeparatelyPublishedEvidenceRequiresTheSameEvent()
    {
        var view = new ThresholdFixture().Forward;
        var source = new Placeholder();
        var earlier = view.Observe(source, new Placeholder(), StructuralConstraint.Closed(view.Above), view.TrendDomain);
        var later = view.Observe(source, new Placeholder(), view.SideDomain, StructuralConstraint.Closed(view.Falling));
        Assert.False(DetectorObservation.TryCombine(earlier, later, out var refused));
        Assert.Same(earlier, refused.Left);
        Assert.Same(later, refused.Right);
        AssertUnresolved(view.ReadNative(refused));
    }

    [Fact]
    public void TwoPortsForTheSameEventCanContributeSideAndTrendWithoutAMagnitude()
    {
        var view = new ThresholdFixture().Forward;
        var source = new Placeholder();
        var eventIdentity = new Placeholder();
        var sidePort = view.Observe(source, eventIdentity, StructuralConstraint.Closed(view.Above), view.TrendDomain);
        var trendPort = view.Observe(source, eventIdentity, view.SideDomain, StructuralConstraint.Closed(view.Falling));
        Assert.True(DetectorObservation.TryCombine(sidePort, trendPort, out var combined));
        AssertSupport(view.ReadNative(combined), view.Approaching);
        Assert.True(Contains(combined, sidePort));
        Assert.True(Contains(combined, trendPort));
        AssertNoNumbers(combined);
    }

    [Fact]
    public void SameEventConflictsCannotBecomeTemporalMotionOrDisappearInAPortJoin()
    {
        var view = new ThresholdFixture().Forward;
        var source = new Placeholder();
        var eventIdentity = new Placeholder();
        var above = view.Observe(source, eventIdentity,
            StructuralConstraint.Closed(view.Above), StructuralConstraint.Closed(view.Falling));
        var below = view.Observe(source, eventIdentity,
            StructuralConstraint.Closed(view.Below), StructuralConstraint.Closed(view.Falling));
        AssertUnresolved(view.CompareCategories(above, below));
        Assert.False(DetectorObservation.TryCombine(above, below, out var conflict));
        Assert.True(Contains(conflict, above));
        Assert.True(Contains(conflict, below));
        var rising = view.Observe(source, eventIdentity,
            StructuralConstraint.Closed(view.Above), StructuralConstraint.Closed(view.Rising));
        Assert.False(DetectorObservation.TryCombine(above, rising, out var trendConflict));
        Assert.True(Contains(trendConflict, above));
        Assert.True(Contains(trendConflict, rising));
    }

    [Fact]
    public void MissingOrForeignObservationContractStaysUnresolved()
    {
        var view = new ThresholdFixture().Forward;
        var malformed = new Relation(new Placeholder(), new Placeholder());
        Assert.False(DetectorObservation.TryRead(malformed, out _, out _, out _, out _, out _, out _));
        AssertUnresolved(view.ReadNative(malformed));
        AssertUnresolved(view.ReadCompiled(malformed));
        var foreignResolution = DetectorObservation.Create(new Placeholder(), new Placeholder(), view.Surface,
            view.Boundary, new Placeholder(), StructuralConstraint.Closed(view.Above),
            StructuralConstraint.Closed(view.Falling), StructuralConstraint.Closed());
        AssertUnresolved(view.ReadNative(foreignResolution));
    }

    [Fact]
    public void ReframingPreservesOpenUncertaintyAndRejectsForeignCategories()
    {
        var fixture = new ThresholdFixture();
        var view = fixture.Forward;
        var open = view.Observe(new Placeholder(), new Placeholder(),
            StructuralConstraint.Open(view.Above), StructuralConstraint.Closed(view.Falling));
        var reversed = fixture.ReframeToReverse(open);
        Assert.True(DetectorObservation.TryRead(reversed, out _, out _, out _, out var sides, out _, out _));
        Assert.True(StructuralConstraint.TryReadScope(sides, out var known, out var seam, out var tail));
        Assert.Equal([fixture.Reverse.Below], known);
        Assert.NotEqual(seam, tail);
        AssertUnresolved(fixture.Reverse.ReadNative(reversed));
        var foreign = view.Observe(new Placeholder(), new Placeholder(),
            StructuralConstraint.Closed(view.Above, new Placeholder()), view.TrendDomain);
        var mappedForeign = fixture.ReframeToReverse(foreign);
        Assert.True(DetectorObservation.TryRead(mappedForeign,
            out _, out _, out _, out var partiallyMapped, out _, out var foreignEvidence));
        Assert.True(StructuralConstraint.TryReadScope(partiallyMapped, out var retained, out var partialSeam, out var partialTail));
        Assert.Equal([fixture.Reverse.Below], retained);
        Assert.NotEqual(partialSeam, partialTail);
        Assert.True(Contains(foreignEvidence, foreign));
        AssertUnresolved(fixture.Reverse.ReadNative(mappedForeign));
    }

    private static void AssertSupport(Relation read, params IElement[] expected)
    {
        Assert.True(FiniteLaw.TryReadResult(read, out var support, out var residue, out _));
        Assert.True(StructuralConstraint.TryReadScope(support, out var actual, out var seam, out var tail));
        Assert.Equal(seam, tail);
        Assert.Equal(expected, actual);
        Assert.Empty(Entries(residue));
    }

    private static IElement[] ResultEntries(Relation read)
    {
        Assert.True(FiniteLaw.TryReadResult(read, out var support, out _, out _));
        return Entries(support);
    }

    private static void AssertUnresolved(Relation read)
    {
        Assert.True(FiniteLaw.TryReadResult(read, out _, out var residue, out _));
        Assert.NotEmpty(Entries(residue));
    }

    private static IElement[] Entries(Relation scope)
    {
        Assert.True(StructuralConstraint.TryReadScope(scope, out var entries, out _, out _));
        return entries;
    }

    private static void AssertNoNumbers(IElement root)
    {
        foreach (var element in Traverse(root))
        {
            Assert.IsNotType<Carrier>(element);
            Assert.IsNotType<CarrierLocation>(element);
        }
    }

    private static bool Contains(IElement root, IElement target) =>
        Traverse(root).Any(element => ReferenceEquals(element, target));

    private static IEnumerable<IElement> Traverse(IElement root)
    {
        var seen = new HashSet<IElement>(ReferenceEqualityComparer.Instance);
        var pending = new Stack<IElement>();
        pending.Push(root);
        while (pending.TryPop(out var element))
        {
            if (!seen.Add(element)) continue;
            yield return element;
            if (element is Relation relation)
            {
                pending.Push(relation.Left);
                pending.Push(relation.Right);
            }
        }
    }
}
