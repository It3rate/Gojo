using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;
using Gojo.Experiments.QualitativeFrieze.Composition;
using Band = Gojo.Experiments.QualitativeExecution.QualitativeBand;
using Corpus = Gojo.Experiments.QualitativeFrieze.Composition.OrnamentCompositionCorpus;
using Study = Gojo.Experiments.QualitativeFrieze.Composition.OrnamentCompositionExperiment;
using Parts = Gojo.Experiments.QualitativeFrieze.Composition.OrnamentBandComposition;

namespace QualitativeExecution.Tests;

public sealed class OrnamentCompositionIntegrationTests
{
    private static readonly Lazy<IReadOnlyList<Corpus.Design>> Samples = new(() => Corpus.Cases(2));
    private static readonly Lazy<IReadOnlyList<Corpus.Design>> Deeper = new(() => Corpus.Cases(9));
    private static IElement[] E(Relation scope)
    { Assert.True(StructuralConstraint.TryReadScope(scope, out var entries, out _, out _)); return entries; }

    [Fact]
    public void Larger_compounds_keep_native_repeats_curves_and_band_support()
    {
        foreach (var sample in Samples.Value)
        {
            var native = Parts.ReadTemplate(sample.Unit.Template.Frame, sample.Unit.Template.Surface);
            Assert.True(native.Complete); Assert.True(native.Envelopes.Count > 30);
            Assert.NotEmpty(sample.Unit.Applications); Assert.NotEmpty(sample.Unit.Repetitions);
            var bands = native.Envelopes.Select(e => Band.ReadEnvelope(native.Frame, e)).ToArray();
            Assert.Contains(bands, b => b.Occupied == true && b.Sweep.Collapsed == false);
            Assert.Contains(bands, b => b.Sweep.Spine.Straight == false);
            Assert.Contains(bands, b => b.Sweep.SideA.Straight == false);
            Assert.Contains(bands, b => b.Sweep.Collapsed == true && b.Occupied is null);
        }
    }

    [Fact]
    public void Role_reads_use_native_selected_descendants_not_diagnostic_arrays()
    {
        var unit = Samples.Value[0].Unit; var roles = Study.Roles(unit with { Roles = [] });
        Assert.Equal(4, roles.Length);
        var selected = roles.SelectMany(E).ToArray();
        Assert.Equal(unit.Template.Envelopes.Count, selected.Length);
        Assert.All(unit.Template.Envelopes, e => Assert.Single(selected, s => ReferenceEquals(e, s)));
    }

    [Fact]
    public void A_highlight_is_an_actual_complete_repeated_unit_subset()
    {
        var unit = Samples.Value[0].Unit; var selected = E(unit.Highlight);
        Assert.NotEmpty(selected); Assert.True(selected.Length < unit.Template.Envelopes.Count);
        Assert.All(selected, s => Assert.Contains(unit.Template.Envelopes, e => ReferenceEquals(s, e)));
        var roleEnvs = Study.Roles(unit).SelectMany(E);
        Assert.All(selected, s => Assert.Contains(roleEnvs, e => ReferenceEquals(s, e)));
    }

    [Fact]
    public void Current_seam_questions_follow_the_placed_descendants()
    {
        var unit = Samples.Value[0].Unit; Assert.NotEmpty(unit.Seams);
        foreach (var seam in unit.Seams)
        {
            var pair = Assert.IsType<Relation>(seam.Left);
            Assert.Contains(unit.Template.Envelopes, e => ReferenceEquals(e, pair.Left));
            Assert.Contains(unit.Template.Envelopes, e => ReferenceEquals(e, pair.Right));
            var read = Parts.Seam(unit.Template.Frame, Assert.IsType<Relation>(pair.Left), Assert.IsType<Relation>(pair.Right));
            Assert.True(read.Coincident); Assert.False(read.Joined);
        }
    }

    [Fact]
    public void Exposed_entry_and_exit_are_native_endpoint_and_side_selections()
    {
        var unit = Samples.Value[0].Unit;
        Assert.NotNull(unit.Entry); Assert.NotNull(unit.Exit);
        var details = (Relation)((Relation)((Relation)unit.Surface.Right).Right).Right;
        var tail = (Relation)details.Right;
        var ports = (Relation)((Relation)tail.Right).Left;
        var entry = Assert.IsType<Relation>(Assert.Single(E((Relation)ports.Left)));
        var exit = Assert.IsType<Relation>(Assert.Single(E((Relation)ports.Right)));
        Assert.Same(unit.Entry, entry.Left); Assert.Same(unit.Exit, exit.Left);
        var band = Band.ReadEnvelope(unit.Template.Frame, unit.Entry!);
        Assert.Same(band.Sweep.Spine.Start, ((Relation)entry.Right).Left);
    }

    [Fact]
    public void Colour_goals_and_observations_retain_open_recession_and_reject_flat_paint()
    {
        var unit = Samples.Value[0].Unit; var palette = Study.Palettes[0];
        var good = Study.AssessColours(unit, palette, false);
        Assert.True(good.Aggregate.Supported >= 2); Assert.Equal(1, good.Aggregate.Unresolved);
        var flat = palette with { Chain = palette.Ground, Motif = palette.Ground, Scaffold = palette.Ground, Accent = palette.Ground, Edge = palette.Ground };
        var bad = Study.AssessColours(unit, flat, false);
        Assert.Equal(3, bad.Aggregate.Contradicted); Assert.Equal(1, bad.Aggregate.Unresolved);
        Assert.NotSame(good.Checks[0].Frame, good.Checks[3].Frame);
        Assert.True(OrnamentColourIntent.TryRead(good.Checks[0].Frame, good.Checks[0].Intent, out var contrast));
        Assert.True(OrnamentColourIntent.TryRead(good.Checks[3].Frame, good.Checks[3].Intent, out var perception));
        Assert.NotSame(contrast.Subject, perception.Subject); Assert.NotSame(contrast.Reference, perception.Reference);
    }

    [Fact]
    public void Colour_focus_changes_paint_without_changing_native_shape()
    {
        var source = Samples.Value[0]; var native = source.Unit.Template.Surface;
        var a = Study.Render(source, Study.Palettes[0], 2); var b = Study.Render(source, Study.Palettes[7], 3);
        Assert.Equal(a.Paths.Select(p => p.Data), b.Paths.Select(p => p.Data));
        Assert.NotEqual(a.Paths.Select(p => p.Fill).ToArray(), b.Paths.Select(p => p.Fill).ToArray());
        Assert.Same(native, source.Unit.Template.Surface);
    }

    [Fact]
    public void Quatrefoil_valleys_retain_coincident_sides_and_corner_tension_without_welding()
    {
        var sample = Deeper.Value[3]; Assert.NotEmpty(sample.Unit.Seams);
        var reads = sample.Unit.Seams.Select(s => {
            var pair = Assert.IsType<Relation>(s.Left);
            return Parts.Seam(sample.Unit.Template.Frame, Assert.IsType<Relation>(pair.Left), Assert.IsType<Relation>(pair.Right));
        }).ToArray();
        Assert.Contains(reads, r => r.Coincident == true && r.SideAgreement == true && r.Tangent == false);
        Assert.All(reads, r => Assert.False(r.Joined));
    }

    [Fact]
    public void Basket_order_alternates_across_rows_and_columns_in_native_order_surfaces()
    {
        var sample = Deeper.Value[8]; var f = sample.Unit.Template.Frame;
        var orders = sample.Unit.Crossings.Select(c => Band.ReadOrder(f, c.Surface)).ToArray();
        Assert.Equal(6, orders.Length); Assert.All(orders, o => Assert.True(o.Complete));
        var horizontal = orders.Select(o => f.Read(o.Front!).StartVector.Y.IsZero).ToArray();
        Assert.Equal(new[] { true, false, true, false, true, false }, horizontal);
    }
}
