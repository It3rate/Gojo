using System.Text;
using System.Text.Json;
using Gojo.Core;
using Gojo.Encoding.Structures;

namespace Gojo.Experiments.QuickDraw;

public static class QuickDrawGestaltEvidenceRunner
{
    public static int Run(string[] args)
    {
        if (args.Length != 5 || args[0] != "--gestalt-evidence" || args[1] != "--input" || args[3] != "--output")
        { Console.Error.WriteLine("Usage: --gestalt-evidence --input <packets.ndjson> --output <directory>"); return 2; }
        var destination = Path.GetFullPath(args[4]); Directory.CreateDirectory(destination);
        using var reader = File.OpenText(args[2]); var summaries = new List<object>(); var ids = new HashSet<string>(StringComparer.Ordinal); string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            using var document = JsonDocument.Parse(line); var packet = document.RootElement;
            var id = packet.GetProperty("program").GetProperty("id").GetString()!;
            if (string.IsNullOrEmpty(id) || !ids.Add(id)) throw new InvalidDataException("Simplification program IDs must be unique.");
            var surface = QuickDrawGestaltEvidence.Import(packet);
            if (!QuickDrawGestaltEvidence.TryRead(surface, out var initial)) throw new InvalidDataException("Source/pass graph is unreadable.");
            var archive = GojoStructureCompactJson.FromElement("quickdraw-gestalt-pass-evidence", surface);
            var json = GojoStructureCompactJson.Serialize(archive);
            if (!GojoStructureCompactJson.TryToElement(GojoStructureCompactJson.Deserialize(json), out var restored) || restored.Root is not Relation root ||
                !QuickDrawGestaltEvidence.TryRead(root, out var replay)) throw new InvalidDataException("Restored source/pass graph did not validate.");
            static object Sample(NativeAcquisition.ReadSample sample) => (sample.StrokeIndex, sample.SampleIndex,
                sample.XNumerator, sample.XDenominator, sample.YNumerator, sample.YDenominator, sample.TimeNumerator, sample.TimeDenominator);
            static string Signature(QuickDrawGestaltEvidence.Read read) => JsonSerializer.Serialize(new {
                intervals = read.Intervals.Select(i => new { i.Stroke, i.Start, i.End }),
                passes = read.Passes.Select(p => new { p.Rung,
                    links = p.Links.Select(l => new { source = l.Source.Select(i => new { i.Stroke, i.Start, i.End }), target = l.Target.Select(e => e.Index) }),
                    unmatched = p.Unmatched.Select(i => new { i.Stroke, i.Start, i.End }), inferred = p.Inferred.Select(e => e.Index), residue = p.Residue.Select(e => e.Index) }) });
            if (!initial.Acquisition.Samples.Select(Sample).SequenceEqual(replay.Acquisition.Samples.Select(Sample)) || Signature(initial) != Signature(replay) ||
                !initial.Construction.Sites.Select(s => s.Position).SequenceEqual(replay.Construction.Sites.Select(s => s.Position)))
                throw new InvalidDataException("Transport changed exact source, many-to-many accounting or constructed geometry.");
            var standalone = GojoStructureCompactJson.Serialize(GojoStructureCompactJson.FromElement("standalone-construction", replay.Program));
            if (!GojoStructureCompactJson.TryToElement(GojoStructureCompactJson.Deserialize(standalone), out var proposal) || proposal.Root is not Relation program ||
                !ConstructionLadder.TryExecute(program, out var executed) ||
                !executed.Sites.Select(s => s.Position).SequenceEqual(replay.Construction.Sites.Select(s => s.Position)))
                throw new InvalidDataException("Construction requires hidden source/proposal metadata.");
            var filename = $"{summaries.Count + 1:D3}.gestalt.gojo.json";
            File.WriteAllText(Path.Combine(destination, filename), json, new UTF8Encoding(false));
            summaries.Add(new { id, archive = filename, nodes = archive.Counts.Nodes,
                sourceId = packet.GetProperty("observation").GetProperty("sourceId").GetString(), sourceStrokes = replay.Acquisition.StrokeCount,
                sourceSamples = replay.Acquisition.Samples.Count, sourceIntervals = replay.Intervals.Length,
                passes = replay.Passes.Select(p => new { rung = p.Rung, links = p.Links.Length,
                    visible = replay.Construction.Rungs[p.Rung].Visible.Length, activeResidueCurves = p.Residue.Length,
                    unmatchedIntervals = p.Unmatched.Length, inferredCurves = p.Inferred.Length,
                    manySourceLinks = p.Links.Count(l => l.Source.Length > 1), manyTargetLinks = p.Links.Count(l => l.Target.Length > 1) }),
                nativeRestoreVerified = true, actualSourceAndTargetAliasesVerified = true, completeSourcePartitionVerified = true,
                standaloneConstructionReplayVerified = true, priorControlDependenciesVerified = true, acquiredIdentityUnwelded = true,
                materialTransfer = false, exactGeometricFitClaimed = false,
                chronology = "Construction passes are authored interpretation history; original acquired stroke/sample/time order remains separate and unchanged.",
                residue = "Every selected source interval remains unaccounted read residue; unmatched intervals and active residue curves are explicit. Historical guides do not count as current source coverage.",
                qualification = "Measured approximation and candidate acceptance are external experimental reads. The native graph verifies correspondence, causality and retained accounting, not intended shape or a learned Gestalt law." });
        }
        File.WriteAllText(Path.Combine(destination, "summary.json"), JsonSerializer.Serialize(summaries, new JsonSerializerOptions { WriteIndented = true }), new UTF8Encoding(false));
        Console.WriteLine($"Restored and validated {summaries.Count} source-to-simplification archives into {destination}"); return 0;
    }
}
