using System.Diagnostics;
using System.Numerics;
using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;
using R = Gojo.Experiments.QualitativeFrieze.OrnamentReconstruction;

namespace Gojo.Experiments.QualitativeFrieze;

/// <summary>Large finite reconstruction and description-ablation study. The
/// sender's goals enter only the evaluator/search baseline. Receiver filtering
/// operates on actual published clauses and separately advertised capabilities.
/// The compiled backend projects native immutable observation/query receipts.</summary>
public sealed class OrnamentReconstructionStudy
{
    public sealed record Options(int MaxCandidates = 0, int MaxTargets = 0, int PoolSize = 10,
        int ProposalCapacity = 4096, int GraphCapacity = 20000, int SnapshotCases = 8);
    public sealed record Goal(string Id, string Name, R.Focus Focus, int[] QueryIndices);
    public sealed record Trial(string Source, string Family, string Goal, string Engine, string Context,
        string Strategy, int Hints, int NativeCost, int Admitted, int Possible, int SafeAdmitted, bool Robust,
        string? Selected, bool? SelectedGoal, int GoalUnknown, int OutputUnknown, string Incidence,
        string Relations, int OutsideFocusMismatches, int Enumerated, double SearchMilliseconds,
        int[] QueryIndices, string[] ClaimAnswers);
    public sealed record RawTrial(string Source, string Family, string Scope, int NativeCost, int Admitted, int Open, int Blocked,
        string? Selected, string Incidence, string Relations, bool? MechanicalRead, int Attempts);
    public sealed record SourceInfo(string Id, string Family, string Name, string Reference, string Image,
        string GroupImage, string Answers, int Sites, int Edges, int Groups, int SourceUnknown);
    public sealed record CandidateInfo(string Id, string Family, string Image, string GroupImage,
        string Answers, int Sites, int Edges, int Groups, int ProgramParticipants);
    public sealed record Replay(string Id, OrnamentReconstructionCases.Sample Output, R.Input Input,
        R.Bank Bank, string[] Answers, Relation Surface);
    public sealed record CacheProbe(int Reads, bool Equivalent, double FreshMilliseconds,
        double CachedMilliseconds, long FreshInductions, long CacheHits);

    public Options Configuration { get; }
    public IReadOnlyList<OrnamentReconstructionVocabulary.Entry> Vocabulary { get; }
    public IReadOnlyList<Goal> Goals { get; }
    public IReadOnlyList<CandidateInfo> Candidates { get; }
    public IReadOnlyList<SourceInfo> Sources { get; }
    public IReadOnlyList<Trial> Trials { get; }
    public IReadOnlyList<RawTrial> RawTrials { get; }
    public CacheProbe Cache { get; }
    public Relation Snapshot { get; }
    public long NativeReads { get; }
    public long CachedReads { get; }
    public int FreshOutputs { get; }
    public long Subsets { get; }
    public double PreparationMilliseconds { get; }
    public double EvaluationMilliseconds { get; }

    private OrnamentReconstructionStudy(Options options, IReadOnlyList<OrnamentReconstructionVocabulary.Entry> vocabulary,
        IReadOnlyList<Goal> goals, IReadOnlyList<CandidateInfo> candidates, IReadOnlyList<SourceInfo> sources,
        IReadOnlyList<Trial> trials, IReadOnlyList<RawTrial> rawTrials, CacheProbe cache,
        Relation snapshot, long nativeReads, long cachedReads, int freshOutputs, long subsets,
        double preparationMilliseconds, double evaluationMilliseconds)
    {
        Configuration = options; Vocabulary = vocabulary; Goals = goals; Candidates = candidates; Sources = sources;
        Trials = trials; RawTrials = rawTrials; Cache = cache; Snapshot = snapshot;
        NativeReads = nativeReads; CachedReads = cachedReads; FreshOutputs = freshOutputs; Subsets = subsets;
        PreparationMilliseconds = preparationMilliseconds; EvaluationMilliseconds = evaluationMilliseconds;
    }

    public static OrnamentReconstructionStudy Run(Options options, string output)
    {
        Directory.CreateDirectory(Path.Combine(output, "patterns"));
        var prep = Stopwatch.StartNew();
        var vocabulary = OrnamentReconstructionVocabulary.Build(); var queries = vocabulary.Select(entry => entry.Query).ToArray();
        var specs = OrnamentReconstructionCases.Specs.OrderBy(spec => spec.Transform)
            .ThenByDescending(spec => spec.ReachRecipe).ThenByDescending(spec => spec.Repeats)
            .Take(options.MaxCandidates > 0 ? options.MaxCandidates : int.MaxValue).ToArray();
        var samples = specs.Select(spec => spec.Create()).ToArray();
        var inputs = samples.Select(sample => new R.Input(sample.Spec.Id, sample.Observation, sample.MechanicalGroups)).ToArray();
        var byInput = inputs.Select((input, index) => (input, index)).ToDictionary(pair => pair.input, pair => pair.index);
        var configuration = new R.Options(6, options.ProposalCapacity, 3, options.ProposalCapacity, 256, 512);
        var bank = new R.Bank(inputs, queries, configuration);
        var answers = new string[inputs.Length][];
        Console.WriteLine($"Preparing {inputs.Length} executable candidates and {queries.Length} native query ports.");
        for (var index = 0; index < inputs.Length; index++)
        {
            answers[index] = queries.Select(query => bank.Read(inputs[index], query).Answer).ToArray();
            if (index % 16 == 0) Console.WriteLine($"Prepared {index + 1}/{inputs.Length}; native reads {bank.EvaluatedReads:N0}.");
        }
        var candidates = samples.Select((sample, index) =>
        {
            var image = "patterns/" + sample.Spec.Id + ".svg"; var groupImage = "patterns/" + sample.Spec.Id + "-groups.svg";
            OrnamentReconstructionRendering.Save(sample, Path.Combine(output, image), false);
            OrnamentReconstructionRendering.Save(sample, Path.Combine(output, groupImage), true);
            return new CandidateInfo(sample.Spec.Id, sample.Spec.Family, image, groupImage, Codes(answers[index]),
                E(sample.Observation.Sites).Length, E(sample.Observation.Segments).Length, E(sample.MechanicalGroups).Length,
                OrnamentReconstructionPacket.Participants(sample.Recipe).Count);
        }).ToArray();
        var goalSpecs = new[] {
            ("motifs", "Local forms", new[] { "visible motif" }),
            ("relations", "Connections and encounters", new[] { "joint occurrence", "crossing", "continuation" }),
            ("mechanics", "Drawing construction", new[] { "mechanical path" }),
            ("combined", "All advertised distinctions", new[] { "visible motif", "joint occurrence", "crossing", "continuation", "mechanical path" }) };
        var goals = goalSpecs.Select(item =>
        {
            var indices = Enumerable.Range(0, queries.Length).Where(index => item.Item3.Contains(queries[index].Kind)).ToArray();
            return new Goal(item.Item1, item.Item2, new R.Focus(queries, indices.Select(index => queries[index]).ToArray()), indices);
        }).ToArray();
        var order = Enumerable.Range(0, inputs.Length).OrderBy(index => candidates[index].Edges)
            .ThenBy(index => candidates[index].Groups).ThenBy(index => index).ToArray();
        var profileSpecs = new[] {
            ("motif", new[] { "visible motif" }),
            ("relational", new[] { "visible motif", "joint occurrence", "crossing", "continuation" }),
            ("mechanical", new[] { "visible motif", "joint occurrence", "crossing", "continuation", "mechanical path" }) };
        var contexts = profileSpecs.Select(profile =>
        {
            var available = queries.Where(query => profile.Item2.Contains(query.Kind)).ToArray();
            var context = new R.Context(order.Select(index => inputs[index]).ToArray(), available);
            return (Id: profile.Item1, Context: context, Projection: new OrnamentReconstructionProjection(bank, context, queries));
        }).ToArray();
        var probe = Probe(bank, inputs, queries);
        prep.Stop(); var evaluate = Stopwatch.StartNew();
        var targetSpecs = specs.Take(options.MaxTargets > 0 ? options.MaxTargets : int.MaxValue).ToArray();
        var trials = new List<Trial>(); var rawTrials = new List<RawTrial>(); var sources = new List<SourceInfo>();
        var replays = new Dictionary<int, Replay>(); var snapshots = new List<IElement>(); long subsets = 0;
        var nativeFullFocus = new R.Focus(queries, queries);
        foreach (var targetSpec in targetSpecs)
        {
            // The sender's actual observation and proofs remain outside the
            // receiver bank and every transmitted message.
            var source = targetSpec.Create(); var sourceInput = new R.Input("sender observation", source.Observation, source.MechanicalGroups);
            var sourceBank = new R.Bank([sourceInput], queries, configuration);
            var truth = queries.Select(query => sourceBank.Read(sourceInput, query).Answer).ToArray();
            var sourceImage = "patterns/source-" + targetSpec.Id + ".svg";
            var sourceGroups = "patterns/source-" + targetSpec.Id + "-groups.svg";
            OrnamentReconstructionRendering.Save(source, Path.Combine(output, sourceImage), false);
            OrnamentReconstructionRendering.Save(source, Path.Combine(output, sourceGroups), true);
            sources.Add(new(targetSpec.Id, targetSpec.Family, targetSpec.Name, targetSpec.SourceReference, sourceImage, sourceGroups,
                Codes(truth), E(source.Observation.Sites).Length, E(source.Observation.Segments).Length,
                E(source.MechanicalGroups).Length, truth.Count(answer => answer is "Open" or "Blocked")));
            var graphReads = new Dictionary<int, (OrnamentReconstructionGraphRead Incidence, OrnamentReconstructionGraphRead Relations)>();
            foreach (var context in contexts)
            {
                var projection = context.Projection;
                var poolIndices = Pool(queries, truth, projection, options.PoolSize);
                var pool = poolIndices.Select(index => new R.Claim(queries[index], truth[index])).ToArray();
                var filters = pool.Select(projection.Mask).ToArray();
                var possibleFilters = pool.Select(projection.PossibleMask).ToArray();
                foreach (var goal in goals)
                {
                    var goalUnknown = goal.QueryIndices.Count(index => truth[index] is "Open" or "Blocked");
                    var profiles = projection.Inputs.Select(input => Profile(answers[byInput[input]], goal.QueryIndices)).ToArray();
                    var goalMask = BigInteger.Zero;
                    for (var index = 0; index < projection.Inputs.Count; index++)
                        if (goalUnknown == 0 && goal.QueryIndices.All(q => answers[byInput[projection.Inputs[index]]][q] == truth[q]))
                            goalMask |= BigInteger.One << index;
                    foreach (var preference in new[] { "compact", "reverse", "withheld family" })
                    {
                        var universe = projection.Universe;
                        if (preference == "withheld family")
                            for (var index = 0; index < projection.Inputs.Count; index++)
                                if (samples[byInput[projection.Inputs[index]]].Spec.Family == targetSpec.Family)
                                    universe &= ~(BigInteger.One << index);
                        var search = OrnamentReconstructionSearch.Run(pool, filters, possibleFilters, universe, goalMask, profiles);
                        subsets += search.Enumerated;
                        var all = (1 << pool.Length) - 1;
                        int KindMask(params string[] kinds) => pool.Select((claim, index) => (claim, index))
                            .Where(pair => kinds.Contains(pair.claim.Query.Kind)).Aggregate(0, (mask, pair) => mask | 1 << pair.index);
                        var choices = new[] {
                            ("empty", 0), ("visible", KindMask("visible motif")),
                            ("relational", KindMask("joint occurrence", "crossing", "continuation")),
                            ("mechanical", KindMask("mechanical path")), ("mixed", all),
                            ("greedy", search.GreedyMask), ("minimum", search.Best.Mask) };
                        foreach (var choice in choices)
                        {
                            var option = search.Options[choice.Item2];
                            var admitted = projection.Indices(option.Admitted);
                            var selectedIndex = admitted.Count == 0 ? -1 : preference == "reverse" ? admitted[^1] : admitted[0];
                            var selected = selectedIndex < 0 ? -1 : byInput[projection.Inputs[selectedIndex]];
                            Replay? replay = selected < 0 ? null : Fresh(selected);
                            var outputUnknown = replay is null ? goal.QueryIndices.Length :
                                goal.QueryIndices.Count(index => replay.Answers[index] is "Open" or "Blocked");
                            bool? goalRead = replay is null || goalUnknown != 0 || outputUnknown != 0 ? null :
                                goal.QueryIndices.All(index => replay.Answers[index] == truth[index]);
                            var graph = selected < 0 ? default : Graph(selected);
                            var outside = replay is null ? 0 : Enumerable.Range(0, queries.Length).Count(index =>
                                !goal.QueryIndices.Contains(index) && truth[index] is "Yes" or "No" &&
                                replay.Answers[index] is "Yes" or "No" && truth[index] != replay.Answers[index]);
                            var sentIndices = poolIndices.Where((_, index) => (choice.Item2 & 1 << index) != 0).ToArray();
                            trials.Add(new(targetSpec.Id, targetSpec.Family, goal.Id, context.Id, preference, choice.Item1,
                                option.Hints, option.NativeCost, admitted.Count, OrnamentReconstructionSearch.Count(option.Possible), OrnamentReconstructionSearch.Count(option.Admitted & goalMask),
                                option.Robust, selected < 0 ? null : candidates[selected].Id, goalRead, goalUnknown, outputUnknown,
                                selected < 0 ? "Open" : graph.Incidence.Answer, selected < 0 ? "Open" : graph.Relations.Answer,
                                outside, choice.Item1 == "minimum" ? search.Enumerated : 0, search.Milliseconds,
                                sentIndices, sentIndices.Select(index => truth[index]).ToArray()));
                            if (snapshots.Count < options.SnapshotCases && targetSpec.Transform == 0 && context.Id == "mechanical" && preference == "compact" &&
                                goal.Id == "combined" && choice.Item1 == "minimum" && replay is not null)
                            {
                                var message = OrnamentReconstructionSearch.Message(pool, choice.Item2);
                                if (message.NativeCost != option.NativeCost) throw new InvalidOperationException("Projected cost must equal actual native message cost.");
                                var native = bank.Evaluate(message, context.Context, nativeFullFocus);
                                if (!native.Admissible.Select(item => item.Input).SequenceEqual(admitted.Select(index => projection.Inputs[index])))
                                    throw new InvalidOperationException("Compiled subset filtering must preserve actual native admissions.");
                                var selectedProof = message.Claims.Select(claim => (IElement)bank.Read(inputs[selected], claim.Query).Surface).ToArray();
                                var judged = goal.QueryIndices.Select(index => (IElement)replay.Bank.Read(replay.Input, queries[index]).Surface).ToArray();
                                snapshots.Add(new Relation(new Relation(source.Surface, goal.Focus.Surface),
                                    new Relation(message.Surface, new Relation(context.Context.Surface,
                                        new Relation(S(selectedProof), new Relation(replay.Surface,
                                            new Relation(S(judged), new Relation(graph.Incidence.Surface, graph.Relations.Surface))))))));
                            }
                        }
                    }
                }
            }
            // A complete source-free observation is the strong communication
            // baseline. It still supplies no author-path partition.
            foreach (var withRelations in new[] { false, true })
            {
                var packet = new OrnamentReconstructionPacket(source.Observation, withRelations);
                var matches = order.Select(index => (Index: index, Read: new OrnamentReconstructionGraphRead(packet.Observation,
                    inputs[index].Observation, options.GraphCapacity, withRelations))).ToArray();
                var eligible = matches.Where(item => item.Read.Answer == "Yes").ToArray();
                var selected = eligible.Length == 0 ? -1 : eligible[0].Index;
                var replay = selected < 0 ? null : Fresh(selected); var graph = selected < 0 ? default : Graph(selected);
                var mechanical = goals.Single(goal => goal.Id == "mechanics").QueryIndices;
                bool? read = replay is null || mechanical.Any(index => truth[index] is "Open" or "Blocked" || replay.Answers[index] is "Open" or "Blocked")
                    ? null : mechanical.All(index => truth[index] == replay.Answers[index]);
                rawTrials.Add(new(targetSpec.Id, targetSpec.Family, withRelations ? "full published relations" : "incidence and headings",
                    OrnamentReconstructionPacket.Participants(packet.Surface).Count, eligible.Length,
                    matches.Count(item => item.Read.Answer == "Open"), matches.Count(item => item.Read.Answer == "Blocked"),
                    selected < 0 ? null : candidates[selected].Id, selected < 0 ? "Open" : graph.Incidence.Answer,
                    selected < 0 ? "Open" : graph.Relations.Answer, read, matches.Sum(item => item.Read.Attempts)));
            }
            Console.WriteLine($"Evaluated {sources.Count}/{targetSpecs.Length} sources; {trials.Count:N0} decisions; {subsets:N0} subsets; {replays.Count} fresh output events.");

            (OrnamentReconstructionGraphRead Incidence, OrnamentReconstructionGraphRead Relations) Graph(int selected)
            {
                if (!graphReads.TryGetValue(selected, out var read))
                {
                    var replay = Fresh(selected);
                    graphReads.Add(selected, read = (new(source.Observation, replay.Output.Observation, options.GraphCapacity, false),
                        new(source.Observation, replay.Output.Observation, options.GraphCapacity, true)));
                }
                return read;
            }
        }
        evaluate.Stop();
        var providers = S(samples.Select((sample, index) => (IElement)new Relation(inputs[index].Surface, sample.Recipe)).ToArray());
        var snapshot = new Relation(new Relation(bank.Surface, providers), S(snapshots.ToArray()));
        return new(options, vocabulary, goals, candidates, sources, trials, rawTrials, probe, snapshot,
            bank.EvaluatedReads, bank.CachedReads, replays.Count, subsets, prep.Elapsed.TotalMilliseconds, evaluate.Elapsed.TotalMilliseconds);

        Replay Fresh(int index)
        {
            if (replays.TryGetValue(index, out var existing)) return existing;
            var generated = samples[index].Replay();
            if (ReferenceEquals(generated.Observation.Surface, samples[index].Observation.Surface))
                throw new InvalidOperationException("Reconstruction needs a fresh output observation event.");
            var input = new R.Input("fresh receiver output", generated.Observation, generated.MechanicalGroups);
            var critic = new R.Bank([input], queries, configuration);
            var actual = queries.Select(query => critic.Read(input, query).Answer).ToArray();
            if (!actual.SequenceEqual(answers[index])) throw new InvalidOperationException("Fresh native execution changed its advertised answer profile.");
            var result = new Replay(candidates[index].Id, generated, input, critic, actual,
                new Relation(new Relation(inputs[index].Surface, samples[index].Recipe),
                    new Relation(generated.Surface, S(queries.Select(query => (IElement)critic.Read(input, query).Surface).ToArray()))));
            replays.Add(index, result); return result;
        }
    }

    private static int[] Pool(R.Query[] queries, string[] truth, OrnamentReconstructionProjection projection, int maximum)
    {
        var scored = Enumerable.Range(0, queries.Length).Where(index => truth[index] is "Yes" or "No")
            .Where(index => projection.Context.Available.Any(query => ReferenceEquals(query.Surface, queries[index].Surface)))
            .Select(index => new { Index = index, Kind = queries[index].Kind,
                Gain = projection.Inputs.Count - OrnamentReconstructionSearch.Count(projection.PossibleMask(new(queries[index], truth[index]))),
                Cost = OrnamentReconstructionPacket.Participants(new R.Claim(queries[index], truth[index]).Surface).Count })
            .Where(item => item.Gain > 0).OrderByDescending(item => (double)item.Gain / item.Cost).ThenBy(item => item.Index).ToArray();
        var chosen = scored.GroupBy(item => item.Kind).SelectMany(group => group.Take(2)).Select(item => item.Index).Take(maximum).ToList();
        foreach (var item in scored) if (chosen.Count < maximum && !chosen.Contains(item.Index)) chosen.Add(item.Index);
        return chosen.Order().ToArray();
    }

    private static CacheProbe Probe(R.Bank bank, R.Input[] inputs, R.Query[] queries)
    {
        var indices = Enumerable.Range(0, Math.Min(8, inputs.Length)).Select(index => index * inputs.Length / Math.Min(8, inputs.Length)).ToArray();
        var ports = new[] { 0, queries.Length / 3, queries.Length / 2, queries.Length - 1 }.Distinct().ToArray();
        var expected = indices.SelectMany(index => ports.Select(port => bank.Read(inputs[index], queries[port]).Answer)).ToArray();
        var before = bank.Inductions; var fresh = Stopwatch.StartNew();
        var actual = indices.SelectMany(index => ports.Select(port => bank.Read(inputs[index], queries[port], false).Answer)).ToArray(); fresh.Stop();
        var freshInductions = bank.Inductions - before; var hits = bank.CachedReads; var cached = Stopwatch.StartNew();
        var retained = indices.SelectMany(index => ports.Select(port => bank.Read(inputs[index], queries[port]).Answer)).ToArray(); cached.Stop();
        if (!expected.SequenceEqual(actual) || !expected.SequenceEqual(retained)) throw new InvalidOperationException("Caching cannot change native verdicts.");
        return new(actual.Length, true, fresh.Elapsed.TotalMilliseconds, cached.Elapsed.TotalMilliseconds, freshInductions, bank.CachedReads - hits);
    }
    private static string Codes(IEnumerable<string> answers) => string.Concat(answers.Select(answer => answer == "Yes" ? '+' : answer == "No" ? '-' : answer == "Blocked" ? '!' : '?'));
    private static string Profile(string[] answers, int[] indices) => Codes(indices.Select(index => answers[index]));
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
    private static Relation S(params IElement[] entries) => StructuralConstraint.Closed(entries);
}
