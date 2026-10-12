using ConnectionAssembly;
using Gojo.Core;
using Gojo.Equations;
using Gojo.Structures;
using static Phototaxis.Native;

namespace Phototaxis;

// G(target stock, next-value recipe, import calibration, interval, next clock, last step).
// Feedback is a later import into the stock, not recursive evaluation of its recipe.
public static class ControllerFeedback
{
    public static Relation Create(Units units, Carrier target, IElement source, int period, long time)
    {
        var interval = new Carrier(units.Clock.UnitShape); Aim(interval, period);
        var next = new Carrier(units.Clock.UnitShape); Aim(next, time);
        return G(target, source, Equation.Affine(NativeMath.Output(source), 0, 1, target, 0, 1),
            interval, next, G(new Placeholder()));
    }

    public static Relation Advance(Relation cell, Units units, long time)
    {
        var fields = Parts(cell);
        if (fields.Length != 6 || fields[0] is not Carrier target || fields[2] is not Relation calibration ||
            fields[3] is not Carrier interval || fields[4] is not Carrier next || time < (long)next.Value) return cell;
        var before = L(target, Held(target)); var read = ReadMany(fields[1])[0];
        Relation import = read;
        if (Equation.TryReadPrediction(read, out var value, out _) && !value.Carrier.IsUnknown)
        {
            if (!Equation.TryPredict(calibration, value, target, out var projected))
                projected = Relation.UnresolvedRead(read, calibration, target);
            import = G(read, projected, TryValue(projected, out var desired) ? Aim(target, desired) : G(new Placeholder()));
        }
        var step = TimeStepSequence.Step(new Carrier(), units.Clock, Math.Max(0, time - (long)interval.Value), time,
            before, import, L(target, Held(target)), read.Right);
        Aim(next, time + Math.Max(1, (long)interval.Value));
        return G(target, fields[1], calibration, interval, next, step);
    }
}
