using Gojo.Core;
using Gojo.Encoding.Structures;
using Gojo.Experiments.QualitativeExecution;
using Gojo.Experiments.QualitativeFrieze;
using Gojo.Patterns;
using R = Gojo.Experiments.QualitativeFrieze.OrnamentReconstruction;

namespace QualitativeExecution.Tests;

public sealed class OrnamentReconstructionImportTests
{
    [Fact]
    public void All_baseline_recipes_generate_after_independent_native_transport_without_authoring_factories()
    {
        var questions = OrnamentReconstructionVocabulary.All.Select(entry => entry.Query).ToArray();
        foreach (var spec in OrnamentReconstructionCases.Specs.Where(spec => spec.Transform == 0 && spec.ReachRecipe == 0 && spec.Repeats == 1))
        {
            var source = spec.Create(); var json = GojoStructureCompactJson.Serialize(
                GojoStructureCompactJson.FromElement("standalone ornament recipe", source.Recipe));
            Assert.True(GojoStructureCompactJson.TryToElement(GojoStructureCompactJson.Deserialize(json), out var restored));
            var recipe = Assert.IsType<Relation>(restored.Root);
            Assert.True(OrnamentReconstructionCases.TryGenerate(recipe, out var generated, out var proof), spec.Id);
            Assert.NotNull(generated); Assert.Equal("IMPORTED", generated.Spec.Family);
            Assert.Throws<InvalidOperationException>(() => generated.Spec.Create());
            Assert.Same(recipe, ((Relation)proof.Left).Left);
            Assert.Same(generated.Surface, Assert.Single(E((Relation)((Relation)proof.Left).Right)));
            Assert.Empty(Open(proof)); Assert.Empty(Blocked(proof));
            Assert.Equal("Yes", new OrnamentReconstructionGraphRead(source.Observation, generated.Observation, capacity: 20000, includeRelations: true).Answer);
            var original = R.Participants(source.Surface);
            Assert.DoesNotContain(R.Participants(generated.Surface), original.Contains);
            Assert.DoesNotContain(R.Participants(proof), original.Contains);
            Assert.DoesNotContain(R.Participants(generated.MechanicalGroups), item => item is Carrier or CarrierLocation);
            var a = new R.Input("original output", source.Observation, source.MechanicalGroups);
            var b = new R.Input("generated from imported recipe", generated.Observation, generated.MechanicalGroups);
            var bank = new R.Bank([a, b], questions);
            Assert.All(questions, question => Assert.Equal(bank.Read(a, question).Answer, bank.Read(b, question).Answer));
        }
    }

    [Fact]
    public void An_open_connection_contract_stays_open_without_a_partial_execution()
    {
        var source = Sample("SQ-SIDES"); var contract = (Relation)source.Recipe.Right;
        var incomplete = new Relation(source.Program.Surface, new Relation(StructuralConstraint.Open(E((Relation)contract.Left)), contract.Right));
        Assert.False(OrnamentReconstructionCases.TryGenerate(incomplete, out var generated, out var proof));
        Assert.Null(generated); Assert.NotEmpty(Open(proof)); Assert.Empty(Blocked(proof));
        Assert.Empty(E((Relation)((Relation)proof.Left).Right));
        Assert.True(source.Publisher.IsComplete);
    }

    [Fact]
    public void Foreign_roles_and_malformed_closed_contracts_are_blocked_as_native_non_reads()
    {
        var source = Sample("SQ-SIDES"); var contract = (Relation)source.Recipe.Right;
        var role = ((Relation)E(source.Program.Ports)[0]).Left;
        var foreign = new Relation(source.Program.Surface, new Relation(S(new Relation(new Placeholder(), role)), contract.Right));
        var malformed = new Relation(source.Program.Surface, new Relation(new Relation(S(), new Placeholder()), contract.Right));
        foreach (var recipe in new[] { foreign, malformed })
        {
            Assert.False(OrnamentReconstructionCases.TryGenerate(recipe, out var generated, out var proof));
            Assert.Null(generated); Assert.Empty(Open(proof)); Assert.NotEmpty(Blocked(proof));
        }
    }

    [Fact]
    public void A_structurally_equal_path_copy_cannot_select_an_actual_crossing_chord()
    {
        var source = Sample("X-CROSS"); var contract = (Relation)source.Recipe.Right;
        var crossing = (Relation)E((Relation)contract.Right).Single(); var selector = (Relation)crossing.Left;
        var path = (Relation)selector.Left; var lookalike = new Relation(path.Left, path.Right);
        var altered = new Relation(new Relation(lookalike, selector.Right), crossing.Right);
        var recipe = new Relation(source.Program.Surface, new Relation(contract.Left, S(altered)));
        Assert.False(OrnamentReconstructionCases.TryGenerate(recipe, out var generated, out var proof));
        Assert.Null(generated); Assert.Empty(Open(proof)); Assert.NotEmpty(Blocked(proof));
    }

    [Fact]
    public void An_impossible_public_role_join_exposes_its_failed_local_admission()
    {
        var source = Sample("SQ-SIDES"); var ports = E(source.Program.Ports).Cast<Relation>().ToArray();
        // Different endpoints of the same nonzero side cannot be joined by
        // declaring that their actual public port identities should agree.
        var path = (Relation)E(source.Program.Paths)[0];
        var selected = ports.Where(port => port.Right is Relation pick && ReferenceEquals(pick.Left, path)).ToArray();
        var recipe = new Relation(source.Program.Surface, new Relation(S(new Relation(selected[0].Left, selected[^1].Left)), S()));
        Assert.False(OrnamentReconstructionCases.TryGenerate(recipe, out var generated, out var proof));
        Assert.Null(generated); Assert.Empty(Open(proof)); Assert.NotEmpty(Blocked(proof));
        Assert.True(source.Join.TryReadPassed(source.Join.Evaluate()));
    }

    private static OrnamentReconstructionCases.Sample Sample(string family) => OrnamentReconstructionCases.Specs.Single(spec =>
        spec.Family == family && spec.Transform == 0 && spec.ReachRecipe == 0 && spec.Repeats == 1).Create();
    private static IElement[] Open(Relation witness) => E((Relation)((Relation)((Relation)witness.Right).Right).Left);
    private static IElement[] Blocked(Relation witness) => E((Relation)((Relation)((Relation)witness.Right).Right).Right);
    private static Relation S(params IElement[] elements) => StructuralConstraint.Closed(elements);
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
}
