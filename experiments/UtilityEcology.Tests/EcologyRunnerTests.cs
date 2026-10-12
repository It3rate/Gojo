using CompositeEmergence;
using Gojo.Core;

namespace UtilityEcology.Tests;

/// <summary>Whole-experiment invariants across actual world, native accounting,
/// publication and learning boundaries. These do not assert a particular
/// authored policy's internal thresholds or a fitted seed's score.</summary>
public sealed class EcologyRunnerTests
{
    [Theory]
    [InlineData(16)] [InlineData(32)] [InlineData(64)]
    public void EveryPhysicalStepConservesMoneyAndTotalsIncludeInitialConstruction(int phaseLength)
    {
        var settings = new EcologySettings(PhaseLength: phaseLength);
        var arm = new EcologyArm("accounting-integration");
        var run = EcologyRunner.Run(803, arm, settings);
        Assert.Equal(4 * phaseLength, run.Trace.Count);
        long external = settings.InitialCapital;
        foreach (var step in run.Trace)
        {
            external += step.Gross;
            Assert.Equal(external, step.ExternalNet); Assert.Equal(external, step.AccountedMoney);
            Assert.True(step.Burned >= 0);
        }
        Assert.Equal(run.Trace.Sum(step => step.Gross), run.Gross);
        Assert.Equal(run.Rows.Sum(row => row.Gross), run.Gross);
        Assert.Equal(run.Trace.Sum(step => step.Burned), run.Burned);
        Assert.Equal(run.Rows.Sum(row => row.Burned), run.Burned);
        Assert.True(run.Trace[0].Burned >= 32); Assert.True(run.Burned >= 32);
        Assert.Equal((double)(run.Gross - run.Burned) / run.Trace.Count, run.NetPerStep);
        Assert.Equal(external, run.ExternalNet); Assert.Equal(external, run.AccountedMoney);
        var native = ServiceEconomy.FromRoot(Assert.IsType<Relation>(NativeGraph.Parts(run.NativeRoot, 9)[0]));
        Assert.Equal(run.AccountedMoney, native.AccountedMoney); Assert.Equal(run.ExternalNet, native.ExternalNet);
    }

    [Theory]
    [InlineData(16)] [InlineData(32)] [InlineData(64)]
    public void NoLearningNeverInventsAnInterpretationOrUpdatesFromAnOpenModel(int phaseLength)
    {
        var arm = EcologyRunner.Arms.Single(candidate => candidate.Name == "no-interpretation-learning");
        var run = EcologyRunner.Run(809, arm, new EcologySettings(PhaseLength: phaseLength));
        Assert.All(run.Trace, step =>
        {
            Assert.False(step.Interpreted); Assert.Null(step.Advice); Assert.False(step.LearningGranted);
            Assert.False(step.Refined); Assert.False(step.Reset); Assert.Equal(0, step.Landmarks);
        });
        Assert.All(run.Rows, row => { Assert.Equal(0, row.LearningUpdates); Assert.Equal(0, row.Refinements); Assert.Equal(0, row.Interpreted); });
        var bodies = NativeGraph.Parts((Relation)NativeGraph.Parts(run.NativeRoot, 9)[1], 4).Cast<Relation>()
            .Select(InterpretationProvider.FromRoot).ToArray();
        Assert.All(bodies, body =>
        {
            Assert.Equal(0, body.Updates); Assert.Equal(0, body.PresenceModel.Updates); Assert.Null(body.OrdinalModel);
            Assert.Equal(0UL, body.Gate.EvidenceCount); Assert.Equal(0UL, body.Gate.GrantCount);
        });
    }

    [Fact]
    public void DifferentEconomicPoliciesReceiveTheIdenticalActionIndependentWorldTape()
    {
        var settings = new EcologySettings(PhaseLength: 32); var tape = WorldTape.Create(811, settings.PhaseLength);
        var arms = new[]
        {
            new EcologyArm("frozen", Learning: false, Renew: false),
            new EcologyArm("always", Always: true, SuccessPaid: false),
            new EcologyArm("adaptive")
        };
        var runs = arms.Select(arm => EcologyRunner.Run(811, arm, settings)).ToArray();
        Assert.All(runs, run =>
        {
            Assert.Equal(tape.Digest(), run.TapeDigest); Assert.Equal(tape.Count, run.Trace.Count);
            foreach (var step in run.Trace)
            { Assert.Equal(tape[step.Tick].Source, step.Source); Assert.Equal(tape[step.Tick].Phase, step.Phase); }
        });
        Assert.Equal(runs[0].Trace.Select(step => (step.Tick, step.Phase, step.Source)),
            runs[1].Trace.Select(step => (step.Tick, step.Phase, step.Source)));
        Assert.Equal(runs[0].Trace.Select(step => (step.Tick, step.Phase, step.Source)),
            runs[2].Trace.Select(step => (step.Tick, step.Phase, step.Source)));
    }

    [Fact]
    public void ChangingPrivateUnitsAndZeroPointsPreservesAllSemanticDecisionsAndCosts()
    {
        var arm = new EcologyArm("opaque-encoding-integration");
        var baseline = EcologyRunner.Run(821, arm, new EcologySettings(PhaseLength: 64, Encoding: 0));
        foreach (var encoding in new[] { 1, 2 })
        {
            var rescaled = EcologyRunner.Run(821, arm, new EcologySettings(PhaseLength: 64, Encoding: encoding));
            Assert.Equal(baseline.TapeDigest, rescaled.TapeDigest);
            Assert.Equal(baseline.Trace, rescaled.Trace);
            Assert.Equal(baseline.Rows, rescaled.Rows);
            Assert.Equal(baseline.Gross, rescaled.Gross); Assert.Equal(baseline.Burned, rescaled.Burned);
            Assert.Equal(baseline.ProducerGraphs, rescaled.ProducerGraphs); Assert.Equal(baseline.WholeGraph, rescaled.WholeGraph);
        }
    }

    [Fact]
    public void WithdrawnCommunicationUsesOnlyAlternatingFallbackCommandsForEachPurchasedSource()
    {
        var run = EcologyRunner.Run(823, new EcologyArm("withdrawn-integration", Communicate: false),
            new EcologySettings(PhaseLength: 64));
        Assert.Contains(run.Trace, step => step.Bought);
        foreach (var group in run.Trace.Where(step => step.Bought).GroupBy(step => step.Source))
        {
            var commands = group.Select(step => step.Action).ToArray(); Assert.NotEmpty(commands);
            Assert.Equal(-1, commands[0]);
            for (var index = 1; index < commands.Length; index++) Assert.Equal(-commands[index - 1], commands[index]);
        }
        Assert.All(run.Trace.Where(step => !step.Bought), step => Assert.Equal(0, step.Action));
    }

    [Fact]
    public void BoundConsequencesBuildUsefulPresenceAdviceWhileTheNoLearningControlRemainsUninterpreted()
    {
        var settings = new EcologySettings(PhaseLength: 128);
        var learning = EcologyRunner.Run(827, new EcologyArm("constant-law-learning", Always: true,
            Opportunity: false, SuccessPaid: false, Renew: false), settings);
        var neutral = EcologyRunner.Run(827, new EcologyArm("constant-law-neutral", Learning: false,
            Opportunity: false, SuccessPaid: false, Renew: false), settings);
        var learnedCommon = learning.Trace.Where(step => step.Phase == 0 && step.Source == 0).ToArray();
        var neutralCommon = neutral.Trace.Where(step => step.Phase == 0 && step.Source == 0).ToArray();
        Assert.True(learnedCommon.Length >= 32); Assert.Equal(learnedCommon.Length, neutralCommon.Length);
        Assert.Contains(learnedCommon, step => step.Interpreted);
        Assert.DoesNotContain(neutralCommon, step => step.Interpreted);
        Assert.True(learnedCommon.Count(step => step.Correct) >= learnedCommon.Length * .75);
        Assert.True(neutralCommon.Count(step => step.Correct) <= neutralCommon.Length * .6);
        Assert.True(learnedCommon.Sum(step => step.Gross) > neutralCommon.Sum(step => step.Gross));
        Assert.True(learning.Rows.Single(row => row.Phase == 0 && row.Source == 0).LearningUpdates > 0);
    }
}
