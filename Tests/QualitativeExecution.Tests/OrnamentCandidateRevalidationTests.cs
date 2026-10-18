using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;
using Gojo.Experiments.QualitativeFrieze;

namespace Gojo.Experiments.QualitativeExecution.Tests;

public sealed class OrnamentCandidateRevalidationTests
{
    [Fact]
    public void AWithdrawnEdgeRefutesOnlyItsCellAndPreservesTheHistoricalFamily()
    {
        var f = new CellsFixture(); var family = Assert.IsType<Relation>(Assert.Single(E(f.Previous.Families)));
        var patch = OrnamentStructurePerturbation.WithdrawEdge(f.Source, f.Topology, S(), f.Edges[0], new Placeholder());
        var read = OrnamentCandidateRevalidation.Cells(f.Previous, family, patch.Topology, S(), new Placeholder(), patch.Surface);
        Assert.True(read.IsComplete); Assert.Single(E(read.Supported)); Assert.Single(E(read.Refuted)); Assert.Empty(E(read.Unresolved));
        Assert.True(f.Previous.TryReadFamily(family, out var retained)); Assert.Equal(2, E(retained.Occurrences).Length);
        var failed = Assert.IsType<Relation>(Assert.Single(E(read.Refuted)));
        Assert.True(read.TryReadWitness(failed, out var old, out _, out var failures, out var unknown));
        Assert.Same(f.Faces[0], ((Relation)old).Left); Assert.NotEmpty(E(failures)); Assert.Empty(E(unknown));
        Assert.False(read.TryReadWitness(new Relation(failed.Left, failed.Right), out _, out _, out _, out _));
        AssertNoNumbers(read.Surface);
    }

    [Fact]
    public void EqualPositionEndpointDetachStillRefutesTheOldCellTopology()
    {
        var f = new CellsFixture(); var family = Assert.IsType<Relation>(Assert.Single(E(f.Previous.Families)));
        var patch = OrnamentStructurePerturbation.DetachEndpoint(f.Source, f.Topology,
            S(f.Sites.Select(site => (IElement)new Relation(site, S())).ToArray()), f.Edges[0], f.Sites[1], new Placeholder());
        var read = OrnamentCandidateRevalidation.Cells(f.Previous, family, patch.Topology, patch.Bindings, new Placeholder(), patch.Surface);
        Assert.Single(E(read.Supported)); Assert.Single(E(read.Refuted)); Assert.Empty(E(read.Unresolved));
    }

    [Fact]
    public void TopologicalCellClaimsDoNotDependOnWordSupply()
    {
        var f = new CellsFixture(); var family = Assert.IsType<Relation>(Assert.Single(E(f.Previous.Families)));
        var read = OrnamentCandidateRevalidation.Cells(f.Previous, family, f.Topology,
            StructuralConstraint.Open(new Relation(f.Sites[0], StructuralConstraint.Open())), new Placeholder());
        Assert.Equal(2, E(read.Supported).Length); Assert.Empty(E(read.Refuted)); Assert.Empty(E(read.Unresolved));
    }

    [Fact]
    public void AnAbsentEdgeInAnOpenSupplyIsUnresolvedRatherThanCounterevidence()
    {
        var f = new CellsFixture(); var family = Assert.IsType<Relation>(Assert.Single(E(f.Previous.Families)));
        var topology = new Relation(S(f.Sites), new Relation(StructuralConstraint.Open(f.Edges.Skip(1).ToArray()), S(f.Faces)));
        var read = OrnamentCandidateRevalidation.Cells(f.Previous, family, topology, S(), new Placeholder());
        Assert.Empty(E(read.Supported)); Assert.Empty(E(read.Refuted)); Assert.Equal(2, E(read.Unresolved).Length);
    }

    [Theory]
    [InlineData(1, 2, 0)]
    [InlineData(2, 1, 1)]
    [InlineData(3, 1, 1)]
    public void HeadingQuestionsIgnoreReachChangesUntilTheDirectionSetChanges(int steps, int supported, int refuted)
    {
        var f = new StrandFixture(); var delta = S(Enumerable.Repeat((IElement)QualitativeMotionFrame.Move(f.Frame.Right, f.Reach), steps).ToArray());
        var patch = OrnamentStructurePerturbation.WordDelta(f.Frame, f.Source, f.Topology, f.BindingScope, f.Bindings[1], delta, new Placeholder());
        var read = OrnamentCandidateRevalidation.Strands(f.Previous, f.Selected, patch.Topology, patch.Bindings, new Placeholder(), patch.Surface);
        Assert.Equal(supported, E(read.Supported).Length); Assert.Equal(refuted, E(read.Refuted).Length); Assert.Empty(E(read.Unresolved));
        Assert.Equal(2, E(f.Selected).Length); Assert.True(f.Previous.IsComplete);
    }

    [Fact]
    public void OpenOneWordOnlyMakesItsDependentOldStrandUnresolved()
    {
        var f = new StrandFixture(); var open = StructuralConstraint.Open(E((Relation)f.Bindings[1].Right));
        var patch = OrnamentStructurePerturbation.ReplaceWord(f.Source, f.Topology, f.BindingScope, f.Bindings[1], open, new Placeholder());
        var fresh = new OrnamentStrandDiscovery(f.Frame, patch.Surface, patch.Topology, patch.Bindings, f.Grammar, new Placeholder());
        Assert.False(fresh.IsComplete); Assert.Empty(E(fresh.Routes));
        var read = OrnamentCandidateRevalidation.Strands(f.Previous, f.Selected, patch.Topology, patch.Bindings, new Placeholder(), patch.Surface);
        Assert.Single(E(read.Supported)); Assert.Single(E(read.Unresolved)); Assert.Empty(E(read.Refuted));
        Assert.False(read.IsComplete); AssertNoNumbers(read.Surface);
    }

    [Fact]
    public void IndependentReachesAreNotUsedToInventAHeading()
    {
        var f = new StrandFixture(); var word = S(QualitativeMotionFrame.Move(f.Frame.Right, new Placeholder()),
            QualitativeMotionFrame.Move(f.Frame.Up, f.Reach));
        var patch = OrnamentStructurePerturbation.ReplaceWord(f.Source, f.Topology, f.BindingScope, f.Bindings[1], word, new Placeholder());
        var read = OrnamentCandidateRevalidation.Strands(f.Previous, f.Selected, patch.Topology, patch.Bindings, new Placeholder(), patch.Surface);
        Assert.Single(E(read.Supported)); Assert.Single(E(read.Unresolved)); Assert.Empty(E(read.Refuted));
    }

    [Fact]
    public void ADeletedEdgeIsCounterevidenceEvenWhenAnotherDependencyIsOpen()
    {
        var f = new StrandFixture();
        var open = OrnamentStructurePerturbation.ReplaceWord(f.Source, f.Topology, f.BindingScope, f.Bindings[1], StructuralConstraint.Open(), new Placeholder());
        var cut = OrnamentStructurePerturbation.WithdrawEdge(open.Surface, open.Topology, open.Bindings, f.Edges[0], new Placeholder());
        var read = OrnamentCandidateRevalidation.Strands(f.Previous, f.Selected, cut.Topology, cut.Bindings, new Placeholder(), cut.Surface);
        Assert.Single(E(read.Supported)); Assert.Single(E(read.Refuted)); Assert.Empty(E(read.Unresolved));
        var failed = Assert.IsType<Relation>(Assert.Single(E(read.Refuted)));
        Assert.True(read.TryReadWitness(failed, out _, out _, out var failures, out var unknown));
        Assert.NotEmpty(E(failures)); Assert.NotEmpty(E(unknown));
    }

    [Fact]
    public void DetachingAtEqualPublishedPositionBreaksTheOldWalk()
    {
        var f = new StrandFixture();
        var patch = OrnamentStructurePerturbation.DetachEndpoint(f.Source, f.Topology, f.BindingScope, f.Edges[0], f.Sites[1], new Placeholder());
        var read = OrnamentCandidateRevalidation.Strands(f.Previous, f.Selected, patch.Topology, patch.Bindings, new Placeholder(), patch.Surface);
        Assert.Single(E(read.Supported)); Assert.Single(E(read.Refuted)); Assert.Empty(E(read.Unresolved));
    }

    [Fact]
    public void AnExtraBranchDoesNotEraseTheSupportForAnEarlierOrderedWalk()
    {
        var f = new StrandFixture(); var extra = new Relation(new Placeholder(), new Relation(f.Sites[1], f.Sites[5]));
        var topology = new Relation(S(f.Sites), new Relation(S([.. f.Edges, extra]), S()));
        var read = OrnamentCandidateRevalidation.Strands(f.Previous, f.Selected, topology, f.BindingScope, new Placeholder());
        Assert.Equal(2, E(read.Supported).Length); Assert.Empty(E(read.Refuted)); Assert.Empty(E(read.Unresolved));
    }

    [Fact]
    public void CopiedOldRouteDoesNotBecomeAnAuthenticatedHistoricalCandidate()
    {
        var f = new StrandFixture(); var actual = Assert.IsType<Relation>(E(f.Selected)[0]);
        var read = OrnamentCandidateRevalidation.Strands(f.Previous, S(new Relation(actual.Left, actual.Right)),
            f.Topology, f.BindingScope, new Placeholder());
        Assert.Empty(E(read.Supported)); Assert.Empty(E(read.Refuted)); Assert.Single(E(read.Unresolved));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ExactRepeatTranslationHasLocalCounterexamplesOrOpenObligations(bool open)
    {
        var frame = new QualitativeMotionFrame(); var reach = new Placeholder();
        var roles = Enumerable.Range(0, 4).Select(_ => (IElement)new Placeholder()).ToArray();
        Relation Word(int count) => S(Enumerable.Repeat((IElement)QualitativeMotionFrame.Move(frame.Right, reach), count).ToArray());
        var bindings = roles.Select((role, i) => new Relation(role, Word(i))).ToArray();
        var previous = new OrnamentOccurrenceRhythm(frame, new Placeholder(), S(roles), S(bindings), S(Word(1)), S(new Placeholder(), new Placeholder()), new Placeholder());
        Assert.True(previous.IsComplete); Assert.Equal(3, E(previous.Successors).Length);
        var changed = bindings.Select((binding, i) => i == 1 ? new Relation(binding.Left, open ? StructuralConstraint.Open() : Word(2)) : binding).ToArray();
        var read = OrnamentCandidateRevalidation.Rhythm(previous, S(changed), new Placeholder());
        Assert.Single(E(read.Supported)); Assert.Equal(open ? 0 : 2, E(read.Refuted).Length);
        Assert.Equal(open ? 2 : 0, E(read.Unresolved).Length); AssertNoNumbers(read.Surface);
    }

    private sealed class CellsFixture
    {
        internal IElement Source { get; } = new Placeholder();
        internal IElement[] Sites { get; } = Enumerable.Range(0, 6).Select(_ => (IElement)new Placeholder()).ToArray();
        internal Relation[] Edges { get; }
        internal Relation[] Faces { get; }
        internal Relation Topology { get; }
        internal OrnamentCellDiscovery Previous { get; }
        internal CellsFixture()
        {
            Edges = Enumerable.Range(0, 6).Select(i => new Relation(new Placeholder(),
                new Relation(Sites[i], Sites[i / 3 * 3 + (i + 1) % 3]))).ToArray();
            Faces = Enumerable.Range(0, 2).Select(i => new Relation(new Placeholder(),
                S(Edges.Skip(i * 3).Take(3).Select(edge => (IElement)new Relation(edge.Left, edge.Right)).ToArray()))).ToArray();
            Topology = new(S(Sites), new Relation(S(Edges), S(Faces)));
            Previous = new(Source, Topology, new Placeholder());
        }
    }
    private sealed class StrandFixture
    {
        internal QualitativeMotionFrame Frame { get; } = new();
        internal IElement Reach { get; } = new Placeholder();
        internal IElement Source { get; } = new Placeholder();
        internal IElement[] Sites { get; } = Enumerable.Range(0, 6).Select(_ => (IElement)new Placeholder()).ToArray();
        internal Relation[] Edges { get; }
        internal Relation[] Bindings { get; }
        internal Relation Topology { get; }
        internal Relation BindingScope { get; }
        internal Relation Grammar { get; }
        internal Relation Selected { get; }
        internal OrnamentStrandDiscovery Previous { get; }
        internal StrandFixture()
        {
            var positions = new[] { (0, 0), (2, 1), (4, 0), (0, 4), (2, 5), (4, 4) };
            Bindings = Sites.Select((site, i) => new Relation(site, S(
                Enumerable.Repeat((IElement)QualitativeMotionFrame.Move(Frame.Right, Reach), positions[i].Item1)
                    .Concat(Enumerable.Repeat((IElement)QualitativeMotionFrame.Move(Frame.Up, Reach), positions[i].Item2)).ToArray()))).ToArray();
            Edges = new[] { (0, 1), (1, 2), (3, 4), (4, 5) }.Select(pair => new Relation(new Placeholder(), new Relation(Sites[pair.Item1], Sites[pair.Item2]))).ToArray();
            Topology = new(S(Sites), new Relation(S(Edges), S())); BindingScope = S(Bindings);
            Grammar = S(S(Frame.Right, Frame.Up), S(Frame.Right, Frame.Down));
            Previous = new(Frame, Source, Topology, BindingScope, Grammar, new Placeholder());
            Selected = S(E(Previous.Routes).Where(item => { Previous.TryReadRoute((Relation)item, out var route); return route.Uses.Length == 2; }).ToArray());
            Assert.Equal(2, E(Selected).Length);
        }
    }
    private static Relation S(params IElement[] entries) => StructuralConstraint.Closed(entries);
    private static IElement[] E(Relation scope) => StructuralConstraint.TryReadScope(scope, out var entries, out _, out _) ? entries : [];
    private static void AssertNoNumbers(IElement root)
    {
        var seen = new HashSet<IElement>(ReferenceEqualityComparer.Instance); var todo = new Stack<IElement>(); todo.Push(root);
        while (todo.TryPop(out var item)) if (seen.Add(item))
        { Assert.IsNotType<Carrier>(item); Assert.IsNotType<CarrierLocation>(item); if (item is Relation row) { todo.Push(row.Left); todo.Push(row.Right); } }
    }
}
