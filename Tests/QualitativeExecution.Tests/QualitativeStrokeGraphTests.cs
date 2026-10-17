using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeFrieze;

namespace Gojo.Experiments.QualitativeExecution.Tests;

public sealed class QualitativeStrokeGraphTests
{
    [Fact]
    public void SharedActualSiteWithOneOpposedPairIsATJunction()
    {
        var f = new Fixture(); var graph = f.Graph(4, (0, 1, f.R), (0, 2, f.L), (0, 3, f.U)); var summary = graph.Describe(graph.Evaluate());
        Assert.Equal(1, summary.T); Assert.Equal(0, summary.Y); Assert.Equal(3, summary.Endpoints); Assert.Equal(1, summary.Branches); Assert.Equal(0, summary.Unresolved);
        var junction = Node(graph, f.Sites[0]); Assert.True(graph.TryReadJunction(junction, out var site, out var rays, out var opposed, out var unknown));
        Assert.Same(f.Sites[0], site); Assert.Equal(3, E(rays).Length); Assert.Single(E(opposed)); Assert.Empty(E(unknown));
        AssertNoNumbers(graph.Surface); AssertNoNumbers(graph.Evaluate());
    }

    [Fact]
    public void DistinctThreeRaysWithoutOppositionAreYAndFourPairedDiagonalRaysAreX()
    {
        var f = new Fixture(); var y = f.Graph(4, (0, 1, f.U), (0, 2, S([.. E(f.D), .. E(f.L)])), (0, 3, S([.. E(f.D), .. E(f.R)])));
        Assert.Equal(1, y.Describe(y.Evaluate()).Y); Assert.Equal(0, y.Describe(y.Evaluate()).T); Assert.Equal(0, y.Describe(y.Evaluate()).Unresolved);
        var x = f.Graph(5, (0, 1, S([.. E(f.R), .. E(f.U)])), (0, 2, S([.. E(f.L), .. E(f.D)])),
            (0, 3, S([.. E(f.L), .. E(f.U)])), (0, 4, S([.. E(f.R), .. E(f.D)])));
        Assert.Equal(1, x.Describe(x.Evaluate()).X); Assert.Equal(4, x.Describe(x.Evaluate()).Endpoints); Assert.Equal(0, x.Describe(x.Evaluate()).Unresolved);
    }

    [Fact]
    public void LIsDifferentFromStraightAndFromRepeatedCoorientedParticipation()
    {
        var f = new Fixture(); var corner = f.Graph(3, (0, 1, f.R), (0, 2, f.U));
        Assert.Equal(1, corner.Describe(corner.Evaluate()).L); Assert.Equal(0, corner.Describe(corner.Evaluate()).Straight);
        var line = f.Graph(3, (0, 1, f.R), (0, 2, f.L)); Assert.Equal(1, line.Describe(line.Evaluate()).Straight); Assert.Equal(0, line.Describe(line.Evaluate()).L);
        var repeated = f.Graph(3, (0, 1, f.R), (0, 2, f.R)); var summary = repeated.Describe(repeated.Evaluate());
        Assert.Equal(0, summary.L); Assert.Equal(0, summary.Straight);
        Assert.True(repeated.TryReadJunction(Node(repeated, f.Sites[0]), out _, out _, out _, out var cooriented, out _)); Assert.Single(E(cooriented));
    }

    [Fact]
    public void IncomingStrokeExposesItsOppositeOutwardRayWithoutChangingStrokeOrder()
    {
        var f = new Fixture(); var graph = f.Graph(3, (1, 0, f.R), (0, 2, f.U));
        Assert.Equal(1, graph.Describe(graph.Evaluate()).L); Assert.True(graph.TryReadJunction(Node(graph, f.Sites[0]), out _, out var rays, out _, out _));
        var ray = Assert.IsType<Relation>(E(rays)[0]); var word = Assert.IsType<Relation>(Assert.IsType<Relation>(ray.Right).Right);
        Assert.Same(f.Frame.Left, Assert.IsType<Relation>(Assert.Single(E(word))).Left);
        var edge = Assert.IsType<Relation>(ray.Left); Assert.True(QualitativeStrokeGraph.TryReadStroke(edge, out _, out var from, out var to, out var actual));
        Assert.Same(f.Sites[1], from); Assert.Same(f.Sites[0], to); Assert.Same(f.R, actual);
    }

    [Fact]
    public void SharedProjectionDoesNotTurnTwoUnjoinedStrokesIntoAnX()
    {
        var f = new Fixture(); var location = new Placeholder(); var graph = f.Graph(4, (0, 1, f.R), (2, 3, f.U));
        var view = new Relation(graph.Surface, S(new Relation(f.Sites[0], location), new Relation(f.Sites[2], location)));
        Assert.Equal(4, graph.Describe(graph.Evaluate()).Endpoints); Assert.Equal(0, graph.Describe(graph.Evaluate()).X);
        Assert.True(Contains(view, location)); Assert.NotSame(f.Sites[0], f.Sites[2]); AssertNoNumbers(view);
    }

    [Fact]
    public void ActualClosedCycleHasOrderedEdgeWitnessAndIsNotInferredFromOneDegreeTwoSite()
    {
        var f = new Fixture(); var square = f.Graph(4, (0, 1, f.R), (1, 2, f.U), (2, 3, f.L), (3, 0, f.D));
        var summary = square.Describe(square.Evaluate()); Assert.Equal(1, summary.O); Assert.Equal(4, summary.L); Assert.Equal(0, summary.Endpoints); Assert.Equal(0, summary.Unresolved);
        Assert.True(square.TryReadResult(square.Evaluate(), out _, out var contours, out _, out _));
        var contour = Assert.IsType<Relation>(Assert.Single(E(contours))); var source = Assert.IsType<Relation>(contour.Left);
        Assert.Equal(4, E(Assert.IsType<Relation>(source.Right)).Length); Assert.True(Contains(contour, Assert.IsType<Relation>(E(square.Strokes)[0])));
        var open = f.Graph(3, (0, 1, f.R), (1, 2, f.U)); Assert.Equal(1, open.Describe(open.Evaluate()).L); Assert.Equal(0, open.Describe(open.Evaluate()).O);
    }

    [Fact]
    public void AClosedCycleWithAnAttachedBranchStillHasAnActualCycleWitness()
    {
        var f = new Fixture(); var graph = f.Graph(5, (0, 1, f.R), (1, 2, f.U), (2, 3, f.L), (3, 0, f.D), (0, 4, f.L));
        var summary = graph.Describe(graph.Evaluate()); Assert.Equal(1, summary.Contours); Assert.Equal(1, summary.T); Assert.Equal(1, summary.Endpoints); Assert.Equal(0, summary.Unresolved);
    }

    [Fact]
    public void ExplicitCylinderPeriodCertifiesWindingWithoutInventingBoundedO()
    {
        var f = new Fixture(); var period = S([.. E(f.R), .. E(f.R), .. E(f.R), .. E(f.R)]);
        var bounded = f.Graph(4, (0, 1, f.R), (1, 2, f.R), (2, 3, f.R), (3, 0, f.R));
        var graph = new QualitativeStrokeGraph(f.Frame, bounded.Sites, bounded.Strokes, period); var summary = graph.Describe(graph.Evaluate());
        Assert.Equal(0, summary.O); Assert.Equal(1, summary.Windings); Assert.Equal(0, summary.Unresolved); Assert.True(Contains(graph.Surface, period));
        Assert.True(graph.TryReadResult(graph.Evaluate(), out _, out var contours, out var winding, out _, out _)); Assert.Empty(E(contours)); Assert.Single(E(winding));
        Assert.True(Contains(Assert.Single(E(winding)), period));
        Assert.Equal(0, bounded.Describe(bounded.Evaluate()).O); Assert.True(bounded.Describe(bounded.Evaluate()).Unresolved > 0);
    }

    [Fact]
    public void UnsupportedMultipleWindingRetainsItsNonzeroCycleEvidence()
    {
        var f = new Fixture(); var twice = S([.. E(f.R), .. E(f.R)]); var shape = f.Graph(2, (0, 1, twice), (1, 0, twice));
        var graph = new QualitativeStrokeGraph(f.Frame, shape.Sites, shape.Strokes, f.R);
        Assert.Equal(0, graph.Describe(graph.Evaluate()).Windings); Assert.True(graph.Describe(graph.Evaluate()).Unresolved > 0);
    }

    [Fact]
    public void UnknownNonzeroPeriodCannotCertifyAWindingFromMatchingFormalTerms()
    {
        var f = new Fixture(); var unknownLeft = S(QualitativeMotionFrame.Move(f.Frame.Left, new Placeholder()));
        var shape = f.Graph(2, (0, 1, f.R), (1, 0, unknownLeft));
        var graph = new QualitativeStrokeGraph(f.Frame, shape.Sites, shape.Strokes, S([.. E(f.R), .. E(unknownLeft)]));
        Assert.Equal(0, graph.Describe(graph.Evaluate()).Windings); Assert.True(graph.Describe(graph.Evaluate()).Unresolved > 0);
    }

    [Fact]
    public void AnOpenKnownPrefixCannotManufactureAClosedContourOrWinding()
    {
        var f = new Fixture(); var open = StructuralConstraint.Open(E(f.R)); var graph = f.Graph(2, (0, 1, open), (0, 1, open));
        var summary = graph.Describe(graph.Evaluate()); Assert.Equal(0, summary.Contours); Assert.Equal(0, summary.Windings); Assert.True(summary.Unresolved > 0);
    }

    [Fact]
    public void TopologicalParallelEdgeCycleDoesNotClaimEnclosedArea()
    {
        var f = new Fixture(); var graph = f.Graph(2, (0, 1, f.R), (0, 1, f.R));
        var summary = graph.Describe(graph.Evaluate()); Assert.Equal(1, summary.Contours); Assert.Equal(0, summary.L); Assert.Equal(0, summary.Straight);
        // Two distinct edge participants form a multigraph cycle. A measured
        // read may find retracing; this graph has no area or bounded-face read.
        Assert.NotSame(E(graph.Strokes)[0], E(graph.Strokes)[1]); AssertNoNumbers(graph.Surface);
    }

    [Fact]
    public void AxisOppositionNeedsNoEqualLengthButUnknownDiagonalRatiosStayPending()
    {
        var f = new Fixture(); var other = new Placeholder(); var single = f.Graph(3, (0, 1, f.R), (0, 2, S(QualitativeMotionFrame.Move(f.Frame.Left, other))));
        Assert.Equal(1, single.Describe(single.Evaluate()).Straight); Assert.Equal(0, single.Describe(single.Evaluate()).Unresolved);
        var diagonal = f.Graph(3, (0, 1, S([.. E(f.R), .. E(f.U)])),
            (0, 2, S(QualitativeMotionFrame.Move(f.Frame.Left, other), QualitativeMotionFrame.Move(f.Frame.Down, new Placeholder()))));
        Assert.Equal(0, diagonal.Describe(diagonal.Evaluate()).Straight); Assert.Equal(0, diagonal.Describe(diagonal.Evaluate()).L); Assert.True(diagonal.Describe(diagonal.Evaluate()).Unresolved > 0);
    }

    [Fact]
    public void SharedRepeatedExtentsSupportUnmeasuredDiagonalRatios()
    {
        var f = new Fixture(); var rightUp = S([.. E(f.R), .. E(f.U)]);
        var twiceOpposite = S([.. E(f.L), .. E(f.L), .. E(f.D), .. E(f.D)]);
        var straight = f.Graph(3, (0, 1, rightUp), (0, 2, twiceOpposite)); Assert.Equal(1, straight.Describe(straight.Evaluate()).Straight); Assert.Equal(0, straight.Describe(straight.Evaluate()).Unresolved);
        var different = f.Graph(3, (0, 1, rightUp), (0, 2, S([.. E(f.L), .. E(f.D), .. E(f.D)])));
        Assert.Equal(1, different.Describe(different.Evaluate()).L); Assert.Equal(0, different.Describe(different.Evaluate()).Unresolved);
    }

    [Fact]
    public void ForeignDirectionsDoNotInventAnAngleAndOpenTopologyDoesNotInventEndpoints()
    {
        var f = new Fixture(); var foreign = S(QualitativeMotionFrame.Move(new Placeholder(), new Placeholder())); var graph = f.Graph(2, (0, 1, foreign));
        Assert.Equal(2, graph.Describe(graph.Evaluate()).Endpoints); Assert.True(graph.Describe(graph.Evaluate()).Unresolved > 0);
        var known = f.Graph(2, (0, 1, f.R)); var open = new QualitativeStrokeGraph(f.Frame, known.Sites, StructuralConstraint.Open(E(known.Strokes)));
        Assert.Equal(0, open.Describe(open.Evaluate()).Endpoints); Assert.True(open.Describe(open.Evaluate()).Unresolved > 0);
        var edge = QualitativeStrokeGraph.Stroke(new Placeholder(), f.Sites[0], new Placeholder(), f.R);
        var cut = new QualitativeStrokeGraph(f.Frame, S(f.Sites), S(edge)); Assert.Equal(1, cut.Describe(cut.Evaluate()).Cuts); Assert.True(cut.Describe(cut.Evaluate()).Unresolved > 0);
    }

    [Fact]
    public void UnusedDeclaredSitesRemainDistinctFromIsolatedInkComponents()
    {
        var f = new Fixture(); var graph = f.Graph(4, (0, 1, f.R)); Assert.Equal(2, graph.Describe(graph.Evaluate()).Isolated); Assert.Equal(2, graph.Describe(graph.Evaluate()).Endpoints);
    }

    [Fact]
    public void DeclaredReflectionMapsActualOrderedEdgesAndChordIdentities()
    {
        var f = new Fixture(); var source = f.Graph(2, (0, 1, f.R)); var sourceSites = f.Sites.ToArray();
        var target = f.Graph(2, (0, 1, f.L)); var targetSites = f.Sites.ToArray();
        var sites = S(sourceSites.Zip(targetSites).Select(pair => (IElement)new Relation(pair.First, pair.Second)).ToArray());
        var strokes = S(new Relation(E(source.Strokes)[0], E(target.Strokes)[0])); var chords = S(new Relation(f.R, f.L));
        var read = source.EvaluateMap(target, sites, strokes, chords); Assert.True(source.TryReadMapPassed(target, read));
        Assert.True(source.TryReadMap(read, out var matches, out var no, out var pending)); Assert.Equal(5, E(matches).Length); Assert.Empty(E(no)); Assert.Empty(E(pending));
        var reversedSites = S(new Relation(sourceSites[0], targetSites[1]), new Relation(sourceSites[1], targetSites[0]));
        var wrong = source.EvaluateMap(target, reversedSites, strokes, chords); Assert.False(source.TryReadMapPassed(target, wrong));
        Assert.True(source.TryReadMap(wrong, out _, out var failed, out _)); Assert.NotEmpty(E(failed));
    }

    [Fact]
    public void ArbitraryTToYChordRelabelingDoesNotPreserveNativeRayOpposition()
    {
        var f = new Fixture(); var source = f.Graph(4, (0, 1, f.R), (0, 2, f.L), (0, 3, f.U)); var sourceSites = f.Sites.ToArray();
        var downLeft = S([.. E(f.D), .. E(f.L)]); var downRight = S([.. E(f.D), .. E(f.R)]);
        var target = f.Graph(4, (0, 1, downLeft), (0, 2, downRight), (0, 3, f.U)); var targetSites = f.Sites.ToArray();
        var sites = S(sourceSites.Zip(targetSites).Select(pair => (IElement)new Relation(pair.First, pair.Second)).ToArray());
        var edges = S(E(source.Strokes).Zip(E(target.Strokes)).Select(pair => (IElement)new Relation(pair.First, pair.Second)).ToArray());
        var words = S(new Relation(f.R, downLeft), new Relation(f.L, downRight), new Relation(f.U, f.U));
        var read = source.EvaluateMap(target, sites, edges, words); Assert.False(source.TryReadMapPassed(target, read));
        Assert.True(source.TryReadMap(read, out _, out var failed, out var pending)); Assert.NotEmpty(E(failed)); Assert.Empty(E(pending));
    }

    [Fact]
    public void DisconnectedChordLawsAndUnknownSelfRaysAreAlsoPreservationObligations()
    {
        var f = new Fixture(); var source = f.Graph(4, (0, 1, f.R), (2, 3, f.L)); var from = f.Sites.ToArray();
        var target = f.Graph(4, (0, 1, f.R), (2, 3, f.U)); var to = f.Sites.ToArray();
        var sites = S(from.Zip(to).Select(pair => (IElement)new Relation(pair.First, pair.Second)).ToArray());
        var edges = S(E(source.Strokes).Zip(E(target.Strokes)).Select(pair => (IElement)new Relation(pair.First, pair.Second)).ToArray());
        Assert.False(source.TryReadMapPassed(target, source.EvaluateMap(target, sites, edges, S(new Relation(f.R, f.R), new Relation(f.L, f.U)))));
        var unknown = S([.. E(f.R), QualitativeMotionFrame.Move(f.Frame.Left, new Placeholder())]); var graph = f.Graph(2, (0, 1, unknown));
        var identity = S(f.Sites.Select(site => (IElement)new Relation(site, site)).ToArray());
        Assert.False(graph.TryReadMapPassed(graph, graph.EvaluateMap(graph, identity,
            S(new Relation(E(graph.Strokes)[0], E(graph.Strokes)[0])), S(new Relation(unknown, unknown)))));
    }

    [Fact]
    public void MissingOpenNonbijectiveAndForeignMapsCannotBecomeCertificates()
    {
        var f = new Fixture(); var graph = f.Graph(2, (0, 1, f.R)); var sites = S(f.Sites.Select(site => (IElement)new Relation(site, site)).ToArray());
        var strokes = S(new Relation(E(graph.Strokes)[0], E(graph.Strokes)[0])); var words = S(new Relation(f.R, f.R));
        foreach (var map in new[] { S(), StructuralConstraint.Open(E(sites)), S(new Relation(f.Sites[0], f.Sites[0]), new Relation(f.Sites[1], f.Sites[0])), S([.. E(sites), new Relation(new Placeholder(), f.Sites[0])]) })
            Assert.False(graph.TryReadMapPassed(graph, graph.EvaluateMap(graph, map, strokes, words)));
        Assert.False(graph.TryReadMapPassed(graph, graph.EvaluateMap(graph, sites, S(), words)));
        Assert.False(graph.TryReadMapPassed(graph, graph.EvaluateMap(graph, sites, strokes, S())));
    }

    [Fact]
    public void FalseReadersRejectForgedProofsAndLostActualOccurrences()
    {
        var f = new Fixture(); var graph = f.Graph(2, (0, 1, f.R)); var read = graph.Evaluate();
        Assert.True(graph.TryReadResult(new Relation(graph.Surface, read.Right), out _, out _, out _, out _));
        Assert.False(graph.TryReadResult(new Relation(graph.Surface, S()), out _, out _, out _, out _));
        var node = Node(graph, f.Sites[0]); Assert.False(graph.TryReadJunction(new Relation(node.Left, node.Right), out _, out _, out _, out _));
        var sites = S(f.Sites.Select(site => (IElement)new Relation(site, site)).ToArray()); var edges = S(new Relation(E(graph.Strokes)[0], E(graph.Strokes)[0])); var chords = S(new Relation(f.R, f.R));
        var map = graph.EvaluateMap(graph, sites, edges, chords); Assert.True(graph.TryReadMapPassed(graph, map));
        var forged = new Relation(map.Left, new Relation(S(), new Relation(S(), S()))); Assert.False(graph.TryReadMapPassed(graph, forged));
        Assert.True(graph.TryReadMap(map, out var matches, out _, out _)); var proofs = E(matches); var pair = Assert.IsType<Relation>(proofs[^1]);
        proofs[^1] = new Relation(pair.Left, new Relation(S(), S()));
        var lostPairProof = new Relation(map.Left, new Relation(S(proofs), new Relation(S(), S()))); Assert.False(graph.TryReadMapPassed(graph, lostPairProof));
        var rebuiltPair = new Relation(pair.Left, pair.Right); proofs[^1] = rebuiltPair;
        var rebuilt = new Relation(map.Left, new Relation(S(proofs), new Relation(S(), S()))); Assert.True(graph.TryReadMapPassed(graph, rebuilt));
        var same = new QualitativeStrokeGraph(f.Frame, graph.Sites, graph.Strokes); Assert.False(same.TryReadMapPassed(graph, map));
    }

    private static Relation Node(QualitativeStrokeGraph graph, IElement site)
    {
        Assert.True(graph.TryReadResult(graph.Evaluate(), out var nodes, out _, out _, out _));
        return E(nodes).Cast<Relation>().Single(node => graph.TryReadJunction(node, out var actual, out _, out _, out _) && ReferenceEquals(actual, site));
    }
    private static Relation S(params IElement[] entries) => StructuralConstraint.Closed(entries);
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
    private static bool Contains(IElement source, IElement target) => Traverse(source).Any(element => ReferenceEquals(element, target));
    private static void AssertNoNumbers(IElement source) => Assert.DoesNotContain(Traverse(source), element => element is Carrier or CarrierLocation);
    private static IEnumerable<IElement> Traverse(IElement source)
    {
        var visited = new HashSet<IElement>(ReferenceEqualityComparer.Instance); var stack = new Stack<IElement>(); stack.Push(source);
        while (stack.TryPop(out var element)) { if (!visited.Add(element)) continue; yield return element; if (element is Relation pair) { stack.Push(pair.Left); stack.Push(pair.Right); } }
    }
    private sealed class Fixture
    {
        public QualitativeMotionFrame Frame { get; } = new(); public IElement H { get; } = new Placeholder(); public IElement V { get; } = new Placeholder();
        public Relation R { get; } public Relation L { get; } public Relation U { get; } public Relation D { get; } public IElement[] Sites { get; private set; } = [];
        public Fixture() { R = S(QualitativeMotionFrame.Move(Frame.Right, H)); L = S(QualitativeMotionFrame.Move(Frame.Left, H)); U = S(QualitativeMotionFrame.Move(Frame.Up, V)); D = S(QualitativeMotionFrame.Move(Frame.Down, V)); }
        public QualitativeStrokeGraph Graph(int count, params (int From, int To, Relation Word)[] edges)
        {
            Sites = Enumerable.Range(0, count).Select(_ => (IElement)new Placeholder()).ToArray();
            return new(Frame, S(Sites), S(edges.Select(edge => (IElement)QualitativeStrokeGraph.Stroke(new Placeholder(), Sites[edge.From], Sites[edge.To], edge.Word)).ToArray()));
        }
    }
}
