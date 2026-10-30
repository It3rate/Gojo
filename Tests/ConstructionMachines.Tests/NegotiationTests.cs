using System.Numerics;
using Gojo.Core;
using Gojo.Encoding;
using Gojo.Experiments.ConstructionMachines;
using Gojo.Structures;
using static Gojo.Experiments.ConstructionMachines.MachineExpressions;

namespace Gojo.ConstructionMachines.Tests;

public sealed class NegotiationTests
{
    [Fact]
    public void CapturedPairLetsBothOpaqueMachinesMeetWithoutSequentialChasing()
    {
        var a = Rectangle(8); var b = Rectangle(16);
        var ap = a.Machine.Port(1, 0); var bp = b.Machine.Port(1, 0);
        a.Machine.AddResponse(ap, a.Width, min: 1);
        b.Machine.AddResponse(bp, b.Width, min: 1);
        var world = new NegotiationWorld([a.Machine, b.Machine], [new NegotiationLink(ap, bp)]);

        world.Step();

        Assert.Equal(12UL, a.Width.Value);
        Assert.Equal(12UL, b.Width.Value);
        Point(a.Machine.Point(1), 12, 0);
        Point(b.Machine.Point(1), 12, 0);
        Point(a.Machine.Point(2), 12, 4);
        Point(b.Machine.Point(2), 12, 4);
        Assert.Equal(0L, a.Width.Tension);
        Assert.Equal(0L, b.Width.Tension);
    }

    [Fact]
    public void SameRequestMovesTheOppositeEdgeOnlyInTheCentreConstruction()
    {
        var c = new ConstructionRecipes();
        var width = Program.Parameter(8); var halfWidth = Program.Parameter(4);
        var path = new NegotiatingMachine(c.Path(c.Zero, c.Point(1, 0), [width, Program.Parameter(4)], 2));
        var centre = new NegotiatingMachine(c.OppositePairs(c.Point(4, 2), halfWidth, Program.Parameter(2)));
        var target = Rectangle(16);
        var pp = path.Port(1, 0); var cp = centre.Port(1, 0); var tp = target.Machine.Port(1, 0);
        path.AddResponse(pp, width, min: 1);
        centre.AddResponse(cp, halfWidth, min: 1);
        var pworld = new NegotiationWorld([path, target.Machine], [new NegotiationLink(pp, tp)]);
        pworld.Step();
        var cworld = new NegotiationWorld([centre, target.Machine], [new NegotiationLink(cp, tp)]);
        cworld.Step();

        Point(path.Point(0), 0, 0);
        Point(path.Point(1), 12, 0);
        Point(path.Point(2), 12, 4);
        Point(centre.Point(0), -4, 0);
        Point(centre.Point(1), 12, 0);
        Point(centre.Point(2), 12, 4);
        Point(centre.Point(3), -4, 4);
    }

    [Fact]
    public void ReadingAndPlanningDoesNotInjectChangeAndPublicationsHidePrivateParameters()
    {
        var a = Rectangle(8); var b = Rectangle(16);
        var ap = a.Machine.Port(1, 0); var bp = b.Machine.Port(1, 0);
        a.Machine.AddResponse(ap, a.Width, min: 1);
        var ao = a.Machine.Publish(ap); var bo = b.Machine.Publish(bp);
        var link = new NegotiationLink(ap, bp);

        var first = link.Plan(ao, bo);
        var second = link.Plan(ao, bo);

        Scalar(NegotiationLink.Residual(first), 8);
        Scalar(NegotiationLink.Residual(second), 8);
        Assert.Equal(8UL, a.Width.Value);
        Assert.Equal(0L, a.Width.Tension);
        Assert.DoesNotContain(a.Width, ao.Carriers());
        Assert.DoesNotContain(a.Height, ao.Carriers());
        Assert.NotEmpty(F(F(ao)[3]));
    }

    [Fact]
    public void ConsumedRequestsCannotApplyTwiceAndPublishedCoordinatesRemainHistorical()
    {
        var a = Rectangle(8); var b = Rectangle(16);
        var ap = a.Machine.Port(1, 0); var bp = b.Machine.Port(1, 0);
        a.Machine.AddResponse(ap, a.Width, min: 1);
        var ao = a.Machine.Publish(ap);
        var link = new NegotiationLink(ap, bp);
        var requests = For(link.Plan(ao, b.Machine.Publish(bp)), ap);
        var site = a.Machine.Point(1);
        var oldTicks = F(site).Cast<CarrierLocation>().ToArray();

        Assert.True(a.Machine.Apply(requests, out var receipt));
        Assert.False(a.Machine.Apply(requests, out _));

        Assert.Equal(12UL, a.Width.Value);
        Scalar(F(ao)[2], 8);
        Point(site, 12, 0);
        var currentTicks = F(a.Machine.Point(1)).Cast<CarrierLocation>().ToArray();
        Assert.True(oldTicks[0].SameTickRefAs(currentTicks[0]));
        Assert.True(oldTicks[1].SameTickRefAs(currentTicks[1]));
        Assert.NotEmpty(F(receipt));
    }

    [Fact]
    public void AZeroResponseConsumesItsPublicationWithoutPretendingToResolveTheGap()
    {
        var a = Rectangle(8); var b = Rectangle(16);
        var ap = a.Machine.Port(1, 0); var bp = b.Machine.Port(1, 0);
        a.Machine.AddResponse(ap, a.Width, responseN: 0, min: 1);
        var link = new NegotiationLink(ap, bp);
        var requests = For(link.Plan(a.Machine.Publish(ap), b.Machine.Publish(bp)), ap);

        Assert.True(a.Machine.Apply(requests, out _));
        Assert.False(a.Machine.Apply(requests, out _));

        Assert.Equal(8UL, a.Width.Value);
        Scalar(NegotiationLink.Residual(link.Plan(a.Machine.Publish(ap), b.Machine.Publish(bp))), 8);
        Assert.Equal(0L, a.Width.Tension);
    }

    [Fact]
    public void StepCapacityProducesPartialMovementAndLeavesTheRemainingDisagreement()
    {
        var a = Rectangle(8); var b = Rectangle(16);
        var ap = a.Machine.Port(1, 0); var bp = b.Machine.Port(1, 0);
        a.Machine.AddResponse(ap, a.Width, responseN: 1, responseD: 1, min: 1, maxStep: 3);
        var link = new NegotiationLink(ap, bp);
        var world = new NegotiationWorld([a.Machine, b.Machine], [link]);

        world.Step();
        Assert.Equal(11UL, a.Width.Value);
        Scalar(NegotiationLink.Residual(link.Plan(a.Machine.Publish(ap), b.Machine.Publish(bp))), 5);
        world.Step();
        Assert.Equal(14UL, a.Width.Value);
        world.Step();
        Assert.Equal(16UL, a.Width.Value);
        Assert.Equal(0L, a.Width.Tension);
    }

    [Fact]
    public void AFixedActuatorRetainsAConflictWithoutAccumulatingRepeatedBlockedTension()
    {
        var a = Rectangle(8); var b = Rectangle(16);
        var ap = a.Machine.Port(1, 0); var bp = b.Machine.Port(1, 0);
        a.Machine.AddResponse(ap, a.Width, responseN: 1, responseD: 1, min: 8, max: 8);
        var link = new NegotiationLink(ap, bp);
        var world = new NegotiationWorld([a.Machine, b.Machine], [link]);

        for (var i = 0; i < 4; i++) world.Step();

        Assert.Equal(8UL, a.Width.Value);
        Assert.Equal(0L, a.Width.Tension);
        Scalar(NegotiationLink.Residual(link.Plan(a.Machine.Publish(ap), b.Machine.Publish(bp))), 8);
        Assert.NotEmpty(F(F(a.Machine.LastReceipt!)[3]));
    }

    [Fact]
    public void ASubQuantumRequestDoesNotInventMotionOrEraseResidual()
    {
        var a = Rectangle(8); var b = Rectangle(10);
        var ap = a.Machine.Port(1, 0); var bp = b.Machine.Port(1, 0);
        a.Machine.AddResponse(ap, a.Width, responseN: 1, responseD: 1, min: 1, quantum: 4);
        var link = new NegotiationLink(ap, bp);
        var world = new NegotiationWorld([a.Machine, b.Machine], [link]);

        world.Step();
        world.Step();

        Assert.Equal(8UL, a.Width.Value);
        Assert.Equal(0L, a.Width.Tension);
        Scalar(NegotiationLink.Residual(link.Plan(a.Machine.Publish(ap), b.Machine.Publish(bp))), 2);
    }

    [Fact]
    public void AnOpposedResponseCalibrationMovesBothSidesThroughTheSameHalfSpan()
    {
        var c = new ConstructionRecipes(); var halfWidth = Program.Parameter(4);
        var centre = new NegotiatingMachine(c.OppositePairs(c.Point(4, 2), halfWidth, Program.Parameter(2)));
        var target = new NegotiatingMachine(c.Offsets(c.Point(-4, 0), Program.Parameter(4), Program.Parameter(4)));
        var cp = centre.Port(0, 0); var tp = target.Port(0, 0);
        centre.AddResponse(cp, halfWidth, gainN: -1, min: 1);
        var world = new NegotiationWorld([centre, target], [new NegotiationLink(cp, tp)]);

        world.Step();

        Assert.Equal(6UL, halfWidth.Value);
        Point(centre.Point(0), -2, 0);
        Point(centre.Point(1), 10, 0);
    }

    [Fact]
    public void AForeignPortCannotBorrowAnEqualLookingLocalObservation()
    {
        var a = Rectangle(8); var b = Rectangle(8); var target = Rectangle(16);
        var ap = a.Machine.Port(1, 0); var bp = b.Machine.Port(1, 0); var tp = target.Machine.Port(1, 0);
        a.Machine.AddResponse(ap, a.Width, min: 1);
        var wrong = For(new NegotiationLink(bp, tp).Plan(b.Machine.Publish(bp), target.Machine.Publish(tp)), bp);

        Assert.False(a.Machine.Apply(wrong, out _));

        Assert.Equal(8UL, a.Width.Value);
        Assert.Equal(0L, a.Width.Tension);
    }

    [Fact]
    public void AFullResponseAtBothEndsCanOscillateWithoutBeingCalledAgreement()
    {
        var a = Rectangle(8); var b = Rectangle(16);
        var ap = a.Machine.Port(1, 0); var bp = b.Machine.Port(1, 0);
        a.Machine.AddResponse(ap, a.Width, responseN: 1, responseD: 1, min: 1);
        b.Machine.AddResponse(bp, b.Width, responseN: 1, responseD: 1, min: 1);
        var link = new NegotiationLink(ap, bp);
        var world = new NegotiationWorld([a.Machine, b.Machine], [link]);

        world.Step();
        Assert.Equal(16UL, a.Width.Value);
        Assert.Equal(8UL, b.Width.Value);
        world.Step();
        Assert.Equal(8UL, a.Width.Value);
        Assert.Equal(16UL, b.Width.Value);
        Scalar(NegotiationLink.Residual(link.Plan(a.Machine.Publish(ap), b.Machine.Publish(bp))), 8);
    }

    [Theory]
    [InlineData(2, 4, 28, 10, 24)]
    [InlineData(-1, 24, 8, 12, 12)]
    public void LinkCalibrationTranslatesRequestsBackIntoEachParticipantsBasis(
        long scale, long offset, long initialB, long expectedA, long expectedB)
    {
        var a = Rectangle(8); var b = Rectangle(initialB);
        var ap = a.Machine.Port(1, 0); var bp = b.Machine.Port(1, 0);
        a.Machine.AddResponse(ap, a.Width, min: 1);
        b.Machine.AddResponse(bp, b.Width, min: 1);
        var link = new NegotiationLink(ap, bp, offset: offset, mapN: scale);
        var world = new NegotiationWorld([a.Machine, b.Machine], [link]);

        world.Step();

        Assert.Equal((ulong)expectedA, a.Width.Value);
        Assert.Equal((ulong)expectedB, b.Width.Value);
        Scalar(NegotiationLink.Residual(link.Plan(a.Machine.Publish(ap), b.Machine.Publish(bp))), 0);
    }

    [Fact]
    public void TwoPortsSharingAnActuatorShareItsPerRoundCapacity()
    {
        var a = Rectangle(8); var b = Rectangle(16); var c = Rectangle(16);
        var lower = a.Machine.Port(1, 0); var upper = a.Machine.Port(2, 0);
        var bp = b.Machine.Port(1, 0); var cp = c.Machine.Port(2, 0);
        a.Machine.AddResponse(lower, a.Width, min: 1, maxStep: 3);
        a.Machine.AddResponse(upper, a.Width, min: 1, maxStep: 3);
        var world = new NegotiationWorld([a.Machine, b.Machine, c.Machine],
            [new NegotiationLink(lower, bp), new NegotiationLink(upper, cp)]);

        world.Step();

        Assert.Equal(11UL, a.Width.Value);
        Point(a.Machine.Point(1), 11, 0);
        Point(a.Machine.Point(2), 11, 4);
        Assert.Equal(0L, a.Width.Tension);
    }

    [Fact]
    public void RestoredWorldContinuesFromNativeMachineAndResponseStructures()
    {
        var a = Rectangle(8); var b = Rectangle(16);
        var ap = a.Machine.Port(1, 0); var bp = b.Machine.Port(1, 0);
        a.Machine.AddResponse(ap, a.Width, min: 1);
        var world = new NegotiationWorld([a.Machine, b.Machine], [new NegotiationLink(ap, bp)]);
        world.Step();
        var encoded = GojoNumericFormat.Encode(world.Surface);
        var loaded = NegotiationWorld.Restore(Assert.IsType<Relation>(Assert.Single(GojoNumericFormat.Decode(encoded))));

        loaded.Step();

        Point(loaded.Machines[0].Point(1), 14, 0);
        Point(loaded.Machines[0].Point(2), 14, 4);
        Assert.Equal(12UL, a.Width.Value);
        Point(a.Machine.Point(1), 12, 0);
        var loadedAgain = NegotiationWorld.Restore(Assert.IsType<Relation>(Assert.Single(
            GojoNumericFormat.Decode(GojoNumericFormat.Encode(loaded.Surface)))));
        loadedAgain.Step();
        Point(loadedAgain.Machines[0].Point(1), 15, 0);
    }

    [Fact]
    public void NativeRestorationKeepsConsumedRequestIdentityAndRejectsReplay()
    {
        var a = Rectangle(8); var b = Rectangle(16);
        var ap = a.Machine.Port(1, 0); var bp = b.Machine.Port(1, 0);
        a.Machine.AddResponse(ap, a.Width, min: 1);
        var requests = For(new NegotiationLink(ap, bp).Plan(a.Machine.Publish(ap), b.Machine.Publish(bp)), ap);
        Assert.True(a.Machine.Apply(requests, out _));
        var saved = F(Assert.Single(GojoNumericFormat.Decode(GojoNumericFormat.Encode(G(a.Machine.Surface, G(requests))))));
        var restored = NegotiatingMachine.Restore(Assert.IsType<Relation>(saved[0]));
        var oldRequests = F(saved[1]).Cast<Relation>().ToArray();

        Assert.False(restored.Apply(oldRequests, out _));

        Point(restored.Point(1), 12, 0);
        Point(restored.Point(2), 12, 4);
    }

    [Fact]
    public void TwoWrappersOfTheSamePublicationPairCannotDuplicateOneRequest()
    {
        var a = Rectangle(8); var b = Rectangle(16);
        var ap = a.Machine.Port(1, 0); var bp = b.Machine.Port(1, 0);
        a.Machine.AddResponse(ap, a.Width, min: 1);
        var link = new NegotiationLink(ap, bp);
        var ao = a.Machine.Publish(ap); var bo = b.Machine.Publish(bp);
        var first = Assert.Single(For(link.Plan(ao, bo), ap));
        var second = Assert.Single(For(link.Plan(ao, bo), ap));
        Assert.NotSame(first, second);

        Assert.False(a.Machine.Apply([first, second], out _));

        Assert.Equal(8UL, a.Width.Value);
        Assert.Equal(0L, a.Width.Tension);
    }

    [Fact]
    public void EqualNumbersOnAForeignBasisCannotMasqueradeAsTheRequestedDisplacement()
    {
        var a = Rectangle(8); var b = Rectangle(16);
        var ap = a.Machine.Port(1, 0); var bp = b.Machine.Port(1, 0);
        a.Machine.AddResponse(ap, a.Width, min: 1);
        var request = F(Assert.Single(For(new NegotiationLink(ap, bp)
            .Plan(a.Machine.Publish(ap), b.Machine.Publish(bp)), ap)));
        request[3] = new CarrierLocation(new Carrier(), 8);

        Assert.False(a.Machine.Apply([G(request)], out _));

        Assert.Equal(8UL, a.Width.Value);
    }

    [Fact]
    public void MalformedPeerParticipationLeavesTheRequestUnresolvedWithoutPartialMutation()
    {
        var a = Rectangle(8); var b = Rectangle(16);
        var ap = a.Machine.Port(1, 0); var bp = b.Machine.Port(1, 0);
        a.Machine.AddResponse(ap, a.Width, min: 1);
        var request = F(Assert.Single(For(new NegotiationLink(ap, bp)
            .Plan(a.Machine.Publish(ap), b.Machine.Publish(bp)), ap)));
        request[2] = new Placeholder();

        Assert.False(a.Machine.Apply([G(request)], out _));

        Assert.Equal(8UL, a.Width.Value);
        Assert.Equal(0L, a.Width.Tension);
    }

    [Fact]
    public void ChangingAPrivateParameterInvalidatesAPlanUsingItsOlderPublication()
    {
        var a = Rectangle(8); var b = Rectangle(16);
        var ap = a.Machine.Port(1, 0); var bp = b.Machine.Port(1, 0);
        a.Machine.AddResponse(ap, a.Width, min: 1);
        var requests = For(new NegotiationLink(ap, bp).Plan(a.Machine.Publish(ap), b.Machine.Publish(bp)), ap);
        a.Width.Add(1);

        Assert.False(a.Machine.Apply(requests, out _));

        Assert.Equal(9UL, a.Width.Value);
        Point(a.Machine.Point(1), 8, 0);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(-1)]
    public void ReceiptsAccountForBoundsCapacityAndQuantumSeparatelyInBothDirections(long sign)
    {
        var a = Rectangle(8);
        var c = new ConstructionRecipes();
        var target = new NegotiatingMachine(c.Offsets(c.Point(sign > 0 ? 0 : -22, 0),
            Program.Parameter(sign > 0 ? 30 : 8), Program.Parameter(4)));
        var ap = a.Machine.Port(1, 0); var tp = target.Port(1, 0);
        a.Machine.AddResponse(ap, a.Width, min: 4, max: 12, maxStep: 3, quantum: 2);
        var requests = For(new NegotiationLink(ap, tp).Plan(a.Machine.Publish(ap), target.Publish(tp)), ap);

        Assert.True(a.Machine.Apply(requests, out var receipt));

        var outcome = Assert.Single(NegotiatingMachine.Outcomes(receipt));
        Scalar(outcome.Requested, sign * 11);
        Scalar(outcome.Accepted, sign * 2);
        Scalar(outcome.BoundsBlocked, sign * 7);
        Scalar(outcome.CapacityDeferred, sign);
        Scalar(outcome.QuantumRemainder, sign);
        Scalar(outcome.CandidateBlocked, 0);
        Assert.Equal((ulong)(8 + sign * 2), a.Width.Value);
        Assert.Equal(0L, a.Width.Tension);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AnUnpublishableFractionalCandidateDoesNotMutateAnyLiveParameterOrSite(bool retainHistory)
    {
        var c = new ConstructionRecipes(); var width = Program.Parameter(8);
        var origin = c.Times(c.Point(1, 0), width.ValueSlot, width, 2);
        var a = new NegotiatingMachine(c.Offsets(origin, width, Program.Parameter(4)), retainHistory);
        var b = Rectangle(13);
        var ap = a.Port(1, 0); var bp = b.Machine.Port(1, 0);
        a.AddResponse(ap, width, responseN: 1, responseD: 1, min: 1);
        var requests = For(new NegotiationLink(ap, bp).Plan(a.Publish(ap), b.Machine.Publish(bp)), ap);

        Assert.True(a.Apply(requests, out var receipt));

        var outcome = Assert.Single(NegotiatingMachine.Outcomes(receipt));
        Scalar(outcome.Requested, 1);
        Scalar(outcome.Accepted, 0);
        Scalar(outcome.CandidateBlocked, 1);
        Assert.Equal(8UL, width.Value);
        Assert.Equal(0L, width.Tension);
        Point(a.Point(0), 4, 0);
        Point(a.Point(1), 12, 0);
    }

    [Fact]
    public void ReplacingThePrivateConstructionPreservesPortsButChangesItsCoupledResponse()
    {
        var c = new ConstructionRecipes(); var width = Program.Parameter(8); var height = Program.Parameter(4);
        var path = c.Path(c.Zero, c.Point(1, 0), [width, height], 2);
        var a = new NegotiatingMachine(path); var target = Rectangle(16);
        var ap = a.Port(1, 0); var tp = target.Machine.Port(1, 0);
        a.AddResponse(ap, width, min: 1);
        var points = a.Points.ToArray(); var ports = a.Ports.ToArray();
        var halfWidth = Program.Parameter(4);
        var replacement = RepeatFirstOutput(c.OppositePairs(c.Point(4, 2), halfWidth, Program.Parameter(2)));
        var link = new NegotiationLink(ap, tp);
        var world = new NegotiationWorld([a, target.Machine], [link]);

        Assert.True(a.TryReplaceDefinition(replacement, out _));

        Assert.Same(replacement, a.Definition);
        Assert.Empty(a.Responses);
        Assert.Equal(points, a.Points);
        Assert.Equal(ports, a.Ports);
        Point(a.Point(0), 0, 0);
        Point(a.Point(1), 8, 0);
        a.AddResponse(ap, halfWidth, min: 1);
        world.Step();
        Point(a.Point(0), -4, 0);
        Point(a.Point(1), 12, 0);
        Point(a.Point(3), -4, 4);
        Point(a.Point(4), -4, 0);
        Assert.Equal(8UL, width.Value);
        Assert.Equal(8UL, halfWidth.Value);
        Assert.Same(ap, link.Left);
    }

    [Fact]
    public void AConstructionSwapInvalidatesOldRequestsEvenWhenEveryCoordinateIsUnchanged()
    {
        var c = new ConstructionRecipes(); var width = Program.Parameter(8);
        var a = new NegotiatingMachine(c.Path(c.Zero, c.Point(1, 0), [width, Program.Parameter(4)], 2));
        var target = Rectangle(16);
        var ap = a.Port(1, 0); var tp = target.Machine.Port(1, 0);
        a.AddResponse(ap, width, min: 1);
        var oldPublication = a.Publish(ap);
        var requests = For(new NegotiationLink(ap, tp).Plan(oldPublication, target.Machine.Publish(tp)), ap);
        var halfWidth = Program.Parameter(4);
        Assert.True(a.TryReplaceDefinition(RepeatFirstOutput(
            c.OppositePairs(c.Point(4, 2), halfWidth, Program.Parameter(2))), out _));
        a.AddResponse(ap, halfWidth, min: 1);

        Assert.False(a.Apply(requests, out _));

        Assert.NotSame(oldPublication, a.Publish(ap));
        Scalar(F(oldPublication)[2], 8);
        Scalar(F(a.Publish(ap))[2], 8);
        Assert.Equal(8UL, width.Value);
        Assert.Equal(4UL, halfWidth.Value);
    }

    [Fact]
    public void ADisplacedReplacementIsRefusedWithoutChangingCurrentRoutesOrPublication()
    {
        var c = new ConstructionRecipes(); var width = Program.Parameter(8);
        var definition = c.Path(c.Zero, c.Point(1, 0), [width, Program.Parameter(4)], 2);
        var a = new NegotiatingMachine(definition);
        var ap = a.Port(1, 0); var route = a.AddResponse(ap, width, min: 1);
        var observation = a.Publish(ap); var point = a.Point(1);
        var displaced = RepeatFirstOutput(c.OppositePairs(c.Point(5, 2), Program.Parameter(4), Program.Parameter(2)));

        Assert.False(a.TryReplaceDefinition(displaced, out _));

        Assert.Same(definition, a.Definition);
        Assert.Same(route, Assert.Single(a.Responses));
        Assert.Same(observation, a.Publish(ap));
        Assert.Same(point, a.Point(1));
        Point(a.Point(0), 0, 0);
        Point(a.Point(1), 8, 0);
        Assert.Equal(8UL, width.Value);
    }

    [Fact]
    public void TheSwappedConstructionAndItsNewRoutesResumeFromNativeStorage()
    {
        var c = new ConstructionRecipes(); var width = Program.Parameter(8);
        var a = new NegotiatingMachine(c.Path(c.Zero, c.Point(1, 0), [width, Program.Parameter(4)], 2));
        var target = Rectangle(16);
        var ap = a.Port(1, 0); var tp = target.Machine.Port(1, 0);
        a.AddResponse(ap, width, min: 1);
        var world = new NegotiationWorld([a, target.Machine], [new NegotiationLink(ap, tp)]);
        var halfWidth = Program.Parameter(4);
        Assert.True(a.TryReplaceDefinition(RepeatFirstOutput(
            c.OppositePairs(c.Point(4, 2), halfWidth, Program.Parameter(2))), out _));
        a.AddResponse(ap, halfWidth, min: 1);
        var restored = NegotiationWorld.Restore(Assert.IsType<Relation>(Assert.Single(
            GojoNumericFormat.Decode(GojoNumericFormat.Encode(world.Surface)))));

        restored.Step();

        Point(restored.Machines[0].Point(0), -4, 0);
        Point(restored.Machines[0].Point(1), 12, 0);
        Point(restored.Machines[0].Point(4), -4, 0);
        Assert.Same(restored.Machines[0].Port(1, 0), restored.Links[0].Left);
        Point(a.Point(0), 0, 0);
        Assert.Equal(4UL, halfWidth.Value);
        Assert.Equal(8UL, width.Value);
    }

    private static (NegotiatingMachine Machine, Carrier Width, Carrier Height) Rectangle(long width)
    {
        var c = new ConstructionRecipes();
        var w = Program.Parameter(width); var h = Program.Parameter(4);
        return (new NegotiatingMachine(c.Offsets(c.Zero, w, h)), w, h);
    }

    private static Relation[] For(Relation plan, Relation port) => NegotiationLink.Requests(plan)
        .Where(request => ReferenceEquals(F(request)[0], port)).ToArray();

    private static Relation RepeatFirstOutput(Relation directMachine)
    {
        var definition = F(directMachine);
        var outputs = F(definition[1]); var roles = F(definition[2]);
        definition[1] = G(outputs.Concat([outputs[0]]).ToArray());
        definition[2] = G(roles.Concat([new Placeholder()]).ToArray());
        return G(definition);
    }

    private static void Point(IElement point, long x, long y)
    {
        Assert.True(MachineRunner.TryNumbers(point, out var values));
        Assert.Equal(x * values[0].D, values[0].N);
        Assert.Equal(y * values[1].D, values[1].N);
    }

    private static void Scalar(IElement value, long numerator, long denominator = 1)
    {
        Assert.True(ProjectionResidue.TryRead(value, out _, out var n, out var d));
        Assert.Equal((BigInteger)numerator * d, (BigInteger)denominator * n);
    }
}
