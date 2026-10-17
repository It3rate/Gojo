using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeFrieze;

namespace Gojo.Experiments.QualitativeExecution.Tests;

public sealed class ConnectedPatternScoringTests
{
    private static readonly IEqualityComparer<IElement> Identity = ReferenceEqualityComparer.Instance;
    [Fact]
    public void NativeBandsRetainActualGraphEvidenceAndContainNoMeasurements()
    {
        var entry = new ConnectedPatternLibrary().Entries[0]; var evaluation = ConnectedPatternScoring.Native(entry);
        Assert.True(evaluation.Summary.Complete); Assert.Equal(4, evaluation.Features.Count);
        Assert.Equal(2 * evaluation.Summary.Preferred + evaluation.Summary.Passed, evaluation.Score);
        Assert.Same(entry.Surface, evaluation.Source);
        Assert.All(evaluation.Features, feature => {
            Assert.NotNull(feature.Rank);
            Assert.Contains(Traverse(evaluation.Surface), value => ReferenceEquals(value, feature.Observation));
            Assert.Contains(Traverse(feature.Observation), value => ReferenceEquals(value, entry.Graph.Evaluate()));
        });
        Assert.DoesNotContain(Traverse(evaluation.Surface), value => value is Carrier or CarrierLocation);
        Assert.True(evaluation.Profile.TryReadResult(evaluation.Read, out _, out _, out _, out var pending)); Assert.Empty(E(pending));
    }

    [Fact]
    public void UnknownPublicationDoesNotNormalizeAnyNativeFeatureToFavorableZero()
    {
        var entry = new ConnectedPatternLibrary().UnknownExample; var evaluation = ConnectedPatternScoring.Native(entry);
        Assert.False(evaluation.Summary.Complete); Assert.Null(evaluation.Score);
        Assert.All(evaluation.Features, feature => Assert.Null(feature.Rank));
        Assert.Equal(4, evaluation.Summary.Unknown); Assert.Equal(0, evaluation.Summary.Preferred);
    }

    [Fact]
    public void KnownProfileViolationsRemainDifferentFromMissingRanks()
    {
        var library = new ConnectedPatternLibrary(); var evaluation = ConnectedPatternScoring.Native(library.BlockedExample);
        Assert.True(evaluation.Summary.Complete); Assert.NotNull(evaluation.Score);
        Assert.All(evaluation.Features, feature => Assert.NotNull(feature.Rank));
        // Known conditional denials do not make a fully observed graph unknown.
        Assert.Empty(E(library.BlockedExample.Unresolved));
    }

    [Fact]
    public void MeasuredFeaturesReadNativeCoverageInsteadOfItsProjectionRecord()
    {
        var entry = new ConnectedPatternLibrary().Entries[0]; var (geometry, coverage) = Measure(entry);
        var ordinary = ConnectedPatternScoring.Measured(entry, geometry, coverage);
        var alteredRecord = coverage with { Projection = new(0, 0, 0, 0, 0, int.MaxValue, int.MaxValue, int.MaxValue) };
        var sameEvidence = ConnectedPatternScoring.Measured(entry, geometry, alteredRecord);
        Assert.True(ordinary.Summary.Complete); Assert.NotNull(ordinary.Score);
        Assert.Equal(ordinary.Score, sameEvidence.Score);
        Assert.Equal(ordinary.Features.Select(feature => feature.Rank), sameEvidence.Features.Select(feature => feature.Rank));
        Assert.DoesNotContain(Traverse(ordinary.Source), value => value is Carrier or CarrierLocation);
        Assert.Contains(Traverse(ordinary.Surface), value => value is CarrierLocation);
    }

    [Fact]
    public void BorrowedGeometryOrAChangedCoverageBasisCannotSupplyKnownMeasuredRanks()
    {
        var library = new ConnectedPatternLibrary(); var entry = library.Entries[0]; var (geometry, coverage) = Measure(entry);
        var foreign = ConnectedPatternScoring.Measured(library.Entries[1], geometry, coverage);
        Assert.Null(foreign.Score); Assert.All(foreign.Features, feature => Assert.Null(feature.Rank));
        var copy = new Relation(geometry.Left, geometry.Right);
        var request = Assert.IsType<Relation>(coverage.Surface.Left); var source = Assert.IsType<Relation>(request.Left);
        var replaced = coverage with { Surface = new Relation(new Relation(new Relation(copy, source.Right), request.Right), coverage.Surface.Right) };
        var mismatched = ConnectedPatternScoring.Measured(entry, geometry, replaced);
        Assert.Null(mismatched.Score); Assert.All(mismatched.Features, feature => Assert.Null(feature.Rank));
    }

    [Fact]
    public void AVisuallyCrowdedBraceFieldCannotWinThroughOtherPreferredFeatures()
    {
        var entry = new ConnectedPatternLibrary().Entries[3]; var (geometry, coverage) = Measure(entry);
        var evaluation = ConnectedPatternScoring.Measured(entry, geometry, coverage);
        Assert.True(coverage.Projection.MultipleFraction > .35);
        Assert.Equal(4, evaluation.Features.Count);
        Assert.Equal(0, evaluation.Features[3].Rank);
        Assert.True(evaluation.Summary.Complete); Assert.False(evaluation.Summary.Acceptable);
        Assert.NotNull(evaluation.Score); // Known ordinal rank remains descriptive; acceptance is separate.
    }

    private static (Relation Geometry, FriezeCoverage.Evaluation Coverage) Measure(ConnectedPatternEntry entry)
    {
        var columns = E(entry.Columns); var levels = E(entry.Levels); var xCarrier = new Carrier(); var yCarrier = new Carrier();
        var locations = E(entry.Coordinates).Cast<Relation>().ToDictionary(row => row.Left, row => {
            var coordinate = Assert.IsType<Relation>(row.Right);
            return (X: Array.FindIndex(columns, value => ReferenceEquals(value, coordinate.Left)) * 4L,
                Y: Array.FindIndex(levels, value => ReferenceEquals(value, coordinate.Right)) * 4L);
        }, Identity);
        Relation Point(long x, long y) => new(new Relation(entry.Graph.Frame.Horizontal, new CarrierLocation(xCarrier, x)),
            new Relation(entry.Graph.Frame.Vertical, new CarrierLocation(yCarrier, y)));
        var clauses = new List<IElement>();
        foreach (var stroke in E(entry.Graph.Strokes).Cast<Relation>())
        {
            Assert.True(QualitativeStrokeGraph.TryReadStroke(stroke, out _, out var from, out _, out var word));
            var start = locations[from]; var x = start.X; var y = start.Y;
            foreach (var move in E(word).Cast<Relation>())
            {
                Assert.True(entry.Graph.Frame.Direction(move, out var axis, out var forward, out _));
                if (ReferenceEquals(axis, entry.Graph.Frame.Horizontal)) x += forward ? 4 : -4;
                else y += forward ? 4 : -4;
            }
            clauses.Add(new Relation(stroke, new Relation(Point(start.X, start.Y), Point(x, y))));
        }
        var calibration = S(Relation.Segment(xCarrier, 0, 4), Relation.Segment(yCarrier, 0, 4)); var period = columns.Length * 4L;
        var provenance = new Relation(entry.Graph.Surface, new Relation(entry.Surface, calibration));
        var geometry = FriezeMeasuredRules.EvaluateStrokes(S(clauses.ToArray()), new Relation(Point(0, 0), Point(period, 0)), provenance, 1_000_000);
        Assert.True(FriezeMeasuredRules.TryReadResult(geometry, out var admitted, out _, out _, out _, out _, out var pending)); Assert.Empty(E(pending));
        var coverage = FriezeCoverage.Evaluate(entry.Graph.Frame, geometry, admitted, period, minimumY: -2, maximumY: 14);
        return (geometry, coverage);
    }

    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
    private static Relation S(params IElement[] values) => StructuralConstraint.Closed(values);
    private static IEnumerable<IElement> Traverse(IElement source)
    {
        var seen = new HashSet<IElement>(ReferenceEqualityComparer.Instance); var pending = new Stack<IElement>(); pending.Push(source);
        while (pending.TryPop(out var value))
        { if (!seen.Add(value)) continue; yield return value; if (value is Relation row) { pending.Push(row.Left); pending.Push(row.Right); } }
    }
}
