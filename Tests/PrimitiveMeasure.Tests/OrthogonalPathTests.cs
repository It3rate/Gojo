using Gojo.Experiments.PrimitiveMeasure;

namespace Gojo.Experiments.PrimitiveMeasure.Tests;

public sealed class OrthogonalPathTests
{
    [Fact]
    public void SharedUnknownReachesCloseRectangleWithExplicitQuarterTurnLayout()
    {
        var shape = Build([2, 2]);
        Assert.True(shape.Read().GuaranteesRectangle);
        Assert.Equal(new[] { 0, 1, 4 }, shape.Wiring(0));
        Assert.Equal(new[] { 1, 2, 5 }, shape.Wiring(1));
        Assert.Equal(new[] { 2, 3, 4 }, shape.Wiring(2));
        Assert.Equal(new[] { 3, 0, 5 }, shape.Wiring(3));
        Assert.Equal(new byte[] { 0x08, 0x01 }, shape.PackedLane(0));
        Assert.Equal(new byte[] { 0x51, 0x01 }, shape.PackedLane(1));
        Assert.Equal(new byte[] { 0x1A, 0x01 }, shape.PackedLane(2));
        Assert.Equal(new byte[] { 0x43, 0x01 }, shape.PackedLane(3));
    }

    [Fact]
    public void FourEqualNonzeroBitsDoNotEstablishEqualOppositeReaches()
    {
        var shape = Build([2, 2, 2, 2], [4, 5, 6, 7]);
        var read = shape.Read();
        Assert.True(read.FourCornerCycle);
        Assert.False(read.SharedOppositeReaches);
        Assert.Null(read.NondegeneracySupport);
        Assert.False(read.GuaranteesRectangle);
    }

    [Theory]
    [InlineData(1, 2, 1)]
    [InlineData(2, 1, 1)]
    [InlineData(1, 1, 1)]
    [InlineData(2, 2, 2)]
    [InlineData(3, 2, 3)]
    [InlineData(3, 3, 3)]
    [InlineData(0, 2, 0)]
    public void ZeroNonzeroUncertainAndMissingReadsRemainDifferent(byte a, byte b, byte expected)
    {
        var shape = Build([a, b]);
        var rows = Enumerable.Range(0, 4).Select(shape.PackedLane).ToArray();
        Assert.Equal((byte?)expected, shape.Read().NondegeneracySupport);
        Assert.True(shape.Read().FourCornerCycle);
        for (var i = 0; i < 4; i++) Assert.Equal(rows[i], shape.PackedLane(i));
    }

    [Fact]
    public void ChangingReachObservationPreservesCornerCorrespondence()
    {
        var owner = new PortStructure();
        var binding = owner.Create(0, 0, 1, 1);
        var shape = new OrthogonalPathFixture(owner.Connect(owner.Publish(binding)));
        for (var lane = 0; lane < 4; lane++) shape.Connect(lane, lane, (lane + 1) % 4, 4);
        Assert.True(shape.Read().GuaranteesRectangle);
        owner.StoreObservation(binding, false, false, 0);
        Assert.Equal((byte?)1, shape.Read().NondegeneracySupport);
        Assert.True(shape.Read().FourCornerCycle);
        owner.StoreObservation(binding, false, false, 1);
        Assert.True(shape.Read().GuaranteesRectangle);
    }

    [Fact]
    public void AnUnjoinedEndDoesNotBecomeClosedBecauseItsReachLooksRight()
    {
        var shape = new OrthogonalPathFixture(new Read(2), new Read(2));
        shape.Connect(0, 0, 1, 4);
        shape.Connect(1, 1, 2, 5);
        shape.Connect(2, 2, 3, 4);
        shape.Connect(3, 3, 1, 5);
        Assert.False(shape.Read().FourCornerCycle);
        Assert.True(shape.Read().SharedOppositeReaches);
        Assert.False(shape.Read().GuaranteesRectangle);
    }

    [Fact]
    public void MissingOrExtraEdgesDoNotSatisfyTheNarrowFourEdgeRead()
    {
        var shape = new OrthogonalPathFixture(new Read(2));
        for (var lane = 0; lane < 3; lane++) shape.Connect(lane, lane, lane + 1, 4);
        Assert.Null(shape.Read().NondegeneracySupport);
        shape.Connect(3, 3, 0, 4);
        Assert.True(shape.Read().GuaranteesRectangle);
        shape.Connect(3, 3, 0, 4);
        Assert.Null(shape.Read().NondegeneracySupport);
    }

    [Fact]
    public void PerimeterOrderAndParallelPairAssemblyGiveSameEncodedBoundary()
    {
        var path = Build([2, 2]);
        var pairs = new OrthogonalPathFixture(new Read(2), new Read(2));
        pairs.Connect(0, 0, 1, 4);
        pairs.Connect(2, 2, 3, 4);
        pairs.Connect(1, 1, 2, 5);
        pairs.Connect(3, 3, 0, 5);
        for (var lane = 0; lane < 4; lane++) Assert.Equal(path.PackedLane(lane), pairs.PackedLane(lane));
        Assert.Equal(path.Read(), pairs.Read());
        // This equality is for the assembled boundary. These bytes do not
        // preserve either builder's history or encode a swept filled interior.
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(2, 9)]
    [InlineData(17, 3)]
    public void DifferentFineRealizationsSatisfyTheSameUnknownReachStructure(int a, int b)
    {
        var shape = Build([2, 2]);
        int[] lengths = [a, b]; // Test world only; never supplied to the fixture.
        (int X, int Y)[] axes = [(1, 0), (0, 1), (-1, 0), (0, -1)];
        var x = 0;
        var y = 0;
        for (var lane = 0; lane < 4; lane++)
        {
            var reach = lengths[shape.Wiring(lane)[2] - 4];
            x += axes[lane].X * reach;
            y += axes[lane].Y * reach;
        }
        Assert.Equal((0, 0), (x, y));
        Assert.True(shape.Read().GuaranteesRectangle);
    }

    [Fact]
    public void AnOdvOrientationChangeIsNotInterpretedAsAGeometricQuarterTurn()
    {
        var shape = Build([1 << 5, 2]);
        Assert.Throws<InvalidOperationException>(() => shape.Read());
    }

    private static OrthogonalPathFixture Build(byte[] supports, int[]? reachRefs = null)
    {
        var shape = new OrthogonalPathFixture(supports.Select(s => (IPrimitiveReadPort)new Read(s)).ToArray());
        reachRefs ??= [4, 5, 4, 5];
        for (var lane = 0; lane < 4; lane++) shape.Connect(lane, lane, (lane + 1) % 4, reachRefs[lane]);
        return shape;
    }

    private sealed class Read(byte support) : IPrimitiveReadPort
    {
        public byte ReadSupport() => support;
    }
}
