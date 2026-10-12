using Gojo.Core;
using Gojo.Equations;
using Gojo.Structures;

namespace Gojo.Tests.Structures;

public class ScopedChoiceTests
{
    private static Relation Read(IElement source) => source is CarrierLocation value
        ? new Relation(new CarrierLocation(value.Carrier, value.Ticks), Relation.Point(value.Carrier, 0))
        : Relation.UnresolvedRead(source, source, Carrier.Unknown);

    [Fact]
    public void OccupiedBandsChooseOneSourceAndDoNotReadTheUnchosenOpenSource()
    {
        var selector = new Carrier(); var value = new Carrier(); var output = new Carrier();
        var at = new CarrierLocation(selector, 1);
        var present = new CarrierLocation(value, 9); var missing = new Placeholder();
        var choice = ScopedChoice.Create(at, Relation.Segment(selector, 0, 2), output,
            ScopedChoice.Offer(Relation.Segment(selector, 0, 1), missing, Equation.Affine(value, 0, 1, output, 0, 1)),
            ScopedChoice.Offer(Relation.Segment(selector, 1, 2), present, Equation.Affine(value, 0, 1, output, 0, 1)));
        var readSources = new List<IElement>();
        Assert.True(ScopedChoice.TryObserve(choice, source => { readSources.Add(source); return Read(source); }, out var result));
        Assert.Equal(9, ((CarrierLocation)result.Left).Ticks); Assert.DoesNotContain(missing, readSources);
        at.Tick.Set(0);
        Assert.False(ScopedChoice.TryObserve(choice, Read, out var open)); Assert.IsType<Placeholder>(open.Left);
        Assert.Equal(9, ((CarrierLocation)result.Left).Ticks); Assert.Equal(0UL, output.Value);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(2)]
    [InlineData(3)]
    public void GapsOutOfScopeAndOverlappingOffersRemainOpen(int sample)
    {
        var selector = new Carrier(); var output = new Carrier(); var value = new CarrierLocation(new Carrier(), 7);
        var choice = ScopedChoice.Create(new CarrierLocation(selector, sample), Relation.Segment(selector, 0, 4), output,
            ScopedChoice.Offer(Relation.Segment(selector, 0, 1), value, Equation.Affine(value.Carrier, 0, 1, output, 0, 1)),
            ScopedChoice.Offer(Relation.Segment(selector, 3, 4), value, Equation.Affine(value.Carrier, 0, 1, output, 0, 1)),
            ScopedChoice.Offer(Relation.Segment(selector, 3, 4), value, Equation.Affine(value.Carrier, 0, 1, output, 0, 1)));
        Assert.False(ScopedChoice.TryObserve(choice, Read, out var result)); Assert.IsType<Placeholder>(result.Left);
    }

    [Fact]
    public void ChoiceIsReadableAfterNumericTransportWithoutAHelperObjectOrOperationTag()
    {
        var selector = new Carrier(); var output = new Carrier(); var value = new CarrierLocation(new Carrier(), -8);
        var choice = ScopedChoice.Create(new CarrierLocation(selector, 1), Relation.Segment(selector, 0, 2), output,
            ScopedChoice.Offer(Relation.Segment(selector, 0, 2), value, Equation.Affine(value.Carrier, 0, 1, output, 0, 1)));
        var restored = (Relation)GojoNumericFormat.Decode(GojoNumericFormat.Encode(choice)).Single();
        Assert.True(ScopedChoice.TryObserve(restored, Read, out var result)); Assert.Equal(-8, ((CarrierLocation)result.Left).Ticks);
        Assert.False(ScopedChoice.TryRead(Relation.OrderedGroup(new Carrier(), choice, new Placeholder()), out _, out _, out _, out _));
    }

    [Fact]
    public void IndependentSelectorAxisAndReversedBandsCannotInventCoverage()
    {
        var selector = new Carrier(); var other = new Carrier(); var output = new Carrier();
        var value = new CarrierLocation(new Carrier(), 5);
        var choice = ScopedChoice.Create(new CarrierLocation(other, 1), Relation.Segment(selector, 0, 2), output,
            ScopedChoice.Offer(Relation.Segment(selector, 0, 2), value, Equation.Affine(value.Carrier, 0, 1, output, 0, 1)));
        Assert.False(ScopedChoice.TryObserve(choice, Read, out _));
        var reverse = ScopedChoice.Create(new CarrierLocation(selector, 1), Relation.Segment(selector, 0, 2), output,
            ScopedChoice.Offer(Relation.Segment(selector, 2, 0), value, Equation.Affine(value.Carrier, 0, 1, output, 0, 1)));
        Assert.False(ScopedChoice.TryObserve(reverse, Read, out _));
    }
}
