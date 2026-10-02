using System.Numerics;
using Gojo.Core;
using Gojo.Encoding.Syntax;
using Gojo.Patterns;
using Gojo.Structures;

namespace Gojo.Encoding.Tests;

/// <summary>
/// The driver chooses events and invokes selected reads. It has no hidden truth,
/// candidate winner, confidence update, or general execution/negotiation engine.
/// </summary>
public sealed class GojoSyntaxCalibrationTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void LandmarkLeavesInterpretationOpenAndEqualPredictionsRetainDistinctCandidates(int restores)
    {
        var f = new Fixture(restores);
        Assert.False(LocalCalibration.TryPredict(G(f.O[0], f.C[2]), out var open));
        Assert.IsType<Placeholder>(F(open)[2]);
        var bound = StructuralPattern.Refine(f.C[2], R(f.C[3]));
        Number(F(Predict(f.O[0], bound))[2], f.Output, 2);
        Assert.IsType<Placeholder>(F(f.C[2])[2]);

        var proportional = Predict(f.O[0], f.C[0]);
        var offset = Predict(f.O[0], f.C[1]);
        Number(F(proportional)[2], f.Output, 2);
        Number(F(offset)[2], f.Output, 2);
        Assert.NotSame(F(proportional)[1], F(offset)[1]);
        Assert.Same(f.O[0], F(proportional)[0]);
        Assert.IsType<Placeholder>(F(F(proportional)[4])[1]);
        Assert.IsType<Placeholder>(F(F(offset)[4])[1]);
        Number(F(f.O[0])[2], f.Input, 10);

        // These forecasts are computed before either candidate sees later feedback.
        Number(F(Predict(f.O[2], f.C[0]))[2], f.Output, 4);
        Number(F(Predict(f.O[2], f.C[1]))[2], f.Output, 12);
        var agreement = f.Compare(Predict(f.O[2], f.C[0]), 2, 3);
        var disagreement = f.Compare(Predict(f.O[2], f.C[1]), 2, 3);
        Number(F(agreement)[2], f.Output, 0);
        Number(F(disagreement)[2], f.Output, -8);
        Assert.Same(f.O[3], F(F(disagreement)[4])[1]);
        Assert.Equal(0UL, f.Gain.Value);
        Assert.Equal(0UL, f.Output.Value);
    }

    [Fact]
    public void DomainRefusalRetainsNegativePredictionAndConstantMapRetainsLostInput()
    {
        var f = new Fixture();
        Assert.False(LocalCalibration.TryPredict(G(f.O[5], f.C[1]), out var refused));
        Number(F(refused)[2], f.Output, -8);
        Assert.Single(F(F(refused)[3]));
        Assert.False(LocalCalibration.TryCompare(G(refused, f.O[6], f.C[4],
            new Relation(f.O[5], f.O[6])), out _));

        var candidate = Replace(f.C[0], 2,
            new Relation(Relation.Segment(f.Input, 0, 1), Relation.Segment(f.Output, 2, 2)));
        var constant = Predict(f.O[0], candidate);
        Number(F(constant)[2], f.Output, 2);
        Assert.Same(F(f.O[0])[2], R(Assert.Single(F(F(constant)[3]))).Left);
        Number(F(f.O[0])[2], f.Input, 10);
    }

    [Fact]
    public void SourceContextSubjectAndEventCorrespondenceCannotBeInferredFromEqualNumbers()
    {
        var f = new Fixture();
        Assert.False(LocalCalibration.TryPredict(G(Replace(f.O[0], 0, G(f.Input, new Placeholder())), f.C[0]), out _));
        Assert.False(LocalCalibration.TryPredict(G(Replace(f.O[0], 4, G(new Placeholder())), f.C[0]), out _));
        var prediction = Predict(f.O[0], f.C[0]);
        foreach (var feedback in new IElement[]
        {
            Replace(f.O[1], 3, new Placeholder()),
            Replace(f.O[1], 4, G(new Placeholder())),
            Replace(f.O[1], 1, new Placeholder()),
            f.O[8]
        })
        {
            var request = G(prediction, feedback, f.C[4], new Relation(f.O[0], feedback));
            Assert.False(LocalCalibration.TryCompare(request, out var unresolved));
            Assert.IsType<Placeholder>(F(unresolved)[2]);
            Assert.Same(request, F(unresolved)[3]);
        }
        Assert.False(LocalCalibration.TryCompare(G(prediction, f.O[1], f.C[4],
            new Relation(Replace(f.O[0], 2, new CarrierLocation(f.Input, 10)), f.O[1])), out _));
        var foreign = new Carrier(f.Output.UnitShape);
        Assert.False(LocalCalibration.TryCompare(G(prediction, f.O[1],
            new Relation(Relation.Segment(foreign, 0, 1), Relation.Segment(f.Output, 0, 1)),
            new Relation(f.O[0], f.O[1])), out _));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void FullResponseChangesCalibrationAndPredictsLaterEventBeforeItsFeedback(int restores)
    {
        var f = new Fixture(restores);
        var before = f.Seed();
        var beforePrediction = Predict(f.O[0], F(before)[2]);
        Number(F(beforePrediction)[2], f.Output, 5);
        var comparison = f.Compare(beforePrediction, 0, 1);
        var plan = f.Plan(before, comparison);
        Number(F(F(plan)[2])[0], f.Gain, -30);
        Number(F(F(plan)[2])[3], f.Output, 0);
        Assert.Equal(50UL, f.Gain.Value); // Planning is observation.
        var history = R(f.P[5]);
        var after = f.Apply(plan, before, ref history);
        Assert.Equal(20UL, f.Gain.Value);
        Assert.Equal(0, f.Gain.Tension);
        Number(F(Predict(f.O[0], F(after)[2]))[2], f.Output, 2);
        Number(F(Predict(f.O[2], F(after)[2]))[2], f.Output, 4);
        Number(F(Predict(f.O[2], F(before)[2]))[2], f.Output, 10);
        Number(F(Predict(f.O[2], f.C[1]))[2], f.Output, 12);
        Number(F(beforePrediction)[2], f.Output, 5);
        Number(F(before)[4], f.Gain, 50);
        Number(F(f.O[0])[2], f.Input, 10);
        Assert.IsType<Placeholder>(F(F(after)[2])[4]);
        Assert.Single(F(history));

        // New behavior at the same exposed subject is allowed to challenge the fit.
        var drift = f.Plan(after, f.Compare(Predict(f.O[2], F(after)[2]), 2, 4));
        var drifted = f.Apply(drift, after, ref history);
        Assert.Equal(40UL, f.Gain.Value);
        Number(F(Predict(f.O[2], F(drifted)[2]))[2], f.Output, 8);
        Number(F(Predict(f.O[0], F(drifted)[2]))[2], f.Output, 4);
        Assert.Equal(2, F(history).Length);
    }

    [Fact]
    public void HalfResponseDistinguishesResolutionStallFromExplicitAcceptance()
    {
        var f = new Fixture();
        var state = f.Seed();
        var initial = state;
        var history = R(f.P[5]);
        foreach (var expected in new ulong[] { 35, 28, 24, 22, 21, 21 })
        {
            var comparison = f.Compare(Predict(f.O[0], F(state)[2]), 0, 1);
            var plan = f.Plan(state, comparison, 1);
            Assert.Same(f.O[1], F(F(comparison)[4])[1]); // Same evidence, another optimization step.
            state = f.Apply(plan, state, ref history);
            Assert.Equal(expected, f.Gain.Value);
        }
        var stalled = F(F(history)[5])[0];
        Number(F(F(stalled)[2])[0], f.Gain, -1, 2);
        Number(F(F(stalled)[2])[1], f.Gain, 0);
        Number(F(F(stalled)[2])[3], f.Output, -1, 10);
        Number(F(F(stalled)[3])[0], f.Gain, -1, 2);
        Number(F(F(stalled)[3])[1], f.Output, -1, 20);

        var currentComparison = f.Compare(Predict(f.O[0], F(state)[2]), 0, 1);
        var accepted = f.Plan(state, currentComparison, 1, 3);
        Number(F(F(accepted)[2])[0], f.Gain, 0);
        Number(F(F(accepted)[2])[3], f.Output, -1, 10);
        Number(F(currentComparison)[2], f.Output, -1, 10);
        Number(F(initial)[4], f.Gain, 50);
        Assert.NotSame(initial, state);
    }

    [Fact]
    public void ConflictingFeedbackOscillatesUntilBudgetExhaustsWithoutDeclaringAConsensus()
    {
        var f = new Fixture();
        var state = f.Seed();
        var initial = state;
        var history = R(f.P[5]);
        var steps = new List<Relation>();
        for (var i = 0; i < 4; i++)
        {
            var before = state;
            var comparison = f.Compare(Predict(f.O[2], F(state)[2]), 2, i % 2 == 0 ? 3 : 4);
            var plan = f.Plan(state, comparison);
            state = f.Apply(plan, state, ref history, 7);
            Assert.Equal(i % 2 == 0 ? 20UL : 40UL, f.Gain.Value);
            steps.Add(TimeStepSequence.Step(new Carrier(), f.Time, i, i + 1, before,
                F(history)[i], state, f.Compare(Predict(f.O[2], F(state)[2]), 2, i % 2 == 0 ? 4 : 3)));
        }
        var unsatisfied = f.Compare(Predict(f.O[2], F(state)[2]), 2, 3);
        Number(F(unsatisfied)[2], f.Output, -4);
        var next = f.Plan(state, unsatisfied);
        var request = f.Application(next, state, history, 7);
        Assert.False(LocalCalibration.TryApplyResponse(request, out var blocked, out var retained));
        Assert.Same(history, retained);
        Assert.Same(request, F(blocked)[3]);
        Assert.Equal(40UL, f.Gain.Value);

        var timeline = TimeStepSequence.Create(new Carrier(), f.Time, initial, [.. steps]);
        var restored = R(GojoNumericFormat.Decode(GojoNumericFormat.Encode(timeline))[0]);
        Assert.True(TimeStepSequence.TryRead(restored, out _, out _, out var restoredSteps));
        Assert.Equal(4, restoredSteps.Length);
        Assert.True(TimeStepSequence.TryReadStep(restoredSteps[3], out _, out _, out _,
            out _, out _, out _, out var unresolved));
        Assert.True(ProjectionResidue.TryRead(F(unresolved)[2], out _, out var n, out var d));
        Assert.Equal(-4 * d, n);
    }

    [Fact]
    public void DuplicateAndStalePlansCannotApplyEvenWhenScalarStateHasReturnedToTheSameValue()
    {
        var f = new Fixture();
        var initial = f.Seed();
        var first = f.Plan(initial, f.Compare(Predict(f.O[0], F(initial)[2]), 0, 1));
        var history = R(f.P[5]);
        var current = f.Apply(first, initial, ref history);
        Assert.False(LocalCalibration.TryApplyResponse(f.Application(first, current, history), out _, out var retained));
        Assert.Same(history, retained);
        Assert.False(LocalCalibration.TryApplyResponse(f.Application(first, initial, history), out _, out _));

        // A freshly captured equal scalar is another version, not the plan's premise.
        var second = f.Plan(current, f.Compare(Predict(f.O[0], F(current)[2]), 0, 1));
        Assert.True(LocalCalibration.TryCapture(f.Recipe, out var recaptured));
        Assert.False(LocalCalibration.TryApplyResponse(f.Application(second, recaptured, history), out _, out _));
        current = f.Apply(second, current, ref history); // An explicit zero application still advances history.
        Assert.False(LocalCalibration.TryApplyResponse(f.Application(second, current, history), out _, out _));
        Assert.Equal(20UL, f.Gain.Value);
        Assert.Equal(2, F(history).Length);

        f.Gain.Add(30); // Return to the original physical value through a separate action.
        Assert.False(LocalCalibration.TryApplyResponse(f.Application(first, initial, history), out _, out _));
        Assert.Equal(50UL, f.Gain.Value);
    }

    [Fact]
    public void UnpaidDrawRemainsTensionAndBlocksFurtherPlanningUntilResolved()
    {
        var f = new Fixture();
        var before = f.Seed();
        var plan = f.Plan(before, f.Compare(Predict(f.O[0], F(before)[2]), 0, 7));
        var history = R(f.P[5]);
        var after = f.Apply(plan, before, ref history);
        Assert.Equal(0UL, f.Gain.Value);
        Assert.Equal(-20, f.Gain.Tension);
        Number(F(after)[3], f.Gain, -20);
        Number(F(F(F(history)[0])[3])[1], f.Gain, -20);
        var comparison = f.Compare(Predict(f.O[0], F(after)[2]), 0, 1);
        Assert.False(LocalCalibration.TryPlanResponse(f.Planning(after, comparison), out var pending));
        Assert.IsType<Placeholder>(F(pending)[2]);
        // A requested negative quantity did not overturn the nonnegative output floor.
        Number(F(Predict(f.O[0], F(after)[2]))[2], f.Output, 0);
        Assert.Equal(-20, f.Gain.Tension);
    }

    [Fact]
    public void ZeroSensitivityRefusesImpossibleResponseButCanRetainAnAlreadyAcceptableRead()
    {
        var f = new Fixture();
        var state = f.Seed();
        var prediction = Predict(f.O[5], F(state)[2]);
        var mismatch = f.Compare(prediction, 5, 6);
        Assert.False(LocalCalibration.TryPlanResponse(f.Planning(state, mismatch), out _));
        var zeroFeedback = Replace(f.O[6], 2, new CarrierLocation(f.Output, 0));
        Assert.True(LocalCalibration.TryCompare(G(prediction, zeroFeedback, f.C[4],
            new Relation(f.O[5], zeroFeedback)), out var acceptable));
        var plan = f.Plan(state, acceptable);
        Number(F(F(plan)[2])[0], f.Gain, 0);
        Assert.IsType<Placeholder>(F(F(plan)[2])[2]);
        Assert.Equal(50UL, f.Gain.Value);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void RestorePendingPlanWithItsWholeContextThenContinueWithoutLosingVersionOrEvidence(int restores)
    {
        var original = new Fixture();
        var state = original.Seed();
        var history = R(original.P[5]);
        var first = original.Plan(state, original.Compare(Predict(original.O[0], F(state)[2]), 0, 1), 1);
        state = original.Apply(first, state, ref history);
        var next = original.Plan(state, original.Compare(Predict(original.O[0], F(state)[2]), 0, 1), 1);
        var packet = new IElement[] { G(original.Roots), state, history, next };
        for (var i = 0; i < restores; i++) packet = GojoNumericFormat.Decode(GojoNumericFormat.Encode(packet));
        var f = new Fixture(F(packet[0]));
        state = R(packet[1]); history = R(packet[2]); next = R(packet[3]);
        Assert.Same(state, F(F(next)[1])[0]);
        var after = f.Apply(next, state, ref history);
        Assert.Equal(28UL, f.Gain.Value);
        Number(F(state)[4], f.Gain, 35);
        Number(F(after)[4], f.Gain, 28);
        Assert.Equal(2, F(history).Length);
        Assert.Same(f.O[1], F(F(F(next)[0])[1])[1]);
        Assert.False(LocalCalibration.TryApplyResponse(f.Application(next, after, history), out _, out _));
    }

    [Fact]
    public void AuthoredActionsConnectTheRequestPatternsAndFeedbackSlots()
    {
        var f = new Fixture();
        Assert.True(RepeatSurface.TryRead(R(f.Roots[6]), out var focus, out var controller,
            out var transition, out var feedback, out _, out var timeline));
        Assert.Same(f.P[4], focus);
        Assert.Equal(8, Assert.IsType<CarrierLocation>(R(R(controller).Right).Left).Ticks);
        var actions = F(transition);
        Assert.True(TransformSurface.TryRead(R(actions[0]), out _, out var planning, out var planPorts, out _, out _));
        Assert.Same(f.Roots[4], planning);
        Assert.Same(f.Slots[4], Assert.Single(F(planPorts)));
        Assert.True(TransformSurface.TryRead(R(actions[1]), out _, out var applying, out var outputs, out _, out _));
        Assert.Same(f.Roots[5], applying);
        Assert.Same(F(outputs)[0], R(F(feedback)[0]).Left);
        Assert.Same(f.Slots[0], R(F(feedback)[0]).Right);
        Assert.Same(F(outputs)[1], R(F(feedback)[1]).Left);
        Assert.Same(f.Slots[5], R(F(feedback)[1]).Right);
        Assert.True(TimeStepSequence.TryRead(R(timeline), out _, out var initial, out var steps));
        Assert.Same(f.Slots[0], initial);
        Assert.Empty(steps);
    }

    [Theory]
    [InlineData(false, -30, 20)]
    [InlineData(true, 30, 80)]
    public void ResponseDirectionFollowsTheCalibratedSensitivity(bool reversed, long delta, ulong final)
    {
        var f = new Fixture();
        if (reversed)
            f.Roots[2] = Replace(f.Recipe, 3, new Relation(Relation.Segment(f.Gain, 0, 10),
                Relation.Segment(f.Output, 10, 9)));
        var initial = f.Seed();
        var prediction = Predict(f.O[0], F(initial)[2]);
        var plan = f.Plan(initial, f.Compare(prediction, 0, 1));
        Number(F(F(plan)[2])[0], f.Gain, delta);

        // Independent finite perturbation checks the model response used by the planner.
        var probe = new Fixture();
        if (reversed)
            probe.Roots[2] = Replace(probe.Recipe, 3, new Relation(Relation.Segment(probe.Gain, 0, 10),
                Relation.Segment(probe.Output, 10, 9)));
        probe.Seed();
        probe.Gain.Add(1);
        Assert.True(LocalCalibration.TryCapture(probe.Recipe, out var perturbed));
        Number(F(Predict(probe.O[0], F(perturbed)[2]))[2], probe.Output, reversed ? 49 : 51, 10);
        var history = R(f.P[5]);
        var after = f.Apply(plan, initial, ref history);
        Assert.Equal(final, f.Gain.Value);
        Number(F(Predict(f.O[0], F(after)[2]))[2], f.Output, 2);
    }

    [Fact]
    public void InvalidArithmeticPolicyAndChangedActuatorDoNotBecomeMaterialChanges()
    {
        var f = new Fixture();
        var state = f.Seed();
        var prediction = Predict(f.O[0], F(state)[2]);
        Assert.False(LocalCalibration.TryCompare(G(Replace(prediction, 2, new CarrierLocation(f.Output, 999)),
            f.O[1], f.C[4], new Relation(f.O[0], f.O[1])), out _));
        var comparison = f.Compare(prediction, 0, 1);
        Assert.False(LocalCalibration.TryPlanResponse(f.Planning(state,
            Replace(comparison, 2, new CarrierLocation(f.Output, 999))), out _));
        var request = f.Planning(state, comparison);
        foreach (var policy in new IElement[]
        {
            new Relation(Relation.Segment(f.Output, 0, 1), Relation.Segment(f.Output, 0, 2)),
            new Relation(Relation.Segment(f.Output, 0, 1), Relation.Segment(f.Output, 0, 0)),
            new Relation(Relation.Segment(f.Output, 0, 1), Relation.Segment(f.Output, 0, -1))
        }) Assert.False(LocalCalibration.TryPlanResponse(Replace(request, 2, policy), out _));
        Assert.False(LocalCalibration.TryPlanResponse(Replace(request, 3, Relation.Segment(f.Output, 1, 2)), out _));
        Assert.False(LocalCalibration.TryPlanResponse(Replace(request, 4,
            Replace(f.P[4], 2, G(F(f.Recipe)[1], new Carrier()))), out _));

        var plan = f.Plan(state, comparison);
        var changedPlan = Replace(plan, 2, Replace(F(plan)[2], 1, new CarrierLocation(f.Gain, -29)));
        var history = R(f.P[5]);
        Assert.False(LocalCalibration.TryApplyResponse(f.Application(changedPlan, state, history), out _, out var retained));
        Assert.Same(history, retained);
        Assert.Equal(50UL, f.Gain.Value);
        f.Gain.Add(1);
        Assert.False(LocalCalibration.TryApplyResponse(f.Application(plan, state, history), out _, out retained));
        Assert.Same(history, retained);
        Assert.Equal(51UL, f.Gain.Value);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void WiderSettlementWorldRequiresItsOwnMutationContract(bool opposed)
    {
        var f = new Fixture();
        var state = f.Seed();
        var plan = f.Plan(state, f.Compare(Predict(f.O[0], F(state)[2]), 0, 1));
        if (opposed) f.Gain.Inverse();
        else f.Gain.Attach(new Relation(Relation.Point(f.Gain, 100), Relation.Point(new Carrier(), 0)));
        var history = R(f.P[5]);
        Assert.False(LocalCalibration.TryApplyResponse(f.Application(plan, state, history), out _, out var retained));
        Assert.Same(history, retained);
        Assert.Equal(50UL, f.Gain.Value);
        Assert.Equal(0, f.Gain.Tension);
    }

    [Fact]
    public void UnrepresentableApplicationIsRefusedBeforeMutation()
    {
        var f = new Fixture();
        f.Roots[2] = Replace(f.Recipe, 3, new Relation(
            Relation.Segment(f.Gain, long.MaxValue - 2, long.MaxValue - 1), Relation.Segment(f.Output, 0, 1)));
        f.Gain.Add((ulong)long.MaxValue - 1);
        Assert.True(LocalCalibration.TryCapture(f.Recipe, out var state));
        var prediction = Predict(f.O[0], F(state)[2]);
        Number(F(prediction)[2], f.Output, 1);
        var feedback = Replace(f.O[1], 2, new CarrierLocation(f.Output, 4));
        Assert.True(LocalCalibration.TryCompare(G(prediction, feedback, f.C[4],
            new Relation(f.O[0], feedback)), out var comparison));
        var plan = f.Plan(state, comparison);
        Number(F(F(plan)[2])[1], f.Gain, 3);
        var history = R(f.P[5]);
        Assert.False(LocalCalibration.TryApplyResponse(f.Application(plan, state, history), out _, out var retained));
        Assert.Same(history, retained);
        Assert.Equal((ulong)long.MaxValue - 1, f.Gain.Value);
        Assert.Equal(0, f.Gain.Tension);
    }

    [Fact]
    public void SyntaxNamesAreAuthorLabelsAndRestoredDebtRemainsPending()
    {
        var source = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "Syntax", "landmark-calibration.gojo"));
        var names = new Dictionary<string, string>();
        var renamed = System.Text.RegularExpressions.Regex.Replace(source, @"[?$][A-Za-z_][A-Za-z_0-9]*", match =>
        {
            if (!names.TryGetValue(match.Value, out var name))
                names[match.Value] = name = match.Value[0] + "renamed_" + names.Count;
            return name;
        });
        var f = new Fixture(GojoSyntax.Parse(renamed).Roots.ToArray());
        var state = f.Seed();
        var history = R(f.P[5]);
        var plan = f.Plan(state, f.Compare(Predict(f.O[0], F(state)[2]), 0, 7));
        state = f.Apply(plan, state, ref history);
        var packet = new IElement[] { G(f.Roots), state, history };
        for (var i = 0; i < 2; i++) packet = GojoNumericFormat.Decode(GojoNumericFormat.Encode(packet));
        f = new Fixture(F(packet[0])); state = R(packet[1]); history = R(packet[2]);
        Assert.Equal(-20, f.Gain.Tension);
        Number(F(state)[3], f.Gain, -20);
        Assert.False(LocalCalibration.TryPlanResponse(f.Planning(state,
            f.Compare(Predict(f.O[0], F(state)[2]), 0, 1)), out _));
        Assert.Single(F(history));
    }

    private sealed class Fixture
    {
        public IElement[] Roots { get; }
        public IElement[] O => F(Roots[0]);
        public IElement[] C => F(Roots[1]);
        public Relation Recipe => R(Roots[2]);
        public IElement[] P => F(Roots[3]);
        public IElement[] Slots => F(Roots[7]);
        public Carrier Gain => Assert.IsType<Carrier>(F(Recipe)[2]);
        public Carrier Input => Assert.IsType<CarrierLocation>(F(O[0])[2]).Carrier;
        public Carrier Output => Assert.IsType<CarrierLocation>(F(O[1])[2]).Carrier;
        public Carrier Time => Assert.IsType<CarrierLocation>(F(O[0])[1]).Carrier;
        public Fixture(int restores = 0)
        {
            Roots = GojoSyntax.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory,
                "Fixtures", "Syntax", "landmark-calibration.gojo"))).Roots.ToArray();
            for (var i = 0; i < restores; i++) Roots = GojoNumericFormat.Decode(GojoNumericFormat.Encode(Roots));
        }
        public Fixture(IElement[] roots) => Roots = roots;
        public Relation Seed()
        {
            Gain.Add(checked((ulong)Assert.IsType<CarrierLocation>(R(P[8]).Left).Ticks));
            Assert.True(LocalCalibration.TryCapture(Recipe, out var state));
            return state;
        }
        public Relation Compare(Relation prediction, int report, int feedback)
        {
            Assert.True(LocalCalibration.TryCompare(G(prediction, O[feedback], C[4],
                new Relation(O[report], O[feedback])), out var witness));
            return witness;
        }
        public Relation Planning(Relation state, Relation comparison, int relaxation = 0, int acceptance = 2) =>
            R(StructuralPattern.Refine(Roots[4], new Relation(Slots[0], state),
                new Relation(Slots[1], comparison), new Relation(Slots[2], P[relaxation]),
                new Relation(Slots[3], P[acceptance])));
        public Relation Plan(Relation state, Relation comparison, int relaxation = 0, int acceptance = 2)
        {
            Assert.True(LocalCalibration.TryPlanResponse(Planning(state, comparison, relaxation, acceptance), out var plan));
            return plan;
        }
        public Relation Application(Relation plan, Relation state, Relation history, int controller = 6) =>
            R(StructuralPattern.Refine(Roots[5], new Relation(Slots[4], plan), new Relation(Slots[0], state),
                new Relation(Slots[5], history), new Relation(Slots[6], P[controller])));
        public Relation Apply(Relation plan, Relation state, ref Relation history, int controller = 6)
        {
            Assert.True(LocalCalibration.TryApplyResponse(Application(plan, state, history, controller),
                out var application, out history));
            return R(F(F(application)[2])[2]);
        }
    }

    private static Relation Predict(IElement observation, IElement candidate)
    {
        Assert.True(LocalCalibration.TryPredict(G(observation, candidate), out var prediction));
        return prediction;
    }
    private static Relation Replace(IElement group, int index, IElement value)
    {
        var fields = F(group); fields[index] = value; return G(fields);
    }
    private static Relation R(IElement e) => Assert.IsType<Relation>(e);
    private static Relation G(params IElement[] fields) => Relation.OrderedGroup(new Carrier(), fields);
    private static IElement[] F(IElement e)
    {
        Assert.True(R(e).TryReadOrderedGroup(out var fields)); return fields;
    }
    private static void Number(IElement e, Carrier basis, long numerator, long denominator = 1)
    {
        Assert.True(ProjectionResidue.TryRead(e, out var actual, out var n, out var d));
        Assert.Same(basis, actual);
        Assert.Equal((BigInteger)numerator * d, (BigInteger)n * denominator);
    }
}
