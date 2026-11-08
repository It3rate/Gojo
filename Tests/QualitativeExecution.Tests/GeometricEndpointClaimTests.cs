using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;
using Gojo.Experiments.QualitativeFrieze.Composition;
using Curve = Gojo.Experiments.QualitativeExecution.QualitativeCurve;

namespace QualitativeExecution.Tests;

public sealed class GeometricEndpointClaimTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void Actual_endpoint_ports_drive_width_without_reading_display_points(int construction)
    {
        var t = construction == 0 ? CoupledShapeTemplate.CardinalCap() :
            construction == 1 ? CoupledShapeTemplate.ChevronCap() : CoupledShapeTemplate.InsetCap();
        var machine = CoupledExtentMachine.Create(1, 12);
        var claim = GeometricEndpointClaim.Create(t.Frame, machine, t.Frame.Motion.Right, t.Frame.Word(1, 0));
        for (var i = 0; i < 9; i++)
        {
            var drawing = CoupledShapeTemplate.Realize(t, 6, (int)machine.Snapshot().Value, 2, 2);
            // The display projection can be discarded without changing the read.
            Array.Clear(drawing.Upper.Points); Array.Clear(drawing.Lower.Points);
            var read = claim.Observe(drawing.Upper.EndPort, drawing.Lower.EndPort);
            Assert.True(read.Complete); Assert.True(read.GeometryComplete);
            Assert.Equal(machine.Snapshot().Value - 6, read.AlongDifference);
            machine.Step(read.Packet);
        }
        Assert.Equal(6, machine.Snapshot().Value); Assert.Equal(0, machine.Snapshot().Tension);
    }

    [Theory]
    [InlineData(-3, 4)]
    [InlineData(0, 2)]
    [InlineData(3, 1)]
    public void Reversing_the_read_axis_preserves_correction_but_reverses_signed_read(int difference, byte expected)
    {
        var f = Frame(); var machine = CoupledExtentMachine.Create(4);
        var a = Endpoint(f, 4, 2); var b = Endpoint(f, 4 + difference, 5);
        var right = GeometricEndpointClaim.Create(f, machine, f.Motion.Right, f.Word(1, 0));
        var left = GeometricEndpointClaim.Create(f, machine, f.Motion.Left, f.Word(1, 0));
        var r = right.Observe(a, b); var l = left.Observe(a, b, compiled: true);
        Assert.Equal(expected, r.Mask); Assert.Equal(expected, l.Mask);
        Assert.Equal(difference, r.AlongDifference); Assert.Equal(-difference, l.AlongDifference);
        Assert.Equal(3, r.TransverseDifference); Assert.Equal(-3, l.TransverseDifference);
        Assert.Equal(r.AxisAgreement, l.AxisAgreement);
        Assert.False(r.CoordinateCoincident); Assert.False(l.CoordinateCoincident);
    }

    [Fact]
    public void Axis_agreement_retains_native_transverse_residue_and_does_not_claim_full_coincidence()
    {
        var f = Frame(); var machine = CoupledExtentMachine.Create(3);
        var a = Endpoint(f, 4, 2); var b = Endpoint(f, 4, -1);
        var claim = GeometricEndpointClaim.Create(f, machine, f.Motion.Right, f.Word(1, 0));
        var read = claim.Observe(a, b);
        Assert.True(read.Complete); Assert.True(read.AxisAgreement); Assert.False(read.CoordinateCoincident);
        Assert.Equal(2, read.Mask); Assert.Equal(0, read.AlongDifference); Assert.Equal(-3, read.TransverseDifference);
        Assert.True(f.Vector(read.TransverseResidue, out var remainder));
        Assert.Equal(new Curve.Vector(0, -3), remainder);
        Assert.Contains(Reachable(read.Witness), item => ReferenceEquals(item, read.TransverseResidue));
        Assert.False(machine.Step(read.Packet).Changed); Assert.Equal(3, machine.Snapshot().Value);
        Assert.NotSame(a.Left, b.Left);
    }

    [Fact]
    public void Coordinate_coincidence_does_not_change_endpoint_identity()
    {
        var f = Frame(); var machine = CoupledExtentMachine.Create(2);
        var a = Endpoint(f, 0, 0); var b = Endpoint(f, 0, 0);
        var claim = GeometricEndpointClaim.Create(f, machine, f.Motion.Up, f.Word(0, 1));
        var read = claim.Observe(a, b);
        Assert.True(read.GeometryComplete); Assert.True(read.AxisAgreement); Assert.True(read.CoordinateCoincident);
        Assert.Equal(2, read.Mask); Assert.Empty(FiniteLaw.Entries(read.TransverseResidue));
        Assert.NotSame(a.Left, b.Left); Assert.NotEqual(a.Left, b.Left);
    }

    [Theory]
    [InlineData(false, false, 1)]
    [InlineData(false, true, 4)]
    [InlineData(true, false, 4)]
    [InlineData(true, true, 1)]
    public void Response_uses_both_the_selected_moving_role_and_motion_polarity(bool moveFirst, bool negativeMotion, byte expected)
    {
        var f = Frame(); var machine = CoupledExtentMachine.Create(4);
        var claim = GeometricEndpointClaim.Create(f, machine, f.Motion.Up,
            f.Word(0, negativeMotion ? -2 : 2), moveSecond: !moveFirst);
        var a = Endpoint(f, 1, 2); var b = Endpoint(f, 1, 7);
        var native = claim.Observe(a, b); var compiled = claim.Observe(a, b, compiled: true);
        Assert.Equal(expected, native.Mask); Assert.Equal(expected, compiled.Mask);
        Assert.Equal(5, native.AlongDifference);
    }

    [Fact]
    public void A_response_with_a_transverse_component_only_promises_selected_axis_correction()
    {
        var f = Frame(); var machine = CoupledExtentMachine.Create(4);
        var claim = GeometricEndpointClaim.Create(f, machine, f.Motion.Right, f.Word(1, 2));
        var read = claim.Observe(Endpoint(f, 5, 0), Endpoint(f, 3, 1));
        Assert.True(read.Complete); Assert.Equal(4, read.Mask);
        Assert.Equal(-2, read.AlongDifference); Assert.Equal(1, read.TransverseDifference);
        Assert.False(read.CoordinateCoincident);
    }

    [Theory]
    [InlineData(false, false, 0)]
    [InlineData(true, true, 7)]
    public void Unavailable_or_ambiguous_publication_does_not_actuate_private_exact_geometry(bool available, bool ambiguous, byte expected)
    {
        var f = Frame(); var machine = CoupledExtentMachine.Create(4);
        var claim = GeometricEndpointClaim.Create(f, machine, f.Motion.Right, f.Word(1, 0));
        var a = Endpoint(f, 5, 0); var b = Endpoint(f, 3, 0);
        var read = claim.Observe(a, b, available, ambiguous);
        var compiled = claim.Observe(a, b, available, ambiguous, compiled: true);
        Assert.True(read.Complete); Assert.Equal(-2, read.AlongDifference);
        Assert.Equal(expected, read.Mask); Assert.Equal(expected, compiled.Mask);
        Assert.False(machine.Step(read.Packet).Resolved);
        Assert.Equal(4, machine.Snapshot().Value); Assert.Equal(0, machine.Snapshot().Tension);
    }

    [Fact]
    public void Missing_or_foreign_position_words_and_axis_remain_unresolved()
    {
        var f = Frame(); var foreign = Frame(); var machine = CoupledExtentMachine.Create(4);
        var claim = GeometricEndpointClaim.Create(f, machine, f.Motion.Right, f.Word(1, 0));
        var a = Endpoint(f, 5, 0);
        var sameMotionDifferentReach = new Curve.Frame(f.Motion);
        var invalid = new[] {
            new Relation(new Placeholder(), StructuralConstraint.Open()),
            Endpoint(foreign, 3, 0),
            Endpoint(sameMotionDifferentReach, 3, 0),
            new Relation(new Placeholder(), new Placeholder())
        };
        foreach (var b in invalid)
        {
            var read = claim.Observe(a, b);
            Assert.False(read.GeometryComplete); Assert.False(read.Complete); Assert.Equal(0, read.Mask);
            Assert.Null(read.AlongDifference); Assert.Null(read.TransverseDifference);
            Assert.Null(read.AxisAgreement); Assert.Null(read.CoordinateCoincident);
            Assert.False(machine.Step(read.Packet).Resolved);
        }
        var noAxis = GeometricEndpointClaim.Create(f, machine, StructuralConstraint.Open(), f.Word(1, 0));
        var foreignAxis = GeometricEndpointClaim.Create(f, machine, foreign.Motion.Right, f.Word(1, 0));
        foreach (var broken in new[] { noAxis, foreignAxis })
        {
            var read = broken.Observe(a, Endpoint(f, 3, 0));
            Assert.False(read.GeometryComplete); Assert.Equal(0, read.Mask);
        }
    }

    [Fact]
    public void Missing_tick_correspondence_or_zero_effect_keeps_geometry_but_withholds_response()
    {
        var f = Frame(); var machine = CoupledExtentMachine.Create(4);
        var baseClaim = GeometricEndpointClaim.Create(f, machine, f.Motion.Right, f.Word(1, 0));
        var fields = FiniteLaw.Entries((Relation)baseClaim.Surface.Right);
        fields[2] = StructuralConstraint.Open(fields[2]);
        var missingCalibration = GeometricEndpointClaim.FromSurface(f,
            new Relation(baseClaim.Surface.Left, StructuralConstraint.Closed(fields)));
        var claims = new[] {
            missingCalibration,
            GeometricEndpointClaim.Create(f, machine, f.Motion.Right, f.Word(0, 0)),
            GeometricEndpointClaim.Create(f, machine, f.Motion.Right, f.Word(0, 1)),
            GeometricEndpointClaim.Create(f, machine, f.Motion.Right, StructuralConstraint.Open())
        };
        foreach (var claim in claims)
        {
            var read = claim.Observe(Endpoint(f, 5, 1), Endpoint(f, 3, 2));
            Assert.True(read.GeometryComplete); Assert.False(read.Complete); Assert.Equal(0, read.Mask);
            Assert.Equal(-2, read.AlongDifference); Assert.Equal(1, read.TransverseDifference);
            Assert.False(machine.Step(read.Packet).Resolved);
        }
        Assert.Throws<ArgumentException>(() => GeometricEndpointClaim.FromSurface(Frame(), baseClaim.Surface));
    }

    [Fact]
    public void Public_packet_contains_no_private_geometry_roles_or_receiver_actuator_port()
    {
        var f = Frame(); var machine = CoupledExtentMachine.Create(4);
        var claim = GeometricEndpointClaim.Create(f, machine, f.Motion.Right, f.Word(1, 0));
        var a = Endpoint(f, 5, 0); var b = Endpoint(f, 3, 1); var read = claim.Observe(a, b);
        var packet = Reachable(read.Packet).ToArray(); var witness = Reachable(read.Witness).ToArray();
        foreach (var item in new IElement[] { a, b, a.Left, b.Left, f.Surface, claim.Surface, machine.ExtentPort, read.TransverseResidue })
        {
            Assert.DoesNotContain(packet, value => ReferenceEquals(value, item));
            Assert.Contains(witness, value => ReferenceEquals(value, item));
        }
        Assert.Throws<ArgumentException>(() => claim.Observe(a, b, eventIdentity: a));
    }

    [Fact]
    public void Same_event_is_consumed_once_and_reading_alone_never_mutates_the_receiver()
    {
        var f = Frame(); var machine = CoupledExtentMachine.Create(4);
        var claim = GeometricEndpointClaim.Create(f, machine, f.Motion.Right, f.Word(1, 0));
        var before = machine.Snapshot(); var occurrence = new Placeholder();
        var read = claim.Observe(Endpoint(f, 5, 0), Endpoint(f, 3, 0), eventIdentity: occurrence);
        Assert.Equal(before.Value, machine.Snapshot().Value); Assert.Equal(before.Tension, machine.Snapshot().Tension);
        Assert.True(machine.Step(read.Packet).ConsumedEvent); Assert.Equal(5, machine.Snapshot().Value);
        Assert.False(machine.Step(read.Packet).ConsumedEvent); Assert.Equal(5, machine.Snapshot().Value);
    }

    private static Curve.Frame Frame() => new(new QualitativeMotionFrame());
    private static Relation Endpoint(Curve.Frame frame, int x, int y) => new(new Placeholder(), frame.Word(x, y));
    private static IEnumerable<IElement> Reachable(IElement root)
    {
        var seen = new HashSet<IElement>(ReferenceEqualityComparer.Instance);
        var todo = new Stack<IElement>(); todo.Push(root);
        while (todo.TryPop(out var item))
        {
            if (!seen.Add(item)) continue;
            yield return item;
            if (item is Relation r) { todo.Push(r.Left); todo.Push(r.Right); }
        }
    }
}
