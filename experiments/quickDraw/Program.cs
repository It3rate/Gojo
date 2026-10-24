using System.Text;
using System.Text.Json;
using Gojo.Encoding.Structures;
using Gojo.Experiments.QuickDraw;

if (args.Length > 0 && args[0] == "--reconstruct") return ReconstructionRunner.Run(args);
if (args.Length > 0 && args[0] == "--annotate") return AnnotatedLineArtRunner.Run(args);
if (args.Length > 0 && args[0] == "--construct") return ConstructionLadderRunner.Run(args);
if (args.Length > 0 && args[0] == "--interpret-evidence") return QuickDrawInterpretationEvidenceRunner.Run(args);
if (args.Length > 0 && args[0] == "--interpret-annotations") return QuickDrawAnnotationEvidenceRunner.Run(args);
if (args.Length > 0 && args[0] == "--gestalt-evidence") return QuickDrawGestaltEvidenceRunner.Run(args);

if (args.Length != 4 || args[0] != "--input" || args[2] != "--output")
{
    Console.Error.WriteLine("Usage: dotnet run --project experiments/quickDraw -- --input <selected.ndjson> --output <directory>");
    return 2;
}
var destination = Path.GetFullPath(args[3]); Directory.CreateDirectory(destination);
var summaries = new List<object>(); var keys = new HashSet<string>(StringComparer.Ordinal);
using var input = File.OpenText(args[1]);
foreach (var (line, source) in QuickDrawRecord.ReadNdjson(input))
{
    if (!keys.Add(source.KeyId)) throw new InvalidDataException($"Duplicate key_id at line {line}: {source.KeyId}");
    // File names use a batch ordinal, never untrusted source text as a path.
    var prefix = Path.Combine(destination, $"{summaries.Count + 1:D3}");
    var acquisition = new NativeAcquisition(source); var publication = new AcquisitionPublication(acquisition);
    var archive = GojoStructureCompactJson.FromElement(source.KeyId, acquisition.Surface);
    var publicArchive = GojoStructureCompactJson.FromElement($"acquisition-observation-{summaries.Count + 1:D3}", publication.Observation.Surface);
    var audit = GojoStructureCompactJson.FromElement(source.KeyId, publication.Audit);
    var acquisitionJson = GojoStructureCompactJson.Serialize(archive);
    if (!GojoStructureCompactJson.TryToElement(GojoStructureCompactJson.Deserialize(acquisitionJson), out var restored) ||
        restored.Root is not Gojo.Core.Relation root || !NativeAcquisition.TryRead(root, out var nativeRead))
        throw new InvalidDataException($"Native acquisition archive did not restore for line {line}.");
    var expected = source.Strokes.SelectMany(stroke => stroke.Samples).ToArray();
    if (nativeRead.StrokeCount != source.Strokes.Count || nativeRead.Samples.Count != expected.Length ||
        nativeRead.Samples.Where((sample, i) => sample.XNumerator != expected[i].X.Numerator || sample.XDenominator != expected[i].X.Denominator ||
            sample.YNumerator != expected[i].Y.Numerator || sample.YDenominator != expected[i].Y.Denominator ||
            sample.TimeNumerator != expected[i].Time?.Numerator || sample.TimeDenominator != expected[i].Time?.Denominator).Any())
        throw new InvalidDataException($"Native roundtrip changed an acquired value on line {line}.");
    File.WriteAllText(prefix + ".source.ndjson", source.OriginalJson + "\n", new UTF8Encoding(false));
    File.WriteAllText(prefix + ".acquisition.gojo.json", acquisitionJson, new UTF8Encoding(false));
    File.WriteAllText(prefix + ".observation.gojo.json", GojoStructureCompactJson.Serialize(publicArchive), new UTF8Encoding(false));
    File.WriteAllText(prefix + ".audit.gojo.json", GojoStructureCompactJson.Serialize(audit), new UTF8Encoding(false));
    summaries.Add(new { ordinal = summaries.Count + 1, sourceLine = line, keyId = source.KeyId, prompt = source.Word,
        sourceRecognized = source.Recognized, format = source.Strokes.Count == 0 ? "unspecified-empty" : source.HasTiming ? "raw-xyt" : "simplified-xy",
        strokes = source.Strokes.Count, samples = nativeRead.Samples.Count,
        recordedEdges = source.Strokes.Sum(stroke => Math.Max(0, stroke.Samples.Count - 1)),
        acquisitionNodes = archive.Counts.Nodes, observationNodes = publicArchive.Counts.Nodes,
        nativeRestoreVerified = true, inferredJoins = 0, semanticClosureAsserted = false,
        backwardWithinStrokeTimeSteps = source.Strokes.Sum(stroke => stroke.Samples.Skip(1).Where((sample, i) =>
            sample.Time is not null && stroke.Samples[i].Time is not null && sample.Time.CompareTo(stroke.Samples[i].Time!) < 0).Count()),
        interpretation = "sample acquisition only; continuations and crossings uninspected" });
}
File.WriteAllText(Path.Combine(destination, "summary.json"), JsonSerializer.Serialize(summaries, new JsonSerializerOptions { WriteIndented = true }), new UTF8Encoding(false));
Console.WriteLine($"Converted and restored {summaries.Count} acquisition records into {destination}");
return 0;
