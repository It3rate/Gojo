using System.Numerics;
using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;
using Gojo.Experiments.QualitativeFrieze.Composition;
using Curve = Gojo.Experiments.QualitativeExecution.QualitativeCurve;
using Shapes = Gojo.Experiments.QualitativeExecution.CurveShapeQualities;
using Scenes = Gojo.Experiments.QualitativeFrieze.Composition.CurveEvidenceScene;

namespace QualitativeExecution.Tests;

public sealed class CurveContextReadTests
{
    [Fact]
    public void Interior_hypothesis_follows_shared_endpoint_topology_instead_of_array_order()
    {
        var source = Box(9, 6); var message = Scenes.Publish(source, interior: false);
        var shuffled = message with { Spans = [message.Spans[2], message.Spans[0], message.Spans[3], message.Spans[1]] };
        var a = CurveContextRead.Read(message, true, false, false);
        var b = CurveContextRead.Read(shuffled, true, false, false);
        Assert.Equal(1, a.ProposedInteriorSide); Assert.Equal(a.ProposedInteriorSide, b.ProposedInteriorSide);
        Assert.True(a.Coarse.Closed); Assert.True(b.Coarse.Closed);
        Assert.Null(message.InteriorSide); Assert.Null(shuffled.InteriorSide);
        Assert.Contains(Reachable(b.InteriorPremise), item => ReferenceEquals(item, shuffled.Surface));
        Assert.True(ContainsOpenScope(b.InteriorPremise));
    }

    [Fact]
    public void Reversing_the_boundary_reverses_the_provisional_orientation()
    {
        var source = Box(9, 6);
        var reversed = new Scenes.Scene(source.Frame, source.Sections.Reverse().Select(source.Frame.Reverse));
        var a = CurveContextRead.Read(Scenes.Publish(source, interior: false), true, false, false);
        var b = CurveContextRead.Read(Scenes.Publish(reversed, interior: false), true, false, false);
        Assert.Equal(1, a.ProposedInteriorSide); Assert.Equal(-1, b.ProposedInteriorSide);
    }

    [Fact]
    public void Crossing_or_degenerate_chord_loops_do_not_supply_an_interior()
    {
        var crossing = Make(true, 1,
            [(-4, -3), (4, 3)], [(4, 3), (-4, 3)], [(-4, 3), (4, -3)], [(4, -3), (-4, -3)]);
        var degenerate = Make(true, 1, [(0, 0), (6, 0)], [(6, 0), (3, 0)], [(3, 0), (0, 0)]);
        foreach (var scene in new[] { crossing, degenerate })
        {
            var message = Scenes.Publish(scene, interior: false);
            var result = CurveContextRead.Read(message, true, false, false);
            Assert.Null(result.ProposedInteriorSide); Assert.Null(result.Coarse.InteriorSide);
            Assert.Null(message.InteriorSide); Assert.True(ContainsOpenScope(result.InteriorPremise));
        }
    }

    [Fact]
    public void Independent_loops_do_not_masquerade_as_one_parent_boundary()
    {
        var source = Box(9, 6); var f = source.Frame;
        var second = f.Line(new Placeholder(), new Placeholder(), f.Word(20, 0), f.Word(25, 0));
        var message = Scenes.Publish(new Scenes.Scene(f, source.Sections.Append(second)), interior: false);
        var result = CurveContextRead.Read(message, true, false, true);
        Assert.Null(result.ProposedInteriorSide); Assert.Null(result.Gap.From); Assert.Null(result.Gap.To);
        Assert.Null(result.Gap.Relatable); Assert.Empty(Entries(result.Coarse.Assumed));
    }

    [Fact]
    public void Gap_ratio_is_scale_free_and_uses_only_public_support()
    {
        var smallSource = Gap(1); var largeSource = Gap(3);
        var small = Scenes.Publish(smallSource); var large = Scenes.Publish(largeSource);
        var a = CurveContextRead.Read(small, false, false, false, Shapes.SimilarityBasis(small.Frame)).Gap;
        var b = CurveContextRead.Read(large, false, false, false, Shapes.SimilarityBasis(large.Frame)).Gap;
        Assert.True(a.Relatable); Assert.True(b.Relatable);
        Assert.Equal(new BigInteger(4), a.GapSquared); Assert.Equal(new BigInteger(52), a.SupportSquared);
        Assert.Equal(a.GapSquared * b.SupportSquared, b.GapSquared * a.SupportSquared);
        Assert.Equal(9 * a.GapSquared, b.GapSquared); Assert.Equal(9 * a.SupportSquared, b.SupportSquared);
        Assert.Same(a.Incoming!.To, a.From); Assert.Same(a.Outgoing!.From, a.To);
        var exposed = Reachable(a.Surface);
        Assert.Contains(exposed, item => ReferenceEquals(item, small.Surface));
        Assert.Contains(exposed, item => ReferenceEquals(item, a.Incoming.EndHeading));
        Assert.Contains(exposed, item => ReferenceEquals(item, a.Outgoing.StartHeading));
        foreach (var section in smallSource.Reads)
        {
            Assert.DoesNotContain(exposed, item => ReferenceEquals(item, section.Section));
            Assert.DoesNotContain(exposed, item => ReferenceEquals(item, section.ControlWords));
        }
    }

    [Fact]
    public void Parallel_offset_tangents_leave_an_S_completion_possible()
    {
        var message = Scenes.Publish(Gap(1, offset: true));
        var gap = CurveContextRead.Read(message, false, false, true, Shapes.SimilarityBasis(message.Frame)).Gap;
        Assert.NotNull(gap.From); Assert.NotNull(gap.To); Assert.True(gap.SupportSquared > 0);
        Assert.Null(gap.Relatable); // no ray-intersection seed, not a rejection
        Assert.Equal(Scenes.Direction(gap.Incoming!.EndHeading, message.Frame),
            Scenes.Direction(gap.Outgoing!.StartHeading, message.Frame));
    }

    [Fact]
    public void A_weak_negative_relatability_seed_does_not_forbid_an_explicit_completion()
    {
        var source = Gap(1, opposed: true); var message = Scenes.Publish(source, interior: false);
        var result = CurveContextRead.Read(message, true, false, true, Shapes.SimilarityBasis(message.Frame));
        Assert.False(result.Gap.Relatable); Assert.True(result.Coarse.Closed);
        Assert.Single(Entries(result.Coarse.Assumed)); Assert.Equal(1, result.ProposedInteriorSide);
        Assert.False(source.Closed); Assert.Null(message.InteriorSide);
    }

    [Fact]
    public void A_gap_can_support_a_parent_hypothesis_without_mutating_the_observed_message()
    {
        var source = Gap(1); var sections = source.Sections.ToArray(); var message = Scenes.Publish(source, interior: false);
        var observedInterior = message.Interior; var observedSurface = message.Surface;
        var withoutBridge = CurveContextRead.Read(message, true, false, false);
        var completed = CurveContextRead.Read(message, true, false, true);
        Assert.Null(withoutBridge.ProposedInteriorSide); Assert.False(withoutBridge.Coarse.Closed);
        Assert.Equal(1, completed.ProposedInteriorSide); Assert.True(completed.Coarse.Closed);
        var bridge = Assert.Single(Entries(completed.Coarse.Assumed));
        Assert.Contains(completed.Coarse.Sections, section => ReferenceEquals(section, bridge));
        Assert.Same(observedInterior, message.Interior); Assert.Same(observedSurface, message.Surface);
        Assert.Null(message.InteriorSide); Assert.False(source.Closed); Assert.Equal(sections, source.Sections);
        Assert.Empty(Entries(source.Assumed));
    }

    [Theory]
    [InlineData(9, 9, true)]
    [InlineData(18, 5, false)]
    public void The_same_boundary_can_support_enclosure_or_constructed_centre_focus(int horizontal, int vertical, bool compact)
    {
        var source = Box(horizontal, vertical); var before = source.Surface; var message = Scenes.Publish(source, interior: false);
        var basis = Shapes.SimilarityBasis(message.Frame);
        var enclosure = CurveContextRead.Read(message, false, false, false, basis);
        var ridge = CurveContextRead.Read(message, false, true, false, basis);
        Assert.Equal(4, enclosure.Coarse.Sections.Count); Assert.Empty(Entries(enclosure.Coarse.Assumed));
        Assert.Equal(5, ridge.Coarse.Sections.Count);
        var centre = Assert.IsType<Relation>(Assert.Single(Entries(ridge.Coarse.Assumed)));
        var read = ridge.Coarse.Frame.Read(centre); Assert.True(read.Complete);
        Assert.Equal(compact, read.Controls[0] == read.Controls[^1]);
        Assert.Equal(BigInteger.Zero, read.Controls[0].Y); Assert.Equal(BigInteger.Zero, read.Controls[^1].Y);
        if (!compact) { Assert.Equal(new BigInteger(-horizontal), read.Controls[0].X); Assert.Equal(new BigInteger(horizontal), read.Controls[^1].X); }
        Assert.Null(enclosure.ProposedInteriorSide); Assert.Null(ridge.ProposedInteriorSide);
        Assert.Same(before, source.Surface); Assert.Equal(4, source.Sections.Count); Assert.Empty(Entries(source.Assumed));
        Assert.Null(message.InteriorSide); Assert.True(ContainsOpenScope(ridge.Surface));
    }

    [Fact]
    public void A_half_reach_centre_is_not_silently_rounded()
    {
        var source = Make(true, 1, [(0, 0), (9, 0)], [(9, 0), (9, 9)], [(9, 9), (0, 9)], [(0, 9), (0, 0)]);
        var message = Scenes.Publish(source, interior: false);
        var result = CurveContextRead.Read(message, false, true, false, Shapes.SimilarityBasis(message.Frame));
        Assert.Equal(4, result.Coarse.Sections.Count); Assert.Empty(Entries(result.Coarse.Assumed));
        Assert.Same(message.Frame, result.Coarse.Frame); Assert.True(ContainsOpenScope(result.Surface));
    }

    [Fact]
    public void Coordinate_coincidence_does_not_supply_observed_closure_or_an_interior()
    {
        var source = Make(false, 1, [(-9, -6), (9, -6)], [(9, -6), (9, 6)], [(9, 6), (-9, 6)], [(-9, 6), (-9, -6)]);
        var message = Scenes.Publish(source, interior: false);
        var result = CurveContextRead.Read(message, true, false, false);
        Assert.False(source.Closed); Assert.False(result.Coarse.Closed); Assert.Null(result.ProposedInteriorSide);
        Assert.NotSame(result.Gap.From, result.Gap.To); Assert.Equal(BigInteger.Zero, result.Gap.GapSquared);
        Assert.Null(result.Gap.Relatable);
    }

    [Fact]
    public void A_shared_site_with_conflicting_positions_cannot_support_a_parent_context()
    {
        var f = new Curve.Frame(new QualitativeMotionFrame()); var site = new Placeholder();
        var source = new Scenes.Scene(f, [f.Line(site, site, f.Word(0, 0), f.Word(9, 0))]);
        var result = CurveContextRead.Read(Scenes.Publish(source, interior: false), true, true, true);
        Assert.Null(result.ProposedInteriorSide); Assert.Null(result.Gap.From); Assert.Null(result.Gap.To);
    }

    [Fact]
    public void Missing_or_foreign_similarity_keeps_metric_gap_relatability_open()
    {
        var message = Scenes.Publish(Gap(1));
        var foreign = Shapes.SimilarityBasis(new Curve.Frame(new QualitativeMotionFrame()));
        var missing = CurveContextRead.Read(message, true, false, true);
        var incompatible = CurveContextRead.Read(message, true, false, true, foreign);
        var local = Shapes.SimilarityBasis(message.Frame);
        var selected = CurveContextRead.Read(message, true, false, true, local);
        Assert.Null(missing.Gap.Relatable); Assert.Null(incompatible.Gap.Relatable); Assert.True(selected.Gap.Relatable);
        Assert.Equal(missing.Gap.GapSquared, selected.Gap.GapSquared);
        Assert.Equal(missing.Gap.SupportSquared, selected.Gap.SupportSquared);
        Assert.Contains(Reachable(incompatible.Gap.Surface), item => ReferenceEquals(item, foreign));
        Assert.Contains(Reachable(selected.Gap.Surface), item => ReferenceEquals(item, local));
        // Chord topology and orientation are affine reads and remain available.
        Assert.Equal(1, missing.ProposedInteriorSide); Assert.Equal(missing.ProposedInteriorSide, selected.ProposedInteriorSide);
    }

    [Fact]
    public void Comparing_horizontal_and_vertical_extent_for_a_centre_focus_needs_similarity()
    {
        var message = Scenes.Publish(Box(18, 5), interior: false);
        var missing = CurveContextRead.Read(message, false, true, false);
        var basis = Shapes.SimilarityBasis(message.Frame);
        var selected = CurveContextRead.Read(message, false, true, false, basis);
        Assert.Equal(4, missing.Coarse.Sections.Count); Assert.Empty(Entries(missing.Coarse.Assumed));
        Assert.Equal(5, selected.Coarse.Sections.Count); Assert.Single(Entries(selected.Coarse.Assumed));
        Assert.Contains(Reachable(selected.Surface), item => ReferenceEquals(item, basis));
        Assert.Null(message.InteriorSide);
    }

    private static Scenes.Scene Box(int horizontal, int vertical) => Make(true, 1,
        [(-horizontal, -vertical), (horizontal, -vertical)], [(horizontal, -vertical), (horizontal, vertical)],
        [(horizontal, vertical), (-horizontal, vertical)], [(-horizontal, vertical), (-horizontal, -vertical)]);

    private static Scenes.Scene Gap(int scale, bool offset = false, bool opposed = false)
    {
        var endY = offset ? 2 : 0;
        (int X, int Y)[][] recipe = [[(0, 0), (opposed ? -3 : 3, 0), (4, 1), (4, 4)],
            [(4, 4), (-4, 4)], [(-4, 4), (-4, 1), (-3, endY), (-2, endY)]];
        return Make(false, 1, recipe.Select(section => section.Select(p => (p.X * scale, p.Y * scale)).ToArray()).ToArray());
    }

    private static Scenes.Scene Make(bool close, int side, params (int X, int Y)[][] recipe)
    {
        var f = new Curve.Frame(new QualitativeMotionFrame()); var start = new Placeholder(); IElement current = start;
        var sections = new List<Relation>();
        for (var i = 0; i < recipe.Length; i++)
        {
            var words = recipe[i].Select(p => f.Word(p.X, p.Y)).ToArray();
            var end = close && i == recipe.Length - 1 ? start : new Placeholder();
            sections.Add(words.Length == 2 ? f.Line(current, end, words[0], words[1]) :
                f.Cubic(current, end, words[0], words[1], words[2], words[3]));
            current = end;
        }
        return new(f, sections, interior: Scenes.Interior(f, side));
    }

    private static IElement[] Entries(Relation scope)
    {
        Assert.True(StructuralConstraint.TryReadScope(scope, out var entries, out var seam, out var tail));
        Assert.Equal(seam, tail); return entries;
    }
    private static bool ContainsOpenScope(IElement surface) => Reachable(surface).OfType<Relation>().Any(scope =>
        StructuralConstraint.TryReadScope(scope, out _, out var seam, out var tail) && !seam.Equals(tail));
    private static IElement[] Reachable(IElement root)
    {
        var seen = new HashSet<IElement>(ReferenceEqualityComparer.Instance);
        void Visit(IElement item) { if (!seen.Add(item)) return; if (item is Relation row) { Visit(row.Left); Visit(row.Right); } }
        Visit(root); return seen.ToArray();
    }
}
