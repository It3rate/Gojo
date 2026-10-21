using System.Diagnostics;
using System.IO.Compression;
using System.Text.Json;
using Gojo.Core;
using Gojo.Encoding.Structures;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;
using A = Gojo.Experiments.QualitativeFrieze.OrnamentArrangementRead;
using C = Gojo.Experiments.QualitativeFrieze.OrnamentParticipationCount;
using D = Gojo.Experiments.QualitativeFrieze.OrnamentDefaultsPolicy;
using G = Gojo.Experiments.QualitativeFrieze.OrnamentDefaultGeometry;
using R = Gojo.Experiments.QualitativeFrieze.OrnamentReconstruction;

namespace Gojo.Experiments.QualitativeFrieze;

/// <summary>Finite experiments in required claims, defeasible defaults and
/// deliberately unselected evidence. Fact producers supply native witnesses;
/// the policy receives their exposed receipts rather than case labels.</summary>
public static partial class OrnamentDefaultsExperiment
{
    private sealed record RuleInfo(string Id, string Label, string Note);
    private sealed record Design(string Id, string Name, string ConstructionNote, string Image,
        string GroupImage, string EnvelopeImage, string EnvelopeGroupImage, string[] Facts, int Sites, int Segments, int Groups);
    private sealed record Result(string Id, string Answer, bool Eligible, bool Frontier,
        int StrongLower, int StrongUpper, int WeakLower, int WeakUpper, string[] Reads);
    private sealed record Profile(string Id, string Name, string Note, int[] Tiers,
        bool[] Expected, int[] Waived, Result[] Results);
    private sealed record Signature(int[] Rules, bool[] Expected, string[] Accepted,
        string[] FalseAccepted, string[] FalseExcluded, int Participants);

    private static readonly RuleInfo[] RuleDescriptions = [
        new("encounters", "Explained encounters", "A shared site or declared crossing must explain contacts. Positive collinear overlap also conflicts. This is a stroke read, not filled-area overlap."),
        new("retracing", "No collinear overlap", "Narrower than the encounter rule: proper crossings may remain, but coincident positive-length intervals conflict."),
        new("breadth", "Resolvable closed contours", "Published closed mechanical contours must retain breadth at the chosen resolution. This does not detect every visible face or thin open ladder."),
        new("aspect", "Ordinary aspect", "A weak preference: the longest overall axis extent is at most four times the other in the explicitly selected equal-reach word basis."),
        new("envelope", "Inside chosen boundary", "All offered sites stay inside the explicitly selected native [-8,+8] square. This is a chosen scene boundary, not an intrinsic law of ornament."),
        new("fill", "Disjoint occupied interiors", "Open: these publications supply paths, not filled-interior ownership. Crossing-free outlines do not establish this claim."),
        new("mirror", "Selected-frame mirror", "Complete segment occupancy reflects about the vertical line through its bounding centre in the chosen word interpretation. It does not promise mechanical or joined-topology symmetry."),
        new("quarter", "Selected-frame quarter turn", "Complete segment occupancy survives the explicit cyclic axis exchange at its bounding centre, preserving multiplicity and segment resolution."),
        new("arrangement", "Reference branch arrangement", "Whole connected branch-and-route correspondence to the reference. Degree-two subdivisions may vary; headings and geometry are outside this promise."),
        new("sites", "Reference every-site topology", "Whole incidence with every site and segment retained. Still no promise of equal lengths, corners, directions or a particular embedding."),
        new("closures", "Reference closed-group participation", "A bijection to the reference inventory of published closed mechanical groups. A rosette rim is a closed group too; this is not a universal petal detector."),
        new("groups", "Reference drawing-group participation", "A bijection to all offered mechanical group rows. This asks about construction units, not the visible number of shapes.")
    ];

    public static int Run(string[] args)
    {
        var output = Path.Combine("experiments", "QualitativeFrieze", "results", "ornament-defaults");
        for (var i = 0; i < args.Length; i++)
            if (args[i] == "--output" && i + 1 < args.Length) output = args[++i];
            else throw new ArgumentException("Usage: --ornament-defaults [--output DIRECTORY]");
        Directory.CreateDirectory(Path.Combine(output, "patterns"));
        var watch = Stopwatch.StartNew(); var standard = new G.Standard(4, 8, 8);
        var archive = new List<IElement> { standard.Surface };
        var families = new List<object>(); int policyReads = 0, transported = 0, signaturesChecked = 0;
        foreach (var family in OrnamentDefaultCases.Families)
        {
            var cases = OrnamentDefaultCases.All.Where(item => item.Family == family.Id).ToArray();
            var samples = cases.Select(item => item.Create()).ToArray();
            var referenceIndex = Array.FindIndex(cases, item => item.Id == family.ReferenceId);
            var reference = samples[referenceIndex];
            var branch = A.Crystallize("Whole reference branch arrangement", reference.Observation, out var branchWitness);
            var sites = new A.Question("Reference every-site topology", reference.Observation, false);
            var closures = C.ClosedGroups(reference.MechanicalGroups, out var closureWitness);
            var groups = C.AllGroups(reference.MechanicalGroups, out var groupWitness);
            var predicates = standard.Questions.Select(question => question.Surface)
                .Concat([branch.Surface, sites.Surface, closures.Surface, groups.Surface]).ToArray();
            var rules = predicates.Select((predicate, i) => new D.Rule(RuleDescriptions[i].Label, predicate)).ToArray();
            var observations = new D.Observation[samples.Length]; var designs = new List<Design>();
            var facts = new string[samples.Length][];
            archive.Add(new Relation(branch.Surface, new Relation(sites.Surface, new Relation(closures.Surface, groups.Surface))));
            archive.Add(new Relation(branchWitness, new Relation(closureWitness, groupWitness)));
            for (var index = 0; index < samples.Length; index++)
            {
                var sample = samples[index]; var item = cases[index];
                var publication = G.Publication.FromSample(sample);
                var geometry = standard.Questions.Select(question => new G.Receipt(question, publication)).ToArray();
                var branchRead = new A.Receipt(branch, sample.Observation);
                var siteRead = new A.Receipt(sites, sample.Observation);
                var closureRead = new C.Receipt(closures, sample.MechanicalGroups);
                var groupRead = new C.Receipt(groups, sample.MechanicalGroups);
                facts[index] = geometry.Select(read => read.Answer).Concat([branchRead.Answer, siteRead.Answer, closureRead.Answer, groupRead.Answer]).ToArray();
                var witnesses = geometry.Select(read => read.Surface).Concat([branchRead.Surface, siteRead.Surface, closureRead.Surface, groupRead.Surface]).ToArray();
                observations[index] = new(publication.Port, predicates.Select((predicate, q) =>
                    D.Evidence.FromAnswer(predicate, facts[index][q], witnesses[q])).ToArray());
                archive.Add(new Relation(sample.Recipe, observations[index].Surface));
                OrnamentDefaultsRendering.Save(sample, Path.Combine(output, "patterns", item.Id + ".svg"), false, showEnvelope: false);
                OrnamentDefaultsRendering.Save(sample, Path.Combine(output, "patterns", item.Id + "-groups.svg"), true, showEnvelope: false);
                OrnamentDefaultsRendering.Save(sample, Path.Combine(output, "patterns", item.Id + "-envelope.svg"), false);
                OrnamentDefaultsRendering.Save(sample, Path.Combine(output, "patterns", item.Id + "-groups-envelope.svg"), true);
                designs.Add(new(item.Id, item.Name, item.ConstructionNote, "patterns/" + item.Id + ".svg",
                    "patterns/" + item.Id + "-groups.svg", "patterns/" + item.Id + "-envelope.svg", "patterns/" + item.Id + "-groups-envelope.svg", facts[index], E(sample.Observation.Sites).Length,
                    E(sample.Observation.Segments).Length, E(sample.MechanicalGroups).Length));
                // Replay from the native recipe alone, then independently
                // read the finer geometry publication and all family contracts.
                if (!OrnamentReconstructionCases.TryGenerate(Restore(sample.Recipe), out var replay, out var generation))
                    throw new InvalidOperationException("A native default-study recipe failed independent execution.");
                var replayPublication = G.Publication.FromSample(replay!);
                var independent = standard.Questions.Select(question => new G.Receipt(question, replayPublication).Answer)
                    .Concat([new A.Receipt(branch, replay!.Observation).Answer, new A.Receipt(sites, replay.Observation).Answer,
                        new C.Receipt(closures, replay.MechanicalGroups).Answer, new C.Receipt(groups, replay.MechanicalGroups).Answer]);
                if (!facts[index].SequenceEqual(independent)) throw new InvalidOperationException("Independent execution changed a predicate receipt.");
                transported += predicates.Length;
                if (index == referenceIndex) archive.Add(generation);
            }
            var ordinaryTiers = Tiers(strong: [0, 2, 4], weak: [3]);
            var profiles = new List<Profile>(); var interventions = new List<Profile>();
            var ordinary = MakePolicy(rules, ordinaryTiers);
            Add("free", "Leave all cues open", "No identity or default is requested. Family headings are catalogue groupings, not admission tests.", Tiers());
            Add("ordinary", "Ordinary defaults", "Explained encounters, legible closed contours and a chosen boundary are strong defaults; proportion is weak. Violations lower preference without excluding.", ordinaryTiers, policy: ordinary);
            Add("allow-crossings", "Allow crossings; resist retracing", "Only the narrower collinear-overlap check remains active. Undeclared proper crossings are permitted here.", Tiers(strong: [1, 2, 4], weak: [3]));
            Add("waive-conflicts", "Explicitly waive encounter default", "The exact ordinary encounter rule is waived. Its conflicting evidence remains visible; this does not alter the drawing or supply a missing join.", ordinaryTiers, waived: [0], policy: ordinary);
            Add("slender", "Seek elongated, unbounded forms", "Reverse the aspect expectation; leave breadth and boundary unselected. This deliberately asks for a non-default proportion rather than treating it as an error.", Tiers(strong: [0, 3]), inverted: [3]);
            Add("pin-arrangement", "Require branch arrangement", "The whole reference branch-and-route contract is now required. Geometry and mechanical construction can still differ.", Tiers(required: [8], strong: [0, 2, 4], weak: [3]));
            Add("pin-sites", "Require every-site topology", "Retain the finer site inventory too. This may distinguish contour point counts without asserting equal angles or regularity.", Tiers(required: [9], strong: [0, 2, 4], weak: [3]));
            Add("pin-closed-units", "Require closed construction units", "Select the reference inventory of published closure promises. For a hub and rim this includes the rim; it does not infer petals from appearance.", Tiers(required: [10], strong: [0, 2, 4], weak: [3]));
            Add("prefer-quarter", "Prefer quarter-turn symmetry", "Add selected-frame quarter-turn occupancy as a strong default, without requiring it or inferring mechanical symmetry.", Tiers(strong: [0, 2, 4, 7], weak: [3]));
            Add("strict-clean", "Require encounters and boundary", "The same two cues now exclude known violations. Explicit default exceptions cannot waive a requirement.", Tiers(required: [0, 4], strong: [2], weak: [3]));
            Add("require-fill", "Require filled-interior evidence", "A missing supplier leaves every candidate unresolved. No outline calculation is substituted for occupied-interior ownership.", Tiers(required: [5], strong: [0, 2, 4], weak: [3]));
            for (var q = 0; q < rules.Length; q++)
            {
                foreach (var tier in new[] { 0, 1, 3 })
                {
                    if (ordinaryTiers[q] == tier) continue;
                    var modified = (int[])ordinaryTiers.Clone(); modified[q] = tier;
                    Add("change-" + q + "-" + tier, RuleDescriptions[q].Label + " → " + new[] { "required", "strong default", "weak preference", "unused" }[tier],
                        "One authority change from ordinary defaults; all evidence is retained.", modified, destination: interventions);
                }
                if (ordinaryTiers[q] is 1 or 2)
                    Add("waive-" + q, "Waive: " + RuleDescriptions[q].Label,
                        "One exact-rule exception from ordinary defaults. The predicate answer is unchanged.", ordinaryTiers,
                        waived: [q], policy: ordinary, destination: interventions);
            }
            var goals = Enumerable.Range(0, cases.Length).Where(i => facts[i][8] == "Yes").ToArray();
            var signatures = Describe(goals, facts, referenceIndex, cases.Select(item => item.Id).ToArray(), rules,
                observations, archive, ref signaturesChecked, ref policyReads);
            families.Add(new { family.Id, family.Name, family.ReferenceId, family.RepertoireNote, Cases = designs,
                Profiles = profiles, Interventions = interventions, Signatures = signatures });
            Console.WriteLine($"Defaults {family.Id}: {cases.Length} drawings; {goals.Length} reference-arrangement matches; {policyReads:N0} policy reads.");

            void Add(string id, string name, string note, int[] tiers, int[]? inverted = null, int[]? waived = null,
                D.Policy? policy = null, List<Profile>? destination = null)
            {
                var actualRules = rules.Select((rule, i) => inverted?.Contains(i) == true ? new D.Rule(rule.Name, rule.Predicate, false) : rule).ToArray();
                policy ??= MakePolicy(actualRules, tiers);
                var directive = new D.Directive(policy, (waived ?? []).Select(i => actualRules[i]).ToArray());
                var reads = observations.Select(observation => new D.Receipt(policy, observation, directive)).ToArray();
                var results = reads.Select((read, i) => new Result(cases[i].Id, read.Answer, read.Eligible,
                    read.Eligible && !reads.Any(other => other.Eligible && D.Receipt.ComparePreference(other, read) == -1),
                    read.StrongRange.Lower, read.StrongRange.Upper, read.PreferenceRange.Lower, read.PreferenceRange.Upper,
                    predicates.Select(predicate => read.Reads.Single(row => ReferenceEquals(row.Rule.Predicate, predicate)).Answer).ToArray())).ToArray();
                (destination ?? profiles).Add(new(id, name, note, tiers, actualRules.Select(rule => rule.Expected).ToArray(), waived ?? [], results));
                policyReads += reads.Length;
                archive.Add(new Relation(policy.Surface, new Relation(directive.Surface, S(reads.Select(read => (IElement)read.Surface).ToArray()))));
            }
        }
        watch.Stop();
        var model = new { Schema = "ornament-defaults-v1", Counts = new { Designs = OrnamentDefaultCases.All.Count,
                Families = families.Count, Rules = RuleDescriptions.Length, PolicyReads = policyReads, TransportAnswers = transported,
                SignaturesChecked = signaturesChecked, Seconds = watch.Elapsed.TotalSeconds },
            Standards = new { AspectLimit = 4, ResolutionLimit = 8, EnvelopeHalfSpan = 8 }, Rules = RuleDescriptions,
            Families = families, Limits = new[] {
                "These are authored defaults over a finite corpus, not learned aesthetic laws or calibrated probabilities. Preference is a partial ordinal ordering; multiple alternatives can remain equally preferred or incomparable.",
                "Family names organize the catalogue. No family-membership test or structural identity is silently required by the ordinary profile. Select a native topology or participation contract when it matters.",
                "The optional geometry supplier interprets uncalibrated native position words under an explicit equal-axis-reach basis and repeated comparison standards. The policy receives categorical receipts, not pixels or renderer coordinates.",
                "The chosen boundary is an explicit local scene convention. Intentional continuation beyond a crop may be correct in another context; the free and exception policies demonstrate this distinction.",
                "Encounter and collinear-overlap reads concern strokes. Filled-interior overlap remains Open without occupied-region ownership; no claim of valid tessellation follows from these checks.",
                "Contour breadth uses declared closed mechanical groups. It is not a detector of all enclosed visual faces, all close open lines, or every kind of visual clutter.",
                "Mirror and quarter-turn receipts concern complete segment occupancy at the published word resolution, not symmetry of joined topology, mechanical grouping or arbitrary affine variants.",
                "Closed-group participation counts published end-to-start closure promises. A rim and petals all contribute; equivalent silhouettes made from open sides can have a different construction inventory.",
                "Unused evidence is deliberately outside the question; waived evidence remains a witnessed conflict with an explicit exception. Neither means the fact became false or disappeared.",
                "Minimal signatures are exhaustive only over the ten selected descriptor predicates and each family's twelve drawings. They do not prove a universal definition or minimum encoding. All evidence is prepared eagerly for this study; no short-circuit speed claim is made." }
        };
        var json = JsonSerializer.Serialize(model, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        File.WriteAllText(Path.Combine(output, "diagnostics.json"), json);
        File.WriteAllText(Path.Combine(output, "data.js"), "globalThis.ORNAMENT_DEFAULTS=" + json + ";");
        File.WriteAllText(Path.Combine(output, "gallery.html"), Html());
        File.WriteAllText(Path.Combine(output, "overview.html"), Overview());
        var native = GojoStructureCompactJson.Serialize(GojoStructureCompactJson.FromElement("ornament-defaults-v1", S(archive.ToArray())));
        using (var file = File.Create(Path.Combine(output, "defaults.gojo.json.gz")))
        using (var zip = new GZipStream(file, CompressionLevel.SmallestSize))
        using (var writer = new StreamWriter(zip)) writer.Write(native);
        Console.WriteLine($"Completed {policyReads:N0} native policy reads and {signaturesChecked:N0} finite descriptor subsets in {watch.Elapsed.TotalSeconds:0.0}s.");
        Console.WriteLine(Path.GetFullPath(output)); return 0;
    }

    private static object Describe(int[] goals, string[][] facts, int referenceIndex, string[] ids,
        D.Rule[] rules, D.Observation[] observations, List<IElement> archive, ref int checkedCount, ref int policyReads)
    {
        // The direct branch question is the target of this description task,
        // not an allowed descriptor. Missing filled-interior evidence cannot
        // discriminate. Polarity comes from the actual reference read.
        var basis = new[] { 0, 1, 2, 3, 4, 6, 7, 9, 10, 11 }.Where(q => facts[referenceIndex][q] is "Yes" or "No").ToArray();
        var subsets = new List<(int[] Rules, int[] Accepted, int FalseAccepted, int FalseExcluded)>();
        for (var mask = 0; mask < 1 << basis.Length; mask++)
        {
            var selected = basis.Where((_, bit) => (mask & (1 << bit)) != 0).ToArray();
            var accepted = Enumerable.Range(0, ids.Length).Where(i => selected.All(q =>
                facts[i][q] == "Open" || facts[i][q] == facts[referenceIndex][q])).ToArray();
            subsets.Add((selected, accepted, accepted.Count(i => !goals.Contains(i)), goals.Count(i => !accepted.Contains(i))));
        }
        checkedCount += subsets.Count;
        var nondominated = subsets.Where(a => !subsets.Any(b =>
            b.FalseAccepted <= a.FalseAccepted && b.FalseExcluded <= a.FalseExcluded && b.Rules.Length <= a.Rules.Length &&
            (b.FalseAccepted < a.FalseAccepted || b.FalseExcluded < a.FalseExcluded || b.Rules.Length < a.Rules.Length)))
            .GroupBy(a => (a.FalseAccepted, a.FalseExcluded, a.Rules.Length))
            .Select(group => group.OrderBy(item => string.Join(",", item.Rules.Select(q => q.ToString("D2")))).First())
            .OrderBy(item => item.FalseExcluded).ThenBy(item => item.FalseAccepted).ThenBy(item => item.Rules.Length).Take(10).ToArray();
        var solutions = new List<Signature>();
        foreach (var item in nondominated)
        {
            var selectedRules = item.Rules.Select(q => new D.Rule(rules[q].Name, rules[q].Predicate, facts[referenceIndex][q] == "Yes")).ToArray();
            var policy = new D.Policy(required: selectedRules);
            var reads = observations.Select(observation => new D.Receipt(policy, observation)).ToArray();
            var admitted = Enumerable.Range(0, ids.Length).Where(i => reads[i].Eligible).ToArray();
            if (!admitted.SequenceEqual(item.Accepted)) throw new InvalidOperationException("Finite signature search disagreed with its native policy execution.");
            archive.Add(new Relation(policy.Surface, S(reads.Select(read => (IElement)read.Surface).ToArray())));
            policyReads += reads.Length;
            solutions.Add(new(item.Rules, item.Rules.Select(q => facts[referenceIndex][q] == "Yes").ToArray(),
                admitted.Select(i => ids[i]).ToArray(), admitted.Where(i => !goals.Contains(i)).Select(i => ids[i]).ToArray(),
                goals.Where(i => !admitted.Contains(i)).Select(i => ids[i]).ToArray(), R.Participants(policy.Surface).Count));
        }
        return new { GoalIds = goals.Select(i => ids[i]).ToArray(), ExactPossible = subsets.Any(item => item.FalseAccepted == 0 && item.FalseExcluded == 0),
            Checked = subsets.Count, Solutions = solutions,
            Note = "Describe the whole reference-branch-compatible set using other cue predicates. Each commitment takes the reference's observed polarity. Shown descriptions trade false inclusions, lost valid alternatives and number of rules without a weighted score; alternatives with the same error counts and rule count are collapsed. Native participant cost is reported separately and is not minimized; it counts the connected required-policy graph, including predicate standards, not bits or a learned description length." };
    }

    private static int[] Tiers(int[]? required = null, int[]? strong = null, int[]? weak = null) =>
        Enumerable.Range(0, RuleDescriptions.Length).Select(i => required?.Contains(i) == true ? 0 : strong?.Contains(i) == true ? 1 : weak?.Contains(i) == true ? 2 : 3).ToArray();
    private static D.Policy MakePolicy(D.Rule[] rules, int[] tiers) => new(
        required: rules.Where((_, i) => tiers[i] == 0).ToArray(), strongDefaults: rules.Where((_, i) => tiers[i] == 1).ToArray(),
        preferences: rules.Where((_, i) => tiers[i] == 2).ToArray(), unused: rules.Where((_, i) => tiers[i] == 3).ToArray());
    private static Relation Restore(Relation element)
    {
        var text = GojoStructureCompactJson.Serialize(GojoStructureCompactJson.FromElement("native-default-case", element));
        if (!GojoStructureCompactJson.TryToElement(GojoStructureCompactJson.Deserialize(text), out var imported) || imported.Root is not Relation root)
            throw new InvalidOperationException("Native default case did not survive transport.");
        return root;
    }
    private static Relation S(params IElement[] items) => StructuralConstraint.Closed(items);
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
}
