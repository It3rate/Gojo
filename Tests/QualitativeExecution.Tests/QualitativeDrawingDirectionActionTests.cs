using Gojo.Core;
using Gojo.Patterns;

namespace Gojo.Experiments.QualitativeExecution.Tests;

public sealed class QualitativeDrawingDirectionActionTests
{
    [Fact]
    public void DirectionActionPortCarriesActualOppositePairsAndAcceptedWitnessUsesThem()
    {
        var frame = new QualitativeMotionFrame(); var receiver = new QualitativeDrawingReceiver(frame);
        Assert.Same(receiver.OrientationPort, receiver.DirectionActionPort.Left);
        Assert.Same(frame.Opposites, receiver.DirectionActionPort.Right);
        var map = Turn(frame); var action = receiver.DirectionAction(map);
        Assert.True(receiver.TryReadDirectionAction(action, out var actual)); Assert.Same(map, actual);
        Assert.False(receiver.TryReadMirror(action, out _));
        var cursor = receiver.CursorSite; var anchor = receiver.AnchorSite; var sites = receiver.Sites; var strokes = receiver.Strokes;
        var history = E(receiver.History); var occurrence = new Placeholder();
        var receipt = receiver.Apply(occurrence, S(action), new Placeholder());
        Assert.True(receiver.TryReadAccepted(receipt, out _, out var added)); Assert.Empty(E(added));
        Assert.Same(cursor, receiver.CursorSite); Assert.Same(anchor, receiver.AnchorSite);
        Assert.Same(sites, receiver.Sites); Assert.Same(strokes, receiver.Strokes);
        Assert.Same(history[0], E(receiver.History)[0]); Assert.Same(occurrence, E(receiver.History)[1]);
        var projection = Projection(Proof(receipt));
        var pairs = Assert.IsType<Relation>(Assert.IsType<Relation>(projection.Right).Right);
        Assert.Equal(4, E(pairs).Length);
        foreach (var entry in E(pairs).Cast<Relation>())
        {
            var correspondence = Assert.IsType<Relation>(entry.Left); var rows = Assert.IsType<Relation>(entry.Right);
            Assert.Contains(E(frame.Opposites), pair => ReferenceEquals(pair, correspondence.Left));
            Assert.Contains(E(frame.Opposites), pair => ReferenceEquals(pair, correspondence.Right));
            Assert.Contains(E(map), row => ReferenceEquals(row, rows.Left));
            Assert.Contains(E(map), row => ReferenceEquals(row, rows.Right));
        }
    }

    [Fact]
    public void TwoTurnsNegateBothAxesAndFourTurnsReturnWithoutCreatingSitesOrStrokes()
    {
        var frame = new QualitativeMotionFrame(); var receiver = new QualitativeDrawingReceiver(frame); var turn = Turn(frame);
        Accept(receiver, receiver.DirectionAction(turn)); Accept(receiver, receiver.DirectionAction(turn));
        foreach (var pair in E(frame.Opposites).Cast<Relation>()) Assert.Same(pair.Right, Image(receiver.Orientation, pair.Left));
        Accept(receiver, receiver.DirectionAction(turn)); Accept(receiver, receiver.DirectionAction(turn));
        foreach (var row in E(receiver.Orientation).Cast<Relation>()) Assert.Same(row.Left, row.Right);
        Assert.Single(E(receiver.Sites)); Assert.Empty(E(receiver.Strokes)); Assert.Equal(5, E(receiver.History).Length);
    }

    [Fact]
    public void TurnAffectsFutureDrawingWhileExistingStrokeAndAnchorKeepTheirIdentity()
    {
        var frame = new QualitativeMotionFrame(); var receiver = new QualitativeDrawingReceiver(frame); var reach = new Placeholder();
        var right = QualitativeMotionFrame.Move(frame.Right, reach);
        Accept(receiver, receiver.Draw(S(right))); var original = Assert.Single(E(receiver.Strokes));
        var cursor = receiver.CursorSite; var position = receiver.Position; var anchor = receiver.AnchorSite;
        Accept(receiver, receiver.DirectionAction(Turn(frame)));
        Assert.Same(cursor, receiver.CursorSite); Assert.Same(position, receiver.Position); Assert.Same(anchor, receiver.AnchorSite);
        var receipt = Accept(receiver, receiver.Draw(S(right)));
        Assert.True(receiver.TryReadAccepted(receipt, out _, out var added));
        Assert.True(QualitativeDrawingReceiver.TryReadStroke(Assert.IsType<Relation>(Assert.Single(E(added))), out _, out var from, out _, out var chord));
        var move = Assert.IsType<Relation>(Assert.Single(E(chord))); Assert.Same(frame.Up, move.Left); Assert.Same(reach, move.Right); Assert.Same(cursor, from);
        Assert.Same(original, E(receiver.Strokes)[0]);
        Assert.True(QualitativeDrawingReceiver.TryReadStroke(Assert.IsType<Relation>(original), out _, out _, out _, out var originalWord));
        Assert.Same(right, Assert.Single(E(originalWord)));
        Accept(receiver, receiver.Reset()); Assert.Same(anchor, receiver.CursorSite);
        Assert.Same(frame.Up, Image(receiver.Orientation, frame.Right));
    }

    [Fact]
    public void ExistingMirrorRejectsCrossAxisMapsWhileDirectionActionAcceptsThem()
    {
        var frame = new QualitativeMotionFrame(); var receiver = new QualitativeDrawingReceiver(frame); var turn = Turn(frame); var before = receiver.State;
        Pending(receiver, receiver.Apply(new Placeholder(), S(receiver.Mirror(turn)), S()), before);
        Accept(receiver, receiver.DirectionAction(turn));
        Accept(receiver, receiver.Mirror(receiver.HorizontalMirror));
        Assert.Same(frame.Up, Image(receiver.Orientation, frame.Right));
        Assert.Same(frame.Right, Image(receiver.Orientation, frame.Up));
    }

    [Fact]
    public void OpenUnknownForeignDuplicateAndNonOppositeMapsRemainPending()
    {
        var frame = new QualitativeMotionFrame(); var receiver = new QualitativeDrawingReceiver(frame); var foreign = new QualitativeMotionFrame();
        var rows = E(Turn(frame)); var before = receiver.State;
        var maps = new[] {
            StructuralConstraint.Open(), StructuralConstraint.Open(rows), S(new Placeholder()), S(rows.Take(3).ToArray()),
            Turn(foreign), S(rows[0], rows[0], rows[2], rows[3]),
            S(new Relation(frame.Right, frame.Up), new Relation(frame.Left, frame.Down), new Relation(frame.Up, frame.Up), new Relation(frame.Down, frame.Left)),
            S(new Relation(frame.Right, frame.Up), new Relation(frame.Left, frame.Right), new Relation(frame.Up, frame.Down), new Relation(frame.Down, frame.Left))
        };
        foreach (var map in maps) Pending(receiver, receiver.Apply(new Placeholder(), S(receiver.DirectionAction(map)), S()), before);
        Assert.Single(E(receiver.History)); Assert.Single(E(receiver.Sites)); Assert.Empty(E(receiver.Strokes));
    }

    [Fact]
    public void ReflectionConjugatesTurnToItsInverseAndActionOrderIsPreserved()
    {
        var frame = new QualitativeMotionFrame(); var turn = Turn(frame);
        var mirrored = new QualitativeDrawingReceiver(frame); var inverse = new QualitativeDrawingReceiver(frame);
        Accept(mirrored, mirrored.Mirror(mirrored.HorizontalMirror));
        Accept(mirrored, mirrored.DirectionAction(turn));
        Accept(mirrored, mirrored.Mirror(mirrored.HorizontalMirror));
        Accept(inverse, inverse.DirectionAction(S(E(turn).Cast<Relation>().Select(row => (IElement)new Relation(row.Right, row.Left)).ToArray())));
        SameMap(mirrored.Orientation, inverse.Orientation);
        var first = new QualitativeDrawingReceiver(frame); var second = new QualitativeDrawingReceiver(frame);
        Accept(first, first.DirectionAction(turn)); Accept(first, first.Mirror(first.HorizontalMirror));
        Accept(second, second.Mirror(second.HorizontalMirror)); Accept(second, second.DirectionAction(turn));
        Assert.NotSame(Image(first.Orientation, frame.Right), Image(second.Orientation, frame.Right));
    }

    [Fact]
    public void DuplicateOrStaleActionOffersCannotAdvanceTheLedger()
    {
        var frame = new QualitativeMotionFrame(); var receiver = new QualitativeDrawingReceiver(frame); var before = receiver.State;
        Assert.True(receiver.TryReadState(before, out var initialEvent, out _, out _, out _, out _, out _, out _));
        var occurrence = new Placeholder(); var action = receiver.DirectionAction(Turn(frame));
        var accepted = receiver.Apply(occurrence, S(action), S()); Assert.True(receiver.TryReadAccepted(accepted, out var after, out _));
        Assert.True(receiver.TryReadStep(accepted, out _, out var offer, out _, out _, out _, out _));
        Blocked(receiver, receiver.Step(after, offer), after);
        Blocked(receiver, receiver.Apply(new Placeholder(), S(action), S(), initialEvent), after);
        Assert.Equal(2, E(receiver.History).Length); Assert.Same(frame.Up, Image(receiver.Orientation, frame.Right));
    }

    [Fact]
    public void AcceptedReaderRejectsForgedCompositionAndMissingOppositeCorrespondence()
    {
        var frame = new QualitativeMotionFrame(); var receiver = new QualitativeDrawingReceiver(frame);
        var receipt = Accept(receiver, receiver.DirectionAction(Turn(frame))); var proof = Proof(receipt); var projection = Projection(proof);
        var result = Assert.IsType<Relation>(projection.Right);
        var noPairs = new Relation(projection.Left, new Relation(result.Left, S()));
        Assert.False(receiver.TryReadAccepted(ReplaceProjection(receipt, noPairs), out _, out _));
        var wrongComposition = new Relation(projection.Left, new Relation(receiver.IdentityOrientation, result.Right));
        Assert.False(receiver.TryReadAccepted(ReplaceProjection(receipt, wrongComposition), out _, out _));
        Assert.True(receiver.TryReadAccepted(receipt, out _, out _));
    }

    private static Relation ReplaceProjection(Relation receipt, Relation projection)
    {
        var proof = Proof(receipt); var proofBody = Assert.IsType<Relation>(proof.Right); var effect = Assert.IsType<Relation>(proofBody.Right);
        var replacement = new Relation(proof.Left, new Relation(proofBody.Left, new Relation(projection, effect.Right)));
        var body = Assert.IsType<Relation>(receipt.Right); var result = Assert.IsType<Relation>(body.Left); var after = Assert.IsType<Relation>(result.Left);
        var positions = Assert.IsType<Relation>(after.Right); var stateBody = Assert.IsType<Relation>(positions.Right); var ledger = Assert.IsType<Relation>(stateBody.Right);
        var provenance = Assert.IsType<Relation>(ledger.Right); var operation = Assert.IsType<Relation>(provenance.Right);
        var revisedAfter = new Relation(after.Left, new Relation(positions.Left, new Relation(stateBody.Left,
            new Relation(ledger.Left, new Relation(provenance.Left, new Relation(operation.Left, replacement))))));
        var refusal = Assert.IsType<Relation>(body.Right); var remainder = Assert.IsType<Relation>(refusal.Right);
        return new Relation(receipt.Left, new Relation(new Relation(revisedAfter, result.Right),
            new Relation(refusal.Left, new Relation(remainder.Left, S(replacement)))));
    }
    private static Relation Projection(Relation proof) => Assert.IsType<Relation>(Assert.IsType<Relation>(Assert.IsType<Relation>(proof.Right).Right).Left);
    private static Relation Proof(Relation receipt)
    {
        var body = Assert.IsType<Relation>(receipt.Right); var refusal = Assert.IsType<Relation>(body.Right);
        var remainder = Assert.IsType<Relation>(refusal.Right); var proofs = Assert.IsType<Relation>(remainder.Right);
        return Assert.IsType<Relation>(Assert.Single(E(proofs)));
    }
    private static Relation Turn(QualitativeMotionFrame frame) => S(new Relation(frame.Right, frame.Up), new Relation(frame.Up, frame.Left),
        new Relation(frame.Left, frame.Down), new Relation(frame.Down, frame.Right));
    private static IElement Image(Relation map, IElement source) => E(map).Cast<Relation>().Single(row => ReferenceEquals(row.Left, source)).Right;
    private static void SameMap(Relation actual, Relation expected) => Assert.All(E(expected).Cast<Relation>(), row => Assert.Same(row.Right, Image(actual, row.Left)));
    private static Relation Accept(QualitativeDrawingReceiver receiver, Relation action)
    {
        var receipt = receiver.Apply(new Placeholder(), S(action), S()); Assert.True(receiver.TryReadAccepted(receipt, out _, out _)); return receipt;
    }
    private static void Pending(QualitativeDrawingReceiver receiver, Relation receipt, Relation before)
    {
        Assert.False(receiver.TryReadAccepted(receipt, out _, out _)); Assert.Same(before, receiver.State);
        Assert.True(receiver.TryReadStep(receipt, out _, out _, out var after, out var accepted, out var blocked, out var pending));
        Assert.Same(before, after); Assert.Empty(E(accepted)); Assert.Empty(E(blocked)); Assert.NotEmpty(E(pending));
    }
    private static void Blocked(QualitativeDrawingReceiver receiver, Relation receipt, Relation before)
    {
        Assert.False(receiver.TryReadAccepted(receipt, out _, out _)); Assert.Same(before, receiver.State);
        Assert.True(receiver.TryReadStep(receipt, out _, out _, out var after, out var accepted, out var blocked, out var pending));
        Assert.Same(before, after); Assert.Empty(E(accepted)); Assert.NotEmpty(E(blocked)); Assert.Empty(E(pending));
    }
    private static Relation S(params IElement[] entries) => StructuralConstraint.Closed(entries);
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
}
