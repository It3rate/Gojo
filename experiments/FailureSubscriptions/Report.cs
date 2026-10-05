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
    public long MaximumActive { get; set; }
    public long Activations { get; set; }
    public long Renewals { get; set; }
    public long Cancellations { get; set; }
    public long Expiries { get; set; }
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
        totals.MaximumActive = Math.Max(totals.MaximumActive, routes.Count(S.Attached));
        foreach (var transition in S.Parts((Relation)f[4]).OfType<Relation>())
        {
            var p = S.Parts(transition);
            if (Number(p[3]) > 0) totals.Activations++;
            if (Number(p[4]) > 0) totals.Cancellations++;
            totals.Renewals += Number(p[5]);
            totals.Expiries += Number(p[6]);
        }
        var current = S.Parts((Relation)f[5]);
        if (current[4] is Relation chosen) totals.Sources[RouteIndex(chosen)].Selected++;
        foreach (var promise in S.Parts((Relation)current[5]).OfType<Relation>())
        {
            var p = S.Parts(promise);
            if (Number(p[3]) == 1) totals.Sources[RouteIndex((Relation)p[0])].Deliveries++;
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
