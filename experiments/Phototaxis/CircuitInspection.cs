using Gojo.Core;
using Gojo.Structures;
using static Phototaxis.Native;

namespace Phototaxis;

/// <summary>Diagnostic size of a selected processing graph, excluding utility/history and unit descriptors.</summary>
public readonly record struct CircuitGraphSize(int Relations, int Carriers, int Sites);

/// <summary>Local, read-only inspection of the protocol's exact circuit and component surfaces.</summary>
public static class CircuitInspection
{
    public static bool TryComponents(Relation circuit, out Relation[] components, out Relation output)
    {
        components = []; output = null!;
        var fields = Parts(circuit);
        if (fields.Length != 3 || fields[0] is not Relation group || fields[1] is not Relation equation || fields[2] is not Relation) return false;
        var terms = Parts(group);
        var found = new List<Relation>();
        foreach (var term in terms)
        {
            if (term is not Relation component) return false;
            var parts = Parts(component);
            if (parts.Length != 4 || parts[1] is not Carrier || parts[2] is not Relation || parts[3] is not Relation) return false;
            found.Add(component);
        }
        components = found.ToArray(); output = equation; return true;
    }

    public static Relation Observe(Relation circuit)
    {
        if (!TryComponents(circuit, out var components, out var output)) return G(circuit, new Placeholder());
        var inputs = components.SelectMany(c => new[] { Parts(c)[0], Parts(c)[2] }).Append(output).ToArray();
        var reads = ReadMany(inputs);
        var terms = components.Select((c, i) => (IElement)G(c, reads[i * 2],
            L((Carrier)Parts(c)[1], Held((Carrier)Parts(c)[1])), reads[i * 2 + 1])).ToArray();
        return G(circuit, G(terms), reads[^1]);
    }

    public static int SourceIndex(Relation catalog, IElement source) => Array.FindIndex(Parts(catalog), candidate =>
        ReferenceEquals(candidate, source) || candidate is CarrierLocation a && source is CarrierLocation b &&
        a.Carrier.SameIdentityAs(b.Carrier) && a.SameTickRefAs(b));

    public static bool TryLastSizeChange(Relation step, out int before, out int after)
    {
        before = after = 0;
        if (!TimeStepSequence.TryReadStep(step, out _, out _, out _, out var prior, out _, out var next, out _) ||
            prior is not Relation a || next is not Relation b ||
            !TryComponents(a, out var previous, out _) || !TryComponents(b, out var current, out _)) return false;
        before = previous.Length; after = current.Length; return true;
    }

    public static CircuitGraphSize Size(params Relation[] circuits)
    {
        var pending = new Stack<IElement>();
        foreach (var circuit in circuits)
        {
            if (!TryComponents(circuit, out var components, out var output)) continue;
            pending.Push(output);
            foreach (var component in components)
            {
                var parts = Parts(component);
                pending.Push(parts[0]); pending.Push(parts[1]); pending.Push(parts[2]);
            }
        }
        var relations = new HashSet<Relation>(ReferenceEqualityComparer.Instance);
        var carriers = new HashSet<Carrier>(ReferenceEqualityComparer.Instance);
        var sites = new HashSet<TickRef>(ReferenceEqualityComparer.Instance);
        while (pending.TryPop(out var element))
        {
            switch (element)
            {
                case Relation relation when relations.Add(relation):
                    pending.Push(relation.Left); pending.Push(relation.Right); break;
                case CarrierLocation location:
                    if (!location.Carrier.IsUnknown) sites.Add(location.Tick);
                    pending.Push(location.Carrier); break;
                case Carrier carrier when !carrier.IsUnknown && carriers.Add(carrier):
                    foreach (var attachment in carrier.AttachedRelations) pending.Push(attachment);
                    break;
            }
        }
        return new(relations.Count, carriers.Count, sites.Count);
    }
}
