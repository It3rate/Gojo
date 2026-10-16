using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeFrieze;

namespace Gojo.Experiments.QualitativeExecution.Tests;

public sealed class QualitativeFieldPassTests
{
    [Fact]
    public void UnionIntersectionAndDifferenceUseExplicitNativeLawRows()
    {
        var f = new Fixture(4); var a = f.Field(true, true, false, false); var b = f.Field(true, false, true, false);
        var union = new QualitativeFieldPass(a, b, QualitativeFieldPass.UnionLaw(f.Roles, f.Ink));
        var intersection = new QualitativeFieldPass(a, b, QualitativeFieldPass.IntersectionLaw(f.Roles, f.Ink));
        var difference = new QualitativeFieldPass(a, b, QualitativeFieldPass.DifferenceLaw(f.Roles, f.Ink));
        Assert.Equal(new[] { true, true, true, false }, f.Read(union.Result));
        Assert.Equal(new[] { true, false, false, false }, f.Read(intersection.Result));
        Assert.Equal(new[] { false, true, false, false }, f.Read(difference.Result));
        Assert.Empty(E(Read(union).Pending)); Assert.Empty(E(Read(intersection).Pending)); Assert.Empty(E(Read(difference).Pending));
        Assert.True(Contains(union.Evaluate(), a.Labels)); Assert.True(Contains(union.Evaluate(), b.Labels));
        Assert.True(Contains(union.Evaluate(), union.Law)); AssertNoNumbers(union.Evaluate());
    }

    [Fact]
    public void RolesCanBeReversedWithoutChangingTheirMeaning()
    {
        var f = new Fixture(2); var a = f.Field(false, true); var b = f.Field(true, false);
        var pass = new QualitativeFieldPass(a, b, QualitativeFieldPass.UnionLaw(f.Roles, f.Other));
        Assert.Equal(new[] { false, false }, f.Read(pass.Result)); Assert.Empty(E(Read(pass).Pending));
    }

    [Fact]
    public void CustomLawMeaningLivesInRowsRatherThanAnOperationTag()
    {
        var f = new Fixture(2); var a = f.Field(true, false); var b = f.Field(true, false); var domain = S(f.Ink, f.Other);
        var law = FiniteLaw.Create(domain, domain, domain,
            FiniteLaw.Row(f.Ink, f.Ink, S(f.Other)), FiniteLaw.Row(f.Other, f.Other, S(f.Ink)));
        var pass = new QualitativeFieldPass(a, b, law);
        Assert.Equal(new[] { false, true }, f.Read(pass.Result));
        foreach (var app in E(Read(pass).Applications).Cast<Relation>())
        {
            Assert.True(pass.TryReadApplication(app, out var cell, out var source, out var tool, out var lawRead, out var result));
            Assert.Same(cell, source.Left); Assert.Same(cell, tool.Left); Assert.Same(cell, result.Left);
            Assert.True(FiniteLaw.TryReadResult(lawRead, out _, out var pending, out var consumed));
            Assert.Empty(E(pending)); Assert.Single(E(consumed)); Assert.True(Contains(pass.Evaluate(), source));
        }
    }

    [Fact]
    public void FocusPreservesActualOutsideLabelsAndCutEdges()
    {
        var f = new Fixture(4); var a = f.Field(false, false, true, true); var b = f.Field(true, true, false, false);
        var focus = S(f.Cells[1]); var goal = new Placeholder();
        var pass = new QualitativeFieldPass(a, b, QualitativeFieldPass.UnionLaw(f.Roles, f.Ink), focus, goal);
        Assert.Equal(new[] { false, true, true, true }, f.Read(pass.Result));
        Assert.Same(E(a.Labels)[0], E(pass.Result.Labels)[0]);
        var read = Read(pass); Assert.Single(E(read.Applications)); Assert.True(Contains(pass.Surface, focus)); Assert.True(Contains(pass.Surface, goal));
        Assert.True(Contains(read.Complement, E(a.Labels)[0])); Assert.True(Contains(read.Complement, f.Edges[0]));
        Assert.True(Contains(read.Complement, f.Edges[1])); Assert.True(Contains(read.Complement, f.Cells[3]));
    }

    [Fact]
    public void AnEmptyFocusIsAnExplicitIdentityRead()
    {
        var f = new Fixture(2); var source = f.Field(true, false);
        var pass = new QualitativeFieldPass(source, f.Field(false, true), QualitativeFieldPass.DifferenceLaw(f.Roles, f.Ink), S());
        Assert.Equal(f.Read(source), f.Read(pass.Result)); Assert.Empty(E(Read(pass).Applications));
        Assert.All(E(source.Labels), label => Assert.True(Contains(pass.Result.Labels, label))); Assert.Empty(E(Read(pass).Pending));
    }

    [Fact]
    public void StructurallyEqualButDifferentBasisScopesAreNotImplicitlyAligned()
    {
        var f = new Fixture(2); var source = f.Field(true, false);
        var tool = new QualitativePartition(S(f.Cells), f.Adjacency, f.Roles, f.Field(false, true).Labels);
        var pass = new QualitativeFieldPass(source, tool, QualitativeFieldPass.UnionLaw(f.Roles, f.Ink));
        Assert.Same(source, pass.Result); Assert.NotEmpty(E(Read(pass).Pending)); Assert.Empty(E(Read(pass).Applications));
    }

    [Fact]
    public void EqualButDistinctRoleFramesRequireExplicitCorrespondence()
    {
        var f = new Fixture(2); var source = f.Field(true, false); var other = f.Field(false, true);
        var tool = new QualitativePartition(f.CellScope, f.Adjacency, new Relation(f.Ink, f.Other), other.Labels);
        var pass = new QualitativeFieldPass(source, tool, QualitativeFieldPass.UnionLaw(f.Roles, f.Ink));
        Assert.Same(source, pass.Result); Assert.NotEmpty(E(Read(pass).Pending));
    }

    [Fact]
    public void MissingToolLabelStaysUnknownEvenWhenUnionCouldChooseAnAnswer()
    {
        var f = new Fixture(2); var source = f.Field(true, false); var tool = f.Field(true, false);
        tool = new QualitativePartition(f.CellScope, f.Adjacency, f.Roles, S(E(tool.Labels)[1]));
        var pass = new QualitativeFieldPass(source, tool, QualitativeFieldPass.UnionLaw(f.Roles, f.Ink));
        Assert.Null(f.Label(pass.Result, 0)); Assert.NotEmpty(E(Read(pass).Pending)); Assert.Single(E(Read(pass).Applications));
        Assert.True(Contains(pass.Evaluate(), E(source.Labels)[0]));
    }

    [Fact]
    public void OpenAndConflictingLabelsCannotBecomeClosedDefaults()
    {
        var f = new Fixture(2); var source = f.Field(true, false); var tool = f.Field(false, true);
        var open = new QualitativePartition(f.CellScope, f.Adjacency, f.Roles, StructuralConstraint.Open(E(tool.Labels)));
        var pass = new QualitativeFieldPass(source, open, QualitativeFieldPass.UnionLaw(f.Roles, f.Ink));
        Assert.Empty(E(pass.Result.Labels)); Assert.NotEmpty(E(Read(pass).Pending));
        var conflict = new QualitativePartition(f.CellScope, f.Adjacency, f.Roles,
            S([.. E(tool.Labels), new Relation(f.Cells[0], f.Ink)]));
        var conflicted = new QualitativeFieldPass(source, conflict, QualitativeFieldPass.UnionLaw(f.Roles, f.Ink));
        Assert.Null(f.Label(conflicted.Result, 0)); Assert.NotEmpty(E(Read(conflicted).Pending));
    }

    [Fact]
    public void OpenSourceComplementStaysOpen()
    {
        var f = new Fixture(2); var known = f.Field(true, false);
        var source = new QualitativePartition(f.CellScope, f.Adjacency, f.Roles, StructuralConstraint.Open(E(known.Labels)));
        var pass = new QualitativeFieldPass(source, f.Field(false, true), QualitativeFieldPass.UnionLaw(f.Roles, f.Ink), S(f.Cells[0]));
        Assert.True(StructuralConstraint.TryReadScope(pass.Result.Labels, out _, out var seam, out var tail)); Assert.False(seam.Equals(tail));
        Assert.NotEmpty(E(Read(pass).Pending)); Assert.True(Contains(pass.Evaluate(), E(known.Labels)[1]));
    }

    [Fact]
    public void MissingAndAmbiguousLawRowsLeaveExplicitObligations()
    {
        var f = new Fixture(1); var source = f.Field(true); var tool = f.Field(true); var domain = S(f.Ink, f.Other);
        var absent = new QualitativeFieldPass(source, tool, FiniteLaw.Create(domain, domain, domain));
        Assert.Empty(E(absent.Result.Labels)); Assert.NotEmpty(E(Read(absent).Pending));
        var ambiguous = new QualitativeFieldPass(source, tool, FiniteLaw.Create(domain, domain, domain,
            FiniteLaw.Row(f.Ink, f.Ink, S(f.Ink, f.Other))));
        Assert.Empty(E(ambiguous.Result.Labels)); Assert.NotEmpty(E(Read(ambiguous).Pending));
    }

    [Fact]
    public void ForeignDomainAndOpenOrForeignFocusDoNotRunAnEdit()
    {
        var f = new Fixture(2); var a = f.Field(true, false); var b = f.Field(false, true);
        var invalids = new[] { StructuralConstraint.Open(f.Cells[0]), S(new Placeholder()), S(f.Cells[0], f.Cells[0]) };
        foreach (var focus in invalids)
        {
            var pass = new QualitativeFieldPass(a, b, QualitativeFieldPass.UnionLaw(f.Roles, f.Ink), focus);
            Assert.Same(a, pass.Result); Assert.NotEmpty(E(Read(pass).Pending));
            var complementCells = Assert.IsType<Relation>(Assert.IsType<Relation>(Read(pass).Complement.Left).Left);
            Assert.True(StructuralConstraint.TryReadScope(complementCells, out var outside, out var seam, out var tail));
            Assert.False(seam.Equals(tail)); Assert.Empty(outside);
        }
        var foreign = S(f.Ink, new Placeholder());
        var badLaw = new QualitativeFieldPass(a, b, FiniteLaw.Create(foreign, foreign, foreign));
        Assert.Same(a, badLaw.Result); Assert.NotEmpty(E(Read(badLaw).Pending));
    }

    [Fact]
    public void OrderedPassTimelineRetainsPriorResultAndCannotClaimAnUnrelatedSource()
    {
        var f = new Fixture(2); var source = f.Field(true, false); var tool = f.Field(false, true);
        var first = new QualitativeFieldPass(source, tool, QualitativeFieldPass.UnionLaw(f.Roles, f.Ink));
        var second = new QualitativeFieldPass(first.Result, tool, QualitativeFieldPass.DifferenceLaw(f.Roles, f.Ink), previous: first.Evaluate());
        Assert.Same(first.Evaluate(), second.Timeline.Left); Assert.True(Contains(second.Timeline, first.Result.Surface));
        Assert.Equal(new[] { true, false }, f.Read(second.Result)); Assert.Empty(E(Read(second).Pending));
        var wrong = new QualitativeFieldPass(source, tool, QualitativeFieldPass.DifferenceLaw(f.Roles, f.Ink), previous: first.Evaluate());
        Assert.Same(source, wrong.Result); Assert.NotEmpty(E(Read(wrong).Pending));
    }

    [Fact]
    public void AResultPointerAloneIsNotAReadablePriorPassAndPriorObligationsRemain()
    {
        var f = new Fixture(2); var source = f.Field(true, false); var tool = f.Field(false, true);
        var forged = new Relation(S(), new Relation(source.Surface, new Relation(S(), S())));
        var invalid = new QualitativeFieldPass(source, tool, QualitativeFieldPass.UnionLaw(f.Roles, f.Ink), previous: forged);
        Assert.Same(source, invalid.Result); Assert.NotEmpty(E(Read(invalid).Pending));
        var missing = new QualitativePartition(f.CellScope, f.Adjacency, f.Roles, S(E(tool.Labels)[1]));
        var first = new QualitativeFieldPass(source, missing, QualitativeFieldPass.UnionLaw(f.Roles, f.Ink));
        var second = new QualitativeFieldPass(first.Result, tool, QualitativeFieldPass.UnionLaw(f.Roles, f.Ink), previous: first.Evaluate());
        Assert.All(E(Read(first).Pending), obligation => Assert.Contains(E(Read(second).Pending), item => ReferenceEquals(item, obligation)));
        Assert.Same(first.Evaluate(), second.Timeline.Left);
    }

    [Fact]
    public void ActualOverlapSupportsStayDistinctButRepeatedSourceParticipationIsNotFresh()
    {
        var f = new Fixture(1); var source = f.Field(true); var other = f.Field(true); var law = QualitativeFieldPass.UnionLaw(f.Roles, f.Ink);
        var overlap = new QualitativeFieldPass(source, other, law); var supports = E(overlap.Supports(f.Ink)).Cast<Relation>().Single();
        Assert.Equal(2, E(Assert.IsType<Relation>(supports.Right)).Length);
        Assert.True(Contains(supports, E(source.Labels)[0])); Assert.True(Contains(supports, E(other.Labels)[0]));
        var repeated = new QualitativeFieldPass(source, source, law); var one = E(repeated.Supports(f.Ink)).Cast<Relation>().Single();
        Assert.Single(E(Assert.IsType<Relation>(one.Right)));
    }

    [Fact]
    public void ReaderRejectsAnotherPassAndOmittedProofOrApplication()
    {
        var f = new Fixture(2); var source = f.Field(true, false); var tool = f.Field(false, true); var law = QualitativeFieldPass.UnionLaw(f.Roles, f.Ink);
        var pass = new QualitativeFieldPass(source, tool, law); var other = new QualitativeFieldPass(source, tool, law); var read = Read(pass);
        Assert.False(pass.TryReadResult(other.Evaluate(), out _, out _, out _, out _, out _));
        var forged = new Relation(pass.Surface, new Relation(pass.Result.Surface,
            new Relation(new Relation(read.Complement, S()), new Relation(S(), pass.Timeline))));
        Assert.False(pass.TryReadResult(forged, out _, out _, out _, out _, out _));
        Assert.False(pass.TryReadApplication(new Relation(f.Cells[0], S()), out _, out _, out _, out _, out _));
        Assert.True(pass.TryReadResult(new Relation(pass.Surface, pass.Evaluate().Right), out _, out _, out _, out _, out _));
    }

    private static (Relation Complement, Relation Applications, Relation Pending) Read(QualitativeFieldPass pass)
    {
        Assert.True(pass.TryReadResult(pass.Evaluate(), out var result, out var complement, out var applications, out var pending, out var timeline));
        Assert.Same(pass.Result.Surface, result); Assert.Same(pass.Timeline, timeline); return (complement, applications, pending);
    }
    private static Relation S(params IElement[] entries) => StructuralConstraint.Closed(entries);
    private static IElement[] E(Relation scope) { Assert.True(StructuralConstraint.TryReadScope(scope, out var entries, out _, out _)); return entries; }
    private static bool Contains(IElement element, IElement item) => ReferenceEquals(element, item) ||
        element is Relation relation && (Contains(relation.Left, item) || Contains(relation.Right, item));
    private static void AssertNoNumbers(IElement element)
    { Assert.False(element is Carrier or CarrierLocation); if (element is Relation relation) { AssertNoNumbers(relation.Left); AssertNoNumbers(relation.Right); } }

    private sealed class Fixture
    {
        public IElement Ink { get; } = new Placeholder(); public IElement Other { get; } = new Placeholder();
        public IElement[] Cells { get; } public Relation[] Edges { get; } public Relation CellScope { get; }
        public Relation Adjacency { get; } public Relation Roles { get; }
        public Fixture(int count)
        {
            Cells = Enumerable.Range(0, count).Select(_ => (IElement)new Placeholder()).ToArray();
            Edges = Cells.Zip(Cells.Skip(1), (a, b) => new Relation(a, b)).ToArray();
            CellScope = S(Cells); Adjacency = S(Edges); Roles = new Relation(Ink, Other);
        }
        public QualitativePartition Field(params bool[] ink) => new(CellScope, Adjacency, Roles,
            S(Cells.Select((cell, i) => (IElement)new Relation(cell, ink[i] ? Ink : Other)).ToArray()));
        public Relation? Label(QualitativePartition field, int index) => E(field.Labels).OfType<Relation>().SingleOrDefault(row => ReferenceEquals(row.Left, Cells[index]));
        public bool[] Read(QualitativePartition field) => Cells.Select((_, i) => ReferenceEquals(Label(field, i)!.Right, Ink)).ToArray();
    }
}
