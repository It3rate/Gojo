using Gojo.ConceptSurface;
using Gojo.Core;
using Gojo.Encoding.Syntax;
using Gojo.Structures;

namespace Gojo.Encoding.Tests;

public sealed class GojoSyntaxActionSemanticsTests
{
    [Fact]
    public void ToolAccessWithholdsActionsWithoutDenyingAmbientLanePossibility()
    {
        var document = ParseFixture("ambient-access.gojo");
        var ambient = Assert.IsType<Relation>(document.Roots[0]);
        var restricted = Assert.IsType<Relation>(document.Roots[1]);

        Assert.True(BoundaryFlowLaneBasis.TryReadLegalLanes(
            ambient,
            out var ambientExposure,
            out var ambientLanes));
        Assert.Equal("PN:FI", ambientExposure.Label);
        Assert.Equal(
            [
                BoundaryFlowLane.PositiveForward,
                BoundaryFlowLane.PositiveInverse,
                BoundaryFlowLane.NegativeForward,
                BoundaryFlowLane.NegativeInverse
            ],
            ambientLanes);

        Assert.True(AccessCapabilitySurface.TryRead(
            restricted,
            out var restrictedAmbient,
            out var exposedPorts,
            out var capabilityPorts,
            out var blockedPorts,
            out var accessResidue,
            out _));
        Assert.Same(ambient, restrictedAmbient);

        var exposed = Assert.Single(ReadPorts(exposedPorts));
        AssertLane(exposed, BoundaryFlowLane.PositiveForward);
        Assert.Empty(ReadPorts(capabilityPorts));

        var blockedAttempt = Assert.IsType<Relation>(Assert.Single(ReadPorts(blockedPorts)));
        Assert.True(ReadWitness.TryRead(
            blockedAttempt,
            out var selected,
            out var attemptedOperation,
            out var result,
            out var blockedResidue,
            out _));
        AssertLane(selected, BoundaryFlowLane.PositiveForward);
        Assert.IsType<Placeholder>(result);
        AssertLane(blockedResidue, BoundaryFlowLane.PositiveInverse);
        AssertLane(accessResidue, BoundaryFlowLane.PositiveInverse);

        var attemptedRoute = Assert.IsType<Relation>(attemptedOperation);
        AssertLane(attemptedRoute.Left, BoundaryFlowLane.PositiveForward);
        AssertLane(attemptedRoute.Right, BoundaryFlowLane.PositiveInverse);
        Assert.True(BoundaryFlowLaneBasis.Allows(
            ambient,
            BoundaryFlowLane.PositiveInverse));
    }

    [Fact]
    public void WiderToolExposesCapabilityToAnAlreadyPossibleLane()
    {
        var document = ParseFixture("ambient-access.gojo");
        var ambient = Assert.IsType<Relation>(document.Roots[0]);
        var enabled = Assert.IsType<Relation>(document.Roots[2]);
        var application = Assert.IsType<Relation>(document.Roots[3]);

        Assert.True(AccessCapabilitySurface.TryRead(
            enabled,
            out var enabledAmbient,
            out var exposedPorts,
            out var capabilityPorts,
            out var blockedPorts,
            out _,
            out _));
        Assert.Same(ambient, enabledAmbient);
        AssertLane(Assert.Single(ReadPorts(exposedPorts)), BoundaryFlowLane.PositiveForward);
        Assert.Empty(ReadPorts(blockedPorts));

        var capability = Assert.IsType<Relation>(Assert.Single(ReadPorts(capabilityPorts)));
        Assert.True(BoundaryFlowLaneTransitionSurface.TryRead(capability, out var transition));
        Assert.Equal(BoundaryFlowLane.PositiveForward, transition.Source);
        Assert.Equal(BoundaryFlowLane.PositiveInverse, transition.Target);
        Assert.True(BoundaryFlowLaneBasis.Allows(ambient, transition.Target));

        Assert.True(ReadWitness.TryRead(
            application,
            out var selected,
            out var operation,
            out var result,
            out _,
            out _));
        AssertLane(selected, BoundaryFlowLane.PositiveForward);
        Assert.Same(capability, operation);
        AssertLane(result, BoundaryFlowLane.PositiveInverse);
    }

    [Fact]
    public void AccessReaderRejectsAnUnrelatedFivePartGroup()
    {
        var unrelated = Relation.OrderedGroup(
            new Carrier(),
            Relation.Point(new Carrier(), 1),
            Relation.Point(new Carrier(), 2),
            Relation.Point(new Carrier(), 3),
            Relation.Point(new Carrier(), 4),
            Relation.Point(new Carrier(), 5));

        Assert.False(AccessCapabilitySurface.TryRead(
            unrelated,
            out _,
            out _,
            out _,
            out _,
            out _,
            out _));
    }

    [Fact]
    public void ProductObservationAndSameIdentityHistoryDoNotMutateCarrierState()
    {
        var document = ParseFixture("preservation.gojo");
        var observation = Assert.IsType<Relation>(document.Roots[0]);
        var history = Assert.IsType<Relation>(document.Roots[1]);

        Assert.True(ReadWitness.TryRead(
            observation,
            out var selected,
            out var scaleAction,
            out var result,
            out _,
            out _));
        var selectedParts = ReadGroup(selected);
        var five = ReadPoint(selectedParts[0]);
        var two = ReadPoint(selectedParts[1]);
        var ten = ReadPoint(result);
        Assert.Equal(5, five.Ticks);
        Assert.Equal(2, two.Ticks);
        Assert.Equal(10, ten.Ticks);
        Assert.True(five.Carrier.SameIdentityAs(ten.Carrier));
        Assert.Equal(0UL, five.Carrier.Value);
        Assert.Equal(0UL, two.Carrier.Value);
        Assert.True(TransformSurface.TryRead(
            Assert.IsType<Relation>(scaleAction),
            out _,
            out _,
            out _,
            out _,
            out _));

        Assert.True(TimeStepSequence.TryRead(
            history,
            out var origin,
            out var initial,
            out var steps));
        Assert.Equal(0, origin.Ticks);
        Assert.Same(selectedParts[0], initial);
        var step = Assert.Single(steps);
        Assert.True(TimeStepSequence.TryReadStep(
            step,
            out _,
            out var start,
            out var end,
            out var before,
            out var transition,
            out var after,
            out _));
        Assert.Equal(0, start.Ticks);
        Assert.Equal(1, end.Ticks);
        Assert.Same(initial, before);
        Assert.Same(result, after);
        Assert.True(ReadWitness.TryRead(Assert.IsType<Relation>(transition), out _));
        Assert.Equal(5, ReadPoint(before).Ticks);
        Assert.Equal(10, ReadPoint(after).Ticks);

        five.Carrier.Set(42);
        Assert.Equal(42UL, five.Carrier.Value);
        Assert.Equal(5, ReadPoint(before).Ticks);
        Assert.Equal(10, ReadPoint(after).Ticks);
    }

    [Fact]
    public void ExplicitForkRetainsSourceAndIntroducesCompatibleNewIdentity()
    {
        var document = ParseFixture("preservation.gojo");
        var fork = Assert.IsType<Relation>(document.Roots[2]);

        Assert.True(ReadWitness.TryRead(
            fork,
            out var selected,
            out var operation,
            out var result,
            out _,
            out _));
        var source = ReadPoint(selected);
        var forkParts = ReadGroup(result);
        Assert.Equal(2, forkParts.Length);
        Assert.Same(selected, forkParts[0]);
        var retained = ReadPoint(forkParts[0]);
        var introduced = ReadPoint(forkParts[1]);
        Assert.True(source.Carrier.SameIdentityAs(retained.Carrier));
        Assert.False(source.Carrier.SameIdentityAs(introduced.Carrier));
        Assert.True(source.Carrier.UnitShape.CompatibleWith(introduced.Carrier.UnitShape));
        Assert.Equal(5, introduced.Ticks);

        Assert.True(TransformSurface.TryRead(
            Assert.IsType<Relation>(operation),
            out _,
            out var forkRecipe,
            out _,
            out _,
            out _));
        var recipe = Assert.IsType<Relation>(forkRecipe);
        Assert.Same(selected, recipe.Left);
        Assert.Same(result, recipe.Right);
    }

    [Fact]
    public void RollbackUsesHistoryRatherThanClaimingAnInverseForErasure()
    {
        var document = ParseFixture("preservation.gojo");
        var eraseHistory = Assert.IsType<Relation>(document.Roots[3]);
        var rollback = Assert.IsType<Relation>(document.Roots[4]);

        Assert.True(TimeStepSequence.TryRead(
            eraseHistory,
            out _,
            out var initial,
            out var steps));
        var eraseStep = Assert.Single(steps);
        Assert.True(TimeStepSequence.TryReadStep(
            eraseStep,
            out _,
            out _,
            out _,
            out var beforeErase,
            out var eraseOccurrence,
            out var afterErase,
            out _));
        Assert.Same(initial, beforeErase);
        Assert.Equal(5, ReadPoint(beforeErase).Ticks);
        Assert.Equal(0, ReadPoint(afterErase).Ticks);

        Assert.True(ReadWitness.TryRead(
            Assert.IsType<Relation>(eraseOccurrence),
            out _,
            out var eraseAction,
            out _,
            out _,
            out _));
        Assert.True(TransformSurface.TryRead(
            Assert.IsType<Relation>(eraseAction),
            out _,
            out var eraseRecipeElement,
            out _,
            out _,
            out _));
        var eraseRecipe = Assert.IsType<Relation>(eraseRecipeElement);
        Assert.Same(beforeErase, eraseRecipe.Left);
        Assert.Same(afterErase, eraseRecipe.Right);

        Assert.True(ReadWitness.TryRead(
            rollback,
            out var rollbackSelected,
            out var rollbackAction,
            out var restored,
            out _,
            out var rollbackProvenance));
        Assert.Same(afterErase, rollbackSelected);
        Assert.Same(beforeErase, restored);
        Assert.Same(eraseHistory, rollbackProvenance);
        Assert.True(TransformSurface.TryRead(
            Assert.IsType<Relation>(rollbackAction),
            out _,
            out var rollbackRecipeElement,
            out _,
            out _,
            out var actionProvenance));
        var rollbackRecipe = Assert.IsType<Relation>(rollbackRecipeElement);
        Assert.Same(eraseHistory, rollbackRecipe.Left);
        Assert.Same(restored, rollbackRecipe.Right);
        Assert.Same(eraseHistory, actionProvenance);
        Assert.NotEqual(eraseRecipe.Reverse(), rollbackRecipe);
    }

    private static GojoSyntaxDocument ParseFixture(string fileName) =>
        GojoSyntax.Parse(File.ReadAllText(Path.Combine(
            AppContext.BaseDirectory,
            "Fixtures",
            "Syntax",
            fileName)));

    private static IElement[] ReadPorts(IElement ports)
    {
        Assert.True(TransformSurface.TryReadPorts(ports, out var elements));
        return elements;
    }

    private static IElement[] ReadGroup(IElement element)
    {
        var group = Assert.IsType<Relation>(element);
        Assert.True(group.TryReadOrderedElementsOn(
            Assert.IsType<Carrier>(group.Right),
            out var elements));
        return elements;
    }

    private static CarrierLocation ReadPoint(IElement element)
    {
        var point = Assert.IsType<Relation>(element);
        var location = Assert.IsType<CarrierLocation>(point.Left);
        Assert.True(location.Carrier.SameIdentityAs(Assert.IsType<Carrier>(point.Right)));
        return location;
    }

    private static void AssertLane(IElement element, BoundaryFlowLane expected)
    {
        Assert.True(BoundaryFlowLaneSurface.TryRead(
            Assert.IsType<Relation>(element),
            out var actual,
            out _,
            out _));
        Assert.Equal(expected, actual);
    }
}
