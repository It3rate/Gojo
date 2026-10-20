using Gojo.Core;
using Gojo.Experiments.QualitativeExecution;
using Gojo.Experiments.QualitativeFrieze;
using C = Gojo.Experiments.QualitativeFrieze.OccurrenceComposition;

namespace QualitativeExecution.Tests;

public sealed class LandmarkCompositionCaseTests
{
    private static readonly Lazy<LandmarkCompositionCases> Catalog = new(LandmarkCompositionCases.Build);

    [Fact]
    public void Whole_motif_contact_does_not_establish_endpoint_contact()
    {
        var example = Case("L01");
        Assert.Equal(new[] { "Yes", "No" }, Answers(example.Candidates[0], example));
        Assert.Equal(new[] { "Yes", "Yes" }, Answers(example.Candidates[1], example));
        Assert.Equal(new[] { "No", "No" }, Answers(example.Candidates[2], example));
        Assert.Equal(new[] { "Yes", "Yes" }, Answers(example.Source, example));
        Assert.Single(E(example.Candidates[0].Join.Accepted));
        Assert.Single(E(example.Candidates[1].Join.Accepted));
        Assert.Empty(E(example.Candidates[2].Join.Accepted));
    }

    [Fact]
    public void A_bridge_must_satisfy_both_selected_endpoint_contacts_in_one_assignment()
    {
        var example = Case("L02");
        Assert.Equal(new[] { "Yes", "No" }, Answers(example.Candidates[0], example));
        Assert.Equal(new[] { "Yes", "Yes" }, Answers(example.Candidates[1], example));
        Assert.Equal(new[] { "Yes", "Yes" }, Answers(example.Source, example));
        var read = new C.Assessment(example.Source.Induction, example.Queries[1]);
        Assert.Equal(3, Assert.Single(read.Witnesses).Matches.Count);
        Assert.Equal(2, E(example.Source.Join.Accepted).Length);
        Assert.Equal(2, E(example.Candidates[0].Join.Accepted).Length);
        Assert.Equal(2, E(example.Queries[1].SharedSelectedSites).Length);
    }

    [Fact]
    public void Local_incident_heading_faces_can_select_a_particular_corner_without_coordinates()
    {
        var example = Case("L03");
        Assert.Equal(new[] { "Yes", "No" }, Answers(example.Candidates[0], example));
        Assert.Equal(new[] { "Yes", "Yes" }, Answers(example.Candidates[1], example));
        Assert.Equal(new[] { "Yes", "Yes" }, Answers(example.Source, example));
        var read = new C.Assessment(example.Source.Induction, example.Queries[1]);
        Assert.Equal("Yes", read.Answer);
        Assert.NotEmpty(E(Assert.Single(read.Witnesses).Checks));
    }

    [Fact]
    public void Endpoint_contact_does_not_prove_equal_subdivision_or_a_geometric_midpoint()
    {
        var example = Case("L04");
        Assert.True(example.ExpectedAmbiguity);
        Assert.All(example.Candidates, candidate => Assert.Equal(new[] { "Yes", "Yes" }, Answers(candidate, example)));
        Assert.Equal(new[] { "Yes", "Yes" }, Answers(example.Source, example));
        Assert.True(StructuralEvidenceInquiry.CompareObservations(example.Candidates[0].Induction,
            example.Candidates[1].Induction).IsCompatibleAtVocabulary);
        // Audit the private construction separately: these really use unequal
        // and equal repetition of the same formal reach, respectively. None
        // of that reach participation is published to the question reader.
        var unequal = BridgeWords(example.Candidates[0]);
        var equal = BridgeWords(example.Candidates[1]);
        Assert.NotEqual(E(unequal[0]).Length, E(unequal[1]).Length);
        Assert.Equal(E(equal[0]).Length, E(equal[1]).Length);
        foreach (var sample in example.Candidates)
        {
            var words = BridgeWords(sample);
            var moves = words.SelectMany(E).Cast<Relation>().ToArray();
            Assert.All(moves, move => Assert.Same(sample.Program.Frame.Right, move.Left));
            Assert.All(moves, move => Assert.Same(moves[0].Right, move.Right));
        }
    }

    [Fact]
    public void Every_sample_executes_freshly_and_queries_reach_only_source_free_pattern_landmarks()
    {
        foreach (var example in Catalog.Value.Cases)
        {
            foreach (var sample in example.Candidates.Append(example.Source))
            {
                Assert.True(sample.Execution.Current.Execution.IsAtBoundary);
                Assert.True(sample.Publisher.IsComplete);
                Assert.True(sample.Induction.IsComplete);
                Assert.Empty(E(sample.Induction.Unsearched));
            }
            foreach (var candidate in example.Candidates)
            {
                Assert.NotSame(candidate.Execution, example.Source.Execution);
                Assert.DoesNotContain(E(candidate.Observation.Sites), site => E(example.Source.Observation.Sites)
                    .Any(other => ReferenceEquals(site, other)));
            }
            var reachable = example.Queries.SelectMany(query => Reachable(query.Surface)).ToArray();
            Assert.DoesNotContain(reachable, element => ReferenceEquals(element, example.Source.Program.Surface));
            Assert.DoesNotContain(reachable, element => ReferenceEquals(element, example.Source.Observation.Surface));
            Assert.DoesNotContain(reachable, element => element is Carrier or CarrierLocation);
            Assert.All(example.Queries, query => Assert.True(query.IsValid));
        }
    }

    private static OccurrenceCompositionCases.Case Case(string id) => Catalog.Value.Cases.Single(item => item.Id == id);
    private static string[] Answers(StructuralEvidenceCases.Sample sample, OccurrenceCompositionCases.Case example) =>
        example.Queries.Select(query => new C.Assessment(sample.Induction, query).Answer).ToArray();
    private static Relation[] BridgeWords(StructuralEvidenceCases.Sample sample) =>
        E((Relation)((Relation)E(sample.Program.Paths).Last()).Right).Cast<Relation>().ToArray();
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
    private static IEnumerable<IElement> Reachable(IElement root)
    {
        var seen = new HashSet<IElement>(ReferenceEqualityComparer.Instance);
        var pending = new Stack<IElement>(); pending.Push(root);
        while (pending.TryPop(out var element))
        {
            if (!seen.Add(element)) continue;
            yield return element;
            if (element is Relation relation) { pending.Push(relation.Left); pending.Push(relation.Right); }
        }
    }
}
