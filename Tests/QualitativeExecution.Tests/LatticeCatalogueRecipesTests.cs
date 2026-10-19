using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeFrieze;

namespace Gojo.Experiments.QualitativeExecution.Tests;

public sealed class LatticeCatalogueRecipesTests
{
    private static readonly Lazy<LatticeCatalogueRecipes> Study = new(() => new(2, 2));

    [Fact]
    public void OneHundredNativeConstructionsExecuteActualCurrentUnitsAndContracts()
    {
        var study = Study.Value;
        Assert.Equal(100, study.Cases.Count);
        Assert.Equal(Enumerable.Range(1, 100).Select(index => $"C{index:000}"), study.Cases.Select(item => item.Id));
        Assert.Equal(20, study.Cases.Select(item => item.Family).Distinct().Count());
        Assert.All(study.Cases.GroupBy(item => item.Family), group => Assert.Equal(5, group.Count()));
        foreach (var item in study.Cases)
        {
            var stage = item.Current; var recipe = stage.Recipe;
            Assert.Same(stage.Ladder.Current.Program.Surface, Assert.Single(E(recipe.Program.Motifs)));
            Assert.True(stage.Ladder.Current.IsComplete, item.Id);
            Assert.True(QualitativeLatticeLadder.TryRead(recipe.Frame, stage.Ladder.Surface, out var native), item.Id);
            Assert.Same(stage.Ladder.Current.Program.Surface, native.Current.Program.Surface);
            var run = recipe.Program.Generate(recipe.Bindings, recipe.Window);
            Assert.True(run.IsComplete, item.Id); Assert.Equal(4, run.Units.Count);
            var field = new OrnamentProgramField(run, recipe.SeamContract);
            Assert.True(field.IsComplete, item.Id);
            Assert.Empty(E(field.Blocked)); Assert.Empty(E(field.Unresolved));
            Assert.All(Reachable(new Relation(stage.Ladder.Surface, new Relation(recipe.Program.Surface, recipe.SeamContract))),
                element => Assert.True(element is Relation or Placeholder, item.Id));
        }
    }

    [Fact]
    public void WithdrawalRemovesActualPathsAndRolesWithoutRewritingEarlierRungs()
    {
        foreach (var item in Study.Value.Cases)
        {
            var releaseIndex = item.Stages.ToList().FindIndex(stage => stage.Name == "Release the old boundary constraints");
            Assert.True(releaseIndex > 0, item.Id);
            var release = item.Stages[releaseIndex]; var source = item.Stages[releaseIndex - 1];
            Assert.Equal("Release the old boundary constraints", release.Name);
            Assert.Same(source.Ladder.Current.Program, release.Ladder.Current.Source);
            var selected = E(release.Ladder.Current.Focus);
            Assert.NotEmpty(selected);
            Assert.Single(E(release.Ladder.Current.Program.Paths));
            if (E(source.Ladder.Current.Program.Paths).Length > 1) Assert.Single(E(release.Ladder.Current.Carried));
            else
            {
                Assert.Empty(E(release.Ladder.Current.Carried));
                Assert.Empty(E(release.Ladder.Current.Program.ClosedPaths));
                var before = (Relation)E(source.Ladder.Current.Program.Paths).Single();
                var after = (Relation)E(release.Ladder.Current.Program.Paths).Single();
                Assert.Equal(E((Relation)before.Right).Length - 1, E((Relation)after.Right).Length);
            }
            Assert.All(selected, path =>
            {
                Assert.Contains(E(source.Ladder.Current.Program.Paths), actual => ReferenceEquals(path, actual));
                Assert.DoesNotContain(E(release.Ladder.Current.Program.Paths), actual => ReferenceEquals(path, actual));
            });
            var portCorrespondence = E((Relation)release.Ladder.Current.Correspondence.Right).Cast<Relation>().ToArray();
            Assert.Contains(portCorrespondence, row => E((Relation)row.Right).Length == 0);
            Assert.True(item.Current.Ladder.TryReadAncestor(source.Ladder.Current.Program.Surface, out var retained, out _));
            Assert.Same(source.Ladder.Current.Program, retained);
        }
    }

    [Fact]
    public void FineCompartmentsRetainActualCoarseEnvelopesAsQueryableAncestors()
    {
        var examples = Study.Value.Cases.Where(item => item.Stages.Any(stage => stage.Name == "Establish the coarse compartment")).ToArray();
        Assert.Equal(20, examples.Length);
        foreach (var item in examples)
        {
            var envelope = item.Stages.Single(stage => stage.Name == "Establish the coarse compartment");
            Assert.Single(E(envelope.Ladder.Current.Program.ClosedPaths));
            Assert.True(item.Current.Ladder.TryReadAncestor(envelope.Ladder.Current.Program.Surface, out var answer, out _));
            Assert.Same(envelope.Ladder.Current.Program, answer);
            Assert.Same(envelope.Ladder.Current.Program, item.Current.Ladder.Current.Source);
            if (item.Family is "Nested diamond chambers" or "Six-way chamber fans")
            {
                var actualPath = Assert.Single(E(answer.Paths));
                Assert.Contains(E(item.Current.Ladder.Current.Carried), path => ReferenceEquals(path, actualPath));
                Assert.Contains(E(item.Current.Ladder.Current.Program.Paths), path => ReferenceEquals(path, actualPath));
                Assert.All(E(answer.Ports), port => Assert.Contains(E(item.Current.Ladder.Current.Program.Ports), actual => ReferenceEquals(port, actual)));
            }
        }
    }

    [Fact]
    public void BoxReadReplacesIndependentSidesWithClosedParticipantsAndKeepsTheOldRead()
    {
        var item = Study.Value.Cases[0];
        var old = item.Stages[0].Ladder.Current.Program;
        var current = item.Current.Ladder.Current.Program;
        Assert.Equal(4, E(old.Paths).Length); Assert.Empty(E(old.ClosedPaths));
        Assert.Equal(2, E(current.Paths).Length); Assert.Equal(2, E(current.ClosedPaths).Length);
        Assert.All(E(current.ClosedPaths), path => Assert.Contains(E(current.Paths), actual => ReferenceEquals(path, actual)));
        Assert.Empty(E(item.Current.Ladder.Current.Carried));
        Assert.True(item.Current.Ladder.TryReadAncestor(old.Surface, out var answer, out _)); Assert.Same(old, answer);
        Assert.False(item.Stages[0].Ladder.TryReadAncestor(current.Surface, out _, out _));
    }

    [Fact]
    public void EveryVisiblePathHasNonzeroChordsAndNoImmediateCollinearRetrace()
    {
        foreach (var item in Study.Value.Cases)
        {
            var recipe = item.Current.Recipe;
            foreach (var path in E(item.Current.Ladder.Current.Program.Paths).Cast<Relation>())
            {
                var directions = E((Relation)path.Right).Cast<Relation>().Select(word => Vector(recipe.Frame, word)).ToArray();
                Assert.All(directions, vector => Assert.True(vector != (0, 0), item.Id));
                for (var index = 1; index < directions.Length; index++)
                {
                    var first = directions[index - 1]; var second = directions[index];
                    var cross = (long)first.X * second.Y - (long)first.Y * second.X;
                    var dot = (long)first.X * second.X + (long)first.Y * second.Y;
                    Assert.True(cross != 0 || dot >= 0, $"{item.Id} retraces a just-drawn chord.");
                }
            }
        }
    }

    [Fact]
    public void PrototypeAssembliesRetainRealTransformAndTranslationInstructions()
    {
        foreach (var item in Study.Value.Cases.Where(item => item.Family is "Herringbone assemblies" or "Basket rectangle assemblies" or "Detached returned frets"))
        {
            Assert.IsType<Relation>(item.Current.Evidence);
            var current = item.Current.Ladder.Current.Program;
            var evidence = ((Relation)((Relation)current.Evidence).Right).Right;
            Assert.Contains(Reachable(evidence), element => element is Relation relation &&
                ReferenceEquals(relation.Left, current.Frame.Surface) && ReferenceEquals(relation.Right, current.Frame.Surface));
        }
    }

    private static (int X, int Y) Vector(QualitativeMotionFrame frame, Relation word)
    {
        var x = 0; var y = 0;
        foreach (var move in E(word).Cast<Relation>())
        {
            Assert.True(frame.Direction(move, out var axis, out var forward, out _));
            if (ReferenceEquals(axis, frame.Horizontal)) x += forward ? 1 : -1;
            else y += forward ? 1 : -1;
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
