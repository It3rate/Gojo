using Gojo.Core;
using Gojo.Patterns;

namespace Gojo.Experiments.QualitativeExecution.Tests;

public sealed class QualitativeImpulseAccumulatorTests
{
    [Fact]
    public void ActualPacketsOccupyActualSitesBeforeTheThresholdIsReady()
    {
        var f = new Fixture(); var initial = f.A.State;
        f.A.Receive(new Placeholder(), S(f.P));
        Assert.Same(f.A.Waiting, Outcome(f.A.Observe()));
        Assert.True(f.A.TryReadReadiness(f.A.Observe(), out var state, out var filled, out var missing, out var pending));
        Assert.Same(f.A.State, state); Assert.Same(f.B, Assert.Single(E(missing))); Assert.Empty(E(pending));
        var placement = Assert.IsType<Relation>(Assert.Single(E(filled)));
        Assert.Same(f.P, placement.Left); Assert.Same(f.ASite, placement.Right);
        f.A.Receive(new Placeholder(), S(f.Q)); Assert.Same(f.A.Ready, Outcome(f.A.Observe()));
        Assert.True(f.A.TryReadState(initial, out _, out var empty, out var unused, out _));
        Assert.Empty(E(empty)); Assert.Empty(E(unused));
        Assert.Empty(f.A.State.Carriers()); Assert.Empty(f.A.State.CarrierLocations());
        Assert.Same(f.Owner, f.A.InputPort.Left); Assert.Same(f.A.Receiver, f.A.InputPort.Right);
    }

    [Fact]
    public void ObservingAndAskingNeverDrainButAnExplicitFiringDoes()
    {
        var f = new Fixture(); f.Fill(); var before = f.A.State;
        var publication = f.A.Observe(); var question = f.Question();
        Assert.Same(before, f.A.State); Assert.Same(publication, f.A.Observe());
        Assert.True(QualitativeQuestion.TryReadAnswer(question, out var answer)); Assert.Same(f.Act, answer);
        var receipt = f.A.Fire(new Placeholder(), question);
        Assert.True(f.A.TryReadReceipt(receipt, out var old, out var request, out var after,
            out var accepted, out var blocked, out var remainder, out var drained, out var pending));
        Assert.Same(before, old); Assert.Same(f.A.State, after); Assert.NotSame(before, after);
        Assert.Equal(new[] { f.P, f.Q }, E(accepted)); Assert.Equal(E(accepted), E(drained));
        Assert.Empty(E(blocked)); Assert.Empty(E(remainder)); Assert.Empty(E(pending));
        Assert.True(Contains(request, question)); Assert.True(Contains(request, publication));
        Assert.True(f.A.TryReadProofs(receipt, out var proofs)); Assert.True(Contains(proofs, publication)); Assert.True(Contains(proofs, question));
        Assert.Same(f.A.Waiting, Outcome(f.A.Observe()));
        Accounting(f.A, receipt, new[] { f.P, f.Q }, new[] { f.P, f.Q }, [], []);
    }

    [Fact]
    public void ExplicitThresholdSubsetDrainsOnlyItsSitesAndRetainsSurplus()
    {
        var f = new Fixture(thresholdBoth: false); f.Fill();
        var receipt = f.A.Fire(new Placeholder(), f.Question());
        Assert.True(f.A.TryReadReceipt(receipt, out _, out _, out _, out _, out _, out _, out var drained, out _));
        Assert.Same(f.P, Assert.Single(E(drained)));
        Assert.True(f.A.TryReadState(out _, out var allocations, out var used, out _));
        var remaining = Assert.IsType<Relation>(Assert.Single(E(allocations)));
        Assert.Same(f.Q, remaining.Left); Assert.Same(f.B, remaining.Right); Assert.Equal(new[] { f.P, f.Q }, E(used));
        Assert.Same(f.A.Waiting, Outcome(f.A.Observe()));
    }

    [Fact]
    public void CapacityOverflowIsBlockedAndMayBeOfferedAgainAfterRelease()
    {
        var f = new Fixture(); var extra = new Placeholder();
        var receipt = f.A.Receive(new Placeholder(), S(f.P, f.Q, extra));
        Accounting(f.A, receipt, new[] { f.P, f.Q, extra }, new[] { f.P, f.Q }, new[] { extra }, []);
        f.A.Fire(new Placeholder(), f.Question());
        var retry = f.A.Receive(new Placeholder(), S(extra));
        Accounting(f.A, retry, new[] { extra }, new[] { extra }, [], []);
    }

    [Fact]
    public void UsedPacketIdentityCannotBeReplayedEvenAfterItDrains()
    {
        var f = new Fixture(); f.Fill(); f.A.Fire(new Placeholder(), f.Question());
        var replay = f.A.Receive(new Placeholder(), S(f.P, f.Q));
        Accounting(f.A, replay, new[] { f.P, f.Q }, [], new[] { f.P, f.Q }, []);
        Assert.True(f.A.TryReadState(out _, out var allocations, out var used, out _));
        Assert.Empty(E(allocations)); Assert.Equal(new[] { f.P, f.Q }, E(used));
    }

    [Fact]
    public void AnEventReplayDoesNotCreateAnotherOccupancyTransition()
    {
        var f = new Fixture(); var eventIdentity = new Placeholder();
        f.A.Receive(eventIdentity, S(f.P)); var state = f.A.State;
        var replay = f.A.Receive(eventIdentity, S(f.Q));
        Assert.Same(state, f.A.State); Accounting(f.A, replay, new[] { f.Q }, [], new[] { f.Q }, []);
        Assert.True(f.A.TryReadState(out var current, out _, out _, out var history));
        Assert.Same(eventIdentity, current); Assert.Equal(2, E(history).Length);
    }

    [Fact]
    public void DuplicatePacketOccurrencesStayPendingRatherThanCloningThePacket()
    {
        var f = new Fixture(); var state = f.A.State; var offered = S(f.P, f.P);
        var receipt = f.A.Receive(new Placeholder(), offered);
        Assert.Same(state, f.A.State); Accounting(f.A, receipt, new[] { f.P, f.P }, [], [], new[] { f.P, f.P });
        Assert.True(f.A.TryReadReceipt(receipt, out _, out _, out _, out _, out _, out _, out _, out var pending));
        Assert.NotEmpty(E(pending));
    }

    [Fact]
    public void AnOpenOfferKeepsItsUnresolvedTailAndDoesNotPartiallyFillCapacity()
    {
        var f = new Fixture(); var state = f.A.State; var offered = StructuralConstraint.Open(f.P);
        var receipt = f.A.Receive(new Placeholder(), offered);
        Assert.Same(state, f.A.State);
        Assert.True(f.A.TryReadAccounting(receipt, out var incoming, out var accepted, out var blocked, out var remainder));
        Assert.Same(offered, incoming); Assert.Same(offered, remainder); Assert.Empty(E(accepted)); Assert.Empty(E(blocked));
        Assert.False(Closed(remainder));
    }

    [Fact]
    public void OpenForeignAndEmptyThresholdsCannotInventReadiness()
    {
        var site = new Placeholder();
        foreach (var threshold in new[] { StructuralConstraint.Open(site), S(new Placeholder()), S() })
        {
            var a = new QualitativeImpulseAccumulator(new Placeholder(), S(site), threshold); var before = a.State;
            var receipt = a.Receive(new Placeholder(), S(new Placeholder()));
            Assert.Same(before, a.State);
            Assert.True(QualitativePublication.TryRead(a.Observe(), out _, out _, out _, out _, out _, out var support, out _));
            Assert.False(Closed(support));
            Assert.True(a.TryReadReadiness(a.Observe(), out _, out _, out _, out var pending)); Assert.NotEmpty(E(pending));
            var question = a.Ask(S(new Relation(a.Ready, a.Ready), new Relation(a.Waiting, a.Ready)), new Placeholder());
            Assert.False(QualitativeQuestion.TryReadAnswer(question, out _));
            a.Fire(new Placeholder(), question); Assert.Same(before, a.State);
            Assert.True(a.TryReadReceipt(receipt, out _, out _, out _, out var accepted, out _, out _, out _, out _)); Assert.Empty(E(accepted));
        }
    }

    [Fact]
    public void AUniqueActionForWaitingStillDoesNotReleasePartialStock()
    {
        var f = new Fixture(); f.A.Receive(new Placeholder(), S(f.P)); var before = f.A.State;
        var question = f.A.Ask(S(new Relation(f.A.Ready, f.Act), new Relation(f.A.Waiting, f.Act)), f.Refine);
        Assert.True(QualitativeQuestion.TryReadAnswer(question, out var answer)); Assert.Same(f.Act, answer);
        var receipt = f.A.Fire(new Placeholder(), question);
        Assert.NotSame(before, f.A.State); // witnessed denial consumes this event, not the packet
        Accounting(f.A, receipt, new[] { f.P }, [], new[] { f.P }, []);
        Assert.True(f.A.TryReadReceipt(receipt, out _, out _, out _, out _, out _, out _, out var drained, out var pending));
        Assert.Empty(E(drained)); Assert.Empty(E(pending));
    }

    [Fact]
    public void AnAmbiguousOrMissingResponseCannotFire()
    {
        var f = new Fixture(); f.Fill(); var state = f.A.State;
        foreach (var routes in new[] { S(), S(new Relation(f.A.Ready, f.Act), new Relation(f.A.Ready, new Placeholder())) })
        {
            var question = f.A.Ask(routes, f.Refine); var receipt = f.A.Fire(new Placeholder(), question);
            Assert.Same(state, f.A.State);
            Accounting(f.A, receipt, new[] { f.P, f.Q }, [], [], new[] { f.P, f.Q });
        }
    }

    [Fact]
    public void ForgedQuestionAnswerIsCheckedAgainstItsActualCurrentRoutes()
    {
        var f = new Fixture(); f.Fill(); var state = f.A.State;
        var honest = f.Question(); var other = new Placeholder();
        Assert.True(QualitativeQuestion.TryRead(honest, out _, out var requests, out var proofs));
        var forged = new Relation(honest.Left, new Relation(S(other), new Relation(requests, proofs)));
        Assert.True(QualitativeQuestion.TryReadAnswer(forged, out var claimed)); Assert.Same(other, claimed);
        f.A.Fire(new Placeholder(), forged); Assert.Same(state, f.A.State);
        f.A.Fire(new Placeholder(), new Relation(S(), S())); Assert.Same(state, f.A.State);
        var copied = new Relation(honest.Left, honest.Right);
        f.A.Fire(new Placeholder(), copied); Assert.NotSame(state, f.A.State);
    }

    [Fact]
    public void AQuestionFromBeforeAnotherInputCannotDrainTheCurrentState()
    {
        var f = new Fixture(); f.Fill(); var stale = f.Question();
        f.A.Receive(new Placeholder(), S(new Placeholder())); var current = f.A.State;
        var receipt = f.A.Fire(new Placeholder(), stale); Assert.Same(current, f.A.State);
        Accounting(f.A, receipt, new[] { f.P, f.Q }, [], [], new[] { f.P, f.Q });
        f.A.Fire(new Placeholder(), f.Question()); Assert.NotSame(current, f.A.State);
    }

    [Fact]
    public void ReplayingAFiringQuestionCannotDrainNewPacketsOrAnEmptyAccumulator()
    {
        var f = new Fixture(); f.Fill(); var question = f.Question(); var eventIdentity = new Placeholder();
        f.A.Fire(eventIdentity, question); var empty = f.A.State;
        f.A.Fire(eventIdentity, question); Assert.Same(empty, f.A.State);
        var p = new Placeholder(); var q = new Placeholder(); f.A.Receive(new Placeholder(), S(p, q)); var filled = f.A.State;
        var replay = f.A.Fire(new Placeholder(), question); Assert.Same(filled, f.A.State);
        Accounting(f.A, replay, new[] { p, q }, [], [], new[] { p, q });
    }

    [Fact]
    public void PacketShapeEqualityDoesNotReplaceActualSourceIdentity()
    {
        var f = new Fixture(); var source = new Placeholder(); var cause = new Placeholder();
        var first = new Relation(source, cause); var second = new Relation(source, cause);
        var receipt = f.A.Receive(new Placeholder(), S(first, second));
        Accounting(f.A, receipt, new IElement[] { first, second }, new IElement[] { first, second }, [], []);
        Assert.True(f.A.TryReadState(out _, out var allocations, out _, out _));
        Assert.Same(first, Assert.IsType<Relation>(E(allocations)[0]).Left);
        Assert.Same(second, Assert.IsType<Relation>(E(allocations)[1]).Left);
    }

    [Fact]
    public void HistoricalReadersRejectForeignFramesAndAccountingThatDuplicatesParticipation()
    {
        var f = new Fixture(); var receipt = f.A.Receive(new Placeholder(), S(f.P)); var other = new Fixture();
        Assert.False(other.A.TryReadReceipt(receipt, out _, out _, out _, out _, out _, out _, out _, out _));
        Assert.False(other.A.TryReadState(f.A.State, out _, out _, out _, out _));
        Assert.False(other.A.TryReadReadiness(f.A.Observe(), out _, out _, out _, out _));
        var body = Assert.IsType<Relation>(receipt.Right); var result = Assert.IsType<Relation>(body.Left);
        var forged = new Relation(receipt.Left, new Relation(new Relation(result.Left, S(f.P, f.Q)), body.Right));
        Assert.False(f.A.TryReadAccounting(forged, out _, out _, out _, out _));
        Assert.True(f.A.TryReadReceipt(new Relation(receipt.Left, receipt.Right), out _, out _, out _, out _, out _, out _, out _, out _));
    }

    [Fact]
    public void AReadinessPublicationCannotReplaceActualMissingSitesWithAReadyClaim()
    {
        var f = new Fixture(); f.A.Receive(new Placeholder(), S(f.P)); var publication = f.A.Observe();
        Assert.True(QualitativePublication.TryRead(publication, out _, out _, out _, out _, out _, out _, out var evidence));
        var forged = new Relation(publication.Left, new Relation(S(f.A.Ready), evidence));
        Assert.False(f.A.TryReadReadiness(forged, out _, out _, out _, out _));
        var coverage = Assert.IsType<Relation>(evidence); var result = Assert.IsType<Relation>(coverage.Right);
        var erased = new Relation(coverage.Left, new Relation(result.Left, new Relation(S(), S())));
        var fakeCoverage = new Relation(publication.Left, new Relation(S(f.A.Ready), erased));
        Assert.False(f.A.TryReadReadiness(fakeCoverage, out _, out _, out _, out _));
    }

    private static IElement Outcome(Relation publication)
    {
        Assert.True(QualitativePublication.TryRead(publication, out _, out _, out _, out _, out _, out var support, out _));
        Assert.True(Closed(support)); return Assert.Single(E(support));
    }
    private static void Accounting(QualitativeImpulseAccumulator a, Relation receipt,
        IElement[] incoming, IElement[] accepted, IElement[] blocked, IElement[] remainder)
    {
        Assert.True(a.TryReadAccounting(receipt, out var offered, out var yes, out var no, out var rest));
        Assert.Equal(incoming, E(offered)); Assert.Equal(accepted, E(yes)); Assert.Equal(blocked, E(no)); Assert.Equal(remainder, E(rest));
    }
    private static bool Contains(IElement element, IElement actual) => ReferenceEquals(element, actual) ||
        element is Relation row && (Contains(row.Left, actual) || Contains(row.Right, actual));
    private static Relation S(params IElement[] values) => StructuralConstraint.Closed(values);
    private static IElement[] E(Relation scope) { Assert.True(StructuralConstraint.TryReadScope(scope, out var values, out _, out _)); return values; }
    private static bool Closed(Relation scope) => StructuralConstraint.TryReadScope(scope, out _, out var start, out var end) && start.Equals(end);

    private sealed class Fixture
    {
        public IElement Owner { get; } = new Placeholder();
        public IElement ASite { get; } = new Placeholder();
        public IElement B { get; } = new Placeholder();
        public IElement P { get; } = new Placeholder();
        public IElement Q { get; } = new Placeholder();
        public IElement Act { get; } = new Placeholder();
        public IElement Rest { get; } = new Placeholder();
        public IElement Refine { get; } = new Placeholder();
        public QualitativeImpulseAccumulator A { get; }
        public Fixture(bool thresholdBoth = true) => A = new(Owner, S(ASite, B), thresholdBoth ? S(ASite, B) : S(ASite));
        public void Fill() => A.Receive(new Placeholder(), S(P, Q));
        public Relation Question() => A.Ask(S(new Relation(A.Ready, Act), new Relation(A.Waiting, Rest)), Refine);
    }
}
