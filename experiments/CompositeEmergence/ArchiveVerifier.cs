using Gojo.Core;
using PrimitiveLearning;

namespace CompositeEmergence;

/// <summary>Behavioral transport checks using independent public fixtures.
/// Probes run on native graph clones, leaving both supplied worlds and their
/// retained parameters untouched. Matching graph size is not a behavior check.</summary>
public static class ArchiveVerifier
{
    /// <summary>Checks a connected deployment, including operational memory.
    /// The four roots must be transported together so their exposed axes remain
    /// the same physical sites across service/consumer/refresh boundaries.</summary>
    public static bool VerifyPipeline(Relation originalRoot, Relation restoredRoot)
    {
        var before = (NativeInspection.Signature(originalRoot), NativeInspection.Signature(restoredRoot));
        var left = Pipeline(Roundtrip(originalRoot)); var right = Pipeline(Roundtrip(restoredRoot));
        if (!SharedAxes(left) || !SharedAxes(right)) return false;
        var leftLearned = PipelineLearned(left); var rightLearned = PipelineLearned(right);
        var learned = (NativeInspection.Signature(leftLearned), NativeInspection.Signature(rightLearned));
        var same = CompareMemory(left, right);
        long time = checked(Math.Max(LatestInputTime(left.Service), LatestInputTime(right.Service)) + 1);
        var fixture = 0;
        foreach (var pass in Enumerable.Range(0, 3))
        foreach (var a in new[] { -1L, 1L })
        foreach (var b in new[] { -1L, 1L })
        foreach (var goal in new[] { -1L, 1L })
        {
            left.Service.AcceptA(P(left.Service.InputAAxis, a, time)); right.Service.AcceptA(P(right.Service.InputAAxis, a, time));
            left.Service.AcceptB(P(left.Service.InputBAxis, b, time + 1)); right.Service.AcceptB(P(right.Service.InputBAxis, b, time + 1));
            var leftFeature = left.Service.PublishFeature(); var rightFeature = right.Service.PublishFeature();
            same &= Same(leftFeature, left.Service.FeatureAxis, rightFeature, right.Service.FeatureAxis);
            left.Memory.Retain(leftFeature); right.Memory.Retain(rightFeature);
            for (var age = 0; age < fixture % 10; age++) { left.Memory.Advance(); right.Memory.Advance(); }
            var observedResource = fixture % 3 == 0 ? -12 : fixture % 3 == 1 ? 0 : 8;
            left.Memory.RememberOutcome(P(left.Consumer.ResourceAxis, observedResource, time + 2));
            right.Memory.RememberOutcome(P(right.Consumer.ResourceAxis, observedResource, time + 2));
            same &= CompareMemory(left, right);
            same &= Same(left.Manager.Decide(left.Memory.Available(), left.Memory.Age(), left.Memory.PreviousOutcome(), false), left.Manager.QueryAxis,
                right.Manager.Decide(right.Memory.Available(), right.Memory.Age(), right.Memory.PreviousOutcome(), false), right.Manager.QueryAxis);
            same &= Same(left.Consumer.Act(left.Memory.ReadFeature(), P(left.Consumer.GoalAxis, goal, time + 3), false), left.Consumer.ActionAxis,
                right.Consumer.Act(right.Memory.ReadFeature(), P(right.Consumer.GoalAxis, goal, time + 3), false), right.Consumer.ActionAxis);
            fixture++; time += 16;
        }
        return same && LearnedUnchanged(PipelineLearned(left), PipelineLearned(right), learned) &&
            NativeInspection.Signature(originalRoot) == before.Item1 && NativeInspection.Signature(restoredRoot) == before.Item2;
    }

    public static bool VerifyService(RelationService original, RelationService restored)
    {
        var originals = (NativeInspection.Signature(original.Root), NativeInspection.Signature(restored.Root));
        var left = RelationService.FromRoot(Roundtrip(original.Root)); var right = RelationService.FromRoot(Roundtrip(restored.Root));
        var learned = (NativeInspection.Signature(left.LearnedRoot), NativeInspection.Signature(right.LearnedRoot));
        var time = checked(Math.Max(LatestInputTime(left), LatestInputTime(right)) + 1);
        var same = Same(left.PublishFeature(), left.FeatureAxis, right.PublishFeature(), right.FeatureAxis);
        foreach (var action in new[] { -1L, 1L })
            same &= Same(left.PredictOutcome(P(left.ActionAxis, action, time)), left.OutcomeAxis,
                right.PredictOutcome(P(right.ActionAxis, action, time)), right.OutcomeAxis);
        // Two complete passes also exercise previous-sample candidates and
        // finite current/previous port combinations after their warm-up.
        foreach (var pass in Enumerable.Range(0, 2))
        foreach (var a in new[] { -1L, 1L })
        foreach (var b in new[] { -1L, 1L })
        {
            left.AcceptA(P(left.InputAAxis, a, time)); right.AcceptA(P(right.InputAAxis, a, time));
            left.AcceptB(P(left.InputBAxis, b, time + 1)); right.AcceptB(P(right.InputBAxis, b, time + 1));
            same &= Same(left.PublishFeature(), left.FeatureAxis, right.PublishFeature(), right.FeatureAxis);
            foreach (var action in new[] { -1L, 1L })
                same &= Same(left.PredictOutcome(P(left.ActionAxis, action, time + 2)), left.OutcomeAxis,
                    right.PredictOutcome(P(right.ActionAxis, action, time + 2)), right.OutcomeAxis);
            time += 4;
        }
        return same && LearnedUnchanged(left.LearnedRoot, right.LearnedRoot, learned) &&
            NativeInspection.Signature(original.Root) == originals.Item1 && NativeInspection.Signature(restored.Root) == originals.Item2;
    }

    public static bool VerifyConsumer(ServiceConsumer original, ServiceConsumer restored)
    {
        var originals = (NativeInspection.Signature(original.Root), NativeInspection.Signature(restored.Root));
        var left = ServiceConsumer.FromRoot(Roundtrip(original.Root)); var right = ServiceConsumer.FromRoot(Roundtrip(restored.Root));
        if (left.FeatureAxes.Count != right.FeatureAxes.Count) return false;
        var learned = (NativeInspection.Signature(left.LearnedRoot), NativeInspection.Signature(right.LearnedRoot));
        var contexts = new List<long[]>();
        // Primitive combinations plus all encountered coarse service values
        // ensure that a sum/other retained feature is checked as well as +/-1.
        contexts.AddRange(BinaryContexts(left.FeatureAxes.Count + 1));
        contexts.AddRange(left.InspectEvidence().Select(row => row.Context));
        contexts.AddRange(right.InspectEvidence().Select(row => row.Context));
        var same = Same(left.LastDecision, left.ActionAxis, right.LastDecision, right.ActionAxis);
        foreach (var context in contexts.Distinct(LongSequenceComparer.Instance))
        {
            if (context.Length != left.FeatureAxes.Count + 1) return false;
            var leftFeatures = left.FeatureAxes.Select((axis, index) => P(axis, context[index], 1)).ToArray();
            var rightFeatures = right.FeatureAxes.Select((axis, index) => P(axis, context[index], 1)).ToArray();
            same &= Same(left.Act(leftFeatures, P(left.GoalAxis, context[^1], 1), false), left.ActionAxis,
                right.Act(rightFeatures, P(right.GoalAxis, context[^1], 1), false), right.ActionAxis);
        }
        return same && LearnedUnchanged(left.LearnedRoot, right.LearnedRoot, learned) &&
            NativeInspection.Signature(original.Root) == originals.Item1 && NativeInspection.Signature(restored.Root) == originals.Item2;
    }

    public static bool VerifyManager(RefreshLearner original, RefreshLearner restored)
    {
        var originals = (NativeInspection.Signature(original.Root), NativeInspection.Signature(restored.Root));
        var left = RefreshLearner.FromRoot(Roundtrip(original.Root)); var right = RefreshLearner.FromRoot(Roundtrip(restored.Root));
        var learned = (NativeInspection.Signature(left.LearnedRoot), NativeInspection.Signature(right.LearnedRoot));
        var same = Same(left.LastDecision, left.QueryAxis, right.LastDecision, right.QueryAxis);
        foreach (var available in new[] { 0L, 1L })
        foreach (var age in new[] { 0L, 1L, 2L, 3L })
        foreach (var outcome in new[] { -1L, 0L, 1L })
            same &= Same(left.Decide(P(left.AvailableAxis, available, 1), P(left.AgeAxis, age, 1), P(left.OutcomeAxis, outcome, 1), false), left.QueryAxis,
                right.Decide(P(right.AvailableAxis, available, 1), P(right.AgeAxis, age, 1), P(right.OutcomeAxis, outcome, 1), false), right.QueryAxis);
        return same && LearnedUnchanged(left.LearnedRoot, right.LearnedRoot, learned) &&
            NativeInspection.Signature(original.Root) == originals.Item1 && NativeInspection.Signature(restored.Root) == originals.Item2;
    }

    private static Relation P(Carrier axis, long ticks, long time) => NativeGraph.Publication(axis, ticks, new Carrier(), time);
    private static (RelationService Service, ServiceConsumer Consumer, RefreshLearner Manager, FeatureMemory Memory) Pipeline(Relation root)
    {
        var fields = NativeGraph.Parts(root, 4);
        return (RelationService.FromRoot((Relation)fields[0]), ServiceConsumer.FromRoot((Relation)fields[1]),
            RefreshLearner.FromRoot((Relation)fields[2]), FeatureMemory.FromRoot((Relation)fields[3]));
    }
    private static bool SharedAxes((RelationService Service, ServiceConsumer Consumer, RefreshLearner Manager, FeatureMemory Memory) pipeline) =>
        pipeline.Consumer.FeatureAxes.Count == 1 && pipeline.Consumer.FeatureAxis.SameIdentityAs(pipeline.Service.FeatureAxis) &&
        pipeline.Memory.FeatureAxis.SameIdentityAs(pipeline.Service.FeatureAxis) &&
        pipeline.Memory.AvailableAxis.SameIdentityAs(pipeline.Manager.AvailableAxis) && pipeline.Memory.AgeAxis.SameIdentityAs(pipeline.Manager.AgeAxis) &&
        pipeline.Memory.OutcomeAxis.SameIdentityAs(pipeline.Manager.OutcomeAxis) && pipeline.Manager.ResourceAxis.SameIdentityAs(pipeline.Consumer.ResourceAxis);
    private static Relation PipelineLearned((RelationService Service, ServiceConsumer Consumer, RefreshLearner Manager, FeatureMemory Memory) pipeline) =>
        NativeGraph.G(pipeline.Service.LearnedRoot, pipeline.Consumer.LearnedRoot, pipeline.Manager.LearnedRoot);
    private static bool CompareMemory((RelationService Service, ServiceConsumer Consumer, RefreshLearner Manager, FeatureMemory Memory) left,
        (RelationService Service, ServiceConsumer Consumer, RefreshLearner Manager, FeatureMemory Memory) right) =>
        Same(left.Memory.ReadFeature(), left.Service.FeatureAxis, right.Memory.ReadFeature(), right.Service.FeatureAxis) &&
        Same(left.Memory.Available(), left.Manager.AvailableAxis, right.Memory.Available(), right.Manager.AvailableAxis) &&
        Same(left.Memory.Age(), left.Manager.AgeAxis, right.Memory.Age(), right.Manager.AgeAxis) &&
        Same(left.Memory.PreviousOutcome(), left.Manager.OutcomeAxis, right.Memory.PreviousOutcome(), right.Manager.OutcomeAxis);
    private static long LatestInputTime(RelationService service)
    {
        var fields = NativeGraph.Parts(service.CaptureOperational(), 7);
        var times = fields.Take(4).OfType<Relation>().Select(publication => NativeGraph.TryTimestamp(publication, out var time) ? time : 0L);
        return times.Max();
    }
    private static Relation Roundtrip(Relation root) => (Relation)GojoNumericFormat.Decode(GojoNumericFormat.Encode(root))[0];
    private static bool Same(Relation left, Carrier leftAxis, Relation right, Carrier rightAxis)
    {
        var leftKnown = NativeGraph.TryValue(left, leftAxis, out var leftValue);
        var rightKnown = NativeGraph.TryValue(right, rightAxis, out var rightValue);
        return leftKnown == rightKnown && (!leftKnown || leftValue.Ticks == rightValue.Ticks);
    }
    private static bool LearnedUnchanged(Relation left, Relation right, (string Left, string Right) before) =>
        NativeInspection.Signature(left) == before.Left && NativeInspection.Signature(right) == before.Right;
    private static IEnumerable<long[]> BinaryContexts(int dimensions)
    {
        if (dimensions > 8) throw new ArgumentException("The bounded experiment fixture supports at most eight binary ports.");
        for (var mask = 0; mask < 1 << dimensions; mask++)
            yield return Enumerable.Range(0, dimensions).Select(index => (mask & 1 << index) == 0 ? -1L : 1L).ToArray();
    }
    private sealed class LongSequenceComparer : IEqualityComparer<long[]>
    {
        internal static readonly LongSequenceComparer Instance = new();
        public bool Equals(long[]? left, long[]? right) => left is not null && right is not null && left.SequenceEqual(right);
        public int GetHashCode(long[] value) { var hash = new HashCode(); foreach (var item in value) hash.Add(item); return hash.ToHashCode(); }
    }
}
