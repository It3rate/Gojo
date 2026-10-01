using Gojo.Core;
using Gojo.Equations;
using Gojo.Structures;

namespace Gojo.Tests.Core;

public sealed class QualifiedReadSurfaceTests
{
    private static Relation Observe(Carrier axis, long value, long basis, long low, long high,
        IElement? coverage = null, IElement? scope = null) =>
        QualifiedReadSurface.Create(new Carrier(), Relation.Point(axis, value), Relation.Segment(axis, 0, basis),
            new CarrierLocation(axis, value), Relation.Segment(axis, low, high), coverage ?? new Placeholder(),
            scope ?? new Placeholder(), new Placeholder(), new Placeholder());

    private static Relation LiveObservation(CarrierLocation input, Relation basis) =>
        QualifiedReadSurface.Create(new Carrier(), input, basis, input, new Relation(input, input),
            new Placeholder(), new Placeholder(), new Placeholder(), input);

    [Fact]
    public void UnresolvedCalculationsRetainTheirOperationAndRespondWhenBasisAppears()
    {
        var axis = new Carrier();
        var span = new TickRef(0);
        var basis = new Relation(new CarrierLocation(axis, 0), new CarrierLocation(axis, span));
        var a = LiveObservation(new CarrierLocation(axis, 3), basis);
        var b = LiveObservation(new CarrierLocation(axis, 2), basis);
        var sum = QualifiedReadSurface.Add(new Carrier(), a, b);
        var difference = QualifiedReadSurface.Subtract(new Carrier(), a, b);
        Assert.False(QualifiedReadSurface.TryReadValue(sum, out _));
        Assert.False(QualifiedReadSurface.TryReadValue(difference, out _));
        span.Set(12);
        Assert.True(QualifiedReadSurface.TryReadValue(sum, out var total)); Assert.Equal(5, total.Ticks);
        Assert.True(QualifiedReadSurface.TryReadValue(difference, out var delta)); Assert.Equal(1, delta.Ticks);
        span.Set(0);
        Assert.False(QualifiedReadSurface.TryReadValue(sum, out _));
        Assert.False(QualifiedReadSurface.TryReadBounds(difference, out _, out _));
    }

    [Fact]
    public void ChainedReadsRecomputeFromLiveInputsWithoutInstallingAnAnswer()
    {
        var axis = new Carrier(); var target = new Carrier();
        axis.Set(2);
        var a = LiveObservation(axis.ValueSlot, Relation.Segment(axis, 0, 12));
        var b = LiveObservation(new CarrierLocation(axis, 3), Relation.Segment(axis, 0, 12));
        var sum = QualifiedReadSurface.Add(new Carrier(), a, b);
        var scaled = QualifiedReadSurface.Reframe(new Carrier(), sum, Equation.Affine(axis, 0, 12, target, 0, 120));
        var separation = QualifiedReadSurface.Separation(new Carrier(), a, b);
        ReadWitness.TryRead(sum, out _, out _, out var openResult, out _, out _);
        Assert.IsType<Placeholder>(openResult);
        Assert.True(QualifiedReadSurface.TryReadValue(scaled, out var first)); Assert.Equal(50, first.Ticks);
        Assert.True(QualifiedReadSurface.TryReadSeparation(separation, out var before, out _)); Assert.Equal(-1, before.Ticks);
        axis.Set(9);
        Assert.True(QualifiedReadSurface.TryReadValue(scaled, out var current)); Assert.Equal(120, current.Ticks);
        Assert.True(QualifiedReadSurface.TryReadBounds(scaled, out var low, out var high));
        Assert.Equal(120, low.Ticks); Assert.Equal(120, high.Ticks);
        Assert.True(QualifiedReadSurface.TryReadSeparation(separation, out var after, out _)); Assert.Equal(6, after.Ticks);
        ReadWitness.TryRead(sum, out _, out _, out var stillOpen, out _, out _);
        Assert.Same(openResult, stillOpen);
        // An explicitly retained return value is an observation of that call,
        // never the source of the next read.
        Assert.Equal(50, first.Ticks);
    }

    [Fact]
    public void CoarseningThenExpandingPreservesAcquisitionBasisAndExcludedParticipation()
    {
        var fine = new Carrier(); var coarse = new Carrier();
        var original = Observe(fine, 100, 200, 99, 101);
        var reduced = QualifiedReadSurface.Reframe(new Carrier(), original, Equation.Affine(fine, 0, 200, coarse, 0, 2));
        Assert.True(QualifiedReadSurface.TryReadValue(reduced, out var half));
        Assert.Equal(1, half.Ticks);
        Assert.True(QualifiedReadSurface.TryRead(reduced, out var selected, out _, out _, out _, out _, out _, out _, out _));
        Assert.Same(original, selected);
        var restored = (Relation)GojoEncodedSnapshot.CaptureArchive(reduced).RestoreRoots()[0];
        Assert.True(QualifiedReadSurface.TryRead(restored, out var originalRead, out var display, out _, out _, out _, out _, out _, out _));
        Assert.True(QualifiedReadSurface.TryRead((Relation)originalRead, out _, out var acquisition, out _, out _, out _, out _, out var excluded, out _));
        Assert.Equal(200, acquisition.FlowOn(((Relation)acquisition.Left).LeftLocation.Carrier).Ticks);
        Assert.IsType<Placeholder>(excluded);
        var back = QualifiedReadSurface.Reframe(new Carrier(), restored,
            new Relation(display, acquisition));
        Assert.True(QualifiedReadSurface.TryReadValue(back, out var value));
        Assert.Equal(100, value.Ticks);
        // The intermediate coarse envelope was not relabeled as new precision.
        Assert.True(QualifiedReadSurface.TryReadBounds(back, out var low, out var high));
        Assert.Equal(0, low.Ticks); Assert.Equal(200, high.Ticks);
    }

    [Fact]
    public void OutwardRoundingPreservesBothNegativeAndPositiveBounds()
    {
        var axis = new Carrier(); var target = new Carrier();
        var source = Observe(axis, 0, 3, -1, 1);
        var read = QualifiedReadSurface.Reframe(new Carrier(), source, Equation.Affine(axis, 0, 3, target, 0, 1));
        Assert.True(QualifiedReadSurface.TryReadBounds(read, out var low, out var high));
        Assert.Equal(-1, low.Ticks); Assert.Equal(1, high.Ticks);
    }

    [Fact]
    public void CoverageIsIndependentOfAmountAndSurvivesArithmetic()
    {
        var packets = new Carrier(); var light = new Carrier(); var source = new Carrier();
        var coverage = ReadWitness.Create(new Carrier(), source, Relation.Segment(packets, 0, 100),
            new CarrierLocation(packets, 20), Relation.Segment(packets, 20, 100), source);
        var scope = new Relation(source, Relation.Segment(new Carrier(), 0, 1));
        var amount = Observe(light, 3000, 4000, 2900, 3100, coverage, scope);
        var more = Observe(light, 100, 4000, 90, 110, coverage, scope);
        var sum = QualifiedReadSurface.Add(new Carrier(), amount, more);
        Assert.True(QualifiedReadSurface.TryReadValue(sum, out var value));
        Assert.Equal(3100, value.Ticks);
        Assert.True(QualifiedReadSurface.TryRead(sum, out _, out _, out _, out _, out var retainedCoverage, out var retainedScope, out _, out _));
        Assert.Same(coverage, retainedCoverage); Assert.Same(scope, retainedScope);
        Assert.True(QualifiedReadSurface.TryReadBounds(sum, out var low, out var high));
        Assert.Equal(2990, low.Ticks); Assert.Equal(3210, high.Ticks);
    }

    [Fact]
    public void DifferencePropagatesIntervalsAndPreservesSharedObservationDependency()
    {
        var axis = new Carrier();
        var johnGap = Observe(axis, 2, 12, 1, 3);
        var sueGap = Observe(axis, 10, 12, 8, 12);
        var difference = QualifiedReadSurface.Subtract(new Carrier(), sueGap, johnGap);
        Assert.True(QualifiedReadSurface.TryReadValue(difference, out var value)); Assert.Equal(8, value.Ticks);
        Assert.True(QualifiedReadSurface.TryReadBounds(difference, out var low, out var high));
        Assert.Equal(5, low.Ticks); Assert.Equal(11, high.Ticks);
        var cancelled = QualifiedReadSurface.Subtract(new Carrier(), sueGap, sueGap);
        Assert.True(QualifiedReadSurface.TryReadBounds(cancelled, out low, out high));
        Assert.Equal(0, low.Ticks); Assert.Equal(0, high.Ticks);
        // Cancellation is conditional on the same observation: its source and
        // scope remain attached even when the local interval collapses.
        Assert.True(QualifiedReadSurface.TryRead(cancelled, out var selected, out _, out _, out _, out _, out _, out _, out _));
        var group = (Relation)selected;
        group.TryReadOrderedElementsOn((Carrier)group.Right, out var operands);
        Assert.Same(sueGap, operands[0]); Assert.Same(sueGap, operands[1]);
    }

    [Fact]
    public void ShiftedBasesRetainTheirOriginThroughArithmeticAndComparison()
    {
        var axis = new Carrier();
        var left = Observe(axis, 8, 12, 7, 9);
        var right = Observe(axis, 2, 12, 1, 3);
        var shift = Equation.Affine(axis, 0, 12, axis, 100, 112);
        var a = QualifiedReadSurface.Reframe(new Carrier(), left, shift);
        var b = QualifiedReadSurface.Reframe(new Carrier(), right, shift);
        var sum = QualifiedReadSurface.Add(new Carrier(), a, b);
        var difference = QualifiedReadSurface.Subtract(new Carrier(), a, b);
        Assert.True(QualifiedReadSurface.TryReadValue(sum, out var total));
        Assert.Equal(110, total.Ticks);
        Assert.True(QualifiedReadSurface.TryReadBounds(difference, out var low, out var high));
        Assert.Equal(104, low.Ticks); Assert.Equal(108, high.Ticks);
        var separation = QualifiedReadSurface.Separation(new Carrier(), a, b);
        Assert.True(QualifiedReadSurface.TryReadSeparation(separation, out var result, out _));
        Assert.Equal(4, result.Ticks);
    }

    [Fact]
    public void OpposedFaceReframingRequiresAndRetainsExplicitAlignment()
    {
        var axis = new Carrier();
        var source = Observe(axis, 2, 12, 1, 3);
        var inverse = axis.Inverse();
        var alignment = Equation.Affine(axis, 0, 12, inverse, 0, -12);
        var reframed = QualifiedReadSurface.Reframe(new Carrier(), source, alignment);
        Assert.True(QualifiedReadSurface.TryReadValue(reframed, out var value));
        Assert.Same(inverse, value.Carrier); Assert.Equal(-2, value.Ticks);
        Assert.True(QualifiedReadSurface.TryReadBounds(reframed, out var low, out var high));
        Assert.Equal(-3, low.Ticks); Assert.Equal(-1, high.Ticks);
        Assert.True(QualifiedReadSurface.TryRead(reframed, out var selected, out _, out _, out _, out _, out _, out _, out _));
        Assert.Same(source, selected);
    }

    [Fact]
    public void EmptyAndUnalignedBasesRemainUnresolved()
    {
        var axis = new Carrier(); var other = new Carrier(axis.UnitShape);
        var empty = Observe(axis, 0, 0, 0, 0);
        Assert.False(QualifiedReadSurface.TryReadValue(empty, out _));
        Assert.False(QualifiedReadSurface.TryReadBounds(empty, out _, out _));
        var aligned = Observe(axis, 0, 12, 0, 0);
        var unaligned = Observe(other, 0, 12, 0, 0);
        var pending = QualifiedReadSurface.Add(new Carrier(), aligned, unaligned);
        ReadWitness.TryRead(pending, out _, out _, out var result, out var residue, out _);
        Assert.IsType<Placeholder>(result); Assert.IsType<Relation>(residue);
        var claim = OperationClaim.AtLeast(new Carrier(), new CarrierLocation(axis, 0), new CarrierLocation(other, 0), new Carrier());
        Assert.False(OperationClaim.TryReadResolvedViolation(claim, out _));
        var known = OperationClaim.AtLeast(new Carrier(), new CarrierLocation(axis, 0), new CarrierLocation(axis, 0), new Carrier());
        Assert.True(OperationClaim.TryReadResolvedViolation(known, out var violation)); Assert.Equal(0, violation.Ticks);
    }

    [Fact]
    public void ZeroProjectionRetainsAbsentBasisEvenWhenInputHasNoMagnitude()
    {
        var source = new Carrier(); var target = new Carrier();
        var empty = Equation.Affine(source, 0, 0, target, 0, 10).Project(new CarrierLocation(source, 0), target);
        var populated = Equation.Affine(source, 0, 10, target, 0, 10).Project(new CarrierLocation(source, 0), target);
        Assert.IsType<Relation>(empty.Right); Assert.IsType<CarrierLocation>(populated.Right);
    }
}
