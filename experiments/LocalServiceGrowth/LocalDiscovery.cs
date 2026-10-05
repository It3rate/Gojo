using Gojo.Core;
using Gojo.Equations;
using Gojo.Structures;
using N = ConnectionAssembly.NativeMath;

namespace LocalServiceGrowth;

/// <summary>
/// Public usage is native incidence accounting, not a reputation declaration.
/// Advertisement = G(service, active count, qualified count, incidence holder).
/// Use = G(advertisement, consumer, gate, qualified, desired sites, delta laws).
/// The qualified contribution means this attached consumer's latest evaluated
/// outcome was useful in its own basis; it is not confidence or independence.
/// </summary>
public static class LocalDiscovery
{
    public static Relation Advertise(IElement service) =>
        G(service, new Carrier(), new Carrier(), new Carrier());

    public static IElement Service(Relation advert) => Parts(advert)[0];
    public static Carrier ActiveCount(Relation advert) => (Carrier)Parts(advert)[1];
    public static Carrier SuccessfulCount(Relation advert) => (Carrier)Parts(advert)[2];
    public static Carrier Incidences(Relation advert) => (Carrier)Parts(advert)[3];
    public static Carrier Gate(Relation use) => (Carrier)Parts(use)[2];
    public static Carrier Qualified(Relation use) => (Carrier)Parts(use)[3];

    /// <summary>
    /// One contribution per advertisement and consumer holder identity. The
    /// consumer must be a carrier or an ordered group with a unique, stable
    /// holder. Registration scans only this advertisement's incidence list.
    /// Repeated registration shares the original gates and equations rather
    /// than creating a second vote. This does not prove distinct consumers are
    /// independent, and the experiment supplies its identities honestly.
    /// </summary>
    public static Relation Register(Relation advert, IElement consumer)
    {
        var identity = Holder(consumer);
        var incidenceHolder = Incidences(advert);
        foreach (var attachment in incidenceHolder.AttachedRelations)
        {
            if (attachment.Left is not Placeholder || attachment.Right is not Relation body)
                continue;
            var parts = Parts(body);
            if (parts.Length == 6 && ReferenceEquals(parts[0], incidenceHolder) &&
                ReferenceEquals(parts[1], identity))
                return G(advert, consumer, parts[2], parts[3], parts[4], parts[5]);
        }

        var gate = new Carrier();
        var qualified = new Carrier();
        var desiredGate = new CarrierLocation(gate, 0);
        var desiredQualified = new CarrierLocation(qualified, 0);
        var desired = G(desiredGate, desiredQualified);
        // The same change contributes to the private gate and public count.
        // All four reads are captured together before any carrier receives it.
        var laws = G(
            N.SumTo(gate, (desiredGate, 1), (gate.ValueSlot, -1), (gate.LiveTensionSlot, -1)),
            N.SumTo(qualified, (desiredQualified, 1), (qualified.ValueSlot, -1), (qualified.LiveTensionSlot, -1)),
            N.SumTo(ActiveCount(advert), (desiredGate, 1), (gate.ValueSlot, -1)),
            N.SumTo(SuccessfulCount(advert), (desiredQualified, 1), (qualified.ValueSlot, -1)));

        // Attach a thin identity/accounting surface only. Attaching the full
        // advertisement would recursively attach private service recipes too.
        // The open left side deliberately makes no numeric settlement promise.
        incidenceHolder.Attach(new Relation(new Placeholder(),
            G(incidenceHolder, identity, gate, qualified, desired, laws)));
        return G(advert, consumer, gate, qualified, desired, laws);
    }

    /// <summary>
    /// External booleans here schedule already-observed connection/outcome
    /// events; callers retain the native outcome witness in their own frame.
    /// A missing result keeps qualification while attached. Withdrawal removes
    /// both contributions, and reactivation starts unqualified unless evaluated.
    /// No outcome count, fee, resource issuance, or benefit is created here.
    /// </summary>
    public static Relation Update(Relation use, bool active, bool? successful)
    {
        var parts = Parts(use);
        var admissibility = new List<IElement>();
        foreach (var state in new[] { Gate(use), Qualified(use) })
        {
            var binary = OperationClaim.AtMost(new Carrier(), state.ValueSlot,
                new CarrierLocation(state, 1), new Carrier());
            var quiet = OperationClaim.Equality(new Carrier(), state.LiveTensionSlot,
                new CarrierLocation(state, 0), new Carrier());
            admissibility.Add(binary);
            admissibility.Add(quiet);
            if (!Pass(binary) || !Pass(quiet))
                return G(use, G(admissibility.ToArray()), Relation.UnresolvedRead(use, use, state));
        }
        var desired = Parts((Relation)parts[4]);
        var desiredGate = (CarrierLocation)desired[0];
        var desiredQualified = (CarrierLocation)desired[1];
        desiredGate.Tick.Set(active ? 1 : 0);
        desiredQualified.Tick.Set(!active ? 0 : successful.HasValue ?
            successful.Value ? 1 : 0 : Qualified(use).ValueSlot.Ticks);
        var laws = Parts((Relation)parts[5]);
        var reads = N.ReadMany(laws);
        var deltas = new List<IElement>();
        foreach (var read in reads)
        {
            if (!Equation.TryReadPrediction(read, out var delta, out _))
                return G(use, G(admissibility.ToArray()), G(reads), new Placeholder());
            deltas.Add(new CarrierLocation(delta.Carrier, delta.Ticks));
        }
        var settlement = Carrier.ReceiveTogether(G(deltas.ToArray()));
        return G(use, G(admissibility.ToArray()), G(reads), settlement);
    }

    /// <summary>
    /// Supplied local topology, capped at eight neighbors. The numeric cursor
    /// participates in native successor/wrap equations; it is not a host index
    /// remembered outside this structure. Construction may enumerate neighbors;
    /// discovery never enumerates a world or an advertisement's consumers.
    /// Neighborhood = G(actor, G(adverts), cursor, length, successor equation).
    /// </summary>
    public static Relation Neighborhood(IElement actor, params Relation[] adverts)
    {
        if (adverts.Length > 8)
            throw new ArgumentOutOfRangeException(nameof(adverts), "This experimental local neighborhood admits at most eight neighbors.");
        // Reaching the same physical offer twice is not a second candidate.
        var unique = new List<Relation>();
        foreach (var advert in adverts)
            if (!unique.Any(existing => ReferenceEquals(existing, advert))) unique.Add(advert);
        var cursor = new Carrier();
        var length = new CarrierLocation(cursor, unique.Count);
        var successor = N.SumTo(cursor, (cursor.ValueSlot, 1), (new CarrierLocation(cursor, 1), 1));
        return G(actor, G(unique.ToArray()), cursor, length, successor);
    }

    public static Carrier Cursor(Relation neighborhood) => (Carrier)Parts(neighborhood)[2];

    /// <summary>
    /// Visit at most two advertisements. Ordered-neighbor traversal enumerates
    /// at most eight shallow references; no private service recipe is read.
    /// The result retains G(visited adverts, native cursor transition evidence).
    /// A single-neighbor neighborhood returns it once, never two votes.
    /// </summary>
    public static Relation ReadNext(Relation neighborhood, int maximum = 2)
    {
        if (maximum is < 0 or > 2)
            throw new ArgumentOutOfRangeException(nameof(maximum), "A discovery read admits zero, one, or two advertisements.");
        var parts = Parts(neighborhood);
        var neighbors = Parts((Relation)parts[1]);
        var cursor = (Carrier)parts[2];
        var length = (CarrierLocation)parts[3];
        var successor = (Relation)parts[4];
        var visited = new List<IElement>();
        var evidence = new List<IElement>();
        var quiet = OperationClaim.Equality(new Carrier(), cursor.LiveTensionSlot,
            new CarrierLocation(cursor, 0), new Carrier());
        evidence.Add(quiet);
        if (!Pass(quiet))
            return G(G(), G(quiet, Relation.UnresolvedRead(cursor.LiveTensionSlot, neighborhood, cursor)));
        for (var step = 0; step < maximum && step < neighbors.Length; step++)
        {
            // The integer is only local structural traversal at the currently
            // read coordinate. Arithmetic and wrap evidence remain native.
            var index = cursor.ValueSlot.Ticks;
            if (index < 0 || index >= neighbors.Length)
            {
                evidence.Add(Relation.UnresolvedRead(cursor.ValueSlot, neighborhood, cursor));
                break;
            }
            visited.Add(neighbors[index]);
            var read = N.Read(successor);
            evidence.Add(read);
            if (!Equation.TryReadPrediction(read, out var next, out _)) break;
            var wraps = OperationClaim.AtLeast(new Carrier(), next, length, new Carrier());
            evidence.Add(wraps);
            if (!OperationClaim.TryReadResolvedViolation(wraps, out var violation)) break;
            var target = violation.Ticks == 0 ? new CarrierLocation(cursor, 0) : next;
            var deltaLaw = N.SumTo(cursor, (target, 1), (cursor.ValueSlot, -1), (cursor.LiveTensionSlot, -1));
            var deltaRead = N.Read(deltaLaw);
            evidence.Add(G(deltaLaw, deltaRead));
            if (!Equation.TryReadPrediction(deltaRead, out var delta, out _)) break;
            evidence.Add(Carrier.ReceiveTogether(G(new CarrierLocation(cursor, delta.Ticks))));
        }
        return G(G(visited.ToArray()), G(evidence.ToArray()));
    }

    public static Relation[] Visited(Relation discoveryRead) =>
        Parts((Relation)Parts(discoveryRead)[0]).Cast<Relation>().ToArray();

    public static Relation Evidence(Relation discoveryRead) => (Relation)Parts(discoveryRead)[1];

    private static Carrier Holder(IElement element)
    {
        if (element is Carrier carrier) return carrier;
        if (element is Relation group && group.Right is Carrier holder && group.TryReadOrderedGroup(out _))
            return holder;
        throw new ArgumentException("Registration requires a carrier or an ordered consumer group with a stable unique holder.", nameof(element));
    }

    private static IElement[] Parts(Relation group) => N.Parts(group);
    private static Relation G(params IElement[] elements) => N.Group(elements);
    private static bool Pass(Relation claim) =>
        OperationClaim.TryReadResolvedViolation(claim, out var violation) && violation.Ticks == 0;
}
