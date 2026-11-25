using Xunit;

namespace Gojo.EncodingC.Tests;

public sealed class OperationNumericDiscoveryTests
{
    private static readonly Shape Operand = Shape.Binary(4);
    private static readonly Shape Result = Shape.Binary(10);
    private static readonly RelationLaw Sum = RelationLaw.FromPredicate([Operand, Operand, Result],
        values => values[0] + values[1] == values[2]);
    private static readonly RelationLaw Product = BoundedProduct.CreateRelation(Operand, Operand, Result);

    [Fact]
    public void AnExposedZeroDistinguishesCandidatesThatFitTheSameSeedReads()
    {
        var (source, left, right, observed) = Samples((0, 0, 0), (0, 3, 3), (2, 2, 4), (0, 3, null));
        var (leftPort, rightPort, resultPort) = Publish(source, left, right, observed, [0, 2, 3]);
        var sum = Check(Sum, leftPort, rightPort, resultPort);
        var product = Check(Product, leftPort, rightPort, resultPort);

        var seeds = Plane.Of(3, 0, 1);
        Assert.Equal(seeds, SettledAs(sum.Gap, 0));
        Assert.Equal(seeds, SettledAs(product.Gap, 0));
        Assert.Equal(Plane.Of(3, 2), sum.Gap.Open);
        Assert.Equal(Plane.Of(3, 2), product.Gap.Open);
        Assert.Equal(3, sum.Prediction.Value(2, 2));
        Assert.Equal(0, product.Prediction.Value(2, 2));

        Assert.True(source.SamePosition(3, resultPort, 2));
        Assert.False(source.SamePosition(1, resultPort, 2));
        Assert.False(observed.Settled[3]);
        Observe(observed, 3, 0);
        sum.Comparison.Apply();
        product.Comparison.Apply();

        Assert.Equal(Plane.Of(3, 2), SettledAs(sum.Gap, 1));
        Assert.Equal(Plane.Ones(3), SettledAs(product.Gap, 0));
        Assert.Equal(1UL, observed.Values(3));
        Assert.Equal(1UL << 3, observed.Values(1));
        Assert.Equal(0, observed.Field.Read(source, 3));
        Assert.Equal(3, observed.Field.Read(source, 1));

        var (fresh, freshLeft, freshRight, freshObserved) = Samples((2, 3, 6));
        var (freshLeftPort, freshRightPort, freshResultPort) = Publish(fresh, freshLeft, freshRight, freshObserved, [0]);
        Assert.False(resultPort.SamePosition(0, freshResultPort, 0));
        var heldOut = Check(Product, freshLeftPort, freshRightPort, freshResultPort);
        Assert.Equal(6, heldOut.Prediction.Value(0, 2));
        Assert.Equal(Plane.Ones(1), SettledAs(heldOut.Gap, 0));
    }

    [Fact]
    public void ExactSubstitutionRequiresAgreementAcrossEveryOrderedInput()
    {
        int[] expected = [0, 0, 0, 0, 0, 1, 2, 3, 0, 2, 4, 6, 0, 3, 6, 9];
        var last = expected.Length - 1;
        var samples = expected.Select((value, row) => (row / Operand.Size, row % Operand.Size,
            row == last ? (int?)null : value)).ToArray();
        var (source, left, right, observed) = Samples(samples);
        var (leftPort, rightPort, resultPort) = Publish(source, left, right, observed,
            Enumerable.Range(0, samples.Length).ToArray());

        var equivalent = RelationLaw.FromPredicate(Product.Roles, values =>
            expected[values[0] * Operand.Size + values[1]] == values[2]);
        var altered = RelationLaw.FromPredicate(Product.Roles, values =>
            values[0] == 3 && values[1] == 3 ? values[2] == 8 : Product.Admits(values));
        var exact = Check(equivalent, leftPort, rightPort, resultPort);
        var changed = Check(altered, leftPort, rightPort, resultPort);
        var all = Plane.Ones(samples.Length);
        var pending = Plane.Of(samples.Length, last);

        Assert.Equal(all.AndNot(pending), SettledAs(exact.Gap, 0));
        Assert.Equal(pending, exact.Gap.Open);
        Assert.Equal(all.AndNot(pending), SettledAs(changed.Gap, 0));
        Assert.Equal(pending, changed.Gap.Open);
        Assert.True(observed.Open[last]);

        Observe(observed, last, expected[last]);
        exact.Comparison.Apply();
        changed.Comparison.Apply();

        Assert.Equal(all, SettledAs(exact.Gap, 0));
        Assert.Equal(all.AndNot(pending), SettledAs(changed.Gap, 0));
        Assert.Equal(pending, SettledAs(changed.Gap, 1));
        Assert.Equal(1UL << 9, observed.Values(last));
        Assert.Equal(9, observed.Field.Read(source, last));
    }

    private static (Store Source, Field Left, Field Right, Supports Observed) Samples(
        params (int Left, int Right, int? Result)[] samples)
    {
        var source = new Store(0, samples.Length);
        var left = AddField(source, Operand);
        var right = AddField(source, Operand);
        var observed = Supports.On(source, AddField(source, Result));
        for (var row = 0; row < samples.Length; row++)
        {
            var sample = samples[row];
            left.Write(source, row, sample.Left);
            right.Write(source, row, sample.Right);
            if (sample.Result is int value) Observe(observed, row, value);
        }
        return (source, left, right, observed);
    }

    private static (Port Left, Port Right, Port Result) Publish(
        Store source, Field left, Field right, Supports observed, int[] positions) =>
        (source.Publish(positions, [.. left.Planes]), source.Publish(positions, [.. right.Planes]),
            source.Publish(positions, [.. observed.Planes]));

    private static (Wiring Prediction, Wiring Comparison, Supports Gap) Check(
        RelationLaw law, Port left, Port right, Port observed)
    {
        var local = new Store(0, observed.Count);
        var result = Supports.On(local, AddField(local, Result));
        var gap = Supports.On(local, AddField(local, Shape.Bit));
        var prediction = new Wiring(law, Read(left), Read(right), result.Variable);
        var comparison = new Wiring(Settling.ValueGap(Result), result.Variable,
            Binding.Open(observed, [.. Enumerable.Range(0, observed.PlaneCount)]), gap.Variable);
        for (var lane = 0; lane < observed.Count; lane++)
        {
            prediction.Add(lane, lane, lane);
            comparison.Add(lane, lane, lane);
        }
        prediction.Apply();
        comparison.Apply();
        return (prediction, comparison, gap);
    }

    private static Binding Read(Port port) => new(port, [.. Enumerable.Range(0, port.PlaneCount)]);

    private static Field AddField(Store store, Shape shape) =>
        new(shape, [.. Enumerable.Range(0, shape.Bits).Select(_ => store.AddPlane())]);

    private static Plane SettledAs(Supports supports, int value) => supports.Settled.And(supports.Of(value));

    private static void Observe(Supports samples, int position, int value)
    {
        samples.Field.Write(samples.Surface, position, value);
        Assert.True(samples.Observe(position));
    }
}
