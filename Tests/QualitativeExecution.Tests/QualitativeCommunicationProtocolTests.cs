using Gojo.Core;
using Gojo.Encoding.Structures;
using Gojo.Experiments.QualitativeExecution;
using Gojo.Experiments.QualitativeFrieze;
using Gojo.Patterns;

namespace QualitativeExecution.Tests;

public sealed class QualitativeCommunicationProtocolTests
{
    [Fact]
    public void A_transported_message_remains_readable_without_its_builder_or_sender_world()
    {
        var sender = new Fixture(); sender.Cycle(1, 4, 2, 8);
        var original = new CommunicationMessage([sender.Pattern("cycle", 4)]);
        var archive = GojoStructureCompactJson.Serialize(GojoStructureCompactJson.FromElement("message", original.Surface));
        Assert.True(GojoStructureCompactJson.TryToElement(GojoStructureCompactJson.Deserialize(archive), out var restored));
        var root = Assert.IsType<Relation>(restored.Root);
        var message = new CommunicationMessage(Assert.IsType<Relation>(root.Left), Assert.IsType<Relation>(root.Right));
        Assert.True(message.IsComplete);
        Assert.NotSame(original.Surface, message.Surface);
        var receiver = new Fixture(); receiver.Cycle(1, 4, 2, 8);
        var assessment = new CommunicationAssessment(receiver.Read(), message);
        Assert.True(assessment.IsSatisfied);
        Assert.NotEmpty(assessment.Matches);
        Assert.All(assessment.Matches, match => Assert.Contains(Entries(message.Required), clause => ReferenceEquals(clause, match.Pattern)));
    }

    [Fact]
    public void Independent_frames_and_fresh_role_identities_match_through_native_bijections()
    {
        var sender = new Fixture(); sender.Cycle(1, 4, 2, 8);
        var pattern = sender.Pattern("cycle", 4);
        var receiver = new Fixture(); receiver.Cycle(1, 4, 2, 8); receiver.Cycle(1, 4, 2, 8);
        var output = receiver.Read(); var assessment = new CommunicationAssessment(output, new([pattern]));
        Assert.True(pattern.IsValid); Assert.True(assessment.IsSatisfied);
        Assert.Equal(2, assessment.Matches.Count);
        Assert.All(assessment.Matches, match =>
        {
            Assert.Same(pattern.Surface, match.Pattern);
            Assert.True(output.TryReadOccurrence(match.Family.Read, match.Occurrence.Read, out _));
            Assert.Same(match.Occurrence.Read, ((Relation)match.Witness.Left).Right);
            var mappings = (Relation)((Relation)match.Witness.Right).Right;
            var roles = (Relation)mappings.Right;
            Assert.Equal(4, Entries((Relation)roles.Left).Length);
            Assert.Equal(4, Entries((Relation)roles.Right).Length);
        });
        Assert.NotSame(sender.Frame.Surface, receiver.Frame.Surface);
    }

    [Fact]
    public void A_changed_heading_or_missing_shared_corner_refutes_the_same_cycle_commitment()
    {
        var sender = new Fixture(); sender.Cycle(1, 4, 2, 8); var pattern = sender.Pattern("cycle", 4);
        var slanted = new Fixture(); slanted.Cycle(1, 5, 2, 10);
        var cut = new Fixture(); foreach (var heading in new[] { 1, 4, 2, 8 }) cut.Edge(cut.Site(), cut.Site(), heading);
        foreach (var output in new[] { slanted.Read(), cut.Read() })
        {
            var assessment = new CommunicationAssessment(output, new([pattern]));
            Assert.False(assessment.IsSatisfied); Assert.Equal(1, assessment.RefutedCount);
            Assert.Empty(assessment.Matches); Assert.Equal(0, assessment.UnresolvedCount);
        }
    }

    [Fact]
    public void An_exclusion_is_refuted_by_actual_support_and_otherwise_needs_completed_search()
    {
        var sender = new Fixture(); sender.Cycle(1, 4, 2, 8); var pattern = sender.Pattern("cycle", 4);
        var receiver = new Fixture(); receiver.Cycle(1, 4, 2, 8);
        var present = new CommunicationAssessment(receiver.Read(), new([], [pattern]));
        Assert.Equal(1, present.RefutedCount); Assert.NotEmpty(present.Matches);
        var exhausted = new CommunicationAssessment(receiver.Read(proposalCapacity: 0), new([], [pattern]));
        Assert.Equal(0, exhausted.SupportedCount); Assert.Equal(0, exhausted.RefutedCount);
        Assert.Equal(1, exhausted.UnresolvedCount); Assert.False(exhausted.IsSatisfied);
        var empty = new CommunicationAssessment(new Fixture().Read(), new([], [pattern]));
        Assert.True(empty.IsSatisfied); Assert.Equal(1, empty.SupportedCount);
    }

    [Fact]
    public void An_existing_match_can_support_existence_despite_unfinished_other_search()
    {
        var sender = new Fixture(); sender.Edge(sender.Site(), sender.Site(), 1);
        var pattern = sender.Pattern("junction", 1);
        var receiver = new Fixture(); receiver.Edge(receiver.Site(), receiver.Site(), 1);
        var output = receiver.Read(proposalCapacity: 1);
        Assert.False(output.IsComplete);
        var assessment = new CommunicationAssessment(output, new([pattern]));
        Assert.True(assessment.IsSatisfied); Assert.NotEmpty(assessment.Matches);
    }

    [Fact]
    public void Omitted_larger_cycles_are_open_when_outside_the_declared_grammar_length()
    {
        var sender = new Fixture(); sender.Cycle(1, 5, 4, 2, 10, 8); var pattern = sender.Pattern("cycle", 6);
        var receiver = new Fixture(); receiver.Cycle(1, 5, 4, 2, 10, 8);
        var output = receiver.Read(maxCycleEdges: 4);
        Assert.True(output.IsComplete);
        var assessment = new CommunicationAssessment(output, new([], [pattern]));
        Assert.False(assessment.IsSatisfied); Assert.Equal(1, assessment.UnresolvedCount);
        Assert.Equal(0, assessment.SupportedCount); Assert.Equal(0, assessment.RefutedCount);
    }

    [Fact]
    public void Two_edge_multigraph_cycle_absence_is_open_outside_the_induction_grammar()
    {
        var fixture = new Fixture(); var a = fixture.Site(); var b = fixture.Site();
        fixture.Edge(a, b, 1); fixture.Edge(b, a, 2);
        var firstRole = new Placeholder(); var secondRole = new Placeholder();
        var firstEdgeRole = new Placeholder(); var secondEdgeRole = new Placeholder();
        var firstEnds = new Relation(firstRole, secondRole); var secondEnds = new Relation(secondRole, firstRole);
        var first = new Relation(firstEdgeRole, new Relation(firstEnds, S(fixture.Frame.Right)));
        var second = new Relation(secondEdgeRole, new Relation(secondEnds, S(fixture.Frame.Left)));
        var definition = new Relation(new Relation(fixture.Frame.Surface,
                new Relation(S(firstRole, secondRole), S(first, second))),
            new Relation(S(), S(S(new Relation(firstEdgeRole, firstEnds), new Relation(secondEdgeRole, secondEnds)))));
        var pattern = new CommunicationPattern(fixture.Frame, definition); Assert.True(pattern.IsValid);
        var output = fixture.Read(); Assert.True(output.IsComplete);
        foreach (var message in new[] { new CommunicationMessage([pattern]), new CommunicationMessage([], [pattern]) })
        {
            var assessment = new CommunicationAssessment(output, message);
            Assert.Equal(1, assessment.UnresolvedCount); Assert.Equal(0, assessment.RefutedCount);
            Assert.Equal(0, assessment.SupportedCount); Assert.False(assessment.IsSatisfied);
        }
    }

    [Theory]
    [InlineData("junction")]
    [InlineData("walk")]
    [InlineData("cycle")]
    public void Reordered_roles_and_equivalent_route_traversals_do_not_create_false_absence(string kind)
    {
        var sender = new Fixture();
        if (kind == "junction")
        {
            var centre = sender.Site(); foreach (var heading in new[] { 1, 2, 4, 8 }) sender.Edge(centre, sender.Site(), heading);
        }
        else if (kind == "cycle") sender.Cycle(1, 4, 2, 8);
        else { var a = sender.Site(); var b = sender.Site(); var c = sender.Site(); sender.Edge(a, b, 1); sender.Edge(b, c, 4); }
        var original = sender.Pattern(kind, kind == "walk" ? 2 : 4);
        var graph = (Relation)((Relation)original.Definition.Left).Right;
        var roleEdges = Entries((Relation)graph.Right).Cast<Relation>().Reverse().ToArray();
        var roles = Entries((Relation)graph.Left).Reverse().ToArray();
        if (kind != "junction")
        {
            roleEdges = roleEdges.Select(edge =>
            {
                var data = (Relation)edge.Right; var ends = (Relation)data.Left;
                var heading = Entries((Relation)data.Right).Select(direction =>
                    Entries(sender.Frame.Opposites).Cast<Relation>().Single(pair => ReferenceEquals(pair.Left, direction)).Right).ToArray();
                return new Relation(edge.Left, new Relation(new Relation(ends.Right, ends.Left), S(heading)));
            }).ToArray();
            if (kind == "cycle") roleEdges = [.. roleEdges.Skip(1), roleEdges[0]];
        }
        var routes = kind == "junction" ? S() : S(S(roleEdges.Select(edge =>
            (IElement)new Relation(edge.Left, ((Relation)edge.Right).Left)).ToArray()));
        var priorDetail = (Relation)original.Definition.Right;
        var definition = new Relation(new Relation(sender.Frame.Surface, new Relation(S(roles), S(roleEdges))),
            new Relation(priorDetail.Left, routes));
        var permuted = new CommunicationPattern(sender.Frame, definition); Assert.True(permuted.IsValid);
        var assessment = new CommunicationAssessment(sender.Read(), new([permuted]));
        Assert.True(assessment.IsSatisfied); Assert.NotEmpty(assessment.Matches); Assert.Equal(0, assessment.RefutedCount);
    }

    [Fact]
    public void Parallel_star_edges_keep_distinct_occurrences_even_when_their_endpoint_relations_are_equal()
    {
        static void AddStar(Fixture fixture)
        {
            var centre = fixture.Site(); var sharedLeaf = fixture.Site();
            fixture.Edge(centre, sharedLeaf, 1); fixture.Edge(centre, sharedLeaf, 1);
            fixture.Edge(centre, fixture.Site(), 4);
        }
        var sender = new Fixture(); AddStar(sender); var pattern = sender.Pattern("junction", 3);
        var receiver = new Fixture(); AddStar(receiver); var output = receiver.Read();
        var required = new CommunicationAssessment(output, new([pattern]));
        Assert.True(required.IsSatisfied); var match = Assert.Single(required.Matches);
        var maps = (Relation)((Relation)((Relation)match.Witness.Right).Right).Right;
        var edgeMaps = Entries((Relation)maps.Right).Cast<Relation>().ToArray();
        Assert.Equal(3, edgeMaps.Length);
        Assert.Equal(3, edgeMaps.Select(row => row.Right).Distinct(ReferenceEqualityComparer.Instance).Count());
        var forbidden = new CommunicationAssessment(output, new([], [pattern]));
        Assert.False(forbidden.IsSatisfied); Assert.Equal(1, forbidden.RefutedCount);
        Assert.Equal(0, forbidden.SupportedCount); Assert.Single(forbidden.Matches);
    }

    [Fact]
    public void A_placeholder_cannot_be_both_a_site_role_and_an_edge_role_in_this_protocol()
    {
        var fixture = new Fixture(); var centre = new Placeholder(); var leaf = new Placeholder();
        var edge = new Relation(centre, new Relation(new Relation(centre, leaf), S(fixture.Frame.Right)));
        var definition = new Relation(new Relation(fixture.Frame.Surface, new Relation(S(centre, leaf), S(edge))),
            new Relation(S(centre), S()));
        var pattern = new CommunicationPattern(fixture.Frame, definition); Assert.False(pattern.IsValid);
        fixture.Edge(fixture.Site(), fixture.Site(), 1);
        var assessment = new CommunicationAssessment(fixture.Read(), new([pattern]));
        Assert.False(assessment.IsSatisfied); Assert.Equal(1, assessment.BlockedCount);
        Assert.Equal(0, assessment.SupportedCount); Assert.Empty(assessment.Matches);
    }

    [Fact]
    public void Foreign_frame_and_unused_role_patterns_cannot_supply_positive_evidence()
    {
        var sender = new Fixture(); sender.Cycle(1, 4, 2, 8); var original = sender.Pattern("cycle", 4);
        var foreign = new CommunicationPattern(new(), original.Definition);
        var head = (Relation)original.Definition.Left; var graph = (Relation)head.Right;
        var extraSites = S(Entries((Relation)graph.Left).Append(new Placeholder()).ToArray());
        var definition = new Relation(new Relation(head.Left, new Relation(extraSites, graph.Right)), original.Definition.Right);
        var unused = new CommunicationPattern(sender.Frame, definition);
        Assert.False(foreign.IsValid); Assert.False(unused.IsValid);
        var output = sender.Read(); var assessment = new CommunicationAssessment(output, new([foreign, unused]));
        Assert.False(assessment.IsSatisfied); Assert.Equal(2, assessment.BlockedCount);
        Assert.Equal(0, assessment.SupportedCount); Assert.Empty(assessment.Matches);
    }

    [Fact]
    public void An_open_message_cannot_claim_that_all_commitments_are_satisfied()
    {
        var sender = new Fixture(); sender.Cycle(1, 4, 2, 8); var pattern = sender.Pattern("cycle", 4);
        var message = new CommunicationMessage(StructuralConstraint.Open(pattern.Surface), S());
        var assessment = new CommunicationAssessment(sender.Read(), message);
        Assert.Equal(1, assessment.SupportedCount); Assert.Equal(1, assessment.UnresolvedCount);
        Assert.False(message.IsComplete); Assert.False(assessment.IsSatisfied);
    }

    [Fact]
    public void Assessment_requires_no_sender_occurrence_or_recipe_and_preserves_the_observation()
    {
        var sender = new Fixture(); var centre = sender.Site();
        foreach (var heading in new[] { 1, 2, 4, 8 }) sender.Edge(centre, sender.Site(), heading);
        var pattern = sender.Pattern("junction", 4);
        var receiver = new Fixture(); var observedCentre = receiver.Site();
        foreach (var heading in new[] { 8, 4, 2, 1 }) receiver.Edge(observedCentre, receiver.Site(), heading);
        var output = receiver.Read(); var priorSites = Entries(output.Observation.Sites);
        var priorSegments = Entries(output.Observation.Segments);
        var messageCopy = new CommunicationMessage(S(new Relation(pattern.Surface.Left, pattern.Surface.Right)), S());
        var assessment = new CommunicationAssessment(output, messageCopy);
        Assert.True(assessment.IsSatisfied); Assert.Single(assessment.Matches);
        Assert.Equal(priorSites, Entries(output.Observation.Sites)); Assert.Equal(priorSegments, Entries(output.Observation.Segments));
        Assert.DoesNotContain(Reachable(pattern.Surface), element => sender.Sites.Any(site => ReferenceEquals(site, element)) ||
            sender.Edges.Any(edge => ReferenceEquals(edge, element)));
        Assert.DoesNotContain(Reachable(assessment.Output.Observation.Surface), element =>
            Reachable(pattern.Surface).Any(source => ReferenceEquals(source, element)));
    }

    private static IElement[] Reachable(IElement root)
    {
        var seen = new HashSet<IElement>(ReferenceEqualityComparer.Instance); var pending = new Stack<IElement>(); pending.Push(root);
        while (pending.TryPop(out var element))
            if (seen.Add(element) && element is Relation relation) { pending.Push(relation.Left); pending.Push(relation.Right); }
        return seen.ToArray();
    }
    private static IElement[] Entries(Relation scope) => StructuralConstraint.TryReadScope(scope, out var entries, out _, out _) ? entries : [];
    private static Relation S(params IElement[] entries) => StructuralConstraint.Closed(entries);
    private sealed class Fixture
    {
        public QualitativeMotionFrame Frame { get; } = new();
        public List<IElement> Sites { get; } = [];
        public List<Relation> Edges { get; } = [];
        public IElement Site() { var site = new Placeholder(); Sites.Add(site); return site; }
        public void Edge(IElement from, IElement to, int mask) => Edges.Add(new(new Placeholder(), new Relation(new Relation(from, to),
            S(new[] { Frame.Right, Frame.Left, Frame.Up, Frame.Down }.Where((_, i) => (mask & 1 << i) != 0).Cast<IElement>().ToArray()))));
        public void Cycle(params int[] headings)
        {
            var sites = headings.Select(_ => Site()).ToArray();
            for (var i = 0; i < headings.Length; i++) Edge(sites[i], sites[(i + 1) % sites.Length], headings[i]);
        }
        public QualitativeMotifInduction Read(int maxCycleEdges = 8, int proposalCapacity = 20000) =>
            new(new(Frame, S(Sites.ToArray()), S(Edges.Cast<IElement>().ToArray())), maxCycleEdges, proposalCapacity);
        public CommunicationPattern Pattern(string kind, int edgeCount)
        {
            var read = Read(); var family = read.Families.First(value => value.Kind == kind && value.SegmentCount == edgeCount);
            return new(Frame, family.Definition);
        }
    }
}
