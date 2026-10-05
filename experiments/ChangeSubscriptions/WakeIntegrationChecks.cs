using System.Text.Json;
using Gojo.Core;
using C = ChangeSubscriptions.ChangeSurface;
using E = ChangeSubscriptions.NativeExperiment;
using R = ChangeSubscriptions.ComparativeRetry;
using W = ChangeSubscriptions.WakeProbe;

namespace ChangeSubscriptions;

internal static class WakeIntegrationChecks
{
    public static int Run()
    {
        BoundedEarlyForecastsKeepPaidEvidence();
        UnknownEarlyOutcomesRemainCounted();
        DefaultControlIsUnchanged();
        TransportContinuesPendingEarlyForecast();
        return 4;
    }

    private static void BoundedEarlyForecastsKeepPaidEvidence()
    {
        var root = Cooling(); var consumers = E.Consumers(root); var magnitude = consumers[1];
        var retry = E.Retry(magnitude); var wake = E.Wake(magnitude)!;
        Require(R.Harmful(retry).Value == 12 && R.Rung(retry).Value == 3 && R.RetryAfter(retry).Ticks == 27 &&
            !C.Attached(E.Subscriber(magnitude)) && C.Attached(W.Watcher(wake)) && W.AnchorValue(wake).Ticks == 0,
            "Steady unit increments naturally withdraw the overlarge amplitude forecast while retaining the useful direction connection.");
        E.Observe(ref root, 23); Accounted(root);
        Require(W.CandidateCount(wake).Value == 1 && W.ProbesIssued(wake).Value == 0 && !C.Attached(E.Subscriber(magnitude)),
            "The first larger increment only proposes a changed coarse cell.");
        E.Observe(ref root, 27); Accounted(root);
        var issued = Pending(root)[1];
        Require(E.Early(issued) && Number(E.Parts(issued)[2]) == 6 && W.ProbesIssued(wake).Value == 1 &&
            W.Tokens(wake).Value == 1 && C.Attached(E.Subscriber(magnitude)) && R.Rung(retry).Value == 3 &&
            R.RetryAfter(retry).Ticks == 27 && R.Harmful(retry).Value == 12 && E.Opportunity(root).Ticks == 21,
            "Confirmation buys one frozen next forecast before ordinary retry, without changing its prior negative evidence or deadline.");
        E.Observe(ref root, 31); Accounted(root);
        Require(W.ProbesIssued(wake).Value == 2 && W.Tokens(wake).Value == 0 &&
            R.Evaluated(retry).Value == 13 && R.Helpful(retry).Value == 1 && E.Early(Pending(root)[1]),
            "The first paid probe is evaluated on the next actual input; a second distinct promise consumes the remaining allowance.");
        E.Observe(ref root, 35); Accounted(root);
        Require(!C.Attached(E.Subscriber(magnitude)) && W.ProbesIssued(wake).Value == 2 && R.Evaluated(retry).Value == 14 &&
            R.Helpful(retry).Value == 2 && R.Harmful(retry).Value == 12 && R.BlockCount(retry).Value == 2 &&
            C.Signed(R.Benefit(retry)).Ticks == -8 && R.Rung(retry).Value == 3 &&
            E.EarlyCounter(magnitude, 0).Value == 4 && E.EarlyCounter(magnitude, 1).Value == 8 &&
            E.EarlyCounter(magnitude, 2).Value == 2 && E.EarlyCounter(magnitude, 3).Value == 0,
            "Both probes remain in ordinary outcome totals and a partial block; a locally useful pair does not erase twelve earlier harms or create a third probe.");
        Require(W.ProbesIssued(E.Wake(consumers[0])!).Value == 0 && C.Attached(E.Subscriber(consumers[0])),
            "The independently useful direction use does not receive speculative credits while ordinarily eligible.");
    }

    private static void UnknownEarlyOutcomesRemainCounted()
    {
        var root = Cooling(); E.Observe(ref root, 23); E.Observe(ref root, 27);
        var magnitude = E.Consumers(root)[1]; var retry = E.Retry(magnitude); var wake = E.Wake(magnitude)!;
        var evaluated = R.Evaluated(retry).Value; var opportunity = E.Opportunity(root).Ticks;
        E.Observe(ref root, null); Accounted(root);
        Require(R.Evaluated(retry).Value == evaluated && R.Unknown(retry).Value == 1 &&
            E.EarlyCounter(magnitude, 3).Value == 1 && E.EarlyCounter(magnitude, 2).Value == 0 &&
            W.ProbesIssued(wake).Value == 1 && W.Tokens(wake).Value == 0 && W.GrantUsed(wake).Value == 1 &&
            E.Opportunity(root).Ticks == opportunity && R.Rung(retry).Value == 3,
            "A missing future target keeps the paid probe as unknown, cancels its unused sibling, and neither refunds cost nor fabricates adverse evidence.");
        E.Observe(ref root, 31); Accounted(root);
        Require(E.SourcePresence(root).Ticks == 0 && E.Opportunity(root).Ticks == opportunity,
            "One post-gap input cannot restore an adjacent delta or confirm change.");
        E.Observe(ref root, 35); Accounted(root); E.Observe(ref root, 39); Accounted(root);
        Require(W.ProbesIssued(wake).Value == 1 && W.Grants(wake).Value == 1 && W.Tokens(wake).Value == 0 &&
            R.Unknown(retry).Value == 1 && R.Evaluated(retry).Value == evaluated,
            "Restored larger increments do not reuse the spent epoch or reinterpret its unavailable outcome as success.");
    }

    private static void DefaultControlIsUnchanged()
    {
        var defaults = E.Create(); var explicitControl = E.Create(wake: false);
        foreach (var value in new long?[] { 0, 1, 2, 3, 4, 5, 6, 10, 14, null, 15, 16, 15, 16, 17 })
        {
            E.Observe(ref defaults, value); defaults = E.Retain(defaults);
            E.Observe(ref explicitControl, value); explicitControl = E.Retain(explicitControl);
            Require(Canonical(defaults) == Canonical(explicitControl) && E.Consumers(defaults).All(c => E.Wake(c) is null),
                "The original default path retains exactly the explicit wake-disabled native shape and behavior.");
            Accounted(defaults); Accounted(explicitControl);
        }
    }

    private static void TransportContinuesPendingEarlyForecast()
    {
        var root = Cooling(); E.Observe(ref root, 23); E.Observe(ref root, 27); root = E.Retain(root);
        var clone = (Relation)GojoNumericFormat.Decode(GojoNumericFormat.Encode(root)).Single();
        Require(Canonical(root) == Canonical(clone) && E.Early(Pending(clone)[1]) &&
            W.Tokens(E.Wake(E.Consumers(clone)[1])!).Value == 1,
            "Transport retains a real paid early promise, its consumed first token, and the remaining grant.");
        var cloneArrival = E.Arrival(clone).Ticks;
        E.Observe(ref root, 31); root = E.Retain(root); Accounted(root);
        Require(E.Arrival(clone).Ticks == cloneArrival && W.ProbesIssued(E.Wake(E.Consumers(clone)[1])!).Value == 1,
            "The restored operational world does not share mutable clocks or payment state with the original.");
        E.Observe(ref clone, 31); clone = E.Retain(clone); Accounted(clone);
        Require(Canonical(root) == Canonical(clone),
            "Both native worlds assess the frozen probe and spend the remaining token identically.");
        foreach (var value in new long?[] { null, 35, 39, 43, 47, 51, 55, 59 })
        {
            E.Observe(ref root, value); root = E.Retain(root); Accounted(root);
            E.Observe(ref clone, value); clone = E.Retain(clone); Accounted(clone);
            Require(Canonical(root) == Canonical(clone),
                "Restored wake state preserves unknown pending outcomes, future normal eligibility and all subsequent accounting.");
        }
    }

    private static Relation Cooling()
    {
        var root = E.Create(wake: true);
        for (var level = 0; level <= 19; level++) { E.Observe(ref root, level); Accounted(root); root = E.Retain(root); }
        return root;
    }
    private static void Accounted(Relation root)
    {
        var pending = Pending(root); var consumers = E.Consumers(root);
        for (var i = 0; i < consumers.Length; i++)
        {
            var consumer = consumers[i]; var retry = E.Retry(consumer);
            var paidPending = E.Parts(pending[i])[2] is CarrierLocation ? 1UL : 0UL;
            Require(E.Counter(consumer, 0).Value == R.Evaluated(retry).Value + R.Unknown(retry).Value + paidPending,
                "Every paid promise appears exactly once as resolved, unknown, or still pending.");
            if (E.Wake(consumer) is Relation wake)
                Require(W.ProbesIssued(wake).Value == E.EarlyCounter(consumer, 2).Value +
                    E.EarlyCounter(consumer, 3).Value + (E.Early(pending[i]) ? 1UL : 0UL),
                    "Every consumed wake token corresponds to one evaluated, unavailable, or still pending early promise.");
        }
    }
    private static Relation[] Pending(Relation root) => E.Parts((Relation)E.Parts(root)[1]).Cast<Relation>().ToArray();
    private static long Number(IElement value) => ((CarrierLocation)value).Ticks;
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
    { if (!condition) throw new InvalidOperationException("Wake integration check failed: " + message); }
}
