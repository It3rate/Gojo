using Gojo.Core;
using Gojo.Encoding.Structures;
using Gojo.Experiments.QualitativeExecution;
using Gojo.Experiments.QualitativeFrieze;
using Gojo.Patterns;

namespace QualitativeExecution.Tests;

public sealed class OrnamentReconstructionGraphReadTests
{
    [Fact]
    public void Independent_fresh_observations_have_certified_bijections_without_shared_site_identity()
    {
        var first = Sample("GRID"); var second = Sample("GRID");
        var read = new OrnamentReconstructionGraphRead(first.Observation, second.Observation);

        Assert.Equal("Yes", read.Answer);
        Assert.Equal(E(first.Observation.Sites).Length, E(read.SiteCorrespondence).Length);
        Assert.Equal(E(first.Observation.Segments).Length, E(read.EdgeCorrespondence).Length);
        Assert.All(E(read.SiteCorrespondence).Cast<Relation>(), row => Assert.NotSame(row.Left, row.Right));
        Assert.Equal(read.Attempts, E(read.Consumed).Length);
        Assert.Empty(E(read.Open)); Assert.Empty(E(read.Refuted)); Assert.Empty(E(read.Blocked));
        Assert.DoesNotContain(Reachable(read.Surface), element => element is Carrier or CarrierLocation);
    }

    [Fact]
    public void Aspect_refinement_is_forgotten_at_heading_resolution_while_fixed_frame_rotation_is_not()
    {
        var first = Sample("T"); var stretched = Sample("T", reach: 1); var rotated = Sample("T", transform: 1);
        Assert.Equal("Yes", new OrnamentReconstructionGraphRead(first.Observation, stretched.Observation).Answer);
        Assert.Equal("No", new OrnamentReconstructionGraphRead(first.Observation, rotated.Observation).Answer);
    }

    [Fact]
    public void Mechanical_square_recipes_can_have_the_same_full_published_graph()
    {
        var path = Sample("SQ-PATH"); var sides = Sample("SQ-SIDES"); var unjoined = Sample("SQ-OPEN");
        Assert.Equal("Yes", new OrnamentReconstructionGraphRead(path.Observation, sides.Observation).Answer);
        Assert.Equal("No", new OrnamentReconstructionGraphRead(path.Observation, unjoined.Observation).Answer);
        Assert.NotEqual(E(path.MechanicalGroups).Length, E(sides.MechanicalGroups).Length);
        var read = new OrnamentReconstructionGraphRead(path.Observation, sides.Observation);
        var reachable = Reachable(read.Surface);
        Assert.DoesNotContain(path.Program.Surface, reachable);
        Assert.DoesNotContain(path.MechanicalGroups, reachable);
        Assert.DoesNotContain(sides.MechanicalGroups, reachable);
    }

    [Fact]
    public void Selected_crossing_relations_distinguish_an_encounter_from_unrelated_strokes()
    {
        var crossing = Sample("X-CROSS"); var unrelated = Sample("X-UNRELATED"); var joined = Sample("X-JOIN");
        var incidence = new OrnamentReconstructionGraphRead(crossing.Observation, unrelated.Observation, includeRelations: false);
        Assert.Equal("Yes", incidence.Answer);
        Assert.Equal("No", new OrnamentReconstructionGraphRead(crossing.Observation, unrelated.Observation).Answer);
        Assert.Equal("No", new OrnamentReconstructionGraphRead(crossing.Observation, joined.Observation, includeRelations: false).Answer);
        var carried = Assert.IsType<Relation>(incidence.Scope.Right);
        Assert.Contains(crossing.Observation.Crossings, E(Assert.IsType<Relation>(carried.Left)));
        Assert.Empty(E(incidence.RelationCorrespondence));
    }

    [Fact]
    public void Repetition_count_is_not_collapsed_to_the_presence_of_one_local_motif()
    {
        var once = Sample("DIAMOND"); var twice = Sample("DIAMOND", repeats: 2);
        var read = new OrnamentReconstructionGraphRead(once.Observation, twice.Observation, capacity: 0);
        Assert.Equal("No", read.Answer); Assert.Equal(0, read.Attempts);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void Exhausting_correspondence_capacity_cannot_certify_absence(int capacity)
    {
        var first = Sample("SQ-PATH"); var second = Sample("SQ-PATH");
        var read = new OrnamentReconstructionGraphRead(first.Observation, second.Observation, capacity);
        Assert.Equal("Open", read.Answer); Assert.Empty(E(read.Refuted));
        Assert.Equal(capacity, read.Attempts);
        Assert.Equal(capacity, Capacity(read.Budget));
    }

    [Fact]
    public void Unknown_heading_and_open_selected_relation_scopes_remain_open()
    {
        var f = new Fixture(); var a = f.Site(); var b = f.Site(); f.Edge(a, b, 1);
        var known = f.Observe();
        var edge = f.Edges[0]; var ends = Assert.IsType<Relation>(Assert.IsType<Relation>(edge.Right).Left);
        var unknownEdge = new Relation(edge.Left, new Relation(ends, StructuralConstraint.Open()));
        var unknown = new QualitativeRecognitionObservation(f.Frame, S(a, b), S(unknownEdge));
        var openRelations = new QualitativeRecognitionObservation(f.Frame, known.Sites, known.Segments,
            crossings: StructuralConstraint.Open());

        Assert.Equal("Open", new OrnamentReconstructionGraphRead(known, unknown).Answer);
        Assert.Equal("Open", new OrnamentReconstructionGraphRead(known, openRelations).Answer);
        Assert.Equal("Yes", new OrnamentReconstructionGraphRead(known, openRelations, includeRelations: false).Answer);
    }

    [Fact]
    public void Parallel_edges_preserve_distinct_identities_and_search_relation_compatible_permutations()
    {
        var left = Parallel(crossSecond: false); var right = Parallel(crossSecond: true);
        var read = new OrnamentReconstructionGraphRead(left.Observe(), right.Observe());

        Assert.Equal("Yes", read.Answer);
        var correspondence = E(read.EdgeCorrespondence).Cast<Relation>().ToArray();
        Assert.Equal(3, correspondence.Length);
        Assert.Equal(3, correspondence.Select(row => Assert.IsType<Relation>(row.Right).Left)
            .Distinct(ReferenceEqualityComparer.Instance).Count());
        var mapped = correspondence.Single(row => ReferenceEquals(row.Left, left.Edges[0]));
        Assert.Same(right.Edges[1], Assert.IsType<Relation>(mapped.Right).Left);
        Assert.Single(E(read.RelationCorrespondence));
    }

    [Fact]
    public void Crossing_and_continuation_row_order_are_part_of_the_strict_read()
    {
        var first = Parallel(crossSecond: false); var second = Parallel(crossSecond: false);
        second.Crossings.Clear(); second.Crossings.Add(new(second.Edges[2], second.Edges[0]));
        Assert.Equal("No", new OrnamentReconstructionGraphRead(first.Observe(), second.Observe()).Answer);
        Assert.Equal("Yes", new OrnamentReconstructionGraphRead(first.Observe(), second.Observe(), includeRelations: false).Answer);

        var a = new Fixture(); var x = a.Site(); var y = a.Site(); var z = a.Site();
        a.Edge(x, y, 1); a.Edge(y, z, 1); a.Continuations.Add(new(y, new Relation(a.Edges[0], a.Edges[1])));
        var b = new Fixture(); var p = b.Site(); var q = b.Site(); var r = b.Site();
        b.Edge(p, q, 1); b.Edge(q, r, 1); b.Continuations.Add(new(q, new Relation(b.Edges[1], b.Edges[0])));
        Assert.Equal("No", new OrnamentReconstructionGraphRead(a.Observe(), b.Observe()).Answer);
        Assert.Equal("Yes", new OrnamentReconstructionGraphRead(a.Observe(), b.Observe(), includeRelations: false).Answer);
    }

    [Fact]
    public void Duplicate_relation_entries_cannot_fake_a_bijective_relation_correspondence()
    {
        var first = Parallel(crossSecond: false); var second = Parallel(crossSecond: false);
        second.Crossings.Add(new(second.Edges[0], second.Edges[2]));
        var read = new OrnamentReconstructionGraphRead(first.Observe(), second.Observe());
        Assert.Equal("Open", read.Answer); Assert.Empty(E(read.Refuted));
        var path = new Fixture(); var a = path.Site(); var b = path.Site(); var c = path.Site();
        path.Edge(a, b, 1); path.Edge(b, c, 1);
        path.Continuations.Add(new(b, new Relation(path.Edges[0], path.Edges[1])));
        path.Continuations.Add(new(b, new Relation(path.Edges[0], path.Edges[1])));
        Assert.Equal("Open", new OrnamentReconstructionGraphRead(path.Observe(), path.Observe()).Answer);
    }

    [Fact]
    public void Reverse_segment_traversal_retains_ordered_endpoint_correspondence()
    {
        var a = new Fixture(); a.Edge(a.Site(), a.Site(), 1);
        var b = new Fixture(); b.Edge(b.Site(), b.Site(), 2);
        var read = new OrnamentReconstructionGraphRead(a.Observe(), b.Observe());
        Assert.Equal("Yes", read.Answer);
        var map = Assert.IsType<Relation>(Assert.Single(E(read.EdgeCorrespondence)));
        var targetAndEnds = Assert.IsType<Relation>(map.Right);
        var actualTargetEnds = Assert.IsType<Relation>(Assert.IsType<Relation>(b.Edges[0].Right).Left);
        var mappedEnds = Assert.IsType<Relation>(targetAndEnds.Right);
        Assert.Same(actualTargetEnds.Right, mappedEnds.Left);
        Assert.Same(actualTargetEnds.Left, mappedEnds.Right);
    }

    [Fact]
    public void Native_transport_preserves_capacity_power_aliases_and_graph_correspondence_without_source_mutation()
    {
        var first = Sample("H"); var second = Sample("H");
        var originalSites = E(first.Observation.Sites); var originalRows = E(first.Observation.Segments);
        var read = new OrnamentReconstructionGraphRead(first.Observation, second.Observation, 100000);
        Assert.Equal("Yes", read.Answer);
        Assert.Equal(originalSites, E(first.Observation.Sites)); Assert.Equal(originalRows, E(first.Observation.Segments));
        Assert.Equal(100000, Capacity(read.Budget));
        Assert.True(Reachable(read.Budget).Count < 100);
        var root = new Relation(read.Budget, new Relation(read.SiteCorrespondence, read.EdgeCorrespondence));
        var json = GojoStructureCompactJson.Serialize(GojoStructureCompactJson.FromElement("graph audit", root));
        Assert.True(GojoStructureCompactJson.TryToElement(GojoStructureCompactJson.Deserialize(json), out var restored));
        var actual = Assert.IsType<Relation>(restored.Root);
        Assert.Equal(100000, Capacity(Assert.IsType<Relation>(actual.Left)));
        var mappings = Assert.IsType<Relation>(actual.Right);
        Assert.Equal(E(read.SiteCorrespondence).Length, E(Assert.IsType<Relation>(mappings.Left)).Length);
        Assert.Equal(E(read.EdgeCorrespondence).Length, E(Assert.IsType<Relation>(mappings.Right)).Length);
    }

    private static OrnamentReconstructionCases.Sample Sample(string family, int transform = 0, int reach = 0, int repeats = 1) =>
        OrnamentReconstructionCases.Specs.Single(spec => spec.Family == family && spec.Transform == transform &&
            spec.ReachRecipe == reach && spec.Repeats == repeats).Create();
    private static Fixture Parallel(bool crossSecond)
    {
        var f = new Fixture(); var a = f.Site(); var b = f.Site();
        f.Edge(a, b, 1); f.Edge(a, b, 1); f.Edge(f.Site(), f.Site(), 4);
        f.Crossings.Add(new(f.Edges[crossSecond ? 1 : 0], f.Edges[2])); return f;
    }
    private static int Capacity(Relation budget)
    {
        var quantum = budget.Left; var memo = new Dictionary<IElement, int>(ReferenceEqualityComparer.Instance);
        return E(Assert.IsType<Relation>(budget.Right)).Sum(Count);
        int Count(IElement element)
        {
            if (ReferenceEquals(element, quantum)) return 1;
            if (memo.TryGetValue(element, out var count)) return count;
            var power = Assert.IsType<Relation>(element); Assert.Same(power.Left, power.Right);
            return memo[element] = checked(Count(power.Left) + Count(power.Right));
        }
    }
    private static Relation S(params IElement[] entries) => StructuralConstraint.Closed(entries);
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
    private static HashSet<IElement> Reachable(IElement root)
    {
        var seen = new HashSet<IElement>(ReferenceEqualityComparer.Instance); var stack = new Stack<IElement>(); stack.Push(root);
        while (stack.TryPop(out var item))
            if (seen.Add(item) && item is Relation row) { stack.Push(row.Left); stack.Push(row.Right); }
        return seen;
    }
    private sealed class Fixture
    {
        internal QualitativeMotionFrame Frame { get; } = new();
        internal List<IElement> Sites { get; } = [];
        internal List<Relation> Edges { get; } = [];
        internal List<Relation> Continuations { get; } = [];
        internal List<Relation> Crossings { get; } = [];
        internal IElement Site() { var site = new Placeholder(); Sites.Add(site); return site; }
        internal void Edge(IElement from, IElement to, int heading)
        {
            var direction = heading == 1 ? Frame.Right : heading == 2 ? Frame.Left : heading == 4 ? Frame.Up : Frame.Down;
            Edges.Add(new(new Placeholder(), new Relation(new Relation(from, to), S(direction))));
        }
        internal QualitativeRecognitionObservation Observe() => new(Frame, S(Sites.ToArray()), S(Edges.Cast<IElement>().ToArray()),
            S(Continuations.Cast<IElement>().ToArray()), S(Crossings.Cast<IElement>().ToArray()));
    }
}
