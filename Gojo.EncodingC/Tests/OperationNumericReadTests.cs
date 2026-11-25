using Xunit;

namespace Gojo.EncodingC.Tests;

public sealed class OperationNumericReadTests
{
    private static readonly RelationLaw MeetsRequest = RelationLaw.FromPredicate(
        [RadixFour.Amount, RadixFour.Amount, Shape.Bit],
        values => values[2] == (values[0] >= values[1] ? 1 : 0));

    [Theory]
    [InlineData(1, 10, 0b11UL, 0)]
    [InlineData(3, 10, 0b11UL, 1)]
    [InlineData(0, 10, 0b11UL, 0)]
    [InlineData(1, 8, 0b10UL, 1)]
    [InlineData(3, 12, 0b01UL, 0)]
    public void AReadUsesOnlyThePublishedGrades(int fine, int requested, ulong coarseAnswer, int exactAnswer)
    {
        var supplier = new Store(0, 1);
        var groups = Allocate(supplier, RadixFour.Digit);
        var remainder = Allocate(supplier, RadixFour.Digit);
        groups.Write(supplier, 0, 2);
        remainder.Write(supplier, 0, fine);
        var coarsePort = supplier.Publish([0], [.. groups.Planes]);
        var finePort = supplier.Publish([0], [.. remainder.Planes]);
        new Engine().Hold(supplier);

        var receiver = new Store(0, 1);
        var refinement = new Socket(1, RadixFour.Digit.Bits);
        var selectedFine = Supports.On(receiver, Allocate(receiver, RadixFour.Digit));
        var amount = Supports.On(receiver, Allocate(receiver, RadixFour.Amount));
        var request = Allocate(receiver, RadixFour.Amount);
        var answer = Supports.On(receiver, Field.Bit(receiver.AddPlane()));
        request.Write(receiver, 0, requested);

        var observeFine = new Wiring(Settling.Same(RadixFour.Digit), new Binding(refinement, 0, 1), selectedFine.Variable);
        observeFine.Add(0, 0);
        var evidence = observeFine.Record();
        var read = new Wiring(RadixFour.ReadAmount, new Binding(coarsePort, 0, 1), selectedFine.Variable, amount.Variable);
        read.Add(0, 0, 0);
        var compare = new Wiring(MeetsRequest, amount.Variable, request.On(receiver), answer.Variable);
        compare.Add(0, 0, 0);
        var observer = new Engine().Add(observeFine).Add(read).Add(compare);

        observer.Step();
        Assert.Null(refinement.Plugged);
        Assert.True(observeFine.Rows.Read(evidence.Skipped, 0));
        Assert.Equal(0b1111UL, selectedFine.Values(0));
        Assert.Equal(0b1111UL << 8, amount.Values(0));
        Assert.Equal(coarseAnswer, answer.Values(0));

        // Connect the optional publication for this same observation.
        refinement.Plug(finePort);
        observer.Step();
        Assert.False(observeFine.Rows.Read(evidence.Skipped, 0));
        Assert.Equal(1UL << fine, selectedFine.Values(0));
        Assert.Equal(1UL << (8 + fine), amount.Values(0));
        Assert.Equal(1UL << exactAnswer, answer.Values(0));
        Assert.Equal(2, groups.Read(supplier, 0));
        Assert.Equal(fine, remainder.Read(supplier, 0));
        Assert.False(coarsePort.IsTaken);
        Assert.False(finePort.IsTaken);
    }

    [Fact]
    public void ExactAmountsSettleBothDigitRolesAcrossTheDeclaredRange()
    {
        var values = new Store(0, RadixFour.Amount.Size);
        var coarse = Supports.On(values, Allocate(values, RadixFour.Digit));
        var fine = Supports.On(values, Allocate(values, RadixFour.Digit));
        var amount = Allocate(values, RadixFour.Amount);
        var read = new Wiring(RadixFour.ReadAmount, coarse.Variable, fine.Variable, amount.On(values));
        for (var value = 0; value < RadixFour.Amount.Size; value++)
        {
            amount.Write(values, value, value);
            read.Add(value, value, value);
        }

        Assert.True(read.Apply().Unadmitted.None);
        for (var value = 0; value < RadixFour.Amount.Size; value++)
        {
            Assert.Equal(1UL << (value / 4), coarse.Values(value));
            Assert.Equal(1UL << (value % 4), fine.Values(value));
        }
    }

    [Fact]
    public void AnIndependentQualitativePortAnswersBeforeAnyNumericBinding()
    {
        var trail = Odv.Create(2);
        Sides.Field.Write(trail, 0, Sides.Before);
        Sides.Field.Write(trail, 1, Sides.With);
        var motion = trail.Publish([0, 1], [Odv.V, Odv.D]);
        new Engine().Hold(trail);

        var receiver = new Store(0, 1);
        var direction = Supports.On(receiver, Field.Bit(receiver.AddPlane()));
        var word = new Binding(motion, 0, 1);
        var read = new Wiring(Sides.Toward, word, word, direction.Variable);
        read.Add(0, 1, 0);

        Assert.True(new Engine().Add(read).Step().Steps[0].Unadmitted.None);
        Assert.Equal(0b10UL, direction.Values(0));
        Assert.False(motion.IsTaken);
    }

    private static Field Allocate(Store store, Shape shape) =>
        new(shape, [.. Enumerable.Range(0, shape.Bits).Select(_ => store.AddPlane())]);
}
