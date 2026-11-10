namespace Gojo.Encoding.Core.Packed;

public enum LandmarkRead { Outside, Conflict, SameSite, Before, After, Unresolved }

/// <summary>A selected directed carrier. Ordered storage supplies successive
/// joins. In a partial layout, the same unary fields are membership only and
/// a separate table supplies the strict comparisons in this carrier's basis.</summary>
public sealed class LandmarkRun
{
    private readonly EndpointRows members = new();
    private readonly EndpointRows? precedes;
    public int Bits { get; }
    public bool IsChain => precedes is null;
    public int Count => members.Count(1, Bits);
    public int ClaimCount => precedes?.Count(2, Bits) ?? 0;

    public LandmarkRun(int siteBits, int[] sites, int[][]? precedence = null)
    {
        if (sites.Length < 2 || sites.Distinct().Count() != sites.Length)
            throw new ArgumentException("This run layout requires at least two distinct landmarks.");
        Bits = siteBits;
        foreach (var site in sites) members.Append(Bits, site);
        if (precedence is null) return;
        precedes = new EndpointRows();
        foreach (var pair in precedence)
        {
            if (pair.Length != 2 || !sites.Contains(pair[0]) || !sites.Contains(pair[1]))
                throw new ArgumentException("Comparisons must name two landmarks on this run.");
            precedes.Append(Bits, pair);
        }
    }

    public int[] Sites() => Enumerable.Range(0, Count).Select(i => members.Read(new(1, Bits, i))[0]).ToArray();
    public int[][] Claims() => Enumerable.Range(0, ClaimCount).Select(i => precedes!.Read(new(2, Bits, i))).ToArray();
    public byte[] SiteBytes() => members.PackedBytes(1, Bits);
    public byte[] ClaimBytes() => precedes?.PackedBytes(2, Bits) ?? [];

    private bool[,] Reach(int[] sites)
    {
        var result = new bool[sites.Length, sites.Length];
        foreach (var pair in Claims()) result[Array.IndexOf(sites, pair[0]), Array.IndexOf(sites, pair[1])] = true;
        CloseOrder(result);
        return result;
    }

    internal static void CloseOrder(bool[,] result)
    {
        int count = result.GetLength(0);
        for (var k = 0; k < count; k++)
            for (var i = 0; i < count; i++)
                if (result[i,k])
                    for (var j = 0; j < count; j++) result[i,j] |= result[k,j];
    }

    public LandmarkRead Compare(int a, int b, bool reversed = false)
    {
        var sites = Sites();
        var first = Array.IndexOf(sites, reversed ? b : a);
        var second = Array.IndexOf(sites, reversed ? a : b);
        if (first < 0 || second < 0) return LandmarkRead.Outside;
        if (IsChain)
        {
            if (first == second) return LandmarkRead.SameSite;
            return first < second ? LandmarkRead.Before : LandmarkRead.After;
        }
        var reach = Reach(sites);
        if (Enumerable.Range(0, sites.Length).Any(i => reach[i,i])) return LandmarkRead.Conflict;
        if (first == second) return LandmarkRead.SameSite;
        if (reach[first,second]) return LandmarkRead.Before;
        return reach[second,first] ? LandmarkRead.After : LandmarkRead.Unresolved;
    }

    // This optional search can grow factorially. Compare answers from
    // reachability alone, with no need to enumerate or pick a linear order.
    public IEnumerable<int[]> Orders()
    {
        var sites = Sites();
        if (IsChain) { yield return sites; yield break; }
        var reach = Reach(sites);
        var used = new bool[sites.Length];
        var order = new int[sites.Length];
        foreach (var completion in Extend(0)) yield return completion;
        IEnumerable<int[]> Extend(int at)
        {
            if (at == sites.Length) { yield return order.ToArray(); yield break; }
            for (var i = 0; i < sites.Length; i++)
            {
                if (used[i] || Enumerable.Range(0, sites.Length).Any(j => !used[j] && reach[j,i])) continue;
                order[at] = sites[i];
                used[i] = true;
                foreach (var next in Extend(at + 1)) yield return next;
                used[i] = false;
            }
        }
    }
}
