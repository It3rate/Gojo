using System.Diagnostics;
using System.Globalization;
using System.IO.Compression;
using System.Text.Json;
using Gojo.Core;
using Gojo.Encoding.Structures;
using Gojo.Experiments.QualitativeExecution;
using static Gojo.Experiments.QualitativeFrieze.Composition.JointNative;
using Band = Gojo.Experiments.QualitativeExecution.QualitativeBand;
using Curve = Gojo.Experiments.QualitativeExecution.QualitativeCurve;
using Parts = Gojo.Experiments.QualitativeFrieze.Composition.OrnamentBandComposition;
using Corpus = Gojo.Experiments.QualitativeFrieze.Composition.OrnamentSubstrateCorpus;
using Study = Gojo.Experiments.QualitativeFrieze.Composition.OrnamentCompositionExperiment;
using Art = Gojo.Experiments.QualitativeFrieze.Composition.BandEvidenceGallery;

namespace Gojo.Experiments.QualitativeFrieze.Composition;

public static class OrnamentSubstrateExperiment
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { WriteIndented = true };
    public static int Run(string[] args)
    {
        var output = Path.Combine("experiments", "QualitativeFrieze", "results", "ornament-substrate"); var limit = int.MaxValue;
        for (var i = 0; i < args.Length; i++)
            if (args[i] == "--output" && i + 1 < args.Length) output = args[++i];
            else if (args[i] == "--limit" && i + 1 < args.Length) limit = int.Parse(args[++i], CultureInfo.InvariantCulture);
            else throw new ArgumentException("Usage: --ornament-substrate [--output DIRECTORY] [--limit COUNT]");
        Directory.CreateDirectory(output); Directory.CreateDirectory(Path.Combine(output, "native"));
        var watch = Stopwatch.StartNew(); var examples = new List<OrnamentSubstrateGallery.Example>(); var results = new List<object>();
        var designs = Corpus.Cases(limit); var crossed = 0; var visited = 0; var pairs = 0; var closed = 0; var curves = 0;
        foreach (var (d, i) in designs.Select((d, i) => (d, i)))
        {
            var unit = d.Ornament.Unit; var f = unit.Template.Frame; var native = Parts.ReadTemplate(f, unit.Template.Surface);
            var lattice = OrnamentSubstrate.Read(f, d.Lattice.Surface);
            var crossingReads = d.Crosses.Select(c => OrnamentSubstrate.ReadCrossEvent(f, c.Surface)).ToArray();
            var visitReads = d.Visits.Select(v => OrnamentSubstrate.ReadVisit(f, v.Surface)).ToArray();
            var cellReads = d.Cells.Select(c => ReadCell(f, c)).ToArray();
            var spanReads = native.Envelopes.Select(e => Band.ReadEnvelope(f, e)).ToArray();
            var joins = native.Threads.SelectMany(t => E(t).Cast<Relation>().Zip(E(t).Cast<Relation>().Skip(1),
                (a, b) => Parts.Seam(f, a, b))).ToArray();
            var palette = Study.Palettes[(i * 3 + 2) % Study.Palettes.Length];
            var colour = Study.AssessColours(unit, palette, false);
            var file = "native/" + d.Id.ToLowerInvariant() + ".gojo.json.gz";
            var selections = new Relation(d.Surface, new Relation(d.Focus, S(d.Cells.Cast<IElement>().ToArray())));
            Save(Path.Combine(output, file), new Relation(d.Surface, new Relation(selections, colour.Surface)));
            var crossingCount = crossingReads.Count(c => c.Crosses == true); var visitCount = visitReads.Count(v => v.AtStart == true || v.AtEnd == true);
            var pairCount = d.Pairs.Count(p => p.Compatible == true); var cellCount = cellReads.Count(c => c);
            var curveCount = spanReads.Count(s => s.Sweep.Spine.Straight == false);
            crossed += crossingCount; visited += visitCount; pairs += pairCount; closed += cellCount; curves += curveCount;
            var audit = new { SubstrateComplete = lattice.Complete, TemplateComplete = native.Complete,
                ColumnLandmarks = lattice.ColumnPhases.Count, RowLandmarks = lattice.RowPhases.Count,
                NativeStrands = native.Threads.Count, Spans = native.Envelopes.Count, CurvedSpans = curveCount,
                Crossings = crossingCount, EndpointVisits = visitCount,
                JoinedLandmarkVisits = visitReads.Count(v => v.JoinedToLandmark),
                SharedSiteThreadSeams = joins.Count(j => j.Joined), TangentThreadSeams = joins.Count(j => j.Tangent == true),
                FullBoundaryCorrespondences = pairCount, BoundaryMismatches = d.Pairs.Count(p => p.Compatible == false),
                OpenBoundaryPairs = d.Pairs.Count(p => p.Compatible is null), ClosedCellCycles = cellCount,
                CellCycles = d.Cells.Count, FrontBackOrders = unit.Crossings.Count(c => Band.ReadOrder(f, c.Surface).Complete),
                DecorativeApplications = unit.Applications.Count, Palette = palette.Name, d.Repertoire,
                NativeProfileApplications = d.Generations.Count,
                ReusableLocalProfiles = d.Generations.Select(g => ((Relation)g.Left).Left).Distinct(ReferenceEqualityComparer.Instance).Count(),
                Limits = "No general curved-intersection discovery, infinite tiling or simple-area/nonoverlap theorem. Boundary compatibility does not weld sites; painted cell interiors are a display selection of closed contours." };
            var evidence = new List<Art.Evidence> {
                new("Independent repeat substrate", $"{lattice.ColumnPhases.Count} × {lattice.RowPhases.Count} landmark phases",
                    "Actual U/V displacement words, two additive routes and occurrence identities", "The substrate supplies the repeat events. Its cells do not require each visible strand or decoration to fit inside a cell."),
                new("Continuous participants", $"{native.Threads.Count} native threads · {joins.Count(j => j.Joined)} shared-site internal seams",
                    "Actual complete straight spans or shared-site curve chains", "A straight strand is not split at its crossing events. Curved sections retain one enclosing thread, shared endpoint and side identities."),
                new("Reusable profile generation", $"{d.Generations.Count} native profile applications",
                    "Shared local control/offset scopes -> actual landmark or word anchor -> additive receipts -> consumed curve controls",
                    "Generation consumes the shared profile and anchor. The subsequent paired-boundary checks independently test the resulting edges."),
                new("Substrate encounters", $"{crossingCount} certified straight crossings · {visitCount} endpoint visits",
                    "Selected actual landmark and actual strand participants", "An endpoint visit does not claim an interior curve intersection. Crossing and joining remain separate questions."),
                new("Opposite boundary correspondence", $"{pairCount} compatible · {d.Pairs.Count(p => p.Compatible == false)} opposed · {d.Pairs.Count(p => p.Compatible is null)} open",
                    "Every ordered control consumed by native substitution, displacement and difference reads", "The entire boundary profile must correspond. Matching endpoints or equal area alone cannot establish matching excursions and recesses."),
                new("Interior cell read", $"{cellCount} closed boundary cycles",
                    "Actual successive section end/start site identities and positions", "These cells reuse the same boundary sections with opposite traversal. Closure does not prove a simple non-self-intersecting region or that no other line bisects it."),
                new("Historical construction family", d.Repertoire, "Primary historical texts and figures linked in the study", "Adaptations of construction ideas, not facsimiles or claims about the original maker's process.") };
            examples.Add(new(d.Id, d.Name, d.Note, RenderSubstrate(d), RenderContinuity(d), RenderCorrespondence(d), RenderDesign(d, palette), evidence, audit, file));
            results.Add(new { d.Id, d.Name, Audit = audit });
            Console.WriteLine($"{d.Id}: {native.Threads.Count} threads; {crossingCount} crosses; {visitCount} visits; {pairCount} paired spans; {cellCount} cells.");
        }
        var summary = new { Designs = designs.Count, CertifiedCrossings = crossed, EndpointVisits = visited,
            BoundaryCorrespondences = pairs, ClosedCellCycles = closed, CurvedSpans = curves,
            Seconds = Math.Round(watch.Elapsed.TotalSeconds, 3),
            Scope = "Finite native substrate-defined generation and exact local correspondence checks. No recognition, aesthetic training or infinite tessellation proof.", Results = results };
        File.WriteAllText(Path.Combine(output, "summary.json"), JsonSerializer.Serialize(summary, Json));
        OrnamentSubstrateGallery.Write(output, summary, examples);
        Console.WriteLine($"Ornament substrate: {designs.Count} designs, {watch.Elapsed.TotalSeconds:0.0}s. {Path.GetFullPath(output)}"); return 0;
    }

    /// <summary>Only a native closed oriented edge cycle. No region simplicity,
    /// bounded area or absence-of-bisecting-edge promise is inferred.</summary>
    public static bool ReadCell(Curve.Frame frame, Relation cell)
    {
        if (!Closed(cell)) return false; var sections = E(cell).OfType<Relation>().Select(frame.Read).ToArray();
        return sections.Length >= 2 && sections.Length == E(cell).Length && sections.All(c => c.Complete) &&
            sections.Where((a, i) => { var b = sections[(i + 1) % sections.Length];
                return !ReferenceEquals(a.End, b.Start) || a.Controls[^1] != b.Controls[0]; }).Any() == false;
    }
    public static Art.ViewBox Bounds(Corpus.Design d)
    {
        var f = d.Ornament.Unit.Template.Frame; var controls = d.Ornament.Unit.Template.Envelopes
            .SelectMany(e => Band.ReadEnvelope(f, e).Sweep.Spine.Controls).ToArray();
        var points = controls.Concat(d.Lattice.Landmarks.Select(l => { f.Vector(l.Position, out var p); return p; })).ToArray();
        var minX = (double)points.Min(p => p.X) - 5; var minY = (double)points.Min(p => p.Y) - 5;
        return new(minX, minY, (double)points.Max(p => p.X) - minX + 5, (double)points.Max(p => p.Y) - minY + 5);
    }
    public static Art.Panel RenderSubstrate(Corpus.Design d)
    {
        var f = d.Ornament.Unit.Template.Frame; var l = OrnamentSubstrate.Read(f, d.Lattice.Surface); var paths = new List<Art.InkPath>();
        Curve.Vector Point(int c, int r) { f.Vector(OrnamentSubstrate.At(l, c, r).Position, out var p); return p; }
        for (var r = 0; r < l.RowPhases.Count; r++) paths.Add(new(Line(Point(0, r), Point(l.ColumnPhases.Count - 1, r)), Stroke: "#879d98", StrokeWidth: .2, Role: "substrate-u", Dash: ".7 .7"));
        for (var c = 0; c < l.ColumnPhases.Count; c++) paths.Add(new(Line(Point(c, 0), Point(c, l.RowPhases.Count - 1)), Stroke: "#879d98", StrokeWidth: .2, Role: "substrate-v", Dash: ".7 .7"));
        foreach (var n in l.Landmarks) { f.Vector(n.Position, out var p); paths.Add(Dot(p, .65, "#335d66", "repeat-landmark")); }
        return new("Repeat substrate and landmarks", paths, "U/V landmarks define the period independently of strand length, width and the visible cell outline. Dots are repeat occurrences, not automatic joins.", Bounds(d), "#f5f1e5");
    }
    public static Art.Panel RenderContinuity(Corpus.Design d)
    {
        var f = d.Ornament.Unit.Template.Frame; var selected = E(d.Focus); var paths = new List<Art.InkPath>();
        foreach (var env in d.Ornament.Unit.Template.Envelopes)
        {
            var s = Band.ReadEnvelope(f, env).Sweep.Spine; var highlight = selected.Any(p => ReferenceEquals(p, env));
            paths.Add(new(CurvePath(s), Stroke: highlight ? "#a74c37" : "#829b98", StrokeWidth: highlight ? 1.35 : .24,
                Opacity: highlight ? 1 : .43, Role: highlight ? "continuous-selected-strand" : "other-strand"));
        }
        var marks = new List<Relation>();
        foreach (var c in d.Crosses)
            if (c.Surface.Right is Relation { Left: Relation pair } && selected.Any(s => ReferenceEquals(s, pair.Left) || ReferenceEquals(s, pair.Right)) &&
                c.Surface.Left is Relation { Right: Relation node }) marks.Add(node);
        foreach (var v in d.Visits)
            if (v.Surface.Left is Relation { Left: Relation { Right: Relation node }, Right: var env } && selected.Any(s => ReferenceEquals(s, env))) marks.Add(node);
        foreach (var node in marks.Distinct<Relation>(ReferenceEqualityComparer.Instance))
            if (node.Left is Relation { Right: Relation position } && f.Vector(position, out var p))
            { paths.Add(Dot(p, 1.15, "#f8e8b6", "event-halo")); paths.Add(Dot(p, .6, "#a74c37", "strand-repeat-event")); }
        return new("One continuous strand and its repeat events", paths,
            "The red path retains its native identity through every gold event marker. Markers select substrate encounters; visibility cuts do not cut the path.", Bounds(d), "#faf6e9");
    }
    public static Art.Panel RenderCorrespondence(Corpus.Design d)
    {
        var f = d.Ornament.Unit.Template.Frame; var paths = new List<Art.InkPath>();
        for (var i = 0; i < d.Cells.Count; i++) if (ReadCell(f, d.Cells[i]))
            paths.Add(new(CellCurvePath(f, d.Cells[i]), Fill: i % 2 == 0 ? "#d6e6dc" : "#e8d5bb", Stroke: "none", StrokeWidth: 0, Role: "selected-closed-cell"));
        foreach (var e in d.Ornament.Unit.Template.Envelopes)
            paths.Add(new(CurvePath(Band.ReadEnvelope(f, e).Sweep.Spine), Stroke: "#94a29b", StrokeWidth: .22, Opacity: .55, Role: "context-strand"));
        // First two distinct primitive directions, avoiding an unreadable fan of
        // every witness. All pair receipts remain in the native packet.
        foreach (var pair in d.Pairs.Take(d.Id is "S010" or "S012" ? 6 : 2))
        {
            var a = f.Read(pair.Source); var b = f.Read(pair.SelectedTarget);
            paths.Add(new(CurvePath(a), Stroke: "#286b8a", StrokeWidth: 1.2, Role: "paired-source-boundary"));
            paths.Add(new(CurvePath(b), Stroke: "#b75b38", StrokeWidth: 1.2, Role: "paired-target-boundary"));
            foreach (var (p, q) in a.Controls.Zip(b.Controls))
            { paths.Add(new(Line(p, q), Stroke: "#9b855d", StrokeWidth: .13, Dash: ".6 .6", Opacity: .65, Role: "control-correspondence")); }
        }
        return new("Paired boundaries and emergent cells", paths,
            "Blue/copper profiles correspond under the actual repeat displacement, including their controls. Pale faces read closed cycles; shared boundary identity and translated compatibility are different relations.", Bounds(d), "#faf6e9");
    }
    public static Art.Panel RenderDesign(Corpus.Design d, Study.Palette palette)
    {
        var panel = Study.Render(d.Ornament, palette, 2); var paths = new List<Art.InkPath>(); var f = d.Ornament.Unit.Template.Frame;
        for (var i = 0; i < d.Cells.Count; i++) if (ReadCell(f, d.Cells[i]))
            paths.Add(new(CellCurvePath(f, d.Cells[i]), Fill: i % 2 == 0 ? palette.Motif : palette.Chain, Stroke: "none", StrokeWidth: 0,
                Opacity: .12, Role: "closed-cell-paint-selection"));
        paths.AddRange(panel.Paths);
        return panel with { Name = palette.Name + " · substrate ornament", Paths = paths, Bounds = Bounds(d),
            Note = "Continuous strands, explicit crossing order where certified, and compatible curved cells. The substrate is available as an overlay; it need not be visible in the finished ornament." };
    }
    private static string CellCurvePath(Curve.Frame frame, Relation cell) => string.Join(" ", E(cell).Cast<Relation>().Select((e, i) => CurvePath(frame.Read(e), i == 0))) + " Z";
    private static string CurvePath(Curve.Read read, bool move = true)
    {
        var c = read.Controls; return (move ? "M " + P(c[0]) + " " : "") + (c.Length == 2 ? "L " + P(c[1]) :
            c.Length == 3 ? "Q " + P(c[1]) + " " + P(c[2]) : "C " + P(c[1]) + " " + P(c[2]) + " " + P(c[3]));
    }
    private static Art.InkPath Dot(Curve.Vector p, double r, string colour, string role) => new(
        $"M {((double)p.X - r).ToString(CultureInfo.InvariantCulture)} {p.Y} a {r} {r} 0 1 0 {2 * r} 0 a {r} {r} 0 1 0 {-2 * r} 0 Z",
        Fill: colour, Stroke: "none", StrokeWidth: 0, Role: role);
    private static string P(Curve.Vector p) => p.X.ToString(CultureInfo.InvariantCulture) + " " + p.Y.ToString(CultureInfo.InvariantCulture);
    private static string Line(Curve.Vector p, Curve.Vector q) => "M " + P(p) + " L " + P(q);
    private static void Save(string path, Relation root)
    {
        var json = GojoStructureCompactJson.Serialize(GojoStructureCompactJson.FromElement("ornament-substrate-v1", root));
        using var file = File.Create(path); using var gzip = new GZipStream(file, CompressionLevel.SmallestSize); using var writer = new StreamWriter(gzip); writer.Write(json);
    }
}
