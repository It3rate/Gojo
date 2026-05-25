using Gojo.Core;

namespace Gojo.Tests.Core;

public sealed class OrdinaryMathFlowTests
{
    [Fact]
    public void BareCarrierAddsAndSubtractsAccountedValue()
    {
        var carrier = new Carrier();

        carrier.Add(17);
        carrier.Remove(5);

        Assert.Equal(12UL, carrier.Value);
        Assert.Equal(0, carrier.Tension);
    }

    [Fact]
    public void BareCarrierUnderflowRemainsNegativeTension()
    {
        var carrier = new Carrier();

        carrier.Add(5);
        carrier.Remove(12);

        Assert.Equal(0UL, carrier.Value);
        Assert.Equal(-7, carrier.Tension);
    }

    [Fact]
    public void WrapAndCarryPreserveResidueAndQuotientWithoutDecimalCollapse()
    {
        var residue = new Carrier();
        var whole = new Carrier();
        residue.Attach(new Relation(Point(residue, 10), Point(whole, 0)));

        residue.Add(234);

        Assert.Equal(4UL, residue.Value);
        Assert.Equal(23UL, whole.Value);
        Assert.Equal(0, residue.Tension);
    }

    [Fact]
    public void BinaryCarryUsesTheSamePointRelationShapeAsDecimalCarry()
    {
        var ones = new Carrier();
        var twos = new Carrier();
        var fours = new Carrier();
        var eights = new Carrier();
        ones.Attach(new Relation(Point(ones, 2), Point(twos, 0)));
        twos.Attach(new Relation(Point(twos, 2), Point(fours, 0)));
        fours.Attach(new Relation(Point(fours, 2), Point(eights, 0)));

        ones.Add(13);

        Assert.Equal(1UL, ones.Value);
        Assert.Equal(0UL, twos.Value);
        Assert.Equal(1UL, fours.Value);
        Assert.Equal(1UL, eights.Value);
    }

    [Fact]
    public void ClampAndWrapAreDifferentSettlementShapes()
    {
        var wrapped = new Carrier();
        var clamped = new Carrier();
        wrapped.Attach(new Relation(Point(wrapped, 10), Point(wrapped, 0)));
        clamped.Attach(new Relation(Point(clamped, 10), Point(clamped, 10)));

        wrapped.Add(23);
        clamped.Add(23);

        Assert.Equal(3UL, wrapped.Value);
        Assert.Equal(20, wrapped.Tension);
        Assert.Equal(10UL, clamped.Value);
        Assert.Equal(13, clamped.Tension);
    }

    [Fact]
    public void PartitionedCycleWrapsWithoutRadiusCalibration()
    {
        var phase = new Carrier();
        phase.Attach(new Relation(Point(phase, 12), Point(phase, 0)));

        phase.Add(27);

        Assert.Equal(3UL, phase.Value);
        Assert.Equal(24, phase.Tension);
    }

    [Fact]
    public void PartitionedCycleCanCountCrossingsThroughOrdinaryCarryRoute()
    {
        var phase = new Carrier();
        var cycles = new Carrier();
        phase.Attach(new Relation(Point(phase, 12), Point(cycles, 0)));

        phase.Add(27);

        Assert.Equal(3UL, phase.Value);
        Assert.Equal(0, phase.Tension);
        Assert.Equal(2UL, cycles.Value);
    }

    [Fact]
    public void PartitionedCyclesCompareByRelationRatioWithoutRadiusCalibration()
    {
        var smallCycle = new Carrier();
        var largeCycle = new Carrier();
        var ratio = new Relation(Point(smallCycle, 12), Point(largeCycle, 36));

        var projected = ratio.Project(new CarrierLocation(smallCycle, 5), largeCycle);

        Assert.Equal(
            new Relation(
                new CarrierLocation(largeCycle, 15),
                new CarrierLocation(smallCycle, 0)),
            projected);
    }

    [Fact]
    public void ExactRatioProjectionMultipliesWithoutMutatingCarriers()
    {
        var source = new Carrier();
        var target = new Carrier();
        var ratio = new Relation(Point(source, 2), Point(target, 3));

        var projected = ratio.Project(new CarrierLocation(source, 8), target);

        Assert.Equal(
            new Relation(
                new CarrierLocation(target, 12),
                new CarrierLocation(source, 0)),
            projected);
        Assert.Equal(0UL, source.Value);
        Assert.Equal(0UL, target.Value);
    }

    [Fact]
    public void InexactBackwardRatioProjectionDividesAndKeepsResidue()
    {
        var source = new Carrier();
        var target = new Carrier();
        var ratio = new Relation(Point(source, 2), Point(target, 3));

        var projected = ratio.Project(new CarrierLocation(target, 10), source);

        Assert.Equal(
            new Relation(
                new CarrierLocation(source, 6),
                new CarrierLocation(target, 2)),
            projected);
    }

    [Fact]
    public void NegativeRatioProjectionRepresentsSubtractionSignWithoutInverseCarrier()
    {
        var source = new Carrier();
        var target = new Carrier();
        var subtract = new Relation(Point(source, 1), Point(target, -1));

        var projected = subtract.Project(new CarrierLocation(source, 9), target);

        Assert.Equal(
            new Relation(
                new CarrierLocation(target, -9),
                new CarrierLocation(source, 0)),
            projected);
    }

    [Fact]
    public void OffsetSegmentFrameReadsWholeAndResidueFromItsFirstEndpoint()
    {
        var source = new Carrier();
        var unit = new Carrier();
        var frame = new Relation(Segment(source, 5, 15), Point(unit, 1));

        var projected = frame.Project(new CarrierLocation(source, 28), unit);

        Assert.Equal(
            new Relation(
                new CarrierLocation(unit, 2),
                new CarrierLocation(source, 3)),
            projected);
    }

    private static Relation Point(Carrier carrier, long ticks) =>
        new(new CarrierLocation(carrier, ticks), carrier);

    private static Relation Segment(Carrier carrier, long left, long right) =>
        new(Point(carrier, left), Point(carrier, right));
}
