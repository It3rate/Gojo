using Gojo.Core;
using Gojo.Structures;

namespace Gojo.Tests.Core;

public sealed class RefinementTransferTests
{
    [Fact]
    public void RefinementMovesParticipationAndCoarseningRetainsUnrepresentableFineTicks()
    {
        var coarse = new Carrier();
        var fine = new Carrier(UnitShape.SubdivisionOf(coarse.UnitShape, 10));
        coarse.Add(3);
        var basis = Basis(coarse, 1, fine, 10);
        var request = RefinementTransfer.Create(new Carrier(), basis, new CarrierLocation(coarse, 2));
        Assert.True(RefinementTransfer.TryApply(request, out var witness));
        Assert.Equal(1UL, coarse.Value);
        Assert.Equal(20UL, fine.Value);
        Assert.True(ReadWitness.TryRead(witness, out _, out _, out var result, out _, out var history));
        Assert.Equal(new Relation(new CarrierLocation(coarse, -2), new CarrierLocation(fine, 20)), result);
        Assert.NotEqual(Assert.IsType<Relation>(history).Left, Assert.IsType<Relation>(history).Right);
        fine.Remove(3);
        var back = RefinementTransfer.Create(new Carrier(), Basis(fine, 10, coarse, 1), new CarrierLocation(fine, 30));
        Assert.True(RefinementTransfer.TryApply(back, out var returned));
        Assert.Equal(2UL, coarse.Value);
        Assert.Equal(7UL, fine.Value);
        Assert.True(ReadWitness.TryRead(returned, out _, out _, out var moved, out var residue, out _));
        Assert.Equal(-10, Assert.IsType<CarrierLocation>(Assert.IsType<Relation>(moved).Left).Ticks);
        Assert.True(Assert.IsType<Relation>(residue).TryReadOrderedGroup(out var remainders));
        Assert.Equal(new CarrierLocation(fine, 13), remainders[0]);
        Assert.Equal(new CarrierLocation(fine, 7), remainders[1]);
        Assert.Equal(30, 10 + Assert.IsType<CarrierLocation>(remainders[0]).Ticks + Assert.IsType<CarrierLocation>(remainders[1]).Ticks);
    }

    [Fact]
    public void BorrowMatrixPreservesExactWeightedValueAndUnmetDraw()
    {
        for (var radix = 2; radix <= 7; radix++)
        for (var c = 0; c <= 5; c++)
        for (var f = 0; f < radix; f++)
        for (var draw = 0; draw <= c * radix + f + 3; draw++)
        {
            var fine = new Carrier();
            var coarse = new Carrier(UnitShape.ResolutionOf(fine.UnitShape, radix));
            fine.Add((ulong)f); coarse.Add((ulong)c);
            fine.Remove((ulong)draw);
            var expected = c * radix + f - draw;
            Assert.True(RefinementTransfer.TryBorrow(Basis(coarse, 1, fine, radix), out _));
            Assert.Equal(expected, radix * (long)coarse.Value + (long)fine.Value + fine.Tension);
            Assert.Equal(Math.Max(0, expected) / radix, (long)coarse.Value);
            Assert.Equal(Math.Max(0, expected) % radix, (long)fine.Value);
            Assert.Equal(Math.Min(0, expected), fine.Tension);
            Assert.Equal(0, coarse.Tension);
        }
    }

    [Fact]
    public void ReceiverCapacityKeepsAcceptedInjectionAsValuePlusTension()
    {
        var coarse = new Carrier();
        var fine = new Carrier(UnitShape.SubdivisionOf(coarse.UnitShape, 10));
        fine.Attach(new Relation(Relation.Point(fine, 4), Relation.Point(fine, 4)));
        coarse.Add(3);
        Assert.True(RefinementTransfer.TryApply(RefinementTransfer.Create(new Carrier(),
            Basis(coarse, 1, fine, 10), new CarrierLocation(coarse, 1)), out _));
        Assert.Equal(2UL, coarse.Value);
        Assert.Equal(4UL, fine.Value);
        Assert.Equal(6, fine.Tension);
        Assert.Equal(30, 10 * (long)coarse.Value + (long)fine.Value + fine.Tension);
    }

    [Fact]
    public void PartialIncompatibleAndOutOfRangeTransfersDoNotPartiallyDrain()
    {
        var source = new Carrier(); var wrong = new Carrier();
        source.Add(3);
        Assert.False(RefinementTransfer.TryApply(RefinementTransfer.Create(new Carrier(),
            Basis(source, 1, wrong, 10), new CarrierLocation(source, 1)), out _));
        var fine = new Carrier(UnitShape.SubdivisionOf(source.UnitShape, 10));
        var basis = Basis(source, 1, fine, 10);
        Assert.False(RefinementTransfer.TryApply(RefinementTransfer.Create(new Carrier(), basis, new Placeholder()), out _));
        fine.Set(long.MaxValue);
        Assert.Throws<OverflowException>(() => RefinementTransfer.TryApply(
            RefinementTransfer.Create(new Carrier(), basis, new CarrierLocation(source, 1)), out _));
        Assert.Equal(3UL, source.Value);
        Assert.Equal((ulong)long.MaxValue, fine.Value);
        Assert.Equal(0, source.Tension);
        Assert.Equal(0, fine.Tension);
    }

    [Fact]
    public void SubdivisionAndResolutionRetainUnitAndOpposedFaceLaws()
    {
        var unit = new Carrier().UnitShape;
        var finer = UnitShape.SubdivisionOf(unit, 10);
        Assert.Equal(unit, UnitShape.ResolutionOf(finer, 10));
        Assert.Equal(finer, UnitShape.SubdivisionOf(UnitShape.ResolutionOf(finer, 5), 5));
        Assert.Equal(UnitShape.SubdivisionOf(unit, 100), UnitShape.SubdivisionOf(finer, 10));
        var opposed = UnitShape.OpposedFaceOf(UnitShape.ResolutionOf(unit, 10));
        Assert.Equal(UnitShape.OpposedFaceOf(unit), UnitShape.SubdivisionOf(opposed, 10));
        Assert.Equal(UnitShape.OpposedFaceOf(UnitShape.ResolutionOf(unit, 20)), UnitShape.ResolutionOf(opposed, 2));
        Assert.True(UnitShape.Product(finer, unit).TryReadOrthogonalProductOperands(out var factors));
        Assert.Equal(new[] { finer, unit }, factors);
    }

    private static Relation Basis(Carrier source, long a, Carrier target, long b) =>
        new(Relation.Segment(source, 0, a), Relation.Segment(target, 0, b));
}
