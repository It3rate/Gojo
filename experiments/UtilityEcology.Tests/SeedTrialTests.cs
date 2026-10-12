using CompositeEmergence;
using Gojo.Core;
using Gojo.Structures;
using PrimitiveLearning;

namespace UtilityEcology.Tests;

public sealed class SeedTrialTests
{
    [Theory]
    [InlineData(8, -1, -12)]
    [InlineData(-12, -1, 8)]
    [InlineData(16, -1, -24)]
    [InlineData(-24, -1, 16)]
    [InlineData(8, 1, 8)]
    public void OpposedPayoutProjectionUsesTheAuthoredRatioWithoutCreatingMoney(long gross, long advice, long expected)
    {
        var f = new Fixture(); var trial = f.DirectTrial(); var command = f.Command(1);
        var physical = WorldPublication.Value(f.World.ResourceAxis, gross, new Carrier(), -24, 16);
        var bound = NativeGraph.BindFeedback(command, physical, f.World.ResourceAxis);
        var before = f.Economy.AccountedMoney;
        var projected = trial.ProjectedGross(WorldPublication.Value(f.Provider.Base.AdviceAxis, advice, new Carrier()), command, bound);
        Assert.Equal(expected, Value(projected, f.World.ResourceAxis));
        Assert.Equal(before, f.Economy.AccountedMoney); Assert.Equal(f.Economy.ExternalNet, before);
        Assert.Equal(gross, Value(trial.ProjectedGross(NativeGraph.Open(new Carrier(), f.Provider.Base.AdviceAxis), command, bound), f.World.ResourceAxis));
        Assert.False(NativeGraph.IsBoundTo(projected, command, f.World.ResourceAxis));
    }

    [Fact]
    public void TrialStartsUnknownAndWrongBindingPresenceOrDuplicateCannotAdvanceIt()
    {
        var f = new Fixture(); var trial = f.DirectTrial(); var e = f.Event(0, 0, -1, 1);
        Assert.Equal(0, trial.Candidate.LandmarkCount);
        Assert.False(AdaptiveOrdinal.TryReadPrediction(trial.Candidate.Predict(e.Sample, f.World.Compare), out _));
        var other = f.Command(1); var wrong = NativeGraph.BindFeedback(other, e.Physical, f.World.ResourceAxis);
        trial.Observe(e.Sample, e.Command, wrong, f.ComparePaid, f.Economy);
        trial.Observe(SamplingSurface.Sample(new Carrier(), f.World.Sources[0], new Carrier(), true), e.Command, e.Bound, f.ComparePaid, f.Economy);
        var before = trial.WorkSpent; Assert.Equal(0, trial.TrainingCount);
        trial.Observe(e.Sample, e.Command, e.Bound, f.ComparePaid, f.Economy);
        Assert.Equal(1, trial.TrainingCount); Assert.True(trial.WorkSpent > before);
        before = trial.WorkSpent; trial.Observe(e.Sample, e.Command, e.Bound, f.ComparePaid, f.Economy);
        Assert.Equal(1, trial.TrainingCount); Assert.Equal(before, trial.WorkSpent);
        var hold = f.Event(1, 1, -1, 0); trial.Observe(hold.Sample, hold.Command, hold.Bound, f.ComparePaid, f.Economy);
        Assert.Equal(1, trial.TrainingCount); Assert.Equal(f.Economy.ExternalNet, f.Economy.AccountedMoney);
    }

    [Fact]
    public void ValidationRequiresASeparatePreExecutionPairAndFrozenHoldoutEvidence()
    {
        var f = new Fixture(); var trial = f.DirectTrial(); Train(f, trial);
        var support = Support(trial.Candidate); var e = f.Event(100, 0, -1, 1);
        var before = trial.WorkSpent; trial.Observe(e.Sample, e.Command, e.Bound, f.ComparePaid, f.Economy);
        Assert.Equal(0, trial.ValidationCount); Assert.Equal(before, trial.WorkSpent);
        trial.PrepareValidation(e.Sample, e.Command, f.UnknownIncumbent, f.ComparePaid, f.Economy);
        Assert.NotNull(trial.CandidateValidationAdvice); Assert.Null(trial.IncumbentValidationAdvice);
        var work = trial.WorkSpent;
        // Repeated preparation is the same paid pair, not a fresh query.
        trial.PrepareValidation(e.Sample, e.Command, f.UnknownIncumbent, f.ComparePaid, f.Economy);
        Assert.Equal(work, trial.WorkSpent);
        var different = f.Event(101, 0, -1, 1); trial.Observe(different.Sample, different.Command, different.Bound, f.ComparePaid, f.Economy);
        Assert.Equal(0, trial.ValidationCount);
        trial.Observe(e.Sample, e.Command, e.Bound, f.ComparePaid, f.Economy);
        Assert.Equal(1, trial.ValidationCount); Assert.Equal(support, Support(trial.Candidate));
        var row = trial.Records.Last(); var fields = NativeGraph.Parts(row, 6);
        Assert.Same(e.Sample, fields[0]); Assert.Same(e.Command, fields[1]); Assert.Same(e.Bound, fields[2]);
        Assert.Equal(f.Economy.ExternalNet, f.Economy.AccountedMoney);
    }

    [Fact]
    public void AUsefulCandidateIsAdmittedOnlyAfterThePaidCommonHoldoutExceedsItsWholeCost()
    {
        var f = new Fixture(); var trial = f.DirectTrial(); Train(f, trial); var before = Support(trial.Candidate);
        for (var i = 0; i < 32; i++)
        {
            var raw = i % 2 == 0 ? 0 : 5; var desired = raw == 0 ? -1 : 1;
            var e = f.Event(200 + i, raw, desired, 1);
            trial.PrepareValidation(e.Sample, e.Command, f.UnknownIncumbent, f.ComparePaid, f.Economy);
            trial.Observe(e.Sample, e.Command, e.Bound, f.ComparePaid, f.Economy);
            if (i < 31) Assert.False(trial.Closed);
        }
        Assert.True(trial.Accepted); Assert.Equal(32, trial.ValidationCount); Assert.Equal(32, trial.SupportedCount);
        Assert.Equal(320, trial.Gain); Assert.True(trial.NetGain > 0); Assert.True(trial.WorkSpent > 16);
        Assert.Equal(before, Support(trial.Candidate)); Assert.Equal(48, trial.Records.Count);
        var trainingSamples = trial.Records.Take(16).Select(row => NativeGraph.Parts(row, 6)[0]).ToArray();
        Assert.All(trial.Records.Skip(16), row => Assert.DoesNotContain(NativeGraph.Parts(row, 6)[0], trainingSamples));
        Assert.Equal(1, Selection(trial.ReplayAdmission()));
        var decision = trial.LastDecision; var purse = trial.Purse.Value; var count = trial.ValidationCount;
        trial.ReplayAdmission(); Assert.Same(decision, trial.LastDecision); Assert.Equal(purse, trial.Purse.Value); Assert.Equal(count, trial.ValidationCount);
    }

    [Fact]
    public void ATrainingSampleCannotBeRecycledIntoTheHoldout()
    {
        var f = new Fixture(); var trial = f.DirectTrial(); Train(f, trial);
        var trainingRow = NativeGraph.Parts(trial.Records.First(), 6);
        var sample = (Relation)trainingRow[0]; var command = (Relation)trainingRow[1]; var bound = (Relation)trainingRow[2];
        trial.PrepareValidation(sample, command, f.UnknownIncumbent, f.ComparePaid, f.Economy);
        var preparedCost = trial.WorkSpent;
        trial.Observe(sample, command, bound, f.ComparePaid, f.Economy);
        Assert.Equal(0, trial.ValidationCount); Assert.Equal(16, trial.Records.Count); Assert.Equal(preparedCost, trial.WorkSpent);
        var fresh = f.Event(100, 0, -1, 1);
        trial.PrepareValidation(fresh.Sample, fresh.Command, f.UnknownIncumbent, f.ComparePaid, f.Economy);
        trial.Observe(fresh.Sample, fresh.Command, fresh.Bound, f.ComparePaid, f.Economy);
        Assert.Equal(1, trial.ValidationCount); Assert.Same(fresh.Sample, NativeGraph.Parts(trial.Records.Last(), 6)[0]);
        Assert.Equal(f.Economy.ExternalNet, f.Economy.AccountedMoney);
    }

    [Fact]
    public void AnUnknownCandidateCannotEarnGainFromAnIncumbentReadThatDiffersFromTheExecutedCommand()
    {
        var f = new Fixture(); var trial = f.DirectTrial(); Train(f, trial);
        var encounter = new EcologyTrial(100, 1, 0, 0, 1, 1); var sample = f.World.Request(encounter); var command = f.Command(1);
        Relation UnresolvedPaidComparison(Carrier purse, Relation a, Relation b)
        {
            Assert.True(f.Economy.BurnWork(purse, 1, out _)); return NativeGraph.Open(NativeGraph.G(a, b), f.World.OrderAxes[0]);
        }
        trial.PrepareValidation(sample, command,
            (_, _) => WorldPublication.Value(f.Provider.Base.AdviceAxis, -1, new Carrier()), UnresolvedPaidComparison, f.Economy);
        Assert.Null(trial.CandidateValidationAdvice); Assert.Equal(-1, trial.IncumbentValidationAdvice);
        // The physical receipt is obtained only after the two answers are fixed.
        var physical = f.World.Execute(encounter, command); var bound = NativeGraph.BindFeedback(command, physical, f.World.ResourceAxis);
        trial.Observe(sample, command, bound, (_, _, _) => throw new InvalidOperationException("No query may occur after the outcome."), f.Economy);
        Assert.Equal(1, trial.ValidationCount); Assert.Equal(0, trial.SupportedCount); Assert.Equal(0, trial.Gain);
        var row = NativeGraph.Parts(trial.Records.Last(), 6); var scores = NativeGraph.Parts((Relation)row[5], 6);
        Assert.Same(scores[2], scores[3]); Assert.Equal(-12, Value((Relation)scores[2], f.World.ResourceAxis));
        Assert.Equal(f.Economy.ExternalNet, f.Economy.AccountedMoney);
    }

    [Fact]
    public void RejectingAFeeFundedReplacementKeepsTheFrozenIncumbentsSupportAndAnchors()
    {
        var f = new Fixture(); Warm(f, 16, purchase: false);
        Assert.True(f.Provider.TryLaunch(f.Economy, f.Service, out _));
        for (var i = 16; i < 64; i++) f.ProviderEvent(i, i % 2 == 0 ? 0 : 5, i % 2 == 0 ? -1 : 1);
        Assert.Equal(1, f.Provider.Accepted); var incumbent = Assert.IsType<AdaptiveOrdinal>(f.Provider.Current);
        for (var i = 64; i < 224; i++) f.ProviderEvent(i, i % 2 == 0 ? 0 : 5, i % 2 == 0 ? -1 : 1, purchase: true);
        Assert.True(f.Provider.TryLaunch(f.Economy, f.Service, out _));
        for (var i = 224; i < 240; i++) f.ProviderEvent(i, i % 2 == 0 ? 0 : 5, i % 2 == 0 ? -1 : 1, permission: true);
        Assert.True(f.Provider.FreezeIncumbent); var support = Support(incumbent);
        Assert.True(AdaptiveOrdinal.TryReadModel(incumbent.Root, out _, out var anchorGroup, out _));
        var anchors = NativeGraph.Parts(anchorGroup).Cast<Relation>().ToArray();
        var answers = anchors.Select(row =>
        {
            Assert.True(AdaptiveOrdinal.TryReadLandmark(row, out var sample, out _, out _));
            Assert.True(AdaptiveOrdinal.TryReadPrediction(incumbent.Predict(sample, (a, b) => f.ComparePaid(f.Economy.Buyer, a, b)), out var answer));
            return answer.Ticks;
        }).ToArray();
        for (var i = 240; i < 272; i++) f.ProviderEvent(i, i % 2 == 0 ? 0 : 5, i % 2 == 0 ? -1 : 1, permission: true);
        Assert.Equal(1, f.Provider.Rejected); Assert.Same(incumbent, f.Provider.Current); Assert.Equal(support, Support(incumbent));
        Assert.True(AdaptiveOrdinal.TryReadModel(incumbent.Root, out _, out var afterGroup, out _));
        var after = NativeGraph.Parts(afterGroup).Cast<Relation>().ToArray(); Assert.Equal(anchors.Length, after.Length);
        for (var i = 0; i < anchors.Length; i++)
        {
            Assert.Same(anchors[i], after[i]); Assert.True(AdaptiveOrdinal.TryReadLandmark(after[i], out var sample, out _, out _));
            Assert.True(AdaptiveOrdinal.TryReadPrediction(incumbent.Predict(sample, (a, b) => f.ComparePaid(f.Economy.Buyer, a, b)), out var answer));
            Assert.Equal(answers[i], answer.Ticks);
        }
        Assert.Equal(f.Economy.ExternalNet, f.Economy.AccountedMoney);
    }

    [Fact]
    public void FilteredCandidateWithNoIncrementalGainIsRejectedButWithdrawalStillAdmitsAfterMatchedWork()
    {
        foreach (var filtered in new[] { true, false })
        {
            var f = new Fixture(); var trial = f.DirectTrial(filtered); Train(f, trial);
            for (var i = 0; i < 32; i++)
            {
                var raw = i % 2 == 0 ? 0 : 5; var desired = raw == 0 ? -1 : 1;
                var e = f.Event(300 + i, raw, desired, desired);
                trial.PrepareValidation(e.Sample, e.Command, f.UnknownIncumbent, f.ComparePaid, f.Economy);
                trial.Observe(e.Sample, e.Command, e.Bound, f.ComparePaid, f.Economy);
            }
            Assert.True(trial.Closed); Assert.Equal(!filtered, trial.Accepted); Assert.Equal(0, trial.Gain);
            Assert.True(trial.NetGain < 0); Assert.Equal(filtered ? 0 : 1, Selection(trial.ReplayAdmission()));
            Assert.Equal(f.Economy.ExternalNet, f.Economy.AccountedMoney);
        }
    }

    [Fact]
    public void ExhaustedPurseClosesWithoutLoweringTheValidationOrCoverageRequirement()
    {
        var f = new Fixture(seed: 16); var trial = f.DirectTrial(reserve: 16);
        var e = f.Event(0, 0, -1, 1); trial.Observe(e.Sample, e.Command, e.Bound, f.ComparePaid, f.Economy);
        Assert.True(trial.Closed); Assert.False(trial.Accepted); Assert.Equal(0, trial.TrainingCount);
        Assert.Equal(0, trial.ValidationCount); Assert.Equal(32, trial.ValidationEvents); Assert.Equal(16, trial.WorkSpent);
        Assert.Equal(0, Selection(trial.ReplayAdmission())); Assert.Equal(f.Economy.ExternalNet, f.Economy.AccountedMoney);
    }

    [Fact]
    public void ZeroSeedCanLaunchWithEarnedFeesAndNoSponsorRefill()
    {
        var f = new Fixture(seed: 0); Warm(f, 16, purchase: false);
        Assert.False(f.Provider.TryLaunch(f.Economy, f.Service, out _)); Assert.Equal(0, f.Provider.Trials);
        for (var i = 16; i < 176; i++) f.ProviderEvent(i, i % 6, i % 6 < 3 ? -1 : 1, purchase: true);
        Assert.Equal(320UL, f.Service.Wallet.Value);
        Assert.True(f.Provider.TryLaunch(f.Economy, f.Service, out _));
        Assert.Equal(1, f.Provider.Trials); Assert.Equal(0, f.Provider.SeedSpent); Assert.Equal(16, f.Provider.EarnedSpent);
        Assert.Equal(0UL, f.Provider.SeedEnergy.Value); Assert.Equal(0UL, f.Service.Wallet.Value);
        Assert.False(f.Provider.TrialHistory.Single().SeedFunded); Assert.Equal(f.Economy.ExternalNet, f.Economy.AccountedMoney);
    }

    [Fact]
    public void SeedIsNotReusedAndAttemptCapPreventsPerpetualTrials()
    {
        var f = new Fixture(maxAttempts: 1); Warm(f, 16, purchase: false);
        Assert.True(f.Provider.TryLaunch(f.Economy, f.Service, out _));
        for (var i = 16; i < 64; i++) f.ProviderEvent(i, i % 2 == 0 ? 0 : 5, i % 2 == 0 ? -1 : 1);
        Assert.Equal(1, f.Provider.Accepted + f.Provider.Rejected); var remaining = f.Provider.SeedEnergy.Value;
        Assert.True(remaining > 0);
        for (var i = 64; i < 240; i++) f.ProviderEvent(i, 0, 1, purchase: true);
        Assert.True(f.Service.Wallet.Value >= 320); Assert.False(f.Provider.TryLaunch(f.Economy, f.Service, out _));
        Assert.Equal(1, f.Provider.Trials); Assert.Equal(remaining, f.Provider.SeedEnergy.Value);
        Assert.Equal(f.Economy.ExternalNet, f.Economy.AccountedMoney);
    }

    [Fact]
    public void NativeRoundtripRetainsPursesAliasesPendingPairAndAdmissionRule()
    {
        var f = new Fixture(); var trial = f.DirectTrial(); Train(f, trial);
        var e = f.Event(400, 0, -1, 1); trial.PrepareValidation(e.Sample, e.Command, f.UnknownIncumbent, f.ComparePaid, f.Economy);
        var root = NativeGraph.G(f.Economy.Root, trial.Root);
        var restored = (Relation)GojoNumericFormat.Decode(GojoNumericFormat.Encode(root)).Single(); var parts = NativeGraph.Parts(restored, 2);
        var economy = ServiceEconomy.FromRoot((Relation)parts[0]); var copy = SeedTrial.FromRoot((Relation)parts[1]);
        Assert.Equal(trial.WorkSpent, copy.WorkSpent); Assert.Equal(trial.TrainingCount, copy.TrainingCount);
        Assert.Equal(trial.CandidateValidationAdvice, copy.CandidateValidationAdvice);
        Assert.Equal(0, Selection(copy.ReplayAdmission()));
        var pending = NativeGraph.Parts(copy.Pending, 6); var sample = (Relation)pending[0]; var command = (Relation)pending[1];
        var bound = NativeGraph.BindFeedback(command, WorldPublication.Value(copy.ResourceAxis, -12, new Carrier(), -24, 16), copy.ResourceAxis);
        copy.Observe(sample, command, bound, (_, _, _) => throw new InvalidOperationException("A prepared validation must not query after feedback."), economy);
        Assert.Equal(1, copy.ValidationCount); Assert.Equal(20, copy.Gain);
        Assert.Equal(economy.ExternalNet, economy.AccountedMoney);
    }

    private static void Train(Fixture f, SeedTrial trial)
    {
        for (var i = 0; i < 16; i++)
        {
            var raw = i % 2 == 0 ? 0 : 5; var desired = raw == 0 ? -1 : 1;
            var e = f.Event(i, raw, desired, 1); trial.Observe(e.Sample, e.Command, e.Bound, f.ComparePaid, f.Economy);
        }
        Assert.Equal(16, trial.TrainingCount); Assert.True(trial.Validating);
        Assert.True(trial.Candidate.LandmarkCount > 0);
    }
    private static void Warm(Fixture f, int count, bool purchase) { for (var i = 0; i < count; i++) f.ProviderEvent(i, i % 6, i % 6 < 3 ? -1 : 1, purchase); }
    private static long Value(Relation read, Carrier axis) { Assert.True(NativeGraph.TryValue(read, axis, out var value)); return value.Ticks; }
    private static long Selection(Relation read)
    {
        Assert.True(SupportDecisionSurface.TryReadSelection(read, out _, out var response, out _, out _));
        Assert.IsType<CarrierLocation>(response); return ((CarrierLocation)response).Ticks;
    }
    private static string Support(AdaptiveOrdinal model)
    {
        Assert.True(AdaptiveOrdinal.TryReadModel(model.Root, out _, out var rows, out _));
        return string.Join(";", NativeGraph.Parts(rows).Cast<Relation>().Select(row =>
        { Assert.True(AdaptiveOrdinal.TryReadLandmark(row, out var sample, out var zero, out var one)); return $"{sample.GetHashCode()}:{zero.Value}:{one.Value}"; }));
    }
    private sealed class Fixture
    {
        public EcologyWorld World { get; } = new();
        public ServiceEconomy Economy { get; }
        public SeededProvider Provider { get; }
        public EconomyService Service { get; }
        public Fixture(long seed = 320, int maxAttempts = 8)
        {
            Economy = new(World.ResourceAxis, World.TimeAxis, 16384, 4);
            Provider = new(World.Sources[0], World.PresenceAxes[0], World.OrderAxes[0], World.ActionAxis, World.ResourceAxis, seedBudget: seed == 16 ? 16 : 320, maxAttempts: maxAttempts);
            Assert.True(Economy.AddService(Provider.Root, 2, 8, ServiceEconomy.UpfrontLaw(World.ResourceAxis), out var service, out _)); Service = service!;
            Economy.Sponsor(Service, seed + 64); Economy.RegisterWorkSink(Provider.Base.Gate.Spent);
            Assert.True(Economy.PayWork(Service, Provider.Base.Gate.Attention, 64, out _));
            if (seed != 0) { Assert.True(Economy.PayWork(Service, Provider.SeedEnergy, seed, out var funding)); Provider.RecordSeedFunding(funding); }
            Assert.Equal(0UL, Service.Wallet.Value);
        }
        public SeedTrial DirectTrial(bool comparison = true, long reserve = 320)
        {
            Assert.True(Economy.BurnWork(Provider.SeedEnergy, 16, out _));
            return new(new AdaptiveOrdinal(World.Sources[0], World.PresenceAxes[0], World.OrderAxes[0], Provider.Base.LabelAxis, 4),
                World.Sources[0], Provider.SeedEnergy, World.ResourceAxis, World.ActionAxis, Provider.Base.AdviceAxis, Provider.Base.LabelAxis,
                1, true, reserve, compareBeforeAdmission: comparison);
        }
        public Relation Command(int action) => WorldPublication.Value(World.ActionAxis, action, new Carrier());
        public (Relation Sample, Relation Command, Relation Physical, Relation Bound) Event(int index, int raw, int desired, int action)
        {
            var trial = new EcologyTrial(index, 1, 0, raw, desired, 1); var sample = World.Request(trial); var command = Command(action);
            var physical = World.Execute(trial, command); return (sample, command, physical, NativeGraph.BindFeedback(command, physical, World.ResourceAxis));
        }
        public Relation ComparePaid(Carrier payer, Relation a, Relation b)
        {
            if (!Economy.BurnWork(payer, 1, out var paid)) return NativeGraph.Open(NativeGraph.G(a, b), World.OrderAxes[0]);
            var read = World.Compare(a, b); Assert.True(ReadWitness.TryRead(read, out var source, out var surface, out var result, out var residue, out var witness));
            return ReadWitness.Create(new Carrier(), source, surface, result, residue, NativeGraph.G(paid, witness));
        }
        public Relation UnknownIncumbent(Relation sample, Func<Relation, Relation, Relation> compare) => NativeGraph.Open(sample, Provider.Base.AdviceAxis);
        public void ProviderEvent(int index, int raw, int desired, bool purchase = false, bool permission = false)
        {
            var trial = new EcologyTrial(index, 1, 0, raw, desired, 1); var sample = World.Request(trial);
            var offered = Provider.Interpret(sample, (a, b) => ComparePaid(Economy.Buyer, a, b), index);
            var action = NativeGraph.TryValue(offered, Provider.Base.AdviceAxis, out var advice) && advice.Ticks is -1 or 1 ? (int)advice.Ticks : 1;
            var command = Command(action); EconomyPurchase? receipt = null;
            if (purchase) Assert.True(Economy.TryPurchase(Service, command, offered, 1, out receipt, out _));
            Provider.PrepareValidation(sample, command, ComparePaid, Economy, Service);
            var physical = World.Execute(trial, command); var bound = NativeGraph.BindFeedback(command, physical, World.ResourceAxis);
            if (receipt is not null) Economy.Settle(receipt, bound); else Economy.Grant(physical);
            Provider.Observe(sample, offered, command, bound, permission, ComparePaid, Economy, Service);
        }
    }
}
