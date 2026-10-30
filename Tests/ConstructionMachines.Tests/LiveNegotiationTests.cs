using Gojo.Core;
using Gojo.Encoding;
using Gojo.Experiments.ConstructionMachines;
using Gojo.Structures;
using static Gojo.Experiments.ConstructionMachines.MachineExpressions;

namespace Gojo.ConstructionMachines.Tests;

public sealed class LiveNegotiationTests
{
    [Fact]
    public void LiveIterationsReuseNativeParticipantsAndMatchAuditedCapacityAccounting()
    {
        var live = Rectangle(8, false); var audit = Rectangle(8, true);
        var low = Rectangle(2, false).Machine; var high = Rectangle(19, false).Machine;
        foreach (var item in new[] { live, audit })
            item.Machine.AddResponse(item.Machine.Port(1, 0), item.Width,
                responseN: 1, responseD: 1, min: 4, max: 16, maxStep: 5, quantum: 2);
        var root = live.Machine.LiveSurface; var definition = live.Machine.Definition;
        var point = live.Machine.Point(1); var site = (CarrierLocation)F(point)[0];
        var port = live.Machine.Port(1, 0); var route = live.Machine.Responses[0];

        for (var i = 0; i < 24; i++)
        {
            var target = i % 2 == 0 ? high : low;
            Assert.True(live.Machine.Apply([Request(live.Machine, target)], out var a));
            Assert.True(audit.Machine.Apply([Request(audit.Machine, target)], out var b));
            Assert.Equal(audit.Width.Value, live.Width.Value);
            var x = Assert.Single(NegotiatingMachine.Outcomes(a));
            var y = Assert.Single(NegotiatingMachine.Outcomes(b));
            Assert.Equal(Read(y.Requested), Read(x.Requested));
            Assert.Equal(Read(y.Accepted), Read(x.Accepted));
            Assert.Equal(Read(y.BoundsBlocked), Read(x.BoundsBlocked));
            Assert.Equal(Read(y.CapacityDeferred), Read(x.CapacityDeferred));
            Assert.Equal(Read(y.QuantumRemainder), Read(x.QuantumRemainder));
            Assert.Equal(Read(y.CandidateBlocked), Read(x.CandidateBlocked));
            Assert.Same(root, live.Machine.LiveSurface);
            Assert.Same(definition, live.Machine.Definition);
            Assert.Same(point, live.Machine.Point(1));
            Assert.True(site.SameTickRefAs((CarrierLocation)F(point)[0]));
            Assert.Same(port, live.Machine.Port(1, 0));
            Assert.Same(route, live.Machine.Responses[0]);
        }
        Assert.Equal(1, live.Machine.PreviewWorldImports);
        Assert.Equal(24, audit.Machine.PreviewWorldImports);
        Assert.Single(F(F(live.Machine.Surface)[7]));
        Assert.True(TimeStepSequence.TryRead(live.Machine.Timeline, out _, out _, out var steps));
        Assert.Empty(steps);
        Assert.True(TimeStepSequence.TryRead(audit.Machine.Timeline, out _, out _, out var auditedSteps));
        Assert.Equal(24, auditedSteps.Length);
    }

    [Fact]
    public void RetainedGraphDoesNotGrowWithPassCountAndNativeLiveGraphResumes()
    {
        var live = Rectangle(8, false); var low = Rectangle(8, false).Machine;
        var high = Rectangle(16, false).Machine;
        live.Machine.AddResponse(live.Machine.Port(1, 0), live.Width, responseN: 1, responseD: 1, min: 1);
        Assert.True(live.Machine.Apply([Request(live.Machine, high)], out _));
        var firstSize = GojoNumericFormat.Encode(live.Machine.Surface).Length;
        var root = live.Machine.LiveSurface;
        for (var i = 0; i < 200; i++)
            Assert.True(live.Machine.Apply([Request(live.Machine, i % 2 == 0 ? low : high)], out _));

        Assert.Same(root, live.Machine.LiveSurface);
        Assert.InRange(GojoNumericFormat.Encode(live.Machine.Surface).Length, firstSize - 128, firstSize + 128);
        Assert.Equal(1, live.Machine.PreviewWorldImports);
        var restoredRoot = Assert.IsType<Relation>(Assert.Single(
            GojoNumericFormat.Decode(GojoNumericFormat.Encode(root))));
        var restored = NegotiatingMachine.Restore(restoredRoot, retainHistory: false);
        Assert.Same(restoredRoot, restored.LiveSurface);
        Assert.True(restored.Apply([Request(restored, low)], out var resumed));
        Assert.Equal((System.Numerics.BigInteger)(-8), Read(Assert.Single(NegotiatingMachine.Outcomes(resumed)).Accepted).N);
        Assert.Equal(8, ((CarrierLocation)F(restored.Point(1))[0]).Ticks);
        Assert.Equal(16UL, live.Width.Value);
    }

    [Fact]
    public void LiveModeStillRejectsConsumedRequestsWithoutMovingParticipants()
    {
        var live = Rectangle(8, false); var target = Rectangle(16, false).Machine;
        live.Machine.AddResponse(live.Machine.Port(1, 0), live.Width, responseN: 1, responseD: 1, min: 1);
        var original = live.Machine.Publish(live.Machine.Port(1, 0));
        var request = Request(live.Machine, target);
        Assert.True(live.Machine.Apply([request], out var accepted));
        var publication = live.Machine.CurrentPublication;
        Assert.False(live.Machine.Apply([request], out _));
        Assert.Same(publication, live.Machine.CurrentPublication);
        Assert.Equal(16UL, live.Width.Value);
        Assert.Equal(8, Read(F(original)[2]).N);
        Assert.Equal(8, Read(Assert.Single(NegotiatingMachine.Outcomes(accepted)).Accepted).N);
        Assert.Single(F(F(live.Machine.Surface)[7]));
    }

    [Fact]
    public void AReusedPreviewCannotRetargetAnEarlierCapturedDirectValueSlotRead()
    {
        var c = new ConstructionRecipes(); var value = Program.Parameter(8);
        NegotiatingMachine Scalar(Carrier x) => new(TransformSurface.Create(new Carrier(), G(x),
            G(G(x.ValueSlot, new CarrierLocation(c.Y, 0))), G(new Placeholder()), G(), G(c.Basis, c.Metric)), false);
        var live = Scalar(value); var high = Scalar(Program.Parameter(16)); var low = Scalar(Program.Parameter(4));
        var port = live.Port(0, 0);
        live.AddResponse(port, value, responseN: 1, responseD: 1);
        Relation To(NegotiatingMachine target)
        {
            var peer = target.Port(0, 0); var link = new NegotiationLink(port, peer);
            return NegotiationLink.Requests(link.Plan(live.Publish(port), target.Publish(peer)))[0];
        }
        Assert.True(live.Apply([To(high)], out var first));
        var preview = F(F(first)[4])[1];
        var import = F(F(preview)[4])[0];
        var captured = F(import)[0];
        Assert.Equal(16, Read(captured).N);

        Assert.True(live.Apply([To(low)], out _));

        Assert.Equal(4UL, value.Value);
        Assert.Equal(16, Read(captured).N);
        Assert.Equal(1, live.PreviewWorldImports);
    }

    [Fact]
    public void ChangedNonActuatorParticipationInvalidatesThePrivatePreviewCache()
    {
        var c = new ConstructionRecipes(); var width = Program.Parameter(8); var context = Program.Parameter(1);
        var fixedSite = new TickRef(0);
        var definition = F(c.Offsets(c.Zero, width, Program.Parameter(4)));
        definition[0] = G(definition[0], context, new CarrierLocation(context, fixedSite));
        var live = new NegotiatingMachine(G(definition), retainHistory: false);
        live.AddResponse(live.Port(1, 0), width, responseN: 1, responseD: 1);
        Assert.True(live.Apply([Request(live, Rectangle(16, false).Machine)], out _));
        Assert.Equal(1, live.PreviewWorldImports);

        context.Add(1);
        Assert.True(live.Apply([Request(live, Rectangle(8, false).Machine)], out _));

        Assert.Equal(8UL, width.Value);
        Assert.Equal(2, live.PreviewWorldImports);

        fixedSite.Move(1);
        Assert.True(live.Apply([Request(live, Rectangle(16, false).Machine)], out _));
        Assert.Equal(16UL, width.Value);
        Assert.Equal(3, live.PreviewWorldImports);
    }

    [Fact]
    public void AttachedParticipationUsesIsolatedPreviewInsteadOfAssumingResetIsUndo()
    {
        var live = Rectangle(8, false);
        live.Width.Attach(new Relation(live.Width, new Placeholder()));
        live.Machine.AddResponse(live.Machine.Port(1, 0), live.Width, responseN: 1, responseD: 1);
        Assert.True(live.Machine.Apply([Request(live.Machine, Rectangle(16, false).Machine)], out _));
        Assert.True(live.Machine.Apply([Request(live.Machine, Rectangle(8, false).Machine)], out _));
        Assert.Equal(8UL, live.Width.Value);
        Assert.Equal(0, live.Width.Tension);
        Assert.Equal(2, live.Machine.PreviewWorldImports);
    }

    private static (NegotiatingMachine Machine, Carrier Width) Rectangle(long width, bool history)
    {
        var c = new ConstructionRecipes(); var w = Program.Parameter(width);
        return (new NegotiatingMachine(c.Offsets(c.Zero, w, Program.Parameter(4)), retainHistory: history), w);
    }

    private static Relation Request(NegotiatingMachine local, NegotiatingMachine target)
    {
        var port = local.Port(1, 0); var peer = target.Port(1, 0);
        var link = new NegotiationLink(port, peer);
        return NegotiationLink.Requests(link.Plan(local.Publish(port), target.Publish(peer)))[0];
    }

    private static (System.Numerics.BigInteger N, System.Numerics.BigInteger D) Read(IElement sample)
    {
        Assert.True(ProjectionResidue.TryRead(sample, out _, out var n, out var d));
        var gcd = System.Numerics.BigInteger.GreatestCommonDivisor(n, d);
        return (n / gcd, d / gcd);
    }
}
