using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;
using Gojo.Experiments.QualitativeFrieze.Composition;
using Curve = Gojo.Experiments.QualitativeExecution.QualitativeCurve;

namespace QualitativeExecution.Tests;

public sealed class AdaptiveCalibrationTests
{
    [Fact]
    public void Calibrated_publication_projects_coarse_ticks_without_mutating_any_source()
    {
        var source = new Carrier(); source.Add(2); var port = new Relation(source.ValueSlot, source);
        var basis = new Carrier(UnitShape.SubdivisionOf(source.UnitShape, 3));
        var template = CoupledShapeTemplate.CardinalCap();
        var binding = CalibratedShapePublication.Bind(port, basis, 3);
        var publisher = new CalibratedShapePublication(template, basis, binding, binding, binding, binding);
        var read = publisher.Observe(); Assert.True(read.Complete); Assert.NotNull(read.Picture);
        Assert.Equal(new[] { 6, 0 }, read.Picture.Upper.Points[^1]);
        Assert.Equal(new[] { 0, 6 }, read.Picture.Upper.Points[1]);
        Assert.Equal(2UL, source.Value); Assert.Equal(0, source.Tension); Assert.Equal(0UL, basis.Value);
        Assert.Contains(Reachable(read.Witness), item => item is Relation { Left: var actual, Right: CarrierLocation sample } &&
            ReferenceEquals(actual, port) && ReferenceEquals(sample.Carrier, source) &&
            !sample.SameTickRefAs(source.ValueSlot) && sample.Ticks == 2);
        source.Add(1);
        Assert.Equal(new[] { 6, 0 }, read.Picture.Upper.Points[^1]);
        Assert.Equal(new[] { 9, 0 }, publisher.Observe().Picture!.Upper.Points[^1]);
    }

    [Fact]
    public void Fractional_projection_residue_does_not_turn_into_a_zero_length_role()
    {
        var source = new Carrier(); source.Add(1); var port = new Relation(source.ValueSlot, source); var basis = new Carrier();
        var half = new Relation(port, new Relation(Relation.Segment(source, 0, 2), Relation.Segment(basis, 0, 1)));
        var publisher = new CalibratedShapePublication(CoupledShapeTemplate.CardinalCap(), basis, half, half, half, half);
        var read = publisher.Observe(); Assert.False(read.Complete); Assert.Null(read.Picture);
        Assert.Contains(Reachable(read.Witness), item => item is Relation projection && projection.TryReadProjection(out _, out var residue) &&
            ProjectionResidue.TryRead(residue, out var c, out var n, out _) && ReferenceEquals(c, source) && n == 1);
        Assert.Equal(1UL, source.Value); Assert.Equal(0, source.Tension); Assert.Equal(0UL, basis.Value);
    }

    [Fact]
    public void An_unbound_or_snapshot_port_does_not_supply_a_live_role()
    {
        var source = new Carrier(); source.Add(1); var basis = new Carrier();
        var snapshot = new Relation(new CarrierLocation(source, 1), source);
        var calibration = new Relation(Relation.Segment(source, 0, 1), Relation.Segment(basis, 0, 1));
        foreach (var invalid in new[] { new Relation(StructuralConstraint.Open(), calibration), new Relation(snapshot, calibration) })
        {
            var publisher = new CalibratedShapePublication(CoupledShapeTemplate.CardinalCap(), basis, invalid, invalid, invalid, invalid);
            var read = publisher.Observe(); Assert.False(read.Complete); Assert.Null(read.Picture);
        }
    }

    [Fact]
    public void Explicit_motion_calibration_allows_distinct_units_and_retains_transverse_effect()
    {
        var frame = new Curve.Frame(new QualitativeMotionFrame()); var machine = CoupledExtentMachine.Create(2);
        var goal = new Carrier(UnitShape.SubdivisionOf(machine.StepUnit.UnitShape, 3)); goal.Add(5);
        var port = new Relation(goal.ValueSlot, goal); var motion = frame.Word(1, -3);
        var first = frame.Line(new Placeholder(), new Placeholder(), frame.Word(0, 2), frame.Word(6, 2));
        var second = frame.Line(new Placeholder(), new Placeholder(), frame.Word(0, -1), frame.Word(6, -1));
        var legacy = GeometricGapClaim.Create(frame, port, machine, frame.Motion.Down, motion);
        var calibrated = GeometricGapClaim.CreateCalibrated(frame, port, machine, frame.Motion.Down, motion);
        Assert.False(legacy.Observe(first, second).Complete);
        var read = calibrated.Observe(first, second); var compiled = calibrated.Observe(first, second, compiled: true);
        Assert.True(read.Complete); Assert.Equal(3, read.Gap); Assert.Equal(5, read.DesiredGap); Assert.Equal(4, read.Mask);
        Assert.Equal(read.Mask, compiled.Mask); Assert.Equal(2, machine.Snapshot().Value);
        Assert.Contains(Reachable(read.Witness), item => ReferenceEquals(item, motion));
        Assert.Equal(1, machine.Step(read.Packet).RequestedDelta);
    }

    private static IEnumerable<IElement> Reachable(IElement root)
    {
        var seen = new HashSet<IElement>(ReferenceEqualityComparer.Instance); var todo = new Stack<IElement>(); todo.Push(root);
        while (todo.TryPop(out var value))
        {
            if (!seen.Add(value)) continue; yield return value;
            if (value is Relation relation) { todo.Push(relation.Left); todo.Push(relation.Right); }
        }
    }
}
