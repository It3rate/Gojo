using Gojo.Core;
using Gojo.Patterns;

namespace Gojo.Experiments.QualitativeExecution.Tests;

public sealed class DemandRefinementTests
{
    [Fact]
    public void CompleteCategoricalAnswerNeverQueriesFinerEvidence()
    {
        var view = new ThresholdFixture().Forward;
        var observation = view.Observe(new Placeholder(), new Placeholder(),
            StructuralConstraint.Closed(view.Above), StructuralConstraint.Closed(view.Falling));
        var routes = Routes(view, out var yes, out _);
        var port = new Relation(observation, view.SideDomain);
        var receipt = QualitativeQuestion.Ask(view.ReadNative(observation), routes, port);

        Assert.True(QualitativeQuestion.TryReadAnswer(receipt, out var answer));
        Assert.Same(yes, answer);
        var serviced = QualitativeQuestion.Service(receipt, _ =>
            throw new InvalidOperationException("No finer signal is available in this system."));
        Assert.Empty(FiniteLaw.Entries((Relation)serviced.Right));
        AssertNoNumbers(receipt);
    }

    [Fact]
    public void MultipleOutcomesCanAnswerOneQuestionWithoutResolvingTheirDifferences()
    {
        var view = new ThresholdFixture().Forward;
        var observation = view.Observe(new Placeholder(), new Placeholder(),
            StructuralConstraint.Closed(view.Above, view.WithinBand), StructuralConstraint.Closed(view.Steady));
        // OutsideSteady and WithinSteady are distinct outcomes, but both answer
        // this particular question: is there evidence of approach?
        var routes = Routes(view, out _, out var no);
        var receipt = QualitativeQuestion.Ask(view.ReadNative(observation), routes, new Placeholder());
        Assert.True(QualitativeQuestion.TryReadAnswer(receipt, out var answer));
        Assert.Same(no, answer);
        QualitativeQuestion.Service(receipt, _ => throw new InvalidOperationException("Unneeded detail."));
    }

    [Fact]
    public void AmbiguityRequestsOnlyTheDeclaredCategoricalPortAndFreshEvidenceNarrowsSameEvent()
    {
        var view = new ThresholdFixture().Forward;
        var source = new Placeholder();
        var eventIdentity = new Placeholder();
        var observation = view.Observe(source, eventIdentity,
            StructuralConstraint.Closed(view.Below, view.Above), StructuralConstraint.Closed(view.Falling));
        var finerSidePort = new Relation(observation, view.SideDomain);
        var routes = Routes(view, out var yes, out _);
        var receipt = QualitativeQuestion.Ask(view.ReadNative(observation), routes, finerSidePort);
        Assert.False(QualitativeQuestion.TryReadAnswer(receipt, out _));
        Assert.True(QualitativeQuestion.TryRead(receipt, out var possibilities, out var requests, out _));
        Assert.Equal(2, FiniteLaw.Entries(possibilities).Length);
        Assert.Same(finerSidePort, Assert.Single(FiniteLaw.Entries(requests)));

        var visits = 0;
        var finer = view.Observe(source, eventIdentity,
            StructuralConstraint.Closed(view.Above), StructuralConstraint.Closed(view.Falling));
        var serviced = QualitativeQuestion.Service(receipt, port =>
        {
            Assert.Same(finerSidePort, port);
            visits++;
            return finer;
        });
        Assert.Equal(1, visits);
        Assert.True(Contains(serviced, observation));
        Assert.True(Contains(serviced, finer));
        Assert.True(DetectorRefinement.TryAccept(view, observation, finer, out var narrowed, out var witness));
        Assert.True(Contains(witness, observation));
        Assert.True(Contains(witness, finer));
        Assert.True(Contains(narrowed, observation));
        var refinedQuestion = QualitativeQuestion.Ask(view.ReadCompiled(narrowed), routes, finerSidePort);
        Assert.True(QualitativeQuestion.TryReadAnswer(refinedQuestion, out var refinedAnswer));
        Assert.Same(yes, refinedAnswer);
        AssertNoNumbers(new Relation(serviced, witness));
    }

    [Fact]
    public void ClosedEmptyPremiseNeverProducesADefaultAnswerOrAnAutomaticRefinementRequest()
    {
        var view = new ThresholdFixture().Forward;
        var read = FiniteLaw.Evaluate(view.Law, StructuralConstraint.Closed(), view.TrendDomain);
        var receipt = QualitativeQuestion.Ask(read, Routes(view, out _, out _), new Placeholder());
        Assert.False(QualitativeQuestion.TryReadAnswer(receipt, out _));
        Assert.True(QualitativeQuestion.TryRead(receipt, out var answers, out var requests, out _));
        Assert.Empty(FiniteLaw.Entries(answers));
        Assert.Empty(FiniteLaw.Entries(requests));
        QualitativeQuestion.Service(receipt, _ => throw new InvalidOperationException("Conflict requires a decision, not an invented sample."));
    }

    [Fact]
    public void OpenPremiseRetainsKnownAnswerButCannotSettleIt()
    {
        var view = new ThresholdFixture().Forward;
        var read = FiniteLaw.Evaluate(view.Law, StructuralConstraint.Open(view.Above),
            StructuralConstraint.Closed(view.Falling));
        var port = new Placeholder();
        var receipt = QualitativeQuestion.Ask(read, Routes(view, out var yes, out _), port);
        Assert.False(QualitativeQuestion.TryReadAnswer(receipt, out _));
        Assert.True(QualitativeQuestion.TryRead(receipt, out var answers, out var requests, out _));
        Assert.Same(yes, Assert.Single(FiniteLaw.Entries(answers)));
        Assert.True(StructuralConstraint.TryReadScope(answers, out _, out var seam, out var tail));
        Assert.NotEqual(seam, tail);
        Assert.Same(port, Assert.Single(FiniteLaw.Entries(requests)));
    }

    [Fact]
    public void OpenOrMissingResponseRoutesPreserveSupportedAnswersWithoutPretendingTheyAreComplete()
    {
        var view = new ThresholdFixture().Forward;
        var read = FiniteLaw.Evaluate(view.Law, StructuralConstraint.Closed(view.Above),
            StructuralConstraint.Closed(view.Falling));
        var yes = new Placeholder();
        var port = new Placeholder();
        var receipt = QualitativeQuestion.Ask(read,
            StructuralConstraint.Open(new Relation(view.Approaching, yes)), port);
        Assert.False(QualitativeQuestion.TryReadAnswer(receipt, out _));
        Assert.True(QualitativeQuestion.TryRead(receipt, out var answers, out var requests, out _));
        Assert.Same(yes, Assert.Single(FiniteLaw.Entries(answers)));
        Assert.Same(port, Assert.Single(FiniteLaw.Entries(requests)));
        var missing = QualitativeQuestion.Ask(read, StructuralConstraint.Closed(), port);
        Assert.False(QualitativeQuestion.TryReadAnswer(missing, out _));
        Assert.True(QualitativeQuestion.TryRead(missing, out _, out var missingRequests, out _));
        Assert.Same(port, Assert.Single(FiniteLaw.Entries(missingRequests)));
    }

    [Fact]
    public void ContraryOrBroaderRefinementIsBlockedAndTheOriginalObservationSurvives()
    {
        var view = new ThresholdFixture().Forward;
        var source = new Placeholder();
        var eventIdentity = new Placeholder();
        var original = view.Observe(source, eventIdentity,
            StructuralConstraint.Closed(view.Above), StructuralConstraint.Closed(view.Falling));
        foreach (var offered in new[]
        {
            view.Observe(source, eventIdentity, StructuralConstraint.Closed(view.Below), StructuralConstraint.Closed(view.Falling)),
            view.Observe(source, eventIdentity, view.SideDomain, StructuralConstraint.Closed(view.Falling)),
            view.Observe(source, eventIdentity, StructuralConstraint.Closed(), StructuralConstraint.Closed(view.Falling)),
            view.Observe(source, eventIdentity, StructuralConstraint.Open(view.Above), StructuralConstraint.Closed(view.Falling))
        })
        {
            Assert.False(DetectorRefinement.TryAccept(view, original, offered, out var retained, out var witness));
            Assert.Same(original, retained);
            Assert.True(Contains(witness, original));
            Assert.True(Contains(witness, offered));
        }
    }

    [Fact]
    public void NewEventOrDifferentObserverCannotRetroactivelyRefineTheOldDetection()
    {
        var fixture = new ThresholdFixture();
        var view = fixture.Forward;
        var source = new Placeholder();
        var eventIdentity = new Placeholder();
        var original = view.Observe(source, eventIdentity, view.SideDomain, view.TrendDomain);
        foreach (var offered in new[]
        {
            view.Observe(source, new Placeholder(), StructuralConstraint.Closed(view.Above), StructuralConstraint.Closed(view.Falling)),
            view.Observe(new Placeholder(), eventIdentity, StructuralConstraint.Closed(view.Above), StructuralConstraint.Closed(view.Falling)),
            fixture.Reverse.Observe(source, eventIdentity, StructuralConstraint.Closed(fixture.Reverse.Below), StructuralConstraint.Closed(fixture.Reverse.Rising))
        })
        {
            Assert.False(DetectorRefinement.TryAccept(view, original, offered, out var retained, out _));
            Assert.Same(original, retained);
        }
    }

    [Fact]
    public void UnknownDetectionCanReceiveNewCategoricalEvidenceButNeverForeignStates()
    {
        var view = new ThresholdFixture().Forward;
        var source = new Placeholder();
        var eventIdentity = new Placeholder();
        var original = view.Observe(source, eventIdentity, StructuralConstraint.Open(), StructuralConstraint.Open());
        var finer = view.Observe(source, eventIdentity,
            StructuralConstraint.Closed(view.Above), StructuralConstraint.Closed(view.Falling));
        Assert.True(DetectorRefinement.TryAccept(view, original, finer, out _, out _));
        var impostor = new Relation(view.Above.Left, view.Above.Right);
        var foreign = view.Observe(source, eventIdentity,
            StructuralConstraint.Closed(impostor), StructuralConstraint.Closed(view.Falling));
        Assert.False(DetectorRefinement.TryAccept(view, original, foreign, out var retained, out _));
        Assert.Same(original, retained);
        var invalidEarlier = view.Observe(source, eventIdentity,
            StructuralConstraint.Open(impostor), view.TrendDomain);
        Assert.False(DetectorRefinement.TryAccept(view, invalidEarlier, finer, out var invalidRetained, out _));
        Assert.Same(invalidEarlier, invalidRetained);
    }

    [Fact]
    public void RepeatedEvidenceDoesNotPreventAnOtherwiseValidSubsetRefinement()
    {
        var view = new ThresholdFixture().Forward;
        var source = new Placeholder();
        var eventIdentity = new Placeholder();
        var repeated = view.Observe(source, eventIdentity,
            StructuralConstraint.Closed(view.Above, view.Above), view.TrendDomain);
        var finer = view.Observe(source, eventIdentity,
            StructuralConstraint.Closed(view.Above), StructuralConstraint.Closed(view.Falling));
        Assert.True(DetectorRefinement.TryAccept(view, repeated, finer, out var accepted, out _));
        Assert.True(Contains(accepted, repeated));
    }

    [Fact]
    public void ComposedQuestionSelectsOnlyOneConditionalBranchAndLeavesDeepAlternativeUnexecuted()
    {
        var view = new ThresholdFixture().Forward;
        var observation = view.Observe(new Placeholder(), new Placeholder(),
            StructuralConstraint.Closed(view.Above), StructuralConstraint.Closed(view.Falling));
        var context = new Placeholder();
        var chosenBranch = new Relation(observation, new Placeholder());
        IElement deepAlternative = new Placeholder();
        for (var i = 0; i < 128; i++) deepAlternative = new Relation(deepAlternative, new Placeholder());
        var branchLaw = FiniteLaw.Create(view.Outcomes, StructuralConstraint.Closed(context),
            StructuralConstraint.Closed(chosenBranch, deepAlternative),
            FiniteLaw.Row(view.Approaching, context, StructuralConstraint.Closed(chosenBranch)),
            FiniteLaw.Row(view.Departing, context, StructuralConstraint.Closed(deepAlternative)));
        var upstream = view.ReadNative(observation);
        Assert.True(FiniteLaw.TryReadResult(upstream, out var outcomes, out _, out _));
        var composed = FiniteLaw.WithProvenance(
            FiniteLaw.Evaluate(branchLaw, outcomes, StructuralConstraint.Closed(context)), upstream);
        var answer = new Placeholder();
        var question = QualitativeQuestion.Ask(composed,
            StructuralConstraint.Closed(new Relation(chosenBranch, answer)), new Relation(deepAlternative, view.SideDomain));
        Assert.True(QualitativeQuestion.TryReadAnswer(question, out var selected));
        Assert.Same(answer, selected);
        QualitativeQuestion.Service(question, _ => throw new InvalidOperationException("Unselected branch was executed."));
        // Composition is observation only: the earlier source remains intact.
        Assert.True(Contains(question, observation));
        AssertNoNumbers(question);
    }

    private static Relation Routes(ThresholdView view, out Placeholder yes, out Placeholder no)
    {
        yes = new Placeholder(); no = new Placeholder();
        return StructuralConstraint.Closed(new Relation(view.Approaching, yes),
            new Relation(view.Departing, no), new Relation(view.OutsideSteady, no),
            new Relation(view.WithinSteady, no), new Relation(view.WithinChanging, no));
    }

    private static bool Contains(IElement root, IElement target) => Traverse(root).Any(e => ReferenceEquals(e, target));

    private static void AssertNoNumbers(IElement root)
    {
        foreach (var element in Traverse(root))
        {
            Assert.IsNotType<Carrier>(element);
            Assert.IsNotType<CarrierLocation>(element);
        }
    }

    private static IEnumerable<IElement> Traverse(IElement root)
    {
        var seen = new HashSet<IElement>(ReferenceEqualityComparer.Instance);
        var pending = new Stack<IElement>(); pending.Push(root);
        while (pending.TryPop(out var next))
        {
            if (!seen.Add(next)) continue;
            yield return next;
            if (next is not Relation relation) continue;
            pending.Push(relation.Left); pending.Push(relation.Right);
        }
    }
}
