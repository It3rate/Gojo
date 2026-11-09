using Gojo.Core;
using Gojo.Patterns;
using Gojo.Structures;
using Gojo.Experiments.QualitativeExecution;
using Gojo.Experiments.QualitativeFrieze.Composition;

namespace QualitativeExecution.Tests;

public sealed class ShapeNeighborhoodTests
{
    private static readonly Lazy<ShapeNeighborhoodStudy.Report> Study = new(ShapeNeighborhoodStudy.Explore);

    [Fact]
    public void Transform_order_preserves_different_constraints_without_shape_name_dispatch()
    {
        var square = Shape("square"); var diagonal = Shape("diagonal-square");
        var rhombus = Shape("rhombus"); var rectangle = Shape("diagonal-rectangle");
        Assert.True(square.EqualSectionLengths); Assert.True(square.PerpendicularJoins);
        Assert.True(diagonal.EqualSectionLengths); Assert.True(diagonal.PerpendicularJoins);
        Assert.True(rhombus.EqualSectionLengths); Assert.False(rhombus.PerpendicularJoins);
        Assert.False(rectangle.EqualSectionLengths); Assert.True(rectangle.PerpendicularJoins);
        Assert.False(Shape("parallelogram").EqualSectionLengths); Assert.False(Shape("parallelogram").PerpendicularJoins);
        Assert.All(new[] { square, diagonal, rhombus, rectangle }, s => Assert.Equal(1, s.Components));
        Assert.Contains("scales", diagonal.Limits);
    }

    [Fact]
    public void Metric_reads_require_actual_basis_and_connected_closed_order()
    {
        var fields = FiniteLaw.Entries(Shape("square").NativeSurface);
        var unpromised = StructuralConstraint.Closed(fields[0], StructuralConstraint.Open(), fields[2], fields[3]);
        var read = ShapeNeighborhoodStudy.ReadGeometry(unpromised);
        Assert.Null(read.EqualSectionLengths); Assert.Null(read.PerpendicularJoins);
        foreach (var id in new[] { "hash", "rotated-hash", "open" })
        {
            Assert.Null(Shape(id).EqualSectionLengths); Assert.Null(Shape(id).PerpendicularJoins);
        }
        Assert.Equal(4, Shape("hash").Components);
        Assert.Equal(4, Shape("rotated-hash").Components);
    }

    [Fact]
    public void An_open_inventory_or_malformed_surface_cannot_prove_a_whole_shape_property()
    {
        var fields = FiniteLaw.Entries(Shape("square").NativeSurface);
        var knownEdges = FiniteLaw.Entries((Relation)fields[2]);
        var openEdges = StructuralConstraint.Open(knownEdges);
        var open = StructuralConstraint.Closed(fields[0], fields[1], openEdges, fields[3]);
        foreach (var candidate in new[] { open, StructuralConstraint.Open(fields), StructuralConstraint.Closed() })
        {
            var read = ShapeNeighborhoodStudy.ReadGeometry(candidate);
            Assert.Null(read.EqualSectionLengths); Assert.Null(read.PerpendicularJoins);
        }
    }

    [Fact]
    public void Conflicting_coordinate_words_at_one_shared_site_leave_geometry_unknown()
    {
        var fields = FiniteLaw.Entries(Shape("square").NativeSurface);
        var frame = QualitativeCurve.Frame.FromSurface((Relation)fields[0]);
        var edges = FiniteLaw.Entries((Relation)fields[2]).Cast<Relation>().ToArray();
        var original = frame.Read(edges[1]);
        var start = original.Controls[0]; var end = original.Controls[1];
        // Preserve the actual site objects and the edge vector, while moving
        // both endpoint observations. Local edge reads remain valid; their
        // disagreement with the neighbors must invalidate the whole-shape read.
        edges[1] = frame.Line(original.Start, original.End,
            frame.Word((int)start.X + 10, (int)start.Y), frame.Word((int)end.X + 10, (int)end.Y));
        Assert.True(frame.Read(edges[1]).Complete);
        var candidate = StructuralConstraint.Closed(fields[0], fields[1], StructuralConstraint.Closed(edges), fields[3]);
        var read = ShapeNeighborhoodStudy.ReadGeometry(candidate);
        Assert.Null(read.EqualSectionLengths); Assert.Null(read.PerpendicularJoins);
        var conflicts = FiniteLaw.Entries((Relation)FiniteLaw.Entries(read.Evidence)[1]);
        Assert.Equal(2, conflicts.Length);
        Assert.All(conflicts.Cast<Relation>(), conflict =>
            Assert.Same(((Relation)conflict.Left).Left, ((Relation)conflict.Right).Left));
    }

    [Fact]
    public void Repeated_nonadjacent_site_observations_must_also_agree()
    {
        var frame = new QualitativeCurve.Frame(new QualitativeMotionFrame());
        IElement a = new Placeholder(), b = new Placeholder(), c = new Placeholder();
        var edges = StructuralConstraint.Closed(
            frame.Line(a, b, frame.Word(0, 0), frame.Word(1, 0)),
            frame.Line(b, a, frame.Word(1, 0), frame.Word(1, 1)),
            frame.Line(a, c, frame.Word(1, 1), frame.Word(0, 1)),
            frame.Line(c, a, frame.Word(0, 1), frame.Word(0, 0)));
        // Every consecutive endpoint has the same position. Only an all-site
        // consistency check notices that the repeated site a has two positions.
        var candidate = StructuralConstraint.Closed(frame.Surface, CurveShapeQualities.SimilarityBasis(frame), edges,
            StructuralConstraint.Closed());
        var read = ShapeNeighborhoodStudy.ReadGeometry(candidate);
        Assert.Null(read.EqualSectionLengths); Assert.Null(read.PerpendicularJoins);
    }

    [Fact]
    public void Six_equal_sections_do_not_claim_equal_maximal_rectangle_sides()
    {
        var frame = new QualitativeCurve.Frame(new QualitativeMotionFrame());
        var machine = ConstructionExchangeMachine.Create(frame, 9);
        var execution = machine.Execute(machine.Publish(1, 1));
        var surface = StructuralConstraint.Closed(frame.Surface, CurveShapeQualities.SimilarityBasis(frame),
            execution.Sections, execution.Surface);
        var read = ShapeNeighborhoodStudy.ReadGeometry(surface);
        Assert.True(read.EqualSectionLengths); Assert.False(read.PerpendicularJoins);
        var sections = FiniteLaw.Entries(execution.Sections).Cast<Relation>().Select(frame.Read).ToArray();
        var horizontalSide = sections[1].Controls[1] - sections[0].Controls[0];
        var verticalSide = sections[2].Controls[1] - sections[2].Controls[0];
        Assert.Equal(4 * QualitativeCurve.Vector.Dot(verticalSide, verticalSide),
            QualitativeCurve.Vector.Dot(horizontalSide, horizontalSide));
        // The existing concave notch has perpendicular joins without asserting
        // all interior angles are 90 degrees or claiming convexity.
        Assert.True(Shape("notch").PerpendicularJoins);
    }

    [Fact]
    public void Geometry_claims_are_recomputed_after_native_archive_restore()
    {
        foreach (var shape in Study.Value.Shapes.Where(s => s.Lines.Length != 0))
        {
            var restored = Assert.IsType<Relation>(Assert.Single(GojoNumericFormat.Decode(GojoNumericFormat.Encode(shape.NativeSurface))));
            var read = ShapeNeighborhoodStudy.ReadGeometry(restored);
            Assert.Equal(shape.EqualSectionLengths, read.EqualSectionLengths); Assert.Equal(shape.PerpendicularJoins, read.PerpendicularJoins);
        }
    }

    [Fact]
    public void Circle_is_native_without_polygon_samples_or_rendering_coordinates()
    {
        var circle = Shape("circle"); Assert.True(circle.WholeContour); Assert.Empty(circle.Lines);
        Assert.Equal(0, circle.RadialParts); Assert.Null(circle.Sections);
        Assert.True(ContourConstruction.TryReadCircle(circle.NativeCircle!, out _));
        var restored = Assert.IsType<Relation>(Assert.Single(GojoNumericFormat.Decode(GojoNumericFormat.Encode(circle.NativeCircle!))));
        Assert.True(ContourConstruction.TryReadCircle(restored, out _));
        Assert.Null(circle.EqualSectionLengths); Assert.Null(circle.PerpendicularJoins);
    }

    [Fact]
    public void Polygon_changes_add_native_markers_adjacencies_and_partition_promises()
    {
        foreach (var count in new[] { 4, 5, 6, 8 })
        {
            var shape = Shape("radial-" + count);
            Assert.True(ContourConstruction.TryReadCircle(shape.NativeCircle!, out _));
            Assert.True(ContourConstruction.TryReadPartition(shape.NativePartition!, out _));
            var fields = FiniteLaw.Entries(shape.NativeSurface);
            var locations = FiniteLaw.Entries((Relation)fields[3]);
            var edges = FiniteLaw.Entries((Relation)fields[4]).Cast<Relation>().ToArray();
            Assert.Equal(count, locations.Length); Assert.Equal(count, edges.Length);
            for (var i = 0; i < count; i++) Assert.Same(edges[i].Right, edges[(i + 1) % count].Left);
            Assert.Empty(shape.Lines); Assert.Null(shape.EqualSectionLengths); Assert.Null(shape.PerpendicularJoins);
            var restored = Assert.IsType<Relation>(Assert.Single(GojoNumericFormat.Decode(GojoNumericFormat.Encode(shape.NativeSurface))));
            Assert.True(ContourConstruction.TryReadPartition((Relation)FiniteLaw.Entries(restored)[1], out _));
        }
    }

    [Fact]
    public void Contour_requires_the_actual_return_relation_not_only_the_word_circle()
    {
        var circle = ShapeNeighborhoodStudy.MakeCircle(out _, out _);
        Assert.True(circle.TryReadOrderedGroup(out var fields));
        var noReturn = Relation.OrderedGroup(new Carrier(), fields.Take(3).Append<IElement>(new Placeholder()).ToArray());
        Assert.False(ContourConstruction.TryReadCircle(noReturn, out _));
    }

    private static ShapeNeighborhoodStudy.Neighbor Shape(string id) => Study.Value.Shapes.Single(s => s.Id == id);
}
