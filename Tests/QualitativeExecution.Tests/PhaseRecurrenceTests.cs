using Gojo.Core;
using Gojo.Patterns;

namespace Gojo.Experiments.QualitativeExecution.Tests;

public sealed class PhaseRecurrenceTests
{
    [Fact]
    public void ReusableCyclicTemplateAndActualOccurrenceHistoryAreDifferentConnectedStructures()
    {
        var (f, a, b, c) = Cycle();
        Assert.Same(f.Receiver, Assert.IsType<Relation>(f.Surface.Left).Left);
        Assert.Same(f.Domain, Assert.IsType<Relation>(f.Surface.Left).Right);
        Assert.Same(f.Transitions, f.Surface.Right);
        Assert.Equal(3, Entries(f.Transitions).Length);
        AssertNoNumbers(f.Surface);
        var source = new Placeholder();
        var initialEvent = new Placeholder();
        var initial = f.Initial(source, initialEvent, StructuralConstraint.Closed(a));
        var state = initial;
        IElement[] expected = [b, c, a, b, c, a];
        foreach (var phase in expected)
        {
            state = Advance(f, state);
            AssertPhase(f, state, phase);
            Assert.True(Contains(state, initial));
            AssertNoNumbers(state);
        }
        Assert.True(f.TryReadHistory(state, out var history));
        var trail = Entries(history);
        Assert.Equal(7, trail.Length);
        Assert.Equal(7, trail.Distinct(ReferenceEqualityComparer.Instance).Count());
        Assert.Same(initialEvent, trail[0]);
        AssertPhase(f, initial, a);
    }

    [Fact]
    public void ReadingOrConstructingADriveDoesNotAdvancePhase()
    {
        var (f, a, _, _) = Cycle();
        var source = new Placeholder();
        var occurrence = new Placeholder();
        var state = f.Initial(source, occurrence, StructuralConstraint.Closed(a));
        var drive = f.Drive(source, new Placeholder(), occurrence);
        for (var i = 0; i < 5; i++)
        {
            Assert.True(f.TryReadState(state, out var owner, out var actual, out var phases));
            Assert.Same(source, owner);
            Assert.Same(occurrence, actual);
            Assert.Same(a, Assert.Single(Entries(phases)));
            Assert.True(f.TryReadDrive(drive, out _, out _, out var prior, out _));
            Assert.Same(occurrence, prior);
        }
    }

    [Fact]
    public void OneDriveAdvancesOneDeclaredEdgeAndDoesNotSkipThroughReachability()
    {
        var (f, a, b, c) = Cycle();
        var initial = f.Initial(new Placeholder(), new Placeholder(), StructuralConstraint.Closed(a));
        AssertPhase(f, Advance(f, initial), b);
        AssertPhase(f, Advance(f, Advance(f, initial)), c);
        var directJump = new PhaseRecurrence(f.Domain, StructuralConstraint.Closed(new Relation(a, c)));
        var jumpInitial = directJump.Initial(new Placeholder(), new Placeholder(), StructuralConstraint.Closed(a));
        AssertPhase(directJump, Advance(directJump, jumpInitial), c);
        // A jump is a declared immediate edge; no intermediate phase or event
        // is synthesized merely because a path exists in another template.
        Assert.True(directJump.TryReadHistory(Advance(directJump, jumpInitial), out var jumpHistory));
        Assert.Equal(2, Entries(jumpHistory).Length);
    }

    [Fact]
    public void SamePhaseCanRecurWithFreshOccurrencesEvenInASinglePhaseTemplate()
    {
        var phase = new Placeholder();
        var f = new PhaseRecurrence(StructuralConstraint.Closed(phase),
            StructuralConstraint.Closed(new Relation(phase, phase)));
        var initial = f.Initial(new Placeholder(), new Placeholder(), StructuralConstraint.Closed(phase));
        var first = Advance(f, initial);
        var second = Advance(f, first);
        AssertPhase(f, second, phase);
        Assert.True(f.TryReadHistory(second, out var history));
        Assert.Equal(3, Entries(history).Length);
    }

    [Fact]
    public void ReplayedStaleAndReusedHistoricalOccurrencesAreBlockedWithoutASecondAdvance()
    {
        var (f, a, _, _) = Cycle();
        var source = new Placeholder();
        var firstEvent = new Placeholder();
        var secondEvent = new Placeholder();
        var initial = f.Initial(source, firstEvent, StructuralConstraint.Closed(a));
        var drive = f.Drive(source, secondEvent, firstEvent);
        var after = After(f, f.Step(initial, drive));
        AssertBlocked(f, f.Step(after, drive), after);
        AssertBlocked(f, f.Step(after, f.Drive(source, new Placeholder(), firstEvent)), after);
        AssertBlocked(f, f.Step(after, f.Drive(source, firstEvent, secondEvent)), after);
        AssertBlocked(f, f.Step(after, f.Drive(source, secondEvent, secondEvent)), after);
    }

    [Fact]
    public void DifferentOwnersAndDifferentRecurrenceSurfacesCannotSupplyTheSameHistory()
    {
        var (f, a, _, _) = Cycle();
        var copy = new PhaseRecurrence(f.Domain, f.Transitions);
        var source = new Placeholder();
        var occurrence = new Placeholder();
        var initial = f.Initial(source, occurrence, StructuralConstraint.Closed(a));
        AssertBlocked(f, f.Step(initial, f.Drive(new Placeholder(), new Placeholder(), occurrence)), initial);
        var foreign = copy.Drive(source, new Placeholder(), occurrence);
        AssertPending(f, f.Step(initial, foreign), initial);
        Assert.False(f.TryReadDrive(foreign, out _, out _, out _, out _));
        Assert.False(copy.TryReadState(initial, out _, out _, out _));
        Assert.False(copy.TryReadStep(f.Step(initial, f.Drive(source, new Placeholder(), occurrence)),
            out _, out _, out _, out _, out _, out _));
    }

    [Fact]
    public void UnknownEmptyForeignAndCompetingPriorPhasesRemainUnsettled()
    {
        var (f, a, b, _) = Cycle();
        var source = new Placeholder();
        var occurrence = new Placeholder();
        foreach (var support in new[]
        {
            StructuralConstraint.Open(), StructuralConstraint.Open(a), StructuralConstraint.Closed(),
            StructuralConstraint.Closed(new Placeholder()), StructuralConstraint.Closed(a, b),
            StructuralConstraint.Closed(a, new Placeholder())
        })
        {
            var initial = f.Initial(source, occurrence, support);
            var drive = f.Drive(source, new Placeholder(), occurrence);
            var receipt = f.Step(initial, drive);
            AssertPending(f, receipt, initial);
            Assert.True(Contains(receipt, support));
            Assert.True(Contains(receipt, drive));
        }
    }

    [Fact]
    public void MissingCompetingOpenAndForeignSuccessorDescriptionsCannotBecomeDefaultSteps()
    {
        var a = new Placeholder();
        var b = new Placeholder();
        var c = new Placeholder();
        var domain = StructuralConstraint.Closed(a, b, c);
        foreach (var edges in new[]
        {
            StructuralConstraint.Closed(),
            StructuralConstraint.Closed(new Relation(a, b), new Relation(a, c)),
            StructuralConstraint.Open(new Relation(a, b)),
            StructuralConstraint.Closed(new Relation(a, new Placeholder())),
            StructuralConstraint.Closed(new Placeholder())
        })
        {
            var f = new PhaseRecurrence(domain, edges);
            var source = new Placeholder();
            var occurrence = new Placeholder();
            var initial = f.Initial(source, occurrence, StructuralConstraint.Closed(a));
            var receipt = f.Step(initial, f.Drive(source, new Placeholder(), occurrence));
            AssertPending(f, receipt, initial);
            Assert.True(Contains(receipt, edges));
        }
    }

    [Fact]
    public void RepeatedJustificationsForTheSameSuccessorDoNotCreateFalsePhaseAmbiguity()
    {
        var a = new Placeholder();
        var b = new Placeholder();
        var first = new Relation(a, b);
        var second = new Relation(a, b);
        var f = new PhaseRecurrence(StructuralConstraint.Closed(a, b), StructuralConstraint.Closed(first, second));
        var initial = f.Initial(new Placeholder(), new Placeholder(), StructuralConstraint.Closed(a));
        var after = Advance(f, initial);
        AssertPhase(f, after, b);
        Assert.True(Contains(after, first));
        Assert.True(Contains(after, second));
    }

    [Fact]
    public void OpenOrDuplicatePhaseDomainDoesNotSilentlySupplyACompleteTemplate()
    {
        var a = new Placeholder();
        foreach (var domain in new[] { StructuralConstraint.Open(a), StructuralConstraint.Closed(a, a) })
        {
            var f = new PhaseRecurrence(domain, StructuralConstraint.Closed(new Relation(a, a)));
            var initial = f.Initial(new Placeholder(), new Placeholder(), StructuralConstraint.Closed(a));
            Assert.True(f.TryReadState(initial, out var source, out var occurrence, out _));
            AssertPending(f, f.Step(initial, f.Drive(source, new Placeholder(), occurrence)), initial);
        }
    }

    [Fact]
    public void PhaseEligibilityDoesNotExecuteAnyExposedEffectRoute()
    {
        var (f, a, b, c) = Cycle();
        var sample = new Placeholder();
        var act = new Placeholder();
        var decay = new Placeholder();
        var effects = StructuralConstraint.Closed(new Relation(a, sample), new Relation(b, act), new Relation(c, decay));
        var declaredSystem = new Relation(f.Surface, effects);
        var initial = f.Initial(new Placeholder(), new Placeholder(), StructuralConstraint.Closed(a));
        var next = Advance(f, initial);
        AssertPhase(f, next, b);
        Assert.False(Contains(next, effects));
        Assert.True(Contains(declaredSystem, act));
        // A separate exposed connection must use the accepted phase receipt
        // and account for the drive, route, receiver and effect settlement.
    }

    [Fact]
    public void FreshDriveIsAcceptedOnceInTheHistoryAndGuardedReceiptRetainsItsCause()
    {
        var (f, a, b, _) = Cycle();
        var source = new Placeholder();
        var beforeEvent = new Placeholder();
        var nextEvent = new Placeholder();
        var upstreamDrive = new Placeholder();
        var evidence = new Relation(upstreamDrive, nextEvent);
        var initial = f.Initial(source, beforeEvent, StructuralConstraint.Closed(a));
        var drive = f.Drive(source, nextEvent, beforeEvent, evidence);
        var receipt = f.Step(initial, drive);
        Assert.True(f.TryReadStep(receipt, out var before, out var offered, out var after,
            out var accepted, out var blocked, out var unresolved));
        Assert.Same(initial, before);
        Assert.Same(drive, offered);
        Assert.Same(drive, Assert.Single(Entries(accepted)));
        Assert.Empty(Entries(blocked));
        Assert.Empty(Entries(unresolved));
        Assert.True(f.TryReadAccepted(receipt, out var actualSource, out var actualEvent, out var phase));
        Assert.Same(source, actualSource);
        Assert.Same(nextEvent, actualEvent);
        Assert.Same(b, phase);
        Assert.True(Contains(after, initial));
        Assert.True(Contains(after, evidence));
        AssertBlocked(f, f.Step(after, drive), after);
    }

    [Fact]
    public void HistoricalBranchesAreDifferentExplicitHistoriesRatherThanAnInvisibleGlobalHead()
    {
        var (f, a, b, _) = Cycle();
        var initial = f.Initial(new Placeholder(), new Placeholder(), StructuralConstraint.Closed(a));
        var left = Advance(f, initial);
        var right = Advance(f, initial);
        AssertPhase(f, left, b);
        AssertPhase(f, right, b);
        Assert.True(f.TryReadState(left, out _, out var leftEvent, out _));
        Assert.True(f.TryReadState(right, out _, out var rightEvent, out _));
        Assert.NotSame(leftEvent, rightEvent);
        Assert.True(Contains(left, initial));
        Assert.True(Contains(right, initial));
        Assert.False(Contains(left, right));
    }

    [Fact]
    public void IndependentlyDrivenControllersDoNotAcquireAFrequencyRelationFromSharedPhaseNames()
    {
        var (fast, a, b, c) = Cycle();
        var slow = new PhaseRecurrence(fast.Domain, fast.Transitions);
        var source = new Placeholder();
        var fastState = fast.Initial(source, new Placeholder(), StructuralConstraint.Closed(a));
        var slowState = slow.Initial(source, new Placeholder(), StructuralConstraint.Closed(a));
        fastState = Advance(fast, Advance(fast, fastState));
        slowState = Advance(slow, slowState);
        AssertPhase(fast, fastState, c);
        AssertPhase(slow, slowState, b);
        Assert.True(fast.TryReadHistory(fastState, out var fastHistory));
        Assert.True(slow.TryReadHistory(slowState, out var slowHistory));
        Assert.Equal(3, Entries(fastHistory).Length);
        Assert.Equal(2, Entries(slowHistory).Length);
        AssertNoNumbers(new Relation(fastState, slowState));
    }

    [Fact]
    public void UnequalMeasuredIntervalsCanCalibrateOccurrencesWithoutChangingPhaseMechanics()
    {
        var (f, a, b, c) = Cycle();
        var source = new Placeholder();
        var event0 = new Placeholder();
        var event1 = new Placeholder();
        var event2 = new Placeholder();
        var clock = new Carrier();
        var firstCalibration = new Relation(new Relation(event0, event1), Relation.Segment(clock, 10, 12));
        var secondCalibration = new Relation(new Relation(event1, event2), Relation.Segment(clock, 12, 30));
        var initial = f.Initial(source, event0, StructuralConstraint.Closed(a));
        var first = After(f, f.Step(initial, f.Drive(source, event1, event0, firstCalibration)));
        var second = After(f, f.Step(first, f.Drive(source, event2, event1, secondCalibration)));
        AssertPhase(f, first, b);
        AssertPhase(f, second, c);
        Assert.True(Contains(second, firstCalibration));
        Assert.True(Contains(second, secondCalibration));
        Assert.Equal(0UL, clock.Value);
        Assert.Equal(0, clock.Tension);
        // One drive per explicitly calibrated occurrence, despite 2 vs 18 ticks.
    }

    [Fact]
    public void MalformedAndCounterfeitReceiptShapesCannotAuthorizeEffects()
    {
        var (f, a, _, c) = Cycle();
        var source = new Placeholder();
        var event0 = new Placeholder();
        var event1 = new Placeholder();
        var initial = f.Initial(source, event0, StructuralConstraint.Closed(a));
        var drive = f.Drive(source, event1, event0);
        var receipt = f.Step(initial, drive);
        Assert.True(f.TryReadStep(receipt, out _, out _, out _, out var accepted, out var blocked, out var unresolved));
        var forgedAfter = f.Initial(source, event1, StructuralConstraint.Closed(c));
        var forged = new Relation(receipt.Left,
            new Relation(new Relation(forgedAfter, accepted),
                new Relation(blocked, new Relation(unresolved, StructuralConstraint.Closed()))));
        Assert.False(f.TryReadAccepted(forged, out _, out _, out _));
        Assert.False(f.TryReadAccepted(new Relation(source, event1), out _, out _, out _));
        var malformed = new Relation(source, event0);
        Assert.False(f.TryReadState(malformed, out _, out _, out _));
        AssertPending(f, f.Step(malformed, drive), malformed);
    }

    private static (PhaseRecurrence Fixture, IElement A, IElement B, IElement C) Cycle()
    {
        IElement a = new Placeholder(), b = new Placeholder(), c = new Placeholder();
        return (new PhaseRecurrence(StructuralConstraint.Closed(a, b, c),
            StructuralConstraint.Closed(new Relation(a, b), new Relation(b, c), new Relation(c, a))), a, b, c);
    }

    private static Relation Advance(PhaseRecurrence f, Relation state)
    {
        Assert.True(f.TryReadState(state, out var source, out var occurrence, out _));
        return After(f, f.Step(state, f.Drive(source, new Placeholder(), occurrence)));
    }

    private static Relation After(PhaseRecurrence f, Relation receipt)
    {
        Assert.True(f.TryReadStep(receipt, out _, out _, out var after, out var accepted, out var blocked, out var unresolved));
        Assert.Single(Entries(accepted));
        Assert.Empty(Entries(blocked));
        Assert.Empty(Entries(unresolved));
        return after;
    }

    private static void AssertPhase(PhaseRecurrence f, Relation state, IElement expected)
    {
        Assert.True(f.TryReadState(state, out _, out _, out var phases));
        Assert.Same(expected, Assert.Single(Entries(phases)));
    }

    private static void AssertBlocked(PhaseRecurrence f, Relation receipt, Relation state)
    {
        Assert.True(f.TryReadStep(receipt, out _, out var drive, out var after,
            out var accepted, out var blocked, out var unresolved));
        Assert.Same(state, after);
        Assert.Empty(Entries(accepted));
        Assert.Same(drive, Assert.Single(Entries(blocked)));
        Assert.Empty(Entries(unresolved));
        Assert.False(f.TryReadAccepted(receipt, out _, out _, out _));
    }

    private static void AssertPending(PhaseRecurrence f, Relation receipt, Relation state)
    {
        Assert.True(f.TryReadStep(receipt, out _, out var drive, out var after,
            out var accepted, out var blocked, out var unresolved));
        Assert.Same(state, after);
        Assert.Empty(Entries(accepted));
        Assert.Empty(Entries(blocked));
        Assert.NotEmpty(Entries(unresolved));
        Assert.True(Contains(unresolved, drive));
        Assert.False(f.TryReadAccepted(receipt, out _, out _, out _));
    }

    private static IElement[] Entries(Relation scope) => FiniteLaw.Entries(scope);

    private static bool Contains(IElement root, IElement target) =>
        Traverse(root).Any(element => ReferenceEquals(element, target));

    private static void AssertNoNumbers(IElement root)
    {
        foreach (var element in Traverse(root))
        {
            Assert.IsNotType<Carrier>(element);
            Assert.IsNotType<CarrierLocation>(element);
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
