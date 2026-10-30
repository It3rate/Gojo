using Gojo.Experiments.PrimitiveMeasure;

namespace Gojo.Experiments.PrimitiveMeasure.Tests;

public sealed class PortStructureTests
{
    [Fact]
    public void AllEightWordsKeepTheirPortsAndConnectionsAcrossRepresentationMoves()
    {
        for (byte word = 0; word < 8; word++)
        {
            var owner = new PortStructure();
            var orientation = (word >> 2) & 1;
            var direction = (word >> 1) & 1;
            var valueCode = (ulong)(word & 1);
            var binding = owner.Create(orientation, direction, 1, valueCode);
            var firstPort = owner.Publish(binding);
            var aliasPort = owner.Publish(binding);
            var firstConnection = owner.Connect(firstPort);
            var aliasConnection = owner.Connect(aliasPort);

            Assert.NotEqual(firstPort, aliasPort);
            Assert.Equal(firstPort, firstConnection.PortIndex);
            Assert.Equal(aliasPort, aliasConnection.PortIndex);
            Assert.Equal(word, firstConnection.Read());

            Assert.True(owner.TryMove(binding, orientation, 2, 2));
            Assert.True(owner.TryReadLocation(owner.Locate(binding), out var signedRaw));
            Assert.Equal(valueCode | ((ulong)direction << 1), signedRaw);
            Assert.Equal(word, firstConnection.Read());
            Assert.Equal(word, aliasConnection.Read());

            // Extra storage capacity supplies no finer observation by itself.
            Assert.True(owner.TryMove(binding, orientation, 2, 3));
            Assert.True(owner.TryReadLocation(owner.Locate(binding), out var widerRaw));
            Assert.Equal(valueCode | ((ulong)direction << 2), widerRaw);
            Assert.Equal(word, firstConnection.Read());
            Assert.Equal(word, aliasConnection.Read());

            Assert.True(owner.TryMove(binding, 2, 0, 4));
            Assert.True(owner.TryReadLocation(owner.Locate(binding), out var freeRaw));
            Assert.Equal(valueCode | ((ulong)direction << 2) | ((ulong)orientation << 3), freeRaw);
            Assert.Equal(word, owner.ReadPort(firstPort));
            Assert.Equal(word, owner.ReadPort(aliasPort));
            Assert.Equal(word, firstConnection.Read());
            Assert.Equal(word, aliasConnection.Read());
            Assert.Equal(firstPort, firstConnection.PortIndex);
            Assert.Equal(aliasPort, aliasConnection.PortIndex);
        }
    }

    [Fact]
    public void RetiredSlotsKeepDirtyBytesButCannotBeReadOrReused()
    {
        var owner = new PortStructure();
        var binding = owner.Create(0, 0, 1, 1);
        var connection = owner.Connect(owner.Publish(binding));
        var oldLocation = owner.Locate(binding);
        var oldBytes = owner.PackedBytes(0, 0, 1);

        Assert.True(owner.TryMove(binding, 0, 2, 2));
        Assert.False(owner.TryReadLocation(oldLocation, out _));
        Assert.Equal(oldBytes, owner.PackedBytes(0, 0, 1));
        Assert.Equal(1, owner.Count(0, 0, 1));
        Assert.Equal(0, owner.LiveCount(0, 0, 1));
        Assert.Equal((byte)1, connection.Read());

        var laterBinding = owner.Create(0, 0, 1, 0);
        Assert.NotEqual(oldLocation, owner.Locate(laterBinding));
        Assert.False(owner.TryReadLocation(oldLocation, out _));
        Assert.True(owner.TryReadLocation(owner.Locate(laterBinding), out var laterRaw));
        Assert.Equal(0UL, laterRaw);
        Assert.Equal(new byte[] { 1 }, owner.PackedBytes(0, 0, 1));
        Assert.Equal(2, owner.Count(0, 0, 1));
        Assert.Equal(1, owner.LiveCount(0, 0, 1));
    }

    [Fact]
    public void OnlyReleasedBitsCanChangeThroughObservationWrites()
    {
        var owner = new PortStructure();
        var binding = owner.Create(0, 1, 1, 1);
        var connection = owner.Connect(owner.Publish(binding));

        Assert.ThrowsAny<ArgumentException>(() => owner.StoreObservation(binding, false, false, 1));
        Assert.ThrowsAny<ArgumentException>(() => owner.StoreObservation(binding, true, true, 1));
        Assert.ThrowsAny<ArgumentException>(() => owner.StoreObservation(binding, false, true, 2));
        Assert.Equal((byte)3, connection.Read());
        Assert.Equal(new byte[] { 1 }, owner.PackedBytes(0, 1, 1));

        Assert.True(owner.TryMove(binding, 0, 2, 2));
        owner.StoreObservation(binding, false, false, 1);
        Assert.Equal((byte)1, connection.Read());
        Assert.ThrowsAny<ArgumentException>(() => owner.StoreObservation(binding, true, false, 1));
        Assert.Equal((byte)1, connection.Read());

        Assert.True(owner.TryMove(binding, 2, 0, 3));
        owner.StoreObservation(binding, true, true, 1);
        Assert.Equal((byte)7, connection.Read());
        Assert.Equal(new byte[] { 1 }, owner.PackedBytes(0, 1, 1));
    }

    [Fact]
    public void RejectedOrMalformedMovesLeaveEveryExistingReadAndByteUnchanged()
    {
        var owner = new PortStructure();
        var binding = owner.Create(0, 1, 2, 3);
        var port = owner.Publish(binding);
        var alias = owner.Connect(owner.Publish(binding));
        var beforeLocation = owner.Locate(binding);
        var beforeBytes = owner.PackedBytes(0, 1, 2);

        Assert.False(owner.TryMove(binding, 1, 1, 2));
        Assert.False(owner.TryMove(binding, 0, 0, 2));
        Assert.False(owner.TryMove(binding, 0, 2, 2)); // Would shrink the value payload.
        Assert.Throws<ArgumentOutOfRangeException>(() => owner.TryMove(binding, 3, 0, 3));
        Assert.Throws<ArgumentOutOfRangeException>(() => owner.TryMove(binding, 0, 3, 3));
        Assert.Throws<ArgumentOutOfRangeException>(() => owner.TryMove(binding, 0, 1, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => owner.TryMove(binding, 2, 0, 2));

        Assert.Equal(beforeLocation, owner.Locate(binding));
        Assert.Equal(beforeBytes, owner.PackedBytes(0, 1, 2));
        Assert.True(owner.TryReadLocation(beforeLocation, out var raw));
        Assert.Equal(3UL, raw);
        Assert.Equal((byte)3, owner.ReadPort(port));
        Assert.Equal((byte)3, alias.Read());
        Assert.Equal(1, owner.Count(0, 1, 2));
        Assert.Equal(1, owner.LiveCount(0, 1, 2));
        Assert.Equal(0, owner.Count(1, 1, 2));
        Assert.Equal(0, owner.Count(0, 0, 2));
        Assert.Equal(0, owner.Count(0, 2, 2));
    }

    [Fact]
    public void SameLayoutIsANoOpAndReleasedConstraintsCannotBeReimposed()
    {
        var owner = new PortStructure();
        var binding = owner.Create(1, 1, 2, 3);
        var connection = owner.Connect(owner.Publish(binding));
        var original = owner.Locate(binding);

        Assert.True(owner.TryMove(binding, 1, 1, 2));
        Assert.Equal(original, owner.Locate(binding));
        Assert.Equal(1, owner.Count(1, 1, 2));
        Assert.Equal(1, owner.LiveCount(1, 1, 2));

        Assert.True(owner.TryMove(binding, 1, 2, 3));
        var signed = owner.Locate(binding);
        Assert.False(owner.TryMove(binding, 1, 1, 3));
        Assert.Equal(signed, owner.Locate(binding));
        Assert.True(owner.TryReadLocation(signed, out var signedRaw));
        Assert.Equal(7UL, signedRaw); // Both value-code bits survive releasing D.

        Assert.True(owner.TryMove(binding, 2, 0, 4));
        var free = owner.Locate(binding);
        Assert.False(owner.TryMove(binding, 1, 2, 4));
        Assert.False(owner.TryMove(binding, 2, 0, 3));
        Assert.Equal(free, owner.Locate(binding));
        Assert.True(owner.TryReadLocation(free, out var freeRaw));
        Assert.Equal(15UL, freeRaw);
        Assert.Equal((byte)7, connection.Read());
    }

    [Fact]
    public void EqualPortNumbersRemainLocalToTheirOwningStructures()
    {
        var first = new PortStructure();
        var second = new PortStructure();
        var firstBinding = first.Create(1, 0, 1, 1);
        var secondBinding = second.Create(1, 1, 1, 0);
        var firstPort = first.Publish(firstBinding);
        var secondPort = second.Publish(secondBinding);
        var firstConnection = first.Connect(firstPort);
        var secondConnection = second.Connect(secondPort);
        var secondLocation = second.Locate(secondBinding);
        var secondBytes = second.PackedBytes(1, 1, 1);

        Assert.Equal(firstPort, secondPort);
        Assert.Equal((byte)5, firstConnection.Read());
        Assert.Equal((byte)6, secondConnection.Read());

        Assert.True(first.TryMove(firstBinding, 1, 2, 2));
        first.StoreObservation(firstBinding, true, true, 1);

        Assert.Equal((byte)7, firstConnection.Read());
        Assert.Equal((byte)6, secondConnection.Read());
        Assert.Equal(secondLocation, second.Locate(secondBinding));
        Assert.Equal(secondBytes, second.PackedBytes(1, 1, 1));
        Assert.Equal(1, second.LiveCount(1, 1, 1));
    }

    [Fact]
    public void MovingAndUpdatingOnePackedEntryPreservesItsNeighbours()
    {
        var owner = new PortStructure();
        var bindings = Enumerable.Range(0, 20).Select(i => owner.Create(0, 0, 1, (ulong)(i & 1))).ToArray();
        var connections = bindings.Select(binding => owner.Connect(owner.Publish(binding))).ToArray();
        var destinationFirst = owner.Create(0, 2, 3, 5);
        var destinationSecond = owner.Create(0, 2, 3, 2);
        var sourceBytes = owner.PackedBytes(0, 0, 1);
        var moved = bindings[9];
        var retired = owner.Locate(moved);

        Assert.True(owner.TryMove(moved, 0, 2, 3));
        owner.StoreObservation(moved, false, true, 3);

        for (var index = 0; index < connections.Length; index++)
            Assert.Equal(index == 9 ? (byte)3 : (byte)(index & 1), connections[index].Read());
        Assert.Equal(sourceBytes, owner.PackedBytes(0, 0, 1));
        Assert.False(owner.TryReadLocation(retired, out _));
        Assert.Equal(20, owner.Count(0, 0, 1));
        Assert.Equal(19, owner.LiveCount(0, 0, 1));
        Assert.Equal(3, owner.Count(0, 2, 3));
        Assert.Equal(3, owner.LiveCount(0, 2, 3));
        Assert.True(owner.TryReadLocation(owner.Locate(destinationFirst), out var firstRaw));
        Assert.True(owner.TryReadLocation(owner.Locate(destinationSecond), out var secondRaw));
        Assert.Equal(5UL, firstRaw);
        Assert.Equal(2UL, secondRaw);
    }

    [Fact]
    public void ZeroAndUnpublishedValuesRemainLiveWithoutImmediateCollection()
    {
        var owner = new PortStructure();
        var unpublishedZero = owner.Create(0, 0, 1, 0);
        var unpublishedNonzero = owner.Create(0, 0, 1, 1);
        var publishedZero = owner.Create(0, 0, 1, 0);
        var unusedPort = owner.Publish(publishedZero);
        // An exposed empty site can later receive an observation; no consumer is required.
        var availableInput = owner.Create(0, 0, 1, 0);
        var inputConnection = owner.Connect(owner.Publish(availableInput));

        Assert.True(owner.TryMove(unpublishedNonzero, 0, 2, 2));
        foreach (var binding in new[] { unpublishedZero, publishedZero, availableInput })
        {
            Assert.True(owner.TryReadLocation(owner.Locate(binding), out var raw));
            Assert.Equal(0UL, raw);
        }
        Assert.True(owner.TryReadLocation(owner.Locate(unpublishedNonzero), out var nonzero));
        Assert.Equal(1UL, nonzero);
        Assert.Equal(4, owner.Count(0, 0, 1));
        Assert.Equal(3, owner.LiveCount(0, 0, 1));
        Assert.Equal((byte)0, owner.ReadPort(unusedPort));
        Assert.Equal((byte)0, inputConnection.Read());

        Assert.True(owner.TryMove(availableInput, 0, 2, 2));
        owner.StoreObservation(availableInput, false, true, 1);
        Assert.Equal((byte)3, inputConnection.Read());
        Assert.Equal((byte)0, owner.ReadPort(unusedPort));
        Assert.True(owner.TryReadLocation(owner.Locate(unpublishedZero), out var stillZero));
        Assert.Equal(0UL, stillZero);
    }
}
