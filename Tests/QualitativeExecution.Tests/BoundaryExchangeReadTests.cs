using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;
using Gojo.Experiments.QualitativeFrieze.Composition;
using Curve = Gojo.Experiments.QualitativeExecution.QualitativeCurve;

namespace QualitativeExecution.Tests;

public sealed class BoundaryExchangeReadTests
{
    [Fact]
    public void Collinear_subdivision_preserves_ink_and_retains_each_original_section()
    {
        var f = Frame(); var longLine = Line(f, 0, 0, 4, 0);
        var a = Line(f, 0, 0, 1, 0); var b = Line(f, 1, 0, 4, 0);
        var read = BoundaryExchangeRead.Evaluate(f, S(longLine), S(a, b));
        Assert.True(read.Equivalent); Assert.Equal(4, E(read.Matches).Length);
        Assert.All(E(read.SourceAtoms).Cast<Relation>(), atom => Assert.Same(longLine, atom.Left));
        Assert.Same(a, ((Relation)E(read.TargetAtoms)[0]).Left);
        Assert.All(E(read.TargetAtoms).Skip(1).Cast<Relation>(), atom => Assert.Same(b, atom.Left));
        Assert.True(BoundaryExchangeRead.TryRead(f, read.Surface, out var recovered));
        Assert.True(recovered.Equivalent);
    }

    [Fact]
    public void Opposite_traversal_agrees_only_under_the_explicit_undirected_contract()
    {
        var f = Frame(); var a = S(Line(f, 0, 0, 3, 0)); var b = S(Line(f, 3, 0, 0, 0));
        var ink = BoundaryExchangeRead.Evaluate(f, a, b);
        var flow = BoundaryExchangeRead.Evaluate(f, a, b, directed: true);
        Assert.True(ink.Equivalent); Assert.False(flow.Equivalent);
        Assert.Empty(E(flow.Matches)); Assert.Equal(3, E(flow.SourceOnly).Length); Assert.Equal(3, E(flow.TargetOnly).Length);
        Assert.True(BoundaryExchangeRead.TryRead(f, ink.Surface, out _));
        Assert.True(BoundaryExchangeRead.TryRead(f, flow.Surface, out _));
    }

    [Fact]
    public void Repeated_occurrences_are_not_silently_merged()
    {
        var f = Frame(); var line = Line(f, 0, 0, 2, 0);
        var read = BoundaryExchangeRead.Evaluate(f, S(line, line), S(line));
        Assert.False(read.Equivalent); Assert.Equal(2, E(read.Matches).Length);
        Assert.Equal(2, E(read.SourceOnly).Length); Assert.Empty(E(read.TargetOnly));
        Assert.Equal(4, E(read.SourceAtoms).Length);
        Assert.True(BoundaryExchangeRead.TryRead(f, read.Surface, out _));
        Assert.True(BoundaryExchangeRead.Evaluate(f, S(line, line), S(line, line)).Equivalent);
    }

    [Fact]
    public void Open_scope_preserves_known_comparisons_without_claiming_complete_equivalence()
    {
        var f = Frame(); var line = Line(f, 0, 0, 2, 0);
        var source = StructuralConstraint.Open(line);
        var read = BoundaryExchangeRead.Evaluate(f, source, S(line));
        Assert.False(read.Equivalent); Assert.Equal(2, E(read.Matches).Length);
        var pending = Assert.IsType<Relation>(Assert.Single(E(read.Unresolved)));
        Assert.Same(source, pending.Left); Assert.Same(source, pending.Right);
        Assert.True(BoundaryExchangeRead.TryRead(f, read.Surface, out _));
    }

    [Fact]
    public void Zero_at_resolution_remains_a_participating_section_with_distinct_sites()
    {
        var f = Frame(); var zero = Line(f, 1, 1, 1, 1); var line = Line(f, 0, 0, 2, 0);
        var before = f.Read(zero);
        var read = BoundaryExchangeRead.Evaluate(f, S(zero, line), S(line));
        Assert.True(read.Equivalent); Assert.Same(zero, Assert.Single(E(read.SourceZero)));
        Assert.Empty(E(read.TargetZero)); Assert.Equal(2, E(read.SourceAtoms).Length);
        Assert.NotSame(before.Start, before.End);
        Assert.Same(before.Start, f.Read(zero).Start); Assert.Same(before.End, f.Read(zero).End);
        Assert.True(BoundaryExchangeRead.TryRead(f, read.Surface, out _));
    }

    [Fact]
    public void Observation_does_not_modify_the_source_or_create_shared_topological_sites()
    {
        var f = Frame(); var first = Line(f, 0, 0, 2, 0); var second = Line(f, 2, 0, 2, 2);
        var source = S(first, second); var beforeFirst = f.Read(first); var beforeSecond = f.Read(second);
        var word = beforeFirst.ControlWords; var firstEnd = beforeFirst.End; var secondStart = beforeSecond.Start;
        var read = BoundaryExchangeRead.Evaluate(f, source, source);
        Assert.True(read.Equivalent); Assert.NotSame(firstEnd, secondStart);
        Assert.Same(firstEnd, f.Read(first).End); Assert.Same(secondStart, f.Read(second).Start);
        Assert.Same(word, f.Read(first).ControlWords);
        Assert.Same(first, E(source)[0]); Assert.Same(second, E(source)[1]);
    }

    [Fact]
    public void Equal_step_diagonals_support_subdivision_and_reversal()
    {
        var f = Frame(); var source = S(Line(f, -2, -2, 2, 2));
        var target = S(Line(f, 2, 2, 0, 0), Line(f, 0, 0, -2, -2));
        var read = BoundaryExchangeRead.Evaluate(f, source, target);
        Assert.True(read.Equivalent); Assert.Equal(4, E(read.Matches).Length);
        Assert.True(BoundaryExchangeRead.TryRead(f, read.Surface, out _));
    }

    [Fact]
    public void Unsupported_slope_is_unresolved_even_when_both_sides_are_the_same_section()
    {
        var f = Frame(); var line = Line(f, 0, 0, 2, 1);
        var read = BoundaryExchangeRead.Evaluate(f, S(line), S(line));
        Assert.False(read.Equivalent); Assert.Equal(2, E(read.Unresolved).Length);
        Assert.Empty(E(read.SourceAtoms)); Assert.Empty(E(read.SourceZero));
        Assert.True(BoundaryExchangeRead.TryRead(f, read.Surface, out _));
    }

    [Fact]
    public void Curves_and_foreign_frames_are_unresolved_not_linearized()
    {
        var f = Frame(); var curve = f.Quadratic(new Placeholder(), new Placeholder(), f.Word(0, 0), f.Word(1, 2), f.Word(2, 0));
        var foreign = Line(Frame(), 0, 0, 2, 0);
        foreach (var item in new[] { curve, foreign })
        {
            var read = BoundaryExchangeRead.Evaluate(f, S(item), S(item));
            Assert.False(read.Equivalent); Assert.Equal(2, E(read.Unresolved).Length);
            Assert.True(BoundaryExchangeRead.TryRead(f, read.Surface, out _));
        }
    }

    [Fact]
    public void Forged_empty_mismatch_scope_cannot_hide_unaccounted_atoms()
    {
        var f = Frame(); var read = BoundaryExchangeRead.Evaluate(f, S(Line(f, 0, 0, 2, 0)), S(Line(f, 0, 0, 1, 0)));
        var fields = E((Relation)read.Surface.Right); fields[1] = S();
        Assert.False(BoundaryExchangeRead.TryRead(f, new Relation(read.Surface.Left, S(fields)), out _));
    }

    [Fact]
    public void Forged_duplicate_match_or_removed_zero_evidence_is_rejected()
    {
        var f = Frame(); var zero = Line(f, 1, 0, 1, 0); var line = Line(f, 0, 0, 2, 0);
        var read = BoundaryExchangeRead.Evaluate(f, S(zero, line), S(line));
        var fields = E((Relation)read.Surface.Right); var matches = E(read.Matches);
        fields[0] = S(matches[0], matches[0]);
        Assert.False(BoundaryExchangeRead.TryRead(f, new Relation(read.Surface.Left, S(fields)), out _));
        fields = E((Relation)read.Surface.Right); fields[4] = S();
        Assert.False(BoundaryExchangeRead.TryRead(f, new Relation(read.Surface.Left, S(fields)), out _));
    }

    [Fact]
    public void Geometry_exchange_does_not_claim_the_same_junction_structure()
    {
        var f = Frame(); var joint = new Placeholder();
        var source = S(f.Line(new Placeholder(), joint, f.Word(0, 0), f.Word(1, 0)),
            f.Line(joint, new Placeholder(), f.Word(1, 0), f.Word(1, 1)));
        var target = S(Line(f, 0, 0, 1, 0), Line(f, 1, 0, 1, 1));
        var read = BoundaryExchangeRead.Evaluate(f, source, target);
        Assert.True(read.Equivalent);
        Assert.Same(f.Read((Relation)E(source)[0]).End, f.Read((Relation)E(source)[1]).Start);
        Assert.NotSame(f.Read((Relation)E(target)[0]).End, f.Read((Relation)E(target)[1]).Start);
        Assert.True(BoundaryExchangeRead.TryRead(f, read.Surface, out _));
    }

    private static Curve.Frame Frame() => new(new QualitativeMotionFrame());
    private static Relation Line(Curve.Frame f, int x0, int y0, int x1, int y1) =>
        f.Line(new Placeholder(), new Placeholder(), f.Word(x0, y0), f.Word(x1, y1));
    private static Relation S(params IElement[] items) => StructuralConstraint.Closed(items);
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
}
