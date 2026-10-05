using System.Text.Json;
using Gojo.Core;
using N = ConnectionAssembly.NativeMath;

namespace FailureSubscriptions;

internal static class DurationOfferChecks
{
    public static int Run()
    {
        ColdAndShortenedOffer();
        HarmWaitsForArrivalClock();
        UnknownAndCensoredTail();
        SnapshotCausality();
        ContextAndOutcomeBasis();
        NativeTransport();
        SignedPressureAndEvidenceGrain();
        return 7;
    }

    private static void ColdAndShortenedOffer()
    {
        var signal = new Carrier(); var clock = new Carrier(); var money = new Carrier();
        var source = N.Group(L(signal, 0)); var premise = N.Group(L(signal, 0), L(clock, 0));
        var state = DurationOffer.Create(source, Relation.Segment(signal, -32, -1), premise: premise);
        var cold = Quote(state, signal, clock, money, 0, 8);
        Require(DurationOffer.Matches(cold) && DurationOffer.Backed(cold).Ticks == 0 &&
            DurationOffer.Offered(cold).Ticks == 1 && DurationOffer.Accepted(cold).Ticks == 1,
            "A cold provider can offer one explicit frontier trial, with no backed lifetime invented.");
        var declined = Quote(state, signal, clock, money, 0, 0);
        Require(DurationOffer.Matches(declined) && DurationOffer.Accepted(declined).Ticks == 0 &&
            DurationOffer.TotalFee(declined).Ticks == 0, "A zero requester need purchases no activation or outputs.");

        var time = TrainPrefix(state, signal, clock, money, 0, 4);
        var quote = Quote(state, signal, clock, money, time, 2);
        Require(DurationOffer.Backed(quote).Ticks == 4 && DurationOffer.Offered(quote).Ticks == 5 &&
            DurationOffer.Accepted(quote).Ticks == 2,
            "Four causally supported rungs offer five including the frontier; a requester may buy only two.");
        Require(DurationOffer.TotalFee(quote).Ticks == 8 && DurationOffer.OfferedFee(quote).Ticks == 17,
            "The accepted tariff charges activation two plus two deliveries at three, not all five offered deliveries.");
        Require(DurationOffer.Future(quote).Left is CarrierLocation start && start.Ticks == time &&
            DurationOffer.Future(quote).Right is CarrierLocation end && end.Ticks == time + 2 &&
            start.Carrier.SameIdentityAs(clock) && end.Carrier.SameIdentityAs(clock),
            "The accepted future window keeps its signed clock basis and ordered extent.");
        Require(ReferenceEquals(DurationOffer.Source(state), source) && ReferenceEquals(DurationOffer.Premise(state), premise),
            "The attach point and buyer baseline remain actual connected premises of duration evidence.");
    }

    private static void HarmWaitsForArrivalClock()
    {
        var signal = new Carrier(); var clock = new Carrier(); var money = new Carrier();
        var state = DurationOffer.Create(signal, Relation.Segment(signal, -32, -1));
        var initial = Quote(state, signal, clock, money, 0, 8);
        DurationOffer.Observe(state, initial, L(clock, 1), L(signal, -3), N.Group(initial, L(clock, 1)));
        var first = DurationOffer.Rows(state)[0];
        Require(DurationOffer.RetryAfter(first).Value == 9 && DurationOffer.Benefit(first).Tension == -3,
            "Harm remains signed pressure and sets a retry deadline eight actual input arrivals later.");
        foreach (var now in new[] { 1L, 2, 8, 8, 8 })
        {
            var quote = Quote(state, signal, clock, money, now, 8);
            Require(DurationOffer.Offered(quote).Ticks == 0 && DurationOffer.Accepted(quote).Ticks == 0 &&
                DurationOffer.TotalFee(quote).Ticks == 0,
                "Repeated inquiries cannot accelerate a known harmful frontier's arrival-clock cooldown.");
        }
        var retry = Quote(state, signal, clock, money, 9, 8);
        Require(DurationOffer.Backed(retry).Ticks == 0 && DurationOffer.Offered(retry).Ticks == 1,
            "An elapsed deadline permits one new trial, not a newly trusted prefix.");

        var negative = DurationOffer.Create(signal, Relation.Segment(signal, -32, -1));
        var past = Quote(negative, signal, clock, money, -20, 8);
        DurationOffer.Observe(negative, past, L(clock, -19), L(signal, 0), new Placeholder());
        Require(DurationOffer.Offered(Quote(negative, signal, clock, money, -12, 8)).Ticks == 0 &&
            DurationOffer.Offered(Quote(negative, signal, clock, money, -11, 8)).Ticks == 1,
            "Negative historical clock deadlines are read through value and tension rather than clipped to zero.");
    }

    private static void UnknownAndCensoredTail()
    {
        var signal = new Carrier(); var clock = new Carrier(); var money = new Carrier();
        var state = DurationOffer.Create(signal, Relation.Segment(signal, -32, -1));
        var quote = Quote(state, signal, clock, money, 0, 1);
        DurationOffer.Observe(state, quote, L(clock, 1), null, new Placeholder());
        var row = DurationOffer.Rows(state)[0];
        Require(DurationOffer.Unknown(row).Value == 1 && DurationOffer.Evaluated(row).Value == 0 &&
            DurationOffer.Helpful(row).Value == 0 && !DurationOffer.RetryAfter(row).HasState,
            "Missing target data adds unknown coverage without invented harm, resolved support or a cooldown.");
        Require(DurationOffer.Offered(Quote(state, signal, clock, money, 2, 8)).Ticks == 1,
            "An unknown-only frontier remains available as a trial.");

        var time = TrainPrefix(state, signal, clock, money, 2, 4);
        var shortQuote = Quote(state, signal, clock, money, time, 2);
        var tailBefore = DurationOffer.Rows(state).Skip(2).Select(Canonical).ToArray();
        DurationOffer.Observe(state, shortQuote, L(clock, time + 1), L(signal, 1), new Placeholder());
        DurationOffer.Observe(state, shortQuote, L(clock, time + 2), null, new Placeholder());
        var beforeOutside = Canonical(state);
        var outside = DurationOffer.Observe(state, shortQuote, L(clock, time + 3), L(signal, -8), new Placeholder());
        Require(Canonical(state) == beforeOutside && tailBefore.SequenceEqual(DurationOffer.Rows(state).Skip(2).Select(Canonical)),
            "The unpurchased tail is right-censored; it cannot accumulate missing or harmful evidence.");
        Require(N.Parts(outside).Last() is Relation, "An out-of-window observation retains its unresolved relation.");
    }

    private static void SnapshotCausality()
    {
        var signal = new Carrier(); var clock = new Carrier(); var money = new Carrier();
        var state = DurationOffer.Create(signal, Relation.Segment(signal, -32, -1));
        var trigger = L(signal, -2); var start = L(clock, 10); var fee = L(money, 3); var activation = L(money, 2);
        var quote = DurationOffer.Quote(state, trigger, start, 8, fee, activation);
        var captured = ProfileTicks(quote);
        var witness = DurationOffer.Observe(state, quote, L(clock, 11), L(signal, 4), N.Group(signal, clock));
        trigger.Tick.Set(3); start.Tick.Set(90); fee.Tick.Set(99); activation.Tick.Set(99);
        Require(ProfileTicks(quote).SequenceEqual(captured) && DurationOffer.Backed(quote).Ticks == 0 &&
            DurationOffer.Offered(quote).Ticks == 1 && DurationOffer.TotalFee(quote).Ticks == 5 &&
            ((CarrierLocation)N.Parts(quote)[1]).Ticks == -2 && ((CarrierLocation)N.Parts(quote)[2]).Ticks == 10,
            "Later outcome, time and tariff changes cannot rewrite the already issued offer.");
        Require(DurationOffer.Evaluated(DurationOffer.Rows(state)[0]).Value == 1 &&
            ReferenceEquals(N.Parts(witness)[1], quote), "The live profile changes while retaining the original quote as outcome provenance.");
        var warm = Quote(state, signal, clock, money, 12, 8);
        Require(DurationOffer.Backed(warm).Ticks == 0 && DurationOffer.Offered(warm).Ticks == 1,
            "One successful outcome still does not back a rung requiring two resolved observations.");
    }

    private static void ContextAndOutcomeBasis()
    {
        var signal = new Carrier(); var clock = new Carrier(); var money = new Carrier();
        var state = DurationOffer.Create(signal, Relation.Segment(signal, -32, -1));
        foreach (var trigger in new[] { L(signal, 0), L(signal, 3), L(signal, -33), L(new Carrier(), -2) })
        {
            var quote = DurationOffer.Quote(state, trigger, L(clock, 0), 8, L(money, 3), L(money, 2));
            var before = Canonical(state);
            Require(!DurationOffer.Matches(quote) && DurationOffer.Accepted(quote).Ticks == 0,
                "A mismatched initiating-error cohort cannot borrow a duration forecast.");
            DurationOffer.Observe(state, quote, L(clock, 1), L(signal, 1), new Placeholder());
            Require(Canonical(state) == before, "An unmatched quote cannot accumulate duration evidence.");
        }
        foreach (var cap in new[] { -1, 9 })
            Require(!DurationOffer.Matches(Quote(state, signal, clock, money, 0, cap)),
                "An unavailable requester extent stays unresolved rather than being silently clamped.");
        var unboundTime = DurationOffer.Quote(state, L(signal, -2), L(Carrier.Unknown, 0), 8, L(money, 3), L(money, 2));
        Require(!DurationOffer.Matches(unboundTime) && DurationOffer.Accepted(unboundTime).Ticks == 0 &&
            DurationOffer.TotalFee(unboundTime).Ticks == 0 && DurationOffer.Future(unboundTime).Right is Relation,
            "An unbound clock preserves an open future read and no purchase, rather than throwing or inventing a deadline.");
        var valid = Quote(state, signal, clock, money, 0, 8);
        var snapshot = Canonical(state);
        DurationOffer.Observe(state, valid, L(new Carrier(), 1), L(signal, 1), new Placeholder());
        DurationOffer.Observe(state, valid, L(clock, 1), L(new Carrier(), 1), new Placeholder());
        Require(Canonical(state) == snapshot, "Coincident numeric target or benefit values cannot borrow an unaligned clock or signal basis.");
        var foreign = DurationOffer.Create(signal, Relation.Segment(signal, -32, -1));
        DurationOffer.Observe(foreign, valid, L(clock, 1), L(signal, 1), new Placeholder());
        Require(DurationOffer.Evaluated(DurationOffer.Rows(foreign)[0]).Value == 0,
            "A quote remains attached to its actual profile identity.");
    }

    private static void NativeTransport()
    {
        var signal = new Carrier(); var clock = new Carrier(); var money = new Carrier();
        var premise = N.Group(L(signal, 0), L(clock, 0));
        var state = DurationOffer.Create(N.Group(signal), Relation.Segment(signal, -32, -1), premise: premise);
        var quote = Quote(state, signal, clock, money, 3, 8);
        DurationOffer.Observe(state, quote, L(clock, 4), L(signal, -3), new Placeholder());
        var root = N.Group(state, quote, signal, clock, money, premise);
        var clone = (Relation)GojoNumericFormat.Decode(GojoNumericFormat.Encode(root)).Single();
        Require(Canonical(root) == Canonical(clone), "Native transport retains duration profile, signed pressure, window and captured tariff.");
        var parts = N.Parts(clone); var copiedState = (Relation)parts[0];
        Require(ReferenceEquals(DurationOffer.Premise(copiedState), parts[5]) && ReferenceEquals(N.Parts((Relation)parts[1])[0], copiedState),
            "Transport retains shared baseline and quote-to-profile identity.");
        var next = Quote(state, signal, clock, money, 12, 8);
        var copiedNext = Quote(copiedState, (Carrier)parts[2], (Carrier)parts[3], (Carrier)parts[4], 12, 8);
        DurationOffer.Observe(state, next, L(clock, 13), L(signal, 2), new Placeholder());
        DurationOffer.Observe(copiedState, copiedNext, L((Carrier)parts[3], 13), L((Carrier)parts[2], 2), new Placeholder());
        Require(Canonical(root) == Canonical(clone), "Restored duration structures continue from the same native evidence.");
    }

    private static void SignedPressureAndEvidenceGrain()
    {
        var signal = new Carrier(); var clock = new Carrier(); var money = new Carrier();
        var state = DurationOffer.Create(signal, Relation.Segment(signal, -32, -1));
        var first = Quote(state, signal, clock, money, 0, 8);
        DurationOffer.Observe(state, first, L(clock, 1), L(signal, -6), new Placeholder());
        var retry = Quote(state, signal, clock, money, 9, 8);
        DurationOffer.Observe(state, retry, L(clock, 10), L(signal, 2), new Placeholder());
        var counts = N.Parts((Relation)N.Parts(DurationOffer.Profile(Quote(state, signal, clock, money, 11, 8)))[0]).Cast<CarrierLocation>().ToArray();
        Require(counts[1].Ticks == 1 && counts[2].Ticks == 2 && counts[3].Ticks == 0 && counts[4].Ticks == -4,
            "The empirical 1-of-2 helpful count and net negative relief remain unreduced independent information.");
        Require(DurationOffer.Backed(Quote(state, signal, clock, money, 11, 8)).Ticks == 0,
            "Two evaluated outcomes alone do not back a rung when aggregate relief remains harmful.");
        var third = Quote(state, signal, clock, money, 18, 8);
        DurationOffer.Observe(state, third, L(clock, 19), L(signal, 8), new Placeholder());
        var warm = Quote(state, signal, clock, money, 20, 8);
        Require(DurationOffer.Backed(warm).Ticks == 1 && DurationOffer.Offered(warm).Ticks == 2,
            "Later sufficient positive net relief can support the first rung while exposing only one further frontier.");
    }

    private static long TrainPrefix(Relation state, Carrier signal, Carrier clock, Carrier money, long start, int backed)
    {
        // Fixture generation only: each accepted outcome is delivered after
        // its captured issue; no counters or offered lengths are assigned.
        for (var term = 0; term < backed * 2; term++)
        {
            var quote = Quote(state, signal, clock, money, start, 8);
            var accepted = DurationOffer.Accepted(quote).Ticks;
            for (long offset = 1; offset <= accepted; offset++)
                DurationOffer.Observe(state, quote, L(clock, start + offset), L(signal, 2), N.Group(quote, L(clock, start + offset)));
            start += accepted + 1;
        }
        return start;
    }

    private static Relation Quote(Relation state, Carrier signal, Carrier clock, Carrier money, long time, int cap) =>
        DurationOffer.Quote(state, L(signal, -2), L(clock, time), cap, L(money, 3), L(money, 2));
    private static long[] ProfileTicks(Relation quote) => N.Parts(DurationOffer.Profile(quote)).Cast<Relation>()
        .SelectMany(row => N.Parts(row).Cast<CarrierLocation>().Select(location => location.Ticks)).ToArray();
    private static CarrierLocation L(Carrier carrier, long ticks) => new(carrier, ticks);
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
    {
        if (!condition) throw new InvalidOperationException("Duration offer check failed: " + message);
    }
}
