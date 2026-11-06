using Gojo.Experiments.PrimitiveMeasure;
using Gojo.PrimitiveExplorer;

namespace Gojo.PrimitiveExplorer.Tests;

public sealed class RaceExplorerTests
{
    private static ExplorerSession Open()
    {
        var session = new ExplorerSession();
        session.Navigate(Array.IndexOf(ExplorerSession.Chapters, "Who crossed first?"));
        return session;
    }

    private static void Choose(ExplorerSession session, string label, int selected)
        => session.Read().Choices.Single(c => c.Label == label).Change(selected);

    private static void Act(ExplorerSession session, string label)
        => session.Read().Actions.Single(a => a.Label == label).Run();

    private static byte[] Bits(ExplorerPage page, string title)
        => page.Blocks.Single(block => block.Title == title).Bytes;

    [Theory]
    [InlineData(1, "horse A")]
    [InlineData(2, "horse A")]
    [InlineData(8, "unmatched board")]
    [InlineData(9, "unmatched board")]
    public void The_board_names_a_horse_only_through_shared_race_and_entrant_roles(int scenario, string favorite)
    {
        var session = Open();
        Choose(session, "Race to try", scenario);
        Act(session, "Run race");
        Assert.Contains(session.Read().Events, entry => entry.Contains($"Favorite before the start · {favorite}."));
    }

    [Fact]
    public void Changing_the_race_or_course_withdraws_the_previous_witness()
    {
        var session = Open();
        Assert.Empty(session.Read().Blocks);
        Act(session, "Run race");
        Assert.Contains(session.Read().Choices, x => x.Label == "Recorded moment");
        Choose(session, "Private course", 1);
        Assert.Empty(session.Read().Blocks);
        Assert.DoesNotContain(session.Read().Choices, x => x.Label == "Recorded moment");
        Act(session, "Run race");
        Choose(session, "Race to try", 1);
        Assert.Empty(session.Read().Blocks);
        Act(session, "Run race");
        Act(session, "Reset");
        Assert.Empty(session.Read().Blocks);
    }

    [Fact]
    public void The_display_distinguishes_an_unresolved_photo_from_a_shared_finish_event()
    {
        var session = Open();
        Choose(session, "Race to try", 4);
        Act(session, "Run race");
        var unknown = session.Read();
        Assert.Contains("order unresolved", unknown.Result);
        Assert.Equal(new byte[]{7}, Bits(unknown, "Finish order"));
        Choose(session, "Race to try", 5);
        Act(session, "Run race");
        var tie = session.Read();
        Assert.Contains("same event", tie.Result);
        Assert.Equal(new byte[]{4}, Bits(tie, "Finish order"));
        Assert.NotEqual(Bits(unknown, "Arrival sites · A and B"), Bits(tie, "Arrival sites · A and B"));
    }

    [Fact]
    public void Inspecting_a_prior_moment_does_not_relabel_the_final_evidence_as_earlier()
    {
        var session = Open();
        Choose(session, "Race to try", 12);
        Act(session, "Run race");
        var reference = RaceExperiments.Run(12);
        var recorded = session.Read();
        Assert.Equal(reference.PassageRows, Bits(recorded, "Passage connections"));
        Assert.Equal(reference.ConsumerLawBytes, Bits(recorded, "Favorite and finish law"));
        for (var i = 0; i < reference.Timeline.Length; i++)
        {
            Choose(session, "Recorded moment", i);
            var page = session.Read();
            Assert.Equal(new[]{ reference.Timeline[i].Assistance }, Bits(page, "Assistance released?"));
            Assert.Equal(new[]{ reference.Timeline[i].FinishOrder }, Bits(page, "Finish order"));
            if (i < reference.Timeline.Length - 1)
                Assert.DoesNotContain(page.Blocks, block => block.Title == "Arrival sites · A and B");
        }
        Assert.Equal(recorded.Result, session.Read().Result);
    }
}
