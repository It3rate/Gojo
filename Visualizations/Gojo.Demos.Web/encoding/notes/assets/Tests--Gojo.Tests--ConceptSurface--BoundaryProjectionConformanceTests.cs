using Gojo.ConceptSurface;
using Gojo.Core;

namespace Gojo.Tests.ConceptSurface;

public sealed class BoundaryProjectionConformanceTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void LegalAndBlockedProjectionBothKeepFractionalSourceRemainder(bool allowed)
    {
        var source = new Carrier();
        var target = new Carrier();
        var sourceBasis = CarrierAxisSurface.SignedForward(new Carrier(), source);
        var targetBasis = BoundaryFlowLaneBasis.Create(new Carrier(),
            allowed ? BoundaryExposure.FullSigned : BoundaryExposure.Positive,
            FlowExposure.Forward, Relation.Segment(target, -10, 10));
        var value = BoundaryFlowValueSurface.Create(new Carrier(), source, 1,
            BoundaryFlowLane.NegativeForward, sourceBasis);
        var projection = new Relation(Relation.Point(source, 3), Relation.Point(target, 10));
        Assert.True(BoundaryFlowValueRoute.TryProject(value, projection, targetBasis, target, out var route));
        Assert.True(BoundaryFlowValueRoute.TryRead(route, out _, out _, out _, out _, out var residue));
        Assert.True(BoundaryFlowValueRoute.TryReadTargetValue(route, out var targetRead));
        Assert.Equal(allowed, targetRead.IsLegal);
        Assert.Equal(-3, targetRead.SignedTicks);
        if (!allowed)
        {
            var group = Assert.IsType<Relation>(residue);
            Assert.True(group.TryReadOrderedElementsOn(Assert.IsType<Carrier>(group.Right), out var elements));
            Assert.Equal(2, elements.Length);
            Assert.Equal(Relation.Point(target, -3), elements[0]);
            residue = elements[1];
        }
        Assert.True(ProjectionResidue.TryRead(residue, out var remainderSource, out var n, out var d));
        Assert.Same(source, remainderSource);
        Assert.Equal(-1, n);
        Assert.Equal(10, d);
        Assert.Equal(0UL, source.Value);
        Assert.Equal(0UL, target.Value);
        Assert.Equal(0, source.Tension);
        Assert.Equal(0, target.Tension);
    }
}
