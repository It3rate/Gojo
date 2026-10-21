using Gojo.Core;
using Gojo.Experiments.QualitativeExecution;
using Gojo.Experiments.QualitativeFrieze;
using Gojo.Patterns;

namespace QualitativeExecution.Tests;

public sealed class OrnamentArrangementReadTests
{
    [Fact]
    public void Crystallized_route_contract_preserves_every_base_repertoire_arrangement_decision()
    {
        var samples = OrnamentReconstructionCases.Specs.Where(spec => spec.Transform == 0 && spec.ReachRecipe == 0 && spec.Repeats == 1)
            .Select(spec => spec.Create()).ToArray();
        Assert.Equal(26, samples.Length);
        foreach (var source in samples)
        {
            var original = new OrnamentArrangementRead.Question("fine template with route freedom", source.Observation);
            var compact = OrnamentArrangementRead.Crystallize("standalone route skeleton", source.Observation, out var correspondence);
            Assert.True(compact.IsValid);
            Assert.Equal("Yes", new OrnamentArrangementRead.Receipt(compact, source.Observation).Answer);
            Assert.Contains(source.Observation.Surface, OrnamentReconstruction.Participants(correspondence));
            Assert.DoesNotContain(source.Observation.Surface, OrnamentReconstruction.Participants(compact.Surface));
            foreach (var target in samples)
                Assert.Equal(new OrnamentArrangementRead.Receipt(original, target.Observation).Answer,
                    new OrnamentArrangementRead.Receipt(compact, target.Observation).Answer);
        }
    }

    [Fact]
    public void Compact_rosette_contract_keeps_five_anchors_and_twelve_routes_without_private_source_roles()
    {
        var source = OrnamentReconstructionCases.Specs.Single(spec => spec.Family == "LOZENGE-ROSETTE" &&
            spec.Transform == 0 && spec.ReachRecipe == 0 && spec.Repeats == 1).Create();
        var fine = new OrnamentArrangementRead.Question("original incidence", source.Observation);
        var compact = OrnamentArrangementRead.Crystallize("coarse correspondence", source.Observation, out _);
        var participants = OrnamentReconstruction.Participants(compact.Surface);
        Assert.Equal(5, E(compact.Template.Sites).Length);
        Assert.Equal(12, E(compact.Template.Segments).Length);
        Assert.True(participants.Count < OrnamentReconstruction.Participants(fine.Surface).Count);
        Assert.All(E(source.Observation.Sites), site => Assert.DoesNotContain(site, participants));
        Assert.All(E(source.Observation.Segments), edge => Assert.DoesNotContain(edge, participants));
        // These scopes are deliberately open/discarded. The topology question
        // cannot turn missing embedding information into a negative promise.
        Assert.False(compact.Template.IsComplete);
        Assert.True(compact.IsValid);
        Assert.Empty(E(compact.AtomicSegments));
        Assert.Equal(12, E(compact.PathSegments).Length);
    }

    [Fact]
    public void Resolution_covers_actual_source_and_target_segments_once_and_query_does_not_mutate()
    {
        var source = OrnamentReconstructionCases.Specs.Single(spec => spec.Family == "LOZENGE-ROSETTE" &&
            spec.Transform == 0 && spec.ReachRecipe == 0 && spec.Repeats == 1).Create();
        var target = source.Replay();
        var question = new OrnamentArrangementRead.Question("whole route participation", source.Observation);
        var retained = question.Surface;
        var receipt = new OrnamentArrangementRead.Receipt(question, target.Observation);
        Assert.Equal("Yes", receipt.Answer);
        Assert.Same(retained, question.Surface);
        Covered(receipt.SourceResolution, question.Template.Segments);
        Covered(receipt.TargetResolution, target.Observation.Segments);
        var correspondence = E(Assert.IsType<Relation>(receipt.Correspondence.Right));
        Assert.Equal(12, correspondence.Length);
        Assert.Equal(5, E(Assert.IsType<Relation>(receipt.Correspondence.Left)).Length);

        static void Covered(Relation resolution, Relation expected)
        {
            var details = Assert.IsType<Relation>(resolution.Right);
            var coverage = Assert.IsType<Relation>(details.Right);
            var rows = E(Assert.IsType<Relation>(coverage.Left)).Cast<Relation>().ToArray();
            Assert.Empty(E(Assert.IsType<Relation>(coverage.Right)));
            Assert.Equal(E(expected).Length, rows.Length);
            Assert.All(E(expected), segment => Assert.Single(rows, row => ReferenceEquals(row.Left, segment)));
            Assert.All(rows, row => Assert.Contains(row.Right, E(Assert.IsType<Relation>(details.Left))));
        }
    }

    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
}
