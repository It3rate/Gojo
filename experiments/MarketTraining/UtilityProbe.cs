using System.Text.Json;
using Gojo.Core;
using Gojo.Structures;
using M = NegotiatedPorts.MarketProbe;

namespace MarketTraining;

/// <summary>
/// Authored potential-utility fixtures, not new learner roles or selection laws.
/// These counters and fixture labels are observer-only and never enter a world.
/// </summary>
public static class UtilityProbe
{
    public static object Run(string directory)
    {
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, "utility-probes.json");
        if (File.Exists(path)) throw new InvalidOperationException("Preserve prior utility-probe evidence; choose a new directory.");
        var runs = new List<object>();
        foreach (var cheaper in new[] { 0, 1 })
        foreach (var cursor in new[] { 0, 1 })
            runs.Add(Observe($"price-cheaper-{cheaper}-cursor-{cursor}", 0, cursor,
                [cheaper == 0 ? 2 : 3, cheaper == 1 ? 2 : 3, 10000, 10000], [1, 1, 2, 3], [0, 1]));
        foreach (var cursor in new[] { 0, 2 })
            runs.Add(Observe($"value-cursor-{cursor}", 1, cursor,
                [3, 10000, 3, 10000], [1, 2, 1, 3], [0, 2]));
        var result = new
        {
            protocol = "MarketTraining/PROTOCOL.md: paired potential-utility fixtures",
            purpose = "Observe price and consumer-value response under the unchanged supplied local policy; no designated winner assertion.",
            scope = "Six fixed-population local worlds. Ports, cyclic encounter order and provider structure are not globally identical. High-ask alternatives remain discoverable and can receive paid contacts and counteroffers.",
            input = "32 arrivals: level 100, followed by 31 changes of +3. Only the active consumer demands service. Both consumer funding requests remain 5/8 from the same original 4096-credit reservoir.",
            measurement = "All arithmetic, predictions, relief, renewal and payments come from native world reads. Host counters aggregate them only after admission. Issued services, evaluated services, unresolved outcomes and final pending obligations are distinct. Gross/net below use the active consumer's calibrated resource basis; raw errors retain its own read basis.",
            runs
        };
        File.WriteAllText(path, JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
        return result;
    }

    private static object Observe(string name, int activeConsumer, int cursor, long[] prices, long[] costs, int[] comparison)
    {
        var root = M.Create(activeConsumer == 0 ? cursor : 0, activeConsumer == 1 ? cursor : 0);
        var offers = M.Offers(root);
        var consumers = M.Consumers(root);
        // Authored informational prices settle through carrier tension; these
        // sites are not spendable wallets. Costs are connected structural sites.
        for (var i = 0; i < offers.Length; i++)
        {
            M.Ask(offers[i]).Set(prices[i]);
            ((CarrierLocation)M.Parts(offers[i])[6]).Tick.Set(costs[i]);
        }
        if (!M.Pass(M.Conservation(root)) || M.Reservoir(root).Value != 4068 ||
            consumers.Any(consumer => M.Wallet(consumer).Value != 8))
            throw new InvalidOperationException("Authoring information must not change spendable resources.");
        var counters = Enumerable.Range(0, offers.Length).Select(index => new OfferCounter(index)).ToArray();
        long demandedEvaluated = 0, demandedBaselineError = 0, demandedDeliveredError = 0;
        long paidServices = 0, evaluatedPaid = 0, unresolvedPaid = 0, noServiceEvaluated = 0;
        long allContacts = 0, allContactFees = 0, allServicePrices = 0, evaluatedGross = 0;
        long evaluatedOngoingNet = 0, evaluatedOwnContactNet = 0;
        var conservedEveryArrival = true;
        var trace = new List<object>();
        for (var step = 0; step < 32; step++)
        {
            // External authored stimulus, never a classifier or forecast input
            // beyond the currently admitted scalar observation.
            var input = 100L + 3L * step;
            if (!M.TryObserve(root, input, activeConsumer == 0, activeConsumer == 1,
                    5, 8, out var next, out var frame))
                throw new InvalidOperationException("The authored utility fixture admission was rejected.");
            root = next;
            var frameParts = M.Parts(frame);
            var conserved = M.Pass((Relation)frameParts[5]);
            conservedEveryArrival &= conserved;
            if (!conserved) throw new InvalidOperationException("Native utility-fixture conservation failed.");
            var decisions = M.Parts((Relation)frameParts[3]);
            var decision = M.Parts((Relation)decisions[activeConsumer]);
            var contactRows = new List<object>();
            foreach (var contact in M.Parts((Relation)decision[3]).Cast<Relation>())
            {
                var row = M.Parts(contact);
                var index = Index(offers, (Relation)row[0]);
                var counter = counters[index];
                counter.ContactAttempts++;
                var paidContact = row[4] is CarrierLocation;
                var fits = row[4] is CarrierLocation fit ? fit.Ticks == 1 : (bool?)null;
                var free = row[5] is CarrierLocation capacity ? capacity.Ticks == 1 : (bool?)null;
                var agreed = Number(row[6]) == 1;
                if (paidContact) { counter.PaidContacts++; allContacts++; counter.ContactFees += Number(row[3]); }
                if (fits == false) counter.PriceRejectedContacts++;
                if (free == false) counter.CapacityBlockedContacts++;
                if (fits == true && free == true) counter.PriceFitAndFreeContacts++;
                if (agreed) counter.Agreements++;
                contactRows.Add(new { offer = index, paidContact, bid = Number(row[1]), quotedAsk = Number(row[2]), fits, free, agreed });
            }

            var issue = M.Parts((Relation)decision[4]);
            var paid = Number(issue[10]) == 1;
            var selected = issue[3] is Relation route ? Index(offers, M.RouteOffer(route)) : (int?)null;
            allContactFees += Number(issue[7]);
            allServicePrices += Number(issue[6]);
            if (paid && selected.HasValue)
            {
                paidServices++;
                counters[selected.Value].ServicesIssued++;
                counters[selected.Value].ServicePrices += Number(issue[6]);
            }

            var outcome = M.Parts((Relation)M.Parts((Relation)frameParts[2])[activeConsumer]);
            object previous = new { issued = false };
            if (outcome[0] is Relation previousIssue)
            {
                var prior = M.Parts(previousIssue);
                var priorPaid = Number(prior[10]) == 1;
                var evaluated = Number(outcome[8]) == 1;
                var priorIndex = prior[3] is Relation priorRoute ? Index(offers, M.RouteOffer(priorRoute)) : (int?)null;
                long? baseline = null, delivered = null, relief = null;
                if (evaluated)
                {
                    if (!OperationClaim.TryReadIntervalResponse((Relation)outcome[2], out _, out _, out _, out var before, out var after, out var reduction))
                        throw new InvalidOperationException("An evaluated utility outcome lacked its native response.");
                    baseline = before.Ticks; delivered = after.Ticks; relief = reduction.Ticks;
                    if (Number(prior[8]) == 1)
                    {
                        demandedEvaluated++;
                        demandedBaselineError += baseline.Value;
                        demandedDeliveredError += delivered.Value;
                    }
                    if (priorPaid && priorIndex.HasValue)
                    {
                        evaluatedPaid++;
                        var counter = counters[priorIndex.Value];
                        counter.ServicesEvaluated++;
                        counter.BaselineError += baseline.Value;
                        counter.DeliveredError += delivered.Value;
                        counter.Relief += relief.Value;
                        var gross = Number(outcome[3]);
                        var ongoingNet = Number(outcome[4]);
                        var totalNet = Number(outcome[5]);
                        counter.GrossCredits += gross; evaluatedGross += gross;
                        counter.OngoingNet += ongoingNet; evaluatedOngoingNet += ongoingNet;
                        counter.NetIncludingOwnContact += totalNet; evaluatedOwnContactNet += totalNet;
                        if (gross > 0) counter.PositiveServices++;
                        else if (gross < 0) counter.HarmfulServices++;
                        else counter.ZeroReliefServices++;
                        if (Number(outcome[9]) == 1) counter.RetainableOutcomes++;
                    }
                    else noServiceEvaluated++;
                }
                else if (priorPaid && priorIndex.HasValue)
                { unresolvedPaid++; counters[priorIndex.Value].UnresolvedOutcomes++; }
                previous = new
                {
                    issued = true, paid = priorPaid, evaluated, offer = priorIndex, issueStep = Number(prior[0]),
                    forecast = prior[4] is CarrierLocation oldForecast ? oldForecast.Ticks : (long?)null,
                    actual = outcome[1] is CarrierLocation actual ? actual.Ticks : (long?)null,
                    baselineError = baseline, deliveredError = delivered, relief,
                    grossCredits = outcome[3] is CarrierLocation grossValue ? grossValue.Ticks : (long?)null,
                    netIncludingOwnContact = outcome[5] is CarrierLocation net ? net.Ticks : (long?)null
                };
            }
            trace.Add(new
            {
                step = M.Version(root), input, selectedOffer = selected, paid,
                forecast = issue[4] is CarrierLocation currentForecast ? currentForecast.Ticks : (long?)null,
                servicePrice = Number(issue[6]), contactFee = Number(issue[7]),
                wallet = M.Wallet(consumers[activeConsumer]).Value,
                reservoir = M.Reservoir(root).Value, spent = M.Spent(root).Value,
                conserved, contacts = contactRows, previous
            });
        }
        var outstanding = M.Parts((Relation)M.Parts(root)[1]).OfType<Relation>()
            .Count(issue => Number(M.Parts(issue)[10]) == 1);
        return new
        {
            name, activeConsumer, basis = activeConsumer == 0 ? "direction ticks" : "source-change ticks",
            comparisonOffers = comparison, initialCursor = cursor, initialAsks = prices, authoredOperatingCosts = costs,
            arrivals = 32, paidServicesIssued = paidServices, evaluatedPaid, unresolvedPaid, outstandingPaid = outstanding,
            demandedEvaluated, noServiceEvaluated, demandedBaselineError, demandedDeliveredError,
            paidContacts = allContacts, allContactFees, allServicePrices,
            evaluatedPaidGross = evaluatedGross, evaluatedPaidOngoingNet = evaluatedOngoingNet,
            evaluatedPaidNetIncludingOwnContact = evaluatedOwnContactNet,
            grossEvaluatedMinusAllCurrentOutlay = evaluatedGross - allContactFees - allServicePrices,
            outlayQualification = "The all-outlay balance charges every contact and service, including the final pending promise, while crediting only evaluated paid outcomes. Per-service net charges the contact captured with that promise, including a failed challenger contact when the incumbent renews; costs from other admissions are reported separately.",
            conservedEveryArrival, finalReservoir = M.Reservoir(root).Value, finalSpent = M.Spent(root).Value,
            finalAsks = offers.Select(offer => M.Ask(offer).Value).ToArray(), offers = counters, trace
        };
    }

    private static int Index(Relation[] offers, Relation offer) => Array.FindIndex(offers, item => ReferenceEquals(item, offer));
    private static long Number(IElement element) => ((CarrierLocation)element).Ticks;

    // Mutable observer accumulators are deliberately outside canonical meaning.
    private sealed class OfferCounter(int id)
    {
        public int Id { get; } = id;
        public long ContactAttempts { get; set; }
        public long PaidContacts { get; set; }
        public long ContactFees { get; set; }
        public long PriceRejectedContacts { get; set; }
        public long CapacityBlockedContacts { get; set; }
        public long PriceFitAndFreeContacts { get; set; }
        public long Agreements { get; set; }
        public long ServicesIssued { get; set; }
        public long ServicePrices { get; set; }
        public long ServicesEvaluated { get; set; }
        public long UnresolvedOutcomes { get; set; }
        public long PositiveServices { get; set; }
        public long ZeroReliefServices { get; set; }
        public long HarmfulServices { get; set; }
        public long RetainableOutcomes { get; set; }
        public long BaselineError { get; set; }
        public long DeliveredError { get; set; }
        public long Relief { get; set; }
        public long GrossCredits { get; set; }
        public long OngoingNet { get; set; }
        public long NetIncludingOwnContact { get; set; }
    }
}
