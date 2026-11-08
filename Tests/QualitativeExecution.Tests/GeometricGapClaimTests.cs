using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;
using Gojo.Experiments.QualitativeFrieze.Composition;
using Curve = Gojo.Experiments.QualitativeExecution.QualitativeCurve;

namespace QualitativeExecution.Tests;

public sealed class GeometricGapClaimTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Actual_parallel_section_gap_drives_the_height_without_target_outline(bool chevron)
    {
        var template = chevron ? CoupledShapeTemplate.ChevronCap() : CoupledShapeTemplate.CardinalCap();
        var actuator = CoupledExtentMachine.Create(1, 8);
        var desired = new Carrier(actuator.StepUnit.UnitShape); desired.Add(6);
        var claim = GeometricGapClaim.Create(template.Frame, Port(desired), actuator,
            template.Frame.Motion.Down, template.Frame.Word(0, -1));
        for (var i = 0; i < 5; i++)
        {
            var drawing = CoupledShapeTemplate.Realize(template, 8, 8, 2, (int)actuator.Snapshot().Value);
            var read = claim.Observe(Section(drawing.Upper, 1), Section(drawing.Lower, 1));
            Assert.True(read.Complete);
            Assert.Equal(2 + actuator.Snapshot().Value, read.Gap);
            actuator.Step(read.Packet);
        }
        Assert.Equal(4, actuator.Snapshot().Value); Assert.Equal(0, actuator.Snapshot().Tension);
        Assert.Equal(6UL, desired.Value);
    }

    [Fact]
    public void The_connected_motion_polarity_changes_response_without_changing_gap()
    {
        var f = new Curve.Frame(new QualitativeMotionFrame());
        var machine = CoupledExtentMachine.Create(1); var desired = new Carrier(machine.StepUnit.UnitShape); desired.Add(5);
        var upper = Line(f, 0, 2, 6, 2); var lower = Line(f, 0, -1, 6, -1);
        var outward = GeometricGapClaim.Create(f, Port(desired), machine, f.Motion.Down, f.Word(0, -1));
        var inward = GeometricGapClaim.Create(f, Port(desired), machine, f.Motion.Down, f.Word(0, 1));
        var moveFirst = GeometricGapClaim.Create(f, Port(desired), machine, f.Motion.Down, f.Word(0, 1), moveSecond: false);
        Assert.Equal(4, outward.Observe(upper, lower).Mask);
        Assert.Equal(1, inward.Observe(upper, lower).Mask);
        Assert.Equal(4, moveFirst.Observe(upper, lower).Mask);
        Assert.Equal(3, outward.Observe(upper, lower).Gap);
    }

    [Fact]
    public void Traversal_reversal_preserves_the_gap_but_does_not_weld_the_sites()
    {
        var f = new Curve.Frame(new QualitativeMotionFrame());
        var machine = CoupledExtentMachine.Create(1); var desired = new Carrier(machine.StepUnit.UnitShape); desired.Add(3);
        var a = Line(f, 0, 2, 6, 2); var b = Line(f, 0, -1, 6, -1);
        var claim = GeometricGapClaim.Create(f, Port(desired), machine, f.Motion.Down, f.Word(0, -1));
        var forward = claim.Observe(a, b); var reverse = claim.Observe(a, f.Reverse(b));
        Assert.Equal(2, forward.Mask); Assert.Equal(forward.Gap, reverse.Gap); Assert.Equal(forward.Mask, reverse.Mask);
        Assert.NotSame(f.Read(a).Start, f.Read(b).Start);
    }

    [Fact]
    public void Zero_gap_between_independent_sections_is_not_missing_participation()
    {
        var f = new Curve.Frame(new QualitativeMotionFrame());
        var machine = CoupledExtentMachine.Create(0); var desired = new Carrier(machine.StepUnit.UnitShape); desired.Add(2);
        var a = Line(f, 0, 0, 6, 0); var b = Line(f, 0, 0, 6, 0);
        var claim = GeometricGapClaim.Create(f, Port(desired), machine, f.Motion.Down, f.Word(0, -1));
        var read = claim.Observe(a, b);
        Assert.True(read.Complete); Assert.Equal(0, read.Gap); Assert.Equal(4, read.Mask);
        Assert.NotSame(f.Read(a).Start, f.Read(b).Start);
    }

    [Fact]
    public void Unsupported_geometry_retains_open_evidence_instead_of_using_a_bounding_box()
    {
        var f = new Curve.Frame(new QualitativeMotionFrame());
        var machine = CoupledExtentMachine.Create(1); var desired = new Carrier(machine.StepUnit.UnitShape); desired.Add(4);
        var claim = GeometricGapClaim.Create(f, Port(desired), machine, f.Motion.Down, f.Word(0, -1));
        var upper = Line(f, 0, 2, 6, 2);
        var bad = new[] {
            Line(f, 7, -1, 9, -1), // No longitudinal overlap.
            Line(f, 6, -1, 9, -1), // A point of overlap does not establish a corridor.
            Line(f, 0, -1, 6, -2), // Sloped second boundary.
            Line(f, 0, -1, 0, -1), // No nonzero section.
            Line(f, 0, 4, 6, 4), // Wrong side of the explicitly selected normal.
            f.Quadratic(new Placeholder(), new Placeholder(), f.Word(0,-1), f.Word(3,-2), f.Word(6,-1))
        };
        foreach (var lower in bad)
        {
            var read = claim.Observe(upper, lower);
            Assert.False(read.Complete); Assert.Null(read.Gap); Assert.Equal(0, read.Mask);
            Assert.False(machine.Step(read.Packet).Resolved);
        }
        Assert.Equal(1, machine.Snapshot().Value);
    }

    [Theory]
    [InlineData(false, false, 0)]
    [InlineData(true, true, 7)]
    public void Unavailable_or_ambiguous_gap_does_not_become_a_correction(bool available, bool ambiguous, byte mask)
    {
        var f = new Curve.Frame(new QualitativeMotionFrame());
        var machine = CoupledExtentMachine.Create(1); var desired = new Carrier(machine.StepUnit.UnitShape); desired.Add(4);
        var claim = GeometricGapClaim.Create(f, Port(desired), machine, f.Motion.Down, f.Word(0, -1));
        var a = Line(f, 0, 1, 6, 1); var b = Line(f, 0, -1, 6, -1);
        var native = claim.Observe(a, b, available, ambiguous);
        var compiled = claim.Observe(a, b, available, ambiguous, compiled: true);
        Assert.True(native.Complete); Assert.Equal(2, native.Gap);
        Assert.Equal(mask, native.Mask); Assert.Equal(native.Mask, compiled.Mask);
        Assert.False(machine.Step(native.Packet).Resolved); Assert.Equal(1, machine.Snapshot().Value);
    }

    [Fact]
    public void No_correspondence_or_no_response_direction_stays_unresolved()
    {
        var f = new Curve.Frame(new QualitativeMotionFrame()); var machine = CoupledExtentMachine.Create(1);
        var foreign = new Carrier(); foreign.Add(4);
        var invalidUnit = GeometricGapClaim.Create(f, Port(foreign), machine, f.Motion.Down, f.Word(0,-1));
        var desired = new Carrier(machine.StepUnit.UnitShape); desired.Add(4);
        var noMotion = GeometricGapClaim.Create(f, Port(desired), machine, f.Motion.Down, f.Word(0,0));
        var alongBoundary = GeometricGapClaim.Create(f, Port(desired), machine, f.Motion.Down, f.Word(1,0));
        var a = Line(f,0,1,6,1); var b = Line(f,0,-1,6,-1);
        foreach (var claim in new[] { invalidUnit, noMotion, alongBoundary })
        {
            var read = claim.Observe(a,b);
            Assert.False(read.Complete); Assert.Equal(0,read.Mask);
        }
    }

    [Fact]
    public void Packet_does_not_expose_sections_goal_or_response_route()
    {
        var f = new Curve.Frame(new QualitativeMotionFrame()); var machine = CoupledExtentMachine.Create(1);
        var desired = new Carrier(machine.StepUnit.UnitShape); desired.Add(4);
        var claim = GeometricGapClaim.Create(f, Port(desired), machine, f.Motion.Down, f.Word(0,-1));
        var a = Line(f,0,1,6,1); var b = Line(f,0,-1,6,-1); var read = claim.Observe(a,b);
        var packet = Reachable(read.Packet).ToArray(); var witness = Reachable(read.Witness).ToArray();
        foreach (var item in new IElement[] { a,b,claim.Surface,desired,machine.ExtentPort })
        { Assert.DoesNotContain(packet, x=>ReferenceEquals(x,item)); Assert.Contains(witness,x=>ReferenceEquals(x,item)); }
        Assert.Throws<ArgumentException>(()=>claim.Observe(a,b,eventIdentity:a));
    }

    [Fact]
    public void Inset_response_can_reach_a_gap_on_its_lattice_but_does_not_invent_fractional_height()
    {
        var t = CoupledShapeTemplate.InsetCap(); var machine = CoupledExtentMachine.Create(1,8);
        var desired = new Carrier(machine.StepUnit.UnitShape); desired.Add(9);
        var claim = GeometricGapClaim.Create(t.Frame, Port(desired), machine, t.Frame.Motion.Down, t.Frame.Word(0,-3));
        GeometricGapClaim.Observation Read()
        {
            var shape = CoupledShapeTemplate.Realize(t,8,8,1,(int)machine.Snapshot().Value);
            return claim.Observe(Section(shape.Upper,1),Section(shape.Lower,1));
        }
        machine.Step(Read().Packet); Assert.Equal(2,machine.Snapshot().Value); Assert.Equal(2,Read().Mask);
        desired.Set(10);
        var gaps = new List<long?>();
        for(var i=0;i<4;i++) { var read=Read(); gaps.Add(read.Gap); machine.Step(read.Packet); }
        Assert.Equal(new long?[]{9,12,9,12},gaps); Assert.NotEqual(2,Read().Mask);
    }

    private static Relation Port(Carrier carrier)=>new(carrier.ValueSlot,carrier);
    private static Relation Line(Curve.Frame f,int x0,int y0,int x1,int y1)=>
        f.Line(new Placeholder(),new Placeholder(),f.Word(x0,y0),f.Word(x1,y1));
    private static Relation Section(CoupledShapeTemplate.RouteRead route,int index)=>(Relation)FiniteLaw.Entries(route.Sections)[index];
    private static IEnumerable<IElement> Reachable(IElement root)
    {
        var seen = new HashSet<IElement>(ReferenceEqualityComparer.Instance); var todo = new Stack<IElement>(); todo.Push(root);
        while(todo.TryPop(out var item)) { if(!seen.Add(item)) continue; yield return item;
            if(item is Relation r) { todo.Push(r.Left);todo.Push(r.Right); } }
    }
}
