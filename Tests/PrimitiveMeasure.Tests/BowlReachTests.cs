namespace Gojo.Experiments.PrimitiveMeasure.Tests;

public sealed class BowlReachTests
{
    [Fact]
    public void Equal_D_ink_can_widen_or_leave_the_request_at_the_pin()
    {
        var free = BowlReach.AdjustableD();
        var pinned = BowlReach.RadialD();
        Assert.Equal(free.Before,pinned.Before);
        Assert.Equal(free.Ink.Catalogue.Edge(1),pinned.Ink.Catalogue.Edge(1));
        Assert.Equal(free.Ink.Heading(1),pinned.Ink.Heading(1));
        Assert.Equal(0,pinned.Ink.CrosswiseStem());
        Assert.True(pinned.Ink.SourceRead!.Value.EqualParts);
        Assert.Equal(3,BowlMotionReader.Predict(free.Before));
        Assert.Equal(pinned.PinRows().Single()[1],pinned.TipRows().Single()[1]);
        Assert.NotEqual(free.PinRows().Single()[1],free.TipRows().Single()[1]);
        free.Push(); pinned.Push();
        Assert.Equal(2,BowlMotionReader.Compare(free.Before,free.ReadTips()));
        Assert.Equal(1,BowlMotionReader.Compare(pinned.Before,pinned.ReadTips()));
        Assert.False(free.Waiting); Assert.True(pinned.Waiting);
        Assert.Equal(new byte[]{0x28},free.StoreBytes());
        Assert.Equal(new byte[]{0x0A},pinned.StoreBytes());
        var after = free.StoreBytes(); free.Push();
        Assert.Equal(after,free.StoreBytes());
    }

    [Theory]
    [InlineData(false,2)] [InlineData(true,8)]
    public void The_lower_B_starts_wider_and_only_moves_on_a_shared_dial(bool shared,int expected)
    {
        var b = BowlReach.B(shared);
        var before = b.ReadTips();
        Assert.Equal(LandmarkRead.Before,BowlReach.Outward.Compare(before.Upper!.Value,before.Lower!.Value));
        Assert.Equal(0x0A,BowlMotionReader.Predict(before));
        Assert.Equal(shared,b.TipRows()[0][1] == b.TipRows()[1][1]);
        b.Push();
        var after = b.ReadTips();
        Assert.Equal(expected,BowlMotionReader.Compare(before,after));
        Assert.Equal(LandmarkRead.Before,BowlReach.Outward.Compare(after.Upper!.Value,after.Lower!.Value));
        Assert.Equal(new byte[]{0x28},b.StoreBytes()); // one request was absorbed in either case
        Assert.False(b.Waiting);
    }

    [Fact]
    public void Ends_only_cannot_turn_a_missing_tip_into_a_stationary_observation()
    {
        var open = BowlReach.EndsOnlyD();
        Assert.Null(open.ReadTips().Upper);
        Assert.Empty(open.SettleRows());
        open.Push();
        Assert.True(open.Waiting);
        Assert.Equal(3,BowlMotionReader.Compare(open.Before,open.ReadTips()));
        Assert.Equal(3,open.Ink.Read().BendSupport);
    }
}
