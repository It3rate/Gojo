using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeFrieze;

namespace Gojo.Experiments.QualitativeExecution.Tests;

public sealed class FriezeDriveAnalysisTests
{
    [Fact]
    public void GroupedZigzagRetainsIndependentReceiptCausesAndAxisContributions()
    {
        var fixture = FriezeFixture.CombinedDriveZigzag(); var trace = fixture.Run(2);
        var analysis = FriezeDriveAnalysis.Evaluate(fixture, trace);
        Assert.True(FriezeDriveAnalysis.TryReadResult(fixture, analysis, out var samples, out var pending));
        Assert.Empty(E(pending)); Assert.Equal(2, E(samples).Length);
        foreach (var row in E(samples).Cast<Relation>())
        {
            Assert.True(FriezeDriveAnalysis.TryReadSample(fixture, row, out var sample, out var route,
                out var apps, out var axes, out var delta, out var position, out var unresolved));
            Assert.Same(fixture.Network.Surface, route.Left); Assert.Empty(E(unresolved)); Assert.Equal(2, E(apps).Length);
            Assert.True(FriezeFixture.TryReadSample(sample, out var cause, out var occurrence, out _, out _, out _, out _));
            Assert.True(fixture.Network.TryReadStep(Assert.IsType<Relation>(cause), out _, out _, out _, out var accepted, out _, out _));
            Assert.Equal(3, E(accepted).Length); // Controller, horizontal and vertical each advance.
            var receipts = new List<IElement>();
            foreach (var app in E(apps).Cast<Relation>())
            {
                var source = Assert.IsType<Relation>(app.Left); var receipt = Assert.IsType<Relation>(source.Left);
                var component = fixture.Components.Single(clock => receipt.Left is Relation owner && ReferenceEquals(owner.Right, clock.Surface));
                Assert.True(component.TryReadAccepted(receipt, out _, out var eventIdentity, out _));
                Assert.NotSame(Assert.IsType<Relation>(occurrence).Left, eventIdentity);
                Assert.Contains(receipt, E(accepted)); Assert.DoesNotContain(receipt, receipts); receipts.Add(receipt);
                Assert.True(FiniteLaw.TryReadResult(Assert.IsType<Relation>(source.Right), out _, out _, out _));
            }
            Assert.Equal(2, E(axes).Length);
            foreach (var contribution in E(axes).Cast<Relation>())
            {
                Assert.True(FriezeDriveAnalysis.TryReadContribution(fixture, contribution, out var axis, out var actual, out var read));
                var app = Assert.IsType<Relation>(Assert.Single(E(actual))); Assert.Contains(app, E(apps));
                Assert.True(fixture.Frame.Direction(Assert.IsType<Relation>(app.Right), out var actualAxis, out _, out _)); Assert.Same(axis, actualAxis);
                Assert.True(fixture.Frame.TryReadResult(read, out var residual, out var canceled, out var unknown));
                Assert.Same(app.Right, Assert.Single(E(residual))); Assert.Empty(E(canceled)); Assert.Empty(E(unknown));
            }
            Assert.True(fixture.Frame.TryReadResult(delta, out var displacement, out _, out _)); Assert.Equal(2, E(displacement).Length);
            Assert.True(fixture.Frame.TryReadResult(position, out _, out _, out _));
        }
        Assert.True(FriezeDriveAnalysis.TryReadSample(fixture, (Relation)E(samples)[^1], out _, out _, out _, out _, out _, out var end, out _));
        Assert.True(fixture.Frame.TryReadResult(end, out var retained, out var cancellations, out _));
        Assert.Equal(2, E(retained).Length); Assert.Single(E(cancellations));
        Assert.All(E(retained).Cast<Relation>(), move => Assert.Same(fixture.Frame.Right, move.Left));
        AssertNoNumbers(analysis);
    }

    [Fact]
    public void ChangingOnlySamplingProducesDiagonalsOrStaircaseWithEqualRootEndpoints()
    {
        var grouped = FriezeFixture.CombinedDriveZigzag();
        var individual = grouped.WithSampling(ComponentPorts(grouped), "Same drives — component samples");
        Assert.Same(grouped.Network, individual.Network); Assert.Same(grouped.OutputLaw, individual.OutputLaw);
        Assert.Same(grouped.Starts, individual.Starts); Assert.Same(grouped.Reaches, individual.Reaches);
        var a = grouped.Run(4); var b = individual.Run(4);
        var combined = Points(grouped, a); var separated = Points(individual, b);
        Assert.Equal(new[] { (4L, 4L), (8L, 0L), (12L, 4L), (16L, 0L) }, combined);
        Assert.Equal(new[] { (4L, 0L), (4L, 4L), (8L, 4L), (8L, 0L), (12L, 0L), (12L, 4L), (16L, 4L), (16L, 0L) }, separated);
        Assert.Equal(combined, separated.Where((_, index) => index % 2 == 1).ToArray());
        Assert.True(grouped.TryReadTrace(a, out _, out _, out _, out var aa, out _, out _));
        Assert.True(individual.TryReadTrace(b, out _, out _, out _, out var bb, out _, out _));
        Assert.All(E(aa).Zip(E(bb)), pair => Assert.Same(((Relation)pair.First).Right, ((Relation)pair.Second).Right));
        var read = FriezeDriveAnalysis.Evaluate(individual, b);
        Assert.True(FriezeDriveAnalysis.TryReadResult(individual, read, out var samples, out var pending));
        Assert.Empty(E(pending)); Assert.Equal(8, E(samples).Length);
        foreach (var clause in E(samples).Cast<Relation>())
        {
            Assert.True(FriezeDriveAnalysis.TryReadSample(individual, clause, out _, out var route, out var apps, out _, out _, out _, out _));
            Assert.NotSame(individual.Network.Surface, route.Left); Assert.Single(E(apps));
        }
        AssertNoNumbers(new Relation(a, read));
    }

    [Fact]
    public void SlowerVerticalDriveAndNativePhaseOffsetRemainDifferentSchedules()
    {
        var slower = FriezeFixture.CombinedDriveZigzag(slowerVertical: true);
        var controller = slower.Components.Single(clock => ReferenceEquals(slower.RootPort.Right, clock.Surface));
        Assert.Equal(2, E(controller.Domain).Length);
        var starts = S(E(slower.Starts).Cast<Relation>().Select(row => ReferenceEquals(row.Left, slower.RootPort)
            ? (IElement)new Relation(row.Left, S(E(controller.Domain)[0])) : row).ToArray());
        var offset = slower.WithStarts(starts, "Same drives — alternate root origin");
        Assert.Same(slower.Network, offset.Network); Assert.Same(slower.OutputLaw, offset.OutputLaw);
        var first = slower.Run(4); var second = offset.Run(4);
        Assert.Equal(new[] { (4L, 4L), (8L, 4L), (12L, 0L), (16L, 0L) }, Points(slower, first));
        Assert.Equal(new[] { (4L, 0L), (8L, 4L), (12L, 4L), (16L, 0L) }, Points(offset, second));
        foreach (var pair in new[] { (slower, first), (offset, second) })
        {
            var read = FriezeDriveAnalysis.Evaluate(pair.Item1, pair.Item2);
            Assert.True(FriezeDriveAnalysis.TryReadResult(pair.Item1, read, out var samples, out var pending)); Assert.Empty(E(pending));
            var counts = E(samples).Cast<Relation>().Select(clause =>
            {
                Assert.True(FriezeDriveAnalysis.TryReadSample(pair.Item1, clause, out _, out _, out var apps, out _, out _, out _, out _));
                return E(apps).Length;
            }).ToArray();
            Assert.Equal(2, counts.Count(count => count == 2)); Assert.Equal(2, counts.Count(count => count == 1));
            AssertNoNumbers(read);
        }
    }

    [Fact]
    public void CancellationKeepsTheContributingApplicationsWhileRetainingOnlyItsNetStroke()
    {
        var frame = new QualitativeMotionFrame(); var h = new Placeholder(); var v = new Placeholder();
        var right = QualitativeMotionFrame.Move(frame.Right, h); var up = QualitativeMotionFrame.Move(frame.Up, v); var down = QualitativeMotionFrame.Move(frame.Down, v);
        var fixture = FriezeFixture.Sequence(frame, S(S(right, up, down)));
        var read = FriezeDriveAnalysis.Evaluate(fixture, fixture.Run(1));
        Assert.True(FriezeDriveAnalysis.TryReadResult(fixture, read, out var samples, out var pending)); Assert.Empty(E(pending));
        Assert.True(FriezeDriveAnalysis.TryReadSample(fixture, (Relation)Assert.Single(E(samples)), out _, out _, out var apps,
            out var axes, out var delta, out _, out _)); Assert.Equal(3, E(apps).Length);
        Assert.True(frame.TryReadResult(delta, out var retained, out var cancellations, out _)); Assert.Same(right, Assert.Single(E(retained)));
        var pair = Assert.IsType<Relation>(Assert.Single(E(cancellations))); Assert.Same(up, pair.Left); Assert.Same(down, pair.Right);
        Assert.True(FriezeDriveAnalysis.TryReadContribution(fixture, (Relation)E(axes)[1], out var axis, out var vertical, out var reduction));
        Assert.Same(frame.Vertical, axis); Assert.Equal(2, E(vertical).Length);
        Assert.True(frame.TryReadResult(reduction, out var zero, out var paired, out _)); Assert.Empty(E(zero)); Assert.True(Closed(zero)); Assert.Single(E(paired));
        AssertNoNumbers(read);
    }

    [Fact]
    public void MissingEmissionKeepsUnattributedAxesOpenRatherThanInventingVerticalZero()
    {
        var original = FriezeFixture.CombinedDriveZigzag();
        Assert.True(FiniteLaw.TryRead(original.OutputLaw, out var a, out var b, out var output, out var rows));
        var upRow = rows.Single(row => row.Right is Relation support && E(support) is [Relation word] &&
            E(word) is [Relation move] && ReferenceEquals(move.Left, original.Frame.Up));
        var fixture = original.WithOutputs(FiniteLaw.Create(a, b, output, rows.Where(row => !ReferenceEquals(row, upRow)).ToArray()));
        var trace = fixture.Run(1); var read = FriezeDriveAnalysis.Evaluate(fixture, trace);
        Assert.True(FriezeDriveAnalysis.TryReadResult(fixture, read, out var samples, out var pending)); Assert.NotEmpty(E(pending));
        Assert.True(FriezeDriveAnalysis.TryReadSample(fixture, (Relation)Assert.Single(E(samples)), out _, out _, out var apps,
            out var axes, out var delta, out _, out var unknown)); Assert.NotEmpty(E(unknown));
        Assert.Equal(2, E(apps).Length); Assert.Contains(E(apps).Cast<Relation>(), app => app.Right is Placeholder);
        Assert.True(fixture.Frame.TryReadResult(delta, out var displacement, out _, out _)); Assert.False(Closed(displacement));
        foreach (var row in E(axes).Cast<Relation>())
        {
            Assert.True(FriezeDriveAnalysis.TryReadContribution(fixture, row, out _, out _, out var contribution));
            Assert.True(fixture.Frame.TryReadResult(contribution, out var residual, out _, out var unsettled));
            Assert.False(Closed(residual)); Assert.NotEmpty(E(unsettled));
        }
        var forged = new Relation(read.Left, new Relation(samples, S()));
        Assert.False(FriezeDriveAnalysis.TryReadResult(fixture, forged, out _, out _)); AssertNoNumbers(read);
    }

    [Fact]
    public void BudgetIncompleteRootExposesItsPrefixWithoutManufacturingACombinedSample()
    {
        var fixture = FriezeFixture.CombinedDriveZigzag(); var trace = fixture.Run(1, eventBudget: 2);
        Assert.True(fixture.TryReadTrace(trace, out _, out _, out _, out var apps, out _, out _)); Assert.Single(E(apps));
        var read = FriezeDriveAnalysis.Evaluate(fixture, trace);
        Assert.True(FriezeDriveAnalysis.TryReadResult(fixture, read, out var samples, out var pending));
        Assert.Empty(E(samples)); Assert.NotEmpty(E(pending)); Assert.True(Contains(read, Assert.Single(E(apps))));

        // The summary is not the sole source of pending work: an actual queue
        // in its network receipt remains visible after a caller drops that summary.
        var controllerOnly = fixture.Run(1, eventBudget: 1);
        Assert.True(fixture.TryReadTrace(controllerOnly, out _, out var final, out var steps, out var noApps, out var noSamples, out _));
        var stripped = new Relation(controllerOnly.Left, new Relation(final,
            new Relation(steps, new Relation(noApps, new Relation(noSamples, S())))));
        var reconstructed = FriezeDriveAnalysis.Evaluate(fixture, stripped);
        Assert.True(FriezeDriveAnalysis.TryReadResult(fixture, reconstructed, out var stillNone, out var stillPending));
        Assert.Empty(E(stillNone)); Assert.NotEmpty(E(stillPending));
        var falseComplete = new Relation(reconstructed.Left, new Relation(stillNone, S()));
        Assert.False(FriezeDriveAnalysis.TryReadResult(fixture, falseComplete, out _, out _));
    }

    [Fact]
    public void CopiedSuccessAndForeignSampleOrAxisEvidenceCannotCreateAValidAnalysis()
    {
        var fixture = FriezeFixture.CombinedDriveZigzag(); var trace = fixture.Run(2);
        var read = FriezeDriveAnalysis.Evaluate(fixture, trace);
        Assert.True(FriezeDriveAnalysis.TryReadResult(fixture, read, out var samples, out _));
        var first = (Relation)E(samples)[0]; var last = (Relation)E(samples)[1];
        var wrongSample = new Relation(last.Left, first.Right);
        Assert.False(FriezeDriveAnalysis.TryReadSample(fixture, wrongSample, out _, out _, out _, out _, out _, out _, out _));
        var body = Assert.IsType<Relation>(first.Right); var inputs = Assert.IsType<Relation>(body.Left);
        var axes = Assert.IsType<Relation>(inputs.Right);
        var wrongAxes = new Relation(first.Left, new Relation(new Relation(inputs.Left, S(E(axes)[0], E(axes)[0])), body.Right));
        Assert.False(FriezeDriveAnalysis.TryReadSample(fixture, wrongAxes, out _, out _, out _, out _, out _, out _, out _));
        Assert.False(FriezeDriveAnalysis.TryReadResult(FriezeFixture.CombinedDriveZigzag(), read, out _, out _));

        Assert.True(fixture.TryReadTrace(trace, out _, out var final, out var steps, out var apps, out var commits, out var pending));
        var duplicatedApps = S(E(apps).Append(E(apps)[0]).ToArray());
        var fabricatedTrace = new Relation(trace.Left, new Relation(final, new Relation(steps,
            new Relation(duplicatedApps, new Relation(commits, pending)))));
        var held = FriezeDriveAnalysis.Evaluate(fixture, fabricatedTrace);
        Assert.True(FriezeDriveAnalysis.TryReadResult(fixture, held, out var none, out var failed));
        Assert.Empty(E(none)); Assert.Same(fabricatedTrace, Assert.Single(E(failed)));

        // The second sample's algebra still accounts for its real motions, but
        // the earlier root cannot be their sampling cause.
        var originalFirst = (Relation)E(commits)[0]; var originalSecond = (Relation)E(commits)[1];
        var premature = new Relation(originalFirst.Left, originalSecond.Right);
        Assert.True(FriezeFixture.TryReadSample(premature, out _, out _, out _, out _, out _, out var validAlgebra));
        Assert.True(fixture.Frame.TryReadResult(validAlgebra, out _, out _, out _));
        var wrongHistory = new Relation(trace.Left, new Relation(final, new Relation(steps,
            new Relation(apps, new Relation(S(originalFirst, premature), pending)))));
        var rejected = FriezeDriveAnalysis.Evaluate(fixture, wrongHistory);
        Assert.True(FriezeDriveAnalysis.TryReadResult(fixture, rejected, out var noFutureSample, out var futurePending));
        Assert.Empty(E(noFutureSample)); Assert.Same(wrongHistory, Assert.Single(E(futurePending)));
    }

    private static Relation ComponentPorts(FriezeFixture fixture) => S(E(fixture.Network.Ports)
        .Where(port => !ReferenceEquals(port, fixture.RootPort)).ToArray());
    private static (long X, long Y)[] Points(FriezeFixture fixture, Relation trace)
    {
        var calibration = FriezeRealization.CreateCalibration(fixture, FriezeRealization.DefaultLengths(fixture, 4, 4));
        Assert.True(FriezeRealization.TryRealize(fixture, trace, calibration, out var read));
        Assert.True(FriezeRealization.TryRead(read, out _, out _, out var clauses, out _));
        return E(clauses).Cast<Relation>().Select(clause =>
        {
            Assert.True(FriezeRealization.TryReadSamplePoints(clause, out _, out _, out var point));
            Assert.True(FriezeRealization.TryReadPoint(point, out var x, out var y)); return (x, y);
        }).ToArray();
    }
    private static void AssertNoNumbers(IElement element) => Assert.DoesNotContain(Traverse(element), item => item is Carrier or CarrierLocation);
    private static bool Contains(IElement source, IElement target) => Traverse(source).Any(item => ReferenceEquals(item, target));
    private static Relation S(params IElement[] entries) => StructuralConstraint.Closed(entries);
    private static IElement[] E(Relation scope) => StructuralConstraint.TryReadScope(scope, out var entries, out _, out _) ? entries : [];
    private static bool Closed(Relation scope) => StructuralConstraint.TryReadScope(scope, out _, out var seam, out var tail) && seam.Equals(tail);
    private static IEnumerable<IElement> Traverse(IElement source)
    {
        var pending = new Stack<IElement>(); var seen = new HashSet<IElement>(ReferenceEqualityComparer.Instance); pending.Push(source);
        while (pending.Count != 0)
        {
            var item = pending.Pop(); if (!seen.Add(item)) continue; yield return item;
            if (item is Relation relation) { pending.Push(relation.Left); pending.Push(relation.Right); }
        }
    }
}
