using System.Runtime.InteropServices;
using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.PrimitiveRuntime;
using Gojo.Experiments.QualitativeExecution;

namespace Gojo.Experiments.PrimitiveRuntime.Tests;

public sealed class CompactRuntimeTests
{
    [Fact]
    public void ArenaPreservesOrderedRelationsSharedAliasesAndPlaceholderFaces()
    {
        var site = new Placeholder(); var opposed = site.Dual();
        var a = new Relation(site, opposed); var b = new Relation(site, opposed);
        var root = new Relation(a, new Relation(b, a));
        var arena = CompactArena.Capture(root);
        var first = arena[arena.AddressOf(a)]; var second = arena[arena.AddressOf(b)];
        Assert.Equal(16, Marshal.SizeOf<CompactRecord>());
        Assert.NotEqual(arena.AddressOf(a), arena.AddressOf(b));
        Assert.Equal(first.Left, second.Left); Assert.Equal(first.Right, second.Right);
        Assert.NotEqual(first.Left, first.Right);
        Assert.Equal(arena[first.Left].Left, arena[first.Right].Left);
        Assert.Equal(0u, arena[first.Left].Right); Assert.Equal(1u, arena[first.Right].Right);
        Assert.Equal(first.Left, arena.AddressOf(site.Dual().Dual()));
        Assert.Equal(arena.AddressOf(a), arena[arena[arena.AddressOf(root)].Right].Right);
        var restored = arena.Materialize();
        var restoredA = Assert.IsType<Relation>(restored[arena.AddressOf(a)]);
        var restoredB = Assert.IsType<Relation>(restored[arena.AddressOf(b)]);
        Assert.NotSame(a, restoredA); Assert.NotSame(restoredA, restoredB);
        Assert.Same(restoredA.Left, restoredB.Left);
        Assert.True(Assert.IsType<Placeholder>(restoredA.Left).SameSlotAs(Assert.IsType<Placeholder>(restoredA.Right)));
        Assert.Throws<ArgumentException>(() => CompactArena.Capture(new Carrier()));
    }

    [Fact]
    public void AllClosedSupportReadsAgreeWithNativeLawIncludingMissingRows()
    {
        var f = Fixture();
        for (ulong state = 0; state < 8; state++)
            for (ulong command = 0; command < 4; command++)
            {
                var native = FiniteLaw.Evaluate(f.Law, Support(state, f.States), Support(command, f.Commands));
                Assert.True(FiniteLaw.TryReadResult(native, out var support, out _, out var proof));
                var expected = FiniteLaw.Entries(support).Aggregate(0UL, (mask, item) => mask | f.Plan.StateMask(item));
                var compact = f.Plan.Read(CompactSupport.Closed(state), CompactSupport.Closed(command), out _, out var matched);
                Assert.Equal(expected, compact.Known); Assert.False(compact.IsOpen);
                // Unique authored rows mean the sum over atomic pairs is the same
                // actual proof set as the native read.
                Assert.Equal((uint)FiniteLaw.Entries(proof).Length, matched);
            }
        Assert.Equal(f.Plan.Arena.AddressOf(f.Rows[0]), Assert.Single(f.Plan.ProofRows(0, 0).ToArray()));
    }

    [Fact]
    public void OpenContradictoryAmbiguousAndMissingTransitionsRemainInspectable()
    {
        var f = Fixture(); var occurrence = f.Plan.CreateOccurrence(CompactSupport.Closed(1));
        var unknown = CompactSupport.Open(0, f.Plan.Arena.AddressOf(f.Open));
        var open = occurrence.Step(unknown);
        Assert.Equal(CompactStepStatus.Open, open.Status); Assert.True(open.Result.IsOpen);
        Assert.Equal(1UL, occurrence.State.Known);
        var impossible = occurrence.Step(CompactSupport.Closed(0));
        Assert.Equal(CompactStepStatus.Contradiction, impossible.Status);
        Assert.False(impossible.Result.IsOpen); Assert.Equal(0UL, impossible.Result.Known);
        var alternatives = occurrence.Step(CompactSupport.Closed(3));
        Assert.Equal(CompactStepStatus.NoTransition, alternatives.Status);
        Assert.Equal(2UL, alternatives.Result.Known); Assert.Equal(1u, alternatives.MissingPairs);
        Assert.Equal(1UL, occurrence.State.Known);
        occurrence.Import(CompactSupport.Closed(3));
        var ambiguous = occurrence.Step(CompactSupport.Closed(1));
        Assert.Equal(CompactStepStatus.Ambiguous, ambiguous.Status);
        Assert.Equal(6UL, ambiguous.Result.Known); Assert.Equal(3UL, occurrence.State.Known);
        var emptyOpen = f.Plan.Read(CompactSupport.Closed(0), unknown, out _, out _);
        Assert.Equal(CompactSupport.Closed(0), emptyOpen);
    }

    [Fact]
    public void TupleAtomsPreserveCorrelationAndEqualLookingTuplesStayDistinct()
    {
        var left = new Placeholder(); var right = new Placeholder();
        var small = new Placeholder(); var large = new Placeholder(); var command = new Placeholder();
        var first = new Relation(left, small); var second = new Relation(right, large);
        var lookalike = new Relation(left, small);
        var states = StructuralConstraint.Closed(first, second, lookalike);
        var law = FiniteLaw.Create(states, StructuralConstraint.Closed(command), states,
            FiniteLaw.Row(first, command, StructuralConstraint.Closed(second)),
            FiniteLaw.Row(second, command, StructuralConstraint.Closed(first)),
            FiniteLaw.Row(lookalike, command, StructuralConstraint.Closed(lookalike)));
        var plan = CompactTransitionPlan.Capture(law);
        Assert.NotEqual(plan.StateMask(first), plan.StateMask(lookalike));
        var result = plan.Read(CompactSupport.Closed(3), CompactSupport.Closed(1), out _, out _);
        Assert.Equal(3UL, result.Known);
        Assert.Equal(3, plan.StateCount);
        Assert.Throws<ArgumentException>(() => plan.StateMask(new Relation(left, large)));
    }

    [Fact]
    public void InvalidSupportsRefuseAtomicallyAndCurrentReceiptIsStable()
    {
        var f = Fixture(); var occurrence = f.Plan.CreateOccurrence(CompactSupport.Closed(1));
        var captured = occurrence.Step(CompactSupport.Closed(1));
        Assert.Equal(CompactStepStatus.Applied, captured.Status); Assert.Equal(2UL, occurrence.State.Known);
        Assert.Throws<ArgumentException>(() => occurrence.Step(CompactSupport.Closed(4)));
        Assert.Throws<ArgumentException>(() => occurrence.Step(CompactSupport.Open(1, f.Plan.LawAddress)));
        Assert.Throws<ArgumentException>(() => occurrence.Import(new CompactSupport(1, 0, 1)));
        Assert.Equal(1UL, occurrence.CurrentStep); Assert.Equal(captured, occurrence.CurrentReceipt);
        Assert.Throws<ArgumentException>(() => f.Plan.InspectReceipt(captured with
            { Status = CompactStepStatus.StaleDefinition, After = captured.Before }));
        occurrence.Step(CompactSupport.Closed(1));
        Assert.Equal(2UL, occurrence.CurrentStep); Assert.Equal(4UL, occurrence.State.Known);
        Assert.Equal(1UL, captured.Before.Known); Assert.Equal(2UL, captured.After.Known);
    }

    [Fact]
    public void DuplicateNativeRoutesDoNotBecomeOneGuardedApplication()
    {
        var a = new Placeholder(); var b = new Placeholder(); var command = new Placeholder();
        var domain = StructuralConstraint.Closed(a, b);
        var law = FiniteLaw.Create(domain, StructuralConstraint.Closed(command), domain,
            FiniteLaw.Row(a, command, StructuralConstraint.Closed(b)),
            FiniteLaw.Row(a, command, StructuralConstraint.Closed(b)));
        var plan = CompactTransitionPlan.Capture(law); var occurrence = plan.CreateOccurrence(CompactSupport.Closed(1));
        var receipt = occurrence.Step(CompactSupport.Closed(1));
        Assert.Equal(2UL, receipt.Result.Known); Assert.Equal(2u, receipt.MatchedRows);
        Assert.Equal(CompactStepStatus.Ambiguous, receipt.Status); Assert.Equal(1UL, occurrence.State.Known);
    }

    [Fact]
    public void FrozenInspectionAndExplicitSourceValidationSurviveSimulatedHostEdits()
    {
        var f = Fixture(); var occurrence = f.Plan.CreateOccurrence(CompactSupport.Closed(1));
        var receipt = occurrence.Step(CompactSupport.Closed(1));
        var firstInspection = f.Plan.InspectReceipt(receipt);
        Assert.True(FiniteLaw.TryReadResult(firstInspection.NativeRead, out var result, out _, out var proof));
        Assert.Same(firstInspection.Snapshot[f.Plan.Arena.AddressOf(f.States[1])], Assert.Single(FiniteLaw.Entries(result)));
        Assert.Same(firstInspection.Snapshot[f.Plan.Arena.AddressOf(f.Rows[0])], Assert.Single(FiniteLaw.Entries(proof)));
        Assert.NotSame(f.Rows[0], Assert.Single(FiniteLaw.Entries(proof)));

        // Carrier-free relation topology has no public in-place edit API. Invoke
        // the archive's internal wiring hook solely to exercise the stale-source
        // boundary against an actual changed source object.
        var wiring = typeof(Relation).GetMethod("RestoreArchiveSides",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
        wiring.Invoke(f.Rows[0], [f.Rows[0].Left, StructuralConstraint.Closed(f.States[2])]);
        var laterInspection = f.Plan.InspectReceipt(receipt);
        Assert.True(FiniteLaw.TryReadResult(laterInspection.NativeRead, out var laterResult, out _, out _));
        Assert.Same(laterInspection.Snapshot[f.Plan.Arena.AddressOf(f.States[1])], Assert.Single(FiniteLaw.Entries(laterResult)));
        Assert.False(f.Plan.ValidateSource());
        var held = occurrence.State;
        Assert.Equal(CompactStepStatus.StaleDefinition, occurrence.Step(CompactSupport.Closed(1)).Status);
        Assert.Equal(held, occurrence.State);
        Assert.Throws<InvalidOperationException>(() => occurrence.Import(CompactSupport.Closed(1)));
        Assert.Same(firstInspection.Snapshot[f.Plan.Arena.AddressOf(f.States[1])], Assert.Single(FiniteLaw.Entries(result)));
    }

    [Fact]
    public void OpenCausesRemainSeparateAndOpposedPlaceholderFacesAreNotScopeTails()
    {
        var state = new Placeholder(); var command = new Placeholder();
        var firstTail = new Placeholder(); var secondTail = new Placeholder(); var opposed = firstTail.Dual();
        var domain = StructuralConstraint.Closed(state);
        var law = FiniteLaw.Create(domain, StructuralConstraint.Closed(command), domain,
            FiniteLaw.Row(state, command, StructuralConstraint.Closed(state)));
        var plan = CompactTransitionPlan.Capture(law, firstTail, secondTail, opposed);
        var occurrence = plan.CreateOccurrence(CompactSupport.Open(1, plan.Arena.AddressOf(firstTail)));
        var receipt = occurrence.Step(CompactSupport.Open(1, plan.Arena.AddressOf(secondTail)));
        var inspection = plan.InspectReceipt(receipt);
        Assert.True(StructuralConstraint.TryReadScope(inspection.Before, out _, out _, out var beforeTail));
        Assert.True(StructuralConstraint.TryReadScope(inspection.Command, out _, out _, out var commandTail));
        Assert.NotSame(beforeTail, commandTail);
        Assert.Same(inspection.Snapshot[plan.Arena.AddressOf(firstTail)], beforeTail);
        Assert.Same(inspection.Snapshot[plan.Arena.AddressOf(secondTail)], commandTail);
        Assert.Throws<ArgumentException>(() => occurrence.Step(CompactSupport.Open(1, plan.Arena.AddressOf(opposed))));
        Assert.Throws<ArgumentException>(() => occurrence.Step(CompactSupport.Open(1, uint.MaxValue)));
    }

    [Fact]
    public void LongLiveRunsHaveNoPerStepAllocationAndOnlyCurrentEvidence()
    {
        var f = Fixture(); var occurrence = f.Plan.CreateOccurrence(CompactSupport.Closed(1));
        var command = CompactSupport.Closed(1);
        for (var i = 0; i < 2000; i++) occurrence.Step(command);
        var allocation = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 100_000; i++) occurrence.Step(command);
        Assert.Equal(0, GC.GetAllocatedBytesForCurrentThread() - allocation);
        Assert.Equal(102000UL, occurrence.CurrentStep);
        Assert.Equal(16, Marshal.SizeOf<CompactSupport>());
        Assert.Equal(104, Marshal.SizeOf<CompactStepReceipt>());
        Assert.Equal(128, CompactOccurrence.MutablePayloadBytes);
        Assert.Equal(168, CompactOccurrence.EstimatedObjectBytes);
        Assert.Equal(102000UL, occurrence.CurrentReceipt.Step);
        Assert.True(f.Plan.ValidateSource());
    }

    [Fact]
    public void OccurrencesQualifyTemplateSitesAndExposeStateSlotBindings()
    {
        var f = Fixture(); var slot = f.Plan.Arena.AddressOf(f.Open);
        var first = f.Plan.CreateOccurrence(CompactSupport.Closed(1), slot);
        var second = f.Plan.CreateOccurrence(CompactSupport.Closed(1), slot);
        Assert.NotEqual(first.OccurrenceIdentity, second.OccurrenceIdentity);
        var a = first.Step(CompactSupport.Closed(1)); var b = second.Step(CompactSupport.Closed(1));
        Assert.Equal(slot, a.StateSlotAddress); Assert.Equal(first.OccurrenceIdentity, a.OccurrenceIdentity);
        Assert.NotEqual(a.OccurrenceIdentity, b.OccurrenceIdentity);
        var inspection = f.Plan.InspectReceipt(a);
        Assert.Same(inspection.Snapshot[slot], inspection.BeforeBinding.Left);
        Assert.Same(inspection.BeforeBinding.Left, inspection.AfterBinding.Left);
        Assert.Same(inspection.Before, inspection.BeforeBinding.Right);
        Assert.Same(inspection.After, inspection.AfterBinding.Right);
        Assert.Throws<ArgumentException>(() => f.Plan.CreateOccurrence(CompactSupport.Closed(1), f.Plan.LawAddress));
    }

    private static Relation Support(ulong mask, IElement[] states) =>
        StructuralConstraint.Closed(states.Where((_, i) => (mask & 1UL << i) != 0).ToArray());

    private static (Relation Law, Relation[] Rows, IElement[] States, IElement[] Commands, Placeholder Open, CompactTransitionPlan Plan) Fixture()
    {
        IElement[] states = [new Placeholder(), new Placeholder(), new Placeholder()];
        IElement[] commands = [new Placeholder(), new Placeholder()]; var open = new Placeholder();
        var domain = StructuralConstraint.Closed(states);
        Relation[] rows = [FiniteLaw.Row(states[0], commands[0], StructuralConstraint.Closed(states[1])),
            FiniteLaw.Row(states[1], commands[0], StructuralConstraint.Closed(states[2])),
            FiniteLaw.Row(states[2], commands[0], StructuralConstraint.Closed(states[0]))];
        var law = FiniteLaw.Create(domain, StructuralConstraint.Closed(commands), domain, rows);
        return (law, rows, states, commands, open, CompactTransitionPlan.Capture(law, open));
    }
}
