using System.Numerics;
using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeFrieze;

namespace Gojo.Experiments.QualitativeExecution.Tests;

public sealed class DiagonalRibbonSupplierTests
{
    [Fact]
    public void DiagonalSquareSweepHasSixOrderedVerticesAndKeepsActualSourceAndSums()
    {
        var f = Diagonal(); var read = Evaluate(f, 2);
        Assert.Empty(E(read.Unresolved)); Assert.Equal(2, E(read.Polygons).Length); AssertNoNumbers(read.Source);
        var polygon = Assert.IsType<Relation>(E(read.Polygons)[0]);
        Assert.True(DiagonalRibbonSupplier.TryReadPolygon(polygon, out var source, out _, out var vertices, out var edges));
        Assert.Equal(new[] { (-1L, -1L), (1L, -1L), (5L, 3L), (5L, 5L), (3L, 5L), (-1L, 1L) }, E(vertices).Cast<Relation>().Select(Whole));
        Assert.Equal(6, E(edges).Length); Assert.True(Contains(read.Geometry, source)); Assert.True(Contains(polygon, source));
        var body = Assert.IsType<Relation>(polygon.Right); var evidence = Assert.IsType<Relation>(body.Right);
        Assert.Equal(8, E(Assert.IsType<Relation>(evidence.Right)).Length);
        Assert.True(Contains(read.Surface, f.Surface)); Assert.True(Contains(read.Surface, read.Trace)); Assert.True(Contains(read.Surface, read.Calibration));
    }

    [Fact]
    public void OddWidthRetainsExactHalfTicksInsteadOfRoundingTheNib()
    {
        var read = Evaluate(Diagonal(), 1); Assert.Empty(E(read.Unresolved));
        var polygon = Assert.IsType<Relation>(E(read.Polygons)[0]);
        Assert.True(DiagonalRibbonSupplier.TryReadPolygon(polygon, out _, out _, out var vertices, out _));
        Assert.True(DiagonalRibbonSupplier.TryReadPoint(Assert.IsType<Relation>(E(vertices)[0]), out var xn, out var xd, out var yn, out var yd));
        Assert.Equal((-1L, 2L, -1L, 2L), (xn, xd, yn, yd));
        Assert.All(E(vertices).Cast<Relation>(), point =>
        { Assert.True(DiagonalRibbonSupplier.TryReadPoint(point, out _, out var x, out _, out var y)); Assert.Equal(2, x); Assert.Equal(2, y); });
    }

    [Fact]
    public void AxisAlignedAndAllFourDiagonalOrientationsUseTheSameSweepLaw()
    {
        var frame = new QualitativeMotionFrame(); var h = new Placeholder(); var v = new Placeholder();
        var fixture = FriezeFixture.Sequence(frame, S(
            S(M(frame.Right, h), M(frame.Up, v)), S(M(frame.Left, h), M(frame.Up, v)),
            S(M(frame.Left, h), M(frame.Down, v)), S(M(frame.Right, h), M(frame.Down, v)),
            S(M(frame.Right, h)), S(M(frame.Right, h))));
        var read = Evaluate(fixture, 2, rounds: 6);
        Assert.Empty(E(read.Unresolved)); Assert.Equal(6, E(read.Polygons).Length);
        var lengths = E(read.Polygons).Cast<Relation>().Select(polygon =>
        { Assert.True(DiagonalRibbonSupplier.TryReadPolygon(polygon, out _, out _, out var vertices, out _)); return E(vertices).Length; }).ToArray();
        Assert.Equal(new[] { 6, 6, 6, 6, 4, 4 }, lengths);
    }

    [Fact]
    public void NativeNibWidthIsAxisAlignedAndDoesNotPretendToBePerpendicularEuclideanWidth()
    {
        var read = Evaluate(Diagonal(), 2); var polygon = Assert.IsType<Relation>(E(read.Polygons)[0]);
        Assert.True(DiagonalRibbonSupplier.TryReadPolygon(polygon, out _, out _, out var vertices, out _));
        var points = E(vertices).Cast<Relation>().Select(Whole).ToArray();
        BigInteger twiceArea = 0;
        for (var i = 0; i < points.Length; i++)
            twiceArea += (BigInteger)points[i].X * points[(i + 1) % points.Length].Y - (BigInteger)points[i].Y * points[(i + 1) % points.Length].X;
        // Square area 2*2 plus segment L1 reach (4+4) times width 2.
        Assert.Equal(new BigInteger(40), twiceArea);
        Assert.True(Contains(polygon, read.Nib));
    }

    [Fact]
    public void AZeroLengthSampleIsAnExplicitSquareNibDabUnderThisSelectedLaw()
    {
        var frame = new QualitativeMotionFrame(); var h = new Placeholder();
        var fixture = FriezeFixture.Sequence(frame, S(S(), S(M(frame.Right, h))));
        var read = Evaluate(fixture, 2); Assert.Empty(E(read.Unresolved));
        var polygon = Assert.IsType<Relation>(E(read.Polygons)[0]);
        Assert.True(DiagonalRibbonSupplier.TryReadPolygon(polygon, out _, out _, out var vertices, out _));
        Assert.Equal(new[] { (-1L, -1L), (1L, -1L), (1L, 1L), (-1L, 1L) }, E(vertices).Cast<Relation>().Select(Whole));
    }

    [Fact]
    public void ThickeningKeepsTheSourceTraceAndCalibrationUnchanged()
    {
        var fixture = Diagonal(); var supplier = new DiagonalRibbonSupplier(fixture, new Placeholder());
        var trace = fixture.Run(2); var calibration = Calibration(fixture); var source = supplier.Surface;
        var thin = supplier.Interpret(trace, calibration, 1); var thick = supplier.Interpret(trace, calibration, 5);
        Assert.Empty(E(thin.Unresolved)); Assert.Empty(E(thick.Unresolved));
        Assert.Same(source, thin.Source); Assert.Same(source, thick.Source); Assert.Same(trace, thick.Trace); Assert.Same(calibration, thick.Calibration);
        AssertNoNumbers(source); Assert.True(Contains(thick.Surface, trace)); Assert.True(Contains(thick.Surface, calibration));
    }

    [Fact]
    public void PeriodicCopiesRetainActualPeriodAndSignedRepetition()
    {
        var read = Evaluate(Diagonal(), 2); var polygon = Assert.IsType<Relation>(E(read.Polygons)[0]);
        Assert.True(DiagonalRibbonSupplier.TryReadPolygon(polygon, out var source, out _, out var vertices, out _));
        var before = E(vertices).Cast<Relation>().Select(Whole).ToArray();
        foreach (var k in new[] { -2L, 0L, 3L })
        {
            Assert.True(DiagonalRibbonSupplier.TryTranslatePolygon(polygon, k, out var copy));
            Assert.True(DiagonalRibbonSupplier.TryReadPolygon(copy, out var copiedSource, out var translation, out var points, out _));
            Assert.Same(source, copiedSource); Assert.True(Contains(translation, read.Period));
            Assert.Equal(before.Select(point => (point.X + k * 8, point.Y)), E(points).Cast<Relation>().Select(Whole));
        }
    }

    [Fact]
    public void CopyIndicesReferToTheOriginalStrokeRatherThanAccumulatingHiddenTranslations()
    {
        var read = Evaluate(Diagonal(), 1); var polygon = Assert.IsType<Relation>(E(read.Polygons)[0]);
        Assert.True(DiagonalRibbonSupplier.TryTranslatePolygon(polygon, 2, out var second));
        Assert.True(DiagonalRibbonSupplier.TryTranslatePolygon(second, -1, out var back));
        Assert.True(DiagonalRibbonSupplier.TryReadPolygon(back, out _, out _, out var vertices, out _));
        Assert.True(DiagonalRibbonSupplier.TryReadPoint(Assert.IsType<Relation>(E(vertices)[0]), out var xn, out var xd, out _, out _));
        Assert.Equal((-17L, 2L), (xn, xd));
    }

    [Fact]
    public void WideIntegerOrientationProductsDoNotWrap()
    {
        var fixture = Diagonal(); var read = Evaluate(fixture, 2, length: 3_000_000_000);
        Assert.Empty(E(read.Unresolved));
        Assert.All(E(read.Polygons).Cast<Relation>(), polygon =>
            Assert.True(DiagonalRibbonSupplier.TryReadPolygon(polygon, out _, out _, out _, out _)));
    }

    [Theory]
    [InlineData(0, 8192)]
    [InlineData(-2, 8192)]
    [InlineData(2, 0)]
    [InlineData(2, 1)]
    public void InvalidWidthOrUnavailableBudgetRemainNativeObligations(long width, int budget)
    {
        var fixture = Diagonal(); var supplier = new DiagonalRibbonSupplier(fixture, new Placeholder());
        var read = supplier.Interpret(fixture.Run(2), Calibration(fixture), width, budget);
        Assert.NotEmpty(E(read.Unresolved)); Assert.Empty(E(read.Polygons));
        Assert.True(supplier.TryReadResult(read.Surface, out _, out _, out _, out _, out _, out var pending)); Assert.NotEmpty(E(pending));
    }

    [Fact]
    public void AnUnclosedPhaseCycleDoesNotInventAPeriodicFootprint()
    {
        var fixture = Diagonal(); var read = Evaluate(fixture, 2, rounds: 1);
        Assert.NotEmpty(E(read.Unresolved)); Assert.Empty(E(read.Polygons)); Assert.True(Contains(read.Surface, read.Trace));
    }

    [Fact]
    public void OpenSampleScopeAndFractionalCalibrationAreRetainedAsUnknown()
    {
        var fixture = Diagonal(); var supplier = new DiagonalRibbonSupplier(fixture, new Placeholder()); var trace = fixture.Run(2);
        Assert.True(fixture.TryReadTrace(trace, out _, out var final, out var steps, out var moves, out var samples, out var pending));
        var open = new Relation(trace.Left, new Relation(final, new Relation(steps,
            new Relation(moves, new Relation(StructuralConstraint.Open(E(samples)), pending)))));
        var openRead = supplier.Interpret(open, Calibration(fixture), 2);
        Assert.Empty(E(openRead.Polygons)); Assert.NotEmpty(E(openRead.Unresolved)); Assert.True(Contains(openRead.Surface, open));
        var calibration = Calibration(fixture); var mapping = Assert.IsType<Relation>(calibration.Right); var rows = E(mapping);
        var original = Assert.IsType<Relation>(rows[0]); var span = Assert.IsType<Relation>(original.Right);
        var carrier = Assert.IsType<Carrier>(Assert.IsType<Relation>(span.Left).Right);
        var fractional = new Relation(new Relation(new CarrierLocation(carrier, 1), Relation.Point(carrier, 2)), carrier);
        rows[0] = new Relation(original.Left, new Relation(span.Left, fractional));
        var invalid = new Relation(calibration.Left, S(rows)); var fractionRead = supplier.Interpret(trace, invalid, 2);
        Assert.Empty(E(fractionRead.Polygons)); Assert.NotEmpty(E(fractionRead.Unresolved)); Assert.True(Contains(fractionRead.Surface, invalid));
    }

    [Fact]
    public void NumericRangeFailureAndUnrepresentableCopiesDoNotWrapCoordinates()
    {
        var read = Evaluate(Diagonal(), 2); var polygon = Assert.IsType<Relation>(E(read.Polygons)[0]);
        Assert.False(DiagonalRibbonSupplier.TryTranslatePolygon(polygon, long.MaxValue, out _));
        var enormous = Evaluate(Diagonal(), 3, length: long.MaxValue / 2);
        Assert.NotEmpty(E(enormous.Unresolved));
    }

    [Fact]
    public void PolygonReaderRejectsMissingSumsWrongEdgesAndClonedSourceWitnesses()
    {
        var read = Evaluate(Diagonal(), 2); var polygon = Assert.IsType<Relation>(E(read.Polygons)[0]);
        var shape = Assert.IsType<Relation>(polygon.Right); var evidence = Assert.IsType<Relation>(shape.Right);
        var noSums = new Relation(polygon.Left, new Relation(shape.Left, new Relation(evidence.Left, S())));
        Assert.False(DiagonalRibbonSupplier.TryReadPolygon(noSums, out _, out _, out _, out _));
        var edges = E(Assert.IsType<Relation>(evidence.Left)); var edge = Assert.IsType<Relation>(edges[0]);
        edges[0] = new Relation(edge.Right, edge.Left);
        var reversed = new Relation(polygon.Left, new Relation(shape.Left, new Relation(S(edges), evidence.Right)));
        Assert.False(DiagonalRibbonSupplier.TryReadPolygon(reversed, out _, out _, out _, out _));
        var context = Assert.IsType<Relation>(polygon.Left); var provenance = Assert.IsType<Relation>(context.Left);
        var stroke = Assert.IsType<Relation>(provenance.Left); var ordered = Assert.IsType<Relation>(stroke.Right);
        var clonedPoint = new Relation(ordered.Left, ordered.Right);
        var cloned = new Relation(new Relation(new Relation(new Relation(stroke.Left, clonedPoint), provenance.Right), context.Right), polygon.Right);
        // Same endpoint identities and independently authored wrapper are a
        // valid stroke surface; replacing endpoint identity is not equivalent.
        Assert.True(DiagonalRibbonSupplier.TryReadPolygon(cloned, out _, out _, out _, out _));
        var first = Assert.IsType<Relation>(ordered.Left); var copiedFirst = new Relation(first.Left, first.Right);
        var replaced = new Relation(new Relation(new Relation(new Relation(stroke.Left, new Relation(copiedFirst, ordered.Right)), provenance.Right), context.Right), polygon.Right);
        Assert.False(DiagonalRibbonSupplier.TryReadPolygon(replaced, out _, out _, out _, out _));
    }

    [Fact]
    public void InterpretationReaderRejectsOmittedPolygonsAndOtherSupplierFrames()
    {
        var fixture = Diagonal(); var supplier = new DiagonalRibbonSupplier(fixture, new Placeholder());
        var read = supplier.Interpret(fixture.Run(2), Calibration(fixture), 2);
        Assert.True(supplier.TryReadResult(read.Surface, out var trace, out var calibration, out var nib, out var period, out var polygons, out var pending));
        Assert.Same(read.Trace, trace); Assert.Same(read.Calibration, calibration); Assert.Same(read.Nib, nib); Assert.Same(read.Period, period);
        Assert.Same(read.Polygons, polygons); Assert.Empty(E(pending));
        var omitted = new Relation(read.Surface.Left, new Relation(period, new Relation(S(), S())));
        Assert.False(supplier.TryReadResult(omitted, out _, out _, out _, out _, out _, out _));
        var duplicate = new Relation(read.Surface.Left, new Relation(period, new Relation(S(E(polygons)[0], E(polygons)[0]), S())));
        Assert.False(supplier.TryReadResult(duplicate, out _, out _, out _, out _, out _, out _));
        Assert.False(new DiagonalRibbonSupplier(fixture, new Placeholder()).TryReadResult(read.Surface, out _, out _, out _, out _, out _, out _));
    }

    [Fact]
    public void InterpretationReaderBindsTraceCalibrationNibExtentAndActualGeometryPeriod()
    {
        var fixture = Diagonal(); var supplier = new DiagonalRibbonSupplier(fixture, new Placeholder());
        var read = supplier.Interpret(fixture.Run(2), Calibration(fixture), 2);
        var header = Assert.IsType<Relation>(read.Surface.Left); var request = Assert.IsType<Relation>(header.Left);
        var detail = Assert.IsType<Relation>(header.Right);
        foreach (var changed in new[] { new Relation(new Relation(S(), read.Calibration), detail),
            new Relation(new Relation(read.Trace, S()), detail) })
        {
            var alteredRequest = new Relation(request.Left, changed.Left);
            var altered = new Relation(new Relation(alteredRequest, changed.Right), read.Surface.Right);
            Assert.False(supplier.TryReadResult(altered, out _, out _, out _, out _, out _, out _));
        }
        var other = new DiagonalRibbonSupplier(Diagonal(), new Placeholder());
        var otherRead = other.Interpret(other.Fixture.Run(2), Calibration(other.Fixture), 2);
        var spliced = new Relation(new Relation(request, Assert.IsType<Relation>(otherRead.Surface.Left).Right), otherRead.Surface.Right);
        Assert.False(supplier.TryReadResult(spliced, out _, out _, out _, out _, out _, out _));
        var periodClone = new Relation(read.Period.Left, read.Period.Right);
        var periodSwap = new Relation(read.Surface.Left, new Relation(periodClone, Assert.IsType<Relation>(read.Surface.Right).Right));
        Assert.False(supplier.TryReadResult(periodSwap, out _, out _, out _, out _, out _, out _));
        var declared = Assert.IsType<Relation>(read.Nib.Left);
        var differentExtentNib = new Relation(new Relation(new Placeholder(), declared.Right), read.Nib.Right);
        var polygons = E(read.Polygons).Cast<Relation>().Select(polygon =>
            (IElement)new Relation(new Relation(Assert.IsType<Relation>(polygon.Left).Left, differentExtentNib), polygon.Right)).ToArray();
        var brushContext = Assert.IsType<Relation>(detail.Right);
        var extentSwap = new Relation(new Relation(request, new Relation(detail.Left,
                new Relation(differentExtentNib, brushContext.Right))),
            new Relation(read.Period, new Relation(S(polygons), read.Unresolved)));
        Assert.False(supplier.TryReadResult(extentSwap, out _, out _, out _, out _, out _, out _));
    }

    private static DiagonalRibbonSupplier.Evaluation Evaluate(FriezeFixture fixture, long width, int rounds = 2, long length = 4) =>
        new DiagonalRibbonSupplier(fixture, new Placeholder()).Interpret(fixture.Run(rounds), Calibration(fixture, length), width);
    private static FriezeFixture Diagonal()
    {
        var frame = new QualitativeMotionFrame(); var h = new Placeholder(); var v = new Placeholder();
        return FriezeFixture.Sequence(frame, S(S(M(frame.Right, h), M(frame.Up, v)), S(M(frame.Right, h), M(frame.Down, v))));
    }
    private static Relation Calibration(FriezeFixture fixture, long length = 4) =>
        FriezeRealization.CreateCalibration(fixture, FriezeRealization.DefaultLengths(fixture, length, length));
    private static (long X, long Y) Whole(Relation point)
    {
        Assert.True(DiagonalRibbonSupplier.TryReadPoint(point, out var x, out var xd, out var y, out var yd));
        Assert.Equal(1, xd); Assert.Equal(1, yd); return (x, y);
    }
    private static Relation M(IElement direction, IElement reach) => QualitativeMotionFrame.Move(direction, reach);
    private static Relation S(params IElement[] values) => StructuralConstraint.Closed(values);
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
    private static bool Contains(IElement root, IElement target) => Traverse(root).Any(value => ReferenceEquals(value, target));
    private static void AssertNoNumbers(IElement root) => Assert.DoesNotContain(Traverse(root), value => value is Carrier or CarrierLocation);
    private static IEnumerable<IElement> Traverse(IElement root)
    {
        var seen = new HashSet<IElement>(ReferenceEqualityComparer.Instance); var pending = new Stack<IElement>(); pending.Push(root);
        while (pending.TryPop(out var value))
        {
            if (!seen.Add(value)) continue; yield return value;
            if (value is Relation relation) { pending.Push(relation.Left); pending.Push(relation.Right); }
        }
    }
}
