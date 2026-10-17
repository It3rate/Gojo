using Gojo.Core;

namespace Gojo.Tests.Core;

public sealed class ElementDualityTests
{
    [Fact]
    public void CarrierDualIsStableOpposedFace()
    {
        var carrier = new Carrier();

        var dual = carrier.Dual();

        Assert.NotSame(carrier, dual);
        Assert.True(dual.IsInverse);
        Assert.Same(carrier, dual.Dual());
    }

    [Fact]
    public void CarrierLocationDualNegatesLocationOnSameCarrier()
    {
        var carrier = new Carrier();
        var location = new CarrierLocation(carrier, 5);

        var dual = location.Dual();

        Assert.Equal(new CarrierLocation(carrier, -5), dual);
        Assert.Equal(-1, dual.ReadFrom(carrier).Direction);
    }

    [Fact]
    public void RelationDualRecursesThroughPointAndNegatesLocations()
    {
        var carrier = new Carrier();
        var segment = new Relation(Point(carrier, 2), Point(carrier, 5));

        var dual = segment.Dual();

        Assert.Equal(
            new Relation(Point(carrier, -2), Point(carrier, -5)),
            dual);
    }

    [Fact]
    public void RelationReverseSwapsTraversalWithoutChangingFaces()
    {
        var carrier = new Carrier();
        var segment = new Relation(Point(carrier, 2), Point(carrier, 5));

        var reverse = segment.Reverse();

        Assert.Equal(new Relation(Point(carrier, 5), Point(carrier, 2)), reverse);
        Assert.Equal(segment, reverse.Reverse());
    }

    [Fact]
    public void DualAndReverseAreIndependentStructuralMoves()
    {
        var carrier = new Carrier();
        var segment = new Relation(Point(carrier, 2), Point(carrier, 5));

        Assert.Equal(segment.Dual().Reverse(), segment.Reverse().Dual());
    }

    private static Relation Point(Carrier carrier, long ticks) =>
        new(new CarrierLocation(carrier, ticks), carrier);
}
