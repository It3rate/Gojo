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
using Corpus = Gojo.Experiments.QualitativeFrieze.Composition.OrnamentIntegratedCorpus;
using Source = Gojo.Experiments.QualitativeFrieze.Composition.OrnamentSubstrateCorpus;
using MeshStudy = Gojo.Experiments.QualitativeFrieze.Composition.OrnamentSubstrateExperiment;
using Study = Gojo.Experiments.QualitativeFrieze.Composition.OrnamentCompositionExperiment;
using Attach = Gojo.Experiments.QualitativeFrieze.Composition.OrnamentMotifAttachment;
using Art = Gojo.Experiments.QualitativeFrieze.Composition.BandEvidenceGallery;

namespace Gojo.Experiments.QualitativeFrieze.Composition;

public static class OrnamentIntegratedExperiment
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { WriteIndented = true };
    public static int Run(string[] args)
    {
        var output = Path.Combine("experiments", "QualitativeFrieze", "results", "ornament-integrated"); var limit = int.MaxValue; string? caseId = null;
        for (var i = 0; i < args.Length; i++)
            if (args[i] == "--output" && i + 1 < args.Length) output = args[++i];
            else if (args[i] == "--limit" && i + 1 < args.Length) limit = int.Parse(args[++i], CultureInfo.InvariantCulture);
            else if (args[i] == "--case" && i + 1 < args.Length) caseId = args[++i];
            else throw new ArgumentException("Usage: --ornament-integrated [--output DIRECTORY] [--limit COUNT] [--case I012]");
        Directory.CreateDirectory(output); Directory.CreateDirectory(Path.Combine(output, "native"));
        var watch = Stopwatch.StartNew(); var examples = new List<OrnamentIntegratedGallery.Example>(); var results = new List<object>();
        var samples = Corpus.Cases(limit, caseId); var total = 0; var forward = 0; var opposed = 0; var shared = 0;
        foreach (var (d, i) in samples.Select((d, i) => (d, i)))
        {
            var f = d.Unit.Template.Frame; var reads = d.Attachments.Select(a => Attach.ReadAttachment(f, a.Surface)).ToArray();
            var native = Parts.ReadTemplate(f, d.Unit.Template.Surface);
            var paletteIndex = int.Parse(d.Id.AsSpan(1), CultureInfo.InvariantCulture) - 1;
            var palette = Study.Palettes[(paletteIndex * 3 + 2) % Study.Palettes.Length];
            var colour = Study.AssessColours(d.Unit, palette, false); var archive = "native/" + d.Id.ToLowerInvariant() + ".gojo.json.gz";
            Save(Path.Combine(output, archive), new Relation(d.Surface, colour.Surface));
            total += reads.Length; forward += reads.Count(a => a.Smooth == true); opposed += reads.Count(a => a.Incident == true && a.Opposed == true); shared += reads.Count(a => a.Incident == true && a.SharedSite);
            var audit = new { SourceId = d.Source.Id, TemplateComplete = native.Complete,
                PreservedMeshSpans = d.Mesh.Template.Envelopes.Count, RemovedOldDecorativeSpans = E(d.Removed).Length,
                NewMotifGroups = d.Motifs.Count, AttachmentQuestions = reads.Length,
                IncidentRoots = reads.Count(a => a.Incident == true), ForwardTangentRoots = reads.Count(a => a.Smooth == true),
                OpposedTangentRoots = reads.Count(a => a.Incident == true && a.Opposed == true),
                CornerRoots = reads.Count(a => a.Incident == true && a.Forward == false && a.Opposed == false),
                SharedEndpointRoots = reads.Count(a => a.Incident == true && a.SharedSite),
                StraightInteriorRoots = reads.Count(a => a.Port.Interior),
                NativeMotifApplications = d.Generations.Count,
                SourceCrossingOrders = d.Source.Ornament.Unit.Crossings.Count, OutputCrossingOrders = d.Unit.Crossings.Count,
                Palette = palette.Name,
                Limits = "Centre-path tangent/incidence checks only; no global collision, side-boundary union, automatic salience or arbitrary curved-interior attachment proof." };
            var evidence = new Art.Evidence[] {
                new("Preserved mesh", $"{d.Mesh.Template.Envelopes.Count} original mesh spans", "Exact source envelope/curve identities, substrate and crossing orders", "The replacement detail layer carries the earlier decorations as a complement; it does not redraw the mesh."),
                new("Rooted motif groups", $"{d.Motifs.Count} larger groups · {reads.Count(a => a.Incident == true)} incident attachments", "Actual exposed mesh positions and generated motif starts", "Leaves and florets originate at endpoint or straight-crossing landmarks. Curve-following bands reuse an actual source curve."),
                new("Flow at roots", $"{reads.Count(a => a.Smooth == true)} forward · {reads.Count(a => a.Incident == true && a.Opposed == true)} opposed tangent", "Native control-displacement directions in one common-reach basis", "Forward and opposed traversal are distinct. Both can give geometric tangency; corner and open reads remain separate. This does not certify whole band union."),
                new("Endpoint identity", $"{reads.Count(a => a.Incident == true && a.SharedSite)} shared endpoint roots", "Actual spine site aliases", "A crossing-landmark attachment is incidence on a selected strand, not a new weld or a join between warp and weft."),
                new("Larger detail", "Paired leaves, boundary-following blades and landmark florets", "Native occupied bands, tapered side profiles and shared radial roots", "The mesh directs disposition, tangent and silhouette. The pass aims for visual integration; the judgement is not a learned aesthetic score.") };
            examples.Add(new(d.Id, d.Name, d.Note, MeshStudy.RenderDesign(d.Source, palette) with { Bounds = Bounds(d) }, Render(d, palette), Connections(d),
                MeshStudy.RenderSubstrate(d.Source) with { Bounds = Bounds(d) }, evidence, audit, archive));
            results.Add(new { d.Id, d.Name, Audit = audit });
            Console.WriteLine($"{d.Id}: {d.Motifs.Count} motif groups; {reads.Length} roots; {reads.Count(a => a.Smooth == true)} forward, {reads.Count(a => a.Opposed == true)} opposed.");
        }
        var summary = new { Designs = samples.Count, RootConnections = total, ForwardTangentRoots = forward, OpposedTangentRoots = opposed,
            SharedEndpointRoots = shared, Seconds = Math.Round(watch.Elapsed.TotalSeconds, 3),
            Scope = "Native mesh-preserving motif refinement, with local attachment evidence and visual review. No aesthetic model training or global non-overlap guarantee.", Results = results };
        File.WriteAllText(Path.Combine(output, "summary.json"), JsonSerializer.Serialize(summary, Json));
        OrnamentIntegratedGallery.Write(output, summary, examples);
        Console.WriteLine($"Integrated ornaments: {samples.Count} designs, {watch.Elapsed.TotalSeconds:0.0}s. {Path.GetFullPath(output)}"); return 0;
    }

    public static Source.Design Canvas(Corpus.Design d) => d.Source with { Ornament = d.Source.Ornament with { Unit = d.Unit } };
    public static Art.ViewBox Bounds(Corpus.Design d) => MeshStudy.Bounds(Canvas(d));
    public static Art.Panel Render(Corpus.Design d, Study.Palette palette) => MeshStudy.RenderDesign(Canvas(d), palette) with {
        Name = "Integrated mesh ornament", Note = "Larger tapered leaves, curve-following blades and florets consume mesh anchors. The original continuous strands and crossing order remain available." };
    public static Art.Panel Connections(Corpus.Design d)
    {
        var f = d.Unit.Template.Frame; var paths = new List<Art.InkPath>();
        foreach (var e in d.Mesh.Template.Envelopes)
            paths.Add(new(CurvePath(Band.ReadEnvelope(f, e).Sweep.Spine), Stroke: "#355d67", StrokeWidth: .45, Role: "preserved-mesh", Opacity: .6));
        foreach (var group in d.Motifs) foreach (var e in E(group).Cast<Relation>())
            paths.Add(new(CurvePath(Band.ReadEnvelope(f, e).Sweep.Spine), Stroke: "#a85537", StrokeWidth: .45, Role: "rooted-motif-flow"));
        foreach (var a in d.Attachments.Select(a => Attach.ReadAttachment(f, a.Surface)))
            if (a.Port.Complete && f.Vector(a.Port.Position, out var p))
            {
                paths.Add(Dot(p, .8, a.Port.Interior ? "#9b7436" : "#2d7666", a.Port.Interior ? "interior-incidence-root" : "endpoint-root"));
                if (f.Vector(a.Port.Tangent, out var t) && !t.Zero)
                {
                    var length = Math.Sqrt((double)(t.X * t.X + t.Y * t.Y));
                    // Normalisation here only draws a finite direction glyph.
                    // Native tangent comparison retains exact word ratios.
                    var x = (double)p.X + (double)t.X / length * 3; var y = (double)p.Y + (double)t.Y / length * 3;
                    paths.Add(new("M " + P(p) + " L " + F(x) + " " + F(y), Stroke: "#4c8b77", StrokeWidth: .24, Role: "source-tangent-glyph"));
                }
            }
        return new("Mesh roots and motif flow", paths,
            "Blue: original mesh; copper: new motif spines; green dots: endpoint roots; amber dots: straight interior incidence. Direction glyphs show the exposed source flow, not a side-boundary union claim.", Bounds(d), "#faf5e7");
    }
    private static string CurvePath(Curve.Read read)
    {
        var c = read.Controls; return "M " + P(c[0]) + " " + (c.Length == 2 ? "L " + P(c[1]) : c.Length == 3 ? "Q " + P(c[1]) + " " + P(c[2]) : "C " + P(c[1]) + " " + P(c[2]) + " " + P(c[3]));
    }
    private static Art.InkPath Dot(Curve.Vector p, double r, string colour, string role) => new(
        $"M {F((double)p.X - r)} {p.Y} a {F(r)} {F(r)} 0 1 0 {F(2 * r)} 0 a {F(r)} {F(r)} 0 1 0 {F(-2 * r)} 0 Z", Fill: colour, Stroke: "none", StrokeWidth: 0, Role: role);
    private static string F(double n) => n.ToString("0.########", CultureInfo.InvariantCulture);
    private static string P(Curve.Vector p) => p.X.ToString(CultureInfo.InvariantCulture) + " " + p.Y.ToString(CultureInfo.InvariantCulture);
    private static void Save(string path, Relation root)
    {
        var json = GojoStructureCompactJson.Serialize(GojoStructureCompactJson.FromElement("ornament-integrated-v1", root));
        using var file = File.Create(path); using var gzip = new GZipStream(file, CompressionLevel.SmallestSize); using var writer = new StreamWriter(gzip); writer.Write(json);
    }
}
