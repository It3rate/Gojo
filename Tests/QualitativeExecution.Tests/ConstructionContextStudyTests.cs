using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeFrieze;

namespace Gojo.Experiments.QualitativeExecution.Tests;

public sealed class ConstructionContextStudyTests
{
    private static readonly Lazy<ConstructionContextStudy> Study = new(ConstructionContextStudy.Build);

    [Fact]
    public void EverySuppliedStepExecutesItsNativePathContractWithoutNumericParticipants()
    {
        Assert.Equal(4, Study.Value.Cases.Count);
        var steps = Study.Value.Cases.SelectMany(c => c.Variants).SelectMany(v => v.Steps).ToArray();
        Assert.Equal(17, steps.Length);
        foreach (var step in steps)
        {
            var node = new UnitArrangementNode(step.Id, [step.Program], step.Program, S());
            Assert.True(node.Current.Join.TryReadPassed(node.Current.Join.Evaluate()), step.Id);
            Assert.All(Reachable(step.Surface), item => Assert.True(item is Relation or Placeholder, step.Id));
        }
        Assert.All(Reachable(Study.Value.Surface), item => Assert.True(item is Relation or Placeholder));
    }

    [Fact]
    public void EqualOrderedSupportIsProvedWithoutEquatingDistinctPathsOrClosureContracts()
    {
        var source = Step("C01-A1").Program; var target = Step("C01-B1").Program;
        var support = Support(source, target); var read = support.Evaluate();
        Assert.True(support.TryReadComplete(read));
        Assert.True(support.TryReadResult(read, out var matched, out var blocked, out var pending));
        Assert.Equal(4, E(matched).Length); Assert.Empty(E(blocked)); Assert.Empty(E(pending));
        Assert.Single(E(source.Paths)); Assert.Equal(4, E(target.Paths).Length);
        Assert.Single(E(source.ClosedPaths)); Assert.Empty(E(target.ClosedPaths));
        var path = (Relation)E(source.Paths).Single();
        Assert.DoesNotContain(E(target.Paths), item => ReferenceEquals(item, path));
        foreach (var row in E(matched).Cast<Relation>())
        {
            var reductions = (Relation)row.Right;
            foreach (var reduction in new[] { (Relation)reductions.Left, (Relation)reductions.Right })
            {
                Assert.True(source.Frame.TryReadResult(reduction, out var remainder, out _, out var unresolved));
                Assert.Empty(E(remainder)); Assert.Empty(E(unresolved));
            }
        }
    }

    [Fact]
    public void ReplacementAvailabilityComesFromActualScopeMembershipAndCarriesOtherPaths()
    {
        var denied = Step("C01-A2").Ladder.Current; var moved = Step("C01-B2").Ladder.Current;
        Assert.NotEmpty(E(denied.Blocked)); Assert.False(denied.IsComplete);
        Assert.Same(Step("C01-A1").Program, denied.Program);
        Assert.True(moved.IsComplete); Assert.Empty(E(moved.Blocked));
        Assert.Equal(3, E(moved.Carried).Length);
        var originalTop = E(Step("C01-B1").Program.Paths)[2];
        Assert.Same(originalTop, E(moved.Focus).Single());
        Assert.DoesNotContain(E(moved.Program.Paths), path => ReferenceEquals(path, originalTop));
        Assert.Empty(E(moved.Program.ClosedPaths));
        // The two sources are not unrelated success/failure flags: the exact
        // same actual side is a member of only one supplied path scope.
        Assert.Same(originalTop, E(denied.Focus).Single());
    }

    [Fact]
    public void ReformulationWithdrawsClosureThenRestoresANewExplicitClosedWalk()
    {
        var start = Step("C02-A1"); var split = Step("C02-A2"); var moved = Step("C02-A3"); var closed = Step("C02-A4");
        Assert.Single(E(start.Program.ClosedPaths)); Assert.Empty(E(split.Program.ClosedPaths));
        Assert.Empty(E(moved.Program.ClosedPaths)); Assert.Single(E(closed.Program.ClosedPaths));
        Assert.True(split.Ladder.Current.IsComplete); Assert.True(closed.Ladder.Current.IsComplete);
        Assert.Same(E(start.Program.Paths).Single(), E(split.Ladder.Current.Focus).Single());
        Assert.Empty(E(split.Ladder.Current.Carried));
        var publicationLoss = E((Relation)split.Ladder.Current.Correspondence.Right).Cast<Relation>().ToArray();
        Assert.All(publicationLoss, row => Assert.Empty(E((Relation)row.Right)));
        Assert.True(closed.Ladder.TryReadAncestor(start.Program.Surface, out var actual, out _));
        Assert.Same(start.Program, actual);
        var path = (Relation)E(closed.Program.Paths).Single(); var words = E((Relation)path.Right).Cast<Relation>().ToArray();
        Assert.Equal(3, E(words[0]).Length); Assert.Equal(4, E(words[1]).Length);
        Assert.Equal(3, E(words[2]).Length); Assert.Equal(4, E(words[3]).Length);
        Assert.NotSame(E(start.Program.Paths).Single(), path);
    }

    [Fact]
    public void ChordRefinementRetainsEndpointsButDoesNotClaimCurrentStraightness()
    {
        var coarse = Step("C03-A1"); var single = Step("C03-A2"); var fine = Step("C03-A3");
        var coarsePath = (Relation)E(coarse.Program.Paths).Single();
        var finePath = (Relation)E(fine.Program.Paths).Single();
        Assert.Equal(4, E((Relation)coarsePath.Right).Length);
        Assert.Equal(8, E((Relation)((Relation)E(single.Program.Paths).Single()).Right).Length);
        Assert.Equal(20, E((Relation)finePath.Right).Length);
        Assert.True(fine.Ladder.TryReadAncestor(coarse.Program.Surface, out var ancestor, out var witness));
        Assert.Same(coarse.Program, ancestor); Assert.Contains(coarse.Program.Surface, Reachable(witness));
        var proof = (Relation)((Relation)fine.Program.Evidence).Right;
        foreach (var row in E((Relation)proof.Right).Cast<Relation>())
        {
            var equalEnds = (Relation)((Relation)row.Right).Right;
            Assert.True(fine.Program.Frame.TryReadResult(equalEnds, out var remainder, out _, out var pending));
            Assert.Empty(E(remainder)); Assert.Empty(E(pending));
        }
        // The coarse word is an ancestor, not an actual current fine chord.
        var oldWord = E((Relation)coarsePath.Right)[1];
        Assert.DoesNotContain(E((Relation)finePath.Right), item => ReferenceEquals(item, oldWord));
    }

    [Fact]
    public void RepetitionSharesDetailedUnitAndRetainsItsPhaseAndStepConstruction()
    {
        var detailed = Step("C03-A3").Program; var repeated = Step("C03-A4");
        var originalPath = (Relation)E(detailed.Paths).Single();
        var paths = E(repeated.Program.Paths).Cast<Relation>().ToArray();
        Assert.Equal(4, paths.Length); Assert.All(paths, path => Assert.Same(originalPath.Right, path.Right));
        Assert.Equal(4, E(repeated.Program.ClosedPaths).Length);
        var run = Assert.IsType<OrnamentProgramRun>(repeated.RepeatRun);
        Assert.True(run.IsComplete); Assert.Equal(4, run.Units.Count);
        Assert.Same(detailed.Surface, E(run.Program.Motifs).Single());
        Assert.Equal(4, E(run.Applications).Length);
        Assert.Contains(run.Program.Layout, Reachable(repeated.Surface));
        Assert.All(run.Units, unit => Assert.True(unit.Current.Join.TryReadPassed(unit.Current.Join.Evaluate())));
    }

    [Fact]
    public void IndependentCapturePreservesShapeAliasesButDoesNotImportAncestorOrSelectors()
    {
        var retained = Step("C04-A1"); var captured = Step("C04-B1");
        var oldSquare = Step("C03-A1").Program;
        Assert.True(retained.Ladder.TryReadAncestor(oldSquare.Surface, out _, out _));
        Assert.False(captured.Ladder.TryReadAncestor(oldSquare.Surface, out _, out _));
        Assert.Single(captured.Ladder.Rungs); Assert.Empty(E((Relation)captured.Program.Evidence));
        var source = Reachable(retained.Program.Surface); var target = Reachable(captured.Program.Surface);
        Assert.DoesNotContain(source, old => target.Any(fresh => ReferenceEquals(old, fresh)));
        Assert.NotSame(retained.Program.Frame, captured.Program.Frame);
        Assert.Equal(E(retained.Program.Paths).Length, E(captured.Program.Paths).Length);
        var pairs = E(captured.Receipts[0]).Cast<Relation>().ToArray();
        var sourcePath = E(retained.Program.Paths).Single(); var targetPath = E(captured.Program.Paths).Single();
        Assert.Single(pairs, pair => ReferenceEquals(pair.Left, sourcePath) && ReferenceEquals(pair.Right, targetPath));
        Assert.Contains(oldSquare.Surface, Reachable(captured.Receipts[1]));
        Assert.DoesNotContain(captured.Receipts[1], target);
        var unknown = Step("C04-B2").Receipts.Single();
        var unknownParts = (Relation)unknown.Right;
        Assert.True(StructuralConstraint.TryReadScope((Relation)unknownParts.Left, out _, out var seam, out var tail));
        Assert.NotEqual(seam, tail);
        var refused = Step("C04-B3").Ladder.Current;
        Assert.False(refused.IsComplete); Assert.NotEmpty(E(refused.Blocked)); Assert.Same(captured.Program, refused.Program);
    }

    [Fact]
    public void SupportRejectsCopiedForeignMissingAndOpenSelectorsAndForgedSuccess()
    {
        var source = Step("C01-A1").Program; var target = Step("C01-B1").Program;
        var full = Support(source, target); var pairs = E(full.Pairings).Cast<Relation>().ToArray();
        var first = pairs[0]; var selector = (Relation)first.Left;
        var copiedPath = new Relation(((Relation)selector.Left).Left, ((Relation)selector.Left).Right);
        var foreign = new ConstructionSegmentSupport(source, target,
            S([new Relation(new Relation(copiedPath, selector.Right), first.Right), .. pairs.Skip(1)]));
        Assert.True(foreign.TryReadResult(foreign.Evaluate(), out _, out var blocked, out var missing));
        Assert.NotEmpty(E(blocked)); Assert.NotEmpty(E(missing));
        var partial = new ConstructionSegmentSupport(source, target, S(pairs.Skip(1).Cast<IElement>().ToArray()));
        Assert.False(partial.TryReadComplete(partial.Evaluate()));
        var open = new ConstructionSegmentSupport(source, target, StructuralConstraint.Open(pairs));
        Assert.True(open.TryReadResult(open.Evaluate(), out _, out _, out var pending)); Assert.NotEmpty(E(pending));
        var fabricated = new Relation(foreign.Surface, new Relation(S(), new Relation(S(), S())));
        Assert.False(foreign.TryReadResult(fabricated, out _, out _, out _));
        var actual = full.Evaluate();
        var omitted = new Relation(full.Surface, new Relation(S(), ((Relation)actual.Right).Right));
        Assert.False(full.TryReadResult(omitted, out _, out _, out _));
    }

    [Fact]
    public void LookalikeActualOccurrencesRemainDistinctInSupportCoverage()
    {
        var author = new LatticeLadderAuthor(); var word = author.Word(1, 0);
        var a = LatticeLadderAuthor.Path(S(), word); var b = new Relation(a.Left, a.Right);
        var c = LatticeLadderAuthor.Path(S(), word); var d = new Relation(c.Left, c.Right);
        var source = new UnitArrangementProgram(author.Frame, "two actual lookalikes", S(a, b), S());
        var target = new UnitArrangementProgram(author.Frame, "two more actual lookalikes", S(c, d), S());
        Relation Pair(Relation from, Relation to) => new(new Relation(from, word), new Relation(to, word));
        var complete = new ConstructionSegmentSupport(source, target, S(Pair(a, c), Pair(b, d)));
        Assert.True(complete.TryReadComplete(complete.Evaluate()));
        var missing = new ConstructionSegmentSupport(source, target, S(Pair(a, c)));
        Assert.True(missing.TryReadResult(missing.Evaluate(), out var yes, out var no, out var pending));
        Assert.Single(E(yes)); Assert.Empty(E(no)); Assert.Equal(2, E(pending).Length);
        var duplicate = new ConstructionSegmentSupport(source, target, S(Pair(a, c), Pair(a, d)));
        Assert.False(duplicate.TryReadComplete(duplicate.Evaluate()));
    }

    [Fact]
    public void OpenFocusCannotAccidentallyWithdrawTheCurrentConstruction()
    {
        var original = Step("C02-A1").Ladder; var actual = E(original.Current.Program.Paths).Single();
        var result = original.ReplacePaths("open focus", StructuralConstraint.Open(actual), S(), S(), S());
        Assert.False(result.Current.IsComplete); Assert.NotEmpty(E(result.Current.Open));
        Assert.Same(original.Current.Program, result.Current.Program);
    }

    [Fact]
    public void ForeignFramesAndEqualLookingIndependentReachesDoNotSupplyFormalEquality()
    {
        var source = Step("C01-A1").Program;
        var differentReach = new LatticeLadderAuthor(source.Frame, new Placeholder());
        var path = LatticeLadderAuthor.Path(S(), differentReach.Word(3, 0), differentReach.Word(0, 3), differentReach.Word(-3, 0), differentReach.Word(0, -3));
        var target = new UnitArrangementProgram(source.Frame, "different reach", S(path), S(), S(path));
        var pairs = ZipPaths(source, target); var comparison = new ConstructionSegmentSupport(source, target, pairs);
        Assert.False(comparison.TryReadComplete(comparison.Evaluate()));
        Assert.True(comparison.TryReadResult(comparison.Evaluate(), out _, out var blocked, out _)); Assert.NotEmpty(E(blocked));
        var other = new LatticeLadderAuthor();
        var foreignPath = LatticeLadderAuthor.Path(S(), other.Word(3, 0), other.Word(0, 3), other.Word(-3, 0), other.Word(0, -3));
        var foreign = new UnitArrangementProgram(other.Frame, "foreign frame", S(foreignPath), S(), S(foreignPath));
        var frameComparison = new ConstructionSegmentSupport(source, foreign, ZipPaths(source, foreign));
        Assert.False(frameComparison.TryReadComplete(frameComparison.Evaluate()));
    }

    [Fact]
    public void NumericEvidenceCannotEnterTheNativeContextSupportContract()
    {
        var source = Step("C01-A1").Program;
        var measured = new UnitArrangementProgram(source.Frame, "measured annotation", source.Paths, source.Ports,
            source.ClosedPaths, new CarrierLocation(new Carrier(), 0));
        Assert.Throws<ArgumentException>(() => new ConstructionSegmentSupport(source, measured, S()));
    }

    private static ConstructionContextStudy.Step Step(string id) => Study.Value.Cases.SelectMany(c => c.Variants).SelectMany(v => v.Steps).Single(s => s.Id == id);
    private static ConstructionSegmentSupport Support(UnitArrangementProgram source, UnitArrangementProgram target)
    {
        var path = (Relation)E(source.Paths).Single(); var words = E((Relation)path.Right); var sides = E(target.Paths).Cast<Relation>().ToArray();
        return new(source, target, S(words.Zip(sides, (word, side) => (IElement)new Relation(new Relation(path, word), new Relation(side, word))).ToArray()));
    }
    private static Relation ZipPaths(UnitArrangementProgram source, UnitArrangementProgram target)
    {
        var a = (Relation)E(source.Paths).Single(); var b = (Relation)E(target.Paths).Single();
        return S(E((Relation)a.Right).Zip(E((Relation)b.Right), (x, y) => (IElement)new Relation(new Relation(a, x), new Relation(b, y))).ToArray());
    }
    private static HashSet<IElement> Reachable(IElement source)
    {
        var seen = new HashSet<IElement>(ReferenceEqualityComparer.Instance); var pending = new Stack<IElement>(); pending.Push(source);
        while (pending.TryPop(out var item)) if (seen.Add(item) && item is Relation relation) { pending.Push(relation.Left); pending.Push(relation.Right); }
        return seen;
    }
    private static Relation S(params IElement[] values) => StructuralConstraint.Closed(values);
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
}
