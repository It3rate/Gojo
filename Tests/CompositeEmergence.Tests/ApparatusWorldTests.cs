using Gojo.Core;
using Gojo.Structures;

namespace CompositeEmergence.Tests;

public sealed class ApparatusWorldTests
{
    [Fact]
    public void ExactBalancedEnumerationMakesEitherPrimitiveUninformativeButPairInformative()
    {
        var world = new ApparatusWorld(); var axisA = new Carrier(); var axisB = new Carrier();
        var actionAxis = new Carrier(); var outcomeAxis = new Carrier();
        var states = (from rule in new[] { -1, 1 } from input in new[] { -1, 1 }
                      from action in new[] { -1, 1 }
                      select ReadState(rule, input, action)).ToArray();
        foreach (var channel in new[] { 0, 1 })
            foreach (var value in new[] { -1, 1 })
            {
                var rows = states.Where(row => (channel == 0 ? row.A : row.B) == value).ToArray();
                Assert.Equal(2, rows.Count(row => row.Rule == -1)); Assert.Equal(2, rows.Count(row => row.Rule == 1));
                Assert.Equal(2, rows.Count(row => row.Action == -1)); Assert.Equal(2, rows.Count(row => row.Action == 1));
                foreach (var action in new[] { -1, 1 })
                {
                    var conditional = rows.Where(row => row.Action == action).ToArray();
                    Assert.Equal(1, conditional.Count(row => row.Outcome == -1)); Assert.Equal(1, conditional.Count(row => row.Outcome == 1));
                }
            }
        foreach (var pair in states.GroupBy(row => (row.A, row.B))) Assert.Single(pair.Select(row => row.Rule).Distinct());
        Assert.All(states, row => Assert.Equal(row.Rule, row.A * row.B));

        ObservationState ReadState(int rule, int input, int action)
        {
            var trial = Trial() with { Rule = rule, Input = input };
            var command = WorldPublication.Value(actionAxis, action, new Carrier());
            Assert.True(WorldPublication.TryValue(world.Input(trial, axisA), axisA, out var a));
            Assert.True(WorldPublication.TryValue(world.Response(trial, axisB), axisB, out var b));
            Assert.True(WorldPublication.TryValue(world.DirectionConsequence(trial, command, actionAxis, outcomeAxis), outcomeAxis, out var outcome));
            return new(rule, checked((int)a), checked((int)b), action, checked((int)outcome));
        }
    }

    [Fact]
    public void ConfigurationChangesDoNotConsumeOtherRandomStreams()
    {
        var iid = ApparatusTape.Create(771, 500);
        var persistent = ApparatusTape.Create(771, 500, persistent: true, switchProbability: .25, maximumDelay: 19, missingProbability: .8);
        for (var i = 0; i < iid.Count; i++)
        {
            Assert.Equal(iid[i].Input, persistent[i].Input); Assert.Equal(iid[i].Goal, persistent[i].Goal);
            Assert.Equal(iid[i].ExploratoryAction, persistent[i].ExploratoryAction);
        }
        var changedMissing = ApparatusTape.Create(771, 500, missingProbability: 1);
        for (var i = 0; i < iid.Count; i++)
        {
            Assert.Equal(iid[i].Rule, changedMissing[i].Rule); Assert.Equal(iid[i].Delay, changedMissing[i].Delay);
            Assert.True(changedMissing[i].MissingA); Assert.True(changedMissing[i].MissingB);
            Assert.False(iid[i].MissingA); Assert.False(iid[i].MissingB);
        }
    }

    [Fact]
    public void EitherThreeEventHistoryRemainsUninformativeForEveryRuleHistory()
    {
        var world = new ApparatusWorld(); var axisA = new Carrier(); var axisB = new Carrier();
        var histories = new List<(int Rules, int A, int B)>();
        for (var rules = 0; rules < 8; rules++) for (var inputs = 0; inputs < 8; inputs++)
        {
            var observedA = 0; var observedB = 0;
            for (var step = 0; step < 3; step++)
            {
                var rule = (rules & (1 << step)) == 0 ? -1 : 1;
                var input = (inputs & (1 << step)) == 0 ? -1 : 1;
                var trial = Trial() with { Index = step, Rule = rule, Input = input };
                Assert.True(WorldPublication.TryValue(world.Input(trial, axisA), axisA, out var a));
                Assert.True(WorldPublication.TryValue(world.Response(trial, axisB), axisB, out var b));
                if (a == 1) observedA |= 1 << step; if (b == 1) observedB |= 1 << step;
            }
            histories.Add((rules, observedA, observedB));
        }
        foreach (var rules in Enumerable.Range(0, 8)) foreach (var observed in Enumerable.Range(0, 8))
        {
            Assert.Single(histories, history => history.Rules == rules && history.A == observed);
            Assert.Single(histories, history => history.Rules == rules && history.B == observed);
        }
        // Conditional on ANY fixed rule trajectory, each local history has
        // exactly the same uniform input preimage. This includes persistent and
        // changing trajectories, rather than only a one-event marginal check.
    }

    [Fact]
    public void TapeIsReproducibleAndExplorerIsNotTheSensorOrGoalStream()
    {
        var a = ApparatusTape.Create(183, 2048); var b = ApparatusTape.Create(183, 2048); var other = ApparatusTape.Create(184, 2048);
        for (var i = 0; i < a.Count; i++) Assert.Equal(a[i], b[i]);
        Assert.Contains(Enumerable.Range(0, a.Count), i => a[i].ExploratoryAction != a[i].Input);
        Assert.Contains(Enumerable.Range(0, a.Count), i => a[i].ExploratoryAction != a[i].Goal);
        Assert.Contains(Enumerable.Range(0, a.Count), i => a[i].ExploratoryAction != other[i].ExploratoryAction);
        // This checks construction and accidental stream reuse, not a statistical
        // proof of independence for arbitrary pseudorandom number generators.
    }

    [Fact]
    public void MissingDirectionsAreOpenPublicationsWithKnownEventTimes()
    {
        var world = new ApparatusWorld(); var a = new Carrier(); var b = new Carrier();
        var trial = Trial() with { Index = 7, Delay = 3, MissingA = true, MissingB = true };
        var input = world.Input(trial, a); var response = world.Response(trial, b);
        Assert.False(WorldPublication.TryValue(input, a, out _)); Assert.False(WorldPublication.TryValue(response, b, out _));
        Assert.True(ReadWitness.TryRead(input, out _, out _, out var result, out _, out _)); Assert.IsType<Placeholder>(result);
        Assert.True(NativeGraph.TryTimestamp(input, out var t0)); Assert.Equal(7 * 64, t0);
        Assert.True(NativeGraph.TryTimestamp(response, out var t1)); Assert.Equal(7 * 64 + 3, t1);
    }

    [Fact]
    public void EventTimelineOrdersObservationActionConsequenceAndResource()
    {
        var world = new ApparatusWorld(); var trial = Trial() with { Index = 2, Delay = 3 };
        var inputAxis = new Carrier(); var responseAxis = new Carrier(); var goalAxis = new Carrier(); var actionAxis = new Carrier();
        var outcomeAxis = new Carrier(); var resourceAxis = new Carrier(); var action = world.ExploratoryAction(trial, actionAxis);
        var publications = new[] { world.Input(trial, inputAxis), world.Response(trial, responseAxis), world.Goal(trial, goalAxis), action,
            world.DirectionConsequence(trial, action, actionAxis, outcomeAxis), world.Resource(trial, trial.ExploratoryAction, resourceAxis, command: action) };
        var times = publications.Select(publication => { Assert.True(NativeGraph.TryTimestamp(publication, out var time)); return time; }).ToArray();
        Assert.Equal(new long[] { 128, 131, 132, 133, 134, 135 }, times);
        Assert.All(publications, publication => Assert.IsType<Carrier>(NativeGraph.Identity(publication)));
    }

    [Theory]
    [InlineData(1, -1, -1)]
    [InlineData(1, 1, 1)]
    [InlineData(-1, -1, 1)]
    [InlineData(-1, 1, -1)]
    public void DirectionConsequenceDependsOnActualPublishedActionAndPrivateRule(int rule, int action, int expected)
    {
        var world = new ApparatusWorld(); var axis = new Carrier(); var outcomeAxis = new Carrier(); var trial = Trial() with { Rule = rule };
        var command = WorldPublication.Value(axis, action, new Carrier()); var outcome = world.DirectionConsequence(trial, command, axis, outcomeAxis);
        Assert.True(WorldPublication.TryValue(outcome, outcomeAxis, out var value)); Assert.Equal(expected, value);
        Assert.False(WorldPublication.TryValue(world.DirectionConsequence(trial, command, new Carrier(), outcomeAxis), outcomeAxis, out _));
        Assert.False(WorldPublication.TryValue(world.DirectionConsequence(trial, WorldPublication.Value(axis, 0, new Carrier()), axis, outcomeAxis), outcomeAxis, out _));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(-1)]
    public void ResourceCorrectnessHoldAndProbeFeeHaveConsistentPhysicalSemantics(int wiring)
    {
        var world = new ApparatusWorld(wiring, probePrice: 3); var resourceAxis = new Carrier();
        foreach (var rule in new[] { -1, 1 }) foreach (var goal in new[] { -1, 1 }) foreach (var action in new[] { -1, 0, 1 })
        {
            var trial = Trial() with { Rule = rule, Goal = goal }; var correct = world.IsCorrect(trial, action);
            var expected = action == 0 ? 0 : correct ? ApparatusWorld.CorrectResource : ApparatusWorld.WrongResource;
            Assert.True(WorldPublication.TryValue(world.Resource(trial, action, resourceAxis), resourceAxis, out var value)); Assert.Equal(expected, value);
            Assert.True(WorldPublication.TryValue(world.Resource(trial, action, resourceAxis, probed: true), resourceAxis, out var paid)); Assert.Equal(expected - 3, paid);
        }
    }

    [Fact]
    public void PublicResourceAndConsequenceWithholdPrivateDecisionAncestry()
    {
        var world = new ApparatusWorld(); var a = new Carrier(); var b = new Carrier(); var privateMemory = new Carrier();
        var actionAxis = new Carrier(); var resourceAxis = new Carrier(); var outcomeAxis = new Carrier();
        var privateDecision = ReadWitness.Create(new Carrier(), NativeGraph.G(a, b, privateMemory), NativeGraph.G(a, b),
            new CarrierLocation(actionAxis, 1), NativeGraph.G(), NativeGraph.G(privateMemory, a));
        var trial = Trial(); var resource = world.Resource(trial, 1, resourceAxis, command: privateDecision);
        var outcome = world.DirectionConsequence(trial, privateDecision, actionAxis, outcomeAxis);
        foreach (var publication in new[] { resource, outcome })
        {
            var reachable = Reach(publication); Assert.DoesNotContain(a, reachable); Assert.DoesNotContain(b, reachable);
            Assert.DoesNotContain(privateMemory, reachable); Assert.DoesNotContain(privateDecision, reachable);
            Assert.Contains(actionAxis, reachable); Assert.Contains(world.TimeAxis, reachable);
        }
        Assert.Contains(resourceAxis, Reach(resource)); Assert.Contains(outcomeAxis, Reach(outcome));
    }

    [Fact]
    public void ObservationPublicationDoesNotAssignOrPerturbLearnerCarriers()
    {
        var world = new ApparatusWorld(); var sensor = new Carrier(); var resource = new Carrier(); sensor.Add(5); resource.Add(7);
        var sensorBefore = (sensor.Value, sensor.Tension); var resourceBefore = (resource.Value, resource.Tension);
        world.Input(Trial(), sensor); world.Response(Trial(), sensor); world.Resource(Trial(), -1, resource, probed: true);
        Assert.Equal(sensorBefore, (sensor.Value, sensor.Tension)); Assert.Equal(resourceBefore, (resource.Value, resource.Tension));
    }

    private static ApparatusTrial Trial() => new(0, 1, -1, 1, 1, 1, false, false);
    private sealed record ObservationState(int Rule, int A, int B, int Action, int Outcome);
    private static HashSet<object> Reach(IElement root)
    {
        var seen = new HashSet<object>(ReferenceEqualityComparer.Instance);
        var seenUnits = new HashSet<UnitShape>(ReferenceEqualityComparer.Instance);
        void Visit(IElement element)
        {
            if (element is CarrierLocation point) { Visit(point.Carrier); return; }
            if (!seen.Add(element)) return;
            if (element is Relation relation) { Visit(relation.Left); Visit(relation.Right); }
            if (element is Carrier carrier)
            {
                foreach (var attachment in carrier.AttachedRelations) Visit(attachment);
                if (seenUnits.Add(carrier.UnitShape))
                {
                    Visit(carrier.UnitShape.Surface); foreach (var descriptor in carrier.UnitShape.DescriptorSurfaces) Visit(descriptor);
                }
            }
        }
        Visit(root); return seen;
    }
}
