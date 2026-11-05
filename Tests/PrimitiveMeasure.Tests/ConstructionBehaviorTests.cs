using Gojo.Experiments.PrimitiveMeasure;

namespace Gojo.Experiments.PrimitiveMeasure.Tests;

public sealed class ConstructionBehaviorTests
{
    [Fact]
    public void AllTrialsOwnDecodedRemaindersAndPreserveOneActivationAcrossEveryPublicSupport()
    {
        Assert.Equal(12, ConstructionBehavior.Cases.Count);
        var pulses = 0;
        for (var trial = 0; trial < ConstructionBehavior.Cases.Count; trial++)
            foreach (byte support in new byte[] { 1, 2, 3, 0 })
            {
                var run = new ConstructionBehavior(trial);
                var source = ConstructionAtlas.All[run.Case.Source].Surface;
                var retained = run.Example.Surface;
                var saved = Bytes(retained);
                Assert.Null(retained.Basis);
                Assert.NotSame(source.Table, retained.Table);
                Assert.NotSame(source.Table.Template, retained.Table.Template);
                Assert.NotSame(source.Uses, retained.Uses);
                Assert.Same(retained.Table.Template, retained.Uses.Template);
                Assert.Equal(Bytes(source), saved);
                Assert.Same(retained.Table, run.Walk.Family);
                Assert.Same(retained.Table, run.Adjustment.Table);
                var clues = ConstructionEvidence.Read(retained);
                Assert.Null(clues.Values[17]);
                Assert.Null(clues.Values[18]);
                var dialStore = run.Adjustment.StoreBytes();
                var before = run.Walk.ValueBytes();
                run.SetRouteObservation(support);
                Assert.Equal(before, run.Walk.ValueBytes());
                for (var beat = 0; beat < 9; beat++)
                {
                    run.Walk.Pulse();
                    pulses++;
                    Assert.Equal(1, run.Walk.OccupiedStateCount);
                    Assert.Equal(beat + 1, run.Walk.Trace.Count);
                    Assert.Equal(saved, Bytes(retained));
                    Assert.Equal(saved, Bytes(source));
                    Assert.Equal(dialStore, run.Adjustment.StoreBytes());
                }
                Assert.Empty(run.Adjustment.Trace);
            }
        Assert.Equal(432, pulses);
    }

    [Fact]
    public void TwoPushTrialsConserveTheirRequestAndActualSupportFitsExecutionPrediction()
    {
        var attempts = 0;
        var moves = 0;
        var waits = 0;
        for (var trial = 0; trial < ConstructionBehavior.Cases.Count; trial++)
            foreach (var initiallyHidden in new[] { false, true })
            {
                var run = new ConstructionBehavior(trial);
                var saved = Bytes(run.Example.Surface);
                var walkStore = run.Walk.ValueBytes();
                Assert.True(run.Adjustment.ReservePresent);
                Assert.False(run.Adjustment.Waiting);
                Assert.Empty(run.Adjustment.AffectedFields());
                for (var beat = 0; beat < 2; beat++)
                {
                    var conceal = initiallyHidden && beat == 0;
                    var store = run.Adjustment.StoreBytes();
                    var prediction = run.Adjustment.Predict(conceal);
                    Assert.Equal(store, run.Adjustment.StoreBytes());
                    var step = run.Adjustment.Push(conceal);
                    attempts++;
                    moves += step.Moved ? 1 : 0;
                    waits += step.Waiting ? 1 : 0;
                    Assert.Equal(prediction.BeforeSupport, step.Prediction.BeforeSupport);
                    Assert.Equal(prediction.AfterSupport, step.Prediction.AfterSupport);
                    Assert.NotEqual(0U, step.ActualScopeSupport);
                    Assert.Equal(0U, step.ActualScopeSupport & ~step.Prediction.AfterSupport);
                    Assert.Equal(1, RequestCount(run));
                    Assert.False(run.Adjustment.ReservePresent);
                    Assert.Equal(saved, Bytes(run.Example.Surface));
                    Assert.Equal(walkStore, run.Walk.ValueBytes());
                    Assert.Empty(run.Walk.Trace);
                    if (conceal && run.Adjustment.HasResponse)
                    {
                        Assert.False(step.SettlementCommitted);
                        Assert.False(step.Moved);
                        Assert.True(step.Waiting);
                        Assert.NotEqual(step.Prediction.AfterSupport, step.Prediction.SettlementSupport);
                    }
                }
                Assert.Equal(2, run.Adjustment.Trace.Count);
                if (run.Case.Pin || run.Case.ResponseField is null)
                {
                    Assert.True(run.Adjustment.Waiting);
                    Assert.Empty(run.Adjustment.AffectedFields());
                    Assert.All(run.Adjustment.Trace, step => Assert.False(step.Moved));
                }
                else
                {
                    Assert.False(run.Adjustment.Waiting);
                    Assert.Equal(new[] { run.Case.ResponseField!.Value }, run.Adjustment.AffectedFields());
                    Assert.Single(run.Adjustment.Trace, step => step.Moved);
                    Assert.Equal(initiallyHidden, run.Adjustment.Trace[1].Moved);
                }
            }
        Assert.Equal(48, attempts);
        Assert.Equal(20, moves);
        Assert.Equal(18, waits);
    }

    [Fact]
    public void SharedHouseSeamBroadcastsOneDialReadToTwoGroupsWhileDuplicateSeamsDoNot()
    {
        var shared = Trial("House · shared seam");
        var independent = Trial("House · independent seams");
        Assert.True(shared.Adjustment.Push().Moved);
        Assert.True(independent.Adjustment.Push().Moved);
        Assert.Equal(new[] { 0 }, shared.Adjustment.AffectedFields());
        Assert.Equal(new[] { 0 }, independent.Adjustment.AffectedFields());
        Assert.Equal(new[] { 0 }, shared.Adjustment.AffectedLinks());
        Assert.Equal(new[] { 0 }, independent.Adjustment.AffectedLinks());
        Assert.Equal(new[] { 0, 1 }, shared.Adjustment.AffectedGroups());
        Assert.Equal(new[] { 0 }, independent.Adjustment.AffectedGroups());
        Assert.Equal(1, RequestCount(shared));
        Assert.Equal(1, RequestCount(independent));
        Assert.Equal(1, shared.Walk.OccupiedStateCount);
        Assert.Equal(1, independent.Walk.OccupiedStateCount);
        Assert.Empty(shared.Walk.Trace);
        Assert.Empty(independent.Walk.Trace);
    }

    [Fact]
    public void IdenticalRetainedPathAndRadialFormsExecuteIdenticallyWithoutTheirBuilders()
    {
        var path = Trial("Path · common reach");
        var radial = Trial("Radial result · builder absent");
        Assert.NotEqual(path.Example.Method, radial.Example.Method);
        Assert.Null(path.Example.Surface.Basis);
        Assert.Null(radial.Example.Surface.Basis);
        Assert.Equal(Bytes(path.Example.Surface), Bytes(radial.Example.Surface));
        Assert.Equal(path.Walk.RouteBytes(), radial.Walk.RouteBytes());
        Assert.Equal(path.Adjustment.FieldBytes(), radial.Adjustment.FieldBytes());
        for (var pulse = 0; pulse < 9; pulse++)
        {
            var first = path.Walk.Pulse(); var second = radial.Walk.Pulse();
            Assert.Equal(first.Moved, second.Moved);
            Assert.Equal(first.Committed, second.Committed);
            Assert.Equal(first.Before, second.Before);
            Assert.Equal(first.After, second.After);
            Assert.Equal(first.Prediction.Targets, second.Prediction.Targets);
            Assert.Equal(path.Walk.ValueBytes(), radial.Walk.ValueBytes());
            Assert.Equal(path.Walk.Trace[^1].CompatibleUses, radial.Walk.Trace[^1].CompatibleUses);
        }
        for (var push = 0; push < 2; push++)
        {
            var first = path.Adjustment.Push(push == 0);
            var second = radial.Adjustment.Push(push == 0);
            Assert.Equal(first.Moved, second.Moved);
            Assert.Equal(first.Waiting, second.Waiting);
            Assert.Equal(first.Prediction.AfterSupport, second.Prediction.AfterSupport);
            Assert.Equal(first.ActualScopeSupport, second.ActualScopeSupport);
            Assert.Equal(first.AfterTips, second.AfterTips);
            Assert.Equal(path.Adjustment.StoreBytes(), radial.Adjustment.StoreBytes());
        }
        Assert.Equal(new[] { 0, 1, 2, 3 }, path.Adjustment.AffectedLinks());
        Assert.Equal(new[] { 0, 1, 2, 3 }, radial.Adjustment.AffectedLinks());
    }

    [Theory]
    [InlineData(1, 2, 2)]
    [InlineData(2, 3, 4)]
    public void SelectedYWaitsForAnUncertainPublicationThenUsesTheSameHeldActivation(int revealed, int target, int signedUse)
    {
        var run = Trial("Y · explicit selector");
        run.SetRouteObservation(3);
        run.Walk.Pulse(); run.Walk.Pulse();
        Assert.Equal(1, run.Walk.ActiveFamilySite);
        var waiting = run.Walk.ValueBytes();
        var step = run.Walk.Pulse();
        Assert.False(step.Committed);
        Assert.False(step.Prediction.Resolved);
        Assert.Equal(new[] { run.Walk.StateForSite(2), run.Walk.StateForSite(3) }, step.Prediction.Targets);
        Assert.Equal(waiting, run.Walk.ValueBytes());
        Assert.Equal(1, run.Walk.OccupiedStateCount);
        run.SetRouteObservation((byte)revealed);
        Assert.Equal(waiting, run.Walk.ValueBytes());
        Assert.True(run.Walk.Pulse().Moved);
        Assert.Equal(target, run.Walk.ActiveFamilySite);
        Assert.Equal(new[] { signedUse }, run.Walk.Trace[^1].CompatibleUses);
        Assert.Equal(1, run.Walk.OccupiedStateCount);
        Assert.False(run.Walk.Trace[2].Step.Committed);
        Assert.True(run.Walk.Trace[^1].Step.Committed);
    }

    [Fact]
    public void BuilderRemovalRetainsPathAndRailHintsWithoutRetainingTheirGeometricProof()
    {
        var path = Trial("Path · common reach");
        var rails = Trial("Parallel sides · common reach");
        var radial = Trial("Radial result · builder absent");
        Assert.Equal(1, path.Example.Surface.Uses.GroupCount);
        Assert.Equal(4, rails.Example.Surface.Uses.GroupCount);
        Assert.Equal(1, radial.Example.Surface.Uses.GroupCount);
        var first = ConstructionEvidence.Read(path.Example.Surface);
        var second = ConstructionEvidence.Read(rails.Example.Surface);
        Assert.Equal(1, first.Values[5]);
        Assert.Equal(4, second.Values[5]);
        Assert.Equal(1, first.Values[7]);
        Assert.Equal(0, second.Values[7]);
        Assert.Equal(first.Values, ConstructionEvidence.Read(radial.Example.Surface).Values);
        foreach (var run in new[] { path, rails, radial })
        {
            var clues = ConstructionEvidence.Read(run.Example.Surface);
            Assert.Equal(0, clues.Values[14]);
            Assert.Equal(0, clues.Values[15]);
            Assert.Equal(0, clues.Values[16]);
            Assert.Null(clues.Values[17]);
            Assert.Null(clues.Values[18]);
        }
        path.Walk.Pulse(); rails.Walk.Pulse();
        Assert.True(path.Walk.Pulse().Moved);
        Assert.True(rails.Walk.Pulse().Prediction.Conflict);
    }

    [Fact]
    public void ChangingOneRuntimeDoesNotSupplyInputOrChooseAnOutcomeInTheOther()
    {
        var run = Trial("Y · explicit selector");
        var walk = run.Walk.ValueBytes();
        run.Adjustment.Push(concealControl: true);
        Assert.True(run.Adjustment.Waiting);
        Assert.Equal(walk, run.Walk.ValueBytes());
        run.SetRouteObservation(2);
        Assert.Equal(walk, run.Walk.ValueBytes());
        var heldAdjustment = run.Adjustment.StoreBytes();
        run.Walk.Pulse(); run.Walk.Pulse(); run.Walk.Pulse();
        Assert.Equal(3, run.Walk.ActiveFamilySite);
        Assert.Equal(heldAdjustment, run.Adjustment.StoreBytes());
        Assert.True(run.Adjustment.Waiting);
        var routedWalk = run.Walk.ValueBytes();
        Assert.True(run.Adjustment.Push().Moved);
        Assert.Equal(routedWalk, run.Walk.ValueBytes());
        Assert.Equal(1, RequestCount(run));
        Assert.Equal(1, run.Walk.OccupiedStateCount);
    }

    [Fact]
    public void SurveyExportsTheWholeMatrixAndPreservesItsAccountingAssertions()
    {
        var folder = Path.Combine(Path.GetTempPath(), "gojo-behavior-test-" + Guid.NewGuid().ToString("N"));
        try
        {
            BehaviorSurvey.Export(folder);
            Assert.Equal(new[] { "adjustments.csv", "summary.txt", "walks.csv" },
                Directory.GetFiles(folder).Select(Path.GetFileName).Order().ToArray());
            Assert.Equal(433, File.ReadAllLines(Path.Combine(folder, "walks.csv")).Length);
            Assert.Equal(49, File.ReadAllLines(Path.Combine(folder, "adjustments.csv")).Length);
            var summary = File.ReadAllText(Path.Combine(folder, "summary.txt"));
            Assert.Contains("12 authored behavior cases", summary);
            Assert.Contains("432 route pulses", summary);
            Assert.Contains("48 adjustment attempts: 20 reach changes; 18 observations retain a waiting request", summary);
            Assert.Contains("No builder names, geometric bases or drawing coordinates enter either executor", summary);
        }
        finally
        {
            if (Directory.Exists(folder)) Directory.Delete(folder, recursive: true);
        }
    }

    private static byte[] Bytes(ConstructionSurface surface) => FamilyUseCodec.Encode(surface.Table, surface.Entry, surface.Uses);
    private static int RequestCount(ConstructionBehavior run) => (run.Adjustment.ReservePresent ? 1 : 0) +
        (run.Adjustment.Waiting ? 1 : 0) + run.Adjustment.AffectedFields().Length;
    private static ConstructionBehavior Trial(string title) => new(Enumerable.Range(0, ConstructionBehavior.Cases.Count)
        .Single(i => ConstructionBehavior.Cases[i].Title == title));
}
