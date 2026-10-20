using Gojo.Core;
using Gojo.Encoding.Structures;
using Gojo.Experiments.QualitativeExecution;
using Gojo.Experiments.QualitativeFrieze;
using Gojo.Patterns;

namespace QualitativeExecution.Tests;

/// <summary>Independent checks of the advertised finite occurrence inventory
/// and its composition boundary, rather than the drawings or case labels.</summary>
public sealed class OccurrenceCompositionAuditTests
{
    [Fact]
    public void A_native_question_survives_transport_without_its_builder_and_contains_no_numeric_state()
    {
        var sender = new Fixture(); sender.Cycle(1, 6, 8);
        var first = new OccurrenceComposition.Role(sender.Pattern("cycle", 3));
        var second = new OccurrenceComposition.Role(sender.Pattern("cycle", 3));
        var original = new OccurrenceComposition.Question("two separate loops", [first, second],
            distinct: S(OccurrenceComposition.Pair(first, second)),
            disjointSites: S(OccurrenceComposition.Pair(first, second)));
        var json = GojoStructureCompactJson.Serialize(
            GojoStructureCompactJson.FromElement("question", original.Surface));
        Assert.True(GojoStructureCompactJson.TryToElement(GojoStructureCompactJson.Deserialize(json), out var restored));
        var question = new OccurrenceComposition.Question("caption deliberately changed", Assert.IsType<Relation>(restored.Root));
        var receiver = new Fixture(); receiver.Cycle(1, 6, 8); receiver.Cycle(1, 6, 8);
        var assessment = new OccurrenceComposition.Assessment(receiver.Read(), question);

        Assert.Equal("Yes", assessment.Answer);
        Assert.NotEmpty(assessment.Witnesses);
        Assert.DoesNotContain(Reachable(assessment.Surface), participant => participant is Carrier or CarrierLocation);
        Assert.DoesNotContain(Reachable(question.Surface), participant => ReferenceEquals(participant, original.Surface));
    }

    [Fact]
    public void Reusing_one_role_slot_twice_is_not_two_independent_requests()
    {
        var fixture = new Fixture(); fixture.Cycle(1, 6, 8);
        var role = new OccurrenceComposition.Role(fixture.Pattern("cycle", 3));
        var malformed = NativeQuestion(S(role.Surface, role.Surface));
        var question = new OccurrenceComposition.Question("duplicate slot", malformed);
        var assessment = new OccurrenceComposition.Assessment(fixture.Read(), question);

        Assert.Empty(E(assessment.Supported));
        Assert.Empty(E(assessment.Refuted));
        Assert.NotEmpty(E(assessment.Blocked));
    }

    [Fact]
    public void A_constraint_cannot_name_an_unadvertised_role_even_if_its_pattern_is_equal()
    {
        var fixture = new Fixture(); fixture.Cycle(1, 6, 8);
        var pattern = fixture.Pattern("cycle", 3);
        var first = new OccurrenceComposition.Role(pattern);
        var second = new OccurrenceComposition.Role(pattern);
        var hidden = new OccurrenceComposition.Role(pattern);
        var question = new OccurrenceComposition.Question("foreign slot", [first, second],
            distinct: S(OccurrenceComposition.Pair(first, hidden)));
        var assessment = new OccurrenceComposition.Assessment(fixture.Read(), question);

        Assert.Empty(E(assessment.Supported));
        Assert.Empty(E(assessment.Refuted));
        Assert.NotEmpty(E(assessment.Blocked));
    }

    [Fact]
    public void Open_role_inventory_cannot_establish_a_closed_composition_answer()
    {
        var fixture = new Fixture(); fixture.Cycle(1, 6, 8);
        var role = new OccurrenceComposition.Role(fixture.Pattern("cycle", 3));
        var question = new OccurrenceComposition.Question("unfinished composition",
            NativeQuestion(StructuralConstraint.Open(role.Surface)));
        var assessment = new OccurrenceComposition.Assessment(fixture.Read(), question);

        Assert.Empty(E(assessment.Supported));
        Assert.Empty(E(assessment.Refuted));
        Assert.True(E(assessment.Open).Length + E(assessment.Blocked).Length > 0);
    }

    [Fact]
    public void A_complete_motif_inventory_does_not_make_zero_assignment_capacity_a_negative_answer()
    {
        var fixture = new Fixture(); fixture.Cycle(1, 6, 8); fixture.Cycle(1, 6, 8);
        var first = new OccurrenceComposition.Role(fixture.Pattern("cycle", 3));
        var second = new OccurrenceComposition.Role(fixture.Pattern("cycle", 3));
        var question = new OccurrenceComposition.Question("two footprints", [first, second],
            distinct: S(OccurrenceComposition.Pair(first, second)));
        var inventory = fixture.Read(); Assert.True(inventory.IsComplete);
        var assessment = new OccurrenceComposition.Assessment(inventory, question, assignmentCapacity: 0);

        Assert.Empty(E(assessment.Supported));
        Assert.Empty(E(assessment.Refuted));
        Assert.NotEmpty(E(assessment.Open));
        Assert.Empty(assessment.Witnesses);
    }

    [Fact]
    public void A_certified_local_witness_can_answer_yes_despite_unfinished_other_inventory_search()
    {
        var sender = new Fixture(); sender.Edge(sender.Site(), sender.Site(), 1);
        var role = new OccurrenceComposition.Role(sender.Pattern("junction", 1));
        var question = new OccurrenceComposition.Question("one actual endpoint footprint", [role]);
        var receiver = new Fixture(); receiver.Edge(receiver.Site(), receiver.Site(), 1);
        var inventory = receiver.Read(proposalCapacity: 1); Assert.False(inventory.IsComplete);
        var assessment = new OccurrenceComposition.Assessment(inventory, question);

        Assert.Equal("Yes", assessment.Answer);
        Assert.NotEmpty(assessment.Witnesses);
        Assert.Empty(E(assessment.Refuted));
    }

    [Fact]
    public void An_unresolved_heading_is_not_proof_of_absence_of_a_loop()
    {
        var sender = new Fixture(); sender.Cycle(1, 6, 8);
        var question = new OccurrenceComposition.Question("closed loop",
            [new OccurrenceComposition.Role(sender.Pattern("cycle", 3))]);
        var receiver = new Fixture(); var a = receiver.Site(); var b = receiver.Site(); var c = receiver.Site();
        receiver.Edge(a, b, 1); receiver.Edge(b, c, 6);
        receiver.Edges.Add(new(new Placeholder(), new Relation(new Relation(c, a), StructuralConstraint.Open())));
        var inventory = receiver.Read(); Assert.False(inventory.IsComplete);
        var assessment = new OccurrenceComposition.Assessment(inventory, question);

        Assert.Empty(E(assessment.Supported));
        Assert.Empty(E(assessment.Refuted));
        Assert.NotEmpty(E(assessment.Open));
    }

    [Fact]
    public void A_valid_two_edge_cycle_outside_the_inventory_grammar_stays_open()
    {
        var fixture = new Fixture(); var a = fixture.Site(); var b = fixture.Site();
        fixture.Edge(a, b, 1); fixture.Edge(b, a, 2);
        var roleA = new Placeholder(); var roleB = new Placeholder();
        var edgeA = new Placeholder(); var edgeB = new Placeholder();
        var forward = new Relation(roleA, roleB); var backward = new Relation(roleB, roleA);
        var definition = new Relation(new Relation(fixture.Frame.Surface,
                new Relation(S(roleA, roleB), S(
                    new Relation(edgeA, new Relation(forward, S(fixture.Frame.Right))),
                    new Relation(edgeB, new Relation(backward, S(fixture.Frame.Left)))))),
            new Relation(S(), S(S(new Relation(edgeA, forward), new Relation(edgeB, backward)))));
        var pattern = new CommunicationPattern(fixture.Frame, definition); Assert.True(pattern.IsValid);
        var question = new OccurrenceComposition.Question("two-edge closure", [new OccurrenceComposition.Role(pattern)]);
        var inventory = fixture.Read(); Assert.True(inventory.IsComplete);
        var assessment = new OccurrenceComposition.Assessment(inventory, question);

        Assert.Empty(E(assessment.Supported));
        Assert.Empty(E(assessment.Refuted));
        Assert.NotEmpty(E(assessment.Open));
    }

    [Fact]
    public void Duplicate_observation_rows_do_not_manufacture_a_second_physical_footprint()
    {
        var fixture = new Fixture(); fixture.Cycle(1, 6, 8);
        var first = new OccurrenceComposition.Role(fixture.Pattern("cycle", 3));
        var second = new OccurrenceComposition.Role(fixture.Pattern("cycle", 3));
        var question = new OccurrenceComposition.Question("different footprints", [first, second],
            distinct: S(OccurrenceComposition.Pair(first, second)));
        var observation = new QualitativeRecognitionObservation(fixture.Frame,
            S(fixture.Sites.ToArray()), S(fixture.Edges.Concat(fixture.Edges).Cast<IElement>().ToArray()));
        var inventory = new QualitativeMotifInduction(observation);
        var assessment = new OccurrenceComposition.Assessment(inventory, question);

        Assert.Empty(E(assessment.Supported));
        Assert.Empty(E(assessment.Refuted));
        Assert.NotEmpty(E(assessment.Open));
    }

    [Fact]
    public void Different_endpoint_reads_of_one_line_are_not_different_edge_footprints()
    {
        var right = new Fixture(); right.Edge(right.Site(), right.Site(), 1);
        var left = new Fixture(); left.Edge(left.Site(), left.Site(), 2);
        var first = new OccurrenceComposition.Role(right.Pattern("junction", 1));
        var second = new OccurrenceComposition.Role(left.Pattern("junction", 1));
        var available = new OccurrenceComposition.Question("both endpoint readings", [first, second]);
        var distinct = new OccurrenceComposition.Question("different actual edge sets", [first, second],
            distinct: S(OccurrenceComposition.Pair(first, second)));
        var inventory = right.Read(); Assert.True(inventory.IsComplete);

        Assert.Equal("Yes", new OccurrenceComposition.Assessment(inventory, available).Answer);
        var assessment = new OccurrenceComposition.Assessment(inventory, distinct);
        Assert.Equal("No", assessment.Answer);
        Assert.Empty(assessment.Witnesses);
        Assert.NotEmpty(E(assessment.Refuted));
    }

    private static Relation NativeQuestion(Relation roles) => new(roles,
        new Relation(S(), new Relation(S(), new Relation(S(), new Relation(S(), S())))));
    private static Relation S(params IElement[] entries) => StructuralConstraint.Closed(entries);
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

    private sealed class Fixture
    {
        public QualitativeMotionFrame Frame { get; } = new();
        public List<IElement> Sites { get; } = [];
        public List<Relation> Edges { get; } = [];
        public IElement Site() { var site = new Placeholder(); Sites.Add(site); return site; }
        public void Edge(IElement from, IElement to, int mask) => Edges.Add(new(new Placeholder(),
            new Relation(new Relation(from, to), S(new[] { Frame.Right, Frame.Left, Frame.Up, Frame.Down }
                .Where((_, index) => (mask & 1 << index) != 0).Cast<IElement>().ToArray()))));
        public void Cycle(params int[] headings)
        {
            var sites = headings.Select(_ => Site()).ToArray();
            for (var index = 0; index < headings.Length; index++)
                Edge(sites[index], sites[(index + 1) % sites.Length], headings[index]);
        }
        public QualitativeRecognitionObservation Observe() => new(Frame, S(Sites.ToArray()), S(Edges.Cast<IElement>().ToArray()));
        public QualitativeMotifInduction Read(int proposalCapacity = 4096) => new(Observe(),
            maxCycleEdges: 6, proposalCapacity: proposalCapacity, retainedCapacity: 1024);
        public CommunicationPattern Pattern(string kind, int edges)
        {
            var family = Read().Families.First(candidate => candidate.Kind == kind && candidate.SegmentCount == edges);
            return new(Frame, family.Definition);
        }
    }
}
