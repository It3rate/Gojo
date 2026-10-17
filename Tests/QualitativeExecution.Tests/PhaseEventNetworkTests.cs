using Gojo.Core;
using Gojo.Patterns;

namespace Gojo.Experiments.QualitativeExecution.Tests;

public sealed class PhaseEventNetworkTests
{
    [Fact]
    public void NativePhaseDividerDrivesFastAndSlowComponentsWithoutHostScheduleDispatch()
    {
        var beat = new Placeholder(); var fastPhase = new Placeholder(); var slowPhase = new Placeholder();
        var d0 = new Placeholder(); var d1 = new Placeholder(); var d2 = new Placeholder();
        var clock = Cycle(beat); var fast = Cycle(fastPhase); var divider = Cycle(d0, d1, d2); var slow = Cycle(slowPhase);
        var clockPort = PhaseEventNetwork.Port(new Placeholder(), clock);
        var fastPort = PhaseEventNetwork.Port(new Placeholder(), fast);
        var dividerPort = PhaseEventNetwork.Port(new Placeholder(), divider);
        var slowPort = PhaseEventNetwork.Port(new Placeholder(), slow);
        var direct = PhaseEventNetwork.Route(clockPort, beat, fastPort);
        var divide = PhaseEventNetwork.Route(clockPort, beat, dividerPort);
        var emit = PhaseEventNetwork.Route(dividerPort, d0, slowPort);
        var network = new PhaseEventNetwork([clock, fast, divider, slow], S(clockPort, fastPort, dividerPort, slowPort), S(direct, divide, emit));
        var state = network.Initial(new Placeholder(), S(Start(clockPort, beat), Start(fastPort, fastPhase),
            Start(dividerPort, d0), Start(slowPort, slowPhase)));
        var fastEvents = new List<IElement>(); var slowEvents = new List<IElement>();
        foreach (var expectedSlowCount in new[] { 0, 0, 1, 1, 1, 2 })
        {
            var receipt = Pulse(network, state, clockPort);
            Assert.True(network.TryReadStep(receipt, out _, out _, out state, out var accepted, out var blocked, out var pending));
            Assert.Empty(E(blocked)); Assert.Empty(E(pending));
            foreach (var item in E(accepted).Cast<Relation>())
            {
                if (fast.TryReadAccepted(item, out _, out var eventIdentity, out _)) fastEvents.Add(eventIdentity);
                if (slow.TryReadAccepted(item, out _, out eventIdentity, out _)) slowEvents.Add(eventIdentity);
            }
            Assert.Equal(expectedSlowCount, slowEvents.Count);
        }
        Assert.Equal(6, fastEvents.Count); Assert.Equal(2, slowEvents.Count);
        Assert.Equal(8, fastEvents.Concat(slowEvents).Distinct(ReferenceEqualityComparer.Instance).Count());
        Assert.True(network.TryReadState(state, out _, out var components, out var consumed, out var queue));
        Assert.Equal(4, E(components).Length); Assert.Equal(14, E(consumed).Length); Assert.Empty(E(queue));
        Assert.Same(network.Ports, network.Surface.Left); Assert.Same(network.Routes, network.Surface.Right);
        AssertNoNumbers(state);
    }

    [Fact]
    public void IndependentRootPortsAdvanceOnlyTheirExplicitlyAddressedComponent()
    {
        var a = new Placeholder(); var b = new Placeholder();
        var first = Cycle(a); var second = Cycle(b);
        var p = PhaseEventNetwork.Port(new Placeholder(), first); var q = PhaseEventNetwork.Port(new Placeholder(), second);
        var network = new PhaseEventNetwork([first, second], S(p, q), S());
        var initialEvent = new Placeholder();
        var initial = network.Initial(initialEvent, S(Start(p, a), Start(q, b)));
        var state = After(network, Pulse(network, initial, p));
        Assert.True(second.TryReadState(Component(network, state, q), out _, out var unchanged, out _));
        Assert.Same(initialEvent, unchanged);
        state = After(network, Pulse(network, state, q));
        Assert.True(first.TryReadHistory(Component(network, state, p), out var firstHistory));
        Assert.True(second.TryReadHistory(Component(network, state, q), out var secondHistory));
        Assert.Equal(2, E(firstHistory).Length); Assert.Equal(2, E(secondHistory).Length);
        Assert.NotSame(E(firstHistory)[^1], E(secondHistory)[^1]);
    }

    [Fact]
    public void TriggeredReceiptsRetainTheirActualProducerRouteAndFreshReceivingOccurrence()
    {
        var f = new Chain();
        var receipt = Pulse(f.Network, f.Initial, f.APort);
        Assert.True(f.Network.TryReadStep(receipt, out _, out _, out var after, out var accepted, out var blocked, out var pending));
        Assert.Equal(3, E(accepted).Length); Assert.Empty(E(blocked)); Assert.Empty(E(pending));
        var steps = E(accepted).Cast<Relation>().ToArray();
        Assert.True(f.A.TryReadAccepted(steps[0], out var ownerA, out var eventA, out _));
        Assert.True(f.B.TryReadAccepted(steps[1], out var ownerB, out var eventB, out _));
        Assert.True(f.C.TryReadAccepted(steps[2], out var ownerC, out var eventC, out _));
        Assert.Same(f.APort.Left, ownerA); Assert.Same(f.BPort.Left, ownerB); Assert.Same(f.CPort.Left, ownerC);
        Assert.NotSame(eventA, eventB); Assert.NotSame(eventB, eventC); Assert.NotSame(eventA, eventC);
        Assert.True(Contains(steps[1], steps[0])); Assert.True(Contains(steps[1], f.AB));
        Assert.True(Contains(steps[2], steps[1])); Assert.True(Contains(steps[2], f.BC));
        Assert.True(f.Network.TryReadState(after, out var root, out _, out var consumed, out _));
        Assert.Same(eventA, root); Assert.Equal(2, E(consumed).Length);
    }

    [Fact]
    public void TwoRoutesToOneComponentBindSequentialActualPredecessorsAtDeliveryTime()
    {
        var a = new Placeholder(); var b = new Placeholder(); var first = Cycle(a); var second = Cycle(b);
        var p = PhaseEventNetwork.Port(new Placeholder(), first); var q = PhaseEventNetwork.Port(new Placeholder(), second);
        var route1 = PhaseEventNetwork.Route(p, a, q); var route2 = PhaseEventNetwork.Route(p, a, q);
        var network = new PhaseEventNetwork([first, second], S(p, q), S(route1, route2));
        var initialEvent = new Placeholder(); var initial = network.Initial(initialEvent, S(Start(p, a), Start(q, b)));
        var receipt = Pulse(network, initial, p);
        Assert.True(network.TryReadStep(receipt, out _, out _, out _, out var accepted, out var blocked, out var pending));
        Assert.Equal(3, E(accepted).Length); Assert.Empty(E(blocked)); Assert.Empty(E(pending));
        var steps = E(accepted).Cast<Relation>().ToArray();
        Assert.True(second.TryReadAccepted(steps[1], out _, out var firstEvent, out _));
        Assert.True(second.TryReadStep(steps[2], out _, out var secondDrive, out _, out _, out _, out _));
        Assert.True(second.TryReadDrive(secondDrive, out _, out var secondEvent, out var actualPredecessor, out _));
        Assert.Same(firstEvent, actualPredecessor); Assert.NotSame(firstEvent, secondEvent);
        Assert.True(Contains(steps[1], route1)); Assert.True(Contains(steps[2], route2));
    }

    [Fact]
    public void RepeatingTheSameRouteReferenceDoesNotReuseOneProducerTrigger()
    {
        var a = new Placeholder(); var b = new Placeholder(); var first = Cycle(a); var second = Cycle(b);
        var p = PhaseEventNetwork.Port(new Placeholder(), first); var q = PhaseEventNetwork.Port(new Placeholder(), second);
        var route = PhaseEventNetwork.Route(p, a, q);
        var network = new PhaseEventNetwork([first, second], S(p, q), S(route, route));
        var initial = network.Initial(new Placeholder(), S(Start(p, a), Start(q, b)));
        var receipt = Pulse(network, initial, p);
        Assert.True(network.TryReadStep(receipt, out _, out var request, out var after, out var accepted, out var blocked, out var pending));
        Assert.Equal(2, E(accepted).Length); Assert.Empty(E(blocked)); Assert.Empty(E(pending));
        Assert.True(network.TryReadState(after, out _, out _, out var consumed, out _)); Assert.Single(E(consumed));
        AssertBlocked(network, network.Step(after, request), after);
    }

    [Fact]
    public void DeclaredRouteOrderAndFifoDetermineTheInspectableDeliveryOrder()
    {
        var a = new Placeholder(); var b = new Placeholder(); var c = new Placeholder(); var d = new Placeholder();
        var first = Cycle(a); var second = Cycle(b); var third = Cycle(c); var fourth = Cycle(d);
        var p = PhaseEventNetwork.Port(new Placeholder(), first); var q = PhaseEventNetwork.Port(new Placeholder(), second);
        var r = PhaseEventNetwork.Port(new Placeholder(), third); var s = PhaseEventNetwork.Port(new Placeholder(), fourth);
        var network = new PhaseEventNetwork([first, second, third, fourth], S(p, q, r, s),
            S(PhaseEventNetwork.Route(p, a, q), PhaseEventNetwork.Route(p, a, r), PhaseEventNetwork.Route(q, b, s)));
        var initial = network.Initial(new Placeholder(), S(Start(p, a), Start(q, b), Start(r, c), Start(s, d)));
        var receipt = Pulse(network, initial, p);
        Assert.True(network.TryReadStep(receipt, out _, out _, out _, out var accepted, out _, out var pending));
        var steps = E(accepted).Cast<Relation>().ToArray(); Assert.Equal(4, steps.Length); Assert.Empty(E(pending));
        Assert.True(first.TryReadAccepted(steps[0], out _, out _, out _));
        Assert.True(second.TryReadAccepted(steps[1], out _, out _, out _));
        Assert.True(third.TryReadAccepted(steps[2], out _, out _, out _));
        Assert.True(fourth.TryReadAccepted(steps[3], out _, out _, out _));
    }

    [Fact]
    public void StaleReplayedAndForeignRootRequestsPreserveTheNetworkState()
    {
        var f = new Chain(); var first = Pulse(f.Network, f.Initial, f.APort); var after = After(f.Network, first);
        Assert.True(f.Network.TryReadStep(first, out _, out var request, out _, out _, out _, out _));
        AssertBlocked(f.Network, f.Network.Step(after, request), after);
        Assert.True(f.Network.TryReadState(after, out var current, out _, out _, out _));
        AssertBlocked(f.Network, f.Network.Step(after, f.Network.Pulse(f.APort, new Placeholder(), new Placeholder())), after);
        var copiedPort = new Relation(f.APort.Left, f.APort.Right);
        AssertBlocked(f.Network, f.Network.Step(after, f.Network.Pulse(copiedPort, new Placeholder(), current)), after);
        AssertBlocked(f.Network, f.Network.Step(after, f.Network.Pulse(f.APort, current, current)), after);
    }

    [Fact]
    public void OpenMalformedAndForeignRoutesDoNotBecomeAnEmptyTriggerSet()
    {
        var a = new Placeholder(); var phase = Cycle(a); var port = PhaseEventNetwork.Port(new Placeholder(), phase);
        foreach (var routes in new[]
        {
            StructuralConstraint.Open(), S(new Placeholder()), S(PhaseEventNetwork.Route(port, new Placeholder(), port)),
            S(PhaseEventNetwork.Route(port, a, new Relation(port.Left, port.Right)))
        })
        {
            var network = new PhaseEventNetwork([phase], S(port), routes);
            var initial = network.Initial(new Placeholder(), S(Start(port, a)));
            var receipt = network.Step(initial, network.Pulse(port, new Placeholder(), new Placeholder()));
            Assert.True(network.TryReadStep(receipt, out _, out _, out var after, out var accepted, out var blocked, out var pending));
            Assert.Same(initial, after); Assert.Empty(E(accepted)); Assert.Empty(E(blocked)); Assert.NotEmpty(E(pending));
            Assert.True(Contains(receipt, routes));
        }
    }

    [Fact]
    public void OpenOrUnrepresentedPortsAndInvalidStartRowsCannotCreateDefaultComponents()
    {
        var a = new Placeholder(); var phase = Cycle(a); var port = PhaseEventNetwork.Port(new Placeholder(), phase);
        var invalid = new[]
        {
            new PhaseEventNetwork([phase], StructuralConstraint.Open(port), S()),
            new PhaseEventNetwork([], S(port), S())
        };
        foreach (var network in invalid)
            Assert.False(network.TryReadState(network.Initial(new Placeholder(), S(Start(port, a))), out _, out _, out _, out _));
        var valid = new PhaseEventNetwork([phase], S(port), S());
        foreach (var starts in new[] { S(), S(Start(port, a), Start(port, a)), StructuralConstraint.Open(Start(port, a)) })
        {
            var state = valid.Initial(new Placeholder(), starts);
            Assert.False(valid.TryReadState(state, out _, out _, out _, out _));
            Assert.True(Contains(state, starts));
        }
    }

    [Fact]
    public void UnknownPhaseAndMissingSuccessorKeepConcreteDeliveryAndFailureEvidencePending()
    {
        var a = new Placeholder();
        foreach (var (phase, support) in new[]
        {
            (Cycle(a), StructuralConstraint.Open(a)),
            (new PhaseRecurrence(S(a), S()), S(a))
        })
        {
            var port = PhaseEventNetwork.Port(new Placeholder(), phase);
            var network = new PhaseEventNetwork([phase], S(port), S());
            var initial = network.Initial(new Placeholder(), S(new Relation(port, support)));
            var receipt = Pulse(network, initial, port);
            Assert.True(network.TryReadStep(receipt, out _, out _, out var after, out var accepted, out var blocked, out var pending));
            Assert.Empty(E(accepted)); Assert.Empty(E(blocked)); Assert.Single(E(pending));
            Assert.Same(Component(network, initial, port), Component(network, after, port));
            var delivery = E(pending)[0]; Assert.True(Contains(after, delivery)); Assert.True(Contains(after, support));
            var resumed = network.Resume(after, budget: 1);
            Assert.True(network.TryReadStep(resumed, out _, out _, out _, out _, out _, out var stillPending));
            Assert.Same(delivery, Assert.Single(E(stillPending)));
        }
    }

    [Fact]
    public void AComponentReplayIsRecordedAsBlockedWithoutGeneratingASecondRoute()
    {
        var f = new Chain(); var first = Pulse(f.Network, f.Initial, f.APort); var after = After(f.Network, first);
        var bState = Component(f.Network, after, f.BPort);
        Assert.True(f.B.TryReadState(bState, out _, out var alreadyUsed, out _));
        Assert.True(f.Network.TryReadState(after, out var root, out _, out var consumedBefore, out _));
        var receipt = f.Network.Step(after, f.Network.Pulse(f.BPort, alreadyUsed, root));
        Assert.True(f.Network.TryReadStep(receipt, out _, out _, out var blockedAfter, out var accepted, out var blocked, out var pending));
        Assert.Empty(E(accepted)); Assert.Single(E(blocked)); Assert.Empty(E(pending));
        Assert.Same(bState, Component(f.Network, blockedAfter, f.BPort));
        Assert.True(f.Network.TryReadState(blockedAfter, out _, out _, out var consumedAfter, out _));
        Assert.Equal(E(consumedBefore), E(consumedAfter));
    }

    [Fact]
    public void BudgetedResumeRetainsTheSamePendingOccurrencesAndRefusesAnotherRootUntilQuiet()
    {
        var f = new Chain(); var initialRequest = Pulse(f.Network, f.Initial, f.APort, budget: 0);
        Assert.True(f.Network.TryReadStep(initialRequest, out _, out _, out var state, out var accepted, out _, out var pending));
        Assert.Empty(E(accepted)); Assert.Single(E(pending));
        var allEvents = new List<IElement>();
        foreach (var expected in new[] { f.A, f.B, f.C })
        {
            Assert.True(f.Network.TryReadState(state, out var root, out _, out _, out var queue));
            var head = Assert.IsType<Relation>(Assert.Single(E(queue)));
            var receivingEvent = Assert.IsType<Relation>(head.Left).Right;
            AssertBlocked(f.Network, f.Network.Step(state, f.Network.Pulse(f.APort, new Placeholder(), root)), state);
            var receipt = f.Network.Resume(state, budget: 1);
            Assert.True(f.Network.TryReadStep(receipt, out _, out _, out state, out accepted, out var blocked, out pending));
            Assert.Empty(E(blocked));
            var phaseReceipt = Assert.IsType<Relation>(Assert.Single(E(accepted)));
            Assert.True(expected.TryReadAccepted(phaseReceipt, out _, out var actual, out _));
            Assert.Same(receivingEvent, actual); allEvents.Add(actual);
        }
        Assert.Empty(E(pending)); Assert.Equal(3, allEvents.Distinct(ReferenceEqualityComparer.Instance).Count());
        Assert.True(f.Network.TryReadState(state, out _, out _, out var used, out _)); Assert.Equal(2, E(used).Length);
    }

    [Fact]
    public void FeedbackStopsAtTheBudgetAndCanResumeWithoutReplayingItsCausalPrefix()
    {
        var a = new Placeholder(); var b = new Placeholder(); var first = Cycle(a); var second = Cycle(b);
        var p = PhaseEventNetwork.Port(new Placeholder(), first); var q = PhaseEventNetwork.Port(new Placeholder(), second);
        var pq = PhaseEventNetwork.Route(p, a, q); var qp = PhaseEventNetwork.Route(q, b, p);
        var network = new PhaseEventNetwork([first, second], S(p, q), S(pq, qp));
        var initial = network.Initial(new Placeholder(), S(Start(p, a), Start(q, b)));
        var receipt = Pulse(network, initial, p, budget: 5);
        Assert.True(network.TryReadStep(receipt, out _, out _, out var state, out var accepted, out var blocked, out var pending));
        Assert.Equal(5, E(accepted).Length); Assert.Empty(E(blocked)); Assert.Single(E(pending));
        var oldQueueHead = E(pending)[0];
        var resumed = network.Resume(state, budget: 4);
        Assert.True(network.TryReadStep(resumed, out _, out _, out var after, out var continued, out blocked, out pending));
        Assert.Equal(4, E(continued).Length); Assert.Empty(E(blocked)); Assert.Single(E(pending));
        var actualEvents = new List<IElement>();
        foreach (var phaseReceipt in E(accepted).Concat(E(continued)).Cast<Relation>())
        {
            if (!first.TryReadAccepted(phaseReceipt, out _, out var occurrence, out _))
                Assert.True(second.TryReadAccepted(phaseReceipt, out _, out occurrence, out _));
            actualEvents.Add(occurrence);
        }
        Assert.Equal(9, actualEvents.Distinct(ReferenceEqualityComparer.Instance).Count());
        Assert.True(Contains(E(continued)[0], oldQueueHead));
        Assert.True(network.TryReadState(after, out _, out _, out var consumed, out _)); Assert.Equal(9, E(consumed).Length);
        AssertNoNumbers(after);
    }

    [Fact]
    public void ReadingAndResumingAnEmptyQueueCannotCreateAnAutonomousClock()
    {
        var f = new Chain(); var state = f.Initial;
        Assert.True(f.Network.TryReadState(state, out var eventIdentity, out var components, out var consumed, out var queue));
        Assert.Empty(E(queue)); Assert.Empty(E(consumed));
        var receipt = f.Network.Resume(state);
        Assert.True(f.Network.TryReadStep(receipt, out _, out _, out var after, out var accepted, out var blocked, out var pending));
        Assert.Empty(E(accepted)); Assert.Empty(E(blocked)); Assert.Empty(E(pending));
        Assert.True(f.Network.TryReadState(after, out var actual, out var unchanged, out _, out _));
        Assert.Same(eventIdentity, actual); Assert.Equal(E(components), E(unchanged));
    }

    [Fact]
    public void CopiedQueuedNotificationsCannotDuplicateOneProducerOccurrenceRoutePair()
    {
        var f = new Chain();
        var partial = Pulse(f.Network, f.Initial, f.APort, budget: 1);
        Assert.True(f.Network.TryReadStep(partial, out _, out _, out var state, out _, out _, out var pending));
        var original = Assert.IsType<Relation>(Assert.Single(E(pending)));
        var addressed = Assert.IsType<Relation>(original.Left);
        var copied = new Relation(new Relation(addressed.Left, new Placeholder()), original.Right);
        var fake = ReplaceQueue(state, S(original, copied));
        Assert.False(f.Network.TryReadState(fake, out _, out _, out _, out _));
        var receipt = f.Network.Resume(fake, budget: 2);
        Assert.True(f.Network.TryReadStep(receipt, out _, out _, out var after, out var accepted, out var blocked, out var unresolved));
        Assert.Same(fake, after); Assert.Empty(E(accepted)); Assert.Empty(E(blocked)); Assert.NotEmpty(E(unresolved));
        var repeatedIdentity = ReplaceQueue(state, S(original, original));
        Assert.False(f.Network.TryReadState(repeatedIdentity, out _, out _, out _, out _));
    }

    [Fact]
    public void DifferentRoutesCannotClaimTheSameQueuedReceivingOccurrence()
    {
        var a = new Placeholder(); var b = new Placeholder(); var c = new Placeholder();
        var first = Cycle(a); var second = Cycle(b); var third = Cycle(c);
        var p = PhaseEventNetwork.Port(new Placeholder(), first); var q = PhaseEventNetwork.Port(new Placeholder(), second);
        var r = PhaseEventNetwork.Port(new Placeholder(), third);
        var network = new PhaseEventNetwork([first, second, third], S(p, q, r),
            S(PhaseEventNetwork.Route(p, a, q), PhaseEventNetwork.Route(p, a, r)));
        var initial = network.Initial(new Placeholder(), S(Start(p, a), Start(q, b), Start(r, c)));
        var partial = Pulse(network, initial, p, budget: 1);
        Assert.True(network.TryReadStep(partial, out _, out _, out var state, out _, out _, out var pending));
        var queued = E(pending).Cast<Relation>().ToArray(); Assert.Equal(2, queued.Length);
        var firstAddress = Assert.IsType<Relation>(queued[0].Left);
        var secondAddress = Assert.IsType<Relation>(queued[1].Left);
        var copiedEvent = new Relation(new Relation(secondAddress.Left, firstAddress.Right), queued[1].Right);
        Assert.False(network.TryReadState(ReplaceQueue(state, S(queued[0], copiedEvent)), out _, out _, out _, out _));
    }

    private sealed class Chain
    {
        public PhaseRecurrence A { get; } public PhaseRecurrence B { get; } public PhaseRecurrence C { get; }
        public Relation APort { get; } public Relation BPort { get; } public Relation CPort { get; }
        public Relation AB { get; } public Relation BC { get; }
        public PhaseEventNetwork Network { get; } public Relation Initial { get; }

        public Chain()
        {
            var a = new Placeholder(); var b = new Placeholder(); var c = new Placeholder();
            A = Cycle(a); B = Cycle(b); C = Cycle(c);
            APort = PhaseEventNetwork.Port(new Placeholder(), A); BPort = PhaseEventNetwork.Port(new Placeholder(), B);
            CPort = PhaseEventNetwork.Port(new Placeholder(), C);
            AB = PhaseEventNetwork.Route(APort, a, BPort); BC = PhaseEventNetwork.Route(BPort, b, CPort);
            Network = new PhaseEventNetwork([A, B, C], S(APort, BPort, CPort), S(AB, BC));
            Initial = Network.Initial(new Placeholder(), S(Start(APort, a), Start(BPort, b), Start(CPort, c)));
        }
    }

    private static PhaseRecurrence Cycle(params IElement[] phases) =>
        new(S(phases), S(phases.Zip(phases.Skip(1).Append(phases[0]), (a, b) => (IElement)new Relation(a, b)).ToArray()));

    private static Relation Start(Relation port, IElement phase) => new(port, S(phase));

    private static Relation Pulse(PhaseEventNetwork network, Relation state, Relation port, int budget = 64)
    {
        Assert.True(network.TryReadState(state, out var root, out _, out _, out _));
        return network.Step(state, network.Pulse(port, new Placeholder(), root), budget);
    }

    private static Relation After(PhaseEventNetwork network, Relation receipt)
    {
        Assert.True(network.TryReadStep(receipt, out _, out _, out var after, out _, out var blocked, out var pending));
        Assert.Empty(E(blocked)); Assert.Empty(E(pending)); return after;
    }

    private static Relation Component(PhaseEventNetwork network, Relation state, Relation port)
    {
        Assert.True(network.TryReadState(state, out _, out var rows, out _, out _));
        return Assert.IsType<Relation>(Assert.Single(E(rows).Cast<Relation>(), row => ReferenceEquals(row.Left, port)).Right);
    }

    private static Relation ReplaceQueue(Relation state, Relation queue)
    {
        var body = (Relation)state.Right;
        var execution = (Relation)body.Right;
        var run = (Relation)execution.Left;
        return new Relation(state.Left, new Relation(body.Left,
            new Relation(new Relation(run.Left, queue), execution.Right)));
    }

    private static void AssertBlocked(PhaseEventNetwork network, Relation receipt, Relation state)
    {
        Assert.True(network.TryReadStep(receipt, out _, out _, out var after, out var accepted, out var blocked, out _));
        Assert.Same(state, after); Assert.Empty(E(accepted)); Assert.NotEmpty(E(blocked));
    }

    private static Relation S(params IElement[] entries) => StructuralConstraint.Closed(entries);
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);

    private static bool Contains(IElement root, IElement target) =>
        Traverse(root).Any(element => ReferenceEquals(element, target));

    private static void AssertNoNumbers(IElement root)
    {
        foreach (var element in Traverse(root))
        {
            Assert.IsNotType<Carrier>(element); Assert.IsNotType<CarrierLocation>(element);
        }
    }

    private static IEnumerable<IElement> Traverse(IElement root)
    {
        var seen = new HashSet<IElement>(ReferenceEqualityComparer.Instance); var queue = new Stack<IElement>(); queue.Push(root);
        while (queue.TryPop(out var element))
        {
            if (!seen.Add(element)) continue; yield return element;
            if (element is Relation relation) { queue.Push(relation.Left); queue.Push(relation.Right); }
        }
    }
}
