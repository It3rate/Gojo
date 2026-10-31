namespace Gojo.Experiments.PrimitiveMeasure;

/// <summary>Builders compile selected experimental read laws into inspectable
/// bits. ReadConnections executes only those bits, never these method names.
/// No law here moves material or guesses an actual value from alternatives.</summary>
public static class ConnectionLaws
{
    public static BitRelation Identity() => BitRelation.FromTuples(2,
        Enumerable.Range(0, 8).Select(word => new[] { (byte)word, (byte)word }));

    /// <summary>Two distinguishable parts, with the same orientation and
    /// direction, expose presence of either part. This is qualitative refinement
    /// of occupancy, not a numeric binary magnitude or a cancellation rule.</summary>
    public static BitRelation Presence()
    {
        var tuples = new List<byte[]>();
        for (byte context = 0; context < 8; context += 2)
            for (byte first = 0; first < 2; first++)
                for (byte second = 0; second < 2; second++)
                    tuples.Add([(byte)(context | first), (byte)(context | second), (byte)(context | first | second)]);
        return BitRelation.FromTuples(3, tuples);
    }

    /// <summary>Exactly one input role reads occupied in a shared O/D context.
    /// This compares presence bits, not underlying amounts. Repeated references
    /// retain their shared candidate through BitRelation's reference-aware read.</summary>
    public static BitRelation ExclusivePresence()
    {
        var tuples = new List<byte[]>();
        for (byte context = 0; context < 8; context += 2)
            for (byte first = 0; first < 2; first++)
                for (byte second = 0; second < 2; second++)
                    tuples.Add([(byte)(context | first), (byte)(context | second), (byte)(context | (first ^ second))]);
        return BitRelation.FromTuples(3, tuples);
    }

    /// <summary>Compile the existing conservative ODV addition read. Opposing
    /// nonzero inputs retain every admitted result, including both zero signs.</summary>
    public static BitRelation Addition()
    {
        var tuples = new List<byte[]>();
        for (byte first = 0; first < 8; first++)
            for (byte second = 0; second < 8; second++)
                foreach (var result in ThreeBitMeasure.Add(first, second)) tuples.Add([first, second, result]);
        return BitRelation.FromTuples(3, tuples);
    }
}
