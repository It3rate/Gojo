using Gojo.Core;
using Gojo.Encoding.Structures;
using Gojo.Experiments.QualitativeExecution;
using Gojo.Experiments.QualitativeFrieze;
using Gojo.Patterns;
using C = Gojo.Experiments.QualitativeFrieze.OccurrenceComposition;

namespace QualitativeExecution.Tests;

public sealed class OccurrenceLandmarkTests
{
    [Fact]
    public void Sharing_a_whole_path_does_not_establish_endpoint_contact()
    {
        var f = new Fixture(); var a = f.Site(); var b = f.Site(); var c = f.Site();
        f.Edge(a, b, 1); f.Edge(b, c, 1); f.Triangle(b);
        var output = f.Read(); var path = new C.Role(WalkPattern()); var loop = new C.Role(TrianglePattern());
        var (start, middle, end) = PathSites(path);
        var allLoop = All(loop);
        Assert.Equal("Yes", new C.Assessment(output, new("contact somewhere", [path, loop],
            sharedSites: S(C.Pair(path, loop)))).Answer);
        var endpoints = new C.Question("endpoint contact", [path, loop],
            sharedSelectedSites: S(C.Pair(C.Select(path, start, end), allLoop)));
        Assert.Equal("No", new C.Assessment(output, endpoints).Answer);
        var centre = new C.Assessment(output, new("middle contact", [path, loop],
            sharedSelectedSites: S(C.Pair(C.Select(path, middle), allLoop))));
        Assert.Equal("Yes", centre.Answer);
        var witness = Assert.Single(centre.Witnesses);
        Assert.Equal(2, E(witness.SelectedSites).Length);
        Assert.Single(E(witness.Checks));
        var selections = E(witness.SelectedSites).Cast<Relation>().ToArray();
        Assert.Contains(selections, selected => E((Relation)selected.Right).Any(site => ReferenceEquals(site, b)));
    }

    [Fact]
    public void Selected_pattern_sites_survive_independent_native_transport_without_names_or_numbers()
    {
        var path = new C.Role(WalkPattern()); var loop = new C.Role(TrianglePattern());
        var (_, middle, _) = PathSites(path);
        var original = new C.Question("unimportant caption", [path, loop],
            sharedSelectedSites: S(C.Pair(C.Select(path, middle), All(loop))));
        var json = GojoStructureCompactJson.Serialize(GojoStructureCompactJson.FromElement("query", original.Surface));
        Assert.True(GojoStructureCompactJson.TryToElement(GojoStructureCompactJson.Deserialize(json), out var restored));
        var question = new C.Question("new caption", Assert.IsType<Relation>(restored.Root));
        Assert.True(question.IsValid); Assert.True(question.HasSelectedSiteConstraints);
        var f = new Fixture(); var a = f.Site(); var b = f.Site(); var c = f.Site();
        f.Edge(a, b, 1); f.Edge(b, c, 1); f.Triangle(b);
        var assessment = new C.Assessment(f.Read(), question);
        Assert.Equal("Yes", assessment.Answer);
        Assert.DoesNotContain(Reachable(assessment.Surface), participant => participant is Carrier or CarrierLocation);
        Assert.DoesNotContain(Reachable(question.Surface), participant => ReferenceEquals(participant, original.Surface));
    }

    [Fact]
    public void A_foreign_pattern_site_is_blocked_and_an_unfinished_selection_remains_open()
    {
        var f = new Fixture(); var a = f.Site(); var b = f.Site(); var c = f.Site();
        f.Edge(a, b, 1); f.Edge(b, c, 1);
        var path = new C.Role(WalkPattern()); var (start, _, _) = PathSites(path);
        var foreign = new C.Question("foreign site", [path], sharedSelectedSites:
            S(C.Pair(C.Select(path, new Placeholder()), C.Select(path, start))));
        Assert.False(foreign.IsValid);
        Assert.Equal("Blocked", new C.Assessment(f.Read(), foreign).Answer);
        var pending = new C.Question("unfinished selection", [path], sharedSelectedSites:
            S(C.Pair(new C.SiteSelection(path, StructuralConstraint.Open()), C.Select(path, start))));
        Assert.Empty(E(pending.Blocked)); Assert.NotEmpty(E(pending.Unresolved));
        var read = new C.Assessment(f.Read(), pending);
        Assert.Equal("Open", read.Answer); Assert.Empty(E(read.Refuted));
    }

    [Fact]
    public void Closed_empty_selections_are_vacuous_for_disjointness_and_cannot_establish_sharing()
    {
        var f = new Fixture(); var a = f.Site(); var b = f.Site(); var c = f.Site();
        f.Edge(a, b, 1); f.Edge(b, c, 1);
        var role = new C.Role(WalkPattern()); var pair = S(C.Pair(C.Select(role), All(role)));
        Assert.Equal("No", new C.Assessment(f.Read(), new("empty intersection", [role], sharedSelectedSites: pair)).Answer);
        Assert.Equal("Yes", new C.Assessment(f.Read(), new("vacuous disjointness", [role], disjointSelectedSites: pair)).Answer);
    }

    [Fact]
    public void Missing_correspondence_capacity_does_not_become_negative_selected_site_evidence()
    {
        var f = new Fixture(); var a = f.Site(); var b = f.Site(); var c = f.Site();
        f.Edge(a, b, 1); f.Edge(b, c, 1);
        var role = new C.Role(WalkPattern()); var (start, _, end) = PathSites(role);
        var question = new C.Question("endpoints are disjoint", [role],
            disjointSelectedSites: S(C.Pair(C.Select(role, start), C.Select(role, end))));
        var cut = new C.Assessment(f.Read(), question, correspondenceCapacity: 0);
        Assert.Equal("Open", cut.Answer); Assert.Empty(E(cut.Refuted));
        Assert.Empty(E(cut.CorrespondenceConsumed));
        var full = new C.Assessment(f.Read(), question, correspondenceCapacity: 32);
        Assert.Equal("Yes", full.Answer);
        Assert.Equal(32, E(full.CorrespondenceConsumed).Length + E(full.CorrespondenceRemaining).Length);
        Assert.All(E(full.CorrespondenceConsumed).Cast<Relation>(), consumed =>
            Assert.Contains(E(full.CorrespondenceBudget), slot => ReferenceEquals(slot, consumed.Left)));
    }

    [Fact]
    public void Equal_heading_star_leaves_keep_alternate_site_correspondences_instead_of_greedy_alias_loss()
    {
        var source = new Fixture(); var centre = source.Site(); var first = source.Site(); var second = source.Site();
        source.Edge(centre, first, 1); source.Edge(centre, second, 1);
        var family = source.Read().Families.Single(candidate => candidate.Kind == "junction" && candidate.SegmentCount == 2);
        var pattern = new CommunicationPattern(source.Frame, family.Definition);
        var receiver = new Fixture(); var a = receiver.Site(); var b = receiver.Site(); var c = receiver.Site();
        receiver.Edge(a, b, 1); receiver.Edge(a, c, 1); receiver.Triangle(c);
        var output = receiver.Read();
        var shape = Shape(pattern.Definition); var leaves = shape.Sites.Where(site => !shape.Centres.Contains(site)).ToArray();
        Assert.Equal(2, leaves.Length);
        foreach (var leaf in leaves)
        {
            var star = new C.Role(pattern); var loop = new C.Role(TrianglePattern());
            var question = new C.Question("this leaf meets the loop", [star, loop],
                disjointEdges: S(C.Pair(star, loop)),
                sharedSelectedSites: S(C.Pair(C.Select(star, leaf), All(loop))));
            var assessment = new C.Assessment(output, question);
            Assert.Equal("Yes", assessment.Answer);
            var witness = Assert.Single(assessment.Witnesses);
            var selected = E(witness.SelectedSites).Cast<Relation>().Single(row =>
                row.Left is Relation selector && ReferenceEquals(selector.Left, star.Slot));
            Assert.Same(c, Assert.Single(E((Relation)selected.Right)));
        }
    }

    [Fact]
    public void Alternate_site_correspondences_cannot_manufacture_two_distinct_edge_footprints()
    {
        var source = new Fixture(); var centre = source.Site(); var first = source.Site(); var second = source.Site();
        source.Edge(centre, first, 1); source.Edge(centre, second, 1);
        var family = source.Read().Families.Single(candidate => candidate.Kind == "junction" && candidate.SegmentCount == 2);
        var pattern = new CommunicationPattern(source.Frame, family.Definition);
        var a = new C.Role(pattern); var b = new C.Role(pattern);
        var leaf = Shape(pattern.Definition).Sites.First(site => !Shape(pattern.Definition).Centres.Contains(site));
        var question = new C.Question("two different stars", [a, b], distinct: S(C.Pair(a, b)),
            sharedSelectedSites: S(C.Pair(C.Select(a, leaf), C.Select(b, leaf))));
        var assessment = new C.Assessment(source.Read(), question);
        Assert.Equal("No", assessment.Answer); Assert.Empty(assessment.Witnesses);
        Assert.NotEmpty(E(assessment.CorrespondenceConsumed));
        Assert.NotEmpty(E(assessment.Rejected));
    }

    private static CommunicationPattern WalkPattern()
    {
        var f = new Fixture(); var a = f.Site(); var b = f.Site(); var c = f.Site();
        f.Edge(a, b, 1); f.Edge(b, c, 1);
        var family = f.Read().Families.First(candidate => candidate.Kind == "walk" && candidate.SegmentCount == 2);
        return new(f.Frame, family.Definition);
    }
    private static CommunicationPattern TrianglePattern()
    {
        var f = new Fixture(); f.Triangle(f.Site());
        var family = f.Read().Families.First(candidate => candidate.Kind == "cycle" && candidate.SegmentCount == 3);
        return new(f.Frame, family.Definition);
    }
    private static C.SiteSelection All(C.Role role) => C.Select(role, Shape((Relation)role.PatternSurface.Right).Sites);
    private static (IElement Start, IElement Middle, IElement End) PathSites(C.Role role)
    {
        var shape = Shape((Relation)role.PatternSurface.Right);
        return (shape.Ends[0].Left, shape.Ends[0].Right, shape.Ends[^1].Right);
    }
    private static (IElement[] Sites, IElement[] Centres, Relation[] Ends) Shape(Relation definition)
    {
        var head = (Relation)definition.Left; var graph = (Relation)head.Right;
        var tail = (Relation)definition.Right;
        return (E((Relation)graph.Left), E((Relation)tail.Left), E((Relation)graph.Right).Cast<Relation>()
            .Select(row => (Relation)((Relation)row.Right).Left).ToArray());
    }
    private static Relation S(params IElement[] entries) => StructuralConstraint.Closed(entries);
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
    private static IEnumerable<IElement> Reachable(IElement root)
    {
        var seen = new HashSet<IElement>(ReferenceEqualityComparer.Instance); var pending = new Stack<IElement>(); pending.Push(root);
        while (pending.TryPop(out var element))
        {
            if (!seen.Add(element)) continue; yield return element;
            if (element is Relation relation) { pending.Push(relation.Left); pending.Push(relation.Right); }
        }
    }
    private sealed class Fixture
    {
        public QualitativeMotionFrame Frame { get; } = new();
        private List<IElement> Sites { get; } = [];
        private List<IElement> Edges { get; } = [];
        public IElement Site() { var site = new Placeholder(); Sites.Add(site); return site; }
        public void Edge(IElement from, IElement to, int mask) => Edges.Add(new Relation(new Placeholder(),
            new Relation(new Relation(from, to), S(new[] { Frame.Right, Frame.Left, Frame.Up, Frame.Down }
                .Where((_, index) => (mask & 1 << index) != 0).Cast<IElement>().ToArray()))));
        public void Triangle(IElement a)
        {
            var b = Site(); var c = Site(); Edge(a, b, 4); Edge(b, c, 9); Edge(c, a, 8);
        }
        public QualitativeMotifInduction Read() => new(new(Frame, S(Sites.ToArray()), S(Edges.ToArray())),
            maxCycleEdges: 3, maxWalkEdges: 3, proposalCapacity: 8192, retainedCapacity: 2048);
    }
}
