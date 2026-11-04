namespace Gojo.Experiments.PrimitiveMeasure;

/// <summary>A possible weak order of A-start, A-tip, B-start, B-tip. The
/// four two-bit fields are ordinal ranks in this read, never distances.</summary>
public readonly record struct SpanPlacement(bool Reversed, byte Ranks)
{
    private int Rank(int role) => (Ranks >> (2 * role)) & 3;

    // Enumerate the 75 distinct four-role orders once. Skipping an unused
    // rank would encode the same ordering twice.
    internal static readonly byte[] Orders = Enumerable.Range(0, 256).Select(n => (byte)n)
        .Where(code =>
        {
            var order = new SpanPlacement(false, code);
            var levels = Enumerable.Range(0, 4).Select(order.Rank).Distinct().ToArray();
            return levels.Length == levels.Max() + 1;
        }).ToArray();

    private static byte Order(int a, int b) => a == b ? (byte)8 : a > b ? (byte)2 : (byte)4;
    internal bool Fits(byte[,] allowed)
    {
        for (int i = 0; i < 4; i++)
            for (int j = i+1; j < 4; j++)
                if ((Order(Rank(i), Rank(j)) & allowed[i,j]) == 0) return false;
        return true;
    }

    public byte TipOrder => Order(Rank(1), Rank(3));

    public (byte Extents, byte Intersection) Spans()
    {
        int a = Math.Min(Rank(0), Rank(1)), b = Math.Max(Rank(0), Rank(1));
        int c = Math.Min(Rank(2), Rank(3)), d = Math.Max(Rank(2), Rank(3));
        int start = Math.Max(a, c), end = Math.Min(b, d);
        byte intersection = end < start ? (byte)1 : end == start ? (byte)2 : (byte)8;
        // Containment or a point can settle extent order. Where each span
        // has its own positive part, arbitrary positive gaps allow any order.
        byte extents;
        if (a == b || c == d) extents = Order(a == b ? 0 : 1, c == d ? 0 : 1);
        else if (a == c && b == d) extents = 8;
        else if (a <= c && d <= b) extents = 2;
        else if (c <= a && b <= d) extents = 4;
        else extents = 14;
        return (extents, intersection);
    }
}

/// <summary>Public extent port for two closed occupied spans. Each reread
/// uses the current contacts. Inspect exposes the joint evidence to its owner.</summary>
public sealed class SpanComparison : PresencePairPort
{
    private readonly RailContacts dock;
    private readonly EndpointRows spans = new();
    internal SpanComparison(RailContacts dock, (int Start, int Tip) a, (int Start, int Tip) b)
    {
        this.dock = dock;
        spans.Append(dock.Bits, a.Start, a.Tip);
        spans.Append(dock.Bits, b.Start, b.Tip);
    }

    public byte[] Bytes() => spans.PackedBytes(2, dock.Bits);
    public SpanComparisonRead Inspect()
    {
        var placements = dock.Placements(spans.Read(new(2, dock.Bits, 0)), spans.Read(new(2, dock.Bits, 1)));
        return new(dock.Count, placements);
    }
    public override byte ReadPairs() => Inspect().ExtentPairs;
}

public sealed class SpanComparisonRead
{
    public int Contacts { get; }
    public IReadOnlyList<SpanPlacement> Placements { get; }
    public byte Faces { get; }
    public byte TipPairs { get; }
    public byte ExtentPairs { get; }
    public byte IntersectionPairs { get; }

    internal SpanComparisonRead(int contacts, SpanPlacement[] placements)
    {
        Contacts = contacts;
        Placements = Array.AsReadOnly(placements);
        foreach (var placement in placements)
        {
            Faces |= (byte)(placement.Reversed ? 2 : 1);
            TipPairs |= placement.TipOrder;
            var span = placement.Spans();
            ExtentPairs |= span.Extents;
            IntersectionPairs |= span.Intersection;
        }
    }
}
