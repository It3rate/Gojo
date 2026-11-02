using Gojo.Experiments.PrimitiveMeasure;

namespace Gojo.Experiments.PrimitiveMeasure.Tests;

public class PathExchangeTests
{
    [Fact]
    public void The_host_claim_can_guard_an_existing_primitive_passage()
    {
        var a = PathExchangeExperiment.MakePath(4, true);
        var b = PathExchangeExperiment.MakePath(4, false);
        var loop = PathExchangeExperiment.Join(a, b);
        var consumer = new PulseCircuit(2);
        var waiting = consumer.State(true);
        var ready = consumer.State();
        var guard = consumer.Guard();
        consumer.Bind(guard, loop);
        consumer.Link(consumer.AddTable(PassageLaw.Controlled()), waiting, guard, ready);
        a.Advance(1);
        b.Advance(4);
        b.ReverseStep(2);
        Assert.False(consumer.Pulse().Committed);
        Assert.Equal(waiting, consumer.Active);
        b.Advance(4);
        Assert.False(consumer.Pulse().Moved);
        b.ReverseStep(2);
        b.Advance(4);
        Assert.True(consumer.Pulse().Moved);
        Assert.Equal(ready, consumer.Active);
    }

    [Theory]
    [InlineData(false, false, 4, 3)]
    [InlineData(true, true, 1, 2)]
    [InlineData(false, true, 4, 2)]
    public void Only_the_edited_owner_loses_its_answer(bool packedA, bool packedB, int coldA, int editedB)
    {
        var a = PathExchangeExperiment.MakePath(4, packedA);
        var b = PathExchangeExperiment.MakePath(4, packedB);
        var loop = PathExchangeExperiment.Join(a, b);
        Assert.Equal(3, loop.ReadSupport());
        Assert.Equal(coldA, a.Advance(4));
        b.Advance(4);
        Assert.Equal(2, loop.ReadSupport());
        Assert.Equal(new byte[] { 0x39 }, loop.SeamBytes());
        b.ReverseStep(2);
        Assert.Equal(2, a.Passage.ReadSupport());
        Assert.Equal(3, loop.ReadSupport());
        Assert.Equal(0, b.Advance(0));
        Assert.Equal(editedB - 1, b.Advance(editedB - 1));
        Assert.Equal(3, loop.ReadSupport());
        Assert.Equal(1, b.Advance(1));
        Assert.Equal(1, loop.ReadSupport());
        Assert.Equal(2, b.FailedStep);
        Assert.Equal(0, a.Advance(10));
        Assert.Equal(coldA, a.Inspections);
        b.ReverseStep(2);
        Assert.Equal(1, b.RunCount);
        Assert.Equal(3, loop.ReadSupport());
        b.Advance(4);
        Assert.Equal(2, loop.ReadSupport());
    }

    [Fact]
    public void Stale_partial_scan_is_discarded_even_when_next_edit_precedes_cursor()
    {
        var b = PathExchangeExperiment.MakePath(5, false);
        b.Advance(4);
        b.ReverseStep(0);
        b.Advance(1);
        Assert.Equal(1, b.Passage.ReadSupport());
        Assert.Equal(0, b.FailedStep);
    }

    [Fact]
    public void Private_index_equality_does_not_supply_a_public_seam()
    {
        var a = PathExchangeExperiment.MakePath(2, true);
        var b = PathExchangeExperiment.MakePath(2, false);
        a.Advance(1); b.Advance(2);
        var loop = PathExchangeExperiment.Join(a, b, false);
        Assert.Equal(1, loop.ReadSupport());
        loop.Join(3, 2);
        Assert.Equal(1, loop.ReadSupport());
    }

    [Fact]
    public void Packing_preserves_interior_attachments_observations_and_public_port_identity()
    {
        var b = PathExchangeExperiment.MakePath(4, false);
        var port = b.Passage;
        b.Advance(4);
        b.ObserveSite(2, 3);
        b.Attach(2, 5);
        Assert.Equal(2, port.ReadSupport());
        var values = b.ValueBytes();
        Assert.True(b.TryPackChain(out var checkedPairs));
        Assert.Equal(4, checkedPairs);
        Assert.Equal(new byte[] { 0x88, 0x46 }, b.TopologyBytes());
        Assert.Equal(new[] { 2, 5 }, b.Branch(0));
        Assert.Equal(values, b.ValueBytes());
        Assert.Same(port, b.Passage);
        Assert.Equal(0, b.Advance(4));
        Assert.Equal(2, port.ReadSupport());
        var malformed = new LocalPath(4, 0, 3, 2, [0, 1], [2, 3]);
        malformed.Advance(2);
        var bytes = malformed.TopologyBytes();
        Assert.False(malformed.TryPackChain(out checkedPairs));
        Assert.Equal(bytes, malformed.TopologyBytes());
        Assert.Equal(1, malformed.Passage.ReadSupport());
    }

    [Theory]
    [InlineData(0, 4, 1)]
    [InlineData(0, 0, 2)]
    public void Chain_can_revisit_sites_and_its_advertised_ends_are_checked(int inlet, int outlet, byte expected)
    {
        var ring = new LocalPath(5, inlet, outlet, 1, [0], [2], [0], [3], [0]);
        Assert.Equal(1, ring.Advance(1));
        Assert.Equal(expected, ring.Passage.ReadSupport());
    }

    [Fact]
    public void Spliced_ranges_agree_with_independent_step_walk_after_multiple_edits()
    {
        foreach (var packed in new[] { false, true })
        {
            var b = PathExchangeExperiment.MakePath(6, packed);
            var flipped = new bool[6];
            foreach (var edge in new[] { 0, 1, 4, 2, 5, 3, 1, 4, 0, 2, 5, 3 })
            {
                flipped[edge] = !flipped[edge];
                b.ReverseStep(edge);
                while (b.Passage.ReadSupport() == 3) b.Advance(1);
                var expected = Array.IndexOf(flipped, true);
                Assert.Equal(expected < 0, b.Walk().Continuous);
                Assert.Equal(expected < 0 ? 2 : 1, b.Passage.ReadSupport());
                Assert.Equal(expected < 0 ? (int?)null : expected, b.FailedStep);
            }
        }
    }
}
