using Gojo.Core;
using GojoCAD.Structures;

namespace GojoCAD.Tests;

public sealed class GraphicOrthogonalFillSurfaceTests
{
    [Fact]
    public void OrthogonalFillParticipantsCreateInnerAndOuterCircleBands()
    {
        var basis = new TestBasis();
        var circle = Circle(basis, 0, 0, 40);
        var witness = GraphicOffsetSurface.BoundarySideWitness(
            new Carrier(),
            circle,
            circle,
            Group(),
            Group());

        var participants = GraphicOrthogonalFillSurface.OrthogonalFillParticipants(witness, 10);

        var areas = GraphicReadGraph.From(Group(participants))
            .ReadsOf<SharedFrameAreaRead>()
            .DistinctBy(area => ReadSourceId(area.Source))
            .ToArray();
        Assert.Equal(8, areas.Count(area => !area.IsAntiArea && area.Boundary.Arcs is { Count: > 0 }));
        Assert.DoesNotContain(areas, area => area.IsAntiArea);
    }

    [Fact]
    public void OrthogonalFillParticipantsCanKeepEndpointCircleOutwardOnly()
    {
        var basis = new TestBasis();
        var circle = Circle(basis, 0, 0, 40);
        var witness = GraphicOffsetSurface.BoundarySideWitness(
            new Carrier(),
            circle,
            circle,
            Group(),
            Group());

        var participants = GraphicOrthogonalFillSurface.OrthogonalFillParticipants(witness, 10, circle);

        var areas = GraphicReadGraph.From(Group(participants))
            .ReadsOf<SharedFrameAreaRead>()
            .DistinctBy(area => ReadSourceId(area.Source))
            .ToArray();
        Assert.Equal(4, areas.Count(area => !area.IsAntiArea && area.Boundary.Arcs is { Count: > 0 }));
        Assert.DoesNotContain(areas, area => area.IsAntiArea);
    }

    private static Relation Circle(TestBasis basis, long centerX, long centerY, long radius) =>
        Group(
            GraphicStructure.FrameArc(basis.P(centerX, centerY), basis.P(centerX + radius, centerY), basis.P(centerX, centerY + radius)),
            GraphicStructure.FrameArc(basis.P(centerX, centerY), basis.P(centerX, centerY + radius), basis.P(centerX - radius, centerY)),
            GraphicStructure.FrameArc(basis.P(centerX, centerY), basis.P(centerX - radius, centerY), basis.P(centerX, centerY - radius)),
            GraphicStructure.FrameArc(basis.P(centerX, centerY), basis.P(centerX, centerY - radius), basis.P(centerX + radius, centerY)));

    private static Relation Group(params IElement[] elements) =>
        Relation.OrderedGroup(new Carrier(), elements ?? []);

    private static ulong ReadSourceId(IElement source) =>
        source is Relation relation ? relation.Id : 0UL;

    private sealed class TestBasis
    {
        private readonly Carrier _frame = new();
        private readonly Carrier _x = new();
        private readonly Carrier _y = new();

        public Relation P(long xTicks, long yTicks) =>
            GraphicStructure.FramePoint(
                _frame,
                new CarrierLocation(_x, xTicks),
                new CarrierLocation(_y, yTicks));
    }
}
