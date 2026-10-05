using Gojo.Core;
using Gojo.Equations;
using Gojo.Structures;
using N = ConnectionAssembly.NativeMath;
using S = FailureSubscriptions.SubscriptionNetwork;

namespace FailureSubscriptions;

/// <summary>Contracts for local adverse evidence, paid shadow trials and
/// negotiated future windows. These fixtures assert no campaign winner.</summary>
internal static class AttentionChecks
{
    public static int Run()
    {
        PaidTrialDoesNotOwnTheAnswer();
        LocalReviewsAreBoundedAndContextual();
        MissingIsNotAReview();
        HarmDelaysShoppingWithoutPermanentExclusion();
        LegacyControlRemainsDistinct();
        ShortenedContractFreezesItsOwnTerms();
        NativeContinuationPreservesNewEvidence();
        return 7;
    }

    private static void PaidTrialDoesNotOwnTheAnswer()
    {
        var root = S.Create(adaptiveAttention: true);
        foreach (var input in new long[] { 100, 103, 100 }) Observe(ref root, input);
        var cold = Issue(root);
        Require(Number(cold, 2) == -3 && Number(cold, 3) == -3 && S.Parts(cold)[4] is Placeholder,
            "The incumbent wins a cold zero-evidence tie even when two paid trial predictions exist.");
        Require(Promises(cold).Count(p => Number(p, 3) == 1) == 2 && Total(S.Deliveries(root)) == 2 &&
            S.Services(root).Sum(s => Total(S.FeesReceived(s))) == 4 && Total(S.CalculationExpense(root)) == 2,
            "Declining to export a trial does not waive its delivery fee or calculation cost.");
        var frozenSelf = Number(cold, 2); var frozenExport = Number(cold, 3);
        var frame = Observe(ref root, 103);
        var outcome = (Relation)S.Parts(frame)[2];
        var inverseResponse = Responses(outcome).Single(r => ReferenceEquals(S.Parts((Relation)S.Parts(r)[0])[0], S.Routes(root)[1]));
        Require(Number(inverseResponse, 2) == 6 && Number(inverseResponse, 3) == 1 &&
            Number(cold, 2) == frozenSelf && Number(cold, 3) == frozenExport,
            "A later target can establish useful shadow evidence without revising the earlier actual answer.");
        Require(S.Parts(Issue(root))[4] is Relation selected && ReferenceEquals(selected, S.Routes(root)[1]),
            "Previously resolved useful help can become the next exported answer.");
        for (var i = 0; i < 16; i++)
        {
            Observe(ref root, i % 2 == 0 ? 100 : 103);
            Require(!ReferenceEquals(S.Parts(Issue(root))[4], S.Routes(root)[0]),
                "An exact duplicate cannot displace the incumbent by receiving payments or a zero-relief review.");
        }
        Accounted(root);
    }

    private static void LocalReviewsAreBoundedAndContextual()
    {
        var root = S.Create(adaptiveAttention: true);
        var inverse = S.Routes(root)[1];
        var reviews = S.LocalReviews(inverse);
        var sampleSites = reviews.SelectMany(S.ReviewSamples).ToArray();
        Require(reviews.Length == 2 && reviews.All(r => S.ReviewSamples(r).Length == 4),
            "Each connection exposes two local contexts with four reusable resolved-outcome sites apiece.");
        foreach (var input in new long[] { 100, 103, 100, 103, 100, 103, 100, 103, 100, 103, 100, 103, 100, 103, 100 })
            Observe(ref root, input);
        var negative = Review(root, inverse, -1); var positive = Review(root, inverse, 1);
        Require(Total(S.ReviewCount(negative)) == 4 && Total(S.ReviewCount(positive)) == 0 &&
            Equation.TryReadPrediction(S.ReadLocalMean(root, negative), out var mean, out _) && mean.Ticks == 6 &&
            !Equation.TryReadPrediction(S.ReadLocalMean(root, positive), out _, out _),
            "Repeated negative-triggered terms fill a bounded exact mean without inventing observations in the opposite context.");
        Require(sampleSites.Zip(S.LocalReviews(inverse).SelectMany(S.ReviewSamples)).All(p => ReferenceEquals(p.First, p.Second)),
            "New outcomes reuse native review sites instead of retaining an ever-growing observation list.");
        Require(reviews.All(r => ReferenceEquals(S.Parts(r)[1], S.Parts(S.Seeker(root))[1])) &&
            S.LocalReview(inverse, new CarrierLocation(new Carrier(), -1)) is null,
            "Reviews retain the buyer baseline and refuse a numerically similar but unaligned trigger.");
    }

    private static void MissingIsNotAReview()
    {
        var root = S.Create(adaptiveAttention: true);
        foreach (var input in new long[] { 100, 103, 100 }) Observe(ref root, input);
        var routes = S.Routes(root).Where(S.Attached).ToArray();
        var reviews = routes.Select(r => Review(root, r, -1)).ToArray();
        var retries = reviews.Select(r => S.RetryAfter(r).Ticks).ToArray();
        for (var i = 0; i < 5; i++)
        {
            var frame = Observe(ref root, null);
            Require(Number((Relation)S.Parts(frame)[2], 7) == 0 &&
                reviews.All(r => Total(S.ReviewCount(r)) == 0 && S.ReviewSamples(r).All(c => Total(c) == 0)) &&
                retries.SequenceEqual(reviews.Select(r => S.RetryAfter(r).Ticks)),
                "Missing opportunities advance contracts but supply neither resolved review samples nor a harmful-source retry penalty.");
        }
        Require(routes.All(r => !S.Attached(r)) && Total(S.CancellationExpense(root)) == 0 &&
            routes.All(r => ProspectiveOffer.Unknown(S.ProspectStates(S.RouteService(r))[0]).Value == 5),
            "The original contextual public coverage records five unknown outcomes while both fixed terms expire normally.");
        Accounted(root);
    }

    private static void HarmDelaysShoppingWithoutPermanentExclusion()
    {
        var root = S.Create(horizon: 1, neighborRotation: 1, adaptiveAttention: true, useProspects: false);
        // These unavailable competitors keep this narrow retry fixture from
        // being silenced by the perfectly suitable supplied extrapolator.
        for (var i = 0; i < 4; i++)
            if (i != 1) ((CarrierLocation)S.Parts(S.Services(root)[i])[4]).Tick.Set(1_000_000);
        foreach (var input in new long[] { 100, 102, 105, 109 }) Observe(ref root, input);
        var inverse = S.Routes(root)[1]; var review = Review(root, inverse, 1);
        var retryAt = S.RetryAfter(review).Ticks;
        Require(!S.Attached(inverse) && Total(S.ReviewCount(review)) == 1 && retryAt >= S.Version(root) + 8 &&
            Equation.TryReadPrediction(S.ReadLocalMean(root, review), out var mean, out _) && mean.Ticks < 0,
            "Resolved harmful service ends with a signed local review and an explicit future same-context retry boundary.");
        var lastGeneration = Total(S.Generation(inverse)); var retried = false; var testedCooldown = false;
        long value = 109;
        for (var i = 0; i < 64; i++)
        {
            value += 2 + i % 7;
            var frame = Observe(ref root, value);
            var outcome = (Relation)S.Parts(frame)[2];
            if (S.Version(root) < retryAt && Number(outcome, 7) == 1 && Number(outcome, 5) > 0)
            {
                testedCooldown = true;
                Require(!S.Parts((Relation)S.Parts(frame)[3]).OfType<Relation>()
                    .Any(row => ReferenceEquals(S.Parts(row)[0], S.RouteService(inverse))),
                    "Known harmful same-context help is skipped before charging another advertisement inspection.");
            }
            var generation = Total(S.Generation(inverse));
            if (generation != lastGeneration && S.Contract(root, inverse) is Relation contract && Number(contract, 3) > 0)
            {
                Require(S.Version(root) >= retryAt, "A fresh same-context subscription cannot cross the retained retry boundary early.");
                retried = S.SourcePresence(S.RouteService(inverse)).Ticks == 1;
                if (retried) break;
            }
            lastGeneration = generation;
        }
        Require(testedCooldown && retried && !ReferenceEquals(S.Parts(Issue(root))[4], inverse),
            "Continuing failure pressure eventually permits a paid retrial, while retained adverse evidence still keeps that trial out of the actual answer.");
        Accounted(root);
    }

    private static void LegacyControlRemainsDistinct()
    {
        var old = S.Create(); var guarded = S.Create(adaptiveAttention: true);
        foreach (var input in new long[] { 100, 103, 100 }) { Observe(ref old, input); Observe(ref guarded, input); }
        Require(S.Parts(Issue(old))[4] is Relation && S.Parts(Issue(guarded))[4] is Placeholder &&
            S.Contracts(old).OfType<Relation>().All(c => Number(c, 5) == 5) &&
            S.Contracts(guarded).OfType<Relation>().All(c => Number(c, 5) == 5),
            "Original fixed-duration attention remains a reproducible control; the guarded difference is export eligibility, not an accidental duration change.");
        var alone = S.Create(horizon: 0, adaptiveAttention: true, adaptiveHorizon: true, resourceBudget: 1_048_576);
        foreach (var input in new long[] { 100, 103, 100, 103, 100 }) Observe(ref alone, input);
        Require(Total(S.Deliveries(alone)) == 0 && Total(S.InspectionExpense(alone)) == 0 &&
            S.Routes(alone).All(r => !S.Attached(r)),
            "The larger declared resource pool and new attention modes cannot turn the no-help control into free source use.");
        Accounted(alone);
    }

    private static void ShortenedContractFreezesItsOwnTerms()
    {
        var root = S.Create(horizon: 2, adaptiveHorizon: true);
        var inverse = S.Routes(root)[1]; var service = S.RouteService(inverse);
        var state = S.DurationStates(service)[0];
        var clock = (CarrierLocation)S.Parts(S.Operational(root))[7];
        var trigger = new CarrierLocation(S.SourcePort(service).Carrier, -1);
        SeedFourSupportedOffsets(root, service, state, trigger, clock);
        var preview = S.QuoteDuration(root, service, trigger, new CarrierLocation(clock.Carrier, 0), 2);
        Require(DurationOffer.Backed(preview).Ticks == 4 && DurationOffer.Offered(preview).Ticks == 5 &&
            DurationOffer.Accepted(preview).Ticks == 2 && DurationOffer.TotalFee(preview).Ticks == 5 &&
            DurationOffer.OfferedFee(preview).Ticks == 11,
            "A buyer can purchase two of five offered opportunities for a lower total quoted tariff, with the fifth still identified as exploratory.");
        var tailBefore = DurationOffer.Rows(state).Skip(2).Select(RowCounts).ToArray();
        foreach (var input in new long[] { 100, 103, 100 }) Observe(ref root, input);
        var contract = S.Contract(root, inverse)!;
        var duration = (Relation)S.Parts(contract)[9];
        var snapshot = ProfileTicks(duration);
        Require(Number(contract, 2) - Number(contract, 1) == 2 && Number(contract, 5) == 2 &&
            Total(S.Remaining(inverse)) == 2 && DurationOffer.Offered(duration).Ticks == 5,
            "The native lease records the accepted interval rather than silently installing the provider's longer offer.");
        var firstOffset = DurationOffer.Rows(state)[0];
        var evaluatedBefore = DurationOffer.Evaluated(firstOffset).Value;
        var reliefBefore = Total(DurationOffer.Benefit(firstOffset));
        var fees = Total(S.FeesReceived(service));
        ((CarrierLocation)S.Parts(service)[5]).Tick.Set(7);
        Observe(ref root, 103);
        Require(DurationOffer.Evaluated(firstOffset).Value == evaluatedBefore + 1 &&
            Total(DurationOffer.Benefit(firstOffset)) == reliefBefore + 6,
            "The actual paid network outcome reaches the provider's first duration offset through a compatible relief basis, not only the helper's authored fixtures.");
        Require(Total(S.FeesReceived(service)) - fees == 2 && Number(contract, 6) == 2 &&
            Number(contract, 7) == 1 && DurationOffer.TotalFee(duration).Ticks == 5 &&
            ProfileTicks(duration).SequenceEqual(snapshot),
            "A changed advertisement price and a new helpful result cannot rewrite an already accepted tariff or its duration evidence.");
        var newOffer = S.QuoteDuration(root, service, trigger, new CarrierLocation(clock.Carrier, S.Version(root)), 2);
        Require(DurationOffer.TotalFee(newOffer).Ticks == 15,
            "A new quote sees the changed seven-credit price while the outstanding lease keeps its own terms.");
        var calculationBefore = Total(S.CalculationExpense(root)); var feesBeforeMissing = Total(S.FeesReceived(service));
        Observe(ref root, null);
        Require(!S.Attached(inverse) && S.Contract(root, inverse) is null && Total(S.Remaining(inverse)) == 0 &&
            S.Version(root) == Number(contract, 2) && Total(S.CalculationExpense(root)) == calculationBefore &&
            Total(S.FeesReceived(service)) == feesBeforeMissing && Total(S.CancellationExpense(root)) == 0,
            "The second shared opportunity expires the shortened contract even when missing, without free production or an early-cancellation fee.");
        Require(tailBefore.SequenceEqual(DurationOffer.Rows(state).Skip(2).Select(RowCounts)) &&
            DurationOffer.Unknown(DurationOffer.Rows(state)[1]).Value == 1,
            "Only the two purchased offsets receive new evidence; the missing second offset is unknown and the unpurchased tail remains untouched.");
        Accounted(root);
    }

    private static void SeedFourSupportedOffsets(Relation root, Relation service, Relation state,
        CarrierLocation trigger, CarrierLocation clock)
    {
        // Authored prior trials occur strictly before live network time. Each
        // length is purchased only after shorter evidence permits its offer.
        long start = -100;
        for (var length = 1; length <= 4; length++)
        for (var trial = 0; trial < 2; trial++)
        {
            var quote = S.QuoteDuration(root, service, trigger, new CarrierLocation(clock.Carrier, start), length);
            Require(DurationOffer.Accepted(quote).Ticks == length, "The duration fixture grows through actually offered frontier trials.");
            for (var offset = 1; offset <= length; offset++)
                DurationOffer.Observe(state, quote, new CarrierLocation(clock.Carrier, start + offset),
                    new CarrierLocation(trigger.Carrier, 2), N.Group(service, new CarrierLocation(clock.Carrier, start + offset)));
            start += 10;
        }
    }

    private static void NativeContinuationPreservesNewEvidence()
    {
        var root = S.Create(horizon: 8, adaptiveAttention: true, adaptiveHorizon: true, resourceBudget: 1_048_576);
        foreach (var input in new long?[] { 100, 103, 100, 103, 100, 103, 100, 103, 100, 103, null }) Observe(ref root, input);
        root = S.RetainContinuation(root);
        var clone = (Relation)GojoNumericFormat.Decode(GojoNumericFormat.Encode(root)).Single();
        Require(Checks.Canonical(root) == Checks.Canonical(clone),
            "Numeric transport preserves review samples, retries, duration coverage and unresolved pending promises.");
        foreach (var input in new long?[] { 106, 103, 106, 103, 106, 109, 112, 115, 115, null, 115, 112, 115, 112, 115 })
        {
            var beforeRoutes = S.Routes(root);
            var frame = Observe(ref root, input); Observe(ref clone, input);
            root = S.RetainContinuation(root); clone = S.RetainContinuation(clone);
            Require(Checks.Canonical(root) == Checks.Canonical(clone) &&
                beforeRoutes.Zip(S.Routes(root)).All(p => ReferenceEquals(p.First, p.Second)) &&
                !GojoEncodedSnapshot.CaptureGraph(root).Relations.Any(r => r.Id == frame.Id),
                "A reloaded adaptive world makes the same subsequent choices while finished frames are released and local route identity is reused.");
            Accounted(root);
        }
    }

    private static string RowCounts(Relation row) =>
        $"{DurationOffer.Helpful(row).Value}:{DurationOffer.Evaluated(row).Value}:{DurationOffer.Unknown(row).Value}:{Total(DurationOffer.Benefit(row))}";
    private static long[] ProfileTicks(Relation quote) => S.Parts(DurationOffer.Profile(quote)).Cast<Relation>()
        .SelectMany(row => S.Parts(row).Cast<CarrierLocation>()).Select(location => location.Ticks).ToArray();
    private static Relation Review(Relation root, Relation route, long sign) =>
        S.LocalReview(route, new CarrierLocation(S.SourcePort(S.Services(root)[0]).Carrier, sign))!;
    private static Relation Issue(Relation root) => (Relation)S.Parts(root)[1];
    private static Relation[] Promises(Relation issue) => S.Parts((Relation)S.Parts(issue)[5]).OfType<Relation>().ToArray();
    private static Relation[] Responses(Relation outcome) => S.Parts((Relation)S.Parts(outcome)[6]).OfType<Relation>().ToArray();
    private static long Number(Relation row, int index) => ((CarrierLocation)S.Parts(row)[index]).Ticks;
    private static long Total(Carrier carrier) => S.SignedValue(carrier).Ticks;
    private static Relation Observe(ref Relation root, long? input)
    {
        Require(S.TryObserve(root, input, out var next, out var frame), "The current native world accepts the supplied next opportunity.");
        root = next; return frame;
    }
    private static void Accounted(Relation root) => Require(S.Pass(S.Conservation(root)) &&
        Total(S.Spent(root)) == Total(S.InspectionExpense(root)) + Total(S.ActivationExpense(root)) +
        Total(S.CalculationExpense(root)) + Total(S.CancellationExpense(root)) + Total(S.SelfExpense(root)),
        "Adaptive choices conserve the declared supply and reconcile all five expenditure accounts.");
    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}
