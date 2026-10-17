using Gojo.Core;
using PrimitiveLearning;

namespace CompositeEmergence.Tests;

public sealed class ArchiveVerifierTests
{
    [Fact]
    public void ConnectedPipelineArchivePreservesBehaviorAndRejectsSeparatedAxes()
    {
        var service = TrainService();
        var consumer = new ServiceConsumer(service.FeatureAxis, new Carrier(), new Carrier(), new Carrier());
        var manager = new RefreshLearner(new Carrier(), new Carrier(), new Carrier(), consumer.ResourceAxis);
        var memory = new FeatureMemory(service.FeatureAxis, manager.AvailableAxis, manager.AgeAxis, manager.OutcomeAxis);
        memory.Retain(service.PublishFeature()); memory.Advance(); memory.RememberOutcome(P(consumer.ResourceAxis, -12));
        var root = NativeGraph.G(service.Root, consumer.Root, manager.Root, memory.Root); var restored = Roundtrip(root);
        var before = NativeInspection.Signature(root);
        Assert.True(ArchiveVerifier.VerifyPipeline(root, restored)); Assert.Equal(before, NativeInspection.Signature(root));
        var fields = NativeGraph.Parts(restored, 4);
        // Independent transport creates a new manager's physical axes even
        // though its candidate counts and isolated decisions still match.
        fields[2] = Roundtrip((Relation)fields[2]);
        Assert.False(ArchiveVerifier.VerifyPipeline(root, NativeGraph.G(fields)));
    }

    [Fact]
    public void ServiceArchiveChecksFeaturesAndPredictionsWithoutChangingOriginalWorlds()
    {
        var source = TrainService(); var restored = RelationService.FromRoot(Roundtrip(source.Root));
        var before = NativeInspection.Signature(source.Root); var restoredBefore = NativeInspection.Signature(restored.Root);
        Assert.True(ArchiveVerifier.VerifyService(source, restored));
        Assert.Equal(before, NativeInspection.Signature(source.Root)); Assert.Equal(restoredBefore, NativeInspection.Signature(restored.Root));
        Assert.False(ArchiveVerifier.VerifyService(source, new RelationService()));
    }

    [Fact]
    public void ConsumerArchiveChecksAllObservedCoarseContextsRatherThanOnlyCounts()
    {
        var consumer = new ServiceConsumer(new Carrier(), new Carrier(), new Carrier(), new Carrier());
        for (var iteration = 0; iteration < 120; iteration++)
        {
            var feature = iteration % 3 - 1; var goal = (iteration / 3 & 1) == 0 ? -1 : 1;
            var correct = feature == 0 ? goal : feature * goal;
            var action = Value(consumer.Act(P(consumer.FeatureAxis, feature), P(consumer.GoalAxis, goal)), consumer.ActionAxis);
            consumer.Observe(consumer.BindFeedback(P(consumer.ResourceAxis, action == correct ? 8 : -12)));
        }
        var restored = ServiceConsumer.FromRoot(Roundtrip(consumer.Root)); var before = NativeInspection.Signature(consumer.Root);
        Assert.True(ArchiveVerifier.VerifyConsumer(consumer, restored)); Assert.Equal(before, NativeInspection.Signature(consumer.Root));
        var rows = NativeGraph.Parts((Relation)NativeGraph.Parts(restored.Root, 13)[7]).Cast<Relation>();
        var row = rows.Single(row =>
        {
            var context = NativeGraph.Parts((Relation)NativeGraph.Parts(row, 5)[0], 2);
            NativeGraph.TryPoint(context[0], out var feature); NativeGraph.TryPoint(context[1], out var goal);
            return feature.Ticks == 0 && goal.Ticks == 1;
        });
        foreach (var candidate in NativeGraph.Parts((Relation)NativeGraph.Parts(row, 5)[1], 2).Cast<Relation>())
        {
            var parts = NativeGraph.Parts(candidate, 2); NativeGraph.TryPoint(parts[0], out var action);
            var ledger = NativeGraph.Parts((Relation)parts[1], 3).Cast<Carrier>().ToArray();
            ledger[0].Set(1); ledger[1].Set(action.Ticks == -1 ? 20 : 0); ledger[2].Set(0);
        }
        Assert.Equal(consumer.RowCount, restored.RowCount);
        Assert.False(ArchiveVerifier.VerifyConsumer(consumer, restored));
    }

    [Fact]
    public void FlatConsumerArchiveUsesEachRestoredPrimitiveAxis()
    {
        var consumer = new ServiceConsumer([new Carrier(), new Carrier()], new Carrier(), new Carrier(), new Carrier());
        for (var trial = 0; trial < 128; trial++)
        {
            var a = (trial & 1) == 0 ? -1 : 1; var b = (trial & 2) == 0 ? -1 : 1; var goal = (trial & 4) == 0 ? -1 : 1;
            var action = Value(consumer.Act([P(consumer.FeatureAxes[0], a), P(consumer.FeatureAxes[1], b)], P(consumer.GoalAxis, goal)), consumer.ActionAxis);
            consumer.Observe(consumer.BindFeedback(P(consumer.ResourceAxis, action == a * b * goal ? 8 : -12)));
        }
        var restored = ServiceConsumer.FromRoot(Roundtrip(consumer.Root));
        Assert.NotSame(consumer.FeatureAxes[0], restored.FeatureAxes[0]); Assert.True(ArchiveVerifier.VerifyConsumer(consumer, restored));
        Assert.False(ArchiveVerifier.VerifyConsumer(consumer, new ServiceConsumer(new Carrier(), new Carrier(), new Carrier(), new Carrier())));
    }

    [Fact]
    public void ManagerArchiveChecksAllTwentyFourLegalContextsAndPreservesOperationalState()
    {
        var manager = new RefreshLearner(new Carrier(), new Carrier(), new Carrier(), new Carrier());
        for (var iteration = 0; iteration < 16; iteration++)
        foreach (var available in new[] { 0, 1 })
        foreach (var age in new[] { 0, 1, 2, 3 })
        foreach (var previous in new[] { -1, 0, 1 })
        {
            var query = Value(manager.Decide(P(manager.AvailableAxis, available), P(manager.AgeAxis, age), P(manager.OutcomeAxis, previous)), manager.QueryAxis);
            var target = available == 0 || age == 3 || previous == -1 ? 1 : 0;
            manager.Observe(manager.BindFeedback(P(manager.ResourceAxis, query == target ? 7 : -12)));
        }
        var restored = RefreshLearner.FromRoot(Roundtrip(manager.Root));
        var before = NativeInspection.Signature(manager.Root); var restoredBefore = NativeInspection.Signature(restored.Root);
        Assert.True(ArchiveVerifier.VerifyManager(manager, restored));
        Assert.Equal(before, NativeInspection.Signature(manager.Root)); Assert.Equal(restoredBefore, NativeInspection.Signature(restored.Root));
        Assert.False(ArchiveVerifier.VerifyManager(manager, new RefreshLearner(new Carrier(), new Carrier(), new Carrier(), new Carrier())));
    }

    private static RelationService TrainService()
    {
        var service = new RelationService();
        for (var trial = 0; trial < 160; trial++)
        {
            var a = (trial & 1) == 0 ? -1 : 1; var rule = (trial & 2) == 0 ? -1 : 1; var action = (trial & 4) == 0 ? -1 : 1;
            service.AcceptA(P(service.InputAAxis, a, trial * 4)); service.AcceptB(P(service.InputBAxis, a * rule, trial * 4 + 1));
            var command = P(service.ActionAxis, action, trial * 4 + 2);
            service.Learn(command, NativeGraph.BindFeedback(command, P(service.OutcomeAxis, action * rule, trial * 4 + 3), service.OutcomeAxis));
        }
        return service;
    }
    private static Relation Roundtrip(Relation root) => (Relation)GojoNumericFormat.Decode(GojoNumericFormat.Encode(root))[0];
    private static Relation P(Carrier axis, long ticks, long time = 0) => NativeGraph.Publication(axis, ticks, new Carrier(), time);
    private static long Value(Relation publication, Carrier axis) { Assert.True(NativeGraph.TryValue(publication, axis, out var value)); return value.Ticks; }
}
