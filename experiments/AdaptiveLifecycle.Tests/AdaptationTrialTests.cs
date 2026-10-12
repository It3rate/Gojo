using CompositeEmergence;
using Gojo.Core;
using Gojo.Structures;
using PrimitiveLearning;
using System.Text.Json;

namespace AdaptiveLifecycle.Tests;

public sealed class AdaptationTrialTests
{
    [Fact]
    public void FrozenUnseenUnavailableContextCanRepeatSkipWithoutAnyNewLearningOrAging()
    {
        var manager = new RefreshLearner(new(), new(), new(), new());
        var featureAxis = new Carrier();
        var memory = new FeatureMemory(featureAxis, manager.AvailableAxis, manager.AgeAxis, manager.OutcomeAxis);
        memory.Retain(WorldPublication.Open(featureAxis, new Carrier()));
        var before = NativeInspection.Signature(manager.LearnedRoot);
        for (var step = 0; step < 20; step++)
        {
            var choice = manager.Decide(memory.Available(), memory.Age(), memory.PreviousOutcome(), false);
            Assert.True(WorldPublication.TryValue(choice, manager.QueryAxis, out var query)); Assert.Equal(0, query);
            Assert.False(WorldPublication.TryValue(memory.ReadFeature(), featureAxis, out _));
            var physical = WorldPublication.Value(manager.ResourceAxis, 0, new Carrier(), -1024, 1024);
            manager.Observe(manager.BindFeedback(physical), false); memory.RememberOutcome(physical); memory.Advance();
        }
        Assert.Equal(before, NativeInspection.Signature(manager.LearnedRoot));
        Assert.Equal(0, manager.RowCount); Assert.Equal(0, manager.UpdateCount); Assert.Equal(0, manager.AgingEvents);
        Assert.Equal(20, memory.ActualAge);
    }

    [Fact]
    public void OnlineUnavailableContextCreatesRowsAndExploresFromBoundPhysicalFeedback()
    {
        var manager = new RefreshLearner(new(), new(), new(), new());
        var available = P(manager.AvailableAxis, 0); var age = P(manager.AgeAxis, 3); var prior = P(manager.OutcomeAxis, 0);
        var choices = new List<long>();
        for (var step = 0; step < 160; step++)
        {
            var decision = manager.Decide(available, age, prior, true);
            Assert.True(WorldPublication.TryValue(decision, manager.QueryAxis, out var action)); choices.Add(action);
            // A public resource publication is bound to the actual native
            // decision. No private apparatus rule or correct-action map enters.
            var physical = P(manager.ResourceAxis, action == 1 ? 6 : 0);
            manager.Observe(manager.BindFeedback(physical), true);
        }
        Assert.Contains(0, choices); Assert.Contains(1, choices);
        Assert.Equal(1, manager.RowCount); Assert.Equal(160, manager.UpdateCount); Assert.True(manager.AgingEvents > 0);
        Assert.Equal(1, Value(manager.Decide(available, age, prior, false), manager.QueryAxis));
        Assert.All(manager.InspectEvidence(), evidence => Assert.Equal(new long[] { 0, 3, 0 }, evidence.Context));
    }

    [Fact]
    public void OnlineReadWithoutObservedFeedbackDoesNotAdvanceSupportOrEvidenceAging()
    {
        var manager = new RefreshLearner(new(), new(), new(), new());
        for (var step = 0; step < 100; step++) manager.Decide(P(manager.AvailableAxis, 0), P(manager.AgeAxis, 3), P(manager.OutcomeAxis, 0), true);
        Assert.Equal(1, manager.RowCount); Assert.Equal(0, manager.UpdateCount); Assert.Equal(0, manager.AgingEvents);
        Assert.All(manager.InspectEvidence(), evidence => Assert.Equal(0, evidence.Observations));
    }

    [Fact]
    public void SameTapeComparatorsPreserveLowerModelsResourcesAndMissingValues()
    {
        var tape = Enumerable.Range(0, 64).Select(index => new ApparatusTrial(index, index < 32 ? 1 : -1,
            index % 2 == 0 ? 1 : -1, index % 3 == 0 ? -1 : 1, 2, 1, index == 0, false)).ToArray();
        var settings = new AdaptationSettings(320, 96, 128, tape.Length, .10, .04, 2);
        var run = AdaptationTrial.Run(0, settings, tape);
        Assert.Equal(6, run.Rows.Count); Assert.Equal(6 * tape.Length, run.Trace.Count); Assert.Equal(6, run.NativeRoots.Count);
        foreach (var row in run.Rows)
        {
            Assert.Equal(row.LowerSignatureBefore, row.LowerSignatureAfter);
            Assert.Equal(row.GrossResource - row.ProbeDebits, row.NetResource);
            Assert.Equal(2L * row.Queries, row.ProbeDebits);
            Assert.Equal(8L * row.Correct - 12L * (row.Answered - row.Correct), row.GrossResource);
            var isOnline = row.Arm.EndsWith("/online", StringComparison.Ordinal);
            Assert.Equal(isOnline ? row.ManagerUpdatesBefore + tape.Length : row.ManagerUpdatesBefore, row.ManagerUpdatesAfter);
            if (!isOnline) Assert.Equal(row.ManagerSignatureBefore, row.ManagerSignatureAfter);
            var stream = run.Trace.Where(step => step.Arm == row.Arm).ToArray();
            Assert.Equal(tape.Select(trial => trial.Rule), stream.Select(step => step.PrivateRule));
            Assert.All(stream.Where(step => !step.Probe), step => { Assert.Null(step.ProbeMissingA); Assert.Null(step.ProbeMissingB); });
            Assert.All(stream.Where(step => step.Hold), step => { Assert.Null(step.Feature); Assert.False(step.AvailableAfter); });
        }
        var always = run.Trace.Where(step => step.Arm == "always-probe").ToArray();
        Assert.True(always[0].Probe); Assert.True(always[0].Hold); Assert.Null(always[0].Feature);
        Assert.True(always[1].AvailableAfter); Assert.False(always[1].Hold);
        Assert.False(run.Rows.Single(row => row.Arm == "always-probe").MeanProbeRecovery is null);
        var frozen = run.Rows.Single(row => row.Arm == "clean-trained/frozen");
        var online = run.Rows.Single(row => row.Arm == "clean-trained/online");
        Assert.Equal(frozen.ManagerSignatureBefore, online.ManagerSignatureBefore);
        Assert.Equal(frozen.LowerSignatureBefore, online.LowerSignatureBefore);
        Assert.DoesNotContain("NativeRoots", JsonSerializer.Serialize(run), StringComparison.Ordinal);
    }

    [Fact]
    public void TrialArchivesRoundTripActualAliasConnectedMarketsAndNextFrozenDecision()
    {
        var settings = new AdaptationSettings(320, 96, 64, 16);
        var run = AdaptationTrial.Run(0, settings);
        foreach (var root in run.NativeRoots.Values)
        {
            var restored = (Relation)GojoNumericFormat.Decode(GojoNumericFormat.Encode(root))[0];
            Assert.Equal(NativeInspection.Signature(root), NativeInspection.Signature(restored));
            var before = NativeGraph.Parts(root, 5); var after = NativeGraph.Parts(restored, 5);
            var source = RelationService.FromRoot((Relation)after[0]); var consumer = ServiceConsumer.FromRoot((Relation)after[1]);
            var manager = RefreshLearner.FromRoot((Relation)after[2]); var memory = FeatureMemory.FromRoot((Relation)after[3]);
            Assert.True(source.FeatureAxis.SameIdentityAs(consumer.FeatureAxis));
            Assert.True(source.FeatureAxis.SameIdentityAs(memory.FeatureAxis));
            Assert.True(consumer.ResourceAxis.SameIdentityAs(manager.ResourceAxis));
            Assert.True(manager.AvailableAxis.SameIdentityAs(memory.AvailableAxis));
            var originalManager = RefreshLearner.FromRoot((Relation)before[2]);
            var originalMemory = FeatureMemory.FromRoot((Relation)before[3]);
            Assert.Equal(Value(originalManager.Decide(originalMemory.Available(), originalMemory.Age(), originalMemory.PreviousOutcome(), false), originalManager.QueryAxis),
                Value(manager.Decide(memory.Available(), memory.Age(), memory.PreviousOutcome(), false), manager.QueryAxis));
        }
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(-.1)]
    [InlineData(1.1)]
    public void InvalidProbabilityDoesNotStartTrial(double probability) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => AdaptationTrial.Run(0, new AdaptationSettings(MissingProbability: probability)));

    private static Relation P(Carrier axis, long value) => WorldPublication.Value(axis, value, new Carrier(), -1024, 1024);
    private static long Value(Relation publication, Carrier axis)
    {
        Assert.True(WorldPublication.TryValue(publication, axis, out var value)); return value;
    }
}
