using Xunit;

namespace Gojo.EncodingC.Tests;

public sealed class BatchFlowTests
{
    [Theory]
    [InlineData(2, 1, 1, 0, true)]
    [InlineData(2, 1, 2, 0, false)]
    [InlineData(2, 1, 1, 2, false)]
    [InlineData(0, 1, 1, 0, false)]
    [InlineData(2, 0, 1, 0, false)]
    public void AReceiptAccountsForOneCaseAndEveryUnit(int source, int pending, int target, int received, bool moved)
    {
        var law = Flows.FulfillBatch(2, 2, 3, 2, 2);
        int[] before = [source, pending, target, received];
        var after = Next(law, before);

        Assert.Equal(moved ? [source - 1, pending - 1, target + 2, received + 1] : before, after);
        Assert.Equal(2 * source + target, 2 * after[0] + after[2]);
        Assert.Equal(pending + received, after[1] + after[3]);
        Assert.Equal(source + received, after[0] + after[3]);
        Assert.All(Enumerable.Range(0, 4), role => Assert.True(law.Accounting.Conserves(role)));
    }

    [Fact]
    public void EveryGateMustBeOpenAndTheGatesStayUnchanged()
    {
        var batch = Flows.FulfillBatch(1, 1, 2, 2, 1);
        var gated = Flows.When(batch, gates: 2);

        Assert.Equal([1, 1, 0, 0, 1, 0], Next(gated, [1, 1, 0, 0, 1, 0]));
        Assert.Equal([1, 1, 0, 0, 0, 1], Next(gated, [1, 1, 0, 0, 0, 1]));
        Assert.Equal([0, 0, 2, 1, 1, 1], Next(gated, [1, 1, 0, 0, 1, 1]));
        Assert.True(gated.Accounting.Invariants.Take(3).SequenceEqual(batch.Accounting.Invariants));
        Assert.False(gated.Writes(4));
        Assert.False(gated.Writes(5));
    }

    [Fact]
    public void GatingRefusesAnOversizedTableOrANonfunctionalLaw()
    {
        var ambiguous = TransitionLaw.FromPairs([Shape.Bit], [(0, 0), (0, 1), (1, 1)], Accounting.None);
        Assert.Throws<ArgumentException>(() => Flows.When(ambiguous));
        Assert.Throws<ArgumentOutOfRangeException>(() => Flows.When(Flows.Toggle, int.MaxValue));
    }

    private static int[] Next(TransitionLaw law, int[] before) => law.Tuple(Assert.Single(law.Afters(law.TupleIndex(before))));
}
