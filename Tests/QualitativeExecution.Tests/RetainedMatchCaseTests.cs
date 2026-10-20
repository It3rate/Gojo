using Gojo.Core;
using Gojo.Experiments.QualitativeExecution;
using Gojo.Experiments.QualitativeFrieze;
using C = Gojo.Experiments.QualitativeFrieze.OccurrenceComposition;

namespace QualitativeExecution.Tests;

public sealed class RetainedMatchCaseTests
{
    private static readonly Lazy<RetainedMatchCases> Catalog = new(RetainedMatchCases.Build);

    [Fact]
    public void A_fresh_yes_can_come_from_a_different_path_than_the_retained_one()
    {
        var example = Case("R01");
        var initial = Initial(example); var focus = RetainedMatch.Capture(initial, Assert.Single(initial.Witnesses), example.RetainedRoles);
        var fresh = new C.Assessment(example.Source.Induction, example.FollowupQuestion);
        var retained = new C.Assessment(example.Source.Induction, example.FollowupQuestion, retained: focus);
        Assert.Equal("Yes", fresh.Answer); Assert.Equal("No", retained.Answer);
        Assert.False(SameEdges(initial.Witnesses[0].Matches[1], fresh.Witnesses[0].Matches[1]));
        Assert.Empty(E(retained.Open)); Assert.Empty(E(retained.Blocked));
        Assert.Equal(2, E(example.Source.Join.Accepted).Length);
    }

    [Fact]
    public void Retaining_only_the_path_permits_a_new_loop_partner()
    {
        var example = Case("R02");
        var initial = Initial(example); var first = Assert.Single(initial.Witnesses);
        var focus = RetainedMatch.Capture(initial, first, example.RetainedRoles);
        var retained = new C.Assessment(example.Source.Induction, example.FollowupQuestion, retained: focus);
        var next = Assert.Single(retained.Witnesses);
        Assert.Equal("Yes", retained.Answer); Assert.Single(E(example.RetainedRoles));
        Assert.True(SameEdges(first.Matches[1], next.Matches[1]));
        Assert.False(SameEdges(first.Matches[0], next.Matches[0]));
        Assert.Equal(3, E(example.Source.Join.Accepted).Length);
    }

    [Fact]
    public void Retaining_both_roles_prevents_replacement_of_the_original_partner()
    {
        var example = Case("R03");
        var initial = Initial(example); var first = Assert.Single(initial.Witnesses);
        var full = RetainedMatch.Capture(initial, first, example.RetainedRoles);
        Assert.Equal(2, E(example.RetainedRoles).Length);
        Assert.Equal("Yes", new C.Assessment(example.Source.Induction, example.FollowupQuestion).Answer);
        Assert.Equal("No", new C.Assessment(example.Source.Induction, example.FollowupQuestion, retained: full).Answer);
        // The same source and questions permit the endpoint join once the loop
        // is genuinely left free. This is a structural scope difference.
        var pathOnly = RetainedMatch.Capture(initial, first, Gojo.Patterns.StructuralConstraint.Closed(example.InitialQuestion.Roles[1].Slot));
        Assert.Equal("Yes", new C.Assessment(example.Source.Induction, example.FollowupQuestion, retained: pathOnly).Answer);
    }

    [Fact]
    public void Between_is_preserved_without_equal_subdivision_or_measurement()
    {
        var unequal = Case("R04"); var equal = Case("R05");
        foreach (var example in new[] { unequal, equal })
        {
            var initial = Initial(example); var focus = RetainedMatch.Capture(initial, Assert.Single(initial.Witnesses), example.RetainedRoles);
            var retained = new C.Assessment(example.Source.Induction, example.FollowupQuestion, retained: focus);
            Assert.Equal("Yes", retained.Answer);
            var first = initial.Witnesses[0]; var next = Assert.Single(retained.Witnesses);
            Assert.True(SameEdges(first.Matches[1], next.Matches[1]));
            Assert.False(SameEdges(first.Matches[0], next.Matches[0]));
        }
        Assert.True(StructuralEvidenceInquiry.CompareObservations(unequal.Source.Induction, equal.Source.Induction).IsCompatibleAtVocabulary);
        // Private construction audit only: the advertised observations omit
        // these reach words, so their size cannot decide the between query.
        var unequalWords = PathWords(unequal.Source); var equalWords = PathWords(equal.Source);
        Assert.NotEqual(E(unequalWords[0]).Length, E(unequalWords[1]).Length);
        Assert.Equal(E(equalWords[0]).Length, E(equalWords[1]).Length);
    }

    [Fact]
    public void Questions_keep_native_roles_but_cannot_reach_private_execution_or_source_observation()
    {
        foreach (var example in Catalog.Value.Cases)
        {
            Assert.True(example.Source.Execution.Current.Execution.IsAtBoundary);
            Assert.True(example.Source.Publisher.IsComplete); Assert.True(example.Source.Induction.IsComplete);
            Assert.True(example.InitialQuestion.IsValid); Assert.True(example.FollowupQuestion.IsValid);
            Assert.All(example.InitialQuestion.Roles, role => Assert.Contains(example.FollowupQuestion.Roles,
                next => ReferenceEquals(role.Slot, next.Slot) && ReferenceEquals(role.PatternSurface, next.PatternSurface)));
            var queryGraph = Reachable(example.InitialQuestion.Surface).Concat(Reachable(example.FollowupQuestion.Surface)).ToArray();
            Assert.DoesNotContain(queryGraph, element => ReferenceEquals(element, example.Source.Program.Surface));
            Assert.DoesNotContain(queryGraph, element => ReferenceEquals(element, example.Source.Observation.Surface));
            Assert.DoesNotContain(queryGraph, element => element is Carrier or CarrierLocation);
            Assert.All(E(example.RetainedRoles), slot => Assert.Contains(example.InitialQuestion.Roles,
                role => ReferenceEquals(role.Slot, slot)));
        }
    }

    [Fact]
    public void Fresh_executions_with_the_same_recipe_do_not_share_occurrence_identity()
    {
        var first = Case("R02").Source; var second = Case("R03").Source;
        Assert.NotSame(first.Execution, second.Execution);
        Assert.DoesNotContain(E(first.Observation.Sites), site => E(second.Observation.Sites).Any(other => ReferenceEquals(site, other)));
        Assert.DoesNotContain(E(first.Observation.Segments), edge => E(second.Observation.Segments).Any(other => ReferenceEquals(edge, other)));
        Assert.True(StructuralEvidenceInquiry.CompareObservations(first.Induction, second.Induction).IsCompatibleAtVocabulary);
    }

    private static C.Assessment Initial(RetainedMatchCases.Case example)
    {
        var read = new C.Assessment(example.Source.Induction, example.InitialQuestion);
        Assert.Equal("Yes", read.Answer); Assert.Single(read.Witnesses); return read;
    }
    private static RetainedMatchCases.Case Case(string id) => Catalog.Value.Cases.Single(item => item.Id == id);
    private static Relation[] PathWords(StructuralEvidenceCases.Sample sample) =>
        E((Relation)((Relation)E(sample.Program.Paths).Last()).Right).Cast<Relation>().ToArray();
    private static bool SameEdges(CommunicationMatch first, CommunicationMatch second)
    {
        var a = E(first.Occurrence.Segments); var b = E(second.Occurrence.Segments);
        return a.Length == b.Length && a.All(edge => b.Any(other => ReferenceEquals(edge, other)));
    }
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
