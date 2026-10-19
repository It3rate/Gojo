using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeFrieze;

namespace Gojo.Experiments.QualitativeExecution.Tests;

public sealed class BourgoinPatternLibraryTests
{
    [Fact]
    public void AllSourceRecipesReplayTheirPathsAndSeamsFromANativeArchive()
    {
        var recipes = new BourgoinPatternLibrary().Recipes;
        Assert.Equal(14, recipes.Count);
        Assert.Equal(14, recipes.Select(recipe => recipe.Id).Distinct().Count());
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
            Assert.True(field.IsComplete, recipe.Id); Assert.NotEmpty(E(field.Accepted)); Assert.NotEmpty(E(field.Frontier));
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
    public void ReturnedRowsAreSelectedByNativePhaseAndActualSubstitution()
    {
        var recipe = new BourgoinPatternLibrary().Recipes.Single(recipe => recipe.Id == "B11");
        var run = recipe.Program.Generate(recipe.Bindings, recipe.Window);
        Assert.True(run.IsComplete);
        var transforms = E(run.Applications).Cast<Relation>().Select(row => ((Relation)((Relation)row.Right).Left).Right)
            .Distinct(ReferenceEqualityComparer.Instance).ToArray();
        Assert.Equal(2, transforms.Length);
        Assert.All(transforms, transform => Assert.Contains(E(recipe.Program.Transforms), actual => ReferenceEquals(actual, transform)));
        Assert.Same(run.Units[0].Current.Program.Paths, run.Units[4].Current.Program.Paths);
        Assert.NotSame(run.Units[0].Transform, run.Units[4].Transform);
        var field = new OrnamentProgramField(run, recipe.SeamContract);
        Assert.True(field.IsComplete); Assert.NotEmpty(E(field.Inactive)); Assert.NotEmpty(E(field.Accepted));
    }

    [Theory]
    [InlineData("B01")]
    [InlineData("B04")]
    [InlineData("B06")]
    [InlineData("B14")]
    public void NetworkFamiliesNeedCrossCellSeamsEvenWhenTheirLocalPathsAreAlreadyJoined(string id)
    {
        var recipe = new BourgoinPatternLibrary().Recipes.Single(recipe => recipe.Id == id);
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
