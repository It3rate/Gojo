using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeFrieze;
using static Gojo.Experiments.QualitativeFrieze.LatticeLadderAuthor;

namespace Gojo.Experiments.QualitativeExecution.Tests;

public sealed class LatticeGeometrySurgeryTests
{
    [Fact]
    public void ANotchBendsTheActualChordPreservesEndpointsAndPublishesNewVertices()
    {
        var a = new LatticeLadderAuthor(); var chord = a.Word(8, 0); var path = Path(a.Word(0, 0), chord);
        var source = QualitativeLatticeLadder.Begin(a.Unit("straight", path));
        var row = LatticeGeometrySurgery.NotchRow(a.Frame, chord, a.Word(2, 0), a.Word(0, 2), a.Word(4, 0), a.Word(2, 0));
        var result = LatticeGeometrySurgery.RefineAndPublish(source, "offset notch", S(row));
        Assert.Equal(3, result.Rungs.Count); Assert.True(result.Current.IsComplete);
        var changed = Assert.IsType<Relation>(Assert.Single(E(result.Current.Program.Paths)));
        var words = E((Relation)changed.Right).Cast<Relation>().ToArray();
        Assert.Equal(5, words.Length); Assert.Contains(words, word => Point(a, word) == (0, 2));
        Assert.Equal((8, 0), Point(a, JoinWords(words))); Assert.Equal((0, 0), Point(a, (Relation)changed.Left));
        Assert.Equal(6, E(result.Current.Program.Ports).Length);
        Assert.All(E(source.Current.Program.Ports).Cast<Relation>(), port =>
            Assert.Contains(E(result.Current.Program.Ports).Cast<Relation>(), current => ReferenceEquals(current.Left, port.Left)));
        Assert.Same(changed, ((Relation)Assert.Single(E(result.Current.Carried))));
        Assert.True(result.TryReadAncestor(source.Current.Program.Surface, out var old, out _)); Assert.Same(source.Current.Program, old);
        Assert.True(QualitativeLatticeLadder.TryRead(a.Frame, result.Surface, out _));
        Assert.All(Reachable(result.Surface), element => Assert.True(element is Relation or Placeholder));
    }

    [Fact]
    public void EndpointChangingOrForeignNotchRowsCannotPretendToBeRefinements()
    {
        var a = new LatticeLadderAuthor(); var chord = a.Word(8, 0); var source = QualitativeLatticeLadder.Begin(a.Unit("line", Path(a.Word(0, 0), chord)));
        Assert.Throws<ArgumentException>(() => LatticeGeometrySurgery.NotchRow(a.Frame, chord,
            a.Word(2, 0), a.Word(0, 2), a.Word(3, 0), a.Word(2, 0)));
        var foreign = a.Word(8, 0);
        var row = LatticeGeometrySurgery.NotchRow(a.Frame, foreign, a.Word(2, 0), a.Word(0, 2), a.Word(4, 0), a.Word(2, 0));
        Assert.Throws<ArgumentException>(() => LatticeGeometrySurgery.RefineAndPublish(source, "foreign", S(row)));
    }

    [Fact]
    public void InverseBoundaryChainReversesBothOrderAndEachOrderedDisplacement()
    {
        var a = new LatticeLadderAuthor(); var chain = S(a.Word(2, 0), a.Word(0, 2), a.Word(4, 0), a.Word(0, -2), a.Word(2, 0));
        var reversed = LatticeGeometrySurgery.InverseChain(a.Frame, chain);
        Assert.Equal(5, E(reversed).Length); Assert.Equal((-8, 0), Point(a, JoinWords(E(reversed).Cast<Relation>().ToArray())));
        Assert.Equal((0, 2), Point(a, (Relation)E(reversed)[1]));
        Assert.Equal((0, -2), Point(a, (Relation)E(reversed)[3]));
    }

    [Fact]
    public void CuttingAChordRemovesItsMiddleInkAndRetiresTheOldClosureClaim()
    {
        var a = new LatticeLadderAuthor(); var bottom = a.Word(8, 0);
        var path = Path(a.Word(0, 0), bottom, a.Word(0, 4), a.Word(-8, 0), a.Word(0, -4));
        var unit = new UnitArrangementProgram(a.Frame, "closed cell", S(path), Ports(path), S(path));
        var source = QualitativeLatticeLadder.Begin(unit);
        var result = LatticeGeometrySurgery.CutChordAndPublish(source, "open side", path, bottom, a.Word(2, 0), a.Word(4, 0), a.Word(2, 0));
        Assert.True(result.Current.IsComplete); Assert.Equal(2, E(result.Current.Program.Paths).Length);
        Assert.Empty(E(result.Current.Program.ClosedPaths)); Assert.Single(E(result.Current.Focus));
        var newPaths = E(result.Current.Program.Paths).Cast<Relation>().ToArray();
        Assert.Equal((0, 0), Point(a, (Relation)newPaths[0].Left));
        Assert.Equal((6, 0), Point(a, (Relation)newPaths[1].Left));
        Assert.Equal((2, 0), Point(a, JoinWords(E((Relation)newPaths[0].Right).Cast<Relation>().ToArray())));
        Assert.DoesNotContain(Reachable(result.Current.Program.Paths), item => ReferenceEquals(item, bottom));
        Assert.True(QualitativeLatticeLadder.TryRead(a.Frame, result.Surface, out _));
        Assert.Throws<ArgumentException>(() => LatticeGeometrySurgery.CutChordAndPublish(source, "false endpoint", path, bottom,
            a.Word(2, 0), a.Word(5, 0), a.Word(2, 0)));
    }

    [Fact]
    public void DeletingAConstraintCarriesOtherActualPathsAndCannotInventAnEmptyUnit()
    {
        var a = new LatticeLadderAuthor(); var first = Path(a.Word(0, 0), a.Word(8, 0)); var second = Path(a.Word(0, 4), a.Word(8, 0));
        var source = QualitativeLatticeLadder.Begin(a.Unit("two runs", first, second));
        var result = LatticeGeometrySurgery.DeletePaths(source, "release one", S(second));
        Assert.True(result.Current.IsComplete); Assert.Same(first, Assert.Single(E(result.Current.Program.Paths)));
        Assert.Same(first, Assert.Single(E(result.Current.Carried))); Assert.Empty(E(result.Current.Program.ClosedPaths));
        Assert.True(QualitativeLatticeLadder.TryRead(a.Frame, result.Surface, out _));
        var all = LatticeGeometrySurgery.DeletePaths(result, "remove last", S(first));
        Assert.False(all.Current.IsComplete); Assert.Same(result.Current.Program, all.Current.Program); Assert.NotEmpty(E(all.Current.Blocked));
        var lookalike = Path(first.Left as Relation ?? throw new InvalidOperationException(), E((Relation)first.Right).Cast<Relation>().ToArray());
        var foreign = LatticeGeometrySurgery.DeletePaths(source, "foreign focus", S(lookalike));
        Assert.False(foreign.Current.IsComplete); Assert.NotEmpty(E(foreign.Current.Blocked));
    }

    [Fact]
    public void TwoParallelChordOccurrencesFuseThroughOneRealSharedRunWithoutChangingOldEndpoints()
    {
        var a = new LatticeLadderAuthor(); var firstChord = a.Word(8, 0); var secondChord = a.Word(-8, 0);
        var first = Path(a.Word(0, 0), firstChord, a.Word(0, -2));
        var second = Path(a.Word(8, 4), secondChord, a.Word(0, 2));
        var source = QualitativeLatticeLadder.Begin(a.Unit("opposed runs", first, second));
        var result = LatticeGeometrySurgery.FuseChordRuns(source, "shared middle", first, firstChord, second, secondChord, a.Word(2, 2), a.Word(6, 2));
        Assert.True(result.Current.IsComplete); Assert.Equal(2, E(result.Current.Program.Paths).Length);
        var changed = E(result.Current.Program.Paths).Cast<Relation>().ToArray();
        Assert.Equal(4, E((Relation)changed[0].Right).Length); Assert.Equal(4, E((Relation)changed[1].Right).Length);
        Assert.Equal((8, -2), Point(a, JoinWords(E((Relation)changed[0].Right).Cast<Relation>().ToArray())));
        Assert.Equal((-8, 2), Point(a, JoinWords(E((Relation)changed[1].Right).Cast<Relation>().ToArray())));
        Assert.Same(E((Relation)first.Right)[1], E((Relation)changed[0].Right)[3]);
        Assert.Same(E((Relation)second.Right)[1], E((Relation)changed[1].Right)[3]);
        Assert.True(QualitativeLatticeLadder.TryRead(a.Frame, result.Surface, out _));
        var recipe = a.Compile("fused", "fused", "", "", result.Current.Program, a.Word(14, 0), a.Word(0, 12), 2, 2);
        var run = recipe.Program.Generate(recipe.Bindings, recipe.Window); var field = new OrnamentProgramField(run, recipe.SeamContract);
        Assert.True(run.IsComplete); Assert.True(field.IsComplete);
        var read = LatticeCatalogueAssessment.Read(result.Current.Program, run, field);
        Assert.True(read.Summary.IsStructurallyAdmissible); Assert.True(read.Summary.SharedBoundaryHistories >= 4);
        Assert.True(read.Summary.JoinedBranchSites >= 8); Assert.Equal(0, read.Summary.GeometricOverlaps);
    }

    [Fact]
    public void FusionKeepsAnAliasedChordInAnUnselectedSiblingAndRejectsNonparallelRuns()
    {
        var a = new LatticeLadderAuthor(); var sharedWord = a.Word(8, 0); var vertical = a.Word(0, 8);
        var first = Path(a.Word(0, 0), sharedWord); var second = Path(a.Word(0, 4), a.Word(8, 0));
        var sibling = Path(a.Word(0, 10), sharedWord); var other = Path(a.Word(12, 0), vertical);
        var source = QualitativeLatticeLadder.Begin(a.Unit("aliased runs", first, second, sibling, other));
        var result = LatticeGeometrySurgery.FuseChordRuns(source, "local fusion", first, sharedWord, second, (Relation)E((Relation)second.Right)[0], a.Word(2, 2), a.Word(6, 2));
        Assert.True(result.Current.IsComplete); Assert.Contains(E(result.Current.Program.Paths), path => ReferenceEquals(path, sibling));
        Assert.Same(sharedWord, Assert.Single(E((Relation)sibling.Right)));
        Assert.Throws<ArgumentException>(() => LatticeGeometrySurgery.FuseChordRuns(source, "nonparallel", first, sharedWord, other, vertical, a.Word(2, 2), a.Word(6, 2)));
        Assert.Throws<ArgumentException>(() => LatticeGeometrySurgery.FuseChordRuns(source, "wrong ordered span", first, sharedWord, second, (Relation)E((Relation)second.Right)[0], a.Word(6, 2), a.Word(2, 2)));
    }

    [Fact]
    public void ChordFusionPreservesActualClosedContourObligationsAndPublishedRoleIdentities()
    {
        var a = new LatticeLadderAuthor(); var top = a.Word(-8, 0); var bottom = a.Word(8, 0);
        var first = Path(a.Word(0, 0), a.Word(8, 0), a.Word(0, 2), top, a.Word(0, -2));
        var second = Path(a.Word(0, 4), bottom, a.Word(0, 2), a.Word(-8, 0), a.Word(0, -2));
        var unit = new UnitArrangementProgram(a.Frame, "two closed boxes", S(first, second), Ports(first, second), S(first, second));
        var source = QualitativeLatticeLadder.Begin(unit);
        var result = LatticeGeometrySurgery.FuseChordRuns(source, "close the gap around one shared border", first, top, second, bottom,
            a.Word(6, 3), a.Word(2, 3));
        Assert.True(result.Current.IsComplete); Assert.Equal(2, E(result.Current.Program.ClosedPaths).Length);
        Assert.All(E(result.Current.Program.ClosedPaths), closed => Assert.Contains(E(result.Current.Program.Paths), path => ReferenceEquals(path, closed)));
        Assert.All(E(unit.Ports).Cast<Relation>(), old => Assert.Contains(E(result.Current.Program.Ports).Cast<Relation>(), current => ReferenceEquals(old.Left, current.Left)));
        foreach (var path in E(result.Current.Program.ClosedPaths).Cast<Relation>())
            Assert.Equal((0, 0), Point(a, JoinWords(E((Relation)path.Right).Cast<Relation>().ToArray())));
        Assert.True(QualitativeLatticeLadder.TryRead(a.Frame, result.Surface, out _));
    }

    [Fact]
    public void WholeRunFusionRetargetsExplicitlyAndPreservesAnIndependentCarriedPath()
    {
        var a = new LatticeLadderAuthor(); var first = Path(a.Word(0, 0), a.Word(8, 0));
        var second = Path(a.Word(8, 4), a.Word(-8, 0)); var carried = Path(a.Word(12, 0), a.Word(0, 8));
        var source = QualitativeLatticeLadder.Begin(a.Unit("three runs", first, second, carried));
        var result = LatticeGeometrySurgery.FuseParallelRuns(source, "one shared run", first, second, a.Word(0, 2));
        Assert.True(result.Current.IsComplete); Assert.Equal(2, E(result.Current.Program.Paths).Length);
        Assert.Same(carried, Assert.Single(E(result.Current.Carried))); Assert.Equal(2, E(result.Current.Focus).Length);
        var replacement = E(result.Current.Program.Paths).Cast<Relation>().Single(path => !ReferenceEquals(path, carried));
        Assert.Equal((0, 2), Point(a, (Relation)replacement.Left)); Assert.Single(E((Relation)replacement.Right));
        Assert.True(QualitativeLatticeLadder.TryRead(a.Frame, result.Surface, out _));
    }

    private static (int X, int Y) Point(LatticeLadderAuthor author, Relation word)
    {
        var x = 0; var y = 0;
        foreach (var move in E(word).Cast<Relation>())
        {
            Assert.True(author.Frame.Direction(move, out var axis, out var forward, out var reach)); Assert.Same(author.Reach, reach);
            if (ReferenceEquals(axis, author.Frame.Horizontal)) x += forward ? 1 : -1; else y += forward ? 1 : -1;
        }
        return (x, y);
    }
    private static IEnumerable<IElement> Reachable(IElement root)
    {
        var seen = new HashSet<IElement>(ReferenceEqualityComparer.Instance); var stack = new Stack<IElement>(); stack.Push(root);
        while (stack.TryPop(out var current))
        { if (!seen.Add(current)) continue; yield return current; if (current is Relation row) { stack.Push(row.Left); stack.Push(row.Right); } }
    }
    private static Relation S(params IElement[] items) => StructuralConstraint.Closed(items);
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
}
