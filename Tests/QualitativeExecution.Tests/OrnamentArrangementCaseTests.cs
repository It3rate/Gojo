using Gojo.Core;
using Gojo.Encoding.Structures;
using Gojo.Experiments.QualitativeExecution;
using Gojo.Experiments.QualitativeFrieze;
using R = Gojo.Experiments.QualitativeFrieze.OrnamentReconstruction;

namespace QualitativeExecution.Tests;

public sealed class OrnamentArrangementCaseTests
{
    [Fact]
    public void Alternatives_and_controls_execute_native_recipes_without_publishing_authoring_or_measurement()
    {
        Assert.Equal(38, OrnamentArrangementCases.All.Count);
        Assert.Equal(38, OrnamentArrangementCases.All.Select(item => item.Id).Distinct().Count());
        foreach (var item in OrnamentArrangementCases.All)
        {
            var sample = item.Create();
            Assert.True(sample.Publisher.IsComplete, item.Id);
            Assert.True(sample.Join.TryReadPassed(sample.Join.Evaluate()), item.Id);
            Assert.True(sample.Execution.Current.Execution.IsAtBoundary, item.Id);
            Assert.InRange(E(sample.Observation.Segments).Length, 4, 48);
            var publicWorld = R.Participants(sample.Observation.Surface);
            Assert.DoesNotContain(publicWorld, element => element is Carrier or CarrierLocation);
            Assert.DoesNotContain(sample.Program.Surface, publicWorld);
            Assert.DoesNotContain(sample.Recipe, publicWorld);
            var memberEdges = E(sample.MechanicalGroups).Cast<Relation>()
                .SelectMany(group => E((Relation)((Relation)group.Right).Left)).ToArray();
            Assert.Equal(E(sample.Observation.Segments).Length, memberEdges.Length);
            Assert.All(E(sample.Observation.Segments), edge => Assert.Single(memberEdges, member => ReferenceEquals(member, edge)));
        }
    }

    [Fact]
    public void Reach_and_shoulder_alternatives_preserve_all_legacy_answers_but_retain_distinct_native_words()
    {
        var samples = OrnamentArrangementCases.All.Take(8).Select(item => item.Create()).ToArray();
        var inputs = samples.Select((sample, index) => new R.Input("opaque alternative " + index,
            sample.Observation, sample.MechanicalGroups)).ToArray();
        var queries = OrnamentReconstructionVocabulary.All.Select(item => item.Query).ToArray();
        var bank = new R.Bank(inputs, queries);
        var reference = queries.Select(query => bank.Read(inputs[0], query).Answer).ToArray();
        Assert.DoesNotContain(reference, answer => answer is "Open" or "Blocked");
        for (var index = 1; index < samples.Length; index++)
        {
            for (var queryIndex = 0; queryIndex < queries.Length; queryIndex++)
                Assert.True(reference[queryIndex] == bank.Read(inputs[index], queries[queryIndex]).Answer,
                    $"{samples[index].Spec.Id}: {queries[queryIndex].Name}; expected {reference[queryIndex]}, actual {bank.Read(inputs[index], queries[queryIndex]).Answer}");
            Assert.NotSame(samples[0].Recipe, samples[index].Recipe);
            Assert.Equal("Yes", new OrnamentReconstructionGraphRead(samples[0].Observation,
                samples[index].Observation, capacity: 20000, includeRelations: true).Answer);
        }
        // Native reach participation, rather than a display-only scale field,
        // distinguishes the elongated construction from the reference.
        Assert.NotEqual(WordParticipation(samples[0]), WordParticipation(samples[1]));
    }

    [Fact]
    public void Route_elaborations_keep_branch_incidence_while_mechanical_regroupings_remain_inspectable()
    {
        var expected = BranchDegrees(OrnamentArrangementCases.Create("R01"));
        Assert.Equal(new[] { 4, 4, 4, 4, 8 }, expected);
        foreach (var id in new[] { "R11", "R12", "R13", "R14", "R15", "R16", "R17", "R18", "R19", "R20", "R21", "R22", "R37", "R38" })
            Assert.Equal(expected, BranchDegrees(OrnamentArrangementCases.Create(id)));
        var whole = OrnamentArrangementCases.Create("R01");
        var splitPetals = OrnamentArrangementCases.Create("R21");
        var splitRim = OrnamentArrangementCases.Create("R22");
        Assert.Equal(5, E(whole.MechanicalGroups).Length);
        Assert.Equal(17, E(splitPetals.MechanicalGroups).Length);
        Assert.Equal(8, E(splitRim.MechanicalGroups).Length);
        Assert.Equal("Yes", new OrnamentReconstructionGraphRead(whole.Observation, splitPetals.Observation,
            capacity: 20000, includeRelations: true).Answer);
        Assert.Equal("Yes", new OrnamentReconstructionGraphRead(whole.Observation, splitRim.Observation,
            capacity: 20000, includeRelations: true).Answer);
    }

    [Fact]
    public void Controls_change_actual_scope_or_connections_instead_of_only_their_descriptions()
    {
        var reference = OrnamentArrangementCases.Create("R01");
        foreach (var id in new[] { "R23", "R24", "R25", "R26", "R27", "R28", "R29", "R30", "R31", "R32", "R33", "R34", "R35", "R36" })
        {
            var control = OrnamentArrangementCases.Create(id);
            Assert.Equal("No", new OrnamentReconstructionGraphRead(reference.Observation, control.Observation,
                capacity: 20000, includeRelations: true).Answer);
        }
        Assert.Equal(10, BranchDegrees(OrnamentArrangementCases.Create("R26")).Max());
    }

    [Fact]
    public void Every_alternative_and_control_survives_standalone_recipe_transport_and_native_generation()
    {
        foreach (var item in OrnamentArrangementCases.All)
        {
            var source = item.Create();
            var saved = GojoStructureCompactJson.Serialize(GojoStructureCompactJson.FromElement("anonymous native recipe", source.Recipe));
            Assert.True(GojoStructureCompactJson.TryToElement(GojoStructureCompactJson.Deserialize(saved), out var imported));
            var recipe = Assert.IsType<Relation>(imported.Root);
            Assert.True(OrnamentReconstructionCases.TryGenerate(recipe, out var output, out var proof), item.Id);
            Assert.NotNull(output);
            Assert.Equal("Yes", new OrnamentReconstructionGraphRead(source.Observation, output.Observation,
                capacity: 20000, includeRelations: true).Answer);
            Assert.Equal(E(source.MechanicalGroups).Length, E(output.MechanicalGroups).Length);
            Assert.DoesNotContain(R.Participants(output.Surface), R.Participants(source.Surface).Contains);
            Assert.Same(recipe, ((Relation)proof.Left).Left);
        }
    }

    private static int[] BranchDegrees(OrnamentReconstructionCases.Sample sample) => E(sample.Observation.Sites).Select(site =>
        E(sample.Observation.Segments).Cast<Relation>().Sum(edge =>
        {
            sample.Observation.TryReadSegment(edge, out _, out var from, out var to, out _);
            return (ReferenceEquals(site, from) ? 1 : 0) + (ReferenceEquals(site, to) ? 1 : 0);
        })).Where(degree => degree > 2).Order().ToArray();

    private static int WordParticipation(OrnamentReconstructionCases.Sample sample) => E(sample.Program.Paths).Cast<Relation>()
        .Sum(path => E((Relation)path.Left).Length + E((Relation)path.Right).Cast<Relation>().Sum(word => E(word).Length));

    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
}
