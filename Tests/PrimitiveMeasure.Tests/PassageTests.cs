using Gojo.Experiments.PrimitiveMeasure;

namespace Gojo.Experiments.PrimitiveMeasure.Tests;

public sealed class PassageTests
{
    [Theory]
    [InlineData(0, 0)]
    [InlineData(0, 1)]
    [InlineData(1, 0)]
    [InlineData(1, 1)]
    public void EveryResolvedPatternFollowsTheEncodedLawAndPreservesContext(int orientation, int direction)
    {
        var (owner, bindings, ports) = Create(0, 3, orientation, direction);
        var law = PassageLaw.Controlled();
        var passages = new PassageConnections(owner, law);
        var row = passages.Connect(2, ports[0], ports[1], ports[2]);
        Assert.Equal(new byte[] { 0x01, 0x02, 0x04, 0x00, 0x10, 0x20, 0x48, 0x80 }, law.PackedBytes());
        Assert.Equal(new byte[] { 0x24 }, passages.PackedRows(2));
        Assert.Equal(ports, passages.Wiring(row));

        for (byte before = 0; before < 8; before++)
        {
            for (int role = 0; role < 3; role++)
                owner.StoreObservation(bindings[role], orientation == 1, direction == 1, (ulong)((before >> role) & 1));
            var expected = before == 3 ? (byte)6 : before;
            var observation = passages.Observe(row);
            Assert.Equal(Support(before), observation);
            Assert.Equal(Support(expected), passages.Predict(row, observation));
            Assert.True(passages.TryStep(row, observation, out var after));
            Assert.Equal(expected, after);
            Assert.Equal(new byte[] { expected }, owner.PackedBytes(orientation, direction, 1));
            Assert.Equal(Support(expected), passages.Observe(row));
            for (int role = 0; role < 3; role++)
                Assert.Equal(ThreeBitMeasure.Encode(orientation == 1, direction == 1, ((expected >> role) & 1) != 0), owner.ReadPort(ports[role]));
            for (byte candidate = 0; candidate < 8; candidate++)
                Assert.Equal(candidate == expected, law.Allows(before, candidate));
        }
        Assert.Equal(ports, passages.Wiring(row));
    }

    [Fact]
    public void ClosedPassageRetainsContentsThenOpeningTransfersWithoutConsumingControl()
    {
        var (owner, bindings, ports) = Create(1, 3);
        var passages = new PassageConnections(owner, PassageLaw.Controlled());
        var row = passages.Connect(2, ports[0], ports[1], ports[2]);

        Assert.True(passages.TryStep(row, passages.Observe(row), out var held));
        Assert.Equal((byte)1, held);
        Assert.Equal(new byte[] { 1 }, owner.PackedBytes(0, 0, 1));
        owner.StoreObservation(bindings[1], false, false, 1);
        Assert.True(passages.TryStep(row, passages.Observe(row), out var moved));
        Assert.Equal((byte)6, moved);
        Assert.True(passages.TryStep(row, passages.Observe(row), out var repeated));
        Assert.Equal(moved, repeated);
        Assert.Equal(new byte[] { 6 }, owner.PackedBytes(0, 0, 1));
        Assert.Equal((byte)1, owner.ReadPort(ports[1]));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void ConcealedControlProducesJointAlternativesWithoutSelectingItsPrivateValue(int actualControl)
    {
        var before = 1 | (actualControl << 1);
        var (owner, _, ports) = Create(before, 3);
        var passages = new PassageConnections(owner, PassageLaw.Controlled());
        var row = passages.Connect(2, ports[0], ports[1], ports[2]);

        var unresolved = passages.Observe(row, 0b101);
        Assert.Equal((byte)0x0A, unresolved); // Before states 1 or 3: only C is hidden.
        Assert.Equal((byte)0xFF, passages.Observe(row, 0));
        var predicted = passages.Predict(row, unresolved);
        Assert.Equal((byte)0x42, predicted); // Whole states 1 or 6, never lost or duplicated flow.
        Assert.Equal(new byte[] { 1, 6 }, ReadSupport.Words(predicted));
        Assert.False(passages.TryStep(row, unresolved, out _));
        Assert.Equal(new byte[] { (byte)before }, owner.PackedBytes(0, 0, 1));
        Assert.False(passages.TryStep(row, 0, out _));
        Assert.Equal(new byte[] { (byte)before }, owner.PackedBytes(0, 0, 1));

        var resolved = passages.Observe(row);
        Assert.True(passages.TryStep(row, resolved, out var after));
        Assert.Equal(actualControl == 1 ? (byte)6 : (byte)1, after);
    }

    [Fact]
    public void ChainUsesThePackedRowsAndOnlyAdvancesWhenEachPassageIsExplicitlyStepped()
    {
        var (owner, _, ports) = Create(0x0B, 5);
        var passages = new PassageConnections(owner, PassageLaw.Controlled());
        var first = passages.Connect(3, ports[0], ports[1], ports[2]);
        var second = passages.Connect(3, ports[2], ports[3], ports[4]);
        Assert.Equal(new[] { 0, 1, 2 }, passages.Wiring(first));
        Assert.Equal(new[] { 2, 3, 4 }, passages.Wiring(second));
        Assert.Equal(new byte[] { 0x88, 0x34, 0x02 }, passages.PackedRows(3));

        Assert.Equal(1u << 11, passages.ObserveScope(ports, 31));
        Assert.True(passages.TryStep(first, passages.Observe(first), out var firstAfter));
        Assert.Equal((byte)6, firstAfter);
        Assert.Equal(new byte[] { 0x0E }, owner.PackedBytes(0, 0, 1));
        Assert.Equal(1u << 14, passages.ObserveScope(ports, 31));
        Assert.True(passages.TryStep(second, passages.Observe(second), out var secondAfter));
        Assert.Equal((byte)6, secondAfter);
        Assert.Equal(new byte[] { 0x1A }, owner.PackedBytes(0, 0, 1));
        Assert.True(passages.TryStep(first, passages.Observe(first), out _));
        Assert.True(passages.TryStep(second, passages.Observe(second), out _));
        Assert.Equal(new byte[] { 0x1A }, owner.PackedBytes(0, 0, 1));
        Assert.Equal(new byte[] { 0x88, 0x34, 0x02 }, passages.PackedRows(3));
    }

    [Fact]
    public void ReversingTheChosenStepOrderLeavesContentsInTheMiddleUntilAnotherStep()
    {
        var (owner, _, ports) = Create(0x0B, 5);
        var passages = new PassageConnections(owner, PassageLaw.Controlled());
        var first = passages.Connect(3, ports[0], ports[1], ports[2]);
        var second = passages.Connect(3, ports[2], ports[3], ports[4]);

        Assert.True(passages.TryStep(second, passages.Observe(second), out _));
        Assert.Equal(new byte[] { 0x0B }, owner.PackedBytes(0, 0, 1));
        Assert.True(passages.TryStep(first, passages.Observe(first), out _));
        Assert.Equal(new byte[] { 0x0E }, owner.PackedBytes(0, 0, 1));
        Assert.True(passages.TryStep(second, passages.Observe(second), out _));
        Assert.Equal(new byte[] { 0x1A }, owner.PackedBytes(0, 0, 1));
    }

    [Theory]
    [InlineData(0b11101, 0x00000A00u, 0x00004200u, 0x04000200u)]
    [InlineData(0b10101, 0x00000A0Au, 0x00004242u, 0x04000242u)]
    public void ChainPredictionKeepsCompleteAlternativesAndMovesNoActualContents(int visible, uint before, uint middle, uint after)
    {
        var (owner, _, ports) = Create(0x0B, 5);
        var passages = new PassageConnections(owner, PassageLaw.Controlled());
        var first = passages.Connect(3, ports[0], ports[1], ports[2]);
        var second = passages.Connect(3, ports[2], ports[3], ports[4]);

        Assert.Equal(before, passages.ObserveScope(ports, visible));
        Assert.Equal(middle, passages.PredictScope(first, ports, before));
        Assert.Equal(after, passages.PredictScope(second, ports, middle));
        Assert.Equal((byte)0x0A, passages.ProjectScope(first, ports, before));
        Assert.False(passages.TryStep(first, passages.ProjectScope(first, ports, before), out _));
        Assert.Equal(new byte[] { 0x0B }, owner.PackedBytes(0, 0, 1));
        foreach (var support in new[] { before, middle, after })
            foreach (var state in States(support, 5))
                Assert.Equal(1, (state & 1) + ((state >> 2) & 1) + ((state >> 4) & 1));
    }

    [Fact]
    public void OneUnresolvedControlSharedByBothPassagesCannotOpenThenCloseWithinOnePrediction()
    {
        // Scope [source, shared control, middle, target]. The prediction holds
        // the control observation fixed throughout this explicitly chosen sequence.
        var (owner, _, ports) = Create(3, 4);
        var passages = new PassageConnections(owner, PassageLaw.Controlled());
        var first = passages.Connect(2, ports[0], ports[1], ports[2]);
        var second = passages.Connect(2, ports[2], ports[1], ports[3]);
        var before = passages.ObserveScope(ports, 0b1101);
        Assert.Equal(0x0Au, before);
        var middle = passages.PredictScope(first, ports, before);
        Assert.Equal(0x42u, middle);
        var after = passages.PredictScope(second, ports, middle);
        Assert.Equal(0x402u, after);
        Assert.Equal(new[] { 1, 10 }, States(after, 4));
        Assert.Equal(new byte[] { 3 }, owner.PackedBytes(0, 0, 1));
    }

    [Fact]
    public void ReorderingTheReadScopePreservesWhichSitesEachPassageTouches()
    {
        var (owner, _, ports) = Create(0x0B, 5);
        var passages = new PassageConnections(owner, PassageLaw.Controlled());
        var first = passages.Connect(3, ports[0], ports[1], ports[2]);
        var second = passages.Connect(3, ports[2], ports[3], ports[4]);
        int[] scope = [ports[2], ports[4], ports[0], ports[3], ports[1]];
        // Scope now means [middle, target, source, C2, C1]. Only C1 is hidden.
        var before = passages.ObserveScope(scope, 0b01111);
        Assert.Equal((1u << 12) | (1u << 28), before);
        Assert.Equal((byte)0x0A, passages.ProjectScope(first, scope, before));
        var middle = passages.PredictScope(first, scope, before);
        Assert.Equal((1u << 12) | (1u << 25), middle);
        Assert.Equal((1u << 12) | (1u << 26), passages.PredictScope(second, scope, middle));
        Assert.Equal(new byte[] { 0x0B }, owner.PackedBytes(0, 0, 1));
    }

    [Fact]
    public void TwoOutgoingPassagesCannotBothConsumeTheSameSource()
    {
        var (owner, _, ports) = Create(3, 4);
        var passages = new PassageConnections(owner, PassageLaw.Controlled());
        var first = passages.Connect(2, ports[0], ports[1], ports[2]);
        var second = passages.Connect(2, ports[0], ports[1], ports[3]);
        var earlierSecondRead = passages.Observe(second);

        Assert.True(passages.TryStep(first, passages.Observe(first), out _));
        Assert.False(passages.TryStep(second, earlierSecondRead, out _));
        Assert.True(passages.TryStep(second, passages.Observe(second), out var unchanged));
        Assert.Equal((byte)2, unchanged);
        Assert.Equal(new byte[] { 6 }, owner.PackedBytes(0, 0, 1));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void AChangedParticipantInvalidatesAnEarlierObservationBeforeAnyWrite(int changedRole)
    {
        var (owner, bindings, ports) = Create(3, 3);
        var passages = new PassageConnections(owner, PassageLaw.Controlled());
        var row = passages.Connect(2, ports[0], ports[1], ports[2]);
        var earlier = passages.Observe(row);
        var changed = 3 ^ (1 << changedRole);
        owner.StoreObservation(bindings[changedRole], false, false, (ulong)((changed >> changedRole) & 1));
        Assert.False(passages.TryStep(row, earlier, out _));
        Assert.Equal(new byte[] { (byte)changed }, owner.PackedBytes(0, 0, 1));
    }

    [Theory]
    [InlineData(0, 1, 0)]
    [InlineData(0, 0, 2)]
    [InlineData(0, 1, 1)]
    public void RepeatedBindingsAreRefusedEvenWhenPublishedThroughDifferentPorts(int source, int control, int target)
    {
        var (owner, bindings, ports) = Create(3, 3);
        var alias = owner.Publish(bindings[target]);
        var passages = new PassageConnections(owner, PassageLaw.Controlled());
        var row = passages.Connect(2, ports[source], ports[control], alias);
        Assert.Equal((byte)0, passages.Observe(row));
        Assert.Equal((byte)0, passages.Predict(row, 0x08));
        Assert.False(passages.TryStep(row, 0x08, out _));
        Assert.Equal(new byte[] { 3 }, owner.PackedBytes(0, 0, 1));
    }

    [Theory]
    [InlineData(1, 0, 1)]
    [InlineData(0, 1, 1)]
    [InlineData(0, 0, 2)]
    [InlineData(0, 2, 2)]
    [InlineData(2, 0, 3)]
    public void IncompatibleOrNonFixedTargetLeavesEverySiteUntouched(int orientation, int direction, int width)
    {
        var owner = new PortStructure();
        var source = owner.Create(0, 0, 1, 1);
        var control = owner.Create(0, 0, 1, 1);
        var target = owner.Create(orientation, direction, width, 0);
        var ports = new[] { owner.Publish(source), owner.Publish(control), owner.Publish(target) };
        var passages = new PassageConnections(owner, PassageLaw.Controlled());
        var row = passages.Connect(2, ports[0], ports[1], ports[2]);
        var before = ports.Select(owner.ReadPort).ToArray();

        Assert.Equal((byte)0, passages.Observe(row));
        Assert.Equal((byte)0, passages.Predict(row, 0x08));
        Assert.False(passages.TryStep(row, 0x08, out _));
        Assert.Equal(before, ports.Select(owner.ReadPort).ToArray());
    }

    [Theory]
    [InlineData(0, 0, 2)]
    [InlineData(0, 2, 2)]
    [InlineData(2, 0, 3)]
    public void MigrationRevalidatesThePassageBeforeApplyingAnOldObservation(int orientation, int direction, int width)
    {
        var (owner, bindings, ports) = Create(3, 3);
        var passages = new PassageConnections(owner, PassageLaw.Controlled());
        var row = passages.Connect(2, ports[0], ports[1], ports[2]);
        var earlier = passages.Observe(row);
        Assert.True(owner.TryMove(bindings[2], orientation, direction, width));
        var before = ports.Select(owner.ReadPort).ToArray();

        Assert.Equal((byte)0, passages.Observe(row));
        Assert.False(passages.TryStep(row, earlier, out _));
        Assert.Equal(before, ports.Select(owner.ReadPort).ToArray());
        Assert.Equal(ports, passages.Wiring(row));
    }

    [Fact]
    public void EmptyAndAmbiguousLawsDoNotChooseAnActualTransition()
    {
        var (owner, _, ports) = Create(3, 3);
        foreach (var bitmap in new[] { new byte[8], Edges((3, 3), (3, 6)) })
        {
            var law = new PassageLaw(bitmap);
            var passages = new PassageConnections(owner, law);
            var row = passages.Connect(2, ports[0], ports[1], ports[2]);
            var expected = bitmap.All(value => value == 0) ? (byte)0 : (byte)0x48;
            Assert.Equal(expected, passages.Predict(row, 0x08));
            Assert.False(passages.TryStep(row, 0x08, out _));
            Assert.Equal(new byte[] { 3 }, owner.PackedBytes(0, 0, 1));
            Array.Clear(bitmap);
            Assert.Equal(expected, law.Predict(0x08));
            Array.Clear(law.PackedBytes());
            Assert.Equal(expected, law.Predict(0x08));
        }
    }

    [Fact]
    public void TheBitmapCanSelectAnAbsentControlWithoutASeparateExecutionOpcode()
    {
        var (owner, _, ports) = Create(1, 3);
        var law = new PassageLaw(Edges((1, 4)));
        var passages = new PassageConnections(owner, law);
        var row = passages.Connect(2, ports[0], ports[1], ports[2]);
        Assert.True(passages.TryStep(row, passages.Observe(row), out var after));
        Assert.Equal((byte)4, after);
        Assert.Equal(new byte[] { 4 }, owner.PackedBytes(0, 0, 1));
    }

    [Fact]
    public void MalformedLawsCannotIntroduceLossDuplicationReversalOrControlMutation()
    {
        Assert.Throws<ArgumentException>(() => new PassageLaw(new byte[7]));
        Assert.Throws<ArgumentException>(() => new PassageLaw(new byte[9]));
        foreach (var edge in new[] { (0, 4), (1, 0), (1, 5), (4, 1), (0, 2), (5, 4) })
            Assert.Throws<ArgumentException>(() => new PassageLaw(Edges(edge)));
    }

    [Fact]
    public void ScopeRejectsOutOfRangeMasksAndInvalidRoleLocations()
    {
        var law = PassageLaw.Controlled();
        Assert.Throws<ArgumentOutOfRangeException>(() => law.PredictScope(1u << 8, new[] { 0, 1, 2 }, 3));
        Assert.Throws<ArgumentOutOfRangeException>(() => PassageLaw.ProjectScope(1u << 8, new[] { 0, 1, 2 }, 3));
        Assert.Throws<ArgumentOutOfRangeException>(() => law.PredictScope(1, new[] { 0, 1, 2 }, 6));
        Assert.ThrowsAny<ArgumentException>(() => law.PredictScope(1, new[] { 0, 0, 2 }, 3));
        Assert.ThrowsAny<ArgumentException>(() => law.PredictScope(1, new[] { 0, 1, 3 }, 3));
        Assert.ThrowsAny<ArgumentException>(() => PassageLaw.ProjectScope(1, new[] { 0, 1 }, 3));
    }

    private static (PortStructure Owner, int[] Bindings, int[] Ports) Create(int state, int count, int orientation = 0, int direction = 0)
    {
        var owner = new PortStructure();
        var bindings = Enumerable.Range(0, count).Select(index => owner.Create(orientation, direction, 1, (ulong)((state >> index) & 1))).ToArray();
        var ports = bindings.Select(owner.Publish).ToArray();
        return (owner, bindings, ports);
    }

    private static byte Support(int state) => (byte)(1 << state);

    private static int[] States(uint support, int count) => Enumerable.Range(0, 1 << count)
        .Where(state => (support & (1u << state)) != 0).ToArray();

    private static byte[] Edges(params (int Before, int After)[] edges)
    {
        var bytes = new byte[8];
        foreach (var (before, after) in edges)
        {
            var bit = before + 8 * after;
            bytes[bit / 8] |= (byte)(1 << (bit % 8));
        }
        return bytes;
    }
}
