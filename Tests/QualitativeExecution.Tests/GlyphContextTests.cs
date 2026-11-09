using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;
using Gojo.Experiments.QualitativeFrieze.Composition;

namespace QualitativeExecution.Tests;

public sealed class GlyphContextTests
{
    private static readonly Lazy<GlyphContextStudy.Report> Study = new(GlyphContextStudy.Run);
    private static GlyphContextStudy.Case Case(string id) => Study.Value.Cases.Single(c => c.Id == id);

    [Fact]
    public void Same_A_shape_has_different_answers_when_the_supplied_load_changes()
    {
        Assert.Equal("inside", Case("a-centered").ProjectionRead);
        Assert.Equal("outside", Case("a-shifted").ProjectionRead);
        Assert.Equal(Case("a-centered").Lines, Case("a-shifted").Lines);
    }

    [Fact]
    public void One_contact_does_not_gain_width_from_two_incident_arms()
    {
        Assert.Equal(1, Case("v-point").ContactCount);
        Assert.Equal("boundary", Case("v-point").ProjectionRead);
        Assert.Equal("outside", Case("v-shifted").ProjectionRead);
        Assert.Equal(2, Case("v-inverted").ContactCount);
        Assert.Equal("inside", Case("v-inverted").ProjectionRead);
    }

    [Fact]
    public void N_and_Z_can_agree_at_one_coarse_contextual_read()
    {
        Assert.Equal("inside", Case("n-ground").ProjectionRead);
        Assert.Equal(Case("n-ground").ProjectionRead, Case("z-ground").ProjectionRead);
        Assert.NotEqual(Case("n-ground").Lines, Case("z-ground").Lines);
        Assert.Equal("inside", Case("z-wall").ProjectionRead);
        Assert.Equal(new double[] { 0, 2 }, Case("z-wall").Projection);
    }

    [Fact]
    public void Empty_or_open_selected_contacts_do_not_turn_shape_familiarity_into_support()
    {
        Assert.Equal("unresolved", Case("a-unselected").ProjectionRead);
        Assert.True(Case("a-unselected").NativeComplete);
        Assert.Equal("unresolved", Case("a-open").ProjectionRead);
        Assert.False(Case("a-open").NativeComplete);
    }

    [Fact]
    public void Coincident_foreign_site_cannot_replace_an_actual_contact()
    {
        var fields = FiniteLaw.Entries(Case("a-centered").Surface);
        fields[4] = StructuralConstraint.Closed(new Placeholder());
        var read = GlyphContextStudy.Read(StructuralConstraint.Closed(fields));
        Assert.False(read.Complete);
        Assert.Equal("unresolved", read.ProjectionRead);
        Assert.Equal(0, read.ContactCount);
    }

    [Fact]
    public void Equal_coordinates_on_distinct_tick_references_are_not_the_same_contact()
    {
        var carrier = new Carrier();
        IElement actual = new CarrierLocation(carrier, new TickRef(0));
        IElement sameCoordinate = new CarrierLocation(carrier, new TickRef(0));
        Assert.Equal(actual, sameCoordinate); // Coordinate equality, not shared sites.
        var surface = ContactSurface(actual, sameCoordinate);
        Assert.False(GlyphContextStudy.Read(surface).Complete);
        Assert.Equal("unresolved", GlyphContextStudy.Read(surface).ProjectionRead);
    }

    [Fact]
    public void A_shared_tick_reference_survives_a_new_boxed_location_read()
    {
        var carrier = new Carrier(); var tick = new TickRef(0);
        IElement actual = new CarrierLocation(carrier, tick);
        IElement sameSite = new CarrierLocation(carrier, tick);
        Assert.NotSame(actual, sameSite);
        var read = GlyphContextStudy.Read(ContactSurface(actual, sameSite));
        Assert.True(read.Complete);
        Assert.Equal("boundary", read.ProjectionRead);
    }

    [Fact]
    public void Equal_relation_shapes_do_not_establish_shared_contact_identity()
    {
        var a = new Placeholder(); var b = new Placeholder();
        IElement actual = new Relation(a, b); IElement equalStructure = new Relation(a, b);
        Assert.Equal(actual, equalStructure);
        Assert.False(GlyphContextStudy.Read(ContactSurface(actual, equalStructure)).Complete);
    }

    private static Relation ContactSurface(IElement actual, IElement selected)
    {
        var frame = new QualitativeCurve.Frame(new QualitativeMotionFrame());
        var section = frame.Line(actual, new Placeholder(), frame.Word(0, 0), frame.Word(0, 4));
        return StructuralConstraint.Closed(frame.Surface, CurveShapeQualities.SimilarityBasis(frame),
            StructuralConstraint.Closed(section), new Relation(frame.Word(0, 0), frame.Word(4, 0)),
            StructuralConstraint.Closed(selected), frame.Word(0, 3));
    }

    [Fact]
    public void A_selected_contact_off_the_declared_ground_is_unresolved()
    {
        var fields = FiniteLaw.Entries(Case("a-centered").Surface);
        var frame = QualitativeCurve.Frame.FromSurface((Relation)fields[0]);
        var sections = FiniteLaw.Entries((Relation)fields[2]).Cast<Relation>().ToArray();
        fields[4] = StructuralConstraint.Closed(frame.Read(sections[0]).End);
        Assert.False(GlyphContextStudy.Read(StructuralConstraint.Closed(fields)).Complete);
    }

    [Fact]
    public void Repeating_one_contact_reference_does_not_create_two_supports()
    {
        var fields = FiniteLaw.Entries(Case("v-point").Surface);
        var contact = FiniteLaw.Entries((Relation)fields[4]).Single();
        fields[4] = StructuralConstraint.Closed(contact, contact);
        var read = GlyphContextStudy.Read(StructuralConstraint.Closed(fields));
        Assert.Equal(1, read.ContactCount);
        Assert.Equal("boundary", read.ProjectionRead);
    }

    [Fact]
    public void Missing_metric_basis_or_open_source_does_not_certify_projection()
    {
        var fields = FiniteLaw.Entries(Case("a-centered").Surface);
        fields[1] = StructuralConstraint.Open();
        Assert.False(GlyphContextStudy.Read(StructuralConstraint.Closed(fields)).Complete);
        Assert.False(GlyphContextStudy.Read(StructuralConstraint.Open(FiniteLaw.Entries(Case("a-centered").Surface))).Complete);
    }

    [Fact]
    public void Reversing_ground_order_changes_comparison_sides_but_not_bracketing()
    {
        var fields = FiniteLaw.Entries(Case("a-centered").Surface);
        var ground = (Relation)fields[3];
        fields[3] = new Relation(ground.Right, ground.Left);
        var read = GlyphContextStudy.Read(StructuralConstraint.Closed(fields));
        Assert.Equal("inside", read.ProjectionRead);
        Assert.Equal(Case("a-centered").Projection, read.Projection);
    }

    [Fact]
    public void Conflicting_positions_at_one_actual_site_invalidate_the_context()
    {
        var fields = FiniteLaw.Entries(Case("a-centered").Surface);
        var frame = QualitativeCurve.Frame.FromSurface((Relation)fields[0]);
        var sections = FiniteLaw.Entries((Relation)fields[2]).Cast<Relation>().ToArray();
        var original = frame.Read(sections[0]);
        sections[0] = frame.Line(original.Start, original.End, frame.Word(0, 0), frame.Word(1, 3));
        fields[2] = StructuralConstraint.Closed(sections);
        Assert.False(GlyphContextStudy.Read(StructuralConstraint.Closed(fields)).Complete);
    }

    [Fact]
    public void Native_archive_recomputes_every_context_without_glyph_labels()
    {
        var decoded = Assert.IsType<Relation>(Assert.Single(GojoNumericFormat.Decode(GojoNumericFormat.Encode(Study.Value.ArchiveRoot))));
        var rows = FiniteLaw.Entries(decoded).Cast<Relation>().ToArray();
        Assert.Equal(Study.Value.Cases.Length, rows.Length);
        for (var i = 0; i < rows.Length; i++)
        {
            var read = GlyphContextStudy.Read(Assert.IsType<Relation>(rows[i].Left));
            Assert.Equal(Study.Value.Cases[i].ProjectionRead, read.ProjectionRead);
            Assert.Equal(Study.Value.Cases[i].NativeComplete, read.Complete);
        }
    }
}
