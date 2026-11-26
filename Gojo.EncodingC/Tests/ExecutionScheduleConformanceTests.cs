using Xunit;

namespace Gojo.EncodingC.Tests;

public sealed class ExecutionScheduleConformanceTests
{
    [Theory]
    [MemberData(nameof(ExecutionConformancePath.All), MemberType = typeof(ExecutionConformancePath), DisableDiscoveryEnumeration = true)]
    public void BothSchedulesMatchTheirOwnTraceAndQuietTick(ExecutionConformancePath path)
    {
        foreach (var together in new[] { false, true })
        {
            var (reference, _, _) = Chain();
            var (engine, sites, bit) = Chain();
            var expected = together ? new[] { 0b010, 0b001, 0b001 } : [0b001, 0b001];
            for (var tick = 0; tick < expected.Length; tick++)
            {
                var graph = together ? reference.StepTogether() : reference.Step();
                var actual = path.Step(engine, together);
                Assert.Equal(tick + 1, actual.Tick);
                Assert.Equal(expected[tick], Occupancy(sites, bit));
                Assert.Equal(tick == expected.Length - 1, actual.Steps.All(step => step.Quiet));
                Assert.Equal(graph.Steps.Select(step => step.Changed), actual.Steps.Select(step => step.Changed));
                Assert.Equal(Transport.Save(reference), Transport.Save(engine));
            }

            var (settled, _, _) = Chain();
            Assert.Equal(expected.Length, path.Settle(settled, 8, together));
            Assert.Equal(expected.Length, settled.Tick);
            Assert.Equal(Transport.Save(reference), Transport.Save(settled));
        }
    }

    [Theory]
    [MemberData(nameof(ExecutionConformancePath.All), MemberType = typeof(ExecutionConformancePath), DisableDiscoveryEnumeration = true)]
    public void ALoadedMachineContinuesUnderTheCallersExplicitSchedule(ExecutionConformancePath path)
    {
        var (engine, sites, bit) = Chain();
        // Captured by Transport.Save from an isolated checkout of master 9b805964.
        var initial = Convert.FromBase64String("RJNJV0rXrO6OnuCWvfxXm7/bAA==");
        Assert.Equal(initial, Transport.Save(engine));
        var (together, togetherStores) = Transport.Load(initial);
        var (ordered, orderedStores) = Transport.Load(initial);
        Assert.Equal(initial, Transport.Save(together));
        Assert.Equal(initial, Transport.Save(ordered));

        path.Step(engine, together: true);
        path.Step(together, together: true);
        path.Step(ordered);
        Assert.Equal(0b010, Occupancy(sites, bit));
        Assert.Equal(0b010, Occupancy(togetherStores[0], bit));
        Assert.Equal(0b001, Occupancy(orderedStores[0], bit));

        var inFlight = Transport.Save(engine);
        Assert.Equal(inFlight, Transport.Save(together));
        var (continued, _) = Transport.Load(inFlight);
        Assert.Equal(inFlight, Transport.Save(continued));
        Assert.Equal(0L, continued.Tick);
        for (var remaining = 0; remaining < 2; remaining++)
        {
            var original = path.Step(engine, together: true);
            var restored = path.Step(continued, together: true);
            Assert.Equal(original.Tick - 1, restored.Tick);
            Assert.Equal(original.Steps.Select(step => step.Quiet), restored.Steps.Select(step => step.Quiet));
            Assert.Equal(Transport.Save(engine), Transport.Save(continued));
        }
        Assert.Equal(0b001, Occupancy(sites, bit));
    }

    [Theory]
    [MemberData(nameof(ExecutionConformancePath.All), MemberType = typeof(ExecutionConformancePath), DisableDiscoveryEnumeration = true)]
    public void AReopenedAlignedLadderMatchesAFreshObservation(ExecutionConformancePath path)
    {
        foreach (var together in new[] { false, true })
        {
            var live = Observation();
            live.Left.Restrict(0, 9);
            live.Right.Restrict(0, 8);
            Assert.NotNull(path.Settle(live.Engine, 12, together));
            Assert.Equal(1, live.Sum.Read(0));
            Assert.Equal(0b10UL, live.Carry[^1].Values(0));

            Ladder.Reopen(live.Engine.Wirings, 0);
            Assert.Null(live.Sum.Read(0));
            Assert.Equal(0, live.Engine.Wirings[0].Value(0, 2));
            live.Left.Restrict(0, 4);
            live.Right.Restrict(0, 6);
            Assert.NotNull(path.Settle(live.Engine, 12, together));

            var fresh = Observation();
            fresh.Left.Restrict(0, 4);
            fresh.Right.Restrict(0, 6);
            Assert.NotNull(fresh.Engine.Settle(12, together));
            Assert.Equal(10, live.Sum.Read(0));
            Assert.Equal(0b01UL, live.Carry[^1].Values(0));
            Assert.Equal(Transport.Save(fresh.Engine), Transport.Save(live.Engine));
        }
    }

    [Theory]
    [MemberData(nameof(ExecutionConformancePath.All), MemberType = typeof(ExecutionConformancePath), DisableDiscoveryEnumeration = true)]
    public void ReopeningAMappedObservationLeavesTheSupplierAndOtherPositionsHeld(ExecutionConformancePath path)
    {
        var source = new Store(0, 2);
        var observed = Supports.On(source, Field.Bit(source.AddPlane()));
        observed.Restrict(0, 0b01UL);
        observed.Restrict(1, 0b10UL);
        var port = source.Publish([1], [.. observed.Planes]);
        new Engine().Hold(source);

        var receiver = new Store(0, 3);
        var received = Supports.On(receiver, Field.Bit(receiver.AddPlane()));
        received.Restrict(0, 0b10UL);
        received.Restrict(1, 0b01UL);
        var read = new Wiring(Settling.Same(Shape.Bit), Binding.Open(port, 0, 1), received.Variable);
        read.Add(0, 2);
        var engine = new Engine().Add(read);
        path.Step(engine);
        Assert.Equal(1, read.Value(0, 1));
        Assert.True(source.SamePosition(1, port, 0));

        Ladder.ReopenRows([(read, 0)]);
        Assert.Equal(0b11UL, received.Values(2));
        Assert.Equal(0b10UL, received.Values(0));
        Assert.Equal(0b01UL, received.Values(1));
        Assert.Equal(0b01UL, observed.Values(0));
        Assert.Equal(0b10UL, observed.Values(1));

        observed.Forget(1);
        observed.Restrict(1, 0b01UL);
        Assert.True(Assert.Single(path.Step(engine).Steps).Unadmitted.None);
        Assert.Equal(0, read.Value(0, 1));
    }

    private static (Engine Engine, Store Sites, Field Bit) Chain()
    {
        var sites = new Store(0, 3);
        var bit = new Field(Shape.Unary(1), sites.AddPlane());
        bit.Write(sites, 0, 1);
        var law = Flows.Convert(1, 1, 1, 1);
        var first = new Wiring(law, bit.On(sites), bit.On(sites));
        first.Add(0, 1);
        var second = new Wiring(law, bit.On(sites), bit.On(sites));
        second.Add(1, 2);
        return (new Engine().Add(first).Add(second), sites, bit);
    }

    private static int Occupancy(Store sites, Field bit) =>
        Enumerable.Range(0, sites.Count).Aggregate(0, (pattern, site) => 2 * pattern + bit.Read(sites, site)!.Value);

    private static (Engine Engine, Ladder Left, Ladder Right, Ladder Sum, IReadOnlyList<Supports> Carry) Observation()
    {
        var store = new Store(0, 1);
        Ladder left = new(store, 4, 2), right = new(store, 4, 2), sum = new(store, 4, 2);
        var (columns, carries) = Ladder.Add(left, right, sum, Plane.Ones(1));
        var engine = new Engine();
        foreach (var column in columns) engine.Add(column);
        return (engine, left, right, sum, carries);
    }
}
