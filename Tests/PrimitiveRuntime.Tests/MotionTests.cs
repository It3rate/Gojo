using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;

namespace Gojo.Experiments.PrimitiveRuntime.Tests;

public sealed class MotionTests
{
    [Fact]
    public void MotionDefinitionHasNoCarrierCoordinateMagnitudeOrNumericClock()
    {
        var definition = new MotionDefinition();
        var visited = new HashSet<IElement>(ReferenceEqualityComparer.Instance);
        var pending = new Stack<IElement>(); pending.Push(definition.Surface);
        while (pending.TryPop(out var element))
        {
            if (!visited.Add(element)) continue;
            Assert.True(element is Relation or Placeholder);
            if (element is Relation relation) { pending.Push(relation.Left); pending.Push(relation.Right); }
        }
        Assert.Contains(definition.Law, visited);
        Assert.Contains(definition.Feedback, visited);
        Assert.Same(definition.Output, definition.Feedback.Left);
        Assert.Same(definition.Input, definition.Feedback.Right);
    }

    [Fact]
    public void SharedSiteMovesWhileIndependentInitiallyEqualBindingDoesNotBecomeShared()
    {
        var motion = new PrimitiveMotion(); var definition = motion.Definition;
        var connection = motion.Plan.Arena[motion.Plan.Arena.AddressOf(definition.Connection)];
        var attached = motion.Plan.Arena[motion.Plan.Arena.AddressOf(definition.AttachedConnection)];
        var independent = motion.Plan.Arena[motion.Plan.Arena.AddressOf(definition.IndependentConnection)];
        Assert.Equal(connection.Right, attached.Left);
        Assert.NotEqual(connection.Right, independent.Right);
        Assert.Equal(1, Assert.Single(motion.Observe().Candidates).Landmark);
        motion.Step();
        Assert.Equal(2, Assert.Single(motion.Observe().Candidates).Landmark);
        Assert.Same(definition.Moving, definition.Connection.Right);
        Assert.Same(definition.Moving, definition.AttachedConnection.Left);
        Assert.NotSame(definition.Moving, definition.Independent);
        Assert.Equal(connection, motion.Plan.Arena[motion.Plan.Arena.AddressOf(definition.Connection)]);
        Assert.Equal(independent, motion.Plan.Arena[motion.Plan.Arena.AddressOf(definition.IndependentConnection)]);
    }

    [Fact]
    public void PulseFollowsAuthoredControllerIncludingTurnBeforeReturn()
    {
        var motion = new PrimitiveMotion();
        var expected = new[] { (2, true), (3, true), (3, false), (2, false),
            (1, false), (0, false), (0, true), (1, true) };
        foreach (var (landmark, forward) in expected)
        {
            motion.Step(); var candidate = Assert.Single(motion.Observe().Candidates);
            Assert.Equal(landmark, candidate.Landmark); Assert.Equal(forward, candidate.Forward);
            Assert.True(motion.Occurrence.CurrentReceipt.Applied);
        }
        Assert.Equal(8UL, motion.Occurrence.CurrentStep);
    }

    [Fact]
    public void ObserverReversalIsReadOnlyAndTravelReversalIsExplicitInput()
    {
        var motion = new PrimitiveMotion(); var before = motion.Occurrence.State;
        var forward = motion.Observe(false, 1); var reversed = motion.Observe(true, 1);
        Assert.Equal(before, motion.Occurrence.State); Assert.Equal(0UL, motion.Occurrence.CurrentStep);
        Assert.Equal(3, Assert.Single(forward.Candidates).Ticks);
        Assert.Equal(9, Assert.Single(reversed.Candidates).Ticks);
        Assert.Equal(forward.CalibratedTicks, reversed.CalibratedTicks);
        motion.ReverseTravel();
        var changed = Assert.Single(motion.Observe().Candidates);
        Assert.Equal(1, changed.Landmark); Assert.False(changed.Forward);
        motion.Step(); Assert.Equal(0, Assert.Single(motion.Observe().Candidates).Landmark);
    }

    [Fact]
    public void UnknownContradictionAndKnownAlternativesAreDistinctAndNeverChooseAPath()
    {
        var motion = new PrimitiveMotion();
        motion.SetUnknown(); motion.Step();
        Assert.True(motion.Occurrence.State.IsOpen); Assert.Equal(0UL, motion.Occurrence.State.Known);
        Assert.Equal(CompactStepStatus.Open, motion.Occurrence.CurrentReceipt.Status);
        motion.SetAlternatives(); motion.Step();
        Assert.False(motion.Occurrence.State.IsOpen);
        var alternatives = motion.Observe().Candidates;
        Assert.Equal(2, alternatives.Length);
        Assert.Contains(alternatives, item => item.Landmark == 0 && item.Forward);
        Assert.Contains(alternatives, item => item.Landmark == 3 && !item.Forward);
        Assert.Equal(CompactStepStatus.Ambiguous, motion.Occurrence.CurrentReceipt.Status);
        motion.Occurrence.Import(CompactSupport.Closed(0)); motion.Step();
        Assert.False(motion.Occurrence.State.IsOpen); Assert.Equal(0UL, motion.Occurrence.State.Known);
        Assert.Equal(CompactStepStatus.Contradiction, motion.Occurrence.CurrentReceipt.Status);
    }

    [Fact]
    public void MissingTransitionKeepsInputAndRefusedAttemptWithoutInventingNumericTension()
    {
        var motion = new PrimitiveMotion(); var before = motion.Occurrence.State;
        motion.Block(); var receipt = motion.Occurrence.CurrentReceipt;
        Assert.Equal(before, receipt.After); Assert.Equal(before, motion.Occurrence.State);
        Assert.Equal(CompactStepStatus.NoTransition, receipt.Status);
        Assert.Equal(1u, receipt.MissingPairs); Assert.Equal(0UL, receipt.Result.Known);
        Assert.NotEqual(0UL, receipt.Command.Known);
        motion.Step(); Assert.Equal(CompactStepStatus.Applied, motion.Occurrence.CurrentReceipt.Status);
    }

    [Fact]
    public void CapturedCurrentBindingConnectsMovingSiteAndRemainsStableAcrossLaterSteps()
    {
        var motion = new PrimitiveMotion();
        Assert.Equal(motion.Plan.Arena.AddressOf(motion.Definition.Input), motion.Occurrence.StateSlotAddress);
        var initial = motion.Inspect();
        motion.Step();
        var receipt = motion.Occurrence.CurrentReceipt;
        var inspection = motion.Plan.InspectReceipt(receipt);
        var snapshot = inspection.Snapshot;
        Assert.Same(snapshot[motion.Plan.Arena.AddressOf(motion.Definition.Input)], inspection.AfterBinding.Left);
        Assert.Same(inspection.After, inspection.AfterBinding.Right);
        var state = Assert.Single(FiniteLaw.Entries(inspection.After));
        Assert.Same(snapshot[motion.Plan.Arena.AddressOf(motion.Definition.States[4])], state);
        motion.Step(); motion.SetUnknown();
        Assert.Same(state, Assert.Single(FiniteLaw.Entries(inspection.After)));
        Assert.Equal(1UL, receipt.Step);
        Assert.NotNull(initial);
    }

    [Fact]
    public void MetricWitnessRetainsSelectedLandmarkProjectionAndExactRead()
    {
        var definition = new MotionDefinition();
        var calibration = new MotionCalibration(definition);
        var observation = calibration.Observe(1, true, 4);
        var evidence = Assert.IsType<Relation>(observation.Witness.Right);
        var selectedBasis = Assert.IsType<Relation>(evidence.Left);
        var selectedSource = Assert.IsType<Relation>(selectedBasis.Left);
        Assert.Same(definition.Landmarks[1], selectedSource.Left);
        var source = Assert.IsType<CarrierLocation>(selectedSource.Right);
        var projection = Assert.IsType<Relation>(selectedBasis.Right);
        var read = Assert.IsType<Relation>(evidence.Right);
        Assert.True(read.TryReadProjection(out var result, out _));
        Assert.True(projection.TryProject(source, result.Carrier, out var repeated));
        Assert.True(repeated.TryReadProjection(out var resultAgain, out var residueAgain));
        Assert.Equal(result.Ticks, resultAgain.Ticks);
        Assert.True(ProjectionResidue.TryRead(residueAgain, out _, out var numerator, out var denominator));
        Assert.Equal(observation.ResidueNumerator, numerator); Assert.Equal(observation.ResidueDenominator, denominator);
    }

    [Theory]
    [InlineData(false, 3, 0, 3)]
    [InlineData(true, 9, 2, -1)]
    public void CoarseMetricReadRetainsExactSourceRemainder(bool reversed, long exact, long coarse, long residue)
    {
        var motion = new PrimitiveMotion();
        var before = motion.Occurrence.State;
        var read = Assert.Single(motion.Observe(reversed, 4).Candidates);
        Assert.Equal(exact, read.Ticks); Assert.Equal(coarse, read.CoarseTicks);
        Assert.Equal(residue, read.ResidueNumerator); Assert.Equal(1, read.ResidueDenominator);
        Assert.Equal(before, motion.Occurrence.State);
        // Residue is in native source coordinates, so a reversed source has
        // negative remainder even when observer progression is positive.
        Assert.Equal(3, reversed ? 12 - coarse * 4 + residue : coarse * 4 + residue);
    }

    [Fact]
    public void EveryClosedSupportReadAgreesWithNativeIncludingCorrelatedAtoms()
    {
        var motion = new PrimitiveMotion(); var definition = motion.Definition;
        for (ulong states = 0; states < 256; states++)
            for (ulong commands = 0; commands < 8; commands++)
            {
                var stateSupport = Support(states, definition.States);
                var commandSupport = Support(commands, [definition.Pulse, definition.ReverseInput, definition.UnadmittedInput]);
                var native = FiniteLaw.Evaluate(definition.Law, stateSupport, commandSupport);
                Assert.True(FiniteLaw.TryReadResult(native, out var expected, out var unresolved, out _));
                var mask = FiniteLaw.Entries(expected).Aggregate(0UL, (bits, item) => bits | motion.Plan.StateMask(item));
                var actual = motion.Plan.Read(CompactSupport.Closed(states), CompactSupport.Closed(commands), out _, out _);
                Assert.Equal(mask, actual.Known); Assert.False(actual.IsOpen);
                Assert.Empty(FiniteLaw.Entries(unresolved));
            }
    }

    private static Relation Support(ulong mask, IReadOnlyList<IElement> participants) => StructuralConstraint.Closed(
        participants.Where((_, index) => (mask & (1UL << index)) != 0).ToArray());
}
