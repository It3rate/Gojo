using System.Numerics;
using Gojo.Core;
using Gojo.Encoding.Syntax;
using Gojo.Patterns;
using Gojo.Structures;

namespace Gojo.Encoding.Tests;

public sealed class GojoSyntaxBoundedQuantityTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void AuthoredSamplesDistinguishInstanceIdentityAndDescriptionReuse(int restores)
    {
        var fields = Fields(Parse(restores)[0]);
        var axis = Assert.IsType<Carrier>(fields[4]);
        var expected = new[] { (90L,110L), (0L,0L), (-20L,20L), (180L,220L) };
        for (var i = 0; i < expected.Length; i++)
        {
            Assert.True(BoundedQuantity.TryObserve(R(fields[i]), out var witness));
            Bounds(witness, axis, expected[i].Item1, expected[i].Item2);
        }
        Assert.Same(Fields(fields[5])[2], Fields(fields[6])[2]);
        Assert.NotEqual(Fields(fields[5])[0], Fields(fields[6])[0]);
        var bound = R(StructuralPattern.Apply(fields[0], R(fields[7])));
        Assert.True(BoundedQuantity.TryObserve(bound, out var exact));
        Bounds(exact, axis, 100, 100);
        Assert.True(BoundedQuantity.TryObserve(R(fields[0]), out var original));
        Bounds(original, axis, 90, 110);
        Assert.Equal(0UL, axis.Value); Assert.Equal(0, axis.Tension);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void AuthoredObservationEventsRetainTheirSeparateNoise(int restores)
    {
        var fields = Fields(Parse(restores)[1]);
        Assert.True(BoundedQuantity.TryObserve(R(fields[0]), out var first));
        Assert.True(BoundedQuantity.TryReadBounds(first, out var axis, out _, out _, out _));
        Bounds(first, axis, 84, 116);
        Assert.True(BoundedQuantity.TryCombine(R(fields[2]), out var difference));
        Bounds(difference, axis, -2, 2);
        Assert.True(BoundedQuantity.TryCombine(R(fields[3]), out var repeated));
        Bounds(repeated, axis, 0, 0);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void OpenCalibrationContradictionsAndZeroDomainsRemainInspectable(int restores)
    {
        var fields = Fields(Parse(restores)[2]);
        Assert.False(BoundedQuantity.TryObserve(R(fields[0]), out var open));
        Open(open);
        var bound = R(StructuralPattern.Apply(fields[0], R(fields[1])));
        Assert.True(BoundedQuantity.TryObserve(bound, out var value));
        Assert.True(BoundedQuantity.TryReadBounds(value, out var axis, out _, out _, out _));
        Bounds(value, axis, 90, 110);
        Assert.False(BoundedQuantity.TryObserve(R(fields[2]), out var contradiction));
        Open(contradiction);
        Assert.False(BoundedQuantity.TryDivide(R(fields[3]), out var domain));
        Open(domain);
        Assert.True(BoundedQuantity.TryDivide(R(fields[4]), out var identity));
        Bounds(identity, Assert.IsType<Carrier>(Fields(fields[4])[2]), 1, 1);
        Assert.True(BoundedQuantity.TryMultiply(R(fields[5]), out var square));
        Assert.True(BoundedQuantity.TryReadBounds(square, out var area, out _, out _, out _));
        Bounds(square, area, 0, 1);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void ClearanceSpecificationAndNominalCatalogueHaveDifferentReads(int restores)
    {
        var fields = Fields(Parse(restores)[3]);
        var fine = Assert.IsType<Carrier>(fields[4]);
        Assert.True(BoundedQuantity.TryObserve(R(fields[0]), out var clearance));
        Bounds(clearance, fine, 10, 30);
        Assert.True(BoundedQuantity.TryObserveConformance(G(clearance, fields[1], new Placeholder()), out var accepted));
        Bounds(accepted, fine, 0, 0);
        Assert.True(BoundedQuantity.TryObserveConformance(G(clearance, fields[2], new Placeholder()), out var undecided));
        Bounds(undecided, fine, 0, 5);
        Assert.False(BoundedQuantity.TryObserve(R(fields[3]), out _));

        var mm = Assert.IsType<Carrier>(fields[5]);
        var conversion = new Relation(G(new Relation(fields[0], fields[6])), Relation.Point(mm, 1));
        Assert.True(BoundedQuantity.TryCombine(conversion, out var millimetres));
        Bounds(millimetres, mm, 1, 10, 3, 10);
        Assert.True(BoundedQuantity.TryReadBounds(millimetres, out _, out _, out _, out var enclosure));
        Assert.Equal(Relation.Segment(mm, 0, 1), enclosure);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void CoarseCommunicationFocusRetainsKnownFineInformationWithoutCallingItError(int restores)
    {
        var fields = Fields(Parse(restores)[4]);
        var distance = Assert.IsType<CarrierLocation>(fields[2]);
        var coarse = Assert.IsType<Carrier>(fields[3]);
        Assert.True(R(fields[1]).TryProject(distance, coarse, out var read));
        Assert.True(read.TryReadProjection(out var published, out var remainder));
        Assert.Equal(1, published.Ticks);
        Scalar(remainder, distance.Carrier, 13, 1);
        var focus = R(StructuralPattern.Apply(fields[0],
            new Relation(fields[4], published), new Relation(fields[5], remainder)));
        Assert.True(FocusSurface.TryRead(focus, out var source, out var basis, out var selected,
            out var complement, out _, out _, out var retained, out _));
        Assert.Equal(distance, source); Assert.Equal(fields[1], basis); Assert.Equal(published, selected);
        Assert.Equal(distance, Assert.Single(Fields(complement)));
        Scalar(retained, distance.Carrier, 13, 1);
        Assert.Equal(113, published.Ticks * 100 + Assert.IsType<CarrierLocation>(retained).Ticks);
        Assert.Equal(0UL, coarse.Value); Assert.Equal(0, distance.Carrier.Tension);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void ADesiredFiveRequiresAnExplicitConservedChangeToTheCurrentTwoPlusTwo(int restores)
    {
        var fields = Fields(Parse(restores)[5]);
        var a = Assert.IsType<Carrier>(fields[0]); var b = Assert.IsType<Carrier>(fields[1]);
        var reserve = Assert.IsType<Carrier>(fields[2]); var basis = Assert.IsType<Carrier>(fields[3]);
        a.Add(2); b.Add(2); reserve.Add(1);
        var initialTotal = a.Value + b.Value + reserve.Value;

        Relation ObserveCurrent()
        {
            // Capture the exposed state for this event; don't retain a live
            // ValueSlot as if it were an immutable earlier observation.
            var expression = R(StructuralPattern.Apply(fields[4],
                new Relation(fields[8], new CarrierLocation(a, a.Value)),
                new Relation(fields[9], new CarrierLocation(b, b.Value))));
            Assert.True(BoundedQuantity.TryObserve(expression, out var witness));
            return witness;
        }

        var before = ObserveCurrent();
        Bounds(before, basis, 4, 4);
        Assert.True(BoundedQuantity.TryObserveConformance(G(before, fields[5], new Placeholder()), out var gap));
        Bounds(gap, basis, 1, 1);
        Assert.Equal(2UL, a.Value); Assert.Equal(2UL, b.Value);

        Assert.True(RefinementTransfer.TryApply(R(fields[6]), out var transfer));
        Assert.Equal(3UL, a.Value); Assert.Equal(2UL, b.Value); Assert.Equal(0UL, reserve.Value);
        Assert.Equal(initialTotal, a.Value + b.Value + reserve.Value);
        var after = ObserveCurrent();
        Bounds(after, basis, 5, 5);
        Bounds(before, basis, 4, 4);
        Assert.True(BoundedQuantity.TryObserveConformance(G(after, fields[5], new Placeholder()), out var satisfied));
        Bounds(satisfied, basis, 0, 0);
        var timeline = R(StructuralPattern.Apply(fields[7], new Relation(fields[10], before),
            new Relation(fields[11], after), new Relation(fields[12], satisfied), new Relation(fields[13], transfer)));

        // Completed action evidence survives independently of the original live world.
        var restoredTimeline = R(GojoNumericFormat.Decode(GojoNumericFormat.Encode(timeline))[0]);
        Assert.True(TimeStepSequence.TryRead(restoredTimeline, out _, out var initial, out var steps));
        Assert.True(BoundedQuantity.TryReadBounds(R(initial), out var restoredBasis, out _, out _, out _));
        Bounds(R(initial), restoredBasis, 4, 4);
        Assert.True(TimeStepSequence.TryReadStep(Assert.Single(steps), out _, out _, out _,
            out var stepBefore, out var transition, out var stepAfter, out var remaining));
        Bounds(R(stepBefore), restoredBasis, 4, 4); Bounds(R(stepAfter), restoredBasis, 5, 5);
        Bounds(R(remaining), restoredBasis, 0, 0);
        Assert.Equal(2, Fields(transition).Length);
        Assert.True(ReadWitness.TryRead(R(Fields(transition)[1]), out _, out _, out var moved, out _, out _));
        Assert.Equal(-1, Assert.IsType<CarrierLocation>(R(moved).Left).Ticks);
        Assert.Equal(1, Assert.IsType<CarrierLocation>(R(moved).Right).Ticks);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void RepeatedRoutesRetainOneObservationAfterComputedWitnessRestoration(int restores)
    {
        var fields = Fields(Parse(restores)[6]);
        Assert.True(BoundedQuantity.TryCombine(R(fields[0]), out var average));
        var packet = new IElement[] { average, fields[1], fields[2], fields[3] };
        for (var i = 0; i < 2; i++) packet = GojoNumericFormat.Decode(GojoNumericFormat.Encode(packet));
        average = R(packet[0]);
        Assert.True(BoundedQuantity.TryReadBounds(average, out var axis, out _, out _, out _));
        Bounds(average, axis, 84, 116);
        Assert.True(ReadWitness.TryRead(average, out _, out var normalized, out _, out _, out _));
        var comparison = new Relation(G(new Relation(normalized, packet[2]),
            new Relation(packet[1], packet[3])), Relation.Point(axis, 1));
        Assert.True(BoundedQuantity.TryCombine(comparison, out var difference));
        Bounds(difference, axis, 0, 0);
    }

    private static IElement[] Parse(int restores)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Fixtures", "Syntax", "bounded-quantities.gojo");
        var roots = GojoSyntax.Parse(File.ReadAllText(path)).Roots.ToArray();
        for (var i = 0; i < restores; i++) roots = GojoNumericFormat.Decode(GojoNumericFormat.Encode(roots));
        return roots;
    }
    private static Relation R(IElement element) => Assert.IsType<Relation>(element);
    private static Relation G(params IElement[] fields) => Relation.OrderedGroup(new Carrier(), fields);
    private static IElement[] Fields(IElement element)
    {
        Assert.True(R(element).TryReadOrderedGroup(out var fields));
        return fields;
    }
    private static void Bounds(Relation witness, Carrier basis, long lo, long hi) => Bounds(witness, basis, lo, 1, hi, 1);
    private static void Bounds(Relation witness, Carrier basis, long loN, long loD, long hiN, long hiD)
    {
        Assert.True(BoundedQuantity.TryReadBounds(witness, out var actualBasis, out var lo, out var hi, out _));
        Assert.Same(basis, actualBasis); Scalar(lo, basis, loN, loD); Scalar(hi, basis, hiN, hiD);
    }
    private static void Scalar(IElement element, Carrier basis, long expectedN, long expectedD)
    {
        Assert.True(ProjectionResidue.TryRead(element, out var actualBasis, out var n, out var d));
        Assert.Same(basis, actualBasis); Assert.Equal((BigInteger)expectedN * d, (BigInteger)n * expectedD);
    }
    private static void Open(Relation witness)
    {
        Assert.True(ReadWitness.TryRead(witness, out _, out _, out var result, out var residue, out _));
        Assert.IsType<Placeholder>(result); Assert.IsType<Relation>(residue);
        Assert.False(BoundedQuantity.TryReadBounds(witness, out _, out _, out _, out _));
    }
}
