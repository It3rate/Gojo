using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;
using Gojo.Experiments.QualitativeFrieze;

namespace Gojo.Experiments.QualitativeExecution.Tests;

public sealed class QualitativeRecognitionObservationTests
{
    [Fact]
    public void PublicationRetainsTopologyAndCoarseHeadingsWithoutSourceHistoryOrExtents()
    {
        var f = new Fixture(); var a = f.Site(); var b = f.Site(f.R(), f.R()); var c = f.Site(f.R(), f.U());
        var ab = f.Edge(a, b); var ac = f.Edge(a, c); var secretFace = new Relation(new Placeholder(), f.Frame.Surface);
        var publication = f.Publish(faces: S(secretFace)); var o = publication.Observation;
        Assert.True(publication.IsComplete); Assert.Equal(3, E(o.Sites).Length); Assert.Equal(2, E(o.Segments).Length);
        Assert.True(publication.TryMapSegment(ab.Left, out var first));
        Assert.True(o.TryReadSegment(first, out _, out var from, out var to, out var heading));
        Assert.Same(o.Frame.Right, Assert.Single(E(heading))); Assert.NotSame(a, from); Assert.NotSame(b, to);
        Assert.True(publication.TryMapSegment(ac.Left, out var diagonal));
        Assert.True(o.TryReadSegment(diagonal, out _, out _, out _, out var diagonalHeading)); Assert.Equal(2, E(diagonalHeading).Length);
        var exposed = Reach(o.Surface);
        Assert.DoesNotContain(exposed, item => item is Carrier or CarrierLocation);
        foreach (var hidden in new IElement[] { a, b, c, ab, ac, f.Reach, f.Frame.Surface, f.Frame.Right, secretFace })
            Assert.DoesNotContain(exposed, item => ReferenceEquals(hidden, item));
        Assert.Contains(Reach(publication.Surface), item => ReferenceEquals(item, f.Reach));
    }

    [Fact]
    public void SourceFaceScopeIsNotARecognitionInputEvenWhenItIsOpen()
    {
        var f = new Fixture(); var a = f.Site(); var b = f.Site(f.R()); f.Edge(a, b);
        var sourceOnly = new Placeholder(); var publication = f.Publish(faces: StructuralConstraint.Open(sourceOnly));
        Assert.True(publication.IsComplete);
        Assert.DoesNotContain(Reach(publication.Observation.Surface), item => ReferenceEquals(item, sourceOnly));
    }

    [Fact]
    public void UnknownWordKeepsTheActualEdgeAndAnOpenHeading()
    {
        var f = new Fixture(); var a = f.Site(); var b = f.Site(f.R()); var edge = f.Edge(a, b);
        f.Bindings[1] = new Relation(b, StructuralConstraint.Open(f.R()));
        var p = f.Publish(); var o = p.Observation;
        Assert.False(p.IsComplete); Assert.Single(E(o.Segments)); Assert.NotEmpty(E(p.Unresolved));
        Assert.True(p.TryMapSegment(edge.Left, out var observed)); Assert.True(o.TryReadSegment(observed, out _, out _, out _, out var heading));
        Assert.False(Closed(heading)); Assert.Empty(E(heading)); Assert.Empty(E(o.Continuations));
    }

    [Fact]
    public void IncomparableOpposingExtentsDoNotInventANetAxisDirection()
    {
        var f = new Fixture(); var a = f.Site(); var otherReach = new Placeholder();
        var b = f.Site(f.R(), QualitativeMotionFrame.Move(f.Frame.Left, otherReach), f.U()); f.Edge(a, b);
        var p = f.Publish(); var o = p.Observation;
        Assert.False(p.IsComplete); var segment = Assert.IsType<Relation>(Assert.Single(E(o.Segments)));
        Assert.True(o.TryReadSegment(segment, out _, out _, out _, out var heading));
        Assert.False(Closed(heading)); Assert.Same(o.Frame.Up, Assert.Single(E(heading)));
    }

    [Fact]
    public void CoarselyOppositeDiagonalsDoNotSupplyStraightContinuation()
    {
        var f = new Fixture(); var origin = f.Site(); var a = f.Site(f.R(), f.U());
        var b = f.Site(f.L(), f.L(), f.D()); f.Edge(origin, a); f.Edge(origin, b);
        var p = f.Publish(); Assert.True(p.IsComplete); Assert.Empty(E(p.Observation.Continuations));
        var headings = E(p.Observation.Segments).Cast<Relation>().Select(row => ((Relation)((Relation)row.Right).Right)).ToArray();
        Assert.All(headings, heading => Assert.Equal(2, E(heading).Length));
    }

    [Fact]
    public void ActualOppositeSymbolicProfilesCanProveStraightContinuationAtUnequalLengths()
    {
        var f = new Fixture(); var origin = f.Site(); var a = f.Site(f.R(), f.U());
        var b = f.Site(f.L(), f.L(), f.D(), f.D()); var first = f.Edge(origin, a); var second = f.Edge(origin, b);
        var p = f.Publish(); var o = p.Observation; Assert.True(p.IsComplete);
        var row = Assert.IsType<Relation>(Assert.Single(E(o.Continuations)));
        Assert.True(o.TryReadContinuation(row, out var site, out var x, out var y));
        Assert.True(p.TryMapSite(origin, out var mappedSite)); Assert.Same(mappedSite, site);
        Assert.True(p.TryMapSegment(first.Left, out var mappedFirst)); Assert.True(p.TryMapSegment(second.Left, out var mappedSecond));
        Assert.Same(mappedFirst, x); Assert.Same(mappedSecond, y);
    }

    [Fact]
    public void AxisOnlyContinuationDoesNotRequireEqualReachIdentity()
    {
        var f = new Fixture(); var origin = f.Site(); var a = f.Site(f.R());
        var b = f.Site(QualitativeMotionFrame.Move(f.Frame.Left, new Placeholder())); f.Edge(origin, a); f.Edge(b, origin);
        var p = f.Publish(); Assert.True(p.IsComplete); Assert.Single(E(p.Observation.Continuations));
    }

    [Fact]
    public void DistinctDiagonalReachProfilesCannotBeSilentlyIdentified()
    {
        var f = new Fixture(); var origin = f.Site(); var a = f.Site(f.R(), f.U()); var other = new Placeholder();
        var b = f.Site(QualitativeMotionFrame.Move(f.Frame.Left, other), QualitativeMotionFrame.Move(f.Frame.Down, other));
        f.Edge(origin, a); f.Edge(origin, b); var p = f.Publish();
        Assert.True(p.IsComplete); Assert.Empty(E(p.Observation.Continuations));
    }

    [Fact]
    public void DeclaredCrossingIsAnObservationWithoutCreatingASharedJoinSite()
    {
        var f = new Fixture(); var left = f.Site(f.L()); var right = f.Site(f.R()); var down = f.Site(f.D()); var up = f.Site(f.U());
        var horizontal = f.Edge(left, right); var vertical = f.Edge(down, up);
        var p = f.Publish(crossings: S(new Relation(horizontal.Left, vertical.Left))); var o = p.Observation;
        Assert.True(p.IsComplete); Assert.Equal(4, E(o.Sites).Length); Assert.Empty(E(o.Continuations));
        var row = Assert.IsType<Relation>(Assert.Single(E(o.Crossings))); Assert.True(o.TryReadCrossing(row, out var a, out var b));
        Assert.True(o.TryReadSegment(a, out _, out var af, out var at, out _)); Assert.True(o.TryReadSegment(b, out _, out var bf, out var bt, out _));
        Assert.NotSame(af, bf); Assert.NotSame(af, bt); Assert.NotSame(at, bf); Assert.NotSame(at, bt);
    }

    [Fact]
    public void JoinedXHasAnActualSharedRoleAndRejectsAnOverpassClaim()
    {
        var f = new Fixture(); var centre = f.Site(); var left = f.Site(f.L()); var right = f.Site(f.R()); var down = f.Site(f.D()); var up = f.Site(f.U());
        var a = f.Edge(left, centre); var b = f.Edge(centre, right); f.Edge(down, centre); f.Edge(centre, up);
        var p = f.Publish(crossings: S(new Relation(a.Left, b.Left)));
        Assert.Empty(E(p.Observation.Crossings)); Assert.Single(E(p.Blocked)); Assert.Equal(2, E(p.Observation.Continuations).Length);
        Assert.True(p.TryMapSite(centre, out var shared));
        Assert.All(E(p.Observation.Continuations).Cast<Relation>(), row => Assert.Same(shared, row.Left));
    }

    [Fact]
    public void ForeignCrossingCannotPublishAnOccurrenceAndItsFailureStaysExternal()
    {
        var f = new Fixture(); var a = f.Site(); var b = f.Site(f.R()); var edge = f.Edge(a, b); var foreign = new Placeholder();
        var p = f.Publish(crossings: S(new Relation(edge.Left, foreign)));
        Assert.False(p.IsComplete); Assert.Single(E(p.Blocked)); Assert.Empty(E(p.Observation.Crossings));
        Assert.True(Closed(p.Observation.Segments)); Assert.True(p.Observation.IsComplete);
        Assert.DoesNotContain(Reach(p.Observation.Surface), item => ReferenceEquals(item, foreign));
    }

    [Fact]
    public void InvalidTopologyStillOpensSegmentInventorySeparatelyFromCrossingClaims()
    {
        var f = new Fixture(); var a = f.Site(); var b = f.Site(f.R()); var edge = f.Edge(a, b);
        f.Edges.Add(new Relation(new Placeholder(), new Relation(a, new Placeholder())));
        var p = f.Publish(crossings: S(new Relation(edge.Left, new Placeholder())));
        Assert.Equal(2, E(p.Blocked).Length); Assert.Single(E(p.Observation.Segments));
        Assert.False(Closed(p.Observation.Segments)); Assert.False(p.Observation.IsComplete);
    }

    [Fact]
    public void ReorderedSourceWithRenamedSitesHasTheSameQualitativeLocalSignature()
    {
        var first = LineAndCorner(reverse: false); var second = LineAndCorner(reverse: true);
        Assert.Equal(Signature(first.Observation), Signature(second.Observation));
        Assert.DoesNotContain(Reach(first.Observation.Surface), item => Reach(second.Observation.Surface).Contains(item));
    }

    [Fact]
    public void OpenSourceScopesPreserveKnownPrefixWithoutClaimingExhaustiveObservation()
    {
        var f = new Fixture(); var a = f.Site(); var b = f.Site(f.R()); f.Edge(a, b);
        var topology = new Relation(StructuralConstraint.Open(f.Sites.ToArray()), new Relation(StructuralConstraint.Open(f.Edges.ToArray()), S()));
        var p = new QualitativeRecognitionPublisher(f.Frame, topology, S(f.Bindings.ToArray()));
        Assert.False(p.IsComplete); Assert.Equal(2, E(p.Observation.Sites).Length); Assert.Single(E(p.Observation.Segments));
        Assert.False(Closed(p.Observation.Sites)); Assert.False(Closed(p.Observation.Segments));
    }

    [Fact]
    public void NativeFixtureRequiresActualRowsAndCannotReadAForeignFrameDirection()
    {
        var frame = new QualitativeMotionFrame(); var a = new Placeholder(); var b = new Placeholder();
        var row = new Relation(new Placeholder(), new Relation(new Relation(a, b), S(frame.Right)));
        var o = new QualitativeRecognitionObservation(frame, S(a, b), S(row)); Assert.True(o.IsComplete);
        Assert.True(o.TryReadSegment(row, out _, out _, out _, out _));
        Assert.False(o.TryReadSegment(new Relation(row.Left, row.Right), out _, out _, out _, out _));
        var foreign = new QualitativeMotionFrame(); var bad = new Relation(new Placeholder(), new Relation(new Relation(a, b), S(foreign.Right)));
        var invalid = new QualitativeRecognitionObservation(frame, S(a, b), S(bad)); Assert.False(invalid.IsComplete);
        Assert.False(invalid.TryReadSegment(bad, out _, out _, out _, out _));
    }

    private static QualitativeRecognitionPublisher LineAndCorner(bool reverse)
    {
        var f = new Fixture(); var a = f.Site(f.L()); var b = f.Site(); var c = f.Site(f.R()); var d = f.Site(f.R(), f.U());
        f.Edge(a, b); f.Edge(b, c); f.Edge(c, d);
        if (reverse) { f.Sites.Reverse(); f.Edges.Reverse(); f.Bindings.Reverse(); }
        return f.Publish();
    }
    private static string Signature(QualitativeRecognitionObservation o)
    {
        string D(IElement dir) => ReferenceEquals(dir, o.Frame.Right) ? "R" : ReferenceEquals(dir, o.Frame.Left) ? "L" : ReferenceEquals(dir, o.Frame.Up) ? "U" : "D";
        return string.Join(";", E(o.Sites).Select(site => string.Join(",", E(o.Segments).Cast<Relation>().Where(row =>
            o.TryReadSegment(row, out _, out var from, out var to, out _) && (ReferenceEquals(from, site) || ReferenceEquals(to, site)))
            .Select(row => { o.TryReadSegment(row, out _, out var from, out _, out var heading); return string.Join("", E(heading).Select(dir =>
                D(ReferenceEquals(from, site) ? dir : E(o.Frame.Opposites).Cast<Relation>().Single(pair => ReferenceEquals(pair.Left, dir)).Right)).Order()); }).Order())).Order());
    }
    private sealed class Fixture
    {
        public QualitativeMotionFrame Frame { get; } = new();
        public IElement Reach { get; } = new Placeholder();
        public List<IElement> Sites { get; } = [];
        public List<IElement> Edges { get; } = [];
        public List<IElement> Bindings { get; } = [];
        public Relation R() => QualitativeMotionFrame.Move(Frame.Right, Reach);
        public Relation L() => QualitativeMotionFrame.Move(Frame.Left, Reach);
        public Relation U() => QualitativeMotionFrame.Move(Frame.Up, Reach);
        public Relation D() => QualitativeMotionFrame.Move(Frame.Down, Reach);
        public IElement Site(params IElement[] moves) { var site = new Placeholder(); Sites.Add(site); Bindings.Add(new Relation(site, S(moves))); return site; }
        public Relation Edge(IElement from, IElement to) { var edge = new Relation(new Placeholder(), new Relation(from, to)); Edges.Add(edge); return edge; }
        public QualitativeRecognitionPublisher Publish(Relation? faces = null, Relation? crossings = null) => new(Frame,
            new Relation(S(Sites.ToArray()), new Relation(S(Edges.ToArray()), faces ?? S())), S(Bindings.ToArray()), crossings);
    }
    private static Relation S(params IElement[] entries) => StructuralConstraint.Closed(entries);
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
    private static bool Closed(Relation scope) => StructuralConstraint.TryReadScope(scope, out _, out var seam, out var tail) && seam.Equals(tail);
    private static HashSet<IElement> Reach(IElement source)
    {
        var seen = new HashSet<IElement>(ReferenceEqualityComparer.Instance); var work = new Stack<IElement>(); work.Push(source);
        while (work.TryPop(out var item)) if (seen.Add(item) && item is Relation row) { work.Push(row.Left); work.Push(row.Right); }
        return seen;
    }
}
