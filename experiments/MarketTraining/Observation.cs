using Gojo.Core;
using Gojo.Structures;
using M = NegotiatedPorts.MarketProbe;

namespace MarketTraining;

// These bounded host accumulators are reports, not learning state. They never
// feed a bid, route, equation, promise, or resource transfer.
internal sealed class ConsumerTotals
{
    public long Demanded { get; set; }
    public long ReadableDemanded { get; set; }
    public long RequestedFunding { get; set; }
    public long DemandedWithSpendableCredit { get; set; }
    public long AdmittedFunding { get; set; }
    public long PriorDemanded { get; set; }
    public long EvaluatedDemanded { get; set; }
    public long UnresolvedDemanded { get; set; }
    public long UnresolvedPaid { get; set; }
    public long EvaluatedPaid { get; set; }
    public long BaselineError { get; set; }
    public long DeliveredError { get; set; }
    public long PositivePaid { get; set; }
    public long ZeroPaid { get; set; }
    public long HarmfulPaid { get; set; }
    public long RetainedPaid { get; set; }
    public long Contacts { get; set; }
    public long PaidContacts { get; set; }
    public long Purchases { get; set; }
    public long SameSupplierConsecutiveServices { get; set; }
    public long NewConnections { get; set; }
    public long OfferSwitches { get; set; }
    public long Releases { get; set; }
    public long QuoteRejections { get; set; }
    public long CapacityBlockedContacts { get; set; }
    public long UnfundedContacts { get; set; }
    public long FailedPaidContactExpense { get; set; }
    public long ServiceOutlay { get; set; }
    public long ContactOutlay { get; set; }
    public long Gross { get; set; }
    public long[] OfferPurchases { get; set; } = new long[4];
    public long[] PaidAskSum { get; set; } = new long[4];
}

internal sealed class WindowTotals
{
    public long Start { get; set; }
    public long End { get; set; }
    public long Admissions { get; set; }
    public long NativeAllocationBytes { get; set; }
    public double NativeWallMilliseconds { get; set; }
    public long ObserverAllocationBytes { get; set; }
    public double ObserverWallMilliseconds { get; set; }
    public bool Conserved { get; set; } = true;
    public long ReservoirEmptyObservations { get; set; }
    public long QuietObservations { get; set; }
    public long NoContactAttemptObservations { get; set; }
    public Dictionary<string, long> SuppliedPhases { get; set; } = new();
    public ConsumerTotals[] Consumers { get; set; } = [new(), new()];
}

internal static class Observation
{
    public static void Add(WindowTotals totals, Relation root, Relation frame, string phase)
    {
        var p = M.Parts(frame);
        var consumers = M.Consumers(root);
        var offers = M.Offers(root);
        var issues = M.Pending(root);
        var outcomes = M.Parts((Relation)p[2]).Cast<Relation>().ToArray();
        var decisions = M.Parts((Relation)p[3]).Cast<Relation>().ToArray();
        var readable = p[1] is CarrierLocation && M.Scalar(ConnectionAssembly.NativeMath.Read(M.SourceEquations(root)[1]), out _);
        var quiet = true;
        var noContact = true;
        totals.End = M.Version(root);
        if (totals.Admissions == 0) totals.Start = totals.End;
        totals.Admissions++;
        totals.SuppliedPhases[phase] = totals.SuppliedPhases.GetValueOrDefault(phase) + 1;
        totals.Conserved &= M.Pass((Relation)p[5]);
        if (M.Reservoir(root).Value == 0) totals.ReservoirEmptyObservations++;
        for (var index = 0; index < consumers.Length; index++)
        {
            var t = totals.Consumers[index];
            var issue = M.Parts(issues[index]);
            var decision = M.Parts(decisions[index]);
            var outcome = M.Parts(outcomes[index]);
            var preceding = outcome[0] is Relation precedingIssue ? M.Parts(precedingIssue) : null;
            if (Number(issue[8]) == 1) t.Demanded++;
            if (Number(issue[8]) == 1 && readable) t.ReadableDemanded++;
            t.RequestedFunding += index == 0 ? 5 : 8;
            if (Number(issue[8]) == 1 && (long)M.Wallet(consumers[index]).Value + Number(issue[6]) + Number(issue[7]) > 0)
                t.DemandedWithSpendableCredit++;
            var paid = Number(issue[10]) == 1;
            var selected = decision[2] as Relation;
            var incumbent = preceding is not null && Number(preceding[10]) == 1 ? preceding[3] as Relation : null;
            if (paid)
            {
                quiet = false;
                t.Purchases++;
                t.ServiceOutlay += Number(issue[6]);
                var offerIndex = Array.FindIndex(offers, offer => ReferenceEquals(offer, M.RouteOffer(selected!)));
                t.OfferPurchases[offerIndex]++;
                t.PaidAskSum[offerIndex] += Number(issue[6]);
                if (ReferenceEquals(incumbent, selected)) t.SameSupplierConsecutiveServices++;
                else if (incumbent is null) t.NewConnections++;
                else t.OfferSwitches++;
            }
            if (incumbent is not null && selected is null) t.Releases++;
            t.ContactOutlay += Number(issue[7]);
            foreach (var contact in M.Parts((Relation)decision[3]).Cast<Relation>())
            {
                var fields = M.Parts(contact);
                noContact = false;
                t.Contacts++;
                if (fields[4] is CarrierLocation fit)
                {
                    t.PaidContacts++; quiet = false;
                    if (fit.Ticks == 0) t.QuoteRejections++;
                    if (Number(fields[5]) == 0) t.CapacityBlockedContacts++;
                    if (Number(fields[6]) == 0) t.FailedPaidContactExpense += Number(fields[3]);
                }
                else t.UnfundedContacts++;
            }
            if (outcome[0] is not Relation oldIssue) continue;
            var prior = M.Parts(oldIssue);
            if (Number(prior[8]) != 1) continue;
            t.PriorDemanded++;
            if (Number(outcome[8]) != 1)
            {
                t.UnresolvedDemanded++;
                if (Number(prior[10]) == 1) t.UnresolvedPaid++;
                continue;
            }
            t.EvaluatedDemanded++;
            if (!OperationClaim.TryReadIntervalResponse((Relation)outcome[2], out _, out _, out _,
                out var before, out var after, out var relief))
                throw new InvalidDataException("An evaluated outcome has no native interval response.");
            t.BaselineError += before.Ticks;
            t.DeliveredError += after.Ticks;
            if (Number(prior[10]) != 1) continue;
            t.EvaluatedPaid++;
            t.Gross += Number(outcome[3]);
            if (relief.Ticks > 0) t.PositivePaid++;
            else if (relief.Ticks < 0) t.HarmfulPaid++;
            else t.ZeroPaid++;
            if (Number(outcome[9]) == 1) t.RetainedPaid++;
        }
        foreach (var witness in M.Parts((Relation)p[4]).OfType<Relation>())
        {
            if (!ReadWitness.TryRead(witness, out _, out _, out var result, out _, out _) ||
                result is not Relation { Left: CarrierLocation from, Right: CarrierLocation to } ||
                !ReferenceEquals(from.Carrier, M.Reservoir(root))) continue;
            var index = Array.FindIndex(consumers, consumer => ReferenceEquals(M.Wallet(consumer), to.Carrier));
            if (index >= 0) totals.Consumers[index].AdmittedFunding += to.Ticks;
        }
        if (quiet) totals.QuietObservations++;
        if (noContact) totals.NoContactAttemptObservations++;
    }

    // Deliberate loss of completed diagnostic evidence; promises still due on
    // the next observation and the entire operational graph remain connected.
    public static Relation ReleaseFrame(Relation root)
    {
        var p = M.Parts(root);
        return Relation.OrderedGroup(new Carrier(), p[0], p[1], new Placeholder(), p[3]);
    }

    public static object State(Relation root) => new
    {
        Version = M.Version(root), Reservoir = M.Reservoir(root).Value, Spent = M.Spent(root).Value,
        Wallets = M.Consumers(root).Select(M.Wallet).Select(c => new { c.Value, c.Tension }).ToArray(),
        Reserves = M.Producers(root).Select(M.Reserve).Select(c => new { c.Value, c.Tension }).ToArray(),
        Asks = M.Offers(root).Select(offer => M.Ask(offer).Value).ToArray(),
        OutstandingDemanded = M.Pending(root).Count(issue => Number(M.Parts(issue)[8]) == 1),
        OutstandingPaid = M.Pending(root).Count(issue => Number(M.Parts(issue)[10]) == 1)
    };

    private static long Number(IElement element) => ((CarrierLocation)element).Ticks;
}
