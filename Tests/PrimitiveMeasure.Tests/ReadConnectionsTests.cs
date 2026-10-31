using Gojo.Experiments.PrimitiveMeasure;

namespace Gojo.Experiments.PrimitiveMeasure.Tests;

public sealed class ReadConnectionsTests
{
    [Fact]
    public void EveryAdditionRunsThroughTheConnectedPortsWithoutMutatingItsOperands()
    {
        var owner = new PortStructure();
        var a = CreateWord(owner, 0);
        var b = CreateWord(owner, 0);
        var network = new ReadConnections();
        var first = network.Import(Published(owner, a));
        var second = network.Import(Published(owner, b));
        var table = network.AddTable(ConnectionLaws.Addition());
        var result = network.Connect(table, 2, first, second);

        for (byte left = 0; left < 8; left++)
            for (byte right = 0; right < 8; right++)
            {
                StoreWord(owner, a, left);
                StoreWord(owner, b, right);
                var before = owner.PackedBytes(2, 0, 3);
                Assert.Equal(Mask(ThreeBitMeasure.Add(left, right)), result.ReadSupport());
                Assert.Equal(before, owner.PackedBytes(2, 0, 3));
                Assert.Equal(2, owner.LiveCount(2, 0, 3));
            }

        var location = network.LocationOf(result.Endpoint);
        Assert.Equal(new[] { first, second, result.Endpoint }, network.Wiring(table, location.Row));
        Assert.Equal(1, network.RowCount(table, 2));
    }

    [Fact]
    public void PresenceRefinesFourPartPatternsWithoutErasingZeroOrRepeatedEndpointRoles()
    {
        var owner = new PortStructure();
        var a = owner.Create(0, 0, 1, 0);
        var b = owner.Create(0, 0, 1, 0);
        var network = new ReadConnections();
        var first = network.Import(Published(owner, a));
        var second = network.Import(Published(owner, b));
        var table = network.AddTable(ConnectionLaws.Presence());
        var coarse = network.Connect(table, 3, first, second);
        var reversed = network.Connect(table, 3, second, first);
        var repeated = network.Connect(table, 3, first, first);
        var originalWiring = network.Wiring(table, network.LocationOf(coarse.Endpoint).Row);

        for (byte pattern = 0; pattern < 4; pattern++)
        {
            owner.StoreObservation(a, false, false, (ulong)(pattern & 1));
            owner.StoreObservation(b, false, false, (ulong)(pattern >> 1));
            Assert.Equal(ReadSupport.Of(pattern == 0 ? (byte)0 : (byte)1), coarse.ReadSupport());
            Assert.Equal(coarse.ReadSupport(), reversed.ReadSupport());
            Assert.Equal(ReadSupport.Of((byte)(pattern & 1)), repeated.ReadSupport());
            Assert.Equal(originalWiring, network.Wiring(table, network.LocationOf(coarse.Endpoint).Row));
        }

        Assert.NotEqual(first, second);
        Assert.Equal(new[] { first, second, coarse.Endpoint }, originalWiring);
        Assert.Equal(new[] { second, first, reversed.Endpoint }, network.Wiring(table, network.LocationOf(reversed.Endpoint).Row));
        Assert.Equal(new[] { first, first, repeated.Endpoint }, network.Wiring(table, network.LocationOf(repeated.Endpoint).Row));
        Assert.Equal(3, network.RowCount(table, 3));
    }

    [Fact]
    public void PresenceDoesNotTreatOpposedOrDifferentlyOrientedInputsAsTwoAlignedParts()
    {
        var owner = new PortStructure();
        var a = CreateWord(owner, 0);
        var b = CreateWord(owner, 0);
        var network = new ReadConnections();
        var table = network.AddTable(ConnectionLaws.Presence());
        var result = network.Connect(table, 2, network.Import(Published(owner, a)), network.Import(Published(owner, b)));

        for (byte left = 0; left < 8; left++)
            for (byte right = 0; right < 8; right++)
            {
                StoreWord(owner, a, left);
                StoreWord(owner, b, right);
                var expected = (left & 6) == (right & 6) ? ReadSupport.Of((byte)(left | right)) : (byte)0;
                Assert.Equal(expected, result.ReadSupport());
            }
    }

    [Fact]
    public void CalculatedOutputChangesImmediatelyWhileHeldOutputRequiresExplicitCapture()
    {
        var owner = new PortStructure();
        var a = owner.Create(0, 0, 1, 0);
        var b = owner.Create(0, 0, 1, 1);
        var held = owner.Create(0, 2, 2, 2); // Negative-marked zero is an actual prior sample.
        var heldPort = Published(owner, held);
        var network = new ReadConnections();
        var add = network.AddTable(ConnectionLaws.Addition());
        var calculated = network.Connect(add, 2, network.Import(Published(owner, a)), network.Import(Published(owner, b)));
        var identity = network.AddTable(ConnectionLaws.Identity());
        var capture = network.Wire(identity, 3, calculated.Endpoint, network.Import(heldPort));

        Assert.Equal(ReadSupport.Of(1), calculated.ReadSupport());
        Assert.Equal((byte)2, heldPort.Read());
        Assert.True(network.TryCapture(identity, capture, owner, held));
        Assert.Equal((byte)1, heldPort.Read());

        owner.StoreObservation(b, false, false, 0);
        Assert.Equal(ReadSupport.Of(0), calculated.ReadSupport());
        Assert.Equal((byte)1, heldPort.Read());
        Assert.Equal(ReadSupport.Of(0), network.Predict(identity, capture));
        Assert.True(network.TryCapture(identity, capture, owner, held));
        Assert.Equal((byte)0, heldPort.Read());
        Assert.Equal(2, owner.LiveCount(0, 0, 1));
        Assert.Equal(1, owner.LiveCount(0, 2, 2));
    }

    [Fact]
    public void CaptureRefusesAmbiguityAbsentReadsContextChangesAndWiderFieldsWithoutLosingHeldBits()
    {
        var owner = new PortStructure();
        var free = CreateWord(owner, 6);
        var fixedNegativeZero = owner.Create(0, 1, 1, 0);
        var fixedOrientation = owner.Create(0, 2, 2, 3);
        var wider = owner.Create(0, 2, 3, 7);

        AssertRejected(free, 0, Mask(0, 1));
        AssertRejected(fixedNegativeZero, ReadSupport.Of(0), ReadSupport.Of(1));
        AssertRejected(fixedOrientation, ReadSupport.Of(7));
        AssertRejected(wider, ReadSupport.Of(3));

        var source = new CountingPort(ReadSupport.Of(2));
        var network = new ReadConnections();
        var table = network.AddTable(ConnectionLaws.Identity());
        var targetPort = Published(owner, free);
        var row = network.Wire(table, 2, network.Import(source), network.Import(targetPort));
        Assert.True(network.TryCapture(table, row, owner, free));
        Assert.Equal((byte)2, targetPort.Read()); // A resolved zero keeps its supplied D bit.

        void AssertRejected(int binding, params byte[] supports)
        {
            var input = new CountingPort(0);
            var connection = new ReadConnections();
            var identity = connection.AddTable(ConnectionLaws.Identity());
            var output = Published(owner, binding);
            var capture = connection.Wire(identity, 2, connection.Import(input), connection.Import(output));
            var address = owner.Locate(binding);
            Assert.True(owner.TryReadLocation(address, out var beforeRaw));
            var beforeWord = output.Read();
            foreach (var support in supports)
            {
                input.Support = support;
                Assert.False(connection.TryCapture(identity, capture, owner, binding));
                Assert.True(owner.TryReadLocation(address, out var afterRaw));
                Assert.Equal(beforeRaw, afterRaw);
                Assert.Equal(beforeWord, output.Read());
            }
        }
    }

    [Fact]
    public void CaptureRequiresTheActualOutputBindingAndNeverWritesALookalikeOrCalculatedPort()
    {
        var owner = new PortStructure();
        var actual = owner.Create(0, 0, 1, 0);
        var lookalike = owner.Create(0, 0, 1, 0);
        var otherOwner = new PortStructure();
        var otherBinding = otherOwner.Create(0, 0, 1, 0);
        var actualPort = Published(owner, actual);
        var lookalikePort = Published(owner, lookalike);
        var otherPort = Published(otherOwner, otherBinding);
        var network = new ReadConnections();
        var source = network.Import(new CountingPort(ReadSupport.Of(1)));
        var table = network.AddTable(ConnectionLaws.Identity());
        var row = network.Wire(table, 2, source, network.Import(actualPort));

        Assert.False(network.TryCapture(table, row, owner, lookalike));
        Assert.False(network.TryCapture(table, row, otherOwner, otherBinding));
        Assert.Equal((byte)0, actualPort.Read());
        Assert.Equal((byte)0, lookalikePort.Read());
        Assert.Equal((byte)0, otherPort.Read());
        Assert.True(network.TryCapture(table, row, owner, actual));
        Assert.Equal((byte)1, actualPort.Read());
        Assert.Equal((byte)0, lookalikePort.Read());
        Assert.Equal((byte)0, otherPort.Read());

        var calculated = network.Connect(table, 2, source);
        var calculatedTarget = network.Wire(table, 2, source, calculated.Endpoint);
        Assert.False(network.TryCapture(table, calculatedTarget, owner, lookalike));
        Assert.Equal((byte)0, lookalikePort.Read());
    }

    [Fact]
    public void CalculatedConnectionFollowsItsPublishedSourceAcrossStorageMoves()
    {
        var owner = new PortStructure();
        var binding = owner.Create(0, 1, 1, 1);
        var port = Published(owner, binding);
        var network = new ReadConnections();
        var table = network.AddTable(ConnectionLaws.Identity());
        var source = network.Import(port);
        var calculated = network.Connect(table, 2, source);
        var row = network.LocationOf(calculated.Endpoint).Row;
        var wiring = network.Wiring(table, row);

        foreach (var (orientation, direction, width) in new[] { (0, 2, 2), (0, 2, 3), (2, 0, 4) })
        {
            var old = owner.Locate(binding);
            Assert.True(owner.TryMove(binding, orientation, direction, width));
            Assert.False(owner.TryReadLocation(old, out _));
            Assert.Equal(ReadSupport.Of(3), calculated.ReadSupport());
            Assert.Equal(wiring, network.Wiring(table, row));
            Assert.Equal(source, network.Import(port));
        }

        owner.StoreObservation(binding, true, false, 1);
        Assert.Equal(ReadSupport.Of(5), calculated.ReadSupport());
        Assert.Equal(wiring, network.Wiring(table, row));
    }

    [Fact]
    public void AnotherStructureCanImportACalculatedPortWithoutObtainingMaterialOrWriteAccess()
    {
        var owner = new PortStructure();
        var a = CreateWord(owner, 1);
        var b = CreateWord(owner, 3);
        var producer = new ReadConnections();
        var addition = producer.AddTable(ConnectionLaws.Addition());
        var output = producer.Connect(addition, 2, producer.Import(Published(owner, a)), producer.Import(Published(owner, b)));
        var consumer = new ReadConnections();
        var identity = consumer.AddTable(ConnectionLaws.Identity());
        var source = consumer.Import(output);
        var view = consumer.Connect(identity, 2, source);
        var before = owner.PackedBytes(2, 0, 3);

        Assert.Equal(Mask(0, 1, 2, 3), view.ReadSupport());
        Assert.Equal(before, owner.PackedBytes(2, 0, 3));
        var attemptedWrite = consumer.Wire(identity, 2, source, source);
        Assert.False(consumer.TryCapture(identity, attemptedWrite, owner, a));
        Assert.Equal(before, owner.PackedBytes(2, 0, 3));

        StoreWord(owner, b, 2);
        Assert.Equal(ReadSupport.Of(1), view.ReadSupport());
        Assert.Equal(2, owner.LiveCount(2, 0, 3));
    }

    [Fact]
    public void RepeatedEndpointsShareOneSampleAndOneCandidateChoiceButEqualMasksDoNot()
    {
        var network = new ReadConnections();
        var firstPort = new CountingPort(Mask(0, 1));
        var secondPort = new CountingPort(Mask(0, 1));
        var first = network.Import(firstPort);
        Assert.Equal(first, network.Import(firstPort));
        var second = network.Import(secondPort);
        var equality = network.AddTable(BitRelation.FromTuples(3,
            new byte[][] { [0, 0, 0], [1, 1, 0], [0, 1, 1], [1, 0, 1] }));
        var repeated = network.Connect(equality, 3, first, first);
        var separate = network.Connect(equality, 3, first, second);

        Assert.Equal(ReadSupport.Of(0), repeated.ReadSupport());
        Assert.Equal(1, firstPort.ReadCount);
        Assert.Equal(Mask(0, 1), separate.ReadSupport());
        Assert.Equal(2, firstPort.ReadCount);
        Assert.Equal(1, secondPort.ReadCount);

        var identity = network.AddTable(ConnectionLaws.Identity());
        var leftPath = network.Connect(identity, 4, first);
        var rightPath = network.Connect(identity, 4, first);
        var joined = network.Connect(equality, 4, leftPath.Endpoint, rightPath.Endpoint);
        Assert.Equal(Mask(0, 1), joined.ReadSupport()); // Cross-path correlation is deliberately still coarse.
        Assert.Equal(3, firstPort.ReadCount); // Both paths sampled the import once in this query.
        Assert.Equal(1, secondPort.ReadCount);
    }

    [Fact]
    public void FreshWrappersOfOnePublicPortShareAnEndpointButSeparateAliasPortsDoNot()
    {
        var owner = new PortStructure();
        var binding = owner.Create(0, 0, 1, 1);
        var publicPort = owner.Publish(binding);
        var aliasPort = owner.Publish(binding);
        var network = new ReadConnections();
        var first = network.Import(owner.Connect(publicPort));
        var samePublicPort = network.Import(owner.Connect(publicPort));
        var separatePublicPort = network.Import(owner.Connect(aliasPort));

        Assert.Equal(first, samePublicPort);
        Assert.NotEqual(first, separatePublicPort);
        Assert.Equal(ReadSupport.Of(1), network.Read(first));
        Assert.Equal(network.Read(first), network.Read(separatePublicPort));
    }

    [Fact]
    public void InvalidWiringDoesNotPublishARowOrConsumeACalculatedEndpointPosition()
    {
        var network = new ReadConnections();
        var source = network.Import(new CountingPort(ReadSupport.Of(1)));
        var table = network.AddTable(ConnectionLaws.Identity());
        Assert.Throws<ArgumentException>(() => network.Connect(table, 2));
        Assert.Throws<ArgumentException>(() => network.Connect(table, 2, source, source));
        Assert.Throws<ArgumentOutOfRangeException>(() => network.Connect(table, 2, -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => network.Connect(table, 2, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => network.Connect(table, 2, 2));
        Assert.Throws<ArgumentOutOfRangeException>(() => network.Connect(table, 0, source));
        Assert.Throws<ArgumentOutOfRangeException>(() => network.Connect(table, 17, source));
        Assert.Throws<ArgumentException>(() => network.Wire(table, 2, source));
        Assert.Throws<ArgumentOutOfRangeException>(() => network.Wire(table, 2, source, 2));
        Assert.Equal(0, network.RowCount(table, 2));
        Assert.Throws<ArgumentOutOfRangeException>(() => network.Read(1));

        var first = network.Connect(table, 1, source);
        Assert.Equal(1, first.Endpoint);
        Assert.Throws<ArgumentOutOfRangeException>(() => network.Connect(table, 1, source));
        Assert.Equal(1, network.RowCount(table, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => network.Read(3));
        var next = network.Connect(table, 2, source);
        Assert.Equal(3, next.Endpoint);
        Assert.Equal(ReadSupport.Of(1), first.ReadSupport());
        Assert.Equal(ReadSupport.Of(1), next.ReadSupport());
    }

    [Fact]
    public void LawAndWiringSnapshotsCannotRewriteTheLiveConnection()
    {
        var owner = new PortStructure();
        var network = new ReadConnections();
        var source = network.Import(Published(owner, CreateWord(owner, 3)));
        var table = network.AddTable(ConnectionLaws.Identity());
        var result = network.Connect(table, 2, source);
        var location = network.LocationOf(result.Endpoint);
        var originalLaw = network.LawBytes(table);
        var lawCopy = network.LawBytes(table);
        var rowCopy = network.PackedRows(table, 2);
        var wiringCopy = network.Wiring(table, location.Row);
        Array.Fill(lawCopy, (byte)0);
        Array.Fill(rowCopy, (byte)255);
        wiringCopy[0] = result.Endpoint;

        Assert.Equal(originalLaw, network.LawBytes(table));
        Assert.Equal(new byte[] { 4 }, network.PackedRows(table, 2));
        Assert.Equal(new[] { source, result.Endpoint }, network.Wiring(table, location.Row));
        Assert.Equal(ReadSupport.Of(3), result.ReadSupport());
    }

    private static PortConnection Published(PortStructure owner, int binding) => owner.Connect(owner.Publish(binding));
    private static int CreateWord(PortStructure owner, byte word) => owner.Create(2, 0, 3, word);
    private static void StoreWord(PortStructure owner, int binding, byte word) =>
        owner.StoreObservation(binding, (word & 4) != 0, (word & 2) != 0, (ulong)(word & 1));
    private static byte Mask(params byte[] words) => words.Aggregate((byte)0, (mask, word) => (byte)(mask | ReadSupport.Of(word)));

    private sealed class CountingPort(byte support) : IPrimitiveReadPort
    {
        public byte Support { get; set; } = support;
        public int ReadCount { get; private set; }
        public byte ReadSupport() { ReadCount++; return Support; }
    }
}
