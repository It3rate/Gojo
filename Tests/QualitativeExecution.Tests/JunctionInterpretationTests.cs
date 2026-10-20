using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;
using Gojo.Experiments.QualitativeFrieze;

namespace Gojo.Experiments.QualitativeExecution.Tests;

public sealed class JunctionInterpretationTests
{
    [Fact]
    public void EqualCrossSupportDoesNotEqualActualConnectivity()
    {
        var study = JunctionInterpretationStudy.Build(); var unjoined = study.Cases[0]; var joined = study.Cases[1];
        Assert.Equal(2, unjoined.Evidence.Describe().ActualComponents);
        Assert.Equal(0, unjoined.Evidence.Describe().ActualJoinedSites);
        Assert.Equal(0, unjoined.Evidence.Describe().ActualStrokePairConnections);
        Assert.Equal(1, unjoined.Evidence.Describe().StrictCrossings);
        Assert.Equal(1, joined.Evidence.Describe().ActualComponents);
        Assert.Equal(1, joined.Evidence.Describe().ActualJoinedSites);
        Assert.Equal(6, joined.Evidence.Describe().ActualStrokePairConnections);
        Assert.Equal(0, joined.Evidence.Describe().ProjectedCrossings);
        Assert.Equal(1, joined.Graph.Describe(joined.Graph.Evaluate()).X);
        Assert.Equal(0, unjoined.Graph.Describe(unjoined.Graph.Evaluate()).X);
        var positive = Endpoints(unjoined); var negative = Endpoints(joined);
        Assert.Equal(positive.Length, negative.Length);
        Assert.All(positive, endpoint => Assert.Contains(negative, actual => EqualWord(unjoined.Frame, endpoint, actual)));
    }

    [Fact]
    public void ActualTJunctionAndProjectedEndpointContactRemainDifferentReads()
    {
        var study = JunctionInterpretationStudy.Build(); var tee = study.Cases[3]; var touching = study.Cases[4];
        Assert.Equal(1, tee.Graph.Describe(tee.Graph.Evaluate()).T);
        Assert.Equal(1, tee.Evidence.Describe().ActualComponents);
        Assert.Equal(3, tee.Evidence.Describe().ActualStrokePairConnections);
        Assert.Equal(0, touching.Graph.Describe(touching.Graph.Evaluate()).T);
        Assert.Equal(2, touching.Evidence.Describe().ActualComponents);
        Assert.Equal(0, touching.Evidence.Describe().StrictCrossings);
        Assert.Equal(1, touching.Evidence.Describe().ProjectedContacts);
        touching.Evidence.TryReadResult(touching.Read, out _, out var crossings, out _, out _, out _, out _);
        var cue = Assert.IsType<Relation>(Assert.Single(E(crossings)));
        Assert.True(touching.Evidence.TryReadProjectedContact(cue, out var endpoints));
        Assert.Single(E(endpoints)); Assert.Contains(E(touching.Graph.Sites), site => ReferenceEquals(site, E(endpoints)[0]));
    }

    [Fact]
    public void BranchingYIsActualIncidenceWithoutClaimingOneOriginalObject()
    {
        var actual = JunctionInterpretationStudy.Build().Cases[2];
        Assert.Equal(1, actual.Graph.Describe(actual.Graph.Evaluate()).Y);
        Assert.Equal(1, actual.Evidence.Describe().ActualComponents);
        Assert.Equal(3, actual.Evidence.Describe().ActualStrokePairConnections);
        var identities = E(actual.Graph.Strokes).Cast<Relation>().Select(stroke => stroke.Left).ToArray();
        Assert.Equal(3, identities.Distinct(ReferenceEqualityComparer.Instance).Count());
    }

    [Fact]
    public void GapKeepsOcclusionAndIntentionalCutAsOpenNonMutatingAlternatives()
    {
        var actual = JunctionInterpretationStudy.Build().Cases[5]; var source = actual.Graph.Surface;
        var before = actual.Graph.Describe(actual.Graph.Evaluate());
        Assert.True(actual.Evidence.TryReadResult(actual.Read, out _, out _, out var gaps, out var candidates, out var blocked, out var open));
        var gap = Assert.IsType<Relation>(Assert.Single(E(gaps))); Assert.Empty(E(blocked)); Assert.Empty(E(open));
        Assert.True(actual.Evidence.TryReadGap(gap, out var endpoints, out var chord, out var proof));
        Assert.NotSame(endpoints.Left, endpoints.Right); Assert.NotEmpty(E(chord));
        Assert.Contains(Traverse(proof), element => ReferenceEquals(element, endpoints.Left));
        var bridgeCandidates = 0; var cutCandidates = 0;
        foreach (var candidate in E(candidates).Cast<Relation>())
        {
            Assert.True(actual.Evidence.TryReadCandidate(candidate, out _, out var bridges, out var cuts, out var depth, out var obligations));
            Assert.True(StructuralConstraint.TryReadScope(obligations, out _, out var seam, out var tail)); Assert.NotEqual(seam, tail);
            if (E(bridges).Length != 0)
            {
                bridgeCandidates++; var proposed = Assert.IsType<Relation>(Assert.Single(E(bridges)));
                Assert.Same(endpoints, proposed); Assert.Empty(E(cuts)); Assert.Single(E(depth));
            }
            if (E(cuts).Length != 0) { cutCandidates++; Assert.Equal(2, E(cuts).Length); Assert.Empty(E(bridges)); Assert.Empty(E(depth)); }
        }
        Assert.Equal(1, bridgeCandidates); Assert.Equal(1, cutCandidates);
        Assert.Same(source, actual.Graph.Surface); Assert.Equal(before, actual.Graph.Describe(actual.Graph.Evaluate()));
        Assert.Equal(3, actual.Evidence.Describe().ActualComponents);
    }

    [Fact]
    public void OffsetGapDoesNotGetACollinearBridgeAndAngularIntersectionStaysOpen()
    {
        var study = JunctionInterpretationStudy.Build(); var offset = study.Cases[6]; var angular = study.Cases[7];
        Assert.Equal(0, offset.Evidence.Describe().FacingGaps); Assert.True(offset.Evidence.Describe().Blocked > 0);
        Assert.Equal(0, offset.Evidence.Describe().Open);
        Assert.Equal(0, angular.Evidence.Describe().ProjectedCrossings); Assert.True(angular.Evidence.Describe().Open > 0);
        Assert.Equal(2, angular.Evidence.Describe().ActualComponents);
    }

    [Fact]
    public void DifferentUnmeasuredReachesCannotTurnUnresolvedOrderIntoNoCrossing()
    {
        var f = new Fixture(); var h = new Placeholder(); var other = new Placeholder(); var v = new Placeholder();
        var right = f.Word(f.Frame.Right, h); var otherRight = f.Word(f.Frame.Right, other);
        var down = f.Word(f.Frame.Down, v); var up = f.Word(f.Frame.Up, v);
        var graph = f.Graph([S(), S([.. E(right), .. E(right)]), S([.. E(otherRight), .. E(down)]), S([.. E(otherRight), .. E(up)])],
            [(0, 1, S([.. E(right), .. E(right)])), (2, 3, S([.. E(up), .. E(up)]))]);
        var evidence = new QualitativeJunctionEvidence(graph, f.Bindings);
        Assert.Equal(0, evidence.Describe().ProjectedCrossings); Assert.True(evidence.Describe().Open > 0);
        evidence.TryReadResult(evidence.Evaluate(), out _, out _, out _, out _, out _, out var open);
        Assert.Contains(Traverse(open), element => ReferenceEquals(element, other));
    }

    [Fact]
    public void OpenTopologyDuplicateBindingAndZeroStrokeCannotCertifyProjectedCues()
    {
        var study = JunctionInterpretationStudy.Build(); var source = study.Cases[0];
        var graph = new QualitativeStrokeGraph(source.Frame, source.Graph.Sites, StructuralConstraint.Open(E(source.Graph.Strokes)));
        foreach (var evidence in new[] {
            new QualitativeJunctionEvidence(graph, source.Bindings),
            new QualitativeJunctionEvidence(source.Graph, S([.. E(source.Bindings), E(source.Bindings)[0]])) })
        { Assert.True(evidence.Describe().Open > 0); Assert.Equal(0, evidence.Describe().ProjectedCrossings); Assert.Equal(0, evidence.Describe().FacingGaps); }
        var f = new Fixture(); var zero = f.Graph([S(), S()], [(0, 1, S())]);
        Assert.True(new QualitativeJunctionEvidence(zero, f.Bindings).Describe().Open > 0);
    }

    [Fact]
    public void IncompatiblePositionPromiseIsBlockedBeforeItSuppliesAGap()
    {
        var source = JunctionInterpretationStudy.Build().Cases[5]; var entries = E(source.Bindings);
        var original = (Relation)entries[0]; entries[0] = new Relation(original.Left, S());
        var evidence = new QualitativeJunctionEvidence(source.Graph, S(entries));
        Assert.True(evidence.Describe().Blocked > 0); Assert.Equal(0, evidence.Describe().FacingGaps);
    }

    [Fact]
    public void ReadersRejectForgedOccurrencesAndAllStudySurfacesStayPreNumeric()
    {
        var study = JunctionInterpretationStudy.Build(); Assert.Equal(8, study.Cases.Count);
        Assert.All(study.Cases.Take(7), actual => Assert.Equal(0, actual.Evidence.Describe().Open));
        Assert.DoesNotContain(Traverse(study.Surface), element => element is Carrier or CarrierLocation);
        var source = study.Cases[5]; var evidence = source.Evidence;
        Assert.False(evidence.TryReadResult(new Relation(evidence.Surface, S()), out _, out _, out _, out _, out _, out _));
        evidence.TryReadResult(source.Read, out _, out _, out var gaps, out var candidates, out _, out _);
        var gap = (Relation)Assert.Single(E(gaps)); var candidate = (Relation)E(candidates)[0];
        Assert.False(evidence.TryReadGap(new Relation(gap.Left, gap.Right), out _, out _, out _));
        Assert.False(evidence.TryReadCandidate(new Relation(candidate.Left, candidate.Right), out _, out _, out _, out _, out _));
        Assert.Throws<ArgumentException>(() => new QualitativeJunctionEvidence(source.Graph, S(new Relation(new Placeholder(), new Carrier()))));
    }

    private static Relation[] Endpoints(JunctionInterpretationStudy.Case actual)
    {
        actual.Graph.TryReadResult(actual.Graph.Evaluate(), out var nodes, out _, out _, out _);
        return E(nodes).Cast<Relation>().Where(node => actual.Graph.TryReadJunction(node, out _, out var rays, out _, out _) && E(rays).Length == 1)
            .Select(node => { actual.Graph.TryReadJunction(node, out var site, out _, out _, out _); return (Relation)E(actual.Bindings).Cast<Relation>().Single(binding => ReferenceEquals(binding.Left, site)).Right; }).ToArray();
    }
    private static bool EqualWord(QualitativeMotionFrame frame, Relation a, Relation b)
    {
        var inverse = S(E(b).Reverse().Cast<Relation>().Select(move => (IElement)QualitativeMotionFrame.Move(
            ((Relation)E(frame.Opposites).Single(pair => pair is Relation relation && ReferenceEquals(relation.Left, move.Left))).Right, move.Right)).ToArray());
        return frame.TryReadResult(frame.Reduce(a, inverse), out var rest, out _, out var open) && E(rest).Length == 0 && E(open).Length == 0;
    }
    private sealed class Fixture
    {
        public QualitativeMotionFrame Frame { get; } = new();
        public Relation Bindings { get; private set; } = S();
        public Relation Word(IElement direction, IElement reach) => S(QualitativeMotionFrame.Move(direction, reach));
        public QualitativeStrokeGraph Graph(Relation[] positions, (int From, int To, Relation Chord)[] edges)
        {
            var sites = positions.Select(_ => (IElement)new Placeholder()).ToArray();
            Bindings = S(positions.Select((word, i) => (IElement)new Relation(sites[i], word)).ToArray());
            return new(Frame, S(sites), S(edges.Select(edge => (IElement)QualitativeStrokeGraph.Stroke(new Placeholder(), sites[edge.From], sites[edge.To], edge.Chord)).ToArray()));
        }
    }
    private static Relation S(params IElement[] entries) => StructuralConstraint.Closed(entries);
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
    private static IEnumerable<IElement> Traverse(IElement source)
    {
        var seen = new HashSet<IElement>(ReferenceEqualityComparer.Instance); var pending = new Stack<IElement>(); pending.Push(source);
        while (pending.TryPop(out var element))
        { if (!seen.Add(element)) continue; yield return element; if (element is Relation relation) { pending.Push(relation.Left); pending.Push(relation.Right); } }
    }
}
