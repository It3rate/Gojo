using Gojo.Core;
using Gojo.Encoding.Structures;
using Gojo.Experiments.QualitativeExecution;
using Gojo.Experiments.QualitativeFrieze;
using D = Gojo.Experiments.QualitativeFrieze.OrnamentDefaultsPolicy;
using G = Gojo.Experiments.QualitativeFrieze.OrnamentDefaultGeometry;

namespace QualitativeExecution.Tests;

public sealed class OrnamentDefaultProfileAuditTests
{
    [Fact]
    public void All_corpus_fact_profiles_survive_fresh_occurrence_execution_without_using_case_identity_as_truth()
    {
        var standard = new G.Standard(envelopeHalfSpan: 12);
        foreach (var item in OrnamentDefaultCases.All)
        {
            var source = item.Create(); var independent = source.Replay();
            var before = Profile(standard, G.Publication.FromSample(source));
            var after = Profile(standard, G.Publication.FromSample(independent));
            Assert.Equal(before, after);
            Assert.All(before, value => Assert.Contains(value, new[] { "Yes", "No", "Open" }));
            Assert.Equal("Open", before[5]); // no occupied-interior producer exists
            Assert.DoesNotContain(source.Observation.Surface,
                OrnamentReconstruction.Participants(G.Publication.FromSample(independent).Port));
        }
    }

    [Fact]
    public void Native_metric_ports_and_questions_preserve_all_seventy_two_profiles_after_separate_transport()
    {
        var standard = new G.Standard(aspectLimit: 5, thinLimit: 10, envelopeHalfSpan: 10);
        var importedQuestions = standard.Questions.Select(question =>
        {
            Assert.True(G.Question.TryRead("No original label is required", Restore(question.Surface), out var imported, out _));
            return imported;
        }).ToArray();
        foreach (var item in OrnamentDefaultCases.All)
        {
            var sample = item.Create(); var source = G.Publication.FromSample(sample);
            Assert.True(G.Publication.TryRead(Restore(source.Port), out var importedSource, out var witness));
            Assert.DoesNotContain(source.Port, OrnamentReconstruction.Participants(importedSource.Port));
            Assert.NotEmpty(OrnamentReconstruction.Participants(witness));
            for (var index = 0; index < importedQuestions.Length; index++)
                Assert.Equal(new G.Receipt(standard.Questions[index], source).Answer,
                    new G.Receipt(importedQuestions[index], importedSource).Answer);
        }
    }

    [Fact]
    public void Selected_symmetry_distinguishes_optional_tip_number_and_shape_equality_from_a_shared_closed_route()
    {
        var standard = new G.Standard();
        string Read(string id, G.Question question) => new G.Receipt(question,
            G.Publication.FromSample(OrnamentDefaultCases.Create(id))).Answer;
        Assert.Equal("Yes", Read("S01", standard.QuarterTurn));
        Assert.Equal("Yes", Read("S01", standard.HorizontalMirror));
        Assert.Equal("No", Read("S02", standard.QuarterTurn));
        Assert.Equal("Yes", Read("S02", standard.HorizontalMirror));
        Assert.Equal("No", Read("S04", standard.QuarterTurn));
        Assert.Equal("Yes", Read("S04", standard.HorizontalMirror));
        Assert.Equal("No", Read("S05", standard.QuarterTurn));
        Assert.Equal("Yes", Read("S06", standard.QuarterTurn));
        var four = OrnamentDefaultCases.Create("S01");
        var cycle = OrnamentArrangementRead.Crystallize("closed-route question leaves silhouette open", four.Observation, out _);
        foreach (var id in new[] { "S02", "S04", "S05", "S06" })
            Assert.Equal("Yes", new OrnamentArrangementRead.Receipt(cycle, OrnamentDefaultCases.Create(id).Observation).Answer);
        // Chiral lobes can have quarter-turn symmetry without reflection.
        Assert.Equal("Yes", Read("P01", standard.QuarterTurn));
        Assert.Equal("No", Read("P01", standard.HorizontalMirror));
        Assert.Equal("No", Read("P02", standard.QuarterTurn));
    }

    [Fact]
    public void Actual_geometric_counterexamples_are_preserved_as_defaults_and_exclude_only_under_explicit_requirement()
    {
        var standard = new G.Standard();
        var encounter = new D.Rule("ordinary encounters", standard.NoUnexpectedEncounters.Surface);
        var overlap = new D.Rule("no retracing", standard.NoCollinearOverlap.Surface);
        var aspect = new D.Rule("ordinary aspect", standard.ModerateAspect.Surface);
        var ordinary = new D.Policy(strongDefaults: [encounter, overlap], preferences: [aspect]);
        var strict = new D.Policy(required: [encounter, overlap], preferences: [aspect]);
        var violations = 0; var ordinaryAspect = 0;
        foreach (var item in OrnamentDefaultCases.All)
        {
            var source = G.Publication.FromSample(item.Create());
            var concrete = new[] { new G.Receipt(standard.NoUnexpectedEncounters, source),
                new G.Receipt(standard.NoCollinearOverlap, source), new G.Receipt(standard.ModerateAspect, source) };
            var rules = new[] { encounter, overlap, aspect };
            var facts = new D.Observation(source.Port, concrete.Select((read, index) =>
                D.Evidence.FromAnswer(rules[index].Predicate, read.Answer, read.Surface)).ToArray());
            var retained = new D.Receipt(ordinary, facts); var required = new D.Receipt(strict, facts);
            Assert.Equal("Yes", retained.Answer); Assert.True(retained.Eligible);
            var fails = concrete.Take(2).Count(read => read.Answer == "No");
            Assert.Equal((fails, fails), retained.StrongRange);
            Assert.Equal(fails == 0 ? "Yes" : "No", required.Answer);
            var waived = new D.Receipt(ordinary, facts, new(ordinary, [encounter, overlap, aspect]));
            Assert.Equal((0, 0), waived.StrongRange); Assert.Equal((0, 0), waived.PreferenceRange);
            Assert.Equal(concrete.Select(read => read.Answer), waived.Reads.Select(read => read.Fact!.Answer));
            if (fails > 0) violations++;
            if (concrete[2].Answer == "No") ordinaryAspect++;
        }
        Assert.InRange(violations, 8, 60);
        Assert.InRange(ordinaryAspect, 3, 30);
    }

    private static string[] Profile(G.Standard standard, G.Publication source) => standard.Questions
        .Select(question => new G.Receipt(question, source).Answer).ToArray();
    private static Relation Restore(Relation source)
    {
        var text = GojoStructureCompactJson.Serialize(GojoStructureCompactJson.FromElement("anonymous native fact publication", source));
        Assert.True(GojoStructureCompactJson.TryToElement(GojoStructureCompactJson.Deserialize(text), out var imported));
        return Assert.IsType<Relation>(imported.Root);
    }
}
