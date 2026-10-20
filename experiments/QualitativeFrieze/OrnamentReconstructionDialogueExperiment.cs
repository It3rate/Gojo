using System.Diagnostics;
using System.IO.Compression;
using System.Numerics;
using System.Text.Json;
using System.Text.Json.Nodes;
using Gojo.Core;
using Gojo.Encoding.Structures;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;
using R = Gojo.Experiments.QualitativeFrieze.OrnamentReconstruction;
using D = Gojo.Experiments.QualitativeFrieze.OrnamentReconstructionDialogue;

namespace Gojo.Experiments.QualitativeFrieze;

public static partial class OrnamentReconstructionExperiment
{
    private static int RunDialogue(string output)
    {
        var watch = Stopwatch.StartNew();
        JsonObject data;
        using (var file = File.OpenRead(Path.Combine(output, "diagnostics.json.gz")))
        using (var zip = new GZipStream(file, CompressionMode.Decompress)) data = JsonNode.Parse(zip)!.AsObject();
        RehydratedCompactStructure restored;
        using (var file = File.OpenRead(Path.Combine(output, "reconstruction.gojo.json.gz")))
        using (var zip = new GZipStream(file, CompressionMode.Decompress))
        using (var reader = new StreamReader(zip))
            if (!GojoStructureCompactJson.TryToElement(GojoStructureCompactJson.Deserialize(reader.ReadToEnd()), out restored!))
                throw new InvalidOperationException("Dialogue needs the archived native provider world.");
        var world = (Relation)((Relation)restored.Root).Left;
        var nativeBank = (Relation)world.Left; var providers = E((Relation)world.Right).Cast<Relation>().ToArray();
        var queries = E((Relation)nativeBank.Right).Cast<Relation>().Select((surface, index) =>
            new R.Query(data["queries"]![index]!["label"]!.GetValue<string>(), surface)).ToArray();
        var metadata = data["candidates"]!.AsArray();
        var samples = new OrnamentReconstructionCases.Sample[providers.Length];
        var inputs = new R.Input[providers.Length];
        var configuration = new R.Options(6, 4096, 3, 4096, 256, 512);
        for (var index = 0; index < providers.Length; index++)
        {
            if (!OrnamentReconstructionCases.TryGenerate((Relation)providers[index].Right, out var sample, out _))
                throw new InvalidOperationException("Every complete archived provider recipe must execute independently.");
            samples[index] = sample!;
            inputs[index] = new(metadata[index]!["id"]!.GetValue<string>(), sample!.Observation, sample.MechanicalGroups);
        }
        var bank = new R.Bank(inputs, queries, configuration); var answers = new string[inputs.Length][];
        for (var index = 0; index < inputs.Length; index++)
        {
            answers[index] = queries.Select(query => bank.Read(inputs[index], query).Answer).ToArray();
            if (Codes(answers[index]) != metadata[index]!["answers"]!.GetValue<string>())
                throw new InvalidOperationException("Archived recipe regeneration changed a native candidate query profile.");
            if (index % 64 == 0) Console.WriteLine($"Dialogue revalidated {index + 1}/{inputs.Length} transported candidate recipes.");
        }
        var goals = data["goals"]!.AsArray().Select(node => (Id: node!["id"]!.GetValue<string>(),
            Indices: node["queryIndices"]!.AsArray().Select(index => index!.GetValue<int>()).ToArray())).ToArray();
        var order = Enumerable.Range(0, inputs.Length).OrderBy(index => metadata[index]!["edges"]!.GetValue<int>())
            .ThenBy(index => metadata[index]!["groups"]!.GetValue<int>()).ThenBy(index => index).ToArray();
        var profiles = new[] { ("motif", new[] { "visible motif" }),
            ("relational", new[] { "visible motif", "joint occurrence", "crossing", "continuation" }),
            ("mechanical", new[] { "visible motif", "joint occurrence", "crossing", "continuation", "mechanical path" }) };
        var contexts = profiles.Select(profile =>
        {
            var context = new R.Context(order.Select(index => inputs[index]).ToArray(), queries.Where(query => profile.Item2.Contains(query.Kind)).ToArray());
            return (Id: profile.Item1, Projection: new OrnamentReconstructionProjection(bank, context, queries));
        }).ToArray();
        var inputIndex = inputs.Select((input, index) => (input, index)).ToDictionary(pair => pair.input, pair => pair.index);
        var policies = new Dictionary<(string Engine, string Goal, string Family), D.Policy>();
        var outputs = new Dictionary<int, (OrnamentReconstructionCases.Sample Sample, string[] Answers)>();
        var rows = new List<object?[]>(); var episodes = new List<object>(); var archives = new List<IElement>();
        var sources = data["sources"]!.AsArray();
        foreach (var sourceNode in sources)
        {
            var id = sourceNode!["id"]!.GetValue<string>(); var family = sourceNode["family"]!.GetValue<string>();
            var originalIndex = Array.FindIndex(inputs, input => input.Name == id);
            var source = samples[originalIndex].Replay();
            var sourceInput = new R.Input("opaque sender", source.Observation, source.MechanicalGroups);
            var sourceBank = new R.Bank([sourceInput], queries, configuration); var supplier = new D.Supplier(sourceBank, sourceInput);
            var truth = queries.Select(query => sourceBank.Read(sourceInput, query).Answer).ToArray();
            if (Codes(truth) != sourceNode["answers"]!.GetValue<string>()) throw new InvalidOperationException("Dialogue sender changed its native promise.");
            var graphReads = new Dictionary<int, (string Incidence, string Relations)>();
            foreach (var context in contexts) foreach (var goal in goals)
            {
                var projection = context.Projection; var targetMask = BigInteger.Zero;
                for (var index = 0; index < projection.Inputs.Count; index++)
                    if (goal.Indices.All(q => truth[q] is "Yes" or "No" && answers[inputIndex[projection.Inputs[index]]][q] == truth[q]))
                        targetMask |= BigInteger.One << index;
                foreach (var preference in new[] { "compact", "reverse", "withheld family" })
                {
                    var key = (context.Id, goal.Id, preference == "withheld family" ? family : "");
                    if (!policies.TryGetValue(key, out var policy))
                    {
                        var universe = projection.Universe;
                        if (preference == "withheld family") for (var index = 0; index < projection.Inputs.Count; index++)
                            if (metadata[inputIndex[projection.Inputs[index]]]!["family"]!.GetValue<string>() == family)
                                universe &= ~(BigInteger.One << index);
                        policies.Add(key, policy = new(projection, goal.Indices, universe));
                    }
                    var result = policy.Run(supplier);
                    foreach (var trimmed in new[] { false, true })
                    {
                        var message = trimmed ? result.FinalMessage : result.OriginalMessage;
                        var admitted = projection.Admitted(message.Claims) & policy.InitialUniverse;
                        var possible = projection.Possible(message.Claims) & policy.InitialUniverse;
                        var candidates = projection.Indices(admitted);
                        var chosen = candidates.Count == 0 ? -1 : preference == "reverse" ? candidates[^1] : candidates[0];
                        var selected = chosen < 0 ? -1 : inputIndex[projection.Inputs[chosen]];
                        var fresh = selected < 0 ? default : Output(selected);
                        var unknown = goal.Indices.Count(index => truth[index] is "Open" or "Blocked");
                        var outputUnknown = selected < 0 ? goal.Indices.Length : goal.Indices.Count(index => fresh.Answers[index] is "Open" or "Blocked");
                        bool? correct = selected < 0 || unknown != 0 || outputUnknown != 0 ? null : goal.Indices.All(index => truth[index] == fresh.Answers[index]);
                        var robust = admitted != BigInteger.Zero && (possible & ~targetMask) == BigInteger.Zero;
                        var graph = selected < 0 ? default : Graph(selected);
                        var outside = selected < 0 ? 0 : Enumerable.Range(0, queries.Length).Count(index => !goal.Indices.Contains(index) &&
                            truth[index] is "Yes" or "No" && fresh.Answers[index] is "Yes" or "No" && truth[index] != fresh.Answers[index]);
                        var clauseIndices = message.Claims.Select(claim => Array.FindIndex(queries, query => ReferenceEquals(query.Surface, claim.Query.Surface))).ToArray();
                        rows.Add([id, family, goal.Id, context.Id, preference, trimmed ? "dialogue-pruned" : "dialogue", message.HintCount, message.NativeCost,
                            candidates.Count, OrnamentReconstructionSearch.Count(possible), OrnamentReconstructionSearch.Count(admitted & targetMask), robust,
                            selected < 0 ? null : inputs[selected].Name, correct, unknown, outputUnknown,
                            selected < 0 ? "Open" : graph.Incidence, selected < 0 ? "Open" : graph.Relations,
                            outside, 0, 0d, clauseIndices, message.Claims.Select(claim => claim.Answer).ToArray()]);
                    }
                    episodes.Add(new { Source = id, Engine = context.Id, Goal = goal.Id, Context = preference,
                        Requests = result.Steps.Count, OriginalHints = result.OriginalMessage.HintCount, FinalHints = result.FinalMessage.HintCount,
                        result.Uniform, result.StopReason, Pruned = result.PruneSteps.Count,
                        Steps = result.Steps.Select(step => new { Query = Array.FindIndex(queries, query => ReferenceEquals(query.Surface, step.Query.Surface)),
                            step.Reply.Answer, Before = OrnamentReconstructionSearch.Count(step.BeforePossible), After = OrnamentReconstructionSearch.Count(step.AfterPossible),
                            step.Decision.Gain, Cost = step.Decision.NativeQueryCost }).ToArray() });
                    if (archives.Count < 8 && id.Contains("-Q0-R1-N2", StringComparison.Ordinal) && context.Id == "mechanical" &&
                        goal.Id == "combined" && preference == "compact")
                    {
                        // Retain the actual communication transcript and source
                        // audits, with a declared open proof-computation scope.
                        // The full prepared matrix is not implicitly archived.
                        archives.Add(new Relation(new Relation(projection.Context.Surface, policy.Focus.Surface),
                            new Relation(supplier.Port, new Relation(S(result.Steps.Select(step => (IElement)step.Surface).ToArray()),
                                new Relation(result.FinalMessage.Surface, new Relation(
                                    S(result.Steps.Select(step => (IElement)step.Reply.AuditSurface).ToArray()), StructuralConstraint.Open()))))));
                    }
                }
            }
            if (sources.IndexOf(sourceNode) % 16 == 0) Console.WriteLine($"Dialogue source {sources.IndexOf(sourceNode) + 1}/{sources.Count}; {episodes.Count:N0} episodes; {outputs.Count} fresh selected outputs.");

            (string Incidence, string Relations) Graph(int selected)
            {
                if (!graphReads.TryGetValue(selected, out var read))
                {
                    var fresh = Output(selected);
                    graphReads.Add(selected, read = (new OrnamentReconstructionGraphRead(source.Observation, fresh.Sample.Observation, 20000, false).Answer,
                        new OrnamentReconstructionGraphRead(source.Observation, fresh.Sample.Observation, 20000, true).Answer));
                }
                return read;
            }
        }
        watch.Stop();
        var payload = new { Schema = "ornament-dialogue-v1", Columns = data["trialColumns"]!.DeepClone(), Rows = rows,
            Episodes = episodes, Seconds = watch.Elapsed.TotalSeconds, NativeReads = bank.EvaluatedReads, CacheHits = bank.CachedReads,
            FreshOutputs = outputs.Count, TransportedCandidates = inputs.Length,
            Limits = new[] { "Query choice precedes the opaque reply and uses only public candidate predictions, available services and the native focus.",
                "Known consensus is conditional on the advertised repertoire and question vocabulary. Withheld families can expose a confident but incorrect reconstruction under an independent judge.",
                "Backward pruning preserves the receiver's inferred focus profile; it is not a global minimum over the full query library.",
                "Sender and candidate profiles are eagerly assessed for the independent critic. Request counts measure communication, not avoided supplier computation; native read counters cover the receiver candidate bank only.",
                "One fresh selected-output event per program is explicitly reused for non-draining questions; transcript count is not a count of independent generated samples." } };
        using (var file = File.Create(Path.Combine(output, "dialogue.json.gz")))
        using (var zip = new GZipStream(file, CompressionLevel.SmallestSize)) JsonSerializer.Serialize(zip, payload, JsonOptions);
        var archive = GojoStructureCompactJson.Serialize(GojoStructureCompactJson.FromElement("ornament-dialogue-v1", S(archives.ToArray())));
        using (var file = File.Create(Path.Combine(output, "dialogue.gojo.json.gz")))
        using (var zip = new GZipStream(file, CompressionLevel.SmallestSize))
        using (var writer = new StreamWriter(zip)) writer.Write(archive);
        Console.WriteLine($"Completed {episodes.Count:N0} active-query episodes, {rows.Count:N0} judged messages in {watch.Elapsed.TotalSeconds:0.0}s.");
        return 0;

        (OrnamentReconstructionCases.Sample Sample, string[] Answers) Output(int index)
        {
            if (!outputs.TryGetValue(index, out var cached))
            {
                var generated = samples[index].Replay();
                var input = new R.Input("fresh dialogue reconstruction", generated.Observation, generated.MechanicalGroups);
                var critic = new R.Bank([input], queries, configuration);
                var actual = queries.Select(query => critic.Read(input, query).Answer).ToArray();
                if (!actual.SequenceEqual(answers[index])) throw new InvalidOperationException("Fresh dialogue output changed its native query profile.");
                outputs.Add(index, cached = (generated, actual));
            }
            return cached;
        }
    }
    private static string Codes(IEnumerable<string> answers) => string.Concat(answers.Select(answer => answer == "Yes" ? '+' : answer == "No" ? '-' : answer == "Blocked" ? '!' : '?'));
}
