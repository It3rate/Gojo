using Gojo.Core;
using Gojo.Structures;
using PrimitiveLearning;

namespace CompositeEmergence.Tests;

public sealed class HigherControllerTests
{
    [Fact]
    public void CoarseServiceAndGoalTeachActionWithoutReceivingPrivateInputs()
    {
        var learner = Consumer(); TrainConsumer(learner, 128);
        Assert.Equal(4, learner.RowCount); Assert.Equal(128, learner.UpdateCount);
        foreach (var feature in new[] { -1L, 1L })
        foreach (var goal in new[] { -1L, 1L })
            Assert.Equal(feature * goal, Value(learner.Act(P(learner.FeatureAxis, feature), P(learner.GoalAxis, goal), false), learner.ActionAxis));
        Assert.All(learner.InspectEvidence(), row => Assert.Equal(2, row.Context.Length));
    }

    [Fact]
    public void SameGenericLearnerCanAcquireReversedTaskWiring()
    {
        var learner = Consumer(); TrainConsumer(learner, 128, -1);
        foreach (var feature in new[] { -1L, 1L })
        foreach (var goal in new[] { -1L, 1L })
            Assert.Equal(-feature * goal, Value(learner.Act(P(learner.FeatureAxis, feature), P(learner.GoalAxis, goal), false), learner.ActionAxis));
    }

    [Fact]
    public void FlatComparisonLearnsEightPrimitiveContextsUsingIdenticalMarket()
    {
        var a = new Carrier(); var b = new Carrier(); var learner = new ServiceConsumer([a, b], new(), new(), new());
        for (var trial = 0; trial < 256; trial++)
        {
            var av = (trial & 1) == 0 ? -1 : 1; var bv = (trial & 2) == 0 ? -1 : 1; var goal = (trial & 4) == 0 ? -1 : 1;
            var action = Value(learner.Act([P(a, av), P(b, bv)], P(learner.GoalAxis, goal)), learner.ActionAxis);
            learner.Observe(learner.BindFeedback(P(learner.ResourceAxis, action == av * bv * goal ? 8 : -12)));
        }
        Assert.Equal(8, learner.RowCount);
        foreach (var av in new[] { -1L, 1L })
        foreach (var bv in new[] { -1L, 1L })
        foreach (var goal in new[] { -1L, 1L })
            Assert.Equal(av * bv * goal, Value(learner.Act([P(a, av), P(b, bv)], P(learner.GoalAxis, goal), false), learner.ActionAxis));
    }

    [Fact]
    public void UnseenFrozenContextUsesDeclaredInitialActionWithoutAddingLearnedRow()
    {
        var learner = Consumer(); var before = NativeInspection.Signature(learner.LearnedRoot);
        Assert.Equal(-1, Value(learner.Act(P(learner.FeatureAxis, 4), P(learner.GoalAxis, 1), false), learner.ActionAxis));
        Assert.Equal(0, learner.RowCount); Assert.Equal(before, NativeInspection.Signature(learner.LearnedRoot));
    }

    [Fact]
    public void MissingFeatureAndWrongPublicAxisStayOpen()
    {
        var learner = Consumer();
        AssertOpen(learner.Act(NativeGraph.Open(new Placeholder(), new Placeholder()), P(learner.GoalAxis, 1)));
        AssertOpen(learner.Act(P(new Carrier(), 1), P(learner.GoalAxis, 1)));
        AssertOpen(learner.Act(P(learner.FeatureAxis, 17), P(learner.GoalAxis, 1)));
        Assert.Equal(0, learner.RowCount);
    }

    [Fact]
    public void CoarseAmbiguousValueCannotBeTreatedAsAnExactContext()
    {
        var learner = Consumer();
        var capacity = FiniteReadSurface.Capacity(new Carrier(), learner.FeatureAxis, -16, 16, 4);
        var ambiguous = FiniteReadSurface.Publish(new Carrier(), capacity, new CarrierLocation(learner.FeatureAxis, 1), new Placeholder());
        AssertOpen(learner.Act(ambiguous, P(learner.GoalAxis, 1))); Assert.Equal(0, learner.RowCount);
    }

    [Fact]
    public void FeedbackMustBeBoundToActualDecisionAndCannotBeCreditedTwice()
    {
        var learner = Consumer(); var feature = P(learner.FeatureAxis, 1); var goal = P(learner.GoalAxis, 1);
        learner.Act(feature, goal); var physical = P(learner.ResourceAxis, 8); var bound = learner.BindFeedback(physical);
        AssertOpen(learner.Observe(physical)); Assert.Equal(0, learner.UpdateCount);
        learner.Observe(bound); Assert.Equal(1, learner.UpdateCount);
        AssertOpen(learner.Observe(bound)); Assert.Equal(1, learner.UpdateCount);
        learner.Act(feature, goal);
        AssertOpen(learner.Observe(bound)); Assert.Equal(1, learner.UpdateCount);
        AssertOpen(learner.Observe(learner.BindFeedback(physical))); Assert.Equal(1, learner.UpdateCount);
    }

    [Fact]
    public void SamePhysicalEventWrappedAgainIsNotNewEvidence()
    {
        var learner = Consumer(); var eventIdentity = new Carrier();
        learner.Act(P(learner.FeatureAxis, 1), P(learner.GoalAxis, 1));
        learner.Observe(learner.BindFeedback(NativeGraph.Publication(learner.ResourceAxis, 8, eventIdentity)));
        learner.Act(P(learner.FeatureAxis, 1), P(learner.GoalAxis, 1));
        AssertOpen(learner.Observe(learner.BindFeedback(NativeGraph.Publication(learner.ResourceAxis, 8, eventIdentity))));
        Assert.Equal(1, learner.UpdateCount);
    }

    [Fact]
    public void EvaluationAndOperationalResetLeaveLearnedParametersUnchanged()
    {
        var learner = Consumer(); TrainConsumer(learner, 64); var before = NativeInspection.Signature(learner.LearnedRoot);
        learner.Act(P(learner.FeatureAxis, 1), P(learner.GoalAxis, 1), false);
        AssertOpen(learner.Observe(learner.BindFeedback(P(learner.ResourceAxis, 8))));
        learner.ResetOperational();
        Assert.Equal(before, NativeInspection.Signature(learner.LearnedRoot)); Assert.Equal(64, learner.UpdateCount);
        AssertOpen(learner.LastDecision);
    }

    [Fact]
    public void EarlierLearnedSnapshotDoesNotChangeDuringFurtherLearning()
    {
        var learner = Consumer(); TrainConsumer(learner, 64); var frozen = learner.LearnedRoot; var before = NativeInspection.Signature(frozen);
        TrainConsumer(learner, 64, -1);
        Assert.Equal(before, NativeInspection.Signature(frozen));
        Assert.NotEqual(before, NativeInspection.Signature(learner.LearnedRoot));
    }

    [Fact]
    public void ConsumerRetainsAdvertisedValueWithoutSupplierPrivateProvenance()
    {
        var learner = Consumer(); var privateA = new Carrier(); var privateB = new Carrier(); var boundaryEvent = new Carrier();
        var publication = ReadWitness.Create(new Carrier(), boundaryEvent, Relation.Segment(learner.FeatureAxis, -16, 16),
            new CarrierLocation(learner.FeatureAxis, 1), NativeGraph.G(), NativeGraph.G(privateA, privateB));
        learner.Act(publication, P(learner.GoalAxis, 1));
        var seen = new HashSet<object>(ReferenceEqualityComparer.Instance);
        bool Contains(IElement element, Carrier sought)
        {
            if (ReferenceEquals(element, sought)) return true;
            if (element is CarrierLocation point) return Contains(point.Carrier, sought);
            if (!seen.Add(element)) return false;
            if (element is Relation relation) return Contains(relation.Left, sought) || Contains(relation.Right, sought);
            if (element is Carrier carrier) return carrier.AttachedRelations.Any(attachment => Contains(attachment, sought));
            return false;
        }
        Assert.False(Contains(learner.Root, privateA)); seen.Clear(); Assert.False(Contains(learner.Root, privateB));
        seen.Clear(); Assert.True(Contains(learner.Root, boundaryEvent));
    }

    [Fact]
    public void NativeTransportRediscoveryPreservesActionAndAllowsNewFeedback()
    {
        var learner = Consumer(); TrainConsumer(learner, 128);
        var restored = ServiceConsumer.FromRoot(Assert.IsType<Relation>(Assert.Single(GojoNumericFormat.Decode(GojoNumericFormat.Encode(learner.Root)))));
        Assert.Equal(NativeInspection.Signature(learner.Root), NativeInspection.Signature(restored.Root));
        Assert.Equal(learner.InspectEvidence(), restored.InspectEvidence(), EvidenceComparer.Instance);
        var action = Value(restored.Act(P(restored.FeatureAxis, 1), P(restored.GoalAxis, -1), false), restored.ActionAxis);
        Assert.Equal(-1, action);
        restored.Act(P(restored.FeatureAxis, 1), P(restored.GoalAxis, -1));
        restored.Observe(restored.BindFeedback(P(restored.ResourceAxis, 8)));
        Assert.Equal(129, restored.UpdateCount);
    }

    [Fact]
    public void PaidRefreshIsLearnedFromTotalResourceRatherThanAHiddenMode()
    {
        var learner = Refresh(); TrainRefresh(learner);
        Assert.Equal(0, Value(learner.Decide(P(learner.AvailableAxis, 1), P(learner.AgeAxis, 0), P(learner.OutcomeAxis, 1), false), learner.ChoiceAxis));
        Assert.Equal(1, Value(learner.Decide(P(learner.AvailableAxis, 1), P(learner.AgeAxis, 3), P(learner.OutcomeAxis, -1), false), learner.ChoiceAxis));
        Assert.Equal(2, learner.RowCount); Assert.Equal(96, learner.UpdateCount);
    }

    [Fact]
    public void RefreshFreezeAndTransportPreserveLearnedChoices()
    {
        var learner = Refresh(); TrainRefresh(learner); var before = NativeInspection.Signature(learner.LearnedRoot);
        learner.Decide(P(learner.AvailableAxis, 1), P(learner.AgeAxis, 3), P(learner.OutcomeAxis, -1), false);
        AssertOpen(learner.Observe(learner.BindFeedback(P(learner.ResourceAxis, 7)))); Assert.Equal(before, NativeInspection.Signature(learner.LearnedRoot));
        var restored = RefreshLearner.FromRoot(Assert.IsType<Relation>(Assert.Single(GojoNumericFormat.Decode(GojoNumericFormat.Encode(learner.Root)))));
        Assert.Equal(1, Value(restored.Decide(P(restored.AvailableAxis, 1), P(restored.AgeAxis, 3), P(restored.OutcomeAxis, -1), false), restored.ChoiceAxis));
        Assert.Equal(before, NativeInspection.Signature(restored.LearnedRoot));
    }

    [Fact]
    public void BoundedAgingAllowsAChangedResourceLawToOvertakeOldEvidence()
    {
        var learner = Consumer();
        for (var trial = 0; trial < 300; trial++)
        {
            var action = Value(learner.Act(P(learner.FeatureAxis, 1), P(learner.GoalAxis, 1)), learner.ActionAxis);
            learner.Observe(learner.BindFeedback(P(learner.ResourceAxis, action == 1 ? 8 : -12)));
        }
        Assert.Equal(1, Value(learner.Act(P(learner.FeatureAxis, 1), P(learner.GoalAxis, 1), false), learner.ActionAxis));
        for (var trial = 0; trial < 600; trial++)
        {
            var action = Value(learner.Act(P(learner.FeatureAxis, 1), P(learner.GoalAxis, 1)), learner.ActionAxis);
            learner.Observe(learner.BindFeedback(P(learner.ResourceAxis, action == -1 ? 8 : -12)));
        }
        Assert.Equal(-1, Value(learner.Act(P(learner.FeatureAxis, 1), P(learner.GoalAxis, 1), false), learner.ActionAxis));
        Assert.True(learner.AgingEvents > 0); Assert.All(learner.InspectEvidence(), row => Assert.InRange(row.Observations, 1, NativeGraph.MaximumLedgerCount));
    }

    [Fact]
    public void AuditRetentionIsBoundedAndDoesNotChainPreviousWholeRoots()
    {
        var learner = Consumer(); TrainConsumer(learner, 128);
        var fields = NativeGraph.Parts(learner.Root, 13); var operational = NativeGraph.Parts((Relation)fields[12], 3);
        Assert.Equal(NativeGraph.ReceiptLimit, NativeGraph.Parts((Relation)operational[2]).Length);
        Assert.InRange(NativeInspection.Count(learner.Root).Relations, 1, 5000);
    }

    private static ServiceConsumer Consumer() => new(new Carrier(), new Carrier(), new Carrier(), new Carrier());
    private static RefreshLearner Refresh() => new(new Carrier(), new Carrier(), new Carrier(), new Carrier());
    private static Relation P(Carrier axis, long ticks) => NativeGraph.Publication(axis, ticks, new Carrier());
    private static long Value(Relation publication, Carrier axis) { Assert.True(NativeGraph.TryValue(publication, axis, out var point)); return point.Ticks; }
    private static void AssertOpen(Relation publication) { Assert.True(ReadWitness.TryRead(publication, out _, out _, out var result, out var residue, out _)); Assert.IsType<Placeholder>(result); Assert.IsType<Relation>(residue); }
    private static void TrainConsumer(ServiceConsumer learner, int count, int wiring = 1)
    {
        for (var trial = 0; trial < count; trial++)
        {
            var feature = (trial & 1) == 0 ? -1 : 1; var goal = (trial & 2) == 0 ? -1 : 1;
            var action = Value(learner.Act(P(learner.FeatureAxis, feature), P(learner.GoalAxis, goal)), learner.ActionAxis);
            learner.Observe(learner.BindFeedback(P(learner.ResourceAxis, action == wiring * feature * goal ? 8 : -12)));
        }
    }
    private static void TrainRefresh(RefreshLearner learner)
    {
        for (var trial = 0; trial < 96; trial++)
        {
            var old = trial % 2 == 1;
            var choice = Value(learner.Decide(P(learner.AvailableAxis, 1), P(learner.AgeAxis, old ? 3 : 0), P(learner.OutcomeAxis, old ? -1 : 1)), learner.ChoiceAxis);
            var resource = choice == 1 ? 7 : old ? -12 : 8;
            learner.Observe(learner.BindFeedback(P(learner.ResourceAxis, resource)));
        }
    }
    private sealed class EvidenceComparer : IEqualityComparer<MarketEvidence>
    {
        public static readonly EvidenceComparer Instance = new();
        public bool Equals(MarketEvidence? left, MarketEvidence? right) => left is not null && right is not null && left.Context.SequenceEqual(right.Context) &&
            left.Choice == right.Choice && left.Observations == right.Observations && left.PositiveResource == right.PositiveResource && left.NegativeResource == right.NegativeResource && left.Mean256 == right.Mean256;
        public int GetHashCode(MarketEvidence obj) => obj.Choice.GetHashCode();
    }
}
