using Gojo.Core;
using Gojo.Patterns;

namespace Gojo.Experiments.QualitativeExecution.Tests;

public sealed class QualitativeDrawingReceiverTests
{
    [Fact]
    public void DefiniteChordCreatesOneFreshSiteAndStrokeWithItsActualCause()
    {
        var frame = new QualitativeMotionFrame(); var receiver = new QualitativeDrawingReceiver(frame);
        var r = QualitativeMotionFrame.Move(frame.Right, new Placeholder()); var u = QualitativeMotionFrame.Move(frame.Up, new Placeholder());
        var start = receiver.CursorSite; var before = receiver.State; var cause = new Placeholder(); var occurrence = new Placeholder();
        var action = receiver.Draw(S(r, u)); Assert.True(receiver.TryReadDraw(action, out var chord));
        var receipt = receiver.Apply(occurrence, S(action), cause);
        Assert.True(receiver.TryReadAccepted(receipt, out var after, out var added)); Assert.Same(after, receiver.State);
        Assert.True(receiver.TryReadStep(receipt, out var actualBefore, out var offer, out _, out _, out _, out var pending));
        Assert.Same(before, actualBefore); Assert.Empty(E(pending)); Assert.True(Contains(offer, cause));
        Assert.NotSame(start, receiver.CursorSite); Assert.Equal(2, E(receiver.Sites).Length); Assert.Same(receiver.Sites, receiver.SiteBindings);
        Assert.Equal(new IElement[] { r, u }, E(receiver.Position)); Assert.Equal(2, E(receiver.History).Length);
        var stroke = Assert.IsType<Relation>(Assert.Single(E(added))); Assert.Same(stroke, Assert.Single(E(receiver.Strokes)));
        Assert.True(QualitativeDrawingReceiver.TryReadStroke(stroke, out var owner, out var from, out var to, out var word));
        Assert.Same(offer, owner); Assert.Same(start, from); Assert.Same(receiver.CursorSite, to);
        Assert.All(E(word).Zip(E(chord)), pair => Assert.Same(pair.Second, pair.First));
        Assert.All(E(receiver.Sites).Cast<Relation>(), binding => Assert.True(receiver.TryReadBinding(binding, out _, out _)));
        Assert.DoesNotContain(Traverse(receiver.State), element => element is Carrier or CarrierLocation);
    }

    [Fact]
    public void CoordinateReturnDoesNotFuseIdentityButExplicitResetReusesAnchorSite()
    {
        var frame = new QualitativeMotionFrame(); var receiver = new QualitativeDrawingReceiver(frame); var reach = new Placeholder();
        var start = receiver.CursorSite;
        Accept(receiver, receiver.Draw(S(QualitativeMotionFrame.Move(frame.Right, reach))));
        Accept(receiver, receiver.Draw(S(QualitativeMotionFrame.Move(frame.Left, reach))));
        Assert.Empty(E(receiver.Position)); Assert.NotSame(start, receiver.CursorSite); Assert.Equal(3, E(receiver.Sites).Length);
        Assert.Equal(2, E(receiver.Strokes).Length); var before = receiver.State;
        var receipt = Accept(receiver, receiver.Reset()); Assert.Same(start, receiver.CursorSite); Assert.Same(start, receiver.AnchorSite);
        Assert.True(receiver.TryReadAccepted(receipt, out _, out var newStrokes)); Assert.Empty(E(newStrokes));
        Assert.Equal(3, E(receiver.Sites).Length); Assert.Equal(2, E(receiver.Strokes).Length); Assert.NotSame(before, receiver.State);
    }

    [Fact]
    public void JumpAnchorAndResetKeepNavigationSeparateFromDrawing()
    {
        var frame = new QualitativeMotionFrame(); var receiver = new QualitativeDrawingReceiver(frame);
        var r = QualitativeMotionFrame.Move(frame.Right, new Placeholder()); var u = QualitativeMotionFrame.Move(frame.Up, new Placeholder());
        var jump = receiver.Jump(S(r)); Assert.True(receiver.TryReadJump(jump, out _)); Assert.False(receiver.TryReadDraw(jump, out _));
        Accept(receiver, jump); var nextAnchor = receiver.CursorSite; Assert.Empty(E(receiver.Strokes));
        Assert.Same(receiver.Anchor(), receiver.Reanchor()); Accept(receiver, receiver.Anchor()); Assert.Same(nextAnchor, receiver.AnchorSite);
        Accept(receiver, receiver.Draw(S(u))); Assert.NotSame(nextAnchor, receiver.CursorSite); Assert.Single(E(receiver.Strokes));
        Accept(receiver, receiver.Reset()); Assert.Same(nextAnchor, receiver.CursorSite); Assert.Same(r, Assert.Single(E(receiver.Position)));
        Assert.Equal(3, E(receiver.Sites).Length); Assert.Single(E(receiver.Strokes));
    }

    [Fact]
    public void VerticalMirrorComposesOnFutureOutputAndDoesNotRewriteHistoryOrDrift()
    {
        var frame = new QualitativeMotionFrame(); var receiver = new QualitativeDrawingReceiver(frame);
        var h = new Placeholder(); var v = new Placeholder();
        var r = QualitativeMotionFrame.Move(frame.Right, h); var u = QualitativeMotionFrame.Move(frame.Up, v);
        Accept(receiver, receiver.Draw(S(r, u))); var oldStroke = Assert.Single(E(receiver.Strokes)); var site = receiver.CursorSite; var position = receiver.Position;
        var mirror = receiver.Mirror(receiver.VerticalMirror); Assert.True(receiver.TryReadMirror(mirror, out var mapping)); Assert.Same(receiver.VerticalMirror, mapping);
        Accept(receiver, mirror); Assert.Same(site, receiver.CursorSite); Assert.Same(position, receiver.Position); Assert.Same(oldStroke, Assert.Single(E(receiver.Strokes)));
        var receipt = Accept(receiver, receiver.Draw(S(r, u)));
        Assert.True(receiver.TryReadAccepted(receipt, out _, out var added));
        Assert.True(QualitativeDrawingReceiver.TryReadStroke(Assert.IsType<Relation>(Assert.Single(E(added))), out _, out _, out _, out var word));
        var transformed = E(word).Cast<Relation>().ToArray(); Assert.Same(r, transformed[0]); Assert.Same(frame.Down, transformed[1].Left); Assert.Same(v, transformed[1].Right);
        Assert.Equal(2, E(receiver.Position).Length); Assert.All(E(receiver.Position), move => Assert.Same(r, move));
        Accept(receiver, receiver.Mirror(receiver.VerticalMirror));
        Assert.All(E(receiver.Orientation).Cast<Relation>(), row => Assert.Same(row.Left, row.Right));
        Assert.Same(oldStroke, E(receiver.Strokes)[0]); Assert.Equal(2, E(receiver.Strokes).Length);
    }

    [Fact]
    public void SimultaneousContributionsNeedOneExplicitChordRatherThanAUnionOfActions()
    {
        var frame = new QualitativeMotionFrame(); var receiver = new QualitativeDrawingReceiver(frame);
        var r = QualitativeMotionFrame.Move(frame.Right, new Placeholder()); var u = QualitativeMotionFrame.Move(frame.Up, new Placeholder());
        var before = receiver.State;
        var union = receiver.Apply(new Placeholder(), S(receiver.Draw(S(r)), receiver.Draw(S(u))), S());
        Pending(receiver, union, before); Assert.Empty(E(receiver.Strokes));
        var causes = S(new Relation(new Placeholder(), r), new Relation(new Placeholder(), u));
        var receipt = receiver.Apply(new Placeholder(), S(receiver.Draw(S(r, u))), causes);
        Assert.True(receiver.TryReadAccepted(receipt, out _, out var added)); Assert.Single(E(added)); Assert.Equal(2, E(receiver.Sites).Length);
        Assert.True(Contains(receipt, causes));
    }

    [Fact]
    public void UnknownOrForeignChordsAndIncompleteOrientationRemainPending()
    {
        var frame = new QualitativeMotionFrame(); var receiver = new QualitativeDrawingReceiver(frame); var before = receiver.State;
        var r = QualitativeMotionFrame.Move(frame.Right, new Placeholder());
        foreach (var support in new[] { StructuralConstraint.Open(), StructuralConstraint.Open(receiver.Draw(S(r))), S(), S(new Placeholder()) })
            Pending(receiver, receiver.Apply(new Placeholder(), support, S()), before);
        Pending(receiver, receiver.Apply(new Placeholder(), S(receiver.Draw(StructuralConstraint.Open(r))), S()), before);
        var otherFrame = new QualitativeMotionFrame();
        Pending(receiver, receiver.Apply(new Placeholder(), S(receiver.Draw(S(QualitativeMotionFrame.Move(otherFrame.Right, new Placeholder())))), S()), before);
        Pending(receiver, receiver.Apply(new Placeholder(), S(receiver.Mirror(StructuralConstraint.Open(E(receiver.VerticalMirror)))), S()), before);
        Pending(receiver, receiver.Apply(new Placeholder(), S(receiver.Mirror(S(E(receiver.VerticalMirror).Take(3).ToArray()))), S()), before);
        var crossAxes = S(new Relation(frame.Right, frame.Up), new Relation(frame.Left, frame.Down), new Relation(frame.Up, frame.Right), new Relation(frame.Down, frame.Left));
        Pending(receiver, receiver.Apply(new Placeholder(), S(receiver.Mirror(crossAxes)), S()), before);
        Assert.Empty(E(receiver.Strokes)); Assert.Single(E(receiver.Sites));
    }

    [Fact]
    public void StaleReplayWrongOwnerAndForeignActionsCannotAdvanceTheLedger()
    {
        var frame = new QualitativeMotionFrame(); var receiver = new QualitativeDrawingReceiver(frame); var before = receiver.State;
        Assert.True(receiver.TryReadState(before, out var origin, out _, out _, out _, out _, out _, out _));
        var occurrence = new Placeholder(); var action = receiver.Draw(S(QualitativeMotionFrame.Move(frame.Right, new Placeholder())));
        var accepted = receiver.Apply(occurrence, S(action), S()); Assert.True(receiver.TryReadAccepted(accepted, out var after, out _));
        Assert.True(receiver.TryReadStep(accepted, out _, out var offer, out _, out _, out _, out _));
        Blocked(receiver, receiver.Step(after, offer), after);
        Blocked(receiver, receiver.Apply(new Placeholder(), S(action), S(), origin), after);
        var wrong = receiver.Offer(new Placeholder(), new Placeholder(), occurrence, S(action));
        Blocked(receiver, receiver.Step(after, wrong), after);
        var foreign = new QualitativeDrawingReceiver(frame);
        Blocked(receiver, receiver.Apply(new Placeholder(), S(foreign.Draw(S())) , S()), after);
        Assert.Same(after, receiver.State); Assert.Single(E(receiver.Strokes));
    }

    [Fact]
    public void FailedUnknownOfferCanBeReplacedWithoutPretendingItAlreadyOccurred()
    {
        var frame = new QualitativeMotionFrame(); var receiver = new QualitativeDrawingReceiver(frame); var eventIdentity = new Placeholder(); var before = receiver.State;
        Pending(receiver, receiver.Apply(eventIdentity, StructuralConstraint.Open(), S()), before);
        Assert.Single(E(receiver.History));
        var corrected = receiver.Apply(eventIdentity, S(receiver.Jump(S())), new Relation(before, new Placeholder()));
        Assert.True(receiver.TryReadAccepted(corrected, out _, out var strokes)); Assert.Empty(E(strokes)); Assert.Equal(2, E(receiver.History).Length);
    }

    [Fact]
    public void CompleteUnequalReachExpressionCanMoveWhileNetComparisonStaysUnresolved()
    {
        var frame = new QualitativeMotionFrame(); var receiver = new QualitativeDrawingReceiver(frame);
        var a = QualitativeMotionFrame.Move(frame.Right, new Placeholder()); var b = QualitativeMotionFrame.Move(frame.Left, new Placeholder());
        var receipt = Accept(receiver, receiver.Draw(S(a, b))); Assert.True(Closed(receiver.Position)); Assert.Equal(2, E(receiver.Position).Length);
        var proof = Proof(receipt); var reduction = Assert.IsType<Relation>(Assert.IsType<Relation>(Assert.IsType<Relation>(proof.Right).Right).Right);
        Assert.True(frame.TryReadResult(reduction, out var remaining, out _, out var unknown));
        Assert.Same(receiver.Position, remaining); Assert.NotEmpty(E(unknown)); Assert.True(Contains(receiver.State, reduction));
    }

    [Fact]
    public void GuardedReadRejectsOmittedProofAndAStrokeEndpointFusedByCoordinateEquality()
    {
        var frame = new QualitativeMotionFrame(); var receiver = new QualitativeDrawingReceiver(frame);
        var receipt = Accept(receiver, receiver.Draw(S())); Assert.Empty(E(receiver.Position)); Assert.NotSame(receiver.AnchorSite, receiver.CursorSite);
        Assert.True(receiver.TryReadStep(receipt, out _, out _, out var after, out _, out _, out _));
        var body = Assert.IsType<Relation>(receipt.Right); var rest = Assert.IsType<Relation>(body.Right); var pendingProofs = Assert.IsType<Relation>(rest.Right);
        var omitted = new Relation(receipt.Left, new Relation(body.Left, new Relation(rest.Left, new Relation(pendingProofs.Left, S()))));
        Assert.False(receiver.TryReadAccepted(omitted, out _, out _));
        var stroke = Assert.IsType<Relation>(Assert.Single(E(receiver.Strokes)));
        Assert.True(QualitativeDrawingReceiver.TryReadStroke(stroke, out var cause, out var from, out _, out var chord));
        var fused = new Relation(cause, new Relation(new Relation(from, receiver.AnchorSite), chord));
        var changedAfter = ReplaceStrokes(after, S(fused));
        var changedReceipt = new Relation(receipt.Left, new Relation(new Relation(changedAfter, Assert.IsType<Relation>(body.Left).Right), body.Right));
        Assert.False(receiver.TryReadAccepted(changedReceipt, out _, out _));
        Assert.True(receiver.TryReadAccepted(receipt, out _, out _));
    }

    private static Relation ReplaceStrokes(Relation state, Relation strokes)
    {
        var positions = Assert.IsType<Relation>(state.Right); var body = Assert.IsType<Relation>(positions.Right);
        var ledger = Assert.IsType<Relation>(body.Right); var ink = Assert.IsType<Relation>(ledger.Left);
        return new Relation(state.Left, new Relation(positions.Left,
            new Relation(body.Left, new Relation(new Relation(strokes, ink.Right), ledger.Right))));
    }
    private static Relation Proof(Relation receipt)
    {
        var body = Assert.IsType<Relation>(receipt.Right); var rest = Assert.IsType<Relation>(body.Right); var remainder = Assert.IsType<Relation>(rest.Right);
        return Assert.IsType<Relation>(Assert.Single(E(Assert.IsType<Relation>(remainder.Right))));
    }
    private static Relation Accept(QualitativeDrawingReceiver receiver, Relation action)
    {
        var receipt = receiver.Apply(new Placeholder(), S(action), new Placeholder());
        Assert.True(receiver.TryReadAccepted(receipt, out _, out _)); return receipt;
    }
    private static void Pending(QualitativeDrawingReceiver receiver, Relation receipt, Relation before)
    {
        Assert.False(receiver.TryReadAccepted(receipt, out _, out _)); Assert.Same(before, receiver.State);
        Assert.True(receiver.TryReadStep(receipt, out _, out _, out var after, out var accepted, out var blocked, out var pending));
        Assert.Same(before, after); Assert.Empty(E(accepted)); Assert.Empty(E(blocked)); Assert.NotEmpty(E(pending));
    }
    private static void Blocked(QualitativeDrawingReceiver receiver, Relation receipt, Relation before)
    {
        Assert.False(receiver.TryReadAccepted(receipt, out _, out _));
        Assert.True(receiver.TryReadStep(receipt, out _, out _, out var after, out var accepted, out var blocked, out var pending));
        Assert.Same(before, after); Assert.Empty(E(accepted)); Assert.NotEmpty(E(blocked)); Assert.Empty(E(pending));
    }
    private static IEnumerable<IElement> Traverse(IElement root)
    {
        var visited = new HashSet<IElement>(ReferenceEqualityComparer.Instance); var queue = new Stack<IElement>(); queue.Push(root);
        while (queue.TryPop(out var item))
        {
            if (!visited.Add(item)) continue; yield return item;
            if (item is Relation relation) { queue.Push(relation.Left); queue.Push(relation.Right); }
        }
    }
    private static bool Contains(IElement root, IElement target) => Traverse(root).Any(item => ReferenceEquals(item, target));
    private static Relation S(params IElement[] entries) => StructuralConstraint.Closed(entries);
    private static bool Closed(Relation scope) => StructuralConstraint.TryReadScope(scope, out _, out var seam, out var tail) && seam.Equals(tail);
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
}
