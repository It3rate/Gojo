using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeFrieze;

namespace Gojo.Experiments.QualitativeExecution.Tests;

public sealed class InteriorBoundaryNegotiationTests
{
    [Fact]
    public void AnExplicitCompromiseAssignsTheContestedInteriorOnceAndRetainsTheLosingProposal()
    {
        var f = Strip(); var left = new Placeholder(); var right = new Placeholder();
        var a = InteriorCellPartition.Claim(left, f.Faces[0], f.Faces[1]);
        var b = InteriorCellPartition.Claim(right, f.Faces[1], f.Faces[2]);
        var proposals = S(a, b); var decision = new Relation(f.Faces[1], right); var choices = S(decision);
        var read = InteriorBoundaryNegotiation.Reconcile(f.Complex, proposals, choices);
        Assert.True(read.IsSettled); Assert.Equal(1, read.Summary.ContestedAtoms);
        Assert.Equal(3, read.Summary.AcceptedAtoms); Assert.Equal(1, read.Summary.RejectedAtomClaims);
        Assert.Empty(E(read.Invalid)); Assert.Empty(E(read.Open)); Assert.Single(E(read.Blocked));
        Assert.True(InteriorCellPartition.Read(f.Complex, read.ResultClaims).IsPartition);
        var output = E(read.ResultClaims).Cast<Relation>().ToArray();
        Assert.Same(f.Faces[0], Assert.Single(E((Relation)output.Single(row => ReferenceEquals(row.Left, left)).Right)));
        Assert.Equal(new[] { f.Faces[1], f.Faces[2] }, E((Relation)output.Single(row => ReferenceEquals(row.Left, right)).Right));
        Assert.Same(proposals, ((Relation)read.Source.Right).Left); Assert.Same(choices, ((Relation)read.Source.Right).Right);
        Assert.Contains(E((Relation)a.Right), item => ReferenceEquals(item, f.Faces[1]));
        Assert.Contains(E((Relation)b.Right), item => ReferenceEquals(item, f.Faces[1]));
        var loss = Assert.IsType<Relation>(Assert.Single(E(read.RejectedClaims)));
        Assert.Same(f.Faces[1], ((Relation)loss.Left).Left); Assert.Same(a, ((Relation)loss.Left).Right);
        Assert.Same(decision, ((Relation)loss.Right).Left); Assert.Same(b, ((Relation)loss.Right).Right);
        Assert.All(Reachable(read.Surface), item => Assert.True(item is Placeholder or Relation));
    }

    [Fact]
    public void MissingCompromiseLeavesOnlyTheContestedAtomOpenWithoutFirstClaimWinning()
    {
        var f = Strip(); var a = InteriorCellPartition.Claim(new Placeholder(), f.Faces[0], f.Faces[1]);
        var b = InteriorCellPartition.Claim(new Placeholder(), f.Faces[1], f.Faces[2]);
        foreach (var proposals in new[] { S(a, b), S(b, a) })
        {
            var read = InteriorBoundaryNegotiation.Reconcile(f.Complex, proposals, S());
            Assert.False(read.IsSettled); Assert.Equal(1, read.Summary.OpenAtoms); Assert.Equal(2, read.Summary.AcceptedAtoms);
            Assert.Empty(E(read.Invalid)); Assert.Empty(E(read.RejectedClaims));
            Assert.DoesNotContain(Assigned(read), atom => ReferenceEquals(atom, f.Faces[1]));
            Assert.False(InteriorCellPartition.Read(f.Complex, read.ResultClaims).IsPartition);
        }
    }

    [Fact]
    public void ALookalikeFaceRowIsNotActualComplexMembership()
    {
        var f = Strip(); var copy = new Relation(f.Faces[1].Left, f.Faces[1].Right);
        var proposals = S(InteriorCellPartition.Claim(new Placeholder(), f.Faces[0], copy, f.Faces[2]));
        var read = InteriorBoundaryNegotiation.Reconcile(f.Complex, proposals, S());
        Assert.False(read.IsSettled); Assert.NotEmpty(E(read.Invalid)); Assert.Empty(E(read.ResultClaims));
        Assert.Equal(3, read.Summary.OpenAtoms);
        Assert.Contains(Reachable(read.Invalid), item => ReferenceEquals(item, copy));
    }

    [Fact]
    public void ContradictoryChoicesRemainUnresolvedInEitherOrder()
    {
        var f = Strip(); var left = new Placeholder(); var right = new Placeholder();
        var proposals = S(InteriorCellPartition.Claim(left, f.Faces[0], f.Faces[1]),
            InteriorCellPartition.Claim(right, f.Faces[1], f.Faces[2]));
        var a = new Relation(f.Faces[1], left); var b = new Relation(f.Faces[1], right);
        foreach (var choices in new[] { S(a, b), S(b, a), S(a, a) })
        {
            var read = InteriorBoundaryNegotiation.Reconcile(f.Complex, proposals, choices);
            Assert.False(read.IsSettled); Assert.NotEmpty(E(read.Invalid)); Assert.NotEmpty(E(read.Open));
            Assert.DoesNotContain(Assigned(read), atom => ReferenceEquals(atom, f.Faces[1]));
            Assert.Empty(E(read.RejectedClaims));
        }
    }

    [Fact]
    public void AForeignWinnerOrUncontroversialResolutionCannotInventConflictEvidence()
    {
        var f = Strip(); var left = new Placeholder(); var right = new Placeholder();
        var proposals = S(InteriorCellPartition.Claim(left, f.Faces[0], f.Faces[1]),
            InteriorCellPartition.Claim(right, f.Faces[1], f.Faces[2]));
        foreach (var choices in new[]
        {
            S(new Relation(f.Faces[1], new Placeholder())),
            S(new Relation(f.Faces[1], right), new Relation(f.Faces[0], left)),
            S(new Relation(new Relation(f.Faces[1].Left, f.Faces[1].Right), right))
        })
        {
            var read = InteriorBoundaryNegotiation.Reconcile(f.Complex, proposals, choices);
            Assert.False(read.IsSettled); Assert.NotEmpty(E(read.Invalid));
        }
    }

    [Fact]
    public void DuplicateGroupIdentitiesAndOpenScopesNeverSelectAFirstBinding()
    {
        var f = Strip(); var group = new Placeholder();
        var a = InteriorCellPartition.Claim(group, f.Faces[0], f.Faces[1]);
        var b = InteriorCellPartition.Claim(group, f.Faces[2]);
        var duplicate = InteriorBoundaryNegotiation.Reconcile(f.Complex, S(a, b), S());
        Assert.False(duplicate.IsSettled); Assert.NotEmpty(E(duplicate.Invalid)); Assert.Empty(E(duplicate.ResultClaims));
        var open = InteriorBoundaryNegotiation.Reconcile(f.Complex, StructuralConstraint.Open(a), S());
        Assert.False(open.IsSettled); Assert.NotEmpty(E(open.Open)); Assert.Empty(E(open.ResultClaims));
    }

    [Fact]
    public void MalformedOrDuplicateAdvertisedAtomsRemainVisibleWithoutThrowing()
    {
        var f = Strip(); var repeated = InteriorCellPartition.Topology((Relation)f.Complex.Left,
            (Relation)((Relation)f.Complex.Right).Left, S(f.Faces[0], f.Faces[0], f.Faces[2]));
        var read = InteriorBoundaryNegotiation.Reconcile(repeated,
            S(InteriorCellPartition.Claim(new Placeholder(), f.Faces[0], f.Faces[2])), S());
        Assert.False(read.IsSettled); Assert.NotEmpty(E(read.Invalid));
        var malformed = InteriorBoundaryNegotiation.Reconcile(new Relation(new Placeholder(), new Placeholder()), S(), S());
        Assert.False(malformed.IsSettled); Assert.NotEmpty(E(malformed.Invalid));
    }

    [Fact]
    public void ASettledClaimResolutionDoesNotPretendToProveCellConnectivity()
    {
        var f = Strip(); var proposals = S(InteriorCellPartition.Claim(new Placeholder(), f.Faces[0], f.Faces[2]),
            InteriorCellPartition.Claim(new Placeholder(), f.Faces[1]));
        var negotiation = InteriorBoundaryNegotiation.Reconcile(f.Complex, proposals, S());
        Assert.True(negotiation.IsSettled); Assert.False(InteriorCellPartition.Read(f.Complex, negotiation.ResultClaims).IsPartition);
    }

    private static Relation[] Assigned(InteriorBoundaryNegotiation read) => E(read.ResultClaims).Cast<Relation>()
        .SelectMany(claim => E((Relation)claim.Right).Cast<Relation>()).ToArray();
    private static Relation S(params IElement[] items) => StructuralConstraint.Closed(items);
    private static IElement[] E(Relation scope) => StructuralConstraint.TryReadScope(scope, out var entries, out _, out _) ? entries : [];
    private static IEnumerable<IElement> Reachable(IElement source)
    {
        var seen = new HashSet<IElement>(ReferenceEqualityComparer.Instance); var pending = new Stack<IElement>(); pending.Push(source);
        while (pending.TryPop(out var item))
        {
            if (!seen.Add(item)) continue; yield return item;
            if (item is Relation relation) { pending.Push(relation.Right); pending.Push(relation.Left); }
        }
    }

    private static (Relation Complex, Relation[] Faces) Strip()
    {
        var sites = Enumerable.Range(0, 8).Select(_ => (IElement)new Placeholder()).ToArray();
        var edges = new List<Relation>(); var byEnds = new Dictionary<(int, int), Relation>();
        Relation Dart(int a, int b)
        {
            var key = (Math.Min(a, b), Math.Max(a, b));
            if (!byEnds.TryGetValue(key, out var edge))
            { edge = InteriorCellPartition.Edge(new Placeholder(), sites[a], sites[b]); byEnds.Add(key, edge); edges.Add(edge); }
            return InteriorCellPartition.Dart(edge, sites[a], sites[b]);
        }
        var faces = Enumerable.Range(0, 3).Select(x => InteriorCellPartition.Face(new Placeholder(),
            Dart(x, x + 1), Dart(x + 1, x + 5), Dart(x + 5, x + 4), Dart(x + 4, x))).ToArray();
        return (InteriorCellPartition.Topology(S(sites), S(edges.ToArray()), S(faces)), faces);
    }
}
