using Gojo.Core;

namespace Gojo.Tests.Core;

public sealed class DualCarrierFootholdTests
{
    [Fact]
    public void NegativeTensionRoutesThroughExplicitDualCarrierReceiver()
    {
        var source = new Carrier();
        var inverse = new Carrier(isInverse: true);
        source.Attach(new Relation(Point(source, 10), Point(inverse, 0)));

        source.Remove(23);

        Assert.Equal(-3, source.Tension);
        Assert.Equal(2UL, inverse.Value);
    }

    [Fact]
    public void RelatedDualCarrierBalancesOpposedVisibleValues()
    {
        var source = new Carrier();
        var inverse = new Carrier(isInverse: true);
        source.Attach(new Relation(Point(source, 1), Point(inverse, 0)));
        source.Add(5);

        inverse.Add(7);

        Assert.Equal(0UL, source.Value);
        Assert.Equal(2UL, inverse.Value);
    }

    [Fact]
    public void OpposedResidualCanFeedLaterDualCarrierRelationsInSameWorld()
    {
        var source = new Carrier();
        var inverse = new Carrier(isInverse: true);
        var target = new Carrier(isInverse: true);
        source.Attach(new Relation(Point(source, 1), Point(inverse, 0)));
        inverse.Attach(new Relation(Point(inverse, 1), Point(target, 0)));
        source.Add(5);

        inverse.Add(7);

        Assert.Equal(0UL, source.Value);
        Assert.Equal(0UL, inverse.Value);
        Assert.Equal(2UL, target.Value);
    }

    private static Relation Point(Carrier carrier, long ticks) =>
        new(new CarrierLocation(carrier, ticks), carrier);
}
