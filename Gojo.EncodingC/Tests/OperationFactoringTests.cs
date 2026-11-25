using Xunit;

namespace Gojo.EncodingC.Tests;

public sealed class OperationFactoringTests
{
    private static readonly Shape OperandShape = Shape.Binary(4);
    private static readonly Shape ResultShape = Shape.Binary(10);

    private static readonly RelationLaw DenseProduct = BoundedProduct.CreateRelation(OperandShape, OperandShape, ResultShape);

    private static readonly RelationLaw CoPresence = RelationLaw.FromPredicate(
        [Shape.Bit, Shape.Bit, Shape.Bit], values => values[2] == (values[0] & values[1]));

    private static readonly RelationLaw HalfAdd = RelationLaw.FromPredicate(
        [Shape.Bit, Shape.Bit, Shape.Bit, Shape.Bit],
        values => values[0] + values[1] == values[2] + 2 * values[3]);

    [Fact]
    public void FourPartialProductsAndTwoCarriesMatchEveryDenseProduct()
    {
        const int specimens = 16;
        var dense = ArrangeDense(specimens);
        var factored = ArrangeFactored(specimens);
        var position = 0;
        for (var left = 0; left < OperandShape.Size; left++)
        {
            for (var right = 0; right < OperandShape.Size; right++)
            {
                Observe(dense.Left, position, left);
                Observe(dense.Right, position, right);
                ObserveDigits(factored.Left, position, left);
                ObserveDigits(factored.Right, position, right);
                position++;
            }
        }

        Assert.All(dense.Engine.Step().Steps, step => Assert.True(step.Unadmitted.None));
        Assert.All(factored.Engine.Step().Steps, step => Assert.True(step.Unadmitted.None));
        for (var specimen = 0; specimen < specimens; specimen++)
        {
            var expected = dense.Engine.Wirings[0].Value(specimen, 2);
            Assert.NotNull(expected);
            Assert.Equal(expected, ReadDigits(factored.Product, specimen));
        }

        // Count shared law payload once per engine. Separate it from the state
        // needed for the value and support planes and for the wired rows.
        Assert.Equal((160, 20, 26, 16), Costs(dense.Engine, dense.Values));
        Assert.Equal((24, 3, 36, 96), Costs(factored.Engine, factored.Values));
    }

    [Fact]
    public void AStepTakenTogetherNeedsThreeWavesOfEvidenceForTheFactoredProduct()
    {
        var dense = ArrangeDense(1);
        var factored = ArrangeFactored(1);
        Observe(dense.Left, 0, 3);
        Observe(dense.Right, 0, 3);
        ObserveDigits(factored.Left, 0, 3);
        ObserveDigits(factored.Right, 0, 3);

        dense.Engine.StepTogether();
        Assert.Equal(9, dense.Engine.Wirings[0].Value(0, 2));

        factored.Engine.StepTogether();
        Assert.Equal(0b10UL, factored.Product[0].Values(0));
        Assert.Null(ReadDigits(factored.Product, 0));

        factored.Engine.StepTogether();
        Assert.Equal(0b01UL, factored.Product[1].Values(0));
        Assert.Null(ReadDigits(factored.Product, 0));

        factored.Engine.StepTogether();
        Assert.Equal(9, ReadDigits(factored.Product, 0));
        Assert.Equal(3, factored.Engine.Tick);
    }

    [Fact]
    public void TheFactoredProductCanNarrowFromAnOutputAndThenFromOneFactor()
    {
        var dense = ArrangeDense(1);
        var factored = ArrangeFactored(1);
        Observe(dense.Product, 0, 6);
        ObserveDigits(factored.Product, 0, 6);

        dense.Engine.Step();
        Assert.NotNull(factored.Engine.Settle(8));

        Assert.Equal(0b1100UL, dense.Left.Values(0));
        Assert.Equal(0b1100UL, dense.Right.Values(0));
        Assert.All(new[] { factored.Left, factored.Right }, factor =>
        {
            Assert.True(factor[0].Open[0]);
            Assert.Equal(0b10UL, factor[1].Values(0));
        });

        Observe(dense.Left, 0, 2);
        ObserveDigits(factored.Left, 0, 2);
        dense.Engine.Step();
        Assert.NotNull(factored.Engine.Settle(8));

        Assert.Equal(3, dense.Engine.Wirings[0].Value(0, 1));
        Assert.Equal(3, ReadDigits(factored.Right, 0));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void QuietLocalSupportsCanLeaveAContradictionThatTheDenseLawDetects(bool sameOperand)
    {
        var dense = ArrangeDense(1, sameOperand);
        var factored = ArrangeFactored(1, sameOperand);
        Observe(dense.Product, 0, 5);
        ObserveDigits(factored.Product, 0, 5);

        Assert.True(dense.Engine.Step().Steps[0].Unadmitted[0]);
        Assert.True(dense.Product.Empty[0]);

        // The two carry rows each retain an option, but their choices cannot
        // all be satisfied at once. The bounded network supplies no search.
        Assert.NotNull(factored.Engine.Settle(8));
        Assert.All(factored.Engine.Step().Steps, step =>
        {
            Assert.True(step.Quiet);
            Assert.True(step.Unadmitted.None);
        });
        Assert.True(factored.Left[1].Open[0]);
        Assert.True(factored.Right[1].Open[0]);
    }

    private static (Store Values, Engine Engine, Supports Left, Supports Right, Supports Product) ArrangeDense(
        int specimens, bool sameOperand = false)
    {
        var values = new Store(0, specimens);
        var left = Allocate(values, OperandShape);
        var right = sameOperand ? left : Allocate(values, OperandShape);
        var product = Allocate(values, ResultShape);
        var engine = new Engine();
        Wire(engine, DenseProduct, left, right, product);
        return (values, engine, left, right, product);
    }

    private static (Store Values, Engine Engine, Supports[] Left, Supports[] Right, Supports[] Product) ArrangeFactored(
        int specimens, bool sameOperand = false)
    {
        var values = new Store(0, specimens);
        Supports Bit() => Allocate(values, Shape.Bit);

        Supports[] left = [Bit(), Bit()];
        Supports[] right = sameOperand ? left : [Bit(), Bit()];
        var lowLow = Bit();
        var lowHigh = Bit();
        var highLow = Bit();
        var highHigh = Bit();
        var middleDigit = Bit();
        var middleCarry = Bit();
        var highDigit = Bit();
        var highCarry = Bit();
        var engine = new Engine();

        Wire(engine, CoPresence, left[0], right[0], lowLow);
        Wire(engine, CoPresence, left[0], right[1], lowHigh);
        Wire(engine, CoPresence, left[1], right[0], highLow);
        Wire(engine, CoPresence, left[1], right[1], highHigh);
        Wire(engine, HalfAdd, lowHigh, highLow, middleDigit, middleCarry);
        Wire(engine, HalfAdd, highHigh, middleCarry, highDigit, highCarry);

        return (values, engine, left, right, [lowLow, middleDigit, highDigit, highCarry]);
    }

    private static Supports Allocate(Store store, Shape shape) =>
        Supports.On(store, new Field(shape, [.. Enumerable.Range(0, shape.Bits).Select(_ => store.AddPlane())]));

    private static void Wire(Engine engine, RelationLaw law, params Supports[] roles)
    {
        var wiring = new Wiring(law, [.. roles.Select(role => role.Variable)]);
        for (var position = 0; position < roles[0].Surface.Count; position++)
            wiring.Add([.. Enumerable.Repeat(position, roles.Length)]);
        engine.Add(wiring);
    }

    private static void ObserveDigits(Supports[] digits, int position, int value)
    {
        for (var digit = 0; digit < digits.Length; digit++)
            Observe(digits[digit], position, (value >> digit) & 1);
    }

    private static void Observe(Supports support, int position, int value)
    {
        support.Field.Write(support.Surface, position, value);
        Assert.True(support.Observe(position));
    }

    private static int? ReadDigits(Supports[] digits, int position)
    {
        var value = 0;
        for (var digit = 0; digit < digits.Length; digit++)
        {
            var support = digits[digit].Values(position);
            if (support == 0b10UL) value |= 1 << digit;
            else if (support != 0b01UL) return null;
        }

        return value;
    }

    private static (int LawBits, int LawBytes, int ValuePlanes, int Rows) Costs(Engine engine, Store values)
    {
        var laws = engine.Wirings.Select(wiring => wiring.Law).Distinct().ToArray();
        return (laws.Sum(law => law.BitCount), laws.Sum(law => law.Bytes().Length),
            values.PlaneCount, engine.Wirings.Sum(wiring => wiring.Count));
    }
}
