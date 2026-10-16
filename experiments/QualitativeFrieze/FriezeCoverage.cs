using System.Numerics;
using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;

namespace Gojo.Experiments.QualitativeFrieze;

/// <summary>A measured supplier for a finite, periodically joined cell complex.
/// Occupancy and ordinal clearance are subsequently read by QualitativeCoverage.
/// Probe counts are resolution-dependent observations, not exact area.</summary>
public static class FriezeCoverage
{
    // surface = ((measured-source -> actual-strokes) ->
    //            (metric-quantum -> (axis-metric-maps -> (selected-radius -> (period -> (probe-band -> cell-budget))))))) ->
    //           ((cell-coordinate-clauses -> periodic-joins) -> (occupancy-read -> supplier-unresolved))
    // cell-coordinate = cell -> ((x-axis -> L(metric,x)) -> (y-axis -> L(metric,y)))
    // footprint = actual-stroke -> S(metric,0,translated-period-offset)
    // join = actual-adjacency-edge -> S(metric,0,period)
    // metric-map = (source-axis -> S(source-calibration,0,1)) -> S(metric,0,1)
    // These 1:1 maps select this observer's metric interpretation; they do not
    // establish physical equivalence of unrelated source units.
    public sealed record Summary(int Cells, int Empty, int Single, int Multiple,
        int Centers, int ClearanceSpread, int MaximumClearance, int Unresolved)
    {
        public double EmptyFraction => Cells == 0 ? 1 : (double)Empty / Cells;
        public double MultipleFraction => Cells == 0 ? 1 : (double)Multiple / Cells;
    }
    public sealed record Evaluation(Relation Surface, QualitativeCoverage Coverage, Relation Read, Summary Projection);

    public static Evaluation Evaluate(FriezeFixture fixture, Relation measuredSource, Relation strokes,
        long period, int cellBudget = 2048, long? minimumY = null, long? maximumY = null)
    {
        var metric = new Carrier();
        var quantum = Relation.Segment(metric, 0, 1); var radius = Relation.Point(metric, 1);
        var periodSpan = Relation.Segment(metric, 0, period);
        var budget = Relation.Segment(metric, 0, cellBudget);
        Relation band = minimumY.HasValue || maximumY.HasValue
            ? new Relation(minimumY.HasValue ? Relation.Point(metric, minimumY.Value) : new Placeholder(),
                maximumY.HasValue ? Relation.Point(metric, maximumY.Value) : new Placeholder())
            : StructuralConstraint.Open();
        var supplierPending = new List<IElement>();
        var cells = new List<IElement>(); var coordinates = new List<IElement>(); var joins = new List<IElement>();
        var edges = new List<IElement>(); var rows = new List<IElement>();
        var lines = new List<(Relation Source, long X0, long Y0, long X1, long Y1)>();
        Carrier? sourceX = null, sourceY = null;
        if (!StructuralConstraint.TryReadScope(strokes, out var strokeEntries, out var seam, out var tail))
        { strokeEntries = []; supplierPending.Add(strokes); }
        else if (!seam.Equals(tail)) supplierPending.Add(new Relation(strokes, tail));
        foreach (var item in strokeEntries)
        {
            if (item is not Relation clause || clause.Right is not Relation { Left: Relation before, Right: Relation after } ||
                !Point(before, out var x0, out var y0, out var xCarrier, out var yCarrier) ||
                !Point(after, out var x1, out var y1, out var otherX, out var otherY) ||
                !ReferenceEquals(xCarrier, otherX) || !ReferenceEquals(yCarrier, otherY) ||
                sourceX is not null && !ReferenceEquals(sourceX, xCarrier) ||
                sourceY is not null && !ReferenceEquals(sourceY, yCarrier))
            { supplierPending.Add(item); continue; }
            sourceX ??= xCarrier; sourceY ??= yCarrier;
            // A sampled pause contributes no moving ink in this supplier.
            if (x0 != x1 || y0 != y1) lines.Add((clause, x0, y0, x1, y1));
        }
        if (minimumY.HasValue != maximumY.HasValue)
        { supplierPending.Add(new Relation(strokes, band)); return Finish(StructuralConstraint.Open(), StructuralConstraint.Open(), StructuralConstraint.Open()); }
        var minimum = minimumY.HasValue ? minimumY.Value : (BigInteger)(lines.Count == 0 ? 0 : lines.Min(line => Math.Min(line.Y0, line.Y1))) - 1;
        var maximum = maximumY.HasValue ? maximumY.Value : (BigInteger)(lines.Count == 0 ? 0 : lines.Max(line => Math.Max(line.Y0, line.Y1))) + 1;
        var height = maximum - minimum + 1;
        if (minimum >= long.MinValue && minimum <= long.MaxValue && maximum >= long.MinValue && maximum <= long.MaxValue)
            band = Relation.Segment(metric, (long)minimum, (long)maximum);
        if (minimum < long.MinValue || maximum > long.MaxValue || minimum > maximum ||
            period <= 0 || period > 128 || height <= 0 || height > 128 || cellBudget <= 0 || period * height > cellBudget)
        { supplierPending.Add(new Relation(strokes, new Relation(periodSpan, budget)));
            return Finish(StructuralConstraint.Open(), StructuralConstraint.Open(), StructuralConstraint.Open());
        }
        var width = (int)period; var countY = (int)height;
        var lattice = new IElement[countY, width];
        var footprints = new Dictionary<(Relation, long), Relation>();
        for (var y = 0; y < countY; y++)
            for (var x = 0; x < width; x++)
            {
                var cell = new Placeholder(); lattice[y, x] = cell; cells.Add(cell);
                var py = checked((long)(minimum + y));
                coordinates.Add(new Relation(cell, new Relation(
                    new Relation(fixture.Frame.Horizontal, new CarrierLocation(metric, x)),
                    new Relation(fixture.Frame.Vertical, new CarrierLocation(metric, py)))));
                var occupied = new List<IElement>();
                foreach (var line in lines)
                {
                    var low = Ceiling((BigInteger)x - 1 - Math.Max(line.X0, line.X1), period);
                    var high = Floor((BigInteger)x + 1 - Math.Min(line.X0, line.X1), period);
                    if (high - low > 512)
                    { supplierPending.Add(new Relation(line.Source, periodSpan));
                        return Finish(StructuralConstraint.Open(cells.ToArray()), StructuralConstraint.Open(), StructuralConstraint.Open()); }
                    for (var offset = low; offset <= high; offset++)
                    {
                        var shift = offset * period;
                        if (!WithinRadius(x, py, (BigInteger)line.X0 + shift, line.Y0, (BigInteger)line.X1 + shift, line.Y1)) continue;
                        if (offset < long.MinValue || offset > long.MaxValue || shift < long.MinValue || shift > long.MaxValue)
                        { supplierPending.Add(new Relation(line.Source, periodSpan));
                            return Finish(StructuralConstraint.Open(cells.ToArray()), StructuralConstraint.Open(), StructuralConstraint.Open()); }
                        var k = (long)offset;
                        if (!footprints.TryGetValue((line.Source, k), out var footprint))
                        {
                            footprint = new Relation(line.Source, Relation.Segment(metric, 0, (long)shift));
                            footprints.Add((line.Source, k), footprint);
                        }
                        occupied.Add(footprint);
                    }
                }
                rows.Add(new Relation(cell, supplierPending.Count == 0 ? S(occupied.ToArray()) : StructuralConstraint.Open(occupied.ToArray())));
            }
        for (var y = 0; y < countY; y++)
            for (var x = 0; x < width; x++)
            {
                if (x + 1 < width) Connect(lattice[y, x], lattice[y, x + 1]);
                else
                {
                    var forward = new Relation(lattice[y, x], lattice[y, 0]);
                    var backward = forward.Reverse(); edges.Add(forward); edges.Add(backward);
                    joins.Add(new Relation(forward, Relation.Segment(metric, 0, period)));
                    joins.Add(new Relation(backward, Relation.Segment(metric, period, 0)));
                }
                if (y + 1 < countY) Connect(lattice[y, x], lattice[y + 1, x]);
            }
        return Finish(S(cells.ToArray()), S(edges.ToArray()), S(rows.ToArray()));

        void Connect(IElement a, IElement b) { edges.Add(new Relation(a, b)); edges.Add(new Relation(b, a)); }
        Evaluation Finish(Relation cellScope, Relation adjacency, Relation coverage)
        {
            var observer = new QualitativeCoverage(cellScope, adjacency, coverage); var read = observer.Evaluate();
            if (!observer.TryReadResult(read, out var empty, out var single, out var multiple,
                    out var layers, out var centers, out var pending))
                return new(new Relation(measuredSource, read), observer, read, new(0, 0, 0, 0, 0, 0, 0, 1));
            var indices = new Dictionary<Relation, int>(ReferenceEqualityComparer.Instance);
            foreach (var value in E(layers).OfType<Relation>())
                if (observer.TryReadLayer(value, out var previous, out _))
                    indices[value] = previous is Relation prior && indices.TryGetValue(prior, out var ordinal) ? ordinal + 1 : 0;
            var centerLevels = E(centers).OfType<Relation>().Select(center =>
                observer.TryReadCenter(center, out var layer, out _) && indices.TryGetValue(layer, out var ordinal) ? ordinal : -1).ToArray();
            var spread = centerLevels.Length == 0 ? 0 : centerLevels.Max() - centerLevels.Min();
            var projection = new Summary(E(cellScope).Length, E(empty).Length, E(single).Length, E(multiple).Length,
                centerLevels.Length, spread, indices.Count == 0 ? 0 : indices.Values.Max(), E(pending).Length + supplierPending.Count);
            var metricMaps = sourceX is not null && sourceY is not null ? S(
                new Relation(new Relation(fixture.Frame.Horizontal, Relation.Segment(sourceX, 0, 1)), Relation.Segment(metric, 0, 1)),
                new Relation(new Relation(fixture.Frame.Vertical, Relation.Segment(sourceY, 0, 1)), Relation.Segment(metric, 0, 1))) : StructuralConstraint.Open();
            var surface = new Relation(new Relation(new Relation(measuredSource, strokes),
                    new Relation(quantum, new Relation(metricMaps, new Relation(radius, new Relation(periodSpan, new Relation(band, budget)))))),
                new Relation(new Relation(S(coordinates.ToArray()), S(joins.ToArray())), new Relation(read, S(supplierPending.ToArray()))));
            return new(surface, observer, read, projection);
        }

        bool Point(Relation point, out long x, out long y, out Carrier xCarrier, out Carrier yCarrier)
        {
            x = y = 0; xCarrier = yCarrier = null!;
            if (point.Left is not Relation { Right: CarrierLocation horizontal } xRole ||
                point.Right is not Relation { Right: CarrierLocation vertical } yRole ||
                !ReferenceEquals(xRole.Left, fixture.Frame.Horizontal) || !ReferenceEquals(yRole.Left, fixture.Frame.Vertical) ||
                horizontal.Carrier.IsUnknown || horizontal.Carrier.IsInverse || vertical.Carrier.IsUnknown || vertical.Carrier.IsInverse) return false;
            x = horizontal.Ticks; y = vertical.Ticks; xCarrier = horizontal.Carrier; yCarrier = vertical.Carrier;
            return true;
        }
    }

    private static bool WithinRadius(BigInteger px, BigInteger py, BigInteger ax, BigInteger ay, BigInteger bx, BigInteger by)
    {
        var vx = bx - ax; var vy = by - ay; var wx = px - ax; var wy = py - ay;
        var length = vx * vx + vy * vy; var dot = wx * vx + wy * vy;
        if (dot <= 0) return wx * wx + wy * wy <= 1;
        if (dot >= length) { var dx = px - bx; var dy = py - by; return dx * dx + dy * dy <= 1; }
        var cross = vx * wy - vy * wx; return cross * cross <= length;
    }
    private static BigInteger Floor(BigInteger a, BigInteger b) { var q = BigInteger.DivRem(a, b, out var r); return r < 0 ? q - 1 : q; }
    private static BigInteger Ceiling(BigInteger a, BigInteger b) => -Floor(-a, b);
    private static Relation S(params IElement[] values) => StructuralConstraint.Closed(values);
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
}
