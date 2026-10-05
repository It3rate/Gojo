using Gojo.Core;
using S = LocalServiceGrowth.ServiceNetwork;

namespace LocalServiceGrowth;

// These bounded accumulators are observer output only. Native selection,
// construction, payment and support never read this report state.
internal sealed class BasisTotals
{
    public long PriorOpportunities { get; set; }
    public long Evaluated { get; set; }
    public long Unresolved { get; set; }
    public long PaidEvaluated { get; set; }
    public long BaselineError { get; set; }
    public long DeliveredError { get; set; }
    public long Gross { get; set; }
    public long PositivePaid { get; set; }
    public long ZeroPaid { get; set; }
    public long HarmfulPaid { get; set; }
}

internal sealed class ConsumerTotals
{
    public long Opportunities { get; set; }
    public long Purchases { get; set; }
    public long InspectedAdvertisements { get; set; }
    public long MaximumInspectedPerSearch { get; set; }
    public long IneligibleInspections { get; set; }
    public long UpstreamPurchases { get; set; }
    public long IntermediaryPurchases { get; set; }
    public long Fees { get; set; }
    public BasisTotals Coarse { get; set; } = new();
    public BasisTotals Fine { get; set; } = new();
}

internal sealed class ReportTotals
{
    public long Start { get; set; }
    public long End { get; set; }
    public long Admissions { get; set; }
    public ConsumerTotals[] Consumers { get; set; } = [new(), new()];
    public Dictionary<string, long> Phases { get; set; } = new();
    public long UpstreamInputFees { get; set; }
    public long SupplierOperatingExpense { get; set; }
    public long IntermediaryDirectExpense { get; set; }
    public long ContactExpense { get; set; }
    public long BuildExpense { get; set; }
    public long TrialExpense { get; set; }
    public long Promotions { get; set; }
    public long? FirstPromotion { get; set; }
    public long UpstreamDependencyObservations { get; set; }
    public long CandidateTrialOutcomes { get; set; }
    public long UnresolvedTrialOutcomes { get; set; }
    public bool Conserved { get; set; } = true;
    public long NativeAllocationBytes { get; set; }
    public double NativeWallMilliseconds { get; set; }
    public long ReportAllocationBytes { get; set; }
    public double ReportWallMilliseconds { get; set; }
}

internal static class Report
{
    public static void Add(ReportTotals totals, Relation root, Relation frame, string phase)
    {
        var f = S.Parts(frame);
        var services = S.Services(root);
        var consumers = S.Consumers(root);
        var providers = S.Providers(root);
        totals.End = S.Version(root);
        if (totals.Admissions == 0) totals.Start = totals.End;
        totals.Admissions++;
        totals.Phases[phase] = totals.Phases.GetValueOrDefault(phase) + 1;
        totals.Conserved &= S.Pass((Relation)f[6]);
        var outcomes = S.Parts((Relation)f[2]);
        var decisions = S.Parts((Relation)f[3]);
        for (var i = 0; i < consumers.Length; i++)
        {
            var t = totals.Consumers[i];
            t.Opportunities++;
            var decision = S.Parts((Relation)decisions[i]);
            var issue = S.Parts((Relation)decision[3]);
            var inspected = S.Parts((Relation)decision[2]);
            t.InspectedAdvertisements += inspected.Length;
            t.MaximumInspectedPerSearch = Math.Max(t.MaximumInspectedPerSearch, inspected.Length);
            t.IneligibleInspections += inspected.Cast<Relation>().Count(row => Number(S.Parts(row)[2]) == 0);
            if (Number(issue[7]) == 1)
            {
                t.Purchases++; t.Fees += Number(issue[6]);
                var service = S.RouteService((Relation)issue[3]);
                if (ReferenceEquals(service, services[0])) t.UpstreamPurchases++;
                else if (ReferenceEquals(service, services[1])) t.IntermediaryPurchases++;
                else throw new InvalidDataException("Paid issue has no supplied service correspondence.");
            }
            var outcome = S.Parts((Relation)outcomes[i]);
            if (outcome[0] is not Relation oldIssue) continue;
            var old = S.Parts(oldIssue);
            var interpretation = S.Parts((Relation)old[5]);
            var bounds = (Relation)interpretation[1];
            // Display grouping follows the captured native read interval, not
            // the current external demand phase (which may have changed).
            var b = Number(bounds.Right) == 1 ? t.Coarse : t.Fine;
            b.PriorOpportunities++;
            if (Number(outcome[5]) == 0) { b.Unresolved++; continue; }
            b.Evaluated++;
            b.BaselineError += Number(outcome[7]); b.DeliveredError += Number(outcome[8]);
            b.Gross += Number(outcome[3]);
            if (Number(old[7]) != 1) continue;
            b.PaidEvaluated++;
            var gross = Number(outcome[3]);
            if (gross > 0) b.PositivePaid++;
            else if (gross < 0) b.HarmfulPaid++;
            else b.ZeroPaid++;
        }
        var candidate = S.Parts((Relation)f[4]);
        var trialOutcome = S.Parts((Relation)candidate[0]);
        if (trialOutcome[0] is Relation)
        {
            if (Number(trialOutcome[5]) == 1) totals.CandidateTrialOutcomes++;
            else totals.UnresolvedTrialOutcomes++;
        }
        if (candidate[3] is Relation)
        {
            totals.Promotions++;
            totals.FirstPromotion ??= S.Version(root);
        }
        if (S.Attached(S.UpstreamRoute(root))) totals.UpstreamDependencyObservations++;
        if (S.Parts(root)[2] is Relation shadow)
            totals.TrialExpense += Number(S.Parts(shadow)[6]);
        foreach (var item in S.Parts((Relation)f[5]).OfType<Relation>())
        {
            var row = S.Parts(item);
            if (row.Length == 3 && ReferenceEquals(row[0], S.Candidate(root)) && row[1] is CarrierLocation investment)
                totals.BuildExpense += investment.Ticks;
            if (row.Length != 4 || row[0] is not Carrier from || row[1] is not Carrier to || row[2] is not CarrierLocation amount)
                continue;
            if (ReferenceEquals(from, S.Wallet(providers[1])) && ReferenceEquals(to, S.Wallet(providers[0])))
                totals.UpstreamInputFees += amount.Ticks;
            if (!ReferenceEquals(to, S.Spent(root))) continue;
            if (ReferenceEquals(from, S.Wallet(providers[0]))) totals.SupplierOperatingExpense += amount.Ticks;
            else if (ReferenceEquals(from, S.Wallet(providers[1]))) totals.IntermediaryDirectExpense += amount.Ticks;
            else if (consumers.Any(c => ReferenceEquals(from, S.Wallet(c)))) totals.ContactExpense += amount.Ticks;
        }
    }

    public static object State(Relation root)
    {
        var services = S.Services(root);
        var adverts = S.Advertisements(root);
        return new
        {
            Version = S.Version(root), Reservoir = S.Reservoir(root).Value, Spent = S.Spent(root).Value,
            Built = S.Built(root).Value, Promoted = S.Promoted(root).Value,
            CandidateSupport = S.SignedValue(S.Support(root)).Ticks,
            UpstreamAttached = S.Attached(S.UpstreamRoute(root)),
            ActualExpenses = new
            {
                Operating = S.OperatingExpense(root).Value,
                Contact = S.ContactExpense(root).Value,
                Construction = S.ConstructionExpense(root).Value,
                Trial = S.TrialExpense(root).Value
            },
            Services = services.Select((s, i) => new
            {
                Ask = S.Ask(s).Value, Omission = S.Omission(s).Ticks,
                ActiveConnections = LocalDiscovery.ActiveCount(adverts[i]).Value,
                QualifiedConnections = LocalDiscovery.SuccessfulCount(adverts[i]).Value,
                Occupancy = S.Occupancy(s).Value, Present = S.Presence(s).Ticks,
                Wallet = S.Wallet(S.Provider(s)).Value, Receipts = S.Receipts(S.Provider(s)).Value,
                Expenses = S.Expenses(S.Provider(s)).Value
            }).ToArray(),
            ConsumerWallets = S.Consumers(root).Select(c => S.Wallet(c).Value).ToArray(),
            OutstandingPaid = S.Parts((Relation)S.Parts(root)[1]).OfType<Relation>().Count(p => Number(S.Parts(p)[7]) == 1),
            Accounted = S.Pass(S.Conservation(root))
        };
    }

    private static long Number(IElement element) => ((CarrierLocation)element).Ticks;
}
