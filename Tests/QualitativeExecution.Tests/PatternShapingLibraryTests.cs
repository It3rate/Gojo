using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeFrieze;

namespace Gojo.Experiments.QualitativeExecution.Tests;

public sealed class PatternShapingLibraryTests
{
    [Fact]
    public void AuthoredProgramsShareTheirFrameReachesAndActualRepeatedMoves()
    {
        var frame = new QualitativeMotionFrame(); var h = new Placeholder(); var v = new Placeholder();
        var library = new PatternShapingLibrary(frame, h, v);
        Assert.Equal(8, library.Patterns.Count);
        Assert.Same(frame, library.Frame); Assert.Same(h, library.HorizontalReach); Assert.Same(v, library.VerticalReach);
        Assert.DoesNotContain(Traverse(library.Surface), element => element is Carrier or CarrierLocation);
        var directions = new Dictionary<IElement, IElement>(ReferenceEqualityComparer.Instance);
        var repeated = false;
        foreach (var entry in library.Patterns)
        {
            Assert.Same(frame, entry.Fixture.Frame);
            Assert.InRange(entry.PhaseCount, 4, 20); Assert.Equal(E(entry.Words).Length, entry.PhaseCount);
            Assert.Same(entry.Fixture.Surface, entry.Surface.Left); Assert.Same(entry.Words, entry.Surface.Right);
            Assert.True(Contains(library.Surface, entry.Surface));
            Assert.Contains(h, entry.Fixture.Reaches); Assert.Contains(v, entry.Fixture.Reaches);
            foreach (var word in E(entry.Words).Cast<Relation>())
            {
                Assert.True(StructuralConstraint.TryReadScope(word, out var moves, out var seam, out var tail)); Assert.Same(seam, tail);
                Assert.NotEmpty(moves);
                foreach (var move in moves.Cast<Relation>())
                {
                    Assert.True(frame.Direction(move, out var axis, out _, out var reach));
                    Assert.Same(ReferenceEquals(axis, frame.Horizontal) ? h : v, reach);
                    if (directions.TryGetValue(move.Left, out var existing)) Assert.Same(existing, move);
                    else directions.Add(move.Left, move);
                }
                if (moves.Length > 1)
                { Assert.All(moves, move => Assert.Same(moves[0], move)); repeated = true; }
            }
        }
        Assert.True(repeated); Assert.Equal(4, directions.Count);
    }

    [Fact]
    public void NativeExecutionEmitsTheAuthoredWordsInOrderAndUsesFreshSampleEvents()
    {
        var library = new PatternShapingLibrary();
        foreach (var entry in library.Patterns)
        {
            var trace = entry.Fixture.Run(entry.PhaseCount * 2);
            Assert.True(entry.Fixture.TryReadTrace(trace, out _, out _, out _, out _, out var samples, out var pending));
            Assert.Empty(E(pending)); Assert.Equal(entry.PhaseCount * 2, E(samples).Length);
            var events = new HashSet<IElement>(ReferenceEqualityComparer.Instance);
            var expectedWords = E(entry.Words).Concat(E(entry.Words)).Cast<Relation>().ToArray();
            foreach (var pair in E(samples).Zip(expectedWords))
            {
                Assert.True(FriezeFixture.TryReadSample((Relation)pair.First, out _, out var occurrence,
                    out _, out _, out var applications, out _));
                Assert.True(events.Add(Assert.IsType<Relation>(occurrence).Left));
                var moves = E(applications).Cast<Relation>().Select(application => application.Right).ToArray();
                Assert.Equal(E(pair.Second).Length, moves.Length);
                Assert.All(moves.Zip(E(pair.Second)), item => Assert.Same(item.Second, item.First));
            }
        }
    }

    [Fact]
    public void EveryAuthoredControlPeriodPassesTheLocalQualitativeRulesWithoutCalibration()
    {
        var library = new PatternShapingLibrary();
        foreach (var entry in library.Patterns)
        {
            var rules = new FriezeStructuralRules(entry.Fixture, Slots(20));
            var trace = entry.Fixture.Run(entry.PhaseCount);
            var read = rules.Evaluate(trace);
            Assert.True(rules.TryReadResult(read, out var passed, out var violations, out var pending));
            Assert.NotEmpty(E(passed)); Assert.Empty(E(violations)); Assert.Empty(E(pending));
            Assert.True(rules.TryReadPeriod(read, out var samples, out var displacement));
            Assert.Equal(entry.PhaseCount, E(samples).Length);
            Assert.True(library.Frame.TryReadResult(displacement, out var residual, out _, out var unresolved));
            Assert.Empty(E(unresolved)); Assert.NotEmpty(E(residual));
            Assert.All(E(residual).Cast<Relation>(), move => Assert.Same(library.Frame.Right, move.Left));
            Assert.DoesNotContain(Traverse(read), element => element is Carrier or CarrierLocation);
        }
    }

    [Theory]
    [InlineData(4, 4)]
    [InlineData(6, 4)]
    public void ExplicitPositiveEmbeddingsHaveRightwardPeriodsAndNoPeriodicRetracingOrCrossing(int horizontal, int vertical)
    {
        var library = new PatternShapingLibrary();
        foreach (var entry in library.Patterns)
        {
            var trace = entry.Fixture.Run(entry.PhaseCount);
            var calibration = FriezeRealization.CreateCalibration(entry.Fixture,
                FriezeRealization.DefaultLengths(entry.Fixture, horizontal, vertical));
            var read = FriezeMeasuredRules.Evaluate(entry.Fixture, trace, calibration);
            var summary = FriezeMeasuredRules.Describe(read);
            Assert.True(summary.PeriodX > 0, entry.Name); Assert.Equal(0, summary.PeriodY);
            Assert.True(summary.Height > 0, entry.Name);
            Assert.Equal(0, summary.OverlapCount); Assert.Equal(0, summary.CrossingCount); Assert.Equal(0, summary.UnresolvedCount);
            Assert.True(Contains(read, trace)); Assert.True(Contains(read, calibration));
            Assert.DoesNotContain(Traverse(entry.Surface), element => element is Carrier or CarrierLocation);
        }
    }

    [Fact]
    public void CenteringChangesOnlyTheNativeStartingPhaseAndRetainsItsParentEdit()
    {
        var library = new PatternShapingLibrary(); var parent = library.Patterns[2]; var child = library.CenteredMeander;
        Assert.Same(parent.Fixture.Network, child.Network); Assert.Same(parent.Fixture.OutputLaw, child.OutputLaw);
        Assert.Same(parent.Fixture.Sampling, child.Sampling); Assert.Same(parent.Fixture.Frame, child.Frame);
        Assert.Same(parent.Fixture.Reaches, child.Reaches); Assert.NotSame(parent.Fixture.Starts, child.Starts);
        Assert.Same(library.CenteringEdit, Assert.Single(E(library.Edits)));
        var correspondence = Assert.IsType<Relation>(library.CenteringEdit.Left);
        Assert.Same(parent.Fixture.Surface, correspondence.Left); Assert.Same(child.Surface, correspondence.Right);
        var bindingChange = Assert.IsType<Relation>(library.CenteringEdit.Right);
        Assert.Same(parent.Fixture.Starts, bindingChange.Left); Assert.Same(child.Starts, bindingChange.Right);
        Assert.True(Contains(library.Surface, child.Surface));

        var oldPoints = Points(parent.Fixture, parent.PhaseCount, 4, 4);
        var newPoints = Points(child, parent.PhaseCount, 4, 4);
        Assert.Equal(0, oldPoints.Min(point => point.Y)); Assert.Equal(8, oldPoints.Max(point => point.Y));
        Assert.Equal(-4, newPoints.Min(point => point.Y)); Assert.Equal(4, newPoints.Max(point => point.Y));
        Assert.Equal(oldPoints[^1], newPoints[^1]);

        Assert.True(child.TryReadTrace(child.Run(parent.PhaseCount), out _, out _, out _, out _, out var samples, out var pending));
        Assert.Empty(E(pending));
        foreach (var pair in E(samples).Zip(E(library.CenteredMeanderWords)))
        {
            Assert.True(FriezeFixture.TryReadSample((Relation)pair.First, out _, out _, out _, out _, out var apps, out _));
            var actual = E(apps).Cast<Relation>().Select(app => app.Right).ToArray();
            Assert.Equal(E((Relation)pair.Second).Length, actual.Length);
            Assert.All(actual.Zip(E((Relation)pair.Second)), item => Assert.Same(item.Second, item.First));
        }
    }

    [Fact]
    public void AnIncompleteShapedPeriodDoesNotInventARepeatOrMetricAnswer()
    {
        var library = new PatternShapingLibrary(); var entry = library.Patterns[^1];
        var rules = new FriezeStructuralRules(entry.Fixture, Slots(20));
        var read = rules.Evaluate(entry.Fixture.Run(entry.PhaseCount - 1));
        Assert.True(rules.TryReadResult(read, out _, out _, out var pending)); Assert.NotEmpty(E(pending));
        Assert.False(rules.TryReadPeriod(read, out _, out _));
        var trace = entry.Fixture.Run(entry.PhaseCount);
        var missing = FriezeRealization.CreateCalibration(entry.Fixture, new Dictionary<IElement, long>());
        Assert.False(FriezeRealization.TryRealize(entry.Fixture, trace, missing, out var measured));
        Assert.True(FriezeRealization.TryRead(measured, out _, out _, out _, out var unresolved));
        Assert.NotEmpty(E(unresolved)); Assert.True(Contains(measured, missing));
    }

    private static (long X, long Y)[] Points(FriezeFixture fixture, int rounds, long h, long v)
    {
        var trace = fixture.Run(rounds);
        var calibration = FriezeRealization.CreateCalibration(fixture, FriezeRealization.DefaultLengths(fixture, h, v));
        Assert.True(FriezeRealization.TryRealize(fixture, trace, calibration, out var read));
        Assert.True(FriezeRealization.TryRead(read, out _, out _, out var clauses, out _));
        return E(clauses).Cast<Relation>().Select(clause =>
        {
            Assert.True(FriezeRealization.TryReadSamplePoints(clause, out _, out _, out var point));
            Assert.True(FriezeRealization.TryReadPoint(point, out var x, out var y)); return (x, y);
        }).ToArray();
    }

    private static Relation Slots(int count) => StructuralConstraint.Closed(Enumerable.Range(0, count).Select(_ => (IElement)new Placeholder()).ToArray());
    private static IElement[] E(Relation scope) => StructuralConstraint.TryReadScope(scope, out var entries, out _, out _) ? entries : [];
    private static bool Contains(IElement root, IElement target) => Traverse(root).Any(item => ReferenceEquals(item, target));
    private static IEnumerable<IElement> Traverse(IElement root)
    {
        var seen = new HashSet<IElement>(ReferenceEqualityComparer.Instance); var pending = new Stack<IElement>(); pending.Push(root);
        while (pending.Count != 0)
        {
            var item = pending.Pop(); if (!seen.Add(item)) continue; yield return item;
            if (item is Relation relation) { pending.Push(relation.Left); pending.Push(relation.Right); }
        }
    }
}
