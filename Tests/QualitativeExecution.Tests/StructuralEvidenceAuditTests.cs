using Gojo.Core;
using Gojo.Experiments.QualitativeExecution;
using Gojo.Experiments.QualitativeFrieze;
using Gojo.Patterns;

namespace QualitativeExecution.Tests;

public sealed class StructuralEvidenceAuditTests
{
    private static readonly Lazy<StructuralEvidenceCases> Cases = new(StructuralEvidenceCases.Build);

    [Fact]
    public void All_structural_cases_use_independent_executions_and_keep_unanswerable_recipe_ambiguity()
    {
        Assert.Equal(6, Cases.Value.Cases.Count);
        foreach (var scenario in Cases.Value.Cases)
        {
            var receiver = scenario.CreateReceiver(); var supplier = scenario.CreateSupplier();
            Assert.Empty(receiver.Responses);
            foreach (var candidate in scenario.Candidates)
            {
                Assert.NotSame(scenario.Source.Execution, candidate.Execution);
                Assert.NotSame(scenario.Source.Observation.Surface, candidate.Observation.Surface);
            }
            foreach (var query in scenario.Queries) receiver.Receive(supplier.Request(receiver, query));
            Assert.Empty(E(receiver.Rejected)); Assert.Empty(E(receiver.Selection.Conflicts));
            if (scenario.ExpectedAmbiguity) Assert.True(receiver.Selection.Eligible.Count > 1);
            else Assert.Single(receiver.Selection.Eligible);
            var read = Reachable(receiver.Surface).ToArray();
            foreach (var privateRoot in new[] { scenario.Source.Program.Surface,
                         scenario.Source.Execution.Current.Surface, scenario.Source.Join.Surface,
                         scenario.Source.Publisher.Surface, scenario.Source.Observation.Surface })
                Assert.DoesNotContain(read, element => ReferenceEquals(element, privateRoot));
        }
    }

    [Fact]
    public void A_positive_reply_does_not_publish_the_suppliers_full_inventory_or_other_components()
    {
        var sender = new Fixture(); sender.Cycle(1, 4, 2, 8);
        var query = new StructuralEvidenceInquiry.Query("closed contour", sender.Pattern("cycle", 4));
        var source = new Fixture(); source.Cycle(1, 4, 2, 8);
        var hiddenA = source.Site(); var hiddenB = source.Site(); source.Edge(hiddenA, hiddenB, 5);
        var observation = source.Observe();
        var candidate = new StructuralEvidenceInquiry.Candidate("square", sender.Observe());
        var receiver = new StructuralEvidenceInquiry.Receiver([candidate], [query]);
        var supplier = new StructuralEvidenceInquiry.Supplier(observation, [query]);
        var response = supplier.Request(receiver, query);

        Assert.NotEmpty(E(response.Supported));
        var disclosed = Reachable(response.Surface).ToArray();
        foreach (var privateRoot in new[] { observation.Surface, observation.Sites, observation.Segments,
                     observation.Continuations, observation.Crossings })
            Assert.DoesNotContain(disclosed, element => ReferenceEquals(element, privateRoot));
        foreach (var privateSite in new[] { hiddenA, hiddenB })
            Assert.DoesNotContain(disclosed, element => ReferenceEquals(element, privateSite));
        receiver.Receive(response);
        Assert.Single(receiver.Responses);
    }

    [Fact]
    public void An_existential_reply_publishes_one_occurrence_without_disclosing_the_repetition_inventory()
    {
        var sender = new Fixture(); sender.Cycle(1, 4, 2, 8);
        var query = new StructuralEvidenceInquiry.Query("one closed contour", sender.Pattern("cycle", 4));
        var source = new Fixture();
        for (var copy = 0; copy < 3; copy++) source.Cycle(1, 4, 2, 8);
        var observation = source.Observe();
        var receiver = new StructuralEvidenceInquiry.Receiver(
            [new StructuralEvidenceInquiry.Candidate("square", sender.Observe())], [query]);
        var response = new StructuralEvidenceInquiry.Supplier(observation, [query]).Request(receiver, query);

        Assert.NotEmpty(E(response.Supported));
        var disclosed = Reachable(response.Surface).ToHashSet(ReferenceEqualityComparer.Instance);
        Assert.Equal(4, source.Sites.Count(disclosed.Contains));
        Assert.Equal(4, source.Edges.Count(disclosed.Contains));
        Assert.DoesNotContain(observation.Surface, disclosed);
        Assert.DoesNotContain(observation.Segments, disclosed);
    }

    [Fact]
    public void A_negative_reply_exposes_a_bounded_read_contract_without_the_suppliers_other_edges()
    {
        var sender = new Fixture(); sender.Cycle(1, 4, 2, 8);
        var query = new StructuralEvidenceInquiry.Query("closed contour", sender.Pattern("cycle", 4));
        var source = new Fixture(); var a = source.Site(); var b = source.Site(); source.Edge(a, b, 1);
        var observation = source.Observe();
        var receiver = new StructuralEvidenceInquiry.Receiver(
            [new StructuralEvidenceInquiry.Candidate("closed alternative", sender.Observe())], [query]);
        var supplier = new StructuralEvidenceInquiry.Supplier(observation, [query]);
        var response = supplier.Request(receiver, query);

        Assert.NotEmpty(E(response.Refuted)); Assert.Empty(E(response.Open));
        var disclosed = Reachable(response.Surface).ToArray();
        Assert.DoesNotContain(disclosed, element => ReferenceEquals(element, observation.Surface));
        Assert.DoesNotContain(disclosed, element => ReferenceEquals(element, observation.Segments));
        Assert.All(source.Edges, edge =>
            Assert.DoesNotContain(disclosed, element => ReferenceEquals(element, edge)));
    }

    [Fact]
    public void An_exhausted_supplier_cannot_publish_absence_of_an_unsearched_cycle()
    {
        var sender = new Fixture(); sender.Cycle(1, 4, 2, 8);
        var query = new StructuralEvidenceInquiry.Query("closed contour", sender.Pattern("cycle", 4));
        var source = new Fixture(); source.Cycle(1, 4, 2, 8);
        var receiver = new StructuralEvidenceInquiry.Receiver(
            [new StructuralEvidenceInquiry.Candidate("closed alternative", sender.Observe())], [query]);
        var supplier = new StructuralEvidenceInquiry.Supplier(source.Observe(), [query], proposalCapacity: 0);
        var response = supplier.Request(receiver, query);

        Assert.Empty(E(response.Refuted)); Assert.Empty(E(response.Supported));
        Assert.NotEmpty(E(response.Open));
        receiver.Receive(response);
        Assert.Single(receiver.Selection.Eligible);
    }

    [Fact]
    public void A_valid_two_edge_cycle_outside_the_induction_grammar_stays_open_at_the_supplier_seam()
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
        var query = new StructuralEvidenceInquiry.Query("two-edge closure", pattern);
        var observation = fixture.Observe();
        var receiver = new StructuralEvidenceInquiry.Receiver(
            [new StructuralEvidenceInquiry.Candidate("parallel-edge candidate", observation)], [query]);
        var response = new StructuralEvidenceInquiry.Supplier(observation, [query]).Request(receiver, query);

        Assert.Empty(E(response.Supported)); Assert.Empty(E(response.Refuted));
        Assert.NotEmpty(E(response.Open));
        receiver.Receive(response);
        Assert.Single(receiver.Selection.Eligible);
    }

    [Fact]
    public void Another_receiver_cannot_consume_a_reply_even_when_it_advertises_the_same_query_object()
    {
        var fixture = new Fixture(); fixture.Cycle(1, 4, 2, 8);
        var query = new StructuralEvidenceInquiry.Query("closed contour", fixture.Pattern("cycle", 4));
        var candidate = new StructuralEvidenceInquiry.Candidate("candidate", fixture.Observe());
        var first = new StructuralEvidenceInquiry.Receiver([candidate], [query]);
        var second = new StructuralEvidenceInquiry.Receiver([candidate], [query]);
        var response = new StructuralEvidenceInquiry.Supplier(fixture.Observe(), [query]).Request(first, query);
        var before = second.Selection;

        Assert.Same(before, second.Receive(response));
        Assert.Empty(second.Responses); Assert.NotEmpty(E(second.Rejected));
        Assert.Single(first.Receive(response).Eligible);
        Assert.Single(first.Responses);
        var admitted = first.Selection;
        Assert.Same(admitted, first.Receive(response));
        Assert.Single(first.Responses); Assert.NotEmpty(E(first.Rejected));
    }

    [Fact]
    public void A_lookalike_query_cannot_read_a_service_that_only_exposes_the_actual_query()
    {
        var fixture = new Fixture(); fixture.Cycle(1, 4, 2, 8);
        var query = new StructuralEvidenceInquiry.Query("actual port", fixture.Pattern("cycle", 4));
        var copy = new StructuralEvidenceInquiry.Query("actual port", query.Pattern);
        var receiver = new StructuralEvidenceInquiry.Receiver(
            [new StructuralEvidenceInquiry.Candidate("candidate", fixture.Observe())], [query]);
        var supplier = new StructuralEvidenceInquiry.Supplier(fixture.Observe(), [query]);
        var response = supplier.Request(receiver, copy); var before = receiver.Selection;

        Assert.Empty(E(response.Supported)); Assert.Empty(E(response.Refuted));
        Assert.Same(before, receiver.Receive(response));
        Assert.Empty(receiver.Responses); Assert.Empty(receiver.Requested);
        Assert.NotEmpty(E(receiver.Rejected));
    }

    [Fact]
    public void Hidden_supplier_choice_and_display_labels_do_not_change_acquisition_features()
    {
        var square = new Fixture(); square.Cycle(1, 4, 2, 8);
        var triangle = new Fixture(); triangle.Cycle(1, 10, 4);
        var squareQuery = new StructuralEvidenceInquiry.Query("square caption", square.Pattern("cycle", 4));
        var triangleQuery = new StructuralEvidenceInquiry.Query("triangle caption", triangle.Pattern("cycle", 3));
        var candidates = new[] { new StructuralEvidenceInquiry.Candidate("first", square.Observe()),
            new StructuralEvidenceInquiry.Candidate("second", triangle.Observe()) };
        var first = new StructuralEvidenceInquiry.Receiver(candidates, [squareQuery, triangleQuery]);
        var renamedSquare = new StructuralEvidenceInquiry.Query("a different caption", squareQuery.Pattern);
        var renamedTriangle = new StructuralEvidenceInquiry.Query("yet another caption", triangleQuery.Pattern);
        var second = new StructuralEvidenceInquiry.Receiver(candidates, [renamedSquare, renamedTriangle]);
        var squareObservation = square.Observe(); var triangleObservation = triangle.Observe();
        var squareSupplier = new StructuralEvidenceInquiry.Supplier(squareObservation, [squareQuery, triangleQuery]);
        var triangleSupplier = new StructuralEvidenceInquiry.Supplier(triangleObservation, [renamedSquare, renamedTriangle]);
        var basis = S(new Placeholder(), new Placeholder(), new Placeholder(), new Placeholder(), new Placeholder());

        for (var index = 0; index < first.Queries.Count; index++)
        {
            var left = StructuralEvidenceInquiry.Features(first, first.Queries[index], basis);
            var right = StructuralEvidenceInquiry.Features(second, second.Queries[index], basis);
            Assert.Equal(left.Values, right.Values);
            foreach (var source in new[] { squareObservation.Surface, triangleObservation.Surface })
            {
                Assert.DoesNotContain(Reachable(left.Surface), element => ReferenceEquals(element, source));
                Assert.DoesNotContain(Reachable(right.Surface), element => ReferenceEquals(element, source));
            }
        }
        Assert.Same(candidates[0], Assert.Single(first.Receive(squareSupplier.Request(first, squareQuery)).Eligible).Candidate);
        Assert.Same(candidates[1], Assert.Single(second.Receive(triangleSupplier.Request(second, renamedSquare)).Eligible).Candidate);
    }

    [Fact]
    public void A_declared_nonjoining_crossing_does_not_supply_a_shared_four_arm_junction()
    {
        var joined = new Fixture(); var centre = joined.Site();
        foreach (var heading in new[] { 1, 2, 4, 8 }) joined.Edge(centre, joined.Site(), heading);
        var query = new StructuralEvidenceInquiry.Query("four shared arms", joined.Pattern("junction", 4));
        var crossed = new Fixture();
        crossed.Edge(crossed.Site(), crossed.Site(), 1);
        crossed.Edge(crossed.Site(), crossed.Site(), 4);
        var crossing = new Relation(crossed.Edges[0], crossed.Edges[1]);
        var observation = crossed.Observe(S(crossing));
        Assert.True(observation.IsComplete);
        Assert.True(observation.TryReadCrossing(crossing, out _, out _));
        var receiver = new StructuralEvidenceInquiry.Receiver(
            [new StructuralEvidenceInquiry.Candidate("joined", joined.Observe()),
                new StructuralEvidenceInquiry.Candidate("crossing", observation)], [query]);
        var response = new StructuralEvidenceInquiry.Supplier(observation, [query]).Request(receiver, query);

        Assert.Empty(E(response.Supported)); Assert.NotEmpty(E(response.Refuted));
        var selected = Assert.Single(receiver.Receive(response).Eligible);
        Assert.Same(receiver.Candidates[1], selected.Candidate);
    }

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
            for (var index = 0; index < sites.Length; index++)
                Edge(sites[index], sites[(index + 1) % sites.Length], headings[index]);
        }
        public QualitativeRecognitionObservation Observe(Relation? crossings = null) =>
            new(Frame, S(Sites.ToArray()), S(Edges.Cast<IElement>().ToArray()), crossings: crossings);
        public CommunicationPattern Pattern(string kind, int length)
        {
            var read = new QualitativeMotifInduction(Observe());
            var family = read.Families.First(item => item.Kind == kind && item.SegmentCount == length);
            return new(Frame, family.Definition);
        }
    }
}
