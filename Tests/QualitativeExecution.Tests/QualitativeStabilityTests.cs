using Gojo.Core;
using Gojo.Patterns;

namespace Gojo.Experiments.QualitativeExecution.Tests;

public sealed class QualitativeStabilityTests
{
    [Fact]
    public void DeclaredFeedbackRestoresEveryStateInThisFiniteDomainAndHoldsTheTarget()
    {
        var f = new QualitativeStockFixture();
        var policy = Feedback(f, f.Charge, f.Hold, f.Draw);
        foreach (var initial in new[] { f.Empty, f.Available, f.Full })
        {
            var state = f.Initial(new Placeholder(), new Placeholder(), StructuralConstraint.Closed(initial));
            var first = Advance(f, state, policy);
            AssertCurrent(f, first.After, f.Available);
            var second = Advance(f, first.After, policy);
            AssertCurrent(f, second.After, f.Available);
            Assert.True(Contains(second.Step, first.After));
            Assert.True(Contains(second.Step, policy));
            Assert.Empty(second.Step.Carriers()); Assert.Empty(second.Step.CarrierLocations());
        }
    }

    [Fact]
    public void RemovingOrReversingFeedbackDoesNotPreserveTheRestorationClaim()
    {
        var f = new QualitativeStockFixture();
        var missing = FiniteLaw.Create(f.StateDomain, StructuralConstraint.Closed(f.Available), f.InputDomain);
        var reversed = Feedback(f, f.Draw, f.Hold, f.Charge);
        foreach (var initial in new[] { f.Empty, f.Full })
        {
            var state = f.Initial(new Placeholder(), new Placeholder(), StructuralConstraint.Closed(initial));
            var absent = Advance(f, state, missing);
            Assert.Same(state, absent.After);
            Assert.True(f.TryReadStep(absent.Step, out _, out _, out _, out _, out var unresolved));
            Assert.NotEmpty(FiniteLaw.Entries(unresolved));
            var wrong = Advance(f, state, reversed);
            AssertCurrent(f, wrong.After, initial);
            Assert.True(f.TryReadStep(wrong.Step, out _, out _, out _, out var alternatives, out _));
            Assert.True(QualitativeStockFixture.TryReadTransition(Assert.Single(FiniteLaw.Entries(alternatives)),
                out _, out _, out _, out var accepted, out var blocked));
            Assert.Empty(FiniteLaw.Entries(accepted)); Assert.Single(FiniteLaw.Entries(blocked));
        }
    }

    [Fact]
    public void QuietIndividualStepsDoNotMakeAnAlternatingFeedbackLoopStable()
    {
        var f = new QualitativeStockFixture();
        var policy = Feedback(f, f.Charge, f.Charge, f.Draw);
        var state = f.Initial(new Placeholder(), new Placeholder(), StructuralConstraint.Closed(f.Available));
        var first = Advance(f, state, policy);
        var second = Advance(f, first.After, policy);
        var third = Advance(f, second.After, policy);
        AssertCurrent(f, first.After, f.Full);
        AssertCurrent(f, second.After, f.Available);
        AssertCurrent(f, third.After, f.Full);
        Assert.True(Contains(third.Step, first.After));
        // This finite recurrent trace shows oscillation. It is not a numerical
        // period, elapsed-time rate, or a general theorem about arbitrary loops.
        Assert.Empty(third.Step.Carriers()); Assert.Empty(third.Step.CarrierLocations());
    }

    private static Relation Feedback(QualitativeStockFixture f, IElement empty, IElement available, IElement full) =>
        FiniteLaw.Create(f.StateDomain, StructuralConstraint.Closed(f.Available), f.InputDomain,
            FiniteLaw.Row(f.Empty, f.Available, StructuralConstraint.Closed(empty)),
            FiniteLaw.Row(f.Available, f.Available, StructuralConstraint.Closed(available)),
            FiniteLaw.Row(f.Full, f.Available, StructuralConstraint.Closed(full)));

    private static (Relation After, Relation Step) Advance(QualitativeStockFixture f, Relation state, Relation policy)
    {
        Assert.True(f.TryReadState(state, out var source, out var eventIdentity, out var modes));
        var read = FiniteLaw.WithProvenance(FiniteLaw.Evaluate(policy, modes,
            StructuralConstraint.Closed(f.Available)), state);
        Assert.True(FiniteLaw.TryReadResult(read, out var input, out _, out _));
        var offer = f.Offer(source, new Placeholder(), eventIdentity, input, read);
        var step = f.Step(state, offer);
        Assert.True(f.TryReadStep(step, out _, out _, out var after, out _, out _));
        return (after, step);
    }

    private static void AssertCurrent(QualitativeStockFixture f, Relation state, IElement expected)
    {
        Assert.True(f.TryReadState(state, out _, out _, out var support));
        Assert.Same(expected, Assert.Single(FiniteLaw.Entries(support)));
    }

    private static bool Contains(IElement root, IElement target)
    {
        var seen = new HashSet<IElement>(ReferenceEqualityComparer.Instance);
        var pending = new Stack<IElement>(); pending.Push(root);
        while (pending.TryPop(out var element))
        {
            if (!seen.Add(element)) continue;
            if (ReferenceEquals(element, target)) return true;
            if (element is not Relation relation) continue;
            pending.Push(relation.Left); pending.Push(relation.Right);
        }
        return false;
    }
}
