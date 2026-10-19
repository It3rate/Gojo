using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeFrieze;

namespace Gojo.Experiments.QualitativeExecution.Tests;

public sealed class InteriorCellPartitionTests
{
    [Fact]
    public void DeletingTwoSharedDividersProducesAnExclusiveLInterior()
    {
        var fixture = Grid(2, 2); var claims = Claims(fixture, [0, 1, 2], [3]);
        var read = fixture.Read(claims);
        Assert.True(read.IsPartition); Assert.True(read.IsEmbeddedPartition);
        var merged = Assert.Single(read.GroupReads, g => g.Atoms.Count == 3);
        Assert.Equal(2, E(merged.RemovedInternalEdges).Length); Assert.Equal(8, merged.BoundaryDarts.Count);
        Assert.NotNull(merged.Shape); Assert.Equal(6, merged.Shape.Sides); Assert.Equal(5, merged.Shape.ConvexCorners);
        Assert.Equal(1, merged.Shape.ReflexCorners); Assert.Equal(3, merged.Shape.Area);
        Assert.All(merged.BoundaryDarts, dart => Assert.Contains(dart, fixture.Faces.Take(3).SelectMany(f => E((Relation)f.Right))));
        Assert.Contains(fixture.Faces[0], Reachable(merged.Surface)); Assert.Contains(fixture.Bindings, Reachable(read.Surface));
        AssertNative(read.Surface);
    }

    [Fact]
    public void OverlappingGroupClaimsRetainActualMultiplyOwnedAtomAndBothClaims()
    {
        var fixture = Grid(2, 2); var claims = Claims(fixture, [0, 1], [1, 2, 3]);
        var read = fixture.Read(claims);
        Assert.False(read.IsPartition); Assert.False(read.IsEmbeddedPartition);
        Assert.Equal(1, read.Summary.MultiplyOwnedAtoms); Assert.Empty(read.GroupReads);
        var witness = Assert.Single(E(read.Blocked));
        Assert.Same(fixture.Faces[1], ((Relation)witness).Left);
        Assert.All(E(claims), claim => Assert.Contains(claim, Reachable(witness)));
    }

    [Fact]
    public void AForeignFaceWrapperCannotClaimAnActualAtomicInterior()
    {
        var fixture = Grid(1, 1); var face = fixture.Faces[0]; var foreign = new Relation(face.Left, face.Right);
        var read = fixture.Read(S(InteriorCellPartition.Claim(new Placeholder(), foreign)));
        Assert.False(read.IsPartition); Assert.Empty(read.GroupReads);
        Assert.Contains(foreign, Reachable(read.Blocked)); Assert.Contains(face, Reachable(read.Open));
    }

    [Fact]
    public void OmittedAtomsRemainOpenInsteadOfBeingSilentlyDropped()
    {
        var fixture = Grid(2, 1); var read = fixture.Read(Claims(fixture, [0]));
        Assert.False(read.IsPartition); Assert.Equal(1, read.Summary.UnownedAtoms);
        Assert.Contains(fixture.Faces[1], Reachable(read.Open)); Assert.Empty(E(read.Blocked));
    }

    [Fact]
    public void DiagonalPointContactDoesNotConnectTwoMergedAtomicInteriors()
    {
        var fixture = Grid(2, 2); var claims = Claims(fixture, [0, 3], [1], [2]); var read = fixture.Read(claims);
        Assert.False(read.IsPartition); Assert.Equal(2, read.GroupReads.Count);
        Assert.Contains(E(claims)[0], Reachable(read.Blocked));
        Assert.Contains(fixture.Faces[3], Reachable(read.Blocked));
    }

    [Fact]
    public void AConnectedRingStillFailsTheSingleBoundaryCycleContract()
    {
        var fixture = Grid(3, 3); var outer = Enumerable.Range(0, 9).Where(i => i != 4).ToArray();
        var claims = Claims(fixture, outer, [4]); var read = fixture.Read(claims);
        Assert.False(read.IsPartition); var middle = Assert.Single(read.GroupReads);
        Assert.Same(fixture.Faces[4], Assert.Single(middle.Atoms)); Assert.Contains(E(claims)[0], Reachable(read.Blocked));
    }

    [Fact]
    public void ConflictingSameDirectionIncidenceCannotBecomeAPlanarComplex()
    {
        var fixture = Grid(1, 1); var duplicate = new Relation(new Placeholder(), fixture.Faces[0].Right);
        var topology = InteriorCellPartition.Topology(fixture.Sites, fixture.Edges, S(fixture.Faces[0], duplicate));
        var read = InteriorCellPartition.Read(topology, S(InteriorCellPartition.Claim(new Placeholder(), fixture.Faces[0]),
            InteriorCellPartition.Claim(new Placeholder(), duplicate)), fixture.Bindings, fixture.Author.Frame.Surface, fixture.Author.Reach);
        Assert.False(read.IsPartition); Assert.Contains(duplicate, Reachable(read.Blocked));
    }

    [Fact]
    public void ExclusiveTopologyDoesNotExcuseAnEmbeddingWithOverlappingAtoms()
    {
        var fixture = IndependentRectangles((0, 0, 2, 2), (1, 1, 3, 3));
        var read = fixture.Read(Claims(fixture, [0], [1]));
        Assert.True(read.IsPartition); Assert.False(read.IsEmbeddedPartition); Assert.Equal(1, read.Summary.OverlappingAtomPairs);
        Assert.All(fixture.Faces, face => Assert.Contains(face, Reachable(read.EmbeddingBlocked)));
        AssertNative(read.Surface);
    }

    [Fact]
    public void CoordinateContactDoesNotSilentlySupplyTheMissingSharedEdgeIdentity()
    {
        var fixture = IndependentRectangles((0, 0, 1, 1), (1, 0, 2, 1));
        var read = fixture.Read(Claims(fixture, [0], [1]));
        Assert.True(read.IsPartition); Assert.False(read.IsEmbeddedPartition);
        Assert.Equal(0, read.Summary.OverlappingAtomPairs); Assert.NotEmpty(E(read.EmbeddingBlocked));
    }

    [Fact]
    public void ForeignReachAndMissingEmbeddingRemainOpenAtTheirOwnRead()
    {
        var fixture = Grid(1, 1); var claims = Claims(fixture, [0]);
        var topologyOnly = InteriorCellPartition.Read(fixture.Complex, claims);
        Assert.True(topologyOnly.IsPartition); Assert.False(topologyOnly.IsEmbeddedPartition);
        Assert.Single(E(topologyOnly.EmbeddingOpen)); Assert.Null(Assert.Single(topologyOnly.GroupReads).Shape);
        var bindings = E(fixture.Bindings).Cast<Relation>().ToArray(); var foreign = new LatticeLadderAuthor(fixture.Author.Frame, new Placeholder());
        bindings[1] = new(bindings[1].Left, foreign.Word(1, 0));
        var read = InteriorCellPartition.Read(fixture.Complex, claims, S(bindings), fixture.Author.Frame.Surface, fixture.Author.Reach);
        Assert.True(read.IsPartition); Assert.False(read.IsEmbeddedPartition); Assert.NotEmpty(E(read.EmbeddingOpen));
        Assert.Empty(E(read.EmbeddingBlocked));
    }

    [Fact]
    public void StraightAtomicSubdivisionsDoNotCountAsNewShapeSides()
    {
        var fixture = Grid(4, 2); var read = fixture.Read(Claims(fixture, Enumerable.Range(0, 8).ToArray()));
        Assert.True(read.IsEmbeddedPartition); var shape = Assert.Single(read.GroupReads).Shape!;
        Assert.Equal(12, shape.AuthoredBoundaryDarts); Assert.Equal(4, shape.Sides);
        Assert.Equal(4, shape.ConvexCorners); Assert.Equal(0, shape.ReflexCorners);
        Assert.Equal(8, shape.Area); Assert.Equal(2, shape.AspectRatio); Assert.Equal(0, shape.FineToothReturns);
    }

    [Fact]
    public void OpenClaimScopesAndZeroAreaEmbeddingCannotProduceCertainFill()
    {
        var fixture = Grid(1, 1); var group = new Relation(new Placeholder(), StructuralConstraint.Open(fixture.Faces));
        var read = fixture.Read(S(group)); Assert.False(read.IsPartition); Assert.Contains(group.Right, Reachable(read.Open));
        var flat = E(fixture.Bindings).Cast<Relation>().Select((b, i) => (IElement)new Relation(b.Left, fixture.Author.Word(i, 0))).ToArray();
        var zeroArea = InteriorCellPartition.Read(fixture.Complex, Claims(fixture, [0]), S(flat), fixture.Author.Frame.Surface, fixture.Author.Reach);
        Assert.True(zeroArea.IsPartition); Assert.False(zeroArea.IsEmbeddedPartition); Assert.NotEmpty(E(zeroArea.EmbeddingBlocked));
    }

    [Fact]
    public void SmallReturnsAlongOneHeadingDifferFromLargeTurningArms()
    {
        var toothed = Grid(12, 5);
        var teeth = Enumerable.Range(0, 60).Where(i => i / 12 < 4 || i % 2 == 0).ToArray();
        var toothedGroups = new[] { teeth }.Concat(Enumerable.Range(0, 60).Except(teeth).Select(i => new[] { i })).ToArray();
        var toothedRead = toothed.Read(Claims(toothed, toothedGroups)); Assert.True(toothedRead.IsEmbeddedPartition);
        var noisy = toothedRead.GroupReads.Single(g => g.Atoms.Count == teeth.Length).Shape!;
        var star = Grid(8, 8);
        var arms = Enumerable.Range(0, 64).Where(i => i % 8 is >= 2 and <= 5 || i / 8 is >= 2 and <= 5).ToArray();
        var starGroups = new[] { arms }.Concat(Enumerable.Range(0, 64).Except(arms).Select(i => new[] { i })).ToArray();
        var starRead = star.Read(Claims(star, starGroups)); Assert.True(starRead.IsEmbeddedPartition);
        var turning = starRead.GroupReads.Single(g => g.Atoms.Count == arms.Length).Shape!;
        Assert.True(noisy.FineToothReturns >= 4); Assert.Equal(0, turning.FineToothReturns);
        Assert.Equal(12, turning.Sides); Assert.Equal(4, turning.ReflexCorners);
        Assert.True(turning.TurnAlternationRatio > 0); // Alternation alone is not a noise verdict.
    }

    [Fact]
    public void AUniformlyTurningSelfCrossingAtomicStarDoesNotSupplyAConvexFillPremise()
    {
        var author = new LatticeLadderAuthor(); (int X, int Y)[] positions = [(0, 3), (2, -3), (-3, 1), (3, 1), (-2, -3)];
        var sites = positions.Select(_ => (IElement)new Placeholder()).ToArray();
        var edges = sites.Select((site, i) => InteriorCellPartition.Edge(new Placeholder(), site, sites[(i + 1) % sites.Length])).ToArray();
        var face = InteriorCellPartition.Face(new Placeholder(), edges.Select((edge, i) => InteriorCellPartition.Dart(edge, sites[i], sites[(i + 1) % sites.Length])).ToArray());
        var complex = InteriorCellPartition.Topology(S(sites), S(edges), S(face));
        var bindings = S(sites.Select((site, i) => (IElement)new Relation(site, author.Word(positions[i].X, positions[i].Y))).ToArray());
        var read = InteriorCellPartition.Read(complex, S(InteriorCellPartition.Claim(new Placeholder(), face)), bindings, author.Frame.Surface, author.Reach);
        Assert.True(read.IsPartition); Assert.False(read.IsEmbeddedPartition);
        Assert.Contains(face, Reachable(read.EmbeddingBlocked)); Assert.Null(Assert.Single(read.GroupReads).Shape);
    }

    private static Fixture Grid(int columns, int rows)
    {
        var author = new LatticeLadderAuthor(); var sites = new Dictionary<(int X, int Y), IElement>();
        for (var y = 0; y <= rows; y++) for (var x = 0; x <= columns; x++) sites.Add((x, y), new Placeholder());
        var edgeRows = new Dictionary<((int, int), (int, int)), Relation>(); var faces = new List<Relation>();
        for (var y = 0; y < rows; y++) for (var x = 0; x < columns; x++)
        {
            (int, int)[] points = [(x, y), (x + 1, y), (x + 1, y + 1), (x, y + 1)]; var darts = new List<Relation>();
            for (var i = 0; i < 4; i++)
            {
                var a = points[i]; var b = points[(i + 1) % 4]; var key = a.CompareTo(b) < 0 ? (a, b) : (b, a);
                if (!edgeRows.TryGetValue(key, out var edge)) edgeRows.Add(key, edge = InteriorCellPartition.Edge(new Placeholder(), sites[a], sites[b]));
                darts.Add(InteriorCellPartition.Dart(edge, sites[a], sites[b]));
            }
            faces.Add(InteriorCellPartition.Face(new Placeholder(), darts.ToArray()));
        }
        return new(author, S(sites.Values.ToArray()), S(edgeRows.Values.ToArray()), faces.ToArray(),
            S(sites.Select(p => (IElement)new Relation(p.Value, author.Word(p.Key.X, p.Key.Y))).ToArray()));
    }
    private static Fixture IndependentRectangles(params (int X0, int Y0, int X1, int Y1)[] boxes)
    {
        var author = new LatticeLadderAuthor(); var sites = new List<IElement>(); var edges = new List<Relation>(); var faces = new List<Relation>(); var bindings = new List<IElement>();
        foreach (var (x0, y0, x1, y1) in boxes)
        {
            var local = Enumerable.Range(0, 4).Select(_ => (IElement)new Placeholder()).ToArray();
            (int X, int Y)[] positions = [(x0, y0), (x1, y0), (x1, y1), (x0, y1)]; var darts = new List<Relation>(); sites.AddRange(local);
            for (var i = 0; i < 4; i++)
            {
                var edge = InteriorCellPartition.Edge(new Placeholder(), local[i], local[(i + 1) % 4]); edges.Add(edge);
                darts.Add(InteriorCellPartition.Dart(edge, local[i], local[(i + 1) % 4]));
                bindings.Add(new Relation(local[i], author.Word(positions[i].X, positions[i].Y)));
            }
            faces.Add(InteriorCellPartition.Face(new Placeholder(), darts.ToArray()));
        }
        return new(author, S(sites.ToArray()), S(edges.ToArray()), faces.ToArray(), S(bindings.ToArray()));
    }
    private static Relation Claims(Fixture fixture, params int[][] groups) => S(groups.Select(indices => (IElement)InteriorCellPartition.Claim(new Placeholder(), indices.Select(i => fixture.Faces[i]).ToArray())).ToArray());
    private sealed record Fixture(LatticeLadderAuthor Author, Relation Sites, Relation Edges, Relation[] Faces, Relation Bindings)
    {
        public Relation Complex => InteriorCellPartition.Topology(Sites, Edges, S(Faces));
        public InteriorCellPartition Read(Relation claims) => InteriorCellPartition.Read(Complex, claims, Bindings, Author.Frame.Surface, Author.Reach);
    }
    private static Relation S(params IElement[] entries) => StructuralConstraint.Closed(entries);
    private static IElement[] E(Relation scope) { Assert.True(StructuralConstraint.TryReadScope(scope, out var entries, out _, out _)); return entries; }
    private static HashSet<IElement> Reachable(IElement root)
    {
        var seen = new HashSet<IElement>(ReferenceEqualityComparer.Instance); var pending = new Stack<IElement>(); pending.Push(root);
        while (pending.TryPop(out var item)) if (seen.Add(item) && item is Relation relation) { pending.Push(relation.Left); pending.Push(relation.Right); }
        return seen;
    }
    private static void AssertNative(IElement root) => Assert.All(Reachable(root), item => Assert.True(item is Relation or Placeholder));
}
