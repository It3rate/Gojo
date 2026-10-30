using Gojo.Experiments.PrimitiveMeasure;

namespace Gojo.Experiments.PrimitiveMeasure.Tests;

public sealed class LocalConnectionTests
{
    [Fact]
    public void ChartBytesNameActualPositionsInStoredOrder()
    {
        Assert.Equal((byte)0x1A, LocalConnection.Link(2, 3));
        Assert.Equal((byte)0x3E, LocalConnection.Link(6, 7));
        Assert.Equal((ushort)0xF9A, LocalConnection.Pair(0x1A, 0x3E));
        Assert.Equal((byte)0x1A, LocalConnection.At(0xF9A, 0));
        Assert.Equal((byte)0x3E, LocalConnection.At(0xF9A, 1));
        for (var source = 0; source < 8; source++)
            for (var target = 0; target < 8; target++)
            {
                var link = LocalConnection.Link(source, target);
                Assert.Equal(source, LocalConnection.Source(link));
                Assert.Equal(target, LocalConnection.Target(link));
                Assert.Equal(source == target, link == LocalConnection.Link(target, source));
            }
    }

    [Fact]
    public void ComplementaryContactsUseOneSourceBitWithoutAnOpeningCache()
    {
        // Material B+ -> A+; control attaches to A's presence bit, b2.
        var whenAbsent = LocalConnection.When(LocalConnection.Link(3, 2), 2, false);
        var whenPresent = LocalConnection.When(LocalConnection.Link(3, 2), 2, true);
        Assert.Equal((ushort)0x093, whenAbsent);
        Assert.Equal((ushort)0x293, whenPresent);
        Assert.Equal((byte)0x13, LocalConnection.MaterialLink(whenAbsent));
        Assert.Equal(2, LocalConnection.ControlSite(whenAbsent));
        for (var tile = 0; tile < 256; tile++)
        {
            var first = LocalConnection.IsOpen((byte)tile, whenAbsent);
            var second = LocalConnection.IsOpen((byte)tile, whenPresent);
            Assert.NotEqual(first, second);
            Assert.Equal((tile & 4) == 0, first);
        }
    }

    [Fact]
    public void EndpointEncodingDoesNotConfuseInvalidWidthsWithRealSites()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => LocalConnection.Link(-1, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => LocalConnection.Link(0, 8));
        Assert.Throws<ArgumentOutOfRangeException>(() => LocalConnection.Source(64));
        Assert.Throws<ArgumentOutOfRangeException>(() => LocalConnection.At(0x1000, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => LocalConnection.At(0, 2));
        Assert.Throws<ArgumentOutOfRangeException>(() => LocalConnection.IsOpen(0x20, 0x400));
    }
}
