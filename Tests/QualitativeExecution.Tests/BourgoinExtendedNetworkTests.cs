using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;
using Gojo.Experiments.QualitativeFrieze;

namespace Gojo.Experiments.QualitativeExecution.Tests;

public sealed class BourgoinExtendedNetworkTests
{
    private static readonly Lazy<BourgoinPatternLibrary> Library = new(() => new());

    [Theory]
    [InlineData("B15", 1, 1, 4, 1)]
    [InlineData("B16", 2, 2, 6, 1)]
    [InlineData("B17", 3, 1, 8, 2)]
    [InlineData("B18", 3, 1, 8, 2)]
    [InlineData("B19", 1, 1, 6, 1)]
    [InlineData("B20", 3, 3, 12, 1)]
    [InlineData("B21", 2, 2, 8, 1)]
    [InlineData("B22", 4, 4, 16, 1)]
    [InlineData("B23", 1, 1, 8, 1)]
    [InlineData("B24", 2, 2, 6, 1)]
    public void ActualPathsRetainTheirSourcePrototypeAndSubstitutionReceipts(string id, int paths, int closed, int chords, int prototypes)
    {
        var recipe = Recipe(id); var motif = Motif(recipe);
        Assert.Equal(paths, E(motif.Paths).Length); Assert.Equal(closed, E(motif.Closed).Length);
        Assert.Equal(chords, E(motif.Paths).Cast<Relation>().Sum(path => E((Relation)path.Right).Length));
        var images = Images(motif); Assert.Equal(paths, images.Length);
        Assert.Equal(prototypes, images.Select(image => ((Relation)image.Left).Left).Distinct(ReferenceEqualityComparer.Instance).Count());
        foreach (var image in images)
        {
            var copy = (Relation)image.Left; var source = (Relation)((Relation)copy.Left).Left;
            var target = (Relation)copy.Right; var mapping = (Relation)image.Right;
            var receiptsAndPlacement = (Relation)mapping.Right;
            var receipts = E((Relation)receiptsAndPlacement.Left).Cast<Relation>().ToArray();
            var sourceWords = new[] { (Relation)source.Left }.Concat(E((Relation)source.Right).Cast<Relation>()).ToArray();
            Assert.Equal(sourceWords.Length, receipts.Length);
            for (var index = 0; index < receipts.Length; index++)
            {
                var selected = Assert.IsType<Relation>(receipts[index].Left);
                Assert.Same(mapping.Left, selected.Left); Assert.Same(sourceWords[index], selected.Right);
            }
            var placement = (Relation)((Relation)receiptsAndPlacement.Right).Right;
            Assert.True(recipe.Frame.TryReadResult(placement, out var actualStart, out _, out var pending));
            Assert.Empty(E(pending)); Assert.Same(actualStart, target.Left);
        }
    }

    [Theory]
    [InlineData("B15")]
    [InlineData("B16")]
    [InlineData("B17")]
    [InlineData("B18")]
    [InlineData("B19")]
    [InlineData("B20")]
    [InlineData("B21")]
    [InlineData("B22")]
    [InlineData("B23")]
    [InlineData("B24")]
    public void EachSmallFieldReplaysItsNativeWordsAndExplicitSeams(string id)
    {
        var recipe = Recipe(id); var phases = (Relation)recipe.Window.Left;
        var window = OrnamentProgram.Window(phases.Left, phases.Right, 2, 2);
        var archive = OrnamentFieldPacket.Save(recipe.Program, recipe.Bindings, window, recipe.SeamContract);
        Assert.DoesNotContain(recipe.Name, archive, StringComparison.Ordinal);
        Assert.True(OrnamentFieldPacket.TryLoad(archive, out var imported, out var pending)); Assert.Empty(E(pending));
        Assert.All(Reachable(new Relation(imported.Program.Surface, imported.SeamContract)), item => Assert.True(item is Relation or Placeholder));
        var run = imported.Program.Generate(imported.Bindings, imported.Window);
        var field = new OrnamentProgramField(run, imported.SeamContract);
        Assert.True(run.IsComplete); Assert.True(field.IsComplete); Assert.Equal(4, run.Units.Count);
        Assert.NotEmpty(E(field.Accepted)); Assert.NotEmpty(E(field.Frontier));
        Assert.Empty(E(field.Blocked)); Assert.Empty(E(field.Unresolved));
        Assert.All(run.Units, unit => Assert.Same(run.Units[0].Current.Program.Paths, unit.Current.Program.Paths));
    }

    [Theory]
    [InlineData("B17")]
    [InlineData("B20")]
    [InlineData("B22")]
    [InlineData("B24")]
    public void RemovingTheSeamLawPreservesCoincidentButDistinctNativeSites(string id)
    {
        var recipe = Recipe(id); var phases = (Relation)recipe.Window.Left;
        var run = recipe.Program.Generate(recipe.Bindings, OrnamentProgram.Window(phases.Left, phases.Right, 2, 2));
        var joined = new OrnamentProgramField(run, recipe.SeamContract);
        var separate = new OrnamentProgramField(run, OrnamentProgramField.Contract(recipe.Program));
        Assert.True(joined.IsComplete); Assert.True(separate.IsComplete); Assert.Empty(E(separate.Accepted));
        Assert.Equal(E(joined.Graph.Strokes).Length, E(separate.Graph.Strokes).Length);
        Assert.True(E(joined.Graph.Sites).Length < E(separate.Graph.Sites).Length);
    }

    [Fact]
    public void CentredLozengesRetainTheActualThreePhaseWordReturn()
    {
        var motif = Motif(Recipe("B20")); var images = Images(motif);
        var extra = Assert.IsType<Relation>(Assert.Single(E(motif.Evidence), row => !images.Any(image => ReferenceEquals(image, row))));
        var source = (Relation)extra.Left; var wordsAndCycle = (Relation)source.Right;
        var cycle = (Relation)wordsAndCycle.Right; Assert.Equal(3, E((Relation)cycle.Left).Length); Assert.Equal(3, E((Relation)cycle.Right).Length);
        var result = (Relation)extra.Right; var returns = (Relation)result.Right;
        Assert.Equal(5, E((Relation)returns.Left).Length);
        var mismatch = (Relation)returns.Right; Assert.Empty(E((Relation)mismatch.Left));
        var blocked = (Relation)mismatch.Right; Assert.Empty(E((Relation)blocked.Left)); Assert.Empty(E((Relation)blocked.Right));
        Assert.All(E((Relation)returns.Left).Cast<Relation>(), returned =>
        {
            var witness = (Relation)returned.Right; Assert.Same(cycle, witness.Left);
            Assert.True(Recipe("B20").Frame.TryReadResult((Relation)witness.Right, out var remaining, out _, out var pending));
            Assert.Empty(E(remaining)); Assert.Empty(E(pending));
        });
        // Formal return is not an intrinsic angle or a solid-cube theorem.
    }

    [Fact]
    public void DifferentTriangleDispositionsRetainTheSameThreeSidedConstructionVocabulary()
    {
        var mesh = Recipe("B16"); var gaps = Recipe("B24");
        Assert.Equal((2, 0), Profile(mesh.Frame, mesh.ColumnPitch)); Assert.Equal((0, 2), Profile(mesh.Frame, mesh.RowPitch));
        Assert.Equal((4, 0), Profile(gaps.Frame, gaps.ColumnPitch)); Assert.Equal((0, 4), Profile(gaps.Frame, gaps.RowPitch));
        foreach (var recipe in new[] { mesh, gaps })
        {
            var motif = Motif(recipe); Assert.Equal(2, E(motif.Closed).Length);
            Assert.All(E(motif.Paths).Cast<Relation>(), path => Assert.Equal(3, E((Relation)path.Right).Length));
            var images = Images(motif); Assert.Same(((Relation)images[0].Left).Left, ((Relation)images[1].Left).Left);
        }
    }

    private static BourgoinPatternLibrary.Recipe Recipe(string id) => Library.Value.Recipes.Single(recipe => recipe.Id == id);
    private static (Relation Paths, Relation Closed, Relation Evidence) Motif(BourgoinPatternLibrary.Recipe recipe)
    {
        var motif = (Relation)Assert.Single(E(recipe.Program.Motifs)); var body = (Relation)motif.Right;
        var pathsPorts = (Relation)body.Left; var rest = (Relation)body.Right;
        return ((Relation)pathsPorts.Left, (Relation)rest.Left, (Relation)rest.Right);
    }
    private static Relation[] Images((Relation Paths, Relation Closed, Relation Evidence) motif) => E(motif.Evidence).OfType<Relation>()
        .Where(row => row.Left is Relation copy && E(motif.Paths).Any(path => ReferenceEquals(path, copy.Right))).ToArray();
    private static (int X, int Y) Profile(QualitativeMotionFrame frame, Relation word)
    {
        var read = frame.Reduce(StructuralConstraint.Closed(), word);
        Assert.True(frame.TryReadResult(read, out var remaining, out _, out var pending)); Assert.Empty(E(pending));
        var x = 0; var y = 0;
        foreach (var item in E(remaining).Cast<Relation>())
        {
            Assert.True(frame.Direction(item, out var axis, out var forward, out _));
            if (ReferenceEquals(axis, frame.Horizontal)) x += forward ? 1 : -1; else y += forward ? 1 : -1;
        }
        return (x, y);
    }
    private static IEnumerable<IElement> Reachable(IElement root)
    {
        var seen = new HashSet<IElement>(ReferenceEqualityComparer.Instance); var pending = new Stack<IElement>(); pending.Push(root);
        while (pending.TryPop(out var item))
        {
            if (!seen.Add(item)) continue; yield return item;
            if (item is Relation row) { pending.Push(row.Left); pending.Push(row.Right); }
        }
    }
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
}
