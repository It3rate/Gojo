using System.Diagnostics;
using System.IO.Compression;
using System.Text.Json;
using Gojo.Core;
using Gojo.Encoding.Structures;
using Gojo.Patterns;
using Gojo.Structures;
using Gojo.Experiments.QualitativeExecution;
using D = Gojo.Experiments.QualitativeFrieze.OrnamentDefaultsPolicy;
using A = Gojo.Experiments.QualitativeFrieze.OrnamentArrangementRead;
using C = Gojo.Experiments.QualitativeFrieze.OrnamentParticipationCount;
using G = Gojo.Experiments.QualitativeFrieze.OrnamentDefaultGeometry;
using R = Gojo.Experiments.QualitativeFrieze.OrnamentReconstruction;
using P = Gojo.Experiments.QualitativeFrieze.ProvisionalEquivalence;

namespace Gojo.Experiments.QualitativeFrieze;

/// <summary>Transfer the same native contracts across differently constructed
/// ornaments, glyphs and networks; then use a shared proposal prior in a
/// source/assumption/execution/revision loop. Labels never supply evidence.</summary>
public static partial class RulePackTransferExperiment
{
    private sealed record RuleFact(string Label, string Answer, string Decision, int Tier, bool Expected, bool Waived);
    private sealed record Transfer(string Pack, string Case, string Context, string Answer, bool Eligible,
        int StrongLower, int StrongUpper, int WeakLower, int WeakUpper, RuleFact[] Facts, bool? TransportSame);
    private sealed record Context(string Id, string Name, string Note, StructuralRulePack Pack, D.Directive? Directive = null, bool Geometry = true);
    private sealed record PackDefinition(string Id, string Name, string Note, D.Rule[] Rules, Context[] Contexts);
    private sealed record Plan(string Id, string Name, P.Hypothesis Hypothesis);

    public static int Run(string[] args)
    {
        var output = Path.Combine("experiments", "QualitativeFrieze", "results", "rule-pack-transfer");
        for (var i = 0; i < args.Length; i++)
            if (args[i] == "--output" && i + 1 < args.Length) output = args[++i];
            else throw new ArgumentException("Usage: --rule-pack-transfer [--output DIRECTORY]");
        Directory.CreateDirectory(Path.Combine(output, "patterns"));
        var watch = Stopwatch.StartNew(); var standard = new G.Standard(4, 8);
        var definitions = new[] {
            BuildPack("CONTOUR", "Closed course", "One complete returning route. Turns, proportions and construction grouping may vary.", "O-C01", standard),
            BuildPack("BRIDGE", "Clamped bridge", "One route joins two branching sites with four free ends. A nearby unjoined bridge does not satisfy it.", "O-B01", standard),
            BuildPack("COURSES", "Independent courses", "Two whole independent open routes. Their crossing, spacing and mechanical grouping are separate questions.", "O-R01", standard)
        };
        var cases = RuleTransferCases.All; var samples = cases.Select(item => item.Create()).ToArray();
        var publications = samples.Select(G.Publication.FromSample).ToArray();
        var inputs = samples.Select((sample, i) => new R.Input(cases[i].Name, sample.Observation, sample.MechanicalGroups)).ToArray();
        var archive = new List<IElement>(); var comparisons = new List<Transfer>(); var transportChecks = 0;
        var catalogue = cases.Select((item, i) => {
            Save(samples[i], output, item.Id);
            if (!OrnamentReconstructionCases.TryGenerate(Restore(samples[i].Recipe), out var replay, out var replayWitness) ||
                new OrnamentReconstructionGraphRead(samples[i].Observation, replay!.Observation).Answer != "Yes")
                throw new InvalidOperationException("A transfer drawing did not survive independent native execution.");
            archive.Add(new Relation(samples[i].Recipe, new Relation(inputs[i].Surface, replayWitness)));
            return new { item.Id, item.Domain, item.Family, item.Name, item.Note,
                Image = "patterns/" + item.Id + ".svg", GroupImage = "patterns/" + item.Id + "-groups.svg" };
        }).ToArray();
        foreach (var definition in definitions)
        {
            var ordinary = definition.Contexts[0].Pack;
            if (!StructuralRulePack.TryRead(Restore(ordinary.Surface), out var imported, out var importWitness))
                throw new InvalidOperationException("A source-free native rule pack failed import.");
            archive.Add(importWitness);
            foreach (var context in definition.Contexts)
            {
                archive.Add(context.Pack.Surface);
                for (var i = 0; i < samples.Length; i++)
                {
                    var application = new StructuralRulePack.Application(context.Pack, inputs[i], context.Geometry ? publications[i] : null, context.Directive);
                    var receipt = application.Receipt;
                    var facts = definition.Rules.Select(rule => {
                        var read = receipt.Reads.Single(row => ReferenceEquals(row.Rule.Predicate, rule.Predicate));
                        return new RuleFact(rule.Name, read.Fact?.Answer ?? "Open", read.Answer, read.Tier, read.Rule.Expected,
                            read.Answer == "Waived");
                    }).ToArray();
                    bool? same = null;
                    if (context.Id == "ordinary")
                    {
                        var independent = new StructuralRulePack.Application(imported, inputs[i], publications[i]);
                        if (receipt.Answer != independent.Receipt.Answer || receipt.StrongRange != independent.Receipt.StrongRange ||
                            receipt.PreferenceRange != independent.Receipt.PreferenceRange ||
                            !application.Observation.Facts.Select(fact => fact.Answer).SequenceEqual(independent.Observation.Facts.Select(fact => fact.Answer)))
                            throw new InvalidOperationException("Rule-pack transport changed the same public-port read.");
                        same = true; transportChecks++;
                    }
                    comparisons.Add(new(definition.Id, cases[i].Id, context.Id, receipt.Answer, receipt.Eligible,
                        receipt.StrongRange.Lower, receipt.StrongRange.Upper, receipt.PreferenceRange.Lower, receipt.PreferenceRange.Upper, facts, same));
                    // Retain every ordinary target receipt and representative
                    // context interventions. The complete matrix is reproducible,
                    // rather than duplicating every audit proof in the archive.
                    if (context.Id == "ordinary" || i == 0) archive.Add(application.Surface);
                }
            }
            Console.WriteLine($"Pack {definition.Id}: same predicates across {samples.Length} independent target ports and five contexts.");
        }
        var prior = new RevisableRuleGeneration(); archive.Add(prior.Surface);
        var stories = new List<object>(); var generationStages = 0;
        foreach (var scenario in RuleTransferCases.Scenarios())
            stories.Add(GenerateStory(scenario, prior, output, archive, ref generationStages));
        watch.Stop();
        var model = new {
            Schema = "rule-pack-transfer-v1",
            Counts = new { Designs = cases.Count, Domains = 3, Packs = definitions.Length, Applications = comparisons.Count,
                TransportChecks = transportChecks, GenerationStages = generationStages, Seconds = watch.Elapsed.TotalSeconds },
            Cases = catalogue,
            Packs = definitions.Select(pack => new { pack.Id, pack.Name, pack.Note,
                Contexts = pack.Contexts.Select(context => new { context.Id, context.Name, context.Note }),
                Rules = pack.Rules.Select(rule => new { Label = rule.Name, PredicateParticipants = R.Participants(rule.Predicate).Count }) }),
            Reads = comparisons, Generation = stories,
            Findings = new[] {
                "The same source-free predicate graphs are connected to ornament, glyph and path-network public ports. Domain labels select presentation only; all three packs inspect every drawing.",
                "Ordinary structural admission is separate from shape defaults. The construction-strict context can reject a different mechanical recipe even when the whole route arrangement agrees.",
                "An explicit contact exception changes the policy consequence while retaining the original encounter evidence. Missing geometry leaves those defaults unresolved; it does not invent a clean drawing.",
                "Generation defaults prefer sharing a proposal parameter. That is evidence about the proposal's binding structure, not evidence that an unobserved source has equal reaches.",
                "A horizontal-only publication can revise a sample while retaining the equality assumption. A later unequal vertical publication withdraws that assumption and selects independently bound parameters.",
                "Conflicting accounts and a restricted proposal bank can leave no committed output. A previous drawing remains historical evidence; an explicit source scope can resolve which account is used without deleting the other account."
            },
            Limits = new[] {
                "Transfer is demonstrated among three interpretations of finite line/path structures, not arbitrary domains or learned semantic analogy. Each supplier must expose the same interpretable contracts or an explicit correspondence.",
                "The ordinary packs and context changes are authored. No automatic rule inheritance, context recognition, probability calibration, learning of default strength or autonomous program invention is claimed.",
                "The generator samples a finite native palette of one, two and three repetitions of one uncalibrated positive reach, and two role-partition schemes. It does not use calibrated measurements or claim arbitrary continuous completion.",
                "The receiver is given an authored symbolic template with exposed role parameters. The independent coarse route question checks the selected output afterward; it does not infer or choose that template from observations.",
                "Proposal sharing, observed source equivalence and actual output conformance are three distinct reads. A generated output cannot validate a missing source publication.",
                "The geometry supplier uses its declared common-reach word interpretation and arithmetic backend. Breadth applies to declared closed construction groups; filled ownership and periodic neighbour seams remain outside these packs.",
                "All transfer services are evaluated eagerly for this audit. The generation engine executes only the selected proposal, after evaluating the finite proposal bank. No demand-driven speedup is measured.",
                "Event ticks order the demonstration timeline; they do not introduce measured physical durations. The native archive retains executable templates, source links, context, exceptions, selected applications and generation witnesses, rather than a minimal concept packet."
            }
        };
        var json = JsonSerializer.Serialize(model, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        File.WriteAllText(Path.Combine(output, "diagnostics.json"), json);
        File.WriteAllText(Path.Combine(output, "data.js"), "globalThis.RULE_PACK_TRANSFER=" + json + ";");
        File.WriteAllText(Path.Combine(output, "gallery.html"), Html()); File.WriteAllText(Path.Combine(output, "overview.html"), Overview());
        var native = GojoStructureCompactJson.Serialize(GojoStructureCompactJson.FromElement("rule-pack-transfer-v1", S(archive.ToArray())));
        using (var file = File.Create(Path.Combine(output, "transfer.gojo.json.gz")))
        using (var zip = new GZipStream(file, CompressionLevel.SmallestSize))
        using (var writer = new StreamWriter(zip)) writer.Write(native);
        Console.WriteLine($"Completed {comparisons.Count} pack applications, {transportChecks} pack-only transport comparisons and {generationStages} revision stages in {watch.Elapsed.TotalSeconds:0.0}s.");
        Console.WriteLine(Path.GetFullPath(output)); return 0;
    }

    private static PackDefinition BuildPack(string id, string name, string note, string referenceId, G.Standard standard)
    {
        var reference = RuleTransferCases.Create(referenceId);
        var arrangement = A.Crystallize(name, reference.Observation, out _);
        var count = C.AllGroups(reference.MechanicalGroups, out _);
        var geometry = new[] { standard.NoUnexpectedEncounters, standard.LegibleClosedGroups, standard.ModerateAspect, standard.HorizontalMirror };
        var rules = new[] { new D.Rule("Whole route arrangement", arrangement.Surface), new D.Rule("Explained encounters", geometry[0].Surface),
            new D.Rule("Resolvable closed construction contours", geometry[1].Surface), new D.Rule("Ordinary aspect", geometry[2].Surface),
            new D.Rule("Selected-frame mirror", geometry[3].Surface), new D.Rule("Reference drawing-group inventory", count.Surface) };
        StructuralRulePack Pack(D.Policy policy) => new(name, policy, geometryQuestions: geometry, arrangementQuestions: [arrangement], countQuestions: [count]);
        var ordinary = Pack(new(required: [rules[0]], strongDefaults: [rules[1], rules[2]], preferences: [rules[3]], unused: [rules[4], rules[5]]));
        return new(id, name, note, rules, [
            new("ordinary", "Ordinary realization", "Require only the whole arrangement; contact and breadth are strong defaults, aspect is weak, and mirror/grouping are unselected.", ordinary),
            new("contact-exception", "Allow unexplained encounters", "Waive this exact encounter default. The original evidence remains visible; no join or over/under order is inferred.", ordinary, new(ordinary.Policy, [rules[1]])),
            new("construction-strict", "Preserve construction grouping", "Also require the reference's mechanical group inventory. This is a stricter question, not a correction of the ordinary concept.",
                Pack(new(required: [rules[0], rules[5]], strongDefaults: [rules[1], rules[2]], preferences: [rules[3]], unused: [rules[4]]))),
            new("symmetry-preferred", "Prefer selected-frame mirror", "Use the same symmetry predicate as an additional strong default without making it a required identifier.",
                Pack(new(required: [rules[0]], strongDefaults: [rules[1], rules[2], rules[4]], preferences: [rules[3]], unused: [rules[5]]))),
            new("geometry-unavailable", "Topology port only", "Offer the same graph and grouping, with no finer word-basis publication. Geometry defaults remain unresolved instead of consulting private words.", ordinary, Geometry: false)
        ]);
    }

    private static object GenerateStory(RuleTransferCases.GenerationCase scenario, RevisableRuleGeneration prior,
        string output, List<IElement> archive, ref int generationStages)
    {
        var template = scenario.Template; var atom = scenario.Reach;
        var palette = new[] { S(atom), S(atom, atom), S(atom, atom, atom) }; var plans = new List<Plan>();
        for (var i = 0; i < palette.Length; i++)
        {
            var parameter = new Placeholder();
            plans.Add(new("SHARED-" + (i + 1), "One parameter · sample " + (i + 1), template.Hypothesis(
                S(new Relation(scenario.HorizontalRole, parameter), new Relation(scenario.VerticalRole, parameter)), S(new Relation(parameter, palette[i])))));
        }
        for (var h = 0; h < palette.Length; h++) for (var v = 0; v < palette.Length; v++)
        {
            var first = new Placeholder(); var second = new Placeholder();
            plans.Add(new("SPLIT-" + (h + 1) + "-" + (v + 1), "Independent parameters · samples " + (h + 1) + "/" + (v + 1), template.Hypothesis(
                S(new Relation(scenario.HorizontalRole, first), new Relation(scenario.VerticalRole, second)), S(new Relation(first, palette[h]), new Relation(second, palette[v])))));
        }
        var supplier = template.Realize(plans[6].Hypothesis); // finite palette order: independent 2/1
        var otherSupplier = template.Realize(plans[3].Hypothesis); // independent 1/1, a competing account
        if (supplier.Answer != "Yes" || otherSupplier.Answer != "Yes") throw new InvalidOperationException("Native suppliers must execute and expose actual role values.");
        Save(supplier.Sample!, output, scenario.Id + "-source");
        var coarse = A.Crystallize("Communicated coarse route contract", supplier.Sample!.Observation, out var coarseWitness);
        var eventRole = new Placeholder(); var source = new Placeholder(); var competitor = new Placeholder();
        var partial = new P.Publication(source, eventRole, S(E(supplier.RoleValues).OfType<Relation>().Single(row => ReferenceEquals(row.Left, scenario.HorizontalRole))));
        var full = new P.Publication(source, eventRole, supplier.RoleValues);
        var competing = new P.Publication(competitor, eventRole, otherSupplier.RoleValues);
        var sourceProvenance = new Relation(new Relation(source, supplier.Surface), new Relation(competitor, otherSupplier.Surface));
        var clock = new Carrier(); var stages = new List<object>(); var nativeStages = new List<Relation>();
        RevisableRuleGeneration.Stage? previous = null; string? previousImage = null; var all = plans.Select(plan => plan.Hypothesis).ToArray();
        Add("initial", "Choose a provisional realization", "The receiver has a symbolic construction with exposed roles, but no source role values. The sharing default guides one sample while source equivalence remains Open; an independent coarse route contract checks the output afterward.", [], prior.Ordinary);
        Add("partial", "One exposed reach", "The horizontal role is supplied from the actual source execution; the vertical role remains unavailable. The sample changes while shared participation remains a hypothesis.", [partial], prior.Ordinary);
        Add("reveal", "Reveal the independent reach", "Both actual source roles are now published. Unequal positive participation withdraws the shared-parameter assumption while preserving the coarse route contract.", [full], prior.Ordinary);
        Add("conflict", "Two conflicting accounts", "Both suppliers are in the same requested event scope. No proposal may silently choose which incompatible account is true.", [full, competing], prior.Ordinary);
        Add("scoped", "Explicitly select an account", "The context selects one supplier as the working account. The other account and its conflict stay in the retained evidence.", [full, competing], prior.Ordinary, workingSources: S(source));
        Add("independent-prior", "Change the construction preference", "With finer source evidence withheld again, prefer independently bound roles. An identical-looking sample can carry a different construction assumption.", [], prior.Independent);
        Add("unused-prior", "Leave sharing deliberately unselected", "All admissible partitions remain possible; an explicit alternative sampling order chooses a different representative without asserting source equality.", [], prior.Unselected, alternativeOrder: true);
        Add("capability-limited", "The offered proposal bank cannot comply", "Only shared-parameter proposals are offered while unequal source roles are required. This limits the available proposals, not what the template interpreter could execute. No exact output is committed; the previous image is historical.", [full], prior.Ordinary, available: plans.Take(3).Select(plan => plan.Hypothesis).ToArray());
        archive.Add(new Relation(template.Surface, new Relation(sourceProvenance, new Relation(coarseWitness,
            new Relation(S(plans.Select(plan => (IElement)plan.Hypothesis.Surface).ToArray()),
                TimeStepSequence.Create(clock, clock, coarse.Surface, nativeStages.ToArray()))))));
        generationStages += stages.Count;
        return new { scenario.Id, scenario.Domain, scenario.Name, scenario.Note,
            SourceImage = "patterns/" + scenario.Id + "-source.svg", Stages = stages };

        void Add(string id, string name, string note, P.Publication[] publications, D.Policy policy,
            Relation? workingSources = null, bool alternativeOrder = false, P.Hypothesis[]? available = null)
        {
            available ??= all;
            var preference = S((alternativeOrder ? available.Skip(9).Concat(available.Take(9)) : available).Select(candidate => (IElement)candidate.Surface).ToArray());
            var context = new P.Context(S(template.Surface), S(), S(), workingSources ?? S(publications.Select(publication => publication.Source).ToArray()));
            var stage = prior.Evaluate(template, S(atom), available, publications, context, policy, preference, clock, stages.Count, previous);
            string? image = null; var coarseAnswer = "Open";
            if (stage.Execution?.Sample is { } sample)
            {
                Save(sample, output, scenario.Id + "-" + id); image = "patterns/" + scenario.Id + "-" + id + ".svg";
                coarseAnswer = new A.Receipt(coarse, sample.Observation).Answer;
                if (stage.Answer != "Yes" || coarseAnswer != "Yes")
                    throw new InvalidOperationException("A committed realization must satisfy actual output conformance and the independently communicated coarse contract.");
            }
            var shared = stage.Proposals.First(proposal => E(proposal.Hypothesis.Equalities).Length != 0).SourceAssessment;
            var equality = E(shared.InScopeConflicts).Length != 0 || E(shared.InScopeRefuted).Length != 0 && E(shared.InScopeSupport).Length != 0 ? "Open" :
                E(shared.InScopeRefuted).Length != 0 ? "No" : E(shared.InScopeSupport).Length != 0 && E(shared.InScopeOpen).Length == 0 ? "Yes" : "Open";
            if (id == "conflict" && stage.Chosen is not null || id == "capability-limited" && stage.Chosen is not null ||
                id == "reveal" && E(stage.Withdrawn).Length != 1 || id == "initial" && equality != "Open")
                throw new InvalidOperationException("A revision control failed: source uncertainty, conflict, withdrawal or capability must remain visible.");
            stages.Add(new { Id = id, Name = name, Note = note, stage.Answer,
                Selected = stage.Chosen is null ? null : plans.Single(plan => ReferenceEquals(plan.Hypothesis, stage.Chosen.Hypothesis)).Id,
                Image = image, PreviousImage = previousImage, SourceEquality = equality,
                SharedAssumed = stage.Chosen is not null && E(stage.Chosen.Hypothesis.Equalities).Length != 0,
                Carried = E(stage.Carried).Length, Withdrawn = E(stage.Withdrawn).Length,
                Eligible = stage.Proposals.Count(proposal => proposal.Eligible), Frontier = stage.Proposals.Count(proposal => proposal.Frontier),
                Proposals = stage.Proposals.Select(proposal => { var plan = plans.Single(plan => ReferenceEquals(plan.Hypothesis, proposal.Hypothesis)); return new {
                    plan.Id, plan.Name, SourceEligible = proposal.SourceAssessment.IsEligible,
                    SourceOpen = E(proposal.SourceAssessment.InScopeOpen).Length,
                    SourceRefuted = E(proposal.SourceAssessment.InScopeRefuted).Length + E(proposal.SourceAssessment.InScopeSampleRefuted).Length + E(proposal.SourceAssessment.InScopeConflicts).Length,
                    Sharing = proposal.Sharing.Answer, DefaultDecision = proposal.DefaultRead.Reads.Single().Answer,
                    proposal.Frontier, Selected = ReferenceEquals(proposal, stage.Chosen) }; }),
                Coarse = coarseAnswer, OutputConformance = stage.Execution?.Answer == "Yes" ? stage.Answer : "Open" });
            nativeStages.Add(stage.Step); previous = stage; if (image is not null) previousImage = image;
        }
    }

    private static void Save(OrnamentReconstructionCases.Sample sample, string output, string id)
    {
        OrnamentDefaultsRendering.Save(sample, Path.Combine(output, "patterns", id + ".svg"), false, showEnvelope: false);
        OrnamentDefaultsRendering.Save(sample, Path.Combine(output, "patterns", id + "-groups.svg"), true, showEnvelope: false);
    }
    private static Relation Restore(Relation surface)
    {
        var json = GojoStructureCompactJson.Serialize(GojoStructureCompactJson.FromElement("source-free-rule-pack", surface));
        if (!GojoStructureCompactJson.TryToElement(GojoStructureCompactJson.Deserialize(json), out var restored) || restored.Root is not Relation root)
            throw new InvalidOperationException("A native transfer root failed transport.");
        return root;
    }
    private static Relation S(params IElement[] entries) => StructuralConstraint.Closed(entries);
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
}
