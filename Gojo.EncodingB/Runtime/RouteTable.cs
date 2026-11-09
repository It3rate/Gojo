namespace Gojo.EncodingB;

/// <summary>Response routes over a tile's (from, to) rows, each with a small
/// saturating strength 0..4 (learning rung L1). Strength rises when a unit sent
/// along the route was accepted and falls when it was blocked: a counter on a
/// named route, so a preference can always be traced to its route.</summary>
public sealed class RouteTable
{
    public const int MaxStrength = 4;

    private readonly byte[] strength;

    public RouteTable(Tile tile, string rowSet, int initialStrength = 2)
    {
        ArgumentNullException.ThrowIfNull(tile);
        if (initialStrength is < 0 or > MaxStrength) throw new ArgumentOutOfRangeException(nameof(initialStrength));
        Rows = tile.Rows(rowSet);
        if (Rows.Roles.Count != 2) throw new ArgumentException("Routes are (from, to) rows.", nameof(rowSet));
        strength = Enumerable.Repeat((byte)initialStrength, Rows.Count).ToArray();
    }

    public RowSet Rows { get; }
    public int Count => Rows.Count;

    public int Strength(int route) => strength[route];
    public int From(int route) => Rows.Row(route)[0];
    public int To(int route) => Rows.Row(route)[1];

    /// <summary>The strongest route leaving a site; ties go to the lower row.
    /// Null when no route leaves it.</summary>
    public int? Strongest(int site)
    {
        int? best = null;
        for (var route = 0; route < Count; route++)
            if (From(route) == site && (best is null || strength[route] > strength[best.Value])) best = route;
        return best;
    }

    /// <summary>Acknowledged relief raises a route; a block lowers it. Saturates at 0 and 4.</summary>
    public void Revise(int route, bool relieved) =>
        strength[route] = (byte)Math.Clamp(strength[route] + (relieved ? 1 : -1), 0, MaxStrength);

    public byte[] Snapshot() => strength.ToArray();

    public void Restore(byte[] snapshot) => snapshot.CopyTo(strength, 0);
}
