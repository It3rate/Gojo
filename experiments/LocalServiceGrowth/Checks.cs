using System.Text.Json;
using Gojo.Core;
using N = ConnectionAssembly.NativeMath;
using S = LocalServiceGrowth.ServiceNetwork;

namespace LocalServiceGrowth;

/// <summary>Small structural contract checks. The supplied monotonic cases
/// exercise accounting and causal laws, not a required winner in the campaign.</summary>
internal static class Checks
{
    public static int Run()
    {
        PaidLiveDependency();
        ConstructionAndDelayedPromotion();
        IndependentReplacement();
        CompatibilityAndDirectAccess();
        PresenceAndMissingOutcome();
        AccountedCostsAndPublicHints();
        BoundedAdvertisementEvidence();
        ObserverAccounting();
        NativeContinuation();
        FinishedWitnessRelease();
        return 10;
    }

    private static void PaidLiveDependency()
    {
        var root = S.Create(learning: false, supportWeight: 0);
        Observe(ref root, 100);
        Observe(ref root, 103);
        var services = S.Services(root);
        var upstream = S.UpstreamRoute(root);
        var intermediary = S.Providers(root)[1];
        var consumer = S.Consumers(root)[0];
        var selected = S.SelectedRoute(root, consumer);
        Require(selected is not null && ReferenceEquals(S.RouteService(selected), services[1]) && S.Attached(upstream),
            "The restricted client buys the intermediary, whose actual source purchase opens its upstream route.");
        Require(Total(S.Receipts(S.Providers(root)[0])) >= 4 && Total(S.Expenses(intermediary)) >= 5 &&
            Total(S.Receipts(intermediary)) >= 6 && S.Gate(upstream).Value == 1,
            "The consumer-to-producer chain incurs an input payment and work, then receives a downstream fee.");
        var promise = Promise(root, 0);
        var promised = Number(promise, 4);
        var port = S.Port(services[0]);
        var original = port.Ticks;
        port.Tick.Set(-3);
        S.Receipt(upstream).Settle();
        Require(Total(S.Receipt(upstream)) == -3,
            "The paid upstream receipt depends on the supplier's actual exposed site, including signed change.");
        Require(S.Value(S.ReadProduction(root, services[1])).Ticks == -1,
            "Re-reading the intermediary's actual production structure follows the changed paid input through its clamp.");
        Require(Number(promise, 4) == promised && promised == 1,
            "Live source perturbation cannot rewrite the already issued downstream forecast.");
        port.Tick.Set(original);
        S.Receipt(upstream).Settle();
        Require(Total(S.Receipt(upstream)) == 3 && S.Pass(S.Conservation(root)),
            "Restoring the current port restores live input without moving any money.");
    }

    private static void ConstructionAndDelayedPromotion()
    {
        var root = S.Create(learning: true, supportWeight: 0);
        var control = S.Create(learning: false, supportWeight: 0);
        Observe(ref root, 100);
        Observe(ref control, 100);
        Require(S.Built(root).Value == 0 && S.CandidateRecipe(root) is null,
            "A single level cannot justify a difference recipe or charge a ready shadow calculation.");
        Observe(ref root, 103);
        Observe(ref control, 103);
        Require(S.Built(root).Value == 1 && S.CandidateRecipe(root) is not null &&
            !ReferenceEquals(S.CandidateRecipe(root), S.Parts(S.Services(root)[0])[8]),
            "The paid candidate is its own relation recipe, not the source's current result or source recipe identity.");
        Require(Total(S.Spent(root)) - Total(S.Spent(control)) == 13,
            "The same first paid opportunity additionally incurs construction 12 and shadow work 1.");
        Require(Total(S.Support(root)) == 0 && S.Promoted(root).Value == 0 && S.Parts(root)[2] is Relation,
            "Paid construction and a current prediction supply a pending promise, not premature outcome support.");
        foreach (var input in new long[] { 106, 109, 112 })
        {
            var before = Total(S.Support(root));
            var frame = Observe(ref root, input);
            var result = (Relation)S.Parts(frame)[4];
            Require(Number((Relation)S.Parts(result)[0], 5) == 1 && Total(S.Support(root)) == before + 1 &&
                S.Promoted(root).Value == 0 && S.Parts(result)[3] is Placeholder,
                "Each later useful outcome supplies one support step without promoting before the four-step threshold.");
        }
        var promotedFrame = Observe(ref root, 115);
        Require(Total(S.Support(root)) == 4 && S.Promoted(root).Value == 1 &&
            S.Parts((Relation)S.Parts(promotedFrame)[4])[3] is Relation && S.Ask(S.Services(root)[1]).Value == 2,
            "The fourth delayed supporting observation and cheaper input produce an inspectable promotion.");
        Require(S.Pass(S.Conservation(root)), "Construction, shadow work and promotion do not create spendable resources.");
    }

    private static void IndependentReplacement()
    {
        var root = S.Create(supportWeight: 0);
        foreach (var input in new long[] { 100, 103, 106, 109, 112, 115 }) Observe(ref root, input);
        var services = S.Services(root);
        var recipe = S.CandidateRecipe(root)!;
        Require(S.Promoted(root).Value == 1 && !S.Attached(S.UpstreamRoute(root)) &&
            S.Gate(S.UpstreamRoute(root)).Value == 0 && Total(S.Receipt(S.UpstreamRoute(root))) == 0,
            "Promotion closes and withdraws the old paid dependency; its last delivered level is cleared.");
        var prediction = S.Value(N.Read(recipe)).Ticks;
        var delivered = S.Port(services[1]).Ticks;
        S.Port(services[0]).Tick.Set(-7);
        S.Presence(services[0]).Tick.Set(0);
        S.Receipt(S.UpstreamRoute(root)).Settle();
        Require(S.Value(N.Read(recipe)).Ticks == prediction && S.Port(services[1]).Ticks == delivered &&
            S.Value(S.ReadProduction(root, services[1])).Ticks == 1 &&
            S.CandidatePort(root).Ticks == 1 && Total(S.Receipt(S.UpstreamRoute(root))) == 0,
            "Changing and withdrawing the old supplier cannot alter the independent raw-input candidate or its current delivery.");
        Require(!ContainsSite(recipe, S.Port(services[0]).Tick, new HashSet<IElement>(ReferenceEqualityComparer.Instance)),
            "The candidate's executable relation operands contain no reference to the replaced supplier's exposed tick site; sharing its carrier basis is not a dependency.");
        Observe(ref root, 112);
        Require(ReferenceEquals(S.CandidateRecipe(root), recipe) && S.Value(N.Read(recipe)).Ticks == -3 &&
            (S.CandidatePresence(root).Ticks == 0 || S.CandidatePort(root).Ticks == -1) && !S.Attached(S.UpstreamRoute(root)),
            "The same independent recipe reads the changed raw input; any actually paid publication agrees, without requiring a free calculation after demand withdrawal.");
    }

    private static void CompatibilityAndDirectAccess()
    {
        var root = S.Create(learning: false, supportWeight: 1, directAccess: true);
        Observe(ref root, 100, fineSecond: true);
        var frame = Observe(ref root, 103, fineSecond: true);
        var services = S.Services(root);
        var firstRows = Inspected(frame, 0);
        var secondRows = Inspected(frame, 1);
        Require(firstRows.Any(row => ReferenceEquals(LocalDiscovery.Service((Relation)S.Parts(row)[0]), services[0]) && Number(row, 2) == 1),
            "Direct access lets a fine service satisfy the coarse consumer; it is not artificially marked incompatible.");
        Require(secondRows.Any(row => ReferenceEquals(LocalDiscovery.Service((Relation)S.Parts(row)[0]), services[1]) && Number(row, 2) == 0) &&
            secondRows.Any(row => ReferenceEquals(LocalDiscovery.Service((Relation)S.Parts(row)[0]), services[0]) && Number(row, 2) == 1),
            "The fine request rejects omitted magnitude before considering price or public use hints.");
        var selected = S.SelectedRoute(root, S.Consumers(root)[1]);
        Require(selected is not null && ReferenceEquals(S.RouteService(selected), services[0]) && Number(Promise(root, 1), 4) == 3,
            "The fine consumer receives a paid fine magnitude, rather than a sign value relabeled as detail.");
        frame = Observe(ref root, 106, fineSecond: false);
        Require(Number(Outcomes(frame)[1], 7) == 3 && Number(Outcomes(frame)[1], 8) == 0,
            "A demand change does not rewrite the previous promise's finer interpretation.");
    }

    private static void PresenceAndMissingOutcome()
    {
        var root = S.Create(supportWeight: 0);
        Observe(ref root, 100);
        Observe(ref root, 103);
        var pending = Promise(root, 0);
        var support = Total(S.Support(root));
        var frame = Observe(ref root, null);
        Require(ReferenceEquals(S.Parts(Outcomes(frame)[0])[0], pending) && Number(Outcomes(frame)[0], 5) == 0 &&
            S.Parts(Outcomes(frame)[0])[1] is Placeholder && Total(S.Support(root)) == support,
            "The missing immediate observation remains unresolved and supplies no candidate verdict.");
        Require(S.Services(root).All(service => S.Presence(service).Ticks == 0) && S.CandidatePresence(root).Ticks == 0 &&
            S.Routes(root).Append(S.UpstreamRoute(root)).All(route => S.Gate(route).Value == 0 && Total(S.Receipt(route)) == 0),
            "Missing input closes current publication; retained coordinates cannot masquerade as a fresh paid result.");
        frame = Observe(ref root, 109);
        Require(Outcomes(frame).All(outcome => Number(outcome, 5) == 0) &&
            S.Services(root).All(service => S.Presence(service).Ticks == 0) && Total(S.Support(root)) == support,
            "The next known level neither fills the earlier missing outcome nor invents a two-level current input.");
        Observe(ref root, 112);
        Require(S.CandidatePresence(root).Ticks == 1 && S.Parts(root)[2] is Relation && Total(S.Support(root)) == support,
            "Two subsequent known levels can start a fresh current prediction without backfilling forgotten observations.");
    }

    private static void AccountedCostsAndPublicHints()
    {
        var root = S.Create(supportWeight: 1);
        var priorSpent = 0L;
        foreach (var input in Inputs())
        {
            Observe(ref root, input);
            var owners = Owners(root);
            Require(owners.Distinct(ReferenceEqualityComparer.Instance).Count() == owners.Length &&
                owners.Sum(Total) == 65536 && S.Pass(S.Conservation(root)) && Total(S.Spent(root)) >= priorSpent,
                "Distinct spendable owners plus incurred expenditure exactly account for the finite initial pool.");
            Require(Total(S.OperatingExpense(root)) + Total(S.ContactExpense(root)) + Total(S.ConstructionExpense(root)) +
                Total(S.TrialExpense(root)) == Total(S.Spent(root)),
                "Native work, contact, construction and shadow accounts partition actual expenditure without double-counting supplier payments.");
            priorSpent = Total(S.Spent(root));
        }
        var before = Owners(root).Select(Total).ToArray();
        var accounts = S.Providers(root).SelectMany(provider => new[] { Total(S.Receipts(provider)), Total(S.Expenses(provider)) }).ToArray();
        var support = Total(S.Support(root));
        var use = S.Usage(S.Routes(root)[0]);
        LocalDiscovery.Update(use, true, true);
        LocalDiscovery.Update(use, true, true);
        Require(before.SequenceEqual(Owners(root).Select(Total)) && support == Total(S.Support(root)) &&
            accounts.SequenceEqual(S.Providers(root).SelectMany(provider => new[] { Total(S.Receipts(provider)), Total(S.Expenses(provider)) })),
            "Public active/qualified contributions cannot mint currency, record invented revenue or reward a candidate.");
        var state = Canonical(root);
        foreach (var advert in S.Advertisements(root))
        {
            _ = LocalDiscovery.ActiveCount(advert).Value;
            _ = LocalDiscovery.SuccessfulCount(advert).Value;
        }
        Require(state == Canonical(root), "Inspecting a public count is a pure local read.");
    }

    private static void BoundedAdvertisementEvidence()
    {
        var root = S.Create(supportWeight: 1, directAccess: false);
        var inspections = 0;
        foreach (var input in Inputs())
        {
            var frame = Observe(ref root, input);
            for (var index = 0; index < 2; index++)
            {
                var rows = Inspected(frame, index);
                var neighbors = S.Parts((Relation)S.Parts(S.Neighborhoods(root)[index])[1]);
                Require(rows.Length <= 2 && rows.Select(row => S.Parts(row)[0]).Distinct(ReferenceEqualityComparer.Instance).Count() == rows.Length &&
                    rows.All(row => neighbors.Any(advert => ReferenceEquals(advert, S.Parts(row)[0]))),
                    "The decision witness contains at most two distinct advertisements from that actor's connected neighborhood.");
                inspections += rows.Length;
                if (index == 0)
                    Require(rows.All(row => ReferenceEquals(LocalDiscovery.Service((Relation)S.Parts(row)[0]), S.Services(root)[1])),
                        "Restricted discovery cannot silently scan or contact the outside fine supplier.");
            }
        }
        Require(inspections > 0, "The local discovery bound is checked on actual searches, not an idle zero case.");
    }

    private static void NativeContinuation()
    {
        var root = S.Create(supportWeight: 1, directAccess: true);
        foreach (var input in new long?[] { 100, 103, 106, null, 112, 115 }) Observe(ref root, input);
        root = S.RetainContinuation(root);
        var clone = (Relation)GojoNumericFormat.Decode(GojoNumericFormat.Encode(root)).Single();
        Require(Canonical(root) == Canonical(clone),
            "Native transport preserves pending forecasts, independent recipe bindings, support, payments and local discovery state.");
        foreach (var (input, fine) in new (long?, bool)[] { (118, false), (121, false), (124, true), (121, true),
            (null, true), (118, false), (115, false), (115, false), (119, false) })
        {
            Observe(ref root, input, fine);
            Observe(ref clone, input, fine);
            root = S.RetainContinuation(root);
            clone = S.RetainContinuation(clone);
            Require(Canonical(root) == Canonical(clone),
                "Reloaded live structure produces the same later native world through demand change, missing data and reversal.");
        }
    }

    private static void ObserverAccounting()
    {
        var root = S.Create(supportWeight: 1);
        var totals = new ReportTotals();
        var inputs = Inputs();
        for (var index = 0; index < inputs.Length; index++)
        {
            var frame = Observe(ref root, inputs[index], fineSecond: index is >= 4 and <= 7);
            var before = Canonical(root);
            Report.Add(totals, root, frame, index is >= 4 and <= 7 ? "fine" : "coarse");
            Require(before == Canonical(root), "Collecting observer totals does not mutate the native system.");
        }
        Require(totals.Conserved && totals.Admissions == inputs.Length && totals.Start == 1 && totals.End == inputs.Length &&
            totals.Phases.Values.Sum() == inputs.Length,
            "Observer intervals and phase counts cover each admitted input exactly once.");
        foreach (var consumer in totals.Consumers)
        {
            var bases = new[] { consumer.Coarse, consumer.Fine };
            Require(consumer.Opportunities == inputs.Length && bases.Sum(b => b.PriorOpportunities) == inputs.Length - 1 &&
                bases.Sum(b => b.Unresolved) == 2 && bases.All(b => b.PriorOpportunities == b.Evaluated + b.Unresolved &&
                    b.PaidEvaluated == b.PositivePaid + b.ZeroPaid + b.HarmfulPaid) &&
                consumer.Purchases == consumer.UpstreamPurchases + consumer.IntermediaryPurchases,
                "Report denominators distinguish pending, resolved, unresolved and paid opportunities instead of dropping unknowns.");
        }
        Require(totals.Consumers[0].Fine.PriorOpportunities == 0 && totals.Consumers[1].Fine.PriorOpportunities == 4 &&
            totals.Consumers[1].Fine.Unresolved == 2,
            "Demand changes group earlier outcomes by their captured request, including a missing fine promise evaluated after returning coarse.");
        Require(totals.SupplierOperatingExpense + totals.IntermediaryDirectExpense + totals.ContactExpense == Total(S.Spent(root)) &&
            totals.UpstreamInputFees + totals.IntermediaryDirectExpense == Total(S.Expenses(S.Providers(root)[1])) &&
            totals.SupplierOperatingExpense == Total(S.Expenses(S.Providers(root)[0])),
            "Observer expenses reconcile to actual drained work/contact accounts without counting internal fees as additional whole-system cost.");
        Require(totals.Consumers.Sum(c => c.Fees) + totals.UpstreamInputFees == S.Providers(root).Sum(p => Total(S.Receipts(p))) &&
            totals.BuildExpense == Total(S.ConstructionExpense(root)) && totals.TrialExpense == Total(S.TrialExpense(root)),
            "Resale/input receipt totals and construction/shadow costs agree with connected native payment evidence.");
    }

    private static void FinishedWitnessRelease()
    {
        var retained = S.Create(supportWeight: 1);
        var released = S.Create(supportWeight: 1);
        var operational = S.Operational(released);
        foreach (var input in Inputs())
        {
            Observe(ref retained, input);
            var frame = Observe(ref released, input);
            var pending = S.Parts((Relation)S.Parts(released)[1]).OfType<Relation>().Select(row => row.Id).ToArray();
            var candidatePromise = S.Parts(released)[2] as Relation;
            var binding = S.Parts(released)[3];
            released = S.RetainContinuation(released);
            var snapshot = GojoEncodedSnapshot.CaptureGraph(released);
            Require(ReferenceEquals(S.Operational(released), operational) && ReferenceEquals(S.Parts(released)[3], binding) &&
                S.Parts(released)[4] is Placeholder && !snapshot.Relations.Any(row => row.Id == frame.Id) &&
                pending.All(id => snapshot.Relations.Any(row => row.Id == id)) &&
                (candidatePromise is null || snapshot.Relations.Any(row => row.Id == candidatePromise.Id)),
                "Completed frames leave the graph while the original mutable world, candidate binding and unsettled obligations remain.");
            Require(Canonical(S.RetainContinuation(retained)) == Canonical(released),
                "Releasing completed diagnostic evidence cannot change later native behavior.");
        }
    }

    private static long?[] Inputs() => [100, 103, 106, 109, 112, 115, 112, null, 109, 106, 106, 109];
    private static Carrier[] Owners(Relation root) => S.Parts((Relation)S.Parts(S.Resources(root))[5]).Cast<Carrier>().ToArray();
    private static Relation Promise(Relation root, int index) => (Relation)S.Parts((Relation)S.Parts(root)[1])[index];
    private static Relation[] Outcomes(Relation frame) => S.Parts((Relation)S.Parts(frame)[2]).Cast<Relation>().ToArray();
    private static Relation[] Inspected(Relation frame, int index)
    {
        var decision = (Relation)S.Parts((Relation)S.Parts(frame)[3])[index];
        return S.Parts((Relation)S.Parts(decision)[2]).Cast<Relation>().ToArray();
    }
    private static Relation Observe(ref Relation root, long? input, bool fineSecond = false)
    {
        Require(S.TryObserve(root, input, fineSecond, out var next, out var frame),
            "The current native world accepts its next supplied observation.");
        root = next;
        return frame;
    }
    private static long Number(Relation row, int index) => ((CarrierLocation)S.Parts(row)[index]).Ticks;
    private static long Total(Carrier carrier) => S.SignedValue(carrier).Ticks;
    private static bool ContainsSite(IElement element, TickRef site, HashSet<IElement> seen)
    {
        if (!seen.Add(element)) return false;
        return element switch
        {
            CarrierLocation location => ReferenceEquals(location.Tick, site),
            Relation relation => ContainsSite(relation.Left, site, seen) || ContainsSite(relation.Right, site, seen),
            _ => false
        };
    }
    internal static string Canonical(Relation root)
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
        if (!condition) throw new InvalidOperationException("Local service integration check failed: " + message);
    }
}
