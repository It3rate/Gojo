using Xunit;

namespace Gojo.EncodingC.Tests;

public sealed class OperationRepartitionTests
{
    [Theory]
    [InlineData(5, 10, 3, 6, 0)]
    [InlineData(5, 8, 3, 4, 4)]
    [InlineData(5, 3, 4, 2, 2)]
    [InlineData(5, 8, 0, 0, 0)]
    [InlineData(1, 8, 1, 8, 0)]
    public void RepartitionKeepsTheUnrepresentedPart(int from, int to, int count, int target, int remainder)
    {
        var (source, result, residue, read) = Arrange(from, to);
        source.Restrict(0, 1UL << count);

        Assert.True(read.Apply().Unadmitted.None);
        Assert.Equal(1UL << target, result.Values(0));
        Assert.Equal(1UL << remainder, residue.Values(0));
        Assert.Equal(1UL << count, source.Values(0));
        Assert.Equal(count * to, target * from + remainder);
    }

    [Fact]
    public void ACoarsePublicationKeepsTheCouplingBetweenRoundedValueAndResidue()
    {
        var (source, target, residue, read) = Arrange(5, 8);
        source.Restrict(0, (1UL << 3) | (1UL << 4));
        var engine = new Engine().Add(read);

        Assert.True(Assert.Single(engine.Step().Steps).Ambiguous[0]);
        Assert.Equal((1UL << 4) | (1UL << 6), target.Values(0));
        Assert.Equal((1UL << 2) | (1UL << 4), residue.Values(0));

        // These four marginal values belong to only two pairs. After loading,
        // select four eighths and make the same bound read to resolve the rest.
        var saved = Transport.Save(engine);
        var (restored, stores) = Transport.Load(saved);
        var local = stores[0];
        var selectedTarget = new Supports(local, target.Field, [.. target.Planes]);
        var selectedResidue = new Supports(local, residue.Field, [.. residue.Planes]);
        var selectedSource = new Supports(local, source.Field, [.. source.Planes]);
        selectedTarget.Restrict(0, 1UL << 4);

        Assert.True(Assert.Single(restored.Step().Steps).Unadmitted.None);
        Assert.Equal(1UL << 4, selectedResidue.Values(0));
        Assert.Equal(1UL << 3, selectedSource.Values(0));
        Assert.Equal((1UL << 3) | (1UL << 4), source.Values(0));
    }

    [Fact]
    public void AResultAndResidueFromDifferentAlternativesDoNotMakeAValidRead()
    {
        var (source, target, residue, read) = Arrange(5, 8);
        source.Restrict(0, (1UL << 3) | (1UL << 4));
        target.Restrict(0, 1UL << 4);
        residue.Restrict(0, 1UL << 2);

        Assert.True(read.Apply().Unadmitted[0]);
        Assert.True(source.Empty[0]);
        Assert.True(target.Empty[0]);
        Assert.True(residue.Empty[0]);
    }

    [Fact]
    public void PoolingCountsAndAddingPortionsChooseDifferentWholes()
    {
        var fifths = PartitionReads.Repartition(5, 15);
        var thirds = PartitionReads.Repartition(3, 15);
        ulong[] left = [1UL << 2, ulong.MaxValue, 1UL];
        ulong[] right = [1UL << 1, ulong.MaxValue, 1UL];
        Assert.Equal(1UL << 6, fifths.Project(1, left));
        Assert.Equal(1UL << 5, thirds.Project(1, right));

        var commonWhole = PartitionReads.SumOnCommonWhole(15);
        ulong[] portions = [fifths.Project(1, left), thirds.Project(1, right), ulong.MaxValue, ulong.MaxValue];
        Assert.Equal(1UL << 11, commonWhole.Project(2, portions));
        Assert.Equal(1UL << 15, commonWhole.Project(3, portions));

        var disjointPools = PartitionReads.PoolDisjoint(5, 3);
        ulong[] counts = [left[0], right[0], ulong.MaxValue, ulong.MaxValue];
        Assert.Equal(1UL << 3, disjointPools.Project(2, counts));
        Assert.Equal(1UL << 8, disjointPools.Project(3, counts));
    }

    private static (Supports Source, Supports Target, Supports Residue, Wiring Read) Arrange(int from, int to)
    {
        var law = PartitionReads.Repartition(from, to);
        var store = new Store(0, 1);
        var roles = law.Roles.Select(shape => Supports.On(store,
            new Field(shape, [.. Enumerable.Range(0, shape.Bits).Select(_ => store.AddPlane())]))).ToArray();
        var read = new Wiring(law, [.. roles.Select(role => role.Variable)]);
        read.Add(0, 0, 0);
        return (roles[0], roles[1], roles[2], read);
    }
}
