using Gojo.Core;
using Gojo.Encoding.Structures;
using Gojo.Experiments.QualitativeExecution;
using Gojo.Experiments.QualitativeFrieze;
using Gojo.Patterns;

namespace QualitativeExecution.Tests;

/// <summary>Independent contracts for concept equivalence, deliberately using
/// graphs outside the authored ornament catalogue and no generating labels.</summary>
public sealed class OrnamentArrangementAuditTests
{
    [Fact]
    public void Subdivision_is_a_declared_freedom_rather_than_an_exact_graph_match()
    {
        var simple = Polygon(4); var subdivided = Polygon(9);
        var coarse = new OrnamentArrangementRead.Question("closed contour", simple.Observe());
        var exact = new OrnamentArrangementRead.Question("four actual sites", simple.Observe(), allowSubdivision: false);

        Assert.Equal("Yes", new OrnamentArrangementRead.Receipt(coarse, subdivided.Observe()).Answer);
        Assert.Equal("No", new OrnamentArrangementRead.Receipt(exact, subdivided.Observe()).Answer);
        Assert.Equal("No", new OrnamentReconstructionGraphRead(simple.Observe(), subdivided.Observe(), includeRelations: false).Answer);
        Assert.Equal(4, simple.Edges.Count); Assert.Equal(9, subdivided.Edges.Count);
    }

    [Fact]
    public void Equal_degree_inventory_and_cycle_rank_do_not_prove_the_same_arrangement()
    {
        var bipartite = new Fixture(6);
        for (var left = 0; left < 3; left++)
            for (var right = 3; right < 6; right++) bipartite.Edge(left, right);
        var prism = new Fixture(6);
        foreach (var offset in new[] { 0, 3 })
        {
            prism.Edge(offset, offset + 1); prism.Edge(offset + 1, offset + 2); prism.Edge(offset + 2, offset);
        }
        for (var index = 0; index < 3; index++) prism.Edge(index, index + 3);

        Assert.Equal(Degrees(bipartite), Degrees(prism));
        Assert.Equal(bipartite.Edges.Count, prism.Edges.Count);
        var question = new OrnamentArrangementRead.Question("connected branch arrangement", bipartite.Observe());
        var receipt = new OrnamentArrangementRead.Receipt(question, prism.Observe());
        Assert.Equal("No", receipt.Answer); Assert.Empty(E(receipt.Supported));
    }

    [Fact]
    public void Returning_routes_and_parallel_routes_are_not_interchangeable()
    {
        var theta = new Fixture(5);
        foreach (var via in new[] { 2, 3, 4 }) { theta.Edge(0, via); theta.Edge(via, 1); }
        var barbell = new Fixture(6);
        barbell.Edge(0, 2); barbell.Edge(2, 3); barbell.Edge(3, 0);
        barbell.Edge(1, 4); barbell.Edge(4, 5); barbell.Edge(5, 1); barbell.Edge(0, 1);
        var question = new OrnamentArrangementRead.Question("three routes between the same anchors", theta.Observe());

        Assert.Equal("No", new OrnamentArrangementRead.Receipt(question, barbell.Observe()).Answer);
        Assert.Equal("Yes", new OrnamentArrangementRead.Receipt(question, theta.Observe()).Answer);
    }

    [Fact]
    public void A_closed_cycle_keeps_nonempty_traversal_and_cannot_match_an_open_chain()
    {
        var cycle = Polygon(6); var chain = new Fixture(7);
        for (var index = 0; index < 6; index++) chain.Edge(index, index + 1);
        var question = new OrnamentArrangementRead.Question("returning traversal", cycle.Observe());
        var positive = new OrnamentArrangementRead.Receipt(question, Polygon(3).Observe());

        Assert.Equal("Yes", positive.Answer);
        Assert.NotEmpty(E(Assert.IsType<Relation>(positive.Correspondence.Left)));
        Assert.Equal("No", new OrnamentArrangementRead.Receipt(question, chain.Observe()).Answer);
        Assert.NotEmpty(OrnamentReconstruction.Participants(positive.TargetResolution));
    }

    [Fact]
    public void Complete_cover_rejects_uncommunicated_branches_copies_and_isolated_sites()
    {
        var source = Polygon(4); var question = new OrnamentArrangementRead.Question("one whole contour", source.Observe());
        var branch = Polygon(4); branch.Site(); branch.Edge(0, 4);
        var copies = new Fixture(6);
        for (var start = 0; start < 6; start += 3)
        { copies.Edge(start, start + 1); copies.Edge(start + 1, start + 2); copies.Edge(start + 2, start); }
        var isolated = Polygon(4); isolated.Site();

        Assert.Equal("No", new OrnamentArrangementRead.Receipt(question, branch.Observe()).Answer);
        Assert.Equal("No", new OrnamentArrangementRead.Receipt(question, copies.Observe()).Answer);
        Assert.Equal("No", new OrnamentArrangementRead.Receipt(question, isolated.Observe()).Answer);
    }

    [Fact]
    public void Unselected_heading_and_encounter_detail_do_not_prevent_a_topological_read()
    {
        var source = Polygon(4); var target = Polygon(4);
        var question = new OrnamentArrangementRead.Question("topology with carried geometry", source.Observe());
        var unknown = target.Observe(unknownHeadings: true, unknownEncounters: true);
        var receipt = new OrnamentArrangementRead.Receipt(question, unknown);

        Assert.Equal("Yes", receipt.Answer);
        Assert.Contains(unknown.Crossings, OrnamentReconstruction.Participants(receipt.Carried));
        Assert.Empty(E(receipt.Open));
    }

    [Fact]
    public void Incomplete_selected_scope_and_exhausted_correspondence_do_not_become_absence()
    {
        var source = Polygon(4); var target = Polygon(4);
        var question = new OrnamentArrangementRead.Question("one complete contour", source.Observe());
        var open = new QualitativeRecognitionObservation(target.Frame, S(target.Sites.ToArray()),
            StructuralConstraint.Open(target.Edges.Cast<IElement>().ToArray()));

        var incomplete = new OrnamentArrangementRead.Receipt(question, open);
        Assert.Equal("Open", incomplete.Answer); Assert.Empty(E(incomplete.Refuted));
        var budget = new OrnamentArrangementRead.Receipt(question, target.Observe(), capacity: 0);
        Assert.Equal("Open", budget.Answer); Assert.Empty(E(budget.Refuted));
    }

    [Fact]
    public void A_source_free_transported_question_retains_its_law_without_source_identity()
    {
        var source = Polygon(4); var observation = source.Observe();
        var question = new OrnamentArrangementRead.Question("transported returning contour", observation);
        var participants = OrnamentReconstruction.Participants(question.Surface);
        Assert.DoesNotContain(observation.Surface, participants);
        Assert.All(source.Sites, site => Assert.DoesNotContain(site, participants));
        Assert.All(source.Edges, edge => Assert.DoesNotContain(edge, participants));
        Assert.DoesNotContain(participants, element => element is Carrier or CarrierLocation);

        var json = GojoStructureCompactJson.Serialize(GojoStructureCompactJson.FromElement("concept contract", question.Surface));
        Assert.True(GojoStructureCompactJson.TryToElement(GojoStructureCompactJson.Deserialize(json), out var restored));
        var imported = new OrnamentArrangementRead.Question("caption has no semantic role", Assert.IsType<Relation>(restored.Root));
        Assert.Equal("Yes", new OrnamentArrangementRead.Receipt(imported, Polygon(7).Observe()).Answer);
        Assert.Equal("No", new OrnamentArrangementRead.Receipt(imported, new Fixture(2).Edge(0, 1).Observe()).Answer);
    }

    [Fact]
    public void Native_site_aliases_do_not_manufacture_an_extra_branch_or_break_a_cycle()
    {
        var source = Polygon(4); var target = new Fixture(4);
        target.Edge(0, 1); target.Edge(1, 2); target.Edge(2, 3);
        target.Edges.Add(new(new Placeholder(), new Relation(new Relation(target.Sites[3], target.Sites[0].Dual().Dual()), S(target.Frame.Right))));
        var question = new OrnamentArrangementRead.Question("actual native sites", source.Observe());
        Assert.Equal("Yes", new OrnamentArrangementRead.Receipt(question, target.Observe()).Answer);
    }

    [Fact]
    public void Open_anchor_contract_is_not_a_closed_default_and_missing_route_participation_is_blocked()
    {
        var source = Polygon(4); var target = Polygon(6);
        var question = new OrnamentArrangementRead.Question("native complete partition", source.Observe());
        var law = Assert.IsType<Relation>(question.Surface.Right);
        var routeScopes = Assert.IsType<Relation>(law.Right);
        var open = new OrnamentArrangementRead.Question("unfinished anchor choice", new Relation(question.Surface.Left,
            new Relation(StructuralConstraint.Open(E(Assert.IsType<Relation>(law.Left))), routeScopes)));
        var openReceipt = new OrnamentArrangementRead.Receipt(open, target.Observe());
        Assert.Equal("Open", openReceipt.Answer); Assert.Empty(E(openReceipt.Refuted));

        var omitted = new OrnamentArrangementRead.Question("unpaid route omission", new Relation(question.Surface.Left,
            new Relation(law.Left, new Relation(routeScopes.Left, S(E(Assert.IsType<Relation>(routeScopes.Right)).Skip(1).ToArray())))));
        var missingReceipt = new OrnamentArrangementRead.Receipt(omitted, target.Observe());
        Assert.Equal("Blocked", missingReceipt.Answer); Assert.Empty(E(missingReceipt.Supported));
    }

    [Fact]
    public void Empty_structure_is_distinct_from_an_isolated_participant_and_a_closed_cycle()
    {
        var empty = new Fixture(0); var point = new Fixture(1);
        var question = new OrnamentArrangementRead.Question("empty whole", empty.Observe());
        Assert.Equal("Yes", new OrnamentArrangementRead.Receipt(question, new Fixture(0).Observe(), capacity: 0).Answer);
        Assert.Equal("No", new OrnamentArrangementRead.Receipt(question, point.Observe()).Answer);
        Assert.Equal("No", new OrnamentArrangementRead.Receipt(question, Polygon(3).Observe()).Answer);
        var isolated = new OrnamentArrangementRead.Question("one isolated participant", point.Observe());
        Assert.Equal("Yes", new OrnamentArrangementRead.Receipt(isolated, new Fixture(1).Observe()).Answer);
        Assert.Equal("No", new OrnamentArrangementRead.Receipt(isolated, empty.Observe()).Answer);
    }

    [Fact]
    public void A_mixed_atomic_and_subdividable_contract_exposes_the_unimplemented_law()
    {
        var graph = Polygon(4); var original = new OrnamentArrangementRead.Question("coarse contour", graph.Observe());
        var body = Assert.IsType<Relation>(original.Surface.Right);
        var scopes = Assert.IsType<Relation>(body.Right);
        var paths = E(Assert.IsType<Relation>(scopes.Right));
        var mixed = new OrnamentArrangementRead.Question("one fixed edge and remaining free routes", new Relation(original.Surface.Left,
            new Relation(body.Left, new Relation(S(paths[0]), S(paths.Skip(1).ToArray())))));
        var receipt = new OrnamentArrangementRead.Receipt(mixed, Polygon(4).Observe());
        Assert.Equal("Open", receipt.Answer); Assert.Empty(E(receipt.Supported)); Assert.Empty(E(receipt.Refuted));
    }

    [Fact]
    public void Equal_topology_does_not_claim_equal_crossing_or_flow_read()
    {
        var crossing = new Fixture(4); crossing.Edge(0, 1); crossing.Edge(2, 3);
        var withEncounter = new QualitativeRecognitionObservation(crossing.Frame, S(crossing.Sites.ToArray()),
            S(crossing.Edges.Cast<IElement>().ToArray()), crossings: S(new Relation(crossing.Edges[0], crossing.Edges[1])));
        var unrelated = new Fixture(4); unrelated.Edge(0, 1); unrelated.Edge(2, 3);
        var target = unrelated.Observe();
        var question = new OrnamentArrangementRead.Question("two independent routes", withEncounter, allowSubdivision: false);
        var receipt = new OrnamentArrangementRead.Receipt(question, target);
        Assert.Equal("Yes", receipt.Answer);
        Assert.Equal("No", new OrnamentReconstructionGraphRead(withEncounter, target).Answer);
        Assert.Contains(target.Crossings, OrnamentReconstruction.Participants(receipt.Carried));
    }

    [Fact]
    public void Crystallized_returning_routes_keep_their_attachment_and_nonzero_cycle_participation()
    {
        var barbell = new Fixture(10);
        foreach (var start in new[] { 0, 5 })
            for (var index = 0; index < 5; index++) barbell.Edge(start + index, start + (index + 1) % 5);
        barbell.Edge(0, 5);
        var source = barbell.Observe();
        var compact = OrnamentArrangementRead.Crystallize("two returning routes with a bridge", source, out var witness);
        Assert.Equal(6, E(compact.Template.Sites).Length);
        Assert.Equal(7, E(compact.Template.Segments).Length);
        Assert.Equal("Yes", new OrnamentArrangementRead.Receipt(compact, source).Answer);
        Assert.Equal("No", new OrnamentArrangementRead.Receipt(compact, Polygon(11).Observe()).Answer);
        Assert.DoesNotContain(source.Surface, OrnamentReconstruction.Participants(compact.Surface));
        Assert.Contains(source.Surface, OrnamentReconstruction.Participants(witness));
    }

    private static Fixture Polygon(int sides)
    {
        var fixture = new Fixture(sides);
        for (var index = 0; index < sides; index++) fixture.Edge(index, (index + 1) % sides);
        return fixture;
    }
    private static int[] Degrees(Fixture fixture) => fixture.Sites.Select(site => fixture.Edges.Count(edge =>
        ((Relation)((Relation)edge.Right).Left).Left.Equals(site) || ((Relation)((Relation)edge.Right).Left).Right.Equals(site)))
        .Order().ToArray();
    private static Relation S(params IElement[] entries) => StructuralConstraint.Closed(entries);
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);

    private sealed class Fixture
    {
        internal QualitativeMotionFrame Frame { get; } = new();
        internal List<IElement> Sites { get; } = [];
        internal List<Relation> Edges { get; } = [];
        internal Fixture(int sites) { for (var index = 0; index < sites; index++) Site(); }
        internal void Site() => Sites.Add(new Placeholder());
        internal Fixture Edge(int from, int to)
        {
            Edges.Add(new(new Placeholder(), new Relation(new Relation(Sites[from], Sites[to]), S(Frame.Right)))); return this;
        }
        internal QualitativeRecognitionObservation Observe(bool unknownHeadings = false, bool unknownEncounters = false) =>
            new(Frame, S(Sites.ToArray()), S((unknownHeadings ? Edges.Select(edge => new Relation(edge.Left,
                new Relation(((Relation)edge.Right).Left, StructuralConstraint.Open()))) : Edges).Cast<IElement>().ToArray()),
                crossings: unknownEncounters ? StructuralConstraint.Open() : S());
    }
}
