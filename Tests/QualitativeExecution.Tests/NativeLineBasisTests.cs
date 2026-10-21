using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;
using Gojo.Experiments.QualitativeFrieze;

namespace QualitativeExecution.Tests;

public sealed class NativeLineBasisTests
{
    [Fact]
    public void Four_radial_markers_and_a_commuting_direction_cycle_do_not_establish_equal_turns()
    {
        var basis = new NativeLineBasis(new QualitativeMotionFrame());
        var read = basis.ReadRightAngle(Question());
        Assert.Equal("Open", read.Answer); Assert.Empty(E(read.Supported)); Assert.NotEmpty(E(read.Open));
        Assert.True(basis.Contour.TryReadClosedContour(basis.Contour.Evaluate(), out _));
        Assert.False(basis.Contour.TryReadEqualTurns(basis.Contour.Evaluate(), out _));
        Assert.Equal(4, E(basis.Contour.Markers).Length);
    }

    [Fact]
    public void Explicit_equal_turns_support_a_conditional_geometric_read_with_all_actual_correspondences()
    {
        var basis = new NativeLineBasis(new QualitativeMotionFrame(), equalTurns: true);
        var predicate = Question(); var evidence = basis.ReadRightAngle(predicate);
        Assert.Equal("Yes", evidence.Answer); Assert.Same(predicate, evidence.Predicate);
        Assert.Single(E(evidence.Supported)); Assert.Empty(E(evidence.Refuted));
        Assert.Empty(E(evidence.Blocked)); Assert.Empty(E(evidence.Open));
        var participants = OrnamentReconstruction.Participants(evidence.Surface);
        Assert.All(E(basis.DirectionMap), mapping => Assert.Contains(mapping, participants));
        Assert.All(E(basis.DirectionSuccessors), successor => Assert.Contains(successor, participants));
        Assert.Contains(basis.Contour.EqualTurnPromise, participants);
        Assert.DoesNotContain(participants, element => element is Carrier or CarrierLocation);
    }

    [Fact]
    public void A_geometric_promise_cannot_bind_the_directions_of_a_different_motion_frame()
    {
        var original = new NativeLineBasis(new QualitativeMotionFrame(), equalTurns: true);
        var foreign = new NativeLineBasis(new QualitativeMotionFrame(), original.Contour,
            original.DirectionMap, original.DirectionSuccessors);
        Assert.Equal("Blocked", foreign.ReadRightAngle(Question()).Answer);
        Assert.Equal("Yes", original.ReadRightAngle(Question()).Answer);
    }

    [Fact]
    public void An_open_correspondence_stays_open_even_when_all_known_rows_happen_to_be_present()
    {
        var original = new NativeLineBasis(new QualitativeMotionFrame(), equalTurns: true);
        var basis = new NativeLineBasis(original.Frame, original.Contour,
            StructuralConstraint.Open(E(original.DirectionMap)), original.DirectionSuccessors);
        Assert.Equal("Open", basis.ReadRightAngle(Question()).Answer);
    }

    [Fact]
    public void A_nonbijective_correspondence_cannot_turn_four_equal_contour_steps_into_four_frame_directions()
    {
        var original = new NativeLineBasis(new QualitativeMotionFrame(), equalTurns: true);
        var rows = E(original.DirectionMap); var first = (Relation)rows[0]; var second = (Relation)rows[1];
        rows[1] = new Relation(second.Left, first.Right);
        var basis = new NativeLineBasis(original.Frame, original.Contour, S(rows), original.DirectionSuccessors);
        Assert.Equal("Blocked", basis.ReadRightAngle(Question()).Answer);
    }

    [Fact]
    public void A_different_discrete_cycle_cannot_silently_replace_the_frames_selected_geometric_action()
    {
        var original = new NativeLineBasis(new QualitativeMotionFrame(), equalTurns: true);
        IElement[] wrongOrder = [original.Frame.Right, original.Frame.Left, original.Frame.Up, original.Frame.Down];
        var map = S(E(original.Contour.Markers).Zip(wrongOrder).Select(pair => (IElement)new Relation(pair.First, pair.Second)).ToArray());
        var basis = new NativeLineBasis(original.Frame, original.Contour, map, Cycle(wrongOrder));
        var action = basis.Contour.CompareAction(basis.DirectionDomain, basis.DirectionSuccessors, basis.DirectionMap);
        Assert.True(basis.Contour.TryReadActionResult(action, out var matched, out var failed, out var pending));
        Assert.Equal(4, E(matched).Length); Assert.Empty(E(failed)); Assert.Empty(E(pending));
        Assert.Equal("Blocked", basis.ReadRightAngle(Question()).Answer);
    }

    [Fact]
    public void An_unclosed_whole_does_not_supply_a_quarter_of_a_complete_turn()
    {
        var original = new NativeLineBasis(new QualitativeMotionFrame(), equalTurns: true);
        var c = original.Contour;
        var contour = new QualitativeMarkedContour(c.Center, c.ReachSupport, c.Markers,
            StructuralConstraint.Open(E(c.Traversal)), c.Reference).WithEqualTurns(new Placeholder());
        var basis = new NativeLineBasis(original.Frame, contour, original.DirectionMap, original.DirectionSuccessors);
        Assert.Equal("Open", basis.ReadRightAngle(Question()).Answer);
    }

    [Fact]
    public void Four_different_turn_promises_are_contradictory_to_the_selected_equal_subdivision()
    {
        var original = new NativeLineBasis(new QualitativeMotionFrame()); var c = original.Contour;
        var promise = new Relation(c.Traversal, S(E(c.Traversal).Select(edge => (IElement)new Relation(edge, new Placeholder())).ToArray()));
        var contour = new QualitativeMarkedContour(c.Center, c.ReachSupport, c.Markers, c.Traversal, c.Reference, promise);
        var basis = new NativeLineBasis(original.Frame, contour, original.DirectionMap, original.DirectionSuccessors);
        Assert.Equal("Blocked", basis.ReadRightAngle(Question()).Answer);
    }

    [Fact]
    public void Changing_the_reference_direction_preserves_the_same_commuting_geometric_action()
    {
        var original = new NativeLineBasis(new QualitativeMotionFrame(), equalTurns: true);
        IElement[] directions = [original.Frame.Up, original.Frame.Left, original.Frame.Down, original.Frame.Right];
        var map = S(E(original.Contour.Markers).Zip(directions).Select(pair => (IElement)new Relation(pair.First, pair.Second)).ToArray());
        var basis = new NativeLineBasis(original.Frame, original.Contour, map, original.DirectionSuccessors);
        Assert.Equal("Yes", basis.ReadRightAngle(Question()).Answer);
    }

    [Theory]
    [InlineData(false, "Open")]
    [InlineData(true, "Yes")]
    public void Native_transport_retains_the_actual_turn_promise_without_an_authoring_option(bool equalTurns, string answer)
    {
        var original = new NativeLineBasis(new QualitativeMotionFrame(), equalTurns);
        var restored = Assert.IsType<Relation>(Assert.Single(GojoNumericFormat.Decode(GojoNumericFormat.Encode(original.Surface))));
        Assert.NotSame(original.Surface, restored);
        Assert.True(NativeLineBasis.TryRead(restored, out var basis, out var witness));
        Assert.Equal(answer, basis.ReadRightAngle(Question()).Answer);
        Assert.Contains(restored, OrnamentReconstruction.Participants(witness));
        Assert.Contains(basis.Surface, OrnamentReconstruction.Participants(witness));
        IElement[] directions = [basis.Frame.Right, basis.Frame.Up, basis.Frame.Left, basis.Frame.Down];
        Assert.Equal(directions, E(basis.DirectionMap).Cast<Relation>().Select(row => row.Right));
    }

    [Fact]
    public void Raw_import_rejects_detached_radial_attachments_and_a_replaced_whole_reference()
    {
        var original = new NativeLineBasis(new QualitativeMotionFrame(), equalTurns: true);
        var c = original.Contour; var contourHead = (Relation)c.Surface.Left;
        var radii = E(c.RadialAttachments); var first = (Relation)radii[0];
        radii[0] = new Relation(first.Left, S(new Placeholder()));
        var wrongRadii = new Relation(new Relation(contourHead.Left, S(radii)), c.Surface.Right);
        var wrongWhole = new Relation(c.Surface.Left, new Relation(new Relation(c.Markers,
            new Relation(c.Reference, c.Traversal)), ((Relation)c.Surface.Right).Right));
        foreach (var shape in new[] { wrongRadii, wrongWhole })
        {
            var surface = new Relation(original.Frame.Surface,
                new Relation(shape, new Relation(original.DirectionMap, original.DirectionSuccessors)));
            Assert.False(NativeLineBasis.TryRead(surface, out _, out _));
        }
    }

    [Fact]
    public void A_larger_import_can_read_the_basis_inside_its_one_already_rebound_frame()
    {
        var original = new NativeLineBasis(new QualitativeMotionFrame(), equalTurns: true);
        var raw = Assert.IsType<Relation>(Assert.Single(GojoNumericFormat.Decode(GojoNumericFormat.Encode(original.Surface))));
        Assert.True(QualitativeMotionFrameRead.TryReadStandard((Relation)raw.Left, out var imported));
        var sharedWorld = Assert.IsType<Relation>(imported.Rebind(raw));
        Assert.True(NativeLineBasis.TryReadInFrame(imported.Target, sharedWorld, out var basis, out var witness));
        Assert.Same(imported.Target, basis.Frame);
        Assert.Equal("Yes", basis.ReadRightAngle(Question()).Answer);
        Assert.Same(((Relation)((Relation)sharedWorld.Right).Right).Left, basis.DirectionMap);
        Assert.Contains(sharedWorld, OrnamentReconstruction.Participants(witness));
    }

    [Fact]
    public void An_in_frame_read_rejects_a_different_frame_or_a_lookalike_frame_wrapper()
    {
        var original = new NativeLineBasis(new QualitativeMotionFrame(), equalTurns: true);
        Assert.False(NativeLineBasis.TryReadInFrame(new QualitativeMotionFrame(), original.Surface, out _, out _));
        var copiedFrame = new Relation(original.Frame.Surface.Left, original.Frame.Surface.Right);
        var lookalike = new Relation(copiedFrame, original.Surface.Right);
        Assert.False(NativeLineBasis.TryReadInFrame(original.Frame, lookalike, out _, out _));
        Assert.True(NativeLineBasis.TryReadInFrame(original.Frame, original.Surface, out _, out _));
    }

    private static Relation Question() => new(new Placeholder(), new Placeholder());
    private static Relation Cycle(IElement[] entries) => S(entries.Select((entry, index) =>
        (IElement)new Relation(entry, entries[(index + 1) % entries.Length])).ToArray());
    private static Relation S(params IElement[] entries) => StructuralConstraint.Closed(entries);
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
}
