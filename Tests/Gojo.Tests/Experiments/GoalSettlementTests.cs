using Gojo.Core;
using Gojo.Equations;
using Gojo.Structures;

namespace Gojo.Tests.Experiments;

public sealed class GoalSettlementTests
{
    [Fact]
    public void OneImportedDrawPersistsUntilConnectedSupplyPaysIt()
    {
        var request = new Carrier();
        var account = new Carrier(request.UnitShape);
        var stock = new Carrier(request.UnitShape);
        var draw = new Relation(Relation.Segment(request, 0, 1), Relation.Segment(account, 0, -1));
        var supply = new Relation(Relation.Point(stock, 1), Relation.Point(account, 0));
        request.Add(3);
        stock.Add(2);

        void ObserveDraw()
        {
            Assert.True(draw.TryProject(request.ValueSlot, account, out var read));
            Assert.True(read.TryGetCarrierLocations(out var target, out var residue));
            Assert.Equal(new CarrierLocation(account, -3), target);
            Assert.Equal(new CarrierLocation(request, 0), residue);
        }

        ObserveDraw();
        Assert.Equal((0UL, 0L), (account.Value, account.Tension));
        Assert.True(account.TrySetFrom(draw, request));
        ObserveDraw();
        Assert.True(SettlementObservation.ObservedQuiet(account.Settle()));
        Assert.Equal((0UL, -3L), (account.Value, account.Tension));
        Assert.Equal((2UL, 0L), (stock.Value, stock.Tension));

        stock.Attach(supply);
        Assert.True(account.IsAttached(supply));
        Assert.Equal((0UL, 0L), (stock.Value, stock.Tension));
        Assert.Equal((0UL, -1L), (account.Value, account.Tension));
        stock.Add(1);
        ObserveDraw();
        Assert.Equal((0UL, 0L), (stock.Value, stock.Tension));
        Assert.Equal((0UL, 0L), (account.Value, account.Tension));
        Assert.Equal((3UL, 0L), (request.Value, request.Tension));

        // A second import of this same request is a second finite draw.
        Assert.True(account.TrySetFrom(draw, request));
        Assert.Equal((0UL, -3L), (account.Value, account.Tension));
    }

    [Fact]
    public void LiveGoalGapFollowsDeliveredWorkFromAnIndependentSupply()
    {
        var goal = new Carrier();
        var completed = new Carrier(goal.UnitShape);
        var gap = new Carrier(goal.UnitShape);
        var stock = new Carrier(goal.UnitShape);
        var operands = new Relation(goal.ValueSlot, completed.ValueSlot);
        var comparison = Equation.Difference(goal, completed, gap);
        var publication = goal.AttachLiveOutput(operands, comparison);
        var supply = new Relation(Relation.Point(stock, 1), Relation.Point(completed, 0));
        Assert.Same(operands, publication.Left);
        Assert.Same(comparison, publication.Right);

        goal.Add(3);
        stock.Add(2); // Available, on the far side of the disconnected route.
        Assert.True(SettlementObservation.ObservedQuiet(gap.Settle()));
        Assert.Equal((0UL, -3L), (gap.Value, gap.Tension));
        Assert.Equal((0UL, 0L), (completed.Value, completed.Tension));
        Assert.Equal((2UL, 0L), (stock.Value, stock.Tension));

        stock.Attach(supply);
        Assert.Equal((2UL, 0L), (completed.Value, completed.Tension));
        Assert.Equal((0UL, -1L), (gap.Value, gap.Tension));
        Assert.True(SettlementObservation.ObservedQuiet(gap.Settle()));
        Assert.Equal((0UL, -1L), (gap.Value, gap.Tension));

        stock.Add(1);
        Assert.Equal((0UL, 0L), (stock.Value, stock.Tension));
        Assert.Equal((3UL, 0L), (completed.Value, completed.Tension));
        Assert.Equal((0UL, 0L), (gap.Value, gap.Tension));
        Assert.Equal((3UL, 0L), (goal.Value, goal.Tension));
    }

    [Fact]
    public void ChangingTheTargetChangesThePublishedGapWhileSupplyFollowsItsOwnRoute()
    {
        var goal = new Carrier();
        var completed = new Carrier(goal.UnitShape);
        var report = new Carrier(goal.UnitShape);
        goal.AttachLiveOutput(new Relation(goal.ValueSlot, completed.ValueSlot),
            Equation.Difference(goal, completed, report));
        var stock = new Carrier(goal.UnitShape);
        stock.Add(2);
        stock.Attach(new Relation(Relation.Point(stock, 1), Relation.Point(completed, 0)));
        Assert.Equal(2UL, completed.Value);
        Assert.Equal((2UL, 0L), (report.Value, report.Tension));

        goal.Add(3);
        Assert.Equal((0UL, -1L), (report.Value, report.Tension));
        goal.Remove(1);
        Assert.Equal((0UL, 0L), (report.Value, report.Tension));
        Assert.Equal(2UL, completed.Value);
        Assert.Equal(0UL, stock.Value);
    }
}
