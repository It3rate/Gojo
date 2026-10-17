using CompositeEmergence;
using Gojo.Core;

namespace UtilityEcology.Tests;

public sealed class SeedTrialRunnerTests
{
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void LegacyModesRetainTheirExactDecisionsAndCosts(bool progress)
    {
        var study = SeedTrialRunner.Arms.Single(arm => !arm.ProtectedTrials && arm.ProgressBaseline == progress);
        var settings = new EcologySettings(PhaseLength: 32);
        var expected = EcologyRunner.Run(887, EcologyRunner.ProgressArms[progress ? 1 : 0] with { Name = study.Name, RetestPeriod = 8 }, settings);
        var actual = SeedTrialRunner.Run(887, study, settings);
        Assert.Equal(expected.Trace, actual.Ecology.Trace); Assert.Equal(expected.Rows, actual.Ecology.Rows);
        Assert.Equal(expected.Gross, actual.Ecology.Gross); Assert.Equal(expected.Burned, actual.Ecology.Burned);
        Assert.Empty(actual.SeedTrace);
    }

    [Fact]
    public void ProtectedTrialsCannotTurnEstimatedBenefitsIntoPhysicalMoney()
    {
        var settings = new EcologySettings(PhaseLength: 128);
        var run = SeedTrialRunner.Run(889, SeedTrialRunner.Arms[2], settings);
        long expected = settings.InitialCapital;
        foreach (var step in run.Ecology.Trace)
        {
            expected += step.Gross;
            Assert.Equal(expected, step.ExternalNet); Assert.Equal(expected, step.AccountedMoney);
            Assert.True(step.Burned >= 0);
        }
        Assert.Equal(run.Ecology.Burned, run.Ecology.Trace.Sum(step => step.Burned));
        Assert.All(run.SeedTrace, step => { Assert.InRange(step.SeedSpent, 0, 320); Assert.InRange(step.SeedRemaining, 0, 320); Assert.InRange(step.Trials, 0, 8); });
        var fields = NativeGraph.Parts(run.NativeRoot, 9); var economy = ServiceEconomy.FromRoot((Relation)fields[0]);
        var providers = NativeGraph.Parts((Relation)fields[1], 4).Cast<Relation>().Select(SeededProvider.FromRoot).ToArray();
        Assert.All(providers, provider => Assert.Contains(economy.Services, service =>
            SeededProvider.FromRoot(service.Body).SeedEnergy.SameIdentityAs(provider.SeedEnergy)));
    }

    [Fact]
    public void NonlinearOpaqueSensorEncodingCannotChangeTrialChoices()
    {
        var arm = SeedTrialRunner.Arms[2];
        var baseline = SeedTrialRunner.Run(893, arm, new(PhaseLength: 64));
        var changed = SeedTrialRunner.Run(893, arm, new(PhaseLength: 64, Encoding: 2));
        Assert.Equal(baseline.Ecology.Trace, changed.Ecology.Trace); Assert.Equal(baseline.SeedTrace, changed.SeedTrace);
        Assert.Equal(baseline.Ecology.Gross, changed.Ecology.Gross); Assert.Equal(baseline.Ecology.Burned, changed.Ecology.Burned);
    }

    [Fact]
    public void SavedActualSeedPursesAndCandidateAnchorsRetainLocalBehavior()
    {
        var run = SeedTrialRunner.Run(899, SeedTrialRunner.Arms[2], new(PhaseLength: 64));
        var restored = (Relation)GojoNumericFormat.Decode(GojoNumericFormat.Encode(run.NativeRoot))[0];
        var check = SeedTrialArchive.Check(run.NativeRoot, restored, true);
        Assert.True(check.Matched); Assert.True(check.Conserved); Assert.True(check.AliasesMatched);
    }
}
