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

    [Theory]
    [MemberData(nameof(ExecutionConformancePath.All), MemberType = typeof(ExecutionConformancePath), DisableDiscoveryEnumeration = true)]
    public void ASameTickPortWriteRetargetingARowMatchesCanonicalExecution(ExecutionConformancePath path)
    {
        foreach (var throughSocket in new[] { false, true })
        {
            static (Engine Engine, Store Sites, Wiring Toggle) Build(bool useSocket)
            {
                var sites = new Store(1, 2);
                var toggleLaw = TransitionLaw.FromFunction([Shape.Bit], v => [1 - v[0]], new Accounting(true));
                var toggle = new Wiring(toggleLaw, new Binding(sites, 0));
                toggle.Add(0);
                var refPort = toggle.Rows.Publish([0], [toggle.ReferencePlanes(0)[0]]);
                Surface target = refPort;
                if (useSocket)
                {
                    var sock = Socket.For(refPort);
                    sock.Plug(refPort);
                    target = sock;
                }
                var source = new Store(1, 1);
                source.Write(0, 0, true);
                var move = new Wiring(Flows.Convert(1, 1, 1, 1), new Binding(source, 0), new Binding(target, 0));
                move.Add(0, 0);
                return (new Engine().Add(move).Add(toggle), sites, toggle);
            }

            var reference = Build(throughSocket);
            var actual = Build(throughSocket);
            var expectedStep = reference.Engine.Step();
            var actualStep = path.Step(actual.Engine);
            AssertStepParity(expectedStep, actualStep);
            Assert.Equal([1], actual.Toggle.Row(0));
            Assert.False(actual.Sites.Read(0, 0));
            Assert.True(actual.Sites.Read(0, 1));
            Assert.Equal(Transport.Save(reference.Engine), Transport.Save(actual.Engine));

            var refSettle = Build(throughSocket);
            var actSettle = Build(throughSocket);
            Assert.Equal(refSettle.Engine.Settle(1), path.Settle(actSettle.Engine, 1));
            Assert.Equal(refSettle.Engine.Tick, actSettle.Engine.Tick);
            Assert.Equal(Transport.Save(refSettle.Engine), Transport.Save(actSettle.Engine));
        }
    }

    [Theory]
    [MemberData(nameof(ExecutionConformancePath.All), MemberType = typeof(ExecutionConformancePath), DisableDiscoveryEnumeration = true)]
    public void RecruitingASparePositionInTheSameTickRunsALaterCompiledFollower(ExecutionConformancePath path)
    {
        static (Engine Engine, Store Sites) Build()
        {
            var sites = new Store(2, 1);
            var spare = sites.AddSpare(1);
            sites.Write(0, 0, true);
            sites.Write(0, spare, true);
            var recruitLaw = TransitionLaw.FromFunction([Shape.Bit], _ => [1], new Accounting(true));
            var recruit = new Wiring(recruitLaw, new Binding(sites.Life, 0));
            recruit.Add(spare);
            var convert = new Wiring(Flows.Convert(1, 1, 1, 1), new Binding(sites, 0), new Binding(sites, 1));
            convert.Add(0, 0);
            convert.Add(spare, spare);
            return (new Engine().Add(recruit).Add(convert), sites);
        }

        var refStep = Build();
        var actStep = Build();
        AssertStepParity(refStep.Engine.Step(), path.Step(actStep.Engine));
        Assert.True(actStep.Sites.IsLive(1));
        Assert.True(actStep.Sites.Read(1, 0));
        Assert.True(actStep.Sites.Read(1, 1));
        Assert.Equal(Transport.Save(refStep.Engine), Transport.Save(actStep.Engine));

        var refSettle = Build();
        var actSettle = Build();
        Assert.Equal(refSettle.Engine.Settle(4), path.Settle(actSettle.Engine, 4));
        Assert.Equal(refSettle.Engine.Tick, actSettle.Engine.Tick);
        Assert.True(actSettle.Sites.Read(1, 1));
        Assert.Equal(Transport.Save(refSettle.Engine), Transport.Save(actSettle.Engine));

        static (Engine Engine, Store Sites) BuildRetire()
        {
            var sites = new Store(2, 2);
            sites.Write(0, 0, true);
            sites.Write(0, 1, true);
            var retireLaw = TransitionLaw.FromFunction([Shape.Bit], _ => [0], new Accounting(true));
            var retire = new Wiring(retireLaw, new Binding(sites.Life, 0));
            retire.Add(1);
            var convert = new Wiring(Flows.Convert(1, 1, 1, 1), new Binding(sites, 0), new Binding(sites, 1));
            convert.Add(0, 0);
            convert.Add(1, 1);
            return (new Engine().Add(retire).Add(convert), sites);
        }

        var refRetire = BuildRetire();
        var actRetire = BuildRetire();
        AssertStepParity(refRetire.Engine.Step(), path.Step(actRetire.Engine));
        Assert.False(actRetire.Sites.IsLive(1));
        Assert.True(actRetire.Sites.Read(1, 0));
        Assert.False(actRetire.Sites.Read(1, 1));
        Assert.Equal(Transport.Save(refRetire.Engine), Transport.Save(actRetire.Engine));
    }

    [Theory]
    [MemberData(nameof(ExecutionConformancePath.All), MemberType = typeof(ExecutionConformancePath), DisableDiscoveryEnumeration = true)]
    public void RebindingAPortLaneBetweenStepsUpdatesEngineWavesAndEvidence(ExecutionConformancePath path)
    {
        foreach (var throughSocket in new[] { false, true })
        {
            static (Engine Engine, Store Sites, Port Port) Build(bool useSocket)
            {
                var sites = new Store(2, 2);
                var toggle = TransitionLaw.FromFunction([Shape.Bit], v => [1 - v[0]], new Accounting(true));
                var writer = new Wiring(toggle, new Binding(sites, 0));
                writer.Add(0);
                var port = sites.Publish([1], [0]);
                Surface readerSource = port;
                if (useSocket)
                {
                    var sock = Socket.For(port);
                    sock.Plug(port);
                    readerSource = sock;
                }
                var copy = TransitionLaw.FromFunction([Shape.Bit, Shape.Bit], v => [v[0], v[0]],
                    new Accounting(true, new Unchanged(0)));
                var reader = new Wiring(copy, new Binding(readerSource, 0), new Binding(sites, 1));
                reader.Add(0, 1);
                return (new Engine().Add(writer).Add(reader), sites, port);
            }

            var reference = Build(throughSocket);
            var actual = Build(throughSocket);
            var firstRef = reference.Engine.Step();
            var firstAct = path.Step(actual.Engine);
            Assert.Single(firstRef.Waves);
            AssertStepParity(firstRef, firstAct);
            Assert.Equal(Transport.Save(reference.Engine), Transport.Save(actual.Engine));

            reference.Sites.Rebind(reference.Port, 0, 0);
            actual.Sites.Rebind(actual.Port, 0, 0);
            var secondRef = reference.Engine.Step();
            var secondAct = path.Step(actual.Engine);
            Assert.Equal(2, secondRef.Waves.Count);
            AssertStepParity(secondRef, secondAct);
            Assert.Equal(Transport.Save(reference.Engine), Transport.Save(actual.Engine));
        }
    }

    [Theory]
    [MemberData(nameof(ExecutionConformancePath.All), MemberType = typeof(ExecutionConformancePath), DisableDiscoveryEnumeration = true)]
    public void AnAliasedNonIdentityPacketRowReportsAliasedRatherThanSkipped(ExecutionConformancePath path)
    {
        static Engine Build()
        {
            var sites = new Store(1, 3);
            sites.Write(0, 0, true);
            sites.Write(0, 1, true);
            var convert = new Wiring(Flows.Convert(1, 1, 1, 1), new Binding(sites, 0), new Binding(sites, 0));
            convert.Add(0, 2);
            convert.Add(1, 2);
            // Mutate saved row 0 destination reference so row 0 becomes (0, 0) while row 1 remains runnable (1, 2).
            foreach (var p in convert.ReferencePlanes(1)) convert.Rows.Write(p, 0, false);
            return new Engine().Add(convert);
        }

        var reference = Build();
        var actual = Build();
        var expectedStep = reference.Step();
        var actualStep = path.Step(actual);
        Assert.True(expectedStep.Steps[0].Aliased[0]);
        Assert.False(expectedStep.Steps[0].Skipped[0]);
        Assert.True(expectedStep.Steps[0].Changed[1]);
        AssertStepParity(expectedStep, actualStep);
        Assert.Equal(Transport.Save(reference), Transport.Save(actual));
    }

    [Theory]
    [MemberData(nameof(ExecutionConformancePath.All), MemberType = typeof(ExecutionConformancePath), DisableDiscoveryEnumeration = true)]
    public void NonIdentityPacketRowsSharingAPositionAcrossDifferentPlanesSplitRowWaves(ExecutionConformancePath path)
    {
        static Engine Build()
        {
            var sites = new Store(2, 3);
            sites.Write(0, 0, true);
            sites.Write(0, 1, true);
            var convert = new Wiring(Flows.Convert(1, 1, 1, 1), new Binding(sites, 0), new Binding(sites, 1));
            convert.Add(0, 1);
            convert.Add(1, 2);
            return new Engine().Add(convert);
        }

        var reference = Build();
        var actual = Build();
        var expectedStep = reference.Step();
        var actualStep = path.Step(actual);
        Assert.Equal(2, expectedStep.Steps[0].Waves.Count);
        AssertStepParity(expectedStep, actualStep);
        Assert.Equal(Transport.Save(reference), Transport.Save(actual));
    }

    [Theory]
    [MemberData(nameof(ExecutionConformancePath.All), MemberType = typeof(ExecutionConformancePath), DisableDiscoveryEnumeration = true)]
    public void AnEmptyRunnableLadderEmitsNoRowWaves(ExecutionConformancePath path)
    {
        static Engine Build()
        {
            var store = new Store(0, 1);
            Ladder a = new(store, [2, 5], binary: true), b = new(store, [2, 5], binary: true), sum = new(store, [2, 5], binary: true);
            var (wirings, _) = Ladder.Add(a, b, sum, Plane.Ones(1));
            store.Retire(0);
            var engine = new Engine();
            foreach (var w in wirings) engine.Add(w);
            return engine;
        }

        var reference = Build();
        var actual = Build();
        var expectedStep = reference.Step();
        var actualStep = path.Step(actual);
        Assert.All(expectedStep.Steps, s => Assert.Empty(s.Waves));
        AssertStepParity(expectedStep, actualStep);
        Assert.Equal(Transport.Save(reference), Transport.Save(actual));
    }

    private static void AssertStepParity(EngineStep expected, EngineStep actual)
    {
        Assert.Equal(expected.Tick, actual.Tick);
        Assert.Equal(expected.Waves.Count, actual.Waves.Count);
        for (var i = 0; i < expected.Waves.Count; i++)
            Assert.Equal(expected.Waves[i], actual.Waves[i]);
        Assert.Equal(expected.Steps.Count, actual.Steps.Count);
        for (var i = 0; i < expected.Steps.Count; i++)
        {
            var e = expected.Steps[i];
            var a = actual.Steps[i];
            Assert.Equal(e.Changed, a.Changed);
            Assert.Equal(e.Unread, a.Unread);
            Assert.Equal(e.Unadmitted, a.Unadmitted);
            Assert.Equal(e.Ambiguous, a.Ambiguous);
            Assert.Equal(e.Skipped, a.Skipped);
            Assert.Equal(e.Aliased, a.Aliased);
            Assert.Equal(e.Contested, a.Contested);
            Assert.Equal(e.Noted, a.Noted);
            Assert.Equal(e.Quiet, a.Quiet);
            Assert.Equal(e.Before, a.Before);
            Assert.Equal(e.After, a.After);
            Assert.Equal(e.Waves, a.Waves);
        }
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
