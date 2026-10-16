using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;
using Gojo.Core;
using Gojo.Patterns;
using Gojo.Equations;
using Gojo.Experiments.QualitativeExecution;

namespace Gojo.Experiments.QualitativeFrieze;

/// <summary>Deliberate shaping and presentation of native programs and fields.
/// Every picture is read from a supplied graph, never selected by its name.</summary>
public static class PatternShapingExperiment
{
    private sealed record Block(double X, double Y, double Width, double Height, bool First);
    private sealed record Picture(string Name, string Detail, Relation Source, Relation Observation,
        double Period, double Low, double High, double Band, IReadOnlyList<Block> Blocks,
        IReadOnlyList<(double X, double Y)> Skeleton, IReadOnlyList<double> Rails, object Diagnostics,
        string Caption, string? BoundaryCaption = null);

    public static int Run(string[] arguments)
    {
        var output = Path.Combine("experiments", "QualitativeFrieze", "results", "shaping");
        for (var i = 0; i < arguments.Length; i++)
            if (arguments[i] == "--output" && i + 1 < arguments.Length) output = arguments[++i];
            else throw new ArgumentException("Usage: --shape [--output DIRECTORY]");
        Directory.CreateDirectory(output);
        var library = new PatternShapingLibrary();
        var gallery = new List<Picture>(); var styles = new List<Picture>();
        foreach (var entry in library.Patterns)
        {
            var trace = entry.Fixture.Run(entry.PhaseCount);
            var calibration = Calibrate(entry.Fixture);
            foreach (var band in new[] { 1L, 2L, 3L })
            {
                var extent = new Placeholder();
                var supplier = new RibbonPartitionSupplier(entry.Fixture, extent, band == 2 ? extent : new Placeholder());
                var field = supplier.Interpret(trace, calibration, band, 4 - band);
                var picture = Ribbon(entry.Name, $"{entry.PhaseCount} phases / {MoveCount(entry.Words)} reach participations", supplier, field);
                styles.Add(picture);
                if (band == 2) gallery.Add(picture);
            }
        }
        var woven = new[] { ("Counterchanging chevron", QualitativeWeave.SteppedChevron()),
            ("Counterchanging nested fret", QualitativeWeave.NestedFret()),
            ("Counterchanging folded blocks", QualitativeWeave.FoldedBlocks()) };
        foreach (var (name, weave) in woven) gallery.Add(Weave(name, weave));

        var original = library.Patterns[2].Fixture;
        var centered = library.CenteredMeander;
        var centering = new List<Picture>();
        foreach (var (name, fixture) in new[] { ("Before / original phase origin", original), ("After / selected phase origin", centered) })
        {
            var extent = new Placeholder(); var supplier = new RibbonPartitionSupplier(fixture, extent, extent);
            var field = supplier.Interpret(fixture.Run(7), Calibrate(fixture), 2, 2, minimumY: -19, maximumY: 19);
            centering.Add(Ribbon(name, "Same phase graph, emission law and shared reaches; fixed observation band", supplier, field));
        }
        var lineage = new Relation(library.CenteringEdit,
            S(centering.Select(picture => (IElement)picture.Observation).ToArray()));
        var main = Draw(gallery, 1, "Qualitative keys and counterchanging fields",
            "Authored turns, repeated reaches and two explicit roles. Numeric proportions enter through the selected view.");
        var variants = Draw(styles, 3, "The same programs, three band / gap interpretations",
            "Columns: thin 1 / 3, shared equal extent 2 / 2, heavy 3 / 1. Elementary pitch stays four source ticks.");
        var placement = Draw(centering, 2, "Shaping by selecting an existing phase origin",
            "Both views use the same fixed strip. The edit changes starting bindings, preserving the network and emission law.");
        File.WriteAllText(Path.Combine(output, "gallery.svg"), main);
        File.WriteAllText(Path.Combine(output, "styles.svg"), variants);
        File.WriteAllText(Path.Combine(output, "centering.svg"), placement);
        File.WriteAllText(Path.Combine(output, "gallery.html"), Html(main, variants, placement));
        File.WriteAllText(Path.Combine(output, "diagnostics.json"), JsonSerializer.Serialize(new
        {
            Schema = "qualitative-pattern-shaping-v1",
            Method = "Authored qualitative programs and fields; explicit measured interpretations; no stochastic training.",
            LibrarySource = Counts(library.Surface),
            CenteringLineageNodes = Traverse(lineage).Count(),
            Gallery = gallery.Select(picture => picture.Diagnostics).ToArray(),
            Styles = styles.Select(picture => picture.Diagnostics).ToArray(),
            Centering = centering.Select(picture => picture.Diagnostics).ToArray()
        }, new JsonSerializerOptions { WriteIndented = true }));
        Console.WriteLine($"Authored {library.Patterns.Count} ribbon programs and {woven.Length} two-role fields.");
        foreach (var picture in gallery) Console.WriteLine($"{picture.Name}: {picture.Caption}");
        foreach (var picture in centering) Console.WriteLine($"{picture.Name}: {picture.Caption}");
        Console.WriteLine($"Wrote shaping gallery, style and phase-origin comparisons to {Path.GetFullPath(output)}.");
        return 0;
    }

    private static Picture Ribbon(string name, string detail, RibbonPartitionSupplier source, RibbonPartitionSupplier.Evaluation field)
    {
        if (field.Projection.Unresolved != 0 || !field.Partition.TryReadResult(field.Read, out var first, out var second, out var regions, out _, out _))
            throw new InvalidOperationException($"Incomplete ribbon interpretation: {name}.");
        var roles = LabelIndex(first, second);
        var cells = new List<(int X, int Y, bool First)>(); Carrier metric = null!;
        foreach (var clause in E(field.Coordinates).Cast<Relation>())
        {
            var axes = (Relation)clause.Right;
            var x = (Relation)((Relation)axes.Left).Right; var y = (Relation)((Relation)axes.Right).Right;
            metric = ((CarrierLocation)((Relation)x.Left).Left).Carrier;
            cells.Add((checked((int)Start(x)), checked((int)Start(y)), roles[clause.Left]));
        }
        var blocks = Blocks(cells);
        var rails = E(field.Rails).Cast<Relation>().Select(rail =>
            FriezeRealization.TryReadPoint((Relation)rail.Left, out _, out var y) ? (double)y : 0).ToArray();
        var skeleton = new List<(double X, double Y)>();
        if (FriezeMeasuredRules.TryReadResult(field.Geometry, out var strokes, out _, out _, out _, out _, out _))
            foreach (var clause in E(strokes).Cast<Relation>())
                if (FriezeRealization.TryReadSamplePoints(clause, out _, out var a, out var b) &&
                    FriezeRealization.TryReadPoint(a, out var x0, out var y0) && FriezeRealization.TryReadPoint(b, out var x1, out var y1))
                { if (skeleton.Count == 0) skeleton.Add((x0 * 2, y0 * 2)); skeleton.Add((x1 * 2, y1 * 2)); }
        var q = field.Projection;
        var filled = cells.Where(cell => cell.First).ToArray();
        var below = filled.Length == 0 ? 0 : filled.Min(cell => cell.Y) - q.MinimumY;
        var above = filled.Length == 0 ? 0 : q.MaximumY - filled.Max(cell => cell.Y) - 1;
        var gapRead = new Relation(field.Partition.Surface, new Relation(
            Relation.Segment(metric, q.MinimumY, q.MinimumY + below),
            Relation.Segment(metric, q.MaximumY - above, q.MaximumY)));
        var lowerCount = new Carrier(); var upperCount = new Carrier(); var error = new Carrier();
        var margins = new Relation(new CarrierLocation(lowerCount, below), new CarrierLocation(upperCount, above));
        var difference = Equation.Difference(lowerCount, upperCount, error);
        var prediction = Equation.Predict(difference, margins, error);
        if (!Equation.TryReadPrediction(prediction, out var marginDifference, out _))
            throw new InvalidOperationException("Expected a native boundary-margin comparison.");
        var marginGoal = new Relation(gapRead, new Relation(difference,
            new Relation(margins, new Relation(prediction, Relation.Point(error, 0)))));
        var geometry = FriezeMeasuredRules.Describe(field.Geometry);
        var caption = $"Ink {q.FirstFraction:P0}; {q.Regions} role regions; band/gap {q.BandWidth}/{q.GapWidth}; centerline overlaps {geometry.OverlapCount}, crossings {geometry.CrossingCount}";
        var observed = new Relation(field.Surface, marginGoal);
        return new(name, detail, source.Surface, observed, q.Period, q.MinimumY, q.MaximumY, q.BandWidth * 2,
            blocks, skeleton, rails, new
            {
                Name = name, Kind = "ribbon", Source = Counts(source.Surface), Field = q, Geometry = geometry,
                RoleRegions = RegionCounts(field.Partition, regions),
                ElementaryEqualExtent = ReferenceEquals(source.BandExtent, source.GapExtent),
                Exchange = field.Exchanges.Select(probe => Exchange(field.Partition, probe.Name, probe.Read)).ToArray(),
                BoundaryGaps = new { Below = below, Above = above, Imbalance = Math.Abs(marginDifference.Ticks) },
                ObservationNodes = Traverse(observed).Count()
            }, caption, $"White margin: {below} cells below / {above} above; imbalance {Math.Abs(below - above)}");
    }

    private static Picture Weave(string name, QualitativeWeave weave)
    {
        var read = weave.Partition.Evaluate(); var exchange = weave.EvaluateHalfShift();
        if (!weave.Partition.TryReadResult(read, out var first, out var second, out var regions, out var interfaces, out var pending) ||
            E(pending).Length != 0 || !weave.Partition.TryReadExchangePassed(exchange))
            throw new InvalidOperationException($"Incomplete weave: {name}.");
        var columns = E(weave.Columns); var rows = E(weave.Rows); var roles = LabelIndex(first, second);
        IEqualityComparer<IElement> identity = ReferenceEqualityComparer.Instance;
        var xIndex = columns.Select((id, index) => (id, index)).ToDictionary(item => item.id, item => item.index, identity);
        var yIndex = rows.Select((id, index) => (id, index)).ToDictionary(item => item.id, item => item.index, identity);
        var cells = new List<(int X, int Y, bool First)>();
        var metric = new Carrier();
        var bindings = columns.Select((id, i) => (IElement)new Relation(id, Relation.Segment(metric, i, i + 1)))
            .Concat(rows.Select((id, i) => (IElement)new Relation(id, Relation.Segment(metric, -i - 1, -i)))).ToArray();
        var calibration = new Relation(new Relation(weave.ColumnPitch, Relation.Segment(metric, 0, 1)),
            new Relation(new Relation(weave.RowPitch, Relation.Segment(metric, 0, -1)), S(bindings)));
        foreach (var clause in E(weave.Coordinates).Cast<Relation>())
        {
            var position = (Relation)clause.Right;
            cells.Add((xIndex[position.Left], -yIndex[position.Right] - 1, roles[clause.Left]));
        }
        Relation Point(long x, long yNumerator) => new(new Relation(weave.ColumnPitch, new CarrierLocation(metric, x)),
            new Relation(weave.RowPitch, new Relation(new CarrierLocation(metric, yNumerator), Relation.Point(metric, 2))));
        var railSurface = new Relation(S(
                new Relation(Point(0, -2 * rows.Length - 3), Point(columns.Length, -2 * rows.Length - 3)),
                new Relation(Point(0, 3), Point(columns.Length, 3))), Relation.Segment(metric, 0, 1));
        var observation = new Relation(new Relation(weave.Surface, new Relation(calibration, railSurface)), new Relation(read, exchange));
        var caption = $"Ink {E(first).Length}/{E(weave.Cells).Length} cells; half-period color exchange VERIFIED; {E(regions).Length} role regions";
        return new(name, "Native seed rows + retained finite-law role exchange; uniform cell view", weave.Surface, observation,
            columns.Length, -rows.Length, 0, 1, Blocks(cells), [], [-rows.Length - 1.5, 1.5],
            new
            {
                Name = name, Kind = "woven field", Source = Counts(weave.Surface), Columns = columns.Length, Rows = rows.Length,
                Cells = E(weave.Cells).Length, First = E(first).Length, Second = E(second).Length,
                Regions = E(regions).Length, Interfaces = E(interfaces).Length, Unresolved = E(pending).Length,
                RoleRegions = RegionCounts(weave.Partition, regions),
                Exchange = Exchange(weave.Partition, "half-period translation under uniform calibration", exchange),
                GenerationReads = E(weave.GenerationEvidence).Length, ObservationNodes = Traverse(observation).Count()
            }, caption);
    }

    private static object Exchange(QualitativePartition partition, string name, Relation read)
    {
        if (!partition.TryReadExchange(read, out var map, out var matched, out var unmatched, out var pending))
            throw new InvalidOperationException("Expected retained exchange evidence.");
        return new { Name = name, Passed = partition.TryReadExchangePassed(read), MapRows = E(map).Length,
            Matched = E(matched).Length, Unmatched = E(unmatched).Length, Unresolved = E(pending).Length };
    }

    private static object RegionCounts(QualitativePartition partition, Relation regions)
    {
        var first = 0; var second = 0;
        foreach (var region in E(regions).Cast<Relation>())
            if (partition.TryReadRegion(region, out var role, out _, out _))
            {
                if (ReferenceEquals(role, partition.RolePair.Left)) first++;
                else if (ReferenceEquals(role, partition.RolePair.Right)) second++;
            }
        return new { First = first, Second = second };
    }

    private static IReadOnlyList<Block> Blocks(List<(int X, int Y, bool First)> cells)
    {
        var rectangles = new List<Block>(); var active = new Dictionary<(int X, int Width, bool First), int>();
        foreach (var row in cells.GroupBy(cell => cell.Y).OrderBy(row => row.Key))
        {
            var next = new Dictionary<(int X, int Width, bool First), int>(); var ordered = row.OrderBy(cell => cell.X).ToArray();
            for (var start = 0; start < ordered.Length;)
            {
                var end = start + 1;
                while (end < ordered.Length && ordered[end].X == ordered[end - 1].X + 1 && ordered[end].First == ordered[start].First) end++;
                var key = (ordered[start].X, end - start, ordered[start].First);
                if (active.TryGetValue(key, out var index) && rectangles[index].Y + rectangles[index].Height == row.Key)
                    rectangles[index] = rectangles[index] with { Height = rectangles[index].Height + 1 };
                else { index = rectangles.Count; rectangles.Add(new(key.Item1, row.Key, key.Item2, 1, key.Item3)); }
                next[key] = index; start = end;
            }
            active = next;
        }
        return rectangles;
    }

    private static string Draw(IReadOnlyList<Picture> pictures, int columns, string title, string subtitle)
    {
        const int width = 1460, header = 92, row = 164;
        var height = header + ((pictures.Count + columns - 1) / columns) * row + 36;
        var svg = new StringBuilder($"<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"{width}\" height=\"{height}\" viewBox=\"0 0 {width} {height}\" role=\"img\" aria-label=\"{H(title)}\"><style>:root{{--a:#20231f;--b:#f4efdf}}.cells{{shape-rendering:crispEdges}}.skeleton{{display:none}}</style><rect width=\"100%\" height=\"100%\" fill=\"var(--b)\"/><g font-family=\"system-ui,sans-serif\" fill=\"var(--a)\">");
        Text(20, 31, title, 25, "600"); Text(20, 59, subtitle, 13);
        var panelWidth = (width - 24.0) / columns;
        for (var i = 0; i < pictures.Count; i++)
        {
            var picture = pictures[i]; var left = 12 + (i % columns) * panelWidth; var top = header + (i / columns) * row;
            var panel = $"{columns}-{i}";
            svg.Append($"<rect x=\"{F(left)}\" y=\"{top}\" width=\"{F(panelWidth - 8)}\" height=\"154\" rx=\"10\" fill=\"none\" stroke=\"#b9b5a8\"/>");
            Text(left + 12, top + 21, picture.Name, columns == 1 ? 16 : 13, "600");
            Text(left + 12, top + 40, columns == 1 ? picture.Detail : $"Band/gap interpretation; {picture.Band / 2:0.#}/{4 - picture.Band / 2:0.#}", 11);
            var low = Math.Min(picture.Low, picture.Rails.Count == 0 ? picture.Low : picture.Rails.Min() - picture.Band / 2);
            var high = Math.Max(picture.High, picture.Rails.Count == 0 ? picture.High : picture.Rails.Max() + picture.Band / 2);
            var scale = 88 / (high - low); var available = panelWidth - 32;
            var periods = Math.Clamp((int)Math.Floor(available / (picture.Period * scale)), 1, 18);
            if (periods * picture.Period * scale > available) scale = available / (periods * picture.Period);
            var full = periods * picture.Period * scale; var ox = left + (panelWidth - 8 - full) / 2; var oy = top + 49;
            svg.Append($"<defs><clipPath id=\"clip-{panel}\"><rect x=\"{F(ox)}\" y=\"{oy}\" width=\"{F(full)}\" height=\"90\"/></clipPath></defs><g clip-path=\"url(#clip-{panel})\">");
            svg.Append("<g class=\"cells\">");
            for (var repeat = 0; repeat < periods; repeat++)
                foreach (var block in picture.Blocks)
                    svg.Append($"<rect x=\"{F(ox + (block.X + repeat * picture.Period) * scale)}\" y=\"{F(oy + (high - block.Y - block.Height) * scale)}\" width=\"{F(block.Width * scale)}\" height=\"{F(block.Height * scale)}\" fill=\"var(--{(block.First ? 'a' : 'b')})\"/>");
            svg.Append("</g>");
            foreach (var y in picture.Rails)
                svg.Append($"<path d=\"M{F(ox)},{F(oy + (high - y) * scale)}h{F(full)}\" stroke=\"var(--a)\" stroke-width=\"{F(picture.Band * scale)}\"/>");
            if (picture.Skeleton.Count != 0)
                for (var repeat = -1; repeat <= periods; repeat++)
                    svg.Append($"<polyline class=\"skeleton\" points=\"{string.Join(' ', picture.Skeleton.Select(point => F(ox + (point.X + repeat * picture.Period) * scale) + ',' + F(oy + (high - point.Y) * scale)))}\" fill=\"none\" stroke=\"#cc2476\" stroke-width=\"1\"/>");
            svg.Append("</g>");
            Text(left + 12, top + 147, columns == 1 ? picture.Caption :
                columns == 2 ? picture.BoundaryCaption ?? ShortCaption(picture) : ShortCaption(picture), columns == 1 ? 11 : 10);
        }
        Text(20, height - 12, "Black and white are two actual roles. Rails frame the selected field and are excluded from its exchange test.", 12);
        return svg.Append("</g></svg>").ToString();
        void Text(double x, double y, string value, int size, string weight = "400") =>
            svg.Append($"<text x=\"{F(x)}\" y=\"{F(y)}\" font-size=\"{size}\" font-weight=\"{weight}\">{H(value)}</text>");
    }

    private static string ShortCaption(Picture picture) => picture.Caption.Split(';')[0] + "; " + picture.Caption.Split(';')[1];
    private static string Html(string gallery, string styles, string centering) =>
        "<!doctype html><html lang=\"en\"><meta charset=\"utf-8\"><meta name=\"viewport\" content=\"width=device-width,initial-scale=1\"><title>Qualitative pattern shaping</title>" +
        "<style>body{margin:0;background:#f4efdf;color:#20231f;font:16px system-ui}main{max-width:1460px;margin:auto;padding:24px}p{max-width:1100px;line-height:1.6}svg{display:block;width:100%;height:auto;margin:24px 0}.controls{display:flex;gap:26px;position:sticky;top:0;background:#f4efdf;padding:15px;z-index:1}.invert svg{--a:#f4efdf;--b:#20231f}.show-skeleton .skeleton{display:inline}a{color:#5e4b8b}details{margin:24px 0}</style>" +
        "<main><h1>Pattern shaping with qualitative structure</h1><p>These constructions are inspired by the supplied fret and meander references. They are deliberately authored programs, with separate drawing interpretations. Turns, repeated reaches, starting phases and two-role cell relations remain inspectable; no random search chooses these examples.</p>" +
        "<div class=\"controls\"><label><input type=\"checkbox\" onchange=\"document.body.classList.toggle('invert',this.checked)\"> Invert the view</label><label><input type=\"checkbox\" onchange=\"document.body.classList.toggle('show-skeleton',this.checked)\"> Show causal centerlines</label></div>" +
        gallery + "<p>The first eight bands come from compact stroke programs. Their elementary band and gap share one native extent; the measured view selects pitch four and band two. Similar visual weight does not establish exact color exchange. The final three fields are generated with a retained role-exchange law and certify an actual half-shift bijection: every dark cell maps to light and every light cell to dark. Uniform cell calibration makes their areas equal. The borders are separate frame structure.</p>" +
        "<details open><summary>Shape the proportions: thin, equal-width, heavy</summary>" + styles + "</details>" +
        "<details open><summary>Shape the placement by choosing an existing starting phase</summary>" + centering +
        "<p>This is a change of phase-origin bindings in the same network, observed in a fixed strip. It does not numerically translate the original source or modify its emitted words. Exact boundary-gap spans remain in the observation witness.</p></details>" +
        "<p>Color inversion here is a display choice. The exchange certificate is a distinct native cell correspondence; neither is Gojo element duality. Ribbon geometry remains a selected measured rectilinear interpretation. This is not a general contour, cavity, learning or economic-bidding implementation.</p>" +
        "<p><a href=\"gallery.svg\">Overview SVG</a> · <a href=\"styles.svg\">Style comparisons</a> · <a href=\"centering.svg\">Phase-origin comparison</a> · <a href=\"diagnostics.json\">Diagnostics</a></p></main></html>";

    private static Dictionary<IElement, bool> LabelIndex(Relation first, Relation second)
    {
        var result = new Dictionary<IElement, bool>(ReferenceEqualityComparer.Instance);
        foreach (var row in E(first).Cast<Relation>()) result.Add(row.Left, true);
        foreach (var row in E(second).Cast<Relation>()) result.Add(row.Left, false);
        return result;
    }
    private static Relation Calibrate(FriezeFixture fixture) =>
        FriezeRealization.CreateCalibration(fixture, FriezeRealization.DefaultLengths(fixture, 4, 4));
    private static long Start(Relation span) => ((CarrierLocation)((Relation)span.Left).Left).Ticks;
    private static int MoveCount(Relation words) => E(words).OfType<Relation>().Sum(word => E(word).Length);
    private static object Counts(IElement source)
    {
        var elements = Traverse(source).ToArray();
        return new { Elements = elements.Length, Carriers = elements.Count(element => element is Carrier), Locations = elements.Count(element => element is CarrierLocation) };
    }
    private static IEnumerable<IElement> Traverse(IElement root)
    {
        var seen = new HashSet<IElement>(ReferenceEqualityComparer.Instance); var pending = new Stack<IElement>(); pending.Push(root);
        while (pending.TryPop(out var value))
        { if (!seen.Add(value)) continue; yield return value; if (value is Relation relation) { pending.Push(relation.Left); pending.Push(relation.Right); } }
    }
    private static string F(double value) => value.ToString("0.###", CultureInfo.InvariantCulture);
    private static string H(string value) => WebUtility.HtmlEncode(value);
    private static Relation S(params IElement[] values) => StructuralConstraint.Closed(values);
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
}
