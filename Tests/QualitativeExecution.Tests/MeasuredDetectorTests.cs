using Gojo.Core;
using Gojo.Patterns;
using Gojo.Structures;
using Xunit;

namespace Gojo.Experiments.QualitativeExecution.Tests;

public sealed class MeasuredDetectorTests
{
    [Fact]
    public void LaterMeasuredEvidenceRefinesTheSameEventAndOriginalCategoryIdentities()
    {
        var view = new ThresholdFixture().Forward;
        var source = new Placeholder();
        var eventIdentity = new Placeholder();
        var original = view.Observe(source, eventIdentity, view.SideDomain, view.TrendDomain);
        Assert.Empty(original.Carriers());
        Assert.Empty(original.CarrierLocations());
        var adapter = new MeasuredDetectorAdapter(view, new Carrier(), -100, -1, 1, 100);
        var measured = adapter.Observe(source, eventIdentity,
            Publish(adapter, 5), Publish(adapter, -2));

        Assert.True(DetectorRefinement.TryAccept(view, original, measured, out var refined, out var witness));
        Assert.True(DetectorObservation.TryRead(refined, out var retainedSource, out var retainedEvent,
            out var frame, out var sides, out var trends, out _));
        Assert.Same(source, retainedSource);
        Assert.Same(eventIdentity, retainedEvent);
        Assert.Same(view.Surface, frame);
        Assert.Equal([view.Above], Entries(sides));
        Assert.Equal([view.Falling], Entries(trends));
        Assert.True(FiniteLaw.TryReadResult(view.ReadCompiled(refined), out var outcomes, out _, out _));
        Assert.Equal([view.Approaching], Entries(outcomes));
        Assert.True(Contains(witness, original));
        Assert.True(Contains(witness, measured));
        Assert.True(Contains(measured, adapter.Calibration));
        Assert.Empty(original.Carriers());
        Assert.Empty(original.CarrierLocations());
    }

    [Fact]
    public void MeasuredContradictionRetainsBothClaimsAndLeavesTheEarlierObservationUnchanged()
    {
        var view = new ThresholdFixture().Forward;
        var source = new Placeholder();
        var eventIdentity = new Placeholder();
        var earlier = view.Observe(source, eventIdentity,
            StructuralConstraint.Closed(view.Below), view.TrendDomain);
        var adapter = new MeasuredDetectorAdapter(view, new Carrier(), -100, -1, 1, 100);
        var conflicting = adapter.Observe(source, eventIdentity, Publish(adapter, 5), Publish(adapter, -2));

        Assert.False(DetectorRefinement.TryAccept(view, earlier, conflicting, out var accepted, out var witness));
        Assert.Same(earlier, accepted);
        Assert.True(Contains(witness, earlier));
        Assert.True(Contains(witness, conflicting));
        Assert.True(DetectorObservation.TryRead(earlier, out _, out _, out _, out var side, out _, out _));
        Assert.Equal([view.Below], Entries(side));
    }

    [Fact]
    public void CoarseNumericSupportDoesNotRecoverPrivateMeasurementAndNeedsANewPublicationToRefine()
    {
        var view = new ThresholdFixture().Forward;
        var source = new Placeholder();
        var eventIdentity = new Placeholder();
        var adapter = new MeasuredDetectorAdapter(view, new Carrier(), -100, -1, 1, 100);
        var coarseCapacity = FiniteReadSurface.Capacity(new Carrier(), adapter.Axis, -100, 100, 4);
        var firstSignal = FiniteReadSurface.Publish(new Carrier(), coarseCapacity,
            new CarrierLocation(adapter.Axis, 2), new Placeholder());
        var otherSignal = FiniteReadSurface.Publish(new Carrier(), coarseCapacity,
            new CarrierLocation(adapter.Axis, 3), new Placeholder());
        var trend = Publish(adapter, -2);
        var coarse = adapter.Observe(source, eventIdentity, firstSignal, trend);
        var equalCoarse = adapter.Observe(source, eventIdentity, otherSignal, trend);
        Assert.True(DetectorObservation.TryRead(coarse, out _, out _, out _, out var coarseSide, out _, out _));
        Assert.True(DetectorObservation.TryRead(equalCoarse, out _, out _, out _, out var otherSide, out _, out _));
        Assert.Same(view.SideDomain, coarseSide);
        Assert.Same(view.SideDomain, otherSide);
        Assert.True(FiniteReadSurface.TryReadSupport(firstSignal, out var low, out var high));
        Assert.Equal(-3, low!.Value.Ticks);
        Assert.Equal(3, high!.Value.Ticks);

        // This is new externally offered evidence, not recovery from coarse.
        var fresh = adapter.Observe(source, eventIdentity, Publish(adapter, 2), trend);
        Assert.True(DetectorRefinement.TryAccept(view, coarse, fresh, out var refined, out _));
        Assert.True(DetectorObservation.TryRead(refined, out _, out _, out _, out var fineSide, out _, out _));
        Assert.Equal([view.Above], Entries(fineSide));
        Assert.Same(view.SideDomain, coarseSide);
    }

    [Fact]
    public void ACalibratedThresholdCanBeAwayFromNumericZeroAndBandMotionStillIsNotStationarity()
    {
        var view = new ThresholdFixture().Forward;
        var adapter = new MeasuredDetectorAdapter(view, new Carrier(), -100, 9, 11, 100);
        var within = adapter.Observe(new Placeholder(), new Placeholder(),
            Publish(adapter, 10), Publish(adapter, -2));
        var atNumericZero = adapter.Observe(new Placeholder(), new Placeholder(),
            Publish(adapter, 0), Publish(adapter, 0));

        Assert.True(FiniteLaw.TryReadResult(view.ReadNative(within), out var withinResult, out _, out _));
        Assert.Equal([view.WithinChanging], Entries(withinResult));
        Assert.True(FiniteLaw.TryReadResult(view.ReadNative(atNumericZero), out var zeroResult, out _, out _));
        Assert.Equal([view.OutsideSteady], Entries(zeroResult));
    }

    private static Relation Publish(MeasuredDetectorAdapter adapter, long ticks) =>
        FiniteReadSurface.Publish(new Carrier(), adapter.Capacity,
            new CarrierLocation(adapter.Axis, ticks), new Placeholder());

    private static IElement[] Entries(Relation scope)
    {
        Assert.True(StructuralConstraint.TryReadScope(scope, out var entries, out _, out _));
        return entries;
    }

    private static bool Contains(IElement root, IElement target)
    {
        var seen = new HashSet<IElement>(ReferenceEqualityComparer.Instance);
        var pending = new Stack<IElement>();
        pending.Push(root);
        while (pending.TryPop(out var element))
        {
            if (!seen.Add(element)) continue;
            if (ReferenceEquals(element, target)) return true;
            if (element is Relation relation)
            { pending.Push(relation.Left); pending.Push(relation.Right); }
        }
        return false;
    }
}
