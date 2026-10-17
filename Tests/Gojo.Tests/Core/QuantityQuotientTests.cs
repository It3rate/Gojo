using System.Numerics;
using Gojo.Core;
using Gojo.Patterns;
using Gojo.Structures;

namespace Gojo.Tests.Core;

public sealed class QuantityQuotientTests
{
    [Fact]
    public void SignedDivisionReconstructsEveryNumeratorWithoutSettlingCarriers()
    {
        var numerator = new Carrier();
        var denominator = new Carrier();
        var standard = new Relation(numerator, denominator);
        var output = new Carrier(QuotientUnitConformanceTests.Read([numerator], [denominator], standard));
        numerator.Set(11);
        denominator.Set(17);
        output.Set(23);
        for (var n = -40; n <= 40; n++)
        for (var d = -20; d <= 20; d++)
        {
            if (d == 0) continue;
            var request = QuantityQuotient.Create(new Carrier(), new CarrierLocation(numerator, n),
                new CarrierLocation(denominator, d), output, standard);
            Assert.True(QuantityQuotient.TryObserve(request, out var witness));
            Assert.True(ReadWitness.TryRead(witness, out var selected, out var surface, out var result, out var residue, out var evidence));
            Assert.Same(request, selected);
            var q = Assert.IsType<CarrierLocation>(result);
            Assert.Same(output, q.Carrier);
            Assert.Equal(n / d, q.Ticks);
            Assert.True(ProjectionResidue.TryRead(residue, out var source, out var r, out var scale));
            Assert.Same(numerator, source);
            Assert.Equal(1, scale);
            Assert.Equal(n, q.Ticks * d + r);
            Assert.True(Math.Abs(r) < Math.Abs(d));
            Assert.True(r == 0 || Math.Sign(r) == Math.Sign(n));
            Assert.Equal(d, Assert.IsType<CarrierLocation>(Assert.IsType<Relation>(surface).Left).Ticks);
            Assert.True(UnitQuotient.TryReadUnit(Assert.IsType<Relation>(evidence), out var unit));
            Assert.Equal(output.UnitShape, unit);
        }
        Assert.Equal(11UL, numerator.Value);
        Assert.Equal(17UL, denominator.Value);
        Assert.Equal(23UL, output.Value);
        Assert.Equal(0, numerator.Tension);
        Assert.Equal(0, denominator.Tension);
        Assert.Equal(0, output.Tension);
    }

    [Fact]
    public void UndefinedAndMismatchedReadsRetainAnOpenResultAndOriginalParticipants()
    {
        var n = new Carrier();
        var d = new Carrier();
        var standard = new Relation(n, d);
        var output = new Carrier(QuotientUnitConformanceTests.Read([n], [d], standard));
        foreach (var denominator in new IElement[] { new CarrierLocation(d, 0), new Placeholder(), Relation.Point(d, 2) })
            AssertUnresolved(QuantityQuotient.Create(new Carrier(), new CarrierLocation(n, 7), denominator, output, standard));
        AssertUnresolved(QuantityQuotient.Create(new Carrier(), new Placeholder(), new CarrierLocation(d, 2), output, standard));
        AssertUnresolved(QuantityQuotient.Create(new Carrier(), new CarrierLocation(n, 7), new CarrierLocation(d, 2), new Carrier(), standard));
        AssertUnresolved(QuantityQuotient.Create(new Carrier(), new CarrierLocation(n, 7), new CarrierLocation(d, 2), output, new Placeholder()));
        var hundredD = new Carrier(UnitShape.ResolutionOf(d.UnitShape, 100));
        AssertUnresolved(QuantityQuotient.Create(new Carrier(), new CarrierLocation(n, 7), new CarrierLocation(hundredD, 2), output, standard));
        AssertUnresolved(QuantityQuotient.Create(new Carrier(), new CarrierLocation(n, 7), new CarrierLocation(d, 2), output.Inverse(), standard));
    }

    [Fact]
    public void BindingAnOpenDenominatorEnablesAReadWithoutRewritingThePattern()
    {
        var n = new Carrier();
        var d = new Carrier();
        var standard = new Relation(n, d);
        var output = new Carrier(QuotientUnitConformanceTests.Read([n], [d], standard));
        var slot = new Placeholder();
        var request = QuantityQuotient.Create(new Carrier(), new CarrierLocation(n, 7), slot, output, standard);
        AssertUnresolved(request);
        var bound = Assert.IsType<Relation>(StructuralPattern.Apply(request, StructuralPattern.Bind(slot, new CarrierLocation(d, 2))));
        Assert.True(QuantityQuotient.TryObserve(bound, out var witness));
        Assert.True(ReadWitness.TryRead(witness, out _, out _, out var result, out _, out _));
        Assert.Equal(3, Assert.IsType<CarrierLocation>(result).Ticks);
        AssertUnresolved(request);
    }

    [Fact]
    public void CheckedRangeBoundaryDoesNotWrapOrApplyPartialState()
    {
        var n = new Carrier();
        var d = new Carrier();
        var standard = new Relation(n, d);
        var output = new Carrier(QuotientUnitConformanceTests.Read([n], [d], standard));
        foreach (var (a, b) in new[] { (long.MinValue, 1L), (long.MinValue, long.MaxValue), (long.MaxValue, long.MinValue) })
        {
            var request = QuantityQuotient.Create(new Carrier(), new CarrierLocation(n, a), new CarrierLocation(d, b), output, standard);
            Assert.True(QuantityQuotient.TryObserve(request, out var witness));
            Assert.True(ReadWitness.TryRead(witness, out _, out _, out var result, out var residue, out _));
            var q = Assert.IsType<CarrierLocation>(result).Ticks;
            Assert.True(ProjectionResidue.TryRead(residue, out _, out var r, out _));
            Assert.Equal((BigInteger)a, (BigInteger)q * b + r);
        }
        var overflow = QuantityQuotient.Create(new Carrier(), new CarrierLocation(n, long.MinValue),
            new CarrierLocation(d, -1), output, standard);
        Assert.Throws<OverflowException>(() => QuantityQuotient.TryObserve(overflow, out _));
        Assert.Equal(0UL, output.Value);
        Assert.Equal(0, output.Tension);
    }

    [Fact]
    public void MalformedRequestsDoNotUseNestedOrExtraFields()
    {
        var carrier = new Carrier();
        var request = QuantityQuotient.Create(new Carrier(), carrier, carrier, carrier, carrier);
        Assert.False(QuantityQuotient.TryObserve(Relation.OrderedGroup(new Carrier(), request), out var nested));
        Assert.Null(nested);
        Assert.False(QuantityQuotient.TryObserve(Relation.OrderedGroup(new Carrier(), carrier, carrier, carrier, carrier, carrier), out _));
    }

    private static void AssertUnresolved(Relation request)
    {
        Assert.False(QuantityQuotient.TryObserve(request, out var witness));
        Assert.True(ReadWitness.TryRead(witness, out var selected, out _, out var result, out var residue, out _));
        Assert.Same(request, selected);
        Assert.IsType<Placeholder>(result);
        Assert.True(QuantityQuotient.TryRead(request, out var n, out var d, out _, out _));
        var participants = Assert.IsType<Relation>(residue);
        Assert.Same(n, participants.Left);
        Assert.Same(d, participants.Right);
    }
}
