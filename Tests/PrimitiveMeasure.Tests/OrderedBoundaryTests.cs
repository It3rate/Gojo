using Gojo.Experiments.PrimitiveMeasure;

namespace Gojo.Experiments.PrimitiveMeasure.Tests;

public sealed class OrderedBoundaryTests
{
    [Fact]
    public void Triangle_cancels_shared_sites_without_any_magnitude_or_geometry()
    {
        var s = new OrderedBoundary(3, [[0, 1], [1, 2], [2, 0]], [0, 2, 4]);
        Assert.Equal(new byte[] { 0x94, 0x02 }, s.EdgeBytes());
        Assert.Equal(new byte[] { 0x10, 0x01 }, s.UseBytes());
        var read = s.Read();
        Assert.True(read.Balanced); Assert.True(read.OrderedClosed); Assert.Equal(1, read.VisitedComponents);
        Assert.Equal(0, s.PatchUseCount);
    }

    [Fact]
    public void Open_chain_has_two_signed_endpoints_and_coincidence_cannot_bind_them()
    {
        var open = new OrderedBoundary(4, [[0, 1], [1, 2], [2, 3]], [0, 2, 4]);
        var read = open.Read();
        Assert.False(read.Balanced); Assert.Equal(-1, read.Balance[0]); Assert.Equal(1, read.Balance[3]);
        Assert.Equal(new[] { 0, 3 }, read.OddSites);
    }

    [Fact]
    public void Parity_cannot_detect_reversal_or_double_traversal_residue()
    {
        var reversed = new OrderedBoundary(3, [[0, 1], [1, 2], [2, 0]], [0, 3, 4]).Read();
        Assert.True(reversed.EvenParity); Assert.False(reversed.Balanced); Assert.False(reversed.OrderedClosed);
        Assert.Equal(2, reversed.Balance[1]); Assert.Equal(-2, reversed.Balance[2]);
        var doubled = new OrderedBoundary(2, [[0, 1]], [0, 0]).Read();
        Assert.True(doubled.EvenParity); Assert.False(doubled.Balanced);
    }

    [Fact]
    public void Xor_of_addresses_can_vanish_without_any_canceled_site()
    {
        Assert.Equal(0, 0 ^ 1 ^ 2 ^ 3);
        var read = new OrderedBoundary(4, [[0, 1], [2, 3]], [0, 2]).Read();
        Assert.False(read.EvenParity); Assert.False(read.Balanced); Assert.Equal(4, read.OddSites.Length);
    }

    [Fact]
    public void Two_loops_are_balanced_but_not_one_ordered_closed_walk()
    {
        var s = new OrderedBoundary(4, [[0, 1], [1, 0], [2, 3], [3, 2]], [0, 2, 4, 6]);
        var r = s.Read(); Assert.True(r.Balanced); Assert.Equal(2, r.VisitedComponents); Assert.False(r.OrderedClosed);
    }

    [Fact]
    public void Self_loop_and_retracing_remain_present_after_boundary_cancellation()
    {
        var self = new OrderedBoundary(1, [[0, 0]], [0]);
        Assert.True(self.Read().OrderedClosed); Assert.Equal(1, self.EdgeCount); Assert.Equal(1, self.UseCount);
        var retrace = new OrderedBoundary(2, [[0, 1]], [0, 1]);
        Assert.True(retrace.Read().OrderedClosed); Assert.Equal(2, retrace.UseCount);
    }

    [Fact]
    public void Patch_has_explicit_boundary_references_and_can_fail_independently()
    {
        var good = new OrderedBoundary(3, [[0, 1], [1, 2], [2, 0]], [0, 2, 4], [0, 1, 2]);
        Assert.Equal(new byte[] { 0x24 }, good.PatchBytes()); Assert.True(good.Read(patchBoundary: true).Balanced);
        var bad = new OrderedBoundary(3, [[0, 1], [1, 2], [2, 0]], [0, 2, 4], [0, 1]);
        Assert.True(bad.Read().Balanced); Assert.False(bad.Read(patchBoundary: true).Balanced);
    }

    [Fact]
    public void Budget_does_not_turn_a_prefix_or_empty_read_into_a_whole_verdict()
    {
        var s = BoundaryScaling.Ring(4096); var partial = s.Read(1024);
        Assert.False(partial.Complete); Assert.Null(partial.Balanced); Assert.Null(partial.OrderedClosed);
        Assert.Equal(1024, partial.Visited); Assert.Equal(-1, partial.Balance[0]); Assert.Equal(1, partial.Balance[1024]);
        Assert.True(s.Read().OrderedClosed); Assert.Null(s.Read(0).Balanced);
        Assert.Equal((4096 * 24 + 7) / 8 + (4096 * 13 + 7) / 8, s.PayloadBytes);
    }

    [Fact]
    public void Strict_order_distinguishes_known_contradiction_from_uncertain_or_absent_claim()
    {
        var s = new OrderedBoundary(3, [[0, 1], [1, 2], [2, 0]], [0, 2, 4]);
        Assert.Equal("Contradiction", StrictOrderRead.Evaluate(s, [2, 2, 2]));
        Assert.Equal("Unresolved", StrictOrderRead.Evaluate(s, [2, 2, 3]));
        Assert.Equal("Unresolved", StrictOrderRead.Evaluate(s, [2, 2, 0]));
        Assert.Equal("No strict-order cycle", StrictOrderRead.Evaluate(s, [2, 2, 1]));
        var reverse = new OrderedBoundary(3, [[0, 1], [1, 2], [2, 0]], [0, 3, 4]);
        Assert.Equal("No strict-order cycle", StrictOrderRead.Evaluate(reverse, [2, 2, 2]));
    }
}
