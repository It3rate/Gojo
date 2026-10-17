using Gojo.Core;
using Gojo.Patterns;

namespace Gojo.Experiments.QualitativeExecution.Tests;

public sealed class QualitativeMotionFrameTests
{
    [Fact]
    public void FrameDeclaresOrderReversalCancellationAndExchangeWithoutMeasurements()
    {
        var f = new QualitativeMotionFrame();
        Assert.Same(f.Horizontal, f.Right); Assert.Same(f.Vertical, f.Up);
        Assert.Same(f.Right.Left, f.Left.Right); Assert.Same(f.Right.Right, f.Left.Left);
        Assert.Same(f.Up.Left, f.Down.Right); Assert.Same(f.Up.Right, f.Down.Left);
        Assert.NotSame(f.Horizontal.Left, f.Vertical.Left);
        Assert.Equal(4, E(f.Opposites).Length); Assert.Equal(4, E(f.CancellationLaws).Length);
        foreach (var declaration in E(f.CancellationLaws).Cast<Relation>())
        {
            var variables = Assert.IsType<Relation>(declaration.Left);
            var reach = Assert.Single(E(variables));
            var equation = Assert.IsType<Relation>(declaration.Right);
            var pair = E(Assert.IsType<Relation>(equation.Left)).Cast<Relation>().ToArray();
            Assert.Equal(2, pair.Length);
            Assert.Same(reach, pair[0].Right); Assert.Same(reach, pair[1].Right);
            Assert.Empty(E(Assert.IsType<Relation>(equation.Right)));
        }
        var exchangeEquation = Assert.IsType<Relation>(f.Exchange.Right);
        var original = E(Assert.IsType<Relation>(exchangeEquation.Left));
        var exchanged = E(Assert.IsType<Relation>(exchangeEquation.Right));
        Assert.Equal(original.Reverse(), exchanged);
        Assert.True(Contains(f.Surface, f.Origin)); Assert.True(Contains(f.Surface, f.Exchange));
        AssertNoNumbers(f.Surface);
    }

    [Theory]
    [InlineData(true, true)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(false, false)]
    public void DeclaredOppositesWithOneSharedExtentCancelInEitherTraversal(bool horizontal, bool forward)
    {
        var f = new QualitativeMotionFrame();
        var positive = horizontal ? f.Right : f.Up;
        var negative = horizontal ? f.Left : f.Down;
        var reach = new Placeholder();
        var first = QualitativeMotionFrame.Move(forward ? positive : negative, reach);
        var second = QualitativeMotionFrame.Move(forward ? negative : positive, reach);
        var before = S(first); var incoming = S(second);
        var witness = f.Reduce(before, incoming);
        var result = Read(f, witness);

        Assert.Empty(E(result.Residual)); Assert.Empty(E(result.Unresolved));
        var cancellation = Assert.IsType<Relation>(Assert.Single(E(result.Cancellations)));
        Assert.Same(first, cancellation.Left); Assert.Same(second, cancellation.Right);
        var header = Assert.IsType<Relation>(witness.Left);
        Assert.Same(f.Surface, header.Left);
        var history = Assert.IsType<Relation>(header.Right);
        Assert.Same(before, history.Left); Assert.Same(incoming, history.Right);
        AssertClosed(result.Residual);
        AssertNoNumbers(witness);
    }

    [Fact]
    public void DistinctUnmeasuredExtentsLeaveCompleteTermsAndAnUnresolvedNetComparison()
    {
        var f = new QualitativeMotionFrame();
        var first = QualitativeMotionFrame.Move(f.Right, new Placeholder());
        var second = QualitativeMotionFrame.Move(f.Left, new Placeholder());
        var result = Read(f, f.Reduce(S(first), S(second)));

        Assert.Equal(new IElement[] { first, second }, E(result.Residual));
        AssertClosed(result.Residual);
        Assert.Empty(E(result.Cancellations));
        var comparison = Assert.IsType<Relation>(Assert.Single(E(result.Unresolved)));
        Assert.Same(first, comparison.Left); Assert.Same(second, comparison.Right);
    }

    [Fact]
    public void EqualLookingExtentStructureDoesNotEstablishSharedExtentIdentity()
    {
        var f = new QualitativeMotionFrame();
        var a = new Placeholder(); var b = new Placeholder();
        var reachA = new Relation(a, b); var reachB = new Relation(a, b);
        var right = QualitativeMotionFrame.Move(f.Right, reachA);
        var left = QualitativeMotionFrame.Move(f.Left, reachB);
        var result = Read(f, f.Reduce(S(right), S(left)));

        Assert.Equal(new IElement[] { right, left }, E(result.Residual));
        Assert.Empty(E(result.Cancellations)); Assert.Single(E(result.Unresolved));
        AssertNoNumbers(result.Residual);
    }

    [Fact]
    public void SharedExtentDoesNotCancelIndependentAxes()
    {
        var f = new QualitativeMotionFrame(); var reach = new Placeholder();
        var right = QualitativeMotionFrame.Move(f.Right, reach);
        var down = QualitativeMotionFrame.Move(f.Down, reach);
        var result = Read(f, f.Reduce(S(right), S(down)));

        Assert.Equal(new IElement[] { right, down }, E(result.Residual));
        Assert.Empty(E(result.Cancellations)); Assert.Empty(E(result.Unresolved));
        Assert.True(f.Direction(right, out var x, out var forwardX, out var readReach));
        Assert.Same(f.Horizontal, x); Assert.True(forwardX); Assert.Same(reach, readReach);
        Assert.True(f.Direction(down, out var y, out var forwardY, out _));
        Assert.Same(f.Vertical, y); Assert.False(forwardY);
    }

    [Fact]
    public void RepeatedParticipationAccumulatesAndOneInverseConsumesOnlyOneOccurrence()
    {
        var f = new QualitativeMotionFrame(); var reach = new Placeholder();
        var right = QualitativeMotionFrame.Move(f.Right, reach);
        var left = QualitativeMotionFrame.Move(f.Left, reach);
        var before = S(right, right, right);
        var result = Read(f, f.Reduce(before, S(left)));

        Assert.Equal(new IElement[] { right, right }, E(result.Residual));
        Assert.Single(E(result.Cancellations)); Assert.Empty(E(result.Unresolved));
        Assert.Equal(new IElement[] { right, right, right }, E(before));
    }

    [Fact]
    public void CancellationCanCrossInterveningMovesUnderTheDeclaredAdditiveRead()
    {
        var f = new QualitativeMotionFrame(); var a = new Placeholder(); var b = new Placeholder();
        var right = QualitativeMotionFrame.Move(f.Right, a);
        var up = QualitativeMotionFrame.Move(f.Up, b);
        var left = QualitativeMotionFrame.Move(f.Left, a);
        var firstHistory = S(right, up, left);
        var secondHistory = S(left, right, up);
        var first = f.Reduce(S(), firstHistory); var second = f.Reduce(S(), secondHistory);

        Assert.Same(up, Assert.Single(E(Read(f, first).Residual)));
        Assert.Same(up, Assert.Single(E(Read(f, second).Residual)));
        Assert.Equal(new IElement[] { right, up, left }, E(firstHistory));
        Assert.Equal(new IElement[] { left, right, up }, E(secondHistory));
        Assert.True(Contains(first, firstHistory)); Assert.True(Contains(second, secondHistory));
        var firstPair = Assert.IsType<Relation>(Assert.Single(E(Read(f, first).Cancellations)));
        var secondPair = Assert.IsType<Relation>(Assert.Single(E(Read(f, second).Cancellations)));
        Assert.Same(right, firstPair.Left); Assert.Same(left, secondPair.Left);
        Assert.NotSame(first, second);
    }

    [Fact]
    public void LaterSharedExtentEvidenceCanResolveAnEarlierUnknownNetWithoutErasingEitherRead()
    {
        var f = new QualitativeMotionFrame(); var a = new Placeholder(); var b = new Placeholder();
        var rightA = QualitativeMotionFrame.Move(f.Right, a);
        var leftB = QualitativeMotionFrame.Move(f.Left, b);
        var rightB = QualitativeMotionFrame.Move(f.Right, b);
        var earlier = f.Reduce(S(), S(rightA, leftB));
        var earlierResult = Read(f, earlier);
        Assert.Single(E(earlierResult.Unresolved));
        var later = f.Reduce(earlierResult.Residual, S(rightB));
        var laterResult = Read(f, later);

        Assert.Same(rightA, Assert.Single(E(laterResult.Residual)));
        Assert.Empty(E(laterResult.Unresolved)); Assert.Single(E(laterResult.Cancellations));
        Assert.Single(E(Read(f, earlier).Unresolved));
        // History is an explicit connection of the reads; the displacement
        // itself does not pretend to remember how every term was acquired.
        var timeline = new Relation(earlier, later);
        Assert.True(Contains(timeline, leftB)); Assert.True(Contains(timeline, rightB));
    }

    [Fact]
    public void ForeignDirectionAndUnknownParticipationStayPresentAndOpen()
    {
        var f = new QualitativeMotionFrame(); var other = new QualitativeMotionFrame();
        var known = QualitativeMotionFrame.Move(f.Right, new Placeholder());
        var foreign = QualitativeMotionFrame.Move(other.Left, new Placeholder());
        var unknown = new Placeholder();
        var result = Read(f, f.Reduce(S(known), S(foreign, unknown)));

        Assert.Equal(new IElement[] { known, foreign, unknown }, E(result.Residual));
        Assert.Equal(2, E(result.Unresolved).Length); Assert.Empty(E(result.Cancellations));
        AssertOpen(result.Residual);
        Assert.False(f.Direction(foreign, out _, out _, out _));
        var lookalike = QualitativeMotionFrame.Move(new Relation(f.Right.Left, f.Right.Right), new Placeholder());
        Assert.False(f.Direction(lookalike, out _, out _, out _));
    }

    [Fact]
    public void OpenTailSurvivesEvenWhenAllKnownMovesCancel()
    {
        var f = new QualitativeMotionFrame(); var reach = new Placeholder();
        var right = QualitativeMotionFrame.Move(f.Right, reach);
        var left = QualitativeMotionFrame.Move(f.Left, reach);
        var incoming = StructuralConstraint.Open(left);
        var result = Read(f, f.Reduce(S(right), incoming));

        Assert.Empty(E(result.Residual)); AssertOpen(result.Residual);
        Assert.Single(E(result.Cancellations)); Assert.Single(E(result.Unresolved));
        Assert.True(Contains(result.Unresolved, incoming));
        var after = Read(f, f.Reduce(result.Residual, S()));
        Assert.Empty(E(after.Residual)); AssertOpen(after.Residual);
        Assert.NotEmpty(E(after.Unresolved));
    }

    [Fact]
    public void MalformedInputPreservesItsCauseAndDoesNotManufactureAClosedOrigin()
    {
        var f = new QualitativeMotionFrame();
        var malformed = new Relation(new Placeholder(), new Placeholder());
        var result = Read(f, f.Reduce(malformed, S()));

        Assert.Empty(E(result.Residual)); AssertOpen(result.Residual);
        Assert.Single(E(result.Unresolved)); Assert.True(Contains(result.Unresolved, malformed));
    }

    [Fact]
    public void ReaderRejectsOtherFramesMissingParticipationAndUnearnedCancellation()
    {
        var f = new QualitativeMotionFrame(); var reach = new Placeholder();
        var right = QualitativeMotionFrame.Move(f.Right, reach);
        var left = QualitativeMotionFrame.Move(f.Left, new Placeholder());
        var witness = f.Reduce(S(right), S(left));
        Assert.False(new QualitativeMotionFrame().TryReadResult(witness, out _, out _, out _));
        Assert.False(f.TryReadResult(new Relation(new Placeholder(), new Placeholder()), out _, out _, out _));
        var lost = new Relation(witness.Left, new Relation(S(), new Relation(S(), S())));
        Assert.False(f.TryReadResult(lost, out _, out _, out _));
        var unequalCancellation = new Relation(witness.Left, new Relation(S(), new Relation(S(new Relation(right, left)), S())));
        Assert.False(f.TryReadResult(unequalCancellation, out _, out _, out _));
        var hiddenComparison = new Relation(witness.Left, new Relation(S(right, left), new Relation(S(), S())));
        Assert.False(f.TryReadResult(hiddenComparison, out _, out _, out _));
    }

    [Fact]
    public void ReaderRejectsDoubleConsumptionOfOneMoveOccurrence()
    {
        var f = new QualitativeMotionFrame(); var reach = new Placeholder();
        var right = QualitativeMotionFrame.Move(f.Right, reach);
        var left = QualitativeMotionFrame.Move(f.Left, reach);
        var genuine = f.Reduce(S(right), S(left));
        var pair = new Relation(right, left);
        var forged = new Relation(genuine.Left, new Relation(S(), new Relation(S(pair, pair), S())));
        Assert.False(f.TryReadResult(forged, out _, out _, out _));
    }

    private static (Relation Residual, Relation Cancellations, Relation Unresolved) Read(QualitativeMotionFrame frame, Relation witness)
    {
        Assert.True(frame.TryReadResult(witness, out var residual, out var cancellations, out var unresolved));
        return (residual, cancellations, unresolved);
    }

    private static Relation S(params IElement[] moves) => StructuralConstraint.Closed(moves);
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
    private static void AssertClosed(Relation scope)
    {
        Assert.True(StructuralConstraint.TryReadScope(scope, out _, out var seam, out var tail));
        Assert.Equal(seam, tail);
    }
    private static void AssertOpen(Relation scope)
    {
        Assert.True(StructuralConstraint.TryReadScope(scope, out _, out var seam, out var tail));
        Assert.NotEqual(seam, tail);
    }
    private static bool Contains(IElement root, IElement target)
    {
        var seen = new HashSet<IElement>(ReferenceEqualityComparer.Instance);
        var pending = new Stack<IElement>(); pending.Push(root);
        while (pending.TryPop(out var node))
        {
            if (ReferenceEquals(node, target)) return true;
            if (!seen.Add(node) || node is not Relation relation) continue;
            pending.Push(relation.Left); pending.Push(relation.Right);
        }
        return false;
    }
    private static void AssertNoNumbers(IElement root)
    {
        var seen = new HashSet<IElement>(ReferenceEqualityComparer.Instance);
        var pending = new Stack<IElement>(); pending.Push(root);
        while (pending.TryPop(out var node))
        {
            if (!seen.Add(node)) continue;
            Assert.IsNotType<Carrier>(node); Assert.IsNotType<CarrierLocation>(node); Assert.IsNotType<TickRef>(node);
            if (node is Relation relation) { pending.Push(relation.Left); pending.Push(relation.Right); }
        }
    }
}
