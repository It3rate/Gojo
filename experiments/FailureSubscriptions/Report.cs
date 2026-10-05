using Gojo.Core;
using S = FailureSubscriptions.SubscriptionNetwork;

namespace FailureSubscriptions;

// Observer-only accumulators. None is consulted by the native actors.
internal sealed class SourceTotals
{
    public long Deliveries { get; set; }
    public long Evaluated { get; set; }
    public long Unknown { get; set; }
    public long Helpful { get; set; }
    public long Neutral { get; set; }
    public long Harmful { get; set; }
    public long ConditionalRelief { get; set; }
    public long Selected { get; set; }
}

internal sealed class ReportTotals
{
    public long Start { get; set; }
    public long End { get; set; }
    public long Admissions { get; set; }
    public long PriorOpportunities { get; set; }
    public long Evaluated { get; set; }
    public long Unknown { get; set; }
    public long SelfError { get; set; }
    public long DeliveredError { get; set; }
    public long DeliveredRelief { get; set; }
    public long SelfDirectionMatches { get; set; }
    public long DeliveredDirectionMatches { get; set; }
    public long Inspections { get; set; }
    public long MaximumInspected { get; set; }
    public long MaximumVisited { get; set; }
    public long MaximumActive { get; set; }
    public long Activations { get; set; }
    public long Renewals { get; set; }
    public long Cancellations { get; set; }
    public long Expiries { get; set; }
    public long SelfSelections { get; set; }
    public long ShadowDeliveries { get; set; }
    public long ShadowDeliveryFees { get; set; }
    public long NonpositiveScoredDeliveries { get; set; }
    public long SuppressedShopping { get; set; }
    public long ShortenedOffers { get; set; }
    public long UnavailableDurationOffers { get; set; }
    public long AbandonedOpportunities { get; set; }
    public long QuotedCommitmentCost { get; set; }
    public Dictionary<long, long> AcceptedHorizons { get; set; } = new();
    public Dictionary<long, long> AdvertisedHorizons { get; set; } = new();
    public Dictionary<long, long> OfferedHorizons { get; set; } = new();
    public Dictionary<long, long> BackedHorizons { get; set; } = new();
    public long CheckpointInspections { get; set; }
    public long PeakRetainedCarriers { get; set; }
    public long PeakRetainedRelations { get; set; }
    public SourceTotals[] Sources { get; set; } = [new(), new(), new(), new()];
    public Dictionary<string, long> Phases { get; set; } = new();
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
        var routes = S.Routes(root);
        totals.End = S.Version(root);
        if (totals.Admissions == 0) totals.Start = totals.End;
        totals.Admissions++;
        totals.Phases[phase] = totals.Phases.GetValueOrDefault(phase) + 1;
        totals.Conserved &= S.Pass((Relation)f[7]);
        var inspected = S.Parts((Relation)f[3]).Length;
        totals.Inspections += inspected;
        totals.MaximumInspected = Math.Max(totals.MaximumInspected, inspected);
        var suppressed = f.Length > 8 ? S.Parts((Relation)f[8]).Length : 0;
        totals.SuppressedShopping += suppressed;
        totals.MaximumVisited = Math.Max(totals.MaximumVisited, inspected + suppressed);
        totals.MaximumActive = Math.Max(totals.MaximumActive, routes.Count(S.Attached));
        foreach (var advertisement in S.Parts((Relation)f[3]).OfType<Relation>())
        {
            var advertised = S.Parts(advertisement);
            if (advertised.Length <= 7 || advertised[7] is not Relation duration) continue;
            Increment(totals.AdvertisedHorizons, DurationOffer.Offered(duration).Ticks);
            if (DurationOffer.Accepted(duration).Ticks == 0) totals.UnavailableDurationOffers++;
        }
        foreach (var transition in S.Parts((Relation)f[4]).OfType<Relation>())
        {
            var p = S.Parts(transition);
            if (Number(p[3]) > 0) totals.Activations++;
            if (Number(p[4]) > 0) totals.Cancellations++;
            totals.Renewals += Number(p[5]);
            totals.Expiries += Number(p[6]);
            if (p[2] is Relation accepted)
            {
                var term = S.Parts(accepted);
                Increment(totals.AcceptedHorizons, Number(term[5]));
                if (term.Length > 9 && term[9] is Relation quote)
                {
                    Increment(totals.OfferedHorizons, DurationOffer.Offered(quote).Ticks);
                    Increment(totals.BackedHorizons, DurationOffer.Backed(quote).Ticks);
                    if (Number(term[5]) < DurationOffer.Offered(quote).Ticks) totals.ShortenedOffers++;
                    totals.QuotedCommitmentCost += DurationOffer.TotalFee(quote).Ticks;
                }
                else totals.QuotedCommitmentCost += Number(term[7]) + Number(term[6]) * Number(term[5]);
            }
            if (Number(p[4]) > 0 && p[1] is Relation canceled)
                totals.AbandonedOpportunities += Math.Max(0, Number(S.Parts(canceled)[2]) - totals.End);
        }
        var current = S.Parts((Relation)f[5]);
        if (current[4] is Relation chosen) totals.Sources[RouteIndex(chosen)].Selected++;
        else if (current[3] is CarrierLocation) totals.SelfSelections++;
        foreach (var promise in S.Parts((Relation)current[5]).OfType<Relation>())
        {
            var p = S.Parts(promise);
            if (Number(p[3]) == 1)
            {
                totals.Sources[RouteIndex((Relation)p[0])].Deliveries++;
                if (Number(p[4]) <= 0) totals.NonpositiveScoredDeliveries++;
                if (!ReferenceEquals(current[4], p[0]))
                {
                    totals.ShadowDeliveries++;
                    totals.ShadowDeliveryFees += Number(S.Parts((Relation)p[2])[6]);
                }
            }
        }
        var outcome = S.Parts((Relation)f[2]);
        if (outcome[0] is Relation oldIssue)
        {
            totals.PriorOpportunities++;
            if (Number(outcome[7]) == 1)
            {
                totals.Evaluated++;
                totals.SelfError += Number(outcome[2]);
                totals.DeliveredError += Number(outcome[3]);
                totals.DeliveredRelief += Number(outcome[4]);
                // Display-only direction diagnostics over already captured
                // native forecasts and actuals. They never select a service.
                var old = S.Parts(oldIssue);
                var actualDirection = Math.Sign(Number(outcome[1]));
                if (Math.Sign(Number(old[2])) == actualDirection) totals.SelfDirectionMatches++;
                if (Math.Sign(Number(old[3])) == actualDirection) totals.DeliveredDirectionMatches++;
            }
            else totals.Unknown++;
        }
        foreach (var response in S.Parts((Relation)outcome[6]).OfType<Relation>())
        {
            var p = S.Parts(response);
            var promised = S.Parts((Relation)p[0]);
            var source = totals.Sources[RouteIndex((Relation)promised[0])];
            if (Number(p[3]) == 0) { source.Unknown++; continue; }
            source.Evaluated++;
            var relief = Number(p[2]);
            source.ConditionalRelief += relief;
            if (relief > 0) source.Helpful++;
            else if (relief < 0) source.Harmful++;
            else source.Neutral++;
        }

        int RouteIndex(Relation route)
        {
            var index = Array.FindIndex(services, s => ReferenceEquals(s, S.RouteService(route)));
            return index >= 0 ? index : throw new InvalidDataException("Unknown supplied service in native evidence.");
        }
    }

    private static void Increment(Dictionary<long, long> counts, long value) => counts[value] = counts.GetValueOrDefault(value) + 1;

    public static object State(Relation root) => new
    {
        Version = S.Version(root), Reservoir = S.Reservoir(root).Value, Spent = S.Spent(root).Value,
        SeekerWallet = S.Wallet(S.Seeker(root)).Value,
        Expenses = new
        {
            Inspection = S.InspectionExpense(root).Value, Activation = S.ActivationExpense(root).Value,
            Cancellation = S.CancellationExpense(root).Value, Calculation = S.CalculationExpense(root).Value,
            Self = S.SelfExpense(root).Value
        },
        Activations = S.Activations(root).Value, Renewals = S.Renewals(root).Value,
        Cancellations = S.Cancellations(root).Value, Deliveries = S.Deliveries(root).Value, Expiries = S.Expiries(root).Value,
        Routes = S.Routes(root).Select(r => new
        {
            Active = S.Attached(r), Remaining = S.SignedValue(S.Remaining(r)).Ticks,
            TermRelief = S.SignedValue(S.TermRelief(r)).Ticks, LastRelief = S.SignedValue(S.LastRelief(r)).Ticks,
            Misses = S.SignedValue(S.MissCount(r)).Ticks, Affinity = S.SignedValue(S.Affinity(r)).Ticks,
            ContextAffinities = S.Affinities(r).Select(a => S.SignedValue(a).Ticks).ToArray(),
            Present = S.SourcePresence(S.RouteService(r)).Ticks,
            Fees = S.FeesReceived(S.RouteService(r)).Value,
            LocalReviews = S.LocalReviews(r).Select(review => new
            {
                Resolved = S.ReviewCount(review).Value,
                Samples = S.ReviewSamples(review).Select(s => S.SignedValue(s).Ticks).ToArray(),
                RetryAfter = S.RetryAfter(review).Ticks
            }).ToArray(),
            DurationProfiles = S.Parts((Relation)S.Parts(S.RouteService(r))[8]).Cast<Relation>().Select(state =>
                DurationOffer.Rows(state).Select(row => new
                {
                    Helpful = DurationOffer.Helpful(row).Value,
                    Evaluated = DurationOffer.Evaluated(row).Value,
                    Unknown = DurationOffer.Unknown(row).Value,
                    Relief = S.SignedValue(DurationOffer.Benefit(row)).Ticks,
                    RetryAfter = S.SignedValue(DurationOffer.RetryAfter(row)).Ticks
                }).ToArray()).ToArray(),
            Prospects = S.ProspectStates(S.RouteService(r)).Select(p => new
            {
                Helpful = ProspectiveOffer.Helpful(p).Value,
                Evaluated = ProspectiveOffer.Evaluated(p).Value,
                Unknown = ProspectiveOffer.Unknown(p).Value,
                SignedRelief = S.SignedValue(ProspectiveOffer.Benefit(p)).Ticks
            }).ToArray()
        }).ToArray(),
        Accounted = S.Pass(S.Conservation(root))
    };

    private static long Number(IElement item) => ((CarrierLocation)item).Ticks;
}
