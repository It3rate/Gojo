using Gojo.Experiments.OrnamentTensionSeries;

namespace Gojo.Tests.OrnamentTension;

public class SeriesTests
{
    [Fact]
    public void RecurringReadSwitchesTrayAndSendsAtMostTheAvailableSignedCharge()
    {
        var owner=new SeriesOwner([15,10,10,4]);
        Assert.Equal(0,owner.Next(SeriesMode.Live).Tool);
        owner.Receive(0x24);
        Assert.False(owner.Next(SeriesMode.Live).Echo);
        owner.Receive(0x34);
        var repeat=owner.Next(SeriesMode.Live);
        Assert.True(repeat.Echo);
        Assert.Equal(1,repeat.Tool);
        Assert.Equal(1,repeat.Spent);
        Assert.Equal(-1,repeat.Pulse);
        Assert.Equal(0,repeat.After);
    }

    [Fact]
    public void MaskedAliasCreatesEqualSubsequentDecisionsWithoutSharingAFrieze()
    {
        var a=new SeriesOwner([3,10,10,8]);
        var b=new SeriesOwner([3,10,10,8]);
        foreach(byte publication in new byte[]{1,2,1,1,3,2,3,2})
        {
            Assert.Equal(a.Next(SeriesMode.Live),b.Next(SeriesMode.Live));
            a.Receive(publication);
            b.Receive((byte)(publication|0xF0));
        }
        Assert.Equal(a.Next(SeriesMode.Live),b.Next(SeriesMode.Live));
    }

    [Fact]
    public void FixedClockAndHoldExposeDifferentTraysWithoutSpendingAnIncomingPulse()
    {
        var clock=new SeriesOwner([255,15,15,0]);
        var hold=new SeriesOwner([255,15,15,0]);
        for(int i=0;i<8;i++)
        {
            var c=clock.Next(SeriesMode.Clock);
            var h=hold.Next(SeriesMode.Hold);
            Assert.Equal(i&1,c.Tool);Assert.Equal(0,h.Tool);
            Assert.Equal(0,c.Pulse);Assert.Equal(0,h.Pulse);
            if(i==7){Assert.Equal(3,h.Before);Assert.Equal(1,h.Overflow);}
            clock.Receive(11);hold.Receive(11);
        }
    }
}
