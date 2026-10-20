using Gojo.Core;
using Gojo.Experiments.QualitativeExecution;
using Gojo.Experiments.QualitativeFrieze;
using Gojo.Patterns;
using C = Gojo.Experiments.QualitativeFrieze.OccurrenceComposition;

namespace QualitativeExecution.Tests;

public sealed class OccurrenceCompositionTests
{
    [Fact]
    public void Repeating_an_existential_clause_is_not_evidence_for_two_physical_copies()
    {
        var one = Triangles(1); var pattern = TrianglePattern(one);
        var clauses = new CommunicationAssessment(one, new CommunicationMessage([pattern, pattern]));
        Assert.Equal(2, clauses.SupportedCount);
        Assert.Single(clauses.Matches.Select(match => match.Occurrence.Read).Distinct());
        var a = new C.Role(pattern); var b = new C.Role(pattern);
        Assert.Equal("Yes", new C.Assessment(one, new("same footprint permitted", [a, b])).Answer);
        Assert.Equal("No", new C.Assessment(one,
            new("two different footprints", [a, b], distinct: S(C.Pair(a, b)))).Answer);
    }

    [Fact]
    public void Independent_role_slots_can_share_a_definition_and_bind_distinct_actual_footprints()
    {
        var two = Triangles(2); var pattern = TrianglePattern(two);
        var a = new C.Role(pattern); var b = new C.Role(pattern);
        var question = new C.Question("two separated loops", [a, b],
            distinct: S(C.Pair(a, b)), disjointSites: S(C.Pair(a, b)));
        var assessment = new C.Assessment(two, question);
        Assert.Equal("Yes", assessment.Answer);
        Assert.Empty(E(assessment.Refuted)); Assert.Empty(E(assessment.Open)); Assert.Empty(E(assessment.Blocked));
        var witness = Assert.Single(assessment.Witnesses);
        var bindings = E(witness.Bindings).Cast<Relation>().ToArray();
        Assert.Same(a.Slot, bindings[0].Left); Assert.Same(b.Slot, bindings[1].Left);
        Assert.NotSame(bindings[0].Right, bindings[1].Right);
        Assert.Equal(2, witness.Matches.Count);
        Assert.All(witness.Matches, match => Assert.Contains(match.Family, two.Families));
        Assert.Equal(2, E(witness.Checks).Length);
        Assert.Equal(2, E(witness.Footprints).Length);
    }

    [Fact]
    public void Sharing_a_site_does_not_imply_sharing_an_edge()
    {
        var touching = Triangles(2, shareSite: true); var pattern = TrianglePattern(touching);
        var a = new C.Role(pattern); var b = new C.Role(pattern);
        var pair = S(C.Pair(a, b));
        var site = new C.Assessment(touching, new("site contact", [a, b],
            distinct: pair, sharedSites: pair, disjointEdges: pair));
        Assert.Equal("Yes", site.Answer);
        var edge = new C.Assessment(touching, new("edge contact", [a, b], distinct: pair, sharedEdges: pair));
        Assert.Equal("No", edge.Answer);
        var separate = new C.Assessment(touching, new("no site contact", [a, b], distinct: pair, disjointSites: pair));
        Assert.Equal("No", separate.Answer);
        Assert.NotEmpty(E(edge.Rejected));
    }

    [Fact]
    public void A_shared_actual_edge_provides_a_witness_and_is_not_inferred_from_equal_headings()
    {
        var joined = Triangles(2, shareEdge: true); var pattern = TrianglePattern(joined);
        var a = new C.Role(pattern); var b = new C.Role(pattern); var pair = S(C.Pair(a, b));
        var shared = new C.Assessment(joined, new("shared edge", [a, b], distinct: pair, sharedEdges: pair));
        Assert.Equal("Yes", shared.Answer);
        var selected = Assert.Single(shared.Witnesses);
        var first = E(selected.Matches[0].Occurrence.Segments);
        var second = E(selected.Matches[1].Occurrence.Segments);
        Assert.Single(first.Intersect(second, ReferenceEqualityComparer.Instance));
        Assert.Equal("No", new C.Assessment(Triangles(2), shared.Question).Answer);
    }

    [Fact]
    public void All_selected_roles_must_satisfy_pair_requirements_in_one_joint_assignment()
    {
        var two = Triangles(2); var pattern = TrianglePattern(two);
        var a = new C.Role(pattern); var b = new C.Role(pattern); var c = new C.Role(pattern);
        var question = new C.Question("three copies", [a, b, c],
            distinct: S(C.Pair(a, b), C.Pair(a, c), C.Pair(b, c)));
        Assert.Equal("No", new C.Assessment(two, question).Answer);
        Assert.Equal("Yes", new C.Assessment(Triangles(3), question).Answer);
    }

    [Fact]
    public void Budget_exhaustion_keeps_a_cut_open_and_accounts_for_consumed_and_remaining_slots()
    {
        var output = Triangles(3); var pattern = TrianglePattern(output);
        var a = new C.Role(pattern); var b = new C.Role(pattern); var pair = S(C.Pair(a, b));
        var question = new C.Question("a touching pair", [a, b], distinct: pair, sharedSites: pair);
        var cut = new C.Assessment(output, question, assignmentCapacity: 3);
        Assert.Equal("Open", cut.Answer); Assert.Empty(E(cut.Refuted)); Assert.Empty(cut.Witnesses);
        Assert.Equal(3, E(cut.Consumed).Length); Assert.Empty(E(cut.Remaining));
        var finished = new C.Assessment(output, question, assignmentCapacity: 64);
        Assert.Equal("No", finished.Answer);
        Assert.Equal(64, E(finished.Consumed).Length + E(finished.Remaining).Length);
        Assert.NotEmpty(E(finished.Rejected));
        Assert.All(E(finished.Consumed).Cast<Relation>(), item =>
            Assert.Contains(E(finished.Budget), slot => ReferenceEquals(slot, item.Left)));
    }

    [Fact]
    public void Mutually_opposed_constraints_are_inspectable_false_claims_rather_than_malformed_queries()
    {
        var output = Triangles(2); var pattern = TrianglePattern(output);
        var a = new C.Role(pattern); var b = new C.Role(pattern); var pair = S(C.Pair(a, b));
        var question = new C.Question("both touching and separate", [a, b],
            distinct: pair, sharedSites: pair, disjointSites: pair);
        Assert.True(question.IsValid);
        var assessment = new C.Assessment(output, question);
        Assert.Equal("No", assessment.Answer); Assert.Empty(E(assessment.Blocked));
    }

    [Fact]
    public void A_known_absent_role_refutes_a_conjunction_without_spending_assignment_capacity()
    {
        var pattern = TrianglePattern(Triangles(1)); var role = new C.Role(pattern);
        var empty = new QualitativeMotifInduction(new(new QualitativeMotionFrame(), S(), S()));
        var assessment = new C.Assessment(empty, new("a loop exists", [role]), assignmentCapacity: 0);
        Assert.True(empty.IsComplete); Assert.Equal("No", assessment.Answer);
        Assert.Empty(E(assessment.Consumed));
    }

    private static CommunicationPattern TrianglePattern(QualitativeMotifInduction output) =>
        new(output.Observation.Frame, output.Families.First(family => family.Kind == "cycle" && family.SegmentCount == 3).Definition);

    private static QualitativeMotifInduction Triangles(int count, bool shareSite = false, bool shareEdge = false)
    {
        var frame = new QualitativeMotionFrame(); var sites = new List<IElement>(); var edges = new List<IElement>();
        Placeholder? commonA = null; Placeholder? commonB = null;
        for (var index = 0; index < count; index++)
        {
            var a = (shareSite || shareEdge) && commonA is not null ? commonA : new Placeholder();
            var b = shareEdge && commonB is not null ? commonB : new Placeholder();
            var c = new Placeholder();
            if (!sites.Contains(a)) sites.Add(a);
            if (!sites.Contains(b)) sites.Add(b);
            sites.Add(c);
            if (!shareEdge || index == 0) Edge(a, b, frame.Right);
            Edge(b, c, frame.Left, frame.Up); Edge(c, a, frame.Down);
            commonA = a; commonB = b;
        }
        return new(new(frame, S(sites.ToArray()), S(edges.ToArray())), maxCycleEdges: 3,
            proposalCapacity: 8192, maxWalkEdges: 3, retainedCapacity: 2048);

        void Edge(IElement from, IElement to, params IElement[] headings) =>
            edges.Add(new Relation(new Placeholder(), new Relation(new Relation(from, to), S(headings))));
    }

    private static Relation S(params IElement[] entries) => StructuralConstraint.Closed(entries);
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
}
