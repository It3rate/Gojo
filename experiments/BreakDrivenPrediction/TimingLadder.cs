using Gojo.Core;
using Gojo.Equations;
using Gojo.Structures;
using C = BreakDrivenPrediction.Continuation;
using N = BreakDrivenPrediction.NativeCircuit;

namespace BreakDrivenPrediction;

/// <summary>
/// Pure, selected timing-resolution reads. Widths are authored clock-tick
/// partitions, not uncertainty intervals or necessarily nested refinements.
/// No observation is consumed and no support or learner state is updated.
/// </summary>
public static class TimingLadder
{
    /// <summary>
    /// Ladder: group(clock, group(authored affine maps)). A map reads width
    /// clock ticks as one cell-index tick. Invalid widths retain an open read.
    /// </summary>
    public static Relation Create(Carrier clock, params long[] widths)
    {
        var basis = clock ?? Carrier.Unknown;
        var maps = (widths ?? []).Select(width =>
            Equation.Affine(basis, 0, width, new Carrier(), 0, 1)).ToArray();
        var root = C.G(basis, C.G(maps.Cast<IElement>().ToArray()));
        return TryLadder(root, out _, out _) ? root : Open(root, root, basis);
    }

    /// <summary>Reads the authored map order; an unresolved ladder has no maps.</summary>
    public static Relation[] Maps(Relation ladder) =>
        TryLadder(ladder, out _, out var maps) ? maps : [];

    /// <summary>
    /// Issue: group(copied issued-at, copied predicted-at, source evidence,
    /// group(rung rows)). Row: group(captured map, full directed clock cell,
    /// projection witness, structural rung). Cells are [lower, upper), with
    /// width one selecting exactly one integer tick. A cell is never clipped
    /// to issuance; assessment separately checks that its event is later.
    /// Numeric map endpoints are captured; the authored map remains provenance.
    /// Negative coordinates and unrepresentable endpoints remain unresolved.
    /// </summary>
    public static Relation Issue(Relation ladder, CarrierLocation issuedAt,
        CarrierLocation predictedAt, IElement sourceEvidence)
    {
        var requested = C.G(issuedAt, predictedAt, sourceEvidence);
        if (!TryLadder(ladder, out var clock, out var maps) ||
            !OnClock(issuedAt, clock) || !OnClock(predictedAt, clock))
            return Open(requested, ladder, clock);
        try
        {
            var issued = C.Copy(issuedAt);
            var predicted = C.Copy(predictedAt);
            var afterIssue = Increment(issued, 1);
            if (!TryCalculate(afterIssue, clock, out var firstFuture, out var futureRead))
                return Open(requested, C.G(ladder, futureRead), clock);
            var nonnegative = OperationClaim.AtLeast(new Carrier(), issued,
                new CarrierLocation(clock, 0), new Carrier());
            var future = OperationClaim.AtLeast(new Carrier(), predicted, firstFuture, new Carrier());
            if (!C.Pass(nonnegative) || !C.Pass(future))
                return Open(requested, C.G(ladder, nonnegative, future), clock);

            // One shared forecast/source, not independent evidence per resolution.
            var source = C.G(issued, predicted, sourceEvidence);
            var rows = new List<IElement>();
            foreach (var authored in maps)
            {
                TryMap(authored, clock, out var width, out var indexAxis);
                var map = Equation.Affine(clock, 0, width.Ticks, indexAxis, 0, 1);
                var projection = Equation.Predict(map, predicted, indexAxis);
                if (!Equation.TryReadPrediction(projection, out var index, out var residue) ||
                    !OnClock(index, indexAxis)) return Open(source, C.G(map, projection), clock);
                var reverse = map.Reverse();
                var lowerRead = Equation.Predict(reverse, index, clock);
                var nextIndex = Increment(index, 1);
                if (!Equation.TryReadPrediction(lowerRead, out var lower, out _) || !OnClock(lower, clock) ||
                    !TryCalculate(nextIndex, indexAxis, out var next, out var nextRead))
                    return Open(source, C.G(map, lowerRead), clock);
                var upperRead = Equation.Predict(reverse, next, clock);
                if (!Equation.TryReadPrediction(upperRead, out var upper, out _) || !OnClock(upper, clock))
                    return Open(source, C.G(map, upperRead), clock);

                var cell = new Relation(new Relation(lower, clock), new Relation(upper, clock));
                var projectionWitness = ReadWitness.Create(new Carrier(), predicted, map, index, residue, projection);
                var correspondence = new Relation(predicted, cell);
                var provenance = C.G(source, authored, projectionWitness,
                    C.G(reverse, lowerRead, nextIndex, nextRead, upperRead),
                    C.G(nonnegative, future, afterIssue, futureRead));
                var focus = FocusSurface.Create(new Carrier(), source, map, predicted,
                    sourceEvidence, cell, correspondence, residue, provenance);
                var rung = StructuralLadderSurface.Rung(new Carrier(), source, focus,
                    cell, correspondence, residue, provenance);
                rows.Add(C.G(map, cell, projectionWitness, rung));
            }
            return C.G(issued, predicted, sourceEvidence, C.G(rows.ToArray()));
        }
        catch (OverflowException) { return Open(requested, ladder, clock); }
    }

    /// <summary>Reads complete issue rows in their authored order.</summary>
    public static Relation[] Rungs(Relation issue) =>
        TryIssue(issue, out _, out _, out var rows) ? rows : [];

    /// <summary>
    /// Assessment: group(issue, copied event-at, group(rows)). Each row is
    /// group(issue rung, event&gt;issued claim, event&gt;=lower claim,
    /// event&lt;upper claim, signed event-predicted, aggregate native evidence).
    /// Strict inequalities use the next/previous integer tick explicitly.
    /// Early/late error and the full cell survive whether or not claims pass.
    /// </summary>
    public static Relation Assess(Relation issue, CarrierLocation eventAt)
    {
        if (!TryIssue(issue, out var issued, out var predicted, out var rungs) ||
            !OnClock(eventAt, issued.Carrier) || eventAt.Ticks < 0)
            return Open(eventAt, issue, issued.Carrier ?? Carrier.Unknown);
        var clock = issued.Carrier;
        try
        {
            var observed = C.Copy(eventAt);
            var nextTick = Increment(issued, 1);
            var errorCircuit = N.Reframe(N.Sum((observed, 1), (predicted, -1)), clock);
            if (!TryCalculate(nextTick, clock, out var firstFuture, out var nextRead) ||
                !TryCalculate(errorCircuit, clock, out var error, out var errorRead))
                return Open(observed, issue, clock);
            var causal = OperationClaim.AtLeast(new Carrier(), observed, firstFuture, new Carrier());
            var rows = new List<IElement>();
            foreach (var rung in rungs)
            {
                TryRung(rung, clock, out var lower, out var upper);
                var priorTick = Increment(upper, -1);
                if (!TryCalculate(priorTick, clock, out var lastInCell, out var priorRead))
                    return Open(observed, C.G(issue, priorRead), clock);
                var lowerClaim = OperationClaim.AtLeast(new Carrier(), observed, lower, new Carrier());
                var upperClaim = OperationClaim.AtMost(new Carrier(), observed, lastInCell, new Carrier());
                var evidence = C.G(new Relation(nextTick, nextRead), new Relation(priorTick, priorRead),
                    new Relation(errorCircuit, errorRead), C.G(causal, lowerClaim, upperClaim));
                rows.Add(C.G(rung, causal, lowerClaim, upperClaim, error, evidence));
            }
            return C.G(issue, observed, C.G(rows.ToArray()));
        }
        catch (OverflowException) { return Open(eventAt, issue, clock); }
    }

    /// <summary>Only resolved, aligned causal and cell claims can match.</summary>
    public static bool Matches(Relation row)
    {
        if (!TryDifference(row, out var difference)) return false;
        var p = C.Parts(row);
        return p.Skip(1).Take(3).All(element => element is Relation claim &&
            OperationClaim.TryRead(claim, out var surface, out _, out _, out _, out _, out _) &&
            surface.Left is CarrierLocation actual && OnClock(actual, difference.Carrier) &&
            surface.Right is CarrierLocation expected && OnClock(expected, difference.Carrier) && C.Pass(claim));
    }

    /// <summary>
    /// Reads signed error even when a valid row does not match. On a malformed
    /// row the returned coordinate is unbound (default); use TryDifference to
    /// distinguish that failure before accessing its ticks.
    /// </summary>
    public static CarrierLocation Difference(Relation row) =>
        TryDifference(row, out var difference) ? difference : default;

    public static bool TryDifference(Relation row, out CarrierLocation difference)
    {
        var p = C.Parts(row);
        if (p.Length == 6 && p[0] is Relation rung && p[4] is CarrierLocation value &&
            value.Carrier is not null && OnClock(value, value.Carrier) &&
            p[1] is Relation && p[2] is Relation && p[3] is Relation && p[5] is Relation &&
            TryRung(rung, value.Carrier, out _, out _))
        { difference = value; return true; }
        difference = default;
        return false;
    }

    private static bool TryLadder(Relation ladder, out Carrier clock, out Relation[] maps)
    {
        clock = Carrier.Unknown;
        maps = [];
        if (C.Parts(ladder) is not { Length: 2 } p || p[0] is not Carrier axis || axis.IsUnknown ||
            p[1] is not Relation frame) return false;
        clock = axis;
        if (!frame.TryReadOrderedGroup(out var parts)) return false;
        if (parts.Any(element => element is not Relation map || !TryMap(map, axis, out _, out _))) return false;
        maps = parts.Cast<Relation>().ToArray();
        return true;
    }

    private static bool TryMap(Relation map, Carrier clock, out CarrierLocation width, out Carrier index)
    {
        width = default;
        index = Carrier.Unknown;
        if (map.Left is not Relation source || map.Right is not Relation target ||
            !TrySpan(source, out var a, out var b) ||
            !TrySpan(target, out var c, out var d) ||
            !OnClock(a, clock) || !OnClock(b, clock) || a.Ticks != 0 || b.Ticks <= 0 ||
            c.Carrier is null || !OnClock(c, c.Carrier) || !OnClock(d, c.Carrier) ||
            c.Ticks != 0 || d.Ticks != 1) return false;
        width = b;
        index = c.Carrier;
        return true;
    }

    private static bool TryIssue(Relation issue, out CarrierLocation issued,
        out CarrierLocation predicted, out Relation[] rows)
    {
        issued = predicted = default;
        rows = [];
        if (C.Parts(issue) is not { Length: 4 } p || p[0] is not CarrierLocation a ||
            p[1] is not CarrierLocation b || a.Carrier is null || !OnClock(a, a.Carrier) ||
            !OnClock(b, a.Carrier) || a.Ticks < 0 || b.Ticks <= a.Ticks || p[3] is not Relation group)
            return false;
        if (!group.TryReadOrderedGroup(out var parts)) return false;
        if (parts.Any(element => element is not Relation row || !TryRung(row, a.Carrier, out _, out _))) return false;
        foreach (var row in parts.Cast<Relation>())
        {
            var rung = (Relation)C.Parts(row)[3];
            if (!StructuralLadderSurface.TryReadRung(rung, out var source, out _, out _, out _, out _, out _, out _, out _) ||
                source is not Relation forecast || C.Parts(forecast) is not { Length: 3 } f ||
                f[0] is not CarrierLocation from || !from.SameTickRefAs(a) ||
                f[1] is not CarrierLocation to || !to.SameTickRefAs(b)) return false;
        }
        issued = a;
        predicted = b;
        rows = parts.Cast<Relation>().ToArray();
        return true;
    }

    private static bool TryRung(Relation row, Carrier clock, out CarrierLocation lower, out CarrierLocation upper)
    {
        lower = upper = default;
        if (C.Parts(row) is not { Length: 4 } p || p[0] is not Relation map ||
            !TryMap(map, clock, out _, out var index) || p[1] is not Relation cell ||
            !TrySpan(cell, out var a, out var b) ||
            !OnClock(a, clock) || !OnClock(b, clock) || a.Ticks < 0 || b.Ticks <= a.Ticks ||
            p[2] is not Relation witness || !Equation.TryReadPredictionWitness(witness,
                out var selected, out var readMap, out var result, out _, out _) ||
            selected is not CarrierLocation prediction || !OnClock(prediction, clock) ||
            !ReferenceEquals(readMap, map) || !OnClock(result, index) || p[3] is not Relation rung ||
            !StructuralLadderSurface.TryReadRung(rung, out var source, out _, out var rungResult,
                out _, out _, out _, out _, out _) || !ReferenceEquals(rungResult, cell) ||
            source is not Relation forecast || C.Parts(forecast) is not { Length: 3 } f ||
            f[0] is not CarrierLocation issued || !OnClock(issued, clock) ||
            f[1] is not CarrierLocation predicted || !OnClock(predicted, clock) ||
            !predicted.SameTickRefAs(prediction)) return false;
        lower = a;
        upper = b;
        return true;
    }

    private static Relation Increment(CarrierLocation value, long amount) =>
        N.Reframe(N.Sum((value, 1), (N.Constant(amount), 1)), value.Carrier);

    private static bool TrySpan(Relation span, out CarrierLocation start, out CarrierLocation end)
    {
        start = end = default;
        if (span.Left is not Relation { Left: CarrierLocation a, Right: Carrier ac } ||
            span.Right is not Relation { Left: CarrierLocation b, Right: Carrier bc } ||
            !OnClock(a, ac) || !OnClock(b, bc) || !ReferenceEquals(ac, bc)) return false;
        start = a;
        end = b;
        return true;
    }

    private static bool TryCalculate(Relation circuit, Carrier expected,
        out CarrierLocation value, out Relation read)
    {
        read = N.Read(circuit);
        return Equation.TryReadPrediction(read, out value, out _) && OnClock(value, expected);
    }

    private static bool OnClock(CarrierLocation value, Carrier clock) =>
        value.Carrier is not null && !value.Carrier.IsUnknown && value.Tick is not null &&
        ReferenceEquals(value.Carrier, clock);

    private static Relation Open(IElement input, IElement surface, Carrier clock) =>
        Relation.UnresolvedRead(input, surface, clock);
}
