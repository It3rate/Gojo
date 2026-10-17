using Gojo.Core;
using Gojo.Structures;

namespace Gojo.Tests.Core;

public sealed class SamplingSurfaceTests
{
    [Fact]
    public void PresenceDoesNotSupplyMagnitudeUnitOrMetric()
    {
        var f = new Samples();
        var sample = f.Sample(100_000);
        Assert.True(SamplingSurface.TryReadSample(sample, out var presence, out var magnitude));
        Assert.Same(f.Presence, presence.Carrier);
        Assert.Equal(1, presence.Ticks);
        Assert.IsType<Placeholder>(magnitude);
        Assert.True(ReadWitness.TryRead(sample, out var selected, out _, out var result, out _, out _));
        Assert.Same(f.Source, selected);
        Assert.True(Assert.IsType<Relation>(result).TryReadOrderedGroup(out var parts));
        Assert.Same(magnitude, parts[0]);
        Assert.Equal(presence, Assert.IsType<CarrierLocation>(parts[1]));

        var state = f.Observe(sample);
        AssertState(state, f.Source, 1, 1);
        Assert.True(SamplingSurface.TryReadState(state, out _, out _, out _, out _, out var metric, out _));
        Assert.IsType<Placeholder>(metric);
        Assert.False(QualifiedReadSurface.TryReadPublishedValue(sample, out _));
    }

    [Fact]
    public void ComparisonsBuildSortedDistinctAnchorsWithoutInventingDistance()
    {
        var f = new Samples(4);
        var middle = f.Sample(20);
        var low = f.Sample(5);
        var high = f.Sample(80);
        var equalLow = f.Sample(5);
        f.Observe(middle);
        f.Observe(low);
        f.Observe(high);
        var state = f.Observe(equalLow);

        Assert.Equal(new[] { low, middle, high }, Landmarks(state));
        AssertState(state, f.Source, 3, 4);
        var last = LastVisit(state);
        Assert.True(SamplingSurface.TryReadVisit(last, out var sample, out var region, out var direction, out _));
        Assert.Same(equalLow, sample);
        AssertRegion(region, low, low);
        AssertDirection(direction, equalLow, high, f.Order, -1);
        Assert.False(QualifiedReadSurface.TryReadPublishedValue(last, out _));
        Assert.True(SamplingSurface.TryReadState(state, out _, out _, out _, out _, out var metric, out _));
        Assert.IsType<Placeholder>(metric);
    }

    [Fact]
    public void FullLandmarkBudgetLeavesOuterPossibilitiesOpen()
    {
        var f = new Samples(2);
        var low = f.Sample(10);
        var high = f.Sample(20);
        f.Observe(low);
        var oldState = f.Observe(high);
        var beyond = f.Sample(1_000_000);
        var current = f.Observe(beyond);

        Assert.Equal(new[] { low, high }, Landmarks(current));
        AssertState(current, f.Source, 2, 3);
        Assert.True(SamplingSurface.TryReadVisit(LastVisit(current), out var sample, out var region, out _, out _));
        Assert.Same(beyond, sample);
        AssertRegion(region, high, null);
        AssertState(oldState, f.Source, 2, 2);

        var below = f.Sample(-1_000_000);
        current = f.Observe(below);
        Assert.True(SamplingSurface.TryReadVisit(LastVisit(current), out _, out region, out _, out _));
        AssertRegion(region, null, low);
        Assert.Equal(new[] { low, high }, Landmarks(current));
    }

    [Fact]
    public void TinyMovementWithinOneIrregularGapHasItsOwnDirectionEvidence()
    {
        var f = new Samples(2);
        var low = f.Sample(0);
        var high = f.Sample(1_000_000_000);
        f.Observe(low);
        f.Observe(high);
        var first = f.Sample(10);
        var a = f.Observe(first);
        var second = f.Sample(11);
        var b = f.Observe(second);

        Assert.True(SamplingSurface.TryReadVisit(LastVisit(a), out _, out var firstRegion, out _, out _));
        AssertRegion(firstRegion, low, high);
        Assert.True(SamplingSurface.TryReadVisit(LastVisit(b), out _, out var nextRegion, out var direction, out _));
        AssertRegion(nextRegion, low, high);
        AssertDirection(direction, second, first, f.Order, 1);
        Assert.Contains(f.Calls, pair => ReferenceEquals(pair.Candidate, second) && ReferenceEquals(pair.Anchor, first));
        Assert.True(SamplingSurface.TryReadState(b, out _, out _, out _, out _, out var metric, out _));
        Assert.IsType<Placeholder>(metric);
    }

    [Fact]
    public void AbsenceIsRetainedButNeverComparedAsZeroMagnitude()
    {
        var f = new Samples(2);
        var first = f.Sample(10);
        f.Observe(first);
        var absent = f.Absent();
        Assert.True(SamplingSurface.TryReadSample(absent, out var presence, out var magnitude));
        Assert.Equal(0, presence.Ticks);
        Assert.IsType<Placeholder>(magnitude);
        var absentState = f.Observe(absent);
        AssertState(absentState, f.Source, 1, 2);
        Assert.True(ReadWitness.TryRead(LastVisit(absentState), out var absentSelection, out _, out _, out _, out _));
        Assert.Same(absent, absentSelection);

        var next = f.Sample(11);
        var state = f.Observe(next);
        Assert.True(SamplingSurface.TryReadVisit(LastVisit(state), out _, out _, out var direction, out _));
        AssertDirection(direction, next, first, f.Order, 1);
        Assert.DoesNotContain(f.Calls, pair => ReferenceEquals(pair.Candidate, absent) || ReferenceEquals(pair.Anchor, absent));
        AssertState(state, f.Source, 2, 3);
    }

    [Fact]
    public void UnknownComparisonsRetainAnUnresolvedVisitAndEstablishedAnchors()
    {
        var f = new Samples(3);
        var anchor = f.Sample(10);
        var prior = f.Observe(anchor);
        var candidate = f.Sample(20);
        var state = SamplingSurface.Observe(new Carrier(), prior, candidate,
            (a, b) => ReadWitness.Create(new Carrier(), a, b, new Placeholder(), new Relation(a, b), new Placeholder()));

        Assert.Equal(new[] { anchor }, Landmarks(state));
        AssertState(state, f.Source, 1, 2);
        Assert.False(SamplingSurface.TryReadVisit(LastVisit(state), out _, out _, out _, out _));
        Assert.True(ReadWitness.TryRead(LastVisit(state), out var selected, out _, out var result, out _, out _));
        Assert.Same(candidate, selected);
        Assert.IsType<Placeholder>(result);
        AssertState(prior, f.Source, 1, 1);
    }

    [Fact]
    public void FailedMembershipDoesNotEraseThePreviousPresentObservation()
    {
        var f = new Samples(2);
        var anchor = f.Sample(10);
        var prior = f.Observe(anchor);
        var uncertain = f.Sample(20);
        var failed = SamplingSurface.Observe(new Carrier(), prior, uncertain,
            (a, b) => ReadWitness.Create(new Carrier(), a, b, new Placeholder(), new Placeholder(), new Placeholder()));
        var next = f.Sample(21);
        var current = SamplingSurface.Observe(new Carrier(), failed, next, f.Compare);

        Assert.True(SamplingSurface.TryReadVisit(LastVisit(current), out _, out _, out var direction, out _));
        AssertDirection(direction, next, uncertain, f.Order, 1);
        AssertState(current, f.Source, 2, 3);
        Assert.False(SamplingSurface.TryReadVisit(Visits(current)[1], out _, out _, out _, out _));
    }

    [Fact]
    public void ContradictoryNeighborEvidenceLeavesTheLandmarksUnchanged()
    {
        var f = new Samples(8);
        foreach (var raw in Enumerable.Range(0, 8)) f.Observe(f.Sample(raw * 10));
        var prior = f.State;
        var anchors = Landmarks(prior);
        var candidate = f.Sample(40);
        var current = SamplingSurface.Observe(new Carrier(), prior, candidate, (a, b) =>
        {
            // The service first identifies anchor 4 as equal, then contradicts
            // that observation by placing the candidate above the next anchor.
            var index = Array.IndexOf(anchors, b);
            var order = index == 5 ? 1 : Math.Sign(4 - index);
            return SamplingSurface.Comparison(new Carrier(), a, b, f.Order, order);
        });

        Assert.Equal(anchors, Landmarks(current));
        Assert.False(SamplingSurface.TryReadVisit(LastVisit(current), out _, out _, out _, out _));
        Assert.True(ReadWitness.TryRead(LastVisit(current), out var selected, out _, out var result, out _, out _));
        Assert.Same(candidate, selected);
        Assert.IsType<Placeholder>(result);
        AssertState(current, f.Source, 8, 9);
        AssertState(prior, f.Source, 8, 8);
    }

    [Fact]
    public void ComparisonWitnessMustNameBothSamplesAndTheActualOrderBasis()
    {
        var f = new Samples();
        var candidate = f.Sample(20);
        var anchor = f.Sample(10);
        var comparison = SamplingSurface.Comparison(new Carrier(), candidate, anchor, f.Order, 1);
        Assert.True(SamplingSurface.TryReadComparison(comparison, candidate, anchor, f.Order, out var order));
        Assert.Equal(1, order);
        Assert.False(SamplingSurface.TryReadComparison(comparison, anchor, candidate, f.Order, out _));
        Assert.False(SamplingSurface.TryReadComparison(comparison, candidate, f.Sample(10), f.Order, out _));
        Assert.False(SamplingSurface.TryReadComparison(comparison, candidate, anchor, new Carrier(f.Order.UnitShape), out _));
        Assert.False(SamplingSurface.TryReadComparison(
            SamplingSurface.Comparison(new Carrier(), candidate, anchor, f.Order, 2), candidate, anchor, f.Order, out _));
        Assert.False(SamplingSurface.TryReadComparison(
            ReadWitness.Create(new Carrier(), candidate, anchor, new CarrierLocation(f.Order, 1), comparison, comparison),
            candidate, anchor, f.Order, out _));
    }

    [Fact]
    public void OrderingCannotCompareAbsenceScalarsOrAcquisitionsFromAnotherContext()
    {
        var f = new Samples();
        var present = f.Sample(10);
        var scalar = Relation.Point(new Carrier(), 10);
        var absent = f.Absent();
        var otherPresence = SamplingSurface.Sample(new Carrier(), f.Source, new Carrier(f.Presence.UnitShape), true);
        var otherSource = SamplingSurface.Sample(new Carrier(), new Carrier(f.Source.UnitShape), f.Presence, true);

        foreach (var invalid in new[] { scalar, absent, otherPresence, otherSource })
        {
            var asCandidate = SamplingSurface.Comparison(new Carrier(), invalid, present, f.Order, 1);
            Assert.False(SamplingSurface.TryReadComparison(asCandidate, invalid, present, f.Order, out _));
            var asAnchor = SamplingSurface.Comparison(new Carrier(), present, invalid, f.Order, 1);
            Assert.False(SamplingSurface.TryReadComparison(asAnchor, present, invalid, f.Order, out _));
        }
    }

    [Fact]
    public void CrossAxisOrReversedEvidenceCannotChangeThePartition()
    {
        foreach (var reversed in new[] { false, true })
        {
            var f = new Samples(3);
            var anchor = f.Sample(10);
            var prior = f.Observe(anchor);
            var candidate = f.Sample(20);
            var foreign = new Carrier(f.Order.UnitShape);
            var state = SamplingSurface.Observe(new Carrier(), prior, candidate,
                (a, b) => reversed
                    ? SamplingSurface.Comparison(new Carrier(), b, a, f.Order, -1)
                    : SamplingSurface.Comparison(new Carrier(), a, b, foreign, 1));

            Assert.Equal(new[] { anchor }, Landmarks(state));
            Assert.False(SamplingSurface.TryReadVisit(LastVisit(state), out _, out _, out _, out _));
            AssertState(state, f.Source, 1, 2);
        }
    }

    [Fact]
    public void AForeignSourceOrPresenceBasisIsRejectedBeforeAnyOrderingQuery()
    {
        var f = new Samples(2);
        var anchor = f.Sample(10);
        var prior = f.Observe(anchor);
        var foreignSource = SamplingSurface.Sample(new Carrier(), new Carrier(f.Source.UnitShape), f.Presence, true);
        var foreignPresence = SamplingSurface.Sample(new Carrier(), f.Source, new Carrier(f.Presence.UnitShape), true);
        var malformed = ReadWitness.Create(new Carrier(), f.Source, Relation.Segment(f.Presence, 0, 1),
            G(new CarrierLocation(f.Order, 50), new CarrierLocation(f.Presence, 1)), new Placeholder(), f.Source);

        foreach (var rejected in new[] { foreignSource, foreignPresence, malformed })
        {
            var calls = 0;
            var state = SamplingSurface.Observe(new Carrier(), prior, rejected, (a, b) =>
            {
                calls++;
                return SamplingSurface.Comparison(new Carrier(), a, b, f.Order, 1);
            });
            Assert.Equal(0, calls);
            Assert.Equal(new[] { anchor }, Landmarks(state));
            Assert.False(SamplingSurface.TryReadVisit(LastVisit(state), out _, out _, out _, out _));
            AssertState(state, f.Source, 1, 2);
        }
    }

    [Fact]
    public void ChangingPhysicalScalePreservesOnlyTheOrdinalResults()
    {
        var original = Trace(1);
        var rescaled = Trace(100_000);
        Assert.Equal(original, rescaled);

        static (bool LowerOpen, bool UpperOpen, bool Equality, long? Direction)[] Trace(int scale)
        {
            var f = new Samples(2);
            var trace = new List<(bool, bool, bool, long?)>();
            Relation? previous = null;
            foreach (var raw in new[] { 5, 10, 7, 7, 0, 9, 20 })
            {
                var sample = f.Sample(raw * scale);
                var state = f.Observe(sample);
                Assert.True(SamplingSurface.TryReadVisit(LastVisit(state), out _, out var region, out var direction, out _));
                var interval = Assert.IsType<Relation>(region);
                long? sign = null;
                if (previous is not null)
                {
                    Assert.True(SamplingSurface.TryReadComparison(Assert.IsType<Relation>(direction), sample, previous, f.Order, out var value));
                    sign = value;
                }
                trace.Add((interval.Left is Placeholder, interval.Right is Placeholder,
                    ReferenceEquals(interval.Left, interval.Right), sign));
                previous = sample;
                Assert.True(SamplingSurface.TryReadState(state, out _, out _, out _, out _, out var metric, out _));
                Assert.IsType<Placeholder>(metric);
            }
            return trace.ToArray();
        }
    }

    [Fact]
    public void ComparisonQueriesDependOnLandmarksRatherThanTheGrowingVisitHistory()
    {
        var f = new Samples(8);
        foreach (var raw in Enumerable.Range(0, 8)) f.Observe(f.Sample(raw * 10));
        for (var i = 0; i < 40; i++) f.Observe(f.Sample(i % 2 == 0 ? 35 : 36));
        f.Calls.Clear();
        var current = f.Observe(f.Sample(37));

        Assert.InRange(f.Calls.Count, 1, 7);
        AssertState(current, f.Source, 8, 49);
        Assert.True(SamplingSurface.TryReadVisit(LastVisit(current), out _, out var region, out _, out var probes));
        AssertRegion(region, Landmarks(current)[3], Landmarks(current)[4]);
        Assert.True(probes.TryReadOrderedGroup(out var retained));
        Assert.NotEmpty(retained);
    }

    [Fact]
    public void ExplicitMetricAttachesWithoutReinterpretingEarlierOrdinalReads()
    {
        var f = new Samples(2);
        var a = f.Sample(5);
        var b = f.Sample(10);
        f.Observe(a);
        var ordinal = f.Observe(b);
        var axis = new Carrier();
        var basis = Relation.Segment(axis, 0, 1);
        var firstMetric = MetricRead(a, axis, basis, 5);
        var observations = G(new Relation(a, firstMetric), new Relation(b, MetricRead(b, axis, basis, 10)));
        var measured = SamplingSurface.WithMetric(new Carrier(), ordinal, basis, observations);

        Assert.True(SamplingSurface.TryReadState(ordinal, out _, out _, out _, out _, out var before, out _));
        Assert.IsType<Placeholder>(before);
        Assert.True(SamplingSurface.TryReadState(measured, out var source, out _, out _, out _, out var after, out _));
        Assert.Same(f.Source, source);
        Assert.True(Assert.IsType<Relation>(after).TryReadOrderedGroup(out var parts));
        Assert.Equal(2, parts.Length);
        Assert.Equal(basis, Assert.IsType<Relation>(parts[0]));
        Assert.True(Assert.IsType<Relation>(parts[1]).TryReadOrderedGroup(out var bindings));
        Assert.Equal(2, bindings.Length);
        var firstBinding = Assert.IsType<Relation>(bindings[0]);
        Assert.Same(a, firstBinding.Left);
        Assert.Same(b, Assert.IsType<Relation>(bindings[1]).Left);
        Assert.True(QualifiedReadSurface.TryReadPublishedValue(Assert.IsType<Relation>(firstBinding.Right), out var captured));
        Assert.Equal(5, captured.Ticks);
        Assert.True(QualifiedReadSurface.TryReadPublishedValue(firstMetric, out var exposed));
        exposed.Tick.Set(99);
        Assert.Equal(5, captured.Ticks);
        Assert.Equal(Landmarks(ordinal), Landmarks(measured));
        AssertState(ordinal, f.Source, 2, 2);
        AssertState(measured, f.Source, 2, 2);
    }

    [Fact]
    public void UnvisitedMetricBindingsAndPendingSuppliersDoNotCreateCalibration()
    {
        var f = new Samples(2);
        var known = f.Sample(5);
        var ordinal = f.Observe(known);
        var unvisited = f.Sample(7);
        var axis = new Carrier();
        var basis = Relation.Segment(axis, 0, 1);
        var a = MetricRead(known, axis, basis, 5);
        var b = MetricRead(known, axis, basis, 2);
        var pending = QualifiedReadSurface.Add(new Carrier(), a, b);
        Assert.True(QualifiedReadSurface.TryReadValue(pending, out var possible));
        Assert.Equal(7, possible.Ticks);

        var invalidBindings = new[]
        {
            G(new Relation(unvisited, MetricRead(unvisited, axis, basis, 7))),
            G(new Relation(known, pending))
        };
        foreach (var bindings in invalidBindings)
        {
            var attempted = SamplingSurface.WithMetric(new Carrier(), ordinal, basis, bindings);
            Assert.True(SamplingSurface.TryReadState(attempted, out _, out _, out _, out _, out var metric, out var last));
            Assert.IsType<Placeholder>(metric);
            Assert.True(ReadWitness.TryRead(last, out _, out _, out var result, out _, out _));
            Assert.IsType<Placeholder>(result);
            Assert.Equal(Landmarks(ordinal), Landmarks(attempted));
            Assert.Equal(Visits(ordinal), Visits(attempted));
        }
        Assert.True(ReadWitness.TryRead(pending, out _, out _, out var stillOpen, out _, out _));
        Assert.IsType<Placeholder>(stillOpen);
    }

    [Fact]
    public void ArchivePreservesSampleIdentityDirectionAndNativeSource()
    {
        var f = new Samples(2);
        f.Observe(f.Sample(5));
        var second = f.Sample(10);
        var prior = f.Observe(second);
        var last = f.Sample(7);
        var current = f.Observe(last);
        var restored = Assert.IsType<Relation>(GojoEncodedSnapshot.CaptureArchive(current).RestoreRoots()[0]);

        Assert.True(SamplingSurface.TryReadState(restored, out var source, out var policy, out _, out _, out var metric, out _));
        Assert.NotSame(f.Source, Assert.IsType<Carrier>(source));
        Assert.True(policy.TryReadOrderedGroup(out var policyParts));
        var orderFrame = Assert.IsType<Relation>(policyParts[1]);
        var restoredOrder = Assert.IsType<CarrierLocation>(Assert.IsType<Relation>(orderFrame.Left).Left).Carrier;
        Assert.IsType<Placeholder>(metric);
        var visits = Visits(restored);
        Assert.Equal(3, visits.Length);
        Assert.True(SamplingSurface.TryReadVisit(visits[1], out var previousSample, out _, out _, out _));
        Assert.True(SamplingSurface.TryReadVisit(visits[2], out var currentSample, out var region, out var direction, out _));
        AssertRegion(region, Landmarks(restored)[0], Landmarks(restored)[1]);
        Assert.Same(previousSample, Landmarks(restored)[1]);
        AssertDirection(direction, currentSample, previousSample, restoredOrder, -1);
        Assert.True(ReadWitness.TryRead(currentSample, out var sampleSource, out _, out _, out _, out _));
        Assert.Same(source, sampleSource);
        AssertState(prior, f.Source, 2, 2);
    }

    private static Relation MetricRead(Relation sample, Carrier axis, Relation basis, long value) =>
        QualifiedReadSurface.Create(new Carrier(), sample, basis, new CarrierLocation(axis, value),
            Relation.Segment(axis, value, value), new Placeholder(), new Placeholder(), new Placeholder(), sample);

    private static Relation G(params IElement[] elements) => Relation.OrderedGroup(new Carrier(), elements);

    private static Relation[] Landmarks(Relation state)
    {
        Assert.True(SamplingSurface.TryReadState(state, out _, out _, out var landmarks, out _, out _, out _));
        Assert.True(landmarks.TryReadOrderedGroup(out var parts));
        return parts.Select(Assert.IsType<Relation>).ToArray();
    }

    private static Relation[] Visits(Relation state)
    {
        Assert.True(SamplingSurface.TryReadState(state, out _, out _, out _, out var visits, out _, out _));
        Assert.True(visits.TryReadOrderedGroup(out var parts));
        return parts.Select(Assert.IsType<Relation>).ToArray();
    }

    private static Relation LastVisit(Relation state)
    {
        Assert.True(SamplingSurface.TryReadState(state, out _, out _, out _, out _, out _, out var last));
        Assert.Same(last, Visits(state)[^1]);
        return last;
    }

    private static void AssertState(Relation state, IElement source, int landmarks, int visits)
    {
        Assert.True(SamplingSurface.TryReadState(state, out var selected, out _, out _, out _, out _, out _));
        Assert.Same(source, selected);
        Assert.Equal(landmarks, Landmarks(state).Length);
        Assert.Equal(visits, Visits(state).Length);
    }

    private static void AssertRegion(IElement region, Relation? lower, Relation? upper)
    {
        var interval = Assert.IsType<Relation>(region);
        if (lower is null) Assert.IsType<Placeholder>(interval.Left);
        else Assert.Same(lower, interval.Left);
        if (upper is null) Assert.IsType<Placeholder>(interval.Right);
        else Assert.Same(upper, interval.Right);
    }

    private static void AssertDirection(IElement direction, Relation candidate, Relation anchor, Carrier axis, long expected)
    {
        Assert.True(SamplingSurface.TryReadComparison(Assert.IsType<Relation>(direction), candidate, anchor, axis, out var order));
        Assert.Equal(expected, order);
    }

    // Private fixture data models an external comparison service. Only its
    // signed relation-shaped reply crosses into the sampling structure.
    private sealed class Samples
    {
        private readonly Dictionary<Relation, int> values = new(ReferenceEqualityComparer.Instance);
        public Carrier Source { get; } = new();
        public Carrier Presence { get; } = new();
        public Carrier Order { get; } = new();
        public Carrier Count { get; } = new();
        public Relation State { get; private set; }
        public List<(Relation Candidate, Relation Anchor)> Calls { get; } = [];

        public Samples(int budget = 8) =>
            State = SamplingSurface.Create(new Carrier(), Source, Presence, Order, Count, budget);

        public Relation Sample(int value)
        {
            var sample = SamplingSurface.Sample(new Carrier(), Source, Presence, true);
            values.Add(sample, value);
            return sample;
        }

        public Relation Absent() => SamplingSurface.Sample(new Carrier(), Source, Presence, false);

        public Relation Observe(Relation sample)
        {
            State = SamplingSurface.Observe(new Carrier(), State, sample, Compare);
            return State;
        }

        public Relation Compare(Relation candidate, Relation anchor)
        {
            Calls.Add((candidate, anchor));
            return SamplingSurface.Comparison(new Carrier(), candidate, anchor, Order,
                Math.Sign(values[candidate].CompareTo(values[anchor])));
        }
    }
}
