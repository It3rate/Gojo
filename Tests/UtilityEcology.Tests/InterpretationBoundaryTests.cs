using CompositeEmergence;
using Gojo.Core;
using Gojo.Structures;
using PrimitiveLearning;

namespace UtilityEcology.Tests;

public sealed class InterpretationBoundaryTests
{
    [Theory]
    [InlineData(-1)]
    [InlineData(1)]
    public void OpposedSuccessfulAndFailedCommandsInferTheSameSupportedDirection(int desired)
    {
        var world = new EcologyWorld(); var successful = Provider(world); var failed = Provider(world);
        Assert.Null(successful.OrdinalModel); AssertOpen(successful.Interpret(Sample(world, 0, desired), world.Compare, 0));
        AssertOpen(failed.Interpret(Sample(world, 1, desired), world.Compare, 0));
        for (var index = 0; index < 4; index++)
        {
            var success = Observe(world, successful, index + 2, desired, desired);
            var failure = Observe(world, failed, index + 10, desired, -desired);
            Assert.Equal(desired, Direction(success, successful.AdviceAxis));
            Assert.Equal(desired, Direction(failure, failed.AdviceAxis));
        }
        Assert.Equal(4, successful.Updates); Assert.Equal(4, failed.Updates);
        Assert.Equal(desired, Direction(successful.Interpret(Sample(world, 20, desired), world.Compare, 20), successful.AdviceAxis));
        Assert.Equal(desired, Direction(failed.Interpret(Sample(world, 21, desired), world.Compare, 21), failed.AdviceAxis));
        Assert.Null(successful.OrdinalModel); Assert.Null(failed.OrdinalModel);
    }

    [Fact]
    public void APhysicalHoldCannotSupplyADirectionLabelAndWrongBindingCannotTrain()
    {
        var world = new EcologyWorld(); var provider = Provider(world);
        for (var index = 0; index < 8; index++) AssertOpen(Observe(world, provider, index, 1, 0));
        Assert.Equal(0, provider.Updates); Assert.Equal(0, provider.PresenceModel.RowCount);
        AssertOpen(provider.Interpret(Sample(world, 8, 1), world.Compare, 8));
        var sample = Sample(world, 9, 1); var offered = provider.Interpret(sample, world.Compare, 9);
        var command = Command(world, 1); var other = Command(world, 1);
        var physical = world.Execute(Trial(9, 1), command);
        var wrong = NativeGraph.BindFeedback(other, physical, world.ResourceAxis);
        var evidence = provider.Gate.EvidenceCount;
        AssertOpen(provider.Observe(sample, offered, command, wrong, true, world.Compare));
        AssertOpen(provider.Observe(sample, offered, command, physical, true, world.Compare));
        Assert.Equal(0, provider.Updates); Assert.Equal(evidence, provider.Gate.EvidenceCount);
    }

    [Fact]
    public void RefinementRequiresRequestsPressureLearningPermissionAndActualFunding()
    {
        var world = new EcologyWorld(); var economy = Economy(world); var provider = Provider(world); var service = Add(economy, provider);
        Assert.Null(provider.OrdinalModel); AssertOpen(service.QuoteUtility());
        for (var index = 0; index < 15; index++) Observe(world, provider, index, index % 2 == 0 ? -1 : 1, 1);
        Assert.True(provider.UnmetPressure >= 4); var before = economy.CostSink.Value;
        Assert.False(provider.TryRefine(economy, service, true, true, out var blocked)); AssertOpen(blocked);
        Assert.Equal(before, economy.CostSink.Value); Assert.Null(provider.OrdinalModel);
        Observe(world, provider, 15, 1, 1);
        Assert.False(provider.TryRefine(economy, service, true, false, out blocked)); AssertOpen(blocked);
        Assert.True(economy.BurnWork(economy.Buyer, checked((long)economy.Buyer.Value - 15), out _));
        before = economy.CostSink.Value;
        Assert.False(provider.TryRefine(economy, service, true, true, out blocked)); AssertOpen(blocked);
        Assert.Equal(before, economy.CostSink.Value); Assert.Null(provider.OrdinalModel);
        economy.Grant(WorldPublication.Value(world.ResourceAxis, 2, new Carrier()));
        Assert.True(provider.TryRefine(economy, service, true, true, out var proposal));
        Assert.Equal(before + 16, economy.CostSink.Value); Assert.Equal(1, provider.Proposals); Assert.Equal(0, provider.UnmetPressure);
        Assert.NotNull(provider.OrdinalModel); Assert.Equal(0, provider.Landmarks);
        Assert.True(AdaptiveOrdinal.TryReadModel(provider.OrdinalModel!.Root, out _, out var landmarks, out _));
        Assert.Empty(NativeGraph.Parts(landmarks));
        Assert.True(ReadWitness.TryRead(proposal, out _, out var admission, out var constructed, out var residue, out _));
        Assert.Same(provider.OrdinalModel.Root, constructed); Assert.IsType<Relation>(admission); Assert.IsType<Relation>(residue);
        Assert.Equal(economy.ExternalNet, economy.AccountedMoney);
    }

    [Fact]
    public void SupportedConstantSignalSettlesWithoutAnUnnecessaryOrdinalProposal()
    {
        var world = new EcologyWorld(); var economy = Economy(world); var provider = Provider(world); var service = Add(economy, provider);
        for (var index = 0; index < 32; index++) Observe(world, provider, index, 1, index % 2 == 0 ? 1 : -1);
        Assert.Equal(32, provider.Updates); Assert.True(provider.Requests >= 16); Assert.Equal(0, provider.UnmetPressure);
        Assert.Equal(1, Direction(provider.Interpret(Sample(world, 32, 1), world.Compare, 32), provider.AdviceAxis));
        Assert.True(NativeGraph.TryValue(provider.Gate.ReadError(), provider.Gate.MeanErrorAxis, out var error)); Assert.Equal(0, error.Ticks);
        var burned = economy.CostSink.Value;
        Assert.False(provider.TryRefine(economy, service, true, true, out var refused)); AssertOpen(refused);
        Assert.Null(provider.OrdinalModel); Assert.Equal(0, provider.Proposals); Assert.Equal(burned, economy.CostSink.Value);
    }

    [Fact]
    public void RetentionPreservesARareUsefulBodyButPaidIdleResetCreatesAnEmptyModel()
    {
        var world = new EcologyWorld(); var economy = Economy(world); var provider = Provider(world); var service = Add(economy, provider);
        for (var index = 0; index < 32; index++) Observe(world, provider, index, 1, 1);
        economy.UpdateBody(service, provider.Root, NativeGraph.G(provider.Root));
        var previous = provider.PresenceModel.Root; var reserve = service.Wallet.Value; var burned = economy.CostSink.Value;
        economy.Park(service); economy.Advance(1000);
        Assert.Equal(reserve, service.Wallet.Value); Assert.Equal(burned, economy.CostSink.Value);
        Assert.Same(previous, provider.PresenceModel.Root);
        Assert.Equal(1, Direction(provider.PresenceModel.Predict(WorldPublication.Value(provider.PresenceAxis, 1, new Carrier())), provider.AdviceAxis));
        Assert.False(provider.ResetAfterIdle(economy, service, provider.LastSeen + 63, 64, out var tooEarly)); AssertOpen(tooEarly);
        Assert.True(provider.ResetAfterIdle(economy, service, provider.LastSeen + 1000, 64, out var reset));
        economy.UpdateBody(service, provider.Root, reset);
        Assert.Equal(burned + 16, economy.CostSink.Value); Assert.NotSame(previous, provider.PresenceModel.Root);
        Assert.Equal(0, provider.PresenceModel.RowCount); Assert.Null(provider.OrdinalModel); Assert.Equal(0, provider.UnmetPressure);
        AssertOpen(provider.Interpret(Sample(world, 1001, 1), world.Compare, 1001));
        var advertised = InterpretationProvider.FromRoot(service.Body); Assert.Equal(0, advertised.PresenceModel.RowCount);
        Assert.Equal(economy.ExternalNet, economy.AccountedMoney);
    }

    [Fact]
    public void CoupledSavedGraphReplaysKnownOrdinalAnchorsAndContinuesPaidGateAccounting()
    {
        var world = new EcologyWorld(); var economy = Economy(world);
        var providers = Enumerable.Range(0, 4).Select(source => Provider(world, source)).ToArray();
        var services = providers.Select(provider => Add(economy, provider)).ToArray();
        var provider = providers[0];
        for (var index = 0; index < 16; index++) Observe(world, provider, index, index % 2 == 0 ? -1 : 1, 1);
        Assert.True(provider.TryRefine(economy, services[0], true, true, out _));
        for (var index = 16; index < 24; index++) Observe(world, provider, index, index % 2 == 0 ? -1 : 1, 1, PaidCompare);
        Assert.Equal(2, provider.Landmarks);
        foreach (var index in Enumerable.Range(0, 4)) economy.UpdateBody(services[index], providers[index].Root, NativeGraph.G());
        var root = NativeGraph.G(economy.Root, NativeGraph.G(providers.Select(p => (IElement)p.Root).ToArray()),
            NativeGraph.G(), NativeGraph.G(), NativeGraph.G(), NativeGraph.G(), NativeGraph.G(), NativeGraph.G(), NativeGraph.G());
        var decoded = (Relation)GojoNumericFormat.Decode(GojoNumericFormat.Encode(root))[0];
        Assert.Equal(NativeInspection.Signature(root), NativeInspection.Signature(decoded));
        var replay = ArchiveReplay.Check(root, decoded);
        Assert.True(replay.Matched); Assert.True(replay.ContinuedMoneyConserved); Assert.Equal(5, replay.SamplesReplayed);
        Assert.InRange(replay.AttentionSpent, 4, 8);

        // A separate fresh restoration verifies actual known-anchor meaning,
        // rather than only agreement between two possibly incorrect replays.
        var fresh = (Relation)GojoNumericFormat.Decode(GojoNumericFormat.Encode(root))[0];
        var restored = InterpretationProvider.FromRoot((Relation)NativeGraph.Parts((Relation)NativeGraph.Parts(fresh, 9)[1], 4)[0]);
        Assert.True(AdaptiveOrdinal.TryReadModel(restored.OrdinalModel!.Root, out _, out var landmarks, out _));
        var rows = NativeGraph.Parts(landmarks).Cast<Relation>().ToArray();
        var anchors = rows.Select(row => { AdaptiveOrdinal.TryReadLandmark(row, out var sample, out _, out _); return sample; }).ToArray();
        foreach (var row in rows)
        {
            Assert.True(AdaptiveOrdinal.TryReadLandmark(row, out var sample, out var zero, out var one));
            Assert.True((zero.Value > 0) != (one.Value > 0));
            var expected = zero.Value > 0 ? -1 : 1;
            var publication = restored.Interpret(sample, OrderedComparison, restored.LastSeen + 1);
            Assert.Equal(expected, Direction(publication, restored.AdviceAxis));
        }

        Relation OrderedComparison(Relation candidate, Relation anchor) => SamplingSurface.Comparison(new Carrier(), candidate, anchor,
            restored.OrderAxis, Array.IndexOf(anchors, candidate).CompareTo(Array.IndexOf(anchors, anchor)));
        Relation PaidCompare(Relation candidate, Relation anchor)
        {
            Assert.True(economy.BurnWork(economy.Buyer, 1, out var payment)); var physical = world.Compare(candidate, anchor);
            Assert.True(ReadWitness.TryRead(physical, out var selected, out var basis, out var result, out var residue, out var provenance));
            return ReadWitness.Create(new Carrier(), selected, basis, result, residue, NativeGraph.G(payment, provenance));
        }
    }

    private static InterpretationProvider Provider(EcologyWorld world, int source = 0) => new(world.Sources[source], world.PresenceAxes[source],
        world.OrderAxes[source], world.ActionAxis, world.ResourceAxis, true, false);
    private static ServiceEconomy Economy(EcologyWorld world) => new(world.ResourceAxis, world.TimeAxis, 1000, 4);
    private static EconomyService Add(ServiceEconomy economy, InterpretationProvider provider)
    {
        Assert.True(economy.AddService(provider.Root, 1, 8, ServiceEconomy.UpfrontLaw(economy.ResourceAxis), out var service, out _, 1, 0));
        economy.Sponsor(service!, 64); economy.RegisterWorkSink(provider.Gate.Spent);
        Assert.True(economy.PayWork(service!, provider.Gate.Attention, 32, out _)); return service!;
    }
    private static Relation Observe(EcologyWorld world, InterpretationProvider provider, int index, int desired, int actual,
        Func<Relation, Relation, Relation>? compare = null)
    {
        compare ??= world.Compare; var sample = Sample(world, index, desired);
        var offered = provider.Interpret(sample, compare, index); var command = Command(world, actual);
        var physical = world.Execute(Trial(index, desired), command);
        return provider.Observe(sample, offered, command, NativeGraph.BindFeedback(command, physical, world.ResourceAxis), true, compare);
    }
    private static EcologyTrial Trial(int index, int desired) => new(index, 1, 0, desired < 0 ? 0 : 5, desired, 1);
    private static Relation Sample(EcologyWorld world, int index, int desired) => world.Request(Trial(index, desired));
    private static Relation Command(EcologyWorld world, long action) => WorldPublication.Value(world.ActionAxis, action, new Carrier());
    private static long Direction(Relation publication, Carrier axis)
    { Assert.True(NativeGraph.TryValue(publication, axis, out var point)); return point.Ticks; }
    private static void AssertOpen(Relation publication)
    { Assert.True(ReadWitness.TryRead(publication, out _, out _, out var result, out _, out _)); Assert.IsType<Placeholder>(result); }
}
