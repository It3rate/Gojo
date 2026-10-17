using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeFrieze;

namespace Gojo.Experiments.QualitativeExecution.Tests;

public sealed class FriezeCoverageTests
{
    [Fact]
    public void MeasuredSupplierRetainsTheRealizationCalibrationSamplesAndOriginalTrace()
    {
        var fixture = FriezeFixture.SquareWave(false);
        var trace = fixture.Run(4);
        var calibration = FriezeRealization.CreateCalibration(fixture, FriezeRealization.DefaultLengths(fixture));
        Assert.True(FriezeRealization.TryRealize(fixture, trace, calibration, out var measured));
        Assert.True(FriezeRealization.TryRead(measured, out _, out _, out var strokes, out _));
        var result = FriezeCoverage.Evaluate(fixture, measured, strokes, 24, minimumY: -1, maximumY: 21);

        Assert.Equal(24 * 23, result.Projection.Cells);
        Assert.Equal(0, result.Projection.Unresolved);
        Assert.True(Contains(result.Surface, measured)); Assert.True(Contains(result.Surface, calibration));
        Assert.True(Contains(result.Surface, trace)); Assert.True(Contains(result.Surface, strokes));
        Assert.All(E(strokes), stroke => Assert.True(Contains(result.Surface, stroke)));
        Assert.True(result.Coverage.TryReadResult(result.Read, out var empty, out var single, out var many, out _, out _, out _));
        Assert.Equal(result.Projection.Cells, E(empty).Length + E(single).Length + E(many).Length);
        Assert.All(Traverse(new Relation(fixture.Surface, trace)), element =>
        { Assert.IsNotType<Carrier>(element); Assert.IsNotType<CarrierLocation>(element); });
        Assert.All(Traverse(calibration).OfType<Carrier>(), carrier =>
        { Assert.Equal(0UL, carrier.Value); Assert.Equal(0L, carrier.Tension); });
    }

    [Fact]
    public void PeriodicSeamHasActualAdjacencyAndTranslatedFootprintWitnesses()
    {
        var f = new Input(); var stroke = f.Stroke(0, 0, 0, 2); var strokes = S(stroke);
        var result = FriezeCoverage.Evaluate(f.Fixture, f.Source, strokes, 6, minimumY: 0, maximumY: 2);
        var coordinates = CoordinateRows(result); var joins = JoinRows(result);
        var metricConfig = Assert.IsType<Relation>(Assert.IsType<Relation>(result.Surface.Left).Right);
        var metricMaps = E(Assert.IsType<Relation>(Assert.IsType<Relation>(metricConfig.Right).Left)).Cast<Relation>().ToArray();
        Assert.Equal(2, metricMaps.Length);
        foreach (var map in metricMaps)
        {
            var sourceRole = Assert.IsType<Relation>(map.Left);
            var sourceSpan = Assert.IsType<Relation>(sourceRole.Right);
            var sourceSite = Assert.IsType<CarrierLocation>(Assert.IsType<Relation>(sourceSpan.Left).Left);
            Assert.Same(ReferenceEquals(sourceRole.Left, f.Fixture.Frame.Horizontal) ? f.X : f.Y, sourceSite.Carrier);
            Assert.Equal((0L, 1L), Span(sourceSpan)); Assert.Equal((0L, 1L), Span(Assert.IsType<Relation>(map.Right)));
            Assert.True(Contains(result.Surface, map));
        }
        Assert.Equal(6, joins.Length);
        var adjacency = E(result.Coverage.Adjacency);
        foreach (var clause in joins)
        {
            var edge = Assert.IsType<Relation>(clause.Left);
            Assert.Contains(adjacency, candidate => ReferenceEquals(candidate, edge));
            var a = Coordinate(coordinates, edge.Left); var b = Coordinate(coordinates, edge.Right);
            Assert.Equal(a.Y, b.Y);
            Assert.True(a.X == 5 && b.X == 0 || a.X == 0 && b.X == 5);
            var translation = Span(Assert.IsType<Relation>(clause.Right));
            Assert.Equal(6L, Math.Abs(translation.To - translation.From));
        }
        var seamCell = coordinates.Single(row => XY(Assert.IsType<Relation>(row.Right)) == (5L, 1L)).Left;
        var coverageRow = E(result.Coverage.Coverage).Cast<Relation>().Single(row => ReferenceEquals(row.Left, seamCell));
        var footprint = Assert.IsType<Relation>(Assert.Single(E(Assert.IsType<Relation>(coverageRow.Right))));
        Assert.Same(stroke, footprint.Left);
        var copy = Span(Assert.IsType<Relation>(footprint.Right));
        Assert.Equal(0L, copy.From); Assert.Equal(6L, copy.To);
        Assert.Equal(0, result.Projection.Unresolved);
    }

    [Fact]
    public void DuplicateRoutesToOneStrokeStayOneFootprintWhileDistinctStrokesCanCoOccupy()
    {
        var f = new Input(); var first = f.Stroke(2, 0, 2, 2);
        var repeated = FriezeCoverage.Evaluate(f.Fixture, f.Source, S(first, first), 8, minimumY: 0, maximumY: 2);
        var second = f.Stroke(2, 0, 2, 2);
        var separate = FriezeCoverage.Evaluate(f.Fixture, f.Source, S(first, second), 8, minimumY: 0, maximumY: 2);

        Assert.Equal(0, repeated.Projection.Multiple);
        Assert.True(repeated.Projection.Single > 0);
        Assert.True(separate.Projection.Multiple > 0);
        Assert.True(separate.Coverage.TryReadResult(separate.Read, out _, out _, out var many, out _, out _, out _));
        foreach (var row in E(many).Cast<Relation>())
        {
            var footprints = E(Assert.IsType<Relation>(row.Right)).Cast<Relation>().ToArray();
            Assert.Contains(footprints, hit => ReferenceEquals(hit.Left, first));
            Assert.Contains(footprints, hit => ReferenceEquals(hit.Left, second));
        }
        // Coarse neighborhood overlap is an occupancy result. No exact line
        // intersection or geometric retrace certificate is manufactured here.
        Assert.True(Contains(separate.Surface, first)); Assert.True(Contains(separate.Surface, second));
    }

    [Fact]
    public void DistinctNearbyStrokesCanShareProbesWithoutAnExactIntersection()
    {
        var f = new Input(); var a = f.Stroke(2, 0, 2, 2); var b = f.Stroke(4, 0, 4, 2);
        var result = FriezeCoverage.Evaluate(f.Fixture, f.Source, S(a, b), 10, minimumY: 0, maximumY: 2);
        Assert.True(result.Projection.Multiple > 0);
        var x3 = CoordinateRows(result).Where(row => XY(Assert.IsType<Relation>(row.Right)).X == 3).Select(row => row.Left).ToArray();
        var collisionRows = E(result.Coverage.Coverage).Cast<Relation>().Where(row => x3.Any(cell => ReferenceEquals(cell, row.Left))).ToArray();
        Assert.Equal(3, collisionRows.Length);
        Assert.All(collisionRows, row => Assert.Equal(2, E(Assert.IsType<Relation>(row.Right)).Length));
        Assert.Equal(0, result.Projection.Unresolved);
    }

    [Fact]
    public void OpenAndMalformedStrokeScopesRemainUnknownInsteadOfKnownEmpty()
    {
        var f = new Input(); var stroke = f.Stroke(2, 0, 2, 2);
        var malformed = new Relation(new Placeholder(), new Placeholder());
        foreach (var strokes in new[]
        {
            malformed,
            StructuralConstraint.Open(stroke),
            S(stroke, new Placeholder()),
            S(stroke, malformed)
        })
        {
            var result = FriezeCoverage.Evaluate(f.Fixture, f.Source, strokes, 6, minimumY: 0, maximumY: 2);
            AssertUnknown(result);
            Assert.True(Contains(result.Surface, strokes));
            Assert.NotEmpty(E(SupplierPending(result)));
            Assert.All(E(result.Coverage.Coverage).Cast<Relation>(), row => AssertOpen(Assert.IsType<Relation>(row.Right)));
        }
    }

    [Fact]
    public void ForeignAxesAndIncompatibleEndpointCalibrationsDoNotSupplySilentCoordinates()
    {
        var f = new Input(); var other = new Input(); var valid = f.Stroke(2, 0, 2, 2);
        var foreignFrame = other.Stroke(2, 0, 2, 2);
        var differentBasis = new Relation(new Placeholder(), new Relation(f.Point(2, 0),
            Point(f.Fixture, new Carrier(), f.Y, 2, 2)));
        foreach (var foreign in new[] { foreignFrame, differentBasis })
        {
            var result = FriezeCoverage.Evaluate(f.Fixture, f.Source, S(valid, foreign), 6, minimumY: 0, maximumY: 2);
            AssertUnknown(result);
            Assert.True(Contains(SupplierPending(result), foreign));
            Assert.True(Contains(result.Surface, valid));
        }
    }

    [Theory]
    [InlineData(0, 2048)]
    [InlineData(-1, 2048)]
    [InlineData(129, 2048)]
    [InlineData(6, 0)]
    [InlineData(6, 1)]
    public void InvalidPeriodsAndInsufficientCellBudgetsReturnExplicitUnknown(long period, int budget)
    {
        var f = new Input(); var strokes = S(f.Stroke(2, 0, 2, 2));
        var result = FriezeCoverage.Evaluate(f.Fixture, f.Source, strokes, period, budget, minimumY: 0, maximumY: 2);
        AssertUnknown(result);
        Assert.True(Contains(result.Surface, strokes)); Assert.NotEmpty(E(SupplierPending(result)));
    }

    [Fact]
    public void PartialReversedAndExcessiveProbeBandsRemainUnresolved()
    {
        var f = new Input(); var strokes = S(f.Stroke(2, 0, 2, 2));
        foreach (var result in new[]
        {
            FriezeCoverage.Evaluate(f.Fixture, f.Source, strokes, 6, minimumY: -8),
            FriezeCoverage.Evaluate(f.Fixture, f.Source, strokes, 6, minimumY: 8, maximumY: -8),
            FriezeCoverage.Evaluate(f.Fixture, f.Source, strokes, 6, minimumY: -100, maximumY: 100)
        })
        {
            AssertUnknown(result); Assert.True(Contains(result.Surface, strokes));
            Assert.NotEmpty(E(SupplierPending(result)));
        }
    }

    [Fact]
    public void ExtremeCoordinatesAndUnrepresentablePeriodicOffsetsReturnUnknownWithoutOverflow()
    {
        var f = new Input();
        var verticalLimit = S(f.Stroke(0, long.MaxValue - 1, 0, long.MaxValue));
        var yFailure = FriezeCoverage.Evaluate(f.Fixture, f.Source, verticalLimit, 4);
        AssertUnknown(yFailure); Assert.True(Contains(yFailure.Surface, verticalLimit));
        var distant = S(f.Stroke(long.MinValue, 0, long.MinValue, 1));
        var xFailure = FriezeCoverage.Evaluate(f.Fixture, f.Source, distant, 1, minimumY: 0, maximumY: 1);
        AssertUnknown(xFailure); Assert.True(Contains(xFailure.Surface, distant));
        Assert.NotEmpty(E(SupplierPending(xFailure)));
    }

    [Fact]
    public void FixedProbeBandDoesNotExpandOrShrinkWithCandidateInkHeight()
    {
        var f = new Input(); var shortStroke = S(f.Stroke(2, 0, 2, 1)); var tallStroke = S(f.Stroke(2, 0, 2, 5));
        var shortRead = FriezeCoverage.Evaluate(f.Fixture, f.Source, shortStroke, 6, minimumY: -8, maximumY: 8);
        var tallRead = FriezeCoverage.Evaluate(f.Fixture, f.Source, tallStroke, 6, minimumY: -8, maximumY: 8);
        Assert.Equal(6 * 17, shortRead.Projection.Cells); Assert.Equal(shortRead.Projection.Cells, tallRead.Projection.Cells);
        foreach (var result in new[] { shortRead, tallRead })
        {
            var ys = CoordinateRows(result).Select(row => XY(Assert.IsType<Relation>(row.Right)).Y).ToArray();
            Assert.Equal(-8L, ys.Min()); Assert.Equal(8L, ys.Max()); Assert.Equal(0, result.Projection.Unresolved);
        }
        var defaultShort = FriezeCoverage.Evaluate(f.Fixture, f.Source, shortStroke, 6);
        var defaultTall = FriezeCoverage.Evaluate(f.Fixture, f.Source, tallStroke, 6);
        Assert.NotEqual(defaultShort.Projection.Cells, defaultTall.Projection.Cells);
    }

    [Fact]
    public void OutsideInkIsObservedThroughTheChosenBandWithoutClampingOrMovingTheSource()
    {
        var f = new Input(); var stroke = f.Stroke(2, -10, 2, 10); var strokes = S(stroke);
        var result = FriezeCoverage.Evaluate(f.Fixture, f.Source, strokes, 6, minimumY: -2, maximumY: 2);
        Assert.Equal(6 * 5, result.Projection.Cells); Assert.Equal(0, result.Projection.Unresolved);
        Assert.All(CoordinateRows(result), row => Assert.InRange(XY(Assert.IsType<Relation>(row.Right)).Y, -2L, 2L));
        var segment = Assert.IsType<Relation>(stroke.Right);
        Assert.Equal(-10L, XY(Assert.IsType<Relation>(segment.Left)).Y);
        Assert.Equal(10L, XY(Assert.IsType<Relation>(segment.Right)).Y);
        Assert.True(Contains(result.Surface, stroke));
        Assert.Equal(0UL, f.X.Value); Assert.Equal(0L, f.X.Tension);
        Assert.Equal(0UL, f.Y.Value); Assert.Equal(0L, f.Y.Tension);
    }

    private sealed class Input
    {
        public readonly FriezeFixture Fixture = FriezeFixture.SquareWave(false);
        public readonly Carrier X = new(); public readonly Carrier Y = new();
        public readonly Relation Source = new(new Placeholder(), new Placeholder());
        public Relation Point(long x, long y) => FriezeCoverageTests.Point(Fixture, X, Y, x, y);
        public Relation Stroke(long x0, long y0, long x1, long y1) =>
            new(new Placeholder(), new Relation(Point(x0, y0), Point(x1, y1)));
    }

    private static Relation Point(FriezeFixture fixture, Carrier xCarrier, Carrier yCarrier, long x, long y) =>
        new(new Relation(fixture.Frame.Horizontal, new CarrierLocation(xCarrier, x)),
            new Relation(fixture.Frame.Vertical, new CarrierLocation(yCarrier, y)));
    private static void AssertUnknown(FriezeCoverage.Evaluation result)
    {
        Assert.True(result.Projection.Unresolved > 0);
        Assert.Equal(0, result.Projection.Empty); Assert.Equal(0, result.Projection.Single); Assert.Equal(0, result.Projection.Multiple);
        Assert.True(result.Coverage.TryReadResult(result.Read, out _, out _, out _, out _, out var centers, out var pending));
        Assert.Empty(E(centers)); Assert.NotEmpty(E(pending));
    }
    private static Relation[] CoordinateRows(FriezeCoverage.Evaluation result) =>
        E(Assert.IsType<Relation>(Assert.IsType<Relation>(Assert.IsType<Relation>(result.Surface.Right).Left).Left)).Cast<Relation>().ToArray();
    private static Relation[] JoinRows(FriezeCoverage.Evaluation result) =>
        E(Assert.IsType<Relation>(Assert.IsType<Relation>(Assert.IsType<Relation>(result.Surface.Right).Left).Right)).Cast<Relation>().ToArray();
    private static Relation SupplierPending(FriezeCoverage.Evaluation result) =>
        Assert.IsType<Relation>(Assert.IsType<Relation>(Assert.IsType<Relation>(result.Surface.Right).Right).Right);
    private static (long X, long Y) Coordinate(Relation[] rows, IElement cell) =>
        XY(Assert.IsType<Relation>(rows.Single(row => ReferenceEquals(row.Left, cell)).Right));
    private static (long X, long Y) XY(Relation point)
    {
        Assert.True(FriezeRealization.TryReadPoint(point, out var x, out var y)); return (x, y);
    }
    private static (long From, long To) Span(Relation span)
    {
        var first = Assert.IsType<CarrierLocation>(Assert.IsType<Relation>(span.Left).Left);
        var last = Assert.IsType<CarrierLocation>(Assert.IsType<Relation>(span.Right).Left);
        Assert.True(first.Carrier.SameIdentityAs(last.Carrier)); return (first.Ticks, last.Ticks);
    }
    private static Relation S(params IElement[] entries) => StructuralConstraint.Closed(entries);
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
    private static bool Contains(IElement root, IElement target) => Traverse(root).Any(item => ReferenceEquals(item, target));
    private static IEnumerable<IElement> Traverse(IElement root)
    {
        var pending = new Stack<IElement>(); var seen = new HashSet<IElement>(ReferenceEqualityComparer.Instance); pending.Push(root);
        while (pending.TryPop(out var item))
        {
            if (!seen.Add(item)) continue;
            yield return item;
            if (item is Relation pair) { pending.Push(pair.Left); pending.Push(pair.Right); }
        }
    }
    private static void AssertOpen(Relation scope)
    {
        Assert.True(StructuralConstraint.TryReadScope(scope, out _, out var seam, out var tail)); Assert.NotEqual(seam, tail);
    }
}
