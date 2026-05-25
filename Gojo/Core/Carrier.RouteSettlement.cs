namespace Gojo.Core;

public sealed partial class Carrier
{
    #region Runtime Route Settlement

    private readonly record struct RuntimeRouteSites(Carrier Carrier, long Min, long Max)
    {
        public long Width => Max - Min;

        public bool IsOrigin => Width == 0 && Min == 0;
    }

    private readonly record struct RuntimeRouteGroupKey(
        Carrier Source,
        long SourceBoundary,
        long LocalTarget,
        long Unit);

    private readonly record struct RuntimeRouteCandidate(
        Relation Relation,
        RuntimeRouteSites Source,
        RuntimeRouteSites Target,
        long SourceBoundary,
        long LocalTarget,
        long Unit)
    {
        public Carrier SourceCarrier => Source.Carrier;

        public Carrier TargetCarrier => Target.Carrier;
    }

    private readonly record struct RuntimeRouteTargetState(Carrier Carrier, ulong? PositiveCapacity)
    {
        public bool HasFinitePositiveCapacity => PositiveCapacity.HasValue;

        public ulong? PositiveCapacityLeft(ulong alreadyAccepted) =>
            !PositiveCapacity.HasValue
                ? null
                : alreadyAccepted >= PositiveCapacity.Value
                    ? 0UL
                    : PositiveCapacity.Value - alreadyAccepted;
    }

    private readonly record struct RuntimeRouteLane(RuntimeRouteCandidate Candidate, int GroupIndex);

    private sealed class RuntimeRouteDiscovery
    {
        public RuntimeRouteCandidate[] Candidates { get; }

        public RuntimeRouteSourceGroup[] ActiveGroups { get; }

        public HashSet<ulong> HandledRelationIds { get; }

        private IReadOnlyDictionary<Carrier, RuntimeRouteTargetState> TargetStates { get; }

        public RuntimeRouteDiscovery(
            RuntimeRouteCandidate[] candidates,
            IReadOnlyDictionary<Carrier, RuntimeRouteTargetState> targetStates)
        {
            Candidates = candidates;
            TargetStates = targetStates;
            ActiveGroups = [.. candidates
                .GroupBy(candidate => new RuntimeRouteGroupKey(
                    candidate.SourceCarrier,
                    candidate.SourceBoundary,
                    candidate.LocalTarget,
                    candidate.Unit))
                .Select(group => new RuntimeRouteSourceGroup(
                    group.Key,
                    [.. group.OrderBy(candidate => candidate.Relation.Id)]))
                .Where(group => group.Candidates.Length > 1 ||
                    group.Candidates.Any(candidate => TargetStateOf(candidate.TargetCarrier).HasFinitePositiveCapacity))];
            HandledRelationIds = [.. ActiveGroups
                .SelectMany(group => group.Candidates)
                .Select(candidate => candidate.Relation.Id)];
        }

        public RuntimeRouteTargetState TargetStateOf(Carrier carrier) =>
            TargetStates.TryGetValue(carrier, out var state)
                ? state
                : new RuntimeRouteTargetState(carrier, RuntimePositiveAbsorptionCapacity(carrier));
    }

    private sealed class RuntimeRouteSourceGroup
    {
        public RuntimeRouteGroupKey Key { get; }

        public RuntimeRouteCandidate[] Candidates { get; }

        public ulong UnitTicks { get; set; }

        public ulong SourceRemainder { get; set; }

        public ulong Remaining { get; set; }

        public Carrier Source => Key.Source;

        public RuntimeRouteSourceGroup(RuntimeRouteGroupKey key, RuntimeRouteCandidate[] candidates)
        {
            Key = key;
            Candidates = candidates;
        }
    }

    /// <summary>
    /// Coordinates direct positive tension routes that compete for the same
    /// source boundary during this settlement pass. Target absorption is read
    /// from existing local clamp structure; all route records here are runtime
    /// scratch, not durable Gojo structure.
    /// </summary>
    private static HashSet<ulong> ApplyRuntimeRouteSettlement(Carrier[] carriers)
    {
        var discovery = DiscoverRuntimeRoutes(carriers);
        if (discovery.ActiveGroups.Length == 0)
        {
            return discovery.HandledRelationIds;
        }

        ApplyRuntimeRouteGroups(discovery);
        return discovery.HandledRelationIds;
    }

    /// <summary>Reads direct route candidates and target intake from current structure.</summary>
    private static RuntimeRouteDiscovery DiscoverRuntimeRoutes(Carrier[] carriers)
    {
        var candidates = carriers
            .SelectMany(carrier => carrier.relations)
            .GroupBy(relation => relation.Id)
            .Select(group => group.First())
            .SelectMany(relation =>
                TryReadPositiveRuntimeRouteCandidate(relation, out var candidate)
                    ? [candidate]
                    : Array.Empty<RuntimeRouteCandidate>())
            .OrderBy(candidate => candidate.Relation.Id)
            .ToArray();
        var targets = candidates
            .Select(candidate => candidate.TargetCarrier)
            .Distinct()
            .ToDictionary(
                target => target,
                target => new RuntimeRouteTargetState(target, RuntimePositiveAbsorptionCapacity(target)));
        return new RuntimeRouteDiscovery(candidates, targets);
    }

    /// <summary>Reads a direct positive route candidate from one relation.</summary>
    private static bool TryReadPositiveRuntimeRouteCandidate(Relation relation, out RuntimeRouteCandidate candidate)
    {
        if (!TryReadRuntimeRouteSites(relation.Left, out var source) ||
            !TryReadRuntimeRouteSites(relation.Right, out var target) ||
            source.Carrier.SameIdentityAs(target.Carrier) ||
            source.Carrier.IsUnknown ||
            target.Carrier.IsUnknown ||
            source.Carrier.IsInverse != target.Carrier.IsInverse ||
            !target.IsOrigin)
        {
            candidate = default;
            return false;
        }

        var unit = source.Width == 0 ? source.Max : source.Width;
        var localTarget = source.Width == 0 ? 0 : source.Min;
        if (unit <= 0 ||
            localTarget < 0 ||
            source.Max < localTarget)
        {
            candidate = default;
            return false;
        }

        candidate = new RuntimeRouteCandidate(
            relation,
            source,
            target,
            source.Max,
            localTarget,
            unit);
        return true;
    }

    /// <summary>Coordinates source offers and target acceptance across direct routes.</summary>
    private static void ApplyRuntimeRouteGroups(RuntimeRouteDiscovery discovery)
    {
        var groups = discovery.ActiveGroups;
        PrepareRuntimeRouteGroups(groups);
        var lanes = groups
            .SelectMany((group, groupIndex) => group.Candidates.Select(candidate => new RuntimeRouteLane(candidate, groupIndex)))
            .ToArray();
        var accepted = new ulong[lanes.Length];

        while (groups.Any(group => group.Remaining != 0))
        {
            var requested = new ulong[lanes.Length];
            RequestRuntimeRouteTicks(discovery, groups, lanes, accepted, requested);
            if (requested.All(ticks => ticks == 0))
            {
                break;
            }

            var moved = AcceptRuntimeRouteTicks(discovery, groups, lanes, requested, accepted);
            if (moved == 0)
            {
                break;
            }
        }

        for (var index = 0; index < lanes.Length; index++)
        {
            if (accepted[index] != 0)
            {
                lanes[index].Candidate.TargetCarrier.Receive(checked((long)accepted[index]));
            }
        }

        foreach (var group in groups)
        {
            var remaining = checked((group.Remaining * group.UnitTicks) + group.SourceRemainder);
            group.Source.KeepTensionRemainder(checked((long)remaining));
        }
    }

    /// <summary>Accounts local source boundaries and records source route demand.</summary>
    private static void PrepareRuntimeRouteGroups(RuntimeRouteSourceGroup[] groups)
    {
        foreach (var group in groups)
        {
            var first = group.Candidates[0];
            AccountRuntimeLocal(group.Source, first.SourceBoundary, first.LocalTarget);
            group.UnitTicks = checked((ulong)first.Unit);
            if (group.Source.Tension <= 0)
            {
                group.SourceRemainder = 0;
                group.Remaining = 0;
                continue;
            }

            var available = checked((ulong)group.Source.Tension);
            group.Remaining = available / group.UnitTicks;
            group.SourceRemainder = available % group.UnitTicks;
        }
    }

    /// <summary>Lets each source split its remaining carried ticks across active route lanes.</summary>
    private static void RequestRuntimeRouteTicks(
        RuntimeRouteDiscovery discovery,
        RuntimeRouteSourceGroup[] groups,
        RuntimeRouteLane[] lanes,
        ulong[] accepted,
        ulong[] requested)
    {
        for (var groupIndex = 0; groupIndex < groups.Length; groupIndex++)
        {
            var group = groups[groupIndex];
            if (group.Remaining == 0)
            {
                continue;
            }

            var active = Enumerable.Range(0, lanes.Length)
                .Where(index => lanes[index].GroupIndex == groupIndex)
                .Where(index => RuntimeRouteTargetCanReceive(
                    discovery,
                    lanes,
                    accepted,
                    lanes[index].Candidate.TargetCarrier))
                .ToArray();
            if (active.Length == 0)
            {
                continue;
            }

            var count = checked((ulong)active.Length);
            var baseShare = group.Remaining / count;
            var remainder = group.Remaining % count;
            for (var order = 0; order < active.Length; order++)
            {
                requested[active[order]] = baseShare + (checked((ulong)order) < remainder ? 1UL : 0UL);
            }
        }
    }

    /// <summary>Lets targets accept requested ticks, sharing finite intake across inbound flows.</summary>
    private static ulong AcceptRuntimeRouteTicks(
        RuntimeRouteDiscovery discovery,
        RuntimeRouteSourceGroup[] groups,
        RuntimeRouteLane[] lanes,
        ulong[] requested,
        ulong[] accepted)
    {
        var moved = 0UL;
        foreach (var targetGroup in Enumerable.Range(0, lanes.Length)
            .Where(index => requested[index] != 0)
            .GroupBy(index => lanes[index].Candidate.TargetCarrier))
        {
            var target = targetGroup.Key;
            var indexes = targetGroup.ToArray();
            var requestedTotal = indexes.Aggregate(0UL, (sum, index) => checked(sum + requested[index]));
            var targetLeft = RuntimeRouteTargetLeft(discovery, lanes, accepted, target);
            var acceptedByTarget = targetLeft.HasValue
                ? AllocateFiniteRuntimeRouteTicks(indexes, requested, Math.Min(requestedTotal, targetLeft.Value))
                : indexes.ToDictionary(index => index, index => requested[index]);

            foreach (var (index, ticks) in acceptedByTarget)
            {
                if (ticks == 0)
                {
                    continue;
                }

                accepted[index] = checked(accepted[index] + ticks);
                groups[lanes[index].GroupIndex].Remaining = checked(groups[lanes[index].GroupIndex].Remaining - ticks);
                moved = checked(moved + ticks);
            }
        }

        return moved;
    }

    /// <summary>Returns true when a route target is unlimited or still has finite intake left.</summary>
    private static bool RuntimeRouteTargetCanReceive(
        RuntimeRouteDiscovery discovery,
        RuntimeRouteLane[] lanes,
        ulong[] accepted,
        Carrier target)
    {
        var left = RuntimeRouteTargetLeft(discovery, lanes, accepted, target);
        return !left.HasValue || left.Value != 0;
    }

    /// <summary>Shares one finite target intake across the flows currently requesting it.</summary>
    private static Dictionary<int, ulong> AllocateFiniteRuntimeRouteTicks(
        int[] indexes,
        ulong[] requested,
        ulong capacity)
    {
        var accepted = indexes.ToDictionary(index => index, _ => 0UL);
        var remaining = capacity;
        var active = indexes.ToList();
        while (remaining != 0 && active.Count != 0)
        {
            var count = checked((ulong)active.Count);
            var baseShare = remaining / count;
            var remainder = remaining % count;
            var moved = 0UL;
            for (var order = 0; order < active.Count; order++)
            {
                var index = active[order];
                var requestLeft = requested[index] - accepted[index];
                var offered = baseShare + (checked((ulong)order) < remainder ? 1UL : 0UL);
                var ticks = Math.Min(requestLeft, offered);
                accepted[index] = checked(accepted[index] + ticks);
                moved = checked(moved + ticks);
            }

            if (moved == 0)
            {
                break;
            }

            remaining = checked(remaining - moved);
            active.RemoveAll(index => accepted[index] == requested[index]);
        }

        return accepted;
    }

    /// <summary>Reads finite target intake left after already accepted route ticks.</summary>
    private static ulong? RuntimeRouteTargetLeft(
        RuntimeRouteDiscovery discovery,
        RuntimeRouteLane[] lanes,
        ulong[] accepted,
        Carrier target)
    {
        var allocated = 0UL;
        for (var index = 0; index < lanes.Length; index++)
        {
            if (lanes[index].Candidate.TargetCarrier.SameIdentityAs(target))
            {
                allocated = checked(allocated + accepted[index]);
            }
        }

        return discovery.TargetStateOf(target).PositiveCapacityLeft(allocated);
    }

    /// <summary>
    /// Reads total positive ticks a target can absorb before its own local clamp
    /// would leave positive tension. Null means this target is not finite here.
    /// </summary>
    private static ulong? RuntimePositiveAbsorptionCapacity(Carrier carrier)
    {
        var limit = RuntimePositiveClampLimit(carrier);
        if (!limit.HasValue)
        {
            return null;
        }

        return RuntimePositiveClampCapacity(carrier, limit.Value);
    }

    /// <summary>Reads the nearest local positive clamp point on this carrier.</summary>
    private static ulong? RuntimePositiveClampLimit(Carrier carrier)
    {
        ulong? limit = null;
        foreach (var relation in carrier.relations
            .GroupBy(relation => relation.Id)
            .Select(group => group.First()))
        {
            if (!TryReadRuntimeRouteSites(relation.Left, out var left) ||
                !TryReadRuntimeRouteSites(relation.Right, out var right) ||
                !left.Carrier.SameIdentityAs(carrier) ||
                !right.Carrier.SameIdentityAs(carrier) ||
                left.Width != 0 ||
                right.Width != 0 ||
                left.Max != right.Max ||
                left.Max < 0)
            {
                continue;
            }

            var ticks = checked((ulong)left.Max);
            limit = limit.HasValue
                ? Math.Min(limit.Value, ticks)
                : ticks;
        }

        return limit;
    }

    /// <summary>Computes clamp intake without mutating the target carrier.</summary>
    private static ulong RuntimePositiveClampCapacity(Carrier carrier, ulong limit)
    {
        var tensionDebt = carrier.Tension < 0
            ? checked((ulong)-carrier.Tension)
            : 0UL;

        if (carrier.Tension > 0 || carrier.Value >= limit)
        {
            return tensionDebt;
        }

        return checked(tensionDebt + (limit - carrier.Value));
    }

    /// <summary>Applies same-carrier local accounting without choosing a route target.</summary>
    private static bool AccountRuntimeLocal(Carrier carrier, long source, long target)
    {
        if (source <= 0 || target < 0 || source < target)
        {
            return false;
        }

        var sourceTicks = checked((ulong)source);
        var targetTicks = checked((ulong)target);
        if (carrier.Value <= targetTicks)
        {
            return false;
        }

        var local = sourceTicks == targetTicks
            ? Math.Min(carrier.Value, sourceTicks)
            : checked(targetTicks + ((carrier.Value - targetTicks) % (sourceTicks - targetTicks)));
        return local != carrier.Value &&
            carrier.LeaveTension(local, checked((long)(carrier.Value - local)));
    }

    /// <summary>Reads all sites in an element only when they share one carrier.</summary>
    private static bool TryReadRuntimeRouteSites(IElement element, out RuntimeRouteSites sites)
    {
        var locations = RuntimeRouteLocationsOf(element);
        if (locations.Length == 0)
        {
            sites = default;
            return false;
        }

        var carrier = locations[0].Carrier;
        if (locations.Any(location => !location.Carrier.SameIdentityAs(carrier)))
        {
            sites = default;
            return false;
        }

        sites = new RuntimeRouteSites(
            carrier,
            locations.Min(location => location.Ticks),
            locations.Max(location => location.Ticks));
        return true;
    }

    /// <summary>Collects carrier locations from an element tree for route discovery.</summary>
    private static CarrierLocation[] RuntimeRouteLocationsOf(IElement element) => element switch
    {
        CarrierLocation location => [location],
        Relation relation => [.. RuntimeRouteLocationsOf(relation.Left), .. RuntimeRouteLocationsOf(relation.Right)],
        _ => []
    };

    #endregion
}
