using CompositeEmergence;
using Gojo.Core;
using Gojo.Structures;
using PrimitiveLearning;

namespace UtilityEcology.Tests;

public sealed class ProgressGateTests
{
    [Fact]
    public void UnknownBootstrapIsFiniteAndRecurringRetestsStillRequirePaidWork()
    {
        var fixture = Funded(); var gate = fixture.Gate;
        for (var index = 0; index < 16; index++) Assert.True(Permit(gate));
        Assert.Equal(16UL, gate.GrantCount); AssertOpen(gate.ReadError()); AssertOpen(gate.Progress!.ReadProgress());
        Assert.Equal(4, Enumerable.Range(0, 16).Count(_ => Permit(gate)));
        Assert.Equal(4, Enumerable.Range(0, 16).Count(_ => Permit(gate)));
        Assert.Equal(24UL, gate.GrantCount); Assert.Equal(48UL, gate.InspectionCount);
        Assert.Equal(72UL, gate.Spent.Value); Assert.Equal(184UL, gate.Attention.Value);
        Assert.Equal(2, gate.RetestEpisodes); Assert.Equal(24, gate.SuppressedRequests);
        Assert.True(gate.ErrorRequestedLearning); Assert.False(gate.RequestedLearning);
        Assert.Equal(0UL, gate.Progress.ObservationCount); Conserved(fixture);
    }

    [Fact]
    public void PassiveObserverDoesNotChangeTheErrorOnlyPermissionContract()
    {
        var baseline = Funded(progressGate: false); var throttled = Funded();
        Assert.Equal(32, Enumerable.Range(0, 32).Count(_ => Permit(baseline.Gate)));
        Assert.Equal(20, Enumerable.Range(0, 32).Count(_ => Permit(throttled.Gate)));
        Assert.Equal(64UL, baseline.Gate.Spent.Value); Assert.Equal(52UL, throttled.Gate.Spent.Value);
        Assert.NotNull(baseline.Gate.Progress); Assert.False(baseline.Gate.ProgressGated);
        Assert.Equal(20, NativeGraph.Parts(baseline.Gate.Root).Length); Conserved(baseline); Conserved(throttled);
    }

    [Fact]
    public void OneCompletedImprovementCannotContinuallyReopenTheSameEpisode()
    {
        var fixture = Funded(); var gate = fixture.Gate; ExhaustBootstrap(gate);
        Observe(gate, 16, wrong: true); Observe(gate, 16, wrong: false);
        Assert.Equal(1UL, gate.Progress!.Version);
        Assert.True(NativeGraph.TryValue(gate.Progress.ReadProgress(), gate.Progress.ProgressAxis, out var gain)); Assert.Equal(512, gain.Ticks);
        Assert.Equal(4, Enumerable.Range(0, 16).Count(_ => Permit(gate)));
        Assert.Equal(1, gate.PositiveEpisodes); Assert.Equal(0, gate.RetestEpisodes); Assert.Equal(0, gate.EpisodeRemaining);
        Assert.Equal(4, Enumerable.Range(0, 16).Count(_ => Permit(gate)));
        Assert.Equal(1, gate.PositiveEpisodes); Assert.Equal(1, gate.RetestEpisodes);
        Assert.Equal(1UL, gate.Progress.Version); Conserved(fixture);
    }

    [Fact]
    public void MonitoringContinuesWhileLearningIsPausedAndUnknownDoesNotBecomeErrorZero()
    {
        var fixture = Funded(useProgress: false); var gate = fixture.Gate; ExhaustBootstrap(gate);
        for (var index = 0; index < 4; index++) Assert.True(Permit(gate));
        Assert.False(Permit(gate)); var grants = gate.GrantCount;
        for (var index = 0; index < 32; index++)
            gate.ObservePrediction(NativeGraph.Open(new Placeholder(), new Placeholder()), P(gate.ResourceAxis, 1));
        Assert.Equal(grants, gate.GrantCount); Assert.Equal(0UL, gate.EvidenceCount);
        Assert.Equal(32UL, gate.UnknownObservationCount); Assert.Equal(32UL, gate.Progress!.ObservationCount);
        Assert.Equal(1UL, gate.Progress.Version); AssertOpen(gate.ReadError()); AssertOpen(gate.Progress.ReadProgress());
        Assert.True(NativeGraph.TryValue(gate.Progress.ReadCoverageChange(), gate.Progress.CoverageAxis, out var coverage)); Assert.Equal(0, coverage.Ticks);
        Assert.False(Permit(gate)); Conserved(fixture);
    }

    [Fact]
    public void NewUnsupportedBlockClearsAnOlderImprovementWithoutReusingItsVersion()
    {
        var fixture = Funded(); var gate = fixture.Gate; ExhaustBootstrap(gate);
        Observe(gate, 16, wrong: true); Observe(gate, 16, wrong: false);
        Assert.True(NativeGraph.TryValue(gate.Progress!.ReadProgress(), gate.Progress.ProgressAxis, out var old)); Assert.Equal(512, old.Ticks);
        for (var index = 0; index < 16; index++)
            gate.ObservePrediction(NativeGraph.Open(new Placeholder(), new Placeholder()), P(gate.ResourceAxis, 1));
        Assert.Equal(2UL, gate.Progress.Version); AssertOpen(gate.Progress.ReadProgress());
        Assert.Equal(4, Enumerable.Range(0, 16).Count(_ => Permit(gate)));
        Assert.Equal(0, gate.PositiveEpisodes); Assert.Equal(0, gate.CoverageEpisodes); Assert.Equal(1, gate.RetestEpisodes);
        Conserved(fixture);
    }

    [Fact]
    public void IncreasingSupportedCoverageCanFundASeparateEpisodeWhileErrorProgressIsOpen()
    {
        var fixture = Funded(); var gate = fixture.Gate; ExhaustBootstrap(gate);
        for (var index = 0; index < 16; index++)
            gate.ObservePrediction(NativeGraph.Open(new Placeholder(), new Placeholder()), P(gate.ResourceAxis, 1));
        Observe(gate, 16, wrong: true);
        AssertOpen(gate.Progress!.ReadProgress());
        Assert.True(NativeGraph.TryValue(gate.Progress.ReadCoverageChange(), gate.Progress.CoverageAxis, out var coverage)); Assert.Equal(256, coverage.Ticks);
        Assert.Equal(4, Enumerable.Range(0, 16).Count(_ => Permit(gate)));
        Assert.Equal(1, gate.CoverageEpisodes); Assert.Equal(0, gate.PositiveEpisodes); Assert.Equal(0, gate.RetestEpisodes);
        AssertOpen(gate.Progress.ReadProgress()); Conserved(fixture);
    }

    [Fact]
    public void SignedProgressAndCyclesDifferOnlyWhenANewImprovementCanSupportAnEpisode()
    {
        var signed = Funded(); var cycles = Funded(useProgress: false);
        foreach (var gate in new[] { signed.Gate, cycles.Gate })
        {
            ExhaustBootstrap(gate); for (var index = 0; index < 4; index++) Assert.True(Permit(gate));
            Observe(gate, 16, wrong: true); Observe(gate, 16, wrong: false);
        }
        Assert.True(Permit(signed.Gate)); Assert.False(Permit(cycles.Gate));
        Assert.Equal(1, signed.Gate.PositiveEpisodes); Assert.Equal(0, cycles.Gate.PositiveEpisodes);
        Assert.Equal(1, signed.Gate.RetestEpisodes); Assert.Equal(1, cycles.Gate.RetestEpisodes);
        Assert.Equal(1UL, signed.Gate.Progress!.Version); Assert.Equal(1UL, cycles.Gate.Progress!.Version);
        Conserved(signed); Conserved(cycles);
    }

    [Fact]
    public void ChangeInterestCanAdmitWorseningWhileSignedImprovementDoesNot()
    {
        var signed = Funded(); var change = Funded(changeInterest: true);
        foreach (var gate in new[] { signed.Gate, change.Gate })
        {
            ExhaustBootstrap(gate); for (var index = 0; index < 4; index++) Assert.True(Permit(gate));
            Observe(gate, 16, wrong: false); Observe(gate, 16, wrong: true);
        }
        Assert.True(NativeGraph.TryValue(change.Gate.Progress!.ReadProgress(), change.Gate.Progress.ProgressAxis, out var deterioration)); Assert.Equal(-512, deterioration.Ticks);
        Assert.False(Permit(signed.Gate)); Assert.True(Permit(change.Gate));
        Assert.Equal(0, signed.Gate.ChangeEpisodes); Assert.Equal(1, change.Gate.ChangeEpisodes);
        Assert.Equal(3, change.Gate.EpisodeRemaining); Conserved(signed); Conserved(change);
    }

    [Fact]
    public void UnfundedCallsDoNotConsumeBootstrapOrAdvanceThePaidRetestCycle()
    {
        var fixture = Funded(attention: 1); var gate = fixture.Gate;
        Assert.False(Permit(gate)); Assert.Equal(1UL, gate.Spent.Value); Assert.Equal(1UL, gate.InspectionCount); Assert.Equal(0UL, gate.GrantCount);
        var extension = NativeGraph.Parts((Relation)NativeGraph.Parts(gate.Root, 20)[19], 12);
        var paidPhase = Assert.IsType<Carrier>(extension[6]); Assert.Equal(1UL, paidPhase.Value);
        for (var index = 0; index < 20; index++) Assert.False(Permit(gate));
        Assert.Equal(1UL, paidPhase.Value); Assert.Equal(1UL, gate.InspectionCount); Assert.Equal(0UL, gate.GrantCount);
        fixture.Economy.Sponsor(fixture.Service, 2); Assert.True(fixture.Economy.PayWork(fixture.Service, gate.Attention, 2, out _));
        Assert.True(Permit(gate)); Assert.Equal(1UL, gate.GrantCount); Assert.Equal(3UL, gate.Spent.Value);
        Assert.Equal(2UL, paidPhase.Value); Conserved(fixture);
    }

    [Fact]
    public void CoupledTransportPreservesProgressVersionsEpisodesMoneyAndSubsequentChoices()
    {
        var fixture = Funded(); var gate = fixture.Gate; ExhaustBootstrap(gate);
        Observe(gate, 16, wrong: true); Observe(gate, 16, wrong: false); Assert.True(Permit(gate)); Assert.Equal(3, gate.EpisodeRemaining);
        fixture.Economy.UpdateBody(fixture.Service, gate.Root, gate.LastDecision);
        var root = NativeGraph.G(fixture.Economy.Root, gate.Root);
        var decoded = (Relation)GojoNumericFormat.Decode(GojoNumericFormat.Encode(root))[0];
        Assert.Equal(NativeInspection.Signature(root), NativeInspection.Signature(decoded));
        var parts = NativeGraph.Parts(decoded, 2); var restoredEconomy = ServiceEconomy.FromRoot((Relation)parts[0]);
        var restored = LearningGate.FromRoot((Relation)parts[1]);
        var advertised = LearningGate.FromRoot(restoredEconomy.Services[0].Body);
        Assert.Same(restored.Attention, advertised.Attention); Assert.Same(restored.Spent, advertised.Spent);
        Assert.Same(restored.Progress!.VersionSite, advertised.Progress!.VersionSite);
        Assert.Equal(gate.Progress!.Version, restored.Progress.Version); Assert.Equal(gate.EpisodeRemaining, restored.EpisodeRemaining);
        for (var index = 0; index < 24; index++) Assert.Equal(Permit(gate), Permit(restored));
        Assert.Equal(gate.GrantCount, restored.GrantCount); Assert.Equal(gate.Spent.Value, restored.Spent.Value);
        Assert.Equal(gate.PositiveEpisodes, restored.PositiveEpisodes); Assert.Equal(gate.RetestEpisodes, restored.RetestEpisodes);
        Assert.Equal(restoredEconomy.ExternalNet, restoredEconomy.AccountedMoney); Conserved(fixture);
    }

    private sealed record Fixture(ServiceEconomy Economy, EconomyService Service, LearningGate Gate);
    private static Fixture Funded(bool progressGate = true, bool useProgress = true, bool changeInterest = false, long attention = 256)
    {
        var resource = new Carrier(); var source = new Carrier(); var economy = new ServiceEconomy(resource, new Carrier(), 1000);
        var gate = LearningGate.Create(resource, 0, opportunity: false, progressSource: source,
            progressGate: progressGate, useProgress: useProgress, changeInterest: changeInterest, retestPeriod: 16);
        Assert.True(economy.AddService(gate.Root, 0, 0, ServiceEconomy.UpfrontLaw(resource), out var service, out _, 0, 0));
        economy.Sponsor(service!, attention); economy.RegisterWorkSink(gate.Spent);
        Assert.True(economy.PayWork(service!, gate.Attention, attention, out _)); return new(economy, service!, gate);
    }
    private static void ExhaustBootstrap(LearningGate gate)
    { for (var index = 0; index < 16; index++) Assert.True(Permit(gate)); Assert.Equal(16UL, gate.GrantCount); }
    private static void Observe(LearningGate gate, int count, bool wrong)
    { for (var index = 0; index < count; index++) gate.ObservePrediction(P(gate.ResourceAxis, 1), P(gate.ResourceAxis, wrong ? -1 : 1)); }
    private static bool Permit(LearningGate gate)
    { Assert.True(gate.TryPermission(gate.Decide(P(gate.TrafficAxis, 1), P(gate.DemandAxis, 1)), out var granted)); return granted; }
    private static Relation P(Carrier axis, long ticks) => NativeGraph.Publication(axis, ticks, new Carrier());
    private static void Conserved(Fixture fixture) => Assert.Equal(fixture.Economy.ExternalNet, fixture.Economy.AccountedMoney);
    private static void AssertOpen(Relation publication)
    { Assert.True(ReadWitness.TryRead(publication, out _, out _, out var result, out _, out _)); Assert.IsType<Placeholder>(result); }
}
