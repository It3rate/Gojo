using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;

namespace Gojo.Experiments.QualitativeFrieze;

/// <summary>Selected rectilinear ribbon interpretation. Its finite cells tile
/// the advertised strip; neither color is treated as absent participation.
/// Widths, calibration and the outside rails are separate from the source path.</summary>
public sealed class RibbonPartitionSupplier
{
    // source = (fixture -> (band-extent -> gap-extent)) ->
    //          Closed((axis -> reach) -> Closed(band-extent,gap-extent))
    // interpretation = ((source -> source-calibration) -> observer-calibration) ->
    //                  ((geometry -> coordinates) -> (partition-read -> (rails -> unresolved)))
    // coordinate = actual-cell -> ((axis -> cell-span) -> (axis -> cell-span))
    // observer-calibration = (source-axis-maps -> extent-bindings) ->
    //                        (period-span -> (strip-span -> cell-budget))
    public FriezeFixture Fixture { get; }
    public IElement BandExtent { get; }
    public IElement GapExtent { get; }
    public Relation Surface { get; }
    public sealed record Summary(int Columns, int Rows, int First, int Second, int Regions,
        int Interfaces, int Unresolved, long Period, long MinimumY, long MaximumY, long BandWidth,
        long GapWidth)
    {
        public double FirstFraction => First + Second == 0 ? 0 : (double)First / (First + Second);
    }
    public sealed record ExchangeProbe(string Name, Relation Map, Relation Read, bool Passed);
    public sealed record Evaluation(Relation Surface, Relation Coordinates, Relation Rails,
        Relation Geometry, Relation Calibration, QualitativePartition Partition, Relation Read,
        Summary Projection, IReadOnlyList<ExchangeProbe> Exchanges);

    public RibbonPartitionSupplier(FriezeFixture fixture, IElement bandExtent, IElement gapExtent)
    {
        Fixture = fixture; BandExtent = bandExtent; GapExtent = gapExtent;
        var roles = new List<IElement>();
        if (FiniteLaw.TryRead(fixture.OutputLaw, out _, out _, out var words, out _))
            foreach (var move in E(words).OfType<Relation>().SelectMany(word => E(word)).OfType<Relation>())
                if (fixture.Frame.Direction(move, out var axis, out _, out var reach) && !roles.OfType<Relation>().Any(row =>
                    row.Left is Relation role && ReferenceEquals(role.Left, axis) && Same(role.Right, reach)))
                    roles.Add(new Relation(new Relation(axis, reach), S(bandExtent, gapExtent)));
        Surface = new(new Relation(fixture.Surface, new Relation(bandExtent, gapExtent)), S(roles.ToArray()));
    }

    public Evaluation Interpret(Relation trace, Relation calibration, long bandWidth, long gapWidth,
        long margin = 3, int cellBudget = 8192, long? minimumY = null, long? maximumY = null)
    {
        var first = new Placeholder(); var second = new Placeholder(); var pair = new Relation(first, second);
        var metric = new Carrier(); var pending = new List<IElement>();
        var coordinates = new List<IElement>(); var cells = new List<IElement>(); var edges = new List<IElement>(); var labels = new List<IElement>();
        var fillEvidence = new List<IElement>();
        var rails = new List<IElement>(); var exchanges = new List<ExchangeProbe>();
        Relation observer = StructuralConstraint.Open(); Relation geometry = StructuralConstraint.Open();
        long period = 0, low = 0, high = 0; var width = 0; var height = 0;
        try
        {
            geometry = FriezeMeasuredRules.Evaluate(Fixture, trace, calibration);
            if (!FriezeMeasuredRules.TryReadResult(geometry, out var strokes, out _, out _, out _, out _, out var unsettled) ||
                E(unsettled).Length != 0 || bandWidth <= 0 || gapWidth <= 0 || margin < bandWidth ||
                Same(BandExtent, GapExtent) && bandWidth != gapWidth || cellBudget <= 0 || minimumY.HasValue != maximumY.HasValue)
            { pending.Add(new Relation(Surface, new Relation(calibration, geometry))); return Finish(false); }
            var summary = FriezeMeasuredRules.Describe(geometry); period = checked(summary.PeriodX * 2);
            var lines = new List<(Relation Source, long X0, long Y0, long X1, long Y1)>();
            foreach (var clause in E(strokes).OfType<Relation>())
            {
                if (clause.Right is not Relation { Left: Relation before, Right: Relation after } ||
                    !FriezeRealization.TryReadPoint(before, out var x0, out var y0) ||
                    !FriezeRealization.TryReadPoint(after, out var x1, out var y1) || x0 != x1 && y0 != y1)
                { pending.Add(clause); continue; }
                if (x0 != x1 || y0 != y1) lines.Add((clause, checked(x0 * 2), checked(y0 * 2), checked(x1 * 2), checked(y1 * 2)));
            }
            if (pending.Count != 0 || lines.Count == 0 || calibration.Right is not Relation mapping)
            { pending.Add(geometry); return Finish(false); }
            // Each advertised source pitch is composed of the two actual extents.
            // The numeric interpretation must satisfy that visible promise.
            foreach (var clause in E((Relation)Surface.Right).Cast<Relation>())
            {
                var role = (Relation)clause.Left;
                var bindings = E(mapping).OfType<Relation>().Where(row => row.Left is Relation candidate &&
                    ReferenceEquals(candidate.Left, role.Left) && Same(candidate.Right, role.Right)).ToArray();
                if (bindings.Length != 1 || bindings[0].Right is not Relation span || !Span(span, out _, out var a, out var b) ||
                    a != 0 || b != checked(bandWidth + gapWidth)) pending.Add(new Relation(clause, S(bindings)));
            }
            low = minimumY ?? checked(lines.Min(line => Math.Min(line.Y0, line.Y1)) - margin);
            high = maximumY ?? checked(lines.Max(line => Math.Max(line.Y0, line.Y1)) + margin);
            var total = checked(period * checked(high - low));
            if (pending.Count != 0 || period <= 0 || period > 256 || high <= low || high - low > 256 || total > cellBudget)
            { pending.Add(geometry); return Finish(false); }
            width = (int)period; height = checked((int)(high - low));
            var axisMaps = new List<IElement>();
            if (calibration.Left is Relation { Right: Relation axes })
                foreach (var axis in E(axes).OfType<Relation>())
                    if (axis.Right is Carrier source)
                        axisMaps.Add(new Relation(axis.Left, new Relation(Relation.Segment(source, 0, 1), Relation.Segment(metric, 0, 2))));
            var extentBindings = Same(BandExtent, GapExtent)
                ? S(new Relation(BandExtent, Relation.Segment(metric, 0, checked(2 * bandWidth))))
                : S(new Relation(BandExtent, Relation.Segment(metric, 0, checked(2 * bandWidth))),
                    new Relation(GapExtent, Relation.Segment(metric, 0, checked(2 * gapWidth))));
            observer = new(new Relation(S(axisMaps.ToArray()), extentBindings),
                new Relation(Relation.Segment(metric, 0, period),
                    new Relation(Relation.Segment(metric, low, high), Relation.Segment(metric, 0, cellBudget))));
            var grid = new IElement[height, width];
            for (var y = 0; y < height; y++)
                for (var x = 0; x < width; x++)
                {
                    var cell = new Placeholder(); grid[y, x] = cell; cells.Add(cell);
                    coordinates.Add(new Relation(cell, new Relation(
                        new Relation(Fixture.Frame.Horizontal, Relation.Segment(metric, x, x + 1)),
                        new Relation(Fixture.Frame.Vertical, Relation.Segment(metric, low + y, low + y + 1)))));
                    var hits = new List<IElement>();
                    foreach (var line in lines)
                    {
                        var min = Math.Min(line.X0, line.X1) - bandWidth; var max = Math.Max(line.X0, line.X1) + bandWidth;
                        var start = Floor(x - max, period); var end = Ceiling(x + 1 - min, period);
                        if (end - start > 512) { pending.Add(line.Source); return Finish(false); }
                        for (var k = start; k <= end; k++)
                        {
                            var shift = checked(k * period);
                            if (x < min + shift || x >= max + shift ||
                                low + y < Math.Min(line.Y0, line.Y1) - bandWidth ||
                                low + y >= Math.Max(line.Y0, line.Y1) + bandWidth) continue;
                            hits.Add(new Relation(line.Source, Relation.Segment(metric, 0, shift)));
                        }
                    }
                    // Retain causal fill witnesses separately from the role row;
                    // either role is fully occupied participation in this partition.
                    labels.Add(new Relation(cell, hits.Count == 0 ? second : first));
                    fillEvidence.Add(new Relation(cell, S(hits.ToArray())));
                }
            for (var y = 0; y < height; y++)
                for (var x = 0; x < width; x++)
                {
                    Join(grid[y, x], grid[y, (x + 1) % width]);
                    if (y + 1 < height) Join(grid[y, x], grid[y + 1, x]);
                }
            // These two rails frame the advertised domain and are not cells of it.
            var lower = checked(low - 2 * gapWidth - bandWidth);
            var upper = checked(high + 2 * gapWidth + bandWidth);
            rails.Add(new Relation(Point(0, lower), Point(period, lower)));
            rails.Add(new Relation(Point(0, upper), Point(period, upper)));
            var result = Finish(true);
            foreach (var (name, reflectY) in new[] { ("half-period translation", false), ("half-period glide", true) })
            {
                if (width % 2 != 0) continue;
                var map = S(Enumerable.Range(0, height).SelectMany(y => Enumerable.Range(0, width).Select(x =>
                    (IElement)new Relation(grid[y, x], grid[reflectY ? height - 1 - y : y, (x + width / 2) % width]))).ToArray());
                var read = result.Partition.EvaluateExchange(map);
                exchanges.Add(new(name, map, read, result.Partition.TryReadExchangePassed(read)));
            }
            return result with { Surface = new Relation(result.Surface, S(exchanges.Select(item => (IElement)item.Read).ToArray())) };
        }
        catch (OverflowException) { pending.Add(new Relation(Surface, calibration)); return Finish(false); }

        Relation Point(long x, long y) => new(new Relation(Fixture.Frame.Horizontal, new CarrierLocation(metric, x)),
            new Relation(Fixture.Frame.Vertical, new CarrierLocation(metric, y)));
        void Join(IElement a, IElement b) { edges.Add(new Relation(a, b)); edges.Add(new Relation(b, a)); }
        Evaluation Finish(bool complete)
        {
            var partition = new QualitativePartition(complete ? S(cells.ToArray()) : StructuralConstraint.Open(cells.ToArray()),
                complete ? S(edges.ToArray()) : StructuralConstraint.Open(edges.ToArray()), pair,
                complete ? S(labels.ToArray()) : StructuralConstraint.Open(labels.ToArray()));
            var read = partition.Evaluate();
            var projection = new Summary(width, height, 0, 0, 0, 0, pending.Count + 1, period, low, high, bandWidth, gapWidth);
            if (partition.TryReadResult(read, out var a, out var b, out var regions, out var boundaries, out var unknown))
                projection = projection with { First = E(a).Length, Second = E(b).Length, Regions = E(regions).Length,
                    Interfaces = E(boundaries).Length, Unresolved = pending.Count + E(unknown).Length };
            var interpretation = new Relation(new Relation(new Relation(Surface, calibration), observer),
                new Relation(new Relation(geometry, new Relation(S(coordinates.ToArray()), S(fillEvidence.ToArray()))),
                    new Relation(read, new Relation(S(rails.ToArray()), S(pending.ToArray())))));
            return new(interpretation, S(coordinates.ToArray()), S(rails.ToArray()), geometry, calibration,
                partition, read, projection, exchanges);
        }
    }

    private static bool Span(Relation span, out Carrier carrier, out long from, out long to)
    {
        carrier = null!; from = to = 0;
        if (span.Left is not Relation { Left: CarrierLocation a, Right: Carrier c } ||
            span.Right is not Relation { Left: CarrierLocation b, Right: Carrier d } ||
            !ReferenceEquals(c, d) || !ReferenceEquals(a.Carrier, c) || !ReferenceEquals(b.Carrier, c)) return false;
        carrier = c; from = a.Ticks; to = b.Ticks; return true;
    }
    private static long Floor(long a, long b) { var q = Math.DivRem(a, b, out var r); return r < 0 ? q - 1 : q; }
    private static long Ceiling(long a, long b) => -Floor(-a, b);
    private static bool Same(IElement a, IElement b) => ReferenceEquals(a, b) || a is Placeholder x && b is Placeholder y && x.Equals(y);
    private static Relation S(params IElement[] values) => StructuralConstraint.Closed(values);
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
}
