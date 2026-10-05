using System.Text.Json;
using Gojo.Core;
using Gojo.Structures;
using N = ConnectionAssembly.NativeMath;
using S = FailureSubscriptions.SubscriptionNetwork;

namespace FailureSubscriptions;

/// <summary>Local causal and accounting contracts. These short authored cases
/// verify the supplied rules; no campaign outcome is required to win.</summary>
internal static class Checks
{
    public static int Run()
    {
        SelfOnlyAndQuietSuccess();
        PaidForecastsAndLivePorts();
        ActualExportOwnsTheTrigger();
        FixedExpiryThroughUnknowns();
        EarlyCancellationAndNaturalExpiry();
        ConditionalEvidenceAndDormantIdentity();
        BoundedSearchAndConcurrency();
        AccountingAndObserverBoundary();
        NativeContinuation();
        FinishedWitnessRelease();
        return 10;
    }

    private static void SelfOnlyAndQuietSuccess()
    {
        var root = S.Create(horizon: 0);
        foreach (var input in Inputs())
        {
            var frame = Observe(ref root, input);
            Require(S.Routes(root).All(route => !S.Attached(route)) && S.Parts((Relation)S.Parts(frame)[3]).Length == 0 &&
                Total(S.InspectionExpense(root)) == 0 && Total(S.ActivationExpense(root)) == 0 &&
                Total(S.CalculationExpense(root)) == 0 && Total(S.CancellationExpense(root)) == 0,
                "The no-help control cannot discover, subscribe, compute paid source outputs or cancel regardless of its own errors.");
            var current = Issue(root);
            if (S.Parts(current)[2] is CarrierLocation self)
                Require(Number(current, 3) == self.Ticks && S.Parts(current)[4] is Placeholder,
                    "The no-help export is the captured self-prediction, not an uncharged outside answer.");
        }
        Require(Total(S.SelfExpense(root)) > 0 && Total(S.Spent(root)) == Total(S.SelfExpense(root)),
            "The incumbent's own successful or unsuccessful calculations are charged in the control.");

        var quiet = S.Create();
        foreach (var input in new long[] { 100, 103, 106, 109, 112, 115, 118, 121 }) Observe(ref quiet, input);
        Require(Total(S.InspectionExpense(quiet)) == 0 && Total(S.Activations(quiet)) == 0 &&
            S.Routes(quiet).All(route => !S.Attached(route)),
            "A stable correct incumbent does not pay to shop merely because another input opportunity arrived.");
    }

    private static void PaidForecastsAndLivePorts()
    {
        var root = S.Create(horizon: 5, useProspects: false);
        Observe(ref root, 100);
        Observe(ref root, 103);
        Require(S.Services(root).All(service => S.SourcePresence(service).Ticks == 0),
            "Unsubscribed current recipes do not masquerade as refreshed paid source publications.");
        var frame = Observe(ref root, 100);
        var active = S.Routes(root).Where(S.Attached).ToArray();
        Require(active.Length == 2 && Total(S.InspectionExpense(root)) == 2 &&
            Total(S.ActivationExpense(root)) == 2 && Total(S.CalculationExpense(root)) == 2 && Total(S.Deliveries(root)) == 2,
            "A first eligible failure pays for two bounded inspections, two activations and two actual source calculations.");
        Require(S.Services(root).Sum(service => Total(S.FeesReceived(service))) == 4,
            "Two deliveries transfer their full two-credit fees, separately from work expense.");
        var issue = (Relation)S.Parts(frame)[5];
        var frozenExport = Number(issue, 3);
        var frozenSelf = Number(issue, 2);
        var promises = SourcePromises(issue);
        Require(promises.Count(p => Number(p, 3) == 1) == 2 &&
            promises.Where(p => Number(p, 3) == 0).All(p => S.Parts(p)[1] is Placeholder) &&
            S.Parts((Relation)S.Parts(issue)[5]).Count(p => p is Placeholder) == 2,
            "Only paid deliveries capture forecasts; absent candidates do not receive hindsight predictions.");
        var route = active[0];
        var service = S.RouteService(route);
        var port = S.SourcePort(service);
        var original = port.Ticks;
        var paidPromise = promises.Single(p => ReferenceEquals(S.Parts(p)[0], route));
        var promised = Number(paidPromise, 1);
        port.Tick.Set(7);
        S.Receipt(route).Settle();
        Require(Total(S.Receipt(route)) == 7 && Number(paidPromise, 1) == promised &&
            Number(issue, 3) == frozenExport && Number(issue, 2) == frozenSelf,
            "The real paid receipt follows its exposed site while already committed source and exported forecasts remain frozen.");
        port.Tick.Set(original);
        S.Receipt(route).Settle();
        Require(Total(S.Receipt(route)) == original && S.Pass(S.Conservation(root)),
            "A diagnostic source perturbation cannot charge money or alter a settled forecast contract.");
    }

    private static void ActualExportOwnsTheTrigger()
    {
        var root = S.Create(horizon: 5, useProspects: false);
        foreach (var input in new long[] { 100, 103, 100, 103 }) Observe(ref root, input);
        var previous = Issue(root);
        Require(Number(previous, 2) == 3 && Number(previous, 3) == -3 &&
            S.Parts(previous)[4] is Relation chosen && ReferenceEquals(S.RouteService(chosen), S.Services(root)[1]),
            "The explicitly alternating fixture commits the previously useful inverse before its next target arrives.");
        var inspectedBefore = Total(S.InspectionExpense(root));
        var frame = Observe(ref root, 100);
        var outcome = (Relation)S.Parts(frame)[2];
        Require(ReferenceEquals(S.Parts(outcome)[0], previous) && Number(outcome, 2) == 6 &&
            Number(outcome, 3) == 0 && Number(outcome, 4) == 6 && Number(outcome, 5) == 0,
            "Later evaluation distinguishes a wrong self-prediction from a correct actual export.");
        Require(S.Parts((Relation)S.Parts(frame)[3]).Length == 0 && Total(S.InspectionExpense(root)) == inspectedBefore,
            "A successful assisted export cannot trigger shopping because the unused private baseline would have missed.");
    }

    private static void FixedExpiryThroughUnknowns()
    {
        var root = S.Create(horizon: 5, useProspects: true);
        foreach (var input in new long[] { 100, 103, 100 }) Observe(ref root, input);
        var routes = S.Routes(root).Where(S.Attached).ToArray();
        var contracts = routes.Select(route => S.Contract(root, route)!).ToArray();
        var quotes = contracts.Select(contract => (Relation)S.Parts(contract)[4]).ToArray();
        var initialState = routes.SelectMany(route => new[] { Total(S.TermRelief(route)), Total(S.LastRelief(route)), Total(S.MissCount(route)), Total(S.Affinity(route)) }).ToArray();
        Require(routes.Length == 2 && routes.All(route => Total(S.Remaining(route)) == 5) &&
            contracts.All(contract => Number(contract, 2) - Number(contract, 1) == 5),
            "The accepted contract carries an ordered five-opportunity interval and full current remaining span.");
        var paidBefore = Total(S.CalculationExpense(root));
        for (var elapsed = 1; elapsed <= 5; elapsed++)
        {
            var frame = Observe(ref root, null);
            Require(Number((Relation)S.Parts(frame)[2], 7) == 0 &&
                routes.All(route => Total(S.Remaining(route)) == 5 - elapsed) &&
                S.Services(root).All(service => S.SourcePresence(service).Ticks == 0) &&
                routes.All(route => S.Gate(route).Value == 0 && Total(S.Receipt(route)) == 0),
                "Unknown input consumes the fixed calendar opportunity while closing current data and preserving an unknown verdict.");
            if (elapsed < 5)
                Require(routes.All(S.Attached) && initialState.SequenceEqual(routes.SelectMany(route => new[] {
                    Total(S.TermRelief(route)), Total(S.LastRelief(route)), Total(S.MissCount(route)), Total(S.Affinity(route)) })),
                    "Unresolved outcomes cannot supply evidence for early cancellation, support decay or a repaired forecast.");
        }
        Require(routes.All(route => !S.Attached(route)) && Total(S.CancellationExpense(root)) == 0 &&
            Total(S.Cancellations(root)) == 0 && Total(S.Expiries(root)) == 2 && Total(S.CalculationExpense(root)) == paidBefore,
            "Both contracts naturally expire despite missing input, without cancellation charges or fictitious production.");
        Require(quotes.All(quote => !ProspectiveOffer.TryExpected(quote, out _)),
            "The cold quote captured at acceptance does not retroactively acquire later information.");
        var resumed = Observe(ref root, 103);
        Require(Number((Relation)S.Parts(resumed)[2], 7) == 0 && S.Services(root).All(service => S.SourcePresence(service).Ticks == 0),
            "A later known sample cannot fill any of the five missing target observations.");
    }

    private static void EarlyCancellationAndNaturalExpiry()
    {
        var root = S.Create(horizon: 5, useProspects: false);
        foreach (var input in new long[] { 100, 103, 100 }) Observe(ref root, input);
        var duplicate = S.Routes(root)[0];
        var contract = S.Contract(root, duplicate)!;
        Observe(ref root, 103);
        Require(S.Attached(duplicate) && Total(S.MissCount(duplicate)) == 1 && Total(S.CancellationExpense(root)) == 0,
            "The first resolved nonpositive marginal outcome does not instantly cancel a five-opportunity trial.");
        var paidFees = Total(S.FeesReceived(S.RouteService(duplicate)));
        var spent = Total(S.Spent(root));
        Observe(ref root, 100);
        Require(!S.Attached(duplicate) && Total(S.Remaining(duplicate)) == 0 &&
            S.Version(root) < Number(contract, 2) && Total(S.Cancellations(root)) == 1 && Total(S.CancellationExpense(root)) == 1,
            "Two resolved nonpositive improvements cancel early and pay exactly the stated cancellation fee.");
        Require(Total(S.FeesReceived(S.RouteService(duplicate))) == paidFees && Total(S.Spent(root)) > spent && S.Pass(S.Conservation(root)),
            "Cancellation neither refunds completed source work nor erases already transferred fees.");

        var shortRoot = S.Create(horizon: 1, useProspects: false);
        foreach (var input in new long[] { 100, 103, 100, 103 }) Observe(ref shortRoot, input);
        Require(Total(S.Expiries(shortRoot)) >= 1 && Total(S.CancellationExpense(shortRoot)) == 0 && Total(S.Cancellations(shortRoot)) == 0,
            "A one-opportunity term ends naturally; expiry cannot incur an early-termination charge.");

        var scarce = S.Create(horizon: 5, useProspects: false);
        foreach (var input in new long[] { 100, 103, 100, 103 }) Observe(ref scarce, input);
        var unpaidCancellation = S.Routes(scarce)[0];
        // Keep enough to calculate the next self-prediction, but no currency
        // for cancellation. These authored native transfers preserve supply.
        Move(S.Wallet(S.Seeker(scarce)), S.Reservoir(scarce), Total(S.Wallet(S.Seeker(scarce))) - 1);
        Move(S.Reservoir(scarce), S.Wallet(S.Services(scarce)[3]), Total(S.Reservoir(scarce)));
        Observe(ref scarce, 100);
        Require(S.Attached(unpaidCancellation) && Total(S.Remaining(unpaidCancellation)) == 3 &&
            Total(S.CancellationExpense(scarce)) == 0 && Total(S.Cancellations(scarce)) == 0 &&
            S.Gate(unpaidCancellation).Value == 0 && S.Pass(S.Conservation(scarce)),
            "An unaffordable early-termination fee preserves the outstanding contract rather than granting a free cancellation or unpaid data.");

        var costly = S.Create(horizon: 5, useProspects: false);
        var costlyService = S.Services(costly)[0];
        ((CarrierLocation)S.Parts(costlyService)[4]).Tick.Set(1000);
        var sourceWallet = Total(S.Wallet(costlyService));
        foreach (var input in new long[] { 100, 103, 100 }) Observe(ref costly, input);
        Require(S.SourcePresence(costlyService).Ticks == 0 && Total(S.FeesReceived(costlyService)) == 0 &&
            Total(S.Wallet(costlyService)) == sourceWallet && S.Gate(S.Routes(costly)[0]).Value == 0 &&
            Total(S.CalculationExpense(costly)) == 1 && S.Pass(S.Conservation(costly)),
            "Work costing more than the provider's available funds plus the incoming fee stays unavailable, without charging a partial sale before a failed calculation.");
    }

    private static void ConditionalEvidenceAndDormantIdentity()
    {
        var root = S.Create(horizon: 5, useProspects: false);
        foreach (var input in new long[] { 100, 103, 100 }) Observe(ref root, input);
        var duplicate = S.Routes(root)[0];
        var inverse = S.Routes(root)[1];
        var publication = S.Publication(inverse);
        var contract = S.Contract(root, inverse)!;
        var quote = (Relation)S.Parts(contract)[4];
        var cohort = (Relation)S.Parts(quote)[0];
        var duplicateCohort = (Relation)S.Parts((Relation)S.Parts(S.Contract(root, duplicate)!)[4])[0];
        Require(ProspectiveOffer.Evaluated(cohort).Value == 0 && ProspectiveOffer.Helpful(cohort).Value == 0,
            "Subscription acceptance and its live output do not themselves establish empirical usefulness.");
        Observe(ref root, 103);
        Require(ProspectiveOffer.Evaluated(cohort).Value == 1 && ProspectiveOffer.Helpful(cohort).Value == 1 &&
            Total(ProspectiveOffer.Benefit(cohort)) == 6 && !ProspectiveOffer.TryExpected(quote, out _),
            "Only the later outcome updates the initiating failure cohort, leaving the originally accepted cold quote unchanged.");
        Require(ProspectiveOffer.Evaluated(duplicateCohort).Value == 1 && ProspectiveOffer.Helpful(duplicateCohort).Value == 0 &&
            Total(ProspectiveOffer.Benefit(duplicateCohort)) == 0,
            "An exact copy of the private baseline cannot claim marginal gain just because its signal is used or paid.");
        foreach (var input in new long[] { 100, 103, 100, 103, 100, 103, 100, 103, 100 }) Observe(ref root, input);
        var affinity = Total(S.Affinity(inverse));
        Require(Total(S.Renewals(root)) > 0 && affinity > 0 && ReferenceEquals(S.Publication(inverse), publication),
            "Repeated useful terms can renew and strengthen the same native connection without reconstructing its recipe.");
        foreach (var input in new long[] { 103, 106, 109, 112, 115 }) Observe(ref root, input);
        Require(Total(S.Affinity(inverse)) < affinity && !S.Attached(inverse),
            "Poor subsequent opportunities oppose accumulated connection support and release the previously useful source.");
        Require(ReferenceEquals(S.Routes(root)[1], inverse) && ReferenceEquals(S.Publication(inverse), publication),
            "Dormancy preserves the original route/publication identity without retaining a current active output.");
    }

    private static void BoundedSearchAndConcurrency()
    {
        var root = S.Create(neighborRotation: 1);
        var seenSearch = false;
        foreach (var input in Inputs())
        {
            var frame = Observe(ref root, input);
            var inspected = S.Parts((Relation)S.Parts(frame)[3]);
            seenSearch |= inspected.Length > 0;
            Require(inspected.Length <= 2 && S.Routes(root).Count(S.Attached) <= 2,
                "Each failure admission respects both the two-advertisement read budget and two active subscriptions.");
            if (Number((Relation)S.Parts(frame)[2], 7) == 0 || Number((Relation)S.Parts(frame)[2], 5) == 0)
                Require(inspected.Length == 0, "Missing or successful actual exports do not trigger unrelated new-source shopping.");
            foreach (var route in S.Routes(root).Where(S.Attached))
                Require(S.Contract(root, route) is Relation contract && ReferenceEquals(S.Parts(contract)[0], route) && Total(S.Remaining(route)) > 0,
                    "Every active subscription has an attached contract for this exact route and a remaining future opportunity.");
        }
        Require(seenSearch, "The bounded-search contract is exercised on actual failures rather than only quiet input.");
    }

    private static void AccountingAndObserverBoundary()
    {
        var root = S.Create();
        var totals = new ReportTotals();
        foreach (var input in Inputs())
        {
            var frame = Observe(ref root, input);
            var before = Canonical(root);
            Report.Add(totals, root, frame, "contract-check");
            Require(before == Canonical(root), "Observer aggregation cannot alter native selection, accounts or forecasts.");
            Require(S.Pass(S.Conservation(root)) && Total(S.InspectionExpense(root)) + Total(S.ActivationExpense(root)) +
                Total(S.CalculationExpense(root)) + Total(S.CancellationExpense(root)) + Total(S.SelfExpense(root)) == Total(S.Spent(root)),
                "The finite supply and five distinct native expenditure categories reconcile without counting source fees as extra system cost.");
            Require(S.Services(root).Sum(service => Total(S.FeesReceived(service))) == 2 * Total(S.Deliveries(root)),
                "Every delivered source calculation has exactly one full service fee, without double payment for its observed outcome.");
        }
        Require(totals.Conserved && totals.Admissions == Inputs().Length && totals.PriorOpportunities == Inputs().Length - 1 &&
            totals.Evaluated + totals.Unknown == totals.PriorOpportunities && totals.DeliveredRelief == totals.SelfError - totals.DeliveredError,
            "The report retains unresolved opportunities and measures improvement of the single actual export against the same frozen baseline.");
        Require(totals.Sources.Sum(source => source.Deliveries) == Total(S.Deliveries(root)) &&
            totals.Inspections == Total(S.InspectionExpense(root)) && totals.Cancellations == Total(S.Cancellations(root)) &&
            totals.Activations == Total(S.Activations(root)) && totals.Renewals == Total(S.Renewals(root)) &&
            totals.Expiries == Total(S.Expiries(root)) && totals.MaximumInspected <= 2 && totals.MaximumActive <= 2,
            "Observer event counts agree with native cumulative accounts and do not sum individual hypothetical relief into actual utility.");
    }

    private static void NativeContinuation()
    {
        var root = S.Create(neighborRotation: 1);
        foreach (var input in new long?[] { 100, 103, 100, 103, null }) Observe(ref root, input);
        root = S.RetainContinuation(root);
        var clone = (Relation)GojoNumericFormat.Decode(GojoNumericFormat.Encode(root)).Single();
        Require(Canonical(root) == Canonical(clone),
            "Native transport preserves active interval endpoints, frozen quotes, conditional evidence and unresolved pending delivery.");
        foreach (var input in new long?[] { 106, 103, 106, 103, 100, null, 103, 106, 109, 112, 112, 112 })
        {
            Observe(ref root, input);
            Observe(ref clone, input);
            root = S.RetainContinuation(root);
            clone = S.RetainContinuation(clone);
            Require(Canonical(root) == Canonical(clone),
                "Reloaded contracts expire, cancel, reactivate and calculate the same future native structure as the uninterrupted world.");
        }
    }

    private static void FinishedWitnessRelease()
    {
        var kept = S.Create();
        var released = S.Create();
        var initialRoutes = S.Routes(released);
        foreach (var input in Inputs())
        {
            Observe(ref kept, input);
            var frame = Observe(ref released, input);
            var pending = Issue(released);
            released = S.RetainContinuation(released);
            var graph = GojoEncodedSnapshot.CaptureGraph(released);
            Require(S.Parts(released)[3] is Placeholder && !graph.Relations.Any(row => row.Id == frame.Id) &&
                graph.Relations.Any(row => row.Id == pending.Id) &&
                initialRoutes.Zip(S.Routes(released)).All(pair => ReferenceEquals(pair.First, pair.Second)),
                "Releasing the finished frame preserves the mutable routes and next-outcome promise, without archiving each completed observation.");
            Require(Canonical(S.RetainContinuation(kept)) == Canonical(released),
                "Discarding completed diagnostic evidence cannot change the continued native behavior.");
        }
    }

    private static long?[] Inputs() => [100, 103, 100, 103, 100, 103, 106, 109, null, 112, 112, 109, 112, 109, 112, 115];
    private static Relation Issue(Relation root) => (Relation)S.Parts(root)[1];
    private static Relation[] SourcePromises(Relation issue) => S.Parts((Relation)S.Parts(issue)[5]).OfType<Relation>().ToArray();
    private static Relation Observe(ref Relation root, long? input)
    {
        Require(S.TryObserve(root, input, out var next, out var frame), "The current native world admits its next input opportunity.");
        root = next;
        return frame;
    }
    private static long Number(Relation row, int index) => ((CarrierLocation)S.Parts(row)[index]).Ticks;
    private static long Total(Carrier carrier) => S.SignedValue(carrier).Ticks;
    private static void Move(Carrier from, Carrier to, long amount)
    {
        var calibration = new Relation(Relation.Segment(from, 0, 1), Relation.Segment(to, 0, 1));
        var request = RefinementTransfer.Create(new Carrier(), calibration, new CarrierLocation(from, amount));
        Require(RefinementTransfer.TryApply(request, out _), "The authored scarcity fixture uses a real conserved transfer.");
    }
    internal static string Canonical(Relation root)
    {
        var graph = GojoEncodedSnapshot.CaptureGraph(root);
        return JsonSerializer.Serialize(graph with
        {
            Carriers = graph.Carriers.Select(row => row with { Id = 0 }).ToArray(),
            TickRefs = graph.TickRefs.Select(row => row with { Id = 0 }).ToArray(),
            Relations = graph.Relations.Select(row => row with { Id = 0 }).ToArray(),
            UnitShapes = graph.UnitShapes.Select(row => row with { Id = 0 }).ToArray()
        });
    }
    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException("Failure-subscription integration check failed: " + message);
    }
}
