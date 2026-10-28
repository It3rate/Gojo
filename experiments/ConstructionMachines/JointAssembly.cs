using Gojo.Core;
using Gojo.Structures;
using static Gojo.Experiments.ConstructionMachines.MachineExpressions;

namespace Gojo.Experiments.ConstructionMachines;

/// <summary>Local readers over explicit node bindings, incidence and exposed roles.</summary>
public static class JointGraph
{
    // G(kinematic transform, node bindings, ordered edges, exposure bindings, provenance).
    public static Relation Create(Relation machine, Relation bindings, Relation edges,
        Relation exposures, IElement provenance) => G(machine, bindings, edges, exposures, provenance);
    public static Relation Machine(Relation definition) => (Relation)F(definition)[0];
    public static Relation[] Bindings(Relation definition) => F(F(definition)[1]).Cast<Relation>().ToArray();
    public static Relation[] Edges(Relation definition) => F(F(definition)[2]).Cast<Relation>().ToArray();
    public static Relation[] Exposures(Relation definition) => F(F(definition)[3]).Cast<Relation>().ToArray();
    public static IElement[] Nodes(Relation definition) => Bindings(definition).Select(b => b.Left).ToArray();

    public static bool Valid(Relation definition)
    {
        if (!definition.TryReadOrderedGroup(out var f) || f.Length != 5 || f[0] is not Relation machine ||
            !machine.TryReadOrderedGroup(out var m) || m.Length != 5 ||
            f.Skip(1).Take(3).Any(p => p is not Relation r || !r.TryReadOrderedGroup(out _))) return false;
        var bindings = Bindings(definition); var nodes = Nodes(definition); var exposures = Exposures(definition);
        if (nodes.Length == 0 || nodes.Any(n => n is not Placeholder) || nodes.Distinct(ReferenceEqualityComparer.Instance).Count() != nodes.Length ||
            exposures.Length == 0 || exposures.Any(e => e.Left is not Placeholder || !nodes.Any(n => ReferenceEquals(n, e.Right))) ||
            exposures.Select(e => e.Left).Distinct(ReferenceEqualityComparer.Instance).Count() != exposures.Length ||
            Edges(definition).Any(e => !nodes.Any(n => ReferenceEquals(n, e.Left)) || !nodes.Any(n => ReferenceEquals(n, e.Right)) || ReferenceEquals(e.Left, e.Right))) return false;
        if (m[1] is not Relation operation || !operation.TryReadOrderedGroup(out var outputs) || outputs.Length != exposures.Length) return false;
        // A port's connection to an internal node comes from its actual producing
        // expression, not from a coincident coordinate found after evaluation.
        return exposures.Select((e, i) => ReferenceEquals(outputs[i], bindings.Single(b => ReferenceEquals(b.Left, e.Right)).Right)).All(x => x);
    }

    public static Relation Observe(Relation definition, Relation requestedPaths)
    {
        var witnesses = new List<Relation>(); var captured = new List<IElement>(); var unresolved = new List<IElement>();
        foreach (var binding in Bindings(definition))
        {
            var value = Evaluate(binding.Right, [], witnesses);
            if (!MachineRunner.TryNumbers(value, out var coordinates)) { unresolved.Add(binding); captured.Add(new Relation(binding.Left, new Placeholder())); continue; }
            var scalars = F(value).Select(c =>
            {
                ProjectionResidue.TryRead(c, out var axis, out var n, out var d);
                return ProjectionResidue.Create(axis, n, d);
            }).ToArray();
            captured.Add(new Relation(binding.Left, Relation.OrderedGroup((Carrier)((Relation)value).Right, scalars)));
        }
        var paths = F(requestedPaths).Cast<Relation>().Select(query => ReadPath(definition, query)).ToArray();
        unresolved.AddRange(paths.Where(p => F(p)[2] is Placeholder));
        return ReadWitness.Create(new Carrier(), definition, G(F(definition)[3], requestedPaths),
            G(G(captured.ToArray()), F(definition)[2], G(paths)), G(unresolved.ToArray()), G(witnesses.ToArray()));
    }

    /// <summary>Selected undirected connectivity. Every traversed edge keeps its order and chosen direction.</summary>
    public static Relation ReadPath(Relation definition, Relation query)
    {
        var exposures = Exposures(definition);
        var from = exposures.SingleOrDefault(b => ReferenceEquals(b.Left, query.Left))?.Right;
        var to = exposures.SingleOrDefault(b => ReferenceEquals(b.Left, query.Right))?.Right;
        var route = new List<Relation>();
        if (from is null || to is null) return Pending();
        var seen = new HashSet<IElement>(ReferenceEqualityComparer.Instance) { from };
        var queue = new Queue<IElement>(); queue.Enqueue(from);
        var previous = new Dictionary<IElement, (IElement Node, Relation Edge)>(ReferenceEqualityComparer.Instance);
        while (queue.Count > 0 && !seen.Contains(to))
        {
            var current = queue.Dequeue();
            foreach (var edge in Edges(definition))
            {
                var next = ReferenceEquals(edge.Left, current) ? edge.Right : ReferenceEquals(edge.Right, current) ? edge.Left : null;
                if (next is null || !seen.Add(next)) continue;
                previous[next] = (current, edge); queue.Enqueue(next);
            }
        }
        if (!seen.Contains(to)) return Pending();
        for (var current = to; !ReferenceEquals(current, from);)
        {
            var back = previous[current];
            route.Add(new Relation(back.Edge, new Relation(back.Node, current))); current = back.Node;
        }
        route.Reverse();
        return ReadWitness.Create(new Carrier(), definition, query, G(route.ToArray()), G(), G(F(definition)[2]));

        Relation Pending() => ReadWitness.Create(new Carrier(), definition, query, new Placeholder(), G(query), G(F(definition)[2]));
    }

    // Covers all old/new participants exactly once. Group endpoints express
    // splitting/folding; an unbound endpoint expresses introduction/retirement.
    public static bool Covers(Relation map, IElement[] oldParts, IElement[] newParts)
    {
        var oldSeen = new HashSet<IElement>(ReferenceEqualityComparer.Instance);
        var newSeen = new HashSet<IElement>(ReferenceEqualityComparer.Instance);
        if (!map.TryReadOrderedGroup(out var rows)) return false;
        foreach (var row in rows)
        {
            if (row is not Relation relation || !Side(relation.Left, oldParts, oldSeen) || !Side(relation.Right, newParts, newSeen)) return false;
        }
        return oldSeen.Count == oldParts.Length && newSeen.Count == newParts.Length;

        static bool Side(IElement selected, IElement[] participants, HashSet<IElement> seen)
        {
            if (participants.Any(p => ReferenceEquals(p, selected))) return seen.Add(selected);
            if (selected is Placeholder) return true;
            return selected is Relation g && g.TryReadOrderedGroup(out var parts) && parts.Length > 0 && parts.All(p => Side(p, participants, seen));
        }
    }
}

/// <summary>
/// A native joint topology around the previous neutral mechanical interface.
/// This shell schedules and caches; its retained relations own the construction,
/// exposed-node correspondence, requested paths and rewrite history.
/// </summary>
public sealed class JointAssembly
{
    // G(definition, external-role/coordinate-port map, requested paths, clock,
    // current topology read, timeline, negotiating machine).
    public Relation Definition { get; private set; }
    public NegotiatingMachine Mechanical { get; }
    public Relation PortMap { get; }
    public Relation RequestedPaths { get; }
    public Relation CurrentRead { get; private set; }
    public Relation Timeline { get; private set; }
    public Relation Surface => G(Definition, PortMap, RequestedPaths, clock, CurrentRead, Timeline, Mechanical.Surface);
    private readonly Carrier clock;
    private readonly IElement initial;
    private readonly List<Relation> steps = [];

    public JointAssembly(Relation definition, Relation requestedPaths)
    {
        if (!JointGraph.Valid(definition)) throw new ArgumentException("The joint must connect actual nodes, edges and producer expressions.");
        Definition = definition; RequestedPaths = requestedPaths; clock = new Carrier();
        Mechanical = new NegotiatingMachine(JointGraph.Machine(definition));
        PortMap = G(JointGraph.Exposures(definition).Select((e, i) => (IElement)new Relation(e.Left,
            G(Mechanical.Port(i, 0), Mechanical.Port(i, 1)))).ToArray());
        CurrentRead = JointGraph.Observe(definition, requestedPaths); initial = CurrentRead;
        Timeline = TimeStepSequence.Create(new Carrier(), clock, initial);
    }

    private JointAssembly(Relation surface, NegotiatingMachine? mechanical)
    {
        var f = F(surface);
        if (f.Length != 7) throw new ArgumentException("An exact joint-instance surface is required.");
        Definition = (Relation)f[0]; PortMap = (Relation)f[1]; RequestedPaths = (Relation)f[2]; clock = (Carrier)f[3];
        CurrentRead = (Relation)f[4]; Timeline = (Relation)f[5]; Mechanical = mechanical ?? NegotiatingMachine.Restore((Relation)f[6]);
        if (!JointGraph.Valid(Definition) || !ReferenceEquals(Mechanical.Definition, JointGraph.Machine(Definition)) || !PortsMatch() ||
            !TimeStepSequence.TryRead(Timeline, out _, out var start, out var retained))
            throw new ArgumentException("The restored joint needs its actual mechanical correspondence and timeline.");
        initial = start; steps.AddRange(retained);
    }

    public static JointAssembly Restore(Relation surface, NegotiatingMachine? mechanical = null) => new(surface, mechanical);

    private bool PortsMatch()
    {
        var rows = F(PortMap); var exposures = JointGraph.Exposures(Definition);
        if (rows.Length != exposures.Length) return false;
        for (var i = 0; i < rows.Length; i++)
            if (rows[i] is not Relation map || !ReferenceEquals(map.Left, exposures[i].Left) ||
                map.Right is not Relation group || !group.TryReadOrderedGroup(out var pair) || pair.Length != 2 ||
                !ReferenceEquals(pair[0], Mechanical.Port(i, 0)) || !ReferenceEquals(pair[1], Mechanical.Port(i, 1))) return false;
        return true;
    }

    public Relation Step(NegotiationWorld world)
    {
        if (!world.Machines.Any(m => ReferenceEquals(m, Mechanical))) throw new ArgumentException("The schedule must use this actual mechanical instance.");
        return ObserveTransition(world.Step());
    }

    /// <summary>Observe an explicitly supplied mechanical transition without
    /// asking a second numerical scheduler to execute another round.</summary>
    public Relation ObserveTransition(IElement transition)
    {
        var before = CurrentRead;
        CurrentRead = JointGraph.Observe(Definition, RequestedPaths);
        return Record(before, transition, CurrentRead);
    }

    public bool TryRewrite(Relation candidate, Relation nodeMap, Relation edgeMap, out Relation witness)
    {
        var before = CurrentRead; var previous = Definition;
        var oldExposed = JointGraph.Exposures(previous);
        if (!JointGraph.Valid(candidate) || oldExposed.Length != JointGraph.Exposures(candidate).Length ||
            oldExposed.Where((e, i) => !ReferenceEquals(e.Left, JointGraph.Exposures(candidate)[i].Left)).Any() ||
            !JointGraph.Covers(nodeMap, JointGraph.Nodes(previous), JointGraph.Nodes(candidate)) ||
            !JointGraph.Covers(edgeMap, JointGraph.Edges(previous), JointGraph.Edges(candidate))) return Rejected(G(candidate, nodeMap, edgeMap), out witness);
        var read = JointGraph.Observe(candidate, RequestedPaths);
        if (F(F(read)[2]).Length != 3 || F(F(F(read)[2])[0]).Cast<Relation>().Any(b => !MachineRunner.TryNumbers(b.Right, out _)))
            return Rejected(read, out witness);
        if (!Mechanical.TryReplaceDefinition(JointGraph.Machine(candidate), out var replacement)) return Rejected(replacement, out witness);
        Definition = candidate; CurrentRead = read;
        var oldPorts = F(F(JointGraph.Machine(previous))[2]); var newPorts = F(F(JointGraph.Machine(candidate))[2]);
        var correspondence = G(nodeMap, edgeMap,
            G(oldPorts.Zip(newPorts).Select(pair => (IElement)new Relation(pair.First, pair.Second)).ToArray()), PortMap);
        var patch = TransformSurface.Patch(new Carrier(), previous, G(nodeMap, edgeMap), before,
            G(F(previous)[1], F(previous)[2]), G(), candidate, correspondence, CurrentRead, F(CurrentRead)[3], replacement);
        witness = ReadWitness.Create(new Carrier(), before, G(PortMap, RequestedPaths), CurrentRead, F(CurrentRead)[3], patch);
        Record(before, witness, CurrentRead);
        return true;

        bool Rejected(IElement remainder, out Relation declined)
        {
            declined = ReadWitness.Create(new Carrier(), before, G(PortMap, RequestedPaths), new Placeholder(), G(remainder), G(candidate, nodeMap, edgeMap));
            Record(before, declined, CurrentRead); return false;
        }
    }

    private Relation Record(IElement before, IElement transition, Relation after)
    {
        var step = TimeStepSequence.Step(new Carrier(), clock, (long)clock.Value, checked((long)clock.Value + 1),
            before, transition, after, F(after)[3]);
        clock.Add(1); steps.Add(step); Timeline = TimeStepSequence.Create(new Carrier(), clock, initial, steps.ToArray());
        return step;
    }
}
