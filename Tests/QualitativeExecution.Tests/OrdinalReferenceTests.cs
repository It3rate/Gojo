using Gojo.Core;
using Gojo.Patterns;

namespace Gojo.Experiments.QualitativeExecution.Tests;

public sealed class OrdinalReferenceTests
{
    [Fact]
    public void FixedThresholdLastStepAndLongerBaselineKeepTheirOwnReferences()
    {
        var fixedFrame = new OrdinalReferenceFrame();
        var stepFrame = new OrdinalReferenceFrame(resolution: new Placeholder());
        var windowFrame = new OrdinalReferenceFrame(resolution: new Placeholder());
        var source = new Placeholder();
        var fixedReference = new Placeholder();
        var early = new Placeholder();
        var previous = new Placeholder();
        var current = new Placeholder();
        var fixedContext = new Relation(fixedReference, current);
        var stepContext = new Relation(previous, current);
        var windowContext = new Relation(early, current);
        var aboveThreshold = fixedFrame.Observe(source, current, fixedReference,
            fixedContext, StructuralConstraint.Closed(fixedFrame.Higher));
        var lastStepDown = stepFrame.Observe(source, current, previous,
            stepContext, StructuralConstraint.Closed(stepFrame.Lower));
        var longerBaselineUp = windowFrame.Observe(source, current, early,
            windowContext, StructuralConstraint.Closed(windowFrame.Higher));

        AssertSupport(fixedFrame, aboveThreshold, fixedFrame.Higher);
        AssertSupport(stepFrame, lastStepDown, stepFrame.Lower);
        AssertSupport(windowFrame, longerBaselineUp, windowFrame.Higher);
        var coexistence = StructuralConstraint.Closed(aboveThreshold, lastStepDown, longerBaselineUp);
        Assert.True(Contains(coexistence, fixedContext));
        Assert.True(Contains(coexistence, stepContext));
        Assert.True(Contains(coexistence, windowContext));
        AssertNoNumbers(coexistence);
        Assert.False(stepFrame.TryRead(stepFrame.Compose(lastStepDown, longerBaselineUp),
            out _, out _, out _, out _, out _, out _));
    }

    [Theory]
    [InlineData(0, 0, 0)]
    [InlineData(0, 1, 0)]
    [InlineData(0, 2, -1)]
    [InlineData(1, 0, 0)]
    [InlineData(1, 1, 1)]
    [InlineData(1, 2, 2)]
    [InlineData(2, 0, -1)]
    [InlineData(2, 1, 2)]
    [InlineData(2, 2, 2)]
    public void ExactOrdinalRowsComposeThroughSharedPivot(int firstIndex, int secondIndex, int expectedIndex)
    {
        var frame = new OrdinalReferenceFrame();
        var states = new[] { frame.Lower, frame.Equal, frame.Higher };
        var source = new Placeholder();
        var a = new Placeholder();
        var b = new Placeholder();
        var c = new Placeholder();
        var first = frame.Observe(source, a, b, new Relation(a, b),
            StructuralConstraint.Closed(states[firstIndex]));
        var second = frame.Observe(source, b, c, new Relation(b, c),
            StructuralConstraint.Closed(states[secondIndex]));
        var result = frame.Compose(first, second);
        AssertSupport(frame, result, expectedIndex < 0 ? states : [states[expectedIndex]]);
        Assert.True(frame.TryRead(result, out var readSource, out var subject,
            out var reference, out var context, out _, out var evidence));
        Assert.Same(source, readSource);
        Assert.Same(a, subject);
        Assert.Same(c, reference);
        Assert.IsType<Relation>(context);
        Assert.True(Contains(evidence, first));
        Assert.True(Contains(evidence, second));
        Assert.True(Contains(evidence, b));
        Assert.True(Contains(evidence, frame.CompositionLaw));
        AssertNoNumbers(result);
    }

    [Fact]
    public void TwoSamplesAboveTheSameReferenceDoNotOrderEachOther()
    {
        var frame = new OrdinalReferenceFrame();
        var source = new Placeholder();
        var firstSample = new Placeholder();
        var secondSample = new Placeholder();
        var marker = new Placeholder();
        var firstAbove = frame.Observe(source, firstSample, marker,
            new Relation(marker, firstSample), StructuralConstraint.Closed(frame.Higher));
        var secondAbove = frame.Observe(source, secondSample, marker,
            new Relation(marker, secondSample), StructuralConstraint.Closed(frame.Higher));
        Assert.False(frame.TryRead(frame.Compose(firstAbove, secondAbove),
            out _, out _, out _, out _, out _, out _));
        // An independently exposed comparison is new evidence, not an answer
        // recovered from the two threshold categories.
        var directComparison = frame.Observe(source, secondSample, firstSample,
            new Relation(firstSample, secondSample), StructuralConstraint.Closed(frame.Lower));
        AssertSupport(frame, directComparison, frame.Lower);
        AssertSupport(frame, firstAbove, frame.Higher);
        AssertSupport(frame, secondAbove, frame.Higher);
    }

    [Fact]
    public void CompatibleComparisonsAcrossDifferentWindowsCanComposeThroughCurrentSample()
    {
        var frame = new OrdinalReferenceFrame();
        var source = new Placeholder();
        var early = new Placeholder();
        var previous = new Placeholder();
        var current = new Placeholder();
        var stepContext = new Relation(previous, current);
        var longerContext = new Relation(early, current);
        var previousAboveCurrent = frame.Observe(source, previous, current,
            stepContext, StructuralConstraint.Closed(frame.Higher));
        var currentAboveEarly = frame.Observe(source, current, early,
            longerContext, StructuralConstraint.Closed(frame.Higher));

        var previousAboveEarly = frame.Compose(previousAboveCurrent, currentAboveEarly);
        AssertSupport(frame, previousAboveEarly, frame.Higher);
        Assert.True(frame.TryRead(previousAboveEarly, out _, out var subject,
            out var reference, out var context, out _, out var evidence));
        Assert.Same(previous, subject);
        Assert.Same(early, reference);
        Assert.True(Contains(context, stepContext));
        Assert.True(Contains(context, longerContext));
        Assert.True(Contains(evidence, previousAboveCurrent));
        Assert.True(Contains(evidence, currentAboveEarly));
    }

    [Fact]
    public void FallingFromAnAboveThresholdSampleDoesNotLocateCurrentSampleAgainstThreshold()
    {
        var frame = new OrdinalReferenceFrame();
        var source = new Placeholder();
        var previous = new Placeholder();
        var current = new Placeholder();
        var threshold = new Placeholder();
        var currentBelowPrevious = frame.Observe(source, current, previous,
            new Relation(previous, current), StructuralConstraint.Closed(frame.Lower));
        var previousAboveThreshold = frame.Observe(source, previous, threshold,
            new Relation(threshold, previous), StructuralConstraint.Closed(frame.Higher));

        var currentVersusThreshold = frame.Compose(currentBelowPrevious, previousAboveThreshold);
        AssertSupport(frame, currentVersusThreshold, frame.Lower, frame.Equal, frame.Higher);
        Assert.True(frame.TryRead(currentVersusThreshold, out _, out var subject,
            out var reference, out _, out _, out _));
        Assert.Same(current, subject);
        Assert.Same(threshold, reference);
    }

    [Fact]
    public void FrameResolutionSourceAndPivotArePartOfTheCompositionContract()
    {
        var frame = new OrdinalReferenceFrame();
        var otherFrame = new OrdinalReferenceFrame(frame.Order, frame.Resolution);
        var otherResolution = new OrdinalReferenceFrame(frame.Order, new Placeholder());
        var source = new Placeholder();
        var a = new Placeholder();
        var pivot = new Relation(new Placeholder(), new Placeholder());
        var c = new Placeholder();
        var first = frame.Observe(source, a, pivot, new Placeholder(),
            StructuralConstraint.Closed(frame.Higher));
        Relation[] incompatible =
        [
            otherFrame.Observe(source, pivot, c, new Placeholder(), StructuralConstraint.Closed(otherFrame.Higher)),
            otherResolution.Observe(source, pivot, c, new Placeholder(), StructuralConstraint.Closed(otherResolution.Higher)),
            frame.Observe(new Placeholder(), pivot, c, new Placeholder(), StructuralConstraint.Closed(frame.Higher)),
            frame.Observe(source, new Relation(pivot.Left, pivot.Right), c, new Placeholder(), StructuralConstraint.Closed(frame.Higher))
        ];
        foreach (var second in incompatible)
        {
            var refused = frame.Compose(first, second);
            Assert.False(frame.TryRead(refused, out _, out _, out _, out _, out _, out _));
            Assert.Same(first, refused.Left);
            Assert.Same(second, refused.Right);
        }
    }

    [Fact]
    public void ADetectorBandCannotImpersonateExactOrdinalEquality()
    {
        var frame = new OrdinalReferenceFrame();
        var band = new ThresholdFixture().Forward.WithinBand;
        var source = new Placeholder();
        var a = new Placeholder();
        var b = new Placeholder();
        var c = new Placeholder();
        var offeredBand = frame.Observe(source, a, b, new Placeholder(), StructuralConstraint.Closed(band));
        var next = frame.Observe(source, b, c, new Placeholder(), StructuralConstraint.Closed(frame.Higher));
        Assert.True(frame.TryRead(frame.Compose(offeredBand, next),
            out _, out _, out _, out _, out var support, out var evidence));
        Assert.True(StructuralConstraint.TryReadScope(support, out var entries, out var seam, out var tail));
        Assert.Empty(entries);
        Assert.NotEqual(seam, tail);
        Assert.True(FiniteLaw.TryReadResult((Relation)evidence, out _, out var residue, out _));
        Assert.NotEmpty(FiniteLaw.Entries(residue));
    }

    [Fact]
    public void OpenComparisonRemainsOpenAndClosedEmptyRemainsContradiction()
    {
        var frame = new OrdinalReferenceFrame();
        var source = new Placeholder();
        var a = new Placeholder();
        var b = new Placeholder();
        var c = new Placeholder();
        var open = frame.Observe(source, a, b, new Placeholder(), StructuralConstraint.Open(frame.Higher));
        var impossible = frame.Observe(source, a, b, new Placeholder(), StructuralConstraint.Closed());
        var next = frame.Observe(source, b, c, new Placeholder(), StructuralConstraint.Closed(frame.Higher));
        Assert.True(frame.TryRead(frame.Compose(open, next),
            out _, out _, out _, out _, out var openSupport, out _));
        Assert.True(StructuralConstraint.TryReadScope(openSupport, out var known, out var seam, out var tail));
        Assert.Equal([frame.Higher], known);
        Assert.NotEqual(seam, tail);
        AssertSupport(frame, frame.Compose(impossible, next));
    }

    [Fact]
    public void NativeAndCompiledOrdinalCompositionAgreeForEveryClosedSupportPair()
    {
        var frame = new OrdinalReferenceFrame();
        var states = new[] { frame.Lower, frame.Equal, frame.Higher };
        var source = new Placeholder();
        var a = new Placeholder();
        var b = new Placeholder();
        var c = new Placeholder();
        for (var firstMask = 0; firstMask < 8; firstMask++)
            for (var secondMask = 0; secondMask < 8; secondMask++)
            {
                var firstSupport = StructuralConstraint.Closed(states.Where((_, index) =>
                    (firstMask & (1 << index)) != 0).Cast<IElement>().ToArray());
                var secondSupport = StructuralConstraint.Closed(states.Where((_, index) =>
                    (secondMask & (1 << index)) != 0).Cast<IElement>().ToArray());
                var first = frame.Observe(source, a, b, new Placeholder(), firstSupport);
                var second = frame.Observe(source, b, c, new Placeholder(), secondSupport);
                Assert.True(frame.TryRead(frame.Compose(first, second),
                    out _, out _, out _, out _, out var nativeSupport, out _));
                Assert.True(frame.TryRead(frame.Compose(first, second, compiled: true),
                    out _, out _, out _, out _, out var compiledSupport, out _));
                AssertSameParticipants(FiniteLaw.Entries(nativeSupport), FiniteLaw.Entries(compiledSupport));
                Assert.True(frame.Plan.TryEvaluateMasks((byte)firstMask, (byte)secondMask, out var resultMask));
                var expectedMask = FiniteLaw.Entries(nativeSupport).Aggregate(0, (mask, participant) =>
                    mask | (1 << Array.FindIndex(states, state => ReferenceEquals(state, participant))));
                Assert.Equal(expectedMask, resultMask);
            }
    }

    private static void AssertSupport(OrdinalReferenceFrame frame, Relation publication, params IElement[] expected)
    {
        Assert.True(frame.TryRead(publication, out _, out _, out _, out _, out var support, out _));
        Assert.True(StructuralConstraint.TryReadScope(support, out var actual, out var seam, out var tail));
        Assert.Equal(seam, tail);
        AssertSameParticipants(expected, actual);
    }

    private static void AssertSameParticipants(IEnumerable<IElement> expected, IEnumerable<IElement> actual)
    {
        var expectedEntries = expected.ToArray();
        var actualEntries = actual.ToArray();
        Assert.Equal(expectedEntries.Length, actualEntries.Length);
        foreach (var entry in expectedEntries)
            Assert.Contains(actualEntries, candidate => ReferenceEquals(entry, candidate));
    }

    private static void AssertNoNumbers(IElement root)
    {
        foreach (var participant in Traverse(root))
        {
            Assert.IsNotType<Carrier>(participant);
            Assert.IsNotType<CarrierLocation>(participant);
        }
    }

    private static bool Contains(IElement root, IElement target) =>
        Traverse(root).Any(participant => ReferenceEquals(participant, target));

    private static IEnumerable<IElement> Traverse(IElement root)
    {
        var seen = new HashSet<IElement>(ReferenceEqualityComparer.Instance);
        var pending = new Stack<IElement>();
        pending.Push(root);
        while (pending.TryPop(out var current))
        {
            if (!seen.Add(current)) continue;
            yield return current;
            if (current is not Relation relation) continue;
            pending.Push(relation.Left);
            pending.Push(relation.Right);
        }
    }
}
