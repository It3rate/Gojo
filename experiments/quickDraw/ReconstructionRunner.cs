using System.Text;
using System.Text.Json;
using Gojo.Core;
using Gojo.Encoding.Structures;

namespace Gojo.Experiments.QuickDraw;

public static class ReconstructionRunner
{
    public static int Run(string[] args)
    {
        if (args.Length != 7 || args[0] != "--reconstruct" || args[1] != "--input" || args[3] != "--model" || args[5] != "--output")
        {
            Console.Error.WriteLine("Usage: --reconstruct --input <packets.ndjson> --model <model.json> --output <directory>"); return 2;
        }
        var destination = Path.GetFullPath(args[6]); Directory.CreateDirectory(destination);
        using var modelJson = JsonDocument.Parse(File.ReadAllText(args[4])); var modelDto = modelJson.RootElement;
        var weights = modelDto.GetProperty("weights").EnumerateArray().Select(row => row.EnumerateArray().Select(value => value.GetInt64()).ToArray()).ToArray();
        var model = NativePrior.Create(modelDto.GetProperty("coefficientQuantum").GetInt64(), weights,
            modelDto.TryGetProperty("trainingCount", out var trainingCount) ? trainingCount.GetInt64() : 0);
        // The detached transport is the execution boundary. No input DTO or
        // pre-transport model object is passed to the reconstruction reader.
        var (restoredModel, modelNodes) = Restore(model, Path.Combine(destination, "model.gojo.json"), "learned-local-equations");
        var summaries = new List<object>(); var ids = new HashSet<string>(StringComparer.Ordinal);
        using var reader = File.OpenText(args[2]);
        using var output = new StreamWriter(Path.Combine(destination, "decoded.ndjson"), false, new UTF8Encoding(false));
        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            using var document = JsonDocument.Parse(line); var row = document.RootElement;
            var id = row.GetProperty("id").GetString() ?? throw new InvalidDataException("An evaluator envelope needs an id.");
            if (!ids.Add(id)) throw new InvalidDataException("Duplicate reconstruction envelope id.");
            var strokes = row.GetProperty("strokes").EnumerateArray().Select(stroke => new SequentialEvidence.StrokeInput(
                stroke.GetProperty("sections").EnumerateArray().Select(section => new SequentialEvidence.SectionInput(
                    section.GetProperty("start").EnumerateArray().Select(value => value.GetInt64()).ToArray(),
                    section.GetProperty("end").EnumerateArray().Select(value => value.GetInt64()).ToArray(),
                    section.GetProperty("features").EnumerateArray().Select(value => value.GetInt64()).ToArray())).ToArray())).ToArray();
            var packet = SequentialEvidence.Create(row.GetProperty("coordinateQuantum").GetInt64(), strokes);
            var prefix = Path.Combine(destination, $"{summaries.Count + 1:D4}");
            var (restoredPacket, evidenceNodes) = Restore(packet, prefix + ".evidence.gojo.json", "sequential-measured-evidence");
            var program = NativeReconstruction.Generate(restoredPacket, restoredModel);
            var (restoredProgram, programNodes) = Restore(program, prefix + ".program.gojo.json", "generated-finite-curves");
            if (!NativeReconstruction.TryRead(restoredProgram, out var decoded)) throw new InvalidDataException("Generated native program failed detached readback.");
            var sequence = 0;
            var decodedStrokes = decoded.Strokes.Select(stroke => new { sections = stroke.Select(curve => new {
                controls = curve.Controls.Select(point => new[] { point.X, point.Y }).ToArray(), sequence = sequence++ }).ToArray() }).ToArray();
            var result = new { id, coordinateQuantum = decoded.Frame.Quantum, strokes = decodedStrokes, sectionCount = sequence,
                nativeRestoreVerified = true, evidenceNodes, programNodes };
            output.WriteLine(JsonSerializer.Serialize(result));
            summaries.Add(new { ordinal = summaries.Count + 1, id, sectionCount = sequence, nativeRestoreVerified = true, evidenceNodes, programNodes });
        }
        File.WriteAllText(Path.Combine(destination, "summary.json"), JsonSerializer.Serialize(new { modelNodes, drawings = summaries },
            new JsonSerializerOptions { WriteIndented = true }), new UTF8Encoding(false));
        Console.WriteLine($"Reconstructed {summaries.Count} detached native evidence packets into {destination}"); return 0;
    }

    private static (Relation Root, int Nodes) Restore(Relation source, string path, string label)
    {
        var packet = GojoStructureCompactJson.FromElement(label, source); var json = GojoStructureCompactJson.Serialize(packet);
        File.WriteAllText(path, json, new UTF8Encoding(false));
        if (!GojoStructureCompactJson.TryToElement(GojoStructureCompactJson.Deserialize(json), out var restored) || restored.Root is not Relation root)
            throw new InvalidDataException("Native compact transport failed to restore a relation root.");
        return (root, packet.Counts.Nodes);
    }
}
