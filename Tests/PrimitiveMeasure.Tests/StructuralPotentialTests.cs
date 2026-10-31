using Gojo.Experiments.PrimitiveMeasure;

namespace Gojo.Experiments.PrimitiveMeasure.Tests;

public sealed class StructuralPotentialTests
{
    [Theory]
    [InlineData(0, 0)]
    [InlineData(0, 1)]
    [InlineData(1, 0)]
    [InlineData(1, 1)]
    public void EqualEmptyReadsRetainOneRoleTwoDistinctRolesAndOneRepeatedParticipant(int orientation, int direction)
    {
        var owner = new PortStructure();
        var a = owner.Create(orientation, direction, 1, 0);
        var b = owner.Create(orientation, direction, 1, 0);
        var reads = new ReadConnections();
        var first = Import(owner, reads, a);
        var second = Import(owner, reads, b);
        var identity = reads.AddTable(ConnectionLaws.Identity());
        var presence = reads.AddTable(ConnectionLaws.Presence());
        var exclusive = reads.AddTable(ConnectionLaws.ExclusivePresence());
        var single = reads.Connect(identity, 4, first);
        var pair = reads.Connect(presence, 4, first, second);
        var repeated = reads.Connect(presence, 4, first, first);
        var pairDifference = reads.Connect(exclusive, 4, first, second);
        var repeatedDifference = reads.Connect(exclusive, 4, first, first);
        var singleRow = Row(single);
        var pairRow = Row(pair);
        var repeatedRow = Row(repeated);
        var zero = Word(orientation, direction, false);
        var nonzero = Word(orientation, direction, true);

        Assert.NotEqual(owner.Locate(a), owner.Locate(b));
        Assert.NotEqual(first, second);
        Assert.Equal(new[] { first, single.Endpoint }, singleRow);
        Assert.Equal(new[] { first, second, pair.Endpoint }, pairRow);
        Assert.Equal(new[] { first, first, repeated.Endpoint }, repeatedRow);
        Assert.Equal(ReadSupport.Of(zero), single.ReadSupport());
        Assert.Equal(single.ReadSupport(), pair.ReadSupport());
        Assert.Equal(single.ReadSupport(), repeated.ReadSupport());
        Assert.Equal(ReadSupport.Of(zero), pairDifference.ReadSupport());
        Assert.Equal(ReadSupport.Of(zero), repeatedDifference.ReadSupport());

        // Supply a changed observation at A. Reading through either connection
        // does not populate B or duplicate material into the repeated role.
        owner.StoreObservation(a, orientation == 1, direction == 1, 1);
        Assert.Equal(ReadSupport.Of(nonzero), reads.Read(first));
        Assert.Equal(ReadSupport.Of(zero), reads.Read(second));
        Assert.Equal(ReadSupport.Of(nonzero), single.ReadSupport());
        Assert.Equal(single.ReadSupport(), pair.ReadSupport());
        Assert.Equal(single.ReadSupport(), repeated.ReadSupport());
        Assert.Equal(ReadSupport.Of(nonzero), pairDifference.ReadSupport());
        Assert.Equal(ReadSupport.Of(zero), repeatedDifference.ReadSupport());

        // Equal presence is again possible without making A and B the same site.
        owner.StoreObservation(b, orientation == 1, direction == 1, 1);
        Assert.Equal(ReadSupport.Of(zero), pairDifference.ReadSupport());
        Assert.Equal(ReadSupport.Of(zero), repeatedDifference.ReadSupport());
        Assert.Equal(singleRow, Row(single));
        Assert.Equal(pairRow, Row(pair));
        Assert.Equal(repeatedRow, Row(repeated));
        Assert.Equal(2, owner.LiveCount(orientation, direction, 1));
        Assert.Equal(new byte[] { 3 }, owner.PackedBytes(orientation, direction, 1));

        owner.StoreObservation(a, orientation == 1, direction == 1, 0);
        owner.StoreObservation(b, orientation == 1, direction == 1, 0);
        Assert.Equal(ReadSupport.Of(zero), single.ReadSupport());
        Assert.Equal(single.ReadSupport(), pair.ReadSupport());
        Assert.Equal(single.ReadSupport(), repeated.ReadSupport());
        Assert.Equal(ReadSupport.Of(zero), pairDifference.ReadSupport());
        Assert.Equal(ReadSupport.Of(zero), repeatedDifference.ReadSupport());
        Assert.Equal(pairRow, Row(pair));
        Assert.Equal(repeatedRow, Row(repeated));
        Assert.Equal(2, owner.LiveCount(orientation, direction, 1));

        int[] Row(CalculatedPort port)
        {
            var location = reads.LocationOf(port.Endpoint);
            return reads.Wiring(location.Table, location.Row);
        }
    }

    [Fact]
    public void ExclusivePresenceAdmitsOnlyTheTruthTuplesWithinOneOrientationAndDirection()
    {
        var law = ConnectionLaws.ExclusivePresence();
        for (byte first = 0; first < 8; first++)
            for (byte second = 0; second < 8; second++)
                for (byte result = 0; result < 8; result++)
                {
                    var context = first & 6;
                    var admitted = context == (second & 6) &&
                        result == (context | ((first ^ second) & 1));
                    Assert.Equal(admitted, law.Allows(first, second, result));
                }

        // Incompatible contexts have no admitted read, including when both V
        // bits are zero. An unavailable result is not a known empty value.
        Assert.Equal((byte)0, law.Predict([ReadSupport.Of(0), ReadSupport.Of(2)]));
        Assert.Equal((byte)0, law.Predict([ReadSupport.Of(0), ReadSupport.Of(4)]));
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(0, 1)]
    [InlineData(1, 0)]
    [InlineData(1, 1)]
    public void SharedUncertainParticipationCannotDisagreeWithItselfButDistinctReadsMay(int orientation, int direction)
    {
        var zero = Word(orientation, direction, false);
        var nonzero = Word(orientation, direction, true);
        var alternatives = (byte)(ReadSupport.Of(zero) | ReadSupport.Of(nonzero));
        var a = new ObservationPort(alternatives);
        var b = new ObservationPort(alternatives);
        var reads = new ReadConnections();
        var first = reads.Import(a);
        var second = reads.Import(b);
        var exclusive = reads.AddTable(ConnectionLaws.ExclusivePresence());
        var repeated = reads.Connect(exclusive, 3, first, first);
        var pair = reads.Connect(exclusive, 3, first, second);

        Assert.Equal(ReadSupport.Of(zero), repeated.ReadSupport());
        Assert.Equal(1, a.ReadCount);
        Assert.Equal(0, b.ReadCount);
        Assert.Equal(alternatives, pair.ReadSupport());
        Assert.Equal(2, a.ReadCount);
        Assert.Equal(1, b.ReadCount);

        // These are separate compatible refinements of the original unresolved
        // read. Distinct endpoints alone promise no statistical independence.
        foreach (var aWord in new[] { zero, nonzero })
            foreach (var bWord in new[] { zero, nonzero })
            {
                a.Support = ReadSupport.Of(aWord);
                b.Support = ReadSupport.Of(bWord);
                var refined = pair.ReadSupport();
                var expectedWord = (byte)((zero & 6) | ((aWord ^ bWord) & 1));
                Assert.Equal(ReadSupport.Of(expectedWord), refined);
                Assert.Equal(refined, (byte)(refined & alternatives));
                Assert.Equal(ReadSupport.Of(zero), repeated.ReadSupport());
            }

        var pairLocation = reads.LocationOf(pair.Endpoint);
        var repeatedLocation = reads.LocationOf(repeated.Endpoint);
        Assert.Equal(new[] { first, second, pair.Endpoint }, reads.Wiring(exclusive, pairLocation.Row));
        Assert.Equal(new[] { first, first, repeated.Endpoint }, reads.Wiring(exclusive, repeatedLocation.Row));
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(0, 1)]
    [InlineData(1, 0)]
    [InlineData(1, 1)]
    public void GrowingStoragePreservesRepeatedAndDistinctAttachmentsAndTheirCoarseReads(int orientation, int direction)
    {
        var owner = new PortStructure();
        var a = owner.Create(orientation, direction, 1, 1);
        var b = owner.Create(orientation, direction, 1, 0);
        var reads = new ReadConnections();
        var first = Import(owner, reads, a);
        var second = Import(owner, reads, b);
        var exclusive = reads.AddTable(ConnectionLaws.ExclusivePresence());
        var pair = reads.Connect(exclusive, 3, first, second);
        var repeated = reads.Connect(exclusive, 3, first, first);
        var pairLocation = reads.LocationOf(pair.Endpoint);
        var repeatedLocation = reads.LocationOf(repeated.Endpoint);
        var pairBefore = reads.Wiring(exclusive, pairLocation.Row);
        var repeatedBefore = reads.Wiring(exclusive, repeatedLocation.Row);
        var oldAddress = owner.Locate(a);
        var otherAddress = owner.Locate(b);
        var zero = Word(orientation, direction, false);
        var nonzero = Word(orientation, direction, true);

        Assert.True(owner.TryMove(a, orientation, direction, 3));
        Assert.NotEqual(oldAddress, owner.Locate(a));
        Assert.False(owner.TryReadLocation(oldAddress, out _));
        Assert.Equal(otherAddress, owner.Locate(b));
        Assert.Equal(ReadSupport.Of(nonzero), pair.ReadSupport());
        Assert.Equal(ReadSupport.Of(zero), repeated.ReadSupport());
        Assert.True(owner.TryReadLocation(owner.Locate(a), out var movedCode));
        Assert.Equal(1UL, movedCode); // More capacity did not discover a magnitude.

        // Supplied wider observations still expose the existing zero/nonzero
        // service. This checks coarsening, not a calibration or material change.
        for (ulong code = 0; code < 8; code++)
        {
            owner.StoreObservation(a, orientation == 1, direction == 1, code);
            Assert.Equal(ReadSupport.Of(code == 0 ? zero : nonzero), pair.ReadSupport());
            Assert.Equal(ReadSupport.Of(zero), repeated.ReadSupport());
            Assert.Equal(ReadSupport.Of(zero), reads.Read(second));
            Assert.Equal(pairBefore, reads.Wiring(exclusive, pairLocation.Row));
            Assert.Equal(repeatedBefore, reads.Wiring(exclusive, repeatedLocation.Row));
        }

        Assert.Equal(1, owner.LiveCount(orientation, direction, 1));
        Assert.Equal(1, owner.LiveCount(orientation, direction, 3));
    }

    private static int Import(PortStructure owner, ReadConnections reads, int binding) =>
        reads.Import(owner.Connect(owner.Publish(binding)));

    private static byte Word(int orientation, int direction, bool present) =>
        ThreeBitMeasure.Encode(orientation == 1, direction == 1, present);

    private sealed class ObservationPort(byte support) : IPrimitiveReadPort
    {
        public byte Support { get; set; } = support;
        public int ReadCount { get; private set; }
        public byte ReadSupport() { ReadCount++; return Support; }
    }
}
