using Gojo.Core;
using Phototaxis;
using static Phototaxis.Native;

namespace Phototaxis.Tests;

public class CircuitInspectionTests
{
    private static Bath World() => new(population: 1, resources: MachineResources.Fixed())
        { ExposureRisk = false, Breeding = false };

    [Fact]
    public void ObservationsRetainSignedContributionsAndEarlierSourceSamplesWithoutTraining()
    {
        var bath = World(); var c = bath.Creatures[0]; c.LeftEye.Admit([9], 1);
        var component = new Component(bath.Units, c.Sources[1], -6);
        var circuit = new Circuit(bath.Units, component);
        var observation = CircuitInspection.Observe(circuit.Root);
        var fields = Parts(observation); var term = Parts((Relation)Parts((Relation)fields[1]).Single());
        Assert.Same(circuit.Root, fields[0]); Assert.Same(component.Root, term[0]);
        Assert.True(TryValue((Relation)term[1], out var input)); Assert.Equal(9, input);
        Assert.Equal(-6, ((CarrierLocation)term[2]).Ticks);
        Assert.True(TryValue((Relation)term[3], out var contribution)); Assert.Equal(-3, contribution);
        Assert.True(TryValue((Relation)fields[2], out var sum)); Assert.Equal(-3, sum);
        Assert.Equal(0UL, circuit.Utility.Visits.Value); Assert.Equal(0, c.Rewrites);
        c.LeftEye.Admit([15], 2); Aim(component.Coefficient, 6);
        var later = Parts(CircuitInspection.Observe(circuit.Root));
        Assert.True(TryValue((Relation)later[2], out var next)); Assert.Equal(6, next);
        Assert.True(TryValue((Relation)fields[2], out var retained)); Assert.Equal(-3, retained);
        Assert.Equal(-6, ((CarrierLocation)term[2]).Ticks);
        Assert.True(TryValue((Relation)term[1], out var prior)); Assert.Equal(9, prior);
    }

    [Fact]
    public void UnsampledInputsAndPartialCircuitsStayUnresolved()
    {
        var bath = World(); var c = bath.Creatures[0];
        var circuit = new Circuit(bath.Units, new Component(bath.Units, c.Sources[1], 6));
        var observation = Parts(CircuitInspection.Observe(circuit.Root));
        var term = Parts((Relation)Parts((Relation)observation[1]).Single());
        Assert.False(TryValue((Relation)term[1], out _)); Assert.False(TryValue((Relation)term[3], out _));
        Assert.False(TryValue((Relation)observation[2], out _)); Assert.Same(circuit.Root, observation[0]);
        var partial = G(new Placeholder());
        Assert.False(CircuitInspection.TryComponents(partial, out _, out _));
        var open = Parts(CircuitInspection.Observe(partial));
        Assert.Same(partial, open[0]); Assert.IsType<Placeholder>(open[1]);
    }

    [Fact]
    public void ProcessingSizeTracksGrowthCountsSharedIdentitiesOnceAndExcludesUtility()
    {
        var bath = World(); var c = bath.Creatures[0];
        var seed = c.LeftMuscle.Current;
        var a = new Component(bath.Units, c.Sources[1], 6);
        var b = new Component(bath.Units, c.Sources[1], -6);
        var larger = new Circuit(bath.Units, a, b);
        var simple = CircuitInspection.Size(seed.Root); var expanded = CircuitInspection.Size(larger.Root);
        Assert.True(expanded.Relations > simple.Relations); Assert.True(expanded.Carriers > simple.Carriers);
        Assert.Equal(expanded, CircuitInspection.Size(larger.Root, larger.Root));
        var singleA = CircuitInspection.Size(new Circuit(bath.Units, a).Root);
        var singleB = CircuitInspection.Size(new Circuit(bath.Units, b).Root);
        Assert.True(expanded.Carriers < singleA.Carriers + singleB.Carriers);
        a.Utility.Visits.Add(100); a.Utility.Work.Add(1000); larger.Utility.Visits.Add(50);
        Assert.Equal(expanded, CircuitInspection.Size(larger.Root));
        Assert.True(CircuitInspection.TryComponents(larger.Root, out var terms, out _)); Assert.Equal(2, terms.Length);
        Assert.True(CircuitInspection.Size(new Circuit(bath.Units, a).Root).Relations < expanded.Relations);
    }

    [Fact]
    public void ArchivedCircuitsRetainSourceRolesSizeAndWitnessedGrowth()
    {
        var bath = World(); var c = bath.Creatures[0]; c.LeftEye.Admit([9], 1);
        Assert.False(CircuitInspection.TryLastSizeChange(c.LeftMuscle.LastRewrite, out _, out _));
        c.LeftMuscle.Wake(c, 1); c.RightMuscle.Wake(c, 1);
        Transfer(c.Energy, bath.MetabolicSink, 620); c.Inspect(400, true);
        Assert.True(CircuitInspection.TryLastSizeChange(c.LeftMuscle.LastRewrite, out var before, out var after));
        Assert.Equal(1, before); Assert.Equal(2, after);
        var restored = (Relation)GojoNumericFormat.Decode(c.ExportNativeGraph()).Single(); var parts = Parts(restored);
        var muscle = (Relation)Parts((Relation)parts[2])[0]; var muscleParts = Parts(muscle);
        var circuit = (Relation)muscleParts[3];
        Assert.True(CircuitInspection.TryComponents(circuit, out var components, out _)); Assert.Equal(2, components.Length);
        Assert.Equal(0, CircuitInspection.SourceIndex((Relation)parts[4], Parts(components[0])[0]));
        Assert.Equal(1, CircuitInspection.SourceIndex((Relation)parts[4], Parts(components[1])[0]));
        Assert.Equal(CircuitInspection.Size(c.LeftMuscle.Current.Root), CircuitInspection.Size(circuit));
        Assert.True(CircuitInspection.TryLastSizeChange((Relation)muscleParts[10], out before, out after));
        Assert.Equal(1, before); Assert.Equal(2, after);
        Assert.True(TryValue((Relation)Parts(CircuitInspection.Observe(circuit))[2], out _));
        bath.CheckConservation();
    }
}
