using System.Text.Json;
using Gojo.Core;
using Gojo.Patterns;

namespace Gojo.Experiments.QualitativeFrieze.Composition;

/// <summary>Local report orchestration; domain meaning stays in the native
/// programs and observations retained by each study.</summary>
public static class ConstraintFamiliesStudy
{
    public sealed record Report(ConstructionRangeStudy.Report Ranges,
        SymmetryCompositionStudy.Report Symmetry, ShapeNeighborhoodStudy.Report Neighborhood);

    public static int RunExperiment(string[] args)
    {
        var output = Path.Combine("experiments", "QualitativeFrieze", "results", "constraint-families");
        for (var i = 0; i < args.Length; i++)
            if (args[i] == "--output" && i + 1 < args.Length) output = args[++i];
            else throw new ArgumentException("Usage: --constraint-families [--output DIRECTORY]");
        Directory.CreateDirectory(output);
        var report = new Report(ConstructionRangeStudy.Explore(), SymmetryCompositionStudy.Explore(), ShapeNeighborhoodStudy.Explore());
        var json = JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(Path.Combine(output, "results.json"), json);
        Save("ranges.gojo", StructuralConstraint.Closed(report.Ranges.Frame.Surface, report.Ranges.Witness));
        Save("symmetry.gojo", StructuralConstraint.Closed(
            StructuralConstraint.Closed(report.Symmetry.Samples.Select(s => (IElement)s.Surface).ToArray()),
            StructuralConstraint.Closed(report.Symmetry.Comparisons.Select(c => (IElement)c.Surface).ToArray())));
        Save("neighborhood.gojo", StructuralConstraint.Closed(report.Neighborhood.Shapes.Select(s => (IElement)s.NativeSurface).ToArray()));
        ConstraintFamiliesGallery.Write(output, json);
        Console.WriteLine($"{report.Ranges.Samples.Length} offers; {report.Ranges.Samples.Count(s => s.Admissible)} admitted; " +
            $"{report.Ranges.Comparisons.Length} range comparisons; {report.Symmetry.Samples.Length} symmetry schedules; " +
            $"{report.Neighborhood.Shapes.Length} neighboring constructions.");
        Console.WriteLine(Path.GetFullPath(Path.Combine(output, "index.html")));
        return 0;

        void Save(string name, Relation root)
        {
            var bytes = GojoNumericFormat.Encode(root);
            if (GojoNumericFormat.Decode(bytes) is not [Relation]) throw new InvalidOperationException("Native archive did not restore.");
            File.WriteAllBytes(Path.Combine(output, name), bytes);
            Console.WriteLine($"{name}: {bytes.Length:N0} bytes (programs and retained audit witnesses, not only active state).");
        }
    }
}
