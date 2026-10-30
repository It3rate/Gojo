namespace Gojo.Experiments.PrimitiveMeasure;

/// <summary>Fine demonstration apparatus in one positive-right basis. Four
/// nonnegative stores are ordered [A+, B+, A-, B-]. Opposed participation stays
/// present; a net read never consumes it. Observe derives two width-one tables
/// in one byte: [O=0,D=0,A+V,B+V | O=0,D=1,A-V,B-V], low bit first.
/// Fine amounts are not fields on the primitive encoding or its predictions.</summary>
public static class RetainedFlowFixture
{
    public static byte Observe(ReadOnlySpan<long> cells)
    {
        CheckCells(cells);
        byte tile = 0b00100000;
        for (var index = 0; index < 4; index++)
            if (cells[index] != 0) tile |= (byte)(1 << (index < 2 ? index + 2 : index + 4));
        return tile;
    }

    public static long Net(ReadOnlySpan<long> cells, int column)
    {
        CheckCells(cells);
        CheckColumn(column);
        return cells[column] - cells[column + 2];
    }

    /// <summary>Prediction reads the encoded table headers and participation,
    /// never the fine stores. Two nonzero sides do not establish balance.
    /// Different table orientations retain the primitive's no-common-read case.</summary>
    public static byte[] PredictNet(byte tile, int column)
    {
        CheckColumn(column);
        var positive = ContextTable.Read([(byte)(tile & 15)], 1, 2, column);
        var negative = ContextTable.Read([(byte)(tile >> 4)], 1, 2, column);
        return ThreeBitMeasure.Add(positive, negative);
    }

    /// <summary>Move all source participation through one same-sign link.</summary>
    public static long Transfer(Span<long> cells, byte link)
    {
        CheckCells(cells);
        var (source, target) = Passage(link);
        return Move(cells, source, target, cells[source]);
    }

    /// <summary>Withdraw at most requested into an actual receiver. Unavailable
    /// draw is requested minus the return value; it is not manufactured as flow.
    /// Source occupancy can reach zero without changing either table's sign.</summary>
    public static long TransferUpTo(Span<long> cells, byte link, long requested)
    {
        CheckCells(cells);
        ArgumentOutOfRangeException.ThrowIfNegative(requested);
        var (source, target) = Passage(link);
        return Move(cells, source, target, Math.Min(requested, cells[source]));
    }

    /// <summary>The ordered pair must contain the positive link followed by
    /// the negative link between the same two distinct columns. Move an equal
    /// amount along both, preserving each column's net read. The return value
    /// is participation moved in EACH side, not a newly created signed amount.</summary>
    public static long TransferPair(Span<long> cells, ushort pair)
    {
        CheckCells(cells);
        var positive = Passage(LocalConnection.At(pair, 0));
        var negative = Passage(LocalConnection.At(pair, 1));
        if (positive.Source > 1 || positive.Source == positive.Target ||
            negative.Source != positive.Source + 2 || negative.Target != positive.Target + 2)
            throw new ArgumentException("Expected matching positive then negative links between distinct columns.", nameof(pair));
        var amount = Math.Min(cells[positive.Source], cells[negative.Source]);
        var positiveTarget = checked(cells[positive.Target] + amount);
        var negativeTarget = checked(cells[negative.Target] + amount);
        cells[positive.Source] -= amount;
        cells[negative.Source] -= amount;
        cells[positive.Target] = positiveTarget;
        cells[negative.Target] = negativeTarget;
        return amount;
    }

    /// <summary>Observe the control site once, then execute the attached
    /// material link if its chosen face matches. Validate the link even when
    /// closed. The selected control bit is neither copied nor consumed.</summary>
    public static long TransferWhen(Span<long> cells, ushort conditional)
    {
        CheckCells(cells);
        var link = LocalConnection.MaterialLink(conditional);
        var (source, target) = Passage(link);
        return LocalConnection.IsOpen(Observe(cells), conditional)
            ? Move(cells, source, target, cells[source]) : 0;
    }

    private static long Move(Span<long> cells, int source, int target, long amount)
    {
        if (source == target) return 0;
        var targetAmount = checked(cells[target] + amount);
        cells[source] -= amount;
        cells[target] = targetAmount;
        return amount;
    }

    private static (int Source, int Target) Passage(byte link)
    {
        var source = Cell(LocalConnection.Source(link));
        var target = Cell(LocalConnection.Target(link));
        if (source / 2 != target / 2)
            throw new ArgumentException("This passage requires the same fixed sign at both ends.", nameof(link));
        return (source, target);
    }

    private static int Cell(int bitPosition)
    {
        if (bitPosition is not (2 or 3 or 6 or 7))
            throw new ArgumentException("A material endpoint must be an instance V bit, not a shared O or D bit.");
        return bitPosition < 4 ? bitPosition - 2 : bitPosition - 4;
    }

    private static void CheckCells(ReadOnlySpan<long> cells)
    {
        if (cells.Length != 4) throw new ArgumentException("Expected [A+, B+, A-, B-].", nameof(cells));
        foreach (var amount in cells)
            if (amount < 0) throw new ArgumentException("Each fixed-sign store holds a nonnegative magnitude.", nameof(cells));
    }

    private static void CheckColumn(int column)
    {
        if (column is < 0 or > 1) throw new ArgumentOutOfRangeException(nameof(column));
    }
}
