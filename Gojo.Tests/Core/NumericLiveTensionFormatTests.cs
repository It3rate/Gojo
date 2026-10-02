using Gojo.Core;

namespace Gojo.Tests.Core;

public sealed class NumericLiveTensionFormatTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CompleteRestorationPreservesLivePressureAndHistoricalObservations(bool throughArchive)
    {
        var source = new Carrier();
        var opposite = source.Inverse();
        var mirror = new Carrier();
        source.Attach(new Relation(Relation.Point(source, 3), Relation.Point(source, 3)));
        source.AttachLiveOutput(source.LiveTensionSlot,
            new Relation(Relation.Segment(source, 0, 1), Relation.Segment(mirror, 0, 1)));
        source.Add(8);
        opposite.Remove(2);
        var root = Relation.OrderedGroup(new Carrier(), source, opposite, mirror,
            source.LiveTensionSlot, source.TensionSlot, source.ValueSlot, opposite.LiveTensionSlot,
            Carrier.Unknown.LiveTensionSlot, Carrier.Unknown.ValueSlot);

        var restored = throughArchive
            ? GojoEncodedSnapshot.CaptureArchive(root).RestoreRoots()[0]
            : GojoNumericFormat.Decode(GojoNumericFormat.Encode(root))[0];
        Assert.True(Assert.IsType<Relation>(restored).TryReadOrderedGroup(out var items));
        var readSource = Assert.IsType<Carrier>(items[0]);
        var readOpposite = Assert.IsType<Carrier>(items[1]);
        var readMirror = Assert.IsType<Carrier>(items[2]);
        var live = Assert.IsType<CarrierLocation>(items[3]);
        var historical = Assert.IsType<CarrierLocation>(items[4]);
        Assert.Same(readOpposite, readSource.Inverse());
        Assert.True(live.SameTickRefAs(readSource.LiveTensionSlot));
        Assert.False(historical.SameTickRefAs(readSource.LiveTensionSlot));
        Assert.True(Assert.IsType<CarrierLocation>(items[5]).SameTickRefAs(readSource.ValueSlot));
        Assert.True(Assert.IsType<CarrierLocation>(items[6]).SameTickRefAs(readOpposite.LiveTensionSlot));
        Assert.True(Assert.IsType<CarrierLocation>(items[7]).SameTickRefAs(Carrier.Unknown.LiveTensionSlot));
        Assert.True(Assert.IsType<CarrierLocation>(items[8]).SameTickRefAs(Carrier.Unknown.ValueSlot));
        Assert.Equal(5, live.Ticks);
        Assert.Equal(-2, readOpposite.Tension);

        readSource.Remove(2);
        Assert.Equal(3, live.Ticks);
        Assert.Equal(3UL, readMirror.Value);
        Assert.Equal(5, historical.Ticks);
        Assert.Equal(5, source.Tension);
        Assert.Equal(0, Carrier.Unknown.Tension);
    }

    [Fact]
    public void NumericVersionTwoRejectsMissingAliasedOrContradictoryPressureCoordinates()
    {
        var source = new Carrier();
        source.Remove(3);
        var graph = GojoEncodedSnapshot.CaptureGraph(source, source.LiveTensionSlot);
        var rows = graph.Carriers.ToArray();
        var original = rows[0];
        rows[0] = original with { TensionTickRefIndex = original.ValueTickRefIndex };
        Assert.Throws<InvalidDataException>(() => GojoNumericFormat.Encode(graph with { Carriers = rows }));
        rows[0] = original with { TensionTickRefIndex = -1 };
        Assert.Throws<InvalidDataException>(() => GojoNumericFormat.Encode(graph with { Carriers = rows }));
        rows[0] = original with { Tension = -4 };
        Assert.Throws<InvalidDataException>(() => GojoNumericFormat.Encode(graph with { Carriers = rows }));
    }

    [Fact]
    public void VersionOneKeepsPressureButDoesNotInventIdentityForAnOldObservation()
    {
        var bytes = VersionOnePressureFixture();
        var graph = GojoNumericFormat.ReadSnapshot(bytes);
        Assert.Equal(-1, graph.Carriers[0].TensionTickRefIndex);
        var roots = GojoNumericFormat.Decode(bytes);
        var carrier = Assert.IsType<Carrier>(roots[0]);
        var value = Assert.IsType<CarrierLocation>(roots[1]);
        var oldPressure = Assert.IsType<CarrierLocation>(roots[2]);
        Assert.True(value.SameTickRefAs(carrier.ValueSlot));
        Assert.False(oldPressure.SameTickRefAs(carrier.LiveTensionSlot));
        Assert.Equal(5, carrier.Tension);
        carrier.Add(1);
        Assert.Equal(9, value.Ticks);
        Assert.Equal(0, carrier.Tension);
        Assert.Equal(5, oldPressure.Ticks);

        var upgraded = GojoNumericFormat.ReadSnapshot(GojoNumericFormat.Encode(roots));
        Assert.True(upgraded.Carriers[0].TensionTickRefIndex >= 0);
        Assert.Equal(2, GojoNumericFormat.Version);
    }

    // Independent, minimal v1 wire fixture: one carrier with held value3 and
    // pressure5, a live value root, an old pressure observation, and atom unit.
    // It intentionally has no pressure-coordinate metadata: v1 never wrote it.
    private static byte[] VersionOnePressureFixture()
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);
        writer.Write(0x4F4A4F47u); writer.Write(1);
        foreach (var count in new[] { 3, 1, 3, 3, 1, 1, 0 }) writer.Write(count);
        Ref(GojoEncodedElementKind.Carrier, 0);
        Ref(GojoEncodedElementKind.CarrierLocation, 0);
        Ref(GojoEncodedElementKind.CarrierLocation, 1);
        writer.Write(1UL); writer.Write(false); writer.Write(false);
        writer.Write(3UL); writer.Write(5L); writer.Write(0); writer.Write(-1); writer.Write(0); writer.Write(0);
        writer.Write(1UL); writer.Write(3L);
        writer.Write(2UL); writer.Write(5L);
        writer.Write(3UL); writer.Write(1L);
        for (var index = 0; index < 3; index++) { writer.Write(0); writer.Write(index); }
        writer.Write(1UL); Ref(GojoEncodedElementKind.CarrierLocation, 2); Ref(GojoEncodedElementKind.Carrier, 0);
        writer.Write(1UL); Ref(GojoEncodedElementKind.Relation, 0); writer.Write(0);
        return stream.ToArray();

        void Ref(GojoEncodedElementKind kind, int index) { writer.Write((byte)kind); writer.Write(index); }
    }
}
