using Gojo.Encoding.Core.Packed;

namespace Gojo.Encoding.Core.Tests.Packed;

public sealed class TokenPassageTests
{
    [Fact]
    public void UndetectableObservationDoesNotAuthorizeWholeTokenTransfer()
    {
        var measurements = new PortStructure();
        var ports = new[] { 1UL, 1UL, 0UL }.Select(value => measurements.Publish(measurements.Create(0, 0, 1, value))).ToArray();
        var passages = new PassageConnections(measurements, PassageLaw.Controlled());
        Assert.Throws<InvalidOperationException>(() => passages.Connect(2, ports[0], ports[1], ports[2]));
        Assert.Throws<InvalidOperationException>(() => measurements.ExportOutlet(ports[0]));
        Assert.Throws<ArgumentException>(() => measurements.ExportIntake(ports[1], ports[2]));
        Assert.Equal((byte)0, measurements.ReadPort(ports[2]));
        Assert.Equal(3, measurements.LiveCount(0, 0, 1));
    }

    [Fact]
    public void UnknownControlKeepsJointAlternativesAndDoesNotMutateTokens()
    {
        var owner = PortStructure.ForBooleanTokens();
        var ports = new[] { 1UL, 1UL, 0UL }.Select(value => owner.Publish(owner.Create(0, 0, 1, value))).ToArray();
        var passages = new PassageConnections(owner, PassageLaw.Controlled());
        var row = passages.Connect(2, ports[0], ports[1], ports[2]);
        var uncertain = passages.Observe(row, visibleRoles: 5);
        Assert.Equal((byte)((1 << 1) | (1 << 3)), uncertain);
        Assert.Equal((byte)((1 << 1) | (1 << 6)), passages.Predict(row, uncertain));
        var before = owner.PackedBytes(0, 0, 1);
        Assert.False(passages.TryStep(row, uncertain, out _));
        Assert.Equal(before, owner.PackedBytes(0, 0, 1));
        Assert.True(passages.TryStep(row, passages.Observe(row), out var after));
        Assert.Equal((byte)6, after);
        Assert.Equal(new byte[] { 6 }, owner.PackedBytes(0, 0, 1));
        Assert.False(passages.TryStep(row, 1 << 3, out _)); // Stale before state.
    }

    [Fact]
    public void TwoPassagesShareTheirControlAndCommitAResolvedScopeTogether()
    {
        var owner = PortStructure.ForBooleanTokens();
        var scope = new[] { 1UL, 1UL, 0UL, 0UL }.Select(v => owner.Publish(owner.Create(0, 0, 1, v))).ToArray();
        var passages = new PassageConnections(owner, PassageLaw.Controlled());
        var first = passages.Connect(2, scope[0], scope[1], scope[2]);
        var second = passages.Connect(2, scope[2], scope[1], scope[3]);
        var unknown = passages.ObserveScope(scope, 13); // Read every site except control.
        var prediction = passages.PredictScope(first, scope, unknown);
        prediction = passages.PredictScope(second, scope, prediction);
        Assert.Equal((1U << 1) | (1U << 10), prediction);
        Assert.False(PassageConnections.TrySequence(scope, unknown, out _, (passages, first), (passages, second)));
        Assert.Equal(new byte[] { 3 }, owner.PackedBytes(0, 0, 1));
        Assert.True(PassageConnections.TrySequence(scope, passages.ObserveScope(scope, 15), out var after,
            (passages, first), (passages, second)));
        Assert.Equal((byte)10, after);
        Assert.Equal(new byte[] { 10 }, owner.PackedBytes(0, 0, 1));
    }

    [Fact]
    public void SeparateOwnersExchangeThroughExposedPortsWithoutDuplicatingAToken()
    {
        var source = PortStructure.ForBooleanTokens();
        var receiver = PortStructure.ForBooleanTokens();
        var sourcePort = source.Publish(source.Create(0, 0, 1, 1));
        var guard = receiver.Publish(receiver.Create(0, 0, 1, 1));
        var target = receiver.Publish(receiver.Create(0, 0, 1, 0));
        var seams = new PortSeams([source.ExportOutlet(sourcePort)], [receiver.ExportIntake(guard, target)], PassageLaw.Controlled());
        seams.Link(0, 0);
        Assert.Equal((byte)1, source.Connect(sourcePort).Read());
        Assert.Equal((byte)1, source.Connect(sourcePort).Read());
        var first = Assert.Single(seams.Pulse());
        Assert.True(first.Transfer.Moved);
        Assert.Equal((byte)0, source.ReadPort(sourcePort));
        Assert.Equal((byte)1, receiver.ReadPort(target));
        Assert.False(Assert.Single(seams.Pulse()).Transfer.Moved);
    }

    [Fact]
    public void SelectedCompatibleTokenHandoffDoesNotPretendToCalibrateOtherFrames()
    {
        var source = PortStructure.ForBooleanTokens();
        var receiver = PortStructure.ForBooleanTokens();
        var sourcePort = source.Publish(source.Create(0, 0, 1, 1));
        var guard = receiver.Publish(receiver.Create(1, 0, 1, 1));
        var target = receiver.Publish(receiver.Create(1, 0, 1, 0));
        var seams = new PortSeams([source.ExportOutlet(sourcePort)], [receiver.ExportIntake(guard, target)], PassageLaw.Unconditional());
        seams.Link(0, 0);
        var transfer = Assert.Single(seams.Pulse()).Transfer;
        Assert.Null(transfer.Before);
        Assert.Null(transfer.After);
        Assert.Equal((byte)1, source.ReadPort(sourcePort));
        Assert.Equal((byte)4, receiver.ReadPort(target));
    }

    [Fact]
    public void BranchPredictionRetainsBothChoicesUntilTheGuardResolves()
    {
        var guardRead = new MutableRead(3);
        var circuit = new PulseCircuit();
        var start = circuit.State(true);
        var left = circuit.State();
        var right = circuit.State();
        var join = circuit.State();
        var guard = circuit.Guard();
        circuit.Bind(guard, guardRead);
        circuit.Link(circuit.AddTable(PassageLaw.Controlled()), start, guard, left);
        circuit.Link(circuit.AddTable(PassageLaw.Controlled(false)), start, guard, right);
        var continueTable = circuit.AddTable(PassageLaw.Unconditional());
        circuit.Link(continueTable, left, guard, join);
        circuit.Link(continueTable, right, guard, join);
        Assert.Equal(new[] { left, right }, circuit.Predict().Targets);
        var before = circuit.ValueBytes();
        Assert.False(circuit.Pulse().Committed);
        Assert.Equal(before, circuit.ValueBytes());
        guardRead.Support = 2;
        Assert.True(circuit.Pulse().Moved);
        Assert.Equal(left, circuit.Active);
        guardRead.Support = 3; // Irrelevant to the unconditional continuation.
        Assert.True(circuit.Pulse().Moved);
        Assert.Equal(join, circuit.Active);
        Assert.False(circuit.Pulse().Moved);
    }

    [Theory]
    [InlineData(2)]
    [InlineData(6)]
    [InlineData(65)]
    public void ReferenceCycleUsesTopologyForCountingAndOnlyAdvancesOneSitePerPulse(int positions)
    {
        var cycle = new ReferenceCycle(positions);
        Assert.Equal(OrderedBoundary.Bits(positions + 1), cycle.Circuit.ReferenceBits);
        for (var pulse = 0; pulse < positions * 2; pulse++)
        {
            Assert.True(cycle.Circuit.Pulse().Moved);
            Assert.Equal((pulse + 1) % positions, cycle.Position);
        }
        var copy = cycle.Sites;
        copy[0] = -1;
        Assert.Equal((byte)2, cycle.AtStart.ReadSupport());
    }

    private sealed class MutableRead(byte support) : IPrimitiveReadPort
    {
        public byte Support { get; set; } = support;
        public byte ReadSupport() => Support;
    }
}
