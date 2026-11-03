namespace Gojo.Experiments.PrimitiveMeasure;

public readonly record struct InkSpan(int Run, int Slot, int From, int To, bool Reversed = false)
{
    public InkSpan Backwards() => new(Run, Slot, To, From, !Reversed);
}

/// <summary>Derived higher-layer walk. Every use names the original ink run and
/// span slot, and records which way to traverse it.</summary>
public sealed class InkLoop
{
    public IReadOnlyList<InkSpan> Walk { get; }
    public int Bits { get; }
    private readonly EndpointRows rows = new();
    internal InkLoop(IEnumerable<InkSpan> walk, int referenceBits)
    {
        Walk = Array.AsReadOnly(walk.ToArray());
        Bits = referenceBits;
        foreach (var span in Walk) rows.Append(Bits, span.Run, span.Slot, span.Reversed ? 1 : 0);
    }
    public byte[] Bytes() => rows.PackedBytes(3, Bits);
    public int[] Sites() => Walk.Select(s => s.From).Append(Walk[^1].To).ToArray();
}

public readonly record struct InkJoint(int Site, int Ends, int Through, bool ProvedCorner)
{
    public int Arms => Ends + 2 * Through;
}

/// <summary>Read-only feature surface for a supplied straight-run decomposition.
/// The forest supplies one independent closed walk per non-tree span. All
/// original edges, including those used by a loop, remain in their ink owner.</summary>
public sealed class StrokeFeatures : IPrimitiveReadPort
{
    public IReadOnlyList<InkLoop> Loops { get; }
    public IReadOnlyList<InkJoint> Joints { get; }
    public bool ExactWalks { get; }
    private readonly byte loopSupport;
    public byte ReadSupport() => loopSupport;

    public StrokeFeatures(StrokeGraph graph)
    {
        var orders = Enumerable.Range(0, graph.RunCount).Select(i => graph.Run(i).Orders().Take(2).ToArray()).ToArray();
        var valid = orders.All(group => group.Length > 0);
        var rank = graph.Incidence().Cycles;
        loopSupport = !valid ? (byte)0 : rank == 0 ? (byte)1 : (byte)2;
        ExactWalks = orders.All(group => group.Length == 1);
        var loops = new List<InkLoop>();
        var joints = new List<InkJoint>();
        Loops = loops.AsReadOnly();
        Joints = joints.AsReadOnly();
        if (!ExactWalks) return;

        var tree = new Dictionary<int,List<InkSpan>>();
        var ends = new Dictionary<int,List<int>>();
        var through = new Dictionary<int,int>();
        var width = Math.Max(graph.RunBits, OrderedBoundary.Bits(orders.Max(g => g[0].Length - 1)));
        for (var run = 0; run < orders.Length; run++)
        {
            var sites = orders[run][0];
            for (var i = 0; i < sites.Length; i++)
            {
                var site = sites[i];
                if (!tree.ContainsKey(site))
                {
                    tree.Add(site, []);
                    ends.Add(site, []);
                    through.Add(site, 0);
                }
                if (i == 0 || i == sites.Length - 1) ends[site].Add(run);
                else through[site]++;
                if (i == 0) continue;
                var edge = new InkSpan(run, i - 1, sites[i - 1], site);
                var back = TreePath(tree, site, edge.From);
                if (back != null) loops.Add(new InkLoop(back.Prepend(edge), width));
                else
                {
                    tree[edge.From].Add(edge);
                    tree[site].Add(edge.Backwards());
                }
            }
        }
        foreach (var (site, arriving) in ends)
        {
            var corner = false;
            if (graph.Frame is { } frame && through[site] == 0 && arriving.Count == 2)
            {
                // Collinearity requires identical sign patterns up to reversal.
                var first = frame.Motion(arriving[0]);
                corner = !first.SequenceEqual(frame.Motion(arriving[1])) &&
                    !first.SequenceEqual(frame.Motion(arriving[1], reversed: true));
            }
            joints.Add(new(site, arriving.Count, through[site], corner));
        }
    }

    private static InkSpan[]? TreePath(Dictionary<int,List<InkSpan>> tree, int start, int end)
    {
        var visited = new HashSet<int> { start };
        var parent = new Dictionary<int,InkSpan>();
        var pending = new Queue<int>();
        pending.Enqueue(start);
        while (pending.TryDequeue(out var site))
        {
            if (site == end)
            {
                var path = new List<InkSpan>();
                while (site != start)
                {
                    var edge = parent[site];
                    path.Add(edge);
                    site = edge.From;
                }
                path.Reverse();
                return path.ToArray();
            }
            foreach (var edge in tree[site])
                if (visited.Add(edge.To))
                {
                    parent.Add(edge.To, edge);
                    pending.Enqueue(edge.To);
                }
        }
        return null;
    }
}
