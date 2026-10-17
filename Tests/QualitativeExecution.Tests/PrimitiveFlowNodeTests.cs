using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeFrieze;

namespace Gojo.Experiments.QualitativeExecution.Tests;

public sealed class PrimitiveFlowNodeTests
{
    [Fact]
    public void ReusedStepHasDistinctCallPhasesAndOnlyWholeChildReturnsAdvanceItsParent()
    {
        var f = Staircase(); var root = f.Root; var step = f.Step;
        Assert.True(root.IsAtBoundary); Assert.Same(root.Children[0], root.Children[1]);
        var calls = E(root.CallRows).Cast<Relation>().ToArray();
        Assert.NotSame(calls[0].Left, calls[1].Left);
        Assert.Same(((Relation)calls[0].Right).Left, ((Relation)calls[1].Right).Left);
        var parentBefore = root.ControllerState;
        Drive(root); Assert.False(root.IsAtBoundary); Assert.Same(parentBefore, root.ControllerState);
        Assert.Empty(E(step.CompletionEvents)); Assert.Empty(E(root.CompletionEvents));
        Drive(root); Assert.NotSame(parentBefore, root.ControllerState);
        Assert.Single(E(step.CompletionEvents)); Assert.Empty(E(root.CompletionEvents));
        Drive(root); Assert.Empty(E(root.CompletionEvents));
        var returned = Drive(root); Assert.True(root.TryReadAccepted(returned, out var completions));
        Assert.Single(E(completions)); Assert.Single(E(root.CompletionEvents)); Assert.Equal(2, E(step.CompletionEvents).Length);
        Assert.True(root.IsAtBoundary); Assert.Equal(4, E(root.Drawing.Strokes).Length);
        Assert.Empty(E(root.CurrentCycleReturns)); Assert.Equal(2, E(root.Account()).Length);
        AssertNoNumbers(root.Capture()); AssertAccounted(root, 4);
    }

    [Fact]
    public void MultiActionLeafDoesNotBecomeAParentReturnAfterItsFirstAcceptedStroke()
    {
        var drawing = new QualitativeDrawingReceiver(new()); var reach = new Placeholder();
        var leaf = new PrimitiveFlowLeaf("step", drawing, S(D(drawing, drawing.Frame.Right, reach), D(drawing, drawing.Frame.Up, reach)));
        var root = new PrimitiveFlowGroup("one complete step", leaf); var phase = root.ControllerState;
        var first = Drive(root); Assert.True(root.TryReadAccepted(first, out var firstCompletions));
        Assert.Empty(E(firstCompletions)); Assert.Same(phase, root.ControllerState); Assert.False(root.IsAtBoundary);
        Drive(root); Assert.Single(E(root.CompletionEvents)); Assert.True(root.IsAtBoundary); AssertAccounted(root, 2);
    }

    [Fact]
    public void RejectedActuatorBranchPreservesTheNestedRequestPacketAndEveryParentPhase()
    {
        var f = Staircase(); var rootPhase = f.Root.ControllerState; var stepPhase = f.Step.ControllerState;
        var request = f.Root.Propose(new Placeholder(), S(new Placeholder()));
        Assert.True(f.Root.TryReadRequest(request, out var before, out var offer));
        Assert.True(f.Root.Drawing.TryReadState(before, out var previous, out _, out _, out _, out _, out _, out _));
        var otherOffer = f.Root.Drawing.Offer(f.Root.Drawing.Source, new Placeholder(), previous,
            S(D(f.Root.Drawing, f.Root.Drawing.Frame.Right, new Placeholder())));
        var acceptedOther = f.Root.Drawing.Step(before, otherOffer);
        Assert.True(f.Root.Drawing.TryReadAccepted(acceptedOther, out _, out _));
        var failed = f.Root.Acknowledge(request, acceptedOther);
        Assert.False(f.Root.TryReadAccepted(failed, out _)); Assert.Same(before, f.Root.Drawing.State);
        Assert.Same(rootPhase, f.Root.ControllerState); Assert.Same(stepPhase, f.Step.ControllerState);
        Assert.Same(request, Assert.Single(E(f.Root.Pending))); Assert.Empty(E(f.Root.Drawing.Strokes));
        Assert.Empty(E(f.Right.Sequence.ReleasedPackets)); AssertAccounted(f.Root, 1);
        var actual = f.Root.Drawing.Step(before, offer); var retried = f.Root.Acknowledge(request, actual);
        Assert.True(f.Root.TryReadAccepted(retried, out var completions)); Assert.Empty(E(completions));
        Assert.Empty(E(f.Root.Pending)); Assert.Single(E(f.Root.Drawing.Strokes));
        Assert.Single(E(f.Right.Sequence.ReleasedPackets)); AssertAccounted(f.Root, 1);
    }

    [Fact]
    public void StaleOrReplayedAcknowledgmentCannotReplayADrawingOrParentReturn()
    {
        var f = Staircase(); var request = f.Root.Propose(new Placeholder(), S(new Placeholder()));
        Assert.True(f.Root.TryReadRequest(request, out var before, out var offer));
        var receipt = f.Root.Drawing.Step(before, offer); var accepted = f.Root.Acknowledge(request, receipt);
        Assert.True(f.Root.TryReadAccepted(accepted, out _)); var phase = f.Step.ControllerState; var drawing = f.Root.Drawing.State;
        var replay = f.Root.Acknowledge(request, receipt);
        Assert.False(f.Root.TryReadAccepted(replay, out _)); Assert.Same(phase, f.Step.ControllerState);
        Assert.Same(drawing, f.Root.Drawing.State); Assert.Single(E(f.Root.Drawing.Strokes)); AssertAccounted(f.Root, 1);
    }

    [Fact]
    public void ALocallyAcceptedHistoricalDrawingCannotOverwriteTheCurrentActuator()
    {
        var f = Staircase(); var request = f.Root.Propose(new Placeholder(), S(new Placeholder()));
        Assert.True(f.Root.TryReadRequest(request, out var before, out var offer));
        var historical = f.Root.Drawing.Step(before, offer);
        f.Root.Drawing.Apply(new Placeholder(), S(D(f.Root.Drawing, f.Root.Drawing.Frame.Up, new Placeholder())), S());
        var live = f.Root.Drawing.State; var phase = f.Root.ControllerState;
        var rejected = f.Root.Acknowledge(request, historical);
        Assert.False(f.Root.TryReadAccepted(rejected, out _)); Assert.Same(live, f.Root.Drawing.State);
        Assert.Same(phase, f.Root.ControllerState); Assert.Same(request, Assert.Single(E(f.Root.Pending)));
        Assert.Empty(E(f.Right.Sequence.ReleasedPackets)); AssertAccounted(f.Root, 1);
    }

    [Fact]
    public void EmptyInputDoesNotProduceAnActionAndWaitingCallsCannotImportExtraPackets()
    {
        var f = Staircase(); var before = f.Root.ControllerState;
        var empty = f.Root.Propose(new Placeholder(), S()); Assert.False(f.Root.TryReadRequest(empty, out _, out _));
        Assert.Same(before, f.Root.ControllerState); Assert.Empty(E(f.Root.Drawing.Strokes));
        var request = f.Root.Propose(new Placeholder(), S(new Placeholder()));
        var another = f.Root.Propose(new Placeholder(), S(new Placeholder()));
        Assert.False(f.Root.TryReadRequest(another, out _, out _)); Assert.Same(request, Assert.Single(E(f.Root.Pending)));
        AssertAccounted(f.Root, 1);
    }

    [Fact]
    public void AReusedEventOrMaterialPacketCannotMoveToAnotherChildAsFreshParticipation()
    {
        var f = Staircase(); var occurrence = new Placeholder(); var packet = new Placeholder();
        Drive(f.Root, occurrence, packet); var drawing = f.Root.Drawing.State;
        var duplicateEvent = f.Root.Propose(occurrence, S(new Placeholder()));
        var duplicatePacket = f.Root.Propose(new Placeholder(), S(packet));
        Assert.False(f.Root.TryReadRequest(duplicateEvent, out _, out _));
        Assert.False(f.Root.TryReadRequest(duplicatePacket, out _, out _));
        Assert.Same(drawing, f.Root.Drawing.State); Assert.Empty(E(f.Up.Sequence.ImportedPackets)); AssertAccounted(f.Root, 1);
    }

    [Fact]
    public void CompletionReaderRejectsAnInventedCompletionPortAndMissingChildProof()
    {
        var f = Staircase(); Relation accepted = null!;
        for (var i = 0; i < 4; i++) accepted = Drive(f.Root);
        Assert.True(f.Root.TryReadAccepted(accepted, out var completions));
        var completion = Assert.IsType<Relation>(Assert.Single(E(completions)));
        var portAndEvent = Assert.IsType<Relation>(completion.Left);
        var invented = new Relation(new Relation(new Placeholder(), portAndEvent.Right), completion.Right);
        var body = Assert.IsType<Relation>(accepted.Right); var result = Assert.IsType<Relation>(body.Left);
        var forged = new Relation(accepted.Left, new Relation(new Relation(result.Left, S(invented)), body.Right));
        Assert.False(f.Root.TryReadAccepted(forged, out _));
        var originalHeader = Assert.IsType<Relation>(accepted.Left);
        var missingChild = new Relation(new Relation(originalHeader.Left, S()), accepted.Right);
        Assert.False(f.Root.TryReadAccepted(missingChild, out _));
        Assert.True(f.Root.TryReadAccepted(accepted, out _));
    }

    [Fact]
    public void TwoWholeTraversalsReuseControllersAndKeepOneOwnershipAccountPerLeaf()
    {
        var f = Staircase(); var rootSurface = f.Root.Surface; var stepSurface = f.Step.Surface;
        for (var i = 0; i < 8; i++) Drive(f.Root);
        Assert.Same(rootSurface, f.Root.Surface); Assert.Same(stepSurface, f.Step.Surface);
        Assert.Equal(2, E(f.Root.CompletionEvents).Length); Assert.Equal(4, E(f.Step.CompletionEvents).Length);
        Assert.Equal(8, E(f.Root.Drawing.Strokes).Length); Assert.Equal(2, E(f.Root.Account()).Length);
        Assert.True(f.Root.IsAtBoundary); AssertAccounted(f.Root, 8); AssertNoNumbers(f.Root.Capture());
    }

    [Fact]
    public void GroupCannotImplicitlyJoinDifferentActuatorsOrCaptureAnAlreadyRunningChild()
    {
        var a = Staircase(); var b = Staircase();
        Assert.Throws<ArgumentException>(() => new PrimitiveFlowGroup("foreign actuator", a.Right, b.Up));
        Assert.Throws<ArgumentException>(() => new PrimitiveFlowGroup("empty"));
        var pending = a.Right.Propose(new Placeholder(), S(new Placeholder()));
        Assert.True(a.Right.TryReadRequest(pending, out _, out _));
        Assert.Throws<ArgumentException>(() => new PrimitiveFlowGroup("already running", a.Right));
    }

    [Fact]
    public void ParentUsesAnOpaqueChildsAdvertisedAccountWithoutDiscoveringItsPrivateHierarchy()
    {
        var f = Staircase(); var opaque = new OpaqueNode(f.Step);
        var root = new PrimitiveFlowGroup("advertised child only", opaque, opaque);
        Assert.Equal(2, E(root.Account()).Length);
        for (var i = 0; i < 4; i++) Drive(root);
        Assert.Single(E(root.CompletionEvents)); Assert.Equal(2, E(root.Account()).Length); AssertAccounted(root, 4);
    }

    private sealed class OpaqueNode(PrimitiveFlowNode inner) : PrimitiveFlowNode
    {
        public override string Name => "opaque child";
        public override QualitativeDrawingReceiver Drawing => inner.Drawing;
        public override Relation Surface => inner.Surface;
        public override Relation InputPort => inner.InputPort;
        public override Relation CompletionPort => inner.CompletionPort;
        public override Relation Pending => inner.Pending;
        public override Relation Unresolved => inner.Unresolved;
        public override Relation Timeline => inner.Timeline;
        public override Relation CompletionEvents => inner.CompletionEvents;
        public override bool IsAtBoundary => inner.IsAtBoundary;
        public override Relation Propose(IElement occurrence, Relation packets) => inner.Propose(occurrence, packets);
        public override Relation Acknowledge(Relation request, Relation receipt) => inner.Acknowledge(request, receipt);
        public override bool TryReadRequest(Relation request, out Relation before, out Relation offer) => inner.TryReadRequest(request, out before, out offer);
        public override bool TryReadAccepted(Relation acknowledgment, out Relation completions) => inner.TryReadAccepted(acknowledgment, out completions);
        public override Relation Account() => inner.Account();
        public override Relation Capture() => inner.Capture();
    }

    private static (PrimitiveFlowGroup Root, PrimitiveFlowGroup Step, PrimitiveFlowLeaf Right, PrimitiveFlowLeaf Up) Staircase()
    {
        var drawing = new QualitativeDrawingReceiver(new()); var h = new Placeholder(); var v = new Placeholder();
        var right = new PrimitiveFlowLeaf("horizontal", drawing, S(D(drawing, drawing.Frame.Right, h)));
        var up = new PrimitiveFlowLeaf("vertical", drawing, S(D(drawing, drawing.Frame.Up, v)));
        var step = new PrimitiveFlowGroup("step", right, up);
        return (new PrimitiveFlowGroup("two steps", step, step), step, right, up);
    }
    private static Relation D(QualitativeDrawingReceiver drawing, IElement direction, IElement reach) =>
        drawing.Draw(S(QualitativeMotionFrame.Move(direction, reach)));
    private static Relation Drive(PrimitiveFlowNode node) => Drive(node, new Placeholder(), new Placeholder());
    private static Relation Drive(PrimitiveFlowNode node, IElement occurrence, IElement packet)
    {
        var request = node.Propose(occurrence, S(packet));
        Assert.True(node.TryReadRequest(request, out var before, out var offer));
        var acknowledgment = node.Acknowledge(request, node.Drawing.Step(before, offer));
        Assert.True(node.TryReadAccepted(acknowledgment, out _)); return acknowledgment;
    }
    private static void AssertAccounted(PrimitiveFlowGroup group, int expectedImports)
    {
        var imports = 0; var owned = new List<IElement>();
        foreach (var account in E(group.Account()).Cast<Relation>())
        {
            var header = Assert.IsType<Relation>(account.Left); imports += E(Assert.IsType<Relation>(header.Right)).Length;
            var active = Assert.IsType<Relation>(account.Right); var retained = Assert.IsType<Relation>(active.Right);
            var released = Assert.IsType<Relation>(retained.Right);
            Assert.Empty(E(Assert.IsType<Relation>(released.Right)));
            foreach (var scope in new[] { active.Left, retained.Left, released.Left }.Cast<Relation>())
                owned.AddRange(E(scope).Cast<Relation>().Select(row => row.Left));
        }
        Assert.Equal(expectedImports, imports); Assert.Equal(imports, owned.Count);
        Assert.Equal(owned.Count, owned.Distinct(ReferenceEqualityComparer.Instance).Count());
    }
    private static void AssertNoNumbers(IElement root)
    {
        var seen = new HashSet<IElement>(ReferenceEqualityComparer.Instance); var pending = new Stack<IElement>(); pending.Push(root);
        while (pending.TryPop(out var item))
        {
            if (!seen.Add(item)) continue; Assert.False(item is Carrier or CarrierLocation);
            if (item is Relation relation) { pending.Push(relation.Left); pending.Push(relation.Right); }
        }
    }
    private static Relation S(params IElement[] entries) => StructuralConstraint.Closed(entries);
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
}
