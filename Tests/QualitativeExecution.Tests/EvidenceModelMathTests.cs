using System.Numerics;
using Gojo.Core;
using Gojo.Patterns;
using Stats = Gojo.Experiments.QualitativeExecution.StatisticalEvidence;

namespace QualitativeExecution.Tests;

/// <summary>Independent arithmetic oracles for the selected statistical
/// interpretation. The expected fractions are calculated here without calling
/// the production ratio operations or reading their intermediate expressions.</summary>
public sealed class EvidenceModelMathTests
{
    [Fact]
    public void Prior_odds_and_a_discriminating_likelihood_multiply_instead_of_averaging_checks()
    {
        var a = new Placeholder(); var b = new Placeholder(); var frame = new Stats.Frame(new Placeholder(), a, b);
        var state = frame.Prior(new Relation(a, frame.Weight(3, 5)), new Relation(b, frame.Weight(2, 5)));
        var question = new Stats.Question(new Placeholder(), new Placeholder(), new Placeholder());
        var offer = Stats.Offer(question.Observe(new Placeholder()),
            S(new Relation(a, frame.Weight(3, 4)), new Relation(b, frame.Weight(1, 4))), S(), S(), new Placeholder());
        var update = Stats.Update(state, offer);
        Assert.True(update.Accepted); Assert.False(update.Open); Assert.False(update.Contradicted);
        AssertProbability(Stats.Read(update.State), a, 9, 11);
        AssertProbability(Stats.Read(update.State), b, 2, 11);
        AssertProbability(Stats.Read(state), a, 3, 5);
    }

    [Theory]
    [InlineData(1, 2)]
    [InlineData(1, 1)]
    public void Equal_posteriors_do_not_hide_that_both_models_regard_the_observation_as_rare(
        int priorNumerator, int priorDenominator)
    {
        var a = new Placeholder(); var b = new Placeholder(); var frame = new Stats.Frame(new Placeholder(), a, b);
        var state = frame.Prior(new Relation(a, frame.Weight(priorNumerator, priorDenominator)),
            new Relation(b, frame.Weight(priorNumerator, priorDenominator)));
        var question = new Stats.Question(new Placeholder(), new Placeholder(), new Placeholder());
        var rare = Stats.Update(state, Stats.Offer(question.Observe(new Placeholder()),
            S(new Relation(a, frame.Weight(1, 100)), new Relation(b, frame.Weight(1, 100))), S(), S(), new Placeholder()));
        var certain = Stats.Update(state, Stats.Offer(question.Observe(new Placeholder()),
            S(new Relation(a, frame.Weight(1, 1)), new Relation(b, frame.Weight(1, 1))), S(), S(), new Placeholder()));
        Assert.True(rare.Accepted); Assert.True(certain.Accepted);
        var rareRead = Stats.Read(rare.State); var certainRead = Stats.Read(certain.State);
        AssertProbability(rareRead, a, 1, 2); AssertProbability(certainRead, a, 1, 2);
        var rareEvidence = rareRead.EvidenceProbability ?? throw new Xunit.Sdk.XunitException("Missing report probability.");
        var certainEvidence = certainRead.EvidenceProbability ?? throw new Xunit.Sdk.XunitException("Missing report probability.");
        Assert.Equal(rareEvidence.Denominator, 100 * rareEvidence.Numerator);
        Assert.Equal(certainEvidence.Denominator, certainEvidence.Numerator);
        // EvidenceExpression is an ordered prior-products -> joint-products
        // correspondence. It is not the selected-subset posterior grammar.
        Assert.True(rareRead.EvidenceExpression.Left is Relation && rareRead.EvidenceExpression.Right is Relation);
        var priorProducts = Entries((Relation)rareRead.EvidenceExpression.Left);
        var jointProducts = Entries((Relation)rareRead.EvidenceExpression.Right);
        Assert.Equal(2, priorProducts.Length); Assert.Equal(2, jointProducts.Length);
        for (var index = 0; index < priorProducts.Length; index++)
        {
            Assert.True(priorProducts[index] is Relation && jointProducts[index] is Relation);
            var priorTerms = Entries((Relation)priorProducts[index]);
            var jointTerms = Entries((Relation)jointProducts[index]);
            Assert.Single(priorTerms); Assert.Equal(2, jointTerms.Length);
            Assert.True(ReferenceEquals(priorTerms[0], jointTerms[0]));
        }
        Assert.False(Stats.TryReadExpression(rareRead.EvidenceExpression, frame.Unit, out _));
    }

    [Fact]
    public void Question_orientation_event_alias_and_unmeasured_partition_structure_survive_without_scalar_carriers()
    {
        var a = new Placeholder(); var b = new Placeholder(); var basis = new Placeholder();
        var frame = new Stats.Frame(basis, a, b); var claim = new Placeholder(); var contrary = new Placeholder();
        var context = new Placeholder(); var eventId = new Placeholder(); var question = new Stats.Question(claim, contrary, context);
        var observation = question.Observe(eventId, askOpposed: true, affirm: false);
        Assert.True(Stats.TryReadObservation(observation, out var report));
        Assert.True(ReferenceEquals(report.Event, eventId)); Assert.True(ReferenceEquals(report.Context, context));
        Assert.True(ReferenceEquals(report.Outcome, claim)); Assert.True(report.Positive);
        Assert.True(ReferenceEquals(report.Orientation.Left, contrary));
        Assert.True(ReferenceEquals(report.Response.Left, claim));
        var state = Equal(frame, a, b);
        var update = Stats.Update(state, Stats.Offer(observation,
            S(new Relation(a, frame.Weight(3, 4)), new Relation(b, frame.Weight(1, 4))), S(), S(), basis));
        Assert.True(update.Accepted);
        var native = Reachable(Stats.Read(update.State).Surface).ToArray();
        Assert.True(native.Any(item => ReferenceEquals(item, eventId)));
        Assert.True(native.Any(item => ReferenceEquals(item, claim)));
        Assert.True(native.Any(item => ReferenceEquals(item, contrary)));
        Assert.True(native.Any(item => ReferenceEquals(item, basis)));
        Assert.False(native.Any(item => item is Carrier or CarrierLocation));
    }

    [Theory]
    [InlineData(false, true, 3)]
    [InlineData(false, false, 1)]
    [InlineData(true, true, 1)]
    [InlineData(true, false, 3)]
    public void Oriented_question_and_response_select_the_same_underlying_binary_event(
        bool askOpposed, bool affirm, int expectedNumerator)
    {
        var a = new Placeholder(); var b = new Placeholder(); var frame = new Stats.Frame(new Placeholder(), a, b);
        var state = Equal(frame, a, b);
        var question = new Stats.Question(new Placeholder(), new Placeholder(), new Placeholder());
        var offer = Stats.Offer(question.Observe(new Placeholder(), askOpposed, affirm),
            S(new Relation(a, frame.Weight(3, 4)), new Relation(b, frame.Weight(1, 4))), S(), S(), new Placeholder());
        var update = Stats.Update(state, offer);
        Assert.True(update.Accepted);
        AssertProbability(Stats.Read(update.State), a, expectedNumerator, 4);
        AssertProbability(Stats.Read(update.State), b, 4 - expectedNumerator, 4);
    }

    [Fact]
    public void A_hundred_common_checks_preserve_prior_preference_and_one_continuation_can_dominate()
    {
        var a = new Placeholder(); var b = new Placeholder(); var frame = new Stats.Frame(new Placeholder(), a, b);
        var state = Equal(frame, a, b); var events = new List<IElement>();
        for (var i = 0; i < 100; i++)
        {
            var id = new Placeholder();
            var question = new Stats.Question(new Placeholder(), new Placeholder(), new Placeholder());
            var offer = Stats.Offer(question.Observe(id),
                S(new Relation(a, frame.Weight(1, 2)), new Relation(b, frame.Weight(1, 2))),
                S(), S(events.ToArray()), new Placeholder());
            var update = Stats.Update(state, offer);
            Assert.True(update.Accepted); state = update.State; events.Add(id);
        }
        AssertProbability(Stats.Read(state), a, 1, 2);
        var deformation = new Stats.Question(new Placeholder(), new Placeholder(), new Placeholder());
        var discriminating = Stats.Offer(deformation.Observe(new Placeholder()),
            S(new Relation(a, frame.Weight(1, 100)), new Relation(b, frame.Weight(99, 100))),
            S(), S(events.ToArray()), new Placeholder());
        var result = Stats.Update(state, discriminating);
        Assert.True(result.Accepted);
        AssertProbability(Stats.Read(result.State), a, 1, 100);
        AssertProbability(Stats.Read(result.State), b, 99, 100);
        AssertProbability(Stats.Read(state), a, 1, 2);
    }

    [Fact]
    public void A_second_route_to_the_same_event_is_not_a_second_independent_observation()
    {
        var a = new Placeholder(); var b = new Placeholder(); var frame = new Stats.Frame(new Placeholder(), a, b);
        var state = Equal(frame, a, b); var id = new Placeholder();
        var question = new Stats.Question(new Placeholder(), new Placeholder(), new Placeholder());
        var offer = Stats.Offer(question.Observe(id),
            S(new Relation(a, frame.Weight(3, 4)), new Relation(b, frame.Weight(1, 4))), S(), S(), new Placeholder());
        var first = Stats.Update(state, offer); Assert.True(first.Accepted);
        var secondRoute = Stats.Offer(question.Observe(id, askOpposed: true, affirm: false),
            S(new Relation(a, frame.Weight(75, 100)), new Relation(b, frame.Weight(25, 100))),
            S(), S(), new Placeholder());
        var repeated = Stats.Update(first.State, secondRoute);
        Assert.True(repeated.Duplicate); Assert.False(repeated.Accepted);
        AssertProbability(Stats.Read(repeated.State), a, 3, 4);
        var independent = Stats.Offer(question.Observe(new Placeholder()),
            S(new Relation(a, frame.Weight(3, 4)), new Relation(b, frame.Weight(1, 4))), S(), S(id), new Placeholder());
        var second = Stats.Update(first.State, independent); Assert.True(second.Accepted);
        AssertProbability(Stats.Read(second.State), a, 9, 10);
    }

    [Fact]
    public void A_double_dual_event_alias_is_one_native_observation_despite_a_different_host_reference()
    {
        var a = new Placeholder(); var b = new Placeholder(); var frame = new Stats.Frame(new Placeholder(), a, b);
        var state = Equal(frame, a, b); var eventId = new Placeholder(); var alias = eventId.Dual().Dual();
        Assert.False(ReferenceEquals(eventId, alias)); Assert.True(eventId.Equals(alias));
        var question = new Stats.Question(new Placeholder(), new Placeholder(), new Placeholder());
        var rows = S(new Relation(a, frame.Weight(3, 4)), new Relation(b, frame.Weight(1, 4)));
        var first = Stats.Update(state, Stats.Offer(question.Observe(eventId), rows, S(), S(), new Placeholder()));
        Assert.True(first.Accepted);
        var replay = Stats.Update(first.State, Stats.Offer(question.Observe(alias), rows,
            S(), S(eventId), new Placeholder()));
        Assert.True(replay.Duplicate); Assert.False(replay.Accepted); Assert.False(replay.Contradicted);
        var read = Stats.Read(replay.State);
        Assert.Single(read.Events); AssertProbability(read, a, 3, 4);
    }

    [Fact]
    public void Alternative_aliases_cannot_manufacture_two_choices_but_opposed_faces_remain_distinct()
    {
        var model = new Placeholder(); var alias = model.Dual().Dual(); var basis = new Placeholder();
        Assert.Throws<ArgumentException>(() => new Stats.Frame(basis, model, alias));
        var opposed = model.Dual(); var frame = new Stats.Frame(basis, model, opposed);
        var read = Stats.Read(frame.Prior(new Relation(model, frame.Weight(1, 2)),
            new Relation(opposed, frame.Weight(1, 2))));
        Assert.Equal(2, read.Rows.Count);
        AssertProbability(read, model, 1, 2); AssertProbability(read, opposed, 1, 2);
    }

    [Fact]
    public void A_binary_question_preserves_native_answer_aliases_without_collapsing_its_contrary()
    {
        var claim = new Placeholder(); var contrary = new Placeholder(); var context = new Placeholder();
        Assert.Throws<ArgumentException>(() => new Stats.Question(claim, claim.Dual().Dual(), context));
        var question = new Stats.Question(claim, contrary, context); var original = question.Observe(new Placeholder());
        var orderAliases = new Relation(claim.Dual().Dual(), contrary.Dual().Dual());
        var aliased = new Relation(original.Left, new Relation(orderAliases, orderAliases));
        Assert.True(Stats.TryReadObservation(aliased, out var read));
        Assert.True(read.Positive);
        Assert.True(((Placeholder)read.Outcome).Equals(claim));
        var opposed = new Stats.Question(claim, claim.Dual(), context);
        Assert.True(Stats.TryReadObservation(opposed.Observe(new Placeholder(), askOpposed: true), out var negative));
        Assert.False(negative.Positive);
    }

    [Fact]
    public void Partition_unit_matching_uses_native_identity_and_preserves_the_opposed_face()
    {
        var frame = new Stats.Frame(new Placeholder(), new Placeholder(), new Placeholder());
        var unit = (Placeholder)frame.Unit; var partition = frame.Weight(1, 2);
        Assert.True(Stats.TryReadPartition(partition, unit.Dual().Dual(), out var half));
        Assert.Equal(half.Denominator, 2 * half.Numerator);
        Assert.False(Stats.TryReadPartition(partition, unit.Dual(), out _));
    }

    [Fact]
    public void Distinct_event_identity_without_a_dependence_contract_remains_open()
    {
        var a = new Placeholder(); var b = new Placeholder(); var frame = new Stats.Frame(new Placeholder(), a, b);
        var state = Equal(frame, a, b); var question = new Stats.Question(new Placeholder(), new Placeholder(), new Placeholder());
        Relation Offer(IElement id, Relation independent) => Stats.Offer(question.Observe(id),
            S(new Relation(a, frame.Weight(3, 4)), new Relation(b, frame.Weight(1, 4))), S(), independent, new Placeholder());
        var first = Stats.Update(state, Offer(new Placeholder(), S())); Assert.True(first.Accepted);
        var second = Stats.Update(first.State, Offer(new Placeholder(), S()));
        Assert.True(second.Open); Assert.False(second.Accepted);
        AssertProbability(Stats.Read(second.State), a, 3, 4);
    }

    [Fact]
    public void A_conditional_unit_factor_preserves_the_first_result_instead_of_double_counting_it()
    {
        var a = new Placeholder(); var b = new Placeholder(); var frame = new Stats.Frame(new Placeholder(), a, b);
        var state = Equal(frame, a, b); var id = new Placeholder();
        var question = new Stats.Question(new Placeholder(), new Placeholder(), new Placeholder());
        var first = Stats.Update(state, Stats.Offer(question.Observe(id),
            S(new Relation(a, frame.Weight(3, 4)), new Relation(b, frame.Weight(1, 4))), S(), S(), new Placeholder()));
        Assert.True(first.Accepted);
        var conditional = Stats.Offer(question.Observe(new Placeholder()),
            S(new Relation(a, frame.Weight(1, 1)), new Relation(b, frame.Weight(1, 1))), S(id), S(), new Placeholder());
        var second = Stats.Update(first.State, conditional);
        Assert.True(second.Accepted);
        AssertProbability(Stats.Read(second.State), a, 3, 4);
    }

    [Fact]
    public void An_unknown_likelihood_is_not_zero_one_half_or_a_neutral_factor()
    {
        var a = new Placeholder(); var b = new Placeholder(); var frame = new Stats.Frame(new Placeholder(), a, b);
        var state = Equal(frame, a, b); var question = new Stats.Question(new Placeholder(), new Placeholder(), new Placeholder());
        var offer = Stats.Offer(question.Observe(new Placeholder()),
            S(new Relation(a, new Placeholder()), new Relation(b, frame.Weight(1, 1))), S(), S(), new Placeholder());
        var update = Stats.Update(state, offer);
        Assert.True(update.Open); Assert.False(update.Accepted); Assert.False(update.Contradicted);
        AssertProbability(Stats.Read(update.State), a, 1, 2);
        Assert.True(Reachable(update.Surface).Any(item => ReferenceEquals(item, offer)));
    }

    [Fact]
    public void Impossible_evidence_for_all_candidates_preserves_the_previous_state_and_exposes_the_failed_proposal()
    {
        var a = new Placeholder(); var b = new Placeholder(); var frame = new Stats.Frame(new Placeholder(), a, b);
        var state = frame.Prior(new Relation(a, frame.Weight(3, 5)), new Relation(b, frame.Weight(2, 5)));
        var question = new Stats.Question(new Placeholder(), new Placeholder(), new Placeholder());
        var offer = Stats.Offer(question.Observe(new Placeholder()),
            S(new Relation(a, frame.Weight(0, 1)), new Relation(b, frame.Weight(0, 1))), S(), S(), new Placeholder());
        var update = Stats.Update(state, offer);
        Assert.True(update.Contradicted); Assert.False(update.Accepted);
        AssertProbability(Stats.Read(update.State), a, 3, 5);
        AssertProbability(Stats.Read(update.State), b, 2, 5);
        Assert.True(Reachable(update.Surface).Any(item => ReferenceEquals(item, offer)));
    }

    [Fact]
    public void A_zero_prior_does_not_revive_under_finite_likelihood_without_explicit_prior_revision()
    {
        var a = new Placeholder(); var b = new Placeholder(); var frame = new Stats.Frame(new Placeholder(), a, b);
        var state = frame.Prior(new Relation(a, frame.Weight(0, 1)), new Relation(b, frame.Weight(1, 1)));
        var question = new Stats.Question(new Placeholder(), new Placeholder(), new Placeholder());
        var update = Stats.Update(state, Stats.Offer(question.Observe(new Placeholder()),
            S(new Relation(a, frame.Weight(1, 1)), new Relation(b, frame.Weight(1, 100))), S(), S(), new Placeholder()));
        Assert.True(update.Accepted);
        AssertProbability(Stats.Read(update.State), a, 0, 1);
        AssertProbability(Stats.Read(update.State), b, 1, 1);
        var reason = new Relation(new Placeholder(), new Placeholder());
        var revised = Stats.RevisePrior(update.State,
            S(new Relation(a, frame.Weight(1, 2)), new Relation(b, frame.Weight(1, 2))), reason);
        Assert.True(revised.Accepted);
        AssertProbability(Stats.Read(revised.State), a, 100, 101);
        Assert.True(Reachable(revised.Surface).Any(item => ReferenceEquals(item, update.State)));
        Assert.True(Reachable(revised.Surface).Any(item => ReferenceEquals(item, reason)));
        AssertProbability(Stats.Read(update.State), a, 0, 1);
    }

    [Fact]
    public void Native_selected_sum_of_products_reconstructs_the_same_probability_as_the_diagnostic_read()
    {
        var a = new Placeholder(); var b = new Placeholder(); var frame = new Stats.Frame(new Placeholder(), a, b);
        var state = frame.Prior(new Relation(a, frame.Weight(3, 5)), new Relation(b, frame.Weight(2, 5)));
        var question = new Stats.Question(new Placeholder(), new Placeholder(), new Placeholder());
        var update = Stats.Update(state, Stats.Offer(question.Observe(new Placeholder()),
            S(new Relation(a, frame.Weight(3, 4)), new Relation(b, frame.Weight(1, 4))), S(), S(), new Placeholder()));
        Assert.True(update.Accepted);
        var read = Stats.Read(update.State);
        Assert.True(read.Surface.Right is Relation);
        var queries = (Relation)read.Surface.Right;
        Assert.True(StructuralConstraint.TryReadScope(queries, out var entries, out _, out _));
        var selected = Assert.Single(entries.OfType<Relation>().Where(row => ReferenceEquals(row.Left, a)));
        Assert.True(selected.Right is Relation);
        Assert.True(Stats.TryReadExpression((Relation)selected.Right, frame.Unit, out var fromStructure));
        Assert.Equal(9 * fromStructure.Denominator, 11 * fromStructure.Numerator);
        Assert.True(Reachable(selected).Any(item => ReferenceEquals(item, question.Surface)));
    }

    [Fact]
    public void An_exact_prior_reader_does_not_drop_an_extra_unaccounted_native_participant()
    {
        var a = new Placeholder(); var b = new Placeholder(); var frame = new Stats.Frame(new Placeholder(), a, b);
        var unexplained = new Placeholder();
        var malformed = new Relation(new Relation(frame.Surface,
            S(new Relation(a, frame.Weight(1, 2)), new Relation(b, frame.Weight(1, 2)), unexplained)), S());
        var read = Stats.Read(malformed);
        Assert.False(read.Complete); Assert.False(read.Inconsistent);
        Assert.True(Reachable(read.Surface).Any(item => ReferenceEquals(item, unexplained)));
    }

    [Fact]
    public void Native_probability_selection_cannot_duplicate_a_product_beyond_its_whole()
    {
        var a = new Placeholder(); var b = new Placeholder(); var frame = new Stats.Frame(new Placeholder(), a, b);
        var state = Equal(frame, a, b); var expression = ExpressionFor(Stats.Read(state), a);
        Assert.True(expression.Left is Relation && expression.Right is Relation);
        var selected = Entries((Relation)expression.Left);
        var product = Assert.Single(selected);
        var inventedDuplicate = new Relation(S(product, product), expression.Right);
        Assert.False(Stats.TryReadExpression(inventedDuplicate, frame.Unit, out _));
        var emptySelection = new Relation(S(), expression.Right);
        Assert.True(Stats.TryReadExpression(emptySelection, frame.Unit, out var zero));
        Assert.Equal(BigInteger.Zero, zero.Numerator); Assert.True(zero.Denominator > 0);
        Assert.True(Stats.TryReadExpression(expression, frame.Unit, out var half));
        Assert.Equal(half.Denominator, 2 * half.Numerator);
    }

    [Fact]
    public void Coherent_conditional_factorizations_agree_in_both_observation_orders()
    {
        // Model A: P(E)=3/4, P(F)=3/4, P(E,F)=1/2.
        // Model B: P(E)=1/2, P(F)=1/4, P(E,F)=1/8.
        // The first-step posteriors differ, while the same joint observation
        // must end at model odds 4:1 in either factorization. For model A,
        // neither conditional likelihood equals its unconditional marginal.
        var a = new Placeholder(); var b = new Placeholder(); var frame = new Stats.Frame(new Placeholder(), a, b);
        var prior = Equal(frame, a, b); var e = new Placeholder(); var f = new Placeholder();
        var eq = new Stats.Question(new Placeholder(), new Placeholder(), new Placeholder());
        var fq = new Stats.Question(new Placeholder(), new Placeholder(), new Placeholder());
        var eFirst = Stats.Update(prior, Stats.Offer(eq.Observe(e),
            S(new Relation(a, frame.Weight(3, 4)), new Relation(b, frame.Weight(1, 2))), S(), S(), new Placeholder()));
        var fFirst = Stats.Update(prior, Stats.Offer(fq.Observe(f),
            S(new Relation(a, frame.Weight(3, 4)), new Relation(b, frame.Weight(1, 4))), S(), S(), new Placeholder()));
        Assert.True(eFirst.Accepted); Assert.True(fFirst.Accepted);
        AssertProbability(Stats.Read(eFirst.State), a, 3, 5);
        AssertProbability(Stats.Read(fFirst.State), a, 3, 4);
        var ef = Stats.Update(eFirst.State, Stats.Offer(fq.Observe(f),
            S(new Relation(a, frame.Weight(2, 3)), new Relation(b, frame.Weight(1, 4))), S(e), S(), new Placeholder()));
        var fe = Stats.Update(fFirst.State, Stats.Offer(eq.Observe(e),
            S(new Relation(a, frame.Weight(2, 3)), new Relation(b, frame.Weight(1, 2))), S(f), S(), new Placeholder()));
        Assert.True(ef.Accepted); Assert.True(fe.Accepted);
        AssertProbability(Stats.Read(ef.State), a, 4, 5);
        AssertProbability(Stats.Read(fe.State), a, 4, 5);
    }

    [Fact]
    public void Very_small_nonzero_support_can_recover_while_exact_zero_remains_absorbing()
    {
        var a = new Placeholder(); var b = new Placeholder(); var frame = new Stats.Frame(new Placeholder(), a, b);
        var state = Equal(frame, a, b); var weakEvent = new Placeholder(); var zeroEvent = new Placeholder();
        var question = new Stats.Question(new Placeholder(), new Placeholder(), new Placeholder());
        var weak = Stats.Update(state, Stats.Offer(question.Observe(weakEvent),
            S(new Relation(a, frame.Weight(1, 100)), new Relation(b, frame.Weight(99, 100))), S(), S(), new Placeholder()));
        var excluded = Stats.Update(state, Stats.Offer(question.Observe(zeroEvent),
            S(new Relation(a, frame.Weight(0, 1)), new Relation(b, frame.Weight(1, 1))), S(), S(), new Placeholder()));
        Assert.True(weak.Accepted); Assert.True(excluded.Accepted);
        var recover = Stats.Update(weak.State, Stats.Offer(question.Observe(new Placeholder()),
            S(new Relation(a, frame.Weight(99, 100)), new Relation(b, frame.Weight(1, 100))),
            S(), S(weakEvent), new Placeholder()));
        var staysExcluded = Stats.Update(excluded.State, Stats.Offer(question.Observe(new Placeholder()),
            S(new Relation(a, frame.Weight(99, 100)), new Relation(b, frame.Weight(1, 100))),
            S(), S(zeroEvent), new Placeholder()));
        Assert.True(recover.Accepted); Assert.True(staysExcluded.Accepted);
        AssertProbability(Stats.Read(recover.State), a, 1, 2);
        AssertProbability(Stats.Read(staysExcluded.State), a, 0, 1);
        AssertProbability(Stats.Read(weak.State), a, 1, 100);
    }

    [Fact]
    public void A_conflicting_report_from_the_same_event_is_blocked_and_both_reports_remain_visible()
    {
        var a = new Placeholder(); var b = new Placeholder(); var frame = new Stats.Frame(new Placeholder(), a, b);
        var state = Equal(frame, a, b); var id = new Placeholder();
        var question = new Stats.Question(new Placeholder(), new Placeholder(), new Placeholder());
        var probabilities = S(new Relation(a, frame.Weight(3, 4)), new Relation(b, frame.Weight(1, 4)));
        var firstOffer = Stats.Offer(question.Observe(id), probabilities, S(), S(), new Placeholder());
        var first = Stats.Update(state, firstOffer); Assert.True(first.Accepted);
        var oppositeOffer = Stats.Offer(question.Observe(id, affirm: false), probabilities, S(), S(), new Placeholder());
        var opposite = Stats.Update(first.State, oppositeOffer);
        Assert.True(opposite.Contradicted); Assert.False(opposite.Accepted); Assert.False(opposite.Duplicate);
        AssertProbability(Stats.Read(opposite.State), a, 3, 4);
        var retained = Reachable(opposite.Surface).ToHashSet(ReferenceEqualityComparer.Instance);
        Assert.True(retained.Contains(firstOffer)); Assert.True(retained.Contains(oppositeOffer));
        Assert.True(retained.Contains(first.State));
    }

    [Fact]
    public void A_missing_partition_or_foreign_count_unit_remains_unread_instead_of_being_recalibrated()
    {
        var a = new Placeholder(); var b = new Placeholder(); var frame = new Stats.Frame(new Placeholder(), a, b);
        var partition = frame.Weight(1, 2);
        Assert.False(Stats.TryReadPartition(partition, new Placeholder(), out _));
        var noWhole = new Relation(new Relation(frame.Unit, new Placeholder()), new Relation(S(), S()));
        Assert.False(Stats.TryReadPartition(noWhole, frame.Unit, out _));
        Assert.True(Stats.TryReadPartition(frame.Weight(0, 1), frame.Unit, out var validZero));
        Assert.Equal(BigInteger.Zero, validZero.Numerator);
        var foreign = new Relation(new Relation(new Placeholder(), new Placeholder()), partition.Right);
        var state = Equal(frame, a, b); var question = new Stats.Question(new Placeholder(), new Placeholder(), new Placeholder());
        var offer = Stats.Offer(question.Observe(new Placeholder()),
            S(new Relation(a, foreign), new Relation(b, frame.Weight(1, 1))), S(), S(), new Placeholder());
        var update = Stats.Update(state, offer);
        Assert.True(update.Open); Assert.False(update.Accepted);
        AssertProbability(Stats.Read(update.State), a, 1, 2);
        Assert.True(Reachable(update.Surface).Any(item => ReferenceEquals(item, foreign)));
        Assert.False(Stats.TryReadExpression(ExpressionFor(Stats.Read(state), a), new Placeholder(), out _));
    }

    [Fact]
    public void Changing_only_fraction_presentation_does_not_change_prior_odds()
    {
        var a = new Placeholder(); var b = new Placeholder(); var frame = new Stats.Frame(new Placeholder(), a, b);
        var coarse = frame.Prior(new Relation(a, frame.Weight(1, 2)), new Relation(b, frame.Weight(1, 2)));
        var expanded = frame.Prior(new Relation(a, frame.Weight(50, 100)), new Relation(b, frame.Weight(50, 100)));
        AssertProbability(Stats.Read(coarse), a, 1, 2);
        AssertProbability(Stats.Read(expanded), a, 1, 2);
        Assert.False(ReferenceEquals(coarse, expanded));
    }

    [Fact]
    public void Published_coarse_zero_is_a_bracket_with_nonzero_upper_support_rather_than_impossibility()
    {
        var a = new Placeholder(); var b = new Placeholder(); var frame = new Stats.Frame(new Placeholder(), a, b);
        var state = frame.Prior(new Relation(a, frame.Weight(1, 3)), new Relation(b, frame.Weight(2, 3)));
        var read = Stats.Quantize(state, a, 2);
        Assert.True(read.Complete);
        Assert.Equal(BigInteger.Zero, read.Lower.Numerator);
        Assert.Equal(read.Upper.Denominator, 2 * read.Upper.Numerator);
        AssertProbability(Stats.Read(state), a, 1, 3);
        Assert.True(Reachable(read.Surface).Any(item => ReferenceEquals(item, state)));
    }

    [Fact]
    public void Small_finite_partitions_match_an_independent_exact_integer_oracle()
    {
        // All denominators and sample partitions are explicitly selected by
        // the fixture. Nothing infers likelihood from a recipe's label.
        for (var priorA = 1; priorA <= 3; priorA++)
        for (var priorB = 1; priorB <= 3; priorB++)
        for (var likelihoodA = 0; likelihoodA <= 3; likelihoodA++)
        for (var likelihoodB = 0; likelihoodB <= 3; likelihoodB++)
        {
            if (likelihoodA + likelihoodB == 0) continue;
            var a = new Placeholder(); var b = new Placeholder(); var frame = new Stats.Frame(new Placeholder(), a, b);
            var state = frame.Prior(new Relation(a, frame.Weight(priorA, priorA + priorB)),
                new Relation(b, frame.Weight(priorB, priorA + priorB)));
            var question = new Stats.Question(new Placeholder(), new Placeholder(), new Placeholder());
            var update = Stats.Update(state, Stats.Offer(question.Observe(new Placeholder()),
                S(new Relation(a, frame.Weight(likelihoodA, 3)), new Relation(b, frame.Weight(likelihoodB, 3))),
                S(), S(), new Placeholder()));
            Assert.True(update.Accepted);
            var expectedA = (BigInteger)priorA * likelihoodA;
            var expectedB = (BigInteger)priorB * likelihoodB;
            AssertProbability(Stats.Read(update.State), a, expectedA, expectedA + expectedB);
            AssertProbability(Stats.Read(update.State), b, expectedB, expectedA + expectedB);
        }
    }

    private static Relation Equal(Stats.Frame frame, IElement a, IElement b) => frame.Prior(
        new Relation(a, frame.Weight(1, 2)), new Relation(b, frame.Weight(1, 2)));

    private static void AssertProbability(Stats.Distribution distribution, IElement model,
        BigInteger expectedNumerator, BigInteger expectedDenominator)
    {
        Assert.True(distribution.Complete); Assert.False(distribution.Inconsistent);
        var row = Assert.Single(distribution.Rows.Where(item => ReferenceEquals(item.Model, model)));
        var actual = row.Probability ?? throw new Xunit.Sdk.XunitException("The complete distribution has no probability for its model.");
        Assert.Equal(expectedNumerator * actual.Denominator, actual.Numerator * expectedDenominator);
        Assert.True(actual.Denominator > 0);
    }

    private static Relation S(params IElement[] elements) => StructuralConstraint.Closed(elements);

    private static IElement[] Entries(Relation scope)
    {
        Assert.True(StructuralConstraint.TryReadScope(scope, out var entries, out _, out _));
        return entries;
    }

    private static Relation ExpressionFor(Stats.Distribution distribution, IElement model)
    {
        Assert.True(distribution.Complete); Assert.True(distribution.Surface.Right is Relation);
        var query = Assert.Single(Entries((Relation)distribution.Surface.Right).OfType<Relation>()
            .Where(row => ReferenceEquals(row.Left, model)));
        Assert.True(query.Right is Relation);
        return (Relation)query.Right;
    }

    private static IEnumerable<IElement> Reachable(IElement root)
    {
        var seen = new HashSet<IElement>(ReferenceEqualityComparer.Instance); var pending = new Stack<IElement>();
        pending.Push(root);
        while (pending.TryPop(out var item))
        {
            if (!seen.Add(item)) continue;
            yield return item;
            if (item is Relation relation) { pending.Push(relation.Left); pending.Push(relation.Right); }
        }
    }
}
