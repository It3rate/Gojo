using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeFrieze;

namespace Gojo.Experiments.QualitativeExecution.Tests;

public sealed class BourgoinColourLibraryTests
{
    private static readonly Lazy<IReadOnlyList<BourgoinColourLibrary.Scene>> Scenes = new(BourgoinColourLibrary.Create);

    [Theory]
    [InlineData("C01", 19, 60, 7)]
    [InlineData("C02", 6, 32, 6)]
    [InlineData("C03", 13, 56, 9)]
    [InlineData("C04", 6, 22, 2)]
    public void AColourSceneTransportsItsThreeNativeLayersAndExecutesWithoutTheSceneFactory(string id, int pathCount, int chordCount, int closedCount)
    {
        var scene = Scenes.Value.Single(scene => scene.Id == id); Assert.True(scene.Enrichment.IsComplete);
        var source = scene.Source; var originalMotif = Assert.IsType<Relation>(Assert.Single(E(source.Motifs)));
        Assert.Same(originalMotif, scene.Enrichment.SourceSurface);
        var adapter = new OrnamentEnrichmentProgram(source.Program, source.Bindings, source.SeamContract, scene.Enrichment);
        var initial = (Relation)source.Window.Left;
        var window = OrnamentProgram.Window(initial.Left, initial.Right, 1, 1);
        Assert.True(OrnamentFieldPacket.TryLoad(adapter.Save(window), out var packet, out var pending)); Assert.Empty(E(pending));
        var actual = Assert.IsType<Relation>(Assert.Single(E(packet.Program.Motifs)));
        Assert.True(OrnamentEnrichment.TryReadEnrichment(packet.Program.Frame, actual, out var decoded)); Assert.True(decoded.IsComplete);
        Assert.Equal(pathCount, E(decoded.Result.Paths).Length); Assert.Equal(closedCount, E(decoded.Result.ClosedPaths).Length);
        Assert.Equal(chordCount, E(decoded.Result.Paths).Cast<Relation>().Sum(path => E((Relation)path.Right).Length));
        Assert.Equal(3, E(decoded.Layers).Length);
        foreach (var layer in E(decoded.Layers).Cast<Relation>())
        {
            Assert.True(decoded.TryReadLayer(layer, out var definition, out _, out _, out _, out _));
            var focus = decoded.Focus(definition.Left); Assert.True(focus.IsComplete);
            Assert.Same(layer, Assert.Single(E(focus.Selected))); Assert.Equal(2, E(focus.Complement).Length);
        }
        var field = new OrnamentProgramField(packet.Program.Generate(packet.Bindings, packet.Window), packet.SeamContract);
        Assert.True(field.IsComplete); Assert.Empty(E(field.Blocked)); Assert.Empty(E(field.Unresolved));
        Assert.Equal(chordCount, E(field.Graph.Strokes).Length);
        AssertNoNumeric(packet.Source); AssertNoNumeric(decoded.Surface);
    }

    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
    private static void AssertNoNumeric(IElement source)
    {
        var visited = new HashSet<IElement>(ReferenceEqualityComparer.Instance); var pending = new Stack<IElement>(); pending.Push(source);
        while (pending.TryPop(out var element))
        {
            if (!visited.Add(element)) continue;
            Assert.False(element is Carrier or CarrierLocation);
            if (element is Relation relation) { pending.Push(relation.Left); pending.Push(relation.Right); }
        }
    }
}
