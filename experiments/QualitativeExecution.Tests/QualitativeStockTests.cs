using Gojo.Core;
using Gojo.Patterns;

namespace Gojo.Experiments.QualitativeExecution.Tests;

public sealed class QualitativeStockTests
{
    [Fact]
    public void OccupancyCapacityQuantumAndEveryTransitionAreNativeConnectedStructure()
    {
        var fixture = new QualitativeStockFixture();
        AssertNoNumbers(fixture.Surface);
        Assert.True(Contains(fixture.Surface, fixture.Capacity));
        Assert.True(Contains(fixture.Surface, fixture.EqualQuantum));
        Assert.True(Contains(fixture.Surface, fixture.Law));
        Assert.True(FiniteLaw.TryRead(fixture.Law, out var stateDomain, out var inputs,
            out var outcomes, out var rows));
        Assert.Same(fixture.StateDomain, stateDomain);
        Assert.Same(fixture.InputDomain, inputs);
        Assert.Same(fixture.TransitionDomain, outcomes);
        Assert.Equal(9, rows.Length);
        Assert.Equal(9, Entries(outcomes).Length);
        foreach (var transition in Entries(outcomes))
        {
            Assert.True(QualitativeStockFixture.TryReadTransition(transition, out var before,
                out var input, out var after, out var accepted, out var blocked));
            Assert.Contains(before, Entries(stateDomain));
            Assert.Contains(after, Entries(stateDomain));
            Assert.Contains(input, Entries(inputs));
            var incoming = ReferenceEquals(input, fixture.Hold) ? 0 : 1;
            Assert.Equal(incoming, Entries(accepted).Length + Entries(blocked).Length);
        }
        var equalParts = Entries(fixture.EqualQuantum).Cast<Relation>().ToArray();
        Assert.Equal(2, equalParts.Length);
        Assert.All(equalParts, part => Assert.Same(fixture.Quantum, part.Right));
    }

    [Fact]
    public void CompleteFiniteLawCoversEveryStatePulsePairWithAccountedParticipation()
    {
        var f = new QualitativeStockFixture();
        var states = new[] { f.Empty, f.Available, f.Full };
        var inputs = new[] { f.Charge, f.Hold, f.Draw };
        Relation[,] expected =
        {
            { f.Available, f.Empty, f.Empty },
            { f.Full, f.Available, f.Empty },
            { f.Full, f.Full, f.Available }
        };
        for (var i = 0; i < states.Length; i++)
            for (var j = 0; j < inputs.Length; j++)
            {
                var source = new Placeholder();
                var priorEvent = new Placeholder();
                var state = f.Initial(source, priorEvent, StructuralConstraint.Closed(states[i]));
                var offer = f.Offer(source, new Placeholder(), priorEvent, StructuralConstraint.Closed(inputs[j]));
                var receipt = f.Step(state, offer);
                var next = After(f, receipt);
                AssertState(f, next, expected[i, j]);
                Assert.True(f.TryReadStep(receipt, out var before, out var incoming, out _, out var transitions,
                    out var unresolved));
                Assert.Same(state, before);
                Assert.Same(offer, incoming);
                Assert.Empty(Entries(unresolved));
                Assert.True(QualitativeStockFixture.TryReadTransition(Assert.Single(Entries(transitions)),
                    out var from, out var pulse, out var to, out _, out _));
                Assert.Same(states[i], from);
                Assert.Same(inputs[j], pulse);
                Assert.Same(expected[i, j], to);
                Assert.True(Contains(next, offer));
                AssertNoNumbers(receipt);
            }
    }

    [Fact]
    public void ChargeAccumulatesAndIndependentDrawEventsDecayWithoutMeasuredAmounts()
    {
        var f = new QualitativeStockFixture();
        var source = new Placeholder();
        var start = f.Initial(source, new Placeholder(), StructuralConstraint.Closed(f.Empty));
        var first = Advance(f, start, f.Charge);
        var second = Advance(f, first, f.Charge);
        var decayClock = new Placeholder();
        var decayRoute = new Relation(decayClock, f.Draw);
        var third = Advance(f, second, f.Draw, decayRoute);
        var fourth = Advance(f, third, f.Draw, decayRoute);
        AssertState(f, start, f.Empty);
        AssertState(f, first, f.Available);
        AssertState(f, second, f.Full);
        AssertState(f, third, f.Available);
        AssertState(f, fourth, f.Empty);
        Assert.True(Contains(fourth, start));
        Assert.True(Contains(fourth, decayClock));
        Assert.True(Contains(fourth, decayRoute));
        AssertNoNumbers(fourth);
    }

    [Fact]
    public void SaturationAndUnmetDrawKeepBlockedPulseAndAdvanceOnlyItsEvent()
    {
        var f = new QualitativeStockFixture();
        foreach (var (state, pulse) in new[] { (f.Full, f.Charge), (f.Empty, f.Draw) })
        {
            var source = new Placeholder();
            var oldEvent = new Placeholder();
            var newEvent = new Placeholder();
            var prior = f.Initial(source, oldEvent, StructuralConstraint.Closed(state));
            var offer = f.Offer(source, newEvent, oldEvent, StructuralConstraint.Closed(pulse));
            var receipt = f.Step(prior, offer);
            Assert.True(f.TryReadStep(receipt, out _, out _, out var after, out var support, out var unresolved));
            Assert.Empty(Entries(unresolved));
            AssertState(f, after, state);
            Assert.True(f.TryReadState(after, out _, out var actualEvent, out _));
            Assert.Same(newEvent, actualEvent);
            Assert.True(QualitativeStockFixture.TryReadTransition(Assert.Single(Entries(support)),
                out _, out _, out _, out var accepted, out var blocked));
            Assert.Empty(Entries(accepted));
            Assert.Same(pulse, Assert.Single(Entries(blocked)));
            Assert.True(Contains(after, blocked));
        }
    }

    [Fact]
    public void OpenForeignEmptyAndOpposedAlternativePulsesCannotBecomeAnOperationalDefault()
    {
        var f = new QualitativeStockFixture();
        var source = new Placeholder();
        var eventIdentity = new Placeholder();
        var prior = f.Initial(source, eventIdentity, StructuralConstraint.Closed(f.Available));
        var lookalike = new Relation(f.Charge.Left, f.Charge.Right);
        var supports = new[]
        {
            StructuralConstraint.Open(), StructuralConstraint.Open(f.Charge),
            StructuralConstraint.Closed(), StructuralConstraint.Closed(lookalike),
            StructuralConstraint.Closed(f.Charge, lookalike),
            StructuralConstraint.Closed(f.Charge, f.Draw)
        };
        foreach (var support in supports)
        {
            var offer = f.Offer(source, new Placeholder(), eventIdentity, support);
            var receipt = f.Step(prior, offer);
            AssertUnapplied(f, receipt, prior);
            Assert.True(Contains(receipt, offer));
            Assert.True(Contains(receipt, support));
        }
        var ambiguous = f.Step(prior, f.Offer(source, new Placeholder(), eventIdentity, supports[^1]));
        Assert.True(f.TryReadStep(ambiguous, out _, out _, out _, out var transitions, out _));
        Assert.Equal(2, Entries(transitions).Length);
        var proposed = Entries(transitions).Select(transition =>
        {
            Assert.True(QualitativeStockFixture.TryReadTransition(transition,
                out _, out _, out var next, out _, out _));
            return next;
        });
        Assert.Contains(f.Empty, proposed);
        Assert.Contains(f.Full, proposed);
    }

    [Fact]
    public void UnknownPriorOccupancyIsRetainedRatherThanInventingAnEmptyMemory()
    {
        var f = new QualitativeStockFixture();
        var source = new Placeholder();
        var eventIdentity = new Placeholder();
        foreach (var states in new[]
        {
            StructuralConstraint.Open(), f.StateDomain, StructuralConstraint.Closed(),
            StructuralConstraint.Closed(f.Available, new Relation(f.Available.Left, f.Available.Right))
        })
        {
            var prior = f.Initial(source, eventIdentity, states);
            var receipt = f.Step(prior, f.Offer(source, new Placeholder(), eventIdentity,
                StructuralConstraint.Closed(f.Charge)));
            AssertUnapplied(f, receipt, prior);
            Assert.True(f.TryReadState(prior, out _, out _, out var retained));
            Assert.Same(states, retained);
        }
    }

    [Fact]
    public void ReplayedStaleAndReusedHistoricalEventsDoNotApplyAgain()
    {
        var f = new QualitativeStockFixture();
        var source = new Placeholder();
        var firstEvent = new Placeholder();
        var secondEvent = new Placeholder();
        var start = f.Initial(source, firstEvent, StructuralConstraint.Closed(f.Empty));
        var offer = f.Offer(source, secondEvent, firstEvent, StructuralConstraint.Closed(f.Charge));
        var after = After(f, f.Step(start, offer));
        AssertState(f, after, f.Available);
        AssertUnapplied(f, f.Step(after, offer), after);
        var stale = f.Offer(source, new Placeholder(), firstEvent, StructuralConstraint.Closed(f.Charge));
        AssertUnapplied(f, f.Step(after, stale), after);
        var reusedOldEvent = f.Offer(source, firstEvent, secondEvent, StructuralConstraint.Closed(f.Charge));
        AssertUnapplied(f, f.Step(after, reusedOldEvent), after);
        var duplicateCurrent = f.Offer(source, secondEvent, secondEvent, StructuralConstraint.Closed(f.Charge));
        AssertUnapplied(f, f.Step(after, duplicateCurrent), after);
    }

    [Fact]
    public void SourceReceiverAndSurfaceIdentityAreRequiredForCausalComposition()
    {
        var f = new QualitativeStockFixture();
        var foreign = new QualitativeStockFixture();
        var source = new Placeholder();
        var eventIdentity = new Placeholder();
        var prior = f.Initial(source, eventIdentity, StructuralConstraint.Closed(f.Empty));
        var otherSource = f.Offer(new Placeholder(), new Placeholder(), eventIdentity,
            StructuralConstraint.Closed(f.Charge));
        var otherReceiver = foreign.Offer(source, new Placeholder(), eventIdentity,
            StructuralConstraint.Closed(f.Charge));
        AssertUnapplied(f, f.Step(prior, otherSource), prior);
        AssertUnapplied(f, f.Step(prior, otherReceiver), prior);
        var malformed = new Relation(source, eventIdentity);
        Assert.False(f.TryReadState(malformed, out _, out _, out _));
        AssertUnapplied(f, f.Step(malformed, otherSource), malformed);
    }

    [Fact]
    public void ExplicitOrderingMattersForOpposedInputsAtCapacity()
    {
        var f = new QualitativeStockFixture();
        var start = f.Initial(new Placeholder(), new Placeholder(), StructuralConstraint.Closed(f.Full));
        var chargeThenDraw = Advance(f, Advance(f, start, f.Charge), f.Draw);
        var drawThenCharge = Advance(f, Advance(f, start, f.Draw), f.Charge);
        AssertState(f, chargeThenDraw, f.Available);
        AssertState(f, drawThenCharge, f.Full);
        // No simultaneous cancellation or scheduling law has been assumed.
        Assert.NotSame(chargeThenDraw, drawThenCharge);
    }

    [Fact]
    public void HoldIsStableButDoesNotRestoreAPerturbedStock()
    {
        var f = new QualitativeStockFixture();
        var start = f.Initial(new Placeholder(), new Placeholder(), StructuralConstraint.Closed(f.Available));
        AssertState(f, Advance(f, start, f.Hold), f.Available);
        var disturbed = Advance(f, start, f.Charge);
        var held = Advance(f, disturbed, f.Hold);
        AssertState(f, disturbed, f.Full);
        AssertState(f, held, f.Full);
        // Recovery needs an explicit feedback law that offers a draw.
        AssertState(f, Advance(f, held, f.Draw), f.Available);
        AssertState(f, start, f.Available);
    }

    [Fact]
    public void ReadingMemoryDoesNotInventDecayOrAClockTick()
    {
        var f = new QualitativeStockFixture();
        var source = new Placeholder();
        var eventIdentity = new Placeholder();
        var memory = f.Initial(source, eventIdentity, StructuralConstraint.Closed(f.Full));
        for (var i = 0; i < 5; i++)
        {
            Assert.True(f.TryReadState(memory, out var actualSource, out var actualEvent, out var state));
            Assert.Same(source, actualSource);
            Assert.Same(eventIdentity, actualEvent);
            Assert.Same(f.Full, Assert.Single(Entries(state)));
        }
    }

    [Fact]
    public void DecliningPositiveSupplyDoesNotMeanNegativeNetParticipation()
    {
        var f = new QualitativeStockFixture();
        var source = new Placeholder();
        var priorEvent = new Placeholder();
        var state = f.Initial(source, priorEvent, StructuralConstraint.Closed(f.Available));
        var higherSupply = new Placeholder();
        var lowerSupply = new Placeholder();
        var decliningPositiveSupply = new Relation(new Relation(higherSupply, lowerSupply), f.Charge);
        var offer = f.Offer(source, new Placeholder(), priorEvent,
            StructuralConstraint.Closed(f.Charge), decliningPositiveSupply);
        var after = After(f, f.Step(state, offer));
        AssertState(f, after, f.Full);
        Assert.True(Contains(after, decliningPositiveSupply));
        // The supplier declared an admitted positive pulse. Its separate trend
        // supplies neither a magnitude nor a negative net-input premise.
    }

    private static Relation Advance(QualitativeStockFixture fixture, Relation state,
        IElement input, IElement? evidence = null)
    {
        Assert.True(fixture.TryReadState(state, out var source, out var previousEvent, out _));
        return After(fixture, fixture.Step(state, fixture.Offer(source, new Placeholder(), previousEvent,
            StructuralConstraint.Closed(input), evidence)));
    }

    private static Relation After(QualitativeStockFixture fixture, Relation receipt)
    {
        Assert.True(fixture.TryReadStep(receipt, out _, out _, out var after, out _, out var unresolved));
        Assert.Empty(Entries(unresolved));
        return after;
    }

    private static void AssertState(QualitativeStockFixture fixture, Relation state, IElement expected)
    {
        Assert.True(fixture.TryReadState(state, out _, out _, out var support));
        Assert.Same(expected, Assert.Single(Entries(support)));
    }

    private static void AssertUnapplied(QualitativeStockFixture fixture, Relation receipt, Relation state)
    {
        Assert.True(fixture.TryReadStep(receipt, out _, out _, out var after, out _, out var unresolved));
        Assert.Same(state, after);
        Assert.NotEmpty(Entries(unresolved));
    }

    private static IElement[] Entries(Relation scope)
    {
        Assert.True(StructuralConstraint.TryReadScope(scope, out var entries, out _, out _));
        return entries;
    }

    private static bool Contains(IElement root, IElement target) =>
        Traverse(root).Any(element => ReferenceEquals(element, target));

    private static void AssertNoNumbers(IElement root)
    {
        foreach (var participant in Traverse(root))
        {
            Assert.IsNotType<Carrier>(participant);
            Assert.IsNotType<CarrierLocation>(participant);
        }
    }

    private static IEnumerable<IElement> Traverse(IElement root)
    {
        var seen = new HashSet<IElement>(ReferenceEqualityComparer.Instance);
        var stack = new Stack<IElement>();
        stack.Push(root);
        while (stack.TryPop(out var element))
        {
            if (!seen.Add(element)) continue;
            yield return element;
            if (element is Relation relation)
            {
                stack.Push(relation.Left);
                stack.Push(relation.Right);
            }
        }
    }
}
