using Gojo.Core;
using Gojo.Patterns;
using Scalar = Gojo.Core.ExactScalar;

namespace Gojo.Structures;

public static partial class LocalCalibration
{
    /// <summary>
    /// Captures group(candidate template, endpoint slot, actuator, endpoint map).
    /// The adjustable slot is the second target endpoint only. Exact observations
    /// of actuator value and tension are copied; other retained premises are owned
    /// as fixed structure by this experiment, not deep-frozen by this method.
    /// </summary>
    public static bool TryCapture(Relation recipe, out Relation state)
    {
        if (!Recipe(recipe, out var r, out _, out var actuator, out var output, out var slope, out var offset))
            return Unresolved(recipe, out state);
        try
        {
            var value = new Scalar(actuator.Value);
            var endpoint = (offset + slope * value).Encode(output);
            var binding = new Relation(r[1], endpoint);
            var candidate = StructuralPattern.Refine(r[0], binding);
            state = ReadWitness.Create(new Carrier(), recipe, binding, candidate,
                new CarrierLocation(actuator, actuator.Tension), value.Encode(actuator));
            if (State(state, out _)) return true;
            return Unresolved(recipe, out state);
        }
        catch (OverflowException) { return Unresolved(recipe, out state); }
    }

    /// <summary>
    /// Plans group(state, comparison, relaxation map, acceptance band, focus).
    /// Uses the selected affine model's sensitivity to its single exposed endpoint.
    /// Output is group(exact delta, whole delta, inverse response, predicted residual).
    /// Residue retains unrepresented actuator and output participation separately.
    /// This read never changes an actuator or counts evidence as confidence.
    /// </summary>
    public static bool TryPlanResponse(Relation request, out Relation plan)
    {
        if (!Fields(request, 5, out var r) || !State(r[0], out var state) || state.Tension != 0 ||
            !Comparison(r[1], out var comparison, out var output, out var error) ||
            !Prediction(comparison[0], out var prediction, out _, out _) || !Same(prediction[1], state.Fields[2]) ||
            !Observation(prediction[0], out var observation) ||
            !TryScalar(observation[2], out _, out var sample) ||
            !Map(state.Candidate[2], out _, out var a, out var b, out _, out _, out _) ||
            !Map(r[2], out var rs, out var ra, out var rb, out var rt, out var rc, out var rd) ||
            !rs.SameIdentityAs(output) || !rt.SameIdentityAs(output) || !ra.IsZero || !rc.IsZero ||
            !Range(r[3], out var ab, out var lo, out var hi) || !ab.SameIdentityAs(output) ||
            lo > Scalar.Zero || hi < Scalar.Zero ||
            !Fields(r[4], 8, out var focus) || !Fields(focus[2], 2, out var selected) ||
            !Same(selected[0], state.Recipe[1]) || !Same(selected[1], state.Actuator))
            return Unresolved(request, out plan);

        var alpha = rd / rb;
        var sensitivity = (sample - a) / (b - a) * state.EndpointSlope;
        var acceptable = error >= lo && error <= hi;
        if (alpha <= Scalar.Zero || alpha > Scalar.One || !acceptable && sensitivity.IsZero)
            return Unresolved(request, out plan);

        try
        {
            var requested = acceptable ? Scalar.Zero : alpha * error / sensitivity;
            var whole = new Scalar(requested.Numerator / requested.Denominator);
            IElement inverseResponse = sensitivity.IsZero ? new Placeholder() :
                new Relation(new Relation(Scalar.Zero.Encode(output), sensitivity.Encode(output)),
                    Relation.Segment(state.Actuator, 0, 1));
            var result = Group(requested.Encode(state.Actuator), whole.Encode(state.Actuator), inverseResponse,
                (error - whole * sensitivity).Encode(output));
            var residue = Group((requested - whole).Encode(state.Actuator),
                ((requested - whole) * sensitivity).Encode(output));
            plan = ReadWitness.Create(new Carrier(), r[1], request, result, residue,
                Group(r[0], r[4], sensitivity.Encode(output)));
            return true;
        }
        catch (OverflowException) { return Unresolved(request, out plan); }
    }

    /// <summary>
    /// Applies group(plan, current state version, ordered application history,
    /// count controller). The controller is a nonnegative integral 0..N step span.
    /// A free forward actuator is the only mutation target. A new state version
    /// and a new history are returned, including zero steps, stalls and unpaid draw.
    /// Failed validation makes no change and returns the original history.
    /// </summary>
    public static bool TryApplyResponse(Relation request, out Relation application, out Relation history)
    {
        history = Group();
        if (!Fields(request, 4, out var r) || r[2] is not Relation previous ||
            !previous.TryReadOrderedGroup(out var attempts)) return Unresolved(request, out application);
        history = previous;
        if (!Fields(r[0], 5, out var p) || p[1] is not Relation planRequest ||
            !Fields(planRequest, 5, out var pr) || !Same(p[0], pr[1]) || !Same(pr[0], r[1]) ||
            !State(r[1], out var current) || !Fields(p[2], 4, out var result) ||
            !TryScalar(result[1], out var receiver, out var delta) ||
            !receiver.SameIdentityAs(current.Actuator) || !delta.Denominator.IsOne ||
            !Range(r[3], out _, out var start, out var limit) || !start.IsZero ||
            limit < Scalar.Zero || !limit.Denominator.IsOne || attempts.Length >= limit.Numerator ||
            current.Actuator.AttachedRelations.Count != 0 || current.Actuator.ExistingInverse is not null ||
            current.Actuator.Value != (ulong)current.Value || current.Actuator.Tension != current.Tension ||
            !TryPlanResponse(planRequest, out var checkedPlan) || !Fields(checkedPlan, 5, out var cp) ||
            !Fields(cp[2], 4, out var checkedResult) || !PlanEqual(result, checkedResult) ||
            !Fields(p[3], 2, out var residue) || !Fields(cp[3], 2, out var checkedResidue) ||
            !ScalarEqual(residue[0], checkedResidue[0]) || !ScalarEqual(residue[1], checkedResidue[1]) ||
            !History(attempts, r[0], r[1], current.Recipe))
            return Unresolved(request, out application);

        // Prepare every representable result before mutation. This boundary is
        // deliberately one free actuator; it makes no network rollback promise.
        try
        {
            var total = new Scalar(current.Value) + new Scalar(current.Tension) + delta;
            var nextValue = Scalar.Max(Scalar.Zero, total);
            var nextTension = Scalar.Min(Scalar.Zero, total);
            var valueTicks = checked((long)nextValue.Numerator);
            var tensionTicks = checked((long)nextTension.Numerator);
            var ticks = checked((long)delta.Numerator);
            if (!Recipe(current.Fields[0], out var recipe, out _, out _, out var output, out var slope, out var offset))
                return Unresolved(request, out application);
            var endpoint = (offset + slope * nextValue).Encode(output);
            var binding = new Relation(recipe[1], endpoint);
            var after = ReadWitness.Create(new Carrier(), current.Fields[0], binding,
                StructuralPattern.Refine(recipe[0], binding), new CarrierLocation(receiver, tensionTicks),
                new CarrierLocation(receiver, valueTicks));
            if (!State(after, out _)) return Unresolved(request, out application);

            application = ReadWitness.Create(new Carrier(), r[0], pr[4],
                Group(r[1], result[1], after), Group(p[3], new CarrierLocation(receiver, tensionTicks)),
                Group(previous, r[3]));
            history = Relation.OrderedGroup((Carrier)previous.Right, [.. attempts, application]);
            if (ticks > 0) receiver.Add((ulong)ticks);
            if (ticks < 0) receiver.Remove((ulong)-(Int128)ticks);
            return true;
        }
        catch (OverflowException) { history = previous; return Unresolved(request, out application); }
    }

    private static bool History(IElement[] attempts, IElement plan, IElement current, IElement[] recipe)
    {
        IElement? previousAfter = null;
        foreach (var attempt in attempts)
        {
            if (!Fields(attempt, 5, out var a) || Same(a[0], plan) || !Fields(a[2], 3, out var result) ||
                !State(result[0], out var before) || !State(result[2], out var after) ||
                !Same(before.Fields[0], after.Fields[0]) || !Same(before.Recipe[2], recipe[2]) ||
                previousAfter is not null && !Same(previousAfter, result[0])) return false;
            previousAfter = result[2];
        }
        return previousAfter is null || Same(previousAfter, current);
    }

    private static bool PlanEqual(IElement[] a, IElement[] b) =>
        ScalarEqual(a[0], b[0]) && ScalarEqual(a[1], b[1]) && ScalarEqual(a[3], b[3]) &&
        (a[2] is Placeholder && b[2] is Placeholder ||
         a[2] is Relation ar && b[2] is Relation br &&
         ar.Left is Relation al && br.Left is Relation bl && ar.Right is Relation at && br.Right is Relation bt &&
         ScalarEqual(al.Left, bl.Left) && ScalarEqual(al.Right, bl.Right) &&
         ScalarEqual(at.Left, bt.Left) && ScalarEqual(at.Right, bt.Right));

    private static bool Recipe(IElement element, out IElement[] r, out IElement[] template,
        out Carrier actuator, out Carrier output, out Scalar slope, out Scalar offset)
    {
        if (Fields(element, 4, out r) && Fields(r[0], 6, out template) &&
            r[1] is Placeholder { IsInverse: false } && r[2] is Carrier { IsUnknown: false, IsInverse: false } gain &&
            template[2] is Relation map && Range(map.Left, out _, out var a, out var b) && a != b &&
            map.Right is Relation target && Same(target.Right, r[1]) &&
            TryScalar(target.Left, out var targetBasis, out _) &&
            TryScalar(template[3], out var floorBasis, out _) && targetBasis.SameIdentityAs(floorBasis) &&
            Map(r[3], out var source, out var c, out var d, out output, out var e, out var f) && e != f &&
            gain.SameIdentityAs(source) && output.SameIdentityAs(targetBasis))
        {
            actuator = gain; slope = (f - e) / (d - c); offset = e - slope * c;
            return true;
        }
        template = []; actuator = output = null!; slope = offset = Scalar.Zero;
        return false;
    }

    // A temporary read over the canonical graph, never serialized separately.
    private readonly record struct StateView(IElement[] Fields, IElement[] Recipe, IElement[] Candidate,
        Carrier Actuator, long Value, long Tension, Scalar EndpointSlope);

    private static bool State(IElement element, out StateView view)
    {
        view = default;
        if (!Fields(element, 5, out var f) ||
            !Recipe(f[0], out var recipe, out var template, out var actuator, out var output, out var slope, out var offset) ||
            !TryScalar(f[4], out var vb, out var value) || !vb.SameIdentityAs(actuator) ||
            !TryScalar(f[3], out var tb, out var tension) || !tb.SameIdentityAs(actuator) ||
            !value.Denominator.IsOne || value < Scalar.Zero || !tension.Denominator.IsOne ||
            !Fields(f[2], 6, out var candidate) ||
            f[1] is not Relation binding || !Same(binding.Left, recipe[1]) ||
            !TryScalar(binding.Right, out var eb, out var endpoint) || !eb.SameIdentityAs(output) ||
            endpoint != offset + slope * value || candidate[2] is not Relation map ||
            template[2] is not Relation pattern || !Same(map.Left, pattern.Left) ||
            map.Right is not Relation right || pattern.Right is not Relation patternRight ||
            !Same(right.Left, patternRight.Left) || !Same(right.Right, binding.Right)) return false;
        foreach (var i in new[] { 0, 1, 3, 4, 5 })
            if (!Same(candidate[i], template[i])) return false;
        view = new StateView(f, recipe, candidate, actuator, checked((long)value.Numerator),
            checked((long)tension.Numerator), slope);
        return true;
    }
}
