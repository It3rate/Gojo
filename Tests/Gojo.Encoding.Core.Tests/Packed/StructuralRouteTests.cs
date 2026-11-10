using Gojo.Encoding.Core.Packed;

namespace Gojo.Encoding.Core.Tests.Packed;

public sealed class StructuralRouteTests
{
    [Fact]
    public void Fixed_faces_do_not_establish_connections_and_owned_faces_do_not_remove_them()
    {
        var loose = StructuralRoute.Pairs(5, [[0, 1], [2, 3], [3, 4], [4, 0]]);
        var chain = StructuralRoute.Chain(5, [0, 1, 2, 3, 0], 2, 0, [1, 7, 2, 4]);
        Assert.Equal(1, loose.WordWidth);
        Assert.Equal(3, chain.WordWidth);
        Assert.Equal(new StructuralRouteRead(false, false, 4), loose.ReadAll());
        Assert.Equal(new StructuralRouteRead(true, true, 4), chain.ReadAll());
        Assert.Equal(2, loose.Inspect(4));
        Assert.Equal((byte)1, loose.ContinuitySupport);
        Assert.Equal(1, chain.Inspect(4));
        Assert.Equal((byte)2, chain.ClosureSupport);
    }

    [Fact]
    public void Same_cycle_has_equal_truth_but_different_storage_and_inspection_work()
    {
        var pairs = StructuralRoute.Pairs(5, [[0, 1], [1, 2], [2, 3], [3, 0]]);
        var chain = StructuralRoute.Chain(5, [0, 1, 2, 3, 0]);
        var cycle = StructuralRoute.Cycle(5, [0, 1, 2, 3]);
        Assert.Equal(pairs.ReadAll(), chain.ReadAll());
        Assert.Equal(pairs.ReadAll(), cycle.ReadAll());
        Assert.Equal(new long[] { 28, 19, 16 }, new[] { pairs.PayloadBits, chain.PayloadBits, cycle.PayloadBits });
        Assert.Equal(4, pairs.Inspect(10));
        Assert.Equal(1, chain.Inspect(10));
        Assert.Equal(1, cycle.Inspect(10));
        Assert.Equal(new long[] { 4, 2, 0 }, new[] { pairs.RowsRead, chain.RowsRead, cycle.RowsRead });
        foreach (var route in new[] { pairs, chain, cycle })
        {
            Assert.Equal((byte)2, route.ContinuitySupport);
            Assert.Equal((byte)2, route.ClosureSupport);
            Assert.Equal(0, route.Inspect(10));
        }
    }

    [Fact]
    public void Zero_budget_and_diagnostic_walk_do_not_settle_operational_claims()
    {
        var route = StructuralRoute.Cycle(4, [0, 1, 2, 3]);
        Assert.True(route.ReadAll().Closed);
        Assert.Equal(0, route.Inspect(0));
        Assert.Equal((byte)3, route.Continuity.ReadSupport());
        Assert.Equal((byte)3, route.Closure.ReadSupport());
        Assert.Equal(0, route.Inspections);
        Assert.Throws<ArgumentOutOfRangeException>(() => route.Inspect(-1));
    }

    [Fact]
    public void Prefix_is_not_a_whole_claim_and_same_outer_ends_do_not_hide_a_broken_join()
    {
        var route = StructuralRoute.Pairs(4, [[0, 1], [1, 2], [3, 0]]);
        Assert.Equal(2, route.Inspect(2));
        Assert.Equal((byte)3, route.ClosureSupport);
        Assert.Equal(1, route.Inspect(1));
        Assert.Equal((byte)1, route.ContinuitySupport);
        Assert.Equal((byte)1, route.ClosureSupport);
        Assert.Equal(2, route.FailedStep);
        Assert.False(route.ReadAll().Closed);
    }

    [Fact]
    public void Retarget_invalidates_a_previously_checked_prefix_and_completed_claim()
    {
        var route = StructuralRoute.Pairs(5, [[0, 1], [1, 2], [2, 3], [3, 0]]);
        route.Inspect(3);
        var port = route.Closure;
        var edit = route.TryRetargetEndpoint(0, 1, 4);
        Assert.True(edit.Applied);
        Assert.Equal(4, edit.RowsRead);
        Assert.Equal(4, edit.RowsWritten);
        Assert.Equal(new[] { 0 }, edit.AffectedEdges);
        Assert.Equal(1, route.TopologyRevision);
        Assert.Equal((byte)3, port.ReadSupport());
        Assert.Equal(1, route.Inspect(1));
        Assert.Equal((byte)3, route.ContinuitySupport);
        Assert.Equal(1, route.Inspect(1));
        Assert.Equal((byte)1, route.ContinuitySupport);
        route.TryRetargetEndpoint(0, 1, 1);
        Assert.Equal((byte)3, port.ReadSupport());
        route.Inspect(4);
        Assert.Equal((byte)2, port.ReadSupport());
        Assert.Same(port, route.Closure);
    }

    [Fact]
    public void Observed_values_do_not_mutate_topology_or_invalidate_its_witness()
    {
        var route = StructuralRoute.Cycle(4, [0, 1, 2, 3], 2, 0);
        route.Inspect(1);
        var topology = route.TopologyBytes();
        for (byte word = 0; word < 8; word++)
        {
            route.SetWord(0, word);
            Assert.Equal(word, route.Word(0));
            Assert.Equal(topology, route.TopologyBytes());
            Assert.Equal((byte)2, route.ClosureSupport);
            Assert.Equal(0, route.TopologyRevision);
        }
        Assert.True(route.SetWord(0, 0));
        Assert.False(route.SetWord(0, 0));
        Assert.True(route.ReadAll().Closed);
        Assert.Equal(9, route.WordWrites);
    }

    [Theory]
    [InlineData(0, 0, 1, 3)]
    [InlineData(1, 1, 7, 5)]
    [InlineData(0, 2, 1, 5)]
    public void Inherited_bits_cannot_be_written_through_value_slot(int o, int d, byte initial, byte rejected)
    {
        var route = StructuralRoute.Cycle(1, [0], o, d, [initial]);
        var bytes = route.Encode();
        Assert.Throws<ArgumentException>(() => route.SetWord(0, rejected));
        Assert.Equal(bytes, route.Encode());
        Assert.Equal(0, route.WordWrites);
    }

    [Fact]
    public void A_cycle_has_no_independent_final_endpoint_until_explicit_expansion()
    {
        var route = StructuralRoute.Cycle(5, [0, 1, 2, 3]);
        route.Inspect(1);
        var before = route.Encode();
        var rejected = route.TryRetargetEndpoint(3, 1, 4);
        Assert.False(rejected.Applied);
        Assert.Empty(rejected.AffectedEdges);
        Assert.Equal(0, rejected.RowsRead + rejected.RowsWritten);
        Assert.Equal(before, route.Encode());
        Assert.Equal((byte)2, route.ClosureSupport);
        var expansion = route.Unpack();
        Assert.True(expansion.Applied);
        Assert.True(expansion.LayoutChanged);
        Assert.Equal(4, expansion.RowsRead);
        Assert.Equal(4, expansion.RowsWritten);
        Assert.Empty(expansion.AffectedEdges); // Representation changed, actual edges did not.
        Assert.Equal(0, route.Layout);
        Assert.True(route.ReadAll().Closed);
        Assert.Equal((byte)3, route.ClosureSupport);
        route.TryRetargetEndpoint(3, 1, 4);
        route.Inspect(4);
        Assert.Equal((byte)2, route.ContinuitySupport);
        Assert.Equal((byte)1, route.ClosureSupport);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Retargeting_a_shared_vertex_moves_both_actual_adjacent_uses(bool cyclic)
    {
        var route = cyclic ? StructuralRoute.Cycle(5, [0, 1, 2, 3]) : StructuralRoute.Chain(5, [0, 1, 2, 3]);
        var edit = route.RetargetVertex(1, 4);
        Assert.True(edit.Applied);
        Assert.Equal(new[] { 0, 1 }, edit.AffectedEdges);
        Assert.Equal(new[] { 0, 4 }, route.Edge(0));
        Assert.Equal(new[] { 4, 2 }, route.Edge(1));
        Assert.True(route.ReadAll().Continuous);
        Assert.Equal(cyclic, route.ReadAll().Closed);
    }

    [Fact]
    public void Cyclic_first_vertex_is_also_the_last_successor_and_repeat_sites_are_not_extra_aliases()
    {
        var route = StructuralRoute.Cycle(4, [0, 1, 0, 2]);
        var edit = route.RetargetVertex(0, 3);
        Assert.Equal(new[] { 0, 3 }, edit.AffectedEdges);
        Assert.Equal(new[] { 2, 3 }, route.Edge(3));
        Assert.Equal(new[] { 3, 1 }, route.Edge(0));
        Assert.Equal(new[] { 1, 0 }, route.Edge(1));
        Assert.Equal(new[] { 0, 2 }, route.Edge(2));
        Assert.True(route.ReadAll().Closed);
    }

    [Fact]
    public void Shared_endpoint_edits_can_open_a_chain_but_not_a_positional_cycle()
    {
        var chain = StructuralRoute.Chain(5, [0, 1, 2, 3, 0]);
        var cycle = StructuralRoute.Cycle(5, [0, 1, 2, 3]);
        chain.RetargetVertex(4, 4);
        cycle.RetargetVertex(0, 4);
        Assert.True(chain.ReadAll().Continuous);
        Assert.False(chain.ReadAll().Closed);
        Assert.True(cycle.ReadAll().Closed);
    }

    [Fact]
    public void Unpacking_preserves_every_occurrence_and_observation_including_revisited_sites()
    {
        foreach (var route in new[]
        {
            StructuralRoute.Chain(3, [0, 1, 0, 2, 0], 2, 0, [7, 0, 5, 2]),
            StructuralRoute.Cycle(3, [0, 1, 0, 2], 2, 0, [7, 0, 5, 2])
        })
        {
            var edges = Enumerable.Range(0, route.StepCount).Select(route.Edge).ToArray();
            var words = route.WordBytes();
            var truth = route.ReadAll();
            route.Unpack();
            Assert.Equal(truth, route.ReadAll());
            Assert.Equal(words, route.WordBytes());
            for (var i = 0; i < edges.Length; i++) Assert.Equal(edges[i], route.Edge(i));
            Assert.False(route.Unpack().Applied);
            Assert.False(route.RetargetVertex(0, 1).Applied);
        }
    }

    [Fact]
    public void Single_site_self_loop_and_out_and_back_are_closed_without_geometric_claims()
    {
        foreach (var route in new[]
        {
            StructuralRoute.Cycle(1, [0]),
            StructuralRoute.Pairs(1, [[0, 0]]),
            StructuralRoute.Chain(2, [0, 1, 0])
        })
        {
            Assert.True(route.ReadAll().Closed);
            route.Inspect(5);
            Assert.Equal((byte)2, route.ClosureSupport);
        }
        var loop = StructuralRoute.Cycle(2, [0]);
        Assert.Equal(new[] { 0 }, loop.RetargetVertex(0, 1).AffectedEdges);
        Assert.Equal(new[] { 1, 1 }, loop.Edge(0));
    }

    [Fact]
    public void Noop_edits_do_not_withdraw_current_witnesses_or_write_rows()
    {
        var cycle = StructuralRoute.Cycle(4, [0, 1, 2, 3]);
        var pairs = StructuralRoute.Pairs(4, [[0, 1], [1, 2], [2, 3], [3, 0]]);
        cycle.Inspect(4); pairs.Inspect(4);
        Assert.False(cycle.RetargetVertex(1, 1).Applied);
        Assert.False(pairs.TryRetargetEndpoint(1, 1, 2).Applied);
        foreach (var route in new[] { cycle, pairs })
        {
            Assert.Equal(0, route.RowsWritten);
            Assert.Equal(0, route.TopologyRevision);
            Assert.Equal((byte)2, route.ClosureSupport);
        }
    }

    [Fact]
    public void Roundtrip_preserves_layout_local_aliases_and_owned_bits_but_not_cached_knowledge()
    {
        foreach (var o in new[] { 0, 1, 2 })
            foreach (var d in o == 2 ? new[] { 0 } : new[] { 0, 1, 2 })
                foreach (var layout in new[] { 0, 1, 2 })
                {
                    var route = layout switch
                    {
                        0 => StructuralRoute.Pairs(4, [[0, 1], [1, 0], [0, 3]], o, d),
                        1 => StructuralRoute.Chain(4, [0, 1, 0, 3], o, d),
                        _ => StructuralRoute.Cycle(4, [0, 1, 0], o, d)
                    };
                    route.SetWord(1, (byte)(route.Word(1) & ~1));
                    route.Inspect(10);
                    var encoded = route.Encode();
                    var restored = StructuralRoute.Decode(encoded);
                    Assert.Equal(encoded, restored.Encode());
                    Assert.Equal(route.ReadAll(), restored.ReadAll());
                    Assert.Equal(route.Layout, restored.Layout);
                    Assert.Equal(route.WordWidth, restored.WordWidth);
                    Assert.Equal(route.TopologyBytes(), restored.TopologyBytes());
                    Assert.Equal((byte)3, restored.ClosureSupport);
                    Assert.Equal(0, restored.Inspections);
                    Assert.Equal(0, restored.TopologyRevision);
                }
    }

    [Fact]
    public void Transport_has_literal_layout_and_canonical_packed_payload()
    {
        var route = StructuralRoute.Cycle(4, [0, 1, 2, 3]);
        Assert.Equal(new byte[] { 1, 2, 0, 0, 4, 4, 0xE4, 0x0F }, route.Encode());
        var restored = StructuralRoute.Decode(new byte[] { 1, 2, 0, 0, 4, 4, 0xE4, 0x0F });
        Assert.Equal(new[] { 3, 0 }, restored.Edge(3));
        Assert.Equal(4, restored.StepCount);
    }

    [Fact]
    public void Transport_rejects_truncation_padding_missing_sites_and_invalid_grammar()
    {
        byte[] good = [1, 2, 0, 0, 4, 4, 0xE4, 0x0F];
        for (var length = 0; length < good.Length; length++)
            Assert.Throws<FormatException>(() => StructuralRoute.Decode(good.AsSpan(0, length)));
        foreach (var malformed in new byte[][]
        {
            [2, 2, 0, 0, 4, 4, 0xE4, 0x0F], // version
            [1, 3, 0, 0, 4, 4, 0xE4, 0x0F], // layout
            [1, 2, 2, 1, 4, 4, 0xE4, 0x0F], // invalid O/D context
            [1, 2, 0, 0, 0, 4, 0xE4, 0x0F], // no sites
            [1, 2, 0, 0, 4, 0],              // no edges
            [1, 1, 0, 0, 4, 1, 0],           // no chain edge
            [1, 2, 0, 0, 4, 4, 0xE4, 0x8F], // nonzero value padding
            [1, 2, 0, 0, 3, 4, 0xE4, 0x0F], // missing site 3
            [1, 2, 0, 0, 4, 4, 0xE4, 0x0F, 0], // trailing bytes
            [1, 2, 0, 0, 0x84, 0, 4, 0xE4, 0x0F], // noncanonical count
            [1, 2, 0, 0, 0xFF, 0xFF, 0xFF, 0xFF, 0x7F] // overflow
        }) Assert.Throws<FormatException>(() => StructuralRoute.Decode(malformed));
        Assert.Throws<FormatException>(() => StructuralRoute.Decode(new byte[] { 1, 2, 0, 0, 2, 1, 0x80, 1 }));
    }

    [Fact]
    public void Empty_and_invalid_programmer_layouts_fail_before_becoming_routes()
    {
        Assert.Throws<ArgumentException>(() => StructuralRoute.Pairs(1, []));
        Assert.Throws<ArgumentException>(() => StructuralRoute.Chain(1, [0]));
        Assert.Throws<ArgumentException>(() => StructuralRoute.Cycle(1, []));
        Assert.Throws<ArgumentException>(() => StructuralRoute.Pairs(2, [[0]]));
        Assert.Throws<ArgumentOutOfRangeException>(() => StructuralRoute.Cycle(2, [0, 2]));
        Assert.Throws<ArgumentException>(() => StructuralRoute.Cycle(2, [0, 1], words: [1]));
        var route = StructuralRoute.Cycle(2, [0, 1]);
        var before = route.Encode();
        Assert.Throws<ArgumentOutOfRangeException>(() => route.RetargetVertex(0, 2));
        Assert.Throws<ArgumentOutOfRangeException>(() => route.TryRetargetEndpoint(0, 2, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => route.SetWord(0, 8));
        Assert.Equal(before, route.Encode());
    }

    [Fact]
    public void Diagnostic_arrays_cannot_rewrite_packed_structure()
    {
        var route = StructuralRoute.Cycle(4, [0, 1, 2, 3]);
        var before = route.Encode();
        route.TopologyRows()[0][0] = 3;
        route.TopologyBytes()[0] = 0;
        route.WordBytes()[0] = 0;
        route.Edge(0)[0] = 2;
        Assert.Equal(before, route.Encode());
    }
}
