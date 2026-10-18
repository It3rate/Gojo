using Gojo.Core;
using N = Gojo.Experiments.QualitativeFrieze.FieldNative;

namespace Gojo.Experiments.QualitativeFrieze;

/// <summary>A new construction topology read from a particular observation
/// cover. Each group receives one fresh representative. Boundary contact and
/// shared chamber membership remain separate relations; neither identifies
/// representatives nor inserts a physical junction into the source ornament.
/// No planar embedding, cells, metric centre or centre-to-centre geometry is
/// inferred from this possibly overlapping network.</summary>
public sealed class OrnamentViewNetwork
{
    // representative = actual accepted view group read -> fresh role
    // contact edge = actual view adjacency witness -> (from role -> to role)
    // overlap edge = actual view overlap witness -> (from role -> to role)
    // topology = sites -> (Closed(contact edge -> (from role -> to role)) -> Closed())
    // The empty final scope supplies no face claims. Overlap edges are retained
    // separately and do not masquerade as boundary-contact edges in topology.
    // Surface = (source.Evaluate() -> context) ->
    //   (topology -> (representatives -> (contacts -> (overlaps ->
    //                           (retained source -> (blocked -> unresolved))))))
    public OrnamentView Source { get; }
    public IElement Context { get; }
    public Relation Representatives { get; }
    public Relation Sites { get; }
    public Relation Contacts { get; }
    public Relation Overlaps { get; }
    public Relation Topology { get; }
    public Relation Retained { get; }
    public Relation Blocked { get; }
    public Relation Unresolved { get; }
    public Relation Surface { get; }
    public bool IsComplete => Source.IsComplete && N.E(Blocked).Length == 0 && N.E(Unresolved).Length == 0;

    public OrnamentViewNetwork(OrnamentView view, IElement context)
    {
        Source = view; Context = context;
        var representatives = new List<IElement>(); var contacts = new List<IElement>(); var overlaps = new List<IElement>();
        var blocked = new List<IElement>(); var pending = new List<IElement>();
        var roles = new Dictionary<Relation, IElement>(ReferenceEqualityComparer.Instance);
        if (!view.IsComplete) pending.Add(view.Evaluate());
        else
        {
            foreach (var group in N.E(view.Groups))
            {
                if (group is not Relation actual || !view.TryReadGroup(actual, out _))
                { pending.Add(new Relation(group, view.Groups)); continue; }
                var role = new Placeholder(); roles.Add(actual, role); representatives.Add(new Relation(actual, role));
            }
            foreach (var adjacency in N.E(view.Adjacency))
            {
                if (adjacency is not Relation actual || !view.TryReadAdjacency(actual, out var left, out var right, out _) ||
                    !roles.TryGetValue(left.Read, out var from) || !roles.TryGetValue(right.Read, out var to))
                { blocked.Add(new Relation(adjacency, view.Adjacency)); continue; }
                contacts.Add(new Relation(actual, new Relation(from, to)));
            }
            foreach (var overlap in N.E(view.Overlaps))
            {
                if (overlap is not Relation { Left: Relation { Left: Relation left, Right: Relation right } } actual ||
                    !roles.TryGetValue(left, out var from) || !roles.TryGetValue(right, out var to))
                { blocked.Add(new Relation(overlap, view.Overlaps)); continue; }
                overlaps.Add(new Relation(actual, new Relation(from, to)));
            }
        }
        Representatives = N.S(representatives.ToArray()); Sites = N.S(representatives.Cast<Relation>().Select(row => row.Right).ToArray());
        Contacts = N.S(contacts.ToArray()); Overlaps = N.S(overlaps.ToArray());
        Topology = new(Sites, new Relation(N.S(contacts.Cast<Relation>().Select(edge => (IElement)new Relation(edge, edge.Right)).ToArray()), N.S()));
        Retained = N.S(view.Source.Evaluate(), new Relation(view.Complement, view.Evaluate()));
        Blocked = N.S(blocked.ToArray()); Unresolved = N.S(pending.ToArray());
        Surface = new(new Relation(view.Evaluate(), context), new Relation(Topology,
            new Relation(Representatives, new Relation(Contacts, new Relation(Overlaps,
                new Relation(Retained, new Relation(Blocked, Unresolved)))))));
    }

    public Relation Evaluate() => Surface;
    public bool TryReadRepresentative(Relation actualRow, out Relation group, out IElement representative)
    {
        group = null!; representative = null!;
        if (!IsComplete || !N.E(Representatives).Any(actual => ReferenceEquals(actual, actualRow)) ||
            actualRow.Left is not Relation actualGroup || !Source.TryReadGroup(actualGroup, out _)) return false;
        group = actualGroup; representative = actualRow.Right; return true;
    }
    public bool TryReadContact(Relation actualEdge, out Relation sourceWitness, out IElement from, out IElement to) =>
        ReadEdge(Contacts, actualEdge, out sourceWitness, out from, out to);
    public bool TryReadOverlap(Relation actualEdge, out Relation sourceWitness, out IElement from, out IElement to) =>
        ReadEdge(Overlaps, actualEdge, out sourceWitness, out from, out to);
    private bool ReadEdge(Relation scope, Relation actualEdge, out Relation witness, out IElement from, out IElement to)
    {
        witness = null!; from = to = null!;
        if (!IsComplete || !N.E(scope).Any(actual => ReferenceEquals(actual, actualEdge)) ||
            actualEdge is not { Left: Relation source, Right: Relation ends }) return false;
        witness = source; from = ends.Left; to = ends.Right; return true;
    }
}
