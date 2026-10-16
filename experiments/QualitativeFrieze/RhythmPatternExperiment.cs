using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;
using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;

namespace Gojo.Experiments.QualitativeFrieze;

/// <summary>Presentation and bounded comparison of native phase compositions,
/// exact square-brush footprints and descriptive process features.</summary>
public static class RhythmPatternExperiment
{
    private static readonly IEqualityComparer<IElement> Identity = ReferenceEqualityComparer.Instance;
    private static readonly string[] Colors = ["#197b85", "#bc507b", "#a57520", "#69549b", "#348261", "#b65b38", "#587bb1", "#777038"];
    private sealed record Spec(string Name, FriezeFixture Fixture, int Rounds, Relation Source, IReadOnlyList<(string Name, Relation Port)> Ports);
    private sealed record Shape(Relation Sample, IReadOnlyList<(double X, double Y)> Points, int Color);
    private sealed record Lane(string Name, Relation Port, IReadOnlyList<string> Emissions);
    private sealed record Picture(string Name, long Horizontal, long Vertical, long Width, double Period,
        IReadOnlyList<Shape> Shapes, IReadOnlyList<Lane> Lanes, FriezeWordFeatures.Summary Features,
        string Coordination, string Symmetry, Relation Source, Relation View, object Diagnostic);

    public static int Run(string[] arguments)
    {
        var output = Path.Combine("experiments", "QualitativeFrieze", "results", "rhythms");
        for (var i = 0; i < arguments.Length; i++)
            if (arguments[i] == "--output" && i + 1 < arguments.Length) output = arguments[++i];
            else throw new ArgumentException("Usage: --rhythms [--output DIRECTORY]");
        Directory.CreateDirectory(output);
        var library = new RhythmPatternLibrary();
        var specs = library.Entries.Select(entry => new Spec(entry.Name, entry.Fixture, entry.PeriodRounds, entry.Surface,
            entry.Components.Select(component => (component.Name, component.Port)).ToArray())).ToList();
        var meander = new PatternShapingLibrary(library.Frame, library.HorizontalReach, library.VerticalReach).Patterns[2];
        specs.Add(new("Rectilinear folded key / same brush", meander.Fixture, meander.PhaseCount, meander.Surface,
            [("Ordered sequence", meander.Fixture.RootPort)]));
        var pictures = new List<Picture>(); var styles = new List<Picture>();
        foreach (var spec in specs)
        {
            var trace = spec.Fixture.Run(spec.Rounds);
            var features = new FriezeWordFeatures(spec.Fixture, trace, Slots(96));
            var coordination = new FriezeCoordinationFeatures(spec.Fixture, trace);
            if (features.Describe() is { Unresolved: not 0 } or { Violations: not 0 } or { LegalityUnresolved: not 0 })
                throw new InvalidOperationException($"The authored process did not settle its selected legality profile: {spec.Name}");
            pictures.Add(Interpret(spec, trace, features, coordination, 4, 4, 2));
            styles.Add(Interpret(spec, trace, features, coordination, 4, 4, 1));
            styles.Add(Interpret(spec, trace, features, coordination, 4, 4, 3));
            styles.Add(Interpret(spec, trace, features, coordination, 4, 8, 2));
        }
        var controls = Controls(library);
        var source = new Relation(library.Surface, S(pictures.Select(picture => (IElement)picture.Source)
            .Concat(controls.Select(picture => (IElement)picture.Source)).ToArray()));
        if (Traverse(source).Any(value => value is Carrier or CarrierLocation))
            throw new InvalidOperationException("Numeric interpretation contaminated the qualitative source.");
        var main = Draw(pictures, true, "Layered rhythms, diagonal ribbons and emitted-word colour", 2, "main");
        var variants = Draw(styles, false, "Interpretation controls: thin, thick, vertically stretched", 3, "styles");
        var counterexamples = Draw(controls, true, "Descriptive features do not settle aesthetic preference", 2, "controls");
        File.WriteAllText(Path.Combine(output, "gallery.svg"), main);
        File.WriteAllText(Path.Combine(output, "styles.svg"), variants);
        File.WriteAllText(Path.Combine(output, "controls.svg"), counterexamples);
        File.WriteAllText(Path.Combine(output, "gallery.html"), Html(main, variants, counterexamples));
        File.WriteAllText(Path.Combine(output, "diagnostics.json"), JsonSerializer.Serialize(new
        {
            Schema = "qualitative-rhythm-patterns-v1", Native = Counts(source),
            Pictures = pictures.Select(picture => picture.Diagnostic), Styles = styles.Select(picture => picture.Diagnostic),
            Controls = controls.Select(picture => picture.Diagnostic),
            Notes = new[] { "Feature counts are descriptive projections, not a human-preference model.",
                "Square-brush sweep is exact; no finite-cell area, Euclidean constant-width or polygon-union claim.",
                "Several native contributors share a sample; event execution remains a bounded FIFO cascade.",
                "Colours encode local emitted-word classes in this view, not material ownership or independent cursors." }
        }, new JsonSerializerOptions { WriteIndented = true }));
        foreach (var picture in pictures.Concat(controls)) Console.WriteLine(JsonSerializer.Serialize(picture.Diagnostic));
        Console.WriteLine($"Wrote rhythm comparison to {Path.GetFullPath(output)}.");
        return 0;
    }

    private static List<Picture> Controls(RhythmPatternLibrary library)
    {
        var frame = library.Frame; var r = QualitativeMotionFrame.Move(frame.Right, library.HorizontalReach);
        var u = QualitativeMotionFrame.Move(frame.Up, library.VerticalReach); var d = QualitativeMotionFrame.Move(frame.Down, library.VerticalReach);
        var a = S(r, u); var b = S(r, d); var result = new List<Picture>();
        foreach (var (name, words) in new[] { ("Constant drift / maximum reuse", S(S(r))),
            ("Alternating words / A B A B", S(a, b, a, b)), ("Grouped words / A A B B", S(a, a, b, b)) })
        {
            var fixture = FriezeFixture.Sequence(frame, words, name); var trace = fixture.Run(E(words).Length);
            var spec = new Spec(name, fixture, E(words).Length, fixture.Surface, [("Word sequence", fixture.RootPort)]);
            result.Add(Interpret(spec, trace, new FriezeWordFeatures(fixture, trace, Slots(96)), new FriezeCoordinationFeatures(fixture, trace), 4, 4, 2));
        }
        return result;
    }

    private static Picture Interpret(Spec spec, Relation trace, FriezeWordFeatures features,
        FriezeCoordinationFeatures coordination, long horizontal, long vertical, long width)
    {
        var fixture = spec.Fixture;
        var calibration = FriezeRealization.CreateCalibration(fixture, FriezeRealization.DefaultLengths(fixture, horizontal, vertical));
        var supplier = new DiagonalRibbonSupplier(fixture, new Placeholder());
        var field = supplier.Interpret(trace, calibration, width);
        if (!supplier.TryReadResult(field.Surface, out _, out _, out _, out _, out _, out var pending) || E(pending).Length != 0)
            throw new InvalidOperationException($"The square-brush interpretation did not settle: {spec.Name}");
        var geometry = FriezeMeasuredRules.Describe(field.Geometry);
        if (geometry.UnresolvedCount != 0 || geometry.OverlapCount != 0 || geometry.CrossingCount != 0)
            throw new InvalidOperationException($"A selected no-crossing/no-retracing centerline read failed: {spec.Name}");
        var paletteRows = new List<IElement>(); var memberships = new List<IElement>();
        var sampleColors = new Dictionary<IElement, int>(Identity); var groupIndex = 0;
        foreach (var group in E(features.Groups).Cast<Relation>())
        {
            var color = new Placeholder(); paletteRows.Add(new Relation(group, color));
            features.TryReadGroup(group, out _, out var members);
            foreach (var member in E(members).Cast<Relation>())
            {
                var word = (Relation)member.Left; sampleColors.Add(word.Left, groupIndex);
                memberships.Add(new Relation(word.Left, group));
            }
            groupIndex++;
        }
        var palette = new Relation(new Relation(features.Groups, S(paletteRows.ToArray())), S(memberships.ToArray()));
        var shapes = new List<Shape>(); var copies = new List<IElement>();
        foreach (var polygon in E(field.Polygons).Cast<Relation>())
        {
            if (!DiagonalRibbonSupplier.TryReadPolygon(polygon, out var stroke, out _, out var originalVertices, out _) ||
                !FriezeRealization.TryReadSamplePoints(stroke, out var sample, out _, out _) || !sampleColors.TryGetValue(sample, out var color))
                throw new InvalidOperationException("A filled footprint lost its actual sample/word-class correspondence.");
            var rawPoints = VertexRead(originalVertices);
            // Only copies meeting the selected three-period display window.
            // This is a bounded render selection over exact supplied support,
            // and does not truncate or mutate the original native polygon.
            var firstCopy = (long)Math.Ceiling(-rawPoints.Max(point => point.X) / geometry.PeriodX);
            var lastCopy = (long)Math.Floor((3.0 * geometry.PeriodX - rawPoints.Min(point => point.X)) / geometry.PeriodX);
            if (lastCopy - firstCopy > 64) throw new InvalidOperationException("The display copy budget is exceeded.");
            for (var repeat = firstCopy; repeat <= lastCopy; repeat++)
            {
                if (!DiagonalRibbonSupplier.TryTranslatePolygon(polygon, repeat, out var copy) ||
                    !DiagonalRibbonSupplier.TryReadPolygon(copy, out _, out _, out var vertices, out _))
                    throw new InvalidOperationException("A periodic footprint copy did not preserve its source.");
                copies.Add(copy); shapes.Add(new(sample, VertexRead(vertices), color));
            }
        }
        var lanes = ReadLanes(spec, trace, out var laneRead);
        if (coordination.Describe().Unresolved != 0) throw new InvalidOperationException("The authored coordination read did not settle.");
        var conditioned = new List<object>(); var focused = new List<IElement>(); var descriptions = new List<string>();
        foreach (var row in E(coordination.Conditioned).Cast<Relation>())
        {
            if (!coordination.TryReadConditioned(row, out var axis, out var direction, out var firstPort, out var secondPort, out var same, out var opposed))
                throw new InvalidOperationException("The face-conditioned read lost its native context.");
            var first = spec.Ports.Single(port => ReferenceEquals(port.Port, firstPort)).Name;
            var second = spec.Ports.Single(port => ReferenceEquals(port.Port, secondPort)).Name;
            var face = ReferenceEquals(direction, fixture.Frame.Up) ? "up" : ReferenceEquals(direction, fixture.Frame.Down) ? "down" :
                ReferenceEquals(direction, fixture.Frame.Right) ? "right" : "left";
            conditioned.Add(new { FirstPort = first, SecondPort = second, FirstFace = face,
                Same = E(same).Length, Opposed = E(opposed).Length });
            if (spec.Ports.Count > 2 && ReferenceEquals(firstPort, spec.Ports[1].Port) && ReferenceEquals(secondPort, spec.Ports[^1].Port))
            { focused.Add(row); descriptions.Add($"{face}: {E(same).Length} same / {E(opposed).Length} opposed"); }
        }
        var coordinationFocus = new Relation(coordination.Evaluate(), S(focused.ToArray()));
        var coordinationCaption = focused.Count == 0 ? $"Cross-port same-axis face pairs: {coordination.Describe().SameFacePairs} same / {coordination.Describe().OpposedFacePairs} opposed." :
            $"First/last vertical contributors, conditioned on first face — {string.Join("; ", descriptions)}.";
        var source = new Relation(spec.Source, new Relation(new Relation(features.Evaluate(), coordinationFocus), new Relation(palette, laneRead)));
        var view = new Relation(new Relation(source, field.Surface), S(copies.ToArray()));
        var diagnostic = new
        {
            Name = spec.Name, RootRounds = spec.Rounds, Components = spec.Ports.Count,
            PhaseParticipants = fixture.Components.Sum(component => E(component.Domain).Length), Features = features.Describe(),
            Coordination = coordination.Describe(), Conditioned = conditioned,
            Calibration = new { Horizontal = horizontal, Vertical = vertical, SquareNibWidth = width },
            Strokes = geometry.StrokeCount, Polygons = E(field.Polygons).Length, Copies = copies.Count,
            CenterlinePeriodX = geometry.PeriodX, CenterlineFundamentalPeriodX = geometry.FundamentalPeriodX,
            CenterlineOverlaps = geometry.OverlapCount, CenterlineCrossings = geometry.CrossingCount,
            CenterlineSymmetries = geometry.VerifiedSymmetryNames, Source = Counts(source), View = Counts(view)
        };
        return new(spec.Name, horizontal, vertical, width, geometry.PeriodX, shapes, lanes, features.Describe(), coordinationCaption,
            string.Join(", ", geometry.VerifiedSymmetryNames), source, view, diagnostic);
    }

    private static IReadOnlyList<(double X, double Y)> VertexRead(Relation vertices) => E(vertices).Cast<Relation>().Select(point =>
    {
        if (!DiagonalRibbonSupplier.TryReadPoint(point, out var xn, out var xd, out var yn, out var yd))
            throw new InvalidOperationException("The exact footprint vertex did not read.");
        return ((double)xn / xd, (double)yn / yd);
    }).ToArray();

    private static IReadOnlyList<Lane> ReadLanes(Spec spec, Relation trace, out Relation witness)
    {
        if (!spec.Fixture.TryReadTrace(trace, out _, out _, out _, out _, out var samples, out _))
            throw new InvalidOperationException("The source trace did not read.");
        var rows = new List<IElement>(); var displays = spec.Ports.Select(_ => new List<string>()).ToArray();
        foreach (var sample in E(samples).Cast<Relation>())
        {
            if (!FriezeFixture.TryReadSample(sample, out var cause, out _, out _, out _, out var applications, out _) ||
                cause is not Relation step || !spec.Fixture.Network.TryReadStep(step, out _, out _, out _, out var accepted, out _, out _))
                throw new InvalidOperationException("Expected grouped root samples for the source lane view.");
            for (var i = 0; i < spec.Ports.Count; i++)
            {
                var port = spec.Ports[i].Port;
                var receipts = E(accepted).OfType<Relation>().Where(receipt => receipt.Left is Relation owner && ReferenceEquals(owner.Right, port.Right)).ToArray();
                if (receipts.Length != 1) throw new InvalidOperationException("The selected lane view requires one actual firing per port and root sample.");
                var apps = E(applications).OfType<Relation>().Where(app => app.Left is Relation owner && receipts.Any(receipt => ReferenceEquals(receipt, owner.Left))).ToArray();
                rows.Add(new Relation(new Relation(sample, port), new Relation(S(receipts), S(apps))));
                var glyphs = new List<string>();
                foreach (var app in apps)
                    if (app.Right is Relation move && spec.Fixture.Frame.Direction(move, out var axis, out var forward, out _))
                        glyphs.Add(ReferenceEquals(axis, spec.Fixture.Frame.Horizontal) ? forward ? "→" : "←" : forward ? "↑" : "↓");
                displays[i].Add(glyphs.Count == 0 ? "·" : string.Concat(glyphs));
            }
        }
        witness = new Relation(new Relation(spec.Fixture.Surface, trace), S(rows.ToArray()));
        return spec.Ports.Select((port, i) => new Lane(port.Name, port.Port, displays[i])).ToArray();
    }

    private static string Draw(IReadOnlyList<Picture> pictures, bool paired, string title, int columns, string prefix)
    {
        const int width = 1460, header = 90; var rowHeight = paired ? 350 : 220;
        var rows = paired ? pictures.Count : (pictures.Count + columns - 1) / columns;
        var height = header + rows * rowHeight + 45;
        var svg = new StringBuilder($"<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"{width}\" height=\"{height}\" viewBox=\"0 0 {width} {height}\"><style>:root{{--paper:#f4efdf;--ink:#202d34}}.ink{{fill:var(--ink)}}.mono .colored{{fill:var(--ink)!important}}</style><rect width=\"100%\" height=\"100%\" fill=\"var(--paper)\"/><g font-family=\"system-ui,sans-serif\" fill=\"var(--ink)\">");
        Text(22, 34, title, 25, "650");
        Text(22, 62, "Exact swept-square footprints; shared native source, explicit sampling, calibrated view. Colours show emitted-word classes.", 14);
        for (var i = 0; i < pictures.Count; i++)
        {
            var picture = pictures[i]; var left = paired ? 14 : 14 + (i % columns) * 480; var top = header + (paired ? i : i / columns) * rowHeight;
            var cardWidth = paired ? 1432 : 468;
            svg.Append($"<rect x=\"{left}\" y=\"{top}\" width=\"{cardWidth}\" height=\"{rowHeight - 10}\" rx=\"10\" fill=\"none\" stroke=\"#c6c6b9\"/>");
            Text(left + 14, top + 25, picture.Name, paired ? 17 : 13, "600");
            Text(left + 14, top + 47, paired ?
                $"{picture.Features.Samples} samples / {picture.Features.DistinctWords} emitted words / {picture.Features.ChangedWordTransitions} changed-word transitions / {picture.Features.CurrentWordContextsWithAlternatives} current-word contexts with alternatives" :
                $"H {picture.Horizontal}, V {picture.Vertical}, square brush {picture.Width}", paired ? 12 : 11);
            if (paired)
            {
                Plot(picture, left + 12, top + 62, 692, 138, false, $"{prefix}-{i}-a");
                Plot(picture, left + 722, top + 62, 692, 138, true, $"{prefix}-{i}-b");
                Text(left + 17, top + 217, $"Same geometry in both views: H {picture.Horizontal}, V {picture.Vertical}, brush {picture.Width}. Source contributors below; · is an explicit pause.", 11);
                var labelWidth = 242.0; var available = 1145.0;
                for (var lane = 0; lane < picture.Lanes.Count; lane++)
                {
                    var item = picture.Lanes[lane]; var y = top + 240 + lane * 18;
                    Text(left + 17, y, item.Name, 11);
                    var stepWidth = Math.Min(40, available / item.Emissions.Count);
                    for (var sample = 0; sample < item.Emissions.Count; sample++)
                    {
                        var x = left + labelWidth + sample * stepWidth;
                        svg.Append($"<rect x=\"{F(x)}\" y=\"{y - 12}\" width=\"{F(stepWidth - 2)}\" height=\"16\" rx=\"2\" fill=\"{Colors[lane % Colors.Length]}\" opacity=\".10\"/>");
                        Text(x + 2, y, item.Emissions[sample], item.Emissions[sample].Length > 2 ? 9 : 11, fill: Colors[lane % Colors.Length]);
                    }
                }
                Text(left + 17, top + rowHeight - 42, picture.Coordination, 11);
                Text(left + 17, top + rowHeight - 24, "Centerline symmetries: " + picture.Symmetry + ". No centerline overlap/crossing in this selected view.", 11);
            }
            else Plot(picture, left + 12, top + 66, cardWidth - 24, 134, true, $"{prefix}-{i}");
        }
        Text(22, height - 16, "Word classes, phase identities, sampled path, occupied brush support and aesthetic judgment remain separate reads.", 12);
        return svg.Append("</g></svg>").ToString();
        void Plot(Picture picture, double x, double y, double w, double h, bool colored, string id)
        {
            var low = picture.Shapes.SelectMany(shape => shape.Points).Min(point => point.Y);
            var high = picture.Shapes.SelectMany(shape => shape.Points).Max(point => point.Y);
            var scale = Math.Min(w / (picture.Period * 3), (h - 12) / Math.Max(1, high - low));
            var shownWidth = picture.Period * 3 * scale; var ox = x + (w - shownWidth) / 2; var oy = y + (h - (high - low) * scale) / 2;
            svg.Append($"<defs><clipPath id=\"{id}\"><rect x=\"{F(ox)}\" y=\"{F(y)}\" width=\"{F(shownWidth)}\" height=\"{F(h)}\"/></clipPath></defs><g clip-path=\"url(#{id})\">");
            foreach (var shape in picture.Shapes)
                svg.Append($"<polygon class=\"{(colored ? "colored" : "ink")}\" points=\"{string.Join(' ', shape.Points.Select(point => F(ox + point.X * scale) + ',' + F(oy + (high - point.Y) * scale)))}\" fill=\"{(colored ? Colors[shape.Color % Colors.Length] : "var(--ink)")}\"/>");
            svg.Append("</g>");
        }
        void Text(double x, double y, string value, int size, string weight = "400", string? fill = null) =>
            svg.Append($"<text x=\"{F(x)}\" y=\"{F(y)}\" font-size=\"{size}\" font-weight=\"{weight}\"{(fill is null ? "" : $" fill=\"{fill}\"")}>{WebUtility.HtmlEncode(value)}</text>");
    }

    private static string Html(string main, string styles, string controls) =>
        "<!doctype html><html lang=\"en\"><meta charset=\"utf-8\"><meta name=\"viewport\" content=\"width=device-width,initial-scale=1\"><title>Rhythm and diagonal pattern experiments</title><style>body{margin:0;background:#f4efdf;color:#202d34;font:16px system-ui}main{max-width:1460px;margin:auto;padding:24px}p{max-width:1120px;line-height:1.6}svg{width:100%;height:auto;display:block;margin:20px 0}.controls{position:sticky;top:0;background:#f4efdf;padding:12px;z-index:1}.mono .colored{fill:var(--ink)!important}details{margin:24px 0}a{color:#176d79}</style><main><h1>Rhythm, phase and thick diagonal patterns</h1><p>Separate native phase cycles contribute to one root sample. The slow excursion, faster triangular detail and optional middle layer remain visible below their summed path. The pulse examples reinterpret authored onset/rest slots as rise/return pairs. They do not read audio or claim a listener's perceived meter.</p><div class=\"controls\"><label><input type=\"checkbox\" onchange=\"document.body.classList.toggle('mono',this.checked)\">Suppress word-class colours</label></div>" + main +
        "<p>These are exact square-brush swept polygons, including diagonals and native half-tick corners. Width is measured along the chosen horizontal/vertical nib axes. A diagonal's perpendicular thickness consequently differs from an axis-aligned stroke's. This experiment preserves the whole footprints and does not claim an exact cell-area partition or a boolean polygon union. Overlapping colours use documented drawing order; colour mixing is a separate possible interpretation.</p><details><summary>Change one interpretation: thin / thick / vertically stretched</summary>" + styles +
        "</details><details open><summary>Counterexamples for fitness: constant, alternating and grouped words</summary>" + controls +
        "<p>Maximum reuse and symmetry can select the constant line. A B A B and A A B B have equal word counts but different transitions and coarse prediction contexts. These descriptive reads support several possible preferences; they do not supply a universal beauty or groove score. Current-word alternatives are not entropy, randomness or human surprise.</p></details><p>The source remains pre-numeric. Time units, aspect ratio, brush dimensions and colours enter through explicit interpretations. The event interpreter uses a bounded FIFO cascade; grouping its contributions does not prove physical simultaneity or CPU parallel execution. Native phase, causal and cancellation evidence remains available.</p><p><a href=\"gallery.svg\">Main SVG</a> · <a href=\"styles.svg\">Style controls</a> · <a href=\"controls.svg\">Feature counterexamples</a> · <a href=\"diagnostics.json\">Diagnostics</a> · <a href=\"../../../../Gojo/docs/GojoPatternFitnessResearch.md\">Research note</a></p></main></html>";
    private static Relation Slots(int count) => S(Enumerable.Range(0, count).Select(_ => (IElement)new Placeholder()).ToArray());
    private static object Counts(IElement source)
    {
        var nodes = Traverse(source).ToArray();
        return new { Elements = nodes.Length, Carriers = nodes.Count(value => value is Carrier), Locations = nodes.Count(value => value is CarrierLocation) };
    }
    private static IEnumerable<IElement> Traverse(IElement source)
    {
        var seen = new HashSet<IElement>(Identity); var stack = new Stack<IElement>(); stack.Push(source);
        while (stack.TryPop(out var node))
        { if (!seen.Add(node)) continue; yield return node; if (node is Relation relation) { stack.Push(relation.Left); stack.Push(relation.Right); } }
    }
    private static string F(double value) => value.ToString("0.###", CultureInfo.InvariantCulture);
    private static Relation S(params IElement[] values) => StructuralConstraint.Closed(values);
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
}
