using Gojo.PrimitiveExplorer;

namespace Gojo.PrimitiveExplorer.Tests;

public sealed class FamilyExplorerTests
{
    [Fact]
    public void Reversing_the_family_view_changes_endpoint_order_without_changing_any_storage()
    {
        var session = new ExplorerSession();
        session.Navigate(39);
        session.Read().Choices[0].Change(5);
        var forward = session.Read();
        Assert.Equal("JoinedFamilies.md", forward.Source);
        Assert.Contains("Ordered return is closed", forward.Result);
        forward.Choices[3].Change(1);
        var reverse = session.Read();
        Assert.Equal(forward.Blocks.SelectMany(b => b.Bytes), reverse.Blocks.SelectMany(b => b.Bytes));
        Assert.Equal("link0", forward.Drawing.First(d => d.Kind == "edge").Key);
        Assert.Equal("link3", reverse.Drawing.First(d => d.Kind == "edge").Key);
        var first = forward.Drawing.First(d => d.Key == "link0");
        var reversed = reverse.Drawing.First(d => d.Key == "link0");
        Assert.Equal(first.X, reversed.X2);
        Assert.Equal(first.Y, reversed.Y2);
        Assert.Equal(first.X2, reversed.X);
        Assert.Equal(first.Y2, reversed.Y);
        Assert.Contains("Ordered return is closed", reverse.Result);
    }

    [Fact]
    public void The_chart_exposes_shared_value_aliases_instead_of_treating_equal_looking_fields_as_shared()
    {
        var session = new ExplorerSession(); session.Navigate(39);
        session.Read().Choices[0].Change(4);
        var independent = session.Read();
        var independentPayload = independent.Blocks.Single(b => b.Title == "1-bit values · packed instances");
        Assert.Equal(12, independentPayload.UsedBits);
        Assert.DoesNotContain(independentPayload.Marks, mark => mark.Keys.Contains("link0") && mark.Keys.Contains("link1"));
        session.Read().Choices[0].Change(5);
        var shared = session.Read();
        var sharedPayload = shared.Blocks.Single(b => b.Title == "1-bit values · packed instances");
        Assert.Equal(3, sharedPayload.UsedBits);
        Assert.Contains(sharedPayload.Marks, mark => mark.Keys.Contains("link0") && mark.Keys.Contains("link1") && mark.Keys.Contains("link3"));
        Assert.Contains(shared.Events, text => text.Contains("one amount") || text.Contains("shared state"));
    }

    [Fact]
    public void Zero_and_bare_participation_do_not_gain_endpoints_from_the_picture()
    {
        var session = new ExplorerSession(); session.Navigate(39);
        var bare = session.Read();
        Assert.Empty(bare.Blocks.Single(b => b.Title == "Template connections").Bytes);
        Assert.DoesNotContain(bare.Drawing, item => item.Kind == "edge");
        bare.Choices[2].Change(2);
        var zero = session.Read();
        Assert.Equal(new byte[] { 0 }, zero.Blocks.Single(b => b.Title == "Positive-side observation").Bytes);
        Assert.Equal(new byte[] { 2 }, zero.Blocks.Single(b => b.Title == "Negative-side observation").Bytes);
        Assert.Contains("no endpoints", zero.Result);
        session.Read().Choices[0].Change(5);
        var zeroCycle = session.Read();
        Assert.Contains("Ordered return is closed", zeroCycle.Result);
        Assert.All(zeroCycle.Drawing.Where(item => item.Kind == "edge"), item => Assert.False(item.Active));
        Assert.Contains(zeroCycle.Events, text => text.Contains("Zero participation preserves"));
    }
}
