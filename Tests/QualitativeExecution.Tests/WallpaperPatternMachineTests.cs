using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeFrieze;

namespace Gojo.Experiments.QualitativeExecution.Tests;

public sealed class WallpaperPatternMachineTests
{
    [Fact]
    public void AcceptedWholeMotifOpensPlacementWithoutPerformingItsFirstAction()
    {
        var fixture = Create(); var machine = fixture.Machine;
        machine.Pulse(); Assert.Empty(E(machine.CompletedMotifs));
        var request = machine.Propose(new Placeholder(), S(new Placeholder()));
        Assert.True(machine.Motif.TryReadRequest(request, out _, out var before, out var offer));
        var receipt = machine.Drawing.Step(before, offer);
        Assert.True(machine.Drawing.TryReadAccepted(receipt, out _, out _));
        Assert.Empty(E(machine.CompletedMotifs)); Assert.Same(machine.Motif.InputPort, machine.ActiveInput);
        Assert.Single(E(machine.Drawing.Strokes));
        machine.Acknowledge(request, receipt);
        Assert.Single(E(machine.CompletedMotifs)); Assert.Single(E(machine.Dispatches));
        Assert.Same(machine.Placements[0].InputPort, machine.ActiveInput); Assert.Same(fixture.Cells[0], machine.CurrentCell);
        Assert.Equal(2, E(machine.Drawing.Strokes).Length); Assert.Equal(2, E(machine.Drawing.Position).Length);
        Assert.NotSame(machine.Drawing.AnchorSite, machine.Drawing.CursorSite);
        Assert.Empty(E(machine.Placements[0].Timeline)); Assert.Empty(E(machine.Placements[0].ImportedPackets));
        Assert.Empty(E(machine.AcceptedPassages)); AssertAccounted(machine);
    }

    [Fact]
    public void EntirePlacementMustBeAcknowledgedBeforeTheNextMotifCanStart()
    {
        var fixture = Create(); var machine = fixture.Machine;
        machine.Pulse(); machine.Pulse(); var motifPhase = machine.Motif.ControllerState;
        machine.Pulse(); // reset only
        Assert.Same(machine.Drawing.AnchorSite, machine.Drawing.CursorSite);
        Assert.Empty(E(machine.Drawing.Position)); Assert.Same(machine.Placements[0].InputPort, machine.ActiveInput);
        Assert.Same(motifPhase, machine.Motif.ControllerState); Assert.Empty(E(machine.AcceptedPassages));
        machine.Pulse(); // jump only
        Assert.Equal(3, E(machine.Drawing.Position).Length); Assert.Equal(2, E(machine.Drawing.Strokes).Length);
        Assert.NotSame(machine.Drawing.AnchorSite, machine.Drawing.CursorSite);
        Assert.Same(motifPhase, machine.Motif.ControllerState); Assert.Same(machine.Placements[0].InputPort, machine.ActiveInput);
        machine.Pulse(); // anchor commits the complete passage
        Assert.Single(E(machine.AcceptedPassages)); Assert.Same(machine.Motif.InputPort, machine.ActiveInput);
        Assert.Same(fixture.Cells[1], machine.CurrentCell); Assert.Same(machine.Drawing.AnchorSite, machine.Drawing.CursorSite);
        Assert.Same(motifPhase, machine.Motif.ControllerState); Assert.Equal(2, E(machine.Drawing.Strokes).Length);
        machine.Pulse(); Assert.Equal(3, E(machine.Drawing.Strokes).Length);
        Assert.NotSame(motifPhase, machine.Motif.ControllerState); AssertAccounted(machine);
    }

    [Fact]
    public void AcceptedResetThenRejectedJumpHoldsThePassageAndExactRetryContinuesOnce()
    {
        var fixture = Create(); var machine = fixture.Machine;
        machine.Pulse(); machine.Pulse(); machine.Pulse(); // motif then reset
        var placement = machine.Placements[0]; var phase = placement.ControllerState;
        var request = machine.Propose(new Placeholder(), S(new Placeholder()));
        Assert.True(placement.TryReadRequest(request, out var action, out var before, out var offer));
        Assert.True(machine.Drawing.TryReadJump(action, out _));
        var rejected = machine.Drawing.Step(before, machine.Drawing.Offer(machine.Drawing.Source,
            new Placeholder(), new Placeholder(), S(action), request));
        Assert.False(machine.Drawing.TryReadAccepted(rejected, out _, out _));
        machine.Acknowledge(request, rejected);
        Assert.Same(phase, placement.ControllerState); Assert.Same(before, machine.Drawing.State);
        Assert.Same(request, Assert.Single(E(placement.Pending))); Assert.Empty(E(machine.AcceptedPassages));
        Assert.Same(placement.InputPort, machine.ActiveInput); Assert.Equal(2, E(machine.Drawing.Strokes).Length);
        Assert.Single(E(placement.ReleasedPackets)); // accepted reset remains paid once
        var actual = machine.Drawing.Step(before, offer); machine.Acknowledge(request, actual);
        Assert.Empty(E(placement.Pending)); Assert.Equal(2, E(placement.ReleasedPackets).Length);
        Assert.Equal(3, E(machine.Drawing.Position).Length); Assert.Empty(E(machine.AcceptedPassages));
        machine.Pulse(); Assert.Single(E(machine.AcceptedPassages)); Assert.Same(machine.Motif.InputPort, machine.ActiveInput);
        Assert.Equal(3, E(placement.ReleasedPackets).Length); Assert.Equal(2, E(machine.Drawing.Strokes).Length);
        AssertAccounted(machine);
    }

    [Fact]
    public void WrongAcceptedBranchAndReplayedOrMisroutedReceiptCannotAdvanceControl()
    {
        var machine = Create().Machine;
        var request = machine.Propose(new Placeholder(), S(new Placeholder()));
        Assert.True(machine.Motif.TryReadRequest(request, out var action, out var before, out var offer));
        Assert.True(machine.Drawing.TryReadState(before, out var previous, out _, out _, out _, out _, out _, out _));
        var otherBranch = machine.Drawing.Step(before, machine.Drawing.Offer(machine.Drawing.Source,
            new Placeholder(), previous, S(action), request));
        Assert.True(machine.Drawing.TryReadAccepted(otherBranch, out _, out _));
        var state = machine.State; var phase = machine.Motif.ControllerState;
        machine.Acknowledge(request, otherBranch);
        Assert.Same(state, machine.State); Assert.Same(phase, machine.Motif.ControllerState); Assert.Same(before, machine.Drawing.State);
        var correct = machine.Drawing.Step(before, offer); machine.Acknowledge(request, correct);
        var acceptedDrawing = machine.Drawing.State; var acceptedPhase = machine.Motif.ControllerState;
        machine.Acknowledge(request, correct);
        Assert.Same(acceptedDrawing, machine.Drawing.State); Assert.Same(acceptedPhase, machine.Motif.ControllerState);
        Assert.Single(E(machine.Drawing.Strokes)); Assert.Single(E(machine.DrawingReceipts));
        machine.Pulse(); Assert.Same(machine.Placements[0].InputPort, machine.ActiveInput);
        var placementPhase = machine.Placements[0].ControllerState; var control = machine.State;
        machine.Acknowledge(request, correct); // old motif proof offered to active placement
        Assert.Same(control, machine.State); Assert.Same(placementPhase, machine.Placements[0].ControllerState);
        Assert.Single(E(machine.CompletedMotifs)); Assert.Empty(E(machine.AcceptedPassages)); AssertAccounted(machine);
    }

    [Fact]
    public void ALocallyAcceptedHistoricalReceiptCannotOverwriteTheLiveReceiverHead()
    {
        var machine = Create().Machine;
        var request = machine.Propose(new Placeholder(), S(new Placeholder()));
        Assert.True(machine.Motif.TryReadRequest(request, out var action, out var before, out var offer));
        var historical = machine.Drawing.Step(before, offer);
        Assert.True(machine.Drawing.TryReadAccepted(historical, out _, out _));
        machine.Drawing.Apply(new Placeholder(), S(action), new Placeholder());
        var live = machine.Drawing.State; var phase = machine.Motif.ControllerState; var control = machine.State;
        machine.Acknowledge(request, historical);
        Assert.Same(live, machine.Drawing.State); Assert.Same(phase, machine.Motif.ControllerState); Assert.Same(control, machine.State);
        Assert.Same(request, Assert.Single(E(machine.Pending))); Assert.Empty(E(machine.DrawingReceipts));
        Assert.Empty(E(machine.CompletedMotifs)); Assert.Empty(E(machine.Motif.ReleasedPackets)); AssertAccounted(machine);
    }

    [Fact]
    public void EmptyInputCannotProduceARequestMoveOrDispatch()
    {
        var machine = Create().Machine; var state = machine.State; var drawing = machine.Drawing.State; var phase = machine.Motif.ControllerState;
        for (var step = 0; step < 4; step++) machine.Drive(new Placeholder(), S());
        Assert.Same(state, machine.State); Assert.Same(drawing, machine.Drawing.State); Assert.Same(phase, machine.Motif.ControllerState);
        Assert.Empty(E(machine.Pending)); Assert.Empty(E(machine.DrawingReceipts)); Assert.Empty(E(machine.Dispatches));
        Assert.Empty(E(machine.CompletedMotifs)); Assert.Empty(E(machine.Drawing.Strokes)); AssertAccounted(machine);
        var published = Traverse(machine.Run(0)).ToArray();
        Assert.Contains(published, item => ReferenceEquals(item, machine.Motif.InputHistory));
        Assert.Contains(published, item => ReferenceEquals(item, machine.Motif.ReservoirState));
    }

    [Fact]
    public void NativeFiniteRouteCompletesAndTerminalInputIsReturnedWithoutImport()
    {
        var fixture = Create(2); var machine = fixture.Machine;
        var result = machine.Run(32);
        Assert.True(machine.IsComplete); Assert.Same(machine.TerminalPort, machine.ActiveInput); Assert.Same(fixture.Cells[^1], machine.CurrentCell);
        Assert.Equal(3, E(machine.CompletedMotifs).Length); Assert.Equal(2, E(machine.AcceptedPassages).Length);
        Assert.Equal(6, E(machine.Drawing.Strokes).Length); Assert.Equal(12, E(machine.DrawingReceipts).Length);
        Assert.Equal(5, E(machine.Dispatches).Length); Assert.Empty(E(machine.Pending)); Assert.Empty(E(machine.Unresolved));
        foreach (var receipt in E(machine.DrawingReceipts).Cast<Relation>()) Assert.True(machine.Drawing.TryReadAccepted(receipt, out _, out _));
        Assert.DoesNotContain(Traverse(new Relation(machine.Surface, result)), element => element is Carrier or CarrierLocation);
        var imported = machine.Components.Sum(sequence => E(sequence.ImportedPackets).Length);
        var drawing = machine.Drawing.State; var control = machine.State; var packet = new Placeholder();
        machine.Drive(new Placeholder(), S(packet));
        Assert.Same(drawing, machine.Drawing.State); Assert.Same(control, machine.State);
        Assert.Equal(imported, machine.Components.Sum(sequence => E(sequence.ImportedPackets).Length));
        var returned = Assert.IsType<Relation>(Assert.Single(E(machine.ReturnedInputs)));
        Assert.Same(machine.TerminalPort, Assert.IsType<Relation>(returned.Left).Left);
        Assert.Same(packet, Assert.Single(E(Assert.IsType<Relation>(returned.Right)))); AssertAccounted(machine);
    }

    [Fact]
    public void ReusingAPacketAcrossMotifAndPlacementCannotImportItTwice()
    {
        var machine = Create().Machine; var packet = new Placeholder();
        machine.Drive(new Placeholder(), S(packet)); machine.Pulse();
        Assert.Same(machine.Placements[0].InputPort, machine.ActiveInput);
        var before = machine.Drawing.State; var controller = machine.Placements[0].ControllerState; var state = machine.State;
        machine.Drive(new Placeholder(), S(packet));
        Assert.Same(before, machine.Drawing.State); Assert.Same(controller, machine.Placements[0].ControllerState); Assert.Same(state, machine.State);
        Assert.Empty(E(machine.Placements[0].ImportedPackets));
        Assert.Equal(1, machine.Components.SelectMany(sequence => E(sequence.ImportedPackets)).Count(item => ReferenceEquals(item, packet)));
        AssertAccounted(machine);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void RowChangeUsesWorldPitchAfterTheMotifsRotationsAndReflections(int family)
    {
        var study = new WallpaperMachineStudy(); var example = study.Cases[family]; var machine = example.Machine;
        for (var pulse = 0; pulse < 512 && E(machine.AcceptedPassages).Length < 4; pulse++) machine.Pulse();
        Assert.Equal(4, E(machine.CompletedMotifs).Length); Assert.Equal(4, E(machine.AcceptedPassages).Length);
        Assert.False(machine.IsComplete); Assert.Same(machine.Motif.InputPort, machine.ActiveInput);
        var secondRow = Assert.IsType<Relation>(E(example.Cells)[1]);
        Assert.Same(E(secondRow)[0], machine.CurrentCell); Assert.Same(machine.Drawing.AnchorSite, machine.Drawing.CursorSite);
        var basis = study.Basis.Basis;
        var position = basis.Frame.Reduce(machine.Drawing.Position, basis.Inverse(example.RowPitch));
        Assert.True(basis.Frame.TryReadResult(position, out var residual, out _, out var pending));
        Assert.Empty(E(residual)); Assert.Empty(E(pending)); Assert.Empty(E(machine.Unresolved));
        var planned = Assert.Single(E(example.OrientationPlan).Cast<Relation>(), row => ReferenceEquals(row.Left, machine.CurrentCell));
        var read = Assert.IsType<Relation>(Assert.IsType<Relation>(planned.Right).Right);
        Assert.True(basis.DirectionActions.TryReadResult(read, out var expected, out var blocked, out var unknown, out _));
        Assert.Empty(E(blocked)); Assert.Empty(E(unknown));
        Assert.True(basis.DirectionActions.TryReadEquality(basis.DirectionActions.Compare(S(machine.Drawing.Orientation), S(expected)), out var same)); Assert.True(same);
        foreach (var receipt in E(machine.DrawingReceipts).Cast<Relation>()) Assert.True(machine.Drawing.TryReadAccepted(receipt, out _, out _));
        AssertAccounted(machine);
    }

    private static (WallpaperPatternMachine Machine, IElement[] Cells) Create(int passages = 1)
    {
        var frame = new QualitativeMotionFrame(); var drawing = new QualitativeDrawingReceiver(frame, initialSite: frame.Origin);
        var reach = new Placeholder(); var cells = Enumerable.Range(0, passages + 1).Select(_ => (IElement)new Placeholder()).ToArray();
        Relation W(IElement direction) => S(QualitativeMotionFrame.Move(direction, reach));
        var motif = S(drawing.Draw(W(frame.Right)), drawing.Draw(W(frame.Up)));
        var jump = S([.. E(W(frame.Right)), .. E(W(frame.Right)), .. E(W(frame.Right))]);
        var edges = S(Enumerable.Range(0, passages).Select(index => (IElement)new Relation(new Relation(cells[index], cells[index + 1]),
            S(drawing.Reset(), drawing.Jump(jump), drawing.Anchor()))).ToArray());
        return (new WallpaperPatternMachine("acknowledged bounded field", drawing, motif, edges, cells[0], frame.Surface), cells);
    }

    private static void AssertAccounted(WallpaperPatternMachine machine)
    {
        var all = new List<IElement>(); var ownership = new List<IElement>();
        foreach (var sequence in machine.Components)
        {
            var read = sequence.Account(); var active = Assert.IsType<Relation>(read.Right);
            var retained = Assert.IsType<Relation>(active.Right); var released = Assert.IsType<Relation>(retained.Right);
            Assert.Empty(E(Assert.IsType<Relation>(released.Right)));
            var locations = E(Assert.IsType<Relation>(active.Left)).Concat(E(Assert.IsType<Relation>(retained.Left)))
                .Concat(E(Assert.IsType<Relation>(released.Left))).Cast<Relation>().ToArray();
            Assert.Equal(E(sequence.ImportedPackets).Length, locations.Length);
            all.AddRange(E(sequence.ImportedPackets)); ownership.AddRange(locations.Select(row => row.Left));
        }
        foreach (var packet in all)
        { Assert.Equal(1, all.Count(item => ReferenceEquals(item, packet))); Assert.Equal(1, ownership.Count(item => ReferenceEquals(item, packet))); }
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
