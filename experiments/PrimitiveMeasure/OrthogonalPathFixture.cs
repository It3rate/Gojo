namespace Gojo.Experiments.PrimitiveMeasure;

/// <summary>A narrow observation-only geometry contract, not a flow simulator.
/// Four array positions supply straight-edge frames [+u,+v,-u,-v] in one
/// Euclidean plane with perpendicular unit axes. These are NOT O/D codes.
/// Each row is [start corner, end corner, shared reach]. Local corners are 0..3;
/// reach references 4..7 denote shared nonnegative unknown lengths at this read.
/// The sources expose only zero/nonzero support; no numeric length is stored.
/// The selected geometric interpretation is an explicit premise of this fixture.</summary>
public sealed class OrthogonalPathFixture
{
    private readonly EndpointRows[] lanes = [new(), new(), new(), new()];
    private readonly IPrimitiveReadPort[] reaches;

    public OrthogonalPathFixture(params IPrimitiveReadPort[] reaches)
    {
        ArgumentNullException.ThrowIfNull(reaches);
        if (reaches.Length is < 1 or > 4 || reaches.Any(r => r is null))
            throw new ArgumentException("Supply one to four local reach read surfaces.", nameof(reaches));
        this.reaches = reaches.ToArray();
    }

    public ConnectionRow Connect(int lane, int start, int end, int reach)
    {
        if (lane is < 0 or > 3 || start is < 0 or > 3 || end is < 0 or > 3 ||
            reach < 4 || reach >= 4 + reaches.Length)
            throw new ArgumentOutOfRangeException(nameof(lane), "Reference is outside the local geometry layout.");
        return lanes[lane].Append(3, start, end, reach);
    }

    public byte[] PackedLane(int lane) => lanes[lane].PackedBytes(3, 3);
    public int[] Wiring(int lane, int index = 0) => lanes[lane].Read(new(3, 3, index));

    /// <summary>Conservative local read. It recognizes precisely one edge per
    /// quarter-turn lane, a four-corner cycle, and reused opposite reaches.
    /// It does not solve unknown equalities or infer a metric from ODV values.</summary>
    public OrthogonalPathRead Read()
    {
        if (lanes.Any(l => l.Count(3, 3) != 1)) return new(false, false, null);
        var edges = Enumerable.Range(0, 4).Select(i => Wiring(i)).ToArray();
        var cycle = edges.Select(e => e[0]).Distinct().Count() == 4 &&
            Enumerable.Range(0, 4).All(i => edges[i][1] == edges[(i + 1) % 4][0]);
        var shared = edges[0][2] == edges[2][2] && edges[1][2] == edges[3][2];
        if (!cycle || !shared) return new(cycle, shared, null);

        // Capture each used read surface once. Repeated references share one
        // candidate; equal bits at distinct references do not share a length.
        var supports = edges.Select(e => e[2]).Distinct()
            .Select(r => reaches[r - 4].ReadSupport()).ToArray();
        if (supports.Any(s => (s & ~3) != 0))
            throw new InvalidOperationException("Reach reads must use fixed positive O/D, words 000 or 001.");
        if (supports.Any(s => s == 0)) return new(cycle, shared, 0);
        byte presence = 0;
        if (supports.Any(s => (s & 1) != 0)) presence |= 1;
        if (supports.All(s => (s & 2) != 0)) presence |= 2;
        return new(cycle, shared, presence);
    }
}

/// <summary>Derived diagnostics, never stored shape tags. Presence bit 0 allows
/// a degenerate boundary, bit 1 a nondegenerate rectangle. Null means the narrow
/// reader cannot establish the required closed construction; zero means no
/// admitted reach observation. None of these cases manufactures a zero reach.</summary>
public readonly record struct OrthogonalPathRead(
    bool FourCornerCycle, bool SharedOppositeReaches, byte? NondegeneracySupport)
{
    public bool GuaranteesRectangle => NondegeneracySupport == 2;
}
