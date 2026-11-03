namespace Gojo.Experiments.PrimitiveMeasure;

/// <summary>Run order is local catalogue order. The distinct tape of signed
/// run uses supplies writing history. Optional axis correspondence refers to
/// the two axes of a supplied local ground and never contributes an ink site.</summary>
public sealed class StrokeGraph
{
    private readonly LandmarkRun[] runs;
    private readonly EndpointRows writing = new(), axes = new();
    public int RunCount => runs.Length;
    public int RunBits { get; }
    public int PenBits { get; }
    public int AxisCount => axes.Count(2, RunBits);
    public StrokeGraph(LandmarkRun[] runs, int[] penUses, int[][] groundLinks)
    {
        this.runs = runs.ToArray();
        if (runs.Length == 0) throw new ArgumentException("Supply an ink run.");
        RunBits = OrderedBoundary.Bits(runs.Length);
        PenBits = RunBits + 2;
        foreach (var code in penUses)
        {
            if (code < 0 || code >> 2 >= runs.Length) throw new ArgumentOutOfRangeException(nameof(penUses));
            writing.Append(PenBits, code);
        }
        foreach (var row in groundLinks)
        {
            if (row.Length != 2 || (uint)row[0] >= runs.Length || row[1] is < 0 or > 1)
                throw new ArgumentException("Expected [ink run, local axis].");
            axes.Append(RunBits, row);
        }
    }
    public LandmarkRun Run(int i) => runs[i];
    public int[] PenUses() => Enumerable.Range(0, writing.Count(1, PenBits)).Select(i => writing.Read(new(1, PenBits, i))[0]).ToArray();
    public int[][] GroundLinks() => Enumerable.Range(0, AxisCount).Select(i => axes.Read(new(2, RunBits, i))).ToArray();
    public byte[] PenBytes() => writing.PackedBytes(1, PenBits);
    public byte[] GroundBytes() => axes.PackedBytes(2, RunBits);

    // Each run is a path connecting its members in every admitted order.
    // The count of independent undirected cycles is edges - vertices + components.
    public (int Sites, int SharedSites, int Components, int Cycles) Incidence()
    {
        var groups = runs.Select(r => r.Sites()).ToArray();
        var membership = groups.SelectMany(g => g).GroupBy(i => i).ToArray();
        var remaining = membership.Select(g => g.Key).ToHashSet();
        var components = 0;
        while (remaining.Count != 0)
        {
            components++;
            var pending = new Stack<int>();
            pending.Push(remaining.First());
            while (pending.TryPop(out var site))
            {
                if (!remaining.Remove(site)) continue;
                foreach (var run in groups.Where(g => g.Contains(site)))
                    foreach (var other in run) if (remaining.Contains(other)) pending.Push(other);
            }
        }
        return (membership.Length, membership.Count(g => g.Count() > 1), components,
            groups.Sum(g => g.Length - 1) - membership.Length + components);
    }

    /// <summary>Compare with a completely ordered construction, permitting
    /// run renumbering, reversal and a bijection of site references. A partial
    /// input fits when at least one consistent completion supplies the mapping.</summary>
    public bool Fits(StrokeGraph model)
    {
        if (RunCount != model.RunCount || Incidence() != model.Incidence()) return false;
        var expected = model.runs.Select(r => r.IsChain ? r.Sites() : throw new ArgumentException("Expected fully ordered model runs.")).ToArray();
        var ground = GroundLinks(); var modelGround = model.GroundLinks();
        // Runs in this construction have positive reach. A single such run
        // cannot lie along both perpendicular axes of the same selected basis.
        static bool ConflictingAxes(int[][] links) =>
            links.GroupBy(link => link[0]).Any(group => group.Select(link => link[1]).Distinct().Count() > 1);
        if (ConflictingAxes(ground) || ConflictingAxes(modelGround)) return false;
        return Match(0, new bool[RunCount], new Dictionary<int,int>());

        bool Match(int at, bool[] used, Dictionary<int,int> mapping)
        {
            if (at == RunCount) return true;
            for (var m = 0; m < RunCount; m++)
            {
                if (used[m] || runs[at].Count != expected[m].Length) continue;
                if (ground.Any(g => g[0] == at && modelGround.Any(t => t[0] == m && t[1] != g[1]))) continue;
                foreach (var order in runs[at].Orders())
                foreach (var reverse in new[] { false, true })
                {
                    var extended = new Dictionary<int,int>(mapping);
                    var fits = true;
                    for (var i = 0; i < order.Length && fits; i++)
                    {
                        var target = expected[m][reverse ? order.Length - 1 - i : i];
                        if (extended.TryGetValue(order[i], out var mapped)) fits = mapped == target;
                        else if (extended.ContainsValue(target)) fits = false;
                        else extended.Add(order[i], target);
                    }
                    if (!fits) continue;
                    used[m] = true;
                    if (Match(at + 1, used, extended)) return true;
                    used[m] = false;
                }
            }
            return false;
        }
    }
}

public static class CapitalStudy
{
    public static IReadOnlyList<string> Labels { get; } = Array.AsReadOnly(new[] { "H", "A", "E", "N", "Z" });
    private static readonly StrokeGraph[] Models = Labels.Select(label => Build(label, ground: true)).ToArray();
    public static string[] Candidates(StrokeGraph input) =>
        Labels.Where((_, i) => input.Fits(Models[i])).ToArray();

    // Names select authored examples only. Fits receives the graph alone.
    public static StrokeGraph Build(string label, bool ground = false, bool otherHand = false, bool partialE = false)
    {
        int[][] paths = label switch
        {
            "H" => [[0,1,2], [3,4,5], [1,4]],
            "A" => [[0,1,2], [0,3,4], [1,3]],
            "E" => [[0,1,2], [0,3], [1,4], [2,5]],
            "N" or "Z" => [[0,1], [1,2], [2,3]],
            _ => throw new ArgumentOutOfRangeException(nameof(label))
        };
        var bits = OrderedBoundary.Bits(paths.SelectMany(p => p).Max() + 1);
        var runs = paths.Select(path => new LandmarkRun(bits, path)).ToArray();
        if (label == "E" && partialE) runs[0] = new LandmarkRun(bits, [2,0,1], [[0,1],[0,2]]);
        var single = label is "N" or "Z";
        var sequence = Enumerable.Range(0, paths.Length);
        if (otherHand) sequence = sequence.Reverse();
        var tape = sequence.Select((run, i) => (run << 2) | ((!single || i == 0) ? 2 : 0) | (otherHand ? 1 : 0)).ToArray();
        int[][] axes = !ground ? [] : label switch
        {
            "H" => [[0,1], [1,1], [2,0]],
            "A" => [[2,0]],
            "E" => [[0,1], [1,0], [2,0], [3,0]],
            "N" => [[0,1], [2,1]],
            _ => [[0,0], [2,0]]
        };
        return new StrokeGraph(runs, tape, axes);
    }
}
