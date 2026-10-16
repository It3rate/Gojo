using System.Numerics;
using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;

namespace Gojo.Experiments.QualitativeFrieze;

/// <summary>Exact, observational rules for one explicitly periodic ink cell.
/// A contact is not material connectivity. Sampled ink support and the ordered
/// motion history are different reads; this reader keeps their correspondence.
/// A crossing site has two noncollinear straight-through support families.
/// Bent path continuations require a separate read; unpaired branches remain
/// exposed even when a site also has straight-through crossing support.</summary>
public static class FriezeMeasuredRules
{
    // read = ((provenance -> strokes) -> (period-translation ->
    //        (fundamental-span -> (height-span -> work-budget)))) ->
    //        (overlaps -> (crossings -> (contacts -> (symmetries -> unresolved))))
    // pair witness = ((actual-stroke -> actual-stroke) -> translation-span) ->
    //                intersection-geometry
    // point site = (Closed(actual-pair-hits) -> canonical-point) ->
    //              (Closed(opposed-ray-families) -> Closed(unpaired-rays))
    // ray = (actual-stroke -> translation-span) -> (canonical-point -> endpoint)
    // opposed family = Closed(forward-rays) -> Closed(opposed-rays)
    // symmetry = (ordered-x/y-calibrations -> strokes) -> (coverage -> unmatched)
    // coverage = (selected-stroke -> compared-support) ->
    //            (transformed-stroke -> Closed(source/shift/covered-fragments))
    public sealed record Summary(long PeriodX, long PeriodY, long FundamentalPeriodX, long Height,
        int StrokeCount, int OverlapCount, int CrossingCount, int ContactCount,
        string[] VerifiedSymmetryNames, int UnresolvedCount);

    public static Relation Evaluate(FriezeFixture fixture, Relation trace, Relation calibration,
        int comparisonBudget = 100_000)
    {
        var proof = new Relation(trace, calibration);
        if (!fixture.TryReadTrace(trace, out var initial, out _, out var steps, out _, out _, out _) ||
            !FriezeRealization.TryRealize(fixture, trace, calibration, out var realized) ||
            !FriezeRealization.TryRead(realized, out _, out _, out var clauses, out _)) return Failed(proof);
        var causes = new HashSet<IElement>(ReferenceEqualityComparer.Instance);
        var found = false;
        foreach (var value in E(steps))
        {
            if (value is not Relation receipt || !fixture.Network.TryReadStep(receipt, out _, out _, out var after,
                out var accepted, out var blocked, out var pending) || E(blocked).Length != 0 || E(pending).Length != 0) return Failed(proof);
            causes.Add(receipt); foreach (var child in E(accepted)) causes.Add(child);
            if (!fixture.SamePhaseConfiguration(initial, after)) continue;
            found = true; break;
        }
        var selected = E(clauses).OfType<Relation>().Where(clause =>
            FriezeRealization.TryReadSamplePoints(clause, out var sample, out _, out _) &&
            FriezeFixture.TryReadSample(sample, out var cause, out _, out _, out _, out _, out _) && causes.Contains(cause)).ToArray();
        if (!found || selected.Length == 0 ||
            !FriezeRealization.TryReadSamplePoints(selected[0], out _, out var start, out _) ||
            !FriezeRealization.TryReadSamplePoints(selected[^1], out _, out _, out var end)) return Failed(proof);
        return EvaluateStrokes(S(selected), new Relation(start, end), new Relation(realized, S(causes.ToArray())), comparisonBudget);
    }

    /// <summary>Checks supplied measured strokes under an explicit periodic
    /// construction premise. Source coordinates select whole calibrated ticks
    /// on distinct axes with known forward carriers. Nonwhole source coordinates
    /// remain unresolved; intersections and transforms retain exact fractions.
    /// Finite strokes alone do not establish repetition.</summary>
    public static Relation EvaluateStrokes(Relation strokes, Relation periodTranslation,
        IElement provenance, int comparisonBudget = 100_000)
    {
        if (comparisonBudget <= 0) throw new ArgumentOutOfRangeException(nameof(comparisonBudget));
        var overlaps = new List<IElement>(); var crossings = new List<IElement>(); var contacts = new List<IElement>();
        var symmetries = new List<IElement>(); var unresolved = new List<IElement>();
        var admitted = new List<Stroke>();
        var sites = new Dictionary<V, PointSite>();
        var budget = Relation.Segment(new Carrier(), 0, comparisonBudget);
        Relation fundamental = S(), height = S(); var work = comparisonBudget;
        try
        {
            if (!Closed(strokes) || periodTranslation.Left is not Relation first || periodTranslation.Right is not Relation last ||
                !Point(first, out var origin, out var xAxis, out var yAxis, out var xCarrier, out var yCarrier) ||
                !Point(last, out var end, out var bx, out var by, out var bc, out var bd) ||
                !ReferenceEquals(xAxis, bx) || !ReferenceEquals(yAxis, by) || !ReferenceEquals(xCarrier, bc) || !ReferenceEquals(yCarrier, bd) ||
                end.X <= origin.X || end.Y != origin.Y)
            { unresolved.Add(periodTranslation); return Result(); }
            var period = end.X - origin.X;
            if (!TryInteger(period, out _)) { unresolved.Add(periodTranslation); return Result(); }
            foreach (var item in E(strokes))
            {
                if (item is not Relation clause || clause.Right is not Relation { Left: Relation a, Right: Relation b } ||
                    !Point(a, out var p, out var ax, out var ay, out var ac, out var ad) ||
                    !Point(b, out var q, out var cx, out var cy, out var cc, out var cd) ||
                    !ReferenceEquals(ax, xAxis) || !ReferenceEquals(ay, yAxis) || !ReferenceEquals(cx, xAxis) || !ReferenceEquals(cy, yAxis) ||
                    !ReferenceEquals(ac, xCarrier) || !ReferenceEquals(ad, yCarrier) || !ReferenceEquals(cc, xCarrier) || !ReferenceEquals(cd, yCarrier))
                { unresolved.Add(item); continue; }
                if (p != q) admitted.Add(new(clause, p, q));
            }
            if (unresolved.Count != 0 || admitted.Count == 0) { unresolved.Add(strokes); return Result(); }
            var minimumY = admitted.Min(stroke => Min(stroke.A.Y, stroke.B.Y));
            var maximumY = admitted.Max(stroke => Max(stroke.A.Y, stroke.B.Y));
            height = Span(yCarrier, 0, maximumY - minimumY);
            for (var i = 0; i < admitted.Count; i++)
                for (var j = i; j < admitted.Count; j++)
                    foreach (var shift in Shifts(admitted[i], admitted[j], period))
                    {
                        if (i == j && shift <= 0) continue;
                        Spend();
                        var b = admitted[j].Shift(period * shift);
                        var pair = new Relation(new Relation(admitted[i].Source, admitted[j].Source), Span(xCarrier, 0, period * shift));
                        var hit = Intersection(admitted[i], b);
                        if (hit is null) continue;
                        if (hit.Value.A != hit.Value.B)
                            overlaps.Add(new Relation(pair, Segment(hit.Value.A, hit.Value.B)));
                        else
                        {
                            var site = NativePoint(hit.Value.A);
                            var incidence = new Relation(site, new Relation(Incidence(admitted[i], hit.Value.A), Incidence(b, hit.Value.A)));
                            var point = new V(origin.X + Mod(hit.Value.A.X - origin.X, period), hit.Value.A.Y);
                            if (!sites.TryGetValue(point, out var support)) sites.Add(point, support = new());
                            support.PairHits.Add(new Relation(pair, incidence));
                            var offset = point.X - hit.Value.A.X;
                            AddSupport(admitted[i], offset); AddSupport(admitted[j], period * shift + offset);

                            void AddSupport(Stroke source, Q translation)
                            {
                                foreach (var present in support.Supports)
                                {
                                    Spend();
                                    if (ReferenceEquals(present.Source.Source, source.Source) && present.Translation == translation) return;
                                }
                                support.Supports.Add(new(source, translation));
                            }
                        }
                    }

            // Pointwise pair classification depends on where a stroke was
            // sampled. Aggregate support at the actual quotient site instead:
            // two split collinear halves supply the same opposed rays as one
            // unsplit stroke, including across the periodic seam.
            foreach (var (point, support) in sites)
            {
                var anchor = NativePoint(point); var families = new Dictionary<V, RayFamily>();
                foreach (var source in support.Supports)
                {
                    Spend(); var translated = source.Source.Shift(source.Translation);
                    foreach (var endpoint in new[] { translated.A, translated.B })
                    {
                        if (endpoint == point) continue;
                        Spend(); var delta = endpoint - point;
                        var line = delta.X != 0 ? new V(1, delta.Y / delta.X) : new V(0, 1);
                        var forward = delta.X != 0 ? delta.X > 0 : delta.Y > 0;
                        if (!families.TryGetValue(line, out var family)) families.Add(line, family = new());
                        var ray = new Relation(new Relation(source.Source.Source, Span(xCarrier, 0, source.Translation)),
                            new Relation(anchor, NativePoint(endpoint)));
                        (forward ? family.Forward : family.Opposed).Add(ray);
                    }
                }
                var through = new List<IElement>(); var unpaired = new List<IElement>();
                foreach (var family in families.Values)
                {
                    Spend();
                    if (family.Forward.Count != 0 && family.Opposed.Count != 0)
                        through.Add(new Relation(S(family.Forward.ToArray()), S(family.Opposed.ToArray())));
                    else { unpaired.AddRange(family.Forward); unpaired.AddRange(family.Opposed); }
                }
                var witness = new Relation(new Relation(S(support.PairHits.ToArray()), anchor),
                    new Relation(S(through.ToArray()), S(unpaired.ToArray())));
                (through.Count >= 2 ? crossings : contacts).Add(witness);
            }

            // Candidate translations come from endpoint correspondences; every
            // candidate must preserve full stroke coverage, including seams.
            var candidates = new SortedSet<Q> { period };
            foreach (var a in admitted.SelectMany(stroke => new[] { stroke.A, stroke.B }))
                foreach (var b in admitted.SelectMany(stroke => new[] { stroke.A, stroke.B }))
                {
                    var candidate = Mod(b.X - a.X, period);
                    if (candidate > 0) candidates.Add(candidate);
                }
            var fundamentalPeriod = period;
            // A set of complete horizontal lines has continuous translation
            // symmetry, not a unique smallest positive period. Keep its supplied
            // construction period rather than deriving one from segmentation.
            var continuous = admitted.All(stroke => stroke.A.Y == stroke.B.Y) &&
                admitted.GroupBy(stroke => stroke.A.Y).All(group => Covered(
                    new Stroke(strokes, new(origin.X, group.Key), new(origin.X + period, group.Key)), group.ToList(), period, out _, out _));
            if (!continuous)
                foreach (var candidate in candidates)
                {
                    if (candidate >= fundamentalPeriod) break;
                    if (Preserves(1, 1, candidate, 0, out _, out _)) { fundamentalPeriod = candidate; break; }
                }
            fundamental = Span(xCarrier, 0, fundamentalPeriod);
            var centerY2 = minimumY + maximumY;
            var vertices = admitted.SelectMany(stroke => new[] { stroke.A, stroke.B }).Distinct().ToArray();
            var mirrorCenters = new SortedSet<Q>(); var turnCenters = new SortedSet<Q>();
            foreach (var a in vertices)
                foreach (var b in vertices)
                {
                    if (a.Y == b.Y) mirrorCenters.Add(Mod(a.X + b.X, period));
                    if (a.Y + b.Y == centerY2) turnCenters.Add(Mod(a.X + b.X, period));
                }
            // Native maps specify the operations. Names exist only in Describe.
            Test(1, 1, fundamentalPeriod, 0);
            foreach (var center in mirrorCenters) if (Test(-1, 1, center, 0)) break;
            Test(1, -1, 0, centerY2);
            foreach (var center in turnCenters) if (Test(-1, -1, center, centerY2)) break;
            Test(1, -1, fundamentalPeriod / 2, centerY2);
            return Result();

            bool Test(int sx, int sy, Q tx, Q ty)
            {
                var success = Preserves(sx, sy, tx, ty, out var proof, out var missing);
                var operation = new Relation(new Relation(Span(xCarrier, 0, 1), Span(xCarrier, tx, tx + sx)),
                    new Relation(Span(yCarrier, 0, 1), Span(yCarrier, ty, ty + sy)));
                symmetries.Add(new Relation(new Relation(operation, strokes), new Relation(S(proof.ToArray()), S(missing.ToArray()))));
                return success;
            }
            bool Preserves(int sx, int sy, Q tx, Q ty, out List<IElement> proofs, out List<IElement> missing)
            {
                var evidence = new List<IElement>(); var absent = new List<IElement>();
                proofs = evidence; missing = absent;
                var mapped = admitted.Select(stroke => new Stroke(stroke.Source,
                    new(stroke.A.X * sx + tx, stroke.A.Y * sy + ty), new(stroke.B.X * sx + tx, stroke.B.Y * sy + ty))).ToList();
                Check(mapped, admitted); Check(admitted, mapped);
                return absent.Count == 0;
                void Check(List<Stroke> selected, List<Stroke> offered)
                {
                    foreach (var stroke in selected)
                    {
                        var complete = Covered(stroke, offered, period, out var pieces, out var gaps);
                        var read = new Relation(new Relation(stroke.Source, S(offered.Select(other => (IElement)other.Source).ToArray())),
                            new Relation(Segment(stroke.A, stroke.B), S(pieces.ToArray())));
                        evidence.Add(read);
                        if (!complete) foreach (var gap in gaps) absent.Add(new Relation(read, Segment(gap.A, gap.B)));
                    }
                }
            }
            bool Covered(Stroke target, List<Stroke> offered, Q wrap, out List<IElement> pieces, out List<(V A, V B)> gaps)
            {
                pieces = []; gaps = [];
                var intervals = new List<(Q A, Q B)>();
                foreach (var source in offered)
                    foreach (var shift in Shifts(target, source, wrap))
                    {
                        Spend(); var translated = source.Shift(wrap * shift);
                        if (Cross(target.B - target.A, translated.A - target.A) != 0 ||
                            Cross(target.B - target.A, translated.B - target.A) != 0) continue;
                        var a = Parameter(target, translated.A); var b = Parameter(target, translated.B);
                        var lo = Max(0, Min(a, b)); var hi = Min(1, Max(a, b));
                        if (lo >= hi) continue;
                        intervals.Add((lo, hi));
                        pieces.Add(new Relation(new Relation(source.Source, Span(xCarrier, 0, wrap * shift)),
                            Segment(target.A + (target.B - target.A) * lo, target.A + (target.B - target.A) * hi)));
                    }
                Q cursor = 0;
                foreach (var interval in intervals.OrderBy(interval => interval.A))
                {
                    if (interval.A > cursor) gaps.Add((target.A + (target.B - target.A) * cursor, target.A + (target.B - target.A) * interval.A));
                    cursor = Max(cursor, interval.B);
                }
                if (cursor < 1) gaps.Add((target.A + (target.B - target.A) * cursor, target.B));
                return gaps.Count == 0;
            }
            Relation NativePoint(V point) => new(new Relation(xAxis, Coordinate(xCarrier, point.X)), new Relation(yAxis, Coordinate(yCarrier, point.Y)));
            Relation Segment(V a, V b) => new(NativePoint(a), NativePoint(b));
            IElement Incidence(Stroke stroke, V point) => !Interior(stroke, point)
                ? new Relation(NativePoint(point), NativePoint(point == stroke.A ? stroke.A : stroke.B))
                : new Relation(Segment(stroke.A, point), Segment(point, stroke.B));
        }
        catch (Exception error) when (error is OverflowException or WorkLimit)
        { unresolved.Add(new Relation(periodTranslation, budget)); return Result(); }

        void Spend() { if (--work < 0) throw new WorkLimit(); }
        Relation Result() => new(new Relation(new Relation(new Relation(provenance, strokes), S(admitted.Select(stroke => (IElement)stroke.Source).ToArray())),
            new Relation(periodTranslation, new Relation(fundamental, new Relation(height, budget)))),
            new Relation(S(overlaps.ToArray()), new Relation(S(crossings.ToArray()), new Relation(S(contacts.ToArray()),
                new Relation(S(symmetries.ToArray()), S(unresolved.ToArray()))))));
    }

    public static bool TryReadResult(Relation read, out Relation strokes, out Relation overlaps, out Relation crossings,
        out Relation contacts, out Relation symmetries, out Relation unresolved)
    {
        strokes = overlaps = crossings = contacts = symmetries = unresolved = null!;
        if (read.Left is not Relation { Left: Relation { Right: Relation source }, Right: Relation { Right: Relation { Right: Relation } } } ||
            read.Right is not Relation { Left: Relation overlap, Right: Relation { Left: Relation crossing,
                Right: Relation { Left: Relation contact, Right: Relation { Left: Relation symmetry, Right: Relation pending } } } } ||
            new[] { source, overlap, crossing, contact, symmetry, pending }.Any(scope => !Closed(scope))) return false;
        strokes = source; overlaps = overlap; crossings = crossing; contacts = contact; symmetries = symmetry; unresolved = pending; return true;
    }

    public static bool TryReadSymmetry(Relation witness, out Relation operation, out Relation coverage, out Relation unmatched)
    {
        operation = coverage = unmatched = null!;
        if (witness.Left is not Relation { Left: Relation map, Right: Relation } ||
            witness.Right is not Relation { Left: Relation proof, Right: Relation missing } || !Closed(proof) || !Closed(missing)) return false;
        operation = map; coverage = proof; unmatched = missing; return true;
    }

    public static Summary Describe(Relation read)
    {
        if (!TryReadResult(read, out var strokes, out var overlaps, out var crossings, out var contacts, out var symmetries, out var unresolved))
            throw new ArgumentException("Expected a measured rule read.", nameof(read));
        var basis = (Relation)((Relation)read.Left).Right;
        var period = (Relation)basis.Left;
        var detail = (Relation)basis.Right;
        long periodX = 0, periodY = 0, fundamental = 0, height = 0;
        if (period.Left is Relation a && period.Right is Relation b && Point(a, out var x, out _, out _, out _, out _) && Point(b, out var y, out _, out _, out _, out _))
        { TryInteger(y.X - x.X, out periodX); TryInteger(y.Y - x.Y, out periodY); }
        if (detail.Left is Relation f && NumericSpan(f, out var from, out var to)) TryInteger(to - from, out fundamental);
        if (detail.Right is Relation { Left: Relation h } && NumericSpan(h, out from, out to)) TryInteger(to - from, out height);
        var names = new List<string>();
        foreach (var item in E(symmetries).OfType<Relation>())
        {
            if (!TryReadSymmetry(item, out var operation, out _, out var missing) || E(missing).Length != 0 ||
                operation.Left is not Relation { Right: Relation xs } || operation.Right is not Relation { Right: Relation ys } ||
                !NumericSpan(xs, out var tx, out var xx) || !NumericSpan(ys, out var ty, out var yy)) continue;
            var sx = xx - tx; var sy = yy - ty;
            names.Add(sx == 1 && sy == 1 ? "translation" : sx == -1 && sy == 1 ? "vertical mirror" :
                sx == -1 && sy == -1 ? "half turn" : tx == 0 ? "horizontal mirror" : "glide");
        }
        return new(periodX, periodY, fundamental, height, E(strokes).Length, E(overlaps).Length, E(crossings).Length,
            E(contacts).Length, names.Distinct().ToArray(), E(unresolved).Length);
    }

    private static Relation Failed(IElement provenance) => new(new Relation(new Relation(provenance, S()),
        new Relation(S(), new Relation(S(), new Relation(S(), S())))), new Relation(S(), new Relation(S(),
            new Relation(S(), new Relation(S(), S(provenance))))));
    private static IEnumerable<BigInteger> Shifts(Stroke a, Stroke b, Q period)
    {
        var first = ((Min(a.A.X, a.B.X) - Max(b.A.X, b.B.X)) / period).Ceiling;
        var last = ((Max(a.A.X, a.B.X) - Min(b.A.X, b.B.X)) / period).Floor;
        for (var shift = first; shift <= last; shift++) yield return shift;
    }
    private static (V A, V B)? Intersection(Stroke a, Stroke b)
    {
        var r = a.B - a.A; var s = b.B - b.A; var denominator = Cross(r, s);
        if (denominator == 0)
        {
            if (Cross(r, b.A - a.A) != 0) return null;
            var p = Parameter(a, b.A); var q = Parameter(a, b.B);
            var lo = Max(0, Min(p, q)); var hi = Min(1, Max(p, q));
            return lo > hi ? null : (a.A + r * lo, a.A + r * hi);
        }
        var t = Cross(b.A - a.A, s) / denominator; var u = Cross(b.A - a.A, r) / denominator;
        if (t < 0 || t > 1 || u < 0 || u > 1) return null;
        var point = a.A + r * t; return (point, point);
    }
    private static Q Parameter(Stroke stroke, V point) => stroke.A.X != stroke.B.X
        ? (point.X - stroke.A.X) / (stroke.B.X - stroke.A.X) : (point.Y - stroke.A.Y) / (stroke.B.Y - stroke.A.Y);
    private static bool Interior(Stroke stroke, V point) => point != stroke.A && point != stroke.B;
    private static Q Cross(V a, V b) => a.X * b.Y - a.Y * b.X;
    private static Q Min(Q a, Q b) => a <= b ? a : b;
    private static Q Max(Q a, Q b) => a >= b ? a : b;
    private static Q Mod(Q value, Q period) => value - period * (value / period).Floor;
    private static IElement Coordinate(Carrier carrier, Q value) => value.D == 1 ? new CarrierLocation(carrier, Integer(value)) :
        new Relation(new CarrierLocation(carrier, checked((long)value.N)), Relation.Point(carrier, checked((long)value.D)));
    private static Relation Span(Carrier carrier, Q a, Q b) => new(new Relation(Coordinate(carrier, a), carrier), new Relation(Coordinate(carrier, b), carrier));
    private static bool NumericSpan(Relation span, out Q a, out Q b)
    {
        a = b = 0;
        return span.Left is Relation { Right: Carrier } first && span.Right is Relation { Right: Carrier } last &&
            ReadCoordinate(first.Left, out a) && ReadCoordinate(last.Left, out b);
    }
    private static bool Point(Relation point, out V value, out Relation x, out Relation y, out Carrier cx, out Carrier cy)
    {
        value = default; x = y = null!; cx = cy = null!;
        if (point.Left is not Relation { Left: Relation a, Right: var first } || point.Right is not Relation { Left: Relation b, Right: var second } ||
            ReferenceEquals(a, b) || !ReadCoordinate(first, out var u) || !ReadCoordinate(second, out var v) || u.D != 1 || v.D != 1) return false;
        var firstCarrier = CarrierOf(first); var secondCarrier = CarrierOf(second);
        if (firstCarrier.IsUnknown || firstCarrier.IsInverse || secondCarrier.IsUnknown || secondCarrier.IsInverse) return false;
        cx = firstCarrier; cy = secondCarrier; x = a; y = b; value = new(u, v); return true;
    }
    private static Carrier CarrierOf(IElement coordinate) => coordinate is CarrierLocation location ? location.Carrier : ((CarrierLocation)((Relation)coordinate).Left).Carrier;
    private static bool ReadCoordinate(IElement coordinate, out Q value)
    {
        value = 0;
        if (coordinate is CarrierLocation location) { value = location.Ticks; return true; }
        if (coordinate is Relation { Left: CarrierLocation numerator, Right: Relation { Left: CarrierLocation denominator, Right: Carrier carrier } } &&
            ReferenceEquals(numerator.Carrier, carrier) && ReferenceEquals(denominator.Carrier, carrier) && denominator.Ticks > 0)
        { value = new(numerator.Ticks, denominator.Ticks); return true; }
        return false;
    }
    private static long Integer(Q value) => value.D == 1 ? checked((long)value.N) : throw new OverflowException("Expected a whole source tick.");
    private static bool TryInteger(Q value, out long result)
    {
        result = 0;
        if (value.D != 1 || value.N < long.MinValue || value.N > long.MaxValue) return false;
        result = (long)value.N; return true;
    }
    private static Relation S(params IElement[] values) => StructuralConstraint.Closed(values);
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
    private static bool Closed(Relation scope) => StructuralConstraint.TryReadScope(scope, out _, out var a, out var b) && a.Equals(b);
    private sealed class WorkLimit : Exception { }
    private sealed class PointSite
    {
        public List<IElement> PairHits { get; } = [];
        public List<TranslatedSupport> Supports { get; } = [];
    }
    private sealed record TranslatedSupport(Stroke Source, Q Translation);
    private sealed class RayFamily
    {
        public List<IElement> Forward { get; } = [];
        public List<IElement> Opposed { get; } = [];
    }
    private sealed record Stroke(Relation Source, V A, V B) { public Stroke Shift(Q x) => new(Source, new(A.X + x, A.Y), new(B.X + x, B.Y)); }
    private readonly record struct V(Q X, Q Y)
    {
        public static V operator +(V a, V b) => new(a.X + b.X, a.Y + b.Y);
        public static V operator -(V a, V b) => new(a.X - b.X, a.Y - b.Y);
        public static V operator *(V a, Q b) => new(a.X * b, a.Y * b);
    }
    private readonly record struct Q : IComparable<Q>
    {
        public BigInteger N { get; } public BigInteger D { get; }
        public Q(BigInteger n, BigInteger d)
        {
            if (d == 0) throw new DivideByZeroException();
            if (d < 0) { n = -n; d = -d; }
            var g = BigInteger.GreatestCommonDivisor(BigInteger.Abs(n), d); N = n / g; D = d / g;
        }
        public BigInteger Floor => N >= 0 ? N / D : -((-N + D - 1) / D);
        public BigInteger Ceiling => -new Q(-N, D).Floor;
        public int CompareTo(Q other) => (N * other.D).CompareTo(other.N * D);
        public static implicit operator Q(long n) => new(n, 1);
        public static implicit operator Q(BigInteger n) => new(n, 1);
        public static Q operator +(Q a, Q b) => new(a.N * b.D + b.N * a.D, a.D * b.D);
        public static Q operator -(Q a, Q b) => new(a.N * b.D - b.N * a.D, a.D * b.D);
        public static Q operator *(Q a, Q b) => new(a.N * b.N, a.D * b.D);
        public static Q operator /(Q a, Q b) => new(a.N * b.D, a.D * b.N);
        public static bool operator <(Q a, Q b) => a.CompareTo(b) < 0;
        public static bool operator >(Q a, Q b) => a.CompareTo(b) > 0;
        public static bool operator <=(Q a, Q b) => a.CompareTo(b) <= 0;
        public static bool operator >=(Q a, Q b) => a.CompareTo(b) >= 0;
    }
}
