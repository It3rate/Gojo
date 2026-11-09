using System.Text.Json;
using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;
using Gojo.Experiments.QualitativeFrieze.Composition;
using static Gojo.Experiments.QualitativeFrieze.Composition.GlyphJointUsage;

namespace QualitativeExecution.Tests;

public sealed class GlyphJointUsageTests
{
    private static readonly Lazy<GlyphJointUsageReport> Study = new(GlyphJointUsageStudy.Run);

    [Fact]
    public void Same_corner_ink_can_retain_different_stroke_history_without_inventing_a_passage()
    {
        var continuous = GlyphJointUsageStudy.Bend(false, true);
        var attached = GlyphJointUsageStudy.Bend(true, false);
        Assert.Equal(continuous.Description.Points, attached.Description.Points);
        Assert.Equal(continuous.Description.Lines.Select(line => (line.From, line.To)), attached.Description.Lines.Select(line => (line.From, line.To)));
        Assert.Single(E(continuous.Model.History)); Assert.Equal(2, E(attached.Model.History).Length);
        continuous.Seed("top"); attached.Seed("top");
        var moved = continuous.Probe("read", ["top"], ["run"], "");
        var unknown = attached.Probe("read", ["top"], ["run"], "");
        Assert.Single(E(moved.Accepted)); Assert.Empty(E(unknown.Accepted)); Assert.Empty(E(unknown.Blocked));
        Assert.Single(E(unknown.Remainder)); Assert.NotEmpty(E(unknown.Unresolved)); Assert.Same(unknown.Before, unknown.After);
    }

    [Fact]
    public void Adding_explicit_passage_to_an_attachment_gives_the_same_selected_response_as_a_bend()
    {
        var attachment = GlyphJointUsageStudy.Bend(true, true); attachment.Seed("top");
        var read = attachment.Probe("go", ["top"], ["run"], "");
        Assert.Equal("moved", read.Outcome); Assert.Single(E(read.Accepted));
        Assert.Single(E(attachment.Model.Transport.PortPackets(attachment.Ports["right"])));
        Assert.Equal(2, E(attachment.Model.History).Length); Assert.True(read.OwnershipPreserved);
    }

    [Fact]
    public void Closed_missing_route_blocks_while_open_use_law_remains_unknown()
    {
        var actual = Study.Value.Cases;
        var stop = Assert.Single(actual, item => item.Id == "t-stop").Probes[0];
        var unknown = Assert.Single(actual, item => item.Id == "l-attached-open").Probes[0];
        Assert.Equal("blocked", stop.Outcome); Assert.Equal(1, stop.Blocked); Assert.Equal(0, stop.Remainder);
        Assert.Equal("unknown", unknown.Outcome); Assert.Equal(0, unknown.Blocked); Assert.Equal(1, unknown.Remainder);
    }

    [Fact]
    public void Uncertain_switch_exposes_both_outlets_without_copying_or_moving_the_packet()
    {
        var fixture = GlyphJointUsageStudy.Switch(); fixture.Seed("bottom");
        var read = fixture.Probe("either", ["bottom"], ["left", "right"], "");
        Assert.Equal("choice", read.Outcome); Assert.Equal(2, E(read.Candidates).Length);
        Assert.Empty(E(read.Accepted)); Assert.Single(E(read.Remainder)); Assert.Same(read.Before, read.After);
        Assert.Single(E(fixture.Model.Transport.PortPackets(fixture.Ports["bottom"])));
        Assert.Empty(E(fixture.Model.Transport.PortPackets(fixture.Ports["left"])));
        Assert.Empty(E(fixture.Model.Transport.PortPackets(fixture.Ports["right"]))); Assert.True(read.OwnershipPreserved);
        var settled = fixture.Probe("left", ["bottom"], ["left"], "");
        Assert.Single(E(settled.Accepted)); Assert.Empty(E(fixture.Model.Transport.PortPackets(fixture.Ports["bottom"])));
    }

    [Fact]
    public void A_known_candidate_plus_an_open_control_tail_does_not_authorize_execution()
    {
        var fixture = GlyphJointUsageStudy.Switch(); fixture.Seed("bottom");
        var read = fixture.Probe("open", ["bottom"], ["left"], "", openControl: true);
        Assert.Equal("unknown", read.Outcome); Assert.Single(E(read.Candidates));
        Assert.Single(E(read.Remainder)); Assert.Empty(E(read.Accepted)); Assert.Same(read.Before, read.After);
    }

    [Fact]
    public void One_candidate_route_does_not_authorize_movement_when_another_control_means_stop()
    {
        var fixture = GlyphJointUsageStudy.Gate(); fixture.Seed("bottom");
        var read = fixture.Probe("maybe", ["bottom"], ["go", "stop"], "");
        Assert.Equal("choice", read.Outcome); Assert.Single(E(read.Candidates)); Assert.Empty(E(read.Accepted));
        Assert.Single(E(read.Remainder)); Assert.Same(read.Before, read.After);
        var narrowed = fixture.Probe("go", ["bottom"], ["go", "also-go"], "");
        Assert.Single(E(narrowed.Accepted)); Assert.True(narrowed.OwnershipPreserved);
    }

    [Fact]
    public void Empty_control_support_is_not_a_known_stop_or_a_permission_to_move()
    {
        var fixture = GlyphJointUsageStudy.Gate(); fixture.Seed("bottom");
        var read = fixture.Probe("no-control", ["bottom"], [], "");
        Assert.Equal("unknown", read.Outcome); Assert.Empty(E(read.Accepted)); Assert.Empty(E(read.Blocked));
        Assert.Single(E(read.Remainder)); Assert.NotEmpty(E(read.Unresolved)); Assert.Same(read.Before, read.After);
    }

    [Fact]
    public void Merge_preserves_two_material_identities_and_shared_receiver_capacity()
    {
        var fixture = GlyphJointUsageStudy.Merge(2); fixture.Seed("left"); fixture.Seed("right");
        var before = E(fixture.Model.Transport.ImportedPackets);
        var read = fixture.Probe("merge", ["left", "right"], ["run"], "");
        Assert.Equal(2, E(read.Accepted).Length); Assert.Empty(E(read.Blocked)); Assert.True(read.OwnershipPreserved);
        Assert.Equal(before, E(fixture.Model.Transport.PortPackets(fixture.Ports["bottom"])));
        Assert.Equal(2, before.Distinct<IElement>(ReferenceEqualityComparer.Instance).Count());
        Assert.True(fixture.Model.Transport.TryReadAccounting(read.Receipt, out var incoming, out var accepted, out var blocked, out var remainder));
        Assert.Equal(E(incoming).Length, E(accepted).Length + E(blocked).Length + E(remainder).Length);
    }

    [Fact]
    public void Insufficient_shared_merge_capacity_refuses_the_atomic_batch_without_erasing_one_input()
    {
        var fixture = GlyphJointUsageStudy.Merge(1); fixture.Seed("left"); fixture.Seed("right");
        var read = fixture.Probe("crowded", ["left", "right"], ["run"], "");
        Assert.Empty(E(read.Accepted)); Assert.Equal(2, E(read.Blocked).Length); Assert.Empty(E(read.Remainder));
        Assert.Same(read.Before, read.After); Assert.True(read.OwnershipPreserved);
        Assert.Single(E(fixture.Model.Transport.PortPackets(fixture.Ports["left"])));
        Assert.Single(E(fixture.Model.Transport.PortPackets(fixture.Ports["right"])));
        Assert.Empty(E(fixture.Model.Transport.PortPackets(fixture.Ports["bottom"])));
    }

    [Fact]
    public void Bidirectional_bridge_requires_two_native_routes_and_can_return_the_same_packet()
    {
        var oneWay = GlyphJointUsageStudy.Bridge(false); oneWay.Seed("left");
        oneWay.Probe("out", ["left"], ["run"], "");
        var denied = oneWay.Probe("back", ["right"], ["run"], "");
        Assert.Single(E(denied.Blocked));
        var twoWay = GlyphJointUsageStudy.Bridge(true); twoWay.Seed("left");
        var packet = Assert.Single(E(twoWay.Model.Transport.ImportedPackets));
        twoWay.Probe("out", ["left"], ["run"], "");
        var returned = twoWay.Probe("back", ["right"], ["run"], "");
        Assert.Same(packet, Assert.Single(E(returned.Accepted)));
        Assert.Same(packet, Assert.Single(E(twoWay.Model.Transport.PortPackets(twoWay.Ports["left"]))));
        Assert.Equal(2, E(twoWay.Model.Transport.Routes).Length);
    }

    [Fact]
    public void Coincident_crossing_sites_stay_distinct_until_a_shared_site_and_turn_are_encoded()
    {
        var independent = GlyphJointUsageStudy.Cross(false);
        Assert.NotSame(independent.Ports["middle-h"].Left, independent.Ports["middle-v"].Left);
        var points = independent.Description.Points;
        Assert.Equal(points.Single(p => p.Id == "middle-h").X, points.Single(p => p.Id == "middle-v").X);
        Assert.Equal(points.Single(p => p.Id == "middle-h").Y, points.Single(p => p.Id == "middle-v").Y);
        independent.Seed("west"); independent.Seed("south");
        var read = independent.Probe("cross", ["west", "south"], ["run"], "");
        Assert.Equal(2, E(read.Accepted).Length); Assert.True(read.OwnershipPreserved);
        var joined = GlyphJointUsageStudy.Cross(true); joined.Seed("west");
        joined.Probe("turn", ["west"], ["run"], "");
        Assert.Single(E(joined.Model.Transport.PortPackets(joined.Ports["north"])));
        Assert.Empty(E(joined.Model.Transport.PortPackets(joined.Ports["east"])));
    }

    [Fact]
    public void A_forged_path_cannot_bridge_distinct_crossing_sites_even_if_their_diagram_coordinates_match()
    {
        var f = GlyphJointUsageStudy.Cross(false); Assert.True(TryRead(f.Model.Surface, out var read));
        var route = new Relation(f.Ports["west"], f.Ports["north"]);
        var horizontal = (Relation)E(f.Model.Paths)[0]; var vertical = (Relation)E(f.Model.Paths)[1];
        var first = E((Relation)horizontal.Right)[0]; var last = E((Relation)vertical.Right)[1];
        var forged = read with { Paths = S(new Relation(route, S(first, last))) };
        Assert.False(RouteIsConnected(forged, route));
    }

    [Fact]
    public void Native_roundtrip_preserves_route_aliases_and_executes_the_permission_read_without_names()
    {
        var fixture = GlyphJointUsageStudy.Switch();
        var roots = GojoNumericFormat.Decode(GojoNumericFormat.Encode(fixture.Model.Surface,
            fixture.Ports["bottom"], fixture.Controls["left"], fixture.Routes["take-left"]));
        var surface = Assert.IsType<Relation>(roots[0]); Assert.True(TryRead(surface, out var definition));
        var probe = FiniteLaw.Evaluate(definition.Law, S(roots[1]), S(roots[2]));
        Assert.True(FiniteLaw.TryReadResult(probe, out var support, out var unresolved, out _));
        Assert.Same(roots[3], Assert.Single(E(support))); Assert.Empty(E(unresolved));
        Assert.True(RouteIsConnected(definition, (Relation)roots[3]));
        Assert.NotSame(fixture.Ports["bottom"], roots[1]);
    }

    [Fact]
    public void Repeated_input_selection_cannot_dispatch_one_packet_twice()
    {
        var fixture = GlyphJointUsageStudy.Bend(false, true); fixture.Seed("top");
        var read = fixture.Model.Apply(S(fixture.Ports["top"], fixture.Ports["top"]), S(fixture.Controls["run"]));
        Assert.Empty(E(read.Accepted)); Assert.Single(E(read.Incoming)); Assert.Single(E(read.Remainder));
        Assert.Same(read.Before, read.After); Assert.True(read.OwnershipPreserved); Assert.Equal("unknown", read.Outcome);
    }

    [Fact]
    public void A_forged_continuous_stroke_with_a_disconnected_jump_does_not_read_as_a_history()
    {
        var f = GlyphJointUsageStudy.Cross(false);
        var horizontal = (Relation)E(f.Model.History)[0]; var vertical = (Relation)E(f.Model.History)[1];
        var falseHistory = S(S(E(horizontal)[0], E(vertical)[1]));
        var falseSurface = new Relation(new Relation(f.Model.Edges, new Relation(f.Model.Ports,
            new Relation(f.Model.Paths, falseHistory))), f.Model.Surface.Right);
        Assert.False(TryRead(falseSurface, out _));
    }

    [Fact]
    public void The_study_is_serializable_and_every_probe_accounts_for_its_selected_packets()
    {
        Assert.Equal(16, Study.Value.Cases.Length);
        Assert.All(Study.Value.Cases.SelectMany(item => item.Probes), probe =>
        {
            Assert.True(probe.OwnershipPreserved);
            Assert.Equal(probe.Incoming, probe.Accepted + probe.Blocked + probe.Remainder);
        });
        var json = JsonSerializer.Serialize(Study.Value); Assert.Contains("t-switch-uncertain", json);
        Assert.DoesNotContain("ArchiveRoot", json);
        var archived = Assert.IsType<Relation>(Assert.Single(GojoNumericFormat.Decode(GojoNumericFormat.Encode(Study.Value.ArchiveRoot))));
        Assert.Equal(16, E(archived).Length);
        Assert.All(E(archived), item => Assert.True(TryRead((Relation)((Relation)item).Left, out _)));
    }
}
