using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;

namespace Gojo.Experiments.QualitativeFrieze;

/// <summary>A selected measured interpretation of an unchanged qualitative
/// trace. Calibration is separate native structure; source motion and event
/// identity survive. Coordinates and distances are not claims of source truth.</summary>
public static class FriezeRealization
{
    // calibration = (motion-frame -> Closed(axis -> numeric-carrier)) ->
    //               Closed((axis -> reach) -> numeric-span).
    // numeric-point = (horizontal-axis -> L(x,n)) -> (vertical-axis -> L(y,n)).
    // read = ((trace -> calibration) -> motion-frame) ->
    //        (Closed(sample -> (before-point -> after-point)) ->
    //         (Closed(unresolved) -> Closed(calibration-checks))).

    public static IReadOnlyDictionary<IElement, long> DefaultLengths(FriezeFixture fixture,
        long horizontal = 12, long vertical = 20)
    {
        if (horizontal < 4 || vertical <= 0) throw new ArgumentOutOfRangeException(nameof(horizontal));
        var lengths = new Dictionary<IElement, long>(ReferenceEqualityComparer.Instance);
        var roles = Roles(fixture);
        foreach (var (axis, reach) in roles)
            lengths.TryAdd(reach, Same(axis, fixture.Frame.Horizontal) ? horizontal : vertical);
        // The native strict order, rather than a fixture name, selects which
        // shared horizontal reach is short and which is long.
        foreach (var item in E(fixture.ReachOrder))
        {
            if (item is not Relation order) continue;
            var a = roles.Where(role => Same(role.Reach, order.Left)).ToArray();
            var b = roles.Where(role => Same(role.Reach, order.Right)).ToArray();
            if (a.Length != 1 || b.Length != 1 || !Same(a[0].Axis, b[0].Axis)) continue;
            var basis = Same(a[0].Axis, fixture.Frame.Horizontal) ? horizontal : vertical;
            lengths[order.Left] = Math.Max(1, basis / 2);
            lengths[order.Right] = basis;
        }
        return lengths;
    }

    /// <summary>Builds candidate calibration, including missing or inadmissible
    /// clauses so TryRealize can retain their failure instead of inventing zero.</summary>
    public static Relation CreateCalibration(FriezeFixture fixture, IReadOnlyDictionary<IElement, long> reachLengths)
    {
        var x = new Carrier(); var y = new Carrier();
        var axes = S(new Relation(fixture.Frame.Horizontal, x), new Relation(fixture.Frame.Vertical, y));
        var mappings = new List<IElement>();
        var roles = Roles(fixture);
        foreach (var (axis, reach) in roles)
        {
            var supplied = reachLengths.Where(pair => Same(pair.Key, reach)).ToArray();
            IElement span = supplied.Length == 1
                ? Relation.Segment(Same(axis, fixture.Frame.Horizontal) ? x : y, 0, supplied[0].Value)
                : new Placeholder();
            mappings.Add(new Relation(new Relation(axis, reach), span));
        }
        foreach (var extra in reachLengths.Where(pair => !roles.Any(role => Same(role.Reach, pair.Key))))
            mappings.Add(new Relation(new Relation(new Placeholder(), extra.Key), Relation.Segment(x, 0, extra.Value)));
        return new(new Relation(fixture.Frame.Surface, axes), S(mappings.ToArray()));
    }

    public static bool TryRealize(FriezeFixture fixture, Relation trace, Relation calibration, out Relation witness)
    {
        var pending = new List<IElement>(); var checks = new List<IElement>(); var points = new List<IElement>();
        if (!fixture.TryReadTrace(trace, out _, out _, out _, out _, out var samples, out _) ||
            !Calibration(fixture, calibration, out var axes, out var lengths, checks, pending))
        { pending.Add(new Relation(trace, calibration)); witness = Result(); return false; }
        foreach (var value in E(samples))
        {
            if (value is not Relation sample || !FriezeFixture.TryReadSample(sample, out _, out _,
                out var before, out var after, out _, out _) ||
                !Point(before, out var first) || !Point(after, out var last))
            { pending.Add(new Relation(value, calibration)); continue; }
            points.Add(new Relation(sample, new Relation(first, last)));
        }
        witness = Result(); return pending.Count == 0;

        bool Point(Relation displacement, out Relation point)
        {
            point = null!;
            if (!Closed(displacement)) return false;
            long x = 0, y = 0;
            try
            {
                foreach (var term in E(displacement))
                {
                    if (term is not Relation move || !fixture.Frame.Direction(move, out var axis, out var forward, out var reach)) return false;
                    var matches = lengths.Where(row => Same(row.Axis, axis) && Same(row.Reach, reach)).ToArray();
                    if (matches.Length != 1) return false;
                    var delta = forward ? matches[0].Length : checked(-matches[0].Length);
                    if (Same(axis, fixture.Frame.Horizontal)) x = checked(x + delta);
                    else if (Same(axis, fixture.Frame.Vertical)) y = checked(y + delta);
                    else return false;
                }
            }
            catch (OverflowException) { return false; }
            point = new(new Relation(fixture.Frame.Horizontal, new CarrierLocation(axes[0], x)),
                new Relation(fixture.Frame.Vertical, new CarrierLocation(axes[1], y)));
            return true;
        }
        Relation Result() => new(new Relation(new Relation(trace, calibration), fixture.Frame.Surface),
            new Relation(S(points.ToArray()), new Relation(S(pending.ToArray()), S(checks.ToArray()))));
    }

    public static bool TryRead(Relation witness, out Relation trace, out Relation calibration,
        out Relation samplePoints, out Relation unresolved)
    {
        trace = calibration = samplePoints = unresolved = null!;
        if (witness.Left is not Relation { Left: Relation { Left: Relation source, Right: Relation basis }, Right: Relation frame } ||
            basis.Left is not Relation { Left: var selected } || !Same(selected, frame) ||
            witness.Right is not Relation { Left: Relation points, Right: Relation { Left: Relation pending, Right: Relation checks } } ||
            !Closed(points) || !Closed(pending) || !Closed(checks)) return false;
        trace = source; calibration = basis; samplePoints = points; unresolved = pending; return true;
    }

    public static bool TryReadSamplePoints(Relation clause, out Relation sample,
        out Relation before, out Relation after)
    {
        sample = before = after = null!;
        if (clause.Left is not Relation source || clause.Right is not Relation { Left: Relation first, Right: Relation last } ||
            !FriezeFixture.TryReadSample(source, out _, out _, out _, out _, out _, out _) ||
            !TryReadPoint(first, out _, out _) || !TryReadPoint(last, out _, out _)) return false;
        sample = source; before = first; after = last; return true;
    }

    public static bool TryReadPoint(Relation point, out long x, out long y)
    {
        x = y = 0;
        if (point.Left is not Relation { Left: Relation a, Right: CarrierLocation first } ||
            point.Right is not Relation { Left: Relation b, Right: CarrierLocation second } || Same(a, b)) return false;
        x = first.Ticks; y = second.Ticks; return true;
    }

    private static bool Calibration(FriezeFixture fixture, Relation calibration,
        out Carrier[] axes, out List<(Relation Axis, IElement Reach, long Length)> lengths,
        List<IElement> checks, List<IElement> pending)
    {
        axes = []; lengths = [];
        if (calibration.Left is not Relation { Left: var frame, Right: Relation axisScope } ||
            !Same(frame, fixture.Frame.Surface) || !Closed(axisScope) ||
            calibration.Right is not Relation mapping || !Closed(mapping)) return false;
        Relation[] declaredAxes = [fixture.Frame.Horizontal, fixture.Frame.Vertical];
        var boundAxes = E(axisScope);
        if (boundAxes.Length != declaredAxes.Length) return false;
        var carriers = new List<Carrier>();
        foreach (var axis in declaredAxes)
        {
            var matches = boundAxes.OfType<Relation>().Where(row => Same(row.Left, axis)).ToArray();
            if (matches.Length != 1 || matches[0].Right is not Carrier carrier || carrier.IsUnknown || carrier.IsInverse) return false;
            carriers.Add(carrier);
        }
        axes = carriers.ToArray();
        var expected = Roles(fixture);
        var clauses = E(mapping);
        foreach (var clause in clauses)
        {
            if (clause is not Relation { Left: Relation role, Right: Relation span } ||
                !expected.Any(pair => Same(pair.Axis, role.Left) && Same(pair.Reach, role.Right)) ||
                !Span(span, out var carrier, out var from, out var to) || from != 0 || to <= 0 ||
                !ReferenceEquals(carrier, axes[Same(role.Left, fixture.Frame.Horizontal) ? 0 : 1]) ||
                lengths.Any(row => Same(row.Axis, role.Left) && Same(row.Reach, role.Right)))
            { pending.Add(clause); continue; }
            lengths.Add(((Relation)role.Left, role.Right, to)); checks.Add(clause);
        }
        foreach (var role in expected)
            if (!lengths.Any(row => Same(row.Axis, role.Axis) && Same(row.Reach, role.Reach)))
                pending.Add(new Relation(role.Axis, role.Reach));
        foreach (var value in E(fixture.ReachOrder))
        {
            if (value is not Relation order) { pending.Add(value); continue; }
            var lower = lengths.Where(row => Same(row.Reach, order.Left)).ToArray();
            var upper = lengths.Where(row => Same(row.Reach, order.Right)).ToArray();
            var evidence = new Relation(order, mapping);
            if (lower.Length != 1 || upper.Length != 1 || !Same(lower[0].Axis, upper[0].Axis) || lower[0].Length >= upper[0].Length)
                pending.Add(evidence);
            else checks.Add(evidence);
        }
        return pending.Count == 0;
    }

    private static List<(Relation Axis, IElement Reach)> Roles(FriezeFixture fixture)
    {
        var roles = new List<(Relation Axis, IElement Reach)>();
        if (!FiniteLaw.TryRead(fixture.OutputLaw, out _, out _, out _, out var rows)) return roles;
        foreach (var row in rows)
        {
            if (row.Right is not Relation support || !Closed(support)) continue;
            foreach (var item in E(support))
            {
                if (item is not Relation word || !Closed(word)) continue;
                foreach (var term in E(word))
                    if (term is Relation move && fixture.Frame.Direction(move, out var axis, out _, out var reach) &&
                        !roles.Any(role => Same(role.Axis, axis) && Same(role.Reach, reach))) roles.Add((axis, reach));
            }
        }
        return roles;
    }

    private static bool Span(Relation span, out Carrier carrier, out long from, out long to)
    {
        carrier = null!; from = to = 0;
        if (span.Left is not Relation { Left: CarrierLocation start, Right: Carrier a } ||
            span.Right is not Relation { Left: CarrierLocation end, Right: Carrier b } ||
            !ReferenceEquals(a, b) || !ReferenceEquals(start.Carrier, a) || !ReferenceEquals(end.Carrier, b)) return false;
        carrier = a; from = start.Ticks; to = end.Ticks; return true;
    }

    private static bool Same(IElement a, IElement b) => ReferenceEquals(a, b) || a is Placeholder p && b is Placeholder q && p.Equals(q);
    private static bool Closed(Relation scope) => StructuralConstraint.TryReadScope(scope, out _, out var a, out var b) && a.Equals(b);
    private static Relation S(params IElement[] values) => StructuralConstraint.Closed(values);
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
}
