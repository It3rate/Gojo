using Gojo.Core;
using Gojo.Encoding.Structures;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;
using Gojo.Experiments.QualitativeFrieze;

namespace QualitativeExecution.Tests;

public sealed class RuleTransferCaseTests
{
    [Fact]
    public void Corpus_executes_fifty_four_native_units_and_regenerates_each_without_its_domain_or_factory()
    {
        Assert.Equal(54, RuleTransferCases.All.Count);
        Assert.Equal(54, RuleTransferCases.All.Select(item => item.Id).Distinct().Count());
        Assert.All(RuleTransferCases.All.GroupBy(item => (item.Domain, item.Family)), group => Assert.Equal(6, group.Count()));
        foreach (var item in RuleTransferCases.All)
        {
            var source = item.Create();
            Assert.True(source.Publisher.IsComplete, item.Id);
            Assert.True(source.Join.TryReadPassed(source.Join.Evaluate()), item.Id);
            Assert.DoesNotContain(OrnamentReconstruction.Participants(source.Observation.Surface), element => element is Carrier or CarrierLocation);
            Assert.True(OrnamentReconstructionCases.TryGenerate(Restore(source.Recipe), out var generated, out _), item.Id);
            Assert.Equal("Yes", new OrnamentReconstructionGraphRead(source.Observation, generated!.Observation, 100000).Answer);
            Assert.DoesNotContain(source.Program.Surface, OrnamentReconstruction.Participants(generated.Recipe));
            Assert.Equal(E(source.MechanicalGroups).Length, E(generated.MechanicalGroups).Length);
        }
    }

    [Fact]
    public void The_same_source_free_arrangement_questions_transfer_to_different_domain_constructions_and_reject_actual_near_misses()
    {
        Check("O-C01", ["G-C01", "G-C02", "G-C05", "N-C01", "N-C05", "O-C06"], ["O-C04", "G-C03", "G-C04", "N-C03", "N-C06"]);
        Check("O-B01", ["O-B02", "O-B06", "G-B01", "G-B02", "G-B06", "N-B01", "N-B02", "N-B04"], ["O-B03", "O-B04", "G-B03", "G-B04", "N-B05"]);
        Check("O-R01", ["O-R02", "O-R03", "O-R06", "G-R01", "G-R02", "G-R04", "G-R06", "N-R01", "N-R02", "N-R04", "N-R06"], ["O-R04", "O-R05", "G-R03", "G-R05", "N-R03", "N-R05"]);

        static void Check(string source, string[] admitted, string[] refused)
        {
            var question = OrnamentArrangementRead.Crystallize("No domain label participates", RuleTransferCases.Create(source).Observation, out _);
            foreach (var id in admitted) Assert.Equal("Yes", new OrnamentArrangementRead.Receipt(question, RuleTransferCases.Create(id).Observation).Answer);
            foreach (var id in refused) Assert.Equal("No", new OrnamentArrangementRead.Receipt(question, RuleTransferCases.Create(id).Observation).Answer);
        }
    }

    [Fact]
    public void A_topological_course_match_keeps_the_distinction_between_declared_crossings_unexpected_contacts_and_retracing()
    {
        var standard = new OrnamentDefaultGeometry.Standard();
        string Encounters(string id) => new OrnamentDefaultGeometry.Receipt(standard.NoUnexpectedEncounters,
            OrnamentDefaultGeometry.Publication.FromSample(RuleTransferCases.Create(id))).Answer;
        Assert.Equal("Yes", Encounters("O-R02")); Assert.Equal("No", Encounters("O-R03"));
        Assert.Equal("Yes", Encounters("G-R01")); Assert.Equal("Yes", Encounters("N-R01"));
        Assert.Equal("No", Encounters("N-R04")); Assert.Equal("No", Encounters("O-R06")); Assert.Equal("No", Encounters("N-R06"));
        Assert.Single(E(RuleTransferCases.Create("N-R01").Observation.Crossings));
        Assert.Empty(E(RuleTransferCases.Create("G-R03").Observation.Crossings));
    }

    [Fact]
    public void Three_symbolic_programs_use_actual_shared_roles_and_explicit_join_contracts_for_independent_reaches()
    {
        var scenarios = RuleTransferCases.Scenarios(); Assert.Equal(3, scenarios.Count);
        Assert.Equal(new[] { 0, 2, 4 }, scenarios.Select(item => E(item.Template.Connections).Length));
        foreach (var scenario in scenarios)
        {
            Assert.Equal(2, E(scenario.Template.Parameters).Length);
            Assert.Contains(scenario.HorizontalRole, E(scenario.Template.Parameters));
            Assert.Contains(scenario.VerticalRole, E(scenario.Template.Parameters));
            foreach (var shared in new[] { true, false })
            {
                var horizontal = new Placeholder(); var vertical = shared ? horizontal : new Placeholder();
                var bindings = S(new Relation(scenario.HorizontalRole, horizontal), new Relation(scenario.VerticalRole, vertical));
                var samples = shared ? S(new Relation(horizontal, S(scenario.Reach, scenario.Reach))) :
                    S(new Relation(horizontal, S(scenario.Reach, scenario.Reach, scenario.Reach)), new Relation(vertical, S(scenario.Reach)));
                var hypothesis = scenario.Template.Hypothesis(bindings, samples);
                var output = scenario.Template.Realize(hypothesis);
                Assert.Equal("Yes", output.Answer); Assert.NotNull(output.Sample);
                Assert.Equal(2, E(output.RoleValues).Length);
                Assert.NotEmpty(E(output.RoleEvidence));
                Assert.Equal(shared ? 1 : 0, E(hypothesis.Equalities).Length);
                Assert.Empty(E(output.Open)); Assert.Empty(E(output.Blocked));
                Assert.True(OrnamentReconstructionCases.TryGenerate(Restore(output.Sample!.Recipe), out var independent, out _));
                Assert.Equal("Yes", new OrnamentReconstructionGraphRead(output.Sample.Observation, independent!.Observation, 100000).Answer);
            }
            var missing = scenario.Template.Hypothesis(S(new Relation(scenario.HorizontalRole, new Placeholder())), S());
            Assert.Equal("Open", scenario.Template.Realize(missing).Answer);
        }
    }

    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
    private static Relation S(params IElement[] entries) => StructuralConstraint.Closed(entries);
    private static Relation Restore(Relation surface)
    {
        var text = GojoStructureCompactJson.Serialize(GojoStructureCompactJson.FromElement("source-free native transfer fixture", surface));
        Assert.True(GojoStructureCompactJson.TryToElement(GojoStructureCompactJson.Deserialize(text), out var world));
        return Assert.IsType<Relation>(world.Root);
    }
}
