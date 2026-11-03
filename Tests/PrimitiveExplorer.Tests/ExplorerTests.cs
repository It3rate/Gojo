using Gojo.PrimitiveExplorer;

namespace Gojo.PrimitiveExplorer.Tests;

public sealed class ExplorerTests
{
    [Fact]
    public void Each_letter_tile_selects_its_own_recipe_and_preserves_the_comparison_settings()
    {
        var session = new ExplorerSession();
        session.Navigate(24);
        session.Variant = 1;
        session.Control = 0;
        var names = Gojo.Experiments.PrimitiveMeasure.StraightCapitals.Names;
        var recipes = new HashSet<string>();
        foreach (var name in names.Reverse())
        {
            var page = session.Read();
            var tiles = page.Drawing.Where(d => d.Selects.HasValue).ToArray();
            Assert.Equal(names, tiles.Select(t => t.Label));
            var tile = Assert.Single(tiles, t => t.Label == name);
            var (choice, index) = tile.Selects!.Value;
            Assert.Same(page.Choices[0], choice);
            Assert.True(choice.BrowseWithArrows);
            choice.Change(index);

            var selected = session.Read();
            Assert.Equal(name, Assert.Single(selected.Drawing, d => d.Selects.HasValue && d.Active).Label);
            var (heading, recipe) = selected.DrawingGuide!.Value;
            Assert.Equal("How to draw " + name, heading);
            Assert.EndsWith(recipe, tile.Detail);
            Assert.True(recipes.Add(recipe));
            Assert.Equal(1, selected.Choices[1].Selected);
            Assert.Equal(0, selected.Choices[2].Selected);
        }
    }

    [Fact]
    public void Every_atlas_drawing_honours_the_qualitative_travel_and_origin_contacts()
    {
        var s = new ExplorerSession(); s.Navigate(24);
        var names = Gojo.Experiments.PrimitiveMeasure.StraightCapitals.Names;
        for (var i = 0; i < names.Count; i++)
        {
            s.Mode = i;
            var page = s.Read();
            var graph = Gojo.Experiments.PrimitiveMeasure.StraightCapitals.Build(names[i]);
            var points = new Dictionary<int,(double X,double Y)>();
            for (var r = 0; r < graph.RunCount; r++)
            {
                var sites = graph.Run(r).Sites();
                var lines = page.Drawing.Where(d => d.Kind == "stroke" && d.Key == "run" + r).ToArray();
                Assert.Equal(sites.Length-1, lines.Length);
                for (var k = 0; k < lines.Length; k++)
                {
                    var line = lines[k];
                    static int Word(double change) => change == 0 ? 0 : change > 0 ? 1 : 3;
                    Assert.Equal(new[] { Word(line.X2-line.X), Word(line.Y-line.Y2) }, graph.Frame!.Motion(r));
                    points[sites[k]] = (line.X,line.Y); points[sites[k+1]] = (line.X2,line.Y2);
                }
            }
            var zero = graph.Frame!.ZeroLinks().Select(row => (row[0],row[1])).ToHashSet();
            foreach (var (site, pt) in points)
            {
                Assert.Equal(pt.X == points.Values.Min(p => p.X), zero.Contains((site,0)));
                Assert.Equal(pt.Y == points.Values.Max(p => p.Y), zero.Contains((site,1)));
            }
            foreach (var crossing in graph.ProjectedCrossings()) Assert.Equal(points[crossing[0]], points[crossing[1]]);
        }
    }

    [Fact]
    public void Rotated_N_and_upright_Z_have_the_same_ink_and_both_candidates_without_ground()
    {
        var s = new ExplorerSession(); s.Navigate(22); s.Mode = 3;
        var original = s.Read();
        s.Variant = 1;
        var rotated = s.Read();
        s.Mode = 4; s.Variant = 0;
        var z = s.Read();
        Assert.Equal("How to draw N upright", original.DrawingGuide!.Value.Title);
        Assert.Equal(original.DrawingGuide, rotated.DrawingGuide);
        Assert.Equal("How to draw Z upright", z.DrawingGuide!.Value.Title);
        static (double,double,double,double)[] Geometry(ExplorerPage p) => p.Drawing.Where(d => d.Kind == "stroke")
            .Select(d => (d.X,d.Y,d.X2,d.Y2)).ToArray();
        Assert.Equal(Geometry(rotated), Geometry(z)); Assert.NotEqual(Geometry(original), Geometry(rotated));
        Assert.Equal(original.Blocks.SelectMany(b => b.Bytes), rotated.Blocks.SelectMany(b => b.Bytes));
        Assert.Equal("Compatible among H, A, E, N and Z: N or Z.", z.Result);
        Assert.Equal(rotated.Result, z.Result);
        s.Control = 0;
        Assert.Equal("Compatible among H, A, E, N and Z: Z.", s.Read().Result);
    }
    [Fact]
    public void Closing_guard_holds_actual_flow_but_does_not_erase_topological_closure()
    {
        var session = new ExplorerSession(); session.Navigate(16); session.Control = 2; session.Steps = 6;
        var page = session.Read();
        Assert.Equal(new byte[] { 4 }, page.Blocks.Single(b => b.Title == "Actual activation and guard bits").Bytes);
        Assert.Contains("unchanged", page.Events.Last());
        session.Control = 1; session.Steps = 3;
        Assert.Equal(new byte[] { 9 }, session.Read().Blocks.Single(b => b.Title == "Actual activation and guard bits").Bytes);
        session.Mode = 1;
        Assert.StartsWith("Contradiction", session.Read().Result);
    }

    [Fact]
    public void Every_exposed_choice_and_step_has_valid_inspectable_bits()
    {
        var session = new ExplorerSession();
        for (var chapter = 0; chapter < ExplorerSession.Chapters.Length; chapter++)
        {
            session.Navigate(chapter);
            var counts = session.Read().Choices.Select(c => c.Values.Length).ToArray();
            void Visit(int depth)
            {
                if (depth == counts.Length)
                {
                    foreach (var steps in new[] { 0, 1, 3, 12, 36 })
                    {
                        session.Steps = steps;
                        var page = session.Read();
                        Assert.NotEmpty(page.Result); Assert.NotEmpty(page.Boundary); Assert.NotEmpty(page.Drawing);
                        foreach (var block in page.Blocks)
                        {
                            Assert.InRange(block.UsedBits, 0, block.Bytes.Length * 8);
                            foreach (var mark in block.Marks)
                            {
                                Assert.InRange(mark.Offset, 0, block.UsedBits);
                                Assert.InRange(mark.Width, 0, block.UsedBits - mark.Offset);
                            }
                            for (var bit = block.UsedBits; bit < block.Bytes.Length * 8; bit++) Assert.Equal(0, (block.Bytes[bit / 8] >> (bit % 8)) & 1);
                        }
                    }
                    return;
                }
                for (var choice = 0; choice < counts[depth]; choice++) { session.Read().Choices[depth].Change(choice); Visit(depth + 1); }
            }
            Visit(0);
        }
    }

    [Fact]
    public void Uncertain_conditional_preserves_actual_storage_and_both_predicted_routes()
    {
        var session = new ExplorerSession(); session.Navigate(9); session.Control = 2; session.Steps = 1;
        var page = session.Read();
        Assert.Equal(new byte[] { 3 }, page.Blocks.Single(b => b.Title == "Actual value array").Bytes);
        var prediction = BitConverter.ToUInt32(page.Blocks.Single(b => b.Title.StartsWith("Joint prediction")).Bytes);
        Assert.Equal((1u << 6) | (1u << 8), prediction);
        Assert.Contains("refused", page.Result);
    }

    [Theory]
    [InlineData(0, 1, 0, true)]
    [InlineData(2, 1, 0, false)]
    [InlineData(0, 2, 0, false)]
    [InlineData(0, 0, 0, false)]
    [InlineData(0, 1, 1, false)]
    public void Shape_drawing_cannot_override_missing_rectangle_evidence(int mode, int control, int seam, bool expected)
    {
        var session = new ExplorerSession(); session.Navigate(11); session.Mode = mode; session.Control = control; session.Variant = seam;
        Assert.Equal(expected, session.Read().Result.StartsWith("Rectangle guaranteed"));
    }

    [Fact]
    public void Shared_reach_highlights_both_matching_fields_but_not_the_other_pair()
    {
        var session = new ExplorerSession(); session.Navigate(11); var page = session.Read();
        var marks = page.Blocks.Take(4).Select(b => b.Marks.Where(m => m.Keys.Contains("reach4")).ToArray()).ToArray();
        Assert.Single(marks[0]); Assert.Empty(marks[1]); Assert.Single(marks[2]); Assert.Empty(marks[3]);
        Assert.All(new[] { marks[0][0], marks[2][0] }, m => { Assert.Equal(6, m.Offset); Assert.Equal(3, m.Width); });
    }

    [Fact]
    public void Opening_contour_seam_removes_circle_and_polygon_guarantees()
    {
        var session = new ExplorerSession(); session.Navigate(12); session.Mode = 3;
        Assert.StartsWith("Regular 5-gon", session.Read().Result);
        session.Variant = 1;
        Assert.StartsWith("No guaranteed", session.Read().Result);
        Assert.DoesNotContain(session.Read().Blocks, b => b.Title == "Seam binding");
    }

    [Fact]
    public void Repetition_uses_kernel_trace_and_waiting_composition_retains_actual_bindings()
    {
        var session = new ExplorerSession(); session.Navigate(13); session.Mode = 1; session.Steps = 10;
        Assert.Contains("A@0 → B@1 → A@2 → B@3", session.Read().Result);
        Assert.Contains("DONE", session.Read().Events.Last());
        session.Variant = 4; session.Steps = 20;
        Assert.Contains("unresolved, held", session.Read().Events.Last());
        session.Navigate(14); session.Steps = 33;
        Assert.Equal(new byte[] { 0, 1, 1, 2, 2, 3 }, session.Read().Blocks.Single(b => b.Title.StartsWith("Retained instance")).Bytes);
    }
}
