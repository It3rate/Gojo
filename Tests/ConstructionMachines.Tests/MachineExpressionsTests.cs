using Gojo.Core;
using Gojo.Encoding;
using Gojo.Experiments.ConstructionMachines;
using static Gojo.Experiments.ConstructionMachines.MachineExpressions;

namespace Gojo.ConstructionMachines.Tests;

public sealed class MachineExpressionsTests
{
    [Theory]
    [InlineData(1, 3)]
    [InlineData(-1, -3)]
    public void AffineReadPreservesExactFractionsAndOrderedParameterFrame(long sample, long denominator)
    {
        var chart = new Chart();
        var expression = Interpolate(chart.Basis, chart.Point(0, 2), chart.Point(1, 0),
            new CarrierLocation(chart.Grade, sample), denominator, chart.Grade);
        var witnesses = new List<Relation>();

        var result = Evaluate(expression, [], witnesses);

        Coordinate(result, 0, chart.X, 1, 3);
        Coordinate(result, 1, chart.Y, 4, 3);
        Assert.Single(witnesses);
        Assert.Empty(F(F(witnesses[0])[3]));
    }

    [Fact]
    public void OneOpenCoordinateDoesNotInventOrHideTheOtherCoordinate()
    {
        var chart = new Chart();
        var missing = new Placeholder();
        var first = Relation.OrderedGroup(chart.Holder, missing, new CarrierLocation(chart.Y, 2));
        var expression = Interpolate(chart.Basis, first, chart.Point(2, 0),
            new CarrierLocation(chart.Grade, 1), 2, chart.Grade);
        var witnesses = new List<Relation>();

        var result = Evaluate(expression, [], witnesses);

        Assert.IsType<Placeholder>(F(result)[0]);
        Coordinate(result, 1, chart.Y, 1);
        Assert.NotEmpty(F(F(Assert.Single(witnesses))[3]));
        Assert.Same(missing, F(first)[0]);
    }

    [Fact]
    public void AnOpenParameterRemainsOpenWithTheFailedReadConnected()
    {
        var chart = new Chart();
        var parameter = new Placeholder();
        var expression = Interpolate(chart.Basis, chart.Point(0, 0), chart.Point(6, 4), parameter, 1, chart.Grade);
        var witnesses = new List<Relation>();

        Assert.IsType<Placeholder>(Evaluate(expression, [], witnesses));

        Assert.NotEmpty(F(F(Assert.Single(witnesses))[3]));
        Assert.Same(expression, F(witnesses[0])[0]);
    }

    [Fact]
    public void CoordinateBindingRetainsItsNativeHolderAndUnchangedCoordinate()
    {
        var chart = new Chart();
        var missing = new Placeholder();
        IElement known = new CarrierLocation(chart.Y, 9);
        IElement supplied = new CarrierLocation(chart.X, 4);
        var point = Relation.OrderedGroup(chart.Holder, missing, known);
        var witnesses = new List<Relation>();

        var result = Assert.IsType<Relation>(Evaluate(point, [new Relation(missing, supplied)], witnesses));

        Assert.Same(chart.Holder, result.Right);
        Assert.Same(supplied, F(result)[0]);
        Assert.Same(known, F(result)[1]);
        Assert.Same(missing, F(point)[0]);
        Assert.Empty(witnesses);
    }

    [Fact]
    public void ConflictingBindingsRemainAlternativesInsteadOfSelectingTheFirst()
    {
        var chart = new Chart();
        var slot = new Placeholder();
        var first = new Relation(slot, new CarrierLocation(chart.Grade, 1));
        var second = new Relation(slot, new CarrierLocation(chart.Grade, 2));
        var witnesses = new List<Relation>();

        Assert.Same(slot, Evaluate(slot, [first, second], witnesses));

        var remainder = F(F(Assert.Single(witnesses))[3]);
        Assert.Equal(2, remainder.Length);
        Assert.Same(first, remainder[0]);
        Assert.Same(second, remainder[1]);
    }

    [Fact]
    public void RepeatedReferenceToTheSameBindingIsNotAConflictingAlternative()
    {
        var slot = new Placeholder();
        IElement sample = new CarrierLocation(new Carrier(), 1);
        var witnesses = new List<Relation>();

        Assert.Same(sample, Evaluate(slot, [new(slot, sample), new(slot, sample)], witnesses));
        Assert.Empty(witnesses);
    }

    [Fact]
    public void SeparatelyBoxedLocationsWithTheSameTickAreOneParticipantButEqualCoordinatesAreNot()
    {
        var slot = new Placeholder();
        var carrier = new Carrier();
        carrier.Add(3);
        var witnesses = new List<Relation>();
        var shared = Evaluate(slot,
            [new(slot, carrier.ValueSlot), new(slot, carrier.ValueSlot)], witnesses);

        Assert.True(Assert.IsType<CarrierLocation>(shared).SameTickRefAs(carrier.ValueSlot));
        Assert.Empty(witnesses);

        Assert.Same(slot, Evaluate(slot,
            [new(slot, carrier.ValueSlot), new(slot, new CarrierLocation(carrier, 3))], witnesses));
        Assert.Single(witnesses);
    }

    [Fact]
    public void CyclicBindingsTerminateAsVisibleOpenParticipation()
    {
        var first = new Placeholder();
        var second = new Placeholder();
        var bindings = new[] { new Relation(first, second), new Relation(second, first) };
        var witnesses = new List<Relation>();

        Assert.IsType<Placeholder>(Evaluate(first, bindings, witnesses));

        var witness = Assert.Single(witnesses);
        Assert.NotEmpty(F(F(witness)[3]));
        Assert.Equal(2, F(F(witness)[1]).Length);
    }

    [Fact]
    public void SharedExpressionIsReadOnceAndItsActualOutputFeedsBothInputs()
    {
        var chart = new Chart();
        var shared = Interpolate(chart.Basis, chart.Point(0, 0), chart.Point(6, 4),
            new CarrierLocation(chart.Grade, 1), 2, chart.Grade);
        var expression = Interpolate(chart.Basis, shared, shared,
            new CarrierLocation(chart.Grade, 1), 2, chart.Grade);
        var witnesses = new List<Relation>();

        var result = Evaluate(expression, [], witnesses);

        Coordinate(result, 0, chart.X, 3);
        Coordinate(result, 1, chart.Y, 2);
        Assert.Equal(2, witnesses.Count);
        var request = Assert.IsType<Relation>(F(witnesses[1])[0]);
        var inputs = F(request.Left);
        Assert.Same(inputs[0], inputs[1]);
        Assert.Same(F(witnesses[0])[2], inputs[0]);
    }

    [Fact]
    public void EvaluationDoesNotMoveSourceSitesOrCarrierState()
    {
        var chart = new Chart();
        chart.X.Add(7);
        chart.Y.Remove(3);
        chart.Grade.Add(1);
        var first = chart.Point(2, 5);
        var last = chart.Point(8, 9);
        var firstX = Assert.IsType<CarrierLocation>(F(first)[0]);
        var expression = Interpolate(chart.Basis, first, last, chart.Grade.ValueSlot, 2, chart.Grade);

        var result = Evaluate(expression, [], []);

        Coordinate(result, 0, chart.X, 5);
        Assert.Equal(2, firstX.Ticks);
        Assert.True(firstX.SameTickRefAs(Assert.IsType<CarrierLocation>(F(first)[0])));
        Assert.Equal(7UL, chart.X.Value);
        Assert.Equal(0L, chart.X.Tension);
        Assert.Equal(0UL, chart.Y.Value);
        Assert.Equal(-3L, chart.Y.Tension);
        Assert.Equal(1UL, chart.Grade.Value);
    }

    [Theory]
    [InlineData(0, 0, 4)]
    [InlineData(1, 1, 4)]
    public void ZeroFrameOrUnsupportedSampleDoesNotManufactureCoordinates(long denominator, long lower, long upper)
    {
        var chart = new Chart();
        var expression = Interpolate(chart.Basis, chart.Point(0, 0), chart.Point(2, 3),
            new CarrierLocation(chart.Grade, 5), denominator, chart.Grade, lower, upper);
        var witnesses = new List<Relation>();

        Assert.IsType<Placeholder>(Evaluate(expression, [], witnesses));
        Assert.NotEmpty(F(F(Assert.Single(witnesses))[3]));
    }

    [Fact]
    public void CompatibleButDifferentParameterCarrierDoesNotSupplyTheSelectedGrade()
    {
        var chart = new Chart();
        var foreign = new Carrier(chart.Grade.UnitShape);
        var expression = Interpolate(chart.Basis, chart.Point(0, 0), chart.Point(2, 3),
            new CarrierLocation(foreign, 1), 2, chart.Grade);
        var witnesses = new List<Relation>();

        Assert.IsType<Placeholder>(Evaluate(expression, [], witnesses));
        Assert.NotEmpty(F(F(Assert.Single(witnesses))[3]));
    }

    [Fact]
    public void UnrelatedContainingStructureDoesNotBecomeAnImplicitEvaluationProgram()
    {
        var chart = new Chart();
        var expression = Interpolate(chart.Basis, chart.Point(0, 0), chart.Point(2, 3),
            new CarrierLocation(chart.Grade, 1), 2, chart.Grade);
        var unrelated = G(expression);
        var witnesses = new List<Relation>();

        Assert.Same(unrelated, Evaluate(unrelated, [], witnesses));
        Assert.Empty(witnesses);
    }

    [Fact]
    public void ProjectionUsesItsConnectedSignedCalibrationAndBindsOpenCoordinates()
    {
        var chart = new Chart();
        var missing = new Placeholder();
        var source = Relation.OrderedGroup(chart.Holder, missing, new CarrierLocation(chart.Y, 1));
        var calibration = G(
            new Relation(Relation.Segment(chart.X, 0, 2), Relation.Segment(chart.X, 10, 14)),
            new Relation(Relation.Segment(chart.Y, 0, 2), Relation.Segment(chart.Y, 4, 0)));
        var expression = Project(source, calibration, chart.Basis);
        var witnesses = new List<Relation>();

        var result = Evaluate(expression, [new(missing, new CarrierLocation(chart.X, 1))], witnesses);

        Coordinate(result, 0, chart.X, 12);
        Coordinate(result, 1, chart.Y, 2);
        Assert.Single(witnesses);
        Assert.Same(missing, F(source)[0]);
    }

    [Fact]
    public void AdditionIsAComposedExactReadInTheSelectedAffineOrigin()
    {
        var chart = new Chart();
        var expression = Add(chart.Basis, chart.Point(2, 3), chart.Point(5, 8), chart.Point(-4, 2), chart.Grade);
        var witnesses = new List<Relation>();

        var result = Evaluate(expression, [], witnesses);

        Coordinate(result, 0, chart.X, -1);
        Coordinate(result, 1, chart.Y, 7);
        Assert.Equal(2, witnesses.Count);
    }

    [Fact]
    public void RestoredMachineKeepsParameterBindingsAndReevaluatesItsOwnLiveValue()
    {
        var chart = new Chart();
        chart.Grade.Add(2);
        var slot = new Placeholder();
        var expression = Interpolate(chart.Basis, chart.Point(0, 0), chart.Point(12, 6), slot, 3, chart.Grade);
        var root = G(expression, G(new Relation(slot, chart.Grade.ValueSlot)), chart.Grade, chart.Basis);
        var restored = F(GojoNumericFormat.Decode(GojoNumericFormat.Encode(root))[0]);
        var loadedGrade = Assert.IsType<Carrier>(restored[2]);
        var bindings = F(restored[1]).Cast<Relation>().ToArray();
        var loadedAxes = F(restored[3]).Cast<Relation>().Select(span =>
            Assert.IsType<CarrierLocation>(Assert.IsType<Relation>(span.Left).Left).Carrier).ToArray();
        var before = Evaluate(restored[0], bindings, []);

        loadedGrade.Remove(1);
        var after = Evaluate(restored[0], bindings, []);

        Assert.False(chart.Grade.SameIdentityAs(loadedGrade));
        Assert.True(Assert.IsType<CarrierLocation>(bindings[0].Right).SameTickRefAs(loadedGrade.ValueSlot));
        Coordinate(before, 0, loadedAxes[0], 8);
        Coordinate(before, 1, loadedAxes[1], 4);
        Coordinate(after, 0, loadedAxes[0], 4);
        Coordinate(after, 1, loadedAxes[1], 2);
        Assert.Equal(2UL, chart.Grade.Value);
        Assert.Equal(1UL, loadedGrade.Value);
    }

    private static void Coordinate(IElement vector, int index, Carrier expectedBasis, long numerator, long denominator = 1)
    {
        Assert.True(ProjectionResidue.TryRead(F(vector)[index], out var basis, out var n, out var d));
        Assert.True(expectedBasis.SameIdentityAs(basis));
        Assert.Equal(numerator, n);
        Assert.Equal(denominator, d);
    }

    private sealed class Chart
    {
        public Carrier X { get; } = new();
        public Carrier Y { get; } = new();
        public Carrier Grade { get; } = new();
        public Carrier Holder { get; } = new();
        public Relation Basis => Relation.OrderedGroup(Holder, Relation.Segment(X, 0, 1), Relation.Segment(Y, 0, 1));
        public Relation Point(long x, long y) => Relation.OrderedGroup(Holder,
            new CarrierLocation(X, x), new CarrierLocation(Y, y));
    }
}
