using System.Diagnostics;
using System.Globalization;
using System.IO.Compression;
using System.Text.Json;
using Gojo.Core;
using Gojo.Patterns;
using Gojo.Encoding.Structures;
using Gojo.Experiments.QualitativeExecution;
using static Gojo.Experiments.QualitativeFrieze.Composition.JointNative;
using Band = Gojo.Experiments.QualitativeExecution.QualitativeBand;
using Curve = Gojo.Experiments.QualitativeExecution.QualitativeCurve;
using Parts = Gojo.Experiments.QualitativeFrieze.Composition.OrnamentBandComposition;
using Corpus = Gojo.Experiments.QualitativeFrieze.Composition.OrnamentCompositionCorpus;
using Art = Gojo.Experiments.QualitativeFrieze.Composition.BandEvidenceGallery;
using Intent = Gojo.Experiments.QualitativeFrieze.Composition.OrnamentColourIntent;
using Stats = Gojo.Experiments.QualitativeExecution.StatisticalEvidence;

namespace Gojo.Experiments.QualitativeFrieze.Composition;

public static class OrnamentCompositionExperiment
{
    public sealed record Palette(string Name, string Ground, string Scaffold, string Chain, string Motif, string Accent, string Edge, string Separator);
    public sealed record ColourRead(IReadOnlyList<Intent.Judgement> Checks, Intent.AggregateRead Aggregate,
        string Compatibility, Relation Surface);
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { WriteIndented = true };
    public static readonly Palette[] Palettes = [
        new("Ivory, teal and madder", "#f4ead1", "#c1bba4", "#275f61", "#ac4e3a", "#e0b95e", "#233b3e", "#fbf1d8"),
        new("Midnight and old gold", "#182f45", "#3c5466", "#c89e54", "#75a49b", "#f4dd9b", "#102335", "#eddfbd"),
        new("Cobalt and coral", "#f1eee1", "#b7c7c6", "#294c7d", "#c15b49", "#dfb44c", "#273c52", "#fff4d9"),
        new("Plum and celadon", "#eee7cf", "#c2c5ad", "#654559", "#668f7b", "#c99c43", "#3b343f", "#f8efd8"),
        new("Copper on charcoal", "#282f33", "#4b5658", "#c07852", "#c5c9aa", "#edce76", "#20282b", "#eddfbc"),
        new("Indigo and saffron", "#ece5cc", "#b6b8ae", "#344f77", "#c28d34", "#b74938", "#283843", "#fff2cb"),
        new("Parchment and olive", "#f0e8ce", "#c6c3a4", "#636d3e", "#ac6954", "#d1ae57", "#3e4939", "#fff1d2"),
        new("Aubergine and rose", "#342b3c", "#57485d", "#b7925a", "#c78680", "#eee0bd", "#272030", "#ecd9ab"),
        new("Sea green and vermilion", "#e8e9d6", "#b0c2b2", "#2f716e", "#bd503b", "#d8af50", "#264748", "#fbf2d8"),
        new("Porcelain blue", "#eaf0e8", "#b3c8ca", "#365f86", "#839b94", "#b75c43", "#2d4659", "#ffefd5"),
        new("Garnet and turquoise", "#efe2cb", "#c5b9a8", "#843e48", "#3f8b89", "#d2ab54", "#493b40", "#fff0d6"),
        new("Ink, gold and jade", "#1d3431", "#415c51", "#bba365", "#83a58d", "#e8d7a4", "#172b29", "#f1dfae"),
        new("Terracotta and mineral blue", "#f2e2ca", "#c7b79e", "#a9573b", "#4b7f92", "#e8c16c", "#4c3b34", "#fff1d5"),
        new("Deep blue and apricot", "#26334a", "#47516b", "#bdbb85", "#d49172", "#f2d99f", "#1d293c", "#e9dcb9"),
        new("Verdigris and ochre", "#e6e5ce", "#afbaaa", "#397368", "#ad8038", "#a54d44", "#304c42", "#fff2d0"),
        new("Mulberry and pale gold", "#eee7dc", "#c6bfc0", "#694b68", "#9b7756", "#cba755", "#403541", "#fff4dc") ];

    public static int Run(string[] args)
    {
        var output = Path.Combine("experiments", "QualitativeFrieze", "results", "ornament-composition"); var limit = int.MaxValue;
        for (var i = 0; i < args.Length; i++)
            if (args[i] == "--output" && i + 1 < args.Length) output = args[++i];
            else if (args[i] == "--limit" && i + 1 < args.Length && int.TryParse(args[++i], out var count) && count > 0) limit = count;
            else throw new ArgumentException("Usage: --ornament-composition [--output DIRECTORY] [--limit COUNT]");
        Directory.CreateDirectory(output); Directory.CreateDirectory(Path.Combine(output, "native"));
        var watch = Stopwatch.StartNew(); var examples = new List<OrnamentCompositionGallery.Example>(); var results = new List<object>();
        var cases = Corpus.Cases(limit).ToArray(); var curveCount = 0; var bandCount = 0; var complete = 0; var checkMatches = 0; var checkConflicts = 0;
        foreach (var (sample, index) in cases.Select((sample, index) => (sample, index)))
        {
            var unit = sample.Unit; var frame = unit.Template.Frame;
            var native = Parts.ReadTemplate(frame, unit.Template.Surface);
            if (native.Complete) complete++;
            var reads = native.Envelopes.Select(e => Band.ReadEnvelope(frame, e)).ToArray();
            bandCount += reads.Count(e => e.Occupied == true && e.Sweep.Collapsed == false);
            curveCount += reads.Count(e => e.Sweep.Spine.Straight == false);
            var palette = Palettes[index % Palettes.Length]; var alternate = Palettes[(index + 7) % Palettes.Length];
            var colours = AssessColours(unit, palette, false); var otherColours = AssessColours(unit, alternate, true);
            var control = palette with { Chain = palette.Ground, Motif = palette.Ground, Scaffold = palette.Ground, Accent = palette.Ground, Edge = palette.Ground };
            var rejected = AssessColours(unit, control, false);
            checkMatches += colours.Aggregate.Supported + otherColours.Aggregate.Supported;
            checkConflicts += colours.Aggregate.Contradicted + otherColours.Aggregate.Contradicted;
            var seams = unit.Seams.Select(s => s.Left is Relation { Left: Relation a, Right: Relation b } ? Parts.Seam(frame, a, b) : Parts.Seam(frame, S(), S())).ToArray();
            var nativeName = "native/" + sample.Id.ToLowerInvariant() + ".gojo.json.gz";
            var roleScopes = Roles(unit);
            var selections = S(new Relation(unit.Surface, roleScopes[0]), new Relation(unit.Surface, unit.Highlight),
                new Relation(unit.Surface, new Relation(S(roleScopes.Skip(1).Cast<IElement>().ToArray()), roleScopes[0])),
                new Relation(unit.Surface, S(colours.Surface, otherColours.Surface)));
            Save(Path.Combine(output, nativeName), new Relation(unit.Surface, new Relation(selections, new Relation(colours.Surface,
                new Relation(otherColours.Surface, rejected.Surface)))));
            var audit = new { TemplateComplete = native.Complete, NativeThreads = native.Threads.Count, Envelopes = reads.Length,
                CurvedSpines = reads.Count(e => e.Sweep.Spine.Straight == false), CurvedBoundaries = reads.Sum(e => (e.Sweep.SideA.Straight == false ? 1 : 0) + (e.Sweep.SideB.Straight == false ? 1 : 0)), OccupiedBands = reads.Count(e => e.Occupied == true),
                CollapsedStrokes = reads.Count(e => e.Sweep.Collapsed == true), CompoundApplications = unit.Applications.Count,
                RepeatControllers = unit.Repetitions.Count, ExplicitCrossingOrders = unit.Crossings.Count(c => Band.ReadOrder(frame, c.Surface).Complete),
                CurrentSeamQuestions = seams.Length, CoincidentSeams = seams.Count(s => s.Coincident == true),
                TangentSeams = seams.Count(s => s.Tangent == true), SameSideSeams = seams.Count(s => s.SideAgreement == true), ExchangedSideSeams = seams.Count(s => s.SideExchange == true),
                JoinedSeams = seams.Count(s => s.Joined),
                MainPalette = palette.Name, AlternatePalette = alternate.Name,
                MainChecks = new { colours.Aggregate.Supported, colours.Aggregate.Contradicted, colours.Aggregate.Unresolved, colours.Compatibility },
                AlternateChecks = new { otherColours.Aggregate.Supported, otherColours.Aggregate.Contradicted, otherColours.Aggregate.Unresolved, otherColours.Compatibility },
                FlatControl = new { rejected.Aggregate.Supported, rejected.Aggregate.Contradicted, rejected.Aggregate.Unresolved },
                sample.Construction, sample.Repertoire, sample.Limit,
                ColourBasis = "Published ordinal comparisons of squared sRGB code-distance: chain/motif against ground, accent against motif, edge against adjacent fills. A declared display proxy, not perceptual depth or beauty.",
                Unimplemented = "Global region union/nonintersection; general curved crossing discovery; inverse skeleton inference; learned salience or pigment balance" };
            var evidence = new List<Art.Evidence> {
                new("Whole-segment repetition", $"{unit.Applications.Count} applications · {unit.Repetitions.Count} repeat controllers",
                    "Reusable native compounds, consumed substitutions, displaced controls and retained source pairs", sample.Construction),
                new("Curves and band occupancy", $"{reads.Count(e => e.Sweep.Spine.Straight == false)} curved spans · {reads.Count(e => e.Occupied == true)} occupied supports",
                    "Actual polynomial curves and corresponding side profiles", "The curved silhouettes come from native control-word interpolation. These are chosen lateral profiles, not exact normal offsets."),
                new("Seam questions", seams.Length == 0 ? "No explicit longitudinal seam question in this design" : $"{seams.Count(s => s.Coincident == true)} coincident · {seams.Count(s => s.Tangent == true)} tangent · {seams.Count(s => s.Joined)} welded",
                    "Actual exposed end ports, positions, tangents and side correspondences", "A visually continuous tile edge does not create shared site identity. Open or opposed seam evidence stays available."),
                new("Main colour intentions", $"{colours.Aggregate.Supported} supported · {colours.Aggregate.Contradicted} opposed · {colours.Aggregate.Unresolved} open",
                    "Separate desired contrast order and published ordinal observations", "Main chain versus motif contrast, accent versus its host fill, edge versus fill, and an unresolved perceptual read. Compatibility " + colours.Compatibility + " is an authored aggregate channel, not beauty probability."),
                new("Alternate colour focus", $"{otherColours.Aggregate.Supported} supported · {otherColours.Aggregate.Contradicted} opposed · {otherColours.Aggregate.Unresolved} open",
                    "Same geometry and identities; another focus/paint assignment", "The alternate asks for motif emphasis. A colour proxy can disagree with that intention; the discrepancy remains visible."),
                new("Flat-paint counterexample", $"{rejected.Aggregate.Contradicted} contrast intentions opposed",
                    "Identical geometry with role colours collapsed to the ground", "The negative control tests the declared contrast relations rather than geometric or aesthetic correctness."),
                new("Jones guidance", "37 propositions in the consulted original; selected colour guidance applied",
                    "14, 16, 22 and 28–34, with geometric groundwork and tangential growth", "Strong accents over quieter masses, readable parts and separating edges are intentions. Historical pigment ratios and architectural recession are not universal flat-image laws.") };
            examples.Add(new(sample.Id, sample.Name, sample.Note,
                Render(sample, palette, 0), Render(sample, palette, 1), Render(sample, palette, 2), Render(sample, alternate, 3),
                evidence, audit, nativeName));
            results.Add(new { sample.Id, sample.Name, Audit = audit });
            Console.WriteLine($"{sample.Id}: {reads.Length} spans, {unit.Applications.Count} compound applications.");
        }
        var summary = new { Designs = cases.Length, Readings = cases.Length * 2, CompleteTemplates = complete, CurvedSpans = curveCount,
            OccupiedBands = bandCount, SupportedColourChecks = checkMatches, OpposedColourChecks = checkConflicts,
            Seconds = Math.Round(watch.Elapsed.TotalSeconds, 3),
            Scope = "Authored native compound ornament generation and ordinal colour-intention checks; no recognition or aesthetic model training.", Results = results };
        File.WriteAllText(Path.Combine(output, "summary.json"), JsonSerializer.Serialize(summary, Json));
        OrnamentCompositionGallery.Write(output, summary, examples);
        Console.WriteLine($"Ornament composition: {cases.Length} designs, {watch.Elapsed.TotalSeconds:0.0}s. {Path.GetFullPath(output)}");
        return 0;
    }

    /// <summary>Read the selected role scopes from the native unit root.</summary>
    public static Relation[] Roles(Corpus.Unit unit)
    {
        if (!ReferenceEquals(unit.Surface.Left, unit.Template.Surface) || unit.Surface.Right is not Relation { Left: Relation roles } ||
            !Closed(roles) || E(roles).Length != 4 || E(roles).Any(r => r is not Relation scope || !Closed(scope))) return [];
        var scopes = E(roles).Cast<Relation>().ToArray(); var actual = Parts.ReadTemplate(unit.Template.Frame, unit.Template.Surface).Envelopes;
        return scopes.All(s => E(s).All(e => actual.Any(a => ReferenceEquals(a, e)))) ? scopes : [];
    }

    public static ColourRead AssessColours(Corpus.Unit unit, Palette palette, bool motifFocus)
    {
        var roles = Roles(unit); var source = unit.Surface; var ordinal = new OrdinalReferenceFrame(); var ground = new Placeholder();
        var published = new Placeholder(); var zeroContrast = new Relation(ground, ground);
        var chainContrast = new Relation(roles[1], ground); var motifContrast = new Relation(roles[2], ground);
        var frameContrast = new Relation(roles[0], ground); var accentContrast = new Relation(roles[3], roles[2]);
        var edgeContrast = new Relation(new Relation(roles[1], roles[2]), new Placeholder());
        var perceivedSubject = new Relation(source, StructuralConstraint.Open(roles[motifFocus ? 2 : 1]));
        var perceivedReference = new Relation(source, StructuralConstraint.Open(roles[motifFocus ? 1 : 2]));
        var selected = S(chainContrast, motifContrast, frameContrast, accentContrast, edgeContrast, zeroContrast, perceivedSubject, perceivedReference);
        var context = new Relation(source, new Relation(selected, published)); var checks = new List<Intent.Judgement>();
        // A second observer has an explicitly open resolution/context. Its
        // unprovided attention/depth read is not another measurement in the
        // sRGB code-distance order.
        var perceptual = new OrdinalReferenceFrame(resolution: StructuralConstraint.Open(source, selected));
        void Check(IElement a, IElement b, int observed, bool open = false, OrdinalReferenceFrame? selectedFrame = null)
        {
            var basis = selectedFrame ?? ordinal;
            var intent = Intent.Declare(source, selected, a, b, basis, S(basis.Higher), context);
            var observer = ReferenceEquals(basis, perceptual) ? (IElement)perceptual.Resolution : published;
            var observation = basis.Observe(source, a, b, observer,
                open ? StructuralConstraint.Open(basis.Higher) : S(observed > 0 ? basis.Higher : observed < 0 ? basis.Lower : basis.Equal), context);
            checks.Add(Intent.Assess(basis, intent, observation));
        }
        var chain = Distance(palette.Chain, palette.Ground); var motif = Distance(palette.Motif, palette.Ground);
        var accent = Distance(palette.Accent, palette.Motif);
        Check(motifFocus ? motifContrast : chainContrast, motifFocus ? chainContrast : motifContrast,
            motifFocus ? motif.CompareTo(chain) : chain.CompareTo(motif));
        Check(accentContrast, zeroContrast, accent.CompareTo(0));
        Check(edgeContrast, zeroContrast, Math.Min(Distance(palette.Edge, palette.Chain), Distance(palette.Edge, palette.Motif)).CompareTo(0));
        // A display contrast does not settle a person's depth/attention read.
        Check(perceivedSubject, perceivedReference, 0, open: true, selectedFrame: perceptual);
        var model = new Placeholder(); var control = new Placeholder();
        var stats = new Stats.Frame(context, model, control); var calibration = CompatibilityEvidence.Calibrate(stats, 15, 16);
        var aggregate = Intent.Aggregate(source, model, checks);
        Intent.TryLikelihood(aggregate, calibration, out var partition, out var receipt);
        var compatibility = Stats.TryReadPartition(partition, stats.Unit, out var ratio) ? ratio.ToString() : "Open";
        var answers = S(checks.Select((c, i) => (IElement)Intent.ObserveAnswer(i == 3 ? perceptual : ordinal, c, new Placeholder(), motifFocus)).ToArray());
        return new(checks, aggregate, compatibility, new Relation(context, new Relation(aggregate.Evidence,
            new Relation(new Relation(calibration, receipt), answers))));
    }

    private static int Distance(string a, string b)
    {
        static int Channel(string colour, int n) => int.Parse(colour.Substring(n, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        return new[] { 1, 3, 5 }.Sum(n => { var d = Channel(a, n) - Channel(b, n); return d * d; });
    }

    public static Art.Panel Render(Corpus.Design source, Palette palette, int mode)
    {
        var unit = source.Unit; var frame = unit.Template.Frame; var template = Parts.ReadTemplate(frame, unit.Template.Surface);
        var roles = Roles(unit); var fills = new List<Art.InkPath>(); var edges = new List<Art.InkPath>();
        var selected = E(unit.Highlight); var bounds = new Art.ViewBox(0, 0, source.Width, source.Height);
        var orders = unit.Crossings.Select(c => Band.ReadOrder(frame, c.Surface)).Where(o => o.Complete).ToArray();
        int Role(Relation env) => Array.FindIndex(roles, scope => E(scope).Any(e => ReferenceEquals(e, env)));
        string Colour(int role) => role switch { 0 => palette.Scaffold, 1 => palette.Chain, 2 => palette.Motif, _ => palette.Accent };
        foreach (var thread in template.Threads)
        {
            var envs = E(thread).Cast<Relation>().Select(e => Band.ReadEnvelope(frame, e)).ToArray();
            if (envs.Length == 0) continue;
            var role = Role(envs[0].Surface); var colour = Colour(role);
            if (role == 0 && mode >= 2) continue;
            var highlighted = envs.Any(e => selected.Any(s => ReferenceEquals(s, e.Surface)));
            var opacity = mode == 1 && !highlighted ? .17 : 1;
            if (mode == 0)
            {
                if (role == 0 || role == 1) foreach (var e in envs.Where(e => e.Sweep.Spine.Complete))
                    edges.Add(new(CurvePath(e.Sweep.Spine), Stroke: role == 0 ? "#8ea6a2" : "#405f69", StrokeWidth: role == 0 ? .11 : .16,
                        Role: role == 0 ? "construction-frame" : "structural-spine", Opacity: role == 0 ? .75 : 1));
                continue;
            }
            var cuts = new List<Art.MaskCut>();
            foreach (var order in orders.Where(o => envs.Any(e => ReferenceEquals(e.Sweep.Spine.Section, o.Back))))
            {
                var front = template.Envelopes.Select(e => Band.ReadEnvelope(frame, e)).First(e => ReferenceEquals(e.Sweep.Spine.Section, order.Front));
                if (front.Sweep.Complete && front.Occupied == true)
                { cuts.Add(new(Fill([front.Sweep]))); cuts.Add(new(CurvePath(front.Sweep.SideA), .28)); cuts.Add(new(CurvePath(front.Sweep.SideB), .28)); }
            }
            if (role is 0 or 3 || envs.All(e => e.Sweep.Collapsed == true))
            {
                foreach (var e in envs.Where(e => e.Sweep.Spine.Complete))
                    edges.Add(new(CurvePath(e.Sweep.Spine), Stroke: colour, StrokeWidth: role == 0 ? .11 : .18,
                        Role: role == 0 ? "recessive-frame" : "fine-spine-accent", Opacity: opacity, Cuts: cuts));
                continue;
            }
            foreach (var run in BandEvidenceExperiment.ConnectedRuns(envs, occupiedOnly: true))
                fills.Add(new(Fill(run), Fill: colour, Stroke: "none", StrokeWidth: 0, Role: "native-occupied-band", Opacity: opacity, Cuts: cuts));
            foreach (var e in envs.Where(e => e.Sweep.Complete))
            {
                foreach (var side in new[] { e.Sweep.SideA, e.Sweep.SideB })
                {
                    edges.Add(new(CurvePath(side), Stroke: palette.Separator, StrokeWidth: .3, Role: "separating-edge", Opacity: opacity, Cuts: cuts));
                    edges.Add(new(CurvePath(side), Stroke: palette.Edge, StrokeWidth: .095, Role: "boundary", Opacity: opacity, Cuts: cuts));
                }
                if (e.Start.Capped == true) edges.Add(new(Cap(e.Sweep, true), Stroke: palette.Edge, StrokeWidth: .095, Role: "visible-cap", Opacity: opacity, Cuts: cuts));
                if (e.End.Capped == true) edges.Add(new(Cap(e.Sweep, false), Stroke: palette.Edge, StrokeWidth: .095, Role: "visible-cap", Opacity: opacity, Cuts: cuts));
            }
        }
        return new(mode switch { 0 => "Underlying frame and main paths", 1 => "One repeated compound highlighted", 2 => palette.Name + " · chain focus", _ => palette.Name + " · motif focus" },
            fills.Concat(edges).ToArray(), mode switch {
                0 => "Native construction-frame paths and primary spines. The view filters out filled leaves and fine veins.",
                1 => "A selected compound is prominent; its repeated siblings are subdued. Source, transform and descendant correspondences remain native.",
                2 => "The coloured ornament is sampled from native curves and selected ruled supports. Pale edging separates coloured masses.",
                _ => "The same native geometry and identities receive another palette and colour intention. Perceptual recession remains a hypothesis." }, bounds,
            mode == 0 ? "#f5f1e5" : palette.Ground);
    }
    private static string Fill(IReadOnlyList<Band.SweepRead> sweeps)
    {
        var a = string.Join(" ", sweeps.Select((s, i) => CurvePath(s.SideA, move: i == 0)));
        var reversed = sweeps.Reverse().ToArray();
        return a + " L " + Point(reversed[0].SideB.Controls[^1]) + " " + string.Join(" ", reversed.Select(s => CurvePath(s.SideB, reverse: true, move: false))) + " Z";
    }
    private static string Cap(Band.SweepRead s, bool start) => "M " + Point(start ? s.SideA.Controls[0] : s.SideA.Controls[^1]) + " L " + Point(start ? s.SideB.Controls[0] : s.SideB.Controls[^1]);
    private static string CurvePath(Curve.Read read, bool reverse = false, bool move = true)
    {
        var c = reverse ? read.Controls.Reverse().ToArray() : read.Controls;
        return (move ? "M " + Point(c[0]) + " " : "") + (c.Length == 2 ? "L " + Point(c[1]) :
            c.Length == 3 ? "Q " + Point(c[1]) + " " + Point(c[2]) : "C " + Point(c[1]) + " " + Point(c[2]) + " " + Point(c[3]));
    }
    private static string Point(Curve.Vector v) => v.X.ToString(CultureInfo.InvariantCulture) + " " + v.Y.ToString(CultureInfo.InvariantCulture);
    private static void Save(string path, Relation root)
    {
        var json = GojoStructureCompactJson.Serialize(GojoStructureCompactJson.FromElement("ornament-composition-v1", root));
        using var file = File.Create(path); using var gzip = new GZipStream(file, CompressionLevel.SmallestSize); using var writer = new StreamWriter(gzip); writer.Write(json);
    }
}
