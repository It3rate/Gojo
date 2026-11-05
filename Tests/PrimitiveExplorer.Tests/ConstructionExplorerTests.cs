using Gojo.PrimitiveExplorer;

namespace Gojo.PrimitiveExplorer.Tests;

public sealed class ConstructionExplorerTests
{
    [Fact]
    public void Shared_seam_uses_have_separate_arrows_and_one_stored_link()
    {
        var session = new ExplorerSession(); session.Navigate(40);
        Choose(session, "Pattern", "House");
        var page = session.Read();
        Assert.Equal("ConstructionHints.md", page.Source);
        Assert.Single(page.Drawing, item => item.Key == "link0");
        var seamUses = page.Drawing.Where(item => item.Kind == "edge" && item.RelatedKeys?.Contains("link0") == true).ToArray();
        Assert.Equal(2, seamUses.Length);
        Assert.NotEqual(seamUses[0].Key, seamUses[1].Key);
        Assert.NotEqual(seamUses[0].Color, seamUses[1].Color);
        Assert.Contains(page.Blocks.Single(block => block.Title == "Source · directed uses").Marks,
            mark => mark.Keys.Contains(seamUses[0].Key) && mark.Keys.Contains("link0"));
        Assert.Contains(page.Blocks.Single(block => block.Title == "Source · links").Marks,
            mark => mark.Keys.Contains(seamUses[0].Key) && mark.Keys.Contains("link0"));
        var field = page.Blocks.Single(block => block.Title == "Source · 1-bit value group").Marks
            .Single(mark => mark.Keys.Contains("field0"));
        Assert.Contains(seamUses[0].Key, field.Keys);
        Assert.Contains(seamUses[1].Key, field.Keys);
        Assert.Equal("field0", field.Keys[^1]);
        Assert.All(seamUses, use => Assert.Contains("field0", use.RelatedKeys!));
    }

    [Fact]
    public void Reverse_and_evidence_controls_preserve_the_complete_source()
    {
        var session = new ExplorerSession(); session.Navigate(40);
        Choose(session, "Pattern", "Y");
        var forward = session.Read();
        var source = forward.Blocks.Where(block => block.Title.StartsWith("Source ·")).SelectMany(block => block.Bytes).ToArray();
        Choose(session, "View", "Reverse");
        var reverse = session.Read();
        Assert.Equal(source, reverse.Blocks.Where(block => block.Title.StartsWith("Source ·")).SelectMany(block => block.Bytes));
        var arrow = forward.Drawing.First(item => item.Key == "use0");
        var reversed = reverse.Drawing.First(item => item.Key == "use0");
        Assert.Equal(arrow.X, reversed.X2, 8); Assert.Equal(arrow.Y, reversed.Y2, 8);
        Assert.Equal(arrow.X2, reversed.X, 8); Assert.Equal(arrow.Y2, reversed.Y, 8);
        Choose(session, "Evidence", "Connections only");
        var reduced = session.Read();
        Assert.Equal(source, reduced.Blocks.Where(block => block.Title.StartsWith("Source ·")).SelectMany(block => block.Bytes));
        Assert.DoesNotContain(reduced.Drawing, item => item.Kind == "edge");
        Assert.NotEqual(reverse.Blocks.Single(block => block.Title == "Published · known clues").Bytes,
            reduced.Blocks.Single(block => block.Title == "Published · known clues").Bytes);
    }

    [Fact]
    public void Changing_construction_keeps_an_outline_but_changes_the_inspectable_use_rows()
    {
        var session = new ExplorerSession(); session.Navigate(40);
        Choose(session, "Pattern", "A");
        var legs = session.Read();
        Choose(session, "Construction", "Loop and feet");
        var loop = session.Read();
        Assert.Equal(legs.Blocks.Single(block => block.Title == "Source · links").Bytes,
            loop.Blocks.Single(block => block.Title == "Source · links").Bytes);
        Assert.NotEqual(legs.Blocks.Single(block => block.Title == "Source · directed uses").Bytes,
            loop.Blocks.Single(block => block.Title == "Source · directed uses").Bytes);
        Assert.Contains(loop.Events, text => text.StartsWith("Source group 0:") && text.EndsWith("closed."));
        Assert.DoesNotContain(legs.Events, text => text.StartsWith("Source group") && text.EndsWith("; closed."));
    }

    private static void Choose(ExplorerSession session, string label, string value)
    {
        var choice = session.Read().Choices.Single(item => item.Label == label);
        choice.Change(Array.IndexOf(choice.Values, value));
    }
}
