using Gojo.Core;
using Gojo.Structures;

namespace PrimitiveLearning.Tests;

public sealed class QueryLearnerTests
{
    [Fact]
    public void LaterBenefitAndPaidCostTeachDifferentPublicContexts()
    {
        var learner = Create(); Train(learner);
        Assert.Equal(0, Value(learner.Decide(P(learner.ContextAxis, 0), false), learner.ChoiceAxis));
        Assert.Equal(1, Value(learner.Decide(P(learner.ContextAxis, 1), false), learner.ChoiceAxis));
        Assert.Equal(48, learner.ObservationCount(0, 0) + learner.ObservationCount(0, 1));
        Assert.Equal(48, learner.ObservationCount(1, 0) + learner.ObservationCount(1, 1));
        Assert.True(learner.Root.TryReadOrderedGroup(out var root));
        Assert.Same(learner.CostAxis, root[2]);
        Assert.IsType<Relation>(root[6]); // explicit cost-to-benefit calibration
    }

    [Fact]
    public void FrozenDecisionAndFeedbackDoNotChangeLearnedCounts()
    {
        var learner = Create(); Train(learner); var before = Counts(learner);
        learner.Decide(P(learner.ContextAxis, 1), false);
        var refusal = learner.Observe(P(learner.ContextAxis, 1), learner.BindBenefit(P(learner.BenefitAxis, 900)), learner.BindCost(P(learner.CostAxis, 0)));
        AssertOpen(refusal); Assert.Equal(before, Counts(learner));
        learner.Decide(P(learner.ContextAxis, 0));
        AssertOpen(learner.Observe(P(learner.ContextAxis, 0), learner.BindBenefit(P(learner.BenefitAxis, 900)), learner.BindCost(P(learner.CostAxis, 0)), false));
        Assert.Equal(before, Counts(learner));
    }

    [Fact]
    public void OpenOrUnrelatedPortsCannotBecomeQueryEvidence()
    {
        var learner = Create();
        AssertOpen(learner.Decide(P(new Carrier(), 0)));
        AssertOpen(learner.Decide(ReadWitness.Create(new Carrier(), new Placeholder(), learner.ContextAxis,
            new Placeholder(), new Placeholder(), new Placeholder())));
        learner.Decide(P(learner.ContextAxis, 0));
        AssertOpen(learner.Observe(P(learner.ContextAxis, 1), learner.BindBenefit(P(learner.BenefitAxis, 4)), learner.BindCost(P(learner.CostAxis, 0))));
        AssertOpen(learner.Observe(P(learner.ContextAxis, 0), learner.BindBenefit(P(new Carrier(), 4)), learner.BindCost(P(learner.CostAxis, 0))));
        AssertOpen(learner.Observe(P(learner.ContextAxis, 0), learner.BindBenefit(P(learner.BenefitAxis, 4)), learner.BindCost(P(learner.CostAxis, -1))));
        Assert.Equal(new long[] { 0, 0, 0, 0 }, Counts(learner));
    }

    [Fact]
    public void ACoarseNominalCannotSupplyAnExactContextOrFeedback()
    {
        var learner = Create(); var capacity = FiniteReadSurface.Capacity(new Carrier(), learner.ContextAxis, 0, 4, 2);
        var coarse = FiniteReadSurface.Publish(new Carrier(), capacity, new CarrierLocation(learner.ContextAxis, 1), new Placeholder());
        AssertOpen(learner.Decide(coarse));
        var benefitCapacity = FiniteReadSurface.Capacity(new Carrier(), learner.BenefitAxis, -10, 10, 4);
        var coarseBenefit = FiniteReadSurface.Publish(new Carrier(), benefitCapacity, new CarrierLocation(learner.BenefitAxis, 5), new Placeholder());
        learner.Decide(P(learner.ContextAxis, 0));
        AssertOpen(learner.Observe(P(learner.ContextAxis, 0), learner.BindBenefit(coarseBenefit), learner.BindCost(P(learner.CostAxis, 0))));
        Assert.Equal(new long[] { 0, 0, 0, 0 }, Counts(learner));
    }

    [Fact]
    public void RepeatedDecisionOrAliasedOutcomeIsNotIndependentFeedback()
    {
        var learner = Create(); var source = new Relation(new Placeholder(), new Placeholder());
        var benefit = Publication(learner.BenefitAxis, 4, source); var context = P(learner.ContextAxis, 0);
        learner.Decide(context); var boundBenefit=learner.BindBenefit(benefit); learner.Observe(context, boundBenefit, learner.BindCost(P(learner.CostAxis, 0)));
        AssertOpen(learner.Observe(context, boundBenefit, learner.BindCost(P(learner.CostAxis, 0))));
        learner.Decide(context);
        AssertOpen(learner.Observe(context, learner.BindBenefit(Publication(learner.BenefitAxis, 4, source)), learner.BindCost(P(learner.CostAxis, 0))));
        Assert.Equal(1, learner.ObservationCount(0, 0) + learner.ObservationCount(0, 1));
    }

    [Fact]
    public void SelectedChoiceAndCreditedCandidateShareNativeProvenance()
    {
        var learner = Create(); var choice = learner.Decide(P(learner.ContextAxis, 0));
        Assert.True(ReadWitness.TryRead(choice, out _, out var surface, out _, out _, out var provenance));
        Assert.True(ScopedChoice.TryRead((Relation)surface, out _, out _, out var offers, out _)); Assert.Equal(2, offers.Length);
        Assert.True(((Relation)provenance).TryReadOrderedGroup(out var decisionFields));
        var feedback = learner.Observe(P(learner.ContextAxis, 0), learner.BindBenefit(P(learner.BenefitAxis, 4)), learner.BindCost(P(learner.CostAxis, 0)));
        Assert.True(ReadWitness.TryRead(feedback, out _, out var creditedDecision, out _, out _, out var feedbackProvenance));
        Assert.Same(choice, creditedDecision);
        Assert.True(((Relation)feedbackProvenance).TryReadOrderedGroup(out var feedbackFields));
        Assert.Same(decisionFields[1], feedbackFields[3]);
    }

    [Fact]
    public void ArchiveRediscoveryPreservesContextChoicesAndFeedbackIdentity()
    {
        var learner = Create(); Train(learner);
        var restored = QueryLearner.FromRoot(Assert.IsType<Relation>(Assert.Single(GojoNumericFormat.Decode(GojoNumericFormat.Encode(learner.Root)))));
        Assert.Equal(Counts(learner), Counts(restored));
        Assert.NotSame(learner.ContextAxis, restored.ContextAxis);
        for (var context = 0; context < 2; context++)
            Assert.Equal(Value(learner.Decide(P(learner.ContextAxis, context), false), learner.ChoiceAxis),
                Value(restored.Decide(P(restored.ContextAxis, context), false), restored.ChoiceAxis));
    }

    [Fact]
    public void EvidencePublicationExposesNativeSignedTotalsAndFrozenMean()
    {
        var learner = Create();
        Assert.True(QueryLearner.TryInspectEvidence(learner.ReadEvidence(0, 0), out var empty));
        Assert.Equal(0, empty.Observations); Assert.Null(empty.NetMean256);
        var context = P(learner.ContextAxis, 0);
        foreach (var benefit in new[] { 4, -4, 4, 4 })
        {
            Assert.Equal(0, Value(learner.Decide(context), learner.ChoiceAxis));
            learner.Observe(context, learner.BindBenefit(P(learner.BenefitAxis, benefit)), learner.BindCost(P(learner.CostAxis, 0)));
        }
        var before = Counts(learner); var publication = learner.ReadEvidence(0, 0);
        Assert.True(QueryLearner.TryInspectEvidence(publication, out var evidence));
        Assert.Equal(4, evidence.Observations); Assert.Equal(12, evidence.PositiveBenefit);
        Assert.Equal(4, evidence.NegativeBenefit); Assert.Equal(0, evidence.PaidCost);
        Assert.Equal(2 * 256, evidence.NetMean256); Assert.Equal(before, Counts(learner));
        learner.Decide(context); learner.Observe(context, learner.BindBenefit(P(learner.BenefitAxis, 4)), learner.BindCost(P(learner.CostAxis, 1)));
        Assert.True(QueryLearner.TryInspectEvidence(publication, out var frozen)); Assert.Equal(evidence, frozen);
        var restored = Assert.IsType<Relation>(Assert.Single(GojoNumericFormat.Decode(GojoNumericFormat.Encode(publication))));
        Assert.True(QueryLearner.TryInspectEvidence(restored, out var transported)); Assert.Equal(evidence, transported);
    }

    private static QueryLearner Create() => new(new Carrier(), new Carrier(), new Carrier());
    private static void Train(QueryLearner learner)
    {
        for (var trial = 0; trial < 96; trial++)
        {
            var context = trial % 2; var publication = P(learner.ContextAxis, context);
            var choice = Value(learner.Decide(publication), learner.ChoiceAxis);
            // Independent fixture consequence: context0 already succeeds;
            // context1 succeeds only after detail. The learner never sees this law.
            var benefit = context == 0 || choice == 1 ? 4 : 0;
            learner.Observe(publication, learner.BindBenefit(P(learner.BenefitAxis, benefit)), learner.BindCost(P(learner.CostAxis, choice)));
        }
    }

    [Fact]
    public void SameAxisOrStaleReceiptsCannotCreditANewDecision()
    {
        var learner=Create(); var context=P(learner.ContextAxis,0);
        learner.Decide(context);
        AssertOpen(learner.Observe(context,P(learner.BenefitAxis,4),learner.BindCost(P(learner.CostAxis,0))));
        var oldBenefit=learner.BindBenefit(P(learner.BenefitAxis,4)); var oldCost=learner.BindCost(P(learner.CostAxis,0));
        learner.Decide(context);
        AssertOpen(learner.Observe(context,oldBenefit,oldCost)); Assert.Equal(new long[]{0,0,0,0},Counts(learner));
    }
    private static long[] Counts(QueryLearner learner) => [learner.ObservationCount(0,0), learner.ObservationCount(0,1), learner.ObservationCount(1,0), learner.ObservationCount(1,1)];
    private static Relation P(Carrier axis, long value) => Publication(axis, value, Relation.Point(axis, value));
    private static Relation Publication(Carrier axis, long value, IElement source) => ReadWitness.Create(new Carrier(), source,
        Relation.Segment(axis, value, value), new CarrierLocation(axis, value), Relation.OrderedGroup(new Carrier()), source);
    private static long Value(Relation publication, Carrier axis)
    {
        Assert.True(ReadWitness.TryRead(publication, out _, out _, out var result, out _, out _));
        var point = Assert.IsType<CarrierLocation>(result); Assert.Same(axis, point.Carrier); return point.Ticks;
    }
    private static void AssertOpen(Relation publication)
    {
        Assert.True(ReadWitness.TryRead(publication, out _, out _, out var result, out var residue, out _));
        Assert.IsType<Placeholder>(result); Assert.IsType<Relation>(residue);
    }
}
