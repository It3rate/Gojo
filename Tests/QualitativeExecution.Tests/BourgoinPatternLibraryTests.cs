using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeFrieze;

namespace Gojo.Experiments.QualitativeExecution.Tests;

public sealed class BourgoinPatternLibraryTests
{
    private static readonly Lazy<BourgoinPatternLibrary> Library = new(() => new());
    [Fact]
    public void AllSourceRecipesReplayTheirPathsAndSeamsFromANativeArchive()
    {
        var recipes = Library.Value.Recipes;
        Assert.Equal(36, recipes.Count);
        Assert.Equal(36, recipes.Select(recipe => recipe.Id).Distinct().Count());
        foreach (var recipe in recipes)
        {
            var saved = OrnamentFieldPacket.Save(recipe.Program, recipe.Bindings, recipe.Window, recipe.SeamContract);
            Assert.DoesNotContain(recipe.Name, saved, StringComparison.Ordinal);
            Assert.True(OrnamentFieldPacket.TryLoad(saved, out var loaded, out var pending), recipe.Id);
            Assert.Empty(E(pending));
            var start = Assert.IsType<Relation>(loaded.Window.Left);
            var window = OrnamentProgram.Window(start.Left, start.Right, 2, 2);
            var run = loaded.Program.Generate(loaded.Bindings, window);
            Assert.True(run.IsComplete, recipe.Id); Assert.Equal(4, run.Units.Count);
            var field = new OrnamentProgramField(run, loaded.SeamContract);
            Assert.True(field.IsComplete, recipe.Id);
            if (recipe.Id == "B09") Assert.Empty(E(field.Accepted));
            else { Assert.NotEmpty(E(field.Accepted)); Assert.NotEmpty(E(field.Frontier)); }
            Assert.True(field.Join.TryReadPassed(field.Join.Evaluate()), recipe.Id);
            Assert.All(run.Units, unit =>
            {
                Assert.True(unit.Current.Join.TryReadPassed(unit.Current.Join.Evaluate()));
                Assert.Single(E(unit.Current.Execution.CompletionEvents));
                Assert.Same(run.Units[0].Current.Program.Paths, unit.Current.Program.Paths);
            });
            Assert.All(Reachable(new Relation(loaded.Program.Surface, loaded.SeamContract)),
                element => Assert.True(element is Relation or Placeholder, recipe.Id));
        }
    }

    [Fact]
    public void DiamondReturnLivesInTheActualTwoRowMacrocell()
    {
        var recipes = Library.Value.Recipes;
        var direct = recipes.Single(recipe => recipe.Id == "B10");
        var returned = recipes.Single(recipe => recipe.Id == "B11");
        var a = direct.Program.Generate(direct.Bindings, SmallWindow(direct));
        var b = returned.Program.Generate(returned.Bindings, SmallWindow(returned));
        Assert.True(a.IsComplete); Assert.True(b.IsComplete);
        Assert.Equal(2, E(a.Units[0].Current.Program.Paths).Length);
        Assert.Equal(4, E(b.Units[0].Current.Program.Paths).Length);
        Assert.Equal(2, E(a.Units[0].Current.Program.ClosedPaths).Length);
        Assert.Equal(4, E(b.Units[0].Current.Program.ClosedPaths).Length);
        Assert.Equal(2 * E(a.Units[0].Current.Graph.Strokes).Length, E(b.Units[0].Current.Graph.Strokes).Length);
        Assert.All(b.Units, unit => Assert.Same(b.Units[0].Current.Program.Paths, unit.Current.Program.Paths));
    }

    [Theory]
    [InlineData("B01")]
    [InlineData("B04")]
    [InlineData("B06")]
    [InlineData("B10")]
    public void NetworkFamiliesNeedCrossCellSeamsEvenWhenTheirLocalPathsAreAlreadyJoined(string id)
    {
        var recipe = Library.Value.Recipes.Single(recipe => recipe.Id == id);
        var start = (Relation)recipe.Window.Left;
        var run = recipe.Program.Generate(recipe.Bindings, OrnamentProgram.Window(start.Left, start.Right, 2, 2));
        var complete = new OrnamentProgramField(run, recipe.SeamContract);
        var localRules = E((Relation)recipe.SeamContract.Right).Cast<Relation>()
            .Where(rule => E((Relation)((Relation)rule.Right).Left).Length == 0).ToArray();
        var localOnly = new OrnamentProgramField(run, OrnamentProgramField.Contract(recipe.Program, localRules));
        Assert.True(complete.IsComplete); Assert.True(localOnly.IsComplete);
        Assert.Equal(1, Components(complete.Graph)); Assert.Equal(4, Components(localOnly.Graph));
        Assert.Equal(E(complete.Graph.Strokes).Length, E(localOnly.Graph.Strokes).Length);
        Assert.True(E(complete.Graph.Sites).Length < E(localOnly.Graph.Sites).Length);
        Assert.True(E(complete.Accepted).Length > E(localOnly.Accepted).Length);
    }

    private static Relation SmallWindow(BourgoinPatternLibrary.Recipe recipe)
    {
        var start = (Relation)recipe.Window.Left;
        return OrnamentProgram.Window(start.Left, start.Right, 2, 2);
    }

    private static int Components(QualitativeStrokeGraph graph)
    {
        var remaining = new HashSet<IElement>(E(graph.Sites), ReferenceEqualityComparer.Instance); var count = 0;
        var adjacency = new Dictionary<IElement, List<IElement>>(ReferenceEqualityComparer.Instance);
        foreach (var site in remaining) adjacency.Add(site, []);
        foreach (var stroke in E(graph.Strokes).Cast<Relation>())
        {
            Assert.True(QualitativeStrokeGraph.TryReadStroke(stroke, out _, out var from, out var to, out _));
            adjacency[from].Add(to); adjacency[to].Add(from);
        }
        while (remaining.Count > 0)
        {
            count++; var pending = new Queue<IElement>(); var first = remaining.First(); remaining.Remove(first); pending.Enqueue(first);
            while (pending.TryDequeue(out var site)) foreach (var neighbour in adjacency[site])
                if (remaining.Remove(neighbour)) pending.Enqueue(neighbour);
        }
        return count;
    }

    private static IEnumerable<IElement> Reachable(IElement root)
    {
        var seen = new HashSet<IElement>(ReferenceEqualityComparer.Instance); var pending = new Stack<IElement>(); pending.Push(root);
        while (pending.TryPop(out var item))
        {
            if (!seen.Add(item)) continue; yield return item;
            if (item is Relation relation) { pending.Push(relation.Left); pending.Push(relation.Right); }
        }
    }
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
}
