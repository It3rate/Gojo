using Gojo.Core;
using static Phototaxis.Native;

namespace Phototaxis;

/// <summary>A disposable builder for a witnessed, coarse progress window and a bounded response.</summary>
public sealed class ProgressEscape
{
    public Carrier AnchorX { get; }
    public Carrier AnchorY { get; }
    public Carrier AnchorTime { get; }
    public Carrier AnchorEnergy { get; }
    public Carrier Ready { get; }
    public Carrier Until { get; }
    public Carrier Episodes { get; }
    public Relation DistanceSquared { get; }
    public Relation Trigger { get; }
    public Relation Active { get; }
    public Relation[] Cells { get; }
    public Relation Root => G(G(AnchorX, AnchorY, AnchorTime, AnchorEnergy, Ready, Until, Episodes),
        DistanceSquared, Trigger, Active, G(Cells), parameters, positionFrame);
    private readonly Relation parameters, positionFrame;

    // G(anchor stocks, position difference squared, elapsed/energy/progress claim,
    //   escape clock, ordered later-input cells, calibrations, periodic position frame).
    public ProgressEscape(Units u, Creature c, BodyObservations o, IElement window, IElement reach, IElement duration)
    {
        AnchorX = new(u.Position.UnitShape); AnchorY = new(u.Position.UnitShape);
        AnchorTime = new(u.Clock.UnitShape); AnchorEnergy = new(u.Calorie.UnitShape);
        Ready = new(u.Feature.UnitShape); Until = new(u.Clock.UnitShape); Episodes = new(u.Count.UnitShape);
        positionFrame = c.PositionFrame;
        var frame = Parts(positionFrame);
        var dx = CognitiveBody.Wrapped(u, ExpressionGraph.Subtract(u, o.X.ValueSlot, AnchorX.ValueSlot), (Relation)frame[0]);
        var dy = CognitiveBody.Wrapped(u, ExpressionGraph.Subtract(u, o.Y.ValueSlot, AnchorY.ValueSlot), (Relation)frame[1]);
        // Two-pixel read ticks keep the comparison within its finite selected
        // scope even in the largest supported arena; projection residue remains.
        var readX = Scale(dx, 1, 32); var readY = Scale(dy, 1, 32);
        DistanceSquared = Sum((Product(readX, readX), 1), (Product(readY, readY), 1));
        var windowClock = Scale(window, 1, 1, new Carrier(u.Clock.UnitShape));
        var reachPosition = Scale(reach, 1, 32, new Carrier(u.Position.UnitShape));
        var durationClock = Scale(duration, 1, 1, new Carrier(u.Clock.UnitShape));
        parameters = G(windowClock, reachPosition, durationClock);
        IElement A(IElement a, IElement b) => ExpressionGraph.And(u, a, b);
        Trigger = ExpressionGraph.And(u, Ready.ValueSlot,
            A(ExpressionGraph.Greater(u, Sum((o.Time.ValueSlot, 1), (AnchorTime.ValueSlot, -1)),
                    Sum((windowClock, 1), (L(u.Clock, -1), 1))),
                A(ExpressionGraph.Less(u, DistanceSquared, Product(reachPosition, reachPosition)),
                    A(ExpressionGraph.Less(u, c.Energy.ValueSlot, AnchorEnergy.ValueSlot),
                        ExpressionGraph.Less(u, c.Sources[4], L(u.Feature, 12))))));
        var deadline = SumTo(new Carrier(u.Clock.UnitShape), (o.Time.ValueSlot, 1), (durationClock, 1));
        var nextUntil = ExpressionGraph.Choose(u, Trigger, deadline, Until.ValueSlot);
        Active = ExpressionGraph.Greater(u, Until.ValueSlot, o.Time.ValueSlot);
        // Read the old anchor before updating it. Sampling and response are native
        // feedback; the host only advances the exposed cadence.
        var cadenceRead = ReadMany(windowClock)[0];
        var cadence = TryValue(cadenceRead, out var ticks) ? checked((int)Math.Max(4, ticks)) : 160;
        IElement Admit(IElement source, IElement previous) => ExpressionGraph.Choose(u,
            ExpressionGraph.Greater(u, o.Presence.ValueSlot, L(u.Count, 0)), source, previous);
        Cells = [ControllerFeedback.Create(u, Until, nextUntil, cadence, c.BornAt + 4),
            ControllerFeedback.Create(u, Episodes, ExpressionGraph.Choose(u, Trigger,
                SumTo(new Carrier(u.Count.UnitShape), (Episodes.ValueSlot, 1), (L(u.Count, 1), 1)), Episodes.ValueSlot), cadence, c.BornAt + 4),
            ControllerFeedback.Create(u, AnchorX, Admit(o.X.ValueSlot, AnchorX.ValueSlot), cadence, c.BornAt + 4),
            ControllerFeedback.Create(u, AnchorY, Admit(o.Y.ValueSlot, AnchorY.ValueSlot), cadence, c.BornAt + 4),
            ControllerFeedback.Create(u, AnchorEnergy, Admit(c.Energy.ValueSlot, AnchorEnergy.ValueSlot), cadence, c.BornAt + 4),
            ControllerFeedback.Create(u, AnchorTime, Admit(o.Time.ValueSlot, AnchorTime.ValueSlot), cadence, c.BornAt + 4),
            ControllerFeedback.Create(u, Ready, Admit(L(u.Feature, 15), Ready.ValueSlot), cadence, c.BornAt + 4)];
    }
}
