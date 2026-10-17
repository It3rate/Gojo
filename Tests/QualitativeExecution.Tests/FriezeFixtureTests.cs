using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeFrieze;

namespace Gojo.Experiments.QualitativeExecution.Tests;

public sealed class FriezeFixtureTests
{
    public static IEnumerable<object[]> RepeatingFixtures()
    {
        yield return [new Func<FriezeFixture>(() => FriezeFixture.SquareWave()), 4];
        yield return [new Func<FriezeFixture>(() => FriezeFixture.SquareWave(coupled: false)), 4];
        yield return [new Func<FriezeFixture>(() => FriezeFixture.Meander()), 9];
        yield return [new Func<FriezeFixture>(() => FriezeFixture.SlowFastDrift()), 6];
        yield return [new Func<FriezeFixture>(() => FriezeFixture.PausesAndDrift()), 8];
        yield return [new Func<FriezeFixture>(() => FriezeFixture.VariableSteps()), 4];
    }

    [Theory]
    [MemberData(nameof(RepeatingFixtures))]
    public void CompletePhaseReturnGeneratesAnotherPeriodWithFreshOccurrencesAndNoMeasuredSource(Func<FriezeFixture> create, int period)
    {
        var f = create(); var trace = f.Run(period * 2);
        Assert.True(f.TryReadTrace(trace, out var initial, out var final, out var steps, out _, out var samples, out _));
        Assert.Equal(period * 2, E(steps).Length); Assert.Equal(period * 2, E(samples).Length);
        Assert.True(f.SamePhaseConfiguration(initial, final));
        Assert.True(f.Network.TryReadStep(Assert.IsType<Relation>(E(steps)[period - 1]),
            out _, out _, out var firstPeriod, out _, out _, out _));
        Assert.True(f.SamePhaseConfiguration(initial, firstPeriod));
        if (period > 1)
        {
            Assert.True(f.Network.TryReadStep(Assert.IsType<Relation>(E(steps)[period - 2]),
                out _, out _, out var incompletePeriod, out _, out _, out _));
            Assert.False(f.SamePhaseConfiguration(initial, incompletePeriod));
        }
        var events = E(samples).Cast<Relation>().Select(sample =>
        {
            Assert.True(FriezeFixture.TryReadSample(sample, out _, out var occurrence, out _, out _, out _, out _));
            return occurrence;
        }).ToArray();
        Assert.Equal(events.Length, events.Distinct(ReferenceEqualityComparer.Instance).Count());
        foreach (var step in E(steps).Cast<Relation>())
        {
            Assert.True(f.Network.TryReadStep(step, out _, out _, out _, out _, out var blocked, out var pending));
            Assert.Empty(E(blocked)); Assert.Empty(E(pending));
        }
        Assert.DoesNotContain(Traverse(trace), element => element is Carrier or CarrierLocation);
        Assert.DoesNotContain(Traverse(f.Surface), element => element is Carrier or CarrierLocation);
    }

    [Fact]
    public void DifferentCausalProgramsCanProduceTheSameSquareWaveWithoutSharingEventHistory()
    {
        var frame = new QualitativeMotionFrame(); var h = new Placeholder(); var v = new Placeholder();
        var direct = FriezeFixture.SquareWave(false, frame, h, v);
        var coupled = FriezeFixture.SquareWave(true, frame, h, v);
        var a = direct.Run(8); var b = coupled.Run(8);
        Assert.True(direct.TryReadTrace(a, out _, out _, out var directSteps, out var directMoves, out var directSamples, out _));
        Assert.True(coupled.TryReadTrace(b, out _, out _, out var coupledSteps, out var coupledMoves, out var coupledSamples, out _));
        Assert.Equal(new IElement[] { frame.Up, frame.Right, frame.Down, frame.Right, frame.Up, frame.Right, frame.Down, frame.Right },
            E(directMoves).Cast<Relation>().Select(app => Assert.IsType<Relation>(app.Right).Left));
        Assert.Equal(8, E(coupledMoves).Length);
        foreach (var pair in E(directMoves).Cast<Relation>().Zip(E(coupledMoves).Cast<Relation>()))
        {
            var first = Assert.IsType<Relation>(pair.First.Right); var second = Assert.IsType<Relation>(pair.Second.Right);
            Assert.Same(first.Left, second.Left); Assert.Same(first.Right, second.Right);
            Assert.NotSame(pair.First.Left, pair.Second.Left);
        }
        Assert.Single(direct.Components); Assert.Equal(3, coupled.Components.Count);
        Assert.Equal(8, AcceptedCount(direct, directSteps)); Assert.Equal(20, AcceptedCount(coupled, coupledSteps));
        Assert.Equal(E(directSamples).Length, E(coupledSamples).Length);
        Assert.NotSame(direct.Surface, coupled.Surface);
        Assert.True(Contains(a, direct.OutputLaw)); Assert.True(Contains(b, coupled.Network.Routes));
    }

    [Fact]
    public void ThreePauseOccurrencesPreserveVerticalPositionBeforeTheFourthTriggersMovement()
    {
        var f = FriezeFixture.PausesAndDrift(); var trace = f.Run(8);
        Assert.True(f.TryReadTrace(trace, out _, out _, out var steps, out var moves, out var samples, out var unresolved));
        Assert.Empty(E(unresolved));
        var commits = E(samples).Cast<Relation>().ToArray();
        for (var i = 0; i < commits.Length; i++)
        {
            Assert.True(FriezeFixture.TryReadSample(commits[i], out _, out _, out _, out var position, out _, out _));
            var vertical = E(position).Cast<Relation>().Where(move => ReferenceEquals(move.Left, f.Frame.Up) || ReferenceEquals(move.Left, f.Frame.Down));
            Assert.Equal(i >= 3 && i < 7 ? 1 : 0, vertical.Count());
        }
        Assert.Equal(10, E(moves).Length); // Eight drift moves and two vertical moves.
        Assert.Equal(18, AcceptedCount(f, steps)); // Every pause still advances the driver and drift.
    }

    [Fact]
    public void SamplingTogetherAndAfterEachFiringKeepsTheSameMotionButDifferentStrokeGroups()
    {
        var frame = new QualitativeMotionFrame();
        var combined = FriezeFixture.SlowFastDrift(false, frame);
        var individual = combined.WithSampling(StructuralConstraint.Closed(E(combined.Network.Ports)
            .Where(port => !ReferenceEquals(port, combined.RootPort)).ToArray()));
        var a = combined.Run(6); var b = individual.Run(6);
        Assert.True(combined.TryReadTrace(a, out _, out _, out _, out var movesA, out var samplesA, out var pendingA));
        Assert.True(individual.TryReadTrace(b, out _, out _, out _, out var movesB, out var samplesB, out var pendingB));
        Assert.Equal(14, E(movesA).Length); Assert.Equal(14, E(movesB).Length);
        Assert.Same(combined.Network, individual.Network); Assert.Same(combined.OutputLaw, individual.OutputLaw);
        Assert.Equal(combined.Reaches, individual.Reaches);
        Assert.Equal(E(movesA).Cast<Relation>().Select(app => Assert.IsType<Relation>(app.Right).Left),
            E(movesB).Cast<Relation>().Select(app => Assert.IsType<Relation>(app.Right).Left));
        Assert.Equal(E(movesA).Cast<Relation>().Select(app => Assert.IsType<Relation>(app.Right).Right),
            E(movesB).Cast<Relation>().Select(app => Assert.IsType<Relation>(app.Right).Right));
        Assert.Equal(6, E(samplesA).Length); Assert.Equal(14, E(samplesB).Length);
        Assert.NotEmpty(E(pendingA)); Assert.Empty(E(pendingB)); // Grouped opposed unequal reaches need a finer net read.
        Assert.Equal(14, E(samplesA).Cast<Relation>().Sum(MoveCount));
        Assert.Equal(14, E(samplesB).Cast<Relation>().Sum(MoveCount));
        Assert.True(Contains(a, combined.Sampling)); Assert.True(Contains(b, individual.Sampling));
    }

    [Fact]
    public void GreekKeyReturnsVerticallyWhileItsSharedHorizontalParticipationDriftsForward()
    {
        var f = FriezeFixture.Meander(); var trace = f.Run(9);
        Assert.True(f.TryReadTrace(trace, out var initial, out var final, out _, out _, out var samples, out var unresolved));
        Assert.Empty(E(unresolved)); Assert.True(f.SamePhaseConfiguration(initial, final));
        Assert.True(FriezeFixture.TryReadSample(Assert.IsType<Relation>(E(samples)[^1]), out _, out _, out _, out var residual, out _, out var proof));
        var terms = E(residual).Cast<Relation>().ToArray();
        Assert.Equal(3, terms.Length);
        Assert.All(terms, move => Assert.Same(f.Frame.Right, move.Left));
        Assert.All(terms, move => Assert.Same(terms[0].Right, move.Right));
        Assert.True(Contains(proof, f.Frame.CancellationLaws));
    }

    [Fact]
    public void DifferentReachIdentitiesStayDistinctBeforeAnyStepLengthIsChosen()
    {
        var f = FriezeFixture.VariableSteps(); var trace = f.Run(4);
        Assert.True(f.TryReadTrace(trace, out _, out _, out _, out _, out var samples, out _));
        Assert.True(FriezeFixture.TryReadSample(Assert.IsType<Relation>(E(samples)[^1]), out _, out _, out _, out var position, out _, out _));
        var horizontal = E(position).Cast<Relation>().ToArray(); Assert.Equal(2, horizontal.Length);
        Assert.All(horizontal, move => Assert.Same(f.Frame.Right, move.Left));
        Assert.NotSame(horizontal[0].Right, horizontal[1].Right);
        var order = Assert.IsType<Relation>(Assert.Single(E(f.ReachOrder)));
        Assert.Same(horizontal[0].Right, order.Left); Assert.Same(horizontal[1].Right, order.Right);
        Assert.DoesNotContain(Traverse(trace), element => element is CarrierLocation);
    }

    [Fact]
    public void AnUnfinishedCascadeCannotCommitAFictionalCompleteSample()
    {
        var f = FriezeFixture.SquareWave(); var trace = f.Run(1, eventBudget: 1);
        Assert.True(f.TryReadTrace(trace, out _, out var final, out var steps, out var moves, out var samples, out var unresolved));
        Assert.Single(E(steps)); Assert.Empty(E(moves)); Assert.Empty(E(samples)); Assert.NotEmpty(E(unresolved));
        Assert.True(f.Network.TryReadState(final, out _, out _, out _, out var queue)); Assert.NotEmpty(E(queue));
    }

    [Fact]
    public void AZeroObservationWindowDoesNotInventAnEventOrAnInitialStroke()
    {
        var f = FriezeFixture.Meander(); var trace = f.Run(0);
        Assert.True(f.TryReadTrace(trace, out var initial, out var final, out var steps, out var moves, out var samples, out var unresolved));
        Assert.Same(initial, final); Assert.Empty(E(steps)); Assert.Empty(E(moves)); Assert.Empty(E(samples)); Assert.Empty(E(unresolved));
    }

    [Fact]
    public void AMissingEmissionDoesNotPretendToBeAnExplicitPause()
    {
        var original = FriezeFixture.SquareWave(coupled: false);
        Assert.True(FiniteLaw.TryRead(original.OutputLaw, out var phases, out var ports, out var words, out _));
        var missing = original.WithOutputs(FiniteLaw.Create(phases, ports, words));
        var trace = missing.Run(1);
        Assert.True(missing.TryReadTrace(trace, out _, out _, out _, out var moves, out var samples, out var unresolved));
        Assert.Single(E(moves)); Assert.NotEmpty(E(unresolved));
        Assert.True(FriezeFixture.TryReadSample(Assert.IsType<Relation>(Assert.Single(E(samples))),
            out _, out _, out _, out var position, out _, out _));
        Assert.True(StructuralConstraint.TryReadScope(position, out _, out var seam, out var tail));
        Assert.NotEqual(seam, tail);
    }

    [Fact]
    public void MatchingUnknownPhaseSupportsCannotCertifyARecurringConfiguration()
    {
        var f = FriezeFixture.Meander();
        var starts = StructuralConstraint.Closed(E(f.Starts).Cast<Relation>().Select(row =>
            (IElement)new Relation(row.Left, StructuralConstraint.Open())).ToArray());
        var first = f.Network.Initial(new Placeholder(), starts);
        var second = f.Network.Initial(new Placeholder(), starts);
        Assert.False(f.SamePhaseConfiguration(first, second));
    }

    private static int AcceptedCount(FriezeFixture f, Relation steps) => E(steps).Cast<Relation>().Sum(step =>
    {
        Assert.True(f.Network.TryReadStep(step, out _, out _, out _, out var accepted, out _, out _)); return E(accepted).Length;
    });
    private static int MoveCount(Relation sample)
    {
        Assert.True(FriezeFixture.TryReadSample(sample, out _, out _, out _, out _, out var applications, out _)); return E(applications).Length;
    }
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
    private static bool Contains(IElement root, IElement target) => Traverse(root).Any(next => ReferenceEquals(next, target));
    private static IEnumerable<IElement> Traverse(IElement root)
    {
        var seen = new HashSet<IElement>(ReferenceEqualityComparer.Instance); var todo = new Stack<IElement>(); todo.Push(root);
        while (todo.TryPop(out var next))
        {
            if (!seen.Add(next)) continue; yield return next;
            if (next is Relation r) { todo.Push(r.Left); todo.Push(r.Right); }
        }
    }
}
