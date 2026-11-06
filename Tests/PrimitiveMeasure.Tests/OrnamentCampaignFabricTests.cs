using Gojo.Experiments.PrimitiveMeasure.OrnamentCampaign;
using System.Numerics;

namespace Gojo.Experiments.PrimitiveMeasure.Tests;

public sealed class OrnamentCampaignFabricTests
{
    [Fact]
    public void ASharedSignalDoesNotDuplicateTheWritersStock()
    {
        byte[] live = [2,2,2,2,2,2,2];
        ulong seed = 1UL << 48;
        var read = FabricTransfer.Draw(seed, live, live, 8, 1, 3);
        Assert.Equal(seed | 14UL, read.Ink);
        Assert.Equal(0, read.Stock);
        Assert.Equal(45, read.Refused);
        Assert.Equal(4, BitOperations.PopCount(read.Ink));
    }

    [Fact]
    public void TheSameOtherAxisBehavesDifferentlyWithMissingUnknownAndMutedPorts()
    {
        byte[] y = [2,1,1,1,1,1,1];
        var missing = FabricTransfer.Draw(0, new byte[7], y, 14, 0, 14);
        var ambiguous = FabricTransfer.Draw(0, Enumerable.Repeat((byte)3,7).ToArray(), y, 14, 0, 14);
        var muted = FabricTransfer.Draw(0, Enumerable.Repeat((byte)1,7).ToArray(), y, 14, 0, 14);
        Assert.Equal(new FabricRead(0,14,49,0), missing);
        Assert.Equal(new FabricRead(127,7,42,0), ambiguous);
        Assert.Equal(new FabricRead(127,7,0,0), muted);
    }
}
