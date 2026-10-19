using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeFrieze;

namespace Gojo.Experiments.QualitativeExecution.Tests;

public sealed class LatticeTransformationRecipesTests
{
    private static readonly Lazy<LatticeTransformationRecipes> Study = new(() => new(2, 2));

    [Fact]
    public void OneHundredActualGeometrySuccessorsKeepNativeBaseAndFiniteExecution()
    {
        var study = Study.Value;
        Assert.Equal(100, study.Cases.Count);
        Assert.Equal(Enumerable.Range(1, 100).Select(index => $"T{index:000}"), study.Cases.Select(item => item.Id));
        var fingerprints = new HashSet<string>();
        foreach (var example in study.Cases)
        {
            var stage = example.Current; var recipe = stage.Recipe;
            Assert.True(stage.Ladder.Current.IsComplete, example.Id);
            Assert.Same(stage.Ladder.Current.Program.Surface, Assert.Single(E(recipe.Program.Motifs)));
            Assert.True(QualitativeLatticeLadder.TryRead(recipe.Frame, stage.Ladder.Surface, out var native), example.Id);
            Assert.Same(stage.Ladder.Current.Program.Surface, native.Current.Program.Surface);
            var baseline = example.Stages[study.BasisStageIndices[example.Id]];
            Assert.Equal("Expose fine surgery landmarks", baseline.Name);
            Assert.True(stage.Ladder.TryReadAncestor(baseline.Ladder.Current.Program.Surface, out var actual, out _), example.Id);
            Assert.Same(baseline.Ladder.Current.Program.Surface, actual.Surface);
            var run = recipe.Program.Generate(recipe.Bindings, recipe.Window);
            Assert.True(run.IsComplete, example.Id); Assert.Equal(4, run.Units.Count);
            var field = new OrnamentProgramField(run, recipe.SeamContract);
            Assert.True(field.IsComplete, example.Id);
            var quality = LatticeCatalogueAssessment.Read(stage.Ladder.Current.Program, run, field).Summary;
            Assert.True(quality.IsStructurallyAdmissible, example.Id);
            Assert.Equal(0, quality.GeometricOverlaps);
            Assert.NotNull(quality.Fingerprint);
            Assert.True(fingerprints.Add(quality.Fingerprint), example.Id + " duplicates an earlier normalized finite field.");
            Assert.All(Reachable(new Relation(stage.Ladder.Surface, recipe.Program.Surface)),
                element => Assert.True(element is Relation or Placeholder, example.Id));
        }
    }

    [Fact]
    public void FineBasisPrecedesRealEndpointConservingProfilesInEveryCase()
    {
        var study = Study.Value;
        foreach (var example in study.Cases)
        {
            var index = study.BasisStageIndices[example.Id];
            var basis = example.Stages[index];
            var refinementIndex = example.Stages.ToList().FindIndex(index + 1, stage => LatticeCatalogueStepRead.Read(stage.Ladder.Current).Kind == "endpoint refinement");
            Assert.True(refinementIndex > index, example.Id);
            var refinement = example.Stages[refinementIndex];
            var actualSource = refinement.Ladder.Current.Source!;
            Assert.Same(example.Stages[refinementIndex - 1].Ladder.Current.Program.Surface, actualSource.Surface);
            var rows = E(refinement.Ladder.Current.Replacement).Cast<Relation>().ToArray();
            Assert.NotEmpty(rows);
            var oldWords = E(actualSource.Paths).Cast<Relation>().SelectMany(path => E((Relation)path.Right)).ToArray();
            Assert.All(rows, row =>
            {
                Assert.Contains(oldWords, actual => ReferenceEquals(actual, row.Left));
                Assert.True(E((Relation)row.Right).Length >= 2, example.Id);
            });
            Assert.True(E(refinement.Ladder.Current.Program.Paths).Cast<Relation>().Sum(path => E((Relation)path.Right).Length) >
                E(actualSource.Paths).Cast<Relation>().Sum(path => E((Relation)path.Right).Length), example.Id);
        }
    }

    [Fact]
    public void SubtractionAndSharedRunsAreActualLaterConstructionStages()
    {
        var examples = Study.Value.Cases;
        Assert.Equal(40, examples.Count(example => example.Stages.Any(stage => stage.Name is
            "Converge routes through a shared spine" or "Open a return where no parallel pair is available" or
            "Withdraw a divider or open a boundary window" or "Open an alternate chamber return")));
        foreach (var example in examples.Where(example => example.Stages.Any(stage => stage.Name == "Converge routes through a shared spine")))
        {
            var fusion = example.Stages.Single(stage => stage.Name == "Converge routes through a shared spine");
            Assert.Equal(2, E(fusion.Ladder.Current.Focus).Length);
            Assert.NotEmpty(E((Relation)fusion.Ladder.Current.Replacement.Left));
            Assert.True(fusion.Ladder.Current.IsComplete, example.Id);
        }
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
