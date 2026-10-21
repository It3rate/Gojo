using Gojo.Core;
using Gojo.Encoding.Structures;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;
using Gojo.Experiments.QualitativeFrieze;
using C = Gojo.Experiments.QualitativeFrieze.OrnamentParticipationCount;

namespace QualitativeExecution.Tests;

public sealed class OrnamentParticipationCountTests
{
    [Fact]
    public void Exact_cardinality_is_an_explicit_bijection_and_not_a_scalar_property()
    {
        var first = Group(); var second = Group(); var support = S(first, second);
        var question = C.AllGroups(support, out var sourceWitness); var read = new C.Receipt(question, S(Group(), Group()));
        Assert.Equal("Yes", read.Answer); Assert.Equal(2, E(read.Correspondence).Length);
        Assert.Empty(E(read.UnmatchedExpected)); Assert.Empty(E(read.UnmatchedActual));
        var fewer = new C.Receipt(question, S(Group())); Assert.Equal("No", fewer.Answer); Assert.Single(E(fewer.UnmatchedExpected));
        var more = new C.Receipt(question, S(Group(), Group(), Group())); Assert.Equal("No", more.Answer); Assert.Single(E(more.UnmatchedActual));
        Assert.Contains(first, OrnamentReconstruction.Participants(sourceWitness));
        Assert.DoesNotContain(first, OrnamentReconstruction.Participants(question.Surface));
    }

    [Fact]
    public void Repeated_offers_of_the_same_participant_do_not_manufacture_multiplicity()
    {
        var one = Group(); var question = C.AllGroups(S(one, one), out _);
        Assert.Single(E(question.ExpectedRoles));
        var repeated = new C.Receipt(question, S(one, one, one));
        Assert.Equal("Yes", repeated.Answer); Assert.Single(E(repeated.DistinctParticipants));
        Assert.Equal("No", new C.Receipt(question, S(Group(), Group())).Answer);
    }

    [Fact]
    public void Closed_groups_match_fresh_closure_seams_and_reject_open_paths()
    {
        var question = C.ClosedGroups(S(Group(closed: true), Group(closed: false)), out _);
        Assert.Single(E(question.ExpectedRoles));
        var fresh = new C.Receipt(question, S(Group(closed: true), Group(closed: false), Group(closed: false)));
        Assert.Equal("Yes", fresh.Answer); Assert.Single(E(fresh.DistinctParticipants));
        Assert.Equal("No", new C.Receipt(question, S(Group(closed: false))).Answer);
        Assert.Equal("No", new C.Receipt(question, S(Group(closed: true), Group(closed: true))).Answer);
        Assert.Equal(4, E(question.Variables).Length);
    }

    [Fact]
    public void Open_support_and_uncertain_native_matches_never_establish_an_exact_count()
    {
        var question = C.AllGroups(S(Group()), out _);
        Assert.Equal("Open", new C.Receipt(question, StructuralConstraint.Open(Group())).Answer);
        var role = (Placeholder)E(question.Variables)[0];
        var undecided = new Relation(role, new Relation(new Placeholder(), new Placeholder()));
        var read = new C.Receipt(question, S(undecided));
        Assert.Equal("Open", read.Answer); Assert.Empty(E(read.Refuted));
        var uncommitted = C.AllGroups(StructuralConstraint.Open(Group()), out _);
        Assert.NotEmpty(E(uncommitted.Open));
        Assert.Equal("Open", new C.Receipt(uncommitted, S(Group(), Group())).Answer);
    }

    [Fact]
    public void Generic_patterns_count_actual_offered_matches_not_variable_bindings()
    {
        var left = new Placeholder(); var right = new Placeholder();
        var pattern = new Relation(left, right); var question = new C.Question(pattern, S(left, right), S(new Placeholder()));
        var actual = new Relation(new Placeholder(), new Placeholder()); var read = new C.Receipt(question, S(actual));
        Assert.Equal("Yes", read.Answer); Assert.Single(E(read.Correspondence));
        Assert.Same(actual, ((Relation)E(read.Correspondence)[0]).Right);
        Assert.True(StructuralConstraint.TryRead(read.Match, out _, out var matches, out _, out _));
        var candidate = Assert.IsType<Relation>(Assert.Single(E(matches)));
        Assert.True(StructuralConstraint.TryReadCandidate(candidate, out _, out _, out var bindings));
        Assert.Equal(2, E(bindings).Length);
    }

    [Fact]
    public void Fresh_expected_roles_and_closed_variable_scope_are_part_of_the_contract()
    {
        var role = new Placeholder(); var bad = new C.Question(role, S(role), S(role));
        Assert.False(bad.IsValid); Assert.Equal("Blocked", new C.Receipt(bad, S(new Placeholder())).Answer);
        Assert.False(new C.Question(role, S(role), S(role.Dual().Dual())).IsValid);
        var pending = new C.Question(role, StructuralConstraint.Open(role), S(new Placeholder()));
        Assert.Equal("Open", new C.Receipt(pending, S(new Placeholder())).Answer);
    }

    [Fact]
    public void Standalone_question_transport_retains_no_reference_groups_and_counts_an_independent_execution()
    {
        var reference = OrnamentReconstructionCases.Specs.First(spec => spec.Family == "LOZENGE-ROSETTE" && spec.Repeats == 1).Create();
        var question = C.ClosedGroups(reference.MechanicalGroups, out var witness);
        var imported = new C.Question(Restore(question.Surface));
        var target = reference.Replay(); var read = new C.Receipt(imported, target.MechanicalGroups);
        Assert.Equal("Yes", read.Answer); Assert.NotEmpty(E(read.Correspondence));
        Assert.DoesNotContain(reference.MechanicalGroups, OrnamentReconstruction.Participants(imported.Surface));
        foreach (var actual in E(reference.MechanicalGroups))
        {
            Assert.DoesNotContain(actual, OrnamentReconstruction.Participants(question.Surface));
            Assert.Contains(actual, OrnamentReconstruction.Participants(witness));
        }
    }

    private static Relation Group(bool closed = false)
    {
        var site = new Placeholder();
        return new(new Placeholder(), new Relation(S(new Placeholder()), closed ? S(new Relation(site, site)) : S()));
    }
    private static Relation S(params IElement[] items) => StructuralConstraint.Closed(items);
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
    private static Relation Restore(Relation source)
    {
        var json = GojoStructureCompactJson.Serialize(GojoStructureCompactJson.FromElement("participation-question", source));
        Assert.True(GojoStructureCompactJson.TryToElement(GojoStructureCompactJson.Deserialize(json), out var imported));
        return Assert.IsType<Relation>(imported.Root);
    }
}
