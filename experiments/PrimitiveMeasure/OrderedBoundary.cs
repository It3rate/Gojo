namespace Gojo.Experiments.PrimitiveMeasure;

/// <summary>One local owner's edge array and an ordered array of signed edge uses.
/// A use is (edge position &lt;&lt; 1) | reverse. Signs belong to incidence, not ODV
/// magnitude. Optional patch rows explicitly reference uses of its boundary.
/// No coordinates, geometric fill, weights, or hidden flow amount are stored.</summary>
public sealed class OrderedBoundary
{
    private readonly EndpointRows edges = new(), uses = new(), patch = new();
    public int SiteCount { get; }
    public int EdgeCount { get; }
    public int UseCount { get; }
    public int PatchUseCount { get; }
    public int SiteBits { get; }
    public int UseBits { get; }
    public int PatchBits { get; }

    public OrderedBoundary(int siteCount, int[][] endpoints, int[] signedUses, int[]? patchUses = null)
    {
        if (siteCount is < 1 or > 65536 || endpoints.Length > 32768 || signedUses.Length > 65536)
            throw new ArgumentOutOfRangeException(nameof(siteCount), "This packed fixture uses at most 16-bit references.");
        SiteCount = siteCount; EdgeCount = endpoints.Length; UseCount = signedUses.Length;
        SiteBits = Bits(siteCount); UseBits = Bits(Math.Max(2, EdgeCount * 2)); PatchBits = Bits(Math.Max(1, UseCount));
        foreach (var pair in endpoints)
        {
            if (pair.Length != 2 || pair.Any(s => s < 0 || s >= siteCount)) throw new ArgumentException("Edge endpoints must exist locally.");
            edges.Append(SiteBits, pair);
        }
        foreach (var use in signedUses)
        {
            if (use < 0 || (use >> 1) >= EdgeCount) throw new ArgumentException("Signed use must name an existing edge.");
            uses.Append(UseBits, use);
        }
        foreach (var use in patchUses ?? [])
        {
            if (use < 0 || use >= UseCount) throw new ArgumentException("Patch boundary must reference existing uses.");
            patch.Append(PatchBits, use);
        }
        PatchUseCount = patchUses?.Length ?? 0;
    }

    public static int Bits(int count) => Math.Max(1, 32 - System.Numerics.BitOperations.LeadingZeroCount((uint)Math.Max(0, count - 1)));
    public int[] Edge(int index) => edges.Read(new(2, SiteBits, index));
    public int SignedUse(int index) => uses.Read(new(1, UseBits, index))[0];
    public int PatchUse(int index) => patch.Read(new(1, PatchBits, index))[0];
    public int[] DirectedUse(int index)
    {
        var use = SignedUse(index); var pair = Edge(use >> 1);
        return (use & 1) == 0 ? pair : [pair[1], pair[0]];
    }
    public byte[] EdgeBytes() => edges.PackedBytes(2, SiteBits);
    public byte[] UseBytes() => uses.PackedBytes(1, UseBits);
    public byte[] PatchBytes() => patch.PackedBytes(1, PatchBits);
    public int PayloadBytes => (EdgeCount * 2 * SiteBits + 7) / 8 + (UseCount * UseBits + 7) / 8 + (PatchUseCount * PatchBits + 7) / 8;

    public BoundaryRead Read(int useBudget = int.MaxValue, bool patchBoundary = false)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(useBudget);
        var total = patchBoundary ? PatchUseCount : UseCount;
        var visited = Math.Min(total, useBudget);
        var balance = new Dictionary<int, int>(); var parity = new HashSet<int>();
        var parent = new Dictionary<int, int>(); var rank = new Dictionary<int, int>();
        int Find(int s)
        {
            if (!parent.ContainsKey(s)) { parent[s] = s; rank[s] = 0; }
            var root = s;
            while (parent[root] != root) root = parent[root];
            while (parent[s] != s) { var next = parent[s]; parent[s] = root; s = next; }
            return root;
        }
        void Add(int s, int sign)
        {
            balance[s] = checked(balance.GetValueOrDefault(s) + sign);
            if (!parity.Add(s)) parity.Remove(s);
        }
        int? first = null, previous = null; var ordered = true;
        for (var i = 0; i < visited; i++)
        {
            var pair = DirectedUse(patchBoundary ? PatchUse(i) : i);
            Add(pair[0], -1); Add(pair[1], 1);
            var a = Find(pair[0]); var b = Find(pair[1]);
            if (a != b)
            {
                if (rank[a] < rank[b]) parent[a] = b;
                else { parent[b] = a; if (rank[a] == rank[b]) rank[a]++; }
            }
            if (previous is not null && previous != pair[0]) ordered = false;
            first ??= pair[0]; previous = pair[1];
        }
        var complete = visited == total;
        var components = parent.Keys.ToArray().Select(Find).Distinct().Count();
        return new(visited, total, complete, balance, parity.Order().ToArray(), components,
            complete ? balance.Values.All(v => v == 0) : null,
            complete ? parity.Count == 0 : null,
            complete ? total > 0 && ordered && previous == first : null);
    }
}

/// <summary>Derived evidence. Counts are structural occurrences, never flow amounts.
/// Incomplete reads expose a prefix; they cannot certify the entire boundary.</summary>
public sealed record BoundaryRead(int Visited, int Total, bool Complete,
    IReadOnlyDictionary<int, int> Balance, int[] OddSites, int VisitedComponents,
    bool? Balanced, bool? EvenParity, bool? OrderedClosed);

/// <summary>Selected common-basis strict-order law. A positive presence support
/// enables the claim source &gt; target, not a material passage. Possible cycles
/// are conservative when uncertain claims have dependencies not supplied here.</summary>
public static class StrictOrderRead
{
    public static string Evaluate(OrderedBoundary structure, IReadOnlyList<byte> support)
    {
        if (support.Count != structure.UseCount || support.Any(s => s > 3)) throw new ArgumentException("One positive-context support per use is required.");
        bool HasCycle(bool possible)
        {
            var outgoing = new Dictionary<int, List<int>>(); var degree = new Dictionary<int, int>();
            for (var i = 0; i < support.Count; i++)
            {
                if (possible ? support[i] == 1 : support[i] != 2) continue;
                var pair = structure.DirectedUse(i);
                if (!outgoing.TryGetValue(pair[0], out var targets)) outgoing[pair[0]] = targets = [];
                targets.Add(pair[1]); degree.TryAdd(pair[0], 0);
                degree[pair[1]] = degree.GetValueOrDefault(pair[1]) + 1;
            }
            var ready = new Queue<int>(degree.Where(x => x.Value == 0).Select(x => x.Key)); var removed = 0;
            while (ready.TryDequeue(out var site))
            {
                removed++;
                if (!outgoing.TryGetValue(site, out var targets)) continue;
                foreach (var target in targets) if (--degree[target] == 0) ready.Enqueue(target);
            }
            return removed != degree.Count;
        }
        if (HasCycle(false)) return "Contradiction";
        if (HasCycle(true)) return "Unresolved";
        return "No strict-order cycle";
    }
}
