using Gojo.Core;
using Gojo.Encoding.Structures;
using Gojo.Experiments.QualitativeExecution;
using Gojo.Experiments.QualitativeFrieze;

namespace QualitativeExecution.Tests;

public sealed class OrnamentDefaultCaseTests
{
    [Fact]
    public void Every_counterfactual_executes_actual_native_paths_and_keeps_no_authoring_metrics_in_publication()
    {
        var cases = OrnamentDefaultCases.All;
        Assert.Equal(72, cases.Count);
        Assert.Equal(6, OrnamentDefaultCases.Families.Count);
        Assert.Equal(cases.Count, cases.Select(item => item.Id).Distinct().Count());
        Assert.All(cases.GroupBy(item => item.Family), group => Assert.Equal(12, group.Count()));
        foreach (var item in cases)
        {
            var sample = item.Create();
            Assert.True(sample.Publisher.IsComplete, item.Id);
            Assert.True(sample.Join.TryReadPassed(sample.Join.Evaluate()), item.Id);
            Assert.True(sample.Execution.Current.Execution.IsAtBoundary, item.Id);
            Assert.InRange(E(sample.Observation.Segments).Length, 3, 40);
            var publicGraph = OrnamentReconstruction.Participants(sample.Observation.Surface);
            Assert.DoesNotContain(publicGraph, element => element is Carrier or CarrierLocation);
            Assert.DoesNotContain(sample.Program.Surface, publicGraph);
            var memberEdges = E(sample.MechanicalGroups).Cast<Relation>()
                .SelectMany(group => E((Relation)((Relation)group.Right).Left)).ToArray();
            Assert.Equal(E(sample.Observation.Segments).Length, memberEdges.Length);
            Assert.All(E(sample.Observation.Segments), edge => Assert.Single(memberEdges, other => ReferenceEquals(edge, other)));
        }
    }

    [Fact]
    public void Saved_recipes_generate_independently_without_fixture_factory_and_preserve_strict_structure()
    {
        foreach (var item in OrnamentDefaultCases.All)
        {
            var original = item.Create();
            var json = GojoStructureCompactJson.Serialize(GojoStructureCompactJson.FromElement("standalone-default-case", original.Recipe));
            Assert.True(GojoStructureCompactJson.TryToElement(GojoStructureCompactJson.Deserialize(json), out var imported));
            Assert.True(OrnamentReconstructionCases.TryGenerate(Assert.IsType<Relation>(imported.Root), out var regenerated, out var witness), item.Id);
            Assert.NotNull(regenerated);
            Assert.Equal("Yes", new OrnamentReconstructionGraphRead(original.Observation, regenerated.Observation, 100000).Answer);
            Assert.Equal(E(original.MechanicalGroups).Length, E(regenerated.MechanicalGroups).Length);
            Assert.DoesNotContain(E(original.Observation.Sites), site => E(regenerated.Observation.Sites).Any(other => ReferenceEquals(site, other)));
            Assert.DoesNotContain(original.Program.Surface, OrnamentReconstruction.Participants(regenerated.Recipe));
            Assert.NotEmpty(OrnamentReconstruction.Participants(witness));
        }
    }

    [Fact]
    public void Defaults_have_real_optional_multiplicity_grouping_and_contact_counterexamples()
    {
        var four = OrnamentDefaultCases.Create("P01");
        var two = OrnamentDefaultCases.Create("P02");
        var six = OrnamentDefaultCases.Create("P03");
        Assert.Equal(5, E(four.MechanicalGroups).Length);
        Assert.Equal(3, E(two.MechanicalGroups).Length);
        Assert.Equal(7, E(six.MechanicalGroups).Length);
        var connected = OrnamentDefaultCases.Create("C06");
        var unjoined = OrnamentDefaultCases.Create("C07");
        Assert.Equal(4, E(connected.Observation.Sites).Length);
        Assert.Equal(8, E(unjoined.Observation.Sites).Length);
        Assert.Equal(4, E(connected.MechanicalGroups).Length);
        var declared = OrnamentDefaultCases.Create("L09");
        Assert.Single(E(declared.Observation.Crossings));
        var crossing = Assert.IsType<Relation>(E(declared.Observation.Crossings)[0]);
        Assert.True(declared.Observation.TryReadCrossing(crossing, out var first, out var second));
        Assert.True(declared.Observation.TryReadSegment(first, out _, out var a, out var b, out _));
        Assert.True(declared.Observation.TryReadSegment(second, out _, out var c, out var d, out _));
        Assert.DoesNotContain(a, new[] { c, d }); Assert.DoesNotContain(b, new[] { c, d });
    }

    [Fact]
    public void Family_cores_and_references_are_fresh_executable_native_units_without_truth_tables()
    {
        foreach (var family in OrnamentDefaultCases.Families)
        {
            Assert.Single(OrnamentDefaultCases.All, item => item.Family == family.Id && item.Id == family.ReferenceId);
            var first = family.Core(); var second = family.Core();
            Assert.True(first.Publisher.IsComplete);
            Assert.NotSame(first.Observation.Surface, second.Observation.Surface);
            Assert.Equal("Yes", new OrnamentReconstructionGraphRead(first.Observation, second.Observation, 100000).Answer);
            Assert.NotEmpty(E(first.Observation.Segments));
        }
    }

    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
}
