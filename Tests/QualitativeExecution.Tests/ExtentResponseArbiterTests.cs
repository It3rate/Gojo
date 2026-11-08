using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;
using Gojo.Experiments.QualitativeFrieze.Composition;

namespace QualitativeExecution.Tests;

public sealed class ExtentResponseArbiterTests
{
    [Fact]
    public void Agreement_selects_one_response_without_adding_the_two_offers()
    {
        var machine = CoupledExtentMachine.Create(2);
        var selector = ExtentResponseArbiter.Create(machine);
        var read = selector.Evaluate(machine.Publication(4), machine.Publication(4));
        Assert.True(read.Resolved); Assert.False(read.Conflict); Assert.Equal(4, read.Mask);
        Assert.Empty(FiniteLaw.Entries(read.Residue));
        var step = machine.Step(read.Packet);
        Assert.Equal(1, step.IntroducedDelta); Assert.Equal(3, step.After.Value);
        Assert.Equal(0, step.After.Tension);
    }

    [Fact]
    public void Conflicting_offers_under_agreement_remain_visible_and_do_not_cancel_into_zero()
    {
        var machine = CoupledExtentMachine.Create(2);
        var selector = ExtentResponseArbiter.Create(machine);
        var first = machine.Publication(1); var second = machine.Publication(4);
        var read = selector.Evaluate(first, second);
        Assert.True(read.Conflict); Assert.False(read.Resolved); Assert.Equal(0, read.Mask);
        Assert.Equal(new IElement[] { first, second }, FiniteLaw.Entries(read.Residue));
        var step = machine.Step(read.Packet);
        Assert.Null(step.RequestedDelta); Assert.False(step.Resolved);
        Assert.Equal(2, step.After.Value); Assert.Equal(0, step.After.Tension);
    }

    [Theory]
    [InlineData("first", 1)]
    [InlineData("second", 4)]
    public void Native_preference_selects_one_conflicting_offer_and_retains_the_other(string policy, byte expected)
    {
        var machine = CoupledExtentMachine.Create(2);
        var selector = ExtentResponseArbiter.Create(machine, policy);
        var first = machine.Publication(1); var second = machine.Publication(4);
        var read = selector.Evaluate(first, second);
        Assert.True(read.Conflict); Assert.True(read.Resolved); Assert.Equal(expected, read.Mask);
        Assert.Same(expected == 1 ? second : first, Assert.Single(FiniteLaw.Entries(read.Residue)));
        Assert.Equal(2, machine.Snapshot().Value); // A selector reads; it does not actuate.
    }

    [Theory]
    [InlineData(0)]
    [InlineData(3)]
    [InlineData(5)]
    [InlineData(7)]
    public void Preference_fallback_uses_a_settled_offer_without_refining_the_unresolved_one(byte firstMask)
    {
        var machine = CoupledExtentMachine.Create();
        var selector = ExtentResponseArbiter.Create(machine, "first");
        var first = machine.Publication(firstMask); var second = machine.Publication(4);
        var read = selector.Evaluate(first, second);
        Assert.False(read.FirstEligible); Assert.True(read.SecondEligible);
        Assert.True(read.Resolved); Assert.Equal(4, read.Mask);
        Assert.Same(first, Assert.Single(FiniteLaw.Entries(read.Residue)));
        QualitativePublication.TryRead(first, out _, out _, out _, out _, out _, out var before, out _);
        Assert.Equal(firstMask == 0 ? 0 : firstMask == 7 ? 3 : 2, FiniteLaw.Entries(before).Length);
    }

    [Fact]
    public void Known_equal_is_a_response_while_missing_or_ambiguous_is_not()
    {
        var machine = CoupledExtentMachine.Create(2);
        var selector = ExtentResponseArbiter.Create(machine);
        var equal = selector.Evaluate(machine.Publication(2), machine.Publication(2));
        Assert.True(equal.Resolved); Assert.Equal(2, equal.Mask);
        Assert.Equal(0, machine.Step(equal.Packet).RequestedDelta);
        foreach (var uncertain in new byte[] { 0, 3, 7 })
        {
            var read = selector.Evaluate(machine.Publication(2), machine.Publication(uncertain));
            Assert.False(read.Resolved); Assert.Equal(0, read.Mask);
            Assert.Null(machine.Step(read.Packet).RequestedDelta);
            Assert.Equal(2, FiniteLaw.Entries(read.Residue).Length);
        }
    }

    [Fact]
    public void Different_receiver_or_domain_is_not_a_local_eligible_offer()
    {
        var machine = CoupledExtentMachine.Create(2); var stranger = CoupledExtentMachine.Create(2);
        var selector = ExtentResponseArbiter.Create(machine, "first");
        var foreign = stranger.Publication(1); var local = machine.Publication(4);
        var read = selector.Evaluate(foreign, local);
        Assert.False(read.FirstEligible); Assert.True(read.SecondEligible); Assert.Equal(4, read.Mask);
        Assert.Same(foreign, Assert.Single(FiniteLaw.Entries(read.Residue)));
        var neither = selector.Evaluate(foreign, stranger.Publication(4));
        Assert.False(neither.Resolved); Assert.Equal(0, neither.Mask);
    }

    [Fact]
    public void A_packet_retains_only_opaque_selector_evidence_while_local_witness_keeps_sources()
    {
        var machine = CoupledExtentMachine.Create(2); var source = CoupledExtentMachine.Create(5, unit: machine.StepUnit.UnitShape);
        var link = CoupledComparisonLink.Create(source.ExtentPort, machine); var observation = link.Observe();
        var selector = ExtentResponseArbiter.Create(machine, "first");
        var read = selector.Evaluate(observation.Packet, machine.Publication(1, evidence: observation.Witness));
        Assert.DoesNotContain(Reachable(read.Packet), item => ReferenceEquals(item, source.ExtentPort));
        Assert.DoesNotContain(Reachable(read.Packet), item => ReferenceEquals(item, observation.Packet));
        Assert.Contains(Reachable(read.Witness), item => ReferenceEquals(item, source.ExtentPort));
        Assert.Contains(Reachable(read.Witness), item => ReferenceEquals(item, observation.Packet));
    }

    [Fact]
    public void Selection_events_are_fresh_but_retransmitting_one_packet_is_deduplicated()
    {
        var machine = CoupledExtentMachine.Create(2); var selector = ExtentResponseArbiter.Create(machine);
        var first = machine.Publication(4); var second = machine.Publication(4);
        var a = selector.Evaluate(first, second); var b = selector.Evaluate(first, second);
        QualitativePublication.TryRead(a.Packet, out _, out _, out var eventA, out _, out _, out _, out _);
        QualitativePublication.TryRead(b.Packet, out _, out _, out var eventB, out _, out _, out _, out _);
        Assert.NotSame(eventA, eventB);
        Assert.True(machine.Step(a.Packet).ConsumedEvent);
        Assert.False(machine.Step(a.Packet).ConsumedEvent);
        Assert.Equal(3, machine.Snapshot().Value);
        Assert.True(machine.Step(b.Packet).ConsumedEvent);
        Assert.Equal(4, machine.Snapshot().Value);
    }

    [Fact]
    public void Every_finite_support_pair_has_the_same_native_and_compiled_result()
    {
        var machine = CoupledExtentMachine.Create();
        foreach (var policy in new[] { "agreement", "first", "second" })
        {
            var selector = ExtentResponseArbiter.Create(machine, policy);
            for (byte a = 0; a < 8; a++)
                for (byte b = 0; b < 8; b++)
                {
                    var first = machine.Publication(a); var second = machine.Publication(b);
                    var native = selector.Evaluate(first, second, compiled: false);
                    var compiled = selector.Evaluate(first, second, compiled: true);
                    Assert.Equal(native.Mask, compiled.Mask); Assert.Equal(native.Resolved, compiled.Resolved);
                    Assert.Equal(native.Conflict, compiled.Conflict);
                    Assert.Equal(FiniteLaw.Entries(native.Residue), FiniteLaw.Entries(compiled.Residue));
                }
        }
    }

    [Fact]
    public void Restoring_the_composed_graph_preserves_receiver_aliases_and_native_preference()
    {
        var machine = CoupledExtentMachine.Create(2); var selector = ExtentResponseArbiter.Create(machine, "second");
        var roots = GojoNumericFormat.Decode(GojoNumericFormat.Encode(machine.ArchiveSurface(), selector.Surface));
        var restored = CoupledExtentMachine.Restore((Relation)roots[0]);
        var restoredSelector = ExtentResponseArbiter.FromSurface((Relation)roots[1]);
        var read = restoredSelector.Evaluate(restored.Publication(1), restored.Publication(4));
        Assert.Equal(4, read.Mask); Assert.True(restored.Step(read.Packet).ConsumedEvent);
        Assert.Equal(3, restored.Snapshot().Value); Assert.Equal(2, machine.Snapshot().Value);
        QualitativePublication.TryRead(read.Packet, out var source, out var subject, out _, out var basis, out var domain, out _, out _);
        Assert.Same(restored.Sensor, source); Assert.Same(restored.Owner, subject);
        Assert.Same(restored.ComparisonBasis, basis); Assert.Same(restored.ComparisonDomain, domain);
        Assert.False(restoredSelector.Evaluate(machine.Publication(1), machine.Publication(4)).Resolved);
    }

    private static IEnumerable<IElement> Reachable(IElement root)
    {
        var seen = new HashSet<IElement>(ReferenceEqualityComparer.Instance); var pending = new Stack<IElement>(); pending.Push(root);
        while (pending.TryPop(out var item))
        {
            if (!seen.Add(item)) continue;
            yield return item;
            if (item is Relation relation) { pending.Push(relation.Left); pending.Push(relation.Right); }
        }
    }
}
