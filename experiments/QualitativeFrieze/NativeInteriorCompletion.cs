using Gojo.Core;
using Gojo.Patterns;

namespace Gojo.Experiments.QualitativeFrieze;

/// <summary>One-gap proposals over an existing observational subdivision.
/// Each added passage is a retained assumption in a separate context. The
/// source observation and its mechanical producer are never changed.</summary>
public sealed class NativeInteriorCompletion
{
    // proposal = (parent observation -> open component) ->
    //            (assumed closing passage -> proposed native observation)
    // result = proposal -> (new topology -> conditional feature reads)
    public NativeInteriorTopology Source { get; }
    public IReadOnlyList<Proposal> Proposals { get; }
    public Relation Unsearched { get; }
    public Relation Unresolved { get; }
    public Relation Surface { get; }

    public NativeInteriorCompletion(NativeInteriorTopology source, int proposalCapacity = 4, int faceCapacity = 96)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(proposalCapacity);
        Source = source;
        var proposals = new List<Proposal>(); var cuts = new List<IElement>(); var pending = new List<IElement>();
        var visited = new HashSet<NativeInteriorTopology.Node>();
        foreach (var seed in source.Nodes)
        {
            if (!visited.Add(seed)) continue;
            var nodes = new List<NativeInteriorTopology.Node> { seed };
            var edges = new HashSet<NativeInteriorTopology.Edge>();
            for (var index = 0; index < nodes.Count; index++)
                foreach (var edge in source.Edges.Where(edge => ReferenceEquals(edge.From, nodes[index]) || ReferenceEquals(edge.To, nodes[index])))
                {
                    edges.Add(edge); var other = ReferenceEquals(edge.From, nodes[index]) ? edge.To : edge.From;
                    if (visited.Add(other)) nodes.Add(other);
                }
            var ends = nodes.Where(node => edges.Count(edge => ReferenceEquals(edge.From, node) || ReferenceEquals(edge.To, node)) == 1).ToArray();
            // A single edge does not supply a polygonal enclosure. Branching
            // components need a separate hypothesis-selection mechanism.
            if (edges.Count < 2 || ends.Length != 2 || nodes.Any(node =>
                edges.Count(edge => ReferenceEquals(edge.From, node) || ReferenceEquals(edge.To, node)) > 2)) continue;
            var component = S(edges.Select(edge => (IElement)edge.Surface).ToArray());
            if (proposals.Count == proposalCapacity) { cuts.Add(component); continue; }
            if (ends[0].Word is null || ends[1].Word is null) { pending.Add(component); continue; }
            var frame = source.Source.Graph.Frame;
            var cancellation = frame.Reduce(ends[1].Word!, UnitArrangementRead.Inverse(frame, ends[0].Word!));
            if (!frame.TryReadResult(cancellation, out var word, out _, out var unknown) ||
                E(unknown).Length != 0 || !Closed(word) || E(word).Length == 0)
            { pending.Add(new Relation(component, cancellation)); continue; }
            var bridge = QualitativeStrokeGraph.Stroke(new Placeholder(), ends[0].Site, ends[1].Site, word);
            var additions = E(source.Graph.Strokes).Append(bridge).ToArray();
            // Unsupported producer passages are not present in the derived
            // split graph. Keep that discarded complement open in the proposed
            // observation, with its exact causes retained by the context below.
            var passages = E(source.EmbeddingUnknown).Length == 0 ? S(additions) : StructuralConstraint.Open(additions);
            var graph = new QualitativeStrokeGraph(frame, source.Graph.Sites, passages);
            var proposedSource = source.Source with { Graph = graph, Bindings = source.Bindings,
                StraightPassages = graph.Strokes, Provenance = S() };
            var context = new Relation(new Relation(source.Surface, component),
                new Relation(new Relation(bridge, cancellation), new Relation(graph.Surface, source.Bindings)));
            var topology = new NativeInteriorTopology(proposedSource, source.GeometryBasis, faceCapacity);
            var assumptions = S(new Relation(context, bridge));
            var features = new NativeInteriorFeatures(topology, assumptions);
            var reads = topology.Publication.TryMapStroke(bridge, out var published)
                ? features.Reads.Where(read => read.Boundary.Darts.Any(dart => E(dart.Edge.SourceUses).OfType<Relation>()
                    .Any(use => ReferenceEquals(use.Left, published)))).ToArray() : [];
            proposals.Add(new(context, bridge, assumptions, topology, features, reads));
        }
        Proposals = proposals; Unsearched = S(cuts.ToArray()); Unresolved = S(pending.ToArray());
        Surface = new(source.Surface, new Relation(S(proposals.Select(proposal => (IElement)proposal.Surface).ToArray()),
            new Relation(Unsearched, Unresolved)));
    }

    public sealed class Proposal(Relation context, Relation bridge, Relation assumptions,
        NativeInteriorTopology topology, NativeInteriorFeatures features, IReadOnlyList<NativeInteriorFeatures.BoundaryRead> reads)
    {
        public Relation Context { get; } = context;
        public Relation Bridge { get; } = bridge;
        public Relation Assumptions { get; } = assumptions;
        public NativeInteriorTopology Topology { get; } = topology;
        public NativeInteriorFeatures Features { get; } = features;
        public IReadOnlyList<NativeInteriorFeatures.BoundaryRead> Reads { get; } = reads;
        public Relation Surface { get; } = new(context, new Relation(assumptions,
            new Relation(topology.Surface, S(reads.Select(read => (IElement)read.Surface).ToArray()))));
    }

    private static Relation S(params IElement[] rows) => StructuralConstraint.Closed(rows);
    private static IElement[] E(Relation scope) => StructuralPotential.Items(scope);
    private static bool Closed(Relation scope) => StructuralConstraint.TryReadScope(scope, out _, out var a, out var b) && a.Equals(b);
}
