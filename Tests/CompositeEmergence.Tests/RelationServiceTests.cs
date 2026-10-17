using Gojo.Core;
using Gojo.Structures;
using PrimitiveLearning;

namespace CompositeEmergence.Tests;

public sealed class RelationServiceTests
{
    [Fact]
    public void StartsWithPrimitiveSourcesAndExpandsOnlyAfterFailedPredictions()
    {
        var service = new RelationService(candidateOrderSeed: 12); Assert.Equal(4, service.CandidateCount); Assert.Equal(0, service.Expansions);
        Train(service, 63); Assert.Equal(4, service.CandidateCount); Assert.Equal(0, service.Expansions);
        Train(service, 257, 63); Assert.Equal(22, service.CandidateCount); Assert.Equal(1, service.Expansions);
        Assert.Contains("A", service.SelectedDescription); Assert.Contains("B", service.SelectedDescription);
        Assert.True(Accuracy(service) > .98); Assert.InRange(service.CandidateDiagnostics.Single(x => x.Selected).PredictionRows, 4, 6);
    }
    [Fact]
    public void NoExpansionAndNoTemporalRetentionCannotPredictFreshJointRule()
    {
        var primitive = new RelationService(expansionEnabled: false); var noMemory = new RelationService(memoryEnabled: false);
        Train(primitive, 320); Train(noMemory, 320);
        Assert.Equal(4, primitive.CandidateCount); Assert.InRange(Accuracy(primitive), 0, .75);
        Assert.InRange(Accuracy(noMemory), 0, .75);
        Assert.False(NativeGraph.TryValue(noMemory.PublishFeature(), noMemory.FeatureAxis, out _) && noMemory.SelectedDescription.Contains("A"));
    }
    [Fact]
    public void MissingInputsStayOpenAndDoNotBecomeConcreteZero()
    {
        var service = new RelationService(); Train(service, 320);
        service.AcceptA(NativeGraph.Open(new Carrier(), service.InputAAxis)); service.AcceptB(P(service.InputBAxis, 1, 9001));
        Assert.False(NativeGraph.TryValue(service.PublishFeature(), service.FeatureAxis, out _));
        service.AcceptA(P(service.InputAAxis, 1, 9002)); service.AcceptB(NativeGraph.Open(new Carrier(), service.InputBAxis));
        Assert.False(NativeGraph.TryValue(service.PublishFeature(), service.FeatureAxis, out _));
    }
    [Fact]
    public void PublishedFeaturePhysicallyWithholdsRawInputAncestry()
    {
        var service = new RelationService(); Train(service, 320); Sensors(service, 1, -1, 9000);
        var publication = service.PublishFeature(); var exposed = Reach(publication);
        Assert.DoesNotContain(service.InputAAxis, exposed); Assert.DoesNotContain(service.InputBAxis, exposed);
        Assert.Contains(service.FeatureAxis, exposed); Assert.True(NativeGraph.TryValue(publication, service.FeatureAxis, out _));
        Assert.Contains(service.InputAAxis, Reach(service.Root)); Assert.Contains(service.InputBAxis, Reach(service.Root));
    }
    [Fact]
    public void FeedbackMustBeBoundAndDuplicatesDoNotProvideAdditionalSupport()
    {
        var service = new RelationService(); Sensors(service, 1, -1, 0); var action = P(service.ActionAxis, 1, 2); var physical = P(service.OutcomeAxis, -1, 3);
        service.Learn(action, physical); Assert.Equal(0, service.TrainingTrials);
        var bound = NativeGraph.BindFeedback(action, physical, service.OutcomeAxis); service.Learn(action, bound); Assert.Equal(1, service.TrainingTrials);
        service.Learn(action, bound); Assert.Equal(1, service.TrainingTrials);
        var otherAction = P(service.ActionAxis, -1, 4); var rebound = NativeGraph.BindFeedback(otherAction, physical, service.OutcomeAxis);
        service.Learn(otherAction, rebound); Assert.Equal(1, service.TrainingTrials);
    }
    [Fact]
    public void FrozenEvaluationChangesOperationalReadsWithoutChangingParameters()
    {
        var service = new RelationService(); Train(service, 320); var learned = service.LearnedRoot; var signature = NativeInspection.Signature(learned);
        for (var i = 0; i < 20; i++)
        {
            Sensors(service, 1, -1, 10000 + i * 4); var action = P(service.ActionAxis, 1, 10002 + i * 4);
            service.PredictOutcome(action); service.Learn(action, NativeGraph.BindFeedback(action, P(service.OutcomeAxis, 1, 10003 + i * 4), service.OutcomeAxis), false);
        }
        Assert.Equal(signature, NativeInspection.Signature(service.LearnedRoot)); Assert.Equal(signature, NativeInspection.Signature(learned));
        Assert.True(Accuracy(service) > .98);
    }
    [Fact]
    public void NativeTransportRetainsBehaviorAliasesAndResumesUpdates()
    {
        var service = new RelationService(candidateOrderSeed: 3); Train(service, 320);
        var bytes = GojoNumericFormat.Encode(service.Root); var restored = RelationService.FromRoot(Assert.IsType<Relation>(Assert.Single(GojoNumericFormat.Decode(bytes))));
        Assert.Equal(NativeInspection.Signature(service.Root), NativeInspection.Signature(restored.Root)); Assert.Equal(service.SelectedDescription, restored.SelectedDescription);
        Assert.True(Accuracy(restored) > .98); var prior = restored.TrainingTrials; Train(restored, 10, 1000); Assert.Equal(prior + 10, restored.TrainingTrials);
    }
    [Fact]
    public void FrozenLearnedParametersCanBeDeployedWithoutOperationalHistory()
    {
        var service = new RelationService(); Train(service, 320); var learned = service.LearnedRoot;
        var restored = RelationService.FromLearnedRoot(Assert.IsType<Relation>(Assert.Single(GojoNumericFormat.Decode(GojoNumericFormat.Encode(learned)))));
        Assert.False(NativeGraph.TryValue(restored.PublishFeature(), restored.FeatureAxis, out _)); Assert.True(Accuracy(restored) > .98);
        Assert.Equal(service.CandidateCount, restored.CandidateCount); Assert.Equal(service.TrainingTrials, restored.TrainingTrials);
    }
    [Fact]
    public void BoundedNativeMemoryDoesNotAccumulateTheEntireTrainingHistory()
    {
        var service = new RelationService(); Train(service, 320); var early = NativeInspection.Count(service.Root);
        Train(service, 320, 1000); var late = NativeInspection.Count(service.Root);
        Assert.InRange(late.Relations, 1, early.Relations + 1000); Assert.InRange(late.Carriers, 1, early.Carriers + 300);
        Assert.All(service.CandidateDiagnostics, candidate => Assert.InRange(candidate.Observations, 0, 64));
        Assert.True(Accuracy(service) > .98);
    }
    [Fact]
    public void CandidateOrderCanVaryWithoutHardcodedCorrectFirstChoice()
    {
        var a = new RelationService(candidateOrderSeed: 1); var b = new RelationService(candidateOrderSeed: 97);
        Train(a, 320); Train(b, 320); Assert.True(Accuracy(a) > .98); Assert.True(Accuracy(b) > .98);
        Assert.NotEqual(a.CandidateDiagnostics.Select(x => x.Description), b.CandidateDiagnostics.Select(x => x.Description));
    }
    internal static void Train(RelationService service, int count, int start = 0)
    {
        var random = new Random(291 + start);
        for (var i = 0; i < count; i++)
        {
            var a = random.Next(2) == 0 ? -1 : 1; var mode = random.Next(2) == 0 ? -1 : 1; var push = random.Next(2) == 0 ? -1 : 1;
            var time = (start + i) * 4L; Sensors(service, a, a * mode, time); var action = P(service.ActionAxis, push, time + 2);
            service.Learn(action, NativeGraph.BindFeedback(action, P(service.OutcomeAxis, push * mode, time + 3), service.OutcomeAxis));
        }
    }
    internal static void Sensors(RelationService service, int a, int b, long time)
    { service.AcceptA(P(service.InputAAxis, a, time)); service.AcceptB(P(service.InputBAxis, b, time + 1)); }
    internal static Relation P(Carrier axis, long value, long time) => NativeGraph.Publication(axis, value, new Carrier(), time);
    private static double Accuracy(RelationService service)
    {
        var correct = 0; var total = 0;
        foreach (var a in new[] { -1, 1 }) foreach (var mode in new[] { -1, 1 }) foreach (var push in new[] { -1, 1 })
        {
            Sensors(service, a, a * mode, 100000 + total * 4); var prediction = service.PredictOutcome(P(service.ActionAxis, push, 100002 + total * 4));
            if (NativeGraph.TryValue(prediction, service.OutcomeAxis, out var result) && result.Ticks == push * mode) correct++; total++;
        }
        return (double)correct / total;
    }
    private static HashSet<object> Reach(IElement root)
    {
        var seen = new HashSet<object>(ReferenceEqualityComparer.Instance);
        void Visit(IElement element)
        {
            if (element is CarrierLocation point) { Visit(point.Carrier); return; }
            if (!seen.Add(element)) return;
            if (element is Relation relation) { Visit(relation.Left); Visit(relation.Right); }
            if (element is Carrier carrier) foreach (var attachment in carrier.AttachedRelations) Visit(attachment);
        }
        Visit(root); return seen;
    }
}
