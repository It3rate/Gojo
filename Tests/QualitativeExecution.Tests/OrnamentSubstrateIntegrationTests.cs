using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeFrieze.Composition;
using Band = Gojo.Experiments.QualitativeExecution.QualitativeBand;
using Parts = Gojo.Experiments.QualitativeFrieze.Composition.OrnamentBandComposition;
using Corpus = Gojo.Experiments.QualitativeFrieze.Composition.OrnamentSubstrateCorpus;
using Study = Gojo.Experiments.QualitativeFrieze.Composition.OrnamentSubstrateExperiment;

namespace QualitativeExecution.Tests;

public sealed class OrnamentSubstrateIntegrationTests
{
    private static readonly Lazy<IReadOnlyList<Corpus.Design>> Samples = new(() => Corpus.Cases());
    private static IElement[] E(Relation scope)
    { Assert.True(StructuralConstraint.TryReadScope(scope, out var entries, out _, out _)); return entries; }

    [Fact]
    public void All_designs_have_native_substrates_and_complete_band_templates()
    {
        Assert.Equal(12, Samples.Value.Count);
        foreach (var d in Samples.Value)
        {
            var f = d.Ornament.Unit.Template.Frame;
            Assert.True(OrnamentSubstrate.Read(f, d.Lattice.Surface).Complete);
            Assert.True(Parts.ReadTemplate(f, d.Ornament.Unit.Template.Surface).Complete);
            Assert.Same(d.Ornament.Unit.Surface, d.Surface.Left);
            Assert.Same(d.Lattice.Surface, ((Relation)d.Surface.Right).Left);
        }
    }

    [Fact]
    public void A_whole_weave_strand_participates_in_many_distinct_repeat_events_without_being_split()
    {
        var d = Samples.Value[0]; var f = d.Ornament.Unit.Template.Frame; var t = d.Ornament.Unit.Template;
        Assert.Equal(10, t.Threads.Count); Assert.Equal(10, t.Envelopes.Count); Assert.Equal(25, d.Crosses.Count);
        Assert.All(t.Threads, thread => Assert.Single(E(thread)));
        Assert.All(d.Crosses, c => { var read = OrnamentSubstrate.ReadCrossEvent(f, c.Surface); Assert.True(read.Crosses); Assert.False(read.Joined); });
        var first = t.Envelopes[0];
        Assert.Equal(5, d.Crosses.Count(c => ((Relation)((Relation)c.Surface.Right).Left).Left == first));
        // Events are reachable from the selected route, not only diagnostic arrays.
        var routes = E((Relation)((Relation)((Relation)d.Surface.Right).Right).Left);
        Assert.Equal(10, routes.Length);
        Assert.All(routes, route => Assert.Equal(5, E((Relation)((Relation)((Relation)route).Right).Right).Length));
    }

    [Fact]
    public void Curve_threads_keep_shared_side_sites_and_consistent_coordinates_across_repeat_markers()
    {
        foreach (var index in new[] { 4, 5, 6, 7, 8, 9, 11 })
        {
            var d = Samples.Value[index]; var f = d.Ornament.Unit.Template.Frame;
            var seams = d.Ornament.Unit.Template.Threads.Where(t => E(t).Length > 1).SelectMany(t =>
                E(t).Cast<Relation>().Zip(E(t).Cast<Relation>().Skip(1), (a, b) => Parts.Seam(f, a, b))).ToArray();
            Assert.NotEmpty(seams);
            Assert.All(seams, s => { Assert.True(s.Joined); Assert.True(s.Coincident); Assert.True(s.SideAgreement); });
        }
    }

    [Fact]
    public void Endpoint_visits_stay_distinct_from_joined_landmarks()
    {
        var waves = Samples.Value[4]; var network = Samples.Value[8];
        Assert.All(waves.Visits, v => { Assert.True(v.AtStart == true || v.AtEnd == true); Assert.False(v.JoinedToLandmark); });
        Assert.All(network.Visits, v => { Assert.True(v.AtStart == true || v.AtEnd == true); Assert.True(v.JoinedToLandmark); });
    }

    [Fact]
    public void Paired_profiles_validate_full_curves_and_do_not_weld_opposite_boundaries()
    {
        foreach (var d in Samples.Value)
        {
            Assert.NotEmpty(d.Pairs);
            Assert.All(d.Pairs, p => { Assert.True(p.Compatible); Assert.False(p.Welded); });
        }
        Assert.Contains(Samples.Value[9].Pairs, p => Samples.Value[9].Ornament.Unit.Template.Frame.Read(p.Source).Controls.Length == 4);
    }

    [Fact]
    public void Curved_cells_close_by_actual_site_identity_and_reuse_neighbouring_edges()
    {
        foreach (var index in new[] { 3, 8, 9, 10, 11 })
        {
            var d = Samples.Value[index]; var f = d.Ornament.Unit.Template.Frame;
            Assert.NotEmpty(d.Cells); Assert.All(d.Cells, c => Assert.True(Study.ReadCell(f, c)));
            var contours = d.Cells.SelectMany(E).Cast<Relation>().Select(f.Read).ToArray();
            Assert.Contains(contours, a => contours.Any(b => !ReferenceEquals(a.Section, b.Section) &&
                ReferenceEquals(a.Start, b.End) && ReferenceEquals(a.End, b.Start) && a.Controls.SequenceEqual(b.Controls.Reverse())));
        }
    }

    [Fact]
    public void Colour_and_overlay_views_retain_a_visible_full_strand_and_repeat_events()
    {
        var d = Samples.Value[0]; var continuity = Study.RenderContinuity(d); var overlay = Study.RenderSubstrate(d);
        Assert.Single(continuity.Paths, p => p.Role == "continuous-selected-strand");
        Assert.Equal(5, continuity.Paths.Count(p => p.Role == "strand-repeat-event"));
        Assert.Equal(25, overlay.Paths.Count(p => p.Role == "repeat-landmark"));
        var design = Study.RenderDesign(d, OrnamentCompositionExperiment.Palettes[0]);
        Assert.Contains(design.Paths, p => p.Cuts is { Count: > 0 });
        Assert.Same(d.Ornament.Unit.Template.Surface, d.Ornament.Unit.Surface.Left);
    }

    [Fact]
    public void Repeated_curve_controls_consume_actual_shared_profiles_and_anchor_receipts()
    {
        var d = Samples.Value[9]; var f = d.Ornament.Unit.Template.Frame;
        var profileRoots = d.Generations.Select(g => ((Relation)g.Left).Left).Distinct(ReferenceEqualityComparer.Instance).ToArray();
        Assert.True(profileRoots.Length < d.Generations.Count);
        var nativeGenerations = (Relation)((Relation)((Relation)((Relation)((Relation)d.Surface.Right).Right).Right).Right).Right;
        Assert.Equal(d.Generations, E(nativeGenerations).Cast<Relation>());
        foreach (var generation in d.Generations)
        {
            var source = (Relation)generation.Left; var profile = (Relation)source.Left; var anchor = (Relation)source.Right;
            Assert.Same(f.Surface, profile.Left);
            var locals = E((Relation)((Relation)profile.Right).Left);
            var output = (Relation)generation.Right; var receipts = E((Relation)output.Left).Cast<Relation>().ToArray();
            var curve = f.Read((Relation)output.Right); var consumed = E(curve.ControlWords);
            Assert.Equal(locals.Length, receipts.Length);
            foreach (var (receipt, i) in receipts.Select((receipt, i) => (receipt, i)))
            {
                var inputs = (Relation)((Relation)receipt.Left).Right;
                Assert.Same(anchor.Right, inputs.Left); Assert.Same(locals[i], inputs.Right);
                Assert.True(f.Motion.TryReadResult(receipt, out var result, out _, out _)); Assert.Same(result, consumed[i]);
            }
        }
    }
}
