using Gojo.Core;
using Gojo.Patterns;
using Gojo.Equations;
using Gojo.Experiments.QualitativeFrieze;

namespace Gojo.Experiments.QualitativeExecution.Tests;

public sealed class FriezeLearningTests
{
    private static readonly Lazy<IReadOnlyList<FriezeLearning.RunResult>> Runs = new(() => FriezeLearning.Run());

    [Fact]
    public void ExhaustedSearchDoesNotPromoteAnIllegalCandidateIntoASuccess()
    {
        foreach (var run in FriezeLearning.Run(1))
        {
            Assert.Same(run.Initial, run.Best);
            Assert.True(run.Best.HardFailures > 0);
            Assert.Null(run.Best.Calibration); Assert.Null(run.Best.Coverage);
            Assert.Equal(0, run.MeasuredQueries); Assert.Equal(0, run.CoverageQueries);
            Assert.Empty(E(run.History)); Assert.Empty(E(run.ArchiveHistory));
        }
    }

    [Fact]
    public void FeedbackSearchFindsHardValidProgramsWithoutNumericsInTheirSources()
    {
        foreach (var run in Runs.Value)
        {
            Assert.True(run.Initial.HardFailures > 0);
            Assert.Equal(0, run.Best.HardFailures);
            Assert.NotNull(run.Best.Coverage);
            Assert.Equal(0, run.Best.Coverage.Projection.Unresolved);
            Assert.True(run.StructuralRejected > 0);
            Assert.True(run.CoverageQueries < run.Candidates.Count);
            Assert.True(run.MeasuredQueries < run.Candidates.Count * 2);
            AssertNoNumbers(run.Best.Fixture.Surface);
            Assert.Same(run.Initial.Fixture.Network, run.Best.Fixture.Network);
            Assert.Same(run.Initial.Fixture.Frame, run.Best.Fixture.Frame);
            Assert.Same(run.Initial.Fixture.RootPort, run.Best.Fixture.RootPort);
            Assert.True(run.Best.Generation > 0);
            var geometry = FriezeMeasuredRules.Describe(run.Best.MeasuredRead!);
            Assert.Equal(0, geometry.OverlapCount);
            if (!run.AllowCrossings) Assert.Equal(0, geometry.CrossingCount);
        }
    }

    [Fact]
    public void EveryProposalRetainsItsActualBindingEditEvaluationAndSelection()
    {
        foreach (var run in Runs.Value)
        {
            Assert.Equal(run.Candidates.Count - 1, E(run.History).Length);
            foreach (var entry in E(run.History).Cast<Relation>())
            {
                var proposal = Assert.IsType<Relation>(entry.Left);
                var edit = Assert.IsType<Relation>(proposal.Left);
                var laws = Assert.IsType<Relation>(edit.Left);
                var evaluation = Assert.Single(run.Candidates, candidate => ReferenceEquals(candidate.Surface, proposal.Right));
                Assert.Same(evaluation.Parent!.Fixture.OutputLaw, laws.Left);
                Assert.Same(evaluation.Fixture.OutputLaw, laws.Right);
                Assert.NotEmpty(E(Assert.IsType<Relation>(edit.Right)));
                var choice = Assert.IsType<Relation>(entry.Right);
                var compared = Assert.IsType<Relation>(choice.Left);
                Assert.Same(evaluation.Parent.Surface, compared.Left);
                Assert.Same(evaluation.Surface, compared.Right);
                Assert.True(ReferenceEquals(choice.Right, compared.Left) || ReferenceEquals(choice.Right, compared.Right));
            }
        }
    }

    [Fact]
    public void FeedbackArchiveRetainsItsActualCapacityOrderingAndEveryDroppedCandidate()
    {
        foreach (var run in Runs.Value)
        {
            Assert.Equal(run.Candidates.Count - 1, E(run.ArchiveHistory).Length);
            foreach (var entry in E(run.ArchiveHistory).Cast<Relation>())
            {
                var context = Assert.IsType<Relation>(entry.Left);
                Assert.Same(run.SearchSurface, context.Left);
                var proposed = Assert.IsType<Relation>(context.Right);
                var before = E(Assert.IsType<Relation>(proposed.Left));
                var result = Assert.IsType<Relation>(entry.Right);
                var retention = Assert.IsType<Relation>(result.Left);
                var after = E(Assert.IsType<Relation>(retention.Left));
                var dropped = E(Assert.IsType<Relation>(retention.Right));
                Assert.InRange(after.Length, 1, 12);
                Assert.Equal(before.Length + 1, after.Length + dropped.Length);
                foreach (var element in before.Append(proposed.Right))
                    Assert.Equal(1, after.Concat(dropped).Count(other => ReferenceEquals(other, element)));
                var candidate = Assert.Single(run.Candidates, read => ReferenceEquals(read.Surface, proposed.Right));
                Assert.Contains(before, member => ReferenceEquals(member, candidate.Parent!.Surface));
                Assert.Equal(before.Length, E(Assert.IsType<Relation>(result.Right)).Length);
            }
        }
    }

    [Fact]
    public void RankingRetainsTheActualGojoEquationInputPredictionAndResidue()
    {
        foreach (var run in Runs.Value)
        {
            var evaluation = run.Best;
            var report = Assert.IsType<Relation>(evaluation.Surface.Right);
            var context = Assert.IsType<Relation>(report.Left);
            var goal = Assert.IsType<Relation>(context.Right);
            var measured = Assert.IsType<Relation>(goal.Right);
            var coverage = Assert.IsType<Relation>(measured.Right);
            var utility = Assert.IsType<Relation>(coverage.Right);
            var calculation = Assert.IsType<Relation>(utility.Right);
            var inputPrediction = Assert.IsType<Relation>(calculation.Right);
            var prediction = Assert.IsType<Relation>(inputPrediction.Right);
            Assert.True(Equation.TryReadPrediction(prediction, out var value, out var residue));
            Assert.Equal(evaluation.CostTicks, value.Ticks);
            Assert.NotNull(residue);
            var equation = Assert.IsType<Relation>(calculation.Left);
            var repeated = Equation.Predict(equation, inputPrediction.Left, value.Carrier);
            Assert.True(Equation.TryReadPrediction(repeated, out var again, out _));
            Assert.Equal(value.Ticks, again.Ticks);
        }
    }

    [Fact]
    public void MixedExtentRetracingHasAnExplicitRefinementThreshold()
    {
        var frame = new QualitativeMotionFrame(); var fast = new Placeholder(); var drift = new Placeholder(); var v = new Placeholder();
        var fixture = FriezeFixture.Sequence(frame, S(
            S(M(frame.Right, fast), M(frame.Right, drift)),
            S(M(frame.Left, fast), M(frame.Right, drift)),
            S(M(frame.Up, v)), S(M(frame.Right, drift)), S(M(frame.Down, v))));
        var trace = fixture.Run(5);
        var rules = new FriezeStructuralRules(fixture, S(Enumerable.Range(0, 5).Select(_ => (IElement)new Placeholder()).ToArray()));
        var read = rules.Evaluate(trace);
        Assert.True(rules.TryReadResult(read, out _, out _, out var pending));
        Assert.NotEmpty(E(pending)); // sign(drift-fast) cannot be invented.
        foreach (var (driftLength, expectedOverlap) in new[] { (2L, true), (4L, false), (6L, false) })
        {
            var calibration = FriezeRealization.CreateCalibration(fixture,
                new Dictionary<IElement, long>(ReferenceEqualityComparer.Instance) { [fast] = 4, [drift] = driftLength, [v] = 4 });
            var measured = FriezeMeasuredRules.Evaluate(fixture, trace, calibration);
            var summary = FriezeMeasuredRules.Describe(measured);
            Assert.Equal(0, summary.UnresolvedCount);
            Assert.Equal(expectedOverlap, summary.OverlapCount > 0);
        }
        AssertNoNumbers(fixture.Surface);
    }

    private static Relation M(IElement direction, IElement reach) => QualitativeMotionFrame.Move(direction, reach);
    private static Relation S(params IElement[] values) => StructuralConstraint.Closed(values);
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
    private static void AssertNoNumbers(IElement root)
    {
        var visited = new HashSet<IElement>(ReferenceEqualityComparer.Instance); var queue = new Stack<IElement>(); queue.Push(root);
        while (queue.TryPop(out var element))
        {
            if (!visited.Add(element)) continue;
            Assert.False(element is Carrier or CarrierLocation);
            if (element is Relation relation) { queue.Push(relation.Left); queue.Push(relation.Right); }
        }
    }
}
