namespace Gojo.Encoding.Core.Packed;

/// <summary>Builders compile selected detection/Boolean read laws into inspectable
/// bits. ReadConnections executes only those bits, never these method names.
/// No law here moves material or guesses an actual value from alternatives.</summary>
public static class ConnectionLaws
{
    /// <summary>In the selected positive Boolean basis, first reads 1 and second
    /// reads 0. This is a predicate on reads, not a material subtraction.</summary>
    public static BitRelation FirstWithoutSecond { get; } = BitRelation.FromTuples(3,
        [[0, 0, 0], [0, 1, 0], [1, 0, 1], [1, 1, 0]]);

    public static BitRelation Identity() => BitRelation.FromTuples(2,
        Enumerable.Range(0, 8).Select(word => new[] { (byte)word, (byte)word }));

    /// <summary>OR of the already observed V bits in a shared O/D context.
    /// This asks whether either input was detected. It does not predict whether
    /// their combined quantity is detectable: two subthreshold amounts may add
    /// to a detectable amount. Clear output is not an empty physical receiver.</summary>
    public static BitRelation Presence()
    {
        var tuples = new List<byte[]>();
        for (byte context = 0; context < 8; context += 2)
            for (byte first = 0; first < 2; first++)
                for (byte second = 0; second < 2; second++)
                    tuples.Add([(byte)(context | first), (byte)(context | second), (byte)(context | first | second)]);
        return BitRelation.FromTuples(3, tuples);
    }

    /// <summary>Exactly one input role reads detectable in a shared O/D context.
    /// This compares detection bits, not underlying amounts. Repeated references
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

}
