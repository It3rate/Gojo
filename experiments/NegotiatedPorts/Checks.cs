using Gojo.Core;
using Gojo.Structures;
using M = NegotiatedPorts.MarketProbe;

namespace NegotiatedPorts;

/// <summary>Native contract checks on explicit local cases. They do not assert
/// that a supplied provider, price policy or ecological hypothesis must win.</summary>
public static class Checks
{
    public static string[] Run()
    {
        var passed = new List<string>();
        RunOne("Initial wallets come from one reservoir; shared-provider offers cannot clone funds.", InitialResources);
        RunOne("Intermediate and synthesized surfaces retain native recipes and signed current reads.", NativePortKinds);
        RunOne("An attached publication follows its exposed changing port, while its issued promise remains frozen.", LivePortFlow);
        RunOne("Signed negative live data clears before withdrawal and reattachment reuses the relation.", WithdrawalAndReuse);
        RunOne("A service freezes its price, demand and interpretation before the subsequent observation.", FrozenServiceContract);
        RunOne("Missing immediate outcomes remain unresolved; later samples cannot replace them.", MissingImmediateOutcome);
        RunOne("Contact, payment and operating expense are separate conserved resource transfers.", PaidServiceAccounting);
        RunOne("An affordable contact cannot buy a service whose full fee is unaffordable.", FullAffordability);
        RunOne("Finite funding respects wallet headroom and cannot manufacture resources from an empty reservoir.", FundingLimits);
        RunOne("Provider reserve excess is recycled rather than spent or silently discarded.", ProviderReserveCap);
        RunOne("Counteroffers apply prospectively and asks cannot cross their operating-cost floors.", ProspectiveCounteroffer);
        RunOne("Two offers of one provider share one service slot; local search is bounded.", SharedCapacityAndLocalSearch);
        RunOne("Pure port and resource inspection cannot charge, refresh or mutate the world.", InspectionIsPure);
        RunOne("Current native forecasts recompute after input retargeting without replacing recipes.", NativeRetargeting);
        RunOne("Operational recipes persist and prior world envelopes do not become a history archive.", BoundedRetainedWorld);
        RunOne("Stale callers and invalid funding are rejected before closing flows or moving resources.", RejectedAdmission);
        return passed.ToArray();

        void RunOne(string description, Action check)
        {
            check();
            passed.Add(description);
        }
    }

    private static void InitialResources()
    {
        var root = M.Create();
        Funds(root);
        var consumers = M.Consumers(root);
        var providers = M.Producers(root);
        var offers = M.Offers(root);
        Require(Owners(root).Length == 7 && M.Reservoir(root).Value == 4068 && M.Spent(root).Value == 0,
            "Exactly seven resource owners retain the 4096 ticks after initial transfers.");
        Require(consumers.All(c => M.Wallet(c).Value == 8) && providers.All(p => M.Reserve(p).Value == 4),
            "Initial consumer and provider funds are transferred from the reservoir.");
        Require(ReferenceEquals(M.Provider(offers[2]), M.Provider(offers[3])) &&
            ReferenceEquals(M.Reserve(M.Provider(offers[2])), M.Reserve(M.Provider(offers[3]))),
            "The rich intermediate and extrapolated offers share one actual reserve.");
        M.Ask(offers[0]).Set(9);
        Funds(root);
        Require(M.Reservoir(root).Value == 4068 && M.Spent(root).Value == 0,
            "An authored informational ask is not minted spending currency.");
    }

    private static void NativePortKinds()
    {
        var root = M.Create();
        Observe(ref root, 0, false, false);
        Require(M.Offers(root).All(offer => !M.Scalar(M.Forecast(offer), out _)),
            "A lone known source level cannot invent a change read.");
        Observe(ref root, 3, false, false);
        Observe(ref root, 7, false, false);
        var offers = M.Offers(root);
        Require(offers.Select(offer => M.Value(M.Forecast(offer)).Ticks).SequenceEqual(new long[] { 1, 1, 4, 5 }),
            "The exposed recipes read direction, an independent direction, intermediate d and synthesized 2d-a.");
        Require(ReferenceEquals(M.Recipe(offers[2]), M.SourceEquations(root)[1]) &&
            ReferenceEquals(M.Recipe(offers[3]), M.SourceEquations(root)[2]),
            "Intermediate and composed ports identify the actual connected source recipes.");
        Require(M.OfferPort(offers[0]).Carrier.Id != M.OfferPort(offers[1]).Carrier.Id,
            "Equal direction output is not shared physical identity.");
        Require(offers.All(offer => M.OfferPresence(offer).Ticks == 0),
            "Unpurchased diagnostic reads do not refresh an exposed paid result.");
        Observe(ref root, 2, false, false);
        Require(offers.Select(offer => M.Value(M.Forecast(offer)).Ticks).SequenceEqual(new long[] { -1, -1, -5, -14 }),
            "Signed source changes propagate through the native recipes.");
        Observe(ref root, null, false, false);
        Require(offers.All(offer => !M.Scalar(M.Forecast(offer), out _) && M.OfferPresence(offer).Ticks == 0),
            "Missing presence does not reinterpret stale exposed coordinates as current input.");
    }

    private static void LivePortFlow()
    {
        var root = M.Create();
        Observe(ref root, 0);
        Observe(ref root, 3);
        var consumer = M.Consumers(root)[0];
        var route = M.SelectedRoute(root, consumer) ??
            throw new InvalidOperationException("The explicit affordable direction service must open a route.");
        var issue = IssueFor(root, consumer);
        var original = Number(issue, 4);
        var port = M.OfferPort(M.RouteOffer(route));
        var initial = port.Ticks;
        port.Tick.Set(-1);
        M.Receipt(route).Settle();
        Require(M.Receipt(route).Value == 0 && M.Receipt(route).Tension == -1,
            "The publication reads the actual exposed port through its native basis mapping.");
        Require(Number(issue, 4) == original && original == 1,
            "A changed current data port does not rewrite its already issued numeric promise.");
        port.Tick.Set(initial);
        M.Receipt(route).Settle();
        Require(Total(M.Receipt(route)) == 1 && M.Attached(route), "The same attached relation follows the restored current port.");
        Funds(root);
    }

    private static void WithdrawalAndReuse()
    {
        var root = M.Create();
        Observe(ref root, 0);
        Observe(ref root, -3);
        var consumer = M.Consumers(root)[0];
        var offer = M.Offers(root)[0];
        var route = Route(root, consumer, offer);
        var publication = M.Publication(route);
        Require(M.Attached(route) && M.Receipt(route).Tension == -1, "An affordable negative data service publishes signed participation.");
        Observe(ref root, -6, false, false);
        Require(!M.Attached(route) && M.Receipt(route).Value == 0 && M.Receipt(route).Tension == 0 &&
            M.Gate(route).Value == 0, "Demand withdrawal closes the live level before detachment.");
        M.OfferPort(offer).Tick.Set(1);
        M.Receipt(route).Settle();
        Require(Total(M.Receipt(route)) == 0, "A detached publication cannot receive later exposed data changes.");
        M.OfferCursor(consumer).Set(0);
        Observe(ref root, -9, true, false, 8, 0);
        Require(M.Attached(route) && ReferenceEquals(M.Publication(route), publication) && Total(M.Receipt(route)) == -1,
            "A new paid service reuses the withdrawn relation and recomputes current data.");
    }

    private static void FrozenServiceContract()
    {
        var root = M.Create();
        Observe(ref root, 0);
        Observe(ref root, 3);
        var consumer = M.Consumers(root)[0];
        var issued = IssueFor(root, consumer);
        var route = (Relation)M.Parts(issued)[3];
        var interpretation = M.ConsumerRead(consumer);
        var bound = (Relation)M.Parts((Relation)M.Parts(interpretation)[1])[0];
        Require(TimeStepSequence.TryReadSpan(bound, out var lower, out var upper), "The current consumer exposes its native interpretation interval.");
        lower.Tick.Set(-2);
        upper.Tick.Set(2);
        M.Ask(M.RouteOffer(route)).Set(7);
        var frame = Observe(ref root, 8, false, false);
        var outcome = OutcomeFor(frame, consumer);
        var response = Response(outcome);
        Require(ReferenceEquals(M.Parts(outcome)[0], issued) && Number(issued, 6) == 3 && Number(issued, 8) == 1,
            "The completed service retains its old price and demand despite current changes.");
        Require(response.Before == 1 && response.After == 0 && Number(outcome, 3) == 4 && Number(outcome, 4) == 1,
            "The subsequent observation uses the issued sign interpretation and price, not the widened live read.");
        Require(Number(IssueFor(root, consumer), 8) == 0 && M.SelectedRoute(root, consumer) is null,
            "Newly withdrawn demand applies only to the next service.");
    }

    private static void MissingImmediateOutcome()
    {
        var root = M.Create();
        Observe(ref root, 0);
        Observe(ref root, 3);
        var consumer = M.Consumers(root)[0];
        var issued = IssueFor(root, consumer);
        var willingness = M.Willingness(consumer).Value;
        var frame = Observe(ref root, null);
        var outcome = OutcomeFor(frame, consumer);
        Require(ReferenceEquals(M.Parts(outcome)[0], issued) && Number(outcome, 8) == 0 &&
            !OperationClaim.TryReadIntervalResponse((Relation)M.Parts(outcome)[2], out _, out _, out _, out _, out _, out _),
            "A missing immediate outcome is unresolved, not a zero-error service.");
        Require(M.Willingness(consumer).Value == willingness && M.SelectedRoute(root, consumer) is null,
            "Unresolved service does not fabricate a negative ability verdict or leave stale publication.");
        frame = Observe(ref root, 6);
        Require(!ReferenceEquals(M.Parts(OutcomeFor(frame, consumer))[0], issued),
            "The next known level cannot be substituted for the missing promised observation.");
    }

    private static void PaidServiceAccounting()
    {
        var root = M.Create();
        Observe(ref root, 0);
        var consumer = M.Consumers(root)[0];
        var provider = M.Producers(root)[0];
        Observe(ref root, 3);
        var issue = IssueFor(root, consumer);
        Require(Number(issue, 6) == 3 && Number(issue, 7) == 1 && Number(issue, 10) == 1,
            "A contact fee and full service price are separately captured before outcome.");
        Require(M.Wallet(consumer).Value == 4 && M.Reserve(provider).Value == 6 && M.Spent(root).Value == 2,
            "Consumer pays contact1 plus price3; provider receives3 and spends operating1.");
        var frame = Observe(ref root, 6, true, false, 8, 0);
        var outcome = OutcomeFor(frame, consumer);
        Require(Number(outcome, 3) == 4 && Number(outcome, 4) == 1 && Number(outcome, 5) == 0 && Number(outcome, 9) == 1,
            "Zero first-trial net including contact can still support positive-net renewal.");
        Require(Number(IssueFor(root, consumer), 7) == 0,
            "An ordinary retained renewal does not repeatedly charge a fresh contact.");
    }

    private static void FullAffordability()
    {
        var root = M.Create();
        var consumer = M.Consumers(root)[0];
        Move(M.Wallet(consumer), M.Spent(root), 5);
        Observe(ref root, 0);
        var beforeSpent = M.Spent(root).Value;
        var beforeProvider = M.Reserve(M.Producers(root)[0]).Value;
        Observe(ref root, 3);
        var issue = IssueFor(root, consumer);
        Require(Number(issue, 7) == 1 && Number(issue, 10) == 0 && Number(issue, 6) == 0,
            "Paying an affordable contact does not pretend the remaining two ticks can fund price three.");
        Require(M.Wallet(consumer).Value == 2 && M.Spent(root).Value == beforeSpent + 1 &&
            M.Reserve(M.Producers(root)[0]).Value == beforeProvider && M.SelectedRoute(root, consumer) is null,
            "No partial payment, operating charge or free publication follows insufficient full affordability.");
        Move(M.Wallet(consumer), M.Spent(root), 2);
        Observe(ref root, 6);
        Require(Number(IssueFor(root, consumer), 7) == 0 && M.Wallet(consumer).Tension == 0,
            "An empty wallet cannot fund a contact by creating negative debt.");
    }

    private static void FundingLimits()
    {
        var root = M.Create();
        Observe(ref root, 0, false, false, 5000, 5000);
        Require(M.Consumers(root).All(c => M.Wallet(c).Value == 16) && M.Reservoir(root).Value == 4052,
            "Large inlet requests only fill each wallet's actual eight-tick headroom.");
        var before = M.Reservoir(root).Value;
        Observe(ref root, 0, false, false, 5000, 5000);
        Require(M.Reservoir(root).Value == before, "Repeated inlet requests cannot refill an already full wallet.");
        foreach (var consumer in M.Consumers(root)) Move(M.Wallet(consumer), M.Spent(root), 16);
        Move(M.Reservoir(root), M.Spent(root), checked((long)M.Reservoir(root).Value));
        Observe(ref root, 0, false, false, 5000, 5000);
        Require(M.Reservoir(root).Value == 0 && M.Consumers(root).All(c => M.Wallet(c).Value == 0 && M.Wallet(c).Tension == 0),
            "An exhausted reservoir cannot clone funding into requested headroom.");
    }

    private static void ProviderReserveCap()
    {
        var root = M.Create();
        var provider = M.Producers(root)[0];
        Move(M.Reservoir(root), M.Reserve(provider), 28);
        Observe(ref root, 0);
        var reservoirBefore = M.Reservoir(root).Value;
        Observe(ref root, 3);
        Require(M.Reserve(provider).Value == 32 && M.Reservoir(root).Value == reservoirBefore + 2,
            "Price3 minus cost1 recycles its two-tick reserve excess before the sale.");
        Require(M.Spent(root).Value == 2, "Reserve recycling is separate from contact and operation spending.");
    }

    private static void ProspectiveCounteroffer()
    {
        var root = M.Create(1, 0);
        var consumer = M.Consumers(root)[0];
        var offer = M.Offers(root)[1];
        Observe(ref root, 0);
        var frame = Observe(ref root, 3);
        var contacts = ContactsFor(frame, consumer);
        Require(contacts.Length == 1 && Number(contacts[0], 2) == 5 && M.Ask(offer).Value == 4,
            "The paid contact records the old ask and changes only a future quote.");
        Require(Number(IssueFor(root, consumer), 10) == 0 && Number(IssueFor(root, consumer), 7) == 1,
            "A newly acceptable counteroffer is not retroactively delivered in its contact frame.");
        ((CarrierLocation)M.Parts(consumer)[3]).Tick.Set(0);
        for (var index = 0; index < 20; index++)
        {
            Observe(ref root, 6 + index * 3L, true, false, 2, 0);
            Require(M.Offers(root).All(item => M.Ask(item).Value >= (ulong)((CarrierLocation)M.Parts(item)[6]).Ticks),
                "Counteroffers cannot cross their native operating-cost floors.");
        }
    }

    private static void SharedCapacityAndLocalSearch()
    {
        var root = M.Create(2, 3);
        var consumers = M.Consumers(root);
        // A selected local bid is a mutable native experimental premise.
        ((CarrierLocation)M.Parts(consumers[0])[3]).Tick.Set(8);
        Observe(ref root, 0, false, false);
        Observe(ref root, 3, false, false);
        var willingnessBefore = consumers.ToDictionary(consumer => consumer.Id,
            consumer => M.Willingness(consumer).Value);
        var frame = Observe(ref root, 7, true, true, 8, 8);
        var provider = M.Producers(root)[2];
        var paid = M.Pending(root).Where(issue => Number(issue, 10) == 1).ToArray();
        Require(paid.Length == 1 && ReferenceEquals(M.Provider(M.RouteOffer((Relation)M.Parts(paid[0])[3])), provider) &&
            M.Occupancy(provider).Value == 1, "Two rich offers cannot sell the same provider service slot twice in one arrival.");
        foreach (var consumer in consumers)
            Require(ContactsFor(frame, consumer).Length <= 1, "One local consumer cannot scan unlimited candidates in an arrival.");
        var blocked = consumers.Single(consumer => !ReferenceEquals(M.Parts(paid[0])[2], consumer));
        Require(M.Willingness(blocked).Value == willingnessBefore[blocked.Id],
            "A capacity-blocked contact cannot install a failed-prediction verdict.");
    }

    private static void InspectionIsPure()
    {
        var root = M.Create();
        Observe(ref root, 0);
        Observe(ref root, -3);
        var before = GojoNumericFormat.Encode(root);
        for (var repeat = 0; repeat < 3; repeat++)
        {
            foreach (var offer in M.Offers(root)) _ = M.Forecast(offer);
            _ = M.Conservation(root);
            _ = M.SelectedRoute(root, M.Consumers(root)[0]);
            _ = M.Pending(root);
        }
        Require(before.AsSpan().SequenceEqual(GojoNumericFormat.Encode(root)),
            "Diagnostics neither spend resources nor refresh exposed data, support or connection state.");
    }

    private static void NativeRetargeting()
    {
        var root = M.Create();
        Observe(ref root, 0, false, false);
        Observe(ref root, 3, false, false);
        Observe(ref root, 7, false, false);
        var equations = M.SourceEquations(root);
        var site = (CarrierLocation)((Relation)M.Parts(M.InputSites(root))[2]).Left;
        site.Tick.Set(-2);
        var offers = M.Offers(root);
        Require(M.Value(M.Forecast(offers[2])).Ticks == -5 && M.Value(M.Forecast(offers[3])).Ticks == -13,
            "Existing input sites recalculate intermediate and composed source reads.");
        Require(equations.Zip(M.SourceEquations(root)).All(pair => ReferenceEquals(pair.First, pair.Second)),
            "Current recalculation reuses the native recipes.");
        Funds(root);
    }

    private static void BoundedRetainedWorld()
    {
        var root = M.Create();
        var operational = M.Operational(root);
        var sourceEquations = M.SourceEquations(root);
        var recipes = M.Offers(root).Select(M.Recipe).ToArray();
        var publications = M.Routes(root).Select(M.Publication).ToArray();
        var oldRootIds = new List<ulong>();
        var frameIds = new List<ulong>();
        var issueIds = new List<(ulong Id, long Version)>();
        for (var index = 0; index < 32; index++)
        {
            oldRootIds.Add(root.Id);
            issueIds.AddRange(M.Pending(root).Select(issue => (issue.Id, Number(issue, 0))));
            var frame = Observe(ref root, 3 * index, true, true, 5, 8);
            frameIds.Add(frame.Id);
            Require(ReferenceEquals(M.Operational(root), operational) &&
                sourceEquations.Zip(M.SourceEquations(root)).All(pair => ReferenceEquals(pair.First, pair.Second)) &&
                recipes.Zip(M.Offers(root).Select(M.Recipe)).All(pair => ReferenceEquals(pair.First, pair.Second)) &&
                publications.Zip(M.Routes(root).Select(M.Publication)).All(pair => ReferenceEquals(pair.First, pair.Second)),
                "Input, commerce and feedback preserve operational recipes and candidate publication identities.");
            Require(M.Parts(root).Length == 4 && M.Pending(root).Length == 2 &&
                M.Pending(root).All(issue => M.Parts(issue).Length == 11), "Only one pending service per consumer is retained.");
            Require(M.Parts((Relation)M.Parts(frame)[2]).Length == 2 && M.Parts((Relation)M.Parts(frame)[3]).Length == 2,
                "The latest frame holds one outcome and decision per consumer.");
            foreach (var consumer in M.Consumers(root))
                Require(ContactsFor(frame, consumer).Length <= 1, "Renewal challenges retain bounded local search.");
        }
        var snapshot = GojoEncodedSnapshot.CaptureGraph(root);
        var retainedIds = snapshot.Relations.Select(relation => relation.Id).ToHashSet();
        Require(!oldRootIds.Any(retainedIds.Contains),
            "Past world envelopes are absent from the live graph; observer history does not drive learning.");
        Require(!frameIds.Take(frameIds.Count - 1).Any(retainedIds.Contains),
            "The latest frame does not retain a chain of earlier diagnostic frames.");
        Require(!issueIds.Where(issue => issue.Version < M.Version(root) - 1).Any(issue => retainedIds.Contains(issue.Id)),
            "Only current pending and immediately evaluated promises survive; older issues do not form a hidden archive.");
    }

    private static void RejectedAdmission()
    {
        var stale = M.Create();
        var root = stale;
        Observe(ref root, 0);
        Observe(ref root, 3);
        var before = GojoNumericFormat.Encode(root);
        Require(!M.TryObserve(stale, 6, true, true, 5, 8, out _, out _), "A stale world cannot spend new funding.");
        Require(before.AsSpan().SequenceEqual(GojoNumericFormat.Encode(root)), "Stale admission cannot close current data or move funds.");
        Require(!M.TryObserve(root, 6, true, true, -1, 8, out _, out _), "Negative external funding is invalid host input.");
        Require(before.AsSpan().SequenceEqual(GojoNumericFormat.Encode(root)), "Rejected host input causes no partial admission.");
    }

    private static Relation Observe(ref Relation root, long? input, bool demand0 = true, bool demand1 = false,
        long funding0 = 0, long funding1 = 0)
    {
        Require(M.TryObserve(root, input, demand0, demand1, funding0, funding1, out var next, out var frame),
            "The supplied local observation is admitted.");
        root = next;
        Funds(root);
        return frame;
    }

    private static Carrier[] Owners(Relation root) => new[] { M.Reservoir(root), M.Spent(root) }
        .Concat(M.Consumers(root).Select(M.Wallet)).Concat(M.Producers(root).Select(M.Reserve)).ToArray();
    private static void Funds(Relation root)
    {
        var owners = Owners(root);
        Require(owners.Select(carrier => carrier.Id).Distinct().Count() == owners.Length,
            "Each wallet, reservoir and spent sink is counted once by physical identity.");
        Require(owners.Sum(Total) == 4096 && M.Pass(M.Conservation(root)),
            "Native and independent accounting preserve all 4096 resource ticks including tension.");
        Require(owners.All(carrier => carrier.Tension == 0), "These fully funded whole-packet transfers leave no hidden debt or overflow.");
        Require(M.Consumers(root).All(consumer => M.Wallet(consumer).Value <= 16) &&
            M.Producers(root).All(provider => M.Reserve(provider).Value <= 32), "Local wallet and reserve caps are respected after admission.");
    }
    private static void Move(Carrier from, Carrier to, long amount)
    {
        var calibration = new Relation(Relation.Segment(from, 0, 1), Relation.Segment(to, 0, 1));
        var request = RefinementTransfer.Create(new Carrier(), calibration, new CarrierLocation(from, amount));
        Require(RefinementTransfer.TryApply(request, out var witness) &&
            ReadWitness.TryRead(witness, out _, out _, out var result, out _, out _) &&
            result is Relation { Left: CarrierLocation drained, Right: CarrierLocation accepted } &&
            drained.Ticks == -amount && accepted.Ticks == amount,
            "Test setup moves an exact packet through the real native material-transfer primitive.");
    }
    private static Relation IssueFor(Relation root, Relation consumer) => M.Pending(root)
        .Single(issue => ReferenceEquals(M.Parts(issue)[2], consumer));
    private static Relation Route(Relation root, Relation consumer, Relation offer) => M.Routes(root)
        .Single(route => ReferenceEquals(M.RouteConsumer(route), consumer) && ReferenceEquals(M.RouteOffer(route), offer));
    private static Relation OutcomeFor(Relation frame, Relation consumer) => M.Parts((Relation)M.Parts(frame)[2])
        .Cast<Relation>().Single(outcome => M.Parts(outcome)[0] is Relation issue && ReferenceEquals(M.Parts(issue)[2], consumer));
    private static Relation[] ContactsFor(Relation frame, Relation consumer)
    {
        var decision = M.Parts((Relation)M.Parts(frame)[3]).Cast<Relation>()
            .Single(item => ReferenceEquals(M.Parts(item)[0], consumer));
        return M.Parts((Relation)M.Parts(decision)[3]).Cast<Relation>().ToArray();
    }
    private static (long Before, long After, long Reduction) Response(Relation outcome)
    {
        Require(OperationClaim.TryReadIntervalResponse((Relation)M.Parts(outcome)[2], out _, out _, out _,
            out var before, out var after, out var reduction), "The issued response resolves on one captured consumer basis.");
        return (before.Ticks, after.Ticks, reduction.Ticks);
    }
    private static long Number(Relation group, int field) => ((CarrierLocation)M.Parts(group)[field]).Ticks;
    private static long Total(Carrier carrier) => checked((long)carrier.Value + carrier.Tension);
    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException("Negotiated ports check failed: " + message);
    }
}
