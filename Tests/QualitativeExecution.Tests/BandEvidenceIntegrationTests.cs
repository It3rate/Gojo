using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;
using Gojo.Experiments.QualitativeFrieze.Composition;
using Curve = Gojo.Experiments.QualitativeExecution.QualitativeCurve;
using Band = Gojo.Experiments.QualitativeExecution.QualitativeBand;

namespace QualitativeExecution.Tests;

public sealed class BandEvidenceIntegrationTests
{
    private static readonly Lazy<IReadOnlyList<BandEvidenceCorpus.Case>> Corpus = new(BandEvidenceCorpus.Cases);
    private static BandEvidenceCorpus.Case Sample(string id) => Corpus.Value.Single(c => c.Id == id);
    private static Relation S(params IElement[] entries) => StructuralConstraint.Closed(entries);
    private static IElement[] E(Relation scope)
    { Assert.True(StructuralConstraint.TryReadScope(scope, out var entries, out _, out _)); return entries; }
    private static BandEvidenceCorpus.Layer Layer(Curve.Frame f, IElement[] envelopes, IElement[]? crossings = null, IElement[]? orders = null) =>
        new(f, [], [], [], new(new Relation(f.Surface, S(new Relation(S(), S(envelopes)))),
            new Relation(new Relation(S(crossings ?? []), S(orders ?? [])), S())));

    [Fact]
    public void Width_rungs_preserve_exact_spine_identity_and_keep_unknown_width_open()
    {
        Assert.Equal(22, Corpus.Value.Count);
        foreach (var source in Corpus.Value)
        {
            var a = BandEvidenceExperiment.Inspect(source.Narrow); var b = BandEvidenceExperiment.Inspect(source.Broad);
            Assert.True(BandEvidenceExperiment.SameSpines(a, b));
            Assert.NotEmpty(b.Threads);
            foreach (var e in b.Threads.SelectMany(t => t.Envelopes))
                Assert.Equal(source.Id != "W020", e.Sweep.Complete);
        }
    }

    [Fact]
    public void Threefold_side_word_participation_changes_boundaries_without_changing_spine()
    {
        var source = Sample("W001");
        var a = Assert.Single(Assert.Single(BandEvidenceExperiment.Inspect(source.Narrow).Threads).Envelopes).Sweep;
        var b = Assert.Single(Assert.Single(BandEvidenceExperiment.Inspect(source.Broad).Threads).Envelopes).Sweep;
        Assert.Same(a.Spine.Section, b.Spine.Section);
        for (var i = 0; i < a.Spine.Controls.Length; i++)
        {
            var da = a.SideA.Controls[i] - a.Spine.Controls[i]; var db = b.SideA.Controls[i] - b.Spine.Controls[i];
            Assert.Equal(da.X * 3, db.X); Assert.Equal(da.Y * 3, db.Y);
        }
    }

    [Fact]
    public void A_taper_can_collapse_at_tips_while_remaining_a_noncollapsed_band()
    {
        var read = Assert.Single(Assert.Single(BandEvidenceExperiment.Inspect(Sample("W002").Broad).Threads).Envelopes).Sweep;
        Assert.Equal(read.SideA.Controls[0], read.SideB.Controls[0]);
        Assert.Equal(read.SideA.Controls[^1], read.SideB.Controls[^1]);
        Assert.NotEqual(read.SideA.Controls[1], read.SideB.Controls[1]);
        Assert.False(read.Collapsed);
    }

    [Fact]
    public void Explicit_zero_and_unexposed_width_are_different_reads()
    {
        var zero = Assert.Single(Assert.Single(BandEvidenceExperiment.Inspect(Sample("W019").Broad).Threads).Envelopes);
        var unknown = Assert.Single(Assert.Single(BandEvidenceExperiment.Inspect(Sample("W020").Broad).Threads).Envelopes);
        Assert.True(zero.Sweep.Complete); Assert.True(zero.Sweep.Collapsed); Assert.True(zero.Occupied);
        Assert.False(unknown.Sweep.Complete); Assert.Null(unknown.Sweep.Collapsed); Assert.Null(unknown.Occupied);
        Assert.True(unknown.Sweep.Spine.Complete);
    }

    [Fact]
    public void Continued_band_is_occupied_without_visible_caps_or_visible_closed_contour()
    {
        var read = Assert.Single(Assert.Single(BandEvidenceExperiment.Inspect(Sample("W017").Broad).Threads).Envelopes);
        Assert.True(read.Occupied); Assert.True(read.Start.Continuing); Assert.True(read.End.Continuing);
        Assert.False(read.Start.Capped); Assert.False(read.End.Capped); Assert.False(read.VisibleContourClosed);
    }

    [Fact]
    public void Whole_side_cycles_do_not_need_a_cap_on_every_span()
    {
        var read = Assert.Single(BandEvidenceExperiment.Inspect(Sample("W006").Broad).Threads);
        Assert.True(read.JoinedSides); Assert.True(read.SideCycles);
        Assert.Single(BandEvidenceExperiment.ConnectedRuns(read.Envelopes));
        Assert.All(read.Envelopes, e => { Assert.False(e.Start.Capped); Assert.False(e.End.Capped); });
    }

    [Fact]
    public void Shared_plus_site_and_projected_crossing_have_different_topology()
    {
        var plus = BandEvidenceExperiment.Inspect(Sample("W008").Broad);
        var branches = plus.Threads.SelectMany(t => t.Envelopes).Select(e => e.Sweep.Spine).ToArray();
        Assert.Equal(4, branches.Length); Assert.All(branches, p => Assert.Same(branches[0].Start, p.Start));
        Assert.All(plus.Threads.SelectMany(t => t.Envelopes), e => Assert.False(e.Start.Capped));
        Assert.Empty(plus.Crossings); Assert.Empty(plus.Orders);
        var cross = BandEvidenceExperiment.Inspect(Sample("W009").Broad);
        Assert.True(Assert.Single(cross.Crossings).Crosses); Assert.False(Assert.Single(cross.Orders).Complete);
        var paths = cross.Threads.SelectMany(t => t.Envelopes).Select(e => e.Sweep.Spine).ToArray();
        Assert.NotSame(paths[0].Start, paths[1].Start); Assert.NotSame(paths[0].End, paths[1].End);
    }

    [Theory]
    [InlineData("W010", true)]
    [InlineData("W011", false)]
    public void Depth_order_is_an_explicit_reversible_surface(string id, bool firstInFront)
    {
        var read = BandEvidenceExperiment.Inspect(Sample(id).Broad);
        var cross = Assert.Single(read.Crossings); var order = Assert.Single(read.Orders);
        Assert.True(order.Complete); Assert.Same(firstInFront ? cross.First : cross.Second, order.Front);
        Assert.Same(firstInFront ? cross.Second : cross.First, order.Back);
    }

    [Fact]
    public void Basket_has_alternating_local_orders_while_curved_crossing_stays_open()
    {
        var basket = BandEvidenceExperiment.Inspect(Sample("W012").Broad);
        Assert.Equal(20, basket.Crossings.Count); Assert.All(basket.Crossings, c => Assert.True(c.Crosses));
        Assert.Equal(20, basket.Orders.Count); Assert.All(basket.Orders, o => Assert.True(o.Complete));
        var curves = BandEvidenceExperiment.Inspect(Sample("W013").Broad);
        Assert.Null(Assert.Single(curves.Crossings).Crosses); Assert.False(Assert.Single(curves.Orders).Complete);
        Assert.All(curves.Threads, t => Assert.True(t.JoinedSides));
    }

    [Fact]
    public void Reader_uses_native_layer_instead_of_diagnostic_host_lists()
    {
        var source = Sample("W012").Broad;
        var hiddenLists = source with { Threads = [], Crossings = [], Orders = [] };
        Assert.Equal(20, BandEvidenceExperiment.Inspect(hiddenLists).Crossings.Count);
    }

    [Fact]
    public void Missing_offered_entries_survive_as_open_reads()
    {
        var f = Sample("W001").Frame; var unknown = new Placeholder();
        var result = BandEvidenceExperiment.Inspect(Layer(f, [unknown], [unknown], [unknown]));
        Assert.False(Assert.Single(Assert.Single(result.Threads).Envelopes).Sweep.Complete);
        Assert.Null(Assert.Single(result.Crossings).Crosses); Assert.False(Assert.Single(result.Orders).Complete);
        Assert.Equal(3, E(result.Remainder).Length);
    }

    [Fact]
    public void Valid_but_unexposed_crossing_and_order_cannot_enter_another_layer()
    {
        var sample = Sample("W010"); var original = BandEvidenceExperiment.Inspect(sample.Broad);
        var onlyFirst = original.Threads[0].Envelopes[0].Surface;
        var result = BandEvidenceExperiment.Inspect(Layer(sample.Frame, [onlyFirst], sample.Broad.Crossings.Cast<IElement>().ToArray(), sample.Broad.Orders.Cast<IElement>().ToArray()));
        Assert.Null(Assert.Single(result.Crossings).Crosses); Assert.False(Assert.Single(result.Orders).Complete);
        Assert.NotEmpty(E(result.Remainder));
    }

    [Fact]
    public void Side_coordinate_equality_does_not_stitch_distinct_sites_and_missing_spans_split_runs()
    {
        var sample = Sample("W005"); var envs = Assert.Single(BandEvidenceExperiment.Inspect(sample.Broad).Threads).Envelopes;
        Assert.Single(BandEvidenceExperiment.ConnectedRuns(envs));
        var old = envs[1].Sweep; var f = sample.Frame;
        Relation[] Offsets(Curve.Read side) => side.Controls.Zip(old.Spine.Controls, (a, b) => f.Word((int)(a.X - b.X), (int)(a.Y - b.Y))).ToArray();
        var sweep = Band.Sweep(f, old.Spine.Section, Offsets(old.SideA), Offsets(old.SideB));
        var replacement = Band.Read(f, sweep);
        var unstitched = Band.ReadEnvelope(f, Band.BandEnvelope(sweep, Band.UnknownEnd(replacement, true), Band.UnknownEnd(replacement, false), Band.Occupy(replacement)));
        var result = BandEvidenceExperiment.Inspect(Layer(f, [envs[0].Surface, unstitched.Surface]));
        Assert.False(Assert.Single(result.Threads).JoinedSides);
        Assert.Equal(2, BandEvidenceExperiment.ConnectedRuns(Assert.Single(result.Threads).Envelopes).Count);
        var unknown = Band.ReadEnvelope(f, StructuralConstraint.Open());
        Assert.Equal(2, BandEvidenceExperiment.ConnectedRuns(new[] { envs[0], unknown, envs[2] }).Count);
    }

    [Fact]
    public void Complete_boundaries_without_selected_occupancy_are_not_painted_as_fill()
    {
        var sample = Sample("W001"); var old = Assert.Single(Assert.Single(BandEvidenceExperiment.Inspect(sample.Broad).Threads).Envelopes);
        var sweep = (Relation)old.Surface.Left;
        var envelope = Band.BandEnvelope(sweep, Band.Cap(sample.Frame, old.Sweep, true), Band.Cap(sample.Frame, old.Sweep, false));
        var layer = Layer(sample.Frame, [envelope]); var read = Assert.Single(Assert.Single(BandEvidenceExperiment.Inspect(layer).Threads).Envelopes);
        Assert.True(read.Sweep.Complete); Assert.Null(read.Occupied);
        var panels = BandEvidenceExperiment.CreatePanels(sample with { Narrow = layer, Broad = layer });
        Assert.DoesNotContain(panels.SelectMany(p => p.Paths), p => p.Role == "occupied-support");
        Assert.Contains(panels.SelectMany(p => p.Paths), p => p.Role == "side-a");
    }

    [Fact]
    public void Ordered_foreground_with_unknown_width_keeps_paths_without_inventing_an_area_mask()
    {
        var sample = Sample("W010"); var old = BandEvidenceExperiment.Inspect(sample.Broad);
        var front = old.Threads[0].Envelopes[0].Sweep;
        var unknownSweep = Band.Sweep(sample.Frame, front.Spine.Section,
            [StructuralConstraint.Open(), StructuralConstraint.Open()], [sample.Frame.Word(0, -1), sample.Frame.Word(0, -1)]);
        var unknown = Band.Read(sample.Frame, unknownSweep);
        var envelope = Band.BandEnvelope(unknownSweep, Band.UnknownEnd(unknown, true), Band.UnknownEnd(unknown, false));
        var layer = Layer(sample.Frame, [envelope, old.Threads[1].Envelopes[0].Surface],
            sample.Broad.Crossings.Cast<IElement>().ToArray(), sample.Broad.Orders.Cast<IElement>().ToArray());
        Assert.True(Assert.Single(BandEvidenceExperiment.Inspect(layer).Orders).Complete);
        var panels = BandEvidenceExperiment.CreatePanels(sample with { Narrow = layer, Broad = layer });
        Assert.All(panels, p => Assert.NotEmpty(p.Paths));
        Assert.DoesNotContain(panels.Skip(1).SelectMany(p => p.Paths), p => p.Cuts is { Count: > 0 });
    }
}
