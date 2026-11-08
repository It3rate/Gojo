using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;
using Gojo.Experiments.QualitativeFrieze.Composition;
using Curve = Gojo.Experiments.QualitativeExecution.QualitativeCurve;

namespace QualitativeExecution.Tests;

public sealed class GeometricGapAlternativeTests
{
    [Fact]
    public void Collapsed_section_keeps_failed_gap_but_admits_landmark_correction()
    {
        var f = Frame(); var m = CoupledExtentMachine.Create(1, 8); var goal = Goal(m, 5);
        var claim = Claim(f, m, goal);
        var upper = Line(f, 0, 2, 6, 2); var lower = Line(f, 2, -1, 2, -1);
        var before = m.Snapshot(); var read = claim.Observe(upper, lower, Start(f, upper), Start(f, lower));
        Assert.False(read.PrimaryComplete); Assert.Null(read.PrimaryGap); Assert.Equal(0, read.PrimaryMask);
        Assert.True(read.AlternativeComplete); Assert.Equal(3, read.AlternativeGap);
        Assert.Equal(3, read.SelectedGap); Assert.True(read.UsedAlternative); Assert.Equal(4, read.Mask);
        Assert.Equal(before.Value, m.Snapshot().Value); Assert.Equal(before.Tension, m.Snapshot().Tension);
        Assert.Equal(1, m.Step(read.Packet).RequestedDelta); Assert.Equal(2, m.Snapshot().Value);
        var graph = Reachable(read.Witness).ToArray();
        Assert.Contains(graph, item => ReferenceEquals(item, upper)); Assert.Contains(graph, item => ReferenceEquals(item, lower));
        Assert.Contains(graph, item => ReferenceEquals(item, read.PrimaryPacket));
    }

    [Fact]
    public void Preferred_section_read_wins_and_disagreeing_landmark_evidence_remains()
    {
        var f = Frame(); var m = CoupledExtentMachine.Create(1, 8); var goal = Goal(m, 5); var claim = Claim(f, m, goal);
        var upper = Line(f, 0, 2, 6, 2); var lower = Line(f, 0, -1, 6, -1);
        // These supplied landmarks ask a different question, visibly bound in
        // the retained alternative. They do not silently redefine section gap.
        var a = new Relation(new Placeholder(), f.Word(0, 3)); var b = new Relation(new Placeholder(), f.Word(0, -4));
        var read = claim.Observe(upper, lower, a, b);
        Assert.True(read.PrimaryComplete); Assert.True(read.AlternativeComplete);
        Assert.Equal(3, read.PrimaryGap); Assert.Equal(7, read.AlternativeGap);
        Assert.Equal(4, read.PrimaryMask); Assert.Equal(1, read.AlternativeMask);
        Assert.Equal(4, read.Mask); Assert.Equal(3, read.SelectedGap); Assert.False(read.UsedAlternative);
        Assert.Contains(Reachable(read.Witness), item => ReferenceEquals(item, read.AlternativePacket));
    }

    [Fact]
    public void Landmark_agreement_does_not_prove_a_uniform_gap_or_join_sites()
    {
        var f = Frame(); var m = CoupledExtentMachine.Create(); var goal = Goal(m, 3); var claim = Claim(f, m, goal);
        var upper = Line(f, 0, 2, 6, 2); var lower = Line(f, 20, -1, 20, -1);
        var a = Start(f, upper); var b = Start(f, lower); var read = claim.Observe(upper, lower, a, b);
        Assert.True(read.UsedAlternative); Assert.Equal(2, read.Mask); Assert.Equal(3, read.AlternativeGap);
        Assert.False(read.PrimaryComplete); Assert.Null(read.PrimaryGap); Assert.NotEqual(a.Left, b.Left);
        Assert.Equal(0, m.Step(read.Packet).RequestedDelta);
    }

    [Theory]
    [InlineData(false, false, 0)]
    [InlineData(true, true, 7)]
    public void Missing_or_ambiguous_observation_is_not_bypassed_by_the_other_read(bool available, bool ambiguous, byte offered)
    {
        var f = Frame(); var m = CoupledExtentMachine.Create(1, 8); var goal = Goal(m, 5); var claim = Claim(f, m, goal);
        var upper = Line(f, 0, 2, 6, 2); var lower = Line(f, 2, -1, 2, -1);
        var read = claim.Observe(upper, lower, Start(f, upper), Start(f, lower), available, ambiguous);
        Assert.Equal(3, read.AlternativeGap); Assert.Equal(offered, read.AlternativeMask);
        Assert.Equal(0, read.Mask); Assert.Null(read.SelectedGap); Assert.False(read.UsedAlternative);
        Assert.Null(m.Step(read.Packet).RequestedDelta); Assert.Equal(1, m.Snapshot().Value);
    }

    [Theory]
    [InlineData(false, false, 4)]
    [InlineData(false, true, 1)]
    [InlineData(true, false, 1)]
    [InlineData(true, true, 4)]
    public void Response_tracks_selected_role_and_motion_polarity(bool moveFirst, bool negativeMotion, byte expected)
    {
        var f = Frame(); var m = CoupledExtentMachine.Create(3, 8); var goal = Goal(m, 5);
        var claim = GeometricGapAlternative.Create(f, Port(goal), m, f.Motion.Down,
            f.Word(0, negativeMotion ? 1 : -1), moveSecond: !moveFirst);
        var upper = Line(f, 0, 2, 6, 2); var lower = Line(f, 2, -1, 2, -1);
        var read = claim.Observe(upper, lower, Start(f, upper), Start(f, lower));
        var compiled = claim.Observe(upper, lower, Start(f, upper), Start(f, lower), compiled: true);
        Assert.True(read.UsedAlternative); Assert.Equal(expected, read.Mask); Assert.Equal(expected, compiled.Mask);
    }

    [Fact]
    public void Missing_foreign_landmark_and_target_capacity_preserve_unresolved_response()
    {
        var f = Frame(); var m = CoupledExtentMachine.Create(1, 8); var goal = Goal(m, 5);
        var upper = Line(f, 0, 2, 6, 2); var lower = Line(f, 2, -1, 2, -1);
        var claim = Claim(f, m, goal);
        foreach (var b in new[] { new Relation(new Placeholder(), Frame().Word(2, -1)),
            new Relation(new Placeholder(), StructuralConstraint.Open()) })
        {
            var read = claim.Observe(upper, lower, Start(f, upper), b);
            Assert.False(read.AlternativeComplete); Assert.Equal(0, read.Mask); Assert.Null(read.AlternativeGap);
        }
        var bounded = GeometricGapAlternative.Create(f, Port(goal), m, f.Motion.Down, f.Word(0, -1), maxTargetReaches: 4);
        var limited = bounded.Observe(upper, lower, Start(f, upper), Start(f, lower));
        Assert.Equal(0, limited.Mask); Assert.Null(limited.AlternativeGap); Assert.False(limited.AlternativeComplete);
    }

    [Fact]
    public void Unequal_units_require_the_explicit_target_and_motion_calibration()
    {
        var f = Frame(); var desired = new Carrier(); desired.Add(5);
        var receiver = CoupledExtentMachine.Create(1, 8, unit: UnitShape.SubdivisionOf(desired.UnitShape, 3));
        var upper = Line(f, 0, 2, 6, 2); var lower = Line(f, 2, -1, 2, -1);
        var ordinary = Claim(f, receiver, desired);
        var calibrated = GeometricGapAlternative.CreateCalibrated(f, Port(desired), receiver, f.Motion.Down, f.Word(0, -1));
        var no = ordinary.Observe(upper, lower, Start(f, upper), Start(f, lower));
        var yes = calibrated.Observe(upper, lower, Start(f, upper), Start(f, lower));
        Assert.Equal(0, no.Mask); Assert.False(no.AlternativeComplete);
        Assert.Equal(4, yes.Mask); Assert.True(yes.UsedAlternative); Assert.Equal(3, yes.AlternativeGap);
    }

    [Fact]
    public void Public_packet_stays_small_in_meaning_and_replay_is_not_a_new_command()
    {
        var f = Frame(); var m = CoupledExtentMachine.Create(1, 8); var goal = Goal(m, 5); var claim = Claim(f, m, goal);
        var upper = Line(f, 0, 2, 6, 2); var lower = Line(f, 2, -1, 2, -1);
        var read = claim.Observe(upper, lower, Start(f, upper), Start(f, lower));
        var packet = Reachable(read.Packet).ToArray();
        foreach (var privateItem in new IElement[] { upper, lower, claim.Surface, f.Surface, read.PrimaryPacket, read.AlternativePacket, m.ExtentPort })
            Assert.DoesNotContain(packet, item => ReferenceEquals(item, privateItem));
        Assert.True(m.Step(read.Packet).ConsumedEvent); Assert.False(m.Step(read.Packet).ConsumedEvent);
        Assert.Equal(2, m.Snapshot().Value);
    }

    [Fact]
    public void A_decoded_alternative_retains_frame_receiver_and_goal_aliases()
    {
        var f = Frame(); var m = CoupledExtentMachine.Create(1, 8); var goal = Goal(m, 5); var claim = Claim(f, m, goal);
        var upper = Line(f, 0, 2, 6, 2); var lower = Line(f, 2, -1, 2, -1);
        var roots = GojoNumericFormat.Decode(GojoNumericFormat.Encode(f.Surface, claim.Surface, m.ArchiveSurface(),
            upper, lower, Start(f, upper), Start(f, lower)));
        var frame = Curve.Frame.FromSurface((Relation)roots[0]);
        var restored = GeometricGapAlternative.FromSurface(frame, (Relation)roots[1]);
        var receiver = CoupledExtentMachine.Restore((Relation)roots[2]);
        var read = restored.Observe((Relation)roots[3], (Relation)roots[4], (Relation)roots[5], (Relation)roots[6], compiled: true);
        Assert.True(read.UsedAlternative); Assert.Equal(3, read.AlternativeGap); Assert.Equal(5, read.DesiredGap);
        Assert.Equal(1, receiver.Step(read.Packet).RequestedDelta); Assert.Equal(2, receiver.Snapshot().Value);
        Assert.Equal(1, m.Snapshot().Value);
        Assert.Throws<ArgumentException>(() => GeometricGapAlternative.FromSurface(f, (Relation)roots[1]));
    }

    [Fact]
    public void Replacing_first_preference_with_a_different_law_is_not_read_as_the_same_selector()
    {
        var f = Frame(); var m = CoupledExtentMachine.Create(); var goal = Goal(m, 5); var claim = Claim(f, m, goal);
        var entries = FiniteLaw.Entries((Relation)claim.Surface.Right);
        entries[3] = ExtentResponseArbiter.Create(m, "second").Surface;
        var changed = new Relation(claim.Surface.Left, StructuralConstraint.Closed(entries));
        Assert.Throws<ArgumentException>(() => GeometricGapAlternative.FromSurface(f, changed));
    }

    private static Curve.Frame Frame() => new(new QualitativeMotionFrame());
    private static Relation Line(Curve.Frame f, int x0, int y0, int x1, int y1) =>
        f.Line(new Placeholder(), new Placeholder(), f.Word(x0, y0), f.Word(x1, y1));
    private static Relation Start(Curve.Frame f, Relation line)
    { var read = f.Read(line); return new(read.Start, FiniteLaw.Entries(read.ControlWords)[0]); }
    private static Relation Port(Carrier carrier) => new(carrier.ValueSlot, carrier);
    private static Carrier Goal(CoupledExtentMachine machine, ulong count)
    { var goal = new Carrier(machine.StepUnit.UnitShape); goal.Add(count); return goal; }
    private static GeometricGapAlternative Claim(Curve.Frame frame, CoupledExtentMachine receiver, Carrier goal) =>
        GeometricGapAlternative.Create(frame, Port(goal), receiver, frame.Motion.Down, frame.Word(0, -1));
    private static IEnumerable<IElement> Reachable(IElement root)
    {
        var seen = new HashSet<IElement>(ReferenceEqualityComparer.Instance); var todo = new Stack<IElement>(); todo.Push(root);
        while (todo.TryPop(out var item))
        {
            if (!seen.Add(item)) continue; yield return item;
            if (item is Relation r) { todo.Push(r.Left); todo.Push(r.Right); }
        }
    }
}
