using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeFrieze;

namespace Gojo.Experiments.QualitativeExecution.Tests;

public sealed class OrnamentCellDiscoveryTests
{
    [Fact]
    public void RecurringCyclesHaveNativeRoleCorrespondencesWhileSingletonFacesRemainComplement()
    {
        var patch = Cycles(3, 4, 4, 8, 8); var source = new Placeholder(); var question = new Placeholder();
        var topology = patch.Topology; var read = new OrnamentCellDiscovery(source, topology, question);
        Assert.True(read.IsComplete); Assert.Equal(2, E(read.Families).Length);
        Assert.Same(patch.Faces[0], Assert.Single(E(read.Complement)));
        Assert.Contains(source, Reach(read.Surface)); Assert.Contains(question, Reach(read.Surface));
        Assert.Contains(topology, Reach(read.Surface));
        foreach (var row in E(read.Families).Cast<Relation>())
        {
            Assert.True(read.TryReadFamily(row, out var family));
            Assert.Contains(family.BoundaryArity, new[] { 4, 8 }); Assert.Equal(2, E(family.Occurrences).Length);
            var signatureEdges = E((Relation)((Relation)family.Signature.Right).Left).Cast<Relation>().ToArray();
            var signatureSites = E((Relation)family.Signature.Left);
            Assert.Equal(family.BoundaryArity, signatureEdges.Length); Assert.Equal(family.BoundaryArity, signatureSites.Length);
            foreach (var occurrence in E(family.Occurrences).Cast<Relation>())
            {
                Assert.True(read.TryReadOccurrence(row, occurrence, out var identity, out var boundary, out var correspondence));
                var face = Assert.Single(patch.Faces, face => ReferenceEquals(face.Left, identity));
                Assert.Same(face.Right, boundary); Assert.Same(face, occurrence.Left);
                var proof = E(correspondence).Cast<Relation>().ToArray();
                Assert.Equal(family.BoundaryArity, proof.Length);
                var actualDarts = E(boundary); var canonicalDarts = proof.Select(pair => Assert.IsType<Relation>(pair.Left)).ToArray();
                for (var i = 0; i < proof.Length; i++)
                {
                    Assert.Same(actualDarts[i], proof[i].Right);
                    var canonical = canonicalDarts[i]; var ends = Assert.IsType<Relation>(canonical.Right);
                    Assert.Contains(signatureEdges, edge => ReferenceEquals(edge.Left, canonical.Left) && ReferenceEquals(edge.Right, ends));
                    Assert.Same(ends.Right, ((Relation)canonicalDarts[(i + 1) % proof.Length].Right).Left);
                    Assert.DoesNotContain(signatureSites, site => ReferenceEquals(site, ((Relation)((Relation)proof[i].Right).Right).Left));
                }
            }
        }
        AssertNoNumbers(read.Surface);
    }

    [Fact]
    public void FreshIdentitiesReorderedSuppliesAndDifferentCycleStartsNeedNoHistoryOrNames()
    {
        var patch = Cycles(6, 6, 6, 5); var random = new Random(1729);
        var faces = patch.Faces.Select((face, index) =>
        {
            var darts = E((Relation)face.Right).Cast<Relation>().ToArray();
            if (index % 2 == 0) darts = darts.Reverse().Select(dart => new Relation(dart.Left,
                new Relation(((Relation)dart.Right).Right, ((Relation)dart.Right).Left))).ToArray();
            darts = darts.Skip(index).Concat(darts.Take(index)).ToArray();
            return (IElement)new Relation(new Placeholder(), S(darts));
        }).OrderBy(_ => random.Next()).ToArray();
        var topology = new Relation(S(patch.Sites.OrderBy(_ => random.Next()).ToArray()),
            new Relation(S(patch.Edges.OrderBy(_ => random.Next()).ToArray()), S(faces)));
        var result = new OrnamentCellDiscovery(new Placeholder(), topology, new Placeholder());
        Assert.True(result.IsComplete); var familyRow = Assert.IsType<Relation>(Assert.Single(E(result.Families)));
        Assert.True(result.TryReadFamily(familyRow, out var family));
        Assert.Equal(6, family.BoundaryArity); Assert.Equal(3, E(family.Occurrences).Length);
        Assert.Single(E(result.Complement)); AssertNoNumbers(result.Surface);
    }

    [Fact]
    public void CoarseCycleQuestionDoesNotRequireAnAuthoredShapeOrEqualNeighbourhood()
    {
        var patch = Cycles(4, 4); var extra = new Placeholder();
        var edge = new Relation(new Placeholder(), new Relation(patch.Sites[0], extra));
        var topology = new Relation(S(patch.Sites.Append(extra).ToArray()),
            new Relation(S(patch.Edges.Append(edge).ToArray()), S(patch.Faces)));
        var read = new OrnamentCellDiscovery(new Placeholder(), topology, new Placeholder());
        Assert.True(read.IsComplete); var row = Assert.IsType<Relation>(Assert.Single(E(read.Families)));
        Assert.True(read.TryReadFamily(row, out var family)); Assert.Equal(4, family.BoundaryArity);
        // Only a fresh four-edge cycle was discovered. No lengths, right angles,
        // orientation, lattice translations or square/diamond identity exist.
        Assert.Equal(4, E((Relation)family.Signature.Left).Length); AssertNoNumbers(family.Signature);
        Assert.Contains(edge, Reach(read.Surface));
    }

    [Fact]
    public void OpenBoundaryCannotSupplyTheSecondOccurrence()
    {
        var patch = Cycles(4, 4); var original = patch.Faces[1];
        var openBoundary = StructuralConstraint.Open(E((Relation)original.Right));
        var openFace = new Relation(original.Left, openBoundary);
        var read = new OrnamentCellDiscovery(new Placeholder(), patch.WithFaces(patch.Faces[0], openFace), new Placeholder());
        Assert.False(read.IsComplete); Assert.Empty(E(read.Families)); Assert.Empty(E(read.Blocked));
        Assert.NotEmpty(E(read.Unresolved)); Assert.Contains(openBoundary, Reach(read.Unresolved));
        Assert.Same(patch.Faces[0], Assert.Single(E(read.Complement)));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void OpenTopologyScopesRetainUncertaintyWithoutPublishingAClosedRecurrence(int scope)
    {
        var patch = Cycles(4, 4);
        var sites = scope == 0 ? StructuralConstraint.Open(patch.Sites) : S(patch.Sites);
        var edges = scope == 1 ? StructuralConstraint.Open(patch.Edges) : S(patch.Edges);
        var faces = scope == 2 ? StructuralConstraint.Open(patch.Faces) : S(patch.Faces);
        var read = new OrnamentCellDiscovery(new Placeholder(), new Relation(sites, new Relation(edges, faces)), new Placeholder());
        Assert.False(read.IsComplete); Assert.Empty(E(read.Families)); Assert.Empty(E(read.Complement));
        Assert.Empty(E(read.Blocked)); Assert.NotEmpty(E(read.Unresolved));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void ForeignEdgesForeignEndpointsBrokenCyclesAndRepeatedEdgesCannotBecomeOccurrences(int corruption)
    {
        var patch = Cycles(4, 4); var face = patch.Faces[1]; var darts = E((Relation)face.Right).Cast<Relation>().ToArray();
        var original = darts[0]; var ends = (Relation)original.Right;
        darts[0] = corruption switch
        {
            0 => new Relation(new Placeholder(), ends),
            1 => new Relation(original.Left, new Relation(new Placeholder(), ends.Right)),
            2 => new Relation(original.Left, new Relation(ends.Right, ends.Left)),
            _ => darts[1]
        };
        var invalidFace = new Relation(face.Left, S(darts));
        var read = new OrnamentCellDiscovery(new Placeholder(), patch.WithFaces(patch.Faces[0], invalidFace), new Placeholder());
        Assert.False(read.IsComplete); Assert.Empty(E(read.Families)); Assert.NotEmpty(E(read.Blocked));
        Assert.Contains(invalidFace, Reach(read.Blocked)); Assert.Same(patch.Faces[0], Assert.Single(E(read.Complement)));
    }

    [Fact]
    public void AmbiguousFaceIdentityCannotSupplyIndependentOccurrences()
    {
        var patch = Cycles(4, 4); var duplicateIdentity = new Relation(patch.Faces[0].Left, patch.Faces[1].Right);
        var read = new OrnamentCellDiscovery(new Placeholder(), patch.WithFaces(patch.Faces[0], duplicateIdentity), new Placeholder());
        Assert.False(read.IsComplete); Assert.Empty(E(read.Families)); Assert.Empty(E(read.Complement));
        Assert.NotEmpty(E(read.Blocked));
    }

    [Fact]
    public void CompleteRecurrenceSurvivesAnotherRejectedFaceWithoutLosingItsEvidence()
    {
        var patch = Cycles(3, 3, 4); var bad = new Relation(patch.Faces[2].Left, S());
        var read = new OrnamentCellDiscovery(new Placeholder(), patch.WithFaces(patch.Faces[0], patch.Faces[1], bad), new Placeholder());
        Assert.False(read.IsComplete); var row = Assert.IsType<Relation>(Assert.Single(E(read.Families)));
        Assert.True(read.TryReadFamily(row, out var family)); Assert.Equal(3, family.BoundaryArity);
        Assert.Equal(2, E(family.Occurrences).Length); Assert.Contains(bad, Reach(read.Blocked));
    }

    [Fact]
    public void ReadersRejectCopiedRowsAndCrossFamilyOccurrences()
    {
        var patch = Cycles(3, 3, 4, 4); var read = new OrnamentCellDiscovery(new Placeholder(), patch.Topology, new Placeholder());
        var rows = E(read.Families).Cast<Relation>().ToArray(); Assert.True(read.TryReadFamily(rows[0], out var family));
        var occurrence = Assert.IsType<Relation>(E(family.Occurrences)[0]);
        Assert.False(read.TryReadFamily(new Relation(rows[0].Left, rows[0].Right), out _));
        Assert.False(read.TryReadOccurrence(rows[0], new Relation(occurrence.Left, occurrence.Right), out _, out _, out _));
        Assert.False(read.TryReadOccurrence(rows[1], occurrence, out _, out _, out _));
    }

    private static Patch Cycles(params int[] arities)
    {
        var allSites = new List<IElement>(); var allEdges = new List<IElement>(); var faces = new List<Relation>();
        foreach (var arity in arities)
        {
            var sites = Enumerable.Range(0, arity).Select(_ => (IElement)new Placeholder()).ToArray();
            var darts = new List<IElement>(); allSites.AddRange(sites);
            for (var i = 0; i < arity; i++)
            {
                var edgeIdentity = new Placeholder(); var ends = new Relation(sites[i], sites[(i + 1) % arity]);
                // Advertised edge direction and boundary traversal are distinct.
                allEdges.Add(new Relation(edgeIdentity, i % 2 == 0 ? ends : new Relation(ends.Right, ends.Left)));
                darts.Add(new Relation(edgeIdentity, ends));
            }
            faces.Add(new Relation(new Placeholder(), S(darts.ToArray())));
        }
        return new(allSites.ToArray(), allEdges.ToArray(), faces.ToArray());
    }
    private sealed class Patch(IElement[] sites, IElement[] edges, Relation[] faces)
    {
        internal IElement[] Sites { get; } = sites;
        internal IElement[] Edges { get; } = edges;
        internal Relation[] Faces { get; } = faces;
        internal Relation Topology => WithFaces(Faces);
        internal Relation WithFaces(params Relation[] supplied) => new(S(Sites), new Relation(S(Edges), S(supplied)));
    }
    private static Relation S(params IElement[] entries) => StructuralConstraint.Closed(entries);
    private static IElement[] E(Relation scope) => StructuralConstraint.TryReadScope(scope, out var entries, out _, out _) ? entries : [];
    private static HashSet<IElement> Reach(IElement source)
    {
        var seen = new HashSet<IElement>(ReferenceEqualityComparer.Instance); var pending = new Stack<IElement>(); pending.Push(source);
        while (pending.TryPop(out var item))
            if (seen.Add(item) && item is Relation row) { pending.Push(row.Left); pending.Push(row.Right); }
        return seen;
    }
    private static void AssertNoNumbers(IElement source) => Assert.DoesNotContain(Reach(source), item => item is Carrier or CarrierLocation);
}
