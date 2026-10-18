using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;
using Gojo.Experiments.QualitativeFrieze;
using Xunit.Abstractions;

namespace Gojo.Experiments.QualitativeExecution.Tests;

public sealed class QualitativeLayeredPatternFixtureTests : IClassFixture<QualitativeLayeredPatternFixtureTests.FixtureSet>
{
    private readonly FixtureSet fixtures;
    private readonly ITestOutputHelper output;
    public QualitativeLayeredPatternFixtureTests(FixtureSet fixtures, ITestOutputHelper output) { this.fixtures = fixtures; this.output = output; }
    public sealed class FixtureSet
    {
        public QualitativeLayeredPatternFixture Small { get; } = new(8, 8, (x, y) => (x + 2 * y) % 4);
        public QualitativeLayeredPatternFixture Large { get; } = new(12, 12, (x, y) => (x + 2 * y) % 4);
        public IEnumerable<QualitativeLayeredPatternFixture> Both => [Small, Large];
    }

    [Fact]
    public void ScaledFieldsRetainCompleteMotifsAndAnActualSharedBoundaryChamberFocus()
    {
        foreach (var source in fixtures.Both)
        {
            Assert.True(source.Publisher.IsComplete); Assert.True(source.Observation.IsComplete); Assert.True(source.Placement.IsComplete);
            Assert.True(source.Motifs.IsComplete); Assert.Empty(E(source.Motifs.Unsearched)); Assert.Empty(E(source.Motifs.Unresolved));
            Assert.True(Closed(source.ChamberFocus)); Assert.Equal(source.Columns * source.Rows, E(source.ChamberFocus).Length);
            Assert.True(Closed(source.AlphabetFocus)); Assert.Equal(4, E(source.AlphabetFocus).Length);
            foreach (var selected in E(source.AlphabetFocus))
            {
                Assert.IsType<Relation>(selected); Assert.True(source.Motifs.TryReadFamily((Relation)selected, out var family));
                Assert.True(source.Motifs.TryReadPattern(family.Definition, out var kind, out _));
                Assert.True(kind == "cycle" && family.SegmentCount is 3 or 4 || kind == "junction" && family.SegmentCount == 8);
            }
        }
    }

    [Fact]
    public void LargerCoarseGraphsPreserveRowsAndColumnsWithoutPositionInputs()
    {
        foreach (var source in fixtures.Both)
        {
            var arrangement = Arrange(source);
            Assert.True(arrangement.IsComplete); Assert.Equal(source.Columns * source.Rows, E(arrangement.Nodes).Length);
            Assert.Equal(source.Rows * (source.Columns - 1), E(arrangement.Horizontal).Length);
            Assert.Equal(source.Columns * (source.Rows - 1), E(arrangement.Vertical).Length);
            Assert.Equal(source.Rows, E(arrangement.HorizontalPaths).Length); Assert.Equal(source.Columns, E(arrangement.VerticalPaths).Length);
            Assert.All(E(arrangement.HorizontalPaths).Cast<Relation>(), path => Assert.Equal(source.Columns, E(path).Length));
            Assert.All(E(arrangement.VerticalPaths).Cast<Relation>(), path => Assert.Equal(source.Rows, E(path).Length));
            Assert.Equal(0, arrangement.ActualQueryCount);
        }
    }

    [Fact]
    public void FourExplicitNativeCellQueriesReturnAllFourActualPrototypeDefinitions()
    {
        foreach (var source in fixtures.Both)
        {
            var arrangement = Arrange(source); var row = Assert.IsType<Relation>(E(arrangement.HorizontalPaths)[0]);
            var requested = E(row).Take(4).ToArray(); var replies = requested.Select(arrangement.ReadCell).ToArray();
            Assert.All(replies, reply => { Assert.True(reply.IsComplete); Assert.Single(E(reply.Symbols)); Assert.Empty(E(reply.Unresolved)); });
            var symbols = replies.SelectMany(reply => E(reply.Symbols)).Distinct(ReferenceEqualityComparer.Instance).ToArray();
            Assert.Equal(4, symbols.Length);
            foreach (var symbol in symbols) Assert.Contains(source.Motifs.Families, family => ReferenceEquals(family.Definition, symbol));
            Assert.Equal(4, arrangement.ActualQueryCount); Assert.Equal(4, arrangement.RequestCount);
            Assert.Same(replies[0], arrangement.ReadCell(requested[0])); Assert.Equal(4, arrangement.ActualQueryCount); Assert.Equal(1, arrangement.CacheHits);
        }
    }

    [Fact]
    public void FineObservationAndOrdinalPublicationExcludeTheAuthoredSource()
    {
        foreach (var source in fixtures.Both)
        {
            var hidden = E((Relation)source.SourceTopology.Left).Concat(new IElement[]
            { source.SourceFrame.Surface, source.SourceTopology, source.SourceBindings, source.SourceEnvelopes, source.Reach }).ToArray();
            var observation = Reach(source.Observation.Surface); var placement = Reach(source.Placement.Surface);
            foreach (var item in hidden)
            {
                Assert.DoesNotContain(observation, actual => ReferenceEquals(item, actual));
                Assert.DoesNotContain(placement, actual => ReferenceEquals(item, actual));
            }
            Assert.DoesNotContain(observation, item => item is Carrier or CarrierLocation);
            Assert.DoesNotContain(placement, item => item is Carrier or CarrierLocation);
        }
    }

    [Fact]
    public void CoarseInterfacesAndDemandRepliesExcludeFineSitesAndOrdinalControls()
    {
        var source = fixtures.Large; var arrangement = Arrange(source); var before = arrangement.PublicSurface;
        var initial = Reach(before); var node = E((Relation)E(arrangement.HorizontalPaths)[0])[0];
        var reply = arrangement.ReadCell(node); var after = Reach(arrangement.PublicSurface);
        Assert.DoesNotContain(initial, item => ReferenceEquals(item, reply.Surface));
        Assert.Contains(after, item => ReferenceEquals(item, reply.Surface));
        Assert.Equal(1, arrangement.ActualQueryCount);
        var hidden = E(source.Observation.Sites).Concat(E(source.Observation.Segments)).Concat(E(source.ChamberFocus)).Concat(new IElement[]
        {
            source.Observation.Surface, source.Motifs.Surface, source.Placement.Surface,
            source.Placement.HorizontalBuckets, source.Placement.VerticalBuckets, source.Placement.Envelopes,
            source.SourceBindings, source.SourceTopology, source.Reach
        }).ToArray();
        foreach (var item in hidden) Assert.DoesNotContain(after, actual => ReferenceEquals(actual, item));
        Assert.DoesNotContain(after, item => item is Carrier or CarrierLocation);
        var audit = Reach(arrangement.AuditSurface);
        Assert.Contains(audit, item => ReferenceEquals(item, source.Observation.Surface));
        Assert.Contains(audit, item => ReferenceEquals(item, source.Placement.Surface));
        Assert.Contains(audit, item => ReferenceEquals(item, reply.Surface));
    }

    [Fact]
    public void AllCellQueriesRetainCompleteCandidateAccountingWithOrdinalExclusionWitnesses()
    {
        foreach (var source in fixtures.Both)
        {
            var watch = System.Diagnostics.Stopwatch.StartNew(); var arrangement = Arrange(source);
            var construction = watch.Elapsed.TotalSeconds; watch.Restart();
            foreach (var cell in E(arrangement.Chambers)) Assert.True(arrangement.ReadCell(cell).IsComplete);
            var seconds = watch.Elapsed.TotalSeconds;
            var cells = source.Columns * source.Rows; var candidates = E(arrangement.AuditCandidates).Length;
            Assert.Equal(cells, arrangement.ActualQueryCount);
            Assert.Equal(cells * candidates, arrangement.FullContainmentReadCount + arrangement.OrdinalExclusionCount);
            Assert.True(arrangement.FullContainmentReadCount < cells * candidates);
            foreach (var cell in E(arrangement.Chambers))
            {
                Assert.True(arrangement.TryReadQueryAudit(cell, out var inventory, out var checks, out var exclusions, out var unknown, out var unsearched));
                Assert.Equal(candidates, E(inventory).Length); Assert.Equal(candidates, E(checks).Length + E(exclusions).Length);
                Assert.Empty(E(unknown)); Assert.Empty(E(unsearched));
            }
            output.WriteLine($"{source.Columns}x{source.Rows}: coarse construction {construction:0.000}s; all {cells} queries {seconds:0.000}s; " +
                $"{arrangement.FullContainmentReadCount} full reads, {arrangement.OrdinalExclusionCount} witnessed exclusions, {candidates} candidates per cell.");
        }
    }

    private static QualitativeMotifArrangement Arrange(QualitativeLayeredPatternFixture source) =>
        new(source.Motifs, source.Placement, source.ChamberFocus, source.AlphabetFocus);
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
    private static bool Closed(Relation scope) => StructuralConstraint.TryReadScope(scope, out _, out var seam, out var tail) && seam.Equals(tail);
    private static HashSet<IElement> Reach(IElement source)
    {
        var seen = new HashSet<IElement>(ReferenceEqualityComparer.Instance); var work = new Stack<IElement>(); work.Push(source);
        while (work.TryPop(out var item)) if (seen.Add(item) && item is Relation row) { work.Push(row.Left); work.Push(row.Right); }
        return seen;
    }
}
