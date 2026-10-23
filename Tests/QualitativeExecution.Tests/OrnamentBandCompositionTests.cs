using Gojo.Core;
using Gojo.Patterns;
using Gojo.Structures;
using Gojo.Experiments.QualitativeExecution;
using Gojo.Experiments.QualitativeFrieze;
using Gojo.Experiments.QualitativeFrieze.Composition;
using Band = Gojo.Experiments.QualitativeExecution.QualitativeBand;
using Curve = Gojo.Experiments.QualitativeExecution.QualitativeCurve;
using Composition = Gojo.Experiments.QualitativeFrieze.Composition.OrnamentBandComposition;

namespace QualitativeExecution.Tests;

public sealed class OrnamentBandCompositionTests
{
    [Fact]
    public void A_compound_placement_substitutes_spine_and_offset_words_and_retains_the_actual_template()
    {
        var f = new Fixture(); var map = f.Quarter();
        var result = Composition.Place(f.Template, map, f.Frame.Word(20, 5));
        Assert.True(result.Complete); Assert.True(Composition.Validate(result));
        Assert.Equal(2, result.Output.Threads.Count); Assert.Equal(3, result.Output.Envelopes.Count);
        Assert.Same(f.Template.Surface, ((Relation)result.Surface.Left).Left);
        Assert.Same(map.Surface, ((Relation)((Relation)result.Surface.Left).Right).Left);
        Assert.Equal(3, result.EnvelopePairs.Count);
        var first = Band.ReadEnvelope(f.Frame, result.Output.Envelopes[0]);
        Assert.Equal(new Curve.Vector(20, 5), first.Sweep.Spine.Controls[0]);
        Assert.Equal(new Curve.Vector(20, 11), first.Sweep.Spine.Controls[^1]);
        Assert.Equal(new Curve.Vector(19, 5), first.Sweep.SideA.Controls[0]);
        Assert.Equal(new Curve.Vector(21, 5), first.Sweep.SideB.Controls[0]);
        Assert.True(first.Occupied); Assert.True(first.Start.Continuing);
        Assert.Same(f.Template.Envelopes[0], result.EnvelopePairs[0].Left);
        Assert.Same(result.Output.Envelopes[0], result.EnvelopePairs[0].Right);
    }

    [Fact]
    public void Shared_spine_and_side_sites_survive_inside_a_copy_but_independent_copies_are_not_welded()
    {
        var f = new Fixture(); var a = Composition.Place(f.Template, Composition.Identity(f.Frame), f.Frame.Word(0, 0));
        var b = Composition.Place(f.Template, Composition.Identity(f.Frame), f.Frame.Word(0, 0));
        var ar = a.Output.Envelopes.Select(e => Band.ReadEnvelope(f.Frame, e).Sweep).ToArray();
        var br = b.Output.Envelopes.Select(e => Band.ReadEnvelope(f.Frame, e).Sweep).ToArray();
        Assert.True(Curve.Join(ar[0].Spine, ar[1].Spine).Smooth);
        Assert.True(Curve.Join(ar[0].SideA, ar[1].SideA).Smooth);
        Assert.True(Curve.Join(ar[0].SideB, ar[1].SideB).Smooth);
        Assert.NotSame(ar[0].Spine.Start, br[0].Spine.Start);
        Assert.Equal(ar[0].Spine.Controls[0], br[0].Spine.Controls[0]);
        Assert.NotSame(ar[0].Spine.Start, Band.ReadEnvelope(f.Frame, f.Template.Envelopes[0]).Sweep.Spine.Start);
    }

    [Fact]
    public void Reflection_preserves_side_correspondence_instead_of_silently_swapping_boundary_identity()
    {
        var f = new Fixture(); var mirror = Composition.Map(f.Frame, f.Frame.Word(-1, 0), f.Frame.Word(1, 0), f.Frame.Word(0, 1), f.Frame.Word(0, -1));
        var result = Composition.Place(f.Template, mirror, f.Frame.Word(0, 0));
        Assert.True(Composition.Validate(result)); var leaf = Band.ReadEnvelope(f.Frame, result.Output.Envelopes[2]);
        Assert.Equal(new Curve.Vector(-4, 7), leaf.Sweep.Spine.Controls[1]);
        Assert.Equal(new Curve.Vector(-3, 9), leaf.Sweep.SideA.Controls[1]);
        Assert.Equal(new Curve.Vector(-5, 5), leaf.Sweep.SideB.Controls[1]);
        Assert.NotSame(leaf.Sweep.SideA.Start, leaf.Sweep.SideB.Start);
    }

    [Fact]
    public void A_placement_rebuilds_actual_visible_caps_and_leaves_unknown_end_evidence_open()
    {
        var f = new Fixture(); var result = Composition.Place(f.Template, f.Quarter(), f.Frame.Word(0, 0));
        var a = Band.ReadEnvelope(f.Frame, result.Output.Envelopes[0]);
        var b = Band.ReadEnvelope(f.Frame, result.Output.Envelopes[1]);
        Assert.Null(a.End.Capped); Assert.Null(b.Start.Capped);
        Assert.True(b.End.Capped); Assert.False(b.End.Continuing);
        Assert.True(a.Start.Continuing); Assert.False(a.VisibleContourClosed);
        Assert.True(Composition.Validate(result));
    }

    [Fact]
    public void A_missing_substitution_does_not_fall_back_to_unchanged_or_zero_width_output()
    {
        var f = new Fixture(); var incomplete = new QualitativePatternTransform(f.Frame.Motion, f.Frame.Motion,
            S(new Relation(QualitativeMotionFrame.Move(f.Frame.Motion.Right, f.Frame.Reach), f.Frame.Word(1, 0))));
        var result = Composition.Place(f.Template, incomplete, f.Frame.Word(0, 0));
        Assert.False(result.Complete); Assert.False(result.Output.Complete); Assert.NotEmpty(E(result.Residue));
        Assert.False(Composition.Validate(result));
        Assert.All(result.Output.Envelopes, e => Assert.Null(Band.ReadEnvelope(f.Frame, e).Sweep.Collapsed));
    }

    [Fact]
    public void Repetition_has_actual_output_to_next_input_feedback_and_finite_controller_history()
    {
        var f = new Fixture(); var run = Composition.Repeat(f.Template, f.Quarter(), f.Frame.Word(0, 0), 4);
        Assert.True(run.Complete); Assert.Equal(4, run.Applications.Count); Assert.Equal(4, run.Instances.Count);
        Assert.Equal(12, run.Output.Envelopes.Count); Assert.Equal(4, E(run.Feedback).Length);
        Assert.True(TimeStepSequence.TryRead(run.Timeline, out _, out var initial, out var steps));
        Assert.Same(f.Template.Surface, initial); Assert.Equal(4, steps.Length);
        for (var i = 0; i < steps.Length; i++)
        {
            Assert.True(Composition.Validate(run.Applications[i]));
            Assert.True(TimeStepSequence.TryReadStep(steps[i], out _, out _, out _, out var before, out var transition, out var after, out var residue));
            Assert.Same(i == 0 ? f.Template.Surface : run.Instances[i - 1].Surface, before);
            Assert.Same(run.Applications[i].Surface, transition); Assert.Same(run.Instances[i].Surface, after);
            Assert.Same(run.Applications[i].Residue, residue);
        }
        Assert.Empty(E((Relation)run.Stop.Right));
    }

    [Fact]
    public void A_failed_repeat_stops_with_its_unfulfilled_controller_visible()
    {
        var f = new Fixture(); var bad = new QualitativePatternTransform(f.Frame.Motion, f.Frame.Motion, S());
        var run = Composition.Repeat(f.Template, bad, f.Frame.Word(0, 0), 5);
        Assert.False(run.Complete); Assert.Equal(2, run.Applications.Count); // initial identity, then first failed transition
        Assert.True(StructuralConstraint.TryReadScope((Relation)run.Stop.Right, out _, out var seam, out var tail));
        Assert.NotEqual(seam, tail); Assert.NotEmpty(E(run.Applications[^1].Residue));
    }

    [Fact]
    public void Nested_composition_retains_actual_child_surfaces_and_can_be_repeated_as_one_unit()
    {
        var f = new Fixture(); var child = Composition.Place(f.Template, f.Quarter(), f.Frame.Word(18, 0)).Output;
        var nested = Composition.Compose(f.Frame, f.Template, child); Assert.True(nested.Complete);
        var children = E((Relation)nested.Surface.Right).Cast<Relation>().ToArray();
        Assert.Same(f.Template.Surface, children[0].Left); Assert.Same(child.Surface, children[1].Left);
        Assert.Same(f.Template.Threads[0], nested.Threads[0]);
        var result = Composition.Place(nested, Composition.Identity(f.Frame), f.Frame.Word(0, 20));
        Assert.True(Composition.Validate(result)); Assert.Equal(6, result.EnvelopePairs.Count);
        Assert.Same(child.Envelopes[0], result.EnvelopePairs[3].Left);
    }

    [Fact]
    public void A_compatible_seam_is_observational_and_does_not_invent_a_join()
    {
        var f = new Fixture(); var seed = Composition.Create(f.Frame, S(f.Template.Envelopes[0]));
        var a = Composition.Place(seed, Composition.Identity(f.Frame), f.Frame.Word(0, 0));
        var b = Composition.Place(seed, Composition.Identity(f.Frame), f.Frame.Word(6, 0));
        var seam = Composition.Seam(f.Frame, a.Output.Envelopes[0], b.Output.Envelopes[0]);
        Assert.True(seam.Coincident); Assert.True(seam.Tangent); Assert.True(seam.SideAgreement); Assert.False(seam.Joined);
        var actual = Composition.Seam(f.Frame, f.Template.Envelopes[0], f.Template.Envelopes[1]);
        Assert.True(actual.Coincident); Assert.True(actual.SideAgreement); Assert.True(actual.Joined);
        var apart = Composition.Place(seed, Composition.Identity(f.Frame), f.Frame.Word(7, 0));
        Assert.False(Composition.Seam(f.Frame, a.Output.Envelopes[0], apart.Output.Envelopes[0]).Coincident);
    }

    [Fact]
    public void Fixed_prototype_arrangement_alternates_mirrors_without_the_two_cycle_of_affine_feedback()
    {
        var f = new Fixture(); var mirror = Composition.Map(f.Frame, f.Frame.Word(-1, 0), f.Frame.Word(1, 0), f.Frame.Word(0, 1), f.Frame.Word(0, -1));
        var recurrence = Composition.Repeat(f.Template, mirror, f.Frame.Word(20, 0), 3);
        var plan = Enumerable.Range(0, 3).Select(i => Composition.Plan(i % 2 == 0 ? Composition.Identity(f.Frame) : mirror,
            f.Frame.Word(20 * i, 0))).ToArray();
        var arranged = Composition.Arrange(f.Template, plan);
        Assert.True(arranged.Complete); Assert.True(recurrence.Complete);
        Curve.Vector Start(Composition.Template t) => Band.ReadEnvelope(f.Frame, t.Envelopes[0]).Sweep.Spine.Controls[0];
        Assert.Equal(new Curve.Vector(0, 0), Start(recurrence.Instances[2]));
        Assert.Equal(new Curve.Vector(40, 0), Start(arranged.Instances[2]));
        Assert.All(arranged.Applications, a => { Assert.Same(f.Template.Surface, a.Source.Surface); Assert.True(Composition.Validate(a)); });
        Assert.Same(recurrence.Instances[1].Surface, recurrence.Applications[2].Source.Surface);
        Assert.Same(plan[1].Surface, E(arranged.Plan)[1]);
        Assert.Same(plan[1].Surface, ((Relation)E((Relation)arranged.Controller.Right)[1]).Right);
    }

    [Fact]
    public void Arrangement_retains_actual_fixed_source_in_every_timed_transition_and_keeps_coincident_copies_distinct()
    {
        var f = new Fixture(); var first = Composition.Plan(Composition.Identity(f.Frame), f.Frame.Word(3, 4));
        var second = Composition.Plan(Composition.Identity(f.Frame), f.Frame.Word(3, 4));
        var arranged = Composition.Arrange(f.Template, first, second);
        Assert.True(arranged.Complete); Assert.Equal(6, arranged.Output.Envelopes.Count);
        var a = Band.ReadEnvelope(f.Frame, arranged.Instances[0].Envelopes[0]).Sweep;
        var b = Band.ReadEnvelope(f.Frame, arranged.Instances[1].Envelopes[0]).Sweep;
        Assert.Equal(a.Spine.Controls[0], b.Spine.Controls[0]); Assert.NotSame(a.Spine.Start, b.Spine.Start);
        Assert.True(TimeStepSequence.TryRead(arranged.Timeline, out _, out var initial, out var steps));
        Assert.Same(f.Template.Surface, initial); Assert.Equal(2, steps.Length);
        for (var i = 0; i < steps.Length; i++)
        {
            Assert.True(TimeStepSequence.TryReadStep(steps[i], out _, out _, out _, out var before, out var transition, out var after, out var residue));
            Assert.Same(f.Template.Surface, before); Assert.Same(arranged.Instances[i].Surface, after);
            var plannedTransition = Assert.IsType<Relation>(transition);
            var binding = Assert.IsType<Relation>(plannedTransition.Right);
            Assert.Same(E(arranged.Plan)[i], binding.Left); Assert.Same(arranged.Applications[i].Surface, binding.Right);
            Assert.Same(arranged.Applications[i].Residue, residue);
        }
    }

    [Fact]
    public void An_open_placement_word_remains_unresolved_while_other_independent_arrangement_entries_can_finish()
    {
        var f = new Fixture(); var map = Composition.Identity(f.Frame);
        var arranged = Composition.Arrange(f.Template, Composition.Plan(map, StructuralConstraint.Open()), Composition.Plan(map, f.Frame.Word(20, 0)));
        Assert.False(arranged.Complete); Assert.Equal(2, arranged.Applications.Count);
        Assert.False(arranged.Applications[0].Complete); Assert.True(arranged.Applications[1].Complete);
        Assert.NotEmpty(E(arranged.Residue));
        Assert.True(StructuralConstraint.TryReadScope((Relation)arranged.Stop.Right, out _, out var seam, out var tail));
        Assert.NotEqual(seam, tail);
        Assert.Null(Band.ReadEnvelope(f.Frame, arranged.Instances[0].Envelopes[0]).Sweep.Collapsed);
        Assert.Equal(new Curve.Vector(20, 0), Band.ReadEnvelope(f.Frame, arranged.Instances[1].Envelopes[0]).Sweep.Spine.Controls[0]);
    }

    [Fact]
    public void Arrangement_uses_native_displacement_rather_than_a_changed_diagnostic_translation_field()
    {
        var f = new Fixture(); var native = f.Frame.Word(12, 0);
        var instruction = Composition.Plan(Composition.Identity(f.Frame), native) with { Translation = f.Frame.Word(999, 0) };
        var arranged = Composition.Arrange(f.Template, instruction);
        Assert.True(arranged.Complete); Assert.Same(native, arranged.Applications[0].Translation);
        Assert.Equal(new Curve.Vector(12, 0), Band.ReadEnvelope(f.Frame, arranged.Instances[0].Envelopes[0]).Sweep.Spine.Controls[0]);
    }

    [Fact]
    public void A_mirrored_strip_can_offer_exchanged_sides_without_changing_path_direction_or_welding()
    {
        var f = new Fixture(); var seed = Composition.Create(f.Frame, S(f.Template.Envelopes[0]));
        var a = Composition.Place(seed, Composition.Identity(f.Frame), f.Frame.Word(0, 0));
        var mirrorY = Composition.Map(f.Frame, f.Frame.Word(1, 0), f.Frame.Word(-1, 0), f.Frame.Word(0, -1), f.Frame.Word(0, 1));
        var b = Composition.Place(seed, mirrorY, f.Frame.Word(6, 0));
        var seam = Composition.Seam(f.Frame, a.Output.Envelopes[0], b.Output.Envelopes[0]);
        Assert.True(seam.Coincident); Assert.True(seam.Tangent); Assert.False(seam.SideAgreement);
        Assert.True(seam.SideExchange); Assert.False(seam.Joined);
        var comparison = (Relation)((Relation)((Relation)seam.Surface.Right).Right).Right;
        var exchanged = Assert.IsType<Relation>(comparison.Right);
        foreach (var item in new[] { exchanged.Left, exchanged.Right })
        {
            var witness = Assert.IsType<Relation>(item);
            Assert.True(f.Frame.Motion.TryReadResult(witness, out var remainder, out _, out _));
            Assert.Empty(E(remainder));
        }
        var originalA = Band.ReadEnvelope(f.Frame, a.Output.Envelopes[0]).Sweep;
        var originalB = Band.ReadEnvelope(f.Frame, b.Output.Envelopes[0]).Sweep;
        Assert.NotSame(originalA.SideA.End, originalB.SideB.Start);
        Assert.Equal(originalA.SideA.Controls[^1], originalB.SideB.Controls[0]);
    }

    [Fact]
    public void Exchanged_side_compatibility_does_not_make_opposed_path_tangents_agree()
    {
        var f = new Fixture(); var seed = Composition.Create(f.Frame, S(f.Template.Envelopes[0]));
        var a = Composition.Place(seed, Composition.Identity(f.Frame), f.Frame.Word(0, 0));
        var half = Composition.Map(f.Frame, f.Frame.Word(-1, 0), f.Frame.Word(1, 0), f.Frame.Word(0, -1), f.Frame.Word(0, 1));
        var b = Composition.Place(seed, half, f.Frame.Word(6, 0));
        var seam = Composition.Seam(f.Frame, a.Output.Envelopes[0], b.Output.Envelopes[0]);
        Assert.True(seam.Coincident); Assert.False(seam.Tangent); Assert.False(seam.SideAgreement);
        Assert.True(seam.SideExchange); Assert.False(seam.Joined);
    }

    [Fact]
    public void Missing_band_support_does_not_become_same_or_exchanged_side_compatibility()
    {
        var f = new Fixture();
        var seam = Composition.Seam(f.Frame, f.Template.Envelopes[0], StructuralConstraint.Open());
        Assert.Null(seam.Coincident); Assert.Null(seam.Tangent); Assert.Null(seam.SideAgreement);
        Assert.Null(seam.SideExchange); Assert.False(seam.Joined);
    }

    [Fact]
    public void Substitution_provenance_is_required_even_if_a_forged_sum_has_equal_coordinates()
    {
        var f = new Fixture(); var result = Composition.Place(f.Template, Composition.Identity(f.Frame), f.Frame.Word(0, 0));
        var outcome = (Relation)result.Surface.Right; var outputs = (Relation)outcome.Left;
        var rows = E((Relation)outputs.Right); var first = (Relation)rows[0]; var parts = (Relation)first.Right;
        var positions = E((Relation)parts.Left); var position = (Relation)positions[0]; var receipts = (Relation)position.Right;
        var forgedSum = f.Frame.Motion.Reduce(f.Frame.Word(0, 0), result.Translation);
        positions[0] = new Relation(position.Left, new Relation(receipts.Left, forgedSum));
        rows[0] = new Relation(first.Left, new Relation(S(positions), parts.Right));
        var forgedSurface = new Relation(result.Surface.Left, new Relation(new Relation(outputs.Left, S(rows)), outcome.Right));
        Assert.False(Composition.Validate(result with { Surface = forgedSurface }));
    }

    [Fact]
    public void All_curve_holder_carriers_remain_unmeasured_zero_state_under_compound_application()
    {
        var f = new Fixture(); var result = Composition.Place(f.Template, f.Quarter(), f.Frame.Word(10, 20));
        foreach (var carrier in Reachable(result.Surface).OfType<Carrier>())
        { Assert.Equal(0UL, carrier.Value); Assert.Equal(0L, carrier.Tension); }
    }

    private sealed class Fixture
    {
        public Curve.Frame Frame { get; } = new(new QualitativeMotionFrame());
        public Composition.Template Template { get; }
        public Fixture()
        {
            var mid = new Placeholder(); var sideA = new Placeholder(); var sideB = new Placeholder();
            var first = Frame.Line(new Placeholder(), mid, Frame.Word(0, 0), Frame.Word(6, 0));
            var second = Frame.Line(mid, new Placeholder(), Frame.Word(6, 0), Frame.Word(12, 0));
            var a = Band.Read(Frame, Band.Sweep(Frame, first, [Frame.Word(0, 1), Frame.Word(0, 1)], [Frame.Word(0, -1), Frame.Word(0, -1)], sideAEnd: sideA, sideBEnd: sideB));
            var b = Band.Read(Frame, Band.Sweep(Frame, second, [Frame.Word(0, 1), Frame.Word(0, 1)], [Frame.Word(0, -1), Frame.Word(0, -1)], sideAStart: sideA, sideBStart: sideB));
            var leaf = Frame.Cubic(new Placeholder(), new Placeholder(), Frame.Word(0, 2), Frame.Word(4, 7), Frame.Word(8, 7), Frame.Word(12, 2));
            var c = Band.Read(Frame, Band.Sweep(Frame, leaf,
                [Frame.Word(0, 0), Frame.Word(-1, 2), Frame.Word(1, 2), Frame.Word(0, 0)],
                [Frame.Word(0, 0), Frame.Word(1, -2), Frame.Word(-1, -2), Frame.Word(0, 0)]));
            Template = Composition.Create(Frame,
                S(Band.BandEnvelope(a.Surface, Band.Continue(Frame, a, true), Band.UnknownEnd(a, false), Band.Occupy(a)),
                  Band.BandEnvelope(b.Surface, Band.UnknownEnd(b, true), Band.Cap(Frame, b, false), Band.Occupy(b))),
                S(Band.BandEnvelope(c.Surface, Band.Cap(Frame, c, true), Band.Cap(Frame, c, false), Band.Occupy(c))));
        }
        public QualitativePatternTransform Quarter() => Composition.Map(Frame, Frame.Word(0, 1), Frame.Word(0, -1), Frame.Word(-1, 0), Frame.Word(1, 0));
    }
    private static Relation S(params IElement[] items) => StructuralConstraint.Closed(items);
    private static IElement[] E(Relation scope)
    { Assert.True(StructuralConstraint.TryReadScope(scope, out var entries, out var seam, out var tail)); Assert.Equal(seam, tail); return entries; }
    private static IEnumerable<IElement> Reachable(IElement root)
    {
        var seen = new HashSet<IElement>(ReferenceEqualityComparer.Instance); var work = new Stack<IElement>(); work.Push(root);
        while (work.Count != 0) { var item = work.Pop(); if (!seen.Add(item)) continue; yield return item; if (item is Relation r) { work.Push(r.Left); work.Push(r.Right); } }
    }
}
