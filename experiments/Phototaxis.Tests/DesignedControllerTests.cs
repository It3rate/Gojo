using Gojo.Core;
using Phototaxis;
using static Phototaxis.Native;

namespace Phototaxis.Tests;

public class DesignedControllerTests
{
    private static void Energy(Bath bath, Creature c, long desired)
    {
        var delta = desired - (long)c.Energy.Value;
        if (delta > 0) Transfer(bath.CalorieReserve, c.Energy, delta);
        else Transfer(c.Energy, bath.MetabolicSink, -delta);
    }

    [Fact]
    public void TimedLightsRepeatSeededSchedulesAndSwitchAtDeclaredBoundaries()
    {
        var field = new TimedLightField(7); Assert.Equal(field.Schedules, new TimedLightField(7).Schedules);
        Assert.NotEqual(field.Schedules, new TimedLightField(19).Schedules);
        foreach (var schedule in field.Schedules)
        {
            var start = schedule.PeriodTicks - schedule.PhaseTicks;
            Assert.Contains(field.At(start), p => p.X == schedule.X && p.Y == schedule.Y);
            Assert.Contains(field.At(start + schedule.OnTicks - 1), p => p.X == schedule.X && p.Y == schedule.Y);
            Assert.DoesNotContain(field.At(start + schedule.OnTicks), p => p.X == schedule.X && p.Y == schedule.Y);
            Assert.Contains(field.At(start + schedule.PeriodTicks), p => p.X == schedule.X && p.Y == schedule.Y);
        }
    }

    [Fact]
    public void AuthoredCircuitSteersTowardLightWhenHungryAndAwayWhenFull()
    {
        var design = DesignedControllers.Round(1).Single(d => d.Name == "R1-07");
        var bath = new Bath(population:1, resources:MachineResources.Fixed(.9), configureController:DesignedControllers.Builder(design));
        var c = bath.Creatures[0]; c.LeftEye.Admit([12], 1); c.RightEye.Admit([3], 1);
        Energy(bath, c, 400); c.LeftMuscle.Wake(c, 2); c.RightMuscle.Wake(c, 3);
        Assert.True(c.ReadMotion(out var forward, out var hunt)); Assert.True(forward > 0); Assert.True(hunt < 0);
        Energy(bath, c, 1100); c.LeftMuscle.Wake(c, 12); c.RightMuscle.Wake(c, 13);
        Assert.True(c.ReadMotion(out _, out var leave)); Assert.True(leave > 0);
        Assert.NotEqual(c.LeftMuscle.NextTime.Value, c.RightMuscle.NextTime.Value); bath.CheckConservation();
    }

    [Fact]
    public void HysteresisRetainsNativeModeUntilTheOppositeThresholdAndSurvivesTransport()
    {
        var d = DesignedControllers.Round(1).Single(d => d.Name == "R1-18");
        var bath = new Bath(population:1, resources:MachineResources.Fixed(.9), configureController:DesignedControllers.Builder(d));
        var c = bath.Creatures[0]; c.LeftEye.Admit([12], 1); c.RightEye.Admit([3], 1);
        var stock = (Carrier)Parts((Relation)Parts(c.ControllerState)[0])[0];
        Energy(bath, c, 800); c.AdvanceController(4); Assert.Equal(15UL, stock.Value);
        Energy(bath, c, 1100); c.AdvanceController(8); Assert.Equal(0UL, stock.Value);
        Energy(bath, c, 800); c.AdvanceController(12); Assert.Equal(0UL, stock.Value);
        var decoded = (Relation)GojoNumericFormat.Decode(c.ExportNativeGraph()).Single();
        var muscles = Parts((Relation)Parts(decoded)[2]);
        var decodedState = (Relation)muscles[3];
        var decodedStock = (Carrier)Parts((Relation)Parts(decodedState)[0])[0];
        Assert.Equal(0UL, decodedStock.Value);
        var leftOutput = (Relation)Parts((Relation)Parts((Relation)muscles[0])[3])[1];
        Assert.True(TryValue(ReferenceRead(leftOutput), out var away));
        Aim(decodedStock, 15); Assert.True(TryValue(ReferenceRead(leftOutput), out var toward)); Assert.True(away > toward);
        Energy(bath, c, 400); c.AdvanceController(16); Assert.Equal(15UL, stock.Value); bath.CheckConservation();
    }

    [Fact]
    public void UndefinedFeedbackPreservesStockAndAnInspectableFailedImport()
    {
        var units = new Units(); var stock = Limited(units.Feature, 15); Aim(stock, 15);
        var undefined = Divide(L(units.Feature, 9), L(units.Feature, 0), new Carrier(units.Feature.UnitShape));
        var cell = ControllerFeedback.Create(units, stock, undefined, 4, 0);
        var advanced = ControllerFeedback.Advance(cell, units, 0);
        Assert.Equal(15UL, stock.Value); Assert.Equal(4UL, ((Carrier)Parts(advanced)[4]).Value);
        var step = (Relation)Parts(advanced)[5];
        Assert.True(Gojo.Structures.TimeStepSequence.TryReadStep(step, out _, out _, out _, out _, out var import, out _, out _));
        Assert.False(TryValue((Relation)import, out _));
    }

    [Fact]
    public void DesignedContactOffspringHaveIndependentNativeMemoryAndSensorPorts()
    {
        var d = DesignedControllers.Round(1).Single(d => d.Name == "R1-18");
        var bath = new Bath(population:2, resources:MachineResources.Fixed(.9), pulsedLights:true,
            configureController:DesignedControllers.Builder(d)) { Learning=false, ExposureRisk=false };
        var a = bath.Creatures[0]; var b = bath.Creatures[1];
        foreach (var c in bath.Creatures)
        {
            Energy(bath,c,900); Aim(c.Age,Bath.MaturityTicks); c.X.Tick.Set(320); c.Y.Tick.Set(320);
        }
        bath.Breed(); Assert.Equal(1,bath.Births); var child = bath.Creatures[2];
        var parentMode = (Carrier)Parts((Relation)Parts(a.ControllerState)[0])[0];
        var childMode = (Carrier)Parts((Relation)Parts(child.ControllerState)[0])[0];
        Assert.False(parentMode.SameIdentityAs(childMode)); Assert.Equal(1UL,child.Generation.Value);
        child.LeftEye.Admit([15],1); child.RightEye.Admit([0],1);
        Assert.True(TryValue(ReadMany(child.LeftMuscle.Current.Output)[0],out var before));
        a.LeftEye.Admit([0],1); a.RightEye.Admit([15],1); Aim(parentMode,0);
        Assert.True(TryValue(ReadMany(child.LeftMuscle.Current.Output)[0],out var after)); Assert.Equal(before,after);
        bath.CheckConservation();
    }

    [Fact]
    public void DesignedTrialsRetainProgramsWithoutOpeningTopologySearch()
    {
        var design = DesignedControllers.Round(1)[18];
        var bath = new Bath(population:2, resources:MachineResources.Fixed(.9), pulsedLights:true,
            configureController:DesignedControllers.Builder(design)) { Learning=false, ExposureRisk=false, Breeding=false };
        var c = bath.Creatures[0]; var left = c.LeftMuscle.Current.Root; var right = c.RightMuscle.Current.Root;
        bath.Step(600); Assert.Same(left,c.LeftMuscle.Current.Root); Assert.Same(right,c.RightMuscle.Current.Root);
        Assert.Equal(0,c.Searches); Assert.Equal(0,bath.Rewrites); Assert.True(c.Computation.Runtime.Value > 0);
        Assert.True(ExpressionGraph.Count(c.Features, c.LeftMuscle.Current.Components.Select(t=>t.Source).ToArray()) > 0);
        bath.CheckConservation();
    }

    [Fact]
    public void DarkSearchPhaseWrapsAndBrightObservationsResetItsNativeStock()
    {
        var d = DesignedControllers.Round(3).Single(d => d.Name == "R3-16");
        var bath = new Bath(population:1, resources:MachineResources.Fixed(.9), configureController:DesignedControllers.Builder(d));
        var c = bath.Creatures[0]; c.LeftEye.Admit([0],1); c.RightEye.Admit([0],1);
        var cells = Parts(c.ControllerState); Assert.Equal(2,cells.Length);
        var phase = (Carrier)Parts((Relation)cells[1])[0];
        var left = c.LeftMuscle.Current.Root;
        for (var i=1; i<=20; i++) c.AdvanceController(i*4);
        Assert.Equal(0UL,phase.Value); c.AdvanceController(84); Assert.Equal(1UL,phase.Value);
        c.LeftEye.Admit([9],85); c.RightEye.Admit([6],85); c.AdvanceController(88);
        Assert.Equal(0UL,phase.Value); Assert.Same(left,c.LeftMuscle.Current.Root);
        Assert.True(phase.AttachedRelations.Count > 0);
    }
}
