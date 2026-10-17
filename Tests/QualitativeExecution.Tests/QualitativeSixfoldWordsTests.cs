using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeFrieze;

namespace Gojo.Experiments.QualitativeExecution.Tests;

public sealed class QualitativeSixfoldWordsTests
{
    [Fact]
    public void NativeActionIsSubstitutionOverSharedReachWordsRatherThanSixStateLabels()
    {
        var frame = new QualitativeMotionFrame(); var reach = new Placeholder(); var action = new QualitativeSixfoldWords(frame, reach);
        Assert.Same(frame, action.Frame); Assert.Same(reach, action.Reach);
        Assert.Equal(6, E(action.Orbit).Length); Assert.Equal(6, E(action.Derivations).Length);
        Assert.Same(action.U, E(action.Orbit)[0]);
        Assert.DoesNotContain(Traverse(action.Surface), element => element is Carrier or CarrierLocation);
        foreach (var row in E(action.Turn.Rows).Cast<Relation>())
        {
            Assert.Same(reach, Assert.IsType<Relation>(row.Left).Right);
            Assert.All(E(Assert.IsType<Relation>(row.Right)).Cast<Relation>(), move => Assert.Same(reach, move.Right));
        }
        var basisV = Assert.IsType<Relation>(Assert.Single(E(action.V)));
        var image = Assert.Single(E(action.Turn.Rows).Cast<Relation>(), row => ReferenceEquals(Assert.IsType<Relation>(row.Left).Left, frame.Up));
        Assert.Equal(2, E(Assert.IsType<Relation>(image.Right)).Length);
        Assert.Same(basisV.Left, Assert.IsType<Relation>(E(Assert.IsType<Relation>(image.Right))[0]).Left);
        Assert.Same(frame.Left, Assert.IsType<Relation>(E(Assert.IsType<Relation>(image.Right))[1]).Left);
        AssertZero(frame, action.HalfTurnRead); AssertZero(frame, action.FullTurnRead);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void BasisAndThirdDirectionHaveExactOrderSixAndHalfTurnNegation(int direction)
    {
        var frame = new QualitativeMotionFrame(); var action = new QualitativeSixfoldWords(frame, new Placeholder());
        var seed = direction == 0 ? action.U : direction == 1 ? action.V : Assert.IsType<Relation>(E(action.Orbit)[2]);
        var current = seed;
        for (var step = 1; step <= 6; step++)
        {
            var proof = action.Apply(current); Assert.True(action.TryReadComplete(proof, out current));
            var comparison = frame.Reduce(current, Inverse(frame, seed));
            Assert.True(frame.TryReadResult(comparison, out var residual, out _, out var pending)); Assert.Empty(E(pending));
            if (step < 6) Assert.NotEmpty(E(residual)); else Assert.Empty(E(residual));
            if (step == 3) AssertZero(frame, frame.Reduce(current, seed));
        }
    }

    [Fact]
    public void OrbitRetainsEveryActualInputSubstitutionAndFormalReduction()
    {
        var action = new QualitativeSixfoldWords(new QualitativeMotionFrame(), new Placeholder()); var orbit = E(action.Orbit);
        var derivations = E(action.Derivations).Cast<Relation>().ToArray();
        for (var index = 0; index < derivations.Length; index++)
        {
            var source = Assert.IsType<Relation>(derivations[index].Left);
            Assert.Same(action.Turn.Surface, source.Left); Assert.Same(orbit[index], source.Right);
            Assert.True(action.TryReadComplete(derivations[index], out var result));
            if (index < orbit.Length - 1) Assert.Same(orbit[index + 1], result);
            var parts = Assert.IsType<Relation>(derivations[index].Right);
            Assert.True(action.Turn.TryReadComplete(Assert.IsType<Relation>(parts.Left), out var substituted));
            var reduction = Assert.IsType<Relation>(parts.Right);
            var operands = Assert.IsType<Relation>(Assert.IsType<Relation>(reduction.Left).Right);
            Assert.Same(substituted, operands.Right);
            Assert.True(action.Frame.TryReadResult(reduction, out var reduced, out _, out _)); Assert.Same(result, reduced);
        }
    }

    [Fact]
    public void InverseActionReturnsAllThreeDirectionsInBothOrders()
    {
        var action = new QualitativeSixfoldWords(new QualitativeMotionFrame(), new Placeholder()); Assert.Equal(6, E(action.InverseReads).Length);
        foreach (var check in E(action.InverseReads).Cast<Relation>())
        {
            var outward = Assert.IsType<Relation>(Assert.IsType<Relation>(check.Left).Right);
            var result = Assert.IsType<Relation>(check.Right); var inward = Assert.IsType<Relation>(result.Left);
            Assert.True(action.TryReadComplete(outward, out _)); Assert.True(action.TryReadComplete(inward, out _));
            AssertZero(action.Frame, Assert.IsType<Relation>(result.Right));
        }
        Assert.True(action.TryReadComplete(action.ApplyInverse(action.U), out var previous));
        AssertZero(action.Frame, action.Frame.Reduce(previous, Inverse(action.Frame, Assert.IsType<Relation>(E(action.Orbit)[5]))));
    }

    [Fact]
    public void OpenOrForeignParticipationCannotBecomeACompleteCompiledAction()
    {
        var frame = new QualitativeMotionFrame(); var action = new QualitativeSixfoldWords(frame, new Placeholder());
        var foreignReach = S(QualitativeMotionFrame.Move(frame.Right, new Placeholder()));
        var foreignFrame = S(QualitativeMotionFrame.Move(new QualitativeMotionFrame().Right, action.Reach));
        foreach (var word in new[] { foreignReach, foreignFrame, StructuralConstraint.Open(E(action.U)) })
        {
            var read = action.Apply(word); Assert.False(action.TryReadComplete(read, out _));
            var compiled = action.TurnWord(word, out var retained);
            Assert.True(StructuralConstraint.TryReadScope(compiled, out var entries, out var seam, out var tail)); Assert.False(seam.Equals(tail));
            Assert.Same(retained, Assert.Single(entries));
        }
        var missing = new QualitativePatternTransform(frame, frame, S(E(action.Turn.Rows).Skip(1).ToArray()));
        Assert.False(missing.TryReadComplete(missing.Apply(action.U), out _));
    }

    [Fact]
    public void BorrowedReductionCannotCertifyAnotherSubstitutionInput()
    {
        var action = new QualitativeSixfoldWords(new QualitativeMotionFrame(), new Placeholder());
        var first = action.Apply(action.U); var second = action.Apply(action.V);
        var a = Assert.IsType<Relation>(first.Right); var b = Assert.IsType<Relation>(second.Right);
        var forged = new Relation(first.Left, new Relation(a.Left, b.Right));
        Assert.False(action.TryReadComplete(forged, out _));
        var wrongInput = new Relation(new Relation(action.Turn.Surface, action.V), first.Right);
        Assert.False(action.TryReadComplete(wrongInput, out _));
    }

    [Fact]
    public void WrongSignInTheSecondGeneratorDoesNotCloseAfterSixApplications()
    {
        var frame = new QualitativeMotionFrame(); var reach = new Placeholder();
        Relation M(IElement direction) => QualitativeMotionFrame.Move(direction, reach);
        var u = M(frame.Right); var v = M(frame.Up); var nu = M(frame.Left); var nv = M(frame.Down);
        var wrong = new QualitativePatternTransform(frame, frame, S(
            new Relation(u, S(v)), new Relation(v, S(v, u)),
            new Relation(nu, S(nv)), new Relation(nv, S(nv, nu))));
        var seed = S(u); var current = seed;
        for (var turn = 0; turn < 6; turn++)
        {
            Assert.True(wrong.TryReadComplete(wrong.Apply(current), out var expanded));
            Assert.True(frame.TryReadResult(frame.Reduce(S(), expanded), out current, out _, out _));
        }
        Assert.True(frame.TryReadResult(frame.Reduce(current, S(nu)), out var residual, out _, out _)); Assert.NotEmpty(E(residual));
    }

    [Fact]
    public void CompiledWordsExecuteThroughTheExistingAcknowledgedDrawingSequence()
    {
        var frame = new QualitativeMotionFrame(); var action = new QualitativeSixfoldWords(frame, new Placeholder());
        var drawing = new QualitativeDrawingReceiver(frame, initialSite: frame.Origin);
        var words = E(action.Orbit).Cast<Relation>().ToArray();
        var sequence = new AcknowledgedPatternSequence("compiled formal orbit", drawing, S(words.Select(drawing.Draw).ToArray()));
        for (var pulse = 0; pulse < words.Length; pulse++)
        {
            var receipt = sequence.Pulse(); Assert.True(sequence.TryReadAccepted(receipt, out var effect, out _, out _, out _));
            Assert.True(drawing.TryReadAccepted(effect, out _, out _));
        }
        Assert.Single(E(sequence.CompletionEvents)); Assert.Equal(6, E(drawing.Strokes).Length); Assert.Empty(E(sequence.Unresolved));
        Assert.Empty(E(drawing.Position)); Assert.NotSame(frame.Origin, drawing.CursorSite);
        Assert.DoesNotContain(Traverse(new Relation(action.Surface, new Relation(sequence.Surface, sequence.Timeline))), element => element is Carrier or CarrierLocation);
    }

    private static Relation Inverse(QualitativeMotionFrame frame, Relation word) => S(E(word).Cast<Relation>().Reverse().Select(move =>
        (IElement)QualitativeMotionFrame.Move(E(frame.Opposites).Cast<Relation>().Single(row => ReferenceEquals(row.Left, move.Left)).Right, move.Right)).ToArray());
    private static void AssertZero(QualitativeMotionFrame frame, Relation read)
    { Assert.True(frame.TryReadResult(read, out var remaining, out _, out var pending)); Assert.Empty(E(remaining)); Assert.Empty(E(pending)); }
    private static IEnumerable<IElement> Traverse(IElement root)
    {
        var seen = new HashSet<IElement>(ReferenceEqualityComparer.Instance); var stack = new Stack<IElement>(); stack.Push(root);
        while (stack.TryPop(out var element))
        { if (!seen.Add(element)) continue; yield return element; if (element is Relation row) { stack.Push(row.Left); stack.Push(row.Right); } }
    }
    private static Relation S(params IElement[] values) => StructuralConstraint.Closed(values);
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
}
