namespace Gojo.Experiments.PrimitiveMeasure;

/// <summary>Half-open use ranges over one immutable, owner-local edge catalogue.</summary>
public sealed class WalkWindows
{
    private readonly OrderedBoundary catalogue;
    private readonly EndpointRows ranges = new();
    public int Bits { get; }
    public int Count => ranges.Count(2, Bits);
    public WalkWindows(OrderedBoundary catalogue, params (int First, int End)[] windows)
    {
        this.catalogue = catalogue;
        Bits = OrderedBoundary.Bits(catalogue.UseCount + 1);
        foreach (var (first, end) in windows)
        {
            if (first < 0 || first >= end || end > catalogue.UseCount) throw new ArgumentOutOfRangeException(nameof(windows));
            ranges.Append(Bits, first, end);
        }
    }

    public int[] Range(int view) => ranges.Read(new(2, Bits, view));
    public byte[] Bytes() => ranges.PackedBytes(2, Bits);
    public bool IsClosed(int view)
    {
        var range = Range(view);
        var start = catalogue.DirectedUse(range[0]);
        var arrival = start[1];
        for (var i = range[0] + 1; i < range[1]; i++)
        {
            var next = catalogue.DirectedUse(i);
            if (next[0] != arrival) return false;
            arrival = next[1];
        }
        return arrival == start[0];
    }
}

/// <summary>Two readings of a stroke. The fourth use is the bridge. Rung order
/// records whether the selected construction adds it or omits it.</summary>
public sealed class StrokeRung
{
    public OrderedBoundary Catalogue { get; } = new(4, [[0, 1], [1, 2], [2, 3], [3, 0]], [0, 2, 4, 6]);
    public WalkWindows Views { get; }
    private readonly EndpointRows rung = new();
    public StrokeRung(bool startWithTemplate)
    {
        Views = new(Catalogue, (0, 3), (0, 4));
        if (startWithTemplate) rung.Append(1, 1, 0);
        else rung.Append(1, 0, 1);
    }
    public int[] Correspondence() => rung.Read(new(2, 1, 0));
    public byte[] RungBytes() => rung.PackedBytes(2, 1);
}
