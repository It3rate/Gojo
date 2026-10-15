using Gojo.Core;
using Gojo.Experiments.EnoughStructure;
using Gojo.Patterns;
using Gojo.Structures;

namespace Gojo.Experiments.EnoughStructure.Tests;

public sealed class ConstructionTests
{
    [Theory]
    [InlineData(null)]
    [InlineData(3L)]
    public void DirectAndPatternConstructionsKeepDifferentProvenanceButSamePhysicalBehavior(long? capacity)
    {
        var unit = new Carrier().UnitShape;
        var direct = Surfaces.CreateDirect(capacity, unit); var bound = Surfaces.CreateBound(capacity, unit);
        Surfaces.Fields(Surfaces.Application(direct), 3, out var d);
        Surfaces.Fields(Surfaces.Application(bound), 3, out var b);
        Assert.Empty(StructuralPattern.ReadSlots(d[0]));
        Assert.Equal(2, StructuralPattern.ReadSlots(b[0]).Length);
        Assert.Empty(StructuralPattern.ReadSlots(b[2]));
        Assert.NotSame(d[2], b[2]);
        Surfaces.Activate(direct); Surfaces.Activate(bound);
        Surfaces.Pulse(direct, 4); Surfaces.Pulse(bound, 4);
        var ds = ReadState(direct); var bs = ReadState(bound);
        Assert.Equal(ds, bs);
        Assert.Equal(capacity.HasValue ? (0L, 1L, 3L, 0L) : (0L, 0L, 4L, 0L), ds);
        AssertSame(Surfaces.Compare(Surfaces.Inspect(Surfaces.Question(direct)), Surfaces.Inspect(Surfaces.Question(bound))));
        if (capacity.HasValue)
        {
            Surfaces.TryWorld(bound, out _, out _, out var boundaries, out _, out _);
            Assert.True(Surfaces.Fields(boundaries, 1, out var apps));
            Assert.True(Surfaces.Fields(apps[0], 3, out var app));
            var clamp = Assert.IsType<Relation>(app[2]);
            Assert.Same(clamp.Left, clamp.Right);
        }
    }

    [Fact]
    public void BoundAndDirectQuestionsUseTheSameReader()
    {
        var world = Surfaces.CreateBound(3); Surfaces.Activate(world); Surfaces.Pulse(world, 4);
        var direct = Surfaces.Question(world); Surfaces.Fields(direct, 5, out var q);
        var slots = Enumerable.Range(0, 5).Select(_ => new Placeholder()).ToArray();
        var pattern = Surfaces.Group(slots);
        var bindings = slots.Select((slot, index) => new Relation(slot, q[index])).ToArray();
        var application = Surfaces.Bind(pattern, bindings);
        Surfaces.Fields(application, 3, out var f);
        Assert.Empty(StructuralPattern.ReadSlots(f[2]));
        AssertSame(Surfaces.Compare(Surfaces.Inspect(direct), Surfaces.Inspect((Relation)f[2])));
    }

    [Fact]
    public void EnoughAnswersAgreeAcrossDeclaredNonnegativeTotalsButAmountCanDistinguishThem()
    {
        // Exhaust the fixture's finite total domain; input decompositions are
        // checked separately. This is conformance, not training examples.
        for (var n = 0; n <= 8; n++)
        {
            var unit = new Carrier().UnitShape;
            var free = Surfaces.CreateBound(unit: unit); var clamped = Surfaces.CreateBound(3, unit);
            Surfaces.Activate(free); Surfaces.Activate(clamped);
            Surfaces.Pulse(free, n); Surfaces.Pulse(clamped, n);
            var a = Surfaces.Inspect(Surfaces.Question(free)); var b = Surfaces.Inspect(Surfaces.Question(clamped));
            AssertSame(Surfaces.Compare(a, b));
            Assert.True(Surfaces.TryAnswer(a, out var answer));
            Assert.True(Surfaces.Span(answer, out var low, out var high));
            Assert.Equal(n >= 3 ? (0L, 0L) : (1L, 8L), (low.Ticks, high.Ticks));
            Assert.True(Surfaces.TryDemand(a, out var demand)); Assert.Equal(Math.Max(0, 3 - n), demand.Ticks);
            var amounts = Surfaces.Compare(Surfaces.Inspect(Surfaces.QuantityQuestion(free)), Surfaces.Inspect(Surfaces.QuantityQuestion(clamped)));
            Assert.True(Surfaces.TryEquality(amounts, out var equal)); Assert.Equal(n <= 3, equal);
        }
    }

    [Fact]
    public void DifferentFalseShortfallsShareTheSameCoarseAnswerWithoutLosingTheirDetail()
    {
        var unit = new Carrier().UnitShape;
        var a = Surfaces.CreateDirect(unit: unit); var b = Surfaces.CreateDirect(unit: unit);
        Surfaces.Activate(a); Surfaces.Activate(b); Surfaces.Pulse(a, 1); Surfaces.Pulse(b, 2);
        var aw = Surfaces.Inspect(Surfaces.Question(a)); var bw = Surfaces.Inspect(Surfaces.Question(b));
        Assert.True(Surfaces.TryDemand(aw, out var ad)); Assert.True(Surfaces.TryDemand(bw, out var bd));
        Assert.Equal(2, ad.Ticks); Assert.Equal(1, bd.Ticks); AssertSame(Surfaces.Compare(aw, bw));
    }

    [Fact]
    public void PulseDecompositionRetainsSameSettlementAndAccounting()
    {
        var single = Surfaces.CreateBound(3); var split = Surfaces.CreateBound(3);
        Surfaces.Activate(single); Surfaces.Activate(split);
        AssertAccounting(Surfaces.Pulse(single, 4));
        foreach (var amount in new[] { 1, 2, 1 }) AssertAccounting(Surfaces.Pulse(split, amount));
        Assert.Equal(ReadState(single), ReadState(split));
    }

    [Fact]
    public void InspectionIsPureAndOldWitnessesSurviveNewInputsAndRetargetedGoals()
    {
        var world = Surfaces.CreateBound(3); Surfaces.Activate(world); Surfaces.Pulse(world, 2);
        var question = Surfaces.Question(world); var before = ReadState(world);
        var old = Surfaces.Inspect(question);
        Assert.Equal(before, ReadState(world));
        Surfaces.Pulse(world, 1);
        Assert.True(Surfaces.TryDemand(old, out var oldDemand)); Assert.Equal(1, oldDemand.Ticks);
        Assert.True(Surfaces.TryDemand(Surfaces.Inspect(question), out var current)); Assert.Equal(0, current.Ticks);
        Surfaces.Fields(question, 5, out var q); Surfaces.Span(q[3], out var lower, out _);
        lower.Tick.Set(4);
        Assert.True(Surfaces.TryDemand(Surfaces.Inspect(question), out var changed)); Assert.Equal(1, changed.Ticks);
        Assert.True(Surfaces.TryDemand(old, out oldDemand)); Assert.Equal(1, oldDemand.Ticks);
        Assert.Equal((0L, 0L, 3L, 0L), ReadState(world));
    }

    [Fact]
    public void AnOpenRouteLeavesAKnownUnmetGoalAndCanLaterConsumeRetainedSupply()
    {
        var world = Surfaces.CreateOpen(3);
        Surfaces.Pulse(world, 4);
        var open = Surfaces.Activate(world);
        Assert.Equal((4L, 0L, 0L, 0L), ReadState(world));
        Assert.True(Surfaces.TryResponse(open, out var before, out var after, out var reduction));
        Assert.Equal((3L, 3L, 0L), (before.Ticks, after.Ticks, reduction.Ticks));
        Surfaces.Fields(Surfaces.Application(world), 3, out var app);
        var pattern = (Relation)app[0]; ((Relation)app[1]).TryReadOrderedGroup(out var oldBindings);
        Surfaces.TryWorld(world, out _, out var rp, out _, out _, out _);
        Surfaces.TryPort(rp, out var receiver, out _, out _);
        var complete = Surfaces.Bind(pattern, (Relation)oldBindings[0], new Relation(pattern.Right, Relation.Point(receiver.Carrier, 0)));
        world = Surfaces.ReplaceApplication(world, complete);
        var activated = Surfaces.Activate(world);
        Assert.Equal((0L, 1L, 3L, 0L), ReadState(world));
        Assert.True(Surfaces.TryResponse(activated, out before, out after, out reduction));
        Assert.Equal((3L, 0L, 3L), (before.Ticks, after.Ticks, reduction.Ticks));
        AssertAccounting(activated);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void FailedOrForgedBindingsRemainVisibleAndNeverActivate(int failure)
    {
        var world = Surfaces.CreateOpen(3);
        Surfaces.TryWorld(world, out var sp, out var rp, out _, out _, out _);
        Surfaces.TryPort(sp, out var s, out _, out _); Surfaces.TryPort(rp, out var r, out _, out _);
        var bad = failure == 0
            ? new Relation(Relation.Point(s.Carrier, 1), Relation.Point(r.Carrier, 1))
            : new Relation(Relation.Point(r.Carrier, 1), Relation.Point(s.Carrier, 0));
        var application = Surfaces.Direct(bad);
        if (failure == 2)
        {
            Surfaces.Fields(Surfaces.Application(world), 3, out var original);
            application = Surfaces.Group(original[0], original[1], new Relation(Relation.Point(s.Carrier, 1), Relation.Point(r.Carrier, 0)));
        }
        world = Surfaces.ReplaceApplication(world, application);
        var activation = Surfaces.Activate(world);
        Surfaces.Pulse(world, 4);
        Assert.Equal((4L, 0L, 0L, 0L), ReadState(world));
        Assert.False(s.Carrier.IsAttached(bad));
        Assert.True(ReadWitness.TryRead(activation, out _, out _, out _, out _, out var admission));
        Assert.True(ReadWitness.TryRead((Relation)admission, out var selected, out _, out var result, out _, out _));
        Assert.Same(application, selected); Assert.IsType<Placeholder>(result);
    }

    [Fact]
    public void ConflictingBindingsAreAlternativesInsteadOfFirstBindingWins()
    {
        var world = Surfaces.CreateOpen(); Surfaces.Fields(Surfaces.Application(world), 3, out var old);
        var pattern = (Relation)old[0]; ((Relation)old[1]).TryReadOrderedGroup(out var rows);
        Surfaces.TryWorld(world, out _, out var port, out _, out _, out _); Surfaces.TryPort(port, out var r, out _, out _);
        var application = Surfaces.Bind(pattern, (Relation)rows[0], new Relation(pattern.Right, Relation.Point(r.Carrier, 0)),
            new Relation(pattern.Right, Relation.Point(r.Carrier, 1)));
        world = Surfaces.ReplaceApplication(world, application);
        Surfaces.Activate(world); Surfaces.Pulse(world, 4);
        Assert.Equal((4L, 0L, 0L, 0L), ReadState(world));
        Surfaces.Fields(application, 3, out var app); Assert.True(Surfaces.Fields(app[1], 3, out _));
    }

    [Fact]
    public void OpenQuestionInputsAndInvalidCalibrationsCannotSupplyFalseCertainty()
    {
        var world = Surfaces.CreateDirect(); Surfaces.Activate(world); Surfaces.Pulse(world, 3);
        Surfaces.Fields(Surfaces.Question(world), 5, out var q);
        var open = new Placeholder();
        var questions = new[]
        {
            Surfaces.Group(open, q[1], q[2], q[3], q[4]),
            Surfaces.Group(q[0], open, q[2], q[3], q[4]),
            Surfaces.Group(q[0], new Relation(Relation.Segment(new Carrier(), 0, 1), ((Relation)q[1]).Right), q[2], q[3], q[4]),
            Surfaces.Group(q[0], new Relation(Relation.Segment(ReadReceiver(world), 0, 0), ((Relation)q[1]).Right), q[2], q[3], q[4]),
            Surfaces.Group(q[0], q[1], q[2], new Relation(new Placeholder(), new Placeholder()), q[4]),
            Surfaces.Group(q[0], q[1], q[2], q[3], q[4], open)
        };
        foreach (var question in questions)
        {
            var witness = Surfaces.Inspect(question);
            Assert.False(Surfaces.TryDemand(witness, out _)); Assert.False(Surfaces.TryAnswer(witness, out _));
            Assert.False(Surfaces.TryEquality(Surfaces.Compare(witness, witness), out _));
            Assert.True(ReadWitness.TryRead(witness, out _, out _, out var result, out var residue, out _));
            Assert.IsType<Placeholder>(result); Assert.NotNull(residue);
        }
    }

    [Fact]
    public void FractionalProjectionResidueAndOutOfDomainObservationsRemainUnresolved()
    {
        var world = Surfaces.CreateDirect(); Surfaces.Activate(world); Surfaces.Pulse(world, 3);
        Surfaces.Fields(Surfaces.Question(world), 5, out var q);
        var projection = (Relation)q[1];
        var coarse = new Relation(Relation.Segment(ReadReceiver(world), 0, 2), projection.Right);
        var witness = Surfaces.Inspect(Surfaces.Group(q[0], coarse, q[2], q[3], q[4]));
        Assert.False(Surfaces.TryAnswer(witness, out _));
        Assert.True(ReadWitness.TryRead(witness, out _, out _, out _, out var residue, out _));
        Assert.True(Surfaces.Fields(residue, 1, out var reads));
        Assert.True(((Relation)reads[0]).TryReadProjection(out var value, out var remaining));
        Assert.Equal(1, value.Ticks); Assert.True(ProjectionResidue.TryRead(remaining, out _, out var n, out _)); Assert.Equal(1, n);
        Surfaces.Pulse(world, 6);
        Assert.False(Surfaces.TryDemand(Surfaces.Inspect(Surfaces.Question(world)), out _));
    }

    [Fact]
    public void DifferentQuestionContractsDoNotAcquireEquivalenceFromCoincidentAnswers()
    {
        var unit = new Carrier().UnitShape;
        var a = Surfaces.CreateDirect(unit: unit); var b = Surfaces.CreateDirect(unit: unit);
        Surfaces.Activate(a); Surfaces.Activate(b); Surfaces.Pulse(a, 4); Surfaces.Pulse(b, 4);
        Surfaces.Fields(Surfaces.Question(b), 5, out var q); Surfaces.Span(q[3], out var lower, out _); lower.Tick.Set(4);
        Assert.False(Surfaces.TryEquality(Surfaces.Compare(Surfaces.Inspect(Surfaces.Question(a)), Surfaces.Inspect(Surfaces.Question(b))), out _));
        Assert.False(Surfaces.TryEquality(Surfaces.Compare(Surfaces.Inspect(Surfaces.Question(a)), Surfaces.Inspect(Surfaces.QuantityQuestion(a))), out _));
    }

    [Fact]
    public void IndependentUnitPromisesDoNotAcquireEquivalenceFromEqualCoordinates()
    {
        var a = Surfaces.CreateDirect(); var b = Surfaces.CreateDirect();
        Surfaces.Activate(a); Surfaces.Activate(b); Surfaces.Pulse(a, 3); Surfaces.Pulse(b, 3);
        Assert.False(Surfaces.TryEquality(Surfaces.Compare(Surfaces.Inspect(Surfaces.Question(a)),
            Surfaces.Inspect(Surfaces.Question(b))), out _));
    }

    [Fact]
    public void ACompatibleUnitDoesNotLetAnUnadvertisedCarrierUseTheReceiversCalibration()
    {
        var world = Surfaces.CreateDirect();
        var other = new Carrier(ReadReceiver(world).UnitShape); other.Add(3);
        Surfaces.Fields(Surfaces.Question(world), 5, out var q);
        var question = Surfaces.IntervalQuestion(Surfaces.Port(other), q[1], (Relation)q[2], (Relation)q[3], (Relation)q[4]);
        Assert.False(Surfaces.TryAnswer(Surfaces.Inspect(question), out _));
        Assert.Equal((0L, 0L, 0L, 0L), ReadState(world));
    }

    [Fact]
    public void BlockedFlowHasItsOwnAccountAndCannotBeMistakenForGoalSatisfaction()
    {
        var world = Surfaces.CreateBound(2); Surfaces.Activate(world);
        var response = Surfaces.Pulse(world, 4);
        Assert.True(Surfaces.TryResponse(response, out var before, out var after, out var reduction));
        Assert.Equal((3L, 1L, 2L), (before.Ticks, after.Ticks, reduction.Ticks));
        Assert.Equal((0L, 2L, 2L, 0L), ReadState(world)); AssertAccounting(response);
        Assert.True(ReadWitness.TryRead(response, out _, out var step, out _, out _, out _));
        Assert.True(TimeStepSequence.TryReadStep((Relation)step, out _, out _, out _, out _, out var transition, out _, out _));
        Surfaces.Fields(transition, 3, out var transitions);
        ReadWitness.TryRead((Relation)transitions[2], out _, out _, out var result, out _, out _);
        Surfaces.Fields(result, 4, out var account);
        Assert.Equal(4, ((CarrierLocation)account[0]).Ticks);
        Assert.Equal(2, ((CarrierLocation)account[1]).Ticks);
        Assert.True(Surfaces.TryPort(account[2], out var remaining, out var blocked, out _));
        Assert.Equal(0, remaining.Ticks); Assert.Equal(2, blocked.Ticks);
        var stalled = Surfaces.Pulse(world, 1);
        Assert.True(Surfaces.TryResponse(stalled, out before, out after, out reduction));
        Assert.Equal((1L, 1L, 0L), (before.Ticks, after.Ticks, reduction.Ticks));
        var satisfiedWorld = Surfaces.CreateBound(3); Surfaces.Activate(satisfiedWorld); Surfaces.Pulse(satisfiedWorld, 3);
        var satisfied = Surfaces.Pulse(satisfiedWorld, 1);
        Assert.True(Surfaces.TryResponse(satisfied, out before, out after, out reduction));
        Assert.Equal((0L, 0L, 0L), (before.Ticks, after.Ticks, reduction.Ticks));
    }

    [Fact]
    public void WithdrawalChangesParticipationAndPreservesAlreadyDeliveredMaterial()
    {
        var world = Surfaces.CreateBound(); Surfaces.Activate(world); Surfaces.Pulse(world, 2);
        Surfaces.Fields(Surfaces.Application(world), 3, out var f); var route = (Relation)f[2];
        Surfaces.TryWorld(world, out var port, out _, out _, out _, out _); Surfaces.TryPort(port, out var source, out _, out _);
        Assert.True(source.Carrier.Detach(route));
        AssertAccounting(Surfaces.Pulse(world, 1));
        Assert.Equal((1L, 0L, 2L, 0L), ReadState(world));
        var resumed = Surfaces.Activate(world);
        Assert.Equal((0L, 0L, 3L, 0L), ReadState(world));
        Assert.True(Surfaces.TryResponse(resumed, out _, out _, out var improvement)); Assert.Equal(1, improvement.Ticks);
    }

    [Fact]
    public void NativeArchiveRetainsQuestionsBindingsWitnessesAndContinuedLiveBehavior()
    {
        var world = Surfaces.CreateBound(3); Surfaces.Activate(world); var history = Surfaces.Pulse(world, 2);
        var oldQuestion = Surfaces.Inspect(Surfaces.Question(world));
        var roots = GojoNumericFormat.Decode(GojoNumericFormat.Encode(world, history, oldQuestion));
        var restored = Assert.IsType<Relation>(roots[0]);
        Assert.True(Surfaces.TryDemand((Relation)roots[2], out var previous)); Assert.Equal(1, previous.Ticks);
        Surfaces.Pulse(restored, 2);
        Assert.Equal((0L, 1L, 3L, 0L), ReadState(restored));
        Assert.Equal((0L, 0L, 2L, 0L), ReadState(world));
        Assert.True(Surfaces.TryResponse((Relation)roots[1], out var before, out var after, out var reduction));
        Assert.Equal((3L, 1L, 2L), (before.Ticks, after.Ticks, reduction.Ticks));
        Assert.True(Surfaces.TryDemand((Relation)roots[2], out previous)); Assert.Equal(1, previous.Ticks);
    }

    private static Carrier ReadReceiver(Relation world)
    {
        Surfaces.TryWorld(world, out _, out var receiver, out _, out _, out _);
        Surfaces.TryPort(receiver, out var value, out _, out _); return value.Carrier;
    }

    private static (long, long, long, long) ReadState(Relation world)
    {
        Assert.True(Surfaces.TryWorld(world, out var source, out var receiver, out _, out _, out _));
        Assert.True(Surfaces.TryPort(source, out var s, out var st, out _));
        Assert.True(Surfaces.TryPort(receiver, out var r, out var rt, out _)); return (s.Ticks, st.Ticks, r.Ticks, rt.Ticks);
    }

    private static void AssertSame(Relation witness)
    { Assert.True(Surfaces.TryEquality(witness, out var same)); Assert.True(same); }

    private static void AssertAccounting(Relation response)
    {
        Assert.True(ReadWitness.TryRead(response, out _, out var stepElement, out _, out _, out _));
        Assert.True(TimeStepSequence.TryReadStep((Relation)stepElement, out _, out _, out _, out _, out var transition, out _, out _));
        Assert.True(Surfaces.Fields(transition, 3, out var transitions));
        Assert.True(ReadWitness.TryRead((Relation)transitions[2], out _, out _, out var result, out _, out var calculations));
        Assert.True(Surfaces.Fields(result, 4, out var accounting));
        Assert.True(OperationClaim.TryReadResolvedViolation((Relation)accounting[3], out var violation)); Assert.Equal(0, violation.Ticks);
        Assert.True(Surfaces.Fields(calculations, 7, out var reads));
        foreach (var read in reads) Assert.True(ReadWitness.TryRead((Relation)read, out _, out _, out _, out _, out _));
    }
}
