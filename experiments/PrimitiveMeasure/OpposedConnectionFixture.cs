namespace Gojo.Experiments.PrimitiveMeasure;

/// <summary>Fine demonstration apparatus for one ordered four-ended law:
/// [active positive, active negative, retained positive, retained negative].
/// Each endpoint names a nonnegative amount in the supplied local cells. These
/// fine amounts belong to the demonstration world, not the primitive payload,
/// connection row, or coarse predictor. The caller selects one common orientation.</summary>
public static class OpposedConnectionFixture
{
    /// <summary>Route equal opposing participation out of the active pair.
    /// The return value is the amount moved on EACH sign, not a third net amount.
    /// Per-sign totals and the overall net are preserved; at most one active
    /// side remains occupied. All destination arithmetic is checked before writing.</summary>
    public static long Settle(Span<long> cells, EndpointRows rows, ConnectionRow row)
    {
        var ends = Validate(cells, rows, row);
        var matched = Math.Min(cells[ends[0]], cells[ends[1]]);
        var retainedPositive = checked(cells[ends[2]] + matched);
        var retainedNegative = checked(cells[ends[3]] + matched);
        cells[ends[0]] -= matched;
        cells[ends[1]] -= matched;
        cells[ends[2]] = retainedPositive;
        cells[ends[3]] = retainedNegative;
        return matched;
    }

    /// <summary>Read only an already settled active pair. Two occupied active
    /// sides remain unresolved here: this reader neither compares their fine
    /// magnitudes nor runs settlement. At empty active zero, previous D supplies
    /// explicit controller memory. The returned ODV word does not erase the
    /// connection's two active roles or its retained opposing participation.</summary>
    public static byte ReadSettled(ReadOnlySpan<long> cells, EndpointRows rows, ConnectionRow row, byte previous)
    {
        if (previous > 7) throw new ArgumentOutOfRangeException(nameof(previous));
        var ends = Validate(cells, rows, row);
        var positive = cells[ends[0]] != 0;
        var negative = cells[ends[1]] != 0;
        if (positive && negative)
            throw new InvalidOperationException("Both active sides remain occupied; settlement has not produced one net side.");
        var observed = ThreeBitMeasure.Encode((previous & ThreeBitMeasure.Orientation) != 0,
            negative, positive || negative);
        return DirectionMemory.Update(previous, observed)!.Value;
    }

    private static int[] Validate(ReadOnlySpan<long> cells, EndpointRows rows, ConnectionRow row)
    {
        ArgumentNullException.ThrowIfNull(rows);
        if (row.Arity != 4) throw new ArgumentException("Expected four fixed-sign endpoint roles.", nameof(row));
        var ends = rows.Read(row);
        for (var role = 0; role < ends.Length; role++)
        {
            if (ends[role] >= cells.Length)
                throw new ArgumentOutOfRangeException(nameof(cells), "A connected endpoint is outside the supplied cells.");
            if (cells[ends[role]] < 0)
                throw new ArgumentException("Each connected fixed-sign store requires a nonnegative magnitude.", nameof(cells));
            for (var earlier = 0; earlier < role; earlier++)
                if (ends[role] == ends[earlier])
                    throw new ArgumentException("This transfer requires four distinct stores; endpoint aliases would duplicate ownership.", nameof(row));
        }
        return ends;
    }
}
