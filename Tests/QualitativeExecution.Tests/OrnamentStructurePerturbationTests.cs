using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;
using Gojo.Experiments.QualitativeFrieze;

namespace Gojo.Experiments.QualitativeExecution.Tests;

public sealed class OrnamentStructurePerturbationTests
{
    [Fact]
    public void WordDeltaRetainsTheOriginalBindingAndItsActualReductionWitness()
    {
        var f = new Fixture(); var old = f.Bindings[1]; var delta = S(QualitativeMotionFrame.Move(f.Frame.Right, f.Reach));
        var patch = OrnamentStructurePerturbation.WordDelta(f.Frame, f.Source, f.Topology, f.BindingScope, old, delta, new Placeholder());
        Assert.True(patch.IsComplete); Assert.Same(f.Topology, patch.Topology);
        var replacement = Assert.IsType<Relation>(Assert.Single(E(patch.Replacements)));
        Assert.Same(old, replacement.Left); var next = Assert.IsType<Relation>(replacement.Right);
        Assert.Same(old.Left, next.Left); Assert.Equal(2, E((Relation)next.Right).Length);
        Assert.Single(E((Relation)old.Right)); Assert.Same(old, E(f.BindingScope)[1]);
        Assert.Contains(old, Reach(patch.Surface)); Assert.Contains(delta, Reach(patch.Surface));
        Assert.Contains(next, Reach(patch.Surface)); Assert.Single(E(patch.Timeline));
        AssertNoNumbers(patch.Surface);
    }

    [Fact]
    public void WithdrawalRetiresOnlyTouchedDeclarationsAndDoesNotGuessNewFaces()
    {
        var f = new Fixture(); var edge = f.Edges[0];
        var patch = OrnamentStructurePerturbation.WithdrawEdge(f.Source, f.Topology, f.BindingScope, edge, new Placeholder());
        Assert.True(patch.IsComplete); Assert.Same(edge, Assert.Single(E(patch.Removed), item => ReferenceEquals(item, edge)));
        Assert.Same(f.Faces[0], Assert.Single(E(patch.WithdrawnFaces)));
        var topologyParts = Assert.IsType<Relation>(patch.Topology.Right);
        Assert.Equal(5, E((Relation)topologyParts.Left).Length);
        Assert.Same(f.Faces[1], Assert.Single(E((Relation)topologyParts.Right)));
        Assert.Equal(6, E((Relation)((Relation)f.Topology.Right).Left).Length);
        Assert.Empty(E(patch.Introduced)); Assert.Contains(f.Faces[1], E(patch.Retained));
    }

    [Fact]
    public void DetachmentMakesAFreshRoleAtTheSameWordWithoutRewritingTheOldJoin()
    {
        var f = new Fixture(); var edge = f.Edges[0]; var oldEnds = (Relation)edge.Right;
        var patch = OrnamentStructurePerturbation.DetachEndpoint(f.Source, f.Topology, f.BindingScope,
            edge, oldEnds.Right, new Placeholder());
        Assert.True(patch.IsComplete);
        var nextEdge = E((Relation)((Relation)patch.Topology.Right).Left).Cast<Relation>().Single(row => ReferenceEquals(row.Left, edge.Left));
        var nextEnds = Assert.IsType<Relation>(nextEdge.Right);
        Assert.Same(oldEnds.Left, nextEnds.Left); Assert.NotSame(oldEnds.Right, nextEnds.Right);
        var newBinding = E(patch.Bindings).Cast<Relation>().Single(row => ReferenceEquals(row.Left, nextEnds.Right));
        Assert.Same(f.Bindings[1].Right, newBinding.Right); Assert.Equal(7, E((Relation)patch.Topology.Left).Length);
        Assert.Same(f.Sites[1], oldEnds.Right); Assert.Same(f.Faces[0], Assert.Single(E(patch.WithdrawnFaces)));
        Assert.Contains(newBinding, Reach(patch.Surface)); Assert.Contains(f.Bindings[1], Reach(patch.Surface));
    }

    [Fact]
    public void CopiedEdgeAndBindingRowsCannotAuthorizeAChange()
    {
        var f = new Fixture(); var edge = f.Edges[0]; var binding = f.Bindings[0];
        var withdrawal = OrnamentStructurePerturbation.WithdrawEdge(f.Source, f.Topology, f.BindingScope,
            new Relation(edge.Left, edge.Right), new Placeholder());
        var word = OrnamentStructurePerturbation.ReplaceWord(f.Source, f.Topology, f.BindingScope,
            new Relation(binding.Left, binding.Right), S(), new Placeholder());
        foreach (var patch in new[] { withdrawal, word })
        { Assert.False(patch.IsComplete); Assert.Same(f.Topology, patch.Topology); Assert.Same(f.BindingScope, patch.Bindings); Assert.Empty(E(patch.Removed)); }
    }

    [Fact]
    public void OpenWordIsAnExplicitNewSupplyAndNotAnAccidentalZero()
    {
        var f = new Fixture(); var open = StructuralConstraint.Open(QualitativeMotionFrame.Move(f.Frame.Right, f.Reach));
        var patch = OrnamentStructurePerturbation.ReplaceWord(f.Source, f.Topology, f.BindingScope, f.Bindings[1], open, new Placeholder());
        var next = E(patch.Bindings).Cast<Relation>().Single(row => ReferenceEquals(row.Left, f.Sites[1]));
        Assert.Same(open, next.Right); Assert.False(StructuralConstraint.TryReadScope(open, out _, out var seam, out var tail) && seam.Equals(tail));
        Assert.Same(f.Bindings[1].Right, ((Relation)f.BindingScopeEntry(1)).Right);
    }

    private sealed class Fixture
    {
        internal QualitativeMotionFrame Frame { get; } = new();
        internal IElement Reach { get; } = new Placeholder();
        internal IElement Source { get; } = new Placeholder();
        internal IElement[] Sites { get; } = Enumerable.Range(0, 6).Select(_ => (IElement)new Placeholder()).ToArray();
        internal Relation[] Edges { get; }
        internal Relation[] Faces { get; }
        internal Relation[] Bindings { get; }
        internal Relation Topology { get; }
        internal Relation BindingScope { get; }
        internal Fixture()
        {
            Edges = Enumerable.Range(0, 6).Select(i => new Relation(new Placeholder(),
                new Relation(Sites[i], Sites[i / 3 * 3 + (i + 1) % 3]))).ToArray();
            Faces = Enumerable.Range(0, 2).Select(i => new Relation(new Placeholder(),
                S(Edges.Skip(i * 3).Take(3).Select(edge => (IElement)new Relation(edge.Left, edge.Right)).ToArray()))).ToArray();
            Topology = new(S(Sites), new Relation(S(Edges), S(Faces)));
            Bindings = Sites.Select((site, i) => new Relation(site, S(Enumerable.Repeat(
                (IElement)QualitativeMotionFrame.Move(Frame.Right, Reach), i).ToArray()))).ToArray();
            BindingScope = S(Bindings);
        }
        internal IElement BindingScopeEntry(int index) => E(BindingScope)[index];
    }
    private static Relation S(params IElement[] entries) => StructuralConstraint.Closed(entries);
    private static IElement[] E(Relation scope) => StructuralConstraint.TryReadScope(scope, out var rows, out _, out _) ? rows : [];
    private static HashSet<IElement> Reach(IElement root)
    {
        var seen = new HashSet<IElement>(ReferenceEqualityComparer.Instance); var stack = new Stack<IElement>(); stack.Push(root);
        while (stack.TryPop(out var item)) if (seen.Add(item) && item is Relation relation) { stack.Push(relation.Left); stack.Push(relation.Right); }
        return seen;
    }
    private static void AssertNoNumbers(IElement root) => Assert.DoesNotContain(Reach(root), item => item is Carrier or CarrierLocation);
}
