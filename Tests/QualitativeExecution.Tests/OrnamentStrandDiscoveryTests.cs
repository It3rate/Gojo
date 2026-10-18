using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;
using Gojo.Experiments.QualitativeFrieze;

namespace Gojo.Experiments.QualitativeExecution.Tests;

public sealed class OrnamentStrandDiscoveryTests
{
    [Fact]
    public void ActualIncidenceAndCyclicHeadingsDiscoverAnOrderedGrowingRoute()
    {
        var f = new Fixture(5);
        f.At(0, 0, 0); f.At(1, 2, 1); f.At(2, 4, 0); f.At(3, 5, 2); f.At(4, 8, 0);
        var edges = new[] { f.Edge(0, 1), f.Edge(2, 1), f.Edge(2, 3), f.Edge(4, 3) };
        var grammar = S(S(f.Frame.Right, f.Frame.Up), S(f.Frame.Right, f.Frame.Down));
        var discovery = f.Discover(grammar);
        Assert.True(discovery.IsComplete); Assert.Empty(E(discovery.Competing));
        Assert.Equal(8, E(discovery.EdgeReads).Length); Assert.Equal(3, E(discovery.Continuations).Length);
        var strand = Read(discovery, Assert.Single(E(discovery.Routes)));
        Assert.Same(f.Sites[0], strand.Start); Assert.Same(f.Sites[4], strand.End);
        Assert.Equal(4, strand.Uses.Length); Assert.False(strand.IsClosed);
        for (var i = 0; i < 4; i++)
        {
            Assert.Same(edges[i].Left, strand.Uses[i].Left);
            Assert.Same(f.Sites[i], ((Relation)strand.Uses[i].Right).Left);
            Assert.Same(f.Sites[i + 1], ((Relation)strand.Uses[i].Right).Right);
        }
        Assert.Equal(2, E(discovery.Boundary).Length);
        AssertNoNumbers(discovery.Surface);
    }

    [Fact]
    public void EqualPublishedWordsDoNotCreateAJoinBetweenIndependentSites()
    {
        var f = new Fixture(4);
        f.At(0, 0, 0); f.At(1, 1, 0); f.At(2, 1, 0); f.At(3, 2, 0);
        f.Edge(0, 1); f.Edge(2, 3);
        var discovery = f.Discover(S(S(f.Frame.Right)));
        Assert.True(discovery.IsComplete); Assert.Equal(2, E(discovery.Routes).Length);
        Assert.Empty(E(discovery.Continuations));
        foreach (var row in E(discovery.Routes)) Assert.Single(Read(discovery, row).Uses);
    }

    [Fact]
    public void ReversedQuestionFindsReversedUsesWithoutReversingTheSource()
    {
        var f = new Fixture(3);
        f.At(0, 0, 0); f.At(1, 1, 0); f.At(2, 3, 0); var first = f.Edge(0, 1); f.Edge(1, 2);
        var discovery = f.Discover(S(S(f.Frame.Left)));
        var route = Read(discovery, Assert.Single(E(discovery.Routes)));
        Assert.Same(f.Sites[2], route.Start); Assert.Same(f.Sites[0], route.End);
        Assert.Same(f.Sites[0], ((Relation)first.Right).Left);
        Assert.Same(f.Sites[1], ((Relation)first.Right).Right);
    }

    [Fact]
    public void AForkRetainsAllAlternativesAndStopsRatherThanPickingOne()
    {
        var f = new Fixture(4);
        f.At(0, 0, 0); f.At(1, 1, 0); f.At(2, 2, 0); f.At(3, 3, 0);
        f.Edge(0, 1); f.Edge(1, 2); f.Edge(1, 3);
        var discovery = f.Discover(S(S(f.Frame.Right)));
        Assert.True(discovery.IsComplete); Assert.Single(E(discovery.Competing));
        Assert.Equal(2, E(discovery.Continuations).Length); Assert.Equal(3, E(discovery.Routes).Length);
        foreach (var row in E(discovery.Routes)) Assert.Single(Read(discovery, row).Uses);
        var competition = Assert.IsType<Relation>(Assert.Single(E(discovery.Competing)));
        var ends = Assert.IsType<Relation>(competition.Right);
        Assert.Equal(2, E((Relation)ends.Right).Length);
    }

    [Fact]
    public void OpenBindingsAndOpenGrammarDoNotClaimMaximalRoutes()
    {
        var f = new Fixture(2); f.At(0, 0, 0); f.At(1, 1, 0); f.Edge(0, 1);
        var openBindings = new OrnamentStrandDiscovery(f.Frame, new Placeholder(), f.Topology,
            StructuralConstraint.Open(f.Bindings.ToArray()), S(S(f.Frame.Right)), new Placeholder());
        Assert.False(openBindings.IsComplete); Assert.Empty(E(openBindings.Routes)); Assert.NotEmpty(E(openBindings.Unresolved));
        var openPattern = f.Discover(StructuralConstraint.Open(S(f.Frame.Right)));
        Assert.False(openPattern.IsComplete); Assert.Empty(E(openPattern.Routes)); Assert.NotEmpty(E(openPattern.Unresolved));
    }

    [Fact]
    public void UnknownRelativeExtentsRemainUnresolvedInsteadOfGuessingAHeading()
    {
        var f = new Fixture(2); var different = new Placeholder();
        f.Bindings.Add(new Relation(f.Sites[0], S(QualitativeMotionFrame.Move(f.Frame.Right, f.Reach))));
        f.Bindings.Add(new Relation(f.Sites[1], S(QualitativeMotionFrame.Move(f.Frame.Right, different))));
        f.Edge(0, 1); var discovery = f.Discover(S(S(f.Frame.Right)));
        Assert.False(discovery.IsComplete); Assert.Empty(E(discovery.Routes)); Assert.Equal(2, E(discovery.Unresolved).Length);
    }

    [Fact]
    public void AClosedHeadingCycleRetainsItsReturnLinkAndActualStartIdentity()
    {
        var f = new Fixture(4);
        f.At(0, 0, 0); f.At(1, 1, 0); f.At(2, 1, 1); f.At(3, 0, 1);
        f.Edge(0, 1); f.Edge(1, 2); f.Edge(2, 3); f.Edge(3, 0);
        var discovery = f.Discover(S(S(f.Frame.Right), S(f.Frame.Up), S(f.Frame.Left), S(f.Frame.Down)));
        var route = Read(discovery, Assert.Single(E(discovery.ClosedCycles)));
        Assert.True(route.IsClosed); Assert.Same(route.Start, route.End);
        Assert.Single(E(discovery.ClosedCycles)); Assert.Equal(4, E(route.Continuations).Length);
        Assert.Equal(4, E(discovery.Boundary).Length); // reverse-use fragments remain visible
    }

    [Fact]
    public void FanIncidenceSuppliesFullWidthDiamondHalfVinesWithoutAuthoringIndexes()
    {
        var source = OrnamentSourcePatch.SquareFans(3, 2);
        var derived = new OrnamentDerivedScaffold(source.Scaffold, new Placeholder());
        var landmarks = new OrnamentWordLandmarks(source.Scaffold, source.Bindings);
        var bindings = S(E(derived.Representatives).Cast<Relation>().Select(row =>
        {
            Assert.True(landmarks.TryRead(row.Left, out var binding, out _));
            return (IElement)new Relation(row.Right, binding.Right);
        }).ToArray());
        var f = source.Frame;
        var grammar = S(S(f.Right, f.Down), S(f.Right, f.Up), S(f.Right));
        var discovery = new OrnamentStrandDiscovery(f, derived.Surface, derived.Topology, bindings, grammar, new Placeholder());
        Assert.True(discovery.IsComplete); Assert.Empty(E(discovery.Competing));
        var routes = E(discovery.Routes).Select(row => Read(discovery, row)).ToArray();
        Assert.Equal(14, routes.Length);
        Assert.Equal(2, routes.Count(route => route.Uses.Length == 8));
        Assert.Equal(12, routes.Count(route => route.Uses.Length == 1));
        Assert.Equal(14, E(discovery.Continuations).Length);
        AssertNoNumbers(discovery.Surface);
    }

    [Fact]
    public void AForeignEndpointAndContradictoryHeadingCannotReadAsValidRoutes()
    {
        var f = new Fixture(2); f.At(0, 0, 0); f.At(1, 1, 0);
        f.Edges.Add(new Relation(new Placeholder(), new Relation(f.Sites[0], new Placeholder())));
        var discovery = f.Discover(S(S(f.Frame.Right)));
        Assert.False(discovery.IsComplete); Assert.NotEmpty(E(discovery.Blocked)); Assert.Empty(E(discovery.Routes));
        var contradiction = f.Discover(S(S(f.Frame.Right, f.Frame.Left)));
        Assert.False(contradiction.IsComplete); Assert.NotEmpty(E(contradiction.Blocked));
    }

    [Fact]
    public void DuplicateParticipantAliasesRemainBlockedInsteadOfThrowing()
    {
        var frame = new QualitativeMotionFrame(); var site = new Placeholder();
        var topology = new Relation(S(site, site.Dual().Dual()), new Relation(S(), S()));
        var read = new OrnamentStrandDiscovery(frame, new Placeholder(), topology,
            S(new Relation(site, S())), S(S(frame.Right)), new Placeholder());
        Assert.False(read.IsComplete); Assert.NotEmpty(E(read.Blocked)); Assert.Empty(E(read.Routes));
    }

    private static OrnamentDiscoveredStrand Read(OrnamentStrandDiscovery discovery, IElement row)
    { Assert.True(discovery.TryReadRoute(Assert.IsType<Relation>(row), out var route)); return route; }
    private static void AssertNoNumbers(IElement root)
    {
        var seen = new HashSet<IElement>(ReferenceEqualityComparer.Instance); var todo = new Stack<IElement>(); todo.Push(root);
        while (todo.TryPop(out var item))
        {
            if (!seen.Add(item)) continue; Assert.IsNotType<Carrier>(item); Assert.IsNotType<CarrierLocation>(item);
            if (item is Relation relation) { todo.Push(relation.Left); todo.Push(relation.Right); }
        }
    }
    private sealed class Fixture(int count)
    {
        public QualitativeMotionFrame Frame { get; } = new();
        public IElement Reach { get; } = new Placeholder();
        public IElement[] Sites { get; } = Enumerable.Range(0, count).Select(_ => (IElement)new Placeholder()).ToArray();
        public List<IElement> Edges { get; } = [];
        public List<IElement> Bindings { get; } = [];
        public Relation Topology => new(S(Sites), new Relation(S(Edges.ToArray()), S()));
        public void At(int index, int x, int y) => Bindings.Add(new Relation(Sites[index], S(
            Enumerable.Repeat(QualitativeMotionFrame.Move(x < 0 ? Frame.Left : Frame.Right, Reach), Math.Abs(x))
                .Concat(Enumerable.Repeat(QualitativeMotionFrame.Move(y < 0 ? Frame.Down : Frame.Up, Reach), Math.Abs(y)))
                .Cast<IElement>().ToArray())));
        public Relation Edge(int from, int to)
        { var edge = new Relation(new Placeholder(), new Relation(Sites[from], Sites[to])); Edges.Add(edge); return edge; }
        public OrnamentStrandDiscovery Discover(Relation pattern) => new(Frame, new Placeholder(), Topology,
            S(Bindings.ToArray()), pattern, new Placeholder());
    }
    private static Relation S(params IElement[] entries) => StructuralConstraint.Closed(entries);
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
}
