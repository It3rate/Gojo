using Gojo.Core;
using Gojo.Encoding.Structures;
using Gojo.Experiments.QualitativeFrieze;
using D = Gojo.Experiments.QualitativeFrieze.OrnamentDefaultsPolicy;
using G = Gojo.Experiments.QualitativeFrieze.OrnamentDefaultGeometry;

namespace QualitativeExecution.Tests;

public sealed class OrnamentDefaultsIntegrationTests
{
    [Fact]
    public void Transported_policy_facts_and_exact_exceptions_preserve_producer_queries_and_can_be_reassessed()
    {
        var standard = new G.Standard(envelopeHalfSpan: 8);
        var rules = standard.Questions.Select(question => new D.Rule("display label", question.Surface)).ToArray();
        var policy = new D.Policy(required: [rules[1]], strongDefaults: [rules[0], rules[2]],
            preferences: [rules[3]], unused: [rules[4], rules[5], rules[6], rules[7]]);
        var exception = new D.Directive(policy, [rules[0]]);
        foreach (var id in new[] { "C10", "P02", "L09", "T10", "M10", "S05" })
        {
            var sample = OrnamentDefaultCases.Create(id); var publication = G.Publication.FromSample(sample);
            var produced = standard.Questions.Select(question => new G.Receipt(question, publication)).ToArray();
            var observation = new D.Observation(publication.Port, produced.Select((read, index) =>
                D.Evidence.FromAnswer(rules[index].Predicate, read.Answer, read.Surface)).ToArray());
            var before = new D.Receipt(policy, observation, exception);
            var imported = Restore(new Relation(policy.Surface, new Relation(observation.Surface, exception.Surface)));
            var importedPolicy = new D.Policy(Assert.IsType<Relation>(imported.Left));
            var remainder = Assert.IsType<Relation>(imported.Right);
            var importedObservation = new D.Observation(Assert.IsType<Relation>(remainder.Left));
            var importedException = new D.Directive(Assert.IsType<Relation>(remainder.Right));
            var after = new D.Receipt(importedPolicy, importedObservation, importedException);
            Assert.Equal(before.Answer, after.Answer);
            Assert.Equal(before.StrongRange, after.StrongRange);
            Assert.Equal(before.PreferenceRange, after.PreferenceRange);
            Assert.Equal(before.Reads.Select(read => read.Answer), after.Reads.Select(read => read.Answer));
            Assert.True(G.Publication.TryRead(importedObservation.SourcePort, out var independentSource, out _));
            // Recompute the actual question from the independently restored
            // public port. Imported categorical facts are not the oracle here.
            foreach (var fact in importedObservation.Facts)
            {
                Assert.True(G.Question.TryRead("without original class metadata", fact.Predicate, out var question, out _));
                Assert.Equal(fact.Answer, new G.Receipt(question, independentSource).Answer);
            }
            Assert.DoesNotContain(sample.Program.Surface, OrnamentReconstruction.Participants(after.Surface));
            Assert.DoesNotContain(publication.Port, OrnamentReconstruction.Participants(after.Surface));
        }
    }

    private static Relation Restore(Relation source)
    {
        var text = GojoStructureCompactJson.Serialize(GojoStructureCompactJson.FromElement("native policy and exposed facts", source));
        Assert.True(GojoStructureCompactJson.TryToElement(GojoStructureCompactJson.Deserialize(text), out var imported));
        return Assert.IsType<Relation>(imported.Root);
    }
}
