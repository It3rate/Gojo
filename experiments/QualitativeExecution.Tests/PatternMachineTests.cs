using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeFrieze;

namespace Gojo.Experiments.QualitativeExecution.Tests;

public sealed class PatternMachineTests
{
    [Fact]
    public void DefinitionsHaveNoOutputGeometryAndRunsProduceCausalStrokesWithoutNumbers()
    {
        foreach (var machine in PatternMachineLibrary.Create())
        {
            Assert.Empty(E(machine.Drawing.Strokes)); machine.Run(32);
            Assert.NotEmpty(E(machine.Drawing.Strokes)); Assert.Empty(E(machine.Unresolved));
            Assert.DoesNotContain(Traverse(new Relation(machine.Surface, machine.Run(0))), item => item is Carrier or CarrierLocation);
            foreach (var stroke in E(machine.Drawing.Strokes).Cast<Relation>())
            {
                Assert.True(QualitativeStrokeGraph.TryReadStroke(stroke, out var cause, out _, out _, out _));
                var offer = Assert.IsType<Relation>(cause);
                var effect = Assert.IsType<Relation>(Assert.IsType<Relation>(offer.Right).Right);
                var contributions = Assert.IsType<Relation>(effect.Right); Assert.NotEmpty(E(contributions));
                foreach (var contribution in E(contributions).Cast<Relation>())
                {
                    var tick = Assert.IsType<Relation>(contribution.Left);
                    var muscle = Assert.Single(machine.Muscles, item => ReferenceEquals(Assert.IsType<Relation>(tick.Left).Left, item.Surface));
                    Assert.True(muscle.TryReadTick(tick, out _, out var drained, out var question, out _));
                    Assert.NotEmpty(E(drained)); Assert.True(QualitativeQuestion.TryReadAnswer(question, out _));
                }
            }
            AssertAccounted(machine);
        }
    }

    [Fact]
    public void WithheldInputDoesNotAdvanceAControllerOrInventAMove()
    {
        var machine = PatternMachineLibrary.Key(); var controller = machine.Muscles[0].ControllerState;
        for (var i = 0; i < 8; i++) machine.Drive(new Placeholder(), S());
        Assert.Empty(E(machine.Drawing.Strokes)); Assert.Same(controller, machine.Muscles[0].ControllerState);
        Assert.Empty(E(machine.ImportedPackets)); AssertAccounted(machine);
    }

    [Fact]
    public void ThresholdChangesTheFutureWithoutChangingTheHeadingProgram()
    {
        var slow = PatternMachineLibrary.Chevron(2); var fast = PatternMachineLibrary.Chevron(1);
        slow.Run(8); fast.Run(8);
        Assert.Equal(8, E(slow.Drawing.Strokes).Length); Assert.Equal(8, E(fast.Drawing.Strokes).Length);
        Assert.Equal(4, E(slow.Drawing.Strokes).Cast<Relation>().Count(stroke => Word(stroke).Length == 2));
        Assert.Equal(8, E(fast.Drawing.Strokes).Cast<Relation>().Count(stroke => Word(stroke).Length == 2));
        AssertAccounted(slow); AssertAccounted(fast);
    }

    [Fact]
    public void ReadinessQuestionReleasesPacketsAndAdvancesOnlyAtTheBoundary()
    {
        var machine = PatternMachineLibrary.Key(); var muscle = machine.Muscles[0]; var before = muscle.ControllerState;
        var first = muscle.Tick(S(new Placeholder()), new Placeholder());
        Assert.True(muscle.TryReadTick(first, out var quiet, out var absent, out _, out _));
        Assert.Empty(E(quiet)); Assert.Empty(E(absent)); Assert.Same(before, muscle.ControllerState);
        Assert.True(muscle.Accumulator.TryReadState(out _, out var stored, out _, out _)); Assert.Single(E(stored));
        var second = muscle.Tick(S(new Placeholder()), new Placeholder());
        Assert.True(muscle.TryReadTick(second, out var actions, out var released, out var question, out _));
        Assert.Single(E(actions)); Assert.Equal(2, E(released).Length); Assert.NotSame(before, muscle.ControllerState);
        Assert.True(QualitativeQuestion.TryReadAnswer(question, out var answer)); Assert.Same(actions, answer);
    }

    [Fact]
    public void CascadedPacketFlowHasOneOwnerAndDownstreamOverflowStaysVisible()
    {
        var frame = new QualitativeMotionFrame(); var drawing = new QualitativeDrawingReceiver(frame);
        var source = PatternMachineLibrary.Muscle("source", 2, S(drawing.Draw(S(QualitativeMotionFrame.Move(frame.Right, new Placeholder())))));
        var target = PatternMachineLibrary.Muscle("target", 1, S(drawing.Mirror(drawing.VerticalMirror)));
        var machine = new PatternMachine("overflow", drawing, [source, target], S(source.Accumulator.InputPort), S(new Relation(source.OutputPort, target.Accumulator.InputPort)));
        machine.Run(2);
        Assert.Single(E(machine.RetainedPackets));
        var retained = Assert.IsType<Relation>(Assert.IsType<Relation>(Assert.Single(E(machine.RetainedPackets))).Right);
        Assert.Single(E(Assert.IsType<Relation>(retained.Right))); AssertAccounted(machine);
    }

    [Fact]
    public void ClonedExternalPacketAndReplayedDriveCannotMutateTheMachine()
    {
        var machine = PatternMachineLibrary.Chevron(); var ports = E(machine.ExternalPorts); var packet = new Placeholder(); var occurrence = new Placeholder();
        machine.Drive(occurrence, S(new Relation(ports[0], S(packet))));
        var strokes = machine.Drawing.Strokes; var imported = machine.ImportedPackets;
        machine.Drive(occurrence, S(new Relation(ports[0], S(new Placeholder()))));
        machine.Drive(new Placeholder(), S(new Relation(ports[1], S(packet))));
        var shared = new Placeholder(); machine.Drive(new Placeholder(), S(new Relation(ports[0], S(shared)), new Relation(ports[1], S(shared))));
        Assert.Same(strokes, machine.Drawing.Strokes); Assert.Same(imported, machine.ImportedPackets);
        Assert.Equal(3, E(machine.Unresolved).Length); AssertAccounted(machine);
    }

    [Fact]
    public void MaterialFanOutIsRejectedAtConstruction()
    {
        var frame = new QualitativeMotionFrame(); var drawing = new QualitativeDrawingReceiver(frame);
        var a = PatternMachineLibrary.Muscle("a", 1, S()); var b = PatternMachineLibrary.Muscle("b", 1, S()); var c = PatternMachineLibrary.Muscle("c", 1, S());
        Assert.Throws<ArgumentException>(() => new PatternMachine("fork", drawing, [a, b, c], S(a.Accumulator.InputPort),
            S(new Relation(a.OutputPort, b.Accumulator.InputPort), new Relation(a.OutputPort, c.Accumulator.InputPort))));
    }

    [Fact]
    public void RepeatedActualBundleIsAnOrderedProgramButOneOutcomeDomainMember()
    {
        var drawing = new QualitativeDrawingReceiver(new QualitativeMotionFrame());
        var bundle = S(drawing.Draw(S(QualitativeMotionFrame.Move(drawing.Frame.Right, new Placeholder()))));
        var muscle = PatternMachineLibrary.Muscle("reused", 1, bundle, bundle, bundle);
        Assert.Equal(3, E(muscle.ActionBundles).Length);
        Assert.True(FiniteLaw.TryRead(muscle.CommandLaw, out _, out _, out var domain, out _)); Assert.Equal(2, E(domain).Length);
        var machine = new PatternMachine("reuse", drawing, [muscle], S(muscle.Accumulator.InputPort), S());
        machine.Run(6); Assert.Equal(6, E(drawing.Strokes).Length); AssertAccounted(machine);
        Assert.Throws<ArgumentException>(() => new PatternMachine("duplicate port", drawing, [muscle], S(muscle.Accumulator.InputPort, muscle.Accumulator.InputPort), S()));
    }

    [Fact]
    public void OpenChordCannotBecomeClosedDuringSimultaneousAggregation()
    {
        var drawing = new QualitativeDrawingReceiver(new QualitativeMotionFrame());
        var word = StructuralConstraint.Open(QualitativeMotionFrame.Move(drawing.Frame.Right, new Placeholder()));
        var muscle = PatternMachineLibrary.Muscle("unknown move", 1, S(drawing.Draw(word)));
        var machine = new PatternMachine("pending actuator", drawing, [muscle], S(muscle.Accumulator.InputPort), S());
        machine.Run(1); Assert.Empty(E(drawing.Strokes)); Assert.Single(E(machine.Unresolved)); AssertAccounted(machine);
    }

    [Fact]
    public void AnchorResetAndJumpMakeNoHiddenConnectorStroke()
    {
        var machine = PatternMachineLibrary.Anchored(); machine.Run(4);
        Assert.Equal(4, E(machine.Drawing.Strokes).Length); Assert.Equal(5, E(machine.Drawing.Sites).Length - 1);
        var bindings = E(machine.Drawing.Sites).Cast<Relation>().ToArray();
        Assert.Same(bindings[^1].Left, machine.Drawing.AnchorSite); Assert.Same(machine.Drawing.CursorSite, machine.Drawing.AnchorSite);
        Assert.All(E(machine.Drawing.Orientation).Cast<Relation>().Where(row => ReferenceEquals(row.Left, machine.Drawing.Frame.Up)), row => Assert.Same(machine.Drawing.Frame.Down, row.Right));
        machine.Run(4); Assert.Equal(8, E(machine.Drawing.Strokes).Length); AssertAccounted(machine);
    }

    [Fact]
    public void ResumedRunUsesTheSameNativeMechanicsAsAnUninterruptedRun()
    {
        var whole = PatternMachineLibrary.Mirrored(); var resumed = PatternMachineLibrary.Mirrored();
        whole.Run(24); resumed.Run(9); resumed.Run(15);
        Assert.Equal(Signatures(whole), Signatures(resumed));
        Assert.Equal(24, E(resumed.Timeline).Length); AssertAccounted(resumed);
    }

    private static string[] Signatures(PatternMachine machine) => E(machine.Drawing.Strokes).Cast<Relation>().Select(stroke => string.Join(",", Word(stroke).Cast<Relation>().Select(move =>
    {
        Assert.True(machine.Drawing.Frame.Direction(move, out var axis, out var forward, out _));
        return (ReferenceEquals(axis, machine.Drawing.Frame.Horizontal) ? "H" : "V") + (forward ? "+" : "-");
    }))).ToArray();
    private static IElement[] Word(Relation stroke)
    { Assert.True(QualitativeStrokeGraph.TryReadStroke(stroke, out _, out _, out _, out var word)); return E(word); }
    private static void AssertAccounted(PatternMachine machine)
    {
        var read = machine.Account(); var active = Assert.IsType<Relation>(read.Right);
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
