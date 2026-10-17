using Gojo.Core;
using Gojo.Equations;
using Gojo.Patterns;
using Gojo.Structures;

namespace Gojo.Experiments.QualitativeExecution.Tests;

/// <summary>
/// Bounded comparisons with real carrier tension settlement. Agreement belongs
/// to the declared quantum and accounting law; these are not speed benchmarks
/// or a claim that a three-state stock reconstructs an arbitrary reservoir.
/// </summary>
public sealed class MeasuredDynamicsComparisonTests
{
    [Fact]
    public void EqualQuantumTemporalTraceAgreesWhileAllPacketsRemainRepresentable()
    {
        var f = new Comparison();
        var eventIdentity = (IElement)new Placeholder();
        var state = f.Stock.Initial(f.Source, eventIdentity, S(f.Stock.Empty));
        var matched = new List<IElement>();
        var chargeBasis = new Relation(f.Stock.Charge, Relation.Segment(f.Amount, 0, 10));
        var drawBasis = new Relation(f.Stock.Draw, Relation.Segment(f.Amount, 10, 0));
        var holdBasis = new Relation(f.Stock.Hold, Relation.Segment(f.Amount, 0, 0));
        (IElement Command, Relation Basis, Action Apply, IElement Expected)[] steps =
        [
            (f.Stock.Charge, chargeBasis, () => f.Amount.Add(10), f.Stock.Available),
            (f.Stock.Charge, chargeBasis, () => f.Amount.Add(10), f.Stock.Full),
            (f.Stock.Hold, holdBasis, () => f.Amount.Add(0), f.Stock.Full),
            (f.Stock.Draw, drawBasis, () => f.Amount.Remove(10), f.Stock.Available),
            (f.Stock.Draw, drawBasis, () => f.Amount.Remove(10), f.Stock.Empty)
        ];
        foreach (var entry in steps)
        {
            var nextEvent = new Placeholder();
            var offer = f.Stock.Offer(f.Source, nextEvent, eventIdentity, S(entry.Command), entry.Basis);
            var step = f.Stock.Step(state, offer);
            state = After(f.Stock, step);
            entry.Apply();
            var observed = f.Observe(nextEvent);
            var structural = f.Publication(state);
            Assert.True(QualitativePublication.TryRefine(structural, observed, out var accepted, out var proof));
            AssertSupport(accepted, entry.Expected);
            Assert.Equal(0, f.Amount.Tension);
            matched.Add(new Relation(new Relation(step, entry.Basis), new Relation(observed, proof)));
            eventIdentity = nextEvent;
        }
        var comparison = new Relation(new Relation(f.Stock.Surface, f.Boundary), S(matched.ToArray()));
        Assert.True(Contains(comparison, chargeBasis));
        Assert.True(Contains(comparison, drawBasis));
        Assert.True(Contains(comparison, f.Supplier.StateCalibration));
        Assert.Equal(0UL, f.Amount.Value);
    }

    [Fact]
    public void AFinerPacketCannotJustifyTheDeclaredStockQuantumTransition()
    {
        var f = new Comparison(); var initialEvent = new Placeholder(); var eventIdentity = new Placeholder();
        var initial = f.Stock.Initial(f.Source, initialEvent, S(f.Stock.Empty));
        var offeredPacket = Relation.Segment(f.Amount, 0, 5);
        var declaredQuantum = Relation.Segment(f.Amount, 0, 10);
        var quantumClaim = OperationClaim.Equality(new Carrier(),
            new CarrierLocation(f.Amount, 5), new CarrierLocation(f.Amount, 10), f.Amount);
        Assert.True(OperationClaim.TryReadResolvedViolation(quantumClaim, out var mismatch));
        Assert.Equal(5, mismatch.Ticks);

        // Deliberately dropping the packet distinction would execute a complete
        // qualitative charge. The measured supplier must not endorse that step.
        var offer = f.Stock.Offer(f.Source, eventIdentity, initialEvent, S(f.Stock.Charge),
            new Relation(new Relation(declaredQuantum, offeredPacket), quantumClaim));
        var qualitative = After(f.Stock, f.Stock.Step(initial, offer));
        f.Amount.Add(5);
        var measured = f.Observe(eventIdentity);
        Assert.Equal(5UL, f.Amount.Value);
        AssertSupport(f.Publication(qualitative), f.Stock.Available);
        AssertSupport(measured, f.Stock.Empty, f.Stock.Available, f.Stock.Full);
        Assert.False(QualitativePublication.TryRefine(f.Publication(qualitative), measured,
            out var retained, out var counterexample));
        AssertSupport(retained, f.Stock.Available);
        var witness = new Relation(new Relation(offer, quantumClaim), counterexample);
        Assert.True(Contains(witness, offeredPacket));
        Assert.True(Contains(witness, measured));
        Assert.True(Contains(witness, declaredQuantum));
    }

    [Fact]
    public void HeldClampTensionAndRejectedQualitativeChargeDivergeOnTheNextDraw()
    {
        var f = new Comparison(); var initialEvent = new Placeholder();
        f.Amount.Add(20);
        var full = f.Stock.Initial(f.Source, initialEvent, S(f.Stock.Full));
        var chargeEvent = new Placeholder();
        var offered = f.Stock.Offer(f.Source, chargeEvent, initialEvent, S(f.Stock.Charge),
            Relation.Segment(f.Amount, 0, 10));
        var chargedStep = f.Stock.Step(full, offered);
        var charged = After(f.Stock, chargedStep);
        f.Amount.Add(10);
        var retainedPressure = f.Amount.TensionSlot;
        IElement retainedPressureEvidence = retainedPressure;
        Assert.Equal(20UL, f.Amount.Value); Assert.Equal(10, retainedPressure.Ticks);
        AssertSupport(f.Publication(charged), f.Stock.Full);
        AssertSupport(f.Observe(chargeEvent), f.Stock.Full);
        Assert.True(f.Stock.TryReadStep(chargedStep, out _, out _, out _, out var transitions, out _));
        Assert.True(QualitativeStockFixture.TryReadTransition(Assert.Single(FiniteLaw.Entries(transitions)),
            out _, out _, out _, out var accepted, out var blocked));
        Assert.Empty(FiniteLaw.Entries(accepted));
        Assert.Same(f.Stock.Charge, Assert.Single(FiniteLaw.Entries(blocked)));

        var drawEvent = new Placeholder();
        var draw = f.Stock.Offer(f.Source, drawEvent, chargeEvent, S(f.Stock.Draw),
            new Relation(Relation.Segment(f.Amount, 10, 0), retainedPressureEvidence));
        var drawnStep = f.Stock.Step(charged, draw);
        var drawn = After(f.Stock, drawnStep);
        f.Amount.Remove(10);
        Assert.Equal(20UL, f.Amount.Value); Assert.Equal(0, f.Amount.Tension);
        var measured = f.Observe(drawEvent);
        var structural = f.Publication(drawn);
        AssertSupport(structural, f.Stock.Available);
        AssertSupport(measured, f.Stock.Full);
        Assert.False(QualitativePublication.TryRefine(structural, measured, out var retained, out var failed));
        Assert.Same(structural, retained);
        var clock = new Carrier();
        var numericCharge = TimeStepSequence.Step(new Carrier(), clock, 0, 1,
            Relation.Point(f.Amount, 20), Relation.Segment(f.Amount, 0, 10),
            Relation.Point(f.Amount, 20), Relation.Point(f.Amount, retainedPressure.Ticks));
        var numericDraw = TimeStepSequence.Step(new Carrier(), clock, 1, 2,
            Relation.Point(f.Amount, 20), Relation.Segment(f.Amount, 10, 0),
            Relation.Point(f.Amount, 20), Relation.Point(f.Amount, 0));
        var comparison = new Relation(new Relation(chargedStep, numericCharge),
            new Relation(new Relation(drawnStep, numericDraw), failed));
        Assert.True(Contains(comparison, retainedPressureEvidence));
        Assert.True(Contains(comparison, f.Boundary));
    }

    [Fact]
    public void CoarseMeasuredPublicationNeedsFreshEvidenceToRecoverTheSameStateIdentity()
    {
        var f = new Comparison(); var eventIdentity = new Placeholder();
        f.Amount.Add(10);
        var coarseCapacity = FiniteReadSurface.Capacity(new Carrier(), f.Amount, 0, 20, 4);
        var coarse = FiniteReadSurface.Publish(new Carrier(), coarseCapacity,
            f.Amount.ValueSlot, new Placeholder());
        Assert.True(FiniteReadSurface.TryReadSupport(coarse, out var lower, out var upper));
        Assert.Equal(8, lower!.Value.Ticks); Assert.Equal(11, upper!.Value.Ticks);
        var earlier = f.Supplier.ObserveState(f.Supplier.Envelope(f.Source, eventIdentity, coarse));
        AssertSupport(earlier, f.Stock.Empty, f.Stock.Available, f.Stock.Full);

        // Only this new exact advertised publication supplies the lost detail.
        var measured = f.Observe(eventIdentity);
        AssertSupport(measured, f.Stock.Available);
        Assert.True(QualitativePublication.TryRefine(earlier, measured, out var accepted, out var witness));
        AssertSupport(accepted, f.Stock.Available);
        Assert.True(Contains(witness, coarse));
        Assert.True(Contains(witness, measured));
        AssertSupport(earlier, f.Stock.Empty, f.Stock.Available, f.Stock.Full);
        Assert.True(QualitativePublication.TryRead(accepted, out _, out var subject, out var retainedEvent,
            out var basis, out _, out _, out _));
        Assert.Same(f.Stock.Receiver, subject); Assert.Same(eventIdentity, retainedEvent);
        Assert.Same(f.Stock.Surface, basis);
    }

    private sealed class Comparison
    {
        public QualitativeStockFixture Stock { get; } = new();
        public Carrier Amount { get; } = new();
        public IElement Source { get; } = new Placeholder();
        public Relation Boundary { get; }
        public MeasuredStructuralSupplier Supplier { get; }
        public Comparison()
        {
            Boundary = Amount.Attach(new Relation(Relation.Point(Amount, 20), Relation.Point(Amount, 20)));
            Supplier = new MeasuredStructuralSupplier(Stock, new StructuralLandmarkFixture(), Amount);
        }
        public Relation Observe(IElement eventIdentity)
        {
            var publication = FiniteReadSurface.Publish(new Carrier(), Supplier.Capacity,
                new CarrierLocation(Amount, checked((long)Amount.Value)), new Relation(Amount, Boundary));
            return Supplier.ObserveState(Supplier.Envelope(Source, eventIdentity, publication));
        }
        public Relation Publication(Relation state)
        {
            Assert.True(Stock.TryReadState(state, out var source, out var eventIdentity, out var support));
            return QualitativePublication.Create(source, Stock.Receiver, eventIdentity,
                Stock.Surface, Stock.StateDomain, support, state);
        }
    }

    private static Relation After(QualitativeStockFixture stock, Relation receipt)
    {
        Assert.True(stock.TryReadStep(receipt, out _, out _, out var after, out _, out var unresolved));
        Assert.Empty(FiniteLaw.Entries(unresolved));
        return after;
    }
    private static Relation S(params IElement[] entries) => StructuralConstraint.Closed(entries);
    private static void AssertSupport(Relation publication, params IElement[] expected)
    {
        Assert.True(QualitativePublication.TryRead(publication, out _, out _, out _, out _,
            out _, out var support, out _));
        Assert.True(StructuralConstraint.TryReadScope(support, out var entries, out var seam, out var tail));
        Assert.Equal(seam, tail); Assert.Equal(expected.Length, entries.Length);
        Assert.All(expected, entry => Assert.Contains(entry, entries));
    }
    private static bool Contains(IElement root, IElement target)
    {
        var pending = new Stack<IElement>(); var seen = new HashSet<IElement>(ReferenceEqualityComparer.Instance);
        pending.Push(root);
        while (pending.TryPop(out var element))
        {
            if (ReferenceEquals(element, target)) return true;
            if (!seen.Add(element) || element is not Relation relation) continue;
            pending.Push(relation.Left); pending.Push(relation.Right);
        }
        return false;
    }
}
