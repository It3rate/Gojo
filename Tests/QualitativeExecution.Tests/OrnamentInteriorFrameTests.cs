using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;
using Curve = Gojo.Experiments.QualitativeExecution.QualitativeCurve;
using Band = Gojo.Experiments.QualitativeExecution.QualitativeBand;
using Interior = Gojo.Experiments.QualitativeFrieze.Composition.OrnamentInteriorFrame;
using Attachment = Gojo.Experiments.QualitativeFrieze.Composition.OrnamentMotifAttachment;

namespace QualitativeExecution.Tests;

public sealed class OrnamentInteriorFrameTests
{
    [Fact]
    public void A_shared_site_cycle_exposes_an_exact_endpoint_average_and_its_actual_output_word()
    {
        var f = new Fixture(); var cycle = f.Rectangle(10, 8); var read = Interior.FromCycle(f.Frame, cycle);
        Assert.True(read.Complete); Assert.True(read.ClosedBySite); Assert.True(read.CentreComplete); Assert.False(read.Scaffold);
        Assert.Equal(4, read.Sections.Length); Assert.Equal(4, read.Corners.Length);
        Assert.True(f.Frame.Vector(read.Centre, out var centre)); Assert.Equal(new Curve.Vector(5, 4), centre);
        var calculation = Calculation(read.Surface); var tail = (Relation)((Relation)calculation.Right).Right;
        Assert.Same(read.Centre, ((Relation)tail.Right).Left);
        Assert.True(Interior.ReadFrame(f.Frame, read.Surface).Complete);
    }

    [Theory]
    [InlineData(-10, 8, -5, 4)]
    [InlineData(10, -8, 5, -4)]
    [InlineData(-10, -8, -5, -4)]
    public void Reflected_cycles_preserve_signed_direction_in_the_partition(int width, int height, int x, int y)
    {
        var f = new Fixture(); var read = Interior.FromCycle(f.Frame, f.Rectangle(width, height));
        Assert.True(read.Complete); Assert.True(read.ClosedBySite);
        Assert.True(f.Frame.Vector(read.Centre, out var centre)); Assert.Equal(new Curve.Vector(x, y), centre);
    }

    [Fact]
    public void A_zero_average_retains_cancellation_receipts_and_an_exact_empty_word()
    {
        var f = new Fixture(); var sites = Enumerable.Range(0, 4).Select(_ => (IElement)new Placeholder()).ToArray();
        var words = new[] { f.Frame.Word(-5, -4), f.Frame.Word(5, -4), f.Frame.Word(5, 4), f.Frame.Word(-5, 4) };
        var cycle = S(Enumerable.Range(0, 4).Select(i => (IElement)f.Frame.Line(sites[i], sites[(i + 1) % 4], words[i], words[(i + 1) % 4])).ToArray());
        var read = Interior.FromCycle(f.Frame, cycle);
        Assert.True(read.Complete); Assert.True(f.Frame.Vector(read.Centre, out var centre)); Assert.True(centre.Zero);
        Assert.Empty(Items(read.Centre));
        var additive = (Relation)Calculation(read.Surface).Left; var inputs = (Relation)additive.Left;
        Assert.Contains(Items((Relation)inputs.Right).Cast<Relation>(), receipt => f.Frame.Motion.TryReadResult(receipt, out _, out var pairs, out _) && Items(pairs).Length > 0);
    }

    [Fact]
    public void Coincident_positions_do_not_invent_shared_sites_or_a_closed_boundary()
    {
        var f = new Fixture(); var source = Items(f.Rectangle(10, 8)).Cast<Relation>().ToArray();
        var last = f.Frame.Read(source[^1]); source[^1] = f.Frame.Line(last.Start, new Placeholder(), Word(last, true), Word(last, false));
        var read = Interior.FromCycle(f.Frame, S(source.Cast<IElement>().ToArray()));
        Assert.False(read.Complete); Assert.False(read.ClosedBySite); Assert.False(read.CentreComplete);
    }

    [Fact]
    public void Shared_site_aliasing_does_not_hide_a_position_gap()
    {
        var f = new Fixture(); var source = Items(f.Rectangle(10, 8)).Cast<Relation>().ToArray(); var last = f.Frame.Read(source[^1]);
        source[^1] = f.Frame.Line(last.Start, last.End, Word(last, true), f.Frame.Word(0, 1));
        Assert.False(Interior.FromCycle(f.Frame, S(source.Cast<IElement>().ToArray())).ClosedBySite);
    }

    [Fact]
    public void An_open_source_selection_remains_open_even_when_its_visible_sections_form_a_cycle()
    {
        var f = new Fixture(); var read = Interior.FromCycle(f.Frame, StructuralConstraint.Open(Items(f.Rectangle(10, 8))));
        Assert.False(read.Complete); Assert.False(read.ClosedBySite); Assert.False(read.CentreComplete);
    }

    [Fact]
    public void A_nonrepresentable_average_keeps_closure_and_an_open_partition_without_rounding()
    {
        var f = new Fixture(); var read = Interior.FromCycle(f.Frame, f.Rectangle(3, 3));
        Assert.True(read.ClosedBySite); Assert.False(read.CentreComplete); Assert.False(read.Complete);
        Assert.False(f.Frame.Vector(read.Centre, out _));
        var calculation = Calculation(read.Surface); var selection = (Relation)((Relation)calculation.Right).Right;
        Assert.True(StructuralConstraint.TryReadScope((Relation)selection.Left, out var pending, out var seam, out var tail));
        Assert.False(seam.Equals(tail)); Assert.Single(pending);
    }

    [Fact]
    public void A_curved_triangle_uses_actual_corner_words_and_exact_three_way_participation()
    {
        var f = new Fixture(); IElement a = new Placeholder(), b = new Placeholder(), c = new Placeholder();
        var aa = f.Frame.Word(0, 0); var bb = f.Frame.Word(12, 0); var cc = f.Frame.Word(6, 12);
        var first = f.Frame.Quadratic(a, b, aa, f.Frame.Word(6, -2), bb);
        var read = Interior.FromCycle(f.Frame, S(first, f.Frame.Line(b, c, bb, cc), f.Frame.Line(c, a, cc, aa)));
        Assert.True(read.Complete); Assert.Equal(3, read.Corners.Length); Assert.Same(aa, read.Corners[0]);
        Assert.True(f.Frame.Vector(read.Centre, out var centre)); Assert.Equal(new Curve.Vector(6, 4), centre);
    }

    [Fact]
    public void An_explicit_mesh_scaffold_provides_a_landmark_without_claiming_closed_fill()
    {
        var f = new Fixture(); var scaffold = f.Scaffold(); var read = Interior.FromScaffold(f.Frame, scaffold.Sections, scaffold.Ports);
        Assert.True(read.Complete); Assert.True(read.Scaffold); Assert.True(read.CentreComplete); Assert.False(read.ClosedBySite);
        Assert.True(f.Frame.Vector(read.Centre, out var centre)); Assert.Equal(new Curve.Vector(5, 4), centre);
        Assert.True(Interior.ReadFrame(f.Frame, read.Surface).Complete);
    }

    [Fact]
    public void An_equal_looking_foreign_section_cannot_authorize_a_selected_mesh_port()
    {
        var f = new Fixture(); var scaffold = f.Scaffold(); var sections = Items(scaffold.Sections).Cast<Relation>().ToArray(); var first = f.Frame.Read(sections[0]);
        sections[0] = f.Frame.Line(first.Start, first.End, Word(first, true), Word(first, false));
        var read = Interior.FromScaffold(f.Frame, S(sections.Cast<IElement>().ToArray()), scaffold.Ports);
        Assert.False(read.Complete); Assert.False(read.CentreComplete);
    }

    [Fact]
    public void An_equal_copied_output_is_not_the_actual_partition_result()
    {
        var f = new Fixture(); var read = Interior.FromCycle(f.Frame, f.Rectangle(10, 8)); var calculation = Calculation(read.Surface);
        var division = (Relation)calculation.Right; var partition = (Relation)division.Right; var result = (Relation)partition.Right;
        var forged = new Relation(calculation.Left, new Relation(division.Left, new Relation(partition.Left,
            new Relation(f.Frame.Word(5, 4), result.Right))));
        Assert.False(Interior.ReadFrame(f.Frame, WithCalculation(read.Surface, forged)).CentreComplete);
    }

    [Fact]
    public void A_copied_equal_endpoint_word_cannot_replace_its_actual_source_dependency()
    {
        var f = new Fixture(); var read = Interior.FromCycle(f.Frame, f.Rectangle(10, 8)); var body = (Relation)read.Surface.Right;
        var rows = Items((Relation)body.Left); var row = (Relation)rows[1]; rows[1] = new Relation(row.Left, f.Frame.Word(10, 0));
        var forged = new Relation(read.Surface.Left, new Relation(S(rows), body.Right));
        Assert.False(Interior.ReadFrame(f.Frame, forged).Complete);
    }

    [Fact]
    public void An_additive_receipt_must_consume_the_actual_previous_output()
    {
        var f = new Fixture(); var read = Interior.FromCycle(f.Frame, f.Rectangle(10, 8)); var calculation = Calculation(read.Surface);
        var additive = (Relation)calculation.Left; var inputs = (Relation)additive.Left; var rows = Items((Relation)inputs.Right);
        var second = (Relation)rows[1]; var request = (Relation)second.Left; var operands = (Relation)request.Right;
        rows[1] = new Relation(new Relation(request.Left, new Relation(f.Frame.Word(0, 0), operands.Right)), second.Right);
        var forged = new Relation(new Relation(new Relation(inputs.Left, S(rows)), additive.Right), calculation.Right);
        Assert.False(Interior.ReadFrame(f.Frame, WithCalculation(read.Surface, forged)).Complete);
    }

    [Fact]
    public void An_equal_partition_cannot_substitute_new_moves_for_actual_sum_occurrences()
    {
        var f = new Fixture(); var read = Interior.FromCycle(f.Frame, f.Rectangle(10, 8)); var calculation = Calculation(read.Surface);
        var division = (Relation)calculation.Right; var partition = (Relation)division.Right; var groups = Items((Relation)partition.Left);
        var actualFirst = (Relation)groups[0]; var moves = Items(actualFirst); var actualMove = (Relation)moves[0];
        moves[0] = new Relation(actualMove.Left, actualMove.Right); groups[0] = S(moves);
        var first = (Relation)groups[0];
        // Even rebuilding equal comparisons cannot supply missing move identity.
        var differences = groups.Skip(1).Cast<Relation>().Select(w => (IElement)OrnamentDifference(f.Frame, first, w)).ToArray();
        var forged = new Relation(calculation.Left, new Relation(division.Left,
            new Relation(S(groups), new Relation(first, S(differences)))));
        Assert.False(Interior.ReadFrame(f.Frame, WithCalculation(read.Surface, forged)).CentreComplete);
    }

    private static Relation OrnamentDifference(Curve.Frame frame, Relation from, Relation to)
    {
        // Tests cannot call the internal shared helper. Construct the same
        // witness solely to ensure the partition accounting is the failed law.
        var inverse = new List<IElement>(); var proofs = new List<IElement>();
        foreach (var item in Items(from).Cast<Relation>())
        {
            frame.Motion.Direction(item, out _, out _, out var reach);
            var opposite = Items(frame.Motion.Opposites).Cast<Relation>().First(p => ReferenceEquals(p.Left, item.Left));
            var move = QualitativeMotionFrame.Move(opposite.Right, reach); inverse.Add(move); proofs.Add(new Relation(new Relation(item, move), opposite));
        }
        return new(new Relation(from, to), new Relation(S(proofs.ToArray()), frame.Motion.Reduce(S(inverse.ToArray()), to)));
    }

    private static Relation Calculation(Relation surface) => (Relation)((Relation)((Relation)surface.Right).Right).Right;
    private static Relation WithCalculation(Relation surface, Relation calculation)
    { var body = (Relation)surface.Right; var rest = (Relation)body.Right; return new(surface.Left, new Relation(body.Left, new Relation(rest.Left, calculation))); }
    private static Relation Word(Curve.Read curve, bool start) => (Relation)Items(curve.ControlWords)[start ? 0 : curve.Controls.Length - 1];
    private static IElement[] Items(Relation scope) { StructuralConstraint.TryReadScope(scope, out var items, out _, out _); return items; }
    private static Relation S(params IElement[] items) => StructuralConstraint.Closed(items);

    private sealed class Fixture
    {
        public Curve.Frame Frame { get; } = new(new QualitativeMotionFrame());
        public Relation Rectangle(int width, int height)
        {
            var sites = Enumerable.Range(0, 4).Select(_ => (IElement)new Placeholder()).ToArray();
            var words = new[] { Frame.Word(0, 0), Frame.Word(width, 0), Frame.Word(width, height), Frame.Word(0, height) };
            return S(Enumerable.Range(0, 4).Select(i => (IElement)Frame.Line(sites[i], sites[(i + 1) % 4], words[i], words[(i + 1) % 4])).ToArray());
        }
        public (Relation Sections, Relation Ports) Scaffold()
        {
            var points = new[] { (0, 0), (10, 0), (10, 8), (0, 8) }; var sections = new List<IElement>(); var ports = new List<IElement>();
            foreach (var (x, y) in points)
            {
                var section = Frame.Line(new Placeholder(), new Placeholder(), Frame.Word(x, y), Frame.Word(x + 2, y));
                var sweep = Band.Sweep(Frame, section, [Frame.Word(0, 1), Frame.Word(0, 1)], [Frame.Word(0, -1), Frame.Word(0, -1)]);
                var band = Band.Read(Frame, sweep); var envelope = Band.BandEnvelope(sweep, Band.Continue(Frame, band, true), Band.Continue(Frame, band, false), Band.Occupy(band));
                sections.Add(section); ports.Add(Attachment.Endpoint(Frame, envelope, true).Surface);
            }
            return (S(sections.ToArray()), S(ports.ToArray()));
        }
    }
}
