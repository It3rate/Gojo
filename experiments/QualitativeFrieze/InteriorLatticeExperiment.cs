using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Gojo.Core;
using Gojo.Encoding.Structures;
using Gojo.Experiments.QualitativeExecution;
using N = Gojo.Experiments.QualitativeFrieze.FieldNative;

namespace Gojo.Experiments.QualitativeFrieze;

/// <summary>Finite occupied-cell experiment. Native atoms, ownership claims,
/// shared boundaries and their witnesses supply the pictures; images never
/// supply the cell partition. Human preference remains a separate question.</summary>
public static partial class InteriorLatticeExperiment
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { WriteIndented = true };
    private sealed record CellPlate(int Index, int Atoms, int Sides, int ReflexTurns, double Area,
        double BoxOccupancy, double ShortRunFraction, bool BoundaryDetailWarning, object? Shape);
    private sealed record StagePlate(string Name, string Description, string Lines, string Fill, string Removed,
        bool Partition, bool Embedded, InteriorPartitionSummary Read, int Blocked, int Open, CellPlate[] Cells);
    private sealed record Plate(string Id, string Name, string Family, string Description,
        string Fingerprint, string PacketSha256, int Numeric, int NativeObjects, int PeriodicOccurrences,
        int SharedInteriorEdges, int RemovedEdges, InteriorPeriodicitySummary PeriodRead, StagePlate[] Stages);

    public static int Run(string[] args)
    {
        var output = Path.Combine("experiments", "QualitativeFrieze", "results", "lattice-interiors");
        var limit = 100;
        for (var i = 0; i < args.Length; i++)
            if (args[i] == "--output" && i + 1 < args.Length) output = args[++i];
            else if (args[i] == "--limit" && i + 1 < args.Length && int.TryParse(args[++i], out var size) && size is >= 1 and <= 100) limit = size;
            else throw new ArgumentException("Usage: --lattice-interiors [--output DIRECTORY] [--limit 1..100]");
        Directory.CreateDirectory(output);
        var study = new InteriorLatticeRecipes();
        var plates = new List<Plate>();
        foreach (var item in study.Cases.Take(limit))
        {
            var root = Packet(item);
            var archive = GojoStructureCompactJson.Serialize(GojoStructureCompactJson.FromElement("lattice-interiors-v1", root));
            Native(output, item.Id + ".gojo.json.gz", archive);
            var restored = Import(archive);
            var actualCurrent = restored.Stages[^1];
            var repeat = InteriorPeriodicityRead.Read(actualCurrent.Complex, actualCurrent.Groups, actualCurrent.Bindings,
                restored.Frame, restored.Reach, restored.Periodicity);
            if (!repeat.IsPeriodic) throw new InvalidOperationException(item.Id + ": actual translated face correspondences must establish both finite period directions.");
            var stagePlates = new List<StagePlate>();
            InteriorCellPartition? final = null;
            for (var i = 0; i < restored.Stages.Length; i++)
            {
                var stage = restored.Stages[i]; var original = item.Stages[i];
                var read = InteriorCellPartition.Read(stage.Complex, stage.Groups, stage.Bindings, restored.Frame.Surface, restored.Reach);
                var picture = Picture(read, stage.Complex, stage.Groups, stage.Bindings, restored.Frame, item.DisplayBasis);
                var prefix = item.Id + "-s" + i;
                Save(output, prefix + "-lines.svg", Svg(item.Name, picture, "lines"));
                Save(output, prefix + "-cells.svg", Svg(item.Name, picture, "cells"));
                Save(output, prefix + "-removed.svg", Svg(item.Name, picture, "removed"));
                stagePlates.Add(new(original.Name, original.Description, prefix + "-lines.svg", prefix + "-cells.svg",
                    prefix + "-removed.svg", read.IsPartition, read.IsEmbeddedPartition, read.Summary,
                    N.E(read.Blocked).Length + N.E(read.EmbeddingBlocked).Length,
                    N.E(read.Open).Length + N.E(read.EmbeddingOpen).Length, picture.Facts));
                final = read;
            }
            if (final is null || !final.IsEmbeddedPartition)
                throw new InvalidOperationException(item.Id + ": the imported final claims must form an embedded exclusive partition.");
            var current = restored.Stages[^1];
            var last = Picture(final, current.Complex, current.Groups, current.Bindings, restored.Frame, item.DisplayBasis);
            Save(output, item.Id + ".svg", Svg(item.Name, last, "lines"));
            Save(output, item.Id + "-cells.svg", Svg(item.Name, last, "cells"));
            var numeric = Reachable(root).Count(e => e is Carrier or CarrierLocation);
            if (numeric != 0) throw new InvalidOperationException("Only relation/placeholder source participants are permitted.");
            if (plates.Count % 20 == 0)
                Native(output, item.Id + "-rules.gojo.json.gz", GojoStructureCompactJson.Serialize(
                    GojoStructureCompactJson.FromElement("lattice-interior-rules-v1", new Relation(final.Surface, repeat.Surface))));
            var owners = final.GroupReads.SelectMany(group => N.E(group.Boundary).Cast<Relation>())
                .GroupBy(dart => dart.Left, N.Participants.Instance).ToArray();
            plates.Add(new(item.Id, item.Name, item.Family, item.Description, Fingerprint(last),
                Hash(System.Text.Encoding.UTF8.GetBytes(archive)), numeric, Reachable(root).Count(), N.E(restored.Periodicity.Right as Relation ?? N.S()).Length,
                owners.Count(group => group.Count() == 2), last.Removed.Length, repeat.Summary, stagePlates.ToArray()));
            Console.WriteLine($"{item.Id}: {item.Family}; {final.Summary.AcceptedGroups} exclusive cells, {last.Removed.Length} internal edges removed.");
        }
        var counterexamples = Counterexamples(study.Cases[0], output);
        var distinct = plates.Select(p => p.Fingerprint).Distinct().Count();
        var diagnostics = new
        {
            Schema = "gojo-lattice-interiors-v1", Cases = plates, Admitted = plates.Count,
            DistinctFiniteInteriorReads = distinct,
            DuplicateGroups = plates.GroupBy(p => p.Fingerprint).Where(g => g.Count() > 1).Select(g => g.Select(p => p.Id).ToArray()),
            Counterexamples = counterexamples,
            Limits = new[] {
                "Exclusive atomic occupancy and convex-atom embedding are separate checked premises, not arbitrary polygon boolean solving.",
                "The finite field records complete translated tiles and its frontier; this is not recognition from pixels or an unbounded tiling theorem.",
                "Shape-turn, proportion and boundary-detail diagnostics are authored preferences, not validated universal beauty laws.",
                "Repeated atomic reach counts choose a construction resolution; no numeric carriers or measured lengths enter the source.",
                "A native losing claim is blocked proposal evidence; observational conflict does not automatically become physical carrier tension."
            }
        };
        Save(output, "diagnostics.json", JsonSerializer.Serialize(diagnostics, new JsonSerializerOptions(Json) { WriteIndented = false }));
        Save(output, "gallery.html", Gallery(plates));
        Save(output, "overview.html", Overview(plates));
        Save(output, "overview.svg", OverviewSvg(plates, output));
        Save(output, "construction-strips.html", ConstructionStrips(plates));
        Save(output, "rules.html", Rules(counterexamples));
        Console.WriteLine($"Saved {plates.Count} embedded partitions; {distinct} distinct finite interior reads; {Path.GetFullPath(output)}");
        return plates.Count == limit && distinct == limit ? 0 : 1;
    }

    private static Relation Packet(InteriorLatticeCase item) => new(new Relation(item.Frame.Surface, item.Reach),
        new Relation(new Relation(item.Column, item.Row), new Relation(item.Periodicity, N.S(item.Stages.Select(stage =>
            (IElement)new Relation(stage.Complex, new Relation(stage.Groups, new Relation(stage.Bindings, stage.Transition)))).ToArray()))));
    private sealed record ImportedStage(Relation Complex, Relation Groups, Relation Bindings, IElement Transition);
    private sealed record Imported(QualitativeMotionFrame Frame, IElement Reach, Relation Periodicity, ImportedStage[] Stages);
    private static Imported Import(string archive)
    {
        var document = GojoStructureCompactJson.Deserialize(archive);
        if (document.Carriers.Count != 0 || document.Nodes.Any(n => n.Kind is not ("relation" or "placeholder")))
            throw new InvalidOperationException("The occupied-cell archive contains a foreign participant kind.");
        var captured = GojoStructureCompactJson.ToElement(document).Root;
        if (captured is not Relation { Left: Relation { Left: Relation frame } } root ||
            !QualitativeMotionFrameRead.TryReadStandard(frame, out var frameRead) ||
            !frameRead.TryRebind(root, out var rebound, out _) || rebound is not Relation
            { Left: Relation context, Right: Relation { Right: Relation { Left: Relation periodicity, Right: Relation stageScope } } })
            throw new InvalidOperationException("An exact frame, period and stage packet must survive transport.");
        var stages = N.E(stageScope).Cast<Relation>().Select(stage => stage is
            { Left: Relation complex, Right: Relation { Left: Relation groups, Right: Relation { Left: Relation bindings, Right: var transition } } }
            ? new ImportedStage(complex, groups, bindings, transition)
            : throw new InvalidOperationException("An imported stage must retain its exact native source surfaces.")).ToArray();
        return new(frameRead.Target, context.Right, periodicity, stages);
    }
    private static IEnumerable<IElement> Reachable(IElement root)
    {
        var seen = new HashSet<IElement>(N.Participants.Instance); var todo = new Stack<IElement>(); todo.Push(root);
        while (todo.TryPop(out var element))
        { if (!seen.Add(element)) continue; yield return element; if (element is Relation row) { todo.Push(row.Left); todo.Push(row.Right); } }
    }
    private static string Hash(byte[] bytes) => Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
    private static void Save(string output, string file, string text)
    { var path = Path.Combine(output, file); File.WriteAllText(path + ".pending", text); File.Move(path + ".pending", path, true); }
    private static void Native(string output, string file, string json)
    {
        var path = Path.Combine(output, file);
        using (var stream = File.Create(path + ".pending")) using (var zip = new GZipStream(stream, CompressionLevel.SmallestSize))
        using (var writer = new StreamWriter(zip)) writer.Write(json);
        File.Move(path + ".pending", path, true);
    }
}
