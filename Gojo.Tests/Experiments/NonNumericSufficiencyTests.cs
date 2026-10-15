using Gojo.Core;
using Gojo.Patterns;

namespace Gojo.Tests.Experiments;

/// <summary>
/// Sufficiency probes using only relations and identity-bearing open sites.
/// The three alternatives differ in topology, without scalar state codes.
/// Their placeholder leaves remain open: these are not grounded symbolic atoms.
/// Passing tests characterize representation and the limits of substitution;
/// they do not install a domain validator or a state-transition dispatcher.
/// </summary>
public sealed class NonNumericSufficiencyTests
{
    [Fact]
    public void ThreeRelationalStatesAndDirectedEdgesRemainInspectableWithAnOpenNextState()
    {
        // States: a -> a, a -> b, b -> a. Edges: first -> second, second -> third.
        var a = new Placeholder();
        var b = new Placeholder();
        var first = new Relation(a, a);
        var second = new Relation(a, b);
        var third = new Relation(b, a);
        var domain = new Relation(new Relation(first, second), third);
        var firstToSecond = new Relation(first, second);
        var secondToThird = new Relation(second, third);
        var allowed = new Relation(firstToSecond, secondToThird);
        var evidence = new Relation(domain, allowed);
        var current = new Placeholder();
        var next = new Placeholder();
        var request = new Relation(current, next);
        var experiment = new Relation(evidence, request);

        var refined = Assert.IsType<Relation>(StructuralPattern.Refine(
            experiment, StructuralPattern.Bind(current, first)));
        var observedRequest = Assert.IsType<Relation>(refined.Right);

        Assert.NotEqual(first, second);
        Assert.NotEqual(second, third);
        Assert.NotEqual(first, third);
        Assert.Equal(third, second.Reverse());
        Assert.Same(first, firstToSecond.Left);
        Assert.Same(second, firstToSecond.Right);
        Assert.Same(second, secondToThird.Left);
        Assert.Same(third, secondToThird.Right);
        Assert.NotEqual(firstToSecond, firstToSecond.Reverse());
        Assert.Same(evidence, refined.Left);
        Assert.Same(first, observedRequest.Left);
        Assert.Same(next, observedRequest.Right);
        Assert.Same(current, request.Left);
        Assert.Equal([a, b, next], StructuralPattern.ReadSlots(refined));
        Assert.Empty(refined.Carriers());
        Assert.Empty(refined.CarrierLocations());
    }

    [Fact]
    public void CompetingCandidatesSurviveAsStructureButOneBindingFrameSelectsItsFirstBinding()
    {
        var a = new Placeholder();
        var b = new Placeholder();
        var first = new Relation(a, a);
        var second = new Relation(a, b);
        var third = new Relation(b, a);
        var evidence = new Relation(new Relation(first, second), new Relation(first, third));
        var next = new Placeholder();
        var request = new Relation(first, next);
        var support = new Relation(evidence, request);
        var toSecond = StructuralPattern.Bind(next, second);
        var toThird = StructuralPattern.Bind(next, third);
        var candidates = new Relation(
            new Relation(support, toSecond),
            new Relation(support, toThird));

        var secondRead = Assert.IsType<Relation>(StructuralPattern.Refine(request, toSecond));
        var thirdRead = Assert.IsType<Relation>(StructuralPattern.Refine(request, toThird));

        Assert.Same(second, secondRead.Right);
        Assert.Same(third, thirdRead.Right);
        Assert.Same(support, Assert.IsType<Relation>(candidates.Left).Left);
        Assert.Same(support, Assert.IsType<Relation>(candidates.Right).Left);
        Assert.Same(toSecond, Assert.IsType<Relation>(candidates.Left).Right);
        Assert.Same(toThird, Assert.IsType<Relation>(candidates.Right).Right);
        Assert.Same(next, request.Right);

        // First-binding-wins is a helper convention, not evidence for a winner.
        Assert.Same(second, Assert.Single(StructuralPattern.ReadSlotBindings(toSecond, toThird)).Right);
        Assert.Same(third, Assert.Single(StructuralPattern.ReadSlotBindings(toThird, toSecond)).Right);
    }

    [Fact]
    public void BindingCanInsertAStateOutsideTheConnectedThreeStateDomain()
    {
        var a = new Placeholder();
        var b = new Placeholder();
        var first = new Relation(a, a);
        var second = new Relation(a, b);
        var third = new Relation(b, a);
        var outside = new Relation(b, b);
        var domain = new Relation(new Relation(first, second), third);
        var allowed = new Relation(new Relation(first, second), new Relation(second, third));
        var evidence = new Relation(domain, allowed);
        var next = new Placeholder();
        var request = new Relation(first, next);
        var experiment = new Relation(evidence, request);
        var binding = StructuralPattern.Bind(next, outside);

        var applied = Assert.IsType<Relation>(StructuralPattern.Refine(experiment, binding));

        Assert.NotEqual(first, outside);
        Assert.NotEqual(second, outside);
        Assert.NotEqual(third, outside);
        Assert.Same(evidence, applied.Left);
        Assert.Same(outside, Assert.IsType<Relation>(applied.Right).Right);
        Assert.Same(next, request.Right);

        // Slot completion supplies no domain-membership or directed-edge law.
        // Supplied placeholder leaves also remain open after that completion.
        Assert.True(StructuralPattern.TryApplyComplete(next, out var supplied, out var unbound, binding));
        Assert.Same(outside, supplied);
        Assert.Empty(unbound);
        Assert.False(StructuralPattern.IsComplete(supplied));
    }

    [Fact]
    public void AdvanceSubstitutesExplicitSlotExpressionsWithoutFollowingOrCheckingStateEdges()
    {
        var a = new Placeholder();
        var b = new Placeholder();
        var first = new Relation(a, a);
        var second = new Relation(a, b);
        var third = new Relation(b, a);
        var firstToSecond = new Relation(first, second);
        var secondToThird = new Relation(second, third);
        var current = new Placeholder();
        var next = new Placeholder();
        var firstFrame = new[] { StructuralPattern.Bind(current, first) };
        var secondFrame = new[] { StructuralPattern.Bind(current, second) };

        // These edges have relation-valued left sides, not binding slots.
        Assert.Empty(StructuralPattern.Advance(firstFrame, firstToSecond, secondToThird));

        var copied = StructuralPattern.Advance(firstFrame, StructuralPattern.Bind(next, current));
        Assert.Same(first, Assert.Single(copied).Right);

        // A requested write to third is unconditional, even from first, where
        // the authored edge surface offers only first -> second.
        var writeThird = StructuralPattern.Bind(next, third);
        var fromFirst = Assert.IsType<Relation>(Assert.Single(StructuralPattern.Advance(firstFrame, writeThird)).Right);
        var fromSecond = Assert.IsType<Relation>(Assert.Single(StructuralPattern.Advance(secondFrame, writeThird)).Right);
        Assert.Equal(third, fromFirst);
        Assert.Equal(third, fromSecond);
        Assert.NotSame(third, fromFirst);
        Assert.NotSame(third, fromSecond);
        Assert.Same(first, firstToSecond.Left);
        Assert.Same(second, firstToSecond.Right);
    }
}
