using Gojo.Core;
using Gojo.Equations;

namespace Gojo.Tests.Core;

public sealed class LiveTensionSlotTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void LivePressureTracksPositiveNegativeAndZeroWhileObservationsRemainFixed(bool inverse)
    {
        var forward = new Carrier();
        var source = inverse ? forward.Inverse() : forward;
        var live = source.LiveTensionSlot;
        var zeroObservation = source.TensionSlot;
        source.Attach(new Relation(Relation.Point(source, 3), Relation.Point(source, 3)));

        source.Add(8);
        var positiveObservation = source.TensionSlot;
        Assert.Equal(5, live.Ticks);
        Assert.Equal(5, source.Tension);
        Assert.Equal(0, zeroObservation.Ticks);
        Assert.True(live.SameTickRefAs(source.LiveTensionSlot));
        Assert.False(live.SameTickRefAs(positiveObservation));

        source.Remove(10);
        Assert.Equal(-2, live.Ticks);
        Assert.Equal(-2, source.Tension);
        Assert.Equal(5, positiveObservation.Ticks);
        source.Add(2);
        Assert.Equal(0, live.Ticks);
        Assert.Equal(0, source.Tension);
        Assert.Same(source, live.Carrier);
        Assert.Equal(inverse, live.Carrier.IsInverse);
        if (inverse) Assert.Equal(0, forward.Tension);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ConnectedPublicationReactsToPressureWithoutReacquiringItsSlot(bool inverse)
    {
        var forward = new Carrier();
        var source = inverse ? forward.Inverse() : forward;
        var observed = new Carrier();
        source.Attach(new Relation(Relation.Point(source, 3), Relation.Point(source, 3)));
        // Offset is an explicitly supplied read frame, keeping these readings
        // within the output carrier's positive material-value range.
        var port = Equation.Affine(source, -10, 10, observed, 0, 20);
        source.AttachLiveOutput(source.LiveTensionSlot, port);

        Assert.Equal(10UL, observed.Value);
        source.Add(8);
        Assert.Equal(15UL, observed.Value);
        source.Remove(10);
        Assert.Equal(8UL, observed.Value);
        source.Add(2);
        Assert.Equal(10UL, observed.Value);
        Assert.Equal(0, source.Tension);
        observed.Add(0);
        Assert.Equal(10UL, observed.Value);
        Assert.Equal(0, observed.Tension);
    }
}
