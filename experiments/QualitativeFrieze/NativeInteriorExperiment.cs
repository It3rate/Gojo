using System.Diagnostics;
using System.IO.Compression;
using System.Text.Json;
using Gojo.Core;
using Gojo.Encoding.Structures;
using Gojo.Patterns;

namespace Gojo.Experiments.QualitativeFrieze;

public static class NativeInteriorExperiment
{
    public static int Run(string[] args)
    {
        var output = Path.Combine("experiments", "QualitativeFrieze", "results", "native-interiors");
        var faces = 128; var closures = 4; string? selection = null;
        for (var i = 0; i < args.Length; i++)
            if (args[i] == "--output" && i + 1 < args.Length) output = args[++i];
            else if (args[i] == "--faces" && i + 1 < args.Length && int.TryParse(args[++i], out var count) && count is >= 0 and <= 512) faces = count;
            else if (args[i] == "--closures" && i + 1 < args.Length && int.TryParse(args[++i], out var slots) && slots is >= 0 and <= 16) closures = slots;
            else if (args[i] == "--source" && i + 1 < args.Length) selection = args[++i];
            else throw new ArgumentException("Usage: --native-interiors [--output DIRECTORY] [--faces 0..512] [--closures 0..16] [--source I01,I02,...]");
        var chosen = selection?.Split(',').ToHashSet(StringComparer.Ordinal);
        var sources = NativeInteriorSources.Cases().Where(source => chosen is null || chosen.Contains(source.Id)).ToArray();
        if (sources.Length == 0) throw new ArgumentException("No source matched the selected fixture IDs.");
        Directory.CreateDirectory(output); var watch = Stopwatch.StartNew();
        var studies = new List<NativeInteriorGallery.Study>(); var archive = new List<IElement>(); var checks = 0;
        foreach (var source in sources)
        {
            var topology = new NativeInteriorTopology(source, faces);
            var features = new NativeInteriorFeatures(topology);
            var completion = new NativeInteriorCompletion(topology, closures, faces);
            var restored = Restore(topology.Port);
            if (!NativeInteriorTopology.TryReadPort(restored, out var imported, out var witness, faces))
                throw new InvalidOperationException("A public native interior port failed transport.");
            var other = new NativeInteriorFeatures(imported);
            if (!Signatures(features).SequenceEqual(Signatures(other)))
                throw new InvalidOperationException("An interior classification changed after port-only transport.");
            if (StructuralPotential.Items(topology.Source.Graph.Strokes).Length != StructuralPotential.Items(source.Graph.Strokes).Length)
                throw new InvalidOperationException("The published source lost passage occurrences.");
            checks++;
            archive.Add(new Relation(topology.Port, witness));
            archive.AddRange(features.Reads.Take(2).Select(read => (IElement)read.Surface));
            archive.AddRange(completion.Proposals.Take(1).Select(proposal => (IElement)proposal.Surface));
            studies.Add(new(topology, features, completion));
            Console.WriteLine($"{source.Id}: {topology.Boundaries.Count} observed walks; {completion.Proposals.Sum(proposal => proposal.Reads.Count)} conditional; {StructuralPotential.Items(topology.EmbeddingUnknown).Length} unresolved embedding witnesses.");
        }
        NativeInteriorGallery.Write(output, studies);
        File.WriteAllText(Path.Combine(output, "summary.json"), JsonSerializer.Serialize(new {
            Sources = sources.Length, FaceCapacity = faces, ClosureCapacity = closures, PortTransportChecks = checks,
            ObservedBoundaries = studies.Sum(study => study.Topology.Boundaries.Count),
            ConditionalBoundaries = studies.Sum(study => study.Completion.Proposals.Sum(proposal => proposal.Reads.Count)),
            SourcesWithEmbeddingUnknown = studies.Count(study => StructuralPotential.Items(study.Topology.EmbeddingUnknown).Length != 0),
            SourcesWithCuts = studies.Count(study => StructuralPotential.Items(study.Topology.Unsearched).Length != 0),
            Results = studies.Select(study => new { study.Topology.Source.Id, study.Topology.Source.Name,
                SourceStrokes = StructuralPotential.Items(study.Topology.Source.Graph.Strokes).Length,
                Edges = study.Topology.Edges.Count, Boundaries = study.Topology.Boundaries.Count,
                Conditional = study.Completion.Proposals.Sum(proposal => proposal.Reads.Count),
                Features = study.Features.Reads.SelectMany(read => read.Claims).GroupBy(claim => claim.Name)
                    .ToDictionary(group => group.Key, group => group.GroupBy(claim => claim.Status)
                        .ToDictionary(status => status.Key, status => status.Count())) }).ToArray(),
            SecondsBeforeArchive = watch.Elapsed.TotalSeconds }, new JsonSerializerOptions { WriteIndented = true }));
        var packet = GojoStructureCompactJson.Serialize(GojoStructureCompactJson.FromElement("native-interior-ports-and-representative-witnesses-v1",
            StructuralConstraint.Closed(archive.ToArray())));
        using (var file = File.Create(Path.Combine(output, "evidence.gojo.json.gz")))
        using (var zip = new GZipStream(file, CompressionLevel.SmallestSize))
        using (var writer = new StreamWriter(zip)) writer.Write(packet);
        Console.WriteLine($"Completed {sources.Length} sources and {checks} public-port replay comparisons in {watch.Elapsed.TotalSeconds:0.00}s. {Path.GetFullPath(output)}");
        return 0;
    }

    private static string[] Signatures(NativeInteriorFeatures features) => features.Reads.Select(read =>
        read.SideCount + ":" + string.Join('|', read.Claims.Select(claim => claim.Name + "=" + claim.Status)))
        .Order(StringComparer.Ordinal).ToArray();

    private static Relation Restore(Relation source)
    {
        var packet = GojoStructureCompactJson.FromElement("public-interior-source", source);
        if (!GojoStructureCompactJson.TryToElement(GojoStructureCompactJson.Deserialize(GojoStructureCompactJson.Serialize(packet)), out var world) ||
            world.Root is not Relation restored) throw new InvalidOperationException("A native source failed transport.");
        return restored;
    }
}
