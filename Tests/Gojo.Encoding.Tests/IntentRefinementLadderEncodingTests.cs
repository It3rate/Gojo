using Gojo.Core;
using Gojo.Encoding;
using Gojo.Structures;

namespace Gojo.Encoding.Tests;

public sealed class IntentRefinementLadderEncodingTests
{
    [Fact]
    public void ConstraintRungElaboratesBaseIntentAsBasis()
    {
        var primary = new Carrier();
        var routeIntent = Relation.Segment(primary, 0, 12);
        var obstacle = Relation.Segment(primary, 5, 7);
        var baseRung = StructuralLadderSurface.BaseRung(new Carrier(), routeIntent);
        var constrainedIntent = Relation.OrderedGroup(new Carrier(), routeIntent, obstacle);
        var correspondence = FocusSurface.BindingFrame(
            new Carrier(),
            FocusSurface.Bind(routeIntent, constrainedIntent));
        var constraintFocus = FocusSurface.Create(
            new Carrier(),
            routeIntent,
            obstacle,
            routeIntent,
            UnknownPoint(),
            constrainedIntent,
            correspondence,
            UnknownPoint(),
            baseRung);
        var constraintRung = StructuralLadderSurface.Rung(
            new Carrier(),
            routeIntent,
            constraintFocus,
            constrainedIntent,
            correspondence,
            UnknownPoint(),
            obstacle);

        Assert.True(StructuralLadderSurface.TryReadRung(
            constraintRung,
            out var source,
            out var focus,
            out var result,
            out var readCorrespondence,
            out _,
            out _,
            out _,
            out var provenance));

        Assert.Same(routeIntent, source);
        Assert.Same(constrainedIntent, result);
        Assert.Same(obstacle, provenance);
        Assert.Same(routeIntent, AssertGroupElement(constrainedIntent, 0));
        Assert.Same(obstacle, AssertGroupElement(constrainedIntent, 1));
        var binding = Assert.IsType<Relation>(AssertGroupElement(Assert.IsType<Relation>(readCorrespondence), 0));
        Assert.Same(routeIntent, binding.Left);
        Assert.Same(constrainedIntent, binding.Right);

        Assert.True(FocusSurface.TryRead(
            Assert.IsType<Relation>(focus),
            out var focusSource,
            out var focusBasis,
            out var participation,
            out var complement,
            out var replacement,
            out var focusCorrespondence,
            out _,
            out var focusProvenance));
        Assert.Same(routeIntent, focusSource);
        Assert.Same(obstacle, focusBasis);
        Assert.Same(routeIntent, participation);
        Assert.True(IsUnknownPoint(complement));
        Assert.Same(constrainedIntent, replacement);
        Assert.Same(correspondence, focusCorrespondence);
        Assert.Same(baseRung, focusProvenance);
    }

    [Fact]
    public void DetourSolutionRungRefinesConstrainedIntentWithoutReplacingBaseIntent()
    {
        var sample = CreatePositiveOutletSample();
        var routeIntent = Relation.Segment(sample.Primary, 0, 12);
        var baseRung = StructuralLadderSurface.BaseRung(new Carrier(), routeIntent);
        var constrainedIntent = Relation.OrderedGroup(new Carrier(), routeIntent, sample.Obstacle);
        var constraintCorrespondence = FocusSurface.BindingFrame(
            new Carrier(),
            FocusSurface.Bind(routeIntent, constrainedIntent));
        var constraintFocus = FocusSurface.Create(
            new Carrier(),
            routeIntent,
            sample.Obstacle,
            routeIntent,
            UnknownPoint(),
            constrainedIntent,
            constraintCorrespondence,
            UnknownPoint(),
            baseRung);
        var constraintRung = StructuralLadderSurface.Rung(
            new Carrier(),
            routeIntent,
            constraintFocus,
            constrainedIntent,
            constraintCorrespondence,
            UnknownPoint(),
            sample.Obstacle);

        Assert.True(ConceptDefinitionDiscoverySurface.TryReadLedger(sample.Ledger, out var graph));
        Assert.True(IntervalDetourSettlementSurface.TryCreateSettlement(
            graph,
            sample.Focus,
            sample.Context,
            new Carrier(),
            out var settlement));
        Assert.True(ReadWitness.TryRead(
            settlement,
            out var selected,
            out _,
            out var detourPath,
            out var directTension,
            out _));
        var solutionCorrespondence = FocusSurface.BindingFrame(
            new Carrier(),
            FocusSurface.Bind(constrainedIntent, detourPath));
        var solutionFocus = FocusSurface.Create(
            new Carrier(),
            constrainedIntent,
            settlement,
            constrainedIntent,
            UnknownPoint(),
            detourPath,
            solutionCorrespondence,
            directTension,
            sample.Context);
        var solutionRung = StructuralLadderSurface.Rung(
            new Carrier(),
            constrainedIntent,
            solutionFocus,
            detourPath,
            solutionCorrespondence,
            directTension,
            sample.Context);
        var ladder = StructuralLadderSurface.CreateWithRungs(
            new Carrier(),
            baseRung,
            constraintRung,
            solutionRung);

        Assert.Same(sample.Focus, selected);
        Assert.True(StructuralLadderSurface.TryRead(ladder, out var readBase, out var frame, out _, out _, out _));
        Assert.Same(baseRung, readBase);
        Assert.True(StructuralLadderSurface.TryReadRungFrame(frame, out var rungs));
        Assert.Equal([constraintRung, solutionRung], rungs);

        Assert.True(StructuralLadderSurface.TryReadCurrentRung(ladder, out var current));
        Assert.Same(solutionRung, current);
        Assert.True(StructuralLadderSurface.TryReadRung(
            current,
            out var source,
            out var focus,
            out var result,
            out var readCorrespondence,
            out var residue,
            out _,
            out _,
            out var provenance));

        Assert.Same(constrainedIntent, source);
        Assert.Same(solutionFocus, focus);
        Assert.Same(detourPath, result);
        Assert.Same(solutionCorrespondence, readCorrespondence);
        Assert.Same(directTension, residue);
        Assert.Same(sample.Context, provenance);
        Assert.Same(routeIntent, AssertGroupElement(constrainedIntent, 0));
        Assert.True(FocusSurface.TryRead(
            Assert.IsType<Relation>(focus),
            out var focusSource,
            out var focusBasis,
            out var participation,
            out var complement,
            out var replacement,
            out var focusCorrespondence,
            out var focusResidue,
            out var focusProvenance));
        Assert.Same(constrainedIntent, focusSource);
        Assert.Same(settlement, focusBasis);
        Assert.Same(constrainedIntent, participation);
        Assert.True(IsUnknownPoint(complement));
        Assert.Same(detourPath, replacement);
        Assert.Same(solutionCorrespondence, focusCorrespondence);
        Assert.Same(directTension, focusResidue);
        Assert.Same(sample.Context, focusProvenance);
        AssertPositiveOutletPath(detourPath, sample.Primary, sample.Branch);
    }

    private static DetourSample CreatePositiveOutletSample()
    {
        var primary = new Carrier();
        var branch = new Carrier();
        var definitionFrame = new Carrier();
        var definitions = new List<Relation>();

        Relation Add(string name, IElement body, IReadOnlyList<IElement>? dependencies = null)
        {
            var definition = ConceptDefinitionSurface.Create(
                definitionFrame,
                name,
                body,
                dependencies);
            definitions.Add(definition);
            return definition;
        }

        var focus = Add("focus", Relation.Point(primary, 0));
        var obstacle = Relation.Segment(primary, 5, 7);
        var branchCandidate = IntervalDetourSettlementSurface.PositiveOutletBranchCandidate(
            new Carrier(),
            primary,
            entryTicks: 4,
            branch,
            branchEntryTicks: 0,
            branchExitTicks: 10,
            exitTicks: 8);
        var context = Add(
            "positive outlet route context",
            Relation.OrderedGroup(new Carrier(), Relation.Point(primary, 12), obstacle, branchCandidate),
            [focus]);
        var ledger = ConceptDefinitionSurface.Ledger(new Carrier(), definitions);

        return new DetourSample(ledger, primary, branch, obstacle, context, focus);
    }

    private static IElement AssertGroupElement(Relation group, int index)
    {
        var holder = Assert.IsType<Carrier>(group.Right);
        Assert.True(group.TryReadOrderedElementsOn(holder, out var elements));
        Assert.InRange(index, 0, elements.Length - 1);
        return elements[index];
    }

    private static void AssertPositiveOutletPath(IElement element, Carrier primary, Carrier branch)
    {
        var path = Assert.IsType<Relation>(element);
        var holder = Assert.IsType<Carrier>(path.Right);
        Assert.True(path.TryReadOrderedElementsOn(holder, out var elements));
        Assert.Equal(5, elements.Length);
        AssertSegment(elements[0], primary, 0, 4);
        AssertLink(elements[1], primary, 4, branch.Inverse(), 0);
        Assert.True(IntervalDetourSettlementSurface.TryReadPositiveOutlet(
            elements[2],
            primary,
            out var sourceStart,
            out var sourceEnd,
            out var branchCarrier,
            out var branchEntry,
            out var branchExit));
        Assert.Equal(4, sourceStart);
        Assert.Equal(8, sourceEnd);
        Assert.True(branchCarrier.SameIdentityAs(branch));
        Assert.Equal(0, branchEntry);
        Assert.Equal(10, branchExit);
        AssertLink(elements[3], branch, 10, primary, 8);
        AssertSegment(elements[4], primary, 8, 12);
    }

    private static void AssertLink(
        IElement element,
        Carrier leftCarrier,
        long leftTicks,
        Carrier rightCarrier,
        long rightTicks)
    {
        var link = Assert.IsType<Relation>(element);
        AssertPoint(link.Left, leftCarrier, leftTicks);
        AssertPoint(link.Right, rightCarrier, rightTicks);
    }

    private static void AssertSegment(IElement element, Carrier carrier, long start, long end)
    {
        var segment = Assert.IsType<Relation>(element);
        AssertPoint(segment.Left, carrier, start);
        AssertPoint(segment.Right, carrier, end);
    }

    private static void AssertPoint(IElement element, Carrier carrier, long ticks)
    {
        var point = Assert.IsType<Relation>(element);
        var location = Assert.IsType<CarrierLocation>(point.Left);
        var pointCarrier = Assert.IsType<Carrier>(point.Right);
        Assert.True(location.Carrier.SameIdentityAs(carrier));
        Assert.True(pointCarrier.SameIdentityAs(carrier));
        Assert.Equal(ticks, location.Ticks);
    }

    private static Relation UnknownPoint() => Relation.Point(Carrier.Unknown, 0);

    private static bool IsUnknownPoint(IElement element) =>
        element is Relation relation &&
        relation.Left is CarrierLocation location &&
        relation.Right is Carrier carrier &&
        carrier.IsUnknown &&
        location.Carrier.IsUnknown &&
        location.Ticks == 0;

    private readonly record struct DetourSample(
        Relation Ledger,
        Carrier Primary,
        Carrier Branch,
        Relation Obstacle,
        Relation Context,
        Relation Focus);
}
