using System.Diagnostics;
using System.Globalization;
using System.IO.Compression;
using System.Text.Json;
using Gojo.Core;
using Gojo.Encoding.Structures;
using static Gojo.Experiments.QualitativeFrieze.Composition.JointNative;
using Band = Gojo.Experiments.QualitativeExecution.QualitativeBand;
using Curve = Gojo.Experiments.QualitativeExecution.QualitativeCurve;
using Corpus = Gojo.Experiments.QualitativeFrieze.Composition.OrnamentInteriorCorpus;
using Parts = Gojo.Experiments.QualitativeFrieze.Composition.OrnamentBandComposition;
using Study = Gojo.Experiments.QualitativeFrieze.Composition.OrnamentCompositionExperiment;
using Art = Gojo.Experiments.QualitativeFrieze.Composition.BandEvidenceGallery;

namespace Gojo.Experiments.QualitativeFrieze.Composition;

public static class OrnamentInteriorExperiment
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { WriteIndented = true };
    public static readonly Study.Palette[] Palettes = [
        new("Midnight brocade", "#173e46", "#3f6870", "#c8a35b", "#e9d4a3", "#c57b52", "#233e43", "#e0c795"),
        new("Madder and parchment", "#f0e3c7", "#b5af92", "#75654e", "#a55248", "#d5ac5c", "#503d3a", "#f4e5c3"),
        new("Indigo counterfield", "#eee5d3", "#314d69", "#708397", "#b5a988", "#b87050", "#3a4c57", "#eee4cd"),
        new("Persimmon trefoils", "#ead8ae", "#978b64", "#6f7152", "#a85636", "#ead4a0", "#65452f", "#f3e2b9"),
        new("Azure and vellum", "#cbdcdf", "#7a9da0", "#6b7870", "#385d70", "#dbb56a", "#304f5b", "#e8dfc5"),
        new("Aubergine ogee", "#e3d9c9", "#624b64", "#8a6d7b", "#926775", "#aa6345", "#4b414b", "#e8ddc8"),
        new("Peacock fans", "#234d50", "#49716e", "#baa36d", "#d9d1a4", "#bd7855", "#233e3a", "#ddcb94"),
        new("Coral scrollwork", "#f0ddc5", "#c2a58d", "#8b6c58", "#b66150", "#56766e", "#664d43", "#f0e2c6"),
        new("Moss counterchange", "#d5d8c3", "#4d675a", "#738276", "#a8b19c", "#bd8a51", "#3d574b", "#e2dcc1"),
        new("Cobalt garden seals", "#e9dec3", "#a7b2aa", "#7c8c84", "#3d607b", "#bd7848", "#3a5261", "#ece2c7"),
        new("Terracotta ground", "#dcbda4", "#8c584c", "#a78266", "#d4c9aa", "#566b5b", "#664a3e", "#e9d2ae"),
        new("Oblique jade", "#203e40", "#597774", "#b5a36d", "#c4cfb3", "#d18b5e", "#234440", "#d9cca2") ];

    public static int Run(string[] args)
    {
        var output = System.IO.Path.Combine("experiments", "QualitativeFrieze", "results", "ornament-interior"); var limit = int.MaxValue; string? caseId = null; var preview = false;
        for (var i = 0; i < args.Length; i++)
            if (args[i] == "--output" && i + 1 < args.Length) output = args[++i];
            else if (args[i] == "--limit" && i + 1 < args.Length) limit = int.Parse(args[++i], CultureInfo.InvariantCulture);
            else if (args[i] == "--case" && i + 1 < args.Length) caseId = args[++i];
            else if (args[i] == "--preview") preview = true;
            else throw new ArgumentException("Usage: --ornament-interior [--output DIRECTORY] [--limit COUNT] [--case F012] [--preview]");
        Directory.CreateDirectory(output); Directory.CreateDirectory(System.IO.Path.Combine(output, "native"));
        var watch = Stopwatch.StartNew(); var examples = new List<OrnamentInteriorGallery.Example>(); var audits = new List<object>();
        foreach (var d in Corpus.Cases(limit, caseId))
        {
            var f = d.Unit.Template.Frame; var palette = Palettes[int.Parse(d.Id.AsSpan(1)) - 1];
            var colours = Study.AssessColours(d.Unit, palette, !d.GroundFocus);
            var archive = "native/" + d.Id.ToLowerInvariant() + ".gojo.json.gz";
            if (!preview) Save(System.IO.Path.Combine(output, archive), new Relation(d.Surface, colours.Surface));
            var seedCounts = d.Placements.Select(p => p.Application.Source.Envelopes.Count).ToArray();
            var audit = new { PreservedMeshSpans = d.Mesh.Template.Envelopes.Count, InteriorCompounds = d.Placements.Count,
                MinimumCompoundStrokes = seedCounts.Min(), MaximumCompoundStrokes = seedCounts.Max(),
                ClosedBoundaryReads = d.Placements.Count(p => p.Interior.ClosedBySite),
                ScaffoldReads = d.Placements.Count(p => p.Interior.Scaffold),
                ExactEndpointAverageCentres = d.Placements.Count(p => p.Interior.CentreComplete),
                CornerRelativeConstructionFoci = d.Placements.Count(p => !ReferenceEquals(p.AnchorRead, p.Interior.Surface)),
                ReusedLocalTemplates = d.Placements.Select(p => p.Application.Source.Surface).Distinct(ReferenceEqualityComparer.Instance).Count(),
                NativePlacementReceipts = d.Placements.Count, NativeGuidePlacements = d.Placements.Count,
                SourceCrossings = d.Previous.Unit.Crossings.Count, OutputCrossings = d.Unit.Crossings.Count,
                Focus = d.GroundFocus ? "Compartment ground and mesh" : "Interior flower and fan masses", Palette = palette.Name,
                Limits = "Authored compartment selection and local motifs. No generic face discovery, fill containment, general collision solver, metric circle or trained aesthetic judgement." };
            var beforePalette = Study.Palettes[((int.Parse(d.Id.AsSpan(1)) - 1) * 3 + 2) % Study.Palettes.Length];
            var before = OrnamentIntegratedExperiment.Render(d.Previous, beforePalette) with { Name = "Previous rooted-leaf pass", Bounds = Bounds(d) };
            var evidence = new Art.Evidence[] {
                new("Interior selection", $"{audit.ClosedBoundaryReads} closed cycles · {audit.ScaffoldReads} scaffolds", "Actual source curve cycles, or selected native mesh ports", "A closed boundary read and a proposed interior scaffold are separate. Neither certifies general containment."),
                new("Compound enrichment", $"{audit.MinimumCompoundStrokes}–{audit.MaximumCompoundStrokes} strokes per compound", "Reusable local curves, bands and internal guides", "Several ribs, lobes, scrolls and centre elements work as one compound; this is a construction count, not a cognitive complexity estimate."),
                new("Placement dependency", $"{audit.NativePlacementReceipts} actual anchor-consuming applications", "Interior read → explicit anchor → native substitution and sum receipts", "The native application consumes the actual derived centre word or a declared corner-relative focus; an unrepresentable centre is never rounded."),
                new("Colour focus", audit.Focus, "Connected selection and colour intent; historical design heuristics", "Colour and stroke choices deliberately promote a layer. Display contrast does not prove perceptual attention."),
                new("Preserved mesh", $"{audit.PreservedMeshSpans} original spans · {audit.OutputCrossings} crossing orders", "Exact native envelope identities, repetition substrate and seam correspondence", "The old detail is retained as provenance, outside the new output. Even outlines are a selected centre-path presentation, not a mesh mutation.") };
            examples.Add(new(d.Id, d.Name, d.Note, before, Render(d, palette), Construction(d), Mesh(d, palette), evidence, audit, preview ? null : archive));
            audits.Add(new { d.Id, d.Name, Audit = audit });
            Console.WriteLine($"{d.Id}: {audit.InteriorCompounds} interior compounds; {audit.MinimumCompoundStrokes}–{audit.MaximumCompoundStrokes} strokes; {watch.Elapsed.TotalSeconds:0.0}s.");
        }
        var summary = new { Designs = examples.Count, InteriorCompounds = audits.Count == 0 ? 0 : examples.Sum(e => JsonSerializer.SerializeToElement(e.Audit).GetProperty("InteriorCompounds").GetInt32()),
            Seconds = Math.Round(watch.Elapsed.TotalSeconds, 3), Preview = preview,
            Scope = "Compound interior enrichment over the preserved native mesh, with authored selection and visual review. No training or global non-overlap claim.", Results = audits };
        File.WriteAllText(System.IO.Path.Combine(output, "summary.json"), JsonSerializer.Serialize(summary, Json));
        OrnamentInteriorGallery.Write(output, summary, examples);
        Console.WriteLine($"Interior ornaments: {examples.Count} designs; {watch.Elapsed.TotalSeconds:0.0}s. {System.IO.Path.GetFullPath(output)}"); return 0;
    }

    public static Art.ViewBox Bounds(Corpus.Design d) => OrnamentIntegratedExperiment.Bounds(d.Previous);
    public static Art.Panel Mesh(Corpus.Design d, Study.Palette palette)
    {
        var f = d.Unit.Template.Frame; var paths = new List<Art.InkPath>();
        var order = d.Unit.Crossings.Select(c => Band.ReadOrder(f, c.Surface)).Where(c => c.Complete).ToArray();
        foreach (var env in d.Mesh.Template.Envelopes)
        {
            var read = Band.ReadEnvelope(f, env).Sweep.Spine; var cuts = new List<Art.MaskCut>();
            foreach (var o in order.Where(o => ReferenceEquals(o.Back, read.Section)))
                cuts.Add(new(Path(f.Read(o.Front!)), 2.2));
            // Constant-width centre-path presentation: the native envelope and
            // its width evidence are retained, but not the focus of this read.
            var width = d.GroundFocus ? 1.12 : .7;
            paths.Add(new(Path(read), Stroke: palette.Edge, StrokeWidth: width + .36, Cuts: cuts, Role: "even-mesh-outline"));
            paths.Add(new(Path(read), Stroke: palette.Scaffold, StrokeWidth: width, Cuts: cuts, Role: "even-mesh-ribbon"));
        }
        return new("Preserved mesh · even outlines", paths, "The same source curves and crossing order, shown with even stroke widths. This presentation does not alter the native band's width or topology.", Bounds(d), palette.Ground);
    }

    public static Art.Panel Render(Corpus.Design d, Study.Palette palette)
    {
        var f = d.Unit.Template.Frame; var paths = new List<Art.InkPath>();
        // A source closed cycle can receive a selected ground colour. Scaffold
        // cases remain unfilled rather than gaining invented closed cells.
        for (var i = 0; i < d.Placements.Count; i++)
        {
            var interior = d.Placements[i].Interior;
            if (!interior.ClosedBySite) continue;
            var path = string.Join(" ", interior.Sections.Select((s, j) => Path(f.Read(s), j == 0))) + " Z";
            paths.Add(new(path, Fill: i % 2 == 0 ? palette.Scaffold : palette.Separator, Stroke: "none", StrokeWidth: 0,
                Opacity: d.GroundFocus ? .30 : .10, Role: "selected-compartment-ground"));
        }
        paths.AddRange(Mesh(d, palette).Paths);
        foreach (var p in d.Placements)
        {
            if (p.BoundaryBranch is { } branch)
                paths.Add(new(Path(Band.ReadEnvelope(f, branch).Sweep.Spine), Stroke: palette.Chain, StrokeWidth: .48, Role: "boundary-rooted-tangent-branch"));
            var roles = p.Roles.Select(r => E(r)).ToArray();
            foreach (var env in p.Application.Output.Envelopes)
            {
                var role = Array.FindIndex(roles, group => group.Any(e => ReferenceEquals(e, env)));
                var read = Band.ReadEnvelope(f, env).Sweep;
                var colour = role switch { 1 => palette.Chain, 2 => palette.Motif, _ => palette.Accent };
                if (read.Collapsed == true)
                    paths.Add(new(Path(read.Spine), Stroke: colour, StrokeWidth: role == 1 ? .45 : .28, Role: role == 1 ? "compound-scroll-rib" : "compound-inner-detail"));
                else
                {
                    paths.Add(new(Fill(read), Fill: colour, Stroke: palette.Separator, StrokeWidth: .30, Role: "compound-petal-mass"));
                    paths.Add(new(Fill(read), Stroke: palette.Edge, StrokeWidth: .13, Role: "even-petal-outline"));
                }
            }
        }
        return new("Interior ornament · " + palette.Name, paths, d.Note, Bounds(d), palette.Ground);
    }
    public static Art.Panel Construction(Corpus.Design d)
    {
        var f = d.Unit.Template.Frame; var paths = new List<Art.InkPath>();
        foreach (var env in d.Mesh.Template.Envelopes) paths.Add(new(Path(Band.ReadEnvelope(f, env).Sweep.Spine), Stroke: "#73908b", StrokeWidth: .3, Role: "mesh-context", Opacity: .7));
        foreach (var env in d.Guides.Envelopes) paths.Add(new(Path(Band.ReadEnvelope(f, env).Sweep.Spine), Stroke: "#bd7e38", StrokeWidth: .4, Role: "interior-construction-line", Dash: ".7 .5"));
        foreach (var p in d.Placements.Where(p => p.BoundaryBranch is not null))
            paths.Add(new(Path(Band.ReadEnvelope(f, p.BoundaryBranch!).Sweep.Spine), Stroke: "#9e4537", StrokeWidth: .5, Role: "boundary-tangent-construction"));
        foreach (var p in d.Placements)
            if (f.Vector(p.Anchor, out var at)) paths.Add(new($"M {at.X - 1} {at.Y} L {at.X + 1} {at.Y} M {at.X} {at.Y - 1} L {at.X} {at.Y + 1}", Stroke: "#9e4537", StrokeWidth: .45, Role: "selected-interior-focus"));
        return new("Interior construction and focus", paths, "Gold: new radial, straight and fan guides. Red: selected construction focus. Muted green: actual mesh. The decoration uses these guides without tracing all of them.", Bounds(d), "#f3ebda");
    }
    private static string Path(Curve.Read read, bool move = true)
    {
        var c = read.Controls;
        return (move ? "M " + P(c[0]) + " " : "") + (c.Length == 2 ? "L " + P(c[1]) : c.Length == 3 ? "Q " + P(c[1]) + " " + P(c[2]) : "C " + P(c[1]) + " " + P(c[2]) + " " + P(c[3]));
    }
    private static string Fill(Band.SweepRead s)
    {
        var reverse = s.SideB.Controls.Reverse().ToArray();
        return Path(s.SideA) + " L " + P(reverse[0]) + " " + (reverse.Length == 2 ? "L " + P(reverse[1]) : reverse.Length == 3 ? "Q " + P(reverse[1]) + " " + P(reverse[2]) : "C " + P(reverse[1]) + " " + P(reverse[2]) + " " + P(reverse[3])) + " Z";
    }
    private static string P(Curve.Vector p) => p.X.ToString(CultureInfo.InvariantCulture) + " " + p.Y.ToString(CultureInfo.InvariantCulture);
    private static void Save(string path, Relation root)
    {
        var json = GojoStructureCompactJson.Serialize(GojoStructureCompactJson.FromElement("ornament-interior-v1", root));
        using var file = File.Create(path); using var gzip = new GZipStream(file, CompressionLevel.SmallestSize); using var writer = new StreamWriter(gzip); writer.Write(json);
    }
}
