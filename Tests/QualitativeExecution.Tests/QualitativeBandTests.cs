using Gojo.Core;
using Gojo.Encoding.Structures;
using Gojo.Experiments.QualitativeExecution;
using Gojo.Patterns;
using Gojo.Structures;
using Band = Gojo.Experiments.QualitativeExecution.QualitativeBand;
using Curve = Gojo.Experiments.QualitativeExecution.QualitativeCurve;

namespace QualitativeExecution.Tests;

public sealed class QualitativeBandTests
{
    [Fact]
    public void Constant_relative_offsets_build_two_boundaries_without_measured_carrier_state()
    {
        var f = Frame(); var source = Line(f); var band = Band.Read(f, Sweep(f, source, 2));
        Assert.True(band.Complete); Assert.False(band.Collapsed);
        Assert.Same(source, band.Spine.Section);
        Assert.Equal(new Curve.Vector(0, 2), band.SideA.Controls[0]);
        Assert.Equal(new Curve.Vector(12, -2), band.SideB.Controls[^1]);
        Assert.All(Reachable(band.Surface).OfType<Carrier>(), holder =>
        { Assert.Equal(0UL, holder.Value); Assert.Equal(0L, holder.Tension); });
    }

    [Fact]
    public void Boundary_outputs_share_the_actual_spine_parameter_and_feed_the_transverse_support()
    {
        var f = Frame(); var source = f.Cubic(new Placeholder(), new Placeholder(),
            f.Word(0, 0), f.Word(3, 5), f.Word(9, -5), f.Word(12, 0));
        var band = Band.Read(f, Sweep(f, source, 2)); Assert.True(band.Complete);
        var interval = Interval(source); var t = ((Relation)interval.Right).Left;
        Assert.Same(interval, Interval(band.SideA.Section)); Assert.Same(interval, Interval(band.SideB.Section));
        foreach (var side in new[] { source, band.SideA.Section, band.SideB.Section })
        {
            var program = (Relation)((Relation)((Relation)side.Right).Right).Right;
            foreach (var operation in Entries((Relation)program.Left))
            {
                Assert.True(TransformSurface.TryReadInterpolate((Relation)operation, out _, out _, out var parameter, out _));
                Assert.Same(t, parameter);
            }
        }
        var support = band.Support; var supportBody = (Relation)support.Right;
        Assert.True(TransformSurface.TryReadInterpolate((Relation)supportBody.Left, out var a, out var b, out var u, out var output));
        Assert.Same(Output(band.SideA.Section), a); Assert.Same(Output(band.SideB.Section), b);
        Assert.NotSame(t, u); Assert.Same(output, supportBody.Right);
    }

    [Fact]
    public void Tapering_and_asymmetric_offsets_are_visible_correspondence_not_one_width_scalar()
    {
        var f = Frame(); var source = f.Cubic(new Placeholder(), new Placeholder(),
            f.Word(0, 0), f.Word(3, 4), f.Word(8, 4), f.Word(12, 0));
        var surface = Band.Sweep(f, source,
            [f.Word(0, 0), f.Word(-1, 3), f.Word(1, 2), f.Word(0, 0)],
            [f.Word(0, 0), f.Word(1, -1), f.Word(-1, -4), f.Word(0, 0)]);
        var read = Band.Read(f, surface); Assert.True(read.Complete); Assert.False(read.Collapsed);
        Assert.Equal(read.SideA.Controls[0], read.SideB.Controls[0]);
        Assert.Equal(read.SideA.Controls[^1], read.SideB.Controls[^1]);
        Assert.NotEqual(read.SideA.Controls[1] - read.Spine.Controls[1], -(read.SideB.Controls[1] - read.Spine.Controls[1]));
        Assert.NotSame(read.SideA.Start, read.SideB.Start);
    }

    [Fact]
    public void A_certified_zero_width_is_not_an_unknown_width()
    {
        var f = Frame(); var source = Line(f);
        var zero = Band.Read(f, Sweep(f, source, 0));
        var unknown = Band.Read(f, Band.Sweep(f, source,
            [StructuralConstraint.Open(), f.Word(0, 1)], [f.Word(0, -1), f.Word(0, -1)]));
        Assert.True(zero.Complete); Assert.True(zero.Collapsed);
        Assert.False(unknown.Complete); Assert.Null(unknown.Collapsed);
    }

    [Fact]
    public void A_foreign_reach_or_missing_correspondence_does_not_supply_band_support()
    {
        var f = Frame(); var other = new Curve.Frame(f.Motion); var source = Line(f);
        var foreign = Band.Sweep(f, source, [other.Word(0, 1), other.Word(0, 1)], [f.Word(0, -1), f.Word(0, -1)]);
        Assert.False(Band.Read(f, foreign).Complete);
        Assert.False(Band.Read(f, Band.Sweep(f, source, [f.Word(0, 1)], [f.Word(0, -1)])).Complete);
        var valid = Sweep(f, source, 1); var body = (Relation)valid.Right;
        var noRows = new Relation(valid.Left, new Relation(StructuralConstraint.Closed(), body.Right));
        Assert.False(Band.Read(f, noRows).Complete);
    }

    [Fact]
    public void A_forged_additive_dependency_is_rejected_even_when_rendered_controls_match()
    {
        var f = Frame(); var valid = Sweep(f, Line(f), 1); var body = (Relation)valid.Right;
        var rows = Entries((Relation)body.Left); var row = (Relation)rows[0]; var head = (Relation)row.Left;
        rows[0] = new Relation(new Relation(f.Word(0, 0), head.Right), row.Right);
        Assert.False(Band.Read(f, new Relation(valid.Left, new Relation(StructuralConstraint.Closed(rows), body.Right))).Complete);
    }

    [Fact]
    public void A_band_cannot_replace_the_transverse_input_with_an_unrelated_boundary_output()
    {
        var f = Frame(); var valid = Sweep(f, Line(f), 1); var body = (Relation)valid.Right;
        var support = (Relation)body.Right; var program = (Relation)support.Right;
        Assert.True(TransformSurface.TryReadInterpolate((Relation)program.Left, out _, out var b, out var u, out var output));
        var forged = new Relation(support.Left, new Relation(TransformSurface.Interpolate(new Placeholder(), b, u, output), output));
        Assert.False(Band.Read(f, new Relation(valid.Left, new Relation(body.Left, forged))).Complete);
    }

    [Fact]
    public void A_fresh_equivalent_parameter_is_not_the_same_longitudinal_dependency()
    {
        var f = Frame(); var valid = Sweep(f, Line(f), 1); var selected = (Relation)valid.Left;
        var boundaries = (Relation)selected.Right; var sideA = (Relation)boundaries.Left;
        var read = f.Read(sideA); var forged = f.Line(read.Start, read.End, (Relation)Entries(read.ControlWords)[0], (Relation)Entries(read.ControlWords)[1]);
        Assert.True(f.Read(forged).Complete);
        Assert.False(Band.Read(f, new Relation(new Relation(selected.Left, new Relation(forged, boundaries.Right)), valid.Right)).Complete);
    }

    [Fact]
    public void Selected_occupancy_does_not_invent_visible_caps_or_contour_closure()
    {
        var f = Frame(); var band = Band.Read(f, Sweep(f, Line(f), 2));
        var native = Band.BandEnvelope(band.Surface, Band.UnknownEnd(band, true), Band.UnknownEnd(band, false), Band.Occupy(band));
        var read = Band.ReadEnvelope(f, native);
        Assert.True(read.Occupied); Assert.Null(read.VisibleContourClosed);
        Assert.Null(read.Start.Capped); Assert.Null(read.End.Capped);
        var capped = Band.ReadEnvelope(f, Band.BandEnvelope(band.Surface, Band.Cap(f, band, true), Band.Cap(f, band, false)));
        Assert.True(capped.VisibleContourClosed); Assert.Null(capped.Occupied);
    }

    [Fact]
    public void A_crop_preserves_bounded_support_and_unknown_outside_continuation_without_a_cap()
    {
        var f = Frame(); var band = Band.Read(f, Sweep(f, Line(f), 2)); var outside = new Placeholder();
        var native = Band.BandEnvelope(band.Surface, Band.Continue(f, band, true, outside), Band.Cap(f, band, false), Band.Occupy(band));
        var read = Band.ReadEnvelope(f, native);
        Assert.True(read.Occupied); Assert.True(read.Start.Continuing); Assert.False(read.Start.Capped);
        Assert.True(read.End.Capped); Assert.False(read.VisibleContourClosed);
        Assert.Same(outside, ((Relation)read.Start.Surface.Right).Right);
        Assert.Same(band.Spine.Start, ((Relation)read.Start.Surface.Right).Left);
    }

    [Fact]
    public void Equal_cap_coordinates_do_not_replace_actual_boundary_endpoint_identity()
    {
        var f = Frame(); var band = Band.Read(f, Sweep(f, Line(f), 2));
        var cap = Band.Cap(f, band, false); var actual = f.Read((Relation)cap.Right);
        var fake = f.Line(new Placeholder(), new Placeholder(), (Relation)Entries(actual.ControlWords)[0], (Relation)Entries(actual.ControlWords)[1]);
        var envelope = Band.BandEnvelope(band.Surface, Band.Cap(f, band, true), new Relation(cap.Left, fake));
        var read = Band.ReadEnvelope(f, envelope);
        Assert.Null(read.End.Capped); Assert.Null(read.VisibleContourClosed);
    }

    [Fact]
    public void Continuation_can_connect_an_exposed_external_structure_without_claiming_its_geometry()
    {
        var f = Frame(); var band = Band.Read(f, Sweep(f, Line(f), 2));
        var exterior = StructuralConstraint.Open(new Relation(new Placeholder(), new Placeholder()));
        var native = Band.BandEnvelope(band.Surface, Band.Continue(f, band, true, exterior), Band.UnknownEnd(band, false));
        var read = Band.ReadEnvelope(f, native);
        Assert.True(read.Start.Continuing); Assert.Same(exterior, ((Relation)read.Start.Surface.Right).Right);
        Assert.Null(read.End.Continuing); Assert.False(read.VisibleContourClosed);
    }

    [Fact]
    public void Boundary_continuity_requires_shared_sites_in_addition_to_equal_offsets()
    {
        var f = Frame(); var middle = new Placeholder(); var aSite = new Placeholder(); var bSite = new Placeholder();
        var first = f.Line(new Placeholder(), middle, f.Word(0, 0), f.Word(6, 0));
        var second = f.Line(middle, new Placeholder(), f.Word(6, 0), f.Word(12, 0));
        var a = Band.Read(f, Band.Sweep(f, first, [f.Word(0, 1), f.Word(0, 1)], [f.Word(0, -1), f.Word(0, -1)], sideAEnd: aSite, sideBEnd: bSite));
        var b = Band.Read(f, Band.Sweep(f, second, [f.Word(0, 1), f.Word(0, 1)], [f.Word(0, -1), f.Word(0, -1)], sideAStart: aSite, sideBStart: bSite));
        var detached = Band.Read(f, Sweep(f, second, 1));
        Assert.True(Curve.Join(a.SideA, b.SideA).Smooth); Assert.True(Curve.Join(a.SideB, b.SideB).Smooth);
        Assert.False(Curve.Join(a.SideA, detached.SideA).Connected);
    }

    [Fact]
    public void Straight_crossing_does_not_weld_the_paths_and_depth_order_is_separate()
    {
        var f = Frame(); var horizontal = f.Line(new Placeholder(), new Placeholder(), f.Word(-6, 0), f.Word(6, 0));
        var vertical = f.Line(new Placeholder(), new Placeholder(), f.Word(0, -6), f.Word(0, 6));
        var crossing = Band.Crossing(f, horizontal, vertical, f.Word(0, 0));
        Assert.True(crossing.Crosses); Assert.False(Curve.Join(f.Read(horizontal), f.Read(vertical)).Connected);
        Assert.False(Band.ReadOrder(f, crossing.Surface).Complete);
        var order = Band.ReadOrder(f, Band.Over(crossing, horizontal, vertical));
        Assert.True(order.Complete); Assert.Same(horizontal, order.Front); Assert.Same(vertical, order.Back);
        var reverse = Band.ReadOrder(f, Band.Over(crossing, vertical, horizontal));
        Assert.True(reverse.Complete); Assert.NotSame(order.Front, reverse.Front);
    }

    [Fact]
    public void Collinear_contact_parallel_lines_and_endpoint_touches_are_not_strict_crossings()
    {
        var f = Frame(); var a = f.Line(new Placeholder(), new Placeholder(), f.Word(-4, 0), f.Word(4, 0));
        var parallel = f.Line(new Placeholder(), new Placeholder(), f.Word(-4, 2), f.Word(4, 2));
        var touch = f.Line(new Placeholder(), new Placeholder(), f.Word(0, 0), f.Word(0, 4));
        Assert.False(Band.Crossing(f, a, parallel, f.Word(0, 0)).Crosses);
        Assert.False(Band.Crossing(f, a, touch, f.Word(0, 0)).Crosses);
        Assert.False(Band.Crossing(f, a, a, f.Word(0, 0)).Crosses);
    }

    [Fact]
    public void Curved_crossing_and_an_unrelated_front_path_remain_unresolved()
    {
        var f = Frame(); var a = Line(f); var b = f.Line(new Placeholder(), new Placeholder(), f.Word(6, -4), f.Word(6, 4));
        var curve = f.Quadratic(new Placeholder(), new Placeholder(), f.Word(0, 0), f.Word(6, 0), f.Word(12, 0));
        Assert.Null(Band.Crossing(f, curve, b, f.Word(6, 0)).Crosses);
        var crossing = Band.Crossing(f, a, b, f.Word(6, 0)); Assert.True(crossing.Crosses);
        Assert.False(Band.ReadOrder(f, Band.Over(crossing, a, curve)).Complete);
    }

    [Fact]
    public void Native_transport_preserves_sweep_dependencies_endpoints_and_selected_occupancy()
    {
        var f = Frame(); var band = Band.Read(f, Sweep(f, Line(f), 2));
        var native = Band.BandEnvelope(band.Surface, Band.Continue(f, band, true), Band.Cap(f, band, false), Band.Occupy(band));
        var json = GojoStructureCompactJson.Serialize(GojoStructureCompactJson.FromElement("band", native));
        Assert.True(GojoStructureCompactJson.TryToElement(GojoStructureCompactJson.Deserialize(json), out var imported));
        var envelope = Assert.IsType<Relation>(imported.Root); var sweep = (Relation)envelope.Left;
        var selected = (Relation)sweep.Left; var spine = (Relation)((Relation)selected.Left).Right;
        var sides = (Relation)selected.Right; var sideA = (Relation)sides.Left; var sideB = (Relation)sides.Right;
        Assert.Same(Interval(spine), Interval(sideA)); Assert.Same(Interval(spine), Interval(sideB));
        var support = (Relation)((Relation)sweep.Right).Right;
        var occupancy = (Relation)((Relation)envelope.Right).Right; Assert.Same(support, occupancy.Left);
        var occupancyPair = Assert.IsType<Relation>(Entries((Relation)occupancy.Right)[0]);
        Assert.Same(sideA, occupancyPair.Left); Assert.Same(sideB, occupancyPair.Right);
    }

    private static Curve.Frame Frame() => new(new QualitativeMotionFrame());
    private static Relation Line(Curve.Frame f) => f.Line(new Placeholder(), new Placeholder(), f.Word(0, 0), f.Word(12, 0));
    private static Relation Sweep(Curve.Frame f, Relation source, int halfWidth)
    {
        var count = f.Read(source).Controls.Length;
        return Band.Sweep(f, source, Enumerable.Range(0, count).Select(_ => f.Word(0, halfWidth)).ToArray(),
            Enumerable.Range(0, count).Select(_ => f.Word(0, -halfWidth)).ToArray());
    }
    private static Relation Interval(Relation section) => (Relation)((Relation)((Relation)((Relation)section.Right).Right).Left).Right;
    private static IElement Output(Relation section) => ((Relation)((Relation)((Relation)section.Right).Right).Right).Right;
    private static IElement[] Entries(Relation scope)
    { Assert.True(StructuralConstraint.TryReadScope(scope, out var items, out var seam, out var tail)); Assert.Equal(seam, tail); return items; }
    private static IElement[] Reachable(IElement root)
    {
        var seen = new HashSet<IElement>(ReferenceEqualityComparer.Instance);
        void Visit(IElement item) { if (!seen.Add(item)) return; if (item is Relation r) { Visit(r.Left); Visit(r.Right); } }
        Visit(root); return seen.ToArray();
    }
}
