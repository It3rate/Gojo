using System.Numerics;
using Gojo.Core;
using Gojo.Encoding.Structures;
using Gojo.Experiments.QualitativeExecution;
using Gojo.Patterns;
using Gojo.Structures;
using Curve = Gojo.Experiments.QualitativeExecution.QualitativeCurve;

namespace QualitativeExecution.Tests;

public sealed class QualitativeCurveTests
{
    [Fact]
    public void Bow_and_straight_line_share_endpoints_without_sharing_their_trace()
    {
        var f = Frame(); var a = new Placeholder(); var b = new Placeholder();
        var line = f.Read(f.Line(a, b, f.Word(0, 0), f.Word(8, 0)));
        var bow = f.Read(f.Cubic(a, b, f.Word(0, 0), f.Word(2, 4), f.Word(6, 4), f.Word(8, 0)));
        Assert.True(line.Complete); Assert.True(bow.Complete);
        Assert.True(line.Straight); Assert.False(bow.Straight);
        Assert.Equal(0, line.BendSign); Assert.Equal(-1, bow.BendSign);
        Assert.True(line.Regular); Assert.True(bow.Regular);
        Assert.Same(line.Start, bow.Start); Assert.Same(line.End, bow.End);
        Assert.NotEqual(line.StartVector, bow.StartVector);
    }

    [Fact]
    public void Reversal_flips_bend_and_forward_tangents_but_preserves_oriented_interior_convexity()
    {
        var f = Frame(); var source = f.Cubic(new Placeholder(), new Placeholder(),
            f.Word(0, 0), f.Word(2, 4), f.Word(6, 4), f.Word(8, 0));
        var original = f.Read(source); var reverse = f.Read(f.Reverse(source));
        Assert.Equal(-original.BendSign, reverse.BendSign);
        Assert.Equal(-original.EndVector, reverse.StartVector);
        Assert.Equal(-original.StartVector, reverse.EndVector);
        Assert.Same(original.Start, reverse.End); Assert.Same(original.End, reverse.Start);
        Assert.Equal(Curve.Convexity(original, -1), Curve.Convexity(reverse, 1));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Reflection_reverses_signed_bend_and_interior_side_together(bool horizontal)
    {
        var f = Frame(); var source = f.Quadratic(new Placeholder(), new Placeholder(),
            f.Word(0, 0), f.Word(3, 4), f.Word(6, 0));
        var original = f.Read(source); var reflected = f.Read(f.Reflect(source, horizontal));
        Assert.Equal(-original.BendSign, reflected.BendSign);
        Assert.Equal(Curve.Convexity(original, -1), Curve.Convexity(reflected, 1));
    }

    [Fact]
    public void An_S_connection_has_a_certified_inflection_instead_of_one_convexity()
    {
        var f = Frame();
        var s = f.Read(f.Cubic(new Placeholder(), new Placeholder(),
            f.Word(0, 0), f.Word(2, 4), f.Word(4, -4), f.Word(6, 0)));
        Assert.True(s.Complete); Assert.True(s.Regular); Assert.True(s.Inflected);
        Assert.Null(s.BendSign); Assert.Null(Curve.Convexity(s, 1)); Assert.Null(Curve.Convexity(s, -1));
    }

    [Fact]
    public void Unknown_interior_and_closure_do_not_manufacture_convexity()
    {
        var f = Frame(); var sameSite = new Placeholder();
        var loop = f.Read(f.Cubic(sameSite, sameSite, f.Word(0, 0), f.Word(4, 5), f.Word(-4, 5), f.Word(0, 0)));
        Assert.Same(loop.Start, loop.End);
        Assert.Null(Curve.Convexity(loop, null)); Assert.Null(Curve.Convexity(loop, 0));
        var bow = f.Read(f.Quadratic(new Placeholder(), new Placeholder(), f.Word(0, 0), f.Word(3, 4), f.Word(6, 0)));
        Assert.NotNull(bow.BendSign); Assert.Null(Curve.Convexity(bow, null));
        Assert.Equal(-Curve.Convexity(bow, 1), Curve.Convexity(bow, -1));
    }

    [Fact]
    public void Equal_forward_tangent_directions_are_smooth_even_with_unequal_handles()
    {
        var f = Frame(); var shared = new Placeholder();
        var incoming = f.Read(f.Cubic(new Placeholder(), shared, f.Word(0, 0), f.Word(1, 2), f.Word(3, 0), f.Word(4, 0)));
        var outgoing = f.Read(f.Cubic(shared, new Placeholder(), f.Word(4, 0), f.Word(6, 0), f.Word(7, 2), f.Word(8, 2)));
        var join = Curve.Join(incoming, outgoing);
        Assert.True(join.Connected); Assert.True(join.Smooth); Assert.False(join.Cusp); Assert.False(join.Corner);
    }

    [Fact]
    public void Opposed_forward_tangents_are_cusp_and_noncollinear_tangents_are_corner()
    {
        var f = Frame(); var shared = new Placeholder();
        var incoming = f.Read(f.Line(new Placeholder(), shared, f.Word(0, 0), f.Word(4, 0)));
        var cusp = Curve.Join(incoming, f.Read(f.Line(shared, new Placeholder(), f.Word(4, 0), f.Word(1, 0))));
        var corner = Curve.Join(incoming, f.Read(f.Line(shared, new Placeholder(), f.Word(4, 0), f.Word(4, 3))));
        Assert.True(cusp.Cusp); Assert.False(cusp.Smooth); Assert.False(cusp.Corner);
        Assert.True(corner.Corner); Assert.False(corner.Smooth); Assert.False(corner.Cusp);
    }

    [Fact]
    public void Coincident_coordinates_do_not_create_a_join_or_close_a_gap()
    {
        var f = Frame();
        var a = f.Read(f.Line(new Placeholder(), new Placeholder(), f.Word(0, 0), f.Word(4, 0)));
        var b = f.Read(f.Line(new Placeholder(), new Placeholder(), f.Word(4, 0), f.Word(8, 0)));
        var join = Curve.Join(a, b);
        Assert.Equal(a.Controls[^1], b.Controls[0]);
        Assert.False(join.Connected); Assert.Null(join.Smooth); Assert.Null(join.Cusp); Assert.Null(join.Corner);
    }

    [Fact]
    public void Shared_site_with_disagreeing_coordinate_words_is_not_a_smooth_join()
    {
        var f = Frame(); var shared = new Placeholder();
        var a = f.Read(f.Line(new Placeholder(), shared, f.Word(0, 0), f.Word(4, 0)));
        var b = f.Read(f.Line(shared, new Placeholder(), f.Word(5, 0), f.Word(8, 0)));
        var join = Curve.Join(a, b);
        Assert.True(join.Connected);
        Assert.Null(join.Smooth); Assert.Null(join.Cusp); Assert.Null(join.Corner);
    }

    [Fact]
    public void Native_placeholder_aliases_share_a_join_but_opposed_faces_do_not()
    {
        var f = Frame(); var shared = new Placeholder();
        var a = f.Read(f.Line(new Placeholder(), shared, f.Word(0, 0), f.Word(4, 0)));
        var alias = f.Read(f.Line(shared.Dual().Dual(), new Placeholder(), f.Word(4, 0), f.Word(8, 0)));
        var opposed = f.Read(f.Line(shared.Dual(), new Placeholder(), f.Word(4, 0), f.Word(8, 0)));
        Assert.True(Curve.Join(a, alias).Smooth); Assert.False(Curve.Join(a, opposed).Connected);
    }

    [Fact]
    public void Zero_handles_retain_tangent_limits_but_not_a_regular_parameterization_claim()
    {
        var f = Frame();
        var curve = f.Read(f.Cubic(new Placeholder(), new Placeholder(),
            f.Word(0, 0), f.Word(0, 0), f.Word(4, 3), f.Word(4, 3)));
        Assert.True(curve.Complete); Assert.False(curve.Regular);
        Assert.False(curve.StartVector.Zero); Assert.False(curve.EndVector.Zero);
        Assert.True(f.Vector(curve.StartTangent, out var start));
        Assert.Equal(new Curve.Vector(4, 3), start);
        Assert.True(f.Vector(curve.EndTangent, out var end)); Assert.Equal(start, end);
        Assert.Null(Curve.Convexity(curve, 1));
    }

    [Fact]
    public void A_constant_point_has_no_direction_or_bend_and_a_backtrack_is_not_regular()
    {
        var f = Frame();
        var point = f.Read(f.Cubic(new Placeholder(), new Placeholder(), f.Word(1, 1), f.Word(1, 1), f.Word(1, 1), f.Word(1, 1)));
        var folded = f.Read(f.Cubic(new Placeholder(), new Placeholder(), f.Word(0, 0), f.Word(4, 0), f.Word(-4, 0), f.Word(0, 0)));
        Assert.True(point.Complete); Assert.Null(point.Straight); Assert.Null(point.BendSign); Assert.False(point.Regular);
        Assert.True(point.StartVector.Zero); Assert.True(point.EndVector.Zero);
        Assert.True(folded.Straight); Assert.False(folded.Regular); Assert.False(folded.Inflected);
    }

    [Fact]
    public void Endpoint_curvature_signs_alone_do_not_claim_an_inflection_without_regular_certificate()
    {
        var f = Frame();
        // Zero start handle: a missing regularity certificate cannot be hidden
        // by assigning a global bend label to this degenerate construction.
        var curve = f.Read(f.Cubic(new Placeholder(), new Placeholder(), f.Word(0, 0), f.Word(0, 0), f.Word(2, 3), f.Word(4, 0)));
        Assert.True(curve.Complete); Assert.False(curve.Regular); Assert.False(curve.Inflected);
    }

    [Fact]
    public void Foreign_reach_and_open_control_word_remain_unresolved()
    {
        var f = Frame(); var other = new Curve.Frame(f.Motion);
        Assert.False(f.Vector(other.Word(2, 0), out _));
        Assert.False(f.Vector(StructuralConstraint.Open(QualitativeMotionFrame.Move(f.Motion.Right, f.Reach)), out _));
        var foreign = f.Read(f.Line(new Placeholder(), new Placeholder(), f.Word(0, 0), other.Word(4, 0)));
        Assert.False(foreign.Complete); Assert.Null(foreign.Straight); Assert.Null(foreign.BendSign);
    }

    [Fact]
    public void Reach_alias_is_valid_but_the_opposed_reach_face_is_not()
    {
        var reach = new Placeholder(); var f = new Curve.Frame(new QualitativeMotionFrame(), reach);
        var alias = StructuralConstraint.Closed(QualitativeMotionFrame.Move(f.Motion.Right, reach.Dual().Dual()));
        var opposite = StructuralConstraint.Closed(QualitativeMotionFrame.Move(f.Motion.Right, reach.Dual()));
        Assert.True(f.Vector(alias, out var vector)); Assert.Equal(new Curve.Vector(1, 0), vector);
        Assert.False(f.Vector(opposite, out _));
    }

    [Fact]
    public void Cubic_scheme_is_a_connected_deCasteljau_tower_with_one_parameter()
    {
        var f = Frame(); var section = f.Cubic(new Placeholder(), new Placeholder(), f.Word(0, 0), f.Word(2, 4), f.Word(6, 4), f.Word(8, 0));
        var body = (Relation)section.Right; var controls = Entries((Relation)body.Left); var scheme = (Relation)body.Right;
        var parameter = ((Relation)((Relation)((Relation)scheme.Left).Right).Right).Left;
        var steps = Entries((Relation)((Relation)scheme.Right).Left);
        Assert.Equal(6, steps.Length);
        var outputs = new List<IElement>();
        foreach (var item in steps)
        {
            Assert.True(TransformSurface.TryReadInterpolate((Relation)item, out var a, out var b, out var t, out var output));
            Assert.Same(parameter, t);
            Assert.Contains(a, controls.Concat(outputs)); Assert.Contains(b, controls.Concat(outputs));
            outputs.Add(output);
        }
        Assert.Same(outputs[^1], ((Relation)scheme.Right).Right);
        Assert.True(f.Read(section).Complete);
    }

    [Fact]
    public void A_forged_interpolation_parameter_or_extra_step_does_not_read_as_a_curve()
    {
        var f = Frame(); var section = f.Quadratic(new Placeholder(), new Placeholder(), f.Word(0, 0), f.Word(3, 4), f.Word(6, 0));
        var body = (Relation)section.Right; var scheme = (Relation)body.Right; var program = (Relation)scheme.Right;
        var steps = Entries((Relation)program.Left);
        Assert.True(TransformSurface.TryReadInterpolate((Relation)steps[0], out var a, out var b, out _, out var output));
        steps[0] = TransformSurface.Interpolate(a, b, new Placeholder(), output);
        var bad = new Relation(section.Left, new Relation(body.Left, new Relation(scheme.Left,
            new Relation(StructuralConstraint.Closed(steps), program.Right))));
        Assert.False(f.Read(bad).Complete);
        var extra = new Relation(section.Left, new Relation(body.Left, new Relation(scheme.Left,
            new Relation(StructuralConstraint.Closed(Entries((Relation)program.Left).Append(steps[0]).ToArray()), program.Right))));
        Assert.False(f.Read(extra).Complete);
    }

    [Fact]
    public void A_missing_or_forged_bounded_parameter_domain_prevents_interval_shape_claims()
    {
        var f = Frame(); var section = f.Quadratic(new Placeholder(), new Placeholder(), f.Word(0, 0), f.Word(3, 4), f.Word(6, 0));
        var body = (Relation)section.Right; var scheme = (Relation)body.Right; var head = (Relation)scheme.Left;
        var interval = (Relation)head.Right; var bounded = (Relation)interval.Right; var parameter = bounded.Left;
        var missing = new Relation(section.Left, new Relation(body.Left,
            new Relation(new Relation(head.Left, parameter), scheme.Right)));
        Assert.False(f.Read(missing).Complete);
        var badInterval = new Relation(interval.Left, new Relation(parameter, StructuralConstraint.Open(Entries((Relation)bounded.Right))));
        var forged = new Relation(section.Left, new Relation(body.Left,
            new Relation(new Relation(head.Left, badInterval), scheme.Right)));
        Assert.False(f.Read(forged).Complete);
        var markers = (Relation)interval.Left;
        var reversedOrder = new Relation(interval.Left, new Relation(parameter,
            StructuralConstraint.Closed(new Relation(parameter, markers.Left), new Relation(markers.Right, parameter))));
        var reversed = new Relation(section.Left, new Relation(body.Left,
            new Relation(new Relation(head.Left, reversedOrder), scheme.Right)));
        Assert.False(f.Read(reversed).Complete);
    }

    [Fact]
    public void Native_transport_keeps_control_aliases_interpolation_dependencies_and_unmeasured_roots()
    {
        var f = Frame(); var a = new Placeholder();
        var section = f.Cubic(a, a, f.Word(0, 0), f.Word(4, 5), f.Word(-4, 5), f.Word(0, 0));
        var packet = new Relation(f.Surface, section);
        var json = GojoStructureCompactJson.Serialize(GojoStructureCompactJson.FromElement("curve-construction", packet));
        Assert.True(GojoStructureCompactJson.TryToElement(GojoStructureCompactJson.Deserialize(json), out var decoded));
        var imported = Assert.IsType<Relation>(decoded.Root); var curve = Assert.IsType<Relation>(imported.Right);
        Assert.Equal(((Placeholder)((Relation)curve.Left).Left).Id, ((Placeholder)((Relation)curve.Left).Right).Id);
        var scheme = (Relation)((Relation)curve.Right).Right;
        Assert.Same(imported.Left, ((Relation)scheme.Left).Left);
        var operations = Entries((Relation)((Relation)scheme.Right).Left); Assert.Equal(6, operations.Length);
        var parameters = operations.Cast<Relation>().Select(operation =>
        { Assert.True(TransformSurface.TryReadInterpolate(operation, out _, out _, out var t, out _)); return t; }).ToArray();
        Assert.All(parameters, t => Assert.Same(parameters[0], t));
        Assert.All(Reachable(imported).OfType<Carrier>(), holder =>
        { Assert.Equal(0UL, holder.Value); Assert.Equal(0L, holder.Tension); });
        Assert.All(Reachable(imported).OfType<CarrierLocation>(), location => Assert.Equal(0L, location.Ticks));
    }

    private static Curve.Frame Frame() => new(new QualitativeMotionFrame());
    private static IElement[] Entries(Relation scope)
    { Assert.True(StructuralConstraint.TryReadScope(scope, out var items, out var seam, out var tail)); Assert.Equal(seam, tail); return items; }
    private static IElement[] Reachable(IElement root)
    {
        var seen = new HashSet<IElement>(ReferenceEqualityComparer.Instance);
        void Visit(IElement item) { if (!seen.Add(item)) return; if (item is Relation relation) { Visit(relation.Left); Visit(relation.Right); } }
        Visit(root); return seen.ToArray();
    }
}
