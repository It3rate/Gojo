using Gojo.Core;
using Gojo.Experiments.QualitativeExecution;
using Gojo.Experiments.QualitativeFrieze;

namespace QualitativeExecution.Tests;

public sealed class OccurrenceCompositionCaseTests
{
    private static readonly Lazy<OccurrenceCompositionCases> Catalog = new(OccurrenceCompositionCases.Build);

    [Fact]
    public void Existential_motif_agreement_does_not_establish_multiplicity()
    {
        var example = Case("C01");
        var one = example.Candidates[0]; var two = example.Candidates[1]; var three = example.Candidates[2];
        Assert.True(StructuralEvidenceInquiry.CompareObservations(one.Induction, two.Induction).IsCompatibleAtVocabulary);
        Assert.True(StructuralEvidenceInquiry.CompareObservations(two.Induction, three.Induction).IsCompatibleAtVocabulary);
        Assert.Equal(new[] { "Yes", "No", "No" }, Answers(one, example));
        Assert.Equal(new[] { "Yes", "Yes", "No" }, Answers(two, example));
        Assert.Equal(new[] { "Yes", "Yes", "Yes" }, Answers(three, example));
        Assert.Equal(new[] { "Yes", "Yes", "No" }, Answers(example.Source, example));
    }

    [Fact]
    public void Shared_corner_and_shared_edge_are_distinct_native_incidence_reads()
    {
        var corner = Case("C02"); var edge = Case("C03");
        Assert.Equal(new[] { "No", "No", "Yes" }, corner.Candidates.Select(sample => Answer(sample, corner.Queries[0])));
        Assert.Equal(new[] { "No", "No", "Yes" }, edge.Candidates.Select(sample => Answer(sample, edge.Queries[0])));
        Assert.Equal("Yes", Answer(corner.Source, corner.Queries[0]));
        Assert.Equal("Yes", Answer(edge.Source, edge.Queries[0]));

        // Two drawn four-edge loops need seven actual edges when one boundary
        // is reused. Eight coincident edges would not establish a shared seam.
        Assert.Equal(7, E(edge.Source.Observation.Segments).Length);
        Assert.Equal(6, E(edge.Source.Observation.Sites).Length);
        Assert.Equal(2, E(edge.Source.Join.Accepted).Length);
        Assert.Equal(8, E(corner.Source.Observation.Segments).Length);
        Assert.Equal(7, E(corner.Source.Observation.Sites).Length);
        Assert.Single(E(corner.Source.Join.Accepted));
    }

    [Fact]
    public void The_bridge_requires_two_admitted_endpoint_joins()
    {
        var example = Case("C04"); var loose = example.Candidates[0]; var joined = example.Candidates[1];
        Assert.Equal("No", Answer(loose, example.Queries[0]));
        Assert.Equal("Yes", Answer(joined, example.Queries[0]));
        Assert.Equal("Yes", Answer(example.Source, example.Queries[0]));
        Assert.Empty(E(loose.Join.Accepted)); Assert.Equal(2, E(joined.Join.Accepted).Length);
        Assert.Equal(10, E(loose.Observation.Segments).Length);
        Assert.Equal(10, E(joined.Observation.Segments).Length);
        Assert.Equal(11, E(loose.Observation.Sites).Length);
        Assert.Equal(9, E(joined.Observation.Sites).Length);
        Assert.Equal(3, example.Queries[0].Roles.Count);
    }

    [Fact]
    public void Spacing_remains_unresolved_without_a_spacing_service()
    {
        var example = Case("C05");
        Assert.True(example.ExpectedAmbiguity);
        Assert.All(example.Candidates, candidate => Assert.Equal("Yes", Answer(candidate, example.Queries[0])));
        Assert.Equal("Yes", Answer(example.Source, example.Queries[0]));
        Assert.True(StructuralEvidenceInquiry.CompareObservations(example.Candidates[0].Induction,
            example.Candidates[1].Induction).IsCompatibleAtVocabulary);
        Assert.All(example.Candidates, candidate => Assert.NotSame(candidate.Observation, example.Source.Observation));
    }

    [Fact]
    public void Equal_placement_does_not_merge_independently_drawn_occurrences()
    {
        var coincident = OccurrenceCompositionCases.CoincidentTriangles(2);
        var example = Case("C01");
        Assert.Equal("Yes", Answer(coincident, example.Queries[1]));
        Assert.Equal(6, E(coincident.Observation.Sites).Length);
        Assert.Equal(6, E(coincident.Observation.Segments).Length);
        Assert.Empty(E(coincident.Join.Accepted));
    }

    [Fact]
    public void Every_source_and_alternative_is_fresh_acknowledged_history_blind_structure()
    {
        foreach (var example in Catalog.Value.Cases)
        {
            Assert.True(example.Source.Execution.Current.Execution.IsAtBoundary);
            Assert.True(example.Source.Publisher.IsComplete); Assert.True(example.Source.Induction.IsComplete);
            foreach (var candidate in example.Candidates)
            {
                Assert.True(candidate.Execution.Current.Execution.IsAtBoundary);
                Assert.True(candidate.Publisher.IsComplete); Assert.True(candidate.Induction.IsComplete);
                Assert.NotSame(candidate.Execution, example.Source.Execution);
                Assert.DoesNotContain(E(candidate.Observation.Sites), site =>
                    E(example.Source.Observation.Sites).Any(other => ReferenceEquals(site, other)));
            }
            var queryReachable = example.Queries.SelectMany(query => Reachable(query.Surface)).ToArray();
            Assert.DoesNotContain(queryReachable, element => ReferenceEquals(element, example.Source.Observation.Surface));
            Assert.DoesNotContain(queryReachable, element => ReferenceEquals(element, example.Source.Program.Surface));
        }
    }

    private static OccurrenceCompositionCases.Case Case(string id) => Catalog.Value.Cases.Single(item => item.Id == id);
    private static string[] Answers(StructuralEvidenceCases.Sample sample, OccurrenceCompositionCases.Case example) =>
        example.Queries.Select(query => Answer(sample, query)).ToArray();
    private static string Answer(StructuralEvidenceCases.Sample sample, OccurrenceComposition.Question question) =>
        new OccurrenceComposition.Assessment(sample.Induction, question).Answer;
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
