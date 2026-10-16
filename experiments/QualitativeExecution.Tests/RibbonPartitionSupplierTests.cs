using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeFrieze;

namespace Gojo.Experiments.QualitativeExecution.Tests;

public sealed class RibbonPartitionSupplierTests
{
    [Fact]
    public void SharedElementaryBandAndGapRemainNativeRepeatedParticipation()
    {
        var fixture = new PatternShapingLibrary().Patterns[2].Fixture; var common = new Placeholder();
        var supplier = new RibbonPartitionSupplier(fixture, common, common);
        AssertNoNumbers(supplier.Surface);
        var promises = E((Relation)supplier.Surface.Right).Cast<Relation>().ToArray();
        Assert.Equal(2, promises.Length);
        Assert.All(promises, promise => Assert.All(E((Relation)promise.Right), value => Assert.Same(common, value)));
        Assert.All(promises, promise => Assert.Equal(2, E((Relation)promise.Right).Length));
        var trace = fixture.Run(7); var calibration = Calibration(fixture);
        var read = supplier.Interpret(trace, calibration, 2, 2);
        Assert.Equal(0, read.Projection.Unresolved);
        Assert.True(Contains(read.Surface, supplier.Surface)); Assert.True(Contains(read.Surface, calibration));
        Assert.True(Contains(read.Surface, trace));
    }

    [Fact]
    public void ExactAlignedCellsPublishTwoOccupiedRolesAndKeepRailsOutsideTheDomain()
    {
        var fixture = FriezeFixture.SquareWave(false); var extent = new Placeholder();
        var read = new RibbonPartitionSupplier(fixture, extent, extent).Interpret(fixture.Run(4), Calibration(fixture), 2, 2);
        Assert.Equal(0, read.Projection.Unresolved);
        Assert.Equal(read.Projection.Columns * read.Projection.Rows, read.Projection.First + read.Projection.Second);
        Assert.True(read.Projection.First > 0); Assert.True(read.Projection.Second > 0);
        Assert.True(read.Partition.TryReadResult(read.Read, out var first, out var second, out _, out var interfaces, out var unknown));
        Assert.NotEmpty(E(interfaces)); Assert.Empty(E(unknown));
        Assert.All(E(first).Cast<Relation>(), row => Assert.Same(read.Partition.RolePair.Left, row.Right));
        Assert.All(E(second).Cast<Relation>(), row => Assert.Same(read.Partition.RolePair.Right, row.Right));
        Assert.Equal(2, E(read.Rails).Length);
        foreach (var rail in E(read.Rails).Cast<Relation>())
        {
            Assert.True(FriezeRealization.TryReadPoint((Relation)rail.Left, out _, out var y));
            Assert.True(y < read.Projection.MinimumY || y > read.Projection.MaximumY);
        }
    }

    [Fact]
    public void OneSharedExtentCannotReceiveTwoDifferentMeasuredWidths()
    {
        var fixture = FriezeFixture.SquareWave(false); var extent = new Placeholder();
        var read = new RibbonPartitionSupplier(fixture, extent, extent).Interpret(fixture.Run(4), Calibration(fixture), 1, 3);
        Assert.True(read.Projection.Unresolved > 0);
        Assert.Equal(0, read.Projection.First + read.Projection.Second);
        Assert.All(read.Exchanges, probe => Assert.False(probe.Passed));
    }

    [Fact]
    public void ThickerInterpretationChangesThePartitionWithoutChangingTheSourceProgram()
    {
        var fixture = new PatternShapingLibrary().Patterns[2].Fixture;
        var supplier = new RibbonPartitionSupplier(fixture, new Placeholder(), new Placeholder());
        var trace = fixture.Run(7); var calibration = Calibration(fixture); var source = fixture.Surface;
        var thin = supplier.Interpret(trace, calibration, 1, 3);
        var heavy = supplier.Interpret(trace, calibration, 3, 1);
        Assert.Equal(0, thin.Projection.Unresolved); Assert.Equal(0, heavy.Projection.Unresolved);
        Assert.Equal(thin.Projection.Columns * thin.Projection.Rows, heavy.Projection.Columns * heavy.Projection.Rows);
        Assert.True(heavy.Projection.First > thin.Projection.First);
        Assert.Same(source, fixture.Surface); Assert.Same(calibration, thin.Calibration); Assert.Same(calibration, heavy.Calibration);
        AssertNoNumbers(supplier.Surface);
    }

    [Fact]
    public void DiagonalInkIsNotInventedByTheRectilinearSupplier()
    {
        var frame = new QualitativeMotionFrame(); var h = new Placeholder(); var v = new Placeholder();
        var fixture = FriezeFixture.Sequence(frame, S(S(M(frame.Right, h), M(frame.Up, v)), S(M(frame.Right, h), M(frame.Down, v))));
        var extent = new Placeholder(); var read = new RibbonPartitionSupplier(fixture, extent, extent)
            .Interpret(fixture.Run(2), Calibration(fixture), 2, 2);
        Assert.True(read.Projection.Unresolved > 0); Assert.Empty(E(read.Partition.Cells));
    }

    [Theory]
    [InlineData(2, 2, 1)]
    [InlineData(4, 0, 8192)]
    [InlineData(2, 3, 8192)]
    public void UnavailableCapacityOrIncompatiblePitchRemainUnresolved(long band, long gap, int budget)
    {
        var fixture = FriezeFixture.SquareWave(false);
        var read = new RibbonPartitionSupplier(fixture, new Placeholder(), new Placeholder())
            .Interpret(fixture.Run(4), Calibration(fixture), band, gap, margin: 4, cellBudget: budget);
        Assert.True(read.Projection.Unresolved > 0);
        Assert.All(read.Exchanges, probe => Assert.False(probe.Passed));
    }

    private static Relation Calibration(FriezeFixture fixture) =>
        FriezeRealization.CreateCalibration(fixture, FriezeRealization.DefaultLengths(fixture, 4, 4));
    private static Relation M(IElement direction, IElement reach) => QualitativeMotionFrame.Move(direction, reach);
    private static Relation S(params IElement[] values) => StructuralConstraint.Closed(values);
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
    private static bool Contains(IElement root, IElement target) => Traverse(root).Any(value => ReferenceEquals(value, target));
    private static void AssertNoNumbers(IElement root) => Assert.DoesNotContain(Traverse(root), value => value is Carrier or CarrierLocation);
    private static IEnumerable<IElement> Traverse(IElement root)
    {
        var seen = new HashSet<IElement>(ReferenceEqualityComparer.Instance); var pending = new Stack<IElement>(); pending.Push(root);
        while (pending.TryPop(out var value))
        {
            if (!seen.Add(value)) continue; yield return value;
            if (value is Relation relation) { pending.Push(relation.Left); pending.Push(relation.Right); }
        }
    }
}
