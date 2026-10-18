using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeFrieze;

namespace Gojo.Experiments.QualitativeExecution.Tests;

public sealed class OrnamentOccurrenceRhythmTests
{
    [Fact]
    public void TwoDeclaredTranslationsDiscoverCheckerboardThroughNativeSuccessors()
    {
        var f = new Fixture(); var items = Enumerable.Range(0, 6).Select(_ => (IElement)new Placeholder()).ToArray();
        var bindings = S(items.Select((item, i) => (IElement)new Relation(item, f.Word(i % 3, i / 3))).ToArray());
        var steps = S(f.Word(1, 0), f.Word(0, 1));
        var read = f.Read(S(items), bindings, steps);
        Assert.True(read.IsComplete); Assert.Equal(7, E(read.Successors).Length);
        Assert.Equal(72, E(read.Comparisons).Length); Assert.Single(E(read.Roots)); Assert.Single(E(read.Components));
        Assert.Equal(6, E(read.Assignments).Length); Assert.Empty(E(read.Conflicts));
        for (var i = 0; i < items.Length; i++)
        {
            Assert.True(read.TryReadAssignment(items[i], out var phase));
            Assert.Same((i % 3 + i / 3) % 2 == 0 ? f.First : f.Second, phase);
        }
        foreach (var edge in E(read.Successors).Cast<Relation>())
        {
            Assert.True(read.TryReadSuccessor(edge, out var a, out var b, out var step, out var proof));
            Assert.Contains(E(steps), actual => ReferenceEquals(actual, step)); Assert.Same(edge.Left, proof);
            Assert.True(read.TryReadAssignment(a, out var first)); Assert.True(read.TryReadAssignment(b, out var second));
            Assert.NotSame(first, second);
        }
        var native = Reach(read.Surface); Assert.Contains(f.Source, native); Assert.Contains(f.Question, native);
        Assert.Contains(bindings, native); Assert.Contains(steps, native);
        Assert.DoesNotContain(native, item => item is Carrier or CarrierLocation);
    }

    [Fact]
    public void ChangingOccurrenceEnumerationCannotReplaceAdjacencyWithIndexParity()
    {
        var f = new Fixture(); var a = new Placeholder(); var b = new Placeholder(); var c = new Placeholder();
        var read = f.Read(S(a, c, b), S(new Relation(a, f.Word(0, 0)), new Relation(b, f.Word(1, 0)),
            new Relation(c, f.Word(2, 0))), S(f.Word(1, 0)));
        Assert.True(read.IsComplete); Assert.Equal(2, E(read.Successors).Length);
        Assert.True(read.TryReadAssignment(a, out var first)); Assert.True(read.TryReadAssignment(c, out var last));
        Assert.True(read.TryReadAssignment(b, out var middle)); Assert.Same(first, last); Assert.NotSame(first, middle);
    }

    [Fact]
    public void OddCycleKeepsConflictsAndProposalsButPublishesOnlyUnaffectedComponent()
    {
        var f = new Fixture(); var items = Enumerable.Range(0, 4).Select(_ => (IElement)new Placeholder()).ToArray();
        var bindings = S(new Relation(items[0], f.Word(0, 0)), new Relation(items[1], f.Word(1, 0)),
            new Relation(items[2], f.Word(2, 0)), new Relation(items[3], f.Word(0, 5)));
        var read = f.Read(S(items), bindings, S(f.Word(1, 0), f.Word(2, 0)));
        Assert.False(read.IsComplete); Assert.Equal(3, E(read.Successors).Length);
        Assert.Equal(2, E(read.Roots).Length); Assert.Equal(2, E(read.Components).Length);
        Assert.NotEmpty(E(read.Conflicts)); Assert.Single(E(read.Assignments)); Assert.Empty(E(read.Unresolved));
        for (var i = 0; i < 3; i++) Assert.False(read.TryReadAssignment(items[i], out _));
        Assert.True(read.TryReadAssignment(items[3], out var phase)); Assert.Same(f.First, phase);
        foreach (var conflict in E(read.Conflicts).Cast<Relation>())
        {
            var pair = Assert.IsType<Relation>(conflict.Right);
            Assert.Same(Assert.IsType<Relation>(pair.Left).Right, Assert.IsType<Relation>(pair.Right).Right);
            Assert.Contains(E(read.Successors), edge => ReferenceEquals(edge, conflict.Left));
        }
    }

    [Fact]
    public void EqualPositionsRemainSeparateOccurrencesAndReceiveSeparateVisibleRoots()
    {
        var f = new Fixture(); var a = new Placeholder(); var b = new Placeholder();
        var read = f.Read(S(a, b), S(new Relation(a, S()), new Relation(b, S())), S(f.Word(1, 0)));
        Assert.True(read.IsComplete); Assert.Empty(E(read.Successors)); Assert.Equal(2, E(read.Roots).Length);
        Assert.Equal(2, E(read.Components).Length); Assert.Equal(2, E(read.Assignments).Length);
        Assert.True(read.TryReadAssignment(a, out var first)); Assert.True(read.TryReadAssignment(b, out var second));
        Assert.Same(first, second); Assert.NotSame(a, b);
    }

    [Fact]
    public void DifferentReachIdentitiesLeaveComparisonUnresolvedRatherThanInventRepeat()
    {
        var f = new Fixture(); var a = new Placeholder(); var b = new Placeholder();
        var other = QualitativeMotionFrame.Move(f.Frame.Right, new Placeholder());
        var read = f.Read(S(a, b), S(new Relation(a, f.Word(1, 0)), new Relation(b, S(other))), S(f.Word(1, 0)));
        Assert.False(read.IsComplete); Assert.Empty(E(read.Successors)); Assert.Empty(E(read.Assignments));
        Assert.NotEmpty(E(read.Unresolved)); Assert.Equal(4, E(read.Comparisons).Length);
    }

    [Fact]
    public void RepeatedStepParticipationIsExactAndRetainsActualBindingsAndCancellationWitnesses()
    {
        var f = new Fixture(); var a = new Placeholder(); var b = new Placeholder(); var c = new Placeholder();
        var first = new Relation(a, f.Word(0, 0)); var second = new Relation(b, f.Word(2, 0));
        var third = new Relation(c, f.Word(3, 0)); var step = f.Word(2, 0);
        var read = f.Read(S(a, b, c), S(first, second, third), S(step));
        Assert.True(read.IsComplete); var edge = Assert.IsType<Relation>(Assert.Single(E(read.Successors)));
        Assert.True(read.TryReadSuccessor(edge, out var from, out var to, out var actualStep, out var proof));
        Assert.Same(a, from); Assert.Same(b, to); Assert.Same(step, actualStep);
        Assert.Contains(first, Reach(proof)); Assert.Contains(second, Reach(proof));
        Assert.False(read.TryReadSuccessor(new Relation(edge.Left, edge.Right), out _, out _, out _, out _));
    }

    [Fact]
    public void ZeroOrOpenStepsAndOpenOrAliasedPhasesCannotEstablishRhythm()
    {
        var f = new Fixture(); var a = new Placeholder(); var bindings = S(new Relation(a, S()));
        var zero = S(QualitativeMotionFrame.Move(f.Frame.Right, f.Reach), QualitativeMotionFrame.Move(f.Frame.Left, f.Reach));
        var cases = new[]
        {
            (S(), f.Phases),
            (S(S()), f.Phases),
            (S(zero), f.Phases),
            (StructuralConstraint.Open(f.Word(1, 0)), f.Phases),
            (S(StructuralConstraint.Open(QualitativeMotionFrame.Move(f.Frame.Right, f.Reach))), f.Phases),
            (S(f.Word(1, 0)), StructuralConstraint.Open(f.First, f.Second)),
            (S(f.Word(1, 0)), S(f.First, f.First.Dual().Dual())),
            (S(f.Word(1, 0)), S(f.First, f.Second.Dual()))
        };
        foreach (var (steps, phases) in cases)
        {
            var read = new OrnamentOccurrenceRhythm(f.Frame, f.Source, S(a), bindings, steps, phases, f.Question);
            Assert.False(read.IsComplete); Assert.NotEmpty(E(read.Unresolved)); Assert.Empty(E(read.Assignments));
        }
    }

    [Fact]
    public void MissingAmbiguousOpenOrDuplicateOccurrenceSupplyCannotPublishLabels()
    {
        var f = new Fixture(); var a = new Placeholder(); var binding = new Relation(a, S());
        var cases = new[]
        {
            (S(a), S()),
            (S(a), S(binding, new Relation(a.Dual().Dual(), S()))),
            (S(a), StructuralConstraint.Open(binding)),
            (S(a), S(new Relation(a, StructuralConstraint.Open()))),
            (S(a, a.Dual().Dual()), S(binding)),
            (StructuralConstraint.Open(a), S(binding))
        };
        foreach (var (items, bindings) in cases)
        {
            var read = f.Read(items, bindings, S(f.Word(1, 0)));
            Assert.False(read.IsComplete); Assert.NotEmpty(E(read.Unresolved)); Assert.Empty(E(read.Assignments));
        }
    }

    [Fact]
    public void EmptySelectionIsCompleteObservationAndUnrelatedBindingsAreNotConsumed()
    {
        var f = new Fixture(); var other = new Relation(new Placeholder(), S());
        var read = f.Read(S(), S(other), S(f.Word(1, 0)));
        Assert.True(read.IsComplete); Assert.Empty(E(read.Comparisons)); Assert.Empty(E(read.Roots));
        Assert.Empty(E(read.Assignments)); Assert.Contains(other, Reach(read.Surface));
    }

    private sealed class Fixture
    {
        internal QualitativeMotionFrame Frame { get; } = new();
        internal IElement Reach { get; } = new Placeholder();
        internal IElement Source { get; } = new Placeholder();
        internal IElement Question { get; } = new Placeholder();
        internal Placeholder First { get; } = new();
        internal Placeholder Second { get; } = new();
        internal Relation Phases => S(First, Second);
        internal Relation Word(int x, int y) => S(Enumerable.Repeat(QualitativeMotionFrame.Move(x < 0 ? Frame.Left : Frame.Right, Reach), Math.Abs(x))
            .Concat(Enumerable.Repeat(QualitativeMotionFrame.Move(y < 0 ? Frame.Down : Frame.Up, Reach), Math.Abs(y))).Cast<IElement>().ToArray());
        internal OrnamentOccurrenceRhythm Read(Relation items, Relation bindings, Relation steps) =>
            new(Frame, Source, items, bindings, steps, Phases, Question);
    }
    private static Relation S(params IElement[] values) => StructuralConstraint.Closed(values);
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
    private static HashSet<IElement> Reach(IElement source)
    {
        var seen = new HashSet<IElement>(ReferenceEqualityComparer.Instance); var work = new Stack<IElement>(); work.Push(source);
        while (work.TryPop(out var item))
        {
            if (!seen.Add(item)) continue;
            if (item is Relation relation) { work.Push(relation.Left); work.Push(relation.Right); }
        }
        return seen;
    }
}
