using Gojo.Core;
using Gojo.Structures;
using Phototaxis;
using static Phototaxis.Native;

namespace Phototaxis.Tests;

public class NativeCreatureTests
{
    private static Bath Fixed(int seed = 7, int population = 1, double duty = 0, long work = 10) =>
        new(seed, population, resources: MachineResources.Fixed(duty, work)) { ExposureRisk = false, Breeding = false };

    private static void Energy(Bath bath, Creature creature, long amount)
    {
        var held = (long)creature.Energy.Value;
        if (amount > held) Transfer(bath.CalorieReserve, creature.Energy, amount - held);
        else Transfer(creature.Energy, bath.MetabolicSink, held - amount);
    }

    [Fact]
    public void CachedReadsMatchNativeReaderAndRetainEarlierAdmittedSamples()
    {
        var bath = Fixed();
        var c = bath.Creatures[0];
        var component = new Component(bath.Units, c.LeftEye.Mean, -6);
        var circuit = new Circuit(bath.Units, component);
        var roots = new IElement[] { c.LeftEye.Mean, c.Deficit, circuit.Output, circuit.Utility.Bid };
        Assert.False(TryValue(ReadMany(c.LeftEye.Mean)[0], out _));
        c.LeftEye.Admit([3], 4); c.RightEye.Admit([12], 6);
        var first = ReadMany(roots);
        var values = first.Select(r => ((CarrierLocation)r.Left).Ticks).ToArray();
        Assert.Equal(roots.Select(r => ((CarrierLocation)ReferenceRead(r).Left).Ticks), values);
        Aim(component.Coefficient, -12); circuit.Utility.Visits.Add(4);
        c.LeftEye.Admit([15], 8);
        var later = ReadMany(roots);
        Assert.Equal(roots.Select(r => ((CarrierLocation)ReferenceRead(r).Left).Ticks), later.Select(r => ((CarrierLocation)r.Left).Ticks));
        Assert.Equal(4, ((CarrierLocation)later[3].Left).Ticks);
        Assert.Equal(values, first.Select(r => ((CarrierLocation)r.Left).Ticks));
        Assert.NotEqual(values[0], ((CarrierLocation)later[0].Left).Ticks);
    }

    [Fact]
    public void CachedProjectionKeepsFractionalResidueAndSeesRetargetedBasis()
    {
        var source = new Carrier();
        var recipe = Scale(L(source, 7), 3, 2);
        var first = ReadMany(recipe)[0];
        Assert.Equal(10, ((CarrierLocation)first.Left).Ticks);
        var localResidue = ((Relation)first.Right).Left;
        Assert.True(ProjectionResidue.TryRead(localResidue, out var residueSource, out var numerator, out var denominator));
        Assert.True(source.SameIdentityAs(residueSource)); Assert.Equal(1, numerator); Assert.Equal(3, denominator);
        var equation = (Relation)Parts(recipe)[0];
        var inputFrame = (Relation)equation.Left;
        var endpoint = (CarrierLocation)((Relation)inputFrame.Right).Left;
        endpoint.Tick.Set(7);
        Assert.Equal(3, ((CarrierLocation)ReadMany(recipe)[0].Left).Ticks);
        endpoint.Tick.Set(0);
        Assert.False(TryValue(ReadMany(recipe)[0], out _));
        Assert.Equal(10, ((CarrierLocation)first.Left).Ticks);
    }

    [Fact]
    public void UnsampledSingleRayIsUnresolvedAndItsSixteenLevelsRetainOverflow()
    {
        var eye = new Eye(new Units(), -35, 3, 1);
        Assert.Single(eye.Pixels); Assert.False(TryValue(Read(eye.Mean), out _));
        eye.Admit([20], 7);
        Assert.Equal(15UL, eye.Pixels[0].Value); Assert.Equal(5, eye.Pixels[0].Tension);
        Assert.True(TryValue(Read(eye.Mean), out var light)); Assert.Equal(15, light);
        Assert.Equal(7UL, eye.SampleTime.Value); Assert.Equal(10UL, eye.NextTime.Value);
    }

    [Fact]
    public void CentralTenPixelBeamStopsAtAnOpaqueRock()
    {
        var bath = Fixed();
        var c = bath.Creatures[0]; var rock = bath.Rocks[0];
        c.X.Tick.Set((long)((rock.X - rock.Radius) * 16)); c.Y.Tick.Set((long)(rock.Y * 16));
        c.Heading.Tick.Set((long)(Eye.OffsetDegrees / 360 * 4096));
        var transparent = bath.Photograph(c, c.LeftEye).Single();
        bath.RocksEnabled = true;
        Assert.Equal(10, Bath.VisionDistance);
        Assert.True(transparent > 0); Assert.Equal(0, bath.Photograph(c, c.LeftEye).Single());
        Assert.True(bath.Illumination(rock.X + rock.Radius + 1, rock.Y) > 0);
        Assert.Equal(-35, c.LeftEye.Offset); Assert.Equal(35, c.RightEye.Offset);
    }

    [Fact]
    public void RightPushTurnsLeftBothPushesMoveForwardAndForceRetainsOverflow()
    {
        var bath = Fixed(); var c = bath.Creatures[0];
        c.X.Tick.Set(8000); c.Y.Tick.Set(5440); c.Heading.Tick.Set(0);
        Aim(c.LeftMuscle.Force, 0); Aim(c.RightMuscle.Force, 15);
        Assert.True(c.ReadMotion(out var thrust, out var yaw)); Assert.Equal(15, thrust); Assert.Equal(-15, yaw);
        bath.Move(c); Assert.True(Bath.AngleDifference(c.Angle, 0) < 0); Assert.True(c.Px > 500);
        c.Heading.Tick.Set(0); Aim(c.LeftMuscle.Force, 15);
        Assert.True(c.ReadMotion(out thrust, out yaw)); Assert.Equal(30, thrust); Assert.Equal(0, yaw);
        Aim(c.LeftMuscle.Force, 20);
        Assert.Equal(15UL, c.LeftMuscle.Force.Value); Assert.Equal(5, c.LeftMuscle.Force.Tension);
    }

    [Theory]
    [InlineData(500)]
    [InlineData(599.9375)]
    public void ActualSwimmingReducesPredationIncludingAtTheWorldWrap(double startX)
    {
        var bath = Fixed(); var c = bath.Creatures[0]; bath.ExposureRisk = true;
        c.X.Tick.Set((long)(startX * 16)); c.Y.Tick.Set(5440); c.Heading.Tick.Set(0);
        Aim(c.LeftMuscle.Force, 0); Aim(c.RightMuscle.Force, 0); bath.Move(c);
        var stationary = bath.PredationChance(c);
        Aim(c.LeftMuscle.Force, 1); bath.Move(c);
        var slow = bath.PredationChance(c);
        c.X.Tick.Set((long)(startX * 16)); c.Heading.Tick.Set(0);
        Aim(c.LeftMuscle.Force, 4); Aim(c.RightMuscle.Force, 4); bath.Move(c);
        var swimming = bath.PredationChance(c);
        Assert.True(stationary > slow); Assert.True(slow > swimming);
        Assert.True(stationary > swimming * 3.8);
        bath.ExposureRisk = false; Assert.Equal(0, bath.PredationChance(c));
    }

    [Fact]
    public void RockBlockedPushesCountAsStationaryAndRockContactIncreasesRiskAgain()
    {
        var bath = Fixed(); var c = bath.Creatures[0]; var rock = bath.Rocks[0];
        bath.ExposureRisk = true; bath.RocksEnabled = true;
        c.X.Tick.Set((long)((rock.X - rock.Radius - 7.5) * 16)); c.Y.Tick.Set((long)(rock.Y * 16));
        c.Heading.Tick.Set(0); Aim(c.LeftMuscle.Force, 15); Aim(c.RightMuscle.Force, 15);
        var before = (c.X.Ticks, c.Y.Ticks); bath.Move(c);
        Assert.Equal(before, (c.X.Ticks, c.Y.Ticks));
        var blocked = bath.PredationChance(c);
        bath.RocksEnabled = false;
        var stationaryWater = bath.PredationChance(c);
        Assert.Equal(stationaryWater * 3, blocked, 12);
        bath.RocksEnabled = true;
        Aim(c.LeftMuscle.Force, 0); Aim(c.RightMuscle.Force, 0); bath.Move(c);
        Assert.Equal(blocked, bath.PredationChance(c), 12);
        Aim(c.LeftMuscle.Force, 15); Aim(c.RightMuscle.Force, 15); c.Heading.Tick.Set(2048); bath.Move(c);
        Assert.True(c.Px < before.Item1 / 16.0);
        Assert.True(blocked > bath.PredationChance(c) * 10);
    }

    [Fact]
    public void StationaryRockHazardActuallyCullsAndConservesTheNativeEconomy()
    {
        // Calibrated seed: the first physical hazard lies between water and rock.
        var water = Fixed(969); var rock = Fixed(969); rock.RocksEnabled = true;
        foreach (var bath in new[] { water, rock })
        {
            bath.ExposureRisk = true; var c = bath.Creatures[0]; var site = bath.Rocks[0];
            c.X.Tick.Set((long)(site.X * 16)); c.Y.Tick.Set((long)(site.Y * 16));
            Aim(c.LeftMuscle.Force, 0); Aim(c.RightMuscle.Force, 0);
            Aim(c.LeftMuscle.NextTime, 10000); Aim(c.RightMuscle.NextTime, 10000);
            bath.Step(); bath.CheckConservation();
        }
        Assert.Single(water.Creatures); Assert.Equal(0, water.PredationDeaths);
        Assert.Empty(rock.Creatures); Assert.Equal(1, rock.PredationDeaths); Assert.Equal(0, rock.StarvationDeaths);
        Assert.True(rock.DeathSink.Value > 0); Assert.NotEmpty(Parts(rock.LastDeath));
    }

    [Fact]
    public void PhotosynthesisTransfersLightButRestingCostsMoreThanItCanHarvest()
    {
        var bath = Fixed(); var c = bath.Creatures[0];
        c.X.Tick.Set((long)(bath.LightX * 16)); c.Y.Tick.Set((long)(bath.LightY * 16));
        Aim(c.LeftMuscle.NextTime, 10000); Aim(c.RightMuscle.NextTime, 10000);
        Energy(bath, c, 600);
        bath.Step(8);
        Assert.True(c.Eaten.Value > 0); Assert.True(c.Eaten.Value <= Bath.MaximumHarvestPerPulse * 2);
        Assert.True(c.Energy.Value < 600);
        Assert.InRange(c.HarvestCarry.Value, 0UL, 999UL);
        Assert.True(ReadWitness.TryRead(Assert.IsType<Relation>(Parts(c.LastIntake)[3]), out _, out _, out _, out _, out _));
        bath.CheckConservation();
    }

    [Fact]
    public void EyeAndMuscleClocksRemainIndependentWithoutAnAuctionAtWakeup()
    {
        var bath = Fixed(3); var c = bath.Creatures[0];
        Aim(c.LeftEye.NextTime, 1); Aim(c.RightEye.NextTime, 3);
        Aim(c.LeftMuscle.NextTime, 4); Aim(c.RightMuscle.NextTime, 6);
        bath.Step();
        Assert.Equal(1UL, c.LeftEye.SampleTime.Value); Assert.Equal(0UL, c.RightEye.Presence.Value);
        bath.Step(3);
        Assert.Equal(4UL, c.LeftEye.SampleTime.Value); Assert.Equal(3UL, c.RightEye.SampleTime.Value);
        Assert.Equal(6UL, c.RightMuscle.NextTime.Value); Assert.Equal(15UL, c.LeftMuscle.NextTime.Value);
        Assert.Equal(0UL, bath.SpentAttention.Value); Assert.Equal(0, c.Searches);
    }

    [Fact]
    public void QuietComfortAndEarlyInspectionsCannotRewriteTheOperatingCircuit()
    {
        var bath = Fixed(); var c = bath.Creatures[0]; Energy(bath, c, 600);
        var original = c.LeftMuscle.Current;
        c.LeftMuscle.Wake(c, 1); c.RightMuscle.Wake(c, 2);
        c.Inspect(Creature.EvaluationTicks - 1, true);
        Assert.Equal(0UL, c.InspectionCount.Value);
        c.Inspect(Creature.EvaluationTicks, true);
        Assert.Same(original, c.LeftMuscle.Current);
        Assert.Equal(0, c.Pressure.Tension); Assert.Equal(0, c.Searches); Assert.Equal(0UL, bath.SpentAttention.Value);
        Assert.Equal((ulong)(Creature.EvaluationTicks * 2), c.NextInspection.Value);
    }

    [Theory]
    [InlineData(100, 440)]
    [InlineData(1100, 200)]
    public void HungerOrExcessFullnessAdmitsPressureAndOpensAPaidTopologySearch(int energy, int pressure)
    {
        var bath = Fixed(); var c = bath.Creatures[0]; Energy(bath, c, energy);
        c.LeftEye.Admit([5], 1); c.RightEye.Admit([7], 2);
        c.LeftMuscle.Wake(c, 3); c.RightMuscle.Wake(c, 4);
        var prior = c.LeftMuscle.Current;
        c.Inspect(Creature.EvaluationTicks, true);
        Assert.Equal(1UL, c.Pressure.Value); Assert.Equal(pressure, c.Pressure.Tension);
        Assert.NotSame(prior, c.LeftMuscle.Current);
        Assert.Equal(2, c.LeftMuscle.Current.Components.Length);
        Assert.Equal(1, c.Searches); Assert.Equal(1, c.Rewrites);
        Assert.True(bath.SpentAttention.Value > 0);
        Assert.True(TimeStepSequence.TryReadStep(c.LeftMuscle.LastRewrite, out _, out _, out _, out var before, out _, out var after, out _));
        Assert.Same(prior.Root, before); Assert.Same(c.LeftMuscle.Current.Root, after);
        var count = c.Rewrites; c.Inspect(Creature.EvaluationTicks + 1, true);
        Assert.Equal(count, c.Rewrites); bath.CheckConservation();
    }

    [Fact]
    public void ProposalsActuallyGrowShrinkAndRebindConnectedTermsWithoutRandomness()
    {
        var bath = Fixed(); var c = bath.Creatures[0]; var cursor = new Carrier(bath.Units.Count.UnitShape);
        var initial = c.LeftMuscle.Current;
        var next = CircuitMarket.Propose(bath.Units, initial, c.Sources, cursor, out var link);
        Assert.Equal(2, next.Components.Length);
        Assert.Same(initial.Root, ((Relation)Parts(link)[0]).Left);
        next = CircuitMarket.Propose(bath.Units, next, c.Sources, cursor, out _);
        next = CircuitMarket.Propose(bath.Units, next, c.Sources, cursor, out _);
        Assert.Equal(Circuit.MaximumComponents, next.Components.Length);
        next = CircuitMarket.Propose(bath.Units, next, c.Sources, cursor, out _);
        Assert.Equal(3, next.Components.Length);
        next = CircuitMarket.Propose(bath.Units, next, c.Sources, cursor, out _);
        var previousSources = next.Components.Select(t => t.Source).ToArray();
        next = CircuitMarket.Propose(bath.Units, next, c.Sources, cursor, out _);
        Assert.Contains(next.Components, t => !previousSources.Contains(t.Source));
    }

    [Fact]
    public void ReviewKeepsPriorComfortFrozenAndRecordsOneSharedOutcome()
    {
        var bath = Fixed(); var c = bath.Creatures[0]; Energy(bath, c, 100);
        c.LeftMuscle.Wake(c, 3);
        var pending = Parts(c.LeftMuscle.Pending);
        var before = (CarrierLocation)pending[1];
        var circuit = c.LeftMuscle.Current;
        Energy(bath, c, 300);
        c.LeftEye.Admit([15], 5); c.LeftMuscle.Review(c, 403);
        Assert.Equal(100, before.Ticks);
        Assert.Equal(1UL, circuit.Utility.Visits.Value);
        Assert.Equal(100, Held(circuit.Utility.Returns));
        Assert.Equal(Held(circuit.Utility.Returns), Held(circuit.Components[0].Utility.Returns));
        Assert.True(TimeStepSequence.TryReadStep(c.LeftMuscle.LastReview, out _, out var start, out var end, out _, out _, out _, out _));
        Assert.Equal(3, start.Ticks); Assert.Equal(403, end.Ticks);
        bath.CheckConservation();
    }

    [Fact]
    public void FrozenControlRecordsTrialsButRetainsItsTopologyAndCoefficients()
    {
        var bath = Fixed(); var c = bath.Creatures[0]; Energy(bath, c, 100);
        c.LeftMuscle.Wake(c, 2); var circuit = c.LeftMuscle.Current;
        var coefficients = circuit.Components.Select(t => Held(t.Coefficient)).ToArray();
        c.Inspect(Creature.EvaluationTicks, false);
        Assert.Same(circuit, c.LeftMuscle.Current); Assert.Equal(coefficients, circuit.Components.Select(t => Held(t.Coefficient)));
        Assert.Equal(1UL, circuit.Utility.Visits.Value); Assert.Equal(0, c.Searches);
    }

    [Fact]
    public void NativeMarketUsesMeasuredWorkPricesAndConsumesOnlyFundedQuotes()
    {
        var bath = Fixed(); var c = bath.Creatures[0];
        var cheap = new Circuit(bath.Units, new Component(bath.Units, c.Sources[0], 4));
        var costly = new Circuit(bath.Units, new Component(bath.Units, c.Sources[0], 12));
        cheap.Utility.Record(L(bath.Units.Return, 100), 400, System.Diagnostics.Stopwatch.Frequency / 10000);
        costly.Utility.Record(L(bath.Units.Return, 100), 400, System.Diagnostics.Stopwatch.Frequency / 100);
        var before = bath.SpentAttention.Value;
        var winner = CircuitMarket.Select(bath.Units, [costly, cheap], c.Wallet, bath.SpentAttention, 400, out var auction);
        Assert.Same(cheap, winner); Assert.Equal(before + 2, bath.SpentAttention.Value);
        Assert.NotEmpty(Parts((Relation)Parts(auction)[2]));
        Transfer(c.Wallet, bath.SpentAttention, (long)c.Wallet.Value);
        Assert.Null(CircuitMarket.Select(bath.Units, [cheap], c.Wallet, bath.SpentAttention, 401, out auction));
        Assert.Empty(Parts((Relation)Parts(auction)[1]));
        bath.CheckConservation();
    }

    [Fact]
    public void MoreObservedProcessingTimeCostsMoreEnergyWithTheSamePhysicalWorld()
    {
        var cheap = Fixed(work: 10); var costly = Fixed(work: 1000);
        cheap.Step(40); costly.Step(40);
        Assert.True(costly.ComputeCalories > cheap.ComputeCalories);
        Assert.True(costly.Creatures.Single().Energy.Value < cheap.Creatures.Single().Energy.Value);
        Assert.True(TryValue(Read(costly.Creatures.Single().Computation.Demand), out var calibrated));
        Assert.True(calibrated >= costly.ComputeCalories);
        cheap.CheckConservation(); costly.CheckConservation();
    }

    [Fact]
    public void MachineSaturationReducesBothYieldAndActualHarvestWithoutCreatingCalories()
    {
        var ample = Fixed(duty: 0); var scarce = Fixed(duty: 1);
        foreach (var bath in new[] { ample, scarce })
        {
            var c = bath.Creatures[0];
            c.X.Tick.Set((long)(bath.LightX * 16)); c.Y.Tick.Set((long)(bath.LightY * 16));
            bath.Step(4); bath.CheckConservation();
        }
        Assert.Equal(1000UL, ample.ResourceYield.Value); Assert.Equal(200UL, scarce.ResourceYield.Value);
        Assert.True(ample.TotalFood > scarce.TotalFood);
        Assert.Equal(ample.TotalCalories, scarce.TotalCalories);
        Assert.True(TryValue(Read(scarce.SupplyLaw), out var nativeYield)); Assert.Equal(200, nativeYield);
    }

    [Fact]
    public void DeathReallyCullsPopulationAndAnEmptyWorldRemainsRunnableAndConserved()
    {
        var bath = Fixed(); var dead = bath.Creatures[0];
        Energy(bath, dead, 0); bath.Step();
        Assert.Empty(bath.Creatures); Assert.Equal(1, bath.StarvationDeaths); Assert.Equal(0, bath.Births);
        bath.Step(1000);
        Assert.Equal(0, bath.MeanEnergy); Assert.Equal(0, bath.MeanExposure);
        Assert.Empty(Parts((Relation)Parts(bath.Root)[4])); bath.CheckConservation();
    }

    [Fact]
    public void MatingSelectsBothParentsComponentsRebindsChildPortsAndTransfersBirthEnergy()
    {
        var bath = Fixed(population: 2); var a = bath.Creatures[0]; var b = bath.Creatures[1];
        a.X.Tick.Set(1280); a.Y.Tick.Set(1280); b.X.Tick.Set(1440); b.Y.Tick.Set(1440);
        Aim(a.Age, Bath.MaturityTicks); Aim(b.Age, Bath.MaturityTicks);
        Energy(bath, a, 900); Energy(bath, b, 900);
        Aim(b.LeftMuscle.Current.Components[0].Coefficient, 12);
        b.LeftMuscle.Current.Components[0].Utility.Record(L(bath.Units.Return, 2000), 400, 0);
        a.RightMuscle.Current.Components[0].Utility.Record(L(bath.Units.Return, 3000), 400, 0);
        var beforeA = a.Energy.Value; var beforeB = b.Energy.Value;
        bath.Breed();
        var child = bath.Creatures[2];
        Assert.Equal(1, bath.Births); Assert.Equal(1UL, child.Generation.Value);
        Assert.Equal((ulong)(Bath.BirthContribution * 2), child.Energy.Value);
        Assert.Equal(beforeA - Bath.BirthContribution, a.Energy.Value); Assert.Equal(beforeB - Bath.BirthContribution, b.Energy.Value);
        Assert.Equal(12, Held(child.LeftMuscle.Current.Components[0].Coefficient));
        Assert.Equal(4, Held(child.RightMuscle.Current.Components[0].Coefficient));
        Assert.Same(child.Sources[0], child.LeftMuscle.Current.Components[0].Source);
        Assert.NotSame(b.LeftMuscle.Current.Components[0].Coefficient, child.LeftMuscle.Current.Components[0].Coefficient);
        Aim(b.LeftMuscle.Current.Components[0].Coefficient, -6);
        Assert.Equal(12, Held(child.LeftMuscle.Current.Components[0].Coefficient));
        Assert.NotEmpty(Parts(child.Inheritance)); Assert.True(bath.SpentAttention.Value > 0);
        bath.CheckConservation();
    }

    [Fact]
    public void MatingWaitsForMaturityDarknessAndTwoFundedParents()
    {
        var bath = Fixed(population: 2);
        foreach (var c in bath.Creatures) { c.X.Tick.Set(1280); c.Y.Tick.Set(1280); Energy(bath, c, 900); }
        bath.Breed(); Assert.Equal(0, bath.Births);
        foreach (var c in bath.Creatures) { Aim(c.Age, Bath.MaturityTicks); c.X.Tick.Set((long)(bath.LightX * 16)); c.Y.Tick.Set((long)(bath.LightY * 16)); }
        bath.Breed(); Assert.Equal(0, bath.Births);
        foreach (var c in bath.Creatures) { c.X.Tick.Set(1280); c.Y.Tick.Set(1280); Energy(bath, c, 100); }
        bath.Breed(); Assert.Equal(0, bath.Births); bath.CheckConservation();
    }

    [Fact]
    public void UnfundedOffspringMarketLeavesOpenEvidenceWithoutSpendingParentBirthEnergy()
    {
        var bath = Fixed(population: 2);
        foreach (var c in bath.Creatures) { Aim(c.Age, Bath.MaturityTicks); c.X.Tick.Set(1280); c.Y.Tick.Set(1280); Energy(bath, c, 900); }
        Transfer(bath.AttentionReserve, bath.SpentAttention, (long)bath.AttentionReserve.Value);
        var before = bath.Creatures.Select(c => c.Energy.Value).ToArray();
        bath.Breed();
        Assert.Equal(0, bath.Births); Assert.Equal(2, bath.Creatures.Length);
        Assert.Equal(before, bath.Creatures.Select(c => c.Energy.Value));
        Assert.NotEmpty(Parts((Relation)Parts(bath.LastBreeding)[1])); bath.CheckConservation();
    }

    [Fact]
    public void NumericArchiveRetainsCircuitMeaningAfterAllReaderHelpersAreGone()
    {
        var bath = Fixed(); var c = bath.Creatures[0]; c.LeftEye.Admit([9], 1);
        c.LeftMuscle.InstallInherited(new Circuit(bath.Units, new Component(bath.Units, c.Sources[1], -6)));
        var before = Read(c.LeftMuscle.Current.Output);
        var root = Assert.IsType<Relation>(GojoNumericFormat.Decode(c.ExportNativeGraph()).Single());
        var left = (Relation)Parts((Relation)Parts(root)[2])[0];
        var circuit = (Relation)Parts(left)[3];
        var output = (Relation)Parts(circuit)[1];
        Assert.True(TryValue(Read(output), out var restored));
        Assert.Equal(((CarrierLocation)before.Left).Ticks, restored);
        var component = (Relation)Parts((Relation)Parts(circuit)[0])[0];
        Assert.Equal(-6, Held((Carrier)Parts(component)[1]));
        var energy = (Carrier)Parts((Relation)Parts(root)[3])[0];
        Assert.Single(energy.AttachedRelations); Assert.NotSame(c.Energy, energy);
        var restoredTally = (Carrier)Parts((Relation)Parts(root)[3])[2];
        Assert.True(energy.UnitShape.Equals(restoredTally.UnitShape));
    }

    [Fact]
    public void FixedResourceObservationsReplayRegardlessOfRuntimeIdsOrBatchSize()
    {
        var a = Fixed(41, 4); var b = Fixed(41, 4);
        a.Step(1000); for (var i = 0; i < 1000; i++) b.Step();
        Assert.Equal(a.TotalFood, b.TotalFood); Assert.Equal(a.SpentAttention.Value, b.SpentAttention.Value);
        Assert.Equal(a.PredationDeaths, b.PredationDeaths); Assert.Equal(a.Rewrites, b.Rewrites);
        Assert.Equal(a.Creatures.Select(c => (c.X.Ticks, c.Y.Ticks, c.Heading.Ticks, c.Energy.Value, c.ComponentCount)),
            b.Creatures.Select(c => (c.X.Ticks, c.Y.Ticks, c.Heading.Ticks, c.Energy.Value, c.ComponentCount)));
        a.CheckConservation(); b.CheckConservation();
    }
}
