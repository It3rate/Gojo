using Gojo.PrimitiveExplorer;

namespace Gojo.PrimitiveExplorer.Tests;

public sealed class BehaviorExplorerTests
{
    [Fact]
    public void Reads_do_not_execute_and_trial_switches_preserve_both_owners()
    {
        var session = Open();
        var first = session.Read();
        var source = Source(first);
        var initial = Bytes(first, "Runtime · current route state");
        session.Steps = 40;
        Assert.Equal(initial, Bytes(session.Read(), "Runtime · current route state"));
        Step(session);
        var routed = Bytes(session.Read(), "Runtime · current route state");
        Assert.NotEqual(initial, routed);
        Assert.Equal(initial, Bytes(session.Read(), "Witness · before last beat"));
        Choose(session, "Trial", "Adjust a reach");
        var before = session.Read();
        Step(session);
        var after = session.Read();
        Assert.Contains("Actual reach: farther", after.Result);
        Assert.Equal(source, Source(after));
        Assert.Equal(Geometry(before), Geometry(after));
        Assert.Contains(after.Drawing, item => item.Key == "link0" && item.Color == "#D66B2A");
        Choose(session, "Trial", "Follow routes");
        Assert.Equal(routed, Bytes(session.Read(), "Runtime · current route state"));
        Choose(session, "Trial", "Adjust a reach");
        Assert.Contains("Actual reach: farther", session.Read().Result);
        session.Read().Actions.Single(action => action.Label == "Reset both trials").Run();
        Assert.Contains("Actual reach: rest", session.Read().Result);
        Assert.DoesNotContain(session.Read().Blocks, block => block.Title == "Witness · before last request");
    }

    [Fact]
    public void Uncertain_selector_holds_the_actual_request_and_can_be_revealed_without_reset()
    {
        var session = Open();
        Choose(session, "Case", "Y · explicit selector");
        Choose(session, "Selector read", "Uncertain · {0,1}");
        Step(session); Step(session); // input to stem, then stem to junction
        var held = session.Read();
        Assert.Contains("s2 or s3", held.Result);
        Assert.Contains("request holds", held.Result);
        var actual = Bytes(held, "Runtime · current route state");
        Assert.Equal(2, held.Drawing.Count(item => item.Kind == "circle" && item.Uncertain));
        Step(session);
        Assert.Equal(actual, Bytes(session.Read(), "Runtime · current route state"));
        Choose(session, "Selector read", "Second arm · 1");
        Assert.Equal(actual, Bytes(session.Read(), "Runtime · current route state"));
        Step(session);
        Assert.Contains("Actual request: s3", session.Read().Result);
    }

    [Fact]
    public void Hidden_reach_control_preserves_a_known_waiting_request_then_retries_it()
    {
        var session = Open();
        Choose(session, "Trial", "Adjust a reach");
        Choose(session, "Response read", "Concealed control");
        Step(session);
        var held = session.Read();
        Assert.Contains("Actual reach: rest", held.Result);
        Assert.Contains("waiting request present", held.Result);
        Assert.False(held.Drawing.Single(item => item.Key == "waiting").Uncertain);
        Assert.NotEqual(Bytes(held, "Prediction · next actual policy"), Bytes(held, "Prediction · possible resolved settlement"));
        Choose(session, "Response read", "Known control");
        Step(session);
        Assert.Contains("Actual reach: farther", session.Read().Result);
        var settled = Bytes(session.Read(), "Runtime · current adjustment state");
        Step(session);
        Assert.Equal(settled, Bytes(session.Read(), "Runtime · current adjustment state"));
    }

    [Theory]
    [InlineData("Path · pinned adjustment", "rest")]
    [InlineData("Path · open response", "unbound")]
    public void Pinned_or_missing_response_cannot_invent_farther(string selected, string tip)
    {
        var session = Open();
        Choose(session, "Case", selected);
        Choose(session, "Trial", "Adjust a reach");
        Step(session);
        var page = session.Read();
        Assert.Contains("Actual reach: " + tip, page.Result);
        Assert.Contains("waiting request present", page.Result);
        Assert.DoesNotContain(page.Drawing, item => item.Kind == "stroke" && item.Color == "#D66B2A");
    }

    private static ExplorerSession Open() { var session = new ExplorerSession(); session.Navigate(41); return session; }
    private static void Step(ExplorerSession session) => session.Read().Actions[0].Run();
    private static void Choose(ExplorerSession session, string label, string value)
    {
        var choice = session.Read().Choices.Single(item => item.Label == label);
        var index = Array.IndexOf(choice.Values, value);
        Assert.True(index >= 0);
        choice.Change(index);
    }
    private static byte[] Bytes(ExplorerPage page, string title) => page.Blocks.Single(block => block.Title == title).Bytes;
    private static byte[] Source(ExplorerPage page) => page.Blocks.Where(block => block.Title.StartsWith("Source ·")).SelectMany(block => block.Bytes).ToArray();
    private static (double, double, double, double)[] Geometry(ExplorerPage page) => page.Drawing.Where(item => item.Kind is "stroke" or "edge")
        .Select(item => (item.X, item.Y, item.X2, item.Y2)).ToArray();
}
