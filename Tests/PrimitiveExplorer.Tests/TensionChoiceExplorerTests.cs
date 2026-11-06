using Gojo.Experiments.PrimitiveMeasure;
using Gojo.PrimitiveExplorer;

namespace Gojo.PrimitiveExplorer.Tests;

public sealed class TensionChoiceExplorerTests
{
    private static ExplorerSession Open()
    {
        var session = new ExplorerSession();
        session.Navigate(Array.IndexOf(ExplorerSession.Chapters, "Which action helps?"));
        return session;
    }

    private static void Select(ExplorerSession session, string label, int value) =>
        session.Read().Choices.Single(c => c.Label == label).Change(value);

    private static void Run(ExplorerSession session) =>
        session.Read().Actions.Single(a => a.Label == "Run trial").Run();

    private static byte[] Bytes(ExplorerPage page, string title) =>
        page.Blocks.Single(block => block.Title == title).Bytes;

    [Fact]
    public void Scrubbing_replays_the_selected_turns_bytes_and_counts_only_choices_through_that_turn()
    {
        var session = Open();
        var trial = TensionChoiceExperiments.Run(0);
        Run(session);
        for (int i = trial.Timeline.Length - 1; i >= 0; i--)
        {
            Select(session, "Recorded turn", i);
            var beat = trial.Timeline[i];
            var page = session.Read();
            Assert.Equal(new byte[] { beat.OpportunityBits }, Bytes(page, "One opportunity"));
            Assert.Equal(beat.MemoryBytes, Bytes(page, "Learned relief"));
            Assert.Equal(trial.ChoiceRows, Bytes(page, "Choice passages"));
            Assert.Equal(trial.ReliefRows, Bytes(page, "Outcome passages"));
            for (var action = 0; action < 3; action++)
            {
                int count = trial.Timeline.Take(i + 1).Count(b => b.Action == action);
                Assert.Contains(page.Events, line => line.StartsWith($"Outlet {(char)('A' + action)} · {count} selections"));
            }
        }
    }

    [Fact]
    public void Changing_the_trial_withdraws_the_recording_and_the_priority_run_exposes_its_own_focus_rows()
    {
        var session = Open();
        Run(session);
        Select(session, "Trial", TensionChoiceExperiments.Names.Length);
        Assert.Empty(session.Read().Blocks);
        Run(session);
        var priority = NeedPriority.RunExample();
        for (int i = 0; i < priority.Timeline.Length; i++)
        {
            Select(session, "Recorded turn", i);
            var page = session.Read();
            Assert.Equal(new byte[] { priority.Timeline[i].FocusSupport }, Bytes(page, "Focus read"));
            Assert.Equal(priority.PriorityRows, page.Blocks.Where(block => block.Title.StartsWith("Focus connection · "))
                .SelectMany(block => block.Bytes).ToArray());
        }
        session.Read().Actions.Single(a => a.Label == "Reset").Run();
        Assert.Empty(session.Read().Blocks);
        Assert.DoesNotContain(session.Read().Choices, c => c.Label == "Recorded turn");
    }

    [Fact]
    public void Unpublished_feedback_stays_unknown_even_when_world_progress_is_visible()
    {
        var session = Open();
        var trial = TensionChoiceExperiments.Run(7);
        var i = Array.FindIndex(trial.Timeline, b => b.Action >= 0 && b.Focus >= 0 &&
            b.Observed[b.Focus] is null && b.Before[b.Focus] != b.After[b.Focus]);
        Assert.True(i >= 0);
        Select(session, "Trial", 7);
        Run(session);
        Select(session, "Recorded turn", i);
        Assert.Contains("Feedback to the chooser unknown.", session.Read().Result);
    }
}
