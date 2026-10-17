using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeFrieze;

namespace Gojo.Experiments.QualitativeExecution.Tests;

public sealed class FriezeMeasuredRulesTests
{
    [Fact]
    public void ActualSquareTraceHasPeriodicContactsWithoutRetracingAndCarriesItsSymmetryMaps()
    {
        var fixture = FriezeFixture.SquareWave(false); var trace = fixture.Run(12);
        var calibration = FriezeRealization.CreateCalibration(fixture, FriezeRealization.DefaultLengths(fixture));
        var read = FriezeMeasuredRules.Evaluate(fixture, trace, calibration);
        var summary = FriezeMeasuredRules.Describe(read);
        Assert.Equal(24, summary.PeriodX); Assert.Equal(0, summary.PeriodY); Assert.Equal(20, summary.Height);
        Assert.Equal(4, summary.StrokeCount); Assert.Equal(0, summary.OverlapCount); Assert.Equal(0, summary.CrossingCount);
        Assert.True(summary.ContactCount > 0); Assert.Equal(0, summary.UnresolvedCount);
        Assert.Contains("translation", summary.VerifiedSymmetryNames); Assert.Contains("vertical mirror", summary.VerifiedSymmetryNames);
        Assert.Contains("half turn", summary.VerifiedSymmetryNames); Assert.Contains("glide", summary.VerifiedSymmetryNames);
        Assert.DoesNotContain("horizontal mirror", summary.VerifiedSymmetryNames);
        Assert.True(Contains(read, trace)); Assert.True(Contains(read, calibration));
    }

    [Fact]
    public void OverlapAcrossThePeriodicSeamCannotHideOutsideTheDrawnCell()
    {
        var geometry = new Geometry(10);
        var read = geometry.Read((8, 0, 12, 0), (1, 0, 3, 0));
        var summary = FriezeMeasuredRules.Describe(read);
        Assert.Equal(1, summary.OverlapCount); Assert.Equal(0, summary.CrossingCount); Assert.Equal(0, summary.UnresolvedCount);
    }

    [Fact]
    public void ExactNeighborRangeIncludesTranslationsMoreThanOneCellAway()
    {
        var geometry = new Geometry(10);
        var read = geometry.Read((0, 0, 2, 0), (30, 0, 32, 0));
        Assert.Equal(1, FriezeMeasuredRules.Describe(read).OverlapCount);
        Assert.True(FriezeMeasuredRules.TryReadResult(read, out _, out var overlaps, out _, out _, out _, out _));
        var witness = Assert.IsType<Relation>(Assert.Single(E(overlaps)));
        var shift = Assert.IsType<Relation>(Assert.IsType<Relation>(witness.Left).Right);
        Assert.Equal(-30, SpanEnd(shift));
    }

    [Fact]
    public void ProperCrossingsAreASeparateQuestionFromPositiveLengthRetracing()
    {
        var geometry = new Geometry(20);
        var read = geometry.Read((1, 0, 9, 8), (1, 8, 9, 0));
        var summary = FriezeMeasuredRules.Describe(read);
        Assert.Equal(0, summary.OverlapCount); Assert.Equal(1, summary.CrossingCount); Assert.Equal(0, summary.ContactCount);
        Assert.Equal(0, summary.UnresolvedCount);
    }

    [Fact]
    public void RationalCrossingLocationsAreKeptExactlyRatherThanRoundedOntoAnEndpoint()
    {
        var geometry = new Geometry(10);
        var read = geometry.Read((0, 0, 3, 3), (0, 2, 3, 0));
        Assert.Equal(1, FriezeMeasuredRules.Describe(read).CrossingCount);
        Assert.True(FriezeMeasuredRules.TryReadResult(read, out _, out _, out var crossings, out _, out _, out _));
        Assert.Contains(Traverse(crossings), element => element is Relation
        {
            Left: CarrierLocation { Ticks: 6 },
            Right: Relation { Left: CarrierLocation { Ticks: 5 } }
        });
    }

    [Fact]
    public void CollinearEndpointJointsIncludingTheWrapSeamAreNotRetracing()
    {
        var geometry = new Geometry(10);
        var summary = FriezeMeasuredRules.Describe(geometry.Read((0, 0, 5, 0), (5, 0, 10, 0)));
        Assert.Equal(0, summary.OverlapCount); Assert.Equal(0, summary.CrossingCount); Assert.Equal(2, summary.ContactCount);
    }

    [Fact]
    public void AReversedAdjacentStrokeStillRetracesPositiveExtent()
    {
        var geometry = new Geometry(20);
        var summary = FriezeMeasuredRules.Describe(geometry.Read((0, 0, 5, 0), (5, 0, 0, 0)));
        Assert.Equal(1, summary.OverlapCount);
    }

    [Fact]
    public void EndpointIntoInteriorIsAContactWithDifferentIncidenceFromAnOrdinaryJoint()
    {
        var geometry = new Geometry(20);
        var read = geometry.Read((0, 0, 10, 0), (5, 0, 5, 5));
        var summary = FriezeMeasuredRules.Describe(read);
        Assert.Equal(0, summary.OverlapCount); Assert.Equal(0, summary.CrossingCount); Assert.Equal(1, summary.ContactCount);
        Assert.True(FriezeMeasuredRules.TryReadResult(read, out _, out _, out _, out var contacts, out _, out _));
        var contact = (Relation)Assert.Single(E(contacts));
        var pairs = (Relation)((Relation)contact.Left).Left;
        var pair = (Relation)Assert.Single(E(pairs));
        var incidences = (Relation)((Relation)pair.Right).Right;
        Assert.False(FriezeRealization.TryReadPoint((Relation)((Relation)incidences.Left).Left, out _, out _));
        Assert.True(FriezeRealization.TryReadPoint((Relation)((Relation)incidences.Right).Left, out _, out _));
        Assert.Single(E((Relation)((Relation)contact.Right).Left));
        Assert.Single(E((Relation)((Relation)contact.Right).Right));
    }

    [Fact]
    public void PausesPreserveTheirSourceButDoNotBecomeZeroLengthInkViolations()
    {
        var geometry = new Geometry(10);
        var read = geometry.Read((0, 0, 0, 0), (0, 0, 10, 0));
        var summary = FriezeMeasuredRules.Describe(read);
        Assert.Equal(1, summary.StrokeCount); Assert.Equal(0, summary.OverlapCount); Assert.Equal(0, summary.CrossingCount);
        Assert.True(Contains(read, geometry.LastStrokes!));
    }

    [Fact]
    public void SymmetryChecksCoverWholeStrokesRatherThanOnlyTheirEndpoints()
    {
        var geometry = new Geometry(10);
        var read = geometry.Read((0, 0, 0, 10), (0, 10, 5, 10), (5, 10, 5, 0), (5, 0, 10, 0));
        Assert.True(FriezeMeasuredRules.TryReadResult(read, out var strokes, out _, out _, out _, out var symmetries, out _));
        foreach (var entry in E(symmetries).Cast<Relation>())
        {
            Assert.True(FriezeMeasuredRules.TryReadSymmetry(entry, out _, out var coverage, out var unmatched));
            Assert.NotEmpty(E(coverage));
            Assert.Contains(E(strokes), source => Contains(entry, source));
            if (E(unmatched).Length != 0) Assert.Contains(Traverse(unmatched), value => value is CarrierLocation);
        }
        Assert.Contains(E(symmetries).Cast<Relation>(), witness =>
            FriezeMeasuredRules.TryReadSymmetry(witness, out _, out _, out var unmatched) && E(unmatched).Length != 0);
    }

    [Fact]
    public void CollinearSubdivisionDoesNotChangeInkSymmetryOrCreateOverlaps()
    {
        var geometry = new Geometry(10);
        var first = FriezeMeasuredRules.Describe(geometry.Read((0, 0, 0, 10), (0, 10, 5, 10), (5, 10, 5, 0), (5, 0, 10, 0)));
        var second = FriezeMeasuredRules.Describe(geometry.Read((0, 0, 0, 5), (0, 5, 0, 10), (0, 10, 5, 10),
            (5, 10, 5, 5), (5, 5, 5, 0), (5, 0, 10, 0)));
        Assert.Equal(first.VerifiedSymmetryNames.Order(), second.VerifiedSymmetryNames.Order());
        Assert.Equal(first.FundamentalPeriodX, second.FundamentalPeriodX);
        Assert.Equal(0, second.OverlapCount); Assert.Equal(0, second.UnresolvedCount);
    }

    [Fact]
    public void TranslatedCoordinatesAndCyclicCellStartKeepTheSameSymmetryReads()
    {
        var baseGeometry = new Geometry(10);
        var translated = new Geometry(10, 37, -12);
        var cycled = new Geometry(10, 0, 10);
        var a = FriezeMeasuredRules.Describe(baseGeometry.Read((0, 0, 0, 10), (0, 10, 5, 10), (5, 10, 5, 0), (5, 0, 10, 0)));
        var b = FriezeMeasuredRules.Describe(translated.Read((37, -12, 37, -2), (37, -2, 42, -2), (42, -2, 42, -12), (42, -12, 47, -12)));
        var c = FriezeMeasuredRules.Describe(cycled.Read((0, 10, 5, 10), (5, 10, 5, 0), (5, 0, 10, 0), (10, 0, 10, 10)));
        Assert.Equal(a.VerifiedSymmetryNames.Order(), b.VerifiedSymmetryNames.Order());
        Assert.Equal(a.VerifiedSymmetryNames.Order(), c.VerifiedSymmetryNames.Order());
        Assert.Equal(0, b.UnresolvedCount); Assert.Equal(0, c.UnresolvedCount);
    }

    [Fact]
    public void GeometricFundamentalPeriodCanBeSmallerThanTheControllerConstructionCell()
    {
        var geometry = new Geometry(20);
        var summary = FriezeMeasuredRules.Describe(geometry.Read((0, 0, 0, 10), (0, 10, 5, 10), (5, 10, 5, 0), (5, 0, 10, 0),
            (10, 0, 10, 10), (10, 10, 15, 10), (15, 10, 15, 0), (15, 0, 20, 0)));
        Assert.Equal(20, summary.PeriodX); Assert.Equal(10, summary.FundamentalPeriodX);
        Assert.Contains("glide", summary.VerifiedSymmetryNames);
    }

    [Fact]
    public void GlideTwiceHasTheNativeTranslationCalibrationAndActualCoverageWitnesses()
    {
        var geometry = new Geometry(10);
        var read = geometry.Read((0, 0, 0, 10), (0, 10, 5, 10), (5, 10, 5, 0), (5, 0, 10, 0));
        Assert.True(FriezeMeasuredRules.TryReadResult(read, out var strokes, out _, out _, out _, out var symmetries, out _));
        var passed = E(symmetries).Cast<Relation>().Where(w => FriezeMeasuredRules.TryReadSymmetry(w, out _, out _, out var missing) && E(missing).Length == 0).ToArray();
        var maps = passed.Select(witness => (Relation)((Relation)witness.Left).Left).ToArray();
        var translation = Assert.Single(maps, map => SpanEnd((Relation)((Relation)map.Left).Right) - SpanStart((Relation)((Relation)map.Left).Right) == 1 &&
            SpanEnd((Relation)((Relation)map.Right).Right) - SpanStart((Relation)((Relation)map.Right).Right) == 1);
        var glide = Assert.Single(maps, map => SpanStart((Relation)((Relation)map.Left).Right) == 5 &&
            SpanEnd((Relation)((Relation)map.Right).Right) - SpanStart((Relation)((Relation)map.Right).Right) == -1);
        Assert.Equal(SpanStart((Relation)((Relation)translation.Left).Right), 2 * SpanStart((Relation)((Relation)glide.Left).Right));
        Assert.Equal(10, SpanStart((Relation)((Relation)glide.Right).Right));
        Assert.Equal(0, SpanStart((Relation)((Relation)translation.Right).Right));
        Assert.All(E(strokes), stroke => Assert.True(Contains(read, stroke)));
    }

    [Fact]
    public void WorkLimitAndInvalidPeriodicClosureStayUnresolved()
    {
        var geometry = new Geometry(10);
        var strokes = S(geometry.Stroke(0, 0, 0, 10), geometry.Stroke(0, 10, 5, 10), geometry.Stroke(5, 10, 5, 0), geometry.Stroke(5, 0, 10, 0));
        var limited = FriezeMeasuredRules.EvaluateStrokes(strokes, new Relation(geometry.Point(0, 0), geometry.Point(10, 0)), geometry.Frame.Surface, 1);
        Assert.True(FriezeMeasuredRules.Describe(limited).UnresolvedCount > 0);
        var invalid = FriezeMeasuredRules.EvaluateStrokes(strokes, new Relation(geometry.Point(0, 0), geometry.Point(10, 1)), geometry.Frame.Surface);
        Assert.True(FriezeMeasuredRules.Describe(invalid).UnresolvedCount > 0); Assert.True(Contains(invalid, strokes));
    }

    [Fact]
    public void AliasedAxesCannotSupplyTwoIndependentMeasuredCoordinateRoles()
    {
        var frame = new QualitativeMotionFrame(); var x = new Carrier(); var y = new Carrier();
        Relation Point(long a, long b) => new(new Relation(frame.Horizontal, new CarrierLocation(x, a)),
            new Relation(frame.Horizontal, new CarrierLocation(y, b)));
        var period = new Relation(Point(0, 0), Point(10, 0));
        var read = FriezeMeasuredRules.EvaluateStrokes(S(new Relation(new Placeholder(), period)), period, frame.Surface);
        Assert.True(FriezeMeasuredRules.TryReadResult(read, out var strokes, out _, out _, out _, out var symmetries, out var unresolved));
        Assert.Empty(E(strokes)); Assert.Empty(E(symmetries)); Assert.Contains(E(unresolved), value => ReferenceEquals(value, period));
        Assert.True(FriezeMeasuredRules.Describe(read).UnresolvedCount > 0);
    }

    [Fact]
    public void UnknownOrOpposedCarriersDoNotBecomePositiveMeasuredAxes()
    {
        foreach (var unsupported in new[] { Carrier.Unknown, new Carrier(true) })
            foreach (var replaceX in new[] { true, false })
            {
                var frame = new QualitativeMotionFrame(); var x = replaceX ? unsupported : new Carrier();
                var y = replaceX ? new Carrier() : unsupported;
                Relation Point(long a, long b) => new(new Relation(frame.Horizontal, new CarrierLocation(x, a)),
                    new Relation(frame.Vertical, new CarrierLocation(y, b)));
                var period = new Relation(Point(0, 0), Point(10, 0));
                var source = S(new Relation(new Placeholder(), period));
                var read = FriezeMeasuredRules.EvaluateStrokes(source, period, frame.Surface);
                var summary = FriezeMeasuredRules.Describe(read);
                Assert.True(summary.UnresolvedCount > 0); Assert.Empty(summary.VerifiedSymmetryNames);
                Assert.True(Contains(read, source)); Assert.True(Contains(read, unsupported));
            }
    }

    [Fact]
    public void NonwholeSourcePeriodRemainsExactUnresolvedEvidenceRatherThanARoundedSummary()
    {
        var frame = new QualitativeMotionFrame(); var x = new Carrier(); var y = new Carrier();
        Relation Point(IElement a, long b) => new(new Relation(frame.Horizontal, a), new Relation(frame.Vertical, new CarrierLocation(y, b)));
        var half = new Relation(new CarrierLocation(x, 1), Relation.Point(x, 2));
        var period = new Relation(Point(new CarrierLocation(x, 0), 0), Point(half, 0));
        var source = S(new Relation(new Placeholder(), period));
        var read = FriezeMeasuredRules.EvaluateStrokes(source, period, frame.Surface);
        var summary = FriezeMeasuredRules.Describe(read);
        Assert.Equal(0, summary.PeriodX); Assert.True(summary.UnresolvedCount > 0); Assert.Empty(summary.VerifiedSymmetryNames);
        Assert.True(FriezeMeasuredRules.TryReadResult(read, out var strokes, out _, out _, out _, out _, out var unresolved));
        Assert.Empty(E(strokes)); Assert.Contains(E(unresolved), item => ReferenceEquals(item, period));
        Assert.True(Contains(read, half)); Assert.True(Contains(read, source));
    }

    [Fact]
    public void NonwholeStrokeCoordinatesAreNotSilentlyAdmittedIntoAWholeTickSourceRead()
    {
        var frame = new QualitativeMotionFrame(); var x = new Carrier(); var y = new Carrier();
        Relation Point(long a, IElement b) => new(new Relation(frame.Horizontal, new CarrierLocation(x, a)), new Relation(frame.Vertical, b));
        var half = new Relation(new CarrierLocation(y, 1), Relation.Point(y, 2));
        var period = new Relation(Point(0, new CarrierLocation(y, 0)), Point(10, new CarrierLocation(y, 0)));
        var clause = new Relation(new Placeholder(), new Relation(Point(0, half), Point(5, half)));
        var read = FriezeMeasuredRules.EvaluateStrokes(S(clause), period, frame.Surface);
        var summary = FriezeMeasuredRules.Describe(read);
        Assert.Equal(10, summary.PeriodX); Assert.True(summary.UnresolvedCount > 0); Assert.Empty(summary.VerifiedSymmetryNames);
        Assert.True(FriezeMeasuredRules.TryReadResult(read, out var strokes, out _, out _, out _, out _, out var unresolved));
        Assert.Empty(E(strokes)); Assert.Contains(E(unresolved), item => ReferenceEquals(item, clause)); Assert.True(Contains(read, half));
    }

    [Fact]
    public void WholeSourcePeriodOutsideTheStoredSummaryRangeIsUnresolvedWithoutOverflowOrSaturation()
    {
        var geometry = new Geometry(10);
        var period = new Relation(geometry.Point(long.MinValue, 0), geometry.Point(long.MaxValue, 0));
        var read = FriezeMeasuredRules.EvaluateStrokes(S(geometry.Stroke(0, 0, 1, 1)), period, geometry.Frame.Surface);
        var summary = FriezeMeasuredRules.Describe(read);
        Assert.Equal(0, summary.PeriodX); Assert.True(summary.UnresolvedCount > 0); Assert.Empty(summary.VerifiedSymmetryNames);
        Assert.True(Contains(read, period));
    }

    [Fact]
    public void SplittingEitherOrBothStraightSupportsDoesNotTurnAnInkCrossingIntoContacts()
    {
        var geometry = new Geometry(20);
        var unsplit = FriezeMeasuredRules.Describe(geometry.Read((2, 2, 18, 18), (2, 18, 18, 2)));
        var one = FriezeMeasuredRules.Describe(geometry.Read((2, 2, 10, 10), (10, 10, 18, 18), (2, 18, 18, 2)));
        var bothRead = geometry.Read((2, 2, 10, 10), (10, 10, 18, 18), (2, 18, 10, 10), (10, 10, 18, 2));
        var both = FriezeMeasuredRules.Describe(bothRead);
        Assert.Equal(1, unsplit.CrossingCount); Assert.Equal(unsplit.CrossingCount, one.CrossingCount);
        Assert.Equal(unsplit.CrossingCount, both.CrossingCount); Assert.Equal(0, both.ContactCount);
        Assert.Equal(0, both.OverlapCount); Assert.Equal(0, both.UnresolvedCount);
        Assert.True(FriezeMeasuredRules.TryReadResult(bothRead, out var sources, out _, out var crossings, out _, out _, out _));
        var crossing = (Relation)Assert.Single(E(crossings));
        Assert.Equal(2, E((Relation)((Relation)crossing.Right).Left).Length);
        Assert.Empty(E((Relation)((Relation)crossing.Right).Right));
        Assert.All(E(sources), source => Assert.True(Contains(crossing, source)));
    }

    [Fact]
    public void ReorderingAndReversingSplitStrokesKeepsTheSameInkCrossing()
    {
        var geometry = new Geometry(20);
        var summary = FriezeMeasuredRules.Describe(geometry.Read((18, 2, 10, 10), (18, 18, 10, 10),
            (10, 10, 2, 18), (10, 10, 2, 2)));
        Assert.Equal(1, summary.CrossingCount); Assert.Equal(0, summary.OverlapCount); Assert.Equal(0, summary.UnresolvedCount);
    }

    [Fact]
    public void SplitCrossingAtThePeriodicSeamCollectsTranslatedRaysInOneCanonicalBasis()
    {
        var geometry = new Geometry(20);
        var read = geometry.Read((16, -4, 20, 0), (0, 0, 4, 4), (16, 4, 20, 0), (0, 0, 4, -4));
        var summary = FriezeMeasuredRules.Describe(read);
        Assert.Equal(1, summary.CrossingCount); Assert.Equal(0, summary.ContactCount); Assert.Equal(0, summary.UnresolvedCount);
        Assert.True(FriezeMeasuredRules.TryReadResult(read, out _, out _, out var crossings, out _, out _, out _));
        var crossing = (Relation)Assert.Single(E(crossings));
        Assert.True(FriezeRealization.TryReadPoint((Relation)((Relation)crossing.Left).Right, out var x, out var y));
        Assert.Equal(0, x); Assert.Equal(0, y);
        Assert.Contains(Traverse(crossing), element => element is Relation
        {
            Left: Relation { Left: CarrierLocation { Ticks: 0 }, Right: Carrier },
            Right: Relation { Left: CarrierLocation { Ticks: -20 }, Right: Carrier }
        });
        Assert.Equal(2, E((Relation)((Relation)crossing.Right).Left).Length);
    }

    [Fact]
    public void DuplicateRaysOnOneSideCannotSupplyTheMissingOpposedSupportOfATJunction()
    {
        var geometry = new Geometry(20);
        var read = geometry.Read((-4, 0, 0, 0), (0, 0, 4, 0), (0, 0, 0, 4), (0, 0, 0, 4));
        var summary = FriezeMeasuredRules.Describe(read);
        Assert.Equal(0, summary.CrossingCount); Assert.Equal(1, summary.ContactCount); Assert.Equal(1, summary.OverlapCount);
        Assert.True(FriezeMeasuredRules.TryReadResult(read, out _, out _, out _, out var contacts, out _, out _));
        var contact = (Relation)Assert.Single(E(contacts));
        Assert.Single(E((Relation)((Relation)contact.Right).Left));
        Assert.Equal(2, E((Relation)((Relation)contact.Right).Right).Length);
    }

    [Fact]
    public void CrossingWitnessKeepsAnAdditionalTerminatingBranchWithoutCountingTheSiteTwice()
    {
        var geometry = new Geometry(20);
        var read = geometry.Read((-4, -4, 4, 4), (-4, 4, 4, -4), (0, 0, 0, 4));
        var summary = FriezeMeasuredRules.Describe(read);
        Assert.Equal(1, summary.CrossingCount); Assert.Equal(0, summary.ContactCount);
        Assert.True(FriezeMeasuredRules.TryReadResult(read, out _, out _, out var crossings, out _, out _, out _));
        var crossing = (Relation)Assert.Single(E(crossings));
        Assert.Equal(2, E((Relation)((Relation)crossing.Right).Left).Length);
        Assert.Single(E((Relation)((Relation)crossing.Right).Right));
    }

    [Fact]
    public void BentInterleavingPathsDoNotInventAnUnprovidedStraightThroughContinuationLaw()
    {
        var geometry = new Geometry(20);
        var summary = FriezeMeasuredRules.Describe(geometry.Read((-2, -1, 0, 0), (0, 0, 2, 2), (-2, 1, 0, 0), (0, 0, 2, -2)));
        Assert.Equal(0, summary.CrossingCount); Assert.Equal(1, summary.ContactCount); Assert.Equal(0, summary.UnresolvedCount);
    }

    private sealed class Geometry(long period, long originX = 0, long originY = 0)
    {
        public QualitativeMotionFrame Frame { get; } = new();
        private readonly Carrier x = new(); private readonly Carrier y = new();
        public Relation? LastStrokes { get; private set; }
        public Relation Point(long a, long b) => new(new Relation(Frame.Horizontal, new CarrierLocation(x, a)), new Relation(Frame.Vertical, new CarrierLocation(y, b)));
        public Relation Stroke(long ax, long ay, long bx, long by) => new(new Placeholder(), new Relation(Point(ax, ay), Point(bx, by)));
        public Relation Read(params (long Ax, long Ay, long Bx, long By)[] strokes)
        {
            LastStrokes = S(strokes.Select(s => (IElement)Stroke(s.Ax, s.Ay, s.Bx, s.By)).ToArray());
            return FriezeMeasuredRules.EvaluateStrokes(LastStrokes, new Relation(Point(originX, originY), Point(originX + period, originY)), Frame.Surface);
        }
    }
    private static long SpanStart(Relation span) => ((CarrierLocation)((Relation)span.Left).Left).Ticks;
    private static long SpanEnd(Relation span) => ((CarrierLocation)((Relation)span.Right).Left).Ticks;
    private static Relation S(params IElement[] values) => StructuralConstraint.Closed(values);
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
    private static bool Contains(IElement root, IElement target) => Traverse(root).Any(value => ReferenceEquals(value, target));
    private static IEnumerable<IElement> Traverse(IElement root)
    {
        var seen = new HashSet<IElement>(ReferenceEqualityComparer.Instance); var stack = new Stack<IElement>(); stack.Push(root);
        while (stack.TryPop(out var value))
        {
            if (!seen.Add(value)) continue; yield return value;
            if (value is Relation r) { stack.Push(r.Right); stack.Push(r.Left); }
            else if (value is CarrierLocation location) stack.Push(location.Carrier);
        }
    }
}
