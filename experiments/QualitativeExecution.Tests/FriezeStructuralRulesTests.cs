using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeFrieze;

namespace Gojo.Experiments.QualitativeExecution.Tests;

public sealed class FriezeStructuralRulesTests
{
    [Fact]
    public void ExplicitPositiveReachAndPhaseCapacityPremisesRemainNative()
    {
        var frame = new QualitativeMotionFrame(); var h = new Placeholder(); var v = new Placeholder();
        var fixture = Sequence(frame, W(frame.Right, h), W(frame.Up, v), W(frame.Right, h), W(frame.Down, v));
        var rules = new FriezeStructuralRules(fixture, Slots(4)); var report = rules.Evaluate(fixture.Run(8));
        Assert.True(rules.TryReadResult(report, out var passed, out var violations, out var unresolved));
        Assert.NotEmpty(E(passed)); Assert.Empty(E(violations)); Assert.Empty(E(unresolved));
        Assert.Equal(2, E(rules.PositiveReaches).Length);
        foreach (var premise in E(rules.PositiveReaches).Cast<Relation>())
        {
            var order = Assert.IsType<Relation>(premise.Right);
            Assert.Same(frame.Origin, order.Left); Assert.NotSame(order.Left, order.Right);
            Assert.Contains(premise.Left, fixture.Reaches);
        }
        Assert.True(Contains(rules.Surface, frame.Opposites)); Assert.True(Contains(rules.Surface, frame.Exchange));
        Assert.True(Contains(rules.Surface, rules.ComplexitySlots)); AssertNoNumbers(report);
    }

    [Fact]
    public void AdjacentOppositeAxisStrokesRetraceEvenWithDistinctPositiveExtents()
    {
        var frame = new QualitativeMotionFrame(); var a = new Placeholder(); var b = new Placeholder(); var v = new Placeholder();
        var fixture = Sequence(frame, W(frame.Right, a), W(frame.Left, b), W(frame.Up, v), W(frame.Right, a), W(frame.Down, v));
        var rules = new FriezeStructuralRules(fixture, Slots(8)); var report = rules.Evaluate(fixture.Run(10));
        Assert.NotEmpty(Under(rules, report, rules.LocalRetraceRule, violations: true));
        Assert.NotSame(a, b);
    }

    [Fact]
    public void PerpendicularInterveningMotionAvoidsFalseProjectedRetracing()
    {
        var frame = new QualitativeMotionFrame(); var h = new Placeholder(); var v = new Placeholder();
        var fixture = Sequence(frame, W(frame.Right, h), W(frame.Up, v), W(frame.Left, h), W(frame.Down, v), W(frame.Right, h));
        var rules = new FriezeStructuralRules(fixture, Slots(5)); var report = rules.Evaluate(fixture.Run(10));
        Assert.True(rules.TryReadResult(report, out _, out var violations, out var unresolved));
        Assert.Empty(E(violations)); Assert.Empty(E(unresolved));
        // The final right stroke overlaps an older nonadjacent edge. This
        // certificate is explicitly local; the global segment rule must reject it.
    }

    [Fact]
    public void KnownZeroPausesDoNotSeparateTwoOppositeStrokes()
    {
        var frame = new QualitativeMotionFrame(); var h = new Placeholder(); var v = new Placeholder();
        var fixture = Sequence(frame, W(frame.Right, h), S(), W(frame.Left, h), W(frame.Up, v), W(frame.Right, h), W(frame.Down, v));
        var rules = new FriezeStructuralRules(fixture, Slots(6)); var report = rules.Evaluate(fixture.Run(12));
        Assert.NotEmpty(Under(rules, report, rules.LocalRetraceRule, violations: true));
    }

    [Fact]
    public void CompletePeriodChecksItsLastToFirstStrokeSeam()
    {
        var frame = new QualitativeMotionFrame(); var h = new Placeholder(); var v = new Placeholder();
        var fixture = Sequence(frame, W(frame.Right, h), W(frame.Up, v), W(frame.Right, h), W(frame.Down, v), W(frame.Left, h));
        var rules = new FriezeStructuralRules(fixture, Slots(5)); var report = rules.Evaluate(fixture.Run(5));
        var violation = Assert.Single(Under(rules, report, rules.LocalRetraceRule, violations: true));
        Assert.True(rules.TryReadPeriod(report, out var samples, out _));
        var pair = Assert.IsType<Relation>(Assert.IsType<Relation>(violation.Right).Left);
        Assert.Same(E(samples)[^1], pair.Left); Assert.Same(E(samples)[0], pair.Right);
        Assert.NotEmpty(Under(rules, report, rules.PeriodRule, violations: false));
    }

    [Fact]
    public void ExactOppositeDiagonalVectorsRetraceWithoutMeasuredLengths()
    {
        var frame = new QualitativeMotionFrame(); var h = new Placeholder(); var v = new Placeholder();
        var fixture = Sequence(frame, S(M(frame.Right, h), M(frame.Up, v)), S(M(frame.Left, h), M(frame.Down, v)), W(frame.Right, h));
        var rules = new FriezeStructuralRules(fixture, Slots(3)); var report = rules.Evaluate(fixture.Run(6));
        Assert.Single(Under(rules, report, rules.LocalRetraceRule, violations: true));
        Assert.NotEmpty(Under(rules, report, rules.PeriodRule, violations: false));
    }

    [Fact]
    public void OppositeDiagonalQuadrantsRetainACollinearityObligation()
    {
        var frame = new QualitativeMotionFrame();
        var fixture = Sequence(frame, S(M(frame.Right, new Placeholder()), M(frame.Up, new Placeholder())),
            S(M(frame.Left, new Placeholder()), M(frame.Down, new Placeholder())), W(frame.Right, new Placeholder()));
        var rules = new FriezeStructuralRules(fixture, Slots(3)); var report = rules.Evaluate(fixture.Run(6));
        Assert.Empty(Under(rules, report, rules.LocalRetraceRule, violations: true));
        Assert.NotEmpty(Pending(rules, report, rules.LocalRetraceRule));
        Assert.True(Contains(report, fixture.ReachOrder));
    }

    [Fact]
    public void GroupedCancellationIsAZeroSampleRatherThanTwoRetracingStrokes()
    {
        var frame = new QualitativeMotionFrame(); var h = new Placeholder();
        var grouped = Sequence(frame, S(M(frame.Right, h), M(frame.Left, h)), W(frame.Right, h));
        var separate = Sequence(frame, W(frame.Right, h), W(frame.Left, h), W(frame.Right, h));
        var groupedRules = new FriezeStructuralRules(grouped, Slots(3));
        var separateRules = new FriezeStructuralRules(separate, Slots(3));
        var groupedRead = groupedRules.Evaluate(grouped.Run(4)); var separateRead = separateRules.Evaluate(separate.Run(6));
        Assert.Empty(Under(groupedRules, groupedRead, groupedRules.LocalRetraceRule, violations: true));
        Assert.Empty(Pending(groupedRules, groupedRead, groupedRules.LocalRetraceRule));
        Assert.NotEmpty(Under(separateRules, separateRead, separateRules.LocalRetraceRule, violations: true));
    }

    [Fact]
    public void MixedExtentDeltaRetainsItsThresholdAndOrderEvidence()
    {
        var frame = new QualitativeMotionFrame(); var drift = new Placeholder(); var bounce = new Placeholder();
        var order = S(new Relation(drift, bounce));
        var fixture = FriezeFixture.Sequence(frame, S(S(M(frame.Right, drift), M(frame.Left, bounce)), W(frame.Right, bounce)),
            reachOrder: order);
        var rules = new FriezeStructuralRules(fixture, Slots(2)); var report = rules.Evaluate(fixture.Run(4));
        Assert.NotEmpty(Pending(rules, report, rules.LocalRetraceRule));
        Assert.True(Contains(report, order));
        Assert.NotEmpty(Under(rules, report, rules.PeriodRule, violations: false));
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void APeriodMustHavePositiveHorizontalAndNeutralVerticalDisplacement(bool verticalDrift)
    {
        var frame = new QualitativeMotionFrame(); var h = new Placeholder(); var v = new Placeholder();
        var fixture = verticalDrift ? Sequence(frame, W(frame.Right, h), W(frame.Up, v)) : Sequence(frame, W(frame.Left, h));
        var rules = new FriezeStructuralRules(fixture, Slots(3)); var report = rules.Evaluate(fixture.Run(4));
        Assert.NotEmpty(Under(rules, report, rules.PeriodRule, violations: true));
    }

    [Fact]
    public void ZeroPeriodFailsRightwardProgressInsteadOfBecomingAnEmptySuccess()
    {
        var frame = new QualitativeMotionFrame(); var fixture = Sequence(frame, S());
        var rules = new FriezeStructuralRules(fixture, Slots(1)); var report = rules.Evaluate(fixture.Run(2));
        Assert.NotEmpty(Under(rules, report, rules.PeriodRule, violations: true));
        Assert.True(rules.TryReadPeriod(report, out _, out var displacement));
        Assert.True(frame.TryReadResult(displacement, out var residual, out _, out _)); Assert.Empty(E(residual));
    }

    [Fact]
    public void IncompleteControlReturnAndMalformedInputRemainUnresolved()
    {
        var fixture = FriezeFixture.SquareWave(); var rules = new FriezeStructuralRules(fixture, Slots(8));
        foreach (var trace in new[] { fixture.Run(3), new Relation(new Placeholder(), new Placeholder()) })
        {
            var report = rules.Evaluate(trace);
            Assert.False(rules.TryReadPeriod(report, out _, out _));
            Assert.NotEmpty(Pending(rules, report, rules.PeriodRule));
            Assert.NotEmpty(Pending(rules, report, rules.LocalRetraceRule));
            Assert.True(Contains(report, trace));
        }
    }

    [Fact]
    public void MissingOutputLeavesOpenPeriodMotionAndCannotPassTheHardRules()
    {
        var fixture = FriezeFixture.SquareWave(false);
        Assert.True(FiniteLaw.TryRead(fixture.OutputLaw, out var left, out var right, out var output, out var rows));
        var incomplete = fixture.WithOutputs(FiniteLaw.Create(left, right, output, rows.Skip(1).ToArray()));
        var rules = new FriezeStructuralRules(incomplete, Slots(4)); var report = rules.Evaluate(incomplete.Run(8));
        Assert.NotEmpty(Pending(rules, report, rules.PeriodRule));
        Assert.NotEmpty(Pending(rules, report, rules.LocalRetraceRule));
        Assert.True(rules.TryReadPeriod(report, out _, out var displacement));
        Assert.True(fixture.Frame.TryReadResult(displacement, out var remaining, out _, out _));
        Assert.True(StructuralConstraint.TryReadScope(remaining, out _, out var seam, out var tail)); Assert.NotEqual(seam, tail);
    }

    [Fact]
    public void ComplexityCountsGeneratorPhaseParticipationRatherThanSamplesOrRunLength()
    {
        var fixture = FriezeFixture.SlowFastDrift();
        var individual = fixture.WithSampling(fixture.Network.Ports);
        var shortRules = new FriezeStructuralRules(fixture, Slots(7));
        var individualRules = new FriezeStructuralRules(individual, Slots(7));
        var first = shortRules.Evaluate(fixture.Run(6)); var second = individualRules.Evaluate(individual.Run(12));
        Assert.Single(Under(shortRules, first, shortRules.ComplexityRule, violations: true));
        Assert.Single(Under(individualRules, second, individualRules.ComplexityRule, violations: true));
        Assert.Equal(8, E((Relation)shortRules.ComplexityRule.Left).Length);
        Assert.Equal(8, E((Relation)individualRules.ComplexityRule.Left).Length);
        var admitted = new FriezeStructuralRules(fixture, Slots(8));
        Assert.Single(Under(admitted, admitted.Evaluate(fixture.Run(6)), admitted.ComplexityRule, violations: false));
    }

    [Fact]
    public void OpenComplexityCapacityRemainsAnExplicitObligation()
    {
        var fixture = FriezeFixture.SquareWave(false);
        var rules = new FriezeStructuralRules(fixture, StructuralConstraint.Open(new Placeholder()));
        var report = rules.Evaluate(fixture.Run(4));
        Assert.Single(Pending(rules, report, rules.ComplexityRule));
        Assert.True(rules.TryReadPeriod(report, out _, out _));
    }

    [Fact]
    public void UnsampledPeriodContributionsCannotBeSilentlyDiscarded()
    {
        var fixture = FriezeFixture.SquareWave(false).WithSampling(S());
        var rules = new FriezeStructuralRules(fixture, Slots(4)); var report = rules.Evaluate(fixture.Run(8));
        Assert.False(rules.TryReadPeriod(report, out _, out _));
        Assert.NotEmpty(Pending(rules, report, rules.PeriodRule));
        Assert.True(Contains(report, fixture.Sampling));
    }

    private static FriezeFixture Sequence(QualitativeMotionFrame frame, params Relation[] words) =>
        FriezeFixture.Sequence(frame, S(words.Cast<IElement>().ToArray()));
    private static Relation W(IElement direction, IElement reach) => S(M(direction, reach));
    private static Relation M(IElement direction, IElement reach) => QualitativeMotionFrame.Move(direction, reach);
    private static Relation Slots(int count) => S(Enumerable.Range(0, count).Select(_ => (IElement)new Placeholder()).ToArray());

    private static Relation[] Under(FriezeStructuralRules rules, Relation report, Relation rule, bool violations)
    {
        Assert.True(rules.TryReadResult(report, out var passed, out var failed, out _));
        return E(violations ? failed : passed).OfType<Relation>().Where(entry => ReferenceEquals(entry.Left, rule)).ToArray();
    }
    private static Relation[] Pending(FriezeStructuralRules rules, Relation report, Relation rule)
    {
        Assert.True(rules.TryReadResult(report, out _, out _, out var pending));
        return E(pending).OfType<Relation>().Where(entry => ReferenceEquals(entry.Left, rule)).ToArray();
    }
    private static Relation S(params IElement[] entries) => StructuralConstraint.Closed(entries);
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
    private static bool Contains(IElement root, IElement target) => Traverse(root).Any(element => ReferenceEquals(element, target));
    private static void AssertNoNumbers(IElement root)
    {
        foreach (var element in Traverse(root)) { Assert.IsNotType<Carrier>(element); Assert.IsNotType<CarrierLocation>(element); }
    }
    private static IEnumerable<IElement> Traverse(IElement root)
    {
        var seen = new HashSet<IElement>(ReferenceEqualityComparer.Instance); var pending = new Stack<IElement>(); pending.Push(root);
        while (pending.TryPop(out var element))
        {
            if (!seen.Add(element)) continue; yield return element;
            if (element is Relation relation) { pending.Push(relation.Left); pending.Push(relation.Right); }
        }
    }
}
