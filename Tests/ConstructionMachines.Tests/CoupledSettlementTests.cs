using Gojo.Core;
using Gojo.Encoding;
using Gojo.Experiments.ConstructionMachines;
using Gojo.Structures;
using static Gojo.Experiments.ConstructionMachines.MachineExpressions;

namespace Gojo.ConstructionMachines.Tests;

public sealed class CoupledSettlementTests
{
    [Fact]
    public void ShapePlacementStartsWithACompatibleJointThenCreatesLocalConflict()
    {
        var model = CoupledSettlement.Create();
        Assert.Equal(0, Value(CoupledSettlement.FeatureMismatch(model.LastRead)));
        Assert.True(Goal(model.LastRead) > 0);
        Assert.Equal(0UL, model.Load.Remaining.Value);
        var initialShape = Program.XY(model.Outline.Point(0));
        var initialWidth = Program.XY(model.Outline.Point(1))[0] - initialShape[0];
        var initialJoint = Program.XY(model.Joint.Mechanical.Point(3));

        var transition = model.Step();

        Assert.True(Program.XY(model.Outline.Point(0))[0] > initialShape[0]);
        Assert.True(Program.XY(model.Outline.Point(1))[0] - Program.XY(model.Outline.Point(0))[0] > initialWidth);
        Assert.Equal(initialJoint, Program.XY(model.Joint.Mechanical.Point(3))); // Captured pair, no forward read.
        Assert.True(Value(CoupledSettlement.FeatureMismatch(transition.AfterMotion)) > 0);
        Assert.False(transition.Rewritten);
        Assert.True(model.Load.Remaining.Value > 0);
    }

    [Fact]
    public void ReviewDependsOnObservedLoadAndDoesNotClearItOnReplacement()
    {
        var model = CoupledSettlement.Create(); var ports = model.Joint.Mechanical.Ports.ToArray();
        var transition = UntilChange(model);

        Assert.True(transition.Rewritten);
        Assert.Equal(ports, model.Joint.Mechanical.Ports);
        Assert.True(model.Load.Remaining.Value >= 12);
        Assert.Equal(Value(CoupledSettlement.FeatureMismatch(transition.AfterMotion)), Value(CoupledSettlement.FeatureMismatch(transition.AfterDecision)));
        Assert.Equal(0UL, model.Load.Released.Value);
        Assert.Same(F(model.Proposals)[2], model.Selected);
        var reviews = Reviews(transition.Decision);
        Assert.False(CoupledSettlement.Eligible(reviews[0]));
        Assert.False(CoupledSettlement.Eligible(reviews[1]));
        Assert.True(CoupledSettlement.Eligible(reviews[2]));
    }

    [Fact]
    public void APositionAndScaleOnlyTargetDoesNotTriggerJointReinterpretation()
    {
        var model = CoupledSettlement.Create(targetRise: 0);
        var initial = Program.XY(model.Outline.Point(0));
        for (var i = 0; i < 30; i++)
        {
            Assert.False(model.Step().Rewritten);
            Assert.Equal(0, Value(CoupledSettlement.FeatureMismatch(model.LastRead)));
            Assert.Equal(0UL, model.Load.Remaining.Value);
        }
        Assert.NotEqual(initial, Program.XY(model.Outline.Point(0)));
        Assert.IsType<Placeholder>(model.Selected);
        Assert.Equal(0UL, ((Carrier)F(model.Policy)[3]).Value);
    }

    [Fact]
    public void SmallerAdmittedShapeChangesDelayTheSameLoadTriggeredReview()
    {
        var fast = CoupledSettlement.Create(shapeStep: 4);
        var slow = CoupledSettlement.Create(shapeStep: 2);
        var fastPass = ChangePass(fast); var slowPass = ChangePass(slow);
        Assert.InRange(fastPass, 2, 30);
        Assert.True(slowPass > fastPass);
        Assert.Equal(Value(F(fast.Policy)[0]), Value(F(slow.Policy)[0]));
    }

    [Fact]
    public void APreviewUsesCapturedNeighbourCoordinatesWithoutMutatingTheLiveWorld()
    {
        var model = CoupledSettlement.Create(reviewBudget: 0);
        UntilLoad(model, 12);
        var publications = model.World.Machines.Select(m => m.CurrentPublication).ToArray();
        var coordinates = model.World.Machines.SelectMany(m => m.Points.SelectMany(Program.XY)).ToArray();
        var load = model.Load.Remaining.Value;
        var proposal = (Relation)F(model.Proposals)[2];

        var preview = model.Probe(proposal);

        Assert.True(CoupledSettlement.Eligible(preview));
        Assert.Equal(publications, model.World.Machines.Select(m => m.CurrentPublication));
        Assert.Equal(coordinates, model.World.Machines.SelectMany(m => m.Points.SelectMany(Program.XY)));
        Assert.Equal(load, model.Load.Remaining.Value);
        Assert.Equal(0UL, ((Carrier)F(model.Policy)[3]).Value);
        var outlineActuators = F(F(model.Outline.Definition)[0]).Cast<Carrier>().ToArray();
        foreach (var privateActuator in outlineActuators) Assert.DoesNotContain(privateActuator, preview.Carriers());
        foreach (var privateActuator in F(F(model.World.Machines[2].Definition)[0]).Cast<Carrier>()) Assert.DoesNotContain(privateActuator, preview.Carriers());
    }

    [Fact]
    public void ACoordinateImprovementCannotAuthorizeBreakingTheSupportingPath()
    {
        var model = CoupledSettlement.Create(reviewBudget: 0);
        UntilLoad(model, 12);
        var review = model.Probe((Relation)F(model.Proposals)[1]);
        var result = F(F(review)[2]);
        Assert.True(Value(result[2]) >= 4);
        Assert.True(Value(result[4]) < Value(result[3]));
        Assert.Equal(0, Value(result[5]));
        Assert.Equal(1, Value(result[6]));
        Assert.False(CoupledSettlement.Eligible(review));
        Assert.Equal(0, CoupledSettlement.OpenPaths(model.Joint));
    }

    [Fact]
    public void AnUnsuitableLibraryLeavesItsResidualAndStopsAtItsLocalReviewBudget()
    {
        var model = CoupledSettlement.Create(suitableProposal: false, reviewBudget: 2);
        for (var i = 0; i < 32; i++) Assert.False(model.Step().Rewritten);
        Assert.IsType<Placeholder>(model.Selected);
        Assert.Equal(2UL, ((Carrier)F(model.Policy)[3]).Value);
        Assert.True(model.Load.Remaining.Value >= 12);
        Assert.Equal(0, CoupledSettlement.OpenPaths(model.Joint));
    }

    [Fact]
    public void ActualResponseCanReleaseLoadWhileTheShapeReferenceRemainsASeparateDemand()
    {
        var adaptive = CoupledSettlement.Create(); var rigid = CoupledSettlement.Create(reviewBudget: 0);
        for (var i = 0; i < 40; i++)
        {
            adaptive.Step(); rigid.Step();
            Assert.Equal(adaptive.Load.Introduced.Value, adaptive.Load.Remaining.Value + adaptive.Load.Released.Value);
        }
        Assert.True(adaptive.Load.Released.Value > 0);
        Assert.True(adaptive.Load.Remaining.Value < rigid.Load.Remaining.Value);
        Assert.True(Goal(adaptive.LastRead) < Goal(rigid.LastRead));
        Assert.True(Goal(adaptive.LastRead) > 0); // No fabricated global success.
    }

    [Fact]
    public void RepeatedReadOnlyObservationsDoNotCreateReviewLoadOrNewEvidenceEvents()
    {
        var model = CoupledSettlement.Create(reviewBudget: 0); model.Step();
        var introduced = model.Load.Introduced.Value; var released = model.Load.Released.Value;
        var publication = model.Joint.Mechanical.CurrentPublication;
        for (var i = 0; i < 5; i++) model.Observe();
        Assert.Equal(introduced, model.Load.Introduced.Value);
        Assert.Equal(released, model.Load.Released.Value);
        Assert.Same(publication, model.Joint.Mechanical.CurrentPublication);
    }

    [Fact]
    public void LoadSamplesAccountForReleasesAndRejectBothImmediateAndOlderReplay()
    {
        var axis = new Carrier(); var load = new MismatchLoad(axis); var first = G(new Placeholder()); var second = G(new Placeholder());
        Assert.True(load.TrySample(new CarrierLocation(axis, 6), first, G(), out _));
        Assert.False(load.TrySample(new CarrierLocation(axis, 9), first, G(), out _));
        Assert.True(load.TrySample(new CarrierLocation(axis, -2), second, G(), out var receipt));
        Assert.False(load.TrySample(new CarrierLocation(axis, 10), first, G(), out _));
        Assert.Equal(6UL, load.Introduced.Value); Assert.Equal(2UL, load.Remaining.Value); Assert.Equal(4UL, load.Released.Value);
        var claim = (Relation)F(F(receipt)[2])[3];
        Assert.True(OperationClaim.TryReadResolvedViolation(claim, out var violation)); Assert.Equal(0, violation.Ticks);
        Assert.Equal(0L, load.Remaining.Tension);
    }

    [Fact]
    public void AForeignNumericBasisCannotEnterTheLoadChannelByValueEquality()
    {
        var axis = new Carrier(); var load = new MismatchLoad(axis);
        Assert.False(load.TrySample(new CarrierLocation(new Carrier(axis.UnitShape), 12), G(), G(), out _));
        Assert.Equal(0UL, load.Remaining.Value);
    }

    [Fact]
    public void OldObservationsKeepTheirFeatureReadAfterLaterShapeAndJointResponses()
    {
        var model = CoupledSettlement.Create(); var original = model.LastRead;
        for (var i = 0; i < 10; i++) model.Step();
        Assert.Equal(0, Value(CoupledSettlement.FeatureMismatch(original)));
        Assert.True(Value(CoupledSettlement.FeatureMismatch(model.LastRead)) > 0);
    }

    [Fact]
    public void NativeRestorationContinuesTheControllerAcrossItsReviewThreshold()
    {
        var original = CoupledSettlement.Create(); for (var i = 0; i < 3; i++) original.Step();
        var restored = CoupledSettlement.Restore(Assert.IsType<Relation>(Assert.Single(GojoNumericFormat.Decode(GojoNumericFormat.Encode(original.Surface)))));
        for (var i = 0; i < 6; i++)
        {
            var a = original.Step(); var b = restored.Step();
            Assert.Equal(a.Rewritten, b.Rewritten);
            Assert.Equal(original.Load.Remaining.Value, restored.Load.Remaining.Value);
            Assert.Equal(Value(CoupledSettlement.FeatureMismatch(original.LastRead)), Value(CoupledSettlement.FeatureMismatch(restored.LastRead)));
            Assert.Equal(Goal(original.LastRead), Goal(restored.LastRead));
        }
        Assert.Same(F(restored.Proposals)[2], restored.Selected);
        Assert.Same(restored.Joint.Mechanical, restored.World.Machines[1]);
    }

    [Fact]
    public void TheIndependentShapeReferenceSharesNoPrivateLiveCarriers()
    {
        var model = CoupledSettlement.Create();
        var live = model.Outline.Definition.Carriers().Where(c => !c.IsUnknown).ToHashSet();
        Assert.DoesNotContain(model.World.Machines[2].Definition.Carriers().Where(c => !c.IsUnknown), live.Contains);
    }

    private static CoupledSettlement.Transition UntilChange(CoupledSettlement model)
    {
        for (var i = 0; i < 40; i++) { var transition = model.Step(); if (transition.Rewritten) return transition; }
        throw new Xunit.Sdk.XunitException("The controlled example never found an eligible reinterpretation.");
    }
    private static int ChangePass(CoupledSettlement model)
    { for (var i = 1; i <= 40; i++) if (model.Step().Rewritten) return i; return -1; }
    private static void UntilLoad(CoupledSettlement model, ulong threshold)
    { for (var i = 0; i < 40 && model.Load.Remaining.Value < threshold; i++) model.Step(); Assert.True(model.Load.Remaining.Value >= threshold); }
    private static Relation[] Reviews(Relation decision) => F(F(F(decision)[2])[1]).Cast<Relation>().ToArray();
    private static double Value(IElement value) => CoupledSettlement.Number(value);
    private static double Goal(Relation read) => Value(CoupledSettlement.NormValue(F(F(read)[2])[0]));
}
