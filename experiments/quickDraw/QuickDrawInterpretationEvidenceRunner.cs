using System.Text;
using System.Text.Json;
using Gojo.Core;
using Gojo.Encoding.Structures;

namespace Gojo.Experiments.QuickDraw;

public static class QuickDrawInterpretationEvidenceRunner
{
    public static int Run(string[] args)
    {
        if (args.Length != 5 || args[0] != "--interpret-evidence" || args[1] != "--input" || args[3] != "--output")
        { Console.Error.WriteLine("Usage: --interpret-evidence --input <packets.ndjson> --output <directory>"); return 2; }
        var destination = Path.GetFullPath(args[4]); Directory.CreateDirectory(destination);
        using var reader = File.OpenText(args[2]);
        var summaries = new List<object>(); var ids = new HashSet<string>(StringComparer.Ordinal); string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            using var document = JsonDocument.Parse(line); var packet = document.RootElement;
            var id = packet.GetProperty("program").GetProperty("id").GetString()!;
            if (string.IsNullOrEmpty(id) || !ids.Add(id)) throw new InvalidDataException("Interpretation programs need unique IDs.");
            var surface = QuickDrawInterpretationEvidence.Import(packet);
            if (!QuickDrawInterpretationEvidence.TryRead(surface, out var initial)) throw new InvalidDataException("Source correspondence is unreadable.");
            var archive = GojoStructureCompactJson.FromElement("quickdraw-observed-construction", surface);
            var json = GojoStructureCompactJson.Serialize(archive);
            if (!GojoStructureCompactJson.TryToElement(GojoStructureCompactJson.Deserialize(json), out var restored) ||
                restored.Root is not Relation root || !QuickDrawInterpretationEvidence.TryRead(root, out var replay))
                throw new InvalidDataException("Restored source correspondence did not validate.");
            static object Sample(NativeAcquisition.ReadSample sample) => (sample.StrokeIndex, sample.SampleIndex,
                sample.XNumerator, sample.XDenominator, sample.YNumerator, sample.YDenominator, sample.TimeNumerator, sample.TimeDenominator);
            if (!initial.Acquisition.Samples.Select(Sample).SequenceEqual(replay.Acquisition.Samples.Select(Sample)) ||
                !initial.Construction.Sites.Select(site => site.Position).SequenceEqual(replay.Construction.Sites.Select(site => site.Position)) ||
                !initial.Spans.Select(span => (span.Stroke, span.Start, span.End, span.Edge.Index))
                    .SequenceEqual(replay.Spans.Select(span => (span.Stroke, span.Start, span.End, span.Edge.Index))))
                throw new InvalidDataException("Restored correspondence changed exact acquisition or proposal geometry.");
            var filename = $"{summaries.Count + 1:D3}.interpretation.gojo.json";
            File.WriteAllText(Path.Combine(destination, filename), json, new UTF8Encoding(false));
            summaries.Add(new { id, archive = filename, nodes = archive.Counts.Nodes,
                sourceId = packet.GetProperty("program").GetProperty("sourceMapping").GetProperty("sourceId").GetString(),
                sourceStrokes = replay.Acquisition.StrokeCount, sourceSamples = replay.Acquisition.Samples.Count,
                matchedIntervals = replay.Spans.Count, constructionRungs = replay.Construction.Rungs.Length,
                nativeRestoreVerified = true, actualSampleAliasesVerified = true, acquiredOrderCoverageVerified = true,
                targetEdgeAliasesVerified = true, penLiftIdentityVerified = true,
                exactSourceTextUsed = packet.TryGetProperty("sourceOriginalJson", out _),
                residue = "Entire source intervals remain visible as unaccounted measured evidence; no exact geometric fit or intended shape is certified.",
                calibration = "Actual source extrema and source basis correspond to actual target envelope sites and target basis; selected measured approximation, not equality.",
                materialTransfer = false, inferredJoins = 0, nativeCandidateClassifier = false });
        }
        File.WriteAllText(Path.Combine(destination, "summary.json"), JsonSerializer.Serialize(summaries,
            new JsonSerializerOptions { WriteIndented = true }), new UTF8Encoding(false));
        Console.WriteLine($"Restored and validated {summaries.Count} source-to-construction evidence archives into {destination}"); return 0;
    }
}
