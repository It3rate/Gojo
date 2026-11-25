using Xunit;

namespace Gojo.EncodingC.Tests;

public sealed class OperationProductRelationTests
{
    private const int FactorCapacity = 4;
    private const int ProductCapacity = 16;

    private static readonly RelationLaw Multiplication = BoundedProduct.CreateRelation(
        Shape.Unary(FactorCapacity), Shape.Unary(FactorCapacity), Shape.Unary(ProductCapacity));

    [Fact]
    public void BoundedMultiplicationHasOneAdmittedProductPerOrderedPair()
    {
        Assert.Equal(425, Multiplication.BitCount);
        Assert.Equal(54, Multiplication.Bytes().Length);
        Assert.Equal(25, Multiplication.AdmittedCount);
    }

    [Fact]
    public void KnownFactorsSettleTheProductWithoutWritingTheSample()
    {
        var (factors, product, read) = ProductRead();
        Observe(factors, 0, 3);
        Observe(factors, 1, 4);
        product.Field.Write(product.Surface, 0, 7);

        var step = read.Apply();

        Assert.True(step.Unadmitted.None);
        Assert.True(product.Settled[0]);
        Assert.Equal(12, read.Value(0, 2));
        Assert.Equal(7, product.Field.Read(product.Surface, 0));
    }

    [Fact]
    public void AProductAndOneFactorSettleTheOtherFactor()
    {
        var (factors, product, read) = ProductRead();
        Observe(factors, 0, 3);
        Observe(product, 0, 12);

        var step = read.Apply();

        Assert.True(step.Unadmitted.None);
        Assert.True(factors.Settled[1]);
        Assert.Equal(4, read.Value(0, 1));
    }

    [Fact]
    public void AProductAloneRetainsOrderedAlternativesBeyondTheMarginals()
    {
        var (factors, product, read) = ProductRead();
        Observe(product, 0, 12);

        Assert.True(read.Apply().Ambiguous[0]);
        Assert.Equal(Mask(3, 4), factors.Values(0));
        Assert.Equal(Mask(3, 4), factors.Values(1));

        // Each candidate row binds an ordered pair and the same observed product.
        // Record the law's exclusions on the rows, preserving the pair references.
        var (candidates, candidate) = Make.Counts(FactorCapacity + 1, FactorCapacity);
        for (var value = 0; value <= FactorCapacity; value++)
            candidate.Write(candidates, value, value);

        var probes = new Wiring(Multiplication, candidate.On(candidates), candidate.On(candidates), product.Sample);
        for (var left = 0; left <= FactorCapacity; left++)
            for (var right = 0; right <= FactorCapacity; right++)
                probes.Add(left, right, 0);

        var evidence = probes.Record();
        var step = probes.Apply();
        var excluded = probes.Rows.Read(evidence.Unadmitted);
        var pairs = probes.Rows.Live.AndNot(excluded).Indexes()
            .Select(row => (probes.Value(row, 0), probes.Value(row, 1)))
            .ToArray();

        Assert.True(step.Unread.None);
        Assert.True(step.Skipped.None);
        Assert.Equal(23, excluded.Count);
        Assert.Equal(new (int?, int?)[] { (3, 4), (4, 3) }, pairs);
    }

    [Fact]
    public void AZeroFactorAndZeroProductLeaveTheOtherFactorOpen()
    {
        var (factors, product, read) = ProductRead();
        Observe(factors, 0, 0);
        Observe(product, 0, 0);

        var step = read.Apply();

        Assert.True(step.Ambiguous[0]);
        Assert.True(step.Unadmitted.None);
        Assert.True(factors.Open[1]);
        Assert.Equal(Mask(0, 1, 2, 3, 4), factors.Values(1));
    }

    [Theory]
    [InlineData(0, 12)]
    [InlineData(2, 5)]
    public void ContradictionLeavesEmptySupportEvenWhenTheSamplesAreInRange(int factor, int observedProduct)
    {
        var (factors, product, read) = ProductRead();
        Observe(factors, 0, factor);
        Observe(product, 0, observedProduct);

        var step = read.Apply();

        Assert.True(step.Unadmitted[0]);
        Assert.True(step.Unread.None);
        Assert.True(factors.Empty[1]);
        Assert.Equal(0UL, factors.Values(1));
        Assert.Equal(observedProduct, product.Field.Read(product.Surface, 0));
    }

    [Fact]
    public void TwoRolesBoundToOneSampleAdmitOnlyDiagonalPairs()
    {
        var (_, independentProduct, independentRead) = ProductRead();
        independentRead.Apply();
        Assert.True(independentProduct.Of(12)[0]);

        var (_, sharedProduct, sharedRead) = ProductRead(sameFactor: true);
        Assert.True(sharedRead.Apply().Ambiguous[0]);
        Assert.Equal(Mask(0, 1, 4, 9, 16), sharedProduct.Values(0));

        var (factor, impossibleProduct, impossibleRead) = ProductRead(sameFactor: true);
        Observe(impossibleProduct, 0, 12);
        Assert.True(impossibleRead.Apply().Unadmitted[0]);
        Assert.True(factor.Empty[0]);
        Assert.True(impossibleProduct.Empty[0]);
    }

    private static (Supports Factors, Supports Product, Wiring Read) ProductRead(bool sameFactor = false)
    {
        var (factorStore, factorField) = Make.Counts(2, FactorCapacity);
        var (productStore, productField) = Make.Counts(1, ProductCapacity);
        var factors = Supports.On(factorStore, factorField);
        var product = Supports.On(productStore, productField);
        var read = new Wiring(Multiplication, factors.Variable, factors.Variable, product.Variable);
        read.Add(0, sameFactor ? 0 : 1, 0);
        return (factors, product, read);
    }

    private static void Observe(Supports support, int position, int value)
    {
        support.Field.Write(support.Surface, position, value);
        Assert.True(support.Observe(position));
    }

    private static ulong Mask(params int[] values) => values.Aggregate(0UL, (mask, value) => mask | (1UL << value));
}
