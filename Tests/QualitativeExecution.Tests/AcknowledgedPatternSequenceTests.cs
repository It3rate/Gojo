using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeFrieze;

namespace Gojo.Experiments.QualitativeExecution.Tests;

public sealed class AcknowledgedPatternSequenceTests
{
    [Fact]
    public void ProposalAndPureEffectDoNotAdvanceOrDrainBeforeExactAcknowledgment()
    {
        var sequence = Sequence(); var phase = sequence.ControllerState; var before = sequence.Drawing.State;
        var packet = new Placeholder(); var request = sequence.Propose(new Placeholder(), S(packet));
        Assert.True(sequence.TryReadRequest(request, out var action, out var actualBefore, out var offer));
        Assert.Same(E(sequence.Actions)[0], action); Assert.Same(before, actualBefore);
        Assert.Same(phase, sequence.ControllerState); Assert.Same(before, sequence.Drawing.State);
        Assert.Single(E(sequence.Pending)); Assert.Empty(E(sequence.ReleasedPackets));
        var effect = sequence.Drawing.Step(before, offer);
        Assert.True(sequence.Drawing.TryReadAccepted(effect, out _, out _)); Assert.Same(before, sequence.Drawing.State);
        var acknowledgment = sequence.Acknowledge(request, effect);
        Assert.True(sequence.TryReadAccepted(acknowledgment, out var accepted, out _, out _, out var completions));
        Assert.Same(effect, accepted); Assert.Empty(E(completions)); Assert.NotSame(phase, sequence.ControllerState);
        Assert.Empty(E(sequence.Pending)); Assert.Single(E(sequence.Drawing.Strokes));
        Assert.Single(E(sequence.ReleasedPackets)); AssertAccounted(sequence);
    }

    [Fact]
    public void CompletionRetainsTheActualWholeCycleEffectsAndNoNumbers()
    {
        var sequence = Sequence(); Assert.True(sequence.IsAtBoundary);
        var first = sequence.Pulse(); Assert.False(sequence.IsAtBoundary); Assert.Empty(E(sequence.CompletionEvents));
        var second = sequence.Pulse(); Assert.True(sequence.IsAtBoundary);
        Assert.True(sequence.TryReadAccepted(first, out var firstEffect, out _, out _, out _));
        Assert.True(sequence.TryReadAccepted(second, out var secondEffect, out var finalPhase, out _, out var complete));
        var completion = Assert.IsType<Relation>(Assert.Single(E(complete)));
        Assert.Same(completion, sequence.LastCompletion); Assert.Same(completion, Assert.Single(E(sequence.CompletionEvents)));
        var owner = Assert.IsType<Relation>(completion.Left); Assert.Same(sequence.CompletionPort, owner.Left);
        var proof = Assert.IsType<Relation>(completion.Right); Assert.Same(finalPhase, proof.Right);
        Assert.Equal(new IElement[] { firstEffect, secondEffect }, E(Assert.IsType<Relation>(proof.Left)));
        Assert.Empty(E(sequence.CurrentCycleReceipts)); Assert.Empty(E(sequence.Unresolved));
        Assert.DoesNotContain(Traverse(new Relation(sequence.Surface, sequence.Timeline)), element => element is Carrier or CarrierLocation);
        AssertAccounted(sequence);
    }

    [Fact]
    public void MissingPacketDoesNotCreateAPendingActionOrMove()
    {
        var sequence = Sequence(); var phase = sequence.ControllerState; var drawing = sequence.Drawing.State;
        var quiet = sequence.Drive(new Placeholder(), S());
        Assert.False(sequence.TryReadRequest(quiet, out _, out _, out _));
        Assert.Same(phase, sequence.ControllerState); Assert.Same(drawing, sequence.Drawing.State);
        Assert.Empty(E(sequence.Pending)); Assert.Empty(E(sequence.ImportedPackets)); AssertAccounted(sequence);
    }

    [Fact]
    public void WrongOfferAndAlternateAcceptedBranchCannotUnlockThePendingStep()
    {
        var sequence = Sequence(); var request = sequence.Propose(new Placeholder(), S(new Placeholder()));
        Assert.True(sequence.TryReadRequest(request, out var action, out var before, out var offer));
        Assert.True(sequence.Drawing.TryReadState(before, out var prior, out _, out _, out _, out _, out _, out _));
        var alternative = sequence.Drawing.Step(before,
            sequence.Drawing.Offer(sequence.Drawing.Source, new Placeholder(), prior, S(action), request));
        Assert.True(sequence.Drawing.TryReadAccepted(alternative, out _, out _));
        var phase = sequence.ControllerState;
        var refused = sequence.Acknowledge(request, alternative);
        Assert.False(sequence.TryReadAccepted(refused, out _, out _, out _, out _));
        Assert.Same(before, sequence.Drawing.State); Assert.Same(phase, sequence.ControllerState);
        Assert.Same(request, Assert.Single(E(sequence.Pending))); Assert.Empty(E(sequence.ReleasedPackets));
        var accepted = sequence.Acknowledge(request, sequence.Drawing.Step(before, offer));
        Assert.True(sequence.TryReadAccepted(accepted, out _, out _, out _, out _));
        Assert.Single(E(sequence.Drawing.Strokes)); AssertAccounted(sequence);
    }

    [Fact]
    public void RejectedEffectLeavesTheSameRequestHeldAndRetryDoesNotDuplicateStrokes()
    {
        var sequence = Sequence(); var request = sequence.Propose(new Placeholder(), S(new Placeholder()));
        Assert.True(sequence.TryReadRequest(request, out var action, out var before, out var offer));
        var rejected = sequence.Drawing.Step(before,
            sequence.Drawing.Offer(sequence.Drawing.Source, new Placeholder(), new Placeholder(), S(action), request));
        Assert.False(sequence.Drawing.TryReadAccepted(rejected, out _, out _));
        sequence.Acknowledge(request, rejected); Assert.Same(request, Assert.Single(E(sequence.Pending)));
        var actual = sequence.Drawing.Step(before, offer);
        var accepted = sequence.Acknowledge(request, actual);
        Assert.True(sequence.TryReadAccepted(accepted, out _, out _, out _, out _));
        var phase = sequence.ControllerState; var drawing = sequence.Drawing.State;
        var replay = sequence.Acknowledge(request, actual);
        Assert.False(sequence.TryReadAccepted(replay, out _, out _, out _, out _));
        Assert.Same(phase, sequence.ControllerState); Assert.Same(drawing, sequence.Drawing.State);
        Assert.Single(E(sequence.Drawing.Strokes)); Assert.Single(E(sequence.ReleasedPackets)); AssertAccounted(sequence);
    }

    [Fact]
    public void HistoricalAcceptedEffectCannotOverwriteTheLiveDrawingHead()
    {
        var sequence = Sequence(); var request = sequence.Propose(new Placeholder(), S(new Placeholder()));
        Assert.True(sequence.TryReadRequest(request, out var action, out var before, out var offer));
        var old = sequence.Drawing.Step(before, offer);
        Assert.True(sequence.Drawing.TryReadAccepted(old, out _, out _));
        sequence.Drawing.Apply(new Placeholder(), S(action), new Placeholder());
        var live = sequence.Drawing.State; var phase = sequence.ControllerState;
        Assert.False(sequence.Drawing.TryCommit(old));
        Assert.False(sequence.TryReadAccepted(sequence.Acknowledge(request, old), out _, out _, out _, out _));
        Assert.Same(live, sequence.Drawing.State); Assert.Same(phase, sequence.ControllerState);
        Assert.Same(request, Assert.Single(E(sequence.Pending))); Assert.Empty(E(sequence.ReleasedPackets)); AssertAccounted(sequence);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void UnknownAndOpenActionsStayPendingWithoutDrainingOrInventingAnEffect(bool openWord)
    {
        var drawing = new QualitativeDrawingReceiver(new QualitativeMotionFrame());
        var action = openWord ? drawing.Draw(StructuralConstraint.Open(QualitativeMotionFrame.Move(drawing.Frame.Right, new Placeholder())))
            : new Relation(new Placeholder(), new Placeholder());
        var sequence = new AcknowledgedPatternSequence("pending", drawing, S(action));
        var phase = sequence.ControllerState; var before = drawing.State;
        var result = sequence.Pulse(); Assert.False(sequence.TryReadAccepted(result, out _, out _, out _, out _));
        Assert.Single(E(sequence.Pending)); Assert.Same(phase, sequence.ControllerState); Assert.Same(before, drawing.State);
        Assert.Empty(E(sequence.ReleasedPackets)); Assert.Empty(E(drawing.Strokes)); AssertAccounted(sequence);
    }

    [Fact]
    public void FreshInputDuringAnOutstandingActionStaysExplicitlyRetainedAtItsPort()
    {
        var sequence = Sequence(); var request = sequence.Propose(new Placeholder(), S(new Placeholder()));
        var held = sequence.ReservoirState; var extra = new Placeholder();
        sequence.Propose(new Placeholder(), S(extra));
        Assert.Same(held, sequence.ReservoirState); Assert.Same(request, Assert.Single(E(sequence.Pending)));
        var retained = Assert.IsType<Relation>(Assert.Single(E(sequence.RetainedPackets)));
        Assert.Same(sequence.InputPort, retained.Left);
        Assert.Same(extra, Assert.Single(E(Assert.IsType<Relation>(Assert.IsType<Relation>(retained.Right).Right))));
        Assert.True(sequence.TryReadRequest(request, out _, out var before, out var offer));
        sequence.Acknowledge(request, sequence.Drawing.Step(before, offer));
        Assert.Equal(2, E(sequence.ImportedPackets).Length); Assert.Single(E(sequence.ReleasedPackets)); AssertAccounted(sequence);
    }

    [Fact]
    public void IntakeCapacityRetainsOverflowAndDuplicateImportsCannotCreateMaterial()
    {
        var sequence = Sequence(); var first = new Placeholder(); var second = new Placeholder();
        var request = sequence.Propose(new Placeholder(), S(first, second));
        Assert.Single(E(sequence.RetainedPackets)); Assert.Equal(2, E(sequence.ImportedPackets).Length);
        sequence.Propose(new Placeholder(), S(first)); Assert.Equal(2, E(sequence.ImportedPackets).Length);
        Assert.True(sequence.TryReadRequest(request, out _, out var before, out var offer));
        sequence.Acknowledge(request, sequence.Drawing.Step(before, offer)); AssertAccounted(sequence);
    }

    [Fact]
    public void CopiedRequestShapeCannotReplaceTheExactOutstandingRequest()
    {
        var sequence = Sequence(); var request = sequence.Propose(new Placeholder(), S(new Placeholder()));
        Assert.True(sequence.TryReadRequest(request, out _, out var before, out var offer));
        var copy = new Relation(request.Left, request.Right); var phase = sequence.ControllerState;
        var effect = sequence.Drawing.Step(before, offer);
        Assert.True(sequence.Drawing.TryReadAccepted(effect, out _, out _));
        Assert.False(sequence.TryReadAccepted(sequence.Acknowledge(copy, effect), out _, out _, out _, out _));
        Assert.Same(before, sequence.Drawing.State); Assert.Same(phase, sequence.ControllerState);
        Assert.True(sequence.TryReadAccepted(sequence.Acknowledge(request, effect), out _, out _, out _, out _)); AssertAccounted(sequence);
    }

    [Fact]
    public void CompletionInspectionRejectsForeignMissingReorderedAndInventedCycleEvidence()
    {
        var sequence = Sequence(); var first = sequence.Pulse(); var terminal = sequence.Pulse();
        Assert.True(sequence.TryReadAccepted(first, out var firstEffect, out _, out _, out _));
        Assert.True(sequence.TryReadAccepted(terminal, out var lastEffect, out var phaseStep, out _, out var completions));
        var completion = Assert.IsType<Relation>(Assert.Single(E(completions)));
        var occurrence = Assert.IsType<Relation>(completion.Left); var proof = Assert.IsType<Relation>(completion.Right);
        var result = Assert.IsType<Relation>(terminal.Right); var steps = Assert.IsType<Relation>(result.Left);
        var forgeries = new[] {
            new Relation(terminal.Left, new Relation(new Relation(new Relation(new Placeholder(), new Placeholder()), steps.Right), result.Right)),
            new Relation(terminal.Left, new Relation(new Relation(StructuralConstraint.Open(phaseStep), steps.Right), result.Right)),
            new Relation(terminal.Left, new Relation(new Relation(steps.Left, new Relation(new Placeholder(), new Placeholder())), result.Right)),
            WithCompletions(first, completions),
            WithCompletions(terminal, S()),
            WithCompletions(terminal, S(completion, completion)),
            WithCompletions(terminal, S(new Relation(new Relation(new Placeholder(), occurrence.Right), proof))),
            WithCompletions(terminal, S(new Relation(new Relation(sequence.CompletionPort, new Placeholder()), proof))),
            WithCompletions(terminal, S(new Relation(occurrence, new Relation(S(firstEffect, lastEffect), new Relation(phaseStep.Left, phaseStep.Right))))),
            WithCompletions(terminal, S(new Relation(occurrence, new Relation(S(), phaseStep)))),
            WithCompletions(terminal, S(new Relation(occurrence, new Relation(S(lastEffect, firstEffect), phaseStep)))),
            WithCompletions(terminal, S(new Relation(occurrence, new Relation(S(new Relation(firstEffect.Left, firstEffect.Right), lastEffect), phaseStep))))
        };
        Assert.All(forgeries, forged => Assert.False(sequence.TryReadAccepted(forged, out _, out _, out _, out _)));
        sequence.Pulse(); sequence.Pulse();
        Assert.True(sequence.TryReadAccepted(first, out _, out _, out _, out _));
        Assert.True(sequence.TryReadAccepted(terminal, out _, out _, out _, out _));
        Assert.Equal(2, E(sequence.CompletionEvents).Length); AssertAccounted(sequence);
    }

    private static AcknowledgedPatternSequence Sequence()
    {
        var frame = new QualitativeMotionFrame(); var drawing = new QualitativeDrawingReceiver(frame); var reach = new Placeholder();
        return new("acknowledged corner", drawing,
            S(drawing.Draw(S(QualitativeMotionFrame.Move(frame.Right, reach))), drawing.Draw(S(QualitativeMotionFrame.Move(frame.Up, reach)))));
    }
    private static Relation WithCompletions(Relation acknowledgment, Relation completions)
    {
        var result = Assert.IsType<Relation>(acknowledgment.Right); var outcome = Assert.IsType<Relation>(result.Right);
        return new Relation(acknowledgment.Left, new Relation(result.Left, new Relation(completions, outcome.Right)));
    }
    private static void AssertAccounted(AcknowledgedPatternSequence sequence)
    {
        var read = sequence.Account(); var active = Assert.IsType<Relation>(read.Right);
        var retained = Assert.IsType<Relation>(active.Right); var released = Assert.IsType<Relation>(retained.Right);
        Assert.Empty(E(Assert.IsType<Relation>(released.Right)));
        Assert.Equal(E(sequence.ImportedPackets).Length, E(Assert.IsType<Relation>(active.Left)).Length +
            E(Assert.IsType<Relation>(retained.Left)).Length + E(Assert.IsType<Relation>(released.Left)).Length);
    }
    private static IEnumerable<IElement> Traverse(IElement root)
    {
        var seen = new HashSet<IElement>(ReferenceEqualityComparer.Instance); var stack = new Stack<IElement>(); stack.Push(root);
        while (stack.TryPop(out var element))
        { if (!seen.Add(element)) continue; yield return element; if (element is Relation row) { stack.Push(row.Left); stack.Push(row.Right); } }
    }
    private static Relation S(params IElement[] values) => StructuralConstraint.Closed(values);
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
}
