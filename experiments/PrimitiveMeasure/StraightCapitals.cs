namespace Gojo.Experiments.PrimitiveMeasure;

/// <summary>Authored reference constructions. The matcher receives packed
/// graphs alone. Signed components below are qualitative comparisons with zero.</summary>
public static class StraightCapitals
{
    private sealed record Pattern(string Letter, string Name, int[][] Runs, (int X, int Y)[] Travel,
        int[] Left, int[] Bottom, bool SinglePen = false, int[][]? Crossings = null);

    private static readonly Pattern[] Patterns =
    [
        new("A","A", [[0,1,2],[0,3,4],[1,3]], [(-1,-1),(1,-1),(1,0)], [2], [2,4]),
        new("E","E", [[0,1,2],[0,3],[1,4],[2,5]], [(0,-1),(1,0),(1,0),(1,0)], [0,1,2], [2,5]),
        new("F","F", [[0,1,2],[0,3],[1,4]], [(0,-1),(1,0),(1,0)], [0,1,2], [2]),
        new("H","H", [[0,1,2],[3,4,5],[1,4]], [(0,-1),(0,-1),(1,0)], [0,1,2], [2,5]),
        new("I","I", [[0,1]], [(0,-1)], [0,1], [1]),
        new("I","Barred I", [[0,1,2],[3,4,5],[1,4]], [(1,0),(1,0),(0,-1)], [0,3], [3,4,5]),
        new("K","K", [[0,1,2],[1,3],[1,4]], [(0,-1),(1,1),(1,-1)], [0,1,2], [2,4]),
        new("L","L", [[0,1],[1,2]], [(0,-1),(1,0)], [0,1], [1,2], true),
        new("M","M", [[0,1],[1,2],[2,3],[3,4]], [(0,1),(1,-1),(1,1),(0,-1)], [0,1], [0,4], true),
        new("N","N", [[0,1],[1,2],[2,3]], [(0,1),(1,-1),(0,1)], [0,1], [0,2], true),
        new("T","T", [[0,1,2],[1,3]], [(1,0),(0,-1)], [0], [3]),
        new("V","V", [[0,1],[1,2]], [(1,-1),(1,1)], [0], [1], true),
        new("W","W", [[0,1],[1,2],[2,3],[3,4]], [(1,-1),(1,1),(1,-1),(1,1)], [0], [1,3], true),
        new("X","X", [[0,1,2],[3,4,5]], [(1,-1),(1,1)], [0,3], [2,3], Crossings: [[1,4]]),
        new("X","Joined X", [[0,1,2],[3,1,4]], [(1,-1),(1,1)], [0,3], [2,3]),
        new("Y","Y", [[0,1],[2,1],[1,3]], [(1,-1),(-1,-1),(0,-1)], [0], [3]),
        new("Z","Z", [[0,1],[1,2],[2,3]], [(1,0),(-1,-1),(1,0)], [0,2], [2,3], true)
    ];
    public static IReadOnlyList<string> Names { get; } = Array.AsReadOnly(Patterns.Select(p => p.Name).ToArray());
    private static readonly StrokeGraph[] Models = Patterns.Select(p => Construct(p, true, false, false, false)).ToArray();
    private static readonly StrokeFeatures[] ModelFeatures = Models.Select(model => new StrokeFeatures(model)).ToArray();
    public static StrokeGraph Build(string name, bool convention = true, bool otherHand = false) =>
        Construct(Patterns.Single(p => p.Name == name), convention, false, otherHand, false);

    internal static StrokeGraph EarlierStudy(string name, bool axes, bool otherHand, bool partialE) =>
        Construct(Patterns.Single(p => p.Name == name), false, axes, otherHand, partialE);

    private static StrokeGraph Construct(Pattern p, bool convention, bool axes, bool otherHand, bool partialE)
    {
        var bits = OrderedBoundary.Bits(p.Runs.SelectMany(r => r).Max() + 1);
        var runs = p.Runs.Select(r => new LandmarkRun(bits, r)).ToArray();
        if (p.Letter == "E" && partialE) runs[0] = new(bits, [2,0,1], [[0,1],[0,2]]);
        var sequence = Enumerable.Range(0, runs.Length);
        if (otherHand) sequence = sequence.Reverse();
        var tape = sequence.Select((r,i) => (r << 2) | (!p.SinglePen || i == 0 ? 2 : 0) | (otherHand ? 1 : 0)).ToArray();
        var links = axes ? Enumerable.Range(0, runs.Length)
            .Where(r => p.Travel[r].X == 0 || p.Travel[r].Y == 0)
            .Select(r => new[] { r, p.Travel[r].X == 0 ? 1 : 0 }).ToArray() : [];
        return new(runs, tape, links, convention ? new LetterGround(p.Travel, p.Left, p.Bottom, bits) : null, p.Crossings);
    }

    public static string[] Candidates(StrokeGraph input) => Patterns.Where((_, i) => input.Fits(Models[i]))
        .Select(p => p.Letter).Distinct().ToArray();
    public static string[] CandidatesByLoop(bool present) => Patterns.Where((_, i) =>
        ModelFeatures[i].ReadSupport() == (present ? 2 : 1)).Select(p => p.Letter).Distinct().ToArray();
}
