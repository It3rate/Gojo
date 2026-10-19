using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeFrieze;
using static Gojo.Experiments.QualitativeFrieze.LatticeLadderAuthor;

namespace Gojo.Experiments.QualitativeExecution.Tests;

public sealed class LatticePublishedContactTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ExplicitConformationSplitsPartialSharedSupportAndRequiresSeparateAcceptedSeams(bool reverse)
    {
        var a = new LatticeLadderAuthor();
        var first = reverse ? Path(a.Word(4, 0), a.Word(-4, 0)) : Path(a.Word(0, 0), a.Word(4, 0));
        var second = reverse ? Path(a.Word(3, 0), a.Word(-2, 0)) : Path(a.Word(1, 0), a.Word(2, 0));
        var source = QualitativeLatticeLadder.Begin(a.Unit("partial support", first, second));
        var before = Field(a, source);
        var beforeSites = E(before.Field.Graph.Sites).ToArray(); var beforeStrokes = E(before.Field.Graph.Strokes).ToArray();
        var observed = LatticeCatalogueAssessment.Read(source.Current.Program, before.Run, before.Field);
        Assert.Equal(4, observed.Summary.GeometricOverlaps); Assert.Empty(E(before.Field.Accepted));
        // Observation sees the overlap and creates neither a cut nor a join.
        Assert.Equal(beforeSites, E(before.Field.Graph.Sites)); Assert.Equal(beforeStrokes, E(before.Field.Graph.Strokes));
        var result = LatticeGeometrySurgery.ConformPublishedContacts(source, "publish existing endpoint contacts");
        Assert.Equal(source.Rungs.Count + 2, result.Rungs.Count); Assert.True(result.Current.IsComplete);
        Assert.Equal(3, E((Relation)((Relation)E(result.Current.Program.Paths)[0]).Right).Length);
        var refined = result.Rungs[^2]; Assert.Single(E(refined.Replacement));
        var row = Assert.IsType<Relation>(Assert.Single(E(refined.Replacement)));
        Assert.Same(E((Relation)first.Right)[0], row.Left); Assert.Equal(3, E((Relation)row.Right).Length);
        var after = Field(a, result);
        Assert.True(after.Field.IsComplete); Assert.NotEmpty(E(after.Field.Accepted));
        var read = LatticeCatalogueAssessment.Read(result.Current.Program, after.Run, after.Field);
        Assert.True(read.Summary.IsStructurallyAdmissible); Assert.Equal(0, read.Summary.GeometricOverlaps);
        Assert.Equal(0, read.Summary.UnjoinedContacts); Assert.Equal(4, read.Summary.SharedBoundaryHistories);
        Assert.True(result.TryReadAncestor(source.Current.Program.Surface, out var old, out _)); Assert.Same(source.Current.Program, old);
        Assert.True(QualitativeLatticeLadder.TryRead(a.Frame, result.Surface, out _));
        Assert.All(Reachable(result.Surface), element => Assert.True(element is Relation or Placeholder));
        Assert.Same(result, LatticeGeometrySurgery.ConformPublishedContacts(result, "already conforms"));
    }

    [Fact]
    public void NeighbourOffersNeedActualPeriodWordsAndRetainNativeTranslationEvidence()
    {
        var a = new LatticeLadderAuthor(); var column = a.Word(4, 0); var row = a.Word(0, 8);
        var first = Path(a.Word(0, 0), a.Word(4, 0)); var neighbour = Path(a.Word(5, 0), a.Word(2, 0));
        var source = QualitativeLatticeLadder.Begin(a.Unit("neighbour contact", first, neighbour));
        Assert.Same(source, LatticeGeometrySurgery.ConformPublishedContacts(source, "local endpoints only"));
        var result = LatticeGeometrySurgery.ConformPublishedContacts(source, "offer neighbouring endpoints", column, row);
        Assert.True(result.Current.IsComplete); Assert.NotSame(source, result);
        Assert.Contains(Reachable(result.Surface), item => ReferenceEquals(item, column));
        Assert.Contains(Reachable(result.Surface), item => ReferenceEquals(item, row));
        var before = Field(a, source, column, row); var after = Field(a, result, column, row);
        Assert.True(LatticeCatalogueAssessment.Read(source.Current.Program, before.Run, before.Field).Summary.GeometricOverlaps > 0);
        var read = LatticeCatalogueAssessment.Read(result.Current.Program, after.Run, after.Field);
        Assert.Equal(0, read.Summary.GeometricOverlaps); Assert.Equal(0, read.Summary.UnjoinedContacts);
        Assert.NotEmpty(E(after.Field.Accepted)); Assert.True(QualitativeLatticeLadder.TryRead(a.Frame, result.Surface, out _));
    }

    [Fact]
    public void AProperCrossingWithoutPublishedInteriorEndpointRemainsUnjoined()
    {
        var a = new LatticeLadderAuthor();
        var first = Path(a.Word(-2, 0), a.Word(4, 0)); var second = Path(a.Word(0, -2), a.Word(0, 4));
        var source = QualitativeLatticeLadder.Begin(a.Unit("unjoined crossing", first, second));
        var result = LatticeGeometrySurgery.ConformPublishedContacts(source, "endpoint correspondence only", a.Word(12, 0), a.Word(0, 12));
        Assert.Same(source, result);
        var field = Field(a, result);
        var read = LatticeCatalogueAssessment.Read(result.Current.Program, field.Run, field.Field);
        Assert.Equal(4, read.Summary.UnjoinedCrossings); Assert.Equal(0, read.Summary.JoinedBranchSites);
    }

    [Fact]
    public void NoncollinearAndUnpublishedEndpointsCannotSupplyAnInteriorContact()
    {
        var a = new LatticeLadderAuthor(); var first = Path(a.Word(0, 0), a.Word(4, 0));
        var offLine = Path(a.Word(2, 1), a.Word(0, 2));
        var source = QualitativeLatticeLadder.Begin(a.Unit("off line", first, offLine));
        Assert.Same(source, LatticeGeometrySurgery.ConformPublishedContacts(source, "no collinear contact"));
        var privateSecond = Path(a.Word(1, 0), a.Word(2, 0));
        var restricted = QualitativeLatticeLadder.Begin(new UnitArrangementProgram(a.Frame, "restricted publication", S(first, privateSecond), Ports(first)));
        Assert.Same(restricted, LatticeGeometrySurgery.ConformPublishedContacts(restricted, "published endpoints only"));
    }

    [Fact]
    public void RelativeCutsAreUnionedAcrossActualAliasedChordUsesWithoutInventingNewContacts()
    {
        var a = new LatticeLadderAuthor(); var chord = a.Word(4, 0);
        var first = Path(a.Word(0, 0), chord); var alias = Path(a.Word(0, 4), chord);
        var contact = Path(a.Word(1, 0), a.Word(2, 0));
        var source = QualitativeLatticeLadder.Begin(a.Unit("two uses", first, alias, contact));
        var result = LatticeGeometrySurgery.ConformPublishedContacts(source, "one shared word, two uses");
        var paths = E(result.Current.Program.Paths).Cast<Relation>().ToArray();
        Assert.Equal(3, E((Relation)paths[0].Right).Length); Assert.Equal(3, E((Relation)paths[1].Right).Length);
        Assert.Single(E(result.Rungs[^2].Replacement));
        Assert.Same(E((Relation)paths[0].Right)[1], E((Relation)paths[1].Right)[1]);
        Assert.True(result.Current.IsComplete); Assert.True(QualitativeLatticeLadder.TryRead(a.Frame, result.Surface, out _));
    }

    [Fact]
    public void IndependentReachPromisesRemainOpenInsteadOfBeingTreatedAsEqualDistances()
    {
        var a = new LatticeLadderAuthor(); var otherReach = new Placeholder();
        var first = Path(a.Word(0, 0), a.Word(4, 0));
        var second = Path(a.Word(1, 0), S(QualitativeMotionFrame.Move(a.Frame.Right, otherReach)));
        var source = QualitativeLatticeLadder.Begin(a.Unit("independent reaches", first, second));
        var result = LatticeGeometrySurgery.ConformPublishedContacts(source, "no common-reach premise");
        Assert.False(result.Current.IsComplete); Assert.Same(source.Current.Program, result.Current.Program);
        Assert.NotEmpty(E(result.Current.Open)); Assert.Empty(E(result.Current.Blocked));
        Assert.True(QualitativeLatticeLadder.TryRead(a.Frame, result.Surface, out _));
    }

    private static (OrnamentProgramRun Run, OrnamentProgramField Field) Field(LatticeLadderAuthor author,
        QualitativeLatticeLadder ladder, Relation? column = null, Relation? row = null)
    {
        var recipe = author.Compile("contact", "contact", "", "", ladder.Current.Program, column ?? author.Word(12, 0), row ?? author.Word(0, 12), 2, 2);
        var run = recipe.Program.Generate(recipe.Bindings, recipe.Window); return (run, new(run, recipe.SeamContract));
    }
    private static IEnumerable<IElement> Reachable(IElement root)
    {
        var seen = new HashSet<IElement>(ReferenceEqualityComparer.Instance); var stack = new Stack<IElement>(); stack.Push(root);
        while (stack.TryPop(out var item)) { if (!seen.Add(item)) continue; yield return item; if (item is Relation pair) { stack.Push(pair.Left); stack.Push(pair.Right); } }
    }
    private static Relation S(params IElement[] items) => StructuralConstraint.Closed(items);
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
}
