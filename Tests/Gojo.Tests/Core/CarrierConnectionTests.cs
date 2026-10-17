using Gojo.Core;

namespace Gojo.Tests.Core;

public sealed class CarrierConnectionTests
{
    [Fact]
    public void AttachmentInspectionIsReadOnlyLiveAndIdentityBased()
    {
        var source = new Carrier();
        var target = new Carrier();
        var relation = new Relation(Relation.Point(source, 1), Relation.Point(target, 2));
        var lookalike = new Relation(relation.Left, relation.Right);
        var view = source.AttachedRelations;

        Assert.Empty(view);
        source.Attach(relation);
        source.Attach(relation);

        Assert.Same(relation, Assert.Single(view));
        Assert.True(source.IsAttached(relation));
        Assert.True(target.IsAttached(relation));
        Assert.False(source.IsAttached(lookalike));
        Assert.Equal(relation, lookalike);
        var guarded = Assert.IsAssignableFrom<IList<Relation>>(view);
        Assert.Throws<NotSupportedException>(() => guarded.Add(lookalike));

        Assert.True(source.Detach(relation));
        Assert.Empty(view);
    }

    [Fact]
    public void WithdrawalRemovesOnlyExactIdentityFromAllReferencedOwners()
    {
        var source = new Carrier();
        var target = new Carrier();
        var relation = new Relation(Relation.Point(source, 1), Relation.Point(target, 2));
        var other = new Relation(relation.Left, relation.Right);
        source.Attach(relation);
        source.Attach(other);

        Assert.True(target.Detach(relation));
        Assert.False(source.IsAttached(relation));
        Assert.False(target.IsAttached(relation));
        Assert.Same(other, Assert.Single(source.AttachedRelations));
        Assert.Same(other, Assert.Single(target.AttachedRelations));
        Assert.False(source.Detach(relation));
        Assert.False(source.Detach(new Relation(other.Left, other.Right)));
        Assert.True(source.IsAttached(other));
    }

    [Fact]
    public void WithdrawalDoesNotSettleOrEraseHeldPressure()
    {
        var source = new Carrier();
        var boundary = new Relation(Relation.Point(source, 10), Relation.Point(source, 0));
        source.Attach(boundary);
        source.Add(23);
        var valueSite = source.ValueSlot;
        var pressureSite = source.LiveTensionSlot;
        Assert.Equal(3UL, source.Value);
        Assert.Equal(20, source.Tension);

        Assert.True(source.Detach(boundary));

        Assert.Equal(3UL, source.Value);
        Assert.Equal(20, source.Tension);
        Assert.True(valueSite.SameTickRefAs(source.ValueSlot));
        Assert.True(pressureSite.SameTickRefAs(source.LiveTensionSlot));
        Assert.False(source.Detach(boundary));
        Assert.Equal(20, source.Tension);
    }

    [Fact]
    public void WithdrawnRouteStopsNewTransferWithoutRetractingMaterialAndCanResume()
    {
        var source = new Carrier();
        var target = new Carrier();
        var route = new Relation(Relation.Point(source, 10), Relation.Point(target, 0));
        source.Attach(route);
        source.Add(23);
        Assert.Equal(3UL, source.Value);
        Assert.Equal(2UL, target.Value);

        Assert.True(source.Detach(route));
        Assert.Equal(2UL, target.Value);
        source.Add(20);
        Assert.Equal(23UL, source.Value);
        Assert.Equal(2UL, target.Value);

        source.Attach(route);

        Assert.Equal(3UL, source.Value);
        Assert.Equal(4UL, target.Value);
        Assert.Equal(0, source.Tension);
        Assert.Same(route, Assert.Single(source.AttachedRelations));
        Assert.Same(route, Assert.Single(target.AttachedRelations));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(-1)]
    public void WithdrawnLivePublicationRetainsLastLevelOrPressureUntilExplicitResume(long sign)
    {
        var source = new Carrier();
        var target = new Carrier();
        var publication = source.AttachLiveOutput(source.ValueSlot,
            new Relation(Relation.Segment(source, 0, 1), Relation.Segment(target, 0, sign)));
        source.Set(3);
        Assert.Equal(sign > 0 ? 3UL : 0UL, target.Value);
        Assert.Equal(sign > 0 ? 0 : -3, target.Tension);

        Assert.True(target.Detach(publication));
        source.Set(5);
        target.Settle();

        Assert.Equal(sign > 0 ? 3UL : 0UL, target.Value);
        Assert.Equal(sign > 0 ? 0 : -3, target.Tension);
        Assert.False(source.IsAttached(publication));
        Assert.False(target.IsAttached(publication));

        source.Attach(publication);

        Assert.Equal(sign > 0 ? 5UL : 0UL, target.Value);
        Assert.Equal(sign > 0 ? 0 : -5, target.Tension);
    }

    [Fact]
    public void LocalDiscoveryWithdrawsReachableArchiveOwnersWithoutInventingReverseOwners()
    {
        var source = new Carrier();
        var target = new Carrier();
        var reachableOwner = new Carrier();
        var remoteOwner = new Carrier();
        var relation = new Relation(Relation.Point(source, 1), Relation.Point(target, 2));
        var bridge = new Relation(Relation.Point(source, 1), Relation.Point(reachableOwner, 2));
        source.Attach(relation);
        source.Attach(bridge);
        var graph = GojoEncodedSnapshot.CaptureGraph(source, target, reachableOwner, remoteOwner, relation, bridge);
        var relationIndex = graph.Relations.Single(row => row.Id == relation.Id).Index;
        foreach (var owner in new[] { reachableOwner, remoteOwner })
        {
            var row = graph.Carriers.Single(row => row.Id == owner.Id);
            graph.Carriers[row.Index] = row with
            {
                AttachedRelationIndexes = [.. row.AttachedRelationIndexes!, relationIndex]
            };
        }
        var restored = GojoNumericFormat.Restore(graph);
        var rs = Assert.IsType<Carrier>(restored[0]);
        var rt = Assert.IsType<Carrier>(restored[1]);
        var reachable = Assert.IsType<Carrier>(restored[2]);
        var remote = Assert.IsType<Carrier>(restored[3]);
        var rr = Assert.IsType<Relation>(restored[4]);
        var rb = Assert.IsType<Relation>(restored[5]);

        Assert.True(rs.Detach(rr));

        Assert.False(rs.IsAttached(rr));
        Assert.False(rt.IsAttached(rr));
        Assert.False(reachable.IsAttached(rr));
        Assert.True(remote.IsAttached(rr)); // No global reverse-owner registry.
        Assert.True(rs.IsAttached(rb));
        Assert.True(reachable.IsAttached(rb));
        Assert.True(remote.Detach(rr));
        Assert.False(remote.IsAttached(rr));
    }

    [Fact]
    public void NumericTransportPreservesWithdrawalWithoutLosingReusableRelation()
    {
        var source = new Carrier();
        var target = new Carrier();
        var relation = source.AttachLiveOutput(source.ValueSlot,
            new Relation(Relation.Segment(source, 0, 1), Relation.Segment(target, 0, 2)));
        source.Set(4);
        source.Detach(relation);
        var restored = GojoNumericFormat.Decode(GojoNumericFormat.Encode(source, target, relation));
        var rs = Assert.IsType<Carrier>(restored[0]);
        var rt = Assert.IsType<Carrier>(restored[1]);
        var rr = Assert.IsType<Relation>(restored[2]);

        Assert.Empty(rs.AttachedRelations);
        Assert.Empty(rt.AttachedRelations);
        rs.Set(7);
        Assert.Equal(8UL, rt.Value);
        rs.Attach(rr);
        Assert.Equal(14UL, rt.Value);
        Assert.Same(rr, Assert.Single(rs.AttachedRelations));
    }
}
