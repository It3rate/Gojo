using Gojo.Core;
using Gojo.Structures;

namespace PrimitiveLearning.Tests;

public sealed class AdaptiveOrdinalTests
{
    [Fact]
    public void ContrastsLearnTwoUsefulEdgesWithoutGivingSamplesMetricCoordinates()
    {
        var f = new Fixture(); var learner = f.Create(); var low = f.Sample(10);
        Assert.False(AdaptiveOrdinal.TryReadPrediction(learner.Predict(low, f.Compare), out _));
        learner.Observe(low, f.Outcome(0), f.Compare);
        learner.Observe(f.Sample(90), f.Outcome(1), f.Compare);
        Assert.Equal(new long[] { 0, 1 }, Alternatives(learner.Predict(f.Sample(50), f.Compare)));
        learner.Observe(f.Sample(49), f.Outcome(0), f.Compare);
        learner.Observe(f.Sample(50), f.Outcome(1), f.Compare);
        Assert.Equal(2, learner.LandmarkCount);
        Assert.Equal(0, Value(learner.Predict(f.Sample(40), f.Compare)));
        Assert.Equal(1, Value(learner.Predict(f.Sample(60), f.Compare)));
        Assert.True(AdaptiveOrdinal.TryReadModel(learner.Root, out _, out var landmarks, out var rules));
        Assert.True(rules.TryReadOrderedGroup(out var nativeRules)); Assert.NotEmpty(nativeRules);
        Assert.True(landmarks.TryReadOrderedGroup(out var rows));
        foreach (var row in rows)
        {
            Assert.True(AdaptiveOrdinal.TryReadLandmark((Relation)row, out var sample, out _, out _));
            Assert.True(SamplingSurface.TryReadSample(sample, out var presence, out var magnitude));
            Assert.Equal(1, presence.Ticks); Assert.IsType<Placeholder>(magnitude);
        }
        Assert.Equal(f.Requests, learner.Comparisons);
    }

    [Fact]
    public void MonotonePrivateTransformsLeaveLearnedOrdinalResponsesUnchanged()
    {
        long[][] signatures = new Func<int, int>[] { x => x, x => 7 * x + 1000, x => x * x + 17 }.Select(transform =>
        {
            var f = new Fixture(); var learner = f.Create();
            foreach (var x in new[] { 10, 90, 49, 50, 20, 80 }) learner.Observe(f.Sample(transform(x)), f.Outcome(x < 50 ? 0 : 1), f.Compare);
            return new[] { 0, 25, 49, 50, 75, 100 }.Select(x => Value(learner.Predict(f.Sample(transform(x)), f.Compare))).ToArray();
        }).ToArray();
        Assert.Equal(signatures[0], signatures[1]); Assert.Equal(signatures[0], signatures[2]);
    }

    [Fact]
    public void FrozenFeedbackExposesMismatchButDoesNotChangeLearnedCounts()
    {
        var f = new Fixture(); var learner = f.Create();
        learner.Observe(f.Sample(10), f.Outcome(0), f.Compare); learner.Observe(f.Sample(90), f.Outcome(1), f.Compare);
        var before = Counts(learner); var old = learner.Predict(f.Sample(10), f.Compare);
        var feedback = learner.Observe(f.Sample(10), f.Outcome(1), f.Compare, false);
        Assert.Equal(before, Counts(learner)); Assert.Equal(2, learner.Updates); Assert.Equal(0, Value(old));
        Assert.True(ReadWitness.TryRead(feedback, out _, out _, out _, out var residue, out _));
        Assert.True(((Relation)residue).TryReadOrderedGroup(out var evidence));
        Assert.True(OperationClaim.TryReadResolvedViolation((Relation)evidence[0], out var violation)); Assert.Equal(1, violation.Ticks);
    }

    [Fact]
    public void ContradictoryConsequenceAtSameOrderClassRetainsBothAlternatives()
    {
        var f = new Fixture(); var learner = f.Create();
        learner.Observe(f.Sample(7), f.Outcome(0), f.Compare); learner.Observe(f.Sample(7), f.Outcome(1), f.Compare);
        var forecast = learner.Predict(f.Sample(7), f.Compare);
        Assert.False(AdaptiveOrdinal.TryReadPrediction(forecast, out _));
        Assert.Equal(new long[] { 0, 1 }, Alternatives(forecast)); Assert.Equal(1, learner.LandmarkCount);
    }

    [Fact]
    public void KnownAbsenceAndUnavailableAcquisitionRemainDistinctAndDoNotCompare()
    {
        var f = new Fixture(); var learner = f.Create();
        var absence = SamplingSurface.Sample(new Carrier(), f.Source, f.Presence, false);
        var unavailable = ReadWitness.Create(new Carrier(), f.Source, Relation.Segment(f.Presence, 0, 1), new Placeholder(), new Placeholder(), f.Source);
        Assert.True(SamplingSurface.TryReadSample(absence, out var p, out _)); Assert.Equal(0, p.Ticks);
        Assert.False(SamplingSurface.TryReadSample(unavailable, out _, out _));
        foreach (var sample in new[] { absence, unavailable })
        {
            var prediction = learner.Predict(sample, f.Compare);
            Assert.False(AdaptiveOrdinal.TryReadPrediction(prediction, out _));
            Assert.True(ReadWitness.TryRead(prediction, out var selected, out _, out _, out _, out _)); Assert.Same(sample, selected);
            learner.Observe(sample, f.Outcome(0), f.Compare);
        }
        Assert.Equal(0, learner.Comparisons); Assert.Equal(0, learner.Updates); Assert.Equal(0, learner.LandmarkCount);
    }

    [Fact]
    public void UnavailableOrWrongAxisComparisonCannotUpdateTheModel()
    {
        var f = new Fixture(); var learner = f.Create(); learner.Observe(f.Sample(10), f.Outcome(0), f.Compare);
        var before = Counts(learner); var candidate = f.Sample(90);
        Relation Failed(Relation a, Relation b) => ReadWitness.Create(new Carrier(), new Relation(a, b), f.Order, new Placeholder(), new Placeholder(), a);
        Assert.False(AdaptiveOrdinal.TryReadPrediction(learner.Predict(candidate, Failed), out _));
        learner.Observe(candidate, f.Outcome(1), Failed);
        learner.Observe(candidate, f.Outcome(1), (a, b) => SamplingSurface.Comparison(new Carrier(), a, b, new Carrier(), 1));
        Assert.Equal(before, Counts(learner)); Assert.Equal(1, learner.Updates);
    }

    [Fact]
    public void BudgetRejectsNewContrastWithoutInventingAConsequence()
    {
        var f = new Fixture(); var learner = f.Create(1);
        learner.Observe(f.Sample(10), f.Outcome(0), f.Compare);
        var blocked = learner.Observe(f.Sample(90), f.Outcome(1), f.Compare);
        Assert.Equal(1, learner.LandmarkCount); Assert.Equal(1, learner.Updates);
        Assert.False(AdaptiveOrdinal.TryReadPrediction(learner.Predict(f.Sample(90), f.Compare), out _));
        Assert.True(ReadWitness.TryRead(blocked, out _, out _, out _, out var residue, out _));
        Assert.True(((Relation)residue).TryReadOrderedGroup(out var parts));
        Assert.True(((Relation)parts[1]).TryReadOrderedGroup(out var losses)); Assert.NotEmpty(losses);
    }

    [Fact]
    public void NativeArchiveCanRediscoverAxesLandmarksAndPredictions()
    {
        var f = new Fixture(); var learner = f.Create();
        learner.Observe(f.Sample(10), f.Outcome(0), f.Compare); learner.Observe(f.Sample(90), f.Outcome(1), f.Compare);
        var restoredRoot = Assert.IsType<Relation>(Assert.Single(GojoNumericFormat.Decode(GojoNumericFormat.Encode(learner.Root))));
        var restored = AdaptiveOrdinal.FromRoot(restoredRoot);
        Assert.True(AdaptiveOrdinal.TryReadModel(restored.Root, out var policy, out var group, out _)); policy.TryReadOrderedGroup(out var contract);
        var source = Assert.IsType<Carrier>(contract[0]); var presence = Axis((Relation)contract[1]);
        var order = Axis((Relation)contract[2]); group.TryReadOrderedGroup(out var rows);
        var privateValues = new Dictionary<Relation, int>(ReferenceEqualityComparer.Instance);
        for (var i = 0; i < rows.Length; i++) { AdaptiveOrdinal.TryReadLandmark((Relation)rows[i], out var anchor, out _, out _); privateValues[anchor] = i == 0 ? 10 : 90; }
        var candidate = SamplingSurface.Sample(new Carrier(), source, presence, true); privateValues[candidate] = 50;
        Relation Compare(Relation a, Relation b) => SamplingSurface.Comparison(new Carrier(), a, b, order, privateValues[a].CompareTo(privateValues[b]));
        Assert.Equal(new long[] { 0, 1 }, Alternatives(restored.Predict(candidate, Compare))); Assert.Equal(Counts(learner), Counts(restored));
    }

    [Fact]
    public void ExactRepeatedFeedbackIsRefusedWithinTheNativeReceiptWindow()
    {
        var f = new Fixture(); var learner = f.Create(); var sample = f.Sample(7); var outcome = f.Outcome(0);
        learner.Observe(sample, outcome, f.Compare); var counts = Counts(learner); var queries = learner.Comparisons;
        learner.Predict(sample, f.Compare); queries = learner.Comparisons;
        var duplicate = learner.Observe(sample, outcome, f.Compare);
        Assert.False(AdaptiveOrdinal.TryReadPrediction(duplicate, out _)); Assert.Equal(counts, Counts(learner));
        Assert.Equal(1, learner.Updates); Assert.Equal(queries, learner.Comparisons);
        for (var i = 0; i < 70; i++) learner.Observe(f.Sample(7), f.Outcome(0), f.Compare);
        learner.Root.TryReadOrderedGroup(out var root); ((Relation)root[5]).TryReadOrderedGroup(out var receipts);
        Assert.Equal(64, receipts.Length);
    }

    [Fact]
    public void RepeatedTrainingAgesFiniteCountsAndKeepsRetainedGraphBounded()
    {
        var f = new Fixture(); var learner = f.Create(); learner.Observe(f.Sample(7), f.Outcome(0), f.Compare);
        for (var i = 0; i < 400; i++) learner.Observe(f.Sample(7), f.Outcome(0), f.Compare);
        var early = NativeInspection.Count(learner.Root);
        for (var i = 0; i < 400; i++) learner.Observe(f.Sample(7), f.Outcome(0), f.Compare);
        var late = NativeInspection.Count(learner.Root);
        Assert.Equal(1, learner.LandmarkCount); Assert.All(Counts(learner), n => Assert.InRange(n, 0, 255));
        Assert.InRange(late.Relations, 1, early.Relations + 20); Assert.InRange(late.Carriers, 1, early.Carriers + 20);
    }

    private static long Value(Relation prediction) { Assert.True(AdaptiveOrdinal.TryReadPrediction(prediction, out var point)); return point.Ticks; }
    private static Carrier Axis(Relation frame) => ((CarrierLocation)((Relation)frame.Left).Left).Carrier;
    private static long[] Alternatives(Relation prediction) { Assert.True(AdaptiveOrdinal.TryReadAlternatives(prediction, out var points)); return points.Select(p => p.Ticks).ToArray(); }
    private static long[] Counts(AdaptiveOrdinal learner)
    {
        AdaptiveOrdinal.TryReadModel(learner.Root, out _, out var group, out _); group.TryReadOrderedGroup(out var rows);
        return rows.SelectMany(row => { AdaptiveOrdinal.TryReadLandmark((Relation)row, out _, out var zero, out var one); return new[] { (long)zero.Value, (long)one.Value }; }).ToArray();
    }
    private sealed class Fixture
    {
        public Carrier Source { get; } = new(); public Carrier Presence { get; } = new(); public Carrier Order { get; } = new(); public Carrier Consequence { get; } = new();
        private readonly Dictionary<Relation, int> privateValues = new(ReferenceEqualityComparer.Instance);
        public int Requests { get; private set; }
        public AdaptiveOrdinal Create(int capacity = 16) => new(Source, Presence, Order, Consequence, capacity);
        public Relation Sample(int value) { var sample = SamplingSurface.Sample(new Carrier(), Source, Presence, true); privateValues[sample] = value; return sample; }
        public Relation Compare(Relation a, Relation b) { Requests++; return SamplingSurface.Comparison(new Carrier(), a, b, Order, privateValues[a].CompareTo(privateValues[b])); }
        public Relation Outcome(int value) => ReadWitness.Create(new Carrier(), new Placeholder(), Relation.Segment(Consequence, 0, 1), new CarrierLocation(Consequence, value), new Placeholder(), new Placeholder());
    }
}
