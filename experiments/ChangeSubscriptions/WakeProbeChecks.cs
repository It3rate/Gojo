using System.Text.Json;
using Gojo.Core;
using C = ChangeSubscriptions.ChangeSurface;
using R = ChangeSubscriptions.ComparativeRetry;
using W = ChangeSubscriptions.WakeProbe;
using N = ConnectionAssembly.NativeMath;

namespace ChangeSubscriptions;

internal static class WakeProbeChecks
{
    public static int Run()
    {
        InitialSynchronizationAndPhysicalWithdrawal();
        ConfirmationNeedsNewResolvedOpportunity();
        OneBoundedGrantPerEpoch();
        InvalidAndReturningReadsCancel();
        LaterEpochCanExploreWithoutErasingEvidence();
        NativeTransportRetainsCandidateAndTokens();
        return 6;
    }

    private static void InitialSynchronizationAndPhysicalWithdrawal()
    {
        var fixture = Create(admit: false); var wake = Wake(fixture); var retry = W.Retry(wake);
        Require(!C.Attached(W.Watcher(wake)) && W.Tokens(wake).Value == 0,
            "A potential wake watcher starts physically detached and has no allowance.");
        var ratings = Ratings(retry);
        W.Advance(wake, Clock(fixture));
        Require(C.Attached(W.Watcher(wake)) && C.TryReceived(W.Watcher(wake), out var current, out _) &&
            current.Ticks == 0 && W.AnchorValue(wake).Ticks == 0 && W.AnchorValidity(wake).Ticks == 1 &&
            W.InitialSyncs(wake).Value == 1 && W.Notifications(wake).Value == 0 && W.Grants(wake).Value == 0,
            "Withdrawal attaches a real publication and freezes its current coarse cell; initial synchronization is not a change grant.");
        var physical = C.Publication(W.Watcher(wake));
        Step(fixture, 19, 19, 3);
        Require(W.RevisionPolls(wake).Value == 1 && W.Notifications(wake).Value == 0 &&
            W.ConfirmationReads(wake).Value == 0 && W.Tokens(wake).Value == 0,
            "An unchanged coarse cell uses one accounted revision poll without selected-value confirmation or new credit.");
        Step(fixture, 26, 26, 3);
        Require(!C.Attached(W.Watcher(wake)) && !C.TryReceived(W.Watcher(wake), out _, out _) &&
            ReferenceEquals(physical, C.Publication(W.Watcher(wake))) && !W.TryPermit(wake, Clock(fixture), out _) &&
            ratings.SequenceEqual(Ratings(retry)),
            "Ordinary retry readiness detaches the cheap watcher without changing its relation identity or prior paid evidence.");
    }

    private static void ConfirmationNeedsNewResolvedOpportunity()
    {
        var fixture = Create(); var wake = Wake(fixture); var ratings = Ratings(W.Retry(wake));
        Step(fixture, 19, 19, 6);
        Require(W.CandidateCount(wake).Value == 1 && W.CandidateValue(wake).Ticks == 1 &&
            W.CandidateArrival(wake).Ticks == 19 && W.CandidateOpportunity(wake).Ticks == 19 &&
            W.Tokens(wake).Value == 0 && !W.TryPermit(wake, Clock(fixture), out _),
            "A first changed coarse cell arms a captured candidate, not an immediate paid allowance.");
        var before = Canonical(fixture);
        for (var i = 0; i < 5; i++)
        {
            W.Advance(wake, Clock(fixture)); W.TryPermit(wake, Clock(fixture), out _);
            C.Notify(W.Watcher(wake), includeCurrent: false);
        }
        Require(Canonical(fixture) == before,
            "Repeated arrival, notification and permit reads cannot manufacture distinct confirmation opportunities.");
        Step(fixture, 20, 20, 6);
        Require(W.Grants(wake).Value == 1 && W.Tokens(wake).Value == 2 && W.ConfirmationReads(wake).Value == 1 &&
            W.TryPermit(wake, Clock(fixture), out _) && ratings.SequenceEqual(Ratings(W.Retry(wake))),
            "The next resolved validation confirms the changed cell while all harmful paid evidence and its deadline remain intact.");
        var granted = Canonical(fixture);
        for (var i = 0; i < 4; i++) W.TryPermit(wake, Clock(fixture), out _);
        Require(Canonical(fixture) == granted, "Reading a live permit is pure, including when it succeeds.");

        var held = Create(); var heldWake = Wake(held);
        Step(held, 19, 19, 6); Step(held, 20, 19, 6); Step(held, 21, 20, 6);
        Require(W.Grants(heldWake).Value == 0 && W.Tokens(heldWake).Value == 0 && W.CandidateCount(heldWake).Value == 0,
            "A held validation counter cannot confirm, and a later unchanged cell cannot invent the missed follow-up.");
        var skipped = Create(); Step(skipped, 19, 19, 6); Step(skipped, 21, 20, 6);
        Require(W.Grants(Wake(skipped)).Value == 0,
            "A skipped arrival does not satisfy the explicitly adjacent confirmation policy.");
        var unaligned = Canonical(skipped);
        W.Advance(Wake(skipped), L(new Carrier(), 22));
        Require(Canonical(skipped) == unaligned,
            "A foreign arrival axis does not mutate the admitted local wake state.");
    }

    private static void OneBoundedGrantPerEpoch()
    {
        var fixture = Create(); var wake = Wake(fixture); Grant(fixture);
        var promise = N.Group(L(C.Current(W.Surface(wake)).Carrier, 1), L(Money(fixture), 1));
        var first = W.Consume(wake, Clock(fixture), promise);
        Require(Number(first, 3) == 1 && ReferenceEquals(W.Parts(first)[2], promise) &&
            W.Tokens(wake).Value == 1 && W.ProbesIssued(wake).Value == 1,
            "An actual paid issue consumes one token and retains its exact promise as provenance.");
        W.Consume(wake, Clock(fixture), promise);
        Require(W.Tokens(wake).Value == 1 && W.ProbesIssued(wake).Value == 1 &&
            !W.TryPermit(wake, Clock(fixture), out _), "One arrival cannot consume the second forecast allowance.");
        Step(fixture, 21, 21, 6); W.Consume(wake, Clock(fixture), promise);
        Require(W.Tokens(wake).Value == 0 && W.ProbesIssued(wake).Value == 2 && W.GrantUsed(wake).Value == 1,
            "Two distinct paid issues exhaust the finite grant.");
        Step(fixture, 22, 22, 10); Step(fixture, 23, 23, 10); Step(fixture, 24, 24, 14);
        Require(W.Grants(wake).Value == 1 && W.ProbesIssued(wake).Value == 2 && W.Tokens(wake).Value == 0 &&
            !W.TryPermit(wake, Clock(fixture), out _),
            "Further changes inside the same retry epoch cannot refresh its already used grant.");
        var single = Create(maximum: 1); Grant(single);
        Require(W.MaximumProbe(Wake(single)).Ticks == 1 && W.Tokens(Wake(single)).Value == 1,
            "The connected policy may narrow the grant below its hard maximum of two.");
    }

    private static void InvalidAndReturningReadsCancel()
    {
        var missing = Create(); Grant(missing); var wake = Wake(missing);
        Step(missing, 21, 20, 6, valid: false);
        Require(W.Tokens(wake).Value == 0 && W.CandidateCount(wake).Value == 0 && W.GrantUsed(wake).Value == 1 &&
            !W.TryPermit(wake, Clock(missing), out _),
            "Unknown current input revokes the unused allowance while preserving the epoch's already used grant.");
        Step(missing, 22, 21, 6); Step(missing, 23, 22, 6);
        Require(W.Grants(wake).Value == 1 && W.Tokens(wake).Value == 0,
            "Restoring validity does not refund canceled probes or create a second grant.");

        var returning = Create(); Grant(returning); var returnedWake = Wake(returning);
        Step(returning, 21, 21, 3);
        Require(W.Tokens(returnedWake).Value == 0 && W.AnchorValue(returnedWake).Ticks == 0 &&
            W.GrantUsed(returnedWake).Value == 1, "Returning to the frozen withdrawal cell cancels live tokens.");
        Step(returning, 22, 22, 6); Step(returning, 23, 23, 6);
        Require(W.Grants(returnedWake).Value == 1 && W.Tokens(returnedWake).Value == 0,
            "Leaving the anchor again cannot reopen an exhausted epoch.");

        var unavailableAnchor = Create(valid: false); var restored = Wake(unavailableAnchor);
        Require(W.AnchorValidity(restored).Ticks == 0, "An unknown withdrawal read remains an invalid anchor.");
        Step(unavailableAnchor, 19, 19, 6); Step(unavailableAnchor, 20, 20, 6);
        Require(W.AnchorValue(restored).Ticks == 1 && W.AnchorValidity(restored).Ticks == 1 &&
            W.Grants(restored).Value == 0 && W.CandidateCount(restored).Value == 0,
            "The first restored value establishes a baseline; restoration alone cannot count as changed-pattern evidence.");

        var skipped = Create(); Grant(skipped); var skippedWake = Wake(skipped);
        ((CarrierLocation)W.Parts(skipped)[4]).Tick.Set(0);
        C.Advance(Surface(skipped), L(Clock(skipped).Carrier, 21));
        Step(skipped, 22, 21, 6);
        Require(W.Tokens(skippedWake).Value == 0 && W.GrantUsed(skippedWake).Value == 1 &&
            W.Grants(skippedWake).Value == 1 && !W.TryPermit(skippedWake, Clock(skipped), out _),
            "A skipped watcher arrival cannot hide an intervening invalid input and preserve the unused grant.");
    }

    private static void LaterEpochCanExploreWithoutErasingEvidence()
    {
        var fixture = Create(); var wake = Wake(fixture); Grant(fixture);
        Step(fixture, 26, 26, 6);
        Require(W.Tokens(wake).Value == 0 && !C.Attached(W.Watcher(wake)),
            "The old override expires when ordinary participation resumes.");
        var retry = W.Retry(wake); var basis = C.Current(W.Surface(wake)).Carrier;
        for (var opportunity = 27; opportunity <= 30; opportunity++)
            R.Observe(retry, L(Opportunity(fixture).Carrier, opportunity), L(basis, 0), L(basis, 1),
                L(Money(fixture), 1), N.Group(L(basis, opportunity)));
        var ratings = Ratings(retry);
        Step(fixture, 30, 30, 6);
        Require(W.InitialSyncs(wake).Value == 2 && W.AnchorValue(wake).Ticks == 1 &&
            W.GrantUsed(wake).Value == 0 && R.Rung(retry).Value == 4 && R.RetryAfter(retry).Ticks == 46,
            "A later completed harmful block creates a fresh anchored epoch with its stronger penalty preserved.");
        Step(fixture, 31, 31, 10); Step(fixture, 32, 32, 10);
        Require(W.Grants(wake).Value == 2 && W.Tokens(wake).Value == 2 && R.Harmful(retry).Value == 16 &&
            ratings.SequenceEqual(Ratings(retry)),
            "A subsequent changed pattern may be explored without deleting or improving any prior negative rating.");
    }

    private static void NativeTransportRetainsCandidateAndTokens()
    {
        var fixture = Create(); Step(fixture, 19, 19, 6);
        var clone = (Relation)GojoNumericFormat.Decode(GojoNumericFormat.Encode(fixture)).Single();
        Require(Canonical(fixture) == Canonical(clone) && C.Attached(W.Watcher(Wake(clone))) &&
            ReferenceEquals(W.Surface(Wake(clone)), Surface(clone)) && ReferenceEquals(W.Retry(Wake(clone)), Retry(clone)) &&
            ReferenceEquals(W.Opportunity(Wake(clone)).Tick, Opportunity(clone).Tick),
            "Transport preserves the pending confirmation, live watcher, shared local axes and retry aliases.");
        Step(fixture, 20, 20, 6); Step(clone, 20, 20, 6);
        Require(Canonical(fixture) == Canonical(clone) && W.Tokens(Wake(clone)).Value == 2,
            "The restored candidate confirms on the same later observation.");
        W.Consume(Wake(clone), Clock(clone), N.Group(L(Money(clone), 1)));
        Require(W.Tokens(Wake(fixture)).Value == 2 && W.Tokens(Wake(clone)).Value == 1,
            "Restored native token and admission state mutate independently.");
        W.Consume(Wake(fixture), Clock(fixture), N.Group(L(Money(fixture), 1)));
        Require(Canonical(fixture) == Canonical(clone), "Equal paid consumption continues the restored native state identically.");
        var tokenClone = (Relation)GojoNumericFormat.Decode(GojoNumericFormat.Encode(clone)).Single();
        Step(clone, 21, 21, 6); Step(tokenClone, 21, 21, 6);
        W.Consume(Wake(clone), Clock(clone), N.Group(L(Money(clone), 1)));
        W.Consume(Wake(tokenClone), Clock(tokenClone), N.Group(L(Money(tokenClone), 1)));
        Require(Canonical(clone) == Canonical(tokenClone) && W.Tokens(Wake(tokenClone)).Value == 0 &&
            W.ProbesIssued(Wake(tokenClone)).Value == 2,
            "Transport after partial consumption preserves the remaining single opportunity and one-grant bound.");
    }

    // Test inputs and scaffolding are authored here; every penalty and wake
    // decision is produced by the connected native structures under test.
    private static Relation Create(bool admit = true, bool valid = true, int maximum = 2)
    {
        var input = L(new Carrier(), 3); var presence = L(new Carrier(), valid ? 1 : 0);
        var clock = L(new Carrier(), 18); var opportunity = L(new Carrier(), 0); var money = new Carrier();
        var surface = C.Create(input, presence, C.MagnitudeRead(input, 4), clock.Carrier);
        var loss = C.Current(surface).Carrier;
        var retry = R.Create(surface, N.Group(input), loss, opportunity.Carrier);
        long at = 0;
        for (var block = 0; block < 3; block++)
        {
            for (var trial = 0; trial < 4; trial++)
                R.Observe(retry, L(opportunity.Carrier, ++at), L(loss, 0), L(loss, 1), L(money, 1), N.Group(input));
            if (block < 2) at = R.RetryAfter(retry).Ticks;
        }
        opportunity.Tick.Set(at);
        var wake = W.Create(surface, retry, clock.Carrier, opportunity, maxProbe: maximum);
        var fixture = N.Group(wake, surface, retry, input, presence, clock, opportunity, money);
        C.Advance(surface, clock);
        if (admit) W.Advance(wake, clock);
        Require(at == 18 && R.RetryAfter(retry).Ticks == 26 && R.Harmful(retry).Value == 12,
            "The fixture reaches its cooldown through twelve resolved paid outcomes.");
        return fixture;
    }
    private static void Grant(Relation fixture) { Step(fixture, 19, 19, 6); Step(fixture, 20, 20, 6); }
    private static void Step(Relation fixture, long arrival, long opportunity, long input, bool valid = true)
    {
        ((CarrierLocation)W.Parts(fixture)[3]).Tick.Set(input);
        ((CarrierLocation)W.Parts(fixture)[4]).Tick.Set(valid ? 1 : 0);
        Clock(fixture).Tick.Set(arrival); Opportunity(fixture).Tick.Set(opportunity);
        C.Advance(Surface(fixture), Clock(fixture)); W.Advance(Wake(fixture), Clock(fixture));
    }
    private static Relation Wake(Relation fixture) => (Relation)W.Parts(fixture)[0];
    private static Relation Surface(Relation fixture) => (Relation)W.Parts(fixture)[1];
    private static Relation Retry(Relation fixture) => (Relation)W.Parts(fixture)[2];
    private static CarrierLocation Clock(Relation fixture) => (CarrierLocation)W.Parts(fixture)[5];
    private static CarrierLocation Opportunity(Relation fixture) => (CarrierLocation)W.Parts(fixture)[6];
    private static Carrier Money(Relation fixture) => (Carrier)W.Parts(fixture)[7];
    private static long[] Ratings(Relation retry) => new[]
    {
        C.Signed(R.Rung(retry)).Ticks, R.RetryAfter(retry).Ticks, C.Signed(R.Evaluated(retry)).Ticks, C.Signed(R.Benefit(retry)).Ticks,
        C.Signed(R.Helpful(retry)).Ticks, C.Signed(R.Harmful(retry)).Ticks, C.Signed(R.Neutral(retry)).Ticks, C.Signed(R.Unknown(retry)).Ticks,
        C.Signed(R.CostlyTies(retry)).Ticks, C.Signed(R.CompletedBlocks(retry)).Ticks, C.Signed(R.BlockCount(retry)).Ticks, C.Signed(R.BlockBenefit(retry)).Ticks
    };
    private static long Number(Relation row, int index) => ((CarrierLocation)W.Parts(row)[index]).Ticks;
    private static CarrierLocation L(Carrier basis, long ticks) => new(basis, ticks);
    private static string Canonical(Relation root)
    {
        var snapshot = GojoEncodedSnapshot.CaptureGraph(root);
        return JsonSerializer.Serialize(snapshot with
        {
            Carriers = snapshot.Carriers.Select(row => row with { Id = 0 }).ToArray(),
            TickRefs = snapshot.TickRefs.Select(row => row with { Id = 0 }).ToArray(),
            Relations = snapshot.Relations.Select(row => row with { Id = 0 }).ToArray(),
            UnitShapes = snapshot.UnitShapes.Select(row => row with { Id = 0 }).ToArray()
        });
    }
    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException("Wake probe check failed: " + message); }
}
