using Gojo.Core;
using Gojo.Patterns;

namespace Gojo.Experiments.QualitativeExecution.Tests;

public sealed class EventScheduleConnectionTests
{
    [Fact]
    public void PortsSchedulesWindowsAndCausalHistoryRemainNativeWithoutNumbers()
    {
        var f = new ScheduleFixture();
        var initial = f.Connection.Initial(f.R0);
        var receipt = f.Connection.Deliver(initial, f.Publication, f.P0, f.R1);
        Assert.True(f.Connection.TryReadAccepted(receipt, out var publication, out var producer, out var receiver));
        Assert.Same(f.Publication, publication);
        Assert.Same(f.P0, producer);
        Assert.Same(f.R1, receiver);
        Assert.True(Contains(receipt, f.Connection.Producer));
        Assert.True(Contains(receipt, f.Connection.Receiver));
        Assert.True(Contains(receipt, f.Connection.ProducerSchedule));
        Assert.True(Contains(receipt, f.Connection.ReceiverSchedule));
        Assert.True(Contains(receipt, f.FirstWindow));
        Assert.True(Contains(receipt, initial));
        AssertNoNumbers(receipt);
    }

    [Fact]
    public void OneObservationCanBeHeldAcrossDeclaredReceiverEventsWithoutDrainingItsSource()
    {
        var f = new ScheduleFixture();
        var initial = f.Connection.Initial(f.R0);
        var first = f.Connection.Deliver(initial, f.Publication, f.P0, f.R1);
        var second = f.Connection.Deliver(After(f.Connection, first), f.Publication, f.P0, f.R2);
        Assert.True(f.Connection.TryReadAccepted(second, out var held, out _, out _));
        Assert.Same(f.Publication, held);
        var secondState = After(f.Connection, second);
        Assert.True(f.Connection.TryReadState(secondState, out var cursor, out var history, out var currentProducer));
        Assert.Same(f.R2, cursor);
        Assert.Equal(new[] { f.R0, f.R1, f.R2 }, FiniteLaw.Entries(history));
        Assert.Same(f.P0, Assert.Single(FiniteLaw.Entries(currentProducer)));
        // Re-reading from the retained original history is a lawful historical branch.
        var historical = f.Connection.Deliver(initial, f.Publication, f.P0, f.R1);
        Assert.True(f.Connection.TryReadAccepted(historical, out var original, out _, out _));
        Assert.Same(f.Publication, original);
    }

    [Fact]
    public void ExpiredHeldObservationIsBlockedAndFreshReplacementCanServeTheSameEvent()
    {
        var f = new ScheduleFixture();
        var first = f.Connection.Deliver(f.Connection.Initial(f.R0), f.Publication, f.P0, f.R1);
        var second = f.Connection.Deliver(After(f.Connection, first), f.Publication, f.P0, f.R2);
        var state = After(f.Connection, second);
        var expired = f.Connection.Deliver(state, f.Publication, f.P0, f.R3);
        AssertRefused(f.Connection, expired, state, blocked: true);
        var replacement = new Placeholder();
        var fresh = f.Connection.Deliver(state, replacement, f.P1, f.R3);
        Assert.True(f.Connection.TryReadAccepted(fresh, out var supplied, out _, out _));
        Assert.Same(replacement, supplied);
        Assert.True(Contains(fresh, f.ProducerEdge));
    }

    [Fact]
    public void OldProducerCannotRegainAuthorityAfterNewerEvenWhenItsWindowOverlaps()
    {
        var f = new ScheduleFixture(overlapping: true);
        var first = f.Connection.Deliver(f.Connection.Initial(f.R0), f.Publication, f.P0, f.R1);
        var newer = f.Connection.Deliver(After(f.Connection, first), new Placeholder(), f.P1, f.R2);
        Assert.True(f.Connection.TryReadAccepted(newer, out _, out _, out _));
        var state = After(f.Connection, newer);
        var regression = f.Connection.Deliver(state, f.Publication, f.P0, f.R3);
        AssertRefused(f.Connection, regression, state, blocked: true);
        Assert.True(Contains(regression, f.ProducerEdge));
    }

    [Fact]
    public void ReceiverReplayIsBlockedButAnIndependentObserverHasItsOwnHistory()
    {
        var f = new ScheduleFixture();
        var first = f.Connection.Deliver(f.Connection.Initial(f.R0), f.Publication, f.P0, f.R1);
        var state = After(f.Connection, first);
        AssertRefused(f.Connection, f.Connection.Deliver(state, f.Publication, f.P0, f.R1), state, blocked: true);
        var other = new EventScheduleConnection(f.Connection.Producer, new Placeholder(),
            f.Connection.ProducerSchedule, f.Connection.ReceiverSchedule, f.Connection.Windows);
        var independent = other.Deliver(other.Initial(f.R0), f.Publication, f.P0, f.R1);
        Assert.True(other.TryReadAccepted(independent, out var shared, out _, out _));
        Assert.Same(f.Publication, shared);
        Assert.False(f.Connection.TryReadAccepted(independent, out _, out _, out _));
        Assert.False(other.TryReadAccepted(first, out _, out _, out _));
    }

    [Fact]
    public void MissingReceiverPredecessorLinkAndForeignProducerStayUnresolved()
    {
        var f = new ScheduleFixture();
        var state = f.Connection.Initial(f.R0);
        AssertRefused(f.Connection, f.Connection.Deliver(state, f.Publication, f.P0, f.R2), state, blocked: false);
        AssertRefused(f.Connection, f.Connection.Deliver(state, f.Publication, new Placeholder(), f.R1), state, blocked: false);
    }

    [Fact]
    public void AReadableLedgerWithAnUnwitnessedPastReceiverJumpCannotAuthorizeANewDelivery()
    {
        var f = new ScheduleFixture(overlapping: true);
        // The retained cursor and unique events are readable, but R0 -> R2 is
        // absent from the authored causal schedule. A valid next edge cannot
        // retrospectively justify that missing transition.
        var state = new Relation(new Relation(f.Connection.Surface, f.R2),
            new Relation(new Relation(StructuralConstraint.Closed(f.R0, f.R2),
                StructuralConstraint.Closed(f.P0)), StructuralConstraint.Closed()));
        Assert.True(f.Connection.TryReadState(state, out _, out _, out _));
        var receipt = f.Connection.Deliver(state, f.Publication, f.P0, f.R3);
        AssertRefused(f.Connection, receipt, state, blocked: false);
        Assert.True(Contains(receipt, state));
    }

    [Fact]
    public void UnorderedProducerEventsDoNotAcquireAnOrderFromTheirWindowOrValue()
    {
        var f = new ScheduleFixture();
        var q0 = new Placeholder();
        var q1 = new Placeholder();
        var disconnected = new EventScheduleConnection(f.Connection.Producer, f.Connection.Receiver,
            StructuralConstraint.Closed(f.ProducerEdge, new Relation(q0, q1)),
            f.Connection.ReceiverSchedule,
            StructuralConstraint.Closed(f.FirstWindow, EventScheduleConnection.Window(q0, f.R2)));
        var first = disconnected.Deliver(disconnected.Initial(f.R0), f.Publication, f.P0, f.R1);
        var state = After(disconnected, first);
        var unrelated = disconnected.Deliver(state, new Placeholder(), q0, f.R2);
        AssertRefused(disconnected, unrelated, state, blocked: false);
        Assert.True(Contains(unrelated, f.Publication));
    }

    [Fact]
    public void SkippedProducerSamplesRetainTheActualForwardCausalPath()
    {
        var f = new ScheduleFixture();
        var p2 = new Placeholder();
        var secondEdge = new Relation(f.P1, p2);
        var c = new EventScheduleConnection(f.Connection.Producer, f.Connection.Receiver,
            StructuralConstraint.Closed(f.ProducerEdge, secondEdge), f.Connection.ReceiverSchedule,
            StructuralConstraint.Closed(f.FirstWindow, EventScheduleConnection.Window(p2, f.R2)));
        var first = c.Deliver(c.Initial(f.R0), f.Publication, f.P0, f.R1);
        var newer = c.Deliver(After(c, first), new Placeholder(), p2, f.R2);
        Assert.True(c.TryReadAccepted(newer, out _, out _, out _));
        Assert.True(Contains(newer, f.ProducerEdge));
        Assert.True(Contains(newer, secondEdge));
        // The receipt carries a certificate in traversal order, not only graph membership.
        var after = After(c, newer);
        var provenance = ((Relation)after.Right).Right;
        Assert.True(ContainsOrderedScope(provenance, f.ProducerEdge, secondEdge));
    }

    [Fact]
    public void OpenOrCyclicSchedulesCannotSilentlyCertifyFreshness()
    {
        var f = new ScheduleFixture();
        var schedules = new[]
        {
            StructuralConstraint.Open(f.ProducerEdge),
            StructuralConstraint.Closed(f.ProducerEdge, new Relation(f.P1, f.P0))
        };
        foreach (var schedule in schedules)
        {
            var c = new EventScheduleConnection(f.Connection.Producer, f.Connection.Receiver,
                schedule, f.Connection.ReceiverSchedule, f.Connection.Windows);
            var state = c.Initial(f.R0);
            AssertRefused(c, c.Deliver(state, f.Publication, f.P0, f.R1), state, blocked: false);
        }
    }

    [Fact]
    public void OpenValidityWindowPreservesUnknownAuthorizationInsteadOfInventingHold()
    {
        var f = new ScheduleFixture();
        var c = new EventScheduleConnection(f.Connection.Producer, f.Connection.Receiver,
            f.Connection.ProducerSchedule, f.Connection.ReceiverSchedule,
            StructuralConstraint.Closed(new Relation(f.P0, StructuralConstraint.Open(f.R1))));
        var state = c.Initial(f.R0);
        AssertRefused(c, c.Deliver(state, f.Publication, f.P0, f.R1), state, blocked: false);
    }

    private static Relation After(EventScheduleConnection connection, Relation receipt)
    {
        Assert.True(connection.TryReadDelivery(receipt, out _, out var after, out _, out _, out _));
        return after;
    }

    private static void AssertRefused(EventScheduleConnection connection, Relation receipt, Relation state, bool blocked)
    {
        Assert.False(connection.TryReadAccepted(receipt, out _, out _, out _));
        Assert.True(connection.TryReadDelivery(receipt, out var before, out var after,
            out var accepted, out var refused, out var pending));
        Assert.Same(state, before);
        Assert.Same(state, after);
        Assert.Empty(FiniteLaw.Entries(accepted));
        Assert.Equal(blocked ? 1 : 0, FiniteLaw.Entries(refused).Length);
        Assert.Equal(blocked ? 0 : 1, FiniteLaw.Entries(pending).Length);
    }

    private static bool Contains(IElement root, IElement sought) =>
        ReferenceEquals(root, sought) || root is Relation r && (Contains(r.Left, sought) || Contains(r.Right, sought));

    private static bool ContainsOrderedScope(IElement root, params IElement[] sought)
    {
        if (root is not Relation relation) return false;
        return StructuralConstraint.TryReadScope(relation, out var entries, out var seam, out var tail) &&
            seam.Equals(tail) && entries.SequenceEqual(sought) ||
            ContainsOrderedScope(relation.Left, sought) || ContainsOrderedScope(relation.Right, sought);
    }

    private static void AssertNoNumbers(IElement root)
    {
        Assert.IsNotType<Carrier>(root);
        Assert.IsNotType<CarrierLocation>(root);
        if (root is Relation relation) { AssertNoNumbers(relation.Left); AssertNoNumbers(relation.Right); }
    }

    private sealed class ScheduleFixture
    {
        public IElement P0 { get; } = new Placeholder();
        public IElement P1 { get; } = new Placeholder();
        public IElement R0 { get; } = new Placeholder();
        public IElement R1 { get; } = new Placeholder();
        public IElement R2 { get; } = new Placeholder();
        public IElement R3 { get; } = new Placeholder();
        public IElement Publication { get; } = new Placeholder();
        public Relation ProducerEdge { get; }
        public Relation FirstWindow { get; }
        public EventScheduleConnection Connection { get; }

        public ScheduleFixture(bool overlapping = false)
        {
            ProducerEdge = new Relation(P0, P1);
            FirstWindow = overlapping ? EventScheduleConnection.Window(P0, R1, R2, R3) :
                EventScheduleConnection.Window(P0, R1, R2);
            Connection = new EventScheduleConnection(new Placeholder(), new Placeholder(),
                StructuralConstraint.Closed(ProducerEdge),
                StructuralConstraint.Closed(new Relation(R0, R1), new Relation(R1, R2), new Relation(R2, R3)),
                StructuralConstraint.Closed(FirstWindow, EventScheduleConnection.Window(P1, R2, R3)));
        }
    }
}
