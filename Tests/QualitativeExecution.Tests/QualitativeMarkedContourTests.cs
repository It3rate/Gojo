using Gojo.Core;
using Gojo.Patterns;
using Xunit;

namespace Gojo.Experiments.QualitativeExecution.Tests;

public sealed class QualitativeMarkedContourTests
{
    [Fact]
    public void SharedRadiusAndClosedWalkLeaveEqualTurnsUnresolved()
    {
        var contour = Create(4); var read = contour.Evaluate();
        Assert.True(contour.TryReadClosedContour(read, out var whole));
        Assert.Same(contour.WholeTraversal, whole);
        Assert.Same(contour.Reference, whole.Left);
        Assert.Same(contour.Traversal, whole.Right);
        Assert.True(contour.TryReadResult(read, out var turns, out var violations, out var pending));
        Assert.Equal(4, E(turns).Length); Assert.Empty(E(violations));
        Assert.Contains(contour.EqualTurnPromise, E(pending));
        Assert.False(contour.TryReadEqualTurns(read, out _));
        foreach (var radius in E(contour.RadialAttachments).Cast<Relation>())
        {
            Assert.Same(contour.Center, ((Relation)radius.Left).Left);
            Assert.Same(contour.ReachSupport, radius.Right);
        }
        Assert.DoesNotContain(Traverse(contour.Surface), item => item is Carrier or CarrierLocation);
    }

    [Fact]
    public void EqualTurnPromiseRetainsActualRepeatedParticipation()
    {
        var turn = new Placeholder(); var contour = Create(4).WithEqualTurns(turn);
        var read = contour.Evaluate();
        Assert.True(contour.TryReadEqualTurns(read, out var actual)); Assert.Same(turn, actual);
        Assert.True(contour.TryReadResult(read, out _, out var failed, out var pending));
        Assert.Empty(E(failed)); Assert.Empty(E(pending));
        var promise = (Relation)contour.EqualTurnPromise.Right;
        Assert.Same(contour.Traversal, promise.Left);
        Assert.Equal(E(contour.Traversal), E((Relation)promise.Right).Cast<Relation>().Select(row => row.Left));
        Assert.All(E((Relation)promise.Right).Cast<Relation>(), row => Assert.Same(turn, row.Right));
    }

    [Fact]
    public void FourSuccessorsReturnToReferenceWithoutErasingTheWholeWalk()
    {
        var contour = Create(4); var recurrence = new PhaseRecurrence(contour.Markers, contour.Successors);
        var owner = new Placeholder(); var state = recurrence.Initial(owner, new Placeholder(), S(contour.Reference));
        for (var i = 0; i < 4; i++)
        {
            Assert.True(recurrence.TryReadState(state, out _, out var previous, out _));
            var step = recurrence.Step(state, recurrence.Drive(owner, new Placeholder(), previous, contour.Surface));
            Assert.True(recurrence.TryReadAccepted(step, out _, out _, out _));
            Assert.True(recurrence.TryReadStep(step, out _, out _, out state, out _, out _, out _));
        }
        Assert.True(recurrence.TryReadState(state, out _, out _, out var final));
        Assert.Same(contour.Reference, Assert.Single(E(final)));
        Assert.Equal(4, E((Relation)contour.WholeTraversal.Right).Length);
        Assert.False(contour.TryReadEqualTurns(contour.Evaluate(), out _));
    }

    [Fact]
    public void AnyActualRimMarkerCanBeTheChosenReference()
    {
        var original = Create(4); var edges = E(original.Traversal);
        var rotated = S([.. edges.Skip(2), .. edges.Take(2)]);
        var reference = ((Relation)edges[2]).Left;
        var contour = new QualitativeMarkedContour(original.Center, original.ReachSupport, original.Markers, rotated, reference);
        Assert.True(contour.TryReadClosedContour(contour.Evaluate(), out var whole));
        Assert.Same(reference, whole.Left); Assert.NotSame(original.Reference, reference);
    }

    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(6)]
    public void OtherCycleSizesUseTheSameDeclaredSubdivisionMechanism(int count)
    {
        var turn = new Placeholder(); var contour = Create(count).WithEqualTurns(turn);
        Assert.True(contour.TryReadEqualTurns(contour.Evaluate(), out var actual));
        Assert.Same(turn, actual); Assert.Equal(count, E(contour.Traversal).Length);
    }

    [Fact]
    public void UnknownRadiusDoesNotBecomeAConcreteZeroOrConstantRadiusCertificate()
    {
        var basis = Create(4);
        var contour = new QualitativeMarkedContour(basis.Center, StructuralConstraint.Open(), basis.Markers, basis.Traversal, basis.Reference)
            .WithEqualTurns(new Placeholder());
        var read = contour.Evaluate();
        Assert.False(contour.TryReadClosedContour(read, out _)); Assert.False(contour.TryReadEqualTurns(read, out _));
        Assert.True(contour.TryReadResult(read, out _, out _, out var pending)); Assert.Contains(contour.ReachSupport, E(pending));
    }

    [Fact]
    public void AnOpenWalkCannotCloseJustBecauseItsKnownPrefixReturns()
    {
        var basis = Create(4);
        var contour = new QualitativeMarkedContour(basis.Center, basis.ReachSupport, basis.Markers,
            StructuralConstraint.Open(E(basis.Traversal)), basis.Reference).WithEqualTurns(new Placeholder());
        Assert.False(contour.TryReadClosedContour(contour.Evaluate(), out _));
        Assert.False(contour.TryReadEqualTurns(contour.Evaluate(), out _));
    }

    [Fact]
    public void AliasedMarkersAndWrongClosingSitesAreNotValidWholeTraversals()
    {
        var basis = Create(4); var markers = E(basis.Markers); var edges = E(basis.Traversal);
        var alias = new QualitativeMarkedContour(basis.Center, basis.ReachSupport, S(markers[0], markers[0], markers[2], markers[3]), basis.Traversal, basis.Reference);
        Assert.False(alias.TryReadClosedContour(alias.Evaluate(), out _));
        edges[^1] = new Relation(((Relation)edges[^1]).Left, new Placeholder());
        var foreign = new QualitativeMarkedContour(basis.Center, basis.ReachSupport, basis.Markers, S(edges), basis.Reference);
        Assert.False(foreign.TryReadClosedContour(foreign.Evaluate(), out _));
    }

    [Fact]
    public void FourDifferentTurnParticipantsDoNotEstablishEqualSubdivision()
    {
        var basis = Create(4);
        var promise = new Relation(basis.Traversal, S(E(basis.Traversal).Select(edge => (IElement)new Relation(edge, new Placeholder())).ToArray()));
        var contour = new QualitativeMarkedContour(basis.Center, basis.ReachSupport, basis.Markers, basis.Traversal, basis.Reference, promise);
        var read = contour.Evaluate();
        Assert.True(contour.TryReadClosedContour(read, out _));
        Assert.False(contour.TryReadEqualTurns(read, out _));
        Assert.True(contour.TryReadResult(read, out _, out var failed, out _)); Assert.NotEmpty(E(failed));
    }

    [Fact]
    public void CopiedEdgesCannotImpersonateTheActualSubdivisionParticipants()
    {
        var basis = Create(4); var turn = new Placeholder();
        var copies = E(basis.Traversal).Cast<Relation>().Select(edge => (IElement)new Relation(new Relation(edge.Left, edge.Right), turn)).ToArray();
        var contour = new QualitativeMarkedContour(basis.Center, basis.ReachSupport, basis.Markers, basis.Traversal, basis.Reference,
            new Relation(basis.Traversal, S(copies)));
        Assert.True(contour.TryReadClosedContour(contour.Evaluate(), out _));
        Assert.False(contour.TryReadEqualTurns(contour.Evaluate(), out _));
    }

    [Fact]
    public void AnOpenEqualTurnPromiseDoesNotInvalidateTheKnownContourOrInventEqualAngles()
    {
        var basis = Create(4); var turn = new Placeholder();
        var promise = new Relation(basis.Traversal, StructuralConstraint.Open(E(basis.Traversal)
            .Select(edge => (IElement)new Relation(edge, turn)).ToArray()));
        var contour = new QualitativeMarkedContour(basis.Center, basis.ReachSupport, basis.Markers, basis.Traversal, basis.Reference, promise);
        var read = contour.Evaluate();
        Assert.True(contour.TryReadClosedContour(read, out _));
        Assert.False(contour.TryReadEqualTurns(read, out _));
        Assert.True(contour.TryReadResult(read, out _, out _, out var pending)); Assert.Contains(contour.EqualTurnPromise, E(pending));
    }

    [Fact]
    public void DirectionCorrespondenceChecksEveryActualSuccessorSquare()
    {
        var contour = Create(4); var frame = new QualitativeMotionFrame();
        IElement[] directions = [frame.Right, frame.Up, frame.Left, frame.Down];
        var action = Cycle(directions); var map = S(E(contour.Markers).Zip(directions).Select(pair => (IElement)new Relation(pair.First, pair.Second)).ToArray());
        var read = contour.CompareAction(S(directions), action, map);
        Assert.True(contour.TryReadActionResult(read, out var matched, out var failed, out var pending));
        Assert.Equal(4, E(matched).Length); Assert.Empty(E(failed)); Assert.Empty(E(pending));
        Assert.False(contour.TryReadEqualTurns(contour.Evaluate(), out _));
        var reversed = S(E(action).Cast<Relation>().Select(edge => (IElement)new Relation(edge.Right, edge.Left)).ToArray());
        var wrong = contour.CompareAction(S(directions), reversed, map);
        Assert.True(contour.TryReadActionResult(wrong, out _, out var mismatched, out _)); Assert.NotEmpty(E(mismatched));
    }

    [Fact]
    public void IncompleteOrForeignCorrespondencesCannotPass()
    {
        var contour = Create(4); var directions = Enumerable.Range(0, 4).Select(_ => (IElement)new Placeholder()).ToArray();
        var maps = E(contour.Markers).Zip(directions).Select(pair => (IElement)new Relation(pair.First, pair.Second)).ToArray();
        var open = contour.CompareAction(S(directions), Cycle(directions), StructuralConstraint.Open(maps));
        Assert.True(contour.TryReadActionResult(open, out _, out _, out var pending)); Assert.NotEmpty(E(pending));
        maps[0] = new Relation(new Placeholder(), directions[0]);
        var foreign = contour.CompareAction(S(directions), Cycle(directions), S(maps));
        Assert.True(contour.TryReadActionResult(foreign, out _, out var failed, out _)); Assert.NotEmpty(E(failed));
    }

    [Fact]
    public void ReadersRejectRemovedObligationsAndRemovedCorrespondenceEvidence()
    {
        var contour = Create(4); var actual = contour.Evaluate();
        Assert.True(contour.TryReadResult(actual, out var turns, out var failed, out _));
        var forged = new Relation(contour.Surface, new Relation(turns, new Relation(failed, S())));
        Assert.False(contour.TryReadResult(forged, out _, out _, out _));
        var directions = Enumerable.Range(0, 4).Select(_ => (IElement)new Placeholder()).ToArray();
        var maps = S(E(contour.Markers).Zip(directions).Select(pair => (IElement)new Relation(pair.First, pair.Second)).ToArray());
        var actionRead = contour.CompareAction(S(directions), Cycle(directions), maps);
        Assert.False(contour.TryReadActionResult(new Relation(actionRead.Left, new Relation(S(), new Relation(S(), S()))), out _, out _, out _));
    }

    [Fact]
    public void CopiedMapRowsCannotImpersonateActualActionCorrespondenceEvidence()
    {
        var contour = Create(4); var directions = Enumerable.Range(0, 4).Select(_ => (IElement)new Placeholder()).ToArray();
        var maps = S(E(contour.Markers).Zip(directions).Select(pair => (IElement)new Relation(pair.First, pair.Second)).ToArray());
        var read = contour.CompareAction(S(directions), Cycle(directions), maps);
        Assert.True(contour.TryReadActionResult(read, out var matched, out var failed, out var pending));
        var proofs = E(matched); var first = (Relation)proofs[0]; var body = (Relation)first.Right;
        var mapRow = (Relation)body.Left;
        proofs[0] = new Relation(first.Left, new Relation(new Relation(mapRow.Left, mapRow.Right), body.Right));
        var copied = new Relation(read.Left, new Relation(S(proofs), new Relation(failed, pending)));
        Assert.False(contour.TryReadActionResult(copied, out _, out _, out _));
    }

    private static QualitativeMarkedContour Create(int count)
    {
        var markers = Enumerable.Range(0, count).Select(_ => (IElement)new Placeholder()).ToArray();
        return new(new Placeholder(), S(new Placeholder()), S(markers), Cycle(markers), markers[0]);
    }
    private static Relation Cycle(IElement[] markers) => S(markers.Select((marker, i) => (IElement)new Relation(marker, markers[(i + 1) % markers.Length])).ToArray());
    private static Relation S(params IElement[] items) => StructuralConstraint.Closed(items);
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
    private static IEnumerable<IElement> Traverse(IElement root)
    {
        var seen = new HashSet<IElement>(ReferenceEqualityComparer.Instance); var stack = new Stack<IElement>(); stack.Push(root);
        while (stack.TryPop(out var item))
        {
            if (!seen.Add(item)) continue;
            yield return item;
            if (item is Relation pair) { stack.Push(pair.Left); stack.Push(pair.Right); }
        }
    }
}
