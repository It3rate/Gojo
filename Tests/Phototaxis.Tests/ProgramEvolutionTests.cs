using Gojo.Core;
using Gojo.Structures;
using Phototaxis;
using static Phototaxis.Native;

namespace Phototaxis.Tests;

public class ProgramEvolutionTests
{
    private static Bath World(int population = 1, double duty = .9) => new(population: population, resources: MachineResources.Fixed(duty))
        { ExposureRisk = false, Breeding = false };
    private static void Energy(Bath bath, Creature creature, long target)
    {
        var current = (long)creature.Energy.Value;
        if (target > current) Transfer(bath.CalorieReserve, creature.Energy, target - current);
        else Transfer(creature.Energy, bath.MetabolicSink, current - target);
    }
    private static long Value(IElement expression)
    {
        Assert.True(TryValue(ReadMany(expression)[0], out var value));
        Assert.True(TryValue(ReferenceRead(expression), out var reference)); Assert.Equal(reference, value); return value;
    }

    [Fact]
    public void ArithmeticComparisonsAndBooleanChoicesAreNativeComposableReads()
    {
        var units = new Units(); var a = L(units.Feature, 9); var b = L(units.Feature, 3); var zero = L(units.Feature, 0);
        Assert.Equal(12, Value(ExpressionGraph.Add(units, a, b)));
        Assert.Equal(6, Value(ExpressionGraph.Subtract(units, a, b)));
        Assert.Equal(1, Value(ExpressionGraph.Multiply(units, a, b)));
        Assert.Equal(45, Value(ExpressionGraph.Quotient(units, a, b)));
        Assert.Equal(0, Value(ExpressionGraph.Less(units, a, b)));
        Assert.Equal(15, Value(ExpressionGraph.Greater(units, a, b)));
        Assert.Equal(15, Value(ExpressionGraph.Equal(units, a, a)));
        var yes = ExpressionGraph.Truth(units, a); var no = ExpressionGraph.Truth(units, zero);
        Assert.Equal(0, Value(ExpressionGraph.And(units, yes, no)));
        Assert.Equal(15, Value(ExpressionGraph.Or(units, no, yes)));
        Assert.Equal(0, Value(ExpressionGraph.Xor(units, yes, yes)));
        Assert.Equal(15, Value(ExpressionGraph.Not(units, no)));
        Assert.False(TryValue(ReadMany(ExpressionGraph.And(units, yes, b))[0], out _));
        Assert.False(TryValue(ReadMany(ExpressionGraph.Quotient(units, a, zero))[0], out _));
        Assert.False(TryValue(ReadMany(ExpressionGraph.Choose(units, b, a, zero))[0], out _)); // no invented truth at 3
    }

    [Fact]
    public void LazyBranchRetainsAnUndefinedUnchosenProgramAndSurvivesNumericTransport()
    {
        var bath = World(); var c = bath.Creatures[0]; c.LeftEye.Admit([9], 1);
        var unavailable = ExpressionGraph.Quotient(bath.Units, c.Sources[1], L(bath.Units.Feature, 0));
        var source = ExpressionGraph.Choose(bath.Units, L(bath.Units.Feature, 15), c.Sources[1], unavailable);
        var circuit = new Circuit(bath.Units, new Component(bath.Units, source, 15));
        c.LeftMuscle.InstallInherited(circuit); Assert.Equal(9, Value(circuit.Output));
        var restored = (Relation)GojoNumericFormat.Decode(c.ExportNativeGraph()).Single();
        var muscle = (Relation)Parts((Relation)Parts(restored)[2])[0];
        var output = (Relation)Parts((Relation)Parts(muscle)[3])[1]; Assert.Equal(9, Value(output));
        Assert.True(ExpressionGraph.Count(c.Features, source) > 1);
        Assert.Equal(0UL, circuit.Utility.Visits.Value);
    }

    [Fact]
    public void ExpressivityFixtureCanReverseSteeringWithEnergyWithoutChangingTheProgram()
    {
        // This is a capability fixture, never a starting or market-supplied strategy.
        var bath = World(); var c = bath.Creatures[0]; var u = bath.Units;
        c.LeftEye.Admit([12], 1); c.RightEye.Admit([3], 1);
        var hungry = ExpressionGraph.Less(u, c.Sources[4], L(u.Feature, 7));
        var turn = ExpressionGraph.Choose(u, hungry, ExpressionGraph.Subtract(u, c.Sources[2], c.Sources[1]),
            ExpressionGraph.Subtract(u, c.Sources[1], c.Sources[2]));
        c.LeftMuscle.InstallInherited(new Circuit(u, new Component(u, c.Sources[0], 8), new Component(u, turn, 6)));
        c.RightMuscle.InstallInherited(new Circuit(u, new Component(u, c.Sources[0], 8), new Component(u, turn, -6)));
        Energy(bath, c, 300); c.LeftMuscle.Wake(c, 2); c.RightMuscle.Wake(c, 3);
        Assert.True(c.ReadMotion(out _, out var hungryYaw)); Assert.True(hungryYaw < 0);
        var retained = c.LeftMuscle.Current.Root;
        Energy(bath, c, 1000); c.LeftMuscle.Wake(c, 20); c.RightMuscle.Wake(c, 21);
        Assert.True(c.ReadMotion(out _, out var fullYaw)); Assert.True(fullYaw > 0);
        Assert.Same(retained, c.LeftMuscle.Current.Root); Assert.NotEqual(c.LeftMuscle.NextTime.Value, c.RightMuscle.NextTime.Value);
        bath.CheckConservation();
    }

    [Fact]
    public void MarketBuildsAndPrunesProgramsInsideItsConstructionBudget()
    {
        var bath = World(); var c = bath.Creatures[0]; var cursor = new Carrier(); var circuit = c.LeftMuscle.Current;
        var largest = 0; var smaller = false;
        for (var i = 0; i < 84; i++)
        {
            var next = CircuitMarket.Propose(bath.Units, circuit, c.Sources, cursor, out var correspondence);
            var count = ExpressionGraph.Count(c.Features, next.Components.Select(t => t.Source).ToArray());
            Assert.InRange(count, 0, ExpressionGraph.MaximumOperations);
            Assert.All(next.Components, term => Assert.InRange(ExpressionGraph.Depth(c.Features, term.Source), 0, ExpressionGraph.MaximumDepth));
            if (count < largest) smaller = true; largest = Math.Max(largest, count);
            Assert.Same(circuit.Root, ((Relation)Parts(correspondence)[0]).Left); circuit = next;
        }
        Assert.True(largest >= 4); Assert.True(smaller);
    }

    [Fact]
    public void RebindingKeepsChoiceCalibrationAndDoesNotReuseParentOutputIdentity()
    {
        var bath = World(); var a = bath.Creatures[0]; var b = new Creature(bath.Units, 90, 0, 50, 50, 0,
            new Random(11), bath.SpentAttention, bath.ResourceYield);
        a.LeftEye.Admit([8], 1); b.LeftEye.Admit([5], 1);
        var output = new Carrier(bath.Units.Feature.UnitShape);
        var selector = a.Sources[0]; var axis = ((CarrierLocation)selector).Carrier;
        var choice = ScopedChoice.Create(selector, Relation.Segment(axis, 0, 16), output,
            ScopedChoice.Offer(Relation.Segment(axis, 15, 16), a.Sources[1],
                Gojo.Equations.Equation.Affine(ConnectionOutput(a.Sources[1]), 0, 1, output, 0, -2)));
        var rebound = ExpressionGraph.Rebind(choice, a.Sources, b.Sources);
        Assert.Equal(-10, Value(rebound)); Assert.Equal(-16, Value(choice));
        Assert.True(ScopedChoice.TryRead((Relation)rebound, out _, out _, out _, out var childOutput));
        Assert.NotSame(output, childOutput);

        static Carrier ConnectionOutput(IElement source) => (Carrier)Parts((Relation)source)[2];
    }

    [Fact]
    public void UndefinedProgramTrialsRetainFailedReadsAndReceiveNoFreeUtility()
    {
        var bath = World(); var c = bath.Creatures[0];
        var source = ExpressionGraph.Quotient(bath.Units, c.Sources[0], L(bath.Units.Feature, 0));
        var circuit = new Circuit(bath.Units, new Component(bath.Units, source, 15)); c.LeftMuscle.InstallInherited(circuit);
        c.LeftMuscle.Wake(c, 1); c.LeftMuscle.Wake(c, 12); c.LeftMuscle.Review(c, 161);
        Assert.Equal(2, c.LeftMuscle.Unresolved); Assert.False(TryValue(c.LeftMuscle.LastRead, out _));
        Assert.True(Held(circuit.Utility.Returns) < 0); Assert.NotEmpty(Parts(c.LeftMuscle.LastReview));
    }

    [Fact]
    public void PressureTrialsChangeOneCircuitAtATimeAndBidsRetainRecentEvidence()
    {
        var bath = World(); var c = bath.Creatures[0]; Energy(bath, c, 100);
        c.LeftEye.Admit([5], 1); c.RightEye.Admit([7], 1);
        c.LeftMuscle.Wake(c, 1); c.RightMuscle.Wake(c, 1);
        var right = c.RightMuscle.Current;
        c.Inspect(Creature.EvaluationTicks, true);
        Assert.Equal(1UL, c.LeftMuscle.SearchCount.Value); Assert.Equal(0UL, c.RightMuscle.SearchCount.Value); Assert.Same(right, c.RightMuscle.Current);
        c.Inspect(Creature.EvaluationTicks * 2, true);
        Assert.Equal(1UL, c.LeftMuscle.SearchCount.Value); Assert.Equal(1UL, c.RightMuscle.SearchCount.Value);
        var utility = new Utility(bath.Units);
        utility.Record(L(bath.Units.Return, 80), 160, 0); var supported = Value(utility.Bid);
        utility.Record(L(bath.Units.Return, -80), 160, 0); var failed = Value(utility.Bid);
        Assert.True(supported > failed); Assert.Equal(-80, Held(utility.RecentReturn)); Assert.Equal(0, Held(utility.Returns));
        Assert.Same(utility.RecentReturn, Parts(utility.Root)[5]); bath.CheckConservation();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(.9)]
    [InlineData(1)]
    public void RestingCannotMaintainEnergyEvenInPeakLightWithRiskDisabled(double duty)
    {
        var bath = World(duty: duty); var c = bath.Creatures[0]; bath.Learning = false;
        c.X.Tick.Set((long)(bath.LightX * 16)); c.Y.Tick.Set((long)(bath.LightY * 16));
        Aim(c.LeftMuscle.NextTime, 10000); Aim(c.RightMuscle.NextTime, 10000); Energy(bath, c, 1100);
        var before = c.Energy.Value; bath.Step(200);
        Assert.Single(bath.Creatures); Assert.True(c.Energy.Value <= before - 150);
        Assert.Equal(0, bath.MeanSpeed); Assert.Equal(1, bath.StationaryFraction); bath.CheckConservation();
    }

    [Fact]
    public void MatingRequiresContactDarknessFullnessAndCooldownIncludingAcrossTheWrap()
    {
        var bath = World(2); var a = bath.Creatures[0]; var b = bath.Creatures[1];
        foreach (var c in bath.Creatures) { Aim(c.Age, Bath.MaturityTicks); Energy(bath, c, 1000); c.Y.Tick.Set(640); }
        a.X.Tick.Set(16); b.X.Tick.Set(1600); bath.Breed(); Assert.Equal(0, bath.Births);
        b.X.Tick.Set((long)((Bath.Width - 1) * 16));
        Assert.Equal(2, Bath.WrappedDistance(a.Px, a.Py, b.Px, b.Py));
        bath.Breed(); Assert.Equal(1, bath.Births);
        var child = bath.Creatures[2]; Assert.True(child.Px < 10 || child.Px > Bath.Width - 10);
        Energy(bath, a, 1000); Energy(bath, b, 1000); bath.Breed(); Assert.Equal(1, bath.Births);
        Assert.Equal((ulong)Bath.MatingCooldownTicks, a.NextMating.Value); bath.CheckConservation();
    }

    [Fact]
    public void OffspringInheritNestedProgramsReboundToIndependentChildSensors()
    {
        var bath = World(2); var a = bath.Creatures[0]; var b = bath.Creatures[1]; var u = bath.Units;
        foreach (var c in bath.Creatures) { Aim(c.Age, Bath.MaturityTicks); Energy(bath, c, 1000); c.X.Tick.Set(640); c.Y.Tick.Set(640); }
        var expression = ExpressionGraph.Choose(u, ExpressionGraph.Greater(u, b.Sources[4], L(u.Feature, 7)),
            ExpressionGraph.Add(u, b.Sources[1], b.Sources[2]), ExpressionGraph.Subtract(u, b.Sources[1], b.Sources[2]));
        b.LeftMuscle.InstallInherited(new Circuit(u, new Component(u, expression, 6)));
        b.LeftMuscle.Current.Components[0].Utility.Record(L(u.Return, 3000), 160, 0);
        bath.Breed(); var child = bath.Creatures[2];
        Energy(bath, child, 1000);
        child.LeftEye.Admit([5], 1); child.RightEye.Admit([10], 1);
        Assert.Equal(10, Value(child.LeftMuscle.Current.Output)); // inherited expression + the other parent's constant term
        b.LeftEye.Admit([15], 2); b.RightEye.Admit([15], 2);
        Assert.Equal(10, Value(child.LeftMuscle.Current.Output));
        Energy(bath, child, 300); Assert.Equal(2, Value(child.LeftMuscle.Current.Output));
        Assert.NotSame(expression, child.LeftMuscle.Current.Components[0].Source);
        Assert.True(ExpressionGraph.Count(child.Features, child.LeftMuscle.Current.Components[0].Source) > 0);
        bath.CheckConservation();
    }
}
