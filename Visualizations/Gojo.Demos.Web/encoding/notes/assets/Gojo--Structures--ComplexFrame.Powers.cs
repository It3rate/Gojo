using System.Numerics;
using Gojo.Core;
using Scalar = Gojo.Core.ExactScalar;

namespace Gojo.Structures;

public static partial class ComplexFrame
{
    /// <summary>
    /// Reads group(basis,value,signed repetition span). Binary composition keeps
    /// each actual multiply and inversion as structure. Zero to zero is left
    /// unresolved by this selected domain; negative powers require nonzero input.
    /// </summary>
    public static bool TryPower(Relation request, out Relation witness)
    {
        var captured = Capture(request);
        if (!Fields(captured, 3, out var f) || !ReadBasis(f[0], out var frame) ||
            !ReadValue(f[1], frame, out var value) || value.Any(v => v is null) ||
            !Count(f[2], out _, out var exponent)) return Unresolved(request, captured, out witness);
        try
        {
            if (!Power(frame, f[1], value, exponent, out var result, out var trace)) return Unresolved(request, captured, out witness);
            return Finish(request, captured, frame, result, trace, out witness);
        }
        catch (OverflowException) { return Unresolved(request, captured, out witness); }
    }

    /// <summary>
    /// Reads group(basis,z,p span,q span), reducing p/q with positive q. Result:
    /// group(power constraint, candidate witnesses). Exact integer and square-
    /// root branches are computed; other roots keep their open power equation.
    /// No principal branch is selected. Each candidate is checked by replaying
    /// its q-th power against z^p in the same coefficient basis.
    /// </summary>
    public static bool TryFractionalPower(Relation request, out Relation witness)
    {
        var captured = Capture(request);
        if (!Fields(captured, 4, out var f) || !ReadBasis(f[0], out var frame) ||
            !ReadValue(f[1], frame, out var z) || !Count(f[2], out var countAxis, out var p) ||
            !Count(f[3], out var qAxis, out var q) || !countAxis.SameIdentityAs(qAxis) || q <= 0)
            return Unresolved(request, captured, out witness);
        var gcd = BigInteger.GreatestCommonDivisor(p, q); p /= gcd; q /= gcd;
        var pSpan = Relation.Segment(countAxis, 0, checked((long)p));
        var qSpan = Relation.Segment(countAxis, 0, checked((long)q));
        var open = Value(frame, [null, null]);
        // The same candidate feeds the repeated product, preserving correlation.
        var targetRequest = Group(f[0], f[1], pSpan);
        IElement target = new Placeholder();
        var trace = Group();
        Scalar?[] targetValue = [null, null];
        try
        {
            if (z.All(v => v is not null) && Power(frame, f[1], z, p, out targetValue, out trace)) target = Value(frame, targetValue);
            var candidateRequest = Group(f[0], open, qSpan);
            var constraint = new Relation(candidateRequest, target);
            var evidence = Group(captured, Group(pSpan, qSpan), new Relation(targetRequest, target), trace);
            var candidates = new List<Scalar?[]>();
            if (target is not Placeholder)
            {
                if (q.IsOne) candidates.Add(targetValue);
                else if (q == 2 && SquareRoots(targetValue, out var roots)) candidates.AddRange(roots);
                // z^p=0 has a unique root for every positive degree.
                else if (targetValue.All(v => v is { IsZero: true })) candidates.Add([Scalar.Zero, Scalar.Zero]);
            }
            var checkedCandidates = new List<IElement>();
            foreach (var candidate in candidates)
            {
                var value = Value(frame, candidate);
                if (!Power(frame, value, candidate, q, out var reproduced, out var steps) ||
                    !reproduced.SequenceEqual(targetValue)) continue;
                var reproduction = Value(frame, reproduced);
                var check = Observe(value, new Relation(Group(f[0], value, qSpan), target), reproduction, [],
                    Group(new Relation(open, value), steps));
                checkedCandidates.Add(check);
            }
            var complete = checkedCandidates.Count != 0;
            // For unresolved real-axis square roots retain the known zero
            // component without pretending to have either irrational magnitude.
            if (!complete && q == 2 && target is not Placeholder && targetValue[1] is { IsZero: true })
            {
                open = Value(frame, targetValue[0]!.Value < Scalar.Zero ? [Scalar.Zero, null] : [null, Scalar.Zero]);
                constraint = new Relation(Group(f[0], open, qSpan), target);
            }
            var result = Group(constraint, Group([.. checkedCandidates]));
            witness = Observe(request, f[0], result, complete ? [] : [constraint, targetRequest], evidence);
            return complete;
        }
        catch (OverflowException)
        {
            var constraint = new Relation(Group(f[0], open, qSpan), target);
            witness = Observe(request, f[0], Group(constraint, Group()), [constraint, targetRequest],
                Group(captured, Group(pSpan, qSpan), new Relation(targetRequest, target), trace));
            return false;
        }
    }

    /// <summary>
    /// Reads group(fractional-power request,reference value). Recomputes the
    /// candidates and exact squared coefficient distances; ties stay plural.
    /// Returns group(root witness, distance witnesses, nearest candidate group).
    /// Success means one locally nearest candidate, not global branch continuity.
    /// </summary>
    public static bool TrySelectNearest(Relation request, out Relation witness)
    {
        var captured = Capture(request);
        if (!Fields(captured, 2, out var f) || f[0] is not Relation rootsRequest ||
            !Fields(rootsRequest, 4, out var r) || !ReadBasis(r[0], out var frame) ||
            !ReadValue(f[1], frame, out var reference)) return Unresolved(request, captured, out witness);
        var completeRoots = TryFractionalPower(rootsRequest, out var roots);
        if (!Fields(roots, 5, out var w) ||
            !Fields(w[2], 2, out var result) || result[1] is not Relation candidateGroup ||
            !candidateGroup.TryReadOrderedGroup(out var candidates)) return Unresolved(request, captured, out witness);
        if (!completeRoots || reference.Any(v => v is null))
            return PendingSelection(request, captured, r[0], roots, candidates, f[1], out witness);
        var distances = new List<IElement>(); var nearest = new List<IElement>(); Scalar? best = null;
        try
        {
            foreach (var candidate in candidates)
            {
                Fields(candidate, 5, out var c); ReadValue(c[0], frame, out var value);
                var dx = value[0]!.Value - reference[0]!.Value; var dy = value[1]!.Value - reference[1]!.Value;
                var squared = dx * dx + dy * dy;
                distances.Add(new Relation(new Relation(c[0], f[1]), squared.Encode(frame.Axes[0])));
                if (best is null || squared < best.Value) { best = squared; nearest.Clear(); }
                if (squared == best) nearest.Add(c[0]);
            }
            var chosen = Group([.. nearest]);
            witness = Observe(request, r[0], Group(roots, Group([.. distances]), chosen),
                nearest.Count == 1 ? [] : [chosen], captured);
            return nearest.Count == 1;
        }
        catch (OverflowException) { return PendingSelection(request, captured, r[0], roots, candidates, f[1], out witness); }
    }

    private static bool PendingSelection(Relation request, IElement captured, IElement basis,
        IElement roots, IElement[] candidates, IElement reference, out Relation witness)
    {
        var values = candidates.Select(c => { Fields(c, 5, out var f); return f[0]; }).ToArray();
        var distances = Group(values.Select(v => (IElement)new Relation(new Relation(v, reference), new Placeholder())).ToArray());
        witness = Observe(request, basis, Group(roots, distances, Group(values)), [roots, reference], captured);
        return false;
    }

    private static bool Count(IElement element, out Carrier axis, out BigInteger count)
    {
        axis = null!; count = 0;
        if (element is not Relation span || !Point(span.Left, out var a) || !Point(span.Right, out var b) ||
            a.Carrier.IsInverse || !a.Carrier.SameIdentityAs(b.Carrier) || a.Ticks != 0) return false;
        axis = a.Carrier; count = b.Ticks; return true;
    }

    private static bool Power(Frame frame, IElement source, Scalar?[] value, BigInteger exponent,
        out Scalar?[] result, out Relation trace)
    {
        result = [Scalar.One, Scalar.Zero]; var steps = new List<IElement>(); trace = Group();
        var zero = value.All(v => v is { IsZero: true });
        if (zero && exponent <= 0) return false;
        var factor = value; IElement factorNode = source, resultNode = Value(frame, result);
        if (exponent < 0)
        {
            var norm = value[0]!.Value * value[0]!.Value + value[1]!.Value * value[1]!.Value;
            factor = [value[0]!.Value / norm, -value[1]!.Value / norm];
            factorNode = Value(frame, factor);
            // Multiplying this inverse by the source reproduces the unit.
            steps.Add(new Relation(Group(frame.Surface, source, factorNode), resultNode));
            exponent = -exponent;
        }
        while (exponent > 0)
        {
            if (!exponent.IsEven)
            {
                result = Product(frame, result, factor);
                var next = Value(frame, result); // Every stored intermediate is range checked.
                steps.Add(new Relation(Group(frame.Surface, resultNode, factorNode), next)); resultNode = next;
            }
            exponent >>= 1;
            if (exponent > 0)
            {
                factor = Product(frame, factor, factor);
                var next = Value(frame, factor);
                steps.Add(new Relation(Group(frame.Surface, factorNode, factorNode), next)); factorNode = next;
            }
        }
        trace = Group([.. steps]); return true;
    }

    private static bool SquareRoots(Scalar?[] value, out Scalar?[][] roots)
    {
        roots = []; var a = value[0]!.Value; var b = value[1]!.Value;
        if (!(a * a + b * b).TrySquareRoot(out var magnitude) ||
            !((magnitude + a) / new Scalar(2)).TrySquareRoot(out var x) ||
            !((magnitude - a) / new Scalar(2)).TrySquareRoot(out var y)) return false;
        if (b < Scalar.Zero) y = -y;
        roots = x.IsZero && y.IsZero ? [[x, y]] : [[x, y], [-x, -y]];
        return true;
    }
}
