using System.Diagnostics;
using System.IO.Compression;
using System.Text.Json;
using Gojo.Core;
using Gojo.Encoding.Structures;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;
using R = Gojo.Experiments.QualitativeFrieze.OrnamentReconstruction;
using A = Gojo.Experiments.QualitativeFrieze.OrnamentArrangementRead;

namespace Gojo.Experiments.QualitativeFrieze;

/// <summary>Compare a published question vocabulary with explicit complete
/// branch-and-route contracts. Names and construction notes never admit a
/// candidate. All images are optional readings of actual executed programs.</summary>
public static partial class OrnamentConceptExperiment
{
    private sealed record Pair(string Source, string Target, string Legacy, int[] Disagreements,
        string Branches, string Corners, string Incidence, string OrderedRelations,
        int BranchWork, int CornerWork, int CorrespondenceRows, int SourceAnchors, int SourceRoutes,
        int TargetAnchors, int TargetRoutes);

    public static int Run(string[] args)
    {
        var output = Path.Combine("experiments", "QualitativeFrieze", "results", "ornament-concepts");
        for (var index = 0; index < args.Length; index++)
            if (args[index] == "--output" && index + 1 < args.Length) output = args[++index];
            else throw new ArgumentException("Usage: --ornament-concepts [--output DIRECTORY]");
        Directory.CreateDirectory(Path.Combine(output, "patterns"));
        var watch = Stopwatch.StartNew();
        var cases = OrnamentArrangementCases.All;
        var samples = cases.Select(item => item.Create()).ToArray();
        var vocabulary = OrnamentReconstructionVocabulary.Build();
        var queries = vocabulary.Select(entry => entry.Query).ToArray();
        var inputs = samples.Select((sample, index) => new R.Input(cases[index].Id, sample.Observation, sample.MechanicalGroups)).ToArray();
        var bank = new R.Bank(inputs, queries, new(6, 4096, 3, 4096, 256, 512));
        var answers = inputs.Select(input => queries.Select(query => bank.Read(input, query).Answer).ToArray()).ToArray();
        var compactWitnesses = new List<IElement>();
        var branchQuestions = samples.Select(sample => {
            var question = A.Crystallize("Whole branch-and-route arrangement", sample.Observation, out var witness);
            compactWitnesses.Add(witness); return question;
        }).ToArray();
        var cornerQuestions = samples.Select(sample => new A.Question("Every published site and segment", sample.Observation, false)).ToArray();
        var pairs = new List<Pair>(); var retained = new List<IElement>(); var importReads = new List<IElement>();
        var nativeQueries = new List<IElement>(); var transportGraphs = 0; var transportAnswers = 0;
        for (var index = 0; index < samples.Length; index++)
        {
            var item = cases[index]; var sample = samples[index];
            OrnamentReconstructionRendering.Save(sample, Path.Combine(output, "patterns", item.Id + ".svg"), false);
            OrnamentReconstructionRendering.Save(sample, Path.Combine(output, "patterns", item.Id + "-groups.svg"), true);
            var importedRecipe = Restore(sample.Recipe);
            if (!OrnamentReconstructionCases.TryGenerate(importedRecipe, out var replay, out var generation))
                throw new InvalidOperationException("A native case recipe must regenerate without its authoring factory.");
            var replayInput = new R.Input("independent concept output", replay!.Observation, replay.MechanicalGroups);
            var critic = new R.Bank([replayInput], queries, bank.Configuration);
            if (!queries.Select(query => critic.Read(replayInput, query).Answer).SequenceEqual(answers[index]))
                throw new InvalidOperationException("Independent native execution changed the published question profile.");
            transportAnswers += queries.Length;
            if (new OrnamentReconstructionGraphRead(sample.Observation, replay.Observation, 100000).Answer != "Yes")
                throw new InvalidOperationException("Independent native execution must preserve the strict published graph.");
            transportGraphs++;
            var restoredQuestion = Restore(branchQuestions[index].Surface);
            var transportedQuestion = new A.Question("Imported source-free contract", restoredQuestion);
            var independentRead = new A.Receipt(transportedQuestion, replay.Observation);
            if (independentRead.Answer != "Yes") throw new InvalidOperationException("A transported native contract must admit the independently generated design.");
            if (index < 8) importReads.Add(new Relation(importedRecipe, new Relation(generation, independentRead.Surface)));
            // The assessed vocabulary is also an actual source-free message,
            // not merely a vector in the report. A read may select this message,
            // either arrangement contract, or their connected conjunction.
            var legacyClaims = new R.Message(queries.Select((query, q) => (query, q))
                .Where(pair => Known(answers[index][pair.q]))
                .Select(pair => new R.Claim(pair.query, answers[index][pair.q])).ToArray());
            var legacyFocus = new R.Focus(queries, queries.Where((_, q) => Known(answers[index][q])).ToArray());
            nativeQueries.Add(new Relation(new Relation(branchQuestions[index].Surface, cornerQuestions[index].Surface),
                new Relation(legacyClaims.Surface, legacyFocus.Surface)));
            for (var target = 0; target < samples.Length; target++)
            {
                var branch = new A.Receipt(branchQuestions[index], samples[target].Observation);
                var corner = new A.Receipt(cornerQuestions[index], samples[target].Observation);
                var graph = new OrnamentReconstructionGraphRead(sample.Observation, samples[target].Observation, 100000, false);
                var relations = new OrnamentReconstructionGraphRead(sample.Observation, samples[target].Observation, 100000, true);
                var disagreements = Enumerable.Range(0, queries.Length).Where(q => Known(answers[index][q]) &&
                    Known(answers[target][q]) && answers[index][q] != answers[target][q]).ToArray();
                var legacy = disagreements.Length != 0 ? "No" : answers[index].Concat(answers[target]).All(Known) ? "Yes" : "Open";
                var sourceSize = ResolutionSize(branch.SourceResolution); var targetSize = ResolutionSize(branch.TargetResolution);
                var correspondences = branch.Answer == "Yes" ? E((Relation)branch.Correspondence.Left).Length + E((Relation)branch.Correspondence.Right).Length : 0;
                pairs.Add(new(item.Id, cases[target].Id, legacy, disagreements, branch.Answer, corner.Answer,
                    graph.Answer, relations.Answer, branch.Work, corner.Work, correspondences,
                    sourceSize.Anchors, sourceSize.Routes, targetSize.Anchors, targetSize.Routes));
                if (index == 0 || retained.Count < 8) retained.Add(new Relation(
                    new Relation(branchQuestions[index].Surface, inputs[target].Surface),
                    new Relation(branch.Surface, new Relation(corner.Surface, new Relation(graph.Surface, relations.Surface)))));
            }
            Console.WriteLine($"Concept {index + 1}/{samples.Length}: {cases[index].Id}; {pairs.Count:N0} native arrangement comparisons.");
        }
        watch.Stop();
        var baseline = pairs.Where(pair => pair.Source == cases[0].Id).ToArray();
        var model = new {
            Schema = "ornament-concepts-v1", DefaultSource = cases[0].Id,
            Counts = new { Designs = samples.Length, Queries = queries.Length, PairComparisons = pairs.Count,
                ReceiverReads = bank.EvaluatedReads, TransportGraphs = transportGraphs, TransportAnswers = transportAnswers,
                TransportedQuestions = samples.Length, Seconds = watch.Elapsed.TotalSeconds },
            Cases = cases.Select((item, index) => new { item.Id, item.Name, item.ConstructionNote,
                Sites = E(samples[index].Observation.Sites).Length, Segments = E(samples[index].Observation.Segments).Length,
                Groups = E(samples[index].MechanicalGroups).Length,
                Image = "patterns/" + item.Id + ".svg", GroupImage = "patterns/" + item.Id + "-groups.svg",
                Answers = answers[index], BranchQuestionParticipants = R.Participants(branchQuestions[index].Surface).Count,
                FineBranchQuestionParticipants = R.Participants(new A.Question("Uncrystallized route template", samples[index].Observation).Surface).Count,
                CornerQuestionParticipants = R.Participants(cornerQuestions[index].Surface).Count }),
            Queries = vocabulary.Select(entry => new { entry.Id, entry.Label, Channel = entry.Query.Kind }),
            Pairs = pairs,
            Baseline = new { Legacy = baseline.Count(pair => pair.Legacy == "Yes"),
                Branches = baseline.Count(pair => pair.Branches == "Yes"), Corners = baseline.Count(pair => pair.Corners == "Yes"),
                FlexibleDifferentIncidence = baseline.Count(pair => pair.Branches == "Yes" && pair.Incidence == "No"),
                FlexibleDifferentVocabulary = baseline.Count(pair => pair.Branches == "Yes" && pair.Legacy == "No") },
            PreviousCatalogue = PreviousCatalogue(),
            Limits = new[] {
                "A matching answer vector is agreement under those advertised questions. It is not a complete definition or a guarantee of perceptual similarity.",
                "The branch-and-route question requires whole-graph coverage and a bijection of branch landmarks and nonempty routes. Subdivision sites of degree two may vary; missing or extra connected participation is not ignored.",
                "Topology-only correspondence carries headings, continuation, crossing declarations and mechanical groups outside its promise. It does not establish planar cyclic order, inside/outside, over/under, equal lengths, colours or a geometric embedding.",
                "One component and two disconnected copies are different under this complete arrangement contract. Relative placement of disconnected components is not observed; selecting two components does not specify their screen positions.",
                "Exact-site topology retains every published site and segment but also leaves headings and measured shape open. Strict incidence and ordered-relation checks are separate, finer questions.",
                "All admission decisions read connected native contracts and observations. Names, repertoire origin and screen coordinates are presentation or authoring information, never recognition truth.",
                "The expanded corpus and query policy are authored experiments, not a learned model. Supported examples demonstrate this finite contract, not that every drawing humans call a rosette has this topology." }
        };
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        var json = JsonSerializer.Serialize(model, options);
        File.WriteAllText(Path.Combine(output, "diagnostics.json"), json);
        File.WriteAllText(Path.Combine(output, "data.js"), "globalThis.ORNAMENT_CONCEPTS=" + json + ";");
        File.WriteAllText(Path.Combine(output, "gallery.html"), Html());
        File.WriteAllText(Path.Combine(output, "overview.html"), Overview());
        var archiveRoot = new Relation(S(inputs.Select((input, index) => (IElement)new Relation(input.Surface, samples[index].Recipe)).ToArray()),
            new Relation(S(nativeQueries.ToArray()), new Relation(S(retained.ToArray()),
                new Relation(S(importReads.ToArray()), S(compactWitnesses.ToArray())))));
        var archive = GojoStructureCompactJson.Serialize(GojoStructureCompactJson.FromElement("ornament-concepts-v1", archiveRoot));
        using (var file = File.Create(Path.Combine(output, "concepts.gojo.json.gz")))
        using (var zip = new GZipStream(file, CompressionLevel.SmallestSize))
        using (var writer = new StreamWriter(zip)) writer.Write(archive);
        Console.WriteLine($"Completed {samples.Length} designs, {pairs.Count:N0} pair comparisons in {watch.Elapsed.TotalSeconds:0.0}s.");
        Console.WriteLine(Path.GetFullPath(output)); return 0;
    }

    private static object PreviousCatalogue()
    {
        var path = Path.Combine("experiments", "QualitativeFrieze", "results", "ornament-reconstruction", "diagnostics.json.gz");
        if (!File.Exists(path)) return new { Available = false };
        using var file = File.OpenRead(path); using var zip = new GZipStream(file, CompressionMode.Decompress);
        using var document = JsonDocument.Parse(zip);
        var candidates = document.RootElement.GetProperty("candidates").EnumerateArray().ToArray();
        var source = candidates.First(item => item.GetProperty("id").GetString() == "LOZENGE-ROSETTE-Q0-R0-N1");
        var profile = source.GetProperty("answers").GetString();
        var matches = candidates.Where(item => item.GetProperty("answers").GetString() == profile).ToArray();
        return new { Available = true, Source = source.GetProperty("id").GetString(), Matches = matches.Length,
            Families = matches.Select(item => item.GetProperty("family").GetString()).Distinct().ToArray(),
            Qualification = "Read from the preceding saved run; its 16 matching rosette variants did not include new construction families." };
    }

    private static Relation Restore(Relation surface)
    {
        var text = GojoStructureCompactJson.Serialize(GojoStructureCompactJson.FromElement("source-free-arrangement", surface));
        if (!GojoStructureCompactJson.TryToElement(GojoStructureCompactJson.Deserialize(text), out var imported) || imported.Root is not Relation root)
            throw new InvalidOperationException("A native arrangement contract must survive independent transport.");
        return root;
    }
    private static bool Known(string answer) => answer is "Yes" or "No";
    private static (int Anchors, int Routes) ResolutionSize(Relation resolution) =>
        resolution is { Left: Relation { Right: Relation sites }, Right: Relation { Left: Relation routes } }
            ? (E(sites).Length, E(routes).Length) : (0, 0);
    private static Relation S(params IElement[] items) => StructuralConstraint.Closed(items);
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
}
