using Gojo.PrimitiveExplorer;

namespace Gojo.PrimitiveExplorer.Tests;

public sealed class GuaranteeExplorerTests
{
    [Fact]
    public void Reading_and_selecting_a_change_do_not_execute_it_or_spend_inspection_work()
    {
        var session = Open(); var first = session.Read();
        Assert.Contains("not yet established", first.Result);
        var bytes = Blocks(first);
        session.Steps = 40;
        Assert.Equal(bytes, Blocks(session.Read()));
        Choose(session, "Change to try", "Move an interior junction");
        Assert.Equal(bytes, Blocks(session.Read()));
        Assert.StartsWith("0 inspection", session.Read().Status);
        Act(session, "Inspect one check");
        Assert.Contains("not yet established", session.Read().Result);
        for (var i = 0; i < 3; i++) Act(session, "Inspect one check");
        Assert.Contains("continuous · closed", session.Read().Result);
        Assert.StartsWith("4 inspection", session.Read().Status);
        Assert.DoesNotContain(session.Read().Blocks, b => b.Title.StartsWith("Witness"));
    }

    [Fact]
    public void A_positional_cycle_cannot_split_one_endpoint_until_explicitly_unpacked()
    {
        var session = Open("Loop · positional cycle");
        Act(session, "Inspect one check");
        var packed = Bytes(session.Read(), "Source · topology");
        Assert.Contains("continuous · closed", session.Read().Result);
        Act(session, "Apply change");
        Assert.Equal(packed, Bytes(session.Read(), "Source · topology"));
        Assert.Contains("continuous · closed", session.Read().Result);
        Assert.Contains(session.Read().Events, line => line.Contains("Unpack first"));
        Choose(session, "Change to try", "Unpack to endpoint pairs"); Act(session, "Apply change");
        Assert.Contains("not yet established", session.Read().Result);
        Assert.NotEqual(packed, Bytes(session.Read(), "Source · topology"));
        for (var i = 0; i < 4; i++) Act(session, "Inspect one check");
        Assert.Contains("continuous · closed", session.Read().Result);
        Choose(session, "Change to try", "Split the final arrival"); Act(session, "Apply change");
        Assert.Contains("not yet established", session.Read().Result);
        for (var i = 0; i < 4; i++) Act(session, "Inspect one check");
        Assert.Contains("continuous · not a closed walk", session.Read().Result);
    }

    [Fact]
    public void The_outer_vertex_of_a_chain_can_open_its_seam_without_unpacking()
    {
        var session = Open("Loop · shared vertex chain");
        Act(session, "Inspect one check");
        Assert.Contains("continuous · closed", session.Read().Result);
        Act(session, "Apply change");
        Assert.Contains("shared vertex chain", session.Read().Status);
        Assert.Contains("not yet established", session.Read().Result);
        Act(session, "Inspect one check");
        Assert.Contains("continuous · not a closed walk", session.Read().Result);
        Assert.Contains(session.Read().Events, line => line.Contains("open the seam directly"));
    }

    [Theory]
    [InlineData("Loop · separate endpoint pairs", "not continuous in this order", 1)]
    [InlineData("Loop · shared vertex chain", "continuous · closed", 2)]
    [InlineData("Loop · positional cycle", "continuous · closed", 2)]
    public void An_interior_move_exposes_separate_and_shared_endpoint_behaviour(string scenario, string result, int highlighted)
    {
        var session = Open(scenario);
        Choose(session, "Change to try", "Move an interior junction"); Act(session, "Apply change");
        for (var i = 0; i < 4; i++) Act(session, "Inspect one check");
        var page = session.Read();
        Assert.Contains(result, page.Result);
        Assert.Equal(highlighted, page.Drawing.Count(item => item.Kind == "edge" && item.Color == "#D66B2A"));
        Assert.Contains("GEOMETRIC SHAPE: NOT REPRESENTED", page.Drawing.Select(item => item.Label));
    }

    [Fact]
    public void Fixed_faces_do_not_prove_connections_and_owned_faces_do_not_erase_them()
    {
        var session = Open("Loose pairs · fixed O/D");
        for (var i = 0; i < 4; i++) Act(session, "Inspect one check");
        Assert.Contains("not continuous in this order", session.Read().Result);
        var values = Bytes(session.Read(), "Source · value observations");
        Choose(session, "Change to try", "Flip first direction"); Act(session, "Apply change");
        Assert.Equal(values, Bytes(session.Read(), "Source · value observations"));
        Assert.Contains(session.Read().Events, line => line.Contains("inherited"));
        Choose(session, "Case", "Chain · owned O/D");
        Act(session, "Inspect one check");
        var topology = Bytes(session.Read(), "Source · topology");
        values = Bytes(session.Read(), "Source · value observations");
        Choose(session, "Change to try", "Flip first direction"); Act(session, "Apply change");
        Assert.NotEqual(values, Bytes(session.Read(), "Source · value observations"));
        Assert.Equal(topology, Bytes(session.Read(), "Source · topology"));
        Assert.Contains("continuous · closed", session.Read().Result);
        Choose(session, "Change to try", "Toggle first participation"); Act(session, "Apply change");
        Assert.Equal(topology, Bytes(session.Read(), "Source · topology"));
        Assert.Contains("continuous · closed", session.Read().Result);
    }

    [Fact]
    public void Reload_preserves_the_artifact_without_importing_a_completed_inspection()
    {
        var session = Open("Chain · owned O/D");
        Act(session, "Inspect one check");
        var topology = Bytes(session.Read(), "Source · topology");
        var values = Bytes(session.Read(), "Source · value observations");
        Choose(session, "Change to try", "Reload the encoded artifact"); Act(session, "Apply change");
        Assert.Equal(topology, Bytes(session.Read(), "Source · topology"));
        Assert.Equal(values, Bytes(session.Read(), "Source · value observations"));
        Assert.Contains("not yet established", session.Read().Result);
        Assert.StartsWith("0 inspection", session.Read().Status);
        Act(session, "Inspect one check");
        Assert.Contains("continuous · closed", session.Read().Result);
    }

    [Theory]
    [InlineData("Four uses · shared reach", 4)]
    [InlineData("Four uses · independent reaches", 1)]
    [InlineData("Opposed uses · shared source", 2)]
    [InlineData("Opposed uses · detached copies", 1)]
    public void Shared_bindings_change_the_right_reads_after_a_hidden_request_is_retried(string scenario, int changed)
    {
        var session = Open(scenario);
        var before = session.Read(); var geometry = Geometry(before); var source = Sources(before);
        Choose(session, "Response read", "Concealed control");
        Act(session, "Offer / retry request");
        Assert.Contains("Changed uses: none", session.Read().Result);
        Assert.Contains("request remains waiting", session.Read().Result);
        var held = Bytes(session.Read(), "Runtime · current adjustment");
        Assert.Equal(held, Bytes(session.Read(), "Runtime · current adjustment"));
        Choose(session, "Response read", "Known control"); Act(session, "Offer / retry request");
        var after = session.Read();
        Assert.Equal(changed, after.Drawing.Count(item => item.Kind == "edge" && item.Color == "#D66B2A"));
        Assert.Equal(geometry, Geometry(after));
        Assert.Equal(source, Sources(after));
        var settled = Bytes(after, "Runtime · current adjustment");
        Act(session, "Offer / retry request");
        Assert.Equal(settled, Bytes(session.Read(), "Runtime · current adjustment"));
        Act(session, "Reset");
        Assert.Contains("Changed uses: none", session.Read().Result);
    }

    [Fact]
    public void A_blocked_or_unresolved_gate_holds_the_activation_without_opening_the_cycle()
    {
        var session = Open("Closed loop · controlled gate");
        Choose(session, "Gate read", "Blocked · 0");
        for (var i = 0; i < 4; i++) Act(session, "Advance one beat");
        var held = Bytes(session.Read(), "Runtime · current activation");
        Act(session, "Advance one beat");
        Assert.Equal(held, Bytes(session.Read(), "Runtime · current activation"));
        Assert.Contains("Topology: closed", session.Read().Result);
        Assert.Contains("Actual activation: s3", session.Read().Result);
        Assert.Contains(session.Read().Drawing, item => item.Kind == "node" && item.Key == "site3" && item.Color == "#D66B2A" && item.Label.Contains("activation"));
        Choose(session, "Gate read", "Uncertain · {0,1}"); Act(session, "Advance one beat");
        Assert.Equal(held, Bytes(session.Read(), "Runtime · current activation"));
        Choose(session, "Gate read", "Open · 1"); Act(session, "Advance one beat");
        Assert.Contains("Topology: closed", session.Read().Result);
        Assert.Contains("Actual activation: s0", session.Read().Result);
        Assert.Contains("1 occupied activation slot", session.Read().Status);
    }

    private static ExplorerSession Open(string? scenario = null)
    {
        var session = new ExplorerSession(); session.Navigate(42);
        if (scenario is not null) Choose(session, "Case", scenario);
        return session;
    }
    private static void Choose(ExplorerSession session, string label, string value)
    {
        var choice = session.Read().Choices.Single(item => item.Label == label);
        var index = Array.IndexOf(choice.Values, value); Assert.True(index >= 0); choice.Change(index);
    }
    private static void Act(ExplorerSession session, string label) => session.Read().Actions.Single(action => action.Label == label).Run();
    private static byte[] Bytes(ExplorerPage page, string title) => page.Blocks.Single(block => block.Title == title).Bytes;
    private static byte[] Blocks(ExplorerPage page) => page.Blocks.SelectMany(block => block.Bytes).ToArray();
    private static byte[] Sources(ExplorerPage page) => page.Blocks.Where(block => block.Title.StartsWith("Source ·")).SelectMany(block => block.Bytes).ToArray();
    private static (double, double, double, double)[] Geometry(ExplorerPage page) => page.Drawing.Where(item => item.Kind == "edge")
        .Select(item => (item.X, item.Y, item.X2, item.Y2)).ToArray();
}
