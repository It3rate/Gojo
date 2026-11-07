using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;
using Gojo.Experiments.QualitativeFrieze;
using Gojo.Experiments.QualitativeFrieze.Composition;
using Curve = Gojo.Experiments.QualitativeExecution.QualitativeCurve;

namespace QualitativeExecution.Tests;

public sealed class CoupledShapeTemplateTests
{
    [Fact]
    public void One_native_template_drives_two_independent_extent_publications_in_opposed_frames()
    {
        var template = CoupledShapeTemplate.ChevronCap();
        var shape = CoupledShapeTemplate.Realize(template, 4, 4, 2, 2);
        Assert.Equal(new[] { "0,0", "2,2", "6,2", "8,0" }, Points(shape.Upper));
        Assert.Equal(new[] { "0,0", "2,-2", "6,-2", "8,0" }, Points(shape.Lower));
        Assert.True(shape.StartCoincident); Assert.True(shape.EndCoincident);
        Assert.NotSame(shape.Upper.Start, shape.Lower.Start); Assert.NotSame(shape.Upper.End, shape.Lower.End);
        Assert.NotSame(shape.Upper.Publication, shape.Lower.Publication);
        Assert.Equal(4, CentrelineAction.Encode(template.Frame, shape.Reflection));
        Assert.Same(template.Surface, ((Relation)shape.Upper.Execution.Left).Left);
        Assert.Same(template.Surface, ((Relation)shape.Lower.Execution.Left).Left);
        Assert.Contains(Reachable(shape.Surface), item => ReferenceEquals(item, shape.Upper.Publication));
        Assert.Contains(Reachable(shape.Surface), item => ReferenceEquals(item, shape.Lower.Publication));
    }

    [Fact]
    public void A_zero_flat_extent_retains_its_phase_and_distinct_native_endpoints()
    {
        var template = CoupledShapeTemplate.ChevronCap();
        var shape = CoupledShapeTemplate.Realize(template, 0, 0);
        Assert.Equal(new[] { "0,0", "2,2", "2,2", "4,0" }, Points(shape.Upper));
        Assert.Equal(3, E(shape.Upper.Sections).Length); Assert.Equal(3, E(shape.Lower.Sections).Length);
        Assert.Equal(1, shape.Upper.ZeroSections); Assert.Equal(1, shape.Lower.ZeroSections);
        var zero = template.Frame.Read((Relation)E(shape.Upper.Sections)[1]);
        Assert.NotSame(zero.Start, zero.End);
        Assert.Equal(zero.Controls[0], zero.Controls[1]);
        Assert.True(shape.EndCoincident);
        Assert.Equal(new[] { 0, 1, 2, 3 }, shape.Upper.SiteIds);
    }

    [Fact]
    public void Changing_a_public_extent_can_open_the_projected_seam_without_cutting_internal_handoffs()
    {
        var template = CoupledShapeTemplate.ChevronCap();
        var shape = CoupledShapeTemplate.Realize(template, 6, 2, 2, 2);
        Assert.False(shape.EndCoincident);
        Assert.Equal(new[] { 10, 0 }, shape.Upper.Points[^1]);
        Assert.Equal(new[] { 6, 0 }, shape.Lower.Points[^1]);
        foreach (var route in new[] { shape.Upper, shape.Lower })
        {
            var sections = E(route.Sections).Cast<Relation>().Select(template.Frame.Read).ToArray();
            for (var i = 1; i < sections.Length; i++) Assert.Same(sections[i - 1].End, sections[i].Start);
        }
        var comparison = (Relation)shape.EndComparison.Right;
        Assert.True(template.Frame.Motion.TryReadResult(comparison, out var remaining, out _, out var open));
        Assert.Equal(4, E(remaining).Length); Assert.Empty(E(open));
        Assert.Same(shape.Upper.EndPort, ((Relation)shape.EndComparison.Left).Left);
        Assert.Same(shape.Lower.EndPort, ((Relation)shape.EndComparison.Left).Right);
    }

    [Fact]
    public void Different_authored_phase_structures_use_the_same_executor()
    {
        var cardinal = CoupledShapeTemplate.Realize(CoupledShapeTemplate.CardinalCap(), 4, 4, 2, 2);
        Assert.Equal(new[] { "0,0", "0,2", "4,2", "4,0" }, Points(cardinal.Upper));
        var inset = CoupledShapeTemplate.Realize(CoupledShapeTemplate.InsetCap(), 4, 4, 2, 2);
        Assert.Equal(new[] { "0,0", "0,6", "8,6", "8,4", "6,4", "6,2", "8,2", "8,0" }, Points(inset.Upper));
        Assert.Equal(7, E(inset.Upper.Sections).Length);
        Assert.True(inset.EndCoincident); Assert.True(cardinal.EndCoincident);
    }

    [Fact]
    public void Phase_order_is_actual_structure_not_a_shape_identifier()
    {
        var template = CoupledShapeTemplate.CardinalCap();
        var body = (Relation)template.Surface.Right;
        var phases = E((Relation)body.Right);
        var reordered = template with { Surface = new Relation(template.Frame.Surface,
            new Relation(body.Left, S(phases[1], phases[0], phases[2]))) };
        var shape = CoupledShapeTemplate.Realize(reordered, 4, 4, 2, 2);
        Assert.Equal(new[] { "0,0", "4,0", "4,2", "4,0" }, Points(shape.Upper));
        Assert.Equal(new[] { "0,0", "4,0", "4,-2", "4,0" }, Points(shape.Lower));
    }

    [Fact]
    public void Missing_open_duplicate_and_foreign_unit_bindings_do_not_become_zero()
    {
        var template = CoupledShapeTemplate.ChevronCap();
        var good = CoupledShapeTemplate.Publish(template, 0, 2); var rows = E(good);
        Assert.True(CoupledShapeTemplate.CanRealize(template, good));
        foreach (var bad in new[]
        {
            S(rows[1]), StructuralConstraint.Open(rows), S(rows[0], rows[0]),
            S(new Relation(template.WidthRole, S(new Placeholder())), rows[1]),
            S(new Relation(template.WidthRole, StructuralConstraint.Open()), rows[1])
        })
        {
            Assert.False(CoupledShapeTemplate.CanRealize(template, bad));
            Assert.Throws<ArgumentException>(() => CoupledShapeTemplate.Realize(template, bad, good));
        }
    }

    [Fact]
    public void Each_phase_keeps_its_role_binding_and_reduction_before_drawing()
    {
        var template = CoupledShapeTemplate.ChevronCap();
        var shape = CoupledShapeTemplate.Realize(template, 3, 2);
        var fields = E((Relation)shape.Upper.Execution.Right);
        var steps = E((Relation)fields[0]);
        var roles = E(shape.Upper.Publication).Cast<Relation>().ToArray();
        Assert.Equal(3, steps.Length);
        foreach (var item in steps.Cast<Relation>())
        {
            var expansion = (Relation)((Relation)item.Left).Left;
            Assert.Same(shape.Upper.Publication, ((Relation)expansion.Left).Right);
            var consumed = E((Relation)((Relation)expansion.Right).Left);
            Assert.All(consumed, binding => Assert.Contains(roles, original => ReferenceEquals(original, binding)));
            var sum = (Relation)((Relation)item.Right).Left;
            Assert.True(template.Frame.Motion.TryReadResult(sum, out _, out _, out var open));
            Assert.Empty(E(open));
            Assert.Contains(E(shape.Upper.Sections), section => ReferenceEquals(section, ((Relation)item.Right).Right));
        }
    }

    [Fact]
    public void Optional_public_samples_are_validated_and_connected_to_each_actual_extent_binding()
    {
        var template = CoupledShapeTemplate.ChevronCap(); var meter = new Carrier();
        var samples = S(new CarrierLocation(meter, 4L), new CarrierLocation(meter, 3L),
            new CarrierLocation(meter, 2L), new CarrierLocation(meter, 2L));
        var shape = CoupledShapeTemplate.Realize(template, 4, 3, 2, 2, samples);
        Assert.Same(samples, ((Relation)shape.PublicationCorrespondence.Left).Left);
        var pairs = E((Relation)shape.PublicationCorrespondence.Right).Cast<Relation>().ToArray();
        var bindings = E(shape.Upper.Publication).Concat(E(shape.Lower.Publication)).ToArray();
        Assert.Equal(4, pairs.Length);
        Assert.All(pairs, pair => Assert.Contains(bindings, row => ReferenceEquals(row, pair.Right)));
        var bad = S(new CarrierLocation(meter, 5L), new CarrierLocation(meter, 3L),
            new CarrierLocation(meter, 2L), new CarrierLocation(meter, 2L));
        Assert.Throws<ArgumentException>(() => CoupledShapeTemplate.Realize(template, 4, 3, 2, 2, bad));
        var foreign = new Carrier();
        Assert.Throws<ArgumentException>(() => CoupledShapeTemplate.Realize(template, 4, 3, 2, 2,
            S(new CarrierLocation(foreign, 4L), E(samples)[1], E(samples)[2], E(samples)[3])));
    }

    [Fact]
    public void Explicit_seam_declarations_admit_a_connected_view_and_preserve_the_independent_sources()
    {
        var shape = CoupledShapeTemplate.Realize(CoupledShapeTemplate.ChevronCap(), 4, 4);
        var cause = new Placeholder(); var join = CoupledShapeTemplate.DeclareSeams(shape, cause);
        Assert.Equal(2, E(join.Accepted).Length); Assert.Empty(E(join.Blocked)); Assert.Empty(E(join.Unresolved));
        Assert.True(join.TryReadPassed(join.Evaluate()));
        Assert.Equal(8, E(join.Source.Sites).Length); Assert.Equal(6, E(join.Graph.Sites).Length);
        Assert.Equal(6, E(join.Source.Strokes).Length); Assert.Equal(6, E(join.Graph.Strokes).Length);
        Assert.All(E(join.DeclaredJoins).Cast<Relation>(), declaration => Assert.Same(cause, declaration.Right));
        Assert.NotSame(shape.Upper.Start, shape.Lower.Start); Assert.NotSame(shape.Upper.End, shape.Lower.End);
        var map = E(join.SiteMap).Cast<Relation>().ToArray();
        IElement Image(IElement site) => map.Single(row => ReferenceEquals(row.Left, site)).Right;
        Assert.Same(Image(shape.Upper.Start), Image(shape.Lower.Start));
        Assert.Same(Image(shape.Upper.End), Image(shape.Lower.End));
        Assert.NotSame(shape.Upper.Start, Image(shape.Upper.Start));
        Assert.All(E(join.Source.Strokes).Cast<Relation>(), stroke =>
            Assert.Contains(E(shape.Upper.Sections).Concat(E(shape.Lower.Sections)), section => ReferenceEquals(section, stroke.Left)));
    }

    [Fact]
    public void An_unmatched_end_seam_stays_blocked_while_the_matching_start_is_admitted()
    {
        var shape = CoupledShapeTemplate.Realize(CoupledShapeTemplate.ChevronCap(), 4, 2);
        var join = CoupledShapeTemplate.DeclareSeams(shape);
        Assert.Single(E(join.Accepted)); Assert.Single(E(join.Blocked)); Assert.Empty(E(join.Unresolved));
        Assert.False(join.TryReadPassed(join.Evaluate()));
        Assert.Equal(7, E(join.Graph.Sites).Length);
        var accepted = (Relation)Assert.Single(E(join.Accepted));
        Assert.Same(shape.Upper.Start, ((Relation)accepted.Left).Left);
        Assert.Same(shape.Lower.Start, ((Relation)accepted.Left).Right);
        var map = E(join.SiteMap).Cast<Relation>().ToArray();
        Assert.NotSame(map.Single(row => ReferenceEquals(row.Left, shape.Upper.End)).Right,
            map.Single(row => ReferenceEquals(row.Left, shape.Lower.End)).Right);
    }

    [Fact]
    public void Declared_outer_seams_do_not_collapse_the_coincident_zero_extent_phase()
    {
        var shape = CoupledShapeTemplate.Realize(CoupledShapeTemplate.ChevronCap(), 0, 0);
        var join = CoupledShapeTemplate.DeclareSeams(shape);
        Assert.True(join.TryReadPassed(join.Evaluate()));
        Assert.Equal(6, E(join.Graph.Sites).Length); Assert.Equal(6, E(join.Graph.Strokes).Length);
        var zero = shape.Template.Frame.Read((Relation)E(shape.Upper.Sections)[1]);
        var map = E(join.SiteMap).Cast<Relation>().ToArray();
        Assert.Same(zero.Start, map.Single(row => ReferenceEquals(row.Left, zero.Start)).Right);
        Assert.Same(zero.End, map.Single(row => ReferenceEquals(row.Left, zero.End)).Right);
        Assert.NotSame(zero.Start, zero.End);
        var output = (Relation)((Relation)E(join.StrokeMap)[1]).Right;
        Assert.True(QualitativeStrokeGraph.TryReadStroke(output, out _, out var a, out var b, out var word));
        Assert.NotSame(a, b); Assert.Empty(E(word));
    }

    private static string[] Points(CoupledShapeTemplate.RouteRead route) => route.Points.Select(point => string.Join(",", point)).ToArray();
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
    private static Relation S(params IElement[] items) => StructuralConstraint.Closed(items);
    private static IEnumerable<IElement> Reachable(IElement source)
    {
        var seen = new HashSet<IElement>(ReferenceEqualityComparer.Instance); var pending = new Stack<IElement>(); pending.Push(source);
        while (pending.TryPop(out var item))
        {
            if (!seen.Add(item)) continue; yield return item;
            if (item is Relation relation) { pending.Push(relation.Left); pending.Push(relation.Right); }
        }
    }
}
