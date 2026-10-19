using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeFrieze;

namespace Gojo.Experiments.QualitativeExecution.Tests;

public sealed class LatticeLadderRecipesTests
{
    private static readonly Lazy<LatticeLadderRecipes> Study = new(() => new(2, 2));

    [Fact]
    public void EveryConstructionCompilesItsActualRungIntoAnExecutableFiniteField()
    {
        var study = Study.Value;
        Assert.Equal(19, study.Cases.Count);
        foreach (var example in study.Cases)
        {
            var recipe = example.Recipe;
            Assert.Same(example.Unit.Surface, Assert.Single(E(recipe.Program.Motifs)));
            Assert.True(example.Ladder.Current.IsComplete, example.Id);
            Assert.True(QualitativeLatticeLadder.TryRead(example.Unit.Frame, example.Ladder.Surface, out var native), example.Id);
            Assert.Same(example.Unit.Surface, native.Current.Program.Surface);
            var run = recipe.Program.Generate(recipe.Bindings, recipe.Window);
            Assert.True(run.IsComplete, example.Id); Assert.Equal(4, run.Units.Count);
            var field = new OrnamentProgramField(run, recipe.SeamContract);
            Assert.True(field.IsComplete, example.Id);
            Assert.NotEmpty(E(field.Accepted)); Assert.NotEmpty(E(field.Frontier));
            Assert.True(field.Join.TryReadPassed(field.Join.Evaluate()), example.Id);
            Assert.All(Reachable(new Relation(recipe.Program.Surface, recipe.SeamContract)),
                element => Assert.True(element is Relation or Placeholder, example.Id));
            if (example.ParentId is { } parentId)
            {
                var parent = study.Cases.Single(candidate => candidate.Id == parentId);
                Assert.Same(parent.Unit, example.Ladder.Current.Source);
                Assert.True(example.Ladder.TryReadAncestor(parent.Unit.Surface, out var ancestor, out _));
                Assert.Same(parent.Unit, ancestor);
            }
        }
    }

    [Fact]
    public void ObliqueAndStretchCasesChangeActualWordsAndPeriodsInOneDisplayBasis()
    {
        foreach (var pair in new[] { ("L00", "L01"), ("L03", "L04"), ("L00", "L05"), ("L07", "L08"), ("L09", "L10") })
        {
            var source = Case(pair.Item1); var result = Case(pair.Item2);
            Assert.Equal(source.Recipe.DisplayBasis, result.Recipe.DisplayBasis);
            Assert.NotSame(source.Unit.Paths, result.Unit.Paths);
            Assert.NotEmpty(E(Assert.IsType<Relation>(result.Ladder.Current.Correspondence.Left)));
            Assert.Same(source.Unit.Surface, ((Relation)result.Unit.Evidence).Left);
            var sourceWords = E(source.Unit.Paths).Cast<Relation>().SelectMany(path => E((Relation)path.Right));
            var targetWords = E(result.Unit.Paths).Cast<Relation>().SelectMany(path => E((Relation)path.Right));
            Assert.True(sourceWords.Zip(targetWords).Any(words =>
                !SamePosition(result.Unit.Frame, (Relation)words.First, (Relation)words.Second)), result.Id);
            Assert.True(!SamePosition(result.Unit.Frame, source.Recipe.ColumnPitch, result.Recipe.ColumnPitch) ||
                !SamePosition(result.Unit.Frame, source.Recipe.RowPitch, result.Recipe.RowPitch), result.Id);
        }
    }

    [Fact]
    public void ThirdTiesChamfersAndBasketAreVisibleTopologyEdits()
    {
        var oblique = Case("L05"); var triangle = Case("L06");
        Assert.Equal(E(oblique.Unit.Paths).Length + 1, E(triangle.Unit.Paths).Length);
        Assert.All(E(oblique.Unit.Paths), path => Assert.Contains(E(triangle.Unit.Paths), actual => ReferenceEquals(path, actual)));
        Assert.NotEmpty(E(triangle.Ladder.Current.Carried));

        var insets = Case("L11"); var chamfer = Case("L12");
        Assert.Equal(4, E(insets.Unit.Paths).Length); Assert.Single(E(chamfer.Unit.Paths));
        Assert.Single(E(chamfer.Unit.ClosedPaths));
        Assert.Equal(E(insets.Unit.Paths).Length, E(chamfer.Ladder.Current.Focus).Length);
        Assert.Empty(E(chamfer.Ladder.Current.Carried));
        // Eight straight side portions are exact retained old chord words;
        // four new diagonal bridge words expose the corner replacement.
        var oldWords = E(insets.Unit.Paths).Cast<Relation>().SelectMany(path => E((Relation)path.Right)).ToArray();
        var newWords = E((Relation)((Relation)Assert.Single(E(chamfer.Unit.Paths))).Right);
        Assert.Equal(8, newWords.Count(word => oldWords.Any(old => ReferenceEquals(word, old))));
        Assert.Equal(4, newWords.Count(word => !oldWords.Any(old => ReferenceEquals(word, old))));

        var basket = Case("L14"); var brick = Case("L03");
        Assert.Equal(16, E(basket.Unit.Paths).Length);
        Assert.False(basket.Ladder.TryReadAncestor(brick.Unit.Surface, out _, out _));
        Assert.True(basket.Ladder.TryReadAncestor(Case("L01").Unit.Surface, out _, out _));
        Assert.Same(Case("L13").Unit.Surface, Assert.IsType<Relation>(basket.Unit.Evidence).Left);
    }

    [Fact]
    public void RealRolesWithWrongNeighbourDisplacementStayBlocked()
    {
        var square = Case("L00").Recipe;
        var run = square.Program.Generate(square.Bindings, square.Window);
        var blocked = new OrnamentProgramField(run, Study.Value.BlockedSeamContract);
        Assert.False(blocked.IsComplete); Assert.NotEmpty(E(blocked.Blocked));
        Assert.Empty(E(blocked.Accepted)); Assert.Empty(E(blocked.Unresolved));
        Assert.Equal(E(blocked.SourceGraph.Strokes).Length, E(blocked.Graph.Strokes).Length);
        Assert.NotEmpty(E(blocked.Frontier));
        var selectedRole = Assert.IsType<Relation>(E(Case("L00").Unit.Ports)[0]).Left;
        Assert.All(E(blocked.Blocked), evidence => Assert.Contains(Reachable(evidence),
            element => ReferenceEquals(element, selectedRole)));
    }

    [Fact]
    public void CrystallizedWholeFieldHasIndependentNativeContractsAndNoAncestry()
    {
        var source = Case("L14");
        var crystallized = source.Ladder.Crystallize("standalone basket");
        var standalone = crystallized.Standalone;
        var rebuilt = LatticeLadderAuthor.Rebuild(source.Recipe, standalone.Current.Program, 2, 2);
        Assert.False(standalone.TryReadAncestor(Case("L00").Unit.Surface, out _, out _));
        Assert.Empty(E(Assert.IsType<Relation>(standalone.Current.Program.Evidence)));
        var oldWorld = Reachable(new Relation(source.Recipe.Program.Surface, source.Recipe.SeamContract)).ToHashSet(ReferenceEqualityComparer.Instance);
        Assert.DoesNotContain(Reachable(new Relation(rebuilt.Program.Surface, rebuilt.SeamContract)), oldWorld.Contains);
        var beforeRun = source.Recipe.Program.Generate(source.Recipe.Bindings, source.Recipe.Window);
        var before = new OrnamentProgramField(beforeRun, source.Recipe.SeamContract);
        var run = rebuilt.Program.Generate(rebuilt.Bindings, rebuilt.Window);
        var after = new OrnamentProgramField(run, rebuilt.SeamContract);
        Assert.True(run.IsComplete); Assert.True(after.IsComplete);
        Assert.Equal(E(before.Graph.Sites).Length, E(after.Graph.Sites).Length);
        Assert.Equal(E(before.Graph.Strokes).Length, E(after.Graph.Strokes).Length);
        Assert.Equal(E(before.Accepted).Length, E(after.Accepted).Length);
        Assert.NotEmpty(E(crystallized.Forgotten));
        Assert.All(Reachable(new Relation(rebuilt.Program.Surface, rebuilt.SeamContract)),
            element => Assert.True(element is Relation or Placeholder));

        var encoded = OrnamentFieldPacket.Save(rebuilt.Program, rebuilt.Bindings, rebuilt.Window, rebuilt.SeamContract);
        Assert.True(OrnamentFieldPacket.TryLoad(encoded, out var imported, out var pending)); Assert.Empty(E(pending));
        var replay = imported.Program.Generate(imported.Bindings, imported.Window);
        var replayField = new OrnamentProgramField(replay, imported.SeamContract);
        Assert.True(replay.IsComplete); Assert.True(replayField.IsComplete);
        Assert.Equal(E(after.Graph.Strokes).Length, E(replayField.Graph.Strokes).Length);
    }

    private static LatticeLadderCase Case(string id) => Study.Value.Cases.Single(example => example.Id == id);
    private static bool SamePosition(QualitativeMotionFrame frame, Relation a, Relation b)
    {
        var inverse = S(E(b).Reverse().Cast<Relation>().Select(move => (IElement)QualitativeMotionFrame.Move(
            E(frame.Opposites).Cast<Relation>().Single(row => ReferenceEquals(row.Left, move.Left)).Right, move.Right)).ToArray());
        return frame.TryReadResult(frame.Reduce(a, inverse), out var result, out _, out var pending) &&
            E(result).Length == 0 && E(pending).Length == 0;
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
    private static Relation S(params IElement[] entries) => StructuralConstraint.Closed(entries);
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
}
