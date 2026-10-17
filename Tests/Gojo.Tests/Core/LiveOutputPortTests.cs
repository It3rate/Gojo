using Gojo.Core;

namespace Gojo.Tests.Core;

public sealed class LiveOutputPortTests
{
    [Fact]
    public void InputToProjectionPortContinuouslyFeedsConcreteOutputCarrier()
    {
        var source = new Carrier();
        var target = new Carrier();
        var port = new Relation(Segment(source, 0, 2), Segment(target, 0, 1));
        source.AttachLiveOutput(source.ValueSlot, port);

        source.Set(8);

        Assert.Equal(8UL, source.Value);
        Assert.Equal(4UL, target.Value);

        source.Set(10);

        Assert.Equal(10UL, source.Value);
        Assert.Equal(5UL, target.Value);
    }

    [Fact]
    public void InputGroupToOperationPortContinuouslyFeedsConcreteOutputCarrier()
    {
        var x = new Carrier();
        var y = new Carrier();
        var result = new Carrier();
        var xPort = new Relation(Point(x, 1), Point(Carrier.Unknown, 1));
        var yPort = new Relation(Point(y, 1), Point(Carrier.Unknown, 1));
        var operation = new Relation(new Relation(xPort, yPort), Segment(result, 0, 1));
        x.AttachLiveOutput(new Relation(x.ValueSlot, y.ValueSlot), operation);

        x.Set(3);
        y.Set(4);

        Assert.Equal(12UL, result.Value);

        x.Set(5);

        Assert.Equal(20UL, result.Value);
    }

    [Fact]
    public void LiveProjectionOutputDoesNotAccumulateReadResidueAsRepeatedTension()
    {
        var source = new Carrier();
        var target = new Carrier();
        var port = new Relation(Segment(source, 0, 3), Segment(target, 0, 1));
        source.Attach(new Relation(source.ValueSlot, port));

        source.Set(5);
        target.Add(0);

        Assert.Equal(1UL, target.Value);
        Assert.Equal(0, source.Tension);
    }

    private static Relation Point(Carrier carrier, long ticks) =>
        new(new CarrierLocation(carrier, ticks), carrier);

    private static Relation Segment(Carrier carrier, long left, long right) =>
        new(Point(carrier, left), Point(carrier, right));
}
