using Gojo.Core;
using Gojo.Encoding;
using Gojo.Experiments.ConstructionMachines;
using Gojo.Structures;
using static Gojo.Experiments.ConstructionMachines.MachineExpressions;

namespace Gojo.ConstructionMachines.Tests;

public sealed class EnsembleSettlementTests
{
    [Fact]
    public void AnErrorRetainsItsActualExcessIntervalAndOpposedResponsePassages()
    {
        var m = EnsembleRecipes.Create(method: 0, reviewBudget: 0).Model; var factor = m.Active[0];
        var samples = m.Capture(); var read = factor.Assess(samples); var f = F(F(read)[2]);
        Assert.True(EvidenceObligation.Resolved(read));
        Assert.Equal(-34, Number(f[0])); Assert.Equal(26, Number(f[3]));
        Assert.True(TimeStepSequence.TryReadSpan((Relation)f[4], out var from, out var to));
        Assert.Equal(-34, from.Ticks); Assert.Equal(-8, to.Ticks);
        var requests = EvidenceObligation.Requests(factor.Propose(samples, read, null, G()));
        Assert.True(Number(F(requests[0])[3]) > 0); // roof left moves right
        Assert.True(Number(F(requests[1])[3]) < 0); // wall left can move left
        Assert.Same(factor.Surface, F(read)[1]);
    }

    [Fact]
    public void PermittedEavesRemainQuietWithoutBeingSnappedToWallEndpoints()
    {
        var m = EnsembleRecipes.Create(condition: "quiet").Model; var before = Coordinates(m);
        Assert.Equal(0, Number(EnsembleSettlement.Cost(m.LastRead)));
        for (var i = 0; i < 6; i++) Assert.False(m.Step().Rewritten);
        Assert.Equal(before, Coordinates(m)); Assert.Equal(0UL, m.Reviews.Value);
        Assert.NotEqual(Program.XY(m.Machines[0].Point(0))[0], Program.XY(m.Machines[1].Point(0))[0]);
    }

    [Fact]
    public void UnknownPermissionIsRetainedAndDoesNotPublishAZeroErrorOrAResponse()
    {
        var m = EnsembleRecipes.Create(condition: "unknown").Model; var factor = m.Active[^1];
        var read = factor.Assess(m.Capture());
        Assert.False(EvidenceObligation.Resolved(read)); Assert.IsType<Placeholder>(F(read)[2]);
        Assert.Empty(EvidenceObligation.Requests(factor.Propose(m.Capture(), read, null, G())));
        Assert.Equal(1, EnsembleSettlement.Unknown(m.LastRead));
        for (var i = 0; i < 5; i++) Assert.False(m.Step().Rewritten);
        Assert.Equal(1, EnsembleSettlement.Unknown(m.LastRead));
    }

    [Fact]
    public void TheFourPoliciesRetainTheSameTotalInfluenceAndUsefulDifferentResponses()
    {
        var results = new List<double>();
        for (var policy = 0; policy < 4; policy++)
        {
            var m = EnsembleRecipes.Create(policy, reviewBudget: 0).Model;
            Assert.Equal(1, m.Active[0].Rules.Sum(r => Number(F(r)[2])));
            var before = Number(EnsembleSettlement.Cost(m.LastRead));
            for (var i = 0; i < 16; i++) m.Step();
            Assert.True(Number(EnsembleSettlement.Cost(m.LastRead)) < before);
            results.Add(Number(EnsembleSettlement.Cost(m.LastRead)));
        }
        Assert.True(results.Distinct().Count() > 1);
    }

    [Fact]
    public void RepeatedQueriesAreObservationAndDoNotIntroduceMotionOrMemory()
    {
        var m = EnsembleRecipes.Create().Model; var before = Coordinates(m); var memory = m.Memory; var read = m.LastRead;
        for (var i = 0; i < 4; i++) foreach (var factor in m.Active) factor.Assess(m.Capture());
        Assert.Equal(before, Coordinates(m)); Assert.Same(memory, m.Memory); Assert.Same(read, m.LastRead);
        Assert.Equal(0UL, m.Persistence.Value); Assert.Equal(0UL, m.Reviews.Value);
    }

    [Fact]
    public void ForeignEqualValuedSamplesCannotPassAnInputCalibration()
    {
        var m = EnsembleRecipes.Create(method: 4).Model; var factor = m.Active[0]; var samples = m.Capture();
        var port = (Relation)F(factor.Terms[0])[0]; var actual = F(samples[port]);
        samples[port] = G(port, actual[1], new CarrierLocation(new Carrier(), (long)Number(actual[2])), actual[3]);
        Assert.False(EvidenceObligation.Resolved(factor.Assess(samples)));
        var current = m.Capture();
        var requests = EvidenceObligation.Requests(factor.Propose(current, factor.Assess(current), null, G()));
        var original = F(requests[0]);
        var foreign = G(original[0], original[1], original[2], new CarrierLocation(new Carrier(), 13), original[4]);
        var negotiated = m.Negotiate([foreign]);
        Assert.Same(foreign, F(F(negotiated)[2])[0]);
        var pending = F(F(negotiated)[4])[0];
        Assert.IsType<Placeholder>(F(F(pending)[0])[2]);
        Assert.False(m.Machines[1].Apply([foreign], out _));
    }

    [Fact]
    public void MissingSupportCannotWinEvenWhenItsNumericalPreviewHelps()
    {
        var m = EnsembleRecipes.Create(reviewBudget: 0).Model; m.Step(); m.Step();
        var missing = m.Probe((Relation)F(m.Proposals)[2]); var baseline = m.Probe(null);
        Assert.True(EnsembleSettlement.ProbeResolved(missing));
        Assert.Equal(1, Number(F(F(missing)[2])[2]));
        Assert.True(Number(F(F(missing)[2])[1]) < Number(F(F(baseline)[2])[1]));
        Assert.Equal(0, CoupledSettlement.OpenPaths(m.Joint));
    }

    [Fact]
    public void ProposalReviewUsesAContinuingBaselineAndLeavesAcceptanceCoordinatesAlone()
    {
        var m = EnsembleRecipes.Create().Model; var ports = m.Joint.Mechanical.Ports.ToArray();
        var step = UntilChange(m);
        Assert.Equal(ports, m.Joint.Mechanical.Ports);
        Assert.Equal(Number(EnsembleSettlement.Cost(step.AfterMotion)), Number(EnsembleSettlement.Cost(step.AfterDecision)));
        Assert.Equal(AssessValues(step.AfterMotion), AssessValues(step.AfterDecision));
        var review = (Relation)step.Review; var result = F(F(review)[2]);
        Assert.Equal(3, result.Length); Assert.True(EnsembleSettlement.ProbeResolved((Relation)result[1]));
        var candidates = F(result[2]).Cast<Relation>().ToArray();
        Assert.True(EnsembleSettlement.Eligible(candidates[0]));
        Assert.False(EnsembleSettlement.Eligible(candidates[2]));
        Assert.True(Number(F(F(candidates[0])[2])[1]) < Number(F(F((Relation)result[1])[2])[1]));
        Assert.True(Number(EnsembleSettlement.Cost(m.LastRead)) > 0);
    }

    [Fact]
    public void APreviewSeesPublicNeighboursWithoutChangingAnyLiveParticipant()
    {
        var m = EnsembleRecipes.Create(reviewBudget: 0).Model; m.Step(); m.Step();
        var before = Coordinates(m); var pubs = m.Machines.Select(c => c.CurrentPublication).ToArray(); var memory = m.Memory;
        var preview = m.Probe((Relation)F(m.Proposals)[0]);
        Assert.Equal(before, Coordinates(m)); Assert.Equal(pubs, m.Machines.Select(c => c.CurrentPublication));
        Assert.Same(memory, m.Memory); Assert.Equal(0UL, m.Reviews.Value);
        foreach (var machine in new[] { m.Machines[0], m.Machines[2] })
            foreach (var actuator in F(F(machine.Definition)[0]).OfType<Carrier>())
                Assert.DoesNotContain(actuator, preview.Carriers());
    }

    [Fact]
    public void FixedParticipantsRemainBlockedWithVisibleDisagreement()
    {
        var m = EnsembleRecipes.Create(condition: "blocked").Model; var before = Coordinates(m);
        for (var i = 0; i < 6; i++) Assert.False(m.Step().Rewritten);
        Assert.Equal(before, Coordinates(m)); Assert.True(Number(EnsembleSettlement.Cost(m.LastRead)) > 0);
        Assert.All(m.Machines.SelectMany(c => c.Responses), r => Assert.Equal(0, StepLimit(r)));
    }

    [Fact]
    public void ADeclaredSupportCorrespondencePoolsCopiesWhileLeavingThemInspectible()
    {
        var pooled = EnsembleRecipes.Create(condition: "pooled-copies", reviewBudget: 0).Model;
        var single = EnsembleRecipes.Create(condition: "outlier", reviewBudget: 0).Model;
        Assert.Equal(single.Active.Length + 4, pooled.Obligations.Length);
        Assert.Equal(single.Active.Length, pooled.Active.Length);
        for (var i = 0; i < 14; i++) { pooled.Step(); single.Step(); }
        Assert.Equal(Coordinates(single), Coordinates(pooled));
        Assert.Equal(Number(EnsembleSettlement.Cost(single.LastRead)), Number(EnsembleSettlement.Cost(pooled.LastRead)));
    }

    [Fact]
    public void UnpooledCopiesCanAmplifyARequestWithoutInflatingTheIndependentAudit()
    {
        var pooled = EnsembleRecipes.Create(condition: "pooled-copies", reviewBudget: 0).Model;
        var unpooled = EnsembleRecipes.Create(condition: "unpooled-copies", reviewBudget: 0).Model;
        Assert.Equal(Number(EnsembleSettlement.Cost(pooled.LastRead)), Number(EnsembleSettlement.Cost(unpooled.LastRead)));
        for (var i = 0; i < 10; i++) { pooled.Step(); unpooled.Step(); }
        Assert.NotEqual(Coordinates(pooled), Coordinates(unpooled));
    }

    [Fact]
    public void HelpAndHarmAreRecordedAgainstTheSamePermittedRegions()
    {
        var m = EnsembleRecipes.Create(condition: "outlier", reviewBudget: 0).Model; var step = m.Step();
        var effects = F(step.Effects).Cast<Relation>().ToArray(); Assert.Equal(m.Active.Length, effects.Length);
        var changes = effects.Select(r =>
        {
            Assert.True(OperationClaim.TryReadIntervalResponse(r, out _, out _, out _, out var before, out var after, out var change));
            Assert.Equal(before.Ticks - after.Ticks, change.Ticks); return change.Ticks;
        }).ToArray();
        Assert.Contains(changes, x => x > 0); Assert.Contains(changes, x => x < 0);
    }

    [Fact]
    public void EveryAdmittedParameterChangeKeepsItsAllocationRemainders()
    {
        var m = EnsembleRecipes.Create(reviewBudget: 0).Model; m.Step();
        foreach (var receipt in m.Machines.Select(c => c.LastReceipt!).Where(r => r is not null))
            foreach (var outcome in NegotiatingMachine.Outcomes(receipt))
                Assert.Equal(Number(outcome.Requested), Number(outcome.Accepted) + Number(outcome.BoundsBlocked) +
                    Number(outcome.CapacityDeferred) + Number(outcome.QuantumRemainder) + Number(outcome.CandidateBlocked), 9);
    }

    [Fact]
    public void LocalNegotiationPassesAgreementAndAccountsForWithheldOpposedRequests()
    {
        var m = EnsembleRecipes.Create(method: 4, reviewBudget: 0).Model; var port = m.Machines[2].Port(0, 0); var own = m.Machines[2].Publish(port);
        Relation Request(long delta)
        {
            Assert.True(ProjectionResidue.TryRead(F(own)[2], out var axis, out _, out _));
            var target = Gojo.Equations.Equation.PredictWitness(new Carrier(), Gojo.Equations.Equation.Sum(axis,
                Gojo.Equations.Equation.UnitPort(axis), Gojo.Equations.Equation.UnitPort(axis)), new Relation(F(own)[2], new CarrierLocation(axis, delta)), axis);
            return EvidenceObligation.Signal(port, own, target, G()).Request;
        }
        var same = m.Negotiate([Request(100), Request(20)]);
        Assert.Equal(new[] { 100d, 20d }, F(F(same)[2]).Select(r => Number(F(r)[3])));
        var opposed = m.Negotiate([Request(100), Request(-20)]);
        Assert.Equal(new[] { 12d, -12d }, F(F(opposed)[2]).Select(r => Number(F(r)[3])));
        foreach (var entry in F(F(opposed)[4]))
        {
            var bound = F(entry)[0]; var raw = Number(F(F(F(bound)[4])[0])[3]);
            Assert.Equal(raw, Number(F(bound)[2]) + Number(F(bound)[3]));
            var guardRead = F(F(bound)[4])[1];
            Assert.All(F(F(guardRead)[2]), proof =>
            {
                var claim = (Relation)F(F(proof)[2])[1];
                Assert.True(OperationClaim.TryReadResolvedBetweenViolation(claim, out var violation)); Assert.Equal(0, violation.Ticks);
            });
        }
        Assert.Equal(174, Program.XY(m.Machines[2].Point(0))[0]);
    }

    [Fact]
    public void NegotiatedBlendIsAConditionalLocalLawAndStillPreservesRequiredPaths()
    {
        var scene = EnsembleRecipes.Create(method: 4); var m = scene.Model; var initial = Number(EnsembleSettlement.Cost(m.LastRead));
        for (var i = 0; i < 24; i++) m.Step();
        Assert.True(Number(EnsembleSettlement.Cost(m.LastRead)) < initial);
        Assert.Equal(0, CoupledSettlement.OpenPaths(m.Joint));
        Assert.True(F(F(m.Policy)[6]).Length > 0);
    }

    [Fact]
    public void RestorationPreservesAliasesAndContinuesAcrossAConstructionChange()
    {
        var original = EnsembleRecipes.Create(method: 4).Model; original.Step(); original.Step();
        var copy = EnsembleSettlement.Restore((Relation)GojoNumericFormat.Decode(GojoNumericFormat.Encode(original.Surface))[0]);
        Assert.Same(copy.Machines[1], copy.Joint.Mechanical);
        for (var i = 0; i < 6; i++)
        {
            var a = original.Step(); var b = copy.Step(); Assert.Equal(a.Rewritten, b.Rewritten);
            Assert.Equal(Coordinates(original), Coordinates(copy));
            Assert.Equal(Number(EnsembleSettlement.Cost(original.LastRead)), Number(EnsembleSettlement.Cost(copy.LastRead)));
        }
        Assert.IsType<Relation>(copy.Selected);
    }

    private static EnsembleSettlement.Transition UntilChange(EnsembleSettlement m)
    { for (var i = 0; i < 20; i++) { var s = m.Step(); if (s.Rewritten) return s; } throw new Xunit.Sdk.XunitException("No eligible construction change."); }
    private static double[] Coordinates(EnsembleSettlement m) => m.Machines.SelectMany(c => c.Points.SelectMany(Program.XY)).ToArray();
    private static double[] AssessValues(Relation r) => EnsembleSettlement.Assessments(r).Select(a => Number(EvidenceObligation.Value(a))).ToArray();
    private static double Number(IElement e) => CoupledSettlement.Number(e);
    private static long StepLimit(Relation r) { TimeStepSequence.TryReadSpan((Relation)F(F(r)[3])[1], out _, out var end); return end.Ticks; }
}
