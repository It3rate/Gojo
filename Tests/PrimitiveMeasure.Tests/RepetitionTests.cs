using Gojo.Experiments.PrimitiveMeasure;

namespace Gojo.Experiments.PrimitiveMeasure.Tests;

public sealed class RepetitionTests
{
    [Theory]
    [InlineData(2)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(32)]
    public void AClockPulseVisitsOnlyOneSuccessorEvenWithFeedback(int size)
    {
        var cycle = new ReferenceCycle(size);
        for (var pulse = 0; pulse < size * 3; pulse++)
        {
            var step = cycle.Circuit.Pulse();
            Assert.True(step.Moved);
            Assert.Equal((pulse + 1) % size, cycle.Position);
            Assert.Equal(size - 1, cycle.Sites.Count(s => cycle.Circuit.Expose(s).ReadSupport() == 1));
        }
    }

    [Theory]
    [InlineData(1, 2)]
    [InlineData(2, 1)]
    public void ConditionalBranchesRejoinWithoutCopyingActivation(byte support, int expectedSite)
    {
        var condition = new Observation(support);
        var (circuit, _, _, _, join) = Fork(condition);
        Assert.True(circuit.Pulse().Moved);
        Assert.Equal(expectedSite, circuit.Active);
        Assert.True(circuit.Pulse().Moved);
        Assert.Equal(join, circuit.Active);
        Assert.False(circuit.Pulse().Moved);
    }

    [Fact]
    public void SharedUnknownGuardKeepsAlternativesAndRefusesMutation()
    {
        var condition = new Observation(3);
        var (circuit, start, left, right, _) = Fork(condition);
        var before = circuit.ValueBytes();
        var read = circuit.Predict();
        Assert.Equal(new[] { left, right }, read.Targets);
        Assert.False(read.Conflict);
        Assert.False(read.Resolved);
        Assert.False(circuit.Pulse().Committed);
        Assert.Equal(before, circuit.ValueBytes());
        condition.Support = 2;
        Assert.True(circuit.Pulse().Moved);
        Assert.NotEqual(start, circuit.Active);
        Assert.Equal(left, circuit.Active);
    }

    [Fact]
    public void TwoEnabledOutletsAreAConflictNotAnImplicitPriority()
    {
        var circuit = new PulseCircuit();
        var start = circuit.State(true);
        var left = circuit.State();
        var right = circuit.State();
        var guard = circuit.Guard();
        var table = circuit.AddTable(PassageLaw.Unconditional());
        circuit.Link(table, start, guard, left);
        circuit.Link(table, start, guard, right);
        var before = circuit.ValueBytes();
        Assert.True(circuit.Predict().Conflict);
        Assert.False(circuit.Pulse().Committed);
        Assert.Equal(before, circuit.ValueBytes());
    }

    [Fact]
    public void NoActivationOrNoObservationDoesNotCreateAnActivation()
    {
        var empty = new PulseCircuit();
        empty.State();
        Assert.False(empty.Pulse().Committed);
        var (circuit, _, _, _, _) = Fork(new Observation(0));
        Assert.True(circuit.Predict().Missing);
        var bytes = circuit.ValueBytes();
        Assert.False(circuit.Pulse().Committed);
        Assert.Equal(bytes, circuit.ValueBytes());
    }

    [Fact]
    public void OneGuardReadIsSharedAcrossItsCompetingRows()
    {
        var condition = new Observation(3);
        var (circuit, _, _, _, _) = Fork(condition);
        circuit.Predict();
        Assert.Equal(1, condition.Reads);
    }

    [Fact]
    public void UncertaintyInAnUnusedControlCannotVetoAnUnconditionalContinuation()
    {
        var condition = new Observation(2);
        var (circuit, _, _, _, join) = Fork(condition);
        Assert.True(circuit.Pulse().Moved);
        var reads = condition.Reads;
        condition.Support = 3;
        Assert.True(circuit.Pulse().Moved);
        Assert.Equal(join, circuit.Active);
        Assert.Equal(reads, condition.Reads);
    }

    [Fact]
    public void AResolvedHoldRefreshesItsGuardObservationWithoutMovingActivation()
    {
        var circuit = new PulseCircuit();
        var start = circuit.State(true);
        var target = circuit.State();
        var guard = circuit.Guard(true);
        circuit.Bind(guard, new Observation(1));
        circuit.Link(circuit.AddTable(PassageLaw.Controlled()), start, guard, target);
        Assert.Equal(new byte[] { 5 }, circuit.ValueBytes());
        var step = circuit.Pulse();
        Assert.True(step.Committed);
        Assert.False(step.Moved);
        Assert.Equal(new byte[] { 1 }, circuit.ValueBytes());
    }

    [Fact]
    public void TurnBasisReadDoesNotKeepAStalePositiveClaim()
    {
        var contour = RepetitionExamples.Contour(4, true, false);
        var body = new RepeatedBody([0], 4);
        body.BindTurns(contour);
        Assert.True(body.HasEqualTurnBasis);
        contour.Close(); // A second closure is outside the exact selected contract.
        Assert.False(body.HasEqualTurnBasis);
    }

    [Fact]
    public void BranchCompletionBarrierObservesBothSourcesWithoutDrainingThem()
    {
        var a = new Observation(1);
        var b = new Observation(1);
        var barrier = new PulseCircuit();
        var wait = barrier.State(true);
        var done = barrier.State();
        var guard = barrier.Guard();
        barrier.Bind(guard, PresencePorts.All(a, b));
        barrier.Link(barrier.AddTable(PassageLaw.Controlled()), wait, guard, done);
        Assert.False(barrier.Pulse().Moved);
        a.Support = 2;
        Assert.False(barrier.Pulse().Moved);
        b.Support = 3;
        Assert.False(barrier.Pulse().Committed);
        b.Support = 2;
        Assert.True(barrier.Pulse().Moved);
        Assert.Equal(done, barrier.Active);
        Assert.Equal((byte)2, a.Support);
        Assert.Equal((byte)2, b.Support);
    }

    [Fact]
    public void SquareBodyReusesOneReachAndChecksReturnOnlyAfterParticipation()
    {
        var body = new RepeatedBody([0], 4);
        Assert.False(body.HasEqualTurnBasis);
        body.BindTurns(RepetitionExamples.Contour(4, true, false));
        Assert.True(body.HasEqualTurnBasis);
        var steps = RepetitionExamples.Run(body, 100);
        Assert.Equal(new[] { 0, 1, 2, 3 }, steps.Where(s => s.Parameter is not null).Select(s => s.Phase));
        Assert.All(steps.Where(s => s.Parameter is not null), s => Assert.Equal(0, s.Parameter));
        Assert.True(steps[^1].Completed);
        Assert.Equal(0, body.Phase.Position);
        Assert.Equal(new byte[] { 0, 0 }, body.ParameterBytes());
        Assert.False(body.Pulse().Completed);
    }

    [Fact]
    public void RectangleBodyRepeatsTheSameTwoParameterReferences()
    {
        var body = new RepeatedBody([0, 1], 4);
        var steps = RepetitionExamples.Run(body, 100);
        Assert.Equal(new int?[] { 0, 1, 0, 1 }, steps.Where(s => s.Parameter is not null).Select(s => s.Parameter));
        Assert.Equal(new[] { 0, 1, 2, 3 }, steps.Where(s => s.Parameter is not null).Select(s => s.Phase));
        Assert.Equal(2, steps.Count(s => s.Before == body.Check));
        Assert.Equal(new byte[] { 0, 0x20, 4 }, body.ParameterBytes());
        Assert.Equal(new byte[] { 0xC4, 0x01, 0 }, body.Circuit.PackedRows(body.AbsentTable));
    }

    [Fact]
    public void HeadingReturnDoesNotProveClosureWithFourIndependentReaches()
    {
        var body = new RepeatedBody([0, 1, 2, 3], 4);
        body.BindTurns(RepetitionExamples.Contour(4, true, false));
        var steps = RepetitionExamples.Run(body, 100);
        Assert.True(steps[^1].Completed);
        var moves = steps.Where(s => s.Parameter is not null).ToArray();
        var geometry = new OrthogonalPathFixture(Enumerable.Range(0, 4).Select(_ => (IPrimitiveReadPort)new Observation(2)).ToArray());
        for (var i = 0; i < 4; i++) geometry.Connect(moves[i].Phase, i, (i + 1) % 4, 4 + moves[i].Parameter!.Value);
        Assert.False(geometry.Read().GuaranteesRectangle);
    }

    [Theory]
    [InlineData(1, 4, 4)]
    [InlineData(2, 4, 4)]
    [InlineData(3, 4, 12)]
    [InlineData(2, 5, 10)]
    public void BodyBoundaryAndPhaseReturnComposeWithoutAStoredLapCounter(int bodyLength, int phases, int expectedParticipations)
    {
        var body = new RepeatedBody(Enumerable.Range(0, bodyLength).ToArray(), phases);
        var result = RepetitionExamples.Run(body, 150);
        Assert.True(result[^1].Completed);
        Assert.Equal(expectedParticipations, result.Count(s => s.Parameter is not null));
    }

    [Fact]
    public void AContinuousLoopHasNoHiddenHarnessStop()
    {
        var body = new RepeatedBody([0, 1], 4, continuous: true);
        var result = RepetitionExamples.Run(body, 100);
        Assert.Equal(100, result.Count);
        Assert.DoesNotContain(result, s => s.Completed);
        Assert.Equal((byte)1, body.Completion.ReadSupport());
        Assert.True(body.Pulse().Committed);
    }

    [Fact]
    public void UnresolvedContactHoldsAtCheckAndLaterEvidenceCanReleaseOrStop()
    {
        var contact = new Observation(3);
        var body = new RepeatedBody([0], 4, stop: contact);
        Assert.False(RepetitionExamples.Run(body, 20)[^1].Committed);
        Assert.Equal(body.Check, body.Circuit.Active);
        var bytes = body.Circuit.ValueBytes();
        Assert.False(body.Pulse().Committed);
        Assert.Equal(bytes, body.Circuit.ValueBytes());
        contact.Support = 1;
        Assert.Equal(body.ParticipationSites[0], body.Pulse().After);
        contact.Support = 2;
        Assert.True(RepetitionExamples.Run(body, 20)[^1].Completed);
    }

    [Fact]
    public void RequestCompletionHandshakeRequiresReleaseBeforeAnotherInvocation()
    {
        var request = new Observation(1);
        var body = new RepeatedBody([0, 1], 4, request: request);
        Assert.False(body.Pulse().Completed);
        Assert.Equal(body.Idle, body.Circuit.Active);
        request.Support = 2;
        Assert.True(RepetitionExamples.Run(body, 100)[^1].Completed);
        Assert.Equal(body.Done, body.Pulse().After);
        request.Support = 1;
        Assert.Equal(body.Idle, body.Pulse().After);
        request.Support = 2;
        Assert.True(RepetitionExamples.Run(body, 100)[^1].Completed);
    }

    [Fact]
    public void FiveWaitingStagesAllowAnotherStructureToMoveTheNextPlacement()
    {
        var pattern = new PatternComposition();
        var steps = new List<PatternStep>();
        for (var i = 0; i < 100 && pattern.Instances < 3; i++) steps.Add(pattern.Pulse());
        Assert.Equal(new int?[] { 1, 2, 3 }, steps.Where(s => s.StartedAt is not null).Select(s => s.StartedAt));
        Assert.Equal(2, steps.Count(s => s.PlacementAdvanced));
        Assert.Equal(new[] { 1, 2, 0 }, pattern.Translation(0));
        Assert.Equal(new[] { 2, 3, 0 }, pattern.Translation(1));
        Assert.Equal(new byte[] { 0, 1, 1, 2, 2, 3 }, pattern.FrameBytes());
        Assert.Equal(10, steps.Count(s => s.ParentBefore is int p && pattern.Waiting.Contains(p)));
        Assert.All(steps.Where(s => s.PlacementAdvanced), s => Assert.Contains(s.ParentBefore!.Value, pattern.Waiting));
        Assert.True(pattern.Body.HasEqualTurnBasis);
    }

    [Fact]
    public void UnequalCirclePartsCannotBeUsedAsEqualTurns()
    {
        var body = new RepeatedBody([0], 4);
        body.BindTurns(RepetitionExamples.Contour(4, false, false));
        Assert.False(body.HasEqualTurnBasis);
    }

    [Fact]
    public void AContinuouslyMovingFramePreservesLocalPatternButChangesItsWorldBindings()
    {
        var moving = new MovingFramePattern();
        for (var beat = 0; beat < 10; beat++) moving.Pulse();
        Assert.Equal(4, moving.SampleCount);
        Assert.Equal(new[] { 0, 0, 1 }, moving.Sample(0));
        Assert.Equal(new[] { 1, 1, 3 }, moving.Sample(1));
        Assert.Equal(new[] { 0, 2, 6 }, moving.Sample(2));
        Assert.Equal(new[] { 1, 3, 8 }, moving.Sample(3));
        Assert.Equal(11, moving.Placement);
        Assert.Equal(30, moving.TranslationBytes().Length);
        Assert.True(moving.Body.HasEqualTurnBasis);
    }

    [Fact]
    public void WrongGuardContextAndNondeterministicLawRefuseBeforeActivationMoves()
    {
        var observation = new Observation(1 << 5);
        var (circuit, _, _, _, _) = Fork(observation);
        var before = circuit.ValueBytes();
        Assert.Throws<InvalidOperationException>(() => circuit.Pulse());
        Assert.Equal(before, circuit.ValueBytes());
        var ambiguous = new PulseCircuit();
        var start = ambiguous.State(true);
        var end = ambiguous.State();
        var guard = ambiguous.Guard();
        var bytes = PassageLaw.Unconditional().PackedBytes();
        bytes[1] |= 1 << 1; // At S=1,C=0,T=0 both hold and handoff are admitted.
        ambiguous.Link(ambiguous.AddTable(new PassageLaw(bytes)), start, guard, end);
        Assert.False(ambiguous.Pulse().Committed);
        Assert.Equal(start, ambiguous.Active);
    }

    private static (PulseCircuit Circuit, int Start, int Left, int Right, int Join) Fork(Observation condition)
    {
        var circuit = new PulseCircuit();
        var start = circuit.State(true);
        var left = circuit.State();
        var right = circuit.State();
        var join = circuit.State();
        var guard = circuit.Guard();
        circuit.Bind(guard, condition);
        circuit.Link(circuit.AddTable(PassageLaw.Controlled()), start, guard, left);
        circuit.Link(circuit.AddTable(PassageLaw.Controlled(false)), start, guard, right);
        var forward = circuit.AddTable(PassageLaw.Unconditional());
        circuit.Link(forward, left, guard, join);
        circuit.Link(forward, right, guard, join);
        return (circuit, start, left, right, join);
    }

    private sealed class Observation(byte support) : IPrimitiveReadPort
    {
        public byte Support { get; set; } = support;
        public int Reads { get; private set; }
        public byte ReadSupport() { Reads++; return Support; }
    }
}
