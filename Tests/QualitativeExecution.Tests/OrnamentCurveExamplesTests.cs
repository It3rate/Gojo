using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeFrieze;

namespace Gojo.Experiments.QualitativeExecution.Tests;

public sealed class OrnamentCurveExamplesTests
{
    [Fact]
    public void ExamplesKeepTheActualSourceGuidesLeafTipsAndClaimedBranchLandmarks()
    {
        var examples = OrnamentCurveExamples.Create();
        Assert.Equal(new[] { "C-D02", "C-D07", "C-D09" }, examples.Select(example => example.Design.Id));
        foreach (var example in examples)
        {
            var design = example.Design; var enrichment = design.Enrichment;
            Assert.Equal(design.Id[2..], design.SourceId);
            var hint = Assert.IsType<Relation>(Assert.Single(E(example.CurveHints)));
            Assert.True(OrnamentCurveSelection.TryRead(design.Source.Frame, enrichment.ResultSurface, hint, out var selection));
            var sourcePaths = E(enrichment.Source.Paths).Cast<Relation>().ToArray();
            var selected = E(selection.Selected).Cast<Relation>().ToArray();
            Assert.Equal(design.SourceId == "D09" ? 3 : 4, selected.Length);
            Assert.True(selection.Contains(sourcePaths[0]));
            if (design.SourceId == "D07") Assert.True(selection.Contains(sourcePaths[^1]));
            if (design.SourceId != "D09")
            {
                foreach (var leaf in selected.Where(path => !sourcePaths.Any(actual => ReferenceEquals(actual, path))))
                {
                    var words = E((Relation)leaf.Right);
                    Assert.Contains(words.Length, new[] { 8, 10 });
                    Assert.Contains(words.Length / 2, selection.FixedLandmarks(leaf));
                }
                Assert.Contains(E(selection.FixedPorts).OfType<Relation>(), port =>
                    port.Right is Relation pick && sourcePaths.Any(path => ReferenceEquals(path, pick.Left)));
            }
            Assert.DoesNotContain(selected, path => E((Relation)path.Right).Length == 12);
            Assert.Equal(E(enrichment.Result.Paths).Length,
                selected.Length + E(selection.Complement).Length);
        }
    }

    [Theory]
    [InlineData("C-D02")]
    [InlineData("C-D07")]
    [InlineData("C-D09")]
    public void ImportedHintsShareTheExecutedNativePathsAndRetainTheInterruption(string id)
    {
        var example = OrnamentCurveExamples.Create().Single(example => example.Design.Id == id);
        var design = example.Design; var source = design.Source;
        var installation = new OrnamentEnrichmentProgram(source.Program, source.Bindings, source.SeamContract, design.Enrichment);
        var phase = (Relation)source.Window.Left;
        var json = installation.Save(OrnamentProgram.Window(phase.Left, phase.Right, 1, 1), example.CurveHints);
        Assert.True(OrnamentFieldPacket.TryLoad(json, out var packet, out var pending)); Assert.Empty(E(pending));
        var actual = Assert.IsType<Relation>(Assert.Single(E(packet.Program.Motifs)));
        var hint = Assert.IsType<Relation>(Assert.Single(E(Assert.IsType<Relation>(packet.PresentationHints))));
        Assert.True(OrnamentCurveSelection.TryRead(packet.Program.Frame, actual, hint, out var curves));
        Assert.True(OrnamentEnrichment.TryReadEnrichment(packet.Program.Frame, actual, out var imported));
        foreach (var path in E(curves.Selected))
            Assert.Contains(E(imported.Result.Paths), actualPath => ReferenceEquals(actualPath, path));
        foreach (var port in E(curves.FixedPorts))
            Assert.Contains(E(imported.Result.Ports), actualPort => ReferenceEquals(actualPort, port));
        if (id == "C-D09")
        {
            var gapSurface = Assert.IsType<Relation>(Assert.Single(E(Assert.IsType<Relation>(imported.Source.Evidence))));
            Assert.True(OrnamentStrandGap.TryRead(packet.Program.Frame, gapSurface, out var gap));
            Assert.Contains(E(imported.Source.Ports), port => ReferenceEquals(port, gap.FromPort));
            Assert.Contains(E(imported.Source.Ports), port => ReferenceEquals(port, gap.ToPort));
            Assert.True(curves.Contains(Assert.IsType<Relation>(Assert.IsType<Relation>(gap.FromPort.Right).Left)));
            Assert.True(curves.Contains(Assert.IsType<Relation>(Assert.IsType<Relation>(gap.ToPort.Right).Left)));
        }
        var field = new OrnamentProgramField(packet.Program.Generate(packet.Bindings, packet.Window), packet.SeamContract);
        Assert.True(field.IsComplete); Assert.Empty(E(field.Blocked)); Assert.Empty(E(field.Unresolved));
        Assert.Equal(E(imported.Result.Paths).Cast<Relation>().Sum(path => E((Relation)path.Right).Length),
            E(field.Graph.Strokes).Length);
    }

    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
}
