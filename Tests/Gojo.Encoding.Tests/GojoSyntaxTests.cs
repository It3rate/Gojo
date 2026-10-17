using Gojo.ConceptSurface;
using Gojo.Core;
using Gojo.Encoding.Syntax;
using Gojo.Structures;

namespace Gojo.Encoding.Tests;

public sealed class GojoSyntaxTests
{
    [Fact]
    public void AliasReusePreservesCarrierAndExplicitTickIdentity()
    {
        var document = GojoSyntax.Parse("""
            gojo 1;
            let $first = carrier();
            let $second = carrier();
            let $site = tick(7);
            root at($first, $site);
            root at($second, $site);
            root point($first, 3);
            root point($first, 3);
            """);

        Assert.Equal(1, document.SyntaxVersion);
        var firstSite = Assert.IsType<CarrierLocation>(document.Roots[0]);
        var secondSite = Assert.IsType<CarrierLocation>(document.Roots[1]);
        Assert.True(firstSite.SameTickRefAs(secondSite));
        Assert.False(firstSite.Carrier.SameIdentityAs(secondSite.Carrier));

        var firstPoint = ReadPoint(document.Roots[2]);
        var secondPoint = ReadPoint(document.Roots[3]);
        Assert.True(firstPoint.Carrier.SameIdentityAs(secondPoint.Carrier));
        Assert.Equal(firstPoint.Ticks, secondPoint.Ticks);
        Assert.False(firstPoint.SameTickRefAs(secondPoint));
    }

    [Fact]
    public void LaneBasisAndPermittedLaneActionRemainSeparateStructures()
    {
        var document = ParseFixture("first-pass.gojo");
        var basis = Assert.IsType<Relation>(document.Roots[0]);
        var positiveLanes = Assert.IsType<Relation>(document.Roots[1]);
        var algebra = Assert.IsType<Relation>(document.Roots[2]);

        Assert.True(BoundaryFlowLaneBasis.TryReadLegalLanes(
            basis,
            out var exposure,
            out var legalLanes));
        Assert.Equal("P:F", exposure.Label);
        Assert.Equal([BoundaryFlowLane.PositiveForward], legalLanes);
        Assert.Empty(BoundaryFlowLaneTransitionSurface.ReadAll(basis));

        Assert.True(BoundaryFlowLaneBasis.TryReadLegalLanes(
            positiveLanes,
            out var positiveExposure,
            out var positiveLegalLanes));
        Assert.Equal("P:FI", positiveExposure.Label);
        Assert.Equal(
            [BoundaryFlowLane.PositiveForward, BoundaryFlowLane.PositiveInverse],
            positiveLegalLanes);
        Assert.Empty(BoundaryFlowLaneTransitionSurface.ReadAll(positiveLanes));

        Assert.True(BoundaryFlowLaneTransitionSurface.TryFindTransition(
            algebra,
            BoundaryFlowLane.PositiveForward,
            BoundaryFlowLane.PositiveInverse,
            out var permitted));
        var recipe = Assert.IsType<Relation>(permitted.Transition);
        Assert.True(BoundaryFlowLaneSurface.TryRead(
            Assert.IsType<Relation>(recipe.Left),
            out var source,
            out _,
            out _));
        Assert.True(BoundaryFlowLaneSurface.TryRead(
            Assert.IsType<Relation>(recipe.Right),
            out var target,
            out _,
            out _));
        Assert.Equal(BoundaryFlowLane.PositiveForward, source);
        Assert.Equal(BoundaryFlowLane.PositiveInverse, target);
    }

    [Fact]
    public void ActionOccurrenceKeepsScopeResultResidueAndProvenanceVisible()
    {
        var document = ParseFixture("first-pass.gojo");
        var occurrence = Assert.IsType<Relation>(document.Roots[3]);

        Assert.True(ReadWitness.TryRead(
            occurrence,
            out var selected,
            out var operation,
            out var result,
            out var residue,
            out var provenance));
        Assert.True(BoundaryFlowLaneSurface.TryRead(
            Assert.IsType<Relation>(selected),
            out var selectedLane,
            out _,
            out _));
        Assert.True(BoundaryFlowLaneTransitionSurface.TryRead(
            Assert.IsType<Relation>(operation),
            out var action));
        Assert.True(BoundaryFlowLaneSurface.TryRead(
            Assert.IsType<Relation>(result),
            out var resultLane,
            out _,
            out _));

        Assert.Equal(BoundaryFlowLane.PositiveForward, selectedLane);
        Assert.Equal(BoundaryFlowLane.PositiveForward, action.Source);
        Assert.Equal(BoundaryFlowLane.PositiveInverse, action.Target);
        Assert.Equal(BoundaryFlowLane.PositiveInverse, resultLane);
        Assert.IsType<Placeholder>(residue);
        Assert.IsType<Placeholder>(provenance);
    }

    [Fact]
    public void FullSurfaceActionCanTargetAnOrthogonalOutletWithoutTypeDispatch()
    {
        var document = GojoSyntax.Parse("""
            gojo 1;
            let $axis = carrier();
            let $four_lanes = basis($axis, PN, FI);
            let $other_axis = orthogonal($four_lanes);
            let $recipe = $four_lanes -> $other_axis;
            let $dimension_inverse = action(
                ports(?source_axis),
                $recipe,
                ports(?target_axis),
                ?residue_policy,
                ?operation_provenance
            );
            root act(
                $four_lanes,
                $dimension_inverse,
                $other_axis,
                ?application_residue,
                ?application_provenance
            );
            """);

        var occurrence = Assert.IsType<Relation>(Assert.Single(document.Roots));
        Assert.True(ReadWitness.TryRead(
            occurrence,
            out var selected,
            out var operation,
            out var result,
            out _,
            out _));
        Assert.True(TransformSurface.TryRead(
            Assert.IsType<Relation>(operation),
            out _,
            out var operationRecipe,
            out _,
            out _,
            out _));

        var recipe = Assert.IsType<Relation>(operationRecipe);
        Assert.Same(selected, recipe.Left);
        Assert.Same(result, recipe.Right);
        var outlet = Assert.IsType<Relation>(result);
        Assert.Same(selected, outlet.Left);
    }

    [Fact]
    public void LadderKeepsRawFineTicksAndRelativeDigitsAsDifferentReads()
    {
        var document = ParseFixture("first-pass.gojo");
        var raw = ReadPoint(document.Roots[4]);
        var relative = Assert.IsType<Relation>(document.Roots[5]);
        var carry = Assert.IsType<Relation>(document.Roots[6]);

        Assert.Equal(32, raw.Ticks);
        Assert.True(relative.TryReadOrderedElementsOn(
            Assert.IsType<Carrier>(relative.Right),
            out var relativeParts));
        Assert.Equal(3, relativeParts.Length);
        var wholeDigit = ReadPoint(relativeParts[0]);
        var fineDigit = ReadPoint(relativeParts[1]);
        Assert.Equal(1, wholeDigit.Ticks);
        Assert.Equal(12, fineDigit.Ticks);
        Assert.Same(carry, relativeParts[2]);

        var lowerCarryPoint = ReadPoint(carry.Left);
        var upperCarryPoint = ReadPoint(carry.Right);
        Assert.Equal(20, lowerCarryPoint.Ticks);
        Assert.Equal(1, upperCarryPoint.Ticks);
        Assert.True(raw.Carrier.SameIdentityAs(fineDigit.Carrier));
        Assert.True(lowerCarryPoint.Carrier.SameIdentityAs(fineDigit.Carrier));
        Assert.True(upperCarryPoint.Carrier.SameIdentityAs(wholeDigit.Carrier));
        Assert.True(fineDigit.Carrier.UnitShape.CompatibleWith(wholeDigit.Carrier.UnitShape));
    }

    [Fact]
    public void RepetitionKeepsControllerTransitionFeedbackResidueAndTimeline()
    {
        var document = GojoSyntax.Parse("""
            gojo 1;
            let $counter = carrier();
            let $step = ?input -> ?output;
            root repeat(
                $step,
                count($counter, 4),
                $step,
                ?output -> ?input,
                ?repeat_residue,
                ?repeat_timeline
            );
            """);

        var repeat = Assert.IsType<Relation>(Assert.Single(document.Roots));
        Assert.True(RepeatSurface.TryRead(
            repeat,
            out var selected,
            out var controller,
            out var transition,
            out var feedback,
            out var residue,
            out var timeline));
        Assert.Same(selected, transition);
        Assert.True(RepeatSurface.TryReadSpan(controller, out var start, out var end));
        Assert.Equal(0, start.Ticks);
        Assert.Equal(4, end.Ticks);
        var feedbackRelation = Assert.IsType<Relation>(feedback);
        Assert.Same(Assert.IsType<Relation>(selected).Right, feedbackRelation.Left);
        Assert.Same(Assert.IsType<Relation>(selected).Left, feedbackRelation.Right);
        Assert.IsType<Placeholder>(residue);
        Assert.IsType<Placeholder>(timeline);
    }

    [Fact]
    public void ProductUnitPreservesRepeatedParticipation()
    {
        var document = GojoSyntax.Parse("""
            gojo 1;
            let $distance = carrier();
            let $distance_unit = unit_of($distance);
            let $area_unit = product_units($distance_unit, $distance_unit);
            root $distance;
            root carrier($area_unit);
            """);

        var distance = Assert.IsType<Carrier>(document.Roots[0]);
        var area = Assert.IsType<Carrier>(document.Roots[1]);
        Assert.Equal(2, area.UnitShape.ProductParticipationOf(distance.UnitShape));
    }

    [Theory]
    [InlineData("gojo 1; root point(carrier(), 1.6);")]
    [InlineData("gojo 2; root carrier();")]
    [InlineData("gojo 1; root point(3, 4);")]
    [InlineData("gojo 1; root $missing;")]
    public void InvalidOrOverSpecifiedInputReportsLocatedSyntaxError(string source)
    {
        var error = Assert.Throws<GojoSyntaxException>(() => GojoSyntax.Parse(source));
        Assert.True(error.Line >= 1);
        Assert.True(error.Column >= 1);
    }

    private static GojoSyntaxDocument ParseFixture(string fileName) =>
        GojoSyntax.Parse(File.ReadAllText(Path.Combine(
            AppContext.BaseDirectory,
            "Fixtures",
            "Syntax",
            fileName)));

    private static CarrierLocation ReadPoint(IElement element)
    {
        var point = Assert.IsType<Relation>(element);
        var location = Assert.IsType<CarrierLocation>(point.Left);
        Assert.True(location.Carrier.SameIdentityAs(Assert.IsType<Carrier>(point.Right)));
        return location;
    }
}
