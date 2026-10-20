using System.Diagnostics;
using Gojo.Core;
using Gojo.Experiments.QualitativeExecution;
using Gojo.Patterns;

namespace Gojo.Experiments.QualitativeFrieze;

/// <summary>Direct local reads of exposed inventories, followed by finite
/// candidate comparison. This does not implement a second supplier transport
/// protocol, learn candidates, or recover unadvertised geometric information.</summary>
public sealed class OccurrenceCompositionStudy
{
    // query read = query -> (source assessment -> Closed(candidate assessments))
    // candidate comparison = actual candidate observation ->
    //                        (agreement -> (opposition -> (open -> blocked)))
    // stage = Closed(actual query reads) -> Closed(candidate comparisons)
    public sealed record QueryRead(OccurrenceComposition.Question Question,
        OccurrenceComposition.Assessment Source,
        IReadOnlyList<OccurrenceComposition.Assessment> Candidates, Relation Surface);

    public sealed class CandidateRead
    {
        public int Index { get; }
        public Relation Agreement { get; }
        public Relation Opposition { get; }
        public Relation Open { get; }
        public Relation Blocked { get; }
        public Relation Surface { get; }
        public bool IsCompatible => E(Opposition).Length == 0 && E(Blocked).Length == 0;
        public bool IsSettled => IsCompatible && E(Open).Length == 0;
        internal CandidateRead(int index, StructuralEvidenceCases.Sample candidate, IReadOnlyList<QueryRead> reads)
        {
            Index = index;
            var agree = new List<IElement>(); var oppose = new List<IElement>();
            var open = new List<IElement>(); var blocked = new List<IElement>();
            foreach (var read in reads)
            {
                var actual = read.Source; var expected = read.Candidates[index];
                var witness = new Relation(actual.Surface, expected.Surface);
                if (E(actual.Blocked).Length != 0 || E(expected.Blocked).Length != 0) blocked.Add(witness);
                else if (E(actual.Open).Length != 0 || E(expected.Open).Length != 0) open.Add(witness);
                else if (E(actual.Supported).Length != 0 == (E(expected.Supported).Length != 0)) agree.Add(witness);
                else oppose.Add(witness);
            }
            Agreement = S(agree.ToArray()); Opposition = S(oppose.ToArray());
            Open = S(open.ToArray()); Blocked = S(blocked.ToArray());
            Surface = new(candidate.Observation.Surface,
                new Relation(Agreement, new Relation(Opposition, new Relation(Open, Blocked))));
        }
    }

    public sealed class Stage
    {
        public IReadOnlyList<QueryRead> Reads { get; }
        public IReadOnlyList<CandidateRead> Candidates { get; }
        public IReadOnlyList<CandidateRead> Compatible { get; }
        public Relation Surface { get; }
        internal Stage(OccurrenceCompositionCases.Case example, IReadOnlyList<QueryRead> reads)
        {
            Reads = reads;
            Candidates = example.Candidates.Select((candidate, index) => new CandidateRead(index, candidate, reads)).ToArray();
            Compatible = Candidates.Where(candidate => candidate.IsCompatible).ToArray();
            Surface = new(S(reads.Select(read => (IElement)read.Surface).ToArray()),
                S(Candidates.Select(candidate => (IElement)candidate.Surface).ToArray()));
        }
    }

    public sealed record Episode(OccurrenceCompositionCases.Case Case, IReadOnlyList<QueryRead> Reads,
        IReadOnlyList<Stage> Stages,
        IReadOnlyList<StructuralEvidenceInquiry.ObservationComparison> EarlierVocabulary, Relation Surface);
    public sealed record Control(string Name, string Purpose, OccurrenceComposition.Assessment Read);
    public sealed record Scale(int Copies, int Sites, int Edges, string Question, int AssignmentCapacity,
        string Answer, int Attempts, double InductionMilliseconds, double JointMilliseconds);

    public OccurrenceCompositionCases Cases { get; }
    public IReadOnlyList<Episode> Episodes { get; }
    public IReadOnlyList<Control> Controls { get; }
    public IReadOnlyList<Scale> Scaling { get; }
    public Relation Surface { get; }
    private OccurrenceCompositionStudy(OccurrenceCompositionCases cases, IReadOnlyList<Episode> episodes,
        IReadOnlyList<Control> controls, IReadOnlyList<Scale> scaling)
    {
        Cases = cases; Episodes = episodes; Controls = controls; Scaling = scaling;
        Surface = new(cases.Surface, new Relation(S(episodes.Select(item => (IElement)item.Surface).ToArray()),
            S(controls.Select(item => (IElement)item.Read.Surface).ToArray())));
    }

    public static OccurrenceCompositionStudy Build()
        => Build(OccurrenceCompositionCases.Build(), includeScaling: true);

    public static OccurrenceCompositionStudy BuildLandmarks()
        => Build(new(LandmarkCompositionCases.Build().Cases), includeScaling: false);

    private static OccurrenceCompositionStudy Build(OccurrenceCompositionCases cases, bool includeScaling)
    {
        var episodes = new List<Episode>();
        foreach (var example in cases.Cases)
        {
            var reads = example.Queries.Select(question =>
            {
                var source = new OccurrenceComposition.Assessment(example.Source.Induction, question);
                var candidates = example.Candidates.Select(candidate => new OccurrenceComposition.Assessment(candidate.Induction, question)).ToArray();
                return new QueryRead(question, source, candidates,
                    new Relation(question.Surface, new Relation(source.Surface, S(candidates.Select(item => (IElement)item.Surface).ToArray()))));
            }).ToArray();
            var stages = Enumerable.Range(0, reads.Length + 1).Select(length => new Stage(example, reads[..length])).ToArray();
            // This diagnostic is deliberately separate: it is the earlier
            // existential vocabulary's agreement, not a count/isomorphism test.
            var earlier = example.Candidates.Select(candidate => StructuralEvidenceInquiry.CompareObservations(
                example.Source.Induction, candidate.Induction)).ToArray();
            var surface = new Relation(example.Surface, new Relation(S(stages.Select(item => (IElement)item.Surface).ToArray()),
                S(earlier.Select(item => (IElement)item.Surface).ToArray())));
            episodes.Add(new(example, reads, stages, earlier, surface));
        }
        var first = cases.Cases[0]; var question = first.Queries.Last();
        var unsearched = new QualitativeMotifInduction(first.Source.Observation, maxCycleEdges: 4,
            proposalCapacity: 0, maxWalkEdges: 3, retainedCapacity: 1024);
        var controls = new List<Control> {
            new("No joint-search budget", "A complete motif inventory is insufficient if the compound assignments have not been searched.",
                new(first.Source.Induction, question, assignmentCapacity: 0)),
            new("No motif-search budget", "Missing a motif in an unsearched inventory cannot establish absence.",
                new(unsearched, question))
        };
        if (!includeScaling)
        {
            controls.Add(new("No correspondence-search budget",
                "A whole-motif match does not establish which actual sites fill the selected pattern landmarks.",
                new(first.Source.Induction, question, correspondenceCapacity: 0)));
            var roles = question.Roles;
            if (roles.Count < 2 || !CommunicationShape.TryRead(roles[1].PatternSurface, out var shape))
                throw new InvalidOperationException("The landmark control requires two valid source-free role patterns.");
            var open = new OccurrenceComposition.SiteSelection(roles[0], StructuralConstraint.Open());
            var other = OccurrenceComposition.Select(roles[1], shape.Sites);
            var incomplete = new OccurrenceComposition.Question("An unfinished landmark publication", roles,
                sharedSelectedSites: S(OccurrenceComposition.Pair(open, other)));
            controls.Add(new("Landmark selection not yet published",
                "The selected site scope is open. Its missing detail is not an empty selection or a negative answer.",
                new(first.Source.Induction, incomplete)));
        }
        return new(cases, episodes, controls, includeScaling ? ScaleReads() : []);
    }

    private static IReadOnlyList<Scale> ScaleReads()
    {
        var rows = new List<Scale>(); var exemplar = OccurrenceCompositionCases.DisjointTriangles(1);
        var family = exemplar.Induction.Families.First(item => item.Kind == "cycle" && item.SegmentCount == 3);
        var pattern = new CommunicationPattern(exemplar.Observation.Frame, family.Definition);
        var a = new OccurrenceComposition.Role(pattern); var b = new OccurrenceComposition.Role(pattern);
        var pairs = S(new Relation(a.Slot, b.Slot));
        var separate = new OccurrenceComposition.Question("Two site-disjoint loops", [a, b], distinct: pairs, disjointSites: pairs);
        var touching = new OccurrenceComposition.Question("Two loops sharing a site", [a, b], distinct: pairs, sharedSites: pairs);
        // Warm the exact reader before reporting these small diagnostic timings.
        _ = new OccurrenceComposition.Assessment(exemplar.Induction, separate);
        foreach (var copies in new[] { 1, 2, 4, 8, 16 })
        {
            var sample = OccurrenceCompositionCases.DisjointTriangles(copies);
            var watch = Stopwatch.StartNew();
            var inventory = new QualitativeMotifInduction(sample.Observation, maxCycleEdges: 4,
                proposalCapacity: 4096, maxWalkEdges: 3, retainedCapacity: 1024);
            watch.Stop(); var inductionMs = watch.Elapsed.TotalMilliseconds;
            foreach (var query in new[] { separate, touching })
            foreach (var capacity in new[] { 8, 4096 })
            {
                watch.Restart(); var read = new OccurrenceComposition.Assessment(inventory, query, assignmentCapacity: capacity); watch.Stop();
                rows.Add(new(copies, E(sample.Observation.Sites).Length, E(sample.Observation.Segments).Length,
                    query.Name, capacity, Answer(read), E(read.Consumed).Length, inductionMs, watch.Elapsed.TotalMilliseconds));
            }
        }
        return rows;
    }

    public static string Answer(OccurrenceComposition.Assessment read) => E(read.Blocked).Length != 0 ? "Blocked"
        : E(read.Supported).Length != 0 ? "Yes" : E(read.Refuted).Length != 0 ? "No" : "Open";
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
    private static Relation S(params IElement[] entries) => StructuralConstraint.Closed(entries);
}
