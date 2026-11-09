using System.Text.Json;
using Gojo.Core;
using Gojo.Patterns;

namespace Gojo.Experiments.QualitativeFrieze.Composition;

/// <summary>Report orchestration. Fixtures supply structure; the native readers
/// and transport, not glyph names or SVG output, establish the recorded reads.</summary>
public static class GlyphJointAtlasStudy
{
    public sealed record Report(GlyphConstructionCorpus.Fixture[] Corpus,
        GlyphJointUsageReport Usage, GlyphContextStudy.Report Contexts);

    public static int RunExperiment(string[] args)
    {
        var output = Path.Combine("experiments", "QualitativeFrieze", "results", "glyph-joints");
        for (var i = 0; i < args.Length; i++)
            if (args[i] == "--output" && i + 1 < args.Length) output = args[++i];
            else throw new ArgumentException("Usage: --glyph-joints [--output DIRECTORY]");
        var report = new Report(GlyphConstructionCorpus.Create().ToArray(), GlyphJointUsageStudy.Run(), GlyphContextStudy.Run());
        if (report.Corpus.Any(c => !c.Topology.Complete) ||
            report.Usage.Cases.SelectMany(c => c.Probes).Any(p => !p.OwnershipPreserved))
            throw new InvalidOperationException("Fixture construction or material accounting failed; report not emitted.");
        Directory.CreateDirectory(output);
        var json = JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(Path.Combine(output, "results.json"), json);
        Save("corpus.gojo", StructuralConstraint.Closed(report.Corpus.Select(c => (IElement)c.Surface).ToArray()));
        Save("usage.gojo", report.Usage.ArchiveRoot);
        Save("contexts.gojo", report.Contexts.ArchiveRoot);
        GlyphJointAtlasGallery.Write(output, json);
        Console.WriteLine($"{report.Corpus.Select(c => c.Glyph).Distinct().Count()} glyphs; {report.Corpus.Length} constructions; " +
            $"{report.Usage.Cases.Length} joint-use machines; {report.Usage.Cases.Sum(c => c.Probes.Length)} route probes; " +
            $"{report.Contexts.Cases.Length} selected contact contexts.");
        Console.WriteLine(Path.GetFullPath(Path.Combine(output, "index.html")));
        return 0;

        void Save(string name, Relation root)
        {
            var bytes = GojoNumericFormat.Encode(root);
            if (GojoNumericFormat.Decode(bytes) is not [Relation]) throw new InvalidOperationException("Native archive failed to restore.");
            File.WriteAllBytes(Path.Combine(output, name), bytes);
            Console.WriteLine($"{name}: {bytes.Length:N0} bytes (apparatus and retained evidence, not only active state).");
        }
    }
}
