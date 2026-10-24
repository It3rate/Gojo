using System.Text;
using System.Text.Json;
using Gojo.Core;
using Gojo.Encoding.Structures;

namespace Gojo.Experiments.QuickDraw;

public static class QuickDrawAnnotationEvidenceRunner
{
    public static int Run(string[] args)
    {
        if (args.Length != 5 || args[0] != "--interpret-annotations" || args[1] != "--input" || args[3] != "--output")
        { Console.Error.WriteLine("Usage: --interpret-annotations --input <annotation-packets.ndjson> --output <directory>"); return 2; }
        var destination = Path.GetFullPath(args[4]); Directory.CreateDirectory(destination);
        using var reader = File.OpenText(args[2]);
        var summaries = new List<object>(); var ids = new HashSet<string>(StringComparer.Ordinal); string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            using var document = JsonDocument.Parse(line); var packet = document.RootElement;
            var id = packet.GetProperty("program").GetProperty("id").GetString()!;
            if (string.IsNullOrEmpty(id) || !ids.Add(id)) throw new InvalidDataException("Annotation programs need unique IDs.");
            var surface = QuickDrawAnnotationEvidence.Import(packet);
            if (!QuickDrawAnnotationEvidence.TryRead(surface, out var initial)) throw new InvalidDataException("Finite source annotation is unreadable.");
            var archive = GojoStructureCompactJson.FromElement("quickdraw-source-annotations", surface);
            var json = GojoStructureCompactJson.Serialize(archive);
            if (!GojoStructureCompactJson.TryToElement(GojoStructureCompactJson.Deserialize(json), out var restored) ||
                restored.Root is not Relation root || !QuickDrawAnnotationEvidence.TryRead(root, out var replay))
                throw new InvalidDataException("Restored finite source annotation did not validate.");
            static object Sample(NativeAcquisition.ReadSample sample) => (sample.StrokeIndex, sample.SampleIndex,
                sample.XNumerator, sample.XDenominator, sample.YNumerator, sample.YDenominator, sample.TimeNumerator, sample.TimeDenominator);
            static string Signature(QuickDrawAnnotationEvidence.Read read) => JsonSerializer.Serialize(new {
                profiles = read.Profiles.Select(p => p.Curve.Controls),
                lines = read.Lines.Select(l => new { l.Profile.Index, l.Orientation, l.Straight, l.Bend }),
                joints = read.Joints.Select(j => new { j.Kind, j.ActualSharedSite, j.Gap, j.OpposedPairs, j.PerpendicularPairs,
                    rays = j.Rays.Select(r => new { r.Profile.Index, r.At, r.Toward, r.Position, r.Direction }),
                    participants = j.Participants.Select(p => new { p.Profile.Index, p.At, p.Position }) }),
                symmetry = read.Symmetries.Select(s => new { s.Pairs, s.WithinTolerance, s.Unmatched, s.MaximumResidual }), read.Relations });
            if (!initial.Interpretation.Acquisition.Samples.Select(Sample).SequenceEqual(replay.Interpretation.Acquisition.Samples.Select(Sample)) ||
                !initial.Interpretation.Construction.Sites.Select(site => site.Position).SequenceEqual(replay.Interpretation.Construction.Sites.Select(site => site.Position)) ||
                Signature(initial) != Signature(replay))
                throw new InvalidDataException("Restored annotations changed acquisition, proposal geometry or finite reads.");
            var programArchive = GojoStructureCompactJson.Serialize(GojoStructureCompactJson.FromElement("proposal", replay.Interpretation.Program));
            if (!GojoStructureCompactJson.TryToElement(GojoStructureCompactJson.Deserialize(programArchive), out var programOnly) ||
                programOnly.Root is not Relation program || !ConstructionLadder.TryExecute(program, out var execution) ||
                !execution.Sites.Select(site => site.Position).SequenceEqual(replay.Interpretation.Construction.Sites.Select(site => site.Position)))
                throw new InvalidDataException("Proposal geometry depends on annotation metadata.");
            var filename = $"{summaries.Count + 1:D3}.annotations.gojo.json";
            File.WriteAllText(Path.Combine(destination, filename), json, new UTF8Encoding(false));
            summaries.Add(new { id, archive = filename, nodes = archive.Counts.Nodes,
                sourceId = packet.GetProperty("annotations").GetProperty("sourceId").GetString(),
                mode = packet.GetProperty("annotations").GetProperty("mode").GetString(),
                sourceStrokes = replay.Interpretation.Acquisition.StrokeCount, sourceSamples = replay.Interpretation.Acquisition.Samples.Count,
                finiteProfiles = replay.Profiles.Length, lineReads = replay.Lines.Length, jointReads = replay.Joints.Length,
                jointParticipantLocations = replay.Joints.Sum(j => j.Participants.Length), jointRays = replay.Joints.Sum(j => j.Rays.Length),
                jointKinds = replay.Joints.GroupBy(j => j.Kind).ToDictionary(group => group.Key, group => group.Count()),
                reflectionReads = replay.Symmetries.Length, reflectionPairs = replay.Symmetries.Sum(s => s.Pairs),
                reflectionPairsWithinTolerance = replay.Symmetries.Sum(s => s.WithinTolerance),
                unmatchedReflectionParticipants = replay.Symmetries.Sum(s => s.Unmatched), selectedRelationReads = replay.Relations,
                coordinateQuantum = QuickDrawAnnotationEvidence.CoordinateQuantum,
                parameterQuantum = QuickDrawAnnotationEvidence.ParameterQuantum,
                angleQuantum = QuickDrawAnnotationEvidence.AngleQuantum,
                angleAllowanceDegrees = (double)replay.Policy.AngleAllowance / QuickDrawAnnotationEvidence.AngleQuantum,
                coincidentAllowanceDegrees = (double)replay.Policy.CoincidentAllowance / QuickDrawAnnotationEvidence.AngleQuantum,
                proximityPageUnits = (double)replay.Policy.Proximity / QuickDrawAnnotationEvidence.CoordinateQuantum,
                nativeRestoreVerified = true, actualSourceAliasesVerified = true, nativeFiniteResidualsVerified = true,
                targetGeometryUnchanged = true, sourceOrderRetained = true, annotationGeneratesGeometry = false,
                basis = "Finite source fitted curves, independently quantized to 1/1000 page unit. Target proposal edges are correspondence only.",
                residue = "Every exact source interval remains unaccounted residue of its fitted profile. Reflection selections retain unmatched participants. No exact source fit or intended object is certified.",
                qualification = "Native reads recompute selected finite ray, line and reflection measurements. Rich adapter relation labels are not imported as native facts. Selection remains an external observation; there is no native candidate search or material transfer." });
        }
        File.WriteAllText(Path.Combine(destination, "summary.json"), JsonSerializer.Serialize(summaries,
            new JsonSerializerOptions { WriteIndented = true }), new UTF8Encoding(false));
        Console.WriteLine($"Restored and validated {summaries.Count} finite source-annotation archives into {destination}"); return 0;
    }
}
