using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;
using Gojo.Experiments.QualitativeFrieze;

namespace Gojo.Experiments.QualitativeExecution.Tests;

public sealed class QualitativeRecognitionPlacementTests
{
    [Fact]
    public void OrdinalPublicationKeepsOnlyObservedRolesAndProjectionEqualityDoesNotJoinThem()
    {
        var f = new Fixture(); var supplier = f.Publish(); var placement = supplier.Placement;
        Assert.True(placement.IsComplete);
        var a = f.Observed(f.OuterSites[0]); var b = f.Observed(f.OuterSites[3]);
        var comparison = placement.Compare(placement.Observation.Frame.Horizontal, a, b);
        Assert.True(placement.TryReadComparison(comparison, out var before, out var equal, out var after, out var unresolved));
        Assert.Empty(E(before)); Assert.Single(E(equal)); Assert.Empty(E(after)); Assert.Empty(E(unresolved)); Assert.NotSame(a, b);
        Assert.Equal(8, E(placement.Observation.Sites).Length); Assert.Equal(8, E(placement.Observation.Segments).Length);
        var exposed = Reach(placement.Surface);
        foreach (var hidden in f.Sites.Cast<IElement>().Append(f.Reach).Append(f.Frame.Surface))
            Assert.DoesNotContain(exposed, item => ReferenceEquals(hidden, item));
        Assert.DoesNotContain(exposed, item => item is Carrier or CarrierLocation);
        Assert.Contains(Reach(supplier.Surface), item => ReferenceEquals(item, f.Reach));
    }

    [Fact]
    public void StrictRectangleContainmentRequiresAllActualPassageEnvelopesAndRetainsEachProof()
    {
        var f = new Fixture(); var placement = f.Publish().Placement;
        var read = placement.ReadContainment(f.ObservedOuter, f.ObservedInner);
        Assert.True(read.IsStrictlyInside); Assert.Single(E(read.Inside)); Assert.Equal(4, E(read.InteriorSites).Length);
        Assert.Empty(E(read.TouchingSites)); Assert.Empty(E(read.OutsideSites)); Assert.Empty(E(read.Blocked)); Assert.Empty(E(read.Unresolved));
        Assert.Contains(Reach(read.Surface), item => ReferenceEquals(item, placement.Surface));
        foreach (var edge in E(f.ObservedInner).Concat(E(f.ObservedOuter)))
            Assert.Contains(E(read.Evidence), item => item is Relation row && ReferenceEquals(row.Left, edge));
    }

    [Fact]
    public void EndpointPositionsDoNotAuthorizeABowedPassageSupportClaim()
    {
        var f = new Fixture(); var supplier = f.Publish(includeEnvelopes: false); var placement = supplier.Placement;
        Assert.Empty(E(placement.Envelopes)); Assert.True(placement.IsComplete);
        var read = placement.ReadContainment(f.ObservedOuter, f.ObservedInner);
        Assert.False(read.IsStrictlyInside); Assert.Empty(E(read.Inside)); Assert.Equal(4, E(read.InteriorSites).Length);
        Assert.Equal(8, E(read.Unresolved).Length); Assert.Empty(E(read.Blocked));
        // A path could bow outside its endpoints' box. The supplier has given
        // no envelope premise that excludes that possibility.
    }

    [Fact]
    public void WithholdingOneEnvelopeLeavesKnownEndpointClassificationsAndOneOpenSupportObligation()
    {
        var f = new Fixture(); var supplier = f.Publish(); var p = supplier.Placement;
        var removed = E(p.Envelopes).Last();
        var partial = new QualitativeRecognitionPlacement(p.Observation, p.HorizontalBuckets, p.VerticalBuckets,
            S(E(p.Envelopes).Where(row => !ReferenceEquals(row, removed)).ToArray()));
        var read = partial.ReadContainment(f.ObservedOuter, f.ObservedInner);
        Assert.False(read.IsStrictlyInside); Assert.Equal(4, E(read.InteriorSites).Length); Assert.Single(E(read.Unresolved)); Assert.Empty(E(read.Inside));
    }

    [Fact]
    public void ProjectionBoundaryTouchIsDistinctFromStrictInteriorAndSharedSiteIdentity()
    {
        var f = new Fixture(innerShiftX: -2); var p = f.Publish().Placement;
        var read = p.ReadContainment(f.ObservedOuter, f.ObservedInner);
        Assert.False(read.IsStrictlyInside); Assert.Single(E(read.TouchingSites)); Assert.Equal(3, E(read.InteriorSites).Length);
        Assert.Empty(E(read.OutsideSites)); Assert.Empty(E(read.Unresolved)); Assert.Empty(E(read.Inside));
        var touching = Assert.IsType<Relation>(Assert.Single(E(read.TouchingSites))).Left;
        Assert.DoesNotContain(E(f.ObservedOuter).Cast<Relation>(), row =>
            p.Observation.TryReadSegment(row, out _, out var a, out var b, out _) && (ReferenceEquals(a, touching) || ReferenceEquals(b, touching)));
    }

    [Fact]
    public void AStraddlingMotifRetainsKnownOutsideAndTouchingVerticesWithoutAStrictClaim()
    {
        var f = new Fixture(innerShiftX: 4); var p = f.Publish().Placement;
        var read = p.ReadContainment(f.ObservedOuter, f.ObservedInner);
        Assert.False(read.IsStrictlyInside); Assert.Single(E(read.OutsideSites)); Assert.Equal(2, E(read.TouchingSites).Length);
        Assert.Single(E(read.InteriorSites)); Assert.Empty(E(read.Inside)); Assert.Empty(E(read.Unresolved));
    }

    [Fact]
    public void MissingAxisOrderIsUnknownRatherThanOutsideOrFalseProjectionEquality()
    {
        var f = new Fixture(); var p = f.Publish().Placement;
        var missing = new QualitativeRecognitionPlacement(p.Observation, p.HorizontalBuckets, StructuralConstraint.Open(), p.Envelopes);
        var read = missing.ReadContainment(f.ObservedOuter, f.ObservedInner);
        Assert.False(read.IsStrictlyInside); Assert.NotEmpty(E(read.Unresolved)); Assert.Empty(E(read.OutsideSites)); Assert.Empty(E(read.Blocked));
        var comparison = missing.Compare(p.Observation.Frame.Vertical, f.Observed(f.InnerSites[0]), f.Observed(f.OuterSites[0]));
        Assert.True(missing.TryReadComparison(comparison, out var before, out var equal, out var after, out var unknown));
        Assert.Empty(E(before)); Assert.Empty(E(equal)); Assert.Empty(E(after)); Assert.Single(E(unknown));
    }

    [Fact]
    public void OpenBucketAndDuplicateProjectionMembershipCannotSettleNecessaryOrderFacts()
    {
        var f = new Fixture(); var p = f.Publish().Placement; var buckets = E(p.HorizontalBuckets);
        var first = Assert.IsType<Relation>(buckets[0]);
        var open = S([StructuralConstraint.Open(E(first)), .. buckets.Skip(1)]);
        var duplicate = S(buckets.Append(S(E(first)[0])).ToArray());
        foreach (var axis in new[] { open, duplicate })
        {
            var revised = new QualitativeRecognitionPlacement(p.Observation, axis, p.VerticalBuckets, p.Envelopes);
            var read = revised.ReadContainment(f.ObservedOuter, f.ObservedInner);
            Assert.False(read.IsStrictlyInside); Assert.NotEmpty(E(read.Unresolved)); Assert.Empty(E(read.Inside)); Assert.Empty(E(read.OutsideSites));
        }
    }

    [Fact]
    public void AxisOrderContradictingKnownSegmentHeadingIsBlockedAndCannotSupplyContainment()
    {
        var f = new Fixture(); var p = f.Publish().Placement;
        var contradictory = new QualitativeRecognitionPlacement(p.Observation, S(E(p.HorizontalBuckets).Reverse().ToArray()), p.VerticalBuckets, p.Envelopes);
        Assert.NotEmpty(E(contradictory.Blocked));
        var read = contradictory.ReadContainment(f.ObservedOuter, f.ObservedInner);
        Assert.False(read.IsStrictlyInside); Assert.NotEmpty(E(read.Unresolved)); Assert.Empty(E(read.Inside));
    }

    [Fact]
    public void ASlantedFourCycleIsNotSilentlyPromotedToAnAxisAlignedRectangle()
    {
        var f = new Fixture(shearOuter: true); var p = f.Publish().Placement;
        var read = p.ReadContainment(f.ObservedOuter, f.ObservedInner);
        Assert.False(read.IsStrictlyInside); Assert.NotEmpty(E(read.Blocked)); Assert.Empty(E(read.Inside));
    }

    [Fact]
    public void CopiedSegmentRowsAndForeignEnvelopeBoundsCannotProduceActualSupport()
    {
        var f = new Fixture(); var p = f.Publish().Placement; var actual = Assert.IsType<Relation>(E(f.ObservedInner)[0]);
        var copy = new Relation(actual.Left, actual.Right);
        var read = p.ReadContainment(f.ObservedOuter, S([copy, .. E(f.ObservedInner).Skip(1)]));
        Assert.False(read.IsStrictlyInside); Assert.NotEmpty(E(read.Blocked));
        var foreign = new Relation(actual, new Relation(new Relation(p.Observation.Frame.Horizontal, new Relation(new Placeholder(), new Placeholder())),
            new Relation(p.Observation.Frame.Vertical, new Relation(new Placeholder(), new Placeholder()))));
        var bad = new QualitativeRecognitionPlacement(p.Observation, p.HorizontalBuckets, p.VerticalBuckets,
            S(E(p.Envelopes).Where(row => row is Relation entry && !ReferenceEquals(entry.Left, actual)).Append(foreign).ToArray()));
        Assert.NotEmpty(E(bad.Blocked)); Assert.False(bad.ReadContainment(f.ObservedOuter, f.ObservedInner).IsStrictlyInside);
    }

    [Fact]
    public void TheOrdinalComparatorChecksActualBucketEvidenceRatherThanAcceptingASwappedResult()
    {
        var f = new Fixture(); var p = f.Publish().Placement;
        var a = f.Observed(f.OuterSites[0]); var b = f.Observed(f.OuterSites[1]);
        var read = p.Compare(p.Observation.Frame.Horizontal, a, b);
        Assert.True(p.TryReadComparison(read, out var before, out var equal, out var after, out var unknown)); Assert.Single(E(before));
        var forged = new Relation(read.Left, new Relation(after, new Relation(equal, new Relation(before, unknown))));
        Assert.False(p.TryReadComparison(forged, out _, out _, out _, out _));
        var reversed = p.Compare(p.Observation.Frame.Horizontal, b, a);
        Assert.True(p.TryReadComparison(reversed, out var reverseBefore, out _, out var reverseAfter, out _));
        Assert.Empty(E(reverseBefore)); Assert.Single(E(reverseAfter));
    }

    [Fact]
    public void TwoActualRowsSharingOnePassageIdentityCannotEstablishStrictContainment()
    {
        var f = new Fixture(); var p = f.Publish().Placement;
        var actual = Assert.IsType<Relation>(E(f.ObservedInner)[0]);
        var alias = new Relation(actual.Left.Dual().Dual(), actual.Right);
        var revisedObservation = new QualitativeRecognitionObservation(p.Observation.Frame, p.Observation.Sites,
            S([.. E(p.Observation.Segments), alias]), p.Observation.Continuations, p.Observation.Crossings);
        var actualEnvelope = E(p.Envelopes).Cast<Relation>().Single(row => ReferenceEquals(row.Left, actual));
        var revised = new QualitativeRecognitionPlacement(revisedObservation, p.HorizontalBuckets, p.VerticalBuckets,
            S([.. E(p.Envelopes), new Relation(alias, actualEnvelope.Right)]));
        Assert.False(revisedObservation.IsComplete); Assert.False(revised.IsComplete); Assert.NotEmpty(E(revised.Unresolved));
        var read = revised.ReadContainment(f.ObservedOuter, f.ObservedInner);
        Assert.False(read.IsStrictlyInside); Assert.Empty(E(read.Inside)); Assert.NotEmpty(E(read.Unresolved));
        Assert.Contains(Reach(read.Surface), item => ReferenceEquals(item, alias));
    }

    [Fact]
    public void DuplicateActualSiteAliasesKeepTheirDependentPlacementUnresolved()
    {
        var f = new Fixture(); var p = f.Publish().Placement; var site = f.Observed(f.InnerSites[0]);
        var revisedObservation = new QualitativeRecognitionObservation(p.Observation.Frame,
            S([.. E(p.Observation.Sites), site.Dual().Dual()]), p.Observation.Segments, p.Observation.Continuations, p.Observation.Crossings);
        var revised = new QualitativeRecognitionPlacement(revisedObservation, p.HorizontalBuckets, p.VerticalBuckets, p.Envelopes);
        Assert.False(revisedObservation.IsComplete); Assert.False(revised.IsComplete);
        var read = revised.ReadContainment(f.ObservedOuter, f.ObservedInner);
        Assert.False(read.IsStrictlyInside); Assert.NotEmpty(E(read.Unresolved)); Assert.Empty(E(read.Inside));
    }

    [Fact]
    public void AnUnrelatedAmbiguousPassageDoesNotEraseKnownLocalContainment()
    {
        var f = new Fixture(); var p = f.Publish().Placement; var template = Assert.IsType<Relation>(E(f.ObservedOuter)[0]);
        var identity = new Placeholder(); var extra = new Relation(identity, template.Right);
        var alias = new Relation(identity.Dual().Dual(), template.Right);
        var revisedObservation = new QualitativeRecognitionObservation(p.Observation.Frame, p.Observation.Sites,
            S([.. E(p.Observation.Segments), extra, alias]), p.Observation.Continuations, p.Observation.Crossings);
        var revised = new QualitativeRecognitionPlacement(revisedObservation, p.HorizontalBuckets, p.VerticalBuckets, p.Envelopes);
        Assert.False(revisedObservation.IsComplete); Assert.False(revised.IsComplete); Assert.NotEmpty(E(revised.Unresolved));
        var read = revised.ReadContainment(f.ObservedOuter, f.ObservedInner);
        Assert.True(read.IsStrictlyInside); Assert.Empty(E(read.Unresolved));
    }

    private sealed class Fixture
    {
        public QualitativeMotionFrame Frame { get; } = new();
        public IElement Reach { get; } = new Placeholder();
        public List<IElement> Sites { get; } = [];
        public List<Relation> Edges { get; } = [];
        public List<IElement> Bindings { get; } = [];
        public IElement[] OuterSites { get; }
        public IElement[] InnerSites { get; }
        public Relation[] OuterEdges { get; }
        public Relation[] InnerEdges { get; }
        public QualitativeRecognitionPublisher ObservationPublisher { get; }
        public Relation ObservedOuter => Map(OuterEdges);
        public Relation ObservedInner => Map(InnerEdges);
        public Fixture(int innerShiftX = 0, bool shearOuter = false)
        {
            OuterSites = [Site(0, 0), Site(8, 0), Site(shearOuter ? 10 : 8, 8), Site(shearOuter ? 2 : 0, 8)];
            InnerSites = [Site(2 + innerShiftX, 4), Site(4 + innerShiftX, 6), Site(6 + innerShiftX, 4), Site(4 + innerShiftX, 2)];
            OuterEdges = Cycle(OuterSites); InnerEdges = Cycle(InnerSites);
            ObservationPublisher = new(Frame, new Relation(S(Sites.ToArray()), new Relation(S(Edges.ToArray()), S())), S(Bindings.ToArray()));
        }
        private IElement Site(int x, int y)
        {
            var site = new Placeholder(); Sites.Add(site); var word = new List<IElement>();
            for (var i = 0; i < Math.Abs(x); i++) word.Add(QualitativeMotionFrame.Move(x < 0 ? Frame.Left : Frame.Right, Reach));
            for (var i = 0; i < Math.Abs(y); i++) word.Add(QualitativeMotionFrame.Move(y < 0 ? Frame.Down : Frame.Up, Reach));
            Bindings.Add(new Relation(site, S(word.ToArray()))); return site;
        }
        private Relation[] Cycle(IElement[] sites) => sites.Select((from, index) =>
        { var edge = new Relation(new Placeholder(), new Relation(from, sites[(index + 1) % sites.Length])); Edges.Add(edge); return edge; }).ToArray();
        public IElement Observed(IElement source) { Assert.True(ObservationPublisher.TryMapSite(source, out var observed)); return observed; }
        private Relation Map(Relation[] source) => S(source.Select(edge =>
        { Assert.True(ObservationPublisher.TryMapSegment(edge.Left, out var observed)); return (IElement)observed; }).ToArray());
        public QualitativeRecognitionPlacementPublisher Publish(bool includeEnvelopes = true) => new(Frame, S(Bindings.ToArray()), ObservationPublisher,
            includeEnvelopes ? S(Edges.Select(edge => { var ends = (Relation)edge.Right; return (IElement)QualitativeRecognitionPlacementPublisher.Envelope(Frame, edge.Left, ends.Left, ends.Right); }).ToArray()) : null);
    }
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
    private static Relation S(params IElement[] entries) => StructuralConstraint.Closed(entries);
    private static HashSet<IElement> Reach(IElement source)
    {
        var seen = new HashSet<IElement>(ReferenceEqualityComparer.Instance); var work = new Stack<IElement>(); work.Push(source);
        while (work.TryPop(out var item)) if (seen.Add(item) && item is Relation row) { work.Push(row.Left); work.Push(row.Right); }
        return seen;
    }
}
