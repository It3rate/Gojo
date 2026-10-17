using Gojo.Core;
using Gojo.Patterns;

namespace Gojo.Experiments.QualitativeExecution.Tests;

public sealed class ConditionalPassageTests
{
    [Fact]
    public void ASelectedResponseNeedsAnActualOfferAndAvailableReceiverSite()
    {
        var f = new Fixture(); f.Advance();
        var receipt = f.Apply(f.State, S(f.SendA), S(f.P));
        var after = Read(f, receipt, out var accepted, out var blocked, out var pending);
        Assert.Same(f.P, Assert.Single(E(accepted))); Assert.Empty(E(blocked)); Assert.Empty(E(pending));
        Assert.True(f.Gate.TryReadState(after, out var eventIdentity, out var available, out var occupied));
        Assert.Same(f.Event, eventIdentity); Assert.Equal(new[] { f.Q, f.R }, E(available));
        var allocation = Assert.IsType<Relation>(Assert.Single(E(occupied)));
        Assert.Same(f.P, allocation.Left);
        var placement = Assert.IsType<Relation>(allocation.Right);
        Assert.Same(f.PassageA, placement.Left); Assert.Same(f.Site, placement.Right);
        Assert.Empty(receipt.Carriers()); Assert.Empty(receipt.CarrierLocations());

        // A legitimate independent read of permission does not itself transfer.
        var noSupply = f.Apply(f.State, S(f.SendA), S());
        Read(f, noSupply, out var none, out _, out _); Assert.Empty(E(none));
        Assert.True(f.Gate.TryReadState(f.State, out _, out var original, out var empty));
        Assert.Equal(3, E(original).Length); Assert.Empty(E(empty));
    }

    [Fact]
    public void SharedSiteCapacityIsNotDuplicatedAcrossConditionalBranches()
    {
        var f = new Fixture(); f.Advance();
        f.State = Read(f, f.Apply(f.State, S(f.SendA), S(f.P)), out _, out _, out _);
        f.Advance(); f.Advance(); f.Advance();
        var receipt = f.Apply(f.State, S(f.SendB), S(f.Q, f.P));
        var after = Read(f, receipt, out var accepted, out var blocked, out var pending);
        Assert.Empty(E(accepted)); Assert.Equal(2, E(blocked).Length); Assert.Empty(E(pending));
        Assert.True(f.Gate.TryReadState(after, out _, out var available, out var occupied));
        Assert.Equal(new[] { f.Q, f.R }, E(available)); Assert.Single(E(occupied));
        Assert.True(Contains(receipt, f.Site)); Assert.True(Contains(receipt, f.PassageB));
    }

    [Fact]
    public void IncomingParticipationPartitionsIntoAcceptedAndBlockedWithoutLosingIdentity()
    {
        var f = new Fixture(sites: 2); f.Advance();
        var after = Read(f, f.Apply(f.State, S(f.SendA), S(f.P, f.Q, f.R)),
            out var accepted, out var blocked, out var pending);
        Assert.Equal(new[] { f.P, f.Q }, E(accepted)); Assert.Equal(new[] { f.R }, E(blocked)); Assert.Empty(E(pending));
        Assert.Equal(3, E(accepted).Concat(E(blocked)).Distinct(ReferenceEqualityComparer.Instance).Count());
        Assert.True(f.Gate.TryReadState(after, out _, out var held, out var occupied));
        Assert.Same(f.R, Assert.Single(E(held))); Assert.Equal(2, E(occupied).Length);
    }

    [Fact]
    public void AllowedAndDeniedPossibilitiesCannotCollapseIntoAFalsePermission()
    {
        var f = new Fixture(); f.Advance();
        var read = FiniteLaw.Evaluate(f.Gate.OpeningLaw, S(f.SendA, f.Rest), S(f.Act));
        Assert.True(FiniteLaw.TryReadResult(read, out var union, out _, out _));
        Assert.Same(f.PassageA, Assert.Single(E(union))); // Tempting but insufficient!
        var after = Read(f, f.Apply(f.State, S(f.SendA, f.Rest), S(f.P)), out var accepted, out _, out var pending);
        Assert.Same(f.State, after); Assert.Empty(E(accepted)); Assert.NotEmpty(E(pending));
    }

    [Fact]
    public void ASecondAdmissibilityClaimCannotHideADenialForTheSameInputPair()
    {
        var f = new Fixture(conflictingActRow: true); f.Advance();
        var receipt = f.Apply(f.State, S(f.SendA), S(f.P));
        var after = Read(f, receipt, out var accepted, out var blocked, out var pending);
        Assert.Same(f.State, after); Assert.Empty(E(accepted)); Assert.Empty(E(blocked)); Assert.NotEmpty(E(pending));
        Assert.True(f.Gate.TryReadAccounting(receipt, out var incoming, out var moved, out var denied, out var remainder));
        Assert.Same(f.P, Assert.Single(E(incoming))); Assert.Empty(E(moved)); Assert.Empty(E(denied));
        Assert.Same(f.P, Assert.Single(E(remainder)));
    }

    [Fact]
    public void AnUnrelatedClockBranchCannotContinueTheGateByReusingOnlyItsLastEvent()
    {
        var f = new Fixture();
        var initial = f.Event; f.Advance();
        f.State = Read(f, f.Apply(f.State, S(f.SendA), S(f.P)), out _, out _, out _);
        f.Advance(); var sharedLast = f.Event;
        f.State = Read(f, f.Apply(f.State, S(f.Rest), S()), out _, out _, out _);
        var branch = f.Clock.Initial(f.ClockSource, initial, S(f.Sense));
        foreach (var next in new IElement[] { new Placeholder(), sharedLast, new Placeholder(), new Placeholder() })
        {
            Assert.True(f.Clock.TryReadState(branch, out _, out var previous, out _));
            var step = f.Clock.Step(branch, f.Clock.Drive(f.ClockSource, next, previous));
            Assert.True(f.Clock.TryReadAccepted(step, out _, out _, out _));
            Assert.True(f.Clock.TryReadStep(step, out _, out _, out branch, out _, out _, out _));
        }
        Assert.True(f.Clock.TryReadState(branch, out _, out var current, out _));
        var decision = QualitativePublication.Create(f.Controller, f.Target, current, f.Basis, f.Commands, S(f.SendB));
        var receipt = f.Gate.Step(f.State, f.Gate.Offer(current, sharedLast, branch, decision, S(f.Q)));
        var after = Read(f, receipt, out var accepted, out var blocked, out _);
        Assert.Same(f.State, after); Assert.Empty(E(accepted)); Assert.Same(f.Q, Assert.Single(E(blocked)));
    }

    [Fact]
    public void UncertaintyCanPermitOnePassageWhenEveryAtomicCaseAgrees()
    {
        var f = new Fixture(samePassage: true); f.Advance();
        Read(f, f.Apply(f.State, S(f.SendA, f.SendB), S(f.P)), out var accepted, out var blocked, out var pending);
        Assert.Same(f.P, Assert.Single(E(accepted))); Assert.Empty(E(blocked)); Assert.Empty(E(pending));
    }

    [Fact]
    public void RepeatedSupportForTheSamePassageIsOneAlternativeRatherThanCompetingRoutes()
    {
        var f = new Fixture(repeatedPassage: true); f.Advance();
        Read(f, f.Apply(f.State, S(f.SendA), S(f.P)), out var accepted, out var blocked, out var pending);
        Assert.Same(f.P, Assert.Single(E(accepted))); Assert.Empty(E(blocked)); Assert.Empty(E(pending));
    }

    [Fact]
    public void CompetingPassagesPreserveAlternativesInsteadOfChoosingTheFirst()
    {
        var f = new Fixture(); f.Advance();
        var after = Read(f, f.Apply(f.State, S(f.SendA, f.SendB), S(f.P)), out var accepted, out _, out var pending);
        Assert.Same(f.State, after); Assert.Empty(E(accepted)); Assert.NotEmpty(E(pending));
    }

    [Fact]
    public void AClosedDenialAndAMissingLawHaveDifferentWitnesses()
    {
        var f = new Fixture(); f.Advance(); f.Advance(); // Decay phase denies both passages explicitly.
        var denied = Read(f, f.Apply(f.State, S(f.SendA), S(f.P)), out var accepted, out var blocked, out var pending);
        Assert.NotSame(f.State, denied); Assert.Empty(E(accepted)); Assert.Same(f.P, Assert.Single(E(blocked)));
        Assert.Empty(E(pending));
        var hole = new Fixture(missingActRow: true); hole.Advance();
        var unknown = Read(hole, hole.Apply(hole.State, S(hole.SendA), S(hole.P)), out _, out _, out var missing);
        Assert.Same(hole.State, unknown); Assert.NotEmpty(E(missing));
    }

    [Fact]
    public void PhaseProgressDoesNotAutomaticallyRetryBlockedParticipation()
    {
        var f = new Fixture(); f.Advance(); f.Advance();
        f.State = Read(f, f.Apply(f.State, S(f.SendA), S(f.P)), out _, out _, out _);
        f.Advance(); f.Advance(); // Returning to Act does not call the passage.
        Assert.True(f.Gate.TryReadState(f.State, out _, out var stillAvailable, out var none));
        Assert.Equal(3, E(stillAvailable).Length); Assert.Empty(E(none));
        Read(f, f.Apply(f.State, S(f.SendA), S(f.P)), out var accepted, out _, out _);
        Assert.Same(f.P, Assert.Single(E(accepted)));
    }

    [Theory]
    [InlineData(0)] [InlineData(1)] [InlineData(2)] [InlineData(3)] [InlineData(4)]
    public void FreshPhaseDoesNotRepairAWrongDecisionContract(int changed)
    {
        var f = new Fixture(); f.Advance();
        var publication = QualitativePublication.Create(changed == 0 ? new Placeholder() : f.Controller,
            changed == 1 ? new Placeholder() : f.Target, changed == 2 ? new Placeholder() : f.Event,
            changed == 3 ? new Placeholder() : f.Basis,
            changed == 4 ? S(f.SendA, f.SendB, f.Rest) : f.Commands, S(f.SendA));
        Assert.True(f.Gate.TryReadState(f.State, out var prior, out _, out _));
        var receipt = f.Gate.Step(f.State, f.Gate.Offer(f.Event, prior, f.Phase, publication, S(f.P)));
        var after = Read(f, receipt, out var accepted, out var blocked, out _);
        Assert.Same(f.State, after); Assert.Empty(E(accepted)); Assert.Same(f.P, Assert.Single(E(blocked)));
    }

    [Fact]
    public void ReplayingAnAcceptedOccurrenceCannotSpendItsPacketAgain()
    {
        var f = new Fixture(); f.Advance();
        var offer = f.MakeOffer(f.State, S(f.SendA), S(f.P));
        var after = Read(f, f.Gate.Step(f.State, offer), out _, out _, out _);
        var repeated = Read(f, f.Gate.Step(after, offer), out var accepted, out var blocked, out _);
        Assert.Same(after, repeated); Assert.Empty(E(accepted)); Assert.Same(f.P, Assert.Single(E(blocked)));
        // A retained before-state remains an explicit alternative history, not a global second spend.
        Read(f, f.Gate.Step(f.State, offer), out var historical, out _, out _); Assert.Single(E(historical));
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void OpenOrRepeatedIncomingParticipationCannotInventSupply(bool repeated)
    {
        var f = new Fixture(); f.Advance();
        var offered = repeated ? S(f.P, f.P) : StructuralConstraint.Open(f.P);
        var after = Read(f, f.Apply(f.State, S(f.SendA), offered), out var accepted, out _, out var pending);
        Assert.Same(f.State, after); Assert.Empty(E(accepted)); Assert.NotEmpty(E(pending));
    }

    [Fact]
    public void UnknownOrContradictoryDecisionSupportDoesNotBecomeAnAction()
    {
        var f = new Fixture(); f.Advance();
        foreach (var commands in new[] { StructuralConstraint.Open(f.SendA), S(), S(new Placeholder()) })
        {
            var after = Read(f, f.Apply(f.State, commands, S(f.P)), out var accepted, out _, out var pending);
            Assert.Same(f.State, after); Assert.Empty(E(accepted)); Assert.NotEmpty(E(pending));
        }
    }

    [Fact]
    public void AForeignHistoryOrUnownedPacketCannotBorrowTheSourceInventory()
    {
        var f = new Fixture(); var other = new Fixture(); f.Advance(); other.Advance();
        var wrong = f.Gate.Step(other.State, f.MakeOffer(f.State, S(f.SendA), S(f.P)));
        Assert.True(f.Gate.TryReadStep(wrong, out _, out _, out var after, out var accepted, out _, out var pending));
        Assert.Same(other.State, after); Assert.Empty(E(accepted)); Assert.NotEmpty(E(pending));
        Read(f, f.Apply(f.State, S(f.SendA), S(other.P)), out var noSupply, out var blocked, out _);
        Assert.Empty(E(noSupply)); Assert.Same(other.P, Assert.Single(E(blocked)));
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
        public IElement Decay { get; } = new Placeholder();
        public IElement PassageA { get; } = new Placeholder();
        public IElement PassageB { get; } = new Placeholder();
        public IElement P { get; } = new Placeholder();
        public IElement Q { get; } = new Placeholder();
        public IElement R { get; } = new Placeholder();
        public IElement Site { get; }
        public Relation SendA { get; }
        public Relation SendB { get; }
        public Relation Rest { get; }
        public Relation Commands { get; }
        public PhaseRecurrence Clock { get; }
        public ConditionalPassage Gate { get; }
        public Relation Phase { get; private set; }
        public Relation State { get; set; }
        public IElement Event { get { Clock.TryReadState(Phase, out _, out var e, out _); return e; } }

        public Fixture(int sites = 1, bool samePassage = false, bool missingActRow = false,
            bool conflictingActRow = false, bool repeatedPassage = false)
        {
            Site = new Relation(Target, new Placeholder());
            SendA = new Relation(Source, PassageA); SendB = new Relation(Source, PassageB); Rest = new Relation(Source, Source);
            Commands = S(SendA, SendB, Rest);
            var phases = S(Sense, Act, Decay);
            Clock = new PhaseRecurrence(phases, S(new Relation(Sense, Act), new Relation(Act, Decay), new Relation(Decay, Sense)));
            var rows = new List<Relation>();
            foreach (var command in new[] { SendA, SendB, Rest })
                foreach (var phase in new[] { Sense, Act, Decay })
                    if (!(missingActRow && ReferenceEquals(command, SendA) && ReferenceEquals(phase, Act)))
                        rows.Add(FiniteLaw.Row(command, phase, ReferenceEquals(phase, Act) && !ReferenceEquals(command, Rest)
                            ? repeatedPassage && ReferenceEquals(command, SendA) ? S(PassageA, PassageA)
                                : S(ReferenceEquals(command, SendA) || samePassage ? PassageA : PassageB) : S()));
            if (conflictingActRow) rows.Add(FiniteLaw.Row(SendA, Act, S()));
            var law = FiniteLaw.Create(Commands, phases, S(PassageA, PassageB), rows.ToArray());
            IElement[] capacity = sites == 1 ? [Site] : [Site, new Relation(Target, new Placeholder())];
            Gate = new ConditionalPassage(Source, Target, ConditionalPassage.Decision(Controller, Target, Basis, Commands),
                Clock, ClockSource, law, S(ConditionalPassage.Capacity(PassageA, capacity), ConditionalPassage.Capacity(PassageB, capacity)));
            var initial = new Placeholder(); Phase = Clock.Initial(ClockSource, initial, S(Sense));
            State = Gate.Initial(initial, S(P, Q, R));
        }
        public void Advance()
        {
            var receipt = Clock.Step(Phase, Clock.Drive(ClockSource, new Placeholder(), Event));
            Assert.True(Clock.TryReadAccepted(receipt, out _, out _, out _));
            Assert.True(Clock.TryReadStep(receipt, out _, out _, out var next, out _, out _, out _)); Phase = next;
        }
        public Relation MakeOffer(Relation state, Relation commands, Relation packets)
        {
            Assert.True(Gate.TryReadState(state, out var prior, out _, out _));
            var publication = QualitativePublication.Create(Controller, Target, Event, Basis, Commands, commands);
            return Gate.Offer(Event, prior, Phase, publication, packets);
        }
        public Relation Apply(Relation state, Relation commands, Relation packets) => Gate.Step(state, MakeOffer(state, commands, packets));
    }

    private static Relation Read(Fixture f, Relation receipt, out Relation accepted, out Relation blocked, out Relation pending)
    {
        Assert.True(f.Gate.TryReadStep(receipt, out _, out _, out var after, out accepted, out blocked, out pending));
        return after;
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
