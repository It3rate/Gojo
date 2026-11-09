namespace Gojo.EncodingB;

/// <summary>One unit moved between a pair of gaps.</summary>
public sealed record GapMove(int Row, int From, int To, int FromBefore, int ToBefore, long SquareSumBefore, long SquareSumAfter);

/// <summary>A settled, quiet state and why it stopped. Quiet is not optimal: a
/// nonzero spread is a legitimate result, and the witness names the missing
/// routes that would lower it.</summary>
public sealed record GapSettlement(
    IReadOnlyList<GapMove> Moves,
    bool Quiet,
    int Spread,
    long SquareSum,
    IReadOnlyList<(int A, int B)> MissingRoutes);

/// <summary>Balancing thermometer-coded gaps along explicit pair rows: descent
/// with a schedule (C3). A move from a to b is admissible when a exceeds b by at
/// least two, tested with two bit operations; the move shifts a unit out of a and
/// into b. Each move lowers the sum of squares by at least two, so settling
/// terminates. A move depends on a neighbour's state, so order can matter: the
/// rotation pointer is state, recorded and replayed.</summary>
public sealed class GapBalance
{
    private readonly string[] planes;

    public GapBalance(Tile tile, string thermometerField, string pairRows)
    {
        ArgumentNullException.ThrowIfNull(tile);
        Tile = tile;
        planes = tile.Grammar.PlanesOf(thermometerField).ToArray();
        Pairs = tile.Rows(pairRows);
        if (Pairs.Roles.Count != 2) throw new ArgumentException("Pairs are two-site rows.", nameof(pairRows));
    }

    public Tile Tile { get; }
    public RowSet Pairs { get; }

    /// <summary>The next pair row to examine: schedule state.</summary>
    public int Pointer { get; set; }

    public int Gap(int site) =>
        Thermometer.Decode(Tile.Gather(Tile, planes, site)) ?? throw new InvalidOperationException($"Broken thermometer at site {site}.");

    public long SquareSum => Enumerable.Range(0, Tile.SiteCount).Sum(s => (long)Gap(s) * Gap(s));

    public int Spread
    {
        get
        {
            var gaps = Enumerable.Range(0, Tile.SiteCount).Select(Gap).ToArray();
            return gaps.Max() - gaps.Min();
        }
    }

    /// <summary>Examines the pair at the pointer, moves one unit if admissible,
    /// and advances the pointer.</summary>
    public GapMove? Step()
    {
        if (Pairs.Count == 0) return null;
        var row = Pointer;
        Pointer = (Pointer + 1) % Pairs.Count;
        var pair = Pairs.Row(row);
        var ta = Tile.Gather(Tile, planes, pair[0]);
        var tb = Tile.Gather(Tile, planes, pair[1]);
        int from, to;
        if (Thermometer.ExceedsByTwo(ta, tb)) (from, to) = (pair[0], pair[1]);
        else if (Thermometer.ExceedsByTwo(tb, ta)) (from, to) = (pair[1], pair[0]);
        else return null;
        var before = SquareSum;
        var (fromBefore, toBefore) = (Gap(from), Gap(to));
        Scatter(from, Thermometer.RemoveOne(Tile.Gather(Tile, planes, from)));
        Scatter(to, Thermometer.AddOne(Tile.Gather(Tile, planes, to)));
        return new GapMove(row, from, to, fromBefore, toBefore, before, SquareSum);
    }

    /// <summary>Steps until a full scan makes no move, or the budget runs out.</summary>
    public GapSettlement Settle(int budget = 10_000)
    {
        var moves = new List<GapMove>();
        var idle = 0;
        for (var i = 0; i < budget && idle < Pairs.Count; i++)
        {
            if (Step() is { } move)
            {
                moves.Add(move);
                idle = 0;
            }
            else idle++;
        }
        var quiet = idle >= Pairs.Count;
        return new GapSettlement(moves, quiet, Spread, SquareSum, quiet ? MissingRoutes() : []);
    }

    /// <summary>Pairs of sites that differ by two or more with no row between them:
    /// the routes whose absence keeps a quiet state from evening out.</summary>
    public IReadOnlyList<(int A, int B)> MissingRoutes()
    {
        var missing = new List<(int, int)>();
        for (var a = 0; a < Tile.SiteCount; a++)
            for (var b = a + 1; b < Tile.SiteCount; b++)
                if (Math.Abs(Gap(a) - Gap(b)) >= 2 && !Enumerable.Range(0, Pairs.Count).Any(r =>
                        Pairs.Row(r) is var p && ((p[0] == a && p[1] == b) || (p[0] == b && p[1] == a))))
                    missing.Add((a, b));
        return missing;
    }

    private void Scatter(int site, ulong code)
    {
        if (code > Bits.Low(planes.Length)) throw new InvalidOperationException($"Gap at site {site} exceeds its thermometer capacity.");
        for (var k = 0; k < planes.Length; k++)
            Tile.WriteState(planes[k], Bits.Get(code, k) ? Bits.Of(site) : 0, Bits.Of(site));
    }
}
