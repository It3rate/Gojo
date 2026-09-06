using Gojo.Core;

namespace Gojo.Structures;

public static partial class BoundedQuantity
{
    /// <summary>
    /// Observes a flat sum of calibrated bounded samples. Shared slots combine
    /// before bounding; all their support premises must agree before cancellation.
    /// </summary>
    public static bool TryObserve(Relation expression, out Relation witness)
    {
        if (!TryAnalyze(expression, out var state)) return Unresolved(expression, out witness);
        witness = ObserveAffine(expression, state);
        return true;
    }

    /// <summary>
    /// Observes ports(affine -> calibration, ...) -> P(output,1). This explicit
    /// additive port read composes existing affine sources without an AST or
    /// automatic inspection of any sample's provenance.
    /// </summary>
    public static bool TryCombine(Relation request, out Relation witness)
    {
        if (!TryPoint(request.Right, out var output) || output.Ticks != 1 ||
            request.Left is not Relation ports || !ports.TryReadOrderedGroup(out var terms))
            return Unresolved(request, out witness);

        var combined = new AffineState(output.Carrier, Scalar.Zero);
        foreach (var term in terms)
        {
            if (term is not Relation { Left: Relation expression, Right: Relation calibration } ||
                !TryAnalyze(expression, out var state) ||
                !TryCalibration(calibration, out var source, out var target, out var scale, out var offset) ||
                !source.SameIdentityAs(state.Basis) || !target.SameIdentityAs(output.Carrier))
                return Unresolved(request, out witness);

            combined.Offset += state.Offset * scale + offset;
            foreach (var (slot, cause) in state.Causes)
                if (!Merge(combined.Causes, slot, cause.Basis, cause.Lower, cause.Upper,
                    cause.Coefficient * scale, cause.Evidence)) return Unresolved(request, out witness);
        }
        witness = ObserveAffine(request, combined);
        return true;
    }

    private static bool TryAnalyze(Relation expression, out AffineState state)
    {
        state = null!;
        if (!TryReadAffine(expression, out var offset, out var contributions, out _) ||
            !TryScalar(offset, out var basis, out var constant) ||
            !contributions.TryReadOrderedGroup(out var terms)) return false;

        state = new AffineState(basis, constant);
        foreach (var term in terms)
        {
            if (term is not Relation { Left: Relation sample, Right: Relation calibration } ||
                !sample.TryReadOrderedGroup(out var fields) || fields.Length != 3 ||
                !TrySpan(fields[1], out var sampleBasis, out var start, out var end) ||
                !TryCalibration(calibration, out var source, out var target, out var slope, out var shift) ||
                !source.SameIdentityAs(sampleBasis) || !target.SameIdentityAs(basis)) return false;

            var lower = new Scalar(Math.Min(start, end));
            var upper = new Scalar(Math.Max(start, end));
            state.Offset += shift;
            if (fields[0] is Placeholder { IsInverse: false } slot)
            {
                if (!Merge(state.Causes, slot, source, lower, upper, slope, [sample])) return false;
            }
            else if (TryScalar(fields[0], out var exactBasis, out var value) &&
                exactBasis.SameIdentityAs(source) && lower <= value && value <= upper)
                state.Offset += slope * value;
            else return false;
        }
        return true;
    }

    private static bool Merge(Dictionary<Placeholder, Cause> causes, Placeholder slot, Carrier basis,
        Scalar lower, Scalar upper, Scalar coefficient, IEnumerable<IElement> evidence)
    {
        if (causes.TryGetValue(slot, out var prior))
        {
            if (!prior.Basis.SameIdentityAs(basis)) return false;
            prior.Lower = Scalar.Max(prior.Lower, lower);
            prior.Upper = Scalar.Min(prior.Upper, upper);
            if (prior.Lower > prior.Upper) return false;
            prior.Coefficient += coefficient;
            prior.Evidence.AddRange(evidence);
        }
        else causes.Add(slot, new Cause(basis, lower, upper, coefficient, evidence));
        return true;
    }

    private static Relation ObserveAffine(IElement source, AffineState state)
    {
        Bounds(state, state.Causes, out var lower, out var upper);
        var normalized = Normalize(state, source);
        return Observation(source, normalized, state.Basis, lower, upper, source);
    }

    private static Relation Normalize(AffineState state, IElement provenance)
    {
        var terms = new List<IElement>();
        foreach (var (slot, cause) in state.Causes)
        {
            var sample = Sample(new Carrier(), slot, Relation.Segment(cause.Basis,
                checked((long)cause.Lower.Numerator), checked((long)cause.Upper.Numerator)),
                Relation.OrderedGroup(new Carrier(), [.. cause.Evidence]));
            var calibration = new Relation(
                Relation.Segment(cause.Basis, 0, checked((long)cause.Coefficient.Denominator)),
                Relation.Segment(state.Basis, 0, checked((long)cause.Coefficient.Numerator)));
            // Keep zero coefficients: their shared-domain premises remain
            // relevant, and the cancellation still has an inspectable source.
            terms.Add(new Relation(sample, calibration));
        }
        return Affine(new Carrier(), state.Offset.Encode(state.Basis),
            Relation.OrderedGroup(new Carrier(), [.. terms]), provenance);
    }

    private static void Bounds(AffineState state, Dictionary<Placeholder, Cause> joint,
        out Scalar lower, out Scalar upper)
    {
        lower = upper = state.Offset;
        foreach (var (slot, cause) in state.Causes)
        {
            var support = joint[slot];
            var a = support.Lower * cause.Coefficient;
            var b = support.Upper * cause.Coefficient;
            lower += Scalar.Min(a, b); upper += Scalar.Max(a, b);
        }
    }

    private static bool TryJoint(AffineState left, AffineState right, out Dictionary<Placeholder, Cause> joint)
    {
        joint = [];
        foreach (var state in new[] { left, right })
            foreach (var (slot, cause) in state.Causes)
                if (!Merge(joint, slot, cause.Basis, cause.Lower, cause.Upper,
                    Scalar.Zero, cause.Evidence)) return false;
        return true;
    }

    private static bool SameFunction(AffineState left, AffineState right)
    {
        if (!left.Basis.SameIdentityAs(right.Basis) || left.Offset != right.Offset) return false;
        foreach (var slot in left.Causes.Keys.Concat(right.Causes.Keys).Distinct())
        {
            var a = left.Causes.TryGetValue(slot, out var l) ? l.Coefficient : Scalar.Zero;
            var b = right.Causes.TryGetValue(slot, out var r) ? r.Coefficient : Scalar.Zero;
            if (a != b) return false;
        }
        return true;
    }

    // These caches are local reads of the input graph. Normalize emits their
    // slots, support intersections, calibrations and correspondence as relations.
    private sealed class AffineState(Carrier basis, Scalar offset)
    {
        internal Carrier Basis { get; } = basis;
        internal Scalar Offset { get; set; } = offset;
        internal Dictionary<Placeholder, Cause> Causes { get; } = [];
    }

    private sealed class Cause(Carrier basis, Scalar lower, Scalar upper,
        Scalar coefficient, IEnumerable<IElement> evidence)
    {
        internal Carrier Basis { get; } = basis;
        internal Scalar Lower { get; set; } = lower;
        internal Scalar Upper { get; set; } = upper;
        internal Scalar Coefficient { get; set; } = coefficient;
        internal List<IElement> Evidence { get; } = [.. evidence];
    }
}
