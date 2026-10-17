using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeFrieze;

namespace Gojo.Experiments.QualitativeExecution.Tests;

public sealed class WallpaperFlowStudyTests
{
    [Fact]
    public void DefinitionsExposeLiveMachinesAndTwoPeriodPortsWithoutNumericGeometry()
    {
        var study = new WallpaperFlowStudy();
        Assert.All(new[] { study.Rotation, study.Glide, study.Placement }, machine => Assert.Empty(E(machine.Drawing.Strokes)));
        Assert.DoesNotContain(Traverse(study.Surface), item => item is Carrier or CarrierLocation);
        Assert.True(study.Basis.Cell.TryReadPassed(study.Basis.CellRead));
        var ports = E(study.PeriodicPorts).Cast<Relation>().ToArray(); Assert.Equal(2, ports.Length);
        Assert.Same(study.Placement.Muscles[0].Accumulator.InputPort, ports[0].Left);
        Assert.Same(study.Basis.Cell.SeamX, Assert.IsType<Relation>(ports[0].Right).Left);
        Assert.Same(study.Basis.Cell.XWord, Assert.IsType<Relation>(ports[0].Right).Right);
        Assert.Same(study.Basis.Cell.SeamY, Assert.IsType<Relation>(ports[1].Right).Left);
        Assert.Same(study.Basis.Cell.YWord, Assert.IsType<Relation>(ports[1].Right).Right);
        AssertZero(study.Basis.Frame, study.DoubleHalfRead);
        AssertZero(study.Basis.Frame, study.MirrorHalfEquality);
        Assert.Same(study.Basis.Cell.Bottom, study.HalfPeriodSubdivision.Left);
        Assert.True(QualitativePeriodicCell.TryReadPath(study.Basis.Cell.Bottom, out var allSites, out var allMoves));
        Assert.True(QualitativePeriodicCell.TryReadPath(study.HalfPeriodPath, out var firstSites, out var firstMoves));
        Assert.True(QualitativePeriodicCell.TryReadPath(study.SecondHalfPeriodPath, out var lastSites, out var lastMoves));
        Assert.All(E(firstSites).Select((site, i) => (site, i)), pair => Assert.Same(E(allSites)[pair.i], pair.site));
        Assert.All(E(lastSites).Select((site, i) => (site, i)), pair => Assert.Same(E(allSites)[pair.i + 2], pair.site));
        Assert.All(E(firstMoves).Select((move, i) => (move, i)), pair => Assert.Same(E(allMoves)[pair.i], pair.move));
        Assert.All(E(lastMoves).Select((move, i) => (move, i)), pair => Assert.Same(E(allMoves)[pair.i + 2], pair.move));
        Assert.Same(E(firstSites)[^1], E(lastSites)[0]);
    }

    [Fact]
    public void FourReservoirSelectedTurnsGenerateActualHooksAndRestoreOrientation()
    {
        var study = new WallpaperFlowStudy(); var machine = study.Rotation;
        machine.Run(8); var trace = study.Capture(machine);
        Assert.Equal(8, E(trace.Graph.Strokes).Length); Assert.Equal(16, E(trace.DrawingReceipts).Length);
        Assert.Empty(E(trace.Unresolved)); Assert.Same(machine.Drawing.AnchorSite, machine.Drawing.CursorSite);
        Assert.Same(study.Basis.Frame.Origin, machine.Drawing.CursorSite);
        AssertZero(study.Basis.Frame, Position(study, trace, S()));
        Assert.True(study.Basis.DirectionActions.TryReadEquality(Orientation(study, trace, machine.Drawing.IdentityOrientation), out var same)); Assert.True(same);
        var directions = E(trace.Graph.Strokes).Cast<Relation>().Select(stroke => Direction(stroke)).ToArray();
        Assert.Equal(new IElement[] { study.Basis.Frame.Right, study.Basis.Frame.Up, study.Basis.Frame.Up, study.Basis.Frame.Left,
            study.Basis.Frame.Left, study.Basis.Frame.Down, study.Basis.Frame.Down, study.Basis.Frame.Right }, directions);
        AssertActualReceipts(machine, trace); AssertAccounted(machine);
        Assert.DoesNotContain(Traverse(trace.Surface), item => item is Carrier or CarrierLocation);
    }

    [Fact]
    public void TwoGlidesRestoreOrientationAndAdvanceANonzeroActualPeriod()
    {
        var study = new WallpaperFlowStudy(); var machine = study.Glide;
        machine.Run(4); var first = study.Capture(machine);
        AssertZero(study.Basis.Frame, Position(study, first, study.HalfPeriod));
        Assert.True(study.Basis.DirectionActions.TryReadEquality(Orientation(study, first, study.Basis.OtherDirectionMirror), out var reflected)); Assert.True(reflected);
        machine.Run(4); var second = study.Capture(machine);
        Assert.Equal(6, E(second.Graph.Strokes).Length); Assert.Empty(E(second.Unresolved));
        AssertZero(study.Basis.Frame, Position(study, second, study.Basis.Cell.XWord));
        Assert.True(study.Basis.Frame.TryReadResult(Position(study, second, S()), out var nonzero, out _, out var unknown));
        Assert.Equal(4, E(nonzero).Length); Assert.Empty(E(unknown));
        Assert.NotSame(study.Basis.Frame.Origin, machine.Drawing.CursorSite);
        Assert.Same(machine.Drawing.AnchorSite, machine.Drawing.CursorSite);
        Assert.True(study.Basis.DirectionActions.TryReadEquality(Orientation(study, second, machine.Drawing.IdentityOrientation), out var restored)); Assert.True(restored);
        Assert.Same(study.Basis.Frame.Up, Direction((Relation)E(second.Graph.Strokes)[0]));
        Assert.Same(study.Basis.Frame.Down, Direction((Relation)E(second.Graph.Strokes)[3]));
        // The older captured state remains a half-period even after live state advances.
        AssertZero(study.Basis.Frame, Position(study, first, study.HalfPeriod));
        AssertActualReceipts(machine, second); AssertAccounted(machine);
    }

    [Fact]
    public void AMirrorThatReversesTheHalfPeriodCannotUseTheGlideSquareProof()
    {
        var study = new WallpaperFlowStudy();
        var wrong = study.Basis.AxisTransform(study.Basis.DirectionMirror);
        Assert.True(wrong.TryReadComplete(wrong.Apply(study.HalfPeriod), out var reversed));
        var mismatch = study.Basis.Frame.Reduce(reversed, study.Basis.Inverse(study.HalfPeriod));
        Assert.True(study.Basis.Frame.TryReadResult(mismatch, out var remaining, out _, out _)); Assert.NotEmpty(E(remaining));
        // Translating by H and then by its mirror image cancels instead of
        // advancing a period: an involution alone is not a glide certificate.
        AssertZero(study.Basis.Frame, study.Basis.Frame.Reduce(study.HalfPeriod, reversed));
    }

    [Fact]
    public void NoInputLeavesControllersOrientationAndPlacementUnchanged()
    {
        var study = new WallpaperFlowStudy();
        foreach (var machine in new[] { study.Rotation, study.Glide, study.Placement })
        {
            var states = machine.Muscles.Select(muscle => muscle.ControllerState).ToArray();
            var site = machine.Drawing.CursorSite; var orientation = machine.Drawing.Orientation;
            for (var step = 0; step < 5; step++) machine.Drive(new Placeholder(), S());
            Assert.Empty(E(machine.Drawing.Strokes)); Assert.Same(site, machine.Drawing.CursorSite); Assert.Same(orientation, machine.Drawing.Orientation);
            Assert.All(machine.Muscles.Select((muscle, i) => (muscle, i)), pair => Assert.Same(states[pair.i], pair.muscle.ControllerState));
            Assert.Empty(E(machine.ImportedPackets)); AssertAccounted(machine);
        }
    }

    [Fact]
    public void AnOppositeMoveReturnsCoordinatesWithoutJoiningTheActualSites()
    {
        var study = new WallpaperFlowStudy(); var drawing = study.Rotation.Drawing;
        var origin = drawing.CursorSite; var reach = study.Basis.Reach;
        Assert.True(drawing.TryReadAccepted(drawing.Apply(new Placeholder(), S(drawing.Draw(S(QualitativeMotionFrame.Move(study.Basis.Frame.Right, reach)))), S()), out _, out _));
        Assert.True(drawing.TryReadAccepted(drawing.Apply(new Placeholder(), S(drawing.Draw(S(QualitativeMotionFrame.Move(study.Basis.Frame.Left, reach)))), S()), out _, out _));
        Assert.Empty(E(drawing.Position)); Assert.NotSame(origin, drawing.CursorSite); Assert.Same(origin, drawing.AnchorSite);
        Assert.True(drawing.TryReadAccepted(drawing.Apply(new Placeholder(), S(drawing.Reset()), S()), out _, out _));
        Assert.Same(origin, drawing.CursorSite); Assert.Equal(2, E(drawing.Strokes).Length);
    }

    [Fact]
    public void IndependentPeriodPortsCanBeDrivenSeparatelyAndRetainTheirSeamBasis()
    {
        var study = new WallpaperFlowStudy(); var machine = study.Placement; var ports = E(machine.ExternalPorts);
        machine.Drive(new Placeholder(), S(new Relation(ports[0], S(new Placeholder()))));
        var x = study.Capture(machine); AssertZero(study.Basis.Frame, Position(study, x, study.Basis.Cell.XWord));
        var yController = machine.Muscles[1].ControllerState;
        machine.Drive(new Placeholder(), S()); Assert.Same(yController, machine.Muscles[1].ControllerState);
        machine.Drive(new Placeholder(), S(new Relation(ports[1], S(new Placeholder()))));
        var xy = study.Capture(machine); var expected = S([.. E(study.Basis.Cell.XWord), .. E(study.Basis.Cell.YWord)]);
        AssertZero(study.Basis.Frame, Position(study, xy, expected));
        Assert.Empty(E(xy.Graph.Strokes)); Assert.NotSame(study.Basis.Frame.Origin, machine.Drawing.CursorSite);
        Assert.Equal(4, E(xy.DrawingReceipts).Length); AssertActualReceipts(machine, xy); AssertAccounted(machine);
        var commutation = study.Basis.Frame.Reduce(expected,
            study.Basis.Inverse(S([.. E(study.Basis.Cell.YWord), .. E(study.Basis.Cell.XWord)])));
        AssertZero(study.Basis.Frame, commutation);
        var reverse = study.CreatePlacement(); var reversePorts = E(reverse.ExternalPorts);
        reverse.Drive(new Placeholder(), S(new Relation(reversePorts[1], S(new Placeholder()))));
        var y = study.Capture(reverse); AssertZero(study.Basis.Frame, Position(study, y, study.Basis.Cell.YWord));
        reverse.Drive(new Placeholder(), S(new Relation(reversePorts[0], S(new Placeholder()))));
        var yx = study.Capture(reverse); AssertZero(study.Basis.Frame, Position(study, yx, expected));
        AssertZero(study.Basis.Frame, study.Basis.Frame.Reduce(machine.Drawing.Position, study.Basis.Inverse(reverse.Drawing.Position)));
        Assert.NotSame(machine.Drawing.CursorSite, reverse.Drawing.CursorSite);
        Assert.DoesNotContain(E(reverse.Drawing.History), occurrence => E(machine.Drawing.History).Any(other => ReferenceEquals(other, occurrence)));
        Assert.Equal(4, E(yx.DrawingReceipts).Length); AssertActualReceipts(reverse, yx); AssertAccounted(reverse);
        // This equality of formal placements does not invent a runtime site seam.
        Assert.NotSame(study.Basis.Cell.SeamX, study.Basis.Cell.SeamY);
    }

    [Fact]
    public void ConcurrentContributionsMakeOneChordWhileOrderedPhasesMakeTwoStrokes()
    {
        var frame = new QualitativeMotionFrame(); var reach = new Placeholder();
        var parallel = new QualitativeDrawingReceiver(frame);
        var h = PatternMachineLibrary.Muscle("horizontal", 1, S(parallel.Draw(S(QualitativeMotionFrame.Move(frame.Right, reach)))));
        var v = PatternMachineLibrary.Muscle("vertical", 1, S(parallel.Draw(S(QualitativeMotionFrame.Move(frame.Up, reach)))));
        var together = new PatternMachine("concurrent", parallel, [h, v], S(h.Accumulator.InputPort, v.Accumulator.InputPort), S());
        together.Run(1); Assert.Equal(2, E(Word((Relation)Assert.Single(E(parallel.Strokes)))).Length);
        var sequential = new QualitativeDrawingReceiver(frame);
        var motor = PatternMachineLibrary.Muscle("ordered", 1,
            S(sequential.Draw(S(QualitativeMotionFrame.Move(frame.Right, reach)))),
            S(sequential.Draw(S(QualitativeMotionFrame.Move(frame.Up, reach)))));
        var apart = new PatternMachine("ordered", sequential, [motor], S(motor.Accumulator.InputPort), S());
        apart.Run(2); Assert.Equal(2, E(sequential.Strokes).Length);
        Assert.All(E(sequential.Strokes).Cast<Relation>(), stroke => Assert.Single(E(Word(stroke))));
        Assert.Equal(2, E(parallel.Sites).Length); Assert.Equal(3, E(sequential.Sites).Length);
        AssertAccounted(together); AssertAccounted(apart);
    }

    [Fact]
    public void TurnAndSampleInOneDriveFollowTheDeclaredPostSampleControlSchedule()
    {
        var study = new WallpaperFlowStudy(); var drawing = new QualitativeDrawingReceiver(study.Basis.Frame);
        var turn = PatternMachineLibrary.Muscle("turn", 1, S(drawing.DirectionAction(study.Basis.DirectionTurn)));
        var draw = PatternMachineLibrary.Muscle("draw", 1, S(drawing.Draw(S(QualitativeMotionFrame.Move(study.Basis.Frame.Right, study.Basis.Reach)))));
        var machine = new PatternMachine("sample then control", drawing, [turn, draw], S(turn.Accumulator.InputPort, draw.Accumulator.InputPort), S());
        machine.Run(2); var strokes = E(drawing.Strokes).Cast<Relation>().ToArray();
        Assert.Same(study.Basis.Frame.Right, Direction(strokes[0])); Assert.Same(study.Basis.Frame.Up, Direction(strokes[1]));
        Assert.Empty(E(machine.Unresolved)); AssertAccounted(machine);
    }

    private static void AssertActualReceipts(PatternMachine machine, WallpaperFlowStudy.Trace trace)
    {
        foreach (var receipt in E(trace.DrawingReceipts).Cast<Relation>())
            Assert.True(machine.Drawing.TryReadAccepted(receipt, out _, out _));
        foreach (var step in E(machine.Timeline).Cast<Relation>())
        {
            var ticks = Assert.IsType<Relation>(Assert.IsType<Relation>(step.Right).Left);
            foreach (var tick in E(ticks).Cast<Relation>())
            {
                var muscle = Assert.Single(machine.Muscles, item => ReferenceEquals(Assert.IsType<Relation>(tick.Left).Left, item.Surface));
                Assert.True(muscle.TryReadTick(tick, out _, out var drained, out var question, out _));
                if (E(drained).Length != 0) Assert.True(QualitativeQuestion.TryReadAnswer(question, out _));
            }
        }
    }
    private static Relation Word(Relation stroke)
    { Assert.True(QualitativeStrokeGraph.TryReadStroke(stroke, out _, out _, out _, out var word)); return word; }
    private static IElement Direction(Relation stroke) => Assert.IsType<Relation>(Assert.Single(E(Word(stroke)))).Left;
    private static Relation Position(WallpaperFlowStudy study, WallpaperFlowStudy.Trace trace, Relation expected)
    {
        var evidence = study.ReadPositionEvidence(trace, expected);
        var origin = Assert.IsType<Relation>(evidence.Left); Assert.Same(trace.Surface, origin.Left);
        var selected = Assert.IsType<Relation>(origin.Right); Assert.Same(trace.DrawingState, selected.Left);
        Assert.True(trace.Machine.Drawing.TryReadState(trace.DrawingState, out _, out var cursor, out _, out _, out _, out _, out _));
        Assert.Same(cursor, selected.Right);
        var comparison = Assert.IsType<Relation>(evidence.Right); Assert.Same(expected, comparison.Left);
        return Assert.IsType<Relation>(comparison.Right);
    }
    private static Relation Orientation(WallpaperFlowStudy study, WallpaperFlowStudy.Trace trace, Relation expected)
    {
        var evidence = study.ReadOrientationEvidence(trace, expected);
        var origin = Assert.IsType<Relation>(evidence.Left); Assert.Same(trace.Surface, origin.Left);
        var selected = Assert.IsType<Relation>(origin.Right); Assert.Same(trace.DrawingState, selected.Left);
        Assert.True(trace.Machine.Drawing.TryReadState(trace.DrawingState, out _, out _, out _, out var orientation, out _, out _, out _));
        Assert.Same(orientation, selected.Right);
        var comparison = Assert.IsType<Relation>(evidence.Right); Assert.Same(expected, comparison.Left);
        return Assert.IsType<Relation>(comparison.Right);
    }
    private static void AssertZero(QualitativeMotionFrame frame, Relation read)
    { Assert.True(frame.TryReadResult(read, out var rest, out _, out var pending)); Assert.Empty(E(rest)); Assert.Empty(E(pending)); }
    private static void AssertAccounted(PatternMachine machine)
    {
        var active = Assert.IsType<Relation>(machine.Account().Right);
        var retained = Assert.IsType<Relation>(active.Right); var released = Assert.IsType<Relation>(retained.Right);
        Assert.Empty(E(Assert.IsType<Relation>(released.Right)));
        Assert.Equal(E(machine.ImportedPackets).Length, E(Assert.IsType<Relation>(active.Left)).Length + E(Assert.IsType<Relation>(retained.Left)).Length + E(Assert.IsType<Relation>(released.Left)).Length);
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
