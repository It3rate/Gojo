using Gojo.Experiments.PrimitiveMeasure;
using Gojo.PrimitiveExplorer;

namespace Gojo.PrimitiveExplorer.Tests;

public sealed class InvarianceExplorerTests
{
    [Fact]
    public void Browsing_or_rereading_never_runs_a_trial()
    {
        var session = Open();
        Assert.Empty(session.Read().Blocks);
        Assert.Contains("Not run yet", session.Read().Result);
        session.Steps = 17;
        Assert.Empty(session.Read().Blocks);
        Choose(session, "Change to try", 1);
        Assert.Empty(session.Read().Blocks);
        Choose(session, "Concept experiment", InvarianceExperiments.Cases.Count - 1);
        Assert.Empty(session.Read().Blocks);
        Assert.Equal(0, session.Variant);
    }

    [Fact]
    public void The_page_charts_actual_probe_bytes_and_endpoint_roles()
    {
        var session = Open();
        Act(session, "Apply change");
        var result = InvarianceExperiments.Run(0, 0);
        var page = session.Read();
        Assert.Equal(result.BeforeBytes, Bytes(page, "Witness · first source excerpt"));
        Assert.Equal(result.AfterBytes, Bytes(page, "Witness · comparison source excerpt"));
        Assert.Equal(result.LawBytes, Bytes(page, "Law · admitted correspondence"));
        Assert.Equal(result.RowBytes, Bytes(page, "Connection · comparison"));
        Assert.Equal(new[] { result.VerdictSupport }, Bytes(page, "Read · correspondence answer"));
        var row = page.Blocks.Single(block => block.Title == "Connection · comparison");
        Assert.Equal(result.ReferenceBits * result.Wiring.Length, row.UsedBits);
        Assert.Equal(new[] { "before", "after", "verdict" }, row.Marks.Select(mark => mark.Keys.Last()));
        foreach (var key in new[] { "before", "after", "law", "verdict", "beforeartifact", "afterartifact" })
        {
            Assert.Contains(page.Drawing, item => item.Key == key);
            Assert.Contains(page.Blocks, block => block.Key == key || block.Marks.Any(mark => mark.Keys.Contains(key)));
        }
    }

    [Fact]
    public void A_retained_witness_is_stable_until_an_explicit_action()
    {
        var session = Open();
        Act(session, "Apply change");
        var page = session.Read();
        var bytes = page.Blocks.SelectMany(block => block.Bytes).ToArray();
        var result = page.Result;
        session.Steps += 100;
        Assert.Equal(bytes, session.Read().Blocks.SelectMany(block => block.Bytes));
        Assert.Equal(result, session.Read().Result);
        Act(session, "Reset");
        Assert.Empty(session.Read().Blocks);
        Assert.Contains("Not run yet", session.Read().Result);
    }

    [Fact]
    public void Changing_the_trial_discards_the_previous_answer()
    {
        var session = Open();
        Act(session, "Apply change");
        Choose(session, "Change to try", 1);
        Assert.Empty(session.Read().Blocks);
        Act(session, "Apply change");
        Choose(session, "Concept experiment", 1);
        Assert.Empty(session.Read().Blocks);
        Assert.Contains("Not run yet", session.Read().Result);
        session.Navigate(0);
        session.Navigate(43);
        Assert.Empty(session.Read().Blocks);
    }

    [Fact]
    public void Every_case_exposes_its_concept_and_limit_without_using_them_as_verdicts()
    {
        var session = Open();
        for (var index = 0; index < InvarianceExperiments.Cases.Count; index++)
        {
            Choose(session, "Concept experiment", index);
            var description = InvarianceExperiments.Cases[index];
            Assert.Equal($"Concept: {description.Concept}\nLimit: {description.Limit}", session.Read().DrawingGuide!.Value.Text);
            for (var variation = 0; variation < 3; variation++)
            {
                Choose(session, "Change to try", variation);
                Assert.Empty(session.Read().Blocks);
                Act(session, "Apply change");
                var result = InvarianceExperiments.Run(index, variation);
                var page = session.Read();
                Assert.Equal(new[] { result.VerdictSupport }, Bytes(page, "Read · correspondence answer"));
                Assert.Contains(result.Observation, page.Result);
                Assert.Contains(result.Change, page.Events);
                Assert.Contains("Losing a guarantee", page.Boundary);
            }
        }
    }

    [Fact]
    public void An_unresolved_read_is_not_painted_as_a_supported_concept()
    {
        var session = Open();
        var unresolved = false;
        for (var index = 0; index < InvarianceExperiments.Cases.Count; index++)
        {
            Choose(session, "Concept experiment", index);
            Choose(session, "Change to try", 2);
            Act(session, "Apply change");
            var page = session.Read();
            var answer = Bytes(page, "Read · correspondence answer")[0];
            if (answer != 3) continue;
            unresolved = true;
            Assert.Contains("Correspondence unresolved", page.Result);
            var node = page.Drawing.Single(item => item.Kind == "node" && item.Key == "verdict");
            Assert.True(node.Uncertain);
            Assert.False(node.Active);
        }
        Assert.True(unresolved);
    }

    [Fact]
    public void Source_sketches_use_the_actual_endpoint_rows_without_geometry_claims()
    {
        var session = Open();
        for (var index = 0; index < InvarianceExperiments.Cases.Count; index++)
        {
            Choose(session, "Concept experiment", index);
            Choose(session, "Change to try", 1);
            Act(session, "Apply change");
            var result = InvarianceExperiments.Run(index, 1);
            var page = session.Read();
            foreach (var (key, rows) in new[] { ("beforeartifact", result.BeforeLinks), ("afterartifact", result.AfterLinks) })
            {
                var drawn = page.Drawing.Where(item => item.Key == key && item.Kind == "edge" && item.Label.Length == 0).ToArray();
                Assert.Equal(rows.Count(row => row[0] != row[1]), drawn.Length);
                foreach (var row in rows.Where(row => row[0] != row[1]))
                    Assert.Contains(drawn, item => item.Detail.Contains($"site {row[0]} → site {row[1]}"));
            }
            Assert.Contains("arbitrary spacing and angles", page.Boundary);
            Assert.All(page.Drawing, item => Assert.InRange(item.Y, 0, 480));
        }
    }

    private static ExplorerSession Open()
    {
        var session = new ExplorerSession(); session.Navigate(43); return session;
    }
    private static void Choose(ExplorerSession session, string label, int value)
        => session.Read().Choices.Single(choice => choice.Label == label).Change(value);
    private static void Act(ExplorerSession session, string label)
        => session.Read().Actions.Single(action => action.Label == label).Run();
    private static byte[] Bytes(ExplorerPage page, string title)
        => page.Blocks.Single(block => block.Title == title).Bytes;
}
