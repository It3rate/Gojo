using Gojo.Structures;

namespace Gojo.Core;

public sealed partial class Carrier
{
    /// <summary>
    /// Observes bounded settlement of this connected world. Quietness is derived
    /// from a pass with no applied change, not from zero tension or equal pass
    /// endpoints. The returned observation is historical, never a cached answer.
    /// </summary>
    public Relation Settle(int maxPasses = 256) =>
        RunSettlement([this], this, Relation.OrderedGroup(new Carrier()), maxPasses, observe: true)!;

    /// <summary>
    /// Receives an ordered group of signed carrier-location deltas as one frame.
    /// All coordinates are sampled and validated before any input is received;
    /// all arrivals enter before any attached equation runs. Calling again is
    /// another arrival, even when the numeric samples are identical or zero.
    /// Unbound, malformed or unrepresentable input remains an unresolved read,
    /// with no partial admission. The host supplies timing, not domain meaning.
    /// </summary>
    public static Relation ReceiveTogether(Relation arrivals, int maxPasses = 256)
    {
        if (maxPasses < 0 || arrivals.Right is not Carrier holder ||
            !arrivals.TryReadOrderedElementsOn(holder, out var elements) ||
            elements.Length == 0 || elements.Any(element => element is not CarrierLocation location ||
                location.Carrier is null || location.Carrier.IsUnknown || location.Tick is null))
            return Unreceived(arrivals, maxPasses);

        // Freeze even shared live slots before touching any of their carriers.
        var samples = elements.Cast<CarrierLocation>()
            .Select(location => new CarrierLocation(location.Carrier, location.Ticks)).ToArray();
        var frozen = Relation.OrderedGroup(new Carrier(), samples.Cast<IElement>().ToArray());
        (Carrier Carrier, long Delta)[] totals;
        try
        {
            totals = samples.GroupBy(location => location.Carrier)
                .Select(group => (group.Key, checked((long)group.Aggregate((Int128)0, (sum, item) => sum + item.Ticks))))
                .ToArray();
            // Check primitive reception, not future equation arithmetic. Runtime
            // overflow inside an arbitrary attached equation is not transactional.
            foreach (var (carrier, delta) in totals)
            {
                if (delta == 0) continue;
                var pressure = checked(carrier.Tension + delta);
                if (pressure > 0) _ = checked((long)carrier.Value + pressure);
                else if (pressure < 0) _ = checked(-pressure);
            }
        }
        catch (OverflowException)
        {
            return Unreceived(arrivals, maxPasses, frozen);
        }

        var anchors = totals.Select(total => total.Carrier).ToArray();
        if (ConnectedCarriers(anchors).Any(carrier => carrier.settling))
            return Unreceived(arrivals, maxPasses, frozen);

        foreach (var (carrier, delta) in totals) carrier.Receive(delta);
        return RunSettlement(anchors, arrivals, frozen, maxPasses, observe: true)!;
    }

    private static Relation Unreceived(IElement arrivals, int maxPasses, IElement? sampled = null)
    {
        var clock = new Carrier();
        var budget = Relation.Segment(clock, 0, maxPasses);
        var pending = Relation.UnresolvedRead(sampled ?? arrivals, budget, clock);
        return SettlementObservation.Create(arrivals, arrivals, budget,
            new CarrierLocation(clock, 0), pending, pending, new Placeholder());
    }

    private static Relation? RunSettlement(Carrier[] anchors, IElement selected,
        IElement admitted, int maxPasses, bool observe)
    {
        var carriers = ConnectedCarriers(anchors);
        if (maxPasses < 0 || carriers.Length == 0 || carriers.Any(carrier => carrier.settling))
            return observe ? Unreceived(selected, maxPasses) : null;

        var clock = observe ? new Carrier() : null;
        var passes = 0;
        List<Relation>? lastActivity = null;
        // Runtime scratch only. Nothing is retained on the carriers as status.
        var guarded = new HashSet<Carrier>();
        try
        {
            for (; passes < maxPasses;)
            {
                foreach (var carrier in carriers)
                    if (guarded.Add(carrier)) carrier.settling = true;

                var active = new List<Relation>();
                var routed = ApplyRuntimeRouteSettlement(carriers, active);
                foreach (var relation in carriers.SelectMany(carrier => carrier.relations)
                    .DistinctBy(relation => relation.Id))
                {
                    if (!routed.Contains(relation.Id) && relation.Apply()) active.Add(relation);
                }

                passes++;
                lastActivity = active;
                if (active.Count == 0) break;
                carriers = ConnectedCarriers(anchors);
            }
        }
        finally
        {
            foreach (var carrier in guarded) carrier.settling = false;
        }

        if (!observe) return null;
        var budget = Relation.Segment(clock!, 0, maxPasses);
        IElement activity = lastActivity is null
            ? Relation.UnresolvedRead(selected, budget, clock!)
            : Relation.OrderedGroup(new Carrier(), lastActivity.DistinctBy(relation => relation.Id).Cast<IElement>().ToArray());
        var scope = Relation.OrderedGroup(new Carrier(), anchors.Cast<IElement>().ToArray());
        return SettlementObservation.Create(selected, scope, budget,
            new CarrierLocation(clock!, passes), activity, activity, admitted);
    }

    /// <summary>Discovers only attached worlds reachable from the supplied local anchors.</summary>
    private static Carrier[] ConnectedCarriers(IEnumerable<Carrier> anchors)
    {
        var found = new HashSet<Carrier>();
        var pending = new Queue<Carrier>(anchors.Where(carrier => !carrier.IsUnknown));
        while (pending.TryDequeue(out var carrier))
        {
            if (!found.Add(carrier)) continue;
            foreach (var relation in carrier.relations)
                foreach (var related in relation.Carriers().Where(related => !related.IsUnknown))
                    pending.Enqueue(related);
        }
        return [.. found.OrderBy(carrier => carrier.Id)];
    }
}
