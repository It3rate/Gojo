using Gojo.Experiments.PrimitiveMeasure;

namespace Gojo.Experiments.PrimitiveMeasure.Tests;

public sealed class ConditionalTests
{
    [Fact]
    public void BuildersEncodeEachResolvedTransitionInTheBitmap()
    {
        var laws = new[] { PassageLaw.Controlled(), PassageLaw.Controlled(false), PassageLaw.Unconditional() };
        var bytes = new byte[][]
        {
            [0x01, 0x02, 0x04, 0x00, 0x10, 0x20, 0x48, 0x80],
            [0x01, 0x00, 0x04, 0x08, 0x12, 0x20, 0x40, 0x80],
            [0x01, 0x00, 0x04, 0x00, 0x12, 0x20, 0x48, 0x80]
        };
        for (var lawIndex = 0; lawIndex < laws.Length; lawIndex++)
        {
            Assert.Equal(bytes[lawIndex], laws[lawIndex].PackedBytes());
            for (byte before = 0; before < 8; before++)
            {
                var open = lawIndex == 2 || ((before & 2) != 0) == (lawIndex == 0);
                var expected = open && (before & 5) == 1 ? (byte)((before & 2) | 4) : before;
                Assert.Equal((byte)(1 << expected), laws[lawIndex].Predict((byte)(1 << before)));
                for (byte candidate = 0; candidate < 8; candidate++)
                    Assert.Equal(candidate == expected, laws[lawIndex].Allows(before, candidate));
            }
        }
        Assert.Equal(PassageLaw.Controlled(true).PackedBytes(), PassageLaw.Controlled().PackedBytes());
    }

    [Fact]
    public void RowsEncodeTheSameFivePortsWithDifferentOrderedRoles()
    {
        var circuit = new Circuit(3);
        Assert.Equal(new[] { 0, 1, 2 }, circuit.Present.Wiring(circuit.Yes));
        Assert.Equal(new[] { 0, 1, 3 }, circuit.Absent.Wiring(circuit.No));
        Assert.Equal(new[] { 2, 1, 4 }, circuit.Forward.Wiring(circuit.YesNext));
        Assert.Equal(new[] { 3, 1, 4 }, circuit.Forward.Wiring(circuit.NoNext));
        Assert.Equal(new byte[] { 0x88, 0x00 }, circuit.Present.PackedRows(3));
        Assert.Equal(new byte[] { 0xC8, 0x00 }, circuit.Absent.PackedRows(3));
        Assert.Equal(new byte[] { 0x0A, 0x17, 0x02 }, circuit.Forward.PackedRows(3));
        Assert.Equal(5, circuit.Owner.LiveCount(0, 0, 1));
    }

    [Theory]
    [InlineData(0, 0, false)]
    [InlineData(0, 1, false)]
    [InlineData(1, 0, false)]
    [InlineData(1, 1, false)]
    [InlineData(0, 0, true)]
    [InlineData(0, 1, true)]
    [InlineData(1, 0, true)]
    [InlineData(1, 1, true)]
    public void ResolvedSelectionMovesToOneOutletThenAnExplicitContinuation(int orientation, int direction, bool control)
    {
        var circuit = new Circuit(control ? 3 : 1, orientation, direction);
        var before = circuit.Present.ObserveScope(circuit.SelectionPorts, 15);
        Assert.True(PassageConnections.TrySequence(circuit.SelectionPorts, before, out var selected,
            (circuit.Present, circuit.Yes), (circuit.Absent, circuit.No)));
        Assert.Equal(control ? (byte)6 : (byte)8, selected);
        Assert.Equal(new[] { selected }, circuit.Owner.PackedBytes(orientation, direction, 1));
        Assert.Equal(0, circuit.Owner.ReadPort(circuit.Ports[4]) & 1);

        // Both continuation rows may be visited: only the occupied inlet can move.
        Assert.True(circuit.Forward.TryStep(circuit.YesNext, circuit.Forward.Observe(circuit.YesNext), out _));
        Assert.True(circuit.Forward.TryStep(circuit.NoNext, circuit.Forward.Observe(circuit.NoNext), out _));
        var completed = control ? (byte)0x12 : (byte)0x10;
        Assert.Equal(new[] { completed }, circuit.Owner.PackedBytes(orientation, direction, 1));
        for (var site = 0; site < 5; site++)
            Assert.Equal(ThreeBitMeasure.Encode(orientation == 1, direction == 1, (completed & (1 << site)) != 0),
                circuit.Owner.ReadPort(circuit.Ports[site]));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ChangingControlAfterSelectionDoesNotChangeTheOccupiedPath(bool originalControl)
    {
        var circuit = new Circuit(originalControl ? 3 : 1);
        Assert.True(circuit.Select(out _));
        circuit.Owner.StoreObservation(circuit.Bindings[1], false, false, originalControl ? 0UL : 1UL);
        var selectedAfterControlChange = originalControl ? 4 : 10;
        Assert.True(circuit.Select(out var selectedAgain));
        Assert.Equal((byte)selectedAfterControlChange, selectedAgain);
        Assert.True(circuit.Forward.TryStep(circuit.YesNext, circuit.Forward.Observe(circuit.YesNext), out _));
        Assert.True(circuit.Forward.TryStep(circuit.NoNext, circuit.Forward.Observe(circuit.NoNext), out _));
        Assert.Equal(new byte[] { originalControl ? (byte)0x10 : (byte)0x12 }, circuit.Owner.PackedBytes(0, 0, 1));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    public void AnEmptySourceDoesNotCreateFlowFromEitherControlState(int state)
    {
        var circuit = new Circuit(state);
        Assert.True(circuit.Select(out var after));
        Assert.Equal((byte)state, after);
        Assert.True(circuit.Forward.TryStep(circuit.YesNext, circuit.Forward.Observe(circuit.YesNext), out _));
        Assert.True(circuit.Forward.TryStep(circuit.NoNext, circuit.Forward.Observe(circuit.NoNext), out _));
        Assert.Equal(new byte[] { (byte)state }, circuit.Owner.PackedBytes(0, 0, 1));
    }

    [Theory]
    [InlineData(7)]
    [InlineData(9)]
    public void AnOccupiedSelectedOutletHoldsSourceInsteadOfFallingThrough(int state)
    {
        var circuit = new Circuit(state);
        Assert.True(circuit.Select(out var after));
        Assert.Equal((byte)state, after);
        Assert.Equal(new byte[] { (byte)state }, circuit.Owner.PackedBytes(0, 0, 1));
    }

    [Fact]
    public void ReversingSelectorOrderPreservesEveryResolvedWholeState()
    {
        for (var state = 0; state < 32; state++)
        {
            var forward = new Circuit(state);
            var reverse = new Circuit(state);
            Assert.True(forward.Select(out var forwardAfter));
            var before = reverse.Present.ObserveScope(reverse.SelectionPorts, 15);
            Assert.True(PassageConnections.TrySequence(reverse.SelectionPorts, before, out var reverseAfter,
                (reverse.Absent, reverse.No), (reverse.Present, reverse.Yes)));
            Assert.Equal(forwardAfter, reverseAfter);
            Assert.Equal(forward.Owner.PackedBytes(0, 0, 1), reverse.Owner.PackedBytes(0, 0, 1));
            Assert.Equal(state & 16, forward.Owner.PackedBytes(0, 0, 1)[0] & 16);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void UnselectedOutletKeepsEarlierContentsAndCanIndependentlyContinue(bool control)
    {
        var circuit = new Circuit(control ? 0x0B : 0x05);
        Assert.True(circuit.Select(out var selected));
        Assert.Equal(control ? (byte)0x0E : (byte)0x0C, selected);
        // The branch not receiving this source is not globally disabled.
        var earlierContents = control ? circuit.NoNext : circuit.YesNext;
        Assert.True(circuit.Forward.TryStep(earlierContents, circuit.Forward.Observe(earlierContents), out _));
        Assert.Equal(new byte[] { control ? (byte)0x16 : (byte)0x18 }, circuit.Owner.PackedBytes(0, 0, 1));
        var newlySelected = control ? circuit.YesNext : circuit.NoNext;
        Assert.True(circuit.Forward.TryStep(newlySelected, circuit.Forward.Observe(newlySelected), out _));
        Assert.Equal(new byte[] { control ? (byte)0x16 : (byte)0x18 }, circuit.Owner.PackedBytes(0, 0, 1));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void UnknownControlPreservesJointPathsWithoutExecutingEitherPrediction(bool actualControl)
    {
        var circuit = new Circuit(actualControl ? 3 : 1);
        var before = circuit.Present.ObserveScope(circuit.Ports, 0b11101);
        var afterYes = circuit.Present.PredictScope(circuit.Yes, circuit.Ports, before);
        var selected = circuit.Absent.PredictScope(circuit.No, circuit.Ports, afterYes);
        var afterYesNext = circuit.Forward.PredictScope(circuit.YesNext, circuit.Ports, selected);
        var completed = circuit.Forward.PredictScope(circuit.NoNext, circuit.Ports, afterYesNext);
        Assert.Equal(0x0Au, before);
        Assert.Equal(0x42u, afterYes);
        Assert.Equal(0x140u, selected);
        Assert.Equal(0x40100u, afterYesNext);
        Assert.Equal(0x50000u, completed);
        Assert.Equal(new[] { 6, 8 }, States(selected));
        Assert.Equal(new[] { 16, 18 }, States(completed));
        foreach (var support in new[] { before, afterYes, selected, afterYesNext, completed })
            foreach (var state in States(support))
                Assert.Equal(1, new[] { 0, 2, 3, 4 }.Count(bit => (state & (1 << bit)) != 0));

        // Every possible completed state has Done=1; that fact still cannot
        // choose which unknown path to execute in the current actual structure.
        Assert.All(States(completed), state => Assert.NotEqual(0, state & 16));
        Assert.False(PassageConnections.TrySequence(circuit.Ports, before, out _,
            (circuit.Present, circuit.Yes), (circuit.Absent, circuit.No),
            (circuit.Forward, circuit.YesNext), (circuit.Forward, circuit.NoNext)));
        Assert.Equal(new byte[] { actualControl ? (byte)3 : (byte)1 }, circuit.Owner.PackedBytes(0, 0, 1));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void AStaleParticipantRefusesTheWholeSelectionUntilItsObservationIsRefreshed(int site)
    {
        var circuit = new Circuit(3);
        var before = circuit.Present.ObserveScope(circuit.SelectionPorts, 15);
        var changed = 3 ^ (1 << site);
        circuit.Owner.StoreObservation(circuit.Bindings[site], false, false, (ulong)((changed >> site) & 1));
        Assert.False(PassageConnections.TrySequence(circuit.SelectionPorts, before, out _,
            (circuit.Present, circuit.Yes), (circuit.Absent, circuit.No)));
        Assert.Equal(new byte[] { (byte)changed }, circuit.Owner.PackedBytes(0, 0, 1));
        var refreshed = circuit.Present.ObserveScope(circuit.SelectionPorts, 15);
        var expected = circuit.Absent.PredictScope(circuit.No, circuit.SelectionPorts,
            circuit.Present.PredictScope(circuit.Yes, circuit.SelectionPorts, refreshed));
        Assert.True(PassageConnections.TrySequence(circuit.SelectionPorts, refreshed, out var after,
            (circuit.Present, circuit.Yes), (circuit.Absent, circuit.No)));
        Assert.Equal(expected, 1u << after);
    }

    [Fact]
    public void UnrelatedDoneChangeDoesNotStaleTheFourSiteSelectionScope()
    {
        var circuit = new Circuit(3);
        var before = circuit.Present.ObserveScope(circuit.SelectionPorts, 15);
        circuit.Owner.StoreObservation(circuit.Bindings[4], false, false, 1);
        Assert.True(PassageConnections.TrySequence(circuit.SelectionPorts, before, out var after,
            (circuit.Present, circuit.Yes), (circuit.Absent, circuit.No)));
        Assert.Equal((byte)6, after);
        Assert.Equal(new byte[] { 0x16 }, circuit.Owner.PackedBytes(0, 0, 1));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ABlockedOrAmbiguousLaterStepCannotPartiallyCommitAnEarlierMove(bool ambiguous)
    {
        var circuit = new Circuit(3);
        var bitmap = new byte[8];
        if (ambiguous)
        {
            bitmap[3] = 1 << 3;
            bitmap[6] = 1 << 3;
        }
        var later = new PassageConnections(circuit.Owner, new PassageLaw(bitmap));
        var row = later.Connect(3, circuit.Ports[2], circuit.Ports[1], circuit.Ports[4]);
        Assert.False(PassageConnections.TrySequence(circuit.Ports, 1u << 3, out _,
            (circuit.Present, circuit.Yes), (later, row)));
        Assert.Equal(new byte[] { 3 }, circuit.Owner.PackedBytes(0, 0, 1));
    }

    [Fact]
    public void MixedOwnersCannotBeIncludedInOneAtomicSelection()
    {
        var first = new Circuit(3);
        var second = new Circuit(1);
        Assert.False(PassageConnections.TrySequence(first.Ports, 1u << 3, out _,
            (first.Present, first.Yes), (second.Absent, second.No)));
        Assert.Equal(new byte[] { 3 }, first.Owner.PackedBytes(0, 0, 1));
        Assert.Equal(new byte[] { 1 }, second.Owner.PackedBytes(0, 0, 1));
    }

    [Fact]
    public void DuplicateBindingsCannotPretendToBeSeparateScopeSites()
    {
        var circuit = new Circuit(3);
        var alias = circuit.Owner.Publish(circuit.Bindings[2]);
        var aliasNo = circuit.Absent.Connect(3, circuit.Ports[0], circuit.Ports[1], alias);
        int[] scope = [circuit.Ports[0], circuit.Ports[1], circuit.Ports[2], alias];
        Assert.False(PassageConnections.TrySequence(scope, 1u << 3, out _,
            (circuit.Present, circuit.Yes), (circuit.Absent, aliasNo)));
        Assert.Equal(new byte[] { 3 }, circuit.Owner.PackedBytes(0, 0, 1));
    }

    [Fact]
    public void ARowOutsideTheSelectedScopeCannotPartiallyCommitTheSequence()
    {
        var circuit = new Circuit(3);
        Assert.False(PassageConnections.TrySequence(circuit.SelectionPorts, 1u << 3, out _,
            (circuit.Present, circuit.Yes), (circuit.Forward, circuit.YesNext)));
        Assert.Equal(new byte[] { 3 }, circuit.Owner.PackedBytes(0, 0, 1));
    }

    [Theory]
    [InlineData(0, 0, 2)]
    [InlineData(0, 2, 2)]
    [InlineData(2, 0, 3)]
    public void AChangedRepresentationInvalidatesTheSequenceBeforeAnyWrite(int orientation, int direction, int width)
    {
        var circuit = new Circuit(3);
        var before = circuit.Present.ObserveScope(circuit.SelectionPorts, 15);
        Assert.True(circuit.Owner.TryMove(circuit.Bindings[3], orientation, direction, width));
        var actual = circuit.Ports.Select(circuit.Owner.ReadPort).ToArray();
        Assert.False(PassageConnections.TrySequence(circuit.SelectionPorts, before, out _,
            (circuit.Present, circuit.Yes), (circuit.Absent, circuit.No)));
        Assert.Equal(actual, circuit.Ports.Select(circuit.Owner.ReadPort).ToArray());
    }

    [Fact]
    public void EmptySupportAndAnEmptyStepListDoNotCommitAnything()
    {
        var circuit = new Circuit(3);
        Assert.False(PassageConnections.TrySequence(circuit.SelectionPorts, 0, out _,
            (circuit.Present, circuit.Yes), (circuit.Absent, circuit.No)));
        Assert.Throws<ArgumentException>(() => PassageConnections.TrySequence(circuit.SelectionPorts, 1u << 3, out _));
        Assert.Equal(new byte[] { 3 }, circuit.Owner.PackedBytes(0, 0, 1));
    }

    [Fact]
    public void FiveSiteScopeAcceptsTheHighestStateBitAndReturnsItsStateCode()
    {
        var circuit = new Circuit(31);
        Assert.True(PassageConnections.TrySequence(circuit.Ports, 1u << 31, out var after,
            (circuit.Present, circuit.Yes), (circuit.Absent, circuit.No)));
        Assert.Equal((byte)31, after);
        Assert.Equal(new byte[] { 31 }, circuit.Owner.PackedBytes(0, 0, 1));
    }

    [Fact]
    public void ThreeSiteSequenceUsesScopeOrderRatherThanPortNumbers()
    {
        var circuit = new Circuit(3);
        int[] scope = [circuit.Ports[2], circuit.Ports[0], circuit.Ports[1]];
        Assert.Equal(1u << 6, circuit.Present.ObserveScope(scope, 7));
        Assert.True(PassageConnections.TrySequence(scope, 1u << 6, out var after,
            (circuit.Present, circuit.Yes)));
        Assert.Equal((byte)5, after);
        Assert.Equal(new byte[] { 6 }, circuit.Owner.PackedBytes(0, 0, 1));
    }

    [Fact]
    public void OneRowsControlCanParticipateAsMaterialInAnotherExplicitRow()
    {
        var circuit = new Circuit(3);
        var later = circuit.Forward.Connect(3, circuit.Ports[1], circuit.Ports[2], circuit.Ports[3]);
        Assert.True(PassageConnections.TrySequence(circuit.SelectionPorts, 1u << 3, out var after,
            (circuit.Present, circuit.Yes), (circuit.Forward, later)));
        Assert.Equal((byte)12, after);
        Assert.Equal(new byte[] { 12 }, circuit.Owner.PackedBytes(0, 0, 1));
    }

    private static int[] States(uint support) =>
        Enumerable.Range(0, 32).Where(state => (support & (1u << state)) != 0).ToArray();

    private sealed class Circuit
    {
        public PortStructure Owner { get; } = new();
        public int[] Bindings { get; }
        public int[] Ports { get; }
        public int[] SelectionPorts => Ports[..4];
        public PassageConnections Present { get; }
        public PassageConnections Absent { get; }
        public PassageConnections Forward { get; }
        public ConnectionRow Yes { get; }
        public ConnectionRow No { get; }
        public ConnectionRow YesNext { get; }
        public ConnectionRow NoNext { get; }

        public Circuit(int state, int orientation = 0, int direction = 0)
        {
            Bindings = Enumerable.Range(0, 5)
                .Select(site => Owner.Create(orientation, direction, 1, (ulong)((state >> site) & 1))).ToArray();
            Ports = Bindings.Select(Owner.Publish).ToArray();
            Present = new PassageConnections(Owner, PassageLaw.Controlled());
            Absent = new PassageConnections(Owner, PassageLaw.Controlled(false));
            Forward = new PassageConnections(Owner, PassageLaw.Unconditional());
            Yes = Present.Connect(3, Ports[0], Ports[1], Ports[2]);
            No = Absent.Connect(3, Ports[0], Ports[1], Ports[3]);
            YesNext = Forward.Connect(3, Ports[2], Ports[1], Ports[4]);
            NoNext = Forward.Connect(3, Ports[3], Ports[1], Ports[4]);
        }

        public bool Select(out byte after) => PassageConnections.TrySequence(SelectionPorts,
            Present.ObserveScope(SelectionPorts, 15), out after, (Present, Yes), (Absent, No));
    }
}
