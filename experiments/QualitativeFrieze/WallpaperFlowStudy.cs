using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;

namespace Gojo.Experiments.QualitativeFrieze;

/// <summary>Small live machines over the square study's unmeasured basis.
/// Reservoir discharge, questions and acknowledged drawing actions produce the
/// traces. The cell is a declared placement basis, not preconstructed output
/// geometry or a claim to classify an infinite wallpaper field.</summary>
public sealed class WallpaperFlowStudy
{
    // half-period = actual ordered subword of the cell's X path
    // glide-support = (half-period -> X-period) ->
    //                 (actual reflection -> (double-half equality -> cell-read))
    // periodic-port correspondence = actual input port ->
    //                               (actual cell seam -> actual period word)
    // trace = (study.Surface -> machine.Run(0)) ->
    //         ((generated graph -> actual site bindings) ->
    //          (drawing receipts -> unresolved))
    // Equal formal positions do not identify runtime sites. In particular a
    // complete drawn circuit creates a fresh endpoint; Reset reuses the anchor.
    public QualitativeSquareStudy Basis { get; } = new();
    public PatternMachine Rotation { get; }
    public PatternMachine Glide { get; }
    public PatternMachine Placement { get; }
    public Relation HalfPeriod { get; }
    public Relation HalfPeriodPath { get; }
    public Relation SecondHalfPeriodPath { get; }
    public Relation HalfPeriodSubdivision { get; }
    public Relation MirrorHalfRead { get; }
    public Relation MirrorHalfEquality { get; }
    public Relation DoubleHalfRead { get; }
    public Relation GlideSupport { get; }
    public Relation PeriodicPorts { get; }
    public Relation Definitions { get; }
    public Relation Surface { get; }

    public sealed record Trace(PatternMachine Machine, Relation RunRead, Relation DrawingState,
        QualitativeStrokeGraph Graph, Relation Bindings, Relation DrawingReceipts,
        Relation Unresolved, Relation Surface);

    public WallpaperFlowStudy()
    {
        var frame = Basis.Frame;
        var x = E(Basis.Cell.XWord);
        QualitativePeriodicCell.TryReadPath(Basis.Cell.Bottom, out var bottomSites, out _);
        var sites = E(bottomSites);
        HalfPeriod = S(x[0], x[1]);
        HalfPeriodPath = QualitativePeriodicCell.Path(S(sites[0], sites[1], sites[2]), HalfPeriod);
        var otherHalf = S(x[2], x[3]);
        SecondHalfPeriodPath = QualitativePeriodicCell.Path(S(sites[2], sites[3], sites[4]), otherHalf);
        // The two subpaths reuse the parent's actual ordered sites and move
        // occurrences. Equality of their reach words is a separate local read.
        HalfPeriodSubdivision = new Relation(Basis.Cell.Bottom,
            new Relation(S(HalfPeriodPath, SecondHalfPeriodPath), new Relation(sites[2],
                frame.Reduce(HalfPeriod, Basis.Inverse(otherHalf)))));
        DoubleHalfRead = frame.Reduce(S([.. E(HalfPeriod), .. E(HalfPeriod)]), Basis.Inverse(Basis.Cell.XWord));
        var mirror = Basis.AxisTransform(Basis.OtherDirectionMirror);
        MirrorHalfRead = mirror.Apply(HalfPeriod);
        var hasImage = mirror.TryReadComplete(MirrorHalfRead, out var image);
        MirrorHalfEquality = frame.Reduce(hasImage ? image : StructuralConstraint.Open(MirrorHalfRead), Basis.Inverse(HalfPeriod));
        GlideSupport = new Relation(new Relation(HalfPeriod, Basis.Cell.XWord),
            new Relation(Basis.OtherDirectionMirror, new Relation(HalfPeriodSubdivision,
                new Relation(new Relation(MirrorHalfRead, MirrorHalfEquality), new Relation(DoubleHalfRead, Basis.CellRead)))));

        var rotating = Drawing();
        var hook = Muscle("hook strokes", 1,
            S(rotating.Draw(W(frame.Right))), S(rotating.Draw(W(frame.Up))));
        var turn = Muscle("turn on completed hook", 2,
            S(rotating.Reset(), rotating.DirectionAction(Basis.DirectionTurn)));
        Rotation = new("Threshold quarter-turn hooks", rotating, [hook, turn],
            S(hook.Accumulator.InputPort), S(new Relation(hook.OutputPort, turn.Accumulator.InputPort)));

        var gliding = Drawing();
        // Three sides and a pause give a simple asymmetric local staple. The
        // explicit reset reuses its anchor before the next placement; no
        // connector is drawn across the remaining gap.
        var staple = Muscle("staple strokes", 1,
            S(gliding.Draw(W(frame.Up))), S(gliding.Draw(W(frame.Right))),
            S(gliding.Draw(W(frame.Down))), S());
        var glide = Muscle("glide on completed staple", 4,
            S(gliding.Reset(), gliding.Jump(HalfPeriod), gliding.Anchor(),
                gliding.DirectionAction(Basis.OtherDirectionMirror)));
        Glide = new("Threshold glide staples", gliding, [staple, glide],
            S(staple.Accumulator.InputPort), S(new Relation(staple.OutputPort, glide.Accumulator.InputPort)));

        Placement = CreatePlacement();
        PeriodicPorts = S(
            new Relation(Placement.Muscles[0].Accumulator.InputPort, new Relation(Basis.Cell.SeamX, Basis.Cell.XWord)),
            new Relation(Placement.Muscles[1].Accumulator.InputPort, new Relation(Basis.Cell.SeamY, Basis.Cell.YWord)));
        Definitions = S(Rotation.Surface, Glide.Surface, Placement.Surface);
        Surface = new Relation(Basis.Surface, new Relation(Definitions, new Relation(GlideSupport, PeriodicPorts)));

        QualitativeDrawingReceiver Drawing() => new(frame, initialSite: frame.Origin);
        Relation W(IElement direction) => S(QualitativeMotionFrame.Move(direction, Basis.Reach));
    }

    /// <summary>An independently identified receiver over the same exposed
    /// period words, suitable for comparing actual alternative schedules.</summary>
    public PatternMachine CreatePlacement()
    {
        var drawing = new QualitativeDrawingReceiver(Basis.Frame, initialSite: Basis.Frame.Origin);
        var alongX = Muscle("X period", 1, S(drawing.Jump(Basis.Cell.XWord), drawing.Anchor()));
        var alongY = Muscle("Y period", 1, S(drawing.Jump(Basis.Cell.YWord), drawing.Anchor()));
        return new("Independent periodic placement ports", drawing, [alongX, alongY],
            S(alongX.Accumulator.InputPort, alongY.Accumulator.InputPort), S());
    }

    /// <summary>Captures the receiver's actual output and accepted effects. No
    /// static motif or seam placement is substituted for the generated trace.</summary>
    public Trace Capture(PatternMachine machine)
    {
        if (!ReferenceEquals(machine.Drawing.Frame, Basis.Frame))
            throw new ArgumentException("The trace must use this study's exposed motion basis.", nameof(machine));
        var run = machine.Run(0); var state = machine.Drawing.State;
        var bindings = machine.Drawing.SiteBindings;
        var graph = new QualitativeStrokeGraph(Basis.Frame,
            S(E(bindings).Cast<Relation>().Select(row => row.Left).ToArray()), machine.Drawing.Strokes);
        var effects = new List<IElement>(); var pending = E(machine.Unresolved).ToList();
        foreach (var step in E(machine.Timeline).Cast<Relation>())
        {
            if (step.Right is not Relation { Right: Relation { Right: Relation { Left: Relation receipts } } })
            { pending.Add(step); continue; }
            foreach (var item in E(receipts))
                if (item is Relation receipt)
                {
                    effects.Add(receipt);
                    if (!machine.Drawing.TryReadAccepted(receipt, out _, out _)) pending.Add(receipt);
                }
                else pending.Add(item);
        }
        var actual = S(effects.ToArray()); var unresolved = S(pending.ToArray());
        var surface = new Relation(new Relation(Surface, run),
            new Relation(new Relation(graph.Surface, bindings), new Relation(actual, unresolved)));
        return new(machine, run, state, graph, bindings, actual, unresolved, surface);
    }

    /// <summary>A bare formal displacement comparison using the captured state.
    /// Use ReadPositionEvidence to retain its historical cursor/trace context.
    /// Empty displacement does not assert shared site identity.</summary>
    public Relation ReadPosition(Trace trace, Relation expected)
    {
        if (!trace.Machine.Drawing.TryReadState(trace.DrawingState, out _, out var cursor, out _, out _, out _, out _, out _))
            return new Relation(new Relation(trace.Surface, expected), StructuralConstraint.Open(trace.DrawingState));
        return Basis.Frame.Reduce((Relation)cursor.Right, Basis.Inverse(expected));
    }

    public Relation ReadOrientation(Trace trace, Relation expected)
    {
        if (!trace.Machine.Drawing.TryReadState(trace.DrawingState, out _, out _, out _, out var actual, out _, out _, out _))
            return Basis.DirectionActions.Compare(StructuralConstraint.Open(trace.DrawingState), S(expected));
        return Basis.DirectionActions.Compare(S(actual), S(expected));
    }

    // contextual position = (trace.Surface -> (historical state -> actual cursor binding)) ->
    //                       (expected word -> bare displacement comparison)
    // contextual orientation uses the actual historical direction map instead.
    public Relation ReadPositionEvidence(Trace trace, Relation expected)
    {
        var known = trace.Machine.Drawing.TryReadState(trace.DrawingState, out _, out var cursor, out _, out _, out _, out _, out _);
        IElement selected = known ? cursor : StructuralConstraint.Open(trace.DrawingState);
        return new Relation(new Relation(trace.Surface, new Relation(trace.DrawingState, selected)),
            new Relation(expected, ReadPosition(trace, expected)));
    }

    public Relation ReadOrientationEvidence(Trace trace, Relation expected)
    {
        var known = trace.Machine.Drawing.TryReadState(trace.DrawingState, out _, out _, out _, out var orientation, out _, out _, out _);
        IElement selected = known ? orientation : StructuralConstraint.Open(trace.DrawingState);
        return new Relation(new Relation(trace.Surface, new Relation(trace.DrawingState, selected)),
            new Relation(expected, ReadOrientation(trace, expected)));
    }

    private static PatternMuscle Muscle(string name, int threshold, params Relation[] bundles) =>
        new(name, S(Enumerable.Range(0, threshold).Select(_ => (IElement)new Placeholder()).ToArray()), bundles);
    private static Relation S(params IElement[] values) => StructuralConstraint.Closed(values);
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
}
