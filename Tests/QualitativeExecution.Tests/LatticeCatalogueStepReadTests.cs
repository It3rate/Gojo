using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeFrieze;

namespace Gojo.Experiments.QualitativeExecution.Tests;

public sealed class LatticeCatalogueStepReadTests
{
    [Fact]
    public void InitialReadExposesActualIntroducedPathsWithNoInventedSource()
    {
        var author = new LatticeLadderAuthor(); var path = LatticeLadderAuthor.Path(S(), author.Word(1, 0));
        var program = author.Unit("caption has no semantics", path);
        var rung = QualitativeLatticeLadder.Begin(program).Current;
        var read = LatticeCatalogueStepRead.Read(rung);
        Assert.Equal("initial", read.Kind); Assert.Null(read.Source); Assert.Same(program, read.Result);
        Assert.Same(path, Assert.Single(E(read.IntroducedPaths)));
        Assert.Empty(E(read.CarriedPaths)); Assert.Empty(E(read.SelectedPaths)); Assert.Empty(E(read.WithdrawnPorts));
        Assert.Same(rung.Correspondence, read.Correspondence);
        Assert.Contains(rung.Surface, Reachable(read.Surface)); AssertNoNumeric(read.Surface);
    }

    [Fact]
    public void TransformUsesNativePathPairsAndRetainsOldBasisWithoutMutation()
    {
        var author = new LatticeLadderAuthor(); var right = author.Word(1, 0); var up = author.Word(0, 1);
        var oldPath = LatticeLadderAuthor.Path(S(), right, up);
        var source = author.Unit("endpoint refinement", oldPath);
        var oldWords = E((Relation)oldPath.Right);
        var transform = author.Map(right, author.Word(1, 1));
        var ladder = QualitativeLatticeLadder.Begin(source).Transform("focused path edit", transform);
        var read = LatticeCatalogueStepRead.Read(ladder.Current);
        Assert.Equal("word substitution", read.Kind);
        Assert.Same(source, read.Source); Assert.Same(oldPath, Assert.Single(E(read.SelectedPaths)));
        Assert.Same(E(ladder.Current.Program.Paths).Single(), Assert.Single(E(read.IntroducedPaths)));
        Assert.Empty(E(read.CarriedPaths)); Assert.Empty(E(read.WithdrawnPorts)); Assert.Empty(E(read.Unresolved));
        Assert.Equal(oldWords, E((Relation)oldPath.Right));
        Assert.True(ladder.TryReadAncestor(source.Surface, out var ancestor, out _)); Assert.Same(source, ancestor);
        var pair = Assert.IsType<Relation>(Assert.Single(E(read.PathCorrespondence)));
        Assert.Same(oldPath, pair.Left); Assert.Same(E(read.IntroducedPaths).Single(), pair.Right);
        Assert.Contains(transform.Surface, Reachable(read.Surface)); AssertNoNumeric(read.Surface);
    }

    [Fact]
    public void RefinementFocusSelectsActualWordsAndCarriesUnaffectedPathThroughNativePair()
    {
        var author = new LatticeLadderAuthor(); var right = author.Word(1, 0); var up = author.Word(0, 1);
        var selected = LatticeLadderAuthor.Path(S(), right);
        var carried = LatticeLadderAuthor.Path(S(), up);
        var source = author.Unit("word substitution", selected, carried);
        var chain = S(up, right, author.Word(0, -1));
        var ladder = QualitativeLatticeLadder.Begin(source).Refine("initial", S(new Relation(right, chain)));
        var read = LatticeCatalogueStepRead.Read(ladder.Current);
        Assert.Equal("endpoint refinement", read.Kind);
        Assert.Same(selected, Assert.Single(E(read.SelectedPaths)));
        Assert.Same(carried, Assert.Single(E(read.CarriedPaths)));
        Assert.Same(E(ladder.Current.Program.Paths)[0], Assert.Single(E(read.IntroducedPaths)));
        var currentCarried = Assert.IsType<Relation>(Assert.Single(E(read.CarriedResultPaths)));
        Assert.NotSame(carried, currentCarried); Assert.Same(carried.Left, currentCarried.Left);
        Assert.Same(up, Assert.Single(E((Relation)currentCarried.Right)));
        // Focus itself is path -> selected-word-scope, not a list of paths.
        Assert.DoesNotContain(E(ladder.Current.Focus), row => ReferenceEquals(row, selected));
        Assert.Contains(right, Reachable(ladder.Current.Focus));
        Assert.Empty(E(read.WithdrawnPorts)); Assert.Empty(E(read.Unresolved)); AssertNoNumeric(read.Surface);
    }

    [Fact]
    public void AllFourLinesCanBeWithdrawnAndReformulatedAsOneClosedBox()
    {
        var author = new LatticeLadderAuthor();
        var right = author.Word(1, 0); var up = author.Word(0, 1);
        var left = author.Word(-1, 0); var down = author.Word(0, -1);
        var sides = new[] {
            LatticeLadderAuthor.Path(S(), right), LatticeLadderAuthor.Path(right, up),
            LatticeLadderAuthor.Path(author.Word(1, 1), left), LatticeLadderAuthor.Path(up, down)
        };
        var source = author.Unit("four independent side paths", sides);
        var box = LatticeLadderAuthor.Path(S(), right, up, left, down);
        var ladder = QualitativeLatticeLadder.Begin(source).ReplacePaths("box viewpoint", source.Paths, S(box), S(), S(box));
        var read = LatticeCatalogueStepRead.Read(ladder.Current);
        Assert.True(ladder.Current.IsComplete); Assert.Equal("focused path edit", read.Kind);
        Assert.Equal(sides, E(read.SelectedPaths)); Assert.Empty(E(read.CarriedPaths));
        Assert.Same(box, Assert.Single(E(read.IntroducedPaths)));
        Assert.Same(box, Assert.Single(E(read.Result.ClosedPaths)));
        Assert.Equal(E(source.Ports), E(read.WithdrawnPorts)); Assert.Empty(E(read.Result.Ports));
        foreach (var oldPort in E(source.Ports))
        {
            var withdrawal = E((Relation)read.Correspondence.Right).Cast<Relation>()
                .Single(pair => ReferenceEquals(pair.Left, oldPort));
            Assert.Empty(E((Relation)withdrawal.Right)); Assert.Contains(oldPort, Reachable(read.Surface));
        }
        Assert.Equal(4, E(source.Paths).Length); Assert.Empty(E(source.ClosedPaths));
        Assert.True(ladder.TryReadAncestor(source.Surface, out var retained, out _)); Assert.Same(source, retained);
        Assert.True(QualitativeLatticeLadder.TryRead(author.Frame, ladder.Surface, out var reread));
        Assert.Same(box, E(LatticeCatalogueStepRead.Read(reread.Current).IntroducedPaths).Single());
        AssertNoNumeric(read.Surface);
    }

    [Fact]
    public void EmptyResultIsBlockedAndDoesNotTurnFailedIntentIntoAcceptedRemoval()
    {
        var author = new LatticeLadderAuthor(); var path = LatticeLadderAuthor.Path(S(), author.Word(1, 0));
        var source = author.Unit("one path", path);
        var ladder = QualitativeLatticeLadder.Begin(source).ReplacePaths("try removing everything", source.Paths, S(), S(), S());
        var read = LatticeCatalogueStepRead.Read(ladder.Current);
        Assert.False(ladder.Current.IsComplete); Assert.Equal("blocked edit", read.Kind);
        Assert.Same(source, read.Result); Assert.Same(path, Assert.Single(E(read.CarriedPaths)));
        Assert.Empty(E(read.SelectedPaths)); Assert.Empty(E(read.IntroducedPaths)); Assert.Empty(E(read.WithdrawnPorts));
        Assert.NotEmpty(E(ladder.Current.Blocked));
        Assert.Contains(ladder.Current.Focus, Reachable(read.Surface));
        Assert.Contains(ladder.Current.Blocked, Reachable(read.Surface));
        Assert.Same(path, E(source.Paths).Single()); AssertNoNumeric(read.Surface);
    }

    [Fact]
    public void PartialReplacementCarriesActualComplementAndOnlyWithdrawsMissingRoles()
    {
        var author = new LatticeLadderAuthor();
        var old = LatticeLadderAuthor.Path(S(), author.Word(1, 0));
        var carried = LatticeLadderAuthor.Path(author.Word(0, 1), author.Word(1, 0));
        var source = author.Unit("two paths", old, carried);
        var next = LatticeLadderAuthor.Path(S(), author.Word(1, 1));
        var retainedPorts = S(E(source.Ports).Cast<Relation>()
            .Where(port => ReferenceEquals(((Relation)port.Right).Left, carried)).Cast<IElement>().ToArray());
        var ladder = QualitativeLatticeLadder.Begin(source).ReplacePaths("replace one", S(old), S(next), retainedPorts, S());
        var read = LatticeCatalogueStepRead.Read(ladder.Current);
        Assert.Same(old, Assert.Single(E(read.SelectedPaths))); Assert.Same(next, Assert.Single(E(read.IntroducedPaths)));
        Assert.Same(carried, Assert.Single(E(read.CarriedPaths)));
        Assert.Same(carried, Assert.Single(E(read.CarriedResultPaths)));
        Assert.Equal(2, E(read.WithdrawnPorts).Length);
        Assert.All(E(read.WithdrawnPorts).Cast<Relation>(), port => Assert.Same(old, ((Relation)port.Right).Left));
        Assert.Empty(E(read.Unresolved));
    }

    [Fact]
    public void EveryExistingConstructionHasAnExactStepReadWithoutCaptionDispatch()
    {
        var recipes = new LatticeLadderRecipes(columns: 2, rows: 2);
        foreach (var item in recipes.Cases)
        {
            var read = LatticeCatalogueStepRead.Read(item.Ladder.Current);
            Assert.NotEqual("unresolved transition", read.Kind); Assert.Empty(E(read.Unresolved));
            Assert.All(E(read.SelectedPaths), path => Assert.Contains(E(read.Source!.Paths), actual => ReferenceEquals(actual, path)));
            Assert.All(E(read.IntroducedPaths), path => Assert.Contains(E(read.Result.Paths), actual => ReferenceEquals(actual, path)));
            AssertNoNumeric(read.Surface);
        }
    }

    private static HashSet<IElement> Reachable(IElement source)
    {
        var seen = new HashSet<IElement>(ReferenceEqualityComparer.Instance); var pending = new Stack<IElement>(); pending.Push(source);
        while (pending.TryPop(out var item))
            if (seen.Add(item) && item is Relation relation) { pending.Push(relation.Left); pending.Push(relation.Right); }
        return seen;
    }
    private static void AssertNoNumeric(IElement root) => Assert.All(Reachable(root), item => Assert.True(item is Relation or Placeholder));
    private static Relation S(params IElement[] values) => StructuralConstraint.Closed(values);
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
}
