using Gojo.Core;
using Gojo.Structures;

namespace Gojo.Tests.Core;

public sealed class NumericFormatTests
{
    [Fact]
    public void DeepOrderedGroupsRoundTripWithoutRecursiveTraversal()
    {
        var carrier = new Carrier();
        var group = Relation.OrderedGroup(carrier, Enumerable.Range(0, 12000)
            .Select(i => (IElement)new CarrierLocation(carrier, i)).ToArray());
        var restored = Assert.IsType<Relation>(Assert.Single(GojoNumericFormat.Decode(GojoNumericFormat.Encode(group))));
        Assert.True(restored.TryReadOrderedGroup(out var members));
        Assert.Equal(12000, members.Length);
        Assert.Equal(Enumerable.Range(0, 12000).Select(i => (long)i), members.Cast<CarrierLocation>().Select(l => l.Ticks));
        Assert.Single(restored.Carriers().Distinct());
    }

    [Fact]
    public void NumericRoundTripPreservesRootOrderSlotsSitesUnitsAndIndependentWorldIdentity()
    {
        var mile = new Carrier(); var hour = new Carrier(); var shared = new TickRef(-7); var slot = new Placeholder();
        var standard = new Relation(Relation.Point(mile, 1), Relation.Point(hour, 1));
        var speed = new Carrier(QuotientUnitConformanceTests.Read([mile], [hour], standard));
        var root = Relation.OrderedGroup(new Carrier(), mile, hour, speed, mile.Inverse(), slot, slot.Dual(), slot,
            new CarrierLocation(mile, shared), new CarrierLocation(hour, shared), new CarrierLocation(mile, -7), standard);
        var bytes = GojoNumericFormat.Encode(root, shared, root);
        var captured = GojoNumericFormat.ReadSnapshot(bytes);
        Assert.NotEmpty(captured.Placeholders);
        Assert.Contains(captured.UnitShapes, unit => unit.Descriptors.Length != 0);
        var restored = GojoNumericFormat.Decode(bytes);
        Assert.Same(restored[0], restored[2]);
        Assert.NotSame(root, restored[0]);
        Assert.True(Assert.IsType<Relation>(restored[0]).TryReadOrderedGroup(out var items));
        var rm = Assert.IsType<Carrier>(items[0]); var rh = Assert.IsType<Carrier>(items[1]);
        var rs = Assert.IsType<Carrier>(items[2]);
        Assert.NotSame(mile, rm);
        Assert.Same(rm.Inverse(), items[3]);
        Assert.Same(items[4], items[6]);
        Assert.True(Assert.IsType<Placeholder>(items[4]).SameSlotAs(Assert.IsType<Placeholder>(items[5])));
        var a = Assert.IsType<CarrierLocation>(items[7]); var b = Assert.IsType<CarrierLocation>(items[8]);
        Assert.True(a.SameTickRefAs(b));
        Assert.Same(restored[1], a.Tick);
        Assert.False(a.SameTickRefAs(Assert.IsType<CarrierLocation>(items[9])));
        Assert.True(rs.UnitShape.TryReadQuotient(out var n, out var d, out var restoredStandard));
        Assert.Equal(rm.UnitShape, Assert.Single(n)); Assert.Equal(rh.UnitShape, Assert.Single(d));
        Assert.Equal(items[10], restoredStandard);
        var request = QuantityQuotient.Create(new Carrier(), new CarrierLocation(rm, 7), new CarrierLocation(rh, 2), rs, items[10]);
        Assert.True(QuantityQuotient.TryObserve(request, out _));
        shared.Set(99);
        Assert.Equal(-7, a.Ticks);
    }

    [Fact]
    public void RestoredAttachmentsAndValueSlotStillDriveLiveOutputAndCarry()
    {
        var input = new Carrier(); var observed = new Carrier(); var carried = new Carrier();
        input.AttachLiveOutput(input.ValueSlot, new Relation(Relation.Point(input, 1), Relation.Point(observed, 2)));
        input.Attach(new Relation(Relation.Point(input, 10), Relation.Point(carried, 0)));
        input.Add(7);
        var bytes = GojoNumericFormat.Encode(input, observed, carried, input.ValueSlot);
        var clone = GojoNumericFormat.Decode(bytes);
        var ri = Assert.IsType<Carrier>(clone[0]); var ro = Assert.IsType<Carrier>(clone[1]); var rc = Assert.IsType<Carrier>(clone[2]);
        Assert.True(ri.ValueSlot.SameTickRefAs(Assert.IsType<CarrierLocation>(clone[3])));
        Assert.Equal(input.Value, ri.Value); Assert.Equal(observed.Value, ro.Value);
        input.Add(8); ri.Add(8);
        Assert.Equal(input.Value, ri.Value); Assert.Equal(observed.Value, ro.Value); Assert.Equal(carried.Value, rc.Value);
        Assert.Equal(5UL, ri.Value); Assert.Equal(10UL, ro.Value); Assert.Equal(1UL, rc.Value);
        var again = GojoNumericFormat.Decode(GojoNumericFormat.Encode(clone));
        Assert.IsType<Carrier>(again[0]).Add(5); ri.Add(5);
        Assert.Equal(ri.Value, Assert.IsType<Carrier>(again[0]).Value);
        Assert.Equal(rc.Value, Assert.IsType<Carrier>(again[2]).Value);
    }

    [Fact]
    public void RestorationDoesNotSettlePendingBorrowOrActivateDetachedRecipes()
    {
        var fine = new Carrier(); var coarse = new Carrier(UnitShape.ResolutionOf(fine.UnitShape, 10));
        var calibration = new Relation(Relation.Segment(coarse, 0, 1), Relation.Segment(fine, 0, 10));
        coarse.Add(2); fine.Remove(3);
        var recipe = new Relation(Relation.Point(fine, 10), Relation.Point(coarse, 0));
        var roots = GojoNumericFormat.Decode(GojoNumericFormat.Encode(fine, coarse, calibration, recipe));
        var rf = Assert.IsType<Carrier>(roots[0]); var rc = Assert.IsType<Carrier>(roots[1]);
        Assert.Equal(-3, rf.Tension); Assert.Equal(2UL, rc.Value);
        Assert.True(RefinementTransfer.TryBorrow(Assert.IsType<Relation>(roots[2]), out _));
        Assert.Equal(7UL, rf.Value); Assert.Equal(1UL, rc.Value);
        rf.Add(10);
        Assert.Equal(17UL, rf.Value); // Detached recipe was not activated during restoration.
        rf.Attach(Assert.IsType<Relation>(roots[3]));
        Assert.Equal(7UL, rf.Value); Assert.Equal(2UL, rc.Value);
    }

    [Fact]
    public void RelationCyclesCanBeTransportedAndInspectedWithoutFlatteningThem()
    {
        var carrier = new Carrier();
        var relation = new Relation(carrier, carrier);
        var graph = GojoEncodedSnapshot.CaptureGraph(relation);
        var index = graph.Root.Index;
        var rows = graph.Relations.ToArray();
        rows[index] = rows[index] with { Left = new(GojoEncodedElementKind.Relation, index) };
        var cyclic = Assert.IsType<Relation>(Assert.Single(GojoNumericFormat.Restore(graph with { Relations = rows })));
        Assert.Same(cyclic, cyclic.Left);
        Assert.Single(cyclic.Carriers());
        Assert.False(cyclic.TryReadOrderedGroup(out _));
        var again = Assert.IsType<Relation>(Assert.Single(GojoNumericFormat.Decode(GojoNumericFormat.Encode(cyclic))));
        Assert.Same(again, again.Left);
    }

    [Fact]
    public void InvalidPacketsAndMutableAliasesOfUnitInternalsAreRefused()
    {
        var graph = GojoEncodedSnapshot.CaptureGraph(new Carrier());
        var bytes = GojoNumericFormat.Encode(graph);
        Assert.Throws<InvalidDataException>(() => GojoNumericFormat.Decode(bytes[..^1]));
        Assert.Throws<InvalidDataException>(() => GojoNumericFormat.Decode([.. bytes, 0]));
        var version = bytes.ToArray(); version[4] = 99;
        Assert.Throws<InvalidDataException>(() => GojoNumericFormat.Decode(version));
        var length = bytes.ToArray(); Array.Fill(length, (byte)255, 8, 4);
        Assert.Throws<InvalidDataException>(() => GojoNumericFormat.Decode(length));
        Assert.Throws<InvalidDataException>(() => GojoNumericFormat.Restore(graph with { Roots = [new(GojoEncodedElementKind.Carrier, int.MaxValue)] }));
        Assert.Throws<InvalidDataException>(() => GojoNumericFormat.Restore(graph with { Roots = [graph.UnitShapes[0].Surface] }));
        Assert.Throws<InvalidDataException>(() => GojoNumericFormat.Encode(GojoEncodedSnapshot.Capture(new Carrier())));
        var c = graph.Carriers.ToArray(); c[0] = c[0] with { Value = ulong.MaxValue };
        Assert.Throws<InvalidDataException>(() => GojoNumericFormat.Restore(graph with { Carriers = c }));
        Assert.Throws<ArgumentException>(() => UnitShape.DescribedAtom(new TickRef(3)));
        var basis = new Carrier();
        var opposed = GojoEncodedSnapshot.CaptureGraph(basis, basis.Inverse());
        var opposedRows = opposed.Carriers.ToArray();
        opposedRows[1] = opposedRows[1] with { UnitShapeIndex = opposedRows[0].UnitShapeIndex };
        Assert.Throws<InvalidDataException>(() => GojoNumericFormat.Restore(opposed with { Carriers = opposedRows }));
    }
}
