using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;
using Gojo.Experiments.QualitativeFrieze.Composition;
using Curve = Gojo.Experiments.QualitativeExecution.QualitativeCurve;

namespace QualitativeExecution.Tests;

public sealed class QualitativeFrameRestoreTests
{
    [Fact]
    public void Rebinding_retains_the_captured_frame_directions_and_native_laws()
    {
        var original = new QualitativeMotionFrame(); var reach = new Placeholder();
        var before = S(QualitativeMotionFrame.Move(original.Right, reach), QualitativeMotionFrame.Move(original.Up, reach));
        var incoming = S(QualitativeMotionFrame.Move(original.Left, reach));
        var witness = original.Reduce(before, incoming);
        var roots = GojoNumericFormat.Decode(GojoNumericFormat.Encode(original.Surface, before, incoming, witness));
        var surface = Assert.IsType<Relation>(roots[0]);
        var saved = GojoNumericFormat.Encode(roots);
        var restored = QualitativeMotionFrame.FromSurface(surface);
        Assert.Same(surface, restored.Surface); Assert.NotSame(original.Surface, restored.Surface);
        Assert.Same(((Relation)surface.Left).Left, restored.Origin);
        Assert.Same(((Relation)((Relation)surface.Left).Right).Left, restored.Horizontal);
        Assert.Same(restored.Horizontal, restored.Right); Assert.Same(restored.Vertical, restored.Up);
        Assert.Same(restored.Right.Left, restored.Left.Right); Assert.Same(restored.Right.Right, restored.Left.Left);
        Assert.True(restored.TryReadResult((Relation)roots[3], out var remaining, out var cancelled, out var pending));
        Assert.Single(E(remaining)); Assert.Single(E(cancelled)); Assert.Empty(E(pending));
        Assert.Same(restored.Up, ((Relation)E(remaining)[0]).Left);
        var repeated = restored.Reduce((Relation)roots[1], (Relation)roots[2]);
        Assert.True(restored.TryReadResult(repeated, out remaining, out cancelled, out pending));
        Assert.Single(E(remaining)); Assert.Single(E(cancelled)); Assert.Empty(E(pending));
        Assert.False(original.TryReadResult((Relation)roots[3], out _, out _, out _));
        Assert.Equal(saved, GojoNumericFormat.Encode(roots));
    }

    [Fact]
    public void Restored_curve_reads_preserve_controls_tangents_and_shared_endpoint_identity()
    {
        var f = new Curve.Frame(new QualitativeMotionFrame()); var joint = new Placeholder();
        var first = f.Cubic(new Placeholder(), joint, f.Word(-2, 0), f.Word(-1, 3), f.Word(3, 3), f.Word(4, 0));
        var second = f.Line(joint, new Placeholder(), f.Word(4, 0), f.Word(5, -3));
        var original = f.Read(first);
        var roots = GojoNumericFormat.Decode(GojoNumericFormat.Encode(f.Surface, first, second, f.Word(-3, 2)));
        var restored = Curve.Frame.FromSurface((Relation)roots[0]);
        Assert.Same(roots[0], restored.Surface); Assert.Same(((Relation)roots[0]).Left, restored.Motion.Surface);
        Assert.Same(((Relation)roots[0]).Right, restored.Reach);
        var read = restored.Read((Relation)roots[1]); var next = restored.Read((Relation)roots[2]);
        Assert.True(read.Complete); Assert.True(next.Complete);
        Assert.Equal(original.Controls, read.Controls); Assert.Equal(original.StartVector, read.StartVector);
        Assert.Equal(original.EndVector, read.EndVector); Assert.Equal(original.BendSign, read.BendSign);
        Assert.Same(read.End, next.Start); Assert.True(Curve.Join(read, next).Smooth);
        Assert.True(restored.Vector((Relation)roots[3], out var vector)); Assert.Equal(new Curve.Vector(-3, 2), vector);
        Assert.False(f.Read((Relation)roots[1]).Complete);
        Assert.False(restored.Vector(f.Word(-3, 2), out _));
        Assert.Equal(-read.BendSign, restored.Read(restored.Reverse((Relation)roots[1])).BendSign);
        Assert.Equal(-read.BendSign, restored.Read(restored.Reflect((Relation)roots[1])).BendSign);
    }

    [Fact]
    public void Reboxed_shared_sites_survive_transport_but_equal_coordinates_remain_independent()
    {
        var carrier = new Carrier(); var reach = new CarrierLocation(carrier, 1);
        var joint = new CarrierLocation(carrier, 7);
        var f = new Curve.Frame(new QualitativeMotionFrame(), reach);
        var first = f.Line(new Placeholder(), joint, f.Word(0, 0), f.Word(2, 0));
        var second = f.Line(new CarrierLocation(carrier, joint.Tick), new Placeholder(), f.Word(2, 0), f.Word(4, 0));
        var unrelated = f.Line(new CarrierLocation(carrier, 7), new Placeholder(), f.Word(2, 0), f.Word(4, 0));
        var alienReach = S(QualitativeMotionFrame.Move(f.Motion.Right, new CarrierLocation(carrier, 1)));
        var roots = GojoNumericFormat.Decode(GojoNumericFormat.Encode(f.Surface, first, second, unrelated, alienReach));
        var restored = Curve.Frame.FromSurface((Relation)roots[0]);
        var a = restored.Read((Relation)roots[1]); var b = restored.Read((Relation)roots[2]);
        var c = restored.Read((Relation)roots[3]);
        Assert.True(a.Complete); Assert.True(b.Complete); Assert.True(c.Complete);
        Assert.True(Curve.Join(a, b).Connected); Assert.True(Curve.Join(a, b).Smooth);
        Assert.False(Curve.Join(a, c).Connected); Assert.Null(Curve.Join(a, c).Smooth);
        Assert.False(restored.Vector((Relation)roots[4], out _));
    }

    [Fact]
    public void Decoded_gap_claim_rereads_native_sections_and_drives_its_decoded_receiver()
    {
        var frame = new Curve.Frame(new QualitativeMotionFrame());
        var receiver = CoupledExtentMachine.Create(1, 8);
        var desired = new Carrier(receiver.StepUnit.UnitShape); desired.Add(5);
        var first = frame.Line(new Placeholder(), new Placeholder(), frame.Word(0, 2), frame.Word(6, 2));
        var second = frame.Line(new Placeholder(), new Placeholder(), frame.Word(0, -1), frame.Word(6, -1));
        var claim = GeometricGapClaim.Create(frame, new Relation(desired.ValueSlot, desired), receiver,
            frame.Motion.Down, frame.Word(0, -1));
        var original = claim.Observe(first, second);
        var roots = GojoNumericFormat.Decode(GojoNumericFormat.Encode(frame.Surface, claim.Surface, first, second, receiver.ArchiveSurface()));
        var saved = GojoNumericFormat.Encode(roots);
        var restoredFrame = Curve.Frame.FromSurface((Relation)roots[0]);
        var restoredClaim = GeometricGapClaim.FromSurface(restoredFrame, (Relation)roots[1]);
        var restoredReceiver = CoupledExtentMachine.Restore((Relation)roots[4]);
        Assert.Equal(saved, GojoNumericFormat.Encode(roots));
        var read = restoredClaim.Observe((Relation)roots[2], (Relation)roots[3]);
        Assert.True(read.Complete); Assert.Equal(original.Gap, read.Gap);
        Assert.Equal(original.DesiredGap, read.DesiredGap); Assert.Equal(original.Mask, read.Mask);
        var step = restoredReceiver.Step(read.Packet);
        Assert.True(step.Resolved); Assert.Equal(1, step.RequestedDelta); Assert.Equal(2, step.After.Value);
        Assert.Equal(step.RequestedDelta, step.AcceptedDelta + step.PendingDelta + step.UnroutedDelta);
        Assert.Equal(1, receiver.Snapshot().Value); Assert.Equal(0, receiver.Snapshot().Beat);
        Assert.Throws<ArgumentException>(() => GeometricGapClaim.FromSurface(frame, (Relation)roots[1]));
    }

    [Theory]
    [InlineData("foreign-direction")]
    [InlineData("lookalike-direction")]
    [InlineData("open-opposites")]
    [InlineData("foreign-reach")]
    [InlineData("exchange-order")]
    [InlineData("exchange-membership")]
    [InlineData("axis-alias")]
    [InlineData("inverse-role")]
    [InlineData("carrier-origin")]
    [InlineData("extra-wrapper")]
    public void Rebinding_refuses_missing_foreign_or_lookalike_native_roles(string change)
    {
        var source = new QualitativeMotionFrame(); Relation wrong;
        switch (change)
        {
            case "foreign-direction":
            case "lookalike-direction":
                var pairs = E(source.Opposites); var pair = (Relation)pairs[0];
                pairs[0] = new Relation(change == "foreign-direction" ? new QualitativeMotionFrame().Right :
                    new Relation(source.Right.Left, source.Right.Right), pair.Right);
                wrong = WithLaw(S(pairs), source.CancellationLaws, source.Exchange); break;
            case "open-opposites":
                wrong = WithLaw(StructuralConstraint.Open(E(source.Opposites)), source.CancellationLaws, source.Exchange); break;
            case "foreign-reach":
                var rows = E(source.CancellationLaws); var row = (Relation)rows[0]; var law = (Relation)row.Right;
                var moves = E((Relation)law.Left); moves[1] = new Relation(((Relation)moves[1]).Left, new Placeholder());
                rows[0] = new Relation(row.Left, new Relation(S(moves), law.Right));
                wrong = WithLaw(source.Opposites, S(rows), source.Exchange); break;
            case "exchange-order":
                var words = (Relation)source.Exchange.Right;
                wrong = WithLaw(source.Opposites, source.CancellationLaws,
                    new Relation(source.Exchange.Left, new Relation(words.Left, words.Left))); break;
            case "exchange-membership":
                var selection = (Relation)source.Exchange.Left;
                wrong = WithLaw(source.Opposites, source.CancellationLaws,
                    new Relation(new Relation(selection.Left, S()), source.Exchange.Right)); break;
            case "axis-alias":
                wrong = new Relation(new Relation(source.Origin, new Relation(source.Horizontal, source.Horizontal)), source.Surface.Right); break;
            case "inverse-role":
                wrong = new Relation(new Relation(((Placeholder)source.Origin).Dual(), ((Relation)source.Surface.Left).Right), source.Surface.Right); break;
            case "carrier-origin":
                wrong = new Relation(new Relation(new Carrier(), ((Relation)source.Surface.Left).Right), source.Surface.Right); break;
            default:
                wrong = new Relation(source.Surface, new Placeholder()); break;
        }
        Assert.False(QualitativeMotionFrame.TryFromSurface(wrong, out _));
        Assert.Throws<ArgumentException>(() => QualitativeMotionFrame.FromSurface(wrong));
        Assert.False(Curve.Frame.TryFromSurface(new Relation(wrong, new Placeholder()), out _));
        Assert.Throws<ArgumentException>(() => Curve.Frame.FromSurface(new Relation(wrong, new Placeholder())));

        Relation WithLaw(Relation opposites, Relation cancellations, Relation exchange) =>
            new(source.Surface.Left, new Relation(opposites, new Relation(cancellations, exchange)));
    }

    private static Relation S(params IElement[] entries) => StructuralConstraint.Closed(entries);
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
}
