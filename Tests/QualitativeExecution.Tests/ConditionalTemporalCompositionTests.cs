using Gojo.Core;
using Gojo.Patterns;

namespace Gojo.Experiments.QualitativeExecution.Tests;

/// <summary>Small executable experiments at the boundaries between a question,
/// a chosen response, an occurrence, an offer and an actual material route.</summary>
public sealed class ConditionalTemporalCompositionTests
{
    [Fact]
    public void OneRefinedPortCanSelectAnActionAndSkipTheOtherSupplierBeforeAnExplicitOffer()
    {
        var f = new Fixture(); f.Advance();
        var query = f.Ask(f.Truths, f.Truths);
        var calls = 0;
        var sufficient = DemandLaw.Service(query, port =>
        {
            calls++;
            Assert.True(DemandLaw.TryReadPort(Assert.IsType<Relation>(port), out _, out var previous));
            Assert.True(QualitativePublication.TryRead(previous, out var source, out var subject, out var eventIdentity,
                out var basis, out var domain, out _, out _));
            return QualitativePublication.Create(source, subject, eventIdentity, basis, domain, S(f.Yes));
        });
        Assert.Equal(1, calls);
        Assert.True(DemandLaw.TryReadAnswer(sufficient, out var command)); Assert.Same(f.Send, command);
        Assert.True(f.Gate.TryReadState(f.State, out _, out var original, out var untouched));
        Assert.Equal(2, E(original).Length); Assert.Empty(E(untouched));

        var decision = f.Decision(S(command), sufficient);
        var receipt = f.Apply(decision, S(f.P));
        var after = f.After(receipt, out var accepted, out var blocked);
        Assert.Same(f.P, Assert.Single(E(accepted))); Assert.Empty(E(blocked));
        Assert.True(f.Gate.TryReadState(after, out _, out var held, out var allocations));
        Assert.Same(f.Q, Assert.Single(E(held))); Assert.Single(E(allocations));
        Assert.True(Contains(receipt, sufficient)); Assert.True(Contains(receipt, query));
        Assert.Empty(receipt.Carriers()); Assert.Empty(receipt.CarrierLocations());
    }

    [Fact]
    public void ASettledQuestionDoesNotSkipIndependentMaintenanceOrAdvanceTimeByBeingRead()
    {
        var f = new Fixture(); f.Advance();
        var query = f.Ask(S(f.No), S(f.No));
        Assert.True(DemandLaw.TryReadAnswer(query, out var command)); Assert.Same(f.Rest, command);
        DemandLaw.Service(query, _ => throw new InvalidOperationException("No finer input is needed."));
        var denied = f.Apply(f.Decision(S(command), query), S(f.P));
        f.State = f.After(denied, out var accepted, out var blocked);
        Assert.Empty(E(accepted)); Assert.Same(f.P, Assert.Single(E(blocked)));

        var amount = new Carrier(); amount.Add(10);
        var beforeRead = f.Event;
        Assert.True(DemandLaw.TryReadAnswer(query, out _));
        Assert.Same(beforeRead, f.Event); Assert.Equal(10UL, amount.Value);
        amount.Remove(10); // Independent physical maintenance remains explicit.
        f.Advance();      // Independent phase drive also remains explicit.
        Assert.Equal(0UL, amount.Value); Assert.Equal(0, amount.Tension);
        Assert.NotSame(beforeRead, f.Event);
        Assert.True(f.Gate.TryReadState(f.State, out _, out var held, out var allocations));
        Assert.Equal(2, E(held).Length); Assert.Empty(E(allocations));
    }

    [Fact]
    public void ACompleteAnswerDoesNotOverrideThePhaseOrSharedReceiverCapacity()
    {
        var f = new Fixture(sites: 1); f.Advance(); f.Advance(); // Closed phase.
        var query = f.Ask(S(f.Yes), f.Truths);
        Assert.True(DemandLaw.TryReadAnswer(query, out var command)); Assert.Same(f.Send, command);
        f.State = f.After(f.Apply(f.Decision(S(command), query), S(f.P)), out var denied, out var blocked);
        Assert.Empty(E(denied)); Assert.Same(f.P, Assert.Single(E(blocked)));
        f.Advance(); f.Advance(); // A new opening, without an implicit retry.
        Assert.True(f.Gate.TryReadState(f.State, out _, out _, out var empty)); Assert.Empty(E(empty));
        var freshQuery = f.Ask(S(f.Yes), f.Truths);
        var attempt = f.Apply(f.Decision(S(f.Send), freshQuery), S(f.P, f.Q));
        f.After(attempt, out var accepted, out var overflow);
        Assert.Same(f.P, Assert.Single(E(accepted))); Assert.Same(f.Q, Assert.Single(E(overflow)));
        Assert.True(f.Gate.TryReadAccounting(attempt, out var incoming, out var moved, out var refused, out var remainder));
        Assert.Equal(E(incoming).Length, E(moved).Length + E(refused).Length + E(remainder).Length);
    }

    [Fact]
    public void OpeningARealCarrierRouteLetsExistingParticipationSettleWithoutAssigningTheTarget()
    {
        var f = new Fixture(sites: 2); f.Advance();
        var source = new Carrier(); var target = new Carrier();
        var route = new Relation(Relation.Point(source, 10), Relation.Point(target, 0));
        var interpretation = new Relation(f.Send, route);
        source.Add(20); // Two offered quanta, with the route still closed.
        Assert.Equal(20UL, source.Value); Assert.Equal(0UL, target.Value);
        var query = f.Ask(S(f.Yes), f.Truths);
        Assert.True(DemandLaw.TryReadAnswer(query, out var response)); Assert.Same(f.Send, response);
        Assert.Equal(0UL, target.Value); // Observation alone has no effect.

        var receipt = f.Apply(f.Decision(S(response), new Relation(query, interpretation)), S(f.P, f.Q));
        f.After(receipt, out var accepted, out var blocked);
        Assert.Equal(2, E(accepted).Length); Assert.Empty(E(blocked));
        // This fixture explicitly interprets the accepted command as route
        // attachment. Local kernel settlement then computes the material effect.
        source.Attach(route);
        Assert.Equal(0UL, source.Value); Assert.Equal(2UL, target.Value); Assert.Equal(0, source.Tension);
        Assert.True(source.IsAttached(route)); Assert.True(target.IsAttached(route));
        var witness = new Relation(receipt, new Relation(interpretation,
            new Relation(source.ValueSlot, target.ValueSlot)));
        Assert.True(Contains(witness, route)); Assert.True(Contains(witness, query));

        Assert.True(source.Detach(route));
        source.Add(10);
        Assert.Equal(10UL, source.Value); Assert.Equal(2UL, target.Value);
        var beforeReading = f.Event;
        Assert.True(DemandLaw.TryReadAnswer(query, out _));
        Assert.Equal(2UL, target.Value); Assert.Same(beforeReading, f.Event);
        // Retained pressure/material and later reconnection are a separate
        // policy from this finite ledger's explicit re-offer contract.
        source.Attach(route);
        Assert.Equal(0UL, source.Value); Assert.Equal(3UL, target.Value);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void AnExplicitChoiceCanContinueOneUncertainBranchWithoutDeclaringAnObservationTrue(bool send)
    {
        var f = new Fixture(); f.Advance();
        var query = f.Ask(f.Truths, f.Truths);
        Assert.False(DemandLaw.TryReadAnswer(query, out _));
        Assert.True(DemandLaw.TryRead(query, out _, out var alternatives, out _, out _, out _));
        Assert.Equal(2, E(alternatives).Length);
        var proposal = f.Decision(alternatives, query);
        var chooser = new Placeholder();
        var selected = send ? f.Send : f.Rest;
        // External sampling is supplied explicitly. The operation checks one
        // member; it assigns neither probabilities nor a truth value to inputs.
        var sample = new Relation(chooser, new Relation(f.Event, selected));
        var choice = FiniteChoice.Select(proposal, chooser, selected, sample);
        Assert.True(FiniteChoice.TryReadSelected(choice, out var command));
        Assert.True(QualitativePublication.TryRead(command, out _, out _, out _, out _, out _, out var one, out _));
        Assert.Same(selected, Assert.Single(E(one)));
        var receipt = f.Apply(command, S(f.P));
        f.After(receipt, out var accepted, out var blocked);
        Assert.Equal(send ? 1 : 0, E(accepted).Length); Assert.Equal(send ? 0 : 1, E(blocked).Length);
        Assert.True(Contains(receipt, proposal)); Assert.True(Contains(receipt, sample));
        Assert.False(DemandLaw.TryReadAnswer(query, out _)); // World uncertainty remains.
        Assert.True(QualitativePublication.TryRead(proposal, out _, out _, out _, out _, out _, out var original, out _));
        Assert.Equal(2, E(original).Length);
    }

    private sealed class Fixture
    {
        public IElement Source { get; } = new Placeholder();
        public IElement Target { get; } = new Placeholder();
        public IElement Controller { get; } = new Placeholder();
        public IElement ClockSource { get; } = new Placeholder();
        public IElement Basis { get; } = new Placeholder();
        public IElement Sense { get; } = new Placeholder();
        public IElement Act { get; } = new Placeholder();
        public IElement Close { get; } = new Placeholder();
        public IElement No { get; } = new Placeholder();
        public IElement Yes { get; } = new Placeholder();
        public IElement P { get; } = new Placeholder();
        public IElement Q { get; } = new Placeholder();
        public IElement Passage { get; } = new Placeholder();
        public Relation Truths { get; }
        public Relation Send { get; }
        public Relation Rest { get; }
        public Relation Commands { get; }
        public Relation DecisionLaw { get; }
        public Relation Routes { get; }
        public PhaseRecurrence Clock { get; }
        public ConditionalPassage Gate { get; }
        public Relation Phase { get; private set; }
        public Relation State { get; set; }
        public IElement Event { get { Assert.True(Clock.TryReadState(Phase, out _, out var e, out _)); return e; } }

        public Fixture(int sites = 1)
        {
            Truths = S(No, Yes); Send = new Relation(Source, Passage); Rest = new Relation(Source, Source);
            Commands = S(Send, Rest);
            DecisionLaw = FiniteLaw.Create(Truths, Truths, Truths,
                FiniteLaw.Row(No, No, S(No)), FiniteLaw.Row(No, Yes, S(Yes)),
                FiniteLaw.Row(Yes, No, S(Yes)), FiniteLaw.Row(Yes, Yes, S(Yes)));
            Routes = S(new Relation(No, Rest), new Relation(Yes, Send));
            var phases = S(Sense, Act, Close);
            Clock = new PhaseRecurrence(phases, S(new Relation(Sense, Act), new Relation(Act, Close), new Relation(Close, Sense)));
            var rows = new[] { Send, Rest }.SelectMany(command => new[] { Sense, Act, Close }.Select(phase =>
                FiniteLaw.Row(command, phase, ReferenceEquals(command, Send) && ReferenceEquals(phase, Act) ? S(Passage) : S())));
            var opening = FiniteLaw.Create(Commands, phases, S(Passage), rows.ToArray());
            var capacity = Enumerable.Range(0, sites).Select(_ => (IElement)new Relation(Target, new Placeholder())).ToArray();
            Gate = new ConditionalPassage(Source, Target, ConditionalPassage.Decision(Controller, Target, Basis, Commands),
                Clock, ClockSource, opening, S(ConditionalPassage.Capacity(Passage, capacity)));
            var initial = new Placeholder(); Phase = Clock.Initial(ClockSource, initial, S(Sense));
            State = Gate.Initial(initial, S(P, Q));
        }

        public Relation Ask(Relation left, Relation right)
        {
            var a = DemandLaw.Port(new Placeholder(), QualitativePublication.Create(new Placeholder(), Source,
                Event, Basis, Truths, left));
            var b = DemandLaw.Port(new Placeholder(), QualitativePublication.Create(new Placeholder(), Target,
                Event, Basis, Truths, right));
            return DemandLaw.Ask(DemandLaw.Create(DecisionLaw, a, b, Routes));
        }
        public Relation Decision(Relation support, IElement evidence) =>
            QualitativePublication.Create(Controller, Target, Event, Basis, Commands, support, evidence);
        public void Advance()
        {
            var receipt = Clock.Step(Phase, Clock.Drive(ClockSource, new Placeholder(), Event));
            Assert.True(Clock.TryReadAccepted(receipt, out _, out _, out _));
            Assert.True(Clock.TryReadStep(receipt, out _, out _, out var after, out _, out _, out _)); Phase = after;
        }
        public Relation Apply(Relation decision, Relation packets)
        {
            Assert.True(Gate.TryReadState(State, out var prior, out _, out _));
            return Gate.Step(State, Gate.Offer(Event, prior, Phase, decision, packets));
        }
        public Relation After(Relation receipt, out Relation accepted, out Relation blocked)
        {
            Assert.True(Gate.TryReadStep(receipt, out _, out _, out var after, out accepted, out blocked, out var pending));
            Assert.Empty(E(pending)); return after;
        }
    }
    private static Relation S(params IElement[] values) => StructuralConstraint.Closed(values);
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
    private static bool Contains(IElement root, IElement target)
    {
        var seen = new HashSet<IElement>(ReferenceEqualityComparer.Instance); var queue = new Stack<IElement>(); queue.Push(root);
        while (queue.TryPop(out var e))
        {
            if (!seen.Add(e)) continue; if (ReferenceEquals(e, target)) return true;
            if (e is Relation r) { queue.Push(r.Left); queue.Push(r.Right); }
        }
        return false;
    }
}
