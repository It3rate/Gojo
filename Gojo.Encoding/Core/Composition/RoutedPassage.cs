using Gojo.Core;
using Gojo.Patterns;
using Gojo.Encoding.Core.Native;

namespace Gojo.Encoding.Core.Composition;

/// <summary>
/// A selected use of finite connected paths. Construction history, permission
/// and material movement are separate connected surfaces. No glyph, domain
/// label or diagram coordinate selects a response. Packet transfers are atomic
/// between exposed ports, not travel or impact simulations along the path.
/// </summary>
public sealed class RoutedPassage
{
    // edge = actual site -> actual site
    // port = actual site -> exposed boundary slot
    // oriented edge use = actual edge -> (from site -> to site)
    // route = actual input port -> actual output port
    // path = actual route -> Closed(ordered oriented edge uses)
    // history = Closed(Closed(ordered oriented edge uses), ... pen lifts ...)
    // definition = (Closed(edges) -> (Closed(ports) -> (Closed(paths) -> history)))
    //              -> (packet-transport.Surface -> finite permission law)
    // permission law: (actual input port, control participation) -> route support.
    // A singleton closed support permits one selected route. Multiple outcomes
    // are alternatives, never simultaneous copies. A missing closed row blocks;
    // an open output keeps a missing law visible. History alone grants no route.
    public Relation Edges { get; }
    public Relation Ports { get; }
    public Relation Paths { get; }
    public Relation History { get; }
    public Relation Law { get; }
    public QualitativePacketTransport Transport { get; }
    public Relation Surface { get; }
    public Relation Timeline { get; private set; } = S();

    /// <summary>Exposes one actual site through its own boundary slot. The
    /// calling structure includes this relation in its exposed port inventory.
    /// This supplies no route, capacity or permission by itself.</summary>
    public static Relation Port(IElement site) => new(site, new Placeholder());

    public RoutedPassage(Relation edges, Relation ports, Relation paths, Relation history,
        Relation capacities, Relation law)
    {
        Edges = edges; Ports = ports; Paths = paths; History = history; Law = law;
        var routes = S(E(paths).OfType<Relation>().Select(path => path.Left).ToArray());
        Transport = new(capacities, routes, new Relation(edges, paths));
        Surface = new(new Relation(edges, new Relation(ports, new Relation(paths, history))),
            new Relation(Transport.Surface, law));
    }

    public sealed record Definition(Relation Edges, Relation Ports, Relation Paths, Relation History,
        Relation Transport, Relation Law);

    public static bool TryRead(Relation surface, out Definition definition)
    {
        definition = null!;
        if (surface.Left is not Relation { Left: Relation edges, Right: Relation { Left: Relation ports,
                Right: Relation { Left: Relation paths, Right: Relation history } } } ||
            surface.Right is not Relation { Left: Relation transport, Right: Relation law } ||
            !Closed(edges) || !Closed(ports) || !Closed(paths) || !Closed(history) ||
            E(edges).Any(edge => edge is not Relation) || E(ports).Any(port => port is not Relation) ||
            !FiniteLaw.TryRead(law, out var inputs, out _, out var outputs, out _) ||
            !SameEntries(inputs, ports) ||
            transport.Left is not Relation { Left: Relation import, Right: Relation transfer } ||
            transport.Right is not Relation { Left: Relation { Left: Relation capacities, Right: Relation routes } } ||
            !Closed(capacities) || !Closed(routes) || !SameEntries(outputs, routes) ||
            !ReferenceEquals(import.Right, capacities) || !ReferenceEquals(transfer.Left, capacities) || !ReferenceEquals(transfer.Right, routes) ||
            E(capacities).Any(row => row is not Relation { Right: Relation sites } || !Closed(sites) ||
                E(sites).Distinct(ParticipantIdentity.Comparer).Count() != E(sites).Length) ||
            !E(capacities).Cast<Relation>().Select(row => row.Left).SequenceEqual(E(ports), ReferenceEqualityComparer.Instance) ||
            E(paths).Any(path => path is not Relation) ||
            !E(paths).Cast<Relation>().Select(path => path.Left).SequenceEqual(E(routes), ReferenceEqualityComparer.Instance)) return false;
        var read = new Definition(edges, ports, paths, history, transport, law);
        foreach (var stroke in E(history))
        {
            if (stroke is not Relation uses || !Closed(uses) || E(uses) is not [Relation { Right: Relation first }, ..] ||
                !TryFollow(edges, uses, first.Left, out _)) return false;
        }
        if (E(routes).Any(route => route is not Relation actual || !RouteIsConnected(read, actual))) return false;
        definition = read; return true;
    }

    /// <summary>Route identity, port identity and every ordered site in the path
    /// must agree. Coordinate coincidence is neither inspected nor sufficient.</summary>
    public static bool RouteIsConnected(Definition definition, Relation route)
    {
        if (!E(definition.Ports).Any(port => ReferenceEquals(port, route.Left)) ||
            !E(definition.Ports).Any(port => ReferenceEquals(port, route.Right)) ||
            route.Left is not Relation fromPort || route.Right is not Relation toPort) return false;
        var matches = E(definition.Paths).OfType<Relation>().Where(row => ReferenceEquals(row.Left, route)).ToArray();
        if (matches is not [var path] || path.Right is not Relation steps || !Closed(steps) || E(steps).Length == 0) return false;
        return TryFollow(definition.Edges, steps, fromPort.Left, out var end) && ParticipantIdentity.Same(end, toPort.Left);
    }

    private static bool TryFollow(Relation edges, Relation steps, IElement start, out IElement end)
    {
        end = start;
        foreach (var step in E(steps))
        {
            if (step is not Relation { Left: Relation edge, Right: Relation direction } ||
                !E(edges).Any(actual => ReferenceEquals(actual, edge)) ||
                !ParticipantIdentity.Same(direction.Left, end) ||
                !(ParticipantIdentity.Same(direction.Left, edge.Left) && ParticipantIdentity.Same(direction.Right, edge.Right) ||
                  ParticipantIdentity.Same(direction.Left, edge.Right) && ParticipantIdentity.Same(direction.Right, edge.Left))) return false;
            end = direction.Right;
        }
        return true;
    }

    public Relation Import(Relation port, params IElement[] packets) =>
        Transport.Import(new Placeholder(), port, S(packets), new Relation(Surface, port));

    public sealed record Read(Relation Surface, Relation Incoming, Relation Candidates, Relation Accepted,
        Relation Blocked, Relation Remainder, Relation Unresolved, Relation Before, Relation After,
        Relation Receipt, string Outcome, bool OwnershipPreserved);

    /// <summary>Reads one control support at one or more inputs. All selected
    /// packets transfer atomically only when every local read has one closed,
    /// connected route. No arbitrary winner is picked from uncertain control.</summary>
    public Read Apply(Relation inputs, Relation control)
    {
        var before = Transport.State;
        var reads = new List<IElement>(); var packets = new List<IElement>(); var routes = new List<IElement>();
        var moves = new List<IElement>(); var pending = new List<IElement>(); var denied = new List<IElement>();
        var valid = TryRead(Surface, out _);
        var onlyClosedChoices = valid && Closed(inputs);
        if (!valid || !Closed(inputs)) pending.Add(Surface);
        foreach (var input in E(inputs))
        {
            var held = Transport.PortPackets(input);
            if (!Closed(held)) { pending.Add(held); onlyClosedChoices = false; continue; }
            packets.AddRange(E(held));
            var read = FiniteLaw.Evaluate(Law, S(input), control); reads.Add(read);
            if (!FiniteLaw.TryReadResult(read, out var support, out var unresolved, out _))
            { pending.Add(read); onlyClosedChoices = false; continue; }
            var choices = E(support); routes.AddRange(choices);
            if (!Closed(support) || E(unresolved).Length != 0) { pending.Add(read); onlyClosedChoices = false; continue; }
            // A union of possible routes loses the difference between
            // {move, stop} and {move}. Preserve every concrete control read:
            // execution needs agreement on the action, not just one route in
            // the union. An empty control support supplies no actual control.
            if (!Closed(control) || E(control).Length == 0)
            { pending.Add(new Relation(read, control)); onlyClosedChoices = false; continue; }
            var permitsStop = false; var unresolvedControl = false;
            foreach (var actual in E(control))
            {
                var local = FiniteLaw.Evaluate(Law, S(input), S(actual)); reads.Add(local);
                if (!FiniteLaw.TryReadResult(local, out var localSupport, out var localResidue, out _) ||
                    !Closed(localSupport) || E(localResidue).Length != 0)
                { pending.Add(local); unresolvedControl = true; onlyClosedChoices = false; continue; }
                permitsStop |= E(localSupport).Length == 0;
            }
            if (unresolvedControl) continue;
            if (choices.Length == 0) { denied.AddRange(E(held)); continue; }
            if (choices.Length != 1 || permitsStop) { pending.Add(read); continue; }
            if (choices[0] is not Relation route || !ReferenceEquals(route.Left, input))
            { pending.Add(read); onlyClosedChoices = false; continue; }
            moves.AddRange(E(held).Select(packet => (IElement)new Relation(packet, route)));
        }
        // Repeated inputs would duplicate a packet request. Preserve that as
        // unresolved rather than giving it two owners or silently deduplicating.
        if (packets.Distinct(ReferenceEqualityComparer.Instance).Count() != packets.Count)
        { pending.Add(inputs); onlyClosedChoices = false; }
        Relation accepted = S(), blocked = S(), remainder = S(), receipt = StructuralConstraint.Open();
        string outcome;
        if (pending.Count != 0)
        { remainder = S(packets.Distinct<IElement>(ReferenceEqualityComparer.Instance).ToArray()); outcome = onlyClosedChoices ? "choice" : "unknown"; }
        else if (denied.Count != 0)
        { blocked = S(denied.ToArray()); remainder = S(packets.Where(packet => !denied.Contains(packet, ReferenceEqualityComparer.Instance)).ToArray()); outcome = "blocked"; }
        else
        {
            receipt = Transport.Move(new Placeholder(), S(moves.ToArray()), new Relation(Surface, S(reads.ToArray())));
            Transport.TryReadAccounting(receipt, out _, out accepted, out blocked, out remainder);
            Transport.TryReadStep(receipt, out _, out _, out _, out _, out _, out var unresolved);
            pending.AddRange(E(unresolved)); outcome = E(accepted).Length == packets.Count ? "moved" : E(blocked).Length > 0 ? "blocked" : "unknown";
        }
        var incoming = S(packets.Distinct<IElement>(ReferenceEqualityComparer.Instance).ToArray());
        var candidates = S(routes.Distinct<IElement>(ReferenceEqualityComparer.Instance).ToArray());
        var unresolvedScope = S(pending.ToArray());
        var after = Transport.State;
        var witness = new Relation(new Relation(Surface, new Relation(before, new Relation(inputs, control))),
            new Relation(S(reads.ToArray()), new Relation(incoming, new Relation(candidates,
                new Relation(accepted, new Relation(blocked, new Relation(remainder,
                    new Relation(unresolvedScope, new Relation(receipt, after)))))))));
        Timeline = S([.. E(Timeline), witness]);
        var conserved = Transport.TryReadState(after, out _, out var placements, out var imported, out _) &&
            E(placements).Length == E(imported).Length &&
            E(placements).Cast<Relation>().Select(row => row.Left).Distinct(ReferenceEqualityComparer.Instance).Count() == E(imported).Length &&
            E(incoming).Length == E(accepted).Length + E(blocked).Length + E(remainder).Length;
        return new(witness, incoming, candidates, accepted, blocked, remainder, unresolvedScope,
            before, after, receipt, outcome, conserved);
    }

    public static Relation S(params IElement[] entries) => StructuralConstraint.Closed(entries);
    public static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
    public static bool Closed(Relation scope) => StructuralConstraint.TryReadScope(scope, out _, out var seam, out var tail) && seam.Equals(tail);
    private static bool SameEntries(Relation first, Relation second) => Closed(first) && Closed(second) &&
        E(first).SequenceEqual(E(second), ReferenceEqualityComparer.Instance);
}
