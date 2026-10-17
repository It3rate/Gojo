using Gojo.Core;

namespace Gojo.Tests.Core;

public sealed class SerializationTests
{
    [Fact]
    public void SnapshotPreservesSharedTickRefIdentityAcrossCarrierLocations()
    {
        var xAxis = new Carrier();
        var yAxis = new Carrier();
        var tick = new TickRef(3);
        var relation = new Relation(
            new CarrierLocation(xAxis, tick),
            new CarrierLocation(yAxis, tick));

        var snapshot = GojoEncodedSnapshot.Capture(relation);

        Assert.Single(snapshot.TickRefs, row => row.Id == tick.Id);
        var row = Assert.Single(snapshot.Relations, row => row.Id == relation.Id);
        var left = snapshot.CarrierLocations[row.Left.Index];
        var right = snapshot.CarrierLocations[row.Right.Index];
        Assert.Equal(left.TickRefIndex, right.TickRefIndex);
        Assert.NotEqual(left.CarrierIndex, right.CarrierIndex);
    }

    [Fact]
    public void SnapshotCapturesCarrierStateAndUnitShapeSurface()
    {
        var carrier = new Carrier();
        carrier.Set(5);
        carrier.Remove(8);

        var snapshot = GojoEncodedSnapshot.Capture(carrier);

        var row = Assert.Single(snapshot.Carriers, row => row.Id == carrier.Id);
        Assert.Equal(0UL, row.Value);
        Assert.Equal(-3, row.Tension);
        Assert.True(row.UnitShapeIndex >= 0);
        Assert.False(snapshot.UnitShapes[row.UnitShapeIndex].Surface.IsNone);
    }

    [Fact]
    public void WorldSnapshotFollowsAttachedRelations()
    {
        var source = new Carrier();
        var target = new Carrier();
        var route = new Relation(Relation.Point(source, 10), Relation.Point(target, 0));

        source.Attach(route);

        var snapshot = GojoEncodedSnapshot.CaptureWorld(source);

        Assert.Contains(snapshot.Relations, row => row.Id == route.Id);
        Assert.Contains(snapshot.Carriers, row => row.Id == source.Id);
        Assert.Contains(snapshot.Carriers, row => row.Id == target.Id);
    }

    [Fact]
    public void SnapshotRecordsExistingInverseFaceWithoutCreatingOne()
    {
        var carrier = new Carrier();

        var withoutInverse = GojoEncodedSnapshot.Capture(carrier);

        var originalRow = Assert.Single(withoutInverse.Carriers, row => row.Id == carrier.Id);
        Assert.Equal(-1, originalRow.InverseCarrierIndex);

        var inverse = carrier.Inverse();
        var withInverse = GojoEncodedSnapshot.Capture(carrier);

        var carrierRow = Assert.Single(withInverse.Carriers, row => row.Id == carrier.Id);
        var inverseRow = Assert.Single(withInverse.Carriers, row => row.Id == inverse.Id);
        Assert.Equal(inverseRow.Index, carrierRow.InverseCarrierIndex);
        Assert.Equal(carrierRow.Index, inverseRow.InverseCarrierIndex);
    }
}
