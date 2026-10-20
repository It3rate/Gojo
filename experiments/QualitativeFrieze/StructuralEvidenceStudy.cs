using Gojo.Core;
using Gojo.Experiments.QualitativeExecution;
using Gojo.Patterns;

namespace Gojo.Experiments.QualitativeFrieze;

/// <summary>Finite topology/heading cases ask whether a question-selection
/// policy learned on span evidence transfers unchanged. Generation, native
/// matching and evaluator-only source comparison remain separate stages.</summary>
public sealed class StructuralEvidenceStudy
{
    // step = receiver-before -> (public feature read ->
    //        (authorized reply -> receiver-after))
    // episode = public acquisition timeline ->
    //           (independent source evaluation -> separate supplier audit)
    public sealed record Step(StructuralEvidenceInquiry.Query Query,
        StructuralEvidenceInquiry.FeatureRead Features, double Prediction,
        StructuralEvidenceInquiry.Response Response, StructuralEvidenceInquiry.Assessment Before,
        StructuralEvidenceInquiry.Assessment After, Relation Surface);
    public sealed record Episode(string Id, StructuralEvidenceCases.Case Case, string Policy, string? Control,
        StructuralEvidenceInquiry.Receiver Receiver, StructuralEvidenceInquiry.Assessment Initial,
        IReadOnlyList<Step> Steps, StructuralEvidenceInquiry.Assessment Final,
        IReadOnlyList<StructuralEvidenceInquiry.ObservationComparison> Evaluations,
        bool CorrectAtVocabulary, bool CorrectAmbiguity, Relation SupplierAudit, Relation Surface)
    {
        public StructuralEvidenceCases.Sample Source => Case.Source;
        public bool HasConflict => E(Final.Conflicts).Length != 0;
        public bool HasBlocked => Final.Candidates.Any(candidate => E(candidate.Blocked).Length != 0);
    }
    public sealed record Summary(string Policy, int Episodes, int Resolved, int Open,
        int Conflicted, int Blocked, int Requests)
    { public double MeanRequests => Episodes == 0 ? 0 : (double)Requests / Episodes; }

    public StructuralEvidenceCases Cases { get; }
    public EvidenceSelectionStudy.LearnedPolicy Policy { get; }
    public IReadOnlyList<Episode> Episodes { get; }
    public IReadOnlyList<Summary> Summaries { get; }
    public Relation Surface { get; }

    private StructuralEvidenceStudy(StructuralEvidenceCases cases, EvidenceSelectionStudy.LearnedPolicy policy,
        IReadOnlyList<Episode> episodes)
    {
        Cases = cases; Policy = policy; Episodes = episodes;
        Summaries = episodes.Where(episode => episode.Control is null).GroupBy(episode => episode.Policy)
            .Select(group => new Summary(group.Key, group.Count(), group.Count(episode => episode.CorrectAtVocabulary),
                group.Count(episode => !episode.CorrectAtVocabulary && !episode.HasConflict && !episode.HasBlocked),
                group.Count(episode => episode.HasConflict), group.Count(episode => episode.HasBlocked),
                group.Sum(episode => episode.Steps.Count))).ToArray();
        Surface = new(policy.Surface, new Relation(cases.Surface,
            S(episodes.Select(episode => (IElement)episode.Surface).ToArray())));
    }

    public static StructuralEvidenceStudy Build()
    {
        var cases = StructuralEvidenceCases.Build();
        // Exactly the earlier span-training recipe, seed and request budget.
        // Its unrelated evaluation episodes do not enter these coefficients.
        var learned = EvidenceSelectionStudy.Build(new(48, 1, 731, 4, 0)).Policy;
        var episodes = new List<Episode>();
        var evaluations = cases.Cases.ToDictionary(example => example,
            example => (IReadOnlyList<StructuralEvidenceInquiry.ObservationComparison>)example.Candidates.Select(candidate =>
                StructuralEvidenceInquiry.CompareObservations(example.Source.Induction, candidate.Induction)).ToArray());
        foreach (var policy in new[] { "fixed", "random", "disagreement", "transferred" })
        for (var index = 0; index < cases.Cases.Count; index++)
        {
            var example = cases.Cases[index]; var queries = Questions(cases, example);
            var receiver = Receiver(example, queries); var supplier = new StructuralEvidenceInquiry.Supplier(example.Source.Observation, queries);
            episodes.Add(Run(example, receiver, supplier, policy, learned, evaluations[example], new Random(731 + index)));
        }

        var first = cases.Cases[0]; var controlQueries = Questions(cases, first);
        var openReceiver = Receiver(first, controlQueries);
        episodes.Add(Run(first, openReceiver, new(first.Source.Observation, controlQueries, proposalCapacity: 0),
            "transferred", learned, evaluations[first], new Random(731), "unsearched"));

        var conflictReceiver = Receiver(first, controlQueries); var initial = conflictReceiver.Selection;
        var steps = new List<Step>(); var revision = new Placeholder(); var query = first.Queries[0];
        var positive = new StructuralEvidenceInquiry.Supplier(first.Source.Observation, controlQueries, revision: revision);
        var negative = new StructuralEvidenceInquiry.Supplier(first.Candidates[0].Observation, controlQueries, revision: revision);
        steps.Add(Acquire(conflictReceiver, positive, query, learned));
        steps.Add(Acquire(conflictReceiver, negative, query, learned));
        episodes.Add(Finish(first, conflictReceiver, initial, steps, "transferred", "conflict", evaluations[first],
            S(positive.AuditSurface, negative.AuditSurface)));

        var incapableReceiver = Receiver(first, controlQueries, available: false);
        episodes.Add(Run(first, incapableReceiver, new(first.Source.Observation, controlQueries), "transferred", learned,
            evaluations[first], new Random(731), "incapable"));
        return new(cases, learned, episodes);
    }

    private static StructuralEvidenceInquiry.Query[] Questions(StructuralEvidenceCases catalog, StructuralEvidenceCases.Case example)
    {
        if (example.ExpectedAmbiguity) return [.. example.Queries];
        var pattern = example.Id == "S01" ? catalog.Cases[1].Queries[0].Pattern : catalog.Cases[5].Queries[0].Pattern;
        // This is a valid broader question, placed first for every policy.
        // Whether it distinguishes candidates is read from native predictions.
        var broader = new StructuralEvidenceInquiry.Query(example.Id == "S01"
            ? "Do four arms share a site? (broader check)"
            : "Is there a four-edge closed contour? (broader check)", pattern);
        return [broader, .. example.Queries];
    }
    private static StructuralEvidenceInquiry.Receiver Receiver(StructuralEvidenceCases.Case example,
        IReadOnlyList<StructuralEvidenceInquiry.Query> queries, bool available = true) => new(
        example.Candidates.Select(sample => new StructuralEvidenceInquiry.Candidate(sample.Name, sample.Observation,
            available, sample.Induction)).ToArray(), queries,
        S(example.Queries.Select(query => (IElement)query.Surface).ToArray()));

    private static Episode Run(StructuralEvidenceCases.Case example, StructuralEvidenceInquiry.Receiver receiver,
        StructuralEvidenceInquiry.Supplier supplier, string policy, EvidenceSelectionStudy.LearnedPolicy learned,
        IReadOnlyList<StructuralEvidenceInquiry.ObservationComparison> evaluations, Random random, string? control = null)
    {
        var initial = receiver.Selection; var steps = new List<Step>();
        while (steps.Count < 4 && receiver.Selection.Eligible.Count != 0 &&
            !(receiver.Selection.Eligible.Count == 1 && E(receiver.Selection.Eligible[0].Open).Length == 0) &&
            StructuralEvidenceInquiry.Available(receiver).Length > 0)
        {
            var available = StructuralEvidenceInquiry.Available(receiver);
            var query = policy switch
            {
                "fixed" => available[0],
                "random" => available[random.Next(available.Length)],
                "disagreement" => available.OrderByDescending(candidate =>
                    StructuralEvidenceInquiry.Features(receiver, candidate, learned.Basis).Values[1]).First(),
                "transferred" => StructuralEvidenceInquiry.Select(receiver, learned)!,
                _ => throw new ArgumentException("Unknown comparison policy.", nameof(policy))
            };
            steps.Add(Acquire(receiver, supplier, query, learned));
        }
        return Finish(example, receiver, initial, steps, policy, control, evaluations, supplier.AuditSurface);
    }
    private static Step Acquire(StructuralEvidenceInquiry.Receiver receiver, StructuralEvidenceInquiry.Supplier supplier,
        StructuralEvidenceInquiry.Query query, EvidenceSelectionStudy.LearnedPolicy learned)
    {
        var features = StructuralEvidenceInquiry.Features(receiver, query, learned.Basis); var before = receiver.Selection;
        var response = supplier.Request(receiver, query); var after = receiver.Receive(response);
        return new(query, features, StructuralEvidenceInquiry.Predict(features, learned), response, before, after,
            new Relation(before.Surface, new Relation(features.Surface, new Relation(response.Surface, after.Surface))));
    }
    private static Episode Finish(StructuralEvidenceCases.Case example, StructuralEvidenceInquiry.Receiver receiver,
        StructuralEvidenceInquiry.Assessment initial, IReadOnlyList<Step> steps, string policy, string? control,
        IReadOnlyList<StructuralEvidenceInquiry.ObservationComparison> evaluations, Relation supplierAudit)
    {
        var final = receiver.Selection;
        bool Verified(StructuralEvidenceInquiry.CandidateRead read)
        {
            var index = receiver.Candidates.ToList().FindIndex(candidate => ReferenceEquals(candidate, read.Candidate));
            return index >= 0 && E(read.Open).Length == 0 && evaluations[index].IsCompatibleAtVocabulary;
        }
        var correct = final.Eligible.Count == 1 && E(final.Conflicts).Length == 0 && Verified(final.Eligible[0]);
        var ambiguity = example.ExpectedAmbiguity && final.Eligible.Count > 1 &&
            E(final.Conflicts).Length == 0 && final.Eligible.All(Verified);
        var surface = new Relation(new Relation(initial.Surface, S(steps.Select(step => (IElement)step.Surface).ToArray())),
            new Relation(final.Surface, new Relation(S(evaluations.Select(evaluation => (IElement)evaluation.Surface).ToArray()), supplierAudit)));
        return new($"{example.Id}-{policy}{(control is null ? "" : "-" + control)}", example, policy, control, receiver,
            initial, steps, final, evaluations, correct, ambiguity, supplierAudit, surface);
    }
    private static Relation S(params IElement[] entries) => StructuralConstraint.Closed(entries);
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
}
