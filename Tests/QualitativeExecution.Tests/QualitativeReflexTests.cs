using Gojo.Core;
using Gojo.Patterns;

namespace Gojo.Experiments.QualitativeExecution.Tests;

public sealed class QualitativeReflexTests
{
    [Fact]
    public void NineAuthoredReflexCasesReadOnlyCategoriesAndKeepTheirPortsConnected()
    {
        var f = new QualitativeReflexFixture();
        var states = new[] { f.Stock.Empty, f.Stock.Available, f.Stock.Full };
        var lights = new[] { f.LightFrame.Lower, f.LightFrame.Equal, f.LightFrame.Higher };
        Relation[,] expected =
        {
            { f.MoveToB, f.Rest, f.MoveToA },
            { f.MoveToB, f.Rest, f.MoveToA },
            { f.MoveToA, f.Rest, f.MoveToB }
        };
        for (var mode = 0; mode < states.Length; mode++)
            for (var light = 0; light < lights.Length; light++)
            {
                var stateEvent = new Placeholder();
                var state = f.Stock.Initial(f.Agent, stateEvent, StructuralConstraint.Closed(states[mode]));
                var observation = f.See(new Placeholder(), stateEvent, StructuralConstraint.Closed(lights[light]));
                foreach (var read in new[] { f.Read(state, observation), f.Read(state, observation, compiled: true) })
                {
                    Assert.True(f.TryReadCommand(read, out var command));
                    Assert.Same(expected[mode, light], command);
                    Assert.True(Contains(read, state)); Assert.True(Contains(read, observation));
                    Assert.True(Contains(read, f.SensorPort)); Assert.True(Contains(read, f.MotorPort));
                    AssertNoNumbers(read);
                }
            }
    }

    [Fact]
    public void ModeUncertaintyAndMissingLightCannotSilentlyChooseACommand()
    {
        var f = new QualitativeReflexFixture();
        var stateEvent = new Placeholder();
        var uncertainMode = f.Stock.Initial(f.Agent, stateEvent,
            StructuralConstraint.Closed(f.Stock.Empty, f.Stock.Full));
        var light = f.See(new Placeholder(), stateEvent, StructuralConstraint.Closed(f.LightFrame.Lower));
        var competing = f.Read(uncertainMode, light);
        Assert.False(f.TryReadCommand(competing, out _));
        Assert.True(FiniteLaw.TryReadResult(competing, out var choices, out var residue, out _));
        Assert.Equal(2, FiniteLaw.Entries(choices).Length); Assert.Empty(FiniteLaw.Entries(residue));
        var empty = f.Stock.Initial(f.Agent, stateEvent, StructuralConstraint.Closed(f.Stock.Empty));
        var missing = f.Read(empty, f.See(new Placeholder(), stateEvent, StructuralConstraint.Open(f.LightFrame.Lower)));
        Assert.False(f.TryReadCommand(missing, out _));
        Assert.True(FiniteLaw.TryReadResult(missing, out var known, out var pending, out _));
        Assert.Same(f.MoveToB, Assert.Single(FiniteLaw.Entries(known)));
        Assert.NotEmpty(FiniteLaw.Entries(pending));
    }

    [Fact]
    public void EyeIdentityAndConsumedMemoryEventArePartOfTheConditionalConnection()
    {
        var f = new QualitativeReflexFixture();
        var stateEvent = new Placeholder();
        var state = f.Stock.Initial(f.Agent, stateEvent, StructuralConstraint.Closed(f.Stock.Empty));
        var reversedEyes = f.LightFrame.Observe(f.LightSource, f.EyeB, f.EyeA,
            new Relation(stateEvent, new Placeholder()), StructuralConstraint.Closed(f.LightFrame.Lower));
        var wrongSource = f.LightFrame.Observe(new Placeholder(), f.EyeA, f.EyeB,
            new Relation(stateEvent, new Placeholder()), StructuralConstraint.Closed(f.LightFrame.Lower));
        foreach (var observation in new[]
        {
            reversedEyes, wrongSource,
            f.See(new Placeholder(), new Placeholder(), StructuralConstraint.Closed(f.LightFrame.Lower))
        })
        {
            var read = f.Read(state, observation);
            Assert.False(f.TryReadCommand(read, out _));
            Assert.True(FiniteLaw.TryReadResult(read, out _, out var pending, out _));
            Assert.NotEmpty(FiniteLaw.Entries(pending));
            Assert.True(Contains(read, observation)); Assert.True(Contains(read, state));
        }
    }

    [Fact]
    public void AReflexCommandMustComeFromItsOwnPolicyAndActionDomain()
    {
        var f = new QualitativeReflexFixture();
        var stockRead = FiniteLaw.Evaluate(f.Stock.Law,
            StructuralConstraint.Closed(f.Stock.Empty), StructuralConstraint.Closed(f.Stock.Charge));
        Assert.False(f.TryReadCommand(stockRead, out _));
        var foreign = new QualitativeReflexFixture();
        var eventIdentity = new Placeholder();
        var read = foreign.Read(foreign.Stock.Initial(foreign.Agent, eventIdentity,
                StructuralConstraint.Closed(foreign.Stock.Empty)),
            foreign.See(new Placeholder(), eventIdentity, StructuralConstraint.Closed(foreign.LightFrame.Lower)));
        Assert.True(foreign.TryReadCommand(read, out _));
        Assert.False(f.TryReadCommand(read, out _));
    }

    [Fact]
    public void AnOldObservationCannotBeCombinedWithAdvancedOrForeignStockState()
    {
        var f = new QualitativeReflexFixture();
        var eventIdentity = new Placeholder();
        var state = f.Stock.Initial(f.Agent, eventIdentity, StructuralConstraint.Closed(f.Stock.Empty));
        var observation = f.See(new Placeholder(), eventIdentity, StructuralConstraint.Closed(f.LightFrame.Lower));
        var advanced = Pulse(f, state, f.Stock.Charge);
        Assert.False(f.TryReadCommand(f.Read(advanced, observation), out _));
        var foreignOwner = f.Stock.Initial(new Placeholder(), eventIdentity, StructuralConstraint.Closed(f.Stock.Empty));
        Assert.False(f.TryReadCommand(f.Read(foreignOwner, observation), out _));
        Assert.True(f.TryReadCommand(f.Read(state, observation), out _)); // Explicit historical reread remains observation.
    }

    [Fact]
    public void EqualEyesCanSettleRestWithoutDeterminingTheInternalMode()
    {
        var f = new QualitativeReflexFixture();
        var eventIdentity = new Placeholder();
        var state = f.Stock.Initial(f.Agent, eventIdentity, f.Stock.StateDomain);
        var read = f.Read(state, f.See(new Placeholder(), eventIdentity, StructuralConstraint.Closed(f.LightFrame.Equal)));
        Assert.True(f.TryReadCommand(read, out var command));
        Assert.Same(f.Rest, command);
    }

    [Fact]
    public void NativeConnectionAutomaticallyMovesTowardThenAwayWhenTheExplicitStockChanges()
    {
        var f = new QualitativeReflexFixture();
        var world = new LineWorld(f);
        IElement site = world.A;
        var state = f.Stock.Initial(f.Agent, new Placeholder(), StructuralConstraint.Closed(f.Stock.Empty));
        var first = Act(f, world, state, site);
        site = first.Site;
        Assert.Same(world.B, site);
        state = Pulse(f, state, f.Stock.Charge);
        state = Pulse(f, state, f.Stock.Charge);
        var second = Act(f, world, state, site);
        site = second.Site;
        Assert.Same(world.A, site);
        state = Pulse(f, state, f.Stock.Draw); // Declared decay event, not inferred elapsed time.
        var third = Act(f, world, state, site);
        Assert.Same(world.B, third.Site);
        var trace = StructuralConstraint.Closed(first.Witness, second.Witness, third.Witness);
        Assert.True(Contains(trace, world.SensorLaw)); Assert.True(Contains(trace, world.MovementLaw));
        Assert.True(Contains(trace, f.Policy)); AssertNoNumbers(trace);
    }

    [Fact]
    public void ASelectedCommandAndAcceptedMovementRemainSeparateAtTheBoundary()
    {
        var f = new QualitativeReflexFixture(); var world = new LineWorld(f);
        var state = f.Stock.Initial(f.Agent, new Placeholder(), StructuralConstraint.Closed(f.Stock.Empty));
        var moved = Act(f, world, state, world.C);
        Assert.Same(world.C, moved.Site);
        Assert.True(FiniteLaw.TryReadResult(moved.Witness, out var support, out _, out _));
        var outcome = Assert.IsType<Relation>(Assert.Single(FiniteLaw.Entries(support)));
        var disposition = Assert.IsType<Relation>(outcome.Right);
        Assert.Empty(FiniteLaw.Entries(Assert.IsType<Relation>(disposition.Left)));
        Assert.Same(f.MoveToB, Assert.Single(FiniteLaw.Entries(Assert.IsType<Relation>(disposition.Right))));
    }

    private static Relation Pulse(QualitativeReflexFixture f, Relation state, IElement input)
    {
        Assert.True(f.Stock.TryReadState(state, out var source, out var stateEvent, out _));
        var offered = f.Stock.Offer(source, new Placeholder(), stateEvent, StructuralConstraint.Closed(input));
        var step = f.Stock.Step(state, offered);
        Assert.True(f.Stock.TryReadStep(step, out _, out _, out var after, out _, out var unresolved));
        Assert.Empty(FiniteLaw.Entries(unresolved));
        return after;
    }

    private static (IElement Site, Relation Witness) Act(QualitativeReflexFixture f, LineWorld world,
        Relation state, IElement site)
    {
        Assert.True(f.Stock.TryReadState(state, out _, out var stateEvent, out _));
        var supplied = FiniteLaw.Evaluate(world.SensorLaw, StructuralConstraint.Closed(site),
            StructuralConstraint.Closed(world.DetectorContext));
        Assert.True(FiniteLaw.TryReadResult(supplied, out var light, out _, out _));
        var observation = f.See(new Placeholder(), stateEvent, light, supplied);
        var command = f.Read(state, observation);
        Assert.True(f.TryReadCommand(command, out _));
        Assert.True(FiniteLaw.TryReadResult(command, out var actions, out _, out _));
        var moved = FiniteLaw.WithProvenance(FiniteLaw.Evaluate(world.MovementLaw,
            StructuralConstraint.Closed(site), actions), command);
        Assert.True(FiniteLaw.TryReadResult(moved, out var result, out _, out _));
        var outcome = Assert.IsType<Relation>(Assert.Single(FiniteLaw.Entries(result)));
        return (outcome.Left, moved);
    }

    // A deliberately declared line environment. Rows are the movement contract,
    // not a claim that contraction mechanically produces propulsion.
    private sealed class LineWorld
    {
        public IElement A { get; } = new Placeholder();
        public IElement B { get; } = new Placeholder();
        public IElement C { get; } = new Placeholder();
        public IElement DetectorContext { get; } = new Placeholder();
        public Relation SensorLaw { get; }
        public Relation MovementLaw { get; }

        public LineWorld(QualitativeReflexFixture f)
        {
            var sites = StructuralConstraint.Closed(A, B, C);
            SensorLaw = FiniteLaw.Create(sites, StructuralConstraint.Closed(DetectorContext), f.LightFrame.Domain,
                FiniteLaw.Row(A, DetectorContext, StructuralConstraint.Closed(f.LightFrame.Lower)),
                FiniteLaw.Row(B, DetectorContext, StructuralConstraint.Closed(f.LightFrame.Lower)),
                FiniteLaw.Row(C, DetectorContext, StructuralConstraint.Closed(f.LightFrame.Lower)));
            Relation Outcome(IElement site, IElement action, bool blocked = false) =>
                new(site, new Relation(blocked ? StructuralConstraint.Closed() : StructuralConstraint.Closed(action),
                    blocked ? StructuralConstraint.Closed(action) : StructuralConstraint.Closed()));
            Relation[] outcomes =
            [
                Outcome(A, f.MoveToA, true), Outcome(A, f.Rest), Outcome(B, f.MoveToB),
                Outcome(A, f.MoveToA), Outcome(B, f.Rest), Outcome(C, f.MoveToB),
                Outcome(B, f.MoveToA), Outcome(C, f.Rest), Outcome(C, f.MoveToB, true)
            ];
            var actions = new[] { f.MoveToA, f.Rest, f.MoveToB };
            var positions = new[] { A, B, C };
            var rows = new List<Relation>();
            for (var p = 0; p < positions.Length; p++)
                for (var a = 0; a < actions.Length; a++)
                    rows.Add(FiniteLaw.Row(positions[p], actions[a], StructuralConstraint.Closed(outcomes[p * 3 + a])));
            MovementLaw = FiniteLaw.Create(sites, f.Actions, StructuralConstraint.Closed(outcomes), rows.ToArray());
        }
    }

    private static bool Contains(IElement root, IElement target) => Traverse(root).Any(e => ReferenceEquals(e, target));
    private static void AssertNoNumbers(IElement root)
    {
        foreach (var element in Traverse(root))
        {
            Assert.IsNotType<Carrier>(element); Assert.IsNotType<CarrierLocation>(element);
        }
    }
    private static IEnumerable<IElement> Traverse(IElement root)
    {
        var seen = new HashSet<IElement>(ReferenceEqualityComparer.Instance);
        var pending = new Stack<IElement>(); pending.Push(root);
        while (pending.TryPop(out var element))
        {
            if (!seen.Add(element)) continue;
            yield return element;
            if (element is not Relation relation) continue;
            pending.Push(relation.Left); pending.Push(relation.Right);
        }
    }
}
