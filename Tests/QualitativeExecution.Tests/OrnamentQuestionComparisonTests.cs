using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;
using Gojo.Experiments.QualitativeFrieze;

namespace Gojo.Experiments.QualitativeExecution.Tests;

public sealed class OrnamentQuestionComparisonTests
{
    [Fact]
    public void HeadingReadersReceiveOnlyTheirActualInducedEdgesAndBindings()
    {
        var f = new Fixture(); var a = f.Line(0, 4); var b = f.Line(3, 4);
        var read = f.Heading(S(a), S(b), S(f.Candidate(S(S(f.Frame.Right)))));
        Assert.True(read.IsComplete); var candidate = Assert.Single(read.Candidates);
        Assert.Equal(4, candidate.Training.CoverageCount); Assert.Equal(4, candidate.HeldOut.CoverageCount);
        Assert.Equal(1, candidate.Training.RecurrenceCount); Assert.Equal(1, candidate.HeldOut.RecurrenceCount);
        var train = candidate.Training.Strands!; var held = candidate.HeldOut.Strands!;
        Assert.Equal(4, E((Relation)((Relation)train.SourceTopology.Right).Left).Length);
        Assert.Equal(4, E((Relation)((Relation)held.SourceTopology.Right).Left).Length);
        Assert.Equal(5, E(train.SourceBindings).Length); Assert.Equal(5, E(held.SourceBindings).Length);
        foreach (var edge in a) Assert.DoesNotContain(E((Relation)((Relation)held.SourceTopology.Right).Left), item => ReferenceEquals(item, edge));
        var native = Reach(read.Surface);
        Assert.Contains(read.Split, native); Assert.Contains(train.Surface, native); Assert.Contains(held.Surface, native);
        Assert.DoesNotContain(native, item => item is Carrier or CarrierLocation);
    }

    [Fact]
    public void RepeatedQuestionPhasesDoNotDuplicateCoverageOrRecurrenceSupport()
    {
        var f = new Fixture(); var a = f.Line(0, 6); var b = f.Line(3, 6);
        var small = f.Candidate(S(S(f.Frame.Right))); var redundant = f.Candidate(S(S(f.Frame.Right), S(f.Frame.Right)));
        var read = f.Heading(S(a), S(b), S(small, redundant));
        Assert.True(read.IsComplete); var one = read.Candidates[0].HeldOut; var two = read.Candidates[1].HeldOut;
        Assert.Equal(one.CoverageCount, two.CoverageCount); Assert.Equal(one.RecurrenceCount, two.RecurrenceCount);
        Assert.Equal(6, two.CoverageCount); Assert.Equal(1, two.RecurrenceCount);
        Assert.Equal(2, E(two.RecurrenceCorrespondence).Length);
        foreach (var alias in E(two.RecurrenceCorrespondence).Cast<Relation>())
            Assert.Same(Assert.Single(E(two.Recurrences)), alias.Right);
        Assert.True(one.DescriptionCount < two.DescriptionCount);
        Assert.Same(read.Candidates[0].Surface, Assert.Single(E(read.HeldOutPareto)));
        var dominance = Assert.IsType<Relation>(Assert.Single(E(read.HeldOutDominance)));
        Assert.Same(read.Candidates[0].Surface, ((Relation)dominance.Left).Left);
        Assert.Equal(4, E((Relation)dominance.Right).Length);
    }

    [Fact]
    public void OverlongSixPhaseHeadingCannotBeatTheSameThreePhaseMaterialRoute()
    {
        var f = new Fixture(); var a = f.Wave(0, 17); var b = f.Wave(4, 17);
        var phases = new[] { S(f.Frame.Right, f.Frame.Up), S(f.Frame.Right, f.Frame.Down), S(f.Frame.Right) };
        var read = f.Heading(S(a), S(b), S(f.Candidate(S(phases)), f.Candidate(S(phases.Concat(phases).Cast<IElement>().ToArray()))));
        Assert.True(read.IsComplete); var one = read.Candidates[0].HeldOut; var two = read.Candidates[1].HeldOut;
        Assert.Equal(17, one.CoverageCount); Assert.Equal(one.CoverageCount, two.CoverageCount);
        Assert.Equal(one.RecurrenceCount, two.RecurrenceCount); Assert.Equal(1, two.RecurrenceCount);
        Assert.True(two.DescriptionCount > one.DescriptionCount); Assert.True(E(two.RecurrenceCorrespondence).Length > 1);
        Assert.Same(read.Candidates[0].Surface, Assert.Single(E(read.HeldOutPareto)));
    }

    [Fact]
    public void HigherCoverageAndLowerCostCanRemainIncomparable()
    {
        var f = new Fixture(); var a = f.Zigzag(0, 6); var b = f.Zigzag(4, 6);
        var zig = f.Candidate(S(S(f.Frame.Right, f.Frame.Up), S(f.Frame.Right, f.Frame.Down)));
        var flat = f.Candidate(S(S(f.Frame.Right)));
        var read = f.Heading(S(a), S(b), S(zig, flat));
        Assert.True(read.IsComplete); Assert.Equal(6, read.Candidates[0].HeldOut.CoverageCount);
        Assert.Equal(0, read.Candidates[1].HeldOut.CoverageCount);
        Assert.True(read.Candidates[1].HeldOut.DescriptionCount < read.Candidates[0].HeldOut.DescriptionCount);
        Assert.Equal(2, E(read.HeldOutPareto).Length); Assert.Empty(E(read.HeldOutDominance));
        Assert.Single(E((Relation)read.Incomparable.Right));
    }

    [Fact]
    public void NoTrainingContinuationCanBorrowAnEdgeFromTheHeldOutHalf()
    {
        var f = new Fixture(); var all = f.Line(0, 2);
        var read = f.Heading(S(all[0]), S(all[1]), S(f.Candidate(S(S(f.Frame.Right)))));
        Assert.True(read.IsComplete); var c = Assert.Single(read.Candidates);
        Assert.Equal(0, c.Training.RecurrenceCount); Assert.Equal(0, c.HeldOut.RecurrenceCount);
        Assert.Empty(E(c.Training.Strands!.Continuations)); Assert.Empty(E(c.HeldOut.Strands!.Continuations));
        Assert.Equal(1, c.Training.UncoveredCount); Assert.Equal(1, c.HeldOut.UncoveredCount);
    }

    [Fact]
    public void EdgeScopeLeakageForeignCopiesAndOpenOrEmptySplitsStayVisible()
    {
        var f = new Fixture(); var a = f.Line(0, 3); var candidates = S(f.Candidate(S(S(f.Frame.Right))));
        var overlap = f.Heading(S(a), S(a[0]), candidates);
        Assert.False(overlap.IsComplete); Assert.NotEmpty(E(overlap.Blocked)); Assert.Empty(overlap.Candidates); Assert.Empty(E(overlap.Pareto));
        var copy = new Relation(((Relation)a[0]).Left, ((Relation)a[0]).Right);
        var foreign = f.Heading(S(a[0]), S(copy), candidates);
        Assert.False(foreign.IsComplete); Assert.NotEmpty(E(foreign.Blocked));
        var open = f.Heading(StructuralConstraint.Open(a[0]), S(a[1]), candidates);
        Assert.False(open.IsComplete); Assert.NotEmpty(E(open.Unresolved)); Assert.Empty(E(open.Pareto));
        var empty = f.Heading(S(), S(a[1]), candidates);
        Assert.False(empty.IsComplete); Assert.NotEmpty(E(empty.Unresolved)); Assert.Empty(E(empty.Pareto));
    }

    [Fact]
    public void UnselectedEdgesRemainCarriedAndDoNotEnterEitherEvidenceRead()
    {
        var f = new Fixture(); var a = f.Line(0, 2); var b = f.Line(3, 2); var extra = f.Line(6, 2);
        var read = f.Heading(S(a), S(b), S(f.Candidate(S(S(f.Frame.Right)))));
        Assert.True(read.IsComplete); Assert.Equal(2, E(read.Carried).Length);
        for (var i = 0; i < 2; i++) Assert.Same(extra[i], E(read.Carried)[i]);
    }

    [Fact]
    public void UnresolvedHeadingBindingsCannotEarnAZeroViolationSelection()
    {
        var f = new Fixture(); var a = f.Line(0, 3); var b = f.Line(3, 3);
        f.Bindings.RemoveAt(0);
        var read = f.Heading(S(a), S(b), S(f.Candidate(S(S(f.Frame.Right)))));
        Assert.False(read.IsComplete); var c = Assert.Single(read.Candidates);
        Assert.False(c.Training.IsComplete); Assert.NotEmpty(E(c.Training.Unresolved));
        Assert.Empty(E(read.TrainingPareto)); Assert.Single(E(read.HeldOutPareto));
    }

    [Fact]
    public void TranslationPartitionsUseActualOccurrencesAndExactRepeatedWords()
    {
        var f = new Fixture(); var a = f.Row(0, 5, 6); var b = f.Row(3, 5, 6);
        var good = f.Candidate(S(f.Word(6, 0))); var wrong = f.Candidate(S(f.Word(5, 0)));
        var read = f.Translation(S(a), S(b), S(good, wrong));
        Assert.True(read.IsComplete); var c = read.Candidates[0];
        Assert.Equal(3, c.Training.RecurrenceCount); Assert.Equal(5, c.Training.CoverageCount);
        Assert.Equal(3, c.HeldOut.RecurrenceCount); Assert.Equal(5, c.HeldOut.CoverageCount);
        Assert.Equal(0, read.Candidates[1].HeldOut.CoverageCount);
        Assert.Equal(25, E(c.HeldOut.Rhythm!.Comparisons).Length); // 5x5, never the full 10x10 supply
        Assert.Equal(5, E(c.HeldOut.Rhythm.Bindings).Length);
        Assert.Equal(6, c.HeldOut.DescriptionCount); Assert.Equal(5, read.Candidates[1].HeldOut.DescriptionCount);
        Assert.Equal(2, E(read.HeldOutPareto).Length); // simpler failed question and useful longer question are incomparable
    }

    [Fact]
    public void OccurrenceAliasesCannotBeSplitAcrossTrainingAndHeldOut()
    {
        var f = new Fixture(); var a = new Placeholder(); var b = new Placeholder();
        f.Bindings.Add(new Relation(a, f.Word(0, 0))); f.Bindings.Add(new Relation(b, f.Word(1, 0)));
        var read = OrnamentQuestionComparison.Translation(f.Frame, f.Source, S(a, b), S(f.Bindings.ToArray()),
            S(a), S(a.Dual().Dual()), S(f.Candidate(S(f.Word(1, 0)))), S(new Placeholder(), new Placeholder()), new Placeholder());
        Assert.False(read.IsComplete); Assert.NotEmpty(E(read.Blocked)); Assert.Empty(E(read.Pareto));
    }

    [Fact]
    public void TranslationConflictsRemainAnExplicitPenaltyRatherThanUnknownEvidence()
    {
        var f = new Fixture(); var a = f.Row(0, 4, 1); var b = f.Row(3, 4, 1);
        var read = f.Translation(S(a), S(b), S(f.Candidate(S(f.Word(1, 0), f.Word(2, 0)))));
        Assert.True(read.IsComplete); var measure = Assert.Single(read.Candidates).HeldOut;
        Assert.NotEmpty(E(measure.Rhythm!.Conflicts)); Assert.NotEmpty(E(measure.Branching));
        Assert.Empty(E(measure.Unresolved)); Assert.True(measure.IsComplete);
        foreach (var conflict in E(measure.Rhythm.Conflicts))
            Assert.Contains(E(measure.Branching), item => ReferenceEquals(item, conflict));
    }

    [Fact]
    public void EqualQuestionsKeepSeparateIdentityAndTiedEvidence()
    {
        var f = new Fixture(); var a = f.Line(0, 3); var b = f.Line(3, 3); var grammar = S(S(f.Frame.Right));
        var read = f.Heading(S(a), S(b), S(f.Candidate(grammar), f.Candidate(grammar)));
        Assert.True(read.IsComplete); Assert.Equal(2, E(read.Pareto).Length);
        Assert.Single(E((Relation)read.Ties.Right)); Assert.Empty(E(read.HeldOutDominance));
        Assert.NotSame(read.Candidates[0].Identity, read.Candidates[1].Identity);
    }

    private sealed class Fixture
    {
        public QualitativeMotionFrame Frame { get; } = new();
        public IElement Source { get; } = new Placeholder();
        public IElement Reach { get; } = new Placeholder();
        public List<IElement> Sites { get; } = [];
        public List<IElement> Edges { get; } = [];
        public List<IElement> Bindings { get; } = [];
        private IElement At(int x, int y)
        { var site = new Placeholder(); Sites.Add(site); Bindings.Add(new Relation(site, Word(x, y))); return site; }
        public Relation Word(int x, int y) => S(Enumerable.Repeat(QualitativeMotionFrame.Move(x < 0 ? Frame.Left : Frame.Right, Reach), Math.Abs(x))
            .Concat(Enumerable.Repeat(QualitativeMotionFrame.Move(y < 0 ? Frame.Down : Frame.Up, Reach), Math.Abs(y))).Cast<IElement>().ToArray());
        public IElement[] Row(int y, int count, int step) => Enumerable.Range(0, count).Select(i => At(i * step, y)).ToArray();
        public IElement[] Line(int y, int count) => Connect(Row(y, count + 1, 1));
        public IElement[] Zigzag(int y, int count) => Connect(Enumerable.Range(0, count + 1).Select(i => At(i, y + i % 2)).ToArray());
        public IElement[] Wave(int y, int count) => Connect(Enumerable.Range(0, count + 1).Select(i => At(i, y + (i % 3 == 1 ? 1 : 0))).ToArray());
        private IElement[] Connect(IElement[] sites) => sites.Zip(sites.Skip(1), (a, b) =>
        { IElement edge = new Relation(new Placeholder(), new Relation(a, b)); Edges.Add(edge); return edge; }).ToArray();
        public Relation Candidate(Relation content) => new(new Placeholder(), content);
        public OrnamentQuestionComparison Heading(Relation a, Relation b, Relation candidates) => OrnamentQuestionComparison.Heading(Frame, Source,
            new Relation(S(Sites.ToArray()), new Relation(S(Edges.ToArray()), S())), S(Bindings.ToArray()), a, b, candidates, new Placeholder());
        public OrnamentQuestionComparison Translation(Relation a, Relation b, Relation candidates) => OrnamentQuestionComparison.Translation(Frame, Source,
            S(Sites.ToArray()), S(Bindings.ToArray()), a, b, candidates, S(new Placeholder(), new Placeholder()), new Placeholder());
    }
    private static HashSet<IElement> Reach(IElement root)
    {
        var seen = new HashSet<IElement>(ReferenceEqualityComparer.Instance); var todo = new Stack<IElement>(); todo.Push(root);
        while (todo.TryPop(out var item)) { if (!seen.Add(item)) continue; if (item is Relation r) { todo.Push(r.Left); todo.Push(r.Right); } }
        return seen;
    }
    private static Relation S(params IElement[] entries) => StructuralConstraint.Closed(entries);
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
}
