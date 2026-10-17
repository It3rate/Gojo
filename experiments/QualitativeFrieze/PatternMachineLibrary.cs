using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;

namespace Gojo.Experiments.QualitativeFrieze;

/// <summary>Small operating definitions. Only local reach words, finite muscle
/// phases, reservoirs and connections are authored. Sites, timing, paths and
/// causal strokes are produced by running the connected machines.</summary>
public static class PatternMachineLibrary
{
    public static PatternMachine[] Create() => [Chevron(), Chevron(1), Key(), Mirrored(), Anchored()];

    public static PatternMachine Chevron(int verticalThreshold = 2)
    {
        var frame = new QualitativeMotionFrame(); var drawing = new QualitativeDrawingReceiver(frame);
        var h = new Placeholder(); var v = new Placeholder();
        var drift = Muscle("drift", 1, S(drawing.Draw(W(frame.Right, h))));
        var lift = Muscle("alternating lift", verticalThreshold,
            S(drawing.Draw(W(frame.Up, v))), S(drawing.Draw(W(frame.Up, v))),
            S(drawing.Draw(W(frame.Down, v))), S(drawing.Draw(W(frame.Down, v))));
        return new(verticalThreshold == 2 ? "Threshold terraces" : "Threshold diagonals", drawing,
            [drift, lift], S(drift.Accumulator.InputPort, lift.Accumulator.InputPort), S());
    }

    public static PatternMachine Key()
    {
        var frame = new QualitativeMotionFrame(); var drawing = new QualitativeDrawingReceiver(frame);
        var h = new Placeholder(); var v = new Placeholder();
        Relation D(IElement direction, IElement reach, int repeats = 1) => S(drawing.Draw(W(direction, reach, repeats)));
        // A small turn controller, not per-column strokes: the same muscles
        // release at every threshold, and the controller advances on discharge.
        var motor = Muscle("turn motor", 2,
            D(frame.Up, v, 3), D(frame.Right, h, 3), D(frame.Down, v, 2),
            D(frame.Left, h, 2), D(frame.Up, v), D(frame.Right, h),
            D(frame.Down, v, 2), D(frame.Right, h, 2));
        return new("Threshold key motor", drawing, [motor], S(motor.Accumulator.InputPort), S());
    }

    public static PatternMachine Mirrored()
    {
        var frame = new QualitativeMotionFrame(); var drawing = new QualitativeDrawingReceiver(frame);
        var h = new Placeholder(); var v = new Placeholder();
        var drift = Muscle("drift", 1, S(drawing.Draw(W(frame.Right, h))));
        var lift = Muscle("lift", 2, S(drawing.Draw(W(frame.Up, v))), S(drawing.Draw(W(frame.Down, v))));
        var mirror = Muscle("mirror latch", 8, S(drawing.Mirror(drawing.VerticalMirror)));
        // The lift's released packets fill the mirror latch. Drawing is an
        // observation of lift firing; it does not consume a duplicate packet.
        return new("Cascaded mirror latch", drawing, [drift, lift, mirror],
            S(drift.Accumulator.InputPort, lift.Accumulator.InputPort),
            S(new Relation(lift.OutputPort, mirror.Accumulator.InputPort)));
    }

    public static PatternMachine Anchored()
    {
        var frame = new QualitativeMotionFrame(); var drawing = new QualitativeDrawingReceiver(frame);
        var h = new Placeholder(); var v = new Placeholder();
        var motor = Muscle("diamond muscle", 1,
            S(drawing.Draw(S(QualitativeMotionFrame.Move(frame.Right, h), QualitativeMotionFrame.Move(frame.Up, v)))),
            S(drawing.Draw(S(QualitativeMotionFrame.Move(frame.Right, h), QualitativeMotionFrame.Move(frame.Down, v)))),
            S(drawing.Draw(S(QualitativeMotionFrame.Move(frame.Left, h), QualitativeMotionFrame.Move(frame.Down, v)))),
            S(drawing.Draw(S(QualitativeMotionFrame.Move(frame.Left, h), QualitativeMotionFrame.Move(frame.Up, v)))));
        var anchor = Muscle("anchor and leap", 4,
            S(drawing.Reset(), drawing.Jump(W(frame.Right, h, 3)), drawing.Anchor(), drawing.Mirror(drawing.VerticalMirror)));
        return new("Anchored diamond shuttles", drawing, [motor, anchor], S(motor.Accumulator.InputPort),
            S(new Relation(motor.OutputPort, anchor.Accumulator.InputPort)));
    }

    public static PatternMuscle Muscle(string name, int threshold, params Relation[] bundles)
    {
        if (threshold is < 1 or > 32) throw new ArgumentOutOfRangeException(nameof(threshold));
        return new PatternMuscle(name, S(Enumerable.Range(0, threshold).Select(_ => (IElement)new Placeholder()).ToArray()), bundles);
    }
    private static Relation W(IElement direction, IElement reach, int repeats = 1) => S(Enumerable.Range(0, repeats)
        .Select(_ => (IElement)QualitativeMotionFrame.Move(direction, reach)).ToArray());
    private static Relation S(params IElement[] values) => StructuralConstraint.Closed(values);
}
