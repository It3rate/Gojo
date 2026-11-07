using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;
using Gojo.Experiments.QualitativeFrieze;
using Gojo.Experiments.QualitativeFrieze.Composition;
using Curve = Gojo.Experiments.QualitativeExecution.QualitativeCurve;
using Action = Gojo.Experiments.QualitativeFrieze.Composition.CentrelineAction;

namespace QualitativeExecution.Tests;

public sealed class CentrelineActionTests
{
    [Fact]
    public void All_local_codes_decode_to_native_rows_and_round_trip_without_an_opcode_in_the_action()
    {
        var f = Frame();
        for (byte bits = 0; bits < 16; bits++)
        {
            var action = Action.FromBits(f, bits, f.Word(2, 3));
            Assert.Equal(bits, Action.Encode(f, action));
            var order = (Relation)((Relation)action.Right).Right;
            Assert.IsType<Placeholder>(((Relation)order.Left).Left);
            Assert.IsType<Placeholder>(((Relation)order.Left).Right);
            Assert.Equal(4, E((Relation)((Relation)((Relation)action.Left).Right).Left).Length);
        }
        Assert.Throws<ArgumentOutOfRangeException>(() => Action.FromBits(f, 16, f.Word(0, 0)));
    }

    [Fact]
    public void Reflection_twice_restores_geometry_and_aliases_without_restoring_site_identity()
    {
        var f = Frame(); var input = Path(f, (0, 0), (2, 3), (5, 3), (5, 1));
        var reflection = Action.FromBits(f, 4, f.Word(0, 0));
        var first = Apply(f, reflection, input); var second = Apply(f, reflection, first.Output);
        var read = Match(f, second.Surface, input);
        Assert.True(read.GeometryExact); Assert.True(read.SitePatternExact);
        var a = f.Read((Relation)E(input)[0]); var b = f.Read((Relation)E(second.Output)[0]);
        Assert.NotSame(a.Start, b.Start); Assert.NotSame(a.End, b.End);
        Assert.Same(reflection, ((Relation)first.Surface.Left).Left);
        Assert.Same(input, ((Relation)first.Surface.Left).Right);
    }

    [Fact]
    public void Translation_and_inverse_are_native_word_reductions()
    {
        var f = Frame(); var input = Path(f, (-2, 1), (0, 3), (3, 3));
        var translated = Apply(f, Action.FromBits(f, 0, f.Word(4, -2)), input);
        var restored = Apply(f, Action.FromBits(f, 0, f.Word(-4, 2)), translated.Output);
        var match = Match(f, restored.Surface, input);
        Assert.True(match.GeometryExact); Assert.True(match.SitePatternExact);
        Assert.All(E(translated.SectionRows).Cast<Relation>(), row => Assert.NotEmpty(E((Relation)row.Right)));
    }

    [Fact]
    public void Traversal_reversal_is_distinct_from_vertical_basis_reflection()
    {
        var f = Frame(); var input = Path(f, (0, 0), (2, 3), (5, 3));
        var reflected = Apply(f, Action.FromBits(f, 4, f.Word(0, 0)), input);
        var reverse = Apply(f, Action.FromBits(f, 12, f.Word(0, 0)), input);
        Assert.False(Match(f, reflected.Surface, reverse.Output).GeometryExact);
        Assert.Equal(new Curve.Vector(5, -3), f.Read((Relation)E(reverse.Output)[0]).Controls[0]);
        var restored = Apply(f, Action.FromBits(f, 8, f.Word(0, 0)), reverse.Output);
        Assert.True(Match(f, restored.Surface, reflected.Output).GeometryExact);
    }

    [Fact]
    public void A_reusable_instruction_accepts_a_new_motif_and_preserves_quadratic_and_cubic_controls()
    {
        var f = Frame(); var action = Action.FromBits(f, 5, f.Word(2, 1));
        Apply(f, action, Path(f, (0, 0), (2, 0)));
        var joint = new Placeholder();
        var newMotif = S(f.Quadratic(new Placeholder(), joint, f.Word(0, 0), f.Word(1, 3), f.Word(2, 2)),
            f.Cubic(joint, new Placeholder(), f.Word(2, 2), f.Word(3, 2), f.Word(3, 0), f.Word(4, 0)));
        var copy = Apply(f, action, newMotif); var sections = E(copy.Output).Cast<Relation>().Select(f.Read).ToArray();
        Assert.Equal(3, sections[0].Controls.Length); Assert.Equal(4, sections[1].Controls.Length);
        Assert.Same(sections[0].End, sections[1].Start); Assert.NotSame(joint, sections[0].End);
        Assert.Equal(new Curve.Vector(2, 1), sections[0].Controls[0]);
        Assert.Equal(new Curve.Vector(-1, 2), sections[0].Controls[1]);
    }

    [Fact]
    public void Missing_substitution_rows_leave_an_open_result_and_cannot_be_laundered_into_complete()
    {
        var f = Frame(); var map = new QualitativePatternTransform(f.Motion, f.Motion,
            S(new Relation(QualitativeMotionFrame.Move(f.Motion.Right, f.Reach), f.Word(1, 0))));
        var action = Action.Create(f, map, f.Word(0, 0)); var input = Path(f, (0, 0), (2, 0), (2, 3));
        var application = Action.Apply(f, action, input);
        Assert.True(Action.TryReadApplication(f, application, out var read)); Assert.False(read.Complete);
        Assert.Single(E(read.Output)); Assert.Single(E(read.Unresolved));
        var forged = new Relation(application.Left, S(S(E(read.Output)), read.SectionRows, read.SitePairs, S()));
        Assert.False(Action.TryReadApplication(f, forged, out _));
        Assert.False(Match(f, application, input).GeometryExact);
        Assert.False(Action.TryBits(f, action, out _));
    }

    [Fact]
    public void A_forged_consumed_receipt_is_rejected_even_when_its_output_coordinates_are_plausible()
    {
        var f = Frame(); var read = Apply(f, Action.FromBits(f, 4, f.Word(0, 0)), Path(f, (1, 1), (3, 2)));
        var fields = E((Relation)read.Surface.Right); var rows = E(read.SectionRows);
        var row = (Relation)rows[0]; var positions = E((Relation)row.Right); var position = (Relation)positions[0];
        var evidence = (Relation)position.Right; var receipt = (Relation)evidence.Left;
        var fakeReceipt = new Relation(new Relation(((Relation)receipt.Left).Left, f.Word(1, 1)), receipt.Right);
        positions[0] = new Relation(position.Left, new Relation(fakeReceipt, evidence.Right));
        rows[0] = new Relation(row.Left, S(positions)); fields[1] = S(rows);
        Assert.False(Action.TryReadApplication(f, new Relation(read.Surface.Left, S(fields)), out _));
    }

    [Fact]
    public void Coordinate_equality_does_not_repair_a_broken_shared_site_pattern()
    {
        var f = Frame(); var source = Path(f, (0, 0), (2, 0), (2, 2));
        var application = Apply(f, Action.FromBits(f, 0, f.Word(0, 0)), source);
        var detached = S(f.Line(new Placeholder(), new Placeholder(), f.Word(0, 0), f.Word(2, 0)),
            f.Line(new Placeholder(), new Placeholder(), f.Word(2, 0), f.Word(2, 2)));
        var match = Match(f, application.Surface, detached);
        Assert.True(match.GeometryExact); Assert.False(match.SitePatternExact);
        Assert.NotEmpty(E(match.AliasMismatches));
        var opposite = Apply(f, Action.FromBits(f, 0, f.Word(0, 0)), detached);
        var reverseMatch = Match(f, opposite.Surface, source);
        Assert.True(reverseMatch.GeometryExact); Assert.False(reverseMatch.SitePatternExact);
    }

    [Fact]
    public void Coordinate_mismatch_keeps_an_independent_valid_alias_pattern()
    {
        var f = Frame(); var application = Apply(f, Action.FromBits(f, 0, f.Word(0, 0)), Path(f, (0, 0), (2, 0), (2, 2)));
        var match = Match(f, application.Surface, Path(f, (0, 0), (3, 0), (3, 2)));
        Assert.False(match.GeometryExact); Assert.True(match.SitePatternExact);
        var fields = E((Relation)match.Surface.Right); fields[3] = S();
        Assert.False(Action.TryReadMatch(f, new Relation(match.Surface.Left, S(fields)), out _));
    }

    [Fact]
    public void Disjoint_coincident_sections_stay_disjoint_and_closed_loops_keep_closure()
    {
        var f = Frame(); var zero = new Placeholder(); var middle = new Placeholder();
        var closed = S(f.Line(zero, middle, f.Word(0, 0), f.Word(2, 0)), f.Line(middle, zero, f.Word(2, 0), f.Word(0, 0)));
        var copied = Apply(f, Action.FromBits(f, 8, f.Word(0, 0)), closed);
        var pieces = E(copied.Output).Cast<Relation>().Select(f.Read).ToArray();
        Assert.Same(pieces[0].Start, pieces[1].End); Assert.Same(pieces[0].End, pieces[1].Start);
        var a = (Relation)E(Path(f, (0, 0), (2, 0)))[0]; var b = (Relation)E(Path(f, (0, 0), (2, 0)))[0];
        var separate = Apply(f, Action.FromBits(f, 0, f.Word(0, 0)), S(a, b));
        var separatePieces = E(separate.Output).Cast<Relation>().Select(f.Read).ToArray();
        Assert.NotSame(separatePieces[0].Start, separatePieces[1].Start);
        Assert.NotSame(separatePieces[0].End, separatePieces[1].End);
    }

    [Fact]
    public void Foreign_basis_open_selection_and_inconsistent_site_locations_do_not_default_to_identity()
    {
        var f = Frame(); var g = Frame(); var input = Path(f, (0, 0), (2, 1));
        var foreign = Action.Apply(f, Action.FromBits(g, 4, g.Word(0, 0)), input);
        Assert.True(Action.TryReadApplication(f, foreign, out var wrongBasis)); Assert.False(wrongBasis.Complete);
        var open = Action.Apply(f, Action.FromBits(f, 0, f.Word(0, 0)), StructuralConstraint.Open(E(input)));
        Assert.True(Action.TryReadApplication(f, open, out var openRead)); Assert.False(openRead.Complete);
        var site = new Placeholder();
        var conflict = S(f.Line(site, new Placeholder(), f.Word(0, 0), f.Word(2, 0)), f.Line(site, new Placeholder(), f.Word(1, 0), f.Word(2, 0)));
        var inconsistent = Action.Apply(f, Action.FromBits(f, 0, f.Word(0, 0)), conflict);
        Assert.True(Action.TryReadApplication(f, inconsistent, out var inconsistentRead)); Assert.False(inconsistentRead.Complete);
    }

    [Fact]
    public void Real_A2_windows_share_the_same_native_reflected_instruction_with_same_traversal()
    {
        var fixture = CentrelinePatternStudy.BuildSource("A2"); var root = E(fixture.Surface);
        var route = (Relation)E((Relation)root[2])[0]; var all = E((Relation)E(route)[0]);
        var source = S(all.Skip(1).Take(7).ToArray()); var target = S(all.Skip(5).Take(7).ToArray());
        var application = Apply(fixture.Frame, Action.FromBits(fixture.Frame, 4, fixture.Frame.Word(2, 2)), source);
        var match = Match(fixture.Frame, application.Surface, target);
        Assert.True(match.GeometryExact); Assert.True(match.SitePatternExact);
    }

    private static Curve.Frame Frame() => new(new QualitativeMotionFrame());
    private static Relation Path(Curve.Frame f, params (int X, int Y)[] points)
    {
        var sites = points.Select(_ => (IElement)new Placeholder()).ToArray();
        return S(points.Zip(points.Skip(1), (a, b) => (a, b)).Select((p, i) => (IElement)f.Line(sites[i], sites[i + 1], f.Word(p.a.X, p.a.Y), f.Word(p.b.X, p.b.Y))).ToArray());
    }
    private static Action.ApplicationRead Apply(Curve.Frame frame, Relation action, Relation source)
    {
        var surface = Action.Apply(frame, action, source);
        Assert.True(Action.TryReadApplication(frame, surface, out var read)); Assert.True(read.Complete); return read;
    }
    private static Action.MatchRead Match(Curve.Frame frame, Relation application, Relation target)
    {
        var surface = Action.Match(frame, application, target);
        Assert.True(Action.TryReadMatch(frame, surface, out var read)); return read;
    }
    private static Relation S(params IElement[] items) => StructuralConstraint.Closed(items);
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
}
