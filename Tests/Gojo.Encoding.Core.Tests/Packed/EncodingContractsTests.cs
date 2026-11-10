using Gojo.Encoding.Core.Packed;

namespace Gojo.Encoding.Core.Tests.Packed;

public sealed class EncodingContractsTests
{
    [Fact]
    public void RelationBitsEncodeOrderedRolesWithoutAnOperationTag()
    {
        var relation = BitRelation.FromTuples(3, [[0, 0, 0], [1, 2, 3], [7, 7, 7]]);
        var expected = new byte[64];
        expected[0] = 1;
        expected[26] = 2; // Bit 1 + 8*2 + 64*3 = 209.
        expected[63] = 128;
        Assert.Equal(expected, relation.PackedBytes());
        Assert.True(relation.Allows(1, 2, 3));
        Assert.False(relation.Allows(3, 2, 1));
        Assert.Equal(ReadSupport.Of(3), relation.Predict([ReadSupport.Of(1), ReadSupport.Of(2)]));
        expected[26] = 0;
        Assert.True(relation.Allows(1, 2, 3)); // Diagnostic bytes are a copy.
    }

    [Fact]
    public void EveryOdvWordSurvivesPackingAndZeroRemainsAnAdmittedRead()
    {
        var words = Enumerable.Range(0, 8).Select(i => (byte)i).ToArray();
        var packed = ThreeBitMeasure.Pack(words);
        Assert.Equal(3, packed.Length);
        for (var i = 0; i < words.Length; i++)
        {
            Assert.Equal(words[i], ThreeBitMeasure.At(packed, i));
            Assert.Equal(words[i], ThreeBitMeasure.Reorient(ThreeBitMeasure.Reorient(words[i])));
            Assert.Equal(words[i], ThreeBitMeasure.ReverseDirection(ThreeBitMeasure.ReverseDirection(words[i])));
        }
        Assert.Equal((byte)1, ReadSupport.Of(0));
        Assert.True(ReadSupport.TrySingle(1, out var undetectable));
        Assert.Equal((byte)0, undetectable);
        Assert.False(ReadSupport.TrySingle(0, out _));
        Assert.False(ReadSupport.TrySingle(3, out _));
        Assert.Throws<ArgumentOutOfRangeException>(() => ThreeBitMeasure.Pack([8]));
        Assert.Throws<ArgumentOutOfRangeException>(() => ThreeBitMeasure.At(packed, 8));
    }

    [Fact]
    public void FixedFramesAreNotWritableAndDirtyRetirementDoesNotChangeNeighbourReads()
    {
        var storage = new PositionalBuckets();
        var slots = Enumerable.Range(0, 25).Select(i => storage.Append(1, 0, 3, (ulong)(i % 8))).ToArray();
        Assert.Throws<ArgumentException>(() => PositionalBuckets.Encode(1, 0, 3, false, false, 1));
        Assert.Throws<ArgumentException>(() => PositionalBuckets.Encode(1, 0, 3, true, true, 1));
        var before = storage.PackedBytes(1, 0, 3);
        Assert.True(storage.Retire(slots[7]));
        Assert.Equal(before, storage.PackedBytes(1, 0, 3));
        Assert.False(storage.TryRead(slots[7], out _));
        for (var i = 0; i < slots.Length; i++)
            if (i != 7)
            {
                Assert.True(storage.TryRead(slots[i], out var code));
                Assert.Equal((ulong)(i % 8), code);
            }
        Assert.True(storage.TryRead(slots[0], out var zeroCode));
        Assert.Equal(0UL, zeroCode);
        Assert.Equal((byte)4, PositionalBuckets.CoarseRead(slots[0], zeroCode));
        Assert.Equal(25, storage.Count(1, 0, 3));
        Assert.Equal(24, storage.LiveCount(1, 0, 3));
    }

    [Fact]
    public void PortsRemainStableWhenOwnedBitsAndStorageCapacityChange()
    {
        for (byte word = 0; word < 8; word++)
        {
            var owner = new PortStructure();
            var binding = owner.Create(word >> 2, (word >> 1) & 1, 1, (ulong)(word & 1));
            var port = owner.Connect(owner.Publish(binding));
            var oldAddress = owner.Locate(binding);
            var oldBytes = owner.PackedBytes(oldAddress.OrientationGroup, oldAddress.DirectionGroup, 1);
            Assert.True(owner.TryMove(binding, word >> 2, 2, 2));
            Assert.True(owner.TryMove(binding, 2, 0, 6));
            Assert.Equal(word, port.Read());
            Assert.Equal(word, port.Read()); // Observation never drains its source.
            Assert.False(owner.TryReadLocation(oldAddress, out _));
            Assert.Equal(oldBytes, owner.PackedBytes(oldAddress.OrientationGroup, oldAddress.DirectionGroup, 1));
            Assert.False(owner.TryMove(binding, 0, 0, 1));
            Assert.Equal(word, port.Read());
        }
    }

    [Fact]
    public void SharedReferencesKeepCorrespondenceButEqualSupportsDoNotEstablishIt()
    {
        var xor = ConnectionLaws.ExclusivePresence();
        Assert.Equal((byte)3, xor.Predict([3, 3], [0, 1]));
        Assert.Equal((byte)1, xor.Predict([3, 3], [0, 0]));
        Assert.Equal((byte)0, xor.Predict([1, 2], [0, 0]));
        Assert.Equal((byte)2, xor.Predict([1, 2], [0, 1]));
        Assert.Equal((byte)0, xor.Predict([0, 2]));

        var observation = new MutableRead(3);
        var reads = new ReadConnections();
        var input = reads.Import(observation);
        var sameInput = reads.Import(observation);
        Assert.Equal(input, sameInput);
        var output = reads.Connect(reads.AddTable(xor), 2, input, sameInput);
        Assert.Equal((byte)1, output.ReadSupport());
        Assert.Equal(1, observation.Reads);
    }

    [Fact]
    public void SelectedDetectionLawDoesNotInventMappingAcrossFrames()
    {
        var either = ConnectionLaws.Presence();
        Assert.Equal((byte)1, either.Predict([ReadSupport.Of(0), ReadSupport.Of(0)]));
        Assert.Equal((byte)0, either.Predict([ReadSupport.Of(1), ReadSupport.Of(5)]));
        // This is OR of observed bits, not addition of represented quantities.
        // A caller-selected frame map can be expressed as its own relation.
        var frame = BitRelation.FromTuples(2, Enumerable.Range(0, 8)
            .Select(word => new[] { (byte)word, ThreeBitMeasure.Reorient((byte)word) }));
        Assert.Equal(ReadSupport.Of(7), frame.Predict([ReadSupport.Of(1)]));
        Assert.Equal(ReadSupport.Of(6), frame.Predict([ReadSupport.Of(0)]));
    }

    [Fact]
    public void PartialOrderDoesNotConfuseStoredOrderWithClaimedOrder()
    {
        var chain = new LandmarkRun(3, [4, 1, 6]);
        var partial = new LandmarkRun(3, [4, 1, 6], [[4, 6]]);
        Assert.Equal(LandmarkRead.Before, chain.Compare(4, 1));
        Assert.Equal(LandmarkRead.Unresolved, partial.Compare(4, 1));
        Assert.Equal(LandmarkRead.Before, partial.Compare(4, 6));
        Assert.Equal(LandmarkRead.After, partial.Compare(4, 6, reversed: true));
        Assert.Equal(3, partial.Orders().Count());
        Assert.Equal(LandmarkRead.Outside, partial.Compare(0, 1));
        var contradiction = new LandmarkRun(3, [4, 1, 6], [[4, 1], [1, 6], [6, 4]]);
        Assert.Equal(LandmarkRead.Conflict, contradiction.Compare(4, 6));
        Assert.Empty(contradiction.Orders());
    }

    [Fact]
    public void BalancedIncidenceDoesNotProveOneOrderedLoopAndPartialReadCannotCertifyClosure()
    {
        var disconnected = new OrderedBoundary(4, [[0, 1], [1, 0], [2, 3], [3, 2]], [0, 2, 4, 6]);
        var complete = disconnected.Read();
        Assert.True(complete.Balanced);
        Assert.True(complete.EvenParity);
        Assert.False(complete.OrderedClosed);
        Assert.Equal(2, complete.VisitedComponents);
        var partial = disconnected.Read(2);
        Assert.False(partial.Complete);
        Assert.Null(partial.Balanced);
        Assert.Null(partial.OrderedClosed);

        var cycle = new OrderedBoundary(3, [[0, 1], [1, 2], [2, 0]], [0, 2, 4]);
        Assert.Equal("Contradiction", StrictOrderRead.Evaluate(cycle, [2, 2, 2]));
        Assert.Equal("Unresolved", StrictOrderRead.Evaluate(cycle, [2, 2, 3]));
        Assert.Equal("Unavailable", StrictOrderRead.Evaluate(cycle, [2, 2, 0]));
    }

    private sealed class MutableRead(byte support) : IPrimitiveReadPort
    {
        public int Reads { get; private set; }
        public byte ReadSupport() { Reads++; return support; }
    }
}
