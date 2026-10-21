using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;
using Gojo.Experiments.QualitativeFrieze;
using D = Gojo.Experiments.QualitativeFrieze.OrnamentDefaultsPolicy;
using G = Gojo.Experiments.QualitativeFrieze.OrnamentDefaultGeometry;

namespace QualitativeExecution.Tests;

public sealed class OrnamentDefaultsAuditTests
{
    [Fact]
    public void A_declared_encounter_permits_only_the_connected_crossing_without_changing_the_actual_contact_read()
    {
        var sample = OrnamentDefaultCases.Create("L09");
        var publication = G.Publication.FromSample(sample); var standard = new G.Standard();
        var declared = new G.Receipt(standard.NoUnexpectedEncounters, publication);
        Assert.Equal("Yes", declared.Answer);
        // Withdraw just the crossing assertion, keeping actual sites, edges,
        // native positions and groups. Its geometry remains the same.
        var noDeclaration = new QualitativeRecognitionObservation(sample.Observation.Frame,
            sample.Observation.Sites, sample.Observation.Segments, sample.Observation.Continuations, StructuralConstraint.Closed());
        var unexplained = new G.Publication(sample.Frame, noDeclaration, publication.Bindings, sample.MechanicalGroups);
        var assessed = new G.Receipt(standard.NoUnexpectedEncounters, unexplained);
        Assert.Equal("No", assessed.Answer);
        Assert.Equal("Yes", new G.Receipt(standard.NoCollinearOverlap, unexplained).Answer);
        var rule = new D.Rule("encounters need explanation", standard.NoUnexpectedEncounters.Surface);
        var policy = new D.Policy(strongDefaults: [rule]);
        var evidence = D.Evidence.FromAnswer(rule.Predicate, assessed.Answer, assessed.Surface);
        var facts = new D.Observation(unexplained.Port, [evidence]);
        var ordinary = new D.Receipt(policy, facts);
        var exception = new D.Receipt(policy, facts, new(policy, [rule]));
        Assert.True(ordinary.Eligible); Assert.Equal((1, 1), ordinary.StrongRange);
        Assert.True(exception.Eligible); Assert.Equal((0, 0), exception.StrongRange);
        Assert.Equal("No", exception.Reads.Single().Fact!.Answer);
        Assert.Contains(assessed.Surface, OrnamentReconstruction.Participants(exception.Surface));
    }

    [Fact]
    public void A_thin_variant_can_retain_the_same_required_arrangement_while_a_default_asks_for_a_different_realization()
    {
        var reference = OrnamentDefaultCases.Create("C01"); var thin = OrnamentDefaultCases.Create("C10");
        var question = OrnamentArrangementRead.Crystallize("closed route", reference.Observation, out _);
        var arrangement = new OrnamentArrangementRead.Receipt(question, thin.Observation);
        Assert.Equal("Yes", arrangement.Answer);
        var publication = G.Publication.FromSample(thin); var standard = new G.Standard();
        var appearance = new G.Receipt(standard.ModerateAspect, publication);
        Assert.Equal("No", appearance.Answer);
        var required = new D.Rule("whole route arrangement", question.Surface);
        var ordinary = new D.Rule("ordinary aspect", standard.ModerateAspect.Surface);
        var evidence = new D.Observation(publication.Port, [
            D.Evidence.FromAnswer(required.Predicate, arrangement.Answer, arrangement.Surface),
            D.Evidence.FromAnswer(ordinary.Predicate, appearance.Answer, appearance.Surface)]);
        var policy = new D.Policy(required: [required], preferences: [ordinary]);
        var receipt = new D.Receipt(policy, evidence);
        Assert.Equal("Yes", receipt.Answer); Assert.Equal((1, 1), receipt.PreferenceRange);
        Assert.Equal("Yes", new D.Receipt(policy, evidence, new(policy, [ordinary])).Answer);
        Assert.Equal("No", new D.Receipt(new(required: [required, ordinary]), evidence).Answer);
    }

    [Fact]
    public void A_missing_fill_service_cannot_become_a_proven_nonoverlap_requirement_from_closed_contours_alone()
    {
        var sample = OrnamentDefaultCases.Create("C08"); var publication = G.Publication.FromSample(sample);
        var standard = new G.Standard(); var read = new G.Receipt(standard.DisjointFilledInteriors, publication);
        Assert.Equal("Open", read.Answer);
        var requirement = new D.Rule("disjoint occupied interiors", standard.DisjointFilledInteriors.Surface);
        var observation = new D.Observation(publication.Port,
            [D.Evidence.FromAnswer(requirement.Predicate, read.Answer, read.Surface)]);
        var active = new D.Receipt(new(required: [requirement]), observation);
        var carried = new D.Receipt(new(unused: [requirement]), observation);
        Assert.Equal("Open", active.Answer); Assert.True(active.Eligible);
        Assert.Equal("Yes", carried.Answer); Assert.Equal("Unused", Assert.Single(carried.Reads).Answer);
        Assert.Contains(read.Surface, OrnamentReconstruction.Participants(carried.Surface));
    }
}
