using System.Numerics;
using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;

namespace Gojo.Experiments.QualitativeFrieze;

/// <summary>An exact selected square-brush sweep of sampled straight strokes.
/// This is an observational measured interpretation above the qualitative
/// source. Square-nib width is not Euclidean perpendicular line width.</summary>
public sealed class DiagonalRibbonSupplier
{
    // Source = fixture.Surface -> opaque-nib-extent
    // nib = (nib-extent -> (ordered-x-span -> ordered-y-span)) -> Closed(corners)
    // translation = (actual-period -> repetition-span) -> (x-shift-span -> y-shift-span)
    // polygon = ((actual-source-stroke -> translation) -> nib) ->
    //           (Closed(ordered-vertices) -> (Closed(ordered-edges) -> Closed(sums)))
    // sum = actual-source-endpoint -> (actual-nib-corner -> translated-sum-point)
    // interpretation = ((Source -> (trace -> calibration)) ->
    //                   (measured-geometry -> (nib -> (requested-width -> polygon-budget)))) ->
    //                  (actual-period -> (Closed(polygons) -> Closed(unresolved)))
    // Source motion order survives the convex occupied-support read. The square
    // nib's four corners and every endpoint/corner sum remain inspectable; the
    // hull does not claim a recovered path, shared site or conserved material.
    public FriezeFixture Fixture { get; }
    public IElement NibExtent { get; }
    public Relation Surface { get; }
    public sealed record Evaluation(Relation Surface, Relation Source, Relation Trace,
        Relation Calibration, Relation Geometry, Relation Nib, Relation Period,
        Relation Polygons, Relation Unresolved);

    public DiagonalRibbonSupplier(FriezeFixture fixture, IElement nibExtent)
    { Fixture = fixture; NibExtent = nibExtent; Surface = new Relation(fixture.Surface, nibExtent); }

    public Evaluation Interpret(Relation trace, Relation calibration, long width, int polygonBudget = 8192)
    {
        var pending = new List<IElement>(); var polygons = new List<IElement>();
        Relation geometry = StructuralConstraint.Open(), nib = StructuralConstraint.Open(), period = StructuralConstraint.Open();
        var budget = Relation.Segment(new Carrier(), 0, polygonBudget);
        var requestedWidth = new Relation(NibExtent, Relation.Segment(new Carrier(), 0, width));
        try
        {
            if (width <= 0 || polygonBudget <= 0 ||
                !Fixture.TryReadTrace(trace, out _, out _, out _, out _, out _, out var tracePending) || E(tracePending).Length != 0)
            { pending.Add(new Relation(requestedWidth, new Relation(trace, new Relation(calibration, budget)))); return Finish(); }
            geometry = FriezeMeasuredRules.Evaluate(Fixture, trace, calibration);
            if (!FriezeMeasuredRules.TryReadResult(geometry, out _, out _, out _, out _, out _, out var unknown) ||
                E(unknown).Length != 0 || !GeometrySurface(geometry, out var strokes, out var actualPeriod, out _) ||
                !Period(actualPeriod, out var basis, out _, out _))
            { pending.Add(geometry); return Finish(); }
            period = actualPeriod;
            nib = Nib(basis, NibExtent, width);
            var offered = E(strokes);
            if (!Closed(strokes) || offered.Length == 0 || offered.Length > polygonBudget)
            { pending.Add(new Relation(strokes, budget)); return Finish(); }
            foreach (var value in offered)
            {
                if (value is not Relation stroke || !Stroke(stroke, basis, out _, out _, out _, out _))
                { pending.Add(new Relation(value, calibration)); continue; }
                polygons.Add(Polygon(stroke, period, nib, 0));
            }
        }
        catch (OverflowException) { pending.Add(new Relation(requestedWidth, new Relation(Surface, new Relation(calibration, budget)))); }
        return Finish();

        Evaluation Finish()
        {
            var footprints = S(polygons.ToArray()); var unresolved = S(pending.ToArray());
            var read = new Relation(new Relation(new Relation(Surface, new Relation(trace, calibration)),
                    new Relation(geometry, new Relation(nib, new Relation(requestedWidth, budget)))),
                new Relation(period, new Relation(footprints, unresolved)));
            return new(read, Surface, trace, calibration, geometry, nib, period, footprints, unresolved);
        }
    }

    public bool TryReadResult(Relation read, out Relation trace, out Relation calibration,
        out Relation nib, out Relation period, out Relation polygons, out Relation unresolved)
    {
        trace = calibration = nib = period = polygons = unresolved = null!;
        if (read.Left is not Relation { Left: Relation request, Right: Relation detail } || !ReferenceEquals(request.Left, Surface) ||
            request.Right is not Relation { Left: Relation source, Right: Relation selectedCalibration } ||
            detail.Left is not Relation geometry || detail.Right is not Relation { Left: Relation brush, Right: Relation capacity } ||
            capacity.Left is not Relation requestedWidth || !ReferenceEquals(requestedWidth.Left, NibExtent) ||
            requestedWidth.Right is not Relation widthSpan || !Span(widthSpan, out _, out var widthFrom, out var widthTo) ||
            widthFrom != new Q(0) || widthTo.D != 1 || capacity.Right is not Relation budget ||
            !Span(budget, out _, out var budgetFrom, out var budgetTo) || budgetFrom != new Q(0) || budgetTo.D != 1 ||
            read.Right is not Relation { Left: Relation translation, Right: Relation { Left: Relation footprints, Right: Relation pending } } ||
            !Closed(footprints) || !Closed(pending)) return false;
        var entries = E(footprints);
        if (entries.Length != 0)
        {
            if (!Period(translation, out _, out _, out _) || !ReadNib(brush, out _, out _, out var width) || width != widthTo.N ||
                brush.Left is not Relation declaredNib || !ReferenceEquals(declaredNib.Left, NibExtent) ||
                !GeometryContext(geometry, source, selectedCalibration, translation, out var strokes) || entries.Length > budgetTo.N) return false;
            var offered = E(strokes);
            if (entries.Where((entry, i) => entry is not Relation polygon ||
                    !TryReadPolygon(polygon, out var stroke, out var shift, out _, out _) ||
                    i >= offered.Length || !ReferenceEquals(offered[i], stroke) ||
                    shift.Left is not Relation repeats || !ReferenceEquals(repeats.Left, translation) ||
                    polygon.Left is not Relation context || !ReferenceEquals(context.Right, brush)).Any()) return false;
        }
        // Missing footprints cannot be hidden behind an empty failure scope.
        if (E(pending).Length == 0 && (!FriezeMeasuredRules.TryReadResult(geometry, out _, out _, out _, out _, out _, out var unknown) ||
            E(unknown).Length != 0 || !GeometrySurface(geometry, out var actualStrokes, out _, out _) ||
            E(actualStrokes).Length != entries.Length || entries.Length == 0)) return false;
        trace = source; calibration = selectedCalibration; nib = brush; period = translation; polygons = footprints; unresolved = pending; return true;
    }

    private bool GeometryContext(Relation geometry, Relation trace, Relation calibration, Relation period, out Relation strokes)
    {
        strokes = null!;
        if (!Fixture.TryReadTrace(trace, out _, out _, out _, out _, out _, out var tracePending) || E(tracePending).Length != 0 ||
            !GeometrySurface(geometry, out var actualStrokes, out var actualPeriod, out var provenance) ||
            !ReferenceEquals(actualPeriod, period) || provenance.Left is not Relation realized ||
            !FriezeRealization.TryRead(realized, out var actualTrace, out var actualCalibration, out var measured, out var unresolved) ||
            !ReferenceEquals(actualTrace, trace) || !ReferenceEquals(actualCalibration, calibration) || E(unresolved).Length != 0 ||
            !FriezeMeasuredRules.TryReadResult(geometry, out _, out _, out _, out _, out _, out _)) return false;
        var selected = E(actualStrokes); var available = E(measured);
        // This supplier reads the sampled prefix ending at the selected period;
        // source clauses cannot be replaced by equal independently authored ones.
        if (selected.Length == 0 || selected.Length > available.Length ||
            selected.Where((stroke, i) => !ReferenceEquals(stroke, available[i])).Any() ||
            selected[0] is not Relation { Right: Relation first } || selected[^1] is not Relation { Right: Relation last } ||
            !ReferenceEquals(period.Left, first.Left) || !ReferenceEquals(period.Right, last.Right)) return false;
        strokes = actualStrokes; return true;
    }

    private static bool GeometrySurface(Relation geometry, out Relation sampledStrokes, out Relation period, out Relation provenance)
    {
        sampledStrokes = period = provenance = null!;
        // The measured reader preserves raw samples and its nonzero centerline
        // selection separately. A square-brush sweep also admits a point dab.
        if (geometry.Left is not Relation { Left: Relation { Left: Relation source }, Right: Relation periodic } ||
            source.Left is not Relation proof || source.Right is not Relation sampled || !Closed(sampled) ||
            periodic.Left is not Relation actualPeriod) return false;
        sampledStrokes = sampled; period = actualPeriod; provenance = proof; return true;
    }

    /// <summary>Reads and rechecks the exact native convex sweep proof. The
    /// ordered source, all endpoint/corner sums and every boundary edge must be
    /// present. Equal coordinates do not substitute for actual witness identity.</summary>
    public static bool TryReadPolygon(Relation polygon, out Relation sourceStroke, out Relation translation,
        out Relation vertices, out Relation edges)
    {
        sourceStroke = translation = vertices = edges = null!;
        if (polygon.Left is not Relation { Left: Relation provenance, Right: Relation nib } ||
            provenance.Left is not Relation stroke || provenance.Right is not Relation shifted ||
            !ReadNib(nib, out var basis, out var corners, out _) ||
            !ReadTranslation(shifted, basis, out _, out var shift) ||
            !Stroke(stroke, basis, out var first, out var last, out var a, out var b) ||
            polygon.Right is not Relation { Left: Relation points, Right: Relation { Left: Relation boundary, Right: Relation sums } } ||
            !Closed(points) || !Closed(boundary) || !Closed(sums)) return false;
        var clauses = E(sums); if (clauses.Length != 8) return false;
        var sumPoints = new List<Relation>(); var values = new List<V>(); var at = 0;
        foreach (var (endpoint, value) in new[] { (first, a), (last, b) })
            foreach (var corner in corners)
            {
                if (clauses[at++] is not Relation row || !ReferenceEquals(row.Left, endpoint) ||
                    row.Right is not Relation added || !ReferenceEquals(added.Left, corner) || added.Right is not Relation sum ||
                    !Point(sum, out var actual, out var sumBasis) || !SameBasis(basis, sumBasis) ||
                    !Point(corner, out var offset, out _) || actual != value + offset + shift) return false;
                sumPoints.Add(sum); values.Add(actual);
            }
        var hull = Hull(values); var actualPoints = E(points); var actualEdges = E(boundary);
        if (actualPoints.Length != hull.Length || actualEdges.Length != hull.Length || hull.Length < 4) return false;
        for (var i = 0; i < hull.Length; i++)
            if (actualPoints[i] is not Relation point || !sumPoints.Any(sum => ReferenceEquals(sum, point)) ||
                !Point(point, out var value, out _) || value != hull[i] ||
                actualEdges[i] is not Relation edge || !ReferenceEquals(edge.Left, point) ||
                !ReferenceEquals(edge.Right, actualPoints[(i + 1) % hull.Length])) return false;
        sourceStroke = stroke; translation = shifted; vertices = points; edges = boundary; return true;
    }

    /// <summary>Creates an explicitly indexed periodic copy of the same source
    /// stroke. The actual period, signed repetition and resulting translation
    /// remain in the returned native proof. No clipping or union is implied.</summary>
    public static bool TryTranslatePolygon(Relation polygon, long repetitions, out Relation translated)
    {
        translated = null!;
        if (!TryReadPolygon(polygon, out var stroke, out var translation, out _, out _) ||
            translation.Left is not Relation { Left: Relation period } || polygon.Left is not Relation { Right: Relation nib }) return false;
        try { translated = Polygon(stroke, period, nib, repetitions); return true; }
        catch (OverflowException) { return false; }
    }

    public static bool TryReadPoint(Relation point, out long xNumerator, out long xDenominator,
        out long yNumerator, out long yDenominator)
    {
        xNumerator = yNumerator = 0; xDenominator = yDenominator = 1;
        if (!Point(point, out var value, out _)) return false;
        try
        {
            var x = new Q(value.X, 2); var y = new Q(value.Y, 2);
            xNumerator = checked((long)x.N); xDenominator = checked((long)x.D);
            yNumerator = checked((long)y.N); yDenominator = checked((long)y.D); return true;
        }
        catch (OverflowException) { return false; }
    }

    private static Relation Polygon(Relation stroke, Relation period, Relation nib, long repetitions)
    {
        if (!ReadNib(nib, out var basis, out var corners, out _) ||
            !Stroke(stroke, basis, out var first, out var last, out var a, out var b) ||
            !Period(period, out var periodBasis, out var origin, out var end) || !SameBasis(basis, periodBasis)) throw new OverflowException();
        var shift = new V((end.X - origin.X) * repetitions, 0);
        var translation = new Relation(new Relation(period, Relation.Segment(new Carrier(), 0, repetitions)),
            new Relation(NumericSpan(basis.XCarrier, new Q(0), new Q(shift.X, 2)), NumericSpan(basis.YCarrier, new Q(0), new Q(0))));
        var sums = new List<IElement>(); var points = new Dictionary<V, Relation>();
        foreach (var (endpoint, value) in new[] { (first, a), (last, b) })
            foreach (var corner in corners)
            {
                Point(corner, out var offset, out _); var sum = value + offset + shift; var point = NativePoint(basis, sum);
                sums.Add(new Relation(endpoint, new Relation(corner, point))); points.TryAdd(sum, point);
            }
        var hull = Hull(points.Keys); var ordered = hull.Select(value => points[value]).ToArray();
        var edges = ordered.Select((point, i) => (IElement)new Relation(point, ordered[(i + 1) % ordered.Length])).ToArray();
        return new Relation(new Relation(new Relation(stroke, translation), nib),
            new Relation(S(ordered), new Relation(S(edges), S(sums.ToArray()))));
    }

    private static Relation Nib(Basis basis, IElement extent, long width)
    {
        BigInteger halfInDoubledTicks = width;
        var corners = new[] { new V(-halfInDoubledTicks, -halfInDoubledTicks), new V(halfInDoubledTicks, -halfInDoubledTicks),
            new V(halfInDoubledTicks, halfInDoubledTicks), new V(-halfInDoubledTicks, halfInDoubledTicks) };
        return new Relation(new Relation(extent, new Relation(
            NumericSpan(basis.XCarrier, new Q(-halfInDoubledTicks, 2), new Q(halfInDoubledTicks, 2)),
            NumericSpan(basis.YCarrier, new Q(-halfInDoubledTicks, 2), new Q(halfInDoubledTicks, 2)))),
            S(corners.Select(value => (IElement)NativePoint(basis, value)).ToArray()));
    }

    private static bool ReadNib(Relation nib, out Basis basis, out Relation[] corners, out BigInteger width)
    {
        basis = default; corners = []; width = 0;
        if (nib.Left is not Relation { Right: Relation { Left: Relation xSpan, Right: Relation ySpan } } ||
            nib.Right is not Relation points || !Closed(points) || E(points) is not [Relation a, Relation b, Relation c, Relation d] ||
            !Point(a, out var av, out var frame) || !Point(b, out var bv, out var bf) ||
            !Point(c, out var cv, out var cf) || !Point(d, out var dv, out var df) ||
            !SameBasis(frame, bf) || !SameBasis(frame, cf) || !SameBasis(frame, df) ||
            !Span(xSpan, out var xc, out var x0, out var x1) || !Span(ySpan, out var yc, out var y0, out var y1) ||
            !ReferenceEquals(xc, frame.XCarrier) || !ReferenceEquals(yc, frame.YCarrier) || x0.N >= 0 || x1.N <= 0 ||
            x0 != -x1 || y0 != x0 || y1 != x1 || x1.D > 2 || x1.N * 2 % x1.D != 0) return false;
        width = x1.N * 2 / x1.D;
        if (av != new V(-width, -width) || bv != new V(width, -width) || cv != new V(width, width) || dv != new V(-width, width)) return false;
        basis = frame; corners = [a, b, c, d]; return true;
    }

    private static bool ReadTranslation(Relation translation, Basis basis, out Relation period, out V shift)
    {
        period = null!; shift = default;
        if (translation.Left is not Relation { Left: Relation source, Right: Relation repetitions } ||
            !Period(source, out var periodBasis, out var a, out var b) || !SameBasis(basis, periodBasis) ||
            !Span(repetitions, out _, out var from, out var to) || from != new Q(0) || to.D != 1 ||
            translation.Right is not Relation { Left: Relation xSpan, Right: Relation ySpan } ||
            !Span(xSpan, out var xc, out var x0, out var x1) || !Span(ySpan, out var yc, out var y0, out var y1) ||
            !ReferenceEquals(xc, basis.XCarrier) || !ReferenceEquals(yc, basis.YCarrier) ||
            x0 != new Q(0) || y0 != new Q(0) || y1 != new Q(0) || x1 != new Q((b.X - a.X) * to.N, 2)) return false;
        shift = new V((b.X - a.X) * to.N, 0); period = source; return true;
    }

    private static bool Stroke(Relation source, Basis basis, out Relation first, out Relation last, out V a, out V b)
    {
        first = last = null!; a = b = default;
        if (source.Left is not Relation sample || !FriezeFixture.TryReadSample(sample, out _, out _, out _, out _, out _, out _) ||
            source.Right is not Relation { Left: Relation start, Right: Relation end } ||
            !Point(start, out var from, out var af) || !Point(end, out var to, out var bf) ||
            !SameBasis(basis, af) || !SameBasis(basis, bf) || !Whole(from) || !Whole(to)) return false;
        first = start; last = end; a = from; b = to; return true;
    }

    private static bool Period(Relation period, out Basis basis, out V a, out V b)
    {
        basis = default; a = b = default;
        if (period.Left is not Relation first || period.Right is not Relation last ||
            !Point(first, out var from, out var af) || !Point(last, out var to, out var bf) ||
            !SameBasis(af, bf) || !Whole(from) || !Whole(to) || to.X <= from.X || to.Y != from.Y) return false;
        basis = af; a = from; b = to; return true;
    }

    private static bool Point(Relation point, out V value, out Basis basis)
    {
        value = default; basis = default;
        if (point.Left is not Relation { Left: Relation x, Right: var first } ||
            point.Right is not Relation { Left: Relation y, Right: var second } || ReferenceEquals(x, y) ||
            !Coordinate(first, out var xc, out var a) || !Coordinate(second, out var yc, out var b) ||
            a.D > 2 || b.D > 2 || a.N * 2 % a.D != 0 || b.N * 2 % b.D != 0) return false;
        value = new V(a.N * 2 / a.D, b.N * 2 / b.D); basis = new Basis(x, y, xc, yc); return true;
    }
    private static bool Coordinate(IElement coordinate, out Carrier carrier, out Q value)
    {
        carrier = null!; value = new Q(0);
        if (coordinate is CarrierLocation location)
        { carrier = location.Carrier; value = new Q(location.Ticks); }
        else if (coordinate is Relation { Left: CarrierLocation numerator, Right: Relation { Left: CarrierLocation denominator, Right: Carrier c } } &&
            ReferenceEquals(numerator.Carrier, c) && ReferenceEquals(denominator.Carrier, c) && denominator.Ticks > 0)
        { carrier = c; value = new Q(numerator.Ticks, denominator.Ticks); }
        else return false;
        return !carrier.IsUnknown && !carrier.IsInverse;
    }
    private static bool Span(Relation span, out Carrier carrier, out Q a, out Q b)
    {
        carrier = null!; a = b = new Q(0);
        if (span.Left is not Relation { Left: var first, Right: Carrier ac } || span.Right is not Relation { Left: var last, Right: Carrier bc } ||
            !ReferenceEquals(ac, bc) || !Coordinate(first, out var xc, out var from) || !Coordinate(last, out var yc, out var to) ||
            !ReferenceEquals(ac, xc) || !ReferenceEquals(ac, yc)) return false;
        carrier = ac; a = from; b = to; return true;
    }
    private static Relation NativePoint(Basis basis, V value) => new(new Relation(basis.X, NativeCoordinate(basis.XCarrier, new Q(value.X, 2))),
        new Relation(basis.Y, NativeCoordinate(basis.YCarrier, new Q(value.Y, 2))));
    private static IElement NativeCoordinate(Carrier carrier, Q value) => value.D == 1
        ? new CarrierLocation(carrier, checked((long)value.N))
        : new Relation(new CarrierLocation(carrier, checked((long)value.N)), Relation.Point(carrier, checked((long)value.D)));
    private static Relation NumericSpan(Carrier carrier, Q a, Q b) => new(new Relation(NativeCoordinate(carrier, a), carrier), new Relation(NativeCoordinate(carrier, b), carrier));
    private static bool Whole(V value) => value.X.IsEven && value.Y.IsEven;
    private static bool SameBasis(Basis a, Basis b) => ReferenceEquals(a.X, b.X) && ReferenceEquals(a.Y, b.Y) &&
        ReferenceEquals(a.XCarrier, b.XCarrier) && ReferenceEquals(a.YCarrier, b.YCarrier);
    private static V[] Hull(IEnumerable<V> values)
    {
        var points = values.Distinct().OrderBy(point => point.X).ThenBy(point => point.Y).ToArray();
        var lower = new List<V>(); var upper = new List<V>();
        foreach (var point in points)
        { while (lower.Count >= 2 && Cross(lower[^1] - lower[^2], point - lower[^1]) <= 0) lower.RemoveAt(lower.Count - 1); lower.Add(point); }
        foreach (var point in points.Reverse())
        { while (upper.Count >= 2 && Cross(upper[^1] - upper[^2], point - upper[^1]) <= 0) upper.RemoveAt(upper.Count - 1); upper.Add(point); }
        return lower.Take(lower.Count - 1).Concat(upper.Take(upper.Count - 1)).ToArray();
    }
    private static BigInteger Cross(V a, V b) => a.X * b.Y - a.Y * b.X;
    private static Relation S(params IElement[] values) => StructuralConstraint.Closed(values);
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
    private static bool Closed(Relation scope) => StructuralConstraint.TryReadScope(scope, out _, out var seam, out var tail) && seam.Equals(tail);
    private readonly record struct Basis(Relation X, Relation Y, Carrier XCarrier, Carrier YCarrier);
    private readonly record struct V(BigInteger X, BigInteger Y)
    { public static V operator +(V a, V b) => new(a.X + b.X, a.Y + b.Y); public static V operator -(V a, V b) => new(a.X - b.X, a.Y - b.Y); }
    private readonly record struct Q
    {
        public BigInteger N { get; } public BigInteger D { get; }
        public Q(BigInteger n) : this(n, 1) { }
        public Q(BigInteger n, BigInteger d)
        {
            if (d <= 0) throw new ArgumentOutOfRangeException(nameof(d));
            var divisor = BigInteger.GreatestCommonDivisor(BigInteger.Abs(n), d); N = n / divisor; D = d / divisor;
        }
        public static Q operator -(Q value) => new(-value.N, value.D);
        public static bool operator <(Q a, Q b) => a.N * b.D < b.N * a.D;
        public static bool operator >(Q a, Q b) => a.N * b.D > b.N * a.D;
    }
}
