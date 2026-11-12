namespace Gojo.EncodingC;

/// <summary>Selections are planes. A selection makes no claim about what it
/// leaves out: within it, an unselected position and a missing one read the
/// same. These helpers build selections from structure (rows and order), not
/// from lists of names.</summary>
public static class Selections
{
    /// <summary>Every <paramref name="step"/>-th position from <paramref name="phase"/>.</summary>
    public static Plane Every(int length, int step, int phase = 0)
    {
        if (step < 1) throw new ArgumentOutOfRangeException(nameof(step));
        var plane = new Plane(length);
        for (var i = phase; i < length; i += step) plane[i] = true;
        return plane;
    }

    /// <summary>Positions that share a row of <paramref name="rows"/> with a
    /// position in <paramref name="frontier"/>, plus the frontier. Rows bound to
    /// another surface are ignored.</summary>
    public static Plane Expand(Plane frontier, Wiring rows, Surface store)
    {
        ArgumentNullException.ThrowIfNull(frontier);
        ArgumentNullException.ThrowIfNull(rows);
        ArgumentNullException.ThrowIfNull(store);
        var next = frontier.Clone();
        var local = Enumerable.Range(0, rows.Bindings.Count).Where(r => ReferenceEquals(rows.Bindings[r].Surface, store)).ToArray();
        foreach (var row in rows.Live.Indexes())
        {
            var indexes = rows.Row(row);
            if (local.Any(r => frontier[indexes[r]]))
                foreach (var r in local) next[indexes[r]] = true;
        }
        return next;
    }

    /// <summary>Positions within <paramref name="steps"/> rows of the seeds.</summary>
    public static Plane Within(Plane seeds, int steps, Wiring rows, Surface store)
    {
        var selection = seeds;
        for (var i = 0; i < steps; i++) selection = Expand(selection, rows, store);
        return selection;
    }
}
