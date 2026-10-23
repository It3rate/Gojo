using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;
using Gojo.Experiments.QualitativeFrieze.Composition;
using Curve = Gojo.Experiments.QualitativeExecution.QualitativeCurve;
using Band = Gojo.Experiments.QualitativeExecution.QualitativeBand;
using Attachment = Gojo.Experiments.QualitativeFrieze.Composition.OrnamentMotifAttachment;
using Substrate = Gojo.Experiments.QualitativeFrieze.Composition.OrnamentSubstrate;

namespace QualitativeExecution.Tests;

public sealed class OrnamentMotifAttachmentTests
{
    [Fact]
    public void Endpoint_ports_expose_actual_position_and_forward_tangent_operands_in_both_orders()
    {
        var f = new Fixture(); var source = f.Frame.Cubic(new Placeholder(), new Placeholder(),
            f.Frame.Word(0, 0), f.Frame.Word(3, 0), f.Frame.Word(8, 5), f.Frame.Word(10, 10));
        var envelope = f.Envelope(source); var start = Attachment.Endpoint(f.Frame, envelope, true); var end = Attachment.Endpoint(f.Frame, envelope, false);
        Assert.True(start.Complete); Assert.True(start.AtStart); Assert.False(start.Interior);
        Assert.True(end.Complete); Assert.False(end.AtStart); Assert.False(end.Interior);
        Assert.True(f.Frame.Vector(start.Tangent, out var a)); Assert.Equal(new Curve.Vector(3, 0), a);
        Assert.True(f.Frame.Vector(end.Tangent, out var b)); Assert.Equal(new Curve.Vector(2, 5), b);
        Assert.Same(f.Frame.Read(source).Start, start.Site); Assert.Same(f.Frame.Read(source).End, end.Site);
        Assert.Same(source, start.Section); Assert.Same(envelope, end.Envelope);
    }

    [Fact]
    public void A_shared_endpoint_and_forward_root_tangent_support_a_smooth_attachment()
    {
        var f = new Fixture(); var mesh = f.Line(new Placeholder(), f.Site, 0, 0, 10, 0);
        var port = Attachment.Endpoint(f.Frame, mesh); var motif = f.Root(f.Site, 10, 0, 3, 0);
        var attached = Attachment.Attach(f.Frame, port.Surface, motif);
        Assert.True(attached.Incident); Assert.True(attached.Forward); Assert.False(attached.Opposed); Assert.True(attached.SharedSite); Assert.True(attached.Smooth);
        Assert.Same(port.Surface, ((Relation)attached.Surface.Left).Left); Assert.Same(motif, attached.Motif);
        Assert.True(Attachment.ReadAttachment(f.Frame, attached.Surface).Smooth);
    }

    [Fact]
    public void Equal_placement_and_tangency_do_not_invent_a_shared_site()
    {
        var f = new Fixture(); var mesh = f.Line(new Placeholder(), f.Site, 0, 0, 10, 0);
        var port = Attachment.Endpoint(f.Frame, mesh); var motif = f.Root(new Placeholder(), 10, 0, 3, 0);
        var attached = Attachment.Attach(f.Frame, port.Surface, motif);
        Assert.True(attached.Incident); Assert.True(attached.Forward); Assert.True(attached.Smooth); Assert.False(attached.SharedSite);
    }

    [Fact]
    public void Opposed_and_noncollinear_tangents_remain_distinct_from_forward_continuation()
    {
        var f = new Fixture(); var mesh = f.Line(new Placeholder(), f.Site, 0, 0, 10, 0); var port = Attachment.Endpoint(f.Frame, mesh);
        var opposed = Attachment.Attach(f.Frame, port.Surface, f.Root(f.Site, 10, 0, -3, 0));
        Assert.True(opposed.Incident); Assert.False(opposed.Forward); Assert.True(opposed.Opposed); Assert.False(opposed.Smooth);
        var corner = Attachment.Attach(f.Frame, port.Surface, f.Root(f.Site, 10, 0, 0, 3));
        Assert.True(corner.Incident); Assert.False(corner.Forward); Assert.False(corner.Opposed); Assert.False(corner.Smooth);
    }

    [Fact]
    public void A_gap_can_coexist_with_aligned_tangents_and_even_a_conflicting_site_alias()
    {
        var f = new Fixture(); var mesh = f.Line(new Placeholder(), f.Site, 0, 0, 10, 0); var port = Attachment.Endpoint(f.Frame, mesh);
        var attached = Attachment.Attach(f.Frame, port.Surface, f.Root(f.Site, 12, 0, 3, 0));
        Assert.False(attached.Incident); Assert.True(attached.Forward); Assert.True(attached.SharedSite); Assert.False(attached.Smooth);
    }

    [Fact]
    public void Closed_curve_end_markers_distinguish_two_tangents_at_one_physical_site()
    {
        var f = new Fixture(); var origin = f.Frame.Word(0, 0);
        var curve = f.Frame.Cubic(f.Site, f.Site, origin, f.Frame.Word(4, 0), f.Frame.Word(0, 4), origin); var envelope = f.Envelope(curve);
        var start = Attachment.Endpoint(f.Frame, envelope, true); var end = Attachment.Endpoint(f.Frame, envelope, false);
        Assert.Same(start.Site, end.Site); Assert.Same(start.Position, end.Position); Assert.True(start.AtStart); Assert.False(end.AtStart);
        Assert.True(f.Frame.Vector(start.Tangent, out var a)); Assert.Equal(new Curve.Vector(4, 0), a);
        Assert.True(f.Frame.Vector(end.Tangent, out var b)); Assert.Equal(new Curve.Vector(0, -4), b);
    }

    [Fact]
    public void Repeated_initial_controls_retain_the_first_available_forward_direction()
    {
        var f = new Fixture(); var origin = f.Frame.Word(0, 0);
        var source = f.Frame.Cubic(f.Site, new Placeholder(), origin, origin, f.Frame.Word(3, 2), f.Frame.Word(6, 4));
        var port = Attachment.Endpoint(f.Frame, f.Envelope(source), true);
        Assert.True(port.Complete); Assert.True(f.Frame.Vector(port.Tangent, out var tangent)); Assert.Equal(new Curve.Vector(3, 2), tangent);
    }

    [Fact]
    public void A_straight_interior_event_port_does_not_split_or_weld_the_continuous_mesh()
    {
        var f = new Fixture(); var grid = f.Grid(); var node = Substrate.At(grid, 1, 0);
        var mesh = f.Line(new Placeholder(), new Placeholder(), -10, 0, 30, 0);
        var crossing = f.Line(new Placeholder(), new Placeholder(), 10, -10, 10, 20);
        var eventRead = Substrate.CrossEvent(grid, node, mesh, crossing);
        var port = Attachment.Interior(grid, node, mesh, eventRead.Surface);
        Assert.True(port.Complete); Assert.True(port.Interior); Assert.Null(port.Site); Assert.Null(port.AtStart); Assert.Same(node.Position, port.Position);
        var original = Band.ReadEnvelope(f.Frame, mesh).Sweep.Spine;
        var attached = Attachment.Attach(f.Frame, port.Surface, f.Root(node.Site, 10, 0, 4, 0));
        Assert.True(attached.Incident); Assert.True(attached.Forward); Assert.False(attached.SharedSite);
        Assert.Equal(2, original.Controls.Length); Assert.NotSame(original.Start, node.Site); Assert.NotSame(original.End, node.Site);
    }

    [Fact]
    public void A_curved_interior_or_noninterior_straight_landmark_does_not_get_an_invented_port()
    {
        var f = new Fixture(); var grid = f.Grid(); var node = Substrate.At(grid, 1, 0);
        var curve = f.Envelope(f.Frame.Cubic(new Placeholder(), new Placeholder(), f.Frame.Word(-10, 0), f.Frame.Word(0, 0), f.Frame.Word(20, 0), f.Frame.Word(30, 0)));
        Assert.False(Attachment.Interior(grid, node, curve).Complete);
        var endpoint = f.Line(node.Site, new Placeholder(), 10, 0, 30, 0);
        Assert.False(Attachment.Interior(grid, node, endpoint).Complete);
        var apart = f.Line(new Placeholder(), new Placeholder(), -10, 5, 30, 5);
        Assert.False(Attachment.Interior(grid, node, apart).Complete);
    }

    [Fact]
    public void A_foreign_crossing_occurrence_cannot_authorize_an_interior_port()
    {
        var f = new Fixture(); var grid = f.Grid(); var node = Substrate.At(grid, 1, 0); var other = Substrate.At(grid, 2, 0);
        var mesh = f.Line(new Placeholder(), new Placeholder(), -10, 0, 30, 0); var cross = f.Line(new Placeholder(), new Placeholder(), 20, -10, 20, 20);
        var wrongEvent = Substrate.CrossEvent(grid, other, mesh, cross);
        Assert.False(Attachment.Interior(grid, node, mesh, wrongEvent.Surface).Complete);
    }

    [Fact]
    public void A_copied_equal_tangent_word_is_not_the_actual_retained_derivative_output()
    {
        var f = new Fixture(); var port = Attachment.Endpoint(f.Frame, f.Line(new Placeholder(), f.Site, 0, 0, 10, 0));
        var body = (Relation)port.Surface.Right; var positionAndTangent = (Relation)body.Right; var tangentAndProof = (Relation)positionAndTangent.Right;
        var forged = new Relation(port.Surface.Left, new Relation(body.Left, new Relation(positionAndTangent.Left,
            new Relation(f.Frame.Word(10, 0), tangentAndProof.Right))));
        Assert.False(Attachment.ReadPort(f.Frame, forged).Complete);
    }

    [Fact]
    public void A_forged_endpoint_position_cannot_replace_the_actual_source_control()
    {
        var f = new Fixture(); var port = Attachment.Endpoint(f.Frame, f.Line(new Placeholder(), f.Site, 0, 0, 10, 0));
        var body = (Relation)port.Surface.Right; var original = (Relation)body.Right;
        var forged = new Relation(port.Surface.Left, new Relation(body.Left, new Relation(f.Frame.Word(10, 0), original.Right)));
        Assert.False(Attachment.ReadPort(f.Frame, forged).Complete);
    }

    [Fact]
    public void Replacing_the_actual_tangent_comparison_or_using_the_motif_end_keeps_attachment_open()
    {
        var f = new Fixture(); var port = Attachment.Endpoint(f.Frame, f.Line(new Placeholder(), f.Site, 0, 0, 10, 0)); var motif = f.Root(f.Site, 10, 0, 3, 0);
        var attachment = Attachment.Attach(f.Frame, port.Surface, motif); var body = (Relation)attachment.Surface.Right; var comparison = (Relation)body.Right;
        var forged = new Relation(attachment.Surface.Left, new Relation(body.Left,
            new Relation(new Relation(f.Frame.Word(10, 0), attachment.Root.Tangent), comparison.Right)));
        Assert.Null(Attachment.ReadAttachment(f.Frame, forged).Forward);
        var end = Attachment.Endpoint(f.Frame, motif, false);
        var wrongEnd = new Relation(new Relation(port.Surface, end.Surface), attachment.Surface.Right);
        Assert.Null(Attachment.ReadAttachment(f.Frame, wrongEnd).Incident);
    }

    [Fact]
    public void Zero_derivative_is_known_position_with_unresolved_orientation()
    {
        var f = new Fixture(); var point = f.Frame.Word(0, 0); var source = f.Envelope(f.Frame.Line(f.Site, f.Site, point, point));
        var port = Attachment.Endpoint(f.Frame, source); Assert.True(port.Complete); Assert.True(f.Frame.Vector(port.Tangent, out var zero)); Assert.True(zero.Zero);
        var attached = Attachment.Attach(f.Frame, port.Surface, f.Root(f.Site, 0, 0, 3, 0));
        Assert.True(attached.Incident); Assert.Null(attached.Forward); Assert.Null(attached.Opposed); Assert.Null(attached.Smooth); Assert.True(attached.SharedSite);
    }

    [Fact]
    public void Valid_spine_ports_survive_missing_width_without_claiming_a_complete_band()
    {
        var f = new Fixture(); var source = f.Frame.Line(new Placeholder(), f.Site, f.Frame.Word(0, 0), f.Frame.Word(10, 0));
        var sweep = Band.Sweep(f.Frame, source, [StructuralConstraint.Open(), f.Frame.Word(0, 1)], [f.Frame.Word(0, -1), f.Frame.Word(0, -1)]);
        var envelope = Band.BandEnvelope(sweep, StructuralConstraint.Open(), StructuralConstraint.Open());
        Assert.False(Band.ReadEnvelope(f.Frame, envelope).Sweep.Complete);
        Assert.True(Attachment.Endpoint(f.Frame, envelope).Complete);
    }

    private sealed class Fixture
    {
        public Curve.Frame Frame { get; } = new(new QualitativeMotionFrame()); public IElement Site { get; } = new Placeholder();
        public Substrate.Lattice Grid() => Substrate.Create(Frame, Frame.Word(10, 0), Frame.Word(0, 10), Frame.Word(0, 0), 3, 2);
        public Relation Line(IElement from, IElement to, int x, int y, int ex, int ey) => Envelope(Frame.Line(from, to, Frame.Word(x, y), Frame.Word(ex, ey)));
        public Relation Root(IElement site, int x, int y, int dx, int dy) => Envelope(Frame.Cubic(site, new Placeholder(),
            Frame.Word(x, y), Frame.Word(x + dx, y + dy), Frame.Word(x + dx * 2 + 1, y + dy * 2 + 1), Frame.Word(x + dx * 3 + 1, y + dy * 3 + 2)));
        public Relation Envelope(Relation section)
        {
            var count = Frame.Read(section).Controls.Length;
            var sweep = Band.Read(Frame, Band.Sweep(Frame, section, Enumerable.Range(0, count).Select(_ => Frame.Word(0, 1)).ToArray(), Enumerable.Range(0, count).Select(_ => Frame.Word(0, -1)).ToArray()));
            return Band.BandEnvelope(sweep.Surface, Band.Continue(Frame, sweep, true), Band.Continue(Frame, sweep, false), Band.Occupy(sweep));
        }
    }
}
