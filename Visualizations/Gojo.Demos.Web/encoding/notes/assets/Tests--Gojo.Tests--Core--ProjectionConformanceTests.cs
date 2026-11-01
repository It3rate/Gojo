using System.Numerics;
using Gojo.Core;
using Gojo.Structures;

namespace Gojo.Tests.Core;

public sealed class ProjectionConformanceTests
{
    [Theory]
    [InlineData(3, 2, 10, 6, 1, 1)]
    [InlineData(3, 10, 1, 3, 1, 10)]
    [InlineData(10, 3, 7, 2, 1, 3)]
    [InlineData(3, 2, -10, -6, -1, 1)]
    [InlineData(-3, 2, 10, -6, 1, 1)]
    [InlineData(3, -10, 1, -3, 1, 10)]
    public void RemainderIsExpressedInSourceTicks(
        long sourceSpan, long targetSpan, long input, long expected, long numerator, long denominator)
    {
        var source = new Carrier();
        var target = new Carrier();
        var route = new Relation(Relation.Point(source, sourceSpan), Relation.Point(target, targetSpan));
        Assert.True(route.TryProject(new CarrierLocation(source, input), target, out var projection));
        Assert.True(projection.TryReadProjection(out var value, out var residue));
        Assert.Equal(expected, value.Ticks);
        AssertResidue(residue, source, numerator, denominator);
    }

    [Fact]
    public void SignedOffsetAndReversedFramesReconstructInputAtEveryTestedRatio()
    {
        var source = new Carrier();
        var target = new Carrier();
        source.Set(17);
        target.Set(23);
        // 5,000 reads, including both signs, both orientations and nonzero origins.
        foreach (var origin in new[] { (Source: 0L, Target: 0L), (Source: 7L, Target: -4L) })
        for (var a = -5; a <= 5; a++)
        for (var b = -5; b <= 5; b++)
        {
            if (a == 0 || b == 0) continue;
            var left = Relation.Segment(source, origin.Source, origin.Source + a);
            var right = Relation.Segment(target, origin.Target, origin.Target + b);
            var route = new Relation(left, right);
            var reversedRelation = new Relation(right, left);
            for (var x = -12; x <= 12; x++)
            {
                var location = new CarrierLocation(source, x);
                Assert.True(route.TryProject(location, target, out var projection));
                Assert.True(projection.TryReadProjection(out var y, out var residue));
                Assert.True(ProjectionResidue.TryRead(residue, out var residueSource, out var n, out var d));
                Assert.Same(source, residueSource);
                Assert.Same(target, y.Carrier);
                Assert.True(d > 0);
                Assert.Equal(BigInteger.One, BigInteger.GreatestCommonDivisor(n, d));
                Assert.Equal(origin.Target + ((BigInteger)x - origin.Source) * b / a, (BigInteger)y.Ticks);
                // x = source origin + represented target converted back + source residue.
                // Clear denominators independently of the implementation's DivRem formula.
                Assert.Equal(((BigInteger)x - origin.Source) * b * d,
                    ((BigInteger)y.Ticks - origin.Target) * a * d + (BigInteger)n * b);
                Assert.Equal(projection, reversedRelation.Project(location, target));
            }
        }
        Assert.Equal(17UL, source.Value);
        Assert.Equal(23UL, target.Value);
        Assert.Equal(0, source.Tension);
        Assert.Equal(0, target.Tension);
    }

    [Fact]
    public void ZeroSourceSpanRefusesProjectionAndImportWithoutMutation()
    {
        var source = new Carrier();
        var target = new Carrier();
        target.Set(11);
        var targetFrame = Relation.Segment(target, 2, 8);
        var route = new Relation(Relation.Segment(source, 4, 4), targetFrame);
        var input = new CarrierLocation(source, 9);
        Assert.False(route.TryProject(input, target, out var unresolved));
        Assert.False(unresolved.TryReadProjection(out _, out _));
        var pending = Assert.IsType<Relation>(unresolved.Left);
        Assert.IsType<Placeholder>(pending.Left);
        Assert.Same(target, pending.Right);
        var evidence = Assert.IsType<Relation>(unresolved.Right);
        Assert.Same(route, evidence.Left);
        Assert.Equal(input, evidence.Right);
        Assert.False(target.TrySetFrom(route, input));
        Assert.False(route.TrySetStructureFrom(input));
        Assert.Equal(Relation.Segment(target, 2, 8), targetFrame);
        Assert.Equal(11UL, target.Value);
        Assert.Equal(0, source.Tension);
    }

    [Fact]
    public void ConstantTargetFrameReadsItsOffsetAndRetainsTheInput()
    {
        var source = new Carrier();
        var target = new Carrier();
        var route = new Relation(Relation.Segment(source, 2, 5), Relation.Segment(target, 7, 7));
        var input = new CarrierLocation(source, 9);
        Assert.True(route.TryProject(input, target, out var projection));
        Assert.Equal(new Relation(new CarrierLocation(target, 7), input), projection);
    }

    [Theory]
    [InlineData(0, 1, 0, 1, long.MinValue, long.MinValue)]
    [InlineData(0, 4000000000, 0, 4000000000, 8000000000, 8000000000)]
    [InlineData(long.MinValue, long.MaxValue, long.MinValue, long.MaxValue, 0, 0)]
    [InlineData(long.MinValue, 0, long.MinValue, 0, long.MaxValue, long.MaxValue)]
    public void RepresentableProjectionSurvivesWideIntermediateArithmetic(
        long a, long b, long c, long d, long x, long expected)
    {
        var source = new Carrier();
        var target = new Carrier();
        var route = new Relation(Relation.Segment(source, a, b), Relation.Segment(target, c, d));
        Assert.True(route.TryProject(new CarrierLocation(source, x), target, out var projection));
        Assert.Equal(new Relation(new CarrierLocation(target, expected), new CarrierLocation(source, 0)), projection);
    }

    [Fact]
    public void OutOfRangeFinalProjectionThrowsBeforeImportChangesState()
    {
        var source = new Carrier();
        var target = new Carrier();
        target.Set(11);
        var route = new Relation(Relation.Point(source, 1), Relation.Point(target, 2));
        Assert.Throws<OverflowException>(() => target.TrySetFrom(route, new CarrierLocation(source, long.MaxValue)));
        Assert.Equal(11UL, target.Value);
        Assert.Equal(0, source.Tension);
    }

    [Fact]
    public void CarrierReadSupportsMinimumSignedCoordinateAndChecksItsFinalSignedRange()
    {
        var source = new Carrier();
        var location = new CarrierLocation(source, long.MinValue);
        var read = CarrierRead.From(location, source);
        Assert.Equal(1UL << 63, read.Magnitude);
        Assert.Equal(long.MinValue, read.ProjectedTicks);
        Assert.Equal(0, CarrierRead.From(location, new Carrier()).ProjectedTicks);
        Assert.Equal(0, CarrierRead.From(location, source.Inverse()).ProjectedTicks);
        Assert.Throws<OverflowException>(() => new CarrierRead(source, source, 1UL << 63, 1).ProjectedTicks);
    }

    [Theory]
    [InlineData(10, 6, 1)]
    [InlineData(-10, -6, -1)]
    public void WholeRemainderImportsRetainExactlyTheUnaccountedSource(long input, long output, long residue)
    {
        var source = new Carrier();
        var target = new Carrier();
        var route = new Relation(Relation.Point(source, 3), Relation.Point(target, 2));
        Assert.True(target.TrySetFrom(route, new CarrierLocation(source, input)));
        Assert.Equal((ulong)Math.Max(0, output), target.Value);
        Assert.Equal(Math.Min(0, output), target.Tension);
        Assert.Equal(residue, source.Tension);
        Assert.Equal((BigInteger)input * 2, (BigInteger)output * 3 + residue * 2);
    }

    [Fact]
    public void StructuralImportRetargetsWithCorrectWholeSourceResidue()
    {
        var source = new Carrier();
        var target = new Carrier();
        var endpoint = Relation.Point(target, 2);
        var tick = Assert.IsType<CarrierLocation>(endpoint.Left).Tick;
        var route = new Relation(Relation.Point(source, 3), endpoint);
        Assert.True(route.TrySetStructureFrom(new CarrierLocation(source, 10)));
        Assert.Equal(6, tick.Ticks);
        Assert.Equal(1, source.Tension);
        Assert.Equal(0UL, target.Value);
    }

    [Fact]
    public void FractionalResidueRemainsObservableWhenWholeTickImportIsRefused()
    {
        var source = new Carrier();
        var target = new Carrier();
        source.Set(1);
        target.Set(11);
        var route = new Relation(Relation.Point(source, 3), Relation.Point(target, 10));
        Assert.False(target.TrySetFrom(route, source.ValueSlot));
        Assert.Equal(1UL, source.Value);
        Assert.Equal(11UL, target.Value);
        Assert.Equal(0, source.Tension);
        Assert.True(route.Project(source.ValueSlot, target).TryReadProjection(out var y, out var residue));
        Assert.Equal(3, y.Ticks);
        AssertResidue(residue, source, 1, 10);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void LaterFractionalSlotRejectsTheWholeImportBeforeEarlierValidSlotChanges(bool structural)
    {
        var x = new Carrier();
        var y = new Carrier();
        var tx = new Carrier();
        var ty = new Carrier();
        tx.Set(11);
        ty.Set(13);
        var firstTarget = Relation.Point(tx, 2);
        var secondTarget = Relation.Point(ty, 10);
        var route = new Relation(new Relation(Relation.Point(x, 1), Relation.Point(y, 3)),
            new Relation(firstTarget, secondTarget));
        var input = new Relation(new CarrierLocation(x, 5), new CarrierLocation(y, 1));
        Assert.False(structural ? route.TrySetStructureFrom(input) : route.TrySetFrom(input));
        Assert.Equal(Relation.Point(tx, 2), firstTarget);
        Assert.Equal(Relation.Point(ty, 10), secondTarget);
        Assert.Equal(11UL, tx.Value);
        Assert.Equal(13UL, ty.Value);
        Assert.Equal(0, x.Tension);
        Assert.Equal(0, y.Tension);
    }

    [Fact]
    public void ExactStructuralScalingSurvivesWideProductsAndKeepsSharedSites()
    {
        var source = new Carrier();
        var tick = new TickRef(8_000_000_000L);
        var frame = new Relation(new CarrierLocation(source, tick), new CarrierLocation(source, tick));
        var scale = new Relation(Relation.Point(source, 4_000_000_000), Relation.Point(new Carrier(), 2_000_000_000));
        Assert.True(frame.Perturb(scale));
        Assert.Equal(4_000_000_000, tick.Ticks);
        Assert.Same(tick, Assert.IsType<CarrierLocation>(frame.Left).Tick);
        Assert.Same(tick, Assert.IsType<CarrierLocation>(frame.Right).Tick);
    }

    [Fact]
    public void ScaledResidueUsesReducedSameSourceStructure()
    {
        var source = new Carrier();
        AssertResidue(ProjectionResidue.Create(source, 2, -6), source, -1, 3);
        Assert.IsType<CarrierLocation>(ProjectionResidue.Create(source, 6, 3));
        AssertResidue(ProjectionResidue.Create(source, 0, -3), source, 0, 1);
        Assert.Throws<ArgumentOutOfRangeException>(() => ProjectionResidue.Create(source, 1, 0));
        Assert.False(ProjectionResidue.TryRead(new Relation(new CarrierLocation(source, 1), Relation.Point(new Carrier(), 3)), out _, out _, out _));
        Assert.False(ProjectionResidue.TryRead(new Relation(new CarrierLocation(source, 2), Relation.Point(source, 6)), out _, out _, out _));
        Assert.False(ProjectionResidue.TryRead(new Relation(new CarrierLocation(source, 1), Relation.Point(source, -3)), out _, out _, out _));
    }

    [Fact]
    public void FollowRetainsFractionalRouteResidueSeparatelyFromRemainingClaimMismatch()
    {
        var follower = new Carrier();
        var error = new Carrier();
        var follow = OperationFollow.Step(new Carrier(), Relation.Point(follower, 0), Relation.Point(follower, 5), error, 2, 3);
        Assert.True(OperationFollow.TryRead(follow, out _, out _, out var applied, out var next, out var remaining, out var routeResidue));
        Assert.Equal(3, Assert.IsType<CarrierLocation>(applied).Ticks);
        Assert.Equal(3, Assert.IsType<CarrierLocation>(next).Ticks);
        Assert.Equal(2, Assert.IsType<CarrierLocation>(remaining).Ticks);
        AssertResidue(routeResidue, error, 1, 2);
        Assert.True(OperationFollow.TryApply(follow, out _));
        Assert.Equal(3UL, follower.Value);
        Assert.Equal(0, error.Tension);
    }

    private static void AssertResidue(IElement residue, Carrier expectedSource, long n, long d)
    {
        Assert.True(ProjectionResidue.TryRead(residue, out var source, out var numerator, out var denominator));
        Assert.Same(expectedSource, source);
        Assert.Equal(n, numerator);
        Assert.Equal(d, denominator);
    }
}
