using Gojo.Core;
using Gojo.Structures;

namespace Gojo.Tests.Core;

public sealed class RelativeSignalSurfaceTests
{
    [Fact]
    public void FirstPresenceHasNoBrightnessMagnitudeOrCalibratedZero()
    {
        var f = new Fixture(); var sample = f.Acquire(123456);
        f.Observe(sample);
        Assert.True(SamplingSurface.TryReadSample(sample, out var presence, out var magnitude));
        Assert.Equal(1, presence.Ticks); Assert.IsType<Placeholder>(magnitude);
        var reads = Latest(f.State);
        Assert.False(RelativeSignalSurface.TryReadBankRead(reads.Fast, out _, out _, out _, out _));
        Assert.False(RelativeSignalSurface.TryReadBankRead(reads.Slow, out _, out _, out _, out _));
        Assert.Equal(0, f.Requests);
        Assert.All(References(f.State), reference => Assert.Equal(64UL, reference.Weight.Value));
        Assert.DoesNotContain(Reachable(f.State), element => element is CarrierLocation location && location.Ticks == 123456);
    }

    [Fact]
    public void NewObservationIsComparedBeforeItCanDiluteItsOwnScore()
    {
        var f = new Fixture(); f.Observe(f.Acquire(1000)); f.Observe(f.Acquire(1001));
        var latest = Latest(f.State);
        foreach (var read in new[] { latest.Fast, latest.Slow })
        {
            Assert.True(RelativeSignalSurface.TryReadBankRead(read, out var score, out var evidence, out var total, out var count));
            Assert.Equal(16, score.Ticks); Assert.Equal(64, evidence); Assert.Equal(64, total); Assert.Equal(1, count);
            Assert.True(RelativeSignalSurface.TryReadBankComparisons(read, out var comparisons)); Assert.Single(comparisons);
        }
        Assert.Equal(1, f.Requests); // One acquisition pair, reused across banks.
        Assert.Equal(new ulong[] { 48, 64, 62, 64 }, References(f.State).Select(reference => reference.Weight.Value));
    }

    [Fact]
    public void ZeroFromOpposedVotesIsDifferentFromEveryComparisonBeingSame()
    {
        var balanced = new Fixture(fastN: 1, fastD: 1, slowN: 1, slowD: 1);
        balanced.Observe(balanced.Acquire(0)); balanced.Observe(balanced.Acquire(10)); balanced.Observe(balanced.Acquire(5));
        var mixed = Latest(balanced.State).Fast;
        Assert.True(RelativeSignalSurface.TryReadBankRead(mixed, out var zero, out var evidence, out var total, out var count));
        Assert.Equal(0, zero.Ticks); Assert.Equal(0, evidence); Assert.Equal(128, total); Assert.Equal(2, count);
        Assert.Equal(new long[] { 1, -1 }, ComparisonOrders(mixed));

        var same = new Fixture(); same.Observe(same.Acquire(5)); same.Observe(same.Acquire(5));
        var equal = Latest(same.State).Fast;
        Assert.True(RelativeSignalSurface.TryReadBankRead(equal, out zero, out evidence, out _, out _));
        Assert.Equal(0, zero.Ticks); Assert.Equal(0, evidence);
        Assert.Equal(new long[] { 0 }, ComparisonOrders(equal));
    }

    [Fact]
    public void ScoresAreInvariantUnderAnOrderPreservingChangeOfPrivateInstrumentScale()
    {
        var original = new Fixture(); var translated = new Fixture();
        foreach (var value in new[] { -20, -18, -18, 5, 4, 8, -2, 8 })
        {
            original.Observe(original.Acquire(value)); translated.Observe(translated.Acquire(value * 7 + 250000));
            Assert.Equal(ScorePair(original.State), ScorePair(translated.State));
        }
        Assert.Equal(original.Requests, translated.Requests);
    }

    [Fact]
    public void LastAttentionQuantumExpiresInsteadOfBecomingSticky()
    {
        var f = new Fixture(initial: 1); f.Observe(f.Acquire(5));
        var absent = f.Acquire(0, exists: false); f.Observe(absent);
        Assert.Empty(References(f.State)); Assert.Equal(0, f.Requests);
        var latest = Latest(f.State);
        Assert.False(RelativeSignalSurface.TryReadBankRead(latest.Fast, out _, out _, out _, out _));
        Assert.False(RelativeSignalSurface.TryReadBankRead(latest.Slow, out _, out _, out _, out _));
        Assert.True(latest.Losses.TryReadOrderedGroup(out var losses)); Assert.Equal(2, losses.Length);
    }

    [Fact]
    public void LeakAndNormalizationRetainTheirOwnFractionalSourceResidues()
    {
        var f = new Fixture(initial: 3); f.Observe(f.Acquire(0)); f.Observe(f.Acquire(10));
        var second = Latest(f.State);
        Assert.True(second.Updates.TryReadOrderedGroup(out var updates));
        Assert.True(ReadWitness.TryRead(Assert.IsType<Relation>(updates[0]), out _, out _, out var after, out var leakResidue, out _));
        Assert.Equal(2, Assert.IsType<CarrierLocation>(after).Ticks);
        Assert.True(ProjectionResidue.TryRead(leakResidue, out _, out var numerator, out var denominator));
        Assert.Equal(1, numerator); Assert.Equal(3, denominator);
        f.Observe(f.Acquire(5)); var read = Latest(f.State).Fast;
        Assert.True(RelativeSignalSurface.TryReadBankRead(read, out var score, out var evidence, out var total, out _));
        Assert.Equal(-3, score.Ticks); Assert.Equal(-1, evidence); Assert.Equal(5, total);
        Assert.True(ReadWitness.TryRead(read, out _, out _, out _, out var residue, out _));
        Assert.True(ProjectionResidue.TryRead(residue, out _, out numerator, out denominator));
        Assert.Equal(-1, numerator); Assert.Equal(16, denominator);
    }

    [Fact]
    public void RepeatedLatestEventDoesNotQueryLeakOrInjectAttentionAgain()
    {
        var f = new Fixture(); f.Observe(f.Acquire(0)); var latest = f.Acquire(1); f.Observe(latest);
        var before = References(f.State).Select(reference => reference.Weight.Value).ToArray();
        var reads = Latest(f.State); var requests = f.Requests;
        f.Observe(latest);
        Assert.Equal(requests, f.Requests); Assert.Equal(before, References(f.State).Select(reference => reference.Weight.Value));
        Assert.Same(reads.Fast, Latest(f.State).Fast);
    }

    [Fact]
    public void RepeatedOlderRetainedEventIsAnOpenRefusalWithoutReplenishingAttention()
    {
        var f = new Fixture(); var first = f.Acquire(0); f.Observe(first); f.Observe(f.Acquire(1));
        var before = References(f.State).Select(reference => reference.Weight.Value).ToArray(); var requests = f.Requests;
        f.Observe(first);
        Assert.Equal(requests, f.Requests); Assert.Equal(before, References(f.State).Select(reference => reference.Weight.Value));
        Assert.False(RelativeSignalSurface.TryReadBankRead(Latest(f.State).Fast, out _, out _, out _, out _));
        Assert.True(Latest(f.State).Losses.TryReadOrderedGroup(out var losses)); Assert.NotEmpty(losses);
    }

    [Fact]
    public void ForeignOrMalformedAcquisitionsDoNotLeakOrEnterMemory()
    {
        var f = new Fixture(); f.Observe(f.Acquire(1));
        foreach (var sample in new[]
        {
            SamplingSurface.Sample(new Carrier(), new Placeholder(), f.Presence, true),
            SamplingSurface.Sample(new Carrier(), f.Source, new Carrier(f.Presence.UnitShape), true),
            Relation.Point(f.Order, 7)
        })
        {
            var before = References(f.State).Select(reference => reference.Weight.Value).ToArray();
            f.Observe(sample);
            Assert.Equal(before, References(f.State).Select(reference => reference.Weight.Value)); Assert.Equal(0, f.Requests);
            Assert.False(RelativeSignalSurface.TryReadBankRead(Latest(f.State).Fast, out _, out _, out _, out _));
        }
    }

    [Fact]
    public void FailedComparisonStaysVisibleAndDoesNotPreventRememberingAValidAcquisition()
    {
        var f = new Fixture(); f.Observe(f.Acquire(0)); var next = f.Acquire(1);
        var failed = SamplingSurface.Comparison(new Carrier(), next, References(f.State)[0].Sample, new Carrier(), 1);
        f.State = RelativeSignalSurface.Observe(new Carrier(), f.State, next, (_, _) => failed);
        var read = Latest(f.State).Fast;
        Assert.False(RelativeSignalSurface.TryReadBankRead(read, out _, out _, out _, out _));
        Assert.True(RelativeSignalSurface.TryReadBankComparisons(read, out var comparisons)); Assert.Same(failed, Assert.Single(comparisons));
        Assert.Equal(2, References(f.State).Count(reference => ReferenceEquals(reference.Sample, next)));
        f.Observe(f.Acquire(2)); Assert.Equal((16L, 16L), ScorePair(f.State));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void InvalidComparisonRoleRangeOrGrammarCannotManufactureANeutralScore(int failure)
    {
        var f = new Fixture(); var anchor = f.Acquire(0); f.Observe(anchor); var current = f.Acquire(1);
        var attempted = failure switch
        {
            0 => SamplingSurface.Comparison(new Carrier(), anchor, current, f.Order, -1),
            1 => SamplingSurface.Comparison(new Carrier(), current, anchor, f.Order, 2),
            _ => ReadWitness.Create(new Carrier(), G(current, anchor), Relation.Segment(f.Order, -1, 1),
                new CarrierLocation(f.Order, 1), new Placeholder(), new Placeholder())
        };
        f.State = RelativeSignalSurface.Observe(new Carrier(), f.State, current, (_, _) => attempted);
        foreach (var read in new[] { Latest(f.State).Fast, Latest(f.State).Slow })
        {
            Assert.False(RelativeSignalSurface.TryReadBankRead(read, out _, out _, out _, out _));
            Assert.True(RelativeSignalSurface.TryReadBankComparisons(read, out var queries));
            Assert.Same(attempted, Assert.Single(queries));
        }
    }

    [Fact]
    public void CapturedReadWeightsAndScoresDoNotFollowLaterCarrierSettlement()
    {
        var f = new Fixture(); f.Observe(f.Acquire(0)); f.Observe(f.Acquire(1)); var early = Latest(f.State).Fast;
        Assert.True(ReadWitness.TryRead(early, out _, out _, out _, out _, out var provenance));
        Assert.True(Assert.IsType<Relation>(provenance).TryReadOrderedGroup(out var fields));
        Assert.True(Assert.IsType<Relation>(fields[1]).TryReadOrderedGroup(out var weights));
        var coordinate = Assert.IsType<CarrierLocation>(Assert.IsType<Relation>(weights[0]).Right);
        Assert.False(coordinate.SameTickRefAs(coordinate.Carrier.ValueSlot)); Assert.Equal(64, coordinate.Ticks);
        f.Observe(f.Acquire(-1));
        Assert.Equal(64, coordinate.Ticks); Assert.Equal(36UL, coordinate.Carrier.Value);
        Assert.True(RelativeSignalSurface.TryReadBankRead(early, out var score, out var votes, out var total, out _));
        Assert.Equal(16, score.Ticks); Assert.Equal(64, votes); Assert.Equal(64, total);
    }

    [Fact]
    public void FiniteCapacityForgettingHasAnExplicitLossAndNoPreviousStateChain()
    {
        var f = new Fixture(capacity: 1); f.Observe(f.Acquire(0)); var previousState = f.State;
        var current = f.Acquire(1); f.Observe(current);
        Assert.All(References(f.State), reference => Assert.Same(current, reference.Sample));
        var latest = Latest(f.State); Assert.True(latest.Losses.TryReadOrderedGroup(out var losses)); Assert.Equal(2, losses.Length);
        foreach (var item in losses)
        {
            Assert.True(ReadWitness.TryRead(Assert.IsType<Relation>(item), out _, out _, out var result, out var forgotten, out _));
            Assert.IsType<Placeholder>(result); Assert.True(Assert.IsType<CarrierLocation>(forgotten).Ticks > 0);
        }
        Assert.DoesNotContain(Reachable(f.State), element => ReferenceEquals(element, previousState));
    }

    [Fact]
    public void UnrelatedWeightUnitsCannotBeReadAsAttentionQuanta()
    {
        var f = new Fixture(); var sample = f.Acquire(1); f.Observe(sample);
        Assert.True(RelativeSignalSurface.TryReadState(f.State, out var source, out var policy, out var fast, out var slow, out var latest));
        Assert.True(RelativeSignalSurface.TryReadPolicy(policy, out _, out _, out var attention, out _, out _, out _));
        var foreign = new Carrier(UnitShape.Product(attention.UnitShape, attention.UnitShape)); foreign.Set(64);
        Assert.False(foreign.UnitShape.CompatibleWith(attention.UnitShape));
        Assert.True(RelativeSignalSurface.TryReadBank(fast, out var law, out _));
        var forged = G(source, policy, G(law, G(new Relation(sample, foreign))), slow, latest);
        Assert.False(RelativeSignalSurface.TryReadState(forged, out _, out _, out _, out _, out _));
    }

    [Theory]
    [InlineData(0, 64, 1, 4)]
    [InlineData(129, 64, 1, 4)]
    [InlineData(2, 0, 1, 4)]
    [InlineData(2, 257, 1, 4)]
    [InlineData(2, 64, 1, 0)]
    [InlineData(2, 64, 5, 4)]
    [InlineData(2, 64, 1, 257)]
    public void InvalidFinitePolicyRemainsUnreadable(int capacity, int initial, int n, int d)
    {
        var state = RelativeSignalSurface.Create(new Carrier(), new Placeholder(), new Carrier(), new Carrier(),
            fastRetentionNumerator: n, fastRetentionDenominator: d, initialWeight: initial, maxReferences: capacity);
        Assert.False(RelativeSignalSurface.TryReadState(state, out _, out _, out _, out _, out _));
    }

    [Fact]
    public void MaximumDeclaredWeightsAndReferenceCountStayWithinTheFiniteScoreLane()
    {
        var f = new Fixture(initial: 256, capacity: 128, fastN: 1, fastD: 1, slowN: 1, slowD: 1);
        for (var value = 0; value <= 128; value++) f.Observe(f.Acquire(value));
        foreach (var read in new[] { Latest(f.State).Fast, Latest(f.State).Slow })
        {
            Assert.True(RelativeSignalSurface.TryReadBankRead(read, out var score, out var evidence, out var total, out var count));
            Assert.Equal(16, score.Ticks); Assert.Equal(32768, evidence); Assert.Equal(32768, total); Assert.Equal(128, count);
        }
        Assert.Equal(256, References(f.State).Length);
        Assert.All(References(f.State), reference => Assert.Equal(256UL, reference.Weight.Value));
    }

    [Fact]
    public void ArchivePreservesLocalAxesEventAliasesAndTheLastRead()
    {
        var f = new Fixture(); f.Observe(f.Acquire(0)); f.Observe(f.Acquire(1)); f.Observe(f.Acquire(-1));
        var restored = Assert.IsType<Relation>(GojoNumericFormat.Decode(GojoNumericFormat.Encode(f.State))[0]);
        Assert.Equal(ScorePair(f.State), ScorePair(restored));
        Assert.Equal(References(f.State).Select(reference => reference.Weight.Value), References(restored).Select(reference => reference.Weight.Value));
        Assert.True(RelativeSignalSurface.TryReadState(restored, out var source, out var policy, out _, out _, out _));
        Assert.True(RelativeSignalSurface.TryReadPolicy(policy, out _, out _, out var attention, out _, out _, out _));
        foreach (var reference in References(restored))
        {
            Assert.True(ReadWitness.TryRead(reference.Sample, out var acquisitionSource, out _, out _, out _, out _));
            Assert.Same(source, acquisitionSource); Assert.True(reference.Weight.UnitShape.CompatibleWith(attention.UnitShape));
        }
    }

    private static (Relation Fast, Relation Slow, Relation Updates, Relation Losses) Latest(Relation state)
    {
        Assert.True(RelativeSignalSurface.TryReadState(state, out _, out _, out _, out _, out var latest));
        Assert.True(RelativeSignalSurface.TryReadLatest(latest, out _, out var fast, out var slow, out var updates, out var losses));
        return (fast, slow, updates, losses);
    }
    private static (long?, long?) ScorePair(Relation state)
    {
        var reads = Latest(state);
        long? Score(Relation read) => RelativeSignalSurface.TryReadBankRead(read, out var value, out _, out _, out _) ? value.Ticks : null;
        return (Score(reads.Fast), Score(reads.Slow));
    }
    private static long[] ComparisonOrders(Relation read)
    {
        Assert.True(RelativeSignalSurface.TryReadBankComparisons(read, out var comparisons));
        return comparisons.Select(comparison =>
        {
            Assert.True(ReadWitness.TryRead(comparison, out _, out _, out var result, out _, out _));
            return Assert.IsType<CarrierLocation>(result).Ticks;
        }).ToArray();
    }
    private static (Relation Sample, Carrier Weight)[] References(Relation state)
    {
        Assert.True(RelativeSignalSurface.TryReadState(state, out _, out _, out var fast, out var slow, out _));
        return new[] { fast, slow }.SelectMany(bank =>
        {
            Assert.True(RelativeSignalSurface.TryReadBank(bank, out _, out var refs));
            return refs.Select(reference =>
            {
                Assert.True(RelativeSignalSurface.TryReadReference(reference, out var sample, out var weight));
                return (sample, weight);
            });
        }).ToArray();
    }
    private static IEnumerable<IElement> Reachable(IElement root)
    {
        var seen = new HashSet<IElement>(ReferenceEqualityComparer.Instance); var stack = new Stack<IElement>(); stack.Push(root);
        while (stack.TryPop(out var element))
        {
            if (!seen.Add(element)) continue; yield return element;
            if (element is Relation relation) { stack.Push(relation.Left); stack.Push(relation.Right); }
        }
    }
    private static Relation G(params IElement[] elements) => Relation.OrderedGroup(new Carrier(), elements);
    private sealed class Fixture
    {
        public IElement Source { get; } = new Placeholder();
        public Carrier Presence { get; } = new();
        public Carrier Order { get; } = new();
        public Relation State { get; set; }
        public int Requests { get; private set; }
        private readonly Dictionary<Relation, int> values = new(ReferenceEqualityComparer.Instance);
        public Fixture(int initial = 64, int capacity = 64, int fastN = 3, int fastD = 4, int slowN = 31, int slowD = 32) =>
            State = RelativeSignalSurface.Create(new Carrier(), Source, Presence, Order, fastN, fastD, slowN, slowD, initial, capacity);
        public Relation Acquire(int value, bool exists = true)
        {
            var sample = SamplingSurface.Sample(new Carrier(), Source, Presence, exists); values.Add(sample, value); return sample;
        }
        public void Observe(Relation sample) => State = RelativeSignalSurface.Observe(new Carrier(), State, sample, (candidate, anchor) =>
        { Requests++; return SamplingSurface.Comparison(new Carrier(), candidate, anchor, Order, values[candidate].CompareTo(values[anchor])); });
    }
}
