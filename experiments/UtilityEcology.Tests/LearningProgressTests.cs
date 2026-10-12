using CompositeEmergence;
using Gojo.Core;
using Gojo.Structures;
using PrimitiveLearning;

namespace UtilityEcology.Tests;

public sealed class LearningProgressTests
{
    [Fact]
    public void NoKnownErrorStaysOpenWhilePhysicalEventsEstablishCoverage()
    {
        var monitor = Create();
        AssertOpen(monitor.ReadProgress()); AssertOpen(monitor.ReadCoverageChange()); AssertOpen(monitor.ReadRecentError());
        Assert.Equal(0UL, monitor.ObservationCount); Assert.Equal(0UL, monitor.Version);
        Feed(monitor, Enumerable.Repeat<long?>(null, 32));
        Assert.Equal(32UL, monitor.ObservationCount); Assert.Equal(1UL, monitor.Version);
        AssertOpen(monitor.ReadProgress()); AssertOpen(monitor.ReadRecentError());
        Assert.Equal(0, Value(monitor.ReadCoverageChange(), monitor.CoverageAxis));
        var latestBlock = Assert.IsType<Relation>(NativeGraph.Parts(monitor.Root, 14)[3]);
        var block = NativeGraph.Parts(latestBlock, 6);
        Assert.Equal(16UL, Assert.IsType<Carrier>(block[2]).Value);
        Assert.Equal(0UL, Assert.IsType<Carrier>(block[3]).Value);
        foreach (var admitted in NativeGraph.Parts(Assert.IsType<Relation>(block[1]))) AssertOpen(Assert.IsType<Relation>(admitted));
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 256)]
    [InlineData(2, 512)]
    public void KnownFlatErrorIsDistinctFromUnknownAndKeepsItsRawDiscrepancyUnit(long error, long mean)
    {
        var monitor = Create(); Feed(monitor, Enumerable.Repeat<long?>(error, 32));
        Assert.Equal(0, Value(monitor.ReadProgress(), monitor.ProgressAxis));
        Assert.Equal(mean, Value(monitor.ReadRecentError(), monitor.MeanErrorAxis));
        Assert.Equal(0, Value(monitor.ReadCoverageChange(), monitor.CoverageAxis));
    }

    [Fact]
    public void OnlyAdjacentCompletedDisjointBlocksChangeTheComparisonEpoch()
    {
        var monitor = Create(); Feed(monitor, Enumerable.Repeat<long?>(2, 16));
        Assert.Equal(0UL, monitor.Version); AssertOpen(monitor.ReadProgress()); AssertOpen(monitor.ReadCoverageChange());
        Assert.Equal(512, Value(monitor.ReadRecentError(), monitor.MeanErrorAxis));
        Feed(monitor, Enumerable.Repeat<long?>(0, 15));
        Assert.Equal(0UL, monitor.Version); AssertOpen(monitor.ReadProgress());
        Assert.Equal(512, Value(monitor.ReadRecentError(), monitor.MeanErrorAxis));
        Feed(monitor, [0]);
        Assert.Equal(1UL, monitor.Version); Assert.Equal(512, Value(monitor.ReadProgress(), monitor.ProgressAxis));
        Assert.Equal(0, Value(monitor.ReadRecentError(), monitor.MeanErrorAxis));
        var completed = monitor.ReadProgress();
        Feed(monitor, Enumerable.Repeat<long?>(2, 15));
        Assert.Equal(1UL, monitor.Version); Assert.Equal(512, Value(monitor.ReadProgress(), monitor.ProgressAxis));
        Assert.Equal(0, Value(monitor.ReadRecentError(), monitor.MeanErrorAxis));
        Assert.Equal(512, Value(completed, monitor.ProgressAxis));
        Feed(monitor, [2]);
        Assert.Equal(2UL, monitor.Version); Assert.Equal(-512, Value(monitor.ReadProgress(), monitor.ProgressAxis));
        Assert.Equal(512, Value(monitor.ReadRecentError(), monitor.MeanErrorAxis));
        Assert.Equal(48UL, monitor.ObservationCount); Assert.Equal(monitor.Version, monitor.VersionSite.Value);
    }

    [Fact]
    public void QuarterPointBinaryMismatchImprovementPublishesSigned128()
    {
        var monitor = Create();
        Feed(monitor, Enumerable.Repeat<long?>(2, 8).Concat(Enumerable.Repeat<long?>(0, 8)));
        Feed(monitor, Enumerable.Repeat<long?>(2, 4).Concat(Enumerable.Repeat<long?>(0, 12)));
        Assert.Equal(128, Value(monitor.ReadProgress(), monitor.ProgressAxis));
        Assert.Equal(128, Value(monitor.ReadRecentError(), monitor.MeanErrorAxis));
        Assert.Equal(0, Value(monitor.ReadCoverageChange(), monitor.CoverageAxis));
    }

    [Fact]
    public void DroppingCoverageCannotMasqueradeAsSuccessfulUnknownPredictions()
    {
        var monitor = Create(); Feed(monitor, Enumerable.Repeat<long?>(2, 16));
        Feed(monitor, Enumerable.Repeat<long?>(0, 8).Concat(Enumerable.Repeat<long?>(null, 8)));
        Assert.Equal(512, Value(monitor.ReadProgress(), monitor.ProgressAxis));
        Assert.Equal(-128, Value(monitor.ReadCoverageChange(), monitor.CoverageAxis));
        Assert.Equal(0, Value(monitor.ReadRecentError(), monitor.MeanErrorAxis));
        Feed(monitor, Enumerable.Repeat<long?>(0, 7).Concat(Enumerable.Repeat<long?>(null, 9)));
        AssertOpen(monitor.ReadProgress()); AssertOpen(monitor.ReadRecentError());
        Assert.Equal(-16, Value(monitor.ReadCoverageChange(), monitor.CoverageAxis));
    }

    [Fact]
    public void CoverageGainCanBeKnownBeforeAnOldNumericErrorExists()
    {
        var monitor = Create(); Feed(monitor, Enumerable.Repeat<long?>(null, 16));
        Feed(monitor, Enumerable.Repeat<long?>(0, 8).Concat(Enumerable.Repeat<long?>(null, 8)));
        AssertOpen(monitor.ReadProgress()); Assert.Equal(128, Value(monitor.ReadCoverageChange(), monitor.CoverageAxis));
        Assert.Equal(0, Value(monitor.ReadRecentError(), monitor.MeanErrorAxis));
    }

    [Fact]
    public void CoarseNominalZeroRemainsMissingErrorCoverage()
    {
        var monitor = Create();
        var capacity = FiniteReadSurface.Capacity(new Carrier(), monitor.ErrorAxis, 0, 2, 2);
        for (var index = 0; index < 16; index++)
        {
            var coarse = FiniteReadSurface.Publish(new Carrier(), capacity, new CarrierLocation(monitor.ErrorAxis, 1), new Carrier());
            Assert.True(FiniteReadSurface.TryReadValue(coarse, out var nominal)); Assert.Equal(0, nominal.Ticks);
            monitor.Observe(coarse, P(monitor.ResourceAxis, 1));
        }
        Feed(monitor, Enumerable.Repeat<long?>(0, 16));
        Assert.Equal(32UL, monitor.ObservationCount); AssertOpen(monitor.ReadProgress());
        Assert.Equal(256, Value(monitor.ReadCoverageChange(), monitor.CoverageAxis));
        Assert.Equal(0, Value(monitor.ReadRecentError(), monitor.MeanErrorAxis));
    }

    [Fact]
    public void ForeignMisframedAmbiguousAndUnsupportedPhysicalFeedbackDoesNotEnterAWindow()
    {
        var monitor = Create(); var error = P(monitor.ErrorAxis, 0); var foreign = new Carrier();
        var invalid = new[]
        {
            P(foreign, 1), P(monitor.ResourceAxis, 0), P(monitor.ResourceAxis, 2),
            NativeGraph.Open(new Carrier(), Relation.Segment(monitor.ResourceAxis, -1, 1)),
            Misframed(monitor.ResourceAxis, 1, foreign),
            FiniteReadSurface.Publish(new Carrier(), FiniteReadSurface.Capacity(new Carrier(), monitor.ResourceAxis, -2, 2, 2),
                new CarrierLocation(monitor.ResourceAxis, 1), new Carrier())
        };
        foreach (var actual in invalid) AssertOpen(monitor.Observe(error, actual));
        Assert.Equal(0UL, monitor.ObservationCount); Assert.Equal(0UL, monitor.Version);
        monitor.Observe(error, P(monitor.ResourceAxis, -1)); Assert.Equal(1UL, monitor.ObservationCount);
    }

    [Fact]
    public void ForeignMisframedAndOutOfDomainErrorsDoNotBecomeUnknownCoverage()
    {
        var monitor = Create(); var foreign = new Carrier();
        var qualifiedWrongBasis = QualifiedReadSurface.Create(new Carrier(), new Carrier(), Relation.Segment(foreign, 0, 1),
            new CarrierLocation(monitor.ErrorAxis, 0), new Placeholder(), new Placeholder(), new Placeholder(), new Placeholder(), new Placeholder());
        var invalid = new[]
        {
            P(foreign, 0), P(monitor.ErrorAxis, -1), P(monitor.ErrorAxis, 3), Misframed(monitor.ErrorAxis, 0, foreign),
            NativeGraph.Open(new Carrier(), Relation.Segment(foreign, 0, 2)), qualifiedWrongBasis,
            ReadWitness.Create(new Carrier(), new Carrier(), Relation.Segment(monitor.ErrorAxis, 0, 2), NativeGraph.G(), new Placeholder(), new Placeholder())
        };
        foreach (var error in invalid) AssertOpen(monitor.Observe(error, P(monitor.ResourceAxis, 1)));
        Assert.Equal(0UL, monitor.ObservationCount);
        monitor.Observe(OpenError(monitor), P(monitor.ResourceAxis, 1)); Assert.Equal(1UL, monitor.ObservationCount);
    }

    [Fact]
    public void DuplicatePhysicalIdentityIsExcludedWithinTheFiniteReceiptContract()
    {
        var monitor = Create(); var identity = new Carrier(); var actual = P(monitor.ResourceAxis, 1, identity);
        monitor.Observe(P(monitor.ErrorAxis, 2), actual);
        AssertOpen(monitor.Observe(P(monitor.ErrorAxis, 0), actual));
        AssertOpen(monitor.Observe(P(monitor.ErrorAxis, 0), P(monitor.ResourceAxis, 1, identity)));
        Assert.Equal(1UL, monitor.ObservationCount);
        Feed(monitor, Enumerable.Repeat<long?>(0, LearningProgress.ReceiptCapacity));
        monitor.Observe(P(monitor.ErrorAxis, 0), actual);
        Assert.Equal(66UL, monitor.ObservationCount);
        var fields = NativeGraph.Parts(monitor.Root, 14);
        Assert.Equal(LearningProgress.ReceiptCapacity, NativeGraph.Parts(Assert.IsType<Relation>(fields[8])).Length);
        Assert.Equal(2UL, Assert.IsType<Carrier>(fields[7]).Value);
    }

    [Fact]
    public void NativeTransportPreservesAliasesPartialBlocksReceiptsAndContinuation()
    {
        var monitor = Create(); Feed(monitor, Enumerable.Repeat<long?>(2, 16)); Feed(monitor, Enumerable.Repeat<long?>(0, 5));
        var restoredRoot = Assert.IsType<Relation>(Assert.Single(GojoNumericFormat.Decode(GojoNumericFormat.Encode(monitor.Root))));
        var restored = LearningProgress.FromRoot(restoredRoot);
        Assert.Equal(NativeInspection.Signature(monitor.Root), NativeInspection.Signature(restored.Root));
        Assert.False(restored.ResourceAxis.SameIdentityAs(monitor.ResourceAxis));
        var fields = NativeGraph.Parts(restored.Root, 14); var law = NativeGraph.Parts(Assert.IsType<Relation>(fields[1]), 8);
        Assert.True(NativeGraph.TryPoint(Assert.IsType<Relation>(law[2]).Left, out var errorBound));
        Assert.True(errorBound.Carrier.SameIdentityAs(restored.ErrorAxis));
        Assert.Same(restored.VersionSite, fields[5]);
        var receipt = Assert.IsType<Relation>(NativeGraph.Parts(Assert.IsType<Relation>(fields[8]))[0]);
        Assert.True(ReadWitness.TryRead(receipt, out var identity, out _, out _, out _, out _));
        AssertOpen(restored.Observe(P(restored.ErrorAxis, 0), P(restored.ResourceAxis, 1, identity)));
        Assert.Equal(21UL, restored.ObservationCount);
        Feed(monitor, Enumerable.Repeat<long?>(0, 11)); Feed(restored, Enumerable.Repeat<long?>(0, 11));
        Assert.Equal(monitor.ObservationCount, restored.ObservationCount); Assert.Equal(monitor.Version, restored.Version);
        Assert.Equal(512, Value(restored.ReadProgress(), restored.ProgressAxis));
        Assert.Equal(Value(monitor.ReadRecentError(), monitor.MeanErrorAxis), Value(restored.ReadRecentError(), restored.MeanErrorAxis));
        Feed(monitor, Enumerable.Repeat<long?>(2, 16)); Feed(restored, Enumerable.Repeat<long?>(2, 16));
        Assert.Equal(-512, Value(restored.ReadProgress(), restored.ProgressAxis)); Assert.Equal(2UL, restored.Version);
    }

    [Fact]
    public void FiniteMeanRetainsItsDiscardedProjectionParticipation()
    {
        var monitor = Create(3, 1); Feed(monitor, [2, 0, 0]);
        var mean = monitor.ReadRecentError(); Assert.Equal(170, Value(mean, monitor.MeanErrorAxis));
        Assert.True(ReadWitness.TryRead(mean, out _, out _, out _, out var remainder, out _));
        Assert.True(ProjectionResidue.TryRead(remainder, out var source, out var numerator, out var denominator));
        Assert.Equal(1, numerator); Assert.Equal(128, denominator);
        var sum = Assert.IsType<Carrier>(NativeGraph.Parts(Assert.IsType<Relation>(NativeGraph.Parts(monitor.Root, 14)[3]), 6)[4]);
        Assert.True(source.SameIdentityAs(sum));
        var restored = LearningProgress.FromRoot(Assert.IsType<Relation>(Assert.Single(GojoNumericFormat.Decode(GojoNumericFormat.Encode(monitor.Root)))));
        var restoredMean = restored.ReadRecentError(); Assert.Equal(170, Value(restoredMean, restored.MeanErrorAxis));
        Assert.True(ReadWitness.TryRead(restoredMean, out _, out _, out _, out var restoredRemainder, out _));
        Assert.True(ProjectionResidue.TryRead(restoredRemainder, out _, out var restoredNumerator, out var restoredDenominator));
        Assert.Equal(numerator, restoredNumerator); Assert.Equal(denominator, restoredDenominator);
    }

    [Fact]
    public void NativeRetentionIsBoundedRatherThanKeepingAllCompletedHistory()
    {
        var monitor = Create(); Feed(monitor, Enumerable.Repeat<long?>(2, 128));
        monitor.ReadProgress(); monitor.ReadCoverageChange(); monitor.ReadRecentError();
        var retained = NativeInspection.Count(monitor.Root);
        Feed(monitor, Enumerable.Repeat<long?>(2, 128));
        monitor.ReadProgress(); monitor.ReadCoverageChange(); monitor.ReadRecentError();
        Assert.Equal(retained, NativeInspection.Count(monitor.Root));
        Assert.Equal(256UL, monitor.ObservationCount); Assert.Equal(15UL, monitor.Version);
    }

    private static LearningProgress Create(int blockSize = 16, int minimumKnown = 8) => new(new Carrier(), new Carrier(), new Carrier(), blockSize, minimumKnown);
    private static void Feed(LearningProgress monitor, IEnumerable<long?> errors)
    {
        foreach (var error in errors)
            monitor.Observe(error.HasValue ? P(monitor.ErrorAxis, error.Value) : OpenError(monitor), P(monitor.ResourceAxis, 1));
    }
    private static Relation OpenError(LearningProgress monitor) => NativeGraph.Open(new Carrier(), Relation.Segment(monitor.ErrorAxis, 0, 2));
    private static Relation P(Carrier axis, long ticks, IElement? source = null) => NativeGraph.Publication(axis, ticks, source ?? new Carrier());
    private static Relation Misframed(Carrier resultAxis, long ticks, Carrier frameAxis) => ReadWitness.Create(new Carrier(), new Carrier(),
        Relation.Segment(frameAxis, -16, 16), new CarrierLocation(resultAxis, ticks), NativeGraph.G(), NativeGraph.G());
    private static long Value(Relation publication, Carrier axis)
    { Assert.True(NativeGraph.TryValue(publication, axis, out var value)); return value.Ticks; }
    private static void AssertOpen(Relation publication)
    { Assert.True(ReadWitness.TryRead(publication, out _, out _, out var result, out _, out _)); Assert.IsType<Placeholder>(result); }
}
