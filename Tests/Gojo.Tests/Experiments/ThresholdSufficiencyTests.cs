using Gojo.Core;
using Gojo.Equations;
using Gojo.Structures;

namespace Gojo.Tests.Experiments;

// Exploratory sufficiency probes, not new predicate machinery. See
// Gojo/docs/GojoPrimitiveSufficiencyExperiments.md for the interpretation limits.
public sealed class ThresholdSufficiencyTests
{
    [Fact]
    public void ExistingLiveOutputSettlesAnExactSignedGapWhenTheThresholdMoves()
    {
        var temperature = new Carrier();
        var threshold = new Carrier(temperature.UnitShape);
        var gap = new Carrier(temperature.UnitShape);
        temperature.Add(24);
        threshold.Add(20);

        // (T.ValueSlot -> A.ValueSlot) -> Sum(-T port, +A port).
        // This is an explicit non-draining output, not a transfer of temperature.
        var operands = new Relation(threshold.ValueSlot, temperature.ValueSlot);
        var difference = Equation.Difference(threshold, temperature, gap);
        var output = temperature.AttachLiveOutput(operands, difference);
        Assert.Same(operands, output.Left);
        Assert.Same(difference, output.Right);
        Assert.True(threshold.IsAttached(output));
        Assert.Equal(4UL, gap.Value);
        Assert.Equal(0, gap.Tension);

        threshold.Add(6);
        Assert.Equal(0UL, gap.Value);
        Assert.Equal(-2, gap.Tension); // The explicit output requests -2; draw is unpaid.

        threshold.Remove(4);
        Assert.Equal(2UL, gap.Value);
        Assert.Equal(0, gap.Tension);

        threshold.Add(2);
        Assert.Equal(0UL, gap.Value);
        Assert.Equal(0, gap.Tension); // Equality differs from the negative gap above.
        Assert.Equal(24UL, temperature.Value);
        Assert.Equal(0, temperature.Tension);
        Assert.Same(output, Assert.Single(gap.AttachedRelations));
    }

    [Theory]
    [InlineData(24, 24, 24, 20, 4, 4)]
    [InlineData(20, 20, 20, 20, 0, 0)]
    [InlineData(16, 16, 16, 20, -4, -4)]
    [InlineData(24, 22, 26, 20, 2, 6)]
    [InlineData(20, 18, 22, 20, -2, 2)]
    [InlineData(18, 16, 20, 20, -4, 0)]
    public void ExactAndIntervalComparisonsRetainTheirEntireSignedSupport(
        long nominal, long low, long high, long limit, long minimumGap, long maximumGap)
    {
        var f = new NumericWorld(nominal, limit, low, high);
        AssertBounds(f.Difference, minimumGap, maximumGap);
        Assert.True(QualifiedReadSurface.TryReadSeparation(f.Separation, out var minimum, out var witness));
        Assert.Equal(minimumGap, minimum.Ticks);
        Assert.True(ReadWitness.TryRead(witness, out var selected, out _, out _, out var support, out _));
        Assert.Equal(new Relation(new CarrierLocation(f.Basis, minimumGap),
            new CarrierLocation(f.Basis, maximumGap)), support);
        Assert.Equal(new IElement[] { f.TemperatureRead, f.ThresholdRead }, Fields(Assert.IsType<Relation>(selected)));
        Assert.Equal((ulong)nominal, f.Temperature.Value);
        Assert.Equal((ulong)limit, f.Threshold.Value);
        Assert.Equal(0, f.Temperature.Tension);
        Assert.Equal(0, f.Threshold.Tension);
        Assert.Equal(0UL, f.Basis.Value); // Observation has installed no answer.
    }

    [Fact]
    public void OneRetainedRelationRereadsAMovingThresholdWithoutErasingEarlierEvidence()
    {
        var f = new NumericWorld(20, 20, 18, 22);
        Assert.True(QualifiedReadSurface.TryReadSeparation(f.Separation, out _, out var first));
        var before = Publish(f.Difference, f.DecisionCapacity);
        AssertBounds(before, -2, 2);

        f.Threshold.Add(3);
        AssertBounds(f.Difference, -5, -1);
        Assert.True(QualifiedReadSurface.TryReadSeparation(f.Separation, out var below, out _));
        Assert.Equal(-5, below.Ticks);

        f.Threshold.Remove(6);
        AssertBounds(f.Difference, 1, 5);
        Assert.True(QualifiedReadSurface.TryReadSeparation(f.Separation, out var above, out _));
        Assert.Equal(1, above.Ticks);
        AssertBounds(before, -2, 2);
        Assert.True(ReadWitness.TryRead(first, out _, out _, out var oldResult, out _, out _));
        Assert.Equal(-2, Assert.IsType<CarrierLocation>(oldResult).Ticks);
        Assert.True(ReadWitness.TryRead(f.Difference, out _, out _, out var pending, out _, out _));
        Assert.IsType<Placeholder>(pending); // Rereading did not cache a new answer here.
    }

    [Theory]
    [InlineData(18, 1, 0, 0)] // [2,6]: the entire gap is strictly positive.
    [InlineData(20, 0, 0, 1)] // [0,4]: equality is possible, so A > T is unsettled.
    [InlineData(24, 0, 1, 0)] // [-4,0]: no alternative is strictly positive.
    public void ARelationCanParticipateDirectlyInAConditionalWithoutBecomingABooleanCarrier(
        long limit, int admittedCount, int rejectedCount, int unresolvedCount)
    {
        var f = new NumericWorld(22, limit, 20, 24);
        var response = new Relation(f.ThresholdRead, f.TemperatureRead);
        // G(G(G(difference relation, S(basis,1,MaxTick))), response).
        // Strict positivity is declared at whole-tick resolution, with origin 0.
        var condition = SupportDecisionSurface.Condition(new Carrier(), f.Difference,
            Relation.Segment(f.Basis, 1, long.MaxValue));
        var rule = SupportDecisionSurface.Rule(new Carrier(), response, condition);
        var policy = SupportDecisionSurface.Create(new Carrier(), rule);
        var publication = Publish(f.Difference, f.DecisionCapacity);
        var binding = SupportDecisionSurface.Binding(new Carrier(), f.Difference, publication);
        var selection = SupportDecisionSurface.Select(new Carrier(), policy, binding);

        Assert.Same(f.Difference, Fields(condition)[0]);
        Assert.Same(f.Difference, Fields(binding)[0]);
        Assert.True(SupportDecisionSurface.TryReadSelection(selection, out var selectedRule, out var result,
            out var admitted, out var earlier));
        Assert.Equal(admittedCount, Fields(admitted).Length);
        Relation ruleRead;
        if (admittedCount == 1)
        {
            Assert.Same(rule, selectedRule);
            Assert.Same(response, result);
            Assert.Empty(Fields(earlier));
            Assert.True(ReadWitness.TryRead(selection, out _, out _, out _, out _, out var provenance));
            ruleRead = Assert.IsType<Relation>(Fields(Assert.IsType<Relation>(provenance))[3]);
        }
        else
        {
            Assert.IsType<Placeholder>(selectedRule);
            Assert.IsType<Placeholder>(result);
            ruleRead = Assert.IsType<Relation>(Assert.Single(Fields(earlier)));
        }
        Assert.True(SupportDecisionSurface.TryReadRuleRead(ruleRead, out var retainedRule, out _,
            out var rejected, out var unresolved));
        Assert.Same(rule, retainedRule);
        Assert.Equal(rejectedCount, Fields(rejected).Length);
        Assert.Equal(unresolvedCount, Fields(unresolved).Length);
        Assert.Equal((ulong)limit, f.Threshold.Value);
        Assert.Equal(0, f.Threshold.Tension);
    }

    [Fact]
    public void AConditionalDoesNotExecuteAPendingRelationOrAutomaticallyRefreshAPublication()
    {
        var f = new NumericWorld(24, 20, 24, 24);
        var response = new Relation(f.TemperatureRead, f.ThresholdRead);
        var rule = SupportDecisionSurface.Rule(new Carrier(), response,
            SupportDecisionSurface.Condition(new Carrier(), f.Difference, Relation.Segment(f.Basis, 1, 100)));
        var policy = SupportDecisionSurface.Create(new Carrier(), rule);
        Relation Select(Relation publication) => SupportDecisionSurface.Select(new Carrier(), policy,
            SupportDecisionSurface.Binding(new Carrier(), f.Difference, publication));

        var pendingSelection = Select(f.Difference);
        Assert.True(SupportDecisionSurface.TryReadSelection(pendingSelection, out _, out var pending, out _, out var earlier));
        Assert.IsType<Placeholder>(pending);
        Assert.True(SupportDecisionSurface.TryReadRuleRead(Assert.IsType<Relation>(Assert.Single(Fields(earlier))),
            out _, out _, out var rejected, out var unresolved));
        Assert.Empty(Fields(rejected));
        Assert.Single(Fields(unresolved));

        var publication = Publish(f.Difference, f.DecisionCapacity);
        Assert.True(SupportDecisionSurface.TryReadSelection(Select(publication), out _, out var accepted, out _, out _));
        Assert.Same(response, accepted);
        f.Threshold.Add(8);
        AssertBounds(f.Difference, -4, -4);
        // A retained publication remains evidence for its earlier observation.
        Assert.True(SupportDecisionSurface.TryReadSelection(Select(publication), out _, out var retained, out _, out _));
        Assert.Same(response, retained);
        Assert.True(SupportDecisionSurface.TryReadSelection(Select(Publish(f.Difference, f.DecisionCapacity)),
            out _, out var current, out _, out _));
        Assert.IsType<Placeholder>(current);
    }

    [Fact]
    public void MissingSupportAndUnalignedCompatibleCarriersDoNotManufactureAComparison()
    {
        var temperature = new Carrier();
        var threshold = new Carrier(temperature.UnitShape);
        temperature.Add(24);
        threshold.Add(20);
        var a = NativeRead(temperature, new Relation(temperature.ValueSlot, temperature.ValueSlot));
        var t = NativeRead(threshold, new Relation(threshold.ValueSlot, threshold.ValueSlot));
        var unaligned = QualifiedReadSurface.Separation(new Carrier(), a, t);
        Assert.False(QualifiedReadSurface.TryReadSeparation(unaligned, out _, out _));

        var basis = new Carrier(temperature.UnitShape);
        var unknown = Align(NativeRead(temperature, new Placeholder()), temperature, basis);
        var known = Align(t, threshold, basis);
        var difference = QualifiedReadSurface.Subtract(new Carrier(), unknown, known);
        Assert.True(QualifiedReadSurface.TryReadValue(difference, out var nominal));
        Assert.Equal(4, nominal.Ticks); // A known nominal is not a support guarantee.
        Assert.False(QualifiedReadSurface.TryReadBounds(difference, out _, out _));
        Assert.False(QualifiedReadSurface.TryReadSeparation(
            QualifiedReadSurface.Separation(new Carrier(), unknown, known), out _, out _));
        Assert.Equal(0, temperature.Tension);
        Assert.Equal(0, threshold.Tension);
    }

    private sealed class NumericWorld
    {
        public NumericWorld(long nominal, long limit, long low, long high)
        {
            Temperature = new Carrier();
            Threshold = new Carrier(Temperature.UnitShape);
            Basis = new Carrier(Temperature.UnitShape);
            Temperature.Add((ulong)nominal);
            Threshold.Add((ulong)limit);
            TemperatureRead = Align(NativeRead(Temperature, Relation.Segment(Temperature, low, high)), Temperature, Basis);
            ThresholdRead = Align(NativeRead(Threshold, new Relation(Threshold.ValueSlot, Threshold.ValueSlot)), Threshold, Basis);
            Difference = QualifiedReadSurface.Subtract(new Carrier(), TemperatureRead, ThresholdRead);
            Separation = QualifiedReadSurface.Separation(new Carrier(), TemperatureRead, ThresholdRead);
            DecisionCapacity = FiniteReadSurface.Capacity(new Carrier(), Basis, -100, 100);
        }

        public Carrier Temperature { get; }
        public Carrier Threshold { get; }
        public Carrier Basis { get; }
        public Relation TemperatureRead { get; }
        public Relation ThresholdRead { get; }
        public Relation Difference { get; }
        public Relation Separation { get; }
        public Relation DecisionCapacity { get; }
    }

    private static Relation NativeRead(Carrier source, IElement support) =>
        QualifiedReadSurface.Create(new Carrier(), source.ValueSlot, Relation.Segment(source, 0, 1),
            source.ValueSlot, support, new Placeholder(), new Placeholder(), new Placeholder(), source);

    private static Relation Align(Relation source, Carrier native, Carrier basis) =>
        QualifiedReadSurface.Reframe(new Carrier(), source, Equation.Affine(native, 0, 1, basis, 0, 1));

    // Explicit publication adapter, not a predicate: copy the existing read's
    // result/support into the consumer's declared finite scope. Retain both the
    // calculation and its earlier scope; introduce no arithmetic or truth flag.
    // The fixture's closed supports fit this scope; this is not a clipping law.
    private static Relation Publish(Relation calculation, Relation capacity)
    {
        Assert.True(QualifiedReadSurface.TryRead(calculation, out _, out var basis, out var value,
            out var variation, out var coverage, out var scope, out var excluded, out var provenance));
        var publication = QualifiedReadSurface.Create(new Carrier(), calculation, basis, value, variation,
            coverage, capacity, excluded, new Relation(scope, provenance));
        Assert.True(FiniteReadSurface.TryReadValue(publication, out _));
        Assert.True(FiniteReadSurface.TryReadSupport(publication, out var low, out var high));
        Assert.NotNull(low);
        Assert.NotNull(high);
        return publication;
    }

    private static void AssertBounds(Relation read, long expectedLow, long expectedHigh)
    {
        Assert.True(QualifiedReadSurface.TryReadBounds(read, out var low, out var high));
        Assert.Equal(expectedLow, low.Ticks);
        Assert.Equal(expectedHigh, high.Ticks);
        Assert.True(low.Carrier.SameIdentityAs(high.Carrier));
    }

    private static IElement[] Fields(Relation group)
    {
        Assert.True(group.TryReadOrderedGroup(out var fields));
        return fields;
    }
}
