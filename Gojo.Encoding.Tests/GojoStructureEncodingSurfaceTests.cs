using Gojo.Core;
using Gojo.Encoding;

namespace Gojo.Encoding.Tests;

public sealed class GojoStructureEncodingSurfaceTests
{
    [Fact]
    public void DiagnosticSnapshotCapturesPlainGojoRelationWithoutConceptHints()
    {
        var block = Relation.Segment(new Carrier(), 0, 10);
        var encoded = GojoStructureEncodingSurface.CreateDiagnosticSnapshot(
            new Carrier(),
            "glossary/resolution-tension/resolution-read",
            block);

        Assert.True(GojoStructureEncodingSurface.TryRead(encoded, out var read));
        Assert.Equal("glossary/resolution-tension/resolution-read", read.SourceId);
        Assert.Same(block, read.OpenedStructure);

        var snapshot = GojoEncodedSnapshot.Capture(block);
        Assert.Equal(snapshot.Roots.Length, read.Roots.Length);
        Assert.Equal(snapshot.Carriers.Length, read.Carriers.Length);
        Assert.Equal(snapshot.TickRefs.Length, read.TickRefs.Length);
        Assert.Equal(snapshot.CarrierLocations.Length, read.CarrierLocations.Length);
        Assert.Equal(snapshot.Relations.Length, read.Relations.Length);
        Assert.Equal(snapshot.UnitShapes.Length, read.UnitShapes.Length);
        Assert.All(read.Roots, root => Assert.True(GojoStructureEncodingSurface.TryReadRef(root, out _)));
    }

    [Fact]
    public void SharedTickReferenceIsVisibleInDiagnosticLocationRows()
    {
        var x = new Carrier();
        var y = new Carrier();
        var sharedTick = new TickRef(7);
        var block = new Relation(new CarrierLocation(x, sharedTick), new CarrierLocation(y, sharedTick));
        var encoded = GojoStructureEncodingSurface.CreateDiagnosticSnapshot(new Carrier(), "shared-tick", block);

        Assert.True(GojoStructureEncodingSurface.TryRead(encoded, out var read));

        var tickRefs = ReadTickRefs(read);
        var locations = ReadCarrierLocations(read);
        var sharedTickRow = Assert.Single(tickRefs, row => row.Ticks == 7);
        var sharedLocations = locations.Where(row => row.TickRefIndex == sharedTickRow.Index).ToArray();

        Assert.Equal(2, sharedLocations.Length);
        Assert.NotEqual(sharedLocations[0].CarrierIndex, sharedLocations[1].CarrierIndex);
    }

    [Fact]
    public void PlaceholderTensionRemainsAsUnresolvedDiagnosticResidue()
    {
        var tension = TensionSlot();
        var encoded = GojoStructureEncodingSurface.CreateDiagnosticSnapshot(new Carrier(), "tension-slot", tension);

        Assert.True(GojoStructureEncodingSurface.TryRead(encoded, out var read));

        var relationRows = ReadRelations(read);
        Assert.Contains(relationRows, row => row.Left.IsNone && row.Right.IsNone);
    }

    [Fact]
    public void DiagnosticSnapshotRowsDoNotMeanCompleteRelationKnowledge()
    {
        var point = Relation.Point(new Carrier(), 0);
        var encoded = GojoStructureEncodingSurface.CreateDiagnosticSnapshot(
            new Carrier(),
            "diagnostic/point-glimpse",
            point);

        Assert.True(GojoStructureEncodingSurface.TryRead(encoded, out var read));

        Assert.Same(point, read.OpenedStructure);
        Assert.NotEmpty(read.Relations);
        Assert.NotEmpty(read.Carriers);
    }

    [Fact]
    public void FirstResolutionTensionPageLikeBlocksProduceIndependentDiagnosticSnapshots()
    {
        var blocks = new Dictionary<string, IElement>
        {
            ["resolution-read"] = PartitionedSegment(),
            ["unspecified-site"] = Group(Site(), TensionSlot()),
            ["closure-tension"] = Group(SharedPolyline(), TensionSlot()),
            ["failed-read"] = Group(LineSegment(), TensionSlot()),
            ["residue-witness"] = Group(LineSegment(), TensionSlot()),
            ["settled-read"] = SharedPolyline()
        };

        foreach (var (id, block) in blocks)
        {
            var encoded = GojoStructureEncodingSurface.CreateDiagnosticSnapshot(
                new Carrier(),
                $"glossary/resolution-tension/{id}",
                block);

            Assert.True(GojoStructureEncodingSurface.TryRead(encoded, out var read));
            Assert.Equal($"glossary/resolution-tension/{id}", read.SourceId);
            Assert.Same(block, read.OpenedStructure);
            Assert.NotEmpty(read.Roots);
            Assert.NotEmpty(read.Relations);
            Assert.False(ConceptSeedSurface.TryRead(encoded, out _));
        }
    }

    private static GojoStructureEncodingSurface.EncodedTickRefRow[] ReadTickRefs(
        GojoStructureEncodingSurface.StructureRead read) =>
        [.. read.TickRefs.Select(ReadTickRef)];

    private static GojoStructureEncodingSurface.EncodedCarrierLocationRow[] ReadCarrierLocations(
        GojoStructureEncodingSurface.StructureRead read) =>
        [.. read.CarrierLocations.Select(ReadCarrierLocation)];

    private static GojoStructureEncodingSurface.EncodedRelationRow[] ReadRelations(
        GojoStructureEncodingSurface.StructureRead read) =>
        [.. read.Relations.Select(ReadRelation)];

    private static GojoStructureEncodingSurface.EncodedTickRefRow ReadTickRef(IElement row)
    {
        Assert.True(GojoStructureEncodingSurface.TryReadTickRefRow(row, out var tickRef));
        return tickRef;
    }

    private static GojoStructureEncodingSurface.EncodedCarrierLocationRow ReadCarrierLocation(IElement row)
    {
        Assert.True(GojoStructureEncodingSurface.TryReadCarrierLocationRow(row, out var location));
        return location;
    }

    private static GojoStructureEncodingSurface.EncodedRelationRow ReadRelation(IElement row)
    {
        Assert.True(GojoStructureEncodingSurface.TryReadRelationRow(row, out var relation));
        return relation;
    }

    private static Relation PartitionedSegment()
    {
        var axis = new Carrier();
        return Group(
            Relation.Segment(axis, 0, 12),
            Relation.Point(axis, 3),
            Relation.Point(axis, 6),
            Relation.Point(axis, 9));
    }

    private static Relation Site() =>
        Relation.Point(new Carrier(), 0);

    private static Relation LineSegment() =>
        Relation.Segment(new Carrier(), 0, 10);

    private static Relation SharedPolyline()
    {
        var axis = new Carrier();
        var a = new TickRef(0);
        var b = new TickRef(5);
        var c = new TickRef(9);

        return Group(
            Segment(axis, a, b),
            Segment(axis, b, c),
            Segment(axis, c, a));
    }

    private static Relation Segment(Carrier carrier, TickRef start, TickRef end) =>
        new(Point(carrier, start), Point(carrier, end));

    private static Relation Point(Carrier carrier, TickRef tick) =>
        new(new CarrierLocation(carrier, tick), carrier);

    private static Relation Group(params IElement[] elements) =>
        Relation.OrderedGroup(new Carrier(), elements);

    private static Relation TensionSlot()
    {
        var slot = new Placeholder();
        return new Relation(slot, slot.Dual());
    }
}
