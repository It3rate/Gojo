using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeFrieze;

namespace Gojo.Experiments.QualitativeExecution.Tests;

public sealed class LatticeCatalogueAssessmentTests
{
    private static readonly Lazy<LatticeLadderRecipes> Study = new(() => new(2, 2));

    [Theory]
    [InlineData("L00")]
    [InlineData("L03")]
    [InlineData("L06")]
    [InlineData("L12")]
    [InlineData("L14")]
    public void ExistingDifferentTopologiesPassWithoutTreatingSharedBoundaryHistoryAsRetracing(string id)
    {
        var item = Study.Value.Cases.Single(item => item.Id == id);
        var read = Read(item.Unit, item.Recipe);
        Assert.True(read.Summary.IsStructurallyAdmissible, id);
        Assert.Equal(2, read.Summary.IndependentRepeatDirections);
        Assert.Equal(0, read.Summary.RetracedPathPairs);
        Assert.NotEmpty(E(read.Accepted)); Assert.Empty(E(read.Blocked)); Assert.Empty(E(read.Open));
        Assert.NotNull(read.Summary.Fingerprint);
        Assert.Same(item.Unit.Surface, ((Relation)read.Source.Left).Left);
        Assert.All(Reachable(read.Surface), element => Assert.True(element is Relation or Placeholder));
        if (id == "L00") Assert.True(read.Summary.SharedBoundaryHistories > 0);
    }

    [Fact]
    public void AuthoredImmediateRetracingIsRejectedButSeparateOutlineHistoriesAreNot()
    {
        var author = new LatticeLadderAuthor();
        var path = LatticeLadderAuthor.Path(author.Word(0, 0), author.Word(3, 0), author.Word(-1, 0));
        var unit = author.Unit("partial return along same line", path);
        var recipe = author.Compile("bad", "bad", "", "", unit, author.Word(5, 0), author.Word(0, 4), 2, 2);
        var read = Read(unit, recipe);
        Assert.False(read.Summary.IsStructurallyAdmissible);
        Assert.Equal(1, read.Summary.ImmediateRetraces); Assert.Equal(1, read.Summary.RetracedPathPairs);
        Assert.Contains(E(read.Blocked), row => Reachable(row).Any(element => ReferenceEquals(element, path)));
        Assert.True(read.Summary.GeometricOverlaps > 0);
    }

    [Fact]
    public void AVerticalExcursionBetweenOpposedHorizontalChordsPreventsFalseRetrace()
    {
        var author = new LatticeLadderAuthor();
        var path = LatticeLadderAuthor.Path(author.Word(0, 0), author.Word(3, 0), author.Word(0, 1), author.Word(-3, 0));
        var unit = author.Unit("separated opposed runs", path);
        var read = Read(unit, author.Compile("ok", "ok", "", "", unit, author.Word(5, 0), author.Word(0, 4), 2, 2));
        Assert.True(read.Summary.IsStructurallyAdmissible);
        Assert.Equal(0, read.Summary.ImmediateRetraces); Assert.Equal(0, read.Summary.RetracedPathPairs);
    }

    [Fact]
    public void AZeroChordAndCollinearRepeatDirectionsAreIndependentFailures()
    {
        var author = new LatticeLadderAuthor();
        var path = LatticeLadderAuthor.Path(author.Word(0, 0), author.Word(0, 0), author.Word(1, 0));
        var unit = author.Unit("zero chord", path);
        var read = Read(unit, author.Compile("bad", "bad", "", "", unit, author.Word(3, 0), author.Word(6, 0), 2, 2));
        Assert.False(read.Summary.IsStructurallyAdmissible); Assert.Equal(1, read.Summary.ZeroChords);
        Assert.Equal(1, read.Summary.IndependentRepeatDirections); Assert.NotEmpty(E(read.Blocked));
    }

    [Fact]
    public void AFalseSeamRemainsBlockedAndFiniteFrontierIsNotAnInteriorFailure()
    {
        var item = Study.Value.Cases.Single(item => item.Id == "L00");
        var run = item.Recipe.Program.Generate(item.Recipe.Bindings, item.Recipe.Window);
        var field = new OrnamentProgramField(run, Study.Value.BlockedSeamContract);
        var read = LatticeCatalogueAssessment.Read(item.Unit, run, field);
        Assert.False(read.Summary.IsStructurallyAdmissible);
        Assert.True(read.Summary.BlockedSeamObligations > 0);
        Assert.True(read.Summary.FiniteFrontierObligations > 0);
        Assert.NotEmpty(E(read.Blocked));
    }

    [Fact]
    public void AnOpenDeclaredSeamOffsetRemainsAMissingObligation()
    {
        var item = Study.Value.Cases.Single(item => item.Id == "L00");
        var actualRule = Assert.IsType<Relation>(E((Relation)item.Recipe.SeamContract.Right)[0]);
        var rolesAndOffset = (Relation)actualRule.Right;
        var openRule = new Relation(actualRule.Left, new Relation(StructuralConstraint.Open(E((Relation)rolesAndOffset.Left)), rolesAndOffset.Right));
        var run = item.Recipe.Program.Generate(item.Recipe.Bindings, item.Recipe.Window);
        var contract = OrnamentProgramField.Contract(item.Recipe.Program, openRule);
        var read = LatticeCatalogueAssessment.Read(item.Unit, run, new(run, contract));
        Assert.False(read.Summary.IsStructurallyAdmissible); Assert.True(read.Summary.MissingSeamObligations > 0);
        Assert.NotEmpty(E(read.Open)); Assert.Empty(E(read.Blocked));
    }

    [Fact]
    public void AGeometricCrossingRemainsUnjoinedAndDoesNotFailTheDefaultRuleSet()
    {
        var author = new LatticeLadderAuthor();
        var paths = new[]
        {
            LatticeLadderAuthor.Path(author.Word(0, 0), author.Word(2, 2)),
            LatticeLadderAuthor.Path(author.Word(0, 2), author.Word(2, -2))
        };
        var unit = author.Unit("two unjoined diagonals", paths);
        var recipe = author.Compile("cross", "cross", "", "", unit, author.Word(4, 0), author.Word(0, 4), 2, 2);
        var run = recipe.Program.Generate(recipe.Bindings, recipe.Window);
        var field = new OrnamentProgramField(run, recipe.SeamContract);
        var before = E(field.Graph.Sites).Length;
        var read = LatticeCatalogueAssessment.Read(unit, run, field);
        Assert.True(read.Summary.IsStructurallyAdmissible); Assert.Equal(4, read.Summary.UnjoinedCrossings);
        Assert.Equal(0, read.Summary.JoinedBranchSites); Assert.Equal(before, E(field.Graph.Sites).Length);
        Assert.Equal(8, read.Summary.Components);
    }

    [Fact]
    public void FingerprintIgnoresTranslationUniformScaleNamesAndIndependentSourceIdentity()
    {
        static LatticeCatalogueAssessment Square(int scale, int x, int y)
        {
            var author = new LatticeLadderAuthor();
            var path = LatticeLadderAuthor.Path(author.Word(x, y), author.Word(scale, 0), author.Word(0, scale), author.Word(-scale, 0), author.Word(0, -scale));
            var unit = author.Unit(Guid.NewGuid().ToString(), path);
            return Read(unit, author.Compile("square", "square", "", "", unit, author.Word(3 * scale, 0), author.Word(0, 3 * scale), 2, 2));
        }
        Assert.Equal(Square(1, 0, 0).Summary.Fingerprint, Square(3, 9, -6).Summary.Fingerprint);
        Assert.NotEqual(Square(1, 0, 0).Summary.Fingerprint, Read(Study.Value.Cases[3].Unit, Study.Value.Cases[3].Recipe).Summary.Fingerprint);
    }

    [Fact]
    public void FingerprintDoesNotConfuseAnActualCrossJoinWithTwoUnjoinedLines()
    {
        var author = new LatticeLadderAuthor();
        var crossing = author.Unit("unjoined", LatticeLadderAuthor.Path(author.Word(-1, 0), author.Word(2, 0)),
            LatticeLadderAuthor.Path(author.Word(0, -1), author.Word(0, 2)));
        var joined = author.Unit("joined", LatticeLadderAuthor.Path(author.Word(-1, 0), author.Word(1, 0), author.Word(1, 0)),
            LatticeLadderAuthor.Path(author.Word(0, -1), author.Word(0, 1), author.Word(0, 1)));
        LatticeCatalogueAssessment Inspect(UnitArrangementProgram unit) => Read(unit,
            author.Compile("cross", "cross", "", "", unit, author.Word(4, 0), author.Word(0, 4), 2, 2));
        var a = Inspect(crossing); var b = Inspect(joined);
        Assert.Equal(4, a.Summary.UnjoinedCrossings); Assert.Equal(0, a.Summary.JoinedBranchSites);
        Assert.Equal(0, b.Summary.UnjoinedCrossings); Assert.Equal(4, b.Summary.JoinedBranchSites);
        Assert.NotEqual(a.Summary.Fingerprint, b.Summary.Fingerprint);
    }

    [Fact]
    public void FingerprintIgnoresStraightDegreeTwoLandmarkSubdivision()
    {
        var author = new LatticeLadderAuthor();
        var coarse = author.Unit("single chord", LatticeLadderAuthor.Path(author.Word(0, 0), author.Word(2, 0)));
        var fine = author.Unit("split chord", LatticeLadderAuthor.Path(author.Word(0, 0), author.Word(1, 0), author.Word(1, 0)));
        (LatticeCatalogueAssessment Read, int RawCoordinateGcd) Inspect(UnitArrangementProgram unit)
        {
            var recipe = author.Compile("line", "line", "", "", unit, author.Word(4, 0), author.Word(0, 4), 2, 2);
            var run = recipe.Program.Generate(recipe.Bindings, recipe.Window);
            var field = new OrnamentProgramField(run, recipe.SeamContract);
            var gcd = 0;
            foreach (var binding in E(field.Bindings).Cast<Relation>())
            {
                var x = 0; var y = 0;
                foreach (var move in E((Relation)binding.Right).Cast<Relation>())
                {
                    Assert.True(author.Frame.Direction(move, out var axis, out var forward, out _));
                    if (ReferenceEquals(axis, author.Frame.Horizontal)) x += forward ? 1 : -1;
                    else y += forward ? 1 : -1;
                }
                gcd = Gcd(Gcd(gcd, x), y);
            }
            return (LatticeCatalogueAssessment.Read(unit, run, field), gcd);
        }
        var a = Inspect(coarse); var b = Inspect(fine);
        // These premises catch normalization accidentally using all raw sites:
        // the added midpoint changes raw gcd 2 -> 1 although support is fixed.
        Assert.Equal(2, a.RawCoordinateGcd); Assert.Equal(1, b.RawCoordinateGcd);
        Assert.True(b.Read.Summary.SourceStrokes > a.Read.Summary.SourceStrokes);
        Assert.Equal(a.Read.Summary.Components, b.Read.Summary.Components);
        Assert.Equal(a.Read.Summary.BoundaryEnds, b.Read.Summary.BoundaryEnds);
        Assert.Equal(a.Read.Summary.JoinedBranchSites, b.Read.Summary.JoinedBranchSites);
        Assert.Equal(a.Read.Summary.Fingerprint, b.Read.Summary.Fingerprint);

        static int Gcd(int a, int b)
        { a = Math.Abs(a); b = Math.Abs(b); while (b != 0) (a, b) = (b, a % b); return a; }
    }

    [Fact]
    public void AUnitFromAnUnrelatedSourceCannotBorrowAnotherExecutionVerdict()
    {
        var item = Study.Value.Cases.Single(item => item.Id == "L00");
        var different = Study.Value.Cases.Single(item => item.Id == "L01");
        var run = item.Recipe.Program.Generate(item.Recipe.Bindings, item.Recipe.Window);
        var read = LatticeCatalogueAssessment.Read(different.Unit, run, new(run, item.Recipe.SeamContract));
        Assert.False(read.Summary.IsStructurallyAdmissible);
        Assert.NotEmpty(E(read.Blocked));
    }

    [Fact]
    public void MalformedOrOpenUnitParticipationIsRetainedRatherThanThrowing()
    {
        var item = Study.Value.Cases.Single(item => item.Id == "L00");
        var malformedPath = new Relation(new Placeholder(), new Placeholder());
        var malformed = new UnitArrangementProgram(item.Unit.Frame, "open path contract", StructuralConstraint.Open(malformedPath), StructuralConstraint.Closed());
        var run = item.Recipe.Program.Generate(item.Recipe.Bindings, item.Recipe.Window);
        var read = LatticeCatalogueAssessment.Read(malformed, run, new(run, item.Recipe.SeamContract));
        Assert.False(read.Summary.IsStructurallyAdmissible);
        Assert.Contains(E(read.Open), row => ReferenceEquals(row, malformedPath));
        Assert.Contains(E(read.Open), row => ReferenceEquals(row, malformed.Surface));
    }

    private static LatticeCatalogueAssessment Read(UnitArrangementProgram unit, BourgoinPatternLibrary.Recipe recipe)
    {
        var run = recipe.Program.Generate(recipe.Bindings, recipe.Window);
        return LatticeCatalogueAssessment.Read(unit, run, new(run, recipe.SeamContract));
    }
    private static IElement[] E(Relation group) => FiniteLaw.Entries(group);
    private static IEnumerable<IElement> Reachable(IElement root)
    {
        var seen = new HashSet<IElement>(ReferenceEqualityComparer.Instance); var stack = new Stack<IElement>(); stack.Push(root);
        while (stack.TryPop(out var current))
        {
            if (!seen.Add(current)) continue; yield return current;
            if (current is Relation relation) { stack.Push(relation.Left); stack.Push(relation.Right); }
        }
    }
}
