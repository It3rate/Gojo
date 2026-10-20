using System.IO.Compression;
using System.Text.Json;
using System.Text.Json.Nodes;
using Gojo.Core;
using Gojo.Encoding.Structures;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;
using R = Gojo.Experiments.QualitativeFrieze.OrnamentReconstruction;

namespace Gojo.Experiments.QualitativeFrieze;

public static partial class OrnamentReconstructionExperiment
{
    private static int Refresh(string output)
    {
        using var compressed = File.OpenRead(Path.Combine(output, "diagnostics.json.gz"));
        using var zip = new GZipStream(compressed, CompressionMode.Decompress);
        var data = JsonNode.Parse(zip)!.AsObject();
        if (data["schema"]?.GetValue<string>() != "ornament-reconstruction-v1") throw new InvalidOperationException("Unknown reconstruction report schema.");
        var vocabulary = OrnamentReconstructionVocabulary.Build();
        var queryMetadata = data["queries"]!.AsArray();
        if (queryMetadata.Count != vocabulary.Count) throw new InvalidOperationException("Archived query metadata must retain its vocabulary order.");
        for (var index = 0; index < vocabulary.Count; index++)
        {
            if (queryMetadata[index]!["id"]!.GetValue<string>() != vocabulary[index].Id)
                throw new InvalidOperationException("Archived query identities must retain their vocabulary order.");
            queryMetadata[index]!["label"] = vocabulary[index].Label;
        }
        var codebook = S(vocabulary.Select(entry => (IElement)entry.Query.Surface).ToArray());
        data["sharedCosts"] = JsonSerializer.SerializeToNode(new {
            QueryDefinitions = OrnamentReconstructionPacket.Participants(codebook).Count,
            IncrementPerClaim = 8, EmptyMessage = 2,
            Qualification = "Additional participants assume these exact native query definitions are already shared. This is not a wire encoding; independent worlds still require codebook correspondence." }, JsonOptions);
        data["assessmentScope"] = "All candidate and sender query profiles are prepared eagerly for the independent comparison. Sparse requests demonstrate communication and inference savings, not avoided supplier computation. Native read counters cover the receiver candidate bank, not source assessments, fresh output critics, graph reads or transport checks.";
        if (File.Exists(Path.Combine(output, "dialogue.json.gz")))
        {
            using var dialogueFile = File.OpenRead(Path.Combine(output, "dialogue.json.gz"));
            using var dialogueZip = new GZipStream(dialogueFile, CompressionMode.Decompress);
            data["dialogue"] = JsonNode.Parse(dialogueZip);
        }
        data["transportedGeneration"] = ImportControls(output, data["candidates"]!.AsArray());
        var json = data.ToJsonString(JsonOptions);
        compressed.Dispose(); zip.Dispose();
        SaveReportData(output, json);
        File.WriteAllText(Path.Combine(output, "gallery.html"), Html());
        File.WriteAllText(Path.Combine(output, "overview.html"), Overview(
            data["candidates"]!.Deserialize<OrnamentReconstructionStudy.CandidateInfo[]>(JsonOptions)!,
            data["sources"]!.Deserialize<OrnamentReconstructionStudy.SourceInfo[]>(JsonOptions)!));
        Console.WriteLine($"Refreshed report. Shared query codebook: {OrnamentReconstructionPacket.Participants(codebook).Count:N0} native participants.");
        return 0;
    }

    private static JsonNode ImportControls(string output, JsonArray metadata)
    {
        using var source = File.OpenRead(Path.Combine(output, "reconstruction.gojo.json.gz"));
        using var zip = new GZipStream(source, CompressionMode.Decompress);
        using var reader = new StreamReader(zip);
        if (!GojoStructureCompactJson.TryToElement(GojoStructureCompactJson.Deserialize(reader.ReadToEnd()), out var restored) ||
            restored.Root is not Relation { Left: Relation { Left: Relation bank, Right: Relation providers } })
            throw new InvalidOperationException("A reconstruction archive exposes a receiver bank and native providers.");
        var queryScope = (Relation)bank.Right;
        var queries = E(queryScope).Cast<Relation>().Select(surface => new R.Query("transported query", surface)).ToArray();
        var providerRows = E(providers).Cast<Relation>().ToArray();
        var specs = OrnamentReconstructionCases.Specs.OrderBy(spec => spec.Transform)
            .ThenByDescending(spec => spec.ReachRecipe).ThenByDescending(spec => spec.Repeats).ToArray();
        var families = new HashSet<string>(); var comparisons = 0; var graphMatches = 0;
        for (var index = 0; index < providerRows.Length; index++)
        {
            families.Add(specs[index].Family);
            var row = providerRows[index];
            if (!OrnamentReconstructionCases.TryGenerate((Relation)row.Right, out var generated, out _))
                throw new InvalidOperationException("A complete transported native recipe must regenerate without its authoring factory.");
            var nativeInput = (Relation)row.Left; var nativeObservation = (Relation)nativeInput.Left;
            var frameSurface = (Relation)((Relation)nativeObservation.Left).Left;
            if (!QualitativeMotionFrameRead.TryReadStandard(frameSurface, out var frame) ||
                !frame.TryRebind(nativeInput, out var rebound, out _) || rebound is not Relation input ||
                input.Left is not Relation { Left: Relation { Right: Relation sites }, Right: Relation { Left: Relation edges,
                    Right: Relation { Left: Relation continuations, Right: Relation crossings } } })
                throw new InvalidOperationException("The transported public observation must have an exact native frame read.");
            var observation = new QualitativeRecognitionObservation(frame.Target, sites, edges, continuations, crossings);
            var before = new R.Input("archived public output", observation, (Relation)input.Right);
            var after = new R.Input("regenerated public output", generated!.Observation, generated.MechanicalGroups);
            var assess = new R.Bank([before, after], queries, new(6, 4096, 3, 4096, 256, 512));
            foreach (var query in queries)
            {
                if (assess.Read(before, query).Answer != assess.Read(after, query).Answer)
                    throw new InvalidOperationException("Transported recipe execution changed an advertised native answer.");
                comparisons++;
            }
            if (new OrnamentReconstructionGraphRead(observation, generated.Observation).Answer != "Yes")
                throw new InvalidOperationException("Transported recipe execution changed the strict published relation graph.");
            graphMatches++;
            var id = metadata[index]!["id"]!.GetValue<string>();
            var image = "patterns/output-" + id + ".svg";
            var grouped = "patterns/output-" + id + "-groups.svg";
            OrnamentReconstructionRendering.Save(generated, Path.Combine(output, image), false);
            OrnamentReconstructionRendering.Save(generated, Path.Combine(output, grouped), true);
            metadata[index]!["outputImage"] = image;
            metadata[index]!["outputGroupImage"] = grouped;
            if (index % 32 == 0) Console.WriteLine($"Native import {index + 1}/{providerRows.Length}: strict graph and {queries.Length} answers preserved.");
        }
        return JsonSerializer.SerializeToNode(new { Families = families.Count, Recipes = providerRows.Length, StrictGraphs = graphMatches,
            RecipeLibraryParticipants = OrnamentReconstructionPacket.Participants(S(providerRows.Select(row => row.Right).ToArray())).Count,
            AnswerComparisons = comparisons, Preserved = true,
            Qualification = "Independent regeneration from transported native recipes; this revalidates provider behavior, not every archived intermediate assessment." }, JsonOptions)!;
    }

    private static void SaveReportData(string output, string json)
    {
        using var bytes = new MemoryStream();
        using (var zip = new GZipStream(bytes, CompressionLevel.SmallestSize, true))
        using (var writer = new StreamWriter(zip)) writer.Write(json);
        var compressed = bytes.ToArray();
        File.WriteAllBytes(Path.Combine(output, "diagnostics.json.gz"), compressed);
        File.WriteAllText(Path.Combine(output, "data.js"), "globalThis.ORNAMENT_RECONSTRUCTION_GZIP=\"" + Convert.ToBase64String(compressed) + "\";");
    }
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static Relation S(params IElement[] entries) => StructuralConstraint.Closed(entries);
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
}
