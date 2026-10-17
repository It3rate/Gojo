using Gojo.Core;
using Gojo.Patterns;
using Gojo.Structures;

namespace Gojo.Experiments.QualitativeExecution.Tests;

public sealed class MeasuredStructuralSupplierTests
{
    [Fact]
    public void MeasuredStockNarrowsTheSameEventUsingOriginalStateIdentities()
    {
        var stock = new QualitativeStockFixture();
        var landmarks = new StructuralLandmarkFixture();
        var source = new Placeholder();
        var eventIdentity = new Placeholder();
        var coarseState = stock.Initial(source, eventIdentity, stock.StateDomain);
        var original = QualitativePublication.Create(source, stock.Receiver, eventIdentity,
            stock.Surface, stock.StateDomain, stock.StateDomain, coarseState);
        var supplier = new MeasuredStructuralSupplier(stock, landmarks, new Carrier());
        var envelope = supplier.Envelope(source, eventIdentity, Publish(supplier, 10));
        var measured = supplier.ObserveState(envelope);

        Assert.True(QualitativePublication.TryRefine(original, measured, out var refined, out var witness));
        Assert.True(QualitativePublication.TryRead(refined, out var s, out var subject,
            out var e, out var basis, out var domain, out var support, out _));
        Assert.Same(source, s);
        Assert.Same(eventIdentity, e);
        Assert.Same(stock.Receiver, subject);
        Assert.Same(stock.Surface, basis);
        Assert.Same(stock.StateDomain, domain);
        Assert.Equal([stock.Available], Entries(support));
        Assert.True(Contains(witness, original));
        Assert.True(Contains(witness, measured));
        Assert.True(Contains(measured, supplier.Calibration));
        Assert.True(Contains(measured, envelope));
        Assert.Empty(coarseState.Carriers());
        Assert.Empty(coarseState.CarrierLocations());
        Assert.Empty(landmarks.Surface.Carriers());
    }

    [Fact]
    public void ContradictoryMeasuredStockPreservesTheOriginalAndBothPremises()
    {
        var stock = new QualitativeStockFixture();
        var supplier = Supplier(stock);
        var source = new Placeholder();
        var eventIdentity = new Placeholder();
        var earlier = QualitativePublication.Create(source, stock.Receiver, eventIdentity,
            stock.Surface, stock.StateDomain, StructuralConstraint.Closed(stock.Empty));
        var offered = supplier.ObserveState(supplier.Envelope(source, eventIdentity, Publish(supplier, 20)));

        Assert.False(QualitativePublication.TryRefine(earlier, offered, out var accepted, out var witness));
        Assert.Same(earlier, accepted);
        Assert.Equal([stock.Empty], Support(earlier));
        Assert.True(Contains(witness, earlier));
        Assert.True(Contains(witness, offered));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ANewEventOrDifferentSourceCannotRefineTheEarlierState(bool changeEvent)
    {
        var stock = new QualitativeStockFixture();
        var supplier = Supplier(stock);
        var source = new Placeholder();
        var eventIdentity = new Placeholder();
        var earlier = QualitativePublication.Create(source, stock.Receiver, eventIdentity,
            stock.Surface, stock.StateDomain, stock.StateDomain);
        var offered = supplier.ObserveState(supplier.Envelope(
            changeEvent ? source : new Placeholder(), changeEvent ? new Placeholder() : eventIdentity,
            Publish(supplier, 10)));
        Assert.False(QualitativePublication.TryRefine(earlier, offered, out var accepted, out var witness));
        Assert.Same(earlier, accepted);
        Assert.True(Contains(witness, offered));
    }

    [Fact]
    public void AnUnmappedFractionalQuantumDoesNotInventANewDiscreteStockState()
    {
        var stock = new QualitativeStockFixture();
        var supplier = Supplier(stock);
        var measured = supplier.ObserveState(supplier.Envelope(new Placeholder(), new Placeholder(), Publish(supplier, 5)));
        Assert.Equal(Entries(stock.StateDomain), Support(measured));
        Assert.True(Contains(measured, supplier.StatePolicy));
    }

    [Fact]
    public void CoarsePublicationsRetainUncertaintyAndOnlyNewEvidenceCanNarrowThem()
    {
        var stock = new QualitativeStockFixture();
        var supplier = Supplier(stock);
        var source = new Placeholder();
        var eventIdentity = new Placeholder();
        var capacity = FiniteReadSurface.Capacity(new Carrier(), supplier.Axis, 0, 20, 11);
        var coarseA = FiniteReadSurface.Publish(new Carrier(), capacity,
            new CarrierLocation(supplier.Axis, 10), new Placeholder());
        var coarseB = FiniteReadSurface.Publish(new Carrier(), capacity,
            new CarrierLocation(supplier.Axis, 9), new Placeholder());
        var first = supplier.ObserveState(supplier.Envelope(source, eventIdentity, coarseA));
        var second = supplier.ObserveState(supplier.Envelope(source, eventIdentity, coarseB));
        Assert.Equal(Entries(stock.StateDomain), Support(first));
        Assert.Equal(Support(first), Support(second));
        var fine = supplier.ObserveState(supplier.Envelope(source, eventIdentity, Publish(supplier, 10)));
        Assert.True(QualitativePublication.TryRefine(first, fine, out var refined, out _));
        Assert.Equal([stock.Available], Support(refined));
        Assert.Equal(Entries(stock.StateDomain), Support(first));
    }

    [Fact]
    public void ClippedAndForeignAxisPublicationsCannotBecomeExactStockStates()
    {
        var stock = new QualitativeStockFixture();
        var supplier = Supplier(stock);
        var clipped = Publish(supplier, 100);
        var foreignAxis = new Carrier();
        var foreign = FiniteReadSurface.Publish(new Carrier(),
            FiniteReadSurface.Capacity(new Carrier(), foreignAxis, 0, 20),
            new CarrierLocation(foreignAxis, 10), new Placeholder());
        foreach (var input in new[] { clipped, foreign })
        {
            var read = supplier.ObserveState(supplier.Envelope(new Placeholder(), new Placeholder(), input));
            Assert.Equal(Entries(stock.StateDomain), Support(read));
            Assert.True(Contains(read, input));
        }
    }

    [Fact]
    public void AForeignCalibrationEnvelopeCannotBeReinterpretedByAnotherSupplier()
    {
        var stock = new QualitativeStockFixture();
        var landmarks = new StructuralLandmarkFixture();
        var axis = new Carrier();
        var first = new MeasuredStructuralSupplier(stock, landmarks, axis);
        var other = new MeasuredStructuralSupplier(stock, landmarks, axis, quantum: 5);
        var source = new Placeholder();
        var eventIdentity = new Placeholder();
        var earlier = QualitativePublication.Create(source, stock.Receiver, eventIdentity,
            stock.Surface, stock.StateDomain, stock.StateDomain);
        var envelope = first.Envelope(source, eventIdentity, Publish(first, 10));
        var refused = other.ObserveState(envelope);
        Assert.True(QualitativePublication.TryRead(refused, out _, out _, out _, out _, out _, out var support, out _));
        AssertOpen(support);
        Assert.False(QualitativePublication.TryRefine(earlier, refused, out var accepted, out var witness));
        Assert.Same(earlier, accepted);
        Assert.True(Contains(witness, envelope));
    }

    [Fact]
    public void MeasuredInteriorRefinesTheOriginalSharedLandmarkWithoutAssumingMidpoint()
    {
        var stock = new QualitativeStockFixture();
        var landmarks = new StructuralLandmarkFixture();
        var supplier = new MeasuredStructuralSupplier(stock, landmarks, new Carrier(), interior: 7);
        var source = new Placeholder();
        var eventIdentity = new Placeholder();
        var earlier = QualitativePublication.Create(source, landmarks.Frame, eventIdentity,
            landmarks.Frame, supplier.SiteDomain, supplier.SiteDomain, landmarks.Surface);
        var offered = supplier.ObserveLandmark(supplier.Envelope(source, eventIdentity, Publish(supplier, 7)));

        Assert.True(QualitativePublication.TryRefine(earlier, offered, out var refined, out var witness));
        Assert.Equal([landmarks.Interior], Support(refined));
        Assert.True(supplier.TryReadSitePosition(landmarks.Interior, out var position));
        Assert.Equal(7, position.Ticks);
        Assert.Same(supplier.Axis, position.Carrier);
        Assert.True(Contains(witness, landmarks.Interior));
        Assert.True(Contains(witness, landmarks.Frame));
        Assert.True(Contains(witness, supplier.LandmarkCalibration));
        Assert.False(supplier.TryReadSitePosition(new Placeholder(), out _));
        Assert.Empty(landmarks.Frame.CarrierLocations());
    }

    [Fact]
    public void TheSameCalibratedInteriorStillParticipatesInSeveralGeneratedPatterns()
    {
        var landmarks = new StructuralLandmarkFixture();
        var glyphs = new[] { landmarks.ETemplate, landmarks.HTemplate, landmarks.BTemplate }
            .Select(template =>
            {
                Assert.True(StructuralConstraint.TryReadUnique(landmarks.Generate(template), out var instance, out _));
                return Assert.IsType<Relation>(instance);
            }).ToArray();
        var supplier = new MeasuredStructuralSupplier(new QualitativeStockFixture(), landmarks, new Carrier());
        Assert.True(supplier.TryReadSitePosition(landmarks.Interior, out var measured));
        Assert.Equal(7, measured.Ticks);
        for (var i = 0; i < glyphs.Length; i++)
        {
            Assert.True(Contains(glyphs[i], landmarks.Interior));
            var roles = landmarks.ReadRoles(glyphs[i]);
            Assert.True(StructuralConstraint.TryReadUnique(roles, out _, out _));
            Assert.Empty(glyphs[i].CarrierLocations());
        }
    }

    [Fact]
    public void AConflictingMeasuredLandmarkDoesNotMoveTheUnmeasuredSharedChoice()
    {
        var landmarks = new StructuralLandmarkFixture();
        var supplier = new MeasuredStructuralSupplier(new QualitativeStockFixture(), landmarks, new Carrier());
        var source = new Placeholder();
        var eventIdentity = new Placeholder();
        var earlier = QualitativePublication.Create(source, landmarks.Frame, eventIdentity,
            landmarks.Frame, supplier.SiteDomain, StructuralConstraint.Closed(landmarks.Interior));
        var conflicting = supplier.ObserveLandmark(supplier.Envelope(source, eventIdentity, Publish(supplier, 20)));
        Assert.Equal([landmarks.End], Support(conflicting));
        Assert.False(QualitativePublication.TryRefine(earlier, conflicting, out var accepted, out var witness));
        Assert.Same(earlier, accepted);
        Assert.True(Contains(witness, earlier));
        Assert.True(Contains(witness, conflicting));
        Assert.True(supplier.TryReadSitePosition(landmarks.Interior, out var position));
        Assert.Equal(7, position.Ticks);
    }

    [Theory]
    [InlineData(20)]
    [InlineData(21)]
    [InlineData(0)]
    public void CollapsedOrReversedMeasuredOrderRemainsAnUnresolvedNativeObligation(long interior)
    {
        var supplier = new MeasuredStructuralSupplier(new QualitativeStockFixture(),
            new StructuralLandmarkFixture(), new Carrier(), interior: interior);
        var read = supplier.ObserveLandmark(supplier.Envelope(new Placeholder(), new Placeholder(), Publish(supplier, interior)));
        Assert.True(QualitativePublication.TryRead(read, out _, out _, out _, out _, out _, out var support, out var evidence));
        AssertOpen(support);
        Assert.True(Contains(evidence, supplier.LandmarkCalibration));
        Assert.Contains(Relations(evidence), relation =>
            OperationClaim.TryReadResolvedViolation(relation, out var violation) && violation.Ticks > 0);
    }

    [Fact]
    public void DistinctCoordinatesDoNotRepairCollapsedLandmarkIdentityRoles()
    {
        var site = new Placeholder();
        var landmarks = new StructuralLandmarkFixture(site, site);
        var supplier = new MeasuredStructuralSupplier(new QualitativeStockFixture(), landmarks, new Carrier());
        var read = supplier.ObserveLandmark(supplier.Envelope(new Placeholder(), new Placeholder(), Publish(supplier, 7)));
        Assert.True(QualitativePublication.TryRead(read, out _, out _, out _, out _, out _, out var support, out _));
        AssertOpen(support);
        Assert.False(supplier.TryReadSitePosition(site, out _));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AFailedIndependentCalibrationDoesNotInvalidateTheSelectedLocalRead(bool badStock)
    {
        var stock = new QualitativeStockFixture();
        var landmarks = new StructuralLandmarkFixture();
        var supplier = new MeasuredStructuralSupplier(stock, landmarks, new Carrier(),
            quantum: badStock ? -10 : 10, interior: badStock ? 7 : 20);
        var envelope = supplier.Envelope(new Placeholder(), new Placeholder(), Publish(supplier, badStock ? 7 : 10));
        var selected = badStock ? supplier.ObserveLandmark(envelope) : supplier.ObserveState(envelope);
        Assert.Equal([badStock ? landmarks.Interior : stock.Available], Support(selected));
        var failed = badStock ? supplier.ObserveState(envelope) : supplier.ObserveLandmark(envelope);
        Assert.True(QualitativePublication.TryRead(failed, out _, out _, out _, out _, out _, out var support, out _));
        AssertOpen(support);
    }

    [Fact]
    public void RetargetingAStateCoordinateCannotSilentlyBreakTheEqualQuantumPromise()
    {
        var stock = new QualitativeStockFixture();
        var supplier = Supplier(stock);
        var bindings = Assert.IsType<Relation>(supplier.StateCalibration.Right);
        var availableBinding = Assert.IsType<Relation>(Entries(bindings)[1]);
        var point = Assert.IsType<Relation>(availableBinding.Right);
        var location = Assert.IsType<CarrierLocation>(point.Left);
        location.Tick.Set(9);
        var read = supplier.ObserveState(supplier.Envelope(new Placeholder(), new Placeholder(), Publish(supplier, 9)));
        Assert.True(QualitativePublication.TryRead(read, out _, out _, out _, out _, out _, out var support, out var evidence));
        AssertOpen(support);
        Assert.Contains(Relations(evidence), relation =>
            OperationClaim.TryReadResolvedViolation(relation, out var mismatch) && mismatch.Ticks > 0);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ReplacingACalibrationPointCannotLeaveAnOldDecisionPolicyActive(bool state)
    {
        var supplier = Supplier(new QualitativeStockFixture());
        var calibration = state ? supplier.StateCalibration : supplier.LandmarkCalibration;
        var bindings = Assert.IsType<Relation>(calibration.Right);
        var row = Assert.IsType<Relation>(Entries(bindings)[1]);
        // The native graph may arrive through a structural port. Reflection is
        // used only by this adversarial test to inject that endpoint rewrite.
        typeof(Relation).GetProperty(nameof(Relation.Right))!
            .SetValue(row, Relation.Point(supplier.Axis, state ? 10 : 8));
        var envelope = supplier.Envelope(new Placeholder(), new Placeholder(), Publish(supplier, state ? 10 : 7));
        var read = state ? supplier.ObserveState(envelope) : supplier.ObserveLandmark(envelope);
        Assert.True(QualitativePublication.TryRead(read, out _, out _, out _, out _, out _, out var support, out var evidence));
        AssertOpen(support);
        Assert.True(Contains(evidence, calibration));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ACalibrationAdvertisingADifferentStructuralOwnerCannotServeTheOldOwner(bool state)
    {
        var supplier = Supplier(new QualitativeStockFixture());
        var calibration = state ? supplier.StateCalibration : supplier.LandmarkCalibration;
        var ownership = Assert.IsType<Relation>(calibration.Left);
        var foreignOwner = new Placeholder();
        typeof(Relation).GetProperty(nameof(Relation.Left))!.SetValue(ownership, foreignOwner);
        var envelope = supplier.Envelope(new Placeholder(), new Placeholder(), Publish(supplier, 10));
        var read = state ? supplier.ObserveState(envelope) : supplier.ObserveLandmark(envelope);
        Assert.True(QualitativePublication.TryRead(read, out _, out _, out _, out _, out _, out var support, out var evidence));
        AssertOpen(support);
        Assert.True(Contains(evidence, foreignOwner));
    }

    [Fact]
    public void DisconnectingTheAdvertisedCalibrationPreservesAnUnresolvedRead()
    {
        var stock = new QualitativeStockFixture();
        var supplier = Supplier(stock);
        var source = new Placeholder();
        var eventIdentity = new Placeholder();
        var earlier = QualitativePublication.Create(source, stock.Receiver, eventIdentity,
            stock.Surface, stock.StateDomain, stock.StateDomain);
        var disconnected = new Placeholder();
        typeof(Relation).GetProperty(nameof(Relation.Left))!.SetValue(supplier.Surface, disconnected);
        var offered = supplier.ObserveState(supplier.Envelope(source, eventIdentity, Publish(supplier, 10)));
        Assert.True(QualitativePublication.TryRead(offered, out _, out _, out _, out _, out _, out var support, out _));
        AssertOpen(support);
        Assert.False(QualitativePublication.TryRefine(earlier, offered, out var accepted, out var witness));
        Assert.Same(earlier, accepted);
        Assert.True(Contains(witness, disconnected));
    }

    [Theory]
    [InlineData(0, -1)]
    [InlineData(0, 0)]
    [InlineData(0, 1)]
    [InlineData(1, -1)]
    [InlineData(1, 0)]
    [InlineData(1, 1)]
    [InlineData(2, -1)]
    [InlineData(2, 0)]
    [InlineData(2, 1)]
    public void CalibratedEqualQuantumStepsAgreeWithARealClampedGojoCarrier(int initial, int pulse)
    {
        var stock = new QualitativeStockFixture();
        var axis = new Carrier();
        axis.Attach(new Relation(Relation.Point(axis, 20), Relation.Point(axis, 20)));
        axis.Set(initial * 10L);
        var supplier = new MeasuredStructuralSupplier(stock, new StructuralLandmarkFixture(), axis);
        var states = new[] { stock.Empty, stock.Available, stock.Full };
        var input = pulse < 0 ? stock.Draw : pulse > 0 ? stock.Charge : stock.Hold;
        var source = new Placeholder();
        var beforeEvent = new Placeholder();
        var afterEvent = new Placeholder();
        var before = stock.Initial(source, beforeEvent, StructuralConstraint.Closed(states[initial]));
        var offer = stock.Offer(source, afterEvent, beforeEvent, StructuralConstraint.Closed(input));
        var step = stock.Step(before, offer);
        Assert.True(stock.TryReadStep(step, out _, out _, out var after, out var transitions, out var pending));
        Assert.Empty(Entries(pending));
        if (pulse > 0) axis.Add(10);
        else if (pulse < 0) axis.Remove(10);
        var publication = FiniteReadSurface.Publish(new Carrier(), supplier.Capacity, axis.ValueSlot, axis);
        var measured = supplier.ObserveState(supplier.Envelope(source, afterEvent, publication));
        Assert.True(stock.TryReadState(after, out _, out _, out var expected));
        Assert.Equal(Entries(expected), Support(measured));
        var coarse = QualitativePublication.Create(source, stock.Receiver, afterEvent,
            stock.Surface, stock.StateDomain, expected, after);
        Assert.True(QualitativePublication.TryRefine(coarse, measured, out _, out var witness));
        Assert.True(Contains(witness, after));
        Assert.True(QualitativeStockFixture.TryReadTransition(Assert.Single(Entries(transitions)),
            out _, out _, out _, out _, out var blocked));
        var atBoundary = initial == 0 && pulse < 0 || initial == 2 && pulse > 0;
        Assert.Equal(atBoundary ? 1 : 0, Entries(blocked).Length);
        Assert.Equal(atBoundary ? pulse * 10L : 0L, axis.Tension);
    }

    private static MeasuredStructuralSupplier Supplier(QualitativeStockFixture stock) =>
        new(stock, new StructuralLandmarkFixture(), new Carrier());

    private static Relation Publish(MeasuredStructuralSupplier supplier, long ticks) =>
        FiniteReadSurface.Publish(new Carrier(), supplier.Capacity,
            new CarrierLocation(supplier.Axis, ticks), new Placeholder());

    private static IElement[] Support(Relation publication)
    {
        Assert.True(QualitativePublication.TryRead(publication, out _, out _, out _, out _, out _, out var support, out _));
        return Entries(support);
    }

    private static IElement[] Entries(Relation scope)
    {
        Assert.True(StructuralConstraint.TryReadScope(scope, out var entries, out _, out _));
        return entries;
    }

    private static void AssertOpen(Relation scope)
    {
        Assert.True(StructuralConstraint.TryReadScope(scope, out _, out var seam, out var tail));
        Assert.NotEqual(seam, tail);
    }

    private static bool Contains(IElement root, IElement target) =>
        Relations(root).Any(relation => ReferenceEquals(relation, target) ||
            ReferenceEquals(relation.Left, target) || ReferenceEquals(relation.Right, target));

    private static IEnumerable<Relation> Relations(IElement root)
    {
        var seen = new HashSet<IElement>(ReferenceEqualityComparer.Instance);
        var pending = new Stack<IElement>();
        pending.Push(root);
        while (pending.TryPop(out var element))
        {
            if (!seen.Add(element) || element is not Relation relation) continue;
            yield return relation;
            pending.Push(relation.Left);
            pending.Push(relation.Right);
        }
    }
}
