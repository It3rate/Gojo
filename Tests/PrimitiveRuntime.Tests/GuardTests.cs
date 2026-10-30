using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;

namespace Gojo.Experiments.PrimitiveRuntime.Tests;

public sealed class GuardTests
{
    [Fact]
    public void AllClosedSupportsAgreeWithNativeGuardedApplication()
    {
        var a = new Placeholder(); var b = new Placeholder(); var c = new Placeholder();
        var pulse = new Placeholder(); var block = new Placeholder();
        IElement[] states = [a, b, c]; IElement[] commands = [pulse, block];
        var domain = StructuralConstraint.Closed(states);
        var law = FiniteLaw.Create(domain, StructuralConstraint.Closed(commands), domain,
            FiniteLaw.Row(a, pulse, StructuralConstraint.Closed(b)),
            FiniteLaw.Row(b, pulse, StructuralConstraint.Closed(c)),
            FiniteLaw.Row(c, pulse, StructuralConstraint.Closed(a)));
        var guard = new TransitionApplicationGuard(law);
        var plan = CompactTransitionPlan.Capture(law);
        for (ulong before = 0; before < 8; before++)
            for (ulong input = 0; input < 4; input++)
            {
                var accepted = guard.TryApply(Support(before, states), Support(input, commands), out var applied, out var read);
                var occurrence = plan.CreateOccurrence(CompactSupport.Closed(before));
                var receipt = occurrence.Step(CompactSupport.Closed(input));
                Assert.Equal(accepted, receipt.Applied);
                Assert.True(StructuralConstraint.TryRead(read, out _, out _, out _, out _));
                if (accepted) Assert.Equal(plan.StateMask(applied), receipt.After.Known);
                else { Assert.Equal(before, receipt.After.Known); Assert.Same(guard.OutputSlot, applied); }
            }
    }

    [Fact]
    public void OpenScopesAndCompetingRowsCannotPassClosedSingletonShape()
    {
        var a = new Placeholder(); var b = new Placeholder(); var command = new Placeholder();
        var domain = StructuralConstraint.Closed(a, b);
        var row = FiniteLaw.Row(a, command, StructuralConstraint.Closed(b));
        var law = FiniteLaw.Create(domain, StructuralConstraint.Closed(command), domain, row);
        var guard = new TransitionApplicationGuard(law);
        Assert.False(guard.TryApply(StructuralConstraint.Open(a), StructuralConstraint.Closed(command), out _, out _));
        Assert.False(guard.TryApply(StructuralConstraint.Closed(a), StructuralConstraint.Open(command), out _, out _));
        Assert.False(guard.TryApply(StructuralConstraint.Closed(a, b), StructuralConstraint.Closed(command), out _, out _));
        var competing = FiniteLaw.Create(domain, StructuralConstraint.Closed(command), domain,
            row, FiniteLaw.Row(a, command, StructuralConstraint.Closed(b)));
        Assert.False(new TransitionApplicationGuard(competing).TryApply(StructuralConstraint.Closed(a),
            StructuralConstraint.Closed(command), out _, out _));
    }

    [Fact]
    public void OneSuccessfulRowCannotHideAnotherApplicableEmptyOrMultiOutputRow()
    {
        var a = new Placeholder(); var b = new Placeholder(); var command = new Placeholder();
        var domain = StructuralConstraint.Closed(a, b);
        foreach (var extra in new[] { StructuralConstraint.Closed(), StructuralConstraint.Closed(a, b) })
        {
            var law = FiniteLaw.Create(domain, StructuralConstraint.Closed(command), domain,
                FiniteLaw.Row(a, command, StructuralConstraint.Closed(b)), FiniteLaw.Row(a, command, extra));
            var guard = new TransitionApplicationGuard(law);
            Assert.False(guard.TryApply(StructuralConstraint.Closed(a), StructuralConstraint.Closed(command), out _, out _));
            var plan = CompactTransitionPlan.Capture(law);
            Assert.False(plan.CreateOccurrence(CompactSupport.Closed(1)).Step(CompactSupport.Closed(1)).Applied);
        }
    }

    [Fact]
    public void CapturedGuardKeepsLawAliasesAndCanExecuteAfterIndependentMaterialization()
    {
        var definition = new MotionDefinition(); var guard = new TransitionApplicationGuard(definition.Law);
        var arena = CompactArena.Capture(guard.Surface, definition.States[2], definition.Pulse);
        var snapshot = arena.Materialize();
        var restored = TransitionApplicationGuard.FromSurface((Relation)snapshot[arena.AddressOf(guard.Surface)]);
        Assert.Same(snapshot[arena.AddressOf(definition.Law)], restored.Law);
        Assert.NotSame(definition.Law, restored.Law);
        Assert.True(restored.TryApply(StructuralConstraint.Closed(snapshot[arena.AddressOf(definition.States[2])]),
            StructuralConstraint.Closed(snapshot[arena.AddressOf(definition.Pulse)]), out var applied, out _));
        Assert.Same(snapshot[arena.AddressOf(definition.States[4])], applied);
        var visited = new HashSet<IElement>(ReferenceEqualityComparer.Instance);
        var pending = new Stack<IElement>(); pending.Push(guard.Surface);
        while (pending.TryPop(out var item))
        {
            if (!visited.Add(item)) continue;
            Assert.True(item is Relation or Placeholder);
            if (item is Relation relation) { pending.Push(relation.Left); pending.Push(relation.Right); }
        }
    }

    private static Relation Support(ulong mask, IElement[] participants) => StructuralConstraint.Closed(
        participants.Where((_, index) => (mask & (1UL << index)) != 0).ToArray());
}
