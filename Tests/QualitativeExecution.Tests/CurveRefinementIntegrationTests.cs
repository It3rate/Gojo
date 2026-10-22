using System.Numerics;
using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;
using Gojo.Experiments.QualitativeFrieze.Composition;
using Curve = Gojo.Experiments.QualitativeExecution.QualitativeCurve;
using Shapes = Gojo.Experiments.QualitativeExecution.CurveShapeQualities;
using Scenes = Gojo.Experiments.QualitativeFrieze.Composition.CurveEvidenceScene;
using Pack = Gojo.Experiments.QualitativeFrieze.Composition.CurveRefinementMessage;
using Stats = Gojo.Experiments.QualitativeExecution.StatisticalEvidence;

namespace QualitativeExecution.Tests;

public sealed class CurveRefinementIntegrationTests
{
    private static readonly Lazy<IReadOnlyList<CurveRefinementCorpus.Case>> Corpus = new(CurveRefinementCorpus.Cases);

    [Fact]
    public void Native_read_focus_keeps_actual_comparisons_after_diagnostic_wrappers_are_discarded()
    {
        var run = CurveRefinementExperiment.Calculate(Case("C018"));
        var retained = Reachable(run.Surface).ToHashSet(ReferenceEqualityComparer.Instance);
        Assert.All(run.BaseChecks.Concat(run.FullChecks), assessment => Assert.Contains(assessment.Surface, retained));
        Assert.All(run.FullChecks.SelectMany(assessment => assessment.Checks), check => Assert.Contains(check.Surface, retained));
        Assert.Contains(run.BaseState, retained); Assert.Contains(run.FullState, retained);
        Assert.Single(run.BaseDistribution.Events); Assert.Single(run.FullDistribution.Events);
    }

    [Fact]
    public void Closure_goal_and_default_factor_applications_are_native_context_not_source_truth()
    {
        var item = Case("C015"); var run = CurveRefinementExperiment.Calculate(item);
        var wanted = Assert.IsType<Relation>(Assert.Single(Entries(run.ClosureGoal)));
        Assert.Same(run.Context.Gap.From, wanted.Left); Assert.Same(run.Context.Gap.To, wanted.Right);
        Assert.Equal(run.Models.Count, Entries(run.Preferences).Length);
        var native = Reachable(run.Surface);
        Assert.Contains(run.Preferences, native); Assert.Contains(run.ClosureGoal, native);
        Assert.False(item.Source.Closed);
        Assert.Contains(run.Models, model => model.Scene.Closed);
    }

    [Fact]
    public void Public_profile_packet_contains_detached_bands_without_private_controls_or_quality_ancestry()
    {
        var source = Case("C018"); var packet = Pack.Publish(source);
        var exposed = Reachable(packet.Surface).ToHashSet(ReferenceEqualityComparer.Instance);
        Assert.DoesNotContain(source.Source.Surface, exposed); Assert.DoesNotContain(source.Source.Frame.Surface, exposed);
        Assert.DoesNotContain(source.Source.Frame.Reach, exposed);
        foreach (var read in source.Source.Reads)
        {
            Assert.DoesNotContain(read.Section, exposed); Assert.DoesNotContain(read.ControlWords, exposed);
            Assert.All(Entries(read.ControlWords), word => Assert.DoesNotContain(word, exposed));
            var quality = Shapes.Read(read, Shapes.SimilarityBasis(source.Source.Frame));
            Assert.DoesNotContain(quality.Surface, exposed);
            Assert.All(quality.Samples, sample => Assert.DoesNotContain(sample.Surface, exposed));
        }
        Assert.All(packet.Spans, report => Assert.All(report.Samples, sample =>
        {
            Assert.True(Pack.TryBand(sample, packet.Base.Frame, out var lower, out var upper, out var representative));
            Assert.True(lower.CompareTo(upper) < 0); Assert.True(lower.CompareTo(representative) <= 0);
            Assert.True(representative.CompareTo(upper) <= 0);
        }));
        Assert.NotSame(source.Source.Frame, packet.Base.Frame);
        Assert.True(packet.Base.Frame.Read(packet.Reference).Complete);
    }

    [Fact]
    public void Native_ratio_bands_refuse_a_foreign_reach_or_similarity_premise()
    {
        var packet = Pack.Publish(Case("C010")); var band = packet.Spans[0].Samples[0];
        var foreign = new Curve.Frame(new QualitativeMotionFrame());
        Assert.True(Pack.TryBand(band, packet.Base.Frame, out _, out _, out var representative));
        Assert.False(Pack.TryBand(band, foreign, out _, out _, out _));
        Assert.Null(Pack.InBand(band, representative, foreign));
        var candidate = Pack.Propose(packet, Pack.Ratio.Of(1, 2), Pack.Ratio.Of(1, 2));
        var read = Assert.Single(candidate.Reads);
        var foreignQuality = Shapes.Read(read, Shapes.SimilarityBasis(foreign));
        Assert.Null(foreignQuality.Strength);
        Assert.All(foreignQuality.Samples, sample => Assert.Null(Pack.InBand(band, sample.SignedExcursion, packet.Base.Frame)));
    }

    [Theory]
    [InlineData("C006")]
    [InlineData("C010")]
    [InlineData("C018")]
    public void Additional_profile_questions_rank_the_same_frozen_pool_and_preserve_informative_differences(string id)
    {
        var source = Case(id); var run = CurveRefinementExperiment.Calculate(source);
        Assert.Equal(run.Models.Count, run.BaseChecks.Count); Assert.Equal(run.Models.Count, run.FullChecks.Count);
        Assert.Equal(run.Models.Count, run.Prior.Rows.Count);
        Assert.Equal(run.Prior.Rows.Select(row => row.Model), run.BaseDistribution.Rows.Select(row => row.Model));
        Assert.Equal(run.Prior.Rows.Select(row => row.Model), run.FullDistribution.Rows.Select(row => row.Model));
        Assert.All(run.Models, model => Assert.Contains(run.Prior.Rows, row => ReferenceEquals(row.Model, model.Scene.Surface)));
        Assert.Single(run.BaseDistribution.Events); Assert.Single(run.FullDistribution.Events);
        // Correlated local reports form one compatibility event; the added
        // questions do not manufacture one independent sample per curve cue.
        Assert.True(run.FullChecks[0].Checks.Count > run.BaseChecks[0].Checks.Count);
        var legacy = Error(source.Source, run.Models[0].Scene, run.Packet);
        var selected = Error(source.Source, run.Models[run.FullSelected].Scene, run.Packet);
        Assert.True(selected.CompareTo(legacy) < 0 || run.FullChecks[run.FullSelected].Conflicts < run.FullChecks[0].Conflicts,
            $"{id} exposed neither a profile improvement nor fewer conflicts relative to the earlier short-handle receiver.");
        Assert.All(run.FullDistribution.Rows, row => Assert.True(row.Probability!.Value.Numerator > 0));
        Assert.Equal(new Stats.Ratio(1, 1), Sum(run.Prior.Rows.Select(row => row.Probability!.Value)));
        Assert.Equal(new Stats.Ratio(1, 1), Sum(run.FullDistribution.Rows.Select(row => row.Probability!.Value)));
    }

    [Fact]
    public void Entirely_withheld_source_information_keeps_the_prior_and_imports_no_observation()
    {
        var run = CurveRefinementExperiment.Calculate(Case("C003"));
        Assert.Empty(run.BaseDistribution.Events); Assert.Empty(run.FullDistribution.Events);
        Assert.Equal(run.Prior.Rows.Select(row => row.Probability), run.BaseDistribution.Rows.Select(row => row.Probability));
        Assert.Equal(run.Prior.Rows.Select(row => row.Probability), run.FullDistribution.Rows.Select(row => row.Probability));
        Assert.All(run.Packet.Spans, report =>
        {
            Assert.False(Closed(report.Entrance)); Assert.False(Closed(report.Exit)); Assert.False(Closed(report.Progress));
            Assert.All(report.Samples, band => Assert.False(Pack.TryBand(band, run.Packet.Base.Frame, out _, out _, out _)));
        });
        Assert.All(run.FullChecks, check => { Assert.Equal(0, check.Matches); Assert.Equal(0, check.Conflicts); Assert.True(check.Unresolved > 0); });
    }

    [Fact]
    public void Coarse_parent_orientation_remains_a_provisional_context_and_does_not_rewrite_the_public_observation()
    {
        var source = Case("C018"); var sections = source.Source.Sections.ToArray(); var root = source.Source.Surface;
        var packet = Pack.Publish(source);
        Assert.True(source.Source.Closed); Assert.Null(packet.Base.InteriorSide);
        var observedInterior = packet.Base.Interior;
        var open = CurveContextRead.Read(packet.Base, false, false, false);
        var proposed = CurveContextRead.Read(packet.Base, true, false, false);
        Assert.Null(open.ProposedInteriorSide); Assert.NotNull(proposed.ProposedInteriorSide);
        Assert.Same(observedInterior, packet.Base.Interior); Assert.Null(packet.Base.InteriorSide);
        Assert.Same(root, source.Source.Surface); Assert.Equal(sections, source.Source.Sections);
        Assert.Equal(proposed.ProposedInteriorSide, proposed.Coarse.InteriorSide);
        Assert.Contains(packet.Base.Surface, Reachable(proposed.InteriorPremise));
        var qualification = Assert.IsType<Relation>(proposed.InteriorPremise.Right);
        Assert.False(Closed(Assert.IsType<Relation>(qualification.Right)));
        // Closure and a provisional orientation can be used as premises without
        // claiming that the private curved boundary must be globally convex.
        Assert.All(packet.Base.Spans, span => Assert.DoesNotContain(span.Surface, Reachable(source.Source.Surface)));
    }

    [Fact]
    public void A_public_band_edit_can_change_the_receiver_without_any_source_handle_connection()
    {
        var source = Case("C025"); var original = Pack.Publish(source); var fuller = Pack.Publish(Case("C031"));
        var originalReport = Assert.Single(original.Spans); var otherReport = Assert.Single(fuller.Spans);
        var f = original.Base.Frame;
        Relation CopyBand(Relation band)
        {
            Assert.True(Pack.TryBand(band, fuller.Base.Frame, out var lower, out var upper, out var at));
            return new(new Relation(Pack.NativeRatio(f, lower), Pack.NativeRatio(f, upper)), Pack.NativeRatio(f, at));
        }
        var samples = otherReport.Samples.Select(CopyBand).ToArray();
        var reports = StructuralConstraint.Closed(samples.Select((band, index) => (IElement)new Relation(
            Pack.NativeRatio(f, Pack.Ratio.Of(index + 1, 4)), band)).ToArray());
        var changedReport = originalReport with
        {
            Samples = samples,
            Surface = new(originalReport.Span.Role, new Relation(new Relation(originalReport.Entrance, originalReport.Exit),
                new Relation(reports, originalReport.Progress)))
        };
        var changed = original with
        {
            Spans = new[] { changedReport },
            Surface = new(new Relation(original.Base.Surface, new Relation(original.Similarity, new Relation(original.Reference, original.HalfReference))),
                new Relation(StructuralConstraint.Closed(changedReport.Surface),
                    StructuralConstraint.Closed(original.Joins.Select(join => (IElement)join.Surface).ToArray())))
        };
        var before = Pack.Propose(original, Pack.Ratio.Of(5, 9), Pack.Ratio.Of(5, 9), adaptive: true);
        var after = Pack.Propose(changed, Pack.Ratio.Of(5, 9), Pack.Ratio.Of(5, 9), adaptive: true);
        Assert.NotEqual(Signature(before), Signature(after));
        Assert.Same(before.Reads[0].Start, after.Reads[0].Start); Assert.Same(before.Reads[0].End, after.Reads[0].End);
        var oldStrength = Shapes.Read(before.Reads[0], original.Similarity).Strength!;
        var newStrength = Shapes.Read(after.Reads[0], changed.Similarity).Strength!;
        Assert.True(newStrength.Numerator * oldStrength.Denominator > oldStrength.Numerator * newStrength.Denominator);
        var exposed = Reachable(changed.Surface);
        Assert.DoesNotContain(source.Source.Surface, exposed);
        Assert.All(source.Source.Reads, read => Assert.DoesNotContain(read.ControlWords, exposed));
        Assert.DoesNotContain(fuller.Base.Frame.Surface, exposed);
    }

    [Fact]
    public void Same_boundary_can_support_enclosure_and_centre_focus_without_becoming_new_observed_geometry()
    {
        var item = Case("C095"); var packet = Pack.Publish(item);
        var enclosure = CurveContextRead.Read(packet.Base, false, false, false, packet.Similarity);
        var ridge = CurveContextRead.Read(packet.Base, false, true, false, packet.Similarity);
        Assert.Equal(enclosure.Coarse.Sections.Count + 1, ridge.Coarse.Sections.Count);
        var centre = Assert.Single(Entries(ridge.Coarse.Assumed));
        Assert.Same(ridge.Coarse.Sections[^1], centre);
        Assert.DoesNotContain(centre, Reachable(packet.Surface));
        Assert.Equal(enclosure.Coarse.Reads.Select(read => (read.Start, read.End)), ridge.Coarse.Reads.Take(enclosure.Coarse.Sections.Count).Select(read => (read.Start, read.End)));
    }

    private static CurveRefinementCorpus.Case Case(string id) => Corpus.Value.Single(item => item.Id == id);
    private static string Signature(Scenes.Scene scene) => string.Join(";", scene.Reads.Select(read => string.Join("/", read.Controls.Select(value => $"{value.X},{value.Y}"))));
    private static Pack.Ratio Error(Scenes.Scene source, Scenes.Scene candidate, Pack.Packet packet)
    {
        var sourceBasis = Shapes.SimilarityBasis(source.Frame); var sum = Pack.Ratio.Of(0, 1); var count = 0;
        for (var i = 0; i < source.Reads.Count; i++)
        {
            var a = Shapes.Read(source.Reads[i], sourceBasis); var b = Shapes.Read(candidate.Reads[i], packet.Similarity);
            for (var j = 0; j < 3; j++)
            {
                if (a.Samples[j].SignedExcursion is not { } x || b.Samples[j].SignedExcursion is not { } y) continue;
                var difference = Pack.Ratio.Of(x.Numerator, x.Denominator) - Pack.Ratio.Of(y.Numerator, y.Denominator);
                sum += Pack.Ratio.Of(BigInteger.Abs(difference.N), difference.D); count++;
            }
        }
        Assert.True(count > 0); return sum / count;
    }
    private static Stats.Ratio Sum(IEnumerable<Stats.Ratio> ratios)
    {
        var sum = Pack.Ratio.Of(0, 1);
        foreach (var ratio in ratios) sum += Pack.Ratio.Of(ratio.Numerator, ratio.Denominator);
        return new(sum.N, sum.D);
    }
    private static bool Closed(Relation scope) => StructuralConstraint.TryReadScope(scope, out _, out var seam, out var tail) && seam.Equals(tail);
    private static IElement[] Entries(Relation scope)
    { Assert.True(StructuralConstraint.TryReadScope(scope, out var entries, out var seam, out var tail)); Assert.Equal(seam, tail); return entries; }
    private static IElement[] Reachable(IElement root)
    {
        var seen = new HashSet<IElement>(ReferenceEqualityComparer.Instance);
        void Visit(IElement item) { if (!seen.Add(item)) return; if (item is Relation relation) { Visit(relation.Left); Visit(relation.Right); } }
        Visit(root); return seen.ToArray();
    }
}
