using CompositeEmergence;
using Gojo.Core;

namespace UtilityEcology.Tests;

public sealed class ProgressIntegrationTests
{
    [Fact]
    public void PassiveObserverPreservesExistingDecisionsCostsAndFunding()
    {
        var settings = new EcologySettings(PhaseLength: 64);
        var original = EcologyRunner.Run(733, EcologyRunner.Arms.Single(arm => arm.Name == "matched/error/upfront"), settings);
        var passive = EcologyRunner.Run(733, EcologyRunner.ProgressArms[0], settings);
        Assert.Equal(original.TapeDigest, passive.TapeDigest);
        Assert.Equal(original.Gross, passive.Gross); Assert.Equal(original.Burned, passive.Burned);
        Assert.Equal(original.Trace.Select(step => step with { Arm = "same", Progress = null }),
            passive.Trace.Select(step => step with { Arm = "same", Progress = null }));
    }

    [Fact]
    public void PausedTrainingKeepsObservingRealPurchasedConsequencesAndConservesMoney()
    {
        var run = EcologyRunner.Run(739, EcologyRunner.ProgressArms[1], new(PhaseLength: 64));
        Assert.All(run.Trace, step => Assert.Equal(step.ExternalNet, step.AccountedMoney));
        var fields = NativeGraph.Parts(run.NativeRoot, 9);
        var providers = NativeGraph.Parts((Relation)fields[1], 4).Cast<Relation>().Select(InterpretationProvider.FromRoot).ToArray();
        Assert.Equal(run.Trace.Count(step => step.Bought), providers.Sum(p => checked((int)p.Gate.Progress!.ObservationCount)));
        Assert.Contains(run.Trace, step => step.Bought && !step.LearningGranted && step.Progress is not null);
        Assert.All(run.Trace.Where(step => step.Progress is not null), step =>
        {
            Assert.Equal(step.LearningGranted, step.Progress!.Funded);
            if (!step.Progress.GateRequested) Assert.False(step.Progress.Funded);
        });
    }

    [Fact]
    public void ProgressOnlyConsumesOrderInformationAndIgnoresPrivateUnitEncodings()
    {
        var baseline = EcologyRunner.Run(743, EcologyRunner.ProgressArms[1], new(PhaseLength: 64));
        var changed = EcologyRunner.Run(743, EcologyRunner.ProgressArms[1], new(PhaseLength: 64, Encoding: 2));
        Assert.Equal(baseline.Trace, changed.Trace); Assert.Equal(baseline.Rows, changed.Rows);
        Assert.Equal(baseline.Gross, changed.Gross); Assert.Equal(baseline.Burned, changed.Burned);
    }

    [Fact]
    public void CompleteProgressEconomySurvivesSavedFileAndContinuesPaidBehavior()
    {
        var run = EcologyRunner.Run(751, EcologyRunner.ProgressArms[1], new(PhaseLength: 64));
        var path = Path.GetTempFileName();
        try
        {
            File.WriteAllBytes(path, GojoNumericFormat.Encode(run.NativeRoot));
            var restored = (Relation)GojoNumericFormat.Decode(File.ReadAllBytes(path))[0];
            var check = ArchiveReplay.Check(run.NativeRoot, restored);
            Assert.True(check.Matched); Assert.True(check.ContinuedMoneyConserved);
        }
        finally { File.Delete(path); }
    }
}
