namespace Gojo.Experiments.PrimitiveMeasure;

/// <summary>Demonstration apparatus, NOT part of the three-bit encoding.
/// Signed integers stand in for finer material in one explicitly chosen local,
/// positive-right basis. The predictor sees only Observe's three-bit output.
/// This fixture selects a lossy signed-net fold: matched opposition is not
/// retained. It is not a general physical law. See RetainedFlowFixture for a
/// different structure with the same net reads but retained participation.
/// Checked overflow leaves every input unchanged.</summary>
public static class FineFlowFixture
{
    // This apparatus chooses positive direction at exactly zero. That choice
    // is not a deduction from coarse inputs; both zero directions remain valid.
    public static byte Observe(long amount) => ThreeBitMeasure.Encode(false, amount < 0, amount != 0);

    /// <summary>Consume two independent stores into one returned NET flow.
    /// Their old slots are emptied; original participation cannot be recovered
    /// from this result. Subsequent opposed reads use the single net result.</summary>
    public static long Join(Span<long> pair)
    {
        if (pair.Length != 2) throw new ArgumentException("Expected two independent stores.", nameof(pair));
        var joined = checked(pair[0] + pair[1]);
        pair.Clear();
        return joined;
    }

    /// <summary>A passage between two stores. An open matching passage moves
    /// all source participation into the receiver. A blocked source stays put.
    /// The two slots remain separate after either operation.</summary>
    public static bool Transfer(Span<long> pair, byte openings)
    {
        if (pair.Length != 2) throw new ArgumentException("Expected source and receiver.", nameof(pair));
        if (SignPassage.Select(Observe(pair[0]), openings) is null) return false;
        var joined = checked(pair[0] + pair[1]);
        pair[0] = 0;
        pair[1] = joined;
        return true;
    }

    /// <summary>Layout: [settled source, negative outlet, zero outlet, positive
    /// outlet]. Move into the one open outlet selected by the actual source
    /// sign, or retain the source when blocked. Zero selects a position but
    /// transports no material. No prediction is executed as if it were a flow.</summary>
    public static int? Route(Span<long> cells, byte openings)
    {
        if (cells.Length != 4) throw new ArgumentException("Expected source and three ordered outlets.", nameof(cells));
        var selected = SignPassage.Select(Observe(cells[0]), openings);
        if (selected is not int position) return null;
        var destination = position + 1;
        var combined = checked(cells[destination] + cells[0]);
        cells[0] = 0;
        cells[destination] = combined;
        return position;
    }
}
