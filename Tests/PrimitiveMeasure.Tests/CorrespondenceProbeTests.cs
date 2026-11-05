using Gojo.Experiments.PrimitiveMeasure;

namespace Gojo.Experiments.PrimitiveMeasure.Tests;

public sealed class CorrespondenceProbeTests
{
    [Fact]
    public void EqualityUsesEveryOdvBitAndThePackedRelationCanBeRebound()
    {
        var law = CorrespondenceProbe.EqualWords();
        var left = new Observation(1);
        var right = new Observation(1);
        var probe = new CorrespondenceProbe(law, left, right);
        for (byte first = 0; first < 8; first++)
        for (byte second = 0; second < 8; second++)
        {
            left.Support = ReadSupport.Of(first);
            right.Support = ReadSupport.Of(second);
            Assert.Equal((byte)(first == second ? 2 : 1), probe.ReadSupport());
        }

        var other = new CorrespondenceProbe(law, new Observation(2), new Observation(2));
        Assert.Same(law, probe.Law);
        Assert.Same(law, other.Law);
        left.Support = 1;
        right.Support = 2;
        Assert.Equal((byte)1, probe.ReadSupport());
        Assert.Equal((byte)2, other.ReadSupport());
        Assert.Equal(new[] { 0, 2, 1 }, probe.Wiring());
        Assert.Equal(2, probe.ReferenceBits);
        Assert.Equal(new byte[] { 0x18 }, probe.RowBytes());
    }

    [Fact]
    public void ReorientationIsCheckedAsCorrespondenceRatherThanUnchangedBits()
    {
        byte[] map = Enumerable.Range(0, 8).Select(word => ThreeBitMeasure.Reorient((byte)word)).ToArray();
        var law = CorrespondenceProbe.Through(map);
        var first = new Observation(1);
        var second = new Observation(1);
        var transformed = new CorrespondenceProbe(law, first, second);
        var unchanged = new CorrespondenceProbe(CorrespondenceProbe.EqualWords(), first, second);
        for (byte word = 0; word < 8; word++)
        {
            first.Support = ReadSupport.Of(word);
            second.Support = ReadSupport.Of(ThreeBitMeasure.Reorient(word));
            Assert.Equal((byte)2, transformed.ReadSupport());
            Assert.Equal((byte)1, unchanged.ReadSupport());
            second.Support = ReadSupport.Of(ThreeBitMeasure.ReverseDirection(word));
            Assert.Equal((byte)1, transformed.ReadSupport());
        }
        Assert.Equal(map, Enumerable.Range(0, 8).Select(word => ThreeBitMeasure.Reorient((byte)word)).ToArray());
    }

    [Theory]
    [InlineData(3, 3, 3)]
    [InlineData(255, 255, 3)]
    [InlineData(1, 3, 3)]
    [InlineData(1, 2, 1)]
    [InlineData(3, 12, 1)]
    [InlineData(0, 3, 0)]
    [InlineData(2, 0, 0)]
    [InlineData(0, 0, 0)]
    public void IndependentSupportsDoNotManufactureAgreement(byte first, byte second, byte expected)
    {
        var probe = new CorrespondenceProbe(CorrespondenceProbe.EqualWords(), new Observation(first), new Observation(second));
        Assert.Equal(expected, probe.ReadSupport());
    }

    [Fact]
    public void RepeatedActualPortRetainsCorrespondenceAndIsSampledOnce()
    {
        var source = new Observation(255);
        var probe = new CorrespondenceProbe(CorrespondenceProbe.EqualWords(), source, source);
        Assert.Equal((byte)2, probe.ReadSupport());
        Assert.Equal(1, source.Reads);
        Assert.Equal(new[] { 0, 0, 1 }, probe.Wiring());
        Assert.Equal(1, probe.ReferenceBits);
        Assert.Equal(new byte[] { 4 }, probe.RowBytes());
        source.Support = 0;
        Assert.Equal((byte)0, probe.ReadSupport());
    }

    [Fact]
    public void IndependentHandlesForTheSamePublishedPortRetainTheirSharedEndpoint()
    {
        var owner = new PortStructure();
        var binding = owner.Create(0, 0, 1, 1);
        var published = owner.Publish(binding);
        var probe = new CorrespondenceProbe(CorrespondenceProbe.EqualWords(), owner.Connect(published), owner.Connect(published));
        Assert.Equal(new[] { 0, 0, 1 }, probe.Wiring());
        Assert.Equal((byte)2, probe.ReadSupport());
    }

    [Fact]
    public void CapturedReadStaysHistoricalWhileItsPublishedSourceChanges()
    {
        var owner = new PortStructure();
        var binding = owner.Create(0, 0, 1, 1);
        var live = owner.Connect(owner.Publish(binding));
        var captured = CapturedRead.Capture(live);
        var probe = new CorrespondenceProbe(CorrespondenceProbe.EqualWords(), captured, live);
        Assert.Same(live, captured.Source);
        Assert.Equal((byte)2, probe.ReadSupport());
        owner.StoreObservation(binding, false, false, 0);
        Assert.Equal((byte)2, captured.ReadSupport());
        Assert.Equal((byte)1, live.ReadSupport());
        Assert.Equal((byte)1, probe.ReadSupport());
    }

    [Fact]
    public void TwoUnknownCapturesDoNotClaimCrossTimeIdentity()
    {
        var live = new Observation(3);
        var before = CapturedRead.Capture(live);
        var after = CapturedRead.Capture(live);
        Assert.Same(before.Source, after.Source);
        Assert.Equal((byte)3, new CorrespondenceProbe(CorrespondenceProbe.EqualWords(), before, after).ReadSupport());
        Assert.Equal((byte)2, new CorrespondenceProbe(CorrespondenceProbe.EqualWords(), before, before).ReadSupport());
    }

    [Fact]
    public void AConsumerOnlyNeedsTheProbePortAndNoMaterialIsMoved()
    {
        var owner = new PortStructure();
        var a = owner.Create(0, 0, 1, 1);
        var b = owner.Create(0, 0, 1, 1);
        var probe = new CorrespondenceProbe(CorrespondenceProbe.EqualWords(), owner.Connect(owner.Publish(a)), owner.Connect(owner.Publish(b)));
        var consumer = new ReadConnections();
        var table = consumer.AddTable(ConnectionLaws.Identity());
        var read = consumer.Connect(table, 1, consumer.Import(probe));
        var before = owner.PackedBytes(0, 0, 1);
        for (var i = 0; i < 8; i++) Assert.Equal((byte)2, read.ReadSupport());
        Assert.Equal(before, owner.PackedBytes(0, 0, 1));
        Assert.Equal(2, owner.LiveCount(0, 0, 1));
    }

    [Fact]
    public void ReconstructedTupleBitsControlTheAnswerAndDiagnosticsCannotMutateThem()
    {
        var law = CorrespondenceProbe.EqualWords();
        var bitmap = law.PackedBytes();
        var copy = new BitRelation(3, bitmap);
        Array.Fill(bitmap, (byte)0);
        var before = new Observation(2);
        var after = new Observation(2);
        var probe = new CorrespondenceProbe(copy, before, after);
        Assert.Equal((byte)2, probe.ReadSupport());
        var diagnostic = probe.LawBytes();
        Array.Fill(diagnostic, (byte)0);
        var rows = probe.RowBytes();
        Array.Fill(rows, (byte)0);
        Assert.Equal((byte)2, probe.ReadSupport());
        Assert.Equal(law.PackedBytes(), probe.LawBytes());

        var opposite = BitRelation.FromTuples(3, new[] { new byte[] { 1, 1, 0 } });
        Assert.Equal((byte)1, new CorrespondenceProbe(opposite, before, after).ReadSupport());
        after.Support = 1;
        Assert.Equal((byte)0, new CorrespondenceProbe(opposite, before, after).ReadSupport());
    }

    [Fact]
    public void NonInvertibleMapsRemainPossibleWithoutBeingCalledSymmetries()
    {
        var collapse = CorrespondenceProbe.Through([0, 0, 0, 0, 0, 0, 0, 0]);
        var source = new Observation(255);
        var zero = new Observation(1);
        Assert.Equal((byte)2, new CorrespondenceProbe(collapse, source, zero).ReadSupport());
        Assert.Equal((byte)3, new CorrespondenceProbe(CorrespondenceProbe.EqualWords(), source, zero).ReadSupport());
    }

    [Fact]
    public void MalformedHostGrammarIsRejectedWithoutInventingAnAnswer()
    {
        var sample = new Observation(2);
        Assert.Throws<ArgumentException>(() => new CorrespondenceProbe(ConnectionLaws.Identity(), sample, sample));
        Assert.Throws<ArgumentException>(() => new CorrespondenceProbe(BitRelation.FromTuples(3, new[] { new byte[] { 1, 1, 2 } }), sample, sample));
        Assert.Throws<ArgumentException>(() => CorrespondenceProbe.Through([0, 1]));
        Assert.Throws<ArgumentOutOfRangeException>(() => CorrespondenceProbe.Through([0, 1, 2, 3, 4, 5, 6, 8]));
        Assert.Throws<ArgumentNullException>(() => CapturedRead.Capture(null!));
    }

    private sealed class Observation(byte support) : IPrimitiveReadPort
    {
        public byte Support { get; set; } = support;
        public int Reads { get; private set; }
        public byte ReadSupport() { Reads++; return Support; }
    }
}
