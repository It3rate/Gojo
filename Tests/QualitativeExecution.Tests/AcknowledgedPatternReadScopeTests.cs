using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeFrieze;

namespace Gojo.Experiments.QualitativeExecution.Tests;

public sealed class AcknowledgedPatternReadScopeTests
{
    [Fact]
    public void ScopedInductionPreservesTheWholeAcknowledgedCycleAndMaterialAccount()
    {
        var ordinary = Run(false); var scoped = Run(true);
        Assert.Equal(ordinary.ActionCount, scoped.ActionCount);
        Assert.Equal(ordinary.ReceiptCount, scoped.ReceiptCount);
        Assert.Equal(ordinary.HistoryCount, scoped.HistoryCount);
        Assert.Equal(ordinary.Releases, scoped.Releases);
        Assert.Equal(ordinary.Directions, scoped.Directions);
        Assert.Equal(ordinary.CompletionReceipts, scoped.CompletionReceipts);

        static (int ActionCount, int ReceiptCount, int HistoryCount, int Releases, int[] Directions, int CompletionReceipts) Run(bool cache)
        {
            var sequence = Sequence(12);
            using var drawingReads = sequence.Drawing.BeginSynchronousReadScope();
            var scope = cache ? sequence.BeginSynchronousReadScope() : null;
            try
            {
                for (var index = 0; index < E(sequence.Actions).Length; index++)
                {
                    var acknowledgment = sequence.Pulse();
                    Assert.True(sequence.TryReadAccepted(acknowledgment, out _, out _, out _, out _));
                    Assert.True(sequence.TryReadAccepted(acknowledgment, out _, out _, out _, out _));
                }
            }
            finally { scope?.Dispose(); }
            Assert.True(sequence.IsAtBoundary); Assert.Empty(E(sequence.Unresolved));
            Assert.Empty(E(sequence.CurrentCycleReceipts)); Assert.Single(E(sequence.CompletionEvents));
            var completion = Assert.IsType<Relation>(Assert.Single(E(sequence.CompletionEvents)));
            var receipts = E((Relation)((Relation)completion.Right).Left);
            Assert.Equal(E(sequence.Actions).Length, receipts.Length);
            foreach (var receipt in receipts.Cast<Relation>()) Assert.True(sequence.Drawing.TryReadAccepted(receipt, out _, out _));
            var directions = E(sequence.Drawing.Strokes).Cast<Relation>().SelectMany(stroke =>
            {
                Assert.True(QualitativeDrawingReceiver.TryReadStroke(stroke, out _, out _, out _, out var word));
                return E(word).Cast<Relation>().Select(move =>
                {
                    Assert.True(sequence.Drawing.Frame.Direction(move, out var axis, out var forward, out _));
                    return ReferenceEquals(axis, sequence.Drawing.Frame.Horizontal) ? forward ? 1 : -1 : forward ? 2 : -2;
                });
            }).ToArray();
            var account = sequence.Account();
            var tail = (Relation)((Relation)((Relation)((Relation)account.Right).Right).Right).Right;
            Assert.Empty(E(tail));
            return (E(sequence.Actions).Length, E(sequence.Timeline).Length, E(sequence.Drawing.History).Length,
                E(sequence.ReleasedPackets).Length, directions, receipts.Length);
        }
    }

    [Fact]
    public void WarmPrefixesDoNotCertifyCopiedMissingOrWronglyBoundProducerEvidence()
    {
        var sequence = Sequence(5);
        using var drawingReads = sequence.Drawing.BeginSynchronousReadScope();
        var initial = sequence.ControllerState;
        Relation first, firstController;
        using (sequence.BeginSynchronousReadScope())
        {
            Assert.True(sequence.TryReadCyclePrefix(sequence.CurrentCycleReceipts, initial));
            Assert.True(sequence.TryReadAccepted(sequence.Pulse(), out _, out _, out _, out _));
            first = sequence.CurrentCycleReceipts; firstController = sequence.ControllerState;
            Assert.True(sequence.TryReadCyclePrefix(first, firstController));
            using (sequence.BeginSynchronousReadScope())
            {
                Assert.True(sequence.TryReadAccepted(sequence.Pulse(), out _, out _, out _, out _));
                var prefix = sequence.CurrentCycleReceipts; var controller = sequence.ControllerState;
                Assert.True(sequence.TryReadCyclePrefix(prefix, controller));
                Assert.True(sequence.TryReadCyclePrefix(S(E(prefix)), controller));
                Assert.False(sequence.TryReadCyclePrefix(S(E(prefix).Take(1).ToArray()), controller));
                Assert.False(sequence.TryReadCyclePrefix(S(E(prefix).Reverse().ToArray()), controller));
                Assert.False(sequence.TryReadCyclePrefix(S(new Placeholder(), E(prefix)[1]), controller));
                Assert.False(sequence.TryReadCyclePrefix(prefix, firstController));
                Assert.False(sequence.TryReadCyclePrefix(first, controller));
                Assert.False(sequence.TryReadCyclePrefix(prefix, initial));
                var beforeScope = new Relation(controller.Left, controller.Right);
                Assert.True(sequence.TryReadCyclePrefix(prefix, beforeScope));
                // Equal state shape is a fresh read. It does not establish
                // provenance for an unrelated or malformed phase state.
                Assert.False(sequence.TryReadCyclePrefix(prefix, new Relation(controller.Left, S())));
            }
            Assert.True(sequence.TryReadCyclePrefix(first, firstController));
        }
        // Disposal removes the shortcut. Exact original and copied inputs
        // remain readable; missing effects and wrong states remain false.
        Assert.True(sequence.TryReadCyclePrefix(first, firstController));
        Assert.True(sequence.TryReadCyclePrefix(S(E(first)), firstController));
        Assert.False(sequence.TryReadCyclePrefix(S(), firstController));
        Assert.False(sequence.TryReadCyclePrefix(first, initial));
    }

    [Fact]
    public void ScopedPrefixReuseDoesNotAcceptAFabricatedAcknowledgmentOrStaleRequest()
    {
        var sequence = Sequence(4);
        using var drawingReads = sequence.Drawing.BeginSynchronousReadScope();
        using (sequence.BeginSynchronousReadScope())
        {
            var accepted = sequence.Pulse(); Assert.True(sequence.TryReadAccepted(accepted, out _, out _, out _, out _));
            var context = (Relation)((Relation)accepted.Left).Left; var request = (Relation)context.Right;
            var copy = new Relation(request.Left, request.Right);
            var effect = (Relation)((Relation)accepted.Left).Right;
            Assert.False(sequence.TryReadAccepted(sequence.Acknowledge(copy, effect), out _, out _, out _, out _));
            Assert.False(sequence.TryReadAccepted(sequence.Acknowledge(request, effect), out _, out _, out _, out _));
            var body = (Relation)accepted.Right; var steps = (Relation)body.Left;
            var forged = new Relation(accepted.Left, new Relation(new Relation(S(), steps.Right), body.Right));
            Assert.False(sequence.TryReadAccepted(forged, out _, out _, out _, out _));
            Assert.True(sequence.TryReadAccepted(accepted, out _, out _, out _, out _));
        }
        Assert.True(sequence.TryReadAccepted(Assert.IsType<Relation>(E(sequence.Timeline)[0]), out _, out _, out _, out _));
    }

    private static AcknowledgedPatternSequence Sequence(int actions)
    {
        var frame = new QualitativeMotionFrame(); var drawing = new QualitativeDrawingReceiver(frame); IElement reach = new Placeholder();
        var directions = new[] { frame.Right, frame.Up, frame.Left, frame.Down };
        return new("bounded native cycle", drawing, S(Enumerable.Range(0, actions).Select(index =>
            (IElement)drawing.Draw(S(QualitativeMotionFrame.Move(directions[index % directions.Length], reach)))).ToArray()));
    }
    private static Relation S(params IElement[] values) => StructuralConstraint.Closed(values);
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
}
