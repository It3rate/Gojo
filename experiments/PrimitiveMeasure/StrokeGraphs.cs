namespace Gojo.Experiments.PrimitiveMeasure;

/// <summary>Run order is local catalogue order. The distinct tape of signed
/// run uses supplies writing history. Optional axis correspondence refers to
/// the two axes of a supplied local ground and never contributes an ink site.</summary>
public sealed class StrokeGraph
{
    private readonly LandmarkRun[] runs;
    private readonly EndpointRows writing = new(), axes = new(), crossings = new();
    public LetterGround? Frame { get; }
    public int SiteBits { get; }
    public int RunCount => runs.Length;
    public int RunBits { get; }
    public int PenBits { get; }
    public int AxisCount => axes.Count(2, RunBits);
    public StrokeGraph(LandmarkRun[] runs, int[] penUses, int[][] groundLinks,
        LetterGround? frame = null, int[][]? projectedCrossings = null)
    {
        this.runs = runs.ToArray();
        if (runs.Length == 0) throw new ArgumentException("Supply an ink run.");
        var sites = runs.SelectMany(r => r.Sites()).ToHashSet();
        SiteBits = OrderedBoundary.Bits(sites.Max() + 1);
        Frame = frame;
        if (frame != null && (frame.RunCount != runs.Length || frame.ZeroLinks().Any(row => !sites.Contains(row[0]))))
            throw new ArgumentException("The frame must refer to the supplied ink.");
        foreach (var pair in projectedCrossings ?? [])
        {
            if (pair.Length != 2 || pair[0] == pair[1] || pair.Any(s => !sites.Contains(s)))
                throw new ArgumentException("A projected crossing references two distinct ink sites.");
            bool HasInterior(int site) => runs.Any(r => r.IsChain && r.Sites()[1..^1].Contains(site));
            if (!pair.All(HasInterior) || runs.Any(r => pair.All(r.Sites().Contains)))
                throw new ArgumentException("This crossing surface requires separate interior cuts on distinct strands.");
            crossings.Append(SiteBits, pair);
        }
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
    public int[][] ProjectedCrossings() => Enumerable.Range(0, crossings.Count(2, SiteBits))
        .Select(i => crossings.Read(new(2, SiteBits, i))).ToArray();
    public byte[] CrossingBytes() => crossings.PackedBytes(2, SiteBits);

    private int[][] KnownAxes()
    {
        var links = GroundLinks().ToList();
        if (Frame != null)
            for (var i = 0; i < RunCount; i++)
            {
                var motion = Frame.Motion(i);
                if (motion[0] == 0) links.Add([i,1]);
                if (motion[1] == 0) links.Add([i,0]);
            }
        return links.ToArray();
    }

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
        var ground = KnownAxes(); var modelGround = model.KnownAxes();
        var projected = ProjectedCrossings(); var modelProjected = model.ProjectedCrossings();
        var zero = Frame?.ZeroLinks(); var modelZero = model.Frame?.ZeroLinks();
        // Runs in this construction have positive reach. A single such run
        // cannot lie along both perpendicular axes of the same selected basis.
        static bool ConflictingAxes(int[][] links) =>
            links.GroupBy(link => link[0]).Any(group => group.Select(link => link[1]).Distinct().Count() > 1);
        if (ConflictingAxes(ground) || ConflictingAxes(modelGround)) return false;
        return Match(0, new bool[RunCount], new Dictionary<int,int>());

        bool Match(int at, bool[] used, Dictionary<int,int> mapping)
        {
            if (at == RunCount)
            {
                // A correspondence of cuts never identifies their ink references.
                return projected.All(pair => modelProjected.Any(other =>
                    other.Contains(mapping[pair[0]]) && other.Contains(mapping[pair[1]])));
            }
            for (var m = 0; m < RunCount; m++)
            {
                if (used[m] || runs[at].Count != expected[m].Length) continue;
                if (ground.Any(g => g[0] == at && modelGround.Any(t => t[0] == m && t[1] != g[1]))) continue;
                foreach (var order in runs[at].Orders())
                foreach (var reverse in new[] { false, true })
                {
                    if (Frame != null && model.Frame != null &&
                        !Frame.Motion(at).SequenceEqual(model.Frame.Motion(m, reverse))) continue;
                    var extended = new Dictionary<int,int>(mapping);
                    var fits = true;
                    for (var i = 0; i < order.Length && fits; i++)
                    {
                        var target = expected[m][reverse ? order.Length - 1 - i : i];
                        if (zero != null && modelZero != null &&
                            zero.Any(z => z[0] == order[i] && !modelZero.Any(t => t[0] == target && t[1] == z[1])))
                        {
                            fits = false;
                            break;
                        }
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

    public static StrokeGraph Build(string label, bool ground = false, bool otherHand = false, bool partialE = false)
    {
        if (!Labels.Contains(label)) throw new ArgumentOutOfRangeException(nameof(label));
        return StraightCapitals.EarlierStudy(label, ground, otherHand, partialE);
    }
}
