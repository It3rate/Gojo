using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;
using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;

namespace Gojo.Experiments.QualitativeFrieze;

/// <summary>Selected views of rhythm-to-phrase correspondences. Source order,
/// phrase expansion, execution and measured appearance remain separate reads.</summary>
public static class RhythmFriezeExperiment
{
    private static readonly string[] Colors = ["#187e87", "#aa4d75", "#987126", "#63559a", "#39805f", "#b45f3c"];
    private sealed record Shape(Relation Sample, IReadOnlyList<(double X, double Y)> Vertices, int Color);
    private sealed record Slot(string Beat, string Word, int Color, int Strokes);
    private sealed record Picture(string Name, double Period, double Low, double High, IReadOnlyList<Shape> Shapes,
        IReadOnlyList<Slot> Slots, Relation Source, Relation View, object Diagnostic, string Caption);

    public static int Run(string[] arguments)
    {
        var output = Path.Combine("experiments", "QualitativeFrieze", "results", "rhythm-friezes");
        for (var i = 0; i < arguments.Length; i++)
            if (arguments[i] == "--output" && i + 1 < arguments.Length) output = arguments[++i];
            else throw new ArgumentException("Usage: --rhythm-friezes [--output DIRECTORY]");
        Directory.CreateDirectory(output);
        var library = new RhythmFriezeLibrary();
        var pictures = library.Entries.Select(entry =>
        {
            Console.WriteLine($"Interpreting {entry.Name} ({entry.PhaseCount} stroke phases).");
            return Interpret(entry);
        }).ToArray();
        var source = new Relation(library.Surface, S(pictures.Select(picture => (IElement)picture.Source).ToArray()));
        if (Traverse(source).Any(element => element is Carrier or CarrierLocation))
            throw new InvalidOperationException("The source interpretation introduced a numeric measurement.");
        var svg = Draw(pictures);
        File.WriteAllText(Path.Combine(output, "gallery.svg"), svg);
        File.WriteAllText(Path.Combine(output, "phrases.svg"), svg.Replace("<svg xmlns=", "<svg class=\"show-phrases\" xmlns="));
        File.WriteAllText(Path.Combine(output, "gallery.html"), Html(svg, pictures));
        File.WriteAllText(Path.Combine(output, "diagnostics.json"), JsonSerializer.Serialize(new
        {
            Schema = "qualitative-rhythm-friezes-v1", Native = Counts(source),
            Pictures = pictures.Select(picture => picture.Diagnostic),
            Notes = new[] {
                "Onset order is preserved. Expanding slots into unequal stroke counts does not preserve musical timing or spatial beat ratios.",
                "Phrase grammar and square brush are authored interpretations; rhythm alone does not imply their geometry.",
                "Fill is exact in each advertised finite strip at H4/V4, band2/gap2. Different strip heights are not one common area objective.",
                "No random search, audio recognition or preference learning is claimed. Colours identify local selected phrase identities." }
        }, new JsonSerializerOptions { WriteIndented = true }));
        foreach (var picture in pictures) Console.WriteLine(JsonSerializer.Serialize(picture.Diagnostic));
        Console.WriteLine($"Wrote rhythm-to-frieze comparison to {Path.GetFullPath(output)}.");
        return 0;
    }

    private static Picture Interpret(RhythmFriezeEntry entry)
    {
        if (E(entry.Unresolved).Length != 0) throw new InvalidOperationException($"An authored phrase mapping remains open: {entry.Name}");
        var fixture = entry.Fixture; var trace = fixture.Run(entry.PhaseCount);
        var rules = new FriezeStructuralRules(fixture, S(Enumerable.Range(0, 64).Select(_ => (IElement)new Placeholder()).ToArray()));
        var ruleRead = rules.Evaluate(trace);
        if (!rules.TryReadResult(ruleRead, out _, out var violations, out var pending) || E(violations).Length != 0 || E(pending).Length != 0)
            throw new InvalidOperationException($"The selected local legality rules did not settle: {entry.Name}");
        var calibration = FriezeRealization.CreateCalibration(fixture, FriezeRealization.DefaultLengths(fixture, 4, 4));
        var extent = new Placeholder(); var fieldSupplier = new RibbonPartitionSupplier(fixture, extent, extent);
        var field = fieldSupplier.Interpret(trace, calibration, 2, 2, margin: 4, cellBudget: 32768);
        var geometry = FriezeMeasuredRules.Describe(field.Geometry);
        if (field.Projection.Unresolved != 0 || geometry.UnresolvedCount != 0 || geometry.OverlapCount != 0 || geometry.CrossingCount != 0)
            throw new InvalidOperationException($"The periodic ribbon rules did not settle: {entry.Name}; {JsonSerializer.Serialize(geometry)}; field {field.Projection.Unresolved}");
        var brush = new DiagonalRibbonSupplier(fixture, extent);
        var ribbons = brush.Interpret(trace, calibration, 2);
        if (!brush.TryReadResult(ribbons.Surface, out _, out _, out _, out _, out _, out var brushPending) || E(brushPending).Length != 0)
            throw new InvalidOperationException($"The square-brush read did not settle: {entry.Name}");

        var phraseColors = new Dictionary<IElement, int>(ReferenceEqualityComparer.Instance);
        var selectionColors = new Dictionary<IElement, int>(ReferenceEqualityComparer.Instance);
        var slotViews = new List<Slot>(); var palette = new List<IElement>();
        foreach (var selection in E(entry.Selections).Cast<Relation>())
        {
            if (!entry.TryReadSelection(selection, out var onset, out _, out _, out var phrase, out var strokes, out var unresolved) || E(unresolved).Length != 0)
                throw new InvalidOperationException("A selected phrase lost its source or interpretation read.");
            if (!phraseColors.TryGetValue(phrase, out var color))
            { color = phraseColors.Count; phraseColors.Add(phrase, color); palette.Add(new Relation(phrase, new Placeholder())); }
            selectionColors.Add(selection, color);
            var beat = ReferenceEquals(onset.Right, entry.Roles.Left) ? "●" : ReferenceEquals(onset.Right, entry.Roles.Right) ? "·" : "?";
            slotViews.Add(new(beat, Glyphs(fixture.Frame, phrase), color, E(strokes).Length));
        }
        if (phraseColors.Count > Colors.Length) throw new InvalidOperationException("The selected phrase palette is exhausted.");
        var phaseOwner = new Dictionary<IElement, (Relation Selection, int Color)>(ReferenceEqualityComparer.Instance);
        foreach (var generation in E(entry.Generation).Cast<Relation>())
        {
            if (!entry.TryReadGeneration(generation, out var selection, out _, out var phase, out _) || !selectionColors.TryGetValue(selection, out var color))
                throw new InvalidOperationException("A compiled stroke lost its actual phrase selection.");
            phaseOwner.Add(phase, (selection, color));
        }
        if (!fixture.TryReadTrace(trace, out _, out _, out _, out _, out var samples, out _)) throw new InvalidOperationException("The trace did not read.");
        var sampleColors = new Dictionary<IElement, int>(ReferenceEqualityComparer.Instance); var correspondences = new List<IElement>();
        foreach (var sample in E(samples).Cast<Relation>())
        {
            if (!FriezeFixture.TryReadSample(sample, out _, out _, out _, out _, out var apps, out _) || E(apps).Length == 0)
                throw new InvalidOperationException("Expected a nonempty compiled stroke.");
            Relation? selected = null; int? color = null;
            foreach (var app in E(apps).Cast<Relation>())
            {
                // The actual recurrence receipt supplies the emitted phase;
                // it is not inferred from stroke index or display labels.
                if (app.Left is not Relation { Left: Relation receipt } ||
                    !fixture.Components.Single().TryReadAccepted(receipt, out _, out _, out var phase) || !phaseOwner.TryGetValue(phase, out var owner))
                    throw new InvalidOperationException("An actual application lost its compiled phase correspondence.");
                if (selected is not null && !ReferenceEquals(selected, owner.Selection)) throw new InvalidOperationException("A stroke mixed separate source phrases.");
                selected = owner.Selection; color = owner.Color;
            }
            sampleColors.Add(sample, color!.Value); correspondences.Add(new Relation(sample, selected!));
        }
        var shapes = new List<Shape>();
        foreach (var polygon in E(ribbons.Polygons).Cast<Relation>())
        {
            if (!DiagonalRibbonSupplier.TryReadPolygon(polygon, out var stroke, out _, out var vertices, out _) ||
                !FriezeRealization.TryReadSamplePoints(stroke, out var sample, out _, out _)) throw new InvalidOperationException("The brush lost its source sample.");
            var points = E(vertices).Cast<Relation>().Select(point =>
            {
                if (!DiagonalRibbonSupplier.TryReadPoint(point, out var xn, out var xd, out var yn, out var yd)) throw new InvalidOperationException("The brush vertex did not read.");
                return ((double)xn / xd, (double)yn / yd);
            }).ToArray();
            shapes.Add(new(sample, points, sampleColors[sample]));
        }
        var coloring = new Relation(new Relation(entry.Selections, S(palette.ToArray())), S(correspondences.ToArray()));
        var source = new Relation(entry.Surface, new Relation(trace, new Relation(ruleRead, coloring)));
        var view = new Relation(source, new Relation(field.Surface, ribbons.Surface));
        var fill = field.Projection;
        var summary = new {
            Name = entry.Name, SourceSlots = E(entry.Onsets).Length, PhraseSelections = slotViews.Count,
            StrokePhases = entry.PhaseCount, ReachParticipations = E(entry.Words).Cast<Relation>().Sum(word => E(word).Length),
            PhraseIdentities = phraseColors.Count, Rhythm = string.Join(" ", slotViews.Select(slot => slot.Beat)),
            CompiledStrokesPerSlot = slotViews.Select(slot => slot.Strokes), Geometry = geometry, Field = fill,
            Source = Counts(source), Observation = Counts(view)
        };
        return new(entry.Name, geometry.PeriodX, fill.MinimumY / 2.0,
            fill.MaximumY / 2.0, shapes, slotViews, source, view, summary,
            $"{entry.PhaseCount} stroke phases / {phraseColors.Count} phrase identities; dark {fill.FirstFraction:P0} in its {fill.Columns} × {fill.Rows} cell strip; periodic retraces {geometry.OverlapCount}, crossings {geometry.CrossingCount}");
    }

    private static string Glyphs(QualitativeMotionFrame frame, Relation phrase) => string.Join(" ", E(phrase).Cast<Relation>().Select(word =>
        string.Concat(E(word).Cast<Relation>().Select(move => frame.Direction(move, out var axis, out var forward, out _) ?
            ReferenceEquals(axis, frame.Horizontal) ? forward ? "→" : "←" : forward ? "↑" : "↓" : "?"))));

    private static string Draw(IReadOnlyList<Picture> pictures)
    {
        const int width = 1460, header = 96, row = 300; var height = header + pictures.Count * row + 34;
        var svg = new StringBuilder($"<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"{width}\" height=\"{height}\" viewBox=\"0 0 {width} {height}\"><style>:root{{--ink:#242a29;--paper:#f4efdf}}.phrase-color{{display:none}}.show-phrases .phrase-color{{display:inline}}.show-phrases .plain{{display:none}}</style><rect width=\"100%\" height=\"100%\" fill=\"var(--paper)\"/><g font-family=\"system-ui,sans-serif\" fill=\"var(--ink)\">");
        Text(24, 34, "Rhythm as a grammar for frets and meanders", 26, "650");
        Text(24, 62, "Beat/rest order selects whole turn phrases. Phrase-state and reusable reach identities shape the band; equal musical durations are not implied.", 14);
        for (var i = 0; i < pictures.Count; i++)
        {
            var picture = pictures[i]; var top = header + i * row;
            svg.Append($"<rect x=\"14\" y=\"{top}\" width=\"1432\" height=\"288\" rx=\"10\" fill=\"none\" stroke=\"#bfbbae\"/>");
            Text(30, top + 26, picture.Name, 18, "600"); Text(30, top + 48, picture.Caption, 12);
            const double left = 34, available = 1392, plotHeight = 126;
            var plotTop = top + 63; var scale = Math.Min(available / (picture.Period * 2), (plotHeight - 16) / (picture.High - picture.Low));
            var repeats = Math.Clamp((int)Math.Floor(available / (picture.Period * scale)), 1, 6);
            var full = repeats * picture.Period * scale; var ox = left + (available - full) / 2;
            var oy = plotTop + (plotHeight - (picture.High - picture.Low) * scale) / 2;
            svg.Append($"<defs><clipPath id=\"rf-{i}\"><rect x=\"{F(ox)}\" y=\"{plotTop}\" width=\"{F(full)}\" height=\"{plotHeight}\"/></clipPath></defs><g clip-path=\"url(#rf-{i})\">");
            // Render copies are an observation of supplied polygons; exact geometry
            // and the two-role fill witness are retained separately in View.
            foreach (var shape in picture.Shapes)
            {
                var first = (int)Math.Ceiling(-shape.Vertices.Max(point => point.X) / picture.Period);
                var last = (int)Math.Floor((repeats * picture.Period - shape.Vertices.Min(point => point.X)) / picture.Period);
                for (var copy = first; copy <= last; copy++)
                {
                    var points = string.Join(' ', shape.Vertices.Select(point => F(ox + (point.X + copy * picture.Period) * scale) + ',' + F(oy + (picture.High - point.Y) * scale)));
                    svg.Append($"<polygon class=\"plain\" points=\"{points}\" fill=\"var(--ink)\"/><polygon class=\"phrase-color\" points=\"{points}\" fill=\"{Colors[shape.Color]}\"/>");
                }
            }
            // Rails frame the display and are excluded from fill/overlap reads.
            foreach (var y in new[] { plotTop + 2, plotTop + plotHeight - 2 })
                svg.Append($"<path d=\"M{F(ox)},{F(y)}h{F(full)}\" stroke=\"var(--ink)\" stroke-width=\"3\"/>");
            svg.Append("</g>");
            var boxWidth = Math.Min(170, 1388.0 / picture.Slots.Count);
            for (var slot = 0; slot < picture.Slots.Count; slot++)
            {
                var item = picture.Slots[slot]; var x = 32 + slot * boxWidth;
                svg.Append($"<rect x=\"{F(x)}\" y=\"{top + 207}\" width=\"{F(boxWidth - 5)}\" height=\"55\" rx=\"4\" fill=\"{Colors[item.Color]}\" opacity=\".10\"/>");
                Text(x + 7, top + 224, $"{item.Beat}  → phrase {((char)('A' + item.Color))}", 12, "600", Colors[item.Color]);
                var glyphLimit = Math.Max(6, (int)((boxWidth - 16) / 5.5));
                Text(x + 7, top + 243, item.Word.Length > glyphLimit ? item.Word[..(glyphLimit - 1)] + "…" : item.Word, 10, fill: Colors[item.Color]);
                Text(x + 7, top + 256, $"{item.Strokes} strokes", 9, fill: Colors[item.Color]);
            }
            Text(32, top + 279, "Source-slot order → selected phrase → ordered strokes → actual output phases → sampled path. Each stroke retains its source selection.", 11);
        }
        Text(24, height - 12, "Colours are local phrase identities. Black/white inversion is a view; neither equal fill nor a role-exchanging symmetry is assumed.", 12);
        return svg.Append("</g></svg>").ToString();
        void Text(double x, double y, string value, int size, string weight = "400", string? fill = null) =>
            svg.Append($"<text x=\"{F(x)}\" y=\"{F(y)}\" font-size=\"{size}\" font-weight=\"{weight}\"{(fill is null ? "" : $" fill=\"{fill}\"")}>{WebUtility.HtmlEncode(value)}</text>");
    }

    private static string Html(string svg, IReadOnlyList<Picture> pictures) =>
        "<!doctype html><html lang=\"en\"><meta charset=\"utf-8\"><meta name=\"viewport\" content=\"width=device-width,initial-scale=1\"><title>Rhythm-derived frieze phrases</title><style>body{margin:0;background:#f4efdf;color:#242a29;font:16px system-ui}main{max-width:1460px;margin:auto;padding:24px}p{max-width:1140px;line-height:1.6}svg{width:100%;height:auto;display:block}.controls{position:sticky;top:0;display:flex;gap:28px;padding:15px;background:#f4efdf;z-index:1}.invert svg{--ink:#f4efdf;--paper:#242a29}.show-phrases .phrase-color{display:inline}.show-phrases .plain{display:none}a{color:#176d79}</style><main><h1>From rhythmic events to repeating turn structures</h1><p>Here a beat can unfold into a backward reach, corners and an outward return. A rest can be a connecting stroke or a smaller motif. The same onset source is interpreted through several authored phrase laws; a separate explicit phase can alternate neighboring keys. No random search chooses the examples.</p><div class=\"controls\"><label><input type=\"checkbox\" onchange=\"document.body.classList.toggle('show-phrases',this.checked)\"> Colour by selected phrase</label><label><input type=\"checkbox\" onchange=\"document.body.classList.toggle('invert',this.checked)\"> Invert black / white</label></div>" + svg +
        "<p>Each small box is a source slot, in order. The dot marks an authored rest role, not absence of an output instruction. Arrows show the selected phrase's individual strokes; repeated arrows in a stroke retain the same unmeasured reach more than once. Long recipes are abbreviated in the boxes and shown in full below. Phrase letters and colours are local display labels. Each selected phrase retains its finite-law evidence and correspondence to actual emitted phases. Flattening a phrase means sequencing its strokes, preserving the corners.</p>" +
        "<details><summary>Full turn recipes for every local phrase</summary>" + string.Concat(pictures.Select(picture => "<h3>" + WebUtility.HtmlEncode(picture.Name) + "</h3><ul>" + string.Concat(picture.Slots.GroupBy(slot => slot.Color).Select(group => "<li>" + (char)('A' + group.Key) + ": <code>" + WebUtility.HtmlEncode(group.First().Word) + "</code> — " + group.First().Strokes + " strokes</li>")) + "</ul>")) + "</details>" +
        "<p>The grammar supplies positive horizontal progress and neutral vertical displacement over each phrase. Local retracing and full periodic centerline overlap/crossing checks run separately. The rendered square brush uses H4/V4 with width2. Dark fraction is measured exactly in each advertised cell strip with four-cell padding from the centerline extrema, leaving two white cells beyond the brush. Strip heights differ, so these figures are descriptive rather than a common fitness ranking. Framing rails are outside that read.</p><p>Expanding one source slot into several strokes preserves event order, not source durations, acoustic meter, or spatial 3–3–2 spacing. This is an explicit structural interpretation for variety. The motifs and preferences are authored; no audio analysis, learned aesthetics or newly discovered primitive is claimed.</p><p><a href=\"gallery.svg\">Standalone SVG</a> · <a href=\"diagnostics.json\">Diagnostics</a> · <a href=\"../../../../Gojo/docs/GojoRhythmFriezeExperiment.md\">Experiment report</a></p></main></html>";
    private static object Counts(IElement source) { var values = Traverse(source).ToArray(); return new { Elements = values.Length, Carriers = values.Count(value => value is Carrier), Locations = values.Count(value => value is CarrierLocation) }; }
    private static IEnumerable<IElement> Traverse(IElement source)
    {
        var seen = new HashSet<IElement>(ReferenceEqualityComparer.Instance); var stack = new Stack<IElement>(); stack.Push(source);
        while (stack.TryPop(out var value)) { if (!seen.Add(value)) continue; yield return value; if (value is Relation pair) { stack.Push(pair.Left); stack.Push(pair.Right); } }
    }
    private static string F(double value) => value.ToString("0.###", CultureInfo.InvariantCulture);
    private static Relation S(params IElement[] values) => StructuralConstraint.Closed(values);
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
}
