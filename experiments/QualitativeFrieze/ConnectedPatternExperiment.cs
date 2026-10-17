using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;
using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;

namespace Gojo.Experiments.QualitativeFrieze;

/// <summary>Port-connected qualitative generators with separate topology,
/// measured ink/probe observations and declared preference interpretations.</summary>
public static class ConnectedPatternExperiment
{
    private sealed record Line(Relation Source, long X0, long Y0, long X1, long Y1, int Owner);
    private sealed record Node(IElement Site, long X, long Y, string Kind);
    private sealed record View(ConnectedPatternEntry Entry, long Pitch, Relation Source, Relation Observation,
        QualitativeStrokeGraph.Summary Junctions, FriezeMeasuredRules.Summary Geometry,
        FriezeCoverage.Evaluation Coverage, ConnectedPatternScoring.Evaluation Native,
        ConnectedPatternScoring.Evaluation Refined, IReadOnlyList<Line> Lines, IReadOnlyList<Node> Nodes,
        int Accepted, int Blocked, int Pending, int? Score, bool Eligible, object Diagnostic);
    private static readonly string[] Colors = ["#176e80", "#b55075", "#b87921"];

    public static int Run(string[] arguments)
    {
        var output = Path.Combine("experiments", "QualitativeFrieze", "results", "connected");
        for (var i = 0; i < arguments.Length; i++)
            if (arguments[i] == "--output" && i + 1 < arguments.Length) output = arguments[++i];
            else throw new ArgumentException("Usage: --connected [--output DIRECTORY]");
        Directory.CreateDirectory(output);
        var library = new ConnectedPatternLibrary(); var views = new List<View>();
        foreach (var entry in library.Candidates)
        {
            Console.WriteLine($"Observing {entry.Name}.");
            views.Add(Observe(library, entry));
        }
        var authored = views.Take(library.Entries.Count).ToArray();
        var refinements = new[] { Observe(library, library.Entries[3], 8), Observe(library, library.Entries[3], 12), Observe(library, library.Entries[5], 8) };
        var ranked = views.Where(view => view.Eligible).OrderByDescending(view => view.Score)
            .ThenBy(view => view.Entry.Name, StringComparer.Ordinal).Take(6).ToArray();
        var unknown = ConnectedPatternScoring.Native(library.UnknownExample);
        if (unknown.Score is not null || unknown.Summary.Complete)
            throw new InvalidOperationException("An unknown source publication received a definite preference rank.");
        var native = new Relation(library.Surface, S(views.Select(view => (IElement)view.Source).Append(unknown.Surface).ToArray()));
        if (Traverse(native).Any(node => node is Carrier or CarrierLocation))
            throw new InvalidOperationException("A measured participant entered the qualitative generation/analysis layer.");
        var main = Draw(authored, "Connected generators: diagonals, branches and repeated contours", "main");
        var best = Draw(ranked, "A bounded comparison under one declared aesthetic profile", "ranked");
        var finer = Draw(refinements, "The same sources with alternative pen-to-pitch calibrations", "refined");
        File.WriteAllText(Path.Combine(output, "gallery.svg"), main);
        File.WriteAllText(Path.Combine(output, "ranked.svg"), best);
        File.WriteAllText(Path.Combine(output, "refined.svg"), finer);
        File.WriteAllText(Path.Combine(output, "layers.svg"), Diagram());
        File.WriteAllText(Path.Combine(output, "gallery.html"), Html(main, best, finer, views, library, unknown));
        File.WriteAllText(Path.Combine(output, "diagnostics.json"), JsonSerializer.Serialize(new
        {
            Schema = "qualitative-connected-patterns-v1", Native = Counts(native),
            Candidates = views.Select(view => view.Diagnostic),
            Refinements = refinements.Select(view => view.Diagnostic),
            Ranked = ranked.Select(view => view.Entry.Name),
            Unknown = new { library.UnknownExample.Name, Native = unknown.Summary, unknown.Score,
                SourceObligations = E(library.UnknownExample.Unresolved).Length },
            Preferences = new { NativeMaximum = 8, RefinedMaximum = 8, Aggregate = "2 per preferred; 1 per acceptable; 0 per known violation; unavailable for unknowns",
                Eligibility = "Complete reads, zero measured retracing, both profiles acceptable; crossings allowed" },
            Notes = new[] {
                "Generation and native decisions contain no numeric carriers or locations; calibration and probe fitness are optional observations.",
                "Coarse consumers inspect advertised publication contracts, not supplier recurrence or finer state.",
                "Native graph correspondence is separate from measured isometry and from causal phase-law symmetry.",
                "Probe occupancy is not exact area. Multiple support near an ordinary join is not itself retracing.",
                "O counts zero-return graph-cycle witnesses, not bounded white regions or geometric circles.",
                "Candidate scores are authored preferences, not empirically validated human-beauty predictions." }
        }, new JsonSerializerOptions { WriteIndented = true }));
        foreach (var view in views) Console.WriteLine($"{view.Entry.Name}: native {view.Native.Score}, measured {view.Refined.Score}, total {view.Score}; overlap {view.Geometry.OverlapCount}, crossing {view.Geometry.CrossingCount}; eligible {view.Eligible}.");
        Console.WriteLine($"Wrote connected pattern study to {Path.GetFullPath(output)}.");
        return 0;
    }

    private static View Observe(ConnectedPatternLibrary library, ConnectedPatternEntry entry, long pitch = 4)
    {
        var graph = entry.Graph; var graphRead = graph.Evaluate(); var junctions = graph.Describe(graphRead);
        var native = ConnectedPatternScoring.Native(entry);
        var columns = E(entry.Columns); var levels = E(entry.Levels); var period = checked(columns.Length * pitch);
        if (levels.Length != 4) throw new InvalidOperationException("This selected view requires four exposed height landmarks.");
        var xBasis = new Carrier(); var yBasis = new Carrier();
        Relation Point(long x, long y) => new(new Relation(graph.Frame.Horizontal, new CarrierLocation(xBasis, x)),
            new Relation(graph.Frame.Vertical, new CarrierLocation(yBasis, y)));
        var positions = new Dictionary<IElement, (int X, int Y, Relation Binding)>(ReferenceEqualityComparer.Instance);
        foreach (var row in E(entry.Coordinates).Cast<Relation>())
        {
            if (row.Right is not Relation pair) throw new InvalidOperationException("Expected exposed site/landmark coordinates.");
            var x = Array.FindIndex(columns, column => ReferenceEquals(column, pair.Left));
            var y = Array.FindIndex(levels, level => ReferenceEquals(level, pair.Right));
            if (x < 0 || y < 0 || !E(graph.Sites).Any(site => ReferenceEquals(site, row.Left)))
                throw new InvalidOperationException("A site binding refers to a foreign landmark.");
            positions.Add(row.Left, (x, y, row));
        }
        var owner = new Dictionary<IElement, (int Index, Relation Receipt, Relation Connection)>(ReferenceEqualityComparer.Instance);
        var accepted = 0; var blocked = 0; var pending = 0;
        foreach (var receipt in E(entry.ConditionalReceipts).Cast<Relation>())
        {
            var connection = receipt.Left is Relation { Right: Relation context } && ReferenceEquals(context.Left, entry.MainConnection.Surface)
                ? entry.MainConnection : entry.DecorationConnection;
            if (connection is null || !connection.TryReadResult(receipt, out var options, out var no, out var unknown))
                throw new InvalidOperationException("A receiver receipt lost its advertised connection contract.");
            blocked += E(no).Length; pending += E(unknown).Length;
            if (Closed(options) && E(options).Length != 0) accepted++;
            foreach (var strokes in E(options).OfType<Relation>())
                foreach (var stroke in E(strokes)) owner.Add(stroke, (ReferenceEquals(connection, entry.MainConnection) ? 0 : 1, receipt, connection.Surface));
        }
        var clauses = new List<IElement>(); var lifts = new List<IElement>(); var lines = new List<Line>(); var attribution = new List<IElement>();
        foreach (var stroke in E(graph.Strokes).Cast<Relation>())
        {
            if (!QualitativeStrokeGraph.TryReadStroke(stroke, out _, out var from, out var to, out var word) ||
                !positions.TryGetValue(from, out var a) || !positions.TryGetValue(to, out var b))
                throw new InvalidOperationException("A stroke lost its actual source sites.");
            var dx = 0; var dy = 0;
            foreach (var term in E(word).Cast<Relation>())
            {
                if (!graph.Frame.Direction(term, out var axis, out var forward, out var reach) ||
                    !ReferenceEquals(reach, ReferenceEquals(axis, graph.Frame.Horizontal) ? library.HorizontalReach : library.VerticalReach))
                    throw new InvalidOperationException("This calibration cannot interpret a foreign reach.");
                if (ReferenceEquals(axis, graph.Frame.Horizontal)) dx += forward ? 1 : -1; else dy += forward ? 1 : -1;
            }
            if (((a.X + dx) % columns.Length + columns.Length) % columns.Length != b.X || a.Y + dy != b.Y)
                throw new InvalidOperationException("Chord participation contradicts the selected equal-pitch landmark calibration.");
            var start = Point(a.X * pitch, a.Y * pitch); var end = Point((a.X + dx) * pitch, b.Y * pitch);
            clauses.Add(new Relation(stroke, new Relation(start, end)));
            lifts.Add(new Relation(stroke, new Relation(new Relation(a.Binding, b.Binding),
                new Relation(entry.Period, Relation.Segment(xBasis, 0, checked((a.X + dx - b.X) * pitch))))));
            if (!owner.TryGetValue(stroke, out var produced)) throw new InvalidOperationException("A rendered stroke has no actual conditional selection.");
            attribution.Add(new Relation(stroke, new Relation(produced.Receipt, produced.Connection)));
            lines.Add(new(stroke, a.X * pitch, a.Y * pitch, (a.X + dx) * pitch, b.Y * pitch, produced.Index));
        }
        var calibration = new Relation(new Relation(graph.Frame.Surface, new Relation(
                new Relation(library.HorizontalReach, Relation.Segment(xBasis, 0, pitch)),
                new Relation(library.VerticalReach, Relation.Segment(yBasis, 0, pitch)))),
            new Relation(S(columns.Select((column, index) => (IElement)new Relation(column, new CarrierLocation(xBasis, index * pitch))).ToArray()),
                new Relation(S(levels.Select((level, index) => (IElement)new Relation(level, new CarrierLocation(yBasis, index * pitch))).ToArray()), S(lifts.ToArray()))));
        var measuredSource = new Relation(graph.Surface, new Relation(entry.Surface, calibration));
        var geometryRead = FriezeMeasuredRules.EvaluateStrokes(S(clauses.ToArray()), new Relation(Point(0, 0), Point(period, 0)), measuredSource, 1_000_000);
        var geometry = FriezeMeasuredRules.Describe(geometryRead);
        if (!FriezeMeasuredRules.TryReadResult(geometryRead, out var admitted, out _, out _, out _, out _, out _))
            throw new InvalidOperationException("The exposed measured stroke read did not parse.");
        var coverage = FriezeCoverage.Evaluate(graph.Frame, geometryRead, admitted, period, minimumY: -2, maximumY: checked(3 * pitch + 2));
        var refined = ConnectedPatternScoring.Measured(entry, geometryRead, coverage);
        var nodes = new List<Node>();
        graph.TryReadResult(graphRead, out var nodeReads, out _, out _, out _);
        foreach (var node in E(nodeReads).Cast<Relation>())
        {
            graph.TryReadJunction(node, out var site, out var rays, out var opposite, out var same, out var unknown);
            if (!positions.TryGetValue(site, out var location) || E(rays).Length == 0) continue;
            var degree = E(rays).Length; var pairs = E(opposite).Length;
            var label = E(unknown).Length != 0 || E(same).Length != 0 ? "?" : degree == 1 ? "E" :
                degree == 2 ? pairs == 1 ? "I" : "L" : degree == 3 ? pairs == 1 ? "T" : pairs == 0 ? "Y" : "B" :
                degree == 4 && pairs == 2 ? "X" : "B";
            nodes.Add(new(site, location.X * pitch, location.Y * pitch, label));
        }
        int? score = native.Score is int first && refined.Score is int second ? first + second : null;
        var eligible = native.Summary.Acceptable && refined.Summary.Acceptable && geometry.UnresolvedCount == 0 &&
            geometry.OverlapCount == 0 && coverage.Projection.Unresolved == 0 && E(entry.Unresolved).Length == 0;
        var source = new Relation(entry.Surface, new Relation(native.Surface, S(attribution.ToArray())));
        var observation = new Relation(source, new Relation(new Relation(geometryRead, coverage.Surface), refined.Surface));
        var diagnostic = new {
            entry.Name, Pitch = pitch, Columns = columns.Length, Levels = levels.Length, Junctions = junctions, Geometry = geometry,
            Native = native.Summary, NativeScore = native.Score, Refined = refined.Summary, RefinedScore = refined.Score, Score = score, Eligible = eligible,
            NativeCriteria = native.Features.Select(feature => new { feature.Name, feature.Rank }),
            RefinedCriteria = refined.Features.Select(feature => new { feature.Name, feature.Rank }),
            ProbeField = coverage.Projection, Receipts = new { Accepted = accepted, Blocked = blocked, Pending = pending },
            StructuralMaps = entry.Correspondences.Select(map => new { map.Name, Passed = graph.TryReadMapPassed(graph, map.Read) }),
            Source = Counts(source), Observation = Counts(observation)
        };
        return new(entry, pitch, source, observation, junctions, geometry, coverage, native, refined, lines, nodes, accepted, blocked, pending, score, eligible, diagnostic);
    }

    private static string Draw(IReadOnlyList<View> views, string title, string prefix)
    {
        const int width = 1460, header = 94, row = 286; var height = header + row * views.Count + 38;
        var svg = new StringBuilder($"<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"{width}\" height=\"{height}\" viewBox=\"0 0 {width} {height}\"><style>:root{{--paper:#f3efe2;--ink:#252d31}}.module{{display:none}}.show-modules .module{{display:inline}}.show-modules .mono{{display:none}}.junctions{{display:none}}.show-junctions .junctions{{display:inline}}</style><rect width=\"100%\" height=\"100%\" fill=\"var(--paper)\"/><g font-family=\"system-ui,sans-serif\" fill=\"var(--ink)\">");
        Text(24, 34, title, 25, "650"); Text(24, 62, "Four ordered levels; exposed coarse decisions; shared-site topology; declared H/V calibration and radius-one probe field.", 14);
        for (var i = 0; i < views.Count; i++)
        {
            var view = views[i]; var top = header + i * row; var g = view.Junctions;
            svg.Append($"<rect x=\"14\" y=\"{top}\" width=\"1432\" height=\"274\" rx=\"10\" fill=\"none\" stroke=\"#c4bfb2\"/>");
            Text(30, top + 26, view.Entry.Name + (view.Pitch == 4 ? "" : $" / pitch {view.Pitch}, pen 2"), 18, "600");
            Text(30, top + 48, $"Declared score {Rank(view.Score)}/16  = native {Rank(view.Native.Score)}/8 + measured {Rank(view.Refined.Score)}/8;  " +
                $"{(view.Eligible ? "eligible" : "outside selection profile")} · retraces {view.Geometry.OverlapCount}, geometric crossings {view.Geometry.CrossingCount}", 12);
            var scale = 100.0 / (3 * view.Pitch + 4); var period = view.Geometry.PeriodX; var repeats = Math.Max(2, (int)Math.Floor(1380 / (period * scale)));
            if (repeats * period * scale > 1380) scale = 1380 / (repeats * period);
            var shown = repeats * period * scale; var ox = 40 + (1380 - shown) / 2; var oy = top + 65;
            svg.Append($"<defs><clipPath id=\"{prefix}-{i}\"><rect x=\"{F(ox)}\" y=\"{F(oy)}\" width=\"{F(shown)}\" height=\"110\"/></clipPath></defs><g clip-path=\"url(#{prefix}-{i})\">");
            foreach (var y in new[] { oy + 3, oy + 107 }) svg.Append($"<path d=\"M{F(ox)},{F(y)}h{F(shown)}\" stroke=\"var(--ink)\" stroke-width=\"2\"/>");
            foreach (var line in view.Lines)
            {
                var first = (long)Math.Ceiling((-Math.Max(line.X0, line.X1) - 1.0) / period);
                var last = (long)Math.Floor((repeats * period - Math.Min(line.X0, line.X1) + 1.0) / period);
                for (var copy = first; copy <= last; copy++)
                {
                    var path = $"M{F(ox + (line.X0 + copy * period) * scale)},{F(oy + (3 * view.Pitch + 2 - line.Y0) * scale)}L{F(ox + (line.X1 + copy * period) * scale)},{F(oy + (3 * view.Pitch + 2 - line.Y1) * scale)}";
                    svg.Append($"<path class=\"mono\" d=\"{path}\" fill=\"none\" stroke=\"var(--ink)\" stroke-width=\"{F(2 * scale)}\" stroke-linecap=\"round\"/><path class=\"module\" d=\"{path}\" fill=\"none\" stroke=\"{Colors[line.Owner]}\" stroke-width=\"{F(2 * scale)}\" stroke-linecap=\"round\"/>");
                }
            }
            svg.Append("<g class=\"junctions\">");
            foreach (var node in view.Nodes.Where(node => node.Kind != "I"))
                for (var copy = 0; copy < repeats; copy++)
                {
                    var x = ox + (node.X + copy * period) * scale; var y = oy + (3 * view.Pitch + 2 - node.Y) * scale;
                    svg.Append($"<circle cx=\"{F(x)}\" cy=\"{F(y)}\" r=\"8\" fill=\"var(--paper)\" stroke=\"#ba4b60\"/>");
                    Text(x - 3.6, y + 3.5, node.Kind, 10, "650", "#a13952");
                }
            svg.Append("</g></g>");
            Text(30, top + 193, $"Native joins: E {g.Endpoints} · L {g.L} · T {g.T} · Y {g.Y} · X {g.X} · branch sites {g.Branches} · zero-return cycles O {g.O}.  Accepted decisions {view.Accepted}; blocked {view.Blocked}; pending {view.Pending}.", 12);
            Text(30, top + 215, "Measured ink symmetries: " + string.Join(", ", view.Geometry.VerifiedSymmetryNames) +
                $". Empty probes {view.Coverage.Projection.EmptyFraction:P0}; multiple support {view.Coverage.Projection.MultipleFraction:P0}; clearance spread {view.Coverage.Projection.ClearanceSpread}.", 12);
            Text(30, top + 237, "Native: " + string.Join(" · ", view.Native.Features.Select(feature => feature.Name + " " + Rank(feature.Rank))), 11);
            Text(30, top + 256, "Measured: " + string.Join(" · ", view.Refined.Features.Select(feature => feature.Name + " " + Rank(feature.Rank))), 11);
        }
        Text(24, height - 14, "Points are an authored ordinal preference, not human-beauty predictions. Unknowns remain unranked; crossings are allowed in this profile.", 12);
        return svg.Append("</g></svg>").ToString();
        void Text(double x, double y, string value, int size, string weight = "400", string? fill = null) =>
            svg.Append($"<text x=\"{F(x)}\" y=\"{F(y)}\" font-size=\"{size}\" font-weight=\"{weight}\"{(fill is null ? "" : $" fill=\"{fill}\"")}>{H(value)}</text>");
    }

    private static string Diagram() =>
        "<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"1460\" height=\"270\" viewBox=\"0 0 1460 270\"><rect width=\"1460\" height=\"270\" fill=\"#f3efe2\"/><g font-family=\"system-ui,sans-serif\" fill=\"#252d31\"><text x=\"26\" y=\"34\" font-size=\"24\">One process, several exposed reads</text><g fill=\"none\" stroke=\"#507c86\" stroke-width=\"2\"><rect x=\"25\" y=\"64\" width=\"245\" height=\"112\" rx=\"8\"/><rect x=\"315\" y=\"64\" width=\"245\" height=\"112\" rx=\"8\"/><rect x=\"605\" y=\"64\" width=\"245\" height=\"112\" rx=\"8\"/><rect x=\"895\" y=\"64\" width=\"245\" height=\"112\" rx=\"8\"/><rect x=\"1185\" y=\"64\" width=\"245\" height=\"112\" rx=\"8\"/><path d=\"M270,119h45m245,0h45m245,0h45m245,0h45\"/></g><g font-size=\"15\"><text x=\"40\" y=\"94\">Private recurring supplier</text><text x=\"40\" y=\"122\">coarse publication only</text><text x=\"40\" y=\"151\">independent event schedule</text><text x=\"330\" y=\"94\">Receiver / conditional law</text><text x=\"330\" y=\"122\">accepted / denied / unknown</text><text x=\"330\" y=\"151\">actual stroke selections</text><text x=\"620\" y=\"94\">Shared-site stroke graph</text><text x=\"620\" y=\"122\">chords, joins, cycles, maps</text><text x=\"620\" y=\"151\">native preference read</text><text x=\"910\" y=\"94\">Optional measured supplier</text><text x=\"910\" y=\"122\">ink / probes / clearance</text><text x=\"910\" y=\"151\">exact selected isometries</text><text x=\"1200\" y=\"94\">Declared preference profile</text><text x=\"1200\" y=\"122\">retain criterion + witness</text><text x=\"1200\" y=\"151\">optional ordinal rank</text></g><text x=\"26\" y=\"221\" font-size=\"16\">Generation and local decisions use no measured values. A finer observer adds information without rewriting the coarse source.</text><text x=\"26\" y=\"249\" font-size=\"14\">Consumers inspect advertised contracts. A geometric crossing does not create a shared site; a graph map does not by itself establish an isometry.</text></g></svg>";

    private static string Html(string main, string ranked, string finer, IReadOnlyList<View> all, ConnectedPatternLibrary library, ConnectedPatternScoring.Evaluation unknown)
    {
        var table = "<table><thead><tr><th>Candidate</th><th>Native /8</th><th>Measured /8</th><th>Total /16</th><th>Eligible</th><th>Retraces</th><th>Crossings</th></tr></thead><tbody>" +
            string.Concat(all.OrderByDescending(view => view.Score).Select(view => $"<tr><td>{H(view.Entry.Name)}</td><td>{Rank(view.Native.Score)}</td><td>{Rank(view.Refined.Score)}</td><td>{Rank(view.Score)}</td><td>{view.Eligible}</td><td>{view.Geometry.OverlapCount}</td><td>{view.Geometry.CrossingCount}</td></tr>")) + "</tbody></table>";
        return "<!doctype html><html lang=\"en\"><meta charset=\"utf-8\"><meta name=\"viewport\" content=\"width=device-width,initial-scale=1\"><title>Connected qualitative pattern study</title><style>body{margin:0;background:#f3efe2;color:#252d31;font:16px system-ui}main{max-width:1460px;margin:auto;padding:24px}p{max-width:1150px;line-height:1.6}svg{display:block;width:100%;height:auto;margin:20px 0}.controls{position:sticky;top:0;display:flex;gap:24px;padding:15px;background:#f3efe2;z-index:1}.show-modules .module{display:inline}.show-modules .mono{display:none}.show-junctions .junctions{display:inline}.invert svg{--paper:#252d31;--ink:#f3efe2}table{border-collapse:collapse;width:100%;font-size:13px}td,th{padding:8px;border-bottom:1px solid #d1cabb;text-align:left}a{color:#176e80}details{margin:24px 0}</style><main><h1>Beyond keys: connected qualitative generators</h1><p>The drawing modules consume coarse event publications through declared ports. Their local finite laws choose strokes, diagonal chords and optional branches without reading the supplier's private recurrence. Shared sites, four ordered height landmarks and an explicit repeating column seam retain native topology. The generator has no numeric measurements; the view below supplies H4/V4 and a round pen of width2.</p>" + Diagram() +
            "<div class=\"controls\"><label><input type=\"checkbox\" onchange=\"document.body.classList.toggle('show-modules',this.checked)\"> Colour receiver contributions</label><label><input type=\"checkbox\" onchange=\"document.body.classList.toggle('show-junctions',this.checked)\"> Show shared-site joins</label><label><input type=\"checkbox\" onchange=\"document.body.classList.toggle('invert',this.checked)\"> Invert ink / field</label></div>" + main +
            "<details open><summary>Two private suppliers, two coarse ports, local conditional actions</summary>" + DecisionTable(library.BlockedExample) + "<p>Each cell reads an actual advertised publication and receiver receipt. A/B are local names for the two shared coarse participants. The main and branch suppliers keep independent histories and 6/3 phase cycles; their consumers use only the matching coarse port. A known denied branch is different from an unknown action.</p></details>" +
            "<p>E marks an endpoint, I a straight continuation, L a turn, T an opposed pair with a third ray, Y three rays without an opposed pair, and X two opposed pairs. These are coarse structural reads, not an assertion of typographic angle or a three-dimensional occlusion label. An O is a zero-return graph-cycle witness; a winding return through the band seam is separate. A cycle does not establish enclosed area or a circle. Crossing ink only becomes a junction when its actual source sites are connected.</p><p>Our preference profile keeps four native observations separate from four measured ones. It prefers a reusable but varied emission vocabulary, limited endpoints, a range of joins and actual supplied graph correspondences. The measured profile examines intermediate probe occupancy, ordinal clearance spread, nontranslation isometry and crowding. Each preferred observation contributes 2, an acceptable one 1, a known violation 0. Missing observations have no score. These thresholds are experimental choices, not fitted human taste.</p>" + ranked +
            "<details open><summary>All sixteen authored candidates and their component scores</summary>" + table + "<p>The second eight constructions change one exposed coarse phase. They retain their own source/event histories. These are small declared candidate constructions, not learned laws or exact parent-child edit receipts. Selection requires both declared profiles to be complete and acceptable, with no measured retracing; crossing is allowed.</p></details>" +
            "<details open><summary>Interpret the same native sources: pitches 8 and 12, pen 2</summary>" + finer + "<p>The initial profile gave the nearly solid crossed braces 13/14. Adding a crowding criterion exposes the lost gaps. These further interpretations keep the actual source graphs, ports and conditional laws while changing the pen-to-pitch ratio. The native scores stay the same. Each measured view supplies proportions permitted by that source; the experiment selects these calibrations explicitly.</p></details>" +
            $"<details open><summary>Unknown input counterexample: deliberately unranked</summary><p>{H(library.UnknownExample.Name)} has {E(library.UnknownExample.Unresolved).Length} source obligations and {unknown.Summary.Unknown} unresolved native preference criteria. Its native score is {Rank(unknown.Score)}. The known stroke prefix is retained alongside an open remainder; the experiment does not fill in a rest or query the supplier's private phase to repair it.</p></details>" +
            "<p>Ink and gap use a common finite probe band y −2 through (3 × pitch + 2), with Euclidean radius 1 and unit sampling. Empty and multiple-support fractions are probe observations, not exact area or a physical conservation claim. Normal joined pen strokes can overlap in their footprints without retracing centerlines. Borders are decorative and excluded from analysis. Native graph maps, measured ink symmetries and retained phase maps answer separate questions; the phase maps here are not causal-law symmetry certificates.</p><p>Read the <a href=\"../../../../Gojo/docs/GojoPatternAestheticRules.md\">research-backed rule vocabulary</a> and <a href=\"../../../../Gojo/docs/GojoConnectedPatternExperiment.md\">experiment report</a>. <a href=\"gallery.svg\">Main SVG</a> · <a href=\"ranked.svg\">Ranked comparison</a> · <a href=\"refined.svg\">Calibration alternatives</a> · <a href=\"layers.svg\">Layer diagram</a> · <a href=\"diagnostics.json\">Diagnostics</a></p></main></html>";
    }
    private static string DecisionTable(ConnectedPatternEntry entry)
    {
        var columns = E(entry.Columns); var rows = new string[4, columns.Length];
        foreach (var receipt in E(entry.ConditionalReceipts).Cast<Relation>())
        {
            if (receipt.Left is not Relation { Left: Relation publication, Right: Relation context } || context.Right is not Relation columnSupport ||
                E(columnSupport) is not [var column] || !QualitativePublication.TryRead(publication, out _, out _, out _, out _, out var domain, out var signals, out _))
                throw new InvalidOperationException("A decision lane lost its exposed source contract.");
            var index = Array.FindIndex(columns, actual => ReferenceEquals(actual, column));
            var main = ReferenceEquals(context.Left, entry.MainConnection.Surface); var connection = main ? entry.MainConnection : entry.DecorationConnection;
            if (index < 0 || connection is null || !connection.TryReadResult(receipt, out var selected, out var blocked, out var pending))
                throw new InvalidOperationException("A decision lane lost its actual receiver receipt.");
            var support = E(signals); var roles = E(domain);
            rows[main ? 0 : 2, index] = Closed(signals) && support is [var signal]
                ? ReferenceEquals(signal, roles[0]) ? "A" : ReferenceEquals(signal, roles[1]) ? "B" : "foreign" : "open";
            rows[main ? 1 : 3, index] = !Closed(selected) || E(pending).Length != 0 ? "unknown" : E(blocked).Length > 0 ? "denied" :
                E(selected).OfType<Relation>().Sum(option => E(option).Length) + " strokes";
        }
        var labels = new[] { "Main port publication", "Main receiver action", "Branch port publication", "Branch receiver action" };
        var html = new StringBuilder("<table><thead><tr><th>Exposed read</th>");
        for (var column = 0; column < columns.Length; column++) html.Append($"<th>Event {column + 1}</th>");
        html.Append("</tr></thead><tbody>");
        for (var row = 0; row < 4; row++)
        {
            html.Append("<tr><th>" + labels[row] + "</th>");
            for (var column = 0; column < columns.Length; column++) html.Append("<td>" + H(rows[row, column] ?? "unbound") + "</td>");
            html.Append("</tr>");
        }
        return html.Append("</tbody></table>").ToString();
    }
    private static string Rank(int? rank) => rank?.ToString(CultureInfo.InvariantCulture) ?? "unavailable";
    private static object Counts(IElement source) { var values = Traverse(source).ToArray(); return new { Elements = values.Length, Carriers = values.Count(value => value is Carrier), Locations = values.Count(value => value is CarrierLocation) }; }
    private static IEnumerable<IElement> Traverse(IElement source)
    {
        var seen = new HashSet<IElement>(ReferenceEqualityComparer.Instance); var stack = new Stack<IElement>(); stack.Push(source);
        while (stack.TryPop(out var value)) { if (!seen.Add(value)) continue; yield return value; if (value is Relation row) { stack.Push(row.Left); stack.Push(row.Right); } }
    }
    private static string H(string value) => WebUtility.HtmlEncode(value);
    private static string F(double value) => value.ToString("0.###", CultureInfo.InvariantCulture);
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
    private static bool Closed(Relation scope) => StructuralConstraint.TryReadScope(scope, out _, out var seam, out var tail) && seam.Equals(tail);
    private static Relation S(params IElement[] values) => StructuralConstraint.Closed(values);
}
